namespace SESS.NexaERP.Tests;

public sealed class Rev869BDatabaseSafetyContractTests
{
    private static readonly string Root = FindRoot();
    private static readonly string Safety = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BDatabaseSafetySql.cs");
    private static readonly string Lifecycle = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BDatabaseLifecycleSql.cs");
    private static readonly string Controlled = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BControlledMutationSql.cs");
    private static readonly string CommandContext = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BCommandContextSql.cs");
    private static readonly string Migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs");
    private static readonly string Model = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.Rev869B.cs");
    private static readonly string Designer = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs");
    private static readonly string Snapshot = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "NexaErpDbContextModelSnapshot.cs");
    private static readonly string Authorizer = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Rev869BCommandContextAuthorizer.cs");
    private static readonly string QualificationEndpoint = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869AConfigurationEndpoints.cs");
    private static readonly string ApplicationPostgres = Read("tests", "SESS.NexaERP.Tests", "Rev869BPostgresApplicationBehaviorTests.cs");
    private static readonly string DirectPostgres = Read("tests", "SESS.NexaERP.Tests", "Rev869BPostgresBehaviorTests.cs");
    private static readonly string Lease = Read("tests", "SESS.NexaERP.Tests", "Rev869BTestDatabaseLease.cs");
    private static readonly string Postgres = Read("tests", "SESS.NexaERP.Tests", "Rev869BPostgresBehaviorTests.cs") +
        Read("tests", "SESS.NexaERP.Tests", "Rev869BPostgresApplicationBehaviorTests.cs") +
        Read("tests", "SESS.NexaERP.Tests", "Rev869BTestDatabaseLease.cs");

    [Fact]
    public void ChildInsertGuardsRequireExactEditableParentVersion()
    {
        foreach (var table in new[] { "request_for_quotation_lines", "rfq_vendor_invitations", "vendor_quotation_lines", "commercial_comparison_lines", "purchase_order_lines", "quotation_technical_verifications", "material_followup_handoffs" })
            Assert.Contains($"TG_TABLE_NAME='{table}'", Safety);
        Assert.Contains("child INSERT requires exactly one editable parent version", Safety);
        Assert.Contains("q.\"Status\"='Draft' AND q.\"Version\"=0", Safety);
        Assert.Contains("c.\"Status\"='Draft' AND c.\"Version\"=0", Safety);
        Assert.Contains("p.\"Status\" IN ('Draft','RevisionDraft')", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.request_for_quotation_lines", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.rfq_vendor_invitations", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.vendor_quotation_lines", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.commercial_comparison_lines", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.purchase_order_lines", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.material_followup_handoffs", Safety);
        Assert.Contains("('Draft','Submitted')", Lifecycle);
    }

    [Fact]
    public void EveryRev869BRelationHasControlledInsertUpdateAndDeleteCoverage()
    {
        foreach (var trigger in new[]
        {
            "trg_rev869b_rfq_lines_immutable", "trg_rev869b_invitation_snapshot_immutable",
            "trg_rev869b_comparison_lines_delete_guard", "trg_rev869b_followup_immutable",
            "trg_rev869b_vendor_quotation_lines_immutable", "trg_rev869b_technical_verifications_immutable",
            "trg_rev869b_purchase_order_lines_immutable", "trg_rev869b_purchase_approval_history_immutable",
            "trg_rev869b_purchase_order_history_immutable", "trg_rev869b_purchase_status_history_immutable"
        })
            Assert.Contains(trigger, Safety + Controlled + Migration);
        Assert.Contains("qualification and provenance snapshot is immutable", Safety);
        Assert.Contains("BEFORE UPDATE OR DELETE ON nexa.request_for_quotation_lines", Safety);
        Assert.Contains("BEFORE UPDATE OR DELETE ON nexa.rfq_vendor_invitations", Safety);
        Assert.Contains("BEFORE DELETE ON nexa.commercial_comparison_lines", Safety);
        Assert.Contains("trg_rev869b_explicit_followup_mutation BEFORE INSERT OR UPDATE ON nexa.material_followup_handoffs", Controlled);
        foreach (var table in new[] { "request_for_quotations", "request_for_quotation_lines", "rfq_vendor_invitations", "vendor_quotations", "vendor_quotation_lines", "quotation_technical_verifications", "commercial_comparisons", "commercial_comparison_lines", "purchase_transaction_approval_history", "purchase_orders", "purchase_order_lines", "purchase_order_history", "material_followup_handoffs", "purchase_transaction_status_history", "purchase_transaction_approval_policies" })
            Assert.Contains($"BEFORE DELETE ON nexa.{table}", Controlled);
        Assert.Contains("rev869b_initial_version_zero", Controlled);
        Assert.Contains("rev869b_exact_version_increment", Controlled);
        Assert.Contains("rev869b_same_status_protected_fields", Controlled);
    }

    [Fact]
    public void CanonicalCommercialFunctionUsesRelationalInputsAndFailClosedJson()
    {
        Assert.Contains("rev869b_commercial_snapshot_reconciles", Safety);
        foreach (var field in new[] { "quantity", "unit_rate", "gross", "assessable", "taxable", "cgst", "sgst", "igst", "cess", "payable" })
            Assert.Contains(field, Safety, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("jsonb_typeof", Safety);
        Assert.Contains("IS NOT DISTINCT FROM", Safety);
        Assert.Contains("EXCEPTION WHEN OTHERS THEN RETURN FALSE", Safety);
        Assert.DoesNotContain("->'taxRule' <>", Safety);
        Assert.Contains("->'taxRule' IS NULL", Safety);
        Assert.Contains("IS DISTINCT FROM", Safety);
        Assert.DoesNotContain(") IS NOT NULL;", Safety);
        Assert.Contains(") IS TRUE;", Lifecycle);
    }

    [Fact]
    public void AuthoritativeTransitionsUseExactJoinsCardinalityTaxAndPoProvenance()
    {
        foreach (var relation in new[] { "request_for_quotations", "request_for_quotation_lines", "rfq_vendor_invitations", "vendor_quotations", "vendor_quotation_lines", "quotation_technical_verifications", "commercial_comparisons", "commercial_comparison_lines", "purchase_orders", "purchase_order_lines", "tax_gst_settings", "purchase_transaction_approval_policies", "purchase_order_history" })
            Assert.Contains(relation, Safety);
        Assert.Contains("expected_count", Safety);
        Assert.Contains("matched_count", Safety);
        Assert.Contains("approval_count<>1", Safety);
        Assert.Contains("exact source/version/cardinality/commercial provenance", Safety);
        Assert.Contains("issue requires exactly one approval history", Safety);
        Assert.DoesNotContain("trg_rev869b_quotation_authoritative_guard", Safety);
        Assert.All(new[] { Safety, Lifecycle }, source => Assert.Contains("SET search_path = pg_catalog, nexa", source));
    }

    [Fact]
    public void MigrationInstallsAndRemovesOnlyOwnedSafetyObjects()
    {
        Assert.Contains("Rev869BDatabaseSafetySql.Install", Migration);
        Assert.Contains("Rev869BDatabaseLifecycleSql.Install", Migration);
        Assert.Contains("Rev869BControlledMutationSql.Install", Migration);
        Assert.Contains("Rev869BCommandContextSql.Install", Migration);
        Assert.Contains("Rev869BControlledMutationSql.Remove", Migration);
        Assert.Contains("Rev869BCommandContextSql.Remove", Migration);
        Assert.Contains("Rev869BDatabaseLifecycleSql.Remove", Migration);
        Assert.Contains("Rev869BDatabaseSafetySql.Remove", Migration);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_", Safety);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_", Lifecycle);
    }

    [Fact]
    public void FuturePostgresSourceRetainsExactDatabaseSafetyAndNoFallback()
    {
        Assert.Contains("sess_nexaerp_rev869b_verify", Postgres);
        Assert.Contains("sess_nexaerp_rev869b_owned_", Postgres);
        Assert.Contains("ISOLATED_REV869B_BEHAVIOR_TESTS", Postgres);
        Assert.Contains("current_database()", Postgres);
        Assert.Contains("no fallback is permitted", Postgres);
        Assert.DoesNotContain("ORDER BY \"Id\" LIMIT 1", Postgres);
        Assert.DoesNotContain("FROM nexa.request_for_quotations r LIMIT 1", Postgres);
        Assert.Contains("BeginTransactionAsync(IsolationLevel.Serializable)", Postgres);
        Assert.Contains("transaction.RollbackAsync()", Postgres);
        Assert.Contains("Unique test database collision", Postgres, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OwnershipToken", Postgres);
        Assert.Contains("owned database marker mismatch", Postgres, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("new EfRev869BPurchaseService", Postgres);
        Assert.Contains("CreateRfqAsync", Postgres);
        Assert.Contains("PurchaseTransactionStatusHistories", Postgres);
        Assert.Contains("AuditLogs", Postgres);
        Assert.DoesNotContain("REV869B-PG-OWNED-DATABASE-GUARDS", Postgres);
        Assert.DoesNotContain("GetHashCode", Postgres);
        Assert.DoesNotContain("Task.Delay(100)", Postgres);
        Assert.DoesNotContain("Assert.ThrowsAsync<PostgresException>(() =>", Postgres);
        Assert.Contains("AssertPostgresGuardAsync", Postgres);
        Assert.Contains("error.SqlState", Postgres);
        Assert.Contains("error.ConstraintName", Postgres);
        Assert.Contains("Mutation matched zero rows", Postgres);
        Assert.Contains("CREATE DATABASE", Postgres);
        Assert.Contains("DROP DATABASE", Postgres);
        Assert.Contains("pg_database WHERE datname=@name", Postgres);
        Assert.Contains("finally", Postgres);
        Assert.Contains("Pooling = false", Postgres);
    }

    [Fact]
    public void TenthCorrectionUsesSignedIssuerBoundTransactionLocalCommandClaims()
    {
        foreach (var value in new[] { "SECURITY DEFINER", "REVOKE ALL", "pg_backend_pid()", "txid_current()", "session_user",
            "transaction_timestamp()", "rev869b_command_token", "set_config('nexa.rev869b_command_token',command_token::text,true)",
            "'claimKind'", "'historyId'", "'entityType'", "'entityId'", "'operation'", "'parentVersion'", "'fromStatus'", "'toStatus'",
            "'actorEmployeeId'", "'identityIssuer'", "'identitySubject'", "'actorRole'", "'organization'",
            "'correlation'", "'remarks'", "'serverTransactionId'", "'serverTimestamp'", "hmac(", "command_nonce", "signature_hex" })
            Assert.Contains(value, CommandContext);
        Assert.Contains("rev869b_command_context_valid", Controlled);
        Assert.Contains("rev869b_claim_command_context", Controlled);
        Assert.DoesNotContain("set_config('nexa.rev869b_command_token',command_token::text,false)", CommandContext);
        Assert.DoesNotContain("set_config('nexa.rev869b_actor_", ServiceSource(), StringComparison.Ordinal);
        Assert.Contains("Rev869BCommandContextAuthorizer.OpenAsync", ServiceSource());
        Assert.Contains("rev869b_open_command_context", Authorizer);
        Assert.DoesNotContain("rev869b_open_command_context(uuid,text,text,text)", CommandContext);
    }

    [Fact]
    public void QualificationLifecycleUsesTheExactCanonicalSpacedLiteral()
    {
        Assert.Contains("'Pending Approval'", Controlled);
        Assert.DoesNotContain("""NEW."VerificationStatus"<>'PendingApproval'""", Controlled);
        Assert.DoesNotContain("""NEW."ApprovalStatus"<>'PendingApproval'""", Controlled);
        Assert.DoesNotContain("""OLD."VerificationStatus"='PendingApproval'""", Controlled);
        Assert.DoesNotContain("""OLD."ApprovalStatus"='PendingApproval'""", Controlled);
        Assert.Contains("rev869b_qualification_lifecycle", Controlled);
        Assert.Contains("rev869b_qualification_provenance_valid", Safety + Controlled);
        Assert.Contains("VerificationStatus = MasterApprovalStatuses.Verified", QualificationEndpoint);
        Assert.Contains("NEW.\"VerificationStatus\"='Verified'", Controlled);
        Assert.Contains("trg_rev869b_qualification_history_insert_guard", Controlled);
        Assert.Contains("normalize-legacy", QualificationEndpoint);
        Assert.Contains("Action = \"Normalize\"", QualificationEndpoint);
        Assert.Contains("legacy_normalization:=OLD.\"VerificationStatus\"='Draft'", Controlled);
        Assert.Contains("creator_matches<>0", Controlled);
        Assert.Contains("expected_action='Normalize'", Controlled);
        Assert.Contains("h.\"AfterJson\"->>'CreatedBy'=h.\"ActorLoginId\"", Controlled);
        Assert.Contains("WHEN expected_action IN ('Create','Normalize') THEN 'Pending Approval'", Controlled);
    }

    [Fact]
    public void RetainedMigrationDateCheckIsSyntacticallyClosedInEveryParityLocation()
    {
        const string valid = "\\\"EffectiveTo\\\" IS NULL OR \\\"EffectiveTo\\\" >= \\\"EffectiveFrom\\\"";
        foreach (var source in new[] { Model, Migration, Designer, Snapshot }) Assert.Contains(valid, source);
        foreach (var source in new[] { Model, Migration, Designer, Snapshot })
            Assert.DoesNotContain("\\\"EffectiveTo\\\" IS NULL OR \\\"EffectiveTo\\\" >= \\\"EffectiveFrom\");", source);
    }

    [Fact]
    public void CommandAuthorityIsExternallyKeyedIssuerBoundReplayBoundAndShortRetained()
    {
        foreach (var value in new[] { "rev869b_command_authorities", "DatabasePrincipal", "IdentityIssuer", "IdentitySubject",
            "Nonce", "AuthenticatedAt", "rev869b_command_signature_stale", "rev869b_command_signature_invalid",
            "rev869b_command_nonce_reused", "interval '15 minutes'", "DELETE FROM nexa.rev869b_command_contexts",
            "CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public", "public.hmac(" })
            Assert.Contains(value, CommandContext);
        Assert.Contains("SHA256.HashData", Authorizer);
        Assert.Contains("HMACSHA256.HashData", Authorizer);
        Assert.Contains("REV869B_COMMAND_SIGNING_KEY", Authorizer);
        Assert.Contains("Convert.FromHexString", Authorizer);
        Assert.DoesNotContain("const string SigningKey", Authorizer + CommandContext);
        Assert.Contains("DELETE FROM nexa.rev869b_command_authorities", Lease);
        Assert.Contains("rev869b_provision_command_authority(current_user,@authoritySecret,NULL)", Lease);
        Assert.Contains("rev869b_provision_command_authority", CommandContext);
        Assert.Contains("session_user IS DISTINCT FROM", CommandContext);
        Assert.Contains("GRANT EXECUTE ON FUNCTION nexa.rev869b_open_command_context", CommandContext);
    }

    [Fact]
    public void HistoryClaimSlotsAreDistinctExactAndSingleUse()
    {
        foreach (var value in new[] { "claim_kind text", "history_id uuid", "'claimKind'", "'historyId'",
            "prior->>'claimKind'=claim_kind", "prior->>'historyId'=history_id::text",
            "prior->>'parentVersion'", "rev869b_command_claim_stale_or_reused" })
            Assert.Contains(value, CommandContext);
        Assert.Contains("rev869b_claim_command_context(TG_TABLE_NAME,NEW.\"Id\"", Controlled);
        Assert.Contains("'qualification_history',expected_history_id", Controlled);
        Assert.Contains("history_matches<>1", Controlled);
        Assert.Contains("specialized<>1", Controlled);
    }

    [Fact]
    public void EmbeddedSqlUsesPairedDollarQuotesSearchPathsAndReverseSafeRemoval()
    {
        foreach (var source in new[] { Safety, Lifecycle, Controlled, CommandContext })
        {
            Assert.Equal(0, Count(source, "$rev869b$") % 2);
            Assert.DoesNotContain("SECURITY DEFINER\r\n        AS", source);
        }
        Assert.Contains("SET search_path=pg_catalog,nexa", CommandContext);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_claim_command_context", CommandContext);
        Assert.True(CommandContext.IndexOf("DROP TABLE IF EXISTS nexa.rev869b_command_contexts", StringComparison.Ordinal) <
                    CommandContext.IndexOf("DROP TABLE IF EXISTS nexa.rev869b_command_authorities", StringComparison.Ordinal));
    }

    [Fact]
    public void PostgresPathsUseExactStructuredErrorsTargetsAndIndependentEvidence()
    {
        Assert.Equal(1, Count(DirectPostgres, "Assert.ThrowsAsync<PostgresException>"));
        Assert.Contains("error.ConstraintName", DirectPostgres);
        Assert.Contains("error.TableName", DirectPostgres);
        Assert.Contains("error.ColumnName", DirectPostgres);
        Assert.DoesNotContain("""Assert.Equal("nexa", error.SchemaName);""", DirectPostgres);
        Assert.Contains("Mutation matched zero rows", DirectPostgres);
        Assert.Contains("CaptureRfqStateAsync(verifier", DirectPostgres);
        Assert.Contains("child.\"Id\"=@sourceId", DirectPostgres);
        Assert.Contains("rev869b_guard_child_insert", DirectPostgres);
        Assert.DoesNotContain("rev869b_validate_child_insert", DirectPostgres);
        Assert.Contains("useAmbientTransaction: false", ApplicationPostgres);
        Assert.Contains("qualificationVerifier", ApplicationPostgres);
        Assert.Contains("rollbackVerifier", ApplicationPostgres);
    }

    [Fact]
    public void DisposableCleanupRemainsOwnedQuarantinedAndRetryable()
    {
        Assert.Contains("if (disposed) return;", ApplicationPostgres);
        Assert.True(ApplicationPostgres.LastIndexOf("disposed = true;", StringComparison.Ordinal) >
                    ApplicationPostgres.LastIndexOf("await databaseLease.DisposeAsync();", StringComparison.Ordinal));
        foreach (var stage in new[] { "rollbackCompleted", "transactionDisposed", "contextDisposed", "baselineVerified" })
            Assert.Contains(stage, ApplicationPostgres);
        foreach (var value in new[] { "DatabasePrefix", "OwnershipToken", "RequireSafeOwnedName",
            "RequireCurrentDatabaseAsync", "RequireMigrationOnceAsync", "Pooling = false",
            "database is quarantined", "RecoverQuarantinedAsync", "Complete high-entropy quarantine recovery proof",
            "Quarantine recovery proof mismatch; DROP is refused.", "DROP DATABASE", "RequireDatabaseAbsentAsync" })
            Assert.Contains(value, Lease);
        Assert.DoesNotContain("DELETE FROM nexa.purchase_transaction", Lease);
    }

    [Fact]
    public void SeventhCorrectionInstallsBoundHistoryCorrelationSodAndQualificationEvidence()
    {
        Assert.DoesNotContain("rev869b_write_bound_history", Controlled);
        Assert.Contains("trg_rev869b_bound_technical_history", Controlled);
        Assert.Contains("rev869b_technical_verification_requires_history", Controlled);
        Assert.Contains("TransitionCorrelationId", Controlled);
        Assert.Contains("parent_correlation", Controlled);
        Assert.Contains("rev869b_creator_self_approval", Controlled);
        Assert.Contains("rev869b_verifier_approver_separation", Controlled);
        Assert.Contains("rev869b_issuer_approver_separation", Controlled);
        foreach (var evidence in new[] { "VerifiedByEmployeeId", "ApprovedByEmployeeId", "verifiedByEmployeeId", "approvedByEmployeeId" })
            Assert.Contains(evidence, Safety + Migration);
        Assert.Contains("InvitedAt", Safety);
        Assert.DoesNotContain("ReceivedAt BETWEEN qualification", Safety);
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));
    private static int Count(string source, string value) =>
        (source.Length - source.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;
    private static string ServiceSource() => Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.cs");
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
