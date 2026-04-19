namespace Toltech.App.Services.Logging
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public class LogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public string Source { get; set; } // Nom de la VM ou classe
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public Exception Exception { get; set; }

        public override string ToString()
        {
            string exMsg = Exception != null ? $" | Exception: {Exception.Message}" : "";
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level}] [{Source}] {Message}{exMsg}";
        }
    }
}
