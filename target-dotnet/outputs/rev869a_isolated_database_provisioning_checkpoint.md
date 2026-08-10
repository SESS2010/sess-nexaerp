# REV869A isolated database secure provisioning checkpoint

Date: 2026-08-10
Starting commit: `8fe9ade5747021d6a921a1666e8e9bc5c6ca50d6`
Checkpoint type: source-only; no helper or PostgreSQL execution

## Outcome

This checkpoint adds a future-use, fail-closed provisioning helper for creating the missing isolated database `sess_nexaerp_rev869a_verify` from the authoritative accepted source `sess_nexaerp_rev868_verify`. It does not create, inspect, alter, clone, restore, or drop any database. It does not apply or remove a migration.

The helper has four mutually exclusive modes:

1. `GeneratePlanOnly` prints the complete plan and returns before tool resolution, password prompting, PostgreSQL access, or filesystem mutation.
2. `SourcePreflightOnly` performs only read-only SQL and requires every source-readiness gate to pass.
3. `Provision` performs the guarded future backup/new-database/restore sequence only after two successful source preflights.
4. `PostProvisionVerification` performs read-only source/target comparison and cannot return acceptance from incomplete evidence.

## Fixed endpoint and database boundary

- Host: exactly `localhost`.
- Port: exactly `5432`.
- Source: exactly `sess_nexaerp_rev868_verify`.
- Target: exactly `sess_nexaerp_rev869a_verify`.
- Protected and rejected as caller-selected source or target: `sess_nexaerp`, `postgres`, `template0`, `template1`, REV861-like names, production/prod/live/main-like names, and every unexpected database.
- The source is queried only through SQL enclosed by `BEGIN TRANSACTION READ ONLY` and guarded against modifying statements.
- Provision must fail closed while the target exists. There is no overwrite path.

## Future provisioning sequence

After exact source preflight succeeds, `Provision` will:

1. Create a new, current custom-format `pg_dump` from the accepted REV868C3 source. The older pre-C3 backup is explicitly ineligible.
2. Store it only below `backups/postgresql/pre-rev869a-isolated`.
3. verify and record its resolved path, byte size, UTC creation time, and SHA-256.
4. Repeat source preflight immediately before creation so target absence and accepted source evidence are re-established.
5. Create the exact new target. The target must have been absent.
6. Restore only into that newly created target with `--no-owner` and `--no-privileges`.
7. Run read-only post-provision comparison and emit sanitized local evidence.

The helper contains no automatic cleanup or repair path. If anything fails after target creation, the target state becomes `QUARANTINED_DO_NOT_USE_OR_AUTO_REPAIR`; it is not automatically dropped, overwritten, or repaired. Unsafe restore options `--clean` and `--create` are rejected. No source modification is permitted.

## Password and evidence controls

The future runtime password is requested as a secure string only after plan mode has returned. Native tools receive authentication through temporary `PGPASSWORD` environment state, never command arguments, connection strings, evidence, or files. The state is removed in `finally`. Error and report output is sanitized, detailed provider error output is not enabled, and employee identity values are not emitted.

Runtime evidence is written outside committed reports under `local-evidence/rev869a-isolated-provisioning`. Backup evidence contains only the approved database identities, approved local backup path, byte size, creation time, SHA-256, and acceptance/quarantine state.

## Exact source-preflight acceptance

`safe_source_state=PASS` and `provisioning_readiness_state=PASS` require all of the following simultaneously:

- `current_database()` is exactly `sess_nexaerp_rev868_verify`.
- The exact target does not exist.
- The exact eleven accepted EF migration IDs exist once each.
- Missing, unexpected, and duplicate migration counts are all zero.
- Active SESS employee count is exactly 42.
- Relieved SESS employee count is exactly 9.
- Active accepted clean department count is exactly 12.
- Active `MANAGER` department-approval mapping count is exactly 14.
- Preservation counts are collected for employees, departments, department manager mappings, purchase requisitions, PR approval/status history, stock availability, reservations/history, pending handoffs, workflow route/steps, page/role permissions, audit logs, and employee status/department/approval/import histories.

No partial result is accepted. Target existence, any migration-set deviation, or any REV868C3 count deviation fails closed.

## Exact accepted migration set

1. `20260808110924_Phase1Foundation`
2. `20260808114550_Phase1AuthorizationSeed`
3. `20260808123411_Rev866EmployeePermissionMatrix`
4. `20260808142353_Rev866CorrectiveStatusPermissionAudit`
5. `20260808151207_Rev867MasterFoundation`
6. `20260808160435_Rev867C1Corrections`
7. `20260808182945_Rev868PurchaseRequisitionFoundation`
8. `20260808190920_Rev868PurchaseLocationAllocationCorrection`
9. `20260809123000_Rev868C2DepartmentManagerApprovalMapping`
10. `20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation`
11. `20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection`

REV869A itself is not part of this provisioning clone. This helper prepares the isolated pre-REV869A database only; the existing secure REV869A apply helper remains responsible for the later, separately authorized migration checkpoint.

## Post-provision acceptance

`provision_acceptance_state=PASS` requires:

- exact target database identity;
- exact source/target ordered migration fingerprint equality;
- exactly the same eleven migrations, once each, with no missing, unexpected, or duplicate rows;
- source/target equality for employee, department, manager-mapping, workflow, permission, history, and audit preservation counts;
- exact preservation equality for purchase requisitions, PR approval/status histories, stock checks, reservations and reservation histories, and pending RFQ handoffs.

Missing keys, count differences, migration differences, or database identity differences fail acceptance.

## Offline negative-test coverage

The focused tests cover wrong source/target, protected/main/REV861/production-like databases, wrong host/port, existing target, missing/unexpected/duplicate migrations, source-side modification SQL, unsafe dump/restore options, password or connection-string leakage, missing/invalid backup SHA-256 or size, absent restore isolation flags, automatic cleanup after failure, false acceptance from incomplete preservation evidence, exact REV868C3 counts, and PowerShell 5.1 source compatibility.

## Source-only schema-qualification correction

The observed `SourcePreflightOnly` undefined-relation failure was caused by unqualified application relations in `New-EvidenceSql`. This source-only correction makes the schema contract explicit without using `SET search_path`:

- EF migration history is read only as `public."__EFMigrationsHistory"`.
- Direct acceptance counts read `nexa.employees`, `nexa.departments`, and `nexa.department_approval_mappings`.
- Every preservation relation is mapped explicitly to its `nexa` identifier: employees, departments, manager mappings, purchase requisitions and their approval/status histories, stock availability checks/lines, reservations/history, requirement handoffs, approval routes/steps, page/role permissions, audit logs, and employee status/department/approval/import histories.
- Dynamic unqualified `%I` relation lookup was removed. Preservation count SQL is generated only from the fixed ordered source-owned `nexa.<relation>` map.
- System catalog target-existence lookup is explicitly `pg_catalog.pg_database`.

The exact eleven-migration set, zero missing/unexpected/duplicate requirements, 42 active employees, 9 relieved employees, 12 active clean departments, 14 active manager mappings, target absence, source/target preservation equality, and all fail-closed gates are unchanged.

Failure evidence now records and prints `provision_acceptance_state`, the failed phase, safe target state, and sanitized evidence-report path. Before-target failures preserve `target_state=NOT_CREATED_SAFE_RETRY_REQUIRES_NEW_PREFLIGHT`. The report path is printed before evidence persistence and the sanitized report redacts credential fields, employee/email fields, and PostgreSQL `DETAIL`, `CONTEXT`, or `STATEMENT` diagnostics. Target creation remains ordered strictly after successful source evidence validation; there is still no automatic cleanup, drop, or repair path.

## Source-only column-contract correction

The observed failure was in the `SOURCE_PREFLIGHT_ACCEPTANCE_AND_PRESERVATION` query, in the `counts` CTE expression that produces `active_manager_mapping_count`. It queried `nexa.department_approval_mappings` with `"RouteCode" = 'MANAGER'`. That column does not exist on this table.

The authoritative source contract is unambiguous and table-specific:

- `DepartmentApprovalMapping.ApprovalRouteCode`, its DbContext property/index/check-constraint mappings, the model snapshot, REV868C2 table creation, and REV868C3 indexes/seeds all map `nexa.department_approval_mappings."ApprovalRouteCode"`.
- `PurchaseApprovalWorkflowStep.RouteCode`, its DbContext mapping, the model snapshot, and the REV868C3 table/index/seed definitions map `nexa.purchase_approval_workflow_steps."RouteCode"`.
- The correction changes only the manager-mapping count predicate to `"ApprovalRouteCode" = 'MANAGER'`. It does not globally replace `RouteCode` and does not alter the workflow-step contract.

A new read-only schema-contract query now runs before acceptance/preservation SQL. It verifies all twenty `nexa` preservation relations, `public."__EFMigrationsHistory"`, and these direct column contracts:

- migration history: `MigrationId`;
- employees: `EmployeeCode`, `Status`;
- departments: `Code`, `IsActive`;
- department approval mappings: `ApprovalRouteCode`, `IsActive`;
- purchase approval workflow steps: `RouteCode`.

The target-absence lookup remains `pg_catalog.pg_database.datname`. Preservation queries use relation counts only. Metadata checks use `information_schema.tables` and `information_schema.columns`. Both schema-contract and acceptance builders remain enclosed in read-only transactions and contain no database-modification statement.

Failure evidence now identifies `failed_phase`, `failed_query_label`, SQLSTATE when PostgreSQL supplies it, identifier-safe schema/table/column metadata when supplied or derivable, safe target state, and sanitized evidence path. Raw native output is not thrown or persisted, so raw SQL, temporary SQL contents, credentials, connection strings, and employee PII are excluded. Undefined relation or column failures remain before target creation with `target_state=NOT_CREATED_SAFE_RETRY_REQUIRES_NEW_PREFLIGHT`.

The exact eleven accepted migrations, zero missing/unexpected/duplicate migrations, 42 active employees, 9 relieved employees, 12 active clean departments, 14 active manager mappings, target absence, preservation equality, and all existing fail-closed acceptance requirements are unchanged.

## Scope confirmation

Only the secure provisioning helper, its focused offline tests, and this report are included. The existing REV869A migration and application implementation are unchanged. No frontend or REV869B work is included. During this source-only correction turn, no helper mode was executed; no PostgreSQL, database, password, backup, restore, migration, production, AWS, `sess_nexaerp`, or REV861 operation occurred.

## Source-only validation evidence

- Windows PowerShell 5.1 parser: PASS, zero parse errors.
- Solution build: PASS, zero warnings and zero errors.
- Focused provisioning-helper tests: PASS, 32 passed, 0 failed, 0 skipped.
- Complete offline suite with PostgreSQL-named tests excluded: PASS, 285 passed, 0 failed, 0 skipped.
- Changed-file secret scan: PASS.
- Changed-file privacy scan: PASS.
- Changed-file safety scan: PASS.
- `git diff --check`: PASS.

## Future plan-only command

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\prepare-rev869a-isolated-database-secure.ps1 -GeneratePlanOnly
```
