# REV869 Legacy Reuse Final Mapping Report

## 1. Executive verdict

This source-only, read-only discovery was performed from mandatory clean checkpoint `b388d46aada34a195f8f2c279179b71e6fb59bd8`. The archives contain useful requirement, field, relationship, UI, algorithm and test evidence, but **no legacy implementation is approved for import or execution**.

The current .NET/PostgreSQL/OIDC/AWS architecture is authoritative. Reuse means rewriting a reviewed concept or test. It never means copying local JSON persistence, shared/local login, legacy JWT, browser authorization, legacy SQL, Flutter/.NET/JavaScript implementation, demo identities, deployment configuration or data.

Strongest value: Purchase/Stores cross-checks, GRN/issue/ledger fields, QC gates/tests, mobile/report UI ideas, and future Project/Machine/production/manpower/attendance/service/warranty/AMC concepts. None is production proof.

## 2. Starting commit and archive hashes

- Starting HEAD: `b388d46aada34a195f8f2c279179b71e6fb59bd8`
- `SESS_NexaERP_REV597_UPGRADE.zip`: SHA-256 `CE1E45EA7516B0B3F9389B462C59FFAEAAAB62962023E105D9F273202F09F41A`; 981,471 bytes.
- `sess-emp-app-9.zip`: SHA-256 `871BBEF3401E2EFA80ADC900DDB30C03A08E3642A46F14D76E374FA04292C3A6`; 688,677 bytes.

Hashes identify inspected bytes only; they do not approve provenance, correctness, security or migration eligibility.

## 3. Archive/file inventory

| Archive | Entries | Files | Directories | Contents |
|---|---:|---:|---:|---|
| REV597 | 33 | 28 | 5 | 23 JS, SQL, HTML, changelog and two binary assets |
| Employee app | 524 | 431 | 93 | 195 C#, 137 Dart, 70 Markdown, SQL and build/deployment/configuration |
| **Total** | **557** | **459** | **98** | Every file appears once in Appendix A |

## 4. Classification counts

| Classification | Count |
|---|---:|
| REUSE_AS_REQUIREMENT | 60 |
| UI_REFERENCE_ONLY | 136 |
| FIELD_AND_RELATIONSHIP_REFERENCE | 29 |
| ALGORITHM_CANDIDATE_REWRITE | 10 |
| REUSE_AS_TEST_CASE | 10 |
| MIGRATE_DATA_LATER | 0 |
| ALREADY_IMPLEMENTED_CURRENT | 12 |
| DEFER_TO_FUTURE_REVISION | 133 |
| DO_NOT_IMPORT | 51 |
| REJECT_SECURITY_RISK | 18 |
| **Total** | **459** |

`MIGRATE_DATA_LATER` is zero because neither ZIP is an approved business-data export.

## 5. Exact file/module mapping

Appendix A gives the exact archive/internal path, primary classification and decision ID for every file. Each file inherits all details in its decision row.

| ID | Purpose and reusable concept | Unsafe details / current conflict / risk | Target, rewrite/test, saving and blocker |
|---|---|---|---|
| R1 | REV597 server, backup/security and payroll seed; only session/audit/payroll vocabulary is reusable | Local JSON/server/process/file trust, embedded identifiable payroll/person data and fixed identities conflict with EF/PostgreSQL/OIDC/AWS; critical privacy/auth/tamper risk | REJECT_SECURITY_RISK; never import; fresh threat model; 1–2 days; security/privacy owner required |
| R2 | REV597 foundation/roles/approval/workflow/phases; statuses, queues, maker-checker and PR→RFQ→PO→GRN intent | Browser state, fixed role maps, client auth and mutable history conflict with REV868/REV869A; duplicate-flow risk | Rewrite REV869B–F state machines/denial tests; 6–10 days; ownership/approval/numbering |
| R3 | REV597 QC/testing; parameter/method/tolerance/rework/hold/reject/dispatch concepts | Fixed client templates/free text/client gates conflict with REV869A policy and REV869D ownership | REUSE_AS_TEST_CASE; policy/boundary/history tests; 8–12 days; QC owner approval |
| R4 | REV597 HTML/report/dashboard/mobile/page/assets; queues/filters/export/navigation | Monolith, client hiding and unverified asset rights; no frontend counterpart | UI_REFERENCE_ONLY/DO_NOT_IMPORT; rebuild REV869F; 8–15 days; frontend/design/licensing |
| R5 | REV597 SQL schema; candidate entity/relationship vocabulary | Non-EF, collision/destructive/incomplete-integrity risk | FIELD_AND_RELATIONSHIP_REFERENCE only; 2–4 days; model/data ownership |
| R6 | Other REV597 modules/changelog; reminders/payroll/operations/document ideas | Hard-coded maps, browser globals and unverified completion claims | Requirement interviews only; 3–6 days; business validation |
| E1 | Employee-app auth/startup/seed; generic login/session-failure wording only | Local password JWT/demo seeds/startup initialization directly conflict with unique OIDC issuer+subject→active employee | REJECT_SECURITY_RISK; OIDC tests only; no implementation saving; production OIDC blocker |
| E2 | Build/deploy/AWS/Docker/config; checklist topics only | Old framework/environment assumptions and possible secrets conflict with current AWS direction | DO_NOT_IMPORT; fresh IaC/security review; 1–2 days; platform decision |
| E3 | Purchase/Stores/Inventory/Vendor; stock-or-buy, PR detail, GRN batch/serial/dates, DC/issue/returnable and reconciliation | Direct balance mutation, free-text identities/UOM/location, weak status/tax/idempotency; must not duplicate REV868 PR/check/reservation/PendingRFQ or REV869A foundations | Rewrite REV869B–E and denial/reconciliation tests; 12–20 days; UOM, Project SoR, posting/valuation/GRN ownership |
| E4 | QC/FAT/PDI; separation and pass-before-dispatch | Boolean shortcuts, role strings and missing effective policy conflict with REV869A/REV869D | Rewrite fail-closed measurement/override/condition tests; 5–8 days; QC/Machine boundary |
| E5 | Employee/role/audit; active employee, assignment and audit concepts | Username/string/broad roles and demos conflict with REV866/REV868C3/REV869A | ALREADY_IMPLEMENTED_CURRENT or reference; regression only; 2–3 days; real OIDC |
| E6 | Project/production/machine/job order/BOM/allocation/manpower | PO→Job Order→Machine, BOM, tasks, booking, progress, consumption | Duplicate masters/string joins/demo people/weak concurrency; REV869A forbids duplicate Project/Customer | DEFER to Machine Project/lifetime-cost revision; 20–35 days; SoR and cost ownership |
| E7 | Attendance/HR/payroll/task/KPI/incentive/expense/shift/location | Punch/correction/shift/task/manpower/KPI vocabulary | GPS/privacy, fixed timezone, mutable scores and person demos; outside REV869 | DEFER; privacy and deterministic formula tests; 15–25 days; HR/legal/timekeeping |
| E8 | Service/AMC/warranty/calibration/installation/dispatch | Machine 360°, coverage/history/due dates/gates | Free-text machine/customer and weak entitlement conflict with future Machine identity | DEFER to Installed Base & Service; 20–30 days; identity/entitlement |
| E9 | Flutter/mobile/preview UI; forms/queues/attachments/signatures/filters | Demo fallback, client visibility, local auth and endpoint coupling; no REV869A frontend | UI_REFERENCE_ONLY; rebuild in owning frontend; 15–25 days; platform/offline/retention |
| E10 | Calculators/generators/export/reminders/tests; formula/template/trigger ideas | Unapproved constants/incomplete coverage | ALGORITHM_CANDIDATE_REWRITE or REUSE_AS_TEST_CASE; 4–8 days; formula approval |
| E11 | SQL/DbContext/entities/enums; fields/relationships/uniqueness intent | Incomplete FKs, strings and mutable balances conflict with EF/current models | FIELD_AND_RELATIONSHIP_REFERENCE; model anew; 5–10 days; owner reconciliation |
| E12 | Documentation; vocabulary/screen inventory/acceptance ideas | Static “complete/verified” claims are not proof | REUSE_AS_REQUIREMENT or defer; 8–12 days; owner sign-off |
| E13 | Miscellaneous supporting source/config | Hidden dependencies/trust assumptions | DO_NOT_IMPORT or future fresh design; 1–3 days; ownership |

## 6. Reusable business requirements

1. Preserve REV868 approved PR, stock check, reservation and PendingRFQ identities as the only REV869 sourcing entry.
2. Available stock routes to reservation/issue; shortage routes to PendingRFQ. Never duplicate PR/RFQ/GRN/posting.
3. Candidate line evidence includes item code/specification/make/model/part/required date/priority/demand snapshot.
4. GRN evidence may include ordered/received/accepted/rejected/hold, batch, serial and manufacture/expiry data.
5. Issue requires AVAILABLE stock, scope, controlled UOM/base quantity, reservation, receiver and immutable posting.
6. Returns use linked compensating postings, never destructive edits.
7. Ledger reconciliation is valid; immutable postings—not mutable `StockQty`—are authoritative.
8. Vendor rating is future; eligibility remains Active+Approved+Effective with segregated approval.
9. QC needs parameter/method/UOM/limits/sample/evidence and hold/rework/reject; missing policy means QC_HOLD.
10. Dispatch/downstream gates are backend transitions, never UI warnings.
11. Project/Machine concepts wait for approved systems of record/lifetime-cost ownership.
12. Attendance/manpower may feed cost only after privacy, correction and timekeeping approval.

## 7. UI and report references

Useful references are role pending cards, Purchase/GRN/stock forms, mobile My Work, Machine 360°, scoped report filters, attachment/photo/signature controls and offline indicators. Backend permission and record scope must protect every query/command.

## 8. Algorithm candidates requiring rewrite

KPI/salary/incentive formulas, task templates/progress, reminder/outbox scheduling, scoped exports, weighted-average/reconciliation and availability/issue logic require new specifications and code. Reuse current REV868 availability/reservation; reject legacy direct quantity mutation.

## 9. Test cases worth recreating

Available-versus-shortage routing; idempotency; received quantity decomposition; AVAILABLE-only issue; missing-QC-policy hold; remarked authorized override; QC/PDI dispatch gate; immutable stock reconciliation by warehouse/RackBin/condition; manpower overlap; effective warranty/AMC/calibration boundaries; approved-only progress; direct API denial.

## 10. Data that may be migrated later

No archive file is approved data. Future candidate domains are vendor/contact history, item aliases, opening-stock evidence, installed machines, service/AMC/calibration history, project/task history and attendance/time. Each requires provenance, owner approval, deduplication, current IDs, privacy/retention, reconciliation and isolated acceptance. The embedded payroll/person seed is rejected.

## 11. Already-implemented current features

Current target foundations are authoritative for Customer, Vendor, Item, Warehouse/RackBin, employee/roles/permissions/audit, REV868 PR/check/reservation/PendingRFQ, and REV869A identity/scope/UOM/tax/vendor qualification/warehouse-condition/QC policy. Legacy counterparts are comparison evidence only.

## 12. Deferred modules and revisions

| Module | Target |
|---|---|
| RFQ/quotation | REV869B |
| Comparison/PO | REV869C |
| Follow-up/GRN/transactional QC | REV869D |
| Posting/issue/return/ledger/aging/consumption | REV869E |
| Purchase/Stores frontend/E2E | REV869F |
| Project/Machine/lifetime cost | New approved revision after SoR decision |
| Production/manpower | Future Production Execution |
| Attendance/HR/payroll | Future Workforce with privacy/legal approval |
| Service/warranty/AMC/calibration | Future Installed Base & Service |
| Mobile/reporting | Owning frontend revisions |

## 13. Must not be imported

Both ZIPs/extracted/binary artifacts; Node/HTML/JS server/security/backup; SQL; old solution/build/Docker/deploy/AWS/config; Flutter and old .NET implementation; startup/seed logic; demo/employee/payroll data; role rows/fixed identities; local/shared login/JWT; unlicensed assets.

## 14. Security-risk rejections

Reject local JSON/server trust, local/shared JWT identity, email/name/code linking, fixed approvers, client authorization, broad combined roles, demo identities/payroll data, mutable stock, unscoped endpoints, legacy backup/upload/export/deploy behavior, free-text master joins, SQL execution, EnsureCreated-like initialization and duplicate schema ownership.

## 15. UOM candidate evidence and unresolved decisions

Legacy has no UOM/dimension/conversion master, effective dating or immutable snapshot. Nullable free text appears on Item, PurchaseLineItem, GRN line, StoreIssue, MaterialRequest, BOM, service spare and production PR concepts.

Observed demo/reference tokens: `m` (likely length), `kg` (likely mass), and `no` (likely count, but ambiguous). None is authoritative. Case/plural/alias collisions, item-versus-transaction differences, base status, dimension, conversion, precision and approval are absent.

Management must approve the authoritative catalogue/dimensions, alias/rejection policy, every active item Base UOM from current controlled data, conversions/factors/precision/effective dates, fail-closed remediation and used-conversion immutability evidence.

**UOM status: CANDIDATE_ONLY / NOT_AUTHORITATIVE / NO BACKFILL MAPPING CREATED.**

## 16. Zero-interruption/current-platform protection

Preserve all REV868/REV868C3 data/history/behavior and REV869A source/tooling. Keep issuer+subject identity, backend role∩department∩warehouse∩RackBin scope, EF/PostgreSQL ownership, immutable history, no duplicate Customer/Project/Machine, UTC/display timezone, localization/tax/currency readiness, AWS direction and the 300,000-user target.

## 17. Estimated time saving

| Module | Person-days |
|---|---:|
| Purchase/Stores | 12–20 |
| QC | 8–12 |
| Purchase/Stores UI | 10–18 |
| Reports | 5–9 |
| Project/Machine | 12–20 |
| Production/manpower | 8–14 |
| Service/warranty/AMC | 12–20 |
| Attendance/HR/payroll | 6–10 |
| Tests | 6–10 |
| **Potential, non-additive** | **79–133** |

No source reuse is included.

## 18. Recommended adoption order

Accept evidence/rejections; resolve UOM from current controlled data; complete isolated REV869A only through separate helper approvals; feed requirements/tests to REV869B–E; rebuild UI in REV869F; decide Project/Machine before production/service; address Workforce after privacy/legal approval.

## 19. Exact next approval gate

Management must explicitly approve that this report is evidence-only; no legacy code/schema/config/identity/seed/data is imported; UOM authority comes from current controlled data; Project/Machine/Workforce/Service remain deferred; and any REV869A helper activity is separately authorized, beginning with `GeneratePlanOnly` only against exactly `sess_nexaerp_rev869a_verify`.

Until then: **discovery complete; no implementation, migration, helper or database action authorized.**

## Appendix A — exhaustive file inventory

Every file appears once and inherits its decision-row details.

| # | Archive | Exact internal path | Primary classification | Decision ID |
|---:|---|---|---|---|
| 1 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/ai-document-intelligence-module.js` | REUSE_AS_REQUIREMENT | R6 |
| 2 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/approval-workflow-module.js` | REUSE_AS_TEST_CASE | R2 |
| 3 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/assets/SESS_NexaERP_Master_Server_Icon_v2.ico` | DO_NOT_IMPORT | R4 |
| 4 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/assets/sess-nexa-login-logo-v2.png` | DO_NOT_IMPORT | R4 |
| 5 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/dashboard-kpi-module.js` | UI_REFERENCE_ONLY | R4 |
| 6 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/db/schema.sql` | FIELD_AND_RELATIONSHIP_REFERENCE | R5 |
| 7 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/electrical-plc-qc-module.js` | REUSE_AS_TEST_CASE | R3 |
| 8 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/final-qc-gates-module.js` | REUSE_AS_TEST_CASE | R3 |
| 9 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/foundation-phase1-module.js` | REUSE_AS_REQUIREMENT | R2 |
| 10 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/InventoryERP_Software.html` | UI_REFERENCE_ONLY | R4 |
| 11 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/mobile-role-ui-module.js` | UI_REFERENCE_ONLY | R4 |
| 12 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/monthly-payroll-module.js` | REUSE_AS_REQUIREMENT | R6 |
| 13 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/monthly-payroll-seed-2026-03.js` | REJECT_SECURITY_RISK | R1 |
| 14 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/notification-reminder-module.js` | REUSE_AS_REQUIREMENT | R6 |
| 15 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/overall-mechanical-qc-module.js` | REUSE_AS_TEST_CASE | R3 |
| 16 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/page-master-module.js` | UI_REFERENCE_ONLY | R4 |
| 17 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/phase2-golive-module.js` | REUSE_AS_REQUIREMENT | R2 |
| 18 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/phase3-operations-integration-module.js` | REUSE_AS_REQUIREMENT | R2 |
| 19 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/powder-coating-qc-module.js` | REUSE_AS_TEST_CASE | R3 |
| 20 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/refrigeration-qc-module.js` | REUSE_AS_TEST_CASE | R3 |
| 21 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/remaining-steps-module.js` | REUSE_AS_REQUIREMENT | R2 |
| 22 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/reports-mis-module.js` | UI_REFERENCE_ONLY | R4 |
| 23 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/role-permission-module.js` | REUSE_AS_REQUIREMENT | R2 |
| 24 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/security-backup-audit-module.js` | REJECT_SECURITY_RISK | R1 |
| 25 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/testing-checklist-module.js` | REUSE_AS_TEST_CASE | R3 |
| 26 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/app/workflow-automation-module.js` | REUSE_AS_TEST_CASE | R2 |
| 27 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/CHANGELOG_REV597.txt` | REUSE_AS_REQUIREMENT | R6 |
| 28 | `SESS_NexaERP_REV597_UPGRADE.zip` | `REV597/server/server.js` | REJECT_SECURITY_RISK | R1 |
| 29 | `sess-emp-app-9.zip` | `sess-emp-app/.github/workflows/build.yml` | DO_NOT_IMPORT | E2 |
| 30 | `sess-emp-app-9.zip` | `sess-emp-app/.gitignore` | DO_NOT_IMPORT | E2 |
| 31 | `sess-emp-app-9.zip` | `sess-emp-app/backend/.dockerignore` | DO_NOT_IMPORT | E2 |
| 32 | `sess-emp-app-9.zip` | `sess-emp-app/backend/Dockerfile` | DO_NOT_IMPORT | E2 |
| 33 | `sess-emp-app-9.zip` | `sess-emp-app/backend/SESS.sln` | DO_NOT_IMPORT | E2 |
| 34 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/appsettings.json` | DO_NOT_IMPORT | E13 |
| 35 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/AccountsController.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 36 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/AllocationController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 37 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/AmcController.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 38 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/AppraisalController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 39 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/AttendanceController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 40 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/AuditController.cs` | ALREADY_IMPLEMENTED_CURRENT | E5 |
| 41 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/AuthController.cs` | REJECT_SECURITY_RISK | E1 |
| 42 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/CalibrationController.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 43 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/CustomerMachineController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 44 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/CustomerMasterController.cs` | ALREADY_IMPLEMENTED_CURRENT | E13 |
| 45 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/CustomerPoController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 46 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/CustomerPortalController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 47 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/DailyReportController.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 48 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/DashboardController.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 49 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/DevicesController.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 50 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/DispatchController.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 51 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/EmployeeController.cs` | ALREADY_IMPLEMENTED_CURRENT | E5 |
| 52 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/EngineerWorkController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 53 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ExpenseController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 54 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/FileController.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 55 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/GatePassController.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 56 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/GrnController.cs` | REUSE_AS_REQUIREMENT | E3 |
| 57 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/HolidayController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 58 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/IncentiveController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 59 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/InstallationController.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 60 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/InternalAssetController.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 61 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/InventoryController.cs` | REUSE_AS_REQUIREMENT | E3 |
| 62 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/InvoiceController.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 63 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/JobOrderController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 64 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/KpiController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 65 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/LeaveController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 66 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/LifecycleController.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 67 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/LocationController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 68 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/MachineBookingController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 69 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/MachineLedgerController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 70 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/MessagingController.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 71 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/NotificationController.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 72 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/OtController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 73 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ProductionController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 74 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ProductionManagerMonitorController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 75 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ProductionPlanController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 76 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ProductionProjectController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 77 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ProductionPurchaseRequestController.cs` | REUSE_AS_REQUIREMENT | E6 |
| 78 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ProductionTaskController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 79 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ProductionTeamController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 80 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ProjectController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 81 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ProjectPerformanceController.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 82 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/PurchaseController.cs` | REUSE_AS_REQUIREMENT | E3 |
| 83 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/QcFatPdiController.cs` | REUSE_AS_REQUIREMENT | E4 |
| 84 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ReminderDashboardController.cs` | DEFER_TO_FUTURE_REVISION | E10 |
| 85 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/RemindersController.cs` | DEFER_TO_FUTURE_REVISION | E10 |
| 86 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ReportController.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 87 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/SalaryController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 88 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ServiceChecklistController.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 89 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ServiceController.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 90 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ServicePerformanceController.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 91 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ServiceReportController.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 92 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/ShiftController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 93 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/StoreController.cs` | REUSE_AS_REQUIREMENT | E3 |
| 94 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/TaskController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 95 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/TaskPerformanceController.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 96 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Controllers/VendorMasterController.cs` | ALREADY_IMPLEMENTED_CURRENT | E3 |
| 97 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/DbInitializer.cs` | REJECT_SECURITY_RISK | E1 |
| 98 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/DevSeed.cs` | REJECT_SECURITY_RISK | E1 |
| 99 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Filters/AuditFilter.cs` | ALREADY_IMPLEMENTED_CURRENT | E5 |
| 100 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Middleware/ExceptionMiddleware.cs` | DO_NOT_IMPORT | E13 |
| 101 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Program.cs` | REJECT_SECURITY_RISK | E1 |
| 102 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Services/ReminderBackgroundService.cs` | ALGORITHM_CANDIDATE_REWRITE | E8 |
| 103 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/Services/ReminderRunner.cs` | ALGORITHM_CANDIDATE_REWRITE | E8 |
| 104 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Api/SESS.Api.csproj` | DO_NOT_IMPORT | E2 |
| 105 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Common/Interfaces/IApplicationDbContext.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 106 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Common/Interfaces/IFileStorage.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 107 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Common/Interfaces/IJwtService.cs` | REJECT_SECURITY_RISK | E1 |
| 108 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Common/Interfaces/IMessageChannel.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 109 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Common/Interfaces/INotificationService.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 110 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Common/Interfaces/IPushSender.cs` | DEFER_TO_FUTURE_REVISION | E13 |
| 111 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/DTOs/Auth/LoginRequest.cs` | REJECT_SECURITY_RISK | E1 |
| 112 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/DTOs/Auth/LoginResponse.cs` | REJECT_SECURITY_RISK | E1 |
| 113 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Incentive/IncentiveCalculator.cs` | ALGORITHM_CANDIDATE_REWRITE | E7 |
| 114 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Kpi/KpiCalculator.cs` | ALGORITHM_CANDIDATE_REWRITE | E7 |
| 115 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Kpi/KpiMetrics.cs` | DO_NOT_IMPORT | E7 |
| 116 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Payroll/PayrollSettings.cs` | DO_NOT_IMPORT | E7 |
| 117 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Payroll/SalaryCalculator.cs` | ALGORITHM_CANDIDATE_REWRITE | E7 |
| 118 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Production/ProductionTaskTemplates.cs` | ALGORITHM_CANDIDATE_REWRITE | E6 |
| 119 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Reports/IReportExporter.cs` | ALGORITHM_CANDIDATE_REWRITE | E10 |
| 120 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Reports/ReportTable.cs` | DO_NOT_IMPORT | E13 |
| 121 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Service/ServiceTaskTemplates.cs` | ALGORITHM_CANDIDATE_REWRITE | E7 |
| 122 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/SESS.Application.csproj` | DO_NOT_IMPORT | E2 |
| 123 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Application/Tasks/TaskGenerator.cs` | ALGORITHM_CANDIDATE_REWRITE | E7 |
| 124 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Common/BaseEntity.cs` | DO_NOT_IMPORT | E13 |
| 125 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/AmcContract.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 126 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/AmcDocument.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 127 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/AmcMachine.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 128 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/AmcPayment.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 129 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/AmcVisit.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 130 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/AppSetting.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 131 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/AttendanceCorrection.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 132 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/AttendanceSession.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 133 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/AuditLog.cs` | ALREADY_IMPLEMENTED_CURRENT | E5 |
| 134 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/CalibrationDocument.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 135 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/CalibrationInstrument.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 136 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/CalibrationLedger.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 137 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/CalibrationVisit.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 138 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/CustomerMachine.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 139 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/CustomerMaster.cs` | ALREADY_IMPLEMENTED_CURRENT | E11 |
| 140 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/CustomerPoLedger.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 141 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/DailyWorkEntry.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 142 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/DailyWorkReport.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 143 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/DeviceToken.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 144 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/DispatchLedger.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 145 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EmployeeAdvance.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E5 |
| 146 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EmployeeAppraisal.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E5 |
| 147 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EmployeeDocument.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E5 |
| 148 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EmployeeFamilyDetails.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E5 |
| 149 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EmployeeInsurance.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E5 |
| 150 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EmployeeLifecycle.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E5 |
| 151 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EmployeeMaster.cs` | ALREADY_IMPLEMENTED_CURRENT | E5 |
| 152 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EmployeePreviousCompany.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E5 |
| 153 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EmployeeRating.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E5 |
| 154 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EmployeeSalaryStructure.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E5 |
| 155 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EmployeeStatutoryDetails.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E5 |
| 156 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/EngineerBooking.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 157 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ExpenseClaim.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 158 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/GatePass.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 159 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/Grn.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E3 |
| 160 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/GrnLineItem.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E3 |
| 161 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/Holiday.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 162 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/InstallationActivity.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 163 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/InstallationChecklistItem.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E8 |
| 164 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/InternalAsset.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 165 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ItemMaster.cs` | ALREADY_IMPLEMENTED_CURRENT | E3 |
| 166 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/JobOrder.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 167 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/LeaveRequest.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 168 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/LocationLog.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 169 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/MachineBooking.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 170 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/MachineLedger.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 171 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/MaterialRequest.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E3 |
| 172 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/Notification.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 173 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/OvertimeRequest.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 174 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProductionDailyPlan.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 175 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProductionProject.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 176 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProductionProjectDocument.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 177 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProductionPurchaseRequest.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E6 |
| 178 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProductionPurchaseRequestItem.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E6 |
| 179 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProductionTaskAssignment.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 180 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProductionTaskAttachment.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 181 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProductionTaskTemplate.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 182 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProductionTaskUpdate.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 183 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProjectBomItem.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E6 |
| 184 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProjectMaster.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 185 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ProjectTimelineStage.cs` | DEFER_TO_FUTURE_REVISION | E6 |
| 186 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/PurchaseBill.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E3 |
| 187 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/PurchaseLineItem.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E3 |
| 188 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/QcFatPdiLedger.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E4 |
| 189 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/Role.cs` | ALREADY_IMPLEMENTED_CURRENT | E5 |
| 190 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/SalarySlip.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 191 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ServiceChecklistTask.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 192 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ServiceCustomerFeedback.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 193 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ServiceEveningReport.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 194 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ServiceMorningReport.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 195 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ServiceQuote.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 196 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ServiceReport.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 197 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ServiceReportAttachment.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 198 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ServiceReportLedger.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 199 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ServiceSparePart.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 200 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ServiceTicket.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 201 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/ServiceVisit.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 202 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/Shift.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 203 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/SpareRequest.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 204 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/StockLedger.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E3 |
| 205 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/StoreIssue.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E3 |
| 206 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/TaskChangeRequest.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 207 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/TaskItem.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E7 |
| 208 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/TaskUpdate.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 209 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/TaxInvoice.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 210 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/User.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E11 |
| 211 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/UserRole.cs` | ALREADY_IMPLEMENTED_CURRENT | E5 |
| 212 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/VendorMaster.cs` | ALREADY_IMPLEMENTED_CURRENT | E3 |
| 213 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Entities/VendorRating.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E11 |
| 214 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Enums/ApprovalStatus.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 215 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Enums/ProcurementEnums.cs` | DEFER_TO_FUTURE_REVISION | E3 |
| 216 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Enums/RoleType.cs` | FIELD_AND_RELATIONSHIP_REFERENCE | E5 |
| 217 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Enums/ServiceEnums.cs` | DEFER_TO_FUTURE_REVISION | E8 |
| 218 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/Enums/TaskStatusType.cs` | DEFER_TO_FUTURE_REVISION | E7 |
| 219 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Domain/SESS.Domain.csproj` | DO_NOT_IMPORT | E2 |
| 220 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Infrastructure/Auth/JwtService.cs` | REJECT_SECURITY_RISK | E1 |
| 221 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Infrastructure/Messaging/EmailChannel.cs` | DO_NOT_IMPORT | E13 |
| 222 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Infrastructure/Messaging/MultiChannelNotifier.cs` | DO_NOT_IMPORT | E13 |
| 223 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Infrastructure/Messaging/SmsChannel.cs` | DO_NOT_IMPORT | E13 |
| 224 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Infrastructure/Messaging/WhatsAppChannel.cs` | DO_NOT_IMPORT | E13 |
| 225 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Infrastructure/Notifications/LoggingPushSender.cs` | DO_NOT_IMPORT | E13 |
| 226 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Infrastructure/Notifications/NotificationService.cs` | DO_NOT_IMPORT | E8 |
| 227 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Infrastructure/Reports/ReportExporter.cs` | ALGORITHM_CANDIDATE_REWRITE | E10 |
| 228 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Infrastructure/SESS.Infrastructure.csproj` | DO_NOT_IMPORT | E2 |
| 229 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Infrastructure/Storage/LocalFileStorage.cs` | DO_NOT_IMPORT | E13 |
| 230 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Infrastructure/Storage/S3FileStorage.cs` | DO_NOT_IMPORT | E13 |
| 231 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Persistence/ApplicationDbContext.cs` | DEFER_TO_FUTURE_REVISION | E11 |
| 232 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Persistence/Migrations/.gitkeep` | DO_NOT_IMPORT | E2 |
| 233 | `sess-emp-app-9.zip` | `sess-emp-app/backend/src/SESS.Persistence/SESS.Persistence.csproj` | DO_NOT_IMPORT | E2 |
| 234 | `sess-emp-app-9.zip` | `sess-emp-app/backend/tests/SESS.Tests/KpiCalculatorTests.cs` | REUSE_AS_TEST_CASE | E7 |
| 235 | `sess-emp-app-9.zip` | `sess-emp-app/backend/tests/SESS.Tests/SalaryCalculatorTests.cs` | REUSE_AS_TEST_CASE | E7 |
| 236 | `sess-emp-app-9.zip` | `sess-emp-app/backend/tests/SESS.Tests/SESS.Tests.csproj` | DO_NOT_IMPORT | E2 |
| 237 | `sess-emp-app-9.zip` | `sess-emp-app/database/checks/verify_item_and_ledger.sql` | FIELD_AND_RELATIONSHIP_REFERENCE | E11 |
| 238 | `sess-emp-app-9.zip` | `sess-emp-app/database/schema.sql` | FIELD_AND_RELATIONSHIP_REFERENCE | E11 |
| 239 | `sess-emp-app-9.zip` | `sess-emp-app/deploy/apprunner-service.json` | DO_NOT_IMPORT | E2 |
| 240 | `sess-emp-app-9.zip` | `sess-emp-app/deploy/AWS_DEPLOY_COMMANDS.md` | DO_NOT_IMPORT | E2 |
| 241 | `sess-emp-app-9.zip` | `sess-emp-app/deploy/backup/backup.sh` | DO_NOT_IMPORT | E2 |
| 242 | `sess-emp-app-9.zip` | `sess-emp-app/deploy/backup/restore.sh` | DO_NOT_IMPORT | E2 |
| 243 | `sess-emp-app-9.zip` | `sess-emp-app/deploy/eb/.ebextensions/options.config` | DO_NOT_IMPORT | E2 |
| 244 | `sess-emp-app-9.zip` | `sess-emp-app/deploy/eb/Dockerrun.aws.json` | DO_NOT_IMPORT | E2 |
| 245 | `sess-emp-app-9.zip` | `sess-emp-app/deploy/eb/README.md` | DO_NOT_IMPORT | E2 |
| 246 | `sess-emp-app-9.zip` | `sess-emp-app/deploy/push-image.sh` | DO_NOT_IMPORT | E2 |
| 247 | `sess-emp-app-9.zip` | `sess-emp-app/docker-compose.yml` | DO_NOT_IMPORT | E2 |
| 248 | `sess-emp-app-9.zip` | `sess-emp-app/docs/API_ROUTES.md` | DEFER_TO_FUTURE_REVISION | E12 |
| 249 | `sess-emp-app-9.zip` | `sess-emp-app/docs/AUDIT_FINDINGS.md` | DEFER_TO_FUTURE_REVISION | E5 |
| 250 | `sess-emp-app-9.zip` | `sess-emp-app/docs/AWS_CHECKLIST.md` | DO_NOT_IMPORT | E2 |
| 251 | `sess-emp-app-9.zip` | `sess-emp-app/docs/AWS_DEPLOYMENT.md` | DO_NOT_IMPORT | E2 |
| 252 | `sess-emp-app-9.zip` | `sess-emp-app/docs/BACKUP.md` | DO_NOT_IMPORT | E12 |
| 253 | `sess-emp-app-9.zip` | `sess-emp-app/docs/BUILD_AND_RUN.md` | DO_NOT_IMPORT | E2 |
| 254 | `sess-emp-app-9.zip` | `sess-emp-app/docs/BUILD_AND_SMOKETEST.md` | DO_NOT_IMPORT | E2 |
| 255 | `sess-emp-app-9.zip` | `sess-emp-app/docs/BUILD_HANDOVER.md` | REUSE_AS_REQUIREMENT | E2 |
| 256 | `sess-emp-app-9.zip` | `sess-emp-app/docs/COMPLETION_REPORT.md` | REUSE_AS_REQUIREMENT | E12 |
| 257 | `sess-emp-app-9.zip` | `sess-emp-app/docs/CUSTOMER_MACHINE_MASTER.md` | REUSE_AS_REQUIREMENT | E6 |
| 258 | `sess-emp-app-9.zip` | `sess-emp-app/docs/CUSTOMER_PORTAL.md` | DEFER_TO_FUTURE_REVISION | E12 |
| 259 | `sess-emp-app-9.zip` | `sess-emp-app/docs/DATA_ENTRY_BY_DEPARTMENT.md` | REUSE_AS_REQUIREMENT | E12 |
| 260 | `sess-emp-app-9.zip` | `sess-emp-app/docs/DATA_RETENTION_SCALING.md` | DEFER_TO_FUTURE_REVISION | E12 |
| 261 | `sess-emp-app-9.zip` | `sess-emp-app/docs/DATABASE_SCHEMA.md` | DEFER_TO_FUTURE_REVISION | E12 |
| 262 | `sess-emp-app-9.zip` | `sess-emp-app/docs/DEVELOPMENT_PLAN.md` | REUSE_AS_REQUIREMENT | E12 |
| 263 | `sess-emp-app-9.zip` | `sess-emp-app/docs/DISTRIBUTE_APP.md` | DO_NOT_IMPORT | E12 |
| 264 | `sess-emp-app-9.zip` | `sess-emp-app/docs/EMPLOYEE_LIFECYCLE.md` | REUSE_AS_REQUIREMENT | E5 |
| 265 | `sess-emp-app-9.zip` | `sess-emp-app/docs/ENGINEER_ALLOCATION.md` | DEFER_TO_FUTURE_REVISION | E6 |
| 266 | `sess-emp-app-9.zip` | `sess-emp-app/docs/FLUTTER_SCREENS.md` | DEFER_TO_FUTURE_REVISION | E12 |
| 267 | `sess-emp-app-9.zip` | `sess-emp-app/docs/FURTHER_WORK.md` | DEFER_TO_FUTURE_REVISION | E12 |
| 268 | `sess-emp-app-9.zip` | `sess-emp-app/docs/GATEPASS_VENDOR_APPRAISAL.md` | REUSE_AS_REQUIREMENT | E7 |
| 269 | `sess-emp-app-9.zip` | `sess-emp-app/docs/GOOGLE_MAPS_SETUP.md` | DEFER_TO_FUTURE_REVISION | E12 |
| 270 | `sess-emp-app-9.zip` | `sess-emp-app/docs/HANDOVER.md` | REUSE_AS_REQUIREMENT | E12 |
| 271 | `sess-emp-app-9.zip` | `sess-emp-app/docs/INCENTIVE_ENGINE.md` | DEFER_TO_FUTURE_REVISION | E7 |
| 272 | `sess-emp-app-9.zip` | `sess-emp-app/docs/INSTALLATION_MODULE.md` | DEFER_TO_FUTURE_REVISION | E8 |
| 273 | `sess-emp-app-9.zip` | `sess-emp-app/docs/INTERNAL_ASSETS.md` | REUSE_AS_REQUIREMENT | E12 |
| 274 | `sess-emp-app-9.zip` | `sess-emp-app/docs/INVENTORY.md` | REUSE_AS_REQUIREMENT | E3 |
| 275 | `sess-emp-app-9.zip` | `sess-emp-app/docs/INVOICING.md` | REUSE_AS_REQUIREMENT | E12 |
| 276 | `sess-emp-app-9.zip` | `sess-emp-app/docs/KEYS_SETUP.md` | DO_NOT_IMPORT | E2 |
| 277 | `sess-emp-app-9.zip` | `sess-emp-app/docs/LEAVE_TYPES.md` | REUSE_AS_REQUIREMENT | E7 |
| 278 | `sess-emp-app-9.zip` | `sess-emp-app/docs/LEDGERS_AND_JOB_ORDER.md` | REUSE_AS_REQUIREMENT | E12 |
| 279 | `sess-emp-app-9.zip` | `sess-emp-app/docs/LOGIN_AND_BARCODE.md` | DEFER_TO_FUTURE_REVISION | E12 |
| 280 | `sess-emp-app-9.zip` | `sess-emp-app/docs/MACHINE_BOOKING.md` | REUSE_AS_REQUIREMENT | E6 |
| 281 | `sess-emp-app-9.zip` | `sess-emp-app/docs/MASTERS_LEDGER.md` | REUSE_AS_REQUIREMENT | E12 |
| 282 | `sess-emp-app-9.zip` | `sess-emp-app/docs/MESSAGING.md` | REUSE_AS_REQUIREMENT | E12 |
| 283 | `sess-emp-app-9.zip` | `sess-emp-app/docs/MODULE1_STATUS.md` | REUSE_AS_REQUIREMENT | E12 |
| 284 | `sess-emp-app-9.zip` | `sess-emp-app/docs/MODULE2_AMC_STATUS.md` | REUSE_AS_REQUIREMENT | E8 |
| 285 | `sess-emp-app-9.zip` | `sess-emp-app/docs/MODULE3_CALIBRATION_STATUS.md` | REUSE_AS_REQUIREMENT | E8 |
| 286 | `sess-emp-app-9.zip` | `sess-emp-app/docs/MODULE5_PRODUCTION_REQUIREMENT.md` | REUSE_AS_REQUIREMENT | E6 |
| 287 | `sess-emp-app-9.zip` | `sess-emp-app/docs/MODULE5_STATUS.md` | REUSE_AS_REQUIREMENT | E12 |
| 288 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PAYROLL_STATUTORY.md` | REUSE_AS_REQUIREMENT | E7 |
| 289 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PO_JOBORDER_MACHINE_LEDGER.md` | REUSE_AS_REQUIREMENT | E6 |
| 290 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PROCESS_FLOWS.md` | REUSE_AS_REQUIREMENT | E12 |
| 291 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PROCESS_FLOWS_COMPLETE.md` | REUSE_AS_REQUIREMENT | E12 |
| 292 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PRODUCTION_CHECKLIST.md` | REUSE_AS_REQUIREMENT | E6 |
| 293 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PRODUCTION_MANAGER_MONITORING.md` | REUSE_AS_REQUIREMENT | E6 |
| 294 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PRODUCTION_TEAM_AND_PR.md` | REUSE_AS_REQUIREMENT | E6 |
| 295 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PROJECT_INDEX.md` | REUSE_AS_REQUIREMENT | E6 |
| 296 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PROJECT_PERFORMANCE_KPI.md` | REUSE_AS_REQUIREMENT | E6 |
| 297 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PROJECT_PLAN.md` | REUSE_AS_REQUIREMENT | E6 |
| 298 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PROOF_STATUS.md` | REUSE_AS_REQUIREMENT | E12 |
| 299 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PUNCH_IN_PLAN.md` | REUSE_AS_REQUIREMENT | E12 |
| 300 | `sess-emp-app-9.zip` | `sess-emp-app/docs/PURCHASE_AND_MATERIAL_REQUEST.md` | REUSE_AS_REQUIREMENT | E3 |
| 301 | `sess-emp-app-9.zip` | `sess-emp-app/docs/QA_TEST_SHEET.md` | DEFER_TO_FUTURE_REVISION | E12 |
| 302 | `sess-emp-app-9.zip` | `sess-emp-app/docs/QA_TRIAL_REPORT.md` | DEFER_TO_FUTURE_REVISION | E12 |
| 303 | `sess-emp-app-9.zip` | `sess-emp-app/docs/QC_DISPATCH.md` | REUSE_AS_REQUIREMENT | E4 |
| 304 | `sess-emp-app-9.zip` | `sess-emp-app/docs/REQUIREMENTS_GAP.md` | REUSE_AS_REQUIREMENT | E12 |
| 305 | `sess-emp-app-9.zip` | `sess-emp-app/docs/ROLE_ACCESS_MATRIX.md` | REUSE_AS_REQUIREMENT | E5 |
| 306 | `sess-emp-app-9.zip` | `sess-emp-app/docs/ROLES_PERMISSIONS.md` | REUSE_AS_REQUIREMENT | E5 |
| 307 | `sess-emp-app-9.zip` | `sess-emp-app/docs/SERVICE_AMC_CALIBRATION_FINAL_STATUS.md` | REUSE_AS_REQUIREMENT | E8 |
| 308 | `sess-emp-app-9.zip` | `sess-emp-app/docs/SERVICE_TASK_MONITORING.md` | REUSE_AS_REQUIREMENT | E7 |
| 309 | `sess-emp-app-9.zip` | `sess-emp-app/docs/SERVICE_WORKFLOW_STATUS.md` | REUSE_AS_REQUIREMENT | E8 |
| 310 | `sess-emp-app-9.zip` | `sess-emp-app/docs/SHIFT_ROSTER.md` | REUSE_AS_REQUIREMENT | E7 |
| 311 | `sess-emp-app-9.zip` | `sess-emp-app/docs/START_HERE.md` | REUSE_AS_REQUIREMENT | E12 |
| 312 | `sess-emp-app-9.zip` | `sess-emp-app/docs/TASK_PERFORMANCE.md` | REUSE_AS_REQUIREMENT | E7 |
| 313 | `sess-emp-app-9.zip` | `sess-emp-app/docs/WORK_MONITORING_FOUNDATION.md` | DEFER_TO_FUTURE_REVISION | E12 |
| 314 | `sess-emp-app-9.zip` | `sess-emp-app/http/api-samples.http` | DO_NOT_IMPORT | E2 |
| 315 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/BUILD_APK.md` | DO_NOT_IMPORT | E2 |
| 316 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/api/api_client.dart` | UI_REFERENCE_ONLY | E9 |
| 317 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/api/file_download_service.dart` | UI_REFERENCE_ONLY | E8 |
| 318 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/api/file_upload_service.dart` | UI_REFERENCE_ONLY | E8 |
| 319 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/app_config.dart` | UI_REFERENCE_ONLY | E9 |
| 320 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/auth/app_lock.dart` | REJECT_SECURITY_RISK | E1 |
| 321 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/auth/auth_service.dart` | REJECT_SECURITY_RISK | E1 |
| 322 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/auth/auth_state.dart` | REJECT_SECURITY_RISK | E1 |
| 323 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/models/app_models.dart` | UI_REFERENCE_ONLY | E9 |
| 324 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/storage/secure_storage.dart` | REJECT_SECURITY_RISK | E9 |
| 325 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/theme.dart` | UI_REFERENCE_ONLY | E9 |
| 326 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/widgets/app_widgets.dart` | UI_REFERENCE_ONLY | E9 |
| 327 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/core/widgets/signature_pad.dart` | UI_REFERENCE_ONLY | E9 |
| 328 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/main.dart` | UI_REFERENCE_ONLY | E9 |
| 329 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/accounts/accounts_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 330 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/accounts/accounts_service.dart` | UI_REFERENCE_ONLY | E8 |
| 331 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/allocation/allocation_service.dart` | UI_REFERENCE_ONLY | E6 |
| 332 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/allocation/engineer_availability_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 333 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/allocation/manpower_utilization_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 334 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/amc/amc_contract_detail_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 335 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/amc/amc_contracts_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 336 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/amc/amc_dashboard_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 337 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/amc/amc_payments_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 338 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/amc/amc_service.dart` | UI_REFERENCE_ONLY | E8 |
| 339 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/amc/amc_visits_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 340 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/approvals/approval_service.dart` | UI_REFERENCE_ONLY | E8 |
| 341 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/approvals/approvals_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 342 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/attendance/attendance_service.dart` | UI_REFERENCE_ONLY | E7 |
| 343 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/attendance/correction_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 344 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/attendance/correction_service.dart` | UI_REFERENCE_ONLY | E7 |
| 345 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/attendance/my_attendance_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 346 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/attendance/punch_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 347 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/attendance/team_attendance_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 348 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/audit/audit_screen.dart` | UI_REFERENCE_ONLY | E5 |
| 349 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/auth/change_password_screen.dart` | REJECT_SECURITY_RISK | E1 |
| 350 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/auth/forgot_password_screen.dart` | REJECT_SECURITY_RISK | E1 |
| 351 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/auth/login_screen.dart` | REJECT_SECURITY_RISK | E1 |
| 352 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/calendar/calendar_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 353 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/calendar/calendar_service.dart` | UI_REFERENCE_ONLY | E8 |
| 354 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/calendar/holidays_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 355 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/calibration/calibration_dashboard_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 356 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/calibration/calibration_service.dart` | UI_REFERENCE_ONLY | E8 |
| 357 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/dashboard/home_dashboard_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 358 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/dispatch/dispatch_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 359 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/employee/add_employee_screen.dart` | UI_REFERENCE_ONLY | E5 |
| 360 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/employee/documents_screen.dart` | UI_REFERENCE_ONLY | E5 |
| 361 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/employee/employee_master_screen.dart` | UI_REFERENCE_ONLY | E5 |
| 362 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/employee/employee_service.dart` | UI_REFERENCE_ONLY | E5 |
| 363 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/employee/import_employees_screen.dart` | UI_REFERENCE_ONLY | E5 |
| 364 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/employee/my_profile_screen.dart` | UI_REFERENCE_ONLY | E5 |
| 365 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/employee/section_form_screen.dart` | UI_REFERENCE_ONLY | E5 |
| 366 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/expense/.gitkeep` | DO_NOT_IMPORT | E2 |
| 367 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/expense/add_expense_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 368 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/expense/expense_list_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 369 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/expense/expense_service.dart` | UI_REFERENCE_ONLY | E7 |
| 370 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/grn/.gitkeep` | DO_NOT_IMPORT | E2 |
| 371 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/incentive/my_incentive_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 372 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/installation/installation_detail_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 373 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/installation/installation_list_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 374 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/installation/installation_service.dart` | UI_REFERENCE_ONLY | E8 |
| 375 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/invoice/invoice_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 376 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/kpi/kpi_ranking_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 377 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/kpi/kpi_service.dart` | UI_REFERENCE_ONLY | E7 |
| 378 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/kpi/my_kpi_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 379 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/leave/leave_request_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 380 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/leave/leave_service.dart` | UI_REFERENCE_ONLY | E7 |
| 381 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/ledgers/customer_po_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 382 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/ledgers/job_order_detail_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 383 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/ledgers/job_orders_list_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 384 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/ledgers/ledger_service.dart` | UI_REFERENCE_ONLY | E8 |
| 385 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/ledgers/machine_ledger_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 386 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/ledgers/po_entry_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 387 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/location/live_locations_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 388 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/location/location_queue.dart` | UI_REFERENCE_ONLY | E7 |
| 389 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/location/location_service.dart` | UI_REFERENCE_ONLY | E7 |
| 390 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/location/route_replay_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 391 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/machines/customer_machine_service.dart` | UI_REFERENCE_ONLY | E6 |
| 392 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/machines/customer_machines_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 393 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/machines/machine_history_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 394 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/masters/customer_master_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 395 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/masters/master_service.dart` | UI_REFERENCE_ONLY | E8 |
| 396 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/masters/vendor_master_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 397 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/mywork/my_work_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 398 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/mywork/my_work_service.dart` | UI_REFERENCE_ONLY | E8 |
| 399 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/notifications/notification_bell.dart` | UI_REFERENCE_ONLY | E9 |
| 400 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/notifications/notification_service.dart` | UI_REFERENCE_ONLY | E8 |
| 401 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/notifications/notifications_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 402 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/ot/ot_request_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 403 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/overview/compliance_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 404 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/overview/dashboard_service.dart` | UI_REFERENCE_ONLY | E8 |
| 405 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/overview/role_dashboard_screen.dart` | UI_REFERENCE_ONLY | E5 |
| 406 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/payroll/generate_salary_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 407 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/payroll/salary_slip_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 408 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production/bom_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 409 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production/material_requests_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 410 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production/production_service.dart` | UI_REFERENCE_ONLY | E6 |
| 411 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/daily_plan_detail_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 412 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/daily_plan_list_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 413 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/manager_monitor_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 414 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/manager_task_review_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 415 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/my_today_tasks_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 416 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/production_project_detail_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 417 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/production_project_service.dart` | UI_REFERENCE_ONLY | E6 |
| 418 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/production_projects_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 419 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/production_task_service.dart` | UI_REFERENCE_ONLY | E6 |
| 420 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/project_task_progress_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 421 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/purchase_request_screen.dart` | UI_REFERENCE_ONLY | E3 |
| 422 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/purchase_request_service.dart` | UI_REFERENCE_ONLY | E3 |
| 423 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/production_monitor/team_performance_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 424 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/project/.gitkeep` | DO_NOT_IMPORT | E2 |
| 425 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/project/add_project_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 426 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/project/daily_report_service.dart` | UI_REFERENCE_ONLY | E6 |
| 427 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/project/daily_work_report_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 428 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/project/project_list_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 429 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/project/project_service.dart` | UI_REFERENCE_ONLY | E6 |
| 430 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/project_perf/project_performance_screen.dart` | UI_REFERENCE_ONLY | E6 |
| 431 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/purchase/add_purchase_bill_screen.dart` | UI_REFERENCE_ONLY | E3 |
| 432 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/purchase/grn_screen.dart` | UI_REFERENCE_ONLY | E3 |
| 433 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/purchase/purchase_list_screen.dart` | UI_REFERENCE_ONLY | E3 |
| 434 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/purchase/purchase_service.dart` | UI_REFERENCE_ONLY | E3 |
| 435 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/purchase/stock_screen.dart` | UI_REFERENCE_ONLY | E3 |
| 436 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/purchase/verify_bills_screen.dart` | UI_REFERENCE_ONLY | E3 |
| 437 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/qc/qc_fat_pdi_screen.dart` | UI_REFERENCE_ONLY | E4 |
| 438 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/reminders/reminder_dashboard_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 439 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/reports/report_view_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 440 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/reports/reports_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 441 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/reports/reports_service.dart` | UI_REFERENCE_ONLY | E8 |
| 442 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/service/checklist_service.dart` | UI_REFERENCE_ONLY | E8 |
| 443 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/service/service_checklist_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 444 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/service/service_ledger_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 445 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/service/service_report_flow_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 446 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/service/service_report_service.dart` | UI_REFERENCE_ONLY | E8 |
| 447 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/service/service_service.dart` | UI_REFERENCE_ONLY | E8 |
| 448 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/service/service_ticket_detail_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 449 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/service/service_tickets_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 450 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/service_perf/service_performance_screen.dart` | UI_REFERENCE_ONLY | E8 |
| 451 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/settings/settings_screen.dart` | UI_REFERENCE_ONLY | E9 |
| 452 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/task/.gitkeep` | DO_NOT_IMPORT | E2 |
| 453 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/task/allocate_task_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 454 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/task/my_task_today_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 455 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/task/task_approval_screen.dart` | UI_REFERENCE_ONLY | E7 |
| 456 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/lib/modules/task/task_service.dart` | UI_REFERENCE_ONLY | E7 |
| 457 | `sess-emp-app-9.zip` | `sess-emp-app/mobile/pubspec.yaml` | DO_NOT_IMPORT | E2 |
| 458 | `sess-emp-app-9.zip` | `sess-emp-app/README.md` | REUSE_AS_REQUIREMENT | E12 |
| 459 | `sess-emp-app-9.zip` | `sess-emp-app/sess-app-preview.html` | UI_REFERENCE_ONLY | E9 |
