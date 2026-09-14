using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
#if CONCURRENCY_WITNESS
    [Fact]
    public async Task ConcurrentAdvanceAndBillPaymentCannotExceedPurchaseOrderCash()
    {
        var observed = false;
        await RunCompletePurchaseFlow(mixedRun: async context =>
        {
            observed = true;
            var company = Guid.Parse("70000000-0000-0000-0000-000000000001");
            var po = await Query(context.Options, db => db.PurchaseOrders.AsNoTracking()
                .Where(x => x.CompanyId == company && x.IsCurrentVersion && x.Status == "Issued" &&
                    x.TotalPayableValue == 5900m).SingleAsync());
            string Named(string name) => new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
                { ApplicationName = name, Pooling = false }.ConnectionString;
            await using var advanceHost = await PurchaseFlowHost.StartAsync(Named("cash-race-advance"),
                await BankAdviceActor(context.Options), true, true);
            await using var paymentHost = await PurchaseFlowHost.StartAsync(Named("cash-race-payment"),
                await BankAdviceActor(context.Options), true, true);
            const string path = "/api/v1/accounts/vendor-financial-evidence/";
            var payables = await Get<VendorPayableView[]>(paymentHost.Client, path + "payables");
            var payable = Assert.Single(payables, x => x.PurchaseOrderId == po.Id);
            var advance = new RecordVendorAdvanceRequest(po.Id, DateOnly.FromDateTime(DateTime.UtcNow),
                100m, "INR", "CASH-RACE-ADVANCE", "evidence/cash-race-advance", "cash-race-advance");
            var payment = new RecordVendorPaymentRequest(po.VendorId, DateOnly.FromDateTime(DateTime.UtcNow),
                payable.OutstandingValue, "INR", "CASH-RACE-PAYMENT", "evidence/cash-race-payment",
                [new(payable.VendorBillId, payable.OutstandingValue)], "cash-race-payment");
            await using var db = new NexaErpDbContext(context.Options);
            await using var gate = new NpgsqlConnection(db.Database.GetConnectionString());
            await using var inspect = new NpgsqlConnection(db.Database.GetConnectionString());
            await gate.OpenAsync();
            await inspect.OpenAsync();
            async Task<JsonElement> Snapshot()
            {
                await using var command = new NpgsqlCommand("""
                    SELECT jsonb_build_object(
                      'Advances',(SELECT count(*) FROM advance.vendor_advances),
                      'Reversals',(SELECT count(*) FROM advance.vendor_advance_reversals),
                      'Payments',(SELECT count(*) FROM advance.vendor_payments),
                      'Allocations',(SELECT count(*) FROM advance.vendor_payment_allocations),
                      'Audits',(SELECT count(*) FROM advance.audit_logs),
                      'Requests',(SELECT count(*) FROM advance.command_requests),
                      'Receipts',(SELECT count(*) FROM advance.command_receipts),
                      'CashPaid',cash."AdvanceAmount"+cash."BillPaymentAmount")::text
                    FROM advance.vendor_po_cash_totals(@company,@root,'INR',@vendor) cash
                    """, inspect);
                command.Parameters.AddWithValue("company", company);
                command.Parameters.AddWithValue("root", po.RootPurchaseOrderId);
                command.Parameters.AddWithValue("vendor", po.VendorId);
                using var result = JsonDocument.Parse((string)(await command.ExecuteScalarAsync())!);
                return result.RootElement.Clone();
            }
            var before = await Snapshot();
            await using var held = await gate.BeginTransactionAsync();
            await using (var command = new NpgsqlCommand(
                "SELECT pg_advisory_xact_lock(hashtextextended(@company::text||':VENDOR-CASH',0))", gate, held))
            {
                command.Parameters.AddWithValue("company", company);
                await command.ExecuteNonQueryAsync();
            }
            var logStart = context.ReadPostgresLog().Length;
            var observations = new List<object>();
            Task<RaceHttpResult>? advanceTask = null;
            Task<RaceHttpResult>? paymentTask = null;
            string? observationError = null;
            try
            {
                advanceTask = TimedRacePost(advanceHost.Client, path + "advances", advance);
                await ObserveEntityWriteWait(inspect, "cash-race-advance", [gate.ProcessID],
                    observations, "record_vendor_advance", "SELECT");
                paymentTask = TimedRacePost(paymentHost.Client, path + "payments", payment);
                await ObserveEntityWriteWait(inspect, "cash-race-payment", [gate.ProcessID],
                    observations, "record_vendor_payment", "SELECT");
            }
            catch (Exception error) { observationError = error.ToString(); }
            finally
            {
                await held.RollbackAsync();
                if (advanceTask is not null) await advanceTask;
                if (paymentTask is not null) await paymentTask;
            }
            var advanceResult = advanceTask is null ? null : await advanceTask;
            var paymentResult = paymentTask is null ? null : await paymentTask;
            var afterRace = await Snapshot();
            var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item11");
            Directory.CreateDirectory(evidence);
            async Task WriteEvidence(object result) => await File.WriteAllTextAsync(
                Path.Combine(evidence, "po-cash-concurrency.json"),
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            await WriteEvidence(new { Before = before, Advance = advanceResult, Payment = paymentResult,
                AfterRace = afterRace, Observations = observations, ObservationError = observationError });
            Assert.True(observationError is null, observationError);
            Assert.NotNull(advanceResult);
            Assert.NotNull(paymentResult);
            Assert.Single(new[] { advanceResult, paymentResult }, x => x.Status == HttpStatusCode.Created);
            Assert.Single(new[] { advanceResult, paymentResult }, x => x.Status == HttpStatusCode.Conflict);
            Assert.True(afterRace.GetProperty("CashPaid").GetDecimal() <= po.TotalPayableValue);
            foreach (var key in new[] { "Audits", "Requests", "Receipts" })
                Assert.Equal(before.GetProperty(key).GetInt64() + 1, afterRace.GetProperty(key).GetInt64());

            RaceHttpResult refusedRetry;
            VendorPaymentView settled;
            if (advanceResult.Status == HttpStatusCode.Created)
            {
                var recorded = JsonSerializer.Deserialize<VendorAdvanceView>(advanceResult.Body)!;
                refusedRetry = await TimedRacePost(paymentHost.Client, path + "payments", payment);
                Assert.Equal(HttpStatusCode.Conflict, refusedRetry.Status);
                Assert.Equal(afterRace.GetRawText(), (await Snapshot()).GetRawText());
                await Post<VendorAdvanceView>(advanceHost.Client, path + "advances/" + recorded.Id + "/reverse",
                    new ReverseVendorAdvanceRequest("Return the competing advance before full settlement",
                        "cash-race-advance-return"));
                settled = await Post<VendorPaymentView>(paymentHost.Client, path + "payments", payment);
            }
            else
            {
                refusedRetry = await TimedRacePost(advanceHost.Client, path + "advances", advance);
                Assert.Equal(HttpStatusCode.Conflict, refusedRetry.Status);
                Assert.Equal(afterRace.GetRawText(), (await Snapshot()).GetRawText());
                settled = JsonSerializer.Deserialize<VendorPaymentView>(paymentResult.Body)!;
            }
            var final = await Snapshot();
            Assert.Equal(po.TotalPayableValue, final.GetProperty("CashPaid").GetDecimal());
            var replay = await Post<VendorPaymentView>(paymentHost.Client, path + "payments", payment);
            Assert.True(replay.Replayed);
            Assert.Equal(settled.Id, replay.Id);
            Assert.Equal(final.GetRawText(), (await Snapshot()).GetRawText());
            var log = context.ReadPostgresLog()[logStart..];
            Assert.DoesNotContain("40P01", log, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("deadlock detected", log, StringComparison.OrdinalIgnoreCase);
            await File.WriteAllTextAsync(Path.Combine(evidence, "po-cash-concurrency-postgresql.log"), log);
            await WriteEvidence(new { Before = before, Advance = advanceResult, Payment = paymentResult,
                AfterRace = afterRace, Observations = observations, ObservationError = observationError,
                RefusedRetry = refusedRetry, Settled = settled, Final = final, Replay = replay,
                NoDeadlock = true });
        });
        Assert.True(observed);
    }
#endif
}
