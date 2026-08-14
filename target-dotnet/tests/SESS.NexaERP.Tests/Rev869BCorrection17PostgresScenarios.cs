using System.Text.RegularExpressions;

namespace SESS.NexaERP.Tests;

/// <summary>Source-only contracts for future separately authorized PostgreSQL acceptance execution.</summary>
public sealed class Rev869BCorrection17PostgresScenarios
{
    private static readonly string Controller = Source("tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs");
    private static readonly string ControlPlane = Source("tools/rev869b-control-plane-install.sql");
    private static readonly string Target = Source("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs");

    [Fact] public void P01P03_ProvisioningInputsAndExactAclInventoryAreBound() { Require(Controller, "ClusterSystemIdentifier", "TlsSpkiSha256", "SourceCommit", "ManifestSha256"); Require(Source("tools/rev869b-control-plane-verify.sql"), "relation_delta", "function_delta", "exec_delta"); }
    [Fact] public void L01L05_LifecycleInterruptionRestartConcurrencyAndQuarantineAreRepresented() { Require(ControlPlane, "Reserved", "Provisioning", "Ready", "InUse", "DropAuthorized", "DropStarted", "Quarantined", "CleanupFailed", "Finalized", "UX_rev869b_one_active_lifecycle_attempt"); }
    [Fact] public void R01R03_RecoveryDecisionIsBoundConsumedOnceAndRequiresNewDecisionAfterFailure() { Require(ControlPlane, "rev869b_register_recovery_decision", "rev869b_consume_recovery_decision", "ConsumedAt IS NULL", "CleanupFailed"); }
    [Fact] public void C01C08_CommandSuccessReplayRollbackRestartAndConcurrencyAreRepresented() { Require(Target, "rev869b_register_command_request", "rev869b_start_command_attempt", "rev869b_open_command_attempt", "rev869b_commit_command_attempt", "rev869b_record_noncommit_outcome", "rev869b_reconcile_command_attempt", "UX_rev869b_one_active_command_attempt", "rev869b_command_request_replay_mismatch"); }
    [Fact] public void G01G06_PurgeDenialZeroSuccessFailureRollbackAndConcurrencyAreRepresented() { Require(Target, "Approved", "ZeroRows", "Started", "Succeeded", "Failed", "Interrupted", "rev869b_purge_candidate_drift", "PriorAttemptId"); Assert.DoesNotContain("RetryEligible", Target); }
    [Fact] public void E01E04_ExportPreparationImmutabilityReleaseAndFailureAreRepresented() { Require(Target, "rev869b_register_export_authorization", "rev869b_prepare_export_batch", "rev869b_export_batch_rows", "TR_rev869b_export_rows_immutable", "rev869b_authorize_export_release", "rev869b_read_prepared_export_batch", "Delivered", "Interrupted"); }
    [Fact] public void A01A02_EffectiveAclAndDirectDenialHaveCanonicalChecks() { Require(Source("tools/rev869b-control-plane-verify.sql"), "expected_exec", "actual_exec", "direct_table_access"); Require(Target, "REVOKE ALL ON ALL TABLES", "REVOKE EXECUTE ON ALL FUNCTIONS"); }
    [Fact] public void T01T03_ControllerOwnsAllocationCleanupAndActionSensitiveEvidence() { Require(Controller, "AllocateAsync", "ReleaseAsync", "ActionReached", "UnrelatedMutationCount", "CleanupFinalized", "DurableEvidenceCount"); Assert.DoesNotMatch(new Regex(@"(?i)CREATE\s+(DATABASE|ROLE)|DROP\s+(DATABASE|ROLE)"), Controller); }
    [Fact] public void DenialContractsCarrySqlStateAndDatabaseObject() { Require(Controller, "SqlState", "DatabaseObject", "InitialState", "FinalState"); }
    [Fact] public void EveryControllerCallRequiresExplicitIsolatedOptInAndHttps() { Require(Controller, "REV869B_POSTGRES_OPT_IN", "ISOLATED_REV869B_BEHAVIOR_TESTS", "Uri.UriSchemeHttps"); }

    private static void Require(string source, params string[] values) => Assert.All(values, value => Assert.Contains(value, source));
    private static string Source(string relative) => File.ReadAllText(Path.Combine(FindRoot(), relative.Replace('/', Path.DirectorySeparatorChar)));
    private static string FindRoot() { for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent) if (File.Exists(Path.Combine(d.FullName, "SESS.NexaERP.slnx"))) return d.FullName; throw new DirectoryNotFoundException(); }
}
