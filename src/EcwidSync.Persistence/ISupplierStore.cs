// EcwidSync.Persistence/ISupplierStore.cs
namespace EcwidSync.Persistence;

public interface ISupplierStore
{
    Task<int> EnsureSupplierAsync(string code, string name, CancellationToken ct = default);

    Task<long> SaveFileAsync(
        int supplierId,
        string fileName,
        string? contentType,
        Stream content,
        CancellationToken ct = default);

    IAsyncEnumerable<Supplier> GetSuppliersAsync(CancellationToken ct = default);

    IAsyncEnumerable<SupplierFileRecord> GetFilesAsync(
        int supplierId,
        CancellationToken ct = default);
}
