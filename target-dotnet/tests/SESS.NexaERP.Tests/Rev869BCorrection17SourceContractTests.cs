using System.Text.RegularExpressions;

namespace SESS.NexaERP.Tests;

public sealed class Rev869BCorrection17SourceContractTests
{
    private static readonly string Sql = Source("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs");
    private static readonly string Authorizer = Source("src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs");

    [Fact]
    public void CommandRequestKeyAttemptOrdinalActiveAttemptAndReceiptAreDatabaseEnforced()
    {
        Assert.Contains("UNIQUE(\"OrganizationId\",\"Operation\",\"IdempotencyKeySha256\")", Sql);
        Assert.Contains("UNIQUE(\"CommandId\",\"AttemptOrdinal\")", Sql);
        Assert.Contains("UX_rev869b_one_active_command_attempt", Sql);
        Assert.Contains("rev869b_command_request_replay_mismatch", Sql);
        Assert.Contains("rev869b_command_receipts", Sql);
        Assert.Contains("rev869b_command_claim_coverage", Sql);
        Assert.True(Sql.IndexOf("rev869b_command_receipts VALUES", StringComparison.Ordinal) < Sql.IndexOf("rev869b_command_attempt_outcomes VALUES(gen_random_uuid(),attempt_id,'Committed'", StringComparison.Ordinal));
    }

    [Fact]
    public void CallerIdempotencyAndRequestFingerprintReplaceProcessGlobalKey()
    {
        Assert.Contains("CommandEnvelope", Authorizer);
        Assert.Contains("envelope.IdempotencyKey", Authorizer);
        Assert.Contains("envelope.RequestFingerprint", Authorizer);
        Assert.DoesNotContain("REV869B_COMMAND_IDEMPOTENCY_KEY", Authorizer + Sql);
        Assert.True(Authorizer.IndexOf("rev869b_register_command_request", StringComparison.Ordinal) < Authorizer.IndexOf("rev869b_start_command_attempt", StringComparison.Ordinal));
        Assert.True(Authorizer.IndexOf("rev869b_start_command_attempt", StringComparison.Ordinal) < Authorizer.IndexOf("rev869b_open_command_attempt", StringComparison.Ordinal));
    }

    [Fact]
    public void CommitIsInsideBusinessTransactionAndNoncommitUsesAuditPrincipal()
    {
        var service = Source("src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs");
        Assert.True(service.IndexOf("StageCommittedReceiptAsync", StringComparison.Ordinal) < service.IndexOf("owned.CommitAsync", StringComparison.Ordinal));
        Assert.True(service.IndexOf("owned.RollbackAsync", StringComparison.Ordinal) < service.IndexOf("RecordRolledBackOutcomesAsync", StringComparison.Ordinal));
        Assert.Contains("REV869B_COMMAND_AUDIT_CONNECTION", Authorizer);
        Assert.Contains("Rejected\" or \"RolledBack\" or \"Abandoned", Authorizer);
        Assert.DoesNotContain("StageCommittedOutcomeAsync", service + Authorizer);
    }

    [Fact]
    public void PurgeFreezesCandidatesAndHasNoRetryEligibleState()
    {
        foreach (var relation in new[] { "rev869b_purge_authorizations", "rev869b_purge_attempts", "rev869b_purge_candidates", "rev869b_purge_events" })
            Assert.Contains("CREATE TABLE nexa." + relation, Sql);
        Assert.Contains("CandidateSha256", Sql);
        Assert.Contains("rev869b_purge_candidate_drift", Sql);
        Assert.Contains("deleted<>expected", Sql);
        Assert.DoesNotContain("RetryEligible", Sql);
        Assert.Contains("PriorAttemptId", Sql);
    }

    [Fact]
    public void ExportMaterializesImmutableRowsBeforeAuditedRelease()
    {
        foreach (var relation in new[] { "rev869b_export_authorizations", "rev869b_export_batches", "rev869b_export_batch_rows", "rev869b_export_releases" })
            Assert.Contains("CREATE TABLE nexa." + relation, Sql);
        Assert.Contains("rev869b_register_export_authorization", Sql);
        Assert.Contains("session_user<>'nexa_rev869b_management_writer'", Sql);
        Assert.Contains("TR_rev869b_export_rows_immutable", Sql);
        Assert.Contains("rev869b_prepare_export_batch", Sql);
        Assert.Contains("rev869b_authorize_export_release", Sql);
        Assert.Contains("rev869b_read_prepared_export_batch", Sql);
        Assert.Contains("Delivered','Failed','Interrupted", Sql);
        var endpoints = Source("src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs");
        Assert.DoesNotContain("/export\"", endpoints);
        Assert.DoesNotContain("ExportComparison", endpoints);
        Assert.DoesNotContain("ExportPo", endpoints);
    }

    [Fact]
    public void FrozenRoleFunctionsHaveExactPurposeGrantsAndNoDirectLedgerDml()
    {
        var roles = new[] { "nexa_rev869b_app_runtime", "nexa_rev869b_command_audit", "nexa_rev869b_management_writer", "nexa_rev869b_purge_worker", "nexa_rev869b_export_service", "nexa_rev869b_target_verifier" };
        Assert.All(roles, role => Assert.Contains(role, Sql));
        Assert.Contains("REVOKE ALL ON ALL TABLES IN SCHEMA nexa FROM PUBLIC", Sql);
        Assert.Contains("REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA nexa FROM PUBLIC", Sql);
        var dmlGrants = Regex.Matches(Sql, @"GRANT\s+(?:SELECT|INSERT|UPDATE|DELETE|TRUNCATE)[^;]+TO\s+(?<roles>[^;]+);", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        Assert.Contains(dmlGrants.Cast<Match>(), grant => grant.Groups["roles"].Value.Contains("nexa_rev869b_app_runtime", StringComparison.Ordinal));
        foreach (Match grant in dmlGrants)
        {
            var grantees = grant.Groups["roles"].Value;
            Assert.DoesNotContain("nexa_rev869b_command_audit", grantees);
            Assert.DoesNotContain("nexa_rev869b_management_writer", grantees);
            Assert.DoesNotContain("nexa_rev869b_purge_worker", grantees);
            Assert.DoesNotContain("nexa_rev869b_export_service", grantees);
            Assert.DoesNotContain("nexa_rev869b_target_verifier", grantees);
            if (grantees.Contains("nexa_rev869b_app_runtime", StringComparison.Ordinal))
                Assert.DoesNotContain("nexa.rev869b_", grant.Value);
        }
    }

    private static string Source(string relative) => File.ReadAllText(Path.Combine(FindRoot(), relative.Replace('/', Path.DirectorySeparatorChar)));
    private static string FindRoot() { for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent) if (File.Exists(Path.Combine(d.FullName, "SESS.NexaERP.slnx"))) return d.FullName; throw new DirectoryNotFoundException(); }
}
