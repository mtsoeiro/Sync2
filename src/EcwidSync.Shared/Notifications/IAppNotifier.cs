namespace EcwidSync.Shared.Notifications;

public interface IAppNotifier
{
    void Info(string title, string message, TimeSpan duration);
    void Success(string title, string message, TimeSpan duration);
    void Warning(string title, string message, TimeSpan duration);
    void Error(string title, string message, TimeSpan duration);
}

public static class NotifierDurations
{
    public static readonly TimeSpan Short = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan Normal = TimeSpan.FromSeconds(4);
    public static readonly TimeSpan Long = TimeSpan.FromSeconds(6);
}