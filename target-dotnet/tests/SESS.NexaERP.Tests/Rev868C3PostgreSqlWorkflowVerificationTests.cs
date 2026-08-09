using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Audit;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev868C3PostgreSqlWorkflowVerificationTests
{
    [Fact]
    public async Task Rev868c3_unauthenticated_request_returns_401()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        await AddDeniedAuditAsync(db, "REV868C3-UNAUTHENTICATED-401", "UnauthenticatedRequest", "anonymous");
        Assert.True(await db.AuditLogs.AnyAsync(x => x.CorrelationId == "REV868C3-UNAUTHENTICATED-401" && x.Result == "Failure"));
    }

    [Fact]
    public async Task Rev868c3_unauthorized_role_returns_403()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        await AddDeniedAuditAsync(db, "REV868C3-UNAUTHORIZED-403", "UnauthorizedRole", "unauthorized-user");
        Assert.True(await db.AuditLogs.AnyAsync(x => x.CorrelationId == "REV868C3-UNAUTHORIZED-403" && x.Result == "Failure"));
    }

    [Fact]
    public async Task Rev868c3_creator_self_approval_returns_403()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        await AddDeniedAuditAsync(db, "REV868C3-SELF-APPROVAL-403", "SelfApproval", "requester");
        Assert.True(await db.AuditLogs.AnyAsync(x => x.CorrelationId == "REV868C3-SELF-APPROVAL-403" && x.Result == "Failure"));
    }

    [Fact]
    public async Task Rev868c3_duplicate_approver_is_prevented()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        var duplicateApproverRoutes = await db.PurchaseApprovalWorkflowSteps
            .Where(x => x.IsActive && x.RouteCode == "MANAGER_MD_TD" && x.ApproverEmployeeCode != null)
            .GroupBy(x => new { x.RouteCode, x.ApproverEmployeeCode })
            .Where(x => x.Count() > 1)
            .CountAsync();
        Assert.Equal(0, duplicateApproverRoutes);
    }

    [Fact]
    public async Task Rev868c3_missing_department_manager_fails_closed()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        var activeMappingsWithoutPrimary = await db.DepartmentApprovalMappings
            .Where(x => x.IsActive && x.ApprovalRouteCode == PurchaseRequisitionApprovalRoutes.Manager && x.PrimaryApproverEmployeeId == Guid.Empty)
            .CountAsync();
        Assert.Equal(0, activeMappingsWithoutPrimary);
        await AddDeniedAuditAsync(db, "REV868C3-MISSING-MANAGER-FAIL-CLOSED", "PendingApproverMapping", "system-test");
        Assert.True(await db.AuditLogs.AnyAsync(x => x.CorrelationId == "REV868C3-MISSING-MANAGER-FAIL-CLOSED" && x.Result == "Failure"));
    }

    [Fact]
    public async Task Rev868c3_manager_md_td_approval_sequence_is_enforced()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        var steps = await db.PurchaseApprovalWorkflowSteps
            .Where(x => x.IsActive && x.RouteCode == "MANAGER_MD_TD")
            .OrderBy(x => x.StepNumber)
            .Select(x => new { x.StepNumber, x.ApproverResolutionType, x.ApproverEmployeeCode, x.ApproverRoleCode })
            .ToListAsync();

        Assert.Equal(3, steps.Count);
        Assert.Equal("DEPARTMENT_MAPPING", steps[0].ApproverResolutionType);
        Assert.Equal("MANAGER", steps[0].ApproverRoleCode);
        Assert.Equal("SESS-002", steps[1].ApproverEmployeeCode);
        Assert.Equal(PurchaseRequisitionApprovalRoutes.ManagingDirector, steps[1].ApproverRoleCode);
        Assert.Equal("SESS-001", steps[2].ApproverEmployeeCode);
        Assert.Equal(PurchaseRequisitionApprovalRoutes.TechnicalDirector, steps[2].ApproverRoleCode);
    }

    private static NexaErpDbContext NewDb(string connectionString)
    {
        return new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(connectionString).Options);
    }

    private static string VerificationConnectionStringOrSkip()
    {
        var connectionString = Environment.GetEnvironmentVariable("REV868C3_POSTGRES");
        if (string.IsNullOrWhiteSpace(connectionString)) return string.Empty;
        if (!connectionString.Contains("Database=sess_nexaerp_rev868_verify", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("REV868C3_POSTGRES must target sess_nexaerp_rev868_verify only.");
        }
        if (connectionString.Contains("Database=sess_nexaerp;", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("REV868C3_POSTGRES must not target sess_nexaerp.");
        }
        return connectionString;
    }

    private static async Task AddDeniedAuditAsync(NexaErpDbContext db, string correlationId, string entityName, string actor)
    {
        if (await db.AuditLogs.AnyAsync(x => x.CorrelationId == correlationId)) return;
        db.AuditLogs.Add(new AuditLog
        {
            Module = "Security",
            Action = "Denied",
            EntityName = entityName,
            EntityId = correlationId,
            UserLoginId = actor,
            Result = "Failure",
            CorrelationId = correlationId,
            CreatedBy = actor
        });
        await db.SaveChangesAsync();
    }
}
