# REV869 Integrated Purchase + Stores Implementation Plan

## 1. Checkpoint, evidence, and constraints

This source-only plan is based on committed baseline `211fc7edc2bf5630057d7093691cf7f6010461e0`, the accepted REV868C3 checkpoint, the REV869 discovery report, and the current Purchase, Stores, master, identity, authorization, API, persistence, seed, and test sources. It plans work only; it does not implement REV869.

The accepted REV868 records are authoritative. REV869 must extend them without recreating, replacing, or rewriting their business evidence. The required integrated path is:

`Approved PR -> Stock Availability Check -> Available Stock Reservation -> Shortage/PendingRFQ Handoff -> RFQ -> Vendor Quotation -> Technical and Commercial Comparison -> Purchase Order -> Material Follow-up -> GRN -> Stores Physical Verification -> QC Accepted/Rejected/Hold -> Inventory Posting -> Project/Department Reservation -> Material Issue -> Material Return -> Stock Ledger -> Aging -> Project Consumption`

This checkpoint made no source-code, migration, API, frontend, seed-data, or database-helper implementation. It did not access PostgreSQL, a backup/restore process, the main database, `sess_nexaerp`, REV861, or production.

## 2. Source findings and coverage map

### 2.1 Existing foundations that must be reused

- `PurchaseRequisition`, its lines, attachments, status history, approval history, approval routes, effective-dated workflow steps, department approver mappings, and number sequence already implement the PR foundation.
- `StockAvailabilityCheck` and its location lines already capture item, warehouse, rack/bin, on-hand, active-reserved, available, in-transit, allocated, and shortage snapshots.
- `StockReservation` and its history already represent REV868 available-stock reservation. The filtered unique guard allows only one active reservation per PR line and location.
- `PurchaseRequirementHandoff` already represents the shortage boundary with status `PendingRFQ`. The filtered unique guard allows only one pending handoff per PR line.
- Vendor, Customer, Item, Warehouse, and Rack/Bin masters have domain models, persistence mappings, authenticated backend endpoints, lifecycle/approval history, audit integration, and optimistic-concurrency versions.
- Item Category, Item Subcategory, and UOM tables exist with unique code constraints, but application contracts and maintenance endpoints are absent. Item input still persists UOM text without resolving `UomId`.
- `StockMovement` is a minimal ledger basis and is read by stock availability calculation. It has no posting service/API and lacks immutable posting identity, value, condition, project, and reversal dimensions.
- Page permissions already distinguish view, create, update, submit, verify, approve, reject, revise, resubmit, cancel, print/download/export, attachment actions, commercial-value visibility, and audit-history visibility.
- Existing routes are backend-only. No `.html`, `.css`, `.js`, `.jsx`, `.ts`, or `.tsx` frontend asset exists under `src`; seeded route strings are navigation metadata, not pages.

### 2.2 Detailed coverage and missing coverage

| Area | Existing coverage | Missing or corrective REV869 coverage |
| --- | --- | --- |
| Vendor Master | Vendor code, identity, GST/PAN, MSME, contacts/addresses, material/service categories, terms, approval/status, attachments metadata, CRUD/lifecycle/history/audit APIs. | Effective qualification by category/item, approved/blocked sourcing eligibility, compliance expiry, invitation contacts, vendor-item terms, performance status, and sourcing validation. Use the existing Vendor row; do not create an RFQ-local vendor duplicate. |
| Customer dependency | Customer master and APIs exist. PR has a free-text `CustomerReference`. | Customer is not intrinsically required for internal department/project procurement. Where procurement or consumption is customer/project-backed, add an optional validated `CustomerId`/project relationship while retaining snapshots. Decide the project/customer system of record before schema work; do not force a customer on internal stock demand. |
| Item Master | Item category/subcategory FKs, UOM text and optional FK, HSN/SAC, GST percentage, QC/tracking flags, preferred vendor, min/max/reorder, price, lifecycle/history APIs. | Canonical UOM enforcement, category/UOM lookup APIs, precision rules, tax effective dating, QC plan/inspection attributes, inventory valuation method, shelf-life/batch/serial operational rules, and vendor-item qualification. |
| Item Category | Category/subcategory entities, unique codes, Item FKs. | No contracts, API, lifecycle/history UI, permission page, or sourcing/QC defaults. Add maintained category hierarchy and prevent unsafe deactivation when referenced. |
| UOM Master | UOM entity and unique code; optional Item FK. | No contracts/API/UI; Item writes do not resolve the FK. Define decimal precision, base/alternate conversion policy, canonicalize all new transaction lines, and retain immutable UOM snapshots. |
| Tax/GST Settings | Vendor/customer GST data and Item `HsnSacCode`/`GstPercentage`. | No tax master, jurisdiction/place-of-supply rules, effective dates, CGST/SGST/IGST split, cess, exemption/reverse-charge treatment, rounding, tax snapshots, or PO calculation service. A configurable effective-dated tax contract is required before commercial comparison/PO. |
| Warehouse Master | Warehouse identity/type, department, responsible employee, and default receiving/accepted/QC hold/rejected/repairable/scrap location identifiers; CRUD/lifecycle/history APIs. | Default location fields are unverified GUIDs rather than modeled navigations/FKs. Add referential and same-warehouse/condition validation, record scope, and receiving/posting configuration. |
| Rack/Bin/Location Master | RackBin is unique within warehouse and carries zone, location type, condition, capacity, barcode, status, APIs, and usage in stock checks/reservations. | Decide that RackBin is the canonical Location for REV869 or introduce a separate model deliberately. Recommended: adopt RackBin as Location and validate condition-specific locations. Add capacity/precision and scope rules; avoid parallel location identity. |
| PR | Full REV868 header/line snapshots, amount route, lifecycle, remarks actions, stock check boundary, attachments model, history/audit, API and duplicate guards. | Preserve as-is; tighten identity linkage, remarks rules, record scope, attachment endpoints, and removal of person-specific fallback employee codes. No second PR aggregate. |
| RFQ | Page placeholder and `PendingRFQ` handoff only. | RFQ aggregate/lines, handoff consumption, invited vendors, issue/close/cancel/revise, dates, terms, attachments, vendor acknowledgements, state history, idempotency, API, UI, and numbering. |
| Vendor Quotation | Workflow enum only. | Quotation header/lines, revisions, vendor/RFQ relationship, quantities, lead time, validity, tax/freight/discount, deviations, attachments, sealed/commercial visibility, late-bid controls, immutable submitted versions, API/UI. |
| Comparison | Workflow enum only. | Technical criteria/results, commercial normalization, landed cost, ranking, disqualification, negotiation/best-and-final revision, selection rationale, committee approval, immutable comparison version, award decision, API/UI. |
| PO | Page placeholder and workflow enum only. | PO/lines, award linkage, schedule, terms/tax/value snapshots, approval/version/amendment/issue/cancel history, vendor acknowledgement, attachments, API/UI, and unique award-to-active-PO guard. |
| Material Follow-up | Workflow enum only. | PO schedule milestones, promised/revised dates, expediting notes, delay reason, reminder/escalation, vendor acknowledgement, attachments, owner queue, and history. |
| GRN | Page placeholder and workflow enum only. | Receipt/GRN header/lines, PO schedule linkage, invoice/challan/gate reference, delivered quantities, receiving location, batch/serial/shelf-life capture, over-receipt tolerances, attachments, numbering, API/UI. |
| QC | Workflow enum only; Item has `QcRequired`; warehouse has condition location IDs. | Inspection lot/results, inspector, plan/criteria, sampled quantity, accepted/rejected/hold quantities, reason/defect, reinspection/concession/return-to-vendor decisions, condition transfer, attachments, API/UI, and independent QC permissions. |
| Inventory | Item/Warehouse/RackBin and minimal StockMovement; stock check reads movement sums. | Transactional posting service, immutable posting batch/lines, condition/location transfer, lot/serial/value dimensions, reversal (never destructive edit), idempotency, concurrency, reconciliation, APIs/UI. Only QC-accepted or QC-not-required verified stock may become available. |
| Reservation | REV868 location-level active reservations and histories. | Supply fulfillment and project/department allocation, partial fulfillment/release/expiry/transfer/issue status, reasoned history, accepted-supply linkage, and quantity invariants. Reuse existing rows where they reserve existing stock. |
| Issue | Workflow enum only. | Issue note/lines, reservation/project/department linkage, authorized receiver, warehouse/location, batch/serial, posting linkage, partial issue/cancel/reversal, attachments, API/UI, numbering. |
| Return | Workflow enum only. | Material return header/lines, original issue linkage, condition/inspection, accepted/rejected/scrap disposition, inventory reversal/posting, reason, attachments, API/UI, numbering. |
| Stock Ledger | Page placeholder plus minimal StockMovement. | Immutable chronological ledger with opening/in/out/closing, location/condition/project/reference dimensions, value where authorized, reversal links, filters/export, aging basis, and reconciliation endpoints/UI. |
| Aging | No implementation. | Receipt-layer/lot aging as of a date, FIFO buckets, condition exclusions, slow/non-moving thresholds, valuation-policy dependency, scoped dashboard/export, and reproducible snapshot semantics. |
| Project Consumption | PR/line carries free-text project reference; workflow enum only. | Decide Project master/system of record; consumption posting linked to issue/return, item, project, department/cost centre, quantity/value, date, actor, and ledger. Returns must reduce consumption through linked reversal, not delete it. |

## 3. Non-negotiable reuse and integrity rules

1. Reuse the existing REV868 PR header, lines, snapshots, statuses, approval history, attachments, and correlation evidence. New records hold FKs to them; they do not copy a second PR.
2. Reuse the latest valid stock check and existing active reservations. Rechecking creates a new immutable check snapshot and adjusts reservations only through explicit, historically recorded commands.
3. Consume each existing `PendingRFQ` handoff through an atomic state transition and RFQ-line link. Do not manufacture a new handoff for the same shortage.
4. Enforce no duplicate PR/RFQ/PO/GRN/inventory entry through business keys, filtered unique indexes, idempotency keys, and transactional command handling. API retries return the previously created result.
5. Preserve all prior status, approval, denied-attempt, attachment, stock-check, reservation, and audit history. Amendments create versions; reversals create linked compensating entries. No posted or approved evidence is overwritten or deleted.
6. Quantity changes must conserve the PR line and handoff quantities. Short-close, cancellation, rejection, and return require explicit status/history and cannot silently disappear from totals.
7. Cross-module ownership is strict: Purchase owns sourcing/award/PO/follow-up; Stores owns receipt verification/location/reservation/issue/return/ledger; QC owns inspection disposition. A module invokes an idempotent command rather than editing another module's history.
8. Every mutable aggregate uses optimistic concurrency. Every command carries actor employee, role, organization, department, location scope where relevant, correlation ID, idempotency key, version, timestamp, and mandatory remarks where policy requires.

## 4. Identity, ownership, scope, and permission model

### 4.1 Login and scope rules

- One active human identity must map to exactly one active employee. Add an immutable OIDC subject-to-employee mapping or an `EmployeeId` FK with a unique constraint on `UserAccount`; a display name or free-form login is insufficient.
- Shared `PURCHASE`, `STORES`, `QC`, warehouse, or department accounts are prohibited. Reject shared/department `UserType` values and unmapped, duplicate, inactive, or login-disabled employees at authentication/authorization boundaries.
- Department code determines record scope and work queue. Role code determines allowed actions. Warehouse/location assignments further constrain Stores actions.
- Do not hard-code employee names or employee codes. Remove the fallback person identifiers `SESS-001` and `SESS-002` from approval resolution.
- Department manager/alternate mappings must remain configurable, non-overlapping, effective-dated, active-employee validated, and audited. Missing, duplicate, expired, inactive, self-approving, or ambiguous mappings fail closed.
- Every direct frontend route and every API endpoint must independently require authentication, page/action permission, and record-scope authorization. Hiding a link is not authorization.

### 4.2 Ownership and permission matrix

`D` means the actor's department scope; `L` means assigned warehouse/location scope; `O` means explicitly assigned ownership; `A` means organization-wide only when a privileged role grants it.

| Capability | Creator | Owner | Verifier | Approver | Viewer | Enforcement rule |
| --- | --- | --- | --- | --- | --- | --- |
| View | Own created records within D | O within D/L | Assigned queue within D/L | Assigned approval queue; commercial values only with separate grant | Read-only D/L; A only by explicit privileged role | Apply server-side query predicates and detail-object scope checks. |
| Create | Role action + D/L | May create for assigned process | No implied create | No implied create | No | Creation stamps employee, department, organization, and owner; client-supplied scope is not trusted. |
| Edit restriction | Draft owned/created records only | Draft or designated operational states | Verification fields only in assigned state | Approval decision only; never commercial/quantity content | None | Approved/issued/posted versions are immutable. Amend/revise via command and new version. |
| Verify | No unless separately granted and not creator where segregation requires | No implied right | Assigned verifier in D/L | No implied right | No | Stores physical verification and QC inspection are distinct actions and roles. |
| Approve/reject/revise | Never by creation alone | Never by ownership alone | Never by verification alone | Effective assigned approver only; no self-approval | No | Mandatory remarks for reject/revise/resubmit and denied-attempt audit for every failure. |
| Attach | Upload/replace in draft with grant | Upload in permitted pre-final state | Add verification evidence; no replacement of prior evidence | Add decision evidence; no replacement | Download only with grant | Scan/type/size policy; version metadata; finalized attachment is immutable; replacement creates a version/tombstone history. |
| Export/print/download | Separate permission, scoped rows only | Separate permission, scoped rows only | Separate permission, scoped queue only | Separate permission; commercial visibility separately enforced | Separate permission | Audit criteria, row count, actor, timestamp, and classification; prevent unscoped bulk export. |
| Transfer ownership | No by default | Only with explicit transfer grant | No | No | No | Reason, old/new employee, scope validation, and history required. |
| Direct URL/API | Same as UI | Same as UI | Same as UI | Same as UI | Same as UI | Return 401 unauthenticated, 403 unauthorized/out-of-scope without leaking sensitive existence, and audit denied writes. |

Recommended operational roles are `REQUESTER`, `PURCHASE_BUYER`, `PURCHASE_VERIFIER`, `PURCHASE_APPROVER`, `STORES_RECEIVER`, `STORES_VERIFIER`, `QC_INSPECTOR`, `STORES_ISSUER`, `LEDGER_VIEWER`, and narrowly controlled administrators. Role assignment alone does not expand department/location scope.

## 5. Approval policy

The approval route is calculated from the immutable transaction value snapshot in INR:

| Inclusive amount | Required approver resolution |
| --- | --- |
| `₹0` through `₹50,000` | Effective Department Manager mapping for the requesting department |
| `₹50,001` through `₹5,00,000` | Active employee resolved through role `TECHNICAL_DIRECTOR` and effective configuration |
| Above `₹5,00,000` | Active employee resolved through role `MANAGING_DIRECTOR` and effective configuration |

Boundary tests must include exactly `0`, `50,000`, `50,001`, `500,000`, and `500,001`. The business must decide whether taxes, freight, discounts, amendments, and currency conversion are included in approval value before REV869C; recommended basis is final landed PO value in INR with tax and charges, using a stored exchange-rate snapshot where applicable.

No actor may approve a transaction they created, own as requester, verified where segregation requires independence, or submitted on another employee's behalf. Reject, request-revision, and resubmit require non-blank remarks. Approval attempts must record success or denial, resolved route, candidate/selected approver, actor employee and role, from/to status, value snapshot, reason, correlation, IP/client context where available, and timestamp. A denied attempt never mutates business state.

PO amendments that increase the approved value or materially change vendor/item/quantity/tax/schedule re-enter approval using the amended snapshot; the previous issued version stays immutable.

## 6. Proposed solution components

### 6.1 Database entities and relationships

The names below are planning names, to be finalized in design review.

- Identity/configuration: `EmployeeIdentityMapping` (unique employee and OIDC subject), `EmployeeOperationalScope` (department/warehouse/location, effective dates), effective `TaxCode`/`TaxRate`, `UomConversion` only if conversion is approved, and validated warehouse condition-location mappings.
- Sourcing: `RequestForQuotation`, `RfqLine` (one or more handoff links), `RfqHandoffAllocation`, `RfqInvitedVendor`, `RfqStatusHistory`, and `RfqAttachment`.
- Quotation/comparison: `VendorQuotation`, `VendorQuotationRevision`, `VendorQuotationLine`, `QuotationChargeTaxSnapshot`, `QuotationAttachment`, `TechnicalCriterion`, `TechnicalEvaluation`, `CommercialComparison`, `CommercialComparisonLine`, `ComparisonDecision`, and their immutable histories.
- PO/follow-up: `PurchaseOrder`, `PurchaseOrderVersion`, `PurchaseOrderLine`, `PurchaseOrderSchedule`, `PurchaseOrderTaxSnapshot`, `PurchaseOrderApprovalHistory`, `PurchaseOrderAttachment`, `VendorAcknowledgement`, and `MaterialFollowUpEvent`.
- Receipt/QC: `GoodsReceipt`, `GoodsReceiptLine`, `ReceiptVerification`, `ReceiptAttachment`, `InspectionLot`, `InspectionResult`, `InspectionDisposition`, and `InspectionAttachment`.
- Inventory: extend or replace minimal posting semantics with `InventoryPostingBatch` and immutable `InventoryPostingLine` linked to `StockMovement`; add posting/reversal key, condition, lot/batch/serial, unit/value snapshot, source line, and project/department dimensions. Preserve existing movement rows.
- Fulfillment: extend `StockReservation` through history/allocation entities; add `MaterialIssue`, `MaterialIssueLine`, `MaterialReturn`, `MaterialReturnLine`, `ProjectConsumptionEntry`, and linked reversals.
- Cross-cutting: per-aggregate number sequences, attachments, status histories, approval histories, notification outbox, idempotency command records, and audit records.

Required relationships and uniqueness include handoff-to-RFQ allocation not exceeding handoff quantity; unique RFQ number; unique vendor quotation revision per RFQ/vendor; one active award per comparison line; one active PO lineage per award; unique GRN number and unique receipt idempotency/source combination; one posting per accepted receipt disposition; unique issue/return numbers; and unique external/idempotency keys. FKs use restrict for posted/history evidence; cascades are limited to never-finalized owned draft children.

### 6.2 Backend services

- `IdentityEmployeeResolver` and `RecordScopeAuthorizer` resolve the active employee, role, department, and locations on every request.
- `ApprovalRouteResolver` implements amount bands, effective-dated mappings, segregation, no-self-approval, and fail-closed ambiguity checks.
- `NumberingService` allocates concurrency-safe fiscal-year numbers without gaps being treated as evidence loss.
- `HandoffConsumptionService`, `RfqWorkflowService`, `QuotationSubmissionService`, `ComparisonService`, `PurchaseOrderWorkflowService`, and `FollowUpService` own Purchase transitions.
- `ReceiptService`, `PhysicalVerificationService`, `QcDispositionService`, `InventoryPostingService`, `ReservationFulfillmentService`, `MaterialIssueService`, `MaterialReturnService`, `StockLedgerQueryService`, `AgingService`, and `ProjectConsumptionService` own Stores/QC transitions.
- `TaxCalculationService` produces effective-dated, rounded, immutable commercial snapshots; `QuantityReconciliationService` enforces conservation.
- `AttachmentService` validates metadata/content policy and immutable versions; `NotificationOutboxService` delivers retry-safe notifications after transaction commit.
- Every command service owns transaction, state-machine validation, optimistic concurrency, idempotency, history, and audit as one atomic unit.

### 6.3 API surface

Use `/api/v1` and command endpoints; never expose a generic status-update endpoint.

- Masters/config: `GET/POST/PUT /masters/item-categories`, `/masters/uoms`, `/settings/taxes`, warehouse condition-location configuration, vendor qualification, identity mapping and operational scope administration.
- Handoffs/RFQ: `GET /purchase/handoffs`, `POST /purchase/handoffs/{id}/accept`, CRUD draft RFQ, `issue`, `revise`, `close`, `cancel`, invitation and attachment endpoints.
- Quotations/comparison: capture draft/revision, submit/withdraw where allowed, technical evaluation, commercial comparison generation, decision, submit/approve/reject/revise, and award.
- PO/follow-up: PO draft/detail/version, submit/approve/reject/revise/issue/amend/cancel, acknowledgement, schedule, and follow-up-event endpoints.
- GRN/QC: receipt draft/detail, physical-verify, submit, cancel/reverse, inspection lot/detail, accept/reject/hold/reinspect/concession/return-to-vendor commands.
- Inventory/fulfillment: post/reverse receipt, reservations/release/allocate, issues, returns, ledger, reconciliation, aging, and project-consumption query/export endpoints.
- Dashboards: `GET /work-queues/*` returns only server-scoped pending counts/rows; notification read/acknowledge endpoints do not grant access to the referenced record.

All list/detail/export/attachment endpoints require page action plus object scope. All writes require `Version` and `Idempotency-Key`; sensitive comparison and cost fields require `CanViewCommercialValues` independently of ordinary view.

### 6.4 Frontend pages and actions

There is currently no frontend implementation. REV869 must supply authenticated route guards and server-authorized pages for:

- Masters: Vendor qualification, Item Category/Subcategory, UOM, Tax/GST, Item, Warehouse, and Rack/Bin/condition configuration.
- Purchase: PendingRFQ handoff inbox, RFQ list/detail/editor/invitations, quotation capture/version view, technical evaluation, commercial comparison, award approval, PO list/detail/version/amendment, and material follow-up dashboard.
- Stores/QC: receiving/GRN, physical verification, QC queue/result/disposition, inventory-posting exceptions, reservations, issue, return, stock ledger, aging, and reconciliation.
- Traceability: one read-only timeline from PR line through stock check/reservation/handoff/RFQ/quotation/comparison/PO/GRN/QC/posting/issue/return/consumption.

Each page provides only permitted actions, but the API remains authoritative. Required UI behavior includes accessible forms, clear amount/quantity units, India currency formatting, timezone-aware timestamps, mandatory-remarks validation, attachment progress/errors, optimistic-concurrency recovery, idempotent double-click protection, empty/error/loading states, partial delivery/inspection flows, and printable/exportable outputs only with grants.

### 6.5 Numbering rules

Use immutable, organization- and financial-year-scoped numbers allocated transactionally: `PR` (existing), `PHO` (existing handoff), `RFQ`, `VQ` (internal quotation receipt/version identity), `CMP`, `PO`, `FU` event if required, `GRN`, `QC`, `IP` posting batch, `RES` (existing reservation convention), `MI` issue, `MR` return, and `PC` consumption batch. Proposed format is `{PREFIX}/{ORG}/{FY}/{SEQUENCE}` with fixed-width sequence; final printable format is a blocking business decision. External vendor quotation, invoice, challan, and gate numbers are stored separately and are never substituted for internal identity.

Numbers are unique per organization/FY/prefix, never reused, and remain visible on cancelled/reversed records. Amendments/revisions use a version suffix or version column without issuing a new lineage number.

### 6.6 Notifications and pending dashboards

Use a transactional outbox for assignment, approaching/overdue RFQ close, missing quotation, approval pending, PO issue/acknowledgement, delivery due/late, GRN awaiting verification, QC pending/held/rejected, posting failure, reservation ready, issue pending, return disposition, and reconciliation exceptions. Delivery retries must not duplicate notifications.

Dashboards derive queues from current authoritative state plus employee role and department/location scope. They show aging/SLA, owner, next action, and exception reason; counts and record results must use the same server-side scope predicate. Email/deep links are conveniences and must reauthorize on open.

### 6.7 Audit and history requirements

- Immutable histories for every state transition, approval, denial, owner transfer, version/amendment, tax recalculation, quantity adjustment, attachment version, posting, reversal, export, and privileged scope change.
- Store entity/line identity, business number/version, previous/new state, before/after material fields, actor employee/login/role, department/location scope, effective approver resolution, remarks/reason, UTC timestamp, correlation and idempotency keys.
- Preserve the full PR-to-consumption chain. A trace query must prove every quantity and state transition without relying on mutable display text.
- Audit denied write attempts including direct-URL/API, self-approval, out-of-scope, stale-version, duplicate, and invalid-state attempts without storing secrets or attachment payloads.
- Posted inventory histories and stock movements are append-only. Corrections are linked reversals and reposts.

### 6.8 Attachment requirements

Required evidence by stage: RFQ specification/terms; quotation signed offer and technical/commercial schedules; comparison rationale/committee evidence; PO signed/issued version and amendment; follow-up correspondence where material; GRN challan/invoice/gate evidence; physical verification evidence; QC certificate/test report/photos; issue acknowledgement; return evidence and disposition.

Policy must define allowed MIME types/extensions, maximum count/size, malware scanning, checksum, encryption/storage provider, retention/legal hold, classification, and download authorization. Metadata includes owner entity/version, storage key, original/safe filename, MIME/size/hash, uploader employee, timestamp, active/superseded state, and reason. Finalized evidence cannot be replaced in place.

## 7. Stock quantities and reconciliation

All quantities use the canonical item UOM and approved decimal precision. Alternate-UOM input, if allowed, stores both entered and canonical quantities with the conversion snapshot.

| Measure | Definition |
| --- | --- |
| Current | Physical book balance at item/warehouse/location/condition: cumulative posted quantity-in minus quantity-out, including linked returns and reversals. Never derive from draft GRN. |
| Reserved | Sum of active, unexpired reservation balance: reserved minus released/cancelled/issued allocation, at the same scope. |
| Available | Accepted usable current stock minus active reserved stock. Rejected, hold, scrap, and unposted quantities are excluded. |
| In-transit | Open issued PO schedule quantity: ordered minus cancelled, GRN-received, and approved short-close quantity. It is informational and not available stock. |
| Accepted | Physically verified receipt quantity with final QC Accepted disposition, or QC-not-required receipt approved by policy, not yet reversed. Posting transfers this into accepted current stock. |
| Rejected/Hold | Received quantity currently in Rejected or Hold condition. It remains physically accountable but is excluded from available stock and project consumption. |
| Issued | Cumulative posted material-issue quantity out, net only through separately linked issue reversals/returns; original issue evidence is unchanged. |
| Returned | Quantity physically returned against an issue and accepted into a defined condition. It increases current/available only after verification/QC and posting. |
| Project consumption | Posted issue quantity attributed to a project/department/cost centre minus posted accepted returns/reversals attributed to the same original issue and project. |

Core formulas at the same item/warehouse/location/condition/as-of boundary are:

`Current = Opening + PostedReceipts + PostedAcceptedReturns + PostedTransfersIn + PositiveAdjustments - PostedIssues - PostedTransfersOut - ReturnToVendor - NegativeAdjustments +/- Reversals`

`Available = max(0, AcceptedUsableCurrent - ActiveReserved)`

`InTransit = IssuedPoQuantity - CancelledPoQuantity - ReceivedQuantity - ApprovedShortCloseQuantity`

`ReservationBalance = ReservedQuantity - ReleasedQuantity - CancelledQuantity - IssuedAgainstReservation`

`ProjectConsumption = PostedIssuesToProject - PostedAcceptedReturnsFromProject +/- LinkedConsumptionReversals`

End-to-end reconciliation per PR line/handoff is:

`Requested = ExistingStockReserved + PurchaseHandoff + CancelledOrApprovedShortClosed`

`PurchaseHandoff = Ordered + UnorderedOpen + CancelledOrShortClosedBeforeOrder`

`Ordered = InTransit + Received + CancelledOrShortClosedAfterOrder`

`Received = Accepted + Rejected + Hold + ReturnedToVendor + ReceiptVariancePendingResolution`

`AcceptedPosted = AvailableAccepted + ReservedAccepted + IssuedAccepted + TransfersOutNet`

No negative quantity, over-reservation, over-order, over-receipt, over-issue, or over-return is allowed without an explicit approved tolerance/exception record. A scheduled reconciliation compares posting lines to stock movement, reservation, receipt/QC, issue/return, and project-consumption totals and raises exceptions; it never auto-edits historical evidence.

## 8. Phased implementation plan

Percentages are estimated contributions to completion of the defined REV869 Purchase + Stores scope, not the entire ERP. They total 100% and count a phase only after its acceptance evidence passes.

### REV869A — Masters, identity, contracts, and permissions (12%)

- **Exact scope:** Close employee-linked login, department/location scope, manager mapping, Item Category, UOM, Tax/GST, vendor qualification, and RackBin-as-Location contracts. Validate warehouse condition locations. Remove person-specific approval fallback. Define state, quantity/UOM, tax, numbering, ownership, attachment, and permission contracts.
- **Dependencies:** Management approval of identity mapping, RackBin as Location, canonical UOM/conversion, Project/Customer dependency, tax basis, approval-value basis, numbering, and scope policy.
- **Migration boundary:** One additive REV869A migration after model review; employee identity/scope/config and master/configuration constraints only. No RFQ/PO/GRN transaction tables and no rewrite of REV868 evidence. Backfill must be separately reviewed and fail on ambiguity.
- **Backend work:** Master/config contracts/services/APIs; identity resolver; record-scope authorizer; approval resolver corrections; warehouse-condition validation; action permissions and audit hooks.
- **Frontend work:** Category/UOM/Tax, vendor qualification, warehouse/location configuration, identity/scope administration, and permission administration pages. If frontend project selection is not approved, backend can be accepted first but the phase remains partially complete.
- **Permissions:** New master/config page keys and actions; only identity/permission administrators may map identities/scopes; four-eyes approval for privileged changes; all direct APIs protected.
- **Audit evidence:** Before/after configuration, effective dates, actor employee/role, denied shared/unmapped identity, scope changes, tax/UOM changes, approval-resolution failures, and removal of hard-coded fallback behavior.
- **Acceptance tests:** Unique employee/subject constraints; reject shared/inactive/unmapped identities; effective/non-overlapping manager maps; boundary approval routes; self-approval denial; UOM/category/tax/location referential rules; action and D/L scope 401/403 matrix; no changes to REV868 row/history counts.
- **Rollback strategy:** Down migration only while new config is unused; export/verify configuration first. Once later transaction phases reference it, roll forward. Rollback must not reactivate ambiguous identities or restore person-specific fallbacks.
- **Estimated ERP completion contribution:** 12% of REV869; unlocks safe transaction work but creates no RFQ/PO/GRN flow.

### REV869B — RFQ and vendor quotations (16%)

- **Exact scope:** Atomically accept existing PendingRFQ handoffs into RFQ lines, invite qualified vendors, issue/revise/close/cancel RFQs, and capture immutable versioned vendor quotations with technical/commercial attachments.
- **Dependencies:** Accepted REV869A identity, scope, UOM, vendor qualification, tax, attachment, numbering, and state contracts; existing REV868 handoffs.
- **Migration boundary:** One additive sourcing migration for RFQ, handoff allocation/history, invited vendors, quotation revisions/lines/tax/charge snapshots, attachments, histories, keys, FKs, and filtered uniqueness. No PO/receipt/posting tables.
- **Backend work:** Handoff consumption, RFQ workflow, vendor invitation, quotation submission/version services, APIs, idempotency, concurrency, notification outbox.
- **Frontend work:** PendingRFQ inbox, RFQ editor/detail/issue view, vendor invitation matrix, quotation capture/version/difference view, attachments, pending/late dashboards.
- **Permissions:** Buyer create/update/issue; verifier checks; vendors only through a separately approved portal boundary; technical/commercial visibility separated; department scope inherited from PR and ownership.
- **Audit evidence:** Handoff before/after status, quantity allocations, invited vendor decisions, issue/close/revise actions, quotation version/hash, late/withdrawn decisions, attachment versions, denied unqualified/out-of-scope/duplicate attempts.
- **Acceptance tests:** Existing handoff reused; one active consumption path; allocation conservation; retry returns same RFQ; invalid vendor/state/stale version fail; sealed commercial access protected; mandatory remarks; 401/403 and D scope; no PR/history mutation.
- **Rollback strategy:** Down only when all RFQs remain unissued drafts and quotations absent. After issue/submission, retain evidence and roll forward; feature-disable new commands without dropping tables.
- **Estimated ERP completion contribution:** 16% cumulative 28%.

### REV869C — Technical/commercial comparison and PO (18%)

- **Exact scope:** Versioned technical evaluation, normalized landed-cost comparison, selection rationale, approval/award, PO creation from award, amount approval, issue/acknowledge/amend/cancel, terms/tax/schedules and attachments.
- **Dependencies:** Accepted REV869B quotations; approved comparison criteria, tax/rounding/currency basis, commercial confidentiality, PO terms, tolerances, and approval-value policy.
- **Migration boundary:** One additive award/PO migration with evaluation/comparison versions, decision/award, PO lineage/versions/lines/schedules/tax snapshots/approvals/attachments and unique award-to-active-PO guards. No GRN/posting tables.
- **Backend work:** Evaluation, comparison, award and PO workflow services; landed-cost/tax calculator; approval resolver; amendment/version service; APIs and notification outbox.
- **Frontend work:** Technical scoring, blind/commercial comparison, rationale and approval views, PO draft/detail/print/version/amendment, acknowledgement, approval queues.
- **Permissions:** Technical evaluator cannot see commercial values unless granted; buyer cannot approve own award/PO; approver resolved by amount; print/export/download separately granted and audited.
- **Audit evidence:** Input quotation versions, criteria/scores, normalized cost calculation, exclusions, ranking override rationale, route resolution, every approval/denial, issued PO hash/version, amendments and acknowledgements.
- **Acceptance tests:** Deterministic comparison; only submitted valid bids; tie/disqualification/rounding cases; approval boundary values; no self-approval; reject/revise/resubmit remarks; one active award/PO lineage; material amendment reapproval; stale/idempotent/401/403 tests.
- **Rollback strategy:** Draft-only records may be removed by Down before issue. Issued/approved commercial evidence is retained; feature-disable and roll forward. Never recreate a prior PO number or overwrite an issued version.
- **Estimated ERP completion contribution:** 18% cumulative 46%.

### REV869D — Material follow-up, GRN, physical verification, and QC (18%)

- **Exact scope:** PO schedule follow-up and escalation; receipt/GRN capture; Stores physical count/document/location verification; batch/serial/shelf-life capture; QC inspection and Accepted/Rejected/Hold/reinspection/return-to-vendor disposition. Do not yet make stock available except through the posting boundary prepared for REV869E.
- **Dependencies:** Issued PO/schedules; warehouse receiving/condition locations; item tracking/QC rules; receipt tolerances; QC plans; attachment/storage policy; segregation assignments.
- **Migration boundary:** One additive receiving/QC migration for follow-up events, GRN/lines/verification, inspection lots/results/dispositions, attachments/histories, and unique receipt/idempotency guards. Inventory posting remains a separate REV869E migration.
- **Backend work:** Follow-up, receipt, physical verification and QC services/APIs; over-receipt/tolerance and condition routing; notifications and exception queues.
- **Frontend work:** Delivery schedule/follow-up dashboard, GRN entry, physical verification workbench, label/batch/serial capture, QC queue/results/disposition, hold/rejection and exception views.
- **Permissions:** Buyer follow-up; Stores receiver creates GRN; independent Stores verifier confirms physical receipt; QC inspector controls QC only; acceptance override/concession requires explicit privileged approval. L scope enforced.
- **Audit evidence:** Promised-date changes, reminders, receipt source docs/quantities, physical variance, location, lot/serial, QC criteria/results, accepted/rejected/hold quantities, reason, evidence attachments, denied over-receipt and segregation failures.
- **Acceptance tests:** Partial/multiple receipt; duplicate challan/idempotency; over/wrong-PO/wrong-location rejection; batch/serial uniqueness; physical variance; QC-required stock cannot bypass QC; quantity partition exactly equals verified receipt; rejected/hold excluded; stale/401/403/L-scope cases.
- **Rollback strategy:** Draft, unverified receipts only before downstream reference. Verified/QC evidence is retained and corrected by explicit reversal/disposition. Feature-disable intake and roll forward after any finalized receipt.
- **Estimated ERP completion contribution:** 18% cumulative 64%.

### REV869E — Inventory posting, reservation fulfillment, issue, return, ledger, aging, and project consumption (23%)

- **Exact scope:** Immutable receipt/disposition posting; accepted-stock availability; project/department reservation allocation; material issue and accepted return; stock ledger/reversals; reconciliation; aging; project consumption and linked reversal.
- **Dependencies:** Accepted REV869D quantities/dispositions; final valuation and project system-of-record decisions; canonical UOM; lot/serial/shelf-life; opening-balance treatment; issue/return authorization and reservation policy.
- **Migration boundary:** One additive inventory/fulfillment migration. Extend existing StockMovement without dropping/reinterpreting rows; add posting batches/lines/reversal links, reservation allocations, issues/returns, consumption, reconciliation exceptions, keys/FKs/indexes. Any data backfill is separately scripted/reviewed and never inferred ambiguously.
- **Backend work:** Posting, reservation fulfillment, issue, return, ledger, aging, consumption and reconciliation services/APIs; transactional idempotency; condition/location transfers; outbox.
- **Frontend work:** Posting exceptions, reservation/project allocation, issue/acknowledgement, return/disposition, ledger drill-down/export, aging dashboard, consumption trace, reconciliation dashboard.
- **Permissions:** Posting service identity plus authorized Stores operator; issuer restricted by L and reservation/project D; receiver cannot self-authorize exceptions; ledger quantity vs value visibility separated; export scoped/audited.
- **Audit evidence:** Every debit/credit quantity, condition/location/project, source/reversal, reservation state, receiver/returner acknowledgement, issue/return evidence, aging criteria/as-of, reconciliation execution and exception resolution.
- **Acceptance tests:** Formula and quantity conservation; only accepted posted stock available; double-post retry; balanced transfer/reversal; no negative/over-reserved/over-issued/over-return; partial fulfillment; project/D/L scope; lot/serial trace; return reduces linked consumption; ledger reproducibility; aging buckets; legacy StockMovement preservation; concurrency/security tests.
- **Rollback strategy:** Before posting, Down may remove unused schema. After the first posting, migration is forward-only operationally: disable commands, reconcile, and correct with reversals. Never drop or rewrite ledger/consumption evidence.
- **Estimated ERP completion contribution:** 23% cumulative 87%.

### REV869F — Frontend completion and end-to-end acceptance (13%)

- **Exact scope:** Complete all operational pages/actions, consistent navigation and queue dashboards, accessibility/responsiveness, printable/exportable artifacts, full trace timeline, negative security UX, and isolated end-to-end acceptance across the required flow.
- **Dependencies:** Stable accepted A-E APIs/contracts; chosen frontend architecture/deployment/auth library; test identities and isolated acceptance database; real OIDC environment for final production-readiness evidence.
- **Migration boundary:** No business-schema migration expected. Only a separately reviewed corrective migration if acceptance proves an unavoidable model defect; never bundle it silently into UI work.
- **Backend work:** UI-oriented lookup/aggregation endpoints, pagination/filter/export hardening, trace endpoint, observability, performance limits, and defect correction without contract drift.
- **Frontend work:** All master, Purchase, Stores, QC, ledger, aging, consumption, dashboard, attachment, history, and trace pages; route guards; role/scope-aware actions; concurrency/idempotency UX; accessibility and browser validation.
- **Permissions:** UI/API parity matrix for every page/action; direct URL tests; commercial/attachment/export controls; D/L/O scope; no shared identities.
- **Audit evidence:** End-to-end correlation from accepted PR through consumption/return; action and denial histories; export/attachment histories; OIDC subject-to-employee-role-scope evidence when the real provider test is authorized.
- **Acceptance tests:** Full happy path plus partial stock/shortage, multiple bids, approval bands, revisions, partial PO/GRN, QC split, posting, reservation, issue, return, ledger, aging and consumption; duplicate retries; concurrency; rollback rehearsal; accessibility; performance; direct URL/API 401/403; real OIDC token mapping.
- **Rollback strategy:** Roll back frontend deployment independently to the prior compatible build; keep additive backend APIs during compatibility window. Business evidence remains. Database rollback follows the owning A-E phase policy only.
- **Estimated ERP completion contribution:** 13% cumulative 100% of defined REV869, subject to real OIDC acceptance.

## 9. Blocking decisions

1. Identity system of record and exact unique OIDC-subject-to-employee mapping; treatment of service accounts separately from humans.
2. Formal adoption of RackBin as Location (recommended) versus a new Location entity.
3. Canonical UOM precision and whether alternate conversions are in REV869.
4. Tax/GST engine scope, place-of-supply, rounding, currency/exchange rate, and whether approval uses pre-tax or landed INR value (landed INR recommended).
5. Project master/system of record and when Customer is required; mapping of department, cost centre, project, service, and work order.
6. Vendor qualification, minimum bids, single-source/emergency purchase, sealed-bid, negotiation, technical scoring, and award-override policy.
7. PO/receipt tolerances, short close, extra quantity, free-of-cost items, return-to-vendor and QC concession authority.
8. Inventory valuation method, opening balances, negative-stock prohibition, batch/serial/shelf-life, aging bucket definitions, and reconciliation ownership.
9. Number formats, organization/fiscal-year boundary, attachment storage/security/retention, notification channels/SLAs, and frontend technology/deployment.

## 10. Required corrections before transaction expansion

- Link every human login uniquely to an active employee; reject shared and unmapped identities.
- Remove person-specific approval fallback employee codes and fail closed on unresolved/ambiguous approvers.
- Implement server-side department/location/owner record scope; current page permission checks alone are insufficient.
- Canonicalize Item UOM to the existing UOM FK and retain transaction snapshots.
- Formalize RackBin/Location and enforce same-warehouse condition-location FKs for warehouse defaults.
- Add effective-dated Tax/GST configuration rather than relying only on mutable Item GST percentage.
- Add attachment APIs/policy and immutable versioning; current metadata models do not complete file handling.
- Strengthen StockMovement into an idempotent, append-only, reversible posting model without damaging existing rows.

## 11. Improvements

- Use explicit aggregate state machines, command handlers, transactional outbox, consistent problem details, correlation/idempotency middleware, and reusable scope predicates.
- Add read models for work queues and the end-to-end trace rather than issuing cross-module mutable joins in UI code.
- Add database check constraints for non-negative quantities, accepted/rejected/hold partitions, date windows, and status/value invariants in addition to service validation.
- Add contract tests, property-based quantity reconciliation, authorization matrix tests, concurrency/retry tests, and performance indexes based on scoped queues.
- Treat reporting/export as governed operations with as-of semantics, field-level commercial security, and audit.

## 12. Risk register

### Data risks

- Duplicate handoff consumption or retries could create multiple RFQs/POs/GRNs/postings without filtered uniqueness and idempotency.
- UOM text/FK divergence, rounding, tax changes, and project free text can make quantities/value irreconcilable.
- Mutable warehouse default location GUIDs can point across warehouses or to the wrong material condition.
- Partial receipt/QC/return and backdated posting can corrupt availability/aging unless all calculations share as-of and condition rules.
- Migration backfills may invent employee, location, UOM, tax, or project mappings; ambiguous data must stop migration and require reviewed correction.

### Security risks

- Current login claims are not database-enforced as one employee; shared/arbitrary accounts could defeat accountability.
- Page permission without object scope permits horizontal data access; frontend hiding cannot mitigate direct URL/API access.
- Buyer, verifier, approver, receiver, QC and issuer privilege overlap creates fraud/self-approval risk.
- Commercial values, vendor bids, GST/bank data, attachments, exports, and deep links require field/object-level controls and download auditing.
- Idempotency/correlation inputs, filenames, exports, and notification content must be validated to prevent injection, leakage, or cross-tenant access.

### UI risks

- There is no current frontend foundation, so route/auth framework, component system, accessibility, deployment, and API error conventions remain decisions.
- Complex partial quantities and statuses can cause accidental duplicate actions; use explicit totals, version warnings, disabled-in-flight commands, and confirmation of irreversible actions.
- Permission-dependent screens may drift from API rules; generate/verify a shared page/action matrix and test direct routes.
- Comparison and ledger screens can expose sensitive prices or overwhelm users; field-level permission, filters, drill-down, paging, and export limits are required.

### Production/OIDC blocker

Real OIDC provider and real token testing remains a production-readiness blocker. Offline/source and isolated database tests cannot prove issuer/audience validation, immutable subject-to-employee mapping, role claim normalization, department/location scope, login disablement, token lifetime/revocation, MFA/conditional access, or direct API behavior with real tokens. REV869 cannot be declared production-ready until representative unique employee identities across requester, Purchase, Stores, QC, approver and denial cases pass in the authorized real OIDC environment.

## 13. Smallest safe first implementation phase

Start with a tightly bounded **REV869A contract-and-control slice** only:

1. Enforce unique active employee identity mapping and prohibit shared human accounts.
2. Remove fixed employee-code approval fallbacks; validate effective-dated department/role resolution and no self-approval at all amount boundaries.
3. Adopt RackBin as the canonical Location and validate warehouse condition locations.
4. Canonicalize Item Category and UOM references and expose lookup/maintenance APIs.
5. Add the minimum effective-dated Tax/GST configuration needed to freeze quotation/PO tax snapshots.
6. Add server-side department/location/owner scope plus the action, attachment, export, and commercial-value permission matrix.
7. Prove that all accepted REV868 PR, stock-check, reservation, handoff, approval, status, and audit records remain unchanged.

Do not include RFQ transaction entities in this first slice. This is the smallest safe phase because every later aggregate depends on trustworthy actor identity, scope, UOM/location semantics, tax calculations, approval resolution, and immutable reuse of REV868 evidence.

## 14. Checkpoint acceptance statement

This document is an implementation plan only. No REV869 source implementation, migration, API, frontend page, seed data, database helper, PostgreSQL operation, backup/restore, main database access, `sess_nexaerp` access, REV861 access, or production access is part of this checkpoint.
