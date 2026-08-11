# REV869A isolated final acceptance checkpoint

Date: 2026-08-11

## Checkpoint identity

- Source commit: `d0ea8fc6af33c658c1e501d9a72ba3ddbc7f579d`
- Migration ID: `20260810120000_Rev869AIdentityMasterScopeFoundation`
- Isolated database: `sess_nexaerp_rev869a_verify`
- Verification boundary: source/file-only review of existing sanitized evidence
- Database/helper activity in this checkpoint: none

## Authoritative evidence and SHA-256

| Evidence | Exact path | Bytes | SHA-256 |
|---|---|---:|---|
| Isolated acceptance report | `local-evidence/rev869a/rev869a_isolated_execution_20260811_074353_165.md` | 44,378 | `DED5B300C491E4DBE2A137C0D61C15CDDD5DB1265221281C519D3DE1964A9503` |
| TRX | `local-evidence/rev869a/trx/20260811_074353_165/rev869a_resume_acceptance.trx` | 3,078 | `151D4EB53617F92F1251E719A909608875C91DC0680E22E9D95AB1ACACB88CC0` |
| Transactional output | `local-evidence/rev869a/trx/20260811_074353_165/rev869a_transactional_constraint_output.txt` | 1,633 | `544BEE6C48D50BE19B1929B4C2009CA1F0B5DF45CEFFBE9871062BAEBB32E446` |
| PostgreSQL test output | `local-evidence/rev869a/trx/20260811_074353_165/rev869a_postgresql_test_output.txt` | 1,273 | `D0B56F9BB07F3B2002C5D7134930B0F2ACAC81094A77B739C466390D3479D15B` |

The “report” hash above is the hash of the authoritative isolated acceptance report supplied for this checkpoint. A report cannot reliably embed its own SHA-256 because doing so changes its content.

## Migration and database acceptance

- Target migration occurrence count: **1** — PASS.
- Total migration count: **12** — PASS.
- Missing migration count: **0**.
- Unexpected migration count: **0**.
- Duplicate migration count: **0**.
- `database_schema_acceptance_state=PASS`.
- `database_preservation_acceptance_state=PASS`.
- `database_acceptance_state=PASS`.

The preflight section records `target_migration_count=0` before application, and the post-migration section records `target_migration_count=1`. These section-scoped values describe the intended before/after states and are not conflicting evidence.

## Active-employee and transactional acceptance

- Expected active employees: **42**.
- Actual matched active employees: **42**.
- Missing: **0**.
- Unexpected: **0**.
- Duplicate: **0**.
- Status mismatch: **0**.
- Active-employee exact-set state: **PASS**.

All seven transactional constraint states are PASS:

1. Identity
2. UOM
3. Tax
4. QC
5. Vendor
6. Warehouse/Rack-Bin
7. Controlled configuration history

- `transactional_constraint_test_state=PASS`.
- `transactional_rollback_state=PASS`.

## PostgreSQL-backed test acceptance

- TRX total: **1**.
- Passed: **1**.
- Failed: **0**.
- Skipped/not executed: **0**.
- `rev869a_postgresql_test_state=PASS`.
- `test_acceptance_state=PASS`.
- `overall_acceptance_state=PASS`.

## Evidence integrity

Every mandatory acceptance label occurs exactly once within its canonical evidence section and has the required value. The standalone transactional output matches the report’s transactional labels. The TRX counters and sanitized PostgreSQL test-output summary both prove 1 passed, 0 failed, and 0 skipped. No mandatory label is missing, duplicated, malformed, or conflicting.

## REV868 and REV868C3 preservation

REV868 and REV868C3 data and behavior are preserved. Exact pre-apply/post-migration equality is recorded for:

- Purchase requisitions: **7 / 7**.
- Purchase requisition approval history: **3 / 3**.
- Stock reservations: **4 / 4**.
- Active employees: **42 / 42**.
- Departments: **16 / 16**.
- Department approval mappings: **14 / 14**.

The authoritative report also records the exact nine relieved employees as matched, with zero missing, unexpected, duplicate, or status-mismatched rows.

## Boundary and exclusions

This checkpoint accepts REV869A only for the isolated verification database. Production deployment, production OIDC activation, and frontend implementation are not included. No production readiness or production acceptance is claimed.

The next approved boundary is **REV869B Purchase transaction foundation**. REV869B work is not started in this checkpoint.

This checkpoint performed no PostgreSQL access, helper execution, migration application/removal, backup/restore, database creation/drop/repair, production operation, REV861 work, frontend work, or REV869B implementation.
