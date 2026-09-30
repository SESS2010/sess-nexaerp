using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task R1_purchase_production_grants_roundtrip_and_production_manager_cannot_approve_po(bool existingCustomGrants)
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260929143000_VendorImportSourceApproval";
        const string target = "20260929181000_R1PurchaseProductionGrants";
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("role-baseline.sql", migrator.GenerateScript("0", previous));
        var up = migrator.GenerateScript(previous, target);
        var down = migrator.GenerateScript(target, previous);
        // A custom full-control grant would bypass a removed Approve bit. Refuse,
        // rather than silently revoke unrelated implicit permissions or leave a bypass.
        server.Execute("role-full-control.sql", """
            UPDATE advance.role_page_permissions g SET "HasFullControl"=true
            FROM advance.roles r,advance.page_definitions p WHERE g."RoleId"=r."Id" AND g."PageDefinitionId"=p."Id"
              AND r."Code"='PRODUCTION_MANAGER' AND p."PageKey"='purchase.po';
            """);
        server.AssertRejected("role-conflict-refused.sql", up, "refuse conflicting custom approval or full-control grants");
        server.Execute("role-reset-control.sql", """
            UPDATE advance.role_page_permissions g SET "HasFullControl"=false
            FROM advance.roles r,advance.page_definitions p WHERE g."RoleId"=r."Id" AND g."PageDefinitionId"=p."Id"
              AND r."Code"='PRODUCTION_MANAGER' AND p."PageKey"='purchase.po';
            """);
        if (existingCustomGrants)
            server.Execute("role-custom.sql", """
                INSERT INTO advance.role_page_permissions
                SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(g) || jsonb_build_object(
                  'Id',md5('role-custom:'||p."PageKey")::uuid,'PageDefinitionId',p."Id",'CanView',false,
                  'CanExport',true,'UpdatedBy','custom-test','Version',17))).*
                FROM advance.role_page_permissions g JOIN advance.roles r ON r."Id"=g."RoleId"
                JOIN advance.page_definitions source ON source."Id"=g."PageDefinitionId"
                CROSS JOIN advance.page_definitions p
                WHERE (r."Code"='PRODUCTION_MANAGER' AND source."PageKey"='masters.uoms' AND p."PageKey"='design.estimated-bom')
                  OR (r."Code"='PURCHASE_MANAGER' AND source."PageKey"='masters.vendors' AND p."PageKey"='purchase.rfq');
                """);
        server.Execute("role-before.sql", """
            CREATE TABLE public.role_before AS SELECT "Id",to_jsonb(g) body FROM advance.role_page_permissions g;
            CREATE TABLE public.page_before AS SELECT to_jsonb(p) body FROM advance.page_definitions p;
            """);
        server.Execute("role-up.sql", up);
        server.Execute("role-assert.sql", """
            DO $check$ BEGIN
              IF (SELECT count(*) FROM advance.r1_purchase_production_backup)<>8 THEN
                RAISE EXCEPTION 'Expected exactly eight changed permission rows.'; END IF;
              IF EXISTS(SELECT 1 FROM public.role_before b JOIN advance.role_page_permissions g ON g."Id"=b."Id"
                LEFT JOIN advance.r1_purchase_production_backup j ON j.permission_id=g."Id"
                JOIN advance.roles r ON r."Id"=g."RoleId"
                WHERE CASE WHEN j.permission_id IS NULL THEN b.body IS DISTINCT FROM to_jsonb(g)
                  WHEN r."Code"='PRODUCTION_MANAGER' AND (b.body->>'CanApprove')::boolean THEN
                    (b.body-'CanApprove'-'UpdatedAt'-'UpdatedBy'-'Version') IS DISTINCT FROM
                    (to_jsonb(g)-'CanApprove'-'UpdatedAt'-'UpdatedBy'-'Version')
                  ELSE (b.body-'CanView'-'CanCreate'-'CanSubmit'-'UpdatedAt'-'UpdatedBy'-'Version') IS DISTINCT FROM
                    (to_jsonb(g)-'CanView'-'CanCreate'-'CanSubmit'-'UpdatedAt'-'UpdatedBy'-'Version') END)
                OR EXISTS(SELECT 1 FROM public.role_before b LEFT JOIN advance.role_page_permissions g ON g."Id"=b."Id" WHERE g."Id" IS NULL)
              THEN RAISE EXCEPTION 'Unrelated grant or action changed.'; END IF;
              IF EXISTS(SELECT 1 FROM advance.r1_purchase_production_backup b
                JOIN advance.role_page_permissions g ON g."Id"=b.permission_id JOIN advance.roles r ON r."Id"=g."RoleId"
                WHERE b.before_row IS NULL AND EXISTS(SELECT 1 FROM jsonb_each(to_jsonb(g)) e
                  WHERE e.value='true'::jsonb AND NOT(e.key='CanView' OR (r."Code"='PURCHASE_MANAGER' AND e.key IN('CanCreate','CanSubmit')))))
              THEN RAISE EXCEPTION 'New grant contains unapproved actions.'; END IF;
              IF EXISTS((SELECT body FROM public.page_before EXCEPT SELECT to_jsonb(p) FROM advance.page_definitions p)
                UNION ALL(SELECT to_jsonb(p) FROM advance.page_definitions p EXCEPT SELECT body FROM public.page_before))
              THEN RAISE EXCEPTION 'Page definitions changed.'; END IF;
            END $check$;
            CREATE TABLE public.role_after AS SELECT "Id",to_jsonb(g) body FROM advance.role_page_permissions g;
            """);
        await using (var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options))
        {
            var permissions = new EfPagePermissionService(db);
            foreach (var page in new[] { "purchase.po", "purchase.commercial-comparisons" })
            {
                Assert.False(await permissions.HasPermissionAsync(["PRODUCTION_MANAGER"], page, "approve", default));
                Assert.True(await permissions.HasPermissionAsync(["TECHNICAL_DIRECTOR"], page, "approve", default));
                Assert.True(await permissions.HasPermissionAsync(["ACCOUNTS_MANAGER"], page, "approve", default));
            }
            foreach (var page in new[] { "purchase.requisitions", "purchase.requisition-approvals", "stores.material-issue-requests" })
                Assert.True(await permissions.HasPermissionAsync(["PRODUCTION_MANAGER"], page, "approve", default));
            foreach (var page in new[] { "design.estimated-bom", "stores.material-issues", "stores.machine-deliveries" })
                Assert.True(await permissions.HasPermissionAsync(["PRODUCTION_MANAGER"], page, "view", default));
            foreach (var page in new[] { "purchase.requisitions", "purchase.rfq", "purchase.vendor-quotations" })
            {
                foreach (var action in new[] { "view", "create", "submit" })
                    Assert.True(await permissions.HasPermissionAsync(["PURCHASE_MANAGER"], page, action, default));
                Assert.False(await permissions.HasPermissionAsync(["PURCHASE_MANAGER"], page, "approve", default));
            }
            Assert.False(await permissions.HasPermissionAsync(["PURCHASE_MANAGER"], "purchase.po", "approve", default));
            Assert.True(await permissions.HasPermissionAsync(["PURCHASE_MANAGER"], "purchase.po", "issue", default));
            Assert.False(await db.RolePagePermissions.AnyAsync(g => g.Role!.Code == "PURCHASE_MANAGER" && g.PageDefinition!.PageKey.StartsWith("accounts.")));
            var employee = await db.Employees.SingleAsync(x => x.EmployeeCode == "SESS-25");
            var company = await db.Companies.SingleAsync(x => x.Code == "SESS_PVT_LTD");
            var assignments = (await db.EmployeeRoleAssignments.Include(x => x.Role)
                .Where(x => x.EmployeeId == employee.Id && x.CompanyId == company.Id && x.EffectiveTo == null).ToListAsync())
                .ToDictionary(x => TaxWorkflowUser.AssignmentKey(x.EmployeeId, x.Role!.Code),
                    x => new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType));
            var user = new TaxWorkflowUser(employee.Id, "SESS-25", "PRODUCTION_MANAGER", assignments);
            await using var host = await PurchaseFlowHost.StartAsync(server.ConnectionString, user, useRealPagePermissions: true);
            // An authorized read proves the actor is admitted. Approval must fail at
            // the real page gate, before even looking up this synthetic PO number.
            using var read = await host.Client.GetAsync("/api/v1/purchase/purchase-orders");
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            using var denied = await host.Client.PostAsJsonAsync("/api/v1/purchase/purchase-orders/ROLE-AUDIT-PO/approve",
                new Rev869BPoApprovalActionRequest("Must be denied", 0, null, "role-audit-deny"));
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }
        server.Execute("role-edit.sql", """
            UPDATE advance.role_page_permissions g SET "CanExport"=NOT g."CanExport"
            FROM advance.roles r,advance.page_definitions p WHERE g."RoleId"=r."Id" AND g."PageDefinitionId"=p."Id"
              AND r."Code"='PRODUCTION_MANAGER' AND p."PageKey"='stores.machine-deliveries';
            """);
        server.AssertRejected("role-down-refused.sql", down, "refuses changed or removed permissions");
        server.Execute("role-restore.sql", """
            UPDATE advance.role_page_permissions g SET "CanExport"=(b.after_row->>'CanExport')::boolean
            FROM advance.r1_purchase_production_backup b WHERE b.permission_id=g."Id";
            """);
        server.Execute("role-down.sql", down);
        server.Execute("role-roundtrip.sql", """
            DO $check$ BEGIN
              IF EXISTS((SELECT "Id",body FROM public.role_before EXCEPT SELECT "Id",to_jsonb(g) FROM advance.role_page_permissions g)
                UNION ALL(SELECT "Id",to_jsonb(g) FROM advance.role_page_permissions g EXCEPT SELECT "Id",body FROM public.role_before))
              THEN RAISE EXCEPTION 'Rollback did not restore exact original grants.'; END IF;
            END $check$;
            """);
        server.Execute("role-up-again.sql", up);
        server.Execute("role-reapply.sql", """
            DO $check$ BEGIN
              IF EXISTS((SELECT "Id",body-'CreatedAt'-'UpdatedAt' FROM public.role_after EXCEPT SELECT "Id",to_jsonb(g)-'CreatedAt'-'UpdatedAt' FROM advance.role_page_permissions g)
                UNION ALL(SELECT "Id",to_jsonb(g)-'CreatedAt'-'UpdatedAt' FROM advance.role_page_permissions g EXCEPT SELECT "Id",body-'CreatedAt'-'UpdatedAt' FROM public.role_after))
              THEN RAISE EXCEPTION 'Reapply changed grant set.'; END IF;
            END $check$;
            """);
    }
}
