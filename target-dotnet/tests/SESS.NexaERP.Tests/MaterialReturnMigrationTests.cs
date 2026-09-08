using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string MaterialReturnTarget = "20260907182204_MaterialReturnToStores";

    [Fact]
    public void Material_return_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, MaterialReturnTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("material-return-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("material-return-up.sql", migrator.GenerateScript(predecessor, MaterialReturnTarget) + MaterialReturnAssertions);
        server.Execute("material-return-down.sql", migrator.GenerateScript(MaterialReturnTarget, predecessor));
        server.Execute("material-return-reapply.sql", migrator.GenerateScript(predecessor, MaterialReturnTarget) + MaterialReturnAssertions);
    }

    [Fact]
    public async Task Material_return_changes_are_collected_as_ordinary_command_slots()
    {
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var row = new MaterialReturn { Version = 3 };
        db.MaterialReturns.Add(row);
        db.MaterialReturnHistories.Add(new MaterialReturnHistory
        {
            MaterialReturnId = row.Id, Action = "ACCEPT", FromStatus = "SUBMITTED",
            ToStatus = "ACCEPTED", CorrelationId = "return-accept", Remarks = "accepted"
        });

        var slots = await Rev869BCommandContextAuthorizer.CollectSlotsAsync(db, CancellationToken.None);

        var slot = Assert.Single(slots);
        Assert.Equal("material_return_history", slot.ClaimKind);
        Assert.Equal(nameof(MaterialReturn), slot.EntityType);
        Assert.Equal(row.Id, slot.EntityId);
        Assert.Equal("ACCEPT", slot.Operation);
        Assert.Equal(3, slot.ParentVersion);
    }

    private const string MaterialReturnAssertions = """

        DO $assert$
        BEGIN
          IF to_regclass('advance.material_returns') IS NULL
             OR to_regclass('advance.material_return_lines') IS NULL
             OR to_regclass('advance.material_return_history') IS NULL THEN
            RAISE EXCEPTION 'Material Return tables are missing.';
          END IF;
          IF to_regprocedure('advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)') IS NULL THEN
            RAISE EXCEPTION 'Controlled Material Return posting function is missing.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_material_return_guard')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_material_return_batch_reconcile')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_material_return_movement_reconcile') THEN
            RAISE EXCEPTION 'Material Return database guards are missing.';
          END IF;
          IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey"='stores.material-returns')<>1 THEN
            RAISE EXCEPTION 'Expected one Material Return page.';
          END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions p
                JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
                WHERE d."PageKey"='stores.material-returns')
             <> (SELECT count(*) FROM advance.roles WHERE "IsActive" AND "IsEmployeeAssignable") THEN
            RAISE EXCEPTION 'Material Return role-page grant count is wrong.';
          END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions p
                JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
                JOIN advance.roles r ON r."Id"=p."RoleId"
                WHERE d."PageKey"='stores.material-returns' AND p."CanApprove")<>3 THEN
            RAISE EXCEPTION 'Exactly three Stores roles must hold return acceptance.';
          END IF;
        END $assert$;
        """;
}
