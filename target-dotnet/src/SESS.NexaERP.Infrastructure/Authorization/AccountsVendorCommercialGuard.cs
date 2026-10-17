using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Authorization;

public static class AccountsVendorCommercialGuard
{
    // Retained subject aliases identify the maker; they never authenticate the checker.
    // Native page/MFA/company checks and optimistic version concurrency remain mandatory.
    public static async Task RequireIndependentAsync(NexaErpDbContext db, ICurrentUser user, Vendor vendor, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.EmployeeId is not { } actor)
            throw new UnauthorizedAccessException("A resolved employee identity is required.");
        if (!await db.Employees.AsNoTracking().AnyAsync(e => e.Id == actor &&
                e.EmployeeCode == AccountsBackupIdentityPolicy.EmployeeCode, ct)) return;
        if (!await db.Companies.AsNoTracking().AnyAsync(c => c.Code == user.OrganizationId && c.IsActive && c.Status == "ACTIVE", ct))
            throw new UnauthorizedAccessException("An active selected company is required.");
        // Vendor masters are shared; a maker fact in either company remains the same maker.
        var aliases = await db.EmployeeIdentityMappings.AsNoTracking().Where(m => m.EmployeeId == actor)
            .Select(m => m.Subject).Distinct().ToListAsync(ct);
        aliases.Add(AccountsBackupIdentityPolicy.EmployeeCode);
        aliases.Add(AccountsBackupIdentityPolicy.EmployeeCode.ToLowerInvariant());
        if (aliases.Contains(vendor.CreatedBy) || await db.MasterApprovalHistories.AsNoTracking().AnyAsync(h =>
                h.MasterType == nameof(Vendor) && h.MasterId == vendor.Id && aliases.Contains(h.ActorLoginId) &&
                (h.Action == "CreateDraft" || h.Action == "Submit" || h.Action == "ControlledDetailsChanged" || h.Action == "UpdateDraft"), ct))
            throw new UnauthorizedAccessException("Vendor commercial verification must be independent of this employee's vendor creation, submission and controlled changes across both identities.");
    }
}
