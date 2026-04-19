namespace TOLTECH_APPLICATION.Services.Logging
{
    public interface ILogTarget
    {
        void Write(LogEntry entry);
    }
}
