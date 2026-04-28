using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace EasyLog
{
    public class JsonLogWriter : ILogWriter
    {
        public void Write(LogEntry entry, string logDirectory)
        {
            string fileName = entry.Timestamp.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".json";
            string path = Path.Combine(logDirectory, fileName);
            string jsonEntry = BuildJsonEntry(entry);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, "[\n" + jsonEntry + "\n]", Encoding.UTF8);
                return;
            }

            string current = File.ReadAllText(path, Encoding.UTF8).Trim();
            string updated = current.EndsWith("]")
                ? current.Substring(0, current.Length - 1).TrimEnd() + ",\n" + jsonEntry + "\n]"
                : current + ",\n" + jsonEntry;

            File.WriteAllText(path, updated, Encoding.UTF8);
        }

        private static string BuildJsonEntry(LogEntry entry)
        {
            return $@"  {{
    ""timestamp"": ""{EscapeJson(entry.Timestamp.ToString("o"))}"",
    ""backupName"": ""{EscapeJson(entry.BackupName)}"",
    ""sourceFilePath"": ""{EscapeJson(entry.SourceFilePath)}"",
    ""targetFilePath"": ""{EscapeJson(entry.TargetFilePath)}"",
    ""fileSize"": {entry.FileSize},
    ""transferTimeMs"": {entry.TransferTimeMs}
  }}";
        }

        private static string EscapeJson(string value) =>
            value?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? string.Empty;
    }
}