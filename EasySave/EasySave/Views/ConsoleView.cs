using System;
using System.Collections.Generic;
using System.Text;
using EasySave.ViewModel;
namespace EasySave.Views
{
    public class ConsoleView
    {
        private MainViewModel _viewModel;

        public ConsoleView(MainViewModel vm) 
        {
            _viewModel = vm;
        }

        public void DisplayMenu()
        {
            bool IsRunning= true;
            while (IsRunning)
            {
                // Displaying the main menu
                // Note: In a fully localized app, these hardcoded strings would be fetched 
                // from the ViewModel (e.g., _viewModel.GetString("MenuTitle"))
                Console.Clear();

                // Fetching translated strings dynamically
                Console.WriteLine("========================================");
                Console.WriteLine($"      {_viewModel.GetString("MenuTitle")}        ");
                Console.WriteLine("========================================");
                Console.WriteLine(_viewModel.GetString("MenuOption1"));
                Console.WriteLine(_viewModel.GetString("MenuOption2"));
                Console.WriteLine(_viewModel.GetString("MenuOption3"));
                Console.WriteLine(_viewModel.GetString("MenuOption4"));
                Console.WriteLine(_viewModel.GetString("MenuOption5"));
                Console.WriteLine(_viewModel.GetString("MenuOption6"));
                Console.WriteLine(_viewModel.GetString("MenuOption7"));

                Console.WriteLine("========================================");
                Console.Write(_viewModel.GetString("PromptSelectOption"));

                string userInput = ReadUserInput();

                switch (userInput)
                {
                    case "1":
                        ExecuteSingleJobMenu();
                        break;
                    case "2":
                        bool hasJobs = _viewModel.ExecuteAllJobsCommand();
                        if (hasJobs)
                        {
                            Console.WriteLine("All jobs execution triggered. Press any key to continue...");
                            Console.ReadKey();
                        }
                        break;
                    case "3":
                        ChangeLanguageMenu();
                        break;
                    case "4":
                        CreateJobMenu();
                        break;
                    case "5":
                        
                        DeleteJobMenu();
                        break;
                    case "6":
                        ModifyJobMenu();
                        break;
                    case "7":
                        IsRunning = false;
                        Console.WriteLine("Exiting EasySave. Goodbye!");
                        break;
                    default:
                        Console.WriteLine("Invalid option selected. Press any key to try again...");
                        Console.ReadKey();
                        break;
                }
            }
        }
        /// <summary>
        /// Reads and sanitizes the user input from the console.
        /// </summary>
        /// <returns>A trimmed string of the user's input.</returns>
        private string ReadUserInput()
        {
            string input = Console.ReadLine();
            return input != null ? input.Trim() : string.Empty;
        }

        /// <summary>
        /// Sub-menu routine to handle the execution of a single specific job.
        /// </summary>
        private void ExecuteSingleJobMenu()
        {
            // Call DisplayJobsMenu and check if there are actually jobs to display
            bool hasJobs = DisplayJobsMenu();

            // If there are no jobs, return immediately to the main while loop
            if (!hasJobs)
            {
                return;
            }

            Console.WriteLine();
            Console.Write(_viewModel.GetString("PromptEnterJobId"));
            string input = ReadUserInput();

            // Validate that the input is a number and falls within the allowed 1-5 range
            if (int.TryParse(input, out int jobId) && jobId >= 1 && jobId <= 5)
            {
                Console.WriteLine();
                _viewModel.ExecuteJobCommand(jobId);
                Console.WriteLine(_viewModel.GetString("MsgJobFinished"));
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine(_viewModel.GetString("ErrorInvalidJobId"));
            }

            Console.WriteLine(_viewModel.GetString("MsgPressAnyKey"));
            Console.ReadKey();
        }

        private void ChangeLanguageMenu()
        {
            Console.Write("Select language [EN/FR] / Sélectionnez la langue [EN/FR] : ");
            string input = ReadUserInput().ToUpper();

            if (input == "EN" || input == "FR")
            {
                _viewModel.ChangeLanguageCommand(input);
                // This message will now print in the newly selected language!
                Console.WriteLine(_viewModel.GetString("MsgLanguageChanged"));
            }
            Console.ReadKey();
        }
        /// <summary>
        /// Sub-menu routine to handle the creation of a new backup job.
        /// </summary>
        private void CreateJobMenu()
        {
            Console.WriteLine();
            Console.WriteLine("--- " + _viewModel.GetString("MenuCreateJobTitle") + " ---");

            // 1. Get Job Name
            Console.Write(_viewModel.GetString("PromptEnterJobName"));
            string name = ReadUserInput();

            // 2. Get Source Path
            Console.Write(_viewModel.GetString("PromptEnterSourcePath"));
            string source = ReadUserInput();

            // 3. Get Target Path
            Console.Write(_viewModel.GetString("PromptEnterTargetPath"));
            string target = ReadUserInput();

            // 4. Get Backup Type (1 for Full, 2 for Differential)
            Console.Write(_viewModel.GetString("PromptEnterJobType"));
            string typeInput = ReadUserInput();

            bool isDifferential = (typeInput == "2");

            // Basic validation to ensure fields are not empty
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            {
                Console.WriteLine();
                Console.WriteLine(_viewModel.GetString("ErrorEmptyFields"));
            }
            else
            {
                // Send data to ViewModel
                Console.WriteLine();
                bool success = _viewModel.CreateJob(name, source, target, isDifferential);

                if (success)
                {
                    Console.WriteLine(_viewModel.GetString("MsgJobCreatedSuccess"));
                }
                else
                {
                    Console.WriteLine(_viewModel.GetString("ErrorMaxJobsReached"));
                }
            }

            Console.WriteLine(_viewModel.GetString("MsgPressAnyKey"));
            Console.ReadKey();
        }

        /// <summary>
        /// Sub-menu routine to display all currently configured backup jobs.
        /// </summary>
        /// <returns>True if jobs exist and were displayed, False if the list is empty.</returns>
        private bool DisplayJobsMenu()
        {
            Console.WriteLine();
            Console.WriteLine("--- " + _viewModel.GetString("MenuDisplayJobsTitle") + " ---");

            var jobs = _viewModel.Jobs;

            if (jobs.Count == 0)
            {
                Console.WriteLine();
                Console.WriteLine(_viewModel.GetString("MsgNoJobsConfigured"));
                Console.ReadKey(); // Wait for user acknowledgment
                return false;      // Tell the calling method that there are no jobs
            }
            else
            {
                // Iterate through the jobs and display their details
                for (int i = 0; i < jobs.Count; i++)
                {
                    var job = jobs[i];

                    // i + 1 is used because the array starts at 0, but user IDs start at 1
                    Console.WriteLine($"[{i + 1}] " + _viewModel.GetString("LabelName") + $": {job.Name}");
                    Console.WriteLine($"    " + _viewModel.GetString("LabelState") + $": {job.State}");
                    Console.WriteLine($"    " + _viewModel.GetString("LabelSource") + $": {job.SourcePath}");
                    Console.WriteLine($"    " + _viewModel.GetString("LabelTarget") + $": {job.TargetPath}");
                    Console.WriteLine("----------------------------------------");
                }
            }

            return true; // Tell the calling method that jobs were successfully displayed
        }
        /// <summary>
        /// Sub-menu routine to handle deleting an existing job.
        /// </summary>
        private void DeleteJobMenu()
        {
            bool hasJobs = DisplayJobsMenu();
            if (!hasJobs) return;

            Console.WriteLine();
            Console.Write(_viewModel.GetString("PromptEnterJobIdToDelete"));
            string input = ReadUserInput();

            // Ensure the input is a valid integer and falls within the current number of jobs
            if (int.TryParse(input, out int jobId) && jobId >= 1 && jobId <= _viewModel.Jobs.Count)
            {
                bool success = _viewModel.DeleteJobCommand(jobId);

                if (success)
                    Console.WriteLine(_viewModel.GetString("MsgJobDeletedSuccess"));
                else
                    Console.WriteLine(_viewModel.GetString("ErrorInvalidJobId"));
            }
            else
            {
                Console.WriteLine(_viewModel.GetString("ErrorInvalidJobId"));
            }

            Console.WriteLine(_viewModel.GetString("MsgPressAnyKey"));
            Console.ReadKey();
        }

        /// <summary>
        /// Sub-menu routine to handle modifying an existing job.
        /// </summary>
        private void ModifyJobMenu()
        {
            bool hasJobs = DisplayJobsMenu();
            if (!hasJobs) return;

            Console.WriteLine();
            Console.Write(_viewModel.GetString("PromptEnterJobIdToModify"));
            string input = ReadUserInput();

            // Ensure the input is a valid integer and falls within the current number of jobs
            if (int.TryParse(input, out int jobId) && jobId >= 1 && jobId <= _viewModel.Jobs.Count)
            {
                // Collect the new parameters
                Console.Write(_viewModel.GetString("PromptEnterJobName"));
                string name = ReadUserInput();

                Console.Write(_viewModel.GetString("PromptEnterSourcePath"));
                string source = ReadUserInput();

                Console.Write(_viewModel.GetString("PromptEnterTargetPath"));
                string target = ReadUserInput();

                Console.Write(_viewModel.GetString("PromptEnterJobType"));
                string typeInput = ReadUserInput();
                bool isDifferential = (typeInput == "2");

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                {
                    Console.WriteLine(_viewModel.GetString("ErrorEmptyFields"));
                }
                else
                {
                    bool success = _viewModel.ModifyJobCommand(jobId, name, source, target, isDifferential);

                    if (success)
                        Console.WriteLine(_viewModel.GetString("MsgJobModifiedSuccess"));
                    else
                        Console.WriteLine(_viewModel.GetString("ErrorInvalidJobId"));
                }
            }
            else
            {
                Console.WriteLine(_viewModel.GetString("ErrorInvalidJobId"));
            }

            Console.WriteLine(_viewModel.GetString("MsgPressAnyKey"));
            Console.ReadKey();
        }
    }

}
