# Finding 4: canonical import categories

Stores/rack category codes ELE, FAB, FAS, MEC, PLC and REF remain canonical.
The legacy item workbook generator and checked-in import now map Electricals to
ELE, Fabrication to FAB, and Refrigeration (including the legacy misspelling
Refridgeration) to REF. The GRN three-character rule is unchanged. An unmapped
workbook department now refuses generation instead of producing a NULL category.

The checked-in import contains 1,368 item rows, distinct from the reported 1,388
items in the customer database. Existing import rows are skipped on ItemCode
conflict; rerunning that file alone cannot repair their category references.

## Existing imported rows

The separate database/postgresql/reconcile-legacy-item-categories.sql artifact
selects only EXCEL_IMPORT items attached to the three known long category codes.
It requires the canonical categories to exist and be active, changes only the
category reference, update metadata and version, and appends a GLOBAL audit entry
with before/after category and version. It preserves original category records,
item identities, all receipt snapshots and all historical QC evidence. Now-unused import-owned aliases are retained as inactive records, with a separate retirement audit, so they disappear from active category choices. Active non-import items or subcategories prevent this retirement and require explicit reconciliation. Replay
finds no further affected rows and writes no additional audit entries.

It refuses an active QC policy, vendor qualification or Stores category route
attached to a legacy category, or an affected imported item with a subcategory.
Those governed references require explicit reconciliation before applying it;
they are not silently moved or abandoned. A company still needs its effective
canonical-category Stores routes and approved QC criteria configured before GRN/QC.

This artifact has not been applied to the live database. It is not a replacement
for the oldest-field-state migration witness. The frozen remaining-findings working candidate passed full Debug 878/878 and Release 875/875, with zero failures/skips, after both solution builds succeeded with zero warnings/errors. All 1,013 recorded source hashes matched after testing. TRX evidence is under local-evidence/finding3. These are shared working-candidate results, not clean per-commit checkout runs; ordered commits and their exact staged builds remain pending. No owner database was changed.

Expected repair counts: N affected imported items and N item audit rows; A import-owned legacy aliases retired and A category audit rows (A=3 in the complete catalogue fixture). A replay changes zero items/categories and appends zero audits. Category IDs and historical snapshots remain unchanged.

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
