# R1 item master import mapping - 29 September 2026

CONFIRMED from the registered F11 implementation: download
`GET /api/v1/master-data/items/template`, then upload the matching `.xlsx` through
`POST /api/v1/master-data/items/import`. Template version is 1. The current default
row limit is 10,000 (`MasterDataTransfer:MaxRows` may be set lower by deployment), so
1,413 rows fit the default. No actual item workbook was supplied or loaded.

Generated current template: `local-evidence/vendor-import-20260929/templates/items-template-v1.xlsx`.
Handoff copy: `C:\dev-mageshwari\mocks\items-template-v1.xlsx`.
This is the current API template, not a claim that every cleaned-CSV field is supported.
Replace its clearly marked example row; do not import the example as a business item.

| Cleaned CSV column | Current F11 template mapping |
|---|---|
| Item Code | Item Code |
| Material Name | Item Name |
| Model/Part No | Missing from F11 template; no silent concatenation into the name |
| Make | Manufacturer Code, requiring a match to an existing active manufacturer; a free-text make is not automatically created |
| Bar Code | Missing from F11 template |
| Department | Category Code: REFRIGERATION -> REF, ELECTRICALS -> ELE, FABRICATION -> FAB |
| HSN | HSN / SAC Code (text, preserve leading zeroes) |
| UOM | UOM Code, exact existing active code |
| Cost | Missing from F11 template; costing meaning must be confirmed before mapping |
| GST % | GST Rate as percentage points, e.g. 18, not the Excel percentage fraction 0.18 |

Department maps to the canonical **item category**, not the employee Department
master. REF/ELE/FAB are the documented category mapping; F11 expects the codes and
does not translate the long department labels automatically. Category rows must
exist and be active before upload.

All eight UOM codes NOS, MTR, KGS, ROLL, BOX, PKT, LTR and FT must already exist and
be active in the UOM master for the operator F11 import. Unknown codes are rejected;
F11 does not create a UOM or assume a conversion. Use `/api/v1/master-data/uoms/template`
and `/api/v1/master-data/uoms/import` for approved missing UOM definitions first.
Required UOM definitions include name, measurement dimension and quantity precision.

The item template also requires Item Type, Is Returnable, Serial Policy and Reorder
Level. These decisions are absent from the supplied CSV columns; do not infer a
single behavior for every item. Reorder Level may be zero if approved by the owner.
Subcategory Code and Preferred Vendor Code are optional lookups.
New F11 items enter Draft and require ERP approval before purchase/receipt.

## Go-live gap and legacy loader distinction

CONFIRMED: the current F11 item workbook cannot retain all supplied columns. Model/
Part No, Bar Code and Cost need a separately authorized extension before this is a
lossless load. Proposed Cost destination is Standard Estimated Price, subject to TD
confirmation that this is a planning price rather than an opening-stock valuation.
Keep opening quantity/value in the separate opening-stock ceremony.

`tools/generate-item-import.py` is an older installer SQL generator, not the F11
operator import. It reads Sheet3 with legacy-specific headers, seeds UOMs/categories,
loads items as Active/Approved and skips conflicting codes. Its existing assumptions
and skip/default behavior do not establish a verified 1,413-row go-live load. Do not
run it against the cleaned CSV or replace the checked-in 1,368-row artifact blindly.
Reconcile item codes and counts against the chosen load path in a disposable rehearsal.

No item code, template schema, costing rule or production data is changed by this note.
