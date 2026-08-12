# REV869B pre-apply source safety re-review after controlled correction 9

Date: 2026-08-13
Review mode: independent, source-only, offline
Reviewed commit: `7da4f34fad6595194b50bf95e6c762fb1a509390`
Reviewed parent: `a235792909f3f0f9c6ac097c6fba882ccde4742e`

## Canonical state

**Source-only safety state: FAIL**
**Disposable PostgreSQL-helper readiness: FAIL**

No PostgreSQL instance was contacted. No disposable database/helper, migration apply, endpoint, web application, or runtime PostgreSQL test was executed. No production-like database or pre-existing business database was used. This report does not authorize PostgreSQL/helper commands.

The ninth correction improves several source controls, but it does not establish a safe or reachable canonical implementation. The retained migration currently emits invalid PostgreSQL SQL; the command-context boundary remains caller-forgeable; paired comparison/PO histories reuse one command claim and therefore reject valid workflows; the PostgreSQL assertion helper requires error metadata that the guards do not emit; two late-child cases select two sources while asserting one; the mapped qualification test starts a transaction inside an existing transaction; and the direct rollback test is neither typed/full-state nor independently verified.

## Entry gate and review boundary

The entry gate passed before inspection:

- `HEAD` was exactly `7da4f34fad6595194b50bf95e6c762fb1a509390`.
- Its parent was exactly `a235792909f3f0f9c6ac097c6fba882ccde4742e`.
- Target-scoped status was clean.
- The commit contained only the 14 controlled correction-9 paths listed below.
- The commit contained no `legacy-reference` path. That sibling tree was not accessed.
- `git diff --check a235792909f3f0f9c6ac097c6fba882ccde4742e..7da4f34fad6595194b50bf95e6c762fb1a509390` passed.

The complete authoritative checkpoint-9 report and the complete independent correction-8 re-review were read before this review. The exact parent-to-HEAD range contains 855 insertions and 192 deletions:

1. `outputs/rev869b_source_correction_checkpoint_9.md`
2. `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextSql.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BControlledMutationSql.cs`
6. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
7. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
8. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
9. `src/SESS.NexaERP.Infrastructure/Seed/CompleteGraphSeeder.Transactions.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs`
12. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
13. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
14. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`

## Blocking findings

### B1. The retained migration emits invalid PostgreSQL SQL

`CK_purchase_transaction_policy_dates` is missing the closing identifier quote around `EffectiveFrom`. The same malformed expression is repeated in the retained migration, EF model configuration, migration designer, and model snapshot:

```text
"EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom
```

The independently generated Up SQL consequently contains:

```sql
CONSTRAINT "CK_purchase_transaction_policy_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom)
```

This is not valid PostgreSQL SQL. The offline model/snapshot parity test passes only because the same malformed string is present in all EF metadata locations. The migration cannot be considered apply-ready or preservation-safe.

### B2. The command context is not a trusted authorization boundary

`nexa.rev869b_open_command_context` is `SECURITY DEFINER`, but all asserted identity fields—employee, login, role, and organization—are supplied by its SQL caller. It checks that the supplied identities exist, but it does not bind them to the authenticated connection principal, an immutable server-set session claim, the OIDC issuer, endpoint authorization, or record scope. A caller able to execute the function can choose another valid employee/login/role/organization tuple.

`REVOKE EXECUTE ... FROM PUBLIC` does not close the effective boundary:

- the function owner retains execution by default;
- no distinct least-privilege runtime database role or explicit grant boundary appears in the reviewed source;
- the application demonstrates that its database role must be able to execute the function; and
- if that application role is the migration/owner role, it can also manipulate the ledger or disable user triggers; if it is not, no source grant makes the application path reachable.

The direct tests call the function with identities selected by test SQL, demonstrating the caller-controlled nature of the boundary. `rev869b_claim_command_context` records a claim after matching caller-controlled parent/history values have been written; it does not validate a pre-authorized command envelope. Subject matching without issuer binding is also insufficient for an OIDC identity boundary.

Result: direct SQL forging remains possible for any role with the execution/ownership capabilities needed by the application, while a properly restricted role has no defined reachable path.

### B3. Command-claim uniqueness rejects required paired histories

`rev869b_guard_history_insert` claims a command for each generic and specialized history insert. Claim uniqueness is based on entity type, entity id, correlation, and action—not history kind. Required paired history writes therefore collide:

- comparison approval writes generic status history and specialized approval history for the same `CommercialComparison` and correlation;
- each PO transition writes PO history and generic status history for the same `PurchaseOrder` and correlation.

The second claim raises `42501` / `rev869b_command_claim_stale_or_reused`. Consequently comparison approval, PO transitions, and the rejected-PO deferred-constraint scenario are unreachable. This is a workflow regression across the PR→RFQ→quotation→comparison→PO chain, not merely a test problem.

### B4. PostgreSQL guard assertions demand metadata the guards do not emit

The central direct-test helper unconditionally requires `PostgresException.SchemaName == "nexa"`. Most custom PL/pgSQL `RAISE EXCEPTION` statements do not specify `USING SCHEMA`, so that field is normally null. Where a custom constraint name is supplied, schema metadata is still not automatically supplied.

The source-identity fallback is also inconsistent. Late-child tests expect `rev869b_validate_child_insert`, while the actual routine is `rev869b_guard_child_insert`. Immutable-delete attempts may be intercepted first by the controlled-delete trigger and report `rev869b_controlled_delete_guard`, not the immutable-update routine expected by the test.

Native unique/not-null errors can reliably populate their native object metadata, but the current custom-guard matrix cannot. Therefore many direct PostgreSQL methods would fail in the assertion helper even if the intended guard rejected the mutation.

### B5. Two late-child test cases fail their own source-cardinality precondition

The seeded graph has two purchase orders and one line under each. Both PO lines match the test's terminal-parent predicate, so the source count is 2 while the test asserts exactly 1. It also has two material-follow-up handoffs: one belongs to the approved current PO and one to the rejected/non-current PO. Both satisfy `(status <> Issued OR not current)`, again producing 2 while the test asserts 1.

These cases stop before attempting the intended child mutation. Final independent counts are only performed for RFQs and follow-ups, not for all seven child types.

### B6. The mapped qualification test nests transactions on one DbContext

The mapped endpoint fixture defaults to an ambient serializable transaction on its singleton `NexaErpDbContext`. The qualification endpoints unconditionally call `db.Database.BeginTransactionAsync`. Unlike `EfRev869BPurchaseService.BeginTransactionScopeAsync`, which reuses `CurrentTransaction`, the mapped endpoints are not ambient-aware. The mapped create request therefore attempts a second transaction on the same context/connection and should return a server error instead of the asserted creation response.

### B7. Qualification lifecycle and history binding are incomplete

- Domain defaults are Draft/Draft, but the API create path inserts Pending Approval/Pending Approval and the database insert trigger rejects Draft. No reachable Draft/configured→Pending transition is defined.
- The semantic “Verified” state is represented as `(VerificationStatus=Approved, ApprovalStatus=Pending Approval)`; there is no explicit Verified state.
- The new tuple transitions, separation-of-duties checks, scope checks, mandatory remarks, and version checks improve source reachability after creation.
- Controlled configuration history has no typed FromStatus/ToStatus fields. The deferred check does not prove that BeforeJson/AfterJson contain the exact old/new lifecycle tuple.
- History `ActorRoleCode` is not matched to the command context or re-authorized by `rev869b_require_qualification_history`.
- `ControlledConfigurationHistory.CreatedAt` remains client-generated; the server timestamp captured in the claim ledger is not exactly linked to the history row.
- Existing actorless/Draft rows remain readable, but the new guards effectively freeze updates/deletes. This is preservation by immutability, not a complete compatibility/migration policy.

### B8. Direct rollback evidence is not the requested typed independent proof

`FailedTransactionRollsBackWithBeforeAfterEquality` compares only the RFQ `Version` scalar and does so through the same connection. It does not compare typed full before/after state or an independent connection fingerprint. The application rollback tests are materially stronger, using typed state/fingerprint and an independent verifier when run without an ambient fixture transaction, but they do not repair this direct-test gap.

### B9. Raw command-ledger schema is outside EF model parity and retention policy

`rev869b_command_contexts` is created by raw migration SQL but is absent from the EF model, designer, and snapshot. The Up SQL contains 16 tables while the EF migration model represents the 15 domain tables. The no-connect model test therefore cannot establish full domain/EF/migration/designer/snapshot parity.

Successful command-ledger rows persist after token clearing. No retention/cleanup rule, immutability contract, ownership/FK model, or privacy lifecycle is defined for the stored actor, organization, claims, and remarks.

### B10. Outer application-fixture cleanup is not retryable

The shared disposable database lease marks itself disposed only after a successful verified drop and is retryable. However, the application `OwnedRfqFixture.DisposeAsync` marks the outer fixture disposed before rollback/verifier/lease cleanup. A transient cleanup failure prevents a later retry through that fixture. Hard process interruption also leaves an orphan for manual recovery, although the ownership markers make an unsafe drop less likely.

## Qualification lifecycle and compatibility matrix

| Operation/state | API/service source | Database enforcement | Independent result |
|---|---|---|---|
| Create Draft | Domain default only | Insert trigger rejects Draft | Unreachable |
| Create Pending Approval | Endpoint writes Pending Approval/Pending Approval | Can pass tuple guard with forgeable context/history | Source-reachable, trust boundary unsafe |
| Pending → Verified semantic step | Endpoint writes Approved/Pending Approval | Tuple, version, verifier≠creator, scope, remarks checks present | Reachable in isolation; “Verified” is implicit |
| Verified → Approved | Endpoint writes Approved/Approved | Approver separation, version, scope, remarks checks present | Reachable in isolation; mapped test is transaction-invalid |
| Unauthorized lifecycle mutation | Endpoint returns denial/not-found and writes audit | DB context remains caller-forgeable | Application evidence is not DB authorization proof |
| Legacy Draft/actorless row | Retained/readable | Update/delete frozen | Data preserved, operational compatibility incomplete |
| History proof | Before/After JSON plus action/version/correlation | Exact tuple, actor role, and exact server time not proven | Incomplete |

## Complete 22-method PostgreSQL discovery and evidence matrix

All 22 methods were discovered only. None was run.

### Seven application behavior methods

| # | Method / category | Fixture and transaction | State and verification | Guard/evidence outcome |
|---:|---|---|---|---|
| A1 | Successful real service transaction | High-entropy owned DB; ambient outer transaction | Creates one RFQ and inspects it in the same ambient context; fixture later rolls back | Does not independently prove committed persistence |
| A2 | Real service failure rollback | Owned DB; no ambient transaction | Typed full state plus fingerprint through independent verifier after audit failure | Strong rollback design, subject to unsafe command context |
| A3 | Idempotent replay | Ambient transaction | Same-context IDs/counts | No independent committed-state verifier |
| A4 | Protected denial | Ambient transaction | Business mutation count plus denial audit | Application denial only; DB context is forgeable |
| A5 | Audit-writer failure rollback | No ambient transaction | Typed independent before/after state/fingerprint | Strong source design |
| A6 | Concurrent same-command execution | No ambient transaction; two contexts/services; advisory transaction lock | Third verifier checks one outcome and typed deltas | Materially improved; does not cure context trust |
| A7 | Mapped qualification endpoints | Ambient transaction on singleton fixture DbContext | Extensive intended qualification, masking, and denial assertions | Fails early because endpoint begins a nested transaction |

### Fifteen direct PostgreSQL behavior methods

| # | Method / category | Connection/transaction | Mutation and post-state evidence | Independent result |
|---:|---|---|---|---|
| D1 | SuccessfulTransactionPersistsAndCanBeVerified | Owned direct DB, committed | Direct audit insert; verification on same connection | Persistence is not independently verified |
| D2 | FailedTransactionRollsBackWithBeforeAfterEquality | Owned direct DB, explicit transaction, rollback | RFQ reserve/history attempt; same-connection Version scalar | Not typed/full-state/independent |
| D3 | TwoIndependentConnectionsCompeteOnTheSameVersion | Two connections, sequential winner then stale loser | Winner commits; stale update returns zero; first connection verifies | Independent connections, but not simultaneous contention |
| D4 | IdempotentReplayReturnsSameOutcomeWithoutDuplicates | Transaction rolled back | Insert RFQ/history; constraints forced; same connection | No durable/independent proof |
| D5 | ConcurrentIdempotencyCollisionCreatesOneOutcome | Two connections | Winner commits; loser expects native unique error; first connection verifies | Native unique path is structurally plausible |
| D6 | DirectTerminalStateInsertIsRejected | Direct guarded mutation | Expects `P0001` and transition routine | Helper wrongly requires schema metadata |
| D7 | SnapshotMismatchIsRejected | Direct PO mutation | Expects `23514` / PO issue allowlist | Custom raise does not supply required schema metadata |
| D8 | Commercial snapshot/organization/total tamper | Direct comparison/PO mutations | Rolls back and checks parent on same connection | Assertion metadata is invalid; post-state not independent |
| D9 | PermissionDenialProducesAuditEvidence | Direct query plus direct audit insert | No protected application/DB denial is attempted | Not authorization evidence |
| D10 | AuditFailureRollsBackMutation | Transaction rollback | Native audit not-null violation; same-connection Version | Native object metadata is valid; state proof is narrow |
| D11 | Skipped/lower version rejection | Direct guarded mutations | Expects `40001` / exact-version constraint | Custom raise schema assertion invalid |
| D12 | Late child under terminal/non-current parent | Seven child types | Source-count precheck; only RFQ/follow-up final counts independently checked | PO/follow-up prechecks count 2; expected routine name is wrong |
| D13 | Immutable history tables | Update/delete three history types | Expects immutable routine | Schema assertion invalid; delete may hit controlled-delete guard first |
| D14 | Rejected PO deferred completeness | Manager context; generic and PO histories; force constraints/commit | Intended independent final status/history check | Second history claim fails `42501` before deferred check |
| D15 | Trigger/function inventory | Catalog count/query | Exact 75 triggers and 21 guard functions expected | Discovery only; runtime catalog was not queried |

## Expected SQLSTATE and object-identity matrix

| Scenario | Expected SQLSTATE | Expected evidence | Source-only assessment |
|---|---:|---|---|
| Idempotency collision | `23505` | Native unique index/constraint | Native metadata can support this |
| Terminal transition | `P0001` | `rev869b_enforce_transition` | `SCHEMA` is not emitted; helper fails |
| PO issue allowlist | `23514` | `rev869b_po_issue_allowlist` | Constraint may be supplied; schema is not |
| Exact version increment | `40001` | `rev869b_exact_version_increment` | Constraint may be supplied; schema is not |
| Organization/total/snapshot mismatch | `23514`/custom | Named custom guard | Required metadata is inconsistent |
| Immutable update | `P0001` | `rev869b_reject_immutable_mutation` | Routine may appear in `Where`; schema is absent |
| Controlled delete | `23514` | `rev869b_controlled_delete_guard` | May pre-empt immutable-delete expectation |
| Audit not-null | `23502` | native `nexa.audit_logs.Id` | Native metadata can support this |
| Late child | `P0001` | actual `rev869b_guard_child_insert` | Test expects nonexistent `rev869b_validate_child_insert` |
| Reused command claim | `42501` | `rev869b_command_claim_stale_or_reused` | Validly exposes the paired-history design defect |

## Disposable database safety review

Improvements independently confirmed in source:

- A 128-bit run identifier, a 24-hex database suffix, and a 256-bit ownership token are generated with cryptographic randomness.
- Source name and explicit opt-in are exact-match checked.
- The source connection verifies `current_database()` and required migration before creation.
- The administrative connection verifies it is connected to `postgres`.
- Target use and drop verify target name, current database, required migration, and a durable ownership marker/token.
- Target names use a fixed prefix and hex allowlist and explicitly deny the source, `postgres`, templates, and REV861/REV868/REV869A names.
- Pooling is disabled; ownership mismatch is quarantined; cleanup is whole-database drop, with no row-delete cleanup.
- The connection string does not embed the ownership secret.
- Direct owned connections dispose the connection before the lease in `finally`.

Remaining readiness defects:

- command-function ownership/runtime grants are undefined, making the same database role either overprivileged or unable to run the application;
- the owner can alter the marker/ledger and disable user triggers (the seeder intentionally disables them);
- outer application-fixture cleanup suppresses retry after a transient failure;
- process interruption has no automatic orphan recovery; and
- the PostgreSQL tests contain the blocking logic/assertion defects above.

Therefore the helper is not ready to be run even against a disposable PostgreSQL instance.

## Workflow, concurrency, audit, and rollback assessment

The service transaction wrapper now detects an existing EF transaction and avoids nesting for purchase-service commands. Advisory transaction locking is taken before replay lookup/number consumption for contested RFQ creation. Typed independent rollback and concurrent-delta checks in the non-ambient application tests are meaningful improvements. Audit writes occur before commit and should participate in rollback when they share the context/transaction.

Those improvements do not make the workflow canonical:

- comparison approval and all PO transitions are rejected by duplicate claim reuse;
- qualification mapped behavior is blocked by nested transactions;
- direct rollback is narrow and same-connection;
- direct “permission denial” merely queries permission and writes an audit row itself;
- command identity remains caller-selected; and
- exact history semantics, role binding, and server timestamp binding are incomplete.

The PR→RFQ→vendor invitation→quotation→technical verification→commercial comparison→PO chain is therefore not proven end-to-end and is source-unreachable at comparison/PO paired history boundaries.

## Migration, preservation, and offline inventory

Authorized offline validations completed successfully unless noted:

- solution build with `--no-restore`: PASS, 0 warnings, 0 errors;
- focused non-PostgreSQL REV869B tests: PASS, 51/51;
- complete non-PostgreSQL tests: PASS, 425/425;
- PostgreSQL discovery: PASS, exactly 22 methods, not run;
- PowerShell AST parse: PASS, 23 tool scripts, 0 parse errors, none executed;
- EF migration list using a non-routable connection and `--no-connect`: PASS, exactly 13 migrations;
- no-connect EF model/snapshot test: PASS, 1/1, but does not detect the repeated malformed SQL string or raw ledger table omission;
- Up and Down SQL: generated in memory only; no file output and no database connection;
- secret/prohibited-operation scans: no private key, AWS access key, or password assignment; no executable production/AWS/reset/drop/truncate helper command found in the reviewed range;
- no old fixed fixture literal, executable `GetHashCode`, or test/seeder random UUID mutation identity remained; `gen_random_uuid()` is intentionally used by the command-context SQL;
- `git diff --check`: PASS.

The 13-migration sequence ends with:

12. `20260810120000_Rev869AIdentityMasterScopeFoundation`
13. `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`

In-memory SQL fingerprints:

- Up: 195,395 bytes; SHA-256 `C40F059851E783B5F752F00A5AD3A3CC7B605E183934D73F3865C4FCCD12C76F`
- Down: 7,718 bytes; SHA-256 `0A7C2785DDA0208FB390F41D6088AC580F4E7DE3FFA731C96B7B18FAC11E2A0C`

Up inventory: 16 created tables, 75 unique triggers, 22 function-definition occurrences representing 21 unique functions, 46 foreign keys, 68 indexes, and 32 check occurrences (30 named unique checks plus two unnamed replacement checks).

Created tables:

`commercial_comparison_lines`, `commercial_comparisons`, `material_followup_handoffs`, `purchase_order_history`, `purchase_order_lines`, `purchase_orders`, `purchase_transaction_approval_history`, `purchase_transaction_approval_policies`, `purchase_transaction_status_history`, `quotation_technical_verifications`, `request_for_quotation_lines`, `request_for_quotations`, `rev869b_command_contexts`, `rfq_vendor_invitations`, `vendor_quotation_lines`, `vendor_quotations`.

Unique functions:

`rev869b_claim_command_context`, `rev869b_command_context_valid`, `rev869b_commercial_snapshot_reconciles`, `rev869b_enforce_quotation_transition`, `rev869b_enforce_transition`, `rev869b_guard_authoritative_transition`, `rev869b_guard_child_insert`, `rev869b_guard_controlled_snapshot`, `rev869b_guard_explicit_mutation`, `rev869b_guard_extended_immutability`, `rev869b_guard_history_insert`, `rev869b_guard_qualification_lifecycle`, `rev869b_open_command_context`, `rev869b_qualification_provenance_valid`, `rev869b_reject_controlled_delete`, `rev869b_reject_immutable_mutation`, `rev869b_reject_overlapping_approval_policy`, `rev869b_require_bound_history`, `rev869b_require_qualification_history`, `rev869b_validate_parent_contract`, `rev869b_write_policy_history`.

The raw command-context table accounts for the difference between the 16 SQL-created tables and the 15 EF-modeled domain tables. Existing REV869B business tables/migration identity remain retained, but invalid check SQL and freeze-only legacy qualification behavior prevent a preservation PASS.

## Reviewed file hashes

| Bytes | SHA-256 | Path |
|---:|---|---|
| 7,322 | `121535F34F2139A3916013A5C79ADEF94136DEDA2E1661676A1C4B66B57CEBBA` | `outputs/rev869b_source_correction_checkpoint_9.md` |
| 32,693 | `37D934929F774A481A0EBC8700453A10064D46FFAE9245696C09E04CD6784C79` | `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs` |
| 148,156 | `6CB16F18BD1A3D6ABEE02A413AC806FC6EAA7057FFBCEDC030AC597803623C81` | retained REV869B migration |
| 7,659 | `E10050C97B52A39FDABF534C683A1F095130BCAF44E354AD61F9CAEC2F9942E7` | `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextSql.cs` |
| 54,135 | `D2FDF1EF94C447155D2B91DB0768CA0EA70F6D6D138746A5BE3635821547BD31` | `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BControlledMutationSql.cs` |
| 52,105 | `CB1109EB3873449E2D7335C0E3767CA2B528D50599E722F4B734817CFE38AF29` | `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs` |
| 33,327 | `2AF89BC6A245AE50BC7CEDEE96B677354F351B67336FE0C00847748739A80072` | `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs` |
| 24,799 | `4C666E0A4B4BA7EABAA9E0B215ABC22D6E22DEF7672D4DE773B366423E31FCD4` | `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs` |
| 15,103 | `2113BAB686276F8F57A2524B02CDA23D921D5D2A87AACCC9ED326996DDCFF8D8` | `src/SESS.NexaERP.Infrastructure/Seed/CompleteGraphSeeder.Transactions.cs` |
| 12,695 | `73D94037F0F00747A55CA858460FB2CF56B23A50EF8CE365D0C514CEF8200BB0` | `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs` |
| 1,872 | `ACDBCFCE9687CEBF8FE5549C9503DBC28CCF43AA84C65E485F517120DCCE9A7D` | `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs` |
| 48,601 | `63E807E8C49930A3C2D1C5CED92C4A9B09C345E6992290D7B35B3A9C3E76E2C6` | `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs` |
| 50,886 | `BAF276E49EB50A8EBB7CD53B3DC2947DF272700C0A3A0C0B90B88E152DFCC334` | `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs` |
| 11,630 | `3FA9728B16FB1D7F5E42C01C0D564D37180693189CE3B08ED840E040788CEB9C` | `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs` |

## Required next controlled correction

A tenth controlled source-only correction is required before any PostgreSQL/helper command can be considered. At minimum it must:

1. repair `CK_purchase_transaction_policy_dates` consistently in the retained migration, model, designer, and snapshot, and add an offline SQL syntax/content guard that cannot pass when all metadata repeat the same malformed text;
2. replace the caller-supplied command identity with a demonstrably trusted, issuer-bound and least-privilege database authorization boundary, with separate migration/owner and runtime roles and explicit grants/revokes;
3. redesign command claims so required generic/specialized paired histories are both authorized and independently single-use without allowing replay;
4. make qualification creation/lifecycle explicit and reachable, and bind exact old/new tuples, actor role, issuer/subject, correlation, and database time to immutable history while defining legacy-row compatibility;
5. align custom PostgreSQL `RAISE` metadata and the assertion helper, including the actual child-guard routine and trigger-order outcomes;
6. select exactly one deterministic late-child source for all seven cases and independently verify every final before/after count;
7. make mapped qualification endpoints ambient-transaction aware or create that fixture without an ambient transaction;
8. add typed full-state rollback equality through an independent connection for direct behavior;
9. make outer fixture cleanup retryable and define safe orphan recovery; and
10. bring the raw command-ledger table into an explicit parity/ownership/immutability/retention contract.

After that correction, repeat an independent source-only re-review from a clean exact entry gate. Because both canonical states are FAIL, this report intentionally supplies no PostgreSQL or helper execution commands.
