# A1 intercompany — routes and PO publication

Status: **A1 is partial.** Governed routes and publication of a normally issued buyer PO are implemented. GST invoice capture, dispatch, in-transit costing, destination acceptance and reconciliation are not yet implemented. No live database operations have been performed. Item 16 was committed and pushed separately as `060132f`.

## Governing record

The frozen [schema guideline](../SESS_ERP_Stores_Full_Schema_Guideline.docx), section 5.8 and X1/X3/X4/X5/X6/X7, governs. [Answers to Open Questions](../SESS_NexaERP_Answers_To_Open_Questions.md) corrects older drafts. [Pending Work item 19](../SESS_NexaERP_Pending_Work_Specification.md), the [Post Session Plan](../SESS_NexaERP_Post_Session_Plan.md), [Role Catalogue](../SESS_NexaERP_Role_Catalogue.md) and owner instructions require a real sale, the receiving company's normal PR/approval/PO naming the seller as vendor, a GST invoice and complete separate company ledgers. No PO means no movement.

The seller retains ownership and carrying value until destination acceptance. The destination storekeeper must use normal GRN/QC. Physical serial/lot selection and accounting FIFO remain separate: FIFO selects the oldest eligible company/item/ownership-value-pool layers, rather than the receipt that supplied the physically selected serial. Machine delivery remains job-order based and is not reused for component sales.

## Implemented behavior

A route binds distinct active companies, approved warehouses, company sites, effective GST registrations and the shared vendor/customer identities with approved company relationships. Accounts Manager proposes a definition; Technical Director or Managing Director approves/rejects it. The proposer cannot approve their own proposal. Approved overlapping routes for the same warehouse pair/date are refused. Revocation stops new use while retaining the immutable definition and decisions.

The buyer Purchase Manager publishes an already current, normally approved and issued PO through an approved route. The command verifies the buyer's operational scope, issued PO history, current version, vendor, delivery warehouse and unchanged route master identities. It does not create or approve a PO.

The seller receives an immutable commercial copy: PO number, items, quantities, rates, commercial amounts, tax rates, terms and the participating company identities. Buyer requisitions, quotations, comparisons, approval records and internal PO/line identifiers are not included in the seller's copy. The buyer retains its own PO identifier. The published public line identity supplies the future invoice/dispatch correlation.

Eligibility is separate from historical content. A revised/cancelled PO or revoked/expired route makes an existing publication `REFRESH_REQUIRED`; it does not rewrite it. An exact command replay returns its original committed receipt. New downstream commands must recheck eligibility inside their transaction.

All commands retain their resolved employee/role/assignment, audit and command receipt. SUPPORT assignments to the permitted roles follow the shared operation-specific authority rules: they may prepare/read/publish documents, but cannot approve, reject or revoke. This does not grant Accounts Assistant or another role the rights of Accounts Manager or Purchase Manager.

## Frontend contract

| Endpoint | Operation / permission |
|---|---|
| `GET /api/v1/stores/intercompany/routes/options` | Active master identifiers for route preparation; `stores.intercompany-routes:view` |
| `GET /api/v1/stores/intercompany/routes` and `/{id}` | Company route page/detail, current Version and retained decisions |
| `POST /api/v1/stores/intercompany/routes` | Accounts Manager proposal; `stores.intercompany-routes:create` |
| `POST /api/v1/stores/intercompany/routes/{id}/approve`, `/reject`, `/revoke` | TD/MD decision; matching approve/reject/deactivate permission, current Version and remarks |
| `GET /api/v1/stores/intercompany/purchases/options` | Buyer PM's eligible PO/route pairs, including `routeId`, `purchaseOrderId` and `version`; `purchase.intercompany-orders:issue` |
| `POST /api/v1/stores/intercompany/purchases` | Publish selected PO, remarks and idempotency key; same issue permission |
| `GET /api/v1/stores/intercompany/purchases` and `/{id}` | PM/Accounts Manager commercial page/detail; `purchase.intercompany-orders:view` |

Typed outer responses follow the existing API PascalCase policy. Option payloads retain their JSON keys (`orders`, `routeId`, `purchaseOrderId`, `version`; route master option objects use `id`). Nested JSON is consumed with its returned key names, such as `CommercialOrder.lines`.

Request and response types are in [IntercompanyContracts.cs](../../src/SESS.NexaERP.Application/Stores/IntercompanyContracts.cs). Every new input identifier/version has a permission-compatible GET declaration in the whole-assembly reachability manifest. Existing ordinary PO endpoints retain their company boundary. No intercompany stock or finance posting endpoint is exposed by this phase.

## Installation and expected rows

New migration-history entries:

- `20260915180000_IntercompanyRoutes` (104).
- `20260915190000_IntercompanyPurchasePublication` (105).

Both directions use the PostgreSQL cluster guard, require PostgreSQL 17+, refuse system databases and check structural prerequisites. Neither prohibits a SESS database name. Applied migrations are unchanged.

Installation adds four private evidence tables, two pages and five role-permission rows; **zero business rows**. Definitions, route decisions, publications and publication lines are append-only. Rollback refuses retained business evidence. Runtime access is through the governed functions, with no direct table privileges.

The migration and Installer compile the same versioned ACL SQL. The Installer's general table check excludes these function-controlled tables only when their entry function exists; separate strict checks verify their ownership/private ACL and each function's owner, search path, security mode and intended execution authority. Deliberately granting runtime direct SELECT is detected by status and repaired by provisioning on the disposable witness.

The route workflow retains one definition and two decisions (approval and revocation), with zero stock movements. Publishing one single-line PO retains one publication and one public line; replay does not duplicate it. The publication command creates no stock movements, FIFO consumption/restoration, GRN or bill. The ordinary purchase workflow around that command still creates its normal business rows.

## Verification

Raw TRX/logs are retained under `local-evidence/item19` and are not committed.

| Check | Result |
|---|---|
| Corrected route/publication workflows, migration lifecycle and whole-assembly reachability | Release 5/5, zero failed/skipped, 5m10s (`support-fixed-release.trx`) |
| New migrations on exact-name disposable `sess_nexa_erp`, schema 103→105, both rollbacks, route reapply and private access | Release 1/1, zero failed/skipped, 1m05s (`deployment2-release.trx`) |
| Final full routine Debug, all witness gates off | 846/846, zero failed/skipped, 1459.2351358 seconds (24.3206 minutes), `final-debug/full-suite.trx` |
| Final full routine Release, all witness gates off | 843/843, zero failed/skipped, 1411.9202038 seconds (23.5320 minutes), `final-release/full-suite.trx` |

The exact-name test creates a fresh cluster on a generated local port, initializes schema 103, provisions its principals, then applies the new migrations with `session_user=nexa_erp_migration`, `current_user=nexa_erp_owner` and `is_superuser=off`. It is a disposable schema witness, not a restore or migration of the owner's current database. The private test helper still defaults to `advance_parser` for existing tests; its expected database now follows the provided disposable connection.

The normal purchase witness starts from actual PR/approval/PO operations, uses real page permissions and operational scope for publication, tests stale version/duplicate/payload-change refusal, verifies no additional stock movement and checks seller disclosure plus retained history after revocation. It remains opt-in under `WorkflowWitness`; the routine suite retains its one canonical full purchase workflow.

Review found a defect that the initial full Release pass (843/843, 23.3916 measured minutes) did not cover: the new A1 helper wrongly refused all SUPPORT assignments. A new test reproduced the actual route-options 403 (`support-baseline2.trx`); the corrected test now passes and retains SUPPORT approval refusal. Earlier failed attempts are retained separately: one lacked configuration authority in its role-assignment fixture; another incorrectly declared `advance_parser` as the expected database while creating the named copy. Their existing guards were correct and unchanged. Both final full suites cover the corrected code and completed successfully before commit. Debug and Release builds had zero warnings/errors. All eight optional witness gates were explicitly disabled for these routine builds.

A similar substantive-assignment restriction exists in the previously shipped machine-delivery service. It needs a separate workflow review; this A1 phase does not change that service or its applied migration.

## Pending owner decision: destination acceptance

The owner has been asked whether the ownership/value transfer happens at finalized GRN or only at QC acceptance. No answer has arrived. The existing GRN service finalizes stock and creates buyer-owned FIFO layers in one Serializable transaction, so this is an accounting transition, not a label.

| Event | If acceptance means finalized GRN | If acceptance means QC acceptance |
|---|---|---|
| Seller dispatch | Reserve source layers; seller retains transit value | Same |
| Buyer finalized GRN | Seller consumption and buyer-owned QC-hold value become effective | Buyer records custody, but uninspected goods remain seller-owned; ordinary owned FIFO creation must be deferred |
| Buyer QC accepts part | Quality disposition; ownership already passed at GRN | Only that accepted quantity transfers ownership/value |
| Buyer QC rejects | Buyer owns rejected goods until its normal return/correction process | Seller still owns rejected goods; physical return closes the matching transit allocation |
| Buyer accepted bill | Existing landed-cost process applies to owned layers | Cost evidence can precede ownership and must not prematurely value seller-owned goods in the buyer ledger |

These are alternatives, not an authorization to choose either. The completed route/publication controls do not depend on the choice.

## Exact continuation

Finish the invoice/dispatch/receiving phase after the acceptance event is settled. Seller invoice evidence must link to the buyer's own normal supplier-invoice and accepted-bill operations; seller commands must not create or approve buyer GRNs/bills. Retain legally applicable e-way evidence. Partial receipts must respect the normal PO remaining quantity; discrepancies must remain visible and block unexplained closure.

Dispatch reservations must reduce availability for later issues while preserving seller transit value. Acceptance consumes the relevant recorded reservations; returns restore/release those recorded allocations. New events must be visible consistently to issue eligibility, FIFO valuation, returns and later landed-cost adjustment. Existing relevant code is concentrated in `VendorBillCostingSql`, `ImmutableLandedCostAdjustmentsSql`, the return/reversed-receipt migrations and `FifoValuationReportSql`. Introduce new versioned migrations/functions rather than edit applied ones.

Preserve separate physical and accounting allocations. Buyer receipt creates its own company-scoped lot/serial/provenance identities linked by the immutable correlation. Existing custody has VEHICLE/SITE accounts, but no implemented transit workflow; stock addresses still require typed warehouse, rack, condition, ownership and provenance. A logistics state must not be invented as a quality condition.

Customer PO is an intake/revision ledger: migration `20260831155638_CorrectCustomerPoIntakeRevisionsAndPrLink` deliberately removed invoice/payment fields still visible in old Designer files. Do not revive that obsolete shape for seller invoice evidence.

Complete both company ledgers, the privileged read-only reconciliation projection, partial/return/concurrency witnesses and report integration; then run both full routine suites with TRX before declaring A1 complete. A2 and later items remain in the owner's specified order.