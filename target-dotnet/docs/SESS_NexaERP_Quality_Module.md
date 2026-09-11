# SESS NexaERP — Quality Module

Date: 11 September 2026
Decided by: A. Paramananthan, Technical Director
Companion to `SESS_NexaERP_Pending_Work_Specification.md` — items 35 to 39

---

## What exists today

Incoming inspection is built and proven: per-lot-allocation inspection, partial
acceptance, concessions with lifelong provenance, fail-closed when no policy
exists.

**Everything else in this document does not exist.** The QC team's real work —
stage inspection through a chamber's construction, calibration of the
instruments that make inspection meaningful, and verification of work returning
from vendors — has no home in the ERP.

---

## 35. THE PROCESS CHECK SHEET

### One per job order. Prepared by the QC team. Nobody else.

A chamber passes through fifteen stages. At each, the QC team verifies against
a drawing, a standard or a measurement, and records the result. The chamber
does not advance until the stage is signed.

| # | Stage | What QC verifies | Against |
|---|---|---|---|
| 1 | CNC cutting and folding | dimensions, angles, hole positions | part drawing, ±tolerance marked by the design team |
| 2 | Fabrication — welding | weld quality, joint integrity | welding process standard |
| 3 | Grinding and finishing | surface preparation | visual standard |
| 4 | Powder coating | **coating thickness in microns** | agreed specification |
| 5 | Reassembly | assembled dimensions | GA drawing |
| 6 | Refrigeration assembly | layout, components, connections | refrigeration drawing |
| 7 | Electrical wiring | wiring, terminations, protection | electrical drawing |
| 8 | Refrigeration pressure test | holding pressure, duration, no drop | test procedure |
| 9 | Vacuum test | vacuum level held | test procedure |
| 10 | Inner tank leak test — smoke | no leakage | test procedure |
| 11 | Internal calibration | temperature and humidity uniformity and accuracy | committed specification |
| 12 | Pressure transmitter validation | reading against a calibrated source | master instrument |
| 13 | **FAT** | every committed technical point | offer, RFQ, customer PO, contract review |
| 14 | **PDI** | the same, with the customer present | as above |
| 15 | Dispatch clearance | packing, material against the PO, DC, documents | PO and packing list |

### Rules

**The QC team prepares the sheet.** Not design, not production. QC decides what
is measured because QC is accountable for the answer.

**Stage-wise, per job order.** A 550-litre bench chamber and a walk-in do not
share a sheet. Both may start from a template, but the sheet belongs to the job
order once created.

**Each stage records:** who inspected, when, the measured value where a value
exists, the reference it was measured against, pass or fail, and remarks.

**A failed stage does not delete.** It records the failure, the rectification,
and the re-inspection. The original failure is evidence.

**A stage cannot be skipped.** If a chamber genuinely does not need a stage —
no humidity, so no humidity calibration — the QC team marks it *not applicable*
with a reason at the time the sheet is created, not at the time it is reached.

**Stage 13 is FAT**, which already exists as a readiness state on the job
order. The check sheet must reach it, not duplicate it.

### What blocks what

- Stage 4 cannot pass until the powder-coating GRN exists
- Stages 8, 9 and 10 cannot pass before stages 6 and 7
- FAT cannot be marked READY while any stage is unsigned, in addition to the
  custody reconciliation already enforced
- Dispatch clearance cannot pass before PDI

---

## 36. JOB-WORK INSPECTION

Powder coating and CNC work go out to vendors and come back. The sequence is
**not** the same as a purchase.

```
DC to vendor  (material leaves, SESS still owns it)
      ↓
vendor processes
      ↓
vendor raises their bill
      ↓
material returns
      ↓
GRN entry
      ↓
QC inspects  ← AFTER the GRN, not before
```

This is the opposite of a purchase, where QC gates the GRN. Here the material
is already SESS's own; the GRN records its return, and QC judges the work done
to it.

### Powder coating

**Measured:** coating thickness in microns, against the agreed specification.

**Three outcomes on failure, all decided by the QC Manager:**

| Outcome | What happens |
|---|---|
| Return for recoating | material goes back to the vendor on a fresh returnable DC |
| Rectified at SESS | the vendor's team attends and corrects on site; recorded, not invisible |
| Total failure | scrapped, or a debit note raised against the vendor |

The third is a commercial act. A debit note needs Accounts; scrap needs the
scrap workflow and MD approval for disposal.

### CNC cutting and folding

**Measured:** against the part drawing, within the ± tolerance the design team
marked on it.

The tolerance is already on the drawing. QC does not invent it and must not be
asked to type it — the check sheet reads it from the part drawing revision
pinned to the job order.

### What must be recorded

- which DC the material went out on
- which GRN it returned against
- the vendor and their bill
- the measurement and the reference
- the outcome, and for a failure, which of the three paths was taken
- the rework DC, where one was raised

**A vendor whose work fails repeatedly must show up in the vendor rating.**
Job-work quality is quality.

---

## 37. CALIBRATION REGISTER

QC measurements mean nothing if the instrument is wrong.

### The instruments

| Instrument | Used for |
|---|---|
| Temperature data logger | chamber calibration, FAT |
| Humidity data logger | chamber calibration, FAT |
| Temperature sensor (master) | reference against the chamber's own sensor |
| Humidity sensor (master) | as above |
| Vernier caliper | CNC dimensional check |
| Weighing scale | job-work weight balance |
| Pressure transmitter (master) | pressure test, transmitter validation |
| Pressure source | transmitter validation |
| Pressure gauge | pressure and vacuum test |
| Force meter | assembly checks |
| Multimeter | electrical verification |
| Acetylene–oxygen brazing set | refrigeration joints |

**This list will grow.** Adding a new master instrument must be a screen, not a
code change — a new data logger or gauge is bought, registered and calibrated
without a developer.

### The cycle

```
instrument due
      ↓
sent to the third-party laboratory
      ↓
laboratory issues a certificate
      ↓
certificate uploaded to the ERP
      ↓
next due date = certificate date + 12 months
      ↓
reminder before expiry
```

**Annual. Third party.** Initially Sansel Calibration Laboratories LLP,
Chennai. The provider is a master record — SESS may use more than one, and will
change over time.

**Before the ISO audit** and before expiry, instruments go out for
recalibration. The ERP must make that list obvious, not leave it to memory.

### What must be recorded

- instrument identity, make, model, serial, range, resolution
- the provider, the certificate number, the certificate date
- the certificate itself, as an attachment
- the calibration due date
- where the instrument is: in the QC store, issued to an engineer, or away at
  the laboratory
- its full history — every certificate, never overwritten

### Rules

**An expired instrument warns, it does not block.** That is the frozen
decision, and it stands. But the fact that a measurement was taken with an
expired instrument is recorded **on that measurement**, permanently. An auditor
asking "what was this measured with" gets the instrument and its calibration
status at the time.

**Instrument custody follows the tool custody rules.** An instrument issued to
an engineer is his responsibility until returned.

**An instrument away for calibration cannot be issued.** Its state says so.

**Notification before the due date**, not on it. Calibration takes time and the
instrument is unavailable while it is away.

---

## 38. THE DELIVERY CHALLAN CORRECTION

`SESS_NexaERP_DC_Custody_Specification.md` describes three DC types:
CONSUMABLE, TOOL, MACHINE. That is **one axis** — what the material is.

The Technical Director has since named a **second axis** — the commercial
nature of the movement:

| Commercial nature | Meaning |
|---|---|
| RETURNABLE | comes back, on this DC |
| NON_RETURNABLE | does not come back; sold, consumed or given |
| DEMO | goes to a customer for trial, comes back unless bought |
| WARRANTY_FREE | a spare supplied at no charge under warranty |

**Both axes are needed.** A demo chamber is MACHINE + DEMO. A warranty spare is
SPARE + WARRANTY_FREE. Powder-coating material going to a vendor is
CONSUMABLE + RETURNABLE.

### Correction required

Add a fourth material type, **SPARE**, alongside CONSUMABLE, TOOL and MACHINE.
Add the commercial nature as a second immutable attribute chosen at creation.

Every rule already frozen stays attached to the **material type**:
- tools return 100 per cent; a missing tool is a loss
- machine DCs need a customer signature
- consumables reconcile used against returned

The **commercial nature** governs:
- whether a return is expected at all
- whether the movement needs a customer PO before it can leave
- whether warranty terms apply
- how the value is treated

**A DEMO that is bought** becomes a sale. Report how that transition is
recorded without rewriting the DC — my expectation is a new document
referencing it, never an edit.

---

## 39. WHAT THE QC TEAM SEES

Not a dashboard tile. A working screen, for NARREN and whoever succeeds him.

| Section | Content |
|---|---|
| Incoming inspection queue | GRN lines awaiting QC, with ageing against the two-day limit |
| Job-work returns awaiting inspection | powder coating, CNC, with the DC and the GRN |
| Stage inspections due | by job order, which stage, how long waiting |
| **Instruments due for calibration** | within 60 days, and overdue |
| Instruments away at the laboratory | with expected return |
| FAT readiness | blocked and why |
| Concessions I raised | and their decisions |
| My deviations | if any |

**The calibration section is the one that prevents an audit finding.** An ISO
auditor asks for the calibration register and the certificates. If the QC
Manager has to look in a drawer, the finding is written before he opens it.

---

## Acceptance criteria

**Done when:**

1. A job order can carry a stage check sheet prepared by QC, with all fifteen
   stages, and a chamber cannot reach FAT with an unsigned stage.
2. A powder-coating return can be inspected after its GRN, fail, and follow any
   of the three failure paths, with the path recorded.
3. A CNC return is measured against the part drawing revision pinned to the job
   order, not against a typed tolerance.
4. A master instrument can be registered, sent for calibration, have its
   certificate uploaded, and appear in a due-date list — all from a screen.
5. A measurement taken with an expired instrument is permitted, and the
   expiry is recorded on that measurement forever.
6. A DC carries both a material type and a commercial nature, both immutable
   after creation.

**Must refuse:**

- FAT readiness while any stage is unsigned
- a stage signed by anyone outside the QC team
- deleting a failed stage rather than recording its rectification
- issuing an instrument that is away at the laboratory
- a job-work inspection with no GRN behind it

**Test against the end-to-end witness:** a chamber that goes out for powder
coating, fails on thickness, is returned for recoating, passes on
re-inspection, completes every stage, and reaches FAT — with the failure still
visible in its record.

---

## Open questions for the Technical Director

1. **Internal verification between annual calibrations.** You calibrate
   annually through a third party. Is there any check in between — a vernier
   against a reference block, a scale against a standard weight — or is the
   annual certificate the only control?

2. **Powder coating thickness.** Is there a single agreed micron range for all
   work, or does it vary by customer or by part?

3. **Who signs a stage?** Any QC team member, or NARREN as QC Manager only? The
   answer decides whether a second QC person can be hired without a code
   change.

4. **Pressure, vacuum and leak test values.** Are the acceptance values fixed by
   SESS standard, or per chamber from the offer?
