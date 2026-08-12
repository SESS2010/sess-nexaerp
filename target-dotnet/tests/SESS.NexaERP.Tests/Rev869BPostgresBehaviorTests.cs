using System.Data;
using System.Runtime.CompilerServices;
using Npgsql;

namespace SESS.NexaERP.Tests;

// Compiled by source gates, but intentionally executed only during the separately authorized
// isolated REV869B database verification. Every entry point calls OpenVerifiedAsync first.
[Collection(Rev869BPostgresSerialCollection.Name)]
public sealed class Rev869BPostgresBehaviorTests
{
    private const string ExactDatabase = "sess_nexaerp_rev869b_verify";
    private const string ExactOptIn = "ISOLATED_REV869B_BEHAVIOR_TESTS";
    private const string MigrationId = "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation";

    [Fact]
    public async Task SuccessfulTransactionPersistsAndCanBeVerified()
    {
        await using var connection = await OpenVerifiedAsync();
        var id = DeterministicId(nameof(SuccessfulTransactionPersistsAndCanBeVerified), "audit"); var correlation = $"REV869B-PG-SUCCESS-{id:N}";
        await InsertAuditAsync(connection, id, correlation, "Success");
        Assert.Equal(1L, await ScalarAsync(connection, "SELECT count(*) FROM nexa.audit_logs WHERE \"Id\"=@id AND \"CorrelationId\"=@correlation", ("id", id), ("correlation", correlation)));
    }

    [Fact]
    public async Task FailedTransactionRollsBackWithBeforeAfterEquality()
    {
        await using var connection = await OpenVerifiedAsync();
        var (id, before) = await DraftRfqAsync(connection);
        await using (var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            Assert.Equal(1, await ReserveRfqAsync(connection, transaction, id, before, "rollback"));
            await transaction.RollbackAsync();
        }
        Assert.Equal(before, await ScalarAsync(connection, "SELECT \"Version\" FROM nexa.request_for_quotations WHERE \"Id\"=@id", ("id", id)));
    }

    [Fact]
    public async Task TwoIndependentConnectionsHaveExactlyOneWinnerAndRejectStaleWriter()
    {
        await using var first = await OpenVerifiedAsync();
        await using var second = await first.OpenPeerAsync();
        var (id, expected) = await DraftRfqAsync(first);
        await using var firstTx = await first.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var secondTx = await second.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await SetPeerCommandContextAsync(second, secondTx);
        var winner = await ReserveRfqAsync(first, firstTx, id, expected, "winner");
        await firstTx.CommitAsync();
        var stale = await ExecuteAsync(second, "UPDATE nexa.request_for_quotations SET \"Version\"=\"Version\"+1,\"TransitionCorrelationId\"=@correlation,\"UpdatedBy\"=@login WHERE \"Id\"=@id AND \"Version\"=@version",
            secondTx, ("correlation","rev869b-pg-owned:stale"),("login",Rev869BOwnedPostgresDatabase.Login),("id",id),("version",expected));
        await secondTx.RollbackAsync();
        Assert.Equal(1, winner);
        Assert.Equal(0, stale);
        Assert.Equal(expected + 1, await ScalarAsync(first, "SELECT \"Version\" FROM nexa.request_for_quotations WHERE \"Id\"=@id", ("id", id)));
    }

    [Fact]
    public async Task IdempotentReplayReturnsOriginalRowWithoutDuplicate()
    {
        await using var connection = await OpenVerifiedAsync();
        var id = DeterministicId(nameof(IdempotentReplayReturnsOriginalRowWithoutDuplicate), "rfq"); var key = $"rev869b-pg-idempotency-{id:N}";
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        var inserted = await ExecuteAsync(connection, """
            INSERT INTO nexa.request_for_quotations
            SELECT (jsonb_populate_record(NULL::nexa.request_for_quotations,
                to_jsonb(r) || jsonb_build_object('Id',@id,'RfqNumber',@number,'SequenceNumber',@sequence,'IdempotencyKey',@key,'TransitionCorrelationId',@key,'Version',0))).*
            FROM nexa.request_for_quotations r
            WHERE r."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED'
              AND r."Status"='Draft' AND r."IdempotencyKey" LIKE 'rev869b-pg-owned:%'
            """, transaction, ("id", id), ("number", $"REV869B-PG-IDEMP-{id:N}"), ("sequence", DeterministicSequence(id)), ("key", key));
        Assert.Equal(1, inserted);
        var original = await ScalarGuidAsync(connection, "SELECT \"Id\" FROM nexa.request_for_quotations WHERE \"OrganizationId\"=(SELECT \"OrganizationId\" FROM nexa.request_for_quotations WHERE \"Id\"=@id) AND \"IdempotencyKey\"=@key", transaction, ("id", id), ("key", key));
        var replay = await ScalarGuidAsync(connection, "SELECT \"Id\" FROM nexa.request_for_quotations WHERE \"IdempotencyKey\"=@key", transaction, ("key", key));
        Assert.Equal(id, original); Assert.Equal(original, replay);
        Assert.Equal(1L, await ScalarAsync(connection, "SELECT count(*) FROM nexa.request_for_quotations WHERE \"IdempotencyKey\"=@key", transaction, ("key", key)));
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task ConcurrentIdempotencyCollisionHasOneWinnerAndReturnsOriginal()
    {
        await using var first = await OpenVerifiedAsync();
        await using var second = await first.OpenPeerAsync();
        var winnerId = DeterministicId(nameof(ConcurrentIdempotencyCollisionHasOneWinnerAndReturnsOriginal), "winner");
        var loserId = DeterministicId(nameof(ConcurrentIdempotencyCollisionHasOneWinnerAndReturnsOriginal), "loser"); var key = $"rev869b-pg-race-{winnerId:N}";
        const string sql = """
            INSERT INTO nexa.request_for_quotations
            SELECT (jsonb_populate_record(NULL::nexa.request_for_quotations,
                to_jsonb(r) || jsonb_build_object('Id',@id,'RfqNumber',@number,'SequenceNumber',@sequence,'IdempotencyKey',@key,'TransitionCorrelationId',@key,'Version',0))).*
            FROM nexa.request_for_quotations r
            WHERE r."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED'
              AND r."Status"='Draft' AND r."IdempotencyKey" LIKE 'rev869b-pg-owned:%'
            """;
        await using var firstTx = await first.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var secondTx = await second.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await SetPeerCommandContextAsync(second, secondTx);
        Assert.Equal(1, await ExecuteAsync(first, sql, firstTx, ("id", winnerId), ("number", $"REV869B-PG-RACE-{winnerId:N}"), ("sequence", DeterministicSequence(winnerId)), ("key", key)));
        await InsertRfqCreateHistoryAsync(first, firstTx, winnerId, $"REV869B-PG-RACE-{winnerId:N}", key);
        var loserAttempt = ExecuteAsync(second, sql, secondTx, ("id", loserId), ("number", $"REV869B-PG-RACE-{loserId:N}"), ("sequence", DeterministicSequence(loserId)), ("key", key));
        await firstTx.CommitAsync();
        await AssertPostgresGuardAsync(() => loserAttempt, PostgresErrorCodes.UniqueViolation,
            "IX_request_for_quotations_OrganizationId_IdempotencyKey");
        await secondTx.RollbackAsync();
        Assert.Equal(1L, await ScalarAsync(first, "SELECT count(*) FROM nexa.request_for_quotations WHERE \"IdempotencyKey\"=@key", ("key", key)));
        Assert.Equal(winnerId, await ScalarGuidAsync(first, "SELECT \"Id\" FROM nexa.request_for_quotations WHERE \"IdempotencyKey\"=@key", null, ("key", key)));
    }

    [Fact]
    public async Task DirectTerminalStateInsertIsRejected()
    {
        await using var connection = await OpenVerifiedAsync();
        var attempts = new[]
        {
            """
            INSERT INTO nexa.request_for_quotations
            SELECT (jsonb_populate_record(NULL::nexa.request_for_quotations,
                to_jsonb(r) || jsonb_build_object('Id',@id,'RfqNumber',@number,'SequenceNumber',@sequence,'IdempotencyKey',@key,'TransitionCorrelationId',@key,'Status','Closed','Version',0))).*
            FROM nexa.request_for_quotations r
            WHERE r."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED'
              AND r."Status"='Draft' AND r."IdempotencyKey" LIKE 'rev869b-pg-owned:%'
            """,
            """
            INSERT INTO nexa.vendor_quotations
            SELECT (jsonb_populate_record(NULL::nexa.vendor_quotations,
                to_jsonb(q) || jsonb_build_object('Id',@id,'QuotationNumber',@number,'IdempotencyKey',@key,'TransitionCorrelationId',@key,'Status','Rejected','Version',0))).*
            FROM nexa.vendor_quotations q WHERE q."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED'
            """,
            """
            INSERT INTO nexa.commercial_comparisons
            SELECT (jsonb_populate_record(NULL::nexa.commercial_comparisons,
                to_jsonb(c) || jsonb_build_object('Id',@id,'ComparisonNumber',@number,'IdempotencyKey',@key,'TransitionCorrelationId',@key,'Status','Approved','Version',0))).*
            FROM nexa.commercial_comparisons c WHERE c."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED'
            """,
            """
            INSERT INTO nexa.purchase_orders
            SELECT (jsonb_populate_record(NULL::nexa.purchase_orders,
                to_jsonb(p) || jsonb_build_object('Id',@id,'PoNumber',@number,'IdempotencyKey',@key,'TransitionCorrelationId',@key,'Status','Issued','Version',0))).*
            FROM nexa.purchase_orders p WHERE p."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED'
            """
        };
        foreach (var attempt in attempts)
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            var id = DeterministicId(nameof(DirectTerminalStateInsertIsRejected), attempt);
            await AssertPostgresGuardAsync(() => ExecuteAsync(connection, attempt, transaction,
                ("id", id), ("number", $"REV869B-PG-TERMINAL-{id:N}"), ("sequence", DeterministicSequence(id)), ("key", $"terminal-{id:N}")),
                PostgresErrorCodes.RaiseException, "rev869b_enforce_transition");
            await transaction.RollbackAsync();
        }
    }

    [Fact]
    public async Task SnapshotMismatchIsRejectedOnIssue()
    {
        await using var connection = await OpenVerifiedAsync();
        var row = await ApprovedPoAsync(connection);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        await AssertPostgresGuardAsync(() => ExecuteAsync(connection,
            "UPDATE nexa.purchase_orders SET \"Version\"=\"Version\"+1,\"Status\"='Issued',\"TotalPayableValue\"=\"TotalPayableValue\"+0.000001,\"TransitionCorrelationId\"=@correlation,\"UpdatedBy\"=@login WHERE \"Id\"=@id AND \"Version\"=@version",
            transaction, ("correlation","rev869b-pg-owned:issue-tamper"),("login",Rev869BOwnedPostgresDatabase.Login),("id", row.Id), ("version", row.Version)), PostgresErrorCodes.CheckViolation, "rev869b_po_issue_allowlist");
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task CommercialJsonTaxTotalsVersionOrganizationAndProvenanceTamperingAllReject()
    {
        await using var connection = await OpenVerifiedAsync();
        var row = await ApprovedPoAsync(connection);
        var attempts = new (string Sql, string SqlState, string Evidence)[]
        {
            ("""UPDATE nexa.purchase_orders SET "Version"="Version"+2 WHERE "Id"=@id AND "Version"=@version""", PostgresErrorCodes.SerializationFailure, "rev869b_exact_version_increment"),
            ("""UPDATE nexa.purchase_orders SET "Version"="Version"+1,"OrganizationId"='WRONG-ORGANIZATION',"TransitionCorrelationId"=@correlation,"UpdatedBy"=@login WHERE "Id"=@id AND "Version"=@version""", PostgresErrorCodes.CheckViolation, "rev869b_po_approval_allowlist"),
            ("""UPDATE nexa.purchase_orders SET "Version"="Version"+1,"TotalPayableValue"="TotalPayableValue"+0.000001,"TransitionCorrelationId"=@correlation,"UpdatedBy"=@login WHERE "Id"=@id AND "Version"=@version""", PostgresErrorCodes.CheckViolation, "rev869b_po_approval_allowlist"),
            ("""UPDATE nexa.purchase_orders SET "Version"="Version"+1,"ApprovalPolicySnapshotJson"=jsonb_set("ApprovalPolicySnapshotJson",'{approvalValue}','-1'),"TransitionCorrelationId"=@correlation,"UpdatedBy"=@login WHERE "Id"=@id AND "Version"=@version""", PostgresErrorCodes.CheckViolation, "rev869b_po_approval_allowlist"),
            ("""UPDATE nexa.purchase_order_lines SET "CommercialSnapshotJson"=jsonb_set("CommercialSnapshotJson",'{result,totalPayableValue}','-1') WHERE "PurchaseOrderId"=@id""", PostgresErrorCodes.RaiseException, "rev869b_reject_immutable_mutation"),
            ("""UPDATE nexa.purchase_order_lines SET "CommercialSnapshotJson"=jsonb_set("CommercialSnapshotJson",'{vendorQuotationLineId}','"00000000-0000-0000-0000-000000000000"') WHERE "PurchaseOrderId"=@id""", PostgresErrorCodes.RaiseException, "rev869b_reject_immutable_mutation"),
            ("""UPDATE nexa.purchase_order_lines SET "TaxRuleSnapshotJson"=jsonb_set("TaxRuleSnapshotJson",'{isActive}','false') WHERE "PurchaseOrderId"=@id""", PostgresErrorCodes.RaiseException, "rev869b_reject_immutable_mutation"),
            ("""DELETE FROM nexa.purchase_order_lines WHERE "PurchaseOrderId"=@id""", PostgresErrorCodes.CheckViolation, "rev869b_controlled_delete_guard")
        };
        foreach (var attempt in attempts)
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            await AssertPostgresGuardAsync(() => ExecuteAsync(connection, attempt.Sql, transaction,
                ("correlation",$"rev869b-pg-owned:tamper:{attempt.Evidence}"),("login",Rev869BOwnedPostgresDatabase.Login),
                ("id", row.Id), ("version", row.Version)), attempt.SqlState, attempt.Evidence);
            await transaction.RollbackAsync();
            Assert.Equal(1L, await ScalarAsync(connection,
                """SELECT count(*) FROM nexa.purchase_orders WHERE "Id"=@id AND "Version"=@version AND "OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED'""",
                ("id", row.Id), ("version", row.Version)));
        }
    }

    [Fact]
    public async Task PermissionDenialPersistsAuditEvidence()
    {
        await using var connection = await OpenVerifiedAsync();
        var denied = await ScalarAsync(connection, """
            SELECT count(*) FROM nexa.role_page_permissions permission
            JOIN nexa.roles role ON role."Id"=permission."RoleId"
            JOIN nexa.page_definitions page ON page."Id"=permission."PageDefinitionId"
            WHERE upper(trim(role."Code"))='MANAGING_DIRECTOR' AND page."PageKey"='purchase.po'
              AND permission."CanIssue"=FALSE
            """);
        Assert.Equal(1L, denied);
        var id = DeterministicId(nameof(PermissionDenialPersistsAuditEvidence), "audit"); var correlation = $"REV869B-PG-DENIED-{id:N}";
        await InsertAuditAsync(connection, id, correlation, "Failure");
        Assert.Equal(1L, await ScalarAsync(connection, "SELECT count(*) FROM nexa.audit_logs WHERE \"Id\"=@id AND \"Action\"='Denied' AND \"Result\"='Failure'", ("id", id)));
    }

    [Fact]
    public async Task AuditFailureCausesProtectedOperationToFailAndRollback()
    {
        await using var connection = await OpenVerifiedAsync();
        var (id, before) = await DraftRfqAsync(connection);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        Assert.Equal(1, await ReserveRfqAsync(connection, transaction, id, before, "audit-failure"));
        await AssertPostgresGuardAsync(() => ExecuteAsync(connection,
            "INSERT INTO nexa.audit_logs (\"Id\",\"Module\",\"Action\",\"EntityName\",\"EntityId\",\"UserLoginId\",\"CreatedAt\",\"CreatedBy\",\"Version\",\"CorrelationId\",\"Result\") VALUES (NULL,'Purchase','ProtectedOperation','RFQ',@entity,'rev869b-pg-test',now(),'rev869b-pg-test',0,'REV869B-PG-AUDIT-FAIL','Success')",
            transaction, ("entity", id.ToString())), PostgresErrorCodes.NotNullViolation, "audit_logs|Id");
        await transaction.RollbackAsync();
        Assert.Equal(before, await ScalarAsync(connection, "SELECT \"Version\" FROM nexa.request_for_quotations WHERE \"Id\"=@id", ("id", id)));
    }

    [Fact]
    public async Task SkippedAndLowerVersionsAreRejected()
    {
        await using var connection = await OpenVerifiedAsync();
        var (id, version) = await DraftRfqAsync(connection);
        foreach (var proposed in new[] { version - 1, version + 2 })
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            await AssertPostgresGuardAsync(() => ExecuteAsync(connection,
                "UPDATE nexa.request_for_quotations SET \"Version\"=@proposed WHERE \"Id\"=@id AND \"Version\"=@version",
                transaction, ("proposed", proposed), ("id", id), ("version", version)), PostgresErrorCodes.SerializationFailure, "rev869b_exact_version_increment");
            await transaction.RollbackAsync();
        }
    }

    [Fact]
    public async Task DirectDatabaseRejectsLateChildInsertForEveryTerminalAggregate()
    {
        await using var connection = await OpenVerifiedAsync();
        var statements = new[]
        {
            """INSERT INTO nexa.request_for_quotation_lines SELECT (jsonb_populate_record(NULL::nexa.request_for_quotation_lines,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.request_for_quotation_lines child JOIN nexa.request_for_quotations parent ON parent."Id"=child."RequestForQuotationId" WHERE parent."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED' AND parent."Status" IN ('Issued','Closed','Cancelled')""",
            """INSERT INTO nexa.vendor_quotation_lines SELECT (jsonb_populate_record(NULL::nexa.vendor_quotation_lines,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.vendor_quotation_lines child JOIN nexa.vendor_quotations parent ON parent."Id"=child."VendorQuotationId" WHERE parent."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED' AND parent."Status"<>'Draft'""",
            """INSERT INTO nexa.commercial_comparison_lines SELECT (jsonb_populate_record(NULL::nexa.commercial_comparison_lines,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.commercial_comparison_lines child JOIN nexa.commercial_comparisons parent ON parent."Id"=child."CommercialComparisonId" WHERE parent."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED' AND parent."Status"<>'Draft'""",
            """INSERT INTO nexa.purchase_order_lines SELECT (jsonb_populate_record(NULL::nexa.purchase_order_lines,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.purchase_order_lines child JOIN nexa.purchase_orders parent ON parent."Id"=child."PurchaseOrderId" WHERE parent."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED' AND parent."Status" IN ('Approved','Issued','Rejected','Cancelled','Superseded')"""
            ,
            """INSERT INTO nexa.rfq_vendor_invitations SELECT (jsonb_populate_record(NULL::nexa.rfq_vendor_invitations,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.rfq_vendor_invitations child JOIN nexa.request_for_quotations parent ON parent."Id"=child."RequestForQuotationId" WHERE parent."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED' AND parent."Status" IN ('Closed','Cancelled')""",
            """INSERT INTO nexa.quotation_technical_verifications SELECT (jsonb_populate_record(NULL::nexa.quotation_technical_verifications,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.quotation_technical_verifications child JOIN nexa.vendor_quotation_lines line ON line."Id"=child."VendorQuotationLineId" JOIN nexa.vendor_quotations parent ON parent."Id"=line."VendorQuotationId" WHERE parent."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED' AND parent."Status"<>'Submitted'""",
            """INSERT INTO nexa.material_followup_handoffs SELECT (jsonb_populate_record(NULL::nexa.material_followup_handoffs,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.material_followup_handoffs child JOIN nexa.purchase_orders parent ON parent."Id"=child."PurchaseOrderId" WHERE parent."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED' AND (parent."Status"<>'Issued' OR NOT parent."IsCurrentVersion")"""
        };
        foreach (var statement in statements)
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            await AssertPostgresGuardAsync(() => ExecuteAsync(connection, statement, transaction,
                ("id", DeterministicId("late-child", statement))), PostgresErrorCodes.RaiseException, "rev869b_validate_child_insert");
            await transaction.RollbackAsync();
        }
    }

    [Fact]
    public async Task ImmutableHistoryRelationsRejectUnauthorizedUpdateAndDelete()
    {
        await using var connection = await OpenVerifiedAsync();
        var targets = new[]
        {
            "nexa.purchase_transaction_status_history WHERE \"OrganizationId\"='REV869B-PG-DIRECT-TEST-OWNED'",
            "nexa.purchase_transaction_approval_history WHERE \"CommercialComparisonId\" IN (SELECT \"Id\" FROM nexa.commercial_comparisons WHERE \"OrganizationId\"='REV869B-PG-DIRECT-TEST-OWNED')",
            "nexa.purchase_order_history WHERE \"PurchaseOrderId\" IN (SELECT \"Id\" FROM nexa.purchase_orders WHERE \"OrganizationId\"='REV869B-PG-DIRECT-TEST-OWNED')"
        };
        foreach (var target in targets)
        foreach (var verb in new[] { $"UPDATE {target} SET \"UpdatedBy\"='unauthorized'", $"DELETE FROM {target}" })
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            await AssertPostgresGuardAsync(() => ExecuteAsync(connection, verb, transaction),
                PostgresErrorCodes.RaiseException, "rev869b_reject_immutable_mutation");
            await transaction.RollbackAsync();
        }
    }

    [Fact]
    public async Task RejectedPoRevisionResubmissionAndRepeatedRevisionKeepExactAncestry()
    {
        await using var connection = await OpenVerifiedAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        var rejected = await RejectedPoAsync(connection, transaction);
        var firstRevision = DeterministicId(nameof(RejectedPoRevisionResubmissionAndRepeatedRevisionKeepExactAncestry), "revision-1");
        var firstLineId = DeterministicId(nameof(RejectedPoRevisionResubmissionAndRepeatedRevisionKeepExactAncestry), "revision-1-line");
        var firstKey = $"rev869b-pg-owned:revision:{firstRevision:N}";
        Assert.Equal(1, await ExecuteAsync(connection, """
            INSERT INTO nexa.purchase_orders
            SELECT (jsonb_populate_record(NULL::nexa.purchase_orders,to_jsonb(p)||jsonb_build_object(
              'Id',@id,'PreviousVersionId',p."Id",'RevisionNumber',p."RevisionNumber"+1,
              'Status','RevisionDraft','Version',0,'IsCurrentVersion',true,'IdempotencyKey',@key,'TransitionCorrelationId',@key))).*
            FROM nexa.purchase_orders p WHERE p."Id"=@prior AND p."Status"='Rejected' AND NOT p."IsCurrentVersion"
            """, transaction, ("id", firstRevision), ("prior", rejected.Id), ("key", firstKey)));
        Assert.Equal(1, await InsertPoHistoryAsync(connection, transaction, firstRevision, "ReviseRejected", "Rejected", "RevisionDraft", 2, 0, firstKey));
        Assert.Equal(rejected.LineCount, await ExecuteAsync(connection, """
            INSERT INTO nexa.purchase_order_lines
            SELECT (jsonb_populate_record(NULL::nexa.purchase_order_lines,to_jsonb(l)||jsonb_build_object(
              'Id',@lineId,'PurchaseOrderId',@id))).*
            FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId"=@prior
            """, transaction, ("id", firstRevision), ("prior", rejected.Id), ("lineId", firstLineId)));
        var firstResubmit = firstKey + ":resubmit";
        Assert.Equal(1, await ExecuteAsync(connection,
            """UPDATE nexa.purchase_orders SET "Status"='Resubmitted',"Version"=1,"TransitionCorrelationId"=@correlation,"UpdatedBy"=@login WHERE "Id"=@id AND "Status"='RevisionDraft' AND "Version"=0""",
            transaction, ("correlation", firstResubmit), ("login", Rev869BOwnedPostgresDatabase.Login), ("id", firstRevision)));
        Assert.Equal(1, await InsertPoHistoryAsync(connection, transaction, firstRevision, "ResubmitRejected", "RevisionDraft", "Resubmitted", 2, 1, firstResubmit));
        var firstReject = firstKey + ":reject";
        Assert.Equal(1, await ExecuteAsync(connection,
            """UPDATE nexa.purchase_orders SET "Status"='Rejected',"Version"=2,"IsCurrentVersion"=false,"TransitionCorrelationId"=@correlation,"UpdatedBy"=@login WHERE "Id"=@id AND "Status"='Resubmitted' AND "Version"=1""",
            transaction, ("correlation", firstReject), ("login", Rev869BOwnedPostgresDatabase.Login), ("id", firstRevision)));
        Assert.Equal(1, await InsertPoHistoryAsync(connection, transaction, firstRevision, "Reject", "Resubmitted", "Rejected", 2, 2, firstReject));

        var secondRevision = DeterministicId(nameof(RejectedPoRevisionResubmissionAndRepeatedRevisionKeepExactAncestry), "revision-2");
        var secondLineId = DeterministicId(nameof(RejectedPoRevisionResubmissionAndRepeatedRevisionKeepExactAncestry), "revision-2-line");
        var secondKey = $"rev869b-pg-owned:revision:{secondRevision:N}";
        Assert.Equal(1, await ExecuteAsync(connection, """
            INSERT INTO nexa.purchase_orders
            SELECT (jsonb_populate_record(NULL::nexa.purchase_orders,to_jsonb(p)||jsonb_build_object(
              'Id',@id,'PreviousVersionId',p."Id",'RevisionNumber',p."RevisionNumber"+1,
              'Status','RevisionDraft','Version',0,'IsCurrentVersion',true,'IdempotencyKey',@key,'TransitionCorrelationId',@key))).*
            FROM nexa.purchase_orders p WHERE p."Id"=@prior AND p."Status"='Rejected' AND NOT p."IsCurrentVersion"
            """, transaction, ("id", secondRevision), ("prior", firstRevision), ("key", secondKey)));
        Assert.Equal(1, await InsertPoHistoryAsync(connection, transaction, secondRevision, "ReviseRejected", "Rejected", "RevisionDraft", 3, 0, secondKey));
        Assert.Equal(rejected.LineCount, await ExecuteAsync(connection, """
            INSERT INTO nexa.purchase_order_lines
            SELECT (jsonb_populate_record(NULL::nexa.purchase_order_lines,to_jsonb(l)||jsonb_build_object(
              'Id',@lineId,'PurchaseOrderId',@id))).*
            FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId"=@prior
            """, transaction, ("id", secondRevision), ("prior", firstRevision), ("lineId", secondLineId)));
        var secondResubmit = secondKey + ":resubmit";
        Assert.Equal(1, await ExecuteAsync(connection,
            """UPDATE nexa.purchase_orders SET "Status"='Resubmitted',"Version"=1,"TransitionCorrelationId"=@correlation,"UpdatedBy"=@login WHERE "Id"=@id AND "Status"='RevisionDraft' AND "Version"=0""",
            transaction, ("correlation", secondResubmit), ("login", Rev869BOwnedPostgresDatabase.Login), ("id", secondRevision)));
        Assert.Equal(1, await InsertPoHistoryAsync(connection, transaction, secondRevision, "ResubmitRejected", "RevisionDraft", "Resubmitted", 3, 1, secondResubmit));
        Assert.Equal(3L, await ScalarAsync(connection,
            """SELECT count(*) FROM nexa.purchase_orders WHERE "RootPurchaseOrderId"=@root""",
            transaction, ("root", rejected.RootId)));
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task ExactRev869BTriggerAndFunctionInventoryOccursOnce()
    {
        await using var connection = await OpenVerifiedAsync();
        var expectedTriggers = new[]
        {
            "trg_rev869b_approval_policy_overlap_guard", "trg_rev869b_comparison_authoritative_guard",
            "trg_rev869b_comparison_history_insert_guard", "trg_rev869b_comparison_line_insert_guard",
            "trg_rev869b_comparison_line_parent_guard", "trg_rev869b_comparison_line_snapshot_guard",
            "trg_rev869b_comparison_snapshot_guard", "trg_rev869b_comparison_transition_guard",
            "trg_rev869b_followup_insert_guard", "trg_rev869b_followup_parent_guard",
            "trg_rev869b_invitation_insert_guard", "trg_rev869b_invitation_transition_guard",
            "trg_rev869b_po_authoritative_guard", "trg_rev869b_po_history_insert_guard", "trg_rev869b_po_line_insert_guard",
            "trg_rev869b_purchase_approval_history_immutable", "trg_rev869b_purchase_order_history_immutable",
            "trg_rev869b_purchase_order_line_parent_guard", "trg_rev869b_purchase_order_lines_immutable",
            "trg_rev869b_purchase_order_parent_guard", "trg_rev869b_purchase_order_snapshot_guard",
            "trg_rev869b_purchase_order_transition_guard", "trg_rev869b_purchase_status_history_immutable",
            "trg_rev869b_quotation_line_insert_guard",
            "trg_rev869b_quotation_line_parent_guard", "trg_rev869b_quotation_transition_guard",
            "trg_rev869b_rfq_line_insert_guard", "trg_rev869b_rfq_transition_guard",
            "trg_rev869b_status_history_insert_guard", "trg_rev869b_technical_insert_guard",
            "trg_rev869b_technical_parent_guard", "trg_rev869b_technical_verifications_immutable",
            "trg_rev869b_vendor_quotation_lines_immutable", "trg_rev869b_vendor_quotation_snapshot_guard",
            "trg_rev869b_rfq_lines_immutable", "trg_rev869b_invitation_snapshot_immutable",
            "trg_rev869b_comparison_lines_delete_guard", "trg_rev869b_followup_immutable"
        }.Concat(new[]
        {
            "trg_rev869b_bound_comparison_history", "trg_rev869b_bound_followup_history", "trg_rev869b_bound_invitation_history",
            "trg_rev869b_bound_po_history", "trg_rev869b_bound_policy_history", "trg_rev869b_bound_qualification_history",
            "trg_rev869b_bound_quotation_history", "trg_rev869b_bound_rfq_history", "trg_rev869b_bound_technical_history",
            "trg_rev869b_delete_approval_history", "trg_rev869b_delete_comparison", "trg_rev869b_delete_comparison_line", "trg_rev869b_delete_followup",
            "trg_rev869b_delete_invitation", "trg_rev869b_delete_po", "trg_rev869b_delete_po_history", "trg_rev869b_delete_po_line",
            "trg_rev869b_delete_policy", "trg_rev869b_delete_quotation", "trg_rev869b_delete_quotation_line", "trg_rev869b_delete_rfq",
            "trg_rev869b_delete_rfq_line", "trg_rev869b_delete_status_history", "trg_rev869b_delete_technical",
            "trg_rev869b_explicit_comparison_line_mutation", "trg_rev869b_explicit_comparison_mutation", "trg_rev869b_explicit_followup_mutation",
            "trg_rev869b_explicit_invitation_mutation", "trg_rev869b_explicit_po_line_insert", "trg_rev869b_explicit_po_mutation",
            "trg_rev869b_explicit_policy_mutation", "trg_rev869b_explicit_quotation_line_insert", "trg_rev869b_explicit_quotation_mutation",
            "trg_rev869b_explicit_rfq_line_insert", "trg_rev869b_explicit_rfq_mutation", "trg_rev869b_explicit_technical_insert",
            "trg_rev869b_qualification_lifecycle"
        }).Where(x => x != "trg_rev869b_followup_immutable").Order().ToArray();
        var expectedFunctions = new[]
        {
            "rev869b_commercial_snapshot_reconciles", "rev869b_enforce_quotation_transition", "rev869b_enforce_transition",
            "rev869b_guard_authoritative_transition", "rev869b_guard_child_insert", "rev869b_guard_controlled_snapshot",
            "rev869b_guard_history_insert", "rev869b_guard_extended_immutability", "rev869b_reject_immutable_mutation",
            "rev869b_reject_overlapping_approval_policy", "rev869b_validate_parent_contract",
            "rev869b_guard_explicit_mutation", "rev869b_reject_controlled_delete", "rev869b_require_bound_history",
            "rev869b_guard_qualification_lifecycle", "rev869b_require_qualification_history",
            "rev869b_qualification_provenance_valid", "rev869b_write_policy_history"
        }.Order().ToArray();
        var triggers = await StringsAsync(connection, "SELECT t.tgname FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='nexa' AND t.tgname LIKE 'trg_rev869b_%' AND NOT t.tgisinternal ORDER BY t.tgname");
        var functions = await StringsAsync(connection, "SELECT p.proname FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='nexa' AND p.proname LIKE 'rev869b_%' ORDER BY p.proname");
        Assert.Equal(expectedTriggers, triggers);
        Assert.Equal(expectedFunctions, functions);
        Assert.All(triggers.GroupBy(x => x), group => Assert.Single(group));
        Assert.All(functions.GroupBy(x => x), group => Assert.Single(group));
    }

    private static async Task<OwnedConnection> OpenVerifiedAsync([CallerMemberName] string scenario = "")
    {
        var database = await Rev869BOwnedPostgresDatabase.CreateAsync(scenario);
        try { return new OwnedConnection(database, await database.OpenConnectionAsync()); }
        catch { await database.DisposeAsync(); throw; }
    }

    private sealed class OwnedConnection : IAsyncDisposable
    {
        private readonly Rev869BOwnedPostgresDatabase database;
        private readonly NpgsqlConnection connection;
        public OwnedConnection(Rev869BOwnedPostgresDatabase database, NpgsqlConnection connection)
        { this.database=database; this.connection=connection; }
        public static implicit operator NpgsqlConnection(OwnedConnection value) => value.connection;
        public async ValueTask<NpgsqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            var transaction = await connection.BeginTransactionAsync(isolationLevel);
            await SetPeerCommandContextAsync(connection, transaction);
            return transaction;
        }
        public Task<NpgsqlConnection> OpenPeerAsync() => database.OpenConnectionAsync();
        public async ValueTask DisposeAsync()
        {
            Exception? failure = null;
            try { await connection.DisposeAsync(); }
            catch (Exception ex) { failure = ex; }
            finally { await database.DisposeAsync(); }
            if (failure is not null) throw failure;
        }
    }

    private static async Task SetPeerCommandContextAsync(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand("SELECT m.\"EmployeeId\" FROM nexa.employee_identity_mappings m WHERE m.\"OrganizationId\"=@organization AND m.\"Subject\"=@login AND m.\"IsActive\"", connection, transaction);
        command.Parameters.AddWithValue("organization", Rev869BOwnedPostgresDatabase.Organization);
        command.Parameters.AddWithValue("login", Rev869BOwnedPostgresDatabase.Login);
        var actor = (Guid)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Owned command actor is missing."));
        await Rev869BOwnedPostgresDatabase.SetCommandContextAsync(connection, transaction, actor, "PURCHASE_EXECUTIVE");
    }

    private static async Task<int> ReserveRfqAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid id, long version, string suffix)
    {
        var correlation=$"rev869b-pg-owned:reserve:{suffix}";
        var affected=await ExecuteAsync(connection,
            "UPDATE nexa.request_for_quotations SET \"Version\"=\"Version\"+1,\"TransitionCorrelationId\"=@correlation,\"UpdatedAt\"=statement_timestamp(),\"UpdatedBy\"=@login WHERE \"Id\"=@id AND \"Version\"=@version",
            transaction,("correlation",correlation),("login",Rev869BOwnedPostgresDatabase.Login),("id",id),("version",version));
        if (affected==1)
        {
            var number=await ScalarStringAsync(connection,"SELECT \"RfqNumber\" FROM nexa.request_for_quotations WHERE \"Id\"=@id",transaction,("id",id));
            await InsertStatusHistoryAsync(connection,transaction,id,number,"Draft","Draft","ReserveInvitation",correlation,version+1);
        }
        return affected;
    }

    private static Task InsertRfqCreateHistoryAsync(NpgsqlConnection connection,NpgsqlTransaction transaction,Guid id,string number,string correlation) =>
        InsertStatusHistoryAsync(connection,transaction,id,number,null,"Draft","Create",correlation,0);

    private static async Task InsertStatusHistoryAsync(NpgsqlConnection connection,NpgsqlTransaction transaction,Guid id,string number,
        string? from,string to,string action,string correlation,long version)
    {
        var actor=await ScalarGuidAsync(connection,"SELECT \"EmployeeId\" FROM nexa.employee_identity_mappings WHERE \"OrganizationId\"=@organization AND \"Subject\"=@login",transaction,
            ("organization",Rev869BOwnedPostgresDatabase.Organization),("login",Rev869BOwnedPostgresDatabase.Login));
        var historyId=DeterministicId(correlation,"status-history");
        Assert.Equal(1,await ExecuteAsync(connection,"""
            INSERT INTO nexa.purchase_transaction_status_history
            ("Id","OrganizationId","EntityType","EntityId","DocumentNumber","Action","FromStatus","ToStatus","ActorEmployeeId","ActorLoginId","ActorRoleCode","Remarks","CorrelationId","CreatedAt","CreatedBy","Version")
            VALUES (@historyId,@organization,'RFQ',@id,@number,@action,@from,@to,@actor,@login,'PURCHASE_EXECUTIVE','Deterministic owned command',@correlation,statement_timestamp(),@login,@version)
            """,transaction,("historyId",historyId),("organization",Rev869BOwnedPostgresDatabase.Organization),("id",id),("number",number),
            ("action",action),("from",(object?)from??DBNull.Value),("to",to),("actor",actor),("login",Rev869BOwnedPostgresDatabase.Login),
            ("correlation",correlation),("version",version)));
    }

    private static async Task<(Guid Id, long Version)> DraftRfqAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SELECT \"Id\",\"Version\" FROM nexa.request_for_quotations WHERE \"OrganizationId\"='REV869B-PG-DIRECT-TEST-OWNED' AND \"Status\"='Draft' AND \"IdempotencyKey\" LIKE 'rev869b-pg-owned:%'", connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("The isolated REV869B fixture requires its exact test-owned Draft RFQ.");
        var result = (reader.GetGuid(0), reader.GetInt64(1));
        if (await reader.ReadAsync()) throw new InvalidOperationException("The exact test-owned Draft RFQ fixture is ambiguous.");
        return result;
    }

    private static async Task<(Guid Id, long Version)> ApprovedPoAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SELECT \"Id\",\"Version\" FROM nexa.purchase_orders WHERE \"OrganizationId\"='REV869B-PG-DIRECT-TEST-OWNED' AND \"Status\"='Approved' AND \"IsCurrentVersion\" AND \"IdempotencyKey\" LIKE 'rev869b-pg-owned:%'", connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("The isolated REV869B fixture requires its exact test-owned approved current PO.");
        var result = (reader.GetGuid(0), reader.GetInt64(1));
        if (await reader.ReadAsync()) throw new InvalidOperationException("The exact test-owned approved PO fixture is ambiguous.");
        return result;
    }

    private static async Task<(Guid Id, Guid RootId, int LineCount)> RejectedPoAsync(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand("""
            SELECT p."Id",p."RootPurchaseOrderId",(SELECT count(*)::integer FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId"=p."Id")
            FROM nexa.purchase_orders p
            WHERE p."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED' AND p."Status"='Rejected' AND NOT p."IsCurrentVersion"
            """, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("The exact test-owned rejected PO fixture is missing.");
        var result = (reader.GetGuid(0), reader.GetGuid(1), reader.GetInt32(2));
        if (result.Item3 <= 0 || await reader.ReadAsync()) throw new InvalidOperationException("The exact test-owned rejected PO fixture is empty or ambiguous.");
        return result;
    }

    private static Task InsertAuditAsync(NpgsqlConnection connection, Guid id, string correlation, string result) =>
        ExecuteAsync(connection, """
            INSERT INTO nexa.audit_logs
            ("Id","Module","Action","EntityName","EntityId","UserLoginId","AfterJson","CreatedAt","CreatedBy","Version","CorrelationId","Result")
            VALUES (@id,'Security','Denied','purchase.po',@entity,'rev869b-pg-test','{"denied":true}',now(),'rev869b-pg-test',0,@correlation,@result)
            """, ("id", id), ("entity", id.ToString()), ("correlation", correlation), ("result", result));

    private static Task<int> ExecuteAsync(NpgsqlConnection connection, string sql, params (string Name, object Value)[] parameters) =>
        ExecuteAsync(connection, sql, null, parameters);
    private static async Task<int> ExecuteAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction, params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertPostgresGuardAsync(Func<Task<int>> mutation, string? sqlState, string evidence)
    {
        var error = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            var affected = await mutation();
            Assert.True(affected > 0, "Mutation matched zero rows, so it did not exercise a database guard.");
        });
        if (sqlState is not null) Assert.Equal(sqlState, error.SqlState);
        var exactEvidence = string.Join('|', error.ConstraintName, error.TableName, error.ColumnName, error.Where, error.MessageText);
        Assert.Contains(evidence, exactEvidence, StringComparison.OrdinalIgnoreCase);
    }

    private static Guid DeterministicId(string scenario, string entity)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("REV869B-DIRECT|" + scenario + "|" + entity));
        return new Guid(bytes[..16]);
    }

    private static long DeterministicSequence(Guid id) => 10_000_000_000L + BitConverter.ToUInt32(id.ToByteArray(), 0);

    private static Task<int> InsertPoHistoryAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid poId,
        string action, string fromStatus, string toStatus, int revision, long version, string correlation) =>
        ExecuteAsync(connection, """
            INSERT INTO nexa.purchase_order_history
              ("Id","PurchaseOrderId","Action","FromStatus","ToStatus","RevisionNumber","ActorEmployeeId","ActorLoginId","ActorRoleCode","Reason","CorrelationId","CreatedAt","CreatedBy","Version")
            SELECT @id,@po,@action,@from,@to,@revision,m."EmployeeId",@login,'PURCHASE_MANAGER','Deterministic owned lifecycle evidence',@correlation,statement_timestamp(),@login,@version
            FROM nexa.employee_identity_mappings m
            WHERE m."OrganizationId"='REV869B-PG-DIRECT-TEST-OWNED' AND m."Subject"=@login AND m."IsActive"
            """, transaction, ("id", DeterministicId(poId.ToString("N"), correlation)), ("po", poId), ("action", action),
            ("from", fromStatus), ("to", toStatus), ("revision", revision), ("login", Rev869BOwnedPostgresDatabase.Login),
            ("correlation", correlation), ("version", version));

    private static Task<long> ScalarAsync(NpgsqlConnection connection, string sql, params (string Name, object Value)[] parameters) =>
        ScalarAsync(connection, sql, null, parameters);
    private static async Task<long> ScalarAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction, params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<Guid> ScalarGuidAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction, params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return (Guid)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Expected one isolated fixture row."));
    }

    private static async Task<string> ScalarStringAsync(NpgsqlConnection connection,string sql,NpgsqlTransaction? transaction,params (string Name,object Value)[] parameters)
    {
        await using var command=new NpgsqlCommand(sql,connection,transaction);
        foreach(var parameter in parameters) command.Parameters.AddWithValue(parameter.Name,parameter.Value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? throw new InvalidOperationException("Expected one owned string value.");
    }

    private static async Task<string[]> StringsAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync()) values.Add(reader.GetString(0));
        return values.ToArray();
    }
}
