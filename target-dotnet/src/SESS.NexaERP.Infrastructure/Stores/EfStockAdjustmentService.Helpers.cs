using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfStockAdjustmentService
{
    /// <summary>The retained approval facts, serialized into ApprovalSnapshotJson at submission.</summary>
    private sealed record SnapshotRecord(int RevisionNumber, decimal AbsoluteValue,
        IReadOnlyList<string> RequiredRoleCodes, IReadOnlyList<Guid> ExcludedEmployeeIds)
    {
        public static SnapshotRecord From(StockAdjustmentApprovalSnapshot snapshot, int revision) =>
            new(revision, snapshot.AbsoluteValue, snapshot.RequiredRoleCodes, snapshot.ExcludedEmployeeIds);
    }

    private sealed record ValidatedLine(Guid ItemId, Guid WarehouseConditionLocationId, decimal QuantityChange,
        decimal? UnitValue, string? LotNumber, string? SerialNumber, string? Remarks);

    private static List<ValidatedLine> ValidateLines(IReadOnlyList<StockAdjustmentLineInput>? inputs, string reasonKind)
    {
        if (inputs is null || inputs.Count == 0) throw new StoresValidationException("At least one adjustment line is required.");
        if (inputs.Count > 500) throw new StoresValidationException("An adjustment carries at most 500 lines.");
        var lines = new List<ValidatedLine>(inputs.Count);
        foreach (var input in inputs)
        {
            if (input.ItemId == Guid.Empty || input.WarehouseConditionLocationId == Guid.Empty)
                throw new StoresValidationException("Every line requires ItemId and WarehouseConditionLocationId.");
            if (input.QuantityChange == 0 || decimal.Round(input.QuantityChange, 6) != input.QuantityChange)
                throw new StoresValidationException("QuantityChange must be a non-zero quantity with at most six decimals.");
            if (input.QuantityChange > 0 && (input.UnitValue is null || input.UnitValue < 0 || decimal.Round(input.UnitValue.Value, 6) != input.UnitValue))
                throw new StoresValidationException("An addition requires its ex-tax UnitValue (zero or more, at most six decimals).");
            if (input.QuantityChange < 0 && input.UnitValue is not null)
                throw new StoresValidationException("A removal is valued from the FIFO layers it consumes; do not state a UnitValue.");
            if (reasonKind == StockAdjustmentReasonKinds.DamageLoss && input.QuantityChange > 0)
                throw new StoresValidationException("A damage/loss write-off removes stock only.");
            var serial = Optional(input.SerialNumber, "SerialNumber", 300);
            if (serial is not null && Math.Abs(input.QuantityChange) != 1)
                throw new StoresValidationException("A serialized line changes exactly one unit.");
            lines.Add(new(input.ItemId, input.WarehouseConditionLocationId, input.QuantityChange, input.UnitValue,
                Optional(input.LotNumber, "LotNumber", 160), serial, Optional(input.Remarks, "Remarks", 500)));
        }
        if (lines.Where(x => x.SerialNumber is not null).GroupBy(x => x.SerialNumber!.ToUpperInvariant()).Any(g => g.Count() > 1))
            throw new StoresValidationException("A serial appears on at most one line.");
        return lines;
    }

    private async Task AddLinesAsync(Guid companyId, StockAdjustment adjustment, int revision, List<ValidatedLine> lines, CancellationToken ct)
    {
        var number = 0;
        foreach (var line in lines)
        {
            var location = await db.WarehouseConditionLocations.AsNoTracking().SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.Id == line.WarehouseConditionLocationId, ct);
            if (location is null || location.WarehouseId != adjustment.WarehouseId || location.ConditionCode != "AVAILABLE"
                || !location.IsEffective(adjustment.EffectiveDate))
                throw new StoresValidationException($"Line {number + 1}: the location must be an effective AVAILABLE location of the adjustment warehouse.");
            if (!await db.Items.AsNoTracking().AnyAsync(x => x.Id == line.ItemId && x.IsActive, ct))
                throw new StoresValidationException($"Line {number + 1}: ItemId is not an active item.");
            var accepted = line.QuantityChange > 0
                ? decimal.Round(line.QuantityChange * line.UnitValue!.Value, 6)
                : await CarryingValueAsync(companyId, line.ItemId, -line.QuantityChange, ct)
                  ?? throw new StoresConflictException($"Line {number + 1}: the FIFO layers hold less than the quantity removed.");
            db.StockAdjustmentLines.Add(new()
            {
                CompanyId = companyId, StockAdjustmentId = adjustment.Id, RevisionNumber = revision, LineNumber = ++number,
                ItemId = line.ItemId, WarehouseConditionLocationId = line.WarehouseConditionLocationId,
                LotNumber = line.LotNumber, SerialNumber = line.SerialNumber, QuantityChange = line.QuantityChange,
                UnitValue = line.UnitValue, AcceptedLineValue = accepted, Remarks = line.Remarks, CreatedBy = user.LoginId
            });
        }
    }

    /// <summary>The FIFO value a removal would consume today, oldest layer first; null when the layers hold less.</summary>
    private async Task<decimal?> CarryingValueAsync(Guid companyId, Guid itemId, decimal quantity, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = new NpgsqlCommand("SELECT advance.fifo_carrying_value_preview(@company,@item,@quantity)", connection,
            (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("item", itemId);
        command.Parameters.AddWithValue("quantity", quantity);
        return await command.ExecuteScalarAsync(ct) is decimal value ? value : null;
    }

    /// <summary>
    /// Re-derives the approval snapshot from the retained current-revision lines, the recorder and the
    /// counters. With revalueRemovals the removals are valued from today's FIFO layers instead of the
    /// accepted line value retained at recording.
    /// </summary>
    private async Task<StockAdjustmentApprovalSnapshot> CaptureAsync(Guid companyId, StockAdjustment adjustment,
        CancellationToken ct, bool revalueRemovals = false, int depth = 0)
    {
        if (depth > 8) throw new StoresConflictException("The reversal chain is too deep to evaluate.");
        var lines = await db.StockAdjustmentLines.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.StockAdjustmentId == adjustment.Id && x.RevisionNumber == adjustment.CurrentRevisionNumber)
            .OrderBy(x => x.LineNumber).ToListAsync(ct);
        var changes = new List<StockAdjustmentValuedChange>(lines.Count);
        foreach (var line in lines)
        {
            var value = line.AcceptedLineValue;
            if (revalueRemovals && line.QuantityChange < 0)
                value = await CarryingValueAsync(companyId, line.ItemId, -line.QuantityChange, ct)
                    ?? throw new StoresConflictException($"Line {line.LineNumber}: the FIFO layers hold less than the quantity removed.");
            changes.Add(new(line.QuantityChange, value, false));
        }
        var counters = JsonSerializer.Deserialize<Guid[]>(adjustment.CounterEmployeeIdsJson) ?? [];
        StockAdjustmentApprovalSnapshot? original = null;
        if (adjustment.ReversesStockAdjustmentId is { } originalId)
        {
            var reversed = await db.StockAdjustments.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == originalId, ct)
                ?? throw new StoresConflictException("The reversed adjustment was not found.");
            original = await CaptureAsync(companyId, reversed, ct, false, depth + 1);
        }
        return StockAdjustmentApprovalSnapshot.Capture(changes, adjustment.RecordedByEmployeeId, counters,
            adjustment.ReasonKind == StockAdjustmentReasonKinds.DamageLoss, original, adjustment.DaysBackdated > 7);
    }

    private async Task RequireReversibleAsync(Guid companyId, Guid originalId, List<ValidatedLine> lines, CancellationToken ct)
    {
        var original = await db.StockAdjustments.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == originalId, ct)
            ?? throw new StoresValidationException("ReversesStockAdjustmentId is not an adjustment of the selected company.");
        if (original.Status != StockAdjustmentStatuses.Posted)
            throw new StoresConflictException("Only a posted adjustment can be reversed.");
        if (await db.StockAdjustments.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.ReversesStockAdjustmentId == originalId
                && x.Status != StockAdjustmentStatuses.Rejected, ct))
            throw new StoresConflictException("That adjustment already has a reversal.");
        var originalLines = await db.StockAdjustmentLines.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.StockAdjustmentId == originalId && x.RevisionNumber == original.CurrentRevisionNumber)
            .OrderBy(x => x.LineNumber).ToListAsync(ct);
        if (originalLines.Count != lines.Count || originalLines.Zip(lines).Any(pair =>
                pair.First.ItemId != pair.Second.ItemId || pair.First.WarehouseConditionLocationId != pair.Second.WarehouseConditionLocationId
                || pair.First.QuantityChange != -pair.Second.QuantityChange
                || !string.Equals(pair.First.SerialNumber, pair.Second.SerialNumber, StringComparison.OrdinalIgnoreCase)))
            throw new StoresValidationException("A reversal mirrors the original lines with the opposite quantity, in order.");
    }

    private async Task<StockAdjustment> TrackedAsync(Guid companyId, Guid id, CancellationToken ct) =>
        await db.StockAdjustments.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct)
        ?? throw new KeyNotFoundException("Stock adjustment was not found.");

    private static void RequireVersion(StockAdjustment adjustment, long version)
    {
        if (adjustment.Version != version)
            throw new DbUpdateConcurrencyException("Stock adjustment changed concurrently. Reload before retrying.");
    }

    private void Touch(StockAdjustment adjustment)
    {
        adjustment.Version = checked(adjustment.Version + 1);
        adjustment.UpdatedAt = DateTimeOffset.UtcNow;
        adjustment.UpdatedBy = user.LoginId;
    }

    private async Task<StockAdjustmentView?> ReplayAsync(Rev869BCommandContextAuthorizer.CommandAttemptHandle attempt,
        IDbContextTransaction tx, CancellationToken ct)
    {
        using var receipt = await Rev869BCommandContextAuthorizer.ReadCommittedReceiptAsync(db, attempt, ct);
        if (receipt is null) return null;
        var retained = receipt.RootElement.GetProperty("Adjustment").Deserialize<StockAdjustmentView>(Json)
            ?? throw new InvalidOperationException("The retained receipt carries no adjustment.");
        await tx.CommitAsync(ct);
        return retained with { Replayed = true };
    }

    private async Task<string> NextNumberAsync(Guid companyId, CancellationToken ct)
    {
        const string prefix = "ADJ";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var year = today.Month >= 4 ? $"{today.Year % 100:00}-{(today.Year + 1) % 100:00}"
            : $"{(today.Year - 1) % 100:00}-{today.Year % 100:00}";
        var organization = Organization();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"NUMBER:{organization}:{year}:{prefix}"},0))", ct);
        var sequence = await db.PurchaseNumberSequences.SingleOrDefaultAsync(x =>
            x.CompanyId == companyId && x.OrganizationId == organization && x.FinancialYear == year && x.Prefix == prefix && x.IsActive, ct);
        if (sequence is null)
        {
            sequence = new() { CompanyId = companyId, OrganizationId = organization, FinancialYear = year, Prefix = prefix, CreatedBy = user.LoginId };
            db.PurchaseNumberSequences.Add(sequence);
        }
        sequence.LastNumber++;
        sequence.UpdatedAt = DateTimeOffset.UtcNow;
        sequence.UpdatedBy = user.LoginId;
        return $"{prefix}-{organization}-{year}-{sequence.LastNumber:000001}";
    }

    private async Task<Guid> PostAsync(Guid companyId, Guid id, string role, string key, string hash, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = new NpgsqlCommand(
            """SELECT "StockPostingBatchId","Replayed" FROM advance.post_stock_adjustment(@company,@adjustment,@actor,@role,@assignment,@type,@login,@key,@hash)""",
            connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("adjustment", id);
        command.Parameters.AddWithValue("actor", Actor());
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("assignment", user.ResolvedRoleAssignmentId!.Value);
        command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!);
        command.Parameters.AddWithValue("login", user.LoginId);
        command.Parameters.AddWithValue("key", "POST:" + key);
        command.Parameters.AddWithValue("hash", hash);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new StoresConflictException("Controlled stock adjustment posting returned no result.");
        return reader.GetGuid(0);
    }

    private async Task<StockAdjustmentView> LoadAsync(Guid companyId, Guid id, bool replayed, CancellationToken ct)
    {
        var x = await db.StockAdjustments.AsNoTracking().SingleAsync(a => a.CompanyId == companyId && a.Id == id, ct);
        var warehouse = await db.Warehouses.AsNoTracking().Where(w => w.Id == x.WarehouseId).Select(w => w.WarehouseCode).SingleAsync(ct);
        var period = await db.FinancialPeriods.AsNoTracking().Where(p => p.Id == x.InventoryPeriodId).Select(p => p.Code).SingleAsync(ct);
        var recorder = await db.Employees.AsNoTracking().Where(e => e.Id == x.RecordedByEmployeeId).Select(e => e.EmployeeCode).SingleAsync(ct);
        var lines = await db.StockAdjustmentLines.AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.StockAdjustmentId == id && l.RevisionNumber == x.CurrentRevisionNumber)
            .OrderBy(l => l.LineNumber)
            .Select(l => new
            {
                Line = l, Item = db.Items.Where(i => i.Id == l.ItemId).Select(i => new { i.ItemCode, i.Name }).Single(),
                Location = db.WarehouseConditionLocations.Where(c => c.Id == l.WarehouseConditionLocationId)
                    .Select(c => new { c.RackBinId, c.RackBin!.BinCode }).Single()
            }).ToListAsync(ct);
        var decisions = await db.StockAdjustmentDecisions.AsNoTracking()
            .Where(d => d.CompanyId == companyId && d.StockAdjustmentId == id)
            .OrderBy(d => d.RevisionNumber).ThenBy(d => d.DecidedAt)
            .Select(d => new { Decision = d, Employee = db.Employees.Where(e => e.Id == d.EmployeeId).Select(e => new { e.EmployeeCode, e.EmployeeName }).Single() })
            .ToListAsync(ct);
        var snapshot = x.ApprovalSnapshotJson is null ? null : JsonSerializer.Deserialize<SnapshotRecord>(x.ApprovalSnapshotJson, Json);
        var required = snapshot?.RequiredRoleCodes ?? [];
        var approved = decisions.Where(d => d.Decision.RevisionNumber == x.CurrentRevisionNumber && d.Decision.Decision == "APPROVE")
            .Select(d => d.Decision.RoleCode).ToArray();
        var counters = JsonSerializer.Deserialize<Guid[]>(x.CounterEmployeeIdsJson) ?? [];
        return new(x.Id, x.AdjustmentNumber, x.WarehouseId, warehouse, x.ReasonKind, x.EffectiveDate, x.InventoryPeriodId, period,
            x.Status, x.CurrentRevisionNumber, x.Remarks, x.RecordedByEmployeeId, recorder, counters, x.BackdateReason, x.BackdateEvidenceId,
            x.DaysBackdated, snapshot?.AbsoluteValue ?? lines.Sum(l => l.Line.AcceptedLineValue), required,
            x.Status is StockAdjustmentStatuses.Submitted ? required.Except(approved, StringComparer.Ordinal).ToArray() : [],
            snapshot?.ExcludedEmployeeIds ?? counters.Append(x.RecordedByEmployeeId).Distinct().Order().ToArray(),
            x.ReversesStockAdjustmentId, x.StockPostingBatchId, x.PostedAt, x.Version, replayed,
            lines.Select(l => new StockAdjustmentLineView(l.Line.Id, l.Line.LineNumber, l.Line.ItemId, l.Item.ItemCode, l.Item.Name,
                l.Line.WarehouseConditionLocationId, l.Location.RackBinId, l.Location.BinCode, l.Line.LotNumber, l.Line.SerialNumber,
                l.Line.QuantityChange, l.Line.UnitValue, l.Line.AcceptedLineValue, l.Line.Remarks)).ToArray(),
            decisions.Select(d => new StockAdjustmentDecisionView(d.Decision.Id, d.Decision.RevisionNumber, d.Decision.Decision,
                d.Decision.EmployeeId, d.Employee.EmployeeCode, d.Employee.EmployeeName, d.Decision.RoleCode, d.Decision.RoleAssignmentId,
                d.Decision.RoleAssignmentType, d.Decision.DecidedAt, d.Decision.Reason)).ToArray());
    }
}
