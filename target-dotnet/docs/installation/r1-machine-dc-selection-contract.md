# R1 machine DC gaps (a) and (c) - decision 14

Backend migration: `20260928100000_MachineDeliverySelection` (139), after TrackingLite (138).

- `GET /api/v1/stores/machine-deliveries/job-orders` excludes every job with an existing machine DC in the selected company, including signed returnable and non-returnable DCs. Exclusion happens before `TotalCount`, ordering and pagination. Search, page limits, permissions and substantive Stores assignment rules are unchanged.
- `GET /api/v1/stores/machine-deliveries/{id}` adds `JobOrderNumber` (PascalCase string), resolved from the linked company job. Fresh dispatch and signature responses use the same projection and carry this field. Existing committed idempotency receipts remain immutable; a replay of a pre-migration receipt may omit it. Reload the detail endpoint to read the added field.
- The existing print endpoint already supplies the job-order number and is unchanged.
- A job can still be dispatched by someone else after the picker loads. Keep the existing 409 duplicate-DC handling and the same-key/same-body retry behavior.
- No DC list or lookup-by-job endpoint is added: gap (b) remains R2.

Runtime reads remain behind SECURITY DEFINER functions; direct access to the DC/signature tables is not granted. Installer provisioning retains the new `machine_delivery_job_ids(uuid)` execute grant. Rollback removes only read projections and preserves all DC rows, signatures, BOM links and command receipts.

Validation evidence belongs in `local-evidence/r1-dc-gaps/`. The focused database filter is `FullyQualifiedName~R1_machine_DC_selection`; run it only after the 27 September nightly finishes, on disposable PostgreSQL. The nightly candidate for 27 September stays `4cb906a`; this feature waits for focused DB tests before integration.
