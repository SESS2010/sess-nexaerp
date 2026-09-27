using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Audit;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

/// <summary>
/// R1 (26 Sep). Reactivate used to set VendorStatus "Approved", which RFQ and PO eligibility never
/// accept, so a held-then-reactivated vendor was unusable for good. Lifecycle actions also had no
/// from-state check: a blacklisted vendor could be approved.
/// </summary>
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Theory]
    [InlineData("Approve", MasterStatuses.PendingApproval, true)]
    [InlineData("Approve", MasterStatuses.Blacklisted, false)]
    [InlineData("Approve", MasterStatuses.OnHold, false)]
    [InlineData("Approve", MasterStatuses.Active, false)]
    [InlineData("Hold", MasterStatuses.Active, true)]
    [InlineData("Hold", MasterStatuses.Draft, false)]
    [InlineData("Reactivate", MasterStatuses.OnHold, true)]
    [InlineData("Reactivate", MasterStatuses.Inactive, true)]
    [InlineData("Reactivate", MasterStatuses.Approved, true)]
    [InlineData("Reactivate", MasterStatuses.Blacklisted, false)]
    [InlineData("Reactivate", MasterStatuses.Active, false)]
    [InlineData("Submit", MasterStatuses.Draft, true)]
    [InlineData("Submit", MasterStatuses.Active, false)]
    [InlineData("Blacklist", MasterStatuses.Active, true)]
    [InlineData("Blacklist", MasterStatuses.Draft, false)]
    [InlineData("Deactivate", MasterStatuses.Active, true)]
    [InlineData("Unknown", MasterStatuses.Active, false)]
    public void Vendor_lifecycle_actions_are_allowed_only_from_their_states(string action, string from, bool allowed) =>
        Assert.Equal(allowed, MasterEndpoints.VendorActionAllowedFrom(action, from));

    [Fact]
    public async Task A_held_vendor_is_reactivated_to_Active_and_is_eligible_again_without_a_new_approver()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("vendor-lifecycle.sql", migrator.GenerateScript("0", model.Database.GetMigrations().Last()));

        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var vendor = new Vendor
        {
            VendorCode = "R1-VENDOR", Name = "R1 Vendor", LegalVendorName = "R1 Vendor Pvt Ltd", VendorType = "MANUFACTURER",
            VendorStatus = MasterStatuses.Active, ApprovalStatus = MasterApprovalStatuses.Approved,
            CommercialVerificationStatus = MasterApprovalStatuses.Approved, ApprovedBy = "original-approver",
            EffectiveFrom = today, IsActive = true, CreatedBy = "maker"
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();
        Assert.True(VendorQualification.IsVendorEligible(vendor, today));

        var user = new LifecycleUser("second-person");
        var audit = new EfAuditWriter(db, user);
        async Task<IResult> Act(string action, string status) =>
            await MasterEndpoints.ApplyVendorActionAsync("R1-VENDOR", action, status, MasterApprovalStatuses.Approved,
                new MasterActionRequest(action + " for the R1 witness", vendor.Version), db, user, audit, CancellationToken.None);

        Assert.Equal(200, ((IStatusCodeHttpResult)await Act("Hold", MasterStatuses.OnHold)).StatusCode);
        Assert.Equal(MasterStatuses.OnHold, vendor.VendorStatus);
        Assert.False(VendorQualification.IsVendorEligible(vendor, today));

        // A held vendor cannot be approved or held again: 409 with a sentence, nothing changes.
        Assert.Equal(409, ((IStatusCodeHttpResult)await Act("Approve", MasterStatuses.Active)).StatusCode);
        Assert.Equal(409, ((IStatusCodeHttpResult)await Act("Hold", MasterStatuses.OnHold)).StatusCode);
        Assert.Equal(MasterStatuses.OnHold, vendor.VendorStatus);

        Assert.Equal(200, ((IStatusCodeHttpResult)await Act("Reactivate", MasterStatuses.Active)).StatusCode);
        db.ChangeTracker.Clear();
        var reloaded = await db.Vendors.SingleAsync(x => x.VendorCode == "R1-VENDOR");
        Assert.Equal(MasterStatuses.Active, reloaded.VendorStatus);
        Assert.True(VendorQualification.IsVendorEligible(reloaded, today));
        Assert.Equal("original-approver", reloaded.ApprovedBy);
        Assert.Equal(2, await db.MasterStatusHistories.CountAsync(x => x.MasterId == reloaded.Id));
    }

    private sealed class LifecycleUser(string loginId) : ICurrentUser
    {
        public string LoginId => loginId;
        public string RoleCode => "MANAGING_DIRECTOR";
        public string? OrganizationId => "SESS_PVT_LTD";
        public bool IsAuthenticated => true;
    }
}
