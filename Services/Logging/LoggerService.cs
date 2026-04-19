using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace TOLTECH_APPLICATION.Services.Logging
{


    public class LoggerService : ILoggerService
    {
        public bool IsAdminMode { get; set; }

        private readonly string _logFolder;
        private readonly string _devLogFile;
        private readonly string _userLogFile;

        private readonly object _lock = new();

        // Collection observable pour l’UI
        public ObservableCollection<LogEntry> Logs { get; }
            = new ObservableCollection<LogEntry>();

        public LoggerService(bool isAdminMode = false)
        {
            #if DEBUG
                        isAdminMode = true;
            #else
                isAdminMode = false;
            #endif

            IsAdminMode = isAdminMode;

            _logFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Logs");

            Directory.CreateDirectory(_logFolder);

            _devLogFile = Path.Combine(_logFolder, "LogDev.log");
            _userLogFile = Path.Combine(_logFolder, "LogUser.log");
        }

        public void LogInfo(string message, string source = null)
            => AddLog(LogLevel.Info, message, source);

        public void LogDebug(string message, string source = null)
        {
            if (IsAdminMode)
                AddLog(LogLevel.Debug, message, source);
        }

        public void LogWarning(string message, string source = null)
            => AddLog(LogLevel.Warning, message, source);

        public void LogError(string message, string source = null, Exception ex = null)
            => AddLog(LogLevel.Error, message, source, ex);

        // Méthode centrale unique
        private void AddLog(
            LogLevel level,
            string message,
            string source,
            Exception ex = null)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = ex == null
                    ? message
                    : $"{message} | {ex.Message}",
                Source = source
            };

            lock (_lock)
            {
                WriteToFile(entry);
            }

            // UI thread safe
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Logs.Add(entry);
            });
        }

        private void WriteToFile(LogEntry entry)
        {
            string line =
                $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] " +
                $"[{entry.Level}] " +
                $"{(string.IsNullOrWhiteSpace(entry.Source) ? "" : $"[{entry.Source}] ")}" +
                $"{entry.Message}";

            // Toujours user
            File.AppendAllText(_userLogFile, line + Environment.NewLine);

            // Dev uniquement si admin
            if (IsAdminMode)
            {
                File.AppendAllText(_devLogFile, line + Environment.NewLine);
            }

            Debug.WriteLine(line);
        }
    }
}
