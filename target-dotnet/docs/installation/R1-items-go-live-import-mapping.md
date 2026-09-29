# R1 item master import v2 - 29 September 2026

CONFIRMED implementation: download `GET /api/v1/master-data/items/template`, then
upload the matching workbook through `POST /api/v1/master-data/items/import`.
Template version is **2**; download a fresh template instead of uploading v1.
The default row limit is 10,000, so the intended 1,413 rows fit. Deployment may
configure a lower `MasterDataTransfer:MaxRows`. No real item data has been supplied
or loaded by this change.

Operator workbook: `C:\dev-mageshwari\mocks\items-template-v2.xlsx`.
Repository-local generated artifact (ignored, never committed):
`local-evidence/items-import-20260929/templates/items-template-v2.xlsx`.
Save a working copy and replace the clearly marked example row before uploading.

| Cleaned CSV column | F11 v2 column / stored meaning |
|---|---|
| Item Code | Item Code; immutable business code |
| Material Name | Item Name |
| Model/Part No | Model/Part No; preserved as Part Number, at most 120 characters; no inferred split into Model and Part Number |
| Make | Manufacturer Code; first reconcile the operator's distinct Make list to active manufacturer master codes |
| Bar Code | Bar Code; text, preserve leading zeroes; at most 128 characters; unique across items |
| Department | Category Code: REFRIGERATION -> REF, ELECTRICALS -> ELE, FABRICATION -> FAB |
| HSN | HSN / SAC Code; text, preserve leading zeroes |
| UOM | UOM Code; exact existing active code |
| Cost | Cost -> Standard Estimated Price; optional non-negative amount with at most two decimal places |
| GST % | GST Rate as percentage points, e.g. 18, not 0.18 |

Cost is a planning estimate. It does not create a purchase, change company Last
Purchase Price, or set opening-stock quantity/value. The separate opening-stock
ceremony still owns valuation. Blank Cost is unknown (null), not zero; explicit
zero is retained. Blank updates preserve an existing estimate, barcode and part
number; use the governed item editor for an intentional clear.

## Blank defaults and lifecycle

On create: Item Type **RAW_MATERIAL** (the supported stock type; MATERIAL is not a
valid enum), Is Returnable **FALSE/No**, Serial Policy **NONE**, Reorder Level **0**.
Explicit valid choices take precedence. TRUE/FALSE and YES/NO are accepted for
returnability. Invalid nonblank values are rejected rather than defaulted.
On update, blank policy values preserve the existing policy. Explicit policy
changes are allowed only for Draft records through the existing import rules.

Existing database rules also require returnable items to have Item Type TOOL; all other types are non-returnable. Reorder Level cannot exceed Maximum Stock (new items start with Maximum Stock 0); maintain stock limits before setting a nonzero reorder level.

These are the TD's requested generic defaults. Items requiring serial/batch/shelf-life
control or returnability must carry explicit values before ERP approval. New items
remain Draft and unapproved; import never supplies approval identities or dates.
Unknown/inactive lookup codes and conflicting barcodes are reported by preflight.
Department is an **item category**, not an employee Department assignment.

## Go-live reference data step (before the item upload)

1. Export/read UOMs and compare these eight definitions. Use the F11 UOM import for
   missing rows, with the operator's authorized account. New UOMs are active.
   Preserve existing matching rows. Stop on an inactive code or a dimension/precision
   conflict; do not silently change a used UOM or invent a conversion.

   | Code | Name | Dimension | Quantity precision |
   |---|---|---|---:|
   | NOS | Numbers | COUNT | 0 |
   | MTR | Metre | LENGTH | 2 |
   | KGS | Kilogram | MASS | 3 |
   | ROLL | Roll | COUNT | 0 |
   | BOX | Box | COUNT | 0 |
   | PKT | Packet | COUNT | 0 |
   | LTR | Litre | VOLUME | 2 |
   | FT | Foot | LENGTH | 2 |

   Prepared workbook: `C:\dev-mageshwari\mocks\r1-uoms-go-live.xlsx`.
   In a working copy, remove existing matching codes and upload only missing rows;
   do not upload an existing code as a new row without its exported ID/version.
   Routes: `GET /api/v1/master-data/uoms/template`,
   `POST /api/v1/master-data/uoms/import`. These definitions match the existing
   go-live loader's dimensions/precision; this step does not run that old loader.
2. Ensure REF, ELE and FAB exist and are active using the established category
   setup step in the fresh-database runbook. Fill **Category Code** with these codes;
   F11 does not translate long department names during upload.
3. The operator supplies the distinct Make list. Reconcile it to
   `GET /api/v1/masters/manufacturers`; create missing approved code/name mappings
   using `POST /api/v1/masters/manufacturers` with `{Code, Name}` through the
   authorized operator session. Read back every row as active, then fill
   Manufacturer Code. The F11 item import never auto-creates manufacturers.
   No manufacturer names or rows are fabricated while that list is pending.
4. Import and reconcile all 1,413 source item codes to the batch result: created,
   updated, unchanged and rejected counts. Resolve every rejected row before
   declaring the load complete. Review and approve items through ERP, then
   rehearse receipt/issue behavior for the actual policy-bearing items.

## Migration and delivery evidence

No new schema migration is required: PartNumber, Barcode and StandardEstimatedPrice
already exist. Migration head remains **143, 20260929143000_VendorImportSourceApproval**.
UOM/manufacturer seeding is an operator data step, not a schema migration and not
an operation against a real database by the integrator.

CONFIRMED: 24/24 focused tests passed, covering the v2 workbook, leading zeroes,
blank defaults, explicit policies, cost precision, duplicate barcodes, Draft-only
updates and a disposable 1,413-row synthetic cohort. The final fast-suite result
is recorded with the branch publication evidence.

The older `tools/generate-item-import.py` is not this F11 workflow. It loads its
legacy-specific workbook as Active/Approved and has skip/default behavior. Do not
run it on the cleaned CSV or replace its checked-in legacy data artifact blindly.
