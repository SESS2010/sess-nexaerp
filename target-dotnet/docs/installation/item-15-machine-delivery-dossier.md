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
## Dispatcher lookup correction after the full-suite failure

The full Release run at a9b1416 reproduced 836 executed, 835 passed and one failure: RequestInputAndActorReadReachabilityTests.Every_http_request_server_selector_is_declared_in_the_reachability_manifest. The missing selector was DispatchMachineRequest.JobOrderId. The earlier targeted report runs did not include this whole-assembly check and were insufficient for full acceptance.

This exposed an actual missing read path: the Production job-order GET requires a permission that the Stores dispatcher does not hold. GET /api/v1/stores/machine-deliveries/job-orders now supplies a paged, searchable list of FAT-ready jobs in the resolved company, using the same stores.machine-deliveries Issue permission and substantive Stores assignment as dispatch. It returns job identity, machine identity, customer name and FAT status. Dispatch still performs its existing authoritative checks.

The selector is declared in the reachability manifest. The existing routine canonical purchase workflow now exercises the GET with real page permissions, verifies that Accounts is refused and that another company cannot expose the job, and confirms that Stores still cannot access the Production endpoint. The gated delivery witness also obtains its dispatch JobOrderId through this GET rather than directly from its database fixture. No additional full-flow test or migration was introduced.

The reproduced failing run took 34.13 minutes while a pre-existing full suite was also running; that is not a clean routine timing benchmark. The corrected full run ran alone after the other suite exited: **836 executed, 836 passed, zero failed, zero skipped**, completed 15 September 2026 at 13:51:47 IST. Measured test-process wall time was **24.272561 minutes (24 minutes 16.35 seconds)**; VSTest reported 24 minutes 10 seconds. The separate routine Release build passed with zero warnings/errors in 4 minutes 7 seconds. The formerly failing reflection check and the canonical purchase workflow both passed. All seven source/test files still matched the versions used for this build.

The full command was `dotnet test SESS.NexaERP.slnx -c Release --no-build --nologo --logger "trx;LogFileName=full-suite.trx" --results-directory local-evidence/overnight-20260914/full-suite-lookup-fixed`. Retained evidence: `full-suite-lookup-fixed/full-suite.trx`, `full-suite-lookup-fixed.log`, `full-suite-lookup-fixed-timing.json` and `full-suite-lookup-fix-build.log`, all under `local-evidence/overnight-20260914`. The original failing TRX is under `full-suite-a9b1416/full-suite.trx` in that directory. No migration SQL changed and no live database was connected or migrated for this fix. This is the full routine acceptance of the lookup correction, not a new run of the opt-in signed-delivery cases.
