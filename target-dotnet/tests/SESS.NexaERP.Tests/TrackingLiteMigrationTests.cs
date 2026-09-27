using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

/// <summary>R1 tracking-lite (migration 138): queues and decision-10 thresholds, the three functions, rollback.</summary>
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void Tracking_lite_installs_sixteen_queues_and_its_functions_and_rolls_back_only_unchanged()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var tracking = Array.IndexOf(migrations, "20260928090000_TrackingLite");
        Assert.True(tracking > 0);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("tracking-up.sql", migrator.GenerateScript("0", migrations[^1]));
        server.Execute("tracking-check.sql", """
            DO $x$ DECLARE company uuid; BEGIN
              IF (SELECT string_agg("Queue"||'='||"OverdueAfterDays",',' ORDER BY "SortOrder") FROM advance.tracking_queues) <>
                 'pr-department-verification=1,pr-approval=2,pr-stock-check=1,rfq-no-quotation=5,quotation-technical-verification=2,'
                 'comparison-decision=2,po-pending-approval=1,po-approved-unissued=1,po-delivery-overdue=0,gate-no-grn=1,'
                 'grn-not-finalised=1,qc-pending=2,mir-approval=1,mir-unissued=1,bill-awaiting-decision=3,grn-without-bill=7'
              THEN RAISE EXCEPTION 'Decision-10 thresholds are wrong.'; END IF;
              SELECT "Id" INTO company FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
              IF EXISTS(SELECT 1 FROM advance.tracking_source(company,'Asia/Kolkata')) THEN
                RAISE EXCEPTION 'An empty company has nothing pending.'; END IF;
              IF (advance.tracking_pending('SESS_PVT_LTD',gen_random_uuid(),'{}','Asia/Kolkata')->>'allowed')::boolean THEN
                RAISE EXCEPTION 'An unknown employee is not allowed.'; END IF;
              IF (advance.tracking_history('SESS_PVT_LTD',gen_random_uuid(),'{}','Asia/Kolkata','PO',gen_random_uuid())->>'found')::boolean THEN
                RAISE EXCEPTION 'An unknown document is not found.'; END IF;
              IF NOT EXISTS(SELECT 1 FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
                  WHERE p."PageDefinitionId"=md5('tracking.pending')::uuid AND r."Code"='STORES_EXECUTIVE')
                OR NOT EXISTS(SELECT 1 FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
                  WHERE p."PageDefinitionId"=md5('tracking.pending')::uuid AND r."Code"='PURCHASE_MANAGER') THEN
                RAISE EXCEPTION 'Stores and Purchase roles must see the pending page.'; END IF;
            END $x$;
            """);
        var down = migrator.GenerateScript(migrations[^1], migrations[tracking - 1]);
        server.Execute("tracking-threshold.sql", """UPDATE advance.tracking_queues SET "OverdueAfterDays"=4,"UpdatedBy"='td' WHERE "Queue"='bill-awaiting-decision';""");
        server.AssertRejected("tracking-down-refused.sql", down, "refuses changed overdue thresholds");
        server.Execute("tracking-threshold-back.sql", """UPDATE advance.tracking_queues SET "OverdueAfterDays"=3,"UpdatedBy"='TrackingLite' WHERE "Queue"='bill-awaiting-decision';""");
        server.Execute("tracking-down.sql", down);
        server.Execute("tracking-up-again.sql", migrator.GenerateScript(migrations[tracking - 1], migrations[^1]));
    }
}
