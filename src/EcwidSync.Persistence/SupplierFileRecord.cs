// EcwidSync.Persistence/SupplierFileRecord.cs
namespace EcwidSync.Persistence;

public sealed class SupplierFileRecord
{
    public long Id { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
  
    public string FileName { get; set; } = "";
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public string Sha256 { get; set; } = "";
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public DateTimeOffset UploadedAt { get; set; }
}
