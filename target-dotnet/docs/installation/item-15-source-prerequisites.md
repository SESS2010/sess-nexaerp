# Item 15 — source prerequisites awaiting a decision

This is a proposal, not an implemented accounting or delivery policy. Item 15's seven unaffected reports continue independently.

## FIFO return attribution

Observed: accepted material returns append physical movements, but no FIFO cost-credit record. The existing purchase witness restores 1.00 serialized unit plus 0.60 and 0.03 non-serialized units physically: 1.63 in total. Its accounting FIFO consumes two complete layers; the serialized physical source and accounting cost layer are deliberately different. A closing FIFO valuation cannot silently treat those units as absent.

The proposed correction is append-only credits linked to the accepted return line and the original issue's FIFO consumption rows. It must retain original consumption history, keep company/item/ownership boundaries, lock the same cost pool as consumption, refuse credits exceeding the original net consumed quantity, and replay idempotently with the return.

The open policy choice is which original layers a partial return restores. Example: an issue consumed one unit at 100 and one at 200. Returning half a unit restores value 100 if the latest consumed layer is unwound first, or 50 if the oldest is restored first. A weighted average would hide this decision and is prohibited.

The proposed default is latest consumed layers first, preserving the remaining original issue's FIFO prefix. It is awaiting the Technical Director's decision. Accepted-bill cost adjustments and reversals must remain traceable without rewriting historical consumption.

## Invoice received before goods

Observed: the current vendor-bill creation path requires a finalized GRN and its received quantity. An empty billed-not-received report would prove only that this workflow cannot record the situation.

A minimal prerequisite would record a supplier invoice against an issued PO before receipt, with company/vendor, invoice number/date, PO line, quantity/unit, currency and source evidence. Recording the supplier document must not pretend that three-way matching or payment approval has already happened. Receipt matching and accepted-bill linkage would be governed, auditable operations with duplicate, cross-company/vendor/currency, excess-quantity and replay refusals.

The precise matching and correction lifecycle needs to be agreed before making this a financial source. No draft or quotation will be relabelled an accepted bill.

## Delivered machine

Observed: the governed machine witness reaches FAT readiness; it does not record delivery. An ancestry dossier must identify the actual job/machine and link delivery evidence before calling it delivered.

The existing DC specification requires a customer signature for machine delivery. Its custody and commercial rules cannot be bypassed by adding a bare Delivered flag. Bringing the necessary delivery behavior forward would change the requested schedule, which places Item 17 later and excludes Items 35–50. The later Stores Completion document adds DC commercial axes under Item 40, so that later scope must not be silently introduced either.

The minimum proof must retain the applicable DC/job/machine/customer identities, a governed delivery event and signed evidence, then traverse actual component fitments to their GRN, vendor, accepted bill/allocated charges, QC and applicable approvals. FAT readiness is not delivery, and foundation rows alone are not authoritative delivery evidence.

The pending scheduling question is whether to bring these report prerequisites into Item 15 or preserve the original order and leave their proofs explicitly pending. No source prerequisite is claimed complete.

## Additional frozen-policy conflict: ownership scope

The frozen Stores Full Schema Guideline, paragraphs 116 and 325, requires strict FIFO within company, item and ownership/value pool, with no retrospective recalculation. The only installed consume_fifo_for_issue definition (VendorBillCostingSql.cs) takes its advisory lock by company/item and selects layers by company/item, without an ownership predicate. No later replacement was found.

This is a source-level conflict with frozen policy, not a witnessed cross-ownership transaction failure yet. The new report groups existing layers by receipt ownership; that does not prove consumption stayed within the required pool. FIFO completion is paused. A scheduling decision has been requested: bring the ownership correction and an actual cross-ownership refusal test into Item 15, or perform it in Item 25 and keep FIFO verification pending. No historical postings were changed.
