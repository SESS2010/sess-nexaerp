# Item 15, report 10: signed machine delivery dossier

The owner selected Option A on 15 September 2026. A machine remains a job order. Components are consumed at fitment. FAT READY permits a MACHINE delivery challan; retained customer signature establishes DELIVERED. This implementation creates no finished-machine inventory and no second stock or FIFO consumption.

## Governed delivery

The job-based machine_delivery_challans, machine_delivery_signatures and machine_delivery_bom_entries tables retain the DC, signature bytes/hash and Actual BOM membership at delivery. Existing inventory DC posting and fitment guards are unchanged. These private tables are not exposed to ordinary runtime DML.

POST /api/v1/stores/machine-deliveries records dispatch using a resolved Stores assignment and the stores.machine-deliveries Issue permission. RETURNABLE/DEMO requires an expected return date; NON_RETURNABLE/CUSTOMER_PO_BASED uses the job's customer PO and records an in-app Managing Director notification. DEMO is a purpose, never a nature.

POST /api/v1/stores/machine-deliveries/{id}/signature retains customer signatory, delivery timestamp and PDF/PNG/JPEG evidence, with SHA-256 computed from retained bytes. Both operations use the existing command ledger, transaction-bound audit and idempotent receipt replay. GET /{id} exposes metadata; GET /{id}/signature-evidence requires the dossier's View permission. Signature bytes are not embedded in general metadata.

The signed RETURNABLE DC remains OUTSTANDING. A signed NON_RETURNABLE DC becomes CLOSED. DELIVERED is derived from the retained signature; FAT readiness alone does not qualify. Dispatch and signing refuse invalid dates or changed FAT/serial identity.

## Dossier

Report key: machine-dossier. Select one machineSerial through the existing selection JSON and resolved company context. The controlled report function returns data, grouped totals, drill-through and export audit in one SQL statement. The existing workbook pipeline supplies About, summary/totals and detail worksheets.

The report traces captured fitment and reversal entries through physical provenance, material issue, GRN, vendor, accepted bill and allocated charges, QC revisions/parameters and recorded concessions. Physical source evidence is aggregated separately to prevent multiplying quantities or costs. Current accepted landed-cost adjustments reuse the Actual BOM valuation function, including negation of adjustments on reversed fitments. This is current accepted valuation, not historical valuation as of a past cutoff.

Readable detail columns name the GRNs, vendors, accepted bills and inspections. Numbered source-evidence parts retain the complete nested evidence below Excel's cell limit. Evidence-only rows contribute zero to totals. Missing source links remain absent evidence; the report does not manufacture a GRN, bill or approval.

## Witness and migration evidence

The canonical governed workflow creates WITNESS-MACHINE-001-CORRECTED through purchasing, receipt, QC, accepted billing, issue, fitment/reversal and FAT readiness. Signed delivery is then exercised through the actual permission-gated endpoints. The signature is explicitly synthetic test evidence, not an external customer's attestation.

The initial RETURNABLE witness passed 1/1 in 4 minutes 16 seconds. It reconciled net quantity 0.300000 TRIAL-MTR, material INR 1,416.00, allocated charges INR 3.60 and landed value INR 1,419.60. Stock movements remained 30; FIFO consumption rows remained four; Actual BOM row count was unchanged. Signature retrieval, denied Accounts dispatch, denied Stores dossier access and Excel export passed. The workbook was separately inspected for machine and bill evidence.

The final two-nature witness passed 2/2 in 8 minutes 8 seconds, including NON_RETURNABLE Managing Director notification, both delivery states and null-filename refusal. Reports 1–9 passed their shared permissions, one-command and export-audit regression, 1/1 in 1 minute 43 seconds.

Migration 20260915103000_MachineDeliveryDossier is number 103. Its embedded SQL is frozen. On the existing owned exact-name witness copy (port 56326, sess_nexa_erp), 102 to 103 passed as nexa_erp_migration with SET ROLE nexa_erp_owner. That copy was restored from before_tuesday.dump at 78 and previously proven through 102. Provision/status returned RECONCILED and VERIFIED without credential changes. The full generated script has 103 migration-history insertions and zero occurrences of either prohibited SESS spelling. A separate disposable clone also passed empty rollback/reapply. No live database was connected or migrated.

Evidence directory: local-evidence/overnight-20260914. Relevant files: report10-manager-role-release.trx, report10-both-natures-release.trx, report10-reports1to9-regression.trx, report10-exact-name-context.log, report10-exact-name-102-to-103.log, report10-exact-name-provision.log, report10-exact-name-status.log, report10-empty-rollback.log, report10-empty-reapply.log and report10-full-chain-sweep.json. JSON and Excel machine dossiers are under local-evidence/item15.

## Scope and checkpoint

At 11:30 this work remained uncommitted, as instructed; the RETURNABLE witness had just passed. Subsequent verification continued on the authorized Report 10 task. No Block A work was started. The previously measured routine suite result remains 836 passing in 23.71 minutes at the earlier baseline; it is not represented as a new full-suite measurement for migration 103. Both new end-to-end cases are behind WORKFLOW_WITNESS.

This is the backend delivery prerequisite and audit report, not completion of the entire DC module. The current bounded path permits one DC per job. Return/re-dispatch, cancellation/correction ceremonies and frontend screens are outside this implementation. The signed DC remains historical evidence; no finished-machine stock receipt is introduced to support those future operations.