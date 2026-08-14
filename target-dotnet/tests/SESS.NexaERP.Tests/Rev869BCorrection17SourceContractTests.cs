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
    public void TerminalizationReferencesOnlyAuthoritativeColumnsWithExactTypes()
    {
        var attempts = TableColumns(Sql, "rev869b_command_attempts");
        var contexts = TableColumns(Sql, "rev869b_command_contexts");
        var terminal = Slice(Sql, "CREATE FUNCTION nexa.rev869b_record_noncommit_outcome", "CREATE FUNCTION nexa.rev869b_reconcile_command_attempt");
        AssertAliasColumnsExist(terminal, "a", attempts);
        AssertAliasColumnsExist(terminal, "c", contexts);
        var quoted = Convert.ToChar(34).ToString();
        Assert.Contains(quoted + "TargetBackendPid" + quoted + " integer NOT NULL", Sql);
        Assert.Contains(quoted + "TargetTransactionId" + quoted + " bigint NOT NULL", Sql);
        Assert.Contains(quoted + "BackendPid" + quoted + " integer NOT NULL", Sql);
        Assert.Contains(quoted + "TransactionId" + quoted + " bigint NOT NULL", Sql);
        Assert.DoesNotContain("a." + quoted + "OpenedAt" + quoted, terminal);
        Assert.DoesNotContain("a." + quoted + "BackendPid" + quoted, terminal);
        Assert.DoesNotContain("a." + quoted + "TransactionId" + quoted, terminal);
        Assert.Contains("c." + quoted + "BackendPid" + quoted + "=a." + quoted + "TargetBackendPid" + quoted, terminal);
        Assert.Contains("c." + quoted + "TransactionId" + quoted + "=a." + quoted + "TargetTransactionId" + quoted, terminal);
        /* Superseded malformed literals:
        Assert.Contains("\TargetBackendPid\ integer NOT NULL", Sql);
        Assert.Contains("\TargetTransactionId\ bigint NOT NULL", Sql);
        Assert.Contains("\BackendPid\ integer NOT NULL", Sql);
        Assert.Contains("\TransactionId\ bigint NOT NULL", Sql);
        Assert.DoesNotContain("a.\OpenedAt\", terminal);
        Assert.DoesNotContain("a.\BackendPid\", terminal);
        Assert.DoesNotContain("a.\TransactionId\", terminal);
        Assert.Contains("c.\BackendPid\=a.\TargetBackendPid\", terminal);
        Assert.Contains("c.\TransactionId\=a.\TargetTransactionId\", terminal);
        Assert.DoesNotContain("EXECUTE format", terminal, StringComparison.OrdinalIgnoreCase);
        */
        Assert.DoesNotContain("EXECUTE format", terminal, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EXCEPTION WHEN", terminal, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PurgeFreezesCandidatesAndBindsAOneWayMonotonicRetryChain()
    {
        foreach (var relation in new[] { "rev869b_purge_authorizations", "rev869b_purge_attempts", "rev869b_purge_candidates", "rev869b_purge_events" })
            Assert.Contains("CREATE TABLE nexa." + relation, Sql);
        Assert.Contains("CandidateSha256", Sql);
        Assert.Contains("rev869b_purge_candidate_drift", Sql);
        Assert.Contains("deleted<>expected", Sql);
        Assert.DoesNotContain("RetryEligible", Sql);
        Assert.Contains("PriorAttemptId", Sql);
        foreach (var binding in new[] { "RootAuthorizationId", "AuthorizedBatchId", "TargetInstanceSha256", "Operation", "RetryOrdinal", "PriorTerminalOutcome", "PriorEvidenceSha256", "UX_rev869b_purge_authorizations_prior_attempt" })
            Assert.Contains(binding, Sql);
        Assert.Contains("FK_rev869b_purge_authorizations_prior_attempt", Sql);
        Assert.Contains("FK_rev869b_purge_authorizations_root", Sql);
        Assert.Contains("scope!~'^organization:", Sql);
        Assert.Contains("approved_organization", Sql);
        Assert.Contains("Failed','Interrupted", Sql);
        Assert.Contains("retry_ordinal:=prior_ordinal+1", Sql);
        Assert.Contains(Convert.ToChar(34) + "AuthorizedBatchId" + Convert.ToChar(34) + "=purge_attempt_id", Sql);
        /* Superseded malformed literal:
        Assert.Contains("\AuthorizedBatchId\=purge_attempt_id", Sql);
        */
        Assert.Contains("rev869b_purge_batch_binding", Sql);
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
        Assert.Contains("REVOKE ALL ON ALL SEQUENCES IN SCHEMA nexa FROM PUBLIC", Sql);
        Assert.Contains("ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_security_owner", Sql);
        foreach (var closure in new[] { "read_relation", "write_relation", "checked_privilege", "Target relation ACL mismatch", "Target sequence ACL mismatch", "Target object ownership mismatch", "Target default ACL mismatch", "Target role membership mismatch", "Target role capability mismatch" })
            Assert.Contains(closure, Sql);
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
    public void AcceptanceInventoryHasExactlyThirtyFourUniqueExecutableDatabaseFacts()
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
            Assert.False(string.IsNullOrWhiteSpace(contract.ExpectedTerminalOutcome));
            Assert.Equal("Finalized", contract.ExpectedCleanupOutcome);
            Assert.False(string.IsNullOrWhiteSpace(contract.ExpectedIdentity.Schema));
            Assert.False(string.IsNullOrWhiteSpace(contract.ExpectedIdentity.Table));
            Assert.False(string.IsNullOrWhiteSpace(contract.ExpectedIdentity.Function));
            Assert.True(contract.ExpectedBeforeCount >= 0);
            Assert.True(contract.ExpectedAfterCount >= 0);
            if (contract.RequiresDenial) { Assert.NotNull(contract.ExpectedSqlState); Assert.NotNull(contract.ExpectedDatabaseObject); }
            else Assert.True(contract.ExpectedAffectedRows > 0 || contract.AllowsZeroRowsTerminal);
        });
        var bodies = Source("tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs");
        Assert.Equal(34, Regex.Matches(bodies, @"\[Fact\]").Count);
        Assert.Equal(32, Regex.Matches(bodies, @"=> RunAsync\(Rev869BAcceptanceScenarioInventory\.[A-Z][0-9]{2}\)").Count);
        Assert.Equal(3, Regex.Matches(bodies, @"controller\.AllocateAsync\(").Count);
        Assert.Equal(3, Regex.Matches(bodies, @"controller\.ReleaseAsync\(").Count);
        Assert.DoesNotContain("File.ReadAllText", bodies);
        Assert.DoesNotContain("Assert.ThrowsAny", bodies);
        var client = Source("tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs");
        foreach (var pin in new[] { "REV869B_EXPECTED_SOURCE_COMMIT", "REV869B_EXPECTED_MANIFEST_SHA256", "REV869B_EXPECTED_TLS_SPKI_SHA256", "REV869B_EXPECTED_CLUSTER_SYSTEM_IDENTIFIER", "REV869B_CONTROLLER_SIGNING_PUBLIC_KEY_PEM", "VerifyData", "ContractSha256", "CommandId", "AuthorizationId", "DurableEvidenceId", "BeforeSha256", "AfterSha256", "DatabaseIdentity", "TerminalOutcome", "CleanupOutcome" })
            Assert.Contains(pin, client);
        Assert.DoesNotContain("ReadFromJsonAsync<AcceptanceEvidence>", client);
    }

    private static HashSet<string> TableColumns(string sql, string table)
    {
        var definition = Slice(sql, "CREATE TABLE nexa." + table + "(", ");");
        return Regex.Matches(definition, Convert.ToChar(34) + "(?<name>[A-Za-z][A-Za-z0-9]*)" + Convert.ToChar(34))
            .Select(match => match.Groups["name"].Value).ToHashSet(StringComparer.Ordinal);
        /* Correction 20 malformed literal retained only inside this compile-time comment.
        return Regex.Matches(definition, "\(?<name>[A-Za-z][A-Za-z0-9]*)\")
            .Select(match => match.Groups["name"].Value).ToHashSet(StringComparer.Ordinal); */
    }

    private static void AssertAliasColumnsExist(string sql, string alias, HashSet<string> authoritativeColumns)
    {
        var references = Regex.Matches(sql, Regex.Escape(alias) + "\\." + Convert.ToChar(34) + "(?<name>[A-Za-z][A-Za-z0-9]*)" + Convert.ToChar(34))
            .Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal).ToArray();
        /* Correction 20 malformed literal retained only inside this compile-time comment.
        var references = Regex.Matches(sql, Regex.Escape(alias) + "\\.\(?<name>[A-Za-z][A-Za-z0-9]*)\")
            .Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal).ToArray(); */
        Assert.NotEmpty(references);
        Assert.All(references, column => Assert.Contains(column, authoritativeColumns));
    }

    private static string Slice(string value, string start, string end) => value[value.IndexOf(start, StringComparison.Ordinal)..value.IndexOf(end, value.IndexOf(start, StringComparison.Ordinal), StringComparison.Ordinal)];

    private static string Source(string relative) => File.ReadAllText(Path.Combine(FindRoot(), relative.Replace('/', Path.DirectorySeparatorChar)));
    private static string FindRoot() { for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent) if (File.Exists(Path.Combine(d.FullName, "SESS.NexaERP.slnx"))) return d.FullName; throw new DirectoryNotFoundException(); }
}
