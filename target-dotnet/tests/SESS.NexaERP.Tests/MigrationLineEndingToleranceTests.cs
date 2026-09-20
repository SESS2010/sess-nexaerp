using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

// Finding #19: a pulled worktree keeps CRLF, and .gitattributes only governs what Git writes
// on checkout. The migrations themselves must tolerate CRLF sources: every derivation must
// normalize both sides before it searches or compares, and the SQL handed to PostgreSQL must
// be identical whichever line endings the binary was compiled from.
public sealed class MigrationLineEndingToleranceTests
{
    private static NexaErpDbContext Model() => new(new DbContextOptionsBuilder<NexaErpDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);

    [Fact]
    public void MigrationSqlGeneratorNormalizesEveryRawOperationToLf()
    {
        using var model = Model();
        var generator = model.GetService<IMigrationsSqlGenerator>();
        Assert.IsType<LineEndingNormalizingMigrationsSqlGenerator>(generator);
        var crlf = "CREATE OR REPLACE FUNCTION advance.crlf_probe() RETURNS text LANGUAGE sql AS $f$\r\n  SELECT E'\\r\\n';\r\nEND $f$;";
        var commands = generator.Generate([new SqlOperation { Sql = crlf }], model.GetService<IDesignTimeModel>().Model);
        // The command builder terminates each command with Environment.NewLine after the
        // SQL; only the SQL text itself (everything before that terminator) is inspected.
        var command = Assert.Single(commands);
        var text = command.CommandText.TrimEnd('\r', '\n');
        Assert.DoesNotContain("\r\n", text, StringComparison.Ordinal);
        Assert.Contains("E'\\r\\n'", text, StringComparison.Ordinal);
        Assert.Equal(crlf.Replace("\r\n", "\n", StringComparison.Ordinal), text);
    }

    [Fact]
    public void EveryGeneratedMigrationCommandUsesLfInBothDirections()
    {
        using var model = Model();
        var assembly = model.GetService<IMigrationsAssembly>();
        var generator = model.GetService<IMigrationsSqlGenerator>();
        var designModel = model.GetService<IDesignTimeModel>().Model;
        var violations = new List<string>();
        var inspected = 0;
        foreach (var (id, type) in assembly.Migrations)
        {
            var migration = assembly.CreateMigration(type, model.Database.ProviderName!);
            foreach (var (direction, operations) in new[] { ("Up", migration.UpOperations), ("Down", migration.DownOperations) })
            {
                // Materializing the operations exercises every C# derivation; generating each raw
                // SQL operation on its own exercises what PostgreSQL would actually receive. EF's
                // own DDL generator (CreateTable and friends) joins lines with Environment.NewLine,
                // which is harmless DDL formatting and outside this finding.
                foreach (var (operation, index) in operations.Select((operation, index) => (operation, index)))
                {
                    if (operation is not SqlOperation) continue;
                    foreach (var command in generator.Generate([operation], designModel))
                    {
                        inspected++;
                        if (command.CommandText.TrimEnd('\r', '\n').Contains("\r\n", StringComparison.Ordinal))
                            violations.Add($"{id} {direction} SQL operation {index}");
                    }
                }
            }
        }
        Assert.True(inspected > 0, "No generated migration command was inspected.");
        Assert.True(violations.Count == 0, "Generated migration SQL contains CRLF:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void ActualBomLandedRateValuationDerivesTheSameSqlFromCrlfBaselines()
    {
        // Migration 108 searched an LF fragment inside a baseline compiled from the worktree.
        // In a CRLF worktree the baseline carried CRLF and the search failed before any SQL
        // was generated. Both derivations must be line-ending invariant.
        var lf = ImmutableLandedCostAdjustmentsSql.Up.Replace("\r\n", "\n", StringComparison.Ordinal);
        var crlf = lf.Replace("\n", "\r\n", StringComparison.Ordinal);
        Assert.NotEqual(lf, crlf);
        Assert.Equal(ActualBomLandedRateValuation.BillJson(true, lf), ActualBomLandedRateValuation.BillJson(true, crlf));
        Assert.Equal(ActualBomLandedRateValuation.BillJson(false, lf), ActualBomLandedRateValuation.BillJson(false, crlf));
        Assert.Equal(ActualBomLandedRateValuation.OriginalProjection(lf), ActualBomLandedRateValuation.OriginalProjection(crlf));
        Assert.DoesNotContain("\r\n", ActualBomLandedRateValuation.BillJson(true, crlf), StringComparison.Ordinal);
        Assert.DoesNotContain("\r\n", ActualBomLandedRateValuation.CorrectedConfirm, StringComparison.Ordinal);
        Assert.Contains("vendor_bill_inventory_landed_rate(p_company,l.\"Id\")", ActualBomLandedRateValuation.BillJson(true, crlf), StringComparison.Ordinal);
    }
}
