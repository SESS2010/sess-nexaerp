# SESS NexaERP — Vendor Rating and Evaluation

Date: 4 September 2026
Decided by: A. Paramananthan, Technical Director
Based on the existing ISO form **F/OP3/3** maintained in Excel

---

## 1. Why this replaces the spreadsheet

The existing Excel does the right things. It captures per-receipt data, scores
five dimensions, and classifies vendors automatically. The ISO structure is
sound and this specification keeps it.

It has one fatal weakness. Of 107 vendors the average score is **95.89 per
cent**, the highest is 96 and almost every vendor sits between 95 and 96.

A rating where everybody scores the same tells you nothing. It cannot inform a
comparison, it cannot trigger corrective action, and an auditor will notice
that no supplier has ever been marked down.

The cause is that Quality, Technical Compliance, Documentation and Overall
Rating are all typed by hand, and every row says "Excellent".

**The fix is to compute what can be computed.** Delivery, quality and
documentation come from data the ERP already holds. Nobody types them, so
nobody can flatter a vendor.

The summary sheet is also wrong — Excellent 107 plus Acceptable 2 plus Poor 1
is 110, against 107 vendors. That is what happens when a spreadsheet grows past
what a spreadsheet can hold.

---

## 2. What is rated, and when

**Rating happens after GRN, performed by the QC Manager.**

He has the goods, the bill, the documents and the inspection result in front of
him. That is the only moment when every dimension can be judged honestly.

| Rated per | Dimension |
|---|---|
| **GRN line** | Quality — one line may be accepted and another rejected |
| **GRN** | Delivery, documentation, technical compliance, response |

The Excel rates every dimension per line. A twelve-line GRN therefore produces
twelve identical delivery scores, which inflates the sample and makes one late
delivery look like twelve. Delivery is a property of the shipment, not the item.

---

## 3. Six dimensions

### Computed — the ERP works these out

**Quality — weight 30%**
```
accepted quantity ÷ received quantity, per GRN line
```
Comes straight from the QC disposition. Concession-accepted material counts as
accepted, but the concession is recorded and visible.

**Delivery — weight 25%**
```
GRN date versus PO date, against the agreed commitment days
```

| Days late | Score |
|---|---|
| On time or early | 100 |
| 1–3 | 90 |
| 4–7 | 75 |
| 8–15 | 50 |
| Over 15 | 0 |

**Documentation — weight 10%**

Four checks, 25 each:
- vendor bill present and matching the PO
- delivery challan present
- test or material certificate where the item requires one
- bill and goods arrived together

The ERP knows all four. Nobody types this.

### Judged — a person decides

**Technical compliance — weight 20%**

Did what arrived match what was ordered — brand, make, specification?

| | Score |
|---|---|
| Exact match | 100 |
| Equivalent, accepted | 80 |
| Deviation, accepted under concession | 50 |
| Wrong item | 0 |

The Danfoss-versus-Castel case is exactly this. If the order said Danfoss and
Castel arrived, that is a deviation whatever the price was.

**Response to non-conformity — weight 10%**

Only scored when there was a rejection. When nothing was rejected the dimension
is not counted and the remaining weights are rescaled.

| | Score |
|---|---|
| Replaced or credited within a week | 100 |
| Within a month | 75 |
| Beyond a month | 40 |
| Ignored | 0 |

**Price competitiveness — weight 5%**

Against the other quotations on the same RFQ.

| | Score |
|---|---|
| Lowest | 100 |
| Within 5% of lowest | 90 |
| Within 10% | 75 |
| Within 20% | 50 |
| Beyond 20% | 25 |

Only where a comparison exists. Direct and emergency purchases skip it and the
weights rescale.

### Weights

| Dimension | Weight |
|---|---|
| Quality | 30% |
| Delivery | 25% |
| Technical compliance | 20% |
| Documentation | 10% |
| Response to non-conformity | 10% |
| Price competitiveness | 5% |

Quality carries the most because a bad compressor in a chamber costs more than
a late delivery.

**Weights are configuration**, editable by TD, MD or IT_MANAGER with full
change history. The ISO auditor may ask for a dimension to be added — that must
be a screen, not a code change.

---

## 4. Rolling window

**The score reflects the last twelve months.** Not lifetime.

A vendor who was poor two years ago and has been good since should show as
good. A vendor coasting on an old reputation should not.

Every receipt keeps its own permanent score. The vendor score is the weighted
average of receipts inside the window, recalculated as receipts age out.

---

## 5. New vendors start at 100%

A new vendor is registered at **100 per cent**, because registration is not
automatic. Before a vendor is accepted, SESS has already:

- inspected the factory or office
- verified the GST registration
- verified bank details
- checked MSME or incorporation status
- assessed capability for the category

That qualification is the justification for the opening score. Real supply then
moves it, up or down.

The score is marked **provisional** until three receipts exist, so nobody
mistakes an unproven vendor for a proven one.

---

## 6. Categories

| Score | Category | Effect |
|---|---|---|
| 90% and above | **Excellent** | Normal |
| 80–89% | **Good** | Normal |
| 70–79% | **Acceptable** | Purchase notified |
| Below 70% | **Poor** | **PO blocked** |

### A poor vendor cannot receive a purchase order

**No PO may be released to a vendor below 70 per cent without the written
concurrence of the Technical Director and the Managing Director.**

Both. Not either.

Without that block the rating changes nothing — buying continues and the score
becomes decoration. The override exists for genuine urgency and single-source
situations, and every use is recorded with its reason.

---

## 7. Revaluation

A poor vendor is not abandoned. ISO 9001 clause 8.4 requires periodic
re-evaluation, and a supplier who improves must be able to return.

```
Vendor falls below 70%
        ↓
SESS notifies the vendor formally
        ↓
Vendor submits a written improvement plan
        ↓
The letter is uploaded to the ERP as evidence
        ↓
QC Manager assesses and recommends
        ↓
Technical Director approves or refuses
        ↓
Reinstated on probation, or suspended
```

**Both are required. The QC Manager recommends; the Technical Director
approves.** Vendor acceptance is a business decision, not only a quality one.

A reinstated vendor is on **probation** for the next five receipts. A second
fall below 70 per cent during probation means suspension without a further
revaluation cycle.

The improvement letter, the assessment and the decision are all retained. That
is the corrective-action record an auditor asks for.

---

## 8. Who does what

| Role | Rating role |
|---|---|
| **QC_MANAGER** | Primary. Scores technical compliance and response after GRN. |
| **PRODUCTION_MANAGER** | May also score, where he received the material |
| **TECHNICAL_DIRECTOR** | Approves revaluation, concurs on poor-vendor override |
| **MANAGING_DIRECTOR** | Concurs on poor-vendor override |
| Everyone else | Read only |

Computed dimensions are nobody's to type.

---

## 9. Vendor qualification — recorded now, built later

Vendor registration today has no structured qualification record. The checks
happen; the evidence is not held.

The schema must carry it now so it can be filled in later without rework:

- factory or office inspection — date, by whom, findings, photographs
- GST registration verification
- bank account verification
- MSME or incorporation verification
- category capability assessment
- approved makes and brands the vendor may supply
- qualification approval — QC Manager recommends, TD approves
- re-qualification due date

Without this, the 100 per cent opening score has no documented basis, and that
is precisely what an ISO auditor will ask about.

---

## 10. What the comparison sheet shows

Three separate rankings. **Never merged into one number.**

```
1. PRICE                   L1, L2, L3
2. TERMS COMPLIANCE        Best 1, 2, 3 — fewest deviations against the RFQ terms
3. PAST PERFORMANCE        Best 1, 2, 3 — the rating from this document
```

The system then proposes a recommendation and states its basis.

**A human selects.** Purchase Manager or Technical Director, with a recorded
reason whenever the recommendation is not followed.

Merging the three into a single score hides why. Keeping them apart makes an
override explainable a year later: "L1 was cheapest but offered Castel instead
of Danfoss, so we took L2."

---

## 11. Migrating the existing data

The Excel holds two financial years of receipts. It is worth importing:
computed dimensions can be recalculated from the GRN, PO and invoice data it
already carries.

The hand-typed ratings should **not** be imported. Every row says Excellent and
importing them would carry the flat distribution into the new system on day one.

Import the facts. Recompute the scores.

---

## 12. Decisions frozen here

1. Rating happens after GRN, by the QC Manager.
2. Quality is per GRN line; delivery, documentation, technical and response are
   per GRN.
3. Six dimensions. Quality 30, delivery 25, technical 20, documentation 10,
   response 10, price 5. Configurable.
4. Delivery, quality and documentation are computed, never typed.
5. Twelve-month rolling window.
6. New vendors open at 100 per cent, marked provisional until three receipts.
7. Bands: 90+ Excellent, 80–89 Good, 70–79 Acceptable, below 70 Poor.
8. **A vendor below 70 per cent cannot receive a PO without both TD and MD
   concurrence.**
9. Revaluation requires the improvement letter as evidence, a QC Manager
   recommendation and Technical Director approval.
10. Reinstatement is on probation for five receipts.
11. The comparison sheet shows three separate rankings; a human selects and
    records the reason.
12. Import the facts from the Excel, recompute the scores, discard the
    hand-typed ratings.
