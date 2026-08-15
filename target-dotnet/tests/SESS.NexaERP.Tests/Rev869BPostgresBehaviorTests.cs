using System.Data;
using System.Runtime.CompilerServices;
using Npgsql;

namespace SESS.NexaERP.Tests;

// Compiled by source gates, but intentionally executed only during the separately authorized
// isolated REV869B database verification. Every entry point calls OpenVerifiedAsync first.
[Collection(Rev869BPostgresSerialCollection.Name)]
public sealed class Rev869BPostgresBehaviorTests
{
    [Fact]
    public async Task SuccessfulTransactionPersistsAndCanBeVerified()
    {
        await using var connection = await OpenVerifiedAsync();
        var id = DeterministicId(nameof(SuccessfulTransactionPersistsAndCanBeVerified), "audit"); var correlation = $"REV869B-PG-SUCCESS-{id:N}";
        await InsertAuditAsync(connection, id, correlation, "Success");
        await using var verifier = await connection.OpenPeerAsync();
        Assert.Equal(1L, await ScalarAsync(verifier, "SELECT count(*) FROM nexa.audit_logs WHERE \"Id\"=@id AND \"CorrelationId\"=@correlation", ("id", id), ("correlation", correlation)));
    }

    [Fact]
    public async Task FailedTransactionRollsBackWithBeforeAfterEquality()
    {
        await using var connection = await OpenVerifiedAsync();
        var (id, version) = await DraftRfqAsync(connection);
        var before = await CaptureRfqStateAsync(connection, id);
        await using (var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            Assert.Equal(1, await ReserveRfqAsync(connection, transaction, id, version, "rollback"));
            await transaction.RollbackAsync();
        }
        await using var verifier = await connection.OpenPeerAsync();
        Assert.Equal(before, await CaptureRfqStateAsync(verifier, id));
    }

    [Fact]
    public async Task TwoIndependentConnectionsHaveExactlyOneWinnerAndRejectStaleWriter()
    {
        await using var first = await OpenVerifiedAsync();
        await using var second = await first.OpenPeerAsync();
        var (id, expected) = await DraftRfqAsync(first);
        await using var firstTx = await first.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var secondTx = await second.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var winner = await ReserveRfqAsync(first, firstTx, id, expected, "winner");
        await firstTx.CommitAsync();
        var stale = await ExecuteAsync(second, "UPDATE nexa.request_for_quotations SET \"Version\"=\"Version\"+1,\"TransitionCorrelationId\"=@correlation,\"UpdatedBy\"=@login WHERE \"Id\"=@id AND \"Version\"=@version",
            secondTx, ("correlation","rev869b-pg-owned:stale"),("login",Rev869BOwnedPostgresDatabase.Login),("id",id),("version",expected));
        await secondTx.RollbackAsync();
        Assert.Equal(1, winner);
        Assert.Equal(0, stale);
        await using var verifier = await first.OpenPeerAsync();
        Assert.Equal(expected + 1, await ScalarAsync(verifier, "SELECT \"Version\" FROM nexa.request_for_quotations WHERE \"Id\"=@id", ("id", id)));
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
            WHERE r."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH'
              AND r."Status"='Draft' AND r."IdempotencyKey" LIKE 'rev869b-pg-owned:%'
            """, transaction, ("id", id), ("number", $"REV869B-PG-IDEMP-{id:N}"), ("sequence", DeterministicSequence(id)), ("key", key));
        Assert.Equal(1, inserted);
        await InsertRfqCreateHistoryAsync(connection, transaction, id, $"REV869B-PG-IDEMP-{id:N}", key);
        var original = await ScalarGuidAsync(connection, "SELECT \"Id\" FROM nexa.request_for_quotations WHERE \"OrganizationId\"=(SELECT \"OrganizationId\" FROM nexa.request_for_quotations WHERE \"Id\"=@id) AND \"IdempotencyKey\"=@key", transaction, ("id", id), ("key", key));
        var replay = await ScalarGuidAsync(connection, "SELECT \"Id\" FROM nexa.request_for_quotations WHERE \"IdempotencyKey\"=@key", transaction, ("key", key));
        Assert.Equal(id, original); Assert.Equal(original, replay);
        Assert.Equal(0, await ExecuteAsync(connection, "SET CONSTRAINTS ALL IMMEDIATE", transaction));
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
            WHERE r."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH'
              AND r."Status"='Draft' AND r."IdempotencyKey" LIKE 'rev869b-pg-owned:%'
            """;
        await using var firstTx = await first.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var secondTx = await second.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        Assert.Equal(1, await ExecuteAsync(first, sql, firstTx, ("id", winnerId), ("number", $"REV869B-PG-RACE-{winnerId:N}"), ("sequence", DeterministicSequence(winnerId)), ("key", key)));
        await InsertRfqCreateHistoryAsync(first, firstTx, winnerId, $"REV869B-PG-RACE-{winnerId:N}", key);
        var loserAttempt = ExecuteAsync(second, sql, secondTx, ("id", loserId), ("number", $"REV869B-PG-RACE-{loserId:N}"), ("sequence", DeterministicSequence(loserId)), ("key", key));
        await firstTx.CommitAsync();
        await AssertPostgresGuardAsync(() => loserAttempt, PostgresErrorCodes.UniqueViolation,
            "IX_request_for_quotations_OrganizationId_IdempotencyKey");
        await secondTx.RollbackAsync();
        await using var verifier = await first.OpenPeerAsync();
        Assert.Equal(1L, await ScalarAsync(verifier, "SELECT count(*) FROM nexa.request_for_quotations WHERE \"IdempotencyKey\"=@key", ("key", key)));
        Assert.Equal(winnerId, await ScalarGuidAsync(verifier, "SELECT \"Id\" FROM nexa.request_for_quotations WHERE \"IdempotencyKey\"=@key", null, ("key", key)));
    }

    [Fact]
    public async Task ExactGrantRejectsEveryOperationSlotAndPrincipalSubstitution()
    {
        await using var connection = await OpenVerifiedAsync();
        var actor = await ScalarGuidAsync(connection,
            """SELECT "EmployeeId" FROM nexa.employee_identity_mappings WHERE "OrganizationId"=@organization AND "Subject"=@login""",
            null, ("organization", Rev869BOwnedPostgresDatabase.Organization), ("login", Rev869BOwnedPostgresDatabase.Login));
        var historyId = DeterministicId(nameof(ExactGrantRejectsEveryOperationSlotAndPrincipalSubstitution), "history");
        var entityId = DeterministicId(nameof(ExactGrantRejectsEveryOperationSlotAndPrincipalSubstitution), "entity");
        var exact = new Rev869BOwnedPostgresDatabase.ExactSlot("purchase_transaction_status_history", historyId, "RFQ", entityId,
            "Submit", 7, "Draft", "Submitted", "exact-binding", "Exact binding evidence");
        var substitutions = new (string Field, string? Guc, object? Value)[]
        {
            ("claim-kind", null, "purchase_transaction_approval_history"),
            ("history", null, DeterministicId(nameof(ExactGrantRejectsEveryOperationSlotAndPrincipalSubstitution), "other-history")),
            ("entity-type", null, "PurchaseOrder"),
            ("entity", null, DeterministicId(nameof(ExactGrantRejectsEveryOperationSlotAndPrincipalSubstitution), "other-entity")),
            ("operation", null, "Approve"), ("version", null, 8L), ("from", null, "Submitted"),
            ("to", null, "Approved"), ("correlation", null, "other-correlation"), ("remarks", null, "Other remarks"),
            ("actor", "nexa.rev869b_actor_employee_id", Guid.NewGuid().ToString()),
            ("organization", "nexa.rev869b_organization", "OTHER-ORGANIZATION"),
            ("issuer", "nexa.rev869b_identity_issuer", "OTHER-ISSUER"),
            ("subject", "nexa.rev869b_identity_subject", "OTHER-SUBJECT"),
            ("role", "nexa.rev869b_actor_role", "OTHER-ROLE")
        };

        foreach (var substitution in substitutions)
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            await Rev869BOwnedPostgresDatabase.SetCommandContextAsync(connection, transaction, actor, "PURCHASE_EXECUTIVE", exact);
            if (substitution.Guc is not null)
                await ExecuteAsync(connection, "SELECT set_config(@name,@value,true)", transaction,
                    ("name", substitution.Guc), ("value", substitution.Value!));
            var values = new object?[] { exact.ClaimKind, exact.HistoryId, exact.EntityType, exact.EntityId, exact.Operation,
                exact.ParentVersion, exact.FromStatus, exact.ToStatus, exact.Correlation, exact.Remarks };
            if (substitution.Guc is null)
            {
                var index = substitution.Field switch
                {
                    "claim-kind" => 0, "history" => 1, "entity-type" => 2, "entity" => 3, "operation" => 4,
                    "version" => 5, "from" => 6, "to" => 7, "correlation" => 8, "remarks" => 9,
                    _ => throw new InvalidOperationException(substitution.Field)
                };
                values[index] = substitution.Value;
            }
            var error = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
                SELECT nexa.rev869b_claim_command_context(@kind,@history,@entityType,@entity,@operation,@version,@from,@to,@correlation,@remarks)
                """, transaction, ("kind", values[0]!), ("history", values[1]!), ("entityType", values[2]!),
                ("entity", values[3]!), ("operation", values[4]!), ("version", values[5]!),
                ("from", values[6] ?? DBNull.Value), ("to", values[7]!), ("correlation", values[8]!), ("remarks", values[9]!)));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
            Assert.Equal("rev869b_command_claim_unissued_or_reused", error.ConstraintName, ignoreCase: true);
            await transaction.RollbackAsync();
        }
    }

    [Fact]
    public async Task SavepointRollbackCannotRestoreConsumedExactClaim()
    {
        await using var connection = await OpenVerifiedAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        var actor = await ScalarGuidAsync(connection,
            """SELECT "EmployeeId" FROM nexa.employee_identity_mappings WHERE "OrganizationId"=@organization AND "Subject"=@login""",
            transaction, ("organization", Rev869BOwnedPostgresDatabase.Organization), ("login", Rev869BOwnedPostgresDatabase.Login));
        var slot = new Rev869BOwnedPostgresDatabase.ExactSlot("purchase_transaction_status_history",
            DeterministicId(nameof(SavepointRollbackCannotRestoreConsumedExactClaim), "history"), "RFQ",
            DeterministicId(nameof(SavepointRollbackCannotRestoreConsumedExactClaim), "entity"),
            "Submit", 3, "Draft", "Submitted", "savepoint-replay", "Savepoint replay evidence");
        await Rev869BOwnedPostgresDatabase.SetCommandContextAsync(connection, transaction, actor, "PURCHASE_EXECUTIVE", slot);
        await ExecuteAsync(connection, "SAVEPOINT rev869b_claim_attempt", transaction);
        async Task<int> Claim() => await ExecuteAsync(connection, """
            SELECT nexa.rev869b_claim_command_context(@kind,@history,@entityType,@entity,@operation,@version,@from,@to,@correlation,@remarks)
            """, transaction, ("kind", slot.ClaimKind), ("history", slot.HistoryId), ("entityType", slot.EntityType),
            ("entity", slot.EntityId), ("operation", slot.Operation), ("version", slot.ParentVersion),
            ("from", slot.FromStatus!), ("to", slot.ToStatus), ("correlation", slot.Correlation), ("remarks", slot.Remarks));
        await Claim();
        await ExecuteAsync(connection, "ROLLBACK TO SAVEPOINT rev869b_claim_attempt", transaction);
        var error = await Assert.ThrowsAsync<PostgresException>(Claim);
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
        Assert.Equal("rev869b_command_claim_unissued_or_reused", error.ConstraintName, ignoreCase: true);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task LeastPrivilegeRuntimeCannotReadSecurityLedgersOrMutateDurableAudit()
    {
        await using var connection = await OpenVerifiedAsync();
        foreach (var relation in new[] { "rev869b_command_requests", "rev869b_command_attempts",
                     "rev869b_command_contexts", "rev869b_command_attempt_outcomes", "rev869b_command_receipts" })
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() =>
                ExecuteAsync(connection, $"SELECT count(*) FROM nexa.{relation}"));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
            Assert.Contains($"permission denied for table {relation}", error.MessageText, StringComparison.OrdinalIgnoreCase);
        }
        foreach (var statement in new[]
                 {
                     """UPDATE nexa.audit_logs SET "Result"="Result" WHERE false""",
                     "DELETE FROM nexa.audit_logs WHERE false"
                 })
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, statement));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
            Assert.Contains("permission denied for table audit_logs", error.MessageText, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task DirectTerminalStateInsertIsRejected()
    {
        await using var connection = await OpenVerifiedAsync();
        var attempts = new[]
        {
            (Sql: """
            INSERT INTO nexa.request_for_quotations
            SELECT (jsonb_populate_record(NULL::nexa.request_for_quotations,
                to_jsonb(r) || jsonb_build_object('Id',@id,'RfqNumber',@number,'SequenceNumber',@sequence,'IdempotencyKey',@key,'TransitionCorrelationId',@key,'Status','Closed','Version',0))).*
            FROM nexa.request_for_quotations r
            WHERE r."Id"=@sourceId AND r."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH'
              AND r."Status"='Draft' AND r."IdempotencyKey" LIKE 'rev869b-pg-owned:%'
            """, Evidence: "rev869b_enforce_transition", SourceKey: "rfq"),
            (Sql: """
            INSERT INTO nexa.vendor_quotations
            SELECT (jsonb_populate_record(NULL::nexa.vendor_quotations,
                to_jsonb(q) || jsonb_build_object('Id',@id,'QuotationNumber',@number,'IdempotencyKey',@key,'TransitionCorrelationId',@key,'Status','Rejected','Version',0))).*
            FROM nexa.vendor_quotations q WHERE q."Id"=@sourceId AND q."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH'
            """, Evidence: "rev869b_enforce_quotation_transition", SourceKey: "quotation"),
            (Sql: """
            INSERT INTO nexa.commercial_comparisons
            SELECT (jsonb_populate_record(NULL::nexa.commercial_comparisons,
                to_jsonb(c) || jsonb_build_object('Id',@id,'ComparisonNumber',@number,'IdempotencyKey',@key,'TransitionCorrelationId',@key,'Status','Approved','Version',0))).*
            FROM nexa.commercial_comparisons c WHERE c."Id"=@sourceId AND c."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH'
            """, Evidence: "rev869b_enforce_transition", SourceKey: "comparison"),
            (Sql: """
            INSERT INTO nexa.purchase_orders
            SELECT (jsonb_populate_record(NULL::nexa.purchase_orders,
                to_jsonb(p) || jsonb_build_object('Id',@id,'PoNumber',@number,'IdempotencyKey',@key,'TransitionCorrelationId',@key,'Status','Issued','Version',0))).*
            FROM nexa.purchase_orders p WHERE p."Id"=@sourceId AND p."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH'
            """, Evidence: "rev869b_enforce_transition", SourceKey: "po-approved")
        };
        foreach (var attempt in attempts)
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            var id = DeterministicId(nameof(DirectTerminalStateInsertIsRejected), attempt.Sql);
            var sourceId = DeterministicId(nameof(DirectTerminalStateInsertIsRejected), attempt.SourceKey);
            var sourceSelect = attempt.Sql[attempt.Sql.IndexOf("SELECT", StringComparison.Ordinal)..];
            Assert.Equal(1L, await ScalarAsync(connection,
                $"SELECT count(*) FROM ({sourceSelect}) exact_owned_source", transaction,
                ("id", id), ("number", $"REV869B-PG-TERMINAL-{id:N}"), ("sequence", DeterministicSequence(id)),
                ("key", $"terminal-{id:N}"), ("sourceId", sourceId)));
            await AssertPostgresGuardAsync(() => ExecuteAsync(connection, attempt.Sql, transaction,
                ("id", id), ("number", $"REV869B-PG-TERMINAL-{id:N}"), ("sequence", DeterministicSequence(id)),
                ("key", $"terminal-{id:N}"), ("sourceId", sourceId)),
                PostgresErrorCodes.RaiseException, attempt.Evidence);
            await transaction.RollbackAsync();
        }
        await using var verifier = await connection.OpenPeerAsync();
        foreach (var attempt in attempts)
        {
            var id = DeterministicId(nameof(DirectTerminalStateInsertIsRejected), attempt.Sql);
            Assert.Equal(0L, await ScalarAsync(verifier, """
                SELECT
                  (SELECT count(*) FROM nexa.request_for_quotations WHERE "Id"=@id)+
                  (SELECT count(*) FROM nexa.vendor_quotations WHERE "Id"=@id)+
                  (SELECT count(*) FROM nexa.commercial_comparisons WHERE "Id"=@id)+
                  (SELECT count(*) FROM nexa.purchase_orders WHERE "Id"=@id)
                """, ("id", id)));
        }
    }

    [Fact]
    public async Task SnapshotMismatchIsRejectedOnIssue()
    {
        await using var connection = await OpenVerifiedAsync();
        var row = await ApprovedPoAsync(connection);
        var before = await CapturePoStateAsync(connection, row.Id);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        await AssertPostgresGuardAsync(() => ExecuteAsync(connection,
            "UPDATE nexa.purchase_orders SET \"Version\"=\"Version\"+1,\"Status\"='Issued',\"TotalPayableValue\"=\"TotalPayableValue\"+0.000001,\"TransitionCorrelationId\"=@correlation,\"UpdatedBy\"=@login WHERE \"Id\"=@id AND \"Version\"=@version",
            transaction, ("correlation","rev869b-pg-owned:issue-tamper"),("login",Rev869BOwnedPostgresDatabase.Login),("id", row.Id), ("version", row.Version)), PostgresErrorCodes.CheckViolation, "rev869b_po_issue_allowlist");
        await transaction.RollbackAsync();
        await using var verifier = await connection.OpenPeerAsync();
        Assert.Equal(before, await CapturePoStateAsync(verifier, row.Id));
    }

    [Fact]
    public async Task CommercialJsonTaxTotalsVersionOrganizationAndProvenanceTamperingAllReject()
    {
        await using var connection = await OpenVerifiedAsync();
        var row = await ApprovedPoAsync(connection);
        var before = await CapturePoStateAsync(connection, row.Id);
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
                """SELECT count(*) FROM nexa.purchase_orders WHERE "Id"=@id AND "Version"=@version AND "OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH'""",
                ("id", row.Id), ("version", row.Version)));
        }
        await using var verifier = await connection.OpenPeerAsync();
        Assert.Equal(before, await CapturePoStateAsync(verifier, row.Id));
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
        var actor = await ScalarGuidAsync(connection,
            """SELECT "EmployeeId" FROM nexa.employee_identity_mappings WHERE "OrganizationId"=@organization AND "Issuer"=@issuer AND "Subject"=@subject AND "IsActive" """,
            null, ("organization", Rev869BOwnedPostgresDatabase.Organization),
            ("issuer", Rev869BOwnedPostgresDatabase.Issuer), ("subject", Rev869BOwnedPostgresDatabase.Login));
        await using (var transaction = await ((NpgsqlConnection)connection).BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var transactionId = Convert.ToInt64(await new NpgsqlCommand("SELECT txid_current()", (NpgsqlConnection)connection, transaction).ExecuteScalarAsync());
            var backendPid = Convert.ToInt32(await new NpgsqlCommand("SELECT pg_backend_pid()", (NpgsqlConnection)connection, transaction).ExecuteScalarAsync());
            await AssertPostgresGuardAsync(() => ExecuteAsync(connection, """
                SELECT nexa.rev869b_open_command_attempt(
                  @attempt,@employee,@issuer,@subject,'MANAGING_DIRECTOR',@organization,digest('denied','sha256'),'[{}]'::jsonb)
                """, transaction, ("attempt", Guid.NewGuid()), ("employee", actor), ("issuer", Rev869BOwnedPostgresDatabase.Issuer),
                ("subject", Rev869BOwnedPostgresDatabase.Login), ("organization", Rev869BOwnedPostgresDatabase.Organization)),
                PostgresErrorCodes.InsufficientPrivilege, "rev869b_attempt_binding");
            await transaction.RollbackAsync();
        }
        var id = DeterministicId(nameof(PermissionDenialPersistsAuditEvidence), "audit"); var correlation = $"REV869B-PG-DENIED-{id:N}";
        await InsertAuditAsync(connection, id, correlation, "Failure");
        await using var verifier = await connection.OpenPeerAsync();
        Assert.Equal(1L, await ScalarAsync(verifier, "SELECT count(*) FROM nexa.audit_logs WHERE \"Id\"=@id AND \"Action\"='Denied' AND \"Result\"='Failure'", ("id", id)));
    }

    [Fact]
    public async Task AuditFailureCausesProtectedOperationToFailAndRollback()
    {
        await using var connection = await OpenVerifiedAsync();
        var (id, version) = await DraftRfqAsync(connection);
        var before = await CaptureRfqStateAsync(connection, id);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        Assert.Equal(1, await ReserveRfqAsync(connection, transaction, id, version, "audit-failure"));
        await AssertPostgresGuardAsync(() => ExecuteAsync(connection,
            "INSERT INTO nexa.audit_logs (\"Id\",\"Module\",\"Action\",\"EntityName\",\"EntityId\",\"UserLoginId\",\"CreatedAt\",\"CreatedBy\",\"Version\",\"CorrelationId\",\"Result\") VALUES (NULL,'Purchase','ProtectedOperation','RFQ',@entity,'rev869b-pg-test',now(),'rev869b-pg-test',0,'REV869B-PG-AUDIT-FAIL','Success')",
            transaction, ("entity", id.ToString())), PostgresErrorCodes.NotNullViolation, "audit_logs|Id");
        await transaction.RollbackAsync();
        await using var verifier = await connection.OpenPeerAsync();
        Assert.Equal(before, await CaptureRfqStateAsync(verifier, id));
    }

    [Fact]
    public async Task SkippedAndLowerVersionsAreRejected()
    {
        await using var connection = await OpenVerifiedAsync();
        var (id, version) = await DraftRfqAsync(connection);
        var before = await CaptureRfqStateAsync(connection, id);
        foreach (var proposed in new[] { version - 1, version + 2 })
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            await AssertPostgresGuardAsync(() => ExecuteAsync(connection,
                "UPDATE nexa.request_for_quotations SET \"Version\"=@proposed WHERE \"Id\"=@id AND \"Version\"=@version",
                transaction, ("proposed", proposed), ("id", id), ("version", version)), PostgresErrorCodes.SerializationFailure, "rev869b_exact_version_increment");
            await transaction.RollbackAsync();
        }
        await using var verifier = await connection.OpenPeerAsync();
        Assert.Equal(before, await CaptureRfqStateAsync(verifier, id));
    }

    [Fact]
    public async Task DirectDatabaseRejectsLateChildInsertForEveryTerminalAggregate()
    {
        await using var connection = await OpenVerifiedAsync();
        var scenario = nameof(DirectDatabaseRejectsLateChildInsertForEveryTerminalAggregate);
        var statements = new[]
        {
            ("""INSERT INTO nexa.request_for_quotation_lines SELECT (jsonb_populate_record(NULL::nexa.request_for_quotation_lines,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.request_for_quotation_lines child JOIN nexa.request_for_quotations parent ON parent."Id"=child."RequestForQuotationId" WHERE child."Id"=@sourceId AND parent."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH' AND parent."Status" IN ('Issued','Closed','Cancelled')""", "terminal-rfq-line"),
            ("""INSERT INTO nexa.vendor_quotation_lines SELECT (jsonb_populate_record(NULL::nexa.vendor_quotation_lines,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.vendor_quotation_lines child JOIN nexa.vendor_quotations parent ON parent."Id"=child."VendorQuotationId" WHERE child."Id"=@sourceId AND parent."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH' AND parent."Status"<>'Draft'""", "quotation-line"),
            ("""INSERT INTO nexa.commercial_comparison_lines SELECT (jsonb_populate_record(NULL::nexa.commercial_comparison_lines,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.commercial_comparison_lines child JOIN nexa.commercial_comparisons parent ON parent."Id"=child."CommercialComparisonId" WHERE child."Id"=@sourceId AND parent."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH' AND parent."Status"<>'Draft'""", "comparison-line"),
            ("""INSERT INTO nexa.purchase_order_lines SELECT (jsonb_populate_record(NULL::nexa.purchase_order_lines,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.purchase_order_lines child JOIN nexa.purchase_orders parent ON parent."Id"=child."PurchaseOrderId" WHERE child."Id"=@sourceId AND parent."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH' AND parent."Status" IN ('Approved','Issued','Rejected','Cancelled','Superseded')""", "po-line-rejected"),
            ("""INSERT INTO nexa.rfq_vendor_invitations SELECT (jsonb_populate_record(NULL::nexa.rfq_vendor_invitations,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.rfq_vendor_invitations child JOIN nexa.request_for_quotations parent ON parent."Id"=child."RequestForQuotationId" WHERE child."Id"=@sourceId AND parent."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH' AND parent."Status" IN ('Closed','Cancelled')""", "terminal-invitation"),
            ("""INSERT INTO nexa.quotation_technical_verifications SELECT (jsonb_populate_record(NULL::nexa.quotation_technical_verifications,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.quotation_technical_verifications child JOIN nexa.vendor_quotation_lines line ON line."Id"=child."VendorQuotationLineId" JOIN nexa.vendor_quotations parent ON parent."Id"=line."VendorQuotationId" WHERE child."Id"=@sourceId AND parent."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH' AND parent."Status"<>'Submitted'""", "technical"),
            ("""INSERT INTO nexa.material_followup_handoffs SELECT (jsonb_populate_record(NULL::nexa.material_followup_handoffs,to_jsonb(child)||jsonb_build_object('Id',@id))).* FROM nexa.material_followup_handoffs child JOIN nexa.purchase_orders parent ON parent."Id"=child."PurchaseOrderId" WHERE child."Id"=@sourceId AND parent."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH' AND (parent."Status"<>'Issued' OR NOT parent."IsCurrentVersion")""", "terminal-followup")
        };
        foreach (var statement in statements)
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            var attemptId = DeterministicId("late-child", statement.Item2);
            var sourceId = DeterministicId(scenario, statement.Item2);
            var sourceSelect = statement.Item1[statement.Item1.IndexOf("SELECT", StringComparison.Ordinal)..];
            Assert.Equal(1L, await ScalarAsync(connection,
                $"SELECT count(*) FROM ({sourceSelect}) exact_owned_source", transaction, ("id", attemptId), ("sourceId", sourceId)));
            await AssertPostgresGuardAsync(() => ExecuteAsync(connection, statement.Item1, transaction,
                ("id", attemptId), ("sourceId", sourceId)), PostgresErrorCodes.RaiseException, "rev869b_guard_child_insert");
            await transaction.RollbackAsync();
        }
        await using var verifier = await connection.OpenPeerAsync();
        foreach (var statement in statements)
        {
            var attemptId = DeterministicId("late-child", statement.Item2);
            Assert.Equal(0L, await ScalarAsync(verifier, """
                SELECT
                  (SELECT count(*) FROM nexa.request_for_quotation_lines WHERE "Id"=@id)+
                  (SELECT count(*) FROM nexa.vendor_quotation_lines WHERE "Id"=@id)+
                  (SELECT count(*) FROM nexa.commercial_comparison_lines WHERE "Id"=@id)+
                  (SELECT count(*) FROM nexa.purchase_order_lines WHERE "Id"=@id)+
                  (SELECT count(*) FROM nexa.rfq_vendor_invitations WHERE "Id"=@id)+
                  (SELECT count(*) FROM nexa.quotation_technical_verifications WHERE "Id"=@id)+
                  (SELECT count(*) FROM nexa.material_followup_handoffs WHERE "Id"=@id)
                """, ("id", attemptId)));
        }
    }

    [Fact]
    public async Task ImmutableHistoryRelationsRejectUnauthorizedUpdateAndDelete()
    {
        await using var connection = await OpenVerifiedAsync();
        var before = await CaptureHistoryStateAsync(connection);
        var targets = new[]
        {
            "nexa.purchase_transaction_status_history WHERE \"OrganizationId\"='REV869B-PG-SELF-OWNED-GRAPH'",
            "nexa.purchase_transaction_approval_history WHERE \"CommercialComparisonId\" IN (SELECT \"Id\" FROM nexa.commercial_comparisons WHERE \"OrganizationId\"='REV869B-PG-SELF-OWNED-GRAPH')",
            "nexa.purchase_order_history WHERE \"PurchaseOrderId\" IN (SELECT \"Id\" FROM nexa.purchase_orders WHERE \"OrganizationId\"='REV869B-PG-SELF-OWNED-GRAPH')"
        };
        foreach (var target in targets)
        foreach (var attempt in new[]
        {
            (Sql: $"UPDATE {target} SET \"UpdatedBy\"='unauthorized'", Evidence: "rev869b_reject_immutable_mutation", State: PostgresErrorCodes.RaiseException),
            (Sql: $"DELETE FROM {target}", Evidence: "rev869b_controlled_delete_guard", State: PostgresErrorCodes.CheckViolation)
        })
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            await AssertPostgresGuardAsync(() => ExecuteAsync(connection, attempt.Sql, transaction),
                attempt.State, attempt.Evidence);
            await transaction.RollbackAsync();
        }
        await using var verifier = await connection.OpenPeerAsync();
        Assert.Equal(before, await CaptureHistoryStateAsync(verifier));
    }

    [Fact]
    public async Task RejectedPoRevisionResubmissionAndRepeatedRevisionKeepExactAncestry()
    {
        await using var connection = await OpenVerifiedAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        var manager = await ScalarGuidAsync(connection,
            """SELECT "EmployeeId" FROM nexa.employee_identity_mappings WHERE "OrganizationId"=@organization AND "Subject"=@login""",
            transaction, ("organization", Rev869BOwnedPostgresDatabase.Organization), ("login", Rev869BOwnedPostgresDatabase.Login));
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
        Assert.Equal(1, await InsertPoStatusHistoryAsync(connection, transaction, firstRevision, "ReviseRejected", "Rejected", "RevisionDraft", 0, firstKey));
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
        Assert.Equal(1, await InsertPoStatusHistoryAsync(connection, transaction, firstRevision, "ResubmitRejected", "RevisionDraft", "Resubmitted", 1, firstResubmit));
        var firstReject = firstKey + ":reject";
        Assert.Equal(1, await ExecuteAsync(connection,
            """UPDATE nexa.purchase_orders SET "Status"='Rejected',"Version"=2,"IsCurrentVersion"=false,"TransitionCorrelationId"=@correlation,"UpdatedBy"=@login WHERE "Id"=@id AND "Status"='Resubmitted' AND "Version"=1""",
            transaction, ("correlation", firstReject), ("login", Rev869BOwnedPostgresDatabase.Login), ("id", firstRevision)));
        Assert.Equal(1, await InsertPoHistoryAsync(connection, transaction, firstRevision, "Reject", "Resubmitted", "Rejected", 2, 2, firstReject));
        Assert.Equal(1, await InsertPoStatusHistoryAsync(connection, transaction, firstRevision, "Reject", "Resubmitted", "Rejected", 2, firstReject));

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
        Assert.Equal(1, await InsertPoStatusHistoryAsync(connection, transaction, secondRevision, "ReviseRejected", "Rejected", "RevisionDraft", 0, secondKey));
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
        Assert.Equal(1, await InsertPoStatusHistoryAsync(connection, transaction, secondRevision, "ResubmitRejected", "RevisionDraft", "Resubmitted", 1, secondResubmit));
        Assert.Equal(3L, await ScalarAsync(connection,
            """SELECT count(*) FROM nexa.purchase_orders WHERE "RootPurchaseOrderId"=@root""",
            transaction, ("root", rejected.RootId)));
        Assert.Equal(0, await ExecuteAsync(connection, "SET CONSTRAINTS ALL IMMEDIATE", transaction));
        await transaction.CommitAsync();
        await using var verifier = await connection.OpenPeerAsync();
        Assert.Equal(3L, await ScalarAsync(verifier,
            """SELECT count(*) FROM nexa.purchase_orders WHERE "RootPurchaseOrderId"=@root""",
            ("root", rejected.RootId)));
        Assert.Equal(5L, await ScalarAsync(verifier,
            """SELECT count(*) FROM nexa.purchase_order_history h JOIN nexa.purchase_orders p ON p."Id"=h."PurchaseOrderId" WHERE p."RootPurchaseOrderId"=@root AND h."CorrelationId" LIKE 'rev869b-pg-owned:revision:%'""",
            ("root", rejected.RootId)));
        Assert.Equal(5L, await ScalarAsync(verifier,
            """SELECT count(*) FROM nexa.purchase_transaction_status_history h JOIN nexa.purchase_orders p ON p."Id"=h."EntityId" WHERE h."EntityType"='PurchaseOrder' AND p."RootPurchaseOrderId"=@root AND h."CorrelationId" LIKE 'rev869b-pg-owned:revision:%'""",
            ("root", rejected.RootId)));
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
            "trg_rev869b_qualification_history_insert_guard", "trg_rev869b_qualification_lifecycle",
            "trg_rev869b_durable_audit_retention"
        }).Where(x => x != "trg_rev869b_followup_immutable").Order().ToArray();
        var expectedFunctions = new[]
        {
            "rev869b_commercial_snapshot_reconciles", "rev869b_enforce_quotation_transition", "rev869b_enforce_transition",
            "rev869b_guard_authoritative_transition", "rev869b_guard_child_insert", "rev869b_guard_controlled_snapshot",
            "rev869b_guard_history_insert", "rev869b_guard_extended_immutability", "rev869b_reject_immutable_mutation",
            "rev869b_reject_overlapping_approval_policy", "rev869b_validate_parent_contract",
            "rev869b_guard_explicit_mutation", "rev869b_reject_controlled_delete", "rev869b_require_bound_history",
            "rev869b_guard_durable_audit_retention",
            "rev869b_guard_qualification_lifecycle", "rev869b_require_qualification_history",
            "rev869b_guard_qualification_history_insert",
            "rev869b_qualification_provenance_valid", "rev869b_write_policy_history",
            "rev869b_deny_ledger_mutation", "rev869b_register_command_request", "rev869b_start_command_attempt", "rev869b_open_command_attempt",
            "rev869b_command_context_valid", "rev869b_claim_command_context", "rev869b_commit_command_attempt",
            "rev869b_record_noncommit_outcome", "rev869b_reconcile_command_attempt",
            "rev869b_read_target_security_state", "rev869b_read_command_evidence", "rev869b_read_purge_evidence",
            "rev869b_read_export_evidence", "rev869b_read_target_acl_evidence",
            "rev869b_register_purge_authorization", "rev869b_start_purge", "rev869b_execute_purge",
            "rev869b_record_purge_failure", "rev869b_reconcile_purge",
            "rev869b_register_export_authorization", "rev869b_prepare_export_batch",
            "rev869b_authorize_export_release", "rev869b_read_prepared_export_batch",
            "rev869b_record_export_release_outcome"
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
            return await connection.BeginTransactionAsync(isolationLevel);
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

    private static async Task ProveBroadPeerCommandContextIsForbiddenAsync(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand("SELECT m.\"EmployeeId\" FROM nexa.employee_identity_mappings m WHERE m.\"OrganizationId\"=@organization AND m.\"Subject\"=@login AND m.\"IsActive\"", connection, transaction);
        command.Parameters.AddWithValue("organization", Rev869BOwnedPostgresDatabase.Organization);
        command.Parameters.AddWithValue("login", Rev869BOwnedPostgresDatabase.Login);
        var actor = (Guid)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Owned command actor is missing."));
        throw new InvalidOperationException("Broad peer command contexts are forbidden; callers must issue exact operation slots.");
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
        await Rev869BOwnedPostgresDatabase.SetCommandContextAsync(connection, transaction, actor, "PURCHASE_EXECUTIVE",
            new Rev869BOwnedPostgresDatabase.ExactSlot("purchase_transaction_status_history", historyId, "RFQ", id,
                action, version, from, to, correlation, "Deterministic owned command"));
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
        await using var command = new NpgsqlCommand("SELECT \"Id\",\"Version\" FROM nexa.request_for_quotations WHERE \"OrganizationId\"='REV869B-PG-SELF-OWNED-GRAPH' AND \"Status\"='Draft' AND \"IdempotencyKey\" LIKE 'rev869b-pg-owned:%'", connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("The isolated REV869B fixture requires its exact test-owned Draft RFQ.");
        var result = (reader.GetGuid(0), reader.GetInt64(1));
        if (await reader.ReadAsync()) throw new InvalidOperationException("The exact test-owned Draft RFQ fixture is ambiguous.");
        return result;
    }

    private sealed record RfqState(Guid Id, string OrganizationId, string Number, string Status, long Version,
        string Correlation, long LineCount, long HistoryCount);

    private static async Task<RfqState> CaptureRfqStateAsync(NpgsqlConnection connection, Guid id)
    {
        await using var command = new NpgsqlCommand("""
            SELECT r."Id",r."OrganizationId",r."RfqNumber",r."Status",r."Version",r."TransitionCorrelationId",
              (SELECT count(*) FROM nexa.request_for_quotation_lines l WHERE l."RequestForQuotationId"=r."Id"),
              (SELECT count(*) FROM nexa.purchase_transaction_status_history h WHERE h."EntityType"='RFQ' AND h."EntityId"=r."Id")
            FROM nexa.request_for_quotations r WHERE r."Id"=@id
            """, connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("The exact owned RFQ state is missing.");
        var state = new RfqState(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetInt64(4), reader.GetString(5), reader.GetInt64(6), reader.GetInt64(7));
        if (await reader.ReadAsync()) throw new InvalidOperationException("The exact owned RFQ state is ambiguous.");
        return state;
    }

    private static Task<string> CapturePoStateAsync(NpgsqlConnection connection, Guid id) =>
        ScalarStringAsync(connection, """
            SELECT jsonb_build_object(
              'purchaseOrder',to_jsonb(p),
              'lines',(SELECT coalesce(jsonb_agg(to_jsonb(l) ORDER BY l."Id"),'[]'::jsonb)
                       FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId"=p."Id"))::text
            FROM nexa.purchase_orders p WHERE p."Id"=@id
            """, null, ("id", id));

    private static Task<string> CaptureHistoryStateAsync(NpgsqlConnection connection) =>
        ScalarStringAsync(connection, """
            SELECT jsonb_build_object(
              'status',(SELECT coalesce(jsonb_agg(to_jsonb(h) ORDER BY h."Id"),'[]'::jsonb)
                        FROM nexa.purchase_transaction_status_history h
                        WHERE h."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH'),
              'approval',(SELECT coalesce(jsonb_agg(to_jsonb(h) ORDER BY h."Id"),'[]'::jsonb)
                          FROM nexa.purchase_transaction_approval_history h
                          JOIN nexa.commercial_comparisons c ON c."Id"=h."CommercialComparisonId"
                          WHERE c."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH'),
              'po',(SELECT coalesce(jsonb_agg(to_jsonb(h) ORDER BY h."Id"),'[]'::jsonb)
                    FROM nexa.purchase_order_history h
                    JOIN nexa.purchase_orders p ON p."Id"=h."PurchaseOrderId"
                    WHERE p."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH'))::text
            """, null);

    private static async Task<(Guid Id, long Version)> ApprovedPoAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SELECT \"Id\",\"Version\" FROM nexa.purchase_orders WHERE \"OrganizationId\"='REV869B-PG-SELF-OWNED-GRAPH' AND \"Status\"='Approved' AND \"IsCurrentVersion\" AND \"IdempotencyKey\" LIKE 'rev869b-pg-owned:%'", connection);
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
            WHERE p."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH' AND p."Status"='Rejected' AND NOT p."IsCurrentVersion"
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
        var fields = evidence.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fields.Length == 2)
        {
            Assert.Equal(fields[0], error.TableName);
            Assert.Equal(fields[1], error.ColumnName);
        }
        else
        {
            Assert.Equal(evidence, error.ConstraintName, ignoreCase: true);
        }
    }

    private static Guid DeterministicId(string scenario, string entity)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("REV869B-DIRECT|" + scenario + "|" + entity));
        return new Guid(bytes[..16]);
    }

    private static long DeterministicSequence(Guid id) => 10_000_000_000L + BitConverter.ToUInt32(id.ToByteArray(), 0);

    private static async Task<int> InsertPoHistoryAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid poId,
        string action, string fromStatus, string toStatus, int revision, long version, string correlation)
    {
        var actor = await ScalarGuidAsync(connection, """SELECT "EmployeeId" FROM nexa.employee_identity_mappings WHERE "OrganizationId"=@organization AND "Subject"=@login""", transaction,
            ("organization", Rev869BOwnedPostgresDatabase.Organization), ("login", Rev869BOwnedPostgresDatabase.Login));
        var historyId = DeterministicId(poId.ToString("N"), correlation);
        await Rev869BOwnedPostgresDatabase.SetCommandContextAsync(connection, transaction, actor, "PURCHASE_MANAGER",
            new Rev869BOwnedPostgresDatabase.ExactSlot("purchase_order_history", historyId, "PurchaseOrder", poId,
                action, version, fromStatus, toStatus, correlation, "Deterministic owned lifecycle evidence"));
        return await ExecuteAsync(connection, """
            INSERT INTO nexa.purchase_order_history
              ("Id","PurchaseOrderId","Action","FromStatus","ToStatus","RevisionNumber","ActorEmployeeId","ActorLoginId","ActorRoleCode","Reason","CorrelationId","CreatedAt","CreatedBy","Version")
            SELECT @id,@po,@action,@from,@to,@revision,m."EmployeeId",@login,'PURCHASE_MANAGER','Deterministic owned lifecycle evidence',@correlation,statement_timestamp(),@login,@version
            FROM nexa.employee_identity_mappings m
            WHERE m."OrganizationId"='REV869B-PG-SELF-OWNED-GRAPH' AND m."Subject"=@login AND m."IsActive"
            """, transaction, ("id", historyId), ("po", poId), ("action", action),
            ("from", fromStatus), ("to", toStatus), ("revision", revision), ("login", Rev869BOwnedPostgresDatabase.Login),
            ("correlation", correlation), ("version", version));
    }

    private static async Task<int> InsertPoStatusHistoryAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid poId,
        string action, string fromStatus, string toStatus, long version, string correlation)
    {
        var actor = await ScalarGuidAsync(connection, """SELECT "EmployeeId" FROM nexa.employee_identity_mappings WHERE "OrganizationId"=@organization AND "Subject"=@login""", transaction,
            ("organization", Rev869BOwnedPostgresDatabase.Organization), ("login", Rev869BOwnedPostgresDatabase.Login));
        var historyId = DeterministicId(poId.ToString("N"), correlation + ":status");
        await Rev869BOwnedPostgresDatabase.SetCommandContextAsync(connection, transaction, actor, "PURCHASE_MANAGER",
            new Rev869BOwnedPostgresDatabase.ExactSlot("purchase_transaction_status_history", historyId, "PurchaseOrder", poId,
                action, version, fromStatus, toStatus, correlation, "Deterministic owned lifecycle evidence"));
        return await ExecuteAsync(connection, """
            INSERT INTO nexa.purchase_transaction_status_history
              ("Id","OrganizationId","EntityType","EntityId","DocumentNumber","Action","FromStatus","ToStatus","ActorEmployeeId","ActorLoginId","ActorRoleCode","Remarks","CorrelationId","CreatedAt","CreatedBy","Version")
            SELECT @id,p."OrganizationId",'PurchaseOrder',p."Id",p."PoNumber",@action,@from,@to,m."EmployeeId",@login,
              'PURCHASE_MANAGER','Deterministic owned lifecycle evidence',@correlation,statement_timestamp(),@login,@version
            FROM nexa.purchase_orders p JOIN nexa.employee_identity_mappings m
              ON m."OrganizationId"=p."OrganizationId" AND m."Subject"=@login AND m."IsActive"
            WHERE p."Id"=@po
            """, transaction, ("id", historyId), ("po", poId),
            ("action", action), ("from", fromStatus), ("to", toStatus), ("login", Rev869BOwnedPostgresDatabase.Login),
            ("correlation", correlation), ("version", version));
    }

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
