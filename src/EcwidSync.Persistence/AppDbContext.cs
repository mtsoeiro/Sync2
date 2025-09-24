using Microsoft.EntityFrameworkCore;

namespace EcwidSync.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<ProductRecord> Products => Set<ProductRecord>();
    public DbSet<CategoryRecord> Categories => Set<CategoryRecord>();

    // 👇 estes três são os do módulo Suppliers
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierRecord> SupplierRecords => Set<SupplierRecord>();   // <-- Faltava
    public DbSet<SupplierFileRecord> SupplierFiles => Set<SupplierFileRecord>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ---------- Products ----------
        b.Entity<ProductRecord>(e =>
        {
            e.ToTable("Products");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Sku).HasMaxLength(128);
            e.Property(p => p.Name).HasMaxLength(1024);
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
            e.Property(p => p.RawJson).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(p => p.SyncedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ---------- Categories ----------
        b.Entity<CategoryRecord>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedNever();
            e.Property(c => c.Name).HasMaxLength(512);
            e.Property(c => c.RawJson).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(c => c.SyncedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ---------- Suppliers ----------
        b.Entity<Supplier>(e =>
        {
            e.ToTable("Suppliers");
            e.HasKey(x => x.Id);

            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();

            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ---------- SupplierRecords ----------
        b.Entity<SupplierRecord>(e =>
        {
            e.ToTable("SupplierRecords");
            e.HasKey(x => x.Id);

            e.Property(x => x.Sku).HasMaxLength(128);
            e.Property(x => x.Raw).HasColumnType("nvarchar(max)");
            e.Property(x => x.ImportedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            // FK correta + navegação para Supplier.Records
            e.HasOne(x => x.Supplier)
             .WithMany(s => s.Records)
             .HasForeignKey(x => x.SupplierId)
             .OnDelete(DeleteBehavior.Restrict);

            // Índice correto por fornecedor + sku
            e.HasIndex(x => new { x.SupplierId, x.Sku });
        });

        // ---------- SupplierFiles ----------
        b.Entity<SupplierFileRecord>(e =>
        {
            e.ToTable("SupplierFiles");
            e.HasKey(x => x.Id);

            e.Property(x => x.FileName).HasMaxLength(256).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(128);
            e.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            e.Property(x => x.Content).HasColumnType("varbinary(max)").IsRequired();
            e.Property(x => x.UploadedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(x => new { x.SupplierId, x.Sha256 }).IsUnique();

            e.HasOne(x => x.Supplier)
             .WithMany()                       // não precisas de coleção de files no Supplier
             .HasForeignKey(x => x.SupplierId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
