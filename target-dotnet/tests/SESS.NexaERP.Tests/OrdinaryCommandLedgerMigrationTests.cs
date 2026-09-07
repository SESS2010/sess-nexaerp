using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string OrdinaryLedgerTarget = "20260906172511_OrdinaryCommandLedger";

    [Fact]
    public void Ordinary_command_ledger_covers_the_whole_controlled_service_surface()
    {
        var root=FindRepositoryRoot();
        var purchase=string.Join('\n',Directory.GetFiles(Path.Combine(root,"src","SESS.NexaERP.Infrastructure","Purchase"),"EfRev869BPurchaseService*.cs").Select(File.ReadAllText));
        var configuration=File.ReadAllText(Path.Combine(root,"src","SESS.NexaERP.Api","Endpoints","Rev869AConfigurationEndpoints.cs"));
        var tax=File.ReadAllText(Path.Combine(root,"src","SESS.NexaERP.Infrastructure","Masters","EfTaxGstWorkflowService.cs"));
        var authorizer=File.ReadAllText(Path.Combine(root,"src","SESS.NexaERP.Infrastructure","Persistence","Rev869BCommandContextAuthorizer.cs"));
        foreach(var operation in new[]{"CreateRFQ","InviteVendor","SubmitQuotation","TechnicalVerification",
            "CreateComparison","RecommendComparison","ResubmitComparison","CreatePO","SubmitPO","IssuePO",
            "AmendPO","ReviseRejectedPO","CancelPO","MaterialFollowUp"})
            Assert.Contains(operation,purchase,StringComparison.Ordinal);
        foreach(var operation in new[]{"Approve","Reject","RequestRevision"})
            Assert.Contains($"request, \"{operation}\"",purchase,StringComparison.Ordinal);
        Assert.Contains("action + \"Comparison\"",purchase,StringComparison.Ordinal);
        Assert.Contains("action + \"PO\"",purchase,StringComparison.Ordinal);
        foreach(var operation in new[]{"CreateVendorQualification","NormalizeVendorQualification"})
            Assert.Contains(operation,configuration,StringComparison.Ordinal);
        foreach(var operation in new[]{"Verify","Approve","Reject","RequestCorrection"}) Assert.Contains($"action == \"{operation}\"",configuration,StringComparison.Ordinal);
        Assert.Contains("action + \"VendorQualification\"",configuration,StringComparison.Ordinal);
        Assert.Contains("CreateTaxGstSetting",tax,StringComparison.Ordinal);
        Assert.Contains("approve ? \"Approve\" : \"Reject\"",tax,StringComparison.Ordinal);
        Assert.Contains("action + \"TaxGstSetting\"",tax,StringComparison.Ordinal);
        Assert.Contains("OrdinaryLedgerAvailableAsync",authorizer,StringComparison.Ordinal);
        Assert.Contains("StageCommittedReceiptAsync",purchase+configuration+tax,StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ordinary_command_ledger_enforces_acl_atomicity_immutability_and_replay_on_postgresql()
    {
        var options=new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var model=new NexaErpDbContext(options);var migrator=model.GetService<IMigrator>();var migrations=model.Database.GetMigrations().ToArray();
        var targetIndex=Array.IndexOf(migrations,OrdinaryLedgerTarget);Assert.True(targetIndex>0);var predecessor=migrations[targetIndex-1];
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("ordinary-ledger-prerequisite.sql",migrator.GenerateScript("0",predecessor));
        server.Execute("ordinary-ledger-up.sql",migrator.GenerateScript(predecessor,OrdinaryLedgerTarget));
        server.Execute("ordinary-ledger-down.sql",migrator.GenerateScript(OrdinaryLedgerTarget,predecessor));
        server.Execute("ordinary-ledger-reapply.sql",migrator.GenerateScript(predecessor,OrdinaryLedgerTarget));

        const string runtimePassword="ordinary-ledger-runtime-123456789";
        using var environment=new OrdinaryPrincipalEnvironment(server.ConnectionString,runtimePassword);
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        var runtime=new NpgsqlConnectionStringBuilder(server.ConnectionString){Username="nexa_erp_runtime",Password=runtimePassword,Pooling=false}.ConnectionString;
        await VerifyOrdinaryLedgerBehavior(server.ConnectionString,runtime);
    }

    private static async Task VerifyOrdinaryLedgerBehavior(string adminConnectionString,string runtimeConnectionString)
    {
        var full=await ReadAuthority(adminConnectionString,"PURCHASE_MANAGER","FULL");
        var support=await ReadAuthority(adminConnectionString,"STORES_EXECUTIVE","SUPPORT");
        var requestHash=Enumerable.Repeat((byte)0x11,32).ToArray();
        var keyHash=Enumerable.Repeat((byte)0x22,32).ToArray();
        var businessHash=Enumerable.Repeat((byte)0x33,32).ToArray();
        const string response="{\"id\":\"ordinary-original\"}";

        await using(var runtime=new NpgsqlConnection(runtimeConnectionString))
        {
            await runtime.OpenAsync();
            var readDenied=await Assert.ThrowsAsync<PostgresException>(async()=>
            {
                await using var command=new NpgsqlCommand("SELECT * FROM advance.command_requests",runtime);
                await command.ExecuteNonQueryAsync();
            });
            Assert.Equal("42501",readDenied.SqlState);
            var insertDenied=await Assert.ThrowsAsync<PostgresException>(async()=>
            {
                await using var command=new NpgsqlCommand("INSERT INTO advance.command_requests DEFAULT VALUES",runtime);
                await command.ExecuteNonQueryAsync();
            });
            Assert.Equal("42501",insertDenied.SqlState);

            await using(var rollback=await runtime.BeginTransactionAsync())
            {
                var commandId=await Register(runtime,rollback,full,"CreateRFQ",keyHash,requestHash);
                await using(var business=new NpgsqlCommand("UPDATE advance.companies SET \"UpdatedBy\"='ordinary-rollback' WHERE \"Code\"='SESS_PVT_LTD'",runtime,rollback))
                    Assert.Equal(1,await business.ExecuteNonQueryAsync());
                _=await Receipt(runtime,rollback,commandId,businessHash,response,Guid.NewGuid());
                await rollback.RollbackAsync();
            }
        }
        Assert.Equal(0,await Count(adminConnectionString,"advance.command_requests"));
        Assert.Equal(0,await Count(adminConnectionString,"advance.command_receipts"));
        Assert.NotEqual("ordinary-rollback",await ScalarString(adminConnectionString,"SELECT \"UpdatedBy\" FROM advance.companies WHERE \"Code\"='SESS_PVT_LTD'"));

        Guid committedCommand;Guid committedReceipt;
        await using(var runtime=new NpgsqlConnection(runtimeConnectionString))
        {
            await runtime.OpenAsync();
            await using(var commit=await runtime.BeginTransactionAsync())
            {
                committedCommand=await Register(runtime,commit,full,"CreateRFQ",keyHash,requestHash);
                await using(var business=new NpgsqlCommand("UPDATE advance.companies SET \"UpdatedBy\"='ordinary-committed' WHERE \"Code\"='SESS_PVT_LTD'",runtime,commit))
                    Assert.Equal(1,await business.ExecuteNonQueryAsync());
                committedReceipt=await Receipt(runtime,commit,committedCommand,businessHash,response,Guid.NewGuid());
                await commit.CommitAsync();
            }
            await using(var replay=await runtime.BeginTransactionAsync())
            {
                Assert.Equal(committedCommand,await Register(runtime,replay,full,"CreateRFQ",keyHash,requestHash));
                Assert.Equal(committedReceipt,await Receipt(runtime,replay,committedCommand,businessHash,response,Guid.NewGuid()));
                await replay.CommitAsync();
            }
            await using(var mismatch=await runtime.BeginTransactionAsync())
            {
                var denied=await Assert.ThrowsAsync<PostgresException>(()=>Register(runtime,mismatch,full,"CreateRFQ",keyHash,Enumerable.Repeat((byte)0x44,32).ToArray()));
                Assert.Equal("23505",denied.SqlState);await mismatch.RollbackAsync();
            }
            await using(var supportAttempt=await runtime.BeginTransactionAsync())
            {
                var denied=await Assert.ThrowsAsync<PostgresException>(()=>Register(runtime,supportAttempt,support,"CancelPO",Enumerable.Repeat((byte)0x55,32).ToArray(),requestHash));
                Assert.Equal("42501",denied.SqlState);Assert.Contains("Required role",denied.MessageText,StringComparison.Ordinal);
                await supportAttempt.RollbackAsync();
            }
        }
        Assert.Equal(1,await Count(adminConnectionString,"advance.command_requests"));
        Assert.Equal(1,await Count(adminConnectionString,"advance.command_receipts"));
        Assert.Equal("ordinary-committed",await ScalarString(adminConnectionString,"SELECT \"UpdatedBy\" FROM advance.companies WHERE \"Code\"='SESS_PVT_LTD'"));
        await AssertImmutable(adminConnectionString);
    }

    private sealed record LedgerAuthority(Guid EmployeeId,Guid AssignmentId,string RoleCode);

    private static async Task<LedgerAuthority> ReadAuthority(string connectionString,string roleCode,string assignmentType)
    {
        await using var connection=new NpgsqlConnection(connectionString);await connection.OpenAsync();
        const string sql="""
            SELECT e."Id",a."Id",r."Code"
            FROM advance.employee_role_assignments a
            JOIN advance.employees e ON e."Id"=a."EmployeeId"
            JOIN advance.roles r ON r."Id"=a."RoleId"
            JOIN advance.companies c ON c."Id"=a."CompanyId"
            WHERE e."EmployeeCode"='SESS-15' AND c."Code"='SESS_PVT_LTD'
              AND r."Code"=@role AND a."AssignmentType"=@type
              AND a."ApprovalStatus" IN ('Approved','SeedApproved')
              AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE)
            """;
        await using var command=new NpgsqlCommand(sql,connection);command.Parameters.AddWithValue("role",roleCode);command.Parameters.AddWithValue("type",assignmentType);
        await using var reader=await command.ExecuteReaderAsync();Assert.True(await reader.ReadAsync());
        return new(reader.GetGuid(0),reader.GetGuid(1),reader.GetString(2));
    }

    private static async Task<Guid> Register(NpgsqlConnection connection,NpgsqlTransaction transaction,LedgerAuthority authority,string operation,byte[] keyHash,byte[] requestHash)
    {
        const string sql="SELECT advance.register_command_request('SESS_PVT_LTD',@operation,@key,@request,@actor,'https://ordinary.test','SESS-15',@role,@assignment)";
        await using var command=new NpgsqlCommand(sql,connection,transaction);command.Parameters.AddWithValue("operation",operation);command.Parameters.AddWithValue("key",keyHash);command.Parameters.AddWithValue("request",requestHash);command.Parameters.AddWithValue("actor",authority.EmployeeId);command.Parameters.AddWithValue("role",authority.RoleCode);command.Parameters.AddWithValue("assignment",authority.AssignmentId);
        return Assert.IsType<Guid>(await command.ExecuteScalarAsync());
    }

    private static async Task<Guid> Receipt(NpgsqlConnection connection,NpgsqlTransaction transaction,Guid commandId,byte[] businessHash,string response,Guid receiptId)
    {
        const string sql="SELECT advance.commit_command_receipt(@command,@business,CAST(@response AS jsonb),@receipt)";
        await using var command=new NpgsqlCommand(sql,connection,transaction);command.Parameters.AddWithValue("command",commandId);command.Parameters.AddWithValue("business",businessHash);command.Parameters.AddWithValue("response",response);command.Parameters.AddWithValue("receipt",receiptId);
        return Assert.IsType<Guid>(await command.ExecuteScalarAsync());
    }

    private static async Task<int> Count(string connectionString,string relation)
    {
        await using var connection=new NpgsqlConnection(connectionString);await connection.OpenAsync();
        await using var command=new NpgsqlCommand($"SELECT count(*)::integer FROM {relation}",connection);
        return Assert.IsType<int>(await command.ExecuteScalarAsync());
    }

    private static async Task<string> ScalarString(string connectionString,string sql)
    {
        await using var connection=new NpgsqlConnection(connectionString);await connection.OpenAsync();
        await using var command=new NpgsqlCommand(sql,connection);
        return Convert.ToString(await command.ExecuteScalarAsync(),System.Globalization.CultureInfo.InvariantCulture)??string.Empty;
    }

    private static async Task AssertImmutable(string connectionString)
    {
        foreach(var statement in new[]{
            "UPDATE advance.command_requests SET \"Operation\"=\"Operation\"",
            "DELETE FROM advance.command_requests",
            "UPDATE advance.command_receipts SET \"CommittedBy\"=\"CommittedBy\"",
            "DELETE FROM advance.command_receipts"})
        {
            await using var connection=new NpgsqlConnection(connectionString);await connection.OpenAsync();
            await using var command=new NpgsqlCommand(statement,connection);
            var denied=await Assert.ThrowsAsync<PostgresException>(()=>command.ExecuteNonQueryAsync());
            Assert.Equal("42501",denied.SqlState);
        }
    }
}
