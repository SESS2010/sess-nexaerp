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

## #12 — QC could not read the receipt it inspects

`QC_MANAGER` held no `inventory.grn` grant; the field workaround was a STORES_ASSISTANT
support cover. `STORES_MANAGER` held none either, so the manager could not see the receipts
their executives post. `20260920120000_QcGoodsReceiptRead` grants both roles view only (no
download, no create, finalize or reverse, which stay with STORES_EXECUTIVE): the rehearsal
shows both reading the GRN detail and both refused on finalize.

## Item permission move (decided by the Technical Director)

The item approval and merge services already required STORES_MANAGER or
PURCHASE_MANAGER, but the `masters.items` page grants belonged to the retired heads and
the directors, so neither manager could reach either operation (14 reachability
diagnostics). `20260920130000_ItemMasterApprovalAuthority` gives both managers the item
master operating grant including approval, refusing to run while STORE_HEAD or
PURCHASE_HEAD is still assigned.

Rules now enforced by the services:

- Stores Manager or Purchase Manager approve item master records; the Technical Director
  is refused an ordinary approval even though the page grant remains.
- A correction to an approved record returns it to approval (`ApprovalStatus =
  PendingApproval`, status unchanged); while pending the item is unavailable to
  operations that require an approved item, the same convention as vendor
  re-verification.
- A correction to a record **more than one month old since creation** (code locked and
  `CreatedAt` older than one month) can be approved only by the Technical Director; a
  first approval never needs the director, whatever the record's age.
- Duplicate merge is the Technical Director's alone.
- Location changes are Stores operations on stock, not item master corrections, and are
  untouched.

The rehearsal proves each rule with the seeded actors (self-approval refused, TD refused
on a young correction, Purchase refused on an old one, TD accepted, merge refused for
Stores and accepted for TD).

### Maker-checker (decided by the Technical Director, 20 September 2026)

The master maker-checker rule (`MasterEndpointHelpers.IsSelfApprovalAttempt`, shared by
items, vendors, customers, warehouses and rack-bins) excluded the record's creator **and
its last editor for the record's life**. With two approving roles that deadlocked a
record: an item the Stores Manager created and the Purchase Manager corrected could be
approved by nobody but the Technical Director, who is refused unless the record is a
month old.

Decision: **only the maker of the current pending change is excluded**; everyone else
holding the grant can approve; the director exceptions are unchanged. The maker is the
actor of the latest maker action in the master approval history (`Submit`, `Resubmit`,
`Correct`, `ControlledDetailsChanged`), falling back to the last editor and then the
creator for records without such history. Checker actions (Accounts verification,
clarification requests) do not make their actor the maker, so a vendor the Managing
Director submitted stays refused to the Managing Director after Accounts verifies it.
The rehearsal proves the decided example: Stores creates, Purchase corrects, Stores
approves the correction; the corrector and the director are refused.

Other lifetime creator exclusions found, **not changed**:

- Vendor qualification (`Rev869AConfigurationEndpoints.ChangeVendorQualificationLifecycle`):
  the creator can never verify or approve the qualification. With one final approver this
  is the reachability diagnostic "MD-created vendor qualification has no independent
  approver". Same class as the rule above; needs the same decision.
- Purchase transactions (requisition, RFQ, purchase order): the Rev869B history trigger
  and `EfPurchaseApprovalWorkflowService` refuse the creator's approval for the
  transaction's life. A transaction's creator is the maker of the whole record, so this
  is the ordinary rule, not a deadlock.
- QC inspection policy and QC concession: the creator cannot approve or decide their own
  record (`DecidedByEmployeeId <> CreatedByEmployeeId` is a check constraint on
  concessions). The approver role (Technical Director) differs from the creating role
  (QC), so no deadlock unless the director creates the record.

## #24 — no item merge had ever succeeded

`guard_estimated_bom_governance` guards both `estimated_bom_history` and
`item_merge_aliases`. Its Estimated BOM clause read `NEW."Action"` in the same expression as
the table-name test; PL/pgSQL prepares the whole expression, and `item_merge_aliases` has no
`Action` column, so every merge insert failed with `42703 record "new" has no field
"Action"` and the API returned 500. No test had exercised the merge endpoint end to end.
The same clause also named STORES_MANAGER/PURCHASE_MANAGER as the merge authority, which
the item permission move contradicts.

Correction: `20260920140000_ItemMergeDirectorAuthority` rewrites the installed function body
(refusing if either clause is not found exactly once), nesting the Estimated BOM clause so
the column is read only on that table and naming FULL or TEMPORARY TECHNICAL_DIRECTOR as
the merge authority. Rollback restores both clauses and refuses once Technical Director
merge evidence exists; the rehearsal proves the refusal.

## The walk after AVAILABLE stock (20 September, evening)

`ProveOperationsAfterAvailable` continues on the same fresh database: consumable MIR →
issue by scan → custody → return; customer → customer PO → job order → Estimated BOM →
Production BOM → job MIR → issue → fitment → Actual BOM → FAT readiness; machine delivery
challan → customer signature → ancestry dossier; vendor bill → landed cost → advance →
payment; intercompany route options; the ten company reports with their Excel exports.
Each step by the seeded actor who owns it, under real page permissions and real
operational scopes. The customer PO had never been created through the API by any test
(fixtures wrote it to the database), and the test host had never mapped
`/api/v1/sales/customer-pos`; it does now.

## #26 — opening stock could never be issued

`CK_stock_movement_outbound_origin` (August) requires every `ISSUE_OUT` or `DISPATCH_OUT`
movement to carry `OriginGoodsReceiptLineId`. Opening stock (September) enters without a
GRN, so the first MIR against the ceremony's stock failed at issue with a check violation
and the API answered 500. Every unit posted by the PROPRIETORSHIP ceremony on 19 September,
and every unit PVT LTD will post at step 13, was unissuable. The FIFO consumption already
understood opening layers; only the ledger origin did not.

Correction: `20260920150000_OpeningStockIssueOrigin`. Stock movements and material issue
lines carry `OriginOpeningStockLineId` exactly as they carry `OriginGoodsReceiptLineId`;
opening receipts are their own origin (existing rows backfilled, `authorize_opening_stock`
writes it from now on); the outbound check accepts either origin; the issue and return
postings copy the opening origin from the issue line. The three functions are rewritten
from their installed bodies and refuse an unexpected body; rollback refuses once opening
stock has been issued. The rehearsal issues 3 of the 10 opening units, returns 1, and
checks the four outbound/return legs carry the opening origin and no GRN origin.

The full suite on `86d6767` (Debug 1016/1017, Release 1013/1014) caught what the focused
runs had not: on a database that already holds posted opening stock, the backfill of the
opening receipts' origin was refused by the append-only ledger trigger and the migration
failed. `ffc7966` suspends the user triggers for exactly that statement (provenance added
to rows that predate the column; nothing else changes) — the field database, with the
PROPRIETORSHIP ceremony posted, is exactly that case.

Superseded on the same evening (see the decisions below): a component drawn from opening
stock could not yet be **fitted** — the Actual BOM values a fitment from the accepted vendor bill of the issued
GRN line, and an opening line has no bill (its value is the opening carrying value).
Dispatch (`DISPATCH_OUT`, delivery challans) from opening stock is not exercised by this
walk either. The rehearsal fits purchased stock.

## Opening stock decisions of 20 September (evening) — built

1. **Value is ex-tax.** The template's `Rate` column is now headed *Unit Value Ex-Tax*;
   Accounts confirms the taxable value; recoverable GST is never entered.
2. **Template carries provenance, optional.** Version 2 of `opening-stock` adds vendor
   name, vendor bill number, bill date, purchase date, make, model, part number and
   remarks — filled where SESS knows them, blank where not. Delivered as
   `docs/installation/opening-stock-template-v2.xlsx` (also `GET
   /api/v1/master-data/opening-stock/template` once the database exists). Migration
   `20260920160000_OpeningStockProvenance` stores the eight columns on staging and
   opening lines; the staging function gains a 22-argument overload (the 14-argument
   signature stays as a wrapper so the installer contract and existing callers are
   unchanged); the installer grants and verifies the overload where present.
3. **Valuation resolves by origin.** `20260920170000_OpeningStockFitmentValuation`:
   Actual BOM entries carry either a GRN line or an opening stock line; an opening-origin
   fitment is valued at the line's confirmed ex-tax unit value with no charges and is
   `OPENING_CONFIRMED` at once; a GRN-origin fitment is valued from the accepted bill as
   before. Declared vendor/bill fields never value anything; no synthetic bill is created.
4. **Dossier distinguishes declared from accepted.** New `provenance` column:
   GRN → `Bill <n> - accepted, matched, paid|part-paid|unpaid` (or `GRN <n> - bill not yet
   accepted`); opening with a declared bill → `Bill <n> - declared at opening stock, not
   verified in this system`; opening without → `Opening stock, authorised <date> by
   <employee>`. The rehearsal asserts all three, and the GRN line's change to
   `part-paid` after the payment.
5. **FIFO: opening stock consumes first.** Opening layers are dated from the ceremony's
   count period end (the cutover date the Stores Manager records), never from the
   declared purchase date. The rehearsal posts 2 opening units and receives 3 of the same
   item: the first issue of 2 consumes only the opening layer (GRN consumption 0), the
   GRN layer is consumed only by the next issue. If the period end is entered later than
   the first GRN date the order would invert; the runbook's step 13 period is the cutover
   date, so it cannot.
6. **The normal path is rehearsed:** three opening lines (one with declared provenance),
   issue from both origins, fitment of both, the dossier rendering each, then the bill.
   **DISPATCH_OUT from opening stock cannot be exercised: no delivery-challan dispatch
   endpoint or service exists** (the ledger accepts `DC_DISPATCH` postings; nothing creates
   them; machine delivery moves no stock). Migration `150000` already accepts the opening
   origin on `DISPATCH_OUT` for when it is built.
7. **Vendor qualification maker-checker:** a qualification has no correction or
   resubmission path, so the maker of its pending change is its creator; the decided rule
   therefore changes nothing and the trigger (`rev869b_qualification_actor_binding`) is
   left as it is. The diagnostic's cause is a Managing Director creating a record that
   only the MD or TD may approve after the TD verified it: the record must be created by
   the Purchase Manager, as the rehearsal does.

## #27 — a fitted component still had to be "returned, consumed or held"

The return statement reconciled the whole issued quantity, ignoring quantity already
fitted into a machine. After fitting 1 of 2 issued units the engineer could not declare
the remaining unit returned without also declaring the fitted unit "consumed" or "still
held". `EfMaterialIssueService.CreateReturnAsync` now subtracts fitted quantity net of
reversals, the same figure the issue detail and FAT reconciliation report. The opt-in
FIFO partial-fitment witness declared the fitted 0.20 as consumed; it now declares only
the outstanding 0.15.

## #28 — an unknown MIR purpose answered 500

`CK_mir_lifecycle` restricts `Purpose` to seven codes; the service validated the situation
and destination but not the purpose, so an unknown purpose reached the database and came
back as an internal error. The service now refuses it with 400 and the allowed list.

## #25 — intercompany routes cannot be prepared on a fresh database

A route needs a seller site, a buyer site and approved customer/vendor company
relationships. `company_sites` and `customer_company_relationships` have no API and no
seed: on a fresh database `GET /stores/intercompany/routes/options` returns two companies,
two GST registrations, **no site and no customer**, so no route can be proposed and the
sale path (route → published PO → GST invoice evidence) is unreachable. The DC-only
transfer path has no endpoint at all (A1, not started). The rehearsal asserts the empty
options and stops there. Same class as the Stores category route finding: configuration
that only SQL could ever create; it belongs in the configuration-export requirement.

## Observations recorded, not changed

- Report **export** was a separate grant from view and had stayed with the retired heads.
  Decided 20 September: the role that can view a report can export it.
  `20260920180000_ReportExportFollowsView` gives every employee-assignable role with view
  on a `reports.*` page the export right (23 rows on the seed: engineer-custody,
  movement-roll-forward, stock-balance for the Stores executive/assistant;
  pending-approvals for ten roles; purchase-register for seven), one receipt each, exact
  rollback. Portal roles (customer, vendor) are untouched. The rehearsal now exports every
  report as the role that viewed it.
- `billed-not-received` and `grni` legitimately return no rows at the end of the walk: the
  only bill follows its receipt, and it is accepted. GRNI shows the receipt before the
  bill is accepted (asserted).
- Estimated BOM preparers were hard-coded by employee code (`SESS-04`, `SESS-05`) in
  `EfEstimatedBomService.RequirePreparerAsync`. The rule is now by role: DESIGN_ENGINEER,
  TECHNICAL_DIRECTOR and TECHNICAL_SUPPORT_MANAGER (the role those two employees hold; a
  SUPPORT assignment of it may prepare), and
  `20260920190000_TechnicalSupportEstimatedBomGrant` gives that role the preparer page
  grant the Design Engineer holds (view, create, update, submit; never approve). The
  rehearsal has SESS-04 open and submit a revision under real page permissions and refuses
  a plain Service Engineer. Observed on the way: a new Estimated BOM revision carries no
  price, and an item with no accepted-bill purchase rate needs a stated value before
  submission. Decided 20 September (evening): **offer, never impose, and record which was
  used.** The line view now carries `SuggestedUnitValue` / `SuggestedValueSource` (the last
  accepted bill's landed rate when one exists, else the opening-stock carrying value);
  `EstimatedBomLineInput.UseSuggestedValue` accepts it; a typed value records `ENGINEER`;
  nothing is prefilled silently (the previous silent fill from the last accepted bill, and
  the silent refresh at approval, are gone). `ValueSource` is stored
  (`20260920200000_EstimatedBomValueSource`, existing lines classified from what was
  recorded; the draft-line replacement function rewritten from its installed body), shown on
  the line, and carried into the Actual BOM variance lines as `BaselineValueSource`
  (`ENGINEER`, `LAST_ACCEPTED_BILL`, `OPENING_STOCK`, or `MIXED` when one item's lines
  differ). The rehearsal accepts the opening value for one line, types the other, proves an
  unpriced line cannot be submitted, and reads the sources back.
- No SALES_ENGINEER or SALES_HEAD is seeded; the IT Manager (and TD/MD) hold
  `sales.customer-po`, so the IT Manager creates the customer PO.
- **Witness-gated tests.** The routine suite compiles without eight gates. What each gate
  hides, and whether it covers a path that runs on 1 October:
  - `WorkflowWitness` (22 files, each re-runs the complete purchase flow): machine
    delivery + dossier, FIFO partial fitment/return and historical return, Actual BOM
    reversal report, QC correction, Stores workload and QC-stock dashboards, purchase
    workload/spending/obligations/open-orders dashboards, PO cancellation, amendment and
    receipt revision, supplier invoices, vendor bank advice, PO cash cap and revision,
    foreign payment advice and currency positions. **Day-one paths: machine delivery,
    partial fitment/return, supplier invoices, the dashboards, PO amendment.** The
    rehearsal now covers machine delivery and partial fitment/return itself; the rest is
    covered only when the gate is on. They are gated for runtime (each is a full ten-minute
    flow), not because they are optional: the routine suite would roughly triple.
    Decided 20 September (evening): a nightly run and the four day-one flows moved into the
    rehearsal. `tools/Run-WorkflowWitness.ps1` builds and runs the `WorkflowWitness`,
    `ConcurrencyWitness` and `MigrationLifecycleWitness` gates one after another (each is a
    compile-time constant, so each gets its own build), writes TRX and logs under
    `local-evidence/nightly/<date>/`, and exits non-zero if any gate fails; the `schtasks`
    line at the top of the script registers it. All three gates compile at `HEAD`; the first
    nightly tells whether they are green. The four day-one flows are now in the rehearsal:
    the supplier invoice recorded before the goods (with `billed-not-received` showing it
    until the receipt, and the link to the accepted bill afterwards), the PO amendment
    approved and issued before receipt (open-orders dashboard flags the unconfirmed delivery
    date; the receipt is against the amended revision), the Stores workload dashboard
    (gate entry waiting for its GRN) and the QC-stock dashboard (the lot in QC hold).
  - `ConcurrencyWitness` (15 files): concurrent GRN, MIR approval, PR approval, QC
    concessions, role change, serial issue, vendor bill and payment, opening stock. Day-one
    paths under load; gated for runtime and flakiness. Same recommendation.
  - `MigrationLifecycleWitness` (16 files): up/down of individual migrations (estimated
    BOM, material issue and return, job orders, FAT readiness, production engineering,
    ACL convergence, login). Not a runtime path; they belong to the migration gate the
    runbook runs before go-live (`-p:MigrationLifecycleWitness=true` once on the release
    build).
  - `KeycloakWitness`, `DiskFullWitness`, `HostFailureWitness`,
    `MigrationInterruptWitness`, `ReportVolumeWitness`: environment witnesses (containers,
    disk-full, killed hosts, volume); not day-one paths.

- `IT_MANAGER` holds view only on `purchase.requisitions`, and the requester must be
  the caller. Department requesters therefore cannot raise their own requisitions;
  Purchase raises them. The fixture's assumption that a department manager creates
  the requisition was wrong, not the grant.
- `STORES_MANAGER` had no `inventory.grn` grant at all; only `STORES_EXECUTIVE`
  creates and finalizes receipts. The view was granted with finding #12 above.
- #17 (Technical Support Manager technical verification returned 500 in the field) is
  **resolved by finding #3** (`f7f0c24`, "Scope technical verification to the verifier
  department"), not a separate defect. The rehearsal's TSM (SESS-04,
  `TECHNICAL_SUPPORT_MANAGER` only) verifies the quotation and receives 200 with status
  `TechnicallyCompliant`; the history guard already admitted the role. The 500 the
  frontend developer saw predates `f7f0c24`. The probe outcome is appended to
  `local-evidence/finding-17/tsm-probe.jsonl` on every run so a regression shows up
  as a status other than 200 there and as a failed rehearsal.
- Every vendor created through the API needs an uploaded GST certificate. On a
  rebuild the certificates carry over from `advance.vendor_attachments` (bytea rows
  with their own GUIDs, no outward foreign keys) and the vendor workbook's
  `AttachmentMetadataJson` references those GUIDs.

## Duplicate merge: what was built

Reading (a) of the decision: **only the Technical Director performs a merge.** The
service resolves `TECHNICAL_DIRECTOR` alone, the trigger admits only a FULL or
TEMPORARY `TECHNICAL_DIRECTOR` assignment on `item_merge_aliases`, and the page
permission on the merge route stays `masters.items:approve` (which the managers also
hold, so the service and trigger are the refusal, not the page). A manager who finds
a duplicate asks the director, who performs the merge.

Reading (b), a manager requests and the director approves, is available later as an
additive change: a pending merge-request record with a manager request endpoint and a
director approve endpoint that performs the merge. The alias row would still be written
in the director's session, so the trigger and this migration stay as they are.

## Evidence

Focused Debug run of the rehearsal: passed, 2 m 04 s (first candidate) and 2 m 21 s
with finding #12, the item move and #24. Full Debug and Release suites with TRX are under
`local-evidence/finding-18-19` (first candidate) and `local-evidence/second-candidate`
and cited by each commit.
