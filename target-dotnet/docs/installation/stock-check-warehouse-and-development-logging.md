# Stock-check warehouse and development logging

Parent: 03400f7. This is the small-fix work explicitly requested alongside the three-day items. No database migration or owner database access is involved.

GET /api/v1/stores/stock-check/requisitions/{prNumber} now includes **DeliveryWarehouseCode** in its existing PascalCase JSON detail. The value comes from the requisition's delivery warehouse; a missing historical warehouse is represented by an empty string, matching the existing requisition-detail convention. Permission, selected-organization and StockCheckPending filters remain in the query. Prices, purpose, customer and approval details are not added. Frontend consumers can use this additive field to display or preselect the requested delivery warehouse. There is no dedicated stock-check-detail frontend type or page in the current source.

When the API environment is Development, the Microsoft.EntityFrameworkCore.Database.Command log category has a Warning minimum. Warnings and errors remain visible. The setting uses the existing application startup and does not add a configuration file containing local credentials.

Verification pending: builds in Release and Debug, the existing stock-check permission/projection checks, and the complete three-band PostgreSQL purchase flow with a DeliveryWarehouseCode assertion for TRIAL-WH-C01. No new standalone test is added for the log filter.

Suranther's Tuesday checklist step 11 also clarifies that the identity-administration screen is not implemented. The setup engineer assists with the existing governed administrator operations after bootstrap; the checklist no longer suggests an available mapping screen. No authentication behavior changes.

Release build: zero warnings/errors, 3m50.84s. Selected Release tests: **6 passed, 0 failed/skipped, 4m04s**, in local-evidence/item25/small-fixes-stock-check-release.trx. These are five existing company-number/stock-check checks and the complete three-band PostgreSQL purchase/report flow. Expected warehouse is TRIAL-WH-C01 in each band's stock-check response, while the wrong-company request remains 404. Debug verification is pending. This is not a new full-suite count.

Debug build: zero warnings/errors, 4m17.93s. The same selected Debug checks passed: **6 passed, 0 failed/skipped, 4m12s**, in local-evidence/item25/small-fixes-stock-check-debug.trx. Release and Debug both verify the complete three-band flow with the warehouse field and wrong-company refusal. No standalone runtime logging test was added for this low-impact filter. The checklist correction is documentation only.
