# SESS NexaERP — Answers to the Twelve Open Questions

Date: 11 September 2026
Decided by: A. Paramananthan, Technical Director

**Corrects** items 38, 40, 42, 43, 44, 46 and 47. Where this document and an
earlier specification disagree, this one governs.

Every question raised across the Quality Module, Stores Completion and
Decisions documents now has an answer. Nothing in items 1 to 50 is left for
Codex to guess.

---

# QUALITY MODULE — items 35 to 39

## 1. Internal verification between annual calibrations

**Annual only. There is no intermediate internal check.**

The third-party certificate is the sole control. Do not build an internal
verification workflow — it would record something SESS does not do.

## 2. Powder coating thickness

**Minimum 80 microns.**

Where a customer specifies a different value, that value governs for that job.

So the acceptance value is: the customer's specified thickness where one
exists, otherwise 80 microns minimum. The check sheet reads it from the job
order, not from a global constant.

## 3. Who signs a stage

**NARREN, SESS-33, QC_MANAGER today. Any QC person appointed later, also.**

Bind the authority to the **role**, never to the employee code. When a second
QC person is hired and given QC_MANAGER, they sign — with no code change, no
migration, no developer.

This is the same principle already applied to approval routing.

## 4. Pressure, vacuum and leak test values

**They change by product.**

Not a SESS standard, not a global constant. The values come from the job
order's committed specification — the offer, the customer PO, and eventually
the contract review.

The check sheet reads them. QC does not type them.

---

# STORES COMPLETION — items 40 to 42

## 5. Demo — A CORRECTION TO ITEM 40

I wrote four commercial natures: RETURNABLE, NON_RETURNABLE, DEMO,
WARRANTY_FREE.

**That is wrong. A demo is returnable.** The Technical Director: *demo is demo
purpose, after demo it needs to return, it will be returnable only.*

### The corrected model

**Three commercial natures:**

| Nature | Returns? | Needs a customer PO? |
|---|---|---|
| RETURNABLE | yes | no |
| NON_RETURNABLE | no | **yes, before dispatch** |
| WARRANTY_FREE | no | no — warranty is the authority |

**Purpose is a separate attribute on a RETURNABLE DC:**

| Purpose | Due date |
|---|---|
| DEMO | **mandatory** |
| JOB_WORK | mandatory |
| SITE_WORK | mandatory |
| TRIAL | mandatory |

A demo chamber is: material type MACHINE, nature RETURNABLE, purpose DEMO,
with a due date.

**If a demo is bought**, that is a new commercial document referencing the DC.
The original DC was returnable and remains so in the record; the new document
records that it will not come back. Never edit the DC.

## 6. Tool custody transfer

**Each tool is entered separately. There is no set.**

Two situations, both tool by tool:

| Situation | Rule |
|---|---|
| Store issues a tool to an engineer for work | returns to the store |
| A new engineer is given tools on joining | each tool recorded individually; all return on resignation |

**No set-level record.** A new engineer receiving eight tools produces eight
custody records, and resignation clearance names all eight that are still open.

## 7. Tool loss value

**Depreciated value, per the Accounts policy.**

Where a tool is damaged rather than lost, **it is replaced** — the replacement
is recorded against the same custody, not written off as a loss.

Loss and damage are different events with different outcomes:

| Event | Outcome |
|---|---|
| Lost | written off at depreciated value, TD approval |
| Damaged | replaced, recorded against the custody |

## 8. Customer property held too long

**Returning it unrepaired is sometimes possible.**

No fixed threshold. The escalation stays as specified — overdue notifies TD and
MD — and returning the machine is a decision they may take, recorded with a
reason.

Do not build an automatic return. Build the ability to return, and record why.

---

# DECISIONS OF 11 SEPTEMBER — items 43 to 50

## 9. Vendor payment — one bill or many

**Both occur in normal work.**

A payment may settle one bill, or several bills to the same vendor together.

So the model is: a payment references **one or more accepted bills**, with an
amount allocated to each. The sum of allocations equals the payment.

**Must refuse:** allocating more to a bill than its accepted value less
adjusted advances; including a bill from a different vendor; including an
unaccepted bill.

## 10. Shelf life — which categories

**Adhesives, sealants, silicone, thread lock and similar chemical consumables.**

**NOT refrigerant.** Refrigerant does not carry a shelf life at SESS.

The category list is configurable — it will grow as items are added. Start with
the chemical consumables and let the Stores Manager extend it.

## 11. Debit note approval

**Purchase Manager or Accounts Manager. Either may approve.**

Not both together. Not a value band. Either one.

## 12. Document control scope — A CORRECTION TO ITEM 43

I wrote document control as though it were a Purchase and Stores concern. It
is not.

**It covers the whole quality system: Purchase, Stores, Production, Service and
HR.**

### Ownership

**Each department owns and is responsible for its own documents.**

A document has an owning department. Its author, reviewer and approver come
from that department — one department cannot approve another's procedure. That
is correct under ISO and it must be enforced, not assumed.

### The quality manual

**The MRM approves it** — the Management Representative.

**Today the MRM is the Managing Director. That will change.**

So bind it to a **role**, `MANAGEMENT_REPRESENTATIVE`, never to SESS-02. When
the MRM changes, the role assignment moves and nothing else does.

The same rule as the QC stage signature, and for the same reason.

---

## What changes in the specifications

| Document | Change |
|---|---|
| Quality Module, item 37 | no intermediate internal verification; annual only |
| Quality Module, item 35 | thickness and test values read from the job order, never global constants |
| Quality Module, items 35 and 39 | stage authority by ROLE, never by employee code |
| Stores Completion, item 40 | **three natures, not four.** DEMO becomes a purpose on a RETURNABLE DC |
| Stores Completion, item 42 | tools entered individually; no set. Loss and damage are different events |
| Decisions, item 44 | a payment may reference several bills, with allocation per bill |
| Decisions, item 46 | shelf life applies to chemical consumables, not refrigerant |
| Decisions, item 43 | **whole quality system, not just Purchase and Stores.** Department owns its documents. MRM approves the manual, by role |

---

## The principle behind three of these answers

Three answers say the same thing in different words:

- stage signature bound to the QC_MANAGER role, not to NARREN
- quality manual approved by MANAGEMENT_REPRESENTATIVE, not by the MD
- test values read from the job order, not from a constant

**Nothing that will change should be written into the code.** People change
roles, SESS hires, specifications vary by product. The ERP must absorb that
without a developer.

That principle has held since the role governance work in early September. It
still holds.

---

## Nothing is left open

Twelve questions asked, twelve answered. Items 1 to 50 are fully decided.

Where Codex finds something genuinely undecided, it stops and asks — as it has
done eleven times so far, and as it should continue to do.
