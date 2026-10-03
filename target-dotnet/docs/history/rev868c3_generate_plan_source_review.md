# REV868C3 GeneratePlanOnly Source Review

This report was prepared from source only. The helper was not executed. No database was accessed.

- Host: localhost
- Port: 5432
- Target DB: sess_nexaerp_rev868_verify
- Rejected DBs: sess_nexaerp, postgres, template0, template1, REV861-like names
- Target migration: 20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation
- Full apply plan: pre-C3 isolated backup, non-zero size check, SHA-256 hash, sanitized backup report before EF migration application.

## Source markers verified

- backup_relation_count
- status_history_partial_count
- department_history_partial_count
- audit_partial_count
- role_assignment_partial_count
- role_page_permission_partial_count
- manager_mapping_partial_count
- deterministic_employee_partial_count
- deterministic_department_partial_count
- deterministic_designation_partial_count
- safe_retry_state
- expected_migration_count
- active_employee_codes_expected
- relieved_employee_codes_expected
- login_enabled_mismatch_count
- approval_status_mismatch_count
- workflow_step Manager -> MD -> TD evidence
