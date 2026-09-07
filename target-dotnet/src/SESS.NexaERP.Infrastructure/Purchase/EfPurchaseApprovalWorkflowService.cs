using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Purchase;

public sealed class EfPurchaseApprovalWorkflowService(NexaErpDbContext db) : IPurchaseApprovalWorkflowService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<PurchaseApprovalWorkflowSnapshot> SelectAndSnapshotAsync(
        string organizationId, Guid requestingDepartmentId, decimal amount, CancellationToken ct)
    {
        if (amount < 0) throw new InvalidOperationException("Approval value cannot be negative.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var companyId = await db.Companies.AsNoTracking()
            .Where(x => x.Code == organizationId && x.IsActive).Select(x => x.Id).SingleAsync(ct);
        var effectiveAt = DateTimeOffset.UtcNow;
        var configuredRoutes = await db.PurchaseApprovalRouteSettings.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.MinimumAmount <= amount &&
                (!x.MaximumAmount.HasValue || amount <= x.MaximumAmount))
            .ToListAsync(ct);
        var selectedRoute = SelectRouteSetting(companyId, effectiveAt, amount, configuredRoutes);
        var candidates = await db.PurchaseApprovalWorkflowSteps.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.RouteCode == selectedRoute.RouteCode && x.IsActive && x.EffectiveFrom <= today &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo >= today) && x.MinimumAmount <= amount &&
                (!x.MaximumAmount.HasValue || amount <= x.MaximumAmount))
            .OrderBy(x => x.StepNumber).ToListAsync(ct);
        var routes = candidates.Select(x => x.RouteCode).Distinct(StringComparer.Ordinal).ToArray();
        if (routes.Length != 1 || candidates.Count == 0 || candidates.Select(x => x.StepNumber).SequenceEqual(Enumerable.Range(1, candidates.Count)) is false)
            throw new Rev869BConflictException($"Purchase approval configuration is missing or incomplete for company {companyId} at effective time {effectiveAt:O}: purchase_approval_workflow_steps for route {selectedRoute.RouteCode} and amount {amount:0.00} must form one complete, contiguous workflow.");

        var steps = new List<PurchaseApprovalWorkflowStepSnapshot>(candidates.Count);
        foreach (var configured in candidates)
        {
            if (configured.ApproverResolutionType == PurchaseApproverResolutionTypes.DepartmentMapping)
            {
                var mappings = await db.DepartmentApprovalMappings.AsNoTracking()
                    .Where(x => x.CompanyId == companyId && x.DepartmentId == requestingDepartmentId &&
                        x.ApprovalRouteCode == PurchaseRequisitionApprovalRoutes.Manager && x.IsActive &&
                        x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo >= today))
                    .Take(2).ToListAsync(ct);
                if (mappings.Count != 1) throw new InvalidOperationException("A single effective department approval mapping is required.");
                var mapping = mappings[0];
                var employeeCode = await db.Employees.AsNoTracking().Where(x => x.Id == mapping.PrimaryApproverEmployeeId && x.Status.ToUpper() == "ACTIVE")
                    .Select(x => x.EmployeeCode).SingleAsync(ct);
                steps.Add(new(configured.StepNumber, configured.ApproverResolutionType, mapping.PrimaryApproverEmployeeId,
                    employeeCode, mapping.ApproverRoleCode));
            }
            else if (configured.ApproverResolutionType == PurchaseApproverResolutionTypes.ConfiguredRole)
            {
                if (string.IsNullOrWhiteSpace(configured.ApproverEmployeeCode) || string.IsNullOrWhiteSpace(configured.ApproverRoleCode))
                    throw new InvalidOperationException("Configured-role approval requires both employee and role.");
                var employee = await db.Employees.AsNoTracking()
                    .SingleAsync(x => x.EmployeeCode == configured.ApproverEmployeeCode && x.Status.ToUpper() == "ACTIVE", ct);
                steps.Add(new(configured.StepNumber, configured.ApproverResolutionType, employee.Id,
                    employee.EmployeeCode, configured.ApproverRoleCode));
            }
            else throw new InvalidOperationException("Unsupported approval resolution type.");
        }

        var unsigned = new PurchaseApprovalWorkflowSnapshot(string.Empty, organizationId, routes[0], amount, today, steps);
        var canonical = JsonSerializer.Serialize(unsigned, Json);
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        return unsigned with { Identity = identity };
    }

    public static PurchaseApprovalRouteSetting SelectRouteSetting(
        Guid companyId,
        DateTimeOffset effectiveAt,
        decimal amount,
        IReadOnlyCollection<PurchaseApprovalRouteSetting> matches)
    {
        if (amount < 0) throw new Rev869BConflictException("Approval value cannot be negative.");
        if (matches.Count == 0)
            throw new Rev869BConflictException($"Purchase approval configuration is missing for company {companyId} at effective time {effectiveAt:O}: no active purchase_approval_route_settings row matches amount {amount:0.00}.");
        if (matches.Count > 1)
        {
            var conflicts = string.Join(", ", matches
                .OrderBy(x => x.RouteCode, StringComparer.Ordinal)
                .ThenBy(x => x.Id)
                .Select(x => $"{x.RouteCode}[{x.Id}]({x.MinimumAmount:0.00}..{(x.MaximumAmount.HasValue ? x.MaximumAmount.Value.ToString("0.00") : "unbounded")})"));
            throw new Rev869BConflictException($"Purchase approval configuration is ambiguous for company {companyId} at effective time {effectiveAt:O}: conflicting purchase_approval_route_settings rows for amount {amount:0.00}: {conflicts}.");
        }
        return matches.Single();
    }
    public PurchaseApprovalWorkflowSnapshot ReadSnapshot(string snapshotJson)
    {
        PurchaseApprovalWorkflowSnapshot snapshot;
        try { snapshot = JsonSerializer.Deserialize<PurchaseApprovalWorkflowSnapshot>(snapshotJson, Json) ?? throw new JsonException(); }
        catch (JsonException) { throw new InvalidOperationException("Approval workflow snapshot is malformed."); }
        var unsigned = snapshot with { Identity = string.Empty };
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(unsigned, Json)))).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(identity), Encoding.ASCII.GetBytes(snapshot.Identity)))
            throw new InvalidOperationException("Approval workflow snapshot identity is invalid.");
        return snapshot;
    }

    public PurchaseApprovalDecision AuthorizeNextStep(string snapshotJson, int approvalCycle, int completedStepCount,
        Guid creatorEmployeeId, Guid actorEmployeeId, IReadOnlyList<string> actorRoleCodes, Guid? priorStepEmployeeId = null)
    {
        var snapshot = ReadSnapshot(snapshotJson);
        var requestedStep = checked(completedStepCount + 1);
        if (approvalCycle < 1 || requestedStep < 1 || requestedStep > snapshot.Steps.Count)
            throw new UnauthorizedAccessException("Requested approval step is out of order or already completed.");
        var step = snapshot.Steps.SingleOrDefault(x => x.StepNumber == requestedStep)
            ?? throw new UnauthorizedAccessException("Requested approval step is absent from the immutable workflow snapshot.");
        if (actorEmployeeId == creatorEmployeeId) throw new UnauthorizedAccessException("Creator self-approval is prohibited.");
        if (requestedStep == 2 && priorStepEmployeeId == actorEmployeeId)
            throw new UnauthorizedAccessException("Level 2 approver must differ from level 1.");
        if (step.EmployeeId != actorEmployeeId)
            throw new UnauthorizedAccessException($"This approval step is awaiting {step.EmployeeCode} ({step.RoleCode}).");
        if (!actorRoleCodes.Any(x => string.Equals(x.Trim(), step.RoleCode, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException($"This approval step is awaiting {step.EmployeeCode} with role {step.RoleCode}.");
        var completed = requestedStep;
        return new(approvalCycle, requestedStep, snapshot.Steps.Count, completed,
            completed == snapshot.Steps.Count, step.EmployeeId, step.RoleCode, snapshot.RouteCode, snapshot.Identity);
    }

    public string Serialize(PurchaseApprovalWorkflowSnapshot snapshot) => JsonSerializer.Serialize(snapshot, Json);
}

public sealed class PurchaseOperationalRoleResolver : IPurchaseOperationalRoleResolver
{
    private static readonly IReadOnlyDictionary<string, string> Roles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["CreateComparison"] = "PURCHASE_MANAGER", ["RecommendComparison"] = "PURCHASE_MANAGER",
        ["ResubmitComparison"] = "PURCHASE_MANAGER", ["CreatePO"] = "PURCHASE_MANAGER",
        ["SubmitPO"] = "PURCHASE_MANAGER", ["IssuePO"] = "PURCHASE_MANAGER",
        ["AmendPO"] = "PURCHASE_MANAGER", ["ReviseRejectedPO"] = "PURCHASE_MANAGER",
        ["CreateRFQ"] = "PURCHASE_EXECUTIVE", ["InviteVendor"] = "PURCHASE_EXECUTIVE",
        ["SubmitQuotation"] = "PURCHASE_EXECUTIVE",
        ["MaterialFollowUp"] = "STORES_EXECUTIVE"
    };

    public string Resolve(string operation, IReadOnlyList<string> effectiveRoleCodes)
    {
        if (operation.StartsWith("Approve", StringComparison.OrdinalIgnoreCase) ||
            operation.StartsWith("Reject", StringComparison.OrdinalIgnoreCase) ||
            operation.StartsWith("RequestRevision", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Approval commands obtain their role from the workflow step.");
        if (operation.Equals("CancelPO", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var role in new[] { "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR" })
                if (effectiveRoleCodes.Any(x => string.Equals(x.Trim(), role, StringComparison.OrdinalIgnoreCase))) return role;
            throw new UnauthorizedAccessException("TECHNICAL_DIRECTOR or MANAGING_DIRECTOR is required for CancelPO.");
        }
        if (operation.Equals("TechnicalVerification", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var role in new[] { "TECHNICAL_SUPPORT_MANAGER", "TECHNICAL_ENGINEER", "TECHNICAL_DIRECTOR" })
                if (effectiveRoleCodes.Any(x => string.Equals(x.Trim(), role, StringComparison.OrdinalIgnoreCase))) return role;
            throw new UnauthorizedAccessException("TECHNICAL_SUPPORT_MANAGER, TECHNICAL_ENGINEER or TECHNICAL_DIRECTOR is required for TechnicalVerification.");
        }
        if (!Roles.TryGetValue(operation, out var required))
            throw new UnauthorizedAccessException("No deterministic operational role is configured for this command.");
        if (!effectiveRoleCodes.Any(x => string.Equals(x.Trim(), required, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException($"{required} is required for {operation}.");
        return required;
    }
}
