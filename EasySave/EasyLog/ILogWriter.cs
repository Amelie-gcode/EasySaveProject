using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyLog
{
    public interface ILogWriter
    {
        void Write(LogEntry entry, string logDirectory);
    }
}
