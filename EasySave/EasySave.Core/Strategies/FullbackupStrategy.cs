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
        public async Task ExecuteBackupAsync(string sourceDir, string targetDir, BackupJob jobContext)
        {
            // Offload file discovery to a background task
            string[] files = await Task.Run(() => Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories));

            foreach (string sourceFile in files)
            {
                string extension = Path.GetExtension(sourceFile);
                bool isPriority = jobContext.Settings.PriorityExtensions.Contains(extension);

                // If this specific file is NOT a priority, check if others are waiting globally
                if (!isPriority)
                {
                    // We wait while ANY job in the manager has priority files pending
                    bool waited = false;
                    // Check the GLOBAL status across all jobs
                    while (BackupJob.OthersHavePriority(jobContext.LocalPriorityFilesCount))
                    {
                        waited = true;
                        jobContext.State = JobState.Paused; // Visual feedback: "I'm waiting"
                        jobContext.NotifyProgress();

                        jobContext.CheckPauseAndCancellation();
                        await Task.Delay(1000);
                    }

                    // CRITICAL FIX: If we were waiting, set state back to Active 
                    // so the copy logic can proceed.
                    if (waited)
                    {
                        jobContext.State = JobState.Active;
                        jobContext.NotifyProgress();
                    }
                }
                // Requirement: "Temporary pause if business software is detected"
                // We use a loop to wait while the business software is open
                bool wasWaitingForBusinessSoftware = false;
                while (jobContext.Settings != null && jobContext.BusinessService.IsBusinessSoftwareRunning(jobContext.Settings.BusinessSoftwareName))
                {
                    wasWaitingForBusinessSoftware = true;
                    jobContext.State = JobState.Paused; // Visual feedback for user
                    jobContext.NotifyProgress();
                    EasyLogger.Instance.WriteLog(new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        BackupName = jobContext.Name,
                        SourceFilePath = $"BLOCKED by: {jobContext.BusinessService.GetDetectedSoftwareName(
                        jobContext.Settings.BusinessSoftwareName)}",
                        TargetFilePath = string.Empty,
                        FileSize = 0,
                        TransferTimeMs = -1
                    });
                    await Task.Delay(1000); // Poll every second
                }

                // CRITICAL FIX: Restore state to Active after business software is closed
                if (wasWaitingForBusinessSoftware)
                {
                    jobContext.State = JobState.Active;
                    jobContext.NotifyProgress();
                }

                jobContext.CheckPauseAndCancellation();

                // 1. Prepare paths
                string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                string targetFile = Path.Combine(targetDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                long fileSize = new FileInfo(sourceFile).Length;
                Stopwatch stopwatch = Stopwatch.StartNew();
                long encryptionTime = 0;

                //check file size against threshold for parallelism
                long fileSizeInBytes = new FileInfo(sourceFile).Length;
                long thresholdInBytes = jobContext.Settings.MaxParallelSize * 1024;

                bool isLargeFile = fileSizeInBytes > thresholdInBytes;
                bool semaphoreAcquired = false;

                try
                {
                    if (isLargeFile)
                    {
                        // If it's a large file, wait here until the semaphore is free.
                        // Small files in other jobs will NOT be blocked by this.
                        await BackupJob.LargeFileSemaphore.WaitAsync();
                        semaphoreAcquired = true;
                    }
                    // 2. Update Context for "Active" display
                    jobContext.CurrentSourceFile = sourceFile;
                    jobContext.CurrentTargetFile = targetFile;

                    // Notify state that we are starting this specific file
                    jobContext.NotifyProgress();

                    if (jobContext.Encryption.ShouldEncrypt(sourceFile))
                    {
                        await CopyFileAsync(sourceFile, targetFile, jobContext);
                        // Encryption is usually an external process call (CryptoSoft)
                        encryptionTime = await Task.Run(() => jobContext.Encryption.Encrypt(targetFile, jobContext.EncryptionKey));
                    }
                    else
                    {
                        await CopyFileAsync(sourceFile, targetFile, jobContext);


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
                    if (isPriority)
                    {
                        BackupJob.DecrementGlobalPriority();
                        jobContext.LocalPriorityFilesCount--; // Important to decrement both!
                    }
                    if (semaphoreAcquired)
                    {
                        // CRITICAL: Release the lock so the next large file can start
                        BackupJob.LargeFileSemaphore.Release();
                    }
                }
            }
                
        }

        private async Task CopyFileAsync(string sourceFile, string targetFile, BackupJob jobContext)
        {
            const int BufferSize = 1024 * 1024; // 1MB
            using var source = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var target = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[BufferSize];
            int read;

            while ((read = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                // This allows the "Stop" button to work even mid-file
                jobContext.CheckPauseAndCancellation();

                await target.WriteAsync(buffer, 0, read);
                jobContext.SizeRemaining -= read;

                // Optional: Throttle NotifyProgress to avoid UI flickering
                // jobContext.NotifyProgress(); 
            }
        }
    }
}

