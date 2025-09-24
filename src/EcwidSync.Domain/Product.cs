namespace EcwidSync.Domain;
public sealed class Product
{
    public long Id { get; init; }
    public string? Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal? Price { get; init; }
    public int? Quantity { get; init; }
    public bool Enabled { get; init; }
    public System.DateTimeOffset? Updated { get; init; }
}
