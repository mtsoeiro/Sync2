using Microsoft.EntityFrameworkCore;

namespace EcwidSync.Persistence;

public sealed class EfCategoryStore : ICategoryStore
{
    private readonly AppDbContext _db;
    public EfCategoryStore(AppDbContext db) => _db = db;

    public async Task UpsertBatchAsync(IEnumerable<CategoryRecord> batch, CancellationToken ct = default)
    {
        foreach (var rec in batch)
        {
            var existing = await _db.Categories.FindAsync([rec.Id], ct);
            if (existing is null)
                _db.Categories.Add(rec);
            else if (!string.Equals(existing.RawJson, rec.RawJson, StringComparison.Ordinal))
                _db.Entry(existing).CurrentValues.SetValues(rec);
        }

        await _db.SaveChangesAsync(ct);
    }

    public IAsyncEnumerable<CategoryRecord> GetAllAsync(CancellationToken ct = default)
        => _db.Categories.AsNoTracking().OrderBy(c => c.Id).AsAsyncEnumerable();
}
