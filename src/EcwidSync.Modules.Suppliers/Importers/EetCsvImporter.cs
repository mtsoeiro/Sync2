using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using EcwidSync.Domain.Suppliers;     // SupplierKind
using EcwidSync.Modules.Suppliers.Abstractions;
using EcwidSync.Persistence;

namespace EcwidSync.Modules.Suppliers.Importers
{
    /// <summary>
    /// Leitor simples para ficheiros EET em CSV.
    /// Assume separador ';' (ajusta se necessário) e mapeia colunas básicas.
    /// </summary>
    public sealed class EetCsvImporter : ISupplierImporter
    {
        public SupplierKind Kind => SupplierKind.Eet;

        public async IAsyncEnumerable<SupplierRecord> ReadAsync(
            Stream source,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            using var reader = new StreamReader(source, leaveOpen: true);
            bool headerSkipped = false;

            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // ignora a primeira linha se parecer cabeçalho
                if (!headerSkipped)
                {
                    headerSkipped = true;
                    if (line.Contains("SKU", System.StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("UPC", System.StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("Description", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                var cols = line.Split(';'); // ajusta se for outro separador no teu ficheiro

                // heurística: [0]=SKU/UPC, [1]=Nome, [2]=Preço, [3]=Stock
                var sku = cols.Length > 0 ? cols[0].Trim() : null;
                var name = cols.Length > 1 ? cols[1].Trim() : null;
                var price = TryDec(cols, 2);
                var stock = TryInt(cols, 3);

                if (string.IsNullOrWhiteSpace(sku))
                    continue;

                yield return new SupplierRecord
                {
                    Sku = sku!,
                    Name = name,
                    Price = price,
                    Stock = stock,
                    Raw = line
                };
            }
        }

        private static decimal? TryDec(string[] cols, int idx)
        {
            if (idx >= cols.Length) return null;
            var s = cols[idx].Trim().Replace('.', ',');
            return decimal.TryParse(s, NumberStyles.Any, new CultureInfo("pt-PT"), out var v) ? v : null;
        }

        private static int? TryInt(string[] cols, int idx)
        {
            if (idx >= cols.Length) return null;
            return int.TryParse(cols[idx].Trim(), out var v) ? v : null;
        }
    }
}
