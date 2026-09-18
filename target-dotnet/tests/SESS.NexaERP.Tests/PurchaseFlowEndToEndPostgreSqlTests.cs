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
using SESS.NexaERP.Infrastructure.Stores;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task RotatePurchaseWitnessSubject(
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user, Guid employeeId, string suffix)
    {
        // Simulate an identity-provider handover in this isolated fixture. The governed
        // runtime create/revoke operations have their own transaction/audit witness.
        await using var db = new NexaErpDbContext(options);
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var previous = await db.EmployeeIdentityMappings.SingleAsync(row =>
            row.CompanyId == companyId && row.EmployeeId == employeeId && row.IsActive);
        previous.IsActive = false;
        previous.EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow);
        previous.Version++;
        previous.UpdatedAt = DateTimeOffset.UtcNow;
        previous.UpdatedBy = "IDENTITY_HANDOVER_WITNESS";
        await db.SaveChangesAsync();
        var subject = "purchase-" + suffix;
        db.EmployeeIdentityMappings.Add(Mapping(companyId, employeeId, subject));
        await db.SaveChangesAsync();
        user.RotateSubject(employeeId, subject);
        Assert.Equal(1, await db.EmployeeIdentityMappings.CountAsync(row =>
            row.CompanyId == companyId && row.EmployeeId == employeeId && row.IsActive));
    }

    [Fact]
    public Task CompletePurchaseFlowRunsAgainstDisposablePostgreSqlInAllThreeApprovalBands() =>
        RunCompletePurchaseFlow(multiSerialQcWitness: true);

    private async Task RunCompletePurchaseFlow(
        Func<ReturnFitmentRaceContext, Task<MaterialReturnView>>? returnRace = null, bool serializedRace = false,
        Func<GrnFinalizeRaceContext, Task<GoodsReceiptResult>>? grnRace = null,
        Func<MirApprovalRaceContext, Task<MaterialIssueRequestView>>? mirRace = null,
        Func<PrApprovalRaceContext, Task<PurchaseRequisitionDetail>>? prRace = null,
        Func<VendorBillRaceContext, Task<VendorBillView>>? billRace = null,
        Func<SerialIssueRaceContext, Task<MaterialIssueView>>? issueRace = null,
        Func<PaymentRaceContext, Task<PaymentRaceResult>>? paymentRace = null,
        Func<QcCorrectionContext, Task<QcInspectionResult>>? qcCorrection = null,
        Func<QcConcessionRaceContext, Task<InventoryConcessionResult>>? qcRace = null,
        bool concessionHistoryWitness = false,
        Func<DirectFifoRaceContext, Task>? fifoRace = null, Func<MixedRunContext, Task>? mixedRun = null,
        bool durableDatabase = false,
        Func<PurchaseWorkloadWitnessContext, Task>? workload = null, int additionalDraftReceipts = 0,
        Func<PurchaseObligationWitnessContext, Task>? obligations = null,
        Func<PurchaseOpenOrderWitnessContext, Task>? openOrders = null, bool overdueQuoteDates = false,
        Func<OpenOrderAmendmentWitnessContext, Task<Rev869BDocumentResult>>? openOrderAmendment = null,
        int additionalIssuedPoVersions = 0,
        Func<StoresWorkloadWitnessContext,Task>? storesWorkload = null,
        Func<StoresQcStockWitnessContext,Task>? qcStock = null,
        Func<FifoPartialFitmentReturnContext,Task>? fifoPartialReturn = null, bool historicalFifoUpgrade = false,
        Func<SupplierInvoiceWitnessContext,Task>? supplierInvoices = null, Func<MachineDeliveryWitnessContext,Task>? machineDelivery = null,
        Func<DbContextOptions<NexaErpDbContext>,Task>? intercompanySetup = null,
        Func<SupplierInvoiceWitnessContext,Task>? intercompanyPurchase = null, bool multiSerialQcWitness = false)
    {
        var bootstrapOptions = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var model = new NexaErpDbContext(bootstrapOptions);
        if (concessionHistoryWitness) Assert.False(model.Database.HasPendingModelChanges());
        var migrator = model.GetService<IMigrator>();
        var latest = model.Database.GetMigrations().Last();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin(), durableDatabase);
        if (returnRace is not null || grnRace is not null || mirRace is not null || prRace is not null || billRace is not null || issueRace is not null || paymentRace is not null || qcCorrection is not null || qcRace is not null || fifoRace is not null || mixedRun is not null || obligations is not null)
            server.Execute("concurrency-log-settings.sql",
                "ALTER SYSTEM SET log_error_verbosity='verbose'; SELECT pg_reload_conf();");
        var initialMigration=historicalFifoUpgrade?model.Database.GetMigrations()
            .TakeWhile(x=>x!="20260914080000_FifoReturnRestorations").Last():latest;
        server.Execute("purchase-flow-business-up.sql", migrator.GenerateScript("0", initialMigration));
        if (historicalFifoUpgrade)
        {
            // This synthetic FIFO predecessor fixture uses the current application.
            // Install only its new tax-rule columns early; FIFO functions/evidence
            // remain at the historical predecessor until the actual upgrade below.
            var migrations = model.Database.GetMigrations().ToArray();
            var credit = Array.IndexOf(migrations, "20260918085900_GovernedTaxInputCreditEligibility");
            Assert.True(credit > 0);
            server.Execute("historical-fifo-current-tax-contract.sql",
                migrator.GenerateScript(migrations[credit - 1], migrations[credit]));
        }
        if (returnRace is not null)
        {
            var migrations = model.Database.GetMigrations().ToArray();
            var lockOrder = Array.IndexOf(migrations, "20260913020000_FitmentIssueHeaderLockOrder");
            server.Execute("fitment-lock-order-down.sql",
                migrator.GenerateScript(migrations[lockOrder], migrations[lockOrder - 1]));
            server.Execute("fitment-lock-order-reapply.sql",
                migrator.GenerateScript(migrations[lockOrder - 1], migrations[lockOrder]));
        }
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
        Guid secondReceiptOperatorId;
        Guid departmentId;
        Guid warehouseId;
        Guid rackBinId;
        Guid categoryId;
        Guid vendor1Id;
        Guid vendor2Id;
        await using (var seed = new NexaErpDbContext(options))
        {
            var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
            departmentId = await seed.Departments.Where(x => x.Code == "IT").Select(x => x.Id).SingleAsync();
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
            secondReceiptOperatorId = await Employee(seed, "SESS-16");
            var identities = new[]
            {
                (creatorId, "SESS-12"), (managerId, "SESS-14"), (tdId, "SESS-01"),
                (mdId, "SESS-02"), (verifierId, "SESS-05"), (purchaseId, "SESS-15"), (storesId, "SESS-35"),
                (qcId, "SESS-33"), (productionId, "SESS-25"), (accountsSupportId, "SESS-41"),
                (secondReceiptOperatorId, "SESS-16")
            };
            var identityEmployeeIds = identities.Select(x => x.Item1).ToArray();
            await seed.Employees.Where(x => identityEmployeeIds.Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(e => e.LoginEnabled, true));
            seed.EmployeeIdentityMappings.AddRange(identities.Select(x => Mapping(companyId, x.Item1, x.Item2)));
            seed.EmployeeOperationalScopes.AddRange(identities
                .Where(x => x.Item1 != managerId && x.Item1 != tdId && x.Item1 != mdId && x.Item1 != qcId)
                .Select(x => new EmployeeOperationalScope
            {
                CompanyId = companyId, OrganizationId = "SESS_PVT_LTD", EmployeeId = x.Item1,
                DepartmentId = departmentId, WarehouseId = warehouseId, OwnRecordsOnly = false,
                AllowsPrivilegedCrossScope = false, EffectiveFrom = new DateOnly(2026, 1, 1),
                IsActive = true, Remarks = "Disposable full Purchase flow", CreatedBy = "PURCHASE_FLOW_TEST"
            }));
            // Explicit disposable TD reporting scope for the QC stock witness.
            if(qcStock is not null)seed.EmployeeOperationalScopes.Add(new EmployeeOperationalScope
            {
                CompanyId=companyId,OrganizationId="SESS_PVT_LTD",EmployeeId=tdId,
                DepartmentId=departmentId,WarehouseId=null,OwnRecordsOnly=false,
                AllowsPrivilegedCrossScope=false,EffectiveFrom=new DateOnly(2026,1,1),IsActive=true,
                Remarks="Disposable QC stock reporting scope",CreatedBy="QC_STOCK_FIXTURE"
            });
            // Explicit disposable department reporting grant: MIRs have no source warehouse yet.
            // This does not change the production scope rule or the ordinary workflow fixture.
            if(storesWorkload is not null)foreach(var reportingDepartment in await seed.Departments
                .Where(x=>x.Code=="IT"||x.Code=="PRODUCTION").Select(x=>x.Id).ToListAsync())
                seed.EmployeeOperationalScopes.Add(new EmployeeOperationalScope
            {
                CompanyId=companyId,OrganizationId="SESS_PVT_LTD",EmployeeId=accountsSupportId,
                DepartmentId=reportingDepartment,WarehouseId=null,OwnRecordsOnly=false,
                AllowsPrivilegedCrossScope=false,EffectiveFrom=new DateOnly(2026,1,1),IsActive=true,
                Remarks="Disposable Stores Manager department reporting scope",CreatedBy="STORES_WORKLOAD_FIXTURE"
            });
            await seed.SaveChangesAsync();
        }

        if (intercompanySetup is not null) await intercompanySetup(options);
        const string runtimePassword = "ordinary-purchase-runtime-123456789";
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, runtimePassword);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        if (concessionHistoryWitness)
            await AssertConcessionHistoryMigration(options, migrator, (name, sql) => server.Execute(name, sql));
        if (paymentRace is not null)
        {
            var migrations = model.Database.GetMigrations().ToArray();
            const string paymentLockOrder = "20260913030000_VendorPaymentBillLockOrder";
            var index = Array.IndexOf(migrations, paymentLockOrder);
            Assert.True(index > 0);
            var beforePermissions = await PaymentFunctionMetadata(options);
            server.Execute("payment-lock-order-down.sql", migrator.GenerateScript(latest, migrations[index - 1]));
            var downPermissions = await PaymentFunctionMetadata(options);
            server.Execute("payment-lock-order-reapply.sql", migrator.GenerateScript(migrations[index - 1], latest));
            var afterPermissions = await PaymentFunctionMetadata(options);
            Assert.Equal(beforePermissions, downPermissions);
            Assert.Equal(beforePermissions, afterPermissions);
            var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "payment-function-permissions.json"),
                JsonSerializer.Serialize(new { Before = beforePermissions, Down = downPermissions, Reapplied = afterPermissions }));
        }
        if (qcRace is not null)
        {
            var migrations = model.Database.GetMigrations().ToArray();
            const string stockBalanceGuard = "20260913040000_StockConditionBalanceGuard";
            var index = Array.IndexOf(migrations, stockBalanceGuard);
            Assert.True(index > 0);
            var beforePermissions = await QcPostingFunctionMetadata(options);
            server.Execute("stock-balance-down.sql", migrator.GenerateScript(stockBalanceGuard, migrations[index - 1]));
            var downPermissions = await QcPostingFunctionMetadata(options);
            server.Execute("stock-balance-reapply.sql", migrator.GenerateScript(migrations[index - 1], stockBalanceGuard));
            var afterPermissions = await QcPostingFunctionMetadata(options);
            Assert.Equal(beforePermissions, downPermissions);
            Assert.Equal(beforePermissions, afterPermissions);
            var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "qc-posting-function-permissions.json"),
                JsonSerializer.Serialize(new { Before = beforePermissions, Down = downPermissions, Reapplied = afterPermissions }));
        }
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

        await AssertFitmentRuntimeTableDmlRefused(runtimeConnection);
        await AssertFatRuntimeTableDmlRefused(runtimeConnection);
        await using var adminHost = await PurchaseFlowHost.StartAsync(server.ConnectionString, user);
        await using var runtimeHost = await PurchaseFlowHost.StartAsync(runtimeConnection, user);
        await using var approvalHost = await PurchaseFlowHost.StartAsync(server.ConnectionString, user, useRealPagePermissions: true);
        await using var scopeHost = await PurchaseFlowHost.StartAsync(runtimeConnection, user, useRealOperationalScopes: true);
        await using var denialHost = await PurchaseFlowHost.StartAsync(runtimeConnection, user, denyLifecycleScope: true);
        var adminClient = adminHost.Client;
        var client = runtimeHost.Client;
        var approvalClient = approvalHost.Client;
        var qcReachability = new QcReachabilityWitness(qcId, tdId,
            roleAssignments.Values.Select(x => x.AssignmentId).ToHashSet());
        await using var qcHost = await PurchaseFlowHost.StartAsync(runtimeConnection, user,
            useRealPagePermissions: true, useRealOperationalScopes: true, captureRequest: qcReachability.Begin);
        var scopeClient = scopeHost.Client;
        var denialClient = denialHost.Client;

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
                if (vendorCode == "TRIAL-VEN-001")
                    await RotatePurchaseWitnessSubject(options, user, purchaseId, "after-qualification");
                user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
                if (vendorCode == "TRIAL-VEN-001")
                {
                    using var deniedRequest = new HttpRequestMessage(HttpMethod.Post,
                        $"/api/v1/rev869a/configuration/vendor-qualifications/{qualification.Id}/verify")
                    {
                        Content = JsonContent.Create(new ChangeVendorQualificationLifecycleRequest(
                            qualification.Version, "Lifecycle scope refusal must leave durable evidence"))
                    };
                    deniedRequest.Headers.Add("Idempotency-Key", "fixture-qualification-lifecycle-denial");
                    using var deniedResponse = await denialClient.SendAsync(deniedRequest);
                    Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
                    var denialAudits = await Query(options, db => db.AuditLogs.AsNoTracking()
                        .Where(x => x.EntityId == qualification.Id.ToString())
                        .Select(x => new { x.Action, x.UserLoginId, x.Result })
                        .ToListAsync());
                    Assert.Contains(denialAudits, x => x.Action == "Denied" &&
                        x.UserLoginId == "SESS-01" && x.Result == "Failure");
                }

                await PostNoResult(scopeClient, $"/api/v1/rev869a/configuration/vendor-qualifications/{qualification.Id}/verify",
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
                    verifierId, purchaseId, storesId, qcId, vendor1Id, vendor2Id,
                    grnRace is not null && band.Code == "LOW"
                        ? draft => grnRace(new(options, runtimeConnection, draft, storesId, secondReceiptOperatorId,
                            "LOW-grn-finalize", server.ReadDiagnosticLog))
                        : null,
                    prRace is not null && band.Code == "TD"
                        ? draft => prRace(new(options, runtimeConnection, draft, managerId, tdId,
                            "TD-pr-approve-1", server.ReadDiagnosticLog))
                        : null,
                    workload is null ? null : (stage, id) => workload(new(options, runtimeConnection, stage, id, band.Code)),
                    openOrders is null ? null : (stage,id) => openOrders(new(options,runtimeConnection,stage,id,band.Code)),
                    overdueQuoteDates,
                    openOrderAmendment is not null && band.Code=="LOW"
                        ? issued => openOrderAmendment(new(options,runtimeConnection,issued,client,user,purchaseId)) : null,
                    storesWorkload is null ? null : (stage,id)=>storesWorkload(new(options,runtimeConnection,stage,id,band.Code)),
                    qcStock is null ? null : (stage,id)=>qcStock(new(options,runtimeConnection,stage,id,band.Code)),
                    supplierInvoices is null && intercompanyPurchase is null ? null : async (stage,id) =>
                    {
                        var context = new SupplierInvoiceWitnessContext(client,options,user,stage,id,band.Code,managerId,purchaseId,storesId,runtimeConnection);
                        if (supplierInvoices is not null) await supplierInvoices(context);
                        if (intercompanyPurchase is not null) await intercompanyPurchase(context);
                    }));
            var runtimeOptions = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(runtimeConnection).Options;
            await using (var notificationDb = new NexaErpDbContext(runtimeOptions))
            {
                var processor = new EfNotificationDueEventProcessor(notificationDb);
                Assert.Equal(1, await processor.RefreshAsync(DateTimeOffset.UtcNow, CancellationToken.None));
                Assert.Equal(0, await processor.RefreshAsync(DateTimeOffset.UtcNow, CancellationToken.None));
            }
            user.Set(qcId, "SESS-33", Rev869ARoleCodes.QcManager);
            var qcNotifications = await Get<PagedResponse<InAppNotificationView>>(client,
                "/api/v1/notifications?unreadOnly=true");
            Assert.Equal("QC_AGEING_OVERDUE", Assert.Single(qcNotifications.Items).EventType);
            for(var i=0;i<grns.Count;i++)await RunQcWitness(qcHost.Client,options,user,bands[i],grns[i],qcId,tdId,
                qcCorrection is null ? null : (original, command) => qcCorrection(new(options, runtimeConnection,
                    original, command, qcId, server.ReadDiagnosticLog)),
                qcRace is null ? null : (original, command, draft, available) => qcRace(new(options, runtimeConnection,
                    original, command, draft, available, qcId, tdId, server.ReadDiagnosticLog)),
                qcStock is null ? null : (stage,id)=>qcStock(new(options,runtimeConnection,stage,id,bands[i].Code)),
                historicalPolicyFixture: historicalFifoUpgrade);
            await using (var notificationDb = new NexaErpDbContext(runtimeOptions))
                Assert.Equal(1, await new EfNotificationDueEventProcessor(notificationDb)
                    .RefreshAsync(DateTimeOffset.UtcNow, CancellationToken.None));
            user.Set(qcId, "SESS-33", Rev869ARoleCodes.QcManager);
            Assert.Equal(0, (await Get<JsonElement>(client, "/api/v1/notifications/unread-count")).GetProperty("Count").GetInt32());
            user.Set(managerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
            var initialGrni = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,"/api/v1/reports/grni");
            Assert.Equal(3m,initialGrni.Totals.Sum(row => row.GetProperty("quantity").GetDecimal()));
            Assert.Contains(initialGrni.TimeZone,new[]{"Asia/Kolkata","Asia/Calcutta"});
            Assert.Equal(DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow,"Asia/Kolkata").DateTime),initialGrni.ToDate);

            var initialFifo=await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,"/api/v1/reports/fifo-valuation");
            Assert.Equal(3m,initialFifo.Totals.Sum(row=>row.GetProperty("quantity").GetDecimal()));
            // The committed GRN costing path posts the PO total payable per unit,
            // including its tax allocation; bare GRN UnitRate is not the layer value.
            Assert.Equal(128620.01m,initialFifo.Totals.Sum(row=>row.GetProperty("value").GetDecimal()));
            var pendingLandedBill = await RunVendorBillWitness(client, options, user, grns, managerId, accountsSupportId,
                billRace is null ? null : draft => billRace(new(options, runtimeConnection, draft,
                    managerId, "vendor-bill-accept-2", server.ReadDiagnosticLog)),
                paymentRace is null ? null : command => paymentRace(new(options, runtimeConnection,
                    command, managerId, server.ReadDiagnosticLog)),
                obligations is null ? null : stage => obligations(new(options,runtimeConnection,stage)));
            user.Set(managerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
            var pendingGrni = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,WitnessReportPath("/api/v1/reports/grni"));
            Assert.Equal(1m,pendingGrni.Totals.Sum(row => row.GetProperty("quantity").GetDecimal()));
            var partlyBilledFifo=await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
                WitnessReportPath("/api/v1/reports/fifo-valuation?mode=details"));
            Assert.Equal(3,partlyBilledFifo.Rows.Count);
            Assert.Equal(2,partlyBilledFifo.Rows.Count(row=>row.GetProperty("costBasis").GetString()=="BILL_LANDED"));
            Assert.Single(partlyBilledFifo.Rows,row=>row.GetProperty("costBasis").GetString()=="PO_PROVISIONAL_IDENTICAL");
            await AssertVendorBillRuntimeTableDmlRefused(runtimeConnection);
            await RunMaterialIssueWitness(client, options, runtimeConnection, user, grns[0], grns[2], verifierId,
                purchaseId, productionId, storesId, tdId, managerId, qcId, pendingLandedBill, returnRace, server.ReadDiagnosticLog, serializedRace,
                mirRace is null ? null : draft => mirRace(new(options, runtimeConnection, draft,
                    productionId, accountsSupportId, "mir-consumable-approve", server.ReadDiagnosticLog)),
                issueRace is null ? null : (draft, command) => issueRace(new(options, runtimeConnection,
                    draft, command, storesId, secondReceiptOperatorId, server.ReadDiagnosticLog, server.Restart)),
                storesWorkload is null ? null : (stage,id)=>storesWorkload(new(options,runtimeConnection,stage,id,"MIR")));

            user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
            if(historicalFifoUpgrade)
            {
                var originalHistory=await ReadOriginalFifoHistory(options);
                server.Execute("fifo-historical-return-upgrade.sql",migrator.GenerateScript(initialMigration,latest,MigrationsSqlGenerationOptions.Idempotent));
                Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
                await VerifyHistoricalFifoRestoration(options,originalHistory);
            }
            await AssertStockReportsFromPurchaseWitness(client, options, user, managerId, tdId);
            await using var verify = new NexaErpDbContext(options);
            Assert.False(await verify.RolePagePermissions.AnyAsync(x => !x.CanView && !x.HasFullControl &&
                (x.CanCreate || x.CanUpdate || x.CanSubmit || x.CanIssue || x.CanVerify || x.CanApprove ||
                 x.CanReject || x.CanRequestClarification || x.CanRequestRevision || x.CanResubmit ||
                 x.CanCancel || x.CanDeactivate)),
                "A fully migrated role-page grant permits a document action without permitting that actor to read the document.");

            Assert.Equal(3, await verify.PurchaseRequisitions.CountAsync());
            Assert.Equal(3, await verify.RequestForQuotations.CountAsync());
            Assert.Equal(6, await verify.RfqVendorInvitations.CountAsync());
            Assert.Equal(6, await verify.VendorQuotations.CountAsync());
            Assert.Equal(6, await verify.QuotationTechnicalVerifications.CountAsync());
            Assert.Equal(3, await verify.CommercialComparisons.CountAsync());
            Assert.Equal(3 + additionalIssuedPoVersions, await verify.PurchaseOrders.CountAsync());
            Assert.Equal(3 + additionalIssuedPoVersions, await verify.MaterialFollowUpHandoffs.CountAsync());
            Assert.Equal(3 + additionalDraftReceipts, await verify.GateEntries.CountAsync());
            Assert.Equal(3 + additionalDraftReceipts, await verify.GoodsReceipts.CountAsync());
            Assert.Equal(3, await verify.GoodsReceipts.CountAsync(x => x.Status == "FINALIZED"));
            Assert.Equal(additionalDraftReceipts, await verify.GoodsReceipts.CountAsync(x => x.Status == "DRAFT"));
            var qcDeadlineReceipts = await verify.GoodsReceipts
                .Select(x => new { x.ReceivedAt, x.FinalizedAt, x.QcCompletionDaysSnapshot, x.QcDueAt })
                .ToListAsync();
            Assert.All(qcDeadlineReceipts, x =>
                Assert.Equal(x.ReceivedAt.AddDays(x.QcCompletionDaysSnapshot), x.QcDueAt));
            var deliberatelyDelayedReceipt = Assert.Single(qcDeadlineReceipts, x =>
                x.FinalizedAt.HasValue && x.FinalizedAt.Value - x.ReceivedAt > TimeSpan.FromDays(2));
            Assert.NotEqual(deliberatelyDelayedReceipt.FinalizedAt!.Value.AddDays(
                deliberatelyDelayedReceipt.QcCompletionDaysSnapshot), deliberatelyDelayedReceipt.QcDueAt);
            Assert.Equal(3 + additionalDraftReceipts, await verify.GoodsReceiptLines.CountAsync());
            Assert.Equal(3 + additionalDraftReceipts, await verify.GoodsReceiptLineLotAllocations.CountAsync());
            Assert.Equal(3, await verify.FifoInventoryCostLayers.CountAsync());
            Assert.Single(await verify.JobOrders.Where(x => x.CustomerPurchaseOrderId != null).ToListAsync());
            Assert.Equal(8, await verify.JobOrderHistories.CountAsync());
            Assert.Equal(5, await verify.VendorBills.CountAsync());
            Assert.Equal(5, await verify.VendorBillLines.CountAsync());
            Assert.Equal(4, await verify.VendorBillCostAllocations.CountAsync());
            Assert.Equal(11, await verify.VendorBillHistories.CountAsync());
            var fifoLayers = await verify.FifoInventoryCostLayers.OrderBy(x => x.ReceivedAt).ThenBy(x => x.Id).ToListAsync();
            Assert.Equal(grns.Select(x => x.Lines.Single().Id),
                fifoLayers.Select(x => x.GoodsReceiptLineId!.Value));
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
            Assert.Equal(3 + additionalDraftReceipts, await verify.InventoryLots.CountAsync());
            Assert.Equal(1, await verify.GoodsReceiptLineSerials.CountAsync());
            Assert.Equal(1, await verify.InventorySerials.CountAsync());
            Assert.Equal(3, await verify.StockPostingBatches.CountAsync(x=>x.PostingKind=="GRN_CUSTODY"));
            Assert.Equal(0m, await verify.StockMovements.Where(x=>x.ConditionCode=="QC_HOLD").SumAsync(x=>x.QuantityIn-x.QuantityOut));
            Assert.Equal(2.65m, await verify.StockMovements.Where(x=>x.ConditionCode=="AVAILABLE").SumAsync(x=>x.QuantityIn-x.QuantityOut));
            Assert.Equal(.05m, await verify.StockMovements.Where(x=>x.ConditionCode=="PENDING_RETURNABLE_DC").SumAsync(x=>x.QuantityIn-x.QuantityOut));
            Assert.Equal(3,await verify.QcInspections.CountAsync());var qcRevisionCount=qcCorrection is null?3:4;Assert.Equal(qcRevisionCount,await verify.QcInspectionRevisions.CountAsync());Assert.Equal(qcRevisionCount,await verify.QcInspectionLotDispositions.CountAsync());Assert.Equal(qcRevisionCount,await verify.StockPostingBatches.CountAsync(x=>x.PostingKind=="QC_DISPOSITION"));Assert.Single(await verify.StockPostingBatches.Where(x=>x.PostingKind=="CONCESSION_ACCEPTANCE").ToListAsync());Assert.Single(await verify.InventoryConcessions.Where(x=>x.Status=="APPROVED").ToListAsync());
            Assert.Equal(6 + 2 * additionalDraftReceipts, await verify.StoresDocumentStatusHistories.CountAsync(x=>x.GateEntryId!=null));
            Assert.Equal(6 + additionalDraftReceipts, await verify.StoresDocumentStatusHistories.CountAsync(x=>x.GoodsReceiptId!=null));
            Assert.Equal(new[]{"ELE","FAB","FAS","MEC","PLC","REF"},await verify.ItemCategories.Where(x=>x.CreatedBy=="TRIAL_DATA").OrderBy(x=>x.Code).Select(x=>x.Code).ToArrayAsync());
            var qcLocations=await verify.WarehouseConditionLocations.Where(x=>x.CompanyId==Guid.Parse("70000000-0000-0000-0000-000000000001")&&x.ConditionCode=="QC_HOLD"&&x.CreatedBy=="TRIAL_DATA").ToListAsync();
            Assert.Equal(6,qcLocations.Count);
            Assert.Equal(6,qcLocations.Select(x=>x.RackBinId).Distinct().Count());
            var qcRackNames=await verify.RackBins.Where(x=>qcLocations.Select(location=>location.RackBinId).Contains(x.Id)).Select(x=>x.RackName).Distinct().ToListAsync();
            Assert.Equal("TRIAL QC Category Rack",Assert.Single(qcRackNames));
            var verifiedPoVersions=await verify.PurchaseOrders.AsNoTracking().ToListAsync();
            Assert.Equal(3,verifiedPoVersions.Count(x=>x.IsCurrentVersion));
            Assert.Equal(additionalIssuedPoVersions,verifiedPoVersions.Count(x=>!x.IsCurrentVersion));
            Assert.All(verifiedPoVersions.Where(x=>x.IsCurrentVersion),x=>Assert.Equal(Rev869BStatuses.Issued,x.Status));
            Assert.All(verifiedPoVersions.Where(x=>!x.IsCurrentVersion),x=>Assert.Equal(Rev869BStatuses.Superseded,x.Status));
            var commandCount=await verify.Database.SqlQueryRaw<int>(@"SELECT count(*)::integer AS ""Value"" FROM advance.command_requests").SingleAsync();
            var receiptCount=await verify.Database.SqlQueryRaw<int>(@"SELECT count(*)::integer AS ""Value"" FROM advance.command_receipts").SingleAsync();
            Assert.True(commandCount>0);Assert.Equal(commandCount,receiptCount);
            var operations=await verify.Database.SqlQueryRaw<string>(@"SELECT DISTINCT ""Operation"" AS ""Value"" FROM advance.command_requests").ToListAsync();
            Assert.All(new[]{"CreateVendorQualification","VerifyVendorQualification","ApproveVendorQualification",
                "CreateTaxGstSetting","ApproveTaxGstSetting","CreateRFQ","InviteVendor","SubmitQuotation",
                "TechnicalVerification","CreateComparison","RecommendComparison","ApproveComparison",
                "CreatePO","SubmitPO","ApprovePO","IssuePO","EstimatedBom.Create","EstimatedBom.Submit","EstimatedBom.ReturnToDraft",
                "EstimatedBom.Approve","ProductionBom.Create","ProductionBom.Submit","ProductionBom.ReturnToDraft","ProductionBom.Approve",
                "ProductionBom.Pin","EngineeringDocument.Create","EngineeringDocument.Submit","EngineeringDocument.ReturnToDraft","EngineeringDocument.Approve","MaterialIssueRequest.Create","MaterialIssueRequest.Submit",
                "MaterialReturn.Create","MaterialReturn.Accept","VendorBill.Create","VendorBill.Accept","VendorBill.Reject","VendorBill.Reverse",
                "JobOrder.Create","JobOrder.ReturnToDraft","JobOrder.ReviseDraft","JobOrder.Resubmit","JobOrder.AccountsConfirm","ComponentFitment.Confirm","ComponentFitment.Reverse",
                "MaterialIssueRequest.Approve","MaterialIssueRequest.DecideExcess","MaterialIssue.Issue"},
                operation=>Assert.Contains(operation,operations));
            var commandAudits=await verify.AuditLogs.Where(x=>x.Result=="Success"&&operations.Contains(x.Action)).ToListAsync();
            Assert.NotEmpty(commandAudits);
            Assert.All(commandAudits,x=>
            {
                Assert.False(string.IsNullOrWhiteSpace(x.ActorRoleCode));
                Assert.NotNull(x.ResolvedRoleAssignmentId);
            });
            if (concessionHistoryWitness) await AssertConcessionHistoryRollbackRefused(options, migrator);
            if (fifoRace is not null)
                await fifoRace(new(options, runtimeConnection, storesId, secondReceiptOperatorId, server.ReadDiagnosticLog));
            if (mixedRun is not null) await mixedRun(new(options, runtimeConnection, server.ReadDiagnosticLog));
            if (obligations is not null) await obligations(new(options,runtimeConnection,"FINAL"));
            await AssertTwoEngineerReport(client,options,user,departmentId,purchaseId,productionId,storesId,tdId);
            await AssertReportsSwitchBetweenAuthorizedCompanies(options,runtimeConnection,tdId,managerId);
            await AssertMachineDeliveryJobOrderReachability(new(options,runtimeConnection,user,storesId,tdId,managerId,productionId));
            if(machineDelivery is not null) await machineDelivery(new(options,runtimeConnection,user,storesId,tdId,managerId,productionId));
            if(fifoPartialReturn is not null)await fifoPartialReturn(new(client,options,user,productionId,storesId,managerId));
            if(supplierInvoices is not null)
            {
                // Earlier governed receipts leave stock. Demand must exceed the
                // remaining unreserved quantity to produce a real purchase handoff.
                var invoiceRequestedQuantity = await PurchaseWitnessRequestedQuantity(options, 1m);
                var extraBand=bands[1] with { Code="INVOICE" };
                Assert.InRange(invoiceRequestedQuantity * extraBand.PrAmount, 5000m, 100000m);
                var partialGrn=await RunPurchaseBand(adminClient,approvalClient,client,options,user,extraBand,
                    creatorId,managerId,tdId,mdId,verifierId,purchaseId,storesId,qcId,vendor1Id,vendor2Id,
                    supplierInvoices:(stage,id)=>supplierInvoices(new(client,options,user,stage,id,"INVOICE",managerId,purchaseId,storesId,runtimeConnection)),
                    receiptQuantity:.4m, requestedQuantity:invoiceRequestedQuantity);
                await supplierInvoices(new(client,options,user,"FINAL",partialGrn.PurchaseOrderId,"INVOICE",managerId,purchaseId,storesId,runtimeConnection));
            }
#if REPORT_VOLUME_WITNESS
            if (returnRace is null && grnRace is null && mirRace is null && prRace is null && billRace is null && issueRace is null && paymentRace is null && qcCorrection is null && qcRace is null && fifoRace is null && mixedRun is null && obligations is null && openOrders is null && openOrderAmendment is null && storesWorkload is null && qcStock is null && fifoPartialReturn is null && supplierInvoices is null) await RunReportVolumeWitness(options,runtimeConnection);
#endif
            if (multiSerialQcWitness)
            {
                var demand = await PurchaseWitnessRequestedQuantity(options, 2m);
                var multi = await RunPurchaseBand(adminClient, approvalClient, client, options, user,
                    new PurchaseFlowBand("QCMULTI", 100000.01m, 100000.01m, 2, managerId, mdId),
                    creatorId, managerId, tdId, mdId, verifierId, purchaseId, storesId, qcId, vendor1Id, vendor2Id,
                    receiptQuantity: 2m, requestedQuantity: demand, expectedHandoffQuantity: 2m);
                await ProveMultiSerialQcDiscrepancy(qcHost.Client, options, user, multi, qcId, tdId);
                await qcReachability.AssertCompleteAsync(qcHost.QcMutationRoutes);
                await ProveSeededUomItemEditing(qcHost.Client, options, user, tdId, categoryId);
                await ProveUnresolvedMasterImports(qcHost.Client, options, user, tdId, accountsSupportId);
                await ProveAccountsGrnReads(qcHost.Client, options, user, managerId, grns[0]);
            }
        }
        finally { }
    }


    private static async Task AssertTwoEngineerReport(HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, Guid departmentId, Guid purchaseId, Guid productionId, Guid storesId, Guid tdId)
    {
        user.Set(storesId,"SESS-35",Rev869ARoleCodes.StoresExecutive);
        var before=await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,WitnessReportPath("/api/v1/reports/engineer-custody"));
        var firstTotal=Assert.Single(before.Totals);
        await using var db=new NexaErpDbContext(options);
        var second=await db.Employees.SingleAsync(row=>row.EmployeeCode=="SESS-06");
        var item=await db.Items.SingleAsync(row=>row.ItemCode=="TRIAL-ITEM-001");
        await CreateAndIssueConsumable(client,user,item.Id,item.BaseUomId,item.ItemCode,departmentId,
            second.Id,purchaseId,productionId,storesId,"mir-second-engineer");
        var report=await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,WitnessReportPath("/api/v1/reports/engineer-custody"));
        Assert.Equal(2,report.Totals.Count);
        Assert.Equal(firstTotal.GetProperty("quantity").GetDecimal(),
            Assert.Single(report.Totals,row=>row.GetProperty("engineerCode").GetString()=="SESS-05").GetProperty("quantity").GetDecimal());
        Assert.Equal(.05m,Assert.Single(report.Totals,row=>row.GetProperty("engineerCode").GetString()=="SESS-06").GetProperty("quantity").GetDecimal());
        foreach(var total in report.Totals)
        {
            var selection=Uri.EscapeDataString(total.GetProperty("group").GetRawText());
            var detail=await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
                WitnessReportPath($"/api/v1/reports/engineer-custody?mode=details&selection={selection}&pageSize=1000"));
            Assert.All(detail.Rows,row=>Assert.Equal(total.GetProperty("engineerCode").GetString(),row.GetProperty("engineerCode").GetString()));
            Assert.Equal(total.GetProperty("quantity").GetDecimal(),detail.Rows.Sum(row=>row.GetProperty("quantity").GetDecimal()));
        }
        using var response=await client.GetAsync(WitnessReportPath("/api/v1/reports/engineer-custody/excel"));
        Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode); // Stores view does not imply export.
        var denied=(await response.Content.ReadFromJsonAsync<StandardErrorEnvelope>())!;
        Assert.Equal("REPORT_ACCESS_DENIED",denied.Code);
        Assert.Null(denied.AdministratorActionRequired);
        using(var invalid=await client.GetAsync(WitnessReportPath("/api/v1/reports/engineer-custody?pageSize=0")))
        {
            Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
            var error=(await invalid.Content.ReadFromJsonAsync<StandardErrorEnvelope>())!;
            Assert.Equal("REPORT_REQUEST_INVALID",error.Code);
            Assert.Null(error.AdministratorActionRequired);
        }
        user.Set(tdId,"SESS-01",Rev869ARoleCodes.TechnicalDirector);
        using var authorizedExport=await client.GetAsync(WitnessReportPath("/api/v1/reports/engineer-custody/excel"));
        authorizedExport.EnsureSuccessStatusCode();
        using var stream=new MemoryStream(await authorizedExport.Content.ReadAsByteArrayAsync());
        using var workbook=new ClosedXML.Excel.XLWorkbook(stream);
        var totals=workbook.Worksheet("Totals");
        Assert.Equal(3,totals.LastRowUsed()!.RowNumber());
        Assert.Equal(new[]{"SESS-05","SESS-06"},new[]{totals.Cell(2,1).GetString(),totals.Cell(3,1).GetString()}.Order().ToArray());
    }

    private static async Task AssertEngineerCustodyReport(HttpClient client, decimal expected)
    {
        var report = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,WitnessReportPath("/api/v1/reports/engineer-custody"));
        var total = Assert.Single(report.Totals);
        Assert.Equal("SESS-05",total.GetProperty("engineerCode").GetString());
        Assert.Equal(expected,total.GetProperty("quantity").GetDecimal());
        Assert.All(report.Rows,row => Assert.Equal("SESS-05",row.GetProperty("engineerCode").GetString()));
        var selection = Uri.EscapeDataString(total.GetProperty("group").GetRawText());
        var detail = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
            WitnessReportPath($"/api/v1/reports/engineer-custody?mode=details&selection={selection}&metric=quantity&pageSize=1000"));
        Assert.Equal(expected,detail.Rows.Sum(row => row.GetProperty("quantity").GetDecimal()));
    }

    private static async Task AssertStockReportsFromPurchaseWitness(HttpClient client, DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user, Guid accountsId, Guid tdId)
    {
        user.Set(accountsId, "SESS-14", Rev869ARoleCodes.AccountsManager);

        var restoredFifo=await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
            WitnessReportPath("/api/v1/reports/fifo-valuation"));
        var restoredTotal=Assert.Single(restoredFifo.Totals);
        Assert.Equal(2.63m,restoredTotal.GetProperty("quantity").GetDecimal());
        Assert.Equal(.63m*(4720m+12m)+5900m+118000.01m,restoredTotal.GetProperty("value").GetDecimal());
        var restoredSelection=Uri.EscapeDataString(restoredTotal.GetProperty("group").GetRawText());
        var restoredDetails=await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
            WitnessReportPath("/api/v1/reports/fifo-valuation?mode=details&metric=value&selection="+restoredSelection));
        Assert.Equal(3,restoredDetails.Rows.Count);
        Assert.Equal(2.63m,restoredDetails.Rows.Sum(row=>row.GetProperty("quantity").GetDecimal()));
        Assert.Equal(restoredTotal.GetProperty("value").GetDecimal(),
            restoredDetails.Rows.Sum(row=>row.GetProperty("value").GetDecimal()));
        using(var export=await client.GetAsync(WitnessReportPath("/api/v1/reports/fifo-valuation/excel")))
        {
            Assert.Equal(HttpStatusCode.OK,export.StatusCode);
            var bytes=await export.Content.ReadAsByteArrayAsync();
            using var book=new ClosedXML.Excel.XLWorkbook(new MemoryStream(bytes));
            Assert.Equal(2.63m,book.Worksheet("Totals").Cell(2,4).GetValue<decimal>());
            Assert.Equal(restoredTotal.GetProperty("value").GetDecimal(),book.Worksheet("Totals").Cell(2,5).GetValue<decimal>());
            Assert.True(book.Worksheet("Totals").Cell(2,5).HasHyperlink);
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item15");
            Directory.CreateDirectory(evidence);
            await File.WriteAllBytesAsync(Path.Combine(evidence,"fifo-after-accepted-returns.xlsx"),bytes);
            await File.WriteAllTextAsync(Path.Combine(evidence,"fifo-after-accepted-returns.json"),
                JsonSerializer.Serialize(new{Summary=restoredFifo,Details=restoredDetails},new JsonSerializerOptions{WriteIndented=true}));
        }
        Assert.Equal(0,await Query(options,db=>db.AuditLogs.CountAsync(row=>
            row.Module=="Reports"&&row.EntityId=="reports.fifo-valuation"&&row.Action=="Unavailable"&&row.Result=="Failure")));
        var grni = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,WitnessReportPath("/api/v1/reports/grni"));
        Assert.Empty(grni.Rows);
        var decisionInstants=await Query(options,db=>db.VendorBills.Where(b=>b.DecidedAt!=null)
            .Select(b=>b.DecidedAt!.Value).ToArrayAsync());
        var reportZone=TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        var beforeFirstDecision=decisionInstants
            .Select(instant=>SESS.NexaERP.Infrastructure.Reporting.ReportCalendarOptions.DateInZone(instant,reportZone))
            .Min().AddDays(-1);
        var beforeBilling=await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
            $"/api/v1/reports/vendor-purchases?fromDate={beforeFirstDecision.AddDays(-30):yyyy-MM-dd}&toDate={beforeFirstDecision:yyyy-MM-dd}");
        Assert.Empty(beforeBilling.Rows);

        var purchases = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
            WitnessReportPath("/api/v1/reports/vendor-purchases?fromDate=2026-01-01&pageSize=1000"));
        Assert.Equal(3m,purchases.Totals.Sum(row => row.GetProperty("quantity").GetDecimal()));
        Assert.Equal(12m,purchases.Totals.Sum(row => row.GetProperty("allocatedCharges").GetDecimal()));
        var events = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
            WitnessReportPath("/api/v1/reports/vendor-purchases?fromDate=2026-01-01&mode=details&pageSize=1000"));
        Assert.Equal(5,events.TotalSourceRows);
        Assert.Single(events.Rows,row => row.GetProperty("event").GetString() == "REVERSED");
        Assert.Equal(purchases.Totals.Sum(row => row.GetProperty("landedValue").GetDecimal()),
            events.Rows.Sum(row => row.GetProperty("landedValue").GetDecimal()));
        using (var exportResponse = await client.GetAsync(WitnessReportPath("/api/v1/reports/vendor-purchases/excel?fromDate=2026-01-01")))
        {
            Assert.True(exportResponse.IsSuccessStatusCode,await exportResponse.Content.ReadAsStringAsync());
            using var exportStream = new MemoryStream(await exportResponse.Content.ReadAsByteArrayAsync());
            using var exportBook = new ClosedXML.Excel.XLWorkbook(exportStream);
            Assert.Equal(6,exportBook.Worksheet("Details").LastRowUsed()!.RowNumber());
            Assert.Equal(3m,exportBook.Worksheet("Totals").Cell(2,3).GetValue<decimal>());
            Assert.Equal(12m,exportBook.Worksheet("Totals").Cell(2,5).GetValue<decimal>());
        }
        user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
        var register = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,WitnessReportPath("/api/v1/reports/purchase-register"));
        Assert.Equal(3,register.TotalRows);
        Assert.Equal(19,register.TotalSourceRows);
        foreach (var measure in new[] { "requested","ordered","received","billed" })
            Assert.Equal(3m,register.Totals.Sum(row => row.GetProperty(measure).GetDecimal()));
        foreach (var row in register.Rows)
            foreach (var measure in new[] { "requested","ordered","received","billed" })
                Assert.Equal(1m,row.GetProperty(measure).GetDecimal());
        var balance = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,"/api/v1/reports/stock-balance?pageSize=1000");
        Assert.Equal(2.7m,balance.Totals.Sum(row => row.GetProperty("quantity").GetDecimal()));
        Assert.NotEmpty(balance.Rows);
        foreach (var row in balance.Rows)
        {
            var selection = Uri.EscapeDataString(row.GetProperty("group").GetRawText());
            var detail = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
                WitnessReportPath($"/api/v1/reports/stock-balance?mode=details&metric=closing&selection={selection}&pageSize=1000"));
            Assert.Equal(row.GetProperty("quantity").GetDecimal(),detail.Rows.Sum(movement => movement.GetProperty("netQuantity").GetDecimal()));
            Assert.Equal(detail.TotalSourceRows,detail.Rows.Count);
        }
        var roll = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
            WitnessReportPath("/api/v1/reports/movement-roll-forward?fromDate=2026-01-01&pageSize=1000"));
        Assert.Equal(2.7m,roll.Totals.Sum(row => row.GetProperty("closing").GetDecimal()));
        foreach (var row in roll.Rows)
            Assert.Equal(row.GetProperty("closing").GetDecimal(),
                row.GetProperty("opening").GetDecimal()+row.GetProperty("receipts").GetDecimal()
                -row.GetProperty("issues").GetDecimal()+row.GetProperty("adjustments").GetDecimal());
        using var response = await client.GetAsync(WitnessReportPath("/api/v1/reports/movement-roll-forward/excel?fromDate=2026-01-01"));
        Assert.True(response.IsSuccessStatusCode,await response.Content.ReadAsStringAsync());
        using var stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        Assert.Equal(roll.TotalSourceRows + 1,workbook.Worksheet("Details").LastRowUsed()!.RowNumber());
        Assert.Equal(roll.TotalRows + 1,workbook.Worksheet("Summary").LastRowUsed()!.RowNumber());
        await using var db = new NexaErpDbContext(options);
        Assert.Equal(3,await db.AuditLogs.CountAsync(row => row.Module == "Reports" && row.Action == "Export"));
    }

    private static async Task<GoodsReceiptResult> RunPurchaseBand(HttpClient prClient, HttpClient approvalClient, HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, PurchaseFlowBand band, Guid creatorId, Guid managerId, Guid tdId, Guid mdId,
        Guid verifierId, Guid purchaseId, Guid storesId, Guid qcId, Guid vendor1Id, Guid vendor2Id,
        Func<GoodsReceiptResult, Task<GoodsReceiptResult>>? finalizeRace = null,
        Func<PurchaseRequisitionDetail, Task<PurchaseRequisitionDetail>>? approveRace = null,
        Func<string, Guid, Task>? workload = null,
        Func<string, Guid, Task>? openOrders = null, bool overdueQuoteDates = false,
        Func<Rev869BDocumentResult, Task<Rev869BDocumentResult>>? amendIssued = null,
        Func<string,Guid,Task>? storesWorkload = null, Func<string,Guid,Task>? qcStock = null,
        Func<string,Guid,Task>? supplierInvoices = null, decimal receiptQuantity = 1m, decimal requestedQuantity = 1m, decimal expectedHandoffQuantity = 1m)
    {
        var required = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        user.Set(creatorId, "SESS-12", "IT_MANAGER");
        var employeePage=await Get<PagedResponse<SESS.NexaERP.Application.Employees.EmployeeSummary>>(prClient,"/api/v1/employees?page=1&pageSize=1");
        Assert.True(employeePage.TotalCount>1);var employee=Assert.Single(employeePage.Items);
        using(var stale=await prClient.PutAsJsonAsync($"/api/v1/employees/{employee.EmployeeCode}",new SESS.NexaERP.Application.Employees.UpdateEmployeeRequest(
            employee.EmployeeName,employee.EmployeeType,employee.Grade,"NOT_USED","NOT_USED","NOT_USED",null,null,null,"Concurrency witness",employee.Version+1)))
            Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);
        var pr = await Post<PurchaseRequisitionDetail>(client, "/api/v1/purchase/requisitions",
            new CreatePurchaseRequisitionRequest("SESS_PVT_LTD", "IT", "SESS-12", required, "NORMAL",
                $"TRIAL {band.Code} full Purchase flow", "TRIAL-WH-C01", null, null, null, null, null,
                [new("TRIAL-ITEM-001", requestedQuantity, band.PrAmount, required, "TRIAL-WH-C01", null, null, null)]));
        Assert.Equal(PurchaseRequisitionStatuses.Draft, pr.Status);
        await AssertPrEvidence(options, pr.Id, "CreateDraft", 1, 1);
        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.StoresExecutive);
        using (var wrongState = await prClient.GetAsync($"/api/v1/stores/stock-check/requisitions/{pr.PrNumber}"))
            Assert.Equal(HttpStatusCode.NotFound, wrongState.StatusCode);
        user.Set(creatorId, "SESS-12", "IT_MANAGER");
        pr = await Post<PurchaseRequisitionDetail>(prClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/submit",
            new PurchaseRequisitionActionRequest(null, pr.Version, $"{band.Code}-pr-submit"));
        Assert.Equal(PurchaseRequisitionStatuses.Submitted, pr.Status);
        await AssertPrEvidence(options, pr.Id, "Submit", 2, 2);
        if (workload is not null) await workload("PR_SUBMITTED", pr.Id);
        using(var refused=await prClient.PostAsJsonAsync($"/api/v1/purchase/requisitions/{pr.PrNumber}/verify",
            new PurchaseRequisitionActionRequest("Requester must not verify",pr.Version,$"{band.Code}-pr-self-verify")))
            Assert.Equal(HttpStatusCode.Forbidden,refused.StatusCode);
        user.Set(managerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        pr = await Post<PurchaseRequisitionDetail>(approvalClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/verify",
            new PurchaseRequisitionActionRequest("Department verified", pr.Version, $"{band.Code}-pr-verify"));
        Assert.Equal(PurchaseRequisitionStatuses.PendingApproval, pr.Status);
        await AssertPrEvidence(options, pr.Id, "DepartmentVerify", 3, 3);
        if (workload is not null) await workload("PR_APPROVAL", pr.Id);

        if (band.Level2EmployeeId.HasValue)
        {
            var futureRole = band.Level2EmployeeId == tdId ? Rev869ARoleCodes.TechnicalDirector : Rev869ARoleCodes.ManagingDirector;
            user.Set(band.Level2EmployeeId.Value,
                futureRole == Rev869ARoleCodes.TechnicalDirector ? "SESS-01" : "SESS-02", futureRole);
            var futureApproverQueue = await Get<PagedResponse<PurchaseRequisitionSummary>>(approvalClient,
                $"/api/v1/purchase/requisitions?prNumber={pr.PrNumber}");
            Assert.Equal(pr.Id, Assert.Single(futureApproverQueue.Items).Id);
            Assert.Equal(pr.Id, (await Get<PurchaseRequisitionDetail>(approvalClient,
                $"/api/v1/purchase/requisitions/{pr.PrNumber}")).Id);
            user.Set(managerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        }

        var pendingReport = await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
            "/api/v1/reports/pending-approvals?mode=details");
        var pendingPr = Assert.Single(pendingReport.Rows,row => row.GetProperty("documentId").GetGuid()==pr.Id);
        Assert.Equal("SESS-14",pendingPr.GetProperty("approverCode").GetString());
        Assert.Equal("NAMED_EMPLOYEE",pendingPr.GetProperty("assignmentKind").GetString());
        Assert.Equal(1,pendingPr.GetProperty("pendingActions").GetInt32());
        Assert.Equal(0,pendingPr.GetProperty("ageDays").GetInt32());
        Assert.NotNull(pendingReport.Coverage);

        var managerQueue = await Get<PagedResponse<PurchaseRequisitionSummary>>(approvalClient,
            $"/api/v1/purchase/requisitions?prNumber={pr.PrNumber}");
        Assert.Equal(pr.Id, Assert.Single(managerQueue.Items).Id);
        Assert.Equal(pr.Id, (await Get<PurchaseRequisitionDetail>(approvalClient,
            $"/api/v1/purchase/requisitions/{pr.PrNumber}")).Id);
        pr = approveRace is not null ? await approveRace(pr)
            : await Post<PurchaseRequisitionDetail>(approvalClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/approve",
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
        if (workload is not null) await workload("PR_STOCK_CHECK", pr.Id);

        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.StoresExecutive,
            Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresExecutive);
        var stockCheckDetail = await Get<StockCheckPurchaseRequisitionDetail>(prClient,
            $"/api/v1/stores/stock-check/requisitions/{pr.PrNumber}");
        Assert.Equal(pr.PrNumber, stockCheckDetail.PrNumber);
        Assert.Equal(pr.Version, stockCheckDetail.Version);
        Assert.Equal("TRIAL-WH-C01", stockCheckDetail.DeliveryWarehouseCode);
        Assert.Equal(PurchaseRequisitionStatuses.StockCheckPending, stockCheckDetail.Status);
        Assert.Equal(1, Assert.Single(stockCheckDetail.Lines).LineNumber);
        user.SetOrganization("SESS_PROPRIETORSHIP");
        using (var wrongCompany = await prClient.GetAsync($"/api/v1/stores/stock-check/requisitions/{pr.PrNumber}"))
            Assert.Equal(HttpStatusCode.NotFound, wrongCompany.StatusCode);
        user.SetOrganization("SESS_PVT_LTD");
        await PostNoResult(prClient, $"/api/v1/purchase/requisitions/{pr.PrNumber}/stock-check",
            new StockCheckRequest("No stock; purchase required", pr.Version, $"{band.Code}-stock",
                [new(1, "TRIAL-WH-C01", "TRIAL-C01-GEN-01")]), $"{band.Code}-stock");
        var handoff = await Query(options, db => db.PurchaseRequirementHandoffs
            .Where(x => x.PurchaseRequisitionId == pr.Id).Select(x => new { x.Id, x.HandoffQuantity }).SingleAsync());
        Assert.Equal(expectedHandoffQuantity, handoff.HandoffQuantity);
        await AssertPrEvidence(options, pr.Id, "StockCheck", 4 + band.RequiredSteps, 4 + band.RequiredSteps);

        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseExecutive,
            Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresExecutive);
        var rfq = await Post<Rev869BDocumentResult>(client, "/api/v1/purchase/rfqs",
            new Rev869BCreateRfqRequest(DateTimeOffset.UtcNow.AddDays(7), "INR", false, null,
                $"{band.Code}-rfq-create", [new(handoff.Id, handoff.HandoffQuantity)]));
        await AssertTransactionEvidence(options, "RFQ", rfq.Id, "CreateRFQ");
        if (band.Code == "LOW") await RotatePurchaseWitnessSubject(options, user, purchaseId, "after-rfq");
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
        if (workload is not null) await workload("RFQ_NO_QUOTATION", rfq.Id);
        Assert.Equal(1,rfqList.TotalCount);Assert.Equal(rfq.Id,Assert.Single(rfqList.Items).Id);
        var rfqDetail=await Get<RfqDetail>(client,$"/api/v1/purchase/rfqs/{rfq.Number}");
        Assert.Equal(rfq.Id,rfqDetail.Id);Assert.Single(rfqDetail.Lines);
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
                    [new(rfqLineId, handoff.HandoffQuantity, rate, 0, 0, 0, 0, 0,
                        overdueQuoteDates && band.Code=="LOW" ? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2) : required,
                        "9025", "33", "33", VendorRegistrationType.REGULAR.ToCanonicalValue(), 0)]));
            quotations.Add(quote);
            await AssertTransactionEvidence(options, "VendorQuotation", quote.Id, "SubmitQuotation");
        }
        var quotationList=await Get<PagedResponse<QuotationListItem>>(client,$"/api/v1/purchase/quotations?quotationNumber={quotations[0].Number}&vendorId={vendor1Id}");
        if (workload is not null) await workload("QUOTATION_VERIFY", quotations[0].Id);
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
        if (workload is not null) await workload("COMPARISON_DRAFT", comparison.Id);
        comparison = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/comparisons/{comparison.Number}/recommend",
            new Rev869BRecommendComparisonRequest(quotations[0].Id, "Lowest compliant offer", null,
                comparison.Version, $"{band.Code}-comparison-recommend"));
        await AssertTransactionEvidence(options, "CommercialComparison", comparison.Id, "RecommendVendor");
        if (workload is not null) await workload("COMPARISON_APPROVAL", comparison.Id);

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
        var comparisonDetail=await Get<JsonElement>(client,$"/api/v1/purchase/comparisons/{comparison.Number}");
        Assert.Equal(comparison.Id,comparisonDetail.GetProperty("Id").GetGuid());
        Assert.Equal(2,comparisonDetail.GetProperty("Lines").GetArrayLength());
        await AssertApprovalActors(options, "CMP", comparison.Id, band.RequiredSteps, managerId, band.Level2EmployeeId);

        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresExecutive);
        var po = await Post<Rev869BDocumentResult>(client, "/api/v1/purchase/purchase-orders",
            new Rev869BCreatePurchaseOrderRequest(comparison.Number, comparison.Version, $"{band.Code}-po-create"));
        await AssertPoEvidence(options, po.Id, "CreatePO");
        if (band.Code == "LOW") await RotatePurchaseWitnessSubject(options, user, purchaseId, "after-po");
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
        if (workload is not null) await workload("PO_APPROVED", po.Id);
        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresExecutive);
        po = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/purchase-orders/{po.Number}/issue",
            new Rev869BIssuePurchaseOrderRequest("PO issued", po.Version, $"{band.Code}-po-issue"));
        Assert.Equal(Rev869BStatuses.Issued, po.Status);
        await AssertPoEvidence(options, po.Id, "IssuePO");
        if(openOrders is not null) await openOrders("ISSUED",po.Id);
        if(supplierInvoices is not null) await supplierInvoices("ISSUED",po.Id);
        if(amendIssued is not null) po=await amendIssued(po);
        var poList=await Get<PagedResponse<PurchaseOrderListItem>>(client,$"/api/v1/purchase/purchase-orders?purchaseOrderNumber={po.Number}&vendorId={vendor1Id}");
        Assert.Equal(1,poList.TotalCount);Assert.Equal(po.Id,Assert.Single(poList.Items).Id);
        var poDetail=await Get<PurchaseOrderCommercialDetail>(client,$"/api/v1/purchase/purchase-orders/{po.Number}");
        Assert.Equal(po.Id,poDetail.Id);Assert.Single(poDetail.Lines);
        var followups=await Get<PagedResponse<MaterialFollowUpListItem>>(client,"/api/v1/purchase/material-followup?pageSize=100");
        Assert.Contains(followups.Items,x=>x.PurchaseOrderId==po.Id);

        user.Set(storesId,"SESS-35",Rev869ARoleCodes.StoresExecutive,Rev869ARoleCodes.StoresExecutive);
        var poLineId=await Query(options,db=>db.PurchaseOrderLines.Where(x=>x.PurchaseOrderId==po.Id).Select(x=>x.Id).SingleAsync());
        var gate=await Post<GateEntryResult>(prClient,"/api/v1/stores/gate-entries/",
            new CreateGateEntryRequest(po.Number,$"TRIAL-DC-{band.Code}","TRIAL-VEHICLE","ROAD",DateTimeOffset.UtcNow,"{\"packagesChecked\":true}",[new(poLineId,receiptQuantity)]),$"{band.Code}-gate-create");
        Assert.Equal("DRAFT",gate.Status); Assert.Single(gate.History);
        gate=await Put<GateEntryResult>(prClient,$"/api/v1/stores/gate-entries/{gate.Id}",new UpdateGateEntryRequest(gate.VendorDcNumber,"TRIAL-VEHICLE-EDITED","ROAD",gate.ArrivedAt,"{\"packagesChecked\":true,\"edited\":true}",[new(poLineId,receiptQuantity)],gate.Version));
        var detail=await Get<GateEntryResult>(prClient,$"/api/v1/stores/gate-entries/{gate.Id}"); Assert.Equal("TRIAL-VEHICLE-EDITED",detail.VehicleNumber);
        var list=await Get<GateEntryListResult>(prClient,$"/api/v1/stores/gate-entries/?gateEntryNumber={gate.GateEntryNumber}"); Assert.Contains(list.Items,x=>x.Id==gate.Id);Assert.Equal(1,list.TotalCount);
        gate=await Post<GateEntryResult>(prClient,$"/api/v1/stores/gate-entries/{gate.Id}/finalize",new FinalizeGateEntryRequest(gate.Version,$"{band.Code}-gate-finalize"));
        Assert.Equal("FINALIZED",gate.Status); Assert.Equal(2,gate.History.Count);
        if(storesWorkload is not null)await storesWorkload("GATE_FINALIZED",gate.Id);
        await using var gateEvidence=new NexaErpDbContext(options); Assert.Equal(3,await gateEvidence.AuditLogs.CountAsync(x=>x.EntityId==gate.Id.ToString()&&x.Module=="Stores"));

        IReadOnlyList<GoodsReceiptSerialRequest> serials=band.QuoteRate>5000m
            ? Enumerable.Range(1,checked((int)receiptQuantity)).Select(n => new GoodsReceiptSerialRequest(n,1,receiptQuantity==1?$"TRIAL-SERIAL-{band.Code}":$"TRIAL-SERIAL-{band.Code}-{n}",receiptQuantity==1?$"TRIAL-SERIAL-{band.Code}":$"TRIAL-SERIAL-{band.Code}-{n}",false,null)).ToArray() : [];
        var billDate=DateOnly.FromDateTime(DateTime.UtcNow);var receivedAt=band.Code=="LOW"?DateTimeOffset.UtcNow.AddDays(-3):DateTimeOffset.UtcNow;
        var grn=await Post<GoodsReceiptResult>(prClient,"/api/v1/stores/goods-receipts/",
            new CreateGoodsReceiptRequest(gate.GateEntryNumber,$"TRIAL-BILL-{band.Code}",billDate,receivedAt,"{\"billChecked\":true}",
                [new(gate.Lines.Single().Id,[new(1,receiptQuantity,$"TRIAL-BATCH-{band.Code}",null,billDate.AddMonths(-1),billDate.AddYears(2))],serials)]),
            $"{band.Code}-grn-create");
        Assert.Equal("DRAFT",grn.Status);Assert.Single(grn.History);Assert.Single(grn.Lines);Assert.Single(grn.Lines[0].Lots);
        if(storesWorkload is not null)await storesWorkload("GRN_DRAFT",gate.Id);
        Assert.Equal(billDate.AddMonths(13),grn.Lines[0].WarrantyExpiryDate);Assert.Equal("9025",grn.Lines[0].HsnSacCode);
        Assert.Equal(band.QuoteRate>5000m?"REQUIRED":"OPTIONAL",grn.Lines[0].SerialCaptureMode);Assert.Empty(grn.Warnings);
        var draftVersion=grn.Version;
        grn = finalizeRace is null
            ? await Post<GoodsReceiptResult>(client,$"/api/v1/stores/goods-receipts/{grn.Id}/finalize",new FinalizeGoodsReceiptRequest(draftVersion,$"{band.Code}-grn-finalize"))
            : await finalizeRace(grn);
        Assert.Equal("FINALIZED",grn.Status);Assert.Equal(2,grn.History.Count);Assert.NotNull(grn.StockPostingBatchId);Assert.False(grn.Replayed);Assert.Empty(grn.Warnings);
        if(openOrders is not null) await openOrders("RECEIVED",po.Id);
        if(supplierInvoices is not null) await supplierInvoices("RECEIVED",po.Id);
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
        if(qcStock is not null) await qcStock("GRN_FINALIZED",grn.Id);
        return grn;
    }

    private static async Task RunQcWitness(HttpClient client,DbContextOptions<NexaErpDbContext> options,TaxWorkflowUser user,PurchaseFlowBand band,GoodsReceiptResult grn,Guid qcId,Guid tdId,
        Func<QcInspectionResult, FinalizeQcInspectionRequest, Task<QcInspectionResult>>? correctionWitness = null,
        Func<QcInspectionResult, FinalizeQcInspectionRequest, InventoryConcessionResult, Guid, Task<InventoryConcessionResult>>? concessionWitness = null,
        Func<string,Guid,Task>? qcStock = null, bool historicalPolicyFixture = false)
    {
        var lot=grn.Lines.Single().Lots.Single();var serialId=grn.Lines.Single().Serials.SingleOrDefault()?.InventorySerialId;var available=await Query(options,db=>db.WarehouseConditionLocations.Where(x=>x.CompanyId==Guid.Parse("70000000-0000-0000-0000-000000000001")&&x.ConditionCode=="AVAILABLE"&&x.IsActive).OrderBy(x=>x.Id).Select(x=>x.Id).FirstAsync());
        user.Set(qcId,"SESS-33",Rev869ARoleCodes.QcManager);
        var accepted=band.Code=="LOW"?.95m:band.Code=="TD"?1m:0m;var rejected=band.Code=="LOW"?.05m:band.Code=="MD"?1m:0m;var observed=rejected>0?12m:5m;IReadOnlyList<QcSerialDispositionRequest> serials=serialId.HasValue?[new QcSerialDispositionRequest(serialId.Value,accepted>0?"ACCEPTED":"REJECTED",accepted>0?null:"Measured parameter failed")]:Array.Empty<QcSerialDispositionRequest>();
        if(band.Code=="LOW")
        {
            var missingQueue=await Get<PagedResponse<QcQueueItem>>(client,"/api/v1/qc/queue?page=1&pageSize=100");Assert.Contains(missingQueue.Items,x=>x.GoodsReceiptLineLotAllocationId==lot.Id&&!x.HasEffectivePolicy);
            var deniedBody=new FinalizeQcInspectionRequest(lot.Id,DateTimeOffset.UtcNow,accepted,rejected,0,available,[],serials);using var deniedRequest=new HttpRequestMessage(HttpMethod.Post,"/api/v1/qc/inspections"){Content=JsonContent.Create(deniedBody)};deniedRequest.Headers.Add("Idempotency-Key","LOW-qc-missing-policy");using var denied=await client.SendAsync(deniedRequest);Assert.Equal(HttpStatusCode.Conflict,denied.StatusCode);
            Assert.False(await Query(options,db=>db.QcInspections.AnyAsync(x=>x.GoodsReceiptLineLotAllocationId==lot.Id)));Assert.Equal(1m,await Query(options,db=>db.StockMovements.Where(x=>x.GoodsReceiptLineLotAllocationId==lot.Id&&x.ConditionCode=="QC_HOLD").SumAsync(x=>x.QuantityIn-x.QuantityOut)));
            if (historicalPolicyFixture)
                await CreateHistoricalQcPolicyFixture(options, grn.Lines.Single().ItemId);
            else
                await CreateAndDecideQcPolicyThroughApi(client, options, user, grn, qcId, tdId);
        }
        var policyId=await Query(options,db=>db.QcInspectionPolicies.Where(x=>x.ItemId==grn.Lines.Single().ItemId&&x.IsActive).Select(x=>x.Id).SingleAsync());var queue=await Get<PagedResponse<QcQueueItem>>(client,"/api/v1/qc/queue?page=1&pageSize=100");var queueItem=Assert.Single(queue.Items,x=>x.GoodsReceiptLineLotAllocationId==lot.Id&&x.IsOverdue==(band.Code=="LOW")&&x.HasEffectivePolicy);if(serialId.HasValue)Assert.Equal(serialId.Value,Assert.Single(queueItem.InventorySerialIds));var request=new FinalizeQcInspectionRequest(lot.Id,DateTimeOffset.UtcNow,accepted,rejected,0,accepted>0?available:null,[new QcParameterResultRequest(policyId,1,observed,null,rejected>0?"FAIL":"PASS",null)],serials);
        var result=await Post<QcInspectionResult>(client,"/api/v1/qc/inspections",request,$"{band.Code}-qc-finalize");Assert.Equal(accepted,result.AcceptedQuantity);Assert.Equal(rejected,result.RejectedQuantity);Assert.NotNull(result.StockPostingBatchId);Assert.False(result.Replayed);
        var replay=await Post<QcInspectionResult>(client,"/api/v1/qc/inspections",request,$"{band.Code}-qc-finalize");Assert.True(replay.Replayed);Assert.Equal(result.RevisionId,replay.RevisionId);Assert.Equal(result.StockPostingBatchId,replay.StockPostingBatchId);
        await using(var evidence=new NexaErpDbContext(options))
        {
            Assert.Single(await evidence.StockPostingBatches.Where(x=>x.QcInspectionRevisionId==result.RevisionId&&x.PostingKind=="QC_DISPOSITION").ToListAsync());var movements=await evidence.StockMovements.Where(x=>x.StockPostingBatchId==result.StockPostingBatchId).ToListAsync();Assert.All(movements,x=>{Assert.NotNull(x.QcInspectionLotDispositionId);Assert.Null(x.QcInspectionRevisionId);});Assert.Equal(accepted,await evidence.StockMovements.Where(x=>x.StockPostingBatchId==result.StockPostingBatchId&&x.ConditionCode=="AVAILABLE").SumAsync(x=>x.QuantityIn-x.QuantityOut));Assert.Equal(rejected,await evidence.StockMovements.Where(x=>x.StockPostingBatchId==result.StockPostingBatchId&&x.ConditionCode=="PENDING_RETURNABLE_DC").SumAsync(x=>x.QuantityIn-x.QuantityOut));
            if(band.Code=="LOW"){var childTypes=await evidence.InventoryProvenanceEdges.Where(x=>movements.Select(m=>m.InventoryProvenanceLayerId).Contains(x.ToProvenanceLayerId)).Join(evidence.InventoryProvenanceLayers,e=>e.ToProvenanceLayerId,l=>l.Id,(e,l)=>l.LayerType).Distinct().ToListAsync();Assert.Contains(InventoryProvenanceLayerTypes.QcAccepted,childTypes);Assert.Contains(InventoryProvenanceLayerTypes.QcRejected,childTypes);}
        }
        if(qcStock is not null) await qcStock("QC_DISPOSITION",grn.Id);
        if(band.Code=="MD")
        {
            if(correctionWitness is not null) result=await correctionWitness(result,request);
            var failed=Assert.Single(result.ParameterResults);var concessionSerialId=Assert.Single(result.SerialDispositions).InventorySerialId;var draft=await Post<InventoryConcessionResult>(client,"/api/v1/qc/concessions",new CreateInventoryConcessionRequest(result.QcInspectionLotDispositionId,failed.Id,1,"DIMENSIONAL_LIMIT",failed.MeasuredValue,"Technical Director accepts measured deviation for controlled non-critical use","Controlled internal test fixture",[concessionSerialId]),"MD-concession-create");Assert.Equal("DRAFT",draft.Status);Assert.Equal(concessionSerialId,Assert.Single(draft.InventorySerialIds));
            user.Set(tdId,"SESS-01",Rev869ARoleCodes.TechnicalDirector);var approved=concessionWitness is null
                ? await Post<InventoryConcessionResult>(client,$"/api/v1/qc/concessions/{draft.ConcessionNumber}/approve",new ApproveInventoryConcessionRequest(draft.Version,available,"Direct technical acceptance"),"MD-concession-approve")
                : await concessionWitness(result,request,draft,available);Assert.Equal("APPROVED",approved.Status);Assert.NotNull(approved.StockPostingBatchId);Assert.Contains("DIMENSIONAL_LIMIT",approved.ProvenanceAnnotationJson);Assert.Contains(tdId.ToString(),approved.ProvenanceAnnotationJson);
            Assert.Equal(result.RevisionId,approved.QcInspectionRevisionId);
            Assert.Equal(failed.MeasuredValue,approved.MeasuredValue);
            using(var annotation=JsonDocument.Parse(approved.ProvenanceAnnotationJson!))
                Assert.Equal(failed.MeasuredValue,annotation.RootElement.GetProperty("measuredValue").GetString());
            await using var evidence=new NexaErpDbContext(options);var moves=await evidence.StockMovements.Where(x=>x.StockPostingBatchId==approved.StockPostingBatchId).ToListAsync();Assert.Equal(1m,moves.Where(x=>x.ConditionCode=="PENDING_RETURNABLE_DC").Sum(x=>x.QuantityOut));Assert.Equal(1m,moves.Where(x=>x.ConditionCode=="AVAILABLE").Sum(x=>x.QuantityIn));Assert.All(moves,x=>Assert.Equal(serialId,x.InventorySerialId));Assert.True(await evidence.InventoryProvenanceAnnotations.AnyAsync(x=>x.InventoryConcessionId==approved.Id&&x.InventoryProvenanceLayerId==approved.AvailableProvenanceLayerId));
            if(qcStock is not null) await qcStock("CONCESSION_APPROVED",grn.Id);
        }
    }

    private static async Task RunMaterialIssueWitness(HttpClient client,
        DbContextOptions<NexaErpDbContext> options, string runtimeConnection, TaxWorkflowUser user, GoodsReceiptResult grn, GoodsReceiptResult serializedGrn,
        Guid engineerId, Guid purchaseId, Guid productionId, Guid storesId, Guid tdId, Guid accountsManagerId, Guid qcId,
        VendorBillView pendingLandedBill, Func<ReturnFitmentRaceContext, Task<MaterialReturnView>>? returnRace = null,
        Func<string>? readPostgresLog = null, bool serializedRace = false,
        Func<MaterialIssueRequestView, Task<MaterialIssueRequestView>>? approveRace = null,
        Func<MaterialIssueRequestView, CreateMaterialIssue, Task<MaterialIssueView>>? issueRace = null,
        Func<string,Guid,Task>? storesWorkload = null)
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
                SalesType = CustomerPoSalesTypes.Machine, WorkStatus = CustomerPoWorkStatuses.Wip, CurrentRevisionNumber = 1,
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
            var spareCpo = new CustomerPurchaseOrder
            {
                CompanyId = companyId, CustomerId = customer.Id, PoRecordNumber = "CPO-MIR-SPARE-001",
                CustomerPoNumber = "CUSTOMER-MIR-SPARE-001", CustomerPoDate = new DateOnly(2026, 9, 7),
                SalesType = CustomerPoSalesTypes.Spares, WorkStatus = CustomerPoWorkStatuses.Wip,
                CurrentRevisionNumber = 1, CreatedBy = "MIR_WITNESS"
            };
            var spareRevision = new CustomerPurchaseOrderRevision
            {
                CustomerPurchaseOrderId = spareCpo.Id, RevisionNumber = 1,
                ChangeReason = "Spare-sale MIR witness", SnapshotJson = "{}", CreatedBy = "MIR_WITNESS"
            };
            var spareLine = new CustomerPurchaseOrderLine
            {
                CustomerPurchaseOrderId = spareCpo.Id, RevisionNumber = 1, SlNo = 1,
                ItemId = item.Id, UomId = item.BaseUomId, Description = item.Name,
                Quantity = .90m, Uom = line.Uom, CreatedBy = "MIR_WITNESS"
            };
            spareRevision.Lines.Add(spareLine); spareCpo.Revisions.Add(spareRevision);
            db.Customers.Add(customer); db.CustomerPurchaseOrders.AddRange(cpo, spareCpo);
            await db.SaveChangesAsync();
            return new { CpoLineId = line.Id, MachineLineId = machineLine.Id, SpareLineId = spareLine.Id,
                CustomerId = customer.Id, UomId = item.BaseUomId, ItemCode = item.ItemCode,
                DepartmentId = productionDepartmentId };
        });

        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        var customerPoLines = await Get<JobOrderCustomerPoLineView[]>(client,
            "/api/v1/production/job-orders/customer-po-lines");
        Assert.Contains(customerPoLines, x => x.Id == fixture.MachineLineId
            && x.CustomerPurchaseOrderId != Guid.Empty && x.ItemId == itemId);
        var jobCommand = new CreateJobOrderRequest(fixture.MachineLineId, 1, "WITNESS-MACHINE-001",
            new DateOnly(2026, 9, 7), new DateOnly(2026, 12, 1), "job-order-create");
        var job = await Post<JobOrderView>(client, "/api/v1/production/job-orders", jobCommand);
        var jobReplay = await Post<JobOrderView>(client, "/api/v1/production/job-orders", jobCommand);
        Assert.Equal("PENDING_ACCOUNTS", job.Status); Assert.Equal(job.Id, jobReplay.Id);
        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        await AssertPostStatus(client, "/api/v1/design/estimated-boms",
            new CreateEstimatedBomRequest(job.Id, "Must wait for Accounts",
                [new EstimatedBomLineInput(itemId, fixture.UomId, .90m, "Witness component", 100m)], "mir-est-before-accounts"),
            HttpStatusCode.Conflict);
        user.Set(accountsManagerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        var returnRequest = new ConfirmJobOrderRequest(job.Version,
            "Machine serial needs Production correction", "job-order-return-draft");
        job = await Post<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}/return-to-draft", returnRequest);
        var jobReturnReplay = await Post<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}/return-to-draft", returnRequest);
        Assert.Equal("DRAFT", job.Status); Assert.Equal(job.Id, jobReturnReplay.Id);
        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        var reviseRequest = new ReviseDraftJobOrderRequest(job.Version, "WITNESS-MACHINE-001-CORRECTED",
            job.JobOrderDate, job.PlannedCompletionDate, "Corrected machine serial", "job-order-revise-draft");
        job = await Put<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}/draft", reviseRequest);
        var reviseReplay = await Put<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}/draft", reviseRequest);
        Assert.Equal("WITNESS-MACHINE-001-CORRECTED", job.MachineSerial); Assert.Equal(job.Id, reviseReplay.Id);
        var resubmit = new ConfirmJobOrderRequest(job.Version, "Corrected metadata ready for Accounts", "job-order-resubmit");
        job = await Post<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}/resubmit", resubmit);
        var resubmitReplay = await Post<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}/resubmit", resubmit);
        Assert.Equal("PENDING_ACCOUNTS", job.Status); Assert.Equal(job.Id, resubmitReplay.Id);
        user.Set(accountsManagerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        var confirm = new ConfirmJobOrderRequest(job.Version, "Customer PO and one-machine scope verified", "job-order-accounts-confirm");
        job = await Post<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}/accounts-confirm", confirm);
        var confirmReplay = await Post<JobOrderView>(client, $"/api/v1/production/job-orders/{job.Id}/accounts-confirm", confirm);
        Assert.Equal("OPEN", job.Status); Assert.Equal(job.Id, confirmReplay.Id);
        Assert.NotEqual(job.InitiatedByEmployeeId, job.AccountsConfirmedByEmployeeId);

        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        var unpriced = await Query(options, async db => await db.Items.AsNoTracking()
            .Where(x => x.IsActive && x.Id != itemId && !db.ItemCompanyLastPurchases.Any(p =>
                p.CompanyId == companyId && p.ItemId == x.Id && p.LastPurchaseRate != null))
            .Select(x => new { x.ItemCode, UomCode = x.BaseUom!.Code })
            .FirstAsync());
        var workbookBytes = EstimatedBomWorkbook.CreateTemplate(DateTimeOffset.Parse("2026-09-08T00:00:00Z"));
        using (var workbook = new ClosedXML.Excel.XLWorkbook(new MemoryStream(workbookBytes)))
        {
            var data = workbook.Worksheet("Data");
            data.Cell(2, 1).Value = job.JobOrderNumber;
            data.Cell(2, 2).Value = "Unpriced workbook submission witness";
            data.Cell(2, 3).Value = 1;
            data.Cell(2, 4).Value = unpriced.ItemCode;
            data.Cell(2, 5).Value = unpriced.UomCode;
            data.Cell(2, 6).Value = 1;
            using var output = new MemoryStream(); workbook.SaveAs(output); workbookBytes = output.ToArray();
        }
        using var workbookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/design/estimated-boms/workbook/import");
        workbookRequest.Headers.Add("Idempotency-Key", "mir-est-workbook-import");
        using var workbookForm = new MultipartFormDataContent();
        workbookForm.Add(new ByteArrayContent(workbookBytes), "file", "unpriced-estimated-bom.xlsx");
        workbookRequest.Content = workbookForm;
        using var workbookResponse = await client.SendAsync(workbookRequest);
        var workbookPayload = await workbookResponse.Content.ReadAsStringAsync();
        Assert.True(workbookResponse.IsSuccessStatusCode, workbookPayload);
        var estimated = JsonSerializer.Deserialize<EstimatedBomView>(workbookPayload, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Null(Assert.Single(estimated.CurrentRevision.Lines).EstimatedUnitValue);
        await AssertPostStatusContains(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/submit",
            new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Must fail", "mir-est-unpriced-submit"),
            HttpStatusCode.Conflict, unpriced.ItemCode, "EstimatedUnitValue", "before submission");
        estimated = await Put<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}",
            new ReplaceEstimatedBomLinesRequest(estimated.CurrentRevision.Version, "Witness commercial baseline",
                [new EstimatedBomLineInput(itemId, fixture.UomId, .90m, "Witness component", 100m)], "mir-est-price-draft"));
        estimated = await Post<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/submit",
            new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Ready for TD review", "mir-est-submit"));
        user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
        estimated = await Post<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/return-to-draft",
            new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Price evidence needs preparer confirmation", "mir-est-return-draft"));
        Assert.Equal("DRAFT", estimated.Status);
        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        estimated = await Post<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/submit",
            new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Price evidence confirmed", "mir-est-resubmit"));
        user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
        estimated = await Post<EstimatedBomView>(client, $"/api/v1/design/estimated-boms/{estimated.BomNumber}/approve",
            new EstimatedBomActionRequest(estimated.CurrentRevision.Version, "Commercial baseline approved", "mir-est-approve"));
        Assert.Equal(100m, estimated.CurrentRevision.Lines.Single().EstimatedUnitValue);
        Assert.True(estimated.CurrentRevision.Lines.Single().EstimatedUnitValueOverridden);
        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        var production = await Post<ProductionBomView>(client, "/api/v1/production/boms",
            new CreateProductionBomRequest(job.Id, "Witness production baseline", "mir-pbom-create"));
        production = await Post<ProductionBomView>(client, $"/api/v1/production/boms/{production.BomNumber}/submit",
            new ProductionBomActionRequest(production.CurrentRevision.Version, "Production baseline submitted", "mir-pbom-submit"));
        user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
        production = await Post<ProductionBomView>(client, $"/api/v1/production/boms/{production.BomNumber}/return-to-draft",
            new ProductionBomActionRequest(production.CurrentRevision.Version, "Production quantities need preparer confirmation", "mir-pbom-return-draft"));
        Assert.Equal("DRAFT", production.Status);
        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        production = await Post<ProductionBomView>(client, $"/api/v1/production/boms/{production.BomNumber}/submit",
            new ProductionBomActionRequest(production.CurrentRevision.Version, "Production quantities confirmed", "mir-pbom-resubmit"));
        user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
        production = await Post<ProductionBomView>(client, $"/api/v1/production/boms/{production.BomNumber}/approve",
            new ProductionBomActionRequest(production.CurrentRevision.Version, "Production baseline approved", "mir-pbom-approve"));
        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        production = await Post<ProductionBomView>(client, $"/api/v1/production/boms/{production.BomNumber}/pin",
            new PinProductionBomRevisionRequest(production.CurrentRevision.Id, job.Version,
                "Pinned to the one-machine Job Order", "mir-pbom-pin"));
        Assert.Equal(production.CurrentRevision.Id, production.PinnedRevisionId);

        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        var drawing = await Post<EngineeringDocumentView>(client, "/api/v1/design/documents",
            new CreateEngineeringDocumentRequest(job.Id, "GA", "Witness general arrangement",
                new EngineeringDocumentRevisionInput("A", engineerId, productionId,
                    "Initial witness drawing", new DateOnly(2026, 9, 8), "drawings/witness-ga-a.pdf",
                    "witness-ga-a.pdf", "application/pdf", 128, new string('a', 64)),
                "mir-drawing-create"));
        var drawingRevision = drawing.Revisions.Single();
        drawing = await Post<EngineeringDocumentView>(client, $"/api/v1/design/documents/{drawing.DocumentNumber}/submit",
            new EngineeringDocumentActionRequest(drawingRevision.Version, "Ready for TD review", "mir-drawing-submit"));
        user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
        drawingRevision = drawing.Revisions.Single();
        drawing = await Post<EngineeringDocumentView>(client, $"/api/v1/design/documents/{drawing.DocumentNumber}/return-to-draft",
            new EngineeringDocumentActionRequest(drawingRevision.Version, "Checker evidence needs confirmation", "mir-drawing-return-draft"));
        Assert.Equal("DRAFT", drawing.Status);
        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        drawingRevision = drawing.Revisions.Single();
        drawing = await Post<EngineeringDocumentView>(client, $"/api/v1/design/documents/{drawing.DocumentNumber}/submit",
            new EngineeringDocumentActionRequest(drawingRevision.Version, "Checker evidence confirmed", "mir-drawing-resubmit"));
        user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
        drawingRevision = drawing.Revisions.Single();
        drawing = await Post<EngineeringDocumentView>(client, $"/api/v1/design/documents/{drawing.DocumentNumber}/approve",
            new EngineeringDocumentActionRequest(drawingRevision.Version, "Drawing approved", "mir-drawing-approve"));
        Assert.Equal("APPROVED", drawing.Status);

        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.StoresExecutive);
        var mir = await Post<MaterialIssueRequestView>(client, "/api/v1/stores/material-issue-requests",
            new CreateMaterialIssueRequest("FACTORY_ASSEMBLY", "CHAMBER_MANUFACTURE", "JOB_ORDER",
                job.Id, null, null, null, "Witness chamber", fixture.DepartmentId,
                new DateOnly(2026, 9, 8),
                [new MaterialIssueRequestLineInput(itemId, fixture.UomId, .95m, null, null)],
                "mir-customer-create"));
        Assert.Equal("SESS-15", mir.EmployeeCode); Assert.False(string.IsNullOrWhiteSpace(mir.EmployeeName));
        Assert.False(string.IsNullOrWhiteSpace(mir.DepartmentCode));
        Assert.Equal(fixture.MachineLineId, Assert.Single(mir.Lines).CustomerPurchaseOrderLineId);
        Assert.Equal(0m, Assert.Single(mir.Lines).CustomerPoBaseQuantity);
        Assert.Equal(.05m, Assert.Single(mir.Lines).ExcessBaseQuantity);
        var spareMir = await Post<MaterialIssueRequestView>(client, "/api/v1/stores/material-issue-requests",
            new CreateMaterialIssueRequest("SALE", "SPARE_SALE", "CUSTOMER",
                null, fixture.CustomerId, null, null, "Spare customer", fixture.DepartmentId,
                new DateOnly(2026, 9, 8),
                [new MaterialIssueRequestLineInput(itemId, fixture.UomId, .95m, fixture.SpareLineId, null)],
                "mir-spare-create"));
        Assert.Null(spareMir.JobOrderId);
        Assert.Equal(.90m, Assert.Single(spareMir.Lines).CustomerPoBaseQuantity);
        Assert.Equal(.05m, Assert.Single(spareMir.Lines).ExcessBaseQuantity);
        var draftMirVersion = mir.Version;
        mir = await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{mir.Id}/submit",
            new MaterialIssueTransitionRequest(mir.Version, "Required for chamber assembly", "mir-customer-submit"));
        Assert.Equal(draftMirVersion + 1, mir.Version);
        if(storesWorkload is not null)await storesWorkload("MIR_SUBMITTED",mir.Id);
        await AssertPostStatus(client, $"/api/v1/stores/material-issue-requests/{mir.Id}/submit",
            new MaterialIssueTransitionRequest(draftMirVersion, "Stale duplicate submit", "mir-customer-stale-submit"),
            HttpStatusCode.Conflict);

        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        var recipients = await Get<MaterialIssueRecipientView[]>(client,
            "/api/v1/stores/material-issues/recipients");
        Assert.Contains(recipients, x => x.EmployeeId == engineerId && x.EmployeeCode == "SESS-05");
        var issueCommand = new CreateMaterialIssue("mir-customer-issue", engineerId,
            DateTimeOffset.UtcNow.AddDays(-2), [new MaterialIssueScan(mir.Lines.Single().Id, fixture.ItemCode, null, .95m)]);
        await AssertPostStatus(client, $"/api/v1/stores/material-issues/from-request/{mir.Id}",
            issueCommand, HttpStatusCode.Conflict);

        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        mir = await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{mir.Id}/approve",
            new MaterialIssueTransitionRequest(mir.Version, "Production approves requirement", "mir-customer-approve"));
        if(storesWorkload is not null)await storesWorkload("MIR_APPROVED",mir.Id);
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
        if(storesWorkload is not null)await storesWorkload("MIR_ISSUED",mir.Id);
        await AssertEngineerCustodyReport(client,.95m);

        var runtimeOptions = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(runtimeConnection).Options;
        await using (var notificationDb = new NexaErpDbContext(runtimeOptions))
        {
            var processor = new EfNotificationDueEventProcessor(notificationDb);
            Assert.Equal(1, await processor.RefreshAsync(DateTimeOffset.UtcNow, CancellationToken.None));
            Assert.Equal(0, await processor.RefreshAsync(DateTimeOffset.UtcNow, CancellationToken.None));
        }
        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        Assert.Equal(1, (await Get<JsonElement>(client, "/api/v1/notifications/unread-count")).GetProperty("Count").GetInt32());
        var notificationPage = await Get<PagedResponse<InAppNotificationView>>(client,
            "/api/v1/notifications?unreadOnly=true");
        var overdueNotification = Assert.Single(notificationPage.Items);
        Assert.Equal("UNUSED_MATERIAL_OVERDUE", overdueNotification.EventType);
        Assert.Equal(issue.Id, overdueNotification.SourceEntityId);
        using (var read = await client.PostAsync($"/api/v1/notifications/{overdueNotification.RecipientId}/read", null))
            Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);
        Assert.Equal(0, (await Get<JsonElement>(client, "/api/v1/notifications/unread-count")).GetProperty("Count").GetInt32());
        Assert.Equal(1, await Query(options, db => db.NotificationDeliveryAttempts.CountAsync(x =>
            x.NotificationRecipientId == overdueNotification.RecipientId && x.Channel == "IN_APP" && x.Status == "SENT")));

        var consumable = await CreateAndIssueConsumable(client, user, itemId, fixture.UomId,
            fixture.ItemCode, fixture.DepartmentId, engineerId, purchaseId, productionId, storesId, approveRace: approveRace);
        await CreateIssueAndReturnSerialized(client, options, user, serializedGrn, fixture.UomId,
            fixture.DepartmentId, engineerId, purchaseId, productionId, storesId,
            serializedRace ? job.Id : null, tdId, runtimeConnection, serializedRace ? returnRace : null, readPostgresLog, issueRace);

        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        var ownIssue = await Get<MaterialIssueView>(client,
            $"/api/v1/stores/material-issues/{issue.Id}");
        Assert.Equal(engineerId, ownIssue.IssuedToEmployeeId);
        var ownCustody = await Get<OutstandingEngineerCustodyView[]>(client,
            "/api/v1/stores/material-issues/outstanding-custody");
        Assert.Contains(ownCustody, x => x.MaterialIssueId == issue.Id && x.EmployeeId == engineerId);
        using var otherCustody = await client.GetAsync(
            $"/api/v1/stores/material-issues/outstanding-custody?employeeId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, otherCustody.StatusCode);

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
        var returnQueue=await Get<SESS.NexaERP.Application.Reporting.CompanyReportPage>(client,
            "/api/v1/reports/pending-approvals?mode=details&pageSize=1000");
        var queuedReturn=Assert.Single(returnQueue.Rows,row=>row.GetProperty("documentId").GetGuid()==materialReturn.Id);
        Assert.Equal("MATERIAL_RETURN",queuedReturn.GetProperty("documentType").GetString());
        Assert.Equal("ROLE_POOL",queuedReturn.GetProperty("assignmentKind").GetString());
        var accept = new AcceptMaterialReturn(materialReturn.Version, DateTimeOffset.UtcNow,
            "Scanner-confirmed return accepted into Stores", "material-return-accept");
        materialReturn = returnRace is null || serializedRace
            ? await Post<MaterialReturnView>(client,
                $"/api/v1/stores/material-returns/{materialReturn.Id}/accept", accept)
            : await returnRace(new(options, runtimeConnection, companyId, job.Id, issue.Lines.Single().Id,
                materialReturn.Id, accept, storesId, productionId, readPostgresLog!));
        var acceptReplay = await Post<MaterialReturnView>(client,
            $"/api/v1/stores/material-returns/{materialReturn.Id}/accept", accept);
        Assert.Equal("ACCEPTED", materialReturn.Status); Assert.False(materialReturn.Replayed);
        await AssertEngineerCustodyReport(client,.40m);
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

        var fifoBeforeAcceptance = await Query(options, db => db.FifoInventoryCostLayers
            .Where(x => x.GoodsReceiptLineId == grn.Lines.Single().Id)
            .Select(x => new { x.Id, x.UnitCost, x.LayerValue, x.QuantityReceived }).SingleAsync());
        var consumptionsBeforeAcceptance = await Query(options, db => db.FifoCostConsumptions
            .Where(x => x.FifoInventoryCostLayerId == fifoBeforeAcceptance.Id)
            .OrderBy(x => x.Id).Select(x => new { x.Id, x.Quantity, x.UnitCost, x.ConsumedValue }).ToListAsync());
        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        var fitCommand = new ConfirmComponentFitmentRequest(job.Id, issue.Lines.Single().Id, .30m,
            DateTimeOffset.UtcNow, "Operator-confirmed component fitment", null, "fitment-confirm");
        var fitment = await Post<ComponentFitmentSummary>(client,
            "/api/v1/production/component-fitments", fitCommand);
        var fitReplay = await Post<ComponentFitmentSummary>(client,
            "/api/v1/production/component-fitments", fitCommand);
        Assert.False(fitment.Replayed); Assert.True(fitReplay.Replayed); Assert.Equal(fitment.Id, fitReplay.Id);
        var provisionalActual = await Get<ActualBomView>(client,
            $"/api/v1/production/component-fitments/job-orders/{job.Id}/actual-bom");
        var provisionalEntry = Assert.Single(provisionalActual.Entries);
        Assert.Equal("PROVISIONAL_UNBILLED", provisionalEntry.ValuationStatus);
        Assert.Null(provisionalEntry.VendorBillLineId);
        Assert.Equal(0m, provisionalActual.TotalAcceptedValue);

        user.Set(accountsManagerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        var acceptLanded = new VendorBillDecisionRequest(pendingLandedBill.Version,
            "Matched bill accepted after issue and fitment", "vendor-bill-corrected-accept-0");
        var acceptedLandedBill = await Post<VendorBillView>(client,
            $"/api/v1/accounts/vendor-bills/{pendingLandedBill.Id}/accept", acceptLanded);
        Assert.Equal("ACCEPTED", acceptedLandedBill.Status);
        Assert.Equal(12m, acceptedLandedBill.TotalChargeValue);
        Assert.Equal(12m, Assert.Single(acceptedLandedBill.Lines).AllocatedChargeValue);
        var evidenceAfterAcceptance = await Query(options, async db => new
        {
            Charges = await db.VendorBillChargeAllocations.CountAsync(x => x.VendorBillLine!.VendorBillId == pendingLandedBill.Id),
            FifoAdjustments = await db.FifoLandedCostAdjustments.CountAsync(x => x.VendorBillLine!.VendorBillId == pendingLandedBill.Id),
            ActualAdjustments = await db.ActualBomValuationAdjustments.CountAsync(x => x.VendorBillLine!.VendorBillId == pendingLandedBill.Id)
        });
        var acceptedReplay = await Post<VendorBillView>(client,
            $"/api/v1/accounts/vendor-bills/{pendingLandedBill.Id}/accept", acceptLanded);
        Assert.True(acceptedReplay.Replayed);
        var evidenceAfterReplay = await Query(options, async db => new
        {
            Charges = await db.VendorBillChargeAllocations.CountAsync(x => x.VendorBillLine!.VendorBillId == pendingLandedBill.Id),
            FifoAdjustments = await db.FifoLandedCostAdjustments.CountAsync(x => x.VendorBillLine!.VendorBillId == pendingLandedBill.Id),
            ActualAdjustments = await db.ActualBomValuationAdjustments.CountAsync(x => x.VendorBillLine!.VendorBillId == pendingLandedBill.Id)
        });
        Assert.Equal(evidenceAfterAcceptance, evidenceAfterReplay);

        var acceptedAllocation = await Query(options, db => db.VendorBillCostAllocations
            .Where(x => x.VendorBillLine!.GoodsReceiptLineId == grn.Lines.Single().Id &&
                x.VendorBillLine.VendorBill!.Status == "ACCEPTED")
            .Select(x => new { x.AcceptedValue, x.AllocatedQuantity, x.VendorBillLineId })
            .SingleAsync());
        var allocatedCharge = await Query(options, db => db.VendorBillChargeAllocations
            .Where(x => x.VendorBillLineId == acceptedAllocation.VendorBillLineId)
            .SumAsync(x => x.AllocatedChargeValue));
        var landedAdjustment = await Query(options, db => db.FifoLandedCostAdjustments
            .SingleAsync(x => x.VendorBillLineId == acceptedAllocation.VendorBillLineId));
        Assert.Equal(12m, allocatedCharge);
        Assert.Equal(landedAdjustment.AllocatedChargeValue,
            landedAdjustment.ConsumedCostAdjustmentValue + landedAdjustment.RemainingStockAdjustmentValue);
        Assert.Equal(landedAdjustment.ConsumedQuantityAtAcceptance + landedAdjustment.RemainingQuantityAtAcceptance,
            fifoBeforeAcceptance.QuantityReceived);
        var fifoAfterAcceptance = await Query(options, db => db.FifoInventoryCostLayers
            .Where(x => x.Id == fifoBeforeAcceptance.Id)
            .Select(x => new { x.Id, x.UnitCost, x.LayerValue, x.QuantityReceived }).SingleAsync());
        var consumptionsAfterAcceptance = await Query(options, db => db.FifoCostConsumptions
            .Where(x => x.FifoInventoryCostLayerId == fifoBeforeAcceptance.Id)
            .OrderBy(x => x.Id).Select(x => new { x.Id, x.Quantity, x.UnitCost, x.ConsumedValue }).ToListAsync());
        Assert.Equal(fifoBeforeAcceptance, fifoAfterAcceptance);
        Assert.Equal(consumptionsBeforeAcceptance, consumptionsAfterAcceptance);

        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        var actual = await Get<ActualBomView>(client,
            $"/api/v1/production/component-fitments/job-orders/{job.Id}/actual-bom");
        Assert.Equal("LANDED_ACCEPTED", Assert.Single(actual.Entries).ValuationStatus);
        var expectedCharges = decimal.Round(allocatedCharge /
            acceptedAllocation.AllocatedQuantity * .30m, 6);
        var expectedLandedTotal = decimal.Round(Assert.Single(acceptedLandedBill.Lines).LandedUnitRate * .30m, 6);
        var expectedMaterial = expectedLandedTotal - expectedCharges;
        Assert.Equal(expectedLandedTotal, actual.TotalAcceptedValue);
        Assert.Equal(expectedMaterial, actual.TotalAcceptedMaterialValue);
        Assert.Equal(expectedCharges, actual.TotalAllocatedChargeValue);
        Assert.Equal(expectedMaterial + expectedCharges, actual.TotalAcceptedValue);
        Assert.Single(actual.Entries);
        Assert.Equal("OPERATIONAL_PRODUCTION_BOM", actual.OperationalVariance.BaselineType);
        Assert.Equal(production.CurrentRevision.Id, actual.OperationalVariance.BaselineRevisionId);
        Assert.True(actual.OperationalVariance.BaselineCostAvailable);
        Assert.Equal(90m, actual.OperationalVariance.BaselineValue);
        Assert.Equal(expectedMaterial + expectedCharges - 90m, actual.OperationalVariance.ValueVariance);
        var operationalLine = Assert.Single(actual.OperationalVariance.Lines);
        Assert.Equal(.90m, operationalLine.BaselineQuantity);
        Assert.Equal(.30m, operationalLine.ActualQuantity);
        Assert.Equal(-.60m, operationalLine.QuantityVariance);
        Assert.Equal(90m, operationalLine.BaselineValue);
        Assert.Equal(expectedMaterial + expectedCharges, operationalLine.ActualAcceptedValue);
        Assert.Equal(expectedMaterial + expectedCharges - 90m, operationalLine.ValueVariance);
        Assert.Equal("COMMERCIAL_ESTIMATED_BOM", actual.CommercialVariance.BaselineType);
        Assert.Equal(estimated.CommercialBaselineRevisionId, actual.CommercialVariance.BaselineRevisionId);
        Assert.True(actual.CommercialVariance.BaselineCostAvailable);
        Assert.Equal(90m, actual.CommercialVariance.BaselineValue);
        Assert.Equal(expectedMaterial + expectedCharges - 90m, actual.CommercialVariance.ValueVariance);
        var commercialLine = Assert.Single(actual.CommercialVariance.Lines);
        Assert.Equal(.90m, commercialLine.BaselineQuantity);
        Assert.Equal(.30m, commercialLine.ActualQuantity);
        Assert.Equal(-.60m, commercialLine.QuantityVariance);
        Assert.Equal(90m, commercialLine.BaselineValue);
        Assert.Equal(expectedMaterial + expectedCharges - 90m, commercialLine.ValueVariance);
        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        estimated = await Post<EstimatedBomView>(client,
            $"/api/v1/design/estimated-boms/{estimated.BomNumber}/revisions",
            new NewEstimatedBomRevisionRequest(estimated.Version,
                "Approved engineering revision must not replace offer baseline", "fitment-est-revision"));
        estimated = await Put<EstimatedBomView>(client,
            $"/api/v1/design/estimated-boms/{estimated.BomNumber}",
            new ReplaceEstimatedBomLinesRequest(estimated.CurrentRevision.Version,
                "Engineering now expects more material",
                [new EstimatedBomLineInput(itemId, fixture.UomId, 1.20m, "Later engineering revision")],
                "fitment-est-revision-lines"));
        estimated = await Post<EstimatedBomView>(client,
            $"/api/v1/design/estimated-boms/{estimated.BomNumber}/submit",
            new EstimatedBomActionRequest(estimated.CurrentRevision.Version,
                "Submit later engineering revision", "fitment-est-revision-submit"));
        user.Set(tdId, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
        estimated = await Post<EstimatedBomView>(client,
            $"/api/v1/design/estimated-boms/{estimated.BomNumber}/approve",
            new EstimatedBomActionRequest(estimated.CurrentRevision.Version,
                "Approve without rewriting offer baseline", "fitment-est-revision-approve"));
        var lastPurchase = await Query(options, db => db.ItemCompanyLastPurchases.AsNoTracking()
            .SingleAsync(x => x.CompanyId == Guid.Parse("70000000-0000-0000-0000-000000000001") && x.ItemId == itemId));
        Assert.NotNull(lastPurchase.LastPurchaseBillId);
        Assert.Equal(lastPurchase.LastPurchaseRate, estimated.CurrentRevision.Lines.Single().EstimatedUnitValue);
        Assert.False(estimated.CurrentRevision.Lines.Single().EstimatedUnitValueOverridden);
        var afterEngineeringRevision = await Get<ActualBomView>(client,
            $"/api/v1/production/component-fitments/job-orders/{job.Id}/actual-bom");
        Assert.NotEqual(estimated.CurrentRevision.Id, afterEngineeringRevision.CommercialVariance.BaselineRevisionId);
        Assert.Equal(estimated.CommercialBaselineRevisionId,
            afterEngineeringRevision.CommercialVariance.BaselineRevisionId);
        Assert.Equal(.90m, Assert.Single(afterEngineeringRevision.CommercialVariance.Lines).BaselineQuantity);
        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        var reverseCommand = new ReverseComponentFitmentRequest(
            "Immediate correction of operator fitment mistake", "fitment-self-reverse");
        var reversed = await Post<ComponentFitmentSummary>(client,
            $"/api/v1/production/component-fitments/{fitment.Id}/reverse", reverseCommand);
        var reversedReplay = await Post<ComponentFitmentSummary>(client,
            $"/api/v1/production/component-fitments/{fitment.Id}/reverse", reverseCommand);
        Assert.True(reversed.IsReversed); Assert.True(reversed.IsSelfReversal);
        Assert.True(reversedReplay.Replayed);
        var reversedActual = await Get<ActualBomView>(client,
            $"/api/v1/production/component-fitments/job-orders/{job.Id}/actual-bom");
        Assert.Equal(0m,reversedActual.TotalAcceptedMaterialValue);
        Assert.Equal(0m,reversedActual.TotalAllocatedChargeValue);
        Assert.Equal(0m,reversedActual.TotalAcceptedValue);
        Assert.Equal(2,reversedActual.Entries.Count);
        Assert.Equal(0m,reversedActual.Entries.Sum(x=>x.QuantityBase));
        Assert.Equal(afterEngineeringRevision.Entries.Single().Id,
            reversedActual.Entries.Single(x=>x.EntryKind=="FITMENT").Id);
        foreach(var comparison in new[]
        {
            (Before:afterEngineeringRevision.OperationalVariance,After:reversedActual.OperationalVariance),
            (Before:afterEngineeringRevision.CommercialVariance,After:reversedActual.CommercialVariance)
        })
        {
            Assert.Equal(comparison.Before.BaselineRevisionId,comparison.After.BaselineRevisionId);
            Assert.Equal(comparison.Before.BaselineValue,comparison.After.BaselineValue);
            Assert.Equal(0m,comparison.After.ActualAcceptedValue);
            Assert.Equal(-comparison.After.BaselineValue,comparison.After.ValueVariance);
            Assert.All(comparison.After.Lines,line=>
            {
                Assert.Equal(0m,line.ActualQuantity);
                Assert.Equal(0m,line.ActualAcceptedValue);
            });
        }
        var reversalConsumptions = await Query(options, db => db.FifoCostConsumptions
            .Where(x=>x.FifoInventoryCostLayerId==fifoBeforeAcceptance.Id).OrderBy(x=>x.Id)
            .Select(x=>new{x.Id,x.Quantity,x.UnitCost,x.ConsumedValue}).ToListAsync());
        Assert.Equal(consumptionsAfterAcceptance,reversalConsumptions);
        var custodyAfterReversal=await Query(options,async db=>
        {
            var assignment=await db.MaterialIssueLines.Where(x=>x.Id==issue.Lines.Single().Id)
                .Select(x=>x.ToCustodyAssignmentId).SingleAsync();
            return await db.StockMovements.Where(x=>x.CompanyId==Guid.Parse("70000000-0000-0000-0000-000000000001")
                &&x.CustodyAssignmentId==assignment).SumAsync(x=>x.QuantityIn-x.QuantityOut);
        });
        Assert.Equal(.35m,custodyAfterReversal);
        var reportEvidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item15");
        Directory.CreateDirectory(reportEvidence);
        await File.WriteAllTextAsync(Path.Combine(reportEvidence,"fitment-reversal-machine-cost.json"),
            JsonSerializer.Serialize(new{Before=afterEngineeringRevision,After=reversedActual,custodyAfterReversal,
                GrossConsumptionsBefore=consumptionsAfterAcceptance,GrossConsumptionsAfter=reversalConsumptions,
                Meaning="Fitment reversal negates machine cost and restores engineer custody; original issue consumption history remains."},
                new JsonSerializerOptions{WriteIndented=true}));
        var reverified = await Post<ComponentFitmentSummary>(client,
            "/api/v1/production/component-fitments",
            fitCommand with { ReverifiesFitmentId = fitment.Id, IdempotencyKey = "fitment-reverify" });
        Assert.Equal(fitment.Id, reverified.ReverifiesFitmentId);
        var finalActual = await Get<ActualBomView>(client,
            $"/api/v1/production/component-fitments/job-orders/{job.Id}/actual-bom");
        Assert.Equal(expectedMaterial, finalActual.TotalAcceptedMaterialValue);
        Assert.Equal(expectedCharges, finalActual.TotalAllocatedChargeValue);
        Assert.Equal(expectedMaterial + expectedCharges, finalActual.TotalAcceptedValue);
        Assert.Equal(3, finalActual.Entries.Count);
        user.Set(qcId, "SESS-33", Rev869ARoleCodes.QcManager);
        var fatBlocked = await Post<FatReconciliationView>(client,
            $"/api/v1/production/job-orders/{job.Id}/fat-readiness/reconcile",
            new ReconcileJobOrderFatRequest("Initial FAT custody reconciliation", "fat-reconcile-blocked"));
        Assert.Equal("BLOCKED", fatBlocked.Result); Assert.Equal(.05m, fatBlocked.UnexplainedQuantityBase);
        user.Set(engineerId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER",
            "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        var explanationRequest = new CreateFatCustodyExplanationRequest(issue.Lines.Single().Id, .05m,
            "SCRAPPED", "Cut remnant scrapped with engineer accountability", "fat-explain-scrap");
        var explanation = await Post<FatCustodyExplanationView>(client,
            $"/api/v1/production/job-orders/{job.Id}/fat-readiness/custody-explanations", explanationRequest);
        var explanationReplay = await Post<FatCustodyExplanationView>(client,
            $"/api/v1/production/job-orders/{job.Id}/fat-readiness/custody-explanations", explanationRequest);
        Assert.False(explanation.Replayed); Assert.True(explanationReplay.Replayed); Assert.Equal(explanation.Id, explanationReplay.Id);
        user.Set(qcId, "SESS-33", Rev869ARoleCodes.QcManager);
        var fatReady = await Post<FatReconciliationView>(client,
            $"/api/v1/production/job-orders/{job.Id}/fat-readiness/reconcile",
            new ReconcileJobOrderFatRequest("All issue custody fitted, returned or explained", "fat-reconcile-ready"));
        var fatReadyReplay = await Post<FatReconciliationView>(client,
            $"/api/v1/production/job-orders/{job.Id}/fat-readiness/reconcile",
            new ReconcileJobOrderFatRequest("All issue custody fitted, returned or explained", "fat-reconcile-ready"));
        Assert.Equal("READY", fatReady.Result); Assert.Equal(0m, fatReady.UnexplainedQuantityBase);
        Assert.True(fatReadyReplay.Replayed); Assert.Equal(fatReady.Id, fatReadyReplay.Id);
        var readiness = await Get<JobOrderFatReadinessView>(client,
            $"/api/v1/production/job-orders/{job.Id}/fat-readiness");
        Assert.Equal("READY", readiness.FatReadinessStatus); Assert.Equal(fatReady.Id, readiness.LatestFatReconciliationId);
        await using var evidence = new NexaErpDbContext(options);
        Assert.Equal(1, await evidence.JobOrderFatCustodyExplanations.CountAsync());
        Assert.Equal(2, await evidence.JobOrderFatReconciliations.CountAsync());
        Assert.Equal(serializedRace ? 4 : 2, await evidence.JobOrderFatReconciliationLines.CountAsync());
        if (serializedRace)
        {
            var serialReconciliations = await evidence.JobOrderFatReconciliationLines
                .Where(row => row.MaterialIssueLine!.InventorySerialId != null).ToArrayAsync();
            Assert.Equal(2, serialReconciliations.Length);
            Assert.All(serialReconciliations, row =>
            {
                Assert.Equal(1m, row.IssuedQuantityBase);
                Assert.Equal(1m, row.ReturnedQuantityBase);
                Assert.Equal(0m, row.FittedQuantityBase);
                Assert.Equal(0m, row.UnexplainedQuantityBase);
            });
        }
        Assert.Equal(1, await evidence.JobOrderFatReconciliations.CountAsync(x => x.Result == "READY"));
        Assert.Equal(1, await evidence.JobOrderFatReconciliations.CountAsync(x => x.Result == "BLOCKED"));
        Assert.Equal(2, await evidence.ComponentFitments.CountAsync());
        Assert.Equal(1, await evidence.ComponentFitmentReversals.CountAsync());
        Assert.Equal(1, await evidence.ComponentFitmentReversals.CountAsync(x => x.IsSelfReversal));
        Assert.Single(await evidence.VendorBillCharges.ToListAsync());
        Assert.Single(await evidence.VendorBillChargeAllocations.ToListAsync());
        Assert.Equal(4, await evidence.FifoLandedCostAdjustments.CountAsync());
        Assert.Single(await evidence.ActualBomValuationAdjustments.ToListAsync());
        Assert.Equal(1, await evidence.ActualBoms.CountAsync());
        Assert.Equal(3, await evidence.ActualBomEntries.CountAsync());
        Assert.Equal(2, await evidence.StockPostingBatches.CountAsync(x => x.PostingKind == "FITMENT_CONSUMPTION"));
        Assert.Equal(1, await evidence.StockPostingBatches.CountAsync(x => x.PostingKind == "REVERSAL" &&
            x.ReversesPostingBatch != null && x.ReversesPostingBatch.PostingKind == "FITMENT_CONSUMPTION"));
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
        var recordedIssues = await evidence.MaterialIssues.AsNoTracking()
            .Include(x => x.ResolvedRoleAssignment)!.ThenInclude(x => x!.Role)
            .ToDictionaryAsync(x => x.Id);
        Assert.All(await evidence.AuditLogs.Where(x => x.Action == "MaterialIssue.Issue").ToListAsync(), x =>
        {
            var recorded = recordedIssues[Guid.Parse(x.EntityId)];
            Assert.Equal(recorded.CreatedBy, x.UserLoginId);
            Assert.Equal(recorded.ActorRoleCode, x.ActorRoleCode);
            Assert.Equal(recorded.ResolvedRoleAssignmentId, x.ResolvedRoleAssignmentId);
            Assert.Equal(recorded.ResolvedRoleAssignmentType, x.ResolvedRoleAssignmentType);
            Assert.NotNull(recorded.ResolvedRoleAssignment);
            Assert.Equal(recorded.IssuedByEmployeeId, recorded.ResolvedRoleAssignment.EmployeeId);
            Assert.Equal(recorded.ActorRoleCode, recorded.ResolvedRoleAssignment.Role!.Code);
            Assert.Equal(recorded.ResolvedRoleAssignmentType, recorded.ResolvedRoleAssignment.AssignmentType);
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
        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        var outstanding = await Get<OutstandingEngineerCustodyView[]>(client,
            $"/api/v1/stores/material-issues/outstanding-custody?employeeId={engineerId}");
        Assert.Equal(2, outstanding.Length); Assert.All(outstanding, x => Assert.Equal(engineerId, x.EmployeeId));
        Assert.Equal(.05m, outstanding.Single(x => x.MaterialIssueId == issue.Id).QuantityBase);
    }

    private static async Task CreateIssueAndReturnSerialized(HttpClient client,
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user, GoodsReceiptResult grn,
        Guid uomId, Guid departmentId, Guid engineerId, Guid purchaseId, Guid productionId, Guid storesId,
        Guid? jobOrderId = null, Guid? tdId = null, string? runtimeConnection = null,
        Func<ReturnFitmentRaceContext, Task<MaterialReturnView>>? returnRace = null, Func<string>? readPostgresLog = null,
        Func<MaterialIssueRequestView, CreateMaterialIssue, Task<MaterialIssueView>>? issueRace = null)
    {
        var receivedLine = grn.Lines.Single();
        var serial = Assert.Single(receivedLine.Serials);
        Assert.NotNull(serial.InventorySerialId);
        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.StoresExecutive);
        var request = await Post<MaterialIssueRequestView>(client, "/api/v1/stores/material-issue-requests",
            new CreateMaterialIssueRequest("FACTORY_ASSEMBLY", jobOrderId.HasValue ? "CHAMBER_MANUFACTURE" : "CONSUMABLE_OFFICE",
                jobOrderId.HasValue ? "JOB_ORDER" : "DEPARTMENT",
                jobOrderId, null, null, jobOrderId.HasValue ? null : departmentId, "Serialized custody witness", departmentId,
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
        if (jobOrderId.HasValue)
        {
            Assert.Equal(.90m, request.Lines.Single().EstimatedBomBaseQuantity);
            Assert.Equal(.10m, request.Lines.Single().ExcessBaseQuantity);
            user.Set(tdId!.Value, "SESS-01", Rev869ARoleCodes.TechnicalDirector);
            request = await Post<MaterialIssueRequestView>(client,
                $"/api/v1/stores/material-issue-excess/{request.Lines.Single().Id}/decision",
                new MaterialIssueExcessDecisionRequest("APPROVED",
                    "One serialized component approved for the job-backed race witness", "mir-serialized-excess"));
        }
        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        var availableSerials = await Get<AvailableMaterialIssueSerialView[]>(client,
            $"/api/v1/stores/material-issues/request-lines/{request.Lines.Single().Id}/available-serials");
        var availableSerial = Assert.Single(availableSerials,
            x => x.InventorySerialId == serial.InventorySerialId);
        Assert.Equal(serial.StoredSerialNumber, availableSerial.StoredSerialNumber);
        Assert.Equal(1m, availableSerial.AvailableQuantity);
        var command = new CreateMaterialIssue("mir-serialized-issue", engineerId, DateTimeOffset.UtcNow,
            [new MaterialIssueScan(request.Lines.Single().Id, serial.StoredSerialNumber,
                serial.InventorySerialId, 1m)]);
        var issue = issueRace is null
            ? await Post<MaterialIssueView>(client,
                $"/api/v1/stores/material-issues/from-request/{request.Id}", command)
            : await issueRace(request, command);

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
        var serialAccept = new AcceptMaterialReturn(materialReturn.Version, DateTimeOffset.UtcNow,
            "Exact issued serial returned to Stores", "material-return-serial-accept");
        Assert.Equal(jobOrderId, issue.JobOrderId);
        materialReturn = returnRace is null
            ? await Post<MaterialReturnView>(client,
                $"/api/v1/stores/material-returns/{materialReturn.Id}/accept", serialAccept)
            : await returnRace(new(options, runtimeConnection!, Guid.Parse("70000000-0000-0000-0000-000000000001"),
                jobOrderId!.Value, issue.Lines.Single().Id, materialReturn.Id, serialAccept, storesId, productionId,
                readPostgresLog!, 1m, "serial-return-fitment", serial.InventorySerialId));

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
        Guid purchaseId, Guid productionId, Guid storesId, string prefix = "mir-consumable",
        Func<MaterialIssueRequestView, Task<MaterialIssueRequestView>>? approveRace = null)
    {
        user.Set(purchaseId, "SESS-15", Rev869ARoleCodes.PurchaseManager,
            Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.StoresExecutive);
        var mir = await Post<MaterialIssueRequestView>(client, "/api/v1/stores/material-issue-requests",
            new CreateMaterialIssueRequest("FACTORY_ASSEMBLY", "CONSUMABLE_OFFICE", "DEPARTMENT",
                null, null, null, departmentId, "Factory consumable custody", departmentId,
                new DateOnly(2026, 9, 8), [new MaterialIssueRequestLineInput(itemId, uomId, .05m, null, null)],
                $"{prefix}-create"));
        mir = await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{mir.Id}/submit",
            new MaterialIssueTransitionRequest(mir.Version, "Cutting wheel custody", $"{prefix}-submit"));
        user.Set(productionId, "SESS-25", "PRODUCTION_MANAGER");
        mir = approveRace is null
            ? await Post<MaterialIssueRequestView>(client, $"/api/v1/stores/material-issue-requests/{mir.Id}/approve",
                new MaterialIssueTransitionRequest(mir.Version, "Consumable custody approved", $"{prefix}-approve"))
            : await approveRace(mir);
        user.Set(storesId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        return await Post<MaterialIssueView>(client, $"/api/v1/stores/material-issues/from-request/{mir.Id}",
            new CreateMaterialIssue($"{prefix}-issue", engineerId, DateTimeOffset.UtcNow,
                [new MaterialIssueScan(mir.Lines.Single().Id, itemCode, null, .05m)]));
    }

    private static async Task AssertVendorBillRuntimeTableDmlRefused(string connectionString)
    {
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var readable = new Npgsql.NpgsqlCommand("SELECT count(*) FROM advance.item_company_last_purchases", connection))
            _ = await readable.ExecuteScalarAsync();
        foreach (var sql in new[]
        {
            "SELECT count(*) FROM advance.vendor_bills",
            "SELECT count(*) FROM advance.fifo_inventory_cost_layers",
            "DELETE FROM advance.vendor_bills WHERE false",
            "DELETE FROM advance.fifo_inventory_cost_layers WHERE false",
            "INSERT INTO advance.item_company_last_purchases DEFAULT VALUES",
            "UPDATE advance.item_company_last_purchases SET \"Version\"=\"Version\"",
            "DELETE FROM advance.item_company_last_purchases"
        })
        {
            await using var command = new Npgsql.NpgsqlCommand(sql, connection);
            var error = await Record.ExceptionAsync(() => command.ExecuteNonQueryAsync());
            Assert.True(error is not null, $"Expected runtime DML refusal for: {sql}");
            var postgres = Assert.IsType<Npgsql.PostgresException>(error);
            Assert.True(postgres.SqlState == Npgsql.PostgresErrorCodes.InsufficientPrivilege,
                $"Expected 42501 for {sql}, received {postgres.SqlState}: {postgres.MessageText}");
        }
        foreach (var table in new[]
        {
            "vendor_bill_charges", "vendor_bill_charge_allocations",
            "fifo_landed_cost_adjustments", "actual_bom_valuation_adjustments",
            "vendor_advances", "vendor_advance_reversals",
            "vendor_advance_adjustments", "vendor_advance_adjustment_restorations",
            "vendor_payments", "vendor_payment_allocations"
        })
        {
            foreach (var sql in new[]
            {
                $"SELECT count(*) FROM advance.{table}",
                $"INSERT INTO advance.{table} DEFAULT VALUES",
                $"UPDATE advance.{table} SET \"Id\"=\"Id\" WHERE false",
                $"DELETE FROM advance.{table} WHERE false"
            })
            {
                await using var command = new Npgsql.NpgsqlCommand(sql, connection);
                var error = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => command.ExecuteNonQueryAsync());
                Assert.Equal(Npgsql.PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
            }
        }
    }

    private static async Task AssertFitmentRuntimeTableDmlRefused(string connectionString)
    {
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var table in new[]
        {
            "component_fitments", "component_fitment_reversals", "actual_boms", "actual_bom_entries"
        })
        {
            foreach (var sql in new[]
            {
                $"INSERT INTO advance.{table} DEFAULT VALUES",
                $"UPDATE advance.{table} SET \"Id\" = \"Id\" WHERE false",
                $"DELETE FROM advance.{table} WHERE false"
            })
            {
                await using var command = new Npgsql.NpgsqlCommand(sql, connection);
                var error = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => command.ExecuteNonQueryAsync());
                Assert.Equal(Npgsql.PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
            }
        }
    }
    private static async Task AssertFatRuntimeTableDmlRefused(string connectionString)
    {
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var table in new[]
        {
            "job_order_fat_custody_explanations", "job_order_fat_reconciliations",
            "job_order_fat_reconciliation_lines"
        })
        {
            foreach (var sql in new[]
            {
                $"INSERT INTO advance.{table} DEFAULT VALUES",
                $"UPDATE advance.{table} SET \"Id\" = \"Id\" WHERE false",
                $"DELETE FROM advance.{table} WHERE false"
            })
            {
                await using var command = new Npgsql.NpgsqlCommand(sql, connection);
                var error = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => command.ExecuteNonQueryAsync());
                Assert.Equal(Npgsql.PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
            }
        }
    }
    private static async Task<VendorBillView> RunVendorBillWitness(HttpClient client,
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user,
        IReadOnlyList<GoodsReceiptResult> grns, Guid accountsManagerId, Guid accountsSupportId,
        Func<VendorBillView, Task<VendorBillView>>? acceptRace = null,
        Func<RecordVendorPaymentRequest, Task<PaymentRaceResult>>? paymentRace = null,
        Func<string, Task>? obligations = null)
    {
        var expected = new List<(decimal UnitRate, decimal Payable)>();
        foreach (var grn in grns)
            expected.Add(await Query(options, db => db.PurchaseOrderLines.Where(x =>
                x.Id == grn.Lines.Single().PurchaseOrderLineId)
                .Select(x => new ValueTuple<decimal, decimal>(x.UnitRate, x.TotalPayableValue)).SingleAsync()));
        if(obligations is not null) await obligations("RECEIVED");

        user.Set(accountsSupportId, "SESS-41", "ACCOUNTS_ASSISTANT");
        await AssertPostStatus(client, "/api/v1/accounts/vendor-financial-evidence/advances",
            new RecordVendorAdvanceRequest(grns[1].PurchaseOrderId, DateOnly.FromDateTime(DateTime.UtcNow),
                100m, "INR", "SUPPORT-REFUSED", "evidence/support-refused", "advance-support-refused"),
            HttpStatusCode.Forbidden);

        user.Set(accountsManagerId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        var eligibleAdvanceOrders = await Get<VendorAdvancePurchaseOrderOption[]>(client,
            "/api/v1/accounts/vendor-financial-evidence/advance-purchase-orders");
        Assert.Contains(eligibleAdvanceOrders, x => x.PurchaseOrderId == grns[1].PurchaseOrderId
            && x.VendorId == grns[1].VendorId && x.AvailableAdvanceAmount > 0);
        var firstAdvance = await Post<VendorAdvanceView>(client,
            "/api/v1/accounts/vendor-financial-evidence/advances",
            new RecordVendorAdvanceRequest(grns[1].PurchaseOrderId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2),
                100m, "INR", "UTR-ADV-001", "evidence/advance-001", "advance-record-001"));
        var secondAdvance = await Post<VendorAdvanceView>(client,
            "/api/v1/accounts/vendor-financial-evidence/advances",
            new RecordVendorAdvanceRequest(grns[1].PurchaseOrderId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
                150m, "INR", "UTR-ADV-002", "evidence/advance-002", "advance-record-002"));
        Assert.Equal(100m, firstAdvance.OutstandingAmount);
        Assert.Equal(150m, secondAdvance.OutstandingAmount);
        if(obligations is not null) await obligations("ADVANCES");
        var firstAdvanceReplay = await Post<VendorAdvanceView>(client,
            "/api/v1/accounts/vendor-financial-evidence/advances",
            new RecordVendorAdvanceRequest(grns[1].PurchaseOrderId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2),
                100m, "INR", "UTR-ADV-001", "evidence/advance-001", "advance-record-001"));
        Assert.True(firstAdvanceReplay.Replayed);
        await AssertPostStatusContains(client,
            "/api/v1/accounts/vendor-financial-evidence/advances",
            new RecordVendorAdvanceRequest(grns[1].PurchaseOrderId, DateOnly.FromDateTime(DateTime.UtcNow),
                expected[1].Payable, "INR", "UTR-ADV-CAP", "evidence/advance-cap", "advance-cap-refused"),
            HttpStatusCode.Conflict, "exceeds Purchase Order value");

        var reversibleAdvance = await Post<VendorAdvanceView>(client,
            "/api/v1/accounts/vendor-financial-evidence/advances",
            new RecordVendorAdvanceRequest(grns[0].PurchaseOrderId, DateOnly.FromDateTime(DateTime.UtcNow),
                10m, "INR", "UTR-ADV-REV", "evidence/advance-reverse", "advance-reverse-record"));
        if(obligations is not null) await obligations("REVERSIBLE_ADVANCE");
        var advanceReversal = new ReverseVendorAdvanceRequest(
            "Payment returned by vendor", "advance-reverse-command");
        var reversedAdvance = await Post<VendorAdvanceView>(client,
            $"/api/v1/accounts/vendor-financial-evidence/advances/{reversibleAdvance.Id}/reverse",
            advanceReversal);
        Assert.True(reversedAdvance.IsReversed);
        var reversedAdvanceReplay = await Post<VendorAdvanceView>(client,
            $"/api/v1/accounts/vendor-financial-evidence/advances/{reversibleAdvance.Id}/reverse",
            advanceReversal);
        Assert.True(reversedAdvanceReplay.Replayed);
        if(obligations is not null) await obligations("ADVANCE_REVERSED");

        var acceptedBills = new List<VendorBillView>();
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
        await AssertPostStatusContains(client,
            "/api/v1/accounts/vendor-financial-evidence/payments",
            new RecordVendorPaymentRequest(mismatched.VendorId, DateOnly.FromDateTime(DateTime.UtcNow),
                1m, "INR", "UTR-DRAFT-REFUSED", "evidence/draft-refused",
                [new VendorPaymentAllocationInput(mismatched.Id, 1m)], "payment-before-acceptance-refused"),
            HttpStatusCode.Conflict, "cannot be paid before acceptance");

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
        var correctedFirst = await Post<VendorBillView>(client,
            $"/api/v1/accounts/vendor-bills/from-grn/{grns[0].Id}",
            new CreateVendorBillRequest(grns[0].VendorBillNumber, grns[0].VendorBillDate,
                [new(grns[0].Lines.Single().Id, grns[0].Lines.Single().ReceivedQuantity,
                    expected[0].UnitRate, expected[0].Payable, 2m)], "vendor-bill-corrected-0",
                [new VendorBillChargeInput("FREIGHT", 12m)]));
        Assert.Equal("DRAFT", correctedFirst.Status);
        Assert.Equal(12m, correctedFirst.TotalChargeValue);
        Assert.Equal(expected[0].Payable + 12m, correctedFirst.TotalLandedValue);
        Assert.Equal("GROSS_WEIGHT", Assert.Single(correctedFirst.Charges).AllocationBasis);

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
            var accepted = acceptRace is not null && index == 2 ? await acceptRace(bill)
                : await Post<VendorBillView>(client,
                    $"/api/v1/accounts/vendor-bills/{bill.Id}/accept", decision);
            Assert.Equal("ACCEPTED", accepted.Status);
            if(obligations is not null) await obligations($"BILL_ACCEPTED_{index}");
            Assert.Equal(accepted.Id, await Query(options, db => db.ItemCompanyLastPurchases
                .Where(x => x.CompanyId == Guid.Parse("70000000-0000-0000-0000-000000000001") && x.ItemId == grn.Lines.Single().ItemId)
                .Select(x => x.LastPurchaseBillId).SingleAsync()));
            var replay = await Post<VendorBillView>(client,
                $"/api/v1/accounts/vendor-bills/{bill.Id}/accept", decision);
            Assert.True(replay.Replayed);
            var finalAccepted = accepted;
            if (index == 1)
            {
                var reversalRequest = new VendorBillDecisionRequest(accepted.Version,
                    "Accepted bill corrected by governed reversal", "vendor-bill-reverse-1");
                var reversed = await Post<VendorBillView>(client,
                    $"/api/v1/accounts/vendor-bills/{accepted.Id}/reverse", reversalRequest);
                Assert.Equal("REVERSED", reversed.Status);
                Assert.NotNull(reversed.ReversedAt);
                Assert.Null(await Query(options, db => db.ItemCompanyLastPurchases
                    .Where(x => x.CompanyId == Guid.Parse("70000000-0000-0000-0000-000000000001") && x.ItemId == grn.Lines.Single().ItemId)
                    .Select(x => x.LastPurchaseBillId).SingleAsync()));
                var reversalReplay = await Post<VendorBillView>(client,
                    $"/api/v1/accounts/vendor-bills/{accepted.Id}/reverse", reversalRequest);
                Assert.True(reversalReplay.Replayed);
                var restoredAdvances = await Get<VendorAdvancePage>(client,
                    $"/api/v1/accounts/vendor-financial-evidence/advances?purchaseOrderId={grn.PurchaseOrderId}&outstandingOnly=false");
                Assert.Equal(250m, restoredAdvances.Items.Sum(x => x.OutstandingAmount));
                if(obligations is not null) await obligations("BILL_REVERSED_1");

                var replacement = await Post<VendorBillView>(client,
                    $"/api/v1/accounts/vendor-bills/from-grn/{grn.Id}",
                    new CreateVendorBillRequest(grn.VendorBillNumber, grn.VendorBillDate,
                        [new(grn.Lines.Single().Id, grn.Lines.Single().ReceivedQuantity,
                            expected[index].UnitRate, expected[index].Payable)], "vendor-bill-reentry-1"));
                replacement = await Post<VendorBillView>(client,
                    $"/api/v1/accounts/vendor-bills/{replacement.Id}/accept",
                    new VendorBillDecisionRequest(replacement.Version,
                        "Corrected bill re-entered after reversal", "vendor-bill-reentry-accept-1"));
                if(obligations is not null) await obligations("BILL_REENTERED_1");
                Assert.Equal("ACCEPTED", replacement.Status);
                Assert.Equal(replacement.Id, await Query(options, db => db.ItemCompanyLastPurchases
                    .Where(x => x.CompanyId == Guid.Parse("70000000-0000-0000-0000-000000000001") && x.ItemId == grn.Lines.Single().ItemId)
                    .Select(x => x.LastPurchaseBillId).SingleAsync()));
                finalAccepted = replacement;
            }
            acceptedBills.Add(finalAccepted);
        }

        var advances = await Get<VendorAdvancePage>(client,
            $"/api/v1/accounts/vendor-financial-evidence/advances?purchaseOrderId={grns[1].PurchaseOrderId}&outstandingOnly=false");
        Assert.Equal(2, advances.Total);
        Assert.All(advances.Items, x => Assert.Equal(0m, x.OutstandingAmount));
        Assert.Equal(100m, advances.Items.Single(x => x.Id == firstAdvance.Id).AdjustedAmount);
        Assert.Equal(150m, advances.Items.Single(x => x.Id == secondAdvance.Id).AdjustedAmount);

        var payableBills = acceptedBills.Where(x => x.VendorId == acceptedBills[0].VendorId).Take(2).ToArray();
        Assert.NotEmpty(payableBills);
        var allocations = payableBills.Select(x => new VendorPaymentAllocationInput(x.Id, 1m)).ToArray();
        var paymentRequest = new RecordVendorPaymentRequest(
            payableBills[0].VendorId, DateOnly.FromDateTime(DateTime.UtcNow), allocations.Sum(x => x.Amount),
            "INR", "UTR-PAY-001", "evidence/payment-001", allocations, "vendor-payment-record-001");
        VendorPaymentView payment;
        if (paymentRace is null)
            payment = await Post<VendorPaymentView>(client,
                "/api/v1/accounts/vendor-financial-evidence/payments", paymentRequest);
        else
        {
            var outcome = await paymentRace(paymentRequest);
            payment = outcome.Payment;
            paymentRequest = outcome.Command;
        }
        Assert.Equal(allocations.Length, payment.Allocations.Count);
        var paymentReplay = await Post<VendorPaymentView>(client,
            "/api/v1/accounts/vendor-financial-evidence/payments", paymentRequest);
        Assert.True(paymentReplay.Replayed);
        var positions = await Get<VendorPositionView[]>(client,
            "/api/v1/accounts/vendor-financial-evidence/vendor-positions");
        if (paymentRace is null) Assert.Contains(positions, x => x.VendorId == payment.VendorId);
        else Assert.DoesNotContain(positions, x => x.VendorId == payment.VendorId); // Full settlement leaves no open position.

        await using var evidence = new NexaErpDbContext(options);
        Assert.Equal(5, await evidence.VendorBills.CountAsync());
        Assert.Equal(2, await evidence.VendorBills.CountAsync(x => x.Status == "ACCEPTED"));
        Assert.Equal(3, await evidence.VendorBillCostAllocations.CountAsync());
        Assert.Equal(3, await evidence.VendorBillHistories.CountAsync(x => x.Action == "ACCEPTED"));
        Assert.Single(await evidence.VendorBillHistories.Where(x => x.Action == "REVERSED").ToListAsync());
        Assert.Equal(3, await evidence.Database.SqlQueryRaw<int>(
            @"SELECT count(*)::integer AS ""Value"" FROM advance.vendor_advances").SingleAsync());
        Assert.Equal(1, await evidence.Database.SqlQueryRaw<int>(
            @"SELECT count(*)::integer AS ""Value"" FROM advance.vendor_advance_reversals").SingleAsync());
        Assert.Equal(4, await evidence.Database.SqlQueryRaw<int>(
            @"SELECT count(*)::integer AS ""Value"" FROM advance.vendor_advance_adjustments").SingleAsync());
        Assert.Equal(2, await evidence.Database.SqlQueryRaw<int>(
            @"SELECT count(*)::integer AS ""Value"" FROM advance.vendor_advance_adjustment_restorations").SingleAsync());
        Assert.Equal(1, await evidence.Database.SqlQueryRaw<int>(
            @"SELECT count(*)::integer AS ""Value"" FROM advance.vendor_payments").SingleAsync());
        Assert.Equal(allocations.Length, await evidence.Database.SqlQueryRaw<int>(
            @"SELECT count(*)::integer AS ""Value"" FROM advance.vendor_payment_allocations").SingleAsync());
        return correctedFirst;
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
        var snapshots = await db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == id)
            .Select(x => x.TaxRuleSnapshotJson).ToListAsync();
        Assert.NotEmpty(snapshots);
        foreach (var json in snapshots)
        {
            using var captured = JsonDocument.Parse(json);
            var taxId = captured.RootElement.GetProperty("id").GetGuid();
            var agreedRule = await db.TaxGstSettings.AsNoTracking().SingleAsync(x => x.Id == taxId);
            Assert.Equal(agreedRule.ItcEligibility, captured.RootElement.GetProperty("itcEligibility").GetString());
            var percent = captured.RootElement.GetProperty("recoverableTaxPercent");
            Assert.Equal(agreedRule.RecoverableTaxPercent,
                percent.ValueKind == JsonValueKind.Null ? (decimal?)null : percent.GetDecimal());
        }
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
        if (body is CreatePurchaseRequisitionRequest && string.IsNullOrWhiteSpace(key) && !client.DefaultRequestHeaders.Contains("Idempotency-Key"))
            key = "test-pr-create-" + Guid.NewGuid().ToString("N");
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
    private static string WitnessReportPath(string path)
    {
        // The test ledger uses the local PostgreSQL business date; bill timestamps
        // use UTC. Select an explicit cutoff containing both, rather than testing
        // a UTC default across a local-midnight boundary.
        var local=DateOnly.FromDateTime(DateTime.Today);
        var utc=DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff=local>utc?local:utc;
        return path+(path.Contains('?')?"&":"?")+"toDate="+cutoff.ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
    }

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
            Set("NexaErp__ExpectedDatabase", new Npgsql.NpgsqlConnectionStringBuilder(installerConnection).Database
                ?? throw new ArgumentException("Disposable installer connection must name its database.", nameof(installerConnection)));
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
        public IReadOnlyList<string> QcMutationRoutes => QcReachabilityWitness.Routes((Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)app);

        public static async Task<PurchaseFlowHost> StartAsync(
            string connectionString,
            TaxWorkflowUser user,
            bool useRealPagePermissions = false,
            bool useRealOperationalScopes = false,
            bool denyLifecycleScope = false, Func<Microsoft.AspNetCore.Http.HttpContext, TaxWorkflowUser>? requestUser = null,
            Action<ICurrentUser, bool>? observeRequest = null,
            Func<Microsoft.AspNetCore.Http.HttpContext, ICurrentUser, Action>? captureRequest = null)
        {
            var port = FreePurchaseFlowPort();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NexaErp"] = connectionString,
                ["Reporting:DefaultTimeZone"] = "Asia/Kolkata",
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
            if (requestUser is null) builder.Services.AddScoped<ICurrentUser>(_ => user.ForRequest());
            else
            {
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddScoped<ICurrentUser>(services => requestUser(services.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext!));
            }
            if (denyLifecycleScope)
                builder.Services.AddSingleton<IRecordScopeAuthorizer, PurchaseFlowSecondScopeDenial>();
            else if (!useRealOperationalScopes)
                builder.Services.AddSingleton<IRecordScopeAuthorizer, PurchaseFlowAllowingScope>();
            if (!useRealPagePermissions)
                builder.Services.AddSingleton<IPagePermissionService, PurchaseFlowAllowingPermissions>();
            var app = builder.Build();
            app.UseMiddleware<StandardErrorEnvelopeMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            if (captureRequest is not null)
                app.Use(async (context, next) =>
                {
                    var completed = captureRequest(context, context.RequestServices.GetRequiredService<ICurrentUser>());
                    await next(context);
                    completed();
                });
            if (observeRequest is not null)
                app.Use(async (context, next) =>
                {
                    var current = context.RequestServices.GetRequiredService<ICurrentUser>();
                    observeRequest(current, true);
                    try { await next(context); }
                    finally { observeRequest(current, false); }
                });
            app.MapRev869AConfigurationEndpoints();
            app.MapReferenceMasterEndpoints();
            app.MapInventoryEndpoints();
            app.MapMasterDataTransferEndpoints();
            app.MapEmployeeEndpoints();
            app.MapPurchaseRequisitionEndpoints();
            app.MapRev869BPurchaseEndpoints();
            app.MapStoresGateEntryEndpoints();
            app.MapStoresGoodsReceiptEndpoints();
            app.MapOpeningStockEndpoints();
            app.MapQcEndpoints();
            app.MapEstimatedBomEndpoints();
            app.MapProductionEngineeringEndpoints();
            app.MapJobOrderEndpoints();
            app.MapMaterialIssueEndpoints();
            app.MapCompanyReportEndpoints();
            app.MapPurchaseWorkloadEndpoints();
            app.MapPurchaseSpendingEndpoints();
            app.MapPurchaseObligationsEndpoints();
            app.MapPurchaseOpenOrdersEndpoints();
            app.MapStoresWorkloadEndpoints();
            app.MapStoresQcStockEndpoints();
            app.MapNotificationEndpoints();
            app.MapVendorBillEndpoints();
            app.MapSupplierInvoiceEndpoints();
            app.MapMachineDeliveryEndpoints();
            app.MapIntercompanyEndpoints();
            app.MapVendorFinancialEvidenceEndpoints();
            app.MapFitmentActualBomEndpoints();
            app.MapJobOrderFatReadinessEndpoints();
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

    private sealed class PurchaseFlowSecondScopeDenial : IRecordScopeAuthorizer
    {
        private int calls;
        public Task<RecordScopeDecision> AuthorizeAnyAsync(Guid employeeId, string roleCode, string organizationId,
            DateOnly onDate, CancellationToken ct) => Task.FromResult(Interlocked.Increment(ref calls) == 1
                ? new RecordScopeDecision(true, "Endpoint scope admitted")
                : new RecordScopeDecision(false, "Lifecycle scope deliberately refused"));
        public Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target,
            DateOnly onDate, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(false,
                "Lifecycle record scope deliberately refused"));
    }
    private sealed class PurchaseFlowAllowingScope : IRecordScopeAuthorizer
    {
        public Task<RecordScopeDecision> AuthorizeAnyAsync(Guid employeeId, string roleCode, string organizationId,
            DateOnly onDate, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(true, "disposable test scope"));
        public Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target,
            DateOnly onDate, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(true, "disposable test scope"));
    }

    private sealed class PurchaseFlowAllowingPermissions : IPagePermissionService
    {
        public Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roleCodes,
            string pageKey, string permission, CancellationToken ct)
        {
            if (pageKey == "stores.material-issues" && permission == PagePermissionActions.View)
                return Task.FromResult(roleCodes.Any(role => role is "STORES_ASSISTANT"
                    or "STORES_EXECUTIVE" or "STORES_MANAGER"));
            return Task.FromResult(true);
        }
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
