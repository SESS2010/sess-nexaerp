# REV868C3 Employee Workbook Source Checkpoint

Date: 2026-08-09  
Source workbook: `local-evidence/rev868c3/SESS_NexaERP_Final_Employee_Master_2026-08-09.xlsx`  
Mode: Source-only. No PostgreSQL access, helper execution, migration application, backup/restore, REV861 touch, or REV869 work was performed.

## Workbook Inspection

Read-only workbook access was confirmed.

Sheets inspected:

- `Summary`
- `Active Employees`
- `Department Master`
- `Manager Mapping`
- `Approval Workflow`
- `Relieved History`
- `Data Quality`

Approved workbook summary:

- Active employees: 42
- Clean departments: 12
- Manager mappings: 14
- Relieved/left history: 9
- Open data-quality items: 8

## Source Changes Prepared

Added `Rev868C3EmployeeWorkbookData` as a source-only, strongly typed representation of the management-approved workbook.

Captured:

- 12 clean Department Master decisions
- 42 active employee rows
- 14 department manager and alternate mappings
- Manager/MD/TD approval workflow thresholds
- 9 relieved employee history rows
- 9 data-quality decisions

This source checkpoint intentionally does not seed PostgreSQL yet. A later management-approved migration/helper must consume this source.

## Department and Manager Mapping Rules Captured

Clean departments:

- MANAGEMENT
- PURCHASE
- STORES
- ACCOUNTS_FINANCE
- HR_ADMIN
- PRODUCTION_FABRICATION
- DESIGN
- ELECTRICAL_PLC_INSTRUMENTATION
- REFRIGERATION_MECHANICAL
- SERVICE_TECHNICAL_SUPPORT
- SOFTWARE_IT
- QUALITY_QC

Manager mappings:

- PURCHASE: SESS-012 primary, SESS-014 alternate
- STORES: SESS-014 primary, SESS-012 alternate
- ACCOUNTS_FINANCE: SESS-007 primary, SESS-002 alternate
- HR_ADMIN: SESS-020 primary, SESS-002 alternate
- PRODUCTION_FABRICATION: SESS-023 primary, SESS-040 alternate
- DESIGN / REGULAR_PRODUCT: SESS-015 primary, SESS-019 alternate
- DESIGN / PROJECT: SESS-019 primary, SESS-015 alternate
- ELECTRICAL_PLC_INSTRUMENTATION: SESS-038 primary, SESS-001 alternate
- REFRIGERATION_MECHANICAL: SESS-003 primary, SESS-004 alternate
- SERVICE_TECHNICAL_SUPPORT / CHENNAI: SESS-004 primary, SESS-003 alternate
- SERVICE_TECHNICAL_SUPPORT / BANGALORE: SESS-011 primary, SESS-004 alternate
- SOFTWARE_IT: SESS-008 primary, SESS-049 alternate
- QUALITY_QC: SESS-040 primary, SESS-009 alternate
- MANAGEMENT: SESS-002 primary, SESS-001 alternate

## Approval Workflow Captured

- INR 0.00 through INR 50,000.00: Department Manager only
- INR 50,000.01 through INR 5,00,000.00: Department Manager, then SESS-002 MD
- Above INR 5,00,000.00: Department Manager, then SESS-002 MD, then SESS-001 TD/CEO

Controls captured:

- Self-approval: blocked
- Same user approving twice: blocked/count once at highest applicable stage
- Missing manager mapping status: `PendingApproverMapping`
- Audit history: required

## Data Separation

The source checkpoint keeps these fields separate:

- Payroll Employee ID
- ERP Employee Code
- Employee name
- Department
- Work location
- HR designation
- ERP functional responsibility
- Primary/alternate PR approver
- Manager scope
- Legacy department
- Relieved history
- Data-quality decisions

Sensitive statutory and banking values were not imported into source.

## Validation

Source-only tests added:

- Workbook counts match management-approved summary
- Clean departments replace mixed legacy categories
- Manager mappings reference active employees and clean departments
- Relieved employees are absent from the active working list
- Approval workflow matches Manager -> MD -> TD chain
- Confidential data boundaries are preserved

Validation result:

- `dotnet test --configuration Release --no-restore`: 152 passed, 0 failed, 0 skipped
- `git diff --check`: passed
- Secret scan: no literal database password/JWT/client secret/API/private key found; existing secure helper placeholders using in-process variables were observed

## Pending Before Database Work

- Create an approved REV868C3 migration to upsert clean departments, employee status changes, active employees, relieved history and manager mappings.
- Preserve existing employee IDs, role history, audit history and transaction history.
- Do not hard-delete relieved employees.
- Update workflow implementation if management confirms the workbook approval chain supersedes earlier single-route boundary logic.
- Run isolated PostgreSQL preflight only after management approves the REV868C3 database plan.
