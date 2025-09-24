using System.Text.Json.Serialization;
namespace EcwidSync.Infrastructure;
internal sealed class EcwidProductsResponse
{
    [JsonPropertyName("items")] public System.Collections.Generic.List<EcwidProductDto> Items { get; set; } = new();
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("limit")] public int? Limit { get; set; }
    [JsonPropertyName("offset")] public int? Offset { get; set; }
}
internal sealed class EcwidProductDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("sku")] public string? Sku { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("price")] public decimal? Price { get; set; }
    [JsonPropertyName("quantity")] public int? Quantity { get; set; }
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("updated")] public string? Updated { get; set; }
}
