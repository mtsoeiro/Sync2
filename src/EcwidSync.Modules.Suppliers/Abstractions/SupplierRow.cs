using EcwidSync.Domain.Suppliers;

namespace EcwidSync.Modules.Suppliers.Abstractions;

public sealed record SupplierRow(
    SupplierKind Supplier,
    string Sku,
    string? Name,
    decimal? Price,
    int? Stock,
    DateTimeOffset ImportedAt,
    string RawLine
);