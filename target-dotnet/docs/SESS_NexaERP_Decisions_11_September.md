# SESS NexaERP — Decisions of 11 September 2026

Decided by: A. Paramananthan, Technical Director
Companion specification — items 43 to 50

**Corrects** the job-work treatment in
`SESS_NexaERP_Pending_Work_Specification.md` item 20, which assumed weight
reconciliation applies to all job work. It does not.

---

# 43. DOCUMENT CONTROL

**Decided: yes, it belongs in the ERP.**

ISO 9001 clause 7.5 requires documented information to be identified,
formatted, reviewed, approved, version controlled, available where needed, and
prevented from unintended use when obsolete. Today the quality manual, SOPs and
work instructions live outside the ERP. Drawings have revision control;
procedures do not.

## What is controlled

| Type | Example |
|---|---|
| Quality manual | the top-level document |
| Standard operating procedure | incoming inspection, dispatch clearance |
| Work instruction | how to measure powder-coating thickness |
| Form template | F/OP3/3 vendor rating, check sheets |
| External standard | IEC 60068-3-5, held for reference |

## Rules

**Annual review is mandatory.** Every controlled document carries a review due
date. Passing it without review is a finding.

**Revision, not replacement.** A new version supersedes the old; the old is
retained and marked obsolete, never deleted. An auditor asks what the procedure
said two years ago.

**Approval before release.** A document has an author, a reviewer and an
approver, and they are not the same person.

**Obsolete versions cannot be opened as current.** Retained for the record,
clearly marked, and not returned by any list that says "current procedures".

**Acknowledgement.** Where a document governs someone's work, record that they
have seen the current revision. That is what an auditor means by "available
where needed".

## What must be recorded

- document number, title, type, owner
- revision number, date, author, reviewer, approver
- the file itself
- review due date and review history
- supersession — which revision replaced which
- distribution and acknowledgement

---

# 44. VENDOR PAYMENT

**Decided: it belongs in this ERP, not in a separate accounting system.**

Vendor advance is already specified (item 10). This adds the rest of the cycle.

## What is needed

| | |
|---|---|
| Payment terms on the PO | days, milestone, or advance percentage |
| Due date computed | from bill acceptance plus terms |
| Payment schedule | what is due this week, this month |
| Overdue payables | with age and vendor |
| Payment recorded against a bill | partial and full |
| Advance adjusted | oldest first, as specified in item 10 |
| Outstanding per vendor | bills, advances, net position |

## Rules

**Payment is recorded, not initiated.** The ERP does not move money. It records
what Accounts paid, against which bill, on what date.

**A payment cannot exceed the accepted bill value** less any advance already
adjusted.

**A bill cannot be paid before it is accepted.** Acceptance is the control.

**Question for Accounts:** does SESS pay against a bill, or against a batch of
bills to one vendor? The answer decides whether a payment references one bill
or many.

---

# 45. REORDER LEVEL

**Decided: reorder levels apply to SOME items, not all.**

| Reorder level set | Why |
|---|---|
| Fasteners, bolts, nuts | regular use, low value, always needed |
| Sensors — pressure, temperature | regular use, kept in stock |
| PLC, sometimes | occasionally stocked |
| Packing material — foam, crate, protection | minimum stock kept |

| No reorder level | Why |
|---|---|
| Compressor | high value, bought against an order |
| Large PLC | bought against an order |

## Rules

**A reorder level is optional per item.** An item without one is never checked
and never appears on the list. That is correct, not a gap.

**When stock falls at or below the level, notify BOTH the purchase team and the
stores team.** Not one. Both need to know — Purchase to act, Stores to expect
the receipt.

**The notification does not create a purchase requisition.** It produces a
list. A person decides.

**The list is a screen**, not only a notification: items at or below level,
current stock, reorder level, last purchase rate, preferred vendor.

---

# 46. SHELF LIFE

**Decided: immediate notification to the concerned person.**

Some stock expires — refrigerant in packs, adhesives, sealants, silicone,
thread lock, calibration gases.

## Rules

**Expiry is recorded at GRN** where the item is marked as having a shelf life.

**Notification is immediate to the concerned person** — not a monthly report.
The Technical Director's words: the person who needs to know is told at once.

**Expired stock warns, it does not block.** Consistent with the calibration
rule already frozen. But issuing expired stock records that fact on the issue,
permanently.

**Question:** which item categories have a shelf life? The list can start small
and grow, but somebody must set it.

---

# 47. JOB WORK — TWO DIFFERENT PROCESSES

**This corrects item 20.** I wrote that job work reconciles by weight with a
per-process tolerance. That is true for one process and wrong for the other.

## Powder coating — COUNT, not weight

```
DC out:  1 cover, 2 inner tanks, 2 outer tanks, with sizes
         no weight - a load can be 1,000 kg and is not weighed
              ↓
vendor coats
              ↓
material returns
              ↓
GRN
              ↓
COUNT verified by BOTH the QC team and the Stores team
MICRON measured by QC
```

**Counted by two teams, together.** QC and Stores both count and agree. That is
how it is done today and it should stay that way in the ERP — the count is
signed by both.

**No tolerance on count.** Five panels out, five panels back. Four is a
shortfall, not a variance.

**A missing panel raises a debit note against the vendor.**

## CNC cutting and folding — WEIGHT, with tolerance

```
DC out:  full sheets - thickness, count, TOTAL WEIGHT
              ↓
vendor cuts and folds
              ↓
returns: cut parts + folded parts + SCRAP
         the same DC comes back - not a new one
              ↓
weighed
              ↓
weight out versus weight back
              ↓
tolerance, default 10 per cent, EDITABLE
```

**Scrap returns.** That is what makes the weight balance possible. A vendor who
keeps the scrap has taken SESS material.

**The same DC returns.** Unlike powder coating, the CNC DC is one document out
and back.

## Weighing

| Weight | Where |
|---|---|
| Up to 500 kg | SESS platform scale |
| Above 500 kg | **external weighbridge** |

SESS's scale reaches 500 kg. Above that, material goes to an outside
weighbridge and comes back with a slip.

**The weighbridge slip is uploaded to the ERP.** An auditor asking where a
weight came from gets the slip, not a typed number.

## Tolerance

**Default 10 per cent, editable.** The Technical Director: sometimes less is
appropriate, and the figure must be adjustable rather than fixed in code.

Configurable by TD, MD or IT_MANAGER, with change history, like every other
configured value.

**Outside tolerance:** the vendor explains. If the explanation is not accepted,
a debit note follows.

## Debit notes

Raised for:
- material missing on a powder-coating return
- scrap not returned from CNC
- weight shortfall outside tolerance without an accepted explanation

**Question:** who approves a debit note — Purchase Manager, Accounts Manager,
or TD? The value will vary.

---

# 48. PURCHASE WEIGHING

Separate from job work, and it does exist.

Copper pipe, wire and fasteners are bought by weight. At GRN the weight is
verified on the SESS scale.

**Needed:** a weight field on the GRN line for items bought by weight, with the
purchase weight compared to the received weight, and a variance recorded.

This is not job work and shares none of its rules.

---

# 49. CALIBRATION BACK-ASSESSMENT

**Not previously specified. ISO 9001 clause 7.1.5 requires it.**

When a master instrument is found out of calibration, ISO requires an
assessment of the validity of previous measurements made with it. If the
vernier was reading wrong, every dimension it measured since its last valid
certificate is suspect.

## Rules

**When a calibration fails**, the ERP lists every measurement taken with that
instrument since its last valid certificate:

- job-work inspections
- stage check sheet entries
- FAT and PDI readings
- purchase GRN weight checks where that scale was used

**QC decides what to re-check.** The ERP does not decide — it produces the list
and records the decision.

**The assessment is recorded** — which measurements, what was decided, what was
re-checked, what was accepted as-is and why.

Without this, a failed calibration is a finding waiting to happen. With it, it
is a controlled event.

---

# 50. CUSTOMER PROPERTY DAMAGE

**Not previously specified. ISO 9001 clause 8.5.3 requires it.**

If a customer's machine or part is lost, damaged or found unsuitable while at
SESS, the customer must be told and the record kept.

## Rules

**A damage or loss event is recorded against the customer property case** —
what happened, when, who found it, photographs.

**The customer is notified.** The notification, its date and the customer's
response are recorded.

**The case cannot close** while a damage event is open and unresolved.

**Resolution paths:** repaired at SESS cost, replaced, accepted by the customer
as-is, or settled commercially. Each is recorded with who approved it.

---

## Acceptance criteria

**Done when:**

1. A controlled document can be created, revised, approved by someone other
   than its author, released, superseded, and marked obsolete — and an obsolete
   revision is never returned as current.
2. Every controlled document has an annual review due date and appears on a
   review-due list before it passes.
3. A vendor payment can be recorded against an accepted bill, cannot exceed it,
   and the outstanding position per vendor is visible.
4. An item can be given a reorder level or left without one, and only those
   with one appear on the below-level list.
5. Falling below a reorder level notifies both Purchase and Stores, and creates
   nothing automatically.
6. A shelf-life item notifies immediately on approaching expiry, warns on
   issue, and records the fact on that issue.
7. A powder-coating return is verified by COUNT, signed by both QC and Stores,
   with no tolerance.
8. A CNC return is verified by WEIGHT against the same DC, with scrap included,
   at a tolerance that defaults to 10 per cent and can be edited.
9. A weighbridge slip can be uploaded and is required above 500 kg.
10. A failed calibration produces the list of affected measurements and records
    what QC decided.
11. Damage to customer property is recorded, the customer notified, and the
    case cannot close until it is resolved.

**Must refuse:**

- a document approved by its own author
- an obsolete revision returned by a current-documents list
- a payment against an unaccepted bill
- a payment exceeding the accepted value less adjusted advances
- a powder-coating count signed by only one team
- a CNC return with no weight where one is required
- a weight above 500 kg with no weighbridge slip
- closing a customer property case with an open damage event

---

## Open questions

1. **Vendor payment:** does SESS pay against one bill or a batch of bills to
   one vendor?
2. **Shelf life:** which item categories have one?
3. **Debit note approval:** Purchase Manager, Accounts Manager, or TD?
4. **Document control scope:** does it cover only Purchase and Stores
   procedures, or the whole quality system including Production and Service?
