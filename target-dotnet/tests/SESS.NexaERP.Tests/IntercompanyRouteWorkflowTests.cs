using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
    public async Task IntercompanyRoutesRequireRealPartiesSeparateApprovalAndCompanyScopedReads()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("intercompany-workflow-schema.sql", model.GetService<IMigrator>().GenerateScript("0", model.Database.GetMigrations().Last()));
        var seller = MultiCompanyFoundationSeedData.SessPvtLtdId;
        var buyer = MultiCompanyFoundationSeedData.SessProprietorshipId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Guid accountsId, directorId, storesId, supportId;
        IReadOnlyDictionary<string, EffectiveRoleAssignment> assignments;
        var sellerSite = new CompanySite { CompanyId = seller, Code = "IC-SELLER-SITE", Name = "Seller witness site", SiteType = "FACTORY", AddressLine1 = "Witness only", City = "Chennai", State = "Tamil Nadu", StateCode = "33", PostalCode = "600001" };
        var buyerSite = new CompanySite { CompanyId = buyer, Code = "IC-BUYER-SITE", Name = "Buyer witness site", SiteType = "FACTORY", AddressLine1 = "Witness only", City = "Chennai", State = "Tamil Nadu", StateCode = "33", PostalCode = "600002" };
        var sellerStore = new Warehouse { CompanyId = seller, WarehouseCode = "IC-SELLER", Name = "Seller witness warehouse", WarehouseType = "STORES", Status = MasterStatuses.Active, ApprovalStatus = MasterApprovalStatuses.Approved };
        var buyerStore = new Warehouse { CompanyId = buyer, WarehouseCode = "IC-BUYER", Name = "Buyer witness warehouse", WarehouseType = "STORES", Status = MasterStatuses.Active, ApprovalStatus = MasterApprovalStatuses.Approved };
        var sellerVendor = new Vendor { VendorCode = "IC-SELLER", Name = "Intercompany seller", LegalVendorName = "Intercompany seller", VendorType = "MATERIAL", VendorStatus = MasterStatuses.Active, ApprovalStatus = MasterApprovalStatuses.Approved };
        var buyerCustomer = new Customer { CustomerCode = "IC-BUYER", Name = "Intercompany buyer", LegalCustomerName = "Intercompany buyer", CustomerType = "BUSINESS", Status = MasterStatuses.Active, ApprovalStatus = MasterApprovalStatuses.Approved };
        Guid sellerGst, buyerGst;
        await using (var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options))
        {
            accountsId = await db.Employees.Where(e => e.EmployeeCode == "SESS-14").Select(e => e.Id).SingleAsync();
            directorId = await db.Employees.Where(e => e.EmployeeCode == "SESS-01").Select(e => e.Id).SingleAsync();
            storesId = await db.Employees.Where(e => e.EmployeeCode == "SESS-35").Select(e => e.Id).SingleAsync();
            await using var setupTx = await db.Database.BeginTransactionAsync();
            var authority = await db.EmployeeRoleAssignments.Where(a => a.CompanyId == seller && a.EmployeeId == directorId
                && a.Role!.Code == "TECHNICAL_DIRECTOR" && a.AssignmentType == "FULL" && a.EffectiveTo == null).Select(a => a.Id).SingleAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('sess.role_authority_assignment_id',{authority.ToString()},true)");
            supportId = await db.Employees.Where(e => e.EmployeeCode == "SESS-41").Select(e => e.Id).SingleAsync();
            await db.Employees.Where(e => e.Id == supportId).ExecuteUpdateAsync(s => s.SetProperty(e => e.LoginEnabled,true));
            foreach (var support in new[] { (supportId,"ACCOUNTS_MANAGER"),(storesId,"TECHNICAL_DIRECTOR") })
            {
                var roleId = await db.Roles.Where(r => r.Code == support.Item2).Select(r => r.Id).SingleAsync();
                Assert.False(await db.EmployeeRoleAssignments.AnyAsync(a => a.CompanyId == seller && a.EmployeeId == support.Item1 && a.RoleId == roleId));
                db.EmployeeRoleAssignments.Add(new EmployeeRoleAssignment { CompanyId = seller, EmployeeId = support.Item1,
                    RoleId = roleId, AssignmentType = "SUPPORT", EffectiveFrom = today, Remarks = "Disposable support authority witness" });
            }
            var registrations = await db.CompanyGstRegistrations.ToListAsync();
            var sg = registrations.Single(g => g.CompanyId == seller);
            var bg = registrations.Single(g => g.CompanyId == buyer);
            sellerGst = sg.Id; buyerGst = bg.Id;
            sellerVendor.GstNumber = sg.Gstin; buyerCustomer.GstNumber = bg.Gstin;
            db.CompanySites.AddRange(sellerSite, buyerSite);
            db.Warehouses.AddRange(sellerStore, buyerStore);
            db.Vendors.Add(sellerVendor); db.Customers.Add(buyerCustomer);
            db.VendorCompanyRelationships.Add(new VendorCompanyRelationship { CompanyId = buyer, VendorId = sellerVendor.Id, EffectiveFrom = today, ApprovedByEmployeeId = directorId, ApprovedAt = DateTimeOffset.UtcNow });
            db.CustomerCompanyRelationships.Add(new CustomerCompanyRelationship { CompanyId = seller, CustomerId = buyerCustomer.Id, EffectiveFrom = today, ApprovedByEmployeeId = directorId, ApprovedAt = DateTimeOffset.UtcNow });
            foreach (var company in new[] { seller, buyer })
                foreach (var employee in new[] { (accountsId, "SESS-14"), (directorId, "SESS-01"), (storesId, "SESS-35"), (supportId, "SESS-41") })
                {
                    var mapping = Mapping(company, employee.Item1, employee.Item2);
                    mapping.OrganizationId = company == seller ? "SESS_PVT_LTD" : "SESS_PROPRIETORSHIP";
                    db.EmployeeIdentityMappings.Add(mapping);
                }
            await db.SaveChangesAsync();
            await setupTx.CommitAsync();
            assignments = await db.EmployeeRoleAssignments.AsNoTracking().Include(a => a.Role)
                .Where(a => a.CompanyId == seller && a.EffectiveTo == null)
                .ToDictionaryAsync(a => TaxWorkflowUser.AssignmentKey(a.EmployeeId, a.Role!.Code),
                    a => new EffectiveRoleAssignment(a.Id, a.Role!.Code, a.AssignmentType));
        }
        const string password = "intercompany-http-runtime-123456789";
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, password);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        var runtime = new NpgsqlConnectionStringBuilder(server.ConnectionString) { Username = "nexa_erp_runtime", Password = password, Pooling = false }.ConnectionString;
        var user = new TaxWorkflowUser(accountsId, "SESS-14", "ACCOUNTS_MANAGER", assignments);
        await using var host = await PurchaseFlowHost.StartAsync(runtime, user, useRealPagePermissions: true);
        var client = host.Client;
        const string path = "/api/v1/stores/intercompany/routes";
        var options = await Get<JsonElement>(client, path + "/options");
        Assert.Contains(options.GetProperty("vendors").EnumerateArray(), v => v.GetProperty("id").GetGuid() == sellerVendor.Id);
        Assert.Contains(options.GetProperty("customers").EnumerateArray(), c => c.GetProperty("id").GetGuid() == buyerCustomer.Id);
        Assert.Contains(options.GetProperty("warehouses").EnumerateArray(), w => w.GetProperty("id").GetGuid() == buyerStore.Id);
        var proposal = new ProposeIntercompanyRouteRequest("IC-WITNESS-1", buyer, sellerSite.Id, buyerSite.Id, sellerStore.Id, buyerStore.Id,
            sellerGst, buyerGst, sellerVendor.Id, buyerCustomer.Id, today, null, "Witness approved company/site/warehouse route", "ic-propose-1");
        using (var wrongSite = await client.PostAsJsonAsync(path, proposal with { SellerSiteId = buyerSite.Id, IdempotencyKey = "ic-wrong-site" }))
            Assert.Equal(HttpStatusCode.Conflict, wrongSite.StatusCode);
        using (var sameCompany = await client.PostAsJsonAsync(path, proposal with { BuyerCompanyId = seller, IdempotencyKey = "ic-same-company" }))
            Assert.Equal(HttpStatusCode.Conflict, sameCompany.StatusCode);
        user.Set(storesId, "SESS-35", "STORES_EXECUTIVE");
        using (var forbidden = await client.PostAsJsonAsync(path, proposal)) Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        user.Set(supportId, "SESS-41", "ACCOUNTS_MANAGER");
        Assert.NotEmpty((await Get<JsonElement>(client,path+"/options")).GetProperty("vendors").EnumerateArray());
        var proposed = await Post<IntercompanyRouteView>(client, path, proposal);
        Assert.Equal("PROPOSED", proposed.Status); Assert.Equal(1u, proposed.Version);
        Assert.Equal(proposed.Id, (await Post<IntercompanyRouteView>(client, path, proposal)).Id);
        using (var changedReplay = await client.PostAsJsonAsync(path, proposal with { RouteCode = "CHANGED" }))
            Assert.Equal(HttpStatusCode.Conflict, changedReplay.StatusCode);
        using (var deniedApproval = await client.PostAsJsonAsync(path + "/" + proposed.Id + "/approve", new DecideIntercompanyRouteRequest(1, "Not authorized", "ic-denied")))
            Assert.Equal(HttpStatusCode.Forbidden, deniedApproval.StatusCode);
        var current = await Get<IntercompanyRouteView>(client, path + "/" + proposed.Id);
        Assert.Equal(proposed.Version, current.Version);
        user.Set(storesId,"SESS-35","TECHNICAL_DIRECTOR");
        using (var supportApproval = await client.PostAsJsonAsync(path+"/"+proposed.Id+"/approve",new DecideIntercompanyRouteRequest(current.Version,"Support may not approve","ic-support-approval")))
            Assert.Equal(HttpStatusCode.Forbidden,supportApproval.StatusCode);
        user.Set(directorId, "SESS-01", "TECHNICAL_DIRECTOR");
        var approved = await Post<IntercompanyRouteView>(client, path + "/" + proposed.Id + "/approve", new DecideIntercompanyRouteRequest(current.Version, "Route approved", "ic-approve"));
        Assert.Equal("APPROVED", approved.Status); Assert.Equal(2u, approved.Version);
        using (var stale = await client.PostAsJsonAsync(path + "/" + proposed.Id + "/revoke", new DecideIntercompanyRouteRequest(1, "Stale version", "ic-stale")))
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        user.SetOrganization("SESS_PROPRIETORSHIP");
        using (var otherCompany = await client.GetAsync(path + "/" + proposed.Id))
            Assert.Contains(otherCompany.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        user.SetOrganization("SESS_PVT_LTD");
        var revoked = await Post<IntercompanyRouteView>(client, path + "/" + proposed.Id + "/revoke", new DecideIntercompanyRouteRequest(2, "Stop future dispatch only", "ic-revoke"));
        Assert.Equal("REVOKED", revoked.Status); Assert.Equal(3u, revoked.Version);
        Assert.Equal(proposed.Definition.GetRawText(), revoked.Definition.GetRawText());
        Assert.Equal(2, revoked.History.GetArrayLength());
        server.AssertRejected("intercompany-history-immutable.sql",
            "UPDATE advance.intercompany_routes SET \"Remarks\"='rewritten';", "immutable");
        server.AssertRejected("intercompany-retained-down.sql",
            model.GetService<IMigrator>().GenerateScript("20260915180000_IntercompanyRoutes", "20260915103000_MachineDeliveryDossier"),
            "retained business evidence");
        server.Execute("intercompany-workflow-counts.sql", """
            DO $assert$ BEGIN
             IF (SELECT count(*) FROM advance.intercompany_routes)<>1 OR (SELECT count(*) FROM advance.intercompany_route_decisions)<>2
              OR EXISTS(SELECT 1 FROM advance.stock_movements)
             THEN RAISE EXCEPTION 'Route witness counts or no-stock-movement boundary differ.'; END IF;
            END $assert$;
            """);
    }
}
