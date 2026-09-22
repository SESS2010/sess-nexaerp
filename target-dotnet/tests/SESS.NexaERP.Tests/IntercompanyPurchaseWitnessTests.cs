using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{

    [Fact]
    public async Task IntercompanyPublicationUsesNormalIssuedPurchaseAndLimitsSellerDisclosure()
    {
        var seller = MultiCompanyFoundationSeedData.SessProprietorshipId;
        var buyer = MultiCompanyFoundationSeedData.SessPvtLtdId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sellerSite = new CompanySite { CompanyId = seller, Code = "IC-PO-SELLER", Name = "Seller witness site", SiteType = "FACTORY", AddressLine1 = "Witness only", City = "Chennai", State = "Tamil Nadu", StateCode = "33", PostalCode = "600001" };
        var buyerSite = new CompanySite { CompanyId = buyer, Code = "IC-PO-BUYER", Name = "Buyer witness site", SiteType = "FACTORY", AddressLine1 = "Witness only", City = "Chennai", State = "Tamil Nadu", StateCode = "33", PostalCode = "600002" };
        var sellerStore = new Warehouse { CompanyId = seller, WarehouseCode = "IC-PO-SELLER", Name = "Seller witness warehouse", WarehouseType = "STORES", Status = MasterStatuses.Active, ApprovalStatus = MasterApprovalStatuses.Approved };
        var customer = new Customer { CustomerCode = "IC-PO-BUYER", Name = "Buyer company", LegalCustomerName = "Buyer company", CustomerType = "BUSINESS", Status = MasterStatuses.Active, ApprovalStatus = MasterApprovalStatuses.Approved };
        Guid sellerGst = default, buyerGst = default, sellerVendor = default, buyerStore = default, director = default, supportPurchase = default;
        var publications = 0;
        await RunCompletePurchaseFlow(intercompanySetup: async options =>
        {
            await using var db = new NexaErpDbContext(options);
            var registrations = await db.CompanyGstRegistrations.ToListAsync();
            var sg = registrations.Single(g => g.CompanyId == seller);
            var bg = registrations.Single(g => g.CompanyId == buyer);
            sellerGst = sg.Id; buyerGst = bg.Id; customer.GstNumber = bg.Gstin;
            var vendor = await db.Vendors.SingleAsync(v => v.VendorCode == "TRIAL-VEN-001");
            // Only disposable master data changes, before any quotation or PO is created.
            vendor.GstNumber = sg.Gstin; vendor.State = "Tamil Nadu"; vendor.StateCode = "33";
            sellerVendor = vendor.Id;
            buyerStore = await db.Warehouses.Where(w => w.WarehouseCode == "TRIAL-WH-C01").Select(w => w.Id).SingleAsync();
            director = await db.Employees.Where(e => e.EmployeeCode == "SESS-01").Select(e => e.Id).SingleAsync();
            await using var setupTx = await db.Database.BeginTransactionAsync();
            var authority = await db.EmployeeRoleAssignments.Where(a => a.CompanyId == buyer && a.EmployeeId == director
                && a.Role!.Code == "TECHNICAL_DIRECTOR" && a.AssignmentType == "FULL" && a.EffectiveTo == null).Select(a => a.Id).SingleAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('sess.role_authority_assignment_id',{authority.ToString()},true)");
            supportPurchase = await db.Employees.Where(e => e.EmployeeCode == "SESS-41").Select(e => e.Id).SingleAsync();
            var purchaseRole = await db.Roles.Where(r => r.Code == "PURCHASE_MANAGER").Select(r => r.Id).SingleAsync();
            Assert.False(await db.EmployeeRoleAssignments.AnyAsync(a => a.CompanyId == buyer && a.EmployeeId == supportPurchase && a.RoleId == purchaseRole));
            db.EmployeeRoleAssignments.Add(new EmployeeRoleAssignment { CompanyId = buyer, EmployeeId = supportPurchase,
                RoleId = purchaseRole, AssignmentType = "SUPPORT", EffectiveFrom = today, Remarks = "Disposable publication preparer" });
            db.CompanySites.AddRange(sellerSite, buyerSite); db.Warehouses.Add(sellerStore); db.Customers.Add(customer);
            db.CustomerCompanyRelationships.Add(new CustomerCompanyRelationship { CompanyId = seller, CustomerId = customer.Id, EffectiveFrom = today, ApprovedByEmployeeId = director, ApprovedAt = DateTimeOffset.UtcNow });
            if (!await db.VendorCompanyRelationships.AnyAsync(r => r.CompanyId == buyer && r.VendorId == vendor.Id))
                db.VendorCompanyRelationships.Add(new VendorCompanyRelationship { CompanyId = buyer, VendorId = vendor.Id, EffectiveFrom = today, ApprovedByEmployeeId = director, ApprovedAt = DateTimeOffset.UtcNow });
            foreach (var code in new[] { "SESS-14", "SESS-01" })
            {
                var employee = await db.Employees.Where(e => e.EmployeeCode == code).Select(e => e.Id).SingleAsync();
                var mapping = Mapping(seller, employee, code); mapping.OrganizationId = "SESS_PROPRIETORSHIP";
                db.EmployeeIdentityMappings.Add(mapping);
                var roleCode = code == "SESS-14" ? "ACCOUNTS_MANAGER" : "TECHNICAL_DIRECTOR";
                var original = await db.EmployeeRoleAssignments.Include(a => a.Role).SingleAsync(a => a.CompanyId == buyer && a.EmployeeId == employee && a.Role!.Code == roleCode && a.EffectiveTo == null);
                if (!await db.EmployeeRoleAssignments.AnyAsync(a => a.CompanyId == seller && a.EmployeeId == employee && a.RoleId == original.RoleId && a.EffectiveTo == null))
                    db.EmployeeRoleAssignments.Add(new EmployeeRoleAssignment { CompanyId = seller, EmployeeId = employee, RoleId = original.RoleId, EffectiveFrom = today, AssignmentType = "FULL", Remarks = "Disposable intercompany witness" });
            }
            await db.SaveChangesAsync();
            await setupTx.CommitAsync();
        }, intercompanyPurchase: async context =>
        {
            if (context.Band != "LOW" || context.Stage != "ISSUED") return;
            await using var db = new NexaErpDbContext(context.Options);
            var assignments = await db.EmployeeRoleAssignments.AsNoTracking().Include(a => a.Role)
                .Where(a => a.CompanyId == seller && a.EffectiveTo == null)
                .ToDictionaryAsync(a => TaxWorkflowUser.AssignmentKey(a.EmployeeId,a.Role!.Code), a => new EffectiveRoleAssignment(a.Id,a.Role!.Code,a.AssignmentType));
            var sellerUser = new TaxWorkflowUser(context.AccountsId,"SESS-14","ACCOUNTS_MANAGER",assignments);
            sellerUser.SetOrganization("SESS_PROPRIETORSHIP");
            await using var sellerHost = await PurchaseFlowHost.StartAsync(context.RuntimeConnection,sellerUser,useRealPagePermissions:true);
            await using var buyerHost = await PurchaseFlowHost.StartAsync(context.RuntimeConnection,context.User,useRealPagePermissions:true,useRealOperationalScopes:true);
            const string routes = "/api/v1/stores/intercompany/routes";
            const string purchases = "/api/v1/stores/intercompany/purchases";
            var route = await Post<IntercompanyRouteView>(sellerHost.Client,routes,new ProposeIntercompanyRouteRequest(
                "IC-PO-WITNESS",buyer,sellerSite.Id,buyerSite.Id,sellerStore.Id,buyerStore,sellerGst,buyerGst,sellerVendor,customer.Id,today,null,"Real sale route","ic-po-route"));
            sellerUser.Set(director,"SESS-01","TECHNICAL_DIRECTOR");
            route = await Post<IntercompanyRouteView>(sellerHost.Client,routes+"/"+route.Id+"/approve",new DecideIntercompanyRouteRequest(route.Version,"Approved separate companies","ic-po-approve"));
            var po = await db.PurchaseOrders.AsNoTracking().Include(p => p.Lines).SingleAsync(p => p.Id == context.PurchaseOrderId);
            var poLine = Assert.Single(po.Lines);
            var beforeMovements = await db.StockMovements.CountAsync();
            var user = context.User;
            user.Set(supportPurchase,"SESS-41","PURCHASE_MANAGER");
            var options = await Get<JsonElement>(buyerHost.Client,purchases+"/options");
            var option = Assert.Single(options.GetProperty("orders").EnumerateArray());
            Assert.Equal(po.Id,option.GetProperty("purchaseOrderId").GetGuid());
            Assert.Equal(route.Id,option.GetProperty("routeId").GetGuid());
            var request = new PublishIntercompanyPurchaseRequest(route.Id,po.Id,option.GetProperty("version").GetUInt32(),"Publish approved commercial order","ic-po-publish");
            using (var stale = await buyerHost.Client.PostAsJsonAsync(purchases,request with { Version = request.Version-1,IdempotencyKey = "ic-po-stale" }))
                Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);
            user.Set(context.AccountsId,"SESS-14","ACCOUNTS_MANAGER");
            using (var denied = await buyerHost.Client.PostAsJsonAsync(purchases,request)) Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
            user.Set(supportPurchase,"SESS-41","PURCHASE_MANAGER");
            var published = await Post<IntercompanyPurchaseView>(buyerHost.Client,purchases,request);
            Assert.Equal(po.Id,published.PurchaseOrderId); Assert.Equal("CURRENT",published.Eligibility);
            Assert.Equal(published.CorrelationId,(await Post<IntercompanyPurchaseView>(buyerHost.Client,purchases,request)).CorrelationId);
            using (var changed = await buyerHost.Client.PostAsJsonAsync(purchases,request with { Remarks = "Changed payload" })) Assert.Equal(HttpStatusCode.Conflict,changed.StatusCode);
            using (var duplicate = await buyerHost.Client.PostAsJsonAsync(purchases,request with { IdempotencyKey = "ic-po-duplicate" })) Assert.Equal(HttpStatusCode.Conflict,duplicate.StatusCode);
            Assert.Empty((await Get<JsonElement>(buyerHost.Client,purchases+"/options")).GetProperty("orders").EnumerateArray());
            sellerUser.Set(context.AccountsId,"SESS-14","ACCOUNTS_MANAGER");
            var received = await Get<IntercompanyPurchaseView>(sellerHost.Client,purchases+"/"+published.CorrelationId);
            Assert.Equal(seller,received.CompanyId); Assert.Null(received.PurchaseOrderId);
            Assert.Equal(published.CommercialOrder.GetRawText(),received.CommercialOrder.GetRawText());
            var publicLine = Assert.Single(received.CommercialOrder.GetProperty("lines").EnumerateArray());
            Assert.Equal(poLine.ItemId,publicLine.GetProperty("itemId").GetGuid());
            Assert.Equal(poLine.TotalPayableValue,publicLine.GetProperty("totalPayableValue").GetDecimal());
            Assert.Equal(JsonValueKind.Object,publicLine.GetProperty("commercial").ValueKind);
            foreach (var internalId in new[] { po.Id,poLine.Id,poLine.CommercialComparisonLineId,poLine.PurchaseRequisitionLineId })
                Assert.DoesNotContain(internalId.ToString(),received.CommercialOrder.GetRawText(),StringComparison.OrdinalIgnoreCase);
            var sellerPage = await Get<PagedResponse<IntercompanyPurchaseView>>(sellerHost.Client,purchases);
            Assert.Equal(published.CorrelationId,Assert.Single(sellerPage.Items).CorrelationId);
            using (var privatePo = await sellerHost.Client.GetAsync("/api/v1/purchase/purchase-orders/"+po.PoNumber))
                Assert.Contains(privatePo.StatusCode,new[] { HttpStatusCode.Forbidden,HttpStatusCode.NotFound });
            const string invoices = "/api/v1/accounts/intercompany-invoices";
            var invoiceRequest = new RecordIntercompanyInvoiceRequest(published.CorrelationId, "IC-LOW-2026-0001", today,
                new("intercompany-gst-invoice.pdf", "application/pdf", SupplierInvoiceFixturePdf("INTERCOMPANY GST")), "ic-invoice-record");
            sellerUser.Set(director, "SESS-01", "TECHNICAL_DIRECTOR");
            using (var forbiddenInvoice = await sellerHost.Client.PostAsJsonAsync(invoices, invoiceRequest))
                Assert.Equal(HttpStatusCode.Forbidden, forbiddenInvoice.StatusCode);
            sellerUser.Set(context.AccountsId, "SESS-14", "ACCOUNTS_MANAGER");
            var invoice = await Post<IntercompanyInvoiceView>(sellerHost.Client, invoices, invoiceRequest);
            Assert.Equal(seller, invoice.CompanyId);
            Assert.Equal(seller, invoice.SellerCompanyId);
            Assert.Equal(buyer, invoice.BuyerCompanyId);
            Assert.Equal(published.CorrelationId, invoice.CorrelationId);
            Assert.Equal(received.CommercialOrder.GetRawText(), invoice.OrderSnapshot.GetRawText());
            Assert.False(invoice.Replayed);
            var invoiceReplay = await Post<IntercompanyInvoiceView>(sellerHost.Client, invoices, invoiceRequest);
            Assert.True(invoiceReplay.Replayed);
            Assert.Equal(invoice.Id, invoiceReplay.Id);
            using (var changedInvoice = await sellerHost.Client.PostAsJsonAsync(invoices,
                invoiceRequest with { InvoiceNumber = "IC-DIFFERENT" }))
                Assert.Equal(HttpStatusCode.Conflict, changedInvoice.StatusCode);
            using (var duplicateInvoice = await sellerHost.Client.PostAsJsonAsync(invoices,
                invoiceRequest with { IdempotencyKey = "ic-invoice-duplicate" }))
                Assert.Equal(HttpStatusCode.Conflict, duplicateInvoice.StatusCode);
            using (var invalidFile = await sellerHost.Client.PostAsJsonAsync(invoices,
                invoiceRequest with { Evidence = new("invalid.pdf", "application/pdf", [1, 2, 3]), IdempotencyKey = "ic-invalid-file" }))
                Assert.Equal(HttpStatusCode.BadRequest, invalidFile.StatusCode);
            Assert.Equal(invoice.Id, Assert.Single(await Get<List<IntercompanyInvoiceView>>(sellerHost.Client,
                invoices + "/for-purchase/" + published.CorrelationId)).Id);
            using (var download = await sellerHost.Client.GetAsync(invoices + "/" + invoice.Id + "/evidence"))
            {
                Assert.Equal(HttpStatusCode.OK, download.StatusCode);
                Assert.Equal(invoiceRequest.Evidence.Content, await download.Content.ReadAsByteArrayAsync());
                Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
            }
            user.Set(context.AccountsId, "SESS-14", "ACCOUNTS_MANAGER");
            var buyerInvoice = await Get<IntercompanyInvoiceView>(buyerHost.Client, invoices + "/" + invoice.Id);
            Assert.Equal(buyer, buyerInvoice.CompanyId);
            Assert.Equal(invoice.OrderSnapshot.GetRawText(), buyerInvoice.OrderSnapshot.GetRawText());
            Assert.Equal(invoice.Evidence.Sha256, buyerInvoice.Evidence.Sha256);
            using (var wrongCompanyRecord = await buyerHost.Client.PostAsJsonAsync(invoices,
                invoiceRequest with { IdempotencyKey = "ic-buyer-cannot-record-sale" }))
                Assert.Equal(HttpStatusCode.Conflict, wrongCompanyRecord.StatusCode);
            Assert.Empty(await Get<List<IntercompanyInvoiceView>>(buyerHost.Client, invoices + "/for-purchase/" + Guid.NewGuid()));
            await using (var privateRead = new NpgsqlConnection(context.RuntimeConnection))
            {
                await privateRead.OpenAsync();
                await using var raw = new NpgsqlCommand("SELECT * FROM advance.intercompany_invoice_evidence", privateRead);
                var refused = await Assert.ThrowsAsync<PostgresException>(async () => await raw.ExecuteNonQueryAsync());
                Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, refused.SqlState);
            }
            var immutableUpdate = await Assert.ThrowsAsync<PostgresException>(async () =>
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE advance.intercompany_invoice_evidence SET \"InvoiceNumber\"='changed' WHERE \"Id\"={invoice.Id}"));
            Assert.Equal(PostgresErrorCodes.RaiseException, immutableUpdate.SqlState);
            Assert.Contains("immutable", immutableUpdate.MessageText, StringComparison.OrdinalIgnoreCase);
            var immutableDelete = await Assert.ThrowsAsync<PostgresException>(async () =>
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM advance.intercompany_invoice_evidence WHERE \"Id\"={invoice.Id}"));
            Assert.Equal(PostgresErrorCodes.RaiseException, immutableDelete.SqlState);
            user.Set(supportPurchase, "SESS-41", "PURCHASE_MANAGER");
            sellerUser.Set(director,"SESS-01","TECHNICAL_DIRECTOR");
            await Post<IntercompanyRouteView>(sellerHost.Client,routes+"/"+route.Id+"/revoke",new DecideIntercompanyRouteRequest(route.Version,"Stop new business, keep evidence","ic-po-revoke"));
            sellerUser.Set(context.AccountsId,"SESS-14","ACCOUNTS_MANAGER");
            var retained = await Get<IntercompanyPurchaseView>(sellerHost.Client,purchases+"/"+published.CorrelationId);
            Assert.Equal("REFRESH_REQUIRED",retained.Eligibility);
            Assert.Equal(received.CommercialOrder.GetRawText(),retained.CommercialOrder.GetRawText());
            var retainedInvoice = await Get<IntercompanyInvoiceView>(sellerHost.Client, invoices + "/" + invoice.Id);
            Assert.Equal(invoice.OrderSnapshot.GetRawText(), retainedInvoice.OrderSnapshot.GetRawText());
            Assert.True((await Post<IntercompanyInvoiceView>(sellerHost.Client, invoices, invoiceRequest)).Replayed);
            using (var revokedRouteInvoice = await sellerHost.Client.PostAsJsonAsync(invoices,
                invoiceRequest with { InvoiceNumber = "IC-AFTER-REVOKE", IdempotencyKey = "ic-after-revoke" }))
                Assert.Equal(HttpStatusCode.Conflict, revokedRouteInvoice.StatusCode);
            Assert.Equal(published.CorrelationId,(await Post<IntercompanyPurchaseView>(buyerHost.Client,purchases,request)).CorrelationId);
            Assert.Equal(beforeMovements,await db.StockMovements.CountAsync());
            await db.Database.OpenConnectionAsync();
            await using var count = db.Database.GetDbConnection().CreateCommand();
            count.CommandText = "SELECT count(*) FROM advance.intercompany_purchase_publications";
            Assert.Equal(1L,await count.ExecuteScalarAsync());
            publications++;
            user.Set(context.PurchaseId,"SESS-15","PURCHASE_MANAGER","PURCHASE_EXECUTIVE","PURCHASE_MANAGER","STORES_EXECUTIVE");
        });
        Assert.Equal(1,publications);
    }

}