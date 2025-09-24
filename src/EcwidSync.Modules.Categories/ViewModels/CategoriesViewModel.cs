using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcwidSync.Domain;
using EcwidSync.Persistence;
using EcwidSync.Shared.Notifications;

namespace EcwidSync.Modules.Categories.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    private readonly ICategoriesService _svc;
    private readonly ICategoryStore _store;
    private readonly IAppNotifier _notify;

    public ObservableCollection<CategoryTreeNode> Roots { get; } = new();

    [ObservableProperty] private bool isLoading;

    public CategoriesViewModel(ICategoriesService svc, ICategoryStore store, IAppNotifier notify)
    {
        _svc = svc;
        _store = store;
        _notify = notify;
    }

    [RelayCommand]
    public async Task LoadLocalAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var items = new List<Category>();
            await foreach (var rec in _store.GetAllAsync(CancellationToken.None))
            {
                items.Add(new Category
                {
                    Id = unchecked((int)rec.Id),
                    Name = rec.Name,
                    ParentId = rec.ParentId.HasValue ? unchecked((int?)rec.ParentId.Value) : null,
                    Enabled = rec.Enabled,
                    Updated = rec.Updated?.UtcDateTime
                });
            }
            RebuildTree(items);
            _notify.Success("Categorias", $"Carregado local: {items.Count}", NotifierDurations.Short);
        }
        catch (Exception ex)
        {
            _notify.Error("Categorias (local)", ex.Message, NotifierDurations.Long);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task LoadAllAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        _notify.Info("Categorias", "A carregar do Ecwid…", NotifierDurations.Short);

        var fetched = 0;
        var batch = new List<CategoryRecord>(200);
        var list = new List<Category>();

        try
        {
            await foreach ((Category c, string raw) in _svc.GetAllCategoriesWithRawAsync(100, CancellationToken.None))
            {
                list.Add(c);

                batch.Add(new CategoryRecord
                {
                    Id = c.Id,       // int -> long
                    Name = c.Name,
                    ParentId = c.ParentId,
                    Enabled = c.Enabled,
                    Updated = c.Updated.HasValue
                                ? new DateTimeOffset(DateTime.SpecifyKind(c.Updated.Value, DateTimeKind.Utc))
                                : null,
                    RawJson = raw
                });

                fetched++;
                if (batch.Count >= 200)
                {
                    await _store.UpsertBatchAsync(batch, CancellationToken.None);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                await _store.UpsertBatchAsync(batch, CancellationToken.None);

            RebuildTree(list);
            _notify.Success("Categorias", $"Concluído. Total: {fetched}", NotifierDurations.Short);
        }
        catch (Exception ex)
        {
            _notify.Error("Categorias (Ecwid)", ex.Message, NotifierDurations.Long);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand] private void ExpandAll() { foreach (var r in Roots) SetExpanded(r, true); }
    [RelayCommand] private void CollapseAll() { foreach (var r in Roots) SetExpanded(r, false); }

    private static void SetExpanded(CategoryTreeNode n, bool val)
    {
        n.IsExpanded = val;
        foreach (var c in n.Children) SetExpanded(c, val);
    }

    private void RebuildTree(IEnumerable<Category> items)
    {
        var dict = new Dictionary<int, CategoryTreeNode>();
        foreach (var c in items) dict[c.Id] = new CategoryTreeNode(c);

        foreach (var n in dict.Values)
        {
            if (n.ParentId.HasValue && dict.TryGetValue(n.ParentId.Value, out var parent))
            {
                n.Parent = parent;
                parent.Children.Add(n);
            }
        }

        var roots = dict.Values.Where(n => n.Parent is null)
                      .OrderBy(n => n.Name, StringComparer.CurrentCultureIgnoreCase)
                      .ToList();

        Roots.Clear();
        foreach (var r in roots) Roots.Add(r);
    }
}

public interface ICategoriesService
{
    IAsyncEnumerable<(Category category, string rawJson)> GetAllCategoriesWithRawAsync(int pageSize, CancellationToken ct);
}
