using CommonTools.Models;
using System;
using System.Globalization;

namespace ApiGateway.Models
{
    public class LogEntry
    {
        public LogEntry(string message)
        {
            TimeStamp = DateTime.UtcNow;
            Message = message;
        }

        public LogEntry(ServiceState state) : this(state.ToString()) {}

        public override string ToString()
        {
            return $"{TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)}: {Message}";
        }

        public DateTime TimeStamp { get; set; }

        public string Message { get; set; }
    }
}