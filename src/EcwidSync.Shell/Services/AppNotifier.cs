using EcwidSync.Shared.Notifications;
using System.Windows; // Dispatcher
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace EcwidSync.Shell.Services;

public sealed class AppNotifier : IAppNotifier
{
    private readonly ISnackbarService _sb;

    public AppNotifier(ISnackbarService snackbarService) => _sb = snackbarService;

    public void Info(string title, string message, TimeSpan duration) => Show(title, message, ControlAppearance.Info, duration);
    public void Success(string title, string message, TimeSpan duration) => Show(title, message, ControlAppearance.Success, duration);
    public void Warning(string title, string message, TimeSpan duration) => Show(title, message, ControlAppearance.Caution, duration);
    public void Error(string title, string message, TimeSpan duration) => Show(title, message, ControlAppearance.Danger, duration);

    private void Show(string title, string message, ControlAppearance style, TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), "duration deve ser > 0");

        void Fire() => _sb.Show(title, message, style, timeout: duration); // em algumas builds: 'duration' em vez de 'timeout'

        var disp = Application.Current?.Dispatcher;
        if (disp is not null && !disp.CheckAccess()) disp.Invoke(Fire);
        else Fire();
    }
}
