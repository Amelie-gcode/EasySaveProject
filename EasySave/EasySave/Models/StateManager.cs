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

        // Dictionary to track the latest state of all jobs by their Name
        private readonly Dictionary<string, JobStateData> _jobStates;

        public StateManager()
        {
            _jobStates = new Dictionary<string, JobStateData>();

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
            if (File.Exists(_stateFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(_stateFilePath);
                    if (!string.IsNullOrWhiteSpace(jsonContent))
                    {
                        // Deserialize the JSON array into a list
                        var existingStates = JsonSerializer.Deserialize<List<JobStateData>>(jsonContent);

                        if (existingStates != null)
                        {
                            foreach (var state in existingStates)
                            {
                                _jobStates[state.Name] = state;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load previous state file: {ex.Message}");
                }
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
            lock (_fileLock)
            {
                // Create or update the specific object for this job
                var stateData = new JobStateData
                {
                    Name = job.Name,
                    LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = job.State.ToString(),
                    TotalFiles = job.TotalFiles,
                    TotalSize = job.TotalSize,
                    Progress = job.TotalFiles > 0 ? (100 - (job.FilesRemaining * 100 / job.TotalFiles)) : 0,
                    FilesRemaining = job.FilesRemaining,
                    SizeRemaining = job.SizeRemaining,
                    CurrentSource = job.CurrentSourceFile ?? string.Empty,
                    CurrentDestination = job.CurrentTargetFile ?? string.Empty
                };

                // Add or replace the job in our in-memory dictionary
                _jobStates[job.Name] = stateData;

                // We serialize the VALUES of the dictionary, which creates a clean JSON array [ {..}, {..} ]
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(_jobStates.Values.ToList(), options);

                try
                {
                    File.WriteAllText(_stateFilePath, jsonString);
                }
                catch (IOException ex)
                {
                    // Log error but don't crash the backup process
                    Console.WriteLine($"Warning: Could not update state file: {ex.Message}");
                }
            }
        }
    }
}