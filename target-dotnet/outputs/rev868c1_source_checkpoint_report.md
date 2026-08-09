# REV868C1 Source Checkpoint Report

## Purpose
REV868 database/schema evidence is present, but the prior read-only post-run verification showed zero PR workflow records. REV868C1 prepares controlled PostgreSQL-backed workflow verification for the isolated database only.

## Root Cause / Gap
- REV868 migrations and schema were applied to `sess_nexaerp`.
- Read-only evidence confirmed tables, constraints, indexes and permissions.
- Workflow counts were all zero for PRs, PR lines, stock checks, reservations, handoffs and histories.
- Therefore business workflow persistence still needs controlled test evidence before REV868 can be fully accepted.

## Source-Only Work Completed
- Added isolated helper: `tools/apply-rev868c1-isolated-workflow-verification-secure.ps1`.
- Added static helper/factory safety tests.
- Added dormant PostgreSQL-backed workflow verification tests that run only when `REV868C1_POSTGRES` targets `Database=sess_nexaerp_rev868_verify`.
- No PostgreSQL command was run by Codex during this source checkpoint.
- No migration, backup, restore, helper execution, database creation/drop, or password prompt was performed by Codex.

## Isolated Database Target
- Host: `localhost`
- Port: `5432`
- Database: `sess_nexaerp_rev868_verify`
- Required expected database guard: `NexaErp__ExpectedDatabase=sess_nexaerp_rev868_verify`

## Safeguards
- Helper rejects `sess_nexaerp`, `postgres`, `template0`, `template1`, and REV861-like database names.
- Design-time factory requires `ConnectionStrings__NexaErp`.
- Design-time factory requires `NexaErp__ExpectedDatabase`.
- Design-time factory fails closed if connection database differs from expected database.
- `-PreflightOnly` performs read-only identity/schema/history checks only.
- `-GeneratePlanOnly` prints proposed operations and SQL without password prompt or PostgreSQL connection.
- Test-only PostgreSQL connection variable is `REV868C1_POSTGRES` and is restricted to `sess_nexaerp_rev868_verify`.
- No temporary authentication was added to production/API source.
- Real OIDC provider/token testing remains a production-readiness blocker.

## Prepared PostgreSQL-Backed Test Coverage
- PR lifecycle: draft, submit, stores verification, approval, reject, revision request, resubmit, hold and cancel history evidence.
- Amount routing: Manager up to 50000, TD from 50001 to 500000, MD above 500000, route-overlap checks.
- Stock reconciliation: full, partial and zero stock scenarios.
- Location evidence: `WarehouseId`, optional `RackBinId`, and `LocationKey` persisted for checks, reservations and handoffs.
- Reconciliation: requested quantity equals reserved plus shortage/handoff.
- Security: self-approval and direct API denial audit evidence prepared; existing AuthorizationIntegrationTests provide 401/403 API behavior under test-only auth.
- Data integrity: duplicate active reservation and duplicate PendingRFQ handoff prevention.
- Failure recovery: rollback test ensures failed allocation leaves no partial reservation/handoff evidence.
- Source guards: inactive item/warehouse/rack-bin selection blocking, no hard delete endpoint, no direct stock editing in PR endpoints.

## Management Execution Boundary
Codex must stop after source checkpoint. Management must approve and manually run preflight/execution commands in normal Windows PowerShell. REV869 must not start until REV868C1 evidence is reviewed.