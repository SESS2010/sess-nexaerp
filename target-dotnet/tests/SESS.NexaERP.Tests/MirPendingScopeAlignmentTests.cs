using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Application.Tracking;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Reporting;
using SESS.NexaERP.Infrastructure.Tracking;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void MirPendingScopeRoundTripPreservesFunctionsAndRefusesChangedQueues()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        const string before = "20261002140000_R1EmailLogReadOnly";
        const string after = "20261003083000_MirPendingScopeAlignment";
        server.Execute("mir-scope-before.sql", migrator.GenerateScript("0", before));
        server.Execute("mir-scope-snapshot.sql", """
            CREATE TABLE public.mir_scope_original_functions AS
            SELECT p.oid::regprocedure::text signature,replace(pg_get_functiondef(p.oid),E'\r\n',E'\n') definition,
              p.proacl,p.proowner,p.prosecdef,p.proconfig FROM pg_proc p
              WHERE p.oid IN('advance.tracking_history(text,uuid,uuid[],text,text,uuid)'::regprocedure,
                'advance.stores_workload(text,uuid,uuid[],text,text,uuid,bigint,integer)'::regprocedure);
            CREATE TABLE public.mir_scope_original_queues AS SELECT to_jsonb(q) row
              FROM advance.tracking_queues q WHERE "Queue" IN('mir-approval','mir-unissued');
            """);
        var up = migrator.GenerateScript(before, after);
        var down = migrator.GenerateScript(after, before);
        server.Execute("mir-scope-up.sql", up);
        server.Execute("mir-scope-up-check.sql", """
            DO $check$ BEGIN
              IF (SELECT count(*) FROM advance.tracking_queues WHERE "DocType"='MIR' AND "ScopeRule"='COMPANY')<>2
                OR EXISTS(SELECT 1 FROM public.mir_scope_original_functions f JOIN pg_proc p ON p.oid=f.signature::regprocedure
                  WHERE p.proacl IS DISTINCT FROM f.proacl OR p.proowner<>f.proowner
                    OR p.prosecdef<>f.prosecdef OR p.proconfig IS DISTINCT FROM f.proconfig) THEN
                RAISE EXCEPTION 'MIR scope update changed security or failed queue alignment.';
              END IF;
            END $check$;
            UPDATE advance.tracking_queues SET "OverdueAfterDays"=2 WHERE "Queue"='mir-approval';
            """);
        server.AssertRejected("mir-scope-down-refused.sql", down, "refuses changed or removed queues/functions");
        server.Execute("mir-scope-restore-threshold.sql", "UPDATE advance.tracking_queues SET \"OverdueAfterDays\"=1 WHERE \"Queue\"='mir-approval';");
        server.Execute("mir-scope-down.sql", down);
        server.Execute("mir-scope-round-trip-check.sql", """
            DO $check$ BEGIN
              IF EXISTS(SELECT 1 FROM public.mir_scope_original_functions f JOIN pg_proc p ON p.oid=f.signature::regprocedure
                WHERE replace(pg_get_functiondef(p.oid),E'\r\n',E'\n') IS DISTINCT FROM f.definition)
                OR (SELECT jsonb_agg(row ORDER BY row->>'Queue') FROM public.mir_scope_original_queues)
                   IS DISTINCT FROM (SELECT jsonb_agg(to_jsonb(q) ORDER BY q."Queue") FROM advance.tracking_queues q
                     WHERE q."Queue" IN('mir-approval','mir-unissued')) THEN
                RAISE EXCEPTION 'MIR scope Down failed to restore exact previous contracts.';
              END IF;
            END $check$;
            """);
        server.Execute("mir-scope-up-again.sql", up);
    }

    [Fact]
    public async Task MirPendingScopeMatchesModuleAcrossOperationalScopesAndStillRequiresGrants()
    {
        var stages = new HashSet<string>();
        await RunCompletePurchaseFlow(storesWorkload: async context =>
        {
            if (context.Band != "MIR") return;
            Assert.True(stages.Add(context.Stage));
            await using var source = new NexaErpDbContext(context.Options);
            var company = await source.Companies.SingleAsync(x => x.Code == "SESS_PVT_LTD");
            var employee = await source.Employees.SingleAsync(x => x.EmployeeCode == "SESS-41");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var assignments = await source.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                .Where(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id
                    && (x.ApprovalStatus == "Approved" || x.ApprovalStatus == "SeedApproved")
                    && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today)).ToListAsync();
            var user = new StoresWorkloadUser(employee.Id, assignments.Select(x =>
                new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType)).ToArray());
            var department = await source.MaterialIssueRequests.Where(x => x.Id == context.DocumentId)
                .Select(x => x.RequestingDepartmentId).SingleAsync();
            var otherDepartment = await source.Departments.Where(x => x.Id != department).Select(x => x.Id).FirstAsync();
            await source.Database.OpenConnectionAsync();
            var connection = (NpgsqlConnection)source.Database.GetDbConnection();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await using (var setup = new NpgsqlCommand("""
                    WITH closed AS (
                      UPDATE advance.employee_operational_scopes SET "EffectiveTo"=greatest(CURRENT_DATE,"EffectiveFrom"),
                        "IsActive"=false,"UpdatedBy"='MIR_SCOPE_PROBE',"UpdatedAt"=now(),"Version"="Version"+1
                      WHERE "CompanyId"=@company AND "EmployeeId"=@employee AND "EffectiveTo" IS NULL
                      RETURNING *)
                    INSERT INTO advance.employee_operational_scopes
                    SELECT (jsonb_populate_record(NULL::advance.employee_operational_scopes,to_jsonb(s)||
                      jsonb_build_object('Id',gen_random_uuid(),'DepartmentId',@department,
                        'WarehouseId',(SELECT "Id" FROM advance.warehouses WHERE "CompanyId"=@company LIMIT 1),
                        'RackBinId',NULL,'OwnRecordsOnly',true,'AllowsPrivilegedCrossScope',false,
                        'EffectiveFrom',CURRENT_DATE,'EffectiveTo',NULL,'IsActive',true,'Version',0,
                        'CreatedAt',now(),'CreatedBy','MIR_SCOPE_PROBE','UpdatedAt',NULL,'UpdatedBy',NULL,
                        'Remarks','Rollback-only MIR scope witness'))).*
                    FROM closed s ORDER BY s."Id" LIMIT 1;
                    SET LOCAL ROLE nexa_erp_runtime;
                    """, connection, transaction))
                {
                    setup.Parameters.AddWithValue("company", company.Id);
                    setup.Parameters.AddWithValue("employee", employee.Id);
                    setup.Parameters.AddWithValue("department", otherDepartment);
                    await setup.ExecuteNonQueryAsync();
                }
                await using var runtime = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(connection).Options);
                await runtime.Database.UseTransactionAsync(transaction);
                var stores = new EfStoresWorkloadService(runtime, user, WorkloadCalendar());
                var tracking = new EfTrackingService(runtime, user, WorkloadCalendar());
                var queue = context.Stage == "MIR_SUBMITTED" ? "mir-approval" : "mir-unissued";
                var workload = await stores.GetAsync(new(queue, context.DocumentId), default);
                var pending = await tracking.PendingAsync(new("MIR", queue, false, false, 1, 25), default);
                if (context.Stage == "MIR_ISSUED")
                {
                    Assert.Empty(workload.Rows);
                    Assert.DoesNotContain(pending.Items, x => x.DocumentId == context.DocumentId);
                }
                else
                {
                    Assert.Equal(context.DocumentId, Assert.Single(workload.Rows).DocumentId);
                    Assert.Equal(context.DocumentId, Assert.Single(pending.Items).DocumentId);
                }
                Assert.NotNull(await tracking.HistoryAsync("MIR", context.DocumentId, default));
                user.OrganizationId = "SESS_PROPRIETORSHIP";
                await Assert.ThrowsAsync<ReportAccessDeniedException>(() => tracking.PendingAsync(new("MIR", queue, false, false, 1, 25), default));
                user.OrganizationId = "SESS_PVT_LTD";
                await using (var deny = new NpgsqlCommand("""
                    SET LOCAL ROLE NONE;
                    UPDATE advance.role_page_permissions rp SET "CanView"=false,"HasFullControl"=false
                      FROM advance.page_definitions p WHERE p."Id"=rp."PageDefinitionId" AND rp."RoleId"=ANY(@roles)
                      AND p."PageKey"='stores.material-issue-requests';
                    UPDATE advance.employee_page_permissions ep SET "CanView"=false
                      FROM advance.page_definitions p WHERE p."Id"=ep."PageDefinitionId" AND ep."CompanyId"=@company
                      AND ep."EmployeeId"=@employee AND p."PageKey"='stores.material-issue-requests';
                    SET LOCAL ROLE nexa_erp_runtime;
                    """, connection, transaction))
                {
                    deny.Parameters.AddWithValue("roles", assignments.Select(x => x.RoleId).Distinct().ToArray());
                    deny.Parameters.AddWithValue("company", company.Id);
                    deny.Parameters.AddWithValue("employee", employee.Id);
                    await deny.ExecuteNonQueryAsync();
                }
                var denied = await stores.GetAsync(new(queue, context.DocumentId), default);
                Assert.Equal("ACCESS_DENIED", Assert.Single(denied.Tiles, x => x.Key == queue).State);
                Assert.Empty(denied.Rows);
                Assert.Empty((await tracking.PendingAsync(new("MIR", queue, false, false, 1, 25), default)).Items);
                Assert.Null(await tracking.HistoryAsync("MIR", context.DocumentId, default));
            }
            finally { await transaction.RollbackAsync(); }
        });
        Assert.Equal(new[] { "MIR_APPROVED", "MIR_ISSUED", "MIR_SUBMITTED" }, stages.Order());
    }
}
