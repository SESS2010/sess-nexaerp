# SESS setup data to prepare now

Approved assisted-entry plan for 28-30 September 2026. One company per workbook or JSON
plan: SESS_PROPRIETORSHIP or SESS_PVT_LTD. Items come from the checked-in legacy script.
No developer dump, exported masters or copied attachment IDs. Examples are preparation
forms, NOT approved business values: replace placeholders and obtain SESS sign-off.

## Exact warehouse Excel columns, in order

`Record ID`, `Version`, **`Warehouse Code`**, **`Name`**, **`Warehouse Type`**,
`Location`, `Responsible Employee Code`, `Department Code`, `Status`, `Approval Status`,
`Is Active`.

Bold columns are required for new rows. Leave Record ID, Version, Status, Approval Status
and Is Active blank for new rows; lifecycle values cannot be supplied by an import.
Warehouse Code is immutable and unique within the selected company, at most 80 characters;
Name at most 200; Warehouse Type at most 80; Location at most 1000. Use an active employee
assignment and department if supplying those optional codes. For this launch use uppercase
codes containing letters, numbers, underscore or hyphen so they also fit the lifecycle
wrapper's deliberately narrow code validation. SESS chooses the actual codes/types.

## Exact rack/bin Excel columns, in order

`Record ID`, `Version`, **`Warehouse Code`**, **`Bin Code`**, **`Rack Name`**,
**`Bin / Partition Number`**, `Zone`, **`Location Type`**, **`Material Condition`**,
`Capacity Quantity`, `Capacity UOM`, `Barcode`, `Description`, `Status`, `Approval Status`,
`Is Active`.

Bold columns are required. Leave identity/version/lifecycle fields blank for new rows.
Warehouse Code must identify an active warehouse in this company. Bin Code is immutable,
unique across this company (not merely within one warehouse), at most 80 characters.
Rack Name and Bin / Partition Number are each at most 120 characters. One partition is one
row; several rows may share Rack Name. Location Type is business-defined, e.g. RACK or
PARTITION. Set Material Condition correctly before a condition-location references the bin.
For this receiving plan prepare separate AVAILABLE, QC_HOLD and PENDING_RETURNABLE_DC
bins. Capacity Quantity and Capacity UOM are either both blank or both supplied; quantity
is non-negative, UOM is an active master. Zone at most 120, barcode 128, description 240.

**Opening-stock template v2 `WarehouseCode` must exactly match Warehouse Code, and
`RackBinCode` must exactly match Bin Code, in the selected company and warehouse.**
Rack Name and Bin / Partition Number are not the opening-stock key. The opening bin must
have Material Condition AVAILABLE and an effective AVAILABLE condition-location mapping.

The four ready-to-fill workbooks are in [excel](excel/), one warehouse and one rack/bin
file for each company. They preserve the three-sheet import contract, version 1 and
hidden metadata; do not rename columns/sheets or alter metadata. There are 200 blank
entry rows. Column Guide contains the opening-stock Bin Code note and company warning.
Dropdowns: Warehouse Type STORES; Location Type RACK/PARTITION/QC_SECTION (checked-in
examples, business choices pending TD review); all seven server material conditions;
Capacity UOM NOS/MTR/FT/KGS/LTR/BOX/PKT/ROLL from the legacy setup script. Verify the UOM
is active before import. Leave capacity and UOM both blank if capacity is not used.
Excel blocks typed values outside the lists, but pasted input still requires governed
server validation. Filename is NOT a company security binding: check signed-in company
and import plan. Use the API Template action for a fresh canonical template if needed.
The import uses REJECT_ENTIRE_FILE; a successful import creates drafts, not approvals.
Stores submits each record separately, then a different TD approves its current version.
Only then build location references. Do this warehouse-first, rack-second.

## Data for the four configuration operations

The supplied `.create.example.json` files use the actual API body field names. Each plan
has Kind, Action, Company, TargetDatabase and Rows. Each row has its own immutable
OperationId (a new GUID) and Body. SESS prepares business values below; the setup operator
binds reference GUIDs from authenticated read-back, never by querying SQL or reusing IDs
from another database. Record the human codes alongside the reviewed input to make each
binding checkable. No business-configuration fields can specify the acting employee, role or identity subject.

| Operation | SESS prepares | Operator binds after dependencies exist |
|---|---|---|
| [Condition location, Stores](condition-locations.create.example.json) | OrganizationId, WarehouseCode, **Bin Code**, ConditionCode, EffectiveFrom, EffectiveTo (blank = open), Remarks | RackBinId from RackBins Read. Check exact company/warehouse/bin/condition. The API field is RackBinId, not a typed bin name. |
| [Category route, Stores](category-routes.create.example.json) | OrganizationId, ItemCategoryCode (ELE/FAB/REF), **Warehouse Code, QC_HOLD Bin Code, PENDING_RETURNABLE_DC Bin Code, AVAILABLE Bin Code**, EffectiveFrom, EffectiveTo, Remarks | QcHoldConditionLocationId, PendingReturnConditionLocationId, DefaultAcceptedConditionLocationId from ConditionLocations Read. All in one warehouse, effective for the route start; verify date coverage through 1 October. |
| [GST rule, Accounts](tax-rules.create.example.json) | OrganizationId, JurisdictionCode, HsnSacCode, SupplierStateCode, PlaceOfSupplyStateCode, VendorRegistrationType, GstRate, CgstRate, SgstRate, IgstRate, CessRate, IsExempt, IsReverseCharge, CurrencyCode, RoundingScale, EffectiveFrom, EffectiveTo, Remarks, ItcEligibility, RecoverableTaxPercent | SupersedesTaxGstSettingId = null for clean initial rules; only an explicitly reviewed replacement references an existing approved ID. Never infer rates from an item display percentage. |
| [Vendor qualification, Purchase](vendor-qualifications.create.example.json) | OrganizationId, VendorCode, ItemCategoryCode (canonical category actually supplied), QualificationCode, EffectiveFrom, EffectiveTo, Remarks | No GUID entry for vendor/category on create: service resolves codes. Vendor must complete its independent commercial/final approval path before use. |

GST JSON rates are numbers (not quoted text), flags true/false; no rates are prefilled.
VendorRegistrationType is one of REGULAR, COMPOSITION, UNREGISTERED, SEZ, OVERSEAS,
DEEMED_EXPORT, UIN. Currency is normally INR; Accounts decides valid splits, exemption/RCM,
ITC and rounding. ItcEligibility is FULLY_RECOVERABLE, BLOCKED or PARTIALLY_RECOVERABLE. Only partial
recovery supplies RecoverableTaxPercent, strictly between 0 and 100 with at most six
decimals; use null for the other two. Accounts approves the classification. Dates are yyyy-MM-dd. Plan examples use 2026-09-28 only as an illustration:
SESS approves dates that cover setup/opening and first use; do not backdate automatically.
Keep nullable fields present as null. Prepare separate copies of the supplied plans for
each company; the examples start with SESS_PVT_LTD and do not cover both companies at once.

For **GST approval**, the different TD/MD prepares: record ID from read-back, current
ExpectedVersion, Remarks and a new OperationId. Wrapper supplies IdempotencyKey from it.
For **qualification verification**, TD prepares the same three fields; after verification,
MD reads again and uses the new current ExpectedVersion to approve, under a separate login.
For **warehouse/rack submission or approval**, use Version (not ExpectedVersion), Remarks,
new OperationId and RecordId: warehouse business code for Warehouses, rack GUID for RackBins.
Use the provided `.submit`, `.verify` and `.approve` plans; null versions deliberately
refuse until replaced. Never approve using a version copied before the preceding action.

Condition locations and category routes have no API approval stage. Stores creates;
TD uses a separate authenticated Read invocation, reviews the original input and the
VERIFIED create receipts, and signs the setup register with their own read receipt/hash.
This is an operational second-person review, not an invented server approval state.
Use the [review register](review-register.md) and attach the actual receipts.

## Data for remaining screen work and security setup

- Vendor screen: vendor code/name/type, GSTIN/PAN, addresses/state/contact details,
  payment/delivery terms, credit days, categories/makes and permitted bank metadata;
  original GST certificate file (plus other required supporting documents). Upload the
  certificate before create. Accounts prepares commercial-verification remarks; MD
  approves independently. Full action contract is [here](../vendor-commercial-verification-contract.md).
- Customer screen: code/legal/trade name/type, GST/PAN, billing/shipping/state/country,
  contacts, industry, payment terms/credit period/limit and applicable supporting files.
  IT prepares, independent TD approves through its existing lifecycle.
- QC screen: organization, item OR category, parameter, measurement UOM, lower/upper limits,
  method, sample size, dates and remarks; QC Manager prepares, independent TD approves.
- Identity mapping (IT/SESS-12): OrganizationId, verified Issuer and Subject from Keycloak,
  EmployeeCode, IdentityType=HUMAN, EffectiveFrom, EffectiveTo, Remarks/approval reference.
  These name the **mapping recipient**, not the authenticated operator. Keep this file
  restricted. Use [identities.create.example.json](identities.create.example.json) and read back the subject hash.
- Operational scope (IT/SESS-12): OrganizationId, EmployeeCode, DepartmentCode (required
  active assignment), WarehouseCode if restricted, RackBinId if restricted, OwnRecordsOnly,
  AllowsPrivilegedCrossScope=false, EffectiveFrom, EffectiveTo, Remarks/approval reference.
  Blank warehouse/bin means broader access within the assigned department, not no access:
  TD must approve it deliberately. Add warehouse-specific scopes after warehouses exist.
  Existing effective scopes are inspected and reused, not blindly recreated.

Employee/company/department assignments and role activations are separate prerequisites;
these wrappers do not create employees, grant roles or override assignments. See the
updated D5 sequence. No UOM-conversion or party/company-relationship data collection is
required for this launch unless a concrete mixed-UOM/visibility dependency is demonstrated.

Source of exact workbook columns:
[warehouse/rack definitions](../../../src/SESS.NexaERP.Infrastructure/MasterData/WarehouseRackMasterDataAdapters.cs).
