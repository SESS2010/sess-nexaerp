using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Database;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    // Resolve the actual installer contract, avoiding a second test-local copy of the shared type.
    private static readonly string ManualAssessmentVerifySql = (string)typeof(DatabasePrincipalCommand).Assembly
        .GetType("SESS.NexaERP.Database.VendorManualAssessment20260919AccessSql", throwOnError: true)!
        .GetField("Verify", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
        .GetRawConstantValue()!;
    private sealed class ManualAssessmentWitnessComplete : Exception { }
    [Fact]
    public async Task VendorManualAssessmentUsesRealQcAuthorityAndImmutableRevisions()
    {
        await Assert.ThrowsAsync<ManualAssessmentWitnessComplete>(() => RunCompletePurchaseFlow(grnRace: async context =>
        {
            await using var source = new NexaErpDbContext(context.Options);
            var actor = await BankAdviceActor(context.Options);
            var company = Guid.Parse("70000000-0000-0000-0000-000000000001");
            var subjects = await source.EmployeeIdentityMappings.Where(x => x.CompanyId == company && x.IsActive)
                .ToDictionaryAsync(x => x.EmployeeId, x => x.Subject);
            actor.Set(context.FirstOperatorId, subjects[context.FirstOperatorId], "STORES_EXECUTIVE");
            await using var stores = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
            var grn = await Post<GoodsReceiptResult>(stores.Client, $"/api/v1/stores/goods-receipts/{context.Draft.Id}/finalize",
                new FinalizeGoodsReceiptRequest(context.Draft.Version, context.FirstKey));
            var stockBefore = await source.StockMovements.CountAsync();
            // RunCompletePurchaseFlow installed the real latest EF migration chain.
            var migration = new VendorManualAssessments { ActiveProvider = "Npgsql.EntityFrameworkCore.PostgreSQL" };
            await source.Database.ExecuteSqlRawAsync(ManualAssessmentVerifySql);
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
            var qc = await source.Employees.SingleAsync(x => x.EmployeeCode == "SESS-33");
            actor.Set(qc.Id, subjects[qc.Id], "QC_MANAGER");
            await AssertResolvedSeedRole(context.Options, actor, "QC_MANAGER");
            await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
            var client = host.Client;
            const string path = "/api/v1/quality/vendor-manual-assessments";
            // The Stores detail remains forbidden; QC obtains only the required receipt selectors.
            using (var storesDetail = await client.GetAsync($"/api/v1/stores/goods-receipts/{grn.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden, storesDetail.StatusCode);
            var receiptPage = await Get<VendorRatingReceiptPage>(client, "/api/v1/quality/vendor-rating-evidence/receipts?page=1&pageSize=50");
            var qcReceipt = Assert.Single(receiptPage.Items, x => x.GoodsReceiptId == grn.Id);
            Assert.Equal(grn.Version, qcReceipt.GoodsReceiptVersion);
            var emptyPage = await Get<VendorRatingReceiptPage>(client, "/api/v1/quality/vendor-rating-evidence/receipts?page=100000&pageSize=50");
            Assert.Empty(emptyPage.Items); Assert.Equal(receiptPage.TotalCount, emptyPage.TotalCount);
            actor.SetOrganization("SESS_PROPRIETORSHIP");
            using (var foreignPage = await client.GetAsync("/api/v1/quality/vendor-rating-evidence/receipts"))
            {
                if (foreignPage.StatusCode == HttpStatusCode.OK)
                    Assert.DoesNotContain((await foreignPage.Content.ReadFromJsonAsync<VendorRatingReceiptPage>())!.Items, x => x.GoodsReceiptId == grn.Id);
                else Assert.Equal(HttpStatusCode.Forbidden, foreignPage.StatusCode);
            }
            actor.SetOrganization("SESS_PVT_LTD");
            var request = new RecordVendorManualAssessmentRequest(qcReceipt.GoodsReceiptId, qcReceipt.GoodsReceiptVersion, null, 15, 5, 5,
                "Observed technical compliance and response", "manual-assessment-first");
            var first = await Post<VendorManualAssessmentView>(client, path, request);
            Assert.False(first.Replayed); Assert.Equal(1, first.RevisionNumber);
            Assert.Equal(qc.Id, first.ActorEmployeeId); Assert.Equal(15, first.TechnicalPoints);
            Assert.Equal(grn.Id, first.GoodsReceiptId); Assert.Equal(grn.Version, first.GoodsReceiptVersion);
            var receipt = await source.GoodsReceipts.AsNoTracking().SingleAsync(x => x.Id == grn.Id);
            Assert.Equal(receipt.VendorId, first.VendorId); Assert.Equal(receipt.PurchaseOrderId, first.PurchaseOrderId);
            var replay = await Post<VendorManualAssessmentView>(client, path, request);
            Assert.True(replay.Replayed); Assert.Equal(first.Id, replay.Id);
            using (var changed = await client.PostAsJsonAsync(path, request with { TechnicalPoints = 14 }))
                Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
            foreach (var invalid in new[] {
                request with { TechnicalPoints = 15.01m }, request with { ResponsePoints = -0.01m },
                request with { OverallPoints = 5.01m } })
            {
                using var response = await client.PostAsJsonAsync(path, invalid with { IdempotencyKey = Guid.NewGuid().ToString() });
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
            using (var staleReceipt = await client.PostAsJsonAsync(path, request with {
                GoodsReceiptVersion = grn.Version + 1, SupersedesAssessmentId = first.Id, IdempotencyKey = "manual-stale-grn" }))
                Assert.Equal(HttpStatusCode.Conflict, staleReceipt.StatusCode);
            using (var duplicateFirst = await client.PostAsJsonAsync(path, request with { IdempotencyKey = "manual-duplicate-first" }))
                Assert.Equal(HttpStatusCode.Conflict, duplicateFirst.StatusCode);
            var priorRead = Assert.Single((await client.GetFromJsonAsync<VendorManualAssessmentView[]>($"{path}/for-receipt/{grn.Id}"))!);
            Assert.Equal(first.Id, priorRead.Id);
            var correction = request with { SupersedesAssessmentId = priorRead.Id, TechnicalPoints = 0,
                ResponsePoints = 0, OverallPoints = 0, Reason = "Corrected assessment supported by review", IdempotencyKey = "manual-correct" };
            var second = await Post<VendorManualAssessmentView>(client, path, correction);
            Assert.Equal(2, second.RevisionNumber); Assert.Equal(first.Id, second.SupersedesAssessmentId);
            Assert.Equal(0, second.TechnicalPoints);
            var raceRequest = correction with { SupersedesAssessmentId = second.Id, TechnicalPoints = 8 };
            var race = await Task.WhenAll(
                client.PostAsJsonAsync(path, raceRequest with { IdempotencyKey = "manual-race-one" }),
                client.PostAsJsonAsync(path, raceRequest with { IdempotencyKey = "manual-race-two" }));
            try
            {
                Assert.Single(race, x => x.StatusCode == HttpStatusCode.OK);
                Assert.Single(race, x => x.StatusCode == HttpStatusCode.Conflict);
            }
            finally { foreach (var response in race) response.Dispose(); }
            var history = await client.GetFromJsonAsync<VendorManualAssessmentView[]>($"{path}/for-receipt/{grn.Id}");
            Assert.Equal(3, history!.Length); Assert.Equal(15, history[0].TechnicalPoints);
            Assert.Equal(0, history[1].TechnicalPoints); Assert.Equal(8, history[2].TechnicalPoints);
            Assert.Equal(new[] { 1, 2, 3 }, history.Select(x => x.RevisionNumber));
            actor.SetOrganization("SESS_PROPRIETORSHIP");
            using (var foreign = await client.GetAsync($"{path}/for-receipt/{grn.Id}"))
            {
                if (foreign.IsSuccessStatusCode) Assert.Empty((await foreign.Content.ReadFromJsonAsync<VendorManualAssessmentView[]>())!);
                else Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
            }
            actor.SetOrganization("SESS_PVT_LTD");
            actor.Set(context.FirstOperatorId, subjects[context.FirstOperatorId], "STORES_EXECUTIVE");
            using (var wrongRole = await client.PostAsJsonAsync(path, request with { IdempotencyKey = "manual-stores-forbidden" }))
                Assert.Equal(HttpStatusCode.Forbidden, wrongRole.StatusCode);
            await source.Database.ExecuteSqlRawAsync("GRANT INSERT, UPDATE ON advance.vendor_manual_assessments TO nexa_erp_runtime;");
            await using (var runtime = new NpgsqlConnection(context.RuntimeConnection))
            {
                await runtime.OpenAsync();
                await using var forged = new NpgsqlCommand("BEGIN; SELECT set_config('sess.vendor_manual_assessment_write',txid_current()::text,true); UPDATE advance.vendor_manual_assessments SET \"Reason\"='Forged'; COMMIT;", runtime);
                var refusal = await Assert.ThrowsAsync<PostgresException>(() => forged.ExecuteNonQueryAsync());
                Assert.Contains("immutable", refusal.MessageText, StringComparison.OrdinalIgnoreCase);
            }
            await using (var runtime = new NpgsqlConnection(context.RuntimeConnection))
            {
                await runtime.OpenAsync();
                await using var forged = new NpgsqlCommand("""
                    BEGIN;
                    SELECT set_config('sess.vendor_manual_assessment_write',txid_current()::text,true);
                    INSERT INTO advance.vendor_manual_assessments("Id","CompanyId","GoodsReceiptId","GoodsReceiptVersion",
                      "VendorId","PurchaseOrderId","RevisionNumber","SupersedesAssessmentId","TechnicalPoints","ResponsePoints",
                      "OverallPoints","Reason","ActorEmployeeId","RoleAssignmentId","RoleAssignmentType","RecordedBy")
                    VALUES(@id,@company,@grn,@version,@vendor,@po,4,@prior,15,5,5,'Forged direct insert',@actor,@assignment,'FULL','forged');
                    COMMIT;
                    """, runtime);
                forged.Parameters.AddWithValue("id", Guid.NewGuid()); forged.Parameters.AddWithValue("company", company);
                forged.Parameters.AddWithValue("grn", grn.Id); forged.Parameters.AddWithValue("version", (long)grn.Version);
                forged.Parameters.AddWithValue("vendor", receipt.VendorId); forged.Parameters.AddWithValue("po", receipt.PurchaseOrderId);
                forged.Parameters.AddWithValue("prior", history[2].Id); forged.Parameters.AddWithValue("actor", qc.Id);
                forged.Parameters.AddWithValue("assignment", first.RoleAssignmentId);
                var refusal = await Assert.ThrowsAsync<PostgresException>(() => forged.ExecuteNonQueryAsync());
                Assert.Equal("42501", refusal.SqlState); Assert.Contains("governed QC Manager command", refusal.MessageText);
            }
            Assert.NotEqual(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
            await source.Database.ExecuteSqlRawAsync("ALTER TABLE advance.vendor_manual_assessments DISABLE TRIGGER trg_vendor_manual_assessment;");
            var missingGuard = await Assert.ThrowsAsync<PostgresException>(() => source.Database.ExecuteSqlRawAsync(ManualAssessmentVerifySql));
            Assert.Contains("append guard", missingGuard.MessageText);
            await source.Database.ExecuteSqlRawAsync("ALTER TABLE advance.vendor_manual_assessments ENABLE TRIGGER trg_vendor_manual_assessment;");
            await source.Database.ExecuteSqlRawAsync(ManualAssessmentVerifySql);
            var down = Assert.Single(migration.DownOperations.Cast<SqlOperation>());
            var refusalDown = await Assert.ThrowsAsync<PostgresException>(() => source.Database.ExecuteSqlRawAsync(down.Sql));
            Assert.Contains("retained business evidence", refusalDown.MessageText);
            Assert.Equal(stockBefore, await source.StockMovements.CountAsync());
            throw new ManualAssessmentWitnessComplete();
        }));
    }
}
