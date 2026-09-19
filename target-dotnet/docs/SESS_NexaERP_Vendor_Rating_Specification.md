# SESS NexaERP — Vendor Rating and Evaluation

Date: 4 September 2026
Decided by: A. Paramananthan, Technical Director
Based on the existing ISO form **F/OP3/3** maintained in Excel

---

## 1. Current evidence and governing correction

The reviewed register contains 1,057 historical bills from 225 vendors, with
an average score of 96.9. Technical /15, Warranty /10, Commercial /10,
Response /5 and Overall /5 score full marks on every bill. Documents /10 is
always 7.5. No rejections were recorded in those 1,057 bills.

Those five constant full-mark dimensions total **45 points**, not 65. The
approved eight weights below total 100: automatic 75 and manual 25. These
arithmetic corrections do not change any dimension or its approved weight.

The latest owner instruction supersedes the earlier six-dimension model and
the instruction to discard typed historical ratings.

## 2. What is rated, and when

Rating happens after GRN. Quality comes from each GRN line's recorded QC
accepted and received quantities. Delivery, warranty, commercial terms,
documents and the three manual dimensions belong to the GRN. A multi-line
GRN must not multiply the manual or shipment scores by its number of lines.

QC Manager records the three manual dimensions per GRN. Automatic dimensions
must retain their source record IDs, effective revisions and scoring-rule
version. Missing source evidence cannot silently become full marks.

## 3. Eight dimensions

| Dimension | Maximum | Source |
|---|---:|---|
| Quality | 25 | QC accepted / received quantity, per GRN line |
| Delivery | 20 | PO committed delivery date against actual GRN receipt date |
| Warranty | 10 | Warranty months already recorded at GRN |
| Commercial | 10 | Payment terms already recorded on the PO |
| Documents | 10 | Presence of the required attachments on the GRN |
| Technical | 15 | Manual per GRN |
| Response | 5 | Manual per GRN |
| Overall | 5 | Manual per GRN |

**Automatic: 75. Manual: 25. Total: 100.** A caller cannot submit scores for
the five automatic dimensions. Retain the evidence behind every calculation.
Concession-accepted quantities count as accepted and remain identifiable.

The existing delivery scale remains applicable, multiplied by its new
20-point weight:

| Days late | Percentage | Points |
|---|---:|---:|
| On time or early | 100 | 20 |
| 1–3 | 90 | 18 |
| 4–7 | 75 | 15 |
| 8–15 | 50 | 10 |
| Over 15 | 0 | 0 |

The approved months-to-score and payment-terms-to-score scales must be
recorded as governed rules before warranty and commercial scores can be
computed. No such scales are defined in the source specification; do not
invent thresholds or substitute constant full marks. Required-document
policy must identify the attachments relevant to that GRN, retaining their
actual presence as evidence rather than importing the old constant 7.5.

Implementation source check: the current typed GRN contract exposes an expiry
date calculated as bill date plus 13 months. It does not expose the supplier's
actual warranty duration in months. PO warranty terms are retained as free
text. The existing source field/JSON key or legacy column for actual supplier
months must be identified before connecting this score. Do not treat the
constant generated expiry as measured supplier warranty, and do not guess a
duration by parsing arbitrary commercial prose.

Weights and scoring-rule changes retain full history and apply through a
versioned rule. Historical measured scores retain the rule and evidence used
at the time; changing a rule must not rewrite old evidence.

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
| **QC_MANAGER** | Records Technical /15, Response /5 and Overall /5 per GRN. |
| **PRODUCTION_MANAGER** | May also record the three manual dimensions where he received the material, as already specified. |
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

Import all **1,057 historical bills**, retaining the original typed dimension
scores and marking every imported rating **LEGACY**. Preserve source workbook,
sheet and row identity, bill identity, vendor mapping and original values.
Re-importing the same source must not duplicate bills or ratings.

A LEGACY score is historical evidence of what was typed, not proof of a
measurement. Display its provenance with the score in detail and summaries.
Do not relabel a typed 15/15 as measured, fabricate QC rejections, or invent
missing GRN attachments. Any later calculation from corroborated source facts
must be separate evidence, preserving the imported original.

Acceptance reconciles 1,057 bills and 225 distinct vendors against the actual
workbook, including source totals and dimension distributions. No generated
fixture can establish that the historical import is complete.

---

## 12. Decisions frozen here

1. Rating happens after GRN, by the QC Manager.
2. Quality is per GRN line; the other seven dimensions are per GRN.
3. Eight dimensions: Quality 25, Delivery 20, Warranty 10, Commercial 10,
   Documents 10, Technical 15, Response 5, Overall 5.
4. Quality, delivery, warranty, commercial and documents are computed.
   Technical, response and overall remain manual per GRN.
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
12. Import all 1,057 historical bills with their original scores marked
    LEGACY; never present typed history as measured evidence.
