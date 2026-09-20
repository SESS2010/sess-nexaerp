# Fresh-company rehearsal: findings #18, #21, #22, #23

Recorded 20 September 2026. Source of truth for these findings is the test
`FreshCompanyReachesAvailableStockThroughSeededStoresAuthority`
(`tests/SESS.NexaERP.Tests/FreshCompanyStoresFoundationTests.cs`).

## What the rehearsal is

A principal-provisioned fresh database (no trial script), migrated 0→current, real
items loaded by the checked-in `legacy-item-import-2026-08-29.sql` as
`nexa_erp_owner`, then everything else done through the API by seeded actors under
real page permissions and real operational scopes:

1. Stores Manager creates the warehouse, one rack per condition, the three condition
   locations (AVAILABLE, QC_HOLD, PENDING_RETURNABLE_DC) and the Stores category
   route.
2. Stores count → Accounts value → Technical Director authorize posts opening stock
   into the AVAILABLE location.
3. IT Manager uploads the GST certificate and creates the vendor; Accounts Manager
   verifies commercial terms; Managing Director approves (seeded
   `VENDOR_FINAL_APPROVER` policy).
4. Purchase Manager creates the vendor qualification; TD verifies; MD approves.
5. Accounts Manager creates the GST rule for the item's HSN; MD approves.
6. QC Manager creates the inspection policy; TD approves.
7. Purchase Executive raises the requisition; Accounts verifies and approves; Stores
   Executive stock-checks; RFQ, single-source invitation, quotation entered on the
   vendor's behalf; TD technical verification; comparison recommended and approved;
   PO created, approved, issued.
8. Stores Executive records the gate entry and the GRN and finalizes it; the GRN
   resolves the route Stores created.
9. QC Manager accepts the lot into the AVAILABLE location Stores created.

Every database tested before this one had been built by the development-only trial
script, which bypasses the API. Each finding below is a step the API could not take
on a database nobody had walked.

## #18 — Stores could not create a warehouse or rack

The operating grant on `masters.warehouses` and `masters.rack-bins` belonged to
`STORE_HEAD`, a legacy role held by nobody, plus IT_MANAGER, TD and MD.
`STORES_MANAGER` had no row on either page; `STORES_EXECUTIVE` had an all-false row
on warehouses. Condition locations (`masters.warehouse-condition-locations`) were
already Stores' to create — the AVAILABLE location was a symptom, not the cause.

Correction: `20260920090000_StoresWarehouseRackGrants` gives STORES_MANAGER the
operating grant STORE_HEAD held (view, create, update, submit, resubmit, print,
download, export, upload, audit history — not approve) and STORES_EXECUTIVE view.
It refuses to run if STORE_HEAD is still assigned, audits each change, and rolls
back only to the exact prior rows. Approval stays with the Technical Director.

## Stores category routes had no API

`store_category_routes` decides where each item category is held for QC, where its
rejected quantity waits, and where accepted stock lands by default. The GRN resolves
exactly one effective route per company and category. Only the trial script ever
wrote the table, so a real warehouse in a fresh company could not receive goods.

Correction: `/api/v1/rev869a/configuration/store-category-routes` (create, list,
close) under a new page `masters.store-category-routes`, whose grants
`20260920100000_StoreCategoryRoutePage` copies role-for-role from the sibling
condition-location page. Creation requires effective same-company QC_HOLD,
PENDING_RETURNABLE_DC and AVAILABLE locations in one warehouse and refuses an
overlapping effective route for the category.

## #21 — no vendor could be approved

Final vendor approval requires Accounts commercial verification, and
`POST /api/v1/masters/vendors/{code}/verify-commercial` accepted only the role
`ACCOUNTS_HEAD`. Role governance has retired that role (not employee-assignable)
and names `ACCOUNTS_MANAGER` as its replacement; a temporary cover cannot even be
granted for it. The endpoint never adopted the successor, so in a fresh company no
vendor could reach Approved and no RFQ could invite one.

Correction: the endpoint accepts the governed successor and is governed by the
vendor page (`masters.vendors:verify`) rather than the qualification page.
`20260920110000_AccountsVendorCommercialVerificationGrant` gives ACCOUNTS_MANAGER
view, verify, request-clarification, print, download, commercial values and audit
history on `masters.vendors`, refusing to run if governance no longer names
ACCOUNTS_MANAGER as the ACCOUNTS_HEAD replacement. Approval remains with the
policy-named final approver (Managing Director). The pending-approvals report still
labels unverified vendors `ACCOUNTS_HEAD`; that display is a follow-up.

## #22 — commercial verification returned 500

The same endpoint wrote its `ControlledConfigurationHistory` without `CompanyId`,
a required foreign key, so the save threw and the client received 500. The
re-verification path already resolved the company; verification now does the same
and returns 400 if the company cannot be resolved.

## #23 — QC could never pass on API-built topology

`FinalizeQcInspection` required a PENDING_RETURNABLE_DC condition location on the
same rack as the QC hold. A rack maps exactly one condition through the API, so no
API-built topology could satisfy it; only the trial script, which writes both
conditions onto one rack directly, ever had. Every customer installation would have
failed at its first inspection.

Correction: QC resolves the pending-return location from the receipt line's route
snapshot (`StoreCategoryRoute.PendingReturnConditionLocationId`, which exists for
exactly this purpose), requiring it to be effective and in the hold's warehouse. The
trial routes already name a distinct pending-return location, so existing fixtures
are unchanged. The GRN custody guard's same-rack clause applies only to
excess-rejected receipt custody, which the current GRN never produces
(over-receipt is refused).

## Observations recorded, not changed

- `IT_MANAGER` holds view only on `purchase.requisitions`, and the requester must be
  the caller. Department requesters therefore cannot raise their own requisitions;
  Purchase raises them. The fixture's assumption that a department manager creates
  the requisition was wrong, not the grant.
- `STORES_MANAGER` has no `inventory.grn` grant at all; only `STORES_EXECUTIVE`
  creates and finalizes receipts. Whether the Stores Manager should at least view
  receipts is decided with finding #12 (QC's missing GRN read), not here.
- Every vendor created through the API needs an uploaded GST certificate. On a
  rebuild the certificates carry over from `advance.vendor_attachments` (bytea rows
  with their own GUIDs, no outward foreign keys) and the vendor workbook's
  `AttachmentMetadataJson` references those GUIDs.

## Evidence

Focused Debug run of the rehearsal: passed, 2 m 04 s. Full Debug and Release suites
with TRX are recorded under `local-evidence/finding-18-19` and cited by each commit.
