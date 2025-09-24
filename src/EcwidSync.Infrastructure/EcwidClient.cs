using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using EcwidSync.Domain;

namespace EcwidSync.Infrastructure;

public interface IEcwidClient
{
    Task<(IReadOnlyList<Product> items, int count, int total)> GetProductsAsync(int offset = 0, int limit = 100, CancellationToken ct = default);

    IAsyncEnumerable<Product> GetAllProductsAsync(int pageSize = 100, CancellationToken ct = default);

    Task<string> GetProfileNameAsync(CancellationToken ct = default);
    IAsyncEnumerable<(EcwidSync.Domain.Product product, string rawJson)> GetAllProductsWithRawAsync(int pageSize = 100, CancellationToken ct = default);

    IAsyncEnumerable<(EcwidSync.Domain.Category category, string rawJson)> GetAllCategoriesWithRawAsync(int pageSize = 100, CancellationToken ct = default);


}

public sealed class EcwidClient : IEcwidClient
{
    private readonly HttpClient _http;
    private readonly EcwidOptions _opt;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    // se o Bearer falhar (401/403), ativa fallback por querystring
    private bool _useQueryAuth = false;

    public EcwidClient(HttpClient http, IOptions<EcwidOptions> opt)
    {
        _http = http;
        _opt = opt.Value;

        _http.BaseAddress = new Uri($"{_opt.BaseUrl.TrimEnd('/')}/{_opt.StoreId}/");
        if (!string.IsNullOrWhiteSpace(_opt.Token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opt.Token);
    }

    public async Task<(IReadOnlyList<Product> items, int count, int total)> GetProductsAsync( int offset = 0, int limit = 100, CancellationToken ct = default)
    {
        var url = BuildUrl($"products?offset={offset}&limit={limit}");

        using var resp = await GetWithAuthRetryAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Ecwid {(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");

        var payload = await resp.Content.ReadFromJsonAsync<EcwidProductsResponse>(_json, ct)
                      ?? new EcwidProductsResponse();

        var items = payload.Items.Select(x => new Product
        {
            Id = x.Id,
            Sku = x.Sku,
            Name = x.Name,
            Price = x.Price,
            Quantity = x.Quantity,
            Enabled = x.Enabled,
            Updated = DateTimeOffset.TryParse(x.Updated, out var dt) ? dt : null
        }).ToList();

        return (items, payload.Count, payload.Total);
    }

    public async IAsyncEnumerable<Product> GetAllProductsAsync( int pageSize = 100, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var offset = 0;
        while (true)
        {
            var (items, count, total) = await GetProductsAsync(offset, pageSize, ct);
            if (items.Count == 0) yield break;

            foreach (var p in items)
                yield return p;

            offset += items.Count;

            // termina quando atingir o total (se fornecido) ou quando vier menos do que pageSize
            if ((total > 0 && offset >= total) || items.Count < pageSize)
                yield break;
        }
    }

    public async Task<string> GetProfileNameAsync(CancellationToken ct = default)
    {
        using var resp = await GetWithAuthRetryAsync(BuildUrl("profile"), ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Ecwid {(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
                  .GetProperty("generalInfo")
                  .GetProperty("storeName")
                  .GetString() ?? "(sem nome)";
    }


    public async IAsyncEnumerable<(EcwidSync.Domain.Product product, string rawJson)> GetAllProductsWithRawAsync(int pageSize = 100, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var offset = 0;
        while (true)
        {
            var url = BuildUrl($"products?offset={offset}&limit={pageSize}");
            using var resp = await GetWithAuthRetryAsync(url, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"Ecwid {(int)resp.StatusCode} {resp.ReasonPhrase}: {json}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("items", out var itemsElem) || itemsElem.GetArrayLength() == 0)
                yield break;

            foreach (var el in itemsElem.EnumerateArray())
            {
                var raw = el.GetRawText();
                // Reutiliza o DTO existente para mapear ao domínio
                var dto = System.Text.Json.JsonSerializer.Deserialize<EcwidProductDto>(raw, _json) ?? new EcwidProductDto();
                var product = new EcwidSync.Domain.Product
                {
                    Id = dto.Id,
                    Sku = dto.Sku,
                    Name = dto.Name,
                    Price = dto.Price,
                    Quantity = dto.Quantity,
                    Enabled = dto.Enabled,
                    Updated = DateTimeOffset.TryParse(dto.Updated, out var dt) ? dt : null
                };
                yield return (product, raw);
            }

            var count = root.TryGetProperty("count", out var c) ? c.GetInt32() : itemsElem.GetArrayLength();
            var total = root.TryGetProperty("total", out var t) ? t.GetInt32() : 0;
            offset += count;
            if ((total > 0 && offset >= total) || count == 0) yield break;
        }
    }

    public async IAsyncEnumerable<(EcwidSync.Domain.Category category, string rawJson)> GetAllCategoriesWithRawAsync(int pageSize = 100, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var offset = 0;

        while (true)
        {
            var url = BuildUrl($"categories?offset={offset}&limit={pageSize}");
            using var resp = await GetWithAuthRetryAsync(url, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"Ecwid {(int)resp.StatusCode} {resp.ReasonPhrase}: {json}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
                yield break;

            foreach (var el in items.EnumerateArray())
            {
                // id e parentId como INT (o teu Domain.Category usa int)
                int id = 0;
                if (el.TryGetProperty("id", out var idEl))
                    id = idEl.TryGetInt32(out var i) ? i :
                         (idEl.TryGetInt64(out var i64) ? unchecked((int)i64) : 0);

                int? parentId = null;
                if (el.TryGetProperty("parentId", out var p) && p.ValueKind != JsonValueKind.Null)
                {
                    if (p.TryGetInt32(out var pi)) parentId = pi;
                    else if (p.TryGetInt64(out var pi64)) parentId = unchecked((int)pi64);
                }

                var name = el.TryGetProperty("name", out var n) ? (n.GetString() ?? "") : "";
                var enabled = el.TryGetProperty("enabled", out var en) && en.ValueKind == JsonValueKind.True;

                // Updated como DateTime? (não DateTimeOffset?)
                DateTime? updated = null;
                if (el.TryGetProperty("updated", out var upd) && upd.ValueKind == JsonValueKind.String)
                {
                    var s = upd.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        if (DateTime.TryParse(s, out var dt)) updated = dt;
                        else if (DateTimeOffset.TryParse(s, out var dto)) updated = dto.UtcDateTime;
                    }
                }

                var cat = new EcwidSync.Domain.Category
                {
                    Id = id,
                    Name = name,
                    ParentId = parentId,
                    Enabled = enabled,
                    Updated = updated
                };

                yield return (cat, el.GetRawText());
            }

            var count = root.TryGetProperty("count", out var cEl) ? cEl.GetInt32() : items.GetArrayLength();
            var total = root.TryGetProperty("total", out var tEl) ? tEl.GetInt32() : 0;

            offset += count;
            if ((total > 0 && offset >= total) || count == 0) yield break;
        }
    }

    // ---------- helpers ----------

    private string BuildUrl(string pathAndQuery)
    {
        if (_useQueryAuth)
        {
            return pathAndQuery.Contains('?')
                ? $"{pathAndQuery}&token={_opt.Token}"
                : $"{pathAndQuery}?token={_opt.Token}";
        }
        return pathAndQuery;
    }

    private async Task<HttpResponseMessage> GetWithAuthRetryAsync(string url, CancellationToken ct)
    {
        if (_useQueryAuth)
            return await _http.GetAsync(BuildUrl(url), ct); // já vai direto com ?token=

        var resp = await _http.GetAsync(url, ct); // tenta Bearer
        if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            resp.Dispose();
            _useQueryAuth = true;
            return await _http.GetAsync(BuildUrl(url), ct); // retry com ?token=
        }
        return resp;
    }

}
