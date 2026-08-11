# REV869B Independent Pre-Application Source Safety Review

## Decision

- `starting_commit=de78ca95254642e7895284fb3e3be9c4e21dac77`
- `migration_id=20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`
- `review_mode=SOURCE_ONLY_READ_ONLY`
- `postgresql_access=NOT_PERFORMED`
- `rev869b_source_safety_state=FAIL`
- `rev869b_execution_helper_readiness_state=FAIL`
- `generate_plan_only_command=NOT_PROVIDED`

REV869B is not safe to apply. The committed source contains Blocking and Required Correction findings. Most importantly, successful technical verification attempts to persist quotation statuses rejected by the migration's own check constraint, optimistic concurrency tokens never change, several multi-record state/history/audit operations are not atomic, and authorization/permission gaps expose commercial values or make required actions unreachable.

No source, migration, snapshot, test, helper, configuration, database, legacy application, production system, REV861, frontend, or REV869C artifact was changed or executed during this review. This report is the only created file.

## Exact reviewed files

Primary REV869B files:

- `outputs/rev869b_source_implementation_checkpoint.md` (claims used only as a cross-check)
- `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs`
- `src/SESS.NexaERP.Api/Program.cs`
- `src/SESS.NexaERP.Application/Purchase/Rev869BPurchaseContracts.cs`
- `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`
- `src/SESS.NexaERP.Infrastructure/DependencyInjection.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BSeedData.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
- `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
- `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
- `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
- `tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs`

Supporting contracts and controls traced:

- `src/SESS.NexaERP.Domain/Common/AuditableEntity.cs`
- `src/SESS.NexaERP.Domain/Purchase/PurchaseRequisition.cs`
- `src/SESS.NexaERP.Domain/Authorization/RolePagePermission.cs`
- `src/SESS.NexaERP.Application/Common/ICurrentUser.cs`
- `src/SESS.NexaERP.Application/Authorization/IRecordScopeAuthorizer.cs`
- `src/SESS.NexaERP.Application/Authorization/IPagePermissionService.cs`
- `src/SESS.NexaERP.Infrastructure/Authorization/EfRecordScopeAuthorizer.cs`
- `src/SESS.NexaERP.Infrastructure/Authorization/EfPagePermissionService.cs`
- `src/SESS.NexaERP.Api/Security/ClaimsCurrentUser.cs`
- `src/SESS.NexaERP.Api/Security/PagePermissionEndpointFilter.cs`
- `src/SESS.NexaERP.Infrastructure/Masters/EfRev869AFoundationServices.cs`
- `src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.Rev869A.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Rev869ASeedData.cs`
- REV868/REV868C3/REV869A migration and focused test sources used by the referenced relationships.

## Implemented flow and business reuse

The source intends this sequence:

`Approved PR / PendingRFQ handoff -> RFQ -> vendor invitation -> versioned quotation -> technical verification -> commercial comparison -> recommendation -> amount approval -> PO -> issue/amend/cancel -> Material Follow-up handoff`

| Control | Classification | Independent result |
|---|---|---|
| Reuse existing PR/PendingRFQ | VERIFIED_PASS | RFQ creation loads existing `PurchaseRequirementHandoffs`, requires one PR, a qualifying approved state, `PendingRFQ`, a valid Item reference, and never inserts a PR (`EfRev869BPurchaseService.RfqQuotation.cs:22-41`). |
| Eligible outstanding quantities | VERIFIED_PASS | Active RFQ quantity is summed per handoff, cancelled RFQs are excluded, requested quantity must be positive and within handoff and current PO-adjusted outstanding quantity (`RfqQuotation.cs:31-40`). |
| Split sourcing | VERIFIED_PASS | Separate RFQs may consume partial quantities while a serializable transaction and cumulative active-RFQ sum prevent exceeding the handoff (`RfqQuotation.cs:20,32-33`). |
| Cumulative PO quantity | VERIFIED_PASS | PO creation is serializable and compares each new quantity with the approved outstanding snapshot after summing current, non-cancelled, non-superseded PO versions (`EfRev869BPurchaseService.cs:40`; `ComparisonPo.cs:47-57`). |
| Cancelled/superseded versions | VERIFIED_PASS | `OrderedQuantityAsync` counts only current versions and excludes both statuses (`EfRev869BPurchaseService.cs:40`). |
| Follow-up from issued current PO | VERIFIED_PASS | Issue loads the current PO, requires `Approved`, changes it to `Issued`, and creates one handoff per PO line; reads filter current issued POs (`ComparisonPo.cs:63-64`; `Rev869BPurchaseEndpoints.cs:50-52`). |
| Explicit exclusions | VERIFIED_PASS | The REV869B domain/service contains no GRN, inventory-ledger, material issue/return, or finance posting flow. |
| End-to-end completion | BLOCKING | The sequence stops at technical completion because `TechnicallyCompliant`/`TechnicallyRejected` cannot satisfy the quotation status check; see B-01. |

## Tables and migration inventory

Independent source counts match 15 tables, 298 columns, 66 indexes, 44 foreign keys, 27 checks, and 6 immutable triggers.

| Physical table | Purpose |
|---|---|
| `nexa.request_for_quotations` | Organization-scoped RFQ header and sequence/idempotency data. |
| `nexa.request_for_quotation_lines` | Immutable sourcing snapshots linked to existing PR lines and PendingRFQ handoffs. |
| `nexa.rfq_vendor_invitations` | RFQ/vendor invitation and qualification snapshot. |
| `nexa.vendor_quotations` | Organization-scoped versioned quotation header. |
| `nexa.vendor_quotation_lines` | Commercial and tax snapshot per RFQ line. |
| `nexa.quotation_technical_verifications` | Per-quotation-line technical decision/evidence. |
| `nexa.commercial_comparisons` | Organization-scoped comparison, recommendation, approval route and value. |
| `nexa.commercial_comparison_lines` | Quote-line commercial/technical snapshots. |
| `nexa.purchase_transaction_approval_history` | Comparison approval/reject/revision/resubmit history. |
| `nexa.purchase_orders` | Organization-scoped, versioned PO header. |
| `nexa.purchase_order_lines` | PR/handoff/comparison commercial snapshot for each PO line. |
| `nexa.purchase_order_history` | PO creation, issue, amendment, approval and cancellation history. |
| `nexa.material_followup_handoffs` | Issued PO-line handoff to future material follow-up. |
| `nexa.purchase_transaction_status_history` | Cross-document append-only status history. |
| `nexa.purchase_transaction_approval_policies` | Organization/effective-dated amount routes. |

All foreign keys use restrictive behavior in the reviewed mapping. No cascade was found that can remove the controlled histories or snapshots. The six triggers protect quotation lines, technical verification records, approval history, PO lines, PO history, and transaction status history. Trigger calls and function ownership are schema-qualified. No `ON CONFLICT` exists, so an arbiter review is not applicable.

## Endpoint inventory

All routes are under `/api/purchase-transactions`, require authentication, and pass through employee/scope and page-permission filters before handler execution.

Writes:

- `POST /rfqs`
- `POST /rfqs/{number}/vendors`
- `POST /rfq-invitations/{id}/quotations`
- `POST /quotations/{number}/technical-verifications`
- `POST /comparisons`
- `POST /comparisons/{number}/recommend`
- `POST /comparisons/{number}/approve`
- `POST /comparisons/{number}/reject`
- `POST /comparisons/{number}/request-revision`
- `POST /comparisons/{number}/resubmit`
- `POST /purchase-orders`
- `POST /purchase-orders/{number}/issue`
- `POST /purchase-orders/{number}/amend`
- `POST /purchase-orders/{number}/approve-amendment`
- `POST /purchase-orders/{number}/cancel`

Reads:

- `GET /rfqs/{number}`
- `GET /comparisons/{number}`
- `GET /purchase-orders/{number}`
- `GET /material-followup`

## Findings

### BLOCKING

#### B-01 — Quotation terminal technical statuses violate the database check

The service assigns `VendorQuotation.Status` to `TechnicallyCompliant` or `TechnicallyRejected` when every line has been verified (`EfRev869BPurchaseService.RfqQuotation.cs:85-86`). The mapped and migrated `CK_vendor_quotation_status` permits only `Submitted`, `PendingTechnicalVerification`, `Superseded`, `Withdrawn`, and `Rejected` (`NexaErpDbContext.Rev869B.cs:77`; migration line 284). The second `SaveChangesAsync` therefore fails, preventing a quotation from becoming selectable for comparison (`ComparisonPo.cs:17`). The first save at line 85 may already have committed the final line verification, worsening recovery.

#### B-02 — Optimistic concurrency tokens do not advance

`AuditableEntity.Version` is `uint` (`AuditableEntity.cs:10`), persisted as PostgreSQL `bigint` in all 15 tables (migration lines 34 through 792) and marked only with `.IsConcurrencyToken()` (`NexaErpDbContext.Rev869B.cs:42-177`). No mapping uses row-version/value-generated behavior, trigger updates Version, or service increments Version. `CheckVersion` compares the request to the in-memory value (`EfRev869BPurchaseService.cs:73`) but cannot prevent two concurrent requests that both read Version 0 from succeeding. Concurrency safety is therefore not enforced.

#### B-03 — Required commercial-value permission is bypassed on reads

The comparison and PO GET handlers return the complete entities/lines, including commercial snapshots, unit rates, taxes and totals, after checking only `CanView` (`Rev869BPurchaseEndpoints.cs:32-33,42-48`). They never enforce `CanViewCommercialValues`, although that permission exists (`EfPagePermissionService.cs:49-52`). Stores and Department Manager are intentionally seeded without commercial-value permission yet can view PO/comparison records. This is a direct API confidentiality failure, not merely a UI concern.

#### B-04 — Required PO amendment approval endpoint has no seeded approver

`POST /purchase-orders/{number}/approve-amendment` requires `CanApprove` on `purchase.po` (`Rev869BPurchaseEndpoints.cs:29`). The seed computes `CanApprove` only when the page is `purchase.commercial-comparisons` (`Rev869BSeedData.cs:77`), including for TD/MD. No seeded role can call the required endpoint, so the amendment lifecycle cannot complete.

#### B-05 — Cross-record organization/parent substitution is not comprehensively prevented

Quotation submission loads an invitation solely by ID, and technical verification loads a quotation line by line ID plus quotation number without asserting the current user's organization in the database predicate (`RfqQuotation.cs:60,83`). Recommendation loads the selected quotation by ID (`ComparisonPo.cs:26`); PO creation loads related RFQ and quotation by IDs (`ComparisonPo.cs:49`); comparison scope loads its RFQ by ID (`EfRev869BPurchaseService.cs:32`). The later scope call uses the loaded record's organization rather than first proving it equals `RequireOrganization()`. A multi-organization scoped identity can therefore substitute cross-organization identifiers. Database FKs are also independent and do not prove that a quotation line belongs to its quote's RFQ, that comparison vendor/quote/line references agree, that PO line PR/handoff/item/comparison references share one parent chain, or that a follow-up PO line belongs to its PO.

### REQUIRED_CORRECTION

#### R-01 — Multi-record state/history/audit writes are not consistently atomic

Technical verification has no explicit transaction and performs two saves before audit (`RfqQuotation.cs:80-87`). Recommendation, resubmission, approval/rejection/revision, PO amendment approval, and PO cancellation also save state/history before a separate audit write without an explicit transaction (`ComparisonPo.cs:23-28,35-42,75-82`). Audit failure can leave committed business state/history without its required audit evidence; the technical flow can leave a verification record without the quotation/status history update.

#### R-02 — Idempotency is incomplete and replay checks are unsafe

RFQ and comparison creation return the row found by idempotency key before record-scope authorization or payload-equivalence verification (`RfqQuotation.cs:21`; `ComparisonPo.cs:15`). Invite, quotation revision, recommendation, approvals, issue, amendment, cancellation and handoff creation do not consistently return a deterministic prior result on an exact retry. Unique indexes cause some retries to fail, but failure is not idempotent success and conflicting payload reuse is not distinguished. Material follow-up's unique PO-line constraint prevents duplicates but an issue replay fails the status/version guard instead of returning the existing handoff result.

#### R-03 — Vendor category qualification is skipped at final PO selection

Invitation and quotation submission correctly check every RFQ item category (`RfqQuotation.cs:51,63`). PO creation rechecks only `IsEligibleAsync(..., itemCategoryId: null, ...)` (`ComparisonPo.cs:49`). The foundation service returns true after header eligibility when the category is null (`EfRev869AFoundationServices.cs:42-45`). An expired/revoked category qualification after quotation submission can therefore reach PO creation.

#### R-04 — Quotation provenance and attachments are insufficient

The quotation request/entity records vendor reference, `SubmittedAt`, and entering `CreatedBy`, but has no submission source/channel, vendor actor/attestation, attachment ID, object-storage reference, hash, or received-document evidence (`Rev869BPurchaseContracts.cs:13-16`; `Rev869BPurchaseTransactions.cs:137-166`). Only internal Purchase roles may call submission (`RfqQuotation.cs:59`; endpoint line 18), so an employee can enter a record that appears as a vendor quotation without durable evidence distinguishing received-on-behalf-of-vendor from vendor-originated submission.

#### R-05 — Technical verifier/commercial approver segregation is not employee-enforced

Technical verification permits Technical Engineer or TD (`RfqQuotation.cs:82`). Comparison approval permits the configured TD/MD role (`EfRev869BPurchaseService.cs:49-63`). No query prevents the same employee who supplied a technical verification from commercially approving the comparison. Role separation alone does not meet the required person-level segregation.

#### R-06 — Permission seed is not least privilege and contains all-false rows

The deterministic matrix creates 8 roles × 6 pages = 48 rows plus 3 Department Manager rows. From `Rev869BSeedData.cs:56-90`, 11 rows are entirely false: Technical Engineer on comparison, PO and follow-up (3), and each Stores role on RFQ, quotation, technical verification and comparison (8). Purchase Executive is granted view of all six pages and commercial values on every row, despite having write duties only for RFQ/quotation. Accounts has export/commercial/audit flags even on four pages it cannot view. These are incoherent or overly broad grants, and the focused test merely asserts that 48 rows exist (`Rev869BPurchaseFoundationTests.cs:46-53`).

#### R-07 — Validation/conflict/not-found API semantics are collapsed

The endpoint wrapper maps every `InvalidOperationException` to HTTP 409 (`Rev869BPurchaseEndpoints.cs:56-60`). Required-field and calculation validation therefore return conflict instead of 400. Several `SingleAsync` loads turn absent resources into unhandled errors rather than 404. Forbidden is distinct, but the required validation/conflict/not-found distinctions are not implemented.

#### R-08 — Material Follow-up list is unbounded

`GET /material-followup` materializes every current issued handoff for the organization, then performs per-row authorization in memory (`Rev869BPurchaseEndpoints.cs:50-52`). There is no page size, cursor, limit, date/status filter, or bounded export behavior.

#### R-09 — Record-specific read denials are not audited

Service scope denials are audited (`EfRev869BPurchaseService.cs:34-37`) and page-filter denials are audited, but GET handlers simply return `Forbid` when `Allowed` is false (`Rev869BPurchaseEndpoints.cs:40-52`). Direct URL/API record-scope denials therefore lack the required denial audit.

#### R-10 — Immutable database coverage is incomplete

The six triggers correctly protect the selected append-only rows and do not appear to block the implemented lifecycle. However, the database does not prevent mutation of completed/current quotation header provenance/terms, recommended comparison snapshots, or issued PO header commercial/term snapshots. Application guards alone are insufficient for the stated immutable snapshot/history requirement.

#### R-11 — Globally unique follow-up numbering is not organization-safe

PO numbering is unique per organization, but `HandoffNumber` is generated only as `MFU-{PoNumber}-{line}` (`ComparisonPo.cs:64`) and has a global unique index (`NexaErpDbContext.Rev869B.cs:162`). Two organizations with the same PO number/line collide. `RfqVendorInvitation.IdempotencyKey` is also globally unique rather than organization/parent-scoped (`NexaErpDbContext.Rev869B.cs:64`).

#### R-12 — Focused tests do not establish behavioral/database safety

Only the approval and commercial-calculator tests execute domain behavior. Most of the 17 focused tests assert source strings, migration text, or counts (`Rev869BPurchaseFoundationTests.cs:46-125`). There are no service/API/EF behavioral tests for transactions, concurrency, cross-organization substitution, idempotency, permission masking, DB checks/FKs/triggers, or rollback ownership. The green focused test run therefore does not contradict the defects above.

### IMPROVEMENT

#### I-01 — Effective approval policy overlap is fail-closed but not prevented

The null-safe unique index covers identical `(OrganizationId, RouteCode, EffectiveFrom, EffectiveTo)` keys (`NexaErpDbContext.Rev869B.cs:177`), while different overlapping ranges remain possible. Runtime resolution correctly requires exactly one amount/date match and fails closed (`Rev869BPurchaseTransactions.cs:35-40`). Add a controlled overlap rule before providing configuration writes.

#### I-02 — Explicit commercial maximum validation would improve API behavior

Negative values and invalid rates/scales are rejected, and database `numeric(24,6)` bounds persistence. Explicit range/overflow validation before database persistence would provide deterministic validation errors for extreme values.

#### I-03 — Down seed deletion should retain ownership guards consistently

The three manually inserted Department Manager permission rows are deleted by deterministic ID plus `CreatedBy='migration-rev869b'` (migration lines 1353-1356). Generated `DeleteData` calls remove the other deterministic IDs without checking `CreatedBy`. They are migration-created IDs, but consistent ownership guards would protect against a later reassignment before rollback.

## Vendor and quotation review

- VERIFIED_PASS: active/approved/effective vendor header eligibility is inherited from REV869A; category qualification is effective-dated and required at invitation/submission (`RfqQuotation.cs:51,63`; `EfRev869AFoundationServices.cs:40-45`).
- VERIFIED_PASS: final vendor approval remains driven by `VENDOR_FINAL_APPROVER`; missing/ambiguous policy fails closed, and SESS maps it to Managing Director without an employee ID (`MasterEndpoints.Rev869A.cs:78-84`; `EfRev869AFoundationServices.cs:48-53`).
- VERIFIED_PASS: no vendor, employee, or approver employee ID is hard-coded.
- VERIFIED_PASS: duplicate RFQ/vendor invitations have both a service check and unique `(RequestForQuotationId, VendorId)` index (`RfqQuotation.cs:50`; mapping line 64).
- VERIFIED_PASS: late submissions/revisions require Purchase Manager, an explicit request flag and remarks (`RfqQuotation.cs:64`).
- VERIFIED_PASS: prior quotation revisions are retained, marked non-current/superseded, and line rows are immutable (`RfqQuotation.cs:65-77`; migration trigger at line 1342).
- VERIFIED_PASS: single-source RFQ/recommendation requires justification and still proceeds through explicit configured approval (`RfqQuotation.cs:19`; `ComparisonPo.cs:26-28`).
- VERIFIED_PASS: comparison loads only current technically compliant quotations and does not select the lowest price automatically (`ComparisonPo.cs:17-19,25-28`).
- BLOCKING/REQUIRED_CORRECTION: status constraint, PO-time category revalidation, provenance/attachment, impersonation, and duty-segregation findings are B-01, R-03, R-04 and R-05.

## Calculation review

The independently recomputed example used by the focused test is consistent:

- quantity × rate: `3 × 100 = 300.00`
- discount: `10.00`; tax base `290.00`
- packing/forwarding `2.00`, freight `3.00`, insurance `4.00`, other `5.00`
- CGST 9%: `26.10`; SGST 9%: `26.10`; IGST: `0.00`; cess 1%: `2.90`
- round-off input: `0.005`
- total: `300 - 10 + 26.10 + 26.10 + 0 + 2.90 + 2 + 3 + 4 + 5 + 0.005 = 359.105`, rounded away from zero to `359.11`

`Rev869BCommercialCalculator` preserves gross taxable value separately from discount and total payable value (`Rev869BPurchaseTransactions.cs:54-72`). Quotation submission resolves effective-dated India GST from REV869A and snapshots the resolved rule (`RfqQuotation.cs:72-75`). The resolver governs intrastate/interstate components. Quantity must be positive; rate, discount and charges must be nonnegative; discount cannot exceed gross taxable value; each tax rate must be 0–100; rounding scale must be 0–6. Zero rate/charges and threshold values are controlled. Persistence uses `numeric(24,6)`.

Currency is normalized to a three-letter code and quotations must exactly equal RFQ currency; no conversion is attempted or guessed (`RfqQuotation.cs:61`; `ComparisonPo.cs:17`; `EfRev869BPurchaseService.cs:74`). Multi-currency comparison is therefore blocked safely rather than converted without an approved source.

## Approval matrix

| Total payable value | Route | Seeded role | Result |
|---:|---|---|---|
| ₹0.00–₹50,000.00 | `MANAGER` | `PURCHASE_MANAGER` plus exact effective Department Manager mapping authorization | VERIFIED_PASS |
| ₹50,000.01–₹5,00,000.00 | `TECHNICAL_DIRECTOR` | `TECHNICAL_DIRECTOR` | VERIFIED_PASS |
| ₹5,00,000.01 and above | `MANAGING_DIRECTOR` | `MANAGING_DIRECTOR` | VERIFIED_PASS |

The boundaries are gap-free at supported two-decimal currency precision, organization-scoped, configurable and effective-dated (`Rev869BSeedData.cs:22-27`; `Rev869BPurchaseTransactions.cs:35-40`). Missing/ambiguous policy or manager mapping fails closed. Creator login cannot approve its own comparison/PO (`EfRev869BPurchaseService.cs:49-63`). Reject/revision/resubmit/cancellation require remarks and create histories. PO controlled-term amendment creates a new `PendingReapproval` version. Findings B-04, R-01 and R-05 still prevent acceptance of the complete approval design.

## Permission review

Six page keys are used: `purchase.rfq`, `purchase.vendor-quotations`, `purchase.technical-verification`, `purchase.commercial-comparisons`, `purchase.po`, and `purchase.material-followup`.

- Purchase Manager: views all six; creates/updates RFQ, quotation, comparison and PO; submits RFQ, quotation and comparison; approves comparison; cancels RFQ, quotation and PO; broad commercial/export/audit access.
- Purchase Executive: views all six; creates/updates/submits RFQ and quotation; commercial-value access on all six.
- Technical Engineer: views RFQ, quotation and technical pages; writes/verifies technical records; three all-false rows.
- TD/MD: view all six; verify technical/comparison; approve comparison; cancel RFQ/quotation/PO; broad commercial/export/audit access. MD has full control flags.
- Stores Manager/Executive: view/print/download PO and follow-up; eight combined all-false rows elsewhere.
- Accounts Head: views comparison/PO, but export/commercial/audit flags are set on all six rows.
- Department Manager: three migration SQL rows for RFQ/comparison/PO with view, request-clarification, print, download and audit-history only (`migration lines 1323-1337`).

The matrix fails least-privilege acceptance for B-03, B-04 and R-06.

## Migration Up/Down and ownership review

- VERIFIED_PASS: Up creates dependencies before dependent indexes/FKs/triggers.
- VERIFIED_PASS: all FKs are restrictive and the 15 REV869B tables are migration-owned.
- VERIFIED_PASS: no existing REV868/REV868C3/REV869A table is altered or dropped; source scan found zero `AlterColumn` and zero `DropColumn` in Up.
- VERIFIED_PASS: no business vendor, employee, quotation or PO row is seeded. Seeds are four new page rows, three approval-policy rows, 48 deterministic role/page permission rows, and three Department Manager permission rows against the existing role/pages.
- VERIFIED_PASS: deterministic seed collisions fail the transaction; no conflict-ignore behavior exists.
- VERIFIED_PASS: Down first deletes the three manually inserted permissions with ownership guard, drops dependent tables, removes deterministic permission/policy/page rows, removes six triggers with their tables, and drops the migration-owned function. It does not delete PRs, employee data, earlier histories, foundation tables or roles.
- VERIFIED_PASS: offline Up SQL is 80,134 characters, contains 15 `CREATE TABLE` statements, starts one transaction and commits once. Offline Down SQL is 5,943 characters, contains 15 `DROP TABLE` statements, starts one transaction and commits once.
- VERIFIED_PASS: migration designer target model and snapshot bodies are exactly equal after whitespace normalization: both 1,200,998 characters and SHA-256 `81BE623029F4C73A9E63C433CDAC42817639619D971DA968F1CDBCEBC943B89D`.
- REQUIRED_CORRECTION: B-01, B-02, B-05, R-06, R-10 and R-11 show that matching metadata/object counts do not establish business safety.

## Test coverage matrix

| Scenario | Existing evidence | Classification |
|---|---|---|
| Happy path | Source-string assertions only; impossible under B-01 | BLOCKING |
| Unapproved PR | Source-string assertion; no service/DB behavior test | REQUIRED_CORRECTION |
| Duplicate RFQ / split quantity | Source strings; no concurrent behavior test | REQUIRED_CORRECTION |
| Quantity over-order | Source strings; no concurrent EF test | REQUIRED_CORRECTION |
| Invalid/expired vendor | Source strings only | REQUIRED_CORRECTION |
| Duplicate invitation | No behavioral unique-index/service test | REQUIRED_CORRECTION |
| Quote revision retention / late quote | Source strings only | REQUIRED_CORRECTION |
| Technical rejection | No behavior/constraint integration test; B-01 missed | BLOCKING |
| Single-source justification | No end-to-end approval test | REQUIRED_CORRECTION |
| Tax failure | No resolver/service behavior test | REQUIRED_CORRECTION |
| Calculation/rounding | One useful domain behavior test; missing null/zero/max/overflow matrix | IMPROVEMENT |
| Manager/TD/MD boundaries | Five domain behavior cases plus missing/ambiguous test | VERIFIED_PASS |
| Self-approval / missing identity, scope, mapping | Source strings only | REQUIRED_CORRECTION |
| Unauthorized API / commercial masking | No handler/filter behavior test; B-03 missed | BLOCKING |
| Concurrent comparison/PO | No behavioral test; B-02 missed | BLOCKING |
| Amendment/reapproval/cancellation | Source strings only; B-04 missed | BLOCKING |
| Immutable histories | Trigger-count/source string only | REQUIRED_CORRECTION |
| Cross-organization substitution | No test | BLOCKING |
| Rollback ownership | No executable Up/Down ownership test | REQUIRED_CORRECTION |
| REV868/REV868C3/REV869A preservation | No migration-backed test in REV869B suite; static no-alter scan only | REQUIRED_CORRECTION for execution acceptance |

Focused tests passed 17/17, but the test design is inadequate for behavioral acceptance. A correction checkpoint needs negative and concurrency tests that would fail for each Blocking/Required Correction above before isolated execution tooling is considered.

## Source-only validation evidence

- Starting Git commit: PASS, exact `de78ca95254642e7895284fb3e3be9c4e21dac77`.
- Initial target worktree: clean; expected sibling `../legacy-reference/` remained untracked and untouched.
- Build: PASS — 0 warnings, 0 errors.
- Focused REV869B tests: PASS — 17 passed, 0 failed, 0 skipped.
- Complete non-PostgreSQL tests: PASS — 402 passed, 0 failed, 0 skipped. Excluded exactly `Rev867C1PostgresVerificationTests`, `Rev868C1PostgresWorkflowVerificationTests`, `Rev868C3PostgreSqlWorkflowVerificationTests`, and `Rev869APostgresAcceptanceTests`.
- EF migration discovery: PASS with `migrations list --no-connect`; 13 migrations were listed and REV869B appeared exactly once after REV869A. The design-time target used host `127.0.0.1`, port `1`, and a non-business dummy database name; no connection was attempted.
- Pending model: EF CLI `has-pending-model-changes` was not executed because this installed command has no `--no-connect` option and the execution safety guard rejected it under the explicit no-PostgreSQL rule. No database access was attempted. Safe source evidence instead proves exact target-model/snapshot equality using the normalized hashes recorded above. This is not claimed as a live database or unrestricted EF pending-model acceptance.
- Offline Up/Down SQL generation: PASS for generation, object counts and single transaction boundaries; SQL remained in memory and was not executed or written.
- PostgreSQL/database acceptance: NOT RUN / NOT CLAIMED.
- Secret/privacy/safety and prohibited-operation scans: recorded after creation of this report.
- `git diff --check`: recorded after creation of this report.

## Preservation result

`rev868_rev868c3_rev869a_source_preservation_state=PASS`

The migration adds 15 REV869B-owned tables and controlled seed/configuration rows without altering or deleting existing REV868/REV868C3/REV869A tables or values. Existing PRs and PendingRFQ handoffs are referenced, not duplicated. Down does not delete earlier PRs, approvals, reservations, employees, histories, roles or REV869A foundation records. This is a source-only preservation result, not database acceptance.

## Blocking decisions and next boundary

1. Do not create REV869B isolated execution tooling and do not apply this migration.
2. Correct every Blocking and Required Correction finding in a dedicated source-only correction checkpoint.
3. Add behavioral service/API/EF tests, including status-check compatibility, real optimistic concurrency, atomic rollback, cross-parent/cross-organization substitution, commercial-value masking, exact permission reachability, idempotent replay and rollback ownership.
4. Re-run an independent pre-application review after corrections.
5. REV869C, frontend, production and production OIDC remain outside this review.
