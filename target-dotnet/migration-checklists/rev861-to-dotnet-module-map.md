# REV861 To .NET/PostgreSQL Module Migration Map

Date: 2026-08-08

Source baseline:

`C:\Users\User\Documents\Codex\2026-07-03\see\current-system-snapshot\REV861`

Catalogue source:

`C:\Users\User\Documents\Codex\2026-07-03\see\architecture\current-system-catalogue`

## Migration Rule

The current REV861 Node.js/single-HTML ERP must remain available as the preserved baseline. New production work should move into the ASP.NET Core/PostgreSQL target architecture using controlled module migration, data scripts, validation, audit, and UAT.

## Foundation Modules

| Target module | Current REV861 source | Migration action |
|---|---|---|
| Identity and Access | Default users, role permission pages, login/session routes | Replace with central identity, password policy, MFA for privileged roles, backend authorization and record-scope rules |
| Employee/Admin | User Admin, Role Permission, Company Settings, master pages | Normalize employee, designation, department, branch, user-role, and permission tables |
| Customer Portal | Customer role portal, customer PO, public offer status | Add customer organization isolation and portal record scope |
| Vendor Portal | Vendor role portal, RFQ/quote/vendor-related screens | Add vendor organization isolation and controlled quotation/negotiation access |
| Audit/Reporting | Activity, deleted logs, generic ledger records | Create append-only audit and report projections with pagination |

## Phase 2 Master Migration

| Master | Required production table direction | Notes |
|---|---|---|
| Item Master | `items`, `item_barcodes`, `item_images`, `item_uom`, `item_reorder_rules` | Barcode and image support must be first-class, not browser-only |
| Vendor Master | `vendors`, `vendor_contacts`, `vendor_approvals`, `vendor_categories`, `vendor_performance` | Duplicate GST/PAN/name/contact rules and TD approval required |
| Customer Master | `customers`, `customer_contacts`, `customer_portal_users`, `customer_projects` | Customer isolation required for portal |
| Employee Master | `employees`, `users`, `roles`, `permissions`, `department_scope`, `project_scope` | Role-based operation depends on this |
| Warehouse/Store | `warehouses`, `stores`, `rack_bins`, `stock_locations` | Needed before stock ledger migration |
| Project/BOM | `projects`, `project_bom`, `bom_lines`, `bom_consumption` | Store issues must settle into project/BOM consumption |
| Numbering Settings | `number_series`, `document_sequences` | Enforce unique PR/RFQ/PO/GRN/DC numbers transactionally |
| Approval Settings | `approval_policies`, `approval_steps`, `approval_instances` | Department-owner and TD/manager routing must be backend controlled |

## Purchase Workflow Migration

| Step | Production requirement |
|---|---|
| PR creation | All permitted roles can raise PR with department/project/item linkage |
| PR approval | Route to department owner/incharge/TD/production manager as configured |
| Procurement start | Approved PR becomes purchase department workload |
| RFQ | Strictly enforce at least 3 unique vendors where policy requires it |
| Vendor quotes | Portal and manual quote updates must write negotiation history, not duplicate uncontrolled records |
| Comparison | Require 3 valid vendor offers before L1/L2/L3 comparison unless exception approved |
| Final vendor | PO cannot save until final vendor selection exists |
| PO | Duplicate PO number blocked by database unique constraint and idempotency key |
| PO release | Confirmation only allowed for approved/released PO status |
| Delivery | Delivery challan and invoice captured against PO |
| QC | QC verification before accepted GRN where required |
| GRN | Barcode/PO autofill, editable GRN number with audit, unknown item popup/new item creation route |

## Stores / Inventory Migration

| Step | Production requirement |
|---|---|
| Material inward | Receive by PO/DC/invoice linkage |
| GRN posting | Transactionally update accepted stock and stock ledger |
| QC hold/reject | Separate accepted, rejected, hold and return quantities |
| Barcode scan | Scan item code/barcode and auto-fill item, PO, vendor, UOM, location |
| Unknown barcode | Open new-item creation flow, then return to pending GRN |
| Reservation | Reserve material for project/job/BOM before issue |
| Project issue | Issue from store to project and post into actual BOM consumption |
| DC | Maintain Returnable DC, Non-returnable DC, Demo DC, Warranty DC, and project issue dispatch |
| Return | Support vendor return, customer/site return, project return and internal return |
| Transfer | Store-to-store/rack-bin transfer with stock ledger |
| Stock ledger | Monthly ledger with opening, inward, outward, adjustment, closing and audit |
| Minimum stock | Reorder rules, minimum stock statement, PR suggestion and dashboard alerts |

## Dashboard Migration

Required Purchase/Inventory dashboards after data model migration:

- Monthly stock value and item movement trend
- Minimum stock and reorder alerts
- PR ageing and approval pending
- RFQ/quotation pipeline
- PO pending confirmation/release
- GRN pending QC
- Stock ledger exceptions
- Top vendors by PO value, delivery delay, rejection and rating
- Slow/non-moving stock
- Project/BOM material consumption variance
- DC pending return and warranty material status

## Evidence Required Before Production Switch

- Database migrations reviewed and repeatable
- Old/new workflow comparison cases passed
- Security authorization tests passed
- Concurrency tests for PO/GRN/stock ledger duplicate prevention passed
- Load-test report prepared with approved concurrent-user targets
- Backup/restore and rollback tested
- UAT approval signed
