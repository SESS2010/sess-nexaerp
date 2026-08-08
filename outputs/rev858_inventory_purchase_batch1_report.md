# REV858 Inventory + Purchase Batch 1 Implementation Report

Date: 2026-08-08

## Base

Started from correct latest ERP line:
- REV857 PostgreSQL/.NET-backed ERP base
- Old REV622 browser view was stale and was not used as the upgrade base

## Recovery Completed Before Upgrade

- Restored stable REV857 installed ERP base.
- Found server health timeout cause: PostgreSQL primary DB cache warm-up was running during startup and blocking HTTP response.
- Changed PostgreSQL warm-up to opt-in only using `SESS_NEXA_PG_WARMUP=1`.
- Server health now responds normally.

## Batch 1 Implemented

Added a read-only Inventory + Purchase Control Dashboard layer to the installed ERP.

Dashboard includes:
- Stock item count
- Below-minimum item count
- Negative stock count
- Approximate stock value
- Purchase Request count
- Purchase Order count
- Monthly stock statement: inward, outward, value
- Minimum stock statement
- High-value stock list
- Purchase pipeline: RFQ, vendor quote, PO confirmation, GRN, PO value

Navigation actions added inside the dashboard:
- Open Stock Ledger
- Open Minimum Stock
- Open Purchase Request
- Open Purchase Dashboard

## Safety

This batch is read-only:
- No ERP module removed
- No localStorage data changed
- No approval logic changed
- No purchase workflow validation changed
- No backup/import/export logic changed
- No PostgreSQL save path removed

## Verification

- Server syntax: PASS
- Menu audit revision: `Software REV858`
- Missing menu sections: `0`
- Missing literal tab jumps: `0`
- Hidden menu buttons: `0`
- Placeholder mismatches: `0`
- Live health: PASS
- Live health revision: `REV858`
- Live port: `8783`

Known existing audit note:
- Duplicate menu label exists for `Proforma / Advance PI`; this was already in current ERP structure and was not part of Inventory/Purchase Batch 1.

## Next Batch

Batch 2 should implement:
- Strong Monthly Stock Statement page with opening/inward/outward/closing/value
- Minimum Stock Statement with reorder quantity and purchase action
- PR ageing dashboard
- RFQ to PO to GRN pipeline dashboard
- Top vendor value and delivery performance view
