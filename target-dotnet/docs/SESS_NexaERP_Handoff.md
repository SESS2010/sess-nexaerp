# SESS NexaERP — Project Handoff

Date: 1 September 2026
Owner: A. Paramananthan, Technical Director
Repository: `github.com/SESS2010/sess-nexaerp` (private)
Current commit: `3580e07`
Live database: `sess_nexa_erp`, PostgreSQL 17, 130 tables in `advance`

---

## 1. What this is

A .NET 10 + PostgreSQL ERP for Sri Eswari Scientific Solution, built for two
purposes: to run SESS itself, and to be sold to other companies as an
installable product.

Deployment model: **per-customer installation on their own AWS**, not shared
SaaS. Each customer gets their own database. Within one installation, multiple
legal entities are supported.

A third-party store package (BroPOS, cloud) is in use as a **temporary**
measure while this is built. It is not the long-term answer — no source code,
data held by the vendor, and SESS intends to sell an ERP in the same space.

---

## 2. Current state

| Layer | Status |
|---|---|
| Database schema | 130 tables live and verified |
| Purchase backend | **Proven end to end** — PR through issued PO, all three approval bands |
| Approval engine | Live, enforced in C# and PostgreSQL |
| GST configuration workflow | Live — Accounts Manager creates, TD or MD approves |
| Master API | Item, vendor, customer, lookups, import framework |
| Stores | Controlled posting function and Gate Entry only |
| Frontend | Login, Employee, Vendor, Customer, Item, Customer PO screens |
| Real data | 91 vendors, 139 customers, 1368 items — on the developer's machine |
| AWS | Account verified. Nothing deployed. |

Tests: 727 total, 669 pass, 58 fail. All 58 are the documented REV869B
PostgreSQL opt-in set and are expected.

---

## 3. The audit verdict — read this first

`outputs/purchase_stores_audit_readiness_gap_analysis.md`

An auditor asking *"show me every component in chamber X, its GRN, vendor,
accepted bill, QC inspection and every deviation approval"* would get an answer
**only as far as the issued purchase order**.

```
PR → RFQ → quotation → comparison → PO → issue     can be shown
PO → Gate Entry                                    can be shown
Gate Entry → GRN                                   FIRST BREAK
GRN → QC → stock                                   no
stock → MIR → issue → fitment                      no
fitment → Actual BOM                               no
Actual BOM → accepted vendor bill                  FINAL BREAK
```

SESS can defend **how a purchase was requested, sourced, approved and issued**.
It cannot defend **what was physically received, inspected, accepted, stored,
issued, fitted, billed, paid or counted**.

That gap is the work.

---

## 4. Migration chain

Twenty-eight migrations applied. The recent ones:

| Migration | What |
|---|---|
| FirstStoresPart1FoundationInboundNotifications | Config, gate entry, notifications |
| FirstStoresPart2GrnAndSerials | GRN and serial tables |
| FirstStoresPart3AQcOutboundDocuments | QC, job order, issue request, DC |
| FirstStoresPart3BLedgerActivation | Stock ledger contract |
| MasterDataImportFramework | Excel import and export |
| ControlledTaxGstWorkflow | GST governance |
| StoresSlice0ControlledPostingAndGateApi | Controlled posting, Gate Entry API |
| WarehouseAndRackMaster | Warehouse and rack masters |
| RemoveDevelopmentLoginPasswords | Removed a production password table |
| CorrectCustomerPoIntakeRevisionsAndPrLink | Customer PO corrections, PR link |

Never edit an applied migration. Write a follow-up.

---

## 5. Business decisions — settled and frozen

`docs/SESS_ERP_Stores_Full_Schema_Guideline.docx` holds the frozen decision
sheet. These are not open for re-litigation.

### Multi-company

Two entities: SESS Pvt Ltd and SESS Proprietorship. Complete data separation;
the user selects a company after login and sees only that company's data.

Masters are shared — items, vendors, customers. Transactions are company-scoped.

**Employees are shared.** Hours are charged to the company whose work it was.
SESS Pvt Ltd bills SESS Proprietorship monthly at salary ÷ 208 hours, no
overhead loading.

**Material moving between the two companies is a real sale.** GST invoice
mandatory, raised through the normal purchase flow — PR, standard approval
matrix, PO with the other SESS company as vendor, GRN, QC. Nobody moves
material between companies without that PO.

### Approvals

| Amount | Approval |
|---|---|
| Below ₹5,000 | Department Manager only |
| ₹5,000 – ₹100,000 | Department Manager → Technical Director |
| Above ₹100,000 | Department Manager → Managing Director |

Level 1 by requesting department: production-side to PRODUCTION_MANAGER
(SESS-25), office-side to ACCOUNTS_MANAGER (SESS-14).

Only four people approve anything: SESS-01 (TD), SESS-02 (MD), SESS-25, SESS-14.
PURCHASE_MANAGER creates purchase orders and never approves one. Self-approval
is blocked; level 2 must differ from level 1.

### The twelve contradictions — final position

Ten kept, two changed.

| # | Rule | Decision |
|---|---|---|
| 1 | QC hold | KEEP — accept or reject only |
| 2 | Over-receipt | KEEP — refused outright |
| 3 | Negative stock | KEEP — never |
| 4 | Issue before MIR approval | KEEP — never |
| 5 | Issue beyond approved BOM | KEEP — **notification only, not blocked** |
| 6 | Duplicate serial | KEEP — always blocked |
| 7 | Returnable loss | CHANGE — closure via approved typed write-off |
| 8 | Actual BOM costing | KEEP — **accepted vendor bill allocation** |
| 9 | QC inspector | CHANGE — resolve effective QC_MANAGER by role |
| 10 | Reserved stock | KEEP |
| 11 | QC hold vs quarantine | KEEP |
| 12 | Tool master identity | KEEP |

Number 5 matters in practice. An engineer needing two more metres of copper
pipe gets it, with a notification. Blocking that stops production while someone
hunts for an approver. The variance shows in Actual versus Estimated BOM.

### Two situations that look the same and are not

| | Internal chamber assembly | Material going out to a customer |
|---|---|---|
| Example | BOM said 4m, used 5m | Offer quoted 4m, installation needs 5m |
| At stake | Internal cost | Revenue |
| Rule | **Notification only** | **TD decides** |
| TD's options | — | supply free, revise the offer, or raise a separate offer and obtain a PO after installation |

The same physical act has different commercial consequences depending on where
the material is going.

### Purchase

Vendor comparison, in order: technically unqualified is rejected regardless of
price; lead time missing the required-by date is rejected; among the rest, a
vendor offering warranty is preferred even at 1–2% higher; otherwise lowest
price. The ERP recommends, a human chooses, the reason is recorded.

**Price mismatch has no middle ground.** If the vendor invoice differs from the
PO, either the invoice is rejected or the PO is revised. A consequence worth
stating: because the bill must match the PO, provisional and final values are
identical and there is no purchase price variance.

### Stores inward

```
Gate entry → GRN → QC rack → inspection → store rack
```

- Gate entry permission is held by **SUDALAI, KAMALI and KARTHICK** — three
  people, because one person means the process breaks when they are away and
  someone writes it on paper.
- One PO may have many gate entries. One gate entry produces one GRN.
- **Vendor bill is mandatory.** No bill, no GRN.
- Serial and warranty captured at GRN, not on the item master.
- Serial mandatory above ₹5,000 **unit rate**.
- Warranty: 12 months from installation or 13 months from bill date, whichever
  is earlier.
- QC is per **GRN-line lot allocation**, not per line. Two supplier batches on
  one line get two inspections; one failure must not contaminate the other.
- Missing policy **fails closed to QC_HOLD**. Absence of a policy never implies
  exemption.
- An inspection shortfall is DISCREPANCY_PENDING, not "QC rejected". Missing
  material is a shortage, not a quality failure.
- **Concession is allowed**, TECHNICAL_DIRECTOR only, after a recorded
  rejection, for an exact quantity and serials, with the failed parameter,
  measured value and technical justification. It must remain traceable into the
  machine's Actual BOM for the life of the chamber.
- All three documents immutable once finalized. Corrections are a reversal plus
  a new document.

### Stores outward

**Stores never issues without an approved request. No exception.**

Issue purposes: FACTORY_ASSEMBLY, PROJECT, SERVICE, WARRANTY, DEMO, SALE,
FREE_OF_COST.

Returnable DCs carry a mandatory expected return date. Non-returnable needs
department-owner approval plus mandatory TD and MD notification.

Subcontract weight balance: dispatched and returned weight both recorded.
Tolerance is **per process** — powder coating around 5%, CNC around 10% —
configurable by TD, MD or IT_MANAGER. Within tolerance, accepted automatically
with scrap recorded. Outside, the vendor explains and it needs approval.

### Job order and BOM

Job order closes on bill entry. **BOM stays open until warranty ends.**

One job order per chamber, mandatory. Three BOM identities:

| BOM | Who | When frozen |
|---|---|---|
| Estimated / Offer | Design prepares, TD approves | when the offer is submitted |
| Production | Production prepares, TD approves | per machine, revision pinned |
| Actual / As-Built | generated from fitment | never authored by hand |

**Two variance baselines, both required.** Operational variance against the
approved Production BOM shows whether production performed. Commercial variance
against the frozen offer BOM shows whether the offer was priced correctly. An
engineering revision must never erase the fact that the original offer
underestimated material.

Consumption begins at **confirmed fitment**, not at issue.

Actual BOM includes material at accepted bill value, subcontract, production
and QC and installation labour, installation expenses, and warranty-period
spares and labour. It excludes AMC and CAMC.

### Valuation

**FIFO, strictly** — even for serial-tracked items. Serial provenance is
recorded but does not select the cost layer. This matches how the accounts
already value closing stock.

Spare pricing: default margin percentage per item on the item master, varying
by item. Overridable on an offer line, recording who, from what, to what, why.

### Service

Complaints arrive by phone, email, WhatsApp and later a customer portal.

**One installed-machine register** covers SESS machines in warranty, out of
warranty, under AMC, under CAMC, and other-brand machines. Other-brand machines
have no job order and no SESS warranty; everything else is the same shape.

| Situation | Approver |
|---|---|
| Warranty, AMC or CAMC material | SERVICE_MANAGER |
| Anything without a customer PO | **TECHNICAL_DIRECTOR** |

The ERP shows the entitlement. A human decides.

### Customer property

Removed parts are **customer property, always, and returned**. Inbound demo
equipment is custody and is returned — never capitalized by possession.

Accessories are recorded as a list with photo evidence, not individually
serialised. Due-date extension requires TECHNICAL_DIRECTOR.

### Stock control

| Control | Rule |
|---|---|
| Adjustment approval | Reuses the ₹5,000 / ₹100,000 bands. Serialized identity always TD. Write-off is TD with Accounts concurrence. |
| Backdating | Open period only, maximum 7 days, beyond that TD |
| Opening stock | Three separate actors — Stores counts, Accounts values, TD authorises |
| Count frequency | A monthly, B quarterly, C half-yearly, full count annually |
| Count freeze break | TD approval, recorded, and the affected scope recounted |
| Scrap sale | Invoice before dispatch, payment before dispatch, Stores selects the buyer, **MD approves every disposal** |

Declaring material scrap and disposing of it are two different acts with two
different approvals. Do not collapse them.

### Configuration — editable, not hard-coded

Emergency purchase count and value, serial threshold, approval bands, QC ageing
limit, expense limits, job-work tolerance per process.

**Only TECHNICAL_DIRECTOR, MANAGING_DIRECTOR and IT_MANAGER may change them.**
Every change records who, when, old value, new value, reason. Changes affect new
documents only.

### Warehouse layout

| Company | Warehouse | Racks |
|---|---|---|
| SESS Pvt Ltd | Old Factory Store | Rack 1–8 |
| | New Factory Store | 4 big racks, 4 partitions each |
| SESS Proprietorship | Old Factory Store | Rack 9–12 |
| QC | One physical rack outside the old factory store | 6 category sections, 12 condition locations across both companies |

Item category codes are exactly three characters so the barcode trigger works:
`ELE`, `REF`, `FAS`, `PLC`, `FAB`, `MEC`. Barcodes read `SESS-ELE-000001`.

Racks can be added, renamed and deactivated through the UI. They cannot be
deleted — movements reference them forever. Deactivating a rack holding stock
returns HTTP 409 and PostgreSQL independently refuses the bypass.

### PII — deliberately excluded

Aadhaar, PAN, UAN, ESI, bank account, IFSC, mobile and emergency contact are
not stored for employees. **Aadhaar will never be stored** — the Aadhaar Act
prohibits it for an offline verification-seeking entity. That is a legal
constraint, not a preference.

Vendor bank metadata is permission-gated on every path including create, update
and audit history, and is excluded from Excel templates and exports entirely.

### Authentication

**OIDC only.** `PasswordHash = PENDING_IDENTITY_PROVIDER`.

A local password table was added by the team and has been removed. It created a
credential store in every customer database, including production, with the
runtime principal holding write access. Debug-only endpoints do not make a
production password table safe.

---

## 6. Documents — read before any work

In `target-dotnet/outputs/`:

| Document | Purpose |
|---|---|
| `purchase_stores_audit_readiness_gap_analysis.md` | **What an auditor would find.** Read this first. |
| `sess_business_process_purchase_stores.md` | Business authority for Purchase and Stores |
| `sess_api_contract.md` | **Frontend authority.** Every endpoint and shape. |
| `backend_architecture_reference.md` | Patterns, conventions, layer rules |
| `first_stores_module_schema_design.md` | 25-table Stores design |
| `first_stores_module_migration_plan.md` | The migration split and why |

In `target-dotnet/docs/`:

| Document | Purpose |
|---|---|
| `SESS_ERP_Stores_Full_Schema_Guideline.docx` | **Frozen decisions.** The twelve contradictions and the schema answers. |
| `SESS_ERP_Overall_Store_Activity_and_Implementation_Roadmap.docx` | Full store scope baseline |
| `SESS_ERP_Advanced_Mobile_Service_Module_Guideline.docx` | Service module — later phase |
| `installation/authentication-bootstrap.md` | Customer deployment checklist |
| `installation/master-data-import.md` | Import framework usage |
| `installation/trial-master-data.md` | Trial data apply and removal |

These are the shared memory. A Claude or Codex conversation does not know what
was decided elsewhere — these documents do.

---

## 7. What works today

- Two-level purchase approval, enforced in C# and in PostgreSQL functions
- The complete PR → RFQ → quotation → technical verification → comparison → PO
  → issue path, proven against real PostgreSQL in all three approval bands
- Controlled GST configuration workflow with separation of duties
- Multi-company employee authorization, 42 people in both companies
- Master data import and export through a validating framework with per-row
  errors and audit batches
- Controlled stock-posting function with deterministic lock ordering, idempotent
  replay and balance protection — built before any posting endpoint exists
- Gate Entry create, edit, list, detail and finalize
- Warehouse and rack masters with stock-aware deactivation guards
- Append-only stock ledger with database triggers
- Runtime principal guard — the API refuses to start as a superuser in Release

---

## 8. What does not work yet

- **Nobody can log in for real.** AWS Cognito is not configured and the
  bootstrap ceremony has not run. A Debug-only development token endpoint
  covers local work.
- **GRN does not exist.** This is the first break in the evidence chain and the
  next piece of work.
- No QC, stock posting, issue, DC, job order or fitment services
- No vendor bill, PO/bill match or accepted-bill allocation
- No FIFO cost layers, inventory periods, counts or valuation reports
- No supplier evaluation, re-evaluation or suspension
- No frontend beyond master screens
- Database principals not provisioned; the API connects as `postgres`

---

## 9. Remediation order — from the audit analysis

**P0 — before Stores can be called operational**

1. PO-linked GRN, lot and serial allocation, QC, controlled receipt posting
2. Rejected material return, repair, scrap, and TD concession with lifelong
   provenance
3. Job Order, approved MIR, issue and return, fitment, reversal
4. Accepted vendor bill, PO/GRN match, GST evidence, allocation to Actual BOM
5. FIFO cost layers, inventory periods, opening stock, counts, adjustments
6. Supplier evaluation, re-evaluation, NCR/CAPA, governed suspension
7. Enforceable segregation chains for receive/inspect/issue/pay, scrap, counts

**P1 — before audit evidence can be produced without spreadsheets**

Component ancestry dossier · approved vendor list and evaluation pack ·
inspection and nonconformance pack · valuation, roll-forward, count and cut-off
reports · GRNI and GST reports · access and segregation review · retention
schedule

**P2 — management effectiveness**

Trend reporting · insurance register · auditor dossier export · mobile and
customer portal

---

## 10. Team

| Person | Role | Work |
|---|---|---|
| A. Paramananthan | Technical Director | Backend with Codex, all business decisions, witnessing every change |
| ILAMPARUTHI D (SESS-32) | Developer | React frontend and master screens |
| SURANTHER P (SESS-12) | IT Manager | AWS, Cognito, environments, deployment |
| MAGESHWARI K (SESS-40) | Developer | Frontend, not yet started |

---

## 11. How this project is run

The method that has worked, and should continue:

1. **The Technical Director makes every business decision.** Codex does not
   decide business rules.
2. **Codex writes code, one bounded piece at a time.** Prompts say explicitly:
   do not invent, stop and ask if something is missing.
3. **Every claim is verified against the live database.** Codex reports a
   number; the owner runs the query. This has caught real failures — an EF model
   matching zero of 119 live tables, a test-count discrepancy that turned out to
   be a classification error, a production password table disguised as a
   Debug-only feature, and a least-privilege bug that would only have surfaced
   at a customer site.
4. **Every migration is witnessed before the next is written.** Backup, apply,
   verify counts, push.
5. **Decisions live in documents, not conversations.**

The habit that matters most: **when a report says something passed, check it.**

---

## 12. Rules for everyone

1. The API contract is authoritative. If an endpoint is not in it, ask.
2. Field names are PascalCase, matching the database. No mapping layer.
3. Frontend lives in `src/SESS.NexaERP.Web/` and does not modify the backend.
4. There is one backend. Node.js is for React tooling only.
5. Work on `feature/frontend`. Never commit to `main` directly.
6. **Run the full suite before pushing.** Expect 58 failures — the REV869B
   opt-in set. Anything else is yours to fix. A broken `main` blocks everyone.
7. Push every day.
8. Never commit passwords, connection strings or `.env` files.
9. Never edit an applied migration. Write a follow-up.
10. Data enters through the import framework, never through direct SQL. Direct
    SQL skips validation, business rules and audit, and afterwards nobody can
    say how a record got there.
