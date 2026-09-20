using System.Data;
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
    public async Task<MaterialReturnView> CreateReturnAsync(
        Guid materialIssueId, CreateMaterialReturn command, CancellationToken ct)
    {
        _ = RequireAny("create");
        var key = Required(command.IdempotencyKey, "IdempotencyKey");
        var hash = Fingerprint(new { materialIssueId, command });
        if (command.DeclaredAt == default) throw new StoresValidationException("DeclaredAt is required.");
        if (command.Lines is null || command.Lines.Count == 0)
            throw new StoresValidationException("At least one scanner-confirmed return line is required.");
        if (command.Lines.GroupBy(x => x.MaterialIssueLineId).Any(x => x.Count() != 1))
            throw new StoresValidationException("Each issue allocation may appear only once in a return declaration.");
        if (command.Lines.Any(x => x.MaterialIssueLineId == Guid.Empty ||
            string.IsNullOrWhiteSpace(x.ScanCode) || x.ReturnedQuantity <= 0 ||
            x.ReportedConsumedQuantity < 0 || x.ReportedStillHeldQuantity < 0))
            throw new StoresValidationException("Each return line requires an issue line, scan, positive returned quantity and non-negative remainder statement.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({'R' + company.Code + key},0))", ct);
        var replay = await ReturnQuery().SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.CreateIdempotencyKey == key, ct);
        if (replay is not null)
        {
            if (replay.CreateRequestFingerprint != hash)
                throw new StoresConflictException("Idempotency key was reused with different return content.");
            await tx.CommitAsync(ct);
            return ReturnView(replay, true);
        }

        var issue = await IssueQuery(true).SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.Id == materialIssueId, ct)
            ?? throw new KeyNotFoundException("Material Issue was not found.");
        if (issue.Status is not ("ISSUED" or "PARTIALLY_RETURNED"))
            throw new StoresConflictException("Only outstanding issued custody may be returned.");
        if (issue.IssuedToEmployeeId != Actor())
            throw new UnauthorizedAccessException("Only the named issue custodian may declare this return.");

        var row = new MaterialReturn
        {
            CompanyId = company.Id, ReturnNumber = await NextNumberAsync(company.Id, "MR", ct),
            MaterialIssueId = issue.Id, ReturnedByEmployeeId = Actor(), DeclaredAt = command.DeclaredAt,
            Status = "SUBMITTED", CreatedByEmployeeId = Actor(), ActorRoleCode = user.RoleCode,
            ResolvedRoleAssignmentId = user.ResolvedRoleAssignmentId!.Value,
            ResolvedRoleAssignmentType = user.ResolvedRoleAssignmentType!,
            CreateIdempotencyKey = key, CreateRequestFingerprint = hash, CreatedBy = user.LoginId
        };
        var lineNumber = 0;
        foreach (var input in command.Lines)
        {
            var issueLine = issue.Lines.SingleOrDefault(x => x.Id == input.MaterialIssueLineId)
                ?? throw new StoresValidationException("A return scan references an allocation outside this Material Issue.");
            var alreadyDeclared = await db.MaterialReturnLines.AsNoTracking().Where(x =>
                x.CompanyId == company.Id && x.MaterialIssueLineId == issueLine.Id)
                .SumAsync(x => (decimal?)x.ReturnedQuantityBase, ct) ?? 0;
            // Quantity already fitted into a machine (net of reversals) is no longer in the engineer's
            // custody; the returner cannot be asked to declare it returned, consumed or still held.
            var fitted = await db.ComponentFitments.AsNoTracking().Where(f =>
                f.CompanyId == company.Id && f.MaterialIssueLineId == issueLine.Id &&
                !db.ComponentFitmentReversals.Any(r => r.CompanyId == f.CompanyId && r.ComponentFitmentId == f.Id))
                .SumAsync(f => (decimal?)f.QuantityBase, ct) ?? 0;
            var outstanding = issueLine.QuantityBase - alreadyDeclared - fitted;
            if (outstanding <= 0 || input.ReturnedQuantity > outstanding)
                throw new StoresConflictException("Returned quantity may not exceed the outstanding issued quantity.");
            if (input.ReturnedQuantity + input.ReportedConsumedQuantity + input.ReportedStillHeldQuantity != outstanding)
                throw new StoresValidationException("Returner statement must reconcile the entire outstanding issue quantity as returned, reportedly consumed, or still held.");
            var normalized = NormalizeScan(input.ScanCode);
            if (issueLine.InventorySerialId.HasValue)
            {
                var serial = await db.InventorySerials.AsNoTracking().SingleAsync(x =>
                    x.CompanyId == company.Id && x.Id == issueLine.InventorySerialId.Value, ct);
                if (NormalizeScan(serial.StoredSerialNumber) != normalized || input.ReturnedQuantity != 1 ||
                    input.ReportedConsumedQuantity != 0 || input.ReportedStillHeldQuantity != 0)
                    throw new StoresValidationException("Serialized return requires the exact issued serial scan and a complete Quantity 1 return.");
            }
            else
            {
                var itemCode = await db.Items.AsNoTracking().Where(x => x.Id == issueLine.ItemId)
                    .Select(x => x.ItemCode).SingleAsync(ct);
                if (NormalizeScan(itemCode) != normalized)
                    throw new StoresValidationException("Non-serialized return scan must match the issued item code.");
            }
            row.Lines.Add(new MaterialReturnLine
            {
                CompanyId = company.Id, MaterialReturnId = row.Id, MaterialIssueLineId = issueLine.Id,
                LineNumber = ++lineNumber, ItemId = issueLine.ItemId,
                ReturnedQuantityBase = input.ReturnedQuantity,
                ReportedConsumedQuantityBase = input.ReportedConsumedQuantity,
                ReportedStillHeldQuantityBase = input.ReportedStillHeldQuantity,
                ScanCode = normalized, InventorySerialId = issueLine.InventorySerialId, CreatedBy = user.LoginId
            });
        }
        db.MaterialReturns.Add(row);
        ReturnHistory(row, "CREATE", null, "SUBMITTED",
            "Return declared by the named custodian; custody remains with the employee until Stores acceptance.", key);
        await CommitAsync("MaterialReturn.Create", key, command, nameof(MaterialReturn), row.Id,
            new { row.ReturnNumber, row.MaterialIssueId }, ct);
        await tx.CommitAsync(ct);
        return ReturnView(row, false);
    }

    public async Task<MaterialReturnView> AcceptReturnAsync(
        Guid id, AcceptMaterialReturn command, CancellationToken ct)
    {
        _ = user.RequireRole("approve", "STORES_ASSISTANT", "STORES_EXECUTIVE", "STORES_MANAGER");
        var key = Required(command.IdempotencyKey, "IdempotencyKey");
        var reason = Required(command.Reason, "Reason");
        if (command.AcceptedAt == default) throw new StoresValidationException("AcceptedAt is required.");
        var hash = Fingerprint(new { id, command });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({'A' + company.Code + key},0))", ct);
        var replay = await ReturnQuery().SingleOrDefaultAsync(x =>
            x.CompanyId == company.Id && x.AcceptanceIdempotencyKey == key, ct);
        if (replay is not null)
        {
            if (replay.AcceptanceRequestFingerprint != hash)
                throw new StoresConflictException("Idempotency key was reused with different return acceptance content.");
            await tx.CommitAsync(ct);
            return ReturnView(replay, true);
        }
        var row = await ReturnQuery(true).SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id, ct)
            ?? throw new KeyNotFoundException("Material Return was not found.");
        if (row.Status != "SUBMITTED") throw new StoresConflictException("Only a Submitted return may be accepted.");
        if (row.Version != command.Version) throw new DbUpdateConcurrencyException("Material Return Version is stale.");
        if (row.ReturnedByEmployeeId == Actor())
            throw new StoresConflictException("Nobody may accept their own material return.");
        if (command.AcceptedAt < row.DeclaredAt)
            throw new StoresValidationException("AcceptedAt cannot precede the return declaration.");

        row.Status = "ACCEPTED"; row.AcceptedAt = command.AcceptedAt;
        row.AcceptedByEmployeeId = Actor(); row.AcceptedActorRoleCode = user.RoleCode;
        row.AcceptedRoleAssignmentId = user.ResolvedRoleAssignmentId!.Value;
        row.AcceptedRoleAssignmentType = user.ResolvedRoleAssignmentType;
        row.AcceptanceReason = reason; row.AcceptanceIdempotencyKey = key;
        row.AcceptanceRequestFingerprint = hash; row.Version++;
        row.UpdatedAt = DateTimeOffset.UtcNow; row.UpdatedBy = user.LoginId;
        ReturnHistory(row, "ACCEPT", "SUBMITTED", "ACCEPTED", reason, key);

        var issue = row.MaterialIssue!;
        var acceptedBefore = await db.MaterialReturnLines.AsNoTracking().Where(x =>
            x.CompanyId == company.Id && x.MaterialReturn!.MaterialIssueId == issue.Id &&
            x.MaterialReturn.Status == "ACCEPTED" && x.MaterialReturnId != row.Id)
            .SumAsync(x => (decimal?)x.ReturnedQuantityBase, ct) ?? 0;
        var issued = issue.Lines.Sum(x => x.QuantityBase);
        var acceptedNow = row.Lines.Sum(x => x.ReturnedQuantityBase);
        var fromIssueStatus = issue.Status;
        issue.Status = acceptedBefore + acceptedNow == issued ? "RETURNED" : "PARTIALLY_RETURNED";
        issue.Version++; issue.UpdatedAt = DateTimeOffset.UtcNow; issue.UpdatedBy = user.LoginId;
        History(null, issue, "RETURN_ACCEPT", fromIssueStatus, issue.Status,
            $"Accepted return {row.ReturnNumber}; original FIFO consumption restored by linked immutable entries.", key + ":ISSUE");

        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
            Organization(), "MaterialReturn.Accept", key, new { id, command });
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(
            db, user, Organization(), envelope, ct, user.RoleCode)
            ?? throw new InvalidOperationException("Material return acceptance produced no immutable operation slot.");
        await db.SaveChangesAsync(ct);
        var posting = await PostReturnAsync(company.Id, row.Id, key, hash, ct);
        row.StockPostingBatchId = posting.BatchId;
        await audit.WriteAsync("Stores", "MaterialReturn.Accept", nameof(MaterialReturn), row.Id.ToString(),
            null, new { row.ReturnNumber, posting.BatchId }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        return ReturnView(row, posting.Replayed);
    }

    private async Task<PostingResult> PostReturnAsync(Guid companyId, Guid returnId,
        string key, string hash, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """SELECT "StockPostingBatchId","Replayed" FROM advance.post_material_return_acceptance(@company,@return,@key,@fingerprint,@correlation,@actor,@login)""";
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("return", returnId);
        command.Parameters.AddWithValue("key", "POST:" + key);
        command.Parameters.AddWithValue("fingerprint", hash);
        command.Parameters.AddWithValue("correlation", key);
        command.Parameters.AddWithValue("actor", Actor());
        command.Parameters.AddWithValue("login", user.LoginId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new StoresConflictException("Controlled material return returned no result.");
        return new(reader.GetGuid(0), reader.GetBoolean(1));
    }

    private void ReturnHistory(MaterialReturn row, string action, string? from, string to,
        string remarks, string correlation)
    {
        db.MaterialReturnHistories.Add(new MaterialReturnHistory
        {
            CompanyId = row.CompanyId, MaterialReturnId = row.Id, Action = action,
            FromStatus = from, ToStatus = to, ActorEmployeeId = Actor(), ActorRoleCode = user.RoleCode,
            ResolvedRoleAssignmentId = user.ResolvedRoleAssignmentId!.Value,
            ResolvedRoleAssignmentType = user.ResolvedRoleAssignmentType!,
            CorrelationId = correlation, Remarks = remarks
        });
    }
}
