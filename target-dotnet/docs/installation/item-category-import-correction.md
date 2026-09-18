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
