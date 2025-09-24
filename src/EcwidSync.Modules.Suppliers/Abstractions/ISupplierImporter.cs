using EcwidSync.Domain.Suppliers;
using EcwidSync.Persistence;
using System.IO;

namespace EcwidSync.Modules.Suppliers.Abstractions
{
    public interface ISupplierImporter
    {
        SupplierKind Kind { get; }
        IAsyncEnumerable<EcwidSync.Persistence.SupplierRecord> ReadAsync(Stream source, CancellationToken ct);
    }
}
