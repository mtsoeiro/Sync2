// EcwidSync.Modules.Suppliers/ViewModels/SuppliersViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcwidSync.Domain.Suppliers;
using EcwidSync.Modules.Suppliers.Services;
using EcwidSync.Persistence;
using EcwidSync.Shared.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EcwidSync.Modules.Suppliers.ViewModels;

public sealed partial class SuppliersViewModel : ObservableObject
{
    private readonly ISupplierImportService _svc;
    private readonly IAppNotifier _notify;
    private readonly AppDbContext _db;

    // ---- Estado (importação) ----------------------------------------------
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? status;
    [ObservableProperty] private string? lastFile;
    [ObservableProperty] private int lastImported;
    [ObservableProperty] private DateTimeOffset? lastImportedAt;

    // ---- Base de dados (listagens) ----------------------------------------
    public ObservableCollection<Supplier> DbSuppliers { get; } = new();
    public ObservableCollection<SupplierFileRecord> DbFiles { get; } = new();
    public ObservableCollection<SupplierRecord> DbRecords { get; } = new();

    [ObservableProperty] private Supplier? dbSelectedSupplier;
    [ObservableProperty] private SupplierFileRecord? dbSelectedFile;
    [ObservableProperty] private string? dbSearchText;
    [ObservableProperty] private bool dbIsBusy;
    [ObservableProperty] private int dbPageIndex;
    [ObservableProperty] private int dbPageSize = 200;
    [ObservableProperty] private int dbTotalRows;

    public IAsyncRelayCommand DbRefreshCommand { get; }
    public IAsyncRelayCommand DbExportCsvCommand { get; }

    private CancellationTokenSource? _dbSearchCts;

    public SuppliersViewModel(ISupplierImportService svc, IAppNotifier notify, AppDbContext db)
    {
        _svc = svc;
        _notify = notify;
        _db = db;

        // comandos BD
        DbRefreshCommand = new AsyncRelayCommand(LoadDbAsync);
        DbExportCsvCommand = new AsyncRelayCommand(ExportDbCsvAsync);

        // reações a seleção/pesquisa
        PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(DbSelectedSupplier))
                await LoadDbFilesAsync();

            if (e.PropertyName == nameof(DbSelectedFile))
            {
                DbPageIndex = 0;
                await LoadDbRecordsAsync();
            }
        };

        // inicializa o painel BD sem depender do Loaded da View
        _ = InitDbPanelAsync();
    }

    // debounce da pesquisa (gerado pelo ObservableProperty)
    partial void OnDbSearchTextChanged(string? value)
    {
        _dbSearchCts?.Cancel();
        _dbSearchCts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, _dbSearchCts.Token);
                DbPageIndex = 0;
                await LoadDbRecordsAsync();
            }
            catch (OperationCanceledException) { /* ignore */ }
        });
    }

    // ---- Inicialização do painel BD ---------------------------------------
    public async Task InitDbPanelAsync()
    {
        if (DbSuppliers.Count > 0) return;
        await LoadDbSuppliersAsync();
        await LoadDbFilesAsync();
        await LoadDbRecordsAsync();
    }

    private async Task LoadDbSuppliersAsync()
    {
        DbIsBusy = true;
        try
        {
            DbSuppliers.Clear();
            var list = await _db.Suppliers.AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync();

            foreach (var s in list) DbSuppliers.Add(s);
            DbSelectedSupplier ??= DbSuppliers.FirstOrDefault();
        }
        finally { DbIsBusy = false; }
    }

    private async Task LoadDbFilesAsync()
    {
        DbIsBusy = true;
        try
        {
            DbFiles.Clear();

            var q = _db.SupplierFiles.AsNoTracking();
            if (DbSelectedSupplier is not null)
                q = q.Where(f => f.SupplierId == DbSelectedSupplier.Id);

            var files = await q.OrderByDescending(f => f.UploadedAt)
                               .Take(300)
                               .ToListAsync();

            foreach (var f in files) DbFiles.Add(f);
            DbSelectedFile ??= DbFiles.FirstOrDefault();
        }
        finally { DbIsBusy = false; }
    }

    private async Task LoadDbRecordsAsync()
    {
        DbIsBusy = true;
        try
        {
            DbRecords.Clear();

            var q = _db.SupplierRecords.AsNoTracking();

            if (DbSelectedSupplier is not null)
                q = q.Where(r => r.SupplierId == DbSelectedSupplier.Id);

            if (DbSelectedFile?.Id > 0)
                q = q.Where(r => r.SupplierFileRecordId == DbSelectedFile!.Id);

            if (!string.IsNullOrWhiteSpace(DbSearchText))
            {
                var s = DbSearchText.Trim();
                q = q.Where(r => r.Sku.Contains(s) || r.Name.Contains(s));
            }

            DbTotalRows = await q.CountAsync();

            var rows = await q.OrderBy(r => r.Sku)
                              .Select(r => new SupplierRecord
                              {
                                  Id = r.Id,
                                  SupplierId = r.SupplierId,
                                  SupplierFileRecordId = r.SupplierFileRecordId,
                                  Sku = r.Sku,
                                  Name = r.Name,
                                  Price = r.Price,
                                  Stock = r.Stock,
                                  ImportedAt = r.ImportedAt
                              })
                              .Skip(DbPageIndex * DbPageSize)
                              .Take(DbPageSize)
                              .ToListAsync();

            foreach (var r in rows) DbRecords.Add(r);
        }
        finally { DbIsBusy = false; }
    }

    private async Task LoadDbAsync()
    {
        await LoadDbSuppliersAsync();
        await LoadDbFilesAsync();
        await LoadDbRecordsAsync();
    }

    private async Task ExportDbCsvAsync()
    {
        DbIsBusy = true;
        try
        {
            var q = _db.SupplierRecords.AsNoTracking();

            if (DbSelectedSupplier is not null)
                q = q.Where(r => r.SupplierId == DbSelectedSupplier.Id);

            if (DbSelectedFile?.Id > 0)
                q = q.Where(r => r.SupplierFileRecordId == DbSelectedFile!.Id);

            if (!string.IsNullOrWhiteSpace(DbSearchText))
            {
                var s = DbSearchText.Trim();
                q = q.Where(r => r.Sku.Contains(s) || r.Name.Contains(s));
            }

            var rows = await q.OrderBy(r => r.Sku).ToListAsync();

            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"SupplierRecords_{DbSelectedSupplier?.Name}_{DbSelectedFile?.Id}.csv");

            await File.WriteAllLinesAsync(path, rows.Select(r =>
                string.Join(';', new[]
                {
                    r.Sku,
                    (r.Name ?? string.Empty).Replace(';', ','),
                    (r.Price ?? 0m).ToString("0.00"),
                    (r.Stock ?? 0).ToString(),
                    r.ImportedAt.ToString("yyyy-MM-dd HH:mm")
                })));

            _notify.Success("Exportação", $"CSV exportado para {path}", NotifierDurations.Normal);
        }
        catch (Exception ex)
        {
            _notify.Error("Erro a exportar", ex.Message, NotifierDurations.Long);
        }
        finally { DbIsBusy = false; }
    }

    // ---- Comandos (ALSO / EET) --------------------------------------------
    [RelayCommand]
    private async Task ImportAlsoAsync()
        => await ImportFromDialogAsync(
            SupplierKind.Also,
            filter: "Ficheiros ALSO|*.txt;*.csv|Todos|*.*");

    [RelayCommand]
    private async Task ImportEetAsync()
        => await ImportFromDialogAsync(
            SupplierKind.Eet,
            filter: "Ficheiros EET|*.csv;*.txt|Todos|*.*");

    // ---- Helpers -----------------------------------------------------------
    private async Task ImportFromDialogAsync(SupplierKind kind, string filter)
    {
        if (IsBusy) return;

        var dlg = new OpenFileDialog
        {
            Title = $"Importar {kind}",
            Filter = filter,
            Multiselect = false
        };

        if (dlg.ShowDialog() == true)
            await ImportFileAsync(kind, dlg.FileName, CancellationToken.None);
    }

    /// <summary>Importa um ficheiro diretamente (também útil para drag&drop).</summary>
    public async Task ImportFileAsync(SupplierKind kind, string path, CancellationToken ct)
    {
        if (IsBusy) return;

        IsBusy = true;
        Status = "A importar…";
        LastFile = Path.GetFileName(path);
        LastImported = 0;
        LastImportedAt = null;

        try
        {
            var total = await _svc.ImportFileAsync(kind, path, ct);

            LastImported = total;
            LastImportedAt = DateTimeOffset.Now;
            Status = $"Importação concluída ({total} registos).";

            _notify.Success("Fornecedores", $"{kind}: {total} registos importados.", NotifierDurations.Short);

            // refresca a área Base de Dados imediatamente
            await LoadDbFilesAsync();
            await LoadDbRecordsAsync();
        }
        catch (OperationCanceledException)
        {
            Status = "Importação cancelada.";
            _notify.Warning("Fornecedores", "Importação cancelada.", TimeSpan.FromSeconds(4));
        }
        catch (Exception ex)
        {
            Status = "Falha na importação.";
            _notify.Error("Fornecedores", ex.Message, NotifierDurations.Long);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
