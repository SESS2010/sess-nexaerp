using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record RevisionReceiptWitnessContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection, Guid PriorId, Guid RevisionId);

#if WORKFLOW_WITNESS
    [Fact]
    public async Task IssuedPurchaseOrderAmendmentMustRetainPreviouslyReceivedQuantity()
    {
        await RunPurchaseOrderRevisionCashWitness(async context =>
        {
            await using var source = new NexaErpDbContext(context.Options);
            var prior = await source.PurchaseOrders.AsNoTracking().Include(x => x.Lines).SingleAsync(x => x.Id == context.PriorId);
            var revision = await source.PurchaseOrders.AsNoTracking().Include(x => x.Lines).SingleAsync(x => x.Id == context.RevisionId);
            var oldLine = Assert.Single(prior.Lines);
            var newLine = Assert.Single(revision.Lines);
            Assert.NotEqual(oldLine.Id, newLine.Id);
            Assert.Equal(prior.RootPurchaseOrderId, revision.RootPurchaseOrderId);
            Assert.Equal(oldLine.PurchaseRequisitionLineId, newLine.PurchaseRequisitionLineId);
            Assert.Equal(oldLine.PurchaseRequirementHandoffId, newLine.PurchaseRequirementHandoffId);
            Assert.Equal(oldLine.CommercialComparisonLineId, newLine.CommercialComparisonLineId);
            Assert.Equal(1m, newLine.OrderedQuantity);
            Assert.Equal(5900m, revision.TotalPayableValue);
            var employee = await source.Employees.SingleAsync(x => x.EmployeeCode == "SESS-35");
            var role = "STORES_EXECUTIVE";
            var scope = await new EfRecordScopeAuthorizer(source).AuthorizeAsync(employee.Id, role,
                new RecordScopeTarget("SESS_PVT_LTD", revision.RequestingDepartmentId,
                    revision.DeliveryWarehouseId, null, revision.OwnerEmployeeId),
                DateOnly.FromDateTime(DateTime.UtcNow), default);
            var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item29");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "receipt-revision-scope.json"),
                JsonSerializer.Serialize(new { employee.EmployeeCode, Role = role, Scope = scope },
                    new JsonSerializerOptions { WriteIndented = true }));
            Assert.True(scope.Allowed, "Actual receipt operator scope must permit this PO before testing receipt continuity.");
            var actor = await BankAdviceActor(context.Options);
            var subject = await source.EmployeeIdentityMappings.Where(x => x.CompanyId == prior.CompanyId &&
                x.EmployeeId == employee.Id && x.IsActive).Select(x => x.Subject).SingleAsync();
            actor.Set(employee.Id, subject, role);
            await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            const string key = "revision-receipt-continuity";
            var gate = await Post<GateEntryResult>(host.Client, "/api/v1/stores/gate-entries/",
                new CreateGateEntryRequest(revision.PoNumber, key + "-DC", "TRIAL-VEHICLE", "ROAD",
                    DateTimeOffset.UtcNow, "{\"packagesChecked\":true}", [new(newLine.Id, 1m)]), key + "-gate");
            gate = await Post<GateEntryResult>(host.Client, $"/api/v1/stores/gate-entries/{gate.Id}/finalize",
                new FinalizeGateEntryRequest(gate.Version, key + "-gate-final"));
            await source.Database.OpenConnectionAsync();
            async Task<JsonElement> Snapshot()
            {
                await using var command = new NpgsqlCommand("""
                    SELECT jsonb_build_object(
                      'Grns',(SELECT count(*) FROM advance.goods_receipts),
                      'Lines',(SELECT count(*) FROM advance.goods_receipt_lines),
                      'Lots',(SELECT count(*) FROM advance.inventory_lots),
                      'FifoLayers',(SELECT count(*) FROM advance.fifo_inventory_cost_layers),
                      'Postings',(SELECT count(*) FROM advance.stock_posting_batches),
                      'Movements',(SELECT count(*) FROM advance.stock_movements),
                      'Audits',(SELECT count(*) FROM advance.audit_logs),
                      'BusinessAudits',(SELECT count(*) FROM advance.audit_logs WHERE "Result"='Success'),
                      'Requests',(SELECT count(*) FROM advance.command_requests),
                      'Receipts',(SELECT count(*) FROM advance.command_receipts),
                      'RootReceived',(SELECT coalesce(sum(l."ReceivedQuantity"),0)
                        FROM advance.goods_receipt_lines l JOIN advance.goods_receipts g ON g."Id"=l."GoodsReceiptId"
                        JOIN advance.purchase_orders p ON p."Id"=g."PurchaseOrderId" AND p."CompanyId"=g."CompanyId"
                        WHERE p."CompanyId"=@company AND p."RootPurchaseOrderId"=@root
                          AND g."DocumentKind"='NORMAL' AND g."Status"='FINALIZED'
                          AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts r
                            WHERE r."CompanyId"=g."CompanyId" AND r."ReversesGoodsReceiptId"=g."Id" AND r."Status"='FINALIZED')))::text
                    """, (NpgsqlConnection)source.Database.GetDbConnection());
                command.Parameters.AddWithValue("company", prior.CompanyId);
                command.Parameters.AddWithValue("root", prior.RootPurchaseOrderId);
                using var json = JsonDocument.Parse((string)(await command.ExecuteScalarAsync())!);
                return json.RootElement.Clone();
            }
            var before = await Snapshot();
            Assert.Equal(1m, before.GetProperty("RootReceived").GetDecimal());
            var request = new CreateGoodsReceiptRequest(gate.GateEntryNumber, key + "-BILL", today,
                DateTimeOffset.UtcNow, "{\"billChecked\":true}",
                [new(gate.Lines.Single().Id, [new(1, 1m, key + "-LOT", null, today.AddMonths(-1), today.AddYears(2))], [])]);
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/stores/goods-receipts/")
                { Content = JsonContent.Create(request) };
            message.Headers.Add("Idempotency-Key", key + "-grn");
            using var response = await host.Client.SendAsync(message);
            var body = await response.Content.ReadAsStringAsync();
            object? finalize = null;
            if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
            {
                var created = JsonSerializer.Deserialize<GoodsReceiptResult>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
                finalize = await TimedRacePost(host.Client, $"/api/v1/stores/goods-receipts/{created.Id}/finalize",
                    new FinalizeGoodsReceiptRequest(created.Version, key + "-grn-final"));
            }
            var after = await Snapshot();
            await File.WriteAllTextAsync(Path.Combine(evidence, "receipt-revision-continuity.json"),
                JsonSerializer.Serialize(new { Prior = prior.Id, Revision = revision.Id, OldLine = oldLine.Id,
                    NewLine = newLine.Id, Ordered = newLine.OrderedQuantity, Before = before,
                    CreateStatus = response.StatusCode, CreateBody = body, Finalize = finalize, After = after },
                    new JsonSerializerOptions { WriteIndented = true }));
            Assert.True(response.StatusCode == HttpStatusCode.Conflict,
                "An issued amendment must not reset the quantity already received. " + body);
            foreach (var property in before.EnumerateObject().Where(x => x.Name != "Audits"))
                Assert.Equal(property.Value.GetRawText(), after.GetProperty(property.Name).GetRawText());
        });
    }
#endif
}
