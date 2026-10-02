# REV869B Second Controlled Source Correction Checkpoint

Date: 2026-08-11 (Asia/Calcutta)

- Starting commit: `192dd4b7a0cd6783172bd8b658f7a415f2e172aa`
- Ending commit: the correction commit containing this checkpoint (reported in the final handoff)
- Reviewed source commit corrected: `cfe9d9e3005f5e631638d2466b73d957d984a8ed`
- Preserved migration ID: `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`
- Scope: source-only correction inside `target-dotnet`
- PostgreSQL/database access, migration application/removal, execution-helper creation, backup/restore, production, REV861, REV869C, frontend, and excluded subsystem operations: not performed
- Disposition: stop after commit and require another independent source-safety re-review

## Failed and new finding corrections

| Finding | Correction made |
|---|---|
| N-03 | Quotation GST is resolved at immutable `ReceivedAt`; a typed approved/effective GST snapshot is stored. Comparison and PO reuse and reconcile that snapshot rather than re-resolving current tax. A single calculator validates line assessable value, taxable charges, discount, taxable value, component tax, round-off, aggregate headers, and numeric(24,6) bounds. Pre-issue validation parses line snapshots and reconciles PO header/lines, organization, RFQ/quotation/comparison/vendor provenance, vendor qualification evidence, attachment key/hash, comparison approval route/time, GST rule, and every commercial component. |
| N-04 | Rejected PO recovery no longer increments or rewrites the rejected row. The rejected version is checked optimistically and retained; a linked immutable `RevisionDraft` is inserted, resubmitted to `Resubmitted`, and can repeat rejection/revision cycles. Required remarks, history retention, direct-transition prohibition, stable revision replay after later transitions, and API reachability are aligned. |
| N-05 | Added scoped quotation-attachment, comparison-export, and PO-export routes protected by employee scope and exact download/export permission filters. Denials and permitted attachment/export actions await audit writes; audit exceptions remain fail-closed. A mapped route is executed in tests through its real endpoint filters and proves 403 plus an awaited audit without reaching the no-connect DbContext. |
| N-06 / M-02 | RFQ, quotation, comparison, and PO transition triggers now cover INSERT and UPDATE. INSERT permits only Draft RFQ, Submitted quotation, Draft comparison, and controlled Draft/RevisionDraft PO. UPDATE protects organization/parents/ancestry, rejects illegal edges/backward versions, and expands issue-time reconciliation across all header components, JSON snapshot identity, approval history, and effective policy evidence. Down drops owned trigger functions with `CASCADE` before tables. |
| N-07 / R-12 | Added executable non-PostgreSQL vectors for the authoritative calculator, immutable pre-issue reconciliation, canonical fingerprint collision/scope/version behavior, and a real mapped endpoint/filter denial. Existing executable HTTP 400/401/403/404/409, audit failure, domain lifecycle, model-differ, and fail-fast service tests remain. Eight separately named PostgreSQL tests were added for the later isolated gate and were not executed. |
| R-02 | Added a canonical SHA-256 command fingerprint binding OrganizationId, operation, idempotency key, normalized/sorted request JSON (including expected versions), and authoritative aggregate identity. RFQ, invitation, quotation revision, technical verification, comparison creation/recommendation/resubmission/approval, initial PO, submit/resubmit, amendment, rejected revision, approval/rejection, issue/cancel, and Material Follow-up correlation now use stable scoped evidence. Same-payload replay returns original status/version rather than mutable current state; changed payloads conflict. |
| I-01 | Added a database trigger rejecting overlapping active purchase approval policies across both amount and effective-date ranges. Runtime still requires exactly one effective policy. |
| I-03 | Down now installs fail-closed ownership triggers before deterministic permission/page deletion. A row whose `CreatedBy` is no longer `migration-rev869b` aborts rollback rather than being deleted; guard triggers/functions are removed afterward. |
| M-01 | Added a distinct `issue` policy action implemented as the conjunction of submit and update grants. Purchase Manager receives PO view/create/update/submit/resubmit/issue but no PO approve/reject/request-revision. The mapped Department Manager receives PO approval-only actions; Technical and Managing Directors retain higher-route approval. This avoids both flag overloading and creator/final-approver combination. |
| I-04 | Test counts use one consistent PostgreSQL-name exclusion. Base was 439 total / 411 non-PostgreSQL cases; ending inventory is 450 total / 414 non-PostgreSQL cases. |

## Preserved passing contracts

- Canonical comparison statuses remain Draft, PendingApproval, Approved, Rejected, RevisionRequested, and Cancelled.
- Approval ranges remain continuous at six-decimal scale: 0..50000, 50000.000001..500000, and 500000.000001..999999999999999999.999999.
- HTTP 400/401/403/404/409 semantics and scoped non-disclosure remain unchanged.
- numeric(24,6) overflow protection, vendor/category qualification, technical/commercial person segregation, organization/parent integrity, attachment provenance, append-only histories, Material Follow-up uniqueness, and accepted REV868/REV868C3/REV869A behavior remain covered.
- No employee-specific permission was introduced.

## Calculation test vectors

| Vector | Authoritative result |
|---|---|
| No charges/no discount, 2 x 50, zero tax | taxable/payable 100 |
| Charges 5 + 5 and permitted discount 10 on 1 x 100 | taxable 100; payable 118 with intra-state 9% + 9% |
| Intra-state GST | CGST 9, SGST 9, IGST 0 on taxable 100 |
| Inter-state GST | CGST 0, SGST 0, IGST 18 on taxable 100 |
| Zero tax | all tax components zero |
| Six-decimal rounding | 0.3333334 rounds to 0.333333 at scale 6 |
| Maximum safe | 999999999999999999.999999 accepted with quantity 1 and zero additions |
| Negative/overflow | negative quantity and maximum-times-two reject before persistence |
| Client/stored mismatch | typed reconciliation rejects any component or total mismatch |
| Reference charges/discount/tax/cess/round-off | taxable 304; CGST 27.36; SGST 27.36; cess 3.04; payable 361.77 |

## PO permission reachability

| Role | View | Create/update | Submit/resubmit | Issue | Approve/reject/revision | Commercial/export/audit |
|---|---:|---:|---:|---:|---:|---:|
| Purchase Manager | Yes | Yes | Yes | Yes (`issue` = submit + update) | No | Yes |
| Department Manager (mapped) | Yes | No | No | No | Yes, manager route only | Audit history only |
| Technical Director | Yes | No | No | No | Yes, configured higher route | Yes |
| Managing Director | Yes | No | No | No | Yes, configured higher route | Yes/full control as existing |
| Stores Manager / Stores Executive | Yes | No | No | No | No | Non-commercial view/download/print |
| Accounts Head | Yes | No | No | No | No | Commercial view/export/audit |
| Purchase Executive | No PO row | No | No | No | No | No |

The endpoint chain is Page Master `purchase.po` -> normalized role -> role-page row -> exact action (`create`, `update`, `submit`, `resubmit`, `issue`, `approve`, `reject`, `request-revision`, `cancel`) -> employee/organization record scope. Service self-approval and configured approver mapping remain fail-closed.

## Test inventory

- Base discovered cases: 439.
- Ending discovered cases: 450.
- Base complete non-PostgreSQL cases: 411.
- Ending complete non-PostgreSQL cases: 414; all 414 passed.
- Focused REV869B non-PostgreSQL cases: 40; all 40 passed.
- Added non-PostgreSQL executable behavior cases: 3.
- Added PostgreSQL gate cases, intentionally not executed: 8.
- Removed or renamed tests: none.
- Materially strengthened existing structural tests: 2 (`MigrationOwnsImmutableAndCrossParentFailClosedGuards`; `MigrationAndMappingEnforceUniquenessConcurrencyAndImmutability`) for the 21 Up-trigger / two guarded-Down-trigger source inventory.
- Materially strengthened permission test: `PermissionMatrixHasNoEmptyOrCommercialOnlyLeakageAndPoApproversExist` now checks exact PO operational/approval separation.
- Behavioral focused cases: 25 (calculator/approval/status/snapshot/HTTP/audit/service/seed/model execution).
- Structural focused cases: 15 (source/migration/API contract inspection); structural checks remain supplementary.

Added non-PostgreSQL cases:

1. `AuthoritativeCommercialPipelineExecutesRequiredVectors`
2. `CanonicalIdempotencyFingerprintBindsOrganizationOperationKeyPayloadAndVersions`
3. `MappedAttachmentEndpointExecutesPermissionDenialAndAwaitsAudit`

PostgreSQL cases added but not executed:

1. `InsertTerminalStateIsRejected`
2. `InvalidUpdateTransitionIsRejectedByTrigger`
3. `SnapshotMismatchBlocksIssue`
4. `ImmutableHistoryRejectsMutation`
5. `TransactionFailureRollsBackAllRows`
6. `ConcurrentAggregateUpdatesHaveSingleWinner`
7. `ConcurrentIdempotencyHasSingleAuthoritativeResult`
8. `TriggerInventoryIsInstalledExactlyOnce`

## Offline validation evidence

- PowerShell 5.1 AST parse: 23 files, 0 errors; scripts were not executed.
- Build: 0 warnings, 0 errors.
- Focused REV869B non-PostgreSQL: 40 passed, 0 failed, 0 skipped.
- Complete non-PostgreSQL: 414 passed, 0 failed, 0 skipped.
- EF `--no-connect` discovery: exactly 13 migrations; REV869B exactly once after REV869A.
- EF pending-model check: no changes since the last migration.
- Migration/model/designer/snapshot parity: executable model-differ test passed.
- Offline Up SQL: 83,539 bytes; SHA-256 `EA2BE57D5088F45CD34597AB2E86B94BFBED6A5776DBB74390E5CECDC7FE137D`; 15 tables, 21 REV869B triggers, 5 functions, 44 FKs, 66 indexes, 29 checks.
- Offline Down SQL: 5,813 bytes; SHA-256 `91AAEE42BF2DFD6E975463AE94D9FB4D8D9E840879E7A8D144600F466A6D9C01`; 15 table drops; two temporary ownership triggers and one temporary ownership function are created only to guard rollback deletion, while all owned Up functions/triggers are dropped before dependent tables.
- Exact permission/policy set: 29 fixed-role rows, three fail-closed Department Manager rows, four page definitions, and three continuous approval policies.
- `git diff --check`: clean before checkpoint creation.
- Secret scan: no secret-like additions.
- No database connection was made; no PostgreSQL test ran.

## Remaining blockers

1. Another independent source-safety re-review must assess this committed correction; this checkpoint does not self-approve.
2. The eight PostgreSQL tests require a later separately authorized isolated database gate. Database application/rollback, live triggers, transaction rollback, concurrency, and immutable-history behavior remain unexecuted and unclaimed here.
3. Production readiness, deployment, OIDC activation, REV861, REV869C, and excluded subsystems remain outside scope.

`../legacy-reference/` was not accessed or changed.
