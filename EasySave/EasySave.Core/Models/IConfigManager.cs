using System.Collections.Generic;

namespace EasySave.Models
{
    /// <summary>
    /// Defines the contract for saving and loading backup job configurations.
    /// </summary>
    public interface IConfigManager
    {
        /// <summary>
        /// Saves the list of currently configured jobs.
        /// </summary>
        void SaveJobs(List<BackupJob> jobs);

        /// <summary>
        /// Loads the saved job configurations.
        /// </summary>
        List<JobSaveData> LoadJobs();
    }
}