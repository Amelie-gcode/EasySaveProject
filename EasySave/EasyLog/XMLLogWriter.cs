using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace EasyLog
{
    public class XmlLogWriter : ILogWriter
    {
        public void Write(LogEntry entry, string logDirectory)
        {
            string fileName = entry.Timestamp.ToLocalTime().ToString("yyyy-MM-dd") + ".xml";
            string path = Path.Combine(logDirectory ?? string.Empty, fileName);

            // Ensure directory exists
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Use a List<LogEntry> container so the XML file remains valid after multiple writes.
            try
            {
                var serializer = new XmlSerializer(typeof(List<LogEntry>));
                List<LogEntry> entries = new List<LogEntry>();

                if (File.Exists(path))
                {
                    // Read existing entries
                    try
                    {
                        using (var fsRead = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            if (fsRead.Length > 0)
                            {
                                var deserialized = serializer.Deserialize(fsRead);
                                if (deserialized is List<LogEntry>)
                                    entries = (List<LogEntry>)deserialized;
                            }
                        }
                    }
                    catch
                    {
                        // If deserialize fails, overwrite file with a fresh list
                        entries = new List<LogEntry>();
                    }
                }

                entries.Add(entry);

                // Serialize the full list back to the file (atomic replace)
                using (var fsWrite = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    serializer.Serialize(fsWrite, entries);
                }
            }
            catch
            {
                // Swallow exceptions to avoid breaking the main flow; logging fallback could be added
            }
        }
    }
}