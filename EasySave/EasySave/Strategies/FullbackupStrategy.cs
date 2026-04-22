using EasyLog;
using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace EasySave.Strategies
{
    /// Copies every file from source to target, every time.
    /// Does not check if files already exist or are unchanged.
    public class FullBackupStrategy : IBackupStrategy
    {
        public void ExecuteBackup(string sourceDir, string targetDir, BackupJob jobContext)
        {
            // Retrieve all files in the source directory, including subdirectories
            string[] files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

            foreach (string sourceFile in files)
            {
                // Calculate the corresponding destination path
                string relativePath = sourceFile.Substring(sourceDir.Length + 1);
                string targetFile = Path.Combine(targetDir, relativePath);

                // Ensure the target folder structure exists
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                long fileSize = 0;
                long transferTimeMs = -1; // Default to -1 in case of error, per specs
                Stopwatch stopwatch = new Stopwatch();

                try
                {
                    FileInfo fileInfo = new FileInfo(sourceFile);
                    fileSize = fileInfo.Length;

                    // Update job context with current file being processed
                    jobContext.CurrentSourceFile = sourceFile;
                    jobContext.CurrentTargetFile = targetFile;

                    stopwatch.Start();

                    // Copy the file, true means overwrite if it exists
                    File.Copy(sourceFile, targetFile, true);

                    stopwatch.Stop();
                    transferTimeMs = stopwatch.ElapsedMilliseconds;
                }
                catch (Exception ex)
                {
                    // Log the error to console, but let the loop continue to the next file
                    Console.WriteLine($"Error copying {sourceFile}: {ex.Message}");
                }
                finally
                {
                    // Update remaining counters
                    jobContext.FilesRemaining--;
                    jobContext.SizeRemaining -= fileSize;

                    // Write to the Daily Log file via the Singleton DLL
                    EasyLogger.Instance.WriteLog(new LogEntry
                    {
                        BackupName = jobContext.Name,
                        SourceFilePath = sourceFile,
                        TargetFilePath = targetFile,
                        FileSize = fileSize,
                        TransferTimeMs = transferTimeMs
                    });

                    // Notify StateManager to update state.json
                    jobContext.NotifyProgress();
                }
            }
        }
    }
}

