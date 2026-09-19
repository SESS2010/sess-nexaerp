using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task InventoryPeriodUsesResolvedCfoAndRetainsCommandReplay()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start("C:/Program Files/PostgreSQL/17/bin");
        server.Execute("period-current-chain.sql",model.GetService<IMigrator>().GenerateScript("0","20260919100000_IntercompanyInvoiceEvidence"));
        // First reproduce the new draft's missing seed, then apply the documented baseline.
        server.Execute("assert-cfo-absent.sql", """
            DO $assert$ BEGIN IF EXISTS(SELECT 1 FROM advance.roles WHERE "Code"='CHIEF_FINANCIAL_OFFICER')
             THEN RAISE EXCEPTION 'The original CFO seed-gap premise changed; reclassify before proceeding.'; END IF; END $assert$;
            """);
        string DraftScript(Migration migration,bool down=false)
        {
            var target = migration is DocumentedCfoAuthorityBaseline
                ? "20260919110000_DocumentedCfoAuthorityBaseline" : "20260919120000_GovernedInventoryPeriods";
            var all = model.Database.GetMigrations().ToArray();
            var previous = all[Array.IndexOf(all,target)-1];
            return model.GetService<IMigrator>().GenerateScript(down?target:previous,down?previous:target);
        }
        var cfoMigration=new DocumentedCfoAuthorityBaseline {ActiveProvider="Npgsql.EntityFrameworkCore.PostgreSQL"};
        var periodMigration=new GovernedInventoryPeriods {ActiveProvider="Npgsql.EntityFrameworkCore.PostgreSQL"};
        server.Execute("documented-cfo-baseline.sql",DraftScript(cfoMigration));
        server.Execute("documented-cfo-unused-down.sql",DraftScript(cfoMigration,true));
        server.Execute("documented-cfo-unused-up-again.sql",DraftScript(cfoMigration));
        var company = MultiCompanyFoundationSeedData.SessPvtLtdId;
        Guid cfoId, accountsId;
        IReadOnlyDictionary<string,EffectiveRoleAssignment> assignments;
        await using (var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options))
        {
            cfoId = await db.Employees.Where(x=>x.EmployeeCode=="SESS-02").Select(x=>x.Id).SingleAsync();
            accountsId = await db.Employees.Where(x=>x.EmployeeCode=="SESS-14").Select(x=>x.Id).SingleAsync();
            db.EmployeeIdentityMappings.Add(Mapping(company,cfoId,"SESS-02"));
            db.EmployeeIdentityMappings.Add(Mapping(company,accountsId,"SESS-14"));
            await db.SaveChangesAsync();
            assignments = await db.EmployeeRoleAssignments.AsNoTracking().Include(x=>x.Role)
                .Where(x=>x.CompanyId==company && x.EffectiveTo==null)
                .ToDictionaryAsync(x=>TaxWorkflowUser.AssignmentKey(x.EmployeeId,x.Role!.Code),
                    x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType));
        }
        const string password="period-disposable-runtime-only";
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString,password);
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        server.Execute("period-migration-up.sql",DraftScript(periodMigration));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        server.Execute("period-unused-migration-down.sql",DraftScript(periodMigration,true));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        server.Execute("period-unused-migration-up-again.sql",DraftScript(periodMigration));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        var runtime = new NpgsqlConnectionStringBuilder(server.ConnectionString) {Username="nexa_erp_runtime",Password=password,Pooling=false}.ConnectionString;
        Assert.True(assignments.ContainsKey(TaxWorkflowUser.AssignmentKey(cfoId,"CHIEF_FINANCIAL_OFFICER")));
        var user = new TaxWorkflowUser(cfoId,"SESS-02","CHIEF_FINANCIAL_OFFICER",assignments);
        await using var host = await PurchaseFlowHost.StartAsync(runtime,user,useRealPagePermissions:true,useRealOperationalScopes:true);
        var client=host.Client;
        const string path="/api/v1/accounts/inventory-periods";
        server.Execute("existing-financial-year.sql", """
            INSERT INTO advance.financial_periods("Id","CompanyId","Code","Name","PeriodType","StartDate","EndDate",
             "Status","IsActive","CreatedAt","CreatedBy","Version")
            SELECT md5('period-test-financial-year')::uuid,"Id",'FY-2026-27','Financial year','FINANCIAL_YEAR',
             DATE '2026-04-01',DATE '2027-03-31','OPEN',true,now(),'period-test',0
             FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
            """);
        var request=new OpenInventoryPeriodRequest("INV-2026-27","Inventory year",new(2026,4,1),new(2027,3,31),"Governed period witness","period-open-1");
        using var response=await client.PostAsJsonAsync(path,request);
        Assert.True(response.IsSuccessStatusCode,$"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        var period=(await response.Content.ReadFromJsonAsync<InventoryPeriodView>())!;
        Assert.Equal("OPEN",period.Status); Assert.Single(period.Decisions); Assert.False(period.Replayed);
        using var replayResponse=await client.PostAsJsonAsync(path,request);
        Assert.True(replayResponse.IsSuccessStatusCode,await replayResponse.Content.ReadAsStringAsync());
        var replay=(await replayResponse.Content.ReadFromJsonAsync<InventoryPeriodView>())!;
        Assert.Equal(period.Id,replay.Id); Assert.True(replay.Replayed);
        using var changed=await client.PostAsJsonAsync(path,request with {Name="Changed payload"});
        Assert.Equal(HttpStatusCode.Conflict,changed.StatusCode);
        var list=(await client.GetFromJsonAsync<InventoryPeriodView[]>(path))!;
        Assert.Equal(period.Id,Assert.Single(list).Id);
        var readBack = (await client.GetFromJsonAsync<InventoryPeriodView>($"{path}/{period.Id}"))!;
        Assert.Equal(period.Id,readBack.Id); Assert.Equal(period.Version,readBack.Version);
        user.Set(cfoId,"SESS-02","MANAGING_DIRECTOR");
        using var wrongRole=await client.PostAsJsonAsync(path,request with {IdempotencyKey="md-period"});
        Assert.Equal(HttpStatusCode.Forbidden,wrongRole.StatusCode);
        user.Set(accountsId,"SESS-14","ACCOUNTS_MANAGER");
        using var accountsRole=await client.PostAsJsonAsync(path,request with {IdempotencyKey="accounts-period"});
        Assert.Equal(HttpStatusCode.Forbidden,accountsRole.StatusCode);
        user.Set(cfoId,"SESS-02","CHIEF_FINANCIAL_OFFICER");
        using var stale=await client.PostAsJsonAsync($"{path}/{period.Id}/close",new CloseInventoryPeriodRequest(8,"Stale","stale-period"));
        Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);
        var close=new CloseInventoryPeriodRequest(readBack.Version,"Close retained period","period-close-1");
        using var closedResponse=await client.PostAsJsonAsync($"{path}/{period.Id}/close",close);
        Assert.True(closedResponse.IsSuccessStatusCode,await closedResponse.Content.ReadAsStringAsync());
        var closed=(await closedResponse.Content.ReadFromJsonAsync<InventoryPeriodView>())!;
        Assert.Equal("CLOSED",closed.Status); Assert.Equal(2,closed.Decisions.Count);
        using var closeReplay=await client.PostAsJsonAsync($"{path}/{period.Id}/close",close);
        Assert.True(closeReplay.IsSuccessStatusCode,await closeReplay.Content.ReadAsStringAsync());
        Assert.True((await closeReplay.Content.ReadFromJsonAsync<InventoryPeriodView>())!.Replayed);
        using var reopen=await client.PostAsJsonAsync(path,request with {Code="REOPEN",IdempotencyKey="period-reopen"});
        Assert.Equal(HttpStatusCode.Conflict,reopen.StatusCode);
        user.SetOrganization("SESS_PROPRIETORSHIP");
        using var otherCompany=await client.GetAsync($"{path}/{period.Id}");
        Assert.Equal(HttpStatusCode.NotFound,otherCompany.StatusCode);
        user.SetOrganization("SESS_PVT_LTD");
        var octoberA=request with {Code="OCT-A",Name="October A",StartDate=new(2027,10,1),EndDate=new(2027,10,31),IdempotencyKey="period-race-a"};
        var octoberB=request with {Code="OCT-B",Name="October B",StartDate=new(2027,10,15),EndDate=new(2027,11,15),IdempotencyKey="period-race-b"};
        var raced=await Task.WhenAll(client.PostAsJsonAsync(path,octoberA),client.PostAsJsonAsync(path,octoberB));
        Assert.Equal(1,raced.Count(x=>x.IsSuccessStatusCode));
        Assert.Equal(1,raced.Count(x=>x.StatusCode==HttpStatusCode.Conflict));
        foreach(var item in raced)item.Dispose();
        server.Execute("period-private-read-drift.sql","GRANT SELECT ON advance.inventory_period_events TO nexa_erp_runtime;");
        Assert.NotEqual(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        server.Execute("period-disabled-guard.sql","ALTER TABLE advance.financial_periods DISABLE TRIGGER trg_inventory_period_guard;");
        Assert.NotEqual(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        // Restore this deliberately disabled test trigger; Provision only promises ACL repair.
        server.Execute("period-restore-test-guard.sql","ALTER TABLE advance.financial_periods ENABLE TRIGGER trg_inventory_period_guard;");
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        server.AssertRejected("cfo-used-down-refused.sql",DraftScript(cfoMigration,true),"CFO rollback refuses changed or used authority");
        server.AssertRejected("period-used-migration-down-refused.sql",DraftScript(periodMigration,true),"Inventory period rollback refuses retained decisions");
        await using var owner = new NpgsqlConnection(server.ConnectionString); await owner.OpenAsync();
        await using var counts = new NpgsqlCommand("""
            SELECT (SELECT count(*) FROM advance.financial_periods WHERE "PeriodType"='INVENTORY'),
             (SELECT count(*) FROM advance.inventory_period_events),
             (SELECT count(*) FROM advance.stock_movements),
             (SELECT count(*) FROM advance.roles WHERE "Code"='CHIEF_FINANCIAL_OFFICER'),
             (SELECT count(*) FROM advance.company_role_activations a JOIN advance.roles r ON r."Id"=a."RoleId" WHERE r."Code"='CHIEF_FINANCIAL_OFFICER'),
             (SELECT count(*) FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId" WHERE r."Code"='CHIEF_FINANCIAL_OFFICER'),
             (SELECT count(*) FROM advance.employee_role_assignment_events WHERE "ToRoleCode"='CHIEF_FINANCIAL_OFFICER')
            """,owner);
        await using var reader=await counts.ExecuteReaderAsync(); Assert.True(await reader.ReadAsync());
        Assert.Equal(2,reader.GetInt64(0)); Assert.Equal(3,reader.GetInt64(1)); Assert.Equal(0,reader.GetInt64(2));
        Assert.Equal(1,reader.GetInt64(3)); Assert.Equal(2,reader.GetInt64(4)); Assert.Equal(2,reader.GetInt64(5)); Assert.Equal(2,reader.GetInt64(6));
    }
}
