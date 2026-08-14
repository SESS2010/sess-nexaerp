

namespace SESS.NexaERP.Tests;

public sealed class Rev869BCorrection16SourceContractTests
{
    private static string Read(string relative) =>
        File.ReadAllText(Path.Combine(FindRoot(), relative.Replace('/', Path.DirectorySeparatorChar)));

    [Fact]
    public void C15N01ProvisioningManifestIsExactReproducibleAndNonMutating()
    {
        var plan = Rev869BControlPlaneProvisioningContract.GeneratePlan(
            Rev869BControlPlaneProvisioningContract.SafeMode.GeneratePlanOnly);
        Assert.Equal(7, Rev869BControlPlaneProvisioningContract.Apis.Length);
        Assert.Equal(4, Rev869BControlPlaneProvisioningContract.Relations.Length);
        Assert.Contains("NO_SILENT_REPAIR=TRUE", plan);
        Assert.Contains("PUBLIC=NO_CONNECT", plan);
        Assert.Contains("DEFAULT_PRIVILEGES=REVOKE_ALL_FROM_PUBLIC", plan);
        Assert.Contains("pg_get_function_identity_arguments", Rev869BControlPlaneProvisioningContract.ExactReadinessSql);
        Assert.Contains("pg_get_function_result", Rev869BControlPlaneProvisioningContract.ExactReadinessSql);
        Assert.DoesNotContain("PASSWORD=", plan, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<InvalidOperationException>(() =>
            Rev869BControlPlaneProvisioningContract.RequireSafeTarget("postgres"));
    }

    [Fact]
    public void C15N02LifecycleSeparatesRequestAndMarkerTimesAndReconcilesPostDrop()
    {
        var source = Read("tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs");
        Assert.Contains("DateTimeOffset leaseRequestedAt", source);
        Assert.Contains("DateTimeOffset markerProvisionedAt", source);
        Assert.Contains(((char)34) + "LeaseRequestedAt" + ((char)34) + "=@leaseRequested", source);
        Assert.Contains(((char)34) + "ProvisionedAt" + ((char)34) + "=@markerProvisioned", source);
        Assert.Contains("post-drop outcome reconciliation", source);
        Assert.True(source.IndexOf("WriteEvidenceAsync(new QuarantineEvidence", StringComparison.Ordinal) <
                    source.IndexOf("ReserveBeforeCreateAsync(reservation)", StringComparison.Ordinal));
        Assert.Contains(((char)34) + "Quarantined" + ((char)34) + ", null", source);
    }

    [Fact]
    public void C15N03PurgeUsesFreshAutocommitPhasesAndDoesNotDestroyApprovalOnProbe()
    {
        var coordinator = Read("tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs");
        var sql = Read("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs");
        Assert.Contains("Pooling = false", coordinator);
        Assert.Contains("fresh autocommit executor connection", coordinator);
        Assert.Contains("Rejected probes are audited but never consume or destroy", sql);
        Assert.Contains(((char)34) + "EventType" + ((char)34) + " IN ('Committed','Failed','Rejected')", sql);
        Assert.DoesNotContain("approval.\"State\"='Approved' THEN\n              UPDATE", sql);
    }

    [Fact]
    public void C15N04DurableAttemptBindsDatabaseExecutionAuthorizationAndBusinessCommand()
    {
        var sql = Read("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs");
        var authorizer = Read("src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs");
        foreach (var field in new[] { "DatabaseInstanceFingerprint", "ControlPlanePolicy", "ExecutionInstanceId",
                     "ServiceInstanceFingerprint", "AuthorizationFingerprint", "AuthorizationExpiresAt",
                     "BusinessCommandFingerprint", "OwnershipLeaseFingerprint", "AttemptSequence", "AttemptedAt" })
            Assert.Contains(field, sql);
        Assert.Contains("rev869b_record_command_consumption_attempt", authorizer);
        Assert.True(authorizer.IndexOf("rev869b_record_command_consumption_attempt", StringComparison.Ordinal) <
                    authorizer.IndexOf("rev869b_open_command_context", StringComparison.Ordinal));
    }

    [Fact]
    public void C15N05ManifestClosesRoleAclOwnershipAndExportByDefault()
    {
        var plan = Rev869BControlPlaneProvisioningContract.GeneratePlan(
            Rev869BControlPlaneProvisioningContract.SafeMode.PostProvisionVerification);
        foreach (var value in new[] { "NOINHERIT", "TABLE_DML=NONE", "PUBLIC=NONE",
                     "NO_FUNCTION_EXECUTE", "OWNERSHIP=ALL_REGISTRY_RELATIONS_FUNCTIONS_TRIGGERS" })
            Assert.Contains(value, plan);
        Assert.DoesNotContain("EXPORT=UNRESTRICTED", plan);
    }

    [Fact]
    public void C15N06FuturePurgeHarnessUsesExactDistinctRolesAndOwnedDatabase()
    {
        var source = Read("tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs");
        Assert.Contains("REV869B_PURGE_EXECUTOR_CONNECTION", source);
        Assert.Contains("nexa_rev869b_purge_executor", source);
        Assert.Contains("DatabasePrefix", source);
        Assert.Contains("session_user=@role", source);
        Assert.Contains("Pooling = false", source);
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
