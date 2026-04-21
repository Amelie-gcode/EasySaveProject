using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace EasySave.Models
{
    /// <summary>
    /// Handles the real-time status file (state.json) by observing BackupJob events.
    /// </summary>
    public class StateManager
    {
        private readonly string _stateFilePath;
        private readonly object _fileLock = new object();

        public StateManager()
        {
            // Professional path in AppData to avoid permission issues
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directory = Path.Combine(appData, "EasySave");

            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            _stateFilePath = Path.Combine(directory, "state.json");
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
        /// Serializes the current job state to the JSON file.
        /// </summary>
        private void UpdateStateFile(BackupJob job)
        {
            lock (_fileLock)
            {
                // Create a structured object for JSON as per specs
                var stateData = new
                {
                    Name = job.Name,
                    LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = job.State.ToString(),
                    TotalFiles = job.TotalFiles,
                    TotalSize = job.TotalSize,
                    Progress = job.TotalFiles > 0 ? (100 - (job.FilesRemaining * 100 / job.TotalFiles)) : 0,
                    FilesRemaining = job.FilesRemaining,
                    SizeRemaining = job.SizeRemaining,
                    CurrentSource = job.CurrentSourceFile,
                    CurrentDestination = job.CurrentTargetFile
                };

                // Indented formatting for "quick reading via Notepad" as requested
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(stateData, options);

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