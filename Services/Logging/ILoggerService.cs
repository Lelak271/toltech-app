using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toltech.App.Services.Logging
{
    public interface ILoggerService
    {
        bool IsAdminMode { get; set; }

        ObservableCollection<LogEntry> Logs { get; }

        void LogInfo(string message, string source = null);
        void LogDebug(string message, string source = null);
        void LogWarning(string message, string source = null);
        void LogError(string message, string source = null, Exception ex = null);
    }
}
