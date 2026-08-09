# REV868C2 Purchase Approval Route Source Checkpoint

## Scope
Source-only correction preparation. No PostgreSQL access, helper execution, migration application, backup, restore, REV861 change or REV869 work was performed.

## Blocking Finding Corrected
The earlier plan treated `DEPARTMENT_MANAGER` like an ERP Role Master role code. That is not correct. It is now modeled as an approver-resolution method for the `MANAGER` route, while TD and MD remain fixed-role routes.

## Canonical Route And Resolution Model
Stable route levels:

- `MANAGER`
- `TECHNICAL_DIRECTOR`
- `MANAGING_DIRECTOR`

Resolution types:

- `DEPARTMENT_MAPPING` for `MANAGER`
- `FIXED_ROLE` for `TECHNICAL_DIRECTOR`
- `FIXED_ROLE` for `MANAGING_DIRECTOR`

Role codes:

- `MANAGER`: `ApproverRoleCode = null`; approver is resolved from `department_approval_mappings`.
- `TECHNICAL_DIRECTOR`: `ApproverRoleCode = TECHNICAL_DIRECTOR`.
- `MANAGING_DIRECTOR`: `ApproverRoleCode = MANAGING_DIRECTOR`.

## Reused/New Table Decision
Existing `reporting_relationships` is employee-to-employee reporting history. It does not define route-level department approval authority for PRs.

A new normalized table is prepared:

- `nexa.department_approval_mappings`
- Department
- ApprovalRouteCode = `MANAGER`
- PrimaryApproverEmployeeId
- Optional AlternateApproverEmployeeId
- EffectiveFrom / EffectiveTo
- IsActive
- Created/modified/audit fields via `AuditableEntity`

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

Failures return a clear configuration/conflict response and write persistent audit denial when executed.

## Amount Boundaries Preserved
- `0.00` through `50000.00` -> `MANAGER`, `DEPARTMENT_MAPPING`.
- `50000.01` through `500000.00` -> `TECHNICAL_DIRECTOR`, `FIXED_ROLE`.
- `500000.01` and above -> `MANAGING_DIRECTOR`, `FIXED_ROLE`.

Currency values use `decimal(18,2)` route settings and explicit `0.01` boundary starts to avoid overlap at paise precision.

## Migration Plan
Existing source-only migration corrected:

- `20260809115500_Rev868C2ApprovalRouteCanonicalization`
- Adds `ApproverResolutionType` to `purchase_approval_route_settings`.
- Makes `ApproverRoleCode` nullable.
- Upserts three canonical routes.
- Stores MANAGER as `ApproverResolutionType = DEPARTMENT_MAPPING` and `ApproverRoleCode = null`.

New source-only migration prepared:

- `20260809123000_Rev868C2DepartmentManagerApprovalMapping`
- Creates `department_approval_mappings` with department/primary/alternate approver/effective-date fields.

## Helper Plan Corrections
Prepared helper:

`tools/apply-rev868c2-approval-route-correction-secure.ps1`

It is restricted to:

`localhost:5432 / sess_nexaerp_rev868_verify`

GeneratePlanOnly now explicitly states:

- Host: localhost
- Port: 5432
- Target DB: `sess_nexaerp_rev868_verify`
- Rejected DBs: `sess_nexaerp`, `postgres`, `template0`, `template1`, REV861-like names
- Prerequisite: existing first 8 migrations exactly once
- Target corrective migration: `20260809123000_Rev868C2DepartmentManagerApprovalMapping`
- No backup/restore/drop/create operation
- No main DB operation

Preflight SQL is separated from post-migration SQL. Preflight does not query `department_approval_mappings` because that table does not exist before the migration.

## Evidence Plan Updates
Post-migration evidence is prepared for:

- ninth migration present exactly once
- `department_approval_mappings` table/columns/indexes/FKs/checks
- exactly three active canonical routes
- duplicate active route-code count
- overlap count
- decimal(18,2) gap count between adjacent ranges
- first range starts at `0.00`
- final range has no maximum
- boundary checks at `0.00`, `50000.00`, `50000.01`, `50001.00`, `500000.00`, `500000.01`, `500001.00`
- active departments requiring PR approval
- active manager mappings
- missing manager mappings
- duplicate active primary manager mappings
- inactive primary approvers
- missing approval permission
- delegate validity
- mapping effective-date validity

## Validation
Source-only validation was run after correction:

- Build passed.
- Non-PostgreSQL tests passed.
- PowerShell parse passed.
- Secret scan clean.
- Safety scan clean.
- `git diff --check` clean.

## Pending Management Execution
No database execution has been done. Management must run plan/preflight/full commands manually after approving this revised source checkpoint.
