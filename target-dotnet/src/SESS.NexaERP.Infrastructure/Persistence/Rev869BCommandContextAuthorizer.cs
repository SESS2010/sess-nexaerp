using System.Data;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class Rev869BCommandContextAuthorizer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string RegisterCommandRequestSql = "SELECT " + DatabaseSchemas.Advance + ".rev869b_register_command_request(@org,@operation,@key,@request,@actor,@issuer,@subject,@role)";
    private const string StartCommandAttemptSql = "SELECT " + DatabaseSchemas.Advance + ".rev869b_start_command_attempt(@command,@execution,@service,@ownership,@runtime,@backend,@transaction)";
    private const string OpenCommandAttemptSql = "SELECT " + DatabaseSchemas.Advance + ".rev869b_open_command_attempt({0},{1},{2},{3},{4},{5},{6},{7}::jsonb)";
    private const string CommitCommandAttemptSql = "SELECT " + DatabaseSchemas.Advance + ".rev869b_commit_command_attempt({0},{1},{2}::jsonb,{3})";
    private const string RecordNoncommitOutcomeSql = "SELECT " + DatabaseSchemas.Advance + ".rev869b_record_noncommit_outcome(@attempt,@execution,@service,@ownership,@state,@category,@outcome)";

    public sealed record CommandEnvelope(string Operation, string IdempotencyKey, string RequestFingerprint)
    {
        public static CommandEnvelope Create(string organization, string operation, string idempotencyKey, object request)
        {
            if (string.IsNullOrWhiteSpace(organization) || string.IsNullOrWhiteSpace(operation) || string.IsNullOrWhiteSpace(idempotencyKey))
                throw new InvalidOperationException("Organization, operation and caller idempotency key are required before command registration.");
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { organization, operation, request }, JsonOptions)))).ToLowerInvariant();
            return new(operation.Trim(), idempotencyKey.Trim(), fingerprint);
        }
    }

    public readonly record struct CommandAttemptHandle(Guid CommandId, Guid AttemptId, byte[] BusinessFingerprint,
        Guid ExecutionInstanceId, byte[] ServiceInstanceFingerprint, byte[] OwnershipLeaseFingerprint);

    public static async Task<CommandAttemptHandle?> OpenForPendingChangesAsync(
        NexaErpDbContext db, ICurrentUser user, string organization, CommandEnvelope envelope, CancellationToken ct)
    {
        RequirePrincipal(user, organization);
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("REV869B command attempts require an active service-owned business transaction.");

        var slots = await CollectSlotsAsync(db, ct);
        if (slots.Count == 0) return null;
        var slotsJson = JsonSerializer.Serialize(slots, JsonOptions);
        var businessFingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(slotsJson));
        var requestFingerprint = Convert.FromHexString(envelope.RequestFingerprint);
        var idempotencyFingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(envelope.IdempotencyKey));

        var runtime = (NpgsqlConnection)db.Database.GetDbConnection();
        if (runtime.State != ConnectionState.Open) throw new InvalidOperationException("REV869B runtime connection must be open.");
        var transaction = (NpgsqlTransaction)db.Database.CurrentTransaction.GetDbTransaction();
        int backendPid;
        long transactionId;
        string runtimePrincipal;
        await using (var identity = new NpgsqlCommand("SELECT pg_backend_pid(),txid_current(),session_user::text", runtime, transaction))
        await using (var reader = await identity.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct)) throw new InvalidOperationException("Runtime database identity is unavailable.");
            backendPid = reader.GetInt32(0);
            transactionId = reader.GetInt64(1);
            runtimePrincipal = reader.GetString(2);
        }

        var auditBuilder = RequireIndependentAuditConnection(runtime.ConnectionString);
        Guid commandId;
        Guid attemptId;
        Guid executionInstanceId = Guid.Empty;
        byte[] exactServiceFingerprint = [];
        byte[] exactOwnershipFingerprint = [];
        await using (var audit = new NpgsqlConnection(auditBuilder.ConnectionString))
        {
            await audit.OpenAsync(ct);
            await using var register = new NpgsqlCommand(RegisterCommandRequestSql, audit);
            register.Parameters.AddWithValue("org", organization);
            register.Parameters.AddWithValue("operation", envelope.Operation);
            register.Parameters.AddWithValue("key", idempotencyFingerprint);
            register.Parameters.AddWithValue("request", requestFingerprint);
            register.Parameters.AddWithValue("actor", user.EmployeeId!.Value);
            register.Parameters.AddWithValue("issuer", user.IdentityIssuer!);
            register.Parameters.AddWithValue("subject", user.IdentitySubject!);
            register.Parameters.AddWithValue("role", user.RoleCode);
            commandId = (Guid)(await register.ExecuteScalarAsync(ct) ?? throw new InvalidOperationException("Command request registration returned no identifier."));

            if (!Guid.TryParse(Environment.GetEnvironmentVariable("REV869B_EXECUTION_INSTANCE_ID"), out var executionId) || executionId == Guid.Empty)
                throw new InvalidOperationException("A non-empty REV869B execution-instance ID is required.");
            var serviceFingerprint = ExactFingerprint("REV869B_SERVICE_INSTANCE_FINGERPRINT");
            var ownershipFingerprint = ExactFingerprint("REV869B_OWNERSHIP_LEASE_FINGERPRINT");
            await using var start = new NpgsqlCommand(StartCommandAttemptSql, audit);
            start.Parameters.AddWithValue("command", commandId);
            start.Parameters.AddWithValue("execution", executionId);
            start.Parameters.AddWithValue("service", serviceFingerprint);
            start.Parameters.AddWithValue("ownership", ownershipFingerprint);
            start.Parameters.AddWithValue("runtime", runtimePrincipal);
            start.Parameters.AddWithValue("backend", backendPid);
            start.Parameters.AddWithValue("transaction", transactionId);
            var started = await start.ExecuteScalarAsync(ct);
            if (started is not Guid exactAttempt || exactAttempt == Guid.Empty)
                throw new InvalidOperationException("The command already has a committed receipt; caller must use the authoritative replay result.");
            attemptId = exactAttempt;
            executionInstanceId = executionId;
            exactServiceFingerprint = serviceFingerprint;
            exactOwnershipFingerprint = ownershipFingerprint;
        }

        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(FormattableStringFactory.Create(
                OpenCommandAttemptSql,
                attemptId, user.EmployeeId.Value, user.IdentityIssuer, user.IdentitySubject,
                user.RoleCode, organization, businessFingerprint, slotsJson), ct);
        }
        catch
        {
            await RecordNoncommitOutcomeAsync(runtime, new(commandId, attemptId, businessFingerprint,
                executionInstanceId, exactServiceFingerprint, exactOwnershipFingerprint), "Rejected", "ContextOpenRejected", ct);
            throw;
        }
        return new(commandId, attemptId, businessFingerprint,
            executionInstanceId, exactServiceFingerprint, exactOwnershipFingerprint);
    }

    public static async Task StageCommittedReceiptAsync(NexaErpDbContext db, CommandAttemptHandle attempt, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A committed receipt must be staged in the exact business transaction.");
        var response = JsonSerializer.Serialize(new { attempt.CommandId, attempt.AttemptId });
        await db.Database.ExecuteSqlInterpolatedAsync(FormattableStringFactory.Create(
            CommitCommandAttemptSql,
            attempt.AttemptId, attempt.BusinessFingerprint, response, Guid.NewGuid()), ct);
    }

    public static async Task RecordNoncommitOutcomeAsync(
        NpgsqlConnection runtimeConnection, CommandAttemptHandle attempt, string terminalState, string category, CancellationToken ct)
    {
        if (terminalState is not ("Rejected" or "RolledBack" or "Abandoned") || string.IsNullOrWhiteSpace(category))
            throw new InvalidOperationException("A minimized Rejected, RolledBack or Abandoned outcome is required.");
        var auditBuilder = RequireIndependentAuditConnection(runtimeConnection.ConnectionString);
        await using var audit = new NpgsqlConnection(auditBuilder.ConnectionString);
        await audit.OpenAsync(ct);
        await using var command = new NpgsqlCommand(RecordNoncommitOutcomeSql, audit);
        command.Parameters.AddWithValue("attempt", attempt.AttemptId);
        command.Parameters.AddWithValue("execution", attempt.ExecutionInstanceId);
        command.Parameters.AddWithValue("service", attempt.ServiceInstanceFingerprint);
        command.Parameters.AddWithValue("ownership", attempt.OwnershipLeaseFingerprint);
        command.Parameters.AddWithValue("state", terminalState);
        command.Parameters.AddWithValue("category", category.Trim());
        command.Parameters.AddWithValue("outcome", DeterministicOutcomeId(attempt.AttemptId, terminalState, category.Trim()));
        if (await command.ExecuteScalarAsync(ct) is not Guid) throw new InvalidOperationException("No durable noncommit outcome was recorded.");
    }

    private static NpgsqlConnectionStringBuilder RequireIndependentAuditConnection(string runtimeConnection)
    {
        var raw = Environment.GetEnvironmentVariable("REV869B_COMMAND_AUDIT_CONNECTION");
        if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException("A distinct REV869B command-audit connection is required.");
        var audit = new NpgsqlConnectionStringBuilder(raw) { Pooling = false };
        var runtime = new NpgsqlConnectionStringBuilder(runtimeConnection);
        if (!string.Equals(audit.Database, runtime.Database, StringComparison.Ordinal) ||
            string.Equals(audit.Username, runtime.Username, StringComparison.Ordinal))
            throw new InvalidOperationException("Command audit must target the exact database through a principal distinct from runtime.");
        return audit;
    }

    private static byte[] ExactFingerprint(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (value is null || value.Length != 64 || value.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException(name + " must be an exact SHA-256 fingerprint.");
        return Convert.FromHexString(value);
    }

    private static Guid DeterministicOutcomeId(Guid attemptId, string terminalState, string category)
    {
        var material = Encoding.UTF8.GetBytes($"REV869B-NONCOMMIT|{attemptId:D}|{terminalState}|{category}");
        return new Guid(SHA256.HashData(material)[..16]);
    }

    private static void RequirePrincipal(ICurrentUser user, string organization)
    {
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(organization) ||
            string.IsNullOrWhiteSpace(user.IdentityIssuer) || string.IsNullOrWhiteSpace(user.IdentitySubject) ||
            string.IsNullOrWhiteSpace(user.RoleCode) || !string.Equals(user.LoginId, user.IdentitySubject, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("An exact authenticated OIDC issuer/subject employee identity is required.");
    }

    private static async Task<List<OperationSlot>> CollectSlotsAsync(NexaErpDbContext db, CancellationToken ct)
    {
        var result = new List<OperationSlot>();
        foreach (var history in db.ChangeTracker.Entries<PurchaseTransactionStatusHistory>().Where(x => x.State == EntityState.Added).Select(x => x.Entity))
        {
            var version = history.EntityType switch
            {
                "RFQ" => TrackedVersion<RequestForQuotation>(db, history.EntityId) ?? await NextVersionAsync(db.RequestForQuotations, history.EntityId, ct),
                "RFQInvitation" => TrackedVersion<RfqVendorInvitation>(db, history.EntityId) ?? await NextVersionAsync(db.RfqVendorInvitations, history.EntityId, ct),
                "VendorQuotation" => TrackedVersion<VendorQuotation>(db, history.EntityId) ?? await NextVersionAsync(db.VendorQuotations, history.EntityId, ct),
                "TechnicalVerification" => TrackedVersion<QuotationTechnicalVerification>(db, history.EntityId) ?? await NextVersionAsync(db.QuotationTechnicalVerifications, history.EntityId, ct),
                "CommercialComparison" => TrackedVersion<CommercialComparison>(db, history.EntityId) ?? await NextVersionAsync(db.CommercialComparisons, history.EntityId, ct),
                "PurchaseOrder" => TrackedVersion<PurchaseOrder>(db, history.EntityId) ?? await NextVersionAsync(db.PurchaseOrders, history.EntityId, ct),
                "MaterialFollowUp" => TrackedVersion<MaterialFollowUpHandoff>(db, history.EntityId) ?? await NextVersionAsync(db.MaterialFollowUpHandoffs, history.EntityId, ct),
                _ => throw new InvalidOperationException("Unsupported command slot entity type.")
            };
            result.Add(new("purchase_transaction_status_history", history.Id, history.EntityType, history.EntityId, history.Action, version, history.FromStatus, history.ToStatus, history.CorrelationId, history.Remarks));
        }
        foreach (var history in db.ChangeTracker.Entries<PurchaseTransactionApprovalHistory>().Where(x => x.State == EntityState.Added).Select(x => x.Entity))
        {
            var version = TrackedVersion<CommercialComparison>(db, history.CommercialComparisonId) ?? await NextVersionAsync(db.CommercialComparisons, history.CommercialComparisonId, ct);
            result.Add(new("purchase_transaction_approval_history", history.Id, "CommercialComparison", history.CommercialComparisonId, history.Action, version, history.FromStatus, history.ToStatus, history.CorrelationId, history.Remarks));
        }
        foreach (var history in db.ChangeTracker.Entries<PurchaseOrderHistory>().Where(x => x.State == EntityState.Added).Select(x => x.Entity))
        {
            var version = TrackedVersion<PurchaseOrder>(db, history.PurchaseOrderId) ?? await NextVersionAsync(db.PurchaseOrders, history.PurchaseOrderId, ct);
            result.Add(new("purchase_order_history", history.Id, "PurchaseOrder", history.PurchaseOrderId, history.Action, version, history.FromStatus, history.ToStatus, history.CorrelationId, history.Reason));
        }
        foreach (var history in db.ChangeTracker.Entries<ControlledConfigurationHistory>().Where(x => x.State == EntityState.Added && x.Entity.EntityType == nameof(VendorQualification)).Select(x => x.Entity))
        {
            var qualification = db.ChangeTracker.Entries<VendorQualification>().Single(x => x.Entity.Id == history.EntityId).Entity;
            var version = history.Action == "Create" ? 0L : checked((long)qualification.Version - 1L);
            var from = history.Action switch { "Approve" or "Reject" => MasterApprovalStatuses.PendingApproval, "RequestCorrection" => MasterApprovalStatuses.Approved, "Normalize" => MasterApprovalStatuses.Draft, _ => null };
            var to = history.Action switch { "Verify" => MasterApprovalStatuses.Verified, "Approve" => MasterApprovalStatuses.Approved, "Reject" => MasterApprovalStatuses.Rejected, "RequestCorrection" => MasterApprovalStatuses.RevisionRequested, _ => MasterApprovalStatuses.PendingApproval };
            result.Add(new("qualification_history", history.Id, nameof(VendorQualification), history.EntityId, history.Action, version, from, to, history.CorrelationId, history.Remarks));
        }
        if (result.GroupBy(x => new { x.ClaimKind, x.EntityType, x.EntityId, x.Operation, x.ParentVersion, x.Correlation }).Any(x => x.Count() != 1))
            throw new InvalidOperationException("Duplicate semantic command slots are prohibited before registration.");
        return result;
    }

    private static long? TrackedVersion<T>(NexaErpDbContext db, Guid id) where T : AuditableEntity =>
        db.ChangeTracker.Entries<T>().Where(x => x.Entity.Id == id).Select(x => (long?)x.Entity.Version).SingleOrDefault();
    private static Task<long> NextVersionAsync<T>(IQueryable<T> query, Guid id, CancellationToken ct) where T : AuditableEntity =>
        query.Where(x => x.Id == id).Select(x => checked((long)x.Version + 1L)).SingleAsync(ct);

    private sealed record OperationSlot(string ClaimKind, Guid HistoryId, string EntityType, Guid EntityId,
        string Operation, long ParentVersion, string? FromStatus, string ToStatus, string Correlation, string Remarks);
}
