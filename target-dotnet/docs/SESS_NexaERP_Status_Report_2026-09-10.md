# SESS NexaERP — Status Report

Date: 10 September 2026
Prepared for: Managing Director, Technical Director, IT team
Basis: Codex specification implementation audit dated 9 September, verified
against the live database and the pushed repository

---

## Where the project actually stands

Codex audited **140 frozen decisions** taken since 27 August against the code
that exists.

| State | Count | Meaning |
|---|---|---|
| **Implemented** | **43** | A real user can complete the stated result end to end |
| **Partial** | **33** | Schema or part of a workflow exists; the result cannot be completed |
| **Not built** | **63** | Decided and written down; no code |

**Weighted completion: 43 per cent** — counting Partial as half.

This is one figure with one definition, and it replaces the several I have
quoted before. It measures *decisions honoured*, not features listed.

---

## What works today, end to end

The core purchasing and stores chain is proven against real PostgreSQL:

```
Purchase requisition → RFQ → quotation → comparison → purchase order
      ↓
Gate entry → GRN → QC inspection → put-away → AVAILABLE stock
      ↓
Material issue request → issue to engineer → material return
      ↓
Vendor bill → three-way match → FIFO layers
      ↓
Job Order → fitment → Actual BOM → FAT readiness
```

Also working:

- Three approval bands, ₹5,000 and ₹1,00,000, failing closed when unconfigured
- 52 roles, 51 employees, 159 role assignments, simultaneous multi-role
- Serial and lot tracking with full provenance from GRN to the machine
- QC partial acceptance and Technical Director concessions, traceable for life
- Estimated and Production BOM with revisions, freezing and Excel import
- Estimated versus Actual variance on quantity and value
- Every posting immutable; corrections are reversals, never edits

**209 tables. 65 migrations. 765 tests, zero failures.**

---

## What is decided but not built

Sixty-three decisions exist only on paper. The largest groups:

| Area | Decisions | Why it matters |
|---|---|---|
| **Delivery challan and engineer accountability** | 11 | The problem raised on 3 September — material leaves with an engineer, he moves site, nobody can find it |
| **Vendor rating** | 8 | The current spreadsheet scores every vendor 95 per cent because four dimensions are hand-typed |
| **Tools and calibration** | 8 | Every tool is an asset; none is tracked |
| **Customer property** | 7 | Other-brand machines and warranty returns |
| **Transfers and intercompany** | 7 | Movement between the two SESS companies |
| **Job work and RMA** | 7 | Subcontract dispatch, weight tolerance |
| **Stock control** | 5 | Opening stock, adjustments, cycle counts, scrap |
| **Landed cost, advance, import** | 3 | Freight into item cost; foreign purchases |
| **Reports and notifications** | 2 | See below |

---

## Two findings that need attention now

### 1. Notifications reach nobody

Tables exist. There is **no insertion path, no delivery worker and no read
endpoint**.

Every alert decided over six weeks currently tells no one:

- overdue returnable material
- BOM excess on an internal issue
- the one-day delivery challan handoff escalation
- calibration due
- QC ageing beyond two days
- approvals waiting
- material not returned the same day
- unexplained custody blocking FAT

The rules are enforced. The people who need to act are not told.

### 2. Opening stock does not exist

There is no way to load a starting balance. On the day SESS switches over, the
ERP would begin at zero and every material issue would fail.

This is a hard prerequisite for go-live, not a refinement.

---

## Frontend

Thirteen screens are built. **Three are proven end to end** against the real
API: purchase requisition, stock check, and material issue request.

The other ten render and their permission gating is correct, but no create,
submit or approve has been proven, because the developer's API runs as
`postgres` and the command ledger correctly refuses it.

The runbook to fix that was delivered yesterday
(`docs/installation/developer-runtime-principals.md`). Once the API runs as
`nexa_erp_runtime`, every screen will be run as the right person and a full
grid produced.

**Until that grid arrives, treat the frontend figure as unknown.** Thirteen
built and three proven is the honest statement.

---

## Quality of what has been built

Worth recording, because it is the reason the 43 per cent is trustworthy:

- **765 tests, zero failures.** For a month we carried 58 "expected" failures
  that turned out to be hiding a security model that had never run. That model
  was removed and replaced; the number is now zero and it means something.
- **Every migration is witnessed** — built, tested, backed up, applied, pushed,
  verified. Sixty-five migrations, no rollbacks.
- **The frontend developer has found seven defects the test suite missed**,
  including two that blocked an entire workflow and one that would have killed
  authentication at every customer site.
- **The build now fails** when a mandatory field has no readable source, or
  when someone granted an action cannot see the document. Those seven finds
  cannot recur.

---

## What remains, and how long

| Stage | Work | Engineering days | Calendar |
|---|---|---|---|
| **1** | Landed cost, vendor advance, import purchase | 12-17 | 3-4 days |
| **2** | Opening stock, item and employee import, notifications | 16-23 | 4-6 days |
| **3** | Reports and the audit dossier | 20-27 | 5-7 days |
| **4** | Cognito and real login | 5-7 | 2 days |
| — | **Subtotal: SESS can begin using it** | **53-74** | **2-3 weeks** |
| 5 | Delivery challan and engineer accountability | 42-56 | 2 weeks |
| 6 | Stock control — counts, adjustments, transfers, scrap | 38-50 | 2 weeks |
| 7 | Tools, calibration, customer property, job work | 32-42 | 1-2 weeks |
| 8 | Vendor rating | 25-30 | 1 week |
| 9 | Installer, backup, upgrade, load testing | 25-35 | 1-2 weeks |

Codex sustains four to six engineering days per calendar day when decisions are
ready and the machine is free.

**The frontend is now the constraint, not the backend.** One developer, ten
unproven screens, and five more modules to build.

---

## Realistic dates

| Milestone | Date |
|---|---|
| Backend ready for SESS to start using | **late September 2026** |
| Frontend caught up, master data loaded, training done | **November 2026** |
| **SESS running Purchase and Stores on NexaERP** | **late November to mid-December 2026** |
| Everything in the specifications | February–March 2027 |
| Installable at a paying customer | March–April 2027 |

Stages 5 to 9 can be built **while SESS is already using the system**. None of
them blocks the first day.

---

## The largest risks

**1. Frontend capacity.** One developer, thirteen screens built, three proven,
five modules still to come. The backend is now ahead and will stay ahead.

**2. Nothing has been tested at volume.** The question that moved this project
to .NET — will it hold 300,000 items and 2,000,000 stock movements — has never
been answered. It is scheduled but not done.

**3. Nothing has been tested with two people at once.** Every test runs one
user at a time. Two storekeepers will work the same afternoon.

**4. Notifications reach nobody.** Every escalation we designed is silent.

**5. No automated backup.** Every backup on this project has been typed by
hand.

---

## What I need from each of you

**Managing Director** — the 43 per cent is real and defensible. The date to
plan against is **late November to mid-December** for Purchase and Stores.
Everything after that is improvement, not launch.

**IT team** — provision the runtime principals, then run every screen as the
right person and send the grid. That single document will tell us more than
another week of building.

**Everyone** — the decisions are the bottleneck, not the typing. Every question
answered the same day turns into working software within two.

---

## One thing worth saying plainly

Six weeks ago this project had an EF model that matched **zero of 119 live
tables**. Today it has 209 tables, 65 witnessed migrations, 765 passing tests
and a proven chain from purchase requisition to a machine's Actual BOM.

The 63 unbuilt decisions are not a failure. They are written down, costed and
ordered — which is why they can be built quickly when their turn comes.

What matters is that **nothing decided has been quietly dropped**. The audit
proves it.
