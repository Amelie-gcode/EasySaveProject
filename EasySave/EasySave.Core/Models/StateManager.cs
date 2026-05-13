using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Models
{
    public class JobStateData
    {
        public string Name { get; set; }
        public string LastUpdate { get; set; }
        public string Status { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int Progress { get; set; }
        public int FilesRemaining { get; set; }
        public long SizeRemaining { get; set; }
        /// <summary>Job root folder (always set).</summary>
        public string SourceDirectory { get; set; }
        /// <summary>Job root folder (always set).</summary>
        public string TargetDirectory { get; set; }
        /// <summary>File currently being copied (empty when idle / between files).</summary>
        public string CurrentSource { get; set; }
        public string CurrentDestination { get; set; }
        /// <summary>Set when Status is Completed (last file copied).</summary>
        public string CompletedAt { get; set; }
    }

    public class StateManager
    {
        private readonly string _stateFilePath;
        private readonly object _fileLock = new object();

        // ONE entry per job — replaced on every update, never accumulated
        private readonly Dictionary<string, JobStateData> _states
            = new Dictionary<string, JobStateData>();

        public StateManager()
        {
            string appData = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            string directory = Path.Combine(appData, "EasySave");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _stateFilePath = Path.Combine(directory, "state.json");
        }

        // Called by BackupJob every time progress changes
        public void OnJobProgressUpdated(object sender, EventArgs e)
        {
            if (sender is BackupJob job)
                UpdateStateFile(job);
        }

        /// <summary>
        /// Persists current job fields to state.json (used after the ProgressUpdated
        /// handler is unsubscribed, e.g. delayed completion cleanup).
        /// </summary>
        public void PublishFromJob(BackupJob job)
        {
            if (job != null)
                UpdateStateFile(job);
        }

        private void UpdateStateFile(BackupJob job)
        {
            lock (_fileLock)
            {
                int progress = ComputeProgressPercent(job);

                string completedAt = string.Empty;
                if (job.State == JobState.Completed)
                    completedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Replace this job's entry — never add a new one
                _states[job.Name] = new JobStateData
                {
                    Name = job.Name,
                    LastUpdate = DateTime.Now
                                           .ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = job.State.ToString(),
                    TotalFiles = job.TotalFiles,
                    TotalSize = job.TotalSize,
                    Progress = progress,
                    FilesRemaining = Math.Max(0, job.FilesRemaining),
                    SizeRemaining = Math.Max(0, job.SizeRemaining),
                    SourceDirectory = job.SourcePath ?? string.Empty,
                    TargetDirectory = job.TargetPath ?? string.Empty,
                    CurrentSource = job.CurrentSourceFile ?? string.Empty,
                    CurrentDestination = job.CurrentTargetFile ?? string.Empty,
                    CompletedAt = completedAt
                };

                WriteFile();
            }
        }

        private static int ComputeProgressPercent(BackupJob job)
        {
            if (job.State == JobState.Completed)
                return 100;

            if (job.TotalSize > 0)
            {
                long transferred = job.TotalSize - job.SizeRemaining;
                if (transferred < 0) transferred = 0;
                if (transferred > job.TotalSize) transferred = job.TotalSize;
                return (int)(transferred * 100L / job.TotalSize);
            }

            // No byte total (empty tree): derive from file counts
            if (job.TotalFiles > 0)
            {
                int done = job.TotalFiles - Math.Max(0, job.FilesRemaining);
                if (done < 0) done = 0;
                if (done > job.TotalFiles) done = job.TotalFiles;
                return done * 100 / job.TotalFiles;
            }

            return 0;
        }

        // Called after 5s timer to remove the job entry
        public void ClearJobState(string jobName)
        {
            lock (_fileLock)
            {
                _states.Remove(jobName);
                WriteFile();
            }
        }

        private void WriteFile()
        {
            try
            {
                var options = new JsonSerializerOptions
                { WriteIndented = true };
                var list = new List<JobStateData>(_states.Values);
                string json = JsonSerializer.Serialize(list, options);
                File.WriteAllText(_stateFilePath, json);
            }
            catch (IOException) { }
        }
    }
}