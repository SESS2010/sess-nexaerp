# REV869B Purchase Transaction Foundation — Source Implementation Checkpoint

- Starting commit: `0d656be39a2a0b9cbffd013466287562a22b1702`
- Migration ID: `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`
- Checkpoint date: 2026-08-11 (Asia/Calcutta)
- Source implementation state: **PASS**
- PostgreSQL/database acceptance state: **NOT RUN**
- Frontend/REV869C state: **NOT STARTED**

## Boundary and preservation

REV869B implements only the source-side Purchase Transaction Foundation: existing approved PR/PendingRFQ handoff reuse, RFQ, vendor invitations, immutable quotation revisions, technical verification, commercial comparison/recommendation, amount-based approval, versioned PO lifecycle, and Material Follow-up handoff. It does not implement frontend, GRN, receipt QC, inventory ledger, issue/return, vendor performance, finance posting, Customer Master, Project Master, production OIDC, REV861, or REV869C.

The migration creates only REV869B-owned objects and source-owned permission/configuration rows. It contains no `AlterColumn`, `DropColumn`, or drop of a REV868/REV868C3/REV869A table. Existing PRs, approval history, PendingRFQ handoffs, reservations, employees, departments, identity/scope configuration, vendor qualification, tax/UOM data, permissions, and audits remain source-preserved. Database preservation must still be proven later on an isolated accepted clone.

Legacy reuse was limited to `outputs/rev869_legacy_reuse_final_mapping_report.md`; no legacy application/file execution or schema import occurred.

## Changed files

1. `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs`
2. `src/SESS.NexaERP.Api/Program.cs`
3. `src/SESS.NexaERP.Application/Purchase/Rev869BPurchaseContracts.cs`
4. `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`
5. `src/SESS.NexaERP.Infrastructure/DependencyInjection.cs`
6. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.cs`
7. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`
8. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BSeedData.cs`
9. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
10. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs`
11. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
12. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
13. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
14. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs`
16. `outputs/rev869b_source_implementation_checkpoint.md`

## Migration schema contract

The one logical migration contains 15 new tables, 298 columns, 15 primary keys, 66 indexes, 44 foreign keys, 27 check constraints, and six immutable UPDATE/DELETE rejection triggers.

| Table | Columns | FKs | Checks | Purpose and principal columns |
|---|---:|---:|---:|---|
| `purchase_transaction_approval_policies` | 14 | 0 | 2 | Organization/effective-dated route, min/max TotalPayableValue, approver role |
| `purchase_transaction_status_history` | 18 | 1 | 1 | Immutable entity/document transition, actor, role, remarks, correlation |
| `request_for_quotations` | 22 | 4 | 3 | Organization-scoped RFQ number/sequence, PR, department, warehouse, owner, due date, currency, single-source evidence, status/idempotency |
| `request_for_quotation_lines` | 22 | 4 | 1 | Existing handoff/PR-line/item links and immutable PR/item/UOM/quantity/date snapshots |
| `rfq_vendor_invitations` | 13 | 2 | 1 | Unique RFQ/vendor invitation and qualification/due-date snapshot |
| `vendor_quotations` | 28 | 4 | 4 | Organization quotation number, root/previous revision, current flag, late authorization, controlled terms, total |
| `vendor_quotation_lines` | 26 | 3 | 2 | Quantity/rate/charge/tax component/round-off/total snapshots and effective Tax/GST rule reference |
| `quotation_technical_verifications` | 12 | 2 | 1 | Separate immutable verifier decision, evidence, remarks, timestamp |
| `commercial_comparisons` | 22 | 4 | 3 | Comparison number, RFQ, recommended quote/vendor, total, approval route, recommendation/single-source evidence |
| `commercial_comparison_lines` | 17 | 3 | 1 | Technical/commercial/delivery/warranty/payment snapshots and explicit recommendation reason |
| `purchase_transaction_approval_history` | 16 | 2 | 1 | Immutable approve/reject/revise/resubmit history and actor/correlation evidence |
| `purchase_orders` | 38 | 6 | 4 | Organization/PO/revision identity, previous/current version, approved comparison/vendor, scope, status, full commercial/term snapshots |
| `purchase_order_lines` | 21 | 5 | 1 | PR/handoff/item/comparison links, ordered/outstanding quantities, rate, commercial/tax snapshots |
| `purchase_order_history` | 16 | 2 | 1 | Immutable issue/amend/supersede/cancel history with reason and actor |
| `material_followup_handoffs` | 13 | 2 | 1 | One future follow-up handoff per PO line, immutable ordered quantity snapshot |

Important unique contracts include organization/number and organization/idempotency keys; RFQ/handoff, RFQ/vendor, quote root/current revision, invitation/revision, comparison/RFQ, PO root/revision, organization/PO/revision, PO root/current version, PO/comparison/revision, PO-line/comparison-line, material-follow-up/PO-line, and history correlation keys. All business FKs use restrictive deletion. `Version` is a PostgreSQL `xid` concurrency token.

`Up` is transaction-scoped by generated SQL, creates tables before dependent indexes/seeds/triggers, resolves the existing active `DEPARTMENT_MANAGER` role fail-closed, inserts exactly three migration-owned scoped view/clarification permissions, and creates immutable triggers last. `Down` first deletes only those three rows by deterministic ID plus `CreatedBy='migration-rev869b'`, drops dependent REV869B tables, drops the now-unreferenced trigger function, removes deterministic seed rows/pages, and does not delete unrelated business/history/audit rows.

## Backend behavior

- Requires an authenticated unique employee ID and organization; no employee name/code or approver ID is hard-coded.
- Applies `IRecordScopeAuthorizer` to department/warehouse/owner targets in services and read APIs; denied record-scope attempts are audited.
- Reuses existing `PurchaseRequirementHandoff` rows in `PendingRFQ`; never recreates PR data.
- Uses serializable transactions, organization-scoped `PurchaseNumberSequence` prefixes (`RFQ`, `VQ`, `CMP`, `PO`), unique indexes, idempotency keys, and concurrency tokens.
- Controlled RFQ splits cannot exceed handoff/outstanding quantity. Cumulative current, non-cancelled/non-superseded PO quantity cannot exceed approved outstanding quantity.
- Vendor invitation, quotation, and PO selection call the REV869A qualification service and fail closed for inactive/unapproved/expired/missing qualification.
- Quotations must cover every RFQ line exactly once. Late submissions/revisions require Purchase Manager authorization and remarks; prior revisions are retained as `Superseded`.
- Technical verification is separate and immutable. Only current technically compliant quotations enter comparison.
- Selection is explicit and reasoned; no lowest-price auto-selection exists. Single-source RFQ/recommendation requires justification.
- GST resolves only via effective-dated REV869A `IN_GST` configuration. All documents must share one ISO three-letter currency; missing exchange-rate support fails closed rather than converting.
- PO creation requires an approved comparison. Issue creates one future Material Follow-up handoff per PO line. Commercial/controlled-term amendments create a new PO revision in `PendingReapproval`; cancellation requires authorized role, reason, timestamp, history, and audit.

## Approval and calculation

Effective-dated, organization-scoped policies route `TotalPayableValue` exactly as follows:

- ₹0–₹50,000 inclusive: `MANAGER` / `PURCHASE_MANAGER` seed, with the acting employee additionally required to match the single effective department approval mapping (Purchase Manager or Department Manager role).
- ₹50,000.01–₹5,00,000 inclusive: `TECHNICAL_DIRECTOR`.
- ₹5,00,000.01 and above: `MANAGING_DIRECTOR`.

Missing/overlapping policy, missing/ambiguous manager mapping, wrong role, missing identity/scope, or self-approval fails closed. Reject/revision/resubmit/approval actions require remarks and immutable history.

Per-line calculations retain quantity, unit rate, discount, packing/forwarding, freight, insurance, other charges, taxable value, CGST, SGST, IGST, cess, round-off, and total payable value. Rounding uses the resolved Tax/GST rule scale (0–6) and `MidpointRounding.AwayFromZero`. Taxable value and every tax/charge component remain separately persisted.

## API endpoints

All routes require authentication, unique employee/scope resolution, page permission, and record-scope checks:

- `POST /api/v1/purchase/rfqs`
- `POST /api/v1/purchase/rfqs/{number}/vendors`
- `GET /api/v1/purchase/rfqs/{number}`
- `POST /api/v1/purchase/rfq-invitations/{id}/quotations`
- `POST /api/v1/purchase/quotations/{number}/technical-verifications`
- `POST /api/v1/purchase/comparisons`
- `GET /api/v1/purchase/comparisons/{number}`
- `POST /api/v1/purchase/comparisons/{number}/recommend`
- `POST /api/v1/purchase/comparisons/{number}/approve`
- `POST /api/v1/purchase/comparisons/{number}/reject`
- `POST /api/v1/purchase/comparisons/{number}/request-revision`
- `POST /api/v1/purchase/comparisons/{number}/resubmit`
- `POST /api/v1/purchase/purchase-orders`
- `GET /api/v1/purchase/purchase-orders/{number}`
- `POST /api/v1/purchase/purchase-orders/{number}/issue`
- `POST /api/v1/purchase/purchase-orders/{number}/amend`
- `POST /api/v1/purchase/purchase-orders/{number}/approve-amendment`
- `POST /api/v1/purchase/purchase-orders/{number}/cancel`
- `GET /api/v1/purchase/material-followup`

## Role/page permission matrix

| Role | Effective REV869B rights |
|---|---|
| Purchase Executive | Scoped RFQ/invitation and quotation entry/submission; commercial visibility; no approval |
| Purchase Manager | Scoped RFQ/quote operations, technical/comparison visibility, recommendation, Manager approval, PO create/issue/amend/cancel, audit/export |
| Technical Engineer | Scoped RFQ/quote visibility and technical verification only |
| Department Manager | Originating scoped RFQ/comparison/PO view, clarification, print/download/audit; no transaction mutation or commercial-value grant |
| Technical Director | Explicitly scoped/privileged technical and configured commercial approval; oversight/read/export |
| Managing Director | Highest configured approval and privileged oversight; Vendor final approval remains the separate REV869A policy |
| Stores Manager/Executive | Approved/current PO and Material Follow-up view only in REV869B |
| Accounts Head | Approved comparison/PO commercial view/export only |
| Unauthorized | Menu/page permission denial, direct endpoint denial, API denial, record-scope denial and audit evidence |

Generated seed model contains 4 new pages, 48 deterministic fixed-role/page rows, and 3 approval policies. The migration adds 3 deterministic Department Manager rows by fail-closed lookup of the pre-existing role.

## Validation evidence

- PowerShell 5.1 parsing: **N/A** — no PowerShell file was added or modified.
- Build: **PASS**, 0 warnings, 0 errors.
- Focused REV869B tests: **PASS**, 17 passed, 0 failed, 0 skipped.
- Complete non-PostgreSQL suite: **PASS**, 403 passed, 0 failed, 0 skipped; all three PostgreSQL-backed test classes were explicitly excluded.
- EF discovery: **PASS** with `--no-connect`; 13 migrations listed and REV869B discovered exactly once after the 12 accepted migrations.
- Pending-model-change check: **PASS** — “No changes have been made to the model since the last migration.”
- Offline Up SQL: **PASS**; `START TRANSACTION`/`COMMIT`, 15 creates, required permissions/triggers; SHA-256 `68d6fa87cf1a16fb9fa712e80fc8d8dd7db14e69201dd9f727098f470a29be7c`.
- Offline Down SQL: **PASS**; transaction/commit, 15 owned drops, ownership-scoped permission cleanup and trigger-function cleanup; SHA-256 `f00ebe354df25bd5205557bcd7412b368017aceecb7e3570e8cc9c0204972680`.
- Auth/permission/calculation/migration negative tests: **PASS** within the focused suite.
- Secret scan: **PASS**, 0 authored-source matches.
- Privacy scan: **PASS**, 0 employee-code/email matches in authored REV869B source/executable migration. EF Designer contains pre-existing accepted seed copies and was not treated as newly authored PII.
- Prohibited database-operation scan: **PASS**, 0 matches.
- `git diff --check`: **PASS**.

## Remaining blockers and future execution boundary

- No PostgreSQL migration application or PostgreSQL-backed REV869B test has run; database acceptance must not be claimed.
- A dedicated fail-closed REV869B isolated preflight/apply/post-verification helper and approved target/backup/preservation evidence do not yet exist. The REV869A helper is hard-bound to REV869A and must not be reused.
- Therefore no truthful future `GeneratePlanOnly` command is emitted in this checkpoint. A separate approved source-only execution-tooling checkpoint must create and validate that helper first.
- Production OIDC activation/testing, production deployment, and frontend remain blocked/out of scope.
- REV869C must not begin until isolated REV869B database/schema/preservation/test acceptance is separately proven.

No helper, PostgreSQL command, migration apply/remove, database create/drop/backup/restore, production, sess_nexaerp, REV861, frontend, legacy application, or REV869C operation occurred in this implementation checkpoint.