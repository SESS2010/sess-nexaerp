# Advance/REV869B migration split inventory

## Migration and gate

- Default business migration: `20260824032638_AdvanceInitialBaseline` in `NexaErpDbContext`.
- Deferred security migration: `20260824120000_Rev869BSecurityPackage` in the separate `Rev869BSecurityDbContext` and `SESS.NexaERP.SecurityMigrations` assembly.
- The deferred context uses `advance.__EFMigrationsHistory_Rev869BSecurity` and its design-time factory requires `NexaErp__Rev869BSecurityMigrationTarget=20260824120000_Rev869BSecurityPackage`.
- The API and default infrastructure migration project do not reference the security migrations project. A default ERP `dotnet ef database update` therefore cannot discover the deferred migration.

## Generated Up SQL

- Business: 1237305 bytes; SHA-256 `786B43B041EC71E730A22430CCB939A0ED8C7B8FC78C5B4CB83037AF0979EED1`.
- Security: 186976 bytes; SHA-256 `5340CC8CCA7445C5838FDBCE5422FDE1F2AB124096FD33A91F4C195D147BCBEB`.

## Business baseline classification

Tables: **72** modelled tables.

Functions: **15**.

- `rev869a_block_history_mutation`
- `rev869a_guard_controlled_version`
- `rev869a_guard_used_uom_conversion`
- `rev869b_commercial_snapshot_reconciles`
- `rev869b_enforce_quotation_transition`
- `rev869b_enforce_transition`
- `rev869b_guard_authoritative_transition`
- `rev869b_guard_child_insert`
- `rev869b_guard_controlled_snapshot`
- `rev869b_guard_extended_immutability`
- `rev869b_guard_history_insert`
- `rev869b_qualification_provenance_valid`
- `rev869b_reject_immutable_mutation`
- `rev869b_reject_overlapping_approval_policy`
- `rev869b_validate_parent_contract`

Triggers: **48**.

- `trg_rev869a_history_append_only`
- `trg_rev869a_identity_version_guard`
- `trg_rev869a_policy_version_guard`
- `trg_rev869a_qc_version_guard`
- `trg_rev869a_scope_version_guard`
- `trg_rev869a_tax_version_guard`
- `trg_rev869a_uom_conversion_version_guard`
- `trg_rev869a_used_uom_conversion`
- `trg_rev869a_vendor_qualification_version_guard`
- `trg_rev869a_warehouse_condition_version_guard`
- `trg_rev869b_approval_policy_overlap_guard`
- `trg_rev869b_comparison_authoritative_guard`
- `trg_rev869b_comparison_history_insert_guard`
- `trg_rev869b_comparison_line_insert_guard`
- `trg_rev869b_comparison_line_parent_guard`
- `trg_rev869b_comparison_line_snapshot_guard`
- `trg_rev869b_comparison_lines_delete_guard`
- `trg_rev869b_comparison_snapshot_guard`
- `trg_rev869b_comparison_transition_guard`
- `trg_rev869b_followup_immutable`
- `trg_rev869b_followup_insert_guard`
- `trg_rev869b_followup_parent_guard`
- `trg_rev869b_invitation_insert_guard`
- `trg_rev869b_invitation_snapshot_immutable`
- `trg_rev869b_invitation_transition_guard`
- `trg_rev869b_po_authoritative_guard`
- `trg_rev869b_po_history_insert_guard`
- `trg_rev869b_po_line_insert_guard`
- `trg_rev869b_purchase_approval_history_immutable`
- `trg_rev869b_purchase_order_history_immutable`
- `trg_rev869b_purchase_order_line_parent_guard`
- `trg_rev869b_purchase_order_lines_immutable`
- `trg_rev869b_purchase_order_parent_guard`
- `trg_rev869b_purchase_order_snapshot_guard`
- `trg_rev869b_purchase_order_transition_guard`
- `trg_rev869b_purchase_status_history_immutable`
- `trg_rev869b_quotation_line_insert_guard`
- `trg_rev869b_quotation_line_parent_guard`
- `trg_rev869b_quotation_transition_guard`
- `trg_rev869b_rfq_line_insert_guard`
- `trg_rev869b_rfq_lines_immutable`
- `trg_rev869b_rfq_transition_guard`
- `trg_rev869b_status_history_insert_guard`
- `trg_rev869b_technical_insert_guard`
- `trg_rev869b_technical_parent_guard`
- `trg_rev869b_technical_verifications_immutable`
- `trg_rev869b_vendor_quotation_lines_immutable`
- `trg_rev869b_vendor_quotation_snapshot_guard`

## Deferred security-package classification

Tables: **16**.

- `rev869b_command_attempt_outcomes`
- `rev869b_command_attempts`
- `rev869b_command_claims`
- `rev869b_command_contexts`
- `rev869b_command_receipts`
- `rev869b_command_requests`
- `rev869b_export_authorizations`
- `rev869b_export_batch_rows`
- `rev869b_export_batches`
- `rev869b_export_releases`
- `rev869b_purge_attempts`
- `rev869b_purge_authorizations`
- `rev869b_purge_candidates`
- `rev869b_purge_events`
- `rev869b_target_catalogue_manifest`
- `rev869b_target_instance_identity`

Functions: **45**.

- `rev869b_authorize_export_release`
- `rev869b_build_raw_facts_v4`
- `rev869b_canonical_json_v3`
- `rev869b_claim_command_context`
- `rev869b_command_context_valid`
- `rev869b_commit_command_attempt`
- `rev869b_deny_ledger_mutation`
- `rev869b_execute_purge`
- `rev869b_guard_durable_audit_retention`
- `rev869b_guard_explicit_mutation`
- `rev869b_guard_history_insert`
- `rev869b_guard_qualification_history_insert`
- `rev869b_guard_qualification_lifecycle`
- `rev869b_open_command_attempt`
- `rev869b_prepare_export_batch`
- `rev869b_read_command_evidence`
- `rev869b_read_command_evidence_v2`
- `rev869b_read_command_facts_v4`
- `rev869b_read_export_evidence`
- `rev869b_read_export_evidence_v2`
- `rev869b_read_export_facts_v4`
- `rev869b_read_prepared_export_batch`
- `rev869b_read_purge_evidence`
- `rev869b_read_purge_evidence_v2`
- `rev869b_read_purge_facts_v4`
- `rev869b_read_target_acl_evidence`
- `rev869b_read_target_acl_evidence_v2`
- `rev869b_read_target_acl_facts_v4`
- `rev869b_read_target_security_state`
- `rev869b_reconcile_command_attempt`
- `rev869b_reconcile_purge`
- `rev869b_record_export_release_outcome`
- `rev869b_record_noncommit_outcome`
- `rev869b_record_purge_failure`
- `rev869b_register_command_request`
- `rev869b_register_export_authorization`
- `rev869b_register_purge_authorization`
- `rev869b_reject_controlled_delete`
- `rev869b_require_bound_history`
- `rev869b_require_qualification_history`
- `rev869b_start_command_attempt`
- `rev869b_start_purge`
- `rev869b_target_catalogue_fingerprint`
- `rev869b_verify_target_catalogue_acl`
- `rev869b_write_policy_history`

Triggers: **39**.

- `trg_rev869b_bound_comparison_history`
- `trg_rev869b_bound_followup_history`
- `trg_rev869b_bound_invitation_history`
- `trg_rev869b_bound_po_history`
- `trg_rev869b_bound_policy_history`
- `trg_rev869b_bound_qualification_history`
- `trg_rev869b_bound_quotation_history`
- `trg_rev869b_bound_rfq_history`
- `trg_rev869b_bound_technical_history`
- `trg_rev869b_delete_approval_history`
- `trg_rev869b_delete_comparison`
- `trg_rev869b_delete_comparison_line`
- `trg_rev869b_delete_followup`
- `trg_rev869b_delete_invitation`
- `trg_rev869b_delete_po`
- `trg_rev869b_delete_po_history`
- `trg_rev869b_delete_po_line`
- `trg_rev869b_delete_policy`
- `trg_rev869b_delete_quotation`
- `trg_rev869b_delete_quotation_line`
- `trg_rev869b_delete_rfq`
- `trg_rev869b_delete_rfq_line`
- `trg_rev869b_delete_status_history`
- `trg_rev869b_delete_technical`
- `trg_rev869b_durable_audit_retention`
- `trg_rev869b_explicit_comparison_line_mutation`
- `trg_rev869b_explicit_comparison_mutation`
- `trg_rev869b_explicit_followup_mutation`
- `trg_rev869b_explicit_invitation_mutation`
- `trg_rev869b_explicit_po_line_insert`
- `trg_rev869b_explicit_po_mutation`
- `trg_rev869b_explicit_policy_mutation`
- `trg_rev869b_explicit_quotation_line_insert`
- `trg_rev869b_explicit_quotation_mutation`
- `trg_rev869b_explicit_rfq_line_insert`
- `trg_rev869b_explicit_rfq_mutation`
- `trg_rev869b_explicit_technical_insert`
- `trg_rev869b_qualification_history_insert_guard`
- `trg_rev869b_qualification_lifecycle`

## Shared object identity

`rev869b_guard_history_insert` is intentionally declared in both artifacts. The business baseline installs the existing fail-closed placeholder and its business-history triggers. The deferred package preserves the original installation order and replaces that function body with the command-context-aware implementation. No role-dependent implementation is executed by the business migration.
