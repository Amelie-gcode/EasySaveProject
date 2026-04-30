using System;
using EasySave.Strategies;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave.Models
{
    /// <summary>
    /// Concrete implementation of IConfigManager that saves data to a JSON file in AppData.
    /// </summary>
    public class ConfigManager : IConfigManager
    {
        private readonly string _configFilePath;

        public ConfigManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directory = Path.Combine(appData, "EasySave");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _configFilePath = Path.Combine(directory, "jobs.json");
        }

        public void SaveJobs(List<BackupJob> jobs)
        {
            var dataToSave = new List<JobSaveData>();

            foreach (var job in jobs)
            {
                dataToSave.Add(new JobSaveData
                {
                    Name = job.Name,
                    Source = job.SourcePath,
                    Target = job.TargetPath,
                    // Determine type based on the active strategy
                    IsDifferential = job.GetStrategy() is DifferentialBackupStrategy
                });
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_configFilePath, JsonSerializer.Serialize(dataToSave, options));
        }

        public List<JobSaveData> LoadJobs()
        {
            if (!File.Exists(_configFilePath)) return new List<JobSaveData>();

            try
            {
                string json = File.ReadAllText(_configFilePath);
                return JsonSerializer.Deserialize<List<JobSaveData>>(json) ?? new List<JobSaveData>();
            }
            catch
            {
                return new List<JobSaveData>();
            }
        }
    }
}