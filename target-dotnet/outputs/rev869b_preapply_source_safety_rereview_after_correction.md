# REV869B Independent Source-Safety Re-review After Correction

Date: 2026-08-11 (Asia/Calcutta)

- Reviewed source commit: `cfe9d9e3005f5e631638d2466b73d957d984a8ed`
- Correction base: `ab4f79a047f6c2bd1eb43952284fe9a527fee626`
- Exact diff: `ab4f79a047f6c2bd1eb43952284fe9a527fee626..cfe9d9e3005f5e631638d2466b73d957d984a8ed`
- Review method: independent source, generated-SQL, test-inventory, build, test, and offline EF inspection; the correction checkpoint was not accepted as proof
- PostgreSQL/database access, migration application/removal, backup/restore, execution-helper creation, production, REV861, REV869C, frontend, legacy, AWS, and OIDC operations: not performed

## Canonical result

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`

The correction closes several findings, but material requirements remain open. An execution helper must not be created yet.

## Exact changed files

The diff contains exactly the reported 17 files. No unrelated file or accepted pre-REV869B migration changed.

1. `outputs/rev869b_source_correction_checkpoint.md`
2. `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs`
3. `src/SESS.NexaERP.Api/Security/EmployeeScopeEndpointFilter.cs`
4. `src/SESS.NexaERP.Api/Security/PagePermissionEndpointFilter.cs`
5. `src/SESS.NexaERP.Application/Purchase/Rev869BPurchaseContracts.cs`
6. `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`
7. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs`
8. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
9. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
10. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`
11. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BSeedData.cs`
12. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
13. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
14. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BPurchaseBehaviorTests.cs`
16. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
17. `tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs`

## Required correction finding results

| Finding | Result | Independent evidence |
|---|---|---|
| N-01 canonical comparison statuses | PASS | Domain and generated `CK_comparison_status` both contain only Draft, PendingApproval, Approved, Rejected, RevisionRequested, and Cancelled. Generated SQL contains no persisted `'Recommended'` status. The exhaustive domain matrix includes all comparison edges. Legitimate recommendation field/action wording remains. |
| N-02 approval thresholds | PASS | Policies cover 0..50000, 50000.000001..500000, and 500000.000001..999999999999999999.999999 without a six-decimal gap or overlap. Executable cases cover 49999.999999, 50000, 50000.000001, 499999.999999, 500000, and 500000.000001. Negative values and missing/ambiguous routes fail closed; maximum is bounded. Approval resolution is called with PO/comparison total payable value. |
| N-03 commercial/GST calculation | FAIL | The corrected formula produces taxable 304 and payable 361.77 for the reference case and guards aggregate arithmetic. However, quotation tax is resolved using entry time (`now`), not `ReceivedAt`, and later comparison/PO recalculation uses the then-current date rather than the immutable quotation tax effective date/snapshot. The pre-issue check compares only total line payable values and nonempty JSON; it does not parse/reconcile header taxable value, discount, tax components, charges, round-off, quotation header, or immutable tax-rule contents. No executable six-decimal end-to-end quotation/comparison/PO reconciliation test exists. |
| N-04 rejected initial PO recovery | FAIL | The domain/API expose Rejected -> RevisionDraft -> Resubmitted -> Approved -> Issued, repeated rejected revisions can be created, remarks are required, rows are copied, and histories are append-only. But recovery calls `ReservePoAsync(rejected.Id,...)`, incrementing the rejected row's Version, so the rejected version is overwritten despite the immutability requirement. Replay also requires the created row still be RevisionDraft, so an exact retry after resubmit/approval fails. In addition, no seeded role has `CanSubmit` for `purchase.po`; both submit/resubmit and issue endpoints require that permission, making the lifecycle unreachable through the API. |
| N-05 denial audit | FAIL | Identity, organization, role, page, record-scope, scoped-missing, masking, service authorization, segregation, approver, self-approval, concurrency, and conflict paths now await audit writes; the wrapper does not swallow audit failure and user-visible forbidden responses omit internal reasons. However, no REV869B attachment-access or export endpoint exists to prove those required denial paths, and no mapped-endpoint/filter integration test executes them. Transaction/rollback interaction with the scoped EF audit writer is also untested. |
| N-06 database transitions/pre-issue snapshot | FAIL | Generated SQL contains four transition triggers and the listed update edges. Nevertheless, RFQ, quotation, and comparison triggers are UPDATE-only, so direct INSERT can start in any check-allowed terminal state. The PO INSERT branch validates only RevisionDraft ancestry and then returns; direct INSERT of PendingApproval, Approved, Issued, Rejected, Resubmitted, Superseded, or Cancelled bypasses transition and pre-issue checks. Approved-to-Issued validates terms, route, line existence, positive quantities, nonempty JSON, and total sum only. It does not reconcile header tax/discount/charges, JSON contents, vendor/quotation/attachment provenance, approval policy, or approval history/approver. RFQ parent/OrganizationId is not immutable, and comparison Draft/RevisionRequested parent/OrganizationId can be changed during transition because the snapshot guard keys off OLD status. |
| N-07 / R-12 executable behavior | FAIL | Only one new test method invokes a real service, and it exits before transaction creation/database access on identity, role, and malformed-request checks. Tests invoke the public error wrapper, not mapped endpoints, authorization middleware, or endpoint filters. No successful service workflow, idempotent replay, transaction, rollback, concurrent CAS, simultaneous PO creation, tax resolver, late quote, technical rejection, amendment/recovery, follow-up deduplication, or immutable-history behavior is executed. PostgreSQL behavior remains correctly unclaimed. |
| R-02 organization-scoped idempotency | FAIL | Several payload comparisons improved and queries are organization-scoped. But request fingerprints omit expected-version fields in multiple operations. Invitation, quotation, comparison/recommendation, initial PO, amendment, issue, cancellation, and rejected-revision replay often return the aggregate's current status/version rather than the original command result after later transitions. Amendment/rejected-revision replays therefore are not stable original results; rejected-revision replay fails once status leaves RevisionDraft. No executable idempotency behavior test exists. |
| R-07 HTTP semantics | PASS | Source maps validation, invalid operation, overflow, and argument errors to 400; unauthenticated to 401; forbidden to 403; scoped missing to 404; concurrency/idempotency conflict to 409. Scoped queries do not disclose cross-organization existence. Tests execute the wrapper mapping, but not actual routes/middleware; that limitation is classified under R-12. |
| I-02 numeric safety | PASS | numeric(24,6) maximum checks cover inputs including round-off, multiplication, taxable charges, discount result, tax components, aggregate sums, PO header aggregates, and final payable value. Invalid/overflow domain errors map to 400. The implementation is conservative for large pre-division tax intermediates but fails closed rather than reaching persistence overflow. |

## Previous finding reconciliation

| Prior finding | Result | Note |
|---|---|---|
| B-01 quotation terminal statuses | PASS | Domain, checks, and service names agree. |
| B-02 optimistic concurrency structure | PASS | Organization-scoped CAS predicates remain; runtime concurrency remains unproved under R-12. |
| B-03 commercial read masking | PASS | Explicit non-commercial projections remain and masking is audited. |
| B-04 amendment approver availability | PASS | Purchase Manager, TD, and MD retain PO approve/reject grants. The separate missing PO submit permission is new finding M-01 below. |
| B-05 organization/parent substitution | PASS with N-06 limitation | Service loads and six parent-chain triggers remain scoped; direct-SQL aggregate insertion/immutability gaps are N-06. |
| R-01 atomic state/history/audit source structure | PASS with evidence limitation | Mutations use explicit serializable transactions and the audit writer shares the scoped DbContext. No rollback test executes this behavior. |
| R-03 PO category qualification | PASS | PO creation revalidates every selected line category. |
| R-04 quotation provenance | PASS | Source, timestamp, object key, SHA-256, attestation, and database checks remain. External object existence is outside this source-only gate. |
| R-05 technical/commercial person segregation | PASS | Selected-quotation verifier/approver overlap is rejected. |
| R-06 least-privilege/nonempty rows | PASS for overbreadth; operational defect M-01 | 29 fixed rows are nonempty and commercial/export flags imply view, but the exact set omits all PO submit grants. |
| R-08 bounded follow-up read | PASS | Page bounds, deterministic ordering, Skip, and Take remain. |
| R-09 audited direct read denial | PASS | Scoped misses and record denials are audited before 404/403. |
| R-10 completed snapshot immutability | PASS with N-06 limitation | History/line relations and completed headers are guarded; semantic pre-issue reconciliation remains insufficient. |
| R-11 handoff/invitation collision | PASS | Handoff number and unique PO-line index remain; invitation arbiter is RFQ-scoped. |
| I-01 database overlap prevention | FAIL | Runtime requires exactly one effective policy, but the database still has no exclusion constraint preventing overlapping amount/effective-date policies. |
| I-03 Down ownership hardening | FAIL | Three manual Department Manager deletions check `CreatedBy`; 29 generated permission and four page deletes remain unconditional by deterministic ID. |

## New findings

### M-01 — material: PO submit/resubmit and issue are unreachable

`Rev869BSeedData.Permission` sets `CanSubmit` only for Purchase Executive on RFQ/quotation, Purchase Manager on RFQ/quotation/comparison, and Technical Engineer on technical verification. No role receives `CanSubmit` on `purchase.po`. Both `/purchase-orders/{number}/submit` and `/purchase-orders/{number}/issue` require `PagePermissionActions.Submit`. Direct URL/API access therefore fails at the page filter for every role, including the Purchase Manager required by the service. This blocks initial PO submission, rejected-PO resubmission, and issue.

### M-02 — material: transition INSERT bypass

The generated SQL transition triggers for RFQ, quotation, and comparison do not run on INSERT. The PO trigger runs on INSERT but returns after checking only the special RevisionDraft case. Direct SQL can therefore create terminal-status aggregates, including an Issued PO, without traversing canonical edges, approval evidence, or the Approved-to-Issued snapshot gate.

### I-04 — improvement: checkpoint test-count comparison was inconsistent

Independent discovery found 428 cases at the base and 439 at the corrected commit. With the exact name exclusion `FullyQualifiedName!~Postgres&FullyQualifiedName!~PostgreSql`, the base has 400 and the corrected commit has 411. The statement that the complete non-PostgreSQL count “remained 411” compares different inventories/filtering or is otherwise unsupported. Coverage increased by the same 11 cases as the focused count.

## Test inventory reconciliation

- Base discovered tests: 428.
- Corrected discovered tests: 439.
- Base focused REV869B: 26.
- Corrected focused REV869B: 37.
- Base under the current PostgreSQL-name exclusion: 400.
- Corrected under the same exclusion: 411.
- Net new discovered cases: 11.
- Removed test methods/cases: none.
- Weakened tests: none identified.
- New behavioral tests are included in the 411-case complete suite.

Added cases:

1. Commercial calculation/taxable-charge/overflow behavior.
2. Pre-issue snapshot completeness/reconciliation behavior.
3-9. Seven error-wrapper cases for 400/401/403/404/409 and audit counts.
10. Audit-write failure propagation.
11. One service test method covering four fail-fast CreateRfq branches before any transaction/database operation.

Renamed or converted:

- `DatabaseStatusSetsMatchCanonicalQuotationAndPurchaseOrderSets` became `DatabaseStatusSetsMatchCanonicalAggregateSets`; comparison status equality and absence of Recommended were added.
- Two ApprovalBoundaries theory rows changed from .01 to the smallest six-decimal values .000001. The method was not removed.

Modified assertion strength:

- PO domain transition assertions were expanded for RevisionDraft/Resubmitted.
- Comparison database status equality and noncanonical-status absence were added.
- Boundary coverage added smallest below/above values.
- Commercial expected values were corrected to 304/27.36/27.36/3.04/361.77.
- Trigger expectations changed from 16 to 20 and assert function/pre-issue text; these remain structural checks, not trigger execution.
- No modified assertion was weakened, but structural checks cannot close R-12.

Behavior classification:

- Real domain behavior: status matrices, approval resolution, calculator/overflow, pre-issue in-memory snapshot, seed construction.
- Actual service methods: only fail-fast `CreateRfqAsync` identity/role/validation branches; zero successful service transactions.
- Actual API endpoints: zero. The seven HTTP cases invoke `Rev869BPurchaseEndpoints.Run` directly.
- Transactions/rollback: zero executable cases.
- Runtime concurrency/CAS: zero executable cases.
- Idempotency: zero executable cases.
- EF model/snapshot parity: executable design-time model differ without a connection.
- Remaining tests: reflection or source-string/structural inspection.

## Migration and rollback assessment

- Migration discovery: exactly 13; REV869B appears exactly once after REV869A.
- Pending model changes: none.
- Current model/snapshot parity: executable focused model-differ test passes.
- Offline Up: 77,361 bytes; 15 tables, 20 REV869B triggers, 4 functions, 44 FKs, 66 indexes, 29 checks.
- Offline Down: 4,508 bytes; 15 table drops and 4 function drops.
- Up SHA-256: `7A93EADD591A0046BCC04137BBE043DBF08F0A51F7630E72A917D9B865C312FD`.
- Down SHA-256: `BAC1AF4A71ED86113BEE2614D8E66A0633C2F22AAFAAF2A63C42E4475FE68D28`.
- Both hashes match the checkpoint.
- Down order is dependency-safe for the 15 owned tables/functions and removes the migration history row last. It does not drop/alter accepted prior tables or migrations.
- Destructive-scope improvement I-03 remains because generated seed/page deletes do not check ownership metadata.

## Permission, policy, and regression review

- Four new page definitions, 29 fixed-role permission rows, three approval policies, and three fail-closed Department Manager rows remain.
- Approval policies are continuous at six-decimal scale and capped at numeric(24,6) maximum.
- No empty fixed permission row or employee-specific application workflow was added.
- Vendor/category qualification, technical/commercial segregation, attachment provenance fields/checks, append-only histories, Material Follow-up unique handoff/PO-line indexes, and cancellation/amendment source paths remain.
- No accepted REV868, REV868C3, or REV869A migration file changed.
- M-01 prevents the permission set from being operationally complete.

## Validation record

- PowerShell 5.1 AST parsing: 23 scripts, 0 parse errors; scripts were not executed.
- Build: 0 warnings, 0 errors.
- Focused REV869B: 37 passed, 0 failed, 0 skipped.
- Complete suite with PostgreSQL/Postgres class names excluded: 411 passed, 0 failed, 0 skipped.
- EF discovery used `--no-connect` and an unreachable loopback port-1 configuration.
- Offline SQL generation and hash comparison completed without a database connection.
- Exact correction diff has 17 files and `git diff --check` is clean.
- Secret, employee-PII, prohibited-operation, employee-hardcoding, and prior-migration-diff scans found no additions.
- Reviewed source commit was clean before creation of this report.

## Evidence limitations and exact next gate

No PostgreSQL behavior, migration application, rollback execution, trigger execution, live concurrency, or production behavior was tested or claimed.

The next gate is another controlled source correction addressing N-03, N-04, N-05, N-06, R-02, R-12, I-01, I-03, M-01, and M-02, followed by a new independent source-safety re-review. Execution-helper work remains blocked until that re-review passes.
