using System.Data;
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
    private const string RecordNoncommitOutcomeSql = "SELECT " + DatabaseSchemas.Advance + ".rev869b_record_noncommit_outcome(@attempt,@execution,@service,@ownership,@state,@category,@outcome)";
    private const string OrdinaryLedgerExistsSql = "SELECT session_user='nexa_erp_runtime' AND to_regprocedure('advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)') IS NOT NULL";
    private const string RegisterOrdinaryCommandSql = "SELECT " + DatabaseSchemas.Advance + ".register_command_request(@org,@operation,@key,@request,@actor,@issuer,@subject,@role,@assignment)";
    private const string CommitOrdinaryReceiptSql = "SELECT " + DatabaseSchemas.Advance + ".commit_command_receipt(@command,@business,CAST(@response AS jsonb),@receipt)";

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
        Guid ExecutionInstanceId, byte[] ServiceInstanceFingerprint, byte[] OwnershipLeaseFingerprint,
        bool IsOrdinaryLedger = false);

    public static async Task<CommandAttemptHandle?> OpenForPendingChangesAsync(
        NexaErpDbContext db, ICurrentUser user, string organization, CommandEnvelope envelope, CancellationToken ct,
        string? selectedRoleCode = null)
    {
        RequirePrincipal(user, organization);
        var actorRole = user.RoleCode;
        var actorAssignmentId = user.ResolvedRoleAssignmentId ??
            throw new UnauthorizedAccessException("A resolved effective role-assignment ID is required for a controlled command.");
        if (string.IsNullOrWhiteSpace(actorRole) || string.Equals(actorRole, "none", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("An effective acting role is required for a controlled command.");
        if (!string.IsNullOrWhiteSpace(selectedRoleCode) &&
            !string.Equals(selectedRoleCode.Trim(), actorRole, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("The command role does not match the validated request acting role.");
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Controlled commands require an active service-owned business transaction.");
        db.Database.AutoSavepointsEnabled = false;

        var slots = await CollectSlotsAsync(db, ct);
        if (slots.Count == 0) return null;
        var slotsJson = JsonSerializer.Serialize(slots, JsonOptions);
        var businessFingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(slotsJson));
        var requestFingerprint = Convert.FromHexString(envelope.RequestFingerprint);
        var idempotencyFingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(envelope.IdempotencyKey));

        var runtime = (NpgsqlConnection)db.Database.GetDbConnection();
        if (runtime.State != ConnectionState.Open)
            throw new InvalidOperationException("Controlled-command runtime connection must be open.");
        var transaction = (NpgsqlTransaction)db.Database.CurrentTransaction.GetDbTransaction();
        if (!await OrdinaryLedgerAvailableAsync(runtime, transaction, ct))
            throw new InvalidOperationException("The ordinary command ledger is not installed for this deployment.");
        var commandId = await RegisterOrdinaryCommandAsync(
            runtime, transaction, user, organization, envelope,
            idempotencyFingerprint, requestFingerprint, actorRole, actorAssignmentId, ct);
        return new(commandId, commandId, businessFingerprint, Guid.Empty, [], [], true);
    }

    private static async Task<bool> OrdinaryLedgerAvailableAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(OrdinaryLedgerExistsSql, connection, transaction);
        return await command.ExecuteScalarAsync(ct) is true;
    }

    private static async Task<Guid> RegisterOrdinaryCommandAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, ICurrentUser user,
        string organization, CommandEnvelope envelope, byte[] idempotencyFingerprint,
        byte[] requestFingerprint, string actorRole, Guid actorAssignmentId, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(RegisterOrdinaryCommandSql, connection, transaction);
        command.Parameters.AddWithValue("org", organization);
        command.Parameters.AddWithValue("operation", envelope.Operation);
        command.Parameters.AddWithValue("key", idempotencyFingerprint);
        command.Parameters.AddWithValue("request", requestFingerprint);
        command.Parameters.AddWithValue("actor", user.EmployeeId!.Value);
        command.Parameters.AddWithValue("issuer", user.IdentityIssuer!);
        command.Parameters.AddWithValue("subject", user.IdentitySubject!);
        command.Parameters.AddWithValue("role", actorRole);
        command.Parameters.AddWithValue("assignment", actorAssignmentId);
        return await command.ExecuteScalarAsync(ct) is Guid commandId && commandId != Guid.Empty
            ? commandId
            : throw new InvalidOperationException("Ordinary command registration returned no identifier.");
    }

    public static async Task StageCommittedReceiptAsync(NexaErpDbContext db, CommandAttemptHandle attempt, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A committed receipt must be staged in the exact business transaction.");
        await db.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL IMMEDIATE", ct);
        var response = JsonSerializer.Serialize(new { attempt.CommandId, attempt.AttemptId });
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var transaction = (NpgsqlTransaction)db.Database.CurrentTransaction.GetDbTransaction();
        await using var command = new NpgsqlCommand(CommitOrdinaryReceiptSql, connection, transaction);
        command.Parameters.AddWithValue("command", attempt.CommandId);
        command.Parameters.AddWithValue("business", attempt.BusinessFingerprint);
        command.Parameters.AddWithValue("response", response);
        command.Parameters.AddWithValue("receipt", Guid.NewGuid());
        if (await command.ExecuteScalarAsync(ct) is not Guid)
            throw new InvalidOperationException("Ordinary command receipt was not staged.");
    }

    public static async Task RecordNoncommitOutcomeAsync(
        NpgsqlConnection runtimeConnection, CommandAttemptHandle attempt, string terminalState, string category, CancellationToken ct)
    {
        if (terminalState is not ("Rejected" or "RolledBack" or "Abandoned") || string.IsNullOrWhiteSpace(category))
            throw new InvalidOperationException("A minimized Rejected, RolledBack or Abandoned outcome is required.");
        if (attempt.IsOrdinaryLedger) return;
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
            var from = history.Action switch
            {
                "Verify" or "Approve" or "Reject" => MasterApprovalStatuses.PendingApproval,
                "RequestCorrection" => MasterApprovalStatuses.Approved,
                "Normalize" => MasterApprovalStatuses.Draft,
                _ => null
            };
            var to = history.Action switch { "Verify" => MasterApprovalStatuses.Verified, "Approve" => MasterApprovalStatuses.Approved, "Reject" => MasterApprovalStatuses.Rejected, "RequestCorrection" => MasterApprovalStatuses.RevisionRequested, _ => MasterApprovalStatuses.PendingApproval };
            result.Add(new("qualification_history", history.Id, nameof(VendorQualification), history.EntityId, history.Action, version, from, to, history.CorrelationId, history.Remarks));
        }
        foreach (var history in db.ChangeTracker.Entries<ControlledConfigurationHistory>().Where(x => x.State == EntityState.Added && x.Entity.EntityType == nameof(TaxGstSetting)).Select(x => x.Entity))
        {
            var rule = db.ChangeTracker.Entries<TaxGstSetting>().Single(x => x.Entity.Id == history.EntityId).Entity;
            var parentVersion = history.Action == "Create" ? 0L : checked((long)rule.Version - 1L);
            var from = history.Action == "Create" ? null : MasterApprovalStatuses.PendingApproval;
            var to = history.Action switch { "Approve" => MasterApprovalStatuses.Approved, "Reject" => MasterApprovalStatuses.Rejected, _ => MasterApprovalStatuses.PendingApproval };
            result.Add(new("tax_history", history.Id, nameof(TaxGstSetting), history.EntityId, history.Action, parentVersion, from, to, history.CorrelationId, history.Remarks));
        }
        if (result.GroupBy(x => new { x.ClaimKind, x.EntityType, x.EntityId, x.Operation, x.ParentVersion, x.Correlation }).Any(x => x.Count() != 1))
            throw new InvalidOperationException("Duplicate semantic command slots are prohibited before registration.");
        return result;
    }

    private static long? TrackedVersion<T>(NexaErpDbContext db, Guid id) where T : AuditableEntity =>
        db.ChangeTracker.Entries<T>()
            .Where(x => x.Entity.Id == id && x.State is EntityState.Added or EntityState.Modified)
            .Select(x => (long?)x.Entity.Version).SingleOrDefault();
    private static Task<long> NextVersionAsync<T>(IQueryable<T> query, Guid id, CancellationToken ct) where T : AuditableEntity =>
        query.Where(x => x.Id == id).Select(x => checked((long)x.Version + 1L)).SingleAsync(ct);

    private sealed record OperationSlot(string ClaimKind, Guid HistoryId, string EntityType, Guid EntityId,
        string Operation, long ParentVersion, string? FromStatus, string ToStatus, string Correlation, string Remarks);
}
