# Item 15 — immutable FIFO return restoration

Source authority: frozen Stores Full Schema Guideline §2.1 ruling 2, §6, V1 and V5; Post_Session_Plan's fitment-reversal and FIFO-return decisions; the user's explicit 14 September reverse-creation-order instruction. The full frozen guideline was read during the specification audit. Its proposed paragraphs are not treated as approved policy.

## Behavior

A Stores return unwinds the issue line's recorded consumptions in reverse creation order. A new append-only creation ordinal records future consumption order, including equal timestamps. For old consumptions the migration reconstructs within-issue-line order from the exact baseline writer's layer ReceivedAt/Id loop, marks that reconstruction, and refuses a changed writer. No policy switch, weighted-average method, or newly selected return layer exists.

Each restoration references the original consumption and accepted return line, preserves the original unit rate, and records quantity, restored value, effective acceptance time, actual recording time and actor. Cumulative rounding cancels the exact original value on full restoration. Original layers and consumptions are immutable.

Restoration and physical return commit in the same ordinary transaction. Excess return, already fully restored consumption and incomplete accepted-return costing are refused. Replay appends nothing. The allocator and later landed-charge split use net consumption after restorations. Allocator eligibility includes ownership and currency; physical serial provenance does not choose the accounting layer.

Fitment reversal remains distinct. It negates machine Actual BOM cost and restores engineer custody. The issue remains outstanding, so reversal alone does not restore FIFO. A later accepted Stores return does. Actual BOM continues to use the physical component's accepted-bill allocation, separately from FIFO issue accounting.

## Upgrade and authority

Migration 20260914080000 adds fifo_consumption_creation_order and fifo_cost_restorations, their immutable/completeness guards, and replacement controlled costing/report functions. It does not modify an applied migration resource.

Historical accepted returns receive explicitly marked reconciliation restorations. EffectiveAt retains the original acceptance time; RecordedAt records the actual migration time. The witness compares complete original layers, consumptions, returns, return lines/history, stock movements and landed adjustments before/after and proves no rewrite.

Historical ownership mismatch refuses migration rather than rewriting consumption. Empty rollback/reapply is supported; rollback refuses retained creation-order/restoration evidence. Runtime cannot directly read or write the private ledgers or execute the private restoration helpers. Installer provisioning handles table-owned identity sequences through their table ownership.

A historical-data test exposed deferred trigger events blocking ALTER TABLE OWNER after reconciliation. Ownership now precedes reconciliation. An earlier fresh-install test exposed ownership changes attempted directly on a table-owned identity sequence; provisioning now lets the table carry that sequence's ownership. Failed results remain in local evidence.

## Witnesses recorded so far

- Mechanical recorded-order test: 1 at 100, then 1 at 200 with equal timestamps and deliberately opposing UUID order. Return 0.5 restores 100 against the 200 row, leaving net issue cost 200. Further partial return crosses to the older row correctly. Excess/full-restored and immutable-history refusals passed in Release.
- Governed partial-fitment/return Release witness: reverse original 0.30 fitment to zero; retain 0.20 with 944 material plus 2.40 charges = 946.40; accept return 0.10. Restoration material value is 472; FIFO report value rises 473.20 including charges. Machine actual stays 946.40. Original rows and frozen baselines remain unchanged; replay/excess checks pass. item15-fifo-partial-retry-release.trx: 1 passed, 0 failed/skipped, 4m37s.
- Latest Release run: item15-fifo-concurrency-upgrade-retry-release.trx, 3 passed, 0 failed/skipped, 8m22s. Historical upgrade 3m12.452; restricted FIFO ownership/concurrency 3m51.897; private migration/empty rollback 1m17.803.
- The FIFO concurrency fixture clones the validated disposable witness database. Its extra issue headers/lines are explicitly unposted inputs to the real restricted costing function, not claimed completed API stock issues. It consumes restored availability down to one unit, observes two blocked calls, proves one commit/one serialization failure, refusal on retry, zero final net remainder and unchanged physical movement count. The parent purchase witness remains untouched.
- Wrong ownership pool is refused despite available layers in the original account. No cost consumption is appended for that rejected input.
- The earlier governed opening witness proves OPENING_BALANCE and OPENING_LANDED in reports using 10 units at INR 25, value INR 250, with one SQL statement and Excel/drill-through checks.

Debug verification passed: item15-fifo-complete-debug.trx, six tests, zero failed/skipped, 11m52s. This document is not a full-suite pass or completion of Item 15. Reports 5 and 10, final scale measurement and subsequent Keycloak verification remain.
