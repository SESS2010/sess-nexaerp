# SESS NexaERP — Backend Work Remaining

Date: 7 September 2026
Scope: Purchase and Stores only. Sales, Accounts, Production and HR are not counted.

---

## How to read this

Days are **engineering days** — one person working without interruption. They
are not calendar days.

"Done" means the code exists, the tests pass and the migration is applied to
the live database. It does not mean a person has used it through a screen.

---

# PURCHASE

## Done — 85%

| # | Piece | Evidence |
|---|---|---|
| 1 | Purchase requisition — raise, submit, verify, approve, reject, cancel | Proven end to end in PostgreSQL |
| 2 | Two-level approval, three bands | ₹4,999.99 / ₹5,000 / ₹1,00,000.01, all proven |
| 3 | Department approval mapping, 42 employees | Live |
| 4 | Approval fails closed when unconfigured | No hard-coded fallback |
| 5 | Stock check against a requisition | SUDALAI and KARTHICK can perform it |
| 6 | RFQ — create, invite vendors | Backend proven |
| 7 | Vendor quotation — submit, technical verification | Backend proven |
| 8 | Commercial comparison — create, recommend, approve, revise | Backend proven |
| 9 | Purchase order — create, submit, approve, issue, amend, cancel | Backend proven |
| 10 | Document numbering per company per year | Cross-company collision fixed |
| 11 | List endpoints, filters, sorting on every register | 17 routes audited |
| 12 | Vendor master, qualification status, bank metadata gating | Live |
| 13 | Item master, categories, UOM, barcode, HSN | Live |
| 14 | Master data import — vendors, customers, UOM | Framework with per-row errors |

## Remaining — 15%

| # | Piece | Days | Why it matters |
|---|---|---|---|
| 15 | **Vendor bill entry** | 5-7 | No bill, no actual cost |
| 16 | **Three-way match** — PO, GRN, bill | 4-6 | Price mismatch must reject the bill or revise the PO. No tolerance. |
| 17 | **Accepted-bill allocation to Actual BOM** | 3-5 | This is what makes offer-versus-actual possible |
| 18 | **FIFO cost layers** | 6-8 | Created at GRN, consumed at issue. Strictly first in first out. |
| 19 | Vendor rating — six computed dimensions | 6-8 | Specification written. Needs GRN and QC history to mean anything. |
| 20 | Vendor revaluation and suspension | 3-4 | ISO 8.4 requires it. Below 70% blocks a PO without TD and MD. |
| 21 | Purchase amendment and cancellation with reasons | 2-3 | Partially there |
| 22 | Advance payment against a PO | 3-4 | 30% advance is in every offer |
| 23 | Import purchase — currency, duty, clearing | 4-6 | Only if SESS imports directly |
| 24 | Vendor debit and credit notes | 3-4 | |
| 25 | Purchase return against a GRN and bill | 3-4 | |
| 26 | Item and Employee import adapters | 3-4 | Item has no Excel path today |
| 27 | Reports — purchase register, GRNI, vendor summary | 6-8 | |

**Purchase remaining: 51-71 days**

---

# STORES

## Done — 60%

| # | Piece | Evidence |
|---|---|---|
| 1 | Complete schema — 190 tables | Live and verified |
| 2 | **Controlled posting function** | Deterministic locking, idempotent replay, refuses negative AVAILABLE |
| 3 | Gate entry — create, edit, finalize | Three operators: SUDALAI, KAMALI, KARTHICK |
| 4 | **GRN** — draft, finalize, reverse | Mandatory vendor bill, lot allocation, serial capture |
| 5 | Over-receipt refused | Frozen decision, enforced |
| 6 | Warranty — bill date plus 13 months | Recorded at GRN |
| 7 | **QC inspection** per lot allocation | Partial acceptance proven: 2.95 accepted, 0.05 rejected |
| 8 | **Concession** — TD only, after rejection | With lifelong provenance |
| 9 | Stock posts to QC_HOLD then AVAILABLE | Witness point 3 passed |
| 10 | Warehouse and rack masters | 16 racks, QC rack, 2 customer-property racks |
| 11 | **Ownership and custody foundation** | 18 tables. Customer property, supplier loan, tool custody. |
| 12 | **Provenance, lot, serial genealogy** | 19 tables. Concession traceable to the machine. |
| 13 | **Estimated BOM** | Lifecycle, Excel import, item Draft governance, duplicate merge |
| 14 | **Production BOM and drawings** | Machine pinning, GA and part drawing revisions |

## Remaining — 40%

### The critical path — stock cannot leave the store without these

| # | Piece | Days | Why it matters |
|---|---|---|---|
| 15 | **Material Issue Request** | 8-10 | Stores never issues without an approved request |
| 16 | **Issue** | 8-12 | Scanner-first. CUSTODY_OUT plus CUSTODY_IN, never consumption. |
| 17 | **Material return** | 5-8 | 10 issued, 6 returned, 4 consumed. Without this everything stays out forever. |
| 18 | **Fitment confirmation** | 6-8 | This is what starts CONSUMPTION_OUT |
| 19 | **Actual BOM from fitment** | 8-10 | Generated, never authored |
| 20 | **Two variance baselines** | 5-7 | Operational against Production BOM, commercial against the frozen offer |

**Critical path: 40-55 days**

### Delivery Challan and engineer accountability

| # | Piece | Days |
|---|---|---|
| 21 | Three DC types — consumable, tool, machine | 6-8 |
| 22 | Customer signature capture on machine DC | 2-3 |
| 23 | Engineer custody assignment | 4-5 |
| 24 | **Custody handoff, line by line** | 8-10 |
| 25 | Shortfall explanation and Stores acceptance | 5-7 |
| 26 | Deviation recording, automatic | 5-7 |
| 27 | Escalation timers — one day, then Service Manager | 3-4 |
| 28 | Waiver with reason, counted against the Service Manager | 3-4 |
| 29 | Unidentified return register | 2-3 |
| 30 | Returnable DC closure and write-off | 4-5 |

**DC and custody: 42-56 days**

### Stock control

| # | Piece | Days |
|---|---|---|
| 31 | **Opening stock** — three-actor ceremony | 5-7 |
| 32 | Stock adjustment with approval bands | 4-5 |
| 33 | Cycle count — A monthly, B quarterly, C half-yearly | 8-10 |
| 34 | Count freeze and break with TD approval | 3-4 |
| 35 | Inventory periods, backdating limit of 7 days | 4-5 |
| 36 | Stock transfer between warehouses | 4-6 |
| 37 | Intercompany movement through a real PO and GST invoice | 6-8 |
| 38 | Scrap declaration and disposal, MD approval | 4-5 |

**Stock control: 38-50 days**

### Job work and customer property

| # | Piece | Days |
|---|---|---|
| 39 | Subcontract dispatch and return | 6-8 |
| 40 | Weight tolerance per process — powder coating 5%, CNC 10% | 3-4 |
| 41 | Customer material inward — separate from vendor GRN | 6-8 |
| 42 | Customer property custody and return | 5-6 |
| 43 | Removed parts as customer property | 3-4 |
| 44 | Tool custody, permanent and temporary | 5-7 |
| 45 | Calibration register, annual, warning not block | 4-5 |

**Job work and property: 32-42 days**

### Reports

| # | Piece | Days |
|---|---|---|
| 46 | Stock balance by every dimension | 4-5 |
| 47 | Movement roll-forward | 3-4 |
| 48 | FIFO valuation with ageing | 4-5 |
| 49 | **Component ancestry for one machine** | 4-6 |
| 50 | Overdue returnables and tools | 2-3 |
| 51 | Count variance and adjustment register | 3-4 |

**Reports: 20-27 days**

---

# TOTALS

| Module | Remaining |
|---|---|
| Purchase | 51-71 days |
| Stores critical path | 40-55 |
| Stores DC and custody | 42-56 |
| Stores stock control | 38-50 |
| Stores job work and property | 32-42 |
| Stores reports | 20-27 |
| **Backend total** | **223-301 days** |

Plus, outside these two modules:

| | Days |
|---|---|
| Cognito and real login | 5-7 |
| Installer and deployment | 8-10 |
| Backup automation | 3-4 |
| **Frontend**, running alongside | 40-60 |

---

# WHAT THIS MEANS IN CALENDAR TIME

223-301 engineering days is not 223-301 calendar days. Codex compresses this
heavily — it built the Estimated BOM lifecycle, item governance and the
Production BOM in one day, which by the table above is roughly fifteen days of
hand-written work.

The real limit is **decisions and witnessing**, not typing. Every piece needs
the Technical Director to decide the rules and then verify the result.

Observed rate over the last six weeks: roughly **4 to 6 engineering days of
work per calendar day**, when the machine is fast and the decisions are ready.

That gives **40 to 60 calendar days** of backend work remaining for both
modules, if nothing new is added and decisions do not stall.

---

# THE ORDER THAT MATTERS

Not everything is equally urgent. This is the sequence.

## Now — the evidence chain is broken here

```
PO → Gate Entry → GRN → QC → AVAILABLE     works
AVAILABLE → ???                             nothing
```

| # | Piece | Days |
|---|---|---|
| 15 | Material Issue Request | 8-10 |
| 16 | Issue | 8-12 |
| 17 | Material return | 5-8 |

**Until these exist, stock enters the ERP and can never leave it.**

## Then — actual cost becomes possible

| # | Piece | Days |
|---|---|---|
| 18-20 | Fitment, Actual BOM, variance | 19-25 |
| 15-18 (Purchase) | Vendor bill, three-way match, allocation, FIFO | 18-26 |

This is the point where **offer versus actual** works — the reason this ERP
exists.

## Then — go-live requirements

| | Days |
|---|---|
| Opening stock | 5-7 |
| Item and Employee import | 3-4 |
| Cognito login | 5-7 |
| Core reports | 12-16 |

## Then — everything else

DC and custody, cycle counting, job work, customer property, tools,
calibration, vendor rating.

**SESS can run Purchase and Stores before these are built.** They matter, but
they are not the first day.

---

# THE HONEST BOTTOM LINE

| Milestone | Backend days | Calendar |
|---|---|---|
| Stock can leave the store | 21-30 | 1 week |
| Offer versus actual works | 37-51 | 2-3 weeks |
| SESS can go live on Purchase and Stores | 60-80 | 4-5 weeks |
| Everything in the specifications | 223-301 | 8-12 weeks |

The four-to-five month figure quoted earlier included frontend, training,
parallel running and the whole specification. **The backend for a working
Purchase and Stores is closer to five weeks.**

What decides it is not typing speed. It is how fast decisions are made and how
carefully each result is witnessed — and that has been the strength of this
project so far.
