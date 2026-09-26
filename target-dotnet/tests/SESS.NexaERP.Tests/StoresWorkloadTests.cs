using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record StoresWorkloadWitnessContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection,string Stage,Guid DocumentId,string Band);
    private sealed class StoresWorkloadUser(Guid employee,IReadOnlyList<EffectiveRoleAssignment> assignments):ICurrentUser
    {
        public string LoginId=>"stores-workload-witness";
        public string RoleCode=>assignments.FirstOrDefault()?.RoleCode??"";
        public string? OrganizationId{get;set;}="SESS_PVT_LTD";
        public bool IsAuthenticated=>true;
        public Guid? EmployeeId=>employee;
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments=>assignments;
    }

#if WORKFLOW_WITNESS
    [Fact]
    public async Task StoresWorkloadReconcilesGateConversionAndMirApprovalIssue()
    {
        var observations=new List<object>();
        var seen=new HashSet<string>();
        await RunCompletePurchaseFlow(storesWorkload:async context=>
        {
            Assert.True(seen.Add(context.Band+":"+context.Stage));
            await using var source=new NexaErpDbContext(context.Options);
            var company=await source.Companies.SingleAsync(x=>x.Code=="SESS_PVT_LTD");
            var readerCode=context.Band=="MIR"?"SESS-41":"SESS-35";
            var employee=await source.Employees.SingleAsync(x=>x.EmployeeCode==readerCode);
            var assignments=await source.EmployeeRoleAssignments.AsNoTracking().Include(x=>x.Role)
                .Where(x=>x.CompanyId==company.Id&&x.EmployeeId==employee.Id&&x.EffectiveTo==null).ToListAsync();
            var user=new StoresWorkloadUser(employee.Id,assignments.Select(x=>
                new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)).ToArray());
            await using var runtime=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql(context.RuntimeConnection).Options);
            var service=new EfStoresWorkloadService(runtime,user,WorkloadCalendar());
            await source.Database.OpenConnectionAsync();
            async Task<string> Counts()
            {
                await using var command=new NpgsqlCommand("""
                    SELECT jsonb_build_object('audits',(SELECT count(*) FROM advance.audit_logs),
                      'requests',(SELECT count(*) FROM advance.command_requests),
                      'receipts',(SELECT count(*) FROM advance.command_receipts),
                      'movements',(SELECT count(*) FROM advance.stock_movements),
                      'gates',(SELECT count(*) FROM advance.gate_entries),
                      'grns',(SELECT count(*) FROM advance.goods_receipts),
                      'mirs',(SELECT count(*) FROM advance.material_issue_requests),
                      'issues',(SELECT count(*) FROM advance.material_issues))::text
                    """,(NpgsqlConnection)source.Database.GetDbConnection());
                return (string)(await command.ExecuteScalarAsync())!;
            }
            var before=await Counts();
            var page=await ObserveSingleReportCommand(()=>service.GetAsync(new(),default));
            var after=await Counts();
            observations.Add(new{context.Band,context.Stage,before,after,Page=page});
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item30");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"stores-workload-states.json"),
                JsonSerializer.Serialize(observations,new JsonSerializerOptions{WriteIndented=true}));
            Assert.Equal(before,after);
            Assert.Equal(3,page.Tiles.Count);

            var active=context.Stage switch
            {
                "GATE_FINALIZED"=>"gate-no-grn",
                "MIR_SUBMITTED"=>"mir-approval",
                "MIR_APPROVED"=>"mir-unissued",
                _=>null
            };
            foreach(var tile in page.Tiles)
            {
                // QcGoodsReceiptRead explicitly grants STORES_MANAGER inventory.grn view.
                // The separate rollback-only permission probes below still require ACCESS_DENIED
                // when both role and employee source-page grants are removed.
                Assert.Equal("READY",tile.State);
                Assert.Equal(tile.Key==active?1L:0L,tile.Count);
            }
            if(active is not null)
            {
                var row=Assert.Single(page.Rows);
                Assert.Equal(context.DocumentId,row.DocumentId); Assert.Equal(active,row.Queue);
                Assert.Equal(1,row.PendingLineCount); Assert.True(row.AgeDays>=0);
                var filtered=await ObserveSingleReportCommand(()=>service.GetAsync(new(active,context.DocumentId,PageSize:1),default));
                Assert.Equal(row.DocumentId,Assert.Single(filtered.Rows).DocumentId);
                var second=await service.GetAsync(new(active,context.DocumentId,Page:2,PageSize:1),default);
                Assert.Equal(1,second.TotalRows); Assert.Empty(second.Rows);
                Assert.Empty((await service.GetAsync(new(active,Guid.NewGuid()),default)).Rows);
                if(active=="mir-approval")
                {
                    Assert.Null(row.AssignedApproverEmployeeId);
                    Assert.Equal(new[]{"STORES_MANAGER","PRODUCTION_MANAGER"},row.EligibleApprovalRoles);
                    Assert.Contains("No named approver",row.ResponsibilityIssue!,StringComparison.Ordinal);
                }
            }
            else Assert.Empty(page.Rows);
            if(context.Band=="LOW"&&context.Stage=="GATE_FINALIZED")
            {
                var assistant=await source.Employees.SingleAsync(x=>x.EmployeeCode=="SESS-16");
                var assistantRoles=await source.EmployeeRoleAssignments.Include(x=>x.Role)
                    .Where(x=>x.CompanyId==company.Id&&x.EmployeeId==assistant.Id&&x.EffectiveTo==null).ToListAsync();
                var assistantReader=new StoresWorkloadUser(assistant.Id,assistantRoles.Select(x=>
                    new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)).ToArray());
                var assistantService=new EfStoresWorkloadService(runtime,assistantReader,WorkloadCalendar());
                Assert.Equal(context.DocumentId,Assert.Single((await ObserveSingleReportCommand(()=>
                    assistantService.GetAsync(new("gate-no-grn"),default))).Rows).DocumentId);
                await AssertStoresWorkloadScopeChanges(source,service,user,company.Id,employee.Id,
                    assignments.Select(x=>x.RoleId).Distinct().ToArray(),context.DocumentId);
                var emptyRoles=new EfStoresWorkloadService(runtime,new StoresWorkloadUser(employee.Id,[]),WorkloadCalendar());
                await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>emptyRoles.GetAsync(new(),default));
                user.OrganizationId="SESS_PROPRIETORSHIP";
                await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>service.GetAsync(new(),default));
                user.OrganizationId="SESS_PVT_LTD";
                var direct=await Assert.ThrowsAsync<PostgresException>(()=>
                    runtime.Database.ExecuteSqlRawAsync("SELECT * FROM advance.vendor_payments LIMIT 1"));
                Assert.Equal("42501",direct.SqlState);
                await Assert.ThrowsAsync<ReportRequestException>(()=>service.GetAsync(new("unknown"),default));
                await Assert.ThrowsAsync<ReportRequestException>(()=>service.GetAsync(new(Page:0),default));
                var actor=await BankAdviceActor(context.Options);
                await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,actor,true,true);
                using(var denied=await host.Client.GetAsync("/api/v1/dashboards/stores/workload"))
                {
                    Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
                    Assert.Contains("DASHBOARD_ACCESS_DENIED",await denied.Content.ReadAsStringAsync(),StringComparison.Ordinal);
                }
                var subject=await source.EmployeeIdentityMappings.Where(x=>x.CompanyId==company.Id&&
                    x.EmployeeId==employee.Id&&x.IsActive).Select(x=>x.Subject).SingleAsync();
                actor.Set(employee.Id,subject,"STORES_EXECUTIVE");
                var http=await Get<StoresWorkloadPage>(host.Client,"/api/v1/dashboards/stores/workload?queue=gate-no-grn");
                Assert.Equal(context.DocumentId,Assert.Single(http.Rows).DocumentId);
                using var invalid=await host.Client.GetAsync("/api/v1/dashboards/stores/workload?queue=unknown");
                Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
            }
        });
        Assert.Equal(9,seen.Count);
    }

#endif
    private static async Task AssertStoresWorkloadScopeChanges(NexaErpDbContext source,
        EfStoresWorkloadService service,ICurrentUser user,Guid company,Guid employee,Guid[] roles,Guid document)
    {
        var connection=(NpgsqlConnection)source.Database.GetDbConnection();
        var scope=await source.EmployeeOperationalScopes.Where(x=>x.CompanyId==company&&x.EmployeeId==employee&&
            x.IsActive&&x.EffectiveTo==null).Select(x=>x.Id).FirstAsync();
        var department=await source.GateEntries.Where(x=>x.Id==document)
            .Select(x=>x.PurchaseOrder!.RequestingDepartmentId).SingleAsync();
        var other=await source.Departments.Where(x=>x.Id!=department).Select(x=>x.Id).FirstAsync();
        async Task Probe(string sql,bool denied,bool sourceDenied=false)
        {
            await using var transaction=await connection.BeginTransactionAsync();
            try
            {
                await using(var setup=new NpgsqlCommand(sql,connection,transaction))
                {
                    setup.Parameters.AddWithValue("company",company);setup.Parameters.AddWithValue("employee",employee);
                    setup.Parameters.AddWithValue("roles",roles);setup.Parameters.AddWithValue("scope",scope);
                    setup.Parameters.AddWithValue("department",other);
                    await setup.ExecuteNonQueryAsync();
                }
                await using(var role=new NpgsqlCommand("SET LOCAL ROLE nexa_erp_runtime",connection,transaction))
                    await role.ExecuteNonQueryAsync();
                await using var probeDb=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(connection).Options);
                await probeDb.Database.UseTransactionAsync(transaction);
                var probe=new EfStoresWorkloadService(probeDb,user,WorkloadCalendar());
                if(denied)await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>probe.GetAsync(new(),default));
                else
                {
                    var page=await ObserveSingleReportCommand(()=>probe.GetAsync(new(),default));
                    Assert.Empty(page.Rows);
                    var tile=Assert.Single(page.Tiles,x=>x.Key=="gate-no-grn");
                    Assert.Equal(sourceDenied?"ACCESS_DENIED":"READY",tile.State);
                    if(sourceDenied)Assert.Null(tile.Count);else Assert.Equal(0,tile.Count);
                }
            }
            finally{await transaction.RollbackAsync();}
            Assert.Equal(document,Assert.Single((await service.GetAsync(new(),default)).Rows).DocumentId);
        }
        await Probe("""
            UPDATE advance.employee_operational_scopes SET "EffectiveTo"=greatest(CURRENT_DATE,"EffectiveFrom"),
              "IsActive"=false,"UpdatedBy"='STORES_WORKLOAD_PROBE',"UpdatedAt"=now(),"Version"="Version"+1
            WHERE "CompanyId"=@company AND "EmployeeId"=@employee AND "EffectiveTo" IS NULL;
            INSERT INTO advance.employee_operational_scopes
            SELECT(jsonb_populate_record(NULL::advance.employee_operational_scopes,to_jsonb(s)||
              jsonb_build_object('Id',gen_random_uuid(),'DepartmentId',@department,'RackBinId',NULL,
                'OwnRecordsOnly',false,'AllowsPrivilegedCrossScope',false,'EffectiveFrom',CURRENT_DATE,'EffectiveTo',NULL,
                'IsActive',true,'Version',0,'CreatedAt',now(),'CreatedBy','STORES_WORKLOAD_PROBE',
                'UpdatedAt',NULL,'UpdatedBy',NULL,'Remarks','Owned rollback-only Stores workload probe'))).*
            FROM advance.employee_operational_scopes s WHERE s."Id"=@scope;
            """,false);
        await Probe("""
            UPDATE advance.company_role_activations SET "IsEnabled"=false WHERE "CompanyId"=@company AND "RoleId"=ANY(@roles);
            """,true);
        foreach(var pageKey in new[]{"dashboards.stores-workload","inventory.grn"})
            await Probe($$"""
                UPDATE advance.role_page_permissions rp SET "CanView"=false,"HasFullControl"=false
                FROM advance.page_definitions p WHERE p."Id"=rp."PageDefinitionId" AND rp."RoleId"=ANY(@roles)
                  AND p."PageKey"='{{pageKey}}';
                UPDATE advance.employee_page_permissions ep SET "CanView"=false
                FROM advance.page_definitions p WHERE p."Id"=ep."PageDefinitionId" AND ep."CompanyId"=@company
                  AND ep."EmployeeId"=@employee AND p."PageKey"='{{pageKey}}';
                """,pageKey=="dashboards.stores-workload",pageKey=="inventory.grn");
    }
}
