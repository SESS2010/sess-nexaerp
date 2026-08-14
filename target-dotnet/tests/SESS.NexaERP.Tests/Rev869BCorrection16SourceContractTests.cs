using System.Text.RegularExpressions;

namespace SESS.NexaERP.Tests;

public sealed class Rev869BCorrection16SourceContractTests
{
    [Fact]
    public void ExternalProvisioningHelperHasOnlyOfflinePlanAndReadOnlyModes()
    {
        var helper = Source("tools/manage-rev869b-control-plane-secure.ps1");
        var modes = Regex.Match(helper, @"ValidateSet\((?<m>[^)]*)\)").Groups["m"].Value;
        Assert.Contains("GeneratePlanOnly", modes);
        Assert.Contains("PreflightOnly", modes);
        Assert.Contains("PostProvisionVerification", modes);
        Assert.DoesNotContain("ProvisionAuthorized", helper);
        Assert.DoesNotContain("RollbackAuthorized", helper);
        Assert.DoesNotContain("Bootstrap", helper);
        Assert.DoesNotContain("Deprovision", helper);
        Assert.False(File.Exists(Path.Combine(Root, "tools", "rev869b-control-plane-bootstrap.sql")));
        Assert.False(File.Exists(Path.Combine(Root, "tools", "rev869b-control-plane-deprovision.sql")));
    }

    [Fact]
    public void PreflightVerifiesExternallyProvisionedIdentityWithoutClusterMutation()
    {
        var sql = Source("tools/rev869b-control-plane-preflight.sql");
        foreach (var required in new[] { "pg_control_system().system_identifier", "expected_server_address", "expected_manifest_sha256", "expected_source_commit", "nexa_rev869b_lifecycle_administrator", "nexa_rev869b_management_writer" }) Assert.Contains(required, sql);
        Assert.DoesNotMatch(new Regex(@"(?im)^\s*(CREATE|ALTER|DROP|GRANT|REVOKE|INSERT|UPDATE|DELETE)\b"), sql);
    }

    [Fact]
    public void ControlPlaneFunctionsArePurposeSpecificAndNoGenericTransitionExists()
    {
        var sql = Source("tools/rev869b-control-plane-install.sql");
        var functions = Regex.Matches(sql, @"CREATE FUNCTION nexa\.(?<name>[a-z0-9_]+)\(").Select(x => x.Groups["name"].Value).ToHashSet(StringComparer.Ordinal);
        var expected = new[] { "rev869b_reserve_lease", "rev869b_begin_provisioning", "rev869b_mark_ready", "rev869b_mark_in_use", "rev869b_authorize_normal_drop", "rev869b_record_quarantine", "rev869b_begin_drop", "rev869b_register_recovery_decision", "rev869b_consume_recovery_decision", "rev869b_record_cleanup_failure", "rev869b_finalize_absent_target", "rev869b_read_lease", "rev869b_read_nonterminal_leases", "rev869b_control_plane_catalogue_fingerprint" };
        Assert.All(expected, name => Assert.Contains(name, functions));
        Assert.DoesNotContain("rev869b_transition_database_lease", functions);
        Assert.Contains("session_user", sql);
        Assert.DoesNotContain("issuer_authority", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LifecycleStateConstraintMatchesTheFrozenGraphAndFinalizerIsIdempotent()
    {
        var sql = Source("tools/rev869b-control-plane-install.sql");
        var states = Regex.Match(sql, @"State IN \((?<states>[^)]*)\)").Groups["states"].Value;
        foreach (var state in new[] { "Reserved", "Provisioning", "Ready", "InUse", "DropAuthorized", "DropStarted", "Quarantined", "RecoveryAuthorized", "CleanupFailed", "Finalized" }) Assert.Contains("'" + state + "'", states);
        Assert.DoesNotContain("'Dropped'", states);
        var finalizer = Slice(sql, "CREATE FUNCTION nexa.rev869b_finalize_absent_target", "CREATE FUNCTION nexa.rev869b_read_lease");
        Assert.Contains("Finalizer replay evidence mismatch", finalizer);
        Assert.Contains("RETURN existing.OutcomeId", finalizer);
        Assert.Contains("State='Finalized'", finalizer);
    }

    [Fact]
    public void CanonicalVerifierComparesCompleteObjectAndEffectiveAclSets()
    {
        var sql = Source("tools/rev869b-control-plane-verify.sql");
        foreach (var set in new[] { "expected_relations", "actual_relations", "relation_delta", "expected_functions", "actual_functions", "function_delta", "expected_exec", "actual_exec", "exec_delta", "direct_relation_access", "direct_sequence_access", "unexpected_database_access", "expected_database_denial", "schema_access_mismatch" }) Assert.Contains(set, sql);
        Assert.Contains("EXCEPT", sql);
        Assert.Contains("pg_auth_members", sql);
        foreach (var fact in new[] { "relation|", "column|", "constraint|", "index|", "trigger|", "function|", "schema|", "defaultacl|", "p.prosrc", "p.proacl", "relacl" })
            Assert.Contains(fact, Source("tools/rev869b-control-plane-install.sql"));
        Assert.DoesNotContain("column_count", sql);
    }

    [Fact]
    public void ExactInventoryModelRejectsAddedRemovedChangedAndDuplicateFacts()
    {
        var canonical = new[] { "column|LeaseId|uuid", "constraint|lease_pk", "function|reserve|sha256:a" };
        Assert.True(Rev869BControlPlaneRegistry.IsExactSetMatch(canonical, canonical));
        Assert.False(Rev869BControlPlaneRegistry.IsExactSetMatch(canonical, canonical.Append("acl|arbitrary|SELECT")));
        Assert.False(Rev869BControlPlaneRegistry.IsExactSetMatch(canonical, canonical.Skip(1)));
        Assert.False(Rev869BControlPlaneRegistry.IsExactSetMatch(canonical, canonical.Select(x => x.Replace("sha256:a", "sha256:b", StringComparison.Ordinal))));
        Assert.False(Rev869BControlPlaneRegistry.IsExactSetMatch(canonical, canonical.Append(canonical[0])));
    }

    [Fact]
    public void QuarantineRecoveryActionAndTerminalReplayAreDatabaseBound()
    {
        var sql = Source("tools/rev869b-control-plane-install.sql");
        var quarantine = Slice(sql, "CREATE FUNCTION nexa.rev869b_record_quarantine", "CREATE FUNCTION nexa.rev869b_begin_drop");
        Assert.Contains("State IN ('Reserved','Provisioning','Ready','InUse')", quarantine);
        Assert.Contains("TerminalState='Quarantined'", quarantine);
        Assert.Contains("Quarantine replay evidence mismatch", quarantine);
        foreach (var binding in new[] { "rev869b_quarantine_outcomes", "ExecutionInstanceId", "TargetDatabase", "ClusterSystemIdentifier", "SourceState", "ObservedTargetState", "EvidenceKind", "FailureReason", "ActorId", "ActorIssuer", "Operation", "LeaseVersion", "TerminalOutcome", "rev869b_quarantine_attempt_binding" })
            Assert.Contains(binding, sql);
        Assert.Contains("TR_rev869b_quarantine_outcomes_immutable", sql);
        var drop = Slice(sql, "CREATE FUNCTION nexa.rev869b_begin_drop", "CREATE FUNCTION nexa.rev869b_register_recovery_decision");
        Assert.Contains("d.AuthorizedAction='DropAndFinalize'", drop);
        Assert.Contains("session_user='nexa_rev869b_lifecycle_api' AND State='DropAuthorized'", drop);
        Assert.Contains("session_user='nexa_rev869b_recovery_executor' AND State='RecoveryAuthorized'", drop);
        var failure = Slice(sql, "CREATE FUNCTION nexa.rev869b_record_cleanup_failure", "CREATE FUNCTION nexa.rev869b_finalize_absent_target");
        Assert.Contains("RETURN existing.OutcomeId", failure);
        Assert.Contains("Cleanup failure replay evidence mismatch", failure);
        var finalizer = Slice(sql, "CREATE FUNCTION nexa.rev869b_finalize_absent_target", "CREATE FUNCTION nexa.rev869b_read_lease");
        Assert.Contains("action='FinalizeAbsent' AND lease_state<>'RecoveryAuthorized'", finalizer);
        Assert.Contains("action='DropAndFinalize' AND lease_state<>'DropStarted'", finalizer);
        var recovery = Slice(sql, "CREATE FUNCTION nexa.rev869b_consume_recovery_decision", "CREATE FUNCTION nexa.rev869b_record_cleanup_failure");
        Assert.Contains("TerminalState='Interrupted'", recovery);
        Assert.Contains("rev869b_recovery_attempt_freshness", recovery);
        Assert.DoesNotMatch(new Regex(@"\bON\s+CONFLICT\b", RegexOptions.IgnoreCase), recovery);
    }

    [Fact]
    public void PreflightRejectsUnexpectedPackageRolesAndWrongCapabilities()
    {
        var sql = Source("tools/rev869b-control-plane-preflight.sql");
        Assert.Contains("r.rolname LIKE 'nexa_rev869b_%'", sql);
        Assert.Contains("role_mismatch", sql);
        Assert.Contains("rolsuper OR r.rolreplication OR r.rolbypassrls OR r.rolinherit", sql);
        foreach (var role in Rev869BControlPlaneProvisioningContract.Roles.Concat(Rev869BControlPlaneProvisioningContract.TargetRoles))
            Assert.Contains(role, sql);
    }

    [Fact]
    public void RollbackIsSchemaOnlyAndGatedOnEveryLeaseFinalized()
    {
        var sql = Source("tools/rev869b-control-plane-rollback.sql");
        Assert.Contains("session_user<>'nexa_rev869b_lifecycle_administrator'", sql);
        Assert.Contains("WHERE State<>'Finalized'", sql);
        Assert.Contains("DROP SCHEMA nexa CASCADE", sql);
        Assert.DoesNotContain("DROP DATABASE", sql);
        Assert.DoesNotContain("DROP ROLE", sql);
    }

    private static string Slice(string value, string start, string end) => value[value.IndexOf(start, StringComparison.Ordinal)..value.IndexOf(end, value.IndexOf(start, StringComparison.Ordinal), StringComparison.Ordinal)];
    private static string Source(string relative) => File.ReadAllText(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
    private static readonly string Root = FindRoot();
    private static string FindRoot() { for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent) if (File.Exists(Path.Combine(d.FullName, "SESS.NexaERP.slnx"))) return d.FullName; throw new DirectoryNotFoundException(); }
}
