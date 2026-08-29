using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class MasterEndpoints
{
    private static bool VendorControlledFieldsChanged(Vendor vendor, UpsertVendorRequest request) =>
        !string.Equals(vendor.GstNumber, MasterEndpointHelpers.NormalizeUpperOptional(request.GstNumber), StringComparison.Ordinal) ||
        !string.Equals(vendor.PanNumber, MasterEndpointHelpers.NormalizeUpperOptional(request.PanNumber), StringComparison.Ordinal) ||
        !string.Equals(vendor.BankMetadataJson, MasterEndpointHelpers.NormalizeOptional(request.BankMetadataJson), StringComparison.Ordinal) ||
        !string.Equals(vendor.PaymentTerms, MasterEndpointHelpers.NormalizeOptional(request.PaymentTerms), StringComparison.Ordinal) ||
        !string.Equals(vendor.DeliveryTerms, MasterEndpointHelpers.NormalizeOptional(request.DeliveryTerms), StringComparison.Ordinal) ||
        vendor.CreditPeriodDays != request.CreditPeriodDays;

    private static object VendorControlledSnapshot(Vendor vendor) => new
    {
        vendor.GstNumber,
        vendor.PanNumber,
        vendor.BankMetadataJson,
        vendor.PaymentTerms,
        vendor.DeliveryTerms,
        vendor.CreditPeriodDays,
        vendor.CommercialVerificationStatus,
        vendor.ApprovalStatus,
        vendor.VendorStatus,
        vendor.EffectiveFrom,
        vendor.EffectiveTo
    };

    private static void AddVendorReverificationEvidence(NexaErpDbContext db, Vendor vendor, string previousApprovalStatus, object before, ICurrentUser user)
    {
        vendor.CommercialVerificationStatus = MasterApprovalStatuses.PendingApproval;
        vendor.ApprovalStatus = MasterApprovalStatuses.PendingApproval;
        vendor.VendorStatus = MasterStatuses.PendingApproval;
        vendor.RequiresReverification = true;
        vendor.CommercialVerifiedBy = null;
        vendor.CommercialVerifiedAt = null;
        vendor.ApprovedBy = null;
        vendor.ApprovedAt = null;
        var correlation = $"REV869A_VENDOR_REVERIFY_{Guid.NewGuid():N}";
        db.MasterApprovalHistories.Add(new MasterApprovalHistory { MasterType = nameof(Vendor), MasterId = vendor.Id, MasterCode = vendor.VendorCode, Action = "ControlledDetailsChanged", FromStatus = previousApprovalStatus, ToStatus = vendor.ApprovalStatus, Remarks = "GST/PAN/bank/commercial details changed; Accounts re-verification and final approval required.", ActorLoginId = user.LoginId, ActorRoleCode = user.RoleCode, CorrelationId = correlation, CreatedBy = user.LoginId });
        db.ControlledConfigurationHistories.Add(new ControlledConfigurationHistory { OrganizationId = user.OrganizationId ?? "SESS", EntityType = nameof(Vendor), EntityId = vendor.Id, Action = "ControlledDetailsChanged", BeforeJson = JsonSerializer.Serialize(before), AfterJson = JsonSerializer.Serialize(VendorControlledSnapshot(vendor)), ActorLoginId = user.LoginId, ActorRoleCode = user.RoleCode, Remarks = "Controlled vendor details changed.", CorrelationId = correlation, CreatedBy = user.LoginId });
    }

    private static async Task<IResult> VerifyVendorCommercial(string vendorCode, MasterActionRequest request, NexaErpDbContext db, ICurrentUser currentUser, IPagePermissionService permissions, IAuditWriter audit, CancellationToken cancellationToken)
    {
        if (!string.Equals(Rev869ARoleCodes.Normalize(currentUser.RoleCode), "ACCOUNTS_HEAD", StringComparison.Ordinal)) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.Remarks)) return Results.BadRequest(new { message = "Accounts verification remarks are required." });
        var vendor = await db.Vendors.SingleOrDefaultAsync(x => x.VendorCode == MasterEndpointHelpers.NormalizeCode(vendorCode), cancellationToken);
        if (vendor is null) return Results.NotFound(new { message = "Vendor not found." });
        if (vendor.Version != request.Version) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
        var before = VendorControlledSnapshot(vendor);
        vendor.CommercialVerificationStatus = MasterApprovalStatuses.Approved;
        vendor.CommercialVerifiedBy = currentUser.LoginId;
        vendor.CommercialVerifiedAt = DateTimeOffset.UtcNow;
        vendor.RequiresReverification = false;
        vendor.ApprovalStatus = MasterApprovalStatuses.PendingApproval;
        vendor.VendorStatus = MasterStatuses.PendingApproval;
        vendor.UpdatedBy = currentUser.LoginId;
        vendor.UpdatedAt = DateTimeOffset.UtcNow;
        var correlation = $"REV869A_VENDOR_VERIFY_{Guid.NewGuid():N}";
        db.MasterApprovalHistories.Add(new MasterApprovalHistory { MasterType = nameof(Vendor), MasterId = vendor.Id, MasterCode = vendor.VendorCode, Action = "AccountsVerify", FromStatus = MasterApprovalStatuses.PendingApproval, ToStatus = MasterApprovalStatuses.Approved, Remarks = request.Remarks.Trim(), ActorLoginId = currentUser.LoginId, ActorRoleCode = currentUser.RoleCode, CorrelationId = correlation, CreatedBy = currentUser.LoginId });
        db.ControlledConfigurationHistories.Add(new ControlledConfigurationHistory { OrganizationId = currentUser.OrganizationId ?? "SESS", EntityType = nameof(Vendor), EntityId = vendor.Id, Action = "AccountsVerify", BeforeJson = JsonSerializer.Serialize(before), AfterJson = JsonSerializer.Serialize(VendorControlledSnapshot(vendor)), ActorLoginId = currentUser.LoginId, ActorRoleCode = currentUser.RoleCode, Remarks = request.Remarks.Trim(), CorrelationId = correlation, CreatedBy = currentUser.LoginId });
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("Masters", "VerifyVendorCommercial", nameof(Vendor), vendor.Id.ToString(), before, VendorControlledSnapshot(vendor), cancellationToken);
        var canViewBank = await MasterEndpointHelpers.CanViewCommercialAsync(permissions, currentUser, "masters.vendors", cancellationToken);
        return Results.Ok(ToDetail(vendor, canViewBank));
    }

    private static async Task<IResult?> ValidateVendorFinalApproval(Vendor vendor, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken)
    {
        if (vendor.CommercialVerificationStatus != MasterApprovalStatuses.Approved || vendor.RequiresReverification) return Results.Conflict(new { message = "Accounts commercial verification is required before final vendor approval." });
        var organizationId = currentUser.OrganizationId ?? "SESS";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var policies = await db.OrganizationPolicies.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.PolicyCode == Rev869APolicyCodes.VendorFinalApprover && x.IsActive && x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today)).Take(2).ToListAsync(cancellationToken);
        if (policies.Count != 1) return Results.Conflict(new { message = "VENDOR_FINAL_APPROVER policy is missing or ambiguous." });
        if (string.Equals(Rev869ARoleCodes.Normalize(currentUser.RoleCode), Rev869ARoleCodes.Normalize(policies[0].PolicyValue), StringComparison.Ordinal)) return null;
        await audit.WriteAsync("Security", "Denied", nameof(Vendor), vendor.Id.ToString(), new { vendor.VendorStatus }, new { reason = "Configured final vendor approver role mismatch", policy = Rev869APolicyCodes.VendorFinalApprover }, cancellationToken);
        return Results.Forbid();
    }
}
