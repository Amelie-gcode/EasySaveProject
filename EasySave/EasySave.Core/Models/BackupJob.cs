
using EasyLog;
using EasyLog;
using EasySave.Core.Strategies;
using EasySave.Strategies;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;

namespace EasySave.Models
{
    /// <summary>
    /// Represents a single backup task with its configuration and current state.
    /// </summary>
    public class BackupJob
    {
        // Basic properties for the job
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }

        // State tracking for state.json
        public JobState State { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int FilesRemaining { get; set; }
        public long SizeRemaining { get; set; }
        public string CurrentSourceFile { get; set; }
        public string CurrentTargetFile { get; set; }

        // Strategy used (Full or Differential)
        private readonly IBackupStrategy _strategy;

        // Event used for the Observer pattern
        public event EventHandler ProgressUpdated;

        private readonly ManualResetEventSlim _pauseGate = new ManualResetEventSlim(true);
        private int _cancelRequested;
        // Encryption Service 
        public EncryptionService Encryption { get; set; }
        public string EncryptionKey { get; set; }


        //needed to check business software during execution
        public BusinessSoftwareService BusinessService { get; set; }
        public AppSettings Settings { get; set; }

        private static int _globalPriorityFilesCount = 0;
        // Track priority files for THIS specific job instance
        public int LocalPriorityFilesCount { get; set; }

        public static bool OthersHavePriority(int myCount)
        {
            // Only wait if the global total is higher than my own total
            return Volatile.Read(ref _globalPriorityFilesCount) > myCount;
        }

        public static void IncrementGlobalPriority() => Interlocked.Increment(ref _globalPriorityFilesCount);
        public static void DecrementGlobalPriority() => Interlocked.Decrement(ref _globalPriorityFilesCount);
        public BackupJob(string name, string source, string target, IBackupStrategy strategy)
        {
            Name = name;
            SourcePath = source;
            TargetPath = target;
            _strategy = strategy;
            State = JobState.Inactive;
            BusinessService = new BusinessSoftwareService();
        }

        public IBackupStrategy GetStrategy()
        {
            return _strategy;
        }
        /// <summary>
        /// Starts the backup process using the assigned strategy.
        /// </summary>
        /// <summary>
        /// Executes the backup job with pre-flight path validation.
        /// </summary>
        public async Task Execute()
        {
            // CHECK #1 — before the backup starts
            if (Settings != null && BusinessService != null && BusinessService.IsBusinessSoftwareRunning(Settings.BusinessSoftwareName))
            {
                string detected = BusinessService.GetDetectedSoftwareName(Settings.BusinessSoftwareName);
                State = JobState.Cancelled;
                FilesRemaining = 0;
                SizeRemaining = 0;

                EasyLogger.Instance.WriteLog(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = Name,
                    SourceFilePath = $"BLOCKED by: {detected}",
                    TargetFilePath = string.Empty,
                    FileSize = 0,
                    TransferTimeMs = -1
                });

                NotifyProgress();
                return;
            }
            this.State = JobState.Active;
            NotifyProgress();

            // 1. SOURCE CHECK: Verify that the source directory actually exists
            if (!Directory.Exists(SourcePath))
            {
                HandlePathError("Source path not found or disconnected.");
                return; // Safely abort the execution
            }

            // 2. TARGET CHECK: Ensure the target exists, or attempt to create it
            try
            {
                if (!Directory.Exists(TargetPath))
                {
                    Directory.CreateDirectory(TargetPath);
                }
            }
            catch (Exception)
            {
                // Catches errors like UnauthorizedAccessException or if a network drive is offline
                HandlePathError("Target path could not be accessed or created.");
                return; // Safely abort the execution
            }
            await Task.Run(() => CalculateInitialStats());
            // Register this job's priority files globally
            for (int i = 0; i < LocalPriorityFilesCount; i++)
                IncrementGlobalPriority();

            // 3. EXECUTION: If both paths are valid, proceed with the Strategy
            try
            {
                // Offload the heavy scanning to a background thread to keep UI responsive
                // CRITICAL: The Strategy must now be Awaited
                await _strategy.ExecuteBackupAsync(SourcePath, TargetPath, this);

                // Only mark as completed if the strategy didn't encounter internal fatal errors
                if (this.State != JobState.Error && this.State != JobState.Cancelled)
                {
                    this.State = JobState.Completed;
                }
            }
            catch (OperationCanceledException)
            {
                this.State = JobState.Cancelled;
            }
            catch (Exception)
            {
                this.State = JobState.Error;
            }
            finally
            {
                while (LocalPriorityFilesCount > 0)
                {
                    DecrementGlobalPriority();
                    LocalPriorityFilesCount--;
                }
                NotifyProgress();
            }
        }


        public void RequestPause()
        {
            if (State != JobState.Active) return;
            State = JobState.Paused;
            _pauseGate.Reset();
            NotifyProgress();
        }

        public void RequestResume()
        {
            if (State != JobState.Paused) return;
            State = JobState.Active;
            _pauseGate.Set();
            NotifyProgress();
        }

        public void RequestCancel()
        {
            Interlocked.Exchange(ref _cancelRequested, 1);
            _pauseGate.Set(); // unblock if paused, so CheckPauseAndCancellation can throw
            State = JobState.Cancelled;
            NotifyProgress();
        }

        internal void CheckPauseAndCancellation()
        {
            if (Volatile.Read(ref _cancelRequested) == 1)
                throw new OperationCanceledException();

            if (!_pauseGate.IsSet)
            {
                // Wait with a timeout to prevent infinite blocking
                // If timeout occurs, check cancellation again
                if (!_pauseGate.Wait(1000))
                {
                    // Timeout occurred, check if cancel was requested
                    if (Volatile.Read(ref _cancelRequested) == 1)
                        throw new OperationCanceledException();

                    // Still paused, continue waiting
                    return;
                }

                // Gate was set (resumed), check one more time for cancellation
                if (Volatile.Read(ref _cancelRequested) == 1)
                    throw new OperationCanceledException();
            }
        }

        /// <summary>
        /// Helper method to centralize error handling and logging for missing paths.
        /// </summary>
        private void HandlePathError(string errorMessage)
        {
            this.State = JobState.Error;

            // Set remaining values to 0 since the job is aborted
            this.FilesRemaining = 0;
            this.SizeRemaining = 0;
            this.CurrentSourceFile = errorMessage;

            // Log the critical failure using your EasyLogger DLL
            // TransferTimeMs = -1 is the standard indicator for a failed transfer
            EasyLogger.Instance.WriteLog(new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = this.Name,
                SourceFilePath = this.SourcePath,
                TargetFilePath = this.TargetPath,
                FileSize = 0,
                TransferTimeMs = -1
            });

            // Trigger the event so the UI and state.json update immediately
            NotifyProgress();
        }
        /// <summary>
        /// Scans the source directory to provide totals for the real-time status file.
        /// </summary>
        private void CalculateInitialStats()
        {
            
            var files = Directory.GetFiles(SourcePath, "*.*", SearchOption.AllDirectories);
            TotalFiles = files.Length;
            FilesRemaining = TotalFiles;

            TotalSize = 0;
            foreach (var file in files)
            {
                TotalSize += new FileInfo(file).Length;
            }
            SizeRemaining = TotalSize;
            // Calculate how many priority files this specific job has
            int LocalPriorityFilesCount = files.Count(f => Settings.PriorityExtensions.Contains(Path.GetExtension(f)));

        }

        /// <summary>
        /// Broadcasts an update to all listeners (Observers).
        /// </summary>
        public void NotifyProgress()
        {
            ProgressUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}