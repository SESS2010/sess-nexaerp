using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Audit;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Audit;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev867C1PostgresVerificationTests
{
    [Fact]
    public async Task Rev867c1_self_approval_denial_persists_postgresql_audit()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        var run = RunId();
        var creator = new TestCurrentUser("SESS-001", "technical_director", "TD-ORG");
        var audit = new EfAuditWriter(db, creator);
        var item = new Item
        {
            ItemCode = "C1-SELF-" + run,
            Name = "REV867C1 self approval item",
            DetailedDescription = "verification",
            MaterialType = "Material",
            Uom = "NOS",
            MinimumStock = 0,
            MaximumStock = 10,
            ReorderLevel = 1,
            Status = MasterStatuses.PendingApproval,
            ApprovalStatus = MasterApprovalStatuses.PendingApproval,
            CreatedBy = creator.LoginId,
            UpdatedBy = creator.LoginId
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();

        var result = await MasterEndpointHelpers.ChangeLifecycleAsync(db, audit, creator, item, nameof(Item), item.ItemCode, "Approve", MasterStatuses.Active, MasterApprovalStatuses.Approved, "self approval must be blocked", item.Version, (x, s, actor) => x.Status = s, x => x.Status, x => x.ApprovalStatus, (x, s) => x.ApprovalStatus = s, CancellationToken.None);

        Assert.Contains("Forbid", result.GetType().Name, StringComparison.OrdinalIgnoreCase);
        Assert.True(await db.AuditLogs.AnyAsync(x => x.Module == "Security" && x.Action == "Denied" && x.EntityName == nameof(Item) && x.EntityId == item.Id.ToString() && x.Result == "Failure"));
    }

    [Fact]
    public async Task Rev867c1_controlled_lifecycle_persists_status_approval_and_audit_history()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        var run = RunId();
        var creator = new TestCurrentUser("SESS-012", "purchase_executive", "PURCHASE");
        var approver = new TestCurrentUser("SESS-002", "managing_director", "MD-ORG");
        var item = new Item
        {
            ItemCode = "C1-LIFE-" + run,
            Name = "REV867C1 lifecycle item",
            DetailedDescription = "verification",
            MaterialType = "Material",
            Uom = "NOS",
            MinimumStock = 0,
            MaximumStock = 20,
            ReorderLevel = 2,
            StandardEstimatedPrice = 1234.56m,
            CreatedBy = creator.LoginId
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();

        var creatorAudit = new EfAuditWriter(db, creator);
        var submit = await MasterEndpointHelpers.ChangeLifecycleAsync(db, creatorAudit, creator, item, nameof(Item), item.ItemCode, "Submit", MasterStatuses.PendingApproval, MasterApprovalStatuses.PendingApproval, "submit for verification", item.Version, (x, s, actor) => x.Status = s, x => x.Status, x => x.ApprovalStatus, (x, s) => x.ApprovalStatus = s, CancellationToken.None);
        Assert.DoesNotContain("Forbid", submit.GetType().Name, StringComparison.OrdinalIgnoreCase);

        await db.Entry(item).ReloadAsync();
        var approverAudit = new EfAuditWriter(db, approver);
        var approve = await MasterEndpointHelpers.ChangeLifecycleAsync(db, approverAudit, approver, item, nameof(Item), item.ItemCode, "Approve", MasterStatuses.Active, MasterApprovalStatuses.Approved, "approve for verification", item.Version, (x, s, actor) => { x.Status = s; x.ApprovedBy = actor; x.ApprovedAt = DateTimeOffset.UtcNow; }, x => x.Status, x => x.ApprovalStatus, (x, s) => x.ApprovalStatus = s, CancellationToken.None);
        Assert.DoesNotContain("Forbid", approve.GetType().Name, StringComparison.OrdinalIgnoreCase);

        Assert.True(await db.MasterStatusHistories.CountAsync(x => x.MasterType == nameof(Item) && x.MasterId == item.Id) >= 2);
        Assert.True(await db.MasterApprovalHistories.CountAsync(x => x.MasterType == nameof(Item) && x.MasterId == item.Id) >= 2);
        Assert.True(await db.AuditLogs.CountAsync(x => x.EntityName == nameof(Item) && x.EntityId == item.Id.ToString() && x.Result == "Success") >= 2);
    }

    [Fact]
    public async Task Rev867c1_customer_and_vendor_scope_use_claim_organization_against_persisted_records()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        var run = RunId();
        var customerA = new Customer { CustomerCode = "C1-CUST-A-" + run, Name = "Customer A", LegalCustomerName = "Customer A", CustomerType = "Direct", Country = "India", PortalOrganizationId = "ORG-A-" + run, CreditLimit = 9999m, CreatedBy = "verify" };
        var customerB = new Customer { CustomerCode = "C1-CUST-B-" + run, Name = "Customer B", LegalCustomerName = "Customer B", CustomerType = "Direct", Country = "India", PortalOrganizationId = "ORG-B-" + run, CreditLimit = 8888m, CreatedBy = "verify" };
        var vendorA = new Vendor { VendorCode = "C1-VEND-A-" + run, Name = "Vendor A", LegalVendorName = "Vendor A", VendorType = "Material", Country = "India", PortalOrganizationId = "VORG-A-" + run, BankMetadataJson = "{\"account\":\"111\"}", CreatedBy = "verify" };
        var vendorB = new Vendor { VendorCode = "C1-VEND-B-" + run, Name = "Vendor B", LegalVendorName = "Vendor B", VendorType = "Material", Country = "India", PortalOrganizationId = "VORG-B-" + run, BankMetadataJson = "{\"account\":\"222\"}", CreatedBy = "verify" };
        db.Customers.AddRange(customerA, customerB);
        db.Vendors.AddRange(vendorA, vendorB);
        await db.SaveChangesAsync();

        var scopedCustomers = await MasterEndpointHelpers.ApplyCustomerOrganizationScope(db.Customers.AsNoTracking().Where(x => x.CustomerCode.StartsWith("C1-CUST-" + run)), new TestCurrentUser("customer-a", "customer", customerA.PortalOrganizationId)).Select(x => x.CustomerCode).ToListAsync();
        var scopedVendors = await MasterEndpointHelpers.ApplyVendorOrganizationScope(db.Vendors.AsNoTracking().Where(x => x.VendorCode.StartsWith("C1-VEND-" + run)), new TestCurrentUser("vendor-a", "vendor", vendorA.PortalOrganizationId)).Select(x => x.VendorCode).ToListAsync();

        Assert.Equal([customerA.CustomerCode], scopedCustomers);
        Assert.Equal([vendorA.VendorCode], scopedVendors);
    }

    private static NexaErpDbContext NewDb(string connectionString)
    {
        return new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(connectionString).Options);
    }

    private static string VerificationConnectionStringOrSkip()
    {
        var connectionString = Environment.GetEnvironmentVariable("REV867C1_POSTGRES");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("Database=sess_nexaerp_rev867c1_verify", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("REV867C1_POSTGRES must target sess_nexaerp_rev867c1_verify only.");
        }

        return connectionString;
    }

    private static string RunId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

    private sealed record TestCurrentUser(string LoginId, string RoleCode, string? OrganizationId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
    }
}

