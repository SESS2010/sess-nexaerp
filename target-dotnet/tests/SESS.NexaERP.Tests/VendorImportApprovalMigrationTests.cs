using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure;
using SESS.NexaERP.Infrastructure.Audit;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.MasterData;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task Vendor_import_source_approval_persists_without_bypassing_ERP_and_refuses_loss_on_rollback()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260929130000_R1RemainingDirectorViews";
        const string target = "20260929143000_VendorImportSourceApproval";
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("vendor-import-baseline.sql", migrator.GenerateScript("0", previous));
        var up = migrator.GenerateScript(previous, target); var down = migrator.GenerateScript(target, previous);
        server.Execute("vendor-import-up.sql", up);
        server.Execute("vendor-import-empty-down.sql", down);
        server.Execute("vendor-import-up-again.sql", up);
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options);
        var user = new LifecycleUser("vendor-import-operator");
        var service = new EfVendorMasterDataService(db, user, new EfAuditWriter(db, user), new EfPagePermissionService(db), new SystemClock());
        var adapter = new VendorMasterDataAdapter(service);
        // Synthetic 89-row cohort: source Approved and Pending are both retained, with optional GSTIN.
        for (var i = 1; i <= 89; i++)
        {
            var row = new MasterDataRawRow(i + 1, new Dictionary<string, string?>
            {
                ["VendorCode"] = $"TEST-V-{i:0000}", ["LegalVendorName"] = $"Synthetic vendor {i}",
                ["VendorType"] = "MATERIAL", ["MsmeStatus"] = "FALSE", ["Country"] = i == 1 ? "China" : "India",
                ["State"] = i == 1 ? "Guangdong" : null,
                ["LegacyApprovalStatus"] = i % 2 == 0 ? "Pending" : "Approved",
                ["LegacyApprovedDate"] = i % 2 == 0 ? null : "29-09-2026"
            });
            Assert.Empty(adapter.Validate(row, null, null));
            await adapter.CreateAsync(row, default);
        }
        db.ChangeTracker.Clear();
        var vendors = await db.Vendors.Where(x => x.VendorCode.StartsWith("TEST-V-")).ToListAsync();
        Assert.Equal(89, vendors.Count);
        Assert.Equal(45, vendors.Count(x => x.LegacyApprovalStatus == "Approved"));
        Assert.Equal(44, vendors.Count(x => x.LegacyApprovalStatus == "Pending"));
        Assert.All(vendors, x => { Assert.Equal(MasterApprovalStatuses.Draft, x.ApprovalStatus); Assert.Null(x.GstNumber);
            Assert.Null(x.ApprovedAt); Assert.False(VendorQualification.IsVendorEligible(x, DateOnly.FromDateTime(DateTime.UtcNow))); });
        Assert.All(vendors.Where(x => x.LegacyApprovalStatus == "Approved"), x => Assert.Equal(new DateOnly(2026, 9, 29), x.LegacyApprovedDate));
        var vendor = vendors.Single(x => x.VendorCode == "TEST-V-0001");
        Assert.Equal("China", vendor.Country); Assert.Equal("Guangdong", vendor.State);
        var existing = (await adapter.LoadExistingAsync([vendor.VendorCode], [], default)).ByCode[vendor.VendorCode];
        var replay = new MasterDataRawRow(2, existing.MaterialValues);
        Assert.True(adapter.IsMateriallyEqual(replay, existing));
        var blank = existing.MaterialValues.ToDictionary(x => x.Key, x => x.Value);
        blank["LegacyApprovalStatus"] = null; blank["LegacyApprovedDate"] = null;
        Assert.True(adapter.IsMateriallyEqual(new(2, blank), existing));
        blank["ContactPerson"] = "Synthetic contact";
        await adapter.UpdateAsync(existing, new(2, blank), existing.Version, default);
        Assert.Equal("Approved", vendor.LegacyApprovalStatus);
        Assert.Equal(new DateOnly(2026, 9, 29), vendor.LegacyApprovedDate);
        // An independently ERP-approved vendor becomes ineligible when the source status is changed to Pending.
        vendor.VendorStatus = MasterStatuses.Active; vendor.ApprovalStatus = MasterApprovalStatuses.Approved;
        vendor.CommercialVerificationStatus = MasterApprovalStatuses.Approved;
        await db.SaveChangesAsync();
        Assert.True(VendorQualification.IsVendorEligible(vendor, DateOnly.FromDateTime(DateTime.UtcNow)));
        existing = (await adapter.LoadExistingAsync([vendor.VendorCode], [], default)).ByCode[vendor.VendorCode];
        var pending = existing.MaterialValues.ToDictionary(x => x.Key, x => x.Value); pending["LegacyApprovalStatus"] = "Pending";
        await adapter.UpdateAsync(existing, new(2, pending), existing.Version, default);
        Assert.Equal(MasterApprovalStatuses.PendingApproval, vendor.ApprovalStatus);
        Assert.False(VendorQualification.IsVendorEligible(vendor, DateOnly.FromDateTime(DateTime.UtcNow)));
        Assert.True(await db.MasterApprovalHistories.AnyAsync(x => x.MasterId == vendor.Id));
        var history = await db.ControlledConfigurationHistories.SingleAsync(x => x.EntityId == vendor.Id);
        Assert.Equal(await db.Companies.Where(x => x.Code == user.OrganizationId).Select(x => x.Id).SingleAsync(), history.CompanyId);
        Assert.Equal(user.OrganizationId, history.OrganizationId);
        // A later genuine ERP approval is not revoked by replaying the unchanged historical fact.
        vendor.VendorStatus = MasterStatuses.Active; vendor.ApprovalStatus = MasterApprovalStatuses.Approved;
        vendor.CommercialVerificationStatus = MasterApprovalStatuses.Approved;
        await db.SaveChangesAsync();
        existing = (await adapter.LoadExistingAsync([vendor.VendorCode], [], default)).ByCode[vendor.VendorCode];
        Assert.True(adapter.IsMateriallyEqual(new(2, existing.MaterialValues), existing));
        Assert.True(VendorQualification.IsVendorEligible(vendor, DateOnly.FromDateTime(DateTime.UtcNow)));
        // Source provenance must never promote a blacklisted vendor into an approvable lifecycle.
        vendor.VendorStatus = MasterStatuses.Blacklisted; vendor.LegacyApprovalStatus = "Approved";
        await db.SaveChangesAsync();
        existing = (await adapter.LoadExistingAsync([vendor.VendorCode], [], default)).ByCode[vendor.VendorCode];
        pending = existing.MaterialValues.ToDictionary(x => x.Key, x => x.Value); pending["LegacyApprovalStatus"] = "Pending";
        await adapter.UpdateAsync(existing, new(2, pending), existing.Version, default);
        Assert.Equal(MasterStatuses.Blacklisted, vendor.VendorStatus);
        Assert.False(VendorQualification.IsVendorEligible(vendor, DateOnly.FromDateTime(DateTime.UtcNow)));
        server.AssertRejected("vendor-import-down-refused.sql", down, "refuses to discard imported approval facts");
        server.Execute("vendor-import-test-cleanup.sql", """UPDATE advance.vendors SET "LegacyApprovalStatus"=NULL,"LegacyApprovedDate"=NULL WHERE "VendorCode" LIKE 'TEST-V-%';""");
        server.Execute("vendor-import-final-down.sql", down);
        server.Execute("vendor-import-final-up.sql", up);
    }
}
