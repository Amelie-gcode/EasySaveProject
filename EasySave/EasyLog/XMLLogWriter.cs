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
            string path = Path.Combine(logDirectory, fileName);

            // Simple XML append logic or serialization
            XmlSerializer serializer = new XmlSerializer(typeof(LogEntry));
            using (FileStream fs = new FileStream(path, FileMode.Append))
            {
                serializer.Serialize(fs, entry);
            }
        }
    }
}