# REV861 Phase 0 Current-System Catalogue Report

Date: 2026-08-08

## Decision

Current REV861 is preserved as the working prototype/current ERP baseline. Per the architecture correction, no further large Purchase, Stores, or Inventory feature expansion should be added to the existing single-HTML/Node.js build until the .NET/PostgreSQL migration foundation is ready.

## Catalogue Completed

Generated catalogue folder:

`C:\Users\User\Documents\Codex\2026-07-03\see\architecture\current-system-catalogue`

Files created:

- `REV861-current-system-catalogue.json`
- `REV861-page-catalogue.md`
- `REV861-menu-catalogue.md`
- `REV861-route-catalogue.md`
- `REV861-postgresql-object-catalogue.md`
- `REV861-role-user-catalogue.md`
- `REV861-local-data-dependencies.md`

## Current REV861 Inventory

| Area | Count / Finding |
|---|---:|
| Frontend page sections | 369 |
| Dynamic placeholder sections | 85 |
| Menu items detected | 677 |
| Unique menu targets | 342 |
| Menu targets without matching section | 2 |
| Sections without direct menu target | 29 |
| Backend/API routes detected | 73 |
| PostgreSQL tables detected | 20 |
| PostgreSQL indexes detected | 49 |
| Default users detected | 30 |
| Default roles detected | 20 |
| Browser localStorage call patterns | 48 |
| Frontend `companyData` collections | 97 |

## PostgreSQL Objects Detected

Tables detected in the current Node backend:

- `deleted_record_logs`
- `erp_company_ledger_records`
- `erp_db_state`
- `erp_json_master_snapshot`
- `holiday_master`
- `ops_flow_status`
- `page_form_records`
- `project_flow_status`
- `project_ledger_records`
- `purchase_flow_status`
- `sales_flow_status`
- `service_assets`
- `service_flow_status`
- `service_ledger_records`
- `simple_master_records`
- `stage_template_lines`
- `stage_templates`
- `stock_movements`
- `vendor_rating_records`
- `work_register_records`

## Identity / Role Baseline

Roles currently detected from default users:

- `admin`
- `md`
- `accounts_head`
- `purchase_head`
- `store_head`
- `production_head`
- `qc_head`
- `design_head`
- `service_head`
- `sales_head`
- `service_coordinator`
- `service_engineer`
- `sales_engineer`
- `it_admin`
- `customer`
- `vendor`
- `document_controller`
- `dcc`
- `branch_manager`
- `ops_admin_no_hr`

Passwords are redacted in the generated report. These users and roles must be migrated into the new identity model with individual login IDs, MFA for privileged roles, backend authorization, and customer/vendor record isolation.

## Main Blockers Before Production Migration

1. The current frontend is still a large single HTML file.
2. 85 page sections are dynamic placeholders and need either real implementation or explicit retirement.
3. 97 `companyData` collection keys and 48 localStorage call patterns show browser/local hybrid persistence still exists.
4. Current PostgreSQL schema is partially generic ledger/snapshot based; the target system needs normalized tables for Item, Vendor, Customer, PR, RFQ, Quote, PO, GRN, QC, Stock Ledger, DC, BOM, approvals, and audit.
5. Current API routes are Node.js endpoints; they need versioned ASP.NET Core equivalents with request validation, authorization, pagination, idempotency, cancellation, and audit logging.
6. File storage hooks exist, but production needs S3/Azure Blob metadata, signed access, checksums, classification, and malware scanning.
7. Scale claims cannot be made until reproducible load testing is run for the approved concurrent-user targets.

## Next Build Step

Start Phase 1 in `target-dotnet`:

1. Install/use the approved current supported .NET LTS SDK for the target environment.
2. Create the ASP.NET Core modular monolith solution and projects.
3. Add PostgreSQL EF Core migrations foundation.
4. Add identity, roles, permissions, audit, validation, exception handling, health/readiness endpoints, OpenAPI, and structured logging.
5. Create migration mapping documents from current REV861 catalogues to target modules.
6. Only after Phase 1 foundation is working, begin Phase 2 master migration: Item Master, Vendor Master, Customer Master, Employee/User/Role, Warehouse, Store, Rack/Bin, Project linkage, approval settings, numbering settings.

## Purchase / Inventory Migration Order After Foundation

1. Master data and permissions.
2. Purchase Request with department owner approval routing.
3. Stock check and requirement validation.
4. RFQ to 3 unique vendors.
5. Vendor quote capture, negotiation history, L1/L2/L3 comparison.
6. Final vendor selection and PO generation with duplicate PO blocking.
7. PO confirmation/release status.
8. Delivery challan and invoice capture.
9. QC verification.
10. GRN with barcode scanning, PO autofill, new-item popup flow, editable GRN number with audit.
11. Inventory receipt, reservation, issue, return, transfer, DC flows.
12. Stock ledger, monthly stock statement, minimum stock/reorder dashboard, vendor performance dashboard.

## Status

Phase 0 catalogue is now complete enough to continue the approved migration foundation. The existing REV861 ERP was not modified during this step.
