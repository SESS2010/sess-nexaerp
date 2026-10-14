using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task ProveReceiptBarcodeScope(HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, Guid storesId, Guid assistantId, Guid accountsId, Guid storesManagerId, GoodsReceiptResult receipt)
    {
        var path = "/api/v1/stores/goods-receipts/gates/" + receipt.GateEntryId + "/scan-candidates";
        await using (var db = new NexaErpDbContext(options))
        {
            var item = await db.Items.SingleAsync(i => i.Id == receipt.Lines[0].ItemId);
            item.Barcode = "000123450067";
            await db.SaveChangesAsync();
        }
        foreach (var actor in new[] { (storesId, "SESS-35", "STORES_EXECUTIVE"), (assistantId, "SESS-16", "STORES_ASSISTANT") })
        {
            user.Set(actor.Item1, actor.Item2, actor.Item3);
            await AssertResolvedSeedRole(options, user, actor.Item3);
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var rows = await response.Content.ReadFromJsonAsync<List<GoodsReceiptBarcodeEndpoints.Candidate>>();
            Assert.NotNull(rows);
            Assert.Contains(rows, row => row.ItemId == receipt.Lines[0].ItemId && row.Barcode == "000123450067");
            Assert.All(rows, row => Assert.Contains(receipt.Lines, line => line.GateEntryLineId.ToString() == row.LineKey));
            user.SetOrganization("SESS_PROPRIETORSHIP");
            using var wrongCompany = await client.GetAsync(path);
            Assert.Contains(wrongCompany.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
            user.SetOrganization("SESS_PVT_LTD");
        }
        user.Set(storesId, "SESS-35", "STORES_EXECUTIVE");
        await AssertResolvedSeedRole(options, user, "STORES_EXECUTIVE");
        await using (var db = new NexaErpDbContext(options))
        {
            var companyId = await db.GateEntries.Where(g => g.Id == receipt.GateEntryId).Select(g => g.CompanyId).SingleAsync();
            // Disposable fixture only: close immutable effective versions, never reopen or rewrite their history.
            var today = await db.Database.SqlQueryRaw<DateOnly>("SELECT CURRENT_DATE AS \"Value\"").SingleAsync();
            var mappings = await db.EmployeeIdentityMappings.Where(m => m.CompanyId == companyId && m.EmployeeId == storesId && m.IsActive).ToListAsync();
            Assert.NotEmpty(mappings);
            foreach (var mapping in mappings) {
                mapping.IsActive = false; mapping.EffectiveTo = today; mapping.Version++;
                mapping.UpdatedAt = DateTimeOffset.UtcNow; mapping.UpdatedBy = "BARCODE_IDENTITY_WITNESS";
            }
            await db.SaveChangesAsync();
            using (var inactive = await client.GetAsync(path))
                Assert.Contains(inactive.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
            var resumedSubject = "barcode-stores-resumed";
            db.EmployeeIdentityMappings.Add(Mapping(companyId, storesId, resumedSubject));
            await db.SaveChangesAsync();
            user.RotateSubject(storesId, resumedSubject);
            Assert.All(await db.EmployeeIdentityMappings.Where(m => mappings.Select(old => old.Id).Contains(m.Id)).ToListAsync(), m => Assert.False(m.IsActive));
            var other = new SESS.NexaERP.Domain.Inventory.Warehouse { CompanyId = companyId,
                WarehouseCode = "BARCODE-OTHER", Name = "Other disposable warehouse", WarehouseType = "STORES", Status = "Active", ApprovalStatus = "Approved" };
            db.Warehouses.Add(other); await db.SaveChangesAsync();
            var scopes = await db.EmployeeOperationalScopes.Where(scope => scope.CompanyId == companyId && scope.EmployeeId == storesId && scope.IsActive).ToListAsync();
            Assert.NotEmpty(scopes);
            foreach (var scope in scopes) { scope.IsActive = false; scope.EffectiveTo = today; scope.Version++; scope.UpdatedAt = DateTimeOffset.UtcNow; scope.UpdatedBy = "BARCODE_SCOPE_WITNESS"; }
            await db.SaveChangesAsync();
            var otherScopes = scopes.Select(scope => new SESS.NexaERP.Domain.Authorization.EmployeeOperationalScope {
                CompanyId = scope.CompanyId, OrganizationId = scope.OrganizationId, EmployeeId = scope.EmployeeId,
                DepartmentId = scope.DepartmentId, WarehouseId = other.Id, RackBinId = null,
                OwnRecordsOnly = scope.OwnRecordsOnly, AllowsPrivilegedCrossScope = scope.AllowsPrivilegedCrossScope,
                EffectiveFrom = today, IsActive = true, Remarks = "Disposable other-warehouse barcode refusal", CreatedBy = "BARCODE_SCOPE_WITNESS"
            }).ToArray();
            db.EmployeeOperationalScopes.AddRange(otherScopes); await db.SaveChangesAsync();
            using (var wrongWarehouse = await client.GetAsync(path))
                Assert.Contains(wrongWarehouse.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
            foreach (var scope in otherScopes) { scope.IsActive = false; scope.EffectiveTo = today; scope.Version++; scope.UpdatedAt = DateTimeOffset.UtcNow; scope.UpdatedBy = "BARCODE_SCOPE_WITNESS"; }
            await db.SaveChangesAsync();
            db.EmployeeOperationalScopes.AddRange(scopes.Select(scope => new SESS.NexaERP.Domain.Authorization.EmployeeOperationalScope {
                CompanyId = scope.CompanyId, OrganizationId = scope.OrganizationId, EmployeeId = scope.EmployeeId,
                DepartmentId = scope.DepartmentId, WarehouseId = scope.WarehouseId, RackBinId = scope.RackBinId,
                OwnRecordsOnly = scope.OwnRecordsOnly, AllowsPrivilegedCrossScope = scope.AllowsPrivilegedCrossScope,
                EffectiveFrom = today, IsActive = true, Remarks = "Disposable restored barcode scope successor", CreatedBy = "BARCODE_SCOPE_WITNESS"
            }));
            await db.SaveChangesAsync();
            using var resumed = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);
        }
        using (var printDenied = await client.GetAsync("/api/v1/stores/barcodes/rack-labels"))
            Assert.Equal(HttpStatusCode.Forbidden, printDenied.StatusCode);
        user.Set(storesManagerId, "SESS-41", "STORES_MANAGER");
        await AssertResolvedSeedRole(options, user, "STORES_MANAGER");
        var rackLabels = await Get<RackBarcodeLabelEndpoints.RackLabel[]>(client, "/api/v1/stores/barcodes/rack-labels");
        Assert.NotEmpty(rackLabels);
        await using (var db = new NexaErpDbContext(options)) {
            var company = await db.Companies.Where(c => c.Code == "SESS_PVT_LTD").Select(c => c.Id).SingleAsync();
            var valid = await db.RackBins.Where(r => r.CompanyId == company && r.IsActive && r.ApprovalStatus == "Approved").Select(r => r.BinCode).ToListAsync();
            Assert.All(rackLabels, row => Assert.Contains(row.BinCode, valid));
        }
        user.Set(accountsId, "SESS-14", "ACCOUNTS_MANAGER");
        await AssertResolvedSeedRole(options, user, "ACCOUNTS_MANAGER");
        using var wrongRole = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Forbidden, wrongRole.StatusCode);
    }
}