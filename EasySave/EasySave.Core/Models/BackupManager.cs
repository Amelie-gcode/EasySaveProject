using EasySave.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Models
{
    public class BackupManager
    {
        // Internal collection of jobs, limited to 5 as per specifications
        private readonly List<BackupJob> _jobs;
        private readonly StateManager _stateManager;
        private readonly IConfigManager _configManager;
        private readonly SettingsManager _settingsManager;
        private readonly EncryptionService _encryptionService;

        public BackupManager(IConfigManager configManager = null)
        {
            _jobs = new List<BackupJob>();

            // If no specific manager is provided, fall back to our default JSON implementation
            _configManager = configManager ?? new ConfigManager();

            _stateManager = new StateManager();

            _settingsManager = new SettingsManager();

            _encryptionService = new EncryptionService(
                _settingsManager.LoadSettings().CryptoSoftPath,
                _settingsManager.LoadSettings().EncryptedExtensions.ToArray());
            // Load saved jobs on startup
            LoadFromConfig();
        }

        private void LoadFromConfig()
        {
            var savedJobs = _configManager.LoadJobs();
            foreach (var data in savedJobs)
            {
                IBackupStrategy strategy = data.IsDifferential
                    ? new DifferentialBackupStrategy()
                    : new FullBackupStrategy();

                _jobs.Add(new BackupJob(data.Name, data.Source, data.Target, strategy));
            }
        }
        /// <summary>
        /// Orchestrates the execution of a specific job by its ID.
        /// </summary>
        /// <param name="id">The index of the job (1-5).</param>
        public void ExecuteJob(int id)
        {
            
            // IDs are 1-based for the user, but 0-based for the list index
            int index = id - 1;

            if (index < 0 || index >= _jobs.Count)
            {
                Console.WriteLine($"Error: Job with ID {id} does not exist.");
                return;
            }

            BackupJob job = _jobs[index];

            
            // 1. Refresh settings (in case the user changed the key or extensions recently)
            var settings = _settingsManager.LoadSettings();

            // 2. "Inject" the dependencies into the Job
            // We pass the ALREADY DECRYPTED key (handled by LoadSettings)
            job.Encryption = _encryptionService;
            job.EncryptionKey = settings.EncryptionKey;
            job.Settings = settings;

            if (job.State == JobState.Active || job.State == JobState.Paused)
            {
                Console.WriteLine($"Error: Job '{job.Name}' is already running.");
                return;
            }

            try
            {
                // WIRING THE OBSERVER:
                // Subscribe the StateManager to the job's progress event before execution
                job.ProgressUpdated += _stateManager.OnJobProgressUpdated;

                // Execute the business logic (which uses the Strategy pattern internally)
                job.Execute();
            }
            finally
            {
                // UNWIRING THE OBSERVER:
                // Always unsubscribe to prevent memory leaks or duplicate logging
                job.ProgressUpdated -= _stateManager.OnJobProgressUpdated;
            }
        }

        /// <summary>
        /// Executes all configured backup jobs one after another.
        /// </summary>
        public void ExecuteAll()
        {
            for (int i = 1; i <= _jobs.Count; i++)
            {
                ExecuteJob(i);
            }
        }

        /// <summary>
        /// Creates and adds a new backup job to the list.
        /// </summary>
        public bool CreateJob(string name, string source, string target, bool isDifferential)
        {
            if (_jobs.Count >= 5)
            {
                return false; // Limit reached
            }

            // Assign the strategy based on the type
            IBackupStrategy strategy = isDifferential
                ? new DifferentialBackupStrategy()
                : new FullBackupStrategy();

            BackupJob newJob = new BackupJob(name, source, target, strategy);
            _jobs.Add(newJob);

            // Immediately update state.json to show the new inactive job
            _stateManager.OnJobProgressUpdated(newJob, EventArgs.Empty);
            _configManager.SaveJobs(_jobs);
            return true;
        }

        /// <summary>
        /// Deletes a backup job at the specified index.
        /// </summary>
        /// <param name="index">The 0-based index of the job.</param>
        /// <returns>True if deletion was successful, False otherwise.</returns>
        public bool DeleteJob(int index)
        {
            // Validate that the index exists in the list
            if (index < 0 || index >= _jobs.Count)
            {
                return false;
            }

            _jobs.RemoveAt(index);

            // Automatically save the updated list to the JSON file
            _configManager.SaveJobs(_jobs);

            return true;
        }

        /// <summary>
        /// Modifies an existing backup job at the specified index.
        /// </summary>
        /// <param name="index">The 0-based index of the job.</param>
        /// <returns>True if modification was successful, False otherwise.</returns>
        public bool ModifyJob(int index, string newName, string newSource, string newTarget, bool isDifferential)
        {
            // Validate that the index exists in the list
            if (index < 0 || index >= _jobs.Count)
            {
                return false;
            }

            // Determine the new strategy
            IBackupStrategy strategy = isDifferential
                ? new DifferentialBackupStrategy()
                : new FullBackupStrategy();

            // Replace the old job with the newly configured job
            _jobs[index] = new BackupJob(newName, newSource, newTarget, strategy);

            // Automatically save the updated list to the JSON file
            _configManager.SaveJobs(_jobs);

            return true;
        }

        /// <summary>
        /// Internal helper to simulate or load job configurations.
        /// </summary>

        // Getter for the ViewModel to display job lists in the UI
        public List<BackupJob> GetJobs() => _jobs;
    }
}