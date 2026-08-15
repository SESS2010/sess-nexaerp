using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

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
        Assert.Contains("MutateEvidence", bodies);
        Assert.Contains("VerifyEvidence", bodies);
        Assert.Contains("ValidateContract(changed)", bodies);
        Assert.DoesNotContain("ExpectedBeforeCount", bodies);
        Assert.DoesNotContain("ExpectedAfterCount", bodies);
        Assert.DoesNotContain("Assert.ThrowsAny", bodies);

        var client = Source("tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs");
        foreach (var required in new[] { "PrepareAsync", "ObserveAsync", "ActAsync", "RequestCleanupAsync", "BuildReadCommand",
            "rev869b_read_lifecycle_evidence_v2", "rev869b_read_control_plane_acl_evidence_v2", "rev869b_read_command_evidence_v2",
            "rev869b_read_purge_evidence_v2", "rev869b_read_export_evidence_v2", "rev869b_read_target_acl_evidence_v2",
            "TargetVerifierConnectionString", "ControlPlaneVerifierConnectionString", "ControllerAudit",
            "AuditSigningPublicKeyPem", "CanonicalObservation", "Evaluate", "FormulaComponent", "BuildOracleEvidence",
            "VerifyEvidence", "MutateEvidence", "EvidenceMutationKind", "ExecuteScalarAsync" })
            Assert.Contains(required, client);
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
    public void Correction26FrozenOracleIsBijectiveLocallyEvaluatedAndTamperSensitive()
    {
        Rev869BCorrection26FrozenOracle.Validate();
        Assert.Equal(34, Rev869BCorrection26FrozenOracle.Scenarios.Length);
        Assert.Equal(108, Rev869BCorrection26FrozenOracle.Subcases.Length);
        Assert.Equal(133, Rev869BCorrection26FrozenOracle.Selectors.Length);
        Assert.Equal("944e9f20e0bc45866891142e9af604f3ddde2b8fca02c0984b39459fca60bd35",
            Rev869BCorrection26FrozenOracle.ComputedSha256);
        var controlSql = Source("tools/rev869b-control-plane-install.sql");
        foreach (var required in new[] { "rev869b_read_lifecycle_evidence_v2", "ObservedDatabaseIdentitySha256=$1",
            "l.Version=$8", "rev869b_read_control_plane_acl_evidence_v2", "aclexplode", "pg_auth_members",
            "PUBLIC", "default|", "function-owner|" })
            Assert.Contains(required, controlSql);
        foreach (var required in new[] { "rev869b_read_command_evidence_v2", "rev869b_read_purge_evidence_v2",
            "rev869b_read_export_evidence_v2", "rev869b_read_target_acl_evidence_v2",
            @"""LeaseId"" uuid NOT NULL UNIQUE", "current_setting('nexa.rev869b_lease_id')",
            "scoped_context", "RootAuthorizationId", "AuthorizedBatchId", "aclexplode",
            "pg_auth_members", "has_table_privilege", "has_function_privilege", "rolbypassrls", "PUBLIC" })
            Assert.Contains(required, Sql);
        var purgeV2 = Sql[Sql.IndexOf("CREATE FUNCTION nexa.rev869b_read_purge_evidence_v2", StringComparison.Ordinal)
            ..Sql.IndexOf("CREATE FUNCTION nexa.rev869b_read_export_evidence_v2", StringComparison.Ordinal)];
        Assert.DoesNotContain("'contextRows'", purgeV2);
        Assert.Contains(@"JOIN authorization a ON cr.""OrganizationId""=substring(a.""Scope"" from 14)", purgeV2);
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
                var pristine = Rev869BLifecycleControllerClient.BuildOracleEvidence(contract, subcase);
                var pristineFailures = Rev869BLifecycleControllerClient.VerifyEvidence(contract, subcase, pristine);
                Assert.True(pristineFailures.Length == 0,
                    contract.ScenarioId + "/" + subcase.SubcaseId + ": " + string.Join(",", pristineFailures));
                foreach (var mutation in Enum.GetValues<Rev869BLifecycleControllerClient.EvidenceMutationKind>())
                    Assert.NotEmpty(Rev869BLifecycleControllerClient.VerifyEvidence(contract, subcase,
                        Rev869BLifecycleControllerClient.MutateEvidence(contract, subcase, pristine, mutation)));
            }
        }
    }
    [Fact]
    public void Correction26OfflineUpDownSqlIsGeneratedWithoutConnectingAndHasPinnedHashes()
    {
        const string rev869A = "20260810120000_Rev869AIdentityMasterScopeFoundation";
        const string rev869B = "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation";
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=rev869b_no_connect;Username=no_connect;Timeout=1;Pooling=false")
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var up = migrator.GenerateScript(rev869A, rev869B);
        var down = migrator.GenerateScript(rev869B, rev869A);
        var upBytes = Encoding.UTF8.GetBytes(up);
        var downBytes = Encoding.UTF8.GetBytes(down);
        var upSha256 = Convert.ToHexString(SHA256.HashData(upBytes));
        var downSha256 = Convert.ToHexString(SHA256.HashData(downBytes));
        Assert.Equal(298085, upBytes.Length);
        Assert.Equal(2535, up.Count(x => x == '\n') + 1);
        Assert.Equal("ECEEE2ECD1A5A3E1FC4D0E227AD59029F25F7903B41EF575CA16DFF8639BA7B3", upSha256);
        Assert.Equal(11046, downBytes.Length);
        Assert.Equal(226, down.Count(x => x == '\n') + 1);
        Assert.Equal("6D9F2A83DA9ADF14A0C763C90AF69DD287805CD00E09132367B714C0B931A4ED", downSha256);
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
