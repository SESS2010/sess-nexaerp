# REV868C2 Purchase Approval Route Source Checkpoint

## Scope
Source-only correction preparation. No PostgreSQL access, helper execution, migration application, backup, restore, REV861 change or REV869 work was performed.

## Blocking Finding Corrected
The previous REV868C2 checkpoint incorrectly mapped the MANAGER route to the fixed ERP role `TECHNICAL_SUPPORT_MANAGER`. That is invalid for company-wide Purchase Requisitions because PRs can originate from Production, Stores, Purchase, Design, Service, Accounts, Engineering, Administration and other departments.

## Reused/New Table Decision
Existing `reporting_relationships` is employee-to-employee reporting history. It does not define route-level department approval authority and cannot safely answer: "Who approves MANAGER-level PRs for this requesting department?"

A new normalized table is prepared:

- `nexa.department_approval_mappings`
- Department
- ApprovalRouteCode = `MANAGER`
- PrimaryApproverEmployeeId
- Optional AlternateApproverEmployeeId
- EffectiveFrom / EffectiveTo
- IsActive
- Created/modified/audit fields via `AuditableEntity`

## Canonical Route Design
Stable route levels remain:

- `MANAGER`
- `TECHNICAL_DIRECTOR`
- `MANAGING_DIRECTOR`

Fixed ERP role codes are retained only for:

- `TECHNICAL_DIRECTOR`
- `MANAGING_DIRECTOR`

`MANAGER` now resolves through active Department Approval Mapping using the PR `RequestingDepartmentId`. Its route setting uses resolver code `DEPARTMENT_MANAGER`; this is not a single fixed ERP approver role.

## Fail-Closed Rules Prepared
MANAGER route fails closed when:

- Requesting department is missing.
- Requester employee is missing.
- No active department MANAGER mapping exists.
- More than one active mapping matches the same department/route/effective date.
- Configured approver is inactive or login disabled.
- Actor is not the configured primary approver or active alternate.
- Actor lacks active manager-level role/permission.
- Requester/creator/submitter is the same as the configured approver.

Failures return a clear configuration/conflict response and write a persistent audit denial through the existing audit writer when executed.

## Amount Boundaries Preserved
- `0.00` through `50000.00` -> `MANAGER`, resolved by department.
- `50000.01` through `500000.00` -> `TECHNICAL_DIRECTOR`.
- `500000.01` and above -> `MANAGING_DIRECTOR`.

Currency values use `decimal(18,2)` route settings and explicit `0.01` boundary starts to avoid overlap at paise precision.

## Revised Migration ID
`20260809123000_Rev868C2DepartmentManagerApprovalMapping`

This migration creates the department approval mapping table. The prior source-only route canonicalization migration remains prepared and now stores MANAGER with `DEPARTMENT_MANAGER` instead of `TECHNICAL_SUPPORT_MANAGER`.

## Helper Plan
Prepared helper:

`tools/apply-rev868c2-approval-route-correction-secure.ps1`

It is restricted to:

`localhost:5432 / sess_nexaerp_rev868_verify`

It blocks `sess_nexaerp`, `postgres`, `template0`, `template1`, and REV861-like names. It has `-GeneratePlanOnly` and `-PreflightOnly` modes. It must be run manually by management only after review.

## Evidence Plan Updates
The resume/final evidence plan is prepared to show:

- PR/requesting department
- route level
- resolved department approver employee/user
- approver role/permission evidence
- expected vs actual route and approver
- PASS/FAIL
- persisted route rows with min/max/active/order
- department MANAGER mapping coverage

## Source Tests Added
Source/non-PostgreSQL tests cover:

- MANAGER is not globally mapped to `TECHNICAL_SUPPORT_MANAGER`.
- MANAGER uses `DEPARTMENT_MANAGER` resolver semantics.
- Production/Stores/Technical Support style manager roles are accepted only as manager-level roles when configured.
- TD/MD are not accepted as manager-level roles.
- Missing manager configuration, inactive manager, self-approval and missing permission checks are present in source.
- TD and MD boundary route logic remains unchanged.
- No gap/overlap/duplicate/disabled route cases remain accepted by route calculation.

## Pending Management Execution
No database execution has been done. Management must run plan/preflight/full commands manually after approving this revised design.
