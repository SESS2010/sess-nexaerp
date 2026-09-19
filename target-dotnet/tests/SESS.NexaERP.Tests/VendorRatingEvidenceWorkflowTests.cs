using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed class VendorEvidenceWitnessComplete : Exception { }
    [Fact]
    public async Task VendorRatingEvidenceUsesCurrentQcAndExcludesReversedConcessions()
    {
        var pendingObserved = false;
        await Assert.ThrowsAsync<VendorEvidenceWitnessComplete>(() => RunCompletePurchaseFlow(
            grnRace: async context =>
            {
                await using var db = new NexaErpDbContext(context.Options);
                var actor = await BankAdviceActor(context.Options);
                var subjects = await db.EmployeeIdentityMappings.Where(x => x.CompanyId == Guid.Parse("70000000-0000-0000-0000-000000000001") && x.IsActive)
                    .ToDictionaryAsync(x => x.EmployeeId, x => x.Subject);
                actor.Set(context.FirstOperatorId, subjects[context.FirstOperatorId], "STORES_EXECUTIVE");
                await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
                var grn = await Post<GoodsReceiptResult>(host.Client, $"/api/v1/stores/goods-receipts/{context.Draft.Id}/finalize",
                    new FinalizeGoodsReceiptRequest(context.Draft.Version, context.FirstKey));
                var qc = await db.Employees.SingleAsync(x => x.EmployeeCode == "SESS-33");
                actor.Set(qc.Id, subjects[qc.Id], "QC_MANAGER");
                var evidence = await Get<VendorRatingReceiptEvidence>(host.Client, $"/api/v1/quality/vendor-rating-evidence/{grn.Id}");
                Assert.All(evidence.Lines, line => {
                    Assert.Null(line.QualityPoints);
                    Assert.All(line.Lots, lot => { Assert.Equal("QC_NOT_RECORDED", lot.Status); Assert.Null(lot.QualityPoints); });
                });
                Assert.Equal(20m, evidence.ShipmentDeliveryPoints);
                pendingObserved = true;
                return grn;
            },
            qcRace: async context =>
            {
                Assert.True(pendingObserved);
                await using var db = new NexaErpDbContext(context.Options);
                var company = Guid.Parse("70000000-0000-0000-0000-000000000001");
                var subjects = await db.EmployeeIdentityMappings.Where(x => x.CompanyId == company && x.IsActive)
                    .ToDictionaryAsync(x => x.EmployeeId, x => x.Subject);
                var actor = await BankAdviceActor(context.Options);
                actor.Set(context.InspectorId, subjects[context.InspectorId], "QC_MANAGER");
                await AssertResolvedSeedRole(context.Options, actor, "QC_MANAGER");
                await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
                var lineId = (await db.QcInspections.AsNoTracking().SingleAsync(x => x.Id == context.Inspection.InspectionId)).GoodsReceiptLineId!.Value;
                var line = await db.GoodsReceiptLines.AsNoTracking().Include(x => x.PurchaseOrderLine).ThenInclude(x => x!.CommercialComparisonLine).SingleAsync(x => x.Id == lineId);
                var path = $"/api/v1/quality/vendor-rating-evidence/{line.GoodsReceiptId}";
                async Task<VendorRatingReceiptEvidence> Read() => await Get<VendorRatingReceiptEvidence>(host.Client, path);
                async Task<T> Send<T>(string route, object request, string key)
                {
                    using var message = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(request) };
                    message.Headers.Add("Idempotency-Key", key);
                    using var response = await host.Client.SendAsync(message);
                    Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                    return (await response.Content.ReadFromJsonAsync<T>())!;
                }
                var initial = await Read();
                var firstLine = Assert.Single(initial.Lines);
                Assert.Equal(0m, firstLine.QualityPoints); Assert.Equal(20m, initial.ShipmentDeliveryPoints);
                Assert.Equal(line.PurchaseOrderLine!.CommercialComparisonLineId, firstLine.CommercialComparisonLineId);
                Assert.Equal(line.PurchaseOrderLine.CommercialComparisonLine!.DeliverySnapshot, firstLine.CommittedDateSnapshot);
                Assert.Equal(context.Inspection.RevisionId, Assert.Single(firstLine.Lots).QcRevisionId);
                Assert.Equal(initial.SourceFingerprint, (await Read()).SourceFingerprint);
                actor.Set(context.DirectorId, subjects[context.DirectorId], "TECHNICAL_DIRECTOR");
                var approved = await Send<InventoryConcessionResult>($"/api/v1/qc/concessions/{context.Draft.ConcessionNumber}/approve",
                    new ApproveInventoryConcessionRequest(context.Draft.Version, context.AvailableLocationId, "Rating evidence witness"), "rating-concession-approve");
                actor.Set(context.InspectorId, subjects[context.InspectorId], "QC_MANAGER");
                var accepted = await Read();
                Assert.Equal(25m, Assert.Single(accepted.Lines).QualityPoints);
                var applied = Assert.Single(Assert.Single(Assert.Single(accepted.Lines).Lots).Concessions);
                Assert.Equal(approved.Id, applied.Id); Assert.Equal(1m, applied.AppliedQuantity); Assert.Null(applied.ReversalId);
                Assert.NotEqual(initial.SourceFingerprint, accepted.SourceFingerprint);
                actor.Set(context.DirectorId, subjects[context.DirectorId], "TECHNICAL_DIRECTOR");
                var reversal = await Send<InventoryConcessionResult>($"/api/v1/qc/concessions/{approved.ConcessionNumber}/reverse",
                    new ReverseInventoryConcessionRequest(approved.Version, "Reversed retained concession"), "rating-concession-reverse");
                actor.Set(context.InspectorId, subjects[context.InspectorId], "QC_MANAGER");
                var reversed = await Read();
                Assert.Equal(0m, Assert.Single(reversed.Lines).QualityPoints);
                var excluded = Assert.Single(Assert.Single(Assert.Single(reversed.Lines).Lots).Concessions);
                Assert.Equal(reversal.Id, excluded.ReversalId); Assert.Equal(0m, excluded.AppliedQuantity);
                Assert.NotEqual(initial.SourceFingerprint, reversed.SourceFingerprint);
                Assert.Equal("APPROVED", (await db.InventoryConcessions.AsNoTracking().SingleAsync(x => x.Id == approved.Id)).Status);
                var command = context.InspectionCommand;
                var corrected = await Send<QcInspectionResult>($"/api/v1/qc/inspections/{context.Inspection.InspectionNumber}/corrections",
                    new CorrectQcInspectionRequest(context.Inspection.RevisionId, "Reinspection now accepts the unit",
                        command.InspectionStartedAt, 1m, 0m, 0m, context.AvailableLocationId,
                        command.ParameterResults.Select(x => x with { ObservedNumericValue = 5m, Result = "PASS" }).ToArray(),
                        command.SerialDispositions.Select(x => x with { Disposition = "ACCEPTED", Reason = null }).ToArray()), "rating-qc-correction");
                var latest = await Read();
                Assert.Equal(25m, Assert.Single(latest.Lines).QualityPoints);
                Assert.Equal(corrected.RevisionId, Assert.Single(Assert.Single(latest.Lines).Lots).QcRevisionId);
                Assert.Equal(0m, Assert.Single(Assert.Single(latest.Lines).Lots).ConcessionAcceptedQuantity);
                Assert.NotEqual(reversed.SourceFingerprint, latest.SourceFingerprint);
                actor.SetOrganization("SESS_PROPRIETORSHIP");
                using (var foreign = await host.Client.GetAsync(path)) Assert.Contains(foreign.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
                actor.SetOrganization("SESS_PVT_LTD"); actor.Set(context.DirectorId, subjects[context.DirectorId], "TECHNICAL_DIRECTOR");
                using (var denied = await host.Client.GetAsync(path)) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
                throw new VendorEvidenceWitnessComplete();
            }));
    }
}
