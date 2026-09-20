using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Sales;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    // Everything after AVAILABLE stock, on the same fresh database, by seeded actors under real
    // page permissions and real operational scopes. Until this walk existed, every step below had
    // only ever been proven on databases the development-only trial script had built.
    private static async Task ProveOperationsAfterAvailable(HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, IReadOnlyDictionary<string, Guid> employees, Guid companyId,
        SESS.NexaERP.Domain.Inventory.Item item, SESS.NexaERP.Domain.Inventory.Item declared, FreshReceipt receipt, DateOnly today)
    {
        void Actor(string code, string role, params string[] effectiveRoles) => user.Set(employees[code], code, role, effectiveRoles);
        var purchased = receipt.Purchased;
        var departments = await Query(options, db => db.Departments.AsNoTracking().Where(x => x.Code == "SERVICE" || x.Code == "PRODUCTION")
            .ToDictionaryAsync(x => x.Code, x => x.Id));
        var evidence = new Dictionary<string, object?>();

        // 1. MIR -> issue by scan -> custody -> return. A Service Engineer draws consumables for the
        // department; the Stores Manager approves; the Stores Executive issues by scanning the item code.
        Actor("SESS-05", "SERVICE_ENGINEER", "SERVICE_ENGINEER", "TECHNICAL_SUPPORT_MANAGER");
        // An unknown purpose is refused as a request error, not a database check violation.
        using (var badPurpose = await client.PostAsJsonAsync("/api/v1/stores/material-issue-requests", new CreateMaterialIssueRequest("CONSUMPTION", "CONSUMABLE_OFFICE", "DEPARTMENT", null, null, null, departments["SERVICE"],
                "Service department", departments["SERVICE"], today, [new MaterialIssueRequestLineInput(item.Id, item.BaseUomId, 1m, null, null)], "go-live-mir-bad-purpose")))
            Assert.Equal(HttpStatusCode.BadRequest, badPurpose.StatusCode);
        var consumableMir = await Post<MaterialIssueRequestView>(client, "/api/v1/stores/material-issue-requests",
            new CreateMaterialIssueRequest("SERVICE", "CONSUMABLE_OFFICE", "DEPARTMENT", null, null, null, departments["SERVICE"],
                "Service department", departments["SERVICE"], today, [new MaterialIssueRequestLineInput(item.Id, item.BaseUomId, 3m, null, "Service consumables")],
                "go-live-mir-consumable"));
        consumableMir = await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{consumableMir.Id}/submit",
            new MaterialIssueTransitionRequest(consumableMir.Version, "Needed this week", "go-live-mir-consumable-submit"));
        Assert.Equal("SUBMITTED", consumableMir.Status);
        Actor("SESS-41", "STORES_MANAGER");
        consumableMir = await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{consumableMir.Id}/approve",
            new MaterialIssueTransitionRequest(consumableMir.Version, "Approved by Stores", "go-live-mir-consumable-approve"));
        Assert.Equal("APPROVED", consumableMir.Status);
        Actor("SESS-35", "STORES_EXECUTIVE");
        var recipients = await Get<MaterialIssueRecipientView[]>(client, "/api/v1/stores/material-issues/recipients");
        Assert.Contains(recipients, x => x.EmployeeCode == "SESS-05");
        var consumableIssue = await Post<MaterialIssueView>(client, $"/api/v1/stores/material-issues/from-request/{consumableMir.Id}",
            new CreateMaterialIssue("go-live-issue-consumable", employees["SESS-05"], DateTimeOffset.UtcNow,
                [new MaterialIssueScan(consumableMir.Lines.Single().Id, item.ItemCode, null, 3m)]));
        Assert.NotNull(consumableIssue.StockPostingBatchId);
        Assert.Equal(3m, Assert.Single(consumableIssue.Lines).QuantityBase);
        // The engineer sees their own custody and nobody else's.
        Actor("SESS-05", "SERVICE_ENGINEER", "SERVICE_ENGINEER", "TECHNICAL_SUPPORT_MANAGER");
        var ownCustody = await Get<OutstandingEngineerCustodyView[]>(client, "/api/v1/stores/material-issues/outstanding-custody");
        Assert.Contains(ownCustody, x => x.MaterialIssueId == consumableIssue.Id && x.EmployeeId == employees["SESS-05"] && x.QuantityBase == 3m);
        using (var otherCustody = await client.GetAsync($"/api/v1/stores/material-issues/outstanding-custody?employeeId={employees["SESS-35"]}"))
            Assert.Equal(HttpStatusCode.Forbidden, otherCustody.StatusCode);
        // Return one, report one consumed, keep one: the engineer declares, Stores accepts by scan.
        var consumableReturn = await Post<MaterialReturnView>(client, $"/api/v1/stores/material-returns/from-issue/{consumableIssue.Id}",
            new CreateMaterialReturn(DateTimeOffset.UtcNow, [new MaterialReturnLineInput(consumableIssue.Lines.Single().Id, item.ItemCode, 1m, 1m, 1m)], "go-live-return-consumable"));
        Assert.Equal("SUBMITTED", consumableReturn.Status);
        Actor("SESS-35", "STORES_EXECUTIVE");
        consumableReturn = await Post<MaterialReturnView>(client, $"/api/v1/stores/material-returns/{consumableReturn.Id}/accept",
            new AcceptMaterialReturn(consumableReturn.Version, DateTimeOffset.UtcNow, "Returned material counted back into Stores", "go-live-return-consumable-accept"));
        Assert.Equal("ACCEPTED", consumableReturn.Status);
        Assert.NotNull(consumableReturn.StockPostingBatchId);
        // Stores custody of the opening-stock item: 10 posted, 3 issued, 1 returned. The issued
        // and returned legs carry the opening stock line as their origin (finding #26).
        var storesCustody = await Query(options, db => db.StockMovements.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.ItemId == item.Id && x.ConditionCode == "AVAILABLE"
                && x.CustodyAssignment!.CustodyAccount!.CustodyType == "WAREHOUSE")
            .SumAsync(x => x.QuantityIn - x.QuantityOut));
        Assert.Equal(8m, storesCustody);
        Assert.Equal(4, await Query(options, db => db.StockMovements.AsNoTracking().CountAsync(x => x.CompanyId == companyId && x.ItemId == item.Id
            && x.MovementLeg != "RECEIPT_IN" && x.OriginOpeningStockLineId != null && x.OriginGoodsReceiptLineId == null)));
        evidence["storesCustodyAfterReturn"] = storesCustody;

        // 2. Customer -> customer PO -> job order -> Estimated BOM -> Production BOM -> job MIR -> issue
        // -> fitment -> Actual BOM -> FAT readiness.
        Actor("SESS-12", "IT_MANAGER");
        var customer = await Post<JsonElement>(client, "/api/v1/masters/customers", new UpsertCustomerRequest("GO-LIVE-CUS-001", "Go-live Customer Private Limited", null, "BUSINESS",
            "29AABCG1234B1Z9", "AABCG1234B", "1 Customer Road, Bengaluru", "1 Customer Road, Bengaluru", "Karnataka", "29", "India", "Customer contact", "9000000001",
            "customer@example.test", "Environmental testing", "30 days", 30, null, "GO_LIVE_CUSTOMER", null));
        await Post<JsonElement>(client, "/api/v1/masters/customers/GO-LIVE-CUS-001/submit", new MasterActionRequest("Customer submitted", customer.GetProperty("Version").GetUInt32()));
        Actor("SESS-01", "TECHNICAL_DIRECTOR");
        var submittedCustomer = await Get<JsonElement>(client, "/api/v1/masters/customers/GO-LIVE-CUS-001");
        await Post<JsonElement>(client, "/api/v1/masters/customers/GO-LIVE-CUS-001/approve", new MasterActionRequest("Customer approved", submittedCustomer.GetProperty("Version").GetUInt32()));
        Actor("SESS-12", "IT_MANAGER");
        var customerPo = await Post<JsonElement>(client, "/api/v1/sales/customer-pos", new CustomerPoEndpoints.UpsertCustomerPoRequest(null, "CUST-PO-GO-LIVE-001", today, null, null,
            "GO-LIVE-CUS-001", null, CustomerPoSalesTypes.Machine, "One environmental chamber", null, null, "30 days", "Road", null, null, "Delivered",
            9m, 9m, null, [new CustomerPoEndpoints.CustomerPoLineDto(1, item.Id, item.BaseUomId, "Environmental chamber " + item.Name, today.AddDays(60), 1m, item.Uom, 250000m, null, null)], null));
        var poRecordNumber = customerPo.GetProperty("PoRecordNumber").GetString()!;
        var customerPoDetail = await Get<CustomerPoEndpoints.CustomerPoDetail>(client, "/api/v1/sales/customer-pos/" + poRecordNumber);
        var machineLine = Assert.Single(customerPoDetail.Lines);
        Actor("SESS-25", "PRODUCTION_MANAGER");
        var poLines = await Get<JobOrderCustomerPoLineView[]>(client, "/api/v1/production/job-orders/customer-po-lines");
        Assert.Contains(poLines, x => x.Id == machineLine.Id);
        var job = await Post<JobOrderView>(client, "/api/v1/production/job-orders", new CreateJobOrderRequest(machineLine.Id!.Value, 1, "GO-LIVE-MACHINE-001", today, today.AddDays(60), "go-live-job-create"));
        Assert.Equal("PENDING_ACCOUNTS", job.Status);
        Actor("SESS-14", "ACCOUNTS_MANAGER");
        job = await Post<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}/accounts-confirm", new ConfirmJobOrderRequest(job.Version, "Customer PO and one-machine scope verified", "go-live-job-confirm"));
        Assert.Equal("OPEN", job.Status);
        // Estimated BOM: Design Engineer prepares and submits; Technical Director approves.
        Actor("SESS-17", "DESIGN_ENGINEER", "DESIGN_ENGINEER", "SERVICE_ENGINEER");
        var estimated = await Post<EstimatedBomView>(client, "/api/v1/design/estimated-boms", new CreateEstimatedBomRequest(job.Id, "Go-live commercial baseline",
            [new EstimatedBomLineInput(purchased.Id, purchased.BaseUomId, 3m, "Chamber component", 100m), new EstimatedBomLineInput(declared.Id, declared.BaseUomId, 1m, "Legacy component", 60m)], "go-live-ebom-create"));
        estimated = await Post<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/submit", new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Ready for review", "go-live-ebom-submit"));
        Actor("SESS-01", "TECHNICAL_DIRECTOR");
        estimated = await Post<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/approve", new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Baseline approved", "go-live-ebom-approve"));
        Assert.Equal("APPROVED", estimated.Status);
        // Preparers are roles, not employee codes: the Technical Support Manager (SESS-04, FULL) opens the
        // next revision under real page permissions; a Service Engineer without that role is refused.
        Actor("SESS-04", "TECHNICAL_SUPPORT_MANAGER");
        estimated = await Post<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/revisions",
            new NewEstimatedBomRevisionRequest(estimated.Version, "Technical Support revises the baseline", "go-live-ebom-revision"));
        Assert.Equal("DRAFT", estimated.Status);
        // The revision carries the approved values and offers, never imposes, a suggestion: no bill is
        // accepted yet, so the opening-stock carrying value is offered (90 and 60). The preparer accepts
        // it for the legacy component and types 100 for the chamber component; both sources are recorded.
        var offered = estimated.CurrentRevision.Lines.Single(x => x.CanonicalItemId == purchased.Id);
        Assert.Equal(90m, offered.SuggestedUnitValue);
        Assert.Equal("OPENING_STOCK", offered.SuggestedValueSource);
        Assert.Equal("ENGINEER", offered.ValueSource);
        estimated = await Put<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}", new ReplaceEstimatedBomLinesRequest(estimated.CurrentRevision.Version, "Revised quantities",
            [new EstimatedBomLineInput(purchased.Id, purchased.BaseUomId, 3m, "Chamber component", 100m), new EstimatedBomLineInput(declared.Id, declared.BaseUomId, 1m, "Legacy component", UseSuggestedValue: true)], "go-live-ebom-revision-lines"));
        Assert.Equal(60m, estimated.CurrentRevision.Lines.Single(x => x.CanonicalItemId == declared.Id).EstimatedUnitValue);
        Assert.Equal("OPENING_STOCK", estimated.CurrentRevision.Lines.Single(x => x.CanonicalItemId == declared.Id).ValueSource);
        using (var unpriced = await client.PutAsJsonAsync($"/api/v1/design/estimated-boms/{estimated.BomNumber}", new ReplaceEstimatedBomLinesRequest(estimated.CurrentRevision.Version, "Unpriced attempt",
            [new EstimatedBomLineInput(purchased.Id, purchased.BaseUomId, 3m, "Chamber component")], "go-live-ebom-unpriced")))
        {
            // A line with neither a typed value nor an accepted suggestion stays unpriced and cannot be submitted.
            Assert.True(unpriced.IsSuccessStatusCode, await unpriced.Content.ReadAsStringAsync());
            var unpricedView = JsonSerializer.Deserialize<EstimatedBomView>(await unpriced.Content.ReadAsStringAsync(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.Null(unpricedView.CurrentRevision.Lines.Single().EstimatedUnitValue);
            using var refusedSubmit = await client.PostAsJsonAsync($"/api/v1/design/estimated-boms/{estimated.BomNumber}/submit", new EstimatedBomActionRequest(unpricedView.CurrentRevision.Version, "Unpriced", "go-live-ebom-unpriced-submit"));
            Assert.Equal(HttpStatusCode.Conflict, refusedSubmit.StatusCode);
            estimated = await Put<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}", new ReplaceEstimatedBomLinesRequest(unpricedView.CurrentRevision.Version, "Revised quantities",
                [new EstimatedBomLineInput(purchased.Id, purchased.BaseUomId, 3m, "Chamber component", 100m), new EstimatedBomLineInput(declared.Id, declared.BaseUomId, 1m, "Legacy component", UseSuggestedValue: true)], "go-live-ebom-revision-lines-2"));
        }
        Actor("SESS-09", "SERVICE_ENGINEER");
        using (var refused = await client.PostAsJsonAsync($"/api/v1/design/estimated-boms/{estimated.BomNumber}/submit", new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Not a preparer", "go-live-ebom-refused")))
            Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Actor("SESS-04", "TECHNICAL_SUPPORT_MANAGER");
        estimated = await Post<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/submit", new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Revision ready", "go-live-ebom-revision-submit"));
        Actor("SESS-01", "TECHNICAL_DIRECTOR");
        estimated = await Post<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/approve", new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Revision approved", "go-live-ebom-revision-approve"));
        Assert.Equal("APPROVED", estimated.Status);
        // Production BOM: Production Manager prepares and submits; Technical Director approves; Production pins it to the job.
        Actor("SESS-25", "PRODUCTION_MANAGER");
        var production = await Post<ProductionBomView>(client, "/api/v1/production/boms", new CreateProductionBomRequest(job.Id, "Go-live production baseline", "go-live-pbom-create"));
        production = await Post<ProductionBomView>(client, $"/api/v1/production/boms/{production.BomNumber}/submit", new ProductionBomActionRequest(production.CurrentRevision.Version, "Production quantities confirmed", "go-live-pbom-submit"));
        Actor("SESS-01", "TECHNICAL_DIRECTOR");
        production = await Post<ProductionBomView>(client, $"/api/v1/production/boms/{production.BomNumber}/approve", new ProductionBomActionRequest(production.CurrentRevision.Version, "Production baseline approved", "go-live-pbom-approve"));
        Actor("SESS-25", "PRODUCTION_MANAGER");
        job = await Get<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}");
        production = await Post<ProductionBomView>(client, $"/api/v1/production/boms/{production.BomNumber}/pin", new PinProductionBomRevisionRequest(production.CurrentRevision.Id, job.Version, "Pinned to the go-live job", "go-live-pbom-pin"));
        Assert.Equal(production.CurrentRevision.Id, production.PinnedRevisionId);
        // Job MIR by the Production Coordinator for 3 purchased units (2 opening + 1 GRN) and 1 declared-provenance
        // unit; approved by the Production Manager; issued by Stores to the coordinator in two issues.
        Actor("SESS-13", "PRODUCTION_COORDINATOR");
        var jobMir = await Post<MaterialIssueRequestView>(client, "/api/v1/stores/material-issue-requests",
            new CreateMaterialIssueRequest("FACTORY_ASSEMBLY", "CHAMBER_MANUFACTURE", "JOB_ORDER", job.Id, null, null, null, "Go-live chamber assembly",
                departments["PRODUCTION"], today, [new MaterialIssueRequestLineInput(purchased.Id, purchased.BaseUomId, 3m, null, "Assembly components"),
                    new MaterialIssueRequestLineInput(declared.Id, declared.BaseUomId, 1m, null, "Legacy component")], "go-live-mir-job"));
        Assert.All(jobMir.Lines, x => Assert.Equal(0m, x.ExcessBaseQuantity));
        jobMir = await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{jobMir.Id}/submit", new MaterialIssueTransitionRequest(jobMir.Version, "Assembly starts", "go-live-mir-job-submit"));
        Actor("SESS-25", "PRODUCTION_MANAGER");
        jobMir = await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{jobMir.Id}/approve", new MaterialIssueTransitionRequest(jobMir.Version, "Production approves requirement", "go-live-mir-job-approve"));
        var purchasedLine = jobMir.Lines.Single(x => x.ItemId == purchased.Id);
        var declaredLine = jobMir.Lines.Single(x => x.ItemId == declared.Id);
        // FIFO: opening stock consumes first. The first issue takes exactly the opening quantity of the
        // purchased item; the GRN layer is untouched while opening stock of that item remains.
        Actor("SESS-35", "STORES_EXECUTIVE");
        var firstIssue = await Post<MaterialIssueView>(client, $"/api/v1/stores/material-issues/from-request/{jobMir.Id}",
            new CreateMaterialIssue("go-live-issue-job-1", employees["SESS-13"], DateTimeOffset.UtcNow,
                [new MaterialIssueScan(purchasedLine.Id, purchased.ItemCode, null, 2m), new MaterialIssueScan(declaredLine.Id, declared.ItemCode, null, 1m)]));
        Assert.Equal(job.Id, firstIssue.JobOrderId);
        var openingPurchasedLine = Assert.Single(firstIssue.Lines, x => x.ItemId == purchased.Id);
        var openingDeclaredLine = Assert.Single(firstIssue.Lines, x => x.ItemId == declared.Id);
        var layers = await Query(options, async db => await db.FifoInventoryCostLayers.AsNoTracking().Where(x => x.CompanyId == companyId && x.ItemId == purchased.Id)
            .Select(x => new { x.Id, Opening = x.OpeningStockLineId != null, Grn = x.GoodsReceiptLineId != null, x.QuantityReceived,
                Consumed = db.FifoCostConsumptions.Where(c => c.FifoInventoryCostLayerId == x.Id).Sum(c => (decimal?)c.Quantity) ?? 0m }).ToListAsync());
        Assert.Equal(2m, Assert.Single(layers, x => x.Opening).Consumed);
        Assert.Equal(0m, Assert.Single(layers, x => x.Grn).Consumed);
        Assert.True(await Query(options, db => db.MaterialIssueLines.AsNoTracking().AnyAsync(x => x.Id == openingPurchasedLine.Id && x.OriginOpeningStockLineId != null && x.OriginGoodsReceiptLineId == null)));
        var secondIssue = await Post<MaterialIssueView>(client, $"/api/v1/stores/material-issues/from-request/{jobMir.Id}",
            new CreateMaterialIssue("go-live-issue-job-2", employees["SESS-13"], DateTimeOffset.UtcNow, [new MaterialIssueScan(purchasedLine.Id, purchased.ItemCode, null, 1m)]));
        var grnPurchasedLine = Assert.Single(secondIssue.Lines);
        Assert.True(await Query(options, db => db.MaterialIssueLines.AsNoTracking().AnyAsync(x => x.Id == grnPurchasedLine.Id && x.OriginGoodsReceiptLineId != null && x.OriginOpeningStockLineId == null)));
        layers = await Query(options, async db => await db.FifoInventoryCostLayers.AsNoTracking().Where(x => x.CompanyId == companyId && x.ItemId == purchased.Id)
            .Select(x => new { x.Id, Opening = x.OpeningStockLineId != null, Grn = x.GoodsReceiptLineId != null, x.QuantityReceived,
                Consumed = db.FifoCostConsumptions.Where(c => c.FifoInventoryCostLayerId == x.Id).Sum(c => (decimal?)c.Quantity) ?? 0m }).ToListAsync());
        Assert.Equal(2m, Assert.Single(layers, x => x.Opening).Consumed);
        Assert.Equal(1m, Assert.Single(layers, x => x.Grn).Consumed);
        // Fitment: one purchased unit of opening origin, one of GRN origin, one declared-provenance unit; the
        // second opening-origin purchased unit goes back to Stores. Valuation resolves by origin.
        Actor("SESS-13", "PRODUCTION_COORDINATOR");
        var openingFitment = await Post<ComponentFitmentSummary>(client, "/api/v1/production/component-fitments",
            new ConfirmComponentFitmentRequest(job.Id, openingPurchasedLine.Id, 1m, DateTimeOffset.UtcNow, "Opening-stock component fitted", null, "go-live-fitment-opening"));
        var grnFitment = await Post<ComponentFitmentSummary>(client, "/api/v1/production/component-fitments",
            new ConfirmComponentFitmentRequest(job.Id, grnPurchasedLine.Id, 1m, DateTimeOffset.UtcNow, "Purchased component fitted", null, "go-live-fitment-grn"));
        var declaredFitment = await Post<ComponentFitmentSummary>(client, "/api/v1/production/component-fitments",
            new ConfirmComponentFitmentRequest(job.Id, openingDeclaredLine.Id, 1m, DateTimeOffset.UtcNow, "Legacy component fitted", null, "go-live-fitment-declared"));
        Assert.False(openingFitment.IsReversed || grnFitment.IsReversed || declaredFitment.IsReversed);
        var fitment = openingFitment;
        var jobReturn = await Post<MaterialReturnView>(client, $"/api/v1/stores/material-returns/from-issue/{firstIssue.Id}",
            new CreateMaterialReturn(DateTimeOffset.UtcNow, [new MaterialReturnLineInput(openingPurchasedLine.Id, purchased.ItemCode, 1m, 0m, 0m)], "go-live-return-job"));
        Actor("SESS-35", "STORES_EXECUTIVE");
        jobReturn = await Post<MaterialReturnView>(client, $"/api/v1/stores/material-returns/{jobReturn.Id}/accept",
            new AcceptMaterialReturn(jobReturn.Version, DateTimeOffset.UtcNow, "Unfitted component returned", "go-live-return-job-accept"));
        Assert.Equal("ACCEPTED", jobReturn.Status);
        Actor("SESS-25", "PRODUCTION_MANAGER");
        var actual = await Get<ActualBomView>(client, $"/api/v1/production/component-fitments/job-orders/{job.Id}/actual-bom");
        Assert.Equal(3, actual.Entries.Count);
        var openingEntry = Assert.Single(actual.Entries, x => x.ComponentFitmentId == openingFitment.Id);
        Assert.Equal("OPENING_CONFIRMED", openingEntry.ValuationStatus);
        Assert.Equal(90m, openingEntry.AcceptedMaterialValue);
        Assert.Equal(0m, openingEntry.AllocatedChargeValue);
        Assert.NotNull(openingEntry.OpeningStockLineId);
        Assert.Null(openingEntry.GoodsReceiptLineId);
        var declaredEntry = Assert.Single(actual.Entries, x => x.ComponentFitmentId == declaredFitment.Id);
        Assert.Equal("OPENING_CONFIRMED", declaredEntry.ValuationStatus);
        Assert.Equal(60m, declaredEntry.TotalAcceptedValue);
        var provisional = Assert.Single(actual.Entries, x => x.ComponentFitmentId == grnFitment.Id);
        Assert.Equal("PROVISIONAL_UNBILLED", provisional.ValuationStatus);
        Assert.NotNull(provisional.GoodsReceiptLineId);
        Assert.Equal(150m, actual.TotalAcceptedValue);
        // The commercial baseline is the first approved revision, where both values were typed; the later
        // revision that accepted the opening-stock value is engineering, not the offer baseline.
        Assert.All(actual.CommercialVariance.Lines, x => Assert.Equal("ENGINEER", x.BaselineValueSource));        // FAT readiness: QC reconciles custody; everything issued is fitted or returned.
        Actor("SESS-33", "QC_MANAGER");
        var fat = await Post<FatReconciliationView>(client, $"/api/v1/production/job-orders/{job.Id}/fat-readiness/reconcile", new ReconcileJobOrderFatRequest("Issue custody fitted or returned", "go-live-fat-reconcile"));
        Assert.Equal("READY", fat.Result);
        Assert.Equal(0m, fat.UnexplainedQuantityBase);
        var readiness = await Get<JobOrderFatReadinessView>(client, $"/api/v1/production/job-orders/{job.Id}/fat-readiness");
        Assert.Equal("READY", readiness.FatReadinessStatus);

        // 3. Machine delivery challan -> customer signature -> ancestry dossier.
        Actor("SESS-35", "STORES_EXECUTIVE");
        var candidates = await Get<PagedResponse<MachineDeliveryJobOrderCandidate>>(client, "/api/v1/stores/machine-deliveries/job-orders?search=" + Uri.EscapeDataString(job.MachineSerial));
        Assert.Equal(job.Id, Assert.Single(candidates.Items).JobOrderId);
        var dispatch = await Post<JsonElement>(client, "/api/v1/stores/machine-deliveries/", new DispatchMachineRequest(job.Id, "GO-LIVE-MDC-001", "NON_RETURNABLE", "CUSTOMER_PO_BASED",
            DateOnly.FromDateTime(DateTime.Now), null, "Customer site, Bengaluru", "go-live-machine-dispatch"));
        var deliveryId = dispatch.GetProperty("Id").GetGuid();
        Assert.Equal("DISPATCHED", dispatch.GetProperty("MachineState").GetString());
        var delivered = await Post<JsonElement>(client, $"/api/v1/stores/machine-deliveries/{deliveryId}/signature", new SignMachineDeliveryRequest(DateTimeOffset.UtcNow, "Customer representative",
            new SupplierInvoiceEvidenceInput("signed-go-live-dc.pdf", "application/pdf", Encoding.ASCII.GetBytes("%PDF-1.7\nGo-live customer signed machine DC\n%%EOF")), "go-live-machine-sign"));
        Assert.Equal("DELIVERED", delivered.GetProperty("MachineState").GetString());
        var dossierSelection = Uri.EscapeDataString(JsonSerializer.Serialize(new { machineSerial = job.MachineSerial }));
        Actor("SESS-14", "ACCOUNTS_MANAGER");
        var dossier = await Get<CompanyReportPage>(client, $"/api/v1/reports/machine-dossier?selection={dossierSelection}&mode=details&pageSize=1000");
        Assert.NotEmpty(dossier.Rows);
        string Provenance(CompanyReportPage page, Guid fitmentId) => Assert.Single(page.Rows, row => row.TryGetProperty("fitmentId", out var f) && f.ValueKind == JsonValueKind.String && f.GetGuid() == fitmentId && row.GetProperty("evidencePart").GetInt32() == 0).GetProperty("provenance").GetString()!;
        // The dossier tells declared from proved: what SESS asserted at opening stock, what this system verified, and what it authorised.
        Assert.Equal("Bill INV-2024-0892 - declared at opening stock, not verified in this system", Provenance(dossier, declaredFitment.Id));
        Assert.Equal($"Opening stock, authorised {DateTime.Now.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture)} by SESS-01", Provenance(dossier, openingFitment.Id));
        Assert.Equal($"GRN {receipt.Grn.GrnNumber} - bill not yet accepted", Provenance(dossier, grnFitment.Id));

        // 4. Vendor bill from the GRN -> landed cost -> advance -> payment. GRNI shows the receipt until the bill is accepted.
        var grni = await Get<CompanyReportPage>(client, WitnessReportPath("/api/v1/reports/grni?mode=details&pageSize=1000"));
        Assert.Contains(grni.Rows, row => row.GetProperty("itemCode").GetString() == purchased.ItemCode);
        var poLine = await Query(options, db => db.PurchaseOrderLines.AsNoTracking().Where(x => x.PurchaseOrderId == receipt.PurchaseOrderId)
            .Select(x => new { x.Id, x.TotalPayableValue }).SingleAsync());
        var grnLine = Assert.Single(receipt.Grn.Lines);
        Actor("SESS-28", "ACCOUNTS_ASSISTANT");
        var bill = await Post<VendorBillView>(client, $"/api/v1/accounts/vendor-bills/from-grn/{receipt.Grn.Id}", new CreateVendorBillRequest(receipt.Grn.VendorBillNumber, receipt.Grn.VendorBillDate,
            [new VendorBillLineInput(grnLine.Id, grnLine.ReceivedQuantity, 100m, poLine.TotalPayableValue, 2m)], "go-live-bill-create", [new VendorBillChargeInput("FREIGHT", 12m)]));
        Assert.Equal("MATCHED", bill.MatchStatus);
        Assert.Equal(12m, bill.TotalChargeValue);
        Actor("SESS-14", "ACCOUNTS_MANAGER");
        bill = await Post<VendorBillView>(client, $"/api/v1/accounts/vendor-bills/{bill.Id}/accept", new VendorBillDecisionRequest(bill.Version, "Three-way match accepted", "go-live-bill-accept"));
        Assert.Equal("ACCEPTED", bill.Status);
        // The supplier invoice recorded before the goods is now linked to the accepted bill.
        var currentInvoice = await Get<SupplierInvoiceView>(client, $"/api/v1/accounts/supplier-invoices/{receipt.SupplierInvoice.Id}");
        Assert.NotEmpty(currentInvoice.ReceiptMatches); // the receipt matched the invoice when the GRN was finalized
        var linkedInvoice = await Post<SupplierInvoiceView>(client, $"/api/v1/accounts/supplier-invoices/{receipt.SupplierInvoice.Id}/link-accepted-bill",
            new LinkSupplierInvoiceAcceptedBillRequest(currentInvoice.Version, bill.Id, "go-live-supplier-invoice-link"));
        Assert.Contains(bill.Id, linkedInvoice.AcceptedBillIds);
        var landed = await Query(options, db => db.FifoLandedCostAdjustments.AsNoTracking().Where(x => x.VendorBillLine!.VendorBillId == bill.Id).SumAsync(x => x.AllocatedChargeValue));
        Assert.Equal(12m, landed);
        Actor("SESS-25", "PRODUCTION_MANAGER");
        actual = await Get<ActualBomView>(client, $"/api/v1/production/component-fitments/job-orders/{job.Id}/actual-bom");
        Assert.Equal("LANDED_ACCEPTED", Assert.Single(actual.Entries, x => x.ComponentFitmentId == grnFitment.Id).ValuationStatus);
        Assert.Equal(2, actual.Entries.Count(x => x.ValuationStatus == "OPENING_CONFIRMED"));
        Assert.True(actual.TotalAcceptedValue > 150m);
        Actor("SESS-14", "ACCOUNTS_MANAGER");
        var advance = await Post<VendorAdvanceView>(client, "/api/v1/accounts/vendor-financial-evidence/advances",
            new RecordVendorAdvanceRequest(receipt.PurchaseOrderId, today, 100m, "INR", "UTR-GO-LIVE-ADV-1", "evidence/go-live-advance-1.pdf", "go-live-advance"));
        Assert.Equal(100m, advance.Amount);
        var payment = await Post<VendorPaymentView>(client, "/api/v1/accounts/vendor-financial-evidence/payments",
            new RecordVendorPaymentRequest(receipt.VendorId, today, 50m, "INR", "UTR-GO-LIVE-PAY-1", "evidence/go-live-payment-1.pdf", [new VendorPaymentAllocationInput(bill.Id, 50m)], "go-live-payment"));
        Assert.Equal(50m, Assert.Single(payment.Allocations).Amount);
        var payments = await Get<VendorPaymentPage>(client, $"/api/v1/accounts/vendor-financial-evidence/payments?vendorId={receipt.VendorId}");
        Assert.Contains(payments.Items, x => x.Id == payment.Id);
        // The dossier now states the GRN-origin component was accepted, matched and part-paid; the opening lines are unchanged.
        var dossierAfterBill = await Get<CompanyReportPage>(client, $"/api/v1/reports/machine-dossier?selection={dossierSelection}&mode=details&pageSize=1000");
        Assert.Equal($"Bill {bill.BillNumber} - accepted, matched, part-paid", Provenance(dossierAfterBill, grnFitment.Id));
        Assert.Equal("Bill INV-2024-0892 - declared at opening stock, not verified in this system", Provenance(dossierAfterBill, declaredFitment.Id));

        // 5. Intercompany. Only the sale path (route -> published PO -> GST invoice evidence) exists; a
        // DC-only transfer has no endpoint (A1, not started). The sale path itself needs company sites
        // and approved customer/vendor company relationships, and neither has an API or a seed: on a
        // fresh database the route options carry no site and no customer, so no route can be proposed.
        var routeOptions = await Get<JsonElement>(client, "/api/v1/stores/intercompany/routes/options");
        evidence["intercompanySites"] = routeOptions.GetProperty("sites").GetArrayLength();
        evidence["intercompanyCustomers"] = routeOptions.GetProperty("customers").GetArrayLength();
        Assert.Equal(2, routeOptions.GetProperty("companies").GetArrayLength());
        Assert.Equal(2, routeOptions.GetProperty("gstRegistrations").GetArrayLength());
        Assert.Equal(0, routeOptions.GetProperty("sites").GetArrayLength());

        // 6. The ten company reports, each by a seeded viewer.
        Actor("SESS-05", "SERVICE_ENGINEER", "SERVICE_ENGINEER", "TECHNICAL_SUPPORT_MANAGER");
        var pendingMir = await Post<MaterialIssueRequestView>(client, "/api/v1/stores/material-issue-requests",
            new CreateMaterialIssueRequest("SERVICE", "CONSUMABLE_OFFICE", "DEPARTMENT", null, null, null, departments["SERVICE"],
                "Service department", departments["SERVICE"], today.AddDays(7), [new MaterialIssueRequestLineInput(item.Id, item.BaseUomId, 1m, null, "Pending for the report")], "go-live-mir-pending"));
        await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{pendingMir.Id}/submit", new MaterialIssueTransitionRequest(pendingMir.Version, "Left pending", "go-live-mir-pending-submit"));
        var reportRows = new Dictionary<string, long>();
        foreach (var (key, code, role, query, expectRows) in new[]
        {
            ("stock-balance", "SESS-01", "TECHNICAL_DIRECTOR", "?pageSize=1000", true),
            ("movement-roll-forward", "SESS-01", "TECHNICAL_DIRECTOR", "?fromDate=2026-01-01&pageSize=1000", true),
            ("engineer-custody", "SESS-01", "TECHNICAL_DIRECTOR", "?pageSize=1000", true),
            ("fifo-valuation", "SESS-14", "ACCOUNTS_MANAGER", "?pageSize=1000", true),
            ("grni", "SESS-14", "ACCOUNTS_MANAGER", "?pageSize=1000", false),
            ("billed-not-received", "SESS-14", "ACCOUNTS_MANAGER", "?pageSize=1000", false), // empty at the end: the invoice met its receipt
            ("vendor-purchases", "SESS-14", "ACCOUNTS_MANAGER", "?fromDate=2026-01-01&pageSize=1000", true),
            ("machine-dossier", "SESS-14", "ACCOUNTS_MANAGER", $"?selection={dossierSelection}&mode=details&pageSize=1000", true),
            ("purchase-register", "SESS-15", "PURCHASE_MANAGER", "?pageSize=1000", true),
            ("pending-approvals", "SESS-41", "STORES_MANAGER", "?mode=details&pageSize=1000", true)
        })
        {
            Actor(code, role, code == "SESS-15" ? new[] { "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE" } : new[] { role });
            var page = await Get<CompanyReportPage>(client, WitnessReportPath($"/api/v1/reports/{key}{query}"));
            reportRows[key] = page.TotalRows;
            Assert.True(!expectRows || page.Rows.Count > 0, $"Report {key} returned no rows.");
            // The role that can view a report can export it (20260920180000): the same actor exports.
            using var excel = await client.GetAsync(WitnessReportPath($"/api/v1/reports/{key}/excel{query}"));
            Assert.True(excel.IsSuccessStatusCode, $"Report {key} Excel export returned {(int)excel.StatusCode}: {await excel.Content.ReadAsStringAsync()}");
        }
        evidence["reportRows"] = reportRows;
        var root = Path.Combine(FindRepositoryRoot(), "local-evidence", "fresh-company-operations");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "operations-witness.json"), JsonSerializer.Serialize(new
        {
            at = DateTimeOffset.Now, consumableIssue = consumableIssue.IssueNumber, consumableReturn = consumableReturn.ReturnNumber,
            job = job.JobOrderNumber, fitment = fitment.FitmentNumber, fat = fat.Result, delivery = deliveryId, bill = bill.BillNumber,
            advance = advance.AdvanceNumber, payment = payment.PaymentNumber, evidence
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
