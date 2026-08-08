# REV866 Role Permission Reconciliation

REV866 evidence before correction showed 38 active ERP roles, 18 page master entries, and 360 role-page permission rows. That matched only the original 20 roles. REV866 corrective migration `20260808142353_Rev866CorrectiveStatusPermissionAudit` changes the matrix seed to include all 38 active roles across all 18 pages.

Expected complete matrix after correction: `38 x 18 = 684` role-page permission rows.

The model is explicit-row based. Every active role-page combination has a stored row. New operational roles are not granted access from role name alone. Unapproved actions are stored as explicit `false` permissions.

| Role Code | Role Name | Active | Employee assignments | Page-permission row count | Allowed actions summary | Missing permission status |
| --- | --- | --- | --- | ---: | --- | --- |
| admin | Administrator | Active | None seeded | 18 | Full control; system configuration and all matrix actions | Complete |
| md | Managing Director / CFO | Active | None seeded | 18 | Full control; high-level approvals and commercial visibility | Complete |
| accounts_head | Accounts Head | Active | None seeded | 18 | View operational pages, commercial visibility, audit-history visibility; commercial approval only where page key permits | Complete |
| purchase_head | Purchase Head | Active | None seeded | 18 | Purchase create/update/submit/verify, master visibility, commercial visibility; no automatic full control | Complete |
| store_head | Store Head | Active | None seeded | 18 | Inventory create/update/submit and master operations; no automatic financial approval | Complete |
| production_head | Production Head | Active | None seeded | 18 | Inventory operational visibility/create for stock-facing work; no commercial/finance approval | Complete |
| qc_head | QC Head | Active | None seeded | 18 | Inventory verify/reject/clarification/revision actions; no commercial/finance approval | Complete |
| design_head | Design Head | Active | None seeded | 18 | Foundation operational page visibility; no commercial/approval expansion | Complete |
| service_head | Service Head | Active | None seeded | 18 | Foundation operational page visibility; no purchase/stock/finance approval expansion | Complete |
| sales_head | Sales Head | Active | None seeded | 18 | Master operations for sales-linked masters; no purchase/stock/finance approval expansion | Complete |
| service_coordinator | Service Coordinator | Active | None seeded | 18 | Foundation operational page visibility only | Complete |
| service_engineer | Service Engineer | Active | None seeded | 18 | Foundation operational page visibility only | Complete |
| sales_engineer | Sales Engineer | Active | None seeded | 18 | Foundation operational page visibility only | Complete |
| it_admin | IT Admin | Active | None seeded | 18 | Audit visibility and selected master/employee configuration; no business approval/full control | Complete |
| customer | Customer Portal User | Active | None seeded | 18 | Explicit deny/default-restricted rows; record-scope enforcement required for customer organization data | Complete |
| vendor | Vendor Portal User | Active | None seeded | 18 | Explicit deny/default-restricted rows; record-scope enforcement required for vendor organization data | Complete |
| document_controller | Document Controller | Active | None seeded | 18 | Foundation operational page visibility only | Complete |
| dcc | DCC / Document Controller | Active | None seeded | 18 | Foundation operational page visibility only | Complete |
| branch_manager | Branch Manager | Active | None seeded | 18 | Foundation operational page visibility only; no MD/TD/commercial escalation | Complete |
| ops_admin_no_hr | Operational Admin without HR | Active | None seeded | 18 | Foundation operational page visibility only; HR/admin approval excluded | Complete |
| technical_director | Technical Director | Active | SESS-001 | 18 | Full control; TD authority restricted to SESS-001 mapping | Complete |
| managing_director | Managing Director | Active | SESS-002 | 18 | Full control; MD authority restricted to SESS-002 mapping | Complete |
| technical_support_manager | Technical Support Manager | Active | SESS-004 | 18 | Explicit deny unless separately authorized later | Complete |
| accounts_assistant | Accounts Assistant | Active | SESS-007, SESS-027, SESS-031 | 18 | Explicit deny for approval/commercial/export/admin actions | Complete |
| software_developer | Software Developer | Active | SESS-008 | 18 | Explicit deny for business approval/commercial/export/admin actions | Complete |
| purchase_executive | Purchase Executive | Active | SESS-012 | 18 | Purchase view/create/update/submit/cancel/print/download/upload/replace/resubmit; no PO approval, commercial visibility, export, or full control | Complete |
| stores_executive | Stores Executive | Active | SESS-012 | 18 | Inventory view/create/update/submit/cancel/print/download/upload/replace/resubmit; no stock-adjustment approval, commercial visibility, export, or full control | Complete |
| stores_assistant | Stores Assistant | Active | SESS-014 | 18 | Inventory entry support; no stock-adjustment approval, commercial visibility, export, or full control | Complete |
| hr_executive | HR Executive | Active | SESS-020 | 18 | Employee page view/create/update/submit/cancel/print/download/upload/replace/resubmit; no business approval/full control | Complete |
| admin_executive | Admin Executive | Active | SESS-021 | 18 | Explicit deny unless separately authorized later | Complete |
| production_coordinator | Production Coordinator | Active | SESS-023 | 18 | Explicit deny unless separately authorized later | Complete |
| technical_engineer | Technical Engineer | Active | SESS-003, SESS-028, SESS-029, SESS-034, SESS-036, SESS-037, SESS-039 | 18 | Explicit deny for purchase approval, finance, admin, export, and commercial actions | Complete |
| electrical_engineer | Electrical Engineer | Active | SESS-010, SESS-018, SESS-022, SESS-024, SESS-030 | 18 | Explicit deny for purchase approval, finance, admin, export, and commercial actions | Complete |
| plc_engineer | PLC Engineer | Active | SESS-038 | 18 | Explicit deny for purchase approval, finance, admin, export, and commercial actions | Complete |
| design_engineer | Design Engineer | Active | SESS-015, SESS-016, SESS-019 | 18 | Explicit deny for purchase approval, finance, admin, export, and commercial actions | Complete |
| junior_engineer | Junior Engineer | Active | SESS-006, SESS-009, SESS-011, SESS-017, SESS-033 | 18 | Explicit deny for purchase approval, finance, admin, export, and commercial actions | Complete |
| production_operator | Production Operator | Active | SESS-005, SESS-013, SESS-025, SESS-026, SESS-035 | 18 | Explicit deny for purchase approval, finance, admin, export, and commercial actions | Complete |
| software_engineer | Software Engineer | Active | SESS-032 | 18 | Explicit deny for business approval, finance, admin, export, and commercial actions | Complete |

Security notes:

- TD and MD authority is restricted to the approved SESS-001 and SESS-002 employee mappings.
- Purchase/Stores entry roles do not automatically include PO approval, stock-adjustment approval, or financial approval.
- Software Developer/Admin-style roles do not automatically include business approval.
- Customer and Vendor roles remain subject to backend record-scope checks for their own organizations.
