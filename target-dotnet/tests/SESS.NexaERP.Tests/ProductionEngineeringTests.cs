using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task Production_engineering_changes_are_collected_as_ordinary_command_slots()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        await using var db = new NexaErpDbContext(options);
        var productionBomId = Guid.NewGuid();
        var productionRevision = new ProductionBomRevision
        {
            ProductionBomId = productionBomId,
            Version = 3
        };
        db.ProductionBomRevisions.Add(productionRevision);
        db.ProductionEngineeringHistories.Add(new ProductionEngineeringHistory
        {
            ProductionBomId = productionBomId,
            ProductionBomRevisionId = productionRevision.Id,
            Action = "Submit",
            ToStatus = "SUBMITTED",
            CorrelationId = "pbom-key",
            Remarks = "submit"
        });

        var documentId = Guid.NewGuid();
        var documentRevision = new EngineeringDocumentRevision
        {
            EngineeringDocumentId = documentId,
            Version = 5
        };
        db.EngineeringDocumentRevisions.Add(documentRevision);
        db.ProductionEngineeringHistories.Add(new ProductionEngineeringHistory
        {
            EngineeringDocumentId = documentId,
            EngineeringDocumentRevisionId = documentRevision.Id,
            Action = "Approve",
            ToStatus = "APPROVED",
            CorrelationId = "drawing-key",
            Remarks = "approve"
        });

        var slots = await Rev869BCommandContextAuthorizer.CollectSlotsAsync(db, CancellationToken.None);

        Assert.Collection(slots.OrderBy(x => x.EntityType),
            slot =>
            {
                Assert.Equal("EngineeringDocument", slot.EntityType);
                Assert.Equal(documentId, slot.EntityId);
                Assert.Equal(5, slot.ParentVersion);
            },
            slot =>
            {
                Assert.Equal("ProductionBom", slot.EntityType);
                Assert.Equal(productionBomId, slot.EntityId);
                Assert.Equal(3, slot.ParentVersion);
            });
        Assert.All(slots, slot => Assert.Equal("production_engineering_history", slot.ClaimKind));
    }

#if MIGRATION_LIFECYCLE_WITNESS
    [Fact]
    public void Production_engineering_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        const string target = "20260907082326_ProductionBomAndEngineeringDocuments";
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, target);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("production-engineering-pre.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("production-engineering-up.sql", migrator.GenerateScript(predecessor, target));
        server.Execute("production-engineering-assert.sql", """
            DO $$ BEGIN
              IF (SELECT count(*) FROM advance.page_definitions
                    WHERE "PageKey" IN ('production.production-bom','design.engineering-documents'))<>2
                THEN RAISE EXCEPTION 'expected two Part 2 pages'; END IF;
              IF (SELECT count(*) FROM advance.role_page_permissions p
                    JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
                    WHERE d."PageKey" IN ('production.production-bom','design.engineering-documents'))<>5
                THEN RAISE EXCEPTION 'expected five Part 2 role grants'; END IF;
              IF (SELECT count(*) FROM advance.employee_page_permissions p
                    JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
                    WHERE d."PageKey"='design.engineering-documents')<>4
                THEN RAISE EXCEPTION 'expected four named drawing grants'; END IF;
              IF to_regclass('advance.production_boms') IS NULL
                 OR to_regclass('advance.production_bom_revisions') IS NULL
                 OR to_regclass('advance.production_bom_lines') IS NULL
                 OR to_regclass('advance.engineering_documents') IS NULL
                 OR to_regclass('advance.engineering_document_revisions') IS NULL
                 OR to_regclass('advance.production_engineering_history') IS NULL
                THEN RAISE EXCEPTION 'Part 2 tables missing'; END IF;
              IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_production_engineering_history_guard')
                 OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_job_order_production_bom_pin')
                 OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_production_bom_revision_guard')
                 OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_engineering_document_revision_guard')
                THEN RAISE EXCEPTION 'Part 2 database guards missing'; END IF;
            END $$;
            """);
        server.Execute("production-engineering-down.sql", migrator.GenerateScript(target, predecessor));
        server.Execute("production-engineering-reup.sql", migrator.GenerateScript(predecessor, target));
    }
#endif

#if MIGRATION_LIFECYCLE_WITNESS
    [Fact]
    public void Production_bom_return_to_draft_authority_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        const string target = "20260911042255_ProductionBomReturnToDraftAuthority";
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, target);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("production-bom-return-pre.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("production-bom-return-up.sql", migrator.GenerateScript(predecessor, target) + AssertProductionBomReturnAuthority(true));
        server.Execute("production-bom-return-down.sql", migrator.GenerateScript(target, predecessor) + AssertProductionBomReturnAuthority(false));
        server.Execute("production-bom-return-reup.sql", migrator.GenerateScript(predecessor, target) + AssertProductionBomReturnAuthority(true));
    }
#endif

    private static string AssertProductionBomReturnAuthority(bool enabled) => $"""
        DO $assert$
        DECLARE grant_count integer; history_definition text; revision_definition text;
        BEGIN
          SELECT count(*) INTO grant_count FROM advance.role_page_permissions p
          JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
          JOIN advance.roles r ON r."Id"=p."RoleId"
          WHERE d."PageKey"='production.production-bom' AND r."Code"='TECHNICAL_DIRECTOR'
            AND p."CanReject" IS {enabled.ToString().ToUpperInvariant()};
          SELECT pg_get_functiondef('advance.guard_production_engineering_history()'::regprocedure) INTO history_definition;
          SELECT pg_get_functiondef('advance.guard_production_bom_revision()'::regprocedure) INTO revision_definition;
          IF grant_count<>1
             OR (position('ReturnToDraft' in history_definition)>0) IS DISTINCT FROM {enabled.ToString().ToLowerInvariant()}
             OR (position('''DRAFT''' in revision_definition)>0) IS DISTINCT FROM {enabled.ToString().ToLowerInvariant()} THEN
            RAISE EXCEPTION 'Production BOM return-to-draft authority mismatch.';
          END IF;
        END $assert$;
        """;
#if MIGRATION_LIFECYCLE_WITNESS
    [Fact]
    public void Engineering_document_return_to_draft_authority_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        const string target = "20260911054347_EngineeringDocumentReturnToDraftAuthority";
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, target);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("engineering-document-return-pre.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("engineering-document-return-up.sql", migrator.GenerateScript(predecessor, target) + AssertEngineeringDocumentReturnAuthority(true));
        server.Execute("engineering-document-return-down.sql", migrator.GenerateScript(target, predecessor) + AssertEngineeringDocumentReturnAuthority(false));
        server.Execute("engineering-document-return-reup.sql", migrator.GenerateScript(predecessor, target) + AssertEngineeringDocumentReturnAuthority(true));
    }
#endif

    private static string AssertEngineeringDocumentReturnAuthority(bool enabled) => $"""
        DO $assert$
        DECLARE grant_count integer; definition text;
        BEGIN
          SELECT count(*) INTO grant_count FROM advance.role_page_permissions p
          JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
          JOIN advance.roles r ON r."Id"=p."RoleId"
          WHERE d."PageKey"='design.engineering-documents' AND r."Code"='TECHNICAL_DIRECTOR'
            AND p."CanReject" IS {enabled.ToString().ToUpperInvariant()};
          SELECT pg_get_functiondef('advance.guard_engineering_document_revision()'::regprocedure) INTO definition;
          IF grant_count<>1 OR (position('''DRAFT''' in definition)>0) IS DISTINCT FROM {enabled.ToString().ToLowerInvariant()} THEN
            RAISE EXCEPTION 'Engineering Document return-to-draft authority mismatch.';
          END IF;
        END $assert$;
        """;}
