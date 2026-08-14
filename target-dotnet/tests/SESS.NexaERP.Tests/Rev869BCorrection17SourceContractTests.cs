namespace SESS.NexaERP.Tests;

public sealed class Rev869BCorrection17SourceContractTests
{
    private static readonly string Root = FindRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(
        Root, path.Replace('/', Path.DirectorySeparatorChar)));

    [Fact]
    public void ExecutableProvisioningPackageIsCompleteAndCredentialFree()
    {
        var texts = new[] {
            Read("tools/rev869b-control-plane-bootstrap.sql"),
            Read("tools/rev869b-control-plane-install.sql"),
            Read("tools/rev869b-control-plane-verify.sql"),
            Read("tools/rev869b-control-plane-rollback.sql"),
            Read("tools/manage-rev869b-control-plane-secure.ps1") };
        foreach (var text in texts.Take(4))
        {
            Assert.NotEmpty(text);
            Assert.DoesNotContain("client_secret", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BEGIN PRIVATE KEY", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Bearer ", text, StringComparison.OrdinalIgnoreCase);
        }
        var helper = texts[4];
        foreach (var mode in new[] { "GeneratePlanOnly", "PreflightOnly", "ProvisionAuthorized",
                     "PostProvisionVerification", "RollbackAuthorized" })
            Assert.Contains(mode, helper);
        Assert.Contains("--no-password", helper);
        Assert.Contains("Get-FileHash", helper);
    }

    [Fact]
    public void ProvisioningDefinesExactRolesObjectsOwnershipAndEffectiveAclFailure()
    {
        var install = Read("tools/rev869b-control-plane-install.sql");
        var verify = Read("tools/rev869b-control-plane-verify.sql");
        foreach (var role in new[] { "control_plane_owner", "control_plane_api", "control_plane_issuer",
                     "control_plane_audit_writer", "recovery_administrator", "purge_authorizer",
                     "purge_executor", "verifier" })
            Assert.Contains("nexa_rev869b_" + role, install + verify);
        foreach (var relation in Rev869BControlPlaneProvisioningContract.Relations)
            Assert.Contains("CREATE TABLE nexa." + relation.Name, install);
        Assert.Contains("ALTER DEFAULT PRIVILEGES", install);
        Assert.Contains("REVOKE EXECUTE ON ALL FUNCTIONS", install);
        Assert.Contains("NOT has_database_privilege('public'", install);
        Assert.Contains("unexpected_relations", verify);
        Assert.Contains("pg_auth_members", verify);
        Assert.Contains("rev869b_verify_exact_control_plane", verify);
        Assert.Contains("i.relkind='i')=20", install);
        Assert.Contains("i.relkind='i')=20", verify);
        Assert.Contains("Rollback refused while non-finalized leases exist",
            Read("tools/rev869b-control-plane-rollback.sql"));
    }

    [Fact]
    public void LifecycleIsRegistryFirstExactAndResumable()
    {
        var lease = Read("tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs");
        var install = Read("tools/rev869b-control-plane-install.sql");
        Assert.True(lease.IndexOf("ReserveBeforeCreateAsync(reservation)", StringComparison.Ordinal) <
                    lease.IndexOf("WriteEvidenceAsync(new QuarantineEvidence", StringComparison.Ordinal));
        Assert.Contains("TryReadVerifiedEvidenceAsync", lease);
        Assert.Contains("recovery post-drop reconciliation", lease);
        foreach (var state in new[] { "PreCreate", "Created", "Provisioned", "Executing", "Failed",
                     "Quarantined", "CleanupAuthorized", "DropStarted", "Dropped", "CleanupFailed", "Finalized" })
            Assert.Contains("'" + state + "'", install);
        Assert.Contains("rev869b_read_drop_started_attempt", install);
        Assert.Contains("rev869b_control_plane_changed_or_substituted_state", install);
    }

    [Fact]
    public void PurgeEligibilityDurabilityAndForeignKeysCoexist()
    {
        var sql = Read("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs");
        Assert.Contains("ARRAY['Expired','Committed','Failed','Rejected']", sql);
        Assert.Contains("nexa_rev869b_purge_audit_writer", sql);
        Assert.DoesNotContain("\"GrantId\" uuid NOT NULL REFERENCES nexa.rev869b_command_grants", sql);
        Assert.Contains("CandidateSetFingerprint", sql);
        Assert.Contains("MaximumPermittedRows", sql);
        Assert.Contains("MGMT-REV869B-SECURITY-LEDGER-20260813-001", sql);
    }

    [Fact]
    public void DurableAttemptsAreDatabaseSequencedMandatoryImmutableAndTerminal()
    {
        var sql = Read("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs");
        var app = Read("src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs");
        var direct = Read("tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs");
        Assert.Contains("GENERATED ALWAYS AS IDENTITY", sql);
        Assert.DoesNotContain("business_command_fingerprint,ownership_lease_fingerprint,1,clock_timestamp()", sql);
        Assert.Contains("rev869b_command_attempt_outcomes", sql);
        Assert.Contains("INTO STRICT durable_attempt_id", sql);
        Assert.Contains("FK_rev869b_command_context_attempt", sql);
        Assert.Contains("rev869b_record_command_attempt_outcome", app);
        Assert.Contains("rev869b_record_command_consumption_attempt", direct);
    }

    [Fact]
    public void RuntimeAndPurgeRolesCannotReadOrFabricateDurableLedger()
    {
        var lease = Read("tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs");
        var sql = Read("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs");
        Assert.DoesNotContain("GRANT SELECT,INSERT,UPDATE,DELETE ON ALL TABLES", lease);
        Assert.Contains("rev869b_command_consumption_attempt_audits,nexa.rev869b_command_attempt_outcomes", lease);
        Assert.Contains("REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA nexa FROM nexa_rev869b_purge_executor", sql);
        Assert.Contains("TO nexa_rev869b_purge_audit_writer", sql);
        Assert.Contains("rev869b_security_export_authorizations", sql);
        Assert.Contains("rev869b_security_export_audits", sql);
        Assert.Contains("rev869b_register_security_export_authorization", sql);
        Assert.Contains("rev869b_export_minimized_security_ledger", sql);
        Assert.Contains("nexa_rev869b_security_export_authorizer", sql);
        Assert.Contains("nexa_rev869b_security_export_reader", sql);
    }

    [Fact]
    public void TwentyFiveFutureScenariosUseRealFixturesConcurrencyAndExactPaths()
    {
        var facts = Read("tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs");
        var harness = Read("tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs");
        Assert.Equal(25, facts.Split("[Fact]", StringSplitOptions.None).Length - 1);
        Assert.Contains("Task.WhenAll", harness);
        Assert.Contains("TaskCompletionSource", harness);
        Assert.Contains("InsertExpiredGrantFixtureAsync", harness);
        Assert.Contains("CREATE TRIGGER", harness);
        Assert.Contains("Assert.Equal(\"42501\"", harness);
        Assert.Contains("rev869b_command_attempt_outcomes", harness);
        Assert.Contains("rev869b_export_minimized_security_ledger", harness);
        Assert.DoesNotContain("SET LOCAL nexa.rev869b_test_failure", harness);
        Assert.DoesNotContain("00000000-0000-0000-0000-", harness);
    }

    [Fact]
    public void ProvisioningHelperGeneratePlanIsOfflineByConstruction()
    {
        var helper = Read("tools/manage-rev869b-control-plane-secure.ps1");
        var generate = helper[helper.LastIndexOf("'GeneratePlanOnly'", StringComparison.Ordinal)..];
        generate = generate[..generate.IndexOf("'PreflightOnly'", StringComparison.Ordinal)];
        Assert.DoesNotContain("Invoke-PsqlFile", generate);
        Assert.Contains("ConvertTo-Json", generate);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
