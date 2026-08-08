# REV868 Isolated Restore Verification Plan

## Purpose

This plan is for future management-approved verification of a clearly named post-REV868 safety baseline backup. It must not be used to imply that a pre-REV868 backup exists.

## Protected Databases

The restore verification process must never target:

- `sess_nexaerp`
- `postgres`
- `template0`
- `template1`
- live REV861 or any production database

## Required Isolated Database Name

Use an explicit isolated database name, for example:

`session_nexaerp_rev868_restore_verify_morning` is not accepted because the required prefix is wrong.

Accepted naming pattern:

`sess_nexaerp_rev868_restore_verify_<management_suffix>`

## Future Management-Approved Steps

1. Management creates or explicitly authorizes creation of the isolated restore database.
2. Management runs the post-REV868 safety backup helper and records the backup path and SHA-256.
3. Management restores only that post-REV868 safety backup into the isolated database.
4. Verification confirms `current_database()` equals the isolated database name.
5. Verification compares migration history, table existence, constraints, indexes, foreign keys and safe framework/history counts.
6. The isolated database remains in place until management separately approves removal.

## Prohibited Overnight Actions

- No database creation was performed.
- No restore was performed.
- No `pg_restore` command was executed.
- No PostgreSQL connection was opened.
- No live REV861 action was performed.

## Helper

Source helper prepared for future management-controlled planning:

`tools/plan-rev868-isolated-restore-verification.ps1`
