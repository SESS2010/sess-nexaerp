# SESS NexaERP — Stores + Purchase Tracking Requirements (v1, 26 Sep 2026)

**Audience:** Claude Code (backend) and the frontend developers (ILAMPARUTHI, MAGESHWARI).
**Owner / approver:** the Technical Director (TD, SESS-01). The TD answers every business rule himself (stores, purchase, accounts, service, IT).
**Status:** requirements baseline for the next development phase. Read this file completely before writing any design or code.

---

## 0. How to use this file (instructions for Claude Code)

1. **Go-live is no longer tied to a date.** The 8/10 Oct plan is cancelled. The ERP starts only when Stores + Purchase are **complete, correct, paper-free, fully tracked, with dashboards and notifications**. Timeline may extend.
2. **Two reference apps** built by SESS staff on Google Sheets + Apps Script are the TD's model of how tracking, follow-up, reminders, roles and reports must work:
   - **DC Tracker Rev 2** (material delivery challans) — §2.
   - **SESS Service Engineer Hub V2.6** (service tickets, daily plan, reports, expenses, revenue, performance) — §3.
   Source files (read them; they are the ground truth for behaviour):
   - `C:\Users\User\Documents\Codex\audit\dc-tracker\` (README.md, Code.gs, Jobs.gs, Reports.gs, Print.gs, App.html, INSTALL-GUIDE.html, DC-Tracker-Database-Rev1.xlsx, DC-Tracker-Import-Template.xlsx)
   - `C:\Users\User\Documents\Codex\audit\service-hub\` (00_READ_ME_FIRST.txt, Code.gs, Index.html, COMPLETE_GUIDE_V2.6.md, PAGE_ACCESS.md, **DATA_MODEL.md** (359 columns), CHANGE_HISTORY.md, backend xlsx)
3. **Do not port Google-Sheets mechanics** (sheet rows, Apps Script triggers, Drive folders, PIN logins). Port the **behaviour, rules, statuses, reports and notifications** into the NexaERP architecture (.NET API + PostgreSQL + Keycloak + React), keeping all existing ERP conventions (two companies, scopes, audit trail, UTC storage / IST display, idempotency keys, maker/checker, full acceptance cycles).
4. **Never commit customer/party data** (the `.xlsx` databases, backups) to the repository or GitHub. Code, README and guides may be committed as `docs/reference/…` for traceability.
5. **Deliver first, build after sign-off** (§9): SRS → TD sign-off → sprints. Sprint 1 (tracking backbone + notification engine core) may start before sign-off.

---

## 1. Business context

- **Company:** Sri Eswari Scientific Solution — two entities:
  | Code | Legal name | GSTIN | State |
  |---|---|---|---|
  | SESS_PVT_LTD ("SPVT") | SRI EASWARI SCIENTIFIC SOLUTION PVT LTD | 33ABACS5491H1ZA | Tamil Nadu (33) |
  | SESS_PROPRIETORSHIP ("SESS") | SRI EASWARI SCIENTIFIC SOLUTION | 33APRPA5532K1ZU | Tamil Nadu (33) |
  Other SPVT details from DC Tracker settings: CIN U24304TN2018PTC123559, PAN ABACS5491H, address 2/298 A N E Garden Perumal Kovil Street, Srinivasapuram Paraniputhur, Iyyapanthangal, Chennai, e-mail info@sess.co.in. **Use as R10 seed data only after the TD confirms.**
- **Business:** manufacturer of environmental / thermal / cascade-refrigeration test chambers (−70 °C to +180 °C) for defence, aerospace, EMS, rail, EV battery. Job-order based production; service, AMC/CAMC/CMC and warranty at customer sites.
- **Accounting** stays in Tally for now (ledgers, GST returns). The ERP owns operations, tracking and payables visibility (§4 T3–T5 define the boundary).
- **Store software BroPOS** runs until the ERP goes live.

---

## 2. Reference app A — DC Tracker Rev 2 (material delivery challans)

### 2.1 Purpose
Every material that leaves the premises on a delivery challan is tracked until it is **returned, consumed, written off, billed and documented**. Built for SPVT; works for both companies.

### 2.2 DC types and closing rules
| Type | Number format | Due date | Stays OPEN until |
|---|---|---|---|
| **RDC** — Returnable | `RDC/<BRANCH>/<nnn>/<FY>` e.g. `RDC/SPVT/220/26-27` | **Compulsory**, default 15 days | Every line returned / consumed / written off **and** signed hardcopy received by store; if chargeable, invoice entered |
| **NRDC** — Non-returnable | `NRDC/<BRANCH>/<nnn>/<FY>` | — | Accounts enters **invoice number** or marks **not billable** with reason; reminder after 3 days |
| **WDC** — Warranty | `WDC/<BRANCH>/<nnn>/<FY>` | **Compulsory**, default 30 days | Same as RDC + vendor/OEM, claim/RMA no, serial no, fault, outcome (repaired / replaced / credit note) |

- Store can **close manually** with remarks (e.g. lost material charged). Admin can **cancel** or **reopen** with reason. **DCs are never deleted.**
- **Manual DC number** allowed (Tally-made DC); duplicates blocked. Separate counter per type per FY per company.
- Current counters: RDC 26-27 = 219, NRDC 26-27 = 67, WDC = 0. Existing data: **303 DCs, 832 lines, 145 parties** (import required, §8).

### 2.3 Data (from the database workbook)
- **DC (header):** DCID, DCNo, DCType, DCDate, FY, PartyName/Address/GSTIN/State, Buyer (consignee) Name/Address/GSTIN/State, Purpose, PurposeCategory, PreparedBy, Responsible (employee), DispatchedThrough, VehicleNo, EWayBillNo, Destination, ModeOfPayment, RefNo, OtherRef, BuyerOrderNo/Date, DispatchDocNo, TermsOfDelivery, ApproxValue, DueDate, OriginalDueDate, Status, HardcopyStatus, HardcopySubmittedOn/By, HardcopyReceivedOn/By, SignedCopy, GateEntryNo/Date, EndUserName, Chargeable, BillingStatus, InvoiceNo/Date, Warranty (Direction, Vendor, ClaimNo, SerialNo, FaultDescription, Outcome), ClosedOn/By, CloseRemarks, Remarks, document links, Source, Created/Updated At/By.
- **DCItems (lines):** LineNo, Description, SubDescription, HSN, Qty, Unit, **Category** (Consumable / Non-consumable / Tool & equipment / Spare part / Job-work material), SerialNo, Value, ReturnedQty, ConsumedQty, LostQty.
- **Movements (returns / consumption submissions):** MoveId, **AckNo**, DC + line, MoveDate, Returned/Consumed/Lost qty, Condition, SubmittedBy/At, Remarks, **VerifyStatus** (PENDING/ACCEPTED/REJECTED), VerifiedBy/At, VerifyRemarks.
- **Documents:** DocType (signed DC, gate entry, end-user ack, DC PDF…), file, uploader, time.
- **Extensions:** OldDueDate, NewDueDate, Reason, RequestedBy/At, Status (PENDING/APPROVED/REJECTED), DecidedBy/At, remarks.
- **Parties** (Name, 3 address lines, GSTIN, State name + code, contact, phone, e-mail, PartyType, Active), **Items** (Description, HSN, Unit, Category), **Users**, **AuditLog** (old → new values), **Settings**, **Counters**.
- **Purposes:** SALES, SERVICE, JOB WORK, RENTAL, DEMO, CALIBRATION, REPAIR, SITE WORK, REWORK, TESTING, PROJECT, REPLACEMENT, WARRANTY, OTHER. **Units:** NOS, MTR, LTR, KG, SET, PAIR, BOX, ROLL, PKT, LOT.

### 2.4 Workflows
- **Create DC** (store): party/item masters auto-fill address, GSTIN, HSN, unit, category; responsible employee; purpose; due date; vehicle, e-way bill (needed above ₹50,000 even on a DC); chargeable flag.
- **Engineer portal ("My DCs", mobile):** upload signed copy / proof (photo or PDF; gate entry no/date; end-user name); submit **returns / consumption / lost** per line (lost requires remarks) → **acknowledgement number + timestamp**, printable, e-mailed; "I handed over hardcopy"; request **extension** (new date + reason). The engineer's submission date is preserved regardless of store action (proof of timely submission).
- **Store desk:** verify/accept/reject returns (with reason), confirm hardcopy, decide extensions, list DCs where material is back but hardcopy isn't.
- **Billing (accounts):** every DC awaiting invoice; enter invoice inline or mark not billable with reason.
- **Close / cancel / reopen** with reasons; everything audited.
- **Print:** Tally "Delivery Note" layout; **Original / Duplicate / Triplicate** copies; **editable Terms & Conditions per DC type** (Rule 55 CGST Rules / Sec. 143); RDC declaration text; per-company header.

### 2.5 Alerts, dashboard, reports
- **Flags:** OVERDUE, DUE_SOON (2 days before), NO_DUE_DATE, NO_SIGNED_COPY, HC_TO_CONFIRM, VERIFY_PENDING, BILL_PENDING, BILL_OVERDUE, **GST_JOBWORK** (job-work material out close to 365 days; after 1 year treated as supply; capital goods 3 years).
- **Escalation:** overdue beyond 7 days copies the employee's reporting manager.
- **Daily 09:00 e-mails:** each engineer (overdue, due soon, hardcopy pending, signed copy missing); store (verifications, hardcopies, extensions, overdue); accounts (awaiting invoice); purchase (overdue warranty / job-work); management summary with top overdue employees. Master switch + per-event switches; "send now".
- **Dashboard:** live counts (open, overdue, due soon, awaiting invoice, hardcopy pending, returns to verify, signed copy missing, no due date, GST job-work alerts); period figures (issued, closed, closure %, on-time %); charts (status by type, overdue ageing, monthly trend, consumable vs non-consumable movement); **employee accountability table**; every number clickable.
- **ISO audit report:** §7.5 documented information, §8.5.2 traceability, §8.5.3 property of customers / external providers — summary by type (closure %, on-time return %, average days to close and to bill, hardcopy %, signed-copy %), employee-wise compliance %, material category movement, **non-conformity list for CAPA**, prepared/reviewed/approved signature lines, CSV/export.
- **Audit log:** every create, edit (old → new), upload, verification, extension, closure.

### 2.6 Known weaknesses (do NOT copy)
Default passwords (`<REDACTED>`, `<REDACTED>`), web app open to "Anyone", scans shared by link (`FILE_LINK_SHARING = YES`), own login system, Gmail sending limits, data in a Google account. The ERP uses Keycloak, ERP permissions/scopes, private document storage and a server-side mailer.

---

## 3. Reference app B — SESS Service Engineer Hub V2.6

### 3.1 Purpose
Daily work management for service, production and office teams: complaints/tickets, daily plan, morning/evening reports with timeliness, expenses, revenue, customer feedback, performance, year-end archive. **Read `DATA_MODEL.md` (full column list) and `PAGE_ACCESS.md` (role × page matrix).**

### 3.2 Entities (22 tables)
Complaints (tickets `SESS/SRV|PRD|TSK/nnnn/FY`, work stream SERVICE / PRODUCTION / TASK, coverage Warranty / OOW / AMC / CAMC / CMC / Other brand / Factory / Internal, status, planned closure, replan count, original planned), Assignments (LEAD/SUPPORT, response ACCEPTED/CLARIFICATION/UNABLE, **Materials to take**), Morning_Reports, Evening_Reports (completed/partial/not, **Parts_Used**, next plan, pending reason, customer ack, timeliness ON_TIME/LATE/VERY_LATE/PROXY), Daily_Tasks, **Expenses** (transport, two-wheeler km/amount, boarding, lodging, food, other, **Material_Cost**, receipt, verify status), Feedback (rating), **Revenue_Ledger** + Revenue_Attribution, **Customers**, **Machines** (brand, type, model, serial, capacity, temperature/humidity range, controller, **refrigerant**, install/commissioning, warranty start/end, coverage/contract, service frequency, next service date), Employees, Roles (Role Master), Companies, Options, **Status_History**, **Audit_Log** (before/after JSON), Attachments, Projects (order → dispatch → installation stages), Archive_Log, Registrations, Settings.

### 3.3 Rules worth keeping
- Status history for every ticket; complaint → WORK_DONE automatically when all assignments complete; manager verifies and closes; re-plan with reason counted.
- Report timeliness (due 09:30 / 19:30; LATE within 24 h; VERY_LATE up to 10 days; PROXY when entered by coordinator).
- **Cannot verify own expense**; revenue entered by one person, verified by another.
- Delete only when unreferenced; otherwise deactivate; every delete audited with full record.
- Performance score (weights configurable): on-time completion, report timeliness, acceptance, rating, revenue vs expense, no missed reports, daily task completion.
- Role Master decides portal and page permissions; AUDITOR role read-only across everything.
- Year-end archive with safety copy.

### 3.4 Integration points with Stores + Purchase (in scope now)
| Service Hub item | ERP Stores/Purchase requirement |
|---|---|
| Assignment "Materials to take", "Request from store" | Engineer raises a **Material Issue Request** linked to the ticket; store issues on **RDC** (returnable tools/spares) or issue-to-job |
| Evening report "Parts_Used" | **Consumption against ticket / machine** → stock issue confirmed; returns of unused parts |
| Expenses "Material_Cost" | Must come from ERP issue value, not typed |
| Machines (serial, refrigerant, warranty, contract) | Link to ERP **machine dossier** and **serial-number traceability**; **WDC** warranty claims reference the machine |
| Coverage AMC/CAMC/Warranty | Decides **chargeable vs free** material on RDC/NRDC and billing follow-up |
| Revenue_Ledger (invoice against ticket) | NRDC / chargeable RDC billing status |
| Customers | Reconcile with ERP customer master (one master) |

A full Service module port (tickets, daily plan, performance) is **phase 2**; design the data model so it can be added without rework.

---

## 4. Requirements for the ERP (T0–T11)

**T0 Tracking backbone (every document type):** status history timeline (from → to, who, when, channel, remark), current owner ("pending with"), age in days, SLA per stage, follow-up log (call / e-mail / note / WhatsApp text), next follow-up date, overdue flags, one "tracking view" per document and a generic "My pending items" list per user.

**T1 PO tracking:** requisition (any employee via their portal; dept-head first approval within slab; then TD / MD limits as configured) → RFQ (**minimum 3 quotes**; single source allowed with reason) → quotes (incl. **vendor portal**) → comparison (**L1 default; L2 or other selectable with reason** based on qualification) → PO (**up to ₹5,000 Purchase Manager**, higher slabs as configured) → vendor acknowledgement / promise date → follow-ups → gate entry → GRN (partial / excess / short) → QC → close / short-close / cancel. PO amendment with **revision history**. **Rate contracts** designed now, used later. PO always generated and sent from the ERP.

**T2 Material DC tracking:** full parity with §2 (RDC / NRDC / WDC, all workflows, flags, reminders, prints, ISO report), **stock linked**: material leaving on a DC reduces the source location and is tracked in an "out on DC" state per line until returned / consumed / lost / billed; returns re-enter stock after store verification. Separate numbering series per type/company/FY; machine DC (existing ERP feature) keeps its own series. Import of the 303 existing DCs.

**T3 Vendor bill tracking:** bill received → 3-way match PO / GRN / bill → price / qty variance with approval → due date → paid; lists: GRNs without bills, bills without GRN, disputed bills.

**T4 Vendor payment + outstanding:** outstanding by vendor, aging buckets, due dates, **MSME 45-day** watch, TDS (194C / 194Q), advances and their adjustment, payment advice to vendor. Tally remains the ledger; define the sync/export boundary.

**T5 PDC tracking (post-dated cheques to suppliers):** register (vendor, bank, cheque no, amount, issue date, cheque date, linked bills), status ISSUED / DUE / PRESENTED / CLEARED / BOUNCED / CANCELLED, reminders before cheque date, daily "cheques due" list for bank-balance planning, bounce handling.

**T6 Stores tracking + consumable control:** issues/returns per job, ticket, machine and employee; **standard (BOM norm) vs actual consumption**, variance and wastage alerts; returnable tools out; reorder / min-max alerts; non-moving and aging stock; refrigerant by weight and cylinder deposit/return; serial-number traceability (component → GRN → job → machine → warranty).

**T7 Notification engine:** e-mail first (company SMTP, templates, schedules, event triggers), WhatsApp hook designed for later. Daily 09:00 digests per role (employees: their pending items; store; purchase; accounts; management summary with top overdue). Escalation to the reporting manager after N days. **Vendor e-mails:** PO issued, delivery reminder, overdue, rejection / RTV, payment advice / PDC details. Sent-mail log, bounce log, per-role and per-event switches, test mailbox before go-live.

**T8 Dashboards:** Purchase (pipeline by stage, overdue POs, vendor performance, spend **excl. recoverable GST**, approvals pending), Stores (stock value, movements, aging, non-moving, reorder, consumables per job/employee, DC out/overdue, QC pending), DC (as §2.5), Payables/PDC, Management; **every number drills down to its list**; employee accountability table; role access as decided (Stores Manager → Stores; QC → QC part; Accounts → Purchase + Payables; TD/MD → all, both companies).

**T9 ISO 9001 records inside the ERP (no paper):** 7.1.5 calibration of measuring equipment, 7.5 documented information (retention, revision, access, no delete), 8.4 supplier evaluation / selection / monitoring / **re-evaluation** and vendor rating, 8.5.2 identification & traceability, 8.5.3 customer / supplier property, 8.7 nonconforming output + concessions, 9.1 supplier performance, 10.2 NC + CAPA / SCAR; auditor reports with prepared / reviewed / approved lines; read-only AUDITOR role. Every register of `manual-registers-DRAFT.xlsx` (27 registers: returnable / non-returnable gate pass, job work ITC-04, RTV + debit note, refrigerant cylinders, bin transfer, e-way bill, expiry, free-issue, calibration, CAPA/SCAR, document control, RCM, MSME 45-day, TDS, GST purchase, approved vendors, PO short-close, service purchases, excess receipt, bill variance, inter-company, scrap sale, service/AMC spares issue, serial numbers, vendor evaluation, stock count sheet) becomes an ERP screen or report.

**T10 Vendor portal:** vendors submit quotes, acknowledge POs, give delivery promise dates, see payment status. Separate identity realm/client, strict data isolation per vendor. May be a later sprint.

**T11 Validation catalogue:** every field and rule, self-approval / maker-checker blocks (already: R3 bill, G1 QC-by-GRN-operator), duplicates, GST state logic (R2), qty vs balance (partial issues), mandatory reasons, date sanity — each with an automated test.

---

## 5. Cross-cutting rules (apply to everything)
- Two companies, company scope on every record; inter-company movement needs a GST document.
- UTC storage, IST display; FY numbering (Apr–Mar) with rollover.
- Maker ≠ checker everywhere an approval exists; reasons mandatory for reject / cancel / override / L2 selection / single source / lost material.
- Soft delete only (deactivate); full audit trail (who, what, when, before/after).
- Attachments stored privately, accessible only through ERP permissions.
- Every list: filter, column chooser, export (CSV/Excel), print.
- Mobile-friendly screens for engineers and store (phone camera upload).
- Idempotent writes; plain-word confirmations before irreversible actions.

## 6. Roles (merge of both apps)
TD, MD, Purchase Manager, Purchase, Store Manager, Store, QC Manager, QC, Accounts Manager, Accounts, Service Manager, Production Manager, Assistant Managers, AMC/CAMC Head, Service Coordinator, Engineer, Technician, HR, IT Manager, IT Executive, Admin Staff, **Auditor (read-only)**, **Vendor (portal)**. Map to existing ERP roles/scopes; list gaps in the SRS.

## 7. Non-functional
Security via Keycloak (no local passwords), private storage, server-side mailer with quotas and retry, backups included in the existing backup design, performance for ~50 concurrent users on the office LAN, printable A4 outputs per company.

## 8. Data migration
- DC Tracker: 303 DCs, 832 lines, 145 parties, counters (RDC 219, NRDC 67) — import with validation report; keep original numbers.
- Service Hub: customers, machines (for serial / warranty / contract links) — reconcile with ERP masters; tickets later (phase 2).
- Cut-over rules already decided (open POs re-entered, pending GRNs finished in BroPOS, advances/unpaid in Tally) remain until the TD changes them.

## 9. Deliverables and process
- **D1 SRS** `C:\Users\User\Documents\Codex\audit\SRS-tracking-v1.md`: for every T0–T11 item — PRESENT / PARTIAL / MISSING today (with file/API refs), screens, fields, rules, reports, notifications, roles, data model, APIs, size in days (backend + frontend).
- **D2 Sprint plan** (1-week sprints). Owners: Claude Code = backend, APIs, migrations, tests; ILAMPARUTHI + MAGESHWARI = frontend, split so they never edit the same files. Weekly demo to the TD. Realistic go-live estimate.
- **D3 Frontend contract per sprint** in `audit\contracts\`.
- **D4 Open questions for the TD** — max 40, only what this file, the repo and the two apps do not answer.
- **D5 Reference commit** after running cycles: `docs/reference/dc-tracker/` and `docs/reference/service-hub/` (code + guides only; no xlsx, no passwords, no customer data), and this file as `docs/requirements/SESS-NexaERP-Tracking-Requirements-v1.md`.
- **Build rules:** never push (the TD pushes); full acceptance cycle per code item; one item per commit; park after one failed re-run; never touch the server; write outside the repo while a cycle runs.
- **Sprint 1 (may start now):** T0 tracking backbone + T7 notification engine core (test mailbox only, no vendor e-mails). Everything else after TD sign-off of D1.
