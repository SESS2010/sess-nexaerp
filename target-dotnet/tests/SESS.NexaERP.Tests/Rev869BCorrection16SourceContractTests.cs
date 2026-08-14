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
        var expected = new[] { "rev869b_reserve_lease", "rev869b_begin_provisioning", "rev869b_mark_ready", "rev869b_mark_in_use", "rev869b_authorize_normal_drop", "rev869b_begin_drop", "rev869b_register_recovery_decision", "rev869b_consume_recovery_decision", "rev869b_record_cleanup_failure", "rev869b_finalize_absent_target", "rev869b_read_lease", "rev869b_read_nonterminal_leases" };
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
        foreach (var set in new[] { "expected_relations", "actual_relations", "relation_delta", "expected_functions", "actual_functions", "function_delta", "expected_exec", "actual_exec", "exec_delta", "direct_table_access" }) Assert.Contains(set, sql);
        Assert.Contains("EXCEPT", sql);
        Assert.Contains("pg_auth_members", sql);
        Assert.DoesNotContain("column_count", sql);
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
