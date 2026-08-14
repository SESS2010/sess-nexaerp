
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

    public static async Task<Guid?> OpenForPendingChangesAsync(
        NexaErpDbContext db,
        ICurrentUser user,
        string organization,
        CancellationToken ct)
    {
        RequirePrincipal(user, organization);
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Exact REV869B grants require an active business transaction.");

        var slots = await CollectSlotsAsync(db, ct);
        if (slots.Count == 0) return null;

        var runtimeConnection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (runtimeConnection.State != ConnectionState.Open)
            throw new InvalidOperationException("REV869B runtime connection must already be open.");
        var runtimeTransaction = (NpgsqlTransaction)db.Database.CurrentTransaction.GetDbTransaction();
        int backendPid;
        long transactionId;
        string runtimePrincipal;
        await using (var identity = new NpgsqlCommand(
            "SELECT pg_backend_pid(),txid_current(),session_user::text", runtimeConnection, runtimeTransaction))
        await using (var reader = await identity.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct)) throw new InvalidOperationException("Runtime database identity is unavailable.");
            backendPid = reader.GetInt32(0);
            transactionId = reader.GetInt64(1);
            runtimePrincipal = reader.GetString(2);
        }

        var issuerRaw = Environment.GetEnvironmentVariable("REV869B_COMMAND_ISSUER_CONNECTION");
        if (string.IsNullOrWhiteSpace(issuerRaw))
            throw new InvalidOperationException("A distinct REV869B command issuer connection is required.");
        var issuerBuilder = new NpgsqlConnectionStringBuilder(issuerRaw) { Pooling = false };
        var runtimeBuilder = new NpgsqlConnectionStringBuilder(runtimeConnection.ConnectionString);
        if (!string.Equals(issuerBuilder.Database, runtimeBuilder.Database, StringComparison.Ordinal) ||
            string.Equals(issuerBuilder.Username, runtimeBuilder.Username, StringComparison.Ordinal))
            throw new InvalidOperationException("Command issuer must target the exact database through a principal distinct from runtime.");

        var authenticatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Guid grantId;
        await using (var issuer = new NpgsqlConnection(issuerBuilder.ConnectionString))
        {
            await issuer.OpenAsync(ct);
            await using var issue = new NpgsqlCommand("""
                SELECT nexa.rev869b_issue_command_grant(
                  @runtime,@backend,@transaction,@actor,@issuer,@subject,@role,@organization,@authenticated,@slots::jsonb)
                """, issuer);
            issue.Parameters.AddWithValue("runtime", runtimePrincipal);
            issue.Parameters.AddWithValue("backend", backendPid);
            issue.Parameters.AddWithValue("transaction", transactionId);
            issue.Parameters.AddWithValue("actor", user.EmployeeId!.Value);
            issue.Parameters.AddWithValue("issuer", user.IdentityIssuer!);
            issue.Parameters.AddWithValue("subject", user.IdentitySubject!);
            issue.Parameters.AddWithValue("role", user.RoleCode);
            issue.Parameters.AddWithValue("organization", organization);
            issue.Parameters.AddWithValue("authenticated", authenticatedAt);
            issue.Parameters.AddWithValue("slots", JsonSerializer.Serialize(slots, JsonOptions));
            grantId = (Guid)(await issue.ExecuteScalarAsync(ct)
                ?? throw new InvalidOperationException("Exact command grant was not returned."));

            var executionValue = Environment.GetEnvironmentVariable("REV869B_EXECUTION_INSTANCE_ID");
            if (!Guid.TryParse(executionValue, out var executionInstanceId) || executionInstanceId == Guid.Empty)
                throw new InvalidOperationException("A non-empty REV869B execution-instance ID is required.");
            static byte[] ExactFingerprint(string name)
            {
                var value = Environment.GetEnvironmentVariable(name);
                if (value is null || value.Length != 64 || value.Any(c => !Uri.IsHexDigit(c)))
                    throw new InvalidOperationException(name + " must be an exact SHA-256 fingerprint.");
                return Convert.FromHexString(value);
            }
            var serviceFingerprint = ExactFingerprint("REV869B_SERVICE_INSTANCE_FINGERPRINT");
            var ownershipFingerprint = ExactFingerprint("REV869B_OWNERSHIP_LEASE_FINGERPRINT");
            var businessFingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(slots, JsonOptions)));
            var attemptId = Guid.NewGuid();
            await using var attempt = new NpgsqlCommand(
                "SELECT nexa.rev869b_record_command_consumption_attempt(@grant,@attempt,@execution,@service,@business,@ownership)", issuer);
            attempt.Parameters.AddWithValue("grant", grantId);
            attempt.Parameters.AddWithValue("attempt", attemptId);
            attempt.Parameters.AddWithValue("execution", executionInstanceId);
            attempt.Parameters.AddWithValue("service", serviceFingerprint);
            attempt.Parameters.AddWithValue("business", businessFingerprint);
            attempt.Parameters.AddWithValue("ownership", ownershipFingerprint);
            if (await attempt.ExecuteScalarAsync(ct) is not Guid recordedAttempt || recordedAttempt != attemptId)
                throw new InvalidOperationException("The exact durable consumption attempt was not recorded before context open.");
        }

        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT nexa.rev869b_open_command_context({grantId},{user.EmployeeId.Value},{user.IdentityIssuer},{user.IdentitySubject},{user.RoleCode},{organization},{backendPid},{transactionId})", ct);
        }
        catch
        {
            await RecordRolledBackOutcomeAsync(runtimeConnection, grantId, "Rejected", "ContextOpenRejected", ct);
            throw;
        }
        return grantId;
    }

    public static async Task StageCommittedOutcomeAsync(NexaErpDbContext db, Guid grantId, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Committed command outcome must be staged in the exact business transaction.");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT nexa.rev869b_record_command_outcome({grantId},{"Committed"},{string.Empty})", ct);
    }

    public static async Task RecordRolledBackOutcomeAsync(
        NpgsqlConnection runtimeConnection, Guid grantId, string terminalEvent, string failureCategory, CancellationToken ct)
    {
        if (terminalEvent is not ("Failed" or "Rejected") || string.IsNullOrWhiteSpace(failureCategory))
            throw new InvalidOperationException("A minimized Failed or Rejected outcome category is required.");
        var issuerRaw = Environment.GetEnvironmentVariable("REV869B_COMMAND_ISSUER_CONNECTION");
        if (string.IsNullOrWhiteSpace(issuerRaw))
            throw new InvalidOperationException("A distinct REV869B command issuer connection is required for durable rollback evidence.");
        var issuerBuilder = new NpgsqlConnectionStringBuilder(issuerRaw) { Pooling = false };
        var runtimeBuilder = new NpgsqlConnectionStringBuilder(runtimeConnection.ConnectionString);
        if (!string.Equals(issuerBuilder.Database, runtimeBuilder.Database, StringComparison.Ordinal) ||
            string.Equals(issuerBuilder.Username, runtimeBuilder.Username, StringComparison.Ordinal))
            throw new InvalidOperationException("Rollback outcome issuer must target the exact database through a distinct principal.");
        await using var issuer = new NpgsqlConnection(issuerBuilder.ConnectionString);
        await issuer.OpenAsync(ct);
        await using var outcome = new NpgsqlCommand(
            "SELECT nexa.rev869b_record_command_outcome(@grant,@event,@failure)", issuer);
        outcome.Parameters.AddWithValue("grant", grantId);
        outcome.Parameters.AddWithValue("event", terminalEvent);
        outcome.Parameters.AddWithValue("failure", failureCategory);
        if (Convert.ToInt32(await outcome.ExecuteScalarAsync(ct)) < 1)
            throw new InvalidOperationException("No durable command rollback outcome was appended.");
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
        foreach (var history in db.ChangeTracker.Entries<PurchaseTransactionStatusHistory>()
                     .Where(x => x.State == EntityState.Added).Select(x => x.Entity))
        {
            var version = history.EntityType switch
            {
                "RFQ" => TrackedVersion<RequestForQuotation>(db, history.EntityId) ?? await NextPersistedVersionAsync(db.RequestForQuotations, history.EntityId, ct),
                "RFQInvitation" => TrackedVersion<RfqVendorInvitation>(db, history.EntityId) ?? await NextPersistedVersionAsync(db.RfqVendorInvitations, history.EntityId, ct),
                "VendorQuotation" => TrackedVersion<VendorQuotation>(db, history.EntityId) ?? await NextPersistedVersionAsync(db.VendorQuotations, history.EntityId, ct),
                "TechnicalVerification" => TrackedVersion<QuotationTechnicalVerification>(db, history.EntityId) ?? await NextPersistedVersionAsync(db.QuotationTechnicalVerifications, history.EntityId, ct),
                "CommercialComparison" => TrackedVersion<CommercialComparison>(db, history.EntityId) ?? await NextPersistedVersionAsync(db.CommercialComparisons, history.EntityId, ct),
                "PurchaseOrder" => TrackedVersion<PurchaseOrder>(db, history.EntityId) ?? await NextPersistedVersionAsync(db.PurchaseOrders, history.EntityId, ct),
                "MaterialFollowUp" => TrackedVersion<MaterialFollowUpHandoff>(db, history.EntityId) ?? await NextPersistedVersionAsync(db.MaterialFollowUpHandoffs, history.EntityId, ct),
                _ => throw new InvalidOperationException("Unsupported exact status-history entity type.")
            };
            result.Add(new("purchase_transaction_status_history", history.Id, history.EntityType, history.EntityId,
                history.Action, version, history.FromStatus, history.ToStatus, history.CorrelationId, history.Remarks));
        }

        foreach (var history in db.ChangeTracker.Entries<PurchaseTransactionApprovalHistory>()
                     .Where(x => x.State == EntityState.Added).Select(x => x.Entity))
        {
            var version = TrackedVersion<CommercialComparison>(db, history.CommercialComparisonId) ??
                await NextPersistedVersionAsync(db.CommercialComparisons, history.CommercialComparisonId, ct);
            result.Add(new("purchase_transaction_approval_history", history.Id, "CommercialComparison",
                history.CommercialComparisonId, history.Action, version, history.FromStatus, history.ToStatus,
                history.CorrelationId, history.Remarks));
        }

        foreach (var history in db.ChangeTracker.Entries<PurchaseOrderHistory>()
                     .Where(x => x.State == EntityState.Added).Select(x => x.Entity))
        {
            var version = TrackedVersion<PurchaseOrder>(db, history.PurchaseOrderId) ??
                await NextPersistedVersionAsync(db.PurchaseOrders, history.PurchaseOrderId, ct);
            result.Add(new("purchase_order_history", history.Id, "PurchaseOrder", history.PurchaseOrderId,
                history.Action, version, history.FromStatus, history.ToStatus, history.CorrelationId, history.Reason));
        }

        foreach (var history in db.ChangeTracker.Entries<ControlledConfigurationHistory>()
                     .Where(x => x.State == EntityState.Added && x.Entity.EntityType == nameof(VendorQualification))
                     .Select(x => x.Entity))
        {
            var qualification = db.ChangeTracker.Entries<VendorQualification>()
                .Single(x => x.Entity.Id == history.EntityId).Entity;
            var parentVersion = history.Action == "Create" ? 0L : checked((long)qualification.Version - 1L);
            var from = history.Action switch
            {
                "Approve" => MasterApprovalStatuses.PendingApproval,
                "Reject" => MasterApprovalStatuses.PendingApproval,
                "RequestCorrection" => MasterApprovalStatuses.Approved,
                "Create" => null,
                "Normalize" => MasterApprovalStatuses.Draft,
                _ => MasterApprovalStatuses.PendingApproval
            };
            var to = history.Action switch
            {
                "Verify" => MasterApprovalStatuses.Verified,
                "Approve" => MasterApprovalStatuses.Approved,
                "Reject" => MasterApprovalStatuses.Rejected,
                "RequestCorrection" => MasterApprovalStatuses.RevisionRequested,
                _ => MasterApprovalStatuses.PendingApproval
            };
            result.Add(new("qualification_history", history.Id, nameof(VendorQualification), history.EntityId,
                history.Action, parentVersion, from, to, history.CorrelationId, history.Remarks));
        }

        if (result.GroupBy(x => new { x.ClaimKind, x.EntityType, x.EntityId, x.Operation, x.ParentVersion, x.Correlation })
            .Any(x => x.Count() != 1))
            throw new InvalidOperationException("Duplicate semantic operation slots are prohibited before issuance.");
        return result;
    }

    private static long? TrackedVersion<T>(NexaErpDbContext db, Guid id) where T : AuditableEntity =>
        db.ChangeTracker.Entries<T>().Where(x => x.Entity.Id == id).Select(x => (long?)x.Entity.Version).SingleOrDefault();

    private static Task<long> NextPersistedVersionAsync<T>(IQueryable<T> query, Guid id, CancellationToken ct) where T : AuditableEntity =>
        query.Where(x => x.Id == id).Select(x => checked((long)x.Version + 1L)).SingleAsync(ct);

    private sealed record OperationSlot(
        string ClaimKind,
        Guid HistoryId,
        string EntityType,
        Guid EntityId,
        string Operation,
        long ParentVersion,
        string? FromStatus,
        string ToStatus,
        string Correlation,
        string Remarks);
}
