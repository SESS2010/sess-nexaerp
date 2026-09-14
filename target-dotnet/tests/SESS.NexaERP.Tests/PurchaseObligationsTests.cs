using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task PurchaseObligationsMigrationGuardsAuthorityAndReconcilesRuntimeExecution()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        Assert.False(model.Database.HasPendingModelChanges());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260914030000_ReceiptQuantityAcrossPoRevisions";
        const string current = "20260914040000_PurchaseObligations";
        server.Execute("obligations-predecessor.sql", migrator.GenerateScript("0", previous));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "obligations-runtime-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("obligations-up.sql", migrator.GenerateScript(previous, current));
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            server.Execute("obligations-guard.sql", PurchaseObligationsMigrationSql.Guard(true));
        }
        server.Execute("obligations-bad-grant.sql",
            "GRANT EXECUTE ON FUNCTION " + PurchaseObligationsMigrationSql.Signature + " TO nexa_erp_bootstrap;");
        Assert.Equal(1, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("obligations-grant-restored.sql", PurchaseObligationsMigrationSql.Guard(true));
        await using var owner = new NpgsqlConnection(server.ConnectionString);
        await owner.OpenAsync();
        await using (var change = new NpgsqlCommand("ALTER FUNCTION " + PurchaseObligationsMigrationSql.Signature + " SECURITY INVOKER", owner))
            await change.ExecuteNonQueryAsync();
        await using (var guard = new NpgsqlCommand(PurchaseObligationsMigrationSql.Guard(true), owner))
            Assert.Equal("P0001", (await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteNonQueryAsync())).SqlState);
        await using (var restore = new NpgsqlCommand("ALTER FUNCTION " + PurchaseObligationsMigrationSql.Signature + " SECURITY DEFINER", owner))
            await restore.ExecuteNonQueryAsync();
        server.Execute("obligations-down.sql", migrator.GenerateScript(current, previous));
        server.Execute("obligations-reapply.sql", migrator.GenerateScript(previous, current));
        server.Execute("obligations-final-guard.sql", PurchaseObligationsMigrationSql.Guard(true));
    }



    private sealed record PurchaseObligationWitnessContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection, string Stage);

    [Fact]
    public async Task PurchaseObligationsReconcileReceiptsAdvancesAcceptanceAndReversal()
    {
        var observations = new List<object>();
        var stages = new HashSet<string>();
        await RunCompletePurchaseFlow(obligations: async context =>
        {
            Assert.True(stages.Add(context.Stage), "Each obligation stage must be observed once.");
            (long Grns, decimal Value, long Advances, decimal AdvanceValue) expected = context.Stage switch
            {
                "RECEIVED" => (3L,109000.01m,0L,0m),
                "ADVANCES" => (3L,109000.01m,2L,250m),
                "REVERSIBLE_ADVANCE" => (3L,109000.01m,3L,260m),
                "ADVANCE_REVERSED" => (3L,109000.01m,2L,250m),
                "BILL_ACCEPTED_1" => (2L,104000.01m,0L,0m),
                "BILL_REVERSED_1" => (3L,109000.01m,2L,250m),
                "BILL_REENTERED_1" => (2L,104000.01m,0L,0m),
                "BILL_ACCEPTED_2" => (1L,4000m,0L,0m),
                "FINAL" => (0L,0m,0L,0m),
                _ => throw new InvalidOperationException(context.Stage)
            };
            await using var source = new NexaErpDbContext(context.Options);
            var company = await source.Companies.SingleAsync(x => x.Code == "SESS_PVT_LTD");
            var employee = await source.Employees.SingleAsync(x => x.EmployeeCode == "SESS-15");
            var assignments = await source.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                .Where(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id && x.EffectiveTo == null).ToListAsync();
            var user = new WorkloadWitnessUser(employee.Id,
                assignments.Select(x => new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)).ToArray());
            await using var runtime = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql(context.RuntimeConnection).Options);
            var service = new EfPurchaseObligationsService(runtime,user,WorkloadCalendar());
            async Task<string> Counts()
            {
                await source.Database.OpenConnectionAsync();
                await using var command = new NpgsqlCommand("""
                    SELECT jsonb_build_object('audits',(SELECT count(*) FROM advance.audit_logs),
                      'requests',(SELECT count(*) FROM advance.command_requests),
                      'receipts',(SELECT count(*) FROM advance.command_receipts),
                      'movements',(SELECT count(*) FROM advance.stock_movements),
                      'advances',(SELECT count(*) FROM advance.vendor_advances),
                      'adjustments',(SELECT count(*) FROM advance.vendor_advance_adjustments),
                      'restorations',(SELECT count(*) FROM advance.vendor_advance_adjustment_restorations),
                      'bills',(SELECT count(*) FROM advance.vendor_bills))::text
                    """,(NpgsqlConnection)source.Database.GetDbConnection());
                return (string)(await command.ExecuteScalarAsync())!;
            }
            var before = await Counts();
            var page = await ObserveSingleReportCommand(() => service.GetAsync(new(),default));
            Assert.Equal(expected.Grns+expected.Advances,page.TotalRows);
            Assert.Equal(2,page.Tiles.Count);
            var grni = Assert.Single(page.Tiles,x => x.Key=="grni");
            var advances = Assert.Single(page.Tiles,x => x.Key=="vendor-advances");
            Assert.Equal(expected.Grns,grni.Count);
            Assert.Equal(expected.Advances,advances.Count);
            Assert.Equal(expected.Value,grni.Amounts.Sum(x=>x.Value));
            Assert.Equal(expected.AdvanceValue,advances.Amounts.Sum(x=>x.Value));
            Assert.Equal(expected.Value,page.Rows.Where(x=>x.Queue=="grni").Sum(x=>x.Value));
            Assert.Equal(expected.AdvanceValue,page.Rows.Where(x=>x.Queue=="vendor-advances").Sum(x=>x.Value));
            Assert.All(page.Rows,x=>Assert.Equal("INR",x.Currency));
            Assert.All(page.Rows.Where(x=>x.Queue=="grni"),x=>
            {
                Assert.NotNull(x.LineId); Assert.NotNull(x.ItemId);
                Assert.Equal(1m,x.Quantity); Assert.Null(x.OriginalAmount);
            });
            Assert.All(page.Rows.Where(x=>x.Queue=="vendor-advances"),x=>
            {
                Assert.Null(x.LineId); Assert.Null(x.ItemId); Assert.Null(x.Quantity);
                Assert.Equal(x.OriginalAmount-x.AdjustedAmount,x.Value);
            });
            Assert.Equal(expected.Value,page.Vendors.Where(x=>x.Queue=="grni").Sum(x=>x.Value));
            Assert.Equal(expected.AdvanceValue,page.Vendors.Where(x=>x.Queue=="vendor-advances").Sum(x=>x.Value));
            if(context.Stage=="ADVANCES")
            {
                var localDate=ReportCalendarOptions.DateInZone(page.GeneratedAt,TimeZoneInfo.FindSystemTimeZoneById(page.TimeZone));
                Assert.Equal(localDate.DayNumber-DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2).DayNumber,advances.OldestAgeDays);
                var vendor = Assert.Single(page.Rows.Select(x=>x.VendorId).Distinct());
                var filtered = await service.GetAsync(new(Queue:"vendor-advances",VendorId:vendor,Currency:" inr "),default);
                Assert.Equal(2,filtered.TotalRows);
                Assert.Equal(250m,filtered.Rows.Sum(x=>x.Value));
                var single = await service.GetAsync(new(DocumentId:filtered.Rows[0].DocumentId),default);
                Assert.Single(single.Rows);
                var paged = await service.GetAsync(new(Page:2,PageSize:2),default);
                Assert.Equal(5,paged.TotalRows); Assert.Equal(2,paged.Rows.Count);
                Assert.Equal(page.Rows.Skip(2).Take(2).Select(x=>x.DocumentId),paged.Rows.Select(x=>x.DocumentId));
                Assert.Equal(0,(await service.GetAsync(new(Currency:"USD"),default)).TotalRows);
                Assert.Equal(0,(await service.GetAsync(new(VendorId:Guid.NewGuid()),default)).TotalRows);
                await AssertObligationsLiveAccess(source,user,service,company.Id,employee.Id,
                    assignments.Select(x=>x.RoleId).Distinct().ToArray());
                user.OrganizationId="SESS_PROPRIETORSHIP";
                await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>service.GetAsync(new(),default));
                user.OrganizationId="SESS_PVT_LTD";
                var emptyRoles=new EfPurchaseObligationsService(runtime,new WorkloadWitnessUser(employee.Id,[]),WorkloadCalendar());
                await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>emptyRoles.GetAsync(new(),default));
                var tableRead=await Assert.ThrowsAsync<PostgresException>(()=>
                    runtime.Database.ExecuteSqlRawAsync("SELECT * FROM advance.vendor_advances LIMIT 1"));
                Assert.Equal("42501",tableRead.SqlState);
                var actor=await BankAdviceActor(context.Options);
                await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,actor,true,true);
                using(var denied=await host.Client.GetAsync("/api/v1/dashboards/purchase/obligations"))
                    Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
                var subject=await source.EmployeeIdentityMappings.Where(x=>x.EmployeeId==employee.Id&&
                    x.CompanyId==company.Id&&x.IsActive).Select(x=>x.Subject).SingleAsync();
                actor.Set(employee.Id,subject,"PURCHASE_MANAGER",assignments.Select(x=>x.Role!.Code).Distinct().ToArray());
                var http=await Get<PurchaseObligationsPage>(host.Client,"/api/v1/dashboards/purchase/obligations?queue=vendor-advances");
                Assert.Equal(250m,http.Rows.Sum(x=>x.Value));
                using var invalid=await host.Client.GetAsync("/api/v1/dashboards/purchase/obligations?queue=unknown");
                Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
            }
            Assert.Equal(before,await Counts());
            observations.Add(new{context.Stage,Before=before,After=await Counts(),Page=page});
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item29");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"purchase-obligations-runtime.json"),
                JsonSerializer.Serialize(observations,new JsonSerializerOptions{WriteIndented=true}));
        });
        Assert.Equal(9,stages.Count);
    }
    private static async Task AssertObligationsLiveAccess(NexaErpDbContext source, WorkloadWitnessUser user,
        EfPurchaseObligationsService service, Guid company, Guid employee, Guid[] roles)
    {
        await source.Database.OpenConnectionAsync();
        var connection = (NpgsqlConnection)source.Database.GetDbConnection();
        var scope = await source.EmployeeOperationalScopes.AsNoTracking()
            .Where(x => x.CompanyId == company && x.EmployeeId == employee && x.IsActive && x.EffectiveTo == null)
            .Select(x => x.Id).FirstAsync();
        var usedDepartments = await source.PurchaseOrders.Where(x => x.CompanyId == company)
            .Select(x => x.RequestingDepartmentId).ToArrayAsync();
        var otherDepartment = await source.Departments.Where(x => !usedDepartments.Contains(x.Id)).Select(x => x.Id).FirstAsync();
        async Task Probe(string sql, bool denied)
        {
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await using (var setup = new NpgsqlCommand(sql, connection, transaction))
                {
                    setup.Parameters.AddWithValue("company", company);
                    setup.Parameters.AddWithValue("employee", employee);
                    setup.Parameters.AddWithValue("roles", roles);
                    setup.Parameters.AddWithValue("scope", scope);
                    setup.Parameters.AddWithValue("department", otherDepartment);
                    await setup.ExecuteNonQueryAsync();
                }
                await using (var role = new NpgsqlCommand("SET LOCAL ROLE nexa_erp_runtime", connection, transaction))
                    await role.ExecuteNonQueryAsync();
                await using var probeDb = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(connection).Options);
                await probeDb.Database.UseTransactionAsync(transaction);
                var probe = new EfPurchaseObligationsService(probeDb, user, WorkloadCalendar());
                if (denied) await Assert.ThrowsAsync<ReportAccessDeniedException>(() => probe.GetAsync(new(), default));
                else
                {
                    var hidden = await ObserveSingleReportCommand(() => probe.GetAsync(new(), default));
                    Assert.Equal(0, hidden.TotalRows);
                    Assert.All(hidden.Tiles, x => Assert.Empty(x.Amounts));
                    Assert.Empty(hidden.Vendors);


                }
            }
            finally { await transaction.RollbackAsync(); }
            Assert.Equal(5, (await service.GetAsync(new(), default)).TotalRows);
        }
        await Probe("""
            UPDATE advance.employee_operational_scopes SET "EffectiveTo"=greatest(CURRENT_DATE,"EffectiveFrom"),
              "IsActive"=false,"UpdatedBy"='OBLIGATIONS_PROBE',"UpdatedAt"=now(),"Version"="Version"+1
            WHERE "CompanyId"=@company AND "EmployeeId"=@employee AND "EffectiveTo" IS NULL;
            INSERT INTO advance.employee_operational_scopes
            SELECT (jsonb_populate_record(NULL::advance.employee_operational_scopes,to_jsonb(s)||
              jsonb_build_object('Id',gen_random_uuid(),'DepartmentId',@department,
                'RackBinId',NULL,'OwnRecordsOnly',false,'AllowsPrivilegedCrossScope',false,
                'EffectiveFrom',CURRENT_DATE,'EffectiveTo',NULL,'IsActive',true,'Version',0,
                'CreatedAt',now(),'CreatedBy','OBLIGATIONS_PROBE','UpdatedAt',NULL,'UpdatedBy',NULL,
                'Remarks','Owned rollback-only obligations scope probe'))).*
            FROM advance.employee_operational_scopes s WHERE s."Id"=@scope;
            """, false);
        await Probe("""
            UPDATE advance.company_role_activations SET "IsEnabled"=false
            WHERE "CompanyId"=@company AND "RoleId"=ANY(@roles);
            """, true);
        await Probe("""
            UPDATE advance.role_page_permissions rp SET "CanViewCommercialValues"=false,"HasFullControl"=false
            FROM advance.page_definitions p WHERE p."Id"=rp."PageDefinitionId"
              AND rp."RoleId"=ANY(@roles) AND p."PageKey"='dashboards.purchase-obligations';
            """, true);
        await Probe("""
            UPDATE advance.role_page_permissions rp SET "CanView"=false,"HasFullControl"=false
            FROM advance.page_definitions p WHERE p."Id"=rp."PageDefinitionId"
              AND rp."RoleId"=ANY(@roles) AND p."PageKey"='dashboards.purchase-obligations';
            UPDATE advance.employee_page_permissions ep SET "CanView"=false
            FROM advance.page_definitions p WHERE p."Id"=ep."PageDefinitionId"
              AND ep."CompanyId"=@company AND ep."EmployeeId"=@employee AND p."PageKey"='dashboards.purchase-obligations';
            """, true);
    }
}
