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
        var expected = new[] { "rev869b_reserve_lease", "rev869b_begin_provisioning", "rev869b_mark_ready", "rev869b_mark_in_use", "rev869b_authorize_normal_drop", "rev869b_record_quarantine", "rev869b_begin_drop", "rev869b_register_recovery_decision", "rev869b_consume_recovery_decision", "rev869b_record_cleanup_failure", "rev869b_finalize_absent_target", "rev869b_read_lease", "rev869b_read_nonterminal_leases", "rev869b_read_lifecycle_evidence", "rev869b_read_control_plane_acl_evidence", "rev869b_control_plane_catalogue_fingerprint" };
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
        foreach (var binding in new[] { "rev869b_quarantine_outcomes", "ExecutionInstanceId", "TargetDatabase", "ClusterSystemIdentifier", "SourceState", "ObservedTargetState", "EvidenceKind", "FailureReason", "ActorId", "ActorIssuer", "Operation", "SourceLeaseVersion", "LeaseVersion", "TerminalOutcome", "RegistrationRequestId", "rev869b_begin_quarantine_attempt", "rev869b_quarantine_attempt_binding" })
            Assert.Contains(binding, sql);
        Assert.Contains("TR_rev869b_quarantine_outcomes_immutable", sql);
        var drop = Slice(sql, "CREATE FUNCTION nexa.rev869b_begin_drop", "CREATE FUNCTION nexa.rev869b_register_recovery_decision");
        Assert.Contains("d.AuthorizedAction='DropAndFinalize'", drop);
        Assert.Contains("session_user='nexa_rev869b_lifecycle_api' AND State='DropAuthorized'", drop);
        Assert.Contains("session_user='nexa_rev869b_recovery_executor' AND State='RecoveryAuthorized'", drop);
        Assert.Contains("transition_request_id uuid", drop);
        Assert.Contains("registration_request_id uuid", drop);
        Assert.Contains("transition_request_id=registration_request_id", drop);
        Assert.Contains("rev869b_drop_transition_request_binding", drop);
        Assert.Contains("a.RegistrationRequestId=registration_request_id", drop);
        Assert.Contains("authorization_event nexa.rev869b_database_lease_events%ROWTYPE", drop);
        Assert.Contains("e.LeaseId=lease_id AND e.RequestId=registration_request_id", drop);
        Assert.Contains("e.FromState IN ('Ready','InUse') AND e.ToState='DropAuthorized' AND e.Version=expected_version", drop);
        Assert.Contains("e.AttemptId IS NULL", drop);
        Assert.Contains("e.Principal='nexa_rev869b_lifecycle_api'", drop);
        Assert.Contains("registration_request_id,authorization_event.EvidenceSha256", drop);
        Assert.Contains("l.State='DropAuthorized' AND l.Version=expected_version", drop);
        Assert.Contains("l.TargetDatabase~'^sess_nexaerp_rev869b_[0-9a-f]{24}$'", drop);
        Assert.Contains("m.ClusterSystemIdentifier=l.ClusterSystemIdentifier", drop);
        Assert.Contains("l.TargetManifestSha256~'^[0-9a-f]{64}$'", drop);
        Assert.Contains("rev869b_drop_authorization_event_binding", drop);
        Assert.True(drop.IndexOf("SELECT e.* INTO authorization_event", StringComparison.Ordinal) <
            drop.IndexOf("UPDATE nexa.rev869b_database_leases SET State='DropStarted'", StringComparison.Ordinal));
        Assert.Contains("rev869b_drop_attempt_binding", drop);
        Assert.Contains("lease_id,transition_request_id,attempt_id", drop);
        Assert.DoesNotContain("lease_id,request_id,attempt_id", drop);
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
    public void NormalDropAuthorizationBindingRejectsEveryProvenanceMutation()
    {
        var sql = Source("tools/rev869b-control-plane-install.sql");
        var drop = Slice(sql, "CREATE FUNCTION nexa.rev869b_begin_drop", "CREATE FUNCTION nexa.rev869b_register_recovery_decision");
        static bool Exact(string value) =>
            value.Contains("e.LeaseId=lease_id", StringComparison.Ordinal) &&
            value.Contains("e.RequestId=registration_request_id", StringComparison.Ordinal) &&
            value.Contains("e.AttemptId IS NULL", StringComparison.Ordinal) &&
            value.Contains("e.FromState IN ('Ready','InUse')", StringComparison.Ordinal) &&
            value.Contains("e.ToState='DropAuthorized'", StringComparison.Ordinal) &&
            value.Contains("e.Version=expected_version", StringComparison.Ordinal) &&
            value.Contains("registration_request_id,authorization_event.EvidenceSha256", StringComparison.Ordinal) &&
            value.Contains("e.Principal='nexa_rev869b_lifecycle_api'", StringComparison.Ordinal) &&
            value.Contains("l.State='DropAuthorized'", StringComparison.Ordinal) &&
            value.Contains("l.Version=expected_version", StringComparison.Ordinal) &&
            value.Contains("l.TargetDatabase~'^sess_nexaerp_rev869b_[0-9a-f]{24}$'", StringComparison.Ordinal) &&
            value.Contains("m.ClusterSystemIdentifier=l.ClusterSystemIdentifier", StringComparison.Ordinal) &&
            value.Contains("l.TargetManifestSha256~'^[0-9a-f]{64}$'", StringComparison.Ordinal) &&
            value.Contains("rev869b_drop_authorization_event_binding", StringComparison.Ordinal);
        Assert.True(Exact(drop));

        var mutations = new[]
        {
            drop.Replace("e.RequestId=registration_request_id", "e.RequestId IS NOT NULL", StringComparison.Ordinal),
            drop.Replace("e.LeaseId=lease_id", "e.LeaseId IS NOT NULL", StringComparison.Ordinal),
            drop.Replace("e.Version=expected_version", "e.Version<=expected_version", StringComparison.Ordinal),
            drop.Replace("e.AttemptId IS NULL", "true", StringComparison.Ordinal),
            drop.Replace("e.FromState IN ('Ready','InUse')", "e.FromState IS NOT NULL", StringComparison.Ordinal),
            drop.Replace("e.ToState='DropAuthorized'", "e.ToState IS NOT NULL", StringComparison.Ordinal),
            drop.Replace("registration_request_id,authorization_event.EvidenceSha256", "registration_request_id,evidence", StringComparison.Ordinal),
            drop.Replace("e.Principal='nexa_rev869b_lifecycle_api'", "e.Principal IS NOT NULL", StringComparison.Ordinal),
            drop.Replace("l.State='DropAuthorized'", "l.State IS NOT NULL", StringComparison.Ordinal),
            drop.Replace("l.TargetDatabase~'^sess_nexaerp_rev869b_[0-9a-f]{24}$'", "l.TargetDatabase IS NOT NULL", StringComparison.Ordinal),
            drop.Replace("m.ClusterSystemIdentifier=l.ClusterSystemIdentifier", "m.ClusterSystemIdentifier IS NOT NULL", StringComparison.Ordinal),
            drop.Replace("l.TargetManifestSha256~'^[0-9a-f]{64}$'", "l.TargetManifestSha256 IS NOT NULL", StringComparison.Ordinal)
        };
        Assert.Equal(12, mutations.Length);
        Assert.All(mutations, mutation => Assert.False(Exact(mutation)));
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
