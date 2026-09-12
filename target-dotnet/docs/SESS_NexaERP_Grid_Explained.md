# The Grid — what it is and why it decides 1 October

Date: 12 September 2026
For: A. Paramananthan, Technical Director

---

## The one question

For every screen, one question:

> **Can a real person, signed in as themselves, finish the job on this screen
> against the real API?**

Yes or no. Nothing in between.

---

## What "yes" means

All four must be true:

1. The screen **loads** with its data, not an empty table
2. The person can **complete the whole task** — create, submit, approve,
   whatever that screen is for
3. Done as the **right person** — PRIYA raises the requisition, ALFATHIMA
   approves it. Not everything as SESS-01.
4. Against the **real API and real database**. Not mock data.

## What "yes" does NOT mean

| Not enough | Why |
|---|---|
| The screen renders | A blank table renders |
| The buttons appear | They may all return 403 |
| It worked last week | The backend has moved 39 commits |
| SESS-01 could do it | SESS-01 can do everything. Nobody else can. |
| The API works | That is a different test, already passed |

---

## The format

| Screen | End to end | Failing step |
|---|---|---|
| Purchase Requisition | YES | |
| Stock Check | NO | old endpoint, rewire in progress |
| RFQ | NO | detail page cuts the connection |
| Gate Entry | NOT YET RUN | no issued PO exists to receive |

Three values only: **YES**, **NO**, **NOT YET RUN**.

"NOT YET RUN" is honest and useful. "Probably fine" is neither.

For every NO, one line naming **the step that failed** — not a guess at the
cause. "Approve returns 403 as SESS-25" is a finding. "Permissions issue" is
not.

---

## The twenty-two screens

| # | Screen | Who should run it |
|---|---|---|
| 1 | Employee master | SESS-12 |
| 2 | Vendor master | SESS-15 |
| 3 | Customer master | SESS-15 |
| 4 | Item master | SESS-15 or SESS-35 |
| 5 | Customer PO | SESS-14 |
| 6 | Purchase Requisition | SESS-15 raises, SESS-14 approves |
| 7 | Stock Check | SESS-35 |
| 8 | RFQ | SESS-15 |
| 9 | Quotation | SESS-15 |
| 10 | Comparison | SESS-15 raises, SESS-01 approves |
| 11 | Purchase Order | SESS-15 raises, SESS-01 approves |
| 12 | Gate Entry | SESS-35 or SESS-16 |
| 13 | GRN | SESS-35 |
| 14 | QC queue | SESS-33 |
| 15 | QC inspection | SESS-33 |
| 16 | Concession | SESS-33 raises, SESS-01 approves |
| 17 | MIR | SESS-25 raises, SESS-25 approves |
| 18 | Issue by scan | SESS-35 |
| 19 | Material Return | custodian declares, SESS-35 accepts |
| 20 | Job Order | SESS-25 initiates, SESS-14 confirms |
| 21 | Estimated / Production BOM | SESS-17 or SESS-19, SESS-01 approves |
| 22 | Fitment and FAT | SESS-25 fits, SESS-33 reconciles |

**The "who" column is the point.** A screen that only works as the Technical
Director is a screen that will fail on the first Monday, because the Technical
Director is not the person who uses it.

---

## Why this decides 1 October

Nineteen days remain. Two numbers matter.

**Backend days I can count.** Opening stock 5-7, reports 10-12, authentication
5-7. Codex sustains four to six engineering days per calendar day. That
arithmetic works.

**Frontend days I cannot count** — because I do not know where it starts from.

| If the grid says | Then |
|---|---|
| **15 or more YES** | Seven screens to fix plus four new ones. Tight but real. **1 October holds.** |
| **10 to 14 YES** | Twelve screens in eighteen days by one person. **Purchase only on 1 October**, Stores mid-October. |
| **Still 7** | The 10 September number after nine defect fixes means something structural. **Replan, not push harder.** |

**This is the only number in the whole plan I cannot estimate.** Everything
else I can work out from what is built and how fast it is being built.

---

## The last grid, 10 September

| | |
|---|---|
| Screens built | 22 |
| **Proven end to end** | **7** |
| Blocked by one defect | 10 |
| Masters that render only | 4 |
| Unverified | 1 |

The one defect blocking ten screens was vendor qualification being unreachable
for every login — zero qualifications could exist, so RFQ vendor-candidates was
always empty, so no PO could be raised, so nothing downstream could be tested.

**That defect is fixed.** So are the other eight he found. The re-run against
`69619be` is what the grid measures.

---

## What has changed since 10 September

Nine defects closed. Four no-exit states fixed. The ACL defect that would have
killed authentication at every customer site. 39 backend commits.

**Any remaining NO is therefore a NEW finding**, not a known one. That is why
the re-run is worth a day of his time rather than an educated guess.

---

## What I do with it

**On the day it arrives**, the day-by-day plan gets rewritten around it, and
one of three things happens:

- 15+ → the plan stands, and I confirm 1 October to the Managing Director
- 10-14 → Purchase starts 1 October, Stores follows mid-October, and I say so
  now rather than on 30 September
- 7 → we stop and find out why nine fixes moved nothing

**Waiting until 30 September to discover which of these is true would be the
expensive mistake.** Nineteen days is enough to change course. Two days is not.

---

## The discipline underneath this

He has refused twice to fill the grid from memory. He was right both times.

A number somebody remembers is worth nothing. A number somebody re-proved this
morning, as the right person, against the real API, is worth the day it took.

Every real defect on this project came from that discipline — his, and
Codex's refusal to report a test count while the model was dirty. Neither was
found by the 787 passing tests.
