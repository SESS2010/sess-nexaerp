using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record DirectFifoRaceContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection, Guid FirstOperatorId, Guid SecondOperatorId, Func<string> ReadPostgresLog);
    private sealed record FifoInput(Guid IssueId, Guid LineId, Guid ActorId, string Subject,
        string Role, Guid AssignmentId, string Key, byte[] RequestHash);
    private sealed class FifoRemainingWitnessRow
    {
        public Guid Id { get; set; }
        public Guid ItemId { get; set; }
        public decimal UnitCost { get; set; }
        public DateTimeOffset ReceivedAt { get; set; }
        public decimal Remaining { get; set; }
    }

    private sealed record FifoCallResult(int? Affected, bool Committed, string? SqlState,
        string? Message, double Seconds);

    [Fact]
    public async Task DirectFifoCallsCannotConsumeTheLastRemainingUnitTwice()
    {
        var observed = false;
        await RunCompletePurchaseFlow(fifoRace: async context =>
        {
            observed = true;
            await RunDirectFifoRace(context);
        });
        Assert.True(observed, "The direct FIFO callback must execute.");
    }

    private static async Task RunDirectFifoRace(DirectFifoRaceContext context)
    {
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        await using var sourceDb = new NexaErpDbContext(context.Options);
        var source = new NpgsqlConnectionStringBuilder(sourceDb.Database.GetConnectionString());
        Assert.Equal("127.0.0.1",source.Host);
        Assert.Equal("advance_parser",source.Database);
        Assert.Equal("postgres",source.Username);
        Assert.NotEqual(5432,source.Port);
        Assert.InRange(source.Port,1025,65535);
        Assert.False(source.Pooling);
        var cloneName = "fifo_race_" + Guid.NewGuid().ToString("N");
        var administrative = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = "postgres" };
        await using (var admin = new NpgsqlConnection(administrative.ConnectionString))
        {
            await admin.OpenAsync();
            await using (var connections = new NpgsqlCommand(
                "SELECT count(*) FROM pg_stat_activity WHERE datname='advance_parser'",admin))
                Assert.Equal(0L,(long)(await connections.ExecuteScalarAsync())!);
            // Only this validated disposable cluster is used. Keep the parent
            // witness intact for its remaining engineer/company report checks.
            await using (var clone = new NpgsqlCommand(
                $"CREATE DATABASE \"{cloneName}\" WITH TEMPLATE advance_parser OWNER nexa_erp_owner",admin))
            {
                clone.CommandTimeout = 60;
                await clone.ExecuteNonQueryAsync();
            }
            await using var grants = new NpgsqlCommand(
                $"REVOKE CONNECT,TEMPORARY ON DATABASE \"{cloneName}\" FROM PUBLIC; " +
                $"GRANT CONNECT ON DATABASE \"{cloneName}\" TO nexa_erp_runtime;",admin);
            await grants.ExecuteNonQueryAsync();
        }
        var cloneAdmin = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = cloneName }.ConnectionString;
        var runtime = new NpgsqlConnectionStringBuilder(context.RuntimeConnection) { Database = cloneName, Pooling = false }.ConnectionString;
        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(cloneAdmin).Options;
        async Task<List<FifoRemainingWitnessRow>> ReadRemaining(DbContextOptions<NexaErpDbContext> sourceOptions)
            => await Query(sourceOptions, db => db.Database.SqlQueryRaw<FifoRemainingWitnessRow>("""
                SELECT f."Id",f."ItemId",f."UnitCost",f."ReceivedAt",
                  f."QuantityReceived"-coalesce((SELECT sum(c."Quantity"-coalesce((
                    SELECT sum(r."Quantity") FROM advance.fifo_cost_restorations r
                    WHERE r."CompanyId"=c."CompanyId" AND r."FifoCostConsumptionId"=c."Id"),0))
                    FROM advance.fifo_cost_consumptions c
                    WHERE c."CompanyId"=f."CompanyId" AND c."FifoInventoryCostLayerId"=f."Id"),0) AS "Remaining"
                FROM advance.fifo_inventory_cost_layers f WHERE f."CompanyId"=@company
                ORDER BY f."ReceivedAt",f."Id"
                """,new NpgsqlParameter("company",companyId)).ToListAsync());
        var remaining=(await ReadRemaining(options)).Where(x=>x.Remaining>0).ToList();
        Assert.Equal(2.63m,remaining.Sum(x=>x.Remaining));
        var oldest=remaining.Last();
        Assert.Equal(1m,oldest.Remaining);
        var sourceLine = await Query(options, db => db.MaterialIssueLines
            .Where(row => row.CompanyId == companyId && row.ItemId == oldest.ItemId
                && row.QuantityBase == 1m && row.InventorySerialId != null)
            .Select(row => new { row.Id,row.MaterialIssueId }).SingleAsync());
        var physicalMovementsBefore = await Query(options,db => db.StockMovements.CountAsync());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        async Task<FifoInput> Prepare(Guid employeeId, string role, string suffix, decimal? quantity=null, Guid? ownership=null)
        {
            var subject = await Query(options, db => db.EmployeeIdentityMappings
                .Where(row => row.CompanyId == companyId && row.EmployeeId == employeeId && row.IsActive)
                .Select(row => row.Subject).SingleAsync());
            var assignment = await Query(options, db => db.EmployeeRoleAssignments
                .Where(row => row.CompanyId == companyId && row.EmployeeId == employeeId && row.Role!.Code == role
                    && row.EffectiveFrom <= today && (!row.EffectiveTo.HasValue || row.EffectiveTo >= today))
                .Select(row => new { row.Id,row.AssignmentType }).SingleAsync());
            Assert.Equal("FULL",assignment.AssignmentType);
            var issueId = Guid.NewGuid();
            var key = "direct-fifo-" + suffix + "-" + issueId.ToString("N");
            var input = new FifoInput(issueId,Guid.NewGuid(),employeeId,subject,role,assignment.Id,key,
                SHA256.HashData(Encoding.UTF8.GetBytes(key)));
            await CreateDirectFifoInput(runtime,input,sourceLine.MaterialIssueId,sourceLine.Id,quantity,ownership);
            return input;
        }
        // A second valid ownership account has no receipt-backed cost layers.
        // The function must not borrow the first account's available layers.
        var otherOwnership=Guid.NewGuid();
        await using(var fixture=new NexaErpDbContext(options))
            await fixture.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO advance.inventory_ownership_accounts
                SELECT (jsonb_populate_record(NULL::advance.inventory_ownership_accounts,
                  to_jsonb(o)||jsonb_build_object('Id',{otherOwnership},
                    'AccountCode',{'F'.ToString()+otherOwnership.ToString("N")}))).*
                FROM advance.inventory_ownership_accounts o
                JOIN advance.material_issue_lines il ON il."OwnershipAccountId"=o."Id" AND il."CompanyId"=o."CompanyId"
                WHERE il."Id"={sourceLine.Id}
                """);
        var wrongPool=await Prepare(context.FirstOperatorId,"STORES_EXECUTIVE","other-ownership",.10m,otherOwnership);
        var wrongPoolResult=await CallDirectFifo(runtime,companyId,wrongPool);
        Assert.False(wrongPoolResult.Committed);
        Assert.Equal(PostgresErrorCodes.RaiseException,wrongPoolResult.SqlState);
        Assert.Equal("Insufficient FIFO cost-layer quantity in this ownership and currency pool.",wrongPoolResult.Message);
        Assert.Equal(2.63m,(await ReadRemaining(options)).Sum(x=>x.Remaining));
        Assert.Equal(0,await Query(options,db=>db.FifoCostConsumptions.CountAsync(x=>x.MaterialIssueLineId==wrongPool.LineId)));
        // Consume restored availability through the same restricted costing boundary.
        // This clone has no physical posting claim; retain exactly the last unit for the race.
        foreach(var row in remaining.SkipLast(1))
        {
            var drain=await Prepare(context.FirstOperatorId,"STORES_EXECUTIVE","drain-"+row.Id,row.Remaining);
            var result=await CallDirectFifo(runtime,companyId,drain);
            Assert.True(result.Committed,result.Message);
        }
        Assert.Equal(1m,(await ReadRemaining(options)).Sum(x=>x.Remaining));
        var firstInput = await Prepare(context.FirstOperatorId,"STORES_EXECUTIVE","first");
        var secondInput = await Prepare(context.SecondOperatorId,"STORES_ASSISTANT","second");
        await using (var restricted = new NpgsqlConnection(runtime))
        {
            await restricted.OpenAsync();
            await using var forbidden = new NpgsqlCommand("SELECT count(*) FROM advance.fifo_cost_consumptions",restricted);
            var denial = await Assert.ThrowsAsync<PostgresException>(() => forbidden.ExecuteScalarAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege,denial.SqlState);
        }
        string Named(string name) => new NpgsqlConnectionStringBuilder(runtime)
            { ApplicationName = name }.ConnectionString;
        await using var observer = new NpgsqlConnection(cloneAdmin);
        await observer.OpenAsync();
        var gateKey = $"FIFO:{companyId}:{oldest.ItemId}";
        await using (var gate = new NpgsqlCommand("SELECT pg_advisory_lock(hashtextextended(@key,0))",observer))
        {
            gate.Parameters.AddWithValue("key",gateKey);
            await gate.ExecuteNonQueryAsync();
        }
        var observations = new List<object>();
        var logStart = context.ReadPostgresLog().Length;
        Task<FifoCallResult>? firstTask = null;
        Task<FifoCallResult>? secondTask = null;
        string? observationError = null;
        try
        {
            firstTask = CallDirectFifo(Named("race-direct-fifo-first"),companyId,firstInput);
            var firstPid = await ObserveEntityWriteWait(observer,"race-direct-fifo-first",[observer.ProcessID],
                observations,"consume_fifo_for_issue","SELECT");
            secondTask = CallDirectFifo(Named("race-direct-fifo-second"),companyId,secondInput);
            await ObserveEntityWriteWait(observer,"race-direct-fifo-second",[observer.ProcessID,firstPid],
                observations,"consume_fifo_for_issue","SELECT");
        }
        catch (Exception error) { observationError = error.ToString(); }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(hashtextextended(@key,0))",observer);
            release.Parameters.AddWithValue("key",gateKey);
            await release.ExecuteNonQueryAsync();
            if (firstTask is not null) await firstTask;
            if (secondTask is not null) await secondTask;
        }
        var first = firstTask is null ? null : await firstTask;
        var second = secondTask is null ? null : await secondTask;
        var retry = await CallDirectFifo(Named("race-direct-fifo-retry"),companyId,secondInput);
        var reentry = await CallDirectFifo(Named("race-direct-fifo-reentry"),companyId,firstInput);
        var log = context.ReadPostgresLog()[logStart..];
        var lineIds = new[] { firstInput.LineId,secondInput.LineId };
        var state = await Query(options, async db => new
        {
            Consumptions = await db.FifoCostConsumptions.Where(row => lineIds.Contains(row.MaterialIssueLineId))
                .Select(row => new { row.MaterialIssueLineId,row.FifoInventoryCostLayerId,row.Quantity,row.UnitCost,row.ConsumedValue }).ToListAsync(),
            Remainders = (await ReadRemaining(options)).Select(x=>x.Remaining).ToList()
        });
        var ledger = await Query(options,db => db.Database.SqlQueryRaw<string>("""
            SELECT jsonb_build_object('requests',count(DISTINCT q."CommandId"),
              'receipts',count(r."ReceiptId"),
              'orphanRequests',count(*) FILTER (WHERE r."ReceiptId" IS NULL))::text AS "Value"
            FROM advance.command_requests q
            LEFT JOIN advance.command_receipts r ON r."CommandId"=q."CommandId"
            WHERE q."IdempotencyKeySha256" IN (@first,@second)
            """,new NpgsqlParameter("first",SHA256.HashData(Encoding.UTF8.GetBytes(firstInput.Key))),
                new NpgsqlParameter("second",SHA256.HashData(Encoding.UTF8.GetBytes(secondInput.Key)))).SingleAsync());
        var evidence = Path.Combine(FindRepositoryRoot(),"local-evidence","item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence,"direct-fifo-postgresql.log"),log);
        await File.WriteAllTextAsync(Path.Combine(evidence,"direct-fifo.json"),
            JsonSerializer.Serialize(new { Database = cloneName,
                Fixture = "Two unposted costing input headers/lines in a clone; not two completed API issues.",
                Oldest = oldest, FirstInput = firstInput, SecondInput = secondInput, WrongOwnershipPool = wrongPoolResult,
                Observations = observations, ObservationError = observationError,
                First = first, Second = second, Retry = retry, Reentry = reentry, State = state, Ledger = ledger },
                new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(observationError is null,observationError);
        Assert.DoesNotContain("40P01",log,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected",log,StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first.Committed,first.Message);
        Assert.Equal(1,first.Affected);
        Assert.False(second.Committed);
        Assert.Equal(PostgresErrorCodes.SerializationFailure,second.SqlState);
        Assert.False(retry.Committed);
        Assert.Equal(PostgresErrorCodes.RaiseException,retry.SqlState);
        Assert.Equal("Insufficient FIFO cost-layer quantity in this ownership and currency pool.",retry.Message);
        Assert.False(reentry.Committed);
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege,reentry.SqlState);
        var consumption = Assert.Single(state.Consumptions);
        Assert.Equal(firstInput.LineId,consumption.MaterialIssueLineId);
        Assert.Equal(oldest.Id,consumption.FifoInventoryCostLayerId);
        Assert.Equal(1m,consumption.Quantity);
        Assert.Equal(oldest.UnitCost,consumption.UnitCost);
        Assert.Equal(oldest.UnitCost,consumption.ConsumedValue);
        Assert.All(state.Remainders,value => Assert.True(value >= 0m));
        Assert.Equal(0m,state.Remainders.Sum());
        using (var document = JsonDocument.Parse(ledger))
        {
            Assert.Equal(1,document.RootElement.GetProperty("requests").GetInt32());
            Assert.Equal(1,document.RootElement.GetProperty("receipts").GetInt32());
            Assert.Equal(0,document.RootElement.GetProperty("orphanRequests").GetInt32());
        }
        Assert.Equal(physicalMovementsBefore,await Query(options,db => db.StockMovements.CountAsync()));
        // The source remains untouched; its subsequent report helper can issue .05.
        Assert.Equal(2.63m,(await ReadRemaining(context.Options)).Sum(x=>x.Remaining));
    }

    private static async Task CreateDirectFifoInput(string runtime, FifoInput input, Guid sourceIssue, Guid sourceLine, decimal? quantity=null, Guid? ownership=null)
    {
        await using var connection = new NpgsqlConnection(runtime);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        // Boundary fixtures only: no stock posting or completed API issue is
        // claimed. The copied immutable source records are never updated.
        await using var insert = new NpgsqlCommand("""
            INSERT INTO advance.material_issues
              SELECT (jsonb_populate_record(NULL::advance.material_issues,
                to_jsonb(i)||jsonb_build_object('Id',@issue,'IssueNumber',@number,
                  'StockPostingBatchId',NULL,'Status','ISSUED','IdempotencyKey',@key,
                  'RequestFingerprint',@hash,'IssuedByEmployeeId',@actor,'ActorRoleCode',@role,
                  'ResolvedRoleAssignmentId',@assignment,'ResolvedRoleAssignmentType','FULL',
                  'CreatedAt',clock_timestamp(),'CreatedBy',@subject,'UpdatedAt',NULL,'UpdatedBy',NULL,'Version',0))).*
              FROM advance.material_issues i WHERE i."Id"=@source_issue;
            INSERT INTO advance.material_issue_lines
              SELECT (jsonb_populate_record(NULL::advance.material_issue_lines,
                to_jsonb(l)||jsonb_build_object('Id',@line,'MaterialIssueId',@issue,'QuantityBase',coalesce(@quantity,l."QuantityBase"),'OwnershipAccountId',coalesce(@ownership,l."OwnershipAccountId"),
                  'CreatedAt',clock_timestamp(),'CreatedBy',@subject))).*
              FROM advance.material_issue_lines l WHERE l."Id"=@source_line;
            """,connection,transaction);
        insert.Parameters.AddWithValue("issue",input.IssueId);
        insert.Parameters.AddWithValue("number","FIFO-" + input.IssueId.ToString("N"));
        insert.Parameters.AddWithValue("key",input.Key);
        insert.Parameters.AddWithValue("hash",Convert.ToHexString(input.RequestHash).ToLowerInvariant());
        insert.Parameters.AddWithValue("actor",input.ActorId);
        insert.Parameters.AddWithValue("role",input.Role);
        insert.Parameters.AddWithValue("assignment",input.AssignmentId);
        insert.Parameters.AddWithValue("subject",input.Subject);
        insert.Parameters.AddWithValue("source_issue",sourceIssue);
        insert.Parameters.AddWithValue("ownership",NpgsqlDbType.Uuid,(object?)ownership??DBNull.Value);
        insert.Parameters.AddWithValue("quantity",NpgsqlDbType.Numeric,(object?)quantity??DBNull.Value);
        insert.Parameters.AddWithValue("line",input.LineId);
        insert.Parameters.AddWithValue("source_line",sourceLine);
        Assert.Equal(2,await insert.ExecuteNonQueryAsync());
        await transaction.CommitAsync();
    }

    private static async Task<FifoCallResult> CallDirectFifo(string runtime, Guid companyId, FifoInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var connection = new NpgsqlConnection(runtime);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        int? affected = null;
        try
        {
            Guid commandId;
            await using (var register = new NpgsqlCommand("""
                SELECT advance.register_command_request('SESS_PVT_LTD','MaterialIssue.Issue',
                  @key,@hash,@actor,'https://issuer.purchase-flow.test',@subject,@role,@assignment)
                """,connection,transaction))
            {
                register.Parameters.AddWithValue("key",SHA256.HashData(Encoding.UTF8.GetBytes(input.Key)));
                register.Parameters.AddWithValue("hash",input.RequestHash);
                register.Parameters.AddWithValue("actor",input.ActorId);
                register.Parameters.AddWithValue("subject",input.Subject);
                register.Parameters.AddWithValue("role",input.Role);
                register.Parameters.AddWithValue("assignment",input.AssignmentId);
                commandId = (Guid)(await register.ExecuteScalarAsync())!;
            }
            await using (var consume = new NpgsqlCommand("""
                SELECT advance.consume_fifo_for_issue(@company,@issue,@actor,@role,@assignment,'FULL',@subject)
                """,connection,transaction))
            {
                consume.CommandTimeout = 60;
                consume.Parameters.AddWithValue("company",companyId);
                consume.Parameters.AddWithValue("issue",input.IssueId);
                consume.Parameters.AddWithValue("actor",input.ActorId);
                consume.Parameters.AddWithValue("role",input.Role);
                consume.Parameters.AddWithValue("assignment",input.AssignmentId);
                consume.Parameters.AddWithValue("subject",input.Subject);
                affected = (int)(await consume.ExecuteScalarAsync())!;
            }
            var response = JsonSerializer.Serialize(new { input.IssueId,Costed = true });
            await using (var receipt = new NpgsqlCommand(
                "SELECT advance.commit_command_receipt(@command,@business,@response,@receipt)",connection,transaction))
            {
                receipt.Parameters.AddWithValue("command",commandId);
                receipt.Parameters.AddWithValue("business",SHA256.HashData(Encoding.UTF8.GetBytes(response)));
                receipt.Parameters.AddWithValue("response",NpgsqlDbType.Jsonb,response);
                receipt.Parameters.AddWithValue("receipt",Guid.NewGuid());
                await receipt.ExecuteScalarAsync();
            }
            await transaction.CommitAsync();
            return new(affected,true,null,null,stopwatch.Elapsed.TotalSeconds);
        }
        catch (PostgresException error)
        {
            try { await transaction.RollbackAsync(); }
            catch (InvalidOperationException) { /* PostgreSQL may already have aborted COMMIT. */ }
            return new(affected,false,error.SqlState,error.MessageText,stopwatch.Elapsed.TotalSeconds);
        }
    }
}
