using EcwidSync.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcwidSync.Shell;

internal static class DbInit
{
    /// <summary>
    /// Garante BD criada/atualizada. Sem migrações:
    /// - cria do zero se a BD não existir;
    /// - se existirem tabelas em falta, em DEV apaga e recria.
    /// </summary>
    public static async Task EnsureUpToDateAsync(IServiceProvider sp, bool devAutoRecreateIfMissing = true)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // 1) Se houver migrações, é sempre o caminho certo
        var applied = await db.Database.GetAppliedMigrationsAsync();
        var pending = await db.Database.GetPendingMigrationsAsync();
        if (applied.Any() || pending.Any())
        {
            await db.Database.MigrateAsync();
            return;
        }

        // 2) Sem migrações: fluxo DEV
        var creator = db.GetService<IRelationalDatabaseCreator>();
        var dbExists = await creator.ExistsAsync();

        if (!dbExists)
        {
            await db.Database.EnsureCreatedAsync();
            return;
        }

        var missing = await GetMissingTablesAsync(db);
        if (missing.Count == 0)
            return;

        // 👉 Se pediste auto-recreate, faz SEMPRE (sem depender de env vars)
        if (devAutoRecreateIfMissing)
        {
            await RecreateDatabaseAsync(sp, config);
            return;
        }

        // Caso contrário, falha de forma explícita
        throw new InvalidOperationException(
            $"Base de dados existe mas faltam tabelas: {string.Join(", ", missing)}. " +
            "Cria migrações EF ou ativa o auto-recreate em desenvolvimento.");
    }

    // Lê do modelo EF e verifica quais as tabelas em falta
    private static async Task<List<string>> GetMissingTablesAsync(AppDbContext db)
    {
        // tabelas que o modelo EF exige
        var requiredKeys = db.Model.GetEntityTypes()
            .Select(et => new
            {
                Name = et.GetTableName(),
                Schema = et.GetSchema() ?? "dbo"
            })
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .Select(x => $"{x.Schema}.{x.Name}")              // chave "schema.tabela"
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // tabelas que existem na BD
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        SELECT s.name AS [Schema], t.name AS [Name]
        FROM sys.tables t
        JOIN sys.schemas s ON s.schema_id = t.schema_id";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            existing.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
        }

        // quais faltam
        var missing = requiredKeys.Where(k => !existing.Contains(k)).ToList();
        return missing;
    }

    private static async Task RecreateDatabaseAsync(IServiceProvider sp, IConfiguration config)
    {
        var cs = GetConnectionString(config);
        var sb = new SqlConnectionStringBuilder(cs);
        if (string.IsNullOrWhiteSpace(sb.InitialCatalog))
            throw new InvalidOperationException("Connection string sem nome da BD (Initial Catalog).");

        var dbName = sb.InitialCatalog;
        var masterCs = new SqlConnectionStringBuilder(cs) { InitialCatalog = "master" }.ConnectionString;

        // 1) DROP + CREATE no master
        await using (var conn = new SqlConnection(masterCs))
        {
            await conn.OpenAsync();
            var sql = $@"
IF DB_ID(@db) IS NOT NULL
BEGIN
    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{dbName}];
END;
CREATE DATABASE [{dbName}];";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@db", dbName));
            await cmd.ExecuteNonQueryAsync();
        }

        // 2) Usa um NOVO DbContext para criar as tabelas do modelo
        using var scope = sp.CreateScope();
        var freshDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await freshDb.Database.EnsureCreatedAsync();
    }


    private static string GetConnectionString(IConfiguration config) =>
        config.GetConnectionString("Sql")
        ?? "Server=.\\SQLEXPRESS;Database=EcwidSync;Trusted_Connection=True;TrustServerCertificate=True";

}
