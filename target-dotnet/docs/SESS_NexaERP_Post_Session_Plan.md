# SESS NexaERP — Work Plan After the Three-Day Session

Date: 14 September 2026
Decided by: A. Paramananthan, Technical Director
Covers: everything after items 15 and 16 land

---

## Where this comes from

Every question I asked over the last two days has been answered. This document
records those answers and turns them into an ordered plan, so that when Codex
finishes the go-live blockers it has somewhere to go without waiting.

---

# PART 1 — WHAT WAS DECIDED

## Stock adjustment and cycle counting (item 18)

| Question | Answer |
|---|---|
| How does an item become A, B or C? | **Automatically, by annual consumption value** |
| Cycle count frequency | **Quarterly** |
| When does counting start? | **From the day the ERP is in use** — not deferred |

**Consequence:** classification must be recomputed on a schedule, and the
Stores Manager may override a class with a reason. Without classification the
cycle-count plan cannot run at all.

## Intercompany movement (item 19)

| Question | Answer |
|---|---|
| How often? | **5 to 10 times a month. Daily or monthly — regular, not rare.** |

**This changes the priority.** I had assumed intercompany was occasional and
placed it low. At 5–10 a month it is routine, and every one is a real sale with
a GST invoice, a PO, and entries in both companies.

Ten a month done by hand is where errors enter. **Item 19 moves up.**

## Customer property (item 21)

| Question | Answer |
|---|---|
| How many machines a month? | **1 or 2** |

Low volume. Real, but not urgent — the workflow matters more than the
throughput.

## Tools (item 22)

The tools register has been read. **The list exists and it is in good order.**

| | |
|---|---|
| Distinct tool types | **164** |
| Purchased | **730** |
| Issued to engineers | **564** |
| Remaining in Stores | **166** |
| Employee sheets | **23** |
| Individual issue lines | **288** |

**Purchased minus issued equals balance on every one of the 164 rows.** Not one
arithmetic error. That register is better maintained than most ERPs manage.

**Consequence:** no physical tool inventory is needed. The register imports
directly — 164 tool types, 288 custody lines, 23 holders.

## Vendor rating (item 23)

The spreadsheet has been analysed. **1,057 bills, 225 vendors, average 96.9.**

**Five of eight dimensions score full marks on every single bill:**

| Dimension | Mean | Full marks |
|---|---|---|
| Technical /15 | 15.00 | **100%** |
| Warranty /10 | 10.00 | **100%** |
| Commercial /10 | 10.00 | **100%** |
| Response /5 | 5.00 | **100%** |
| Overall /5 | 5.00 | **100%** |
| Documents /10 | 7.50 | 0% — but the same 7.5 every time |
| Quality /25 | 24.93 | 99% |
| **Delivery /20** | **19.46** | **88%** |

**Sixty-five points of a hundred measure nothing.** Documents adds a further
ten that are constant.

**Only two dimensions carry signal**, and one of those is undermined:

- **Delivery** — 127 bills late, worst 40 days. Real.
- **Quality** — but **zero rejections across 1,057 bills**. Either SESS has
  never rejected anything in two financial years, or rejections were never
  recorded.

**The model itself is sound.** Eight dimensions, sensible weights, four already
designed as automatic. The failure is that five are typed and everyone types
the same number.

## ISO documentation (item 43)

| | |
|---|---|
| Quality manual | **21 documents** |
| Process turtle diagrams | **12 processes** |
| Quality procedures | 2 PDFs |
| **Total** | **roughly 35 to 40** |

I estimated 200 to 500. **It is 35 to 40.**

**Document control drops from 40-50 days to 20-25.**

**The twelve turtle diagrams are the ISO process map:** Top Management,
Marketing, Training, Manufacturing and Assembly, External Provider, Quality,
Stores, Calibration, Maintenance, Design and Development, Installation, Tech
Support.

## Fitment reversal (settled during the session)

Codex established, and I accept, that a reversed fitment is **not** a return:

| Event | Quantity | Machine cost | FIFO |
|---|---|---|---|
| **Fitment reversal** | to engineer custody | **falls to zero** | **untouched** |
| **Return to Stores** | to stock | — | **restored, reverse consumption order** |

The machine did not consume the component, so its cost is zero. The material is
still issued, so the FIFO consumption stays on the issue. Two different events,
two different effects — and Codex checked rather than applying one rule to both.

## FIFO partial return (settled during the session)

**Unwind the latest consumed layer first**, mechanically, by reversing recorded
consumption rows in creation order. Not a policy the code chooses — the rows
already exist and the return unwinds them.

Layers stay immutable. A return appends a restoration entry referencing the
consumption it unwinds.

---

# PART 2 — THE ORDER OF WORK

## Now — the go-live blockers

| Item | Status |
|---|---|
| **15 — the ten reports** | in progress |
| **16 — authentication** | report done, Keycloak witness next |

Nothing else starts until these land. Without reports, Stores cannot answer a
question. Without authentication, nobody can sign in.

## Block A — what SESS needs in the first weeks of use

Ordered by how soon the absence will hurt.

### A1 — Intercompany movement (item 19, part)

**Five to ten a month, starting 1 October.** Every one is a sale from one SESS
company to the other: GST invoice, purchase order, entries in both ledgers.

Ten a month by hand is where mistakes accumulate quietly.

**8 to 10 days.**

### A2 — Stock adjustment (item 18, part)

Opening stock will be wrong somewhere. It always is. Without an adjustment
path, the only fix is direct SQL.

- bands: below ₹5,000 Stores Manager; ₹5,000–₹1,00,000 TD; above MD
- serialised identity always TD
- write-off TD with Accounts concurrence

**6 to 8 days.**

### A3 — Tools (item 22)

730 tools, 564 already with engineers, and the register is clean enough to
import as it stands.

- import the 164 types, 288 custody lines, 23 holders
- individual custody, never a set
- temporary issue in **days**
- loss written off at depreciated value; damage replaced
- resignation clearance blocked while custody is open

**10 to 12 days**, and lower than I first estimated because the list exists.

### A4 — Vendor rating (item 23)

**Five dimensions become automatic**, sourced from evidence the ERP already
holds:

| Dimension | Source |
|---|---|
| Quality | accepted ÷ received, from the QC inspection |
| Delivery | PO committed date against GRN receipt date |
| Warranty | months already recorded at GRN |
| Commercial | payment terms already on the PO |
| Documents | whether the required attachments exist on the GRN |

**Three stay manual**, per GRN, by the QC Manager: Technical /15, Response /5,
Overall /5.

Then **80 of 100 points come from evidence**, and a vendor who delivers late or
ships rejects cannot score 96.

The 1,057 historical bills import as history, **marked LEGACY**, so nobody
mistakes a typed 15/15 for a measured one.

**12 to 15 days.**

**Total Block A: 36 to 45 days.**

## Block B — the dashboards (items 29-34)

Partly started. Purchase workload, spending, GRNI, vendor advance, open
commitments and Stores workload projections already exist.

Remaining: Job Order and cost dashboard, the TD and MD decision view, the
notification page, the engineer portal.

Two rules that do not bend: **outstanding custody is always reported BY
ENGINEER, never as a total**, and **offer-versus-actual is the primary number**.

**15 to 20 days** from where it stands.

## Block C — quality and ISO (items 35-39, 49)

### C1 — Calibration register (item 37)

Twelve master instruments, annual third-party certification, certificate stored,
due-date reminder. Adding an instrument must be a screen.

**8 to 10 days.**

### C2 — Calibration back-assessment (item 49)

**ISO 9001 clause 7.1.5.** When an instrument fails calibration, list every
measurement taken with it since its last valid certificate. QC decides what to
re-check. The ERP produces the list; it does not decide.

**5 to 6 days.**

### C3 — Job-work inspection (item 36)

**Powder coating by COUNT**, signed by both QC and Stores, no tolerance.
**CNC by WEIGHT** on the same DC, scrap included, 10 per cent editable, external
weighbridge slip above 500 kg.

**10 to 12 days.**

### C4 — Stage check sheet (item 35)

Fifteen stages per job order, prepared by QC only. A chamber cannot reach FAT
with an unsigned stage. Thickness and test values read from the job order, never
a global constant. Stage authority binds to the **QC_MANAGER role**, never to a
person.

**20 to 25 days.**

### C5 — QC working screen (item 39)

**6 to 8 days.**

**Total Block C: 49 to 61 days.**

## Block D — custody and customer property (items 17, 21, 40-42, 50)

### D1 — Delivery challan, two axes (items 40, 38)

Material type: CONSUMABLE, TOOL, MACHINE, SPARE.
Commercial nature: RETURNABLE, NON_RETURNABLE, WARRANTY_FREE — **three, not
four.** DEMO is a purpose on a RETURNABLE DC with a mandatory due date.

TOOL may only ever be RETURNABLE.

**12 to 15 days.**

### D2 — Engineer custody and accountability (item 17)

The problem raised on 3 September: material goes out with an engineer, he moves
site, nobody can find it.

Line-by-line handoff, one day to accept, one day to explain, Stores decides on
shortfall, deviations recorded automatically, waivers counted against the
Service Manager.

**25 to 30 days.**

### D3 — Customer property (items 21, 50)

**1 to 2 machines a month.** The inward is always accepted even with no PO; the
**work** is blocked. Removed parts are always customer property. Damage is
reported to the customer and recorded — **ISO 8.5.3**.

**15 to 18 days.**

**Total Block D: 52 to 63 days.**

## Block E — remaining stores and purchase (items 18 rest, 19 rest, 20, 24)

| | Days |
|---|---|
| Cycle counting, ABC classification | 10-12 |
| Scrap declaration and disposal | 8-10 |
| Job work and subcontract | 10-12 |
| Debit and credit notes, purchase return, contracts | 10-12 |

**Total: 38 to 46 days.**

## Block F — document control (item 43)

**Thirty-five to forty documents**, not hundreds.

Whole quality system: Purchase, Stores, Production, Service, HR. Each department
owns its own documents. Annual review mandatory. The approver is never the
author. Obsolete revisions retained but never returned as current. The quality
manual is approved by the **MANAGEMENT_REPRESENTATIVE role**, never by a named
person.

**20 to 25 days.**

## Block G — the remaining decided items (44-48)

| | Days |
|---|---|
| Reorder level on selected items, notifying both Purchase and Stores | 5-6 |
| Shelf life on chemical consumables, immediate notification | 5-6 |
| Purchase weighing at GRN | 3-4 |

**Total: 13 to 16 days.**

---

# PART 3 — TOTALS

| Block | Days |
|---|---|
| A — first weeks of use | 36-45 |
| B — dashboards | 15-20 |
| C — quality and ISO | 49-61 |
| D — custody and customer property | 52-63 |
| E — remaining stores and purchase | 38-46 |
| F — document control | 20-25 |
| G — remaining decided items | 13-16 |
| **Total** | **223-276 engineering days** |

At the observed rate of **5 to 8 engineering days per calendar day**:

**28 to 55 calendar days — mid-October to mid-November.**

---

# PART 4 — WHAT IS NOT IN THIS PLAN

| | Why |
|---|---|
| **Sales module** | specified; 35-45 days for stage 1, 40-60 for the AI writer. After Purchase and Stores are in real use. |
| **Service module** | questions asked, not yet answered |
| Accounts, Production, Design, HR, Maintenance | not started |
| Items 25-28 hardening | **already done** — concurrency, failure, backup, import purchase |

---

# PART 5 — WHAT STILL BLOCKS WORK

| Question | Needed for |
|---|---|
| Shelf life — which item categories? | item 46 |
| Spare and service offer type codes | Sales |
| A salesperson's view of another region | Sales |
| SMTP settings for both email addresses | Sales, Service |
| Whether SESS hardware can run a local model | Sales stage 2 |
| Service module — the eight questions asked | Service |

None of these blocks Block A. They can be answered while it is built.

---

# PART 6 — THE HONEST SHAPE OF IT

**Backend go-live readiness: 1 to 2 days away.** Items 15 and 16.

**Backend feature-complete against everything decided: mid-October to
mid-November.**

**Frontend: 22 screens built, 7 proven end to end.** One developer. That is the
real constraint and it has been since 10 September.

**The grid remains the only number I cannot estimate.** It is due tomorrow when
ILAMPARUTHI returns.

Everything in Blocks B through G can be built **while SESS is already using
Purchase and Stores**. None of it blocks 1 October, and holding the go-live for
any of it would trade a working system today for a more complete one later.
