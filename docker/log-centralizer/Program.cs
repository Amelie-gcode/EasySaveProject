using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string logsDirectory = Environment.GetEnvironmentVariable("LOGS_DIRECTORY") ?? "/data/logs";
Directory.CreateDirectory(logsDirectory);
var writeLock = new object();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/api/logs", async (HttpRequest request) =>
{
  CentralizedLogEntry? entry;
  try
  {
    entry = await JsonSerializer.DeserializeAsync<CentralizedLogEntry>(
      request.Body,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
  }
  catch
  {
    return Results.BadRequest(new { error = "Invalid JSON payload." });
  }

  if (entry == null)
    return Results.BadRequest(new { error = "Empty log entry." });

  if (entry.Timestamp == default)
    entry.Timestamp = DateTime.UtcNow;

  string fileName = entry.Timestamp.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".json";
  string path = Path.Combine(logsDirectory, fileName);
  string jsonEntry = BuildJsonEntry(entry);

  lock (writeLock)
  {
    if (!File.Exists(path))
    {
      File.WriteAllText(path, "[\n" + jsonEntry + "\n]", Encoding.UTF8);
    }
    else
    {
      string current = File.ReadAllText(path, Encoding.UTF8).Trim();
      string updated = current.EndsWith(']')
        ? current[..^1].TrimEnd() + ",\n" + jsonEntry + "\n]"
        : current + ",\n" + jsonEntry;
      File.WriteAllText(path, updated, Encoding.UTF8);
    }
  }

  return Results.Accepted();
});

app.Run();

static string BuildJsonEntry(CentralizedLogEntry entry) =>
  $@"  {{
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

static string EscapeJson(string? value) =>
  (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

sealed class CentralizedLogEntry
{
  [JsonPropertyName("timestamp")]
  public DateTime Timestamp { get; set; }

  [JsonPropertyName("machineName")]
  public string? MachineName { get; set; }

  [JsonPropertyName("userName")]
  public string? UserName { get; set; }

  [JsonPropertyName("backupName")]
  public string? BackupName { get; set; }

  [JsonPropertyName("sourceFilePath")]
  public string? SourceFilePath { get; set; }

  [JsonPropertyName("targetFilePath")]
  public string? TargetFilePath { get; set; }

  [JsonPropertyName("fileSize")]
  public long FileSize { get; set; }

  [JsonPropertyName("transferTimeMs")]
  public long TransferTimeMs { get; set; }

  [JsonPropertyName("encryptionTimeMs")]
  public long EncryptionTimeMs { get; set; }
}
