# Go-live runbook: fresh database, 1 October 2026

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
In steps 2 onward, `<host>` means DESKTOP-SPF5420 (or its reserved LAN address).
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

## 2. Create the database directly on DESKTOP-SPF5420 (DBA)

Option C is a fresh database on the final production server, not a full restore of the
field database. Install PostgreSQL 17 with its production data directory on C: first.
Confirm the target hostname and `SHOW data_directory`; do not drop or overwrite an
existing database if this name is already present. Keep the laptop source intact.


```powershell
createdb --host DESKTOP-SPF5420 --port 5432 --username postgres sess_nexa_erp
```

Check: `SELECT current_database()` = `sess_nexa_erp`; `to_regnamespace('advance')` is null.

## 3. Provision the principals (DBA)

Run the source-based `dotnet run` commands below from the development laptop targeting
DESKTOP-SPF5420. Alternatively run the matching published Installer on the server;
server installation does not require a source checkout, SDK, compiler or test suite.


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

Run this development command from the laptop against DESKTOP-SPF5420 while its API
is stopped. It builds tooling on the laptop and creates the production schema on the
server. Applying reviewed ERP migrations is production maintenance; running builds,
tests or disposable development clusters on the server is prohibited at all hours.

```powershell
$env:ConnectionStrings__NexaErp = 'Host=<host>;Port=5432;Database=sess_nexa_erp;Username=nexa_erp_migration;Password=<migration-secret>;Options=-c role=nexa_erp_owner'
$env:NexaErp__ExpectedDatabase = 'sess_nexa_erp'
dotnet ef database update --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
```

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

## 15. DESKTOP-SPF5420: dedicated production server at all hours

Decision, 21 September 2026: use the existing office PC DESKTOP-SPF5420. The laptop
returns to development and LabVIEW after successful cutover. The former laptop
09:30-18:30 production-only rule and 1 October witness cutoff are superseded.

**Nothing except the ERP and its operational dependencies runs on the server, ever.**
This includes PostgreSQL, required identity/runtime services, verified backup/restore
verification, monitoring, OS/security maintenance and controlled ERP deployment.
No development editors/agents, builds, tests, nightly witnesses, LabVIEW/NI or general
office work, including at night. Build/publish on the laptop and deploy artifacts.
A backup's isolated restore-verification cluster is an ERP backup operation, not a
permission to run development witnesses on the server.

### 15.1 Selected server specification and disk allocation

Owner-supplied specification; deployment checks on the actual server remain to be recorded:

| Component | Selected hardware / allocation |
|---|---|
| Host / OS | DESKTOP-SPF5420; currently Windows 10 Pro 64-bit; Windows 10 retained by TD decision; Windows 11 planned later; ESU costs in 15.1a |
| CPU | Intel Core i5-10600K, desktop, 6 physical cores / 12 logical; reported not throttled |
| RAM | 15.9 GB; reserved for ERP and its dependencies |
| Physical SSD | WDC WDS240G2G0A, 224 GB; C:, currently 40 GB free; Windows, ERP binaries and production PostgreSQL data |
| Physical HDD | Seagate ST1000DM010, 932 GB; D: currently 284 GB free, E: 339 GB free |
| Verified bundles | `D:SESS-Backups` on the separate physical HDD |
| Verification work | Prefer `E:SESS-Backup-Verification`; `D:SESS-Backup-Verification` also permitted as a separate directory |
| Network | Realtek Gaming GbE Ethernet adapter; currently negotiated at 100 Mbps |
| Power / account | UPS connected; Windows login password set |

C: and D: are on **different physical disks**. D: and E: are partitions of the **same
HDD**; using both does not create a third independent copy. Record the volume-to-disk
mapping during setup (`Get-Partition -DriveLetter C,D,E | Get-Disk`), and confirm
PostgreSQL `SHOW data_directory` resolves to C:. Never infer physical independence
from drive letters alone.

The CPU and RAM are suitable for the estimated eleven-user ERP workload, subject to
normal cutover checks under real use. There is no need to buy the previously proposed
server before this deployment. Windows 10 support ended on 14 October 2025. Resolve the OS decision now as follows.

### 15.1a Windows 10 retained now; Windows 11 planned later

The Technical Director has decided to retain Windows 10 for now, without ESU/security
patch coverage. Do not upgrade or reinstall it as part of this ERP deployment.
This supersedes c7832b3's upgrade-before-API requirement. Required controls:

- At least WEEKLY, copy the newest complete VERIFIED ERP backup, identity/configuration
  backups and required external files OFF this machine. Verify copied hashes and retain
  a dated copy record. Disconnect removable media after the copy, or use off-machine
  storage with credentials/immutability that this PC cannot use to encrypt/delete history.
  An always-writable network share is not sufficient ransomware isolation. C: and D:
  are different disks, but ransomware on this unpatched PC can encrypt both together.
- Windows Firewall enabled; ERP inbound TCP 8443 allowed ONLY from `192.168.68.0/24`.
  Default inbound block; no internet/router port forwarding. Audit existing broad allow
  rules: one scoped new rule does not narrow other existing rules. Do not blindly disable
  rules needed by protected SOLIDWORKS services; any conflict with the requested
  server-wide ERP-only inbound policy must be resolved and recorded by the TD first.
- Microsoft Defender active, real-time protection on and definitions current. Record
  `Get-MpComputerStatus`; OS security patches and Defender definitions are different.
- RDP off, including inbound RDP rules. Use local console for deployment/maintenance.

Plan a later Windows 11 in-place upgrade after a recoverable PC/data backup and
SOLIDWORKS compatibility review. NEVER a clean Windows install. The i5-10600K CPU
family is eligible, but run PC Health Check and verify TPM 2.0, UEFI/Secure Boot and
drivers on this actual motherboard. Back up encryption recovery keys and verify
SQL Server/SOLIDWORKS, PostgreSQL, .NET and firewall behaviour after that future upgrade.

ESU cost is a reason to plan the upgrade rather than leave this indefinite:
Microsoft's standard commercial price is US$61/device for programme Year 1, US$122
for Year 2 and US$244 for Year 3 (US$427 total), before taxes/local reseller pricing.
Prior years are cumulative; no partial-year purchase. Near the October 2026 Year 1
boundary, Years 1+2 total US$183. These are not twelve months from purchase. ESU is
an available option, not purchased/activated by this decision. Confirm INR quotes and
coverage with the supplier ([Microsoft terms](https://learn.microsoft.com/en-us/windows/whats-new/extended-security-updates)).
The retained OS risk is accepted by the TD; the controls do not make it equivalent
to a patched OS. Do not claim Windows 10 is supported merely because Defender is current.

### 15.2 Capacity, network and operating checks

**40 GB free on C: is enough for the estimated 5-8 GB PostgreSQL footprint at six
months.** Conservatively subtracting the whole 8 GB leaves about 32 GB before new
software, Windows updates, pagefile changes and other growth. This is a planning
estimate, not a measured growth guarantee. Keep **at least 25 GB free** in operation;
target **40 GB or more free after installation and the colleague's files are moved**.
Do not count pending file moves as already recovered space. If installation or growth
would breach the 25 GB reserve, reclaim space or expand the SSD before proceeding.

The 5-8 GB estimate assumes modest eleven-user transactions and approximately
20 attachments/day at 1 MB across working days. Attachments stored as `bytea`, indexes,
audit rows, identity data, logs and actual usage must be measured. `max_wal_size` is
not a hard cap: do not assume WAL can never exceed 1 GB
([PostgreSQL WAL guidance](https://www.postgresql.org/docs/17/wal-configuration.html)).
Keep dumps and verification restores off C:. Check free space daily and trend database,
cluster/WAL, logs, attachment and backup sizes monthly. `pg_database_size` measures the
database, not the whole cluster or its WAL.

100 Mbps is adequate for the expected eleven-user transactional workload. Its nominal
ceiling is 12.5 MB/s shared across transfers; large uploads, exports and initial copies
will take longer. No go-live step requires gigabit: database creation/migrations are
small transfers and scheduled dump/restore verification runs locally between SSD and
HDD. Allow time for initial file copies and verify normal client response at cutover.
Check/replace the cable with a known-good Cat5e/Cat6 cable and confirm the switch port
supports gigabit; the cable is a likely cause, not a confirmed diagnosis. A stable
100 Mbps link alone does not block go-live.

Before admitting users, record: production service startup and restart behaviour,
ERP/API readiness and login from client PCs, UPS operation, no sleep/hibernation,
reserved LAN address/name resolution and firewall rules, C: reserve, and successful
ERP plus identity backups. Use service accounts and supported service/task hosting;
do not assume the laptop's interactive startup shortcut survives sign-out/reboot.
Keep unrelated services and applications off the server.

`tools/production-state.json` and `Enter-ProductionDay.ps1` describe the old laptop
setup; do not deploy or run them unchanged. A server monitoring profile must use its
actual services, API readiness URL, cluster path and backup paths, require no witness
task, use C: >= 25 GB free and a provisional available-RAM alert at 4 GB (not the old
12 GB threshold for a 32 GB laptop), and account for the authorised backup verification
cluster during its run. Measure memory during backup and normal use before tuning.
Keep backup/identity freshness checks; a successful old laptop check is not server evidence.

### 15.3 Daily verified backup on the separate HDD at 18:45

Install the published Installer and both scheduling scripts on DESKTOP-SPF5420.
The old laptop's prepared configuration, cluster ID and DPAPI credential are not the
new server's configuration. Create these on the server:

- Installer: `C:SESS-BackupInstallerSESS.NexaERP.Installer.exe`.
- Config: `C:SESS-Backupackup.json`, database `sess_nexa_erp`, local host
  `127.0.0.1:5432`, **the new server's own verified cluster system identifier** from
  `pg_control_system()` / `pg_controldata`; never copy `7647792057875705176` from the laptop.
- `BackupRoot`: `D:SESS-Backups`; `WorkingRoot`: `E:SESS-Backup-Verification`
  (or the separate D: directory above). Roots must not overlap; initially empty/absent
  roots are enrolled by the tool. Do not initialise them over unrelated colleague files.
- Protected logs: `C:SESS-Backuplogs`; DPAPI file made by the scheduled Windows
  account **on this server**. Retain application configuration in the backup/recovery
  inventory; credentials are managed separately.

As that scheduled account, on DESKTOP-SPF5420:

```powershell
Read-Host 'Database backup connection string' -AsSecureString | Export-Clixml -LiteralPath 'C:SESS-Backupconnection.clixml'
$backupAccount = Get-Credential
.	oolsRegister-VerifiedDatabaseBackup.ps1 -InstallerPath 'C:SESS-BackupInstallerSESS.NexaERP.Installer.exe' -ConfigPath 'C:SESS-Backupackup.json' -CredentialFile 'C:SESS-Backupconnection.clixml' -LogDirectory 'C:SESS-Backuplogs' -DailyAt '18:45' -Credential $backupAccount
Start-ScheduledTask -TaskName 'SESS-NexaERP-VerifiedBackup'
```

Use a database principal able to dump the database/globals and read the control-system
identity, not the ERP runtime account. Check exit 0 in `logslast-run.json`, a VERIFIED
manifest under `D:SESS-Backups
un-<id>`, and restore/count/schema/security/hash evidence.
Test the scheduled run while signed out. Register the separate identity backup with
its own config, account credential, roots and task; avoid overlapping restore jobs.
Measure job duration on this HDD; the laptop's duration is not a server measurement.
The 18:45 schedule remains, but it is no longer a boundary before server development.

Each run dumps a consistent snapshot and globals, restores into a private cluster in
WorkingRoot, verifies it, records hashes and applies retention. This deliberately runs
on the production server as ERP recovery verification. Daily retention is 30 days;
Sunday UTC runs are retained 84 days; the newest two verified bundles are protected.
Budget roughly 30-42 full bundles **times the measured dump size**, plus failed-run
residue and identity bundles. The former "under 10 GB total at six months" estimate is
withdrawn: attachments can compress poorly. For example, 42 bundles at 3 GB need
126 GB; at 8 GB they need 336 GB, exceeding today's 284 GB free on D:. Trend measured
usage and extend/reallocate backup capacity before D: fills; do not silently shorten
retention. Reserve at least 20 GB free on D: beyond the retained-bundle budget and
about 25 GB free in the verification partition for the estimated 8 GB database
(roughly 2x database size plus margin). If WorkingRoot is on D:, budget both together.

The D: backup **does protect against failure of the C: SSD**, unlike the laptop's two
partitions on one SSD. It does not protect against whole-PC loss, theft or damage to
both disks. Copy the newest complete VERIFIED bundle plus required identity/config/file
backups to an independent external/offsite destination at least weekly. E: on this PC
is not that destination. Retain the ownership marker and manifests with recovery media;
see `automated-backup-and-recovery.md`. Daily backup leaves loss since the last usable
run; weekly offsite copies leave a longer possible gap if the whole PC is lost.

### 15.4 Build Option C directly on the final server and cut over

1. Preserve the laptop field backup, exports, attachment/configuration files and identity
   mapping evidence (steps 0-1); keep them intact through acceptance.
2. Apply the Windows 10 compensating controls in 15.1a; do not upgrade/reinstall Windows.
   Install the production dependencies on DESKTOP-SPF5420. Publish the release on the
   laptop. Use the server's SSD PostgreSQL cluster and create the fresh production
   database there (step 2); run provisioning/migrations against that target (steps 3-4).
   Do not restore the entire field database as the go-live database.
3. Complete steps 5-13 on the server: bootstrap identities, selectively carry attachments,
   import/configure masters, routes/QC/qualifications, and complete both authorised
   opening-stock ceremonies. No rehearsal transactions in the production companies.
4. Carry required external ERP files/configuration, configure identity and startup,
   reserve the server LAN address, and point client apps at it. Use the existing
   `CHANGE_SESS_NEXA_MASTER_IP.bat` / firewall setup where applicable. Check one user
   per company, permissions, readiness and reconciliations without polluting opening stock.
5. Complete step 14 and the server's first VERIFIED ERP and identity backups, including
   external-file recovery coverage. Record acceptance and prevent further writes to the
   laptop field database; keep its preserved source evidence.
6. Stop the laptop production ERP endpoints and retire its production-backup tasks only
   after server acceptance. Keep development services needed by the laptop and ensure
   production credentials/connections are not used by development processes.

Controlled ERP updates and database migrations remain permitted maintenance on the
server: prepare/test on the laptop, take a fresh verified backup, deploy in an agreed
window, apply the reviewed migration and reconcile principals, then verify readiness.
Even an urgent production defect is built/tested on the laptop, not on the server.

### 15.5 Laptop returns to development, LabVIEW and nightly witnesses

After section 15.4 acceptance, the production-hours rule no longer applies to the
laptop. Do not run the production-day closer there. Restore its NI/Siemens recorded
service/startup state with the existing script, elevated, on the laptop only:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .pc-maintenance-20260921RESTORE-NI-Siemens.ps1
```

This restores recorded settings, including services that were intentionally disabled;
it is not a blanket "start every NI service" command. Confirm the LabVIEW/chamber setup.
`NexaERP nightly witnesses` may stay scheduled on the laptop. There is no requirement
to disable it on 30 September or move it to the frontend developer's PC. Keep witness
failure review and isolated-cluster cleanup; coordinate resource-heavy runs with LabVIEW.
No witnesses run on DESKTOP-SPF5420, at any time.

### 15.6 Deployment evidence still to record

The selected hardware and layout are confirmed by the owner's report; this document
update has not installed software or created a database on the remote PC. Record the
actual server disk mapping, installed release, fresh cluster ID, service configuration,
security-update status, post-install free space, client checks and verified backup runs
at deployment. Old laptop disk/ACL measurements and prepared backup files are historical
and must not be applied to this server as though they had been inspected here.
