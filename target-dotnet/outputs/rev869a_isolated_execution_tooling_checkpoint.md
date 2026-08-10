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
- mandatory `items.BaseUomId`, exact `BaseUomId = UomId` backfill, and zero mismatches;
- state-aware GST consistency;
- database configuration guard triggers;
- exactly 81 migration-owned seeds: 5 roles, 8 pages, 66 permissions, 2 policies;
- every migration-owned seed ID and zero all-false REV869A Department Manager permissions;
- zero legacy-column differences between current items/UOMs/vendors and the three pre-change backup tables;
- exact REV868/REV868C3 pre/post preservation counts;
- `database_acceptance_state=PASS`, `test_acceptance_state=PASS`, and `overall_acceptance_state=PASS` before full acceptance is reported.

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
