namespace EcwidSync.Domain;
public sealed class Category
{
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public int ProductCount { get; init; }
    public System.DateTime? Updated { get; init; }
}
