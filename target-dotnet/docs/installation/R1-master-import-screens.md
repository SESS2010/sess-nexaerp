# R1 master imports and Item Export (#60)

Use the master owner's own login and select the intended company. Import is limited to Purchase Manager / TD / MD for vendors, and Stores Manager / TD / MD for items, UOMs and manufacturers. Having other page grants does not bypass this owner check. Export is checked independently; an Import owner does not automatically receive Export.

| Workbook | Web menu / route | API master key |
|---|---|---|
| Vendors | Masters > Vendor Master > Template / Import (`/vendors`) | `vendors` |
| Items | Masters > Item Master > Import / Export (`/items/import`) | `items` |
| UOMs | Masters > UOM Import (`/masters/uoms/import`) | `uoms` |
| Manufacturers | Masters > Manufacturer Import (`/masters/manufacturers/import`) | `manufacturers` |

Download the template from the screen (GET `/api/v1/master-data/{key}/template`). Keep its columns, guide and hidden metadata. Fill the Data sheet; import order is **UOM -> Manufacturer -> Vendors -> Items**. Existing item categories must also exist before Item Import.

All four uploads use `REJECT_ENTIRE_FILE`. Every row is validated before master changes; an invalid row rejects the whole file. The result shows row numbers and errors, Created / Updated / Rejected / Not imported counts. Correct every error and upload the whole corrected file. The audit batch may be retained even when zero master records are changed. If a write fails after validation, the entire master-write transaction rolls back.

Manufacturer template v1: Record ID, Version, Code, Name, Is Active. New manufacturers are active; do not import lifecycle changes. UOM template v1 also requires Measurement Dimension and Quantity Precision (0-6). Item template/export is v2, including Model/Part No, Bar Code and cost; Item Code and Bar Code are Excel text, preserving leading zeros. Item Export downloads all items rather than only the visible list page, subject to the configured export row limit.

Import does not approve a new item or vendor in the ERP. Source vendor approval facts do not replace governed ERP approval. For changes to existing records, download a current export and retain Record ID and Version; stale versions or business-code renames are rejected. Identical rows can be replayed unchanged. Never upload the error workbook unchanged: copy corrections into the original template.

The API exposes template, import and export through the existing master-data transfer endpoints. Migration 145 (`20261002090000_R1MasterImportOwnerGrants`) adds missing View/Create/Update/Download for the approved import owners. Export and all other actions are preserved; other roles remain blocked from these four imports by the API owner check. This is not the withdrawn Accounts Manager Inventory Periods proposal. Browser/operator rehearsal remains required on RC2.