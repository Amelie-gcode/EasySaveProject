using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EasyLog
{
    public sealed class HttpCentralizedLogWriter : ILogWriter
    {
        private static readonly HttpClient SharedClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        private string _baseUrl = "http://localhost:5080";

        public void SetBaseUrl(string baseUrl)
        {
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? "http://localhost:5080"
                : baseUrl.TrimEnd('/');
        }

        public void Write(LogEntry entry, string logDirectory)
        {
            if (entry == null) return;

            string payload = BuildJsonPayload(entry);
            string endpoint = _baseUrl + "/api/logs";

            Task.Run(() => PostLogAsync(endpoint, payload));
        }

        private static async Task PostLogAsync(string endpoint, string payload)
        {
            try
            {
                using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
                {
                    await SharedClient.PostAsync(endpoint, content).ConfigureAwait(false);
                }
            }
            catch
            {
                // Centralized logging must not interrupt backups.
            }
        }

        public static string BuildJsonPayload(LogEntry entry)
        {
            return $@"{{
  ""timestamp"": ""{EscapeJson(entry.Timestamp.ToString("o", CultureInfo.InvariantCulture))}"",
  ""machineName"": ""{EscapeJson(entry.MachineName)}"",
  ""userName"": ""{EscapeJson(entry.UserName)}"",
  ""backupName"": ""{EscapeJson(entry.BackupName)}"",
  ""sourceFilePath"": ""{EscapeJson(entry.SourceFilePath)}"",
  ""targetFilePath"": ""{EscapeJson(entry.TargetFilePath)}"",
  ""fileSize"": {entry.FileSize},
  ""transferTimeMs"": {entry.TransferTimeMs},
  ""encryptionTimeMs"": {entry.EncryptionTimeMs}
}}";
        }

        private static string EscapeJson(string value) =>
            (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
