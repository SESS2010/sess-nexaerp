# R1 vendor go-live import - template version 2

CONFIRMED TD Option A, 29 September 2026: retain source register Approval Status
and Date of Approved through the F11 vendor workbook. These are historical source
facts. ERP approval, commercial verification and category qualification remain
required before a PO can be issued. No real vendor register is included in the repo.

## Operator template and path

Download `GET /api/v1/master-data/vendors/template` from the deployed version-2 API.
Submit to `POST /api/v1/master-data/vendors/import` with multipart File, Mode and
IdempotencyKey. Use `REJECT_ENTIRE_FILE` so validation errors prevent a partial load.
Rehearse on DEMO/disposable data first, then reconcile the authorized import result.
This mode imports when validation succeeds; it is not a validate-only preview. Preserve all three workbook sheets,
column order and hidden metadata. Upload `.xlsx`, not the original CSV.
The implementation default/ceiling is **10,000** data rows; a deployment may configure
a lower limit. The earlier 1,000-row note was stale. All 89 vendor rows fit the default.

Generated local template: `local-evidence/vendor-import-20260929/templates/vendors-template-v2.xlsx`.
Operator handoff: `C:\dev-mageshwari\mocks\vendors-template-v2.xlsx`.
The generated blank workbook is not committed, per the no-xlsx-commits rule; its
schema and generation code are version-controlled. Old version-1 workbooks must be
replaced with version 2; do not edit the version cell to reuse an old workbook.

| Cleaned CSV column | Template column |
|---|---|
| Sl.No (`SESS-V-0001` style code) | Vendor Code (preserve business code as text) |
| Name | Legal Vendor Name |
| Address | Billing Address |
| Contact Person | Contact Person |
| Mobile | Phone (text, retaining prefix/leading zero) |
| Email | Email |
| GSTIN | GSTIN (optional; validated when supplied) |
| PAN | PAN (optional; validated when supplied) |
| MSME No | MSME Number; separately fill required MSME Status TRUE/FALSE |
| Nature of Vendor | Vendor Type (TD-approved mapping; free text up to 80 characters) |
| Product Description | Material / Service Categories (TD-approved mapping) |
| Supplier Approved | Approval Status: Approved or Pending |
| Date of Approved | Date of Approved: DD-MM-YYYY, or blank if unknown |

Fill Country explicitly for the overseas row: China, State Guangdong, State Code
blank. Blank Country defaults to India. GSTIN may be blank for the Chinese vendor
and the two Indian vendors awaiting correction; this is software validation behavior,
not a statutory tax determination. Blank GSTIN does not itself assign Pending status.

## Source facts and governed approval

`Approval Status` maps to nullable `LegacyApprovalStatus`, accepting exactly Approved
or Pending. `Date of Approved` maps to nullable `LegacyApprovedDate` (database date,
no invented time zone). Text dates must be valid DD-MM-YYYY; native Excel date cells
are also read into that format. Invalid statuses and impossible dates are reported
against the row and column. Unknown source facts may be blank. Blank source cells
on update preserve existing facts, rather than silently erasing them.

`ERP Approval Status` is a separate read-only column. Import never sets ERP approval
or fabricates ApprovedBy/ApprovedAt. New vendors enter Draft, including source
Approved rows. Changing an active approved vendor's source status to Pending triggers
re-verification and removes current PO eligibility. After actual ERP approval, an
unchanged source Pending fact remains as historical provenance; replay does not
revoke that subsequent approval. Existing bank metadata is preserved.

Migration 143 `20260929143000_VendorImportSourceApproval` adds the two nullable
columns and an Approved/Pending check. Down refuses while either field contains
any data. It introduces no SECURITY DEFINER function and edits no merged migration.
The EF snapshot is synchronized for these two newly mapped columns only, with a
no-pending-model-changes assertion so the installer does not encounter model drift.
Source facts are included in normal import before/after audit and export. No source
approval field is added to the general vendor write API.

## Rehearsal and reconciliation

Validate the completed workbook, then reconcile exactly 89 distinct business codes,
source Approved/Pending counts and dates. Resolve rejected rows; do not count a
partial batch as a completed import. The operator alone performs the approved load.
Focused evidence uses synthetic vendors only, including 89-row persistence, optional
GSTIN, China/India, date round-trip, no-change replay, preservation on blank update,
Pending eligibility refusal and migration rollback refusal.
Evidence: `local-evidence/vendor-import-20260929/`.

CONFIRMED Vendor Type accepts all 13 supplied classifications as free text; there is
no fixed enumeration in F11 and none requires normalization to pass validation.
Manufacturer, Authorized Dealer, Distributor, Stockiest, Trader, Refrigeration material
Supplier, Electrical material supplier, Mechanical Material supplier, Fabrication
Material Supplier, Labour Work, Contractor, SUB CONTRACTOR and Others are accepted.

The focused Pending-update case also exposed missing CompanyId on existing vendor
re-verification history. The service now resolves the active request company and
writes its matching CompanyId/OrganizationId; missing context is refused.

CONFIRMED final validation: focused **33/33**, fast **1053/1053**, zero skips;
all **13/13** supplied Vendor Type values pass the actual adapter. The synthetic DB
witness retains 89/89 rows (45 source Approved, 44 source Pending). Saved workbooks
passed header/version/metadata/formula read-back; affected cells were rendered and
reviewed locally. Handoff copies are hash-identical and read-only; use Save As to fill
a working copy. The initial date-fixture and company-history failures are retained
in evidence alongside their corrected green reruns. No real vendor data was loaded.
