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
        var terminal = Slice(Sql, "CREATE FUNCTION nexa.rev869b_record_noncommit_outcome", "CREATE FUNCTION nexa.rev869b_reconcile_command_attempt");
        foreach (var binding in new[] { "ExecutionInstanceId", "ServiceInstanceSha256", "OwnershipLeaseSha256", "pg_stat_activity", "backend_xid", "rev869b_noncommit_replay_mismatch", "rev869b_noncommit_terminalizer_binding" })
            Assert.Contains(binding, terminal);
        Assert.DoesNotContain("ON CONFLICT(", terminal, StringComparison.Ordinal);
        Assert.Contains("ExecutionInstanceId", Authorizer);
        Assert.Contains("DeterministicOutcomeId", Authorizer);
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
        Assert.Contains("FK_rev869b_purge_authorizations_prior_attempt", Sql);
        Assert.Contains("scope!~'^organization:", Sql);
        Assert.Contains("approved_organization", Sql);
        Assert.Contains("Failed','Interrupted", Sql);
        var grants = Slice(Sql, "GRANT EXECUTE ON FUNCTION nexa.rev869b_start_purge", "GRANT EXECUTE ON FUNCTION nexa.rev869b_prepare_export_batch");
        Assert.DoesNotContain("rev869b_record_purge_failure(uuid,text,text,bytea) TO nexa_rev869b_purge_worker", grants);
        Assert.Contains("rev869b_record_purge_failure(uuid,text,text,bytea),nexa.rev869b_reconcile_purge(uuid) TO nexa_rev869b_purge_audit", grants);
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
        Assert.Contains("field.key=ANY", Sql);
        Assert.Contains("UX_rev869b_one_active_export_release", Sql);
        Assert.Contains("clock_timestamp()", Sql);
        Assert.Contains("NOT IN ('Failed','Interrupted')", Sql);
        Assert.Contains("rev869b_verify_target_catalogue_acl", Sql);
        foreach (var category in new[] { "relation|", "column|", "constraint|", "index|", "trigger|", "function|", "defaultacl|" }) Assert.Contains(category, Sql);
        var endpoints = Source("src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs");
        Assert.DoesNotContain("/export\"", endpoints);
        Assert.DoesNotContain("ExportComparison", endpoints);
        Assert.DoesNotContain("ExportPo", endpoints);
    }

    [Fact]
    public void FrozenRoleFunctionsHaveExactPurposeGrantsAndNoDirectLedgerDml()
    {
        var roles = new[] { "nexa_rev869b_app_runtime", "nexa_rev869b_command_audit", "nexa_rev869b_management_writer", "nexa_rev869b_purge_worker", "nexa_rev869b_purge_audit", "nexa_rev869b_export_service", "nexa_rev869b_target_verifier" };
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
            Assert.DoesNotContain("nexa_rev869b_purge_audit", grantees);
            Assert.DoesNotContain("nexa_rev869b_export_service", grantees);
            Assert.DoesNotContain("nexa_rev869b_target_verifier", grantees);
            if (grantees.Contains("nexa_rev869b_app_runtime", StringComparison.Ordinal))
                Assert.DoesNotContain("nexa.rev869b_", grant.Value);
        }
    }

    [Fact]
    public void AcceptanceInventoryHasExactlyThirtyFourUniqueExecutablePostgresFacts()
    {
        var inventory = Rev869BAcceptanceScenarioInventory.All;
        Assert.Equal(34, inventory.Count);
        Assert.Equal(34, inventory.Select(x => x.ScenarioId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(new[] { "P01", "P02", "P03", "L01", "L02", "L03", "L04", "L05", "R01", "R02", "R03", "C01", "C02", "C03", "C04", "C05", "C06", "C07", "C08", "G01", "G02", "G03", "G04", "G05", "G06", "E01", "E02", "E03", "E04", "A01", "A02", "T01", "T02", "T03" }, inventory.Select(x => x.ScenarioId));
        Assert.All(inventory, contract =>
        {
            Assert.False(string.IsNullOrWhiteSpace(contract.Setup));
            Assert.False(string.IsNullOrWhiteSpace(contract.Action));
            Assert.False(string.IsNullOrWhiteSpace(contract.ExpectedInitialState));
            Assert.False(string.IsNullOrWhiteSpace(contract.ExpectedFinalState));
            if (contract.RequiresDenial) { Assert.NotNull(contract.ExpectedSqlState); Assert.NotNull(contract.ExpectedDatabaseObject); }
            else Assert.True(contract.ExpectedAffectedRows > 0 || contract.AllowsZeroRowsTerminal);
        });
        var bodies = Source("tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs");
        Assert.Equal(34, Regex.Matches(bodies, @"\[Fact\]\s+public Task").Count);
        Assert.Equal(34, Regex.Matches(bodies, @"=> RunAsync\(Rev869BAcceptanceScenarioInventory\.[A-Z][0-9]{2}\)").Count);
        Assert.DoesNotContain("File.ReadAllText", bodies);
        Assert.DoesNotContain("Assert.ThrowsAny", bodies);
    }

    private static string Slice(string value, string start, string end) => value[value.IndexOf(start, StringComparison.Ordinal)..value.IndexOf(end, value.IndexOf(start, StringComparison.Ordinal), StringComparison.Ordinal)];

    private static string Source(string relative) => File.ReadAllText(Path.Combine(FindRoot(), relative.Replace('/', Path.DirectorySeparatorChar)));
    private static string FindRoot() { for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent) if (File.Exists(Path.Combine(d.FullName, "SESS.NexaERP.slnx"))) return d.FullName; throw new DirectoryNotFoundException(); }
}
