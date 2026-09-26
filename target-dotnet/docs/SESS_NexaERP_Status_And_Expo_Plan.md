# SESS NexaERP — Status and Three-Day Plan

> **Superseded on 23 September 2026.** Go-live moved from 1 October to **8 October 2026**
> by governed decision: setup 1-3 October, both opening-stock ceremonies 5-6 October,
> BroPOS frozen 7 October, daily transactions from 8 October. The amended schedule and the
> reason for it are recorded in
> [the go-live runbook](installation/go-live-fresh-database-runbook.md#governed-schedule-change-23-september-2026-go-live-moves-to-8-october).
> This document is kept as the dated record of what was planned and known on 15 September.
> Its body is deliberately unchanged; its dates are no longer instructions.

Date: 15 September 2026
For: A. Paramananthan, Technical Director
Context: expo 16–18 September; TD, SURANTHER and MAGESHWARI away

---

# PART 1 — WHERE THE ERP STANDS TODAY

## Verified this afternoon

| | |
|---|---|
| origin/main | `fc6de74` |
| Release suite | **836 passed, 0 failed**, 22m 35s |
| Live database | **migrations 78 → 103 applied** |
| Principals | RECONCILED, VERIFIED |
| Backup | taken before migrating |

Everything below is measured, not estimated.

## The go-live blockers

| # | Item | Status |
|---|---|---|
| 13 | Opening stock | **done** |
| 14 | Import adapters and templates | **done** |
| 15 | **The ten reports** | **done today** |
| 16 | Authentication | **decision changed to local Keycloak; build pending** |

**Three of four.** Item 16 is the last one, and it is now smaller than it was —
the code already works against Keycloak, proven during the overnight session.
What remains is where to run it, how to back it up, and a checklist for
SURANTHER.

## What works end to end, on the backend

```
PR → RFQ → quotation → comparison → PO
   → gate entry → GRN → QC → AVAILABLE
   → MIR → issue → return → custody
   → vendor bill → landed cost → advance → payment
   → job order → fitment → Actual BOM → FAT
   → signed machine delivery → component ancestry dossier
```

Plus, proven this week and not on any earlier list:

| | |
|---|---|
| **Eleven concurrent users** | proven against real PostgreSQL |
| **Failure behaviour** | connection loss, restart, disk full, duplicate request, two API instances |
| **Automated backup** | with a tested restore |
| **Import purchase** | banker's rate per payment, bank advice mandatory |
| **Ten reports** | including the audit dossier |

## The honest percentages

There are three, and they answer different questions.

| Question | Answer |
|---|---|
| Does the core purchase-to-cost chain work? | **~90%** |
| Is everything we have specified built? | **20 of 50 items — 40%** |
| **Can SESS start using Purchase and Stores?** | **backend yes, once item 16 lands** |

The middle number is the one that has moved least, because the specification
kept growing — 20 items in August, 50 now. That is not drift; it is the
business being written down properly for the first time.

## What is NOT built

| Block | Contents | Engineering days |
|---|---|---|
| A | intercompany, stock adjustment, tools, vendor rating | 36-45 |
| B | dashboards (6 projections exist) | 15-20 |
| C | QC check sheets, calibration, job-work inspection | 49-61 |
| D | DC custody, customer property | 52-63 |
| E | cycle counting, scrap, job work, credit notes | 38-46 |
| F | document control (35-40 documents) | 20-25 |
| G | reorder levels, shelf life, purchase weighing | 13-16 |

**223-276 engineering days**, roughly 28-55 calendar days at the observed rate.

**None of it blocks 1 October.** All of it can be built while SESS is already
using Purchase and Stores.

## Modules not started

| | Status |
|---|---|
| **Sales** | fully specified, 35-45 days for stage 1 |
| **Service** | fully specified this week, not written up yet |
| **HR and attendance** | specified this week |
| **Project incentive** | specified this week |
| Accounts, Production, Design, Maintenance | not started |

## The real constraint

**The frontend.** 22 screens built, **7 proven end to end** as of 10 September.

That number is five days old and every defect behind it has been fixed. The
re-run is what ILAMPARUTHI is doing now.

**Backend days I can count. Frontend days I cannot, until the grid arrives.**

---

# PART 2 — WHAT CODEX DOES WHILE YOU ARE AWAY

## Timing

The instruction covers six items. At the rate Codex has sustained — five to
eight engineering days per calendar day — here is what three days buys:

| Item | Engineering days | Likely |
|---|---|---|
| Scale measurement | 2-3 | **day 1** |
| Item 16 Keycloak report + build | 5-7 | **days 1-2** |
| A1 intercompany | 8-10 | **days 2-3** |
| A2 stock adjustment | 6-8 | **day 3** |
| A3 tools | 10-12 | day 3 or beyond |
| A4 vendor rating | 12-15 | beyond |
| Item 35 correction | 1 | anywhere |
| REV869B removal | 1-2 | anywhere |
| Fitment-reversal-return proof | 1 | anywhere |

**Three days should complete: scale measurement, item 16, A1, A2, and the three
small items.** Tools and vendor rating will be in progress.

**That would make it four of four on go-live blockers**, with Block A half done.

## What you do from your phone

Twice a day, two minutes:

```powershell
cd C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet
git --no-pager log -5 --oneline
git --no-pager status --short | Select-Object -First 5
git push
```

**`git push` does not build and cannot disturb Codex.** Safe at any moment.

**Do not run build, test or migration while you are away.** Those need
attention you will not have at an expo.

**If Codex has stopped** — capacity errors happen — send this:

```
Continue. Do not stop at a verification checkpoint. Report when you commit, but
keep going.
```

---

# PART 3 — WHAT ILAMPARUTHI DOES

He returns tomorrow with SURANTHER and MAGESHWARI away. He is the only person
on the ERP for three days.

## Priority 1 — the grid, today or tomorrow

Twenty-two screens, one line each: does it run end to end, as the right person,
against the real API. Yes, no, or not yet run, with the blocking reason on
every no.

**This is the only number in the whole plan I cannot estimate.** Everything
else follows from it.

## Priority 2 — screens, in this order

**a. Opening stock ceremony**

The largest remaining go-live blocker on the frontend. Three stages against one
uploaded workbook: Stores counts, Accounts values, TD authorises. No one person
does two stages.

The screen **consumes** an import batch — it does not collect data. Pass
`ImportBatchId` and the transition `Version`.

Seven distinct refusals, each needing its own sentence on screen: movements
already exist, period already loaded, same employee twice, authority,
assignment, company, version, command context. "Refused" alone sends somebody
to the TD every time.

Each company is separate. Two imports, two ceremonies.

**b. Vendor advance and payment**

Backend is live. A payment may allocate across one or more accepted bills.

One detail: unambiguous day terms show a due date; **free-text milestone terms
deliberately show NO date**. The server refuses to invent one and the screen
must not either.

**c. Master data import**

Template download, upload, and the `errors.xlsx` round trip. The TD's team must
be able to download, fill, upload, see what failed, correct and re-upload
without asking anyone.

**d. The ten report screens**

All ten exist on the backend as of today. The last is the component ancestry
dossier — an auditor names a chamber and gets every component, its GRN, its
vendor, its accepted bill with allocated charges, its QC inspection and every
deviation approval.

## What he should NOT build

Dashboards, DC custody, QC check sheets, tool custody, calibration.

Some have no backend yet; others have projections but no agreed screen. Building
against an endpoint that does not exist wastes days we do not have.

## What he should know

**Authentication is changing to local Keycloak, not Cognito.** The login page
will change. Codex is building it while he works — he should not build against
a guess, and the contract will be sent when it lands.

**Migrations are clean now.** The thirteen guards that blocked migration at 84
are fixed and proven. He can migrate his local database freely.

---

# PART 4 — THE DATE

| | |
|---|---|
| **1 October** | Purchase and Stores, parallel run with BroPOS |
| Mid-October to mid-November | backend feature-complete against everything specified |
| December to January | frontend catches up |
| January to March 2027 | Sales |
| April to June 2027 | Service and HR |
| **September 2027** | full ERP |

**1 October holds if the grid comes back at fifteen or more.**

If it comes back at seven to ten, the sensible course is **Purchase only on 1
October**, with Stores following in mid-October. That is not a failure — it is
one department proving the system while the other finishes.

**That decision gets made on 25 September**, not on 30 September.

---

# PART 5 — WHAT TO DO FIRST WHEN YOU RETURN

1. Read Codex's reports — especially the **scale measurement seconds** and the
   **Keycloak answers**
2. Run the full witness: build, test, backup, migrate, provision, push
3. Read the grid
4. Decide: full go-live or Purchase-only on 1 October

**The scale numbers matter more than they look.** Eleven people on a laptop
with 300,000 items is the question nobody has answered yet, and the answer
decides whether the server purchase is urgent or planned.
