# Go-live runbook: fresh database, 1 October 2026

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.


For this runtime-only server, use [server-deployment.md](server-deployment.md) for
package verification, DEMO-first order, service setup and migration bundle commands.
Its deployment sequence supersedes the source/SDK examples below. Principal provisioning
requires `CREATE SCHEMA advance` on an empty database before the Installer; then the
bundle runs as migration login with `Options=-c role=nexa_erp_owner`, followed by
RECONCILED/VERIFIED. The checked-in frontend's production authentication is still a gate.


Status: updated 21 September 2026: Option C directly on DESKTOP-SPF5420. Row counts marked
`[dump]` are filled in from the frontend developer's dump when it arrives. The fresh-company
database workflow has disposable-database evidence in
`FreshCompanyReachesAvailableStockThroughSeededStoresAuthority`; the selected server
installation, capacity and backup checks still need deployment evidence. Nothing here touches
the current field database except the backup and the exports in steps 0 and 1.

Actors: **DBA** = the PostgreSQL administrator (superuser session, `postgres`);
**Owner** = `nexa_erp_migration` acting as `nexa_erp_owner` (psql `SET ROLE`);
**API** = a signed-in seeded employee through the screens or the API.

Precondition for every step that runs a migration: the pulled worktree carries commit
`b13cebd` or later (finding #19). Build Release once on the development laptop: `dotnet build SESS.NexaERP.slnx -c Release`.
Publish the deployment artifacts there; do not build or test on DESKTOP-SPF5420.
In steps 2 onward, execute on the server; `<host>` means 127.0.0.1. No remote DB connection from the laptop.
`<field-host>` in step 0 remains the old laptop.

## 0. Preserve what exists (DBA, before anything else)

### Production server and development laptop (decision, 21 September)

DESKTOP-SPF5420 is the production server. Build the fresh Option C database there
using steps 2-14; section 15 records its specification, disk layout and cutover.
The laptop remains the source of the field backup/exports until cutover, then returns
to development and LabVIEW. The previous 30 September laptop witness cutoff is
superseded: `NexaERP nightly witnesses` may remain on the laptop after server cutover.
Never install or run that task on DESKTOP-SPF5420. Pause laptop witnesses during the
source backup/export window if they would compete with that work.

The existing laptop witness tooling and PostgreSQL binaries can remain in place.
The failure-cleanup limitations below still apply; moving production does not cure
them. Retain gate logs/TRX, review failures, and prevent overlapping witness runs.
The frontend developer's PC is no longer required as the replacement witness host.

### Laptop memory-guard inspection (21 September 2026; still relevant to witnesses)

**Partial protection only; complete failure-safe cleanup is not guaranteed.** The
installed task is Ready, daily at 01:00, `MultipleInstances=IgnoreNew`, with no trigger
end date and a 72-hour execution limit. No task settings were changed in this inspection.
The script waits for each gate before the next. The private `DisposablePostgreSql`
helper belongs to the partial `AdvanceMigrationSqlSyntaxTests` xUnit class, whose tests
run sequentially within one test process. There is no machine-wide lock against another
manual or scheduled test process.

Test `using` scopes dispose clusters on ordinary assertion/exception failures, and
startup catches call `Dispose`. But `_started` is set only after `pg_ctl start` succeeds:
partially successful startup can escape stop cleanup. `Dispose` does not check the stop
exit code, and process termination can bypass disposal. The nightly wrapper has no final
cleanup or orphan-cluster check before the next gate. Thus one cluster at a time with
cleanup on every failure is not guaranteed. This was source/settings inspection only;
no heavy witnesses were launched.

### Backup

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

## 1.5 Server OS decision (Technical Director, 21 September)

Windows 10 Pro stays for this deployment, without ESU currently activated. Windows 11
is a planned later upgrade, not a prerequisite for API deployment. Apply the compensating
controls in section 15.1a and follow `server-deployment.md`. No clean Windows install.

## 2. Create the database on the server (DBA)

Only AFTER accepted DEMO and its deletion, follow server-deployment.md steps 2-3 with
$db='sess_nexa_erp' on the SERVER. Use the published Installer and bundle from the
hash-verified package. Do not execute database commands remotely from the laptop;
pg_hba stays local-only and there is no 5432 firewall opening. No SDK on the server.

## 3. Provision principals (server-local published Installer)

Set exact ExpectedDatabase and the protected installer DBA connection as documented
in server-deployment.md. Use `$installer database-principals provision` and
`$installer database-principals status`. No dotnet run/build/tool installation.

## 4. Migrate 0 to current (migration login assuming owner)

Run the package's migrate/efbundle.exe on the server with the protected migration
connection and Options=-c role=nexa_erp_owner. No dotnet ef; no owner DB application
from this laptop. Follow the package's post-migration checks and replay rules.

Then, API still stopped, as DBA: `database-principals provision` then `database-principals status`
(mandatory reconciliation after every migration run).

Check: `SELECT count(*) FROM advance."__EFMigrationsHistory"` equals the number of migrations
in the built assembly (117 at `c69366d`; 130 with the thirteen 20 September migrations, which
apply in timestamp order 090000 → 210000 with no other ordering requirement). Expected
configuration rows from the 20 September commits: 3 + 1 page + 9 + 1 + 2 + 2 role page
permissions, 8 audit receipts (the route page copies its 9 grants without receipts; its
rollback compares rows instead), one rewritten trigger function
(`guard_estimated_bom_governance`), two new nullable columns (`OriginOpeningStockLineId`
on `stock_movements` and `material_issue_lines`) with their backfill of existing opening
receipts and three rewritten posting functions (#26), eight provenance columns on the two
opening-stock line tables plus the 22-argument staging overload, and the Actual BOM
entry origin column with its rewritten fitment, reversal and dossier functions, 23 report
export grants (one receipt each) and the Technical Support Manager Estimated BOM preparer
grant (one receipt), the Estimated BOM line `ValueSource` column (existing priced lines
classified) and the rewritten draft-line replacement function, and the A2 stock-adjustment
contract (20260920210000): three new tables (`stock_adjustments`, `stock_adjustment_lines`,
`stock_adjustment_decisions`) with their immutability triggers, five new nullable ledger
columns (`StockAdjustmentId` on batches; `StockAdjustmentLineId` and
`OriginStockAdjustmentLineId` on movements; `StockAdjustmentLineId` on FIFO layers and
consumptions, whose `MaterialIssueLineId` becomes nullable under a one-of check), the
batch-kind, batch-source, movement-contract, outbound-origin and FIFO-layer checks widened,
five rewritten functions (batch/movement/reconcile guards, `consume_fifo_for_issue`,
`company_report_fifo_valuation`), two new functions (`post_stock_adjustment`,
`fifo_carrying_value_preview`), the page `stores.stock-adjustments` with 5 role grants (no
receipts; the rollback refuses once an adjustment exists); zero business rows. `status`
prints VERIFIED.

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


### Category reconciliation command (Owner)

```powershell
# Run from the repository root after backup; select the intended database explicitly.
# For today's repair use the developer's server at 130; for go-live use the go-live host/database.
$categoryHost = Read-Host 'PostgreSQL host'
$categoryPort = Read-Host 'PostgreSQL port (normally 5432)'
$categoryDatabase = Read-Host 'Target database name'
# Principal: nexa_erp_migration, SET ROLE nexa_erp_owner (not the API runtime login).
psql -X --host "$categoryHost" --port "$categoryPort" --username nexa_erp_migration --dbname "$categoryDatabase" -v ON_ERROR_STOP=1 -c "SET ROLE nexa_erp_owner" -f .\database\postgresql\reconcile-legacy-item-categories.sql
if ($LASTEXITCODE -ne 0) { throw 'Category reconciliation failed; retain output and resolve the reported guard before retrying.' }
# Changes: EXCEL_IMPORT item categories ELECTRICALS->ELE, FABRICATION->FAB,
# REFRIGERATION->REF; increments versions/update metadata and writes GLOBAL item audits.
# Retires active import-owned legacy aliases, with separate category audits.
# Preserves IDs, receipt snapshots and historical QC evidence.
# Refuses: system databases; missing/inactive required canonical categories; active
# legacy QC policies, vendor qualifications or Stores routes; affected imported items
# with subcategories; active non-import items or active subcategories preventing alias retirement.
# The script owns one SERIALIZABLE transaction: any error rolls the repair back.
# Replay: paste this same block with the same target. With no new affected data it
# changes zero items/aliases and appends zero audits. Do not rerun the original import as a repair.
# Check afterwards: exit 0 and COMMIT; corrected_items sum = 1,087 for today's
# guarded database at 130 (go-live: use its own preflight count, possibly zero).
# Check zero EXCEL_IMPORT items remain on the three legacy codes; ELE/FAB/REF are active;
# retired_import_aliases matches the active import-owned aliases (normally 3).
# Audit deltas: ReconcileImportedCategory = corrected items; RetireImportedCategoryAlias
# = retired aliases; Scope GLOBAL, UserLoginId nexa_erp_migration, ActorRoleCode INSTALLER.
# Replay check: zero corrected items/retired aliases and zero additional audit rows.
# Before receipts, configure effective canonical Stores routes, QC policies and vendor qualifications.
```

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
- Backup: run the server verified-backup job from section 15.3; retain its VERIFIED
  manifest and successful restore evidence, labelled go-live. Verify identity and
  external ERP files through their separate backup procedures too.
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

## 15. DESKTOP-SPF5420: current operating specification

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.

These rules apply to every linked deployment, identity, certificate and backup procedure. Stop and report a conflict; do not reclaim ports or memory from protected services.

Latest facts are [server-facts-20260921.md](server-facts-20260921.md), 16:01:
6.4 GB RAM free, C:41.6 GB free, PG 17.11 local-only pg_hba, .NET10.0.12 runtime,
no SDK. Already prepared controls are recorded at C:\SESS-ServerPrep; do not redo.
Use [server-deployment.md](server-deployment.md) for the authoritative SDK-free,
server-local DEMO-first installation. Earlier laptop/remote dotnet ef examples in
this document are historical development guidance and MUST NOT be used on the server.

Windows 10 stays, no clean install. A later compatible in-place Windows11 upgrade
remains planned because standard commercial ESU costs US$61/$122/$244 per device
in successive programme years (US$427 total), cumulative purchases, taxes/reseller
pricing extra; ESU has not been activated by this decision. Check CPU/TPM2.0/UEFI
Secure Boot/driver compatibility, backup first, and verify protected services,
PostgreSQL, .NET and firewall after any later approved upgrade.
[Microsoft ESU terms](https://learn.microsoft.com/en-us/windows/whats-new/extended-security-updates).

ERP is a Windows service on https://192.168.68.130:8443. Keycloak is local native
Java 17 on 8444, its own sess_keycloak database in the same PG cluster. Boundaries,
ports, memory pools and no-login startup checks are in the linked server procedures.
Keep all protected engineering workloads running; add no development or general
work to the server. Eleven transactional users do not require more than the stable
100Mbps link; allow extra transfer time for daily off-machine copies.

### 15.1 Backup and capacity supersede all earlier D:/E: directions

D:/E: share Disk1 with documented hardware/filesystem errors. Neither may hold new
backups or restore work. Follow [daily backup procedure](server-daily-backups.md):
C:\SESS-Backups component roots, C:\SESS-Backup-Verification, and DAILY verified
copy to another PC plus receiver-owned protected archive. Keep 25 GiB free, only two
verified local bundles per DB, prune only with matching off-machine copies. The
old assertion that 40 GB fits six months is withdrawn for this combined layout:
ERP/identity growth, WAL, dumps and restore peaks must pass the measured space gate.
At a 5-8 GB database this can require more SSD space; fix capacity before that point.
Move to a healthy/replaced HDD only after later acceptance; daily off-machine remains.

### 15.2 Cutover and laptop release

DEMO first, accepted frontend/login/MFA/other-PC/reboot/backup witnesses, drop DEMO,
then fresh Option C sess_nexa_erp directly ON THE SERVER. No stock-moving command
before BOTH opening ceremonies are posted. Admit only named setup/ceremony operators
until both receipts exist; not even a test GRN beforehand.

Only AFTER successful cutover does the laptop return to development/LabVIEW: its
production-hours rule ends, NI services return with RESTORE-NI-Siemens.ps1 and the
nightly development witness can remain. This applies ONLY to the laptop; NEVER run
that maintenance script on the server. Server NI/Siemens/Rockwell stay running.
Backup verification uses one private disposable cluster at a time; no laptop tests
or builds, stock tests or owner-data experiments run on the server.
