// EcwidSync.Persistence/EfSupplierStore.cs
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace EcwidSync.Persistence;

public sealed class EfSupplierStore : ISupplierStore
{
    private readonly AppDbContext _db;
    public EfSupplierStore(AppDbContext db) => _db = db;

    public async Task<int> EnsureSupplierAsync(string code, string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("code obrigatório.", nameof(code));

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Code == code, ct);
        if (supplier is null)
        {
            supplier = new Supplier { Code = code.Trim(), Name = name?.Trim() ?? "" };
            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync(ct);
            return supplier.Id; // int
        }

        var newName = name?.Trim() ?? "";
        if (!string.Equals(supplier.Name, newName, StringComparison.Ordinal))
        {
            supplier.Name = newName;
            await _db.SaveChangesAsync(ct);
        }

        return supplier.Id;
    }

    public async Task<long> SaveFileAsync(
        int supplierId,
        string fileName,
        string? contentType,
        Stream content,
        CancellationToken ct = default)
    {
        if (supplierId <= 0) throw new ArgumentOutOfRangeException(nameof(supplierId));
        if (content is null) throw new ArgumentNullException(nameof(content));

        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await content.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }

        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        var exists = await _db.SupplierFiles
            .AnyAsync(f => f.SupplierId == supplierId && f.Sha256 == sha, ct);

        if (exists)
            throw new InvalidOperationException("Este ficheiro já foi carregado para este fornecedor (hash igual).");

        var rec = new SupplierFileRecord
        {
            SupplierId = supplierId,
            FileName = fileName,
            ContentType = contentType,
            Size = bytes.LongLength,
            Sha256 = sha,
            Content = bytes
        };

        _db.SupplierFiles.Add(rec);
        await _db.SaveChangesAsync(ct);
        return rec.Id; // long
    }

    public async IAsyncEnumerable<Supplier> GetSuppliersAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var s in _db.Suppliers.AsNoTracking()
                                            .OrderBy(x => x.Name)
                                            .AsAsyncEnumerable()
                                            .WithCancellation(ct))
        {
            yield return s;
        }
    }

    public async IAsyncEnumerable<SupplierFileRecord> GetFilesAsync(
        int supplierId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var q = _db.SupplierFiles.AsNoTracking()
                                 .Where(f => f.SupplierId == supplierId)
                                 .OrderByDescending(f => f.UploadedAt);

        await foreach (var f in q.AsAsyncEnumerable().WithCancellation(ct))
            yield return f;
    }
}

