# REV859 Inventory + Purchase Batch 2 Upgrade Report

Date: 2026-08-08

Installed ERP upgraded from REV858 to REV859 using the restored REV857/.NET/PostgreSQL line as the base.

## Added in REV859

- Full read-only Monthly Stock Statement panel:
  - Opening
  - Inward
  - Outward
  - Adjustment
  - Closing
  - Value
- Minimum Stock / Reorder Statement:
  - Closing stock
  - Minimum level
  - Shortage
  - Suggested PR quantity
  - Open PR action button
- PR Ageing dashboard:
  - Open PR rows
  - PR date
  - Item/project
  - Quantity
  - Status
  - Age days
- Purchase Pipeline dashboard:
  - PR raised
  - Open PR
  - Approved/released PR
  - RFQ
  - Vendor offers
  - Purchase orders
  - PO confirmations
  - GRN/receive rows
- Top Vendors by PO Value dashboard:
  - Vendor
  - PO count
  - PO value

## Safety

- Existing modules were not removed.
- Existing localStorage/company data logic was not removed.
- Existing approval logic was not removed.
- Existing dashboard logic was not removed.
- Existing backup/import/export logic was not removed.
- Existing PostgreSQL/server logic was not removed.
- New dashboards are read-only, except the reorder button opens the existing Purchase Request screen and shows guidance. It does not auto-save a PR.

## Files Updated

- `C:\Users\User\AppData\Local\SESS NexaERP\app\InventoryERP_Software.html`
- `C:\Users\User\AppData\Local\SESS NexaERP\server\server.js`

## Backups Created

- `C:\Users\User\AppData\Local\SESS NexaERP\app\InventoryERP_Software.html.bak-REV858-before-inventory-purchase-batch2-2026-08-08T09-43-11-692Z`
- `C:\Users\User\AppData\Local\SESS NexaERP\server\server.js.bak-REV858-before-inventory-purchase-batch2-2026-08-08T09-43-11-692Z`

## Verification

- Server syntax check: PASS
- ERP health endpoint: PASS
  - Revision: `REV859`
  - URL: `http://127.0.0.1:8783/ERP_TD_FAST_LOGIN.html?cacheBust=REV859`
- UI/menu mapping audit: PASS
  - Menu buttons: 334
  - Sections: 369
  - Literal tab jumps: 341
  - Dynamic pages: 85
  - Missing menu sections: 0
  - Missing literal jumps: 0
  - Hidden menu buttons: 0
  - Placeholder mismatches: 0

## Existing Note

- Duplicate label still exists for `Proforma / Advance PI`, both pointing to `proformaInvoice`. This was already present and not changed in this inventory/purchase batch.
