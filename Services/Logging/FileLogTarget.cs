using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toltech.App.Services.Logging
{
    public class FileLogTarget : ILogTarget
    {
        private readonly string _logFile;

        public FileLogTarget()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ToltechLogs");
            Directory.CreateDirectory(folder);
            _logFile = Path.Combine(folder, "User.log");
        }

        public void Write(LogEntry entry)
        {
            try
            {
                File.AppendAllText(_logFile, entry.ToString() + Environment.NewLine);
            }
            catch { }
        }
    }
}
