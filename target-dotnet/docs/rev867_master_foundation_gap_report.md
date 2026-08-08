# REV867 Master Foundation Gap Report

Starting point after REV866C1:

- Item, Vendor, Customer, Warehouse and Rack/Bin existed only as thin entities with basic create/list APIs.
- Customer/Vendor Page Master entries existed as `masters.customers` and `masters.vendors`.
- Item/Warehouse/Rack-Bin master page entries were missing under the required `masters.*` keys.
- Normalized child/support tables for contacts, addresses, categories, UOM, manufacturers, status history, approval history and attachment metadata were missing.
- Master APIs did not yet provide full lifecycle actions, commercial masking, audit/status/approval history routes, or complete server-side query envelopes.

REV867 source implementation:

- Expanded Item Master with technical fields, QC/tracking flags, stock thresholds, barcode/image metadata, preferred vendor reference, status, approval status and immutable-code flag.
- Expanded Vendor Master with GST/PAN/MSME/contact/address/payment/delivery/bank metadata, vendor status, approval status and immutable-code flag.
- Expanded Customer Master with GST/PAN/contact/address/site/portal organization/credit metadata, status, approval status and immutable-code flag.
- Expanded Warehouse/Store and Rack/Bin with operational status, approval status, location classification, employee/department references and deactivation safeguards.
- Added normalized support tables and EF configuration for categories, subcategories, UOM, manufacturers, contacts, addresses, status history, approval history and attachment metadata.
- Added required Page Master entries for `masters.items`, `masters.warehouses` and `masters.rack-bins`; existing `masters.customers` and `masters.vendors` were preserved.
- Expanded role-page permissions through the existing seed matrix; operational roles remain denied for unapproved approval/commercial/export/full-control powers.
- Replaced basic master endpoints with permission-protected paginated list/detail/create/update/lifecycle/history APIs.
- Added GSTIN/PAN format validation, duplicate prevention, version checks, self-approval prevention, remarks-required lifecycle actions, commercial/bank/credit masking and no hard-delete endpoints.

Not included by design:

- Purchase Requisition, RFQ, PO, GRN, stock reservation, stock issue and stock movement transaction workflows.
- Real production OIDC provider configuration or real external token testing.
- Direct stock balance editing.

Generated artifacts:

- Migration: `20260808151207_Rev867MasterFoundation`
- Idempotent SQL script: `target-dotnet/outputs/rev867_master_foundation_idempotent.sql`
- Manual secure helper: `target-dotnet/tools/apply-rev867-secure.ps1`
