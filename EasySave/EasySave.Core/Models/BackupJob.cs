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
    public class BackupJob
    {
        // Basic properties
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

        // Strategy (Full or Differential)
        private readonly IBackupStrategy _strategy;

        // Observer pattern event
        public event EventHandler ProgressUpdated;

        // Pause / Cancel controls
        private readonly ManualResetEventSlim _pauseGate
            = new ManualResetEventSlim(true);
        private int _cancelRequested;
        private int _stateCleanupGeneration;

        // Dependencies injected by BackupManager
        public EncryptionService Encryption { get; set; }
        public string EncryptionKey { get; set; }
        public BusinessSoftwareService BusinessService { get; set; }
        public AppSettings Settings { get; set; }

        // Reference to StateManager for the 5s Inactive timer
        public StateManager StateManager { get; set; }

        public BackupJob(string name, string source,
                         string target, IBackupStrategy strategy)
        {
            Name = name;
            SourcePath = source;
            TargetPath = target;
            _strategy = strategy;
            State = JobState.Inactive;
            BusinessService = new BusinessSoftwareService();
        }

        public IBackupStrategy GetStrategy() => _strategy;

        public void Execute()
        {
            // Invalidate any pending "completed → cleanup" work from a prior run
            Interlocked.Increment(ref _stateCleanupGeneration);

            // CHECK #1 — block if business software is running
            if (Settings != null && BusinessService != null &&
                BusinessService.IsBusinessSoftwareRunning(
                    Settings.BusinessSoftwareName))
            {
                string detected = BusinessService
                    .GetDetectedSoftwareName(Settings.BusinessSoftwareName);

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

            State = JobState.Active;
            NotifyProgress();

            // CHECK #2 — source path must exist
            if (!Directory.Exists(SourcePath))
            {
                HandlePathError("Source path not found or disconnected.");
                return;
            }

            // CHECK #3 — target path must be accessible
            try
            {
                if (!Directory.Exists(TargetPath))
                    Directory.CreateDirectory(TargetPath);
            }
            catch
            {
                HandlePathError("Target path could not be accessed or created.");
                return;
            }

            // EXECUTE
            try
            {
                CalculateInitialStats();
                _strategy.ExecuteBackup(SourcePath, TargetPath, this);

                if (State != JobState.Error && State != JobState.Cancelled)
                {
                    // Mark as Completed — keep last file paths for one state.json snapshot
                    State = JobState.Completed;
                    FilesRemaining = 0;
                    SizeRemaining = 0;
                    NotifyProgress();

                    CurrentSourceFile = string.Empty;
                    CurrentTargetFile = string.Empty;

                    // After 5 seconds: Inactive + remove from state.json (direct StateManager
                    // call because BackupManager unsubscribes ProgressUpdated in finally).
                    int generation = Volatile.Read(ref _stateCleanupGeneration);
                    var jobRef = this;
                    var stateRef = this.StateManager;
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Thread.Sleep(5000);
                        if (Volatile.Read(ref jobRef._stateCleanupGeneration) != generation)
                            return;

                        jobRef.State = JobState.Inactive;
                        stateRef?.ClearJobState(jobRef.Name);
                    });
                }
            }
            catch (OperationCanceledException)
            {
                State = JobState.Cancelled;
                NotifyProgress();
            }
            catch (Exception)
            {
                State = JobState.Error;
                NotifyProgress();
            }
        }

        public void RequestPause()
        {
            if (State != JobState.Active) return;
            _pauseGate.Reset();
            State = JobState.Paused;
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
            _pauseGate.Set();
            State = JobState.Cancelled;
            NotifyProgress();
        }

        internal void CheckPauseAndCancellation()
        {
            if (Volatile.Read(ref _cancelRequested) == 1)
                throw new OperationCanceledException();

            if (!_pauseGate.IsSet)
            {
                State = JobState.Paused;
                NotifyProgress();
                _pauseGate.Wait();

                if (Volatile.Read(ref _cancelRequested) == 1)
                    throw new OperationCanceledException();

                State = JobState.Active;
                NotifyProgress();
            }
        }

        private void HandlePathError(string errorMessage)
        {
            State = JobState.Error;
            FilesRemaining = 0;
            SizeRemaining = 0;
            CurrentSourceFile = errorMessage;

            EasyLogger.Instance.WriteLog(new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = Name,
                SourceFilePath = SourcePath,
                TargetFilePath = TargetPath,
                FileSize = 0,
                TransferTimeMs = -1
            });

            NotifyProgress();
        }

        private void CalculateInitialStats()
        {
            var files = Directory.GetFiles(
                SourcePath, "*.*", SearchOption.AllDirectories);

            TotalFiles = files.Length;
            FilesRemaining = TotalFiles;
            TotalSize = 0;

            foreach (var file in files)
                TotalSize += new FileInfo(file).Length;

            SizeRemaining = TotalSize;
            CurrentSourceFile = string.Empty;
            CurrentTargetFile = string.Empty;

            NotifyProgress(); // write initial state immediately
        }

        public void NotifyProgress()
        {
            ProgressUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}