# Item 15 delivered-machine prerequisite - source findings

Report 10 is not implemented or accepted. Its read-only source probe is outputs/item15_component_ancestry_source_probe.sql. It requires psql company_code and machine_serial variables, runs in a read-only transaction, and is an administrator diagnostic, not a permission-gated ERP endpoint. Retained BOM costs in its output exclude late valuation adjustments and must not be used as final Actual BOM totals.

The probe was executed against the governed machine WITNESS-MACHINE-001-CORRECTED after the canonical purchase workflow passed. It traced three BOM history entries (+0.30, +0.30, -0.30), each through two physical provenance layers to one GRN, one accepted bill and one QC revision. Recursive layer traversal uses UNION to deduplicate ancestry, and nested evidence avoids multiplying component quantities by charge/QC rows. Concession histories are included where present; this machine does not by itself prove every deviation scenario.

The machine is FAT READY, not delivered. Its business-state backup excludes all synthetic volume tables and restores to an identical trace: 2,711,181 bytes, SHA-256 77503D55AEAFCC90211E797EB0AC1F5E3FF19309451FC7BD44B7563E08B6EE7B. Local copy: report10_source on the owned isolated server 127.0.0.1:56949, 30 governed movements. Its fixture migration history is public.__EFMigrationsHistory at 102; the user's backup history is in advance. Do not confuse these histories or replay the entire migration chain over this populated fixture.

## Decision still required

Component fitment already posts CONSUMPTION_OUT. Existing DC dispatch requires a separate stock posting batch. Simply posting those components out again to make a chamber look delivered would double-consume them.

The user has been asked whether the bounded report prerequisite should record an immutable signed external machine DC against the job/customer without a second component stock-out, or whether finished-machine inventory receipt and dispatch accounting must be built first. No option has been silently selected. Either route still needs signed delivery evidence; FAT readiness is insufficient.

The existing DC foundation has two commercial natures. Answers_To_Open_Questions corrects this to RETURNABLE, NON_RETURNABLE, WARRANTY_FREE, with DEMO as a RETURNABLE purpose. Any DC implementation must honor that correction and the Stores Completion/DC Custody rules, including customer PO and applicable notifications. No old constraint has been weakened.

Next implementation must reuse the controlled one-statement report pipeline, exact-company/machine selection, live permissions, Excel and total-to-source links. Final cost must reuse the established late landed-cost valuation and reversal mechanism. Missing required source evidence must be explicit. The source probe proves neither delivery nor runtime authorization.

Read for this work: frozen Stores Full Schema Guideline, Answers_To_Open_Questions, DC_Custody_Specification, Stores_Completion, Sales_Module, Pending_Work_Specification item15 and the final overnight instruction. This is the written source boundary for resumption, not a completion claim.