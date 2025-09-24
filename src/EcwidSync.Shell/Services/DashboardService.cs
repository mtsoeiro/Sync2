using Microsoft.EntityFrameworkCore;
using EcwidSync.Persistence;
namespace EcwidSync.Shell.Dashboard;
public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken ct)
    {
        try
        {
            var can = await _db.Database.CanConnectAsync(ct);
            if (!can) return new DashboardSnapshot { DbOk = false, DbError = "Sem ligação" };

            var cnt = await _db.Products.CountAsync(ct);
            var last = await _db.Products
                                .OrderByDescending(p => p.SyncedAt)
                                .Select(p => (DateTimeOffset?)p.SyncedAt)
                                .FirstOrDefaultAsync(ct);

            var ver = await _db.Database.GetDbConnection().GetSchemaAsync("DataSourceInformation", ct);
            var serverVersion = _db.Database.GetDbConnection().ServerVersion ?? "SQL Server";

            return new DashboardSnapshot
            {
                DbOk = true,
                ServerVersion = serverVersion,
                ProductCount = cnt,
                LastSyncUtc = last
            };
        }
        catch (Exception ex)
        {
            return new DashboardSnapshot { DbOk = false, DbError = ex.Message };
        }
    }
}
