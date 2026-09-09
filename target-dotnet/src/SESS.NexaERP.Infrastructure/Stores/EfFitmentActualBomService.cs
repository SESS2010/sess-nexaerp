using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfFitmentActualBomService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit)
    : IFitmentActualBomService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] ConfirmRoles =
        ["PRODUCTION_OPERATOR", "PRODUCTION_COORDINATOR", "PRODUCTION_MANAGER", "SERVICE_ENGINEER"];
    private static readonly string[] ReverseRoles = ["PRODUCTION_MANAGER", "SERVICE_MANAGER"];

    public async Task<PagedResponse<ComponentFitmentSummary>> ListAsync(int? page, int? pageSize,
        Guid? jobOrderId, bool? activeOnly, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var number = Math.Max(1, page ?? 1); var size = Math.Clamp(pageSize ?? 50, 1, 200);
        var query = Query().Where(x => x.CompanyId == company.Id);
        if (jobOrderId.HasValue) query = query.Where(x => x.JobOrderId == jobOrderId);
        if (activeOnly == true) query = query.Where(x => !db.ComponentFitmentReversals.Any(r =>
            r.CompanyId == x.CompanyId && r.ComponentFitmentId == x.Id));
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.FittedAt).ThenByDescending(x => x.FitmentNumber)
            .Skip((number - 1) * size).Take(size).ToListAsync(ct);
        return new(total, number, size, rows.Select(x => View(x, false)).ToArray());
    }

    public async Task<ComponentFitmentSummary?> GetAsync(Guid id, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var row = await Query().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id, ct);
        return row is null ? null : View(row, false);
    }

    public async Task<ComponentFitmentSummary> ConfirmAsync(ConfirmComponentFitmentRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("create", ConfirmRoles);
        RequireFull("confirm fitment");
        if (request.JobOrderId == Guid.Empty) throw new StoresValidationException("JobOrderId is required.");
        if (request.MaterialIssueLineId == Guid.Empty) throw new StoresValidationException("MaterialIssueLineId is required.");
        if (request.QuantityBase <= 0) throw new StoresValidationException("QuantityBase must be positive.");
        if (request.FittedAt == default) throw new StoresValidationException("FittedAt is required.");
        var note = Required(request.ConfirmationNote, "ConfirmationNote");
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var fingerprint = Fingerprint(new { request.JobOrderId, request.MaterialIssueLineId,
            request.QuantityBase, request.FittedAt, note, request.ReverifiesFitmentId });
        var correlation = Fingerprint("ComponentFitment.Confirm:" + key);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization(),
            "ComponentFitment.Confirm", key, request);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user,
            Organization(), envelope, "actual_bom_entries", nameof(ComponentFitment),
            request.MaterialIssueLineId, "FITMENT", 0, null, "CONFIRMED", correlation, note, ct);
        (Guid Id, bool Replayed) result;
        try
        {
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = "SELECT \"ComponentFitmentId\",\"Replayed\" FROM advance.confirm_component_fitment(@company,@job,@line,@quantity,@fitted,@note,@reverify,@key,@hash,@correlation,@actor,@role,@assignment,@type,@login)";
            command.Parameters.AddWithValue("company", company.Id);
            command.Parameters.AddWithValue("job", request.JobOrderId);
            command.Parameters.AddWithValue("line", request.MaterialIssueLineId);
            command.Parameters.AddWithValue("quantity", request.QuantityBase);
            command.Parameters.AddWithValue("fitted", request.FittedAt);
            command.Parameters.AddWithValue("note", note);
            command.Parameters.Add("reverify", NpgsqlDbType.Uuid).Value = (object?)request.ReverifiesFitmentId ?? DBNull.Value;
            command.Parameters.AddWithValue("key", key); command.Parameters.AddWithValue("hash", fingerprint);
            command.Parameters.AddWithValue("correlation", correlation); AddActor(command);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) throw new StoresConflictException("Controlled fitment confirmation returned no result.");
            result = (reader.GetGuid(0), reader.GetBoolean(1));
        }
        catch (PostgresException e) { throw Translate(e); }
        if (!result.Replayed) await audit.WriteAsync("Production", "ComponentFitment.Confirm",
            nameof(ComponentFitment), result.Id.ToString(), null,
            new { request.JobOrderId, request.MaterialIssueLineId, request.QuantityBase, request.ReverifiesFitmentId }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        var row = await Query().SingleAsync(x => x.CompanyId == company.Id && x.Id == result.Id, ct);
        return View(row, result.Replayed);
    }

    public async Task<ComponentFitmentSummary> ReverseAsync(Guid id,
        ReverseComponentFitmentRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("reverse", ReverseRoles);
        RequireFull("reverse fitment");
        var reason = Required(request.Reason, "Reason");
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var fingerprint = Fingerprint(new { id, reason });
        var correlation = Fingerprint("ComponentFitment.Reverse:" + key);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization(),
            "ComponentFitment.Reverse", key, new { id, request });
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user,
            Organization(), envelope, "actual_bom_entries", nameof(ComponentFitment), id,
            "REVERSAL", 0, "CONFIRMED", "REVERSED", correlation, reason, ct);
        bool replayed;
        try
        {
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = "SELECT \"Replayed\" FROM advance.reverse_component_fitment(@company,@fitment,@reason,@key,@hash,@correlation,@actor,@role,@assignment,@type,@login)";
            command.Parameters.AddWithValue("company", company.Id); command.Parameters.AddWithValue("fitment", id);
            command.Parameters.AddWithValue("reason", reason); command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("hash", fingerprint); command.Parameters.AddWithValue("correlation", correlation);
            AddActor(command);
            replayed = await command.ExecuteScalarAsync(ct) is bool value ? value
                : throw new StoresConflictException("Controlled fitment reversal returned no result.");
        }
        catch (PostgresException e) { throw Translate(e); }
        if (!replayed) await audit.WriteAsync("Production", "ComponentFitment.Reverse",
            nameof(ComponentFitment), id.ToString(), new { Status = "CONFIRMED" },
            new { Status = "REVERSED", Reason = reason }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        var row = await Query().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id, ct)
            ?? throw new KeyNotFoundException("Component fitment was not found.");
        return View(row, replayed);
    }

    public async Task<ActualBomView?> GetActualBomAsync(Guid jobOrderId, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var bom = await db.ActualBoms.AsNoTracking().SingleOrDefaultAsync(x =>
            x.CompanyId == company.Id && x.JobOrderId == jobOrderId, ct);
        if (bom is null) return null;
        var job = await db.JobOrders.AsNoTracking().Where(x => x.CompanyId == company.Id && x.Id == jobOrderId)
            .Select(x => new { x.JobOrderNumber, x.PinnedProductionBomRevisionId }).SingleAsync(ct);
        if (!job.PinnedProductionBomRevisionId.HasValue)
            throw new StoresConflictException("Actual BOM variance requires the Job Order's pinned Production BOM revision.");
        var entries = await db.ActualBomEntries.AsNoTracking()
            .Where(x => x.CompanyId == company.Id && x.ActualBomId == bom.Id)
            .Include(x => x.Item).Include(x => x.Uom).Include(x => x.InventorySerial)
            .OrderBy(x => x.OccurredAt).ThenBy(x => x.Id).ToListAsync(ct);
        var views = entries.Select(x => new ActualBomEntryView(x.Id, x.EntryKind,
            x.ComponentFitmentId, x.ComponentFitmentReversalId, x.MaterialIssueLineId,
            x.ItemId, x.Item!.ItemCode, x.Item.Name, x.UomId, x.Uom!.Code, x.QuantityBase,
            x.InventoryProvenanceLayerId, x.InventoryLotId, x.InventorySerialId,
            x.InventorySerial?.StoredSerialNumber, x.GoodsReceiptLineId,
            x.GrnNumberSnapshot, x.VendorBillLineId,
            x.VendorBillNumberSnapshot, x.AcceptedMaterialValue,
            x.AllocatedChargeValue, x.TotalAcceptedValue, x.OccurredAt)).ToArray();
        var operational = await OperationalVarianceAsync(company.Id,
            job.PinnedProductionBomRevisionId.Value, views, ct);
        var commercial = await CommercialVarianceAsync(company.Id, jobOrderId, views, ct);
        return new(bom.Id, jobOrderId, job.JobOrderNumber, bom.GeneratedAt,
            views.Sum(x => x.AcceptedMaterialValue), views.Sum(x => x.AllocatedChargeValue),
            views.Sum(x => x.TotalAcceptedValue), views, operational, commercial);
    }

    private async Task<ActualBomBaselineVarianceView> OperationalVarianceAsync(Guid companyId,
        Guid revisionId, IReadOnlyList<ActualBomEntryView> actual, CancellationToken ct)
    {
        var revision = await db.ProductionBomRevisions.AsNoTracking().Include(x => x.Lines)
            .SingleAsync(x => x.CompanyId == companyId && x.Id == revisionId && x.Status == "APPROVED", ct);
        return await VarianceAsync("OPERATIONAL_PRODUCTION_BOM", revision.Id, revision.RevisionNumber,
            revision.ApprovedAt ?? revision.CreatedAt,
            revision.Lines.Select(x => new VarianceBaselineLine(x.ItemId, x.UomId, x.Quantity, x.PlannedUnitValue)), actual, ct);
    }

    private async Task<ActualBomBaselineVarianceView> CommercialVarianceAsync(Guid companyId,
        Guid jobOrderId, IReadOnlyList<ActualBomEntryView> actual, CancellationToken ct)
    {
        var baselineId = await db.EstimatedBoms.AsNoTracking().Where(x => x.CompanyId == companyId &&
            x.JobOrderId == jobOrderId && x.CommercialBaselineRevisionId != null)
            .Select(x => x.CommercialBaselineRevisionId!.Value).SingleAsync(ct);
        var revision = await db.EstimatedBomRevisions.AsNoTracking().Include(x => x.Lines)
            .SingleAsync(x => x.CompanyId == companyId && x.Id == baselineId && x.Status == "APPROVED", ct);
        return await VarianceAsync("COMMERCIAL_ESTIMATED_BOM", revision.Id, revision.RevisionNumber,
            revision.ApprovedAt ?? revision.CreatedAt,
            revision.Lines.Select(x => new VarianceBaselineLine(x.ItemId, x.UomId, x.Quantity, x.EstimatedUnitValue)), actual, ct);
    }

    private async Task<ActualBomBaselineVarianceView> VarianceAsync(string type, Guid revisionId,
        int revisionNumber, DateTimeOffset effectiveAt, IEnumerable<VarianceBaselineLine> baselineLines,
        IReadOnlyList<ActualBomEntryView> actualLines, CancellationToken ct)
    {
        var baseline = new Dictionary<Guid, (decimal Quantity, decimal Value, bool Available)>();
        foreach (var line in baselineLines)
        {
            var itemId = await TerminalItemIdAsync(line.ItemId, ct);
            var item = await db.Items.AsNoTracking().SingleAsync(x => x.Id == itemId, ct);
            var quantity = await ToBaseQuantityAsync(line.Quantity, line.UomId, item.BaseUomId,
                DateOnly.FromDateTime(effectiveAt.UtcDateTime), ct);
            var prior = baseline.GetValueOrDefault(itemId);
            baseline[itemId] = (prior.Quantity + quantity,
                prior.Value + (line.UnitValue.HasValue ? quantity * line.UnitValue.Value : 0),
                (prior.Available || prior.Quantity == 0) && line.UnitValue.HasValue);
        }
        var actual = new Dictionary<Guid, (decimal Quantity, decimal Value)>();
        foreach (var line in actualLines)
        {
            var itemId = await TerminalItemIdAsync(line.ItemId, ct);
            var prior = actual.GetValueOrDefault(itemId);
            actual[itemId] = (prior.Quantity + line.QuantityBase, prior.Value + line.TotalAcceptedValue);
        }
        var ids = baseline.Keys.Union(actual.Keys).ToArray();
        var items = await db.Items.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.ItemCode, x.Name, x.BaseUomId }).ToDictionaryAsync(x => x.Id, ct);
        var uomIds = items.Values.Select(x => x.BaseUomId).Distinct().ToArray();
        var uoms = await db.Uoms.AsNoTracking().Where(x => uomIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Code, ct);
        var lines = ids.Select(id =>
        {
            var item = items[id]; var actualValue = actual.GetValueOrDefault(id);
            var baselineValue = baseline.GetValueOrDefault(id);
            decimal? frozenValue = baselineValue.Available ? baselineValue.Value : null;
            return new ActualBomVarianceLineView(id, item.ItemCode, item.Name, item.BaseUomId,
                uoms[item.BaseUomId], baselineValue.Quantity, actualValue.Quantity,
                actualValue.Quantity - baselineValue.Quantity, frozenValue, actualValue.Value,
                frozenValue.HasValue ? actualValue.Value - frozenValue.Value : null);
        }).OrderBy(x => x.ItemCode).ToArray();
        var available = baseline.Values.All(x => x.Available);
        var totalBaseline = available ? baseline.Values.Sum(x => x.Value) : (decimal?)null;
        var totalActual = actualLines.Sum(x => x.TotalAcceptedValue);
        return new(type, revisionId, revisionNumber, available, totalBaseline,
            totalActual, totalBaseline.HasValue ? totalActual - totalBaseline.Value : null, lines);
    }
    private async Task<Guid> TerminalItemIdAsync(Guid itemId, CancellationToken ct)
    {
        var seen = new HashSet<Guid>();
        while (true)
        {
            if (!seen.Add(itemId)) throw new StoresConflictException("Item merge alias cycle detected.");
            var next = await db.ItemMergeAliases.AsNoTracking().Where(x => x.SourceItemId == itemId)
                .Select(x => (Guid?)x.SurvivorItemId).SingleOrDefaultAsync(ct);
            if (!next.HasValue) return itemId;
            itemId = next.Value;
        }
    }

    private async Task<decimal> ToBaseQuantityAsync(decimal quantity, Guid fromUomId,
        Guid baseUomId, DateOnly effectiveOn, CancellationToken ct)
    {
        if (fromUomId == baseUomId) return quantity;
        var conversion = await db.UomConversions.AsNoTracking().SingleAsync(x => x.IsActive &&
            x.ApprovalStatus == "APPROVED" && x.EffectiveFrom <= effectiveOn &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo >= effectiveOn) &&
            ((x.FromUomId == fromUomId && x.ToUomId == baseUomId) ||
             (x.FromUomId == baseUomId && x.ToUomId == fromUomId)), ct);
        return conversion.FromUomId == fromUomId
            ? quantity * conversion.ConversionFactor : quantity / conversion.ConversionFactor;
    }

    private sealed record VarianceBaselineLine(Guid ItemId, Guid UomId, decimal Quantity, decimal? UnitValue);
    private IQueryable<ComponentFitment> Query() => db.ComponentFitments.AsNoTracking()
        .Include(x => x.JobOrder).Include(x => x.MaterialIssueLine)!.ThenInclude(x => x!.Item)
        .Include(x => x.MaterialIssueLine)!.ThenInclude(x => x!.MaterialIssue)
        .Include(x => x.ReverifiesFitment);

    private ComponentFitmentSummary View(ComponentFitment x, bool replayed)
    {
        var reversal = db.ComponentFitmentReversals.AsNoTracking()
            .SingleOrDefault(r => r.CompanyId == x.CompanyId && r.ComponentFitmentId == x.Id);
        return new(x.Id, x.FitmentNumber, x.JobOrderId, x.JobOrder!.JobOrderNumber,
            x.MaterialIssueLineId, x.MaterialIssueLine!.ItemId, x.MaterialIssueLine.Item!.ItemCode,
            x.QuantityBase, x.FittedAt, x.ConfirmedByEmployeeId, x.ActorRoleCode,
            x.ResolvedRoleAssignmentId, x.ResolvedRoleAssignmentType, reversal is not null,
            reversal?.IsSelfReversal ?? false, reversal?.ReversedAt, reversal?.Reason,
            x.ReverifiesFitmentId, replayed);
    }

    private void RequireFull(string action)
    {
        if (!string.Equals(user.ResolvedRoleAssignmentType, "FULL", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Only a FULL assignment may {action}.");
    }
    private void AddActor(NpgsqlCommand command)
    {
        command.Parameters.AddWithValue("actor", Actor()); command.Parameters.AddWithValue("role", user.RoleCode);
        command.Parameters.AddWithValue("assignment", Assignment());
        command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!);
        command.Parameters.AddWithValue("login", user.LoginId);
    }
    private string Organization() => !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.OrganizationId.Trim().ToUpperInvariant() : throw new UnauthorizedAccessException("Company scope is required.");
    private Guid Actor() => user.EmployeeId ?? throw new UnauthorizedAccessException("Resolved employee identity is required.");
    private Guid Assignment() => user.ResolvedRoleAssignmentId ?? throw new UnauthorizedAccessException("Resolved effective assignment is required.");
    private async Task<Company> CompanyAsync(CancellationToken ct) => await db.Companies.SingleOrDefaultAsync(
        x => x.Code == Organization() && x.IsActive && x.Status == "ACTIVE", ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
    private static string Required(string? value, string field) => !string.IsNullOrWhiteSpace(value)
        ? value.Trim() : throw new StoresValidationException(field + " is required.");
    private static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions)))).ToLowerInvariant();
    private static Exception Translate(PostgresException error) =>
        error.SqlState == PostgresErrorCodes.InsufficientPrivilege
            ? new UnauthorizedAccessException(error.MessageText, error)
            : new StoresConflictException(error.MessageText);
}