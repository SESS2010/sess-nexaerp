# REV869B Fifth Independent Source-Safety Re-review After Correction 5

Date: 2026-08-12 (Asia/Calcutta)

## Review identity and boundary

- Correction commit: `8e929f48c6abebda510205defb7bf2c3214cae18`.
- Parent commit: `b510a4963ec95258f4a3ffc1bd3610f2371ef95d`.
- Exact reviewed range: `b510a4963ec95258f4a3ffc1bd3610f2371ef95d..8e929f48c6abebda510205defb7bf2c3214cae18`.
- Authoritative inputs read: `outputs/rev869b_preapply_source_safety_rereview_after_correction_4.md`, `outputs/rev869b_source_correction_checkpoint_5.md`, and `outputs/rev869a_isolated_final_acceptance_checkpoint.md`.
- Method: independent source/diff review, table-by-table trigger analysis, all-test-body review, offline build and non-PostgreSQL execution, no-connect EF/model checks, and independently regenerated offline SQL.
- PostgreSQL-backed tests were compiled/listed and **NOT RUN**. No PostgreSQL connection, database helper, migration application/removal/creation, database creation, backup, restore, production, REV861, frontend, REV869C, AWS, or legacy-reference operation was performed.

The source-safety gate remains closed. Correction 5 removes the false-is-non-null quotation consumer, fixes function search paths, adds four mutation triggers, and adds useful source test designs. It does not create the direct-test fixture, its exact-one fixture rules are internally incompatible with the states the suite requires, its service concurrency case commits no winner, and material lifecycle/history/mutation coverage remains incomplete.

## Entry verification and exact correction scope

| Check | Result | Independent evidence |
|---|---|---|
| HEAD | PASS | Exact requested correction commit. |
| Parent | PASS | Exact requested parent commit. |
| Range scope | PASS | 9 files, 556 insertions, 75 deletions. |
| Initial target status | PASS | `git status --short -- .` returned no entries. |
| Whitespace | PASS | Exact-range `git diff --check` returned no findings. |
| Accepted migrations | PASS | No REV868, REV868C3, or REV869A migration path changed. |
| Legacy isolation | PASS | The range has zero `../legacy-reference/` paths; that directory was not accessed or changed. |
| Source-only boundary | PASS | No database/helper/migration execution operation occurred. |

Exact range paths:

1. `outputs/rev869b_source_correction_checkpoint_5.md`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseLifecycleSql.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs`

## Canonical review disposition

| Area | State | Independent conclusion |
|---|---|---|
| Fifteen-table mutation control | **FAIL** | RFQ, quotation, comparison, and PO DELETE remain unguarded; approval-policy DELETE/version/audit control is absent. RFQ and PO same-status +1 updates can change business/lifecycle fields, and quotation same-status updates can toggle current-revision state. |
| Child/snapshot immutability | **FAIL** | Four named gaps gained triggers, but comparison-line DELETE is prohibited even in the declared editable states, and follow-up UPDATE is prohibited without any controlled operational handoff transition/history path. |
| Boolean reconciliation | **PASS semantically; FAIL contract coverage** | The removed quotation safety trigger eliminates `FALSE IS NOT NULL`; quotation uses `IS TRUE`, authoritative matched rows use SQL truth filtering, and mismatch counters use `IS NOT TRUE`. Retained critical comparison SQL still contains nullable `taxRule <>` source, and no runtime test separately proves FALSE and NULL rejection. |
| JSON/cardinality/provenance | **FAIL** | Strong canonical and independent counters exist, but qualification snapshot provenance is not tied to an exact qualification row/version, history remains fabricable, and the tamper test is intercepted by immutable-line triggers rather than proving the named semantic guards. |
| Trigger/function architecture | **FAIL** | Inventory, fixed search paths, static qualification, and teardown reproduce. Semantic mutation and history gaps remain; inventory equality is not safety acceptance. |
| PostgreSQL fixture safety | **FAIL** | Application prerequisites are scenario-owned, but the direct suite merely requires undeclared pre-existing `REV869B-PG-OWNED-DATABASE-GUARDS` rows and creates none. Exact-one RFQ/PO requirements are incompatible with the states used across tests. |
| Rollback proof | **FAIL** | The service now owns its failure transaction, but post-failure verification reuses the same DbContext/connection and does not independently verify numbering or every touched supporting relation. |
| Genuine concurrency | **FAIL** | Two DbContexts/connections/services exist, but writer one is deliberately rolled back, writer two is also rolled back, no committed authoritative winner exists, no loser conflict/replay is asserted, and coordination is a 100 ms timing assumption. |
| Mapped endpoint | **PASS source design; NOT RUN** | The new case starts an ASP.NET application, authenticates, traverses mapped authorization/permission/scope filters, invokes the real EF service, and checks persistence. Live behavior remains unaccepted because PostgreSQL tests were not run. |
| Rejected-PO lifecycle | **FAIL** | Coverage is direct SQL, not service-level; it omits the requested complete Draft/submission/rejection/revision/resubmission history/audit/permission lifecycle and is dependent on an impossible exact-one PO fixture. |
| Previously passed controls | **PASS within offline boundary** | Build, 422 permitted tests, EF discovery/model parity, accepted migration isolation, exact +1/CAS/409/permission/decimal source controls, and REV868/REV868C3/REV869A regressions remain green. |

## Fifteen-table INSERT/UPDATE/DELETE matrix

| Relation | Permitted source behavior | Rejected behavior | Material result |
|---|---|---|---|
| `purchase_transaction_approval_policies` | INSERT/UPDATE when the active amount/date range does not overlap | Overlapping active policy | **FAIL:** no exact +1/actor/timestamp update rule and no DELETE guard/history. |
| `purchase_transaction_status_history` | INSERT when current parent organization/status, actor login, and a five-second timestamp window match | UPDATE/DELETE | **FAIL:** `FromStatus`, action, actor employee/role, correlation, and same-command identity are not bound. |
| `request_for_quotations` | INSERT Draft; exact +1 allowed transition or same-status update | Illegal status transition; organization/PR mutation | **FAIL:** INSERT does not explicitly require version 0, same-status updates can alter RFQ terms after issue, and DELETE is unguarded. |
| `request_for_quotation_lines` | INSERT under exact Draft/version-0 RFQ | All UPDATE/DELETE; late INSERT | PASS for immutable-line policy, but no editable Draft correction path remains. |
| `rfq_vendor_invitations` | INSERT under Issued RFQ; exact +1 lifecycle/status-audit update | Qualification/provenance UPDATE; DELETE; illegal transition | Qualification snapshot is materially protected. INSERT does not explicitly require version 0 and same-status audit metadata remains client-controlled. |
| `vendor_quotations` | INSERT current Draft/version 0; exact +1 lifecycle transitions | Provenance/commercial update; illegal transition | **FAIL:** same-status +1 can toggle `IsCurrentRevision`; DELETE is unguarded. |
| `vendor_quotation_lines` | INSERT under current Draft/version-0 quotation | UPDATE/DELETE; late INSERT | PASS for immutable line behavior. |
| `commercial_comparisons` | INSERT Draft; exact +1 Draft/approval/revision lifecycle; Draft/RevisionRequested edits | Post-boundary snapshot mutation; illegal transition | **FAIL:** INSERT does not explicitly require version 0 and aggregate DELETE is unguarded. |
| `commercial_comparison_lines` | INSERT under Draft/version-0 comparison; UPDATE while parent Draft/RevisionRequested | Post-boundary UPDATE; every DELETE | **FAIL:** DELETE has no editable-boundary exception, so it does not implement the declared Draft/RevisionRequested edit model. |
| `quotation_technical_verifications` | INSERT under Submitted quotation | UPDATE/DELETE; late INSERT | PASS for immutable evidence behavior. |
| `purchase_orders` | INSERT Draft/RevisionDraft with rejected-predecessor check; exact +1 lifecycle | Provenance parent mutation; illegal transition | **FAIL:** INSERT version 0 is not explicit; same-status +1 can alter `IsCurrentVersion` and issue/cancel metadata; DELETE is unguarded. |
| `purchase_transaction_approval_history` | INSERT when comparison status/route, action family, actor login, and five-second window match | UPDATE/DELETE | **FAIL:** transition command, from-state, employee/role authorization, and correlation are not bound. |
| `purchase_order_history` | INSERT when PO status/revision, actor login, and five-second window match | UPDATE/DELETE; creator self-approval by login | **FAIL:** after-the-fact fabrication remains possible and action/from-state/role/correlation are not transition-bound. |
| `purchase_order_lines` | INSERT under Draft/RevisionDraft version-0 PO | UPDATE/DELETE; late INSERT | PASS for immutable-line behavior. |
| `material_followup_handoffs` | INSERT under exact current Issued PO | Every UPDATE/DELETE; late INSERT | **FAIL:** `PendingFollowUp` to `Closed`/`Cancelled` has no exact +1 actor/timestamp/history transition, despite those statuses being modeled. |

Foreign-key restrictions do not replace a direct mutation contract: an empty or detached aggregate can still lack the requested DELETE rejection. Ordinary owner/superuser trigger disabling is outside proof and is not claimed safe.

## Boolean, JSON, canonical calculation, and provenance review

The quotation transition now counts a line only when `rev869b_commercial_snapshot_reconciles(...) IS TRUE`. Comparison/PO authoritative matched queries use the Boolean function directly in a `WHERE` predicate, which admits only TRUE, and named mismatch queries use `IS NOT TRUE`, which catches FALSE and NULL. The former defective `) IS NOT NULL;` consumer and duplicate quotation authoritative trigger are absent.

The canonical function remains fail closed: it validates object shape, uses exact relational quotation/tax inputs, recomputes gross, assessable, taxable, each tax component and payable with PostgreSQL `numeric`, enforces scale/capacity/rate/effective/organization/currency/HSN/state/registration/exemption/reverse-charge rules, compares complete input/result/tax JSON, and returns FALSE on any exception. PostgreSQL `round(numeric, scale)` remains aligned with the application midpoint-away-from-zero intent.

Material remaining defects:

- The retained comparison guard still contains `cl."CommercialSnapshotJson"->'taxRule' <> ql."TaxRuleSnapshotJson"`; missing/JSON-null evidence makes this nullable predicate UNKNOWN. The newer authoritative guard rejects the same case, but the requested absence of a nullable critical path is not achieved.
- The structural test checks source substrings. It does not execute separate TRUE, FALSE, SQL NULL, missing, JSON null, malformed, and wrong-type cases.
- Qualification evidence proves a live eligible qualification join and a two-field invitation object, but the snapshot is not tied to an exact qualification ID/version/effective record. A fabricated eligible/checkedAt object can satisfy that portion while a live row exists.
- The five-second history rule uses application-writable parent/history timestamps and updated login. It is not transaction- or command-bound and does not validate exact `FromStatus`, action/status mapping, employee identity, role authority, or correlation.
- Named comparison/PO mismatch counters are now separately queried, a real improvement. The runtime test matrix does not isolate each named category or assert the expected SQLSTATE/trigger/function.

## Trigger/function inventory and architecture

Independently regenerated Up SQL contains 15 tables, 38 trigger occurrences/38 unique triggers, 11 function occurrences/11 unique functions, 44 foreign keys, 66 indexes, 31 check-constraint creation occurrences/29 unique check names. All 11 installed REV869B functions explicitly set `search_path = pg_catalog, nexa`. No unsafe dynamic SQL, `SECURITY DEFINER`, session-setting bypass, trigger disabling, recursion, or `pg_temp` resolution path was found.

The table map confirms the uncovered DELETE controls: no DELETE trigger exists on `request_for_quotations`, `vendor_quotations`, `commercial_comparisons`, `purchase_orders`, or `purchase_transaction_approval_policies`. It also confirms unconditional UPDATE/DELETE rejection for material follow-up and unconditional comparison-line DELETE rejection.

Down SQL drops all 15 REV869B tables and correction/retained owned functions. No accepted REV868, REV868C3, or REV869A migration object is named for teardown. Generated SQL contains zero transaction-control statements under `--no-transactions`.

## PostgreSQL test safety and fixture review — NOT RUN

All 22 methods in the two database-backed classes compile and were **NOT RUN**. Both entry paths require exact opt-in `ISOLATED_REV869B_BEHAVIOR_TESTS`, exact database `sess_nexaerp_rev869b_verify`, post-open `current_database()`, and the retained migration exactly once; there is no fallback. Those source gates are sound.

The direct fixture contract is not sound:

1. `RequireExactOwnedFixtureAsync` creates no row. It only requires externally prepared rows for `REV869B-PG-OWNED-DATABASE-GUARDS`.
2. It requires exactly one RFQ, while `DraftRfqAsync` requires that row to be Draft and the late-child RFQ case requires an Issued/Closed/Cancelled RFQ. Both cannot be true.
3. It requires exactly one PO, while `ApprovedPoAsync` requires an Approved current PO and `RejectedPoAsync` requires a Rejected non-current PO. Both cannot be true.
4. Application fixtures create scenario-specific prerequisites only; they do not create or own the direct fixture.
5. `GetHashCode(StringComparison.Ordinal)` and current timestamps prevent the application prerequisite values from being fully stable across processes/runs.
6. Several direct tests commit mutations and clean them later without `finally`; the stale-writer test permanently commits a fixture version increment. Assertion/cancellation failure can leak state.
7. Expected database rejections assert only generic `PostgresException`, not exact SQLSTATE and responsible trigger/function.

## Method-by-method classification

| Method | Classification and independent result |
|---|---|
| `RealServiceTransactionPersistsParentChildHistoryAndAudit` | Real EF service under ambient rollback; credible source success, **NOT RUN**. |
| `RealServiceFailureAfterWritesRollsBackEveryAffectedRelation` | Service-owned transaction is corrected; same-context and incomplete supporting-state verification: **FAIL design**. |
| `RealServiceIdempotentReplayReturnsAuthoritativeOriginalWithoutDuplicates` | Real sequential replay on one DbContext; source-useful, **NOT RUN**. |
| `RealProtectedServiceDenialHasNoBusinessMutationAndNoCrossOrganizationDisclosure` | Real service denial and awaited Denied/Failure audit assertion; source-useful, **NOT RUN**. |
| `RealProtectedServicePropagatesAuditWriterFailureWithoutFalseSuccess` | Real private injected writer, but same-context/incomplete rollback proof: **FAIL design**. |
| `TwoIndependentDbContextsConnectionsAndServicesProduceOneAuthoritativeWinner` | Independent instances exist; both transactions roll back and no loser conflict/replay exists: **FAIL design**. |
| `AuthenticatedMappedAspNetEndpointTraversesPermissionScopeServiceAndEf` | Genuine in-process ASP.NET socket/pipeline/service/EF design: **PASS source design, NOT RUN**. |
| `SuccessfulTransactionPersistsAndCanBeVerified` | Manual audit INSERT/DELETE, not a REV869B business service success; cleanup not fail-safe. |
| `FailedTransactionRollsBackWithBeforeAfterEquality` | Direct RFQ version rollback, not application rollback; contradictory external fixture dependency. |
| `TwoIndependentConnectionsHaveExactlyOneWinnerAndRejectStaleWriter` | Genuine connection-level CAS but commits a shared fixture version mutation with no restoration. |
| `IdempotentReplayReturnsOriginalRowWithoutDuplicate` | Direct SQL clone/reselect inside rollback; not service replay. |
| `ConcurrentIdempotencyCollisionHasOneWinnerAndReturnsOriginal` | Connection-level unique race; winner cleanup is not in `finally`. |
| `DirectTerminalStateInsertIsRejected` | Four aggregate INSERT attempts; generic exception only and external fixture dependency. |
| `SnapshotMismatchIsRejectedOnIssue` | One PO total mutation; generic exception only. |
| `CommercialJsonTaxTotalsVersionOrganizationAndProvenanceTamperingAllReject` | Eight attempts, but line JSON/tax/DELETE cases are rejected first by immutable-line trigger; no complete isolated JSON/tax/provenance matrix. |
| `PermissionDenialPersistsAuditEvidence` | Manual permission query and manual audit INSERT, not protected service/endpoint behavior. |
| `AuditFailureCausesProtectedOperationToFailAndRollback` | Manual invalid audit INSERT rollback, not injected application audit failure. |
| `SkippedAndLowerVersionsAreRejected` | Direct exact +1 negative coverage; generic exception only. |
| `DirectDatabaseRejectsLateChildInsertForEveryTerminalAggregate` | Seven source attempts, but exact-one parent-state contradictions make the suite non-runnable; zero-row attempts would fail rather than falsely pass. |
| `ImmutableHistoryRelationsRejectUnauthorizedUpdateAndDelete` | Exact-one relation precheck prevents zero-row false pass; generic exception and impossible shared fixture remain. |
| `RejectedPoRevisionResubmissionAndRepeatedRevisionKeepExactAncestry` | Direct SQL only, no service/permission/history/audit lifecycle; impossible exact-one PO fixture. |
| `ExactRev869BTriggerAndFunctionInventoryOccursOnce` | Exact 38/11 runtime contract is correctly expressed; **NOT RUN**. |

The private failure hook is test-only constructor injection and has no production request/header/environment activation path. The mapped endpoint case is genuine. Those positives do not cure the fixture, rollback, concurrency, or lifecycle defects.

## Test-count reconciliation

- Previous discovered cases: **469**.
- Current discovered cases: **473**.
- Exact source declarations added: **5**.
- Exact source declarations removed: **1**.
- Net increase: **4**.
- One rename/replacement: `TwoRealServiceInstancesRejectConflictingOrganizationScopedIdempotencyPayload` was replaced by `TwoIndependentDbContextsConnectionsAndServicesProduceOneAuthoritativeWinner`.
- The replacement drops explicit different-payload conflict coverage and does not establish a committed concurrent winner; it is a material weakening of that named contract.
- Added non-PostgreSQL structural method: **1**.
- Actual PostgreSQL methods increased from **19 to 22**: endpoint success plus two direct cases, with the concurrency declaration replaced.
- Current REV869B discovered names: **71**.
- Actual methods in the two REV869B PostgreSQL classes: **22**, all **NOT RUN**.
- All discovered names containing Postgres/PostgreSql: **53**, all excluded and **NOT RUN**.
- Focused REV869B non-PostgreSQL execution: **48/48**.
- Complete non-PostgreSQL execution: **422/422**.

## Permitted offline validation

| Validation | Result |
|---|---|
| PowerShell 5.1 AST parse | PASS: 23 files, 0 errors; scripts not executed. |
| Release build `--no-restore` | PASS: 0 warnings, 0 errors. |
| Focused REV869B non-PostgreSQL | PASS: 48/48. |
| Complete non-PostgreSQL | PASS: 422/422. |
| PostgreSQL compilation/listing | PASS: 22 methods; **NOT RUN**. |
| EF migration discovery | PASS with `--no-connect`: 13 migrations; retained REV869B exactly once after REV869A. |
| Executable model/snapshot parity | PASS: 1/1 without connecting. |
| Accepted regression boundary | PASS through 422 tests; accepted migration paths unchanged. |
| Architecture/secret scan | PASS: no unsafe trigger/session construct and no secret-like source addition. |
| Exact-range diff check | PASS. |

## Independently regenerated SQL evidence

Generated from REV869A to the retained REV869B migration and in reverse with `--no-build`, `--no-transactions`, an unreachable loopback port-1 design-time string, and matching offline expected-database identity. No database connection was opened. Unique temporary SQL files were deleted after hashing.

- Up: **137,006 bytes**, SHA-256 `2B9CEDA0618F88122E54D893D53DD1592041490A111BB8A4DD8E9CDE3A232A33`.
- Down: **6,317 bytes**, SHA-256 `19712B3C4843797AF55927AD1DA720E11310A00A64A85639C193CBA0020A6591`.
- Up inventory: **15 tables, 38 trigger occurrences/38 unique, 11 function occurrences/11 unique, 44 foreign keys, 66 indexes, 31 check occurrences/29 unique check names**.
- Down inventory: **15 table drops** plus scoped owned-function teardown.
- Transaction-control statements in no-transaction Up SQL: **0**.

Hashes and inventory reproduce the checkpoint apart from clarifying that generated SQL has 31 check creation occurrences representing 29 unique names. Reproduction proves deterministic generation, not semantic acceptance.

## Remaining blockers and next gate

1. Add direct DELETE control for every aggregate and approval policy, enforce version-zero inserts, and close same-status aggregate field mutation paths.
2. Define controlled material-follow-up lifecycle transitions with exact +1, actor/timestamp and immutable history, and align comparison-line DELETE with the editable boundary.
3. Replace the five-second history heuristic with transition-command-bound evidence including exact from/to/action, actor employee/login/role, correlation and server-controlled timing.
4. Remove the retained nullable critical `<>` path and add explicit FALSE/NULL/missing/JSON-null/malformed/wrong-type Boolean and JSON contracts.
5. Build all direct fixtures in test source with separately owned state-specific rows. Eliminate the impossible exact-one RFQ/PO contract, external dependency, committed shared-fixture mutation, and non-finally cleanup.
6. Verify service-owned rollback from a fresh independent context/connection and include every touched business, audit, numbering and supporting relation.
7. Create controlled concurrent service tests with a committed authoritative winner, observable loser conflict/replay, zero partial loser state, and separate-organization non-interference without timing-only coordination.
8. Add a genuine service-level repeated rejected-PO lifecycle with histories, audits, permissions, mandatory remarks, self-approval/issuer separation and exact ancestry.
9. Isolate every commercial/tax/totals/version/organization/qualification/attachment/provenance tamper and assert exact SQLSTATE plus responsible trigger/function.

A sixth controlled source-only correction followed by a new independent source-only re-review is required. Isolated database provisioning and execution-helper design are not ready and are not authorized by this report.

rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
