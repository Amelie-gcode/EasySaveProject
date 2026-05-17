namespace EasyLog
{
    public sealed class LogConfiguration
    {
        public LogDestinationMode Destination { get; set; } = LogDestinationMode.Local;

        public string CentralizerBaseUrl { get; set; } = "http://localhost:5080";

        public bool UseXmlFormat { get; set; }
    }
}
