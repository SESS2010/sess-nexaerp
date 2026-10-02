# REV869B Independent Pre-Application Source Safety Re-Review

## Decision

- `starting_commit=2b12fb288a8725b64365f7c1287b7e6fd46cd629`
- `migration_id=20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`
- `review_mode=SOURCE_ONLY_INDEPENDENT_REVIEW`
- `postgresql_access=NOT_PERFORMED`
- `rev869b_source_safety_state=FAIL`
- `rev869b_execution_helper_readiness_state=FAIL`
- `generate_plan_only_command=NOT_PROVIDED`

The corrected source is not safe to apply. Structural migration counts, model parity, rollback object ordering, basic compare-and-swap source structure, commercial masking, parent guards, and most previous blockers improved. However, Blocking and Required Correction findings remain. The database still permits a `Recommended` comparison state that the canonical domain rejects, six-decimal payable values fall into approval-policy gaps immediately above both thresholds, and the commercial calculation does not follow the required discount/charges/taxable/GST sequence. Replay, HTTP semantics, denial auditing, PO rejection recovery, controlled-state database enforcement, and behavioral test evidence are also incomplete.

This review did not modify source, migration, snapshot, tests, helpers, or configuration. It did not access PostgreSQL or run any helper, migration application/removal, backup, restore, production, REV861, frontend, or REV869C operation. This report is the only created file.

## Exact reviewed files

Primary REV869B evidence:

- `outputs/rev869b_preapply_source_safety_review.md`
- `outputs/rev869b_source_correction_checkpoint.md`
- `outputs/rev869b_source_implementation_checkpoint.md`
- `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`
- `src/SESS.NexaERP.Application/Purchase/Rev869BPurchaseContracts.cs`
- `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
- `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
- `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
- `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BSeedData.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
- `tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`

Supporting contracts traced:

- `src/SESS.NexaERP.Domain/Common/AuditableEntity.cs`
- `src/SESS.NexaERP.Domain/Masters/TaxGstSetting.cs`
- `src/SESS.NexaERP.Domain/Authorization/RolePagePermission.cs`
- `src/SESS.NexaERP.Application/Masters/Rev869AFoundationServices.cs`
- `src/SESS.NexaERP.Application/Common/ICurrentUser.cs`
- `src/SESS.NexaERP.Application/Authorization/IRecordScopeAuthorizer.cs`
- `src/SESS.NexaERP.Application/Authorization/IPagePermissionService.cs`
- `src/SESS.NexaERP.Infrastructure/Masters/EfRev869AFoundationServices.cs`
- `src/SESS.NexaERP.Infrastructure/Authorization/EfRecordScopeAuthorizer.cs`
- `src/SESS.NexaERP.Infrastructure/Authorization/EfPagePermissionService.cs`
- `src/SESS.NexaERP.Infrastructure/Audit/EfAuditWriter.cs`
- `src/SESS.NexaERP.Api/Security/ClaimsCurrentUser.cs`
- `src/SESS.NexaERP.Api/Security/PagePermissionEndpointFilter.cs`
- REV868, REV868C3, and REV869A migration/snapshot/test sources referenced by preserved FKs and services.

## Previous finding closure

| Previous finding | Classification | Independent closure result |
|---|---|---|
| B-01 quotation terminal status rejected by DB | VERIFIED_PASS | Quotation canonical states and `CK_vendor_quotation_status` now both contain Submitted, TechnicallyCompliant, TechnicallyRejected, Superseded, Withdrawn, and Rejected. The new comparison-status mismatch is separately classified N-01. |
| B-02 passive optimistic concurrency | VERIFIED_PASS | Every material existing-aggregate request carries an expected version. Five CAS helpers match ID, OrganizationId/organization parent, and Version, atomically increment Version, and throw `DbUpdateConcurrencyException` unless exactly one row changes. PostgreSQL concurrency behavior is still untested under R-12. |
| B-03 commercial read leakage | VERIFIED_PASS | Comparison and PO GET paths require `ViewCommercialValues`; otherwise explicit projections omit rates, commercial snapshots, tax, and totals. |
| B-04 no PO amendment approver | VERIFIED_PASS | Purchase Manager, TD, and MD have PO approve/reject grants; submit/approve/reject endpoints exist and backend approver resolution still applies. |
| B-05 cross-organization/parent substitution | VERIFIED_PASS | Organization predicates are present on aggregate loads/CAS. Six schema-qualified parent-contract triggers validate quotation, technical, comparison, PO, PO-line, and follow-up chains. All 44 FKs are restrictive. |
| R-01 non-atomic state/history/audit | VERIFIED_PASS | Every REV869B mutation opens an explicit serializable transaction. `EfAuditWriter` uses the same scoped DbContext and saves before commit, so failure rolls back business state/history/audit together. |
| R-02 incomplete idempotency | REQUIRED_CORRECTION | Quotation replay compares only invitation and vendor reference, omitting lines, provenance, attachment hash, terms, and totals. Recommendation replay omits selected quotation/justification. PO submit and PO approval filter to pre-transition status before replay lookup, so a successful exact retry becomes 404. Approved-amendment replay can bind to the new current version and conflict. |
| R-03 PO skips category qualification | VERIFIED_PASS | PO creation rechecks every distinct selected item category with effective vendor qualification. |
| R-04 quotation provenance absent | VERIFIED_PASS | Submission source, received timestamp, object key, SHA-256, attestation, source constraint, provenance constraint, audit payload, and quotation snapshot guard are present. External object existence remains outside this source-only proof. |
| R-05 verifier/approver person segregation | VERIFIED_PASS | Commercial approval rejects an actor who technically verified any selected quotation line. |
| R-06 excessive/all-false permission seeds | VERIFIED_PASS | The fixed set is 29 deterministic, non-empty rows. Commercial/export/audit flags imply view, PO approvers exist, and three Department Manager rows are scoped/non-commercial. |
| R-07 collapsed HTTP semantics | REQUIRED_CORRECTION | Dedicated exception mappings exist, but many request-validation paths still throw `InvalidOperationException`, and the wrapper still maps every such exception to 409. Missing single-source justification, duplicate handoffs, invalid quantities, incomplete quote lines, and currency mismatch therefore do not reliably return 400. |
| R-08 unbounded Material Follow-up read | VERIFIED_PASS | `page >= 1`, `pageSize 1..100`, deterministic order, Skip, and Take are enforced before per-record scope filtering. |
| R-09 unaudited record-read denial | VERIFIED_PASS | Direct RFQ/comparison/PO denials and rejected follow-up rows write sanitized Security/Denied audit evidence. Broader service authorization denials remain N-05. |
| R-10 incomplete immutable snapshots | VERIFIED_PASS | Quotation provenance/terms, recommended comparison header/lines, issued PO terms, and six history/line relations have schema-qualified update/delete guards. Pre-issued PO header control remains N-06. |
| R-11 global handoff/invitation collision | VERIFIED_PASS | Handoff numbers contain the PO UUID; invitation idempotency is unique by RFQ plus key; PO-line handoff uniqueness prevents duplicates. |
| R-12 tests do not prove behavior | REQUIRED_CORRECTION | Still unresolved. Of 26 focused tests, only status matrices, calculator/policy functions, seed construction, and model-differ parity execute relevant logic. Service transaction/concurrency/auth/idempotency/API/database behavior remains source-text inspection. |
| I-01 effective-policy overlap | IMPROVEMENT | Runtime requires exactly one effective match and fails closed, but the database unique key prevents identical rows only, not overlapping amount/date ranges. |
| I-02 commercial maximum | REQUIRED_CORRECTION | Individual inputs are bounded, but summed taxable/tax/charge/total results are not checked against numeric(24,6) capacity. Multiple individually valid maximum values can reach the database out of range. |
| I-03 Down ownership guards | IMPROVEMENT | The three manual Department Manager deletes check `CreatedBy`; the 29 generated permission and four page deletes remain unconditional deterministic-ID `DeleteData` operations. They are source-owned, but ownership checks would make rollback safer after later edits. |

## New regression findings

### BLOCKING

#### N-01 - comparison database status contract is not canonical

`Rev869BStatusContracts.Comparison` contains Draft, PendingApproval, Approved, Rejected, RevisionRequested, and Cancelled. Both mapping and migration `CK_comparison_status` additionally permit `Recommended`. No service transition produces `Recommended`, and the correction test compares only quotation and PO constraint sets. The database can therefore store a domain-invalid state and a direct Draft-to-Recommended or other invalid transition. This fails the exact status-set and no-database-bypass requirements and shows the prior test was too narrow.

#### N-02 - approval thresholds contain six-decimal gaps

Approval values and policy bounds are `numeric(24,6)`, but seeds use Manager `[0, 50000]`, TD `[50000.01, 500000]`, and MD `[500000.01, infinity]`. Values such as `50000.000001` and `500000.000001` match no policy. Runtime correctly fails closed, but legitimate values immediately above each approved threshold cannot proceed. The tests cover only two-decimal boundary examples and do not prove gap-free six-decimal behavior.

### REQUIRED_CORRECTION

#### N-03 - commercial calculation sequence differs from the required contract

The calculator persists `TaxableValue = quantity * rate`, subtracts discount only into a local tax base, taxes that base, then adds packing, freight, insurance, and other charges after tax. The required sequence is quantity/rate, discount, charges, taxable value, then GST/cess. For quantity 3, rate 100, discount 10, charges 2+3+4+5, CGST 9%, SGST 9%, cess 1%, round-off 0.005 at scale 2:

- implemented: stored taxable 300; tax base 290; tax 26.10 + 26.10 + 2.90; total 359.11;
- required sequence: taxable 304; tax 27.36 + 27.36 + 3.04; rounded total 361.77.

Effective-date resolution and intrastate/interstate component validation are present, but they operate on the implemented tax base. Approval therefore uses a server-recalculated total, yet not the required commercial formula.

#### N-04 - rejected initial PO is a current-version dead end

Initial POs are current. Rejecting one sets status Rejected while retaining `IsCurrentVersion=true`. Rejected has no outgoing transition or resubmit/revise endpoint. A replacement PO from the same comparison uses revision 1 and conflicts with the unique `(CommercialComparisonId, RevisionNumber)` key. Amendment rejection correctly leaves its issued predecessor current, but initial rejection has no recoverable controlled path.

#### N-05 - not every denied attempt is audited

Record-scope and endpoint-filter denials are audited, but `RequireRole`, missing identity/organization, self-approval, missing/ambiguous manager mapping, wrong configured approver role, cross-organization guards, and technical/commercial person segregation throw `UnauthorizedAccessException`. The API wrapper returns 403 without writing a denial audit for those paths.

#### N-06 - database transition and pre-issue PO controls remain incomplete

Status check constraints validate membership, not transition edges. Apart from snapshot immutability conditions, there is no database transition guard preventing direct invalid edges. The PO snapshot guard protects only rows whose old state is Issued, Cancelled, or Superseded; a PendingApproval or Approved PO header can have totals, approval route, vendor, or terms rewritten directly without a new controlled version/reapproval. PO lines remain immutable, which can also create header/line divergence.

#### N-07 - behavioral acceptance evidence is insufficient

There are no executable service/API tests proving stale-version conflict, simultaneous PO creation, server-side tax resolution/recalculation, client bypass resistance, amendment approval/rejection, organization/parent substitution, missing identity/scope/manager mapping, late quotation, technical rejection, follow-up deduplication, immutable database histories, or transaction rollback. The migration tests mostly search source text. Green offline totals therefore do not establish execution-helper readiness.

## Actual schema contract

Direct migration-source and generated-SQL counts:

| Object | Claimed | Actual | Result |
|---|---:|---:|---|
| Tables | 15 | 15 | VERIFIED_PASS |
| Columns | 309 | 309 | VERIFIED_PASS |
| Primary keys | 15 | 15 | VERIFIED_PASS |
| Indexes | 66 | 66 | VERIFIED_PASS |
| Restrictive foreign keys | 44 | 44 | VERIFIED_PASS |
| Check constraints | 29 | 29 | VERIFIED_PASS |
| Triggers | 16 | 16 | VERIFIED_PASS |
| Trigger functions | 3 | 3 | VERIFIED_PASS |
| New pages | 4 | 4 | VERIFIED_PASS |
| Fixed permission rows | 29 | 29 | VERIFIED_PASS |
| Approval policies | 3 | 3 | VERIFIED_PASS |
| Scoped Department Manager permissions | 3 | 3 | VERIFIED_PASS |

Additional schema results:

- VERIFIED_PASS: filtered unique current quotation/PO indexes, organization/number and organization/idempotency uniqueness, and null-safe effective-date uniqueness are present.
- VERIFIED_PASS: no `ON CONFLICT` exists, so arbiter compatibility is not applicable.
- VERIFIED_PASS: all trigger functions/calls are explicitly in `nexa`; tables are created before indexes/seeds/triggers and dropped before the three functions.
- VERIFIED_PASS: numbering is organization/financial-year/prefix scoped and used within serializable transactions; CAS source predicates are organization scoped.
- VERIFIED_PASS: no vendor, employee, quotation, PO, PR, reservation, or other business-record seed exists.
- VERIFIED_PASS: no existing column/table is altered or dropped by Up; all 15 Down table drops are REV869B-owned.
- REQUIRED_CORRECTION: status transition integrity and pre-issue PO header control are incomplete as N-01/N-06.
- IMPROVEMENT: generated seed deletion ownership guards remain inconsistent as I-03.

## Approval, calculation, and security matrix

| Area | Classification | Result |
|---|---|---|
| Manager 0 through 50,000 inclusive | VERIFIED_PASS | Exact endpoints resolve to Manager. |
| Above 50,000 through 500,000 | BLOCKING | `50000.000001..50000.009999` has no policy. |
| Above 500,000 | BLOCKING | `500000.000001..500000.009999` has no policy. |
| Missing/ambiguous policy | VERIFIED_PASS | Exactly one effective match is required. |
| Missing/ambiguous manager mapping | VERIFIED_PASS | Exactly one active effective mapping and matching employee are required. |
| Self-approval | VERIFIED_PASS | Creator login equality fails closed. |
| Technical/commercial person segregation | VERIFIED_PASS | Same employee cannot approve the selected technically verified quotation. |
| Identity and operational scope | VERIFIED_PASS | Employee ID and organization are mandatory; department/warehouse/owner scope is resolved server-side. |
| Actor role/request bypass | VERIFIED_PASS | Role is read from `ICurrentUser`; request contracts contain no role/approver field. |
| Route/body parent IDs | VERIFIED_PASS | Service predicates and parent triggers bind route documents and supplied UUIDs to organization/parent chains. |
| Vendor qualification/final approval | VERIFIED_PASS | Category qualification is rechecked; REV869A configurable `VENDOR_FINAL_APPROVER` remains unchanged and MD-only by accepted policy. |
| Commercial sequence | REQUIRED_CORRECTION | Charges are added after tax instead of before taxable/GST computation. |
| Effective tax and GST split | VERIFIED_PASS | Exactly one active approved effective HSN/state/registration rule is required; intrastate CGST+SGST and interstate IGST splits are validated. |
| Precision and maximum | REQUIRED_CORRECTION | Scale 0..6 and negative inputs are controlled; aggregate numeric(24,6) overflow is not prevalidated. |
| Unsupported currency conversion | VERIFIED_PASS | RFQ/quotation currency must match; no conversion is attempted. |
| Denied-attempt audit | REQUIRED_CORRECTION | Record/page denials are audited, but service role/approver/identity/segregation denials are not. |

## Permission matrix result

The 29 fixed rows resolve as Purchase Manager 6, Purchase Executive 2, Technical Engineer 3, TD 6, MD 6, Stores Manager 2, Stores Executive 2, and Accounts Head 2. All rows have at least one grant. Commercial/export/audit flags require view. PO approve/reject is held by Purchase Manager, TD, and MD. Three separate Department Manager rows grant scoped view/clarification/print/download/audit without commercial values or mutation. Endpoint page permissions are backed by service identity, role, approver, and record-scope checks. Classification: VERIFIED_PASS, subject to N-05 denial-audit coverage.

## Behavioral test matrix

| Required evidence | Actual test type | Classification |
|---|---|---|
| Canonical transitions | Executable exhaustive domain matrix; DB comparison set omitted | REQUIRED_CORRECTION |
| Stale Version conflict | Reflection/source-string assertions only | REQUIRED_CORRECTION |
| Concurrent PO creation | No executable concurrency test | REQUIRED_CORRECTION |
| Server recalculation/tax resolution | Pure calculator only; no service/tax-resolver behavior | REQUIRED_CORRECTION |
| Approval boundaries | Executable only at two-decimal examples | BLOCKING |
| Client bypass attempts | Source-string assertions only | REQUIRED_CORRECTION |
| PO amendment approval/rejection | Source-string assertions only | REQUIRED_CORRECTION |
| Cross-organization/cross-parent | Trigger/source strings only | REQUIRED_CORRECTION |
| Missing identity/scope/mapping | Source strings only | REQUIRED_CORRECTION |
| Single-source/late quotation/technical rejection | Source strings only | REQUIRED_CORRECTION |
| Follow-up deduplication | Source/index strings only | REQUIRED_CORRECTION |
| Immutable history/rollback | Trigger strings only | REQUIRED_CORRECTION |
| Migration/model/snapshot parity | Executable `IMigrationsModelDiffer` and EF pending-model check | VERIFIED_PASS |
| REV868/REV868C3/REV869A preservation | Migration ownership/static scan only; database proof pending | VERIFIED_PASS for source boundary |

## Offline Up/Down and preservation evidence

- EF discovery: 13 migrations, REV869B exactly once after REV869A.
- Pending-model check: `No changes have been made to the model since the last migration.`
- In-process model-differ test: no migration/snapshot differences.
- Up: one `START TRANSACTION`, one `COMMIT`, 15 table creates, 16 trigger creates.
- Up SHA-256: `e48b74e3c057e5f648ed6d87405ee130e006230154ef30755a2646c535cf3481` (matches required value).
- Down: one `START TRANSACTION`, one `COMMIT`, 15 owned table drops, 3 owned function drops.
- Down SHA-256: `5af0c755302580dff3792b22784c8c7540a766e87fe115089ba0ec438e687618` (matches required value).
- REV868/REV868C3/REV869A preservation: VERIFIED_PASS at source boundary. Up has zero existing-table alterations/drops and seeds only REV869B pages, permissions, and policies. Existing PRs, approval histories, reservations, employees, departments, mappings, and audits are referenced through restrictive FKs and are not mutated.
- Database preservation/application/rollback behavior: NOT RUN and not claimed.

## Validation

- Build: PASS, 0 warnings and 0 errors.
- Focused REV869B tests: PASS, 26 passed, 0 failed, 0 skipped; insufficient coverage is N-07/R-12.
- Complete non-PostgreSQL tests: PASS, 411 passed, 0 failed, 0 skipped.
- EF no-connect discovery: PASS, 13 total and REV869B exactly once.
- Pending model changes: PASS, none detected without database access.
- Migration/model/snapshot parity: PASS.
- Offline Up/Down generation: PASS; hashes match required values.
- Permission/authorization static scan: completed; findings N-05 and N-07 remain.
- Secret/privacy/safety scan: PASS, zero secret, employee-PII, or executable database-operation matches in the report diff.
- `git diff --check`: PASS.
- PostgreSQL tests/application: NOT RUN.

## Final states

- `rev869b_source_safety_state=FAIL`
- `rev869b_execution_helper_readiness_state=FAIL`

Execution tooling must not be created or approved while N-01 through N-07 and unresolved R-02, R-07, R-12/I-02 remain. A new correction checkpoint and another independent source re-review are required before any isolated database preflight/application workflow.
