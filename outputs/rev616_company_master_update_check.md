# REV616 Company Master Update Check

Checked: 2026-07-03
ERP health: PASS - running REV616 at http://127.0.0.1:8783/InventoryERP_Software.html

## Result

Company Master is mostly ready for company profile, statutory, bank, invoice, and document attachment maintenance, but it is not yet strict/clear enough for a full company-document register using the certificate files provided.

## Good points confirmed

- Company Master screen exists and is controlled by `companyForm`.
- TD Admin/MD edit control is present through `companyMasterEditAllowed()`.
- Duplicate company creation is blocked by company id/name.
- Save & Lock is present; locked company records are protected from non-admin edit.
- Audit log is written for Company Master create/update.
- Company fields are present for:
  - Company name/code
  - GSTIN
  - CIN
  - PAN
  - TAN
  - MSME/Udyam number
  - State code/place of supply/GST type
  - Address/factory address
  - Phone/mobile/email/department emails/website
  - Bank name/account/IFSC/branch/UPI/bank details
  - Invoice/PO/DC/offer prefixes and next numbers
  - Default payment/warranty/delivery/invoice terms
  - Currency/time zone/default company/active status
- Upload fields exist for logo, authorised signature, seal, letterhead, and multi-file compliance documents.
- AI Read & Auto Fill button exists for company compliance documents.
- Clear Documents button exists.
- SESS PVT LTD seed/default data exists with key values:
  - GSTIN: `33ABACS5491H1ZA`
  - CIN: `U24304TN2018PTC123559`
  - PAN: `ABACS5491H`
  - TAN: `CHES52840E`
  - Bank: Indian Overseas Bank
  - Account number: `228002000027748`
  - IFSC: `IOBA0002280`
- Duplicate/old SESS PVT company document records are cleaned/merged by `cleanupCompanyMasterRecords()`.
- Export and print buttons exist for Company Master.

## Source document availability

All 19 referenced files were found on disk.

| Status | Document |
|---|---|
| FOUND | `D:\SESS PVT\sess pvt\ISO CERTIFICATE 2025-28.pdf` |
| FOUND | `D:\SESS PVT\sess pvt\PAN_compressed.pdf` |
| FOUND | `D:\SESS PVT\sess pvt\MOA 1.pdf` |
| FOUND | `D:\SESS PVT\sess pvt\GST CERTIFICATE.pdf` |
| FOUND | `D:\SESS PVT\sess pvt\IOBCERTIFICATE.pdf` |
| FOUND | `D:\SESS PVT\sess pvt\GST -TM-ISO-MSME.pdf` |
| FOUND | `D:\SESS PVT\sess pvt\GSTTMISOMSME.pdf` |
| FOUND | `D:\SESS PVT\sess pvt\Udyam Registration Certificate.pdf` |
| FOUND | `D:\SESS PVT\sess pvt\msme.pdf` |
| FOUND | `D:\SESS PVT\sess pvt\CERTIFICATE OF INCORPORATION sess pvt ltd.pdf` |
| FOUND | `D:\SESS PVT\sess pvt\ap pan card.pdf` |
| FOUND | `D:\SESS PVT\sess pvt\easwari aadhar NEW.pdf` |
| FOUND | `D:\SESS PVT\COMPANY DETAILS\SESS\GST\AA330317045877Y_RC20092017 address.pdf` |
| FOUND | `D:\SESS PVT\COMPANY DETAILS\SESS\MSME\ALAGUEASWARI P.pdf` |
| FOUND | `D:\SESS PVT\COMPANY DETAILS\SESS\MSME\MSME udyogaadhaar.gov.in_UA_PrintApplication SESS.pdf` |
| FOUND | `D:\SESS PVT\COMPANY DETAILS\SESS\PAN\SESS -PANCARD.pdf` |
| FOUND | `D:\SESS PVT\COMPANY DETAILS\SESS\BANK\Cheque-SESS.pdf` |
| FOUND | `D:\SESS PVT\COMPANY DETAILS\SESS\BANK\EFTBankCertificate.pdf` |
| FOUND | `D:\SESS PVT\COMPANY DETAILS\SESS\IE Code\IEC_Certificate.pdf` |

## Missing/fix points

- Company documents are stored as one generic multi-file compliance upload, not as named checklist slots.
- No strict separate slots/status are visible for:
  - GST Certificate
  - PAN Card
  - TAN, if applicable
  - MSME/Udyam Certificate
  - ISO Certificate with validity date tracking
  - Bank Certificate
  - Cancelled Cheque
  - IEC Certificate
  - Certificate of Incorporation
  - MOA
  - Director/Proprietor PAN
  - Director/Proprietor Aadhaar
- ISO certificate validity `2025-28` is not tracked as a structured valid-from/valid-to field.
- Company Master does not show per-document verification status such as Pending, Verified, Expired, Rejected, or Reupload Required.
- Company Master does not show per-document number fields for extracted values like GSTIN/PAN/Udyam/IEC/ISO certificate number and issue/expiry date.
- The listed `D:\...` source file paths are not linked/imported as structured source references inside the company record; files must be manually uploaded through the browser file input.
- Duplicate/alternate documents across the two folders are not grouped by document type, so users may upload both old/new copies without knowing which one is final.
- There is no clear Company Document Register grid/filter for quickly checking which mandatory company documents are complete.

## Recommendation

Next proper upgrade should add a Company Master Document Checklist/Register with named document slots, file upload per slot, extracted reference numbers, validity dates, verification status, remarks, and final/old-copy control. Keep the existing generic compliance upload as supporting attachment storage, but add the checklist so SESS PVT LTD documents are clear and auditable.

## Check conclusion

No ERP code was modified in this check. Company Master basic profile update is OK, but company certificate/document tracking needs the focused checklist upgrade before calling the Company Master document update fully complete.
