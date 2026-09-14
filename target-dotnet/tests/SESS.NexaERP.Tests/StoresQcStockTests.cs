using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record StoresQcStockWitnessContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection,string Stage,Guid DocumentId,string Band);

    [Fact]
    public async Task StoresQcStockReconcilesReceiptsDispositionAndConcession()
    {
        var observations=new List<object>();
        var stages=new HashSet<string>();
        await RunCompletePurchaseFlow(qcStock:async context=>
        {
            Assert.True(stages.Add(context.Band+":"+context.Stage));
            await using var source=new NexaErpDbContext(context.Options);
            var company=await source.Companies.SingleAsync(x=>x.Code=="SESS_PVT_LTD");
            var director=await QcStockReader(source,"SESS-01");
            var floor=await QcStockReader(source,"SESS-35");
            await using var runtime=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql(context.RuntimeConnection).Options);
            var service=new EfStoresQcStockService(runtime,director,WorkloadCalendar());
            var floorService=new EfStoresQcStockService(runtime,floor,WorkloadCalendar());
            var before=await QcStockCounts(source);
            var page=await ObserveSingleReportCommand(()=>service.GetAsync(new(),default));
            var floorPage=await ObserveSingleReportCommand(()=>floorService.GetAsync(new(),default));
            var after=await QcStockCounts(source);
            observations.Add(new{context.Band,context.Stage,before,after,Page=page,Floor=floorPage});
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item30");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"stores-qc-stock-states.json"),
                JsonSerializer.Serialize(observations,new JsonSerializerOptions{WriteIndented=true}));
            Assert.Equal(before,after);
            Assert.True(page.CanViewCommercialValues); Assert.False(floorPage.CanViewCommercialValues);
            Assert.All(floorPage.Rows,x=>Assert.Null(x.ReceiptProvisionalValue));
            Assert.All(floorPage.Tiles,x=>Assert.Null(x.Values));
            Assert.Equal(page.Rows.Count,floorPage.Rows.Count);
            var expectedHold=context.Stage=="GRN_FINALIZED" ? context.Band switch{"LOW"=>1,"TD"=>2,_=>3}
                : context.Stage=="CONCESSION_APPROVED"?0:context.Band switch{"LOW"=>2,"TD"=>1,_=>0};
            var expectedRejected=context.Stage=="GRN_FINALIZED"?0:
                context.Stage=="CONCESSION_APPROVED"?1:context.Band=="MD"?2:1;
            Assert.Equal(expectedHold,Assert.Single(page.Tiles,x=>x.Key=="QC_HOLD").LineCount);
            Assert.Equal(expectedRejected,Assert.Single(page.Tiles,x=>x.Key=="PENDING_RETURNABLE_DC").LineCount);
            Assert.Equal(context.Stage=="GRN_FINALIZED"?1:0,
                Assert.Single(page.Tiles,x=>x.Key=="QC_HOLD").OverdueLineCount);
            Assert.Equal(0,Assert.Single(page.Tiles,x=>x.Key=="PENDING_RETURNABLE_DC").OverdueLineCount);
            foreach(var row in page.Rows)
            {
                var receipt=await source.GoodsReceipts.AsNoTracking().SingleAsync(x=>x.Id==row.DocumentId);
                var line=await source.GoodsReceiptLines.AsNoTracking().SingleAsync(x=>x.Id==row.LineId);
                var actual=await source.StockMovements.Where(x=>x.CompanyId==company.Id&&
                    x.GoodsReceiptLineLotAllocationId==row.AllocationId&&x.ConditionCode==row.Queue&&
                    x.WarehouseId==row.WarehouseId&&x.RackBinId==row.RackBinId&&
                    x.OwnershipAccountId==row.OwnershipAccountId&&x.CustodyAssignmentId==row.CustodyAssignmentId&&
                    x.InventoryProvenanceLayerId==row.ProvenanceLayerId&&x.InventorySerialId==row.SerialId)
                    .SumAsync(x=>x.QuantityIn-x.QuantityOut);
                Assert.Equal(actual,row.Quantity); Assert.True(row.Quantity>0);
                Assert.Equal(actual*line.UnitRateSnapshot,row.ReceiptProvisionalValue);
                Assert.Equal(receipt.QcDueAt,row.QcDueAt);
                Assert.Equal(receipt.ReceivedAt,row.ReceivedAt);
                Assert.Equal(line.UomSnapshot,row.Uom);
            }
            foreach(var tile in page.Tiles)
                foreach(var total in tile.Values!)
                    Assert.Equal(page.Rows.Where(x=>x.Queue==tile.Key&&x.Currency==total.Currency)
                        .Sum(x=>x.ReceiptProvisionalValue),total.ReceiptProvisionalValue);
            var filtered=await service.GetAsync(new(DocumentId:context.DocumentId,PageSize:1),default);
            Assert.All(filtered.Rows,x=>Assert.Equal(context.DocumentId,x.DocumentId));
            Assert.Equal(page.Tiles.Select(x=>x.LineCount),filtered.Tiles.Select(x=>x.LineCount));
            Assert.Empty((await service.GetAsync(new(DocumentId:Guid.NewGuid()),default)).Rows);
            if(context.Band=="LOW"&&context.Stage=="GRN_FINALIZED")
            {
                Assert.Single(page.Rows);
                Assert.Empty((await service.GetAsync(new(Page:2,PageSize:1),default)).Rows);
                var denied=new EfStoresQcStockService(runtime,await QcStockReader(source,"SESS-41"),WorkloadCalendar());
                await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>denied.GetAsync(new(),default));
                director.OrganizationId="SESS_PROPRIETORSHIP";
                await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>service.GetAsync(new(),default));
                director.OrganizationId="SESS_PVT_LTD";
                await Assert.ThrowsAsync<ReportRequestException>(()=>service.GetAsync(new("unknown"),default));
                var roleless=new EfStoresQcStockService(runtime,new StoresWorkloadUser(director.EmployeeId!.Value,[]),WorkloadCalendar());
                await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>roleless.GetAsync(new(),default));
                Assert.Equal("42501",(await Assert.ThrowsAsync<PostgresException>(()=>
                    runtime.Database.ExecuteSqlRawAsync("SELECT * FROM advance.vendor_payments LIMIT 1"))).SqlState);
                await AssertQcStockLiveAuthority(source,floor,company.Id,context.DocumentId);
                var actor=await BankAdviceActor(context.Options);
                await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,actor,true,true);
                using(var deniedHttp=await host.Client.GetAsync("/api/v1/dashboards/stores/qc-stock"))
                    Assert.Equal(HttpStatusCode.Forbidden,deniedHttp.StatusCode);
                var subject=await source.EmployeeIdentityMappings.Where(x=>x.CompanyId==company.Id&&
                    x.EmployeeId==floor.EmployeeId&&x.IsActive).Select(x=>x.Subject).SingleAsync();
                actor.Set(floor.EmployeeId!.Value,subject,"STORES_EXECUTIVE");
                var http=await Get<StoresQcStockPage>(host.Client,"/api/v1/dashboards/stores/qc-stock?queue=QC_HOLD");
                Assert.Single(http.Rows); Assert.Null(http.Rows[0].ReceiptProvisionalValue);
                using var invalid=await host.Client.GetAsync("/api/v1/dashboards/stores/qc-stock?queue=unknown");
                Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
            }
        });
        Assert.Equal(7,stages.Count);
    }

    private static async Task<StoresWorkloadUser> QcStockReader(NexaErpDbContext db,string code)
    {
        var employee=await db.Employees.SingleAsync(x=>x.EmployeeCode==code);
        var assignments=await db.EmployeeRoleAssignments.Include(x=>x.Role)
            .Where(x=>x.CompanyId==Guid.Parse("70000000-0000-0000-0000-000000000001")&&
                x.EmployeeId==employee.Id&&x.EffectiveTo==null).ToListAsync();
        return new(employee.Id,assignments.Select(x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)).ToArray());
    }

    private static async Task<string> QcStockCounts(NexaErpDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        await using var command=new NpgsqlCommand("""
            SELECT jsonb_build_object('audits',(SELECT count(*) FROM advance.audit_logs),
              'requests',(SELECT count(*) FROM advance.command_requests),
              'receipts',(SELECT count(*) FROM advance.command_receipts),
              'movements',(SELECT count(*) FROM advance.stock_movements),
              'batches',(SELECT count(*) FROM advance.stock_posting_batches),
              'inspections',(SELECT count(*) FROM advance.qc_inspections),
              'revisions',(SELECT count(*) FROM advance.qc_inspection_revisions),
              'concessions',(SELECT count(*) FROM advance.inventory_concessions))::text
            """,(NpgsqlConnection)db.Database.GetDbConnection());
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task AssertQcStockLiveAuthority(NexaErpDbContext source,StoresWorkloadUser user,Guid company,Guid document)
    {
        var connection=(NpgsqlConnection)source.Database.GetDbConnection();
        var roles=await source.EmployeeRoleAssignments.Where(x=>x.EmployeeId==user.EmployeeId&&x.CompanyId==company)
            .Select(x=>x.RoleId).ToArrayAsync();
        foreach(var target in new[]{"scope","activation","dashboard","source"})
        {
            await using var transaction=await connection.BeginTransactionAsync();
            try
            {
                var sql=target switch
                {
                    "scope"=>"""
            UPDATE advance.employee_operational_scopes SET "EffectiveTo"=greatest(CURRENT_DATE,"EffectiveFrom"),
              "IsActive"=false,"UpdatedBy"='STORES_WORKLOAD_PROBE',"UpdatedAt"=now(),"Version"="Version"+1
            WHERE "CompanyId"=@company AND "EmployeeId"=@employee AND "EffectiveTo" IS NULL;
            INSERT INTO advance.employee_operational_scopes
            SELECT(jsonb_populate_record(NULL::advance.employee_operational_scopes,to_jsonb(s)||
              jsonb_build_object('Id',gen_random_uuid(),'DepartmentId',(SELECT "Id" FROM advance.departments WHERE "Code"='PRODUCTION' LIMIT 1),'RackBinId',NULL,
                'OwnRecordsOnly',false,'AllowsPrivilegedCrossScope',false,'EffectiveFrom',CURRENT_DATE,'EffectiveTo',NULL,
                'IsActive',true,'Version',0,'CreatedAt',now(),'CreatedBy','STORES_WORKLOAD_PROBE',
                'UpdatedAt',NULL,'UpdatedBy',NULL,'Remarks','Owned rollback-only Stores workload probe'))).*
            FROM advance.employee_operational_scopes s WHERE s."CompanyId"=@company AND s."EmployeeId"=@employee LIMIT 1;
            """,
                    "activation"=>"""UPDATE advance.company_role_activations SET "IsEnabled"=false WHERE "CompanyId"=@company AND "RoleId"=ANY(@roles);""",
                    _=>"""
                        UPDATE advance.role_page_permissions rp SET "CanView"=false,"HasFullControl"=false
                        FROM advance.page_definitions p WHERE p."Id"=rp."PageDefinitionId" AND rp."RoleId"=ANY(@roles) AND p."PageKey"=@page;
                        UPDATE advance.employee_page_permissions ep SET "CanView"=false
                        FROM advance.page_definitions p WHERE p."Id"=ep."PageDefinitionId" AND ep."CompanyId"=@company AND ep."EmployeeId"=@employee AND p."PageKey"=@page;
                        """
                };
                await using(var command=new NpgsqlCommand(sql,connection,transaction))
                {
                    command.Parameters.AddWithValue("company",company);command.Parameters.AddWithValue("employee",user.EmployeeId!.Value);
                    command.Parameters.AddWithValue("roles",roles);
                    command.Parameters.AddWithValue("page",target=="source"?"inventory.grn":"dashboards.stores-qc-stock");
                    await command.ExecuteNonQueryAsync();
                }
                await using(var role=new NpgsqlCommand("SET LOCAL ROLE nexa_erp_runtime",connection,transaction))
                    await role.ExecuteNonQueryAsync();
                await using var db=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(connection).Options);
                await db.Database.UseTransactionAsync(transaction);
                var service=new EfStoresQcStockService(db,user,WorkloadCalendar());
                if(target=="scope") Assert.Empty((await service.GetAsync(new(),default)).Rows);
                else await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>service.GetAsync(new(),default));
            }
            finally{await transaction.RollbackAsync();}
        }
    }
}
