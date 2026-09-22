using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class MigrationSqlLineEndingTests
{
    [Fact]
    public void EveryMigrationEmbeddedSqlUsesLfInBothDirections()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var assembly = model.GetService<IMigrationsAssembly>();
        var violations = new List<string>();
        var inspected = 0;
        foreach (var (id, type) in assembly.Migrations)
        {
            var migration = assembly.CreateMigration(type, model.Database.ProviderName!);
            foreach (var (direction, operations) in new[]
            {
                ("Up", migration.UpOperations),
                ("Down", migration.DownOperations)
            })
            {
                foreach (var (operation, index) in operations.OfType<SqlOperation>().Select((sql, index) => (sql, index)))
                {
                    inspected++;
                    if (operation.Sql.Contains("\r\n", StringComparison.Ordinal))
                        violations.Add($"{id} {direction} SQL operation {index}");
                }
            }
        }
        Assert.True(inspected > 0, "No embedded migration SQL was inspected.");
        Assert.True(violations.Count == 0,
            "Embedded migration SQL contains CRLF. Rebuild from LF migration sources; see .gitattributes.\n" +
            string.Join("\n", violations));
    }
}
