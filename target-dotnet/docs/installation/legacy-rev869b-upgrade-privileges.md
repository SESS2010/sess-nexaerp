# Legacy REV869B upgrade privileges

Status: **correction under verification; not deployment clearance.** The real pre-75 field backup with REV869B installed has been requested. The available `before_grid_defects.dump` contains 65 migration entries but records `WasInstalled=false`; its matching globals contain no REV869B roles. It cannot witness the reported upgrade defect. The earlier 78-to-103 witness never executed migration 75.

## What failed and what changes

Migration 75, `20260911125548_ConvergeOrdinaryOwnerExplicitAcl`, runs `REASSIGN OWNED` for all nine retired roles when `WasInstalled=true`. PostgreSQL checks transfer authority even if a source role owns no objects. Ordinary provisioning grants the migration login SET access to `nexa_erp_owner`, not authority over retired roles.

A disposable PostgreSQL 17 probe confirmed:

- Reassignment from an empty retired role still fails without membership.
- The unprivileged owner cannot grant itself the missing membership.
- An administrator can grant membership to the executing owner, switch to the migration/owner identity for reassignment, reset to administrator and revoke membership in one transaction. This proves the PostgreSQL mechanism, not permission for the migration to elevate itself.

The correction introduces one shared catalog check used by:

| Entry | Direction | Previous operation |
|---|---|---|
| 50 — `20260906203000_RetireRev869BOrdinaryDeployment` | Up | Reassign each of nine retired roles |
| 55 — `20260907123000_ConvergeAppliedRev869BRetirementOwnership` | Up and Down | Repeat the ownership repair |
| 75 — `20260911125548_ConvergeOrdinaryOwnerExplicitAcl` | Up and Down | Repeat ownership repair, then explicit owner ACL grants |

For each role, the check looks for work actually performed by PostgreSQL 17 `REASSIGN OWNED` in the current database and shared objects: ownership dependencies except default ACLs/user mappings, plus initial extension ACL references. PostgreSQL deliberately ignores default ACLs and user mappings. The implementation is checked against [PostgreSQL 17 shdepReassignOwned](https://raw.githubusercontent.com/postgres/postgres/REL_17_STABLE/src/backend/catalog/pg_shdepend.c). If none exist, there is no ownership work and no reassignment is attempted. If dependencies remain, both source and target SET authority are required; otherwise the migration refuses with SQLSTATE 42501 and an administrator-preparation message. With authority it performs the original transfer.

This does not clear retirement history, remove roles, manufacture membership, mark a migration applied, or suppress a real transfer failure. Complete/partial installation checks and explicit ACL grants remain unchanged. Already-applied history is not replayed. Regenerate pending scripts from the corrected code; an older generated SQL file retains the defect.

## Owner-ACL prerequisite and authorized repair

The new regression starts with the existing migration-75 ownership/ACL defect fixture, then uses real Installer provisioning and the migration/owner identity. It exposed a false green: provisioning and status succeeded even though reassignment preserved revoked owner schema USAGE. Migration 75 then cannot read its retirement-state precondition. This is distinct from the empty-role reassignment defect.

On 16 September the owner explicitly authorized exactly the four migration-75 grants during administrator-only Installer provisioning: USAGE/CREATE on advance, ALL on its tables, ALL on its sequences, and ALL on its functions. Those grants are now implemented. Status additionally checks schema, table, sequence and routine privileges for nexa_erp_owner. No database-level grants, runtime/bootstrap/migration-login privileges, or memberships are added by this repair.

The regression covers initially revoked and initially usable owner ACLs. It separately removes schema USAGE, schema CREATE, table SELECT, table REFERENCES, sequence privileges and function EXECUTE. Each removal must make status refuse and an actual operation under the migration/owner identity fail. Provisioning must then repair it and status must pass. The owner creates a foreign key, inserts a valid reference and receives a foreign-key violation for a missing parent. Probe table and row changes are rolled back in the disposable fixture; status itself stays read-only.

## Change boundary

Expected business-row changes: **zero**. No new migration-history entry or frontend API contract is added. Existing unapplied migrations 50, 55 and 75 generate the corrected reassignment check; already-applied migration history is not rewritten. The Installer repair restores only the four authorized owner grants and tightens read-only verification. Existing database-level ACL handling and runtime/bootstrap/migration-login grants and memberships are unchanged.

## Normal upgrade procedure after the corrected chain is witnessed

1. Stop the API and scheduled writers. Take and verify both a custom-format database backup and matching globals backup.
2. Inspect migration history, retirement state, all nine retired roles, role memberships and residual ownership. Preserve the exact starting state for the witness.
3. Using the administrator Installer connection, run the existing `database-principals provision` and `database-principals status` commands from [developer runtime principals](developer-runtime-principals.md). This is the existing administrator ownership/ACL preparation, not an elevated migration. Do not grant the migration login CREATEROLE, superuser, or retired-role membership.
4. Run the reviewed pending chain authenticated as `nexa_erp_migration` with `Options=-c role=nexa_erp_owner`. If residual ownership is refused, stop. The ordinary Installer reconciles application objects; unexpected shared or non-application objects need administrator investigation, not a blind cluster-wide transfer.
5. With the API still stopped, run administrator `database-principals provision` followed by `status` again. Require RECONCILED and VERIFIED before starting the API.

The required acceptance witness is the actual `sess_nexa_erp-pre75-2026-09-16.dump` and its matching private globals backup, restored into a fresh isolated cluster with database name `sess_nexa_erp`. The owner reports a 2.98 MB dump, 229 tables, `WasInstalled=true`, nine REV869B roles, and 14 roles in the globals backup. Inspect the restored migration history to establish the exact starting migration; neither the filename nor the earlier synthetic fixtures establish it. Upgrade the reviewed pending chain under the migration/owner identity. A fresh schema or a synthetic retired-role fixture is regression coverage, not this field witness. The live database was reported at migration 106 on 18 September; it is not the witness target and must not be changed by this exercise.

## Owner-approved immediate exception for the frontend developer

This is a bounded workaround for the uncorrected migration, not the normal upgrade design. Migration 75 needs transfer authority over retired roles. The owner approved applying that one migration as `postgres`.

Keep the API stopped and preserve backups. Check actual history after any failed run; do not assume a failed batch applied nothing before 75.

Set the connection secret securely in the current process, following the existing installation runbook. The commands below deliberately specify the stopping migration.

```powershell
# Existing secure environment: migration login, Options=-c role=nexa_erp_owner.
# Only if the actual history has not yet reached 74:
dotnet ef database update 20260911104631_AlignGrnQcDueAtWithReceiptTime --project .\src\SESS.NexaERP.Infrastructure --startup-project .\src\SESS.NexaERP.Api --context NexaErpDbContext --configuration Release

# Switch ConnectionStrings__NexaErp to the administrator connection for the SAME database.
# NexaErp__ExpectedDatabase must still identify that database.
dotnet ef database update 20260911125548_ConvergeOrdinaryOwnerExplicitAcl --project .\src\SESS.NexaERP.Infrastructure --startup-project .\src\SESS.NexaERP.Api --context NexaErpDbContext --configuration Release

# Use the administrator Installer connection for both:
dotnet run --project .\src\SESS.NexaERP.Installer -c Release -- database-principals provision
dotnet run --project .\src\SESS.NexaERP.Installer -c Release -- database-principals status

# Switch ConnectionStrings__NexaErp BACK to migration login + owner SET role.
dotnet ef database update 20260915103000_MachineDeliveryDossier --project .\src\SESS.NexaERP.Infrastructure --startup-project .\src\SESS.NexaERP.Api --context NexaErpDbContext --configuration Release

# Repeat administrator Installer provision, then status, before starting the API.
```

Do not use an unbounded `database update` while the migration connection is administrative. Do not edit `__EFMigrationsHistory`. If a different migration refuses, report that failure rather than extending this exception.

## Privilege sweep

The historical generated 0-to-103 forward script contained **103 migration-history entries** and the three reassignment consumers above. Source review also covers SQL helpers, both directions, and the two later A1 migrations.

| Operation | Finding / required authority |
|---|---|
| REASSIGN OWNED | Entries 50, 55, 75; corrected as described above |
| DROP OWNED; CREATE/ALTER/DROP ROLE or USER | None in the migration source/generated forward chain |
| Direct pg_authid reads/writes | None. The new ownership check resolves its catalog OID with `::regclass`; it reads `pg_shdepend` and `pg_roles`, not password-bearing role data |
| pg_auth_members | Read-only membership checks; not role grants |
| ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_security_owner | Optional legacy package install and restoration in migration 50 Down; confirmed in the separately generated 50-to-49 script. Requires authority over the legacy role. Ordinary unprivileged rollback across retirement is not supported by this correction |
| CREATE EXTENSION btree_gist | Entry 11. Installed PostgreSQL 17 control file declares `trusted=true`; installation requires database CREATE privilege, which the database owner has |
| ALTER ... OWNER TO | Forward generated chain targets `nexa_erp_owner`; existing object ownership and target-role authority are prerequisites, supplied by Installer preparation. Not permission-free |
| ALTER SYSTEM; event triggers; server-file/program COPY; untrusted language creation | None found in the migration source sweep |

Cluster role creation/attribute changes in the **Installer** remain explicitly administrator-only; they are not ordinary migrations. The optional retired security package has its own privileged installation/rollback prerequisites.

PostgreSQL references: [REASSIGN OWNED](https://www.postgresql.org/docs/17/sql-reassign-owned.html), [GRANT](https://www.postgresql.org/docs/17/sql-grant.html), [role membership](https://www.postgresql.org/docs/17/role-membership.html).

## Evidence and outstanding work

- PostgreSQL mechanism probe passed in an owned disposable cluster. An unprivileged migration cannot grant itself the missing retired-role membership; administrator-granted temporary membership works, but is not the permanent migration design.
- The first real-principal regressions exposed the owner-USAGE false green. Those failing TRX files are retained as historical evidence; the owner authorized the four-grant repair on 16 September.
- Authorized repair: Release build passed with zero warnings/errors (39.34s). The targeted matrix passed **6/6, zero failed/skipped, 5m13s**, including the owner-USAGE/SELECT/FK regressions (`authorized/targeted-release/authorized-privilege-matrix.trx`).
- The subsequent full Release routine suite passed **845/845, zero failed, 25.34 minutes**, with TRX (`authorized/routine-release/full-suite.trx`). The following Debug run was interrupted when the Windows line-ending defect arrived; it is not a completed result.
- The [line-ending correction](migration-sql-line-endings.md) is now included. The combined correction passed the full Release routine suite: **846/846, zero failed, 25.29 minutes** (`local-evidence/migration-89/final/release/full-suite.trx`). That historical run does not certify the current candidate; current full Debug and Release evidence is recorded separately. Do not reuse the earlier Release result as verification of later edits.
- Requested restored installed-REV869B witness (actual starting migration history not yet inspected): **waiting for local paths to the privately handed-over field database and matching globals**. The owner reports that `sess_nexa_erp-pre75-2026-09-16.dump` exists with `WasInstalled=true` and nine REV869B roles; those facts have not yet been independently verified here.
- Record every field-environment match and gap using [migration witness environment](migration-witness-environment.md) before running the restored chain.
- Current candidate acceptance and the requested field restore are separate requirements; the field restore remains pending.

Raw evidence is local under `local-evidence/migration-75` and `local-evidence/migration-89`. Restore rehearsals must start from the oldest deployed state and preserve the installed/retired-role topology; counting migration entries alone is insufficient.

## Requested field witness, 18 September 2026

The named dump and private globals paths have been requested but are not available in this workspace. No restore or field-chain witness has been run. Globals contain SCRAM hashes: use their private filesystem path and never print their contents into logs or reports.

Before the run, record what is and is not reproduced: the database name, restored migration/installation starting state, compiled SQL line endings, restored role ownership and membership history, and the field operating system (currently unconfirmed). The Windows/PostgreSQL 17 synthetic regressions are separate evidence. Record the restored 229-table and 14-role claims as verified only after inspecting the actual restored backups.

## Current shared-candidate regression evidence

The frozen remaining-findings working candidate passed full Debug 878/878 and Release 875/875, with zero failures/skips, after both solution builds succeeded with zero warnings/errors. All 1,013 recorded source hashes matched after testing. TRX evidence is under local-evidence/finding3. These are shared working-candidate results, not clean per-commit checkout runs; ordered commits and their exact staged builds remain pending. No owner database was changed. The exact pre-75 field-backup witness remains unperformed.
