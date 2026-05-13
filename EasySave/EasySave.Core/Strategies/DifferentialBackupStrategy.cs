using EasyLog;
using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EasySave.Strategies
{
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        public void ExecuteBackup(string sourceDir, string targetDir,
                                  BackupJob jobContext)
        {
            string[] files = Directory.GetFiles(
                sourceDir, "*.*", SearchOption.AllDirectories);

            foreach (string sourceFile in files)
            {
                // Stop if business software just opened
                if (jobContext.Settings != null &&
                    jobContext.BusinessService.IsBusinessSoftwareRunning(
                        jobContext.Settings.BusinessSoftwareName))
                {
                    string detected = jobContext.BusinessService
                        .GetDetectedSoftwareName(
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
                    return;
                }

                jobContext.CheckPauseAndCancellation();

                string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                string targetFile = Path.Combine(targetDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                FileInfo sourceInfo = new FileInfo(sourceFile);
                FileInfo targetInfo = new FileInfo(targetFile);
                long fileSize = sourceInfo.Length;

                // Differential check — only copy if new or modified
                bool shouldCopy = !targetInfo.Exists ||
                                  sourceInfo.LastWriteTime > targetInfo.LastWriteTime;

                if (!shouldCopy)
                {
                    // File unchanged — skip but update counters correctly
                    jobContext.FilesRemaining--;
                    jobContext.SizeRemaining -= fileSize;
                    jobContext.NotifyProgress();
                    continue;
                }

                // Show current file BEFORE copy starts
                jobContext.CurrentSourceFile = sourceFile;
                jobContext.CurrentTargetFile = targetFile;
                jobContext.NotifyProgress();

                long encryptionTime = 0;
                long transferTimeMs = -1;
                Stopwatch stopwatch = new Stopwatch();

                try
                {
                    stopwatch.Start();

                    if (jobContext.Encryption.ShouldEncrypt(sourceFile))
                    {
                        CopyFileWithControl(sourceFile, targetFile, jobContext);
                        encryptionTime = jobContext.Encryption.Encrypt(
                            targetFile, jobContext.EncryptionKey);
                    }
                    else
                    {
                        CopyFileWithControl(sourceFile, targetFile, jobContext);
                    }

                    stopwatch.Stop();
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
                    // Always runs — even if file had an error
                    jobContext.FilesRemaining--;

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
        }

        private static void CopyFileWithControl(string sourceFile,
            string targetFile, BackupJob jobContext)
        {
            const int BufferSize = 1024 * 1024; // 1 MB

            using var source = new FileStream(sourceFile,
                FileMode.Open, FileAccess.Read, FileShare.Read);
            using var target = new FileStream(targetFile,
                FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[BufferSize];
            int read;
            var notify = Stopwatch.StartNew();

            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                jobContext.CheckPauseAndCancellation();
                target.Write(buffer, 0, read);

                // Decrement size in real time during copy
                jobContext.SizeRemaining -= read;

                // Notify UI every 100ms
                if (notify.ElapsedMilliseconds >= 100)
                {
                    jobContext.NotifyProgress();
                    notify.Restart();
                }
            }
        }

        public Task ExecuteBackupAsync(string source, string target, BackupJob job)
        {
            throw new NotImplementedException();
        }
    }
}