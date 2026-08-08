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
        var customerA = await UpsertCustomerAsync(db, "REV867C1-SCOPE-CUST-A", "REV867C1-ORG-CUSTOMER-A", 9999m);
        var customerB = await UpsertCustomerAsync(db, "REV867C1-SCOPE-CUST-B", "REV867C1-ORG-CUSTOMER-B", 8888m);
        var vendorA = await UpsertVendorAsync(db, "REV867C1-SCOPE-VEND-A", "REV867C1-ORG-VENDOR-A", "{\"account\":\"111\"}");
        var vendorB = await UpsertVendorAsync(db, "REV867C1-SCOPE-VEND-B", "REV867C1-ORG-VENDOR-B", "{\"account\":\"222\"}");
        db.ChangeTracker.Clear();

        var customerCodes = new[] { customerA.CustomerCode, customerB.CustomerCode };
        var vendorCodes = new[] { vendorA.VendorCode, vendorB.VendorCode };
        var scopedCustomers = await MasterEndpointHelpers.ApplyCustomerOrganizationScope(db.Customers.AsNoTracking().Where(x => customerCodes.Contains(x.CustomerCode)), new TestCurrentUser("customer-a", "customer", customerA.PortalOrganizationId)).OrderBy(x => x.CustomerCode).Select(x => x.CustomerCode).ToListAsync();
        var scopedVendors = await MasterEndpointHelpers.ApplyVendorOrganizationScope(db.Vendors.AsNoTracking().Where(x => vendorCodes.Contains(x.VendorCode)), new TestCurrentUser("vendor-a", "vendor", vendorA.PortalOrganizationId)).OrderBy(x => x.VendorCode).Select(x => x.VendorCode).ToListAsync();
        var crossCustomerCount = await MasterEndpointHelpers.ApplyCustomerOrganizationScope(db.Customers.AsNoTracking().Where(x => x.CustomerCode == customerB.CustomerCode), new TestCurrentUser("customer-a", "customer", customerA.PortalOrganizationId)).CountAsync();
        var crossVendorCount = await MasterEndpointHelpers.ApplyVendorOrganizationScope(db.Vendors.AsNoTracking().Where(x => x.VendorCode == vendorB.VendorCode), new TestCurrentUser("vendor-a", "vendor", vendorA.PortalOrganizationId)).CountAsync();

        Assert.Equal([customerA.CustomerCode], scopedCustomers);
        Assert.Equal([vendorA.VendorCode], scopedVendors);
        Assert.Equal(0, crossCustomerCount);
        Assert.Equal(0, crossVendorCount);
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

    private static async Task<Customer> UpsertCustomerAsync(NexaErpDbContext db, string code, string organizationId, decimal creditLimit)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.CustomerCode == code);
        if (customer is null)
        {
            customer = new Customer { CustomerCode = code, CreatedBy = "verify" };
            db.Customers.Add(customer);
        }

        customer.Name = code;
        customer.LegalCustomerName = code;
        customer.CustomerType = "Direct";
        customer.Country = "India";
        customer.PortalOrganizationId = organizationId;
        customer.CreditLimit = creditLimit;
        customer.UpdatedBy = "verify";
        customer.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return customer;
    }

    private static async Task<Vendor> UpsertVendorAsync(NexaErpDbContext db, string code, string organizationId, string bankMetadata)
    {
        var vendor = await db.Vendors.SingleOrDefaultAsync(x => x.VendorCode == code);
        if (vendor is null)
        {
            vendor = new Vendor { VendorCode = code, CreatedBy = "verify" };
            db.Vendors.Add(vendor);
        }

        vendor.Name = code;
        vendor.LegalVendorName = code;
        vendor.VendorType = "Material";
        vendor.Country = "India";
        vendor.PortalOrganizationId = organizationId;
        vendor.BankMetadataJson = bankMetadata;
        vendor.UpdatedBy = "verify";
        vendor.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return vendor;
    }

    private sealed record TestCurrentUser(string LoginId, string RoleCode, string? OrganizationId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
    }
}
