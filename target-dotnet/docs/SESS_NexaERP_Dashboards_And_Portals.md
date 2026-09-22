# SESS NexaERP — Dashboards, Notifications and Portals

Date: 10 September 2026
Companion to `SESS_NexaERP_Pending_Work_Specification.md` — items 29 to 34

---

## The principle

Do not build generic ERP dashboards. Build the numbers that answer the
questions SESS actually asks, and nothing else.

Every tile below exists because someone at SESS has asked that question during
this project. If a tile cannot be traced to a real question, it does not belong
on the screen.

**Backend builds the projection. Frontend builds the tile.** A projection is
proven when it returns correct numbers from the end-to-end witness database,
which walks the full chain from PR to Actual BOM. "It renders" is not proof.

---

## 29. PURCHASE DASHBOARD

**Who opens it:** PRIYA (Purchase Manager), Technical Director, Managing
Director.

**The question it answers:** what is stuck, what is owed, and what did we
spend.

### Section 1 — What is waiting for someone

| Tile | Number | Why it matters |
|---|---|---|
| Requisitions awaiting department verification | count, oldest age in days | The verify step stalls silently; nobody is told |
| Requisitions awaiting approval, by band | count and value per band | Three bands: below ₹5,000, ₹5,000–₹1,00,000, above. The TD and MD need to see their own queue |
| Requisitions awaiting stock check | count, oldest age | SUDALAI and KARTHICK act here; today they learn by being told |
| RFQs issued with no quotation | count, oldest age, vendor names | A vendor who never quotes stalls a chamber |
| Quotations awaiting technical verification | count, oldest age | TECHNICAL_ENGINEER or TD acts |
| Comparisons awaiting recommendation or approval | count, value | |
| POs approved but not issued | count, value | Approved and forgotten is the worst state |

**Each tile must be clickable to the underlying list, filtered.** A number
nobody can act on is decoration.

### Section 2 — What is owed and owing

| Tile | Number |
|---|---|
| Open POs — issued, not fully received | count, value, oldest |
| Goods received not billed (GRNI) | count, value, oldest |
| Billed not received | count, value |
| Vendor advances outstanding | count, value, per vendor |
| POs overdue against committed delivery date | count, value, days late, vendor |

The last one is the one PRIYA will use every morning.

### Section 3 — What we spent

| Tile | Number |
|---|---|
| Purchase value this month, this quarter, this financial year | by company |
| Purchase value by vendor, top ten | with count of POs |
| Purchase value by item category | |
| Month-on-month trend, twelve months | line |

**Financial year is April to March.** Not calendar.

### Section 4 — Vendor quality

Only once vendor rating exists (spec item 23).

| Tile | Number |
|---|---|
| Vendors by band | Excellent / Good / Acceptable / Poor counts |
| Vendors below 70 per cent | list — these cannot receive a PO without TD and MD |
| Vendors in revaluation | count, days since improvement letter |
| Rejection rate this quarter | percentage, trend |

Until three receipts exist per vendor, show "provisional" rather than a score.
Do not average two data points and present it as a rating.

---

## 30. STORES DASHBOARD

**Who opens it:** SUDALAI, KARTHICK, KAMALI, Stores Manager, Technical
Director.

**The question it answers:** what is here, what is stuck, and what has left
and not come back.

### Section 1 — What is stuck

| Tile | Number | Why |
|---|---|---|
| Gate entries not converted to GRN | count, oldest age | Material sitting at the gate |
| GRN lines in QC_HOLD | count, value, **overdue against the two-day limit** | The frozen QC ageing rule |
| QC rejections awaiting returnable DC | count, value | Rejected material with nowhere to go |
| Stock awaiting put-away | count | Accepted but not on a rack |
| MIRs awaiting approval | count, oldest, by approver | |
| Approved MIRs not yet issued | count, oldest | Production waiting on Stores |

### Section 2 — What has left and not come back

**This is the section that matters most.** It is the problem raised on
3 September: material goes out with an engineer, he moves site, nobody can find
it.

| Tile | Number |
|---|---|
| **Outstanding engineer custody** | count, value, **by engineer** |
| Custody past the one-day return expectation | count, by engineer, days |
| Returnable DCs overdue | count, value, customer, days |
| Tools not returned | count, by engineer, days |
| Unidentified returns awaiting matching | count |
| Handoffs awaiting acceptance | count, from whom to whom, days |
| Shortfalls awaiting explanation | count, by engineer, days |

**By engineer, always.** A total tells nobody what to do. A name does.

### Section 3 — What is here

| Tile | Number |
|---|---|
| Stock value by company | FIFO, landed |
| Stock value by warehouse | Old Factory, New Factory, QC, customer property |
| Ageing buckets | 0–30, 31–90, 91–180, 181–365, over 365 days |
| Non-moving stock — no movement in 180 days | count, value |
| Items below reorder level | count, list |
| Serialized items in stock | count |
| Customer property held | count, value, **oldest, and whose** |

Customer property value is shown but **never included in SESS stock value**.
Label it clearly on the tile — an accountant reading the wrong number once is
enough.

### Section 4 — Accuracy

| Tile | Number |
|---|---|
| Last cycle count by class | A, B, C — date and variance |
| Adjustments this month | count, value, by approver |
| Count variance trend | six months |

Only once cycle counting exists (spec item 18).

---

## 31. JOB ORDER AND COST DASHBOARD

**Who opens it:** Technical Director, Production Manager, Managing Director.

**The question it answers:** what did we quote, what did it cost, and where is
each machine.

| Tile | Number |
|---|---|
| Job orders by state | pending Accounts, open, in production, FAT ready, dispatched |
| Job orders awaiting Accounts confirmation | count, days — these block the BOM |
| Estimated BOMs awaiting approval | count, oldest |
| Production BOMs awaiting approval or pinning | count |
| **FAT readiness** | ready / blocked / not reconciled, with the blocked ones named |
| **Offer versus actual, per machine** | estimated value, actual value, variance, percentage |
| Machines where actual exceeds estimate by more than 10 per cent | list — configurable threshold |
| Actual BOM lines still PROVISIONAL_UNBILLED | count, value, per machine |

**The offer-versus-actual tile is the reason this ERP exists.** It should be
the largest thing on the screen.

The last tile matters because a machine can be delivered before its bills are
accepted, and its cost is not final until they are. Show which machines have an
incomplete cost picture rather than presenting a provisional number as final.

---

## 32. TECHNICAL DIRECTOR AND MANAGING DIRECTOR VIEW

Not a separate dashboard — a filtered view of the three above, showing only
what needs their decision.

| Tile |
|---|
| Approvals waiting on me, by document type, with age |
| Concessions awaiting my decision |
| Customer-facing excess issues awaiting my decision |
| Vendors below 70 per cent needing TD and MD concurrence |
| Write-offs, scrap disposals and adjustments awaiting me |
| Deviations above threshold, by engineer |
| Service Manager waivers this month — **and how many** |

The last one exists because a Service Manager who waives everything makes the
deviation system meaningless. That number is watched.

---

## 33. NOTIFICATION PAGE

The in-app notification engine exists (`8838f50`). The page does not.

**Every employee, their own list only.**

| Requirement |
|---|
| Unread count in the header, visible on every screen |
| List: newest first, unread distinct from read |
| Each notification names the document and links to it |
| Mark read — one way, never unread |
| Filter by type and by unread |
| A notification whose underlying condition has cleared stops counting as unread |

### Notification types, and who receives each

| Type | Recipient | Timer |
|---|---|---|
| Approval waiting | the mapped approver | immediate, escalating by age |
| Material not returned same day | the custodian | after one day |
| Custody handoff awaiting acceptance | the receiving engineer | immediate, then Service Manager after one day |
| Shortfall awaiting explanation | the sending engineer | after one day, then Service Manager |
| QC ageing | QC Manager | after two days from receipt |
| Returnable DC overdue | Stores, then TD and MD | on the due date |
| Calibration due | QC Manager | before the due date |
| Tool overdue | the custodian, then Stores Manager | on the due date |
| Vendor below 70 per cent | Purchase Manager, TD, MD | on the rating change |
| Deviation threshold reached | Service Manager, then TD | at 3 and 5 per month |
| BOM excess on internal issue | Production Manager | at issue |
| Customer-facing excess | Technical Director | at request |
| FAT blocked by unexplained custody | QC Manager, Production Manager | on reconciliation |

**Escalation is by role mapping, never by hard-coded employee.** When SARATH
leaves, the notifications must follow the role.

---

## 34. ENGINEER PORTAL

Specified in `SESS_NexaERP_DC_Custody_Specification.md`. Every service and
production engineer signs in and sees only their own.

| Section | Content |
|---|---|
| My sites | assigned sites and job orders |
| My tasks | assigned, completed, pending |
| **My custody** | every item I hold, by DC, with expected return date |
| Handoffs waiting for me | line by line, accept or mark missing |
| Shortfalls I must explain | with the quantity and the deadline |
| My returns | declared, accepted, rejected |
| **My deviation count** | this month, this quarter, with reasons |

**The engineer must never have to ask Stores what he is holding.** That single
requirement is why this portal exists.

Showing an engineer his own deviation count is deliberate. A person who can see
their own record corrects it. A person who cannot, does not know there is
anything to correct.

---

## Acceptance criteria for all six items

**A projection is done when:**

1. It returns correct numbers from the **end-to-end witness database** — the
   disposable cluster that walks PR through Actual BOM. Not from a hand-built
   fixture.
2. It is **company-scoped**. A user sees one company at a time and the number
   changes when they switch.
3. It is **permission-gated**. A tile a role may not act on is not returned to
   that role at all — not returned and hidden.
4. It respects **operational scope**. A Stores executive sees Stores numbers.
5. It answers in **one round trip**. A dashboard that issues forty queries will
   be abandoned.
6. Every count has a **drill-through** to the filtered list behind it.

**Must refuse:**

- returning another company's numbers
- returning a tile the caller cannot act on
- including customer property in SESS stock value
- presenting a provisional cost as final
- averaging fewer than three receipts into a vendor rating

**Report:**

- which tiles cannot be built yet, and what each waits for
- the query cost of the heaviest tile at the volumes in spec item 27 — the
  stock balance tile aggregates movements and will degrade first
- whether any tile needs a materialised view or a cache, and what would
  invalidate it

---

## What NOT to build

- pie charts of anything
- a tile showing a number with no action behind it
- "total transactions" or any count that measures activity rather than a
  decision
- year-on-year comparisons before there is a year of data
- a rating, score or index that is not defined in a frozen decision

If a tile cannot be traced to a question someone at SESS has actually asked, it
does not belong on the screen.
