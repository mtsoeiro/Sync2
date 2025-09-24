using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcwidSync.Domain;
using EcwidSync.Modules.Products.Details;
using EcwidSync.Persistence;
using EcwidSync.Shared.Notifications;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace EcwidSync.Modules.Products.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    private readonly IProductsService _svc;
    private readonly IProductStore _store;
    private readonly IAppNotifier _notify;

    // Grelha
    public System.Collections.ObjectModel.ObservableCollection<Product> Items { get; } = new();

    // Índice para atualizar linhas existentes (Id -> index em Items)
    private readonly Dictionary<long, int> _indexById = new();
    public ICollectionView ProductsView { get; }

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string? searchText;
    [ObservableProperty] private Product? selected;
    [ObservableProperty] private string? selectedRawJson;
    [ObservableProperty] private bool detailsLoading; 
    [ObservableProperty] private ProductDetails? selectedDetails;
    

    public ProductsViewModel(IProductsService svc, IProductStore store, IAppNotifier notify)
    {
        _svc = svc;
        _store = store;
        _notify = notify;
        ProductsView = CollectionViewSource.GetDefaultView(Items);
        ProductsView.Filter = FilterProduct;
    }
    partial void OnSearchTextChanged(string? value) => ProductsView.Refresh();

    partial void OnSelectedChanged(Product? value)
    {
        _ = LoadDetailsAsync(value);
    }

    // 1) Filtro por ID como long
    private bool FilterProduct(object? obj)
    {
        if (obj is not Product p) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var q = SearchText.Trim();

        if (p.Name?.Contains(q, StringComparison.CurrentCultureIgnoreCase) == true) return true;
        if (!string.IsNullOrEmpty(p.Sku) &&
            p.Sku.Contains(q, StringComparison.CurrentCultureIgnoreCase)) return true;

        if (long.TryParse(q, out var id)) return p.Id == id;   // <-- aqui
        return false;
    }

    private async Task LoadDetailsAsync(Product? p)
    {
        SelectedRawJson = null;
        SelectedDetails = null;
        if (p is null) return;

        DetailsLoading = true;
        try
        {
            var rec = await _store.GetByIdAsync(p.Id, CancellationToken.None);
            SelectedRawJson = rec?.RawJson; // mantém se quiseres um botão “ver JSON”
            SelectedDetails = MapDetailsFromJson(p, rec?.RawJson);
        }
        catch (Exception ex)
        {
            _notify.Error("Produtos", $"Falha a ler detalhes: {ex.Message}", NotifierDurations.Long);
        }
        finally { DetailsLoading = false; }
    }



    [RelayCommand]
    private void ClearSearch() => SearchText = null;



    // ----------------- 1) CARREGAR DA BD LOCAL AO ABRIR -----------------
    [RelayCommand]
    public async Task LoadLocalAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        Selected = null;
        SelectedRawJson = null;
        try
        {
            Items.Clear();
            _indexById.Clear();

            await foreach (var rec in _store.GetAllAsync(CancellationToken.None))
            {
                var p = new Product
                {
                    Id = rec.Id,
                    Sku = rec.Sku,
                    Name = rec.Name,
                    Price = rec.Price,
                    Quantity = rec.Quantity,
                    Enabled = rec.Enabled,
                    Updated = rec.Updated
                };
                _indexById[p.Id] = Items.Count;
                Items.Add(p);
            }
            ProductsView.Refresh();
            _notify.Info("Produtos", $"Carregados {Items.Count} da BD local", NotifierDurations.Short);
        }
        catch (Exception ex)
        {
            _notify.Error("Erro BD local", ex.Message, NotifierDurations.Long);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ----------------- 2) SINCRONIZAR TUDO DO ECWID (UPSERT) -----------------
    [RelayCommand]
    private async Task LoadAllAsync()
    {

        if (IsLoading) return;

        IsLoading = true;
        Items.Clear();
        _indexById.Clear();
        _notify.Info("Produtos", "A carregar…", TimeSpan.FromSeconds(4));

        var fetched = 0;
        var batch = new List<ProductRecord>(200);

        try
        {
            var seen = new HashSet<long>();
            await foreach ((Product p, string raw) in _svc.GetAllProductsWithRawAsync(100, CancellationToken.None))
            {
                UpsertInGrid(p);
                seen.Add(p.Id);
                Items.Add(p);

                batch.Add(new ProductRecord
                {
                    Id = p.Id,
                    Sku = p.Sku,
                    Name = p.Name,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    Enabled = p.Enabled,
                    Updated = p.Updated,
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

            _notify.Success("Produtos", $"Concluído. Total: {fetched}", NotifierDurations.Short);
            // remove itens que deixaram de existir
            for (int i = Items.Count - 1; i >= 0; i--)
                if (!seen.Contains(Items[i].Id))
                    Items.RemoveAt(i);
            ProductsView.Refresh();
        }
        catch (Exception ex)
        {
            _notify.Error("Erro ao carregar", ex.Message, NotifierDurations.Long);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpsertInGrid(Product p)
    {
        if (_indexById.TryGetValue(p.Id, out var idx))
        {
            // substitui o item na posição (notifica a grelha)
            Items[idx] = p;
        }
        else
        {
            _indexById[p.Id] = Items.Count;
            Items.Add(p);
        }
    }

    private static ProductDetails MapDetailsFromJson(Product p, string? json)
    {
        var d = new ProductDetails
        {
            Id = p.Id,
            Sku = p.Sku,
            Name = p.Name,
            Price = p.Price,
            Enabled = p.Enabled,
            Updated = p.Updated
        };

        if (string.IsNullOrWhiteSpace(json))
            return d;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            static string? GetStr(JsonElement e, string name)
                => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

            static bool? GetBool(JsonElement e, string name)
                => e.TryGetProperty(name, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : null;

            static double? GetNum(JsonElement e, string name)
                => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : (double?)null;

            d.Url = GetStr(root, "url");
            d.InStock = GetBool(root, "inStock");
            d.Unlimited = GetBool(root, "unlimited");
            d.Weight = GetNum(root, "weight");

            // imagem
            d.ImageUrl = GetStr(root, "imageUrl");
            if (root.TryGetProperty("media", out var media) &&
                media.TryGetProperty("images", out var imgs) &&
                imgs.ValueKind == JsonValueKind.Array && imgs.GetArrayLength() > 0)
            {
                var first = imgs[0];
                d.ImageUrl = GetStr(first, "image800pxUrl")
                          ?? GetStr(first, "imageOriginalUrl")
                          ?? d.ImageUrl;
            }

            // categorias
            if (root.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Array)
            {
                foreach (var cc in cats.EnumerateArray())
                {
                    var name = GetStr(cc, "name");
                    if (!string.IsNullOrWhiteSpace(name)) d.Categories.Add(name);
                }
            }

            // atributos
            if (root.TryGetProperty("attributes", out var attrs) && attrs.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in attrs.EnumerateArray())
                {
                    var name = GetStr(a, "name");
                    var val = GetStr(a, "value");
                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(val))
                        d.Attributes.Add(new NameValue(name!, val!));
                }
            }

            // opções (nome + lista de escolhas)
            if (root.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
            {
                foreach (var o in opts.EnumerateArray())
                {
                    var oname = GetStr(o, "name") ?? "Opção";
                    var choices = new List<string>();
                    if (o.TryGetProperty("choices", out var ch) && ch.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var ce in ch.EnumerateArray())
                        {
                            var t = GetStr(ce, "text");
                            if (!string.IsNullOrWhiteSpace(t)) choices.Add(t!);
                        }
                    }
                    d.Options.Add(new OptionItem(oname, choices));
                }
            }

            // descrição (guardar html e versão “texto”)
            d.DescriptionHtml = GetStr(root, "description");
            d.DescriptionText = HtmlToPlain(d.DescriptionHtml);
            // created/updated em string (se existir)
            if (root.TryGetProperty("created", out var c) && c.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(c.GetString(), out var cdt))
                d.Created = cdt;

            if (root.TryGetProperty("updated", out var u) && u.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(u.GetString(), out var udt))
                d.Updated = udt;
        }
        catch
        {
            // ignora erros de parsing e devolve o que já temos
        }

        return d;
    }

    private static string? HtmlToPlain(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return html;
        var text = Regex.Replace(html, "<.*?>", string.Empty);
        text = System.Net.WebUtility.HtmlDecode(text);
        return text.Trim();
    }
}

public interface IProductsService
{
    // já existente no Infrastructure
    IAsyncEnumerable<(Product product, string rawJson)> GetAllProductsWithRawAsync(int pageSize, CancellationToken ct);
}
