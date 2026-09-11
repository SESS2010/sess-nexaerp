# SESS NexaERP — Completeness Review

Date: 11 September 2026
Scope: Purchase and Stores, against what a manufacturing, project and service
company actually does, and against ISO 9001:2015

---

## Purpose

Five specifications now describe 42 items. This document asks a different
question: **what has nobody written down yet?**

It is a gap review, not a plan. Where something is missing I say so; where I am
uncertain whether SESS does it at all, I ask.

---

# PART 1 — WHAT IS COVERED

Across the five specifications:

| Area | Where |
|---|---|
| PR, RFQ, quotation, comparison, PO | built |
| Gate entry, GRN, QC, put-away | built |
| MIR, issue, return, custody | built |
| Vendor bill, three-way match, FIFO, landed cost | built |
| Job order, fitment, Actual BOM, variance, FAT | built |
| Estimated and Production BOM, drawings | built |
| Role governance, approvals, notifications | built |
| DC with two axes, customer property, tools | specified, items 40–42 |
| Stage inspection, calibration, job-work QC | specified, items 35–39 |
| Opening stock, imports, reports, dashboards | specified, items 13–34 |
| Vendor rating, cycle counting, scrap, intercompany | specified, items 17–24 |

**Nothing decided has been dropped.** The specification audit of 9 September
proves it.

---

# PART 2 — PURCHASE GAPS

Ten things a manufacturing company does that no specification covers.

## 2.1 Reorder-driven purchase requisition

The item master has a reorder level. Nothing reads it.

Today a PR is raised because somebody noticed. A reorder level that triggers
nothing is a number in a form.

**Needed:** a daily check that lists items at or below reorder level, and a
one-click PR from that list. Not automatic PR creation — a person must still
decide — but the list must exist.

**Question:** does SESS want reorder levels to drive purchasing at all, or is
buying always driven by a job order?

## 2.2 Emergency and spot purchase

Frozen: up to ₹5,000, maximum ten per month.

Nothing enforces the count. Nothing identifies a purchase as emergency. The
monthly limit cannot be checked because the category does not exist.

**Needed:** an emergency purchase type, the monthly counter, refusal at eleven,
and who may raise one.

**Question:** who may raise an emergency purchase — anyone, or named roles?

## 2.3 Minimum quotation count

You said two vendors is acceptable. Nothing enforces a minimum, and nothing
records why a comparison has only one.

**Needed:** a configurable minimum, and a mandatory reason when a comparison
falls below it. Single-source purchasing is legitimate — it just has to be
stated, not silent.

## 2.4 Purchase budget

Nothing limits spend against a budget, a period, or a job order.

**Question:** does SESS budget purchasing at all, or is the approval matrix the
only control? If the approval matrix is the control, this gap closes itself and
needs no work.

## 2.5 Payment terms and the payment schedule

Payment terms are captured on the PO. Nothing computes when payment is due,
what is overdue, or what is scheduled this week.

Vendor advance is specified (item 10). The rest of the payment cycle is not.

**Question:** does Accounts want payment scheduling in this ERP, or does that
stay in Tally or whatever they use today? This is the boundary question and it
should be answered before anyone builds towards it.

## 2.6 Transporter and freight forwarder master

Landed cost allocates freight. Nothing records who carried it.

For imports this matters more: a clearing agent is a party with a GST number, a
contact and a performance record.

**Needed:** a transporter and clearing-agent master, referenced from the GRN
and the bill.

## 2.7 Vendor communication record

A PO is chased by phone and email. None of it is in the ERP.

When a delivery is late and the vendor says "you approved the delay", nothing
supports or refutes it.

**Needed:** a simple communication log against a PO — date, mode, who, what was
agreed. Not an email client; a record.

## 2.8 Purchase order amendment after issue

Amendment exists in the schema. What is not specified: what may change after
issue, who approves it, and whether a vendor must reconfirm.

Price cannot change without revising the PO — that is frozen. Quantity, date
and delivery terms are not covered.

**Question:** after a PO is issued and the vendor has accepted it, what may
change, and does the vendor have to confirm?

## 2.9 Inbound insurance

Mentioned once as a landed-cost component. No policy record, no claim path.

**Question:** does SESS insure inbound material, or only imports, or not at all?

## 2.10 Import documentation set

Item 11 covers currency, duty and exchange rate. It does not cover the
documents: bill of entry, bill of lading or airway bill, packing list,
certificate of origin, insurance certificate.

Customs requires them. An auditor will ask for them.

**Needed:** a document set attached to an import purchase, with the mandatory
ones enforced before the GRN closes.

---

# PART 3 — STORES GAPS

Eight things a stores function does that no specification covers.

## 3.1 Stock reservation against a job order

Material can be allocated to a chamber before it is issued. Nothing reserves
it, so two job orders can both plan to use the same last compressor.

**Needed:** reservation at MIR approval, released on issue or cancellation, and
AVAILABLE reduced by reserved quantity in every projection that matters.

**Question:** does SESS want reservation, or is first-come-first-served at the
issue counter how it actually works?

## 3.2 ABC classification

Cycle counting is specified as A monthly, B quarterly, C half-yearly. **Nothing
defines how an item becomes A, B or C.**

**Needed:** classification by annual consumption value, recomputed periodically,
overridable by the Stores Manager with a reason.

Without it the cycle-count specification cannot run.

## 3.3 Shelf life and expiry

Some items expire: refrigerant in small packs, adhesives, sealants, silicone,
thread lock, calibration gases.

Nothing records a shelf life or blocks issue of expired stock.

**Question:** does SESS hold anything with a shelf life? If yes, which
categories, and should expired stock block issue or only warn?

## 3.4 Bin capacity and put-away rules

Racks and bins exist. Nothing limits what fits, or suggests where something
should go.

At SESS's volume this may not matter. I raise it so it is a decision rather
than an oversight.

## 3.5 Packing material

Chambers are dispatched in crates with foam, strapping and desiccant. That is
stock, consumed at dispatch, costed to the job.

Nothing specifies it. **Question:** is packing material stocked and issued
against a job order, or bought and consumed without passing through stores?

## 3.6 Consignment stock

A vendor keeps material at SESS, owned by the vendor until used.

Foundation 2 has `VENDOR_CONSIGNMENT` as an ownership type. No workflow uses
it.

**Question:** does SESS hold any consignment stock today, or was that
anticipated for later?

## 3.7 Weighbridge and weight capture

Job work reconciles by weight. Powder coating and CNC returns are weighed.

Nothing specifies where the weight comes from. **Question:** is there a
weighbridge or a platform scale, and is the weight typed or captured?

## 3.8 Non-moving and slow-moving provisioning

Frozen: the CFO approves provisions for slow and non-moving stock.

The report is specified (item 15). The provision workflow is not — who
proposes, who approves, what the accounting effect is, and whether the material
stays usable.

**Needed:** a provision workflow, because an ISO auditor and a statutory
auditor will both ask.

---

# PART 4 — WHAT ISO 9001 REQUIRES

SESS is certified. These two departments carry specific obligations. This is a
general reading of ISO 9001:2015 — your auditor may ask for things I cannot
predict, and where you already have a documented procedure, it governs.

## 4.1 Clause 8.4 — Externally provided processes, products and services

This is the purchasing clause and the most heavily audited.

| Requirement | In the ERP? |
|---|---|
| Criteria for evaluating and selecting suppliers | **specified, item 23** — not built |
| Criteria for monitoring performance | **specified, item 23** — not built |
| Criteria for re-evaluation | **specified, item 23** — not built |
| Records of evaluation and any actions arising | **specified** — not built |
| Verification of purchased product | **built** — QC inspection |
| Communicating requirements to suppliers | **partial** — the RFQ carries them; the terms-compliance comparison is not built |
| Control of externally provided processes | **specified, item 36** — job work, not built |

**The whole of 8.4 is specified and none of it is built.** That is the largest
ISO exposure in these two departments.

Your current vendor rating spreadsheet scores every vendor at 95 per cent. An
auditor who looks at it will ask why no supplier has ever been marked down, and
the honest answer is that four of five dimensions are typed by hand.

## 4.2 Clause 7.1.5 — Monitoring and measuring resources

| Requirement | In the ERP? |
|---|---|
| Calibrated or verified at specified intervals | **specified, item 37** — not built |
| Identified to determine status | **specified** — not built |
| Safeguarded from adjustment or damage | procedural, not software |
| Records of calibration | **specified** — not built |
| Action when equipment found unfit | **not specified** — see below |

**Gap I have not covered:** when an instrument is found out of calibration,
ISO requires you to assess the validity of previous measurements made with it.
If the vernier was wrong, every dimension it measured since the last valid
calibration is suspect.

**Needed:** a back-assessment path — when a calibration fails, list every
measurement taken with that instrument since its last valid certificate, so QC
can decide what to re-check.

That is a real ISO requirement and it is in none of the five specifications.

## 4.3 Clause 8.5.2 — Identification and traceability

| Requirement | In the ERP? |
|---|---|
| Identify outputs throughout production | **built** — serial and lot provenance |
| Identify status with respect to inspection | **built** — QC_HOLD, AVAILABLE, PENDING_RETURNABLE_DC |
| Control unique identification where traceability is required | **built** |
| Retain documented information | **built** — immutable ledger |

**This clause is fully covered.** The component ancestry report answers it
completely.

## 4.4 Clause 8.5.4 — Preservation

| Requirement | In the ERP? |
|---|---|
| Preserve outputs during production and delivery | **partial** |
| Includes identification, handling, contamination control, packaging, storage, protection | packaging and storage are gaps 3.4 and 3.5 above |

## 4.5 Clause 8.7 — Control of nonconforming outputs

| Requirement | In the ERP? |
|---|---|
| Identify and control nonconforming output | **built** — QC rejection |
| Correction, segregation, return, suspension | **built** — rejection to PENDING_RETURNABLE_DC |
| Concession by authority | **built** — TD only, with provenance |
| Records of nonconformity, action, concession, and the authority deciding | **built** |

**Fully covered, and better than most ERPs.** The concession provenance that
carries through to the delivered chamber is exactly what 8.7 asks for.

## 4.6 Clause 7.5 — Documented information

| Requirement | In the ERP? |
|---|---|
| Creation and updating — identification, format, review and approval | **partial** — drawings have revision control; procedures do not |
| Control — available where needed, protected, version controlled, retained, prevented from unintended use of obsolete versions | **gap** |

**The gap:** your quality manual, SOPs and work instructions are not in the
ERP. Drawings are. Forms like F/OP3/3 are not.

**Question:** do you want document control in the ERP, or does it stay where it
is today? It is a real module — revision, approval, distribution, obsolete
marking, acknowledgement — and it is not in any of the five specifications.

## 4.7 Clause 8.5.3 — Property belonging to customers or external providers

| Requirement | In the ERP? |
|---|---|
| Identify, verify, protect and safeguard | **specified, item 41** — not built |
| Report loss, damage or unsuitability to the customer | **not specified** |

**Gap:** if a customer's machine is damaged at SESS, ISO requires you to tell
them and keep the record. Item 41 does not cover damage.

---

# PART 5 — WHAT I RECOMMEND

## Build before go-live

Nothing in Parts 2 and 3 blocks the first day. SESS can run Purchase and Stores
without any of it.

## Build before the next ISO audit

| Item | Why |
|---|---|
| Vendor rating and qualification (item 23) | the whole of clause 8.4 |
| Calibration register (item 37) | clause 7.1.5 |
| **Calibration back-assessment** | 7.1.5, and not yet specified |
| Job-work inspection (item 36) | 8.4 control of external processes |
| Customer property damage reporting | 8.5.3, and not yet specified |

## Decide, then schedule

| Gap | Decision needed |
|---|---|
| Reorder-driven PR | does SESS buy to reorder level at all? |
| Payment scheduling | does it live here or in Accounts' existing system? |
| Stock reservation | does SESS reserve, or is it first come first served? |
| Shelf life | does SESS hold anything that expires? |
| Consignment stock | does SESS hold any today? |
| Document control | does the quality manual belong in the ERP? |
| Packing material | stocked and issued, or bought and consumed? |

## Build when convenient

ABC classification, emergency purchase counting, minimum quotation count,
transporter master, vendor communication log, import document set, non-moving
provision workflow, bin capacity.

---

# PART 6 — QUESTIONS FOR THE TECHNICAL DIRECTOR

Twelve, in the order that matters.

1. **Document control.** Does the quality manual, SOPs and work instructions
   belong in this ERP? It is a module in its own right.
2. **Payment scheduling.** In this ERP, or in whatever Accounts uses today?
3. **Reorder-driven purchasing.** Does SESS buy to reorder level, or only to a
   job order?
4. **Stock reservation.** Reserve at MIR approval, or first come first served?
5. **Shelf life.** Does SESS hold anything that expires?
6. **Consignment stock.** Any today?
7. **Emergency purchase.** Who may raise one?
8. **PO amendment after issue.** What may change, and must the vendor
   reconfirm?
9. **Packing material.** Stocked and issued to a job, or bought and consumed?
10. **Weight capture.** Weighbridge, platform scale, or typed?
11. **Inbound insurance.** All purchases, imports only, or none?
12. **Purchase budget.** Is the approval matrix the only spend control?

Answer these and every remaining gap has a decision behind it. Then nothing in
Purchase or Stores is unaccounted for — which is the standard this project has
held since 27 August.
