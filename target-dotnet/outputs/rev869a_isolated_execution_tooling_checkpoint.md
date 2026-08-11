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
- exactly 88 migration-owned seeds: 4 new roles, 8 pages, 74 permissions, and 2 policies; the single suitable active pre-existing `DEPARTMENT_MANAGER` role is reused and remains legacy-owned;
- exact role/page/permission/policy seed sets and zero all-false Department Manager permissions anywhere;
- zero legacy-column differences between current items/UOMs/vendors and the three pre-change backup tables;
- exact REV868/REV868C3 pre/post preservation counts;
- schema PASS plus exact pre/post preservation equality before `database_acceptance_state=PASS`; PostgreSQL test PASS is additionally required before overall PASS.

Identity fallback, direct record-scope denial, missing/non-`AVAILABLE` reservation denial, and no email/name/employee-code linking are covered by the focused source tests that full apply is configured to run. The helper never treats UI hiding as authorization.

## Rollback design evidence

- Down contains exactly 88 migration-owned seed deletions and does not delete or modify the reused Department Manager role.
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
- Offline Down SQL: offline regenerated SQL reviewed with 12 table drops and exactly 88 owned deletions: 4 roles, 8 pages, 74 permissions, and 2 policies.
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

Final seed acceptance requires exactly 4 REV869A-created roles, 1 unchanged reused Department Manager role, 8 pages, 74 REV869A-owned permission role/page pairs, and 2 policies. The exact stable role/page/policy natural-key sets must have zero unexpected, duplicate, or missing pairs and no all-false Department Manager permission anywhere.

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
### Source-only physical-relation contract correction

Correction starting commit: `984efe1eb90cdb6566022abeb2415683cf100f73`.

The authoritative physical name is `nexa.purchase_requisition_approval_history`. It is confirmed by `NexaErpDbContext`, the current model snapshot, accepted REV868 migrations and verification tooling, and the isolated provisioning helper preservation map. Accepted provisioning evidence records the source/target count as 3. The execution helper had invented the plural `nexa.purchase_requisition_approval_histories` in exactly two places. Both the preflight and post-migration preservation queries now directly count the singular relation.

The generated-SQL audit covers all three builders: preflight, post-migration verification, and transaction-rolled-back constraint verification. Their complete canonical 25-relation union is:

- `controlled_configuration_histories`, `department_approval_mappings`, `departments`, `employee_identity_mappings`, `employees`
- `items`, `organization_policies`, `page_definitions`, `purchase_requisition_approval_history`, `purchase_requisitions`
- `qc_inspection_policies`, `rack_bins`, `rev869a_items_prechange_backup`, `rev869a_uoms_prechange_backup`, `rev869a_vendors_prechange_backup`
- `role_page_permissions`, `roles`, `stock_reservations`, `tax_gst_settings`, `uom_conversions`
- `uoms`, `vendor_qualifications`, `vendors`, `warehouse_condition_locations`, `warehouses`

Every non-backup relation is confirmed by the current DbContext and snapshot; each migration-owned backup relation is confirmed by the REV869A migration source. Unknown or invented relation names fail the offline exact-set contract.

The provisioning/EF/snapshot audit separately confirms all 20 accepted preservation relations: `employees`, `departments`, `department_approval_mappings`, `purchase_requisitions`, `purchase_requisition_approval_history`, `purchase_requisition_status_history`, `stock_availability_checks`, `stock_availability_check_lines`, `stock_reservations`, `stock_reservation_history`, `purchase_requirement_handoffs`, `purchase_approval_route_settings`, `purchase_approval_workflow_steps`, `page_definitions`, `role_page_permissions`, `audit_logs`, `employee_status_history`, `employee_department_history`, `employee_approval_history`, and `employee_import_history`.

Both required approval-history preservation counts remain unconditional direct relation reads. There is no guessed alternate name, dynamic fallback, or `information_schema`/`to_regclass` silent skip for this required evidence. Preflight and post-migration verification remain SELECT-only and fail closed.

All prior gates remain unchanged: exactly 11 prerequisite migrations; exact nine relieved-employee comparison; UOM management state `PENDING`; empty approved UOM classification and Item/BaseUom sets; data readiness FAIL pending approval; safe-retry artifact/collision checks; and all preservation and acceptance formulas.

### Physical-relation correction validation evidence

- Windows PowerShell 5.1 parser: PASS, zero errors.
- Build: PASS, zero warnings and zero errors.
- Focused isolated-execution helper tests: PASS, 40 passed, 0 failed, 0 skipped.
- Complete offline suite with all three PostgreSQL-backed verification classes excluded: PASS, 333 passed, 0 failed, 0 skipped.
- Physical-relation contract scan: PASS; 25 exact generated-SQL relations and 20 accepted preservation relations confirmed.
- PostgreSQL SQL-contract scan: PASS.
- Acceptance-formula scan: PASS.
- Changed-line secret, privacy and safety scans: PASS.
- `git diff --check`: PASS.
- No helper mode, PostgreSQL test, PostgreSQL/database access, migration, backup/restore, production, REV861, AWS, frontend or REV869B operation was executed.

## Final source-only role-reuse/UOM evidence checkpoint

Starting commit: `0691a0d31c6d17a99df1e9a211eecf08dc7cbeb9`.

- Colliding code: `DEPARTMENT_MANAGER`, proven by the accepted REV868C3 reconciliation migration's role-code upsert, permission rows, and employee assignments.
- Reuse gate: exactly one normalized role, exactly one active role, zero duplicates, exactly one suitable legacy-owned identity, and zero collisions among PURCHASE_MANAGER, STORES_MANAGER, QC_MANAGER, and QC_INSPECTOR.
- Preservation: no insert/update/delete of the reused role; exact modeled-value fingerprint equality; existing assignments and permissions unchanged.
- New permission boundary: eight REV869A-owned Department Manager rows on the eight new pages, with exact view/print/download/audit-history rights and no mutation/export/commercial/full-control rights.
- Exact ownership: 4 roles + 8 pages + 74 permissions + 2 policies = 88. Down removes only those 88 owned rows and drops backup tables last.
- UOM evidence: all master rows and safe unmapped-item fields are reported read-only; zero-master management decision is explicit; approval remains PENDING and both approved expected sets remain empty.

Final offline validation:

- Windows PowerShell 5.1 parse: PASS, 0 errors.
- Build: PASS, 0 warnings, 0 errors.
- Focused REV869A tests: 64 passed, 0 failed, 0 skipped.
- Complete non-PostgreSQL tests: 332 passed, 0 failed, 0 skipped.
- EF migration discovery with `--no-connect`: PASS, exact 11 prerequisites plus unchanged target migration.
- Pending-model-change check: PASS, no model changes after the migration.
- Offline Up SQL: 82,328 bytes, one transaction, one dynamic reused-role permission insert, all 8 deterministic permission IDs present.
- Offline Down SQL: 9,539 bytes, one transaction, 81 SQL delete statements effecting exactly 88 owned-row deletions; the ownership-qualified Department Manager delete is present and backup tables drop last.
- Relation/schema/acceptance scan: PASS.
- Secret/privacy/safety scan: PASS.
- `git diff --check`: PASS.

No helper mode or PostgreSQL/database/migration-apply/backup/production/REV861/REV869B action occurred. `../legacy-reference/` remained read-only, unchanged, and untracked.
## 2026-08-10 management-approved EA UOM implementation addendum

Starting source commit: `ff27fe632e22bd0eef2742e08e6bfb251062f5dc`.

Management approval `MGMT-REV869A-UOM-20260810-001` replaces the former pending/empty UOM decision. The deterministic migration-owned EA identifier is `f71a4725-bb15-e7bf-e97b-991985e96328` (derived with the repository's SHA-256-to-Guid convention from `rev869a-uom|EA`). The exact approved contract is EA / Each / COUNT / precision 0 / canonical base / identity-only conversion / CREATE / APPROVED. `IsCanonicalBase` and `ConversionPolicy` are not physical columns in the current `uoms` model, so they are preserved—together with the approval reference and item mapping—in the immutable controlled-configuration history row `0007efa3-4888-a87d-45ef-72cc55f4dd45`.

Preflight now evaluates a creation/backfill plan, not post-migration state. It requires one exact UOM contract, one exact item mapping, no ID/code/name collision, the exact approved current item (`8c428e59-db05-471d-a7e7-4f7dc1c13b54`, `REV868C1-ITEM`) with null `UomId`, and no additional uncovered null-UOM item. Thus raw `unmapped_item_count=1` is permitted only when the exact plan covers that same row; missing, extra, duplicate, invalid, defaulted, inferred, or guessed mappings fail closed.

Up creates backups first, adds the REV869A columns, inserts exactly one migration-owned EA UOM, updates exactly the approved Item's `UomId` and `BaseUomId`, creates the remaining objects in FK-safe order, and writes one immutable approval-history row. Post-verification separately requires the 88 security/configuration rows, one EA row, one updated Item, one UOM history row, zero collisions/mismatches, and exact backup equality for every row/field outside the approved delta.

Ownership reconciliation: 4 roles + 8 pages + 74 permissions + 2 policies = 88 security/configuration inserts; plus 1 EA UOM and 1 controlled history = 90 inserted migration-owned rows; exactly 1 existing Item is updated. Down restores that Item's exact backed-up `UomId=NULL`, deletes exactly the owned history and EA rows after proving no other Item references EA, removes the existing 88 owned security/configuration rows, removes the added `BaseUomId` column through the existing Down path, and drops the three backup tables last.

Source-only validation: Windows PowerShell 5.1 parse PASS; build PASS with 0 warnings and 0 errors; focused REV869A tests 59/59 PASS; complete non-PostgreSQL tests 344/344 PASS; EF discovery PASS with 12 migrations and REV869A exactly once; pending-model check PASS; offline Up/Down generation PASS (Up 85,381 bytes, one EA insert and one Item update; Down 12,077 bytes, one EA delete and one Item restore; all three backup tables drop last); acceptance/relation/safety scans and `git diff --check` PASS.

No helper mode, PostgreSQL/database access, migration application/removal, backup/restore, production, `sess_nexaerp`, `sess_nexaerp_rev868_verify`, REV861, AWS, frontend, or REV869B action occurred. `../legacy-reference/` remained read-only, unchanged, and untracked.
## 2026-08-10 post-migration catalogue-character cast correction

Starting commit: `ca4d2509a2ae7e3abfde74942d335f787750586d`.

The source-only audit confirmed the failure was confined to the `constraint_contract` evidence projection. PostgreSQL exposes `pg_constraint.contype` as internal type `"char"`; direct concatenation with text is not supported. The verifier now emits the constraint type through the explicit expression `c.contype::text`. The full helper catalogue audit found no other internal `"char"` field used in output concatenation. `confdeltype` remains only in an equality predicate, where no cast is required.

All preflight, schema, ownership, preservation, relieved-employee, Department Manager, UOM, Item mapping, backup, and acceptance formulas remain unchanged. The post-migration SQL remains SELECT-only/read-only. No apply, migration, rollback, restore, backup, create/drop database, cleanup, or repair action was added.

Source-only validation: Windows PowerShell 5.1 parse PASS; build PASS with 0 warnings and 0 errors; focused isolated-execution-helper tests 51/51 PASS; complete non-PostgreSQL tests 346/346 PASS; catalogue-character scan PASS; read-only/prohibited-operation scan PASS; secret/privacy/safety scans PASS; `git diff --check` PASS.

No helper mode, PostgreSQL/database access, migration application/removal/rollback, backup/restore, production, REV861, frontend, AWS, or REV869B action occurred. The existing REV869A migration and model snapshot were unchanged. `../legacy-reference/` remained read-only, unchanged, and untracked.
## 2026-08-10 exact seed-set verifier correction

Starting commit: `1deb0ea03283401cb2471acdfe7d91fd75a5b00e`.

Root cause of `seed_set_mismatch_count=32`: the former permission-unexpected component used a broad, case-sensitive role-code allow-list. Four reused REV866 roles are physically persisted lowercase (`purchase_executive`, `stores_executive`, `technical_director`, and `managing_director`), while that allow-list contained uppercase values. All eight REV869A page permissions for each of those four roles were falsely classified as unexpected: 4 roles x 8 pages = 32. The actual 74 permission rows were not defective.

The verifier now defines exact source-derived contracts for 4 roles, 8 pages, 2 policies, and all 74 permissions. Each permission contract contains its deterministic permission ID, normalized logical role used to resolve the exact current RoleId, exact PageDefinitionId, and all 20 permission flags. This includes the eight existing-role `DEPARTMENT_MANAGER` permissions with view/print/download/audit-history only. The broad role-code allow-list and fragile distinct role/page count were removed.

`seed_set_mismatch_count` is now the sum of 12 independently emitted and enforced metrics: role unexpected/missing, page unexpected/missing, policy unexpected/missing, permission unexpected/missing, permission flag mismatch, permission RoleId mismatch, permission PageDefinitionId mismatch, and owned role/page duplicate count. `database_schema_acceptance_state` still requires exact counts 4/8/74/2, total 88, every independent mismatch count zero, and all existing schema, preservation, Department Manager, UOM, Item, history, and backup gates to pass.

Offline negative coverage proves wrong RoleId, wrong PageDefinitionId, one changed flag, missing permission, unexpected permission, duplicate permission, wrong `CreatedBy`, and wrong policy value each produce a positive seed mismatch and database acceptance failure.

Source-only validation: Windows PowerShell 5.1 parse PASS; build PASS with 0 warnings and 0 errors; focused helper tests 62/62 PASS; complete non-PostgreSQL tests 357/357 PASS; exact 74-record/20-flag contract scan PASS; read-only/prohibited-operation scan PASS; secret/privacy/safety scans PASS; `git diff --check` PASS.

No helper mode, PostgreSQL/database access, migration application/removal/rollback, restore, repair, backup, database-data change, production, REV861, frontend, AWS, or REV869B action occurred. The applied REV869A migration source and model snapshot were unchanged. `../legacy-reference/` remained read-only, unchanged, and untracked.

## 2026-08-10 final post-apply acceptance resume checkpoint

Starting source commit: `772d6b2148b9b7e30f317211fbf65aaf2a0e7598`.

The earlier `REV869A transactional prerequisites unavailable` failure was caused by a verifier-only assumption, not an applied-schema defect. The accepted pre-apply evidence proves zero UOM rows before REV869A, and the accepted post-apply contract creates exactly one EA UOM. The old transactional SQL nevertheless required a second distinct UOM (`u2`), so the prerequisite guard failed before any constraint assertion ran. It also unnecessarily required a second warehouse.

The rolled-back verifier now creates its own collision-guarded temporary UOM inside the transaction and uses it to test the zero conversion-factor check with distinct UOM IDs. It uses a collision-guarded nonexistent warehouse ID to test the Rack/Bin composite foreign key. Every test write remains between explicit `begin` and `rollback`; no second business UOM or warehouse is required and no test data persists.

The new `ResumePostApplyAcceptance` mode is restricted to `sess_nexaerp_rev869a_verify`. It requires a SHA-256-pinned approved pre-apply evidence report under `local-evidence/rev869a`, exactly one canonical preflight evidence section, the exact target/migration headers, pre-apply target migration count zero, and preflight acceptance PASS. Numeric and state labels must each occur exactly once and be well formed.

Before tests, resume mode performs the existing read-only post-migration verification and requires exactly 12 migrations, REV869A exactly once, and `database_schema_acceptance_state=PASS`. It then requires exact pre/post equality for purchase requisitions, purchase requisition approval history, stock reservations, active employees, departments, and department approval mappings. Only then may it emit `database_preservation_acceptance_state=PASS` and `database_acceptance_state=PASS`.

The resume test boundary contains no EF migration update/remove, backup, restore, database create/drop, cleanup, repair, or main-database logic. It runs only the transaction-rolled-back REV869A constraint SQL and the exact `Rev869APostgresAcceptanceTests` class. That class is connection-free during ordinary offline runs and, when explicitly enabled by the helper, verifies the exact target, disables detailed errors, opens a read-only rolled-back transaction, and checks the 12/1 migration contract plus preservation counts 7/3/4/42/16/14. Test output and a unique TRX path are sanitized into the evidence report on success or failure. Overall PASS is emitted only after database and test acceptance both pass.

The approved pre-apply report for future review is `rev869a_isolated_execution_20260810_225308.md`, with SHA-256 `078816A6C3D2A05C0E114B29597BF82C9D7585B840862197553476A51EB25485`.

Source-only validation: Windows PowerShell 5.1 parse PASS; build PASS with 0 warnings and 0 errors; focused helper tests 67/67 PASS; complete non-PostgreSQL tests 362/362 PASS. Safety, privacy, secret, prohibited-operation and diff checks are recorded in the committed checkpoint result.

No helper mode, PostgreSQL/database access, migration application/removal/rollback, backup/restore, database create/drop/repair, production, `sess_nexaerp`, `sess_nexaerp_rev868_verify`, REV861, AWS, frontend, or REV869B action occurred. The applied migration and model snapshot were unchanged, and `../legacy-reference/` remained read-only, unchanged, and untracked.

## 2026-08-11 remaining transactional-prerequisite correction

Starting commit: `4399905e1101cf36e3bd5be9343d20b63786502b`.

Failed evidence: `local-evidence/rev869a/rev869a_isolated_execution_20260811_064224_060.md`. It proves schema acceptance, preservation acceptance, and database acceptance PASS, followed by a failure at the old combined prerequisite guard before any transactional constraint result. The guard required five values: an active/login-enabled employee (`e`), any UOM (`u1`), an arbitrary first warehouse (`w1`), a Rack/Bin belonging to that arbitrary warehouse (`rb`), and any vendor (`v`).

The exact missing prerequisite was `v`: an existing vendor row. The accepted evidence proves 42 active employees and the post-REV869A EA UOM. The committed REV868C1 accepted setup creates the single controlled warehouse and its Rack/Bin used by the seven preserved PRs, but creates no vendor; no accepted migration seeds vendor business data. The generic guard therefore failed on an optional Vendor Master dependency. The arbitrary warehouse/RackBin selection was also fragile and has been removed even though it was not the observed missing row.

The transactional verifier now emits individual safe count/state labels. It requires the already-proven exact active employee count only for a non-mutating FK reference, reports the existing vendor count as informational and explicitly `NOT_REQUIRED_TEST_OWNED`, and reports independent identity, UOM, tax, QC, vendor, warehouse/RackBin, and history collision counts. Missing or colliding prerequisites raise `transactional_prerequisite_failed=<exact_name>|expected_count=<n>|actual_count=<n>` instead of the former generic error.

Each of the seven negative tests has a distinct DO block and PASS label. Disposable UOMs, QC UOM, vendor, two warehouses, and Rack/Bin are created with reserved UUID/code collision checks inside the same explicit transaction, before their dependent negative test. Existing employee and business-master rows are never updated or deleted. The SQL contains exactly one outer `begin`, exactly one final `rollback`, and no `commit`; a failed psql session also rolls back its open transaction. Schema, seed, EA UOM, Item mapping, permissions, and all preservation formulas are unchanged.

Resume enforcement now parses every transactional prerequisite and constraint label exactly once before invoking the exact REV869A PostgreSQL-backed test class. Missing, duplicate, malformed, conflicting, or FAIL evidence stops before .NET PostgreSQL tests. Sanitized transactional output is written on success or failure; sanitized .NET output and TRX evidence are recorded whenever the .NET stage is reached. Database/test/overall states remain fail closed, and overall PASS still requires schema, preservation, transactional, and PostgreSQL-backed test acceptance.

Source-only validation: Windows PowerShell 5.1 parse PASS; build PASS with 0 warnings and 0 errors; focused helper tests 83/83 PASS; complete non-PostgreSQL tests 378/378 PASS. SQL-contract, prohibited-operation, secret/privacy/safety, exact-file-boundary, and `git diff --check` results are recorded in the final committed checkpoint verification.

No helper mode or PostgreSQL test was executed. No PostgreSQL/database access, migration application/removal/modification, backup/restore, database create/drop/repair, production, `sess_nexaerp`, `sess_nexaerp_rev868_verify`, REV861, AWS, frontend, or REV869B action occurred. Final REV869A acceptance is not claimed.

## 2026-08-11 transactional rendered-SQL and active-employee correction

Starting commit: `7efbc5c3986152fc72282f7f84c5b18828a88509`.

Failed evidence: `local-evidence/rev869a/rev869a_isolated_execution_20260811_070247_413.md` and its sanitized transactional output. The source-only diagnosis identified two verifier defects. First, the transactional SQL was held in an expandable PowerShell here-string and its DO openings used incomplete dollar-delimiter escaping. The final text sent to psql therefore contained the invalid `do $identity` instead of a paired PostgreSQL `$tag$` delimiter. All seven independent DO blocks now use unique paired `$rev869a_<test>$` delimiters inside a literal here-string, preventing PowerShell interpolation. Offline tests extract and validate that final literal SQL text.

Second, the identity constraint test selected employees with `Status='Active' AND LoginEnabled=true`. That is not the accepted REV868C3 active-employee contract: the REV868C3 migration inserts the approved active set with `LoginEnabled=false` and does not change existing login enablement on conflict. Login eligibility is not required to test the employee foreign key on an identity mapping. The verifier now uses the exact 42 employee codes defined by committed `Rev868C3EmployeeWorkbookData.ActiveEmployees`, requires normalized status `active`, and emits independent expected, matched, missing, unexpected, duplicate, and status-mismatch counts. PASS requires exactly `42/42/0/0/0/0`. The identity negative test selects only from this proven set and does not require a pre-existing identity mapping or enabled login.

All seven negative constraint tests remain independent, collision guarded, test-owned where support rows are needed, and enclosed by one explicit transaction with one final rollback and no commit. Missing or malformed prerequisite/constraint evidence remains fail closed and prevents the PostgreSQL-backed .NET stage. Existing schema, seed, EA UOM, Item mapping, permission, preservation, database-acceptance, test-acceptance, and overall-acceptance formulas are unchanged.

Source-only validation: Windows PowerShell 5.1 parse PASS; build PASS with 0 warnings and 0 errors; focused helper tests 90/90 PASS; complete non-PostgreSQL tests 402/402 PASS. Final SQL delimiter, prerequisite formula, prohibited-operation, secret/privacy/safety, exact-file-boundary, and `git diff --check` scans are required to remain PASS at commit.

No helper mode or PostgreSQL test was executed. No PostgreSQL/database access, migration application/removal/modification, backup/restore, database create/drop/repair, production, `sess_nexaerp`, `sess_nexaerp_rev868_verify`, REV861, AWS, frontend, or REV869B action occurred. Final REV869A acceptance is not claimed.
