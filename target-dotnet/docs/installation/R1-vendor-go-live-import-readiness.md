# R1 vendor go-live import readiness - 29 September 2026

CONFIRMED from the checked-in F11 implementation: the proposed 89-row CSV is
**not accepted as-is**. The current endpoint accepts the generated `.xlsx` template
only, up to 1,000 rows. No real vendor file was supplied or imported for this review.

## Existing path and column mapping

Download `GET /api/v1/master-data/vendors/template`, then submit the populated
workbook to `POST /api/v1/master-data/vendors/import` with File, Mode and
IdempotencyKey. The import requires Create and Update permissions. See
[master data import](master-data-import.md) for batch results and error downloads.

| Supplied column | Existing template field / gap |
|---|---|
| Sl.No (`SESS-V-0001` style) | VendorCode / Vendor Code; preserve as the business code, not a row number |
| Name | LegalVendorName / Legal Vendor Name |
| Address | BillingAddress / Billing Address |
| Contact Person | ContactPerson / Contact Person |
| Mobile | Phone / Phone |
| Email | Email |
| GSTIN | GstNumber / GSTIN; optional |
| PAN | PanNumber / PAN; optional |
| MSME No | MsmeNumber / MSME Number; a separate required MsmeStatus TRUE/FALSE is also needed |
| Nature of Vendor | Candidate mapping to required VendorType / Vendor Type; business values need confirmation |
| Product Description | No dedicated field; MaterialServiceCategories is a possible mapping requiring confirmation |
| Supplier Approved | ApprovalStatus is read-only; cannot import Approved/Pending through the current template |
| Date of Approved (DD-MM-YYYY) | No import field; cannot retain this source date through the current template |

Country is another necessary input for the overseas vendor: blank defaults to India.
Set Country to China for that row; State may be Guangdong, with Indian State Code
blank. Do not infer country silently from an address.

## GSTIN and PO eligibility

CONFIRMED software behavior: GSTIN and PAN are optional and validated when supplied.
Blank GSTIN is therefore accepted for both the Chinese vendor and the two Indian
vendors awaiting correction. A malformed nonblank GSTIN is rejected. This is a
statement about software validation, not tax eligibility or statutory advice.

CONFIRMED from the creation service: new imported vendors start as Draft. Source
Approved/Pending values and their historical approval dates are not retained by the
current adapter. A blank GSTIN does not itself set a vendor to Pending.

CONFIRMED from the qualification policy and database safety guard: a PO cannot be
issued while the current vendor is Pending; it requires Active vendor lifecycle,
Approved approval and commercial verification, and eligible category qualification.
The database checks these conditions for the protected PO states including Issued.

## Required work before using the 89-row file

The TD requires every row to be imported, source status to be retained, and Pending
vendors blocked until ERP approval. The current generic template is insufficient;
this is a go-live import gap, not a completed import. Add a reviewed CSV mapping or
conversion/import path that preserves the source status and date, reports every row,
uses the existing idempotent framework, and does not bypass PO eligibility guards.
Reconcile exactly 89 distinct business codes after a disposable rehearsal and after
the operator's approved load. Never commit the register, workbook or personal data.

CLAIMED proposed default, awaiting TD business confirmation: retain legacy approval
status/date as audited source facts and require normal ERP approval before PO issue,
including for rows marked Approved in the source. Confirm whether legacy Approved
rows should instead enter an explicitly authorized ERP approval workflow during load.
Also confirm Nature of Vendor and Product Description mappings before conversion.

Source references: `CustomerVendorMasterDataAdapters.cs`, `EfMasterDataTransferService.cs`,
`EfPartyMasterDataServices.cs`, `PartyMasterRules.cs`, `VendorQualification.cs`, and
`Rev869BDatabaseSafetySql.cs`. No schema or import behavior is changed by this note.
