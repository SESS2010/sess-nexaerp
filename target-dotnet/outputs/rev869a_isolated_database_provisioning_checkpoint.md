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
- The exact nine-code relieved-employee set and accepted persisted statuses pass all expected/matched/missing/unexpected/duplicate/status-mismatch checks.
- Active accepted clean department count is exactly 12.
- Active `MANAGER` department-approval mapping count is exactly 14.
- Schema contract state is PASS.
- Preservation relation count is exactly 20 and preservation evidence state is PASS.
- Preservation counts are collected for employees, departments, department manager mappings, purchase requisitions, PR approval/status history, stock availability, reservations/history, pending handoffs, workflow route/steps, page/role permissions, audit logs, and employee status/department/approval/import histories.

Each canonical source label must be well formed and occur exactly once. No partial result is accepted. Target existence, any migration-set deviation, schema/preservation failure, malformed or duplicate evidence, or any REV868C3 count deviation fails closed.

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

## Source-only evidence-contract correction

The canonical SQL label was already emitted once with the correct name. It was not aliased or duplicated in the SQL. The defect was in `Convert-Evidence`: it assumed each PowerShell native-output object contained exactly one psql line. When psql output arrived as one CRLF-framed string, the parser found the first `=` and treated the entire payload as one value, so later labels—including `provisioning_readiness_state`—were not captured. The previous hashtable parser also overwrote duplicate labels instead of rejecting them.

The corrected parser splits every native-output object on CRLF or LF, ignores only normal `BEGIN`/`COMMIT` framing and blank lines, accepts only canonical safe `key=value` records, tracks each label's cardinality, removes duplicate values from acceptance, and records malformed-line count. PowerShell acceptance now requires every required label to be well formed, present exactly once, and equal to its exact expected value. Missing, duplicate, malformed, or explicit `FAIL` readiness evidence fails closed.

The single canonical label is:

`provisioning_readiness_state=PASS|FAIL`

It is computed directly from `all_source_conditions_pass`; it is not inferred from or aliased to `safe_source_state`. PASS requires all of these simultaneously:

- `database_identity=sess_nexaerp_rev868_verify`;
- `target_database_count=0`;
- `expected_migration_count=11`;
- `actual_matched_migration_count=11`;
- `missing_migration_count=0`;
- `unexpected_migration_count=0`;
- `duplicate_migration_count=0`;
- `active_employee_count=42`;
- `relieved_employee_expected_count=9`;
- `relieved_employee_actual_matched_count=9`;
- `relieved_employee_missing_count=0`;
- `relieved_employee_unexpected_count=0`;
- `relieved_employee_duplicate_count=0`;
- `relieved_employee_status_mismatch_count=0`;
- `relieved_employee_acceptance_state=PASS`;
- `active_clean_department_count=12`;
- `active_manager_mapping_count=14`;
- `schema_contract_state=PASS`;
- `preservation_relation_count=20`;
- `preservation_evidence_state=PASS`.

The source query still emits `safe_source_state` independently from the same complete boolean formula. The readiness label appears exactly once in the SQL result. Source PowerShell enforcement also requires both state labels exactly once and requires all count/identity/state labels above exactly once. No partial count set can produce accepted readiness.

Future sanitized failure evidence includes every safely parsed database identity, count, state, and preservation-count label returned before rejection, plus returned-evidence malformed count. Unsafe or malformed lines are not persisted. Raw SQL, temporary SQL contents, passwords, connection strings, migration SQL, and employee data remain excluded. Any evidence-contract failure retains `failed_phase=SOURCE_PREFLIGHT`, the precise query label, and `target_state=NOT_CREATED_SAFE_RETRY_REQUIRES_NEW_PREFLIGHT`; target creation remains after complete source assertion.
## Source-only relieved-employee exact-set correction

Correction starting commit: `776b8d9838d7c84159906247ebe58a7bdb96cd67`

The incorrect source-preflight predicate was:

`"EmployeeCode" like 'SESS-%' and lower("Status") = 'relieved'`

REV868C3 does not persist `Relieved`. Its migration writes `Left / Resigned`; the committed workbook decision records `LEFT / RESIGNED`; and the accepted read-only REV868C3 verifier normalizes with `lower("Status")` and accepts exactly `left / resigned`, `left/resigned`, `resigned`, or `inactive`. The accepted nine codes are `SESS-016`, `SESS-018`, `SESS-022`, `SESS-027`, `SESS-028`, `SESS-032`, `SESS-036`, `SESS-037`, and `SESS-039`.

The corrected source-preflight SQL uses fixed `relieved_expected` and `accepted_relieved_statuses` CTEs plus source rows from `nexa.employees`. It emits exactly:

- `relieved_employee_expected_count=9`;
- `relieved_employee_actual_matched_count=9`;
- `relieved_employee_missing_count=0`;
- `relieved_employee_unexpected_count=0`;
- `relieved_employee_duplicate_count=0`;
- `relieved_employee_status_mismatch_count=0`;
- `relieved_employee_acceptance_state=PASS|FAIL`.

PASS requires the fixed expected set to contain nine codes, nine correctly status-matched rows, and zero missing codes, accepted-status rows outside the expected set, duplicate expected-code rows, or expected-code status mismatches. It is not inferred from `51 - 42`. Both `all_source_conditions_pass` outputs - `safe_source_state` and `provisioning_readiness_state` explicitly require `relieved_employee_acceptance_state='PASS'`, and PowerShell independently requires every relieved evidence label exactly once with its canonical value.

All exact migration, database identity/absence, active employee, department, manager-mapping, schema-contract, preservation relation, source/target equality, and fail-closed target-ordering gates remain unchanged. The accepted REV868/REV868C3 source, migration, application model, and employee data are unchanged.

## Source-only post-provision evidence-reporting correction

Correction starting commit: `4148ea0c2ba4ba087e12e0c8e3fd6d1b500d9d80`

The standalone `PostProvisionVerification` branch previously validated source and target evidence, printed `provision_acceptance_state=PASS`, and returned. Unlike the `Provision` branch, it never called `Write-SanitizedEvidence`, so it could display PASS without creating or printing a new report path.

The corrected branch now builds a canonical sanitized report, validates its complete label set, creates a new timestamp-and-GUID evidence path, writes the report, re-reads and revalidates the persisted file, and only then prints:

- `post_provision_acceptance_state=PASS`;
- `provision_acceptance_state=PASS`;
- `sanitized_evidence_path=<exact path>`.

The report records execution mode and UTC timestamp; exact source and target identities; expected migration count; source and target matched/missing/unexpected/duplicate counts; exact accepted migration IDs; exact migration-set equality; source and target active employee, exact relieved-set, clean department, and manager-mapping evidence; source and target schema-contract states; source and target preservation-relation counts; and, for every one of the 20 preserved relations, source count, target count, and mismatch state. It finishes with preservation equality plus both acceptance states.

PASS requires every mandatory canonical label exactly once, the exact eleven-migration fingerprint on both databases, all established counts and states, all relieved-set gates, and exact equality for every preserved relation. The report validator rejects missing, duplicate, malformed, conflicting, incomplete, or unexpected labels. A report-write or persisted-file validation failure occurs before PASS output.

Standalone post-verification failure uses `failed_phase=POST_PROVISION_VERIFICATION`, retains the safe query label and sanitized SQLSTATE/schema/table/column metadata, emits both FAIL states, uses `target_state=EXISTING_TARGET_DO_NOT_AUTO_REPAIR_OR_DROP`, and attempts a separate sanitized failure report. No automatic drop, cleanup, repair, backup, restore, database creation, migration, or modifying SQL was added to this read-only mode. Existing `Provision` sequencing and evidence behavior remain unchanged.

## Scope confirmation

Only the secure provisioning helper, its focused offline tests, and this report are included. The existing REV869A migration and application implementation are unchanged. No frontend or REV869B work is included. During this source-only correction turn, no helper mode was executed; no PostgreSQL, database, password, backup, restore, migration, production, AWS, `sess_nexaerp`, or REV861 operation occurred.

## Source-only validation evidence

- Windows PowerShell 5.1 parser: PASS, zero parse errors.
- Solution build: PASS, zero warnings and zero errors.
- Focused provisioning-helper tests: PASS, 57 passed, 0 failed, 0 skipped.
- Complete offline suite with PostgreSQL-named tests excluded: PASS, 323 passed, 0 failed, 0 skipped.
- Changed-line secret scan: PASS.
- Changed-line privacy scan: PASS.
- Source-preflight SQL safety scan: PASS.
- Exact relieved-set/formula scan: PASS.
- Post-verification evidence-label/formula scan: PASS.
- Post-verification read-only/prohibited-operation scan: PASS.
- `git diff --check`: PASS.

## Future post-verification command

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\prepare-rev869a-isolated-database-secure.ps1 -PostProvisionVerification
```
## Future plan-only command

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\prepare-rev869a-isolated-database-secure.ps1 -GeneratePlanOnly
```
