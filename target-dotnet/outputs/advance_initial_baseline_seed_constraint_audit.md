# AdvanceInitialBaseline seed-constraint audit

- Audit date: 2026-08-24
- Baseline identity: `20260824032638_AdvanceInitialBaseline`
- Parent commit: `16dcf5a61956350d690ceeb81bd27012d456e256`
Execution mode: offline design-time model and migration-operation inspection; zero connection opens and zero migration applications.

## Root cause and duplicate pairs

The baseline model combined `Rev866SeedData.RolePagePermissions` and `Rev869BSeedData.RolePagePermissions`. Eight REV869B rows reused REV866 `(RoleId, PageDefinitionId)` natural keys but used new primary-key IDs. The archived REV869B migration inserted those rows directly while `IX_role_page_permissions_RoleId_PageDefinitionId` already existed; it did not update or delete the REV866 rows first. Therefore this was not an insert-then-update history flattened into repeated inserts: the duplicate insert definitions were already present in the archived migration.

| Role | RoleId | Page | PageDefinitionId | Count | Sources | Final retained row |
|---|---|---|---|---:|---|---|
| ACCOUNTS_HEAD | `10000000-0000-0000-0000-000000000003` | purchase.po | `20000000-0000-0000-0000-000000000012` | 2 | REV866 + REV869B | REV869B `9d745645-4715-debc-7899-f8f307dea12e` |
| MANAGING_DIRECTOR | `03325f4f-c6d4-b3f3-f4b3-11b728c275da` | purchase.po | `20000000-0000-0000-0000-000000000012` | 2 | REV866 + REV869B | REV869B `4235c07c-e564-bbf7-e475-eda92f8f8a15` |
| MANAGING_DIRECTOR | `03325f4f-c6d4-b3f3-f4b3-11b728c275da` | purchase.rfq | `20000000-0000-0000-0000-000000000011` | 2 | REV866 + REV869B | REV869B `84b35820-207b-51a5-bf29-205365672b1d` |
| PURCHASE_EXECUTIVE | `46899b83-f5d7-793d-f008-5b15bcf06b17` | purchase.rfq | `20000000-0000-0000-0000-000000000011` | 2 | REV866 + REV869B | REV869B `d5caa783-5666-ecf5-aa06-6d2b302c30c7` |
| STORES_EXECUTIVE | `8481d263-cb63-6bc1-76ac-b4c2a56fc1c5` | purchase.po | `20000000-0000-0000-0000-000000000012` | 2 | REV866 + REV869B | REV869B `a915b601-7adc-9bdc-9f57-38f51929fd64` |
| TECHNICAL_DIRECTOR | `45eb9032-3689-8526-caee-41db0e7e2644` | purchase.po | `20000000-0000-0000-0000-000000000012` | 2 | REV866 + REV869B | REV869B `4e10adcf-6248-dceb-cb7c-5ce74abcec69` |
| TECHNICAL_DIRECTOR | `45eb9032-3689-8526-caee-41db0e7e2644` | purchase.rfq | `20000000-0000-0000-0000-000000000011` | 2 | REV866 + REV869B | REV869B `20a822a5-146b-b1ab-b849-5b19b42d053b` |
| TECHNICAL_ENGINEER | `80c408fe-3f95-ba8a-54b2-d0eee2374adf` | purchase.rfq | `20000000-0000-0000-0000-000000000011` | 2 | REV866 + REV869B | REV869B `7802b358-eab1-01d0-24b6-2ea8f479222a` |

`AdvanceSeedData` now produces one final row per natural key in historical order, retaining the later REV869B row and its final permission values for these eight overrides.

## Additional consolidation finding

The archived migrations dynamically inserted eight REV869A and three REV869B Department Manager permissions against a required pre-existing `DEPARTMENT_MANAGER` role. The fresh baseline neither seeded that role nor retained the eight REV869A dynamic rows. The consolidated model now seeds one deterministic Department Manager role (`30000000-0000-0000-0000-000000000005`) and all 11 final permission rows. The redundant immediate raw-SQL insert was removed, so every baseline seed is represented in EF model metadata and audited uniformly.

## Complete seeded-table constraint audit

No seeded entity defines an EF alternate key; all alternate-key checks therefore pass vacuously. The guard still enumerates alternate keys model-wide and will validate any added later.

| Table | Seed rows | Primary key | Unique indexes / constraints | Check constraints | Result |
|---|---:|---|---|---|---|
| `advance.audit_logs` | 6 | `Id` | none | none | PASS |
| `advance.departments` | 6 | `Id` | `Code` | none | PASS |
| `advance.designations` | 21 | `Id` | `Code` | none | PASS |
| `advance.employee_import_history` | 39 | `Id` | `ImportBatch, SourceEmployeeCode` | none | PASS |
| `advance.employee_role_assignments` | 40 | `Id` | `EmployeeId, RoleId, EffectiveFrom` | none | PASS |
| `advance.employee_skills` | 39 | `Id` | `EmployeeId, SkillId` | none | PASS |
| `advance.employee_status_history` | 39 | `Id` | none | none | PASS |
| `advance.employees` | 39 | `Id` | `EmployeeCode`; filtered `PayrollEmployeeId` where non-null | none | PASS |
| `advance.organization_policies` | 2 | `Id` | `OrganizationId, PolicyCode, EffectiveFrom, EffectiveTo` (`NULLS NOT DISTINCT`) | effective-to is null or not before effective-from | PASS |
| `advance.page_definitions` | 38 | `Id` | `PageKey` | none | PASS |
| `advance.purchase_transaction_approval_policies` | 3 | `Id` | `OrganizationId, RouteCode, EffectiveFrom, EffectiveTo` (`NULLS NOT DISTINCT`) | nonnegative/ordered amount range; effective date order | PASS |
| `advance.role_page_permissions` | 1,086 | `Id` | `RoleId, PageDefinitionId` | none | PASS |
| `advance.roles` | 43 | `Id` | `Code` | none | PASS |
| `advance.skills` | 6 | `Id` | `Code` | none | PASS |

All seeded foreign keys also resolve to another seed row or are null.

## Offline guard implementation

`AdvanceBaselineSeedConstraintTests` now:

1. Enumerates every EF primary key, alternate key, and unique index across the model, including PostgreSQL `NULLS NOT DISTINCT` metadata and every current filtered-index predicate form.
2. Reads the actual `InsertDataOperation` rows from the single generated baseline migration, verifies their counts match model seeds, and repeats every key/index duplicate check against those migration operations.
3. Enumerates every check constraint on a seeded table. Each constraint must have an exact-SQL offline validator; an unknown future constraint fails the guard until a validator is added.
4. Validates seeded foreign-key references.
5. Pins the exact eight historical source overlaps and asserts that each final row is the later REV869B value.
6. Pins the deterministic 11-row Department Manager consolidation.

## Regenerated offline SQL

- Artifact: `outputs/advance_initial_baseline_up.sql`
- Raw artifact: 1,429,174 bytes, 6,957 lines, SHA-256 `E3C8F32FA159CB269F32F11634572A280F734E61C7EA15BBEBB656F2C874C382`
- Canonical evidence: 1,423,460 bytes, 6,952 LF, SHA-256 `521A3877A426AAAC73F4B15642A75DB17EF0C9D7105F94EAC517454936B7DE23`
- Role-page inserts: 1,086; unique index retained and emitted after the seed inserts.
Connection opens: 0. Migration applications: 0.