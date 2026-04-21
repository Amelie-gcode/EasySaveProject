using System;

namespace EasyLog
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }

        public string BackupName { get; set; }

        public string SourceFilePath { get; set; }

        public string TargetFilePath { get; set; }

        public long FileSize { get; set; }

        public long TransferTimeMs { get; set; }

        public LogEntry()
        {
            Timestamp = DateTime.UtcNow;
            BackupName = string.Empty;
            SourceFilePath = string.Empty;
            TargetFilePath = string.Empty;
        }
    }
}
