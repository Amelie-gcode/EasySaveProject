using EasyLog;
using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace EasySave.Strategies
{
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        public void ExecuteBackup(string sourceDir, string targetDir, BackupJob jobContext)
        {
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
                jobContext.CheckPauseAndCancellation();

                string relativePath = sourceFile.Substring(sourceDir.Length + 1);
                string targetFile = Path.Combine(targetDir, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                FileInfo sourceInfo = new FileInfo(sourceFile);
                FileInfo targetInfo = new FileInfo(targetFile);

                long fileSize = sourceInfo.Length;

                // Differential Logic: Check if we actually need to copy this file
                bool shouldCopy = false;

                if (!targetInfo.Exists)
                {
                    // File is brand new
                    shouldCopy = true;
                }
                else if (sourceInfo.LastWriteTime > targetInfo.LastWriteTime)
                {
                    // Source file has been modified more recently than the target
                    shouldCopy = true;
                }

                if (shouldCopy)
                {
                    long transferTimeMs = -1;
                    Stopwatch stopwatch = new Stopwatch();
                    long encryptionTime = 0;
                    try
                    {
                        jobContext.CurrentSourceFile = sourceFile;
                        jobContext.CurrentTargetFile = targetFile;

                        stopwatch.Start();
                        
                        if (jobContext.Encryption.ShouldEncrypt(sourceFile))
                        {
                            CopyFileWithControl(sourceFile, targetFile, jobContext);
                            encryptionTime =jobContext.Encryption.Encrypt(targetFile, jobContext.EncryptionKey);
                        }
                        else
                        {
                            CopyFileWithControl(sourceFile, targetFile, jobContext);

                        }
                        stopwatch.Stop();
                        jobContext.NotifyProgress();
                        transferTimeMs = stopwatch.ElapsedMilliseconds;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error copying {sourceFile}: {ex.Message}");
                    }
                    finally
                    {
                        jobContext.FilesRemaining--;
                        // SizeRemaining is updated during the copy to keep progress smooth.
                        // Log the file transfer details to EasyLogger
                        EasyLogger.Instance.WriteLog(new LogEntry
                        {
                            BackupName = jobContext.Name,
                            SourceFilePath = sourceFile,
                            TargetFilePath = targetFile,
                            FileSize = fileSize,
                            TransferTimeMs = transferTimeMs,
                            EncryptionTimeMs = encryptionTime

                        });
                        jobContext.NotifyProgress();
                    }
                }
                else
                {
                    // If we skip the file, we still need to decrease the remaining counters 
                    // so the progress bar / state.json updates correctly
                    jobContext.FilesRemaining--;
                    jobContext.SizeRemaining -= fileSize;
                    jobContext.NotifyProgress();
                }
            }
        }

        private static void CopyFileWithControl(string sourceFile, string targetFile, BackupJob jobContext)
        {
            const int BufferSize = 1024 * 1024; // 1MB

            using var source = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var target = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[BufferSize];
            int read;

            var notify = Stopwatch.StartNew();
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                jobContext.CheckPauseAndCancellation();
                target.Write(buffer, 0, read);
                jobContext.SizeRemaining -= read;

                if (notify.ElapsedMilliseconds >= 100)
                {
                    jobContext.NotifyProgress();
                    notify.Restart();
                }
            }
        }
    }
}
