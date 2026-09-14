# Item 29 open purchase orders and delivery coverage

Status: implemented and verified in Release and Debug. This is a backend projection; no frontend or complete Item 29 claim is made.

GET /api/v1/dashboards/purchase/open-orders returns open issued commitments, delivery coverage and a filtered line list. Filters: vendorId, currency, rootPurchaseOrderId, overdueOnly, page (default 1), pageSize (default 100, maximum 1000). Filters select the detail list; the overview covers the full authorized scope.

The projection chooses the latest actually issued revision within each company/root. A newer draft or approved amendment does not replace that commitment until issue. Remaining quantity subtracts finalized normal, unreversed receipts across the root and commercial-comparison line, with requisition/handoff/item/UOM provenance checked. Value is remaining quantity times the latest issued line payable per unit, separated by native currency. This is committed payable value, not final landed INR or bank cash. Age starts at the root's first issue; both first and latest issue timestamps are exposed.

Complete identifies whether open totals are reliable. A cancelled latest-issued PO has no open commitment. A later cancelled, never-issued current revision with outstanding quantity produces CANCELLED_UNISSUED_AMENDMENT, identifies the root/PO and leaves aggregate counts and amounts unset. The report cannot decide whether that cancellation extinguished the earlier issued commitment. Already fully received commitments remain closed when a later unissued amendment is cancelled. Missing current revision, inconsistent line provenance and over-received quantities also require reconciliation.

DeliveryComplete separately describes delivery-date coverage. A quotation's structured promised date is usable only while it agrees with the comparison snapshot and the selected PO's delivery terms still agree with the quotation. Changed terms produce CONFIRMATION_REQUIRED and retain the quoted date for reference. No date is inferred from free text. Missing confirmation leaves overdue totals unset and reports DeliveryDateUnconfirmedPoCount; confirmed overdue lines remain filterable.

The frontend must display incomplete/source-issue states instead of converting null totals to zero. Each row identifies the root, issued/current revision, PO/line, vendor, item/UOM, ordered/received/remaining quantities, currency/value, dates and delivery state. Source identifiers support drill-down through the same filtered endpoint; opening another page retains that page's own permission checks.

The dedicated dashboards.purchase-open-orders page derives eligible PM/TD/MD view/commercial grants from existing PO permissions. One private SQL statement checks live company membership, employee/role status, role activation, operational department/warehouse/rack/owner scope and explicit director cross-scope. Runtime receives EXECUTE only. Installer provision/status and migration 20260914050000 protect ownership, ACL and definition. Permission rollback refuses changed runtime grants. No financial rows are written.

Access denial returns 403 DASHBOARD_ACCESS_DENIED; invalid page/currency returns 400 DASHBOARD_REQUEST_INVALID. A permitted empty scope returns zero counts and empty arrays. A permitted scope containing an unresolved source issue returns the explicit incomplete response above.

Release verification:
- Basic full three-band PR-to-Actual-BOM flow: after issue, LOW has one overdue open PO / 4720, TD one not-overdue / 5900, MD one not-overdue / 118000.01. Each closes after receipt. LOW's past promised date is supplied through the quotation API.
- Nonempty amendment: original issue, draft amendment and approved-unissued amendment retain the original issued PO and 4720 commitment. Issuing changed delivery terms selects revision 2, preserves first-issue age and 4720, but sets DeliveryComplete false and overdue totals null. The full flow finishes with zero open POs after receipt.
- A fully received issued amendment stays closed. A later approved, unissued amendment also stays closed after actual director cancellation.
- Outstanding cancellation: one issued PO / 4720 stays visible until a later unissued amendment is cancelled, then totals are unset with the reconciliation code. This terminal cancellation case intentionally leaves its GRN DRAFT, with zero FIFO layers and posting batches. Other witnesses complete the full three-band chain.
- Reads make one SQL call and leave all recorded audit/request/receipt/movement/PO/GRN counts unchanged. Scope/company/role/commercial/filter/paging checks include actual restricted-runtime HTTP and direct financial-table SELECT refusal.
- Migration tests cover install, repeated principal provisioning, altered authority refusal, rollback and reapply. The associated cancellation fix has separate migration and exact row-count/replay tests.

The expanded first Release run passed five facts and failed two because the historical cash fixture attempted to downgrade an older full-body history guard while the new cancellation patch was installed. The fixture now removes and restores that dependent patch around its disposable historical downgrade. No production guard was weakened. Corrected Release run passed all three affected/additional facts, zero failed/skipped, in 11m36s. Combined coverage is eight distinct passing facts across the two runs, not a full suite. Release builds were clean: 4m34.40s and 32.86s.

Evidence: item29-open-orders-cancellation-release.trx (5 pass / 2 fixture failures, 16m03s), item29-open-orders-revision-correction-release.trx (3 pass / 0 fail), and purchase-open-orders-{basic,nonempty,cancelled,outstanding,closed-amendment}-verified-release.json. The earlier genuine cancellation HTTP 500 is retained in item29-open-orders-scenarios-release.trx; its authorization fix is documented separately.

Limits: no production-volume, multi-currency procurement or every cancellation/reversal scenario claim. Billed-before-received still lacks its source workflow. Vendor quality depends on item 23. Final FIFO/landed-INR prerequisites remain as recorded under items 11 and 15.

Debug verification: item29-open-orders-debug.trx passed all eight facts, zero failed/skipped, in 19m18s. Build passed with zero warnings/errors in 4m48.28s. Matching verified-debug artifacts preserve each report scenario and cancellation row counts. Every report-read before/after snapshot is unchanged. These are targeted checks, not a full-suite or completed Item 29 claim.
