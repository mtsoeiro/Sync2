namespace EcwidSync.Persistence;

public sealed class CategoryRecord
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public bool Enabled { get; set; }
    public DateTimeOffset? Updated { get; set; }

    // guarda TODOS os campos vindos da API
    public string RawJson { get; set; } = "{}";

    public DateTimeOffset SyncedAt { get; set; } = DateTimeOffset.UtcNow;
}
