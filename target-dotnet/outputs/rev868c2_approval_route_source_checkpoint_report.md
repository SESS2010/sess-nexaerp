# REV868C2 Approval Route Source Checkpoint Report

## Scope
Source-only diagnosis and correction preparation for REV868C1 amount-routing evidence failures. Codex did not access PostgreSQL, execute helpers, apply migrations, create backups/restores, request a password, touch `sess_nexaerp`, touch `sess_nexaerp_rev868_verify` data, touch live REV861, or start REV869.

## Root Cause
The final REV868C1 evidence failed amount-routing checks because three concepts were mixed:

- Stable route code in PR workflow: previously `Manager`, `TD`, `MD`.
- Expected report labels: `Manager`, `TechnicalDirector`, `ManagingDirector`.
- Approved ERP Role Master codes: `TECHNICAL_SUPPORT_MANAGER`, `TECHNICAL_DIRECTOR`, `MANAGING_DIRECTOR`.

The persistent route configuration evidence displayed only `Manager`, proving that the isolated verification database did not yet contain all three active configurable route rows. Therefore this is not only a report-label issue; persistent route seed correction is required.

## Canonical Mapping Decision
Stable internal route codes:

| Band | Route Code | Approver Role Code | Display Label |
| --- | --- | --- | --- |
| 0 through 50,000 | MANAGER | TECHNICAL_SUPPORT_MANAGER | Manager |
| 50,000.01 through 500,000 | TECHNICAL_DIRECTOR | TECHNICAL_DIRECTOR | Technical Director |
| Above 500,000 | MANAGING_DIRECTOR | MANAGING_DIRECTOR | Managing Director |

Legacy aliases `TD`, `MD`, `TechnicalDirector`, and `ManagingDirector` are normalized for compatibility, but new PR route decisions use canonical route codes.

## Source Corrections Prepared
- `PurchaseRequisitionApprovalRoutes` now separates canonical route code, display label, and approver role code.
- PR route selection can use active persisted `purchase_approval_route_settings`; static thresholds remain only as no-data fallback.
- Duplicate/missing/overlapping/disabled routes fail closed by returning no single valid configured route.
- REV868C1 final evidence SQL now reports amount, expected route, configured route, calculated route, canonical role, display label, and PASS/FAIL.
- Data-only corrective migration prepared: `20260809115500_Rev868C2ApprovalRouteCanonicalization`.
- Restricted isolated helper prepared: `tools/apply-rev868c2-approval-route-correction-secure.ps1`.

## Corrective Migration Requirement
Required. Existing evidence showed only the `Manager` route persisted. The corrective migration safely upserts all three canonical route rows using `ON CONFLICT ("RouteCode") DO UPDATE`; it does not rewrite or delete historical PR, approval, audit, reservation, or handoff records.

## Management Boundary
The REV868C2 helper is prepared for future management review and manual execution only. It is restricted to `localhost:5432 / sess_nexaerp_rev868_verify` and blocks `sess_nexaerp`, `postgres`, `template0`, `template1`, and REV861-like targets.
