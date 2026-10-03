# Root clutter review: proposal only

Inventory of `target-dotnet/` on 2 October 2026 (IST), source base `31517ad51b0be70ff05fccb24597978edbe4ed92` plus local filenames. No moves, deletions or ignore changes are executed. Deletion requires the user's approval of exact candidates. Log/backup contents are not copied here.

| Root item | Size / type | Git status | Proposal |
|---|---|---|---|
| `.codex-doc-review/` | Directory | No tracked files | Move scratch artifacts to local-evidence after reference review; propose `/.codex-doc-review/` ignore; preserve authoritative source documents separately. |
| `api-phase1-smoke-err.log` | 0 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `api-phase1-smoke-out.log` | 3768 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `api-rev863-smoke-err.log` | 0 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `api-rev863-smoke-out.log` | 2156 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `api-rev864-db-smoke-err.log` | 0 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `api-rev864-db-smoke-out.log` | 12577 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `api-rev864-db-smoke2-err.log` | 0 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `api-rev864-db-smoke2-out.log` | 11023 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `api-rev864-permanent-auth-err.log` | 0 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `api-rev864-permanent-auth-out.log` | 3184 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `api-smoke-err.log` | 0 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `api-smoke-out.log` | 2965 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `backups/` | Directory | No tracked files | Keep protected local backups pending retention review; later move to an approved local backup/evidence location. Propose `/backups/` ignore after checking tracked exceptions. Never delete the only verified backup. |
| `candidate-build-v6.log` | 2242 bytes | No tracked files | Move nonempty logs to dated `local-evidence/legacy-logs/`; propose deletion of empty logs after approval; propose root `/*.log` ignore rule. |
| `outputs/` | Directory | 237 tracked files | Keep tracked test fixtures and historical evidence at current paths through go-live; move only reviewed unreferenced generated output to local-evidence; ignore explicit generated filenames. No blanket move or ignore. |
| `pc-maintenance-20260919/` | Directory | No tracked files | Move to dated local-evidence after review; propose `/pc-maintenance-*/` ignore. Never commit or execute these scripts. |
| `pc-maintenance-20260921/` | Directory | No tracked files | Move to dated local-evidence after review; propose `/pc-maintenance-*/` ignore. Never commit or execute these scripts. |

## Keep root source and configuration

Keep `src/`, `tests/`, `tools/`, `docs/`, `database/`, `load-tests/`, `migration-checklists/`, solution/config files, root README, `.gitattributes` and `.gitignore`. Keep ignored `local-evidence/` as local acceptance evidence. `.claude/` is local agent configuration; review its policy separately before cleanup. These are not deletion candidates.

## Output dependencies

Tests/tools refer to reports, archived EF sources and SQL in `outputs/`; blanket removal would break evidence paths. These tracked source files mention outputs (filenames only). This scan is a starting point, not approval to move unlisted files:

- `tests/SESS.NexaERP.A5Slice1.Probe/Program.cs`
- `tests/SESS.NexaERP.Tests/CompanyRelationshipExternalCodesTests.cs`
- `tests/SESS.NexaERP.Tests/CustomerPoIntakeRegisterCorrectionTests.cs`
- `tests/SESS.NexaERP.Tests/Foundation2InventoryOwnershipCustodyTests.cs`
- `tests/SESS.NexaERP.Tests/Foundation3InventoryProvenanceGenealogyTests.cs`
- `tests/SESS.NexaERP.Tests/RequestInputReachabilityGapCorrectionTests.cs`
- `tests/SESS.NexaERP.Tests/Rev867VerificationScriptTests.cs`
- `tests/SESS.NexaERP.Tests/Rev868C1PreparationTests.cs`
- `tests/SESS.NexaERP.Tests/Rev868C3ImplementationTests.cs`
- `tests/SESS.NexaERP.Tests/Rev868OvernightRemediationTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869AIsolatedDatabasePreparationHelperTests.cs`
- `tools/apply-rev868c2-approval-route-correction-secure.ps1`
- `tools/plan-rev868-isolated-restore-verification.ps1`
- `tools/resume-rev868c3-postgresql-tests-secure.ps1`
- `tools/verify-rev868c3-postrun-readonly-secure.ps1`

Tracked immediate output inventory (retain pending per-file dependency review):

- `outputs/advance_initial_baseline_seed_constraint_audit.md`
- `outputs/advance_initial_baseline_up.sql`
- `outputs/authentication_bootstrap_steps_7_12.md`
- `outputs/backend_architecture_reference.md`
- `outputs/calibration_purchase_item_type_open_items.md`
- `outputs/calibration_purchase_pair_item_type_corrections_down.sql`
- `outputs/calibration_purchase_pair_item_type_corrections_up.sql`
- `outputs/claude_current_status_and_next_stage_handoff_2026-09-11.md`
- `outputs/customer_po_intake_register_schema.md`
- `outputs/employee-master-rebuild-42.sql`
- `outputs/first_stores_module_migration_plan.md`
- `outputs/first_stores_module_schema_design.md`
- `outputs/item15_component_ancestry_source_probe.sql`
- `outputs/managing-director-department-priority.sql`
- `outputs/mir_concurrency_readability_and_post_audit_reachability.md`
- `outputs/multi_company_shared_identity_foundation_down.sql`
- `outputs/multi_company_shared_identity_foundation_up.sql`
- `outputs/proposed-reviewed-migration-target.md`
- `outputs/purchase_stores_audit_readiness_gap_analysis.md`
- `outputs/purchase_stores_erp_current_status_and_roadmap.md`
- `outputs/qc_due_at_stored_derivative_audit.md`
- `outputs/rev866_database_runtime_verification_20260808_182656.md`
- `outputs/rev867_master_foundation_idempotent.sql`
- `outputs/rev867c1_isolated_resume_sql_source_verification.md`
- `outputs/rev867c1_isolated_verification_preflight_source_report.md`
- `outputs/rev867c1_main_db_readonly_diagnostic_sql_source_verification.md`
- `outputs/rev867c1_migration_target_mismatch_incident.md`
- `outputs/rev867c1_readonly_diagnostic_sql_source_verification.md`
- `outputs/rev868_isolated_restore_verification_plan.md`
- `outputs/rev868_missing_backup_incident.md`
- `outputs/rev868_offline_rollback_to_rev867c1.sql`
- `outputs/rev868_purchase_requisition_foundation_idempotent.sql`
- `outputs/rev868_rollback_readiness_review.md`
- `outputs/rev868_source_checkpoint_report.md`
- `outputs/rev868c1_source_checkpoint_report.md`
- `outputs/rev868c2_approval_route_source_checkpoint_report.md`
- `outputs/rev868c2_department_manager_management_decision.md`
- `outputs/rev868c2_down_fix_down_idempotent.sql`
- `outputs/rev868c2_down_fix_up_idempotent.sql`
- `outputs/rev868c3_down_review.sql`
- `outputs/rev868c3_employee_reconciliation_down.sql`
- `outputs/rev868c3_employee_reconciliation_down_review.sql`
- `outputs/rev868c3_employee_reconciliation_idempotent.sql`
- `outputs/rev868c3_employee_workbook_source_checkpoint.md`
- `outputs/rev868c3_generate_plan_source_review.md`
- `outputs/rev868c3_isolated_acceptance_checkpoint_20260810_124101.md`
- `outputs/rev868c3_source_checkpoint_report.md`
- `outputs/rev868c3_up_idempotent.sql`
- `outputs/rev869_integrated_purchase_stores_implementation_plan.md`
- `outputs/rev869_legacy_reuse_final_mapping_report.md`
- `outputs/rev869_source_only_discovery_report.md`
- `outputs/rev869a_isolated_database_provisioning_checkpoint.md`
- `outputs/rev869a_isolated_execution_tooling_checkpoint.md`
- `outputs/rev869a_isolated_final_acceptance_checkpoint.md`
- `outputs/rev869a_management_decision_and_scope_checkpoint.md`
- `outputs/rev869a_preapply_source_safety_review.md`
- `outputs/rev869a_source_correction_checkpoint.md`
- `outputs/rev869a_source_implementation_checkpoint.md`
- `outputs/rev869b_a5_canonical_form_v2_specification.md`
- `outputs/rev869b_a5_formal_evidence_harness_independent_review_failure_state.md`
- `outputs/rev869b_a5_harness_abandonment_and_replacement_acceptance_state.md`
- `outputs/rev869b_a5_slice1_acceptance_state.md`
- `outputs/rev869b_architecture_freeze_root_cause_review.md`
- `outputs/rev869b_correction19_failure_reconciliation.md`
- `outputs/rev869b_correction21_failure_reconciliation.md`
- `outputs/rev869b_correction23_failure_reconciliation.md`
- `outputs/rev869b_correction23_internal_precheck.md`
- `outputs/rev869b_correction24_allowlist_evidence_reconciliation.md`
- `outputs/rev869b_correction24_internal_adversarial_precheck.md`
- `outputs/rev869b_correction24_internal_precheck_failure_reconciliation.md`
- `outputs/rev869b_correction25_internal_adversarial_precheck.md`
- `outputs/rev869b_correction25_internal_precheck_failure_reconciliation.md`
- `outputs/rev869b_correction26_failure_evidence_interface_reconciliation.md`
- `outputs/rev869b_correction26_internal_adversarial_precheck.md`
- `outputs/rev869b_correction27_failure_reconciliation.md`
- `outputs/rev869b_correction28_failure_and_source_feasibility_reconciliation.md`
- `outputs/rev869b_correction_22_failure_reconciliation.md`
- `outputs/rev869b_external_controller_architecture_freeze_review.md`
- `outputs/rev869b_external_controller_phase1_architecture_freeze_specification.md`
- `outputs/rev869b_external_controller_phase1_checkpoint.md`
- `outputs/rev869b_external_controller_phase1_correction1_checkpoint.md`
- `outputs/rev869b_external_controller_phase1_correction1_failure_reconciliation.md`
- `outputs/rev869b_external_controller_phase1_correction1_independent_source_safety_review.md`
- `outputs/rev869b_external_controller_phase1_failure_reconciliation.md`
- `outputs/rev869b_external_controller_phase1_independent_source_safety_review.md`
- `outputs/rev869b_external_controller_phase1_overnight_entry_blocker.md`
- `outputs/rev869b_external_controller_phase_a_a4_failure_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a4_independent_review.md`
- `outputs/rev869b_external_controller_phase_a_a4_lease_atomic_boundary_architecture_freeze.md`
- `outputs/rev869b_external_controller_phase_a_a5_boundary_and_immutable_plan_contract_decision.md`
- `outputs/rev869b_external_controller_phase_a_a5_checkpoint.md`
- `outputs/rev869b_external_controller_phase_a_a5_control_plane_ef_tooling_boundary_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a5_controlled_official_ef_package_graph_verification.md`
- `outputs/rev869b_external_controller_phase_a_a5_controlled_official_package_acquisition_and_identity.md`
- `outputs/rev869b_external_controller_phase_a_a5_dual_context_migration_and_package_boundary_decision.md`
- `outputs/rev869b_external_controller_phase_a_a5_ef_package_verification_evidence_integrity_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a5_erp_project_aware_package_lock_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a5_first_acceptance_failure_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a5_formal_evidence_harness_architecture_and_boundary_decision.md`
- `outputs/rev869b_external_controller_phase_a_a5_formal_timing_and_sequence_failure_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a5_historical_evidence_unavailability_and_fresh_attempt_decision.md`
- `outputs/rev869b_external_controller_phase_a_a5_implementation_checkpoint.md`
- `outputs/rev869b_external_controller_phase_a_a5_migration_inventory_and_context_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a5_mutant_gate_failure_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a5_npgsql_package_artifact_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a5_persistence_and_classifier_architecture_freeze.md`
- `outputs/rev869b_external_controller_phase_a_a5_project_aware_package_lock_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a5_project_graph_and_boundary_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a5_revised_implementation_checkpoint.md`
- `outputs/rev869b_external_controller_phase_a_a5_revised_mutant_harness_failure_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker.md`
- `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker_2.md`
- `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker_3.md`
- `outputs/rev869b_external_controller_phase_a_checkpoint.md`
- `outputs/rev869b_external_controller_phase_a_correction_a1_failure_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_correction_a1_independent_source_safety_review.md`
- `outputs/rev869b_external_controller_phase_a_correction_a2_failure_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_correction_a2_failure_reconciliation_entry_blocker.md`
- `outputs/rev869b_external_controller_phase_a_correction_a2_independent_source_safety_review.md`
- `outputs/rev869b_external_controller_phase_a_correction_a3_failure_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_correction_a3_independent_source_safety_review.md`
- `outputs/rev869b_external_controller_phase_a_failure_reconciliation.md`
- `outputs/rev869b_external_controller_phase_a_independent_source_safety_review.md`
- `outputs/rev869b_internal_adversarial_source_only_precheck_after_correction_27.md`
- `outputs/rev869b_internal_adversarial_source_only_precheck_after_correction_28.md`
- `outputs/rev869b_lifecycle_administrator_contract_conflict.md`
- `outputs/rev869b_overnight_blocker_report.md`
- `outputs/rev869b_overnight_run_final_statement.md`
- `outputs/rev869b_preapply_source_safety_rereview.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_10.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_11.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_12.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_13.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_14.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_15.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_16.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_17.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_18.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_19.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_2.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_20.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_21.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_22.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_23.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_3.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_4.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_5.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_6.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_7.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_8.md`
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_9.md`
- `outputs/rev869b_preapply_source_safety_review.md`
- `outputs/rev869b_retirement_decision_report.md`
- `outputs/rev869b_security_package_split_inventory.md`
- `outputs/rev869b_security_package_up.sql`
- `outputs/rev869b_source_correction_checkpoint.md`
- `outputs/rev869b_source_correction_checkpoint_10.md`
- `outputs/rev869b_source_correction_checkpoint_11.md`
- `outputs/rev869b_source_correction_checkpoint_12.md`
- `outputs/rev869b_source_correction_checkpoint_13.md`
- `outputs/rev869b_source_correction_checkpoint_14.md`
- `outputs/rev869b_source_correction_checkpoint_15.md`
- `outputs/rev869b_source_correction_checkpoint_16.md`
- `outputs/rev869b_source_correction_checkpoint_17.md`
- `outputs/rev869b_source_correction_checkpoint_18.md`
- `outputs/rev869b_source_correction_checkpoint_19.md`
- `outputs/rev869b_source_correction_checkpoint_2.md`
- `outputs/rev869b_source_correction_checkpoint_20.md`
- `outputs/rev869b_source_correction_checkpoint_21.md`
- `outputs/rev869b_source_correction_checkpoint_22.md`
- `outputs/rev869b_source_correction_checkpoint_23.md`
- `outputs/rev869b_source_correction_checkpoint_24.md`
- `outputs/rev869b_source_correction_checkpoint_24_implementation.md`
- `outputs/rev869b_source_correction_checkpoint_25.md`
- `outputs/rev869b_source_correction_checkpoint_26.md`
- `outputs/rev869b_source_correction_checkpoint_27.md`
- `outputs/rev869b_source_correction_checkpoint_28.md`
- `outputs/rev869b_source_correction_checkpoint_3.md`
- `outputs/rev869b_source_correction_checkpoint_4.md`
- `outputs/rev869b_source_correction_checkpoint_5.md`
- `outputs/rev869b_source_correction_checkpoint_6.md`
- `outputs/rev869b_source_correction_checkpoint_7.md`
- `outputs/rev869b_source_correction_checkpoint_8.md`
- `outputs/rev869b_source_correction_checkpoint_9.md`
- `outputs/rev869b_source_implementation_checkpoint.md`
- `outputs/sess_api_contract.md`
- `outputs/sess_business_process_purchase_stores.md`
- `outputs/specification_implementation_audit_2026-09-09.md`
- `outputs/tax_gst_controlled_workflow.md`

## Approval / cleanup sequence

1. Preserve verified backups, red-run worktrees and nightly evidence; never replace source evidence with retrospective claims.
2. Search each exact candidate path in source, tests, docs and packaging; review local scheduled-task/configuration references separately.
3. Present exact destinations and a manifest before moving evidence. Raw sensitive files stay local. Untracked `outputs/item_vendor_customer_api_fields_report.tex` stays uncommitted.
4. Obtain approval for the exact deletion list. Empty error logs are candidates only.
5. Make approved ignore changes in a separate reviewed cleanup commit. Ignore rules do not untrack committed files.
