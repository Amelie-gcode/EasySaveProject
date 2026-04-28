using System;
using System.IO;

namespace EasyLog
{
    public sealed class EasyLogger
    {
        private static readonly Lazy<EasyLogger> LazyInstance = new Lazy<EasyLogger>(() => new EasyLogger());
        private readonly object _writeLock = new object();
        private ILogWriter _writer;
        private string _logDirectory;

        public static EasyLogger Instance => LazyInstance.Value;

        // Set default strategy to JSON
        private EasyLogger()
        {
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave", "Logs");
            Directory.CreateDirectory(_logDirectory);
            _writer = new JsonLogWriter();
        }

        // Method to change the strategy (Format)
        public void SetLogFormat(ILogWriter writer)
        {
            lock (_writeLock)
            {
                _writer = writer;
            }
        }

        public void WriteLog(LogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            if (entry.Timestamp == default)
                entry.Timestamp = DateTime.UtcNow;

            lock (_writeLock)
            {
                _writer.Write(entry, _logDirectory);
            }
        }
    }
}