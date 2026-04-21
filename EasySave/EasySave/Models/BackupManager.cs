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

        public BackupManager()
        {
            _jobs = new List<BackupJob>();
            _stateManager = new StateManager();

            // In a real scenario, you might load existing jobs from a config file here
            LoadConfiguredJobs();
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

            if (job.State == JobState.Active)
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

            return true;
        }

        /// <summary>
        /// Internal helper to simulate or load job configurations.
        /// </summary>
        private void LoadConfiguredJobs()
        {
            // Example of pre-loading a job for testing purposes
            // In the final version, this would read from a JSON config file.
        }

        // Getter for the ViewModel to display job lists in the UI
        public List<BackupJob> GetJobs() => _jobs;
    }
}