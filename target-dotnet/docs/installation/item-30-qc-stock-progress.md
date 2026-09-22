# Item 30 current QC and rejected stock

Status: implemented; all three targeted facts pass in Release and Debug. Parent: cb49ee6. This is the final dashboard component before the user's report-first priority; the complete Stores dashboard is not claimed.

GET /api/v1/dashboards/stores/qc-stock returns QC_HOLD and PENDING_RETURNABLE_DC tiles and current stock rows. Optional queue, documentId, page and pageSize (1..1000, default 100) filter details. Tiles cover the full authorized scope. Counts are distinct GRN lines; a line may have multiple allocation, location, ownership, custody, provenance or serial rows. Quantities retain the receipt UOM.

The ledger determines remaining stock. GRN receipt movements set GoodsReceiptLineId, while QC transfer legs leave that optional field null. Both preserve GoodsReceiptLineLotAllocationId; the projection resolves receipt lines through that allocation. An existing inspection does not imply that no stock remains on hold.

QC lateness uses stored GoodsReceipt.QcDueAt and the current timestamp. ReceiptAgeDays is local calendar age from ReceivedAt, not days overdue. Rejected material awaiting returnable DC is not overdue QC. The DC workflow remains a separate prerequisite.

ReceiptProvisionalValue is remaining quantity times the stored receipt unit rate, grouped by native PO currency. It excludes later bill/landed-cost adjustments and is not final FIFO or bank-actual INR valuation. Values are null without commercial permission; permitted empty monetary groups are empty lists. There is no mixed-currency total.

The dashboards.stores-qc-stock page derives view/commercial grants from inventory.grn for eligible Stores/TD/MD roles. Live authorization requires both pages, active company membership, effective role/activation and operational scope. Scope uses actual stock warehouse/rack and the PO requesting department. The existing Stores Manager GRN denial remains a refusal; no command permission was added. Invalid requests return 400 DASHBOARD_REQUEST_INVALID; denied access returns 403 DASHBOARD_ACCESS_DENIED.

One private SECURITY DEFINER statement provides the projection under a single snapshot, with fixed search_path, owner/definition/ACL guards and Installer runtime EXECUTE reconciliation. Runtime receives no direct financial-table SELECT.

## QC discrepancy defect corrected

The real all-discrepancy inspection returned HTTP 500: the service intentionally posted no stock movement, but its deferred database guard required one batch. The witness reproduces the prior guard failure and exact diagnostic, verifies full rollback, restores the fixed guard and retries the same request.

Migration 20260914065000 expects zero disposition batches only when accepted and rejected quantities are zero and discrepancy equals inspected quantity. Any accepted/rejected disposition still requires exactly one batch. It checks the predecessor function body, owner, ACL and deferred trigger. Down refuses retained no-movement inspections.

Correction of a no-movement revision skips reversal of a nonexistent batch. Revisions that moved stock retain their existing reversal path. Original inspections, revisions and movements remain in history.

## Verified Release evidence

Migration/install/reprovision/ACL tamper/down/reapply: 1 passed, zero failed/skipped, 1m18s (item30-qc-stock-guard-release.trx). Full flow and discrepancy/correction: 2 passed, zero failed/skipped, 5m41s (item30-qc-stock-runtime-fixed-release.trx). Individual runtime durations: full flow 3m18.608s; discrepancy 2m22.599s. Final Release build: zero warnings/errors, 3m44.20s. These are targeted facts, not a full suite.

The complete three-band PR-to-Actual-BOM flow observes:
- Receipt finalizations: held line counts 1, 2, 3; rejected counts 0.
- LOW QC: held 2, rejected 1.
- TD QC: held 1, rejected 1.
- MD QC: held 0, rejected 2.
- MD concession: held 0, rejected 1.

All seven dashboard read snapshots are unchanged. Every row reconciles quantity to ledger dimensions and provisional value to receipt rate. The test proves overdue LOW receipt, stored deadlines, commercial redaction, live permission/scope withdrawal, company/role refusal, paging/filtering, restricted-runtime HTTP and one database command. The disposable TD has an explicit IT reporting scope; production predicates are unchanged.

Discrepancy edge: initial stock has 1 movement / 1 batch / 0 inspections / 0 revisions. The rejected old-guard command changes nothing. Successful pure discrepancy has 1 / 1 / 1 / 1, retains held quantity 1 and is absent from the old inspection queue. Half resolution has 3 / 2 / 1 / 2 and held quantity 0.5. Full correction has 7 / 4 / 1 / 3 and held quantity zero. All states retain 40 audits and 24 requests/receipts; reads and final replay add no rows. This is a terminal edge, not a claimed complete machine chain.

Verified Release artifacts: stores-qc-stock-states-verified-release.json and stores-qc-stock-discrepancy-verified-release.json. Earlier actual failure and migration-syntax runs remain separately retained. Debug build passed with zero warnings/errors in 5m10.29s; all three matching tests passed, zero failures/skips, in 6m54s (item30-qc-stock-debug.trx). Seven stage counts/tiles and all discrepancy/correction snapshots match Release exactly. Verified Debug artifacts are retained alongside Release.

No frontend, foreign-transaction proof, multi-allocation scale benchmark or completed returnable-DC workflow is claimed.
