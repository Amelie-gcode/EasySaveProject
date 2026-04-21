using EasySave.ViewModel;

using EasySave.Views;
using System;

namespace EasySave
{
    class Program
    {
        // 'args' captures anything typed after "EasySave.exe" in the terminal
        static void Main(string[] args)
        {
            // 1. We instantiate the ViewModel first, as both the CLI and the Menu need it
            MainViewModel viewModel = new MainViewModel();

            // 2. THE FORK IN THE ROAD: Check if arguments exist
            if (args.Length > 0)
            {
                // SCENARIO A: The user typed something like "EasySave.exe 1-3"
                // We pass that argument directly to your method!
                viewModel.ExecuteJobsFromCommandLine(args[0]);
            }
            else
            {
                // SCENARIO B: The user just double-clicked the .exe or typed "EasySave.exe"
                // Since there are no arguments, we launch the visual menu instead.
                ConsoleView view = new ConsoleView(viewModel);
                view.DisplayMenu();
            }
        }
    }
}