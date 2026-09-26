# Finding: no installation-configuration export

Status: finding, recorded 20 September 2026. Not scheduled for go-live on 8 October. It is for the
version that is sold.

## What was found

Rebuilding the go-live database from a fresh install exposed that the product can export
six masters as workbooks and nothing else. There is no path that captures an
installation's configuration as a whole. A customer moving servers, restoring after a
failed upgrade, or standing up a second environment hits the same wall SESS hit on
20 September.

The per-master workbook path is `GET /api/v1/master-data/{masterKey}/export` with a
matching `POST /api/v1/master-data/{masterKey}/import`. It covers `uoms`, `vendors`,
`customers`, `warehouses`, `rack-bins`, `items`, `item-vendors` and `employees`
(`opening-stock` exports nothing by design: it stages only).

## What cannot be exported today

| Configuration | What exists | Gap |
|---|---|---|
| Item categories, subcategories, manufacturers | JSON list and `POST` under `/api/v1/masters` | No workbook; re-created one row at a time |
| GST tax rules | JSON list, `POST`, and `POST …/approve` by a second actor | No workbook; every rule needs re-approval |
| Warehouse condition locations | JSON list and `POST` under `/api/v1/rev869a/configuration` | No workbook |
| Stores category routes | **No API.** Only the development-only trial script creates them | A real warehouse cannot receive goods in a fresh company until this exists |
| QC inspection policies, vendor qualifications, UOM conversions, operational scopes | JSON list, `POST`, approve/reject | No workbook; governed re-approval each |
| Item per-company inventory settings (serial capture) | Governed endpoint only | No export |
| Vendor bank details | Excluded from the vendor workbook by design | Re-entered in the UI only |
| Vendor and customer attachments | Content endpoints only | No bulk export |
| Role assignments, temporary covers, identity mappings, page-permission changes | Governed endpoints | No export; only the migration-seeded baseline returns |
| Company sites, GST registrations, financial periods, inventory periods | Migration seed or governed endpoints | No export |

Two further limits of the workbook path matter for a rebuild:

- **Items**: the workbook carries none of `StandardEstimatedPrice`, `ManufacturerMake`,
  `Model`, `PartNumber`, `Barcode`, and every re-imported item lands `DRAFT`, needing
  individual approval. The real item master is therefore reloaded from the checked-in
  `database/postgresql/legacy-item-import-2026-08-29.sql` (fixed identities, `Approved`),
  not from an export.
- **Lifecycle**: exported `Status`, `ApprovalStatus` and `IsActive` are read-only on
  import. Approved vendors, customers, warehouses and racks come back as drafts.

## What a complete configuration export must carry

One archive, produced by one governed command, restorable by one governed command, with:

1. **Reference masters in dependency order**: UOMs and conversions, item categories and
   subcategories, manufacturers, items (all columns, including the five above and the
   per-company inventory settings), vendors (with attachments and, under a separate
   sensitive-permission gate, bank details), customers (with attachments), item-vendor
   links, employees and company assignments.
2. **Stores topology**: warehouses, racks, condition locations, Stores category routes,
   QC inspection policies — with their effective dates and approval status, so a restore
   does not silently demote approved configuration to draft.
3. **Tax and commercial rules**: GST rules per HSN and state with approval evidence,
   purchase approval bands, vendor qualifications.
4. **Authority**: roles, page permissions, employee role assignments (including temporary
   covers and their dates), identity mappings, operational scopes.
5. **Company structure**: companies, sites, GST registrations, financial and inventory
   periods.
6. **Integrity**: a manifest with the source database's migration id, row counts per
   table, and a SHA-256 per file, checked on restore; and a restore that refuses a
   target whose migration id differs from the manifest.
7. **Provenance preserved**: original `CreatedAt`/`CreatedBy`, record identities and
   versions kept, so audit references from later evidence still resolve. A restore is
   an import of governed configuration, not a re-entry.

Transactional evidence (stock movements, receipts, bills, FIFO layers) is deliberately
outside this export. That is what `pg_dump` is for; configuration export exists so that a
clean database can be brought to the same configured state without carrying transactional
history.

## Immediate consequence for 20 September

The go-live rebuild reloads items from the checked-in script, re-imports the six
workbook masters, and re-creates every row in the table above by hand or by script.
The runbook records each of those steps explicitly. The Stores category route gap is
handled under finding #18.
