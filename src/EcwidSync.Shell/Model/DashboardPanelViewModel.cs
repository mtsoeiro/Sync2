using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcwidSync.Shared.Notifications;

namespace EcwidSync.Shell.Dashboard;

public partial class DashboardPanelViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IDashboardService _svc;
    private readonly IAppNotifier _notify;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _ticker;

    [ObservableProperty] private string nowFormatted = DateTime.Now.ToString("HH:mm:ss");
    [ObservableProperty] private string dbStatusText = "—";
    [ObservableProperty] private Color dbStatusColor = Colors.Gray;
    [ObservableProperty] private int productCount;
    [ObservableProperty] private string lastSyncFormatted = "—";

    public DashboardPanelViewModel(IDashboardService svc, IAppNotifier notify)
    {
        _svc = svc;
        _notify = notify;

        // arranca já com uma leitura
        _ = RefreshAsync();

        // relógio a cada segundo
        _ticker = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                NowFormatted = DateTime.Now.ToString("HH:mm:ss");
                await Task.Delay(1000, _cts.Token);
            }
        }, _cts.Token);
    }

    [RelayCommand]
    private async Task CheckDbAsync() => await RefreshAsync();

    private async Task RefreshAsync()
    {
        try
        {
            var snap = await _svc.GetSnapshotAsync(_cts.Token);

            DbStatusText = snap.DbOk ? $"OK ({snap.ServerVersion})" : $"Falha: {snap.DbError}";
            DbStatusColor = snap.DbOk ? Colors.LimeGreen : Colors.OrangeRed;

            ProductCount = snap.ProductCount;
            LastSyncFormatted = snap.LastSyncUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "—";
        }
        catch (Exception ex)
        {
            DbStatusText = $"Erro: {ex.Message}";
            DbStatusColor = Colors.OrangeRed;
            _notify.Error("Dashboard", ex.Message, TimeSpan.FromSeconds(5));
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try { await _ticker.ConfigureAwait(false); } catch { }
        _cts.Dispose();
    }
}
