# REV615 Master Product/Service Fix

Generated: 2026-07-03T14:12:56.526Z

## Applied
- Added real Finished Goods / Product Master entry form, ledger, duplicate product/model guard, edit flow, audit log and master-freeze hooks.
- Product Master now stores rows in `productMasters` and is included in page backup profile data keys.
- Service/Machine Master now blocks duplicate Service Asset No during create/update.
- Service/Machine Master now blocks duplicate Machine Serial No during create/update when serial number is provided.
- Updated visible/backend revision from REV614 to REV615.

## Backups
- `outputs/InventoryERP_Software_before_REV615.html`
- `outputs/server_before_REV615.js`