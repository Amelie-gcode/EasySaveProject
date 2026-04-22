using EasySave.Strategies;
using System;
using System.IO;

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
        public void Execute()
        {
            try
            {
                // Initialize job stats
                State = JobState.Active;
                CalculateInitialStats();
                NotifyProgress();

                // Run the strategy logic
                _strategy.ExecuteBackup(SourcePath, TargetPath, this);

                State = JobState.Completed;
            }
            catch (Exception)
            {
                State = JobState.Error;
                throw;
            }
            finally
            {
                NotifyProgress();
            }
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