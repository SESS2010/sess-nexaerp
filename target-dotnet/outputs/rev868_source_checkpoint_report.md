# REV868 Source Checkpoint - Purchase Requisition Foundation

## Scope Completed
- Implemented source-only REV868 foundation for Purchase Requisition through Department Verification, amount-based approval routing, Stores stock check, stock reservation, and Purchase/RFQ shortage handoff.
- No RFQ quotation, vendor comparison, PO, GRN, QC, stock issue, return, or accounts posting workflow was implemented.
- PostgreSQL migration was generated but not applied from Codex.
- Live REV861 was not modified.

## Migration
- Migration name: `20260808182945_Rev868PurchaseRequisitionFoundation`
- Idempotent SQL script: `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\outputs\rev868_purchase_requisition_foundation_idempotent.sql`
- Migration application status: not applied by Codex; management must run future secure helper after review.

## Main Source Changes
- Added Purchase Requisition domain entities, line items, status history, approval history, attachments, stock availability checks, reservations, reservation history, purchase handoffs, and approval route settings.
- Added DTO contracts for PR create/update/action/detail/list/history and stock check results.
- Added EF Core mappings, keys, foreign keys, indexes, check constraints, concurrency tokens, and filtered uniqueness for active reservation/pending handoff protection.
- Added Page Master entries for `purchase.requisitions`, `purchase.requisition-approvals`, `stores.stock-check`, `stores.reservations`, and `purchase.requirement-handoff`.
- Added `/api/v1/purchase/requisitions` endpoints for draft, submit, verify, approve, reject, revision, resubmit, cancel, hold, stock check, history, reservations, and handoffs.
- Added backend self-approval prevention for PR approval.
- Added future secure helper `tools\apply-rev868-secure.ps1` with secure password prompt, expected database guard, preflight/generate-SQL modes, and in-process environment cleanup.

## Authorization and Workflow Notes
- Endpoints use existing REV866/REV867 page-permission authorization framework.
- Approval route is amount-based: Manager up to 50,000; Technical Director above 50,000 up to 500,000; Managing Director above 500,000.
- Creator/submitter self-approval is blocked in source.
- Stores stock check computes on-hand from stock ledger movements, subtracts active reservations, reserves available quantity, and creates handoff rows only for shortages.
- No direct stock-balance editing table or endpoint was added.

## Build and Test Evidence
- Build: `dotnet build .\SESS.NexaERP.slnx -c Release` succeeded with 0 warnings and 0 errors.
- Tests: `dotnet test .\SESS.NexaERP.slnx -c Release --no-build` passed 73/73.
- PowerShell parse: `tools\apply-rev868-secure.ps1` parsed successfully.

## Secret and Safety Scan
- Source-only scan method: PowerShell scan over git tracked/untracked non-ignored files plus redacted Git history classification.
- Excluded paths: `bin/`, `obj/`, `local-evidence/`, `backups/`, `*.dump`, `*.bak`, `*.pdb`, `*.dll`, `*.exe`.
- Current source result: no literal database password, hard-coded connection string password, JWT/OIDC secret, API token, private key, or populated `PGPASSWORD` value found.
- Git history classification: matching history lines were secure variable placeholders or secret-related labels only; no literal secret values detected.

## Live REV861 Health
- Read-only health check returned HTTP 200.
- Reported live app revision: REV861.
- This REV868 work did not modify or replace live REV861.

## Future Management Command - Preflight Only
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\tools\apply-rev868-secure.ps1" -GitPath "C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe" -PreflightOnly
```

## Pending Before Database Application
- Management review of REV868 source checkpoint.
- Management manual execution of secure preflight/helper only after approval.
- PostgreSQL backup and migration application evidence, if management proceeds.
- Real OIDC provider and real token testing remains a production-readiness blocker.

## Not Started
- REV869.
- RFQ quotation/vendor comparison.
- Purchase Order.
- GRN/QC.
- Stock issue/return.
- Accounts posting.
