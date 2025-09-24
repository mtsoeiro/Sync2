using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EcwidSync.Persistence;

public interface IProductStore
{
    Task UpsertBatchAsync(IEnumerable<ProductRecord> batch, CancellationToken ct = default);
    IAsyncEnumerable<ProductRecord> GetAllAsync(CancellationToken ct = default);
    Task<ProductRecord?> GetByIdAsync(long id, CancellationToken ct = default);
}
