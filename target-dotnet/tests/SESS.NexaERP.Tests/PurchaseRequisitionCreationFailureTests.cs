using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public Task PurchaseRequisitionCreationRetryAndAuditFailureAreAtomic() =>
        RunCompletePurchaseFlow(mixedRun: RunPrCreationFailureWitness);

    private static async Task RunPrCreationFailureWitness(MixedRunContext context)
    {
        await using var sourceDb = new NexaErpDbContext(context.Options);
        var source = new NpgsqlConnectionStringBuilder(sourceDb.Database.GetConnectionString());
        Assert.Equal("127.0.0.1",source.Host);
        Assert.Equal("advance_parser",source.Database);
        Assert.Equal("postgres",source.Username);
        Assert.False(source.Pooling);
        Assert.InRange(source.Port,1025,65535);
        Assert.NotEqual(5432,source.Port);
        var cloneName = "pr_failure_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(source.ConnectionString)
            { Database = "postgres" }.ConnectionString))
        {
            await admin.OpenAsync();
            await using (var idle = new NpgsqlCommand("SELECT count(*) FROM pg_stat_activity WHERE datname='advance_parser'",admin))
                Assert.Equal(0L,(long)(await idle.ExecuteScalarAsync())!);
            await using (var clone = new NpgsqlCommand($"CREATE DATABASE \"{cloneName}\" WITH TEMPLATE advance_parser OWNER nexa_erp_owner",admin))
                await clone.ExecuteNonQueryAsync();
            await using var grants = new NpgsqlCommand($"REVOKE CONNECT,TEMPORARY ON DATABASE \"{cloneName}\" FROM PUBLIC; GRANT CONNECT ON DATABASE \"{cloneName}\" TO nexa_erp_runtime",admin);
            await grants.ExecuteNonQueryAsync();
        }
        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(
            new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = cloneName }.ConnectionString).Options;
        var runtime = new NpgsqlConnectionStringBuilder(context.RuntimeConnection) { Database = cloneName,Pooling = false }.ConnectionString;
        var company = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var actorId = await Query(options,db => db.Employees.Where(x => x.EmployeeCode == "SESS-15").Select(x => x.Id).SingleAsync());
        var subject = await Query(options,db => db.EmployeeIdentityMappings.Where(x => x.CompanyId == company && x.EmployeeId == actorId && x.IsActive).Select(x => x.Subject).SingleAsync());
        var assignments = await Query(options,async db => (await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
            .Where(x => x.CompanyId == company && x.EffectiveTo == null).ToListAsync())
            .ToDictionary(x => TaxWorkflowUser.AssignmentKey(x.EmployeeId,x.Role!.Code),
                x => new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)));
        var actor = new TaxWorkflowUser(actorId,subject,"PURCHASE_EXECUTIVE",assignments);
        await using var host = await PurchaseFlowHost.StartAsync(runtime,actor,true,true);
        var required = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var body = new CreatePurchaseRequisitionRequest("SESS_PVT_LTD","IT","SESS-15",required,"NORMAL",
            "Network retry witness","TRIAL-WH-C01",null,null,null,null,null,
            [new("TRIAL-ITEM-001",1,25,required,"TRIAL-WH-C01",null,null,null)]);
        const string path = "/api/v1/purchase/requisitions";
        var missingKey = await TimedRacePost(host.Client,path,body);
        Assert.Equal(HttpStatusCode.BadRequest,missingKey.Status);
        Assert.Equal(0,await Query(options,db => db.PurchaseRequisitions.CountAsync(x => x.PurposeJustification == body.PurposeJustification)));
        host.Client.DefaultRequestHeaders.Add("Idempotency-Key","pr-network-retry-witness");
        var first = await TimedRacePost(host.Client,path,body);
        var repeated = await TimedRacePost(host.Client,path,body);
        Assert.Equal(HttpStatusCode.Created,first.Status);
        Assert.Equal(HttpStatusCode.Created,repeated.Status);
        var firstPr = JsonSerializer.Deserialize<PurchaseRequisitionDetail>(first.Body)!;
        var repeatedPr = JsonSerializer.Deserialize<PurchaseRequisitionDetail>(repeated.Body)!;
        var duplicateCount = await Query(options,db => db.PurchaseRequisitions.CountAsync(x => x.PurposeJustification == body.PurposeJustification));
        var mismatch = await TimedRacePost(host.Client,path,body with { PurposeJustification = "Changed request with original key" });
        Assert.Equal(HttpStatusCode.Conflict,mismatch.Status);
        Assert.Equal(0,await Query(options,db => db.PurchaseRequisitions.CountAsync(x => x.PurposeJustification == "Changed request with original key")));


        var concurrentBody = body with { PurposeJustification = "Concurrent retry witness" };
        host.Client.DefaultRequestHeaders.Remove("Idempotency-Key");
        host.Client.DefaultRequestHeaders.Add("Idempotency-Key","pr-concurrent-retry-witness");
        var concurrent = await Task.WhenAll(
            TimedRacePost(host.Client,path,concurrentBody),TimedRacePost(host.Client,path,concurrentBody));
        Assert.All(concurrent,result => Assert.True(result.Status is HttpStatusCode.Created or HttpStatusCode.Conflict,result.Body));
        Assert.Contains(concurrent,result => result.Status == HttpStatusCode.Created);
        var concurrentReplay = await TimedRacePost(host.Client,path,concurrentBody);
        Assert.Equal(HttpStatusCode.Created,concurrentReplay.Status);
        var concurrentId = JsonSerializer.Deserialize<PurchaseRequisitionDetail>(concurrentReplay.Body)!.Id;
        foreach (var success in concurrent.Where(x => x.Status == HttpStatusCode.Created))
            Assert.Equal(concurrentId,JsonSerializer.Deserialize<PurchaseRequisitionDetail>(success.Body)!.Id);
        Assert.Equal(1,await Query(options,db => db.PurchaseRequisitions.CountAsync(x => x.PurposeJustification == concurrentBody.PurposeJustification)));
        Assert.Equal(1,await Query(options,db => db.AuditLogs.CountAsync(x => x.Action == "CreateDraft" && x.EntityId == concurrentId.ToString())));

        var failureBody = body with { PurposeJustification = "Audit failure witness" };
        host.Client.DefaultRequestHeaders.Remove("Idempotency-Key");
        host.Client.DefaultRequestHeaders.Add("Idempotency-Key","pr-audit-failure-witness");
        await using var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = cloneName }.ConnectionString);
        await connection.OpenAsync();
        await using (var install = new NpgsqlCommand("""
            CREATE FUNCTION advance.witness_pr_audit_failure() RETURNS trigger LANGUAGE plpgsql AS $f$
            BEGIN RAISE EXCEPTION 'Witness audit sink unavailable'; END $f$;
            CREATE TRIGGER witness_pr_audit_failure BEFORE INSERT ON advance.audit_logs
            FOR EACH ROW WHEN (NEW."Action"='CreateDraft' AND NEW."EntityName"='PurchaseRequisition')
            EXECUTE FUNCTION advance.witness_pr_audit_failure();
            """,connection))
            await install.ExecuteNonQueryAsync();
        var logStart = context.ReadPostgresLog().Length;
        RaceHttpResult failed;
        try { failed = await TimedRacePost(host.Client,path,failureBody); }
        finally
        {
            await using var remove = new NpgsqlCommand("""
                DROP TRIGGER witness_pr_audit_failure ON advance.audit_logs;
                DROP FUNCTION advance.witness_pr_audit_failure();
                """,connection);
            await remove.ExecuteNonQueryAsync();
        }
        var afterFailure = await Query(options,db => db.PurchaseRequisitions.CountAsync(x => x.PurposeJustification == failureBody.PurposeJustification));
        await using (var rolledBack = new NpgsqlCommand("""
            SELECT count(*) FROM advance.command_requests
            WHERE "Operation"='PurchaseRequisition.Create'
              AND "IdempotencyKeySha256"=sha256(convert_to('pr-audit-failure-witness','UTF8'))
            """,connection))
            Assert.Equal(0L,(long)(await rolledBack.ExecuteScalarAsync())!);
        var retry = await TimedRacePost(host.Client,path,failureBody);
        var afterRetry = await Query(options,db => db.PurchaseRequisitions.CountAsync(x => x.PurposeJustification == failureBody.PurposeJustification));
        var ids = await Query(options,db => db.PurchaseRequisitions.Where(x => x.PurposeJustification == failureBody.PurposeJustification).Select(x => x.Id.ToString()).ToListAsync());
        var audits = await Query(options,db => db.AuditLogs.CountAsync(x => x.Action == "CreateDraft" && ids.Contains(x.EntityId)));
        var directory = Path.Combine(FindRepositoryRoot(),"local-evidence","item26");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory,"pr-create-failures.json"),JsonSerializer.Serialize(new {
            First = first,Repeated = repeated,DuplicateCount = duplicateCount,Concurrent = concurrent,ConcurrentReplay = concurrentReplay,Failed = failed,
            DraftsAfterFailure = afterFailure,Retry = retry,DraftsAfterRetry = afterRetry,CreationAudits = audits
        },new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(directory,"pr-create-failures-postgresql.log"),context.ReadPostgresLog()[logStart..]);
        Assert.Equal(firstPr.Id,repeatedPr.Id);
        Assert.Equal(1,duplicateCount);
        Assert.Equal(HttpStatusCode.InternalServerError,failed.Status);
        Assert.Equal(0,afterFailure);
        Assert.Equal(HttpStatusCode.Created,retry.Status);
        Assert.Equal(1,afterRetry);
        Assert.Equal(1,audits);
    }
}
