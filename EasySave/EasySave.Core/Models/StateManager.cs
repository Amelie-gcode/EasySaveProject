using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Models
{
    /// <summary>
    /// A structured model specifically for the JSON serialization of the state file.
    /// </summary>
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
        public string CurrentSource { get; set; }
        public string CurrentDestination { get; set; }
    }

    /// <summary>
    /// Handles the real-time status file (state.json) by observing BackupJob events.
    /// </summary>
    public class StateManager
    {
        private readonly string _stateFilePath;
        private readonly object _fileLock = new object();

        // We keep a historical sequence of state updates so the state.json
        // file reflects the live progression of jobs (appended entries).
        // This replaces the previous approach which only stored the latest
        // snapshot per job and caused the file to appear as "only the last"
        // state. Keeping a list makes it easy to show a live feed.
        private readonly List<JobStateData> _history;

        public StateManager()
        {
            _history = new List<JobStateData>();

            // Professional path in AppData to avoid permission issues
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directory = Path.Combine(appData, "EasySave");

            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            _stateFilePath = Path.Combine(directory, "state.json");

            // Load existing states from the file so we don't overwrite history on restart
            LoadExistingStates();
        }

        /// <summary>
        /// Reads the state.json file on startup to populate the dictionary.
        /// </summary>
        private void LoadExistingStates()
        {
            if (!File.Exists(_stateFilePath)) return;

            try
            {
                string jsonContent = File.ReadAllText(_stateFilePath);
                if (string.IsNullOrWhiteSpace(jsonContent)) return;

                // Try to deserialize as a list of JobStateData (historical format)
                var existingList = JsonSerializer.Deserialize<List<JobStateData>>(jsonContent);
                if (existingList != null && existingList.Count > 0)
                {
                    // If the file contains multiple entries (history), reuse them
                    _history.AddRange(existingList);
                    return;
                }

                // Back-compat: if file was previously a dictionary-like array (one entry per job),
                // we still load those entries into the history so the file continues to represent
                // previous states.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load previous state file: {ex.Message}");
            }
        }

        /// <summary>
        /// Triggered by a BackupJob via the Observer pattern.
        /// </summary>
        public void OnJobProgressUpdated(object sender, EventArgs e)
        {
            if (sender is BackupJob job)
            {
                UpdateStateFile(job);
            }
        }

        /// <summary>
        /// Serializes the array of all job states to the JSON file.
        /// </summary>
        private void UpdateStateFile(BackupJob job)
        {
            lock (_fileLock) // Protects both the history list and the File I/O
            {
                // 1. Create a new snapshot entry and append it to the history
                var entry = new JobStateData
                {
                    Name = job.Name,
                    LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = job.State.ToString(),
                    TotalFiles = job.TotalFiles,
                    TotalSize = job.TotalSize,
                    // Calculate based on size for better accuracy
                    Progress = job.TotalSize > 0 ? (int)((job.TotalSize - job.SizeRemaining) * 100 / job.TotalSize) : 0,
                    FilesRemaining = job.FilesRemaining,
                    SizeRemaining = job.SizeRemaining,
                    CurrentSource = job.CurrentSourceFile ?? string.Empty,
                    CurrentDestination = job.CurrentTargetFile ?? string.Empty
                };

                _history.Add(entry);

                // 2. Write the entire history to disk (so external viewers can follow a live feed)
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(_history, options);
                    File.WriteAllText(_stateFilePath, jsonString);
                }
                catch (IOException)
                {
                    // Fail silently or log to console
                }
            }
        }
    }
}