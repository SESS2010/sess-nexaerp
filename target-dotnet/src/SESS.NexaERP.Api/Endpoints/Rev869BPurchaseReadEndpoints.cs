using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class Rev869BPurchaseEndpoints
{
    private static async Task<IResult> ListRfqs(string? rfqNumber, string? status, DateOnly? from, DateOnly? to,
        Guid? vendorId, string? vendor, int? page, int? pageSize, string? sortBy, string? sortDirection,
        NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        if (InvalidDates(from, to)) return DateError();
        var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize);
        var query = ScopeRfqs(db.RequestForQuotations.AsNoTracking(), db, user);
        if (!string.IsNullOrWhiteSpace(rfqNumber)) { var number = Normalize(rfqNumber); query = query.Where(x => x.RfqNumber == number); }
        if (!string.IsNullOrWhiteSpace(status)) { var value = Normalize(status); query = query.Where(x => x.Status == value); }
        query = DateRange(query, from, to);
        if (vendorId.HasValue) query = query.Where(x => x.Invitations.Any(i => i.VendorId == vendorId));
        if (!string.IsNullOrWhiteSpace(vendor))
        {
            var value = Normalize(vendor);
            query = query.Where(x => x.Invitations.Any(i => i.Vendor != null && (i.Vendor.VendorCode == value || i.Vendor.Name.ToUpper() == value)));
        }
        var total = await query.CountAsync(ct);
        var rows = await SortRfqs(query, sortBy, sortDirection).Skip(paging.Skip).Take(paging.PageSize)
            .Select(x => new RfqListItem(x.Id, x.RfqNumber, x.QuoteDueAt, x.Status, x.Invitations.Count, x.CreatedAt, x.Version)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<RfqListItem>(total, paging.PageNumber, paging.PageSize, rows));
    }

    private static async Task<IResult> ListQuotations(string? quotationNumber, string? status, DateOnly? from, DateOnly? to,
        Guid? vendorId, string? vendor, int? page, int? pageSize, string? sortBy, string? sortDirection,
        NexaErpDbContext db, ICurrentUser user, IPagePermissionService permissions, IAuditWriter audit, CancellationToken ct)
    {
        if (InvalidDates(from, to)) return DateError();
        var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize);
        var query = ScopeQuotations(db.VendorQuotations.AsNoTracking(), db, user).Where(x => x.IsCurrentRevision);
        if (!string.IsNullOrWhiteSpace(quotationNumber)) { var number = Normalize(quotationNumber); query = query.Where(x => x.QuotationNumber == number); }
        if (!string.IsNullOrWhiteSpace(status)) { var value = Normalize(status); query = query.Where(x => x.Status == value); }
        query = DateRange(query, from, to);
        if (vendorId.HasValue) query = query.Where(x => x.VendorId == vendorId);
        if (!string.IsNullOrWhiteSpace(vendor)) { var value = Normalize(vendor); query = query.Where(x => x.Vendor != null && (x.Vendor.VendorCode == value || x.Vendor.Name.ToUpper() == value)); }
        var showCommercial = await permissions.HasPermissionAsync(user.RoleCodes, "purchase.vendor-quotations", PagePermissionActions.ViewCommercialValues, ct);
        if (!showCommercial) await Masked(audit, "purchase.vendor-quotations", user, ct);
        var total = await query.CountAsync(ct);
        var rows = await SortQuotations(query, sortBy, sortDirection).Skip(paging.Skip).Take(paging.PageSize)
            .Select(x => new QuotationListItem(x.Id, x.QuotationNumber, x.RfqVendorInvitation!.RequestForQuotation!.RfqNumber,
                x.VendorId, x.Vendor!.VendorCode, x.Vendor.Name, x.RevisionNumber, x.ReceivedAt, x.Status,
                showCommercial ? x.TotalPayableValue : null, x.Version)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<QuotationListItem>(total, paging.PageNumber, paging.PageSize, rows));
    }

    private static async Task<IResult> ListComparisons(string? comparisonNumber, string? status, DateOnly? from, DateOnly? to,
        Guid? vendorId, string? vendor, int? page, int? pageSize, string? sortBy, string? sortDirection,
        NexaErpDbContext db, ICurrentUser user, IPagePermissionService permissions, IAuditWriter audit, CancellationToken ct)
    {
        if (InvalidDates(from, to)) return DateError();
        var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize);
        var query = ScopeComparisons(db.CommercialComparisons.AsNoTracking(), db, user);
        if (!string.IsNullOrWhiteSpace(comparisonNumber)) { var number = Normalize(comparisonNumber); query = query.Where(x => x.ComparisonNumber == number); }
        if (!string.IsNullOrWhiteSpace(status)) { var value = Normalize(status); query = query.Where(x => x.Status == value); }
        query = DateRange(query, from, to);
        if (vendorId.HasValue) query = query.Where(x => x.Lines.Any(line => line.VendorId == vendorId));
        if (!string.IsNullOrWhiteSpace(vendor)) { var value = Normalize(vendor); query = query.Where(x => x.Lines.Any(line => line.Vendor != null && (line.Vendor.VendorCode == value || line.Vendor.Name.ToUpper() == value))); }
        var showCommercial = await permissions.HasPermissionAsync(user.RoleCodes, "purchase.commercial-comparisons", PagePermissionActions.ViewCommercialValues, ct);
        if (!showCommercial) await Masked(audit, "purchase.commercial-comparisons", user, ct);
        var total = await query.CountAsync(ct);
        var rows = await SortComparisons(query, sortBy, sortDirection).Skip(paging.Skip).Take(paging.PageSize)
            .Select(x => new ComparisonListItem(x.Id, x.ComparisonNumber, x.RequestForQuotation!.RfqNumber, x.SelectedVendorId,
                x.SelectedVendor == null ? null : x.SelectedVendor.VendorCode, x.SelectedVendor == null ? null : x.SelectedVendor.Name,
                x.Status, x.CreatedAt, showCommercial ? x.TotalPayableValue : null, x.Version)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<ComparisonListItem>(total, paging.PageNumber, paging.PageSize, rows));
    }

    private static async Task<IResult> ListPurchaseOrders(string? purchaseOrderNumber, string? status, DateOnly? from, DateOnly? to,
        Guid? vendorId, string? vendor, int? page, int? pageSize, string? sortBy, string? sortDirection,
        NexaErpDbContext db, ICurrentUser user, IPagePermissionService permissions, IAuditWriter audit, CancellationToken ct)
    {
        if (InvalidDates(from, to)) return DateError();
        var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize);
        var query = ScopePurchaseOrders(db.PurchaseOrders.AsNoTracking(), db, user).Where(x => x.IsCurrentVersion);
        if (!string.IsNullOrWhiteSpace(purchaseOrderNumber)) { var number = Normalize(purchaseOrderNumber); query = query.Where(x => x.PoNumber == number); }
        if (!string.IsNullOrWhiteSpace(status)) { var value = Normalize(status); query = query.Where(x => x.Status == value); }
        query = DateRange(query, from, to);
        if (vendorId.HasValue) query = query.Where(x => x.VendorId == vendorId);
        if (!string.IsNullOrWhiteSpace(vendor)) { var value = Normalize(vendor); query = query.Where(x => x.Vendor != null && (x.Vendor.VendorCode == value || x.Vendor.Name.ToUpper() == value)); }
        var showCommercial = await permissions.HasPermissionAsync(user.RoleCodes, "purchase.po", PagePermissionActions.ViewCommercialValues, ct);
        if (!showCommercial) await Masked(audit, "purchase.po", user, ct);
        var total = await query.CountAsync(ct);
        var rows = await SortPurchaseOrders(query, sortBy, sortDirection).Skip(paging.Skip).Take(paging.PageSize)
            .Select(x => new PurchaseOrderListItem(x.Id, x.PoNumber, x.RevisionNumber, x.VendorId, x.Vendor!.VendorCode,
                x.Vendor.Name, x.Status, x.CreatedAt, x.IssuedAt, showCommercial ? x.TotalPayableValue : null, x.Version)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<PurchaseOrderListItem>(total, paging.PageNumber, paging.PageSize, rows));
    }

    private static async Task<IResult> GetQuotation(string number, NexaErpDbContext db, ICurrentUser user,
        IPagePermissionService permissions, IAuditWriter audit, CancellationToken ct)
    {
        var normalized = Normalize(number);
        var row = await ScopeQuotations(db.VendorQuotations.AsNoTracking().Include(x => x.Vendor).Include(x => x.Lines)
            .Include(x => x.RfqVendorInvitation)!.ThenInclude(x => x!.RequestForQuotation), db, user)
            .SingleOrDefaultAsync(x => x.QuotationNumber == normalized && x.IsCurrentRevision, ct);
        if (row is null) return await Missing(audit, "purchase.vendor-quotations", number, user, ct);
        if (await permissions.HasPermissionAsync(user.RoleCodes, "purchase.vendor-quotations", PagePermissionActions.ViewCommercialValues, ct))
            return Results.Ok(new { row.Id, row.QuotationNumber, RfqNumber = row.RfqVendorInvitation!.RequestForQuotation!.RfqNumber,
                row.VendorId, VendorCode = row.Vendor!.VendorCode, VendorName = row.Vendor.Name, row.RevisionNumber, row.IsCurrentRevision,
                row.VendorQuoteReference, row.SubmissionSource, row.ReceivedAt, row.Status, row.SubmittedAt, row.IsLateSubmission,
                row.PaymentTermsSnapshot, row.DeliveryTermsSnapshot, row.WarrantyTermsSnapshot, row.TotalPayableValue,
                row.HeaderDiscountValue, row.Version, Lines = row.Lines.Select(x => new { x.Id, x.LineNumber, x.Quantity,
                    x.UnitRate, x.DiscountValue, x.HeaderDiscountValue, x.PackingForwarding, x.Freight, x.Insurance,
                    x.OtherCharges, x.TaxableValue, x.CgstValue, x.SgstValue, x.IgstValue, x.CessValue, x.RoundOff,
                    x.TotalPayableValue, x.HsnSacCode, x.PromisedDeliveryDate }) });
        await Masked(audit, "purchase.vendor-quotations", user, ct, row.Id.ToString());
        return Results.Ok(new { row.Id, row.QuotationNumber, RfqNumber = row.RfqVendorInvitation!.RequestForQuotation!.RfqNumber,
            row.VendorId, VendorCode = row.Vendor!.VendorCode, VendorName = row.Vendor.Name, row.RevisionNumber, row.IsCurrentRevision,
            row.VendorQuoteReference, row.SubmissionSource, row.ReceivedAt, row.Status, row.SubmittedAt, row.IsLateSubmission,
            row.PaymentTermsSnapshot, row.DeliveryTermsSnapshot, row.WarrantyTermsSnapshot, row.Version,
            Lines = row.Lines.Select(x => new { x.Id, x.LineNumber, x.Quantity, x.HsnSacCode, x.PromisedDeliveryDate }) });
    }

    private static IQueryable<RequestForQuotation> ScopeRfqs(IQueryable<RequestForQuotation> query, NexaErpDbContext db, ICurrentUser user)
    {
        if (!ValidUser(user)) return query.Where(_ => false);
        query = query.Where(x => x.OrganizationId == user.OrganizationId);
        var scopes = EffectiveScopes(db, user);
        if (CrossScope(user, scopes)) return query;
        var employeeId = user.EmployeeId!.Value;
        return query.Where(x => scopes.Any(s => (!s.DepartmentId.HasValue || s.DepartmentId == x.RequestingDepartmentId) &&
            (!s.WarehouseId.HasValue || s.WarehouseId == x.DeliveryWarehouseId) && !s.RackBinId.HasValue && (!s.OwnRecordsOnly || x.OwnerEmployeeId == employeeId)));
    }

    private static IQueryable<VendorQuotation> ScopeQuotations(IQueryable<VendorQuotation> query, NexaErpDbContext db, ICurrentUser user)
    {
        if (!ValidUser(user)) return query.Where(_ => false);
        query = query.Where(x => x.OrganizationId == user.OrganizationId);
        var scopes = EffectiveScopes(db, user);
        if (CrossScope(user, scopes)) return query;
        var employeeId = user.EmployeeId!.Value;
        return query.Where(x => scopes.Any(s => (!s.DepartmentId.HasValue || s.DepartmentId == x.RfqVendorInvitation!.RequestForQuotation!.RequestingDepartmentId) &&
            (!s.WarehouseId.HasValue || s.WarehouseId == x.RfqVendorInvitation!.RequestForQuotation!.DeliveryWarehouseId) && !s.RackBinId.HasValue &&
            (!s.OwnRecordsOnly || x.RfqVendorInvitation!.RequestForQuotation!.OwnerEmployeeId == employeeId)));
    }

    private static IQueryable<CommercialComparison> ScopeComparisons(IQueryable<CommercialComparison> query, NexaErpDbContext db, ICurrentUser user)
    {
        if (!ValidUser(user)) return query.Where(_ => false);
        query = query.Where(x => x.OrganizationId == user.OrganizationId);
        var scopes = EffectiveScopes(db, user);
        if (CrossScope(user, scopes)) return query;
        var employeeId = user.EmployeeId!.Value;
        return query.Where(x => scopes.Any(s => (!s.DepartmentId.HasValue || s.DepartmentId == x.RequestForQuotation!.RequestingDepartmentId) &&
            (!s.WarehouseId.HasValue || s.WarehouseId == x.RequestForQuotation!.DeliveryWarehouseId) && !s.RackBinId.HasValue && (!s.OwnRecordsOnly || x.OwnerEmployeeId == employeeId)));
    }

    private static IQueryable<PurchaseOrder> ScopePurchaseOrders(IQueryable<PurchaseOrder> query, NexaErpDbContext db, ICurrentUser user)
    {
        if (!ValidUser(user)) return query.Where(_ => false);
        query = query.Where(x => x.OrganizationId == user.OrganizationId);
        var scopes = EffectiveScopes(db, user);
        if (CrossScope(user, scopes)) return query;
        var employeeId = user.EmployeeId!.Value;
        return query.Where(x => scopes.Any(s => (!s.DepartmentId.HasValue || s.DepartmentId == x.RequestingDepartmentId) &&
            (!s.WarehouseId.HasValue || s.WarehouseId == x.DeliveryWarehouseId) && !s.RackBinId.HasValue && (!s.OwnRecordsOnly || x.OwnerEmployeeId == employeeId)));
    }

    private static IQueryable<EmployeeOperationalScope> EffectiveScopes(NexaErpDbContext db, ICurrentUser user)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var employeeId = user.EmployeeId!.Value;
        return db.EmployeeOperationalScopes.Where(x => x.OrganizationId == user.OrganizationId && x.EmployeeId == employeeId && x.IsActive &&
            x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today));
    }

    private static bool CrossScope(ICurrentUser user, IQueryable<EmployeeOperationalScope> scopes) =>
        Rev869ARoleCodes.IsExplicitCrossScopeRole(user.RoleCode) && scopes.Any(x => x.AllowsPrivilegedCrossScope);
    private static bool ValidUser(ICurrentUser user) => user.IsAuthenticated && user.EmployeeId.HasValue && !string.IsNullOrWhiteSpace(user.OrganizationId);
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static bool InvalidDates(DateOnly? from, DateOnly? to) => from.HasValue && to.HasValue && from > to;
    private static IResult DateError() => Results.BadRequest(new { message = "from must be on or before to." });
    private static DateTimeOffset Start(DateOnly value) => new(value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

    private static IQueryable<T> DateRange<T>(IQueryable<T> query, DateOnly? from, DateOnly? to) where T : SESS.NexaERP.Domain.Common.AuditableEntity
    {
        if (from.HasValue) { var start = Start(from.Value); query = query.Where(x => x.CreatedAt >= start); }
        if (to.HasValue) { var end = Start(to.Value.AddDays(1)); query = query.Where(x => x.CreatedAt < end); }
        return query;
    }

    private static IOrderedQueryable<RequestForQuotation> SortRfqs(IQueryable<RequestForQuotation> q, string? by, string? direction) =>
        (by?.Trim().ToLowerInvariant(), Desc(direction)) switch { ("status", true) => q.OrderByDescending(x => x.Status).ThenByDescending(x => x.Id), ("status", false) => q.OrderBy(x => x.Status).ThenBy(x => x.Id), ("date", true) => q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), ("date", false) => q.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id), ("rfqnumber", true) => q.OrderByDescending(x => x.RfqNumber).ThenByDescending(x => x.Id), _ => q.OrderBy(x => x.RfqNumber).ThenBy(x => x.Id) };
    private static IOrderedQueryable<VendorQuotation> SortQuotations(IQueryable<VendorQuotation> q, string? by, string? direction) =>
        (by?.Trim().ToLowerInvariant(), Desc(direction)) switch { ("status", true) => q.OrderByDescending(x => x.Status).ThenByDescending(x => x.Id), ("status", false) => q.OrderBy(x => x.Status).ThenBy(x => x.Id), ("date", true) => q.OrderByDescending(x => x.ReceivedAt).ThenByDescending(x => x.Id), ("date", false) => q.OrderBy(x => x.ReceivedAt).ThenBy(x => x.Id), ("quotationnumber", true) => q.OrderByDescending(x => x.QuotationNumber).ThenByDescending(x => x.Id), _ => q.OrderBy(x => x.QuotationNumber).ThenBy(x => x.Id) };
    private static IOrderedQueryable<CommercialComparison> SortComparisons(IQueryable<CommercialComparison> q, string? by, string? direction) =>
        (by?.Trim().ToLowerInvariant(), Desc(direction)) switch { ("status", true) => q.OrderByDescending(x => x.Status).ThenByDescending(x => x.Id), ("status", false) => q.OrderBy(x => x.Status).ThenBy(x => x.Id), ("date", true) => q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), ("date", false) => q.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id), ("comparisonnumber", true) => q.OrderByDescending(x => x.ComparisonNumber).ThenByDescending(x => x.Id), _ => q.OrderBy(x => x.ComparisonNumber).ThenBy(x => x.Id) };
    private static IOrderedQueryable<PurchaseOrder> SortPurchaseOrders(IQueryable<PurchaseOrder> q, string? by, string? direction) =>
        (by?.Trim().ToLowerInvariant(), Desc(direction)) switch { ("status", true) => q.OrderByDescending(x => x.Status).ThenByDescending(x => x.Id), ("status", false) => q.OrderBy(x => x.Status).ThenBy(x => x.Id), ("date", true) => q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), ("date", false) => q.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id), ("purchaseordernumber", true) => q.OrderByDescending(x => x.PoNumber).ThenByDescending(x => x.Id), _ => q.OrderBy(x => x.PoNumber).ThenBy(x => x.Id) };
    private static bool Desc(string? direction) => string.Equals(direction?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);
    private static Task Masked(IAuditWriter audit, string page, ICurrentUser user, CancellationToken ct, string record = "list") =>
        audit.WriteAsync("Security", "Denied", "CommercialValues", record, null, new { reason = "Commercial values masked", page, user.RoleCode }, ct);
}
