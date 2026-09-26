# Setup screens for the 1-3 October setup window: source review and launch alternatives

Decision record, 21 September 2026. Report/proposals only; no setup UI, fallback tool or
business data was created. The Configuration Page remains deferred until after go-live.
Production login remains the critical path for screens AND the governed API alternatives.

## Finding

**Seven required checklist operations lack an entry/decision screen in the reviewed
frontend.** In particular, a vendor page exists but its Accounts commercial-verification
action does not. Thus the present frontend cannot deliver the whole screen-only training
plan. Two other checklist qualifications (additional UOM conversions and company
relationships) need the conditional treatment below; identity/scope prerequisites are
also listed so that they are not mistaken for solved screen workflows.

Source baseline: backend `ac45aa8` (including `828816c`); frontend
`0c59254f58bd49fc13a8b919387ba0cf5a1d9988`, confirmed against remote
`origin/feature/frontend` on 21 September. Inspected the entire frontend source tree,
App.tsx routes/navigation, API wrappers and form/action implementations, including
embedded quick-add/import controls, not just menu labels. This is source evidence, not
an accepted Release browser/login witness. Estimates are engineering person-days for
one developer familiar with this code, including permissions, validation, retry/conflict
handling and focused role acceptance. They exclude resolving production login and field
sign-off; shared components can reduce totals. They are estimates, not delivery promises.

## Missing operations on the required checklist

| Checklist item without a screen | API today; intended operator / checker | Blocks opening stock? | Blocks first GRN? | Screen effort | Fastest safe fallback if screen misses the date |
|---|---|---|---|---|---|
| 8.1 Warehouse create/submit/approve | Yes: inventory warehouses CRUD and lifecycle; workbook master-data `warehouses` template/import. Stores Manager makes/submits; different TD approves. | Yes: opening rows need an active company warehouse. | Yes. | 1-2 days | SESS completes fresh warehouse workbook; a reviewed operator script calls existing governed import as Stores, then lifecycle submit and TD approval separately. Import alone is not approval. |
| 8.2 Rack/bin create/submit/approve | Yes: inventory rack-bins CRUD/lifecycle; workbook master-data `rack-bins` template/import. Stores Manager then independent TD. | Yes: opening rows need the correct company/warehouse bin. | Yes. | 1-2 days | Same governed workbook/import approach, after warehouses; separate submit/approve sessions. Check condition before creating location references. |
| 8.3 Warehouse condition location create/close | Yes: configuration `warehouse-condition-locations`, create/list/close. Stores Manager; TD independently reviews recorded results. No separate API approval stage currently. | Yes for AVAILABLE locations used by opening rows. QC_HOLD/pending-return are not opening-stock prerequisites. | Yes: receiving routes need effective locations with all required conditions. | 1-2 days | One-time reviewed API script reads SESS's signed location plan, resolves approved warehouse/bin IDs, checks company/condition/dates, creates under Stores login, rereads effective rows; TD signs review. It must not claim the service enforces a second approval. |
| 8.4 Stores category route create/close, ELE/FAB/REF per company | Yes: configuration `store-category-routes`, create/list/close. Stores Manager; independent TD operational review, no API approval stage. | No: opening uses AVAILABLE locations directly. | Yes: GRN resolves exactly one effective company/category route. | 1-2 days | One-time API script after locations: resolve canonical category and same-warehouse QC_HOLD/PENDING_RETURNABLE_DC/AVAILABLE IDs, create as Stores, read back all six company/category combinations, retain independent review. |
| 9 GST rule create/approve | Yes: configuration `tax-gst` create/read/approve/reject. Accounts Manager makes; different TD or MD decides; creator decision refused. | No: opening values are ex-tax carrying values. | Yes on the normal purchase-to-GRN path: quotation pricing needs a matching effective approved GST rule upstream of the PO. This is not a claim that every GRN action creates/resolves a new tax rule. | 2-3 days | One-time API script submits Accounts-approved rule input under Accounts login; TD/MD separately approves the returned ID/version. Read back effective coverage for HSN/state/registration/date; never insert pre-approved rows or use item GST percentage as a substitute. |
| 11.2 Vendor Accounts commercial verification | Yes: `POST /api/v1/masters/vendors/{code}/verify-commercial`. Accounts Manager with version/remarks; vendor maker and MD final approver stay separate. | No: opening provenance is declared, not dependent on a verified supplier master. | Yes upstream: MD final vendor approval refuses until verification is complete. | 0.5-1 day, extending existing vendor detail page | Small reviewed API action script run by Accounts, one vendor/current version at a time, with remarks and reread/history evidence. Then use the existing MD approve button. Prefer adding this small button over a new administration screen if frontend capacity permits. |
| 11.4 Vendor qualification create/verify/approve | Yes: configuration `vendor-qualifications` lifecycle. Purchase Manager creates; TD verifies; MD approves, three distinct employees enforced. | No. | Yes upstream: RFQ invitation needs exactly one authoritative effective qualification for each supplied category. | 2-3 days | One-time API script submits SESS qualification sheet as Purchase, saves IDs/versions, then separate TD verify and MD approve runs. Read back effective company/vendor/category coverage. No installer-owner impersonation or pre-approved bulk load. |

All seven fallbacks are proposals, **not newly delivered commands**. Warehouse/rack
workbook import engines and the listed business APIs exist; the convenient operator
wrapper does not. The current Installer supports principals/bootstrap/backup and related
maintenance, not a `setup` command for these business records. Extending it to write as
owner would bypass the named employee controls and is not the fastest safe choice.
A small common API wrapper plus prepared input schemas and a DEMO rehearsal is estimated
at **2-4 person-days across the seven operations**, including independent actor hand-offs
and negative/replay checks, after login works. Do not add the screen estimates to that
alternative estimate. TD must select the screen route or a documented assisted-entry
exception for each missing operation before training; this report does not silently
change the screen-only decision. SESS still owns/prepares the inputs and performs each
business decision; a developer does not supply a source database.

## Other checklist entries and conditional gaps

- **QC policies have a screen**, `/qc/inspection-policies`: create and TD approve/reject
  are implemented against the configuration API. The page uses `qc.inspection-policies`
  permissions. Missing policy blocks successful QC completion/release, not the ex-tax
  opening ceremony; it may still allow a GRN into QC_HOLD. Witness coverage with QC/TD.
- **Vendor creation, certificate upload and final approval have screens**. VendorFormModal
  uploads GST certificate bytes before posting the vendor with the returned attachment ID;
  VendorDetailPage has submit/approve but its action list and VendorAction type omit
  `verify-commercial`. Do not count the whole vendor lifecycle as screen-complete.
- **Customers have create/edit/upload/submit/approve screens**. IT makes and TD independently
  approves; there is no vendor-style commercial-verification stage. Customers are not an
  opening-stock or incoming-GRN technical prerequisite, although SESS must complete the
  agreed sales setup before sales use.
- **Item/vendor links have embedded controls** in ItemFormModal (`getItemVendors` /
  `setItemVendors`), alongside category/subcategory/UOM quick-add. They do not need a new
  standalone screen to satisfy this checklist. Items still come from the checked-in script.
- **Additional UOM conversions have no screen**. API create/list exists at configuration
  `uom-conversions`, but create leaves PendingApproval and no conversion decision endpoint
  was found in that route group. Stores/Purchase prepare; TD reviews the intended factor,
  but an effective approval path must be established before claiming it is usable.
  Estimate **1-2 days for a screen alone; 2-4 days for a complete governed workflow** if
  required. This is conditional: opening counts and first purchase quantities expressed
  in the item's existing base UOM need no conversion. Fastest safe launch choice is to
  use truthful base-UOM quantities/rates where commercially valid, with Accounts/Stores
  review. If a real conversion is indispensable, deliver and test its governed backend
  decision path as well; a script setting Approved directly is not acceptable.
- **Party/company relationship editing has no identified screen or dedicated public
  endpoint in the inspected party endpoints**. The earlier checklist's wording was a
  visibility check, not proof of an available editing workflow. Verify shared-party
  visibility and actual selected-company consumption in DEMO; do not duplicate a vendor
  or invent a relationship row. A customer relationship is not a GRN/opening dependency;
  no manually entered relationship requirement was established by this review. IT and
  Accounts own the visibility check; TD/MD own any proposed relationship/credit control.
  A newly demonstrated vendor visibility failure would block its purchase flow. Estimate **1-2 days for
  UI if an existing supported service is confirmed, otherwise 3-5 days including the
  scoped service/governance**. This is a conditional discovery item, not an instruction
  to fabricate a link or an assertion that every fresh party needs manual linking.

## Screenless prerequisites outside steps 8-11

The runbook's step 5 identity setup is also not delivered as an ERP admin screen in this
frontend: configuration `employee-identities` and `operational-scopes` APIs exist;
IT Manager manages named mappings/scopes after the published Installer's one-time
SESS-12 bootstrap. These are security setup, not stock configuration. They block **all**
role-governed setup/opening/GRN if the required operators cannot authenticate or act in
scope. A narrow identity/scope UI is approximately **2-4 days**, excluding OIDC login.
Use the documented bootstrap and a reviewed, administrator-run API setup procedure with
IT identity for subsequent mappings; no token sharing or broad emergency role grants.
Do not treat the developer Debug sign-in page as a production solution.

## Required controls on any proposed fallback

Use only the expected HTTPS server/company and approved input file/hash; explicitly bind
record identities, dates and current versions. Provide validation/preview before writes;
record each result and approval history without logging bearer tokens or credentials.
Authenticate each named employee separately. Never accept a caller-supplied role as proof
of authority. Preserve service-side permissions, scope, audit, version conflicts and
maker-checker refusal. Check existing state before retries; use stable idempotency keys
only where the endpoint supports them. For non-idempotent create, ambiguous failure means
reread/resolve, not blind retry. Do not add blanket retry to condition-location creation.

Rehearse first in DEMO with wrong-role/self-approval, stale-version, duplicate/date-overlap,
wrong-company and rerun cases; record source commit/input hash and receipts. No new SQL
by hand, owner-level insertion, trigger disabling or copying from the developer database.
No GRN, issue or adjustment on go-live before BOTH template-v2 ceremonies are POSTED.
Configuration does not write stock movements; inspect ledger read-only after setup.
No stock-moving verification just to prove a configuration entry works.

## Source pointers

Frontend paths below refer to commit `0c59254f58bd49fc13a8b919387ba0cf5a1d9988`,
not the older web files in the backend checkout:
`src/SESS.NexaERP.Web/src/App.tsx` (Warehouse/Rack-Bin disabled; no configuration routes),
`api/masterdata.ts` (UI MasterKey union excludes warehouses/rack-bins),
`features/vendors/VendorDetailPage.tsx`, `VendorFormModal.tsx`, `api/vendors.ts`,
`features/customers/CustomerDetailPage.tsx`, `features/qc/QcPolicyPage.tsx`,
`features/items/ItemFormModal.tsx`. Source snapshot is retained locally under
`local-evidence/setup-screen-review`; no source modifications were made there.

Backend evidence: [warehouse/rack lifecycle](../../src/SESS.NexaERP.Api/Endpoints/InventoryEndpoints.cs),
[workbook adapters](../../src/SESS.NexaERP.Infrastructure/MasterData/WarehouseRackMasterDataAdapters.cs),
[configuration routes](../../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs),
[route creation](../../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.StoreCategoryRoutes.cs),
[vendor verification gate](../../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.Rev869A.cs),
[GST workflow](../../src/SESS.NexaERP.Infrastructure/Masters/EfTaxGstWorkflowService.cs),
[RFQ qualification and quotation tax](../../src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs),
[GRN route resolution](../../src/SESS.NexaERP.Infrastructure/Stores/EfGoodsReceiptService.cs),
[opening-stock guards](../../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/OpeningStockSql.cs),
and [Installer command dispatch](../../src/SESS.NexaERP.Installer/Program.cs).
