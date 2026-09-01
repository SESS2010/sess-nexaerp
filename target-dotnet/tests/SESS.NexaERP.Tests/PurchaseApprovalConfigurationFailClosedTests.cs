using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Purchase;

namespace SESS.NexaERP.Tests;

public sealed class PurchaseApprovalConfigurationFailClosedTests
{
    [Fact]
    public void SubmittingPurchaseRequisitionWithoutActiveRouteSettingsFailsClosed()
    {
        var companyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var effectiveAt = new DateTimeOffset(2026, 9, 1, 10, 30, 0, TimeSpan.FromHours(5.5));
        var requisition = new PurchaseRequisition
        {
            CompanyId = companyId,
            Status = PurchaseRequisitionStatuses.Draft,
            ApprovalRoute = "UNSELECTED",
            ApprovalCycle = 0,
            RequiredApprovalStepCount = 0,
            CompletedApprovalStepCount = 0,
            ApprovalWorkflowSnapshotJson = "{}"
        };
        var statusHistory = new List<PurchaseRequisitionStatusHistory>();
        var approvalHistory = new List<PurchaseRequisitionApprovalHistory>();

        var error = Assert.Throws<Rev869BConflictException>(() =>
            EfPurchaseApprovalWorkflowService.SelectRouteSetting(companyId, effectiveAt, 5_000m, []));

        Assert.Contains(companyId.ToString(), error.Message, StringComparison.Ordinal);
        Assert.Contains(effectiveAt.ToString("O"), error.Message, StringComparison.Ordinal);
        Assert.Contains("purchase_approval_route_settings", error.Message, StringComparison.Ordinal);
        Assert.Equal(PurchaseRequisitionStatuses.Draft, requisition.Status);
        Assert.Equal("UNSELECTED", requisition.ApprovalRoute);
        Assert.Equal(0, requisition.ApprovalCycle);
        Assert.Equal(0, requisition.RequiredApprovalStepCount);
        Assert.Equal(0, requisition.CompletedApprovalStepCount);
        Assert.Equal("{}", requisition.ApprovalWorkflowSnapshotJson);
        Assert.Empty(statusHistory);
        Assert.Empty(approvalHistory);
    }

    [Fact]
    public void AmbiguousPurchaseApprovalRouteSettingsNameEveryConflict()
    {
        var companyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var effectiveAt = new DateTimeOffset(2026, 9, 1, 11, 0, 0, TimeSpan.FromHours(5.5));
        var first = new PurchaseApprovalRouteSetting { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), CompanyId = companyId, RouteCode = "DEPARTMENT_ONLY", MinimumAmount = 0, MaximumAmount = 5_000, IsActive = true };
        var second = new PurchaseApprovalRouteSetting { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), CompanyId = companyId, RouteCode = "OVERLAP", MinimumAmount = 4_000, MaximumAmount = 6_000, IsActive = true };

        var error = Assert.Throws<Rev869BConflictException>(() =>
            EfPurchaseApprovalWorkflowService.SelectRouteSetting(companyId, effectiveAt, 5_000m, [first, second]));

        Assert.Contains("ambiguous", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(first.Id.ToString(), error.Message, StringComparison.Ordinal);
        Assert.Contains(second.Id.ToString(), error.Message, StringComparison.Ordinal);
        Assert.Contains(first.RouteCode, error.Message, StringComparison.Ordinal);
        Assert.Contains(second.RouteCode, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PurchaseRequisitionSourceContainsNoApprovalFallbackBands()
    {
        var root = FindRepositoryRoot();
        var helper = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpointHelpers.cs"));

        Assert.DoesNotContain("DefaultApprovalRoutes", helper, StringComparison.Ordinal);
        Assert.DoesNotContain("DefaultApprovalWorkflowSteps", helper, StringComparison.Ordinal);
        Assert.DoesNotContain("500000", helper, StringComparison.Ordinal);
        Assert.DoesNotContain("50000.01", helper, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}