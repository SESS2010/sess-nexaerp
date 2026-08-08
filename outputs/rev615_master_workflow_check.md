# REV615 Master Workflow Check

Generated: 2026-07-03T14:14:47.805Z
Installed file: C:\Users\User\AppData\Local\SESS NexaERP\app\InventoryERP_Software.html
Result: PASS (32/32 checks passed)

## Passed checks
- Revision is REV615 in installed UI: Installed HTML visible revision is aligned.
- Customer Master menu/page exists: Customer Master button is present.
- Vendor Master menu/page exists: Vendor Master button is present.
- Approved Vendor Master exists: Approved Vendor Master button is present.
- Item Master menu/page exists: Item Master button is present.
- Finished Goods/Product Master screen exists: Product master screen exists in menu and page shell.
- Product Master is a real entry form: Separate finished-goods/product save form is present.
- Item Master requires item code: Normal save blocks blank item code.
- Item Master requires barcode: Normal save blocks blank barcode.
- Item Master blocks duplicate item code: Normal create/update checks duplicate item code.
- Item Master blocks duplicate barcode: Normal create/update checks duplicate barcode.
- Item Master saves approval inactive by default: New item waits for approval before active use.
- Item Master writes audit/freeze control: Master freeze and audit hooks are present.
- Vendor Master blocks duplicate vendor: Vendor save blocks duplicate vendor name.
- Vendor Master protects approved vendor edits: Approved vendor edits are routed through change request control.
- Vendor Master saves pending inactive by default: New vendor waits for TD approval.
- Customer Master blocks duplicate customer name: Customer save blocks duplicate customer name.
- Customer Master writes audit/freeze control: Customer master has freeze and audit hooks.
- Customer PO blocks duplicate PO number: Customer PO number duplicate guard is present.
- Customer PO validates PDF attachment: Customer PO PDF validation is present.
- Project Master blocks duplicate job number: Project/job number duplicate guard is present.
- Project Master links offer/OA context: Project master pulls linked offer/OA data.
- Service Master computes warranty dates: Warranty start/end calculation is present.
- Service Master stores AMC/CAMC contract fields: AMC/CAMC fields are captured.
- Service Master blocks duplicate asset number: Duplicate asset number guard is present in normal save handler.
- Service Master blocks duplicate serial number: Duplicate serial number guard is present in normal save handler.
- Master data control screen exists: Master Data Control screen is available.
- Master freeze helpers exist: Shared master freeze helpers are installed.
- Barcode print support exists: Barcode printing/search support is present in the installed file.
- Visible revision guard is REV615: Bottom visible revision guard is aligned to REV615.
- Product Master writes productMasters data: Product Master stores separate product master rows.
- Product Master blocks duplicate product/model: Product/model duplicate guard is present.

## Evidence lines
- Product Master page shell: line 12047
- Product Master dynamic row source: line 45133
- Item Master save handler: line 51901
- Customer Master save handler: line 51144
- Vendor Master save handler: line 50988
- Customer PO save handler: line 53772
- Service Master save handler: line 54226
- Project Master save handler: line 55255

## Auditor note
- This was a read-only check. No installed ERP files were modified.
- Product Master now has a real REV615 entry form backed by productMasters.
- Service/Machine Master now has strict duplicate asset and serial guards in the normal save handler.