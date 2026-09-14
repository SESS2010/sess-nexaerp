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
    public async Task PurchaseOpenOrdersMigrationGuardsAuthorityAndReconcilesRuntimeExecution()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        Assert.False(model.Database.HasPendingModelChanges());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260914045000_PurchaseOrderCancellationHistory";
        const string current = "20260914050000_PurchaseOpenOrders";
        server.Execute("open-orders-predecessor.sql", migrator.GenerateScript("0", previous));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "open-orders-runtime-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("open-orders-up.sql", migrator.GenerateScript(previous, current));
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            server.Execute("open-orders-guard.sql", PurchaseOpenOrdersMigrationSql.Guard(true));
        }
        server.Execute("open-orders-bad-grant.sql",
            "GRANT EXECUTE ON FUNCTION " + PurchaseOpenOrdersMigrationSql.Signature + " TO nexa_erp_bootstrap;");
        Assert.Equal(1, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("open-orders-grant-restored.sql", PurchaseOpenOrdersMigrationSql.Guard(true));
        await using var owner = new NpgsqlConnection(server.ConnectionString);
        await owner.OpenAsync();
        await using (var change = new NpgsqlCommand("ALTER FUNCTION " + PurchaseOpenOrdersMigrationSql.Signature + " SECURITY INVOKER", owner))
            await change.ExecuteNonQueryAsync();
        await using (var guard = new NpgsqlCommand(PurchaseOpenOrdersMigrationSql.Guard(true), owner))
            Assert.Equal("P0001", (await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteNonQueryAsync())).SqlState);
        await using (var restore = new NpgsqlCommand("ALTER FUNCTION " + PurchaseOpenOrdersMigrationSql.Signature + " SECURITY DEFINER", owner))
            await restore.ExecuteNonQueryAsync();
        server.Execute("open-orders-down.sql", migrator.GenerateScript(current, previous));
        server.Execute("open-orders-reapply.sql", migrator.GenerateScript(previous, current));
        server.Execute("open-orders-final-guard.sql", PurchaseOpenOrdersMigrationSql.Guard(true));
    }




    private sealed record PurchaseOpenOrderWitnessContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection,string Stage,Guid PurchaseOrderId,string Band);

#if WORKFLOW_WITNESS
    [Fact]
    public async Task PurchaseOpenOrdersReconcileIssueReceiptAndOverdueAcrossApprovalBands()
    {
        var observations=new List<object>();
        var stages=new HashSet<string>();
        await RunCompletePurchaseFlow(openOrders: async context=>
        {
            Assert.True(stages.Add(context.Band+":"+context.Stage));
            await using var source=new NexaErpDbContext(context.Options);
            var company=await source.Companies.SingleAsync(x=>x.Code=="SESS_PVT_LTD");
            var employee=await source.Employees.SingleAsync(x=>x.EmployeeCode=="SESS-15");
            var assignments=await source.EmployeeRoleAssignments.AsNoTracking().Include(x=>x.Role)
                .Where(x=>x.CompanyId==company.Id&&x.EmployeeId==employee.Id&&x.EffectiveTo==null).ToListAsync();
            var user=new WorkloadWitnessUser(employee.Id,
                assignments.Select(x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)).ToArray());
            await using var runtime=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql(context.RuntimeConnection).Options);
            var service=new EfPurchaseOpenOrdersService(runtime,user,WorkloadCalendar());
            async Task<string> Counts()
            {
                await source.Database.OpenConnectionAsync();
                await using var command=new NpgsqlCommand("""
                    SELECT jsonb_build_object('audits',(SELECT count(*) FROM advance.audit_logs),
                      'requests',(SELECT count(*) FROM advance.command_requests),
                      'receipts',(SELECT count(*) FROM advance.command_receipts),
                      'movements',(SELECT count(*) FROM advance.stock_movements),
                      'pos',(SELECT count(*) FROM advance.purchase_orders),
                      'grns',(SELECT count(*) FROM advance.goods_receipts))::text
                    """,(NpgsqlConnection)source.Database.GetDbConnection());
                return (string)(await command.ExecuteScalarAsync())!;
            }
            var before=await Counts();
            var page=await ObserveSingleReportCommand(()=>service.GetAsync(new(),default));
            Assert.True(page.Complete); Assert.True(page.DeliveryComplete);
            Assert.Empty(page.SourceIssues); Assert.Equal(0,page.DeliveryDateUnconfirmedPoCount);
            if(context.Stage=="ISSUED")
            {
                var expected=context.Band switch {"LOW"=>4720m,"TD"=>5900m,"MD"=>118000.01m,_=>throw new InvalidOperationException()};
                Assert.Equal(1,page.OpenPoCount); Assert.Equal(1,page.TotalRows);
                var row=Assert.Single(page.Rows);
                Assert.Equal(context.PurchaseOrderId,row.PurchaseOrderId);
                Assert.Equal(1m,row.OrderedQuantity); Assert.Equal(0m,row.ReceivedQuantity);
                Assert.Equal(1m,row.RemainingQuantity); Assert.Equal(expected,row.Value);
                Assert.Equal(expected,Assert.Single(page.Amounts!).Value);
                Assert.Equal("INR",row.Currency); Assert.Equal(1,row.RevisionNumber);
                Assert.Equal(row.FirstIssuedAt,row.IssuedAt);
                Assert.Equal(0,row.AgeDays);
                Assert.Equal(row.QuotedDeliveryDate,row.CommittedDeliveryDate);
                if(context.Band=="LOW")
                {
                    var localDate=ReportCalendarOptions.DateInZone(page.GeneratedAt,TimeZoneInfo.FindSystemTimeZoneById(page.TimeZone));
                    Assert.True(row.QuotedDeliveryDate<localDate);
                    Assert.Equal(localDate.DayNumber-row.QuotedDeliveryDate.DayNumber,row.DaysLate);
                    Assert.Equal("OVERDUE",row.DeliveryState); Assert.Equal(1,page.OverduePoCount);
                    Assert.Equal(expected,Assert.Single(page.Amounts!).OverdueValue);
                    Assert.Single((await service.GetAsync(new(OverdueOnly:true),default)).Rows);
                    var filtered=await service.GetAsync(new(row.VendorId," inr ",row.RootPurchaseOrderId),default);
                    Assert.Equal(row,Assert.Single(filtered.Rows));
                    Assert.Equal(0,(await service.GetAsync(new(Currency:"USD"),default)).TotalRows);
                    Assert.Equal(0,(await service.GetAsync(new(RootPurchaseOrderId:Guid.NewGuid()),default)).TotalRows);
                    Assert.Empty((await service.GetAsync(new(Page:2,PageSize:1),default)).Rows);
                    await AssertOpenOrdersLiveAccess(source,user,service,company.Id,employee.Id,
                        assignments.Select(x=>x.RoleId).Distinct().ToArray());
                    user.OrganizationId="SESS_PROPRIETORSHIP";
                    await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>service.GetAsync(new(),default));
                    user.OrganizationId="SESS_PVT_LTD";
                    var emptyRoles=new EfPurchaseOpenOrdersService(runtime,new WorkloadWitnessUser(employee.Id,[]),WorkloadCalendar());
                    await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>emptyRoles.GetAsync(new(),default));
                    var tableRead=await Assert.ThrowsAsync<PostgresException>(()=>
                        runtime.Database.ExecuteSqlRawAsync("SELECT * FROM advance.vendor_bill_lines LIMIT 1"));
                    Assert.Equal("42501",tableRead.SqlState);
                    var actor=await BankAdviceActor(context.Options);
                    await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,actor,true,true);
                    using(var denied=await host.Client.GetAsync("/api/v1/dashboards/purchase/open-orders"))
                        Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
                    var subject=await source.EmployeeIdentityMappings.Where(x=>x.EmployeeId==employee.Id&&x.CompanyId==company.Id&&x.IsActive)
                        .Select(x=>x.Subject).SingleAsync();
                    actor.Set(employee.Id,subject,"PURCHASE_MANAGER",assignments.Select(x=>x.Role!.Code).Distinct().ToArray());
                    var http=await Get<PurchaseOpenOrdersPage>(host.Client,"/api/v1/dashboards/purchase/open-orders?overdueOnly=true");
                    Assert.Equal(expected,Assert.Single(http.Rows).Value);
                    using var invalid=await host.Client.GetAsync("/api/v1/dashboards/purchase/open-orders?pageSize=0");
                    Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
                }
                else
                {
                    Assert.Equal("WITHIN_COMMITMENT",row.DeliveryState);
                    Assert.Equal(0,page.OverduePoCount);
                    Assert.Equal(0m,Assert.Single(page.Amounts!).OverdueValue);
                    Assert.Empty((await service.GetAsync(new(OverdueOnly:true),default)).Rows);
                }
            }
            else
            {
                Assert.Equal("RECEIVED",context.Stage);
                Assert.Equal(0,page.OpenPoCount); Assert.Equal(0,page.TotalRows);
                Assert.Equal(0,page.OverduePoCount); Assert.Empty(page.Rows); Assert.Empty(page.Amounts!);
            }
            Assert.Equal(before,await Counts());
            observations.Add(new{context.Band,context.Stage,Before=before,After=await Counts(),Page=page});
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item29");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"purchase-open-orders-runtime.json"),
                JsonSerializer.Serialize(observations,new JsonSerializerOptions{WriteIndented=true}));
        },overdueQuoteDates:true);
        Assert.Equal(6,stages.Count);
    }

#endif
#if WORKFLOW_WITNESS
    [Fact]
    public async Task PurchaseOpenOrdersKeepFullyReceivedIssuedAmendmentClosed()
    {
        await RunPurchaseOrderRevisionCashWitness(async context=>
        {
            await using var source=new NexaErpDbContext(context.Options);
            var company=await source.Companies.SingleAsync(x=>x.Code=="SESS_PVT_LTD");
            var employee=await source.Employees.SingleAsync(x=>x.EmployeeCode=="SESS-15");
            var assignments=await source.EmployeeRoleAssignments.AsNoTracking().Include(x=>x.Role)
                .Where(x=>x.CompanyId==company.Id&&x.EmployeeId==employee.Id&&x.EffectiveTo==null).ToListAsync();
            var user=new WorkloadWitnessUser(employee.Id,
                assignments.Select(x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)).ToArray());
            await using var runtime=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql(context.RuntimeConnection).Options);
            var service=new EfPurchaseOpenOrdersService(runtime,user,WorkloadCalendar());
            var revision=await source.PurchaseOrders.AsNoTracking().SingleAsync(x=>x.Id==context.RevisionId);
            Assert.Equal("Issued",revision.Status); Assert.True(revision.RevisionNumber>1);
            var page=await ObserveSingleReportCommand(()=>service.GetAsync(new(RootPurchaseOrderId:revision.RootPurchaseOrderId),default));
            Assert.True(page.Complete); Assert.True(page.DeliveryComplete); Assert.Empty(page.SourceIssues);
            Assert.Equal(0,page.OpenPoCount); Assert.Equal(0,page.TotalRows); Assert.Empty(page.Rows); Assert.Empty(page.Amounts!);
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item29");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"purchase-open-orders-amendment.json"),
                JsonSerializer.Serialize(new{context.PriorId,context.RevisionId,revision.RootPurchaseOrderId,Page=page},
                    new JsonSerializerOptions{WriteIndented=true}));
        });
    }
#endif
    private static async Task AssertOpenOrdersLiveAccess(NexaErpDbContext source, WorkloadWitnessUser user,
        EfPurchaseOpenOrdersService service, Guid company, Guid employee, Guid[] roles)
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
                var probe = new EfPurchaseOpenOrdersService(probeDb, user, WorkloadCalendar());
                if (denied) await Assert.ThrowsAsync<ReportAccessDeniedException>(() => probe.GetAsync(new(), default));
                else
                {
                    var hidden = await ObserveSingleReportCommand(() => probe.GetAsync(new(), default));
                    Assert.Equal(0, hidden.TotalRows);
                    Assert.Empty(hidden.Amounts!);



                }
            }
            finally { await transaction.RollbackAsync(); }
            Assert.Equal(1, (await service.GetAsync(new(), default)).TotalRows);
        }
        await Probe("""
            UPDATE advance.employee_operational_scopes SET "EffectiveTo"=greatest(CURRENT_DATE,"EffectiveFrom"),
              "IsActive"=false,"UpdatedBy"='OPEN_ORDERS_PROBE',"UpdatedAt"=now(),"Version"="Version"+1
            WHERE "CompanyId"=@company AND "EmployeeId"=@employee AND "EffectiveTo" IS NULL;
            INSERT INTO advance.employee_operational_scopes
            SELECT (jsonb_populate_record(NULL::advance.employee_operational_scopes,to_jsonb(s)||
              jsonb_build_object('Id',gen_random_uuid(),'DepartmentId',@department,
                'RackBinId',NULL,'OwnRecordsOnly',false,'AllowsPrivilegedCrossScope',false,
                'EffectiveFrom',CURRENT_DATE,'EffectiveTo',NULL,'IsActive',true,'Version',0,
                'CreatedAt',now(),'CreatedBy','OPEN_ORDERS_PROBE','UpdatedAt',NULL,'UpdatedBy',NULL,
                'Remarks','Owned rollback-only open orders scope probe'))).*
            FROM advance.employee_operational_scopes s WHERE s."Id"=@scope;
            """, false);
        await Probe("""
            UPDATE advance.company_role_activations SET "IsEnabled"=false
            WHERE "CompanyId"=@company AND "RoleId"=ANY(@roles);
            """, true);
        await Probe("""
            UPDATE advance.role_page_permissions rp SET "CanViewCommercialValues"=false,"HasFullControl"=false
            FROM advance.page_definitions p WHERE p."Id"=rp."PageDefinitionId"
              AND rp."RoleId"=ANY(@roles) AND p."PageKey"='dashboards.purchase-open-orders';
            """, true);
        await Probe("""
            UPDATE advance.role_page_permissions rp SET "CanView"=false,"HasFullControl"=false
            FROM advance.page_definitions p WHERE p."Id"=rp."PageDefinitionId"
              AND rp."RoleId"=ANY(@roles) AND p."PageKey"='dashboards.purchase-open-orders';
            UPDATE advance.employee_page_permissions ep SET "CanView"=false
            FROM advance.page_definitions p WHERE p."Id"=ep."PageDefinitionId"
              AND ep."CompanyId"=@company AND ep."EmployeeId"=@employee AND p."PageKey"='dashboards.purchase-open-orders';
            """, true);
    }
}
