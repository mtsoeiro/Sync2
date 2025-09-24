public sealed class DashboardSnapshot
{
    public bool DbOk { get; init; }
    public string? DbError { get; init; }
    public string ServerVersion { get; init; } = "-";
    public int ProductCount { get; init; }
    public DateTimeOffset? LastSyncUtc { get; init; }
}

public interface IDashboardService
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken ct);
}
