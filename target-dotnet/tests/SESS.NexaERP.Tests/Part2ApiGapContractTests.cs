using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Employees;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Tests;

public sealed class Part2ApiGapContractTests
{
    [Fact]
    public void Purchase_exposes_every_required_paged_read_route_and_exact_number_filter()
    {
        var routes = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869BPurchaseEndpoints.cs");
        var reads = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869BPurchaseReadEndpoints.cs");
        foreach (var route in new[] { "/rfqs", "/quotations", "/comparisons", "/purchase-orders", "/quotations/{number}" })
            Assert.Contains(route, routes, StringComparison.Ordinal);
        foreach (var filter in new[] { "rfqNumber", "quotationNumber", "comparisonNumber", "purchaseOrderNumber", "vendorId", "from", "to", "sortBy", "sortDirection" })
            Assert.Contains(filter, reads, StringComparison.Ordinal);
        Assert.Contains("PagedResponse<RfqListItem>", reads, StringComparison.Ordinal);
        Assert.Contains("PagedResponse<QuotationListItem>", reads, StringComparison.Ordinal);
        Assert.Contains("PagedResponse<ComparisonListItem>", reads, StringComparison.Ordinal);
        Assert.Contains("PagedResponse<PurchaseOrderListItem>", reads, StringComparison.Ordinal);
    }

    [Fact]
    public void Stores_and_employee_lists_use_the_canonical_envelope()
    {
        Assert.NotNull(typeof(GateEntryListResult).GetProperty("TotalCount"));
        Assert.NotNull(typeof(GateEntryListResult).GetProperty("PageNumber"));
        Assert.NotNull(typeof(GoodsReceiptListResult).GetProperty("TotalCount"));
        Assert.NotNull(typeof(GoodsReceiptListResult).GetProperty("PageNumber"));
        var employee = Read("src", "SESS.NexaERP.Api", "Endpoints", "EmployeeEndpoints.cs");
        Assert.Contains("PagedResponse<EmployeeSummary>", employee, StringComparison.Ordinal);
        Assert.Contains("Stale employee version", employee, StringComparison.Ordinal);
    }

    [Fact]
    public void Session_and_all_same_row_admin_mutations_publish_or_enforce_version_context()
    {
        Assert.NotNull(typeof(SessionMe).GetProperty("Permissions"));
        Assert.NotNull(typeof(EmployeeSummary).GetProperty("Version"));
        Assert.NotNull(typeof(EmployeeDetail).GetProperty("Version"));
        Assert.NotNull(typeof(UpdateEmployeeRequest).GetProperty("Version"));
        Assert.NotNull(typeof(EmployeeApprovalRequest).GetProperty("Version"));
        Assert.NotNull(typeof(LoginStatusRequest).GetProperty("Version"));
        Assert.NotNull(typeof(RolePagePermissionSummary).GetProperty("Version"));
        Assert.NotNull(typeof(UpsertRolePagePermissionRequest).GetProperty("Version"));
    }

    [Fact]
    public void Remaining_top_level_administration_lists_are_paged()
    {
        var identity = Read("src", "SESS.NexaERP.Api", "Endpoints", "IdentityEndpoints.cs");
        var authorization = Read("src", "SESS.NexaERP.Api", "Endpoints", "AuthorizationEndpoints.cs");
        var audit = Read("src", "SESS.NexaERP.Infrastructure", "Audit", "EfAuditHistoryService.cs");
        Assert.Equal(2, Count(identity, "new PagedResponse<"));
        Assert.Equal(2, Count(authorization, "PagedResponse<"));
        Assert.Contains("new PagedResponse<AuditLogSummary>", audit, StringComparison.Ordinal);
    }

    private static int Count(string value, string fragment) =>
        (value.Length - value.Replace(fragment, string.Empty, StringComparison.Ordinal).Length) / fragment.Length;

    private static string Read(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return File.ReadAllText(Path.Combine([directory?.FullName ?? throw new DirectoryNotFoundException(), .. parts]));
    }
}
