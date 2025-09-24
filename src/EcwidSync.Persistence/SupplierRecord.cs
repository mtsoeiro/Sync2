// EcwidSync.Persistence/SupplierRecord.cs
using EcwidSync.Domain.Suppliers;

namespace EcwidSync.Persistence;

public sealed class SupplierRecord
{
    public long Id { get; set; }
    public int SupplierId { get; set; }                 // FK → Suppliers
    public Supplier? Supplier { get; set; }
    public long? SupplierFileRecordId { get; set; }                 // <—
    public SupplierFileRecord? SupplierFileRecord { get; set; }     // <—
    public string Sku { get; set; } = "";
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public int? Stock { get; set; }
    public DateTimeOffset ImportedAt { get; set; }
    public string Raw { get; set; } = "";
}
