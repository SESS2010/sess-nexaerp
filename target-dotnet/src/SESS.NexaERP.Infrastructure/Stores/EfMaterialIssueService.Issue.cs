using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfMaterialIssueService
{
    public async Task<MaterialIssueView> IssueAsync(
        Guid requestId, CreateMaterialIssue command, CancellationToken ct)
    {
        _ = user.RequireRole("issue", "STORES_ASSISTANT", "STORES_EXECUTIVE", "STORES_MANAGER");
        var key = Required(command.IdempotencyKey, "IdempotencyKey");
        var hash = Fingerprint(new { requestId, command });
        if (command.IssuedAt == default) throw new StoresValidationException("IssuedAt is required.");
        if (command.Scans is null || command.Scans.Count == 0)
            throw new StoresValidationException("At least one scanner-confirmed issue line is required.");
        if (command.Scans.Any(x => x.MaterialIssueRequestLineId == Guid.Empty ||
            string.IsNullOrWhiteSpace(x.ScanCode) || x.Quantity <= 0))
            throw new StoresValidationException("Every scan requires MIR line, ScanCode and positive Quantity.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({'I' + company.Code + key},0))", ct);
        var replay = await IssueQuery(true).SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.IdempotencyKey == key, ct);
        if (replay is not null)
        {
            if (replay.RequestFingerprint != hash)
                throw new StoresConflictException("Idempotency key was reused with different Issue content.");
            await tx.CommitAsync(ct);
            return IssueView(replay, true);
        }

        var request = await RequestQuery(true).SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.Id == requestId, ct)
            ?? throw new KeyNotFoundException("MIR was not found.");
        if (request.Status is not ("APPROVED" or "PARTIALLY_FULFILLED"))
            throw new StoresConflictException("Material may be issued only against an Approved MIR.");
        var on = DateOnly.FromDateTime(command.IssuedAt.UtcDateTime);
        var recipient = await db.Employees.SingleOrDefaultAsync(x => x.Id == command.IssuedToEmployeeId && x.Status == "Active", ct)
            ?? throw new StoresValidationException("IssuedToEmployeeId is not an active employee.");
        if (!await db.EmployeeRoleAssignments.AsNoTracking().AnyAsync(x =>
            x.CompanyId == company.Id && x.EmployeeId == recipient.Id &&
            x.EffectiveFrom <= on && (!x.EffectiveTo.HasValue || x.EffectiveTo >= on), ct))
            throw new StoresValidationException("Issue recipient has no effective assignment in the selected company.");
        if (JobSituations.Contains(request.Situation) && request.JobOrderId is null)
            throw new StoresConflictException("This customer-facing MIR has no Job Order.");
        if (request.Situation == "CONSUMABLE_OFFICE" && request.JobOrderId is not null)
            throw new StoresConflictException("Consumable/office custody must not be attached to a Job Order.");

        var excessLineIds = request.Lines.Where(l => l.ExcessBaseQuantitySnapshot > 0)
            .Select(l => l.Id).ToArray();
        var decisions = await db.MaterialIssueExcessDecisions.AsNoTracking().Where(d =>
            d.CompanyId == company.Id && excessLineIds.Contains(d.MaterialIssueRequestLineId))
            .Select(d => new { d.MaterialIssueRequestLineId, d.Decision }).ToListAsync(ct);
        if (excessLineIds.Any(id => decisions.SingleOrDefault(d =>
                d.MaterialIssueRequestLineId == id)?.Decision != "APPROVED"))
            throw new StoresConflictException("Customer-facing excess requires an APPROVED Technical Director decision before issue.");

        var destinationAccount = await EmployeeCustodyAccountAsync(company.Id, recipient, ct);
        var issue = new MaterialIssue
        {
            CompanyId = company.Id, IssueNumber = await NextNumberAsync(company.Id, "MI", ct),
            MaterialIssueRequestId = request.Id, JobOrderId = request.JobOrderId,
            IssuedToEmployeeId = recipient.Id, IssuedAt = command.IssuedAt,
            ReturnDueAt = command.IssuedAt.AddDays(1), Status = "ISSUED",
            IdempotencyKey = key, RequestFingerprint = hash, IssuedByEmployeeId = Actor(),
            ActorRoleCode = user.RoleCode, ResolvedRoleAssignmentId = user.ResolvedRoleAssignmentId!.Value,
            ResolvedRoleAssignmentType = user.ResolvedRoleAssignmentType!, CreatedBy = user.LoginId
        };
        var allocations = new List<IssuePostingLeg>();
        var lineNumber = 0;
        foreach (var group in command.Scans.GroupBy(x => x.MaterialIssueRequestLineId))
        {
            var requestLine = request.Lines.SingleOrDefault(x => x.Id == group.Key)
                ?? throw new StoresValidationException("A scan references a line outside this MIR.");
            var previouslyIssued = await db.MaterialIssueLines.AsNoTracking()
                .Where(x => x.CompanyId == company.Id && x.MaterialIssueRequestLineId == requestLine.Id)
                .SumAsync(x => (decimal?)x.QuantityBase, ct) ?? 0;
            var requestedNow = group.Sum(x => x.Quantity);
            if (previouslyIssued + requestedNow > requestLine.RequestedBaseQuantity)
                throw new StoresConflictException($"Issue quantity exceeds MIR line {requestLine.LineNumber}.");
            foreach (var scan in group)
            {
                var normalized = NormalizeScan(scan.ScanCode);
                if (scan.InventorySerialId.HasValue)
                {
                    var serial = await db.InventorySerials.AsNoTracking().SingleOrDefaultAsync(x =>
                        x.CompanyId == company.Id && x.Id == scan.InventorySerialId &&
                        x.ItemId == requestLine.ItemId, ct)
                        ?? throw new StoresValidationException("Scanned serial does not belong to the MIR company/item.");
                    if (NormalizeScan(serial.StoredSerialNumber) != normalized || scan.Quantity != 1)
                        throw new StoresValidationException("Serialized issue requires the exact serial scan and Quantity 1.");
                }
                else
                {
                    var itemCode = await db.Items.AsNoTracking().Where(x => x.Id == requestLine.ItemId)
                        .Select(x => x.ItemCode).SingleAsync(ct);
                    if (NormalizeScan(itemCode) != normalized)
                        throw new StoresValidationException("Non-serialized scan must match the MIR item code.");
                }
                var available = await AvailableLayersAsync(company.Id, requestLine.ItemId,
                    scan.InventorySerialId, ct);
                var remaining = scan.Quantity;
                foreach (var layer in available)
                {
                    if (remaining <= 0) break;
                    var take = Math.Min(remaining, layer.Balance);
                    if (take <= 0) continue;
                    var assignment = new InventoryCustodyAssignment
                    {
                        CompanyId = company.Id, CustodyAccountId = destinationAccount.Id,
                        WarehouseId = null, RackBinId = null, AssignedQuantity = take,
                        EffectiveFrom = command.IssuedAt, IsCurrent = true,
                        AssignmentReason = $"Material issue {issue.IssueNumber}; Job Order {request.JobOrderId?.ToString() ?? "not applicable"}.",
                        CreatedBy = user.LoginId
                    };
                    db.InventoryCustodyAssignments.Add(assignment);
                    var issueLine = new MaterialIssueLine
                    {
                        CompanyId = company.Id, MaterialIssueId = issue.Id,
                        MaterialIssueRequestLineId = requestLine.Id, LineNumber = ++lineNumber,
                        ItemId = requestLine.ItemId, QuantityBase = take,
                        OwnershipAccountId = layer.OwnershipAccountId,
                        FromCustodyAssignmentId = layer.CustodyAssignmentId,
                        ToCustodyAssignmentId = assignment.Id,
                        InventoryProvenanceLayerId = layer.InventoryProvenanceLayerId,
                        CustodyCaseLineId = layer.CustodyCaseLineId,
                        InventoryLotId = layer.InventoryLotId, InventorySerialId = layer.InventorySerialId,
                        OriginGoodsReceiptLineId = layer.OriginGoodsReceiptLineId,
                        GoodsReceiptLineLotAllocationId = layer.GoodsReceiptLineLotAllocationId,
                        QcInspectionLotDispositionId = layer.QcInspectionLotDispositionId,
                        WarehouseConditionLocationId = layer.WarehouseConditionLocationId,
                        CreatedBy = user.LoginId
                    };
                    issue.Lines.Add(issueLine);
                    allocations.Add(new(issueLine.Id, requestLine.Id, issueLine.LineNumber,
                        requestLine.ItemId, take, layer.WarehouseConditionLocationId,
                        layer.OwnershipAccountId, layer.CustodyAssignmentId, assignment.Id,
                        layer.InventoryProvenanceLayerId, layer.CustodyCaseLineId,
                        layer.InventoryLotId, layer.InventorySerialId, layer.OriginGoodsReceiptLineId,
                        layer.GoodsReceiptLineLotAllocationId, layer.QcInspectionLotDispositionId));
                    remaining -= take;
                }
                if (remaining > 0)
                    throw new StoresConflictException($"Insufficient AVAILABLE Stores custody for MIR line {requestLine.LineNumber}.");
            }
        }
        db.MaterialIssues.Add(issue);
        var allIssued = true;
        foreach (var line in request.Lines)
        {
            var prior = await db.MaterialIssueLines.AsNoTracking().Where(x =>
                x.CompanyId == company.Id && x.MaterialIssueRequestLineId == line.Id)
                .SumAsync(x => (decimal?)x.QuantityBase, ct) ?? 0;
            var current = issue.Lines.Where(x => x.MaterialIssueRequestLineId == line.Id).Sum(x => x.QuantityBase);
            if (prior + current < line.RequestedBaseQuantity) allIssued = false;
        }
        var from = request.Status;
        request.Status = allIssued ? "FULFILLED" : "PARTIALLY_FULFILLED";
        request.Version = checked(request.Version + 1);
        request.UpdatedAt = DateTimeOffset.UtcNow; request.UpdatedBy = user.LoginId;
        History(request, issue, "ISSUE", from, request.Status,
            "Custody transferred to engineer; no consumption recorded.", key);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
            Organization(), "MaterialIssue.Issue", key, new { requestId, command });
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(
            db, user, Organization(), envelope, ct, user.RoleCode)
            ?? throw new InvalidOperationException("Material issue produced no immutable operation slot.");
        await db.SaveChangesAsync(ct);
        var posting = await PostIssueAsync(company.Id, issue.Id, key, hash, allocations, ct);
        await ConsumeFifoAsync(company.Id, issue.Id, ct);
        issue.StockPostingBatchId = posting.BatchId;
        await audit.WriteAsync("Stores", "MaterialIssue.Issue", nameof(MaterialIssue),
            issue.Id.ToString(), null, new { issue.IssueNumber, posting.BatchId }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        return IssueView(issue, posting.Replayed);
    }

    private async Task<InventoryCustodyAccount> EmployeeCustodyAccountAsync(
        Guid companyId, SESS.NexaERP.Domain.Employees.Employee employee, CancellationToken ct)
    {
        var code = "EMPLOYEE:" + employee.EmployeeCode;
        var account = await db.InventoryCustodyAccounts.Include(x => x.AccountHolder)
            .SingleOrDefaultAsync(x => x.CompanyId == companyId && x.AccountCode == code, ct);
        if (account is not null) return account;
        var holder = await db.InventoryAccountHolders.SingleOrDefaultAsync(x =>
            x.CompanyId == companyId && x.HolderCode == code, ct);
        if (holder is null)
        {
            holder = new() { CompanyId = companyId, HolderType = "EMPLOYEE",
                EmployeeId = employee.Id, HolderCode = code,
                HolderNameSnapshot = employee.EmployeeName, CreatedBy = user.LoginId };
            db.InventoryAccountHolders.Add(holder);
        }
        account = new() { CompanyId = companyId, AccountHolderId = holder.Id,
            AccountCode = code, CustodyType = "EMPLOYEE", CreatedBy = user.LoginId };
        db.InventoryCustodyAccounts.Add(account);
        return account;
    }

    private async Task<List<AvailableLayer>> AvailableLayersAsync(
        Guid companyId, Guid itemId, Guid? serialId, CancellationToken ct)
    {
        var grouped = await db.StockMovements.AsNoTracking().Where(x =>
                x.CompanyId == companyId && x.ItemId == itemId &&
                x.ConditionCode == "AVAILABLE" && x.CustodyAssignment != null &&
                x.CustodyAssignment.CustodyAccount != null &&
                x.CustodyAssignment.CustodyAccount.CustodyType == "WAREHOUSE" &&
                (!serialId.HasValue || x.InventorySerialId == serialId))
            .GroupBy(x => new { x.WarehouseConditionLocationId, x.OwnershipAccountId,
                x.CustodyAssignmentId, x.InventoryProvenanceLayerId, x.CustodyCaseLineId,
                x.InventoryLotId, x.InventorySerialId, x.OriginGoodsReceiptLineId,
                x.GoodsReceiptLineLotAllocationId, x.QcInspectionLotDispositionId })
            .Select(g => new { g.Key.WarehouseConditionLocationId, g.Key.OwnershipAccountId,
                g.Key.CustodyAssignmentId, g.Key.InventoryProvenanceLayerId,
                g.Key.CustodyCaseLineId, g.Key.InventoryLotId, g.Key.InventorySerialId,
                g.Key.OriginGoodsReceiptLineId, g.Key.GoodsReceiptLineLotAllocationId,
                g.Key.QcInspectionLotDispositionId, Balance = g.Sum(x => x.QuantityIn - x.QuantityOut) })
            .Where(x => x.Balance > 0).ToListAsync(ct);
        var rows = grouped.Select(x => new AvailableLayer(x.WarehouseConditionLocationId!.Value,
            x.OwnershipAccountId, x.CustodyAssignmentId, x.InventoryProvenanceLayerId,
            x.CustodyCaseLineId, x.InventoryLotId, x.InventorySerialId,
            x.OriginGoodsReceiptLineId, x.GoodsReceiptLineLotAllocationId,
            x.QcInspectionLotDispositionId, x.Balance)).ToList();
        var provenanceDates = await db.InventoryProvenanceLayers.AsNoTracking()
            .Where(x => rows.Select(r => r.InventoryProvenanceLayerId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.CreatedAt, ct);
        return rows.OrderBy(x => provenanceDates[x.InventoryProvenanceLayerId])
            .ThenBy(x => x.InventoryProvenanceLayerId).ToList();
    }

    private async Task<PostingResult> PostIssueAsync(Guid companyId, Guid issueId,
        string key, string hash, IReadOnlyList<IssuePostingLeg> legs, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """SELECT "StockPostingBatchId","Replayed" FROM advance.post_material_issue_custody(@company,@issue,@key,@fingerprint,@correlation,@actor,@login)""";
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("issue", issueId);
        command.Parameters.AddWithValue("key", "POST:" + key);
        command.Parameters.AddWithValue("fingerprint", hash);
        command.Parameters.AddWithValue("correlation", key);
        command.Parameters.AddWithValue("actor", Actor());
        command.Parameters.AddWithValue("login", user.LoginId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new StoresConflictException("Controlled material issue returned no result.");
        return new(reader.GetGuid(0), reader.GetBoolean(1));
    }

    private async Task ConsumeFifoAsync(Guid companyId, Guid issueId, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT advance.consume_fifo_for_issue(@company,@issue,@actor,@role,@assignment,@type,@login)";
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("issue", issueId);
        command.Parameters.AddWithValue("actor", Actor());
        command.Parameters.AddWithValue("role", user.RoleCode);
        command.Parameters.AddWithValue("assignment", user.ResolvedRoleAssignmentId!.Value);
        command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!);
        command.Parameters.AddWithValue("login", user.LoginId);
        await command.ExecuteNonQueryAsync(ct);
    }
    private static string NormalizeScan(string value) =>
        string.Concat(Required(value, "ScanCode").Where(char.IsLetterOrDigit)).ToUpperInvariant();
    private sealed record AvailableLayer(Guid WarehouseConditionLocationId, Guid OwnershipAccountId,
        Guid CustodyAssignmentId, Guid InventoryProvenanceLayerId, Guid? CustodyCaseLineId,
        Guid? InventoryLotId, Guid? InventorySerialId, Guid? OriginGoodsReceiptLineId,
        Guid? GoodsReceiptLineLotAllocationId, Guid? QcInspectionLotDispositionId, decimal Balance);
    private sealed record IssuePostingLeg(Guid MaterialIssueLineId, Guid MaterialIssueRequestLineId,
        int LineNumber, Guid ItemId, decimal Quantity, Guid WarehouseConditionLocationId,
        Guid OwnershipAccountId, Guid FromCustodyAssignmentId, Guid ToCustodyAssignmentId,
        Guid InventoryProvenanceLayerId, Guid? CustodyCaseLineId, Guid? InventoryLotId,
        Guid? InventorySerialId, Guid? OriginGoodsReceiptLineId,
        Guid? GoodsReceiptLineLotAllocationId, Guid? QcInspectionLotDispositionId);
    private sealed record PostingResult(Guid BatchId, bool Replayed);
}
