using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Identity;

public sealed class EfSessionService(NexaErpDbContext db, ICurrentUser currentUser) : ISessionService
{
    public async Task<SessionMe> GetCurrentAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.EmployeeId.HasValue || !currentUser.DepartmentId.HasValue ||
            string.IsNullOrWhiteSpace(currentUser.OrganizationId) || string.IsNullOrWhiteSpace(currentUser.IdentityIssuer) ||
            string.IsNullOrWhiteSpace(currentUser.IdentitySubject))
            throw new UnauthorizedAccessException("A resolved employee OIDC session is required.");

        var organization = currentUser.OrganizationId.Trim().ToUpperInvariant();
        var company = await db.Companies.AsNoTracking().SingleOrDefaultAsync(x => x.Code == organization && x.IsActive && x.Status == "ACTIVE", cancellationToken)
            ?? throw new UnauthorizedAccessException("The resolved company is inactive or unavailable.");
        var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Id == currentUser.EmployeeId.Value, cancellationToken)
            ?? throw new UnauthorizedAccessException("The resolved employee is unavailable.");
        var departmentCode = await db.Departments.AsNoTracking().Where(x => x.Id == currentUser.DepartmentId.Value && x.IsActive)
            .Select(x => x.Code).SingleOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("The resolved primary department is unavailable.");

        var permissions = await ResolvePermissionsAsync(currentUser.RoleCodes, cancellationToken);
        return new SessionMe(employee.Id, employee.EmployeeCode, employee.EmployeeName, company.Id, company.Code,
            currentUser.DepartmentId.Value, departmentCode, currentUser.RoleCodes.Order(StringComparer.Ordinal).ToArray(), permissions,
            currentUser.IdentityIssuer!, currentUser.IdentitySubject!);
    }

    private async Task<IReadOnlyList<string>> ResolvePermissionsAsync(IReadOnlyCollection<string> roleCodes, CancellationToken ct)
    {
        var roles = roleCodes.Select(x => x.Trim().ToUpperInvariant()).Distinct().ToArray();
        var grants = await db.RolePagePermissions.AsNoTracking()
            .Where(x => x.Role != null && x.PageDefinition != null && roles.Contains(x.Role.Code) && x.Role.IsActive && x.PageDefinition.IsActive)
            .Select(x => new { x.PageDefinition!.PageKey, x.CanView, x.CanCreate, x.CanUpdate, x.CanSubmit, x.CanIssue, x.CanVerify,
                x.CanApprove, x.CanReject, x.CanRequestClarification, x.CanRequestRevision, x.CanResubmit, x.CanCancel, x.CanDeactivate,
                x.CanPrint, x.CanDownload, x.CanExport, x.CanUploadAttachment, x.CanReplaceAttachment, x.CanViewCommercialValues,
                x.CanViewAuditHistory, x.HasFullControl })
            .ToListAsync(ct);
        var explicitPages = new HashSet<string>(["purchase.rfq", "purchase.vendor-quotations", "purchase.technical-verification",
            "purchase.commercial-comparisons", "purchase.po", "purchase.material-followup", "purchase.requisition-approvals", "inventory.grn"], StringComparer.Ordinal);
        var resolved = new HashSet<string>(StringComparer.Ordinal);
        foreach (var grant in grants)
        {
            var broadFullControl = grant.HasFullControl && !explicitPages.Contains(grant.PageKey);
            void Add(bool granted, string action) { if (granted || broadFullControl) resolved.Add($"{grant.PageKey}:{action}"); }
            Add(grant.CanView, PagePermissionActions.View); Add(grant.CanCreate, PagePermissionActions.Create);
            Add(grant.CanUpdate, PagePermissionActions.Update); Add(grant.CanSubmit, PagePermissionActions.Submit);
            Add(grant.CanIssue, PagePermissionActions.Issue); Add(grant.CanVerify, PagePermissionActions.Verify);
            Add(grant.CanApprove, PagePermissionActions.Approve); Add(grant.CanReject, PagePermissionActions.Reject);
            Add(grant.CanRequestClarification, PagePermissionActions.RequestClarification); Add(grant.CanRequestRevision, PagePermissionActions.RequestRevision);
            Add(grant.CanResubmit, PagePermissionActions.Resubmit); Add(grant.CanCancel, PagePermissionActions.Cancel);
            Add(grant.CanDeactivate, PagePermissionActions.Deactivate); Add(grant.CanPrint, PagePermissionActions.Print);
            Add(grant.CanDownload, PagePermissionActions.Download); Add(grant.CanExport, PagePermissionActions.Export);
            Add(grant.CanUploadAttachment, PagePermissionActions.UploadAttachment); Add(grant.CanReplaceAttachment, PagePermissionActions.ReplaceAttachment);
            Add(grant.CanViewCommercialValues, PagePermissionActions.ViewCommercialValues); Add(grant.CanViewAuditHistory, PagePermissionActions.ViewAuditHistory);
            Add(grant.HasFullControl, PagePermissionActions.FullControl);
        }
        return resolved.Order(StringComparer.Ordinal).ToArray();
    }
}
