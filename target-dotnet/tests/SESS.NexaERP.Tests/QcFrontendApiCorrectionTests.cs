using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class QcFrontendApiCorrectionTests
{
    private const string QcPage = "qc.inspection-policies";

    [Fact]
    public void TechnicalDirectorAloneCanApproveRejectAndReverseConcessions()
    {
        var permission = Permission(Rev869ARoleCodes.TechnicalDirector);
        Assert.True(permission.CanApprove);
        Assert.True(permission.CanCancel);
        Assert.False(permission.CanDeactivate);

        var endpoints = Read("src", "SESS.NexaERP.Api", "Endpoints", "QcEndpoints.cs");
        var service = Read("src", "SESS.NexaERP.Infrastructure", "Stores", "EfQcWorkflowService.cs");
        Assert.Contains("/concessions/{number}/approve", endpoints);
        Assert.Contains("/concessions/{number}/reject", endpoints);
        Assert.Contains("PagePermissionActions.Approve", endpoints);
        Assert.Contains("/concessions/{number}/reverse", endpoints);
        Assert.Contains("PagePermissionActions.Cancel", endpoints);
        Assert.Contains("RequireTechnicalDirector()", service);
    }

    [Fact]
    public void ManagingDirectorAloneCannotDecideOrReverseConcessions()
    {
        var permission = Permission(Rev869ARoleCodes.ManagingDirector);
        Assert.False(permission.CanApprove);
        Assert.False(permission.CanCancel);
        Assert.False(permission.HasFullControl);
    }

    [Fact]
    public void QcManagerAloneCannotDecideOrReverseConcessions()
    {
        var permission = Permission(Rev869ARoleCodes.QcManager);
        Assert.False(permission.CanApprove);
        Assert.False(permission.CanCancel);
        Assert.True(permission.CanCreate);
    }

    [Fact]
    public void QcPolicyReadApiExposesFiltersAndFinalizationFields()
    {
        var source = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869AConfigurationEndpoints.cs");
        Assert.Contains("/qc-inspection-policies", source);
        Assert.Contains("ListQcPolicies(Guid? itemId,Guid? categoryId,bool? effectiveOnly", source);
        foreach (var field in new[] { "x.Id", "x.ParameterCode", "MeasurementUomCode", "x.LowerLimit", "x.UpperLimit", "x.InspectionMethod", "x.SampleSize" })
            Assert.Contains(field, source);
        Assert.Contains("x.ApprovalStatus==MasterApprovalStatuses.Approved", source);
    }

    [Fact]
    public void QcQueueSupportsDirectAllocationGrnAndOverdueFilters()
    {
        var endpoint = Read("src", "SESS.NexaERP.Api", "Endpoints", "QcEndpoints.cs");
        var contract = Read("src", "SESS.NexaERP.Application", "Stores", "QcContracts.cs");
        var service = Read("src", "SESS.NexaERP.Infrastructure", "Stores", "EfQcWorkflowService.cs");
        Assert.Contains("Guid? allocationId,string? grnNumber,bool? overdueOnly", endpoint);
        Assert.Contains("QueueAsync(Guid? allocationId, string? grnNumber, bool overdueOnly", contract);
        Assert.Contains("a.Id==allocationId.Value", service);
        Assert.Contains("GrnNumber==normalized", service);
        Assert.Contains("QcCompletionDaysSnapshot)<now", service);
        Assert.Contains("return new(total,page,pageSize,items)", service);
    }

    [Fact]
    public void PermissionMigrationIsPostgresqlGuardedAndRowStable()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260903103611_CorrectQcConcessionAuthority.cs");
        Assert.Equal(2, Count(migration, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Equal(6, Count(migration, "migrationBuilder.UpdateData("));
        Assert.DoesNotContain("InsertData", migration);
        Assert.DoesNotContain("DeleteData", migration);
    }

    [Fact]
    public void PurchaseRequisitionNumbersUseSixDigitSequenceWithoutLiteralSuffix()
    {
        var source = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpointHelpers.cs");
        Assert.Contains("sequence.LastNumber:000000", source);
        Assert.DoesNotContain("sequence.LastNumber:000001", source);
    }

    [Fact]
    public void DepartmentVerificationRequiresTheEffectiveMappedApproverAndRejectsRequester()
    {
        var source = Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfPurchaseRequisitionWorkflowService.cs");
        Assert.Contains("RequireMappedDepartmentVerifierAsync(pr, ct)", source);
        Assert.Contains("actor==pr.RequesterEmployeeId||actor==pr.CreatorEmployeeId", source);
        Assert.Contains("x.ApprovalRouteCode==PurchaseRequisitionApprovalRoutes.Manager", source);
        Assert.Contains("mapping.PrimaryApproverEmployeeId!=actor", source);
        Assert.Contains("user.RoleCodes.Contains(mapping.ApproverRoleCode", source);
    }

    private static RolePagePermission Permission(string roleCode)
    {
        var roles = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Concat(Rev869ASeedData.Roles);
        var role = roles.Single(x => x.Code == roleCode);
        return Rev869ASeedData.RolePagePermissions.Single(x => x.RoleId == role.Id &&
            Rev869ASeedData.Pages.Single(p => p.PageKey == QcPage).Id == x.PageDefinitionId);
    }

    private static int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));
    private static readonly string Root = FindRoot();
    private static string FindRoot() { var d = new DirectoryInfo(AppContext.BaseDirectory); while (d is not null && !File.Exists(Path.Combine(d.FullName, "SESS.NexaERP.slnx"))) d = d.Parent; return d?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."); }
}
