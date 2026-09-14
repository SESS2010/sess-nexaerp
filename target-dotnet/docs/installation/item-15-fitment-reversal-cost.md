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

The separate Stores-return gap remains: acceptance currently posts physical returns without FIFO restoration entries. That correction follows the approved immutable restoration mechanism. It is not evidence of a fitment-reversal defect.
