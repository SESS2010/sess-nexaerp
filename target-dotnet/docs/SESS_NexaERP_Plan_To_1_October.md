# SESS NexaERP — Day-by-Day Plan to 1 October

Date: 12 September 2026
Target: SESS begins using NexaERP on 1 October 2026, in parallel with BroPOS
Prepared for: A. Paramananthan, Technical Director

**19 days remain.**

---

## Where we stand today — verified

| | |
|---|---|
| origin/main | `b669fb0` |
| Tests | 787 Debug, 784 Release, **zero failures** |
| Tables | 215 |
| Migrations | 76 |
| **Specification items acceptance-complete** | **13 of 50 — 26%** |
| Partial or foundation | 19 |
| Not started | 18 |
| Frontend screens built | 22 |
| **Frontend screens proven end to end** | **7** |

The chain from purchase requisition to a machine's Actual BOM works and is
proven against real PostgreSQL. What is missing is everything around it that
makes it usable by eleven people on a Monday morning.

---

## What still blocks 1 October

Five things. Nothing else on the 50-item list blocks the first day.

| # | Item | Why it blocks | Engineering days |
|---|---|---|---|
| 1 | **Import adapters** | 392 stock rows and 1,388 items cannot be loaded except by direct SQL. And the template defines what SESS fills in. | 3-4 |
| 2 | **Opening stock** | Without it the ERP starts at zero and every issue fails | 5-7 |
| 3 | **Authentication** | Nobody can sign in for real. Dev tokens are Debug-only. | 5-7 |
| 4 | **Core reports** | Stores and Purchase cannot answer a question without them | 10-12 |
| 5 | **Frontend screens** | 22 built, 7 proven. Advance, payment, opening stock, reports have no screen. | 25-35 |

Import purchase (item 11) is decided but does not block the first day — SESS can
raise a rupee PO on 1 October and add the foreign-currency path in October.

---

# THE DAY-BY-DAY PLAN

## Friday 12 September — today

| Who | What |
|---|---|
| **Codex** | Item 14 import adapters, starting with the templates |
| **ILAMPARUTHI** | **Send the grid.** It is overdue and it is the only unknown left. |
| **You** | Run `database-principals provision` so the API runs as `nexa_erp_runtime` |
| **You** | Correct the negative stock row in BroPOS at source |

The provisioning takes fifteen minutes with a backup first. Until it is done
you cannot see a screen work, and neither can anyone else on your machine.

## Saturday 13 – Sunday 14 September

| Who | What |
|---|---|
| **Codex** | Finish item 14. Every adapter offers a downloadable template with exact columns, example rows and a validation sheet. |
| **Codex** | Start item 13 opening stock |
| **You** | Walk the chain yourself on two or three items — PR, approve, RFQ, PO, gate entry, GRN, QC, and see it in stock |
| **ILAMPARUTHI** | Screens for vendor advance and payment |

**Your walkthrough on Saturday matters more than anything Codex builds that
day.** If something is wrong in the flow, better to find it on the 13th than
the 30th.

## Monday 15 – Tuesday 16 September

| Who | What |
|---|---|
| **Codex** | Finish item 13 opening stock — three actors, controlled posting, immutable |
| **Codex** | Item 16 authentication report, then build the path SURANTHER has prepared |
| **You** | Download the item and opening-stock templates, hand them to SUDALAI and KARTHICK |
| **SUDALAI, KARTHICK** | Fill the item template — 1,388 rows, and the rack for each |
| **ALFATHIMA** | Fill the rate column on the opening-stock template |
| **ILAMPARUTHI** | Opening stock screens — the three-actor ceremony |

The rack is the column your old system does not hold. Nobody else can supply
it. That is two days of work for the stores team and it cannot start until the
template exists.

## Wednesday 17 – Thursday 18 September

| Who | What |
|---|---|
| **Codex** | Item 15 reports — stock balance, movement, FIFO valuation, GRNI, purchase register |
| **You + ILAMPARUTHI** | Load master data: 91 vendors, 143 customers, 1,388 items |
| **ILAMPARUTHI** | Report screens |

Master data loading will expose problems in the spreadsheets. Expect a day of
correction. That is normal and better now than on 30 September.

## Friday 19 – Saturday 20 September

| Who | What |
|---|---|
| **Codex** | Finish item 15. Then component ancestry — the audit dossier. |
| **Codex** | Item 25 concurrency — eleven people at once has never been tested |
| **You** | **Load opening stock** through the three-actor ceremony |
| **ILAMPARUTHI** | Finish the remaining screens |

**Opening stock loads on the 20th.** From that moment the ERP holds real
balances and every subsequent test is against real data.

## Sunday 21 – Monday 22 September

| Who | What |
|---|---|
| **Codex** | Item 26 failure behaviour — connection loss, restart, duplicate request |
| **Codex** | Automated backup — every backup so far has been typed by hand |
| **You** | **Full rehearsal**: raise a real PR, run it to a real PO, receive it, inspect it, issue it |
| **ILAMPARUTHI** | Fix whatever the rehearsal finds |

## Tuesday 23 – Thursday 25 September

**Training. Three days.**

| Day | Who |
|---|---|
| 23 | PRIYA — requisition, RFQ, quotation, comparison, purchase order |
| 24 | SUDALAI, KAMALI, KARTHICK — gate entry, GRN, stock check, issue, return |
| 24 | NARREN — QC queue, inspection, partial acceptance, concession |
| 25 | SARATH, ALFATHIMA — approvals, vendor bill, payment |
| 25 | Service engineers — custody, return |

You said you will train the team. Three days is what it needs, and it must be
on the real system with real data — not slides.

## Friday 26 – Monday 29 September

| Who | What |
|---|---|
| **Everyone** | **Parallel run.** Every transaction entered in both systems. |
| **You** | Compare the two at the end of each day. Where they disagree, find out why. |
| **Codex** | Fix what the parallel run finds — nothing new |
| **ILAMPARUTHI** | Same |

**These four days decide whether 1 October is real.** If the two systems agree
on 29 September, go. If they do not, the disagreement is telling you something
and it is better heard then.

## Tuesday 30 September

| Who | What |
|---|---|
| **You** | Final backup, both database and globals |
| **You** | Freeze BroPOS entry for new transactions |
| **Codex** | Nothing. Stop changing the system the day before go-live. |

## Wednesday 1 October — go-live

Purchase and Stores on NexaERP. BroPOS continues in parallel for reference.

---

# WHAT CODEX BUILDS, IN ORDER

| Day | Item | Days |
|---|---|---|
| 12-14 Sep | **14 — import adapters and templates** | 3-4 |
| 14-16 Sep | **13 — opening stock** | 5-7 |
| 16-18 Sep | **16 — authentication** | 5-7 |
| 17-20 Sep | **15 — the ten reports** | 10-12 |
| 20-21 Sep | 25 — concurrency, eleven users | 4-6 |
| 21-22 Sep | 26 — failure behaviour, automated backup | 4-6 |
| 22-25 Sep | 11 — import purchase | 4-6 |
| 26-30 Sep | Only what the parallel run finds | — |

**35-48 engineering days.** At the observed rate of four to six per calendar
day, that is 7 to 11 calendar days of work spread across 18. It fits, with
room for what goes wrong.

---

# WHAT ILAMPARUTHI BUILDS

This is the constraint, not Codex.

| Priority | Screens | Days |
|---|---|---|
| 1 | Vendor advance, vendor payment | 3-4 |
| 2 | Opening stock — the three-actor ceremony | 3-4 |
| 3 | The ten reports | 8-10 |
| 4 | Whatever the grid shows as unproven | ? |
| 5 | Parallel-run fixes | 3-5 |

**17-23 days for one person in 18 calendar days.** That is tight and it has no
slack.

**The grid decides whether this is possible.** Seven proven out of twenty-two
on 10 September. If it is now fifteen, this plan holds. If it is still seven,
1 October needs to become a Purchase-only start with Stores following in
mid-October.

---

# WHAT ONLY YOU CAN DO

| Date | What |
|---|---|
| 12 Sep | Provision the runtime principals |
| 13 Sep | Walk the chain yourself and tell me what is wrong |
| 15-16 Sep | Get the item and rack templates filled |
| 17-18 Sep | Load master data |
| 20 Sep | Load opening stock and authorise it |
| 22 Sep | Full rehearsal |
| 23-25 Sep | Train the team |
| 26-29 Sep | Run the comparison each evening |

**Nobody else can do any of these.** The rack information exists only in your
team's heads. The authorisation is yours by the frozen rule. The training is
yours because you know what the ERP is for.

---

# THE HONEST RISKS

**1. The frontend has no slack.** One developer, 17-23 days of work, 18 days
available. Any illness, any surprise, and screens slip.

**2. The grid is still unknown.** Seven proven is a ten-day-old number. It
could be fifteen by now, or it could still be seven.

**3. Nothing has been tested with eleven people at once.** Every test so far is
one user at a time. That is item 25 and it is scheduled for the 20th — late,
but before training.

**4. The server is your laptop.** Eleven people on one machine for a month has
never been tried. If it struggles, AWS or a new PC becomes urgent rather than
planned.

**5. Master data will have problems.** The vendor and customer spreadsheets
have never been through the import framework. Expect a day of correction.

---

# THE FALLBACK, IF IT IS NEEDED

If on 25 September the screens are not ready:

**Start Purchase only on 1 October.** PRIYA raises requisitions, runs RFQs and
issues POs in NexaERP. Stores continues in BroPOS until mid-October.

That is not a failure. It is one department proving the system while the other
finishes. It also means the material arriving in October already has a real PO
behind it when Stores does switch.

**Decide this on 25 September, not on 30 September.**

---

# THE SINGLE MOST USEFUL THING TODAY

**Ask ILAMPARUTHI for the grid.**

Everything in this plan assumes the frontend is roughly two-thirds proven. If
it is not, the plan changes shape and it is better to know on the 12th than the
25th.

Second: provision the runtime principals on your machine. Until that is done
you cannot see a single screen work, and your Saturday walkthrough — the most
valuable check in this whole plan — cannot happen.
