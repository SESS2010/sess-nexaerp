using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev869BCorrection17SourceContractTests
{
    private static readonly string Sql = Source("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs");
    private static readonly string Authorizer = Source("src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs");
    private static readonly Lazy<(CanonicalSqlEvidence Run1, CanonicalSqlEvidence Run2)> CanonicalSqlRuns =
        new(RunCanonicalSqlWorkerTwice);

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
        foreach (var binding in new[] { "ExecutionInstanceId", "ServiceInstanceSha256", "OwnershipLeaseSha256", "pg_try_advisory_xact_lock", "rev869b_transaction_still_active", "rev869b_noncommit_replay_mismatch", "rev869b_noncommit_terminalizer_binding" })
            Assert.Contains(binding, terminal);
        Assert.DoesNotContain("ON CONFLICT(", terminal, StringComparison.Ordinal);
        Assert.Contains("ExecutionInstanceId", Authorizer);
        Assert.Contains("DeterministicOutcomeId", Authorizer);
    }

    [Fact]
    public void Correction22RollbackProofUsesDurableAttemptIdentityWithoutRequiringRolledBackContext()
    {
        var terminal = Slice(Sql, "CREATE FUNCTION nexa.rev869b_record_noncommit_outcome", "CREATE FUNCTION nexa.rev869b_reconcile_command_attempt");
        var rolledBack = Slice(terminal, "terminal_state='RolledBack'", "terminal_state='Abandoned'");
        var quote = Convert.ToChar(34);
        Assert.Contains("a." + quote + "TargetBackendPid" + quote, rolledBack);
        Assert.Contains("a." + quote + "TargetTransactionId" + quote, rolledBack);
        /* Superseded malformed literals:
        Assert.Contains("a.\TargetBackendPid\", rolledBack);
        Assert.Contains("a.\TargetTransactionId\", rolledBack);
        */
        Assert.Contains("rev869b_command_receipts", rolledBack);
        Assert.Contains("pg_try_advisory_xact_lock", terminal);
        Assert.Contains("rev869b_transaction_still_active", terminal);
        Assert.Contains("86923", terminal);
        Assert.Contains("86924", terminal);
        Assert.Contains("86923", Sql);
        Assert.Contains("86924", Sql);
        Assert.DoesNotContain("pg_stat_activity", terminal);
        Assert.DoesNotContain("backend_xid", terminal);
        Assert.DoesNotContain("RolledBack' AND EXISTS", rolledBack);
    }

    [Fact]
    public void Correction22TargetIdentityAndUnresolvedPurgeChainAreAuthoritative()
    {
        Assert.Contains("CREATE TABLE nexa.rev869b_target_instance_identity", Sql);
        Assert.Contains("TR_rev869b_target_instance_identity_immutable", Sql);
        Assert.Contains("target_instance_sha256 text", Sql);
        var quote = Convert.ToChar(34);
        Assert.Contains("i." + quote + "DatabaseName" + quote + "=current_database()", Sql);
        Assert.Contains("i." + quote + "InstanceSha256" + quote + "=target_instance_sha", Sql);
        Assert.Contains("ra." + quote + "RootAuthorizationId" + quote + "=a." + quote + "RootAuthorizationId" + quote, Sql);
        /* Superseded malformed literals:
        Assert.Contains("i.\DatabaseName\=current_database()", Sql);
        Assert.Contains("i.\InstanceSha256\=target_instance_sha", Sql);
        Assert.Contains("child.\PriorAttemptId\=p.\PurgeAttemptId\", Sql);
        */
        Assert.Contains("requires an exact consumed retry chain", Sql);
        Assert.Contains("'Expired'", Sql);
        Assert.Contains("rev869b_purge_retry_child_unique", Sql);
        Assert.Contains("pg_advisory_xact_lock", Sql);
        Assert.Contains("Succeeded','ZeroRows", Sql);
    }

    [Fact]
    public void Correction22AclClosureAndQuarantineAuthorityCoverTheCompleteUniverse()
    {
        var control = Source("tools/rev869b-control-plane-install.sql");
        var verify = Source("tools/rev869b-control-plane-verify.sql");
        Assert.Contains("rev869b_begin_quarantine_attempt", control);
        foreach (var binding in new[] { "ExecutionInstanceId", "ActorId", "ActorIssuer", "Operation", "RegistrationRequestId", "AuthorityEvidenceSha256", "SourceLeaseVersion" }) Assert.Contains(binding, control);
        Assert.Contains("replay.SourceLeaseVersion<>expected_version", control);
        Assert.Contains("n.nspname='nexa' AND pg_get_userbyid(p.proowner)<>'nexa_rev869b_security_owner'", Sql);
        Assert.Contains("d.defaclnamespace='nexa'::regnamespace AND (x.grantee=0 OR x.grantee<>d.defaclrole)", Sql);
        Assert.Contains("d.defaclnamespace='nexa'::regnamespace AND (x.grantee=0 OR x.grantee<>d.defaclrole)", verify);
        Assert.Contains("rolname!~'^pg_'", Sql);
        Assert.Contains("rolname!~'^pg_'", verify);
        Assert.Contains("Target predefined role direct ACL mismatch", Sql);
        Assert.Contains("predefined_direct_acl", verify);
        Assert.Contains("aclexplode(c.relacl)", Sql);
        Assert.Contains("aclexplode(p.proacl)", verify);
        Assert.Contains("Target database owner mismatch", Sql);
        Assert.Contains("a.rolname='nexa_rev869b_security_owner' AND b.rolname='nexa_rev869b_lifecycle_administrator'", Sql);
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
    public void AcceptanceInventoryHasExactlyThirtyFourUniqueIndependentEvidencePlans()
    {
        var inventory = Rev869BAcceptanceScenarioInventory.All;
        Assert.Equal(34, inventory.Count);
        Assert.Equal(34, inventory.Select(x => x.ScenarioId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(new[] { "P01", "P02", "P03", "L01", "L02", "L03", "L04", "L05", "R01", "R02", "R03",
            "C01", "C02", "C03", "C04", "C05", "C06", "C07", "C08", "G01", "G02", "G03", "G04", "G05", "G06",
            "E01", "E02", "E03", "E04", "A01", "A02", "T01", "T02", "T03" }, inventory.Select(x => x.ScenarioId));

        Assert.Equal(34, inventory.Select(x => x.Plan.FixtureOperationId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(34, inventory.Select(x => x.Plan.ActionOperationId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(34, inventory.Select(x => x.Plan.CleanupOperationId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(170, inventory.SelectMany(x => new[] { x.Plan.Before.ReadId, x.Plan.After.ReadId, x.Plan.Durable.ReadId, x.Plan.Audit.ReadId, x.Plan.Cleanup.ReadId }).Distinct(StringComparer.Ordinal).Count());

        Assert.All(inventory, contract =>
        {
            Rev869BLifecycleControllerClient.ValidateContract(contract);
            Assert.StartsWith("rev869b/" + contract.ScenarioId + "/fixture/", contract.Plan.FixtureOperationId, StringComparison.Ordinal);
            Assert.StartsWith("rev869b/" + contract.ScenarioId + "/action/", contract.Plan.ActionOperationId, StringComparison.Ordinal);
            Assert.StartsWith("rev869b/" + contract.ScenarioId + "/cleanup/", contract.Plan.CleanupOperationId, StringComparison.Ordinal);
            Assert.NotEqual(contract.Plan.Before.ReadId, contract.Plan.After.ReadId);
            Assert.NotEqual(contract.Plan.After.ReadId, contract.Plan.Durable.ReadId);
            Assert.NotEqual(contract.Plan.Durable.ReadId, contract.Plan.Cleanup.ReadId);
            Assert.Equal(contract.Plan.Assertions.Count * 2 + 6, contract.Plan.Mutations.Count);
            Assert.Contains("AND", contract.Plan.ExactFormula, StringComparison.Ordinal);
            Assert.DoesNotContain("PASS", contract.Plan.ExactFormula, StringComparison.OrdinalIgnoreCase);
            Assert.NotEmpty(contract.RequiredSubcases);
            Assert.All(contract.RequiredSubcases, subcase => Assert.StartsWith(contract.ScenarioId + ":", subcase.SubcaseId, StringComparison.Ordinal));
        });

        var design = Source("tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs");
        Assert.DoesNotContain("ScenarioExpectedResult", design);
        Assert.DoesNotContain("ExpectedBeforeCount", design);
        Assert.DoesNotContain("ExpectedAfterCount", design);
        Assert.DoesNotContain("\"22012\"", design);
        Assert.DoesNotContain("int4div", design, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("constant PASS", design, StringComparison.OrdinalIgnoreCase);
        foreach (var required in new[] { "Formula(id)", "EvidenceAssertion", "SemanticMutation", "mutate-action",
            "mutate-before-read", "mutate-after-read", "mutate-durable-read", "mutate-assertion:", "weaken-assertion:",
            "Rev869BCorrection26FrozenOracle.SelectorsFor", "Rev869BCorrection26FrozenOracle.SubcasesFor" })
            Assert.Contains(required, design);

        var bodies = Source("tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs");
        Assert.Equal(34, Regex.Matches(bodies, @"\[Fact\]").Count);
        Assert.Equal(33, Regex.Matches(bodies, @"RunAsync\(Rev869BAcceptanceScenarioInventory\.[A-Z][0-9]{2}\)").Count);
        Assert.Contains("BuildDatabaseShapedRawEvidence", bodies);
        Assert.Contains("AdaptAndVerifyDatabaseShapedEvidence", bodies);
        Assert.Contains("EvaluatePipelineMutation", bodies);
        Assert.Contains("VerifyEvidence", bodies);
        Assert.DoesNotContain("BuildOracleEvidence", bodies);
        Assert.DoesNotContain("ExpectedBeforeCount", bodies);
        Assert.DoesNotContain("ExpectedAfterCount", bodies);
        Assert.DoesNotContain("Assert.ThrowsAny", bodies);

        var client = Source("tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs");
        foreach (var required in new[] { "PrepareAsync", "ObserveAsync", "ActAsync", "RequestCleanupAsync", "BuildReadCommand",
            "rev869b_read_lifecycle_facts_v4", "rev869b_read_control_acl_facts_v4", "rev869b_read_command_facts_v4",
            "rev869b_read_purge_facts_v4", "rev869b_read_export_facts_v4", "rev869b_read_target_acl_facts_v4",
            "TargetVerifierConnectionString", "ControlPlaneVerifierConnectionString", "ControllerAudit",
            "AuditSigningPublicKeyPem", "CanonicalObservation", "Evaluate", "FormulaComponent",
            "BuildDatabaseShapedRawEvidence", "AdaptAndVerifyDatabaseShapedEvidence", "ParseTypedObservation",
            "PipelineMutationIsRejected", "VerifyEvidence", "ExecuteScalarAsync" })
            Assert.Contains(required, client);
        Assert.DoesNotContain("BuildOracleEvidence", client);
        Assert.DoesNotContain("RequireAcceptanceEvidence", client);
        Assert.DoesNotContain("AcceptanceEvidence", client);
        Assert.DoesNotContain("ActionPerformed, contract.Action", client);
        Assert.DoesNotContain("evidence.EvidenceQuery", client);
        Assert.DoesNotContain("ExpectedBeforeCount", client);
        Assert.DoesNotContain("ExpectedAfterCount", client);
        Assert.DoesNotContain("JsonElement Metrics", client);
        Assert.DoesNotContain("lifecycle_administrator credentials", client, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Correction28FactPipelineIsBijectiveLocallyEvaluatedAndTamperSensitive()
    {
        Rev869BCorrection26FrozenOracle.Validate();
        Assert.Equal(34, Rev869BCorrection26FrozenOracle.Scenarios.Length);
        Assert.Equal(108, Rev869BCorrection26FrozenOracle.Subcases.Length);
        Assert.Equal(133, Rev869BCorrection26FrozenOracle.Selectors.Length);
        Assert.Equal("bc0d3a4b292553041a6e1b6bf756ca1c04a84d0b76fc3062092e3c54c9b5c0ca",
            Rev869BCorrection26FrozenOracle.ComputedSha256);
        var controlSql = Source("tools/rev869b-control-plane-install.sql");
        foreach (var required in new[] { "rev869b_read_lifecycle_facts_v4", "REV869B-FACTS-v4", "CP-L4",
            "ObservedDatabaseIdentitySha256=$1", "l.Version=$3", "requested_facts text[]",
            "rev869b_read_control_acl_facts_v4", "CP-A4", "aclexplode", "pg_auth_members",
            "PUBLIC", "default|", "function|", "rev869b_canonical_json_v3" })
            Assert.Contains(required, controlSql);
        foreach (var required in new[] { "rev869b_read_command_facts_v4", "rev869b_read_purge_facts_v4",
            "rev869b_read_export_facts_v4", "rev869b_read_target_acl_facts_v4", "REV869B-FACTS-v4",
            @"""LeaseId"" uuid NOT NULL UNIQUE", "current_setting('nexa.rev869b_lease_id')",
            "RootAuthorizationId", "AuthorizedBatchId", "aclexplode", "pg_auth_members",
            "has_table_privilege", "has_function_privilege", "rolbypassrls", "PUBLIC",
            "rev869b_build_raw_facts_v4", "rev869b_canonical_json_v3" })
            Assert.Contains(required, Sql);
        var purgeV3 = Sql[Sql.IndexOf("CREATE FUNCTION nexa.rev869b_read_purge_facts_v4", StringComparison.Ordinal)
            ..Sql.IndexOf("CREATE FUNCTION nexa.rev869b_read_purge_evidence_v2", StringComparison.Ordinal)];
        Assert.DoesNotContain("'contextRows'", purgeV3);
        Assert.DoesNotContain("scopedContexts", purgeV3);
        Assert.Contains(@"c.""OrganizationId""=$1", purgeV3);
        foreach (var exactScope in new[] { "a.\"AuthorizationId\"=$5", "a.\"RootAuthorizationId\"=$7",
            "a.\"AuthorizedBatchId\"=$8", "p.\"PurgeAttemptId\"=$9", "$6=$9", "LIMIT (SELECT \"MaximumRows\"" })
            Assert.Contains(exactScope, purgeV3);
        Assert.Contains("IX_rev869b_command_contexts_organization_opened_token", Sql);
        Assert.Contains("IX_rev869b_lease_events_request_version", controlSql);
        Assert.Contains("IX_rev869b_lease_events_attempt_version", controlSql);
        foreach (var forbidden in new[] { "oracleVersion", "oracleSha256", "expectedOutcome", "assertionResult", "passState" })
        {
            var v3Control = controlSql[controlSql.IndexOf("CREATE FUNCTION nexa.rev869b_read_lifecycle_facts_v4", StringComparison.Ordinal)
                ..controlSql.IndexOf("CREATE FUNCTION nexa.rev869b_control_plane_catalogue_fingerprint", StringComparison.Ordinal)];
            Assert.DoesNotContain(forbidden, v3Control, StringComparison.OrdinalIgnoreCase);
            var v3Target = Sql[Sql.IndexOf("CREATE FUNCTION nexa.rev869b_canonical_json_v3", StringComparison.Ordinal)
                ..Sql.IndexOf("CREATE FUNCTION nexa.rev869b_read_target_acl_evidence_v2", StringComparison.Ordinal)];
            Assert.DoesNotContain(forbidden, v3Target, StringComparison.OrdinalIgnoreCase);
        }
        foreach (var contract in Rev869BAcceptanceScenarioInventory.All)
        {
            Rev869BLifecycleControllerClient.ValidateContract(contract);
            Assert.Equal(
                contract.Plan.Assertions.Select(x => x.AssertionId).Order(StringComparer.Ordinal),
                contract.Plan.RequiredComponentIds.Order(StringComparer.Ordinal));
            Assert.DoesNotContain(contract.Plan.Assertions, x => x.Stage == Rev869BLifecycleControllerClient.EvidenceStage.Audit);
            Assert.DoesNotContain(contract.Plan.Assertions, x => x.Expected.StartsWith("Audit:", StringComparison.Ordinal));

            foreach (var subcase in contract.RequiredSubcases)
            {
                var raw = Rev869BLifecycleControllerClient.BuildDatabaseShapedRawEvidence(contract, subcase);
                var pristine = Rev869BLifecycleControllerClient.AdaptAndVerifyDatabaseShapedEvidence(contract, subcase, raw);
                var pristineFailures = Rev869BLifecycleControllerClient.VerifyEvidence(contract, subcase, pristine);
                Assert.True(pristineFailures.Length == 0,
                    contract.ScenarioId + "/" + subcase.SubcaseId + ": " + string.Join(",", pristineFailures));
                foreach (var mutation in Enum.GetValues<Rev869BLifecycleControllerClient.PipelineMutationKind>())
                {
                    var result = Rev869BLifecycleControllerClient.EvaluatePipelineMutation(contract, subcase, mutation);
                    Assert.True(result.Killed, result.MutationId);
                    Assert.False(result.Survived, result.MutationId);
                    Assert.Equal(result.ExpectedRejectionCode, result.ActualRejectionCode);
                }

                foreach (var assertion in contract.Plan.Assertions)
                {
                    var assertionRemoved = contract with
                    {
                        Plan = contract.Plan with
                        {
                            Assertions = contract.Plan.Assertions
                                .Where(candidate => candidate.AssertionId != assertion.AssertionId)
                                .ToArray()
                        }
                    };
                    Assert.Throws<ArgumentException>(() =>
                        Rev869BLifecycleControllerClient.ValidateContract(assertionRemoved));
                }
            }
        }
    }
    [Fact]
    public void Correction28IndependentFixturesFreshObservationsStructuredRejectionsAndLiveOr3AreExecutable()
    {
        Rev869BCorrection28IndependentEvidenceFixtures.Validate();
        var fixtures = Rev869BCorrection28IndependentEvidenceFixtures.All;
        Assert.Equal(108, fixtures.Length);
        Assert.Equal(108, fixtures.Select(x => x.BeforeObservationId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(108, fixtures.Select(x => x.AfterObservationId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(108, fixtures.Select(x => x.DurableObservationId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(fixtures, fixture => Assert.Equal(5, new[] { fixture.BeforeObservationId,
            fixture.AfterObservationId, fixture.DurableObservationId, fixture.AuditObservationId,
            fixture.CleanupObservationId }.Distinct(StringComparer.Ordinal).Count()));

        var fixtureSource = Source("tests/SESS.NexaERP.Tests/Rev869BCorrection28IndependentEvidenceFixtures.cs");
        Assert.DoesNotContain("Rev869BCorrection26FrozenOracle", fixtureSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ScenarioSpec", fixtureSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectorSpec", fixtureSource, StringComparison.Ordinal);
        var clientSource = Source("tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs");
        Assert.DoesNotContain("catch (Exception", clientSource, StringComparison.Ordinal);
        Assert.True(clientSource.IndexOf("readerIds.Contains(\"OR3\"", StringComparison.Ordinal) <
            clientSource.IndexOf("new NpgsqlConnection", StringComparison.Ordinal));

        Rev869BLifecycleControllerClient.RequireLocalOr3Route("OR3",
            Rev869BLifecycleControllerClient.EvidenceStage.Durable, new[] { "OR3" });
        var wrongOperation = Assert.Throws<InvalidOperationException>(() =>
            Rev869BLifecycleControllerClient.RequireLocalOr3Route("TC4",
                Rev869BLifecycleControllerClient.EvidenceStage.Durable, new[] { "OR3" }));
        Assert.Equal("OR3_WRONG_OPERATION", wrongOperation.Message);

        var contract = Rev869BAcceptanceScenarioInventory.T03;
        var subcase = contract.RequiredSubcases[0];
        var fixture = Rev869BCorrection28IndependentEvidenceFixtures.For(subcase.SubcaseId);
        var results = Enum.GetValues<Rev869BLifecycleControllerClient.PipelineMutationKind>()
            .Select(mutation => Rev869BLifecycleControllerClient.EvaluatePipelineMutation(contract, subcase, mutation)).ToArray();
        Assert.Equal(20, results.Length);
        var records = results.Select(result => new Rev869BLifecycleControllerClient.MutationRunRecord(
            Rev869BCorrection26FrozenOracle.Version, Rev869BCorrection26FrozenOracle.ExpectedSha256,
            fixture.ActionIdentity, contract.ScenarioId, subcase.SubcaseId, result.MutationId,
            result.TargetComponent, result.ExpectedRejectionCode, result.ActualRejectionCode,
            result.Survived, result.EvidenceSha256)).ToArray();
        var observed = Rev869BLifecycleControllerClient.DispatchLocalOr3(records, contract.ScenarioId,
            subcase.SubcaseId, fixture.PreparationIdentity, fixture.ObservationIdentity, fixture.EnvelopeIdentity);
        Assert.Equal(20, observed.Facts.Single(x => x.Name == "killedMutants").Value.GetInt64());
        Assert.Equal(0, observed.Facts.Single(x => x.Name == "survivingMutants").Value.GetInt64());
        var substituted = records.ToArray();
        substituted[0] = substituted[0] with { ActualRejectionCode = "WRONG_OPERATION" };
        var rejected = Assert.Throws<InvalidOperationException>(() => Rev869BLifecycleControllerClient.DispatchLocalOr3(
            substituted, contract.ScenarioId, subcase.SubcaseId, fixture.PreparationIdentity,
            fixture.ObservationIdentity, fixture.EnvelopeIdentity));
        Assert.Equal("OR3_RECORD_EXACT_SET", rejected.Message);
    }
    [Fact]
    public void Correction28OfflineUpDownSqlIsGeneratedWithoutConnectingAndHasPinnedHashes()
    {
        var evidence = CaptureCanonicalSqlEvidence();
        Assert.Equal(0, evidence.ConnectionOpenCount);
        Assert.Equal(0, evidence.MigrationApplyCount);
        var output = Environment.GetEnvironmentVariable("REV869B_A3_SQL_EVIDENCE_PATH");
        if (!string.IsNullOrWhiteSpace(output))
            File.WriteAllText(output, JsonSerializer.Serialize(evidence), new UTF8Encoding(false));
        Assert.Equal(evidence.Up.ByteCount, Convert.FromBase64String(evidence.Up.CanonicalBytesBase64).Length);
        Assert.Equal(evidence.Down.ByteCount, Convert.FromBase64String(evidence.Down.CanonicalBytesBase64).Length);
    }

    [Fact]
    public void A3_CanonicalOfflineSqlGenerationIsStableAcrossTwoFreshProcesses()
    {
        var (run1, run2) = CanonicalSqlRuns.Value;
        AssertCanonicalEvidence(run1, run2);
        Assert.Equal(run1.Up.CanonicalBytesBase64, run2.Up.CanonicalBytesBase64);
        Assert.Equal(run1.Down.CanonicalBytesBase64, run2.Down.CanonicalBytesBase64);
        Assert.Equal(0, run1.ConnectionOpenCount + run2.ConnectionOpenCount);
        Assert.Equal(0, run1.MigrationApplyCount + run2.MigrationApplyCount);
    }

    [Fact]
    public void A3_CheckpointSqlEvidenceMatchesMachineCapturedCanonicalResultExactly()
    {
        var evidence = CanonicalSqlRuns.Value.Run1;
        var checkpoint = Source("outputs/rev869b_external_controller_phase_a_checkpoint.md");
        const string begin = "A3_CANONICAL_SQL_EVIDENCE_JSON_BEGIN";
        const string end = "A3_CANONICAL_SQL_EVIDENCE_JSON_END";
        var start = checkpoint.IndexOf(begin, StringComparison.Ordinal);
        var finish = checkpoint.IndexOf(end, StringComparison.Ordinal);
        Assert.True(start >= 0 && finish > start);
        var actual = checkpoint[(start + begin.Length)..finish].Trim();
        Assert.Equal(JsonSerializer.Serialize(CheckpointProjection(evidence)), actual);
    }

    [Fact]
    public void A3_WrongMigrationEndpointOptionInputHashNewlineEncodingSizeOrSqlHashFailsEvidenceGate()
    {
        var evidence = CanonicalSqlRuns.Value.Run1;
        var mutations = new Func<CanonicalSqlEvidence, CanonicalSqlEvidence>[]
        {
            value => value with { UpFrom = "wrong" },
            value => value with { GenerationOptions = "IdempotentScript" },
            value => value with { SourceHashes = value.SourceHashes.ToDictionary(pair => pair.Key, pair => new string('0', 64), StringComparer.Ordinal) },
            value => value with { NewlineRule = "trim-and-lf" },
            value => value with { EncodingRule = "UTF-16" },
            value => value with { Up = value.Up with { ByteCount = value.Up.ByteCount + 1 } },
            value => value with { Down = value.Down with { Sha256 = new string('0', 64) } }
        };
        foreach (var mutate in mutations)
            Assert.Throws<InvalidDataException>(() => AssertCanonicalEvidence(mutate(evidence), evidence));
    }
    [Fact]
    public void Correction24EvidenceReadersAreVerifierOnlyMinimalAndAclClosed()
    {
        foreach (var function in new[] { "rev869b_read_command_evidence", "rev869b_read_purge_evidence",
            "rev869b_read_export_evidence", "rev869b_read_target_acl_evidence" })
            Assert.Contains("CREATE FUNCTION nexa." + function, Sql);
        Assert.Contains("session_user='nexa_rev869b_target_verifier'", Sql);
        Assert.Contains("REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA nexa FROM PUBLIC", Sql);
        Assert.Contains("GRANT EXECUTE ON FUNCTION nexa.rev869b_reconcile_command_attempt(uuid),nexa.rev869b_reconcile_purge(uuid),nexa.rev869b_read_target_security_state(),nexa.rev869b_read_command_evidence", Sql);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_read_command_evidence(uuid,uuid)", Sql);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_read_purge_evidence(uuid,uuid)", Sql);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_read_export_evidence(uuid,uuid,uuid)", Sql);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_read_target_acl_evidence()", Sql);
        Assert.DoesNotContain("GRANT SELECT ON nexa.rev869b_", Sql);
        Assert.DoesNotContain("EXECUTE format", Slice(Sql, "CREATE FUNCTION nexa.rev869b_read_command_evidence", "CREATE FUNCTION nexa.rev869b_target_catalogue_fingerprint"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("recomputedSha256", Sql);
        Assert.Contains("recomputedBatchSha256", Sql);
        Assert.Contains("fieldKeys", Sql);
    }
    private static CanonicalSqlEvidence CaptureCanonicalSqlEvidence()
    {
        const string rev869A = "20260810120000_Rev869AIdentityMasterScopeFoundation";
        const string rev869B = "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation";
        const string connectionString = "Host=127.0.0.1;Port=1;Database=rev869b_no_connect;Username=no_connect;Timeout=1;Pooling=false";
        var connectionCounter = new ConnectionOpenCounter();
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(connectionCounter)
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var generationOptions = MigrationsSqlGenerationOptions.Default;
        var up = CanonicalizeSql(migrator.GenerateScript(rev869A, rev869B, generationOptions));
        var down = CanonicalizeSql(migrator.GenerateScript(rev869B, rev869A, generationOptions));
        var encoding = new UTF8Encoding(false, true);
        var root = FindRoot();
        var inputs = new[]
        {
            "src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260810120000_Rev869AIdentityMasterScopeFoundation.cs",
            "src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260810120000_Rev869AIdentityMasterScopeFoundation.Designer.cs",
            "src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs",
            "src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs",
            "src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.cs",
            "src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869A.cs",
            "src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs",
            "src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs",
            "src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs"
        };
        var sourceHashes = inputs.ToDictionary(
            static path => path,
            path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(
                root,
                path.Replace('/', Path.DirectorySeparatorChar))))),
            StringComparer.Ordinal);
        var npgsqlAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(assembly =>
            assembly.GetName().Name == "Npgsql.EntityFrameworkCore.PostgreSQL");
        return new(
            Environment.GetEnvironmentVariable("REV869B_A3_SOURCE_COMMIT") ??
                "8c78f6a480fcbf86afbf9f5460598ece5b8d6732",
            RunCommand(root, "dotnet", "--version"),
            RuntimeInformation.FrameworkDescription,
            RunCommand(root, "dotnet", "ef", "--version"),
            typeof(DbContext).Assembly.GetName().Version!.ToString(),
            npgsqlAssembly.GetName().Version!.ToString(),
            RuntimeInformation.OSDescription,
            CultureInfo.CurrentCulture.Name,
            connectionString,
            rev869A,
            rev869B,
            rev869B,
            rev869A,
            generationOptions.ToString(),
            "CRLF and lone CR to LF only; no trim, format, rewrite, or execute",
            "UTF-8 without BOM",
            sourceHashes,
            SqlOutput(up, encoding),
            SqlOutput(down, encoding),
            connectionCounter.OpenCount,
            0);
    }

    private static (CanonicalSqlEvidence Run1, CanonicalSqlEvidence Run2) RunCanonicalSqlWorkerTwice()
    {
        var root = FindRoot();
        var project = Path.Combine(root, "tests", "SESS.NexaERP.Tests", "SESS.NexaERP.Tests.csproj");
        var paths = new[]
        {
            Path.Combine(Path.GetTempPath(), $"rev869b-a3-sql-{Guid.NewGuid():N}-1.json"),
            Path.Combine(Path.GetTempPath(), $"rev869b-a3-sql-{Guid.NewGuid():N}-2.json")
        };
        try
        {
            foreach (var path in paths)
            {
                var start = new ProcessStartInfo("dotnet")
                {
                    WorkingDirectory = root,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                foreach (var argument in new[]
                {
                    "test", project, "--no-build", "--no-restore", "--filter",
                    "FullyQualifiedName~Correction28OfflineUpDownSqlIsGeneratedWithoutConnectingAndHasPinnedHashes",
                    "--logger", "console;verbosity=minimal"
                }) start.ArgumentList.Add(argument);
                start.Environment["REV869B_A3_SQL_EVIDENCE_PATH"] = path;
                using var process = Process.Start(start) ?? throw new InvalidOperationException("SQL evidence worker did not start.");
                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                Task.WaitAll(stdout, stderr);
                if (process.ExitCode != 0 || !File.Exists(path))
                    throw new InvalidOperationException($"SQL evidence worker failed: {stdout.Result}{stderr.Result}");
            }
            return (
                JsonSerializer.Deserialize<CanonicalSqlEvidence>(File.ReadAllText(paths[0]))!,
                JsonSerializer.Deserialize<CanonicalSqlEvidence>(File.ReadAllText(paths[1]))!);
        }
        finally
        {
            foreach (var path in paths)
                if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void AssertCanonicalEvidence(CanonicalSqlEvidence actual, CanonicalSqlEvidence expected)
    {
        if (actual.Commit != expected.Commit ||
            actual.SdkVersion != expected.SdkVersion ||
            actual.RuntimeVersion != expected.RuntimeVersion ||
            actual.EfCliVersion != expected.EfCliVersion ||
            actual.EfCoreVersion != expected.EfCoreVersion ||
            actual.NpgsqlVersion != expected.NpgsqlVersion ||
            actual.OperatingSystem != expected.OperatingSystem ||
            actual.Culture != expected.Culture ||
            actual.ConnectionString != expected.ConnectionString ||
            actual.UpFrom != expected.UpFrom || actual.UpTo != expected.UpTo ||
            actual.DownFrom != expected.DownFrom || actual.DownTo != expected.DownTo ||
            actual.GenerationOptions != expected.GenerationOptions ||
            actual.NewlineRule != expected.NewlineRule ||
            actual.EncodingRule != expected.EncodingRule ||
            JsonSerializer.Serialize(actual.SourceHashes) != JsonSerializer.Serialize(expected.SourceHashes) ||
            actual.Up != expected.Up || actual.Down != expected.Down ||
            actual.ConnectionOpenCount != 0 || actual.MigrationApplyCount != 0)
            throw new InvalidDataException("Canonical SQL evidence differs from the trusted machine result.");
    }

    private static CanonicalSqlCheckpointProjection CheckpointProjection(CanonicalSqlEvidence evidence) => new(
        evidence.Commit,
        evidence.SdkVersion,
        evidence.RuntimeVersion,
        evidence.EfCliVersion,
        evidence.EfCoreVersion,
        evidence.NpgsqlVersion,
        evidence.OperatingSystem,
        evidence.Culture,
        evidence.ConnectionString,
        evidence.UpFrom,
        evidence.UpTo,
        evidence.DownFrom,
        evidence.DownTo,
        evidence.GenerationOptions,
        evidence.NewlineRule,
        evidence.EncodingRule,
        evidence.SourceHashes,
        evidence.Up.ByteCount,
        evidence.Up.LfCount,
        evidence.Up.Sha256,
        evidence.Down.ByteCount,
        evidence.Down.LfCount,
        evidence.Down.Sha256,
        evidence.ConnectionOpenCount,
        evidence.MigrationApplyCount);

    private static CanonicalSqlOutput SqlOutput(string sql, Encoding encoding)
    {
        var bytes = encoding.GetBytes(sql);
        return new(bytes.Length, sql.Count(static character => character == '\n'),
            Convert.ToHexString(SHA256.HashData(bytes)), Convert.ToBase64String(bytes));
    }

    private static string CanonicalizeSql(string sql) =>
        sql.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string RunCommand(string workingDirectory, string fileName, params string[] arguments)
    {
        var start = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException($"{fileName} failed: {stderr}");
        return stdout.Trim();
    }

    private sealed class ConnectionOpenCounter : DbConnectionInterceptor
    {
        public int OpenCount { get; private set; }

        public override InterceptionResult ConnectionOpening(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result)
        {
            OpenCount++;
            return result;
        }

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            OpenCount++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed record CanonicalSqlOutput(
        int ByteCount,
        int LfCount,
        string Sha256,
        string CanonicalBytesBase64);

    private sealed record CanonicalSqlEvidence(
        string Commit,
        string SdkVersion,
        string RuntimeVersion,
        string EfCliVersion,
        string EfCoreVersion,
        string NpgsqlVersion,
        string OperatingSystem,
        string Culture,
        string ConnectionString,
        string UpFrom,
        string UpTo,
        string DownFrom,
        string DownTo,
        string GenerationOptions,
        string NewlineRule,
        string EncodingRule,
        Dictionary<string, string> SourceHashes,
        CanonicalSqlOutput Up,
        CanonicalSqlOutput Down,
        int ConnectionOpenCount,
        int MigrationApplyCount);

    private sealed record CanonicalSqlCheckpointProjection(
        string Commit,
        string SdkVersion,
        string RuntimeVersion,
        string EfCliVersion,
        string EfCoreVersion,
        string NpgsqlVersion,
        string OperatingSystem,
        string Culture,
        string ConnectionString,
        string UpFrom,
        string UpTo,
        string DownFrom,
        string DownTo,
        string GenerationOptions,
        string NewlineRule,
        string EncodingRule,
        Dictionary<string, string> SourceHashes,
        int UpByteCount,
        int UpLfCount,
        string UpSha256,
        int DownByteCount,
        int DownLfCount,
        string DownSha256,
        int ConnectionOpenCount,
        int MigrationApplyCount);

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
