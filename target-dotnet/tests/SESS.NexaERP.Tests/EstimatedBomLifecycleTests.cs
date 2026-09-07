using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Stores;
using ClosedXML.Excel;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void Estimated_bom_lifecycle_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        const string target = "20260907061217_EstimatedBomLifecycleAndItemGovernance";
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, target);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("estimated-bom-lifecycle-pre.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("estimated-bom-lifecycle-up.sql", migrator.GenerateScript(predecessor, target));
        server.Execute("estimated-bom-lifecycle-assert.sql", """
            DO $$ BEGIN
              IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey"='design.estimated-bom')<>1
                THEN RAISE EXCEPTION 'expected one Estimated BOM page'; END IF;
              IF (SELECT count(*) FROM advance.role_page_permissions p JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
                    WHERE d."PageKey"='design.estimated-bom')<>2
                THEN RAISE EXCEPTION 'expected two Estimated BOM role grants'; END IF;
              IF (SELECT count(*) FROM advance.employee_page_permissions p JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
                    WHERE d."PageKey"='design.estimated-bom')<>4
                THEN RAISE EXCEPTION 'expected four named employee grants'; END IF;
              IF to_regclass('advance.estimated_bom_history') IS NULL OR to_regclass('advance.item_merge_aliases') IS NULL
                THEN RAISE EXCEPTION 'Part 1A evidence tables missing'; END IF;
              IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_estimated_bom_history_governance')
                OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_rfq_line_approved_item')
                THEN RAISE EXCEPTION 'Part 1A database guards missing'; END IF;
            END $$;
            """);
        server.Execute("estimated-bom-lifecycle-down.sql", migrator.GenerateScript(target, predecessor));
        server.Execute("estimated-bom-lifecycle-reup.sql", migrator.GenerateScript(predecessor, target));
    }
}

public sealed class EstimatedBomPolicyTests
{
    [Theory]
    [InlineData("masters.items", "view")]
    [InlineData("masters.items", "create")]
    public void Every_resolved_employee_gets_only_the_draft_item_entry_permissions(string page, string action)
    {
        Assert.True(RoleAuthorityResolution.IsUniversalEmployeePermission(page, action));
        Assert.DoesNotContain("masters.items:approve", RoleAuthorityResolution.UniversalEmployeePermissions);
        Assert.DoesNotContain("masters.items:update", RoleAuthorityResolution.UniversalEmployeePermissions);
    }

    [Fact]
    public void Three_sheet_estimated_bom_import_has_no_master_batch_line_ceiling()
    {
        var bytes = EstimatedBomWorkbook.CreateTemplate(DateTimeOffset.Parse("2026-09-07T00:00:00Z"));
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var data = workbook.Worksheet("Data");
        for (var index = 0; index < 1205; index++)
        {
            var row = index + 2;
            data.Cell(row, 1).Value = "JO-TEST";
            data.Cell(row, 2).Value = "Initial estimate";
            data.Cell(row, 3).Value = index + 1;
            data.Cell(row, 4).Value = "ITEM-" + index;
            data.Cell(row, 5).Value = "NOS";
            data.Cell(row, 6).Value = 1;
        }
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        var parsed = EstimatedBomWorkbook.Read(stream.ToArray());
        Assert.Equal(1205, parsed.Lines.Count);
    }
}
