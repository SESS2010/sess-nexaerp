# Fitment reversal and machine cost — Item 15 source finding

A fitment reversal cancels machine cost. It does not leave the reversed component charged to that machine.

The reversal command appends a REVERSAL Actual BOM entry containing the negatives of the original fitted quantity, accepted material value, allocated charges and total. The original FITMENT entry is retained. The read path also subtracts any subsequent landed-cost adjustments attached to the original entry. The net quantity and cost of that reversed fitment are therefore zero.

Both the operational Production BOM comparison and commercial offer-versus-actual comparison use these signed entries. The frozen commercial baseline is retained; reversal changes the actual side.

The stock reversal restores the exact original engineer custody and provenance. Material remains issued until Stores accepts a return. The original FIFO issue consumption consequently remains at fitment reversal. This is the deliberate separation described by the user: machine fitment and outstanding issue are different events. No FIFO restoration should be introduced merely for reversing fitment.

Source:
- Infrastructure/Persistence/Migrations/ComponentFitmentActualBomSql.Reverse.cs: negative Actual BOM entry and compensating engineer-custody movement.
- Infrastructure/Stores/EfFitmentActualBomService.cs: reversal of later landed adjustments, signed totals, operational and commercial comparisons.
- Tests/PurchaseFlowEndToEndPostgreSqlTests.cs: real governed fitment, reversal, replay and re-fit.

Initial isolated Release witness passed (item15-opening-reversal-release.trx: two tests, zero failures/skips, 5m43s including opening-stock reports). The machine showed material 1416.00 plus charges 3.60 before reversal, zero material/charges/total afterward, and 0.35 restored engineer custody (0.30 reversed fitment plus 0.05 already held). The original FIFO consumption records were unchanged. Commercial actual cost was zero; its frozen baseline stayed 90.00. Verified: Release and Debug builds succeeded with zero warnings/errors. The final targeted runs each passed two tests, zero failures/skips: item15-opening-reversal-verified-release.trx (5m01s) and item15-opening-reversal-verified-debug.trx (5m28s). Reversal full-flow durations were 4m26.424s / 4m50.310s; opening-stock durations 35.519s / 38.415s. These are two distinct tests in two configurations, not a full-suite run. Reversal values and custody agree; opening quantities, values and business-row counts agree. Generated fixture ownership UUIDs differ between the disposable databases.

The separate Stores-return correction is now implemented and has passed a targeted Release witness (item15-fifo-partial-retry-release.trx: one test, zero failures/skips, 4m37s). Reversing the original 0.30 fitment gives zero machine cost; confirming the retained 0.20 gives material 944.00 plus charges 2.40 = 946.40. Stores acceptance of the removed 0.10 appends FIFO restoration quantity 0.10 and original material value 472.00. Closing FIFO valuation increases by 473.20 including allocated charges, while retained machine cost stays 946.40. Original layers and consumptions are unchanged, replay creates no additional business records, and excess return is refused. This correction is still undergoing ownership/concurrency and upgrade validation; it is not a fitment-reversal defect.
