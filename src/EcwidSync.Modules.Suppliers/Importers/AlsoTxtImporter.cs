// EcwidSync.Modules.Suppliers/Importers/AlsoTxtImporter.cs
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using EcwidSync.Domain.Suppliers;     // SupplierKind
using EcwidSync.Persistence;           // SupplierRecord

namespace EcwidSync.Modules.Suppliers.Importers;

public sealed class AlsoTxtImporter : Abstractions.ISupplierImporter
{
    public SupplierKind Kind => SupplierKind.Also;

    public async IAsyncEnumerable<SupplierRecord> ReadAsync(
        Stream source,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // ALSO costuma vir em Latin1/Windows-1252; se vires acentos trocados,
        // troca para Encoding.GetEncoding(1252).
        using var reader = new StreamReader(
            source,
            Encoding.Latin1,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 8192,
            leaveOpen: true);

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (ct.IsCancellationRequested) yield break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            // PARTIR POR TAB
            var parts = line.Split('\t');
            // linhas boas têm pelo menos 10 colunas (vemos até à marca)
            if (parts.Length < 10) continue;

            var sku = parts[0].Trim(); // ALSO ID (curto e estável)
            var name = parts[5].Trim();
            var stockStr = parts[6].Trim();
            var priceStr = parts[7].Trim();

            // Stock pode vir vazio; tenta inteiro
            int stock = 0;
            _ = int.TryParse(stockStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out stock);

            // Preço em ponto (InvariantCulture)
            decimal price = 0m;
            _ = decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out price);

            yield return new SupplierRecord
            {
                // SupplierId é preenchido pelo serviço durante a gravação
                Sku = sku,        // <= caberá nos 128 chars
                Name = name,
                Price = price,
                Stock = stock,
                Raw = line        // guarda a linha inteira para auditoria
            };
        }
    }
}
