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
                // ✅ CHECK #2 — before each file, stop if business software just opened
                if (jobContext.Settings != null &&
                    jobContext.BusinessService.IsBusinessSoftwareRunning(
                        jobContext.Settings.BusinessSoftwareName))
                {
                    string detected = jobContext.BusinessService.GetDetectedSoftwareName(
                        jobContext.Settings.BusinessSoftwareName);

                    jobContext.State = JobState.Error;
                    jobContext.CurrentSourceFile = $"BLOCKED by: {detected}";

                    EasyLogger.Instance.WriteLog(new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        BackupName = jobContext.Name,
                        SourceFilePath = $"BLOCKED by: {detected}",
                        TargetFilePath = string.Empty,
                        FileSize = 0,
                        TransferTimeMs = -1
                    });

                    jobContext.NotifyProgress();
                    return; // previous file already fully copied, next ones are skipped
                }

                // 1. Prepare paths
                string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                string targetFile = Path.Combine(targetDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                long fileSize = new FileInfo(sourceFile).Length;
                Stopwatch stopwatch = Stopwatch.StartNew();

                try
                {
                    // 2. Update Context for "Active" display
                    jobContext.CurrentSourceFile = sourceFile;
                    jobContext.CurrentTargetFile = targetFile;

                    // Notify state that we are starting this specific file
                    jobContext.NotifyProgress();
                    if (jobContext.Encryption.ShouldEncrypt(sourceFile))
                    {
                        jobContext.Encryption.Encrypt(sourceFile, targetFile, jobContext.EncryptionKey);
                    }
                    else
                    {
                        File.Copy(sourceFile, targetFile, true);
                    }
                    stopwatch.Stop();

                    // 3. Update Progress Counters immediately after success
                    jobContext.FilesRemaining--;
                    jobContext.SizeRemaining -= fileSize;
                    // The JobContext.NotifyProgress() logic should recalculate 
                    // Progress = (Total - Remaining) / Total
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    // 4. Final log and State Update
                    EasyLogger.Instance.WriteLog(new LogEntry
                    {
                        BackupName = jobContext.Name,
                        SourceFilePath = sourceFile,
                        TargetFilePath = targetFile,
                        FileSize = fileSize,
                        TransferTimeMs = stopwatch.ElapsedMilliseconds
                    });

                    // Final notification for this file iteration
                    jobContext.NotifyProgress();
                }
            }
                
        }
    }
}

