# SESS NexaERP — Stores Completion

Date: 11 September 2026
Decided by: A. Paramananthan, Technical Director
Companion specification — items 40 to 42

**Supersedes** the brief treatment of delivery challans, customer property and
tools in `SESS_NexaERP_Pending_Work_Specification.md` items 17, 21 and 22.

---

## Why these three

Material enters the store and leaves it. Everything built so far handles
material that SESS owns, arriving from a vendor and going to a chamber.

These three are the cases where that is not true:

- material leaving on a **delivery challan** — to a customer, a vendor, a site
- material arriving that **belongs to the customer**
- **tools**, which are SESS assets that leave and must come back whole

Without all three, Stores does not describe what actually happens at SESS.

---

# 40. DELIVERY CHALLAN — TWO AXES

## The correction

`SESS_NexaERP_DC_Custody_Specification.md` names three material types. That is
one axis. The Technical Director has since named a second — the commercial
nature of the movement.

**Both are chosen at creation. Both are immutable afterwards.**

### Axis 1 — what the material is

| Type | Return expectation |
|---|---|
| CONSUMABLE | unused portion returns; used portion is explained |
| TOOL | **100 per cent returns.** A missing tool is a loss, never consumption |
| MACHINE | returns, with a customer signature at delivery |
| **SPARE** | depends entirely on axis 2 |

SPARE is new. Until now a spare had to be forced into one of the other three.

### Axis 2 — the commercial nature

| Nature | Meaning | Returns? | Needs a customer PO? |
|---|---|---|---|
| RETURNABLE | comes back on this DC | yes | no |
| NON_RETURNABLE | sold, consumed or given away | no | **yes, before it leaves** |
| DEMO | trial at a customer site | yes, unless bought | no, but a due date is mandatory |
| WARRANTY_FREE | supplied at no charge under warranty | no | no — warranty is the authority |

### The combinations that occur at SESS

| Combination | Example |
|---|---|
| CONSUMABLE + RETURNABLE | cutting wheels to a site; five used, five back |
| CONSUMABLE + NON_RETURNABLE | material consumed at a customer site under a PO |
| TOOL + RETURNABLE | **the only valid tool combination** |
| MACHINE + RETURNABLE | chamber to a customer for rental or trial |
| MACHINE + DEMO | chamber to a customer for evaluation |
| MACHINE + NON_RETURNABLE | a sold chamber going out permanently |
| SPARE + NON_RETURNABLE | a spare sold against a customer PO |
| SPARE + WARRANTY_FREE | a replacement spare under warranty |

**TOOL may only ever be RETURNABLE.** A tool that does not come back is a
written-off loss, not a different kind of DC. Refuse any other combination.

## What each nature requires

### NON_RETURNABLE

Material leaves and does not come back. That is a commercial act.

- a customer PO reference is **mandatory before dispatch**
- the Managing Director is **notified on every one**, as already frozen
- value leaves SESS inventory permanently
- no return is expected and none is chased

### DEMO

- a **due date is mandatory** at creation
- overdue notifies the Sales owner, then TD and MD
- the chamber remains SESS stock in a customer-custody location
- extension requires TD approval, recorded with a reason

**If a demo is bought**, that is a new commercial document referencing the DC —
never an edit of it. The DC closes as *converted to sale*. Report how you
record that link.

### WARRANTY_FREE

- the installed machine must be identified
- its warranty must be **live on the dispatch date** — this is checkable, and
  must be checked
- no customer PO; the warranty is the authority
- if warranty has expired, refuse and require a chargeable path instead

Warranty is bill date plus thirteen months, already frozen and already
recorded at GRN.

## What stays unchanged

Everything in `SESS_NexaERP_DC_Custody_Specification.md` remains in force:
custody follows the material, line-by-line handoff, one day to accept, one day
to explain, Stores decides on shortfall, Service Manager to ₹5,000 and TD
above, deviations recorded automatically, waivers counted against the Service
Manager.

Those rules attach to the **material type**. This document adds what the
**commercial nature** governs.

---

# 41. CUSTOMER PROPERTY

## What arrives

Three kinds, each with different commercial treatment:

| Kind | Chargeable? | Authority |
|---|---|---|
| Another brand's machine for PLC or control modification | **always** | offer, then customer PO |
| A SESS machine returned under warranty | no | warranty terms |
| A SESS spare returned under warranty | no | warranty terms |

## The rule that matters most

**A machine may arrive with no purchase order.** Customers send equipment and
discuss commercials afterwards. Refusing the inward entry would leave a machine
standing outside the gate.

So:

```
machine arrives at the gate
      ↓
gate entry, then customer property inward  ← always accepted
      ↓
state: AWAITING_COMMERCIAL_AUTHORISATION
      ↓
offer raised → customer PO received
      ↓
work may begin
```

**The inward is accepted. The work is blocked.** Those are different things and
the ERP must not conflate them.

## Rules

**Customer property never enters SESS inventory value.** Not in stock
valuation, not in FIFO, not in any total that an accountant reads. Where it
appears on a dashboard it is labelled, separately, so that reading the wrong
number once is not possible.

**Separate rack, per company.** Already configured: one customer-property rack
in SESS Pvt Ltd, one in SESS Proprietorship.

**Accessories are a list with photographs**, not individually serialised. A
customer sends a machine with cables, a trolley and a manual; those are
recorded and photographed, not given inventory identity.

**A due date is entered by Stores after consulting management.** There is no
system default. Somebody decides.

**Overdue notifies TD and MD.** Extension requires TD approval with a reason.

**A component beyond the PO scope needs a separate offer and a new PO.** Work
does not silently expand.

## Removed parts — the rule people get wrong

**Parts removed from a customer's machine are ALWAYS customer property.** They
go back with the machine.

SESS keeps them **only** under an explicit, recorded buyback agreement — a
document, not an assumption, not a verbal arrangement.

This is where disputes happen. An old compressor removed from a customer's
chamber is the customer's compressor until they agree otherwise in writing.

## The return

```
work complete
      ↓
QC verifies the work done
      ↓
outward DC — material type MACHINE, nature RETURNABLE
      ↓
removed parts accompany it, listed
      ↓
customer signature at delivery
      ↓
custody case closes
```

The case cannot close while any removed part is unaccounted for.

---

# 42. TOOLS

## What a tool is

**Every tool is an asset with its own identity. There is no value threshold.**

A ₹200 spanner and a ₹40,000 torque wrench both get an identity, because the
question "who has it" matters equally for both, and a threshold invites
argument.

## Two kinds of custody

### Permanent custody

Each tool is assigned individually to an employee indefinitely. There is no
set-level custody record. Eight tools mean eight individually identified
assets and eight custody records, including on transfer to a successor.

- assigned by Stores, accepted by the employee
- no due date
- transfers require both parties, like a DC handoff
- **resignation clearance is blocked while any tool custody is open, permanent
  or temporary, and names every outstanding tool**

### Temporary issue

A tool issued for a job, for a stated period.

- **a duration in DAYS is entered at issue.** Not a date picked from a
  calendar — a number of days, because that is how the storekeeper thinks and
  says it.
- the due date is computed from the issue date plus that duration
- overdue notifies the custodian, then the Stores Manager
- extension is recorded, never silent

## A tool that does not come back

**It is a LOSS, never consumption.**

- written off at depreciated value under the Accounts policy, with Technical
  Director approval; retain the accepted valuation and policy evidence
- the write-off names the custodian
- the loss is recorded against that engineer's deviation count
- the tool's identity is retired, not deleted — its history remains

A consumable that is used up is consumption. A tool that disappears is a loss.
The ERP must never let one be recorded as the other.

## A damaged tool

Damage is not a loss write-off. Record the replacement against the same open
custody, naming both the damaged asset and its replacement. Preserve the
original asset's history and the custodian's acceptance of the replacement;
do not make the custody disappear or create a set-level transaction.

## Opening register and clearance

The supplied register totals are 164 tool types, 730 purchased, 564 issued,
166 in Stores, 288 historical custody lines and 23 holders. Every type row
balances purchased minus issued. Import each physical asset with its own
identity, retaining the source row and custody-line reference when a legacy
line represents several tools. Do not collapse 564 individually held assets
into 288 set records. These totals are import acceptance checks; they do not
substitute for the actual register or establish purchase/depreciation values.

Clearance is a business decision separate from login disablement. Open
custody blocks resignation clearance and names the tools, but must never
prevent revoking a departing employee's login access.

## What must be visible

| Question | Answer available from |
|---|---|
| Who has this tool? | the tool record |
| What does this engineer hold? | his portal, and the Stores dashboard **by engineer** |
| What is overdue, and by how long? | the overdue list |
| What have we lost this year, and to whom? | the write-off register |

**By engineer, always.** A total tells nobody what to do.

## Calibrated instruments

Master instruments — data loggers, vernier calipers, pressure transmitters —
are tools **and** calibrated equipment. Both sets of rules apply:

- tool custody rules for who holds them
- calibration rules from `SESS_NexaERP_Quality_Module.md` item 37 for whether
  they may be used

**An instrument away at the laboratory cannot be issued.** Its state says so.

---

## Acceptance criteria for all three

**Done when:**

1. A DC carries both a material type and a commercial nature, both immutable,
   and TOOL can only ever be RETURNABLE.
2. A NON_RETURNABLE DC cannot be dispatched without a customer PO reference,
   and the MD is notified.
3. A DEMO DC cannot be created without a due date, and overdue escalates.
4. A WARRANTY_FREE DC checks the installed machine's warranty is live on the
   dispatch date and refuses if it is not.
5. Customer property can be received with no PO, work is blocked until one
   exists, and the block is visible as a state rather than an absence.
6. Customer property never appears in any SESS stock value.
7. Removed parts are recorded as customer property and the case cannot close
   while any is unaccounted for.
8. A tool can be issued for a number of days, becomes overdue, escalates, and
   can be written off as a loss with TD approval.
9. Resignation clearance refuses while tool custody is open, naming the tools.

**Must refuse:**

- TOOL with any nature other than RETURNABLE
- NON_RETURNABLE dispatch with no customer PO
- WARRANTY_FREE against an expired warranty
- recording a lost tool as consumption
- closing a customer property case with an unaccounted removed part
- including customer property in stock valuation
- issuing an instrument that is away at the laboratory
- clearing a resignation with open tool custody

**Test against the end-to-end witness:**

- a chamber goes out as MACHINE + DEMO, comes back, and the DC closes
- a spare goes out as SPARE + WARRANTY_FREE against a live warranty, and a
  second attempt against an expired one is refused
- a customer machine arrives with no PO, work is blocked, a PO arrives, work
  proceeds, removed parts go back with it
- a tool is issued for seven days, becomes overdue, escalates, and is written
  off with TD approval — and the engineer's deviation count reflects it

---

## Open questions

1. **Demo conversion.** When a customer buys a demo chamber, what document
   records the sale? A customer PO against which the DC closes, or something
   else?

2. **Permanent tool custody transfer: answered.** Each tool transfers
   individually; both parties participate. There is no set-level record.

3. **Tool loss value: answered.** Depreciated value under the Accounts
   policy, with TD approval. Damage is replacement against the same custody,
   not a loss write-off.

4. **Customer property held too long.** A machine has sat for eight months
   awaiting a PO that never came. What is the escalation beyond notifying TD
   and MD — is there a point at which SESS returns it unrepaired?
