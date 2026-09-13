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
    private sealed record PurchaseWorkloadWitnessContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection, string Stage, Guid DocumentId, string Band);

    private sealed class WorkloadWitnessUser(Guid employee, IReadOnlyList<EffectiveRoleAssignment> assignments) : ICurrentUser
    {
        public string LoginId => "purchase-workload-witness";
        public string RoleCode => "PURCHASE_MANAGER";
        public string? OrganizationId { get; set; } = "SESS_PVT_LTD";
        public bool IsAuthenticated => true;
        public Guid? EmployeeId => employee;
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments => assignments;
    }

    private static IOptions<ReportCalendarOptions> WorkloadCalendar()
    {
        var calendar = new ReportCalendarOptions();
        calendar.CompanyTimeZones.Add("SESS_PVT_LTD", "Asia/Kolkata");
        return Options.Create(calendar);
    }

    [Fact]
    public async Task PurchaseWorkloadMigrationGuardsAuthorityAndReconcilesRuntimeExecution()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        Assert.False(model.Database.HasPendingModelChanges());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260913100000_VendorPoCashCap";
        const string current = "20260914010000_PurchaseWorkload";
        server.Execute("workload-predecessor.sql", migrator.GenerateScript("0", previous));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "workload-runtime-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("workload-up.sql", migrator.GenerateScript(previous, current));
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            server.Execute("workload-guard.sql", PurchaseWorkloadMigrationSql.Guard(true));
        }
        server.Execute("workload-bad-grant.sql",
            "GRANT EXECUTE ON FUNCTION " + PurchaseWorkloadMigrationSql.Signature + " TO nexa_erp_bootstrap;");
        Assert.Equal(1, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("workload-grant-restored.sql", PurchaseWorkloadMigrationSql.Guard(true));
        await using var owner = new NpgsqlConnection(server.ConnectionString);
        await owner.OpenAsync();
        await using (var change = new NpgsqlCommand("ALTER FUNCTION " + PurchaseWorkloadMigrationSql.Signature + " SECURITY INVOKER", owner))
            await change.ExecuteNonQueryAsync();
        await using (var guard = new NpgsqlCommand(PurchaseWorkloadMigrationSql.Guard(true), owner))
            Assert.Equal("P0001", (await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteNonQueryAsync())).SqlState);
        await using (var restore = new NpgsqlCommand("ALTER FUNCTION " + PurchaseWorkloadMigrationSql.Signature + " SECURITY DEFINER", owner))
            await restore.ExecuteNonQueryAsync();
        server.Execute("workload-down.sql", migrator.GenerateScript(current, previous));
        server.Execute("workload-reapply.sql", migrator.GenerateScript(previous, current));
        server.Execute("workload-final-guard.sql", PurchaseWorkloadMigrationSql.Guard(true));
    }

    [Fact]
    public async Task PurchaseWorkloadReconcilesEightRuntimeStatesAcrossAllApprovalBands()
    {
        var observations = new List<object>();
        var seen = new HashSet<string>();
        await RunCompletePurchaseFlow(workload: async context =>
        {
            var queue = context.Stage switch
            {
                "PR_SUBMITTED" => "pr-department-verification",
                "PR_APPROVAL" => "pr-approval",
                "PR_STOCK_CHECK" => "pr-stock-check",
                "RFQ_NO_QUOTATION" => "rfq-no-quotation",
                "QUOTATION_VERIFY" => "quotation-technical-verification",
                "COMPARISON_DRAFT" or "COMPARISON_APPROVAL" => "comparison-decision",
                "PO_APPROVED" => "po-approved-unissued",
                _ => throw new InvalidOperationException(context.Stage)
            };
            var expectedCount = context.Stage == "QUOTATION_VERIFY" ? 2 : 1;
            var expectedRoute = context.Band switch { "LOW" => "DEPARTMENT_ONLY", "TD" => "DEPARTMENT_THEN_TD", _ => "DEPARTMENT_THEN_MD" };
            var expectedPr = context.Band switch { "LOW" => 4999.99m, "TD" => 5000m, _ => 100000.01m };
            var expectedPo = context.Band switch { "LOW" => 4720m, "TD" => 5900m, _ => 118000.01m };
            await using var source = new NexaErpDbContext(context.Options);
            var company = await source.Companies.SingleAsync(x => x.Code == "SESS_PVT_LTD");
            var employee = await source.Employees.SingleAsync(x => x.EmployeeCode == "SESS-15");
            var assignments = await source.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                .Where(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id &&
                    x.EffectiveTo == null).ToListAsync();
            Assert.NotEmpty(assignments);
            var user = new WorkloadWitnessUser(employee.Id,
                assignments.Select(x => new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType)).ToArray());
            await using var runtime = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql(context.RuntimeConnection).Options);
            var service = new EfPurchaseWorkloadService(runtime, user, WorkloadCalendar());
            async Task<string> Counts()
            {
                await source.Database.OpenConnectionAsync();
                await using var command = new NpgsqlCommand("""
                    SELECT jsonb_build_object('audits',(SELECT count(*) FROM advance.audit_logs),
                      'requests',(SELECT count(*) FROM advance.command_requests),
                      'receipts',(SELECT count(*) FROM advance.command_receipts),
                      'movements',(SELECT count(*) FROM advance.stock_movements))::text
                    """, (NpgsqlConnection)source.Database.GetDbConnection());
                return (string)(await command.ExecuteScalarAsync())!;
            }
            var before = await Counts();
            var result = await ObserveSingleReportCommand(() => service.GetAsync(new(queue, PageSize: 1000), default));
            observations.Add(new { context.Band, context.Stage, context.DocumentId, Assignments = assignments.Select(x => new { x.Id, Role = x.Role!.Code, x.AssignmentType }), Result = result });
            var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item29");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "purchase-workload-states.json"),
                JsonSerializer.Serialize(observations, new JsonSerializerOptions { WriteIndented = true }));
            Assert.Equal(expectedCount, result.TotalRows);
            Assert.Equal(expectedCount, result.Rows.Count);
            var tile = Assert.Single(result.Tiles, x => x.Key == queue);
            Assert.Equal("READY", tile.State);
            Assert.Equal(expectedCount, tile.Count);
            Assert.Equal(0, tile.OldestAgeDays);
            var row = Assert.Single(result.Rows, x => x.DocumentId == context.DocumentId);
            Assert.Equal(0, row.AgeDays);
            Assert.StartsWith("/api/v1/purchase/", row.DetailPath);
            if (context.Stage != "RFQ_NO_QUOTATION")
                Assert.True(tile.CommercialValuesVisible, "The actual purchase identity must see the permitted estimate or commercial value.");
            if (tile.CommercialValuesVisible)
            {
                if (context.Stage is "RFQ_NO_QUOTATION" or "COMPARISON_DRAFT")
                    Assert.Null(row.Value);
                else Assert.Equal(context.Stage.StartsWith("PR_", StringComparison.Ordinal) ? expectedPr : expectedPo, row.Value);
                Assert.Equal(result.Rows.Sum(x => x.Value ?? 0m), tile.Amounts.Sum(x => x.Amount));
                if (context.Stage == "COMPARISON_DRAFT") Assert.Equal(1, tile.UnvaluedDocumentCount);
            }
            else
            {
                Assert.All(result.Rows, x => { Assert.Null(x.Value); Assert.Null(x.Currency); });
                Assert.Empty(tile.Amounts);
                Assert.Null(tile.UnvaluedDocumentCount);
            }
            if (context.Stage is "PR_APPROVAL" or "COMPARISON_APPROVAL")
            {
                Assert.Equal("SESS-14", row.NextApproverEmployeeCode);
                Assert.Equal("ACCOUNTS_MANAGER", row.NextApproverRole);
                Assert.Null(row.ResponsibilityIssue);
            }
            if (context.Stage == "PR_APPROVAL")
            {
                var band = Assert.Single(tile.ApprovalBands);
                Assert.Equal(expectedRoute, band.ApprovalRoute);
                Assert.Equal(1, band.Count);
                Assert.Equal(expectedPr, Assert.Single(band.Amounts).Amount);
                Assert.Equal("INR", Assert.Single(band.Amounts).Currency);
                var filtered = await ObserveSingleReportCommand(() => service.GetAsync(new(queue, expectedRoute), default));
                Assert.Equal(row.DocumentId, Assert.Single(filtered.Rows).DocumentId);
            }
            if (context.Stage == "RFQ_NO_QUOTATION") Assert.Equal(2, row.Vendors.Count);
            if (context.Stage == "QUOTATION_VERIFY")
            {
                Assert.All(result.Rows, x => Assert.Equal(1, x.PendingLineCount));
                var page1 = await ObserveSingleReportCommand(() => service.GetAsync(new(queue, PageSize: 1), default));
                var page2 = await ObserveSingleReportCommand(() => service.GetAsync(new(queue, Page: 2, PageSize: 1), default));
                Assert.Equal(2, page1.TotalRows);
                Assert.NotEqual(Assert.Single(page1.Rows).DocumentId, Assert.Single(page2.Rows).DocumentId);
            }
            Assert.Equal(before, await Counts());
            if (context.Stage == "PR_SUBMITTED" && context.Band == "LOW")
            {
                user.OrganizationId = "SESS_PROPRIETORSHIP";
                await Assert.ThrowsAsync<ReportAccessDeniedException>(() => service.GetAsync(new(queue), default));
                user.OrganizationId = "SESS_PVT_LTD";
                var noAssignments = new EfPurchaseWorkloadService(runtime,
                    new WorkloadWitnessUser(employee.Id, []), WorkloadCalendar());
                await Assert.ThrowsAsync<ReportAccessDeniedException>(() => noAssignments.GetAsync(new(), default));
                var denied = await Assert.ThrowsAsync<PostgresException>(() =>
                    runtime.Database.ExecuteSqlRawAsync("SELECT * FROM advance.vendor_bill_lines LIMIT 1"));
                Assert.Equal("42501", denied.SqlState);
                await AssertWorkloadScopeAndPermissionChanges(source, service, user, company.Id, employee.Id,
                    assignments.Select(x => x.RoleId).Distinct().ToArray(), row.DocumentId);
                var afterAccessProbes = await Counts();
                Assert.Equal(before, afterAccessProbes);
                await File.WriteAllTextAsync(Path.Combine(evidence, "purchase-workload-access.json"),
                    JsonSerializer.Serialize(new { Role = "nexa_erp_runtime", Before = before, After = afterAccessProbes,
                        WrongDepartmentHidden = true, OwnRecordOnlyEnforced = true, RoleActivationRechecked = true,
                        CommercialValuesRedacted = true, SourcePageRevocationHidden = true, OriginalScopeRestored = true },
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            Assert.True(seen.Add(context.Band + ":" + context.Stage));
        }, mixedRun: async context =>
        {
            var actor = await BankAdviceActor(context.Options);
            var employee = await Query(context.Options, db => db.Employees.Where(x => x.EmployeeCode == "SESS-15").Select(x => x.Id).SingleAsync());
            var subject = await Query(context.Options, db => db.EmployeeIdentityMappings
                .Where(x => x.EmployeeId == employee && x.IsActive && x.CompanyId == MultiCompanyFoundationSeedData.SessPvtLtdId)
                .Select(x => x.Subject).SingleAsync());
            await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
            using (var denied = await host.Client.GetAsync("/api/v1/dashboards/purchase/workload"))
            {
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
                Assert.Contains("DASHBOARD_ACCESS_DENIED", await denied.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            }
            var effectiveRoles = await Query(context.Options, db => db.EmployeeRoleAssignments
                .Where(x => x.EmployeeId == employee && x.CompanyId == MultiCompanyFoundationSeedData.SessPvtLtdId && x.EffectiveTo == null)
                .Select(x => x.Role!.Code).Distinct().ToArrayAsync());
            actor.Set(employee, subject, "PURCHASE_MANAGER", effectiveRoles);
            var page = await Get<PurchaseWorkloadPage>(host.Client, "/api/v1/dashboards/purchase/workload");
            Assert.Equal(7, page.Tiles.Count);
            Assert.All(page.Tiles, tile => Assert.Equal("READY", tile.State));
            Assert.Equal(0, page.TotalRows);
            using var invalid = await host.Client.GetAsync("/api/v1/dashboards/purchase/workload?queue=unknown");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Contains("DASHBOARD_REQUEST_INVALID", await invalid.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        });
        Assert.Equal(24, seen.Count);
    }

    private static async Task AssertWorkloadScopeAndPermissionChanges(NexaErpDbContext source,
        EfPurchaseWorkloadService service, ICurrentUser user, Guid company, Guid employee, Guid[] roles, Guid document)
    {
        // Metadata probes are local transactions. The scope guard remains enabled:
        // close an open version and insert a replacement, then roll back the whole probe.
        // The projection executes with current_user=nexa_erp_runtime on that transaction.
        await source.Database.OpenConnectionAsync();
        var connection = (NpgsqlConnection)source.Database.GetDbConnection();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var scope = await source.EmployeeOperationalScopes.AsNoTracking()
            .Where(x => x.CompanyId == company && x.EmployeeId == employee && x.IsActive &&
                x.EffectiveFrom <= today && x.EffectiveTo == null).Select(x => x.Id).FirstAsync();
        var department = await source.PurchaseRequisitions.Where(x => x.Id == document)
            .Select(x => x.RequestingDepartmentId).SingleAsync();
        Assert.NotNull(department);
        var otherDepartment = await source.Departments.Where(x => x.Id != department).Select(x => x.Id).FirstAsync();
        const string queue = "pr-department-verification";

        async Task Probe(string sql, Func<EfPurchaseWorkloadService, Task> assertion)
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
                    setup.Parameters.AddWithValue("department", department.Value);
                    setup.Parameters.AddWithValue("other_department", otherDepartment);
                    await setup.ExecuteNonQueryAsync();
                }
                await using (var role = new NpgsqlCommand("SET LOCAL ROLE nexa_erp_runtime", connection, transaction))
                    await role.ExecuteNonQueryAsync();
                await using (var identity = new NpgsqlCommand("SELECT current_user", connection, transaction))
                    Assert.Equal("nexa_erp_runtime", (string)(await identity.ExecuteScalarAsync())!);
                await using var runtimeProbe = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                    .UseNpgsql(connection).Options);
                await runtimeProbe.Database.UseTransactionAsync(transaction);
                await assertion(new EfPurchaseWorkloadService(runtimeProbe, user, WorkloadCalendar()));
            }
            finally { await transaction.RollbackAsync(); }
            Assert.Equal(document, Assert.Single((await service.GetAsync(new(queue), default)).Rows).DocumentId);
        }

        const string closeScopes = """
            UPDATE advance.employee_operational_scopes SET "EffectiveTo"=greatest(CURRENT_DATE,"EffectiveFrom"),
              "IsActive"=false,"UpdatedBy"='WORKLOAD_PROBE',"UpdatedAt"=now(),"Version"="Version"+1
            WHERE "CompanyId"=@company AND "EmployeeId"=@employee AND "EffectiveTo" IS NULL;
            """;
        string Replacement(bool own) => closeScopes + $$"""
            INSERT INTO advance.employee_operational_scopes
            SELECT (jsonb_populate_record(NULL::advance.employee_operational_scopes,to_jsonb(s)||
              jsonb_build_object('Id',gen_random_uuid(),'DepartmentId',{{(own ? "@department" : "@other_department")}},
                'RackBinId',NULL,'OwnRecordsOnly',{{(own ? "true" : "false")}},
                'AllowsPrivilegedCrossScope',false,'EffectiveFrom',CURRENT_DATE,'EffectiveTo',NULL,
                'IsActive',true,'Version',0,'CreatedAt',now(),'CreatedBy','WORKLOAD_PROBE',
                'UpdatedAt',NULL,'UpdatedBy',NULL,'Remarks','Owned rollback-only workload scope probe'))).*
            FROM advance.employee_operational_scopes s WHERE s."Id"=@scope;
            """;
        foreach (var own in new[] { false, true })
            await Probe(Replacement(own), async scoped =>
            {
                var result = await ObserveSingleReportCommand(() => scoped.GetAsync(new(queue), default));
                Assert.Equal("READY", Assert.Single(result.Tiles, x => x.Key == queue).State);
                Assert.Equal(0, result.TotalRows);
            });

        await Probe("""
            UPDATE advance.company_role_activations SET "IsEnabled"=false
            WHERE "CompanyId"=@company AND "RoleId"=ANY(@roles);
            """, async scoped =>
                await Assert.ThrowsAsync<ReportAccessDeniedException>(() => scoped.GetAsync(new(), default)));

        await Probe("""
            UPDATE advance.role_page_permissions rp SET "CanView"=true,"CanViewCommercialValues"=false,"HasFullControl"=false
            FROM advance.page_definitions p WHERE p."Id"=rp."PageDefinitionId"
              AND rp."RoleId"=ANY(@roles) AND p."PageKey"='dashboards.purchase';
            """, async scoped =>
            {
                var result = await ObserveSingleReportCommand(() => scoped.GetAsync(new(queue), default));
                Assert.Null(Assert.Single(result.Rows).Value);
                Assert.False(Assert.Single(result.Tiles, x => x.Key == queue).CommercialValuesVisible);
            });
        await Probe("""
            UPDATE advance.role_page_permissions rp SET "CanView"=false,"HasFullControl"=false
            FROM advance.page_definitions p WHERE p."Id"=rp."PageDefinitionId"
              AND rp."RoleId"=ANY(@roles) AND p."PageKey"='purchase.requisitions';
            UPDATE advance.employee_page_permissions ep SET "CanView"=false
            FROM advance.page_definitions p WHERE p."Id"=ep."PageDefinitionId"
              AND ep."CompanyId"=@company AND ep."EmployeeId"=@employee AND p."PageKey"='purchase.requisitions';
            """, async scoped =>
            {
                var result = await ObserveSingleReportCommand(() => scoped.GetAsync(new(queue), default));
                var tile = Assert.Single(result.Tiles, x => x.Key == queue);
                Assert.Equal("ACCESS_DENIED", tile.State);
                Assert.Null(tile.Count);
                Assert.Empty(result.Rows);
            });
    }
}
