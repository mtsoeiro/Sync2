namespace EcwidSync.Persistence;

public interface ICategoryStore
{
    Task UpsertBatchAsync(IEnumerable<CategoryRecord> batch, CancellationToken ct = default);
    IAsyncEnumerable<CategoryRecord> GetAllAsync(CancellationToken ct = default);
}
