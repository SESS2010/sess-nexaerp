using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Api.Serialization;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.SecurityMigrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task CompletePurchaseFlowRunsAgainstDisposablePostgreSqlInAllThreeApprovalBands()
    {
        var bootstrapOptions = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var model = new NexaErpDbContext(bootstrapOptions);
        var migrator = model.GetService<IMigrator>();
        var latest = model.Database.GetMigrations().Last();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("purchase-flow-business-up.sql", migrator.GenerateScript("0", latest));
        server.Execute("purchase-flow-trial.sql", "\\set expected_database advance_parser\n" +
            File.ReadAllText(Path.Combine(FindRepositoryRoot(), "database", "postgresql", "trial-master-data-apply.sql")));

        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options;
        Guid creatorId;
        Guid managerId;
        Guid tdId;
        Guid mdId;
        Guid verifierId;
        Guid purchaseId;
        Guid storesId;
        Guid qcId;
        Guid warehouseId;
        Guid rackBinId;
        Guid categoryId;
        Guid vendor1Id;
        Guid vendor2Id;
        await using (var seed = new NexaErpDbContext(options))
        {
            var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
            var departmentId = await seed.Departments.Where(x => x.Code == "IT").Select(x => x.Id).SingleAsync();
            warehouseId = await seed.Warehouses.Where(x => x.WarehouseCode == "TRIAL-WH-C01").Select(x => x.Id).SingleAsync();
            rackBinId = await seed.RackBins.Where(x => x.BinCode == "TRIAL-C01-GEN-01").Select(x => x.Id).SingleAsync();
            var item = await seed.Items.SingleAsync(x => x.ItemCode == "TRIAL-ITEM-001");
            categoryId = item.CategoryId ?? throw new InvalidOperationException("Trial item category is required.");
            vendor1Id = await seed.Vendors.Where(x => x.VendorCode == "TRIAL-VEN-001").Select(x => x.Id).SingleAsync();
            vendor2Id = await seed.Vendors.Where(x => x.VendorCode == "TRIAL-VEN-002").Select(x => x.Id).SingleAsync();
            creatorId = await Employee(seed, "SESS-12");
            managerId = await Employee(seed, "SESS-14");
            tdId = await Employee(seed, "SESS-01");
            mdId = await Employee(seed, "SESS-02");
            verifierId = await Employee(seed, "SESS-05");
            purchaseId = await Employee(seed, "SESS-15");
            storesId = await Employee(seed, "SESS-35");
            qcId = await Employee(seed, "SESS-33");
            var identities = new[]
            {
                (creatorId, "SESS-12"), (managerId, "SESS-14"), (tdId, "SESS-01"),
                (mdId, "SESS-02"), (verifierId, "SESS-05"), (purchaseId, "SESS-15"), (storesId, "SESS-35"), (qcId, "SESS-33")
            };
            var identityEmployeeIds = identities.Select(x => x.Item1).ToArray();
            await seed.Employees.Where(x => identityEmployeeIds.Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(e => e.LoginEnabled, true));
            seed.EmployeeIdentityMappings.AddRange(identities.Select(x => Mapping(companyId, x.Item1, x.Item2)));
            seed.EmployeeOperationalScopes.AddRange(identities.Select(x => new EmployeeOperationalScope
            {
                CompanyId = companyId, OrganizationId = "SESS_PVT_LTD", EmployeeId = x.Item1,
                DepartmentId = departmentId, WarehouseId = warehouseId, OwnRecordsOnly = false,
                AllowsPrivilegedCrossScope = false, EffectiveFrom = new DateOnly(2026, 1, 1),
                IsActive = true, Remarks = "Disposable full Purchase flow", CreatedBy = "PURCHASE_FLOW_TEST"
            }));
            await seed.SaveChangesAsync();
        }

        server.Execute("purchase-flow-security-roles.sql", ExternalRolePrerequisites);
        var securityOptions = new DbContextOptionsBuilder<Rev869BSecurityDbContext>()
            .UseNpgsql(server.ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(Rev869BSecurityDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Rev869BSecurity", "advance");
            }).Options;
        using (var security = new Rev869BSecurityDbContext(securityOptions))
        {
            var securityMigrator = security.GetService<IMigrator>();
            var securityMigration = Assert.Single(security.Database.GetMigrations());
            server.Execute("purchase-flow-security-up.sql", securityMigrator.GenerateScript("0", securityMigration));
        }
        var auditConnection = new Npgsql.NpgsqlConnectionStringBuilder(server.ConnectionString)
        {
            Username = "nexa_rev869b_command_audit",
            Pooling = false
        }.ConnectionString;
        using var environment = new TaxWorkflowEnvironment(auditConnection);
        var user = new TaxWorkflowUser(purchaseId, "SESS-15", Rev869ARoleCodes.StoresExecutive);
        var runtimeConnection = new Npgsql.NpgsqlConnectionStringBuilder(server.ConnectionString)
        {
            Username = "nexa_rev869b_app_runtime",
            Pooling = false
        }.ConnectionString;
        await using var adminHost = await PurchaseFlowHost.StartAsync(server.ConnectionString, user);
        await using var runtimeHost = await PurchaseFlowHost.StartAsync(runtimeConnection, user);
        var adminClient = adminHost.Client;
        var client = runtimeHost.Client;

        try
        {
            var categoryCode = await Query(options, db => db.ItemCategories.Where(x => x.Id == categoryId).Select(x => x.Code).SingleAsync());
            foreach (var vendorCode in new[] { "TRIAL-VEN-001", "TRIAL-VEN-002" })
            {
                user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseExecutive,
                    Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresExecutive);
                await PostNoResult(client, "/api/v1/rev869a/configuration/vendor-qualifications",
                    new CreateVendorQualificationRequest("SESS_PVT_LTD", vendorCode, categoryCode,
                        "TRIAL-PURCHASE-FLOW", new DateOnly(2026, 1, 1), null, "Disposable qualification"),
                    $"fixture-qualification-create-{vendorCode}");
                var qualification = await Query(options, db => db.VendorQualifications
                    .Where(x => x.Vendor!.VendorCode == vendorCode && x.QualificationCode == "TRIAL-PURCHASE-FLOW")
                    .Select(x => new { x.Id, x.Version }).SingleAsync());
                user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
                await PostNoResult(client, $"/api/v1/rev869a/configuration/vendor-qualifications/{qualification.Id}/verify",
                    new ChangeVendorQualificationLifecycleRequest(qualification.Version, "Technical qualification checked"),
                    $"fixture-qualification-verify-{vendorCode}");
                var verifiedVersion = await Query(options, db => db.VendorQualifications.Where(x => x.Id == qualification.Id).Select(x => x.Version).SingleAsync());
                user.Set(mdId, "SESS-02", Rev869ARoleCodes.ManagingDirector);
                await PostNoResult(client, $"/api/v1/rev869a/configuration/vendor-qualifications/{qualification.Id}/approve",
                    new ChangeVendorQualificationLifecycleRequest(verifiedVersion, "Final qualification approved"),
                    $"fixture-qualification-approve-{vendorCode}");
                var provenanceSql = string.Concat(
                    "SELECT advance.rev869b_qualification_provenance_valid('", qualification.Id,
                    "') AS ", (char)34, "Value", (char)34);
                var provenance = await Query(options, db => db.Database.SqlQueryRaw<bool>(provenanceSql).SingleAsync());
                var provenanceFacts = await Query(options, async db =>
                {
                    var current = await db.VendorQualifications.Where(x => x.Id == qualification.Id)
                        .Select(x => new { x.Version, x.VerifiedByEmployeeId, x.ApprovedByEmployeeId }).SingleAsync();
                    var histories = await db.ControlledConfigurationHistories
                        .Where(x => x.EntityType == nameof(VendorQualification) && x.EntityId == qualification.Id)
                        .OrderBy(x => x.Version).Select(x => new { x.Action, x.Version, x.ActorLoginId }).ToListAsync();
                    return new { current, histories };
                });
                Assert.True(provenance, JsonSerializer.Serialize(provenanceFacts));
            }

            user.Set(managerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
            await PostNoResult(client, "/api/v1/rev869a/configuration/tax-gst",
                Request("9025"), "fixture-tax-create-9025");
            var tax = await Query(options, db => db.TaxGstSettings.Where(x => x.HsnSacCode == "9025").Select(x => new { x.Id, x.Version }).SingleAsync());
            user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
            await PostNoResult(client, $"/api/v1/rev869a/configuration/tax-gst/{tax.Id}/approve",
                new DecideTaxGstSettingRequest(tax.Version, "Government GST portal manually cross-checked", "fixture-tax-approve-9025"),
                "fixture-tax-approve-9025");

            var bands = new[]
            {
                new PurchaseFlowBand("LOW", 4999.99m, 4000m, 1, managerId, null),
                new PurchaseFlowBand("TD", 5000.00m, 5000m, 2, managerId, tdId),
                new PurchaseFlowBand("MD", 100000.01m, 100000.01m, 2, managerId, mdId)
            };
            var grns=new List<GoodsReceiptResult>();
            foreach (var band in bands)
                grns.Add(await RunPurchaseBand(adminClient, client, options, user, band, creatorId, managerId, tdId, mdId,
                    verifierId, purchaseId, storesId, qcId, vendor1Id, vendor2Id));
            for(var i=0;i<grns.Count;i++)await RunQcWitness(adminClient,options,user,bands[i],grns[i],qcId,tdId);

            await using var verify = new NexaErpDbContext(options);
            Assert.Equal(3, await verify.PurchaseRequisitions.CountAsync());
            Assert.Equal(3, await verify.RequestForQuotations.CountAsync());
            Assert.Equal(6, await verify.RfqVendorInvitations.CountAsync());
            Assert.Equal(6, await verify.VendorQuotations.CountAsync());
            Assert.Equal(6, await verify.QuotationTechnicalVerifications.CountAsync());
            Assert.Equal(3, await verify.CommercialComparisons.CountAsync());
            Assert.Equal(3, await verify.PurchaseOrders.CountAsync());
            Assert.Equal(3, await verify.MaterialFollowUpHandoffs.CountAsync());
            Assert.Equal(3, await verify.GateEntries.CountAsync());
            Assert.Equal(3, await verify.GoodsReceipts.CountAsync());
            Assert.Equal(3, await verify.GoodsReceiptLines.CountAsync());
            Assert.Equal(3, await verify.GoodsReceiptLineLotAllocations.CountAsync());
            Assert.Equal(3, await verify.InventoryLots.CountAsync());
            Assert.Equal(1, await verify.GoodsReceiptLineSerials.CountAsync());
            Assert.Equal(1, await verify.InventorySerials.CountAsync());
            Assert.Equal(3, await verify.StockPostingBatches.CountAsync(x=>x.PostingKind=="GRN_CUSTODY"));
            Assert.Equal(0m, await verify.StockMovements.Where(x=>x.ConditionCode=="QC_HOLD").SumAsync(x=>x.QuantityIn-x.QuantityOut));
            Assert.Equal(2.95m, await verify.StockMovements.Where(x=>x.ConditionCode=="AVAILABLE").SumAsync(x=>x.QuantityIn-x.QuantityOut));
            Assert.Equal(.05m, await verify.StockMovements.Where(x=>x.ConditionCode=="PENDING_RETURNABLE_DC").SumAsync(x=>x.QuantityIn-x.QuantityOut));
            Assert.Equal(3,await verify.QcInspections.CountAsync());Assert.Equal(3,await verify.QcInspectionRevisions.CountAsync());Assert.Equal(3,await verify.QcInspectionLotDispositions.CountAsync());Assert.Equal(3,await verify.StockPostingBatches.CountAsync(x=>x.PostingKind=="QC_DISPOSITION"));Assert.Single(await verify.StockPostingBatches.Where(x=>x.PostingKind=="CONCESSION_ACCEPTANCE").ToListAsync());Assert.Single(await verify.InventoryConcessions.Where(x=>x.Status=="APPROVED").ToListAsync());
            Assert.Equal(6, await verify.StoresDocumentStatusHistories.CountAsync(x=>x.GateEntryId!=null));
            Assert.Equal(6, await verify.StoresDocumentStatusHistories.CountAsync(x=>x.GoodsReceiptId!=null));
            Assert.Equal(new[]{"ELE","FAB","FAS","MEC","PLC","REF"},await verify.ItemCategories.Where(x=>x.CreatedBy=="TRIAL_DATA").OrderBy(x=>x.Code).Select(x=>x.Code).ToArrayAsync());
            var qcLocations=await verify.WarehouseConditionLocations.Where(x=>x.CompanyId==Guid.Parse("70000000-0000-0000-0000-000000000001")&&x.ConditionCode=="QC_HOLD"&&x.CreatedBy=="TRIAL_DATA").ToListAsync();
            Assert.Equal(6,qcLocations.Count);
            Assert.Equal(6,qcLocations.Select(x=>x.RackBinId).Distinct().Count());
            var qcRackNames=await verify.RackBins.Where(x=>qcLocations.Select(location=>location.RackBinId).Contains(x.Id)).Select(x=>x.RackName).Distinct().ToListAsync();
            Assert.Equal("TRIAL QC Category Rack",Assert.Single(qcRackNames));
            Assert.All(await verify.PurchaseOrders.AsNoTracking().ToListAsync(), x => Assert.Equal(Rev869BStatuses.Issued, x.Status));

        }
        finally { }
    }

    private static async Task<GoodsReceiptResult> RunPurchaseBand(HttpClient prClient, HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, PurchaseFlowBand band, Guid creatorId, Guid managerId, Guid tdId, Guid mdId,
        Guid verifierId, Guid purchaseId, Guid storesId, Guid qcId, Guid vendor1Id, Guid vendor2Id)
    {
        var required = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        user.Set(creatorId, "SESS-12", "SOFTWARE_DEVELOPER");
        var employeePage=await Get<PagedResponse<SESS.NexaERP.Application.Employees.EmployeeSummary>>(prClient,"/api/v1/employees?page=1&pageSize=1");
        Assert.True(employeePage.TotalCount>1);var employee=Assert.Single(employeePage.Items);
        using(var stale=await prClient.PutAsJsonAsync($"/api/v1/employees/{employee.EmployeeCode}",new SESS.NexaERP.Application.Employees.UpdateEmployeeRequest(
            employee.EmployeeName,employee.EmployeeType,employee.Grade,"NOT_USED","NOT_USED","NOT_USED",null,null,null,"Concurrency witness",employee.Version+1)))
            Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);
        var pr = await Post<PurchaseRequisitionDetail>(prClient, "/api/v1/purchase/requisitions",
            new CreatePurchaseRequisitionRequest("SESS_PVT_LTD", "IT", "SESS-12", required, "NORMAL",
                $"TRIAL {band.Code} full Purchase flow", "TRIAL-WH-C01", null, null, null, null, null,
                [new("TRIAL-ITEM-001", 1, band.PrAmount, required, "TRIAL-WH-C01", null, null, null)]));
        Assert.Equal(PurchaseRequisitionStatuses.Draft, pr.Status);
        await AssertPrEvidence(options, pr.Id, "CreateDraft", 1, 1);
        pr = await Post<PurchaseRequisitionDetail>(prClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/submit",
            new PurchaseRequisitionActionRequest("Submitted", pr.Version, $"{band.Code}-pr-submit"));
        Assert.Equal(PurchaseRequisitionStatuses.Submitted, pr.Status);
        await AssertPrEvidence(options, pr.Id, "Submit", 2, 2);
        pr = await Post<PurchaseRequisitionDetail>(prClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/verify",
            new PurchaseRequisitionActionRequest("Department verified", pr.Version, $"{band.Code}-pr-verify"));
        Assert.Equal(PurchaseRequisitionStatuses.PendingApproval, pr.Status);
        await AssertPrEvidence(options, pr.Id, "DepartmentVerify", 3, 3);

        user.Set(managerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        pr = await Post<PurchaseRequisitionDetail>(prClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/approve",
            new PurchaseRequisitionActionRequest("Level 1 approved", pr.Version, $"{band.Code}-pr-approve-1"));
        Assert.Equal(band.RequiredSteps == 1 ? PurchaseRequisitionStatuses.StockCheckPending : PurchaseRequisitionStatuses.PendingApproval, pr.Status);
        if (band.Level2EmployeeId.HasValue)
        {
            var role = band.Level2EmployeeId == tdId ? Rev869ARoleCodes.TechnicalDirector : Rev869ARoleCodes.ManagingDirector;
            user.Set(band.Level2EmployeeId.Value, role == Rev869ARoleCodes.TechnicalDirector ? "SESS-01" : "SESS-02", role);
            pr = await Post<PurchaseRequisitionDetail>(prClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/approve",
                new PurchaseRequisitionActionRequest("Level 2 approved", pr.Version, $"{band.Code}-pr-approve-2"));
        }
        Assert.Equal(PurchaseRequisitionStatuses.StockCheckPending, pr.Status);
        await AssertApprovalActors(options, "PR", pr.Id, band.RequiredSteps, managerId, band.Level2EmployeeId);

        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.StoresExecutive,
            Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresExecutive);
        await PostNoResult(prClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/stock-check",
            new StockCheckRequest("No stock; purchase required", pr.Version, $"{band.Code}-stock",
                [new(1, "TRIAL-WH-C01", "TRIAL-C01-GEN-01")]), $"{band.Code}-stock");
        var handoff = await Query(options, db => db.PurchaseRequirementHandoffs
            .Where(x => x.PurchaseRequisitionId == pr.Id).Select(x => new { x.Id, x.HandoffQuantity }).SingleAsync());
        await AssertPrEvidence(options, pr.Id, "StockCheck", 4 + band.RequiredSteps, 4 + band.RequiredSteps);

        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseExecutive,
            Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresExecutive);
        var rfq = await Post<Rev869BDocumentResult>(client, "/api/v1/purchase/rfqs",
            new Rev869BCreateRfqRequest(DateTimeOffset.UtcNow.AddDays(7), "INR", false, null,
                $"{band.Code}-rfq-create", [new(handoff.Id, handoff.HandoffQuantity)]));
        await AssertTransactionEvidence(options, "RFQ", rfq.Id, "CreateRFQ");
        var invitations = new List<Guid>();
        foreach (var vendorId in new[] { vendor1Id, vendor2Id })
        {
            var rfqVersion = await Query(options, db => db.RequestForQuotations.Where(x => x.Id == rfq.Id).Select(x => x.Version).SingleAsync());
            var invitation = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/rfqs/{rfq.Number}/vendors",
                new Rev869BInviteVendorRequest(vendorId, "Qualified vendor invited", rfqVersion,
                    $"{band.Code}-invite-{vendorId:N}"));
            invitations.Add(invitation.Id);
            await AssertTransactionEvidence(options, "RFQInvitation", invitation.Id, "InviteVendor");
        }
        var rfqList=await Get<PagedResponse<RfqListItem>>(client,$"/api/v1/purchase/rfqs?rfqNumber={rfq.Number}&vendorId={vendor1Id}&sortBy=date&sortDirection=desc");
        Assert.Equal(1,rfqList.TotalCount);Assert.Equal(rfq.Id,Assert.Single(rfqList.Items).Id);
        var rfqLineId = await Query(options, db => db.RequestForQuotationLines.Where(x => x.RequestForQuotationId == rfq.Id).Select(x => x.Id).SingleAsync());
        var quotations = new List<Rev869BDocumentResult>();
        for (var index = 0; index < invitations.Count; index++)
        {
            var rate = band.QuoteRate + index * 1m;
            var quote = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/rfq-invitations/{invitations[index]}/quotations",
                new Rev869BSubmitQuotationRequest($"TRIAL-{band.Code}-V{index + 1}", "INR", "30 days",
                    "Delivered to trial warehouse", "12 months", false, null, "EMAIL_RECEIVED",
                    DateTimeOffset.UtcNow.AddMinutes(-1), $"trial/{band.Code}/vendor-{index + 1}.pdf",
                    new string((char)('A' + index), 64), "Entered from synthetic vendor quotation", 0, null,
                    $"{band.Code}-quote-{index + 1}",
                    [new(rfqLineId, handoff.HandoffQuantity, rate, 0, 0, 0, 0, 0, required,
                        "9025", "33", "33", VendorRegistrationType.REGULAR.ToCanonicalValue(), 0)]));
            quotations.Add(quote);
            await AssertTransactionEvidence(options, "VendorQuotation", quote.Id, "SubmitQuotation");
        }
        var quotationList=await Get<PagedResponse<QuotationListItem>>(client,$"/api/v1/purchase/quotations?quotationNumber={quotations[0].Number}&vendorId={vendor1Id}");
        Assert.Equal(1,quotationList.TotalCount);Assert.Equal(quotations[0].Id,Assert.Single(quotationList.Items).Id);
        var quotationDetail=await Get<JsonElement>(client,$"/api/v1/purchase/quotations/{quotations[0].Number}");
        Assert.Equal(quotations[0].Number,quotationDetail.GetProperty("QuotationNumber").GetString());
        user.Set(verifierId, "SESS-05", "TECHNICAL_ENGINEER");
        foreach (var quote in quotations)
        {
            var lineId = await Query(options, db => db.VendorQuotationLines.Where(x => x.VendorQuotationId == quote.Id).Select(x => x.Id).SingleAsync());
            await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/quotations/{quote.Number}/technical-verifications",
                new Rev869BTechnicalVerificationRequest(lineId, true, """{"trial":true}""", "Technically compliant",
                    quote.Version, $"{band.Code}-technical-{quote.Id:N}"));
            await AssertTechnicalEvidence(options, quote.Id, verifierId);
        }

        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresExecutive);
        var rfqCurrentVersion = await Query(options, db => db.RequestForQuotations.Where(x => x.Id == rfq.Id).Select(x => x.Version).SingleAsync());
        var comparison = await Post<Rev869BDocumentResult>(client, "/api/v1/purchase/comparisons",
            new Rev869BCreateComparisonRequest(rfq.Number, rfqCurrentVersion, $"{band.Code}-comparison-create"));
        await AssertTransactionEvidence(options, "CommercialComparison", comparison.Id, "CreateComparison");
        comparison = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/comparisons/{comparison.Number}/recommend",
            new Rev869BRecommendComparisonRequest(quotations[0].Id, "Lowest compliant offer", null,
                comparison.Version, $"{band.Code}-comparison-recommend"));
        await AssertTransactionEvidence(options, "CommercialComparison", comparison.Id, "RecommendVendor");

        user.Set(managerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        comparison = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/comparisons/{comparison.Number}/approve",
            new Rev869BApprovalActionRequest("Level 1 comparison approval", comparison.Version, $"{band.Code}-comparison-approve-1"));
        if (band.Level2EmployeeId.HasValue)
        {
            var role = band.Level2EmployeeId == tdId ? Rev869ARoleCodes.TechnicalDirector : Rev869ARoleCodes.ManagingDirector;
            user.Set(band.Level2EmployeeId.Value, role == Rev869ARoleCodes.TechnicalDirector ? "SESS-01" : "SESS-02", role);
            comparison = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/comparisons/{comparison.Number}/approve",
                new Rev869BApprovalActionRequest("Level 2 comparison approval", comparison.Version, $"{band.Code}-comparison-approve-2"));
        }
        Assert.Equal(Rev869BStatuses.Approved, comparison.Status);
        var comparisonList=await Get<PagedResponse<ComparisonListItem>>(client,$"/api/v1/purchase/comparisons?comparisonNumber={comparison.Number}&vendorId={vendor1Id}");
        Assert.Equal(1,comparisonList.TotalCount);Assert.Equal(comparison.Id,Assert.Single(comparisonList.Items).Id);
        await AssertApprovalActors(options, "CMP", comparison.Id, band.RequiredSteps, managerId, band.Level2EmployeeId);

        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresExecutive);
        var po = await Post<Rev869BDocumentResult>(client, "/api/v1/purchase/purchase-orders",
            new Rev869BCreatePurchaseOrderRequest(comparison.Number, comparison.Version, $"{band.Code}-po-create"));
        await AssertPoEvidence(options, po.Id, "CreatePO");
        po = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/purchase-orders/{po.Number}/submit",
            new Rev869BSubmitPurchaseOrderRequest("PO submitted", po.Version, $"{band.Code}-po-submit"));
        await AssertPoEvidence(options, po.Id, "SubmitPO");
        user.Set(managerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        po = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/purchase-orders/{po.Number}/approve",
            new Rev869BPoApprovalActionRequest("Level 1 PO approval", po.Version, null, $"{band.Code}-po-approve-1"));
        if (band.Level2EmployeeId.HasValue)
        {
            var role = band.Level2EmployeeId == tdId ? Rev869ARoleCodes.TechnicalDirector : Rev869ARoleCodes.ManagingDirector;
            user.Set(band.Level2EmployeeId.Value, role == Rev869ARoleCodes.TechnicalDirector ? "SESS-01" : "SESS-02", role);
            po = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/purchase-orders/{po.Number}/approve",
                new Rev869BPoApprovalActionRequest("Level 2 PO approval", po.Version, null, $"{band.Code}-po-approve-2"));
        }
        Assert.Equal(Rev869BStatuses.Approved, po.Status);
        await AssertApprovalActors(options, "PO", po.Id, band.RequiredSteps, managerId, band.Level2EmployeeId);
        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresExecutive);
        po = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/purchase-orders/{po.Number}/issue",
            new Rev869BIssuePurchaseOrderRequest("PO issued", po.Version, $"{band.Code}-po-issue"));
        Assert.Equal(Rev869BStatuses.Issued, po.Status);
        await AssertPoEvidence(options, po.Id, "IssuePO");
        var poList=await Get<PagedResponse<PurchaseOrderListItem>>(client,$"/api/v1/purchase/purchase-orders?purchaseOrderNumber={po.Number}&vendorId={vendor1Id}");
        Assert.Equal(1,poList.TotalCount);Assert.Equal(po.Id,Assert.Single(poList.Items).Id);
        var followups=await Get<PagedResponse<MaterialFollowUpListItem>>(client,"/api/v1/purchase/material-followup?pageSize=100");
        Assert.Contains(followups.Items,x=>x.PurchaseOrderId==po.Id);

        user.Set(storesId,"SESS-35",Rev869ARoleCodes.StoresExecutive,Rev869ARoleCodes.StoresExecutive);
        var poLineId=await Query(options,db=>db.PurchaseOrderLines.Where(x=>x.PurchaseOrderId==po.Id).Select(x=>x.Id).SingleAsync());
        var gate=await Post<GateEntryResult>(prClient,"/api/v1/stores/gate-entries/",
            new CreateGateEntryRequest(po.Number,$"TRIAL-DC-{band.Code}","TRIAL-VEHICLE","ROAD",DateTimeOffset.UtcNow,"{\"packagesChecked\":true}",[new(poLineId,1)]),$"{band.Code}-gate-create");
        Assert.Equal("DRAFT",gate.Status); Assert.Single(gate.History);
        gate=await Put<GateEntryResult>(prClient,$"/api/v1/stores/gate-entries/{gate.Id}",new UpdateGateEntryRequest(gate.VendorDcNumber,"TRIAL-VEHICLE-EDITED","ROAD",gate.ArrivedAt,"{\"packagesChecked\":true,\"edited\":true}",[new(poLineId,1)],gate.Version));
        var detail=await Get<GateEntryResult>(prClient,$"/api/v1/stores/gate-entries/{gate.Id}"); Assert.Equal("TRIAL-VEHICLE-EDITED",detail.VehicleNumber);
        var list=await Get<GateEntryListResult>(prClient,$"/api/v1/stores/gate-entries/?gateEntryNumber={gate.GateEntryNumber}"); Assert.Contains(list.Items,x=>x.Id==gate.Id);Assert.Equal(1,list.TotalCount);
        gate=await Post<GateEntryResult>(prClient,$"/api/v1/stores/gate-entries/{gate.Id}/finalize",new FinalizeGateEntryRequest(gate.Version,$"{band.Code}-gate-finalize"));
        Assert.Equal("FINALIZED",gate.Status); Assert.Equal(2,gate.History.Count);
        await using var gateEvidence=new NexaErpDbContext(options); Assert.Equal(3,await gateEvidence.AuditLogs.CountAsync(x=>x.EntityId==gate.Id.ToString()&&x.Module=="Stores"));

        IReadOnlyList<GoodsReceiptSerialRequest> serials=band.QuoteRate>5000m
            ? [new(1,1,$"TRIAL-SERIAL-{band.Code}",$"TRIAL-SERIAL-{band.Code}",false,null)] : [];
        var billDate=DateOnly.FromDateTime(DateTime.UtcNow);var receivedAt=DateTimeOffset.UtcNow;
        var grn=await Post<GoodsReceiptResult>(prClient,"/api/v1/stores/goods-receipts/",
            new CreateGoodsReceiptRequest(gate.GateEntryNumber,$"TRIAL-BILL-{band.Code}",billDate,receivedAt,"{\"billChecked\":true}",
                [new(gate.Lines.Single().Id,[new(1,1,$"TRIAL-BATCH-{band.Code}",null,billDate.AddMonths(-1),billDate.AddYears(2))],serials)]),
            $"{band.Code}-grn-create");
        Assert.Equal("DRAFT",grn.Status);Assert.Single(grn.History);Assert.Single(grn.Lines);Assert.Single(grn.Lines[0].Lots);
        Assert.Equal(billDate.AddMonths(13),grn.Lines[0].WarrantyExpiryDate);Assert.Equal("9025",grn.Lines[0].HsnSacCode);
        Assert.Equal(band.QuoteRate>5000m?"REQUIRED":"OPTIONAL",grn.Lines[0].SerialCaptureMode);Assert.Empty(grn.Warnings);
        var draftVersion=grn.Version;
        grn=await Post<GoodsReceiptResult>(prClient,$"/api/v1/stores/goods-receipts/{grn.Id}/finalize",new FinalizeGoodsReceiptRequest(draftVersion,$"{band.Code}-grn-finalize"));
        Assert.Equal("FINALIZED",grn.Status);Assert.Equal(2,grn.History.Count);Assert.NotNull(grn.StockPostingBatchId);Assert.False(grn.Replayed);Assert.Empty(grn.Warnings);
        if(serials.Count==1)Assert.NotNull(grn.Lines[0].Serials.Single().InventorySerialId);
        var grnList=await Get<GoodsReceiptListResult>(prClient,$"/api/v1/stores/goods-receipts/?goodsReceiptNumber={grn.GrnNumber}");
        Assert.Equal(1,grnList.TotalCount);Assert.Equal(grn.Id,Assert.Single(grnList.Items).Id);
        await using(var evidence=new NexaErpDbContext(options))
        {
            Assert.True(await evidence.StockPostingBatches.AnyAsync(x=>x.Id==grn.StockPostingBatchId&&x.GoodsReceiptId==grn.Id&&x.PostingKind=="GRN_CUSTODY"));
            var movements=await evidence.StockMovements.Where(x=>x.StockPostingBatchId==grn.StockPostingBatchId).ToListAsync();
            Assert.NotEmpty(movements);Assert.All(movements,x=>
            {
                Assert.Equal(2,x.LedgerSchemaVersion);
                Assert.NotEqual(Guid.Empty,x.OwnershipAccountId);
                Assert.NotEqual(Guid.Empty,x.CustodyAssignmentId);
                Assert.NotEqual(Guid.Empty,x.InventoryProvenanceLayerId);
                Assert.Equal("QC_HOLD",x.ConditionCode);
                Assert.NotNull(x.GoodsReceiptLineLotAllocationId);
                Assert.NotNull(x.InventoryLotId);
            });
            Assert.Equal(0,await evidence.StockMovements.Where(x=>x.StockPostingBatchId==grn.StockPostingBatchId&&x.ConditionCode=="AVAILABLE").SumAsync(x=>x.QuantityIn-x.QuantityOut));
            Assert.Equal(2,await evidence.AuditLogs.CountAsync(x=>x.EntityId==grn.Id.ToString()&&x.Module=="Stores"));
        }
        var replay=await Post<GoodsReceiptResult>(prClient,$"/api/v1/stores/goods-receipts/{grn.Id}/finalize",new FinalizeGoodsReceiptRequest(draftVersion,$"{band.Code}-grn-finalize"));
        Assert.True(replay.Replayed);Assert.Equal(grn.StockPostingBatchId,replay.StockPostingBatchId);Assert.Equal(2,replay.History.Count);
        await using var replayEvidence=new NexaErpDbContext(options);Assert.Single(await replayEvidence.StockPostingBatches.Where(x=>x.GoodsReceiptId==grn.Id).ToListAsync());Assert.Equal(2,await replayEvidence.AuditLogs.CountAsync(x=>x.EntityId==grn.Id.ToString()&&x.Module=="Stores"));
        return grn;
    }

    private static async Task RunQcWitness(HttpClient client,DbContextOptions<NexaErpDbContext> options,TaxWorkflowUser user,PurchaseFlowBand band,GoodsReceiptResult grn,Guid qcId,Guid tdId)
    {
        var lot=grn.Lines.Single().Lots.Single();var serialId=grn.Lines.Single().Serials.SingleOrDefault()?.InventorySerialId;var available=await Query(options,db=>db.WarehouseConditionLocations.Where(x=>x.CompanyId==Guid.Parse("70000000-0000-0000-0000-000000000001")&&x.ConditionCode=="AVAILABLE"&&x.IsActive).OrderBy(x=>x.Id).Select(x=>x.Id).FirstAsync());
        user.Set(qcId,"SESS-33",Rev869ARoleCodes.QcManager);
        var accepted=band.Code=="LOW"?.95m:band.Code=="TD"?1m:0m;var rejected=band.Code=="LOW"?.05m:band.Code=="MD"?1m:0m;var observed=rejected>0?12m:5m;IReadOnlyList<QcSerialDispositionRequest> serials=serialId.HasValue?[new QcSerialDispositionRequest(serialId.Value,accepted>0?"ACCEPTED":"REJECTED",accepted>0?null:"Measured parameter failed")]:Array.Empty<QcSerialDispositionRequest>();
        if(band.Code=="LOW")
        {
            var missingQueue=await Get<PagedResponse<QcQueueItem>>(client,"/api/v1/qc/queue?page=1&pageSize=100");Assert.Contains(missingQueue.Items,x=>x.GoodsReceiptLineLotAllocationId==lot.Id&&!x.HasEffectivePolicy);
            var deniedBody=new FinalizeQcInspectionRequest(lot.Id,DateTimeOffset.UtcNow,accepted,rejected,0,available,[],serials);using var deniedRequest=new HttpRequestMessage(HttpMethod.Post,"/api/v1/qc/inspections"){Content=JsonContent.Create(deniedBody)};deniedRequest.Headers.Add("Idempotency-Key","LOW-qc-missing-policy");using var denied=await client.SendAsync(deniedRequest);Assert.Equal(HttpStatusCode.Conflict,denied.StatusCode);
            Assert.False(await Query(options,db=>db.QcInspections.AnyAsync(x=>x.GoodsReceiptLineLotAllocationId==lot.Id)));Assert.Equal(1m,await Query(options,db=>db.StockMovements.Where(x=>x.GoodsReceiptLineLotAllocationId==lot.Id&&x.ConditionCode=="QC_HOLD").SumAsync(x=>x.QuantityIn-x.QuantityOut)));
            await Query(options,async db=>{var uom=await db.Uoms.OrderBy(x=>x.Code).Select(x=>x.Id).FirstAsync();var p=new QcInspectionPolicy{CompanyId=Guid.Parse("70000000-0000-0000-0000-000000000001"),OrganizationId="SESS_PVT_LTD",ItemId=grn.Lines.Single().ItemId,ParameterCode="DIMENSIONAL_LIMIT",MeasurementUomId=uom,LowerLimit=0,UpperLimit=10,InspectionMethod="Disposable calibrated measurement",SampleSize=1,EffectiveFrom=new DateOnly(2026,1,1),ApprovalStatus="APPROVED",IsActive=true,CreatedBy="PURCHASE_FLOW_TEST"};db.QcInspectionPolicies.Add(p);await db.SaveChangesAsync();return p.Id;});
        }
        var policyId=await Query(options,db=>db.QcInspectionPolicies.Where(x=>x.ItemId==grn.Lines.Single().ItemId&&x.IsActive).Select(x=>x.Id).SingleAsync());var queue=await Get<PagedResponse<QcQueueItem>>(client,"/api/v1/qc/queue?page=1&pageSize=100");Assert.Contains(queue.Items,x=>x.GoodsReceiptLineLotAllocationId==lot.Id&&!x.IsOverdue&&x.HasEffectivePolicy);var request=new FinalizeQcInspectionRequest(lot.Id,DateTimeOffset.UtcNow,accepted,rejected,0,accepted>0?available:null,[new QcParameterResultRequest(policyId,1,observed,null,rejected>0?"FAIL":"PASS",null)],serials);
        var result=await Post<QcInspectionResult>(client,"/api/v1/qc/inspections",request,$"{band.Code}-qc-finalize");Assert.Equal(accepted,result.AcceptedQuantity);Assert.Equal(rejected,result.RejectedQuantity);Assert.NotNull(result.StockPostingBatchId);Assert.False(result.Replayed);
        var replay=await Post<QcInspectionResult>(client,"/api/v1/qc/inspections",request,$"{band.Code}-qc-finalize");Assert.True(replay.Replayed);Assert.Equal(result.RevisionId,replay.RevisionId);Assert.Equal(result.StockPostingBatchId,replay.StockPostingBatchId);
        await using(var evidence=new NexaErpDbContext(options))
        {
            Assert.Single(await evidence.StockPostingBatches.Where(x=>x.QcInspectionRevisionId==result.RevisionId&&x.PostingKind=="QC_DISPOSITION").ToListAsync());var movements=await evidence.StockMovements.Where(x=>x.StockPostingBatchId==result.StockPostingBatchId).ToListAsync();Assert.All(movements,x=>{Assert.NotNull(x.QcInspectionLotDispositionId);Assert.Null(x.QcInspectionRevisionId);});Assert.Equal(accepted,await evidence.StockMovements.Where(x=>x.StockPostingBatchId==result.StockPostingBatchId&&x.ConditionCode=="AVAILABLE").SumAsync(x=>x.QuantityIn-x.QuantityOut));Assert.Equal(rejected,await evidence.StockMovements.Where(x=>x.StockPostingBatchId==result.StockPostingBatchId&&x.ConditionCode=="PENDING_RETURNABLE_DC").SumAsync(x=>x.QuantityIn-x.QuantityOut));
            if(band.Code=="LOW"){var childTypes=await evidence.InventoryProvenanceEdges.Where(x=>movements.Select(m=>m.InventoryProvenanceLayerId).Contains(x.ToProvenanceLayerId)).Join(evidence.InventoryProvenanceLayers,e=>e.ToProvenanceLayerId,l=>l.Id,(e,l)=>l.LayerType).Distinct().ToListAsync();Assert.Contains(InventoryProvenanceLayerTypes.QcAccepted,childTypes);Assert.Contains(InventoryProvenanceLayerTypes.QcRejected,childTypes);}
        }
        if(band.Code=="MD")
        {
            var failed=Assert.Single(result.ParameterResults);var lotDisposition=await Query(options,db=>db.QcInspectionLotDispositions.Where(x=>x.QcInspectionRevisionId==result.RevisionId).Select(x=>x.Id).SingleAsync());var draft=await Post<InventoryConcessionResult>(client,"/api/v1/qc/concessions",new CreateInventoryConcessionRequest(lotDisposition,failed.Id,1,"DIMENSIONAL_LIMIT","12","Technical Director accepts measured deviation for controlled non-critical use","Controlled internal test fixture",[serialId!.Value]),"MD-concession-create");Assert.Equal("DRAFT",draft.Status);Assert.Equal(serialId.Value,Assert.Single(draft.InventorySerialIds));
            user.Set(tdId,"SESS-01",Rev869ARoleCodes.TechnicalDirector);var approved=await Post<InventoryConcessionResult>(client,$"/api/v1/qc/concessions/{draft.ConcessionNumber}/approve",new ApproveInventoryConcessionRequest(draft.Version,available,"Direct technical acceptance"),"MD-concession-approve");Assert.Equal("APPROVED",approved.Status);Assert.NotNull(approved.StockPostingBatchId);Assert.Contains("DIMENSIONAL_LIMIT",approved.ProvenanceAnnotationJson);Assert.Contains(tdId.ToString(),approved.ProvenanceAnnotationJson);
            await using var evidence=new NexaErpDbContext(options);var moves=await evidence.StockMovements.Where(x=>x.StockPostingBatchId==approved.StockPostingBatchId).ToListAsync();Assert.Equal(1m,moves.Where(x=>x.ConditionCode=="PENDING_RETURNABLE_DC").Sum(x=>x.QuantityOut));Assert.Equal(1m,moves.Where(x=>x.ConditionCode=="AVAILABLE").Sum(x=>x.QuantityIn));Assert.All(moves,x=>Assert.Equal(serialId,x.InventorySerialId));Assert.True(await evidence.InventoryProvenanceAnnotations.AnyAsync(x=>x.InventoryConcessionId==approved.Id&&x.InventoryProvenanceLayerId==approved.AvailableProvenanceLayerId));
        }
    }

    private static async Task AssertPrEvidence(DbContextOptions<NexaErpDbContext> options, Guid id, string auditAction,
        int minimumHistory, int minimumAudits)
    {
        await using var db = new NexaErpDbContext(options);
        Assert.True(await db.PurchaseRequisitionStatusHistories.CountAsync(x => x.PurchaseRequisitionId == id) >= minimumHistory);
        Assert.True(await db.AuditLogs.CountAsync(x => x.EntityId == id.ToString()) >= minimumAudits);
        Assert.True(await db.AuditLogs.AnyAsync(x => x.EntityId == id.ToString() && x.Action == auditAction));
    }

    private static async Task AssertTransactionEvidence(DbContextOptions<NexaErpDbContext> options, string type, Guid id, string auditAction)
    {
        await using var db = new NexaErpDbContext(options);
        Assert.True(await db.PurchaseTransactionStatusHistories.AnyAsync(x => x.EntityType == type && x.EntityId == id));
        Assert.True(await db.AuditLogs.AnyAsync(x => x.EntityId == id.ToString() && x.Action == auditAction));
    }

    private static async Task AssertTechnicalEvidence(DbContextOptions<NexaErpDbContext> options, Guid quotationId, Guid verifierId)
    {
        await using var db = new NexaErpDbContext(options);
        var verificationId = await db.QuotationTechnicalVerifications
            .Where(x => x.VendorQuotationLine!.VendorQuotationId == quotationId && x.VerifierEmployeeId == verifierId)
            .Select(x => x.Id).SingleAsync();
        Assert.True(await db.PurchaseTransactionStatusHistories.AnyAsync(x => x.EntityType == "VendorQuotation" && x.EntityId == quotationId && x.Action == "Verify"));
        Assert.True(await db.AuditLogs.AnyAsync(x => x.Action == "TechnicalVerification" &&
            x.EntityId == verificationId.ToString()));
    }

    private static async Task AssertPoEvidence(DbContextOptions<NexaErpDbContext> options, Guid id, string auditAction)
    {
        await using var db = new NexaErpDbContext(options);
        Assert.True(await db.PurchaseOrderHistories.AnyAsync(x => x.PurchaseOrderId == id));
        Assert.True(await db.PurchaseTransactionStatusHistories.AnyAsync(x => x.EntityType == "PurchaseOrder" && x.EntityId == id));
        Assert.True(await db.AuditLogs.AnyAsync(x => x.EntityId == id.ToString() && x.Action == auditAction));
    }

    private static async Task AssertApprovalActors(DbContextOptions<NexaErpDbContext> options, string kind, Guid id,
        int steps, Guid level1, Guid? level2)
    {
        await using var db = new NexaErpDbContext(options);
        var actors = kind switch
        {
            "PR" => await db.PurchaseRequisitionApprovalHistories.Where(x => x.PurchaseRequisitionId == id && x.Action == "Approve")
                .OrderBy(x => x.StepNumber).Select(x => x.ResolvedEmployeeId).ToListAsync(),
            "CMP" => await db.PurchaseTransactionApprovalHistories.Where(x => x.CommercialComparisonId == id && x.Action == "Approve")
                .OrderBy(x => x.StepNumber).Select(x => x.ResolvedEmployeeId).ToListAsync(),
            _ => await db.PurchaseOrderHistories.Where(x => x.PurchaseOrderId == id && x.Action == "Approve")
                .OrderBy(x => x.StepNumber).Select(x => x.ResolvedEmployeeId!.Value).ToListAsync()
        };
        Assert.Equal(steps, actors.Count);
        Assert.Equal(level1, actors[0]);
        if (level2.HasValue) Assert.Equal(level2.Value, actors[1]);
        Assert.True(await db.AuditLogs.AnyAsync(x => x.EntityId == id.ToString() && x.Action.Contains("Approve")));
    }

    private static async Task<Guid> Employee(NexaErpDbContext db, string code) =>
        await db.Employees.Where(x => x.EmployeeCode == code).Select(x => x.Id).SingleAsync();

    private static async Task<T> Query<T>(DbContextOptions<NexaErpDbContext> options,
        Func<NexaErpDbContext, Task<T>> query)
    {
        await using var db = new NexaErpDbContext(options);
        return await query(db);
    }

    private static async Task<T> Post<T>(HttpClient client, string path, object body, string? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        if (!string.IsNullOrWhiteSpace(key)) request.Headers.Add("Idempotency-Key", key);
        using var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{path} returned {(int)response.StatusCode} {response.StatusCode}: {payload}");
        return JsonSerializer.Deserialize<T>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"{path} returned an empty response.");
    }

    private static async Task PostNoResult(HttpClient client, string path, object body, string key) =>
        _ = await Post<JsonElement>(client, path, body, key);

    private static async Task<T> Put<T>(HttpClient client,string path,object body)
    { using var response=await client.PutAsJsonAsync(path,body);var payload=await response.Content.ReadAsStringAsync();Assert.True(response.IsSuccessStatusCode,$"{path} returned {(int)response.StatusCode}: {payload}");return JsonSerializer.Deserialize<T>(payload,new JsonSerializerOptions(JsonSerializerDefaults.Web))!; }
    private static async Task<T> Get<T>(HttpClient client,string path)
    { using var response=await client.GetAsync(path);var payload=await response.Content.ReadAsStringAsync();Assert.True(response.IsSuccessStatusCode,$"{path} returned {(int)response.StatusCode}: {payload}");return JsonSerializer.Deserialize<T>(payload,new JsonSerializerOptions(JsonSerializerDefaults.Web))!; }

    private static int FreePurchaseFlowPort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class PurchaseFlowHost(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public static async Task<PurchaseFlowHost> StartAsync(string connectionString, TaxWorkflowUser user)
        {
            var port = FreePurchaseFlowPort();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NexaErp"] = connectionString,
                ["MasterDataTransfer:MaxRows"] = "1000",
                ["MasterDataTransfer:SensitiveRowRetentionDays"] = "90"
            });
            builder.Services.AddRouting();
            builder.Services.AddAuthentication(PurchaseFlowAuthentication.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, PurchaseFlowAuthentication>(
                    PurchaseFlowAuthentication.SchemeName, _ => { });
            builder.Services.AddAuthorization();
            builder.Services.ConfigureHttpJsonOptions(x => ApiJsonContract.Configure(x.SerializerOptions));
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddSingleton<ICurrentUser>(user);
            builder.Services.AddSingleton<IRecordScopeAuthorizer, PurchaseFlowAllowingScope>();
            builder.Services.AddSingleton<IPagePermissionService, PurchaseFlowAllowingPermissions>();
            var app = builder.Build();
            app.UseMiddleware<StandardErrorEnvelopeMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapRev869AConfigurationEndpoints();
            app.MapEmployeeEndpoints();
            app.MapPurchaseRequisitionEndpoints();
            app.MapRev869BPurchaseEndpoints();
            app.MapStoresGateEntryEndpoints();
            app.MapStoresGoodsReceiptEndpoints();
            app.MapQcEndpoints();
            await app.StartAsync();
            var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            client.DefaultRequestHeaders.Authorization = new("PurchaseFlow");
            return new PurchaseFlowHost(app, client);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private sealed record PurchaseFlowBand(string Code, decimal PrAmount, decimal QuoteRate, int RequiredSteps,
        Guid Level1EmployeeId, Guid? Level2EmployeeId);

    private sealed class PurchaseFlowAllowingScope : IRecordScopeAuthorizer
    {
        public Task<RecordScopeDecision> AuthorizeAnyAsync(Guid employeeId, string roleCode, string organizationId,
            DateOnly onDate, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(true, "disposable test scope"));
        public Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target,
            DateOnly onDate, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(true, "disposable test scope"));
    }

    private sealed class PurchaseFlowAllowingPermissions : IPagePermissionService
    {
        public Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roleCodes, string pageKey, string permission,
            CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class PurchaseFlowAuthentication(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "PurchaseFlow";
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization")) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "PURCHASE-FLOW")], SchemeName);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }
}
