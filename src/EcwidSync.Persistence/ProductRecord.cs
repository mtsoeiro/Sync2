using System;

namespace EcwidSync.Persistence;

public sealed class ProductRecord
{
    public long Id { get; set; }
    public string? Sku { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public int? Quantity { get; set; }
    public bool Enabled { get; set; }
    public DateTimeOffset? Updated { get; set; }

    // <- AQUI vão TODOS os campos vindos do Ecwid
    public string RawJson { get; set; } = "{}";

    public DateTimeOffset SyncedAt { get; set; } = DateTimeOffset.UtcNow;
}
