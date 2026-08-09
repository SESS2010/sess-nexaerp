using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class PurchaseRequisitionEndpoints
{
    private static async Task<IResult?> ValidateDraftAsync(CreatePurchaseRequisitionRequest request, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationId) || string.IsNullOrWhiteSpace(request.RequestingDepartmentCode) || string.IsNullOrWhiteSpace(request.RequesterEmployeeCode) || string.IsNullOrWhiteSpace(request.DeliveryWarehouseCode) || string.IsNullOrWhiteSpace(request.PurposeJustification)) return Results.BadRequest(new { message = "Organization, department, requester, delivery warehouse and purpose are required." });
        if (!string.IsNullOrWhiteSpace(user.OrganizationId) && !string.Equals(request.OrganizationId.Trim(), user.OrganizationId, StringComparison.OrdinalIgnoreCase)) return Results.Forbid();
        if (!await db.Departments.AnyAsync(x => x.Code == MasterEndpointHelpers.NormalizeCode(request.RequestingDepartmentCode), ct)) return Results.BadRequest(new { message = "Requesting department not found." });
        if (!await db.Employees.AnyAsync(x => x.EmployeeCode == MasterEndpointHelpers.NormalizeCode(request.RequesterEmployeeCode), ct)) return Results.BadRequest(new { message = "Requester employee not found." });
        if (!await db.Warehouses.AnyAsync(x => x.WarehouseCode == MasterEndpointHelpers.NormalizeCode(request.DeliveryWarehouseCode) && x.IsActive, ct)) return Results.BadRequest(new { message = "Active delivery warehouse not found." });
        return await ValidateLines(request.Lines, db, ct);
    }

    private static async Task<IResult?> ValidateDraftAsync(UpdatePurchaseRequisitionRequest request, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DeliveryWarehouseCode) || string.IsNullOrWhiteSpace(request.PurposeJustification)) return Results.BadRequest(new { message = "Delivery warehouse and purpose are required." });
        if (!await db.Warehouses.AnyAsync(x => x.WarehouseCode == MasterEndpointHelpers.NormalizeCode(request.DeliveryWarehouseCode) && x.IsActive, ct)) return Results.BadRequest(new { message = "Active delivery warehouse not found." });
        return await ValidateLines(request.Lines, db, ct);
    }

    private static async Task<IResult?> ValidateLines(IReadOnlyList<PurchaseRequisitionLineRequest> lines, NexaErpDbContext db, CancellationToken ct)
    {
        if (lines.Count == 0) return Results.BadRequest(new { message = "At least one PR line is required." });
        var lineNo = 0;
        foreach (var line in lines)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line.ItemCode)) return Results.BadRequest(new { message = $"Line {lineNo}: Item Master reference is required. Use controlled New Item Request for missing items." });
            if (line.RequestedQuantity <= 0 || line.EstimatedUnitPrice < 0) return Results.BadRequest(new { message = $"Line {lineNo}: requested quantity must be positive and estimated price cannot be negative." });
            var itemCode = MasterEndpointHelpers.NormalizeCode(line.ItemCode);
            if (!await db.Items.AnyAsync(x => x.ItemCode == itemCode && x.IsActive, ct)) return Results.BadRequest(new { message = $"Line {lineNo}: active Item Master record not found. New item request is required." });
            if (!string.IsNullOrWhiteSpace(line.PreferredWarehouseCode) && !await db.Warehouses.AnyAsync(x => x.WarehouseCode == MasterEndpointHelpers.NormalizeCode(line.PreferredWarehouseCode) && x.IsActive, ct)) return Results.BadRequest(new { message = $"Line {lineNo}: active preferred warehouse not found." });
        }
        return null;
    }

    private static async Task<PurchaseRequisition> BuildDraftAsync(CreatePurchaseRequisitionRequest request, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var organization = string.IsNullOrWhiteSpace(user.OrganizationId) ? request.OrganizationId.Trim() : user.OrganizationId;
        var pr = new PurchaseRequisition { OrganizationId = organization, RequestDate = DateOnly.FromDateTime(DateTime.UtcNow), RequiredByDate = request.RequiredByDate, Priority = request.Priority.Trim(), PurposeJustification = request.PurposeJustification.Trim(), CostCentre = Norm(request.CostCentre), ProjectReference = Norm(request.ProjectReference), ServiceReference = Norm(request.ServiceReference), WorkOrderReference = Norm(request.WorkOrderReference), CustomerReference = Norm(request.CustomerReference), CreatedBy = user.LoginId };
        pr.RequestingDepartmentId = await db.Departments.Where(x => x.Code == MasterEndpointHelpers.NormalizeCode(request.RequestingDepartmentCode)).Select(x => x.Id).SingleAsync(ct);
        pr.RequesterEmployeeId = await db.Employees.Where(x => x.EmployeeCode == MasterEndpointHelpers.NormalizeCode(request.RequesterEmployeeCode)).Select(x => x.Id).SingleAsync(ct);
        pr.DeliveryWarehouseId = await db.Warehouses.Where(x => x.WarehouseCode == MasterEndpointHelpers.NormalizeCode(request.DeliveryWarehouseCode) && x.IsActive).Select(x => x.Id).SingleAsync(ct);
        await ApplyLines(pr, request.Lines, db, ct);
        return pr;
    }

    private static async Task ApplyDraftAsync(PurchaseRequisition pr, UpdatePurchaseRequisitionRequest request, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        pr.RequiredByDate = request.RequiredByDate;
        pr.Priority = request.Priority.Trim();
        pr.PurposeJustification = request.PurposeJustification.Trim();
        pr.CostCentre = Norm(request.CostCentre);
        pr.ProjectReference = Norm(request.ProjectReference);
        pr.ServiceReference = Norm(request.ServiceReference);
        pr.WorkOrderReference = Norm(request.WorkOrderReference);
        pr.CustomerReference = Norm(request.CustomerReference);
        pr.DeliveryWarehouseId = await db.Warehouses.Where(x => x.WarehouseCode == MasterEndpointHelpers.NormalizeCode(request.DeliveryWarehouseCode) && x.IsActive).Select(x => x.Id).SingleAsync(ct);
        pr.UpdatedBy = user.LoginId;
        pr.UpdatedAt = DateTimeOffset.UtcNow;
        pr.Lines.Clear();
        await ApplyLines(pr, request.Lines, db, ct);
    }

    private static async Task ApplyLines(PurchaseRequisition pr, IReadOnlyList<PurchaseRequisitionLineRequest> lines, NexaErpDbContext db, CancellationToken ct)
    {
        var lineNo = 0;
        pr.EstimatedTotal = 0;
        foreach (var request in lines)
        {
            lineNo++;
            var itemCode = MasterEndpointHelpers.NormalizeCode(request.ItemCode);
            var item = await db.Items.AsNoTracking().SingleAsync(x => x.ItemCode == itemCode && x.IsActive, ct);
            Guid? warehouseId = null;
            if (!string.IsNullOrWhiteSpace(request.PreferredWarehouseCode)) warehouseId = await db.Warehouses.Where(x => x.WarehouseCode == MasterEndpointHelpers.NormalizeCode(request.PreferredWarehouseCode) && x.IsActive).Select(x => x.Id).SingleAsync(ct);
            var total = decimal.Round(request.RequestedQuantity * request.EstimatedUnitPrice, 2);
            pr.EstimatedTotal += total;
            pr.Lines.Add(new PurchaseRequisitionLine { LineNumber = lineNo, ItemId = item.Id, ItemCodeSnapshot = item.ItemCode, ItemNameSnapshot = item.Name, UomSnapshot = item.Uom, SpecificationSnapshot = item.TechnicalSpecification, RequestedQuantity = request.RequestedQuantity, EstimatedUnitPriceSnapshot = request.EstimatedUnitPrice, EstimatedLineTotal = total, RequiredDate = request.RequiredDate, PreferredWarehouseId = warehouseId, ProjectReference = Norm(request.ProjectReference), MachineReference = Norm(request.MachineReference), ServiceReference = Norm(request.ServiceReference), CreatedBy = pr.CreatedBy });
        }
        pr.ApprovalRoute = await RouteForConfiguredAsync(db, pr.EstimatedTotal, ct);
    }

    private static async Task<string> NextPrNumberAsync(NexaErpDbContext db, string organizationId, DateOnly requestDate, ICurrentUser user, CancellationToken ct)
    {
        var financialYear = FinancialYear(requestDate);
        var sequence = await db.PurchaseNumberSequences.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.FinancialYear == financialYear && x.Prefix == "PR" && x.IsActive, ct);
        if (sequence is null)
        {
            sequence = new PurchaseNumberSequence { OrganizationId = organizationId, FinancialYear = financialYear, Prefix = "PR", LastNumber = 0, CreatedBy = user.LoginId };
            db.PurchaseNumberSequences.Add(sequence);
        }
        sequence.LastNumber++;
        sequence.UpdatedBy = user.LoginId;
        sequence.UpdatedAt = DateTimeOffset.UtcNow;
        return $"{sequence.Prefix}-{financialYear}-{sequence.LastNumber:000001}";
    }

    public sealed record ApprovalRouteDefinition(string RouteCode, decimal MinimumAmount, decimal? MaximumAmount, string? ApproverRoleCode, string ApproverResolutionType, bool IsActive = true);
    private static readonly ApprovalRouteDefinition[] DefaultApprovalRoutes =
    [
        new(PurchaseRequisitionApprovalRoutes.Manager, 0m, 50000m, null, PurchaseApproverResolutionTypes.DepartmentMapping),
        new(PurchaseRequisitionApprovalRoutes.TechnicalDirector, 50000.01m, 500000m, PurchaseRequisitionApprovalRoutes.ApproverRoleCode(PurchaseRequisitionApprovalRoutes.TechnicalDirector), PurchaseApproverResolutionTypes.FixedRole),
        new(PurchaseRequisitionApprovalRoutes.ManagingDirector, 500000.01m, null, PurchaseRequisitionApprovalRoutes.ApproverRoleCode(PurchaseRequisitionApprovalRoutes.ManagingDirector), PurchaseApproverResolutionTypes.FixedRole)
    ];

    public sealed record ApprovalWorkflowStepDefinition(string RouteCode, decimal MinimumAmount, decimal? MaximumAmount, int StepNumber, string ApproverResolutionType, string? ApproverEmployeeCode, string? ApproverRoleCode, bool IsActive = true);
    private static readonly ApprovalWorkflowStepDefinition[] DefaultApprovalWorkflowSteps =
    [
        new("MANAGER_ONLY", 0m, 50000m, 1, PurchaseApproverResolutionTypes.DepartmentMapping, null, null),
        new("MANAGER_MD", 50000.01m, 500000m, 1, PurchaseApproverResolutionTypes.DepartmentMapping, null, null),
        new("MANAGER_MD", 50000.01m, 500000m, 2, PurchaseApproverResolutionTypes.FixedRole, "SESS-002", PurchaseRequisitionApprovalRoutes.ManagingDirector),
        new("MANAGER_MD_TD", 500000.01m, null, 1, PurchaseApproverResolutionTypes.DepartmentMapping, null, null),
        new("MANAGER_MD_TD", 500000.01m, null, 2, PurchaseApproverResolutionTypes.FixedRole, "SESS-002", PurchaseRequisitionApprovalRoutes.ManagingDirector),
        new("MANAGER_MD_TD", 500000.01m, null, 3, PurchaseApproverResolutionTypes.FixedRole, "SESS-001", PurchaseRequisitionApprovalRoutes.TechnicalDirector)
    ];

    public static IReadOnlyList<ApprovalWorkflowStepDefinition> ApprovalWorkflowFor(decimal total) => ApprovalWorkflowFor(total, DefaultApprovalWorkflowSteps);
    public static IReadOnlyList<ApprovalWorkflowStepDefinition> ApprovalWorkflowFor(decimal total, IEnumerable<ApprovalWorkflowStepDefinition> steps)
    {
        if (total < 0) throw new InvalidOperationException("PR approval amount cannot be negative.");
        var matchedRouteCodes = steps
            .Where(x => x.IsActive)
            .Where(x => total >= x.MinimumAmount && (!x.MaximumAmount.HasValue || total <= x.MaximumAmount.Value))
            .Select(x => x.RouteCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (matchedRouteCodes.Count != 1) throw new InvalidOperationException($"No single active PR approval workflow is configured for amount {total}.");
        return steps.Where(x => x.IsActive && string.Equals(x.RouteCode, matchedRouteCodes[0], StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.StepNumber).ToList();
    }
    public static string RouteFor(decimal total) => RouteFor(total, DefaultApprovalRoutes);
    public static string RouteFor(decimal total, IEnumerable<ApprovalRouteDefinition> routes)
    {
        if (total < 0) throw new InvalidOperationException("PR approval amount cannot be negative.");
        var matches = routes
            .Where(x => x.IsActive)
            .Where(x => total >= x.MinimumAmount && (!x.MaximumAmount.HasValue || total <= x.MaximumAmount.Value))
            .Select(x => PurchaseRequisitionApprovalRoutes.Normalize(x.RouteCode))
            .ToList();
        return matches.Count == 1 ? matches[0] : throw new InvalidOperationException($"No single active PR approval route is configured for amount {total}.");
    }
    private static async Task<string> RouteForConfiguredAsync(NexaErpDbContext db, decimal total, CancellationToken ct)
    {
        var routes = await db.PurchaseApprovalRouteSettings.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new ApprovalRouteDefinition(x.RouteCode, x.MinimumAmount, x.MaximumAmount, x.ApproverRoleCode, x.ApproverResolutionType, x.IsActive))
            .ToListAsync(ct);
        return RouteFor(total, routes.Count == 0 ? DefaultApprovalRoutes : routes);
    }
    public sealed record DepartmentApproverResolution(bool Success, Guid? EmployeeId, string? EmployeeCode, string? EmployeeName, string Message);
    public static bool IsManagerLevelRoleCode(string? roleCode)
    {
        var normalized = roleCode?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized.EndsWith("_MANAGER", StringComparison.OrdinalIgnoreCase) || normalized == "MANAGER" || normalized == "DEPARTMENT_MANAGER";
    }

    private static async Task<DepartmentApproverResolution> ResolveDepartmentManagerApproverAsync(PurchaseRequisition pr, NexaErpDbContext db, ICurrentUser actor, CancellationToken ct)
    {
        if (pr.RequestingDepartmentId is null) return new(false, null, null, null, "Requesting department is required for MANAGER approval routing.");
        if (pr.RequesterEmployeeId is null) return new(false, null, null, null, "Requester employee is required for MANAGER approval routing.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var mappings = await db.DepartmentApprovalMappings.AsNoTracking()
            .Where(x => x.DepartmentId == pr.RequestingDepartmentId.Value && x.ApprovalRouteCode == PurchaseRequisitionApprovalRoutes.Manager && x.IsActive)
            .Where(x => x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today))
            .OrderByDescending(x => x.EffectiveFrom)
            .Take(2)
            .ToListAsync(ct);
        if (mappings.Count != 1) return new(false, null, null, null, mappings.Count == 0 ? "No active department MANAGER approval mapping is configured." : "More than one active department MANAGER approval mapping is configured.");
        var mapping = mappings[0];
        var actorLogin = actor.LoginId.Trim();
        var candidateIds = new[] { mapping.PrimaryApproverEmployeeId, mapping.AlternateApproverEmployeeId }.Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        var candidates = await db.Employees.AsNoTracking()
            .Where(x => candidateIds.Contains(x.Id))
            .Select(x => new { x.Id, x.EmployeeCode, x.EmployeeName, x.Status, x.LoginEnabled })
            .ToListAsync(ct);
        var actorEmployee = candidates.SingleOrDefault(x => string.Equals(x.EmployeeCode, actorLogin, StringComparison.OrdinalIgnoreCase));
        if (actorEmployee is null) return new(false, mapping.PrimaryApproverEmployeeId, null, null, "Actor is not the configured department manager or active delegate.");
        if (!string.Equals(actorEmployee.Status, "Active", StringComparison.OrdinalIgnoreCase) || !actorEmployee.LoginEnabled) return new(false, actorEmployee.Id, actorEmployee.EmployeeCode, actorEmployee.EmployeeName, "Configured department manager is inactive or login disabled.");
        if (actorEmployee.Id == pr.RequesterEmployeeId || string.Equals(actorEmployee.EmployeeCode, pr.CreatedBy, StringComparison.OrdinalIgnoreCase) || string.Equals(actorEmployee.EmployeeCode, pr.SubmittedBy, StringComparison.OrdinalIgnoreCase)) return new(false, actorEmployee.Id, actorEmployee.EmployeeCode, actorEmployee.EmployeeName, "Requester/submitter cannot approve their own MANAGER route PR.");
        var hasManagerRole = await db.EmployeeRoleAssignments.AsNoTracking()
            .Include(x => x.Role)
            .AnyAsync(x => x.EmployeeId == actorEmployee.Id && x.ApprovalStatus == "SeedApproved" && x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today) && x.Role != null && IsManagerLevelRoleCode(x.Role.Code), ct);
        if (!hasManagerRole) return new(false, actorEmployee.Id, actorEmployee.EmployeeCode, actorEmployee.EmployeeName, "Configured department manager lacks active manager-level approval permission.");
        return new(true, actorEmployee.Id, actorEmployee.EmployeeCode, actorEmployee.EmployeeName, "Department manager approval resolved.");
    }
    public static decimal AvailableQuantity(decimal onHand, decimal activeReserved) => Math.Max(onHand - activeReserved, 0);
    public static decimal ReserveQuantity(decimal requested, decimal available) => Math.Min(Math.Max(requested, 0), Math.Max(available, 0));
    public static decimal ShortageQuantity(decimal requested, decimal reserved) => Math.Max(requested - reserved, 0);
    public static string LocationKey(Guid warehouseId, Guid? rackBinId) => rackBinId.HasValue ? $"W:{warehouseId:N}:B:{rackBinId.Value:N}" : $"W:{warehouseId:N}:B:NONE";
    public static decimal ReconciledShortage(decimal requested, decimal totalActiveReserved) => Math.Max(requested - totalActiveReserved, 0);
    public static bool IsRouteLimitValid(decimal min, decimal? max) => min >= 0 && (!max.HasValue || max.Value >= min);
    public static bool HasOverlappingRoute(decimal min, decimal? max, IEnumerable<(decimal Min, decimal? Max)> activeRanges)
    {
        var right = max ?? decimal.MaxValue;
        return activeRanges.Any(r => min <= (r.Max ?? decimal.MaxValue) && r.Min <= right);
    }
    private static string FinancialYear(DateOnly date) => date.Month >= 4 ? $"{date.Year}-{(date.Year + 1) % 100:00}" : $"{date.Year - 1}-{date.Year % 100:00}";
    private static bool CanApproveRoute(string role, string route) { var normalizedRole = role.Trim().ToUpperInvariant(); return PurchaseRequisitionApprovalRoutes.Normalize(route) switch { PurchaseRequisitionApprovalRoutes.Manager => normalizedRole.Contains("MANAGER", StringComparison.OrdinalIgnoreCase) || normalizedRole is "ADMIN" or "MD" or "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR", PurchaseRequisitionApprovalRoutes.TechnicalDirector => normalizedRole is "TECHNICAL_DIRECTOR" or "ADMIN", PurchaseRequisitionApprovalRoutes.ManagingDirector => normalizedRole is "MANAGING_DIRECTOR" or "MD" or "ADMIN", _ => false }; }
    private static IQueryable<PurchaseRequisition> Scope(IQueryable<PurchaseRequisition> query, ICurrentUser user) => string.IsNullOrWhiteSpace(user.OrganizationId) ? query : query.Where(x => x.OrganizationId == user.OrganizationId);
    private static IQueryable<PurchaseRequisition> IncludeDetail(IQueryable<PurchaseRequisition> query) => query.Include(x => x.RequestingDepartment).Include(x => x.RequesterEmployee).Include(x => x.DeliveryWarehouse).Include(x => x.Lines).ThenInclude(x => x.Item);
    private static IQueryable<PurchaseRequisition> Sort(IQueryable<PurchaseRequisition> q, string? sortBy, string? dir) => (sortBy?.Trim().ToLowerInvariant(), dir?.Trim().ToLowerInvariant()) switch { ("requiredby", "desc") => q.OrderByDescending(x => x.RequiredByDate), ("requiredby", _) => q.OrderBy(x => x.RequiredByDate), ("status", "desc") => q.OrderByDescending(x => x.Status), ("status", _) => q.OrderBy(x => x.Status), ("total", "desc") => q.OrderByDescending(x => x.EstimatedTotal), ("total", _) => q.OrderBy(x => x.EstimatedTotal), ("prnumber", "desc") => q.OrderByDescending(x => x.PrNumber), _ => q.OrderBy(x => x.PrNumber) };
    private static string NormalizePr(string value) => value.Trim().ToUpperInvariant();
    private static string? Norm(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Correlation(string action) => $"REV868_{action}_{Guid.NewGuid():N}";
    private static string Idempotency(PurchaseRequisitionActionRequest request, string action) => string.IsNullOrWhiteSpace(request.IdempotencyKey) ? Correlation(action) : request.IdempotencyKey.Trim();
    private static async Task<PurchaseRequisition> Reload(Guid id, NexaErpDbContext db, CancellationToken ct) => await IncludeDetail(db.PurchaseRequisitions.AsNoTracking()).SingleAsync(x => x.Id == id, ct);
    private static async Task<decimal> OnHand(NexaErpDbContext db, Guid itemId, Guid warehouseId, Guid? rackBinId, CancellationToken ct) => await db.StockMovements.Where(x => x.ItemId == itemId && x.WarehouseId == warehouseId && x.RackBinId == rackBinId).SumAsync(x => x.QuantityIn - x.QuantityOut, ct);
    private static async Task<decimal> ActiveReserved(NexaErpDbContext db, Guid itemId, Guid warehouseId, Guid? rackBinId, CancellationToken ct) => await db.StockReservations.Where(x => x.ItemId == itemId && x.WarehouseId == warehouseId && x.RackBinId == rackBinId && x.Status == "Active").SumAsync(x => x.ReservedQuantity, ct);
    private static async Task<decimal> ActiveReservedForLine(NexaErpDbContext db, Guid lineId, CancellationToken ct) => await db.StockReservations.Where(x => x.PurchaseRequisitionLineId == lineId && x.Status == "Active").SumAsync(x => x.ReservedQuantity, ct);
    private static void SetStatus(NexaErpDbContext db, PurchaseRequisition pr, string next, string reason, ICurrentUser user, string correlation) { var previous = pr.Status; pr.Status = next; pr.UpdatedBy = user.LoginId; pr.UpdatedAt = DateTimeOffset.UtcNow; AddStatus(db, pr, previous, next, reason, user, correlation); }
    private static void AddStatus(NexaErpDbContext db, PurchaseRequisition pr, string? previous, string next, string reason, ICurrentUser user, string correlation) => db.PurchaseRequisitionStatusHistories.Add(new PurchaseRequisitionStatusHistory { PurchaseRequisitionId = pr.Id, PrNumber = pr.PrNumber, PreviousStatus = previous, NewStatus = next, Reason = reason.Trim(), ActorLoginId = user.LoginId, ActorRoleCode = user.RoleCode, CorrelationId = correlation, CreatedBy = user.LoginId });
    private static void AddApproval(NexaErpDbContext db, PurchaseRequisition pr, string action, string from, string to, string remarks, ICurrentUser user, string correlation) => db.PurchaseRequisitionApprovalHistories.Add(new PurchaseRequisitionApprovalHistory { PurchaseRequisitionId = pr.Id, PrNumber = pr.PrNumber, Action = action, FromStatus = from, ToStatus = to, ApprovalRoute = pr.ApprovalRoute, Remarks = remarks.Trim(), ActorLoginId = user.LoginId, ActorRoleCode = user.RoleCode, CorrelationId = correlation, CreatedBy = user.LoginId });
    private static async Task<IResult> History(NexaErpDbContext db, string prNumber, CancellationToken ct) => Results.Ok(await db.PurchaseRequisitionStatusHistories.AsNoTracking().Where(x => x.PrNumber == NormalizePr(prNumber)).OrderByDescending(x => x.CreatedAt).Select(x => new PurchaseRequisitionHistorySummary(x.Id, "Status", x.PreviousStatus, x.NewStatus, x.Reason, x.ActorLoginId, x.ActorRoleCode, x.CreatedAt, x.CorrelationId)).ToListAsync(ct));
    private static async Task<IResult> ApprovalHistory(NexaErpDbContext db, string prNumber, CancellationToken ct) => Results.Ok(await db.PurchaseRequisitionApprovalHistories.AsNoTracking().Where(x => x.PrNumber == NormalizePr(prNumber)).OrderByDescending(x => x.CreatedAt).Select(x => new PurchaseRequisitionHistorySummary(x.Id, x.Action, x.FromStatus, x.ToStatus, x.Remarks, x.ActorLoginId, x.ActorRoleCode, x.CreatedAt, x.CorrelationId)).ToListAsync(ct));
    private static PurchaseRequisitionSummary ToSummary(PurchaseRequisition x) => new(x.Id, x.PrNumber, x.OrganizationId, x.RequestingDepartment?.Name ?? string.Empty, x.RequesterEmployee?.EmployeeCode ?? string.Empty, x.RequestDate, x.RequiredByDate, x.Priority, x.Status, x.ApprovalRoute, x.EstimatedTotal, x.Version);
    private static PurchaseRequisitionDetail ToDetail(PurchaseRequisition x) => new(x.Id, x.PrNumber, x.OrganizationId, x.RequestingDepartment?.Name ?? string.Empty, x.RequesterEmployee?.EmployeeCode ?? string.Empty, x.RequestDate, x.RequiredByDate, x.Priority, x.PurposeJustification, x.DeliveryWarehouse?.WarehouseCode ?? string.Empty, x.CostCentre, x.ProjectReference, x.ServiceReference, x.WorkOrderReference, x.CustomerReference, x.Status, x.ApprovalRoute, x.EstimatedTotal, x.Version, x.Lines.OrderBy(l => l.LineNumber).Select(l => new PurchaseRequisitionLineSummary(l.Id, l.LineNumber, l.ItemCodeSnapshot, l.ItemNameSnapshot, l.UomSnapshot, l.RequestedQuantity, l.EstimatedUnitPriceSnapshot, l.EstimatedLineTotal, l.OnHandSnapshot, l.ActiveReservedSnapshot, l.AvailableSnapshot, l.ReservedQuantity, l.ShortageQuantity, l.ProcurementHandoffQuantity, l.LineStatus)).ToList());
}
