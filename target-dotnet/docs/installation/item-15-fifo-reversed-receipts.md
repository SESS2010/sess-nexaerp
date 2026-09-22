# Item 15 — reversed GRN and FIFO eligibility

Source finding: the FIFO report already excludes finalized reversed GRNs as of the selected report date. The allocator introduced with immutable return restoration did not apply that same exclusion. A GRN's physical source may differ from an issue's accounting FIFO source, so "no QC on this receipt" does not prove its cost layer is unused.

Migration 20260914100000:
- Retain OPENING_LANDED eligibility; require a normal, finalized, unreversed receipt for GRN-backed layers.
- Before a finalized GRN reversal is inserted, take the existing company/item FIFO locks in sorted order.
- Refuse reversal while any original layer has consumption remaining after its recorded restorations.
- Refuse reversal while the original GRN has an active accepted bill.
- Refuse migration over historical reversed receipts that still retain those downstream facts. Do not rewrite historical layers, consumptions, bill evidence or receipt rows.
- Retain the existing report's as-of exclusion; no duplicate valuation query is introduced.

The passing witness completes real partial receipt/reversal/replacement through the invoice workflow. It then clones only the validated disposable advance_parser database and submits explicitly labelled costing-function inputs through the restricted runtime principal. An issue larger than eligible stock must fail even though the reversed layer would otherwise fill the gap. Consuming exactly the eligible amount must succeed; subsequent reversal of a consumed receipt must fail. The original purchase witness stays unchanged.

Targeted invoice/FIFO-boundary verification passed in both Release and Debug (report5-page-permissions-release.trx and report5-page-permissions-debug.trx: 3/3 each). Full routine Release passed 836/836, zero failed/skipped, 23m42s. Both builds passed with zero warnings/errors. The renamed backup copy upgraded from 100 to 102 and installer reconciliation/status passed; the protected live-name chain remains blocked at CommandReceiptReplay. No live database was touched.
