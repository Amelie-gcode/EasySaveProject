using EasySave.ViewModel;

using EasySave.Views;
using System;
using System.Windows;

namespace EasySave
{
    class Program
    {
        // 'args' captures anything typed after "EasySave.exe" in the terminal
        [STAThread]
        static void Main(string[] args)
        {
            // Flags:
            //  - --gui (or --mode gui) => launch WPF UI
            //  - --console (or --mode console) => launch console mode
            ParseArgs(args, out string? mode, out string? positionalArg);

            if (string.Equals(mode, "gui", StringComparison.OrdinalIgnoreCase))
            {
                RunGui();
                return;
            }

            if (string.Equals(mode, "console", StringComparison.OrdinalIgnoreCase))
            {
                RunConsole(positionalArg);
                return;
            }

            // No explicit mode flag:
            // - if a positional arg exists => console CLI
            // - else => interactive choice
            if (!string.IsNullOrWhiteSpace(positionalArg))
            {
                RunConsole(positionalArg);
                return;
            }

            Console.WriteLine("Select mode: [1] Console  [2] GUI");
            var choice = Console.ReadLine()?.Trim();
            if (choice == "2")
                RunGui();
            else
                RunConsole(null);
        }

        private static void ParseArgs(string[] args, out string? mode, out string? positionalArg)
        {
            mode = null;
            positionalArg = null;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.IsNullOrWhiteSpace(arg)) continue;

                if (string.Equals(arg, "--gui", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-g", StringComparison.OrdinalIgnoreCase))
                {
                    mode = "gui";
                    continue;
                }

                if (string.Equals(arg, "--console", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-c", StringComparison.OrdinalIgnoreCase))
                {
                    mode = "console";
                    continue;
                }

                if (string.Equals(arg, "--mode", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    mode = args[i + 1];
                    i++;
                    continue;
                }

                // First non-flag argument is treated as the job selector (e.g. "1-3", "1;3", "2").
                if (!arg.StartsWith("-", StringComparison.Ordinal) && positionalArg == null)
                {
                    positionalArg = arg;
                }
            }
        }

        private static void RunConsole(string? positionalArg)
        {
            MainViewModel viewModel = new MainViewModel();

            if (!string.IsNullOrWhiteSpace(positionalArg))
            {
                viewModel.ExecuteJobsFromCommandLine(positionalArg);
                return;
            }

            ConsoleView view = new ConsoleView(viewModel);
            view.DisplayMenu();
        }

        private static void RunGui()
        {
            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;

            // MainWindow is defined in the EasySave.WPF project.
            // We instantiate the GUI ViewModel here to keep the View "dumb".
            var guiViewModel = new EasySave.WPF.MainViewModel();
            var window = new EasySave.WPF.MainWindow(guiViewModel);
            app.Run(window);
        }
    }
}