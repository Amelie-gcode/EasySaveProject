
using EasySave.Strategies;
using System;
using System.IO;
using EasyLog;

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

        // Encryption Service 
        public EncryptionService Encryption { get; set; }
        public string EncryptionKey { get; set; }

        public BackupJob(string name, string source, string target, IBackupStrategy strategy)
        {
            Name = name;
            SourcePath = source;
            TargetPath = target;
            _strategy = strategy;
            State = JobState.Inactive;
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
        public void Execute()
        {
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

            // 3. EXECUTION: If both paths are valid, proceed with the Strategy
            try
            {
                CalculateInitialStats(); // Gather totals for progress tracking
                _strategy.ExecuteBackup(SourcePath, TargetPath, this);

                // Only mark as completed if the strategy didn't encounter internal fatal errors
                if (this.State != JobState.Error)
                {
                    this.State = JobState.Completed;
                }
            }
            catch (Exception)
            {
                this.State = JobState.Error;
            }
            finally
            {
                NotifyProgress();
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