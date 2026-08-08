# REV868 Missing Pre-Migration Backup Incident

## Summary

REV868 migrations were applied to the `sess_nexaerp` .NET development database without a verified pre-REV868 PostgreSQL backup.

Applied REV868 migration IDs reported by management:

- `20260808182945_Rev868PurchaseRequisitionFoundation`
- `20260808190920_Rev868PurchaseLocationAllocationCorrection`

This incident was discovered only after migration application, during post-run verification preparation.

## Root Cause

The original REV868 application helper, `tools/apply-rev868-secure.ps1`, contained preflight checks and EF migration application logic, but it did not contain mandatory `pg_dump` backup creation logic before migration application.

Because the helper lacked backup creation, no `target-dotnet/backups/postgresql/pre-rev868/` custom-format backup and SHA-256 evidence was produced before REV868 was applied.

## Evidence

Existing PostgreSQL backup folders identified before this report:

- `backups/postgresql/pre-rev866/`
- `backups/postgresql/post-rev866-pre-correction/`
- `backups/postgresql/pre-rev867/`

No valid `pre-rev868` backup was found.

## Integrity Rules

- A new backup created after REV868 application must never be represented as a pre-REV868 backup.
- The existing pre-REV867 backup must not be represented as a pre-REV868 backup.
- A post-REV868 safety baseline may be created only with clear post-migration naming.
- No false, retroactive or misleading rollback evidence is permitted.

## Impact

REV868 may still be technically valid, but final REV868 acceptance requires management-approved compensating controls because the expected pre-migration rollback backup evidence does not exist.

## Required Compensating Controls

Management must explicitly approve the recovery posture before final REV868 acceptance. Safe options include:

1. Create a clearly named post-REV868 safety baseline backup for future recovery.
2. Run read-only post-run evidence collection to verify the actual schema state.
3. Restore the future post-REV868 safety backup into an isolated verification database for backup integrity testing.
4. Document that rollback to pre-REV868 cannot rely on a true pre-REV868 backup.
5. If rollback is required, stop the API first and use either EF Down migration review or a management-approved restore strategy.

## Actions Not Performed Overnight

- No database connection was opened.
- No password was requested or stored.
- No `pg_dump`, `pg_restore`, `psql`, `createdb` or EF database update command was run.
- No database was modified.
- Live REV861 was not touched.
- REV869 was not started.
