# REV868 Rollback Readiness Review

## Scope

This is a source-only rollback review for REV868. No rollback, database query, PostgreSQL connection, restore, migration apply/remove, or live REV861 action was executed.

Reviewed migrations:

- 20260808182945_Rev868PurchaseRequisitionFoundation
- 20260808190920_Rev868PurchaseLocationAllocationCorrection

## Down Method Review

### 20260808190920_Rev868PurchaseLocationAllocationCorrection

The Down method reverses the additive correction by removing foreign keys first, then dropping the new purchase_number_sequences table, dropping location-level indexes/check constraints, removing new location and PR numbering columns, and restoring earlier nullable/default shapes and previous indexes.

Observed dependency order is broadly safe at schema level: FKs/indexes/check constraints are removed before dependent columns or tables are removed.

### 20260808182945_Rev868PurchaseRequisitionFoundation

The Down method drops REV868 purchase requisition foundation tables in dependency order: approval route settings, handoffs, PR histories/attachments, stock availability/reservation records, then PR lines and PR header tables.

Observed dependency order is broadly safe at schema level because child/dependent tables are dropped before parent tables where required.

## Irreversible/Data-Loss Risk

Rollback through EF Down would drop REV868 PR, stock-check, reservation, handoff, status-history, approval-history and attachment metadata tables/columns. If live development data exists in those objects, EF Down would remove that data.

Because a valid pre-REV868 backup does not exist, rollback cannot be treated as fully recoverable using pre-migration backup restore.

## Rollback Recommendation

Preferred recovery posture requires management decision:

1. If the goal is to preserve the current post-REV868 state, first create a clearly named post-REV868 safety baseline backup using 	ools/create-rev868-post-safety-backup-secure.ps1.
2. If the goal is to reverse REV868 schema, stop the API/application first, confirm no users are writing data, and review generated rollback SQL with management.
3. Use EF Down rollback only with explicit acceptance of data-loss risk for REV868 objects.
4. Backup restore should be used only from a verified backup whose point-in-time is clearly understood; no existing backup may be mislabeled as pre-REV868.

## API Stop Requirement

The API/runtime must be stopped before any rollback attempt. No rollback should occur while background jobs, users, or app processes can write to sess_nexaerp.

## Offline Rollback SQL

Generated source-only using design-only connection configuration (127.0.0.1:1, database sess_nexaerp_rev868_design_only, user design_only). EF completed SQL generation without PostgreSQL access.

- SQL path: $(C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\outputs\rev868_offline_rollback_to_rev867c1.sql.FullName)
- SQL bytes: $(C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\outputs\rev868_offline_rollback_to_rev867c1.sql.Length)

## Actions Not Executed

- No rollback was executed.
- No migration was applied, removed or recreated.
- No database was connected, queried or modified.
- No backup/restore command was run.
- Live REV861 was untouched.
- REV869 was not started.
