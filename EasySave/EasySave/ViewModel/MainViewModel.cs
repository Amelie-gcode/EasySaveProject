using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.ViewModel
{
    public class MainViewModel
    {
        // Reference to the core business logic manager
        private readonly BackupManager _backupManager;

        /// <summary>
        /// Gets the list of currently configured backup jobs.
        /// </summary>
        public List<BackupJob> Jobs => _backupManager.GetJobs();
        /// <summary>
        /// Constructor initializes the necessary managers.
        /// </summary>
        public MainViewModel()
        {
            _backupManager = new BackupManager();
        }
        // ==========================================
        // PROPERTIES FOR THE VIEW
        // ==========================================

        /// <summary>
        /// Exposes the current language code (e.g., "EN" or "FR") to the View.
        /// </summary>
        public string CurrentLanguage => LocalizationManager.Instance.CurrentLanguage;
        /// <summary>
        /// Retrieves translated strings from the LocalizationManager for the UI.
        /// </summary>
        /// <param name="key">The JSON key for the text string.</param>
        /// <returns>The translated string.</returns>
        public string GetString(string key)
        {
            return LocalizationManager.Instance.GetString(key);
        }

        // ==========================================
        // COMMANDS (TRIGGERED BY THE VIEW)
        // ==========================================

        /// <summary>
        /// Executes a single backup job based on its ID.
        /// </summary>
        /// <param name="id">The ID of the job to execute (1-5).</param>
        public void ExecuteJobCommand(int id)
        {
            Console.WriteLine(GetString("MsgStartingJob") + " " + id);
            _backupManager.ExecuteJob(id);
        }

        /// <summary>
        /// Executes all configured backup jobs sequentially.
        /// </summary>
        public bool ExecuteAllJobsCommand()
        {
            if (Jobs.Count == 0)
            {
                Console.WriteLine();
                Console.WriteLine(GetString("MsgNoJobsConfigured"));
                Console.ReadKey(); // Wait for user acknowledgment
                return false;
            }
            else
            {
                Console.WriteLine(GetString("MsgStartingAllJobs"));
                _backupManager.ExecuteAll();
                return true;
            }
            
        }

        /// <summary>
        /// Changes the application's language state.
        /// </summary>
        /// <param name="langCode">The language code ("EN" or "FR").</param>
        public void ChangeLanguageCommand(string langCode)
        {
            LocalizationManager.Instance.SetLanguage(langCode);
        }

        // ==========================================
        // COMMAND LINE INTERFACE (CLI) LOGIC
        // ==========================================

        /// <summary>
        /// Parses and executes jobs passed directly via the terminal (e.g., "EasySave.exe 1-3" or "1;3").
        /// </summary>
        /// <param name="arguments">The string argument passed from the console.</param>
        public void ExecuteJobsFromCommandLine(string arguments)
        {
            try
            {
                // Handle continuous range (e.g., "1-3")
                if (arguments.Contains("-"))
                {
                    string[] range = arguments.Split('-');
                    if (range.Length == 2 &&
                        int.TryParse(range[0], out int startId) &&
                        int.TryParse(range[1], out int endId))
                    {
                        for (int i = startId; i <= endId; i++)
                        {
                            ExecuteJobCommand(i);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid range format. Use '1-3'.");
                    }
                }
                // Handle specific isolated jobs (e.g., "1;3")
                else if (arguments.Contains(";"))
                {
                    string[] specificJobs = arguments.Split(';');
                    foreach (string jobString in specificJobs)
                    {
                        if (int.TryParse(jobString, out int jobId))
                        {
                            ExecuteJobCommand(jobId);
                        }
                    }
                }
                // Handle a single specific job (e.g., "2")
                else if (int.TryParse(arguments, out int singleJobId))
                {
                    ExecuteJobCommand(singleJobId);
                }
                else
                {
                    Console.WriteLine("Invalid argument format provided.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during CLI execution: {ex.Message}");
            }
        }

        public bool CreateJob(string name,string source,string target,bool type)
        {
            return _backupManager.CreateJob(name, source, target, type);
        }

        public void GetJobs()
        {
            List<BackupJob> jobs= _backupManager.GetJobs();

        }
    }
}
