# REV614 Purchase Workflow Control Fix Report

Installed ERP upgraded:

- `C:\Users\User\AppData\Local\SESS NexaERP\app\InventoryERP_Software.html`
- `C:\Users\User\AppData\Local\SESS NexaERP\server\server.js`

Backups created before upgrade:

- `C:\Users\User\Documents\Codex\2026-07-03\see\outputs\InventoryERP_Software_before_REV614.html`
- `C:\Users\User\Documents\Codex\2026-07-03\see\outputs\server_before_REV614.js`

## Fixed Controls

- RFQ now strictly requires at least 3 unique vendor names.
- Vendor comparison/finalisation now requires the required number of unique vendor offers, minimum 3.
- Manual vendor quote for the same RFQ + PR line + vendor updates the same offer and stores negotiation history.
- Vendor portal quote revision uses the same update/history path.
- Duplicate PO number is blocked.
- PO save requires a final vendor selection/comparison row.
- PO confirmation requires the PO to be Approved, Released, Sent to Vendor, or already Acknowledged.

## Verification

- Server syntax check passed.
- Installed ERP HTML inline-script parse passed.
- Purchase workflow QA passed: 24/24 checks.
- Running server health passed.
- Health revision: `REV614`.
- Live ERP page fetch passed.
- Live page contains `Software REV614` and purchase-control functions.

## Running URL

- `http://127.0.0.1:8783/InventoryERP_Software.html`
