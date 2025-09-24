using Microsoft.EntityFrameworkCore;

namespace EcwidSync.Persistence;

public sealed class EfProductStore : IProductStore
{
    private readonly AppDbContext _db;
    public EfProductStore(AppDbContext db) => _db = db;

    public async Task UpsertBatchAsync(IEnumerable<ProductRecord> batch, CancellationToken ct = default)
    {
        foreach (var rec in batch)
        {
            var existing = await _db.Products.FindAsync([rec.Id], ct);
            if (existing is null)
            {
                _db.Products.Add(rec);
            }
            else
            {
                // só atualiza se mudou algo (especialmente o JSON)
                if (!string.Equals(existing.RawJson, rec.RawJson, StringComparison.Ordinal))
                    _db.Entry(existing).CurrentValues.SetValues(rec);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public IAsyncEnumerable<ProductRecord> GetAllAsync(CancellationToken ct = default) => _db.Products.AsNoTracking().OrderBy(p => p.Id).AsAsyncEnumerable();

    public async Task<ProductRecord?> GetByIdAsync(long id, CancellationToken ct = default) => await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
}
