# Customer PO intake register boundary

## Purpose and ownership

`advance.customer_purchase_orders` is an **intake register** for a customer purchase order received by one SESS company. It is **NOT the canonical Sales model**. Its purpose is to identify the shared customer, preserve the customer-supplied order and its immutable revisions, and optionally link Purchase Requisitions raised to fulfil that order.

Offers, offer revisions, contract review, sales invoicing, receipts and payment status do not belong to this register. Those require their own governed Sales and Accounts aggregates. This note supersedes older statements that no Customer PO identity exists, but it does not expand the intake register into those deferred modules.

## Identity, company scope and revisions

- `CustomerId` is mandatory and references the shared customer master. There is no free-text customer identity.
- `CompanyId` is mandatory and comes from the authenticated operational session; clients cannot select it in the payload.
- `CurrentRevisionNumber` identifies the current read model.
- Every accepted create, edit or PO-file replacement appends a `customer_purchase_order_revisions` row containing the complete canonical JSON snapshot.
- Lines are keyed by customer PO, revision and serial number. Earlier revision lines and snapshots are append-only in PostgreSQL.
- Optimistic `Version` remains mandatory for edits and file replacement.
- `purchase_requisitions.CustomerPurchaseOrderId` is nullable. Customer-order PRs use it; stock, tool, consumable and other non-customer PRs leave it null. A composite foreign key binds the linked PO to the PR company.

## Cost rollup

The current database can start a customer-PO rollup through:

1. customer PO intake identity;
2. same-company linked PRs and their estimated totals;
3. Purchase hand-offs from those PR lines;
4. supplier PO lines and ordered values;
5. GRN and QC provenance as Stores services are implemented.

The defensible final actual cost remains the **accepted vendor bill allocation**, not the supplier PO estimate and not FIFO inventory valuation. FIFO answers stock valuation; accepted-bill allocation answers offer-versus-actual commercial costing.

A complete offer-versus-actual report is still blocked by these planned schema/application capabilities:

- canonical offer and immutable offer revision identity;
- frozen Estimated BOM and approved Production BOM linkage to the customer order;
- contract-review and canonical customer-order model linked back to this intake record;
- accepted vendor bill and allocation to supplier PO, receipt, PR/customer order and Actual BOM ancestry;
- fitment/consumption ancestry from the inventory lot or serial through the machine Actual BOM;
- agreed allocation of freight and other landed costs, if those are included in commercial actual cost;
- a reporting projection that preserves both baselines: frozen offer versus actual and approved Production BOM versus operational actual.

Until those exist, the system may report linked PR estimates and downstream ordered values as provisional evidence, but it must not label either as final actual cost.

## Migration row effects

A fresh database gains no business rows: zero customer POs, zero revisions and zero PR links. On an existing database, the correction adds exactly one migration-baseline revision for every existing Customer PO and preserves every existing line as revision 1. It creates no Customer PO or PR. The migration refuses to run if mandatory customer/company mappings are absent or if the removed Accounts fields contain data, preventing silent loss.