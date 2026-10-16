using System.Net;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed class ReceiptScopeBoundaryClock : IDateTimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public DateTimeOffset UtcNow => Now;
    }

    private static async Task ProveBarcodeScopeBusinessDateBoundaries(DbContextOptions<NexaErpDbContext> options,
        string runtimeConnection, TaxWorkflowUser user, Guid employeeId, string path, IReadOnlyList<EmployeeOperationalScope>? restoreScopes = null)
    {
        await using var db = new NexaErpDbContext(options);
        var today = await db.Database.SqlQueryRaw<DateOnly>("SELECT CURRENT_DATE AS \"Value\"").SingleAsync();
        var original = await db.EmployeeOperationalScopes.AsNoTracking().Where(s => s.EmployeeId == employeeId &&
            s.OrganizationId == user.OrganizationId && s.IsActive && s.EffectiveFrom <= today && (s.EffectiveTo == null || s.EffectiveTo >= today)).ToListAsync();
        Assert.NotEmpty(original);
        var tomorrow = today.AddDays(1);
        var clock = new ReceiptScopeBoundaryClock();
        await using var host = await PurchaseFlowHost.StartAsync(runtimeConnection, user,
            useRealPagePermissions: true, useRealOperationalScopes: true, dateTimeProvider: clock);

        async Task CloseActiveAsync()
        {
            var active = await db.EmployeeOperationalScopes.Where(s => s.EmployeeId == employeeId &&
                s.OrganizationId == user.OrganizationId && s.IsActive && s.EffectiveTo == null).ToListAsync();
            var lastClose = await db.EmployeeOperationalScopes.Where(s => s.EmployeeId == employeeId && s.EffectiveTo != null)
                .MaxAsync(s => s.EffectiveTo) ?? today;
            foreach (var scope in active)
            {
                scope.IsActive = false;
                lastClose = (lastClose > scope.EffectiveFrom ? lastClose : scope.EffectiveFrom).AddDays(1);
                scope.EffectiveTo = lastClose;
                scope.Version++; scope.UpdatedAt = DateTimeOffset.UtcNow; scope.UpdatedBy = "BARCODE_DATE_BOUNDARY";
            }
            await db.SaveChangesAsync();
        }
        EmployeeOperationalScope Copy(EmployeeOperationalScope scope, DateOnly from, DateOnly? to) => new()
        {
            CompanyId = scope.CompanyId, OrganizationId = scope.OrganizationId, EmployeeId = scope.EmployeeId,
            DepartmentId = scope.DepartmentId, WarehouseId = scope.WarehouseId, RackBinId = scope.RackBinId,
            OwnRecordsOnly = scope.OwnRecordsOnly, AllowsPrivilegedCrossScope = scope.AllowsPrivilegedCrossScope,
            EffectiveFrom = from, EffectiveTo = to, IsActive = true,
            Remarks = "Disposable barcode scope date boundary", CreatedBy = "BARCODE_DATE_BOUNDARY"
        };
        async Task AssertAt(DateTime ist, HttpStatusCode expected)
        {
            clock.Now = new DateTimeOffset(ist, TimeSpan.FromMinutes(330)).ToUniversalTime();
            using var response = await host.Client.GetAsync(path);
            var payload = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"{path} at IST {ist:O}: expected {expected}, got {response.StatusCode}: {payload}");
            if (expected == HttpStatusCode.OK) Assert.True(System.Text.Json.JsonDocument.Parse(payload).RootElement.GetArrayLength() > 0, path);
        }
        var midnight = tomorrow.ToDateTime(TimeOnly.MinValue);
        try
        {
            await CloseActiveAsync();
            db.EmployeeOperationalScopes.AddRange(original.Select(scope => Copy(scope, tomorrow, null)));
            await db.SaveChangesAsync();
            await AssertAt(midnight.AddSeconds(-1), HttpStatusCode.Forbidden);
            await AssertAt(midnight, HttpStatusCode.OK);
            await AssertAt(midnight.AddHours(5.5).AddSeconds(-1), HttpStatusCode.OK);
            await AssertAt(midnight.AddHours(5.5), HttpStatusCode.OK);
            user.SetOrganization("SESS_PROPRIETORSHIP");
            using (var wrongCompany = await host.Client.GetAsync(path))
            {
                if (path == "/api/v1/stores/barcodes/rack-labels")
                {
                    // A company catalogue returns an empty list, not a document-not-found response.
                    // This fixture has no matching PROP identity, so no PVT labels may leak into it.
                    Assert.Equal(HttpStatusCode.OK, wrongCompany.StatusCode);
                    using var payload = System.Text.Json.JsonDocument.Parse(await wrongCompany.Content.ReadAsStringAsync());
                    Assert.Equal(0, payload.RootElement.GetArrayLength());
                }
                else Assert.Contains(wrongCompany.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
            }
            user.SetOrganization("BARCODE_NO_COMPANY");
            using (var unknownCompany = await host.Client.GetAsync(path))
                Assert.Contains(unknownCompany.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
            user.SetOrganization("SESS_PVT_LTD");

            await CloseActiveAsync();
            var historyCount = await db.EmployeeOperationalScopes.CountAsync(s => s.EmployeeId == employeeId);
            db.EmployeeOperationalScopes.AddRange(original.Select(scope => Copy(scope, today.AddDays(-100 - historyCount), today.AddDays(-1))));
            await db.SaveChangesAsync();
            midnight = today.ToDateTime(TimeOnly.MinValue);
            await AssertAt(midnight.AddSeconds(-1), HttpStatusCode.OK); // EffectiveTo is inclusive in IST.
            await AssertAt(midnight, HttpStatusCode.Forbidden);
            await AssertAt(midnight.AddHours(5.5).AddSeconds(-1), HttpStatusCode.Forbidden);
            await AssertAt(midnight.AddHours(5.5), HttpStatusCode.Forbidden);
        }
        finally
        {
            user.SetOrganization("SESS_PVT_LTD");
            await CloseActiveAsync();
            // Restore the input fixture after the explicit future/past-date assertions, without reopening old versions.
            db.EmployeeOperationalScopes.AddRange((restoreScopes ?? original).Select(scope => Copy(scope, scope.EffectiveFrom, scope.EffectiveTo)));
            await db.SaveChangesAsync();
        }
    }

    private static async Task ProveOtherBarcodeScopeDateBoundaries(DbContextOptions<NexaErpDbContext> options,
        string runtimeConnection, TaxWorkflowUser user, Guid storesId, Guid storesManagerId,
        SESS.NexaERP.Application.Stores.GoodsReceiptResult receipt)
    {
        await using var db = new NexaErpDbContext(options);
        var company = await db.GateEntries.Where(g => g.Id == receipt.GateEntryId).Select(g => g.CompanyId).SingleAsync();
        var itemCode = await db.Items.Where(i => i.Id == receipt.Lines[0].ItemId).Select(i => i.ItemCode).SingleAsync();
        var ownIssue = await db.MaterialIssues.Where(i => i.CompanyId == company && db.Employees.Any(e =>
            e.Id == i.IssuedToEmployeeId && e.EmployeeCode == "SESS-05"))
            .OrderBy(i => i.Id).Select(i => new { i.Id, i.IssuedToEmployeeId }).FirstAsync();

        user.Set(ownIssue.IssuedToEmployeeId, "SESS-05", "TECHNICAL_SUPPORT_MANAGER", "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        await ProveBarcodeScopeBusinessDateBoundaries(options, runtimeConnection, user, ownIssue.IssuedToEmployeeId,
            "/api/v1/stores/barcodes/request-items?scan=" + Uri.EscapeDataString(itemCode));
        await ProveBarcodeScopeBusinessDateBoundaries(options, runtimeConnection, user, ownIssue.IssuedToEmployeeId,
            "/api/v1/stores/barcodes/return-issues/" + ownIssue.Id);

        user.Set(storesManagerId, "SESS-41", "STORES_MANAGER");
        await ProveBarcodeScopeBusinessDateBoundaries(options, runtimeConnection, user, storesManagerId,
            "/api/v1/stores/barcodes/rack-labels");
    }
}
