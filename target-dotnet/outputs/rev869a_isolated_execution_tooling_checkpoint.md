# REV869A isolated execution tooling checkpoint

Date: 2026-08-10

Baseline commit: `0c139d3a14cd319cb7c0090f19b4f3a2bd05fedd`

Target migration: `20260810120000_Rev869AIdentityMasterScopeFoundation`

Scope: source-only isolated execution tooling. No helper mode was executed.

## Result

The secure future-execution helper is prepared with four explicit fail-closed modes: `GeneratePlanOnly`, `PreflightOnly`, full `Apply`, and `PostMigrationVerification`. The helper is permanently restricted to `sess_nexaerp_rev869a_verify` and cannot create, drop, restore, replace, or back up a database.

The accepted REV869A migration, designer, and model snapshot were not modified. No new material migration defect was found. The known measurement-dimension business-data blocker remains deliberately fail closed.

## Exact changed files

1. `tools/apply-rev869a-isolated-foundation-secure.ps1`
2. `tests/SESS.NexaERP.Tests/Rev869AIsolatedExecutionHelperTests.cs`
3. `outputs/rev869a_isolated_execution_tooling_checkpoint.md`

## Helper modes

### `-GeneratePlanOnly`

- Returns before password prompting, executable resolution, PostgreSQL access, or any EF database operation.
- Displays host, port, user, and exact target database.
- Displays the 11 prerequisite migrations in order and only the REV869A target migration.
- Displays all 9 foundation tables, 3 migration-owned backup tables, and 7 null-safe unique indexes.
- Displays preflight SQL and post-migration verification SQL as separate sections.
- Displays item/UOM and measurement-dimension readiness rules.
- Displays rollback design and exact-value backup comparison evidence.
- Explicitly states that no create/drop/restore/backup/main-database/REV861/production operation is permitted.

This mode was not executed in this checkpoint.

### `-PreflightOnly`

- Validates the exact target before requesting one isolated-database password.
- Executes the preflight SQL inside `BEGIN TRANSACTION READ ONLY` and rolls it back.
- Requires exactly the accepted 11 prerequisite migrations once each, total migration count 11, and REV869A count zero.
- Requires all 9 foundation and 3 backup relations to be absent.
- Requires REV869A columns, indexes, constraints, functions, triggers, seed rows, and other partial artifacts to be absent.
- Checks deterministic role/page seed collisions and the future RackBin alternate key.
- Reports future unique/effective overlap state only as zero when every future relation is absent; any partial relation fails safe retry.
- Reports `unmapped_item_count`, invalid UOM references, exact item/UOM evidence count, and unclassified referenced measurement-dimension count.
- Sets `safe_retry_state=PASS` only when migration and partial-artifact/collision evidence is exact.
- Sets `data_readiness_state=PASS` only when item mappings and measurement-dimension classifications are exact.
- Sets `preflight_acceptance_state=PASS` only when both states pass.
- Contains no cleanup, repair, update, delete, alter, or insert SQL.

### `-Apply`

- Cannot reach EF apply unless all three preflight states equal `PASS`.
- Requires an existing approved pre-REV869A backup path and matching SHA-256 evidence; it does not create a backup.
- Requires the target workspace to be clean while ignoring unrelated paths outside the target workspace.
- Sets process-only secrets and expected-database guards.
- Invokes only `dotnet ef database update 20260810120000_Rev869AIdentityMasterScopeFoundation`.
- Sanitizes EF, psql, and test failure evidence; connection strings and passwords are redacted.
- Runs transaction-rolled-back negative tests for duplicate OIDC identity, invalid UOM conversion, invalid state GST, missing QC owner, invalid vendor qualification dates, cross-warehouse RackBin, and configuration-history mutation.
- Runs the focused REV869A tests plus the six REV868C1 PostgreSQL workflow tests against the isolated target.
- Compares pre/post PR, approval-history, reservation, employee, department, and manager-mapping counts exactly.
- Clears password and connection environment variables in `finally`.

### `-PostMigrationVerification`

- Performs read-only post-schema/evidence inspection against the exact isolated database.
- Does not claim overall acceptance by itself because it lacks the apply-mode preflight preservation baseline and transactional test evidence.

## Exact protected databases and names

- `sess_nexaerp`
- `sess_nexaerp_rev868_verify`
- `postgres`
- `template0`
- `template1`
- every `REV861`-like name
- every production/prod/live/main-like name
- every database not exactly `sess_nexaerp_rev869a_verify`

Exact case-sensitive target equality is enforced before any password prompt or database access.

## Preflight evidence contract

The helper reports database identity, session user/host/port, migration totals, prerequisite discrepancies, REV869A count, all partial relation/column/index/constraint/function/trigger/seed counts, seed collisions, RackBin-key duplicates, item/UOM readiness, measurement-dimension readiness, and preservation baseline counts.

No UOM, item, dimension, seed, schema, migration, or business row is automatically repaired or fabricated.

## Post-migration evidence contract

The helper requires and reports:

- migration count 12 and REV869A present exactly once;
- 9 foundation tables and 3 backup tables;
- 149 foundation columns with per-table total/nullability shape checks;
- full actual column contracts with datatype and nullability;
- 9 PKs, 15 restrictive FKs, 22 checks, composite Warehouse/RackBin integrity, and actual constraint definitions;
- all actual index definitions and exactly 7 `NULLS NOT DISTINCT` indexes;
- mandatory `items.BaseUomId` only after an exact management-approved item mapping; the current migration assignment cannot be accepted while the contract is PENDING;
- state-aware GST consistency;
- database configuration guard triggers;
- exactly 81 migration-owned seeds: 5 roles, 8 pages, 66 permissions, 2 policies;
- exact role/page/permission/policy seed sets and zero all-false Department Manager permissions anywhere;
- zero legacy-column differences between current items/UOMs/vendors and the three pre-change backup tables;
- exact REV868/REV868C3 pre/post preservation counts;
- schema PASS plus exact pre/post preservation equality before `database_acceptance_state=PASS`; PostgreSQL test PASS is additionally required before overall PASS.

Identity fallback, direct record-scope denial, missing/non-`AVAILABLE` reservation denial, and no email/name/employee-code linking are covered by the focused source tests that full apply is configured to run. The helper never treats UI hiding as authorization.

## Rollback design evidence

- Down contains exactly 81 migration-owned seed deletions.
- Post-verification compares exact pre-existing item/UOM/vendor JSON values, excluding only REV869A-added columns, against migration-owned backups.
- REV869A does not rewrite pre-existing columns; Down removes only REV869A additions and therefore restores the exact prior row shape while preserving legacy values.
- REV868/REV868C3 PR, approval, reservation, employee, department, mapping, history, and audit records are outside the removal boundary.
- `rev869a_vendors_prechange_backup`, `rev869a_uoms_prechange_backup`, and `rev869a_items_prechange_backup` are dropped last.

## Offline validation evidence

- Windows PowerShell 5.1 parser: PASS, zero parse errors.
- Build: PASS, zero warnings and zero errors.
- Focused helper tests: 10 passed, 0 failed, 0 skipped.
- All non-PostgreSQL tests: 262 passed, 0 failed, 0 skipped.
- EF `migrations list --no-connect`: PASS; exact 11 prerequisites followed by REV869A.
- EF model/snapshot check: PASS; no pending model changes.
- Offline Up SQL: 79,970 bytes, one transaction, 12 table creates, 7 null-safe unique indexes.
- Offline Down SQL: 9,313 bytes, 12 table drops and exactly 81 seed deletions.
- Secret/privacy/protected-database scan: PASS.
- Helper safety scan: PASS; no `createdb`, `dropdb`, `pg_restore`, `pg_dump`, migration remove, arbitrary migration target, or database replacement command.
- Accepted migration diff: zero; migration/designer/snapshot unchanged.
- `git diff --check`: PASS.

## Remaining UOM and business decisions

1. The current pre-REV869A schema has no approved measurement-dimension classification field or mapping source. The helper therefore counts every referenced UOM without approved classification as unclassified and fails `data_readiness_state`.
2. Management/data owners must approve an exact UOM-code-to-measurement-dimension mapping source before isolated apply. The helper will not infer dimensions from names/codes or assign a shared default.
3. Any item with null `UomId` requires an exact approved item/UOM correction. No default UOM is allowed.
4. Until these counts are zero, full isolated apply is intentionally unreachable.
5. Production OIDC mapping and real OIDC testing remain a separate production blocker.

## Boundary confirmation

No PostgreSQL access occurred. No helper mode was executed. No migration was applied, removed, or modified. No database was created, dropped, restored, backed up, repaired, or cleaned. No protected database, REV861, production, frontend, REV869B, or legacy ZIP/source was accessed or changed.

Git also reported an unrelated untracked `../legacy-reference/` directory outside the target workspace. It was not read, modified, staged, or removed.
## Source-only UOM readiness and preservation correction

This section supersedes the earlier UOM-readiness counting rule. No helper mode or database operation was executed for this correction.

### UOM management contract and state

The source contract is intentionally empty and has `ApprovalStatus = PENDING`. Its future approved UOM rows require stable `UomId`, `UomCode`, `MeasurementDimension`, `QuantityPrecision`, `IsCanonicalBase`, `ConversionPolicy`, `ManagementApprovalReference`, and `ApprovalStatus`. Exact item mappings require `ItemId`, `BaseUomId`, `MappingStatus`, `MappingBasis = MANAGEMENT_APPROVED`, and the same approval reference.

Legacy `m`, `kg`, and ambiguous `no` remain candidate-only. They do not populate the contract. There is no guessed, default, inferred or automatic classification/BaseUom acceptance.

Preflight emits read-only candidates:

- UOM ID, code, name, explicit `symbol=NOT_MODELED`, active state and item-reference count;
- duplicate/ambiguous normalized code/name counts;
- exact item IDs/codes having null or invalid `UomId`;
- every item's BaseUom mapping status;
- the management decisions still required.

### Correct readiness formula

The former `unclassified_measurement_dimension_count = distinct referenced UomId count` rule is removed.

`data_readiness_state = PASS` only when:

`uom_management_decision_state = APPROVED`
and null item Uom IDs = 0
and invalid item Uom references = 0
and missing expected classifications = 0
and unexpected expected classifications = 0
and duplicate expected classifications = 0
and unapproved/invalid classifications = 0
and missing BaseUom mappings = 0
and invalid BaseUom mappings = 0
and inferred/default mappings = 0.

While management state is PENDING, both data readiness and preflight acceptance are FAIL. Safe retry additionally requires exact relieved-employee acceptance; it does not override business readiness.

### Exact migrations, seeds and backups

Pre-apply requires exactly the 11 prerequisite migrations once each, no unexpected or duplicate migration, and REV869A absent. Final schema acceptance requires exactly the expected set of 12 migrations once each, including REV869A, with zero missing, unexpected or duplicate rows.

Final seed acceptance requires exactly 5 REV869A roles, 8 pages, 66 permission role/page pairs and 2 policies, with the exact stable role/page/policy natural-key sets, zero unexpected/duplicate/missing pairs, and no all-false Department Manager permission anywhere.

Before mutation, all three altered tables—items, UOMs and vendors—must have complete migration-owned backup coverage. Post-verification requires zero missing/extra backup IDs, equal row coverage and exact equality of every pre-existing value after excluding only REV869A-added columns. Down keeps the three backup tables until last.

### Correct preservation and database acceptance formula

For each of these keys, the post count must equal the captured preflight count exactly:

- purchase requisitions;
- purchase-requisition approval histories;
- stock reservations;
- active employees;
- the exact nine-code relieved-employee metrics and accepted-status state;
- departments;
- department approval mappings.

`database_schema_acceptance_state = PASS` covers exact migrations/schema/seeds/backups. It is insufficient by itself.

`database_preservation_acceptance_state = PASS` is added only after every pre/post equality succeeds.

`database_acceptance_state = PASS` is emitted only after both schema acceptance and preservation equality pass. Standalone post-verification has no baseline and therefore reports database/preservation acceptance as NOT_CLAIMED. Overall acceptance still additionally requires PostgreSQL-backed tests, which were not run here.

### Offline negative coverage

Focused source tests now fail closed for missing, unexpected, duplicate and unapproved UOM classifications; null/invalid item Uom IDs; missing BaseUom mapping; inferred/default mapping; preservation mismatch; protected databases; non-read-only preflight SQL; and any GeneratePlanOnly path reaching password, PostgreSQL, EF apply or other database action.
### Source-only relieved-employee preservation correction

Correction starting commit: `9c93ec8522a03b3c25e1dd599c0233d83a76a9f7`.

Both preflight and post-migration preservation SQL incorrectly counted `nexa.employees where "Status"='Relieved'`. REV868C3 instead persists `Left / Resigned`. The committed workbook data and accepted verifier establish exactly these codes: `SESS-016`, `SESS-018`, `SESS-022`, `SESS-027`, `SESS-028`, `SESS-032`, `SESS-036`, `SESS-037`, and `SESS-039`; accepted normalized statuses are `left / resigned`, `left/resigned`, `resigned`, and `inactive`.

A shared source-owned CTE contract is now embedded into both SQL builders. Each emits expected, actual matched, missing, unexpected, duplicate and status-mismatch counts plus `relieved_employee_acceptance_state`. PASS requires expected and matched counts of nine with every negative count zero. It is not inferred from total minus active employees.

`safe_retry_state` and `preflight_acceptance_state` now require `relieved_employee_acceptance_state=PASS`. Post-migration `database_schema_acceptance_state`, the explicit PowerShell post gate, and pre/post preservation equality require the same exact-set evidence. Existing migration, artifact, collision, seed, schema, backup, permission and preservation gates remain intact.

The preflight `pg_indexes` artifact predicate is now explicitly `schemaname='nexa' AND (...)`, so every `OR` branch remains constrained to the `nexa` schema without changing the intended artifact set.

The UOM management decision remains `PENDING`. `UomClassifications` and `ItemBaseUomMappings` remain empty, and their SQL expected sets remain empty `WHERE false` contracts. No guessed, default, inferred or automatic classification/BaseUom mapping was introduced. Read-only candidate evidence remains available while data and preflight acceptance remain FAIL pending approval.

### Correction validation evidence

- Windows PowerShell 5.1 parser: PASS, zero errors.
- Build: PASS, zero warnings and zero errors.
- Focused helper tests: PASS, 32 passed, 0 failed, 0 skipped.
- Complete offline suite with PostgreSQL-named tests excluded: PASS, 330 passed, 0 failed, 0 skipped.
- Migration source changes: zero.
- Acceptance-formula scan: PASS.
- UOM/artifact/safety scan: PASS.
- Changed-line secret and privacy scans: PASS.
- `git diff --check`: PASS.
- No helper mode, PostgreSQL test, database, migration, backup/restore or production operation was executed.
### Source-only UUID aggregation correction

Correction starting commit: `1f0cb3da543612a2b1a9c15317005307a8f097fb`.

The expected-UOM classification comparison used `min("UomId")` to manufacture one representative value per duplicate normalized-code group. PostgreSQL does not define `min(uuid)`, so the otherwise read-only preflight stopped before it could emit readiness evidence.

The duplicate formula is now type-safe and additive:

```sql
(
  (select count(*)
   from (
     select "UomId"
     from expected_uom_classifications
     group by "UomId"
     having count(*) <> 1
   ) duplicate_uom_ids)
  +
  (select count(*)
   from (
     select upper(trim("UomCode")) as normalized_uom_code
     from expected_uom_classifications
     group by upper(trim("UomCode"))
     having count(*) <> 1
   ) duplicate_uom_codes)
) as duplicate_uom_classification_count
```

The first subquery counts duplicate UUID-key groups without aggregating UUID values. The second counts duplicate normalized-code groups. Addition means either defect contributes to failure and simultaneous defects cannot cancel. An empty expected classification contract yields `0 + 0`. No UUID-to-text aggregate workaround is used.

A complete source scan of every SQL builder in this helper found no remaining `min(...)` or `max(...)` aggregate over UUID identifier columns. Offline tests cover the generated SQL contract, both duplicate dimensions, additive behavior, the empty-set result and fail-closed readiness for either duplicate type.

All existing gates remain unchanged: exactly 11 prerequisites; the exact nine relieved employees; artifact, collision, backup, seed, schema, permission and preservation checks; SELECT-only preflight; protected-database checks; and no automatic cleanup or repair. `uom_management_decision_state` remains `PENDING`; both approved UOM classifications and approved Item/BaseUom mappings remain empty; and `data_readiness_state` cannot pass until management approval. No guessed, default, inferred or automatic UOM mapping was added.

### UUID correction validation evidence

- Windows PowerShell 5.1 parser: PASS, zero errors.
- Build: PASS, zero warnings and zero errors.
- Focused isolated-execution helper tests: PASS, 36 passed, 0 failed, 0 skipped.
- Complete offline suite with all three PostgreSQL-backed verification classes excluded: PASS, 329 passed, 0 failed, 0 skipped.
- PostgreSQL SQL-contract scan: PASS; zero UUID `min`/`max` aggregate matches.
- Acceptance-formula and UOM-state scan: PASS.
- Changed-line secret, privacy and safety scans: PASS.
- `git diff --check`: PASS.
- No helper mode, PostgreSQL test, PostgreSQL/database access, migration, backup/restore, production, REV861, frontend or REV869B operation was executed.
