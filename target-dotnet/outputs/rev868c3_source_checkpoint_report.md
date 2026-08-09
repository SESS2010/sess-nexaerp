# REV868C3 Source Checkpoint Report

Revision: REV868C3 Employee Department Manager Reconciliation
Migration ID: 20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation
Target verification database for future manual helper: sess_nexaerp_rev868_verify
Source baseline continued from: 486b05cb460810de50cbb27276cdc2477f5266b7

## Source-only status

No PostgreSQL database was accessed by Codex for this checkpoint. No helper was executed. No migration was applied. No password was requested. Live REV861 and sess_nexaerp were not touched.

## Implementation summary

- Uses Rev868C3EmployeeWorkbookData.cs as the approved source dataset.
- Reconciles employees by immutable EmployeeCode only.
- Preserves existing Employee IDs, EmployeeCode, LoginEnabled, ApprovalStatus, history and audit records.
- Adds PayrollEmployeeId, gender, qualification, date accuracy, responsibility, work location, manager scope and legacy department metadata.
- Adds EmployeeDepartmentHistory mapping/table for department transfer evidence.
- Adds DepartmentApprovalMapping Scope for 14 scoped manager mappings.
- Adds Manager -> MD -> TD workflow-step model while keeping thresholds configurable.
- Adds isolated secure helper for GeneratePlanOnly, PreflightOnly, full isolated apply and ResumeVerifyOnly modes.

## Expected database evidence after management-run isolated migration

- Active employee count: 42
- Relieved/inactive employee count for approved former employees: 9
- Department count: 12
- Department manager mapping count: 14
- New EmployeeCode evidence: SESS-041 through SESS-051
- Duplicate EmployeeCode count: 0
- Duplicate non-null PayrollEmployeeId count: 0
- Narren S: SESS-040, approximate DOJ 2026-02-09 with auditable approximate-date flag
- Mageshwari K: SESS-049, Payroll ID 1072, Gender Female, Software/IT alternate only through effective mapping

## Rollback design

Before changing rows, the migration creates migration-owned backup tables:

- nexa.rev868c3_employee_backup
- nexa.rev868c3_department_backup
- nexa.rev868c3_department_mapping_backup
- nexa.rev868c3_role_backup

Down rollback removes only REV868C3-owned status-history, department-history, audit, role-assignment, permission, manager-mapping and newly-created employee rows before deleting REV868C3-created employees. It restores exact previous employee, department, role and manager-mapping values from backup tables, checks employee-code integrity, then drops migration-owned backup tables last.

## Offline SQL review artifacts

- outputs/rev868c3_up_idempotent.sql
- outputs/rev868c3_down_review.sql

## Verification completed

- Build: passed, 0 warnings, 0 errors
- Non-PostgreSQL tests: 159 passed, 0 failed, 0 skipped
- PowerShell parse: passed for tools/apply-rev868c3-employee-reconciliation-secure.ps1
- Helper safety scan: passed; isolated target, protected DB rejects, plan/preflight/resume modes, pre-C3 backup and explicit future EF target present
- Secret/privacy scan: passed for REV868C3 changed files; no literal credentials or prohibited sensitive identifiers found. In-process password variable placeholders are present only for secure manual helper execution.
- git diff --check: passed

## Future management commands only - do not run yet

GeneratePlanOnly:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\tools\apply-rev868c3-employee-reconciliation-secure.ps1" -GitPath "C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe" -GeneratePlanOnly
```

PreflightOnly:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\tools\apply-rev868c3-employee-reconciliation-secure.ps1" -GitPath "C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe" -PreflightOnly
```

Full isolated execution after management approval:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\tools\apply-rev868c3-employee-reconciliation-secure.ps1" -GitPath "C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe"
```

Resume-only verifier after isolated execution:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\tools\apply-rev868c3-employee-reconciliation-secure.ps1" -GitPath "C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe" -ResumeVerifyOnly
```
