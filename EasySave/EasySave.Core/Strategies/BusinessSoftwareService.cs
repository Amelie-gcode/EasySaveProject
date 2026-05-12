using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EasySave.Core.Strategies
{
        /// <summary>
        /// Detects if any business software defined by the user is currently running.
        /// 
        /// Requirements:
        /// - If detected BEFORE a job: prevent the backup from launching
        /// - If detected DURING a job: finish the current file then stop
        /// - The user defines the software list in Settings
        /// - The shutdown must be recorded in the log file
        /// </summary>
        public class BusinessSoftwareService
        {
            /// <summary>
            /// Returns true if at least one software from the list is running.
            /// Used to decide whether to block or continue a backup.
            /// </summary>
            public bool IsBusinessSoftwareRunning(List<string> softwareList)
            {
                if (softwareList == null || softwareList.Count == 0)
                    return false;

                foreach (string software in softwareList)
                {
                    // Remove .exe if user typed it (e.g. "calc.exe" becomes "calc")
                    string processName = software
                        .Replace(".exe", "")
                        .Trim()
                        .ToLower();

                    // Check Windows running processes
                    Process[] running = Process.GetProcessesByName(processName);

                    if (running.Length > 0)
                        return true;
                }

                return false;
            }

            /// <summary>
            /// Returns the name of the first detected software.
            /// Used to write in the log file which software caused the shutdown.
            /// Example: "Blocked by: Calculator"
            /// </summary>
            public string GetDetectedSoftwareName(List<string> softwareList)
            {
                if (softwareList == null) return string.Empty;

                foreach (string software in softwareList)
                {
                    string processName = software
                        .Replace(".exe", "")
                        .Trim()
                        .ToLower();

                    Process[] running = Process.GetProcessesByName(processName);

                    if (running.Length > 0)
                        return software;
                }

                return string.Empty;
            }
        }
}

