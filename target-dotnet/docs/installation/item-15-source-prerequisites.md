# Item 15 — source prerequisites and approved decisions

Updated 14 September: the user requires all ten reports before further dashboard work, with delivered-machine ancestry last. These prerequisites are now in Item 15's authorized scope. Implementation and witnesses remain outstanding where stated below.

## FIFO return attribution

Observed: accepted material returns append physical movements, but no FIFO cost-credit record. The existing purchase witness restores 1.00 serialized unit plus 0.60 and 0.03 non-serialized units physically: 1.63 in total. Its accounting FIFO consumes two complete layers; the serialized physical source and accounting cost layer are deliberately different. A closing FIFO valuation cannot silently treat those units as absent.

The proposed correction is append-only credits linked to the accepted return line and the original issue's FIFO consumption rows. It must retain original consumption history, keep company/item/ownership boundaries, lock the same cost pool as consumption, refuse credits exceeding the original net consumed quantity, and replay idempotently with the return.

The user has settled the mechanism: unwind the original issue consumption rows in reverse creation order. There is no configurable attribution policy. Issue 1 at 100 and 1 at 200, then return 0.5: append a restoration of 0.5 against the 200 consumption, restoring value 100 and leaving net issue cost 200.

Keep layers and original consumptions immutable. Refuse returns above the issue's remaining unreturned quantity and restorations against fully restored consumption. Accepted-bill adjustments remain traceable. Fitment reversal is separate: it negates machine Actual BOM cost and restores engineer custody while issue consumption remains. See item-15-fitment-reversal-cost.md.

## Invoice received before goods

Observed: the current vendor-bill creation path requires a finalized GRN and its received quantity. An empty billed-not-received report would prove only that this workflow cannot record the situation.

A minimal prerequisite would record a supplier invoice against an issued PO before receipt, with company/vendor, invoice number/date, PO line, quantity/unit, currency and source evidence. Recording the supplier document must not pretend that three-way matching or payment approval has already happened. Receipt matching and accepted-bill linkage would be governed, auditable operations with duplicate, cross-company/vendor/currency, excess-quantity and replay refusals.

Implement the governed matching and correction lifecycle as the report's prerequisite. A recorded supplier invoice is not an accepted three-way-matched bill.

## Delivered machine

Observed: the governed machine witness reaches FAT readiness; it does not record delivery. An ancestry dossier must identify the actual job/machine and link delivery evidence before calling it delivered.

The existing DC specification requires a customer signature for machine delivery. The user's latest priority brings the necessary governed delivery proof into Item 15. Implement the report prerequisite without expanding into unrelated later Stores workflows.

The minimum proof must retain the applicable DC/job/machine/customer identities, a governed delivery event and signed evidence, then traverse actual component fitments to their GRN, vendor, accepted bill/allocated charges, QC and applicable approvals. FAT readiness is not delivery, and foundation rows alone are not authoritative delivery evidence.

Scheduling is settled: complete these prerequisites within Item 15 and build the delivered-machine dossier last. No source prerequisite is claimed complete.

## Additional frozen-policy conflict: ownership scope

The frozen Stores Full Schema Guideline, paragraphs 116 and 325, requires strict FIFO within company, item and ownership/value pool, with no retrospective recalculation. The only installed consume_fifo_for_issue definition (VendorBillCostingSql.cs) takes its advisory lock by company/item and selects layers by company/item, without an ownership predicate. No later replacement was found.

This is a source-level conflict with frozen policy, not a witnessed cross-ownership transaction failure yet. The new report groups existing layers by receipt ownership; that does not prove consumption stayed within the required pool. The ownership correction and actual cross-ownership refusal witness belong to the current Item 15 work. No historical postings have been rewritten.
