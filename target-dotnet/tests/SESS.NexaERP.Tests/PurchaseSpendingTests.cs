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
    public async Task PurchaseSpendingMigrationGuardsAuthorityAndReconcilesRuntimeExecution()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        Assert.False(model.Database.HasPendingModelChanges());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260914010000_PurchaseWorkload";
        const string current = "20260914020000_PurchaseSpending";
        server.Execute("spending-predecessor.sql", migrator.GenerateScript("0", previous));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "spending-runtime-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("spending-up.sql", migrator.GenerateScript(previous, current));
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            server.Execute("spending-guard.sql", PurchaseSpendingMigrationSql.Guard(true));
        }
        server.Execute("spending-bad-grant.sql",
            "GRANT EXECUTE ON FUNCTION " + PurchaseSpendingMigrationSql.Signature + " TO nexa_erp_bootstrap;");
        Assert.Equal(1, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("spending-grant-restored.sql", PurchaseSpendingMigrationSql.Guard(true));
        await using var owner = new NpgsqlConnection(server.ConnectionString);
        await owner.OpenAsync();
        await using (var change = new NpgsqlCommand("ALTER FUNCTION " + PurchaseSpendingMigrationSql.Signature + " SECURITY INVOKER", owner))
            await change.ExecuteNonQueryAsync();
        await using (var guard = new NpgsqlCommand(PurchaseSpendingMigrationSql.Guard(true), owner))
            Assert.Equal("P0001", (await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteNonQueryAsync())).SqlState);
        await using (var restore = new NpgsqlCommand("ALTER FUNCTION " + PurchaseSpendingMigrationSql.Signature + " SECURITY DEFINER", owner))
            await restore.ExecuteNonQueryAsync();
        server.Execute("spending-down.sql", migrator.GenerateScript(current, previous));
        server.Execute("spending-reapply.sql", migrator.GenerateScript(previous, current));
        server.Execute("spending-final-guard.sql", PurchaseSpendingMigrationSql.Guard(true));
    }


    [Fact]
    public async Task PurchaseSpendingReconcilesAcceptedAndReversedBillsFromRuntimeFlow()
    {
        await RunCompletePurchaseFlow(mixedRun: async context =>
        {
            await using var source = new NexaErpDbContext(context.Options);
            var company = await source.Companies.SingleAsync(x => x.Code == "SESS_PVT_LTD");
            var employee = await source.Employees.SingleAsync(x => x.EmployeeCode == "SESS-15");
            var assignments = await source.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                .Where(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id && x.EffectiveTo == null).ToListAsync();
            var user = new WorkloadWitnessUser(employee.Id,
                assignments.Select(x => new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType)).ToArray());
            await using var runtime = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql(context.RuntimeConnection).Options);
            var service = new EfPurchaseSpendingService(runtime, user, WorkloadCalendar());
            async Task<string> Counts()
            {
                await source.Database.OpenConnectionAsync();
                await using var command = new NpgsqlCommand("""
                    SELECT jsonb_build_object('audits',(SELECT count(*) FROM advance.audit_logs),
                      'requests',(SELECT count(*) FROM advance.command_requests),
                      'receipts',(SELECT count(*) FROM advance.command_receipts),
                      'movements',(SELECT count(*) FROM advance.stock_movements),
                      'bills',(SELECT count(*) FROM advance.vendor_bills))::text
                    """, (NpgsqlConnection)source.Database.GetDbConnection());
                return (string)(await command.ExecuteScalarAsync())!;
            }
            var before = await Counts();
            var page = await ObserveSingleReportCommand(() => service.GetAsync(new(), default));
            Assert.Equal(5, page.TotalRows);
            Assert.Equal(5, page.Rows.Count);
            Assert.Single(page.Rows, x => x.Event == "REVERSED");
            Assert.Equal(3m, page.Rows.Sum(x => x.Quantity));
            Assert.Equal(128620.01m, page.Rows.Sum(x => x.MaterialValue));
            Assert.Equal(12m, page.Rows.Sum(x => x.AllocatedCharges));
            Assert.Equal(128632.01m, page.Rows.Sum(x => x.Amount));
            Assert.All(page.Rows, x => Assert.Equal("INR", x.Currency));
            Assert.Equal(3, page.Periods.Count);
            Assert.All(page.Periods, period =>
            {
                var total = Assert.Single(period.Amounts);
                Assert.Equal(128632.01m, total.Amount);
                Assert.Equal(3, total.PoCount);
                Assert.Equal(4, total.BillCount);
                Assert.Equal(12m, total.AllocatedCharges);
            });
            var localDate = ReportCalendarOptions.DateInZone(page.GeneratedAt, TimeZoneInfo.FindSystemTimeZoneById(page.TimeZone));
            var month = new DateOnly(localDate.Year, localDate.Month, 1);
            var fiscalStart = new DateOnly(localDate.Month >= 4 ? localDate.Year : localDate.Year - 1, 4, 1);
            Assert.Equal(fiscalStart, Assert.Single(page.Periods, x => x.Key == "financial-year").FromDate);
            Assert.Equal(new DateOnly(localDate.Year, (localDate.Month - 1) / 3 * 3 + 1, 1),
                Assert.Single(page.Periods, x => x.Key == "quarter").FromDate);
            Assert.Equal(12, page.MonthlyTrend.Count);
            Assert.Equal(month.AddMonths(-11), page.MonthlyTrend[0].FromDate);
            Assert.Equal(month, page.MonthlyTrend[^1].FromDate);
            Assert.Equal(128632.01m, page.MonthlyTrend.Sum(x => x.Amounts.Sum(a => a.Amount)));
            Assert.All(page.MonthlyTrend.Take(11), x => Assert.Empty(x.Amounts));
            Assert.Equal(128632.01m, page.TopVendors.Sum(x => x.Amount));
            Assert.Equal(128632.01m, page.Categories.Sum(x => x.Amount));
            var sourceLines = await source.VendorBillLines.AsNoTracking().Select(x => x.Id).ToArrayAsync();
            Assert.All(page.Rows, x => Assert.Contains(x.BillLineId, sourceLines));
            var receipts = await source.GoodsReceiptLines.AsNoTracking().ToListAsync();
            Assert.All(page.Rows, x => Assert.Contains(receipts,
                receipt => receipt.ItemId == x.ItemId && receipt.ItemCategoryIdSnapshot == x.CategoryId &&
                    receipt.ItemCategoryCodeSnapshot == x.CategoryCode));
            foreach (var vendor in page.TopVendors)
            {
                var drill = await ObserveSingleReportCommand(() => service.GetAsync(new(VendorId: vendor.Id, Currency: vendor.Currency), default));
                Assert.Equal(vendor.Amount, drill.Rows.Sum(x => x.Amount));
                Assert.All(drill.Rows, x => Assert.Equal(vendor.Id, x.VendorId));
            }
            foreach (var category in page.Categories)
            {
                var drill = await ObserveSingleReportCommand(() => service.GetAsync(new(CategoryId: category.Id, Currency: category.Currency), default));
                Assert.Equal(category.Amount, drill.Rows.Sum(x => x.Amount));
            }
            var reversed = Assert.Single(page.Rows, x => x.Event == "REVERSED");
            var billEvents = await service.GetAsync(new(BillId: reversed.BillId), default);
            Assert.Equal(2, billEvents.TotalRows);
            Assert.Equal(0m, billEvents.Rows.Sum(x => x.Amount));
            var first = await service.GetAsync(new(PageSize: 1), default);
            var second = await service.GetAsync(new(Page: 2, PageSize: 1), default);
            Assert.Equal(5, first.TotalRows);
            Assert.NotEqual(Assert.Single(first.Rows), Assert.Single(second.Rows));
            Assert.Equal(0, (await service.GetAsync(new("month", month.AddMonths(-1)), default)).TotalRows);
            Assert.Equal(0, (await service.GetAsync(new(Currency: "USD"), default)).TotalRows);
            await Assert.ThrowsAsync<ReportRequestException>(() => service.GetAsync(new("month", month.AddMonths(-12)), default));
            await Assert.ThrowsAsync<ReportRequestException>(() => service.GetAsync(new("month", month.AddMonths(1)), default));
            await Assert.ThrowsAsync<ReportRequestException>(() => service.GetAsync(new("quarter", month), default));
            user.OrganizationId = "SESS_PROPRIETORSHIP";
            await Assert.ThrowsAsync<ReportAccessDeniedException>(() => service.GetAsync(new(), default));
            user.OrganizationId = "SESS_PVT_LTD";
            var emptyRoles = new EfPurchaseSpendingService(runtime, new WorkloadWitnessUser(employee.Id, []), WorkloadCalendar());
            await Assert.ThrowsAsync<ReportAccessDeniedException>(() => emptyRoles.GetAsync(new(), default));
            var tableRead = await Assert.ThrowsAsync<PostgresException>(() =>
                runtime.Database.ExecuteSqlRawAsync("SELECT * FROM advance.vendor_bill_lines LIMIT 1"));
            Assert.Equal("42501", tableRead.SqlState);
            await AssertSpendingLiveAccess(source, user, service, company.Id, employee.Id,
                assignments.Select(x => x.RoleId).Distinct().ToArray());
            Assert.Equal(before, await Counts());

            var actor = await BankAdviceActor(context.Options);
            await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
            using (var denied = await host.Client.GetAsync("/api/v1/dashboards/purchase/spending"))
            {
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
                Assert.Contains("DASHBOARD_ACCESS_DENIED", await denied.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            }
            var subject = await source.EmployeeIdentityMappings.Where(x => x.EmployeeId == employee.Id &&
                x.IsActive && x.CompanyId == company.Id).Select(x => x.Subject).SingleAsync();
            actor.Set(employee.Id, subject, "PURCHASE_MANAGER", assignments.Select(x => x.Role!.Code).Distinct().ToArray());
            var response = await Get<PurchaseSpendingPage>(host.Client, "/api/v1/dashboards/purchase/spending?period=month");
            Assert.Equal(128632.01m, response.Rows.Sum(x => x.Amount));
            using var invalid = await host.Client.GetAsync("/api/v1/dashboards/purchase/spending?period=unknown");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Contains("DASHBOARD_REQUEST_INVALID", await invalid.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item29");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "purchase-spending-runtime.json"),
                JsonSerializer.Serialize(new { Before = before, After = await Counts(), Page = page,
                    ScopeAndPermissionProbesPassed = true, HttpReadPassed = true },
                    new JsonSerializerOptions { WriteIndented = true }));
        });
    }

    private static async Task AssertSpendingLiveAccess(NexaErpDbContext source, WorkloadWitnessUser user,
        EfPurchaseSpendingService service, Guid company, Guid employee, Guid[] roles)
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
                var probe = new EfPurchaseSpendingService(probeDb, user, WorkloadCalendar());
                if (denied) await Assert.ThrowsAsync<ReportAccessDeniedException>(() => probe.GetAsync(new(), default));
                else
                {
                    var hidden = await ObserveSingleReportCommand(() => probe.GetAsync(new(), default));
                    Assert.Equal(0, hidden.TotalRows);
                    Assert.All(hidden.Periods, x => Assert.Empty(x.Amounts));
                    Assert.Empty(hidden.TopVendors);
                    Assert.Empty(hidden.Categories);
                    Assert.All(hidden.MonthlyTrend, x => Assert.Empty(x.Amounts));
                }
            }
            finally { await transaction.RollbackAsync(); }
            Assert.Equal(5, (await service.GetAsync(new(), default)).TotalRows);
        }
        await Probe("""
            UPDATE advance.employee_operational_scopes SET "EffectiveTo"=greatest(CURRENT_DATE,"EffectiveFrom"),
              "IsActive"=false,"UpdatedBy"='SPENDING_PROBE',"UpdatedAt"=now(),"Version"="Version"+1
            WHERE "CompanyId"=@company AND "EmployeeId"=@employee AND "EffectiveTo" IS NULL;
            INSERT INTO advance.employee_operational_scopes
            SELECT (jsonb_populate_record(NULL::advance.employee_operational_scopes,to_jsonb(s)||
              jsonb_build_object('Id',gen_random_uuid(),'DepartmentId',@department,
                'RackBinId',NULL,'OwnRecordsOnly',false,'AllowsPrivilegedCrossScope',false,
                'EffectiveFrom',CURRENT_DATE,'EffectiveTo',NULL,'IsActive',true,'Version',0,
                'CreatedAt',now(),'CreatedBy','SPENDING_PROBE','UpdatedAt',NULL,'UpdatedBy',NULL,
                'Remarks','Owned rollback-only spending scope probe'))).*
            FROM advance.employee_operational_scopes s WHERE s."Id"=@scope;
            """, false);
        await Probe("""
            UPDATE advance.company_role_activations SET "IsEnabled"=false
            WHERE "CompanyId"=@company AND "RoleId"=ANY(@roles);
            """, true);
        await Probe("""
            UPDATE advance.role_page_permissions rp SET "CanViewCommercialValues"=false,"HasFullControl"=false
            FROM advance.page_definitions p WHERE p."Id"=rp."PageDefinitionId"
              AND rp."RoleId"=ANY(@roles) AND p."PageKey"='dashboards.purchase-spending';
            """, true);
        await Probe("""
            UPDATE advance.role_page_permissions rp SET "CanView"=false,"HasFullControl"=false
            FROM advance.page_definitions p WHERE p."Id"=rp."PageDefinitionId"
              AND rp."RoleId"=ANY(@roles) AND p."PageKey"='dashboards.purchase-spending';
            UPDATE advance.employee_page_permissions ep SET "CanView"=false
            FROM advance.page_definitions p WHERE p."Id"=ep."PageDefinitionId"
              AND ep."CompanyId"=@company AND ep."EmployeeId"=@employee AND p."PageKey"='dashboards.purchase-spending';
            """, true);
    }
}
