# R1 director page grants - Option B

CONFIRMED subsequent TD decision: migration 141 adds `admin.email` View + Update for
MD and ensures TD View + Update / IT View; see the [email contract](R1-email-lite-contract-for-TD.md).
The full table below is the migration-140 baseline; migration 141 adds one MD page.

CONFIRMED: TD approval, 29 September 2026. Migration
`20260929110000_R1DirectorPageGrants` preserves all existing grants and adds only
missing View on the named director pages. Pending additionally requires ViewAuditHistory.
No write, approval, export, commercial-value or full-control action is added.

## Approved resulting page groups

| Role | Pages covered by the approved additions |
|---|---|
| TECHNICAL_DIRECTOR | Material Issues; Fitments / Actual BOM; all five Accounts pages |
| MANAGING_DIRECTOR | All four Production pages; all five Accounts pages |

Accounts: Intercompany Invoices, Inventory Periods, Supplier Invoices, Vendor Bills,
Vendor Advances and Payments (`accounts.vendor-financial-evidence`).
Production: Fitments / Actual BOM, FAT Readiness, Job Orders, Production BOM.

CONFIRMED by disposable PostgreSQL read-back at migration 139: migration 138 already
provides both Pending View and ViewAuditHistory to STORES_MANAGER, STORES_EXECUTIVE,
QC_MANAGER, ACCOUNTS_MANAGER, TECHNICAL_DIRECTOR and MANAGING_DIRECTOR. Those seed
rows remain unchanged. Migration 140 also repairs a missing row or either missing
flag without removing any other action; the focused tests exercise both cases.

## Complete director page visibility after migration 140

CONFIRMED: read back from a fresh disposable PostgreSQL database, not a server or
operator-customized database. Existing deployment-specific grants are retained.
The table lists effective View (`CanView` or `HasFullControl`); it does not imply
other actions. Blank means no seeded View for that role.

| Page key | TECHNICAL_DIRECTOR | MANAGING_DIRECTOR |
|---|---|---|
| `accounts.intercompany-invoices` | View | View |
| `accounts.inventory-periods` | View | View |
| `accounts.supplier-invoices` | View | View |
| `accounts.vendor-bills` | View | View |
| `accounts.vendor-financial-evidence` | View | View |
| `admin.email` | View |  |
| `audit.history` | View | View |
| `authorization.pages` | View | View |
| `authorization.role-pages` | View | View |
| `dashboards.purchase` | View | View |
| `dashboards.purchase-obligations` | View | View |
| `dashboards.purchase-open-orders` | View | View |
| `dashboards.purchase-spending` | View | View |
| `dashboards.stores-qc-stock` | View | View |
| `dashboards.stores-workload` | View | View |
| `design.engineering-documents` | View |  |
| `design.estimated-bom` | View |  |
| `employees.audit-history` | View | View |
| `employees.master` | View | View |
| `employees.role-mapping` | View | View |
| `identity.roles` | View | View |
| `identity.users` | View | View |
| `inventory.grn` | View | View |
| `inventory.items` | View | View |
| `inventory.rack-bins` | View | View |
| `inventory.stock-ledger` | View | View |
| `inventory.warehouses` | View | View |
| `masters.customers` | View | View |
| `masters.item-categories` | View | View |
| `masters.item-subcategories` | View | View |
| `masters.items` | View | View |
| `masters.manufacturers` | View | View |
| `masters.rack-bins` | View | View |
| `masters.store-category-routes` | View | View |
| `masters.uom-conversions` | View | View |
| `masters.uoms` | View | View |
| `masters.vendor-qualifications` | View | View |
| `masters.vendors` | View | View |
| `masters.warehouse-condition-locations` | View | View |
| `masters.warehouses` | View | View |
| `production.component-fitments` | View | View |
| `production.fat-readiness` |  | View |
| `production.job-orders` | View | View |
| `production.production-bom` | View | View |
| `purchase.commercial-comparisons` | View | View |
| `purchase.material-followup` | View | View |
| `purchase.po` | View | View |
| `purchase.requests` | View | View |
| `purchase.requirement-handoff` | View | View |
| `purchase.requisition-approvals` | View | View |
| `purchase.requisitions` | View | View |
| `purchase.rfq` | View | View |
| `purchase.technical-verification` | View | View |
| `purchase.vendor-quotations` | View | View |
| `qc.inspection-policies` | View | View |
| `reports.engineer-custody` | View | View |
| `reports.movement-roll-forward` | View | View |
| `reports.pending-approvals` | View | View |
| `reports.purchase-register` | View | View |
| `reports.stock-balance` | View | View |
| `sales.customer-po` | View | View |
| `security.employee-identities` | View | View |
| `security.operational-scopes` | View | View |
| `settings.tax-gst` | View | View |
| `stores.intercompany-routes` | View | View |
| `stores.machine-deliveries` | View |  |
| `stores.material-issue-excess` | View |  |
| `stores.material-issue-requests` | View | View |
| `stores.material-issues` | View |  |
| `stores.material-returns` | View | View |
| `stores.opening-stock` | View |  |
| `stores.reservations` | View | View |
| `stores.stock-adjustments` | View | View |
| `stores.stock-check` | View | View |
| `tracking.pending` | View | View |

The read-back contains 74 TD pages and 68 MD pages.

## Scope question retained for the TD

CLAIMED (pending TD decision): expand the named additions to include TD View on
FAT Readiness and MD View on Material Issues / Machine DC. These three are outside
the explicit role/page additions above and remain absent in this read-back.

## Validation and rollback

CONFIRMED: the focused tests verify the approved page groups, all six Pending grants,
no removed rows or actions, no unrelated row changes, refusal to roll back changed
permissions, exact restoration of all prior rows, and equivalent grants after reapply.
The existing tracking-138 rollback test also runs with migration 140 as a successor.
An internal before/after journal protects subsequent operator changes during Down.
No SECURITY DEFINER function is introduced. Both merged migrations 138 and 139 are untouched.

CONFIRMED final validation: focused DB **3/3**, fast suite **1045/1045**, zero skips.

Evidence: `local-evidence/director-grants-20260929/`; final counts are recorded in
`results-lf.json`. The initial fast run caught CRLF in the new migration source;
the source was corrected to LF per `.gitattributes` before the final checks.
