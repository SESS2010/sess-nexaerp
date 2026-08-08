# REV861 Architecture Correction Phase 0 Report

Date: 2026-08-08

## Action Taken

- Stopped further major Purchase/Stores feature patching as requested.
- Read and applied the architecture correction instruction.
- Verified current supported .NET LTS from Microsoft official sources: .NET 10 LTS is active and supported until 2028-11-14.
- Checked local SDK: only .NET SDK 8.0.129 is installed, so .NET 10 SDK installation is required before target build/test can be claimed.
- Initialized a Git repository in the migration workspace.
- Preserved the current working ERP as a REV861 snapshot.
- Created architecture-gap and migration-proposal documents.
- Created a separate ASP.NET Core target architecture workspace as a blueprint without deleting or replacing the current ERP.

## Current ERP Preservation

Current installed ERP remains running as `REV861`.

Health endpoint verification:

- `http://127.0.0.1:8783/api/health`
- Status: PASS
- Revision: `REV861`

Snapshot location:

- `C:\Users\User\Documents\Codex\2026-07-03\see\current-system-snapshot\REV861`

Snapshot files:

- `app\InventoryERP_Software.html`
- `server\server.js`
- `server\package.json`
- `SNAPSHOT_MANIFEST.md`

## Created Architecture Files

- `C:\Users\User\Documents\Codex\2026-07-03\see\architecture\current-architecture-gap-report.md`
- `C:\Users\User\Documents\Codex\2026-07-03\see\architecture\migration-proposal.md`

## Created Target .NET Workspace

- `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\README.md`
- `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\src\SESS.NexaERP.Api`
- `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\src\SESS.NexaERP.Domain`
- `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\database\postgresql\README.md`
- `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\load-tests\README.md`
- `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\migration-checklists\phase-0-checklist.md`

## Important Status

- The target .NET project is a foundation blueprint, not a completed migration.
- It targets `net10.0`, but the local computer currently has only .NET 8 SDK.
- No claim is made that the ERP supports 300,000 concurrent users.
- No claim is made that the system is production-ready for the target scale.
- Production approval requires database migration, security testing, deployment testing, backup/restore testing and reproducible load-test evidence.

## Next Required Phase 0 Items

1. Install .NET 10 SDK.
2. Export current PostgreSQL schema.
3. Export current local JSON structures.
4. Generate current page/field/route/role catalogue.
5. Create database backup and restore evidence.
6. Confirm cloud provider, SLA and real concurrent-user target.
7. Create security and disaster-recovery plan.

