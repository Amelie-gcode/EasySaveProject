using EasyLog;
using EasySave.Models;
using Xunit;

namespace EasySave.Tests
{
    public class LogCentralizationTests
    {
        [Theory]
        [InlineData("Local", LogDestinationMode.Local)]
        [InlineData("CENTRALIZED", LogDestinationMode.Centralized)]
        [InlineData("Both", LogDestinationMode.Both)]
        [InlineData("", LogDestinationMode.Local)]
        public void ParseDestination_ShouldMapSettings(string input, LogDestinationMode expected)
        {
            Assert.Equal(expected, LogBootstrapper.ParseDestination(input));
        }

        [Theory]
        [InlineData(LogDestinationMode.Local, "Local")]
        [InlineData(LogDestinationMode.Centralized, "Centralized")]
        [InlineData(LogDestinationMode.Both, "Both")]
        public void ToSettingValue_ShouldRoundTrip(LogDestinationMode mode, string expected)
        {
            Assert.Equal(expected, LogBootstrapper.ToSettingValue(mode));
        }

        [Fact]
        public void BuildJsonPayload_ShouldIncludeUserAndMachine()
        {
            var entry = new LogEntry
            {
                Timestamp = new DateTime(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc),
                MachineName = "PC-01",
                UserName = "alice",
                BackupName = "Daily"
            };

            string json = HttpCentralizedLogWriter.BuildJsonPayload(entry);

            Assert.Contains("\"machineName\": \"PC-01\"", json);
            Assert.Contains("\"userName\": \"alice\"", json);
            Assert.Contains("\"backupName\": \"Daily\"", json);
        }
    }
}
