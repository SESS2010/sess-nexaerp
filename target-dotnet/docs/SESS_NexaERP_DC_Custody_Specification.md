# SESS NexaERP — Delivery Challan Custody and Engineer Accountability

Date: 3 September 2026
Decided by: A. Paramananthan, Technical Director
Status: Specification. Schema exists in Foundation 2. Behaviour not yet built.

---

## 1. The problem this solves

Material goes out on a returnable DC with an engineer. The engineer moves to
another site. The DC does not move with him. Weeks later Stores asks where the
material is and nobody can answer — the first engineer says he handed over, the
second says he never received anything, and the paper copy is lost.

Material comes back without its DC. Stores cannot match it to anything.

Nothing is recorded, so nothing can be traced and nobody is accountable.

**Custody must follow the material, not the paper, and every change of hands
must be recorded by both people.**

---

## 2. Three kinds of returnable DC

They look alike and behave differently. The type is chosen when the DC is
raised and cannot be changed afterwards.

| | Consumable | Tool | Machine |
|---|---|---|---|
| Example | Cutting wheel, grinding wheel, welding rod | SESS-owned tools | Chamber for trial or rental |
| Customer signature | No — gate entry only | No | **Mandatory at delivery** |
| Expected return | Unused portion only | **100%, always** | **100%, always** |
| Consumption allowed | Yes, explained at return | **Never** | Never |
| Commercial cover | Lot amount in the offer and customer PO | None | Rental period in the customer PO |
| Return period | Site work duration | Site work duration | **From the customer PO** |

### Consumable

Ten cutting wheels go out. Five are used, five come back. The engineer explains
the consumption at return. There is no tolerance figure — the offer carried a
lot amount for consumables and the customer PO covers it. What matters is that
somebody said what happened to each one.

### Tool

A SESS asset. It comes back, all of it. A tool that does not return is a
**loss**, never a consumption. It follows the write-off path with Technical
Director approval, not the consumable explanation path.

### Machine

Goes out with a customer signature on the DC at the security gate. A duplicate
copy comes back to Stores immediately as delivery evidence. When the machine is
collected, the same DC is shown at the customer gate to take it out.

The DC closes only when the machine is physically back.

---

## 3. Custody follows the material

**Custody belongs to the person who took it from Stores.** It stays with them
until somebody else accepts it.

A DC references a site or job order for ownership, and an engineer for custody.
The engineer changes; the DC does not need reissuing.

---

## 4. Handoff — the loop that fixes this

```
Stores issues DC
        │
        ▼
Engineer A's portal — notification
        │
        ▼
A accepts                      custody: A
        │
   A moves site
        │
        ▼
A starts handoff to B
        │
        ▼
Engineer B's portal — notification, same soft copy
        │
        ▼
B accepts LINE BY LINE
   "8 present"  ✅        "2 missing"  ❌
        │
        ├──── 8 items:  custody moves to B
        │
        ▼
   2 items:  A must explain
        │
        ▼
Stores reviews the explanation
        │
   ┌────┴────┐
Accepted   Not accepted
   │            │
 closed    DEVIATION on A
```

### What each person is responsible for

After a partial acceptance, **B is accountable for eight items and A for two**.
Both positions are explicit and recorded. Neither can later claim the other
holds it.

That is the whole point. Today the argument has no evidence on either side.

### The soft copy

No scanning at handoff. Both engineers see the **same DC in the ERP** and mark
quantities on it. A can see what he consumed and what he passed on. B marks
what he actually received.

Scanning belongs at Stores receipt, where barcodes exist. In the field, on a
phone, marking a quantity is what works.

### Stores is the referee

Not the engineers, not the Service Manager. Stores accepts or rejects the
explanation. They issued the material and they take it back.

---

## 5. Timing — one day

**B must accept on the day he takes charge of the site work.** A must explain
any shortfall the same day.

The notification appears on the assignment day itself, not later.

| Who is late | What happens |
|---|---|
| B has not accepted after one day | Reminders continue to B. Service Manager notified. |
| A has not explained after one day | Reminders continue to A. Service Manager notified. |
| Service Manager does not respond | **The defect falls on the Service Manager.** |

Nobody escapes by staying silent. That is deliberate — silence is the failure
mode this whole design exists to close.

Custody does not move until B accepts. If B never accepts, the material stays
with A and A remains accountable.

---

## 6. Value limits for accepting a shortfall

| Value | Who may accept |
|---|---|
| Up to ₹5,000 | Service Manager |
| Above ₹5,000 | Technical Director |

Same first band as the purchase approval matrix. Configurable by TD, MD or
IT_MANAGER with change history, like every other configured value.

---

## 7. Deviation recording

A deviation is recorded automatically. Nobody types one in.

| Deviation | Raised when |
|---|---|
| Return without a DC | Stores receives material with no DC reference |
| Handoff not performed | Engineer left the site with custody unmoved |
| Acceptance overdue | B did not accept within one day |
| Explanation overdue | A did not explain within one day |
| Consumption unexplained | Consumable issued, no account given at return |
| Tool not returned | A SESS asset is missing |
| Service Manager unresponsive | Escalation ignored |

### Material without a DC is still accepted

Stores takes the material in. It must never stand outside the gate.

But it is recorded as an **unidentified return** and stays open until somebody
matches it to a DC. The deviation is raised against the engineer who should
have brought the paperwork.

---

## 8. Pattern detection — the real purpose

One shortfall is an accident. Every month is a pattern.

| Deviations in a month | Then |
|---|---|
| 3 | Service Manager notified |
| 5 | Technical Director notified |
| 3 or more for three consecutive months | Performance review |

**These figures are configuration, not code.** They are a starting point and
will change after a trial period. TD, MD and IT_MANAGER may edit them, with the
same change history as every other configured value.

---

## 9. Waiver — and why it is watched

An engineer diverted to an emergency has no time to hand over. That is not his
failure and the system must not treat it as one.

**The Service Manager may waive a deviation**, with a written reason. The
waived deviation leaves the engineer's count.

**Waivers are counted against the Service Manager.**

Without that, one of two things happens. Either the Service Manager waives
everything, because it is easier, and the system means nothing. Or he waives
nothing, to stay safe, and engineers stop trusting it — and an engineer who
does not trust the system stops drawing material, which stops the work.

TD and MD see both numbers: deviations per engineer, and waivers per Service
Manager.

---

## 10. Engineer portal

Every engineer signs in and sees:

- Sites assigned to them
- Tasks assigned, and which are complete
- **Material in their custody**, by DC
- Handoffs waiting for them to accept
- Shortfalls waiting for them to explain
- Their own deviation count

An engineer should never have to ask Stores what he is holding.

---

## 11. Schema — already built

Foundation 2 carries the structure:

| Table | Purpose |
|---|---|
| `inventory_custody_accounts` | Who holds material |
| `inventory_custody_assignments` | Current holder of a document |
| `inventory_custody_handoffs` | A to B transfer |
| `inventory_custody_handoff_lines` | Line-by-line acceptance and shortfall |
| `inventory_custody_cases` | Site or job order the material serves |

Missing and to be added with the DC module:

- Engineer explanation against a shortfall line
- Stores acceptance or rejection of that explanation
- Deviation records, typed, with the engineer and the source event
- Waivers with reason, waiving employee and timestamp
- Deviation and waiver counters for performance reporting
- Unidentified return register

---

## 12. What this depends on

The DC module is not built. It comes after material issue requests and issue.
This specification is written now, while the decisions are fresh, so DC is
built correctly the first time.

Engineer, vehicle and site sub-locations are deferred. The custody design must
accommodate them later without rework — the assignment already carries an
optional vehicle and site.

---

## 13. Decisions frozen here

1. Custody follows material, not paperwork.
2. Three DC types — consumable, tool, machine — chosen at creation, immutable.
3. Machine DCs require a customer signature at delivery. Consumable and tool
   DCs do not.
4. Tools return 100 per cent. A missing tool is a loss, not consumption.
5. Handoff is line by line, in the ERP, on the same soft copy both engineers
   see. No scanning in the field.
6. Custody moves only on acceptance. Unaccepted lines stay with the sender.
7. One day to accept, one day to explain. Then the Service Manager. Then his
   own defect.
8. Stores decides whether an explanation is accepted. Not the engineers, not
   the Service Manager.
9. Shortfall acceptance: Service Manager to ₹5,000, Technical Director above.
10. Material returning without a DC is accepted and recorded as an unidentified
    return, with a deviation raised.
11. Deviations are recorded automatically from real events, never typed in.
12. Thresholds are configuration — 3 and 5 per month to start, editable after
    trial.
13. The Service Manager may waive with a reason, and waivers are counted
    against him.
