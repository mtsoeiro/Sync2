namespace EcwidSync.Persistence;

public sealed class Supplier
{
    public int Id { get; set; }
    public string Code { get; set; } = "";     // e.g. "ALSO", "EET"
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<SupplierRecord> Records { get; set; } = new List<SupplierRecord>();
}
