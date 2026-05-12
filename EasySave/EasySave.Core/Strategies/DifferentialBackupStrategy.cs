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
        public async Task ExecuteBackupAsync(string sourceDir, string targetDir, BackupJob jobContext)
        {
            // Requirement: Non-blocking discovery
            string[] files = await Task.Run(() => Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories));

            foreach (string sourceFile in files)
            {
                string extension = Path.GetExtension(sourceFile);
                bool isPriority = jobContext.Settings.PriorityExtensions.Contains(extension);

                if (!isPriority)
                {
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

                //  Temporary pause if business software is detected
                // Instead of returning, we wait for the user to close the business app.
                bool wasWaitingForBusinessSoftware = false;
                while (jobContext.Settings != null &&
                       jobContext.BusinessService.IsBusinessSoftwareRunning(jobContext.Settings.BusinessSoftwareName))
                {
                    wasWaitingForBusinessSoftware = true;
                    jobContext.State = JobState.Paused;
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
                    await Task.Delay(1000); // Polling every second
                }

                // CRITICAL FIX: Restore state to Active after business software is closed
                if (wasWaitingForBusinessSoftware)
                {
                    jobContext.State = JobState.Active;
                    jobContext.NotifyProgress();
                }

                // Check for manual Pause or Stop (Play/Pause/Stop functionality)
                jobContext.CheckPauseAndCancellation();

                string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                string targetFile = Path.Combine(targetDir, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                FileInfo sourceInfo = new FileInfo(sourceFile);
                FileInfo targetInfo = new FileInfo(targetFile);
                long fileSize = sourceInfo.Length;

                // Differential Logic
                bool shouldCopy = !targetInfo.Exists || sourceInfo.LastWriteTime > targetInfo.LastWriteTime;

                if (shouldCopy)
                {
                    //check file size against threshold for parallelism
                    long fileSizeInBytes = new FileInfo(sourceFile).Length;
                    long thresholdInBytes = jobContext.Settings.MaxParallelSize * 1024;

                    bool isLargeFile = fileSizeInBytes > thresholdInBytes;
                    bool semaphoreAcquired = false;

                    long transferTimeMs = -1;
                    Stopwatch stopwatch = new Stopwatch();
                    long encryptionTime = 0;

                    try
                    {
                        if (isLargeFile)
                        {
                            // If it's a large file, wait here until the semaphore is free.
                            // Small files in other jobs will NOT be blocked by this.
                            await BackupJob.LargeFileSemaphore.WaitAsync();
                            semaphoreAcquired = true;
                        }
                        jobContext.CurrentSourceFile = sourceFile;
                        jobContext.CurrentTargetFile = targetFile;
                        jobContext.NotifyProgress();

                        stopwatch.Start();

                        if (jobContext.Encryption.ShouldEncrypt(sourceFile))
                        {
                            await CopyFileAsync(sourceFile, targetFile, jobContext);
                            // CryptoSoft is an external single-instance process, offload to task
                            encryptionTime = await Task.Run(() => jobContext.Encryption.Encrypt(targetFile, jobContext.EncryptionKey));
                        }
                        else
                        {
                            await CopyFileAsync(sourceFile, targetFile, jobContext);
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
                        Debug.WriteLine($"Error copying {sourceFile}: {ex.Message}");
                    }
                    finally
                    {
                        jobContext.FilesRemaining--;

                        EasyLogger.Instance.WriteLog(new LogEntry
                        {
                            Timestamp = DateTime.Now,
                            BackupName = jobContext.Name,
                            SourceFilePath = sourceFile,
                            TargetFilePath = targetFile,
                            FileSize = fileSize,
                            TransferTimeMs = transferTimeMs,
                            EncryptionTimeMs = encryptionTime
                        });
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
                else
                {
                    // Logic for skipped files to keep progress bar accurate
                    jobContext.FilesRemaining--;
                    jobContext.SizeRemaining -= fileSize;
                    jobContext.NotifyProgress();
                    if (isPriority)
                    {
                        BackupJob.DecrementGlobalPriority();
                        jobContext.LocalPriorityFilesCount--; // Important to decrement both!
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
                // Check Pause/Cancel between buffer writes (Effective pause after current chunk)
                jobContext.CheckPauseAndCancellation();

                await target.WriteAsync(buffer, 0, read);
                jobContext.SizeRemaining -= read;

                // Update UI every few chunks to prevent overhead
                // jobContext.NotifyProgress(); 
            }
        }
    }
}