# REV613 Store / Stock / BOM Mapping Check

Checked installed ERP:

- `C:\Users\User\AppData\Local\SESS NexaERP\app\InventoryERP_Software.html`
- Running URL: `http://127.0.0.1:8783/InventoryERP_Software.html`

## Result

Store mapping, stock update interlocks, and project BOM update links are OK in the installed REV613 build.

## Passed Checks

- GRN accepted quantity increases stock.
- Rejected GRN quantity does not increase stock.
- DC issued quantity reduces stock.
- Project / Job Order DC updates Actual Project BOM lines.
- Actual Project BOM is audit/cost linkage only and does not double-deduct stock.
- Project material return adds stock back.
- Project material return creates a negative return line in Actual BOM view.
- Material return cannot exceed the remaining issued balance.
- Stock adjustment supports add/reduce signed quantity.
- Stock adjustment is blocked if it would take stock below zero.
- Returnable DC is excluded from issued stock after it is fully closed.
- Spare invoice stock deduction is guarded.
- Invoice linked to existing DC does not deduct stock again.
- Project/machine invoice avoids double deduction when project BOM/DC already issued the item.
- Reserved stock is calculated from open purchase request rows.
- Store material issue PostgreSQL mirror hook exists.
- REV613 final screens are served: Daily Material Movement Register, Material Transfer Note, Inspection Note Before GRN.

## Live Verification

- Health endpoint passed.
- ERP page fetch passed.
- `Software REV613` found in served page.
- Store final screens found in served page.
- Actual Project BOM renderer found in served page.

## Important Note

The current design intentionally avoids stock deduction from Actual Project BOM itself. Stock is deducted by DC/material issue, and the Actual BOM line is used for project consumption/cost evidence. This prevents double stock reduction.
