using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace net.leksi
{
    internal class OutToLog : TextWriter
    {
        private TextWriter prevOut = null;
        private EventLog eventLog;
        private EventLogEntryType logType = EventLogEntryType.Information;
        private string logfile = null;
        private bool need_timestamp = true;

        public OutToLog(TextWriter prevOut, EventLog eventLog, EventLogEntryType logType)
        {
            this.prevOut = prevOut;
            this.eventLog = eventLog;
            this.logType = logType;
        }

        public OutToLog(TextWriter prevOut, string logfile)
        {
            this.prevOut = prevOut;
            this.logfile = logfile;
        }

        public override Encoding Encoding
        {
            get
            {
                return prevOut.Encoding;
            }
        }

        public override void Write(string value)
        {
            if(logfile == null)
            {
                eventLog.WriteEntry(value, this.logType);
            } else
            {
                File.AppendAllText(logfile, (need_timestamp ? string.Format("[{0:s}]", DateTime.Now) : "") + value);
            }
        }

        public override void WriteLine(string value)
        {
            Write(value + "\n");
        }

    }
}
