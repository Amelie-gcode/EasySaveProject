using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace EasyLog
{
    public sealed class EasyLogger
    {
        private static readonly Lazy<EasyLogger> LazyInstance = new Lazy<EasyLogger>(() => new EasyLogger());
        private readonly object _writeLock = new object();
        private readonly string _logDirectory;

        public static EasyLogger Instance
        {
            get { return LazyInstance.Value; }
        }

        public EasyLogger()
            : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EasySave", "Logs"))
        {
        }

        public EasyLogger(string logDirectory)
        {
            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                throw new ArgumentException("Log directory cannot be empty.", nameof(logDirectory));
            }

            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
        }

        public void WriteLog(LogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (entry.Timestamp == default(DateTime))
            {
                entry.Timestamp = DateTime.UtcNow;
            }

            string dailyPath = GetDailyLogPath(entry.Timestamp);
            string jsonEntry = BuildJsonEntry(entry);

            lock (_writeLock)
            {
                if (!File.Exists(dailyPath))
                {
                    File.WriteAllText(dailyPath, "[\n" + jsonEntry + "\n]", Encoding.UTF8);
                    return;
                }

                string current = File.ReadAllText(dailyPath, Encoding.UTF8).Trim();
                if (string.IsNullOrEmpty(current))
                {
                    File.WriteAllText(dailyPath, "[\n" + jsonEntry + "\n]", Encoding.UTF8);
                    return;
                }

                string updated;
                if (current == "[]")
                {
                    updated = "[\n" + jsonEntry + "\n]";
                }
                else if (current.EndsWith("]"))
                {
                    string prefix = current.Substring(0, current.Length - 1).TrimEnd();
                    updated = prefix + ",\n" + jsonEntry + "\n]";
                }
                else
                {
                    updated = current + "\n" + jsonEntry;
                }

                File.WriteAllText(dailyPath, updated, Encoding.UTF8);
            }
        }

        private string GetDailyLogPath(DateTime timestamp)
        {
            string fileName = timestamp.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".json";
            return Path.Combine(_logDirectory, fileName);
        }

        private static string BuildJsonEntry(LogEntry entry)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("  {").Append('\n');
            sb.Append("    \"timestamp\": \"").Append(EscapeJson(entry.Timestamp.ToString("o", CultureInfo.InvariantCulture))).Append("\",").Append('\n');
            sb.Append("    \"backupName\": \"").Append(EscapeJson(entry.BackupName ?? string.Empty)).Append("\",").Append('\n');
            sb.Append("    \"sourceFilePath\": \"").Append(EscapeJson(entry.SourceFilePath ?? string.Empty)).Append("\",").Append('\n');
            sb.Append("    \"targetFilePath\": \"").Append(EscapeJson(entry.TargetFilePath ?? string.Empty)).Append("\",").Append('\n');
            sb.Append("    \"fileSize\": ").Append(entry.FileSize.ToString(CultureInfo.InvariantCulture)).Append(',').Append('\n');
            sb.Append("    \"transferTimeMs\": ").Append(entry.TransferTimeMs.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("  }");
            return sb.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
