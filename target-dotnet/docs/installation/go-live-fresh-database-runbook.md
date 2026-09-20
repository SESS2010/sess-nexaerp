# Go-live runbook: fresh database, 1 October 2026

Status: draft of 20 September 2026 for the owner's decision "Option C". Row counts marked
`[dump]` are filled in from the frontend developer's dump when it arrives; every command,
principal and check below is already witnessed on a disposable database by
`FreshCompanyReachesAvailableStockThroughSeededStoresAuthority`. Nothing here touches
the current field database except the backup and the exports in steps 0 and 1.

Actors: **DBA** = the PostgreSQL administrator (superuser session, `postgres`);
**Owner** = `nexa_erp_migration` acting as `nexa_erp_owner` (psql `SET ROLE`);
**API** = a signed-in seeded employee through the screens or the API.

Precondition for every step that runs a migration: the pulled worktree carries commit
`b13cebd` or later (finding #19). Build Release once: `dotnet build SESS.NexaERP.slnx -c Release`.

## 0. Preserve what exists (DBA, before anything else)

```powershell
pg_dump --format=custom --file .\sess_nexa_erp-pre-go-live.dump --dbname "host=<field-host> port=5432 dbname=sess_nexa_erp user=postgres"
pg_dumpall --globals-only --file .\postgres-globals-pre-go-live.sql --host <field-host> --port 5432 --username postgres
```

Check: both files non-empty; in the dump, `advance.stock_movements` > 0, one POSTED
`advance.opening_stocks` row for SESS_PROPRIETORSHIP, ~1,368 `advance.items` with
`CreatedBy='EXCEL_IMPORT'`. This is the only copy of the 16 September masters and the
19 September ceremony.

## 1. Export the masters from the field database (API, as the roles named)

`GET /api/v1/master-data/{key}/export` saves `{key}-export.xlsx`. Company-scoped keys are
exported once per company (switch the selected company between calls).

| Key | Role that holds `export` | Scope | Notes |
|---|---|---|---|
| `uoms` | TD / MD | shared | round-trips |
| `vendors` | TD / MD (also needs `view-commercial-values`) | shared | bank details excluded by design; attachments carried by step 6 |
| `customers` | TD / MD (also needs `view-commercial-values`) | shared | |
| `item-vendors` | STORES_MANAGER / PURCHASE_MANAGER (after commit for the item move) or TD | shared | import after items and vendors |
| `warehouses` | STORES_MANAGER (after `f2958cb`) or IT_MANAGER | per company | |
| `rack-bins` | STORES_MANAGER (after `f2958cb`) or IT_MANAGER | per company | condition locations are not in it |
| `items` | STORES_MANAGER / PURCHASE_MANAGER or TD | shared | **diff only** — items are reloaded by the script in step 7 |
| `employees` | IT_MANAGER | per company | reference only; employees are migration-seeded |

Also list, as JSON, for re-creation in steps 9–12 (`GET`): item categories/subcategories/
manufacturers, GST rules (`/api/v1/rev869a/configuration/tax-gst`), condition locations,
Stores category routes, QC inspection policies, vendor qualifications, UOM conversions,
operational scopes, identity mappings, temporary role covers.

Check: row counts equal the field screen counts; keep the files with the backup.

## 2. Create the database (DBA)

```powershell
createdb --host <host> --port 5432 --username postgres sess_nexa_erp
```

Check: `SELECT current_database()` = `sess_nexa_erp`; `to_regnamespace('advance')` is null.

## 3. Provision the principals (DBA)

```powershell
$env:ConnectionStrings__NexaErpInstaller = 'Host=<host>;Port=5432;Database=sess_nexa_erp;Username=postgres;Password=<administrator-secret>'
$env:NexaErp__ExpectedDatabase = 'sess_nexa_erp'
$env:NEXAERP_MIGRATION_PASSWORD = '<new>'; $env:NEXAERP_BOOTSTRAP_PASSWORD = '<new>'; $env:NEXAERP_RUNTIME_PASSWORD = '<new>'
dotnet run --project .\src\SESS.NexaERP.Installer\SESS.NexaERP.Installer.csproj -c Release -- database-principals status     # expect NOT_PROVISIONED, exit 3
dotnet run --project .\src\SESS.NexaERP.Installer\SESS.NexaERP.Installer.csproj -c Release -- database-principals provision
dotnet run --project .\src\SESS.NexaERP.Installer\SESS.NexaERP.Installer.csproj -c Release -- database-principals status     # expect VERIFIED, exit 0
```

Check: four roles exist, `nexa_erp_owner` is `NOLOGIN`, no business rows (there are none yet).

## 4. Migrate 0 → current (Owner)

```powershell
$env:ConnectionStrings__NexaErp = 'Host=<host>;Port=5432;Database=sess_nexa_erp;Username=nexa_erp_migration;Password=<migration-secret>;Options=-c role=nexa_erp_owner'
$env:NexaErp__ExpectedDatabase = 'sess_nexa_erp'
dotnet ef database update --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
```

Then, API still stopped, as DBA: `database-principals provision` then `database-principals status`
(mandatory reconciliation after every migration run).

Check: `SELECT count(*) FROM advance."__EFMigrationsHistory"` equals the number of migrations
in the built assembly (117 at `c69366d`; 128 with the eleven 20 September migrations, which
apply in timestamp order 090000 → 190000 with no other ordering requirement). Expected
configuration rows from the 20 September commits: 3 + 1 page + 9 + 1 + 2 + 2 role page
permissions, 8 audit receipts (the route page copies its 9 grants without receipts; its
rollback compares rows instead), one rewritten trigger function
(`guard_estimated_bom_governance`), two new nullable columns (`OriginOpeningStockLineId`
on `stock_movements` and `material_issue_lines`) with their backfill of existing opening
receipts and three rewritten posting functions (#26), eight provenance columns on the two
opening-stock line tables plus the 22-argument staging overload, and the Actual BOM
entry origin column with its rewritten fitment, reversal and dossier functions, 23 report
export grants (one receipt each) and the Technical Support Manager Estimated BOM preparer
grant (one receipt); zero business rows. `status` prints VERIFIED.

## 5. Authentication bootstrap and identities (DBA, then API)

Run the one-time ceremony as `nexa_erp_bootstrap` with SESS-12's exact issuer and `sub`
(see `authentication-bootstrap.md` steps 7–12), then sign in as SESS-12 and create every
other employee's identity mapping through the governed identity endpoint
(`security.employee-identities`) — `[dump]` mappings. Re-create any temporary role covers
still in effect (`[dump]`).

Check: `/api/v1/session/me` for SESS-12; each mapped user can sign in and sees their company.

## 6. Carry the attachments across (Owner)

Vendor and customer attachments are `bytea` rows with their own GUIDs and no outward foreign
keys; the vendor workbook's `AttachmentMetadataJson` references them by GUID.

```powershell
pg_restore --host <host> --port 5432 --username nexa_erp_migration --dbname sess_nexa_erp --data-only --schema advance --table vendor_attachments --table customer_attachments .\sess_nexa_erp-pre-go-live.dump
```

(run with `PGOPTIONS="-c role=nexa_erp_owner"`). Check: row counts equal the field
(`[dump]` vendor attachments, `[dump]` customer attachments); no other table touched.

## 7. Load the item master (Owner)

```powershell
psql --host <host> --port 5432 --username nexa_erp_migration --dbname sess_nexa_erp -v ON_ERROR_STOP=1 -c "SET ROLE nexa_erp_owner" -f .\database\postgresql\legacy-item-import-2026-08-29.sql
```

Check: `SELECT count(*) FROM advance.items WHERE "CreatedBy"='EXCEL_IMPORT'` = 1,368, all
`Approved`; categories ELE/FAB/REF present; UOMs present. The #11 reconcile script is not
needed on a database the corrected import created.

**Applying the item differences.** Diff `items-export.xlsx` from step 1 against the script
by ItemCode (`tools/` gets a small script for this when the dump arrives; it prints one row
per differing item and column). Then:

- **Up to about ten differences**: `PUT /api/v1/inventory/items/{code}` through the item
  screen as Stores or Purchase Manager. Each correction returns the item to approval; the
  other manager approves it (the record is less than a month old on the fresh database).
- **More than that**: correct the checked-in script instead and re-run step 7 on a fresh
  database — the script is the source of truth for 1,368 items, and regenerating it from
  the corrected workbook (`tools/generate-item-import.py`) is one reviewed change plus one
  owner command, not fifty screen edits followed by fifty approvals. Commit the corrected
  script so the next installation carries the corrections too.

Either way the check is the same: after the corrections, the export of the fresh database
equals the export of the field database column for column.

## 8. Import the workbook masters (API)

Order and roles, per company where scoped:

1. `uoms` — Purchase or Stores Manager.
2. item categories, subcategories, manufacturers not created by step 7 — `POST /api/v1/masters/…` (`[dump]` rows).
3. `vendors` — IT Manager or TD (create), then per vendor: Accounts Manager
   `verify-commercial`, Managing Director `approve` (`[dump]` vendors → 2 governed actions each).
4. `customers` — same lifecycle (`[dump]` customers).
5. `item-vendors` — Purchase Manager.
6. `warehouses`, then `rack-bins` — Stores Manager, per company.

Check after each: import result `InvalidRows = 0`; counts equal step 1.

## 9. Stores topology (API, Stores Manager, per company)

For each rack: `POST /api/v1/rev869a/configuration/warehouse-condition-locations`
(`[dump]` rows). For each item category received: `POST …/store-category-routes` naming
the QC_HOLD, PENDING_RETURNABLE_DC and AVAILABLE locations of one warehouse (`[dump]` rows).

Check: `GET …/warehouse-condition-locations?effectiveOnly=true` shows one AVAILABLE per
receiving warehouse; `GET …/store-category-routes?effectiveOnly=true` shows one route per
category that will be received.

## 10. Tax and QC rules (API)

GST rules: Accounts Manager `POST …/tax-gst`, Managing Director `POST …/tax-gst/{id}/approve`
(`[dump]` rules × 2 actions). QC policies: QC Manager `POST …/qc-inspection-policies`,
Technical Director approve (`[dump]` policies × 2 actions). UOM conversions if any.

## 11. Vendor qualifications (API)

Per vendor and category: Purchase Manager create, Technical Director verify, Managing
Director approve (`[dump]` qualifications × 3 actions).

## Rollback points

Nothing before step 12 posts stock, so every step before it can be redone from a known
state without losing evidence that matters:

| If this goes wrong | Restore | Redo |
|---|---|---|
| Steps 2–4 (create, provision, migrate) | `dropdb sess_nexa_erp` (DBA) | from step 2 |
| Step 5 (bootstrap, identities) | the ceremony is one-time per database: `dropdb` and restart from step 2 | from step 2 |
| Steps 6–7 (attachments, item script) | `dropdb` and restart from step 2 — both are idempotent scripts but a partial script leaves rows with `CreatedBy='EXCEL_IMPORT'` you would have to reason about | from step 2 |
| Steps 8–11 halfway (imports, topology, rules) | **take a `pg_dump` before step 8** (`go-live-after-items.dump`) and restore it (`dropdb`, `createdb`, `pg_restore`, then `database-principals provision` + `status`) | from the first import that did not complete; every import is idempotent per workbook and idempotency key, so re-running a completed import is a replay, not a duplicate |
| Step 12 or 13 fails before `authorize` | nothing to restore: count and value are drafts | re-run the failed action with a new idempotency key |
| Step 12 or 13 fails after `authorize` returned 200 | the ceremony is posted; **do not** restore a pre-ceremony dump on top of a database that has been used since | none — investigate before touching anything |

Take one more `pg_dump` after step 11 (`go-live-configured.dump`): that is the
configured-but-empty database, the natural restart point for either ceremony.

## 12. Opening stock, SESS_PROPRIETORSHIP (API, three actors)

Stores Manager: `POST /api/v1/master-data/opening-stock/import` with the retained
workbook, then `POST /api/v1/stores/opening-stock/from-import`. Accounts Manager:
`…/confirm-value`. Technical Director: `…/authorize`.

Check: status POSTED; `stock_movements` count for the company equals the workbook line
count; FIFO layer value equals the confirmed total; the values equal the 19 September
ceremony in the backup.

## 13. Opening stock, SESS_PVT_LTD (API, three actors) — the one step with no rehearsal

PVT LTD's opening stock has never been posted anywhere. The PROPRIETORSHIP ceremony was
done once on the developer's machine, so its shape is known; PVT LTD will be the first
time, with real quantities, on go-live day. Prepare the workbook before the day, not on it.

**Who prepares it.** The Stores Manager (SESS-41) prepares the physical count; Accounts
(SESS-14) supplies the rate per line from the carrying-value policy; the Technical
Director authorizes and does not prepare. The count is a physical count of what is on the
racks on the cutover date, not a copy of any earlier system's balance.

**What it must contain.** One row per item, warehouse, rack and (where the item is tracked)
lot or serial, in version 2 of the `opening-stock` template — `docs/installation/opening-stock-template-v2.xlsx`
now, `GET /api/v1/master-data/opening-stock/template` once the database exists. Values are
**ex-tax**. The optional provenance columns (vendor name, vendor bill number, bill date,
purchase date, make, model, part number, remarks) are filled where SESS knows them and left
blank where it does not; they never change the value:

| Column | Rule |
|---|---|
| `LineReference` | unique within the workbook (e.g. `PVT-OPEN-0001`); it is the operator's stable row reference in every later report |
| `ItemCode` | an approved, active item — exactly as loaded by step 7 |
| `WarehouseCode` | an active PVT LTD warehouse created in step 8 |
| `RackBinCode` | an active rack of that warehouse whose condition is AVAILABLE and that has an effective AVAILABLE condition location (step 9); the rack is mandatory because the legacy export has no rack data |
| `LotNumber` | required for batch-tracked items, blank otherwise |
| `SerialNumber` | required for serial-tracked items; one row per unit with `Quantity` = 1 |
| `Quantity` | greater than zero; zero-quantity lines are left out, negative is refused |
| `Unit Value Ex-Tax` | the taxable unit value in rupees without GST, zero or more; the stated legacy value is deliberately not imported and the line value is recomputed as Quantity × Unit Value |
| provenance columns | optional; declared, not verified; shown in the machine dossier as "declared at opening stock, not verified in this system" |

Items with lot or serial tracking are the rows most likely to be wrong on the day: get the
serial lists from the racks, not from the old system. A row that fails validation stops
nothing — the import reports it and the corrected workbook is re-imported under a new
idempotency key before the count is recorded.

**What the ceremony then does.** Stores Manager: import, then `from-import` with the
fiscal period (1 April – cutover date). Accounts Manager: `confirm-value`. Technical
Director: `authorize`. The authorization is the moment PVT LTD acquires stock movements;
after it, the rule "company already has movements" applies to PVT LTD for good.

Check: status POSTED; movement count equals the workbook line count; FIFO layer value
equals the confirmed total; `GET /api/v1/reports/…` stock and FIFO valuation agree.
Then the first MIR: until `20260920150000_OpeningStockIssueOrigin` (finding #26) no
opening-stock unit could be issued at all; the rehearsal now issues, returns and fits
opening stock, values the fitment at the confirmed ex-tax value, and shows it in the
dossier as opening stock. Opening stock consumes before any later receipt of the same
item (FIFO dates it from the period end above).

## 14. Close

- Reconciliation: DBA `database-principals provision` then `status` → VERIFIED.
- Backup: `pg_dump` + `pg_dumpall --globals-only` of the new database, labelled go-live.
- **No walk-throughs in either company from this point.** The opening-stock rule refuses a
  company that already has movements; there is no legitimate escape from it.

## Human effort (to be completed from the dump)

| Step | Rows | Actions | Second actor |
|---|---:|---:|---|
| 5 identities | `[dump]` | 1 each | — |
| 8.3 vendors | `[dump]` | 3 each | Accounts + MD |
| 8.4 customers | `[dump]` | 2–3 each | approver |
| 9 locations + routes | `[dump]` | 1 each | — |
| 10 GST rules | `[dump]` | 2 each | MD |
| 10 QC policies | `[dump]` | 2 each | TD |
| 11 qualifications | `[dump]` | 3 each | TD + MD |
| 12–13 ceremonies | 2 | 3 each | Accounts + TD |
