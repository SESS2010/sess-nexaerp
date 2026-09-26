# SESS NexaERP — Status and Day-by-Day Plan

> **Superseded on 23 September 2026.** Go-live moved from 1 October to **8 October 2026**
> by governed decision: setup 1-3 October, both opening-stock ceremonies 5-6 October,
> BroPOS frozen 7 October, daily transactions from 8 October. The amended schedule and the
> reason for it are recorded in
> [the go-live runbook](installation/go-live-fresh-database-runbook.md#governed-schedule-change-23-september-2026-go-live-moves-to-8-october).
> This document is kept as the dated record of what was verified on 19 September.
> Its body is deliberately unchanged; its dates are no longer instructions.

Date: 19 September 2026, 11:35
Verified from git and the live database, not estimated

---

## Verified correction — 19 September, 14:04

The 11:35 note below is retained as a historical planning record. Its completion,
live-database, percentage and schedule claims are not current acceptance evidence.
This run has not accessed or changed the owner database. Its proposed owner-database
upgrade is not an action authorized by the current task.

- Main is published through `4293aa4`; the latest accepted runtime foundation is
  `17366ee` (full Debug 922/922, Release 919/919, zero failed/skipped). Subsequent
  commits through `4293aa4` are documentation only.
- The twelve-finding code changes are pushed, including the exact −1,250 Actual BOM
  regression. Migration #2 still lacks the exact field pre-75 dump/globals witness;
  disposable-database tests do not establish a successful upgrade of that field state.
- Original authority diagnostics: 16 real (14 current item, 2 unpublished UOM draft),
  4 test-model mismatches and 4 seed/configuration dead ends. Correcting the model
  exposed two additional seed dead ends: the graph still fails with 22 diagnostics.
  See [the classification report](installation/reachability-authority-classification.md).
- **A1 is incomplete:** routes, buyer PO publication and GST invoice evidence exist;
  seller dispatch, destination acceptance and both complete ledgers remain.
- **A2 is incomplete:** approval/date/revision domain rules are published. Inventory
  periods and independent CFO MFA are under final-candidate acceptance; actual
  physical/FIFO adjustment posting remains unfinished.
- **A3 is incomplete:** `a88d4e2` corrects the specification. Individual-custody and
  opening-identity domain code is under candidate acceptance. Persisted custody,
  actual register import and operational resignation clearance remain unfinished.
- **A4 is incomplete:** `d46f47f` corrects the specification. Domain calculations,
  governed manual GRN assessments and quality/delivery source reads are under
  candidate acceptance. Remaining automatic source rules, aggregation, purchasing
  decisions and the actual 1,057-row LEGACY import remain unfinished.
- **Item 35 is a specification correction**, including any one of PM/QC/Design signing
  and shared rework responsibility. The operational fifteen-stage sheet is not built.
  The fitment/reversal/return witness is in the ordinary suite; REV869B retirement
  includes `ControlPlane.Tests`.

The current frozen combined candidate has a clean Debug build and 14/14 focused
Debug checks. Its full Debug suite is running; full Debug/Release acceptance is
not yet reported. Each subsequent item commit will state its actual scope and
identify any shared final-candidate suite evidence. Do not infer readiness or a
calendar delivery commitment from the historical percentages below.

## Historical 11:35 note

# PART 1 — WHERE IT STOOD IN THAT NOTE

## Measured this morning

| | |
|---|---|
| origin/main | `f94f332` |
| Nothing unpushed | 0 commits |
| Live database | **103 — eleven migrations behind** |
| feature/frontend | `bcfe80b`, unchanged since 18 September |
| Last full suite | 881 Debug, Release verification pending |

## Backend — what is complete

**All twelve findings from the grid walk are fixed and pushed.**

| # | | |
|---|---|---|
| 1 | CRLF in migration 89 | Windows upgrades work |
| 2 | Migration 75 superuser | REV869B upgrades work |
| 3 | Technical verification scope | quotation verification reachable |
| 4 | GRN 3-char category | items can be received |
| 5 | Item edit UOM precision | 1,388 of 1,388 editable |
| 6 | QC — three parts | **Stores no longer stops at GRN** |
| 7 | Grant gaps | vendor picker, job order list |
| 8 | GST rule prerequisite | documented |
| 9 | Serial and fitment read paths | scan-based return possible |
| 11 | Accounts GRN read | vendor bill screen possible |
| 12 | **Actual BOM valued at taxable** | **−1,250, exactly as predicted** |
| 14 | Master-data import 403 | opening stock and import unblocked |
| 15 | 42501 surfaced as 403 not 500 | |
| 16 | QC policy list carries item code | |

**Plus, beyond the findings:**

| Commit | |
|---|---|
| `8de95f0` | **A1 intercompany** — GST invoice evidence against issued buyer POs |
| `7421863` | fitted → reversed → returned witness, in the ordinary suite |
| `1d7945d` | **Item 35 corrected** — PM, QC and Design all sign; rework responsibility shared |
| `a88d4e2` | **A3 tools** — custody, damage replacement, clearance |
| `d46f47f` | **A4 vendor rating** — eight dimensions, LEGACY history |

**Codex is building A2 stock adjustment right now.** New files in the worktree:
`StockAdjustmentApprovalPolicy`, `StockAdjustmentApprovalSnapshot`,
`StockAdjustmentPostingDatePolicy`, `StockAdjustmentReview`.

## The #12 result is the one that matters

The frontend developer wrote a prediction down **before** the fix, with the
assumption it rested on:

```
before:    1,475 − 2,500 = −1,025     GST-inclusive, wrong
corrected: 1,250 − 2,500 = −1,250     ex-tax, correct
```

Codex's regression produced −1,250 exactly.

Along the way it refused something I told it to do. I said derive ITC
eligibility from the PO line's tax snapshot using registration type and
reverse-charge status. It refused — reverse-charge tax **can** qualify for
credit, so those are different facts. It added three states to the governed
snapshot instead: `FULLY_RECOVERABLE`, `BLOCKED`, `PARTIALLY_RECOVERABLE` with
a claimable percentage.

**That was correct and my instruction was wrong.**

## Backend percentage

| Question | Answer |
|---|---|
| Purchase and Stores core chain | **~95%** |
| Everything specified, all 50 items | **~50%** |
| **Blocking 1 October** | **one item — reachability diagnostics** |

## Frontend — what is complete

| | |
|---|---|
| **Grid** | **24 of 25** |
| Screens built | 25 |
| Proven end to end | 24 |
| Not proven | line 25, DC — **API-created, no screen, deliberate** |

**The frontend is not the constraint and has not been since 16 September.**

### What still has no screen

| | Backend ready? |
|---|---|
| Opening stock ceremony | yes — built, blocked by #14 until today |
| Vendor bill | yes — blocked by #11 until today |
| Vendor advance and payment | yes |
| Master data import round trip | yes — blocked by #14 until today |
| Delivery challan | yes, API only |

**Four screens.** All four were gated by backend fixes that landed yesterday.

---

# PART 2 — WHAT IS LEFT

## Blocking 1 October

| | Days |
|---|---|
| **Reachability — 22 authority-graph diagnostics** | 2-3 |
| Your database 103 → 114 | today |
| Four frontend screens | 8-12 |
| Master data load | 2-3 |
| Opening stock ceremony, for real | 2 |
| Training | 3 |
| Parallel run | 4 |

## Not blocking — build while SESS uses it

| Block | | Days |
|---|---|---|
| A2 | stock adjustment | **in progress** |
| B | dashboards | 15-20 |
| C | QC check sheets, calibration, job-work | 49-61 |
| D | DC custody, customer property | 52-63 |
| E | cycle counting, scrap, job work, credit notes | 38-46 |
| F | document control | 20-25 |
| G | reorder, shelf life, weighing | 13-16 |

**188-231 engineering days**, roughly 24-46 calendar days.

## Modules not started

| | Specified? | Days |
|---|---|---|
| Sales | **yes, fully** | 35-45 |
| Service | **yes, fully** | 45-60 |
| HR and attendance | **yes** | 25-35 |
| Project incentive | **yes** | 20-30 |
| Accounts, Production, Design, Maintenance | no | — |

---

# PART 3 — THE DAY-BY-DAY PLAN

## Today, Saturday 19 September

| Who | What |
|---|---|
| **You** | Codex is mid-test. When it finishes: stop, stash, build, test, backup, **migrate 103 → 114**, provision replay, stash pop, resume |
| **ILAMPARUTHI** | pull, migrate, then **the Actual BOM re-read first** — seeing −1,250 on screen is the confirmation that matters |
| **Codex** | finish A2, then the 22 reachability diagnostics |

**45-60 minutes for your part.**

## Sunday 20 September

| Who | What |
|---|---|
| Codex | reachability diagnostics — classify all 22: real gap, test wrong, or seed |
| ILAMPARUTHI | line 24 opening stock; re-run 7 to 12 |

**The 22 diagnostics are the last backend item before go-live.** Do not let
them be weakened to pass — if a check is wrong, it must be named and explained.

## Monday 21 – Tuesday 22 September

| Who | What |
|---|---|
| Codex | fix whatever the 22 turn out to be; then A2 witness; then Block B dashboards |
| ILAMPARUTHI | **vendor bill screen**, then vendor advance and payment |
| You | witness, migrate, push each day |

## Wednesday 23 – Thursday 24 September

| Who | What |
|---|---|
| ILAMPARUTHI | opening stock ceremony screen; master data import round trip |
| **You** | **load master data** — 91 vendors, 143 customers, 1,388 items |
| SURANTHER | Keycloak install, from Codex's checklist |

**Master data loading will expose problems in the spreadsheets.** Expect a day
of correction. Better now than on 30 September.

## Friday 25 September — the gate

**Decide: full go-live or Purchase-only.**

| If | Then |
|---|---|
| four screens done, reachability clean, master data loaded | **full go-live 1 October** |
| screens incomplete | **Purchase only 1 October**, Stores mid-October |

**On today's evidence I expect full go-live.** The frontend is at 24 of 25 and
the four remaining screens were gated by fixes that landed yesterday.

## Saturday 26 – Sunday 27 September

| Who | What |
|---|---|
| **You** | **opening stock, for real** — three actors: Stores counts, Accounts values, TD authorises |
| | Both companies. Two imports, two ceremonies. |
| | SESS_PROPRIETORSHIP first — it is the only company with zero movements |

## Monday 28 – Tuesday 29 September

**Training. Two days, on the real system with real data.**

| Day | Who |
|---|---|
| 28 | PRIYA — requisition, RFQ, quotation, comparison, purchase order |
| 28 | SUDALAI, KAMALI, KARTHICK — gate entry, GRN, stock check, issue, return |
| 29 | NARREN — QC queue, inspection, partial acceptance, concession |
| 29 | SARATH, ALFATHIMA — approvals, vendor bill, payment |

## Wednesday 30 September

| Who | What |
|---|---|
| You | final backup, both database and globals |
| You | freeze BroPOS for new transactions |
| Codex | **nothing** — do not change the system the day before go-live |

## Thursday 1 October — go-live

Purchase and Stores on NexaERP. BroPOS in parallel for reference.

---

# PART 4 — AFTER 1 OCTOBER

| Period | |
|---|---|
| October – November | Blocks B to G — build while SESS uses it |
| December – March | **Sales** — fully specified, 35-45 days |
| April – June | **Service and HR** — fully specified |
| July – September | Accounts, Production, Design |
| **From April 2027** | **six months of your own use complete — begin selling** |

**Running it yourself for six months before selling is the right decision.**
Every problem surfaces on your data rather than a customer's, and the
installer, backup and upgrade paths get tested by real use rather than by
intention.

---

# PART 5 — THE THREE RISKS

**1. Your database is eleven migrations behind.** Everything Codex proved this
week is proven against a database further ahead than yours. Fix today.

**2. The frontend has not moved since 18 September.** Four screens in the days
remaining, by one person. That is the tight part of the plan, not the backend.

**3. The 22 reachability diagnostics are unclassified.** Until Codex says how
many are real gaps versus test modelling, we do not know whether they are two
days or five.

---

# PART 6 — WHAT ONLY YOU CAN DO

| Date | |
|---|---|
| Today | migrate 103 → 114 |
| 23-24 Sep | load master data |
| **25 Sep** | **the go-live decision** |
| 26-27 Sep | authorise opening stock — nobody else may |
| 28-29 Sep | train the team |
| 30 Sep | final backup, freeze BroPOS |

The opening stock authorisation is yours by the frozen rule. Stores counts,
Accounts values, **the Technical Director authorises** — and no one person may
do two stages.
