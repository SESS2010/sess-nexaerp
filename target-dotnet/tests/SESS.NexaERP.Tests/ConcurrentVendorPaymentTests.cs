using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record PaymentRaceContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection, RecordVendorPaymentRequest OriginalCommand,
        Guid ApproverId, Func<string> ReadPostgresLog);
    private sealed record PaymentRaceResult(VendorPaymentView Payment, RecordVendorPaymentRequest Command);

#if CONCURRENCY_WITNESS
    [Fact]
    public async Task ReversedBillOrderCannotDeadlockOrOverpayConcurrentSettlements()
    {
        var observed = false;
        await RunCompletePurchaseFlow(paymentRace: context =>
        {
            observed = true;
            return RunPaymentRace(context);
        });
        Assert.True(observed, "The full-settlement payment callback must execute.");
    }

#endif
    private static async Task<PaymentRaceResult> RunPaymentRace(PaymentRaceContext context)
    {
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var assignments = await Query(context.Options, async db =>
            (await db.EmployeeRoleAssignments.AsNoTracking().Include(row => row.Role)
                .Where(row => row.CompanyId == companyId && row.EffectiveTo == null).ToListAsync())
            .ToDictionary(row => TaxWorkflowUser.AssignmentKey(row.EmployeeId, row.Role!.Code),
                row => new EffectiveRoleAssignment(row.Id, row.Role!.Code, row.AssignmentType)));
        var subject = await Query(context.Options, db => db.EmployeeIdentityMappings
            .Where(row => row.CompanyId == companyId && row.EmployeeId == context.ApproverId && row.IsActive)
            .Select(row => row.Subject).SingleAsync());
        var firstActor = new TaxWorkflowUser(context.ApproverId, subject, "ACCOUNTS_MANAGER", assignments);
        var secondActor = new TaxWorkflowUser(context.ApproverId, subject, "ACCOUNTS_MANAGER", assignments);
        Assert.All(firstActor.EffectiveRoleAssignments.Concat(secondActor.EffectiveRoleAssignments), assignment =>
        {
            Assert.NotEqual(Guid.Empty, assignment.AssignmentId);
            Assert.Equal("FULL", assignment.AssignmentType);
        });
        string Named(string name) => new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
        { ApplicationName = name, Pooling = false }.ConnectionString;
        await using var firstHost = await PurchaseFlowHost.StartAsync(Named("race-payment-first"), firstActor, true, true);
        await using var secondHost = await PurchaseFlowHost.StartAsync(Named("race-payment-second"), secondActor, true, true);
        var payablePath = $"/api/v1/accounts/vendor-financial-evidence/payables?vendorId={context.OriginalCommand.VendorId}";
        var payableIds = context.OriginalCommand.Allocations.Select(row => row.VendorBillId).ToArray();
        var before = (await Get<VendorPayableView[]>(firstHost.Client, payablePath))
            .Where(row => payableIds.Contains(row.VendorBillId))
            .OrderBy(row => row.VendorBillId.ToString("D"), StringComparer.Ordinal).ToArray();
        Assert.Equal(2, before.Length);
        Assert.All(before, row => Assert.True(row.OutstandingValue > 0m));
        var allocations = before.Select(row => new VendorPaymentAllocationInput(row.VendorBillId, row.OutstandingValue)).ToArray();
        var firstCommand = context.OriginalCommand with
        { Amount = allocations.Sum(row => row.Amount), Allocations = allocations };
        var secondCommand = firstCommand with
        {
            Allocations = allocations.Reverse().ToArray(),
            IdempotencyKey = firstCommand.IdempotencyKey + "-second-session",
            PaymentReference = "UTR-CONCURRENT-SETTLEMENT",
            EvidenceObjectKey = "evidence/concurrent-settlement"
        };

        await using var db = new NexaErpDbContext(context.Options);
        await using var gateA = new NpgsqlConnection(db.Database.GetConnectionString());
        await using var gateB = new NpgsqlConnection(db.Database.GetConnectionString());
        await using var observer = new NpgsqlConnection(db.Database.GetConnectionString());
        await gateA.OpenAsync(); await gateB.OpenAsync(); await observer.OpenAsync();
        await using var transactionA = await gateA.BeginTransactionAsync();
        await using var transactionB = await gateB.BeginTransactionAsync();
        await HoldPaymentBill(gateA, transactionA, companyId, allocations[0].VendorBillId);
        await HoldPaymentBill(gateB, transactionB, companyId, allocations[1].VendorBillId);
        var path = "/api/v1/accounts/vendor-financial-evidence/payments";
        var observations = new List<object>();
        var logStart = context.ReadPostgresLog().Length;
        Task<RaceHttpResult>? firstTask = null;
        Task<RaceHttpResult>? secondTask = null;
        string? observationError = null;
        var releasedA = false;
        try
        {
            firstTask = TimedRacePost(firstHost.Client, path, firstCommand);
            var firstPid = await ObserveEntityWriteWait(observer, "race-payment-first",
                [gateA.ProcessID], observations, "record_vendor_payment", "SELECT");
            secondTask = TimedRacePost(secondHost.Client, path, secondCommand);
            // Before a lock-order fix, the reversed request waits on B. A sorted
            // implementation waits on A instead. Record the actual backend blockers.
            var secondPid = await ObserveEntityWriteWait(observer, "race-payment-second",
                [gateA.ProcessID, gateB.ProcessID, firstPid], observations, "record_vendor_payment", "SELECT");
            await transactionA.RollbackAsync();
            releasedA = true;
            // First now holds A and waits for B (possibly behind the second
            // backend's tuple lock). Releasing B exposes a real inverse-order cycle.
            await ObserveEntityWriteWait(observer, "race-payment-first",
                [gateB.ProcessID, secondPid], observations, "record_vendor_payment", "SELECT");
        }
        catch (Exception error) { observationError = error.ToString(); }
        finally
        {
            if (!releasedA) await transactionA.RollbackAsync();
            await transactionB.RollbackAsync();
            if (firstTask is not null) await firstTask;
            if (secondTask is not null) await secondTask;
        }
        var first = firstTask is null ? null : await firstTask;
        var second = secondTask is null ? null : await secondTask;
        var retry = await TimedRacePost(secondHost.Client, path, secondCommand);
        var replay = await TimedRacePost(firstHost.Client, path, firstCommand);
        var after = await Get<VendorPayableView[]>(firstHost.Client, payablePath);
        var log = context.ReadPostgresLog()[logStart..];
        await using var ledgerCommand = new NpgsqlCommand("""
            WITH balances AS (
              SELECT b."Id",b."TotalPayableValue",
                coalesce((SELECT sum(a."Amount") FROM advance.vendor_advance_adjustments a
                  WHERE a."VendorBillId"=b."Id"),0)
                -coalesce((SELECT sum(r."Amount") FROM advance.vendor_advance_adjustment_restorations r
                  JOIN advance.vendor_advance_adjustments a ON a."Id"=r."VendorAdvanceAdjustmentId"
                  WHERE a."VendorBillId"=b."Id"),0) adjusted,
                coalesce((SELECT sum(a."Amount") FROM advance.vendor_payment_allocations a
                  WHERE a."CompanyId"=@company AND a."VendorBillId"=b."Id"),0) paid
              FROM advance.vendor_bills b WHERE b."CompanyId"=@company AND b."Id"=ANY(@bills))
            SELECT jsonb_build_object(
              'Payments',(SELECT coalesce(jsonb_agg(jsonb_build_object(
                'Id',p."Id",'Amount',p."Amount",'IdempotencyKey',p."IdempotencyKey")),'[]'::jsonb)
                FROM advance.vendor_payments p WHERE p."CompanyId"=@company AND p."IdempotencyKey"=ANY(@keys)),
              'Allocations',(SELECT coalesce(jsonb_agg(jsonb_build_object(
                'VendorBillId',a."VendorBillId",'Amount',a."Amount")),'[]'::jsonb)
                FROM advance.vendor_payment_allocations a JOIN advance.vendor_payments p ON p."Id"=a."VendorPaymentId"
                WHERE p."CompanyId"=@company AND p."IdempotencyKey"=ANY(@keys)),
              'Balances',(SELECT jsonb_agg(jsonb_build_object(
                'VendorBillId',"Id",'Adjusted',adjusted,'Paid',paid,
                'Outstanding',"TotalPayableValue"-adjusted-paid)) FROM balances)
            )::text
            """, observer);
        ledgerCommand.Parameters.AddWithValue("company", companyId);
        ledgerCommand.Parameters.AddWithValue("bills", allocations.Select(row => row.VendorBillId).ToArray());
        ledgerCommand.Parameters.AddWithValue("keys", new[] { firstCommand.IdempotencyKey, secondCommand.IdempotencyKey });
        using var ledger = JsonDocument.Parse((string)(await ledgerCommand.ExecuteScalarAsync())!);
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "payment-settlement-postgresql.log"), log);
        await File.WriteAllTextAsync(Path.Combine(evidence, "payment-settlement.json"), JsonSerializer.Serialize(
            new { Before = before, FirstCommand = firstCommand, SecondCommand = secondCommand,
                GateA = gateA.ProcessID, GateB = gateB.ProcessID, Observations = observations,
                ObservationError = observationError, First = first, Second = second,
                Retry = retry, Replay = replay, AfterPayables = after, Ledger = ledger.RootElement },
            new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(observationError is null, observationError + $" First: {first?.Body}; second: {second?.Body}");
        Assert.DoesNotContain("40P01", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected", log, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first.Status == HttpStatusCode.Created, first.Body);
        Assert.True(second.Status == HttpStatusCode.Conflict, second.Body);
        Assert.True(retry.Status == HttpStatusCode.Conflict, retry.Body);
        Assert.Contains("exceeds outstanding value", retry.Body, StringComparison.OrdinalIgnoreCase);
        Assert.True(replay.Status == HttpStatusCode.Created, replay.Body);
        Assert.True(JsonSerializer.Deserialize<VendorPaymentView>(replay.Body)!.Replayed);
        Assert.DoesNotContain(after, row => payableIds.Contains(row.VendorBillId));
        var payments = ledger.RootElement.GetProperty("Payments");
        Assert.Equal(1, payments.GetArrayLength());
        Assert.Equal(firstCommand.IdempotencyKey, payments[0].GetProperty("IdempotencyKey").GetString());
        Assert.Equal(firstCommand.Amount, payments[0].GetProperty("Amount").GetDecimal());
        var posted = ledger.RootElement.GetProperty("Allocations").EnumerateArray().ToArray();
        Assert.Equal(allocations.Length, posted.Length);
        Assert.Equal(firstCommand.Amount, posted.Sum(row => row.GetProperty("Amount").GetDecimal()));
        foreach (var allocation in allocations)
            Assert.Equal(allocation.Amount, Assert.Single(posted, row =>
                row.GetProperty("VendorBillId").GetGuid() == allocation.VendorBillId).GetProperty("Amount").GetDecimal());
        var balances = ledger.RootElement.GetProperty("Balances").EnumerateArray().ToArray();
        Assert.Equal(2, balances.Length);
        Assert.All(balances, row => Assert.Equal(0m, row.GetProperty("Outstanding").GetDecimal()));
        return new(JsonSerializer.Deserialize<VendorPaymentView>(first.Body)!, firstCommand);
    }

    private static async Task<string> PaymentFunctionMetadata(DbContextOptions<NexaErpDbContext> options)
    {
        var value = await Query(options, db => db.Database.SqlQueryRaw<string>("""
            SELECT jsonb_build_object('oid',p.oid::bigint,'owner',r.rolname,
              'securityDefiner',p.prosecdef,'configuration',to_jsonb(p.proconfig),
              'acl',coalesce(array_to_string(p.proacl,','),''),
              'runtimeExecute',has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE'),
              'runtimeTableSelect',has_table_privilege('nexa_erp_runtime','advance.vendor_payments','SELECT')
            )::text AS "Value"
            FROM pg_proc p JOIN pg_roles r ON r.oid=p.proowner
            WHERE p.oid='advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)'::regprocedure
            """).SingleAsync());
        using var document = JsonDocument.Parse(value);
        Assert.Equal("nexa_erp_owner", document.RootElement.GetProperty("owner").GetString());
        Assert.True(document.RootElement.GetProperty("securityDefiner").GetBoolean());
        Assert.True(document.RootElement.GetProperty("runtimeExecute").GetBoolean());
        Assert.False(document.RootElement.GetProperty("runtimeTableSelect").GetBoolean());
        return value;
    }
    private static async Task HoldPaymentBill(NpgsqlConnection connection, NpgsqlTransaction transaction,
        Guid companyId, Guid billId)
    {
        await using var command = new NpgsqlCommand("""
            SELECT "Id" FROM advance.vendor_bills
            WHERE "CompanyId"=@company AND "Id"=@id FOR NO KEY UPDATE
            """, connection, transaction);
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("id", billId);
        Assert.Equal(billId, Assert.IsType<Guid>(await command.ExecuteScalarAsync()));
    }
}