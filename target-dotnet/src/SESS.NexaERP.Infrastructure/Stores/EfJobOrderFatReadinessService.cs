using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfJobOrderFatReadinessService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit)
    : IJobOrderFatReadinessService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] ExplainRoles = ["PRODUCTION_OPERATOR", "PRODUCTION_COORDINATOR", "PRODUCTION_MANAGER", "SERVICE_ENGINEER"];
    private static readonly string[] ReconcileRoles = ["QC_MANAGER", "DESIGN_ENGINEER"];

    public async Task<JobOrderFatReadinessView?> GetAsync(Guid jobOrderId, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var job = await db.JobOrders.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == jobOrderId, ct);
        if (job is null) return null;
        FatReconciliationView? latest = null;
        if (job.LatestFatReconciliationId.HasValue)
            latest = View(await Reconciliations().SingleAsync(x => x.CompanyId == company.Id && x.Id == job.LatestFatReconciliationId, ct), false);
        return new(job.Id, job.JobOrderNumber, job.FatReadinessStatus, job.FatReconciledAt,
            job.FatReconciledByEmployeeId, job.LatestFatReconciliationId, latest);
    }

    public async Task<FatCustodyExplanationView> ExplainAsync(Guid jobOrderId,
        CreateFatCustodyExplanationRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("create", ExplainRoles); RequireFull("explain FAT custody");
        if (request.MaterialIssueLineId == Guid.Empty) throw new StoresValidationException("MaterialIssueLineId is required.");
        if (request.QuantityBase <= 0) throw new StoresValidationException("QuantityBase must be positive.");
        var disposition = Required(request.Disposition, "Disposition").ToUpperInvariant();
        if (disposition is not ("LOST" or "SCRAPPED"))
            throw new StoresValidationException("Disposition must be LOST or SCRAPPED. An accepted return document proves RETURNED_LATE.");
        var reason = Required(request.Reason, "Reason"); var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var fingerprint = Fingerprint(new { jobOrderId, request.MaterialIssueLineId, request.QuantityBase, disposition, reason });
        var correlation = Fingerprint("JobOrder.FatExplain:" + key);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var version = await db.JobOrders.Where(x => x.CompanyId == company.Id && x.Id == jobOrderId).Select(x => (long)x.Version).SingleOrDefaultAsync(ct);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization(), "JobOrder.FatExplain", key, request);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user, Organization(), envelope,
            "job_order_history", nameof(JobOrder), jobOrderId, "FAT_EXPLAIN", version, null, "EXPLAINED", correlation, reason, ct);
        (Guid Id, bool Replayed) result;
        try
        {
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var command = connection.CreateCommand(); command.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = "SELECT \"ExplanationId\",\"Replayed\" FROM advance.create_fat_custody_explanation(@company,@job,@line,@quantity,@disposition,@reason,@key,@hash,@correlation,@actor,@role,@assignment,@type,@login)";
            command.Parameters.AddWithValue("company", company.Id); command.Parameters.AddWithValue("job", jobOrderId);
            command.Parameters.AddWithValue("line", request.MaterialIssueLineId); command.Parameters.AddWithValue("quantity", request.QuantityBase);
            command.Parameters.AddWithValue("disposition", disposition); command.Parameters.AddWithValue("reason", reason);
            command.Parameters.AddWithValue("key", key); command.Parameters.AddWithValue("hash", fingerprint); command.Parameters.AddWithValue("correlation", correlation); AddActor(command);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) throw new StoresConflictException("Controlled FAT explanation returned no result.");
            result = (reader.GetGuid(0), reader.GetBoolean(1));
        }
        catch (PostgresException e) { throw Translate(e); }
        if (!result.Replayed) await audit.WriteAsync("Production", "JobOrder.FatExplain", nameof(JobOrder), jobOrderId.ToString(), null,
            new { request.MaterialIssueLineId, request.QuantityBase, Disposition = disposition, Reason = reason }, ct);
        if (!result.Replayed) await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        var row = await db.JobOrderFatCustodyExplanations.AsNoTracking().SingleAsync(x => x.CompanyId == company.Id && x.Id == result.Id, ct);
        return ExplanationView(row, result.Replayed);
    }

    public async Task<FatReconciliationView> ReconcileAsync(Guid jobOrderId,
        ReconcileJobOrderFatRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("verify", ReconcileRoles); RequireFull("reconcile FAT readiness");
        var reason = Required(request.Reason, "Reason"); var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var fingerprint = Fingerprint(new { jobOrderId, reason }); var correlation = Fingerprint("JobOrder.FatReconcile:" + key);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var job = await db.JobOrders.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == jobOrderId, ct)
            ?? throw new KeyNotFoundException("Job Order was not found.");
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization(), "JobOrder.FatReconcile", key, request);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user, Organization(), envelope,
            "job_order_history", nameof(JobOrder), jobOrderId, "FAT_RECONCILE", job.Version,
            job.FatReadinessStatus, "FAT_RESULT", correlation, reason, ct);
        (Guid Id, string Result, bool Replayed) result;
        try
        {
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var command = connection.CreateCommand(); command.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = "SELECT \"ReconciliationId\",\"Result\",\"Replayed\" FROM advance.reconcile_job_order_fat(@company,@job,@reason,@key,@hash,@correlation,@actor,@role,@assignment,@type,@login)";
            command.Parameters.AddWithValue("company", company.Id); command.Parameters.AddWithValue("job", jobOrderId);
            command.Parameters.AddWithValue("reason", reason); command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("hash", fingerprint); command.Parameters.AddWithValue("correlation", correlation); AddActor(command);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) throw new StoresConflictException("Controlled FAT reconciliation returned no result.");
            result = (reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2));
        }
        catch (PostgresException e) { throw Translate(e); }
        if (!result.Replayed) await audit.WriteAsync("Production", "JobOrder.FatReconcile", nameof(JobOrder), jobOrderId.ToString(),
            new { job.FatReadinessStatus }, new { FatReadinessStatus = result.Result, Reason = reason }, ct);
        if (!result.Replayed) await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        return View(await Reconciliations().SingleAsync(x => x.CompanyId == company.Id && x.Id == result.Id, ct), result.Replayed);
    }

    private IQueryable<JobOrderFatReconciliation> Reconciliations() => db.JobOrderFatReconciliations.AsNoTracking().Include(x => x.Lines);
    private static FatCustodyExplanationView ExplanationView(JobOrderFatCustodyExplanation x, bool replayed) =>
        new(x.Id, x.JobOrderId, x.MaterialIssueLineId, x.QuantityBase, x.Disposition, x.Reason,
            x.ExplainedByEmployeeId, x.ActorRoleCode, x.ResolvedRoleAssignmentId, x.ResolvedRoleAssignmentType, x.CreatedAt, replayed);
    private static FatReconciliationView View(JobOrderFatReconciliation x, bool replayed) =>
        new(x.Id, x.JobOrderId, x.AttemptNumber, x.Result, x.IssuedQuantityBase, x.FittedQuantityBase,
            x.ReturnedQuantityBase, x.ExplainedQuantityBase, x.UnexplainedQuantityBase, x.ReconciledAt,
            x.ReconciledByEmployeeId, x.ActorRoleCode, x.ResolvedRoleAssignmentId, x.ResolvedRoleAssignmentType,
            x.Reason, x.Lines.OrderBy(l => l.CreatedAt).ThenBy(l => l.Id).Select(l => new FatReconciliationLineView(
                l.Id, l.MaterialIssueLineId, l.ItemId, l.ItemCodeSnapshot, l.CustodianEmployeeId,
                l.CustodianEmployeeCodeSnapshot, l.IssuedQuantityBase, l.FittedQuantityBase,
                l.ReturnedQuantityBase, l.ReturnedLateQuantityBase, l.ExplainedLostQuantityBase,
                l.ExplainedScrappedQuantityBase, l.UnexplainedQuantityBase, l.Classification)).ToArray(), replayed);
    private void AddActor(NpgsqlCommand command)
    {
        command.Parameters.AddWithValue("actor", Actor()); command.Parameters.AddWithValue("role", user.RoleCode);
        command.Parameters.AddWithValue("assignment", Assignment()); command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!);
        command.Parameters.AddWithValue("login", user.LoginId);
    }
    private void RequireFull(string action) { if (!string.Equals(user.ResolvedRoleAssignmentType, "FULL", StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException($"Only a FULL assignment may {action}."); }
    private Guid Actor() => user.EmployeeId ?? throw new UnauthorizedAccessException("Resolved employee identity is required.");
    private Guid Assignment() => user.ResolvedRoleAssignmentId ?? throw new UnauthorizedAccessException("Resolved effective assignment is required.");
    private string Organization() => !string.IsNullOrWhiteSpace(user.OrganizationId) ? user.OrganizationId.Trim().ToUpperInvariant() : throw new UnauthorizedAccessException("Company scope is required.");
    private async Task<SESS.NexaERP.Domain.Foundation.Company> CompanyAsync(CancellationToken ct) => await db.Companies.SingleOrDefaultAsync(x => x.Code == Organization() && x.IsActive && x.Status == "ACTIVE", ct) ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
    private static string Required(string? value, string field) => !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new StoresValidationException(field + " is required.");
    private static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions)))).ToLowerInvariant();
    private static Exception Translate(PostgresException error) => error.SqlState == PostgresErrorCodes.InsufficientPrivilege ? new UnauthorizedAccessException(error.MessageText, error) : new StoresConflictException(error.MessageText);
}