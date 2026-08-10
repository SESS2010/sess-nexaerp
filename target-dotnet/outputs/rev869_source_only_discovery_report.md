# REV869 Source-Only Discovery Report

## Scope and method

This is discovery only. It was produced by reading repository source at the REV868C3 accepted baseline. No PostgreSQL connection, helper execution, migration operation, backup operation, main-database access, or REV869 implementation occurred.

## 1. Existing master foundations

| Master | Existing source coverage | Discovery finding |
| --- | --- | --- |
| Vendor | `Domain/Masters/Vendor.cs`; vendor DTOs in `Application/Masters/MasterContracts.cs`; authenticated CRUD, approval actions, histories, page permissions, and audit writes in `Api/Endpoints/MasterEndpoints.cs` | A substantial Vendor master already exists and should be reused. REV869 still needs purchasing-specific vendor qualification/eligibility and sourcing relationships if those rules are required. |
| Item | `Domain/Inventory/Item.cs`; item DTOs in `Application/Inventory/InventoryContracts.cs`; CRUD, lifecycle actions, history, permissions, and audit in `Api/Endpoints/InventoryEndpoints.cs` | Item identity, specifications, tracking flags, reorder thresholds, preferred vendor, price estimate, barcode, and approval state already exist. |
| UOM | `Domain/Masters/MasterSupport.cs:43`; `DbSet<Uom>` and a unique code index in `Infrastructure/Persistence/NexaErpDbContext.cs`; `Item` has both `Uom` text and nullable `UomId` | Schema support exists, but no UOM application contract or API endpoint was found. Item endpoints currently accept and persist the UOM string and do not resolve `UomId`; this is a normalization/integrity gap for REV869. |
| Warehouse | `Domain/Inventory/Warehouse.cs`; warehouse DTOs and CRUD/lifecycle/history endpoints in `Application/Inventory/InventoryContracts.cs` and `Api/Endpoints/InventoryEndpoints.cs` | Warehouse master, responsible employee, department, display location, and default condition-location identifiers already exist. |
| Location | `Domain/Inventory/RackBin.cs`; rack/bin DTOs and endpoints in `Api/Endpoints/InventoryEndpoints.cs`; location-aware stock entities in `Domain/Purchase/PurchaseRequisition.cs` | There is no separate `Location` entity. Rack/bin is the implemented location master, keyed within a warehouse and carrying zone, location type, material condition, capacity, barcode, and state. REV869 must either formally adopt RackBin as Location or introduce a distinct location model and migrate references deliberately. |

All master APIs are backend endpoints. `rg --files` found no `.html`, `.js`, `.jsx`, `.ts`, `.tsx`, or `.css` frontend asset in this repository, so no master UI implementation is present here.

## 2. Existing Purchase and Stores coverage

The implemented Purchase scope is the REV868 purchase-requisition foundation, not a complete procure-to-pay module:

- `Domain/Purchase/PurchaseRequisition.cs` defines PR header/lines, status and approval histories, attachments, stock checks, stock-check lines, reservations and reservation histories, shortage handoffs, approval route settings, department approver mappings, number sequences, and workflow steps.
- `Application/Purchase/PurchaseRequisitionContracts.cs` exposes create/update/action, stock-check location, detail/history, reservation, and handoff contracts.
- `Api/Endpoints/PurchaseRequisitionEndpoints.cs` maps authenticated list/detail/create/update, submit, verify, approve, reject, revision, resubmit, cancel, hold, stock-check, history, reservation-list, and handoff-list routes under `/api/v1/purchase/requisitions`.
- The endpoints use the page keys `purchase.requisitions`, `purchase.requisition-approvals`, `stores.stock-check`, `stores.reservations`, and `purchase.requirement-handoff`; `FoundationSeedData.cs` supplies those page definitions.
- `PurchaseRequisitionSupport.cs` calculates location-level availability, creates reservations for available quantities, creates pending RFQ handoffs for shortages, and writes a Stores stock-check audit event.
- `Inventory/StockMovement.cs` supplies a stock-ledger entity used by availability calculation, but no stock-movement posting API was found.
- `tests/SESS.NexaERP.Tests/Rev868PurchaseRequisitionTests.cs` covers routing boundaries, transitions, quantity reconciliation, duplicate guards, location allocation, audit/history behavior, and security/source invariants.

There is no frontend project or UI asset in the repository. Consequently Purchase and Stores have backend/API coverage only; the seeded route strings are navigation metadata, not implemented pages.

## 3. Missing REV869 components

Source searches found page placeholders for RFQ, Purchase Order, GRN, and stock ledger, but no corresponding domain aggregate, application contract, or endpoint implementation. The material missing scope is:

- Schema: sourcing event/RFQ, invited vendors, vendor quotations and revisions, commercial/technical comparison, selection rationale, purchase order and lines, terms/taxes, approval/version histories, receipts/GRN, inspection/QC disposition, accepted/rejected quantities, stock posting linkage, issues/returns, and explicit handoff consumption/status history.
- API: UOM maintenance; RFQ issue/respond/close; quotation capture; comparison and award; PO draft/approve/issue/amend/cancel; receipt and QC; stock posting/issue/return; handoff acknowledgement and traceability; and UI-oriented lookups.
- Permissions: action-specific gates for every new page and separation of requester, buyer, approver, receiver, inspector, and store issuer. Existing page records alone do not provide new endpoint enforcement.
- Workflow: durable transitions and idempotency from pending handoff through RFQ, award, PO, receipt, QC, stock posting, reservation fulfillment, issue, cancellation, and exception/rework paths.
- Audit: immutable actor/correlation histories and before/after audit events for each new aggregate and every denial, amendment, override, and state transition.
- UI: all master, Purchase, and Stores screens, validation, work queues, approval inboxes, comparison views, receipt/QC flows, and end-to-end status traceability.

These are absence findings from the current source tree, not an instruction to implement them in this checkpoint.

## 4. Unique employee login architecture

The employee model uses a unique `EmployeeCode`, per-employee `LoginEnabled`, and per-employee role assignments. Login activation/deactivation and role assignment endpoints address a single employee code. Claims are read from email/name-identifier/preferred-username and role claims (`ClaimsCurrentUser.cs`), and production startup configures JWT bearer/OIDC authority and audience (`Program.cs:21-27`). No shared department account definition or seeded shared department login was found.

However, `UserAccount` has a unique `LoginId` but no `EmployeeId` foreign key, and the user-creation endpoint accepts an arbitrary login/display name/user type. Therefore the present source does **not** enforce one identity account per employee at the database boundary. REV869 should require an employee-linked unique account (or an immutable OIDC subject-to-employee mapping), reject department/shared account types, and prove that every human workflow actor resolves to exactly one active employee. Real OIDC provider/token testing remains a production-readiness blocker.

## 5. Workflow identity constants

No employee **names** are hard-coded in the Purchase workflow. Runtime histories use login/role claims, manager approval resolves through department-to-employee mappings, and workflow rows carry employee codes or roles rather than names.

The fallback workflow definition in `PurchaseRequisitionEndpointHelpers.cs:113-121` does hard-code employee codes `SESS-002` and `SESS-001` for MD/TD steps. Although these are not names, REV869 should remove person-specific fallback identifiers and resolve approvers exclusively from effective-dated configuration, roles, and employee mappings, failing closed when resolution is missing, duplicate, inactive, self-approving, or ambiguous.

## 6. Integrated Purchase to Stores handoff design

The recommended REV869 flow extends, rather than replaces, REV868:

1. Approved PR line triggers location-aware Stores stock check.
2. Available quantity becomes an idempotent active reservation; shortage becomes one traceable pending purchase handoff.
3. Purchase accepts the handoff into an RFQ without copying or rewriting the PR, stock-check, or reservation evidence.
4. RFQ quotations feed a versioned technical/commercial comparison and approval; the award creates a versioned PO.
5. Stores receives against the PO into the warehouse/rack-bin receiving location; QC-required items move through hold and accepted/rejected disposition.
6. Accepted quantity posts immutable stock movements, closes or updates the purchase handoff, and becomes available to fulfill the originating reservation/issue demand.
7. Every transition carries PR line, handoff, item, warehouse/location, actor, correlation, quantity, and version identifiers; quantity conservation and duplicate-active-artifact constraints fail closed.

Purchase owns sourcing/award/PO state. Stores owns availability, reservation, receipt location, stock movement, and issue/return state. QC owns inspection disposition. Cross-module commands must be explicit and idempotent; modules should not mutate each other's history records.

## 7. Reusable REV868 components

- PR and line snapshots preserve item/UOM/specification and estimated-value context.
- Amount-based approval routes, effective workflow steps, department manager mappings, self-approval denial, and approval/status histories provide the approval foundation.
- `StockAvailabilityCheck` and its location lines preserve on-hand, reserved, available, in-transit, reserved, and shortage evidence.
- `StockReservation` plus filtered uniqueness guards reuse for supply allocation and later fulfillment.
- `PurchaseRequirementHandoff` plus filtered pending-handoff uniqueness is the boundary into sourcing.
- Warehouse/RackBin and `LocationKey` provide the existing Stores location vocabulary.
- `StockMovement` provides the ledger basis; `IAuditWriter`, page permissions, concurrency `Version`, correlation identifiers, and idempotency keys are cross-cutting foundations.

## 8. Proposed implementation phases and acceptance tests

| Phase | Source scope | Minimum acceptance evidence |
| --- | --- | --- |
| 0. Identity and contract gates | Enforce unique employee-linked human identity; formalize RackBin-versus-Location and UOM canonicalization; freeze state/ownership contracts | Duplicate/shared/unmapped identity rejected; inactive employee rejected; UOM and location references canonical; no person-specific workflow fallback |
| 1. Sourcing schema | RFQ, vendor invitation, quotation/version, comparison, award, handoff status history | Keys/FKs/checks and filtered uniqueness; rollback-safe migration; no destructive rewrite of REV868 evidence; duplicate quotation/award/handoff consumption fails |
| 2. Sourcing API and permissions | RFQ through comparison/award with audit and optimistic concurrency | 401/403 matrix; action permissions; stale version and invalid transition fail; invited/active vendor constraints; complete actor/correlation audit |
| 3. PO and approval | PO generation, terms/taxes, approval, issue/amend/cancel | Award-to-PO quantity/value reconciliation; self-approval blocked; amendment history immutable; duplicate issued PO prevented |
| 4. Stores receipt and QC | GRN/receipt, location allocation, QC disposition, accepted stock posting | Over-receipt and wrong PO/location rejected; QC-required stock unavailable before acceptance; rejected stock excluded; balanced immutable movements |
| 5. Reservation fulfillment and exceptions | Link accepted supply to handoff/reservation, issue/return/cancel/short-close | End-to-end quantity conservation; idempotent retries; partial receipt and short-close proven; no orphan active reservation or pending handoff |
| 6. UI | Master and operational work queues, forms, approvals, comparison, receiving/QC, trace view | Role-based UI/API parity; validation/error states; accessibility and concurrency handling; end-to-end Purchase-to-Stores scenario |
| 7. Isolated acceptance | Offline tests, isolated database verification, rollback and security review, then real identity testing | Migration set exactly once; all canonical sections once; negative fail-closed cases; rollback preservation; secret/safety scans; real OIDC token/role/employee mapping tests |

## Discovery conclusion

REV868 provides a reusable PR-to-stock-check/reservation/shortage-handoff boundary. REV869 should begin only after management approves a contract that closes the identity-link, UOM, and location-model gaps. The major implementation beyond that boundary—RFQ, quotation comparison, PO, receipt/QC, stock posting, operational UI, and complete cross-module acceptance—does not yet exist in this repository.
