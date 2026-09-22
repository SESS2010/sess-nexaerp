using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task InventoryPeriodMigrationUsesDeploymentOwnerAndRefusesChangedCfoSeed()
    {
        const string previous = "20260919100000_IntercompanyInvoiceEvidence";
        const string cfo = "20260919110000_DocumentedCfoAuthorityBaseline";
        const string period = "20260919120000_GovernedInventoryPeriods";
        const string deploy = "SET SESSION AUTHORIZATION nexa_erp_migration; SET ROLE nexa_erp_owner;\n";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator=model.GetService<IMigrator>();
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin(),databaseName:"sess_nexa_erp");
        server.Execute("period-deploy-pre.sql",migrator.GenerateScript("0",previous));
        using var environment=new OrdinaryPrincipalEnvironment(server.ConnectionString,"period-deployment-disposable-only");
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        server.Execute("period-deploy-up.sql",deploy+migrator.GenerateScript(previous,period));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        server.Execute("period-install-row-effects.sql","""
            DO $assert$ BEGIN
             IF EXISTS(SELECT 1 FROM advance.financial_periods WHERE "PeriodType"='INVENTORY')
              OR EXISTS(SELECT 1 FROM advance.inventory_period_events)
              OR EXISTS(SELECT 1 FROM advance.stock_movements)
              OR (SELECT count(*) FROM advance.roles WHERE "Code"='CHIEF_FINANCIAL_OFFICER')<>1
              OR (SELECT count(*) FROM advance.company_role_activations WHERE "RoleId"=md5('role:CHIEF_FINANCIAL_OFFICER')::uuid)<>2
              OR (SELECT count(*) FROM advance.employee_role_assignments WHERE "RoleId"=md5('role:CHIEF_FINANCIAL_OFFICER')::uuid)<>2
              OR (SELECT count(*) FROM advance.employee_role_assignment_events WHERE "ToRoleCode"='CHIEF_FINANCIAL_OFFICER')<>2
              OR (SELECT count(*) FROM advance.page_definitions WHERE "PageKey"='accounts.inventory-periods')<>1
              OR (SELECT count(*) FROM advance.role_page_permissions WHERE "RoleId"=md5('role:CHIEF_FINANCIAL_OFFICER')::uuid)<>1
             THEN RAISE EXCEPTION 'Period installation must create only nine explicit authority/configuration rows and zero period or stock rows.'; END IF;
            END $assert$;
            """);
        server.AssertRejected("period-runtime-evidence-read.sql",
            "SET SESSION AUTHORIZATION nexa_erp_runtime; SELECT * FROM advance.inventory_period_events;","permission denied");
        server.AssertRejected("period-forged-runtime-write.sql", """
            SET SESSION AUTHORIZATION nexa_erp_runtime;
            BEGIN;
            SELECT set_config('sess.inventory_period_write',txid_current()::text,true);
            INSERT INTO advance.financial_periods("Id","CompanyId","Code","Name","PeriodType","StartDate","EndDate",
              "Status","IsActive","CreatedAt","CreatedBy","Version")
             SELECT gen_random_uuid(),"Id",'FORGED','Forged','INVENTORY',DATE '2026-09-01',DATE '2026-09-30',
               'OPEN',true,now(),'forged',0 FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
            COMMIT;
            """, "Inventory periods require the governed CFO command");
        server.AssertRejected("period-noncanonical-forged-runtime-write.sql", """
            SET SESSION AUTHORIZATION nexa_erp_runtime;
            BEGIN;
            SELECT set_config('sess.inventory_period_write',txid_current()::text,true);
            INSERT INTO advance.financial_periods("Id","CompanyId","Code","Name","PeriodType","StartDate","EndDate",
              "Status","IsActive","CreatedAt","CreatedBy","Version")
             SELECT gen_random_uuid(),"Id",'FORGED','Forged',' inventory ',DATE '2026-09-01',DATE '2026-09-30',
               'OPEN',true,now(),'forged',0 FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
            COMMIT;
            """, "Inventory periods require the governed CFO command");
        server.Execute("period-forged-event-grant-fixture.sql","GRANT INSERT ON advance.inventory_period_events TO nexa_erp_runtime;");
        server.AssertRejected("period-forged-event-write.sql", """
            SET SESSION AUTHORIZATION nexa_erp_runtime;
            BEGIN;
            SELECT set_config('sess.inventory_period_write',txid_current()::text,true);
            INSERT INTO advance.inventory_period_events("Id","CompanyId","FinancialPeriodId","Action","PeriodVersion",
              "ActorEmployeeId","RoleAssignmentId","RoleAssignmentType","Reason","RecordedBy")
             VALUES(gen_random_uuid(),gen_random_uuid(),gen_random_uuid(),'OPENED',0,
              gen_random_uuid(),gen_random_uuid(),'FULL','Forged variable','forged');
            COMMIT;
            """, "Inventory period events require a controlled append");
        Assert.NotEqual(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        server.Execute("period-unused-package-down.sql",deploy+migrator.GenerateScript(period,previous));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        server.Execute("period-deploy-reapply.sql",deploy+migrator.GenerateScript(previous,period));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        server.Execute("period-empty-down.sql",deploy+migrator.GenerateScript(period,cfo));
        server.Execute("period-change-authority-fixture.sql","""
            UPDATE advance.roles SET "Name"='Changed CFO authority',"Version"="Version"+1
             WHERE "Code"='CHIEF_FINANCIAL_OFFICER';
            """);
        server.AssertRejected("period-changed-cfo-down.sql",deploy+migrator.GenerateScript(cfo,previous),
            "CFO rollback refuses changed or used authority");
        // The changed fixture is intentionally retained; rollback must not erase it.
    }
}
