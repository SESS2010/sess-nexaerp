using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Inventory;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.MasterData;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    // Go-live rehearsal for one company on a principal-provisioned fresh database with no
    // trial data: the seeded Stores Manager must be able to bring a real warehouse to the
    // point where opening stock can post into AVAILABLE, through the API alone.
    [Fact]
    public async Task FreshCompanyReachesAvailableStockThroughSeededStoresAuthority()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin(), databaseName: "sess_nexa_erp");
        server.Execute("fresh-company-chain.sql", migrator.GenerateScript("0", migrations[^1]));
        const string password = "fresh-company-runtime-123456789";
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, password);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        // The real item master arrives through the checked-in owner script, exactly as on go-live day.
        server.Execute("fresh-company-legacy-items.sql", "SET SESSION AUTHORIZATION nexa_erp_migration; SET ROLE nexa_erp_owner;\n"
            + await File.ReadAllTextAsync(Path.Combine(FindRepositoryRoot(), "database", "postgresql", "legacy-item-import-2026-08-29.sql")));
        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options;
        var companyId = MultiCompanyFoundationSeedData.SessPvtLtdId;
        var employees = new Dictionary<string, Guid>();
        await using (var seed = new NexaErpDbContext(options))
        {
            Assert.False(await seed.Warehouses.AnyAsync(x => x.CompanyId == companyId), "A fresh database must start without warehouses.");
            Assert.False(await seed.WarehouseConditionLocations.AnyAsync(x => x.CompanyId == companyId));
            Assert.True(await seed.Items.CountAsync(x => x.CreatedBy == "EXCEL_IMPORT" && x.ApprovalStatus == "Approved") > 1000);
            foreach (var code in new[] { "SESS-41", "SESS-14", "SESS-01", "SESS-02", "SESS-04", "SESS-05", "SESS-12", "SESS-15", "SESS-33", "SESS-35" })
            {
                var employee = await seed.Employees.SingleAsync(x => x.EmployeeCode == code);
                employee.LoginEnabled = true;
                employees[code] = employee.Id;
                seed.EmployeeIdentityMappings.Add(Mapping(companyId, employee.Id, code));
            }
            await seed.SaveChangesAsync();
        }
        var assignments = await Query(options, async db =>
            (await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                .Where(x => x.CompanyId == companyId && x.EffectiveTo == null).ToListAsync())
            .ToDictionary(x => TaxWorkflowUser.AssignmentKey(x.EmployeeId, x.Role!.Code),
                x => new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType)));
        var runtime = new NpgsqlConnectionStringBuilder(server.ConnectionString) { Username = "nexa_erp_runtime", Password = password, Pooling = false }.ConnectionString;
        var user = new TaxWorkflowUser(employees["SESS-41"], "SESS-41", "STORES_MANAGER", assignments);
        Assert.NotEqual(Guid.Empty, Assert.Single(user.EffectiveRoleAssignments).AssignmentId);
        await using var host = await PurchaseFlowHost.StartAsync(runtime, user, useRealPagePermissions: true, useRealOperationalScopes: true);
        var client = host.Client;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Real warehouse and one rack per stock condition the receipt chain needs.
        await Post<JsonElement>(client, "/api/v1/inventory/warehouses", new UpsertWarehouseRequest(
            "MAIN", "Main Stores", "STORES", "Factory", null, null, null, null, null, null, null, null, null));
        var racks = new Dictionary<string, Guid>();
        foreach (var (bin, condition) in new[] { ("MAIN-R01-A", "AVAILABLE"), ("MAIN-QC-01", "QC_HOLD"), ("MAIN-RET-01", "PENDING_RETURNABLE_DC") })
        {
            var created = await Post<JsonElement>(client, "/api/v1/inventory/rack-bins", new UpsertRackBinRequest(
                "MAIN", bin, "Rack " + bin, "1", "Stores", "RACK", condition, null, null, null, null, null));
            racks[condition] = created.GetProperty("RecordId").GetGuid();
        }

        // 2. Condition locations. AVAILABLE first: without it neither QC acceptance nor opening stock can post.
        var locations = new Dictionary<string, Guid>();
        foreach (var (condition, rack) in racks)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/rev869a/configuration/warehouse-condition-locations",
                new CreateWarehouseConditionLocationRequest("SESS_PVT_LTD", "MAIN", rack, condition, today, null, "Go-live location for " + condition));
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.Created,
                $"Seeded STORES_MANAGER could not create the {condition} condition location: {(int)response.StatusCode} {body}");
            locations[condition] = JsonDocument.Parse(body).RootElement.GetProperty("Id").GetGuid();
        }
        var listed = await Get<JsonElement>(client, "/api/v1/rev869a/configuration/warehouse-condition-locations?warehouseCode=MAIN&effectiveOnly=true");
        Assert.Equal(3, listed.EnumerateArray().Count());
        Assert.Contains(listed.EnumerateArray(), x => x.GetProperty("ConditionCode").GetString() == "AVAILABLE");

        // 3. Stores category route for the imported item's category: the GRN resolves exactly one.
        var item = await Query(options, db => db.Items.AsNoTracking().Include(x => x.Category).Where(x => x.CreatedBy == "EXCEL_IMPORT" && x.IsActive
            && !x.SerialNumberTracking && !x.BatchTracking).OrderBy(x => x.ItemCode).FirstAsync());
        const string routes = "/api/v1/rev869a/configuration/store-category-routes";
        var routeRequest = new CreateStoreCategoryRouteRequest("SESS_PVT_LTD", item.Category!.Code, locations["QC_HOLD"], locations["PENDING_RETURNABLE_DC"], locations["AVAILABLE"], today, null, "Go-live route for " + item.Category.Code);
        using (var swapped = await client.PostAsJsonAsync(routes, routeRequest with { QcHoldConditionLocationId = locations["AVAILABLE"], DefaultAcceptedConditionLocationId = locations["QC_HOLD"] }))
            Assert.Equal(HttpStatusCode.Conflict, swapped.StatusCode);
        using (var unknown = await client.PostAsJsonAsync(routes, routeRequest with { ItemCategoryCode = "NO-SUCH-CATEGORY" }))
            Assert.Equal(HttpStatusCode.Conflict, unknown.StatusCode);
        var route = await Post<StoreCategoryRouteSummary>(client, routes, routeRequest);
        Assert.Equal(item.Category.Code, route.ItemCategoryCode);
        Assert.Equal("MAIN", route.WarehouseCode);
        using (var overlap = await client.PostAsJsonAsync(routes, routeRequest))
            Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        Assert.Equal(route.Id, Assert.Single(await Get<List<StoreCategoryRouteSummary>>(client, routes + "?effectiveOnly=true&itemCategoryCode=" + item.Category.Code)).Id);
        user.Set(employees["SESS-14"], "SESS-14", "ACCOUNTS_MANAGER");
        using (var forbidden = await client.PostAsJsonAsync(routes, routeRequest with { ItemCategoryCode = "FAB" }))
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        user.Set(employees["SESS-41"], "SESS-41", "STORES_MANAGER");
        Assert.True(await Query(options, db => db.StoreCategoryRoutes.AnyAsync(x => x.Id == route.Id && x.CompanyId == companyId && x.CreatedBy == "SESS-41")));

        // 4. Opening stock for a real imported item, through the three-actor ceremony.
        var workbook = new MasterDataWorkbookService().Create(new OpeningStockImportDefinition(),
            [new(new Dictionary<string, object?> {
                ["LineReference"] = "GO-LIVE-0001", ["ItemCode"] = item.ItemCode, ["WarehouseCode"] = "MAIN",
                ["RackBinCode"] = "MAIN-R01-A", ["LotNumber"] = null, ["SerialNumber"] = null, ["Quantity"] = 10m, ["Rate"] = 125m })],
            DateTimeOffset.UtcNow);
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(workbook), "File", "opening-stock.xlsx");
        form.Add(new StringContent(MasterDataImportModes.ImportValidRows), "Mode");
        form.Add(new StringContent("fresh-company-opening-import"), "IdempotencyKey");
        using var imported = await client.PostAsync("/api/v1/master-data/opening-stock/import", form);
        var importBody = await imported.Content.ReadAsStringAsync();
        Assert.True(imported.IsSuccessStatusCode, importBody);
        var batch = JsonSerializer.Deserialize<MasterDataImportResult>(importBody, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(1, batch.CreatedRows);
        Assert.Equal(0, batch.InvalidRows);
        var opening = await Post<OpeningStockView>(client, "/api/v1/stores/opening-stock/from-import",
            new CreateOpeningStockFromImportRequest(batch.BatchId, new DateOnly(today.Year, 4, 1), today, "Physical count on go-live day", "fresh-company-count"));
        user.Set(employees["SESS-14"], "SESS-14", "ACCOUNTS_MANAGER");
        opening = await Post<OpeningStockView>(client, $"/api/v1/stores/opening-stock/{opening.Id}/confirm-value",
            new OpeningStockTransitionRequest(opening.Version, "Carrying value confirmed", "fresh-company-value"));
        user.Set(employees["SESS-01"], "SESS-01", "TECHNICAL_DIRECTOR");
        opening = await Post<OpeningStockView>(client, $"/api/v1/stores/opening-stock/{opening.Id}/authorize",
            new OpeningStockTransitionRequest(opening.Version, "Opening stock authorized", "fresh-company-authorize"));
        Assert.Equal("POSTED", opening.Status);
        Assert.Equal(1250m, opening.TotalValue);
        await using var evidence = new NexaErpDbContext(options);
        var movement = await evidence.StockMovements.AsNoTracking().SingleAsync(x => x.CompanyId == companyId);
        Assert.Equal("AVAILABLE", movement.ConditionCode);
        Assert.Equal(locations["AVAILABLE"], movement.WarehouseConditionLocationId);
        Assert.Equal(10m, movement.QuantityIn);
        Assert.Equal(1250m, await evidence.FifoInventoryCostLayers.Where(x => x.CompanyId == companyId).SumAsync(x => x.LayerValue));
        await ProveFreshCompanyReceipt(client, options, user, assignments, employees, companyId, item, locations["AVAILABLE"], today);
        await ProveItemMasterAuthority(server, client, options, user, employees, item);
        // The merge trigger rewrite refuses rollback once Technical Director merge evidence exists.
        const string mergeAuthority = "20260920140000_ItemMergeDirectorAuthority";
        server.AssertRejected("item-merge-authority-refuse-down.sql", "SET SESSION AUTHORIZATION nexa_erp_migration; SET ROLE nexa_erp_owner;\n"
            + migrator.GenerateScript(mergeAuthority, migrations[Array.IndexOf(migrations, mergeAuthority) - 1]), "Technical Director item merge evidence exists");
        await ProveStoresTopologyGrantMigration(server, options, migrator, migrations);
    }

    // Item permission move: Stores and Purchase Managers approve item master records; the
    // Technical Director approves only a correction to a record more than one month old (since
    // creation) and a duplicate merge. A first approval never needs the director.
    private static async Task ProveItemMasterAuthority(DisposablePostgreSql server, HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, IReadOnlyDictionary<string, Guid> employees, SESS.NexaERP.Domain.Inventory.Item template)
    {
        void Actor(string code, string role, params string[] effectiveRoles) => user.Set(employees[code], code, role, effectiveRoles);
        UpsertItemRequest Draft(string code) => new(code, "Go-live item " + code, "Go-live item " + code, template.CategoryId!.Value, null, template.MaterialType, template.ItemType,
            false, template.Uom, null, null, null, template.HsnSacCode, template.GstPercentage, null, null, false, false, false, false, 0, 0, 0, null, null, null, null, null, null, null, null);
        async Task<JsonElement> Detail(string code) => await Get<JsonElement>(client, "/api/v1/inventory/items/" + code);
        async Task<HttpStatusCode> Approve(string code, string key)
        {
            var detail = await Detail(code);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/inventory/items/{code}/approve") { Content = JsonContent.Create(new MasterActionRequest("Approved", detail.GetProperty("Version").GetUInt32())) };
            request.Headers.Add("Idempotency-Key", key);
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }
        // Stores Manager creates and submits; the Purchase Manager approves (self-approval is refused).
        Actor("SESS-41", "STORES_MANAGER");
        var created = await Post<JsonElement>(client, "/api/v1/inventory/items", Draft("GO-LIVE-ITM-001"));
        await Post<JsonElement>(client, "/api/v1/inventory/items/GO-LIVE-ITM-001/submit", new MasterActionRequest("Submitted", created.GetProperty("Version").GetUInt32()));
        Assert.Equal(HttpStatusCode.Forbidden, await Approve("GO-LIVE-ITM-001", "item-self-approve"));
        Actor("SESS-01", "TECHNICAL_DIRECTOR");
        Assert.Equal(HttpStatusCode.Forbidden, await Approve("GO-LIVE-ITM-001", "item-td-ordinary-approve"));
        Actor("SESS-15", "PURCHASE_MANAGER", "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE");
        Assert.Equal(HttpStatusCode.OK, await Approve("GO-LIVE-ITM-001", "item-purchase-approve"));
        Assert.Equal(MasterApprovalStatuses.Approved, (await Detail("GO-LIVE-ITM-001")).GetProperty("ApprovalStatus").GetString());
        // A correction within one month of creation returns the record to approval. Maker-checker
        // excludes only the maker of the current pending change: the Purchase Manager corrects the
        // item the Stores Manager created, and the Stores Manager approves that correction.
        Actor("SESS-15", "PURCHASE_MANAGER", "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE");
        var detail = await Detail("GO-LIVE-ITM-001");
        var corrected = JsonSerializer.Deserialize<UpsertItemRequest>(detail.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web))! with { Name = "Go-live item corrected" };
        await Put<JsonElement>(client, "/api/v1/inventory/items/GO-LIVE-ITM-001", corrected);
        Assert.Equal(MasterApprovalStatuses.PendingApproval, (await Detail("GO-LIVE-ITM-001")).GetProperty("ApprovalStatus").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, await Approve("GO-LIVE-ITM-001", "item-maker-young-correction"));
        Actor("SESS-01", "TECHNICAL_DIRECTOR");
        Assert.Equal(HttpStatusCode.Forbidden, await Approve("GO-LIVE-ITM-001", "item-td-young-correction"));
        Actor("SESS-41", "STORES_MANAGER");
        Assert.Equal(HttpStatusCode.OK, await Approve("GO-LIVE-ITM-001", "item-creator-approves-young-correction"));
        // A correction to a record more than one month old (since creation) needs the Technical Director.
        server.Execute("age-go-live-item.sql", """UPDATE advance.items SET "CreatedAt"=now()-interval '35 days' WHERE "ItemCode"='GO-LIVE-ITM-001';""");
        Actor("SESS-15", "PURCHASE_MANAGER", "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE");
        detail = await Detail("GO-LIVE-ITM-001");
        corrected = JsonSerializer.Deserialize<UpsertItemRequest>(detail.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web))! with { Name = "Go-live item corrected after one month" };
        await Put<JsonElement>(client, "/api/v1/inventory/items/GO-LIVE-ITM-001", corrected);
        Actor("SESS-41", "STORES_MANAGER");
        Assert.Equal(HttpStatusCode.Forbidden, await Approve("GO-LIVE-ITM-001", "item-stores-old-correction"));
        Actor("SESS-01", "TECHNICAL_DIRECTOR");
        Assert.Equal(HttpStatusCode.OK, await Approve("GO-LIVE-ITM-001", "item-td-old-correction"));
        Assert.Equal(MasterApprovalStatuses.Approved, (await Detail("GO-LIVE-ITM-001")).GetProperty("ApprovalStatus").GetString());
        // Duplicate merge is the director's alone.
        Actor("SESS-41", "STORES_MANAGER");
        var duplicate = await Post<JsonElement>(client, "/api/v1/inventory/items", Draft("GO-LIVE-ITM-002"));
        await Post<JsonElement>(client, "/api/v1/inventory/items/GO-LIVE-ITM-002/submit", new MasterActionRequest("Submitted", duplicate.GetProperty("Version").GetUInt32()));
        Actor("SESS-15", "PURCHASE_MANAGER", "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE");
        Assert.Equal(HttpStatusCode.OK, await Approve("GO-LIVE-ITM-002", "item-duplicate-approve"));
        var survivorId = (await Detail("GO-LIVE-ITM-001")).GetProperty("Id").GetGuid();
        var sourceId = (await Detail("GO-LIVE-ITM-002")).GetProperty("Id").GetGuid();
        var merge = new MergeItemRequest(survivorId, "Duplicate of GO-LIVE-ITM-001", "item-merge-go-live");
        Actor("SESS-41", "STORES_MANAGER");
        using (var refused = await client.PostAsJsonAsync($"/api/v1/inventory/items/{sourceId}/merge", merge))
            Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Actor("SESS-01", "TECHNICAL_DIRECTOR");
        using (var merged = await client.PostAsJsonAsync($"/api/v1/inventory/items/{sourceId}/merge", merge))
            Assert.True(merged.StatusCode == HttpStatusCode.NoContent, await merged.Content.ReadAsStringAsync());
        Assert.True(await Query(options, db => db.ItemMergeAliases.AnyAsync(x => x.SourceItemId == sourceId && x.SurvivorItemId == survivorId)));
    }

    // 5. First real receipt. Every governed prerequisite a fresh company needs before its
    // first GRN is created here by the seeded actor who owns it, under real page
    // permissions and real operational scopes: vendor, vendor qualification, GST rule,
    // QC policy, then requisition -> RFQ -> quotation -> technical verification ->
    // comparison -> PO -> gate entry -> GRN -> QC acceptance into Stores' AVAILABLE location.
    private static async Task ProveFreshCompanyReceipt(HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, Dictionary<string, EffectiveRoleAssignment> assignments, IReadOnlyDictionary<string, Guid> employees, Guid companyId,
        SESS.NexaERP.Domain.Inventory.Item item, Guid availableLocationId, DateOnly today)
    {
        void Actor(string code, string role, params string[] effectiveRoles) => user.Set(employees[code], code, role, effectiveRoles);
        const string configuration = "/api/v1/rev869a/configuration";
        var category = item.Category!.Code;
        // Purchase a second item of the routed category: the opening-stock item is fully available, so a
        // requisition for it would close at stock check without any purchase requirement.
        var purchased = await Query(options, db => db.Items.AsNoTracking().Where(x => x.CreatedBy == "EXCEL_IMPORT" && x.IsActive
            && x.CategoryId == item.CategoryId && x.Id != item.Id && !x.SerialNumberTracking && !x.BatchTracking).OrderBy(x => x.ItemCode).FirstAsync());
        var hsn = purchased.HsnSacCode!;
        // Vendor: IT Manager uploads the mandatory GST certificate, creates and submits; Technical Director approves.
        Actor("SESS-12", "IT_MANAGER");
        using var certificateForm = new MultipartFormDataContent();
        var certificate = new ByteArrayContent(SupplierInvoiceFixturePdf("GST CERTIFICATE"));
        certificate.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        certificateForm.Add(certificate, "file", "gst-certificate.pdf");
        certificateForm.Add(new StringContent("GST_CERTIFICATE"), "kind");
        using var uploaded = await client.PostAsync("/api/v1/masters/vendors/attachments", certificateForm);
        var uploadBody = await uploaded.Content.ReadAsStringAsync();
        Assert.True(uploaded.StatusCode == HttpStatusCode.Created, uploadBody);
        var certificateId = JsonDocument.Parse(uploadBody).RootElement.GetProperty("Id").GetGuid();
        var vendorRequest = new UpsertVendorRequest("GO-LIVE-VEN-001", "Go-live Supplies Pvt Ltd", null, "MANUFACTURER", "33AAACG1234A1Z5", "AAACG1234A",
            false, null, "Supplier contact", "9000000000", "supplier@example.test", "Chennai", "Chennai", "Tamil Nadu", "33", "India",
            category, null, "30 days", "Delivered", 30, null, $$$"""{"gstCertificate":{"id":"{{{certificateId}}}"}}""", null);
        var vendor = await Post<JsonElement>(client, "/api/v1/masters/vendors", vendorRequest);
        var vendorId = vendor.GetProperty("Id").GetGuid();
        await Post<JsonElement>(client, "/api/v1/masters/vendors/GO-LIVE-VEN-001/submit", new MasterActionRequest("Vendor submitted", vendor.GetProperty("Version").GetUInt32()));
        // Final approval requires Accounts commercial verification. The legacy verifier role
        // ACCOUNTS_HEAD is retired (not employee-assignable); its governed replacement is the
        // Accounts Manager (finding #21). Approval stays independent, with the Technical Director.
        Actor("SESS-14", "ACCOUNTS_MANAGER");
        var submitted = await Get<JsonElement>(client, "/api/v1/masters/vendors/GO-LIVE-VEN-001");
        await Post<JsonElement>(client, "/api/v1/masters/vendors/GO-LIVE-VEN-001/verify-commercial", new MasterActionRequest("Commercial terms verified", submitted.GetProperty("Version").GetUInt32()));
        // The seeded VENDOR_FINAL_APPROVER policy names the Managing Director.
        Actor("SESS-02", "MANAGING_DIRECTOR");
        submitted = await Get<JsonElement>(client, "/api/v1/masters/vendors/GO-LIVE-VEN-001");
        await Post<JsonElement>(client, "/api/v1/masters/vendors/GO-LIVE-VEN-001/approve", new MasterActionRequest("Vendor approved", submitted.GetProperty("Version").GetUInt32()));
        // Vendor qualification: Purchase Manager creates, Technical Director verifies, Managing Director approves.
        Actor("SESS-15", "PURCHASE_MANAGER", "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE");
        await PostNoResult(client, configuration + "/vendor-qualifications",
            new CreateVendorQualificationRequest("SESS_PVT_LTD", "GO-LIVE-VEN-001", category, "GO-LIVE-QUALIFICATION", today, null, "Qualified for go-live category"), "go-live-qualification-create");
        var qualification = await Query(options, db => db.VendorQualifications.Where(x => x.VendorId == vendorId && x.QualificationCode == "GO-LIVE-QUALIFICATION").Select(x => new { x.Id, x.Version }).SingleAsync());
        Actor("SESS-01", "TECHNICAL_DIRECTOR");
        await PostNoResult(client, $"{configuration}/vendor-qualifications/{qualification.Id}/verify", new ChangeVendorQualificationLifecycleRequest(qualification.Version, "Technical qualification checked"), "go-live-qualification-verify");
        var verifiedVersion = await Query(options, db => db.VendorQualifications.Where(x => x.Id == qualification.Id).Select(x => x.Version).SingleAsync());
        Actor("SESS-02", "MANAGING_DIRECTOR");
        await PostNoResult(client, $"{configuration}/vendor-qualifications/{qualification.Id}/approve", new ChangeVendorQualificationLifecycleRequest(verifiedVersion, "Qualification approved"), "go-live-qualification-approve");
        // GST rule for the item's HSN: Accounts Manager creates, Managing Director approves.
        Actor("SESS-14", "ACCOUNTS_MANAGER");
        await PostNoResult(client, configuration + "/tax-gst", new CreateTaxGstSettingRequest("SESS_PVT_LTD", TaxJurisdictions.IndiaGst, hsn, "33", "33",
            VendorRegistrationType.REGULAR.ToCanonicalValue(), purchased.GstPercentage, purchased.GstPercentage / 2, purchased.GstPercentage / 2, 0, 0, false, false, "INR", 2, today, null, "GST portal cross-checked"), "go-live-tax-create");
        var tax = await Query(options, db => db.TaxGstSettings.Where(x => x.HsnSacCode == hsn && x.CompanyId == companyId).Select(x => new { x.Id, x.Version }).SingleAsync());
        Actor("SESS-02", "MANAGING_DIRECTOR");
        await PostNoResult(client, $"{configuration}/tax-gst/{tax.Id}/approve", new DecideTaxGstSettingRequest(tax.Version, "GST rule approved", "go-live-tax-approve"), "go-live-tax-approve");
        // QC policy: QC Manager creates, Technical Director approves.
        Actor("SESS-33", "QC_MANAGER");
        var policy = await Post<JsonElement>(client, configuration + "/qc-inspection-policies", new CreateQcInspectionPolicyRequest("SESS_PVT_LTD", purchased.ItemCode, null,
            "VISUAL_CHECK", purchased.Uom, 0, 1, "Visual inspection", 1, today, null, "Go-live acceptance criterion"));
        Actor("SESS-01", "TECHNICAL_DIRECTOR");
        await Post<JsonElement>(client, $"{configuration}/qc-inspection-policies/{policy.GetProperty("Id").GetGuid()}/approve",
            new MasterActionRequest("Criterion approved", policy.GetProperty("Version").GetUInt32()), "go-live-policy-approve");
        // Requisition raised by the Purchase Executive as requester. The requester must be the caller, and
        // department requesters such as the IT Manager hold only view on purchase.requisitions (finding #22);
        // delivery to the real warehouse; Accounts verifies and approves.
        var required = today.AddDays(30);
        Actor("SESS-15", "PURCHASE_EXECUTIVE", "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE");
        var pr = await Post<PurchaseRequisitionDetail>(client, "/api/v1/purchase/requisitions", new CreatePurchaseRequisitionRequest("SESS_PVT_LTD", "PURCHASE", "SESS-15", required, "NORMAL",
            "Go-live first receipt", "MAIN", null, null, null, null, null, [new(purchased.ItemCode, 5m, 100m, required, "MAIN", null, null, null)]));
        pr = await Post<PurchaseRequisitionDetail>(client, $"/api/v1/purchase/requisitions/{pr.PrNumber}/submit", new PurchaseRequisitionActionRequest(null, pr.Version, "go-live-pr-submit"));
        Actor("SESS-14", "ACCOUNTS_MANAGER");
        pr = await Post<PurchaseRequisitionDetail>(client, $"/api/v1/purchase/requisitions/{pr.PrNumber}/verify", new PurchaseRequisitionActionRequest("Department verified", pr.Version, "go-live-pr-verify"));
        pr = await Post<PurchaseRequisitionDetail>(client, $"/api/v1/purchase/requisitions/{pr.PrNumber}/approve", new PurchaseRequisitionActionRequest("Approved", pr.Version, "go-live-pr-approve"));
        Assert.Equal(PurchaseRequisitionStatuses.StockCheckPending, pr.Status);
        Actor("SESS-35", "STORES_EXECUTIVE");
        await PostNoResult(client, $"/api/v1/purchase/requisitions/{pr.PrNumber}/stock-check", new StockCheckRequest("No stock; purchase required", pr.Version, "go-live-stock", [new(1, "MAIN", "MAIN-R01-A")]), "go-live-stock");
        var handoff = await Query(options, db => db.PurchaseRequirementHandoffs.Where(x => x.PurchaseRequisitionId == pr.Id).Select(x => new { x.Id, x.HandoffQuantity }).SingleAsync());
        // RFQ, single-source invitation, quotation entered on the vendor's behalf.
        Actor("SESS-15", "PURCHASE_EXECUTIVE", "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE");
        var rfq = await Post<Rev869BDocumentResult>(client, "/api/v1/purchase/rfqs", new Rev869BCreateRfqRequest(DateTimeOffset.UtcNow.AddDays(7), "INR", true, "Only qualified vendor at go-live", "go-live-rfq", [new(handoff.Id, handoff.HandoffQuantity)]));
        var rfqVersion = await Query(options, db => db.RequestForQuotations.Where(x => x.Id == rfq.Id).Select(x => x.Version).SingleAsync());
        var invitation = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/rfqs/{rfq.Number}/vendors", new Rev869BInviteVendorRequest(vendorId, "Qualified vendor invited", rfqVersion, "go-live-invite"));
        var rfqLineId = await Query(options, db => db.RequestForQuotationLines.Where(x => x.RequestForQuotationId == rfq.Id).Select(x => x.Id).SingleAsync());
        var quotation = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/rfq-invitations/{invitation.Id}/quotations", new Rev869BSubmitQuotationRequest("GO-LIVE-Q1", "INR", "30 days", "Delivered to MAIN", "12 months", false, null, "EMAIL_RECEIVED",
            DateTimeOffset.UtcNow.AddMinutes(-1), "go-live/vendor-1.pdf", new string('A', 64), "Entered from vendor quotation", 0, null, "go-live-quote",
            [new(rfqLineId, handoff.HandoffQuantity, 100m, 0, 0, 0, 0, 0, required, hsn, "33", "33", VendorRegistrationType.REGULAR.ToCanonicalValue(), 0)]));
        // Finding #17 probe: the seeded Technical Support Managers (SESS-04 FULL, SESS-05 SUPPORT) must
        // receive a decision (200) or a refusal (403) on technical verification, never a 500.
        var quotationLineId = await Query(options, db => db.VendorQuotationLines.Where(x => x.VendorQuotationId == quotation.Id).Select(x => x.Id).SingleAsync());
        var verified = false;
        foreach (var (code, roles) in new[] { ("SESS-04", new[] { "TECHNICAL_SUPPORT_MANAGER" }), ("SESS-05", new[] { "SERVICE_ENGINEER", "TECHNICAL_SUPPORT_MANAGER" }) })
        {
            if (verified) break;
            Actor(code, "TECHNICAL_SUPPORT_MANAGER", roles);
            using var attempt = await client.PostAsJsonAsync($"/api/v1/purchase/quotations/{quotation.Number}/technical-verifications",
                new Rev869BTechnicalVerificationRequest(quotationLineId, true, """{"goLive":true}""", "Technically compliant", quotation.Version, "go-live-technical-" + code));
            var attemptBody = await attempt.Content.ReadAsStringAsync();
            Assert.True(attempt.StatusCode is HttpStatusCode.OK or HttpStatusCode.Forbidden, $"{code} technical verification returned {(int)attempt.StatusCode}: {attemptBody}");
            var probeDirectory = Path.Combine(FindRepositoryRoot(), "local-evidence", "finding-17");
            Directory.CreateDirectory(probeDirectory);
            await File.AppendAllTextAsync(Path.Combine(probeDirectory, "tsm-probe.jsonl"), JsonSerializer.Serialize(new { at = DateTimeOffset.Now, actor = code, roles, status = (int)attempt.StatusCode, body = attemptBody }) + "\n");
            verified = attempt.StatusCode == HttpStatusCode.OK;
        }
        if (!verified)
        {
            Actor("SESS-01", "TECHNICAL_DIRECTOR");
            await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/quotations/{quotation.Number}/technical-verifications", new Rev869BTechnicalVerificationRequest(quotationLineId, true, """{"goLive":true}""", "Technically compliant", quotation.Version, "go-live-technical"));
        }
        Actor("SESS-15", "PURCHASE_MANAGER", "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE");
        rfqVersion = await Query(options, db => db.RequestForQuotations.Where(x => x.Id == rfq.Id).Select(x => x.Version).SingleAsync());
        var comparison = await Post<Rev869BDocumentResult>(client, "/api/v1/purchase/comparisons", new Rev869BCreateComparisonRequest(rfq.Number, rfqVersion, "go-live-comparison"));
        comparison = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/comparisons/{comparison.Number}/recommend", new Rev869BRecommendComparisonRequest(quotation.Id, "Only compliant offer", "Only qualified vendor at go-live", comparison.Version, "go-live-recommend"));
        Actor("SESS-14", "ACCOUNTS_MANAGER");
        comparison = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/comparisons/{comparison.Number}/approve", new Rev869BApprovalActionRequest("Comparison approved", comparison.Version, "go-live-comparison-approve"));
        Assert.Equal(Rev869BStatuses.Approved, comparison.Status);
        Actor("SESS-15", "PURCHASE_MANAGER", "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE");
        var po = await Post<Rev869BDocumentResult>(client, "/api/v1/purchase/purchase-orders", new Rev869BCreatePurchaseOrderRequest(comparison.Number, comparison.Version, "go-live-po"));
        po = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/purchase-orders/{po.Number}/submit", new Rev869BSubmitPurchaseOrderRequest("PO submitted", po.Version, "go-live-po-submit"));
        Actor("SESS-14", "ACCOUNTS_MANAGER");
        po = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/purchase-orders/{po.Number}/approve", new Rev869BPoApprovalActionRequest("PO approved", po.Version, null, "go-live-po-approve"));
        Actor("SESS-15", "PURCHASE_MANAGER", "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE");
        po = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/purchase-orders/{po.Number}/issue", new Rev869BIssuePurchaseOrderRequest("PO issued", po.Version, "go-live-po-issue"));
        Assert.Equal(Rev869BStatuses.Issued, po.Status);
        // Gate entry and GRN by Stores; the GRN resolves the route Stores created.
        Actor("SESS-35", "STORES_EXECUTIVE");
        var poLineId = await Query(options, db => db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == po.Id).Select(x => x.Id).SingleAsync());
        var gate = await Post<GateEntryResult>(client, "/api/v1/stores/gate-entries/", new CreateGateEntryRequest(po.Number, "GO-LIVE-DC-1", "TN-01-0001", "ROAD", DateTimeOffset.UtcNow, """{"packagesChecked":true}""", [new(poLineId, 5m)]), "go-live-gate");
        gate = await Post<GateEntryResult>(client, $"/api/v1/stores/gate-entries/{gate.Id}/finalize", new FinalizeGateEntryRequest(gate.Version, "go-live-gate-finalize"));
        var grn = await Post<GoodsReceiptResult>(client, "/api/v1/stores/goods-receipts/", new CreateGoodsReceiptRequest(gate.GateEntryNumber, "GO-LIVE-BILL-1", today, DateTimeOffset.UtcNow, """{"billChecked":true}""",
            [new(gate.Lines.Single().Id, [new(1, 5m, "GO-LIVE-LOT-1", null, today.AddMonths(-1), today.AddYears(2))], [])]), "go-live-grn");
        // inventory.grn create/submit belongs to the Stores Executive; the Stores Manager holds no GRN grant (see #12).
        grn = await Post<GoodsReceiptResult>(client, $"/api/v1/stores/goods-receipts/{grn.Id}/finalize", new FinalizeGoodsReceiptRequest(grn.Version, "go-live-grn-finalize"));
        Assert.Equal("FINALIZED", grn.Status);
        Assert.NotNull(grn.StockPostingBatchId);
        // The Stores Manager supervises receipts: view only, never finalize (finding #12).
        Actor("SESS-41", "STORES_MANAGER");
        Assert.Equal(grn.Id, (await Get<GoodsReceiptResult>(client, "/api/v1/stores/goods-receipts/" + grn.Id)).Id);
        using (var managerCannotFinalize = await client.PostAsJsonAsync($"/api/v1/stores/goods-receipts/{grn.Id}/finalize", new FinalizeGoodsReceiptRequest(grn.Version, "stores-manager-read-does-not-grant-submit")))
            Assert.Equal(HttpStatusCode.Forbidden, managerCannotFinalize.StatusCode);
        // QC reads the receipt it inspects (finding #12) and accepts the lot into the AVAILABLE location Stores created.
        Actor("SESS-33", "QC_MANAGER");
        var qcRead = await Get<GoodsReceiptResult>(client, "/api/v1/stores/goods-receipts/" + grn.Id);
        Assert.Equal(grn.GrnNumber, qcRead.GrnNumber);
        using (var qcCannotFinalize = await client.PostAsJsonAsync($"/api/v1/stores/goods-receipts/{grn.Id}/finalize", new FinalizeGoodsReceiptRequest(qcRead.Version, "qc-read-does-not-grant-submit")))
            Assert.Equal(HttpStatusCode.Forbidden, qcCannotFinalize.StatusCode);
        var queue = await Get<PagedResponse<QcQueueItem>>(client, "/api/v1/qc/queue?pageSize=100");
        var lot = Assert.Single(queue.Items, x => x.GrnNumber == grn.GrnNumber);
        var policies = await Get<JsonElement>(client, $"{configuration}/qc-inspection-policies?effectiveOnly=true&itemId={lot.ItemId}");
        var policyId = Assert.Single(policies.EnumerateArray()).GetProperty("Id").GetGuid();
        var inspection = await Post<QcInspectionResult>(client, "/api/v1/qc/inspections", new FinalizeQcInspectionRequest(lot.GoodsReceiptLineLotAllocationId, DateTimeOffset.UtcNow, 5m, 0m, 0m, availableLocationId,
            [new QcParameterResultRequest(policyId, 1, 1, null, "PASS", "Accepted")], []), "go-live-qc");
        Assert.NotNull(inspection.StockPostingBatchId);
        await using var db = new NexaErpDbContext(options);
        var received = await db.StockMovements.AsNoTracking().Where(x => x.CompanyId == companyId && x.ItemId == purchased.Id && x.WarehouseConditionLocationId == availableLocationId).SumAsync(x => x.QuantityIn - x.QuantityOut);
        Assert.Equal(5m, received);
        Assert.Equal(0m, await db.StockMovements.AsNoTracking().Where(x => x.CompanyId == companyId && x.ConditionCode == "QC_HOLD").SumAsync(x => x.QuantityIn - x.QuantityOut));
        Assert.True(await db.FifoInventoryCostLayers.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.GoodsReceiptLineId != null));
    }

    // The grant is audited and exactly reversible; drift refuses rollback; a site-added
    // permission on the same page survives up/down untouched.
    private static async Task ProveStoresTopologyGrantMigration(DisposablePostgreSql server,
        DbContextOptions<NexaErpDbContext> options, IMigrator migrator, string[] migrations)
    {
        const string target = "20260920090000_StoresWarehouseRackGrants";
        var index = Array.IndexOf(migrations, target);
        Assert.True(index > 0);
        const string deploy = "SET SESSION AUTHORIZATION nexa_erp_migration; SET ROLE nexa_erp_owner;\n";
        var down = deploy + migrator.GenerateScript(target, migrations[index - 1]);
        var up = deploy + migrator.GenerateScript(migrations[index - 1], target);
        await using var db = new NexaErpDbContext(options);
        var stores = await db.Roles.Where(x => x.Code == "STORES_MANAGER").Select(x => x.Id).SingleAsync();
        var legacy = await db.Roles.Where(x => x.Code == "STORE_HEAD").Select(x => x.Id).SingleAsync();
        var warehouses = await db.PageDefinitions.Where(x => x.PageKey == "masters.warehouses").Select(x => x.Id).SingleAsync();
        var racks = await db.PageDefinitions.Where(x => x.PageKey == "masters.rack-bins").Select(x => x.Id).SingleAsync();
        foreach (var page in new[] { warehouses, racks })
        {
            var grant = await db.RolePagePermissions.AsNoTracking().SingleAsync(x => x.RoleId == stores && x.PageDefinitionId == page);
            Assert.True(grant.CanView && grant.CanCreate && grant.CanUpdate && grant.CanSubmit && grant.CanExport && grant.CanViewAuditHistory);
            Assert.False(grant.CanApprove || grant.CanVerify || grant.CanDeactivate || grant.HasFullControl);
            var untouched = await db.RolePagePermissions.AsNoTracking().SingleAsync(x => x.RoleId == legacy && x.PageDefinitionId == page);
            Assert.Equal(0u, untouched.Version);
        }
        Assert.Equal(3, await db.AuditLogs.CountAsync(x => x.CreatedBy == "StoresWarehouseRackGrants" && x.Action == "GrantStoresTopology"));
        var permission = await db.RolePagePermissions.AsNoTracking().SingleAsync(x => x.RoleId == stores && x.PageDefinitionId == warehouses);
        server.Execute("stores-grant-drift.sql", $"UPDATE advance.role_page_permissions SET \"CanPrint\"=NOT \"CanPrint\" WHERE \"Id\"='{permission.Id:D}';");
        server.AssertRejected("stores-grant-refuse-drift.sql", down, "rollback refuses a changed or missing permission");
        server.Execute("stores-grant-restore-drift.sql", $"UPDATE advance.role_page_permissions SET \"CanPrint\"=NOT \"CanPrint\" WHERE \"Id\"='{permission.Id:D}';");
        server.Execute("stores-grant-down.sql", down);
        Assert.False(await db.RolePagePermissions.AsNoTracking().AnyAsync(x => x.RoleId == stores && x.PageDefinitionId == warehouses));
        server.Execute("stores-grant-up.sql", up);
        Assert.Equal(6, await db.AuditLogs.CountAsync(x => x.CreatedBy == "StoresWarehouseRackGrants" && x.Action == "GrantStoresTopology"));
        server.Execute("stores-grant-down-for-site-grant.sql", down);
        permission.CanPrint = true;
        permission.CreatedBy = "SITE_PERMISSION_WITNESS";
        var siteJson = JsonSerializer.Serialize(permission).Replace("'", "''", StringComparison.Ordinal);
        server.Execute("stores-grant-site-grant.sql", "INSERT INTO advance.role_page_permissions SELECT (jsonb_populate_record(NULL::advance.role_page_permissions, '" + siteJson + "'::jsonb)).*;");
        server.Execute("stores-grant-preserve-site-up.sql", up);
        server.Execute("stores-grant-preserve-site-down.sql", down);
        var preserved = await db.RolePagePermissions.AsNoTracking().SingleAsync(x => x.Id == permission.Id);
        Assert.True(preserved.CanView && preserved.CanPrint);
        Assert.Equal("SITE_PERMISSION_WITNESS", preserved.CreatedBy);
        server.Execute("stores-grant-remove-site-fixture.sql", $"DELETE FROM advance.role_page_permissions WHERE \"Id\"='{permission.Id:D}' AND \"CreatedBy\"='SITE_PERMISSION_WITNESS';");
        server.Execute("stores-grant-final-up.sql", up);
    }
}
