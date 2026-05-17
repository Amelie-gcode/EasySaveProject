using System;
using System.IO;

namespace EasyLog
{
    public sealed class EasyLogger
    {
        private static readonly Lazy<EasyLogger> LazyInstance = new Lazy<EasyLogger>(() => new EasyLogger());
        private readonly object _writeLock = new object();
        private readonly HttpCentralizedLogWriter _centralizedWriter;
        private ILogWriter _localWriter;
        private string _logDirectory;
        private string _machineName;
        private string _userName;
        private LogDestinationMode _destination;
        private string _centralizerBaseUrl;

        public static EasyLogger Instance => LazyInstance.Value;

        private EasyLogger()
        {
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave", "Logs");
            Directory.CreateDirectory(_logDirectory);
            _machineName = Environment.MachineName;
            _userName = Environment.UserName;
            _localWriter = new JsonLogWriter();
            _centralizedWriter = new HttpCentralizedLogWriter();
            _destination = LogDestinationMode.Local;
            _centralizerBaseUrl = "http://localhost:5080";
        }

        public void Configure(LogConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            lock (_writeLock)
            {
                _destination = configuration.Destination;
                _centralizerBaseUrl = string.IsNullOrWhiteSpace(configuration.CentralizerBaseUrl)
                    ? "http://localhost:5080"
                    : configuration.CentralizerBaseUrl;
                _centralizedWriter.SetBaseUrl(_centralizerBaseUrl);
                _localWriter = configuration.UseXmlFormat
                    ? (ILogWriter)new XmlLogWriter()
                    : new JsonLogWriter();
            }
        }

        public void SetLogFormat(ILogWriter writer)
        {
            lock (_writeLock)
            {
                _localWriter = writer ?? new JsonLogWriter();
            }
        }

        public void WriteLog(LogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            if (entry.Timestamp == default)
                entry.Timestamp = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(entry.MachineName))
                entry.MachineName = _machineName;
            if (string.IsNullOrWhiteSpace(entry.UserName))
                entry.UserName = _userName;

            lock (_writeLock)
            {
                if (_destination == LogDestinationMode.Local || _destination == LogDestinationMode.Both)
                    _localWriter.Write(entry, _logDirectory);

                if (_destination == LogDestinationMode.Centralized || _destination == LogDestinationMode.Both)
                    _centralizedWriter.Write(entry, _logDirectory);
            }
        }
    }
}
