namespace Toltech.App.Services.Logging
{
    public interface ILogTarget
    {
        void Write(LogEntry entry);
    }
}
