# SESS NexaERP — Master Requirements for Claude Code (v2, 27 Sep 2026)

**Supersedes** `SESS-NexaERP-Tracking-Requirements-v1.md` (all of v1 is included here).
**Audience:** Claude Code (backend, TD PC) and the frontend developers (ILAMPARUTHI = Track A, MAGESHWARI = Track B, others as assigned).
**Owner / approver:** the Technical Director (TD, SESS-01). The TD answers every business rule himself (stores, purchase, accounts, service, production, sales, IT). Do not wait for department heads.

---

## 0. How to use this file (instructions for Claude Code)

1. **Goal:** NexaERP becomes the **single system for every SESS employee**. The TD and his team built working Google-Sheets + Apps-Script apps for each department. Each one is a **proven reference** of how that department wants to work. Port their **behaviour, rules, statuses, numbering, approvals, reports, dashboards, reminders and notifications** into NexaERP (.NET API + PostgreSQL + Keycloak + React), **not** their Google mechanics (sheet rows, triggers, Drive links, OTP/PIN logins, Gmail quotas).
2. **Go-live is not tied to a date.** Completeness, correctness and paper-free operation come first. Tracks release one after another (§4).
3. **Reference apps and where their files are on the TD PC** (read everything; the code and guides are the ground truth):
   | # | Reference app | Folder on TD PC | Track |
   |---|---|---|---|
   | R-A | **DC Tracker Rev 2** (material delivery challans) | `C:\Users\User\Documents\Codex\audit\dc-tracker\` | A |
   | R-B | **SESS Service Engineer Hub V2.6** (service + production activity) | `C:\Users\User\Documents\Codex\audit\service-hub\` | B |
   | R-C | **SESS Combined Expense ERP V4.1** (employee expenses, travel KM, advances, petty cash, cash-flow planner) | `C:\Users\User\Documents\Codex\audit\expense-erp\` | C |
   | R-D | **SESS Sales CRM V3.2** (enquiries, offers, revisions, pipeline, follow-ups, auto reminders) | `C:\Users\User\Documents\Codex\audit\sales-crm\` | D |
   | — | TD's earlier requirement documents (Purchase/Store complete requirement, Stores full schema, Store activity roadmap, Mobile service module, Backend atlas, CRM–ERP integration blueprint V2) | `C:\Users\User\Documents\Codex\audit\requirements-inputs\` | all |
   | — | Manual registers (27) | `C:\Users\User\Documents\golive-data\manual-registers-DRAFT.xlsx` | A (ISO) |
4. **More reference apps will come.** For each new one the TD places its files in `audit\<app-name>\` and a new section is added to this file using the template in §9. Design the platform so a new department module plugs into the shared foundation (§3) without rework.
5. **Never commit** customer / vendor / employee data (`.xlsx` databases, backups, register workbooks, e-mail lists, GST certificates) to the repository or GitHub. Code, READMEs and guides may be committed under `docs/reference/<app>/`; this file under `docs/requirements/`.
6. **Process:** SRS per track → TD sign-off → sprints (§8). The shared foundation Sprint 1 may start before sign-off.

---

## 1. Business context

- **Company:** Sri Eswari Scientific Solution — two entities:
  | Code | Legal name | GSTIN | State |
  |---|---|---|---|
  | SESS_PVT_LTD ("SPVT") | SRI EASWARI SCIENTIFIC SOLUTION PVT LTD | 33ABACS5491H1ZA | Tamil Nadu (33) |
  | SESS_PROPRIETORSHIP ("SESS") | SRI EASWARI SCIENTIFIC SOLUTION | 33APRPA5532K1ZU | Tamil Nadu (33) |
  SPVT: CIN U24304TN2018PTC123559, PAN ABACS5491H, 2/298 A N E Garden Perumal Kovil Street, Srinivasapuram Paraniputhur, Iyyapanthangal, Chennai, info@sess.co.in. GST certificates of both entities and the logo are in `audit\sales-crm\` (reference_documents). **Verify against the certificates; use as R10 seed data only after the TD confirms.**
- **Business:** manufacturer of environmental / thermal / cascade-refrigeration test chambers (−70 °C to +180 °C) for defence, aerospace, EMS, rail, EV battery. Job-order production; service, AMC/CAMC/CMC and warranty at customer sites; ~50–60 employees, many on phones in the field.
- **Accounting ledgers and GST returns stay in Tally** for now; NexaERP owns operations, tracking, approvals, payables/receivables visibility and cash planning. Every money module must state its Tally boundary (export/sync or manual).
- BroPOS (store) runs until Track A goes live; each Google app keeps running until its ERP module goes live, then its data is imported.

---

## 2. Reference apps

## 2.A Reference app A — DC Tracker Rev 2 (material delivery challans)

### 2.A.1 Purpose
Every material that leaves the premises on a delivery challan is tracked until it is **returned, consumed, written off, billed and documented**. Built for SPVT; works for both companies.

### 2.A.2 DC types and closing rules
| Type | Number format | Due date | Stays OPEN until |
|---|---|---|---|
| **RDC** — Returnable | `RDC/<BRANCH>/<nnn>/<FY>` e.g. `RDC/SPVT/220/26-27` | **Compulsory**, default 15 days | Every line returned / consumed / written off **and** signed hardcopy received by store; if chargeable, invoice entered |
| **NRDC** — Non-returnable | `NRDC/<BRANCH>/<nnn>/<FY>` | — | Accounts enters **invoice number** or marks **not billable** with reason; reminder after 3 days |
| **WDC** — Warranty | `WDC/<BRANCH>/<nnn>/<FY>` | **Compulsory**, default 30 days | Same as RDC + vendor/OEM, claim/RMA no, serial no, fault, outcome (repaired / replaced / credit note) |

- Store can **close manually** with remarks (e.g. lost material charged). Admin can **cancel** or **reopen** with reason. **DCs are never deleted.**
- **Manual DC number** allowed (Tally-made DC); duplicates blocked. Separate counter per type per FY per company.
- Current counters: RDC 26-27 = 219, NRDC 26-27 = 67, WDC = 0. Existing data: **303 DCs, 832 lines, 145 parties** (import required, §8).

### 2.A.3 Data (from the database workbook)
- **DC (header):** DCID, DCNo, DCType, DCDate, FY, PartyName/Address/GSTIN/State, Buyer (consignee) Name/Address/GSTIN/State, Purpose, PurposeCategory, PreparedBy, Responsible (employee), DispatchedThrough, VehicleNo, EWayBillNo, Destination, ModeOfPayment, RefNo, OtherRef, BuyerOrderNo/Date, DispatchDocNo, TermsOfDelivery, ApproxValue, DueDate, OriginalDueDate, Status, HardcopyStatus, HardcopySubmittedOn/By, HardcopyReceivedOn/By, SignedCopy, GateEntryNo/Date, EndUserName, Chargeable, BillingStatus, InvoiceNo/Date, Warranty (Direction, Vendor, ClaimNo, SerialNo, FaultDescription, Outcome), ClosedOn/By, CloseRemarks, Remarks, document links, Source, Created/Updated At/By.
- **DCItems (lines):** LineNo, Description, SubDescription, HSN, Qty, Unit, **Category** (Consumable / Non-consumable / Tool & equipment / Spare part / Job-work material), SerialNo, Value, ReturnedQty, ConsumedQty, LostQty.
- **Movements (returns / consumption submissions):** MoveId, **AckNo**, DC + line, MoveDate, Returned/Consumed/Lost qty, Condition, SubmittedBy/At, Remarks, **VerifyStatus** (PENDING/ACCEPTED/REJECTED), VerifiedBy/At, VerifyRemarks.
- **Documents:** DocType (signed DC, gate entry, end-user ack, DC PDF…), file, uploader, time.
- **Extensions:** OldDueDate, NewDueDate, Reason, RequestedBy/At, Status (PENDING/APPROVED/REJECTED), DecidedBy/At, remarks.
- **Parties** (Name, 3 address lines, GSTIN, State name + code, contact, phone, e-mail, PartyType, Active), **Items** (Description, HSN, Unit, Category), **Users**, **AuditLog** (old → new values), **Settings**, **Counters**.
- **Purposes:** SALES, SERVICE, JOB WORK, RENTAL, DEMO, CALIBRATION, REPAIR, SITE WORK, REWORK, TESTING, PROJECT, REPLACEMENT, WARRANTY, OTHER. **Units:** NOS, MTR, LTR, KG, SET, PAIR, BOX, ROLL, PKT, LOT.

### 2.A.4 Workflows
- **Create DC** (store): party/item masters auto-fill address, GSTIN, HSN, unit, category; responsible employee; purpose; due date; vehicle, e-way bill (needed above ₹50,000 even on a DC); chargeable flag.
- **Engineer portal ("My DCs", mobile):** upload signed copy / proof (photo or PDF; gate entry no/date; end-user name); submit **returns / consumption / lost** per line (lost requires remarks) → **acknowledgement number + timestamp**, printable, e-mailed; "I handed over hardcopy"; request **extension** (new date + reason). The engineer's submission date is preserved regardless of store action (proof of timely submission).
- **Store desk:** verify/accept/reject returns (with reason), confirm hardcopy, decide extensions, list DCs where material is back but hardcopy isn't.
- **Billing (accounts):** every DC awaiting invoice; enter invoice inline or mark not billable with reason.
- **Close / cancel / reopen** with reasons; everything audited.
- **Print:** Tally "Delivery Note" layout; **Original / Duplicate / Triplicate** copies; **editable Terms & Conditions per DC type** (Rule 55 CGST Rules / Sec. 143); RDC declaration text; per-company header.

### 2.A.5 Alerts, dashboard, reports
- **Flags:** OVERDUE, DUE_SOON (2 days before), NO_DUE_DATE, NO_SIGNED_COPY, HC_TO_CONFIRM, VERIFY_PENDING, BILL_PENDING, BILL_OVERDUE, **GST_JOBWORK** (job-work material out close to 365 days; after 1 year treated as supply; capital goods 3 years).
- **Escalation:** overdue beyond 7 days copies the employee's reporting manager.
- **Daily 09:00 e-mails:** each engineer (overdue, due soon, hardcopy pending, signed copy missing); store (verifications, hardcopies, extensions, overdue); accounts (awaiting invoice); purchase (overdue warranty / job-work); management summary with top overdue employees. Master switch + per-event switches; "send now".
- **Dashboard:** live counts (open, overdue, due soon, awaiting invoice, hardcopy pending, returns to verify, signed copy missing, no due date, GST job-work alerts); period figures (issued, closed, closure %, on-time %); charts (status by type, overdue ageing, monthly trend, consumable vs non-consumable movement); **employee accountability table**; every number clickable.
- **ISO audit report:** §7.5 documented information, §8.5.2 traceability, §8.5.3 property of customers / external providers — summary by type (closure %, on-time return %, average days to close and to bill, hardcopy %, signed-copy %), employee-wise compliance %, material category movement, **non-conformity list for CAPA**, prepared/reviewed/approved signature lines, CSV/export.
- **Audit log:** every create, edit (old → new), upload, verification, extension, closure.

### 2.A.6 Known weaknesses (do NOT copy)
Default passwords (`<REDACTED>`, `<REDACTED>`), web app open to "Anyone", scans shared by link (`FILE_LINK_SHARING = YES`), own login system, Gmail sending limits, data in a Google account. The ERP uses Keycloak, ERP permissions/scopes, private document storage and a server-side mailer.

---

## 2.B Reference app B — SESS Service Engineer Hub V2.6

### 2.B.1 Purpose
Daily work management for service, production and office teams: complaints/tickets, daily plan, morning/evening reports with timeliness, expenses, revenue, customer feedback, performance, year-end archive. **Read `DATA_MODEL.md` (full column list) and `PAGE_ACCESS.md` (role × page matrix).**

### 2.B.2 Entities (22 tables)
Complaints (tickets `SESS/SRV|PRD|TSK/nnnn/FY`, work stream SERVICE / PRODUCTION / TASK, coverage Warranty / OOW / AMC / CAMC / CMC / Other brand / Factory / Internal, status, planned closure, replan count, original planned), Assignments (LEAD/SUPPORT, response ACCEPTED/CLARIFICATION/UNABLE, **Materials to take**), Morning_Reports, Evening_Reports (completed/partial/not, **Parts_Used**, next plan, pending reason, customer ack, timeliness ON_TIME/LATE/VERY_LATE/PROXY), Daily_Tasks, **Expenses** (transport, two-wheeler km/amount, boarding, lodging, food, other, **Material_Cost**, receipt, verify status), Feedback (rating), **Revenue_Ledger** + Revenue_Attribution, **Customers**, **Machines** (brand, type, model, serial, capacity, temperature/humidity range, controller, **refrigerant**, install/commissioning, warranty start/end, coverage/contract, service frequency, next service date), Employees, Roles (Role Master), Companies, Options, **Status_History**, **Audit_Log** (before/after JSON), Attachments, Projects (order → dispatch → installation stages), Archive_Log, Registrations, Settings.

### 2.B.3 Rules worth keeping
- Status history for every ticket; complaint → WORK_DONE automatically when all assignments complete; manager verifies and closes; re-plan with reason counted.
- Report timeliness (due 09:30 / 19:30; LATE within 24 h; VERY_LATE up to 10 days; PROXY when entered by coordinator).
- **Cannot verify own expense**; revenue entered by one person, verified by another.
- Delete only when unreferenced; otherwise deactivate; every delete audited with full record.
- Performance score (weights configurable): on-time completion, report timeliness, acceptance, rating, revenue vs expense, no missed reports, daily task completion.
- Role Master decides portal and page permissions; AUDITOR role read-only across everything.
- Year-end archive with safety copy.

### 2.B.4 Integration points with Stores + Purchase
| Service Hub item | ERP Stores/Purchase requirement |
|---|---|
| Assignment "Materials to take", "Request from store" | Engineer raises a **Material Issue Request** linked to the ticket; store issues on **RDC** (returnable tools/spares) or issue-to-job |
| Evening report "Parts_Used" | **Consumption against ticket / machine** → stock issue confirmed; returns of unused parts |
| Expenses "Material_Cost" | Must come from ERP issue value, not typed |
| Machines (serial, refrigerant, warranty, contract) | Link to ERP **machine dossier** and **serial-number traceability**; **WDC** warranty claims reference the machine |
| Coverage AMC/CAMC/Warranty | Decides **chargeable vs free** material on RDC/NRDC and billing follow-up |
| Revenue_Ledger (invoice against ticket) | NRDC / chargeable RDC billing status |
| Customers | Reconcile with ERP customer master (one master) |

The full Service + Production port is Track B (§5.2).

---


## 2.C Reference app C — SESS Combined Expense ERP V4.1

### Purpose
Employee expense and travel claims with policy control, advances, settlement ledger, petty cash, and a company cash-flow planner. Roles: EMPLOYEE (own records only), ACCOUNTS (verify + recommend, cannot final-approve), MD (final approval only after Accounts), ADMIN.

### Data (sheets → future tables)
- **Travel Claims:** Claim ID, date, employee, vehicle no, start/end odometer, start / up to 4 stops / end location (**Google Place IDs**), route points + segments JSON, **Google KM** (server-recomputed), employee claimed KM, odometer KM, variance KM/%, deviation status, excess-KM reason, rate/KM, claimed ₹, Accounts recommended KM/₹, **MD approved KM/₹ (final = MD KM × rate)**, receipt URL, map route, status.
- **Expense Register:** Expense ID, date, company, employee, category / subcategory, vendor/payee, description, claimed / accepted / rejected amount, payment mode, GST included + amount, cost centre, project/customer, invoice no/date, receipt/attachments (multiple), expense type, employee grade, people count, GST bill available, ticket type, **policy limit, policy status, policy exception reason**, cash account, approver.
- **Employee Advances**, **Returns & Payments** (employee returns / paid to employee / adjustment), **Employee Ledger** (opening, approved expense, advance given, returns, paid, adjustment, **net balance, settlement status**).
- **Expense Policy** (policy, applies to, limit ₹, evidence required, action), **Daily Policy Usage** (per employee/day/type aggregate).
- **Cash Flow Obligations** (month, company, head: GST, PF, ESI, rent, EB, insurance, salary, loan, TDS, vendor payments; due date, planned ₹, status, paid date, reference, attachment), **Fixed Commitments** (monthly/annual estimate, frequency, due day, payee).
- **Project Receivables** (customer/project, scope, company, total incl. GST, advance received, balance, terms, expected date, **BG amount**, status).
- Location Master, Employee Master (grade, approver e-mail), Vendor Master, Role Master, Login Audit, **Approval Audit** (old → new status, amount, remarks, by, at), Archive Index, Config.

### Rules to keep
- **Route KM control:** locations must be chosen from map suggestions; the server recomputes every leg and the total; outside **±10 %** of map KM, or odometer mismatch > 1 KM, needs a detailed reason; Accounts recommendation defaults to the **lower of employee KM and map KM**; MD may approve only up to the employee claim, and outside ±10 % needs remarks; the employee's claimed value is never overwritten.
- **Allowances:** food per day Standard ₹250 / Senior ₹300 / Superior ₹350 (by employee grade); lodging single ₹800, double ₹1,500 (exactly 2 people); bills/tickets mandatory for food, lodging, bus/train, petty purchase; multiple evidence files.
- **Petty cash:** non-GST ≤ ₹500 per employee per day; above ₹500 needs GST bill + invoice no; **₹5,000 or more is blocked → must go through Purchase / PO (Track A)**.
- Two-stage approval: Accounts verify → MD approve; reject / clarification with mandatory remarks at the current stage; audit row for every change; Accounts cannot call MD approval.
- Dashboards: map KM vs claimed vs approved KM, exceptions, KM saved, petty cash, policy exceptions, missing evidence, planned vs paid cash, **funding gap**, unpaid obligations, monthly cash-flow chart; filters 1/3/6/12 months, FY, employee, company.
- Volume: ~60 employees × 20 lines/day ≈ 438,000 rows/year → design indexes, partitions/archive by FY, paged ledgers.

### Integration with other tracks
- Petty purchase ≥ ₹5,000 → Purchase requisition (A). Material bought at site → stock/consumption against job or ticket (A/B).
- Expense against **service ticket / job / project** (B) — replaces Service Hub's own expense sheet; one expense module for all.
- Vendor payments and PDC (A: T4/T5) feed the cash-flow planner; project receivables link to sales orders (D) and invoices.
- Maps: the map API key stays server-side only; budget alerts; allow a manual fallback when the API is down (flagged).

---

## 2.D Reference app D — SESS Sales CRM V3.2

### Purpose
Enquiry → offer → revisions → follow-ups → stage/probability → PO won/lost, with **automatic 10-day customer reminders**, e-mail logging, reply-based stage updates and a management KPI dashboard. ~2,470 offers 2020-21 → 2026-27 migrated.

### Data (sheets → future tables)
- **All Leads / Offers (65 columns):** Offer ID, customer, enquiry mode/date, offer date, offer age, sales owner, **business type** (Sales / Service / Spares), department, zone, customer/tender detail, product/scope, qty, original / revised / current offer value, **sales stage + probability %, weighted pipeline**, priority, last / next follow-up, follow-up alert, days overdue, customer response, expected decision date, **PO number / date / value**, discount, **lost reason, competitor**, contact person, phone, e-mail, CC, address, send by, authorised by, year, notes/next action, data completeness %, missing information, opportunity score, sales grade, recommended action, FY quarter, month, UUID, requested/prepared by, created/updated by/at, **ERP Sync Status, ERP Record ID**, e-mail automation on/off, reminder scope, company, **base offer ID, revision no, revision status, parent revision**, secure response token.
- **Line Items** (sl no, item/product, description/scope, qty, unit, unit price, amount), **Offer Terms** (per-offer snapshot of terms), **Terms & Conditions** master, **Offer Revisions** (before/after JSON, reason), **Communications** (direction in/out, channel, from/to/cc, subject, message, status, thread ID), **Email Log**, Audit Log, Company Master, Users (Admin / Manager / Sales / Viewer), Custom Options, Settings (stages with probability: NEW ENQUIRY 10, REQUIREMENT CLARIFICATION 20, OFFER SUBMITTED 30, TECHNICAL EVALUATION 40, IN PROGRESS 50, NEGOTIATION 80, FINAL 90, WORK COMPLETED 95, MATERIAL DELIVERED 95, WON 100, LOST 0, ON HOLD 10; zones; priorities; owners).

### Rules to keep
- **One common FY offer series** for Sales, Service and Spares: `SESS/OFR/0941/2026-27`; revisions `SESS/OFR/0941/R01/2026-27`; new FY restarts at 0001; number allocated **only after validation, inside a lock** (no gaps from invalid saves, no duplicates under concurrency).
- **Edit (same number, reason + before/after audit)** vs **Revise (new R-number, parent kept, previous marked superseded)**.
- Editable terms per offer, stored as a snapshot (master changes never alter a saved offer).
- **Auto reminder** 10 days after offer/revision date and then every 10 days after the last successful reminder; never on Sunday; per-lead on/off; master switch; skip won/lost/closed/cancelled/superseded; reminder scope (current FY / 6 / 12 months).
- **Inbound replies** logged to the offer; clear phrases (PO confirmed, cancelled, negotiation, technical evaluation, final approval, under review, on hold) update the stage; unclear → "MANUAL REVIEW REQUIRED".
- Quotation print with company letterhead, GSTIN, logo, line items, GST, total, terms; ledger defaults to current FY with 3/6/12-month views; dashboard: total leads, open, pipeline, weighted pipeline, won value, win rate, funnel, monthly and 3-year comparison.

### Integration with other tracks
- **WON offer → Customer PO → Job Order** (existing ERP production flow) → BOM → purchase requisitions (A) → FAT → machine DC → invoice → receivables (C cash-flow).
- Service offers (AMC/CAMC, repairs, spares) link to customers/machines and service tickets (B).
- One **customer master** and one **communication/notification engine** for customers, vendors and employees.

---

## 3. Shared foundation (build first — used by every track)

| # | Component | Requirement |
|---|---|---|
| F1 | **All-employee access** | Every employee has a Keycloak identity (staff realm) mapped to an ERP employee; one web + mobile-friendly portal; role master decides pages and actions, enforced server-side; "own records only" where the reference apps use it. Onboarding/offboarding (disable = immediate block everywhere) usable by HR/Admin. |
| F2 | **Role & permission master** | Merge roles of all four apps (§6); configurable page/action matrix like Service Hub PAGE_ACCESS; AUDITOR read-only role; maker ≠ checker everywhere. |
| F3 | **Masters (single source)** | Company (letterhead, logo, GSTIN, PAN, CIN, address, state, bank, terms per document type), Employee (department, sub-department, grade, reporting manager, phone, e-mail), Customer (+ sites, contacts, GSTIN, zone), Vendor (+ qualification per category), Item, Machine (serial, model, refrigerant, warranty/AMC/contract dates), Location/Place, Options/Lists. |
| F4 | **Numbering service** | FY-based series per document type and company, revisions (R01…), allocation only after validation inside a transaction lock, manual/legacy numbers with duplicate block, counters imported from the reference apps. |
| F5 | **Tracking backbone** | Status history (from → to, who, when, channel, remark), pending-with, age, SLA per stage, follow-up log, next follow-up date, overdue/escalation flags, "My pending items" per user, for every document type in every track. |
| F6 | **Notification engine** | E-mail (company SMTP/Workspace, from-aliases), templates per event, event triggers + scheduled digests (09:00/09:30), reminders with rules (every N days, skip Sundays/holidays, per-record on/off, master switch), escalation to reporting manager, recipients: employees, managers, management, **vendors, customers**; sent/bounce log; **inbound reply capture** (phase 2) linked to the record; WhatsApp hook designed for later. |
| F7 | **Attachments** | Private storage, many files per record, phone camera upload with compression, type/size limits, access only via ERP permissions, virus-safe handling; replaces Drive links. |
| F8 | **Print engine** | A4 per company letterhead + logo, O/D/T copies where required, editable terms snapshot per document, amount in words, IST times, page X of Y (MAGESHWARI's `src/print/` layouts are the start). |
| F9 | **Dashboards framework** | KPI cards, charts, period filters (1/3/6/12 months, FY, custom), company/department/employee filters, **every number drills down to its list**, export, cached aggregates. |
| F10 | **Audit, archive, retention** | Before/after audit on every change and approval, login audit, soft delete only, FY archive with read-only access, ≥ 3 FY online, backups (existing server design). |
| F11 | **Import framework** | Validate-then-load imports with a report for every reference app's data (DC Tracker 303 DCs; Service Hub tickets/customers/machines; Expense claims/advances/ledger; CRM 2,470 offers), idempotent (no double import). |
| F12 | **Maps service** (Track C) | Server-side place search + route distance with key protection, cost limits, audit of each calculation, manual fallback flagged. |

---

## 4. Tracks and release order

| Track | Module | Reference | Frontend owner (proposal) | Release |
|---|---|---|---|---|
| F | Shared foundation (§3) | all | both | first (Sprint 1–2) |
| **A** | **Stores + Purchase** incl. material DC, bills, vendor payments, PDC, ISO records | R-A + earlier docs + registers | ILAMPARUTHI | **first go-live** |
| B | Service + Production activity | R-B | MAGESHWARI | parallel; after A or with A when ready |
| C | Expenses, advances, petty cash, cash-flow planner | R-C | to assign | after A (TD to order B/C/D) |
| D | Sales CRM (offers → PO → job order) | R-D | to assign | after A |

One backend agent (Claude Code) only; items of several tracks may share one acceptance cycle; frontend owners never edit the same files.

---

## 5. Track requirements
### 5.1 Track A — Stores + Purchase (T0–T11)

**T0 Tracking backbone (every document type):** status history timeline (from → to, who, when, channel, remark), current owner ("pending with"), age in days, SLA per stage, follow-up log (call / e-mail / note / WhatsApp text), next follow-up date, overdue flags, one "tracking view" per document and a generic "My pending items" list per user.

**T1 PO tracking:** requisition (any employee via their portal; dept-head first approval within slab; then TD / MD limits as configured) → RFQ (**minimum 3 quotes**; single source allowed with reason) → quotes (incl. **vendor portal**) → comparison (**L1 default; L2 or other selectable with reason** based on qualification) → PO (**up to ₹5,000 Purchase Manager**, higher slabs as configured) → vendor acknowledgement / promise date → follow-ups → gate entry → GRN (partial / excess / short) → QC → close / short-close / cancel. PO amendment with **revision history**. **Rate contracts** designed now, used later. PO always generated and sent from the ERP.

**T2 Material DC tracking:** full parity with §2.A (RDC / NRDC / WDC, all workflows, flags, reminders, prints, ISO report), **stock linked**: material leaving on a DC reduces the source location and is tracked in an "out on DC" state per line until returned / consumed / lost / billed; returns re-enter stock after store verification. Separate numbering series per type/company/FY; machine DC (existing ERP feature) keeps its own series. Import of the 303 existing DCs.

**T3 Vendor bill tracking:** bill received → 3-way match PO / GRN / bill → price / qty variance with approval → due date → paid; lists: GRNs without bills, bills without GRN, disputed bills.

**T4 Vendor payment + outstanding:** outstanding by vendor, aging buckets, due dates, **MSME 45-day** watch, TDS (194C / 194Q), advances and their adjustment, payment advice to vendor. Tally remains the ledger; define the sync/export boundary.

**T5 PDC tracking (post-dated cheques to suppliers):** register (vendor, bank, cheque no, amount, issue date, cheque date, linked bills), status ISSUED / DUE / PRESENTED / CLEARED / BOUNCED / CANCELLED, reminders before cheque date, daily "cheques due" list for bank-balance planning, bounce handling.

**T6 Stores tracking + consumable control:** issues/returns per job, ticket, machine and employee; **standard (BOM norm) vs actual consumption**, variance and wastage alerts; returnable tools out; reorder / min-max alerts; non-moving and aging stock; refrigerant by weight and cylinder deposit/return; serial-number traceability (component → GRN → job → machine → warranty).

**T7 Notification engine:** e-mail first (company SMTP, templates, schedules, event triggers), WhatsApp hook designed for later. Daily 09:00 digests per role (employees: their pending items; store; purchase; accounts; management summary with top overdue). Escalation to the reporting manager after N days. **Vendor e-mails:** PO issued, delivery reminder, overdue, rejection / RTV, payment advice / PDC details. Sent-mail log, bounce log, per-role and per-event switches, test mailbox before go-live.

**T8 Dashboards:** Purchase (pipeline by stage, overdue POs, vendor performance, spend **excl. recoverable GST**, approvals pending), Stores (stock value, movements, aging, non-moving, reorder, consumables per job/employee, DC out/overdue, QC pending), DC (as §2.A.5), Payables/PDC, Management; **every number drills down to its list**; employee accountability table; role access as decided (Stores Manager → Stores; QC → QC part; Accounts → Purchase + Payables; TD/MD → all, both companies).

**T9 ISO 9001 records inside the ERP (no paper):** 7.1.5 calibration of measuring equipment, 7.5 documented information (retention, revision, access, no delete), 8.4 supplier evaluation / selection / monitoring / **re-evaluation** and vendor rating, 8.5.2 identification & traceability, 8.5.3 customer / supplier property, 8.7 nonconforming output + concessions, 9.1 supplier performance, 10.2 NC + CAPA / SCAR; auditor reports with prepared / reviewed / approved lines; read-only AUDITOR role. Every register of `manual-registers-DRAFT.xlsx` (27 registers: returnable / non-returnable gate pass, job work ITC-04, RTV + debit note, refrigerant cylinders, bin transfer, e-way bill, expiry, free-issue, calibration, CAPA/SCAR, document control, RCM, MSME 45-day, TDS, GST purchase, approved vendors, PO short-close, service purchases, excess receipt, bill variance, inter-company, scrap sale, service/AMC spares issue, serial numbers, vendor evaluation, stock count sheet) becomes an ERP screen or report.

**T10 Vendor portal:** vendors submit quotes, acknowledge POs, give delivery promise dates, see payment status. Separate identity realm/client, strict data isolation per vendor. May be a later sprint.

**T11 Validation catalogue:** every field and rule, self-approval / maker-checker blocks (already: R3 bill, G1 QC-by-GRN-operator), duplicates, GST state logic (R2), qty vs balance (partial issues), mandatory reasons, date sanity — each with an automated test.

---


### 5.1 Track A — additional notes
Stores + Purchase answers already given by the TD are in §5.1 T1. Petty purchases ≥ ₹5,000 arrive from Track C as requisitions. Vendor payments/PDC (T4/T5) feed the Track C cash-flow planner.

### 5.2 Track B — Service + Production activity
Full port of R-B (§2.B and its DATA_MODEL.md / PAGE_ACCESS.md): tickets SRV/PRD/TSK per FY, assignments (lead/support, accept/clarify/unable), daily plan for many employees, carry-forward, reassign, morning/evening reports with timeliness (on-time / late / very late / proxy / missed), open & overdue with re-plan reasons, verify & close, expenses **via Track C module**, revenue ledger + attribution + verification, customer feedback, projects (order → dispatch → installation), performance score with configurable weights, report calendar, task ledger, data quality page, year-end archive. Store integration: material requests from tasks, parts used = consumption, returnable DC for tools/spares, machine serial/warranty/contract links.

### 5.3 Track C — Expenses, advances, petty cash, cash flow
Full port of R-C (§2.C) with the rules exactly as listed; one expense module for all employees and for service tickets; employee ledger and settlement; cash-flow planner merged with Track A payables/PDC and Track D receivables; policies configurable per grade.

### 5.4 Track D — Sales CRM
Full port of R-D (§2.D): offers with line items, terms snapshot, edit vs revise, FY numbering with revisions, stages/probability/weighted pipeline, follow-ups, 10-day auto reminders (Sunday skip), communications log, inbound reply stage updates (phase 2), quotation print, dashboards; hand-over of WON offers to Customer PO → Job Order.

---

## 6. Roles (merge of all reference apps)
TD, MD/CFO, Admin, HR, IT Manager, IT Executive, Purchase Manager, Purchase, Store Manager, Store, QC Manager, QC, Accounts Manager, Accounts, Service Manager, Assistant Service Manager, AMC/CAMC Head, Service Coordinator, Production Manager, Assistant Manager, Sales Manager, Sales, Engineer, Technician, Admin Staff, Employee, Viewer, **Auditor (read-only)**, **Vendor (portal)**, later **Customer (response link/portal)**. Map to existing ERP roles/scopes and list gaps.

## 7. Cross-cutting rules
Two companies on every record; UTC storage / IST display; FY (Apr–Mar) numbering; maker ≠ checker; mandatory reasons for reject / cancel / override / exception / L2 choice / single source / lost material / excess KM; soft delete only; full audit; private attachments; lists with filter, columns, export, print; mobile-first screens for field staff; idempotent writes; plain-word confirmation before irreversible actions; performance designed for ~500k rows/year in the busiest table.

---

## 8. Deliverables and process
- **D1 SRS per track:** `audit\SRS-foundation-v1.md`, `SRS-tracking-v1.md` (A), `SRS-service-production-v1.md` (B), `SRS-expenses-v1.md` (C), `SRS-sales-crm-v1.md` (D). For every item: PRESENT / PARTIAL / MISSING in NexaERP today (file/API refs), screens, fields, rules, reports, notifications, roles, data model, APIs, size in days (backend + frontend).
- **D2 Multi-track sprint plan** (1-week sprints), foundation first, one backend agent, frontend owners per track, weekly TD demo, go-live estimate **per track**.
- **D3 Frontend contract per sprint** in `audit\contracts\`.
- **D4 Open questions for the TD** per track (max 40 each), only what this file, the repo and the reference apps do not answer.
- **D5 Reference commit** after running cycles: `docs/reference/<app>/` (code + guides only) and this file as `docs/requirements/SESS-NexaERP-Master-Requirements-v2.md`. Never the xlsx, backups, registers, e-mail lists or certificates.
- **Build rules:** never push (the TD pushes); full acceptance cycle per code item; one item per commit; park after one failed re-run; never touch the server; work outside the repo while a cycle runs.
- **Start now:** Sprint 1 = F5 tracking backbone + F6 notification engine core (test mailbox only). Everything else after the TD signs the relevant SRS.

---

## 9. Template for the next reference app (the TD will keep adding)
When the TD adds `audit\<app-name>\`, append a section with: purpose; roles; data (tables and key columns); numbering; workflows and statuses; rules and limits; reminders/notifications; dashboards/reports; prints; integrations with existing tracks and masters; data to import; known weaknesses not to copy. Then update the SRS, sprint plan and open questions.
