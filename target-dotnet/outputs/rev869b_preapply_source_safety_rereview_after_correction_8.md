# REV869B independent pre-apply source-safety re-review after correction 8

## Canonical verdict

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```

Correction 8 materially improves test isolation, qualification provenance structure, mapped-pipeline coverage, rollback snapshots, and runtime inventory. It does not close the source-safety gate. The new qualification workflow is unreachable because its database update trigger expects `PendingApproval` while the application and insert trigger use `Pending Approval`. Transaction-local settings and matching histories remain SQL-caller-fabricable, several future PostgreSQL tests cannot reach the intended guard, and owned-database cleanup and exact ownership evidence are not yet safe enough for a helper.

PostgreSQL tests were compiled/listed only and were **NOT RUN**. No PostgreSQL server, database helper, migration apply/remove operation, backup, restore, production resource, `sess_nexaerp`, REV861, frontend, REV869C, AWS resource, or `../legacy-reference/` was accessed.

## Entry gate and commit topology

All entry conditions passed before review:

- Starting commit: `7fd9539421a59793a311f22ff877383ea0b0db5e`.
- Main correction: `c961eff21b44f95749c967ec192091d49a9c40dd`.
- Ending commit/HEAD reviewed: `624ca346028589022654136b5e4861cf099fb419`.
- HEAD parent: `c961eff21b44f95749c967ec192091d49a9c40dd`.
- The starting commit is the direct parent of the main correction and an ancestor of HEAD.
- Initial target-scoped Git status was clean.
- Neither reviewed commit contains a `legacy-reference` path. This was established from commit path lists without accessing that directory.

### Main correction file list

Commit `c961eff21b44f95749c967ec192091d49a9c40dd` changes exactly these 12 files:

1. `outputs/rev869b_source_correction_checkpoint_8.md` (added)
2. `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs`
3. `src/SESS.NexaERP.Application/Rev869A/Rev869AContracts.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
6. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCompleteGraphSeeder.Transactions.cs` (added)
8. `tests/SESS.NexaERP.Tests/Rev869BCompleteGraphSeeder.cs` (added)
9. `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs` (added)
10. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
12. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`

### Ending formatting commit file list and semantic analysis

Commit `624ca346028589022654136b5e4861cf099fb419` changes only:

1. `outputs/rev869b_source_correction_checkpoint_8.md`

Its entire patch deletes one trailing blank line. It changes no executable source, SQL, migration, test, configuration, hash-bearing content, claim, or behavior. The complete range therefore has 12 unique changed files, not 13. The checkpoint's reported committed-file count of 12 is correct. No unrelated path was found.

## Finding-by-finding review

| Review area | Verdict | Independent result |
|---|---|---|
| Workflow/status transitions | **FAIL** | Qualification INSERT requires `Pending Approval`, but both UPDATE branches require `PendingApproval`; verify/approve cannot pass the trigger. Other REV869B transition allowlists are materially improved. |
| One-record/one-process | **PARTIAL** | Unique keys and version checks remain, but caller-controlled correlation does not establish an authoritative command identity. The retained-qualification replacement query can admit overlapping actorless approved ranges. |
| Organization/record scope | **FAIL** | Purchase endpoints use record-scope controls, but the new qualification endpoints use organization filtering and page permission only; they do not call `IRecordScopeAuthorizer`/the employee scope filter. |
| Segregation of duties | **FAIL** | Creator/verifier and verifier/approver checks exist but are unreachable at the DB boundary. Technical-verifier/comparison-approver and submitter/approver checks exist. A distinct PO issuer-versus-approver control is not established. |
| Permissions/direct API | **FAIL** | Purchase mapped coverage improved. Qualification lifecycle has no mapped/direct API tests for its required status, scope, audit, and rollback behavior. Attachment/export tests deny missing records through page permission, not existing records through record scope. |
| Approval-value routing | **PARTIAL** | Existing comparison/PO routing remains source-covered; this correction adds no runtime evidence. PostgreSQL behavior remains unexecuted. |
| Commercial/GST/payable | **PARTIAL** | Exact fail-closed reconciliation is substantially present, including `IS TRUE`/`IS DISTINCT FROM` patterns and qualification joins. The mapped test does not prove commercial-value masking. |
| Qualification provenance | **FAIL** | Snapshot/live/history reconciliation is stronger, but matching histories and transaction-local actor context can still be fabricated by a SQL caller. The lifecycle that should create authoritative histories is unreachable. |
| Immutable snapshots/histories | **PARTIAL** | Immutable guards and inventory are expanded. The rejected-PO lifecycle test never forces deferred constraints and omits required generic status histories, so its apparent coverage can falsely pass. |
| Database mutation controls | **FAIL** | The qualification literal defect blocks legitimate updates. Direct SQL can set the same custom GUCs used by the service and insert matching parent/history data. |
| Idempotency/rollback/concurrency | **FAIL** | Independent contexts and a third verifier are present. One rollback test still accepts a scalar aggregate, concurrency releases before service entry rather than at the contested DB boundary, and exhaustive loser-state reconciliation is incomplete. |
| PostgreSQL ownership/isolation | **FAIL** | Per-method disposable clones and owned seed graphs are a major improvement. Cleanup is not retry-safe or replacement-safe, app clones are not verified before use, deterministic names collide, and interrupted databases are unrecoverable. |
| Migration Up/Down/preservation | **PARTIAL** | ID/order/model parity and offline inventory reproduce. Down is scoped to REV869B objects, but runtime compilation/atomicity/preservation were not run and cannot be accepted. |
| Execution-helper readiness | **FAIL** | Unsafe cleanup identity/retry/interruption properties and invalid future-test paths prohibit helper design approval. |

## Complete fixture-ownership analysis

### Common fixture profiles

**D — direct complete-graph profile.** `Rev869BOwnedPostgresDatabase.CreateAsync([CallerMemberName])` requires opt-in and the source connection string database name `sess_nexaerp_rev869b_verify`, derives a deterministic `rev869b_direct_<20 hex>` target, requires exact absence, clones the source with pooling disabled, checks `current_database()`, checks the retained migration once, and calls `Rev869BCompleteGraphSeeder.SeedAsync`. The seeder owns an organization, category, UOM, item, vendor, approved qualification plus Verify/Approve configuration histories, warehouse, Rack/Bin, tax rule, three identity mappings, PR, PR line, procurement handoff, all 15 REV869B relations, histories, audit/idempotency-shaped values, number-series state, and Material Follow-up. IDs and business timestamps are SHA-256/fixed-time derived. It relies on accepted template employees and their role/permission assignments rather than creating them. Each verification connection is opened against the target and rechecks `current_database()`. Disposal force-drops the target database.

**A — application RFQ profile.** `OwnedRfqFixture.CreateAsync(scenario)` requires the same opt-in/source name, derives a deterministic disposable target, and creates its own organization, identity mapping, warehouse, PR, PR line, and procurement handoff. The service creates the tested RFQ, line, status history, audit, idempotency, and numbering data. Vendor, qualification, UOM, Rack/Bin, invitation, quotation/line, technical verification, comparison, PO, approval/PO histories, and Material Follow-up are not required by these RFQ-only cases and are absent. IDs are SHA-256 derived; fixed fixture times are used, while production timestamps are server/service controlled. Verification is through the fixture context or a named independent/third DbContext. `OwnedDatabaseLease.DisposeAsync` drops the target in fixture `finally`.

The literal `REV869B-PG-OWNED-DATABASE-GUARDS` is absent from the executable PostgreSQL suites (it appears only in a source assertion that it must be absent). `REV869B-PG-DIRECT-TEST-OWNED` remains in 22 executable direct-suite locations as the internally seeded organization. It no longer denotes an externally prepared business graph, but retaining the explicitly rejected legacy-style fixture literal prevents an unqualified prohibited-dependency PASS. Current PostgreSQL test source contains no `GetHashCode()` or `gen_random_uuid()`. `now()`/`statement_timestamp()` uses are server-controlled event timestamps. `DELETE FROM` occurs only in negative immutable-delete tests, not cleanup.

### Per-method ownership matrix

Legend: graph column order is **vendor/qualification/UOM/warehouse+RackBin/PR+RFQ/invitation/quotation+lines/technical/comparison/PO/histories/follow-up**. `D-full` means every item is seeded by D; `A-RFQ` means only the RFQ-required A profile is built and all later/vendor-side nodes are intentionally not required. All rows use deterministic IDs; `D-time` means fixed seed time plus server-controlled tested timestamps, and `A-time` means fixed fixture inputs plus service/server timestamps.

| PostgreSQL test method | DB ownership; organization; identities/roles | Required graph | Audit/idempotency/numbering | Time/IDs; setup; verification; cleanup | Assessment |
|---|---|---|---|---|---|
| `RealServiceTransactionPersistsParentChildHistoryAndAudit` | A clone; scenario org; owned actor mapping, accepted employee/role/permission | A-RFQ | service-owned all three | A-time; `CreateAsync("service-success")`; fixture DbContext; A disposal | Required graph owned. |
| `RealServiceFailureAfterWritesRollsBackEveryAffectedRelation` | A clone; scenario org; same A identity model | A-RFQ | fixture baseline + failed command | A-time; `CreateAsync("audit-rollback", false)`; independent context only for scalar total; A disposal | **FAIL:** scalar total can hide offsetting/field changes. |
| `RealServiceIdempotentReplayReturnsAuthoritativeOriginalWithoutDuplicates` | A clone; scenario org; same A identity model | A-RFQ | exact RFQ/line/history/audit counts | A-time; `CreateAsync("idempotent-replay")`; fixture context | Owned; replay is sequential, not concurrency evidence. |
| `RealProtectedServiceDenialHasNoBusinessMutationAndNoCrossOrganizationDisclosure` | A clone; scenario org; actor plus denying scope | A-RFQ baseline only | denial audit; no RFQ/line | A-time; `CreateAsync("scope-denial")`; fixture context | Owned; verifies service denial, not mapped cross-org response. |
| `RealProtectedServicePropagatesAuditWriterFailureWithoutFalseSuccess` | A clone; scenario org; same A identity model | A-RFQ | full typed/fingerprint pre/post | A-time; `CreateAsync("audit-propagation", false)`; independent contexts | Strong rollback design. |
| `TwoIndependentDbContextsConnectionsAndServicesProduceOneAuthoritativeWinner` | A clone; one org/key; two contexts/services, same actor | A-RFQ | one RFQ/line/status/audit; later nodes zero | A-time; `CreateAsync("concurrent-services", false)`; third context; A disposal | **PARTIAL:** start gate precedes service/DB contention; number state and all loser families are not explicitly reconciled. |
| `AuthenticatedMappedAspNetEndpointTraversesPermissionScopeServiceAndEf` | A clone; scenario org; authenticated actor, toggle page/scope/audit services | A-RFQ | success/denial audits, RFQ idempotency/numbering | A-time; `CreateAsync("mapped-endpoint")`; HTTP pipeline + fixture DbContext; app stop then A disposal | **FAIL coverage:** no commercial masking; attachment/export use nonexistent records and page denial; no qualification routes. |
| `SuccessfulTransactionPersistsAndCanBeVerified` | D clone; D org; 3 mappings + accepted employees/roles | D-full | D seed plus committed test history/audit/numbering | D-time; `OpenVerifiedAsync`; same owned connection; D disposal | Full required graph owned. |
| `FailedTransactionRollsBackWithBeforeAfterEquality` | D clone; D org/identities | D-full | canonical full-DB pre/post | D-time; D setup; independent connection/fingerprint; D disposal | Strong direct rollback design. |
| `TwoIndependentConnectionsHaveExactlyOneWinnerAndRejectStaleWriter` | D clone; same D org/actor; two connections | D-full | fresh correlation/history | D-time; D setup; peer + authoritative connection; D disposal | Independent connections; sequencing is not a simultaneous barrier. |
| `IdempotentReplayReturnsOriginalRowWithoutDuplicate` | D clone; D org/identities | D-full | test RFQ key | D-time; D setup; owned connection; rollback/D disposal | **FAIL evidence:** inserted RFQ lacks deferred Create history and transaction rolls back without forcing deferred checks. |
| `ConcurrentIdempotencyCollisionHasOneWinnerAndReturnsOriginal` | D clone; D org/identities; two connections | D-full | unique RFQ key | D-time; D setup; peer and original connection; D disposal | Exact unique index intended; second is started before first commit but no two-way DB barrier. |
| `DirectTerminalStateInsertIsRejected` | D clone; D org/identities | D-full | deterministic new keys | D-time; D setup; owned connection; rollback/D disposal | Intended transition guard likely reached; assertion object evidence is not field-exact. |
| `SnapshotMismatchIsRejectedOnIssue` | D clone; D org/identities | D-full | fresh correlation | D-time; D setup; owned connection; rollback/D disposal | Intended PO allowlist path is source-consistent. |
| `CommercialJsonTaxTotalsVersionOrganizationAndProvenanceTamperingAllReject` | D clone; D org/identities | D-full | per-attempt fresh correlation | D-time; D setup; owned connection plus postcheck; D disposal | Broad tamper coverage; object assertion remains concatenated. |
| `PermissionDenialPersistsAuditEvidence` | D clone; D org/identities/seed permission | D-full | direct denial audit | D-time; D setup; owned connection; D disposal | Does not exercise application/API permission denial. |
| `AuditFailureCausesProtectedOperationToFailAndRollback` | D clone; D org/identities | D-full | RFQ reservation + invalid audit | D-time; D setup; independent post-state query; D disposal | Exact not-null error intended; object assertion remains concatenated. |
| `SkippedAndLowerVersionsAreRejected` | D clone; D org/identities | D-full | existing RFQ state | D-time; D setup; owned connection; rollback/D disposal | Nonzero target and exact SQLSTATE intended. |
| `DirectDatabaseRejectsLateChildInsertForEveryTerminalAggregate` | D clone; D org/identities | D-full | none beyond seed | D-time; D setup; owned connection; per-attempt rollback/D disposal | **FAIL:** RFQ-line, invitation and follow-up SELECTs match zero seed rows; technical copy uses a creator/login inconsistent with command context and can hit actor binding first. |
| `ImmutableHistoryRelationsRejectUnauthorizedUpdateAndDelete` | D clone; D org/identities | D-full | seeded status/approval/PO histories | D-time; D setup; owned connection; per-attempt rollback/D disposal | Existing rows targeted; generic evidence does not distinguish update/delete trigger identity. |
| `RejectedPoRevisionResubmissionAndRepeatedRevisionKeepExactAncestry` | D clone; D org/identities | D-full | PO histories/correlations added | D-time; D setup; same connection; transaction rollback/D disposal | **FAIL:** generic status histories are omitted and deferred constraints are never made immediate or committed. |
| `ExactRev869BTriggerAndFunctionInventoryOccursOnce` | D clone; D org/identities | D-full | seeded support state | D-time; D setup; catalog query on owned connection; D disposal | Exact runtime inventory design is improved; runtime not executed. |

Every method now obtains business prerequisites from its own disposable clone/seeder or creates only its own RFQ-required graph. There is no remaining assumed pre-existing REV869B business record. The direct seeder does, however, depend on accepted template employees, roles/permissions and schema/reference data; these are explicit source/template prerequisites, not a completely self-created identity/authorization graph.

## Owned disposable database safety

| Requirement | Result |
|---|---|
| Exact approved source name and opt-in | PASS: both fixture families require `sess_nexaerp_rev869b_verify` plus exact opt-in. |
| Reject production/unexpected source | PARTIAL: exact equality rejects other names, including production/REV861-like names. It does not validate host/server provenance. |
| Prove source identity before CREATE | FAIL: direct/app creation trusts the connection-string database name; neither queries `current_database()` on the source before `CREATE DATABASE`. |
| Verify target identity/migration | Direct PASS before seed/open; application FAIL because `OwnedDatabaseLease.CreateAsync` returns without calling the separately defined `VerifiedConnectionStringAsync`. |
| Unique target per invocation | FAIL: name is deterministic per method/scenario, so simultaneous/retried invocations collide rather than receiving unique leases. |
| Discovery side effects | PASS: list/discovery does not invoke test methods or fixture creation. |
| Secrets | PASS: no embedded password/private key/AWS key marker in reviewed source. |
| Cleanup in outer `finally` | PARTIAL: callers generally use `await using`/`finally`, but disposal marks `disposed=true` before admin open/drop. A failure suppresses any retry. |
| Drop identity/ownership proof | FAIL: drop uses only the stored deterministic name; it has no durable lease token/owner marker and no re-verification that the existing database is the one this process created. |
| Concurrent database safety | FAIL: a later/replacement database with the same name could be dropped; deterministic collision prevents concurrent same-scenario execution. |
| Interrupted/partial-failure safety | FAIL: process interruption can orphan the clone; future execution rejects it and offers no ownership-safe recovery. Creation failure before lease construction can also leave a clone. |
| Immutable-history-safe cleanup | PASS: whole owned DB is dropped; business/history rows are not deleted for cleanup. Negative DELETE statements are assertions only. |
| Source DB preservation | Source code never issues DROP/ALTER against the source name, but the missing source/lease identity proofs prevent helper readiness. |

## SQLSTATE and database-object assertion matrix

`AssertPostgresGuardAsync` rejects normal completion and zero affected rows, and compares SQLSTATE when supplied. It then concatenates `ConstraintName`, `TableName`, `ColumnName`, `Where`, and `MessageText` and performs one substring assertion. Consequently an expected object token can be satisfied only by message text; the helper does not independently assert the relevant structured database fields. This fails the requirement that message parsing not be sufficient evidence.

| Negative evidence | Expected SQLSTATE/object | Row/intended-guard analysis | Verdict |
|---|---|---|---|
| Concurrent idempotency collision | `23505`; `IX_request_for_quotations_OrganizationId_IdempotencyKey` | Insert is nonzero and should reach unique index. | PARTIAL: SQLSTATE exact, object field not exact. |
| Terminal RFQ/quotation/comparison/PO inserts | `P0001`; `rev869b_enforce_transition` | Source predicates generally provide rows. | PARTIAL: concatenated evidence only. |
| PO issue snapshot tamper | `23514`; `rev869b_po_issue_allowlist` | Exact approved current PO selected. | PARTIAL. |
| JSON/tax/totals/version/org/provenance/delete tampering | declared `23514`, `P0001`, `40001`; named allowlist/immutable/delete guards | Exact PO/line targets and nonzero check exist. | PARTIAL: no structured-object-specific assertion. |
| Audit null ID | `23502`; `audit_logs`/`Id` | RFQ reservation asserts one first. | PARTIAL: table and column are not asserted independently. |
| Skipped/lower versions | `40001`; `rev869b_exact_version_increment` | Exact ID/version predicate; zero row rejected. | PARTIAL. |
| Late RFQ line | `P0001`; `rev869b_validate_child_insert` | Seeder has Draft RFQ only; terminal parent predicate matches zero. | **FAIL: intended guard not reached.** |
| Late invitation | `P0001`; `rev869b_validate_child_insert` | Seeder lacks Closed/Cancelled RFQ source. | **FAIL: zero-row path.** |
| Late quotation/comparison/PO lines | `P0001`; `rev869b_validate_child_insert` | Seed includes noneditable parents; source rows appear available. | PARTIAL. |
| Late technical verification | `P0001`; child-insert guard | Copied creator/login conflicts with command GUC actor and can reach `rev869b_command_actor_binding` first. | **FAIL: unrelated guard can intercept.** |
| Late Material Follow-up | `P0001`; child-insert guard | Seed follow-up belongs to approved current PO, while predicate requires non-Issued/noncurrent; no source row. | **FAIL: zero-row path.** |
| Immutable history update/delete | `P0001`; `rev869b_reject_immutable_mutation` | Existing status/approval/PO-history rows selected. | PARTIAL: expected identity does not distinguish exact bound trigger/table. |

The rejected-PO test is a false-positive design of a different kind: it inserts only PO-specific histories, not the required generic status histories, and rolls the transaction back without `SET CONSTRAINTS ALL IMMEDIATE` or commit. Deferred bound-history triggers therefore never have to succeed.

## Qualification lifecycle and provenance

### Lifecycle/API

- Authentication, `EmployeeId`, organization, mandatory remarks, expected version, creator separation, verifier/approver separation, Verify-before-Approve, exact `+1` version, controlled history, audit, and a wrapping EF transaction are present in application source.
- Early returns occur before `SaveChangesAsync`; async transaction disposal rolls them back, so they do not persist partial tracked changes.
- Audit is before commit, so an audit exception should roll back qualification/history changes. There is no mapped or service test proving this behavior.
- Cross-organization records are filtered out and return 404, but this has no mapped test.
- Page/action permissions are attached. Record-level scope is not: these routes do not inject `IRecordScopeAuthorizer` or attach the purchase employee-scope endpoint filter.
- A pre-read stale version returns 409, but a concurrent database-version failure is not caught/mapped and may surface as 500.
- There are no qualification tests for required 400/401/403/404/409/success, segregation, audit propagation, or rollback.

Most importantly, application source compares and writes `MasterApprovalStatuses.PendingApproval`, whose exact value is `Pending Approval`. The INSERT trigger also requires `Pending Approval`. `rev869b_guard_qualification_lifecycle`, however, requires `PendingApproval` in its verify and approve transition predicates. Every legitimate update therefore falls through to `rev869b_qualification_transition` and fails. The identity fields and histories are not reachable through the new controlled API.

The create-qualification endpoint itself saves the entity/history before calling audit without a surrounding transaction. An audit failure can therefore leave a committed qualification creation while returning failure; this remains relevant to the complete qualification lifecycle.

### Provenance reconciliation

Invitation/comparison/PO SQL now reconciles qualification ID, vendor, organization, category, version, effective range, active state, verification/approval status, verifier/approver IDs, invitation event time, and retained snapshot JSON; snapshot mutation is guarded. `rev869b_qualification_provenance_valid` requires matching Verify and Approve configuration histories with expected versions and actor mappings.

This is not yet authoritative provenance. A SQL caller can choose a valid mapped employee/login/role, call `set_config`, update the parent, and insert matching controlled histories in the same transaction. The function does not independently establish a server-issued correlation/command, full before/after state, role, remarks, or event-time identity ownership. Matching live data plus caller-fabricable histories can therefore validate fabricated-looking provenance once the status literal is corrected.

The retained actorless qualification replacement query also does not block overlap with actorless approved legacy rows even where the effective range is identical. That conflicts with its stated distinct-range policy and can create overlapping approved records.

## Correlation, history binding, and segregation of duties

- Material Follow-up correctly uses its declared `CorrelationId`; this fixes the correction-7 nonexistent-property blocker.
- Deferred history checks bind transaction ID (`xmin`/`txid_current`), parent entity/status/action/version/correlation, actor login, and required histories more tightly than before.
- The service sets transaction-local `nexa.rev869b_*` employee/login/role/organization settings. The database reads them with `current_setting`.
- These settings are not server-authoritative: any SQL caller on the connection can execute the same freely available `set_config` calls. The direct tests do exactly this. There is no protected command ledger, non-forgeable session principal, or server-issued command token.
- Initial correlation equals caller-supplied idempotency data, and later transition correlation is supplied by application/SQL. Parent and matching history can therefore still be fabricated together.
- Server timestamps and mandatory exception remarks are generally enforced, and history mutation/delete guards remain.
- Creator versus qualification verifier/approver and verifier versus approver source checks exist but are unreachable due to the literal mismatch. Technical verifier versus comparison approver and PO submitter/resubmitter versus approver checks exist. A separate PO issuer identity versus approver rule is not established.
- Seeder logins are synthetic but map to accepted template employees; it does not create shared human logins. Role/permission assignments remain template prerequisites.

## Rollback, concurrency, idempotency, and API security

The audit-propagation application test captures per-relation counts and a canonical SHA-256 database fingerprint through independent contexts before and after failure. That is strong evidence design. In contrast, `RealServiceFailureAfterWritesRollsBackEveryAffectedRelation` still compares only `CountOwnedAsync`; one removed and one added row, or an in-place mutation, could preserve that scalar.

The concurrency test creates two independent DbContexts, physical connections, and service instances, uses the same organization/key, and verifies through a third context. It covers same-payload replay and a later conflicting payload. Its `TaskCompletionSource` releases both calls before they enter the service; it is not a barrier at the contested transaction/insert boundary. The third-context assertions cover RFQ, line, Create status history, audit, and zero invitations/quotations/comparisons/POs, but not exact number-series state, all history/follow-up families, or a complete loser fingerprint. Exactly one committed attempt versus one replay is inferred rather than instrumented.

The mapped ASP.NET test genuinely traverses routing, authentication, authorization, endpoint filters, service, and EF for purchase RFQs, and covers success, 400, 401, page-permission 403, known-record cross-org 404, scope denial with an audit, audit-writer 500, and 409. It does not assert commercial-value masking. Attachment/export 403 requests use `NO-SUCH` while page permission is disabled, so they do not prove record-scope denial for an existing resource or its denial audit. The audit-failure case is a permission-denial path on a missing attachment, not a business-write rollback. The new qualification endpoints are not included in the mapped application.

## Migration, SQL, and preservation

- EF discovery with `--no-connect` returned exactly 13 migrations.
- `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation` occurs exactly once and immediately follows `20260810120000_Rev869AIdentityMasterScopeFoundation`.
- No new migration ID was introduced.
- The executable no-connect model/snapshot parity test passed 1/1.
- Offline Up SQL contains 15 `CREATE TABLE` statements, 75 trigger statements with 75 unique trigger names, 19 function-definition occurrences representing 18 unique runtime functions, 46 foreign keys, 68 indexes, and 31 checks.
- All new PL/pgSQL functions set `search_path=pg_catalog,nexa` and references are schema qualified in the reviewed SQL.
- Down removes REV869B-owned triggers/functions/tables and the two REV869B-added qualification actor columns; no earlier revision table is dropped. Removing those owned columns necessarily loses post-Up actor values, so runtime business-history preservation still requires explicit acceptance.
- Offline source indicates one migration transaction and no unrelated-data delete path. Atomic execution, PostgreSQL compilation, runtime transition behavior, and preservation of existing REV868/REV868C3/REV869A data were **NOT RUN** and are not accepted.

## Reproduced offline validation

| Validation | Independent result |
|---|---|
| Solution build `--no-restore` | PASS: 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL tests | PASS: 49/49 |
| Complete non-PostgreSQL suite | PASS: 423/423 |
| REV869B PostgreSQL discovery | PASS: exactly 22 methods compiled/listed; **NOT RUN** |
| PowerShell AST | PASS: 23 files, 0 parse errors; scripts not executed |
| EF migrations | PASS with unreachable loopback port 1 and `--no-connect`: 13, retained REV869B exactly once after REV869A |
| Model/snapshot parity | PASS: 1/1 no-connect executable test |
| `git diff --check` | PASS for the complete correction range |
| Secret scan | PASS: no private-key, AWS access-key, or embedded-password assignment marker in reviewed diff |
| Ending-tree prohibited randomness | PASS: no executable REV869B PostgreSQL `GetHashCode()` or `gen_random_uuid()` |
| Old external graph marker | PASS: no executable `REV869B-PG-OWNED-DATABASE-GUARDS`; direct owned-org literal remains and is discussed above |
| Legacy path scan | PASS: neither commit contains a legacy-reference path; directory not accessed |

Offline SQL was captured and hashed in memory, not written or applied:

| Direction | UTF-8 bytes | SHA-256 |
|---|---:|---|
| REV869A to retained REV869B Up | 187,749 | `82BC6CB3EDBF6F24413788D9121AE339C87EC7EEC0ABF5EA592B4FB045A30A6B` |
| Retained REV869B to REV869A Down | 7,417 | `5B10F990341FC2B60EE820F0AE12245347BEB04076D78E0E7C5B69CFB2A91788` |

These build/test/model/SQL results establish offline reproducibility only. They do not override the source-level failures or provide PostgreSQL acceptance.

## Blocking findings

1. Qualification verify/approve is unreachable: database UPDATE predicates use `PendingApproval`, while application/domain/INSERT state is `Pending Approval`.
2. Qualification endpoints lack record-scope enforcement and all required mapped/direct lifecycle tests; create-qualification audit is not transaction-bound.
3. Transaction-local custom settings, aggregate correlation, and matching histories remain caller-fabricable; direct SQL can manufacture apparently valid actor/provenance agreement.
4. The replacement policy can admit overlapping actorless approved qualification ranges.
5. Late-child direct tests have at least three zero-row fixture predicates, and the technical case can be intercepted by actor binding rather than the intended child guard.
6. Rejected-PO revision/resubmission omits generic status histories and never forces deferred constraints, so invalid lifecycle evidence can pass the test.
7. Database-object assertions collapse structured fields and error message into one substring, so message text alone can satisfy the expected object evidence.
8. Direct and application owned-database disposal is not retry-safe, lease-token/replacement-safe, concurrent-scenario-safe, or interruption-recoverable; application clones are not verified before use.
9. One service rollback test still uses a scalar total; concurrency is not gated at the contested DB boundary and does not exhaustively reconcile loser/number/history state.
10. Mapped coverage omits commercial masking, existing-record attachment/export scope denial/audit, and all qualification endpoints.
11. Explicit PO issuer-versus-approver separation is not established where required.
12. PostgreSQL runtime compilation and behavior remain unverified by restriction.

Any one of findings 1, 3, 5, 6, or 8 independently prevents both source-safety and execution-helper readiness PASS.

## Required ninth controlled source-only correction

1. Use the canonical qualification status constants/literals consistently in INSERT, Verify, and Approve SQL; add source-contract and mapped lifecycle tests that would fail on any literal divergence.
2. Put qualification creation and lifecycle mutations/history/audit in tested atomic transactions; enforce record scope and exact 400/401/403/404/409/success semantics, including cross-org masking, stale-race mapping, audit failure rollback, and no partial state.
3. Replace forgeable GUC-only authority with a database-verifiable, protected server-issued command/actor mechanism that direct SQL cannot manufacture; bind initial/subsequent aggregate and event correlations, histories, identities, roles, timestamps, and versions to it.
4. Enforce non-overlap for retained qualification replacements, including actorless approved legacy ranges, while preserving retained records.
5. Seed exact terminal parents for every late-child test, use exact actor/context fields, assert source-row count before mutation, and prove only the intended trigger/function can reject each statement.
6. Add both generic status and PO histories to every revision transition and force all deferred constraints before assertions/rollback; verify repeated ancestry and full histories from an independent connection.
7. Assert SQLSTATE and relevant `ConstraintName`, `TableName`, `ColumnName`, `SchemaName`, or routine context independently; never allow message text to substitute for structured object evidence.
8. Give each disposable database a unique invocation ID plus durable ownership token; query source and target identity/migration before create/use/drop; make disposal retryable and replacement-safe; support ownership-safe orphan recovery without touching the source.
9. Replace every scalar rollback assertion with typed per-relation/state/fingerprint comparison; coordinate concurrency at the contested transaction boundary and reconcile exact winner, replay/loser, number-series, histories, audit, idempotency, and all downstream zero state.
10. Add real mapped commercial masking and existing-record attachment/export record-scope denial/audit cases, plus the complete qualification pipeline.
11. Establish and test explicit PO issuer-versus-approver separation if the approved process requires it.
12. Remove/rename the retained `REV869B-PG-DIRECT-TEST-OWNED` legacy fixture literal so prohibited-dependency scans are unambiguous, while retaining fully test-owned graph creation.

Preserve the existing REV869B migration ID and all accepted REV868, REV868C3, and REV869A behavior. Do not access PostgreSQL during that correction. A new independent source-only safety re-review is mandatory afterward.

## Improvements retained

- Material Follow-up deferred history now uses the real `CorrelationId`.
- Qualification verification/approval contracts, endpoints, histories, and snapshot fields exist in source, pending correction of reachability/security.
- Qualification snapshot reconciliation now covers substantially more exact provenance fields and fail-closed Boolean conditions.
- All 15 direct methods receive a per-method disposable database and a deterministic owned graph rather than an external prepared REV869B business graph.
- Zero-row success is rejected centrally, deterministic IDs replace randomized generation, and row deletion is not used for cleanup.
- Typed/fingerprinted audit rollback evidence exists for the stronger application failure case.
- Two independent service contexts/connections and third-context verification exist.
- A genuine mapped ASP.NET purchase endpoint test now covers most core HTTP outcomes.
- Rejected-PO repeated revision source scenarios and exact runtime trigger/function inventory are present, although their deferred-history proof needs correction.
- Build, non-PostgreSQL tests, AST, EF discovery, model parity, offline SQL generation, and hashes all reproduce cleanly.

## Exact next gate

The next authorized gate is the **ninth controlled source-only REV869B correction** implementing every item above, followed by a new independent source-only safety re-review. Because both canonical states are FAIL, no PostgreSQL, helper, migration-execution, backup/restore, or production command is authorized or provided here.
