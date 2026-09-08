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
using SESS.NexaERP.Domain.Sales;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure;
using SESS.NexaERP.Infrastructure.Persistence;

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
        Guid productionId;
        Guid accountsSupportId;
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
            productionId = await Employee(seed, "SESS-25");
            accountsSupportId = await Employee(seed, "SESS-41");
            var identities = new[]
            {
                (creatorId, "SESS-12"), (managerId, "SESS-14"), (tdId, "SESS-01"),
                (mdId, "SESS-02"), (verifierId, "SESS-05"), (purchaseId, "SESS-15"), (storesId, "SESS-35"),
                (qcId, "SESS-33"), (productionId, "SESS-25"), (accountsSupportId, "SESS-41")
            };
            var identityEmployeeIds = identities.Select(x => x.Item1).ToArray();
            await seed.Employees.Where(x => identityEmployeeIds.Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(e => e.LoginEnabled, true));
            seed.EmployeeIdentityMappings.AddRange(identities.Select(x => Mapping(companyId, x.Item1, x.Item2)));
            seed.EmployeeOperationalScopes.AddRange(identities
                .Where(x => x.Item1 != managerId && x.Item1 != tdId && x.Item1 != mdId)
                .Select(x => new EmployeeOperationalScope
            {
                CompanyId = companyId, OrganizationId = "SESS_PVT_LTD", EmployeeId = x.Item1,
                DepartmentId = departmentId, WarehouseId = warehouseId, OwnRecordsOnly = false,
                AllowsPrivilegedCrossScope = false, EffectiveFrom = new DateOnly(2026, 1, 1),
                IsActive = true, Remarks = "Disposable full Purchase flow", CreatedBy = "PURCHASE_FLOW_TEST"
            }));
            await seed.SaveChangesAsync();
        }

        const string runtimePassword = "ordinary-purchase-runtime-123456789";
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, runtimePassword);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        var roleAssignments = await Query(options, async db => (await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
            .Where(x => x.CompanyId == Guid.Parse("70000000-0000-0000-0000-000000000001") && x.EffectiveTo == null)
            .ToListAsync()).ToDictionary(x => TaxWorkflowUser.AssignmentKey(x.EmployeeId, x.Role!.Code),
                x => new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType)));
        var user = new TaxWorkflowUser(purchaseId, "SESS-15", Rev869ARoleCodes.StoresExecutive, roleAssignments);
        var runtimeConnection = new Npgsql.NpgsqlConnectionStringBuilder(server.ConnectionString)
        {
            Username = "nexa_erp_runtime",
            Password = runtimePassword,
            Pooling = false
        }.ConnectionString;
        await AssertVendorBillRuntimeTableDmlRefused(runtimeConnection);
        await using var adminHost = await PurchaseFlowHost.StartAsync(server.ConnectionString, user);
        await using var runtimeHost = await PurchaseFlowHost.StartAsync(runtimeConnection, user);
        await using var approvalHost = await PurchaseFlowHost.StartAsync(server.ConnectionString, user, useRealPagePermissions: true);
        var adminClient = adminHost.Client;
        var client = runtimeHost.Client;
        var approvalClient = approvalHost.Client;

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
                var provenanceFacts = await Query(options, async db =>
                {
                    var current = await db.VendorQualifications.Where(x => x.Id == qualification.Id)
                        .Select(x => new { x.Version, x.VerifiedByEmployeeId, x.ApprovedByEmployeeId }).SingleAsync();
                    var histories = await db.ControlledConfigurationHistories
                        .Where(x => x.EntityType == nameof(VendorQualification) && x.EntityId == qualification.Id)
                        .OrderBy(x => x.Version).Select(x => new { x.Action, x.Version, x.ActorLoginId }).ToListAsync();
                    return new { current, histories };
                });
                Assert.NotNull(provenanceFacts.current.VerifiedByEmployeeId);
                Assert.NotNull(provenanceFacts.current.ApprovedByEmployeeId);
                Assert.NotEqual(provenanceFacts.current.VerifiedByEmployeeId, provenanceFacts.current.ApprovedByEmployeeId);
                Assert.Equal(new[] { "Create", "Verify", "Approve" }, provenanceFacts.histories.Select(x => x.Action));
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
            user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseExecutive);
            Assert.NotEmpty(await Get<PurchaseRequisitionLookupOption[]>(approvalClient, "/api/v1/purchase/requisitions/lookups/departments"));
            Assert.NotEmpty(await Get<PurchaseRequisitionLookupOption[]>(approvalClient, "/api/v1/purchase/requisitions/lookups/warehouses"));
            Assert.NotEmpty(await Get<PurchaseRequisitionLookupOption[]>(approvalClient, "/api/v1/purchase/requisitions/lookups/items"));

            user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
            using (var refused = await approvalClient.PostAsJsonAsync("/api/v1/purchase/requisitions",
                new CreatePurchaseRequisitionRequest("SESS_PVT_LTD", "PRODUCTION", "SESS-01",
                    DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30), "NORMAL",
                    "Must be rejected outside requester scope", "TRIAL-WH-C01", null, null, null, null, null,
                    [new("TRIAL-ITEM-001", 1, 100m, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
                        "TRIAL-WH-C01", null, null, null)])))
            {
                Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
            }

            var grns=new List<GoodsReceiptResult>();
            foreach (var band in bands)
                grns.Add(await RunPurchaseBand(adminClient, approvalClient, client, options, user, band, creatorId, managerId, tdId, mdId,
                    verifierId, purchaseId, storesId, qcId, vendor1Id, vendor2Id));
            for(var i=0;i<grns.Count;i++)await RunQcWitness(approvalClient,options,user,bands[i],grns[i],qcId,tdId);
            await RunVendorBillWitness(client, options, user, grns, managerId, accountsSupportId);
            await RunMaterialIssueWitness(client, options, user, grns[0], grns[2], verifierId,
                purchaseId, productionId, storesId, tdId, managerId);

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
            Assert.Equal(3, await verify.FifoInventoryCostLayers.CountAsync());
            Assert.Single(await verify.JobOrders.Where(x => x.CustomerPurchaseOrderId != null).ToListAsync());
            Assert.Equal(2, await verify.JobOrderHistories.CountAsync());
            Assert.Equal(4, await verify.VendorBills.CountAsync());
            Assert.Equal(4, await verify.VendorBillLines.CountAsync());
            Assert.Equal(3, await verify.VendorBillCostAllocations.CountAsync());
            Assert.Equal(9, await verify.VendorBillHistories.CountAsync());
            var fifoLayers = await verify.FifoInventoryCostLayers.OrderBy(x => x.ReceivedAt).ThenBy(x => x.Id).ToListAsync();
            Assert.Equal(grns.Select(x => x.Lines.Single().Id), fifoLayers.Select(x => x.GoodsReceiptLineId));
            var fifoUse = await verify.FifoCostConsumptions.GroupBy(x => x.FifoInventoryCostLayerId)
                .Select(x => new { LayerId = x.Key, Quantity = x.Sum(y => y.Quantity) }).ToDictionaryAsync(x => x.LayerId, x => x.Quantity);
            Assert.Equal(1m, fifoUse[fifoLayers[0].Id]);
            Assert.Equal(1m, fifoUse[fifoLayers[1].Id]);
            Assert.False(fifoUse.ContainsKey(fifoLayers[2].Id));
            var serializedIssueLine = await verify.MaterialIssueLines.SingleAsync(x => x.InventorySerialId != null);
            Assert.Equal(grns[2].Lines.Single().Id, serializedIssueLine.OriginGoodsReceiptLineId);
            var serializedCostLayer = await verify.FifoCostConsumptions.Where(x => x.MaterialIssueLineId == serializedIssueLine.Id)
                .Select(x => x.FifoInventoryCostLayer!.GoodsReceiptLineId).SingleAsync();
            Assert.Equal(grns[1].Lines.Single().Id, serializedCostLayer);
            Assert.NotEqual(serializedIssueLine.OriginGoodsReceiptLineId, serializedCostLayer);
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
            var commandCount=await verify.Database.SqlQueryRaw<int>(@"SELECT count(*)::integer AS ""Value"" FROM advance.command_requests").SingleAsync();
            var receiptCount=await verify.Database.SqlQueryRaw<int>(@"SELECT count(*)::integer AS ""Value"" FROM advance.command_receipts").SingleAsync();
            Assert.True(commandCount>0);Assert.Equal(commandCount,receiptCount);
            var operations=await verify.Database.SqlQueryRaw<string>(@"SELECT DISTINCT ""Operation"" AS ""Value"" FROM advance.command_requests").ToListAsync();
            Assert.All(new[]{"CreateVendorQualification","VerifyVendorQualification","ApproveVendorQualification",
                "CreateTaxGstSetting","ApproveTaxGstSetting","CreateRFQ","InviteVendor","SubmitQuotation",
                "TechnicalVerification","CreateComparison","RecommendComparison","ApproveComparison",
                "CreatePO","SubmitPO","ApprovePO","IssuePO","EstimatedBom.Create","EstimatedBom.Submit",
                "EstimatedBom.Approve","ProductionBom.Create","ProductionBom.Submit","ProductionBom.Approve",
                "ProductionBom.Pin","MaterialIssueRequest.Create","MaterialIssueRequest.Submit",
                "MaterialReturn.Create","MaterialReturn.Accept","VendorBill.Create","VendorBill.Accept","VendorBill.Reject","VendorBill.Reverse",
                "JobOrder.Create","JobOrder.AccountsConfirm",
                "MaterialIssueRequest.Approve","MaterialIssueRequest.DecideExcess","MaterialIssue.Issue"},
                operation=>Assert.Contains(operation,operations));
            var commandAudits=await verify.AuditLogs.Where(x=>x.Result=="Success"&&operations.Contains(x.Action)).ToListAsync();
            Assert.NotEmpty(commandAudits);
            Assert.All(commandAudits,x=>
            {
                Assert.False(string.IsNullOrWhiteSpace(x.ActorRoleCode));
                Assert.NotNull(x.ResolvedRoleAssignmentId);
            });

        }
        finally { }
    }

    private static async Task<GoodsReceiptResult> RunPurchaseBand(HttpClient prClient, HttpClient approvalClient, HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, PurchaseFlowBand band, Guid creatorId, Guid managerId, Guid tdId, Guid mdId,
        Guid verifierId, Guid purchaseId, Guid storesId, Guid qcId, Guid vendor1Id, Guid vendor2Id)
    {
        var required = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        user.Set(creatorId, "SESS-12", "IT_MANAGER");
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
            new PurchaseRequisitionActionRequest(null, pr.Version, $"{band.Code}-pr-submit"));
        Assert.Equal(PurchaseRequisitionStatuses.Submitted, pr.Status);
        await AssertPrEvidence(options, pr.Id, "Submit", 2, 2);
        using(var refused=await prClient.PostAsJsonAsync($"/api/v1/purchase/requisitions/{pr.PrNumber}/verify",
            new PurchaseRequisitionActionRequest("Requester must not verify",pr.Version,$"{band.Code}-pr-self-verify")))
            Assert.Equal(HttpStatusCode.Forbidden,refused.StatusCode);
        user.Set(managerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        pr = await Post<PurchaseRequisitionDetail>(approvalClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/verify",
            new PurchaseRequisitionActionRequest("Department verified", pr.Version, $"{band.Code}-pr-verify"));
        Assert.Equal(PurchaseRequisitionStatuses.PendingApproval, pr.Status);
        await AssertPrEvidence(options, pr.Id, "DepartmentVerify", 3, 3);

        var managerQueue = await Get<PagedResponse<PurchaseRequisitionSummary>>(approvalClient,
            $"/api/v1/purchase/requisitions?prNumber={pr.PrNumber}");
        Assert.Equal(pr.Id, Assert.Single(managerQueue.Items).Id);
        Assert.Equal(pr.Id, (await Get<PurchaseRequisitionDetail>(approvalClient,
            $"/api/v1/purchase/requisitions/{pr.PrNumber}")).Id);
        pr = await Post<PurchaseRequisitionDetail>(approvalClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/approve",
            new PurchaseRequisitionActionRequest("Level 1 approved", pr.Version, $"{band.Code}-pr-approve-1"));
        Assert.Equal(band.RequiredSteps == 1 ? PurchaseRequisitionStatuses.StockCheckPending : PurchaseRequisitionStatuses.PendingApproval, pr.Status);
        if (band.Level2EmployeeId.HasValue)
        {
            var role = band.Level2EmployeeId == tdId ? Rev869ARoleCodes.TechnicalDirector : Rev869ARoleCodes.ManagingDirector;
            user.Set(band.Level2EmployeeId.Value, role == Rev869ARoleCodes.TechnicalDirector ? "SESS-01" : "SESS-02", role);
            var level2Queue = await Get<PagedResponse<PurchaseRequisitionSummary>>(approvalClient,
                $"/api/v1/purchase/requisitions?prNumber={pr.PrNumber}");
            Assert.Equal(pr.Id, Assert.Single(level2Queue.Items).Id);
            pr = await Post<PurchaseRequisitionDetail>(approvalClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/approve",
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
        user.Set(verifierId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER");
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
        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresExecutive);
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
        grn=await Post<GoodsReceiptResult>(client,$"/api/v1/stores/goods-receipts/{grn.Id}/finalize",new FinalizeGoodsReceiptRequest(draftVersion,$"{band.Code}-grn-finalize"));
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
        var replay=await Post<GoodsReceiptResult>(client,$"/api/v1/stores/goods-receipts/{grn.Id}/finalize",new FinalizeGoodsReceiptRequest(draftVersion,$"{band.Code}-grn-finalize"));
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
        var policyId=await Query(options,db=>db.QcInspectionPolicies.Where(x=>x.ItemId==grn.Lines.Single().ItemId&&x.IsActive).Select(x=>x.Id).SingleAsync());var queue=await Get<PagedResponse<QcQueueItem>>(client,"/api/v1/qc/queue?page=1&pageSize=100");var queueItem=Assert.Single(queue.Items,x=>x.GoodsReceiptLineLotAllocationId==lot.Id&&!x.IsOverdue&&x.HasEffectivePolicy);if(serialId.HasValue)Assert.Equal(serialId.Value,Assert.Single(queueItem.InventorySerialIds));var request=new FinalizeQcInspectionRequest(lot.Id,DateTimeOffset.UtcNow,accepted,rejected,0,accepted>0?available:null,[new QcParameterResultRequest(policyId,1,observed,null,rejected>0?"FAIL":"PASS",null)],serials);
        var result=await Post<QcInspectionResult>(client,"/api/v1/qc/inspections",request,$"{band.Code}-qc-finalize");Assert.Equal(accepted,result.AcceptedQuantity);Assert.Equal(rejected,result.RejectedQuantity);Assert.NotNull(result.StockPostingBatchId);Assert.False(result.Replayed);
        var replay=await Post<QcInspectionResult>(client,"/api/v1/qc/inspections",request,$"{band.Code}-qc-finalize");Assert.True(replay.Replayed);Assert.Equal(result.RevisionId,replay.RevisionId);Assert.Equal(result.StockPostingBatchId,replay.StockPostingBatchId);
        await using(var evidence=new NexaErpDbContext(options))
        {
            Assert.Single(await evidence.StockPostingBatches.Where(x=>x.QcInspectionRevisionId==result.RevisionId&&x.PostingKind=="QC_DISPOSITION").ToListAsync());var movements=await evidence.StockMovements.Where(x=>x.StockPostingBatchId==result.StockPostingBatchId).ToListAsync();Assert.All(movements,x=>{Assert.NotNull(x.QcInspectionLotDispositionId);Assert.Null(x.QcInspectionRevisionId);});Assert.Equal(accepted,await evidence.StockMovements.Where(x=>x.StockPostingBatchId==result.StockPostingBatchId&&x.ConditionCode=="AVAILABLE").SumAsync(x=>x.QuantityIn-x.QuantityOut));Assert.Equal(rejected,await evidence.StockMovements.Where(x=>x.StockPostingBatchId==result.StockPostingBatchId&&x.ConditionCode=="PENDING_RETURNABLE_DC").SumAsync(x=>x.QuantityIn-x.QuantityOut));
            if(band.Code=="LOW"){var childTypes=await evidence.InventoryProvenanceEdges.Where(x=>movements.Select(m=>m.InventoryProvenanceLayerId).Contains(x.ToProvenanceLayerId)).Join(evidence.InventoryProvenanceLayers,e=>e.ToProvenanceLayerId,l=>l.Id,(e,l)=>l.LayerType).Distinct().ToListAsync();Assert.Contains(InventoryProvenanceLayerTypes.QcAccepted,childTypes);Assert.Contains(InventoryProvenanceLayerTypes.QcRejected,childTypes);}
        }
        if(band.Code=="MD")
        {
            var failed=Assert.Single(result.ParameterResults);var concessionSerialId=Assert.Single(result.SerialDispositions).InventorySerialId;var draft=await Post<InventoryConcessionResult>(client,"/api/v1/qc/concessions",new CreateInventoryConcessionRequest(result.QcInspectionLotDispositionId,failed.Id,1,"DIMENSIONAL_LIMIT","12","Technical Director accepts measured deviation for controlled non-critical use","Controlled internal test fixture",[concessionSerialId]),"MD-concession-create");Assert.Equal("DRAFT",draft.Status);Assert.Equal(concessionSerialId,Assert.Single(draft.InventorySerialIds));
            user.Set(tdId,"SESS-01",Rev869ARoleCodes.TechnicalDirector);var approved=await Post<InventoryConcessionResult>(client,$"/api/v1/qc/concessions/{draft.ConcessionNumber}/approve",new ApproveInventoryConcessionRequest(draft.Version,available,"Direct technical acceptance"),"MD-concession-approve");Assert.Equal("APPROVED",approved.Status);Assert.NotNull(approved.StockPostingBatchId);Assert.Contains("DIMENSIONAL_LIMIT",approved.ProvenanceAnnotationJson);Assert.Contains(tdId.ToString(),approved.ProvenanceAnnotationJson);
            await using var evidence=new NexaErpDbContext(options);var moves=await evidence.StockMovements.Where(x=>x.StockPostingBatchId==approved.StockPostingBatchId).ToListAsync();Assert.Equal(1m,moves.Where(x=>x.ConditionCode=="PENDING_RETURNABLE_DC").Sum(x=>x.QuantityOut));Assert.Equal(1m,moves.Where(x=>x.ConditionCode=="AVAILABLE").Sum(x=>x.QuantityIn));Assert.All(moves,x=>Assert.Equal(serialId,x.InventorySerialId));Assert.True(await evidence.InventoryProvenanceAnnotations.AnyAsync(x=>x.InventoryConcessionId==approved.Id&&x.InventoryProvenanceLayerId==approved.AvailableProvenanceLayerId));
        }
    }

    private static async Task RunMaterialIssueWitness(HttpClient client,
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user, GoodsReceiptResult grn, GoodsReceiptResult serializedGrn,
        Guid engineerId, Guid purchaseId, Guid productionId, Guid storesId, Guid tdId, Guid accountsManagerId)
    {
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var itemId = grn.Lines.Single().ItemId;
        var fixture = await Query(options, async db =>
        {
            var item = await db.Items.SingleAsync(x => x.Id == itemId);
            var customer = new Customer
            {
                CustomerCode = "MIR-WITNESS-CUSTOMER", IsCustomerCodeLocked = true,
                Name = "MIR Witness Customer", LegalCustomerName = "MIR Witness Customer Private Limited",
                CustomerType = "BUSINESS", PortalOrganizationId = "MIR_WITNESS_CUSTOMER",
                Status = MasterStatuses.Active, ApprovalStatus = MasterApprovalStatuses.Approved,
                ApprovedBy = "MIR_WITNESS", ApprovedAt = DateTimeOffset.UtcNow,
                IsActive = true, CreatedBy = "MIR_WITNESS"
            };
            var productionDepartmentId = await db.Departments.Where(x => x.Code == "PRODUCTION").Select(x => x.Id).SingleAsync();
            var cpo = new CustomerPurchaseOrder
            {
                CompanyId = companyId, CustomerId = customer.Id, PoRecordNumber = "CPO-MIR-WITNESS-001",
                CustomerPoNumber = "CUSTOMER-MIR-WITNESS-001", CustomerPoDate = new DateOnly(2026, 9, 7),
                WorkStatus = CustomerPoWorkStatuses.Wip, CurrentRevisionNumber = 1,
                CreatedBy = "MIR_WITNESS"
            };
            var revision = new CustomerPurchaseOrderRevision
            {
                CustomerPurchaseOrderId = cpo.Id, RevisionNumber = 1,
                ChangeReason = "Material issue witness baseline", SnapshotJson = "{}", CreatedBy = "MIR_WITNESS"
            };
            var line = new CustomerPurchaseOrderLine
            {
                CustomerPurchaseOrderId = cpo.Id, RevisionNumber = 1, SlNo = 1,
                ItemId = item.Id, UomId = item.BaseUomId, Description = item.Name,
                Quantity = .90m, Uom = await db.Uoms.Where(x => x.Id == item.BaseUomId).Select(x => x.Code).SingleAsync(),
                CreatedBy = "MIR_WITNESS"
            };
            var machineLine = new CustomerPurchaseOrderLine
            {
                CustomerPurchaseOrderId = cpo.Id, RevisionNumber = 1, SlNo = 2,
                ItemId = item.Id, UomId = item.BaseUomId, Description = "Witness chamber machine",
                Quantity = 1m, Uom = line.Uom, CreatedBy = "MIR_WITNESS"
            };
            revision.Lines.Add(line); revision.Lines.Add(machineLine); cpo.Revisions.Add(revision);
            db.Customers.Add(customer); db.CustomerPurchaseOrders.Add(cpo);
            await db.SaveChangesAsync();
            return new { CpoLineId = line.Id, MachineLineId = machineLine.Id,
                UomId = item.BaseUomId, ItemCode = item.ItemCode, DepartmentId = productionDepartmentId };
        });

        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        var jobCommand = new CreateJobOrderRequest(fixture.MachineLineId, 1, "WITNESS-MACHINE-001",
            new DateOnly(2026, 9, 7), new DateOnly(2026, 12, 1), "job-order-create");
        var job = await Post<JobOrderView>(client, "/api/v1/production/job-orders", jobCommand);
        var jobReplay = await Post<JobOrderView>(client, "/api/v1/production/job-orders", jobCommand);
        Assert.Equal("PENDING_ACCOUNTS", job.Status); Assert.Equal(job.Id, jobReplay.Id);
        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        await AssertPostStatus(client, "/api/v1/design/estimated-boms",
            new CreateEstimatedBomRequest(job.Id, "Must wait for Accounts",
                [new EstimatedBomLineInput(itemId, fixture.UomId, .90m, "Witness component")], "mir-est-before-accounts"),
            HttpStatusCode.Conflict);
        user.Set(accountsManagerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        var confirm = new ConfirmJobOrderRequest(job.Version, "Customer PO and one-machine scope verified", "job-order-accounts-confirm");
        job = await Post<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}/accounts-confirm", confirm);
        var confirmReplay = await Post<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}/accounts-confirm", confirm);
        Assert.Equal("OPEN", job.Status); Assert.Equal(job.Id, confirmReplay.Id);
        Assert.NotEqual(job.InitiatedByEmployeeId, job.AccountsConfirmedByEmployeeId);

        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        var estimated = await Post<EstimatedBomView>(client, "/api/v1/design/estimated-boms",
            new CreateEstimatedBomRequest(job.Id, "Witness commercial baseline",
                [new EstimatedBomLineInput(itemId, fixture.UomId, .90m, "Witness component")], "mir-est-create"));
        estimated = await Post<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/submit",
            new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Ready for TD approval", "mir-est-submit"));
        user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
        estimated = await Post<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/approve",
            new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Commercial baseline approved", "mir-est-approve"));

        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        var production = await Post<ProductionBomView>(client, "/api/v1/production/boms",
            new CreateProductionBomRequest(job.Id, "Witness production baseline", "mir-pbom-create"));
        production = await Post<ProductionBomView>(client, $"/api/v1/production/boms/{production.BomNumber}/submit",
            new ProductionBomActionRequest(production.CurrentRevision.Version, "Production baseline submitted", "mir-pbom-submit"));
        user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
        production = await Post<ProductionBomView>(client, $"/api/v1/production/boms/{production.BomNumber}/approve",
            new ProductionBomActionRequest(production.CurrentRevision.Version, "Production baseline approved", "mir-pbom-approve"));
        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        production = await Post<ProductionBomView>(client, $"/api/v1/production/boms/{production.BomNumber}/pin",
            new PinProductionBomRevisionRequest(production.CurrentRevision.Id, job.Version,
                "Pinned to the one-machine Job Order", "mir-pbom-pin"));
        Assert.Equal(production.CurrentRevision.Id, production.PinnedRevisionId);

        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.StoresExecutive);
        var mir = await Post<MaterialIssueRequestView>(client, "/api/v1/stores/material-issue-requests",
            new CreateMaterialIssueRequest("FACTORY_ASSEMBLY", "CHAMBER_MANUFACTURE", "JOB_ORDER",
                job.Id, null, null, null, "Witness chamber", fixture.DepartmentId,
                new DateOnly(2026, 9, 8),
                [new MaterialIssueRequestLineInput(itemId, fixture.UomId, .95m, fixture.CpoLineId, null)],
                "mir-customer-create"));
        Assert.Equal(.05m, Assert.Single(mir.Lines).ExcessBaseQuantity);
        mir = await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{mir.Id}/submit",
            new MaterialIssueTransitionRequest(mir.Version, "Required for chamber assembly", "mir-customer-submit"));

        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        var issueCommand = new CreateMaterialIssue("mir-customer-issue", engineerId,
            DateTimeOffset.UtcNow, [new MaterialIssueScan(mir.Lines.Single().Id, fixture.ItemCode, null, .95m)]);
        await AssertPostStatus(client, $"/api/v1/stores/material-issues/from-request/{mir.Id}",
            issueCommand, HttpStatusCode.Conflict);

        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        mir = await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{mir.Id}/approve",
            new MaterialIssueTransitionRequest(mir.Version, "Production approves requirement", "mir-customer-approve"));
        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        await AssertPostStatus(client, $"/api/v1/stores/material-issues/from-request/{mir.Id}",
            issueCommand, HttpStatusCode.Conflict);

        user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
        mir = await Post<MaterialIssueRequestView>(client,
            $"/api/v1/stores/material-issue-excess/{mir.Lines.Single().Id}/decision",
            new MaterialIssueExcessDecisionRequest("APPROVED", "Controlled 0.05 base-unit excess",
                "mir-excess-approve"));
        Assert.True(Assert.Single(mir.Lines).TdDecisionPresent);
        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        var issue = await Post<MaterialIssueView>(client,
            $"/api/v1/stores/material-issues/from-request/{mir.Id}", issueCommand);
        var replay = await Post<MaterialIssueView>(client,
            $"/api/v1/stores/material-issues/from-request/{mir.Id}", issueCommand);
        Assert.False(issue.Replayed); Assert.True(replay.Replayed); Assert.Equal(issue.Id, replay.Id);
        Assert.Equal(job.Id, issue.JobOrderId); Assert.Equal(engineerId, issue.IssuedToEmployeeId);

        var consumable = await CreateAndIssueConsumable(client, user, itemId, fixture.UomId,
            fixture.ItemCode, fixture.DepartmentId, engineerId, purchaseId, productionId, storesId);
        await CreateIssueAndReturnSerialized(client, options, user, serializedGrn, fixture.UomId,
            fixture.DepartmentId, engineerId, purchaseId, productionId, storesId);

        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        await AssertPostStatus(client, $"/api/v1/stores/material-returns/from-issue/{issue.Id}",
            new CreateMaterialReturn(DateTimeOffset.UtcNow,
                [new MaterialReturnLineInput(issue.Lines.Single().Id, "NOT-THE-ISSUED-ITEM", .60m, 0, .35m)],
                "material-return-bad-scan"), HttpStatusCode.BadRequest);
        var returnCommand = new CreateMaterialReturn(DateTimeOffset.UtcNow,
            [new MaterialReturnLineInput(issue.Lines.Single().Id, fixture.ItemCode, .60m, 0, .35m)],
            "material-return-create");
        var materialReturn = await Post<MaterialReturnView>(client,
            $"/api/v1/stores/material-returns/from-issue/{issue.Id}", returnCommand);
        var returnReplay = await Post<MaterialReturnView>(client,
            $"/api/v1/stores/material-returns/from-issue/{issue.Id}", returnCommand);
        Assert.False(materialReturn.Replayed); Assert.True(returnReplay.Replayed);
        Assert.Equal(materialReturn.Id, returnReplay.Id); Assert.Equal("SUBMITTED", materialReturn.Status);
        Assert.Equal(0, await Query(options, db => db.StockPostingBatches.CountAsync(x => x.MaterialReturnId == materialReturn.Id)));
        await AssertPostStatus(client, $"/api/v1/stores/material-returns/from-issue/{issue.Id}",
            new CreateMaterialReturn(DateTimeOffset.UtcNow,
                [new MaterialReturnLineInput(issue.Lines.Single().Id, fixture.ItemCode, .40m, 0, 0)],
                "material-return-over"), HttpStatusCode.Conflict);

        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.StoresExecutive,
            Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.StoresExecutive);
        await AssertPostStatus(client, $"/api/v1/stores/material-returns/{materialReturn.Id}/accept",
            new AcceptMaterialReturn(materialReturn.Version, DateTimeOffset.UtcNow,
                "SUPPORT authority must not accept custody", "material-return-support-refused"),
            HttpStatusCode.Forbidden);

        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        var accept = new AcceptMaterialReturn(materialReturn.Version, DateTimeOffset.UtcNow,
            "Scanner-confirmed return accepted into Stores", "material-return-accept");
        materialReturn = await Post<MaterialReturnView>(client,
            $"/api/v1/stores/material-returns/{materialReturn.Id}/accept", accept);
        var acceptReplay = await Post<MaterialReturnView>(client,
            $"/api/v1/stores/material-returns/{materialReturn.Id}/accept", accept);
        Assert.Equal("ACCEPTED", materialReturn.Status); Assert.False(materialReturn.Replayed);
        Assert.True(acceptReplay.Replayed); Assert.Equal(materialReturn.StockPostingBatchId, acceptReplay.StockPostingBatchId);
        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        var consumableReturn = await Post<MaterialReturnView>(client,
            $"/api/v1/stores/material-returns/from-issue/{consumable.Id}",
            new CreateMaterialReturn(DateTimeOffset.UtcNow,
                [new MaterialReturnLineInput(consumable.Lines.Single().Id, fixture.ItemCode, .03m, 0, .02m)],
                "material-return-consumable-create"));
        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        consumableReturn = await Post<MaterialReturnView>(client,
            $"/api/v1/stores/material-returns/{consumableReturn.Id}/accept",
            new AcceptMaterialReturn(consumableReturn.Version, DateTimeOffset.UtcNow,
                "Consumable remainder accepted exactly like any other material", "material-return-consumable-accept"));

        await using var evidence = new NexaErpDbContext(options);
        var issueIds = new[] { issue.Id, consumable.Id };
        var batches = await evidence.StockPostingBatches.Where(x => issueIds.Contains(x.MaterialIssueId!.Value)).ToListAsync();
        Assert.Equal(2, batches.Count);
        var movements = await evidence.StockMovements.Where(x => batches.Select(b => b.Id).Contains(x.StockPostingBatchId!.Value))
            .Include(x => x.CustodyAssignment)!.ThenInclude(x => x!.CustodyAccount).ToListAsync();
        Assert.DoesNotContain(movements, x => x.MovementType == "CONSUMPTION_OUT");
        Assert.Equal(0m, movements.Sum(x => x.QuantityIn - x.QuantityOut));
        Assert.All(movements.GroupBy(x => x.MaterialIssueLineId), pair =>
        {
            Assert.Equal(2, pair.Count()); Assert.Single(pair.Select(x => x.OwnershipAccountId).Distinct());
            Assert.Single(pair.Select(x => x.InventoryProvenanceLayerId).Distinct());
            Assert.Single(pair, x => x.QuantityOut > 0 && x.CustodyAssignment!.CustodyAccount!.CustodyType == "WAREHOUSE");
            Assert.Single(pair, x => x.QuantityIn > 0 && x.CustodyAssignment!.CustodyAccount!.CustodyType == "EMPLOYEE");
        });
        Assert.Equal(3, await evidence.MaterialIssues.CountAsync());
        Assert.Equal(3, await evidence.StockPostingBatches.CountAsync(x => x.PostingKind == "MATERIAL_ISSUE"));
        Assert.Equal(3, await evidence.AuditLogs.CountAsync(x => x.Action == "MaterialIssue.Issue"));
        Assert.All(await evidence.AuditLogs.Where(x => x.Action == "MaterialIssue.Issue").ToListAsync(), x =>
        {
            Assert.NotNull(x.ResolvedRoleAssignmentId); Assert.Equal(Rev869ARoleCodes.StoresExecutive, x.ActorRoleCode);
        });
        var returnBatch = await evidence.StockPostingBatches.SingleAsync(x => x.MaterialReturnId == materialReturn.Id);
        var returnMovements = await evidence.StockMovements.Where(x => x.StockPostingBatchId == returnBatch.Id)
            .Include(x => x.CustodyAssignment)!.ThenInclude(x => x!.CustodyAccount).ToListAsync();
        Assert.Equal(2, returnMovements.Count); Assert.Equal(0m, returnMovements.Sum(x => x.QuantityIn - x.QuantityOut));
        Assert.DoesNotContain(returnMovements, x => x.MovementType == "CONSUMPTION_OUT");
        Assert.Single(returnMovements.Select(x => x.OwnershipAccountId).Distinct());
        Assert.Single(returnMovements.Select(x => x.InventoryProvenanceLayerId).Distinct());
        Assert.Single(returnMovements, x => x.MovementLeg == "RETURN_OUT" &&
            x.CustodyAssignment!.CustodyAccount!.CustodyType == "EMPLOYEE" && x.QuantityOut == .60m);
        Assert.Single(returnMovements, x => x.MovementLeg == "RETURN_IN" &&
            x.CustodyAssignment!.CustodyAccount!.CustodyType == "WAREHOUSE" && x.QuantityIn == .60m);
        Assert.Equal("PARTIALLY_RETURNED", await evidence.MaterialIssues.Where(x => x.Id == issue.Id).Select(x => x.Status).SingleAsync());
        Assert.Equal(3, await evidence.MaterialReturns.CountAsync());
        Assert.Equal(6, await evidence.MaterialReturnHistories.CountAsync());
        Assert.Equal(3, await evidence.AuditLogs.CountAsync(x => x.Action == "MaterialReturn.Accept"));
        var outstanding = await Get<OutstandingEngineerCustodyView[]>(client,
            $"/api/v1/stores/material-issues/outstanding-custody?employeeId={engineerId}");
        Assert.Equal(2, outstanding.Length); Assert.All(outstanding, x => Assert.Equal(engineerId, x.EmployeeId));
        Assert.Equal(.35m, outstanding.Single(x => x.MaterialIssueId == issue.Id).QuantityBase);
        Assert.Equal(.02m, outstanding.Single(x => x.MaterialIssueId == consumable.Id).QuantityBase);
    }

    private static async Task CreateIssueAndReturnSerialized(HttpClient client,
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user, GoodsReceiptResult grn,
        Guid uomId, Guid departmentId, Guid engineerId, Guid purchaseId, Guid productionId, Guid storesId)
    {
        var receivedLine = grn.Lines.Single();
        var serial = Assert.Single(receivedLine.Serials);
        Assert.NotNull(serial.InventorySerialId);
        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.StoresExecutive);
        var request = await Post<MaterialIssueRequestView>(client, "/api/v1/stores/material-issue-requests",
            new CreateMaterialIssueRequest("FACTORY_ASSEMBLY", "CONSUMABLE_OFFICE", "DEPARTMENT",
                null, null, null, departmentId, "Serialized custody witness", departmentId,
                new DateOnly(2026, 9, 8),
                [new MaterialIssueRequestLineInput(receivedLine.ItemId, uomId, 1m, null, null)],
                "mir-serialized-create"));
        request = await Post<MaterialIssueRequestView>(client,
            $"/api/v1/stores/material-issue-requests/{request.Id}/submit",
            new MaterialIssueTransitionRequest(request.Version, "Serialized issue submitted", "mir-serialized-submit"));
        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        request = await Post<MaterialIssueRequestView>(client,
            $"/api/v1/stores/material-issue-requests/{request.Id}/approve",
            new MaterialIssueTransitionRequest(request.Version, "Serialized custody approved", "mir-serialized-approve"));
        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        var issue = await Post<MaterialIssueView>(client,
            $"/api/v1/stores/material-issues/from-request/{request.Id}",
            new CreateMaterialIssue("mir-serialized-issue", engineerId, DateTimeOffset.UtcNow,
                [new MaterialIssueScan(request.Lines.Single().Id, serial.StoredSerialNumber,
                    serial.InventorySerialId, 1m)]));

        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        await AssertPostStatus(client, $"/api/v1/stores/material-returns/from-issue/{issue.Id}",
            new CreateMaterialReturn(DateTimeOffset.UtcNow,
                [new MaterialReturnLineInput(issue.Lines.Single().Id, "SERIAL-NEVER-ISSUED", 1m, 0, 0)],
                "material-return-serial-not-issued"), HttpStatusCode.BadRequest);
        var materialReturn = await Post<MaterialReturnView>(client,
            $"/api/v1/stores/material-returns/from-issue/{issue.Id}",
            new CreateMaterialReturn(DateTimeOffset.UtcNow,
                [new MaterialReturnLineInput(issue.Lines.Single().Id, serial.StoredSerialNumber, 1m, 0, 0)],
                "material-return-serial-create"));
        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        materialReturn = await Post<MaterialReturnView>(client,
            $"/api/v1/stores/material-returns/{materialReturn.Id}/accept",
            new AcceptMaterialReturn(materialReturn.Version, DateTimeOffset.UtcNow,
                "Exact issued serial returned to Stores", "material-return-serial-accept"));

        await using var evidence = new NexaErpDbContext(options);
        Assert.Equal("RETURNED", await evidence.MaterialIssues.Where(x => x.Id == issue.Id)
            .Select(x => x.Status).SingleAsync());
        var movements = await evidence.StockMovements
            .Where(x => x.StockPostingBatchId == materialReturn.StockPostingBatchId).ToListAsync();
        Assert.Equal(2, movements.Count);
        Assert.All(movements, x => Assert.Equal(serial.InventorySerialId, x.InventorySerialId));
        Assert.DoesNotContain(movements, x => x.MovementType == "CONSUMPTION_OUT");
    }
    private static async Task<MaterialIssueView> CreateAndIssueConsumable(HttpClient client, TaxWorkflowUser user,
        Guid itemId, Guid uomId, string itemCode, Guid departmentId, Guid engineerId,
        Guid purchaseId, Guid productionId, Guid storesId)
    {
        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.StoresExecutive);
        var mir = await Post<MaterialIssueRequestView>(client, "/api/v1/stores/material-issue-requests",
            new CreateMaterialIssueRequest("FACTORY_ASSEMBLY", "CONSUMABLE_OFFICE", "DEPARTMENT",
                null, null, null, departmentId, "Factory consumable custody", departmentId,
                new DateOnly(2026, 9, 8), [new MaterialIssueRequestLineInput(itemId, uomId, .05m, null, null)],
                "mir-consumable-create"));
        mir = await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{mir.Id}/submit",
            new MaterialIssueTransitionRequest(mir.Version, "Cutting wheel custody", "mir-consumable-submit"));
        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        mir = await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{mir.Id}/approve",
            new MaterialIssueTransitionRequest(mir.Version, "Consumable custody approved", "mir-consumable-approve"));
        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        return await Post<MaterialIssueView>(client, $"/api/v1/stores/material-issues/from-request/{mir.Id}",
            new CreateMaterialIssue("mir-consumable-issue", engineerId, DateTimeOffset.UtcNow,
                [new MaterialIssueScan(mir.Lines.Single().Id, itemCode, null, .05m)]));
    }

    private static async Task AssertVendorBillRuntimeTableDmlRefused(string connectionString)
    {
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var sql in new[]
        {
            "SELECT count(*) FROM advance.vendor_bills",
            "SELECT count(*) FROM advance.fifo_inventory_cost_layers",
            "DELETE FROM advance.vendor_bills WHERE false",
            "DELETE FROM advance.fifo_inventory_cost_layers WHERE false"
        })
        {
            await using var command = new Npgsql.NpgsqlCommand(sql, connection);
            var error = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal(Npgsql.PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
        }
    }

    private static async Task RunVendorBillWitness(HttpClient client,
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user,
        IReadOnlyList<GoodsReceiptResult> grns, Guid accountsManagerId, Guid accountsSupportId)
    {
        var expected = new List<(decimal UnitRate, decimal Payable)>();
        foreach (var grn in grns)
            expected.Add(await Query(options, db => db.PurchaseOrderLines.Where(x =>
                x.Id == grn.Lines.Single().PurchaseOrderLineId)
                .Select(x => new ValueTuple<decimal, decimal>(x.UnitRate, x.TotalPayableValue)).SingleAsync()));

        user.Set(accountsManagerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        await AssertPostStatusContains(client,
            $"/api/v1/accounts/vendor-bills/from-grn/{grns[1].Id}",
            new CreateVendorBillRequest(grns[1].VendorBillNumber, grns[1].VendorBillDate,
                [new(grns[1].Lines.Single().Id, 2m, expected[1].UnitRate, expected[1].Payable)],
                "vendor-bill-quantity-mismatch"), HttpStatusCode.Conflict,
            "GRN quantity", "bill quantity");

        var mismatchRate = expected[0].UnitRate + 1m;
        var mismatchValue = expected[0].Payable + 1m;
        var mismatched = await Post<VendorBillView>(client,
            $"/api/v1/accounts/vendor-bills/from-grn/{grns[0].Id}",
            new CreateVendorBillRequest(grns[0].VendorBillNumber, grns[0].VendorBillDate,
                [new(grns[0].Lines.Single().Id, 1m, mismatchRate, mismatchValue)],
                "vendor-bill-price-mismatch"));
        Assert.Equal("PRICE_MISMATCH", mismatched.MatchStatus);

        user.Set(accountsSupportId, "SESS-41", "ACCOUNTS_ASSISTANT");
        Assert.Equal("SUPPORT", Assert.Single(user.EffectiveRoleAssignments).AssignmentType);
        await AssertPostStatus(client, $"/api/v1/accounts/vendor-bills/{mismatched.Id}/accept",
            new VendorBillDecisionRequest(mismatched.Version, "Support cannot accept", "vendor-bill-support-accept"),
            HttpStatusCode.Forbidden);
        await AssertPostStatus(client, $"/api/v1/accounts/vendor-bills/{mismatched.Id}/reject",
            new VendorBillDecisionRequest(mismatched.Version, "Support cannot reject", "vendor-bill-support-reject"),
            HttpStatusCode.Forbidden);

        user.Set(accountsManagerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        await AssertPostStatusContains(client, $"/api/v1/accounts/vendor-bills/{mismatched.Id}/accept",
            new VendorBillDecisionRequest(mismatched.Version, "Mismatch cannot be accepted", "vendor-bill-mismatch-accept"),
            HttpStatusCode.Conflict, "PO unit rate", "bill unit rate",
            expected[0].UnitRate.ToString(System.Globalization.CultureInfo.InvariantCulture),
            mismatchRate.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var rejected = await Post<VendorBillView>(client,
            $"/api/v1/accounts/vendor-bills/{mismatched.Id}/reject",
            new VendorBillDecisionRequest(mismatched.Version, "Rejected: PO must be revised", "vendor-bill-reject"));
        Assert.Equal("REJECTED", rejected.Status);

        for (var index = 1; index < grns.Count; index++)
        {
            var grn = grns[index];
            var bill = await Post<VendorBillView>(client,
                $"/api/v1/accounts/vendor-bills/from-grn/{grn.Id}",
                new CreateVendorBillRequest(grn.VendorBillNumber, grn.VendorBillDate,
                    [new(grn.Lines.Single().Id, grn.Lines.Single().ReceivedQuantity,
                        expected[index].UnitRate, expected[index].Payable)], $"vendor-bill-create-{index}"));
            Assert.Equal("MATCHED", bill.MatchStatus);
            var decision = new VendorBillDecisionRequest(bill.Version,
                "Three-way match accepted", $"vendor-bill-accept-{index}");
            var accepted = await Post<VendorBillView>(client,
                $"/api/v1/accounts/vendor-bills/{bill.Id}/accept", decision);
            Assert.Equal("ACCEPTED", accepted.Status);
            var replay = await Post<VendorBillView>(client,
                $"/api/v1/accounts/vendor-bills/{bill.Id}/accept", decision);
            Assert.True(replay.Replayed);            if (index == 1)
            {
                var reversalRequest = new VendorBillDecisionRequest(accepted.Version,
                    "Accepted bill corrected by governed reversal", "vendor-bill-reverse-1");
                var reversed = await Post<VendorBillView>(client,
                    $"/api/v1/accounts/vendor-bills/{accepted.Id}/reverse", reversalRequest);
                Assert.Equal("REVERSED", reversed.Status);
                Assert.NotNull(reversed.ReversedAt);
                var reversalReplay = await Post<VendorBillView>(client,
                    $"/api/v1/accounts/vendor-bills/{accepted.Id}/reverse", reversalRequest);
                Assert.True(reversalReplay.Replayed);

                var replacement = await Post<VendorBillView>(client,
                    $"/api/v1/accounts/vendor-bills/from-grn/{grn.Id}",
                    new CreateVendorBillRequest(grn.VendorBillNumber, grn.VendorBillDate,
                        [new(grn.Lines.Single().Id, grn.Lines.Single().ReceivedQuantity,
                            expected[index].UnitRate, expected[index].Payable)], "vendor-bill-reentry-1"));
                replacement = await Post<VendorBillView>(client,
                    $"/api/v1/accounts/vendor-bills/{replacement.Id}/accept",
                    new VendorBillDecisionRequest(replacement.Version,
                        "Corrected bill re-entered after reversal", "vendor-bill-reentry-accept-1"));
                Assert.Equal("ACCEPTED", replacement.Status);
            }
        }

        await using var evidence = new NexaErpDbContext(options);
        Assert.Equal(4, await evidence.VendorBills.CountAsync());
        Assert.Equal(2, await evidence.VendorBills.CountAsync(x => x.Status == "ACCEPTED"));
        Assert.Equal(3, await evidence.VendorBillCostAllocations.CountAsync());
        Assert.Equal(3, await evidence.VendorBillHistories.CountAsync(x => x.Action == "ACCEPTED"));
        Assert.Single(await evidence.VendorBillHistories.Where(x => x.Action == "REVERSED").ToListAsync());
    }

    private static async Task AssertPostStatusContains(HttpClient client, string path, object body,
        HttpStatusCode expected, params string[] fragments)
    {
        using var response = await client.PostAsJsonAsync(path, body);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Equal(expected, response.StatusCode);
        Assert.All(fragments, fragment => Assert.Contains(fragment, payload, StringComparison.OrdinalIgnoreCase));
    }
    private static async Task AssertPostStatus(HttpClient client, string path, object body, HttpStatusCode expected)
    {
        using var response = await client.PostAsJsonAsync(path, body);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected,
            $"{path} returned {(int)response.StatusCode} {response.StatusCode}, expected {(int)expected}: {payload}");
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

    private sealed class OrdinaryPrincipalEnvironment : IDisposable
    {
        private readonly Dictionary<string, string?> prior = new(StringComparer.Ordinal);

        public OrdinaryPrincipalEnvironment(string installerConnection, string runtimePassword)
        {
            Set("ConnectionStrings__NexaErpInstaller", installerConnection);
            Set("NexaErp__ExpectedDatabase", "advance_parser");
            Set("NEXAERP_MIGRATION_PASSWORD", "ordinary-migration-test-123456789");
            Set("NEXAERP_BOOTSTRAP_PASSWORD", "ordinary-bootstrap-test-123456789");
            Set("NEXAERP_RUNTIME_PASSWORD", runtimePassword);
        }

        private void Set(string name, string value)
        {
            prior[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            foreach (var value in prior) Environment.SetEnvironmentVariable(value.Key, value.Value);
        }
    }

    private sealed class PurchaseFlowHost(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public static async Task<PurchaseFlowHost> StartAsync(
            string connectionString,
            TaxWorkflowUser user,
            bool useRealPagePermissions = false)
        {
            var port = FreePurchaseFlowPort();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
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
            if (!useRealPagePermissions)
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
            app.MapEstimatedBomEndpoints();
            app.MapProductionEngineeringEndpoints();
            app.MapJobOrderEndpoints();
            app.MapMaterialIssueEndpoints();
            app.MapVendorBillEndpoints();
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
