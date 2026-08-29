using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class MasterEndpoints
{
    public static IEndpointRouteBuilder MapMasterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/masters").WithTags("Masters").RequireAuthorization();

        MapCustomerEndpoints(group);
        MapVendorEndpoints(group);
        MapVendorAttachmentEndpoints(group);
        return endpoints;
    }

    private static void MapCustomerEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/customers", async (NexaErpDbContext db, IPagePermissionService permissions, ICurrentUser currentUser, int? page, int? pageSize, string? search, string? status, string? type, string? sortBy, string? sortDirection, CancellationToken cancellationToken) =>
        {
            var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize);
            var query = MasterEndpointHelpers.ApplyCustomerOrganizationScope(db.Customers.AsNoTracking(), currentUser);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpperInvariant();
                query = query.Where(customer => customer.CustomerCode.ToUpper().Contains(term) || customer.LegalCustomerName.ToUpper().Contains(term) || (customer.GstNumber != null && customer.GstNumber.ToUpper().Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(customer => customer.Status == status.Trim());
            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(customer => customer.CustomerType == type.Trim());
            var total = await query.CountAsync(cancellationToken);
            query = (sortBy?.Trim().ToLowerInvariant(), sortDirection?.Trim().ToLowerInvariant()) switch
            {
                ("name", "desc") => query.OrderByDescending(customer => customer.LegalCustomerName),
                ("name", _) => query.OrderBy(customer => customer.LegalCustomerName),
                ("status", "desc") => query.OrderByDescending(customer => customer.Status),
                ("status", _) => query.OrderBy(customer => customer.Status),
                ("code", "desc") => query.OrderByDescending(customer => customer.CustomerCode),
                _ => query.OrderBy(customer => customer.CustomerCode)
            };
            var canViewCredit = await MasterEndpointHelpers.CanViewCommercialAsync(permissions, currentUser, "masters.customers", cancellationToken);
            var rows = await query.Skip(paging.Skip).Take(paging.PageSize)
                .Select(customer => new CustomerSummary(customer.Id, customer.CustomerCode, customer.Name, customer.GstNumber, customer.PanNumber, customer.PortalOrganizationId, customer.Status, customer.ApprovalStatus, customer.IsActive, customer.Version, canViewCredit ? customer.CreditLimit : null))
                .ToListAsync(cancellationToken);
            return Results.Ok(new PagedResponse<CustomerSummary>(total, paging.PageNumber, paging.PageSize, rows));
        }).RequirePagePermission("masters.customers", PagePermissionActions.View);

        group.MapGet("/customers/{customerCode}", async (string customerCode, NexaErpDbContext db, IPagePermissionService permissions, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            var code = MasterEndpointHelpers.NormalizeCode(customerCode);
            var customer = await MasterEndpointHelpers.ApplyCustomerOrganizationScope(db.Customers.AsNoTracking(), currentUser).SingleOrDefaultAsync(existing => existing.CustomerCode == code, cancellationToken);
            if (customer is null) return Results.NotFound(new { message = "Customer not found." });
            var canViewCredit = await MasterEndpointHelpers.CanViewCommercialAsync(permissions, currentUser, "masters.customers", cancellationToken);
            return Results.Ok(ToDetail(customer, canViewCredit));
        }).RequirePagePermission("masters.customers", PagePermissionActions.View);

        group.MapPost("/customers", async (UpsertCustomerRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            var validation = await ValidateCustomerAsync(request, db, null, cancellationToken);
            if (validation is not null) return validation;
            var code = MasterEndpointHelpers.NormalizeCode(request.CustomerCode);
            var customer = new Customer { CustomerCode = code, IsCustomerCodeLocked = false };
            ApplyCustomer(customer, request, currentUser.LoginId, true);
            db.Customers.Add(customer);
            db.MasterStatusHistories.Add(new MasterStatusHistory { MasterType = nameof(Customer), MasterId = customer.Id, MasterCode = code, PreviousStatus = null, NewStatus = customer.Status, Reason = "REV867 customer draft created", SourceRevision = "REV867", CorrelationId = $"REV867_CUSTOMER_CREATE_{Guid.NewGuid():N}", CreatedBy = currentUser.LoginId });
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Masters", "CreateDraft", nameof(Customer), customer.Id.ToString(), null, customer, cancellationToken);
            return Results.Created($"/api/v1/masters/customers/{customer.CustomerCode}", ToSummary(customer, true));
        }).RequirePagePermission("masters.customers", PagePermissionActions.Create);

        group.MapPut("/customers/{customerCode}", async (string customerCode, UpsertCustomerRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            var code = MasterEndpointHelpers.NormalizeCode(customerCode);
            var customer = await db.Customers.SingleOrDefaultAsync(existing => existing.CustomerCode == code, cancellationToken);
            if (customer is null) return Results.NotFound(new { message = "Customer not found." });
            if (customer.IsCustomerCodeLocked && MasterEndpointHelpers.NormalizeCode(request.CustomerCode) != customer.CustomerCode) return Results.BadRequest(new { message = "Customer code is immutable after approval." });
            if (request.Version is null || MasterEndpointHelpers.IsMismatch(request.Version.Value, customer.Version)) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
            var validation = await ValidateCustomerAsync(request, db, customer.Id, cancellationToken);
            if (validation is not null) return validation;
            var before = ToDetail(customer, true);
            ApplyCustomer(customer, request, currentUser.LoginId, false);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Masters", "UpdateDraft", nameof(Customer), customer.Id.ToString(), before, customer, cancellationToken);
            return Results.Ok(ToDetail(customer, true));
        }).RequirePagePermission("masters.customers", PagePermissionActions.Update);

        MapCustomerAction(group, "submit", "Submit", MasterStatuses.PendingApproval, MasterApprovalStatuses.PendingApproval, PagePermissionActions.Submit);
        MapCustomerAction(group, "approve", "Approve", MasterStatuses.Active, MasterApprovalStatuses.Approved, PagePermissionActions.Approve);
        MapCustomerAction(group, "reject", "Reject", MasterStatuses.Rejected, MasterApprovalStatuses.Rejected, PagePermissionActions.Reject);
        MapCustomerAction(group, "request-clarification", "RequestClarification", MasterStatuses.PendingApproval, MasterApprovalStatuses.ClarificationRequested, PagePermissionActions.RequestClarification);
        MapCustomerAction(group, "request-revision", "RequestRevision", MasterStatuses.Draft, MasterApprovalStatuses.RevisionRequested, PagePermissionActions.RequestRevision);
        MapCustomerAction(group, "resubmit", "Resubmit", MasterStatuses.PendingApproval, MasterApprovalStatuses.PendingApproval, PagePermissionActions.Resubmit);
        MapCustomerAction(group, "hold", "Hold", MasterStatuses.OnHold, MasterApprovalStatuses.Approved, PagePermissionActions.Deactivate);
        MapCustomerAction(group, "reactivate", "Reactivate", MasterStatuses.Active, MasterApprovalStatuses.Approved, PagePermissionActions.Update);
        MapCustomerAction(group, "deactivate", "Deactivate", MasterStatuses.Inactive, MasterApprovalStatuses.Approved, PagePermissionActions.Deactivate);

        group.MapGet("/customers/{customerCode}/status-history", (string customerCode, NexaErpDbContext db, CancellationToken cancellationToken) => MasterEndpointHelpers.GetStatusHistoryAsync(db, nameof(Customer), MasterEndpointHelpers.NormalizeCode(customerCode), cancellationToken)).RequirePagePermission("masters.customers", PagePermissionActions.ViewAuditHistory);
        group.MapGet("/customers/{customerCode}/approval-history", (string customerCode, NexaErpDbContext db, CancellationToken cancellationToken) => MasterEndpointHelpers.GetApprovalHistoryAsync(db, nameof(Customer), MasterEndpointHelpers.NormalizeCode(customerCode), cancellationToken)).RequirePagePermission("masters.customers", PagePermissionActions.ViewAuditHistory);
        group.MapGet("/customers/{customerCode}/audit-history", async (string customerCode, NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var id = await db.Customers.AsNoTracking().Where(customer => customer.CustomerCode == MasterEndpointHelpers.NormalizeCode(customerCode)).Select(customer => customer.Id.ToString()).SingleOrDefaultAsync(cancellationToken);
            return id is null ? Results.NotFound(new { message = "Customer not found." }) : await MasterEndpointHelpers.GetAuditHistoryAsync(db, nameof(Customer), id, cancellationToken);
        }).RequirePagePermission("masters.customers", PagePermissionActions.ViewAuditHistory);
    }

    private static void MapVendorEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/vendors", async (NexaErpDbContext db, IPagePermissionService permissions, ICurrentUser currentUser, int? page, int? pageSize, string? search, string? status, string? type, string? sortBy, string? sortDirection, CancellationToken cancellationToken) =>
        {
            var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize);
            var query = MasterEndpointHelpers.ApplyVendorOrganizationScope(db.Vendors.AsNoTracking(), currentUser);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpperInvariant();
                query = query.Where(vendor => vendor.VendorCode.ToUpper().Contains(term) || vendor.LegalVendorName.ToUpper().Contains(term) || (vendor.GstNumber != null && vendor.GstNumber.ToUpper().Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(vendor => vendor.VendorStatus == status.Trim());
            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(vendor => vendor.VendorType == type.Trim());
            var total = await query.CountAsync(cancellationToken);
            query = (sortBy?.Trim().ToLowerInvariant(), sortDirection?.Trim().ToLowerInvariant()) switch
            {
                ("name", "desc") => query.OrderByDescending(vendor => vendor.LegalVendorName),
                ("name", _) => query.OrderBy(vendor => vendor.LegalVendorName),
                ("status", "desc") => query.OrderByDescending(vendor => vendor.VendorStatus),
                ("status", _) => query.OrderBy(vendor => vendor.VendorStatus),
                ("code", "desc") => query.OrderByDescending(vendor => vendor.VendorCode),
                _ => query.OrderBy(vendor => vendor.VendorCode)
            };
            var canViewBank = await MasterEndpointHelpers.CanViewCommercialAsync(permissions, currentUser, "masters.vendors", cancellationToken);
            var rows = await query.Skip(paging.Skip).Take(paging.PageSize)
                .Select(vendor => new VendorSummary(vendor.Id, vendor.VendorCode, vendor.Name, vendor.GstNumber, vendor.PanNumber, vendor.ApprovalStatus, vendor.VendorStatus, vendor.IsActive, vendor.Version, canViewBank ? vendor.BankMetadataJson : null))
                .ToListAsync(cancellationToken);
            return Results.Ok(new PagedResponse<VendorSummary>(total, paging.PageNumber, paging.PageSize, rows));
        }).RequirePagePermission("masters.vendors", PagePermissionActions.View);

        group.MapGet("/vendors/{vendorCode}", async (string vendorCode, NexaErpDbContext db, IPagePermissionService permissions, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            var vendor = await db.Vendors.AsNoTracking().SingleOrDefaultAsync(existing => existing.VendorCode == MasterEndpointHelpers.NormalizeCode(vendorCode), cancellationToken);
            if (vendor is null) return Results.NotFound(new { message = "Vendor not found." });
            return Results.Ok(ToDetail(vendor, await MasterEndpointHelpers.CanViewCommercialAsync(permissions, currentUser, "masters.vendors", cancellationToken)));
        }).RequirePagePermission("masters.vendors", PagePermissionActions.View);

        group.MapPost("/vendors", async (UpsertVendorRequest request, NexaErpDbContext db, IAuditWriter audit, IPagePermissionService permissions, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            var validation = await ValidateVendorAsync(request, db, null, cancellationToken);
            if (validation is not null) return validation;
            var gstCertificateError = await ValidateVendorGstCertificateAsync(request.AttachmentMetadataJson, db, cancellationToken);
            if (gstCertificateError is not null) return Results.BadRequest(new { message = gstCertificateError });
            var bankMetadataError = ValidateVendorBankMetadata(request.BankMetadataJson);
            if (bankMetadataError is not null) return Results.BadRequest(new { message = bankMetadataError });
            var vendor = new Vendor { VendorCode = MasterEndpointHelpers.NormalizeCode(request.VendorCode), IsVendorCodeLocked = false };
            ApplyVendor(vendor, request, currentUser.LoginId, true);
            db.Vendors.Add(vendor);
            db.MasterStatusHistories.Add(new MasterStatusHistory { MasterType = nameof(Vendor), MasterId = vendor.Id, MasterCode = vendor.VendorCode, PreviousStatus = null, NewStatus = vendor.VendorStatus, Reason = "REV867 vendor draft created", SourceRevision = "REV867", CorrelationId = $"REV867_VENDOR_CREATE_{Guid.NewGuid():N}", CreatedBy = currentUser.LoginId });
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Masters", "CreateDraft", nameof(Vendor), vendor.Id.ToString(), null, vendor, cancellationToken);
            var canViewBank = await MasterEndpointHelpers.CanViewCommercialAsync(permissions, currentUser, "masters.vendors", cancellationToken);
            return Results.Created($"/api/v1/masters/vendors/{vendor.VendorCode}", ToSummary(vendor, canViewBank));
        }).RequirePagePermission("masters.vendors", PagePermissionActions.Create);

        group.MapPut("/vendors/{vendorCode}", async (string vendorCode, UpsertVendorRequest request, NexaErpDbContext db, IAuditWriter audit, IPagePermissionService permissions, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            var vendor = await db.Vendors.SingleOrDefaultAsync(existing => existing.VendorCode == MasterEndpointHelpers.NormalizeCode(vendorCode), cancellationToken);
            if (vendor is null) return Results.NotFound(new { message = "Vendor not found." });
            if (vendor.IsVendorCodeLocked && MasterEndpointHelpers.NormalizeCode(request.VendorCode) != vendor.VendorCode) return Results.BadRequest(new { message = "Vendor code is immutable after approval." });
            if (request.Version is null || MasterEndpointHelpers.IsMismatch(request.Version.Value, vendor.Version)) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
            var validation = await ValidateVendorAsync(request, db, vendor.Id, cancellationToken);
            if (validation is not null) return validation;
            var updateBankMetadataError = ValidateVendorBankMetadata(request.BankMetadataJson);
            if (updateBankMetadataError is not null) return Results.BadRequest(new { message = updateBankMetadataError });
            var before = ToDetail(vendor, true);
            var beforeControlled = VendorControlledSnapshot(vendor);
            var controlledChange = VendorControlledFieldsChanged(vendor, request);
            ApplyVendor(vendor, request, currentUser.LoginId, false);
            if (controlledChange)
            {
                var companyId = await db.Companies.AsNoTracking()
                    .Where(company => company.Code == currentUser.OrganizationId)
                    .Select(company => (Guid?)company.Id)
                    .SingleOrDefaultAsync(cancellationToken);
                if (companyId is null) return Results.BadRequest(new { message = "Company scope could not be resolved for re-verification evidence." });
                AddVendorReverificationEvidence(db, vendor, before.ApprovalStatus, beforeControlled, currentUser, companyId.Value);
            }
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Masters", "UpdateDraft", nameof(Vendor), vendor.Id.ToString(), before, vendor, cancellationToken);
            var canViewBank = await MasterEndpointHelpers.CanViewCommercialAsync(permissions, currentUser, "masters.vendors", cancellationToken);
            return Results.Ok(ToDetail(vendor, canViewBank));
        }).RequirePagePermission("masters.vendors", PagePermissionActions.Update);

        MapVendorAction(group, "submit", "Submit", MasterStatuses.PendingApproval, MasterApprovalStatuses.PendingApproval, PagePermissionActions.Submit);
        group.MapPost("/vendors/{vendorCode}/verify-commercial", VerifyVendorCommercial)
            .RequirePagePermission("masters.vendor-qualifications", PagePermissionActions.Verify);
        MapVendorAction(group, "approve", "Approve", MasterStatuses.Active, MasterApprovalStatuses.Approved, PagePermissionActions.Approve);
        MapVendorAction(group, "reject", "Reject", MasterStatuses.Rejected, MasterApprovalStatuses.Rejected, PagePermissionActions.Reject);
        MapVendorAction(group, "request-clarification", "RequestClarification", MasterStatuses.PendingApproval, MasterApprovalStatuses.ClarificationRequested, PagePermissionActions.RequestClarification);
        MapVendorAction(group, "request-revision", "RequestRevision", MasterStatuses.Draft, MasterApprovalStatuses.RevisionRequested, PagePermissionActions.RequestRevision);
        MapVendorAction(group, "resubmit", "Resubmit", MasterStatuses.PendingApproval, MasterApprovalStatuses.PendingApproval, PagePermissionActions.Resubmit);
        MapVendorAction(group, "hold", "Hold", MasterStatuses.OnHold, MasterApprovalStatuses.Approved, PagePermissionActions.Deactivate);
        MapVendorAction(group, "blacklist", "Blacklist", MasterStatuses.Blacklisted, MasterApprovalStatuses.Approved, PagePermissionActions.Deactivate);
        MapVendorAction(group, "reactivate", "Reactivate", MasterStatuses.Approved, MasterApprovalStatuses.Approved, PagePermissionActions.Update);
        MapVendorAction(group, "deactivate", "Deactivate", MasterStatuses.Inactive, MasterApprovalStatuses.Approved, PagePermissionActions.Deactivate);

        group.MapGet("/vendors/{vendorCode}/status-history", (string vendorCode, NexaErpDbContext db, CancellationToken cancellationToken) => MasterEndpointHelpers.GetStatusHistoryAsync(db, nameof(Vendor), MasterEndpointHelpers.NormalizeCode(vendorCode), cancellationToken)).RequirePagePermission("masters.vendors", PagePermissionActions.ViewAuditHistory);
        group.MapGet("/vendors/{vendorCode}/approval-history", (string vendorCode, NexaErpDbContext db, CancellationToken cancellationToken) => MasterEndpointHelpers.GetApprovalHistoryAsync(db, nameof(Vendor), MasterEndpointHelpers.NormalizeCode(vendorCode), cancellationToken)).RequirePagePermission("masters.vendors", PagePermissionActions.ViewAuditHistory);
        group.MapGet("/vendors/{vendorCode}/audit-history", async (string vendorCode, NexaErpDbContext db, IPagePermissionService permissions, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            var id = await db.Vendors.AsNoTracking().Where(vendor => vendor.VendorCode == MasterEndpointHelpers.NormalizeCode(vendorCode)).Select(vendor => vendor.Id.ToString()).SingleOrDefaultAsync(cancellationToken);
            if (id is null) return Results.NotFound(new { message = "Vendor not found." });
            var canViewBank = await MasterEndpointHelpers.CanViewCommercialAsync(permissions, currentUser, "masters.vendors", cancellationToken);
            return await MasterEndpointHelpers.GetAuditHistoryAsync(db, nameof(Vendor), id, cancellationToken, redactVendorBankMetadata: !canViewBank);
        }).RequirePagePermission("masters.vendors", PagePermissionActions.ViewAuditHistory);
    }

    private static void MapCustomerAction(RouteGroupBuilder group, string route, string action, string status, string approvalStatus, string permission)
    {
        group.MapPost($"/customers/{{customerCode}}/{route}", async (string customerCode, MasterActionRequest request, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            var customer = await db.Customers.SingleOrDefaultAsync(existing => existing.CustomerCode == MasterEndpointHelpers.NormalizeCode(customerCode), cancellationToken);
            if (customer is null) return Results.NotFound(new { message = "Customer not found." });
            return await MasterEndpointHelpers.ChangeLifecycleAsync(db, audit, currentUser, customer, nameof(Customer), customer.CustomerCode, action, status, approvalStatus, request.Remarks, request.Version, (entity, next, actor) => { entity.Status = next; entity.IsActive = next != MasterStatuses.Inactive; if (next == MasterStatuses.Active) { entity.IsCustomerCodeLocked = true; entity.ApprovedBy = actor; entity.ApprovedAt = DateTimeOffset.UtcNow; } }, entity => entity.Status, entity => entity.ApprovalStatus, (entity, next) => entity.ApprovalStatus = next, cancellationToken);
        }).RequirePagePermission("masters.customers", permission);
    }

    private static void MapVendorAction(RouteGroupBuilder group, string route, string action, string status, string approvalStatus, string permission)
    {
        group.MapPost($"/vendors/{{vendorCode}}/{route}", async (string vendorCode, MasterActionRequest request, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            var vendor = await db.Vendors.SingleOrDefaultAsync(existing => existing.VendorCode == MasterEndpointHelpers.NormalizeCode(vendorCode), cancellationToken);
            if (vendor is null) return Results.NotFound(new { message = "Vendor not found." });
            if (action == "Approve")
            {
                var gate = await ValidateVendorFinalApproval(vendor, db, currentUser, audit, cancellationToken);
                if (gate is not null) return gate;
            }
            return await MasterEndpointHelpers.ChangeLifecycleAsync(db, audit, currentUser, vendor, nameof(Vendor), vendor.VendorCode, action, status, approvalStatus, request.Remarks, request.Version, (entity, next, actor) => { entity.VendorStatus = next; entity.IsActive = next is not MasterStatuses.Inactive and not MasterStatuses.Blacklisted; if (next == MasterStatuses.Active) { entity.IsVendorCodeLocked = true; entity.ApprovedBy = actor; entity.ApprovedAt = DateTimeOffset.UtcNow; entity.RequiresReverification = false; } }, entity => entity.VendorStatus, entity => entity.ApprovalStatus, (entity, next) => entity.ApprovalStatus = next, cancellationToken);
        }).RequirePagePermission("masters.vendors", permission);
    }

    private static async Task<IResult?> ValidateCustomerAsync(UpsertCustomerRequest request, NexaErpDbContext db, Guid? currentId, CancellationToken cancellationToken)
    {
        var code = MasterEndpointHelpers.NormalizeCode(request.CustomerCode);
        var gst = MasterEndpointHelpers.NormalizeUpperOptional(request.GstNumber);
        var pan = MasterEndpointHelpers.NormalizeUpperOptional(request.PanNumber);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.LegalCustomerName) || string.IsNullOrWhiteSpace(request.CustomerType)) return Results.BadRequest(new { message = "Customer code, legal name and type are required." });
        if (!MasterEndpointHelpers.IsValidGstin(gst)) return Results.BadRequest(new { message = "Invalid Indian GSTIN format." });
        if (!MasterEndpointHelpers.IsValidPan(pan)) return Results.BadRequest(new { message = "Invalid Indian PAN format." });
        if (await db.Customers.AnyAsync(customer => customer.Id != currentId && (customer.CustomerCode == code || (gst != null && customer.GstNumber == gst)), cancellationToken)) return Results.Conflict(new { message = "Duplicate customer code/GST blocked." });
        return null;
    }

    private static async Task<IResult?> ValidateVendorAsync(UpsertVendorRequest request, NexaErpDbContext db, Guid? currentId, CancellationToken cancellationToken)
    {
        var code = MasterEndpointHelpers.NormalizeCode(request.VendorCode);
        var gst = MasterEndpointHelpers.NormalizeUpperOptional(request.GstNumber);
        var pan = MasterEndpointHelpers.NormalizeUpperOptional(request.PanNumber);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.LegalVendorName) || string.IsNullOrWhiteSpace(request.VendorType)) return Results.BadRequest(new { message = "Vendor code, legal name and type are required." });
        if (!MasterEndpointHelpers.IsValidGstin(gst)) return Results.BadRequest(new { message = "Invalid Indian GSTIN format." });
        if (!MasterEndpointHelpers.IsValidPan(pan)) return Results.BadRequest(new { message = "Invalid Indian PAN format." });
        if (await db.Vendors.AnyAsync(vendor => vendor.Id != currentId && (vendor.VendorCode == code || (gst != null && vendor.GstNumber == gst) || (pan != null && vendor.PanNumber == pan && vendor.LegalVendorName == request.LegalVendorName.Trim())), cancellationToken)) return Results.Conflict(new { message = "Duplicate vendor identity blocked." });
        return null;
    }

    private static void ApplyCustomer(Customer customer, UpsertCustomerRequest request, string loginId, bool create)
    {
        customer.CustomerCode = MasterEndpointHelpers.NormalizeCode(request.CustomerCode);
        customer.LegalCustomerName = request.LegalCustomerName.Trim();
        customer.Name = customer.LegalCustomerName;
        customer.TradeName = MasterEndpointHelpers.NormalizeOptional(request.TradeName);
        customer.CustomerType = request.CustomerType.Trim();
        customer.GstNumber = MasterEndpointHelpers.NormalizeUpperOptional(request.GstNumber);
        customer.PanNumber = MasterEndpointHelpers.NormalizeUpperOptional(request.PanNumber);
        customer.BillingAddress = MasterEndpointHelpers.NormalizeOptional(request.BillingAddress);
        customer.ShippingAddress = MasterEndpointHelpers.NormalizeOptional(request.ShippingAddress);
        customer.State = MasterEndpointHelpers.NormalizeOptional(request.State);
        customer.StateCode = MasterEndpointHelpers.NormalizeUpperOptional(request.StateCode);
        customer.Country = string.IsNullOrWhiteSpace(request.Country) ? "India" : request.Country.Trim();
        customer.ContactPerson = MasterEndpointHelpers.NormalizeOptional(request.ContactPerson);
        customer.Phone = MasterEndpointHelpers.NormalizeOptional(request.Phone);
        customer.Email = MasterEndpointHelpers.NormalizeOptional(request.Email);
        customer.Industry = MasterEndpointHelpers.NormalizeOptional(request.Industry);
        customer.PaymentTerms = MasterEndpointHelpers.NormalizeOptional(request.PaymentTerms);
        customer.CreditPeriodDays = request.CreditPeriodDays;
        customer.CreditLimit = request.CreditLimit;
        customer.PortalOrganizationId = string.IsNullOrWhiteSpace(request.PortalOrganizationId) ? customer.CustomerCode : request.PortalOrganizationId.Trim();
        if (create) customer.CreatedBy = loginId; else { customer.UpdatedBy = loginId; customer.UpdatedAt = DateTimeOffset.UtcNow; }
    }

    private static void ApplyVendor(Vendor vendor, UpsertVendorRequest request, string loginId, bool create)
    {
        vendor.VendorCode = MasterEndpointHelpers.NormalizeCode(request.VendorCode);
        vendor.LegalVendorName = request.LegalVendorName.Trim();
        vendor.Name = vendor.LegalVendorName;
        vendor.TradeName = MasterEndpointHelpers.NormalizeOptional(request.TradeName);
        vendor.VendorType = request.VendorType.Trim();
        vendor.GstNumber = MasterEndpointHelpers.NormalizeUpperOptional(request.GstNumber);
        vendor.PanNumber = MasterEndpointHelpers.NormalizeUpperOptional(request.PanNumber);
        vendor.MsmeStatus = request.MsmeStatus;
        vendor.MsmeNumber = MasterEndpointHelpers.NormalizeOptional(request.MsmeNumber);
        vendor.ContactPerson = MasterEndpointHelpers.NormalizeOptional(request.ContactPerson);
        vendor.Phone = MasterEndpointHelpers.NormalizeOptional(request.Phone);
        vendor.Email = MasterEndpointHelpers.NormalizeOptional(request.Email);
        vendor.BillingAddress = MasterEndpointHelpers.NormalizeOptional(request.BillingAddress);
        vendor.ShippingAddress = MasterEndpointHelpers.NormalizeOptional(request.ShippingAddress);
        vendor.State = MasterEndpointHelpers.NormalizeOptional(request.State);
        vendor.StateCode = MasterEndpointHelpers.NormalizeUpperOptional(request.StateCode);
        vendor.Country = string.IsNullOrWhiteSpace(request.Country) ? "India" : request.Country.Trim();
        vendor.MaterialServiceCategories = MasterEndpointHelpers.NormalizeOptional(request.MaterialServiceCategories);
        vendor.ApprovedMakes = MasterEndpointHelpers.NormalizeOptional(request.ApprovedMakes);
        vendor.PaymentTerms = MasterEndpointHelpers.NormalizeOptional(request.PaymentTerms);
        vendor.DeliveryTerms = MasterEndpointHelpers.NormalizeOptional(request.DeliveryTerms);
        vendor.CreditPeriodDays = request.CreditPeriodDays;
        vendor.BankMetadataJson = MasterEndpointHelpers.NormalizeOptional(request.BankMetadataJson);
        vendor.AttachmentMetadataJson = MasterEndpointHelpers.NormalizeOptional(request.AttachmentMetadataJson);
        vendor.PortalOrganizationId = vendor.VendorCode;
        if (create) vendor.CreatedBy = loginId; else { vendor.UpdatedBy = loginId; vendor.UpdatedAt = DateTimeOffset.UtcNow; }
    }

    private static CustomerSummary ToSummary(Customer customer, bool canViewCredit) => new(customer.Id, customer.CustomerCode, customer.Name, customer.GstNumber, customer.PanNumber, customer.PortalOrganizationId, customer.Status, customer.ApprovalStatus, customer.IsActive, customer.Version, canViewCredit ? customer.CreditLimit : null);

    private static CustomerDetail ToDetail(Customer customer, bool canViewCredit) => new(customer.Id, customer.CustomerCode, customer.Name, customer.LegalCustomerName, customer.TradeName, customer.CustomerType, customer.GstNumber, customer.PanNumber, customer.BillingAddress, customer.ShippingAddress, customer.State, customer.StateCode, customer.Country, customer.ContactPerson, customer.Phone, customer.Email, customer.Industry, customer.PaymentTerms, customer.CreditPeriodDays, canViewCredit ? customer.CreditLimit : null, customer.PortalOrganizationId, customer.Status, customer.ApprovalStatus, customer.IsActive, customer.Version);

    private static VendorSummary ToSummary(Vendor vendor, bool canViewBank) => new(vendor.Id, vendor.VendorCode, vendor.Name, vendor.GstNumber, vendor.PanNumber, vendor.ApprovalStatus, vendor.VendorStatus, vendor.IsActive, vendor.Version, canViewBank ? vendor.BankMetadataJson : null);

    private static VendorDetail ToDetail(Vendor vendor, bool canViewBank) => new(vendor.Id, vendor.VendorCode, vendor.Name, vendor.LegalVendorName, vendor.TradeName, vendor.VendorType, vendor.GstNumber, vendor.PanNumber, vendor.MsmeStatus, vendor.MsmeNumber, vendor.ContactPerson, vendor.Phone, vendor.Email, vendor.BillingAddress, vendor.ShippingAddress, vendor.State, vendor.StateCode, vendor.Country, vendor.MaterialServiceCategories, vendor.ApprovedMakes, vendor.PaymentTerms, vendor.DeliveryTerms, vendor.CreditPeriodDays, canViewBank ? vendor.BankMetadataJson : null, vendor.AttachmentMetadataJson, vendor.ApprovalStatus, vendor.VendorStatus, vendor.IsActive, vendor.Version);
}

