using EasyLog;
using System;

namespace EasySave.Models
{
    public static class LogBootstrapper
    {
        public static void Apply(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            EasyLogger.Instance.Configure(new LogConfiguration
            {
                Destination = ParseDestination(settings.LogDestination),
                CentralizerBaseUrl = settings.LogCentralizerUrl,
                UseXmlFormat = string.Equals(settings.LogFormat, "XML", StringComparison.OrdinalIgnoreCase)
            });
        }

        public static LogDestinationMode ParseDestination(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return LogDestinationMode.Local;

            switch (value.Trim().ToUpperInvariant())
            {
                case "CENTRALIZED":
                case "CENTRAL":
                    return LogDestinationMode.Centralized;
                case "BOTH":
                case "LOCALANDCENTRALIZED":
                    return LogDestinationMode.Both;
                default:
                    return LogDestinationMode.Local;
            }
        }

        public static string ToSettingValue(LogDestinationMode mode)
        {
            switch (mode)
            {
                case LogDestinationMode.Centralized:
                    return "Centralized";
                case LogDestinationMode.Both:
                    return "Both";
                default:
                    return "Local";
            }
        }
    }
}
