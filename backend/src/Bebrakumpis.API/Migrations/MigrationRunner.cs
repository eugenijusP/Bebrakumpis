using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Bebrakumpis.Infrastructure.Persistence;

namespace Bebrakumpis.API.Migrations;

public class MigrationRunner(IDbConnectionFactory connectionFactory, IConfiguration configuration, ILogger<MigrationRunner> logger)
{
    public virtual async Task RunAsync()
    {
        await EnsureDatabaseExistsAsync();

        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        await connection.ExecuteAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '_migrations')
            CREATE TABLE _migrations (
                migration_name NVARCHAR(255) NOT NULL PRIMARY KEY,
                applied_at     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
            )
            """);

        var applied = (await connection.QueryAsync<string>(
            "SELECT migration_name FROM _migrations ORDER BY applied_at")).ToHashSet();

        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames()
            .Where(r => r.Contains(".Migrations.") && r.EndsWith(".sql"))
            .OrderBy(r => r);

        foreach (var resource in resources)
        {
            var parts = resource.Split('.');
            var migrationName = string.Join(".", parts.TakeLast(2));

            if (applied.Contains(migrationName))
            {
                logger.LogDebug("Migration already applied: {Migration}", migrationName);
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync();

            logger.LogInformation("Applying migration: {Migration}", migrationName);
            await connection.ExecuteAsync(sql);
            await connection.ExecuteAsync(
                "INSERT INTO _migrations (migration_name, applied_at) VALUES (@Name, SYSUTCDATETIME())",
                new { Name = migrationName });
        }
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        using var masterConnection = new SqlConnection(builder.ConnectionString);
        await masterConnection.OpenAsync();

        var exists = await masterConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM sys.databases WHERE name = @name",
            new { name = databaseName });

        if (exists == 0)
        {
            logger.LogInformation("Database '{Database}' not found — creating.", databaseName);
            await masterConnection.ExecuteAsync($"CREATE DATABASE [{databaseName}]");
        }
    }
}
