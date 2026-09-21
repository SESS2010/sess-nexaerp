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

## 15. Operating rule from 1 October: production by day, development by night

Decision, 21 September 2026: a separate server is not affordable yet, so this laptop
(HP ProBook 440 G5, i5-7200U 2 cores/4 threads, 32 GB, one 477 GB SATA SSD carrying both
C: and D:) is the production ERP server from 1 October. The ERP runtime itself is light
(measured 21 September: node + PostgreSQL 17 + MySQL 5.1 = 264 MB working set, near-idle
CPU); what pins the machine is development â€” builds, the full suites (1,018 tests, ~1â€“2 h
per configuration, Roslyn compiler server up to 6 GB), the nightly witnesses (4 h 25 m on
21 September) and the editor tooling (VS Code + Edge + Roslyn â‰ˆ 7 GB). Production and
development are therefore separated **in time**:

| Window | State | Allowed | Not allowed |
|---|---|---|---|
| **09:30 â€“ 18:30** | PRODUCTION ONLY | ERP running for eleven users; PostgreSQL; the verified backup at 18:45 (starts after the window) | VS Code, Claude Code, Codex; `dotnet build`/`test`/`publish`; any migration; witnesses; LabVIEW / NI services; anything that creates a PostgreSQL cluster |
| **18:30 â€“ 09:30** | DEVELOPMENT | builds, suites, migrations, witnesses â€” after the 18:45 backup has finished (`logs\last-run.json` ExitCode 0) | touching the production database from a development process; leaving a disposable cluster or a test host running past 09:30 |

Tools (committed with this section): `tools/Enter-ProductionDay.ps1`,
`tools/Test-ProductionState.ps1`, `tools/production-state.json`. Both scripts run as the
logged-in user, need no administrator rights, write their evidence to
`local-evidence/production-days/`, and exit 0 only when the machine is in production state.

### 15.1 Before 09:30 â€” close development (checklist)

1. Finish or abandon the current edit; commit and push what is committed-worthy. A
   migration that is executing is never interrupted: let `dotnet ef database update`
   finish, then run `database-principals provision` + `status` (step 4) before closing.
2. Run the closer from any PowerShell window (it closes VS Code gracefully first, then
   Claude Code, Codex, test hosts, the Roslyn/MSBuild compiler servers, the C# Dev Kit
   helpers; stops every disposable PostgreSQL cluster with `pg_ctl stop -m fast` and removes
   leftover `advance-postgresql-parser-*` / `nexa-installer-release-*` folders in `%TEMP%`;
   never touches the PostgreSQL service, the ERP's own node/dotnet processes or browsers):

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File tools\Enter-ProductionDay.ps1
   ```

   Preview first with `-DryRun`. It refuses (exit 2) while a migration is running.
3. Read the FAIL lines it prints (it ends by running `Test-ProductionState.ps1`). Exit 0
   means the production day is open. Anything else is fixed before users start, not after.
4. Leave the laptop plugged in, lid open, sleep and hibernation off, Wiâ€‘Fi/LAN as on the
   previous day. Do not sign out: the ERP master server runs in the interactive session
   (`SESS NexaERP Master Server` startup shortcut â†’ `node.exe --port=8783`).

### 15.2 Confirming production state

`tools/Test-ProductionState.ps1` (read-only; contract in `tools/production-state.json`)
checks, and records under `local-evidence/production-days/<date>-state-<time>.txt`:

| # | Check | Passes when |
|---|---|---|
| 1 | development processes closed | no VS Code, Claude Code, Codex, Roslyn/MSBuild servers, C# Dev Kit helpers |
| 2 | no test/build/migration running | no `testhost`, `VBCSCompiler`, `MSBuild`, `dotnet test/build/ef` |
| 3 | no disposable PostgreSQL clusters | every `postgres.exe` belongs to `C:\Program Files\PostgreSQL\17\data`; no cluster folders left in `%TEMP%` |
| 4 | service `postgresql-x64-17` | Running |
| 5 | ERP endpoints | node master server `http://127.0.0.1:8783/` answers (200/301/302/401/403); the API readiness URL is filled in at step 14 and then made `Required` |
| 6 | headroom | â‰¥ 12,000 MB available RAM, commit < 70 %, CPU (3 s) â‰¤ 60 %, C: â‰¥ 15 GB, D: â‰¥ 20 GB free |
| 7 | NI / LabVIEW services | none running (all 15 disabled on 21 September; `pc-maintenance-20260921\RESTORE-NI-Siemens.ps1` restores them for a chamber test) |
| 8 | nightly witness task | `Disabled` (section 0 cutoff); `-BeforeGoLive` tolerates `Ready` until 30 September |
| 9 | verified backup fresh | `C:\SESS-Backup\logs\last-run.json` ExitCode 0 and finished â‰¤ 26 h ago |

Run it again at any time during the day if the ERP feels slow; the FAIL line names the
cause. Free memory below the threshold with no development process present means a
production process is growing â€” capture with `pc-maintenance-20260921\CAPTURE-NOW.cmd`
and report; do not "fix" it by restarting PostgreSQL during the day.

### 15.3 Daily verified backup to D: at 18:45 (Item 26)

This laptop is the only copy of production. Findings of 21 September: none of the three
existing backup tasks produce anything â€” `NEXA_ERP_Daily_Backup` (23:00) writes to
`D:\CODEX\NEXA_ERP_BACKUPS\DAILY\`, which does not exist, and masks the `pg_dump` failure as
exit 0 (it also carries the `postgres` password in clear text in the task action);
`SESS NexaERP PostgreSQL Daily Backup` (23:30) and its rotation task point to script files
that no longer exist (result 0xFFFD0000). The newest dump on D: is from 16 July. The Item 26
verified backup (`automated-backup-and-recovery.md`) replaces them.

Prepared on 21 September (no task registered yet):

- Installer published to `C:\SESS-Backup\Installer\SESS.NexaERP.Installer.exe`.
- `C:\SESS-Backup\backup.json`: database `sess_nexa_erp` on `127.0.0.1:5432`, cluster
  system identifier `7647792057875705176` (from `pg_controldata`; unchanged by the fresh
  database of step 2 because the cluster is not re-initialised), `BackupRoot`
  `D:\SESS-Backups` (must not exist or be empty at first run), `WorkingRoot`
  `D:\SESS-Backup-Verification` (temporary verification clusters; D: has the space, C: does not).
- `C:\SESS-Backup\logs\` for the wrapper's per-run log and `last-run.json`.

Two commands remain for the account that will run the task (the DPAPI credential is bound to
that Windows account, and registration needs its password â€” neither is typed by anyone else):

```powershell
# 1. as the scheduled account, once: the backup connection string (postgres or a role that can dump all ERP data, globals and pg_control_system)
Read-Host 'Database backup connection string' -AsSecureString | Export-Clixml -LiteralPath 'C:\SESS-Backup\connection.clixml'
# 2. register the 18:45 task, then run it once by hand and read logs\last-run.json and D:\SESS-Backups\run-<id>\manifest.json (VERIFIED)
$backupAccount = Get-Credential   # the Windows account the task runs as
.\tools\Register-VerifiedDatabaseBackup.ps1 -InstallerPath 'C:\SESS-Backup\Installer\SESS.NexaERP.Installer.exe' -ConfigPath 'C:\SESS-Backup\backup.json' -CredentialFile 'C:\SESS-Backup\connection.clixml' -LogDirectory 'C:\SESS-Backup\logs' -DailyAt '18:45' -Credential $backupAccount
Start-ScheduledTask -TaskName 'SESS-NexaERP-VerifiedBackup'
```

What one run does: REPEATABLE READ snapshot â†’ custom-format `pg_dump` + globals (no
password hashes) â†’ restore into a fresh private cluster on D: â†’ table counts and
schema/security metadata compared â†’ SHA-256 recorded â†’ manifest `VERIFIED` â†’ retention.
Only a VERIFIED run counts; the 15.2 check reads the wrapper's `last-run.json`.

Retention (built into Item 26): daily bundles 30 days; runs starting on Sunday UTC are
weekly bundles kept 84 days; the newest two verified bundles are never deleted. Expected
footprint on D: at six months: â‰¤ 42 bundles Ã— (dump + globals), i.e. under 10 GB at the
growth estimate below, plus a transient 2 Ã— database size in `D:\SESS-Backup-Verification`
during each run.

Timing: 18:45 is after the production window closes and before development starts;
development waits for `last-run.json` (ExitCode 0). A run of the 526 MB cluster takes minutes,
not the four-hour limit the task allows.

**D: is the same physical disk as C:** (disk 0). The D: bundle protects against deletion,
a bad migration and database corruption; it does not protect against the SSD failing. Once a
week, copy the newest `D:\SESS-Backups\run-<id>` (dump, globals, manifest together) to a
device that leaves the building â€” the external archive disk already used on 19 September
(`E:\PC_Storage_Archive`) or a cloud folder â€” and note the copy in the go-live evidence. The
recovery procedure is `automated-backup-and-recovery.md` â†’ "If the laptop disk fails".

After the first VERIFIED run, the three dead tasks are unregistered (owner approval;
`Unregister-ScheduledTask` for `NEXA_ERP_Daily_Backup`, `SESS NexaERP PostgreSQL Daily
Backup`, `SESS NexaERP PostgreSQL Evidence Backup Rotation`) so that the Task Scheduler no
longer shows a "successful" backup that wrote nothing.

### 15.4 Development during the day: it waits

The answer to "can this build/test/migration run now?" between 09:30 and 18:30 is **no**;
it runs at 18:30 after the backup. The one exception is a production defect that stops the
eleven users from working and cannot wait for the evening. Then:

1. The Technical Director decides, and the decision is written first: what is broken,
   which users are stopped, why 18:30 is too late. One line in
   `local-evidence/production-days/<date>-exception.md`.
2. Users are told the ERP will be slow or paused, and when.
3. An ad-hoc verified backup runs first (`Start-ScheduledTask -TaskName 'SESS-NexaERP-VerifiedBackup'`,
   wait for `last-run.json` ExitCode 0) â€” a daytime change is made only on top of a fresh
   VERIFIED bundle.
4. The smallest possible action: a hot-fix build of the one project, or one migration,
   never a full suite or a witness. Targeted evidence is labelled targeted
   (`routine-tests-and-witness-gates.md`); the full suites run that night.
5. Then `Enter-ProductionDay.ps1` again, and the exception file gets the outcome, the
   commit SHA and the time production resumed.

Everything else â€” "just a quick build", a refactor, a test run to check something â€”
waits. A developer machine for daytime work is the frontend developer's PC (section 0,
"Separate witness machine"), not this laptop.

### 15.5 Nightly witnesses on this server

Section 0, "Production-server witness cutoff": the task `NexaERP nightly witnesses`
stays as it is through 30 September and is disabled before its 01:00 trigger on
1 October. Check 8 of 15.2 enforces that from 1 October. The first unattended run
(21 September 01:00â€“05:25) failed all three gates; its evidence is in
`local-evidence/nightly/2026-09-21/` and is reported separately â€” it does not change
the cutoff.

### 15.6 Planned step: the separate server (when payment allows)

Minimum specification (measured need, 21 September): 4 cores / 8 threads without thermal
limits (Core i5-12400 / Xeon E-2400 / Ryzen 5 PRO class), 16 GB RAM (32 GB ECC recommended),
500 GB NVMe (2 Ã— 1 TB NVMe RAID-1 recommended) plus a separate backup disk or NAS, 1 GbE
wired, on a UPS, Windows Server 2022 Standard or Windows 11 Pro, the ERP and PostgreSQL run
as services under service accounts. Business-tower minimum â‰ˆ â‚¹70â€“90 k; tower server with
ECC and RAID-1 â‰ˆ â‚¹1.8â€“2.6 lakh.

Migration path from this laptop, in order:

1. Evening window: verified backup runs; development stops on the laptop for the night.
2. New server: PostgreSQL 17 (same major), the Installer, the ERP publish; restore the
   newest VERIFIED bundle with `backup recover` (`automated-backup-and-recovery.md`, steps
   1â€“5; preserve the `postgres` bootstrap identity); `database-principals provision` +
   `status` â†’ VERIFIED; re-establish the three runtime passwords (globals omit them).
3. Copy the ERP's non-database files: `C:\Users\User\AppData\Local\SESS NexaERP` (server,
   app, runtime, secure â€” the node master server) and `D:\SESS_NEXA_ERP_DATA` (uploads,
   exports, departments, legacy-retained); MySQL 5.1 data only if the node server still
   reads it on that date.
4. Point the clients at the new master IP (`CHANGE_SESS_NEXA_MASTER_IP.bat` in the app
   folder), open the firewall rule (`ALLOW_SESS_NEXA_ERP_FIREWALL.bat`), sign in as SESS-12
   and one user per company, run the reports of step 14.
5. Register the verified backup on the server (its own DPAPI file and account), run it,
   read the manifest. Only then stop the laptop's ERP and unregister its backup task.
6. The laptop returns to development and LabVIEW: `pc-maintenance-20260921\RESTORE-NI-Siemens.ps1`
   (elevated) re-enables the NI services; the operating rule of this section ends.

### 15.7 Pending owner decisions recorded here

- `Everyone = Full Control` on `C:\` and `C:\Users` (non-default; report of 21 September):
  correction is `icacls C:\ /remove:g Everyone` and `icacls C:\Users /remove:g Everyone`
  then `icacls C:\Users /grant "Everyone:(OI)(CI)(RX)"`. It does not touch the PostgreSQL
  data directory, the ERP folder under the user profile, `ProgramData\MySQL` or anything on
  D: (all have their own protected ACLs). It changes the inherited ACE on the third-party
  folders at the root of C: (EBpro, Watlow, Angelantoni, WinKratos, ISRO_EProcClient,
  Siemens, Intel, chamber_calc, logs, temp) and on the IIS profile folders under
  `C:\Users`; `C:\Users\SESS` and `C:\Users\Public` carry their own explicit `Everyone
  Full` and are separate decisions. Not applied until approved.
- C: free space (31 GB, 16 %): reclaimable without loss â€” `hiberfil.sys` 12.8 GB
  (`powercfg /h off`, elevated; a server does not hibernate), `bin/` + `obj/` under
  `Documents\Codex` 8.2 GB (regenerated by the next build), `%TEMP%` 1.7 GB; movable to D:
  â€” `local-evidence/` 16.2 GB (retained TRX evidence: move, do not delete). Together â‰ˆ 39 GB.
- Production database growth, six months, fresh database of step 2 (estimate, no field
  volume yet): masters + configuration < 100 MB; 11 users Ã— ~200 governed actions/day Ã—
  audit receipts â‰ˆ 0.5â€“1 GB of rows and indexes; vendor/customer attachments are `bytea`
  rows in the database â€” at 20 attachments/day Ã— 1 MB â‰ˆ 2.5 GB; WAL steady state â‰¤ 1 GB
  (`max_wal_size`). Plan for **5â€“8 GB on C:** for the cluster at six months and **10 GB on
  D:** for the retained bundles; both fit after the reclaim above. Re-measure monthly with
  `SELECT pg_size_pretty(pg_database_size('sess_nexa_erp'))` and the D: bundle sizes.
