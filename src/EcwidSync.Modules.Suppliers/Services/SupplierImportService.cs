using EcwidSync.Domain.Suppliers;   // SupplierKind
using EcwidSync.Modules.Suppliers.Abstractions;
using EcwidSync.Persistence;        // AppDbContext, SupplierRecord, ISupplierStore
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EcwidSync.Modules.Suppliers.Services
{
    public interface ISupplierImportService
    {
        Task<int> ImportFileAsync(SupplierKind kind, string path, CancellationToken ct = default);
    }

    public sealed class SupplierImportService : ISupplierImportService
    {
        private readonly IEnumerable<ISupplierImporter> _importers;
        private readonly AppDbContext _db;
        private readonly ISupplierStore _store;

        public SupplierImportService(
            IEnumerable<ISupplierImporter> importers,
            AppDbContext db,
            ISupplierStore store)
        {
            _importers = importers;
            _db = db;
            _store = store;
        }

        public async Task<int> ImportFileAsync(SupplierKind kind, string path, CancellationToken ct = default)
        {
            // 1) Escolhe o importador certo
            var importer = _importers.FirstOrDefault(i => i.Kind == kind)
                ?? throw new InvalidOperationException($"Não há importador registado para {kind}.");

            // 2) Garante que o fornecedor existe e obtém o ID
            var (code, name) = GetSupplierInfo(kind);
            var supplierId = await _store.EnsureSupplierAsync(code, name, ct);

            // 3) Abre o ficheiro
            await using var fs = File.OpenRead(path);

            // (Opcional) Guarda o ficheiro em SupplierFiles
            // Nota: SaveFileAsync consome o stream, por isso voltamos ao início para o parser
            await _store.SaveFileAsync(
                supplierId,
                Path.GetFileName(path),
                contentType: null,
                content: fs,
                ct);

            fs.Position = 0;

            // 4) Lê e grava registos em lotes
            var total = 0;
            var batch = new List<SupplierRecord>(200);

            await foreach (var r in importer.ReadAsync(fs, ct))
            {
                r.SupplierId = supplierId;
                if (r.ImportedAt == default)
                    r.ImportedAt = DateTimeOffset.UtcNow;

                batch.Add(r);
                total++;

                if (batch.Count >= 200)
                {
                    _db.SupplierRecords.AddRange(batch);
                    await _db.SaveChangesAsync(ct);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                _db.SupplierRecords.AddRange(batch);
                await _db.SaveChangesAsync(ct);
            }

            return total;
        }

        private static (string code, string name) GetSupplierInfo(SupplierKind kind) =>
            kind switch
            {
                SupplierKind.Also => ("ALSO", "ALSO"),
                SupplierKind.Eet => ("EET", "EET Europarts"),
                _ => (kind.ToString().ToUpperInvariant(), kind.ToString())
            };
    }
}
