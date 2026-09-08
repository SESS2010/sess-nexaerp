# SESS NexaERP — Remaining Work Plan

Date: 8 September 2026
Scope: Purchase and Stores, everything decided and not yet built
Owner: A. Paramananthan, Technical Director

---

## Three new decisions, taken today

**Vendor advance payment.** SESS does pay some vendors in advance. The advance
attaches to the purchase order and is adjusted against the bill.

**Landed cost.** Freight, packing and every other charge incurred to bring an
item in becomes part of the item cost. It is not a separate expense. When SESS
later sells that item, the price already carries the freight, so stock value
must carry it too.

**Import purchase.** SESS buys components from China and other countries, by
sea and by air. Currency, customs duty, clearing charges and exchange rate all
apply, and all of them are landed cost.

The second one changes the vendor bill that was committed today. Item cost is
no longer the accepted bill line value alone; it is the accepted bill line value
plus its allocated share of every charge on that bill.

---

## Where the work stands

```
PR → RFQ → PO → Gate → GRN → QC → AVAILABLE → MIR → Issue → Return → Vendor bill
                                                                        ✅ today
Job Order → Fitment → Actual BOM → Offer vs Actual
   in progress          ❌              ❌
```

Purchase 90 per cent. Stores 75 per cent. What remains is listed below in the
order it should be built.

Day counts are **engineering days** — one person working uninterrupted. At the
rate this project has actually moved, four to six of these fit into one working
day.

---

# STAGE 1 — FINISH THE COST CHAIN

**Days 1 to 30. This is what makes offer-versus-actual work.**

Nothing else matters as much. It is the reason this ERP exists.

| Day | Work | Why |
|---|---|---|
| 1-4 | **Job Order API** — in progress | One machine, one job order. Production initiates, Accounts confirms. May exist with no customer PO for stock chambers. |
| 5-7 | **Landed cost on the vendor bill** | Freight, packing, insurance, and every charge allocated to item cost. Changes what was built today. |
| 8-12 | **Import purchase** | Currency, exchange rate, customs duty, clearing agent, sea or air. All of it landed cost. |
| 13-16 | **Vendor advance** | Advance against a PO, adjusted at bill acceptance, outstanding advance visible per vendor. |
| 17-25 | **Fitment and Actual BOM** | Consumption starts here, never at issue. Component ancestry — which serial went into which machine. |
| 26-30 | **Offer versus Actual variance** | Two baselines: operational against the approved Production BOM, commercial against the frozen Estimated BOM. |

**At day 30 SESS can answer: what did we quote, what did it actually cost.**

---

# STAGE 2 — MAKE IT USABLE

**Days 31 to 55. Without these, nobody can start using it.**

| Day | Work | Why |
|---|---|---|
| 31-32 | **ACL repair** | A database where the REV869B reassign failed cannot authenticate anybody. Deployment-breaking. |
| 33-39 | **FAT and PDI** | One chamber, one FAT. Component serials read from the Actual BOM, never retyped. Failed FAT produces a revision, not a cancellation. PDI is the same document with the customer present. |
| 40-46 | **Opening stock** | Three actors — Stores counts, Accounts values, TD authorises. Without it the ERP starts at zero and every issue fails. |
| 47-50 | **Item and Employee import adapters** | Item has no Excel path today. 1,388 items cannot be loaded any other way. |
| 51-55 | **Cognito and real login** | Nobody can sign in for real. Dev tokens are Debug-only. |

**At day 55 SESS can put real data in and real people can sign in.**

---

# STAGE 3 — THE REPORTS

**Days 56 to 70.**

| Day | Work |
|---|---|
| 56-58 | Stock balance by item, warehouse, rack, lot, serial, ownership, condition |
| 59-60 | Movement roll-forward — opening, receipts, issues, adjustments, closing |
| 61-63 | FIFO valuation with ageing buckets |
| 64-65 | GRNI — goods received not billed; and billed not received |
| 66-67 | Purchase register — PR through PO through GRN through bill |
| 68-70 | **Component ancestry for one delivered machine** |

The last one is the audit dossier. An auditor names a chamber and gets every
component, its GRN, its vendor, its accepted bill, its QC inspection and every
deviation approval.

**At day 70 SESS can face an auditor.**

---

# STAGE 4 — DELIVERY CHALLAN AND ENGINEER ACCOUNTABILITY

**Days 71 to 105.**

Specification already written: `docs/SESS_NexaERP_DC_Custody_Specification.md`.

| Day | Work |
|---|---|
| 71-76 | Three DC types — consumable, tool, machine — chosen at creation, immutable |
| 77-79 | Customer signature on machine DC at delivery |
| 80-83 | Engineer custody assignment |
| 84-91 | **Custody handoff, line by line** — both engineers on the same soft copy |
| 92-96 | Shortfall explanation and Stores acceptance |
| 97-101 | Deviation recording, automatic from real events |
| 102-103 | Escalation timers — one day, then Service Manager, then his own defect |
| 104-105 | Waiver with reason, counted against the Service Manager |

This solves the problem raised on 3 September: material goes out with one
engineer, he moves site, the DC does not move with him, and nobody can find the
material.

---

# STAGE 5 — STOCK CONTROL

**Days 106 to 140.**

| Day | Work |
|---|---|
| 106-110 | Stock adjustment with the frozen approval bands |
| 111-120 | Cycle count — A monthly, B quarterly, C half-yearly, full annually |
| 121-124 | Count freeze, break with TD approval, affected scope recounted |
| 125-129 | Inventory periods, backdating limited to 7 days |
| 130-134 | Stock transfer between warehouses inside one company |
| 135-140 | **Intercompany movement** — a real sale with a GST invoice, through the normal purchase flow |

---

# STAGE 6 — JOB WORK, CUSTOMER PROPERTY, TOOLS

**Days 141 to 175.**

| Day | Work |
|---|---|
| 141-148 | Subcontract dispatch and return, weight balance both ways |
| 149-152 | Tolerance per process — powder coating 5 per cent, CNC 10, configurable |
| 153-160 | **Customer material inward** — separate from the vendor GRN. Other-brand machines, SESS warranty returns, spares. A machine may arrive with no PO. |
| 161-166 | Customer property custody, due date, extension by TD |
| 167-170 | Removed parts as customer property, buyback only when explicitly agreed |
| 171-175 | Tool custody, permanent and temporary, resignation clearance |

---

# STAGE 7 — VENDOR RATING AND CALIBRATION

**Days 176 to 200.**

Specification already written:
`docs/SESS_NexaERP_Vendor_Rating_Specification.md`.

| Day | Work |
|---|---|
| 176-183 | Six-dimension rating, three of them computed and never typed |
| 184-187 | Twelve-month rolling window, provisional until three receipts |
| 188-191 | Bands, and the PO block below 70 per cent without both TD and MD |
| 192-195 | Revaluation — improvement letter, QC recommends, TD approves, probation |
| 196-200 | Calibration register, annual, Sansel as provider, expired warns not blocks |

---

# STAGE 8 — SCRAP, DEBIT NOTES, REMAINING PURCHASE

**Days 201 to 225.**

| Day | Work |
|---|---|
| 201-205 | Scrap declaration and disposal — invoice and payment before dispatch, MD approves every disposal |
| 206-210 | Vendor debit and credit notes |
| 211-214 | Purchase return against a specific GRN and bill |
| 215-218 | Purchase amendment and cancellation with reasons |
| 219-225 | Blanket and rate contracts drawn down over time |

---

# STAGE 9 — DEPLOYMENT

**Days 226 to 250. Only needed before a paying customer, not before SESS uses it.**

| Day | Work |
|---|---|
| 226-235 | Customer installation — empty PostgreSQL to running ERP in one day, by an IT person |
| 236-239 | Automated backup and a restore procedure that has been tested |
| 240-243 | Upgrade path — how version 2 reaches a customer database |
| 244-250 | **Load testing** — 300,000 items, 50,000 POs, 2,000,000 movements. Never tested. This was the question that moved us to .NET. |

---

# THE FRONTEND, RUNNING ALONGSIDE

One developer. Roughly 25 to 35 days, and it is the real constraint from here.

| Priority | Screens |
|---|---|
| 1 | MIR, Issue, Return |
| 2 | Vendor bill |
| 3 | Estimated and Production BOM, Excel import |
| 4 | Job Order, FAT |
| 5 | Reports |
| 6 | DC custody and the engineer portal |

Forty screens exist. Two have run end to end against the real API. That gap,
not the backend, decides when SESS can start using this.

---

# WHAT THIS MEANS IN CALENDAR TIME

| Stage | Engineering days | Calendar |
|---|---|---|
| 1 — cost chain | 30 | 1 week |
| 2 — usable | 25 | 1 week |
| 3 — reports | 15 | 4 days |
| 1 to 3 together | **70** | **2 to 3 weeks** |
| 4 to 8 — everything else | 155 | 5 to 7 weeks |
| 9 — deployment | 25 | 1 week |

**SESS can start using Purchase and Stores after stages 1 to 3 — roughly three
weeks of backend work.**

The frontend then decides the date. Allow four to six weeks in total, plus
training and a parallel run.

**Realistic go-live: late November to mid-December 2026.**

Everything from stage 4 onward can be built while SESS is already using the
system. None of it blocks the first day.

---

# HOW TO USE THIS LIST

Take one stage at a time. Do not start stage 2 before stage 1 is witnessed and
applied — that discipline is what has kept the migration chain intact through
forty migrations.

Within a stage, the order matters less. Across stages, it matters a great deal:
Actual BOM cannot exist before vendor bill, FAT cannot exist before Actual BOM,
and reports cannot exist before the data they read.

Nothing on this list has been dropped. Everything decided since 27 August is
here, with a day count and a place in the order.
