using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
#if WORKFLOW_WITNESS
    [Fact]
    public async Task FullyPaidPurchaseOrderRefusesAnotherAdvanceWithoutLeavingEvidence()
    {
        var observed = false;
        await RunCompletePurchaseFlow(paymentRace: async context =>
        {
            var settled = await RunPaymentRace(context);
            observed = true;
            var bills = settled.Command.Allocations.Select(x => x.VendorBillId).ToArray();
            var po = await Query(context.Options, db => db.VendorBills
                .Where(x => bills.Contains(x.Id)).OrderBy(x => x.TotalPayableValue)
                .Select(x => x.PurchaseOrderId).FirstAsync());
            await using var db = new NexaErpDbContext(context.Options);
            await using var source = new NpgsqlConnection(db.Database.GetConnectionString());
            await source.OpenAsync();
            async Task<JsonElement> Snapshot()
            {
                await using var command = new NpgsqlCommand("""
                    WITH chosen AS(SELECT * FROM advance.purchase_orders WHERE "Id"=@po),
                    versions AS(SELECT p."Id" FROM advance.purchase_orders p JOIN chosen c
                        ON c."CompanyId"=p."CompanyId" AND c."RootPurchaseOrderId"=p."RootPurchaseOrderId"),
                    amounts AS(
                        SELECT coalesce((SELECT sum(a."Amount") FROM advance.vendor_advances a
                            WHERE a."PurchaseOrderId" IN(SELECT "Id" FROM versions)
                            AND NOT EXISTS(SELECT 1 FROM advance.vendor_advance_reversals r
                                WHERE r."VendorAdvanceId"=a."Id")),0) advance,
                        coalesce((SELECT sum(a."Amount") FROM advance.vendor_payment_allocations a
                            JOIN advance.vendor_bills b ON b."Id"=a."VendorBillId"
                            WHERE b."PurchaseOrderId" IN(SELECT "Id" FROM versions)),0) paid)
                    SELECT jsonb_build_object('PurchaseOrderId',c."Id",'RootPurchaseOrderId',c."RootPurchaseOrderId",
                        'CurrencyCode',c."CurrencyCode",'PurchaseOrderValue',c."TotalPayableValue",
                        'ActiveAdvanceAmount',a.advance,'BillPaymentAmount',a.paid,'CashPaid',a.advance+a.paid,
                        'AdvanceRows',(SELECT count(*) FROM advance.vendor_advances),
                        'Requests',(SELECT count(*) FROM advance.command_requests),
                        'Receipts',(SELECT count(*) FROM advance.command_receipts),
                        'Audits',(SELECT count(*) FROM advance.audit_logs))::text
                    FROM chosen c CROSS JOIN amounts a
                    """, source);
                command.Parameters.AddWithValue("po", po);
                using var json = JsonDocument.Parse((string)(await command.ExecuteScalarAsync())!);
                return json.RootElement.Clone();
            }
            var before = await Snapshot();
            Assert.Equal(before.GetProperty("PurchaseOrderValue").GetDecimal(),
                before.GetProperty("CashPaid").GetDecimal());
            var actor = await BankAdviceActor(context.Options);
            await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
            var availableOrders = await Get<VendorAdvancePurchaseOrderOption[]>(host.Client,
                "/api/v1/accounts/vendor-financial-evidence/advance-purchase-orders");
            var response = await TimedRacePost(host.Client,
                "/api/v1/accounts/vendor-financial-evidence/advances",
                new RecordVendorAdvanceRequest(po, DateOnly.FromDateTime(DateTime.UtcNow), 1m, "INR",
                    "OVER-CASH-CAP", "evidence/cash-cap-refusal", "cash-cap-after-settlement"));
            var after = await Snapshot();
            var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item11");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "po-cash-cap.json"),
                JsonSerializer.Serialize(new { Before = before, AvailableOrders = availableOrders, Response = response, After = after },
                    new JsonSerializerOptions { WriteIndented = true }));
            Assert.True(response.Status == HttpStatusCode.Conflict, response.Body);
            Assert.DoesNotContain(availableOrders, x => x.PurchaseOrderId == po);
            foreach (var name in new[] { "CashPaid", "AdvanceRows", "Requests", "Receipts", "Audits" })
                Assert.Equal(before.GetProperty(name).GetDecimal(), after.GetProperty(name).GetDecimal());
            return settled;
        });
        Assert.True(observed);
    }
#endif
}
