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
                jobContext.CheckPauseAndCancellation();

                // 1. Prepare paths
                string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                string targetFile = Path.Combine(targetDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                long fileSize = new FileInfo(sourceFile).Length;
                Stopwatch stopwatch = Stopwatch.StartNew();
                long encryptionTime = 0;

                try
                {
                    // 2. Update Context for "Active" display
                    jobContext.CurrentSourceFile = sourceFile;
                    jobContext.CurrentTargetFile = targetFile;

                    // Notify state that we are starting this specific file
                    jobContext.NotifyProgress();
                    if (jobContext.Encryption.ShouldEncrypt(sourceFile))
                    {
                        CopyFileWithControl(sourceFile, targetFile, jobContext);
                        encryptionTime = jobContext.Encryption.Encrypt(targetFile, jobContext.EncryptionKey);
                    }
                    else
                    {
                        CopyFileWithControl(sourceFile, targetFile, jobContext);

                    
                    }
                    stopwatch.Stop();

                    // 3. Update Progress Counters immediately after success
                    jobContext.FilesRemaining--;
                    // SizeRemaining is updated during the copy to keep progress smooth.
                }
                catch (OperationCanceledException)
                {
                    throw;
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
                        TransferTimeMs = stopwatch.ElapsedMilliseconds,
                        EncryptionTimeMs = encryptionTime

                    });

                    // Final notification for this file iteration
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

