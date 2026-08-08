# REV861 Purchase + Stores Full Requirement Inspection and Phase 1 Start

Date: 2026-08-08

## 1. Existing Purchase and Stores Implementation Found

- Purchase flow pages exist: Purchase Request, RFQ, Vendor Quote, Vendor Comparison, Purchase Order, PO Confirmation, Purchase Follow-up, Material Pending, Vendor Performance and Purchase Cost Comparison.
- Store flow pages exist: GRN Entry, Receive GRN, QC Vendor Rating, DC, Final Stock, Stock Ledger, Stock Adjustment, Material Issue to Project, Material Return, Actual BOM Ledger, Inventory Aging and Minimum Stock Alert.
- Recent REV859/REV860 additions already added inventory/purchase dashboards, item image support, barcode scan unknown-item popup, and PO-to-GRN autofill.
- Purchase workflow has strict controls already present for 3-vendor RFQ, comparison readiness, quote history/upsert, PO from final vendor selection, PO duplicate prevention and PO confirmation status gate.
- Store backend has stock movement ledger support for GRN/DC/material issue/return and negative-stock blocking with approval override reference.

## 2. Existing Technology and Database

- Frontend: single installed HTML application, `InventoryERP_Software.html`, with embedded CSS/JavaScript.
- Backend: Node.js server, `server.js`, running on local runtime Node.
- Database/storage: hybrid local JSON state plus PostgreSQL support.
- PostgreSQL objects found include `erp_db_state`, master tables, fast purchase tables, `grn_lines`, `dc_lines`, `stock_movements`, `purchase_flow_status`, `approval_workflow_records`, ledger records and flow-status tables.
- Cloud/AWS readiness exists partially through environment-driven file store settings: S3 bucket/region/endpoint/prefix variables, session/file-store health and retention policy controls.
- Current app is not a .NET SDK project in the installed folder. It is a Node.js + HTML app with PostgreSQL hooks. A full .NET conversion would be a separate architecture phase, not a small patch.

## 3. Missing Components / Upgrade Needed

- Warehouse/Store Master and Rack/Bin Master were still placeholder/dynamic before REV861.
- Material Category, Subcategory, Manufacturer/Make, payment/delivery/freight terms, valuation and numbering settings needed clearer configurable foundation.
- Full backend normalized relational enforcement is incomplete for every transaction; several workflows still rely on JSON/page-record persistence plus PostgreSQL mirrors.
- Gate Entry / Material Inward before GRN is not yet a complete independent transaction.
- GRN still needs strict main-save gate for inward link, invoice/DC evidence, PO balance, excess receipt approval and QC status.
- QC acceptance/rejection/hold must be connected so only accepted quantity becomes usable stock.
- Project reservation, material issue, return, transfer and physical count need stricter quantity validation and ledger reconciliation.
- Accounts three-way matching is present in concept but not yet fully enforced as payment-ready gate.
- Full backend/API action-level authorization for every page/action is not complete; current implementation has strong controls on users/master APIs and approval routes, but many embedded UI actions still need backend transaction APIs.
- AWS production readiness needs deployment package, env file, backup/restore runbook, DPL/IT handover docs and security checklist.

## 4. Files / Tables / Routes That Require Changes

- `C:\Users\User\AppData\Local\SESS NexaERP\app\InventoryERP_Software.html`
- `C:\Users\User\AppData\Local\SESS NexaERP\server\server.js`
- Existing PostgreSQL/backend objects to extend:
  - `items`
  - `vendors`
  - `customers`
  - `purchase_requests`
  - `purchase_orders`
  - `grn_lines`
  - `dc_lines`
  - `stock_movements`
  - `purchase_flow_status`
  - `approval_workflow_records`
  - future normalized tables for material inward, QC inspection, reservation, issue, return, transfer, stock count, vendor return and accounts matching.
- Routes to extend/harden:
  - `/api/fast/items`
  - `/api/fast/vendors`
  - `/api/fast/purchase-requests`
  - `/api/fast/purchase-orders`
  - `/api/fast/grn-lines`
  - `/api/fast/stock-balance`
  - `/api/fast/purchase-flow-status`
  - `/api/fast/portal-pending`
  - future `/api/fast/material-inward`, `/api/fast/qc-inspection`, `/api/fast/stock-reservation`, `/api/fast/material-issue`, `/api/fast/material-return`, `/api/fast/store-transfer`, `/api/fast/accounts-match`.

## 5. Risk Areas

- Current installed project is not a normal source repository; workspace folder has only `work` and `outputs`, and `git status` showed this folder is not a git repository.
- The live app is a very large single HTML file; safe patches must be narrowly inserted and verified.
- Some functionality is visually present but still dynamic/derived, so it must not be called fully complete until persistence, authorization and workflow tests pass.
- Some backend table mirrors exist, but not all transactions are normalized with concurrency-safe database transactions.
- S3/AWS settings exist but production AWS deployment has not been fully tested in this session.
- Demo cannot be claimed complete until browser workflow testing is run with realistic records.

## 6. Phase-Wise Implementation Plan

### Phase 1 - Foundation

- Complete role/permission review.
- Complete Item, Vendor, Customer, Employee master review.
- Add/complete Warehouse and Rack/Bin Master.
- Add material category, make, payment/delivery/freight/purchase terms, valuation and numbering settings.
- Verify configurable approval limits.

### Phase 2 - Purchase Request

- PR live stock check, reservation option, shortage calculation, duplicate PR prevention and PR approval history.

### Phase 3 - Procurement

- RFQ, vendor quote, comparison, purchase approval, PO amendment/reapproval and material follow-up hardening.

### Phase 4 - Material Receipt

- Material inward, strict GRN gate, QC/physical verification, accepted/rejected/hold stock separation.

### Phase 5 - Stores Operation

- Project reservation, material issue, material return, store transfer, stock adjustment, vendor return and immutable stock ledger.

### Phase 6 - Integration and Reporting

- Accounts three-way match, vendor performance, dashboards, reports, notifications and escalations.

### Phase 7 - Testing and Handover

- End-to-end tests, role/security tests, database persistence tests, calculation reconciliation, backup/restore, documentation and handover.

## Phase 1 Started in REV861

Implemented:

- Real Warehouse / Store Master inside `BIN / Rack Master`.
- Real Rack / Bin Location Master with warehouse, rack, bin, location type, item/barcode mapping, capacity and status.
- Configurable Material / Purchase Foundation Settings:
  - Material Category
  - Material Subcategory
  - Manufacturer / Make
  - Payment Terms
  - Delivery Terms
  - Freight / Packing Terms
  - Purchase Terms & Conditions
  - Stock Valuation Setting
  - Numbering Sequence Setting
  - Approval Limit Setting
- Default seed settings:
  - Weighted Average valuation
  - PR/RFQ/PO/GRN numbering patterns
  - Purchase approval levels for Manager, TD and MD
- Duplicate warehouse code/name blocking.
- Duplicate rack/bin per warehouse blocking.

## Verification

- Server syntax check: PASS.
- REV861 browser script syntax check: PASS.
- UI/menu audit: PASS.
  - Missing menu sections: 0
  - Missing literal jumps: 0
  - Hidden menu buttons: 0
  - Placeholder mismatches: 0
- Health endpoint: PASS.
  - Revision: `REV861`
  - URL: `http://127.0.0.1:8783/ERP_TD_FAST_LOGIN.html?cacheBust=REV861`

## Backups Created

- `C:\Users\User\AppData\Local\SESS NexaERP\app\InventoryERP_Software.html.bak-REV860-before-phase1-foundation-2026-08-08T10-06-51-108Z`
- `C:\Users\User\AppData\Local\SESS NexaERP\server\server.js.bak-REV860-before-phase1-foundation-2026-08-08T10-06-51-108Z`

## Next Step

REV862 should continue Phase 1 by hardening master data:

- Vendor Master advanced required fields and duplicate GSTIN/PAN control.
- Item Master advanced fields for QC required, serial/batch/shelf-life tracking, max stock, reorder level, preferred vendor, standard price, attachments and revision history.
- Employee/User mapping check so every employee login maps to a role and role-permission scope.
