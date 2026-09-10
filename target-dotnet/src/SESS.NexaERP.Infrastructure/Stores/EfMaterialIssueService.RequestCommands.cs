using System.Data;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfMaterialIssueService
{
    public async Task<MaterialIssueRequestView> CreateRequestAsync(
        CreateMaterialIssueRequest command, CancellationToken ct)
    {
        _ = RequireAny("create");
        var key = Required(command.IdempotencyKey, "IdempotencyKey");
        var hash = Fingerprint(command);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({'M' + company.Code + key},0))", ct);
        var replay = await RequestQuery().SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.IdempotencyKey == key, ct);
        if (replay is not null)
        {
            if (replay.RequestFingerprint != hash)
                throw new StoresConflictException("Idempotency key was reused with different MIR content.");
            await tx.CommitAsync(ct);
            return await RequestViewAsync(replay, ct);
        }
        var request = new MaterialIssueRequest
        {
            CompanyId = company.Id, RequestNumber = await NextNumberAsync(company.Id, "MIR", ct),
            RequestedByEmployeeId = Actor(), Status = "DRAFT", IdempotencyKey = key,
            RequestFingerprint = hash, CreatedBy = user.LoginId
        };
        await ApplyAsync(request, command.Purpose, command.Situation, command.DestinationType,
            command.JobOrderId, command.CustomerId, command.VendorId, command.DestinationDepartmentId,
            command.DestinationName, command.RequestingDepartmentId, command.RequiredDate,
            command.Lines, ct);
        db.MaterialIssueRequests.Add(request);
        History(request, null, "CREATE", null, "DRAFT", "MIR created.", key);
        await CommitAsync("MaterialIssueRequest.Create", key, command,
            nameof(MaterialIssueRequest), request.Id, new { request.RequestNumber }, ct);
        await tx.CommitAsync(ct);
        return await RequestViewAsync(request, ct);
    }

    public async Task<MaterialIssueRequestView> UpdateRequestAsync(
        Guid id, UpdateMaterialIssueRequest command, CancellationToken ct)
    {
        _ = RequireAny("update");
        var key = Required(command.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var request = await RequestQuery(true).SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.Id == id, ct)
            ?? throw new KeyNotFoundException("MIR was not found.");
        if (request.Status != "DRAFT") throw new StoresConflictException("Only a Draft MIR can be edited.");
        if (request.Version != command.Version) throw new DbUpdateConcurrencyException("MIR Version is stale.");
        if (request.RequestedByEmployeeId != Actor())
            throw new UnauthorizedAccessException("Only the MIR creator may edit its Draft.");
        db.MaterialIssueRequestLines.RemoveRange(request.Lines);
        request.Lines.Clear();
        await ApplyAsync(request, command.Purpose, command.Situation, command.DestinationType,
            command.JobOrderId, command.CustomerId, command.VendorId, command.DestinationDepartmentId,
            command.DestinationName, command.RequestingDepartmentId, command.RequiredDate,
            command.Lines, ct);
        db.MaterialIssueRequestLines.AddRange(request.Lines);
        request.RequestFingerprint = Fingerprint(command);
        request.Version = checked(request.Version + 1);
        request.UpdatedAt = DateTimeOffset.UtcNow; request.UpdatedBy = user.LoginId;
        History(request, null, "UPDATE", "DRAFT", "DRAFT", "MIR Draft updated.", key);
        await CommitAsync("MaterialIssueRequest.Update", key, command,
            nameof(MaterialIssueRequest), request.Id, new { request.RequestNumber }, ct);
        await tx.CommitAsync(ct);
        return await RequestViewAsync(request, ct);
    }

    public Task<MaterialIssueRequestView> SubmitAsync(Guid id, MaterialIssueTransitionRequest request, CancellationToken ct)
    {
        _ = RequireAny("submit");
        return TransitionAsync(id, request, "DRAFT", "SUBMITTED", "SUBMIT", "MaterialIssueRequest.Submit", false, ct);
    }

    public Task<MaterialIssueRequestView> ApproveAsync(Guid id, MaterialIssueTransitionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("approve", "STORES_MANAGER", "PRODUCTION_MANAGER");
        return TransitionAsync(id, request, "SUBMITTED", "APPROVED", "APPROVE", "MaterialIssueRequest.Approve", true, ct);
    }

    public Task<MaterialIssueRequestView> RejectAsync(Guid id, MaterialIssueTransitionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("reject", "STORES_MANAGER", "PRODUCTION_MANAGER");
        return TransitionAsync(id, request, "SUBMITTED", "REJECTED", "REJECT", "MaterialIssueRequest.Reject", true, ct);
    }

    public Task<MaterialIssueRequestView> CancelAsync(Guid id, MaterialIssueTransitionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("cancel", "STORES_MANAGER");
        return TransitionAsync(id, request, null, "CANCELLED", "CANCEL", "MaterialIssueRequest.Cancel", false, ct);
    }

    public async Task<MaterialIssueRequestView> DecideExcessAsync(
        Guid lineId, MaterialIssueExcessDecisionRequest command, CancellationToken ct)
    {
        _ = user.RequireRole("approve", "TECHNICAL_DIRECTOR");
        var key = Required(command.IdempotencyKey, "IdempotencyKey");
        var decision = Code(command.Decision, "Decision");
        if (decision is not ("APPROVED" or "REJECTED"))
            throw new StoresValidationException("Decision must be APPROVED or REJECTED.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var line = await db.MaterialIssueRequestLines.Include(x => x.MaterialIssueRequest)
            .SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == lineId, ct)
            ?? throw new KeyNotFoundException("MIR line was not found.");
        if (line.ExcessBaseQuantitySnapshot <= 0)
            throw new StoresConflictException("This MIR line has no customer-facing excess to decide.");
        if (line.MaterialIssueRequest!.RequestedByEmployeeId == Actor())
            throw new StoresConflictException("Nobody may decide excess on their own MIR.");
        var replay = await db.MaterialIssueExcessDecisions.SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.MaterialIssueRequestLineId == line.Id, ct);
        if (replay is not null)
        {
            if (replay.IdempotencyKey != key || replay.Decision != decision ||
                replay.Reason != Required(command.Reason, "Reason"))
                throw new StoresConflictException("The excess decision already exists.");
            await tx.CommitAsync(ct);
            return await RequestViewAsync(line.MaterialIssueRequest!, ct);
        }
        var row = new MaterialIssueExcessDecision
        {
            CompanyId = company.Id, MaterialIssueRequestLineId = line.Id,
            Decision = decision, Reason = Required(command.Reason, "Reason"),
            DecidedByEmployeeId = Actor(), ActorRoleCode = user.RoleCode,
            ResolvedRoleAssignmentId = user.ResolvedRoleAssignmentId!.Value,
            ResolvedRoleAssignmentType = user.ResolvedRoleAssignmentType!,
            IdempotencyKey = key
        };
        db.MaterialIssueExcessDecisions.Add(row);
        History(line.MaterialIssueRequest, null, "EXCESS_" + decision,
            line.MaterialIssueRequest!.Status, line.MaterialIssueRequest.Status, row.Reason, key);
        await CommitAsync("MaterialIssueRequest.DecideExcess", key, command,
            nameof(MaterialIssueRequestLine), line.Id, new { decision }, ct);
        await tx.CommitAsync(ct);
        return await RequestViewAsync(line.MaterialIssueRequest, ct);
    }

    private async Task<MaterialIssueRequestView> TransitionAsync(Guid id,
        MaterialIssueTransitionRequest command, string? expected, string next,
        string action, string operation, bool independent, CancellationToken ct)
    {
        var key = Required(command.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var request = await RequestQuery(true).SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.Id == id, ct)
            ?? throw new KeyNotFoundException("MIR was not found.");
        if (request.Version != command.Version) throw new DbUpdateConcurrencyException("MIR Version is stale.");
        if (expected is not null && request.Status != expected)
            throw new StoresConflictException($"MIR must be {expected} for {action.ToLowerInvariant()}.");
        if (expected is null && request.Status is not ("DRAFT" or "SUBMITTED"))
            throw new StoresConflictException("Only a Draft or Submitted MIR can be cancelled.");
        if (independent && request.RequestedByEmployeeId == Actor())
            throw new StoresConflictException("Nobody may approve or reject their own MIR.");
        if (next == "SUBMITTED" && request.Lines.Count == 0)
            throw new StoresConflictException("An empty MIR cannot be submitted.");
        var from = request.Status; request.Status = next;
        request.Version = checked(request.Version + 1);
        request.UpdatedAt = DateTimeOffset.UtcNow; request.UpdatedBy = user.LoginId;
        if (next == "APPROVED")
        {
            request.ApprovedAt = DateTimeOffset.UtcNow; request.ApprovedByEmployeeId = Actor();
        }
        History(request, null, action, from, next, Required(command.Reason, "Reason"), key);
        await CommitAsync(operation, key, command, nameof(MaterialIssueRequest), request.Id,
            new { request.RequestNumber, request.Status }, ct);
        await tx.CommitAsync(ct);
        return await RequestViewAsync(request, ct);
    }

    private async Task ApplyAsync(MaterialIssueRequest request, string purpose, string situation,
        string destinationType, Guid? jobOrderId, Guid? customerId, Guid? vendorId,
        Guid? destinationDepartmentId, string destinationName, Guid requestingDepartmentId,
        DateOnly requiredDate, IReadOnlyList<MaterialIssueRequestLineInput> inputs, CancellationToken ct)
    {
        request.Purpose = Code(purpose, "Purpose");
        request.Situation = Code(situation, "Situation");
        request.DestinationType = Code(destinationType, "DestinationType");
        var jobRequired = JobSituations.Contains(request.Situation);
        var spareSale = request.Situation == SpareSaleSituation;
        if (!jobRequired && !spareSale && request.Situation != "CONSUMABLE_OFFICE")
            throw new StoresValidationException("Situation must be CHAMBER_MANUFACTURE, SERVICE_CUSTOMER_PO, SITE_PROJECT_PO, SPARE_SALE or CONSUMABLE_OFFICE.");
        if (jobRequired && (jobOrderId is null || request.DestinationType != "JOB_ORDER"))
            throw new StoresValidationException("This MIR situation requires a JobOrderId and JOB_ORDER destination.");
        if (spareSale && (jobOrderId is not null || request.DestinationType != "CUSTOMER" || customerId is null))
            throw new StoresValidationException("SPARE_SALE has no Job Order and requires a CUSTOMER destination and CustomerId.");
        if (!jobRequired && !spareSale && (jobOrderId is not null || request.DestinationType is not ("DEPARTMENT" or "OTHER")))
            throw new StoresValidationException("CONSUMABLE_OFFICE has no Job Order and must target DEPARTMENT or OTHER.");
        if (requiredDate == default) throw new StoresValidationException("RequiredDate is required.");
        if (inputs is null || inputs.Count == 0) throw new StoresValidationException("At least one MIR line is required.");
        if (inputs.Any(x => x.ItemId == Guid.Empty || x.UomId == Guid.Empty || x.Quantity <= 0))
            throw new StoresValidationException("Every MIR line requires ItemId, UomId and a positive Quantity.");
        if (inputs.Select(x => new { x.ItemId, x.CustomerPurchaseOrderLineId }).Distinct().Count() != inputs.Count)
            throw new StoresValidationException("Duplicate MIR item/customer-PO line combinations are not allowed.");
        var company = await CompanyAsync(ct);
        if (!await db.Departments.AnyAsync(x => x.Id == requestingDepartmentId && x.IsActive, ct))
            throw new StoresValidationException("RequestingDepartmentId is invalid.");
        Guid? jobCustomerPoLineId = null;
        if (jobRequired)
        {
            jobCustomerPoLineId = await db.JobOrders.AsNoTracking()
                .Where(x => x.CompanyId == company.Id && x.Id == jobOrderId && x.Status == "OPEN")
                .Select(x => (Guid?)x.CustomerPurchaseOrderLineId).SingleOrDefaultAsync(ct);
            if (!jobCustomerPoLineId.HasValue)
                throw new StoresValidationException("JobOrderId is not an Accounts-confirmed Open Job Order in the selected company.");
        }
        request.JobOrderId = jobOrderId; request.CustomerId = customerId; request.VendorId = vendorId;
        request.DestinationDepartmentId = destinationDepartmentId;
        request.DestinationNameSnapshot = Required(destinationName, "DestinationName");
        request.RequestingDepartmentId = requestingDepartmentId; request.RequiredDate = requiredDate;
        var lineNo = 0;
        foreach (var input in inputs)
        {
            var item = await db.Items.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.ItemId && x.IsActive, ct)
                ?? throw new StoresValidationException("MIR item is inactive or does not exist.");
            var uom = await db.Uoms.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.UomId && x.IsActive, ct)
                ?? throw new StoresValidationException("MIR UOM is inactive or does not exist.");
            var baseQuantity = await ToBaseAsync(input.Quantity, input.UomId, item.BaseUomId, requiredDate, ct);
            var estimated = jobRequired
                ? await EstimatedQuantityAsync(company.Id, jobOrderId!.Value, item.Id, requiredDate, ct) : 0m;
            var production = jobRequired
                ? await ProductionQuantityAsync(company.Id, jobOrderId!.Value, item.Id, requiredDate, ct) : 0m;
            Guid? customerPoLineId = input.CustomerPurchaseOrderLineId;
            decimal cpo = 0m;
            if (jobRequired)
            {
                if (customerPoLineId.HasValue && customerPoLineId != jobCustomerPoLineId)
                    throw new StoresValidationException("A job-backed MIR line may only reference the Customer PO line pinned by its Job Order.");
                customerPoLineId = jobCustomerPoLineId;
            }
            else if (spareSale)
            {
                if (!customerPoLineId.HasValue)
                    throw new StoresValidationException("CustomerPurchaseOrderLineId is required for every SPARE_SALE line.");
                cpo = await CustomerPoQuantityAsync(company.Id, customerPoLineId.Value,
                    item.Id, item.BaseUomId, requiredDate, customerId!.Value, ct);
            }
            else if (customerPoLineId.HasValue)
                throw new StoresValidationException("CONSUMABLE_OFFICE lines cannot reference a Customer PO line.");

            // A machine, project or service CPO line identifies what was sold; its components
            // are governed by the frozen Estimated BOM. Only a spare sale is component-to-component.
            var limit = jobRequired ? estimated : spareSale ? cpo : 0m;
            var excess = jobRequired || spareSale ? Math.Max(0, baseQuantity - limit) : 0m;
            request.Lines.Add(new MaterialIssueRequestLine
            {
                CompanyId = company.Id, MaterialIssueRequestId = request.Id, LineNumber = ++lineNo,
                ItemId = item.Id, UomId = uom.Id, CustomerPurchaseOrderLineId = customerPoLineId,
                ItemCodeSnapshot = item.ItemCode, ItemNameSnapshot = item.Name, UomSnapshot = uom.Code,
                RequestedQuantity = input.Quantity, RequestedBaseQuantity = baseQuantity,
                EstimatedBomBaseQuantitySnapshot = estimated,
                ProductionBomBaseQuantitySnapshot = production,
                CustomerPoBaseQuantitySnapshot = cpo, ExcessBaseQuantitySnapshot = excess,
                ExcessClassification = excess > 0 ? "CUSTOMER_FACING_EXCESS" : "NONE",
                Remarks = input.Remarks?.Trim(), CreatedBy = user.LoginId
            });
        }
    }

    private async Task<decimal> ToBaseAsync(decimal quantity, Guid from, Guid to, DateOnly on, CancellationToken ct)
    {
        if (from == to) return quantity;
        var direct = await db.UomConversions.AsNoTracking().Where(x => x.FromUomId == from && x.ToUomId == to &&
            x.ApprovalStatus == MasterApprovalStatuses.Approved && x.IsActive && x.EffectiveFrom <= on &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo >= on)).Select(x => (decimal?)x.ConversionFactor).SingleOrDefaultAsync(ct);
        if (direct.HasValue) return decimal.Round(quantity * direct.Value, 6);
        var reverse = await db.UomConversions.AsNoTracking().Where(x => x.FromUomId == to && x.ToUomId == from &&
            x.ApprovalStatus == MasterApprovalStatuses.Approved && x.IsActive && x.EffectiveFrom <= on &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo >= on)).Select(x => (decimal?)x.ConversionFactor).SingleOrDefaultAsync(ct);
        if (reverse.HasValue) return decimal.Round(quantity / reverse.Value, 6);
        throw new StoresConflictException("No approved effective UOM conversion reaches the item's base UOM.");
    }

    private async Task<decimal> EstimatedQuantityAsync(Guid companyId, Guid jobId, Guid itemId, DateOnly on, CancellationToken ct)
    {
        var rows = await db.EstimatedBoms.AsNoTracking().Where(x => x.CompanyId == companyId && x.JobOrderId == jobId && x.CommercialBaselineRevisionId != null)
            .SelectMany(x => x.Revisions.Where(r => r.Id == x.CommercialBaselineRevisionId))
            .SelectMany(r => r.Lines.Where(l => l.ItemId == itemId)).Select(l => new { l.Quantity, l.UomId }).ToListAsync(ct);
        var item = await db.Items.AsNoTracking().SingleAsync(x => x.Id == itemId, ct);
        decimal total = 0; foreach (var row in rows) total += await ToBaseAsync(row.Quantity, row.UomId, item.BaseUomId, on, ct);
        return total;
    }

    private async Task<decimal> ProductionQuantityAsync(Guid companyId, Guid jobId, Guid itemId, DateOnly on, CancellationToken ct)
    {
        var pinned = await db.JobOrders.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == jobId)
            .Select(x => x.PinnedProductionBomRevisionId).SingleAsync(ct);
        if (!pinned.HasValue) throw new StoresConflictException("The Job Order must pin an approved Production BOM revision before MIR creation.");
        var rows = await db.ProductionBomLines.AsNoTracking().Where(x => x.CompanyId == companyId &&
            x.ProductionBomRevisionId == pinned && x.ItemId == itemId).Select(x => new { x.Quantity, x.UomId }).ToListAsync(ct);
        var item = await db.Items.AsNoTracking().SingleAsync(x => x.Id == itemId, ct);
        decimal total = 0; foreach (var row in rows) total += await ToBaseAsync(row.Quantity, row.UomId, item.BaseUomId, on, ct);
        return total;
    }

    private async Task<decimal> CustomerPoQuantityAsync(Guid companyId, Guid lineId, Guid itemId,
        Guid baseUomId, DateOnly on, Guid customerId, CancellationToken ct)
    {
        var line = await db.CustomerPurchaseOrderLines.AsNoTracking().Include(x => x.CustomerPurchaseOrder)
            .SingleOrDefaultAsync(x => x.Id == lineId && x.ItemId == itemId &&
                x.RevisionNumber == x.CustomerPurchaseOrder!.CurrentRevisionNumber &&
                x.CustomerPurchaseOrder.CompanyId == companyId &&
                x.CustomerPurchaseOrder.CustomerId == customerId &&
                (x.CustomerPurchaseOrder.SalesType == "Spares" ||
                 x.CustomerPurchaseOrder.SalesType == "Spares & Service"), ct)
            ?? throw new StoresValidationException("CustomerPurchaseOrderLineId must be the current matching spare line for the selected company and customer.");
        if (!line.Quantity.HasValue || line.Quantity.Value <= 0)
            throw new StoresValidationException("The selected Customer PO spare line must have a positive quantity.");
        return await ToBaseAsync(line.Quantity.Value, line.UomId, baseUomId, on, ct);
    }
}
