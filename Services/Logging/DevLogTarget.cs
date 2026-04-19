using System.IO;

namespace TOLTECH_APPLICATION.Services.Logging
{
    public class DevLogTarget : ILogTarget
    {
        private readonly string _logFile;

        public DevLogTarget(string logFilePath = null)
        {
            _logFile = logFilePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Toltech_Dev.log");
        }

        public void Write(LogEntry entry)
        {
            // Console
            Console.WriteLine(entry.ToString());

            // Fichier
            try
            {
                File.AppendAllText(_logFile, entry.ToString() + Environment.NewLine);
            }
            catch { /* ne pas bloquer l'UI */ }
        }
    }
}
