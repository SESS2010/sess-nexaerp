# SESS NexaERP — Sales Module

Date: 13 September 2026
Decided by: A. Paramananthan, Technical Director
Status: **specification only — not for building before October 2026**

Written after reading four real offers: a chamber offer `SESS/OFR/26-0234`, an
AMC offer `SESS/111/R1/UCAL/2026-27`, its matching CAMC, and a Proprietorship
CAMC. The structure here is what SESS actually issues.

---

## Why Sales matters to what is already built

| Today | With Sales |
|---|---|
| Customer PO typed in by hand | Raised from a won offer |
| Job Order links to a PO line typed in | Traced to the offer behind it |
| Contract review done outside the ERP | Generated from offer and PO together |
| FAT parameters typed by QC | Read from the offer's committed specification |
| **Commercial variance compares Estimated BOM to Actual BOM** | **Compares OFFER PRICE to actual cost — profit or loss per machine** |

The last one is why this module exists. Everything built in Purchase and Stores
since 27 August was to make it possible.

---

# BUILT IN TWO STAGES

**Stage 1 — the offer arrives as a PDF.** The Technical Director writes it as he
does today and uploads it. Everything downstream works: funnel, follow-up,
contract review, AMC tracking, offer-versus-actual. **35 to 45 engineering
days.**

**Stage 2 — the ERP writes the offer.** An AI document writer produces the Word
document from the data sheet; the TD edits it, uploads it back, and the ERP
converts it to PDF and emails it. **A further 40 to 60 days.**

**Stage 1 first.** It delivers every business outcome three months earlier, and
stage 2 is then built against real offers, a real price list and a proven
template rather than against guesses.

---

# 1. FOUR KINDS OF OFFER

| Kind | Pages | Prepared by | Approved by | Payment default |
|---|---|---|---|---|
| **Chamber** — finished goods | 12 | TECHNICAL_DIRECTOR | TECHNICAL_DIRECTOR | 70% advance, 30% before delivery |
| **Spare** | 4 | SERVICE_COORDINATOR | TD or SERVICE_MANAGER | 70/30 or 100% advance |
| **Service** — per visit | short | SERVICE_COORDINATOR | TD or SERVICE_MANAGER | per visit |
| **AMC / CAMC** | 6 | SERVICE_COORDINATOR | TD or SERVICE_MANAGER | AMC 100% advance; CAMC 50/50 |

**Chamber offers are the Technical Director's alone today**, both preparation
and approval. When a salesperson joins, those separate into two permissions.
Design it that way from the start.

---

# 2. NUMBERING

Two formats are in live use today. **Going forward, one:**

```
<COMPANY>/<TYPE>/<FY>-<SERIAL>[-R<n>]
```

| Example | Meaning |
|---|---|
| `SESS/OFR/26-0234` | chamber offer, Pvt Ltd |
| `SESS/SPR/26-0045-R1` | spare offer, stage 2 |
| `SESS/AMC/26-0111` | AMC |
| `SESS/CMC/26-0112` | CAMC |
| `SESS/SRV/26-0089` | service visit |
| `SPRO/OFR/26-0034` | chamber offer, Proprietorship |

**Why this shape:**

- **No customer name in the number.** The old AMC format
  `SESS/111/R1/UCAL/2026-27` embeds it; if a customer renames or is acquired,
  the number becomes wrong.
- **Type is visible.** Anyone reading a number knows what document it is.
- **Serial resets per financial year, per type, per company.**
- **Revision last**, so numbers sort correctly.

**Offers already issued keep their existing numbers.** The ERP records history
as it is and does not renumber.

---

# 3. ENQUIRY

**Arrives by** phone, email or WhatsApp.

**Every enquiry is registered before anything else, and every registered enquiry
becomes an offer.** Without a proper enquiry it is not entered at all. So the
funnel starts at *offer submitted*, and there is no category of enquiry that
never became one.

**An enquiry that dies** is handled by **cancelling the offer number**, not by
deleting the enquiry. The number is consumed and the cancellation recorded.

**Captured:** customer, contact, arrival channel, date, region, brief
requirement.

---

# 4. THE DATA SHEET

**One standard data sheet per chamber type.** Today these live in Google
Sheets; they move into the ERP.

| Chamber type |
|---|
| Climatic test chamber |
| Walk-in climatic test chamber |
| Thermal shock test chamber |
| Altitude test chamber |
| Rain test chamber |
| Vibration-combined climatic chamber |
| others as SESS adds them |

**How it works:**

1. Enquiry registered
2. ERP generates a link, emailed to the customer
3. The customer opens it — **no login, no portal account**
4. They fill it in, mostly by selecting options
5. The answers land against that enquiry

**If the customer will not fill it**, they tell SESS by phone and SESS fills it
in. Both paths end in the same record, and the ERP records which was used.

**Adding a chamber type must be a screen, not a code change.**

**Selection over free text.** Dropdowns, checkboxes and numbers, with one
remarks field at the end. Customers do not write paragraphs.

---

# 5. CHAMBER OFFER

## Structure — twelve pages

| Page | Content |
|---|---|
| 1 | Cover — model, customer, offer number, date, validity, product image |
| 2 | Prepared by — SESS address and contact |
| 3 | Cover letter |
| 4 | **Commercial quotation summary** — lines, GST, grand total, amount in words |
| 5 | Optional packages, priced separately |
| 6 | Scope of supply; main technical specifications |
| 7 | Integration and mechanical movement |
| 8 | Construction and refrigeration |
| 9 | Control system and electrical load |
| 10 | Utility basis |
| 11 | Installation, commissioning, acceptance; warranty |
| 12 | Commercial terms; bank details; exclusions; signature; **customer acceptance block** |

## Three line types

`SESS/OFR/26-0234` uses all three:

| Type | Behaviour | Real example |
|---|---|---|
| **BASE** | counts toward the total | chamber ₹42,50,000; vibration package ₹18,00,000; freight ₹1,00,000; installation ₹3,00,000 |
| **OPTIONAL** | priced, **excluded from the total** | battery-test safety package ₹9,90,000 |
| **ALTERNATIVE** | choose one of a set, excluded until chosen | CO₂ suppression E1 ₹2,50,000 **or** FirePro E2 ₹1,80,000 |

**The offer total is BASE lines only.**

**An alternative set must refuse a second selection.** E1 or E2, never both.

## Money

| | |
|---|---|
| India | **INR** |
| Abroad | **quoted in USD, paid in INR** — the bank converts |
| GST | **18%, shown separately**, prevailing rate at dispatch applies |

Stored value for the funnel is **INR**. A USD quote also carries its USD figure.

Real example: basic ₹64,50,000 + GST ₹11,61,000 = **₹76,11,000**.

**Amount in words is generated, never typed.**

## Standard terms — template defaults, each editable

| Term | Default |
|---|---|
| **Payment** | **70% advance with the PO, 30% before delivery** |
| Late payment | interest at 3% per month |
| Price basis | delivered to customer gate, Chennai; unloading and civil work excluded |
| Packing and forwarding | SESS scope |
| Freight | SESS scope within the quoted transit allowance |
| Insurance | customer scope |
| **Delivery** | **16 to 20 weeks** from confirmed order, advance payment and approved GA |
| Installation | minimum 15 days after delivery and site readiness |
| **Validity** | **30 days** |
| Jurisdiction | Chennai only |
| Order amendment | changes to specification, model, quantity or integration entail price and delivery revision |

## Warranty — outbound, dual-basis

**12 months from commissioning, or 13 months from dispatch, whichever occurs
first.**

**This is not the inbound warranty already built.** The GRN rule is bill date
plus 13 months for material SESS buys. This is for machines SESS sells, and it
has two clocks running.

**Exclusions** are part of the template: refrigeration oil and filter driers,
flexible thermal connectors and wear parts, hydraulic oil and seals, damage from
overload or misuse, and preventive-maintenance non-compliance.

---

# 6. SPARE OFFER

## Structure — four pages

| Page | Content |
|---|---|
| 1 | Introduction and machine identification |
| 2 | Spare list — part, description, quantity, rate, amount |
| 3 | Commercial terms |
| 4 | **Inspection-staging note** |

## Pricing

**Most recent purchase price plus 50 per cent margin, by default.**

**The margin is editable per line and per offer.** Some customers get a
different number.

Where a spare has never been purchased, there is no rate — the price is entered
by hand.

## Inspection staging — the rule that matters

A machine cannot be fully run during inspection, for safety. So the first offer
covers **only what inspection could see**.

```
Inspection — machine cannot be fully run
     ↓
SESS/SPR/26-0045       Stage 1 — visible defects
     ↓
Machine runs after repair
     ↓
SESS/SPR/26-0045-R1    Stage 2 — what running revealed
     ↓
SESS/SPR/26-0045-R2    Stage 3 — if needed
```

**Each stage is a REVISION of the same offer**, with the stage stated inside the
document.

**Why revisions and not new offers:** the customer sees one continuing
conversation about one machine rather than a series of unrelated quotes.

**The staging note must be on every spare offer**, because an ageing machine —
five, ten years — will reveal more once it runs. Fixing one component often
exposes the next.

## Warranty

**None, by default.**

**Major components get a warranty against a recorded serial number.** That
serial is the identity; without it there is nothing to honour.

## Payment

**70/30 or 100% advance. Editable.**

---

# 7. SERVICE OFFER

**Priced per visit, per day.**

| Element | Treatment |
|---|---|
| Engineer visit charge | per day |
| Travel | **extra** |
| Spares | **extra** |

Nothing is bundled. Each is quoted and billed separately.

---

# 8. AMC AND CAMC

Both run six pages and share a structure. **The difference is spares and
payment.**

## Structure

| Page | Content |
|---|---|
| 1 | Covering letter |
| 2 | **Contract form — ORIGINAL / DUPLICATE**: equipment, HSN, quantity, rate, contract period, and signature blocks for both parties |
| 3 | Terms — preventive maintenance scope, breakdown definition |
| 4 | Payment terms, spares coverage, exclusions |
| 5 | Cancellation, relocation, response time, payment method |
| 6 | Response-time guarantee, spare availability, training, customer feedback |

## The contract form is an agreement, not an offer

> "Please send one signed copy along with your Purchase Order."

**The customer signs and returns it with the PO.** That signed copy is the
contract.

**The ERP must hold the signed scan**, not only the offer that was sent. Until
it arrives the AMC is not live.

## Visits

| | Default | Editable |
|---|---|---|
| Preventive maintenance calls | **4** | yes |
| Breakdown calls | **1** | yes |
| Additional visits | **₹5,000 per day + GST** | yes |

Some customers take two preventive visits, some four. Some take two breakdowns.
**Every count is per contract.**

## Contract period

**One, two or three years**, at the customer's request. Visit counts are stated
per year.

## AMC — spares

**A running balance of ₹3,000 per machine per year.** Consumables and emergency
items such as fuses.

```
Q1: ₹300 consumed    → ₹2,700 remaining
Q2: ₹1,500 consumed  → ₹1,200 remaining
Q3: ₹1,200 consumed  → ₹0 remaining
Q4: anything further → a spare offer is raised
```

**The ERP tracks the balance and warns when it is exhausted.** Today somebody
has to remember.

**Everything else is excluded** — refrigeration compressor, PLC and controllers,
sensors, refrigerant, solenoid valves, copper tubes, expansion valves, driers,
electrical spares, cables, fan motor rewinding, PC and monitor, air compressor,
vacuum pump, coils, filters.

## CAMC — spares

**A specified list is included free of charge.** From the real CAMC:

| Covered |
|---|
| R23 / R404A refrigerant; R410a for chillers |
| Refrigeration line solenoid valve |
| Refrigeration line and copper tubes |
| Expansion valves |
| Suction and discharge line vibrators |
| Drier |
| Copper and silver brazing rods |
| Gas charging and leak testing tools |
| Contactors, relays, OLP, MCB, SMPS, fuses, SSR |
| Control panel electrical cables |
| Condenser and air circulation fan motor, chiller pump — rewinding or repair only |

**This list must be editable per contract.** What is covered and what is not
varies by customer and by machine.

**Defective parts must be returned.** A free replacement is conditional on the
old part coming back — the same custody principle already in the Stores module.

## Payment

| | Default |
|---|---|
| **AMC** | 100% advance with the PO |
| **CAMC** | 50% advance against proforma invoice; 50% after two consecutive quarterly visits |

**Both editable.** Some customers pay 100% advance on a CAMC; some pay the
balance after the second or third visit.

## Operational terms

| | |
|---|---|
| Response time | **24 to 48 hours** from a breakdown call |
| Breakdown reporting | **tech-support@sess.co.in** and 9444427748 |
| Relocation | 15 days' notice; charges may be revised |
| Customer cancellation | 30 days' written notice, with deduction by elapsed duration |
| Transferable | **no** |
| NABL calibration | extra |
| HSN | 998719 |
| Payment method | demand draft or online to a nationalised bank |

## Renewal

**Remind one month before expiry — both SESS and the customer.**

With 156 machines supplied since 2014, renewals are a recurring revenue stream.
A missed renewal is a lost sale that nobody notices.

## Visit completion triggers billing

```
AMC visit completed
     ↓
ERP notifies Accounts automatically
     ↓
Invoice raised within one week
     ↓
If not, it stays on the pending list
```

**Today this is verbal.** The engineer finishes, somebody tells Accounts, and
the invoice may or may not follow. In the ERP a completed visit puts itself on
a billing list and cannot be forgotten.

---

# 9. FOLLOW-UP — THE TEN STAGES

These are the **customer's** internal process, not SESS's.

| # | Stage |
|---|---|
| 1 | Offer submitted |
| 2 | Customer technical team evaluation |
| 3 | Customer manager evaluation |
| 4 | Top management technical evaluation |
| 5 | Budget allocation |
| 6 | Purchase request raised — the customer's internal PR |
| 7 | Commercial department, initial terms |
| 8 | Commercial negotiation, final |
| 9 | PO under process |
| 10 | PO released |

**Stage 9 is roughly 90 per cent probability.**

## The automated email

**Every Monday**, for every open offer, from **info@sess.co.in**.

A **temporary link specific to that offer**. No portal account. The customer
ticks the current stage and adds a remark if they want to. That updates the ERP
directly.

## Stopping

| Trigger | Effect |
|---|---|
| Customer marks it **won** | stops, PO expected |
| Customer marks it **lost** | stops, loss reason captured |
| **Two years** elapsed | stops automatically |
| SESS closes it by hand | stops |

## Six weeks of silence

Notify SESS. The Technical Director calls and updates the stage by hand. **Do
not stop the email** — notify, and let a person decide.

---

# 10. WHEN AN OFFER IS LOST

The customer ticks **not selected**, then chooses:

| Reason |
|---|
| Price high |
| Technical specification not met |
| Delivery period |
| Other — with a remark |

**If price:** ask for the L1 price, **or the percentage difference**. Some give
a figure, some say "yours was 15 per cent higher". Accept both.

**Never mandatory:**

> "If you are able to share the L1 price, we will try to meet it next time."

**Competitor name** — optional, valuable when given.

**Tick boxes, not essays.**

---

# 11. CUSTOMER PO AND CONTRACT REVIEW

**The customer PO is entered by hand and its scan uploaded.** Customers do not
send structured data.

**Linked to the offer by hand** — nothing in a customer PO reliably carries the
offer number.

**The contract review is then generated, AI-assisted**, comparing offer against
PO line by line: parameters compared, deviations highlighted, the TD edits the
draft, customer agreement by email or phone recorded, and a **line-count check**
that catches a PO line missing from the review.

**Every line must be checked.** The EMR case proved why — a cooling tower on the
PO was absent from the contract review entirely.

---

# 12. THE CUSTOMER PORTAL

## Who gets one

| | Access |
|---|---|
| **AMC, CAMC and warranty customers** | **portal account with login** |
| Everyone else | **temporary link only**, per purpose |

An enquiry customer has one offer and one follow-up. They will not create an
account, and should not be asked to.

An AMC customer has a continuing relationship — machines, visits, renewals,
complaints. A portal earns its place.

## One login, two companies

A customer may own machines bought from **both** SESS companies.

```
UCAL — one customer, one portal login
    ↓
Machine SN-1  →  purchased from SESS Pvt Ltd
Machine SN-2  →  purchased from SESS Proprietorship
    ↓
each machine shows its own company's documents
```

**The serial number is the identity.** Inside the portal, each machine carries
its company, and its offers, invoices, AMC and visit reports come from that
company's ledger.

## Inside SESS, the ledgers are separate

**This is the opposite of the portal, and deliberately so.**

| | |
|---|---|
| SESS staff | **one company at a time.** Complaint register, service activity, spare offers, service offers — all company-scoped, as already built |
| Customer | **one login**, machines from both companies visible together |

SESS needs separate ledgers for accounting, GST and audit. A customer does not
care which of two entities sold them a chamber — they care about the chamber.

## What a portal customer sees

| |
|---|
| Their machines — model, serial, company, purchase date |
| Warranty or AMC status and expiry |
| **AMC spare balance** — "₹1,200 of ₹3,000 remaining" |
| Visit history and service reports |
| Raise a complaint |
| Invoices |
| Feedback |

**The AMC contract already promises a feedback link.** It does not exist yet;
the portal is where it lives.

---

# 13. REGIONS AND OWNERSHIP

**One region per Indian state**, plus international.

Today: Chennai, Bangalore, Hyderabad, Pune, Gujarat, Delhi, Kolkata, Odisha,
Madhya Pradesh — and growing.

**A region master maintained from a screen.** SESS will add regions; that must
never require a developer.

**Each region has a salesperson who owns its offers.** Today the Technical
Director holds every region. As SESS hires — a marketing person, then a
salesperson per region — ownership moves.

**Still to decide:** what a salesperson sees of another region's offers.

---

# 14. THE DASHBOARD

## The funnel — two views

| By count | By value |
|---|---|
| how many customers at each stage | how much rupee value at each stage |

Ten customers at negotiation is one story. Ten customers worth ₹1.2 crore is
another, and so is two customers worth the same.

## Year comparison

**Current year plus the previous two.** The ERP is new, so those years are not
in it — **the Technical Director will import them** from the existing offer
ledger using a download template.

## Five-year turnover chart

Separate from the funnel — **completed sales**. Older years can be typed as five
rows.

## The map

**Offers plotted geographically:** India by district, worldwide for exports.
Count per area. Where SESS is winning and where it is absent, on one screen.

## Loss analysis

> "Twelve lost this quarter — seven on price, three on delivery, two on
> technical."

## AMC renewals due

Within 30, 60 and 90 days. With 156 machines, this is a standing sales list.

---

# 15. EMAIL

| Address | Purpose |
|---|---|
| **info@sess.co.in** | data sheet links, offers, Monday follow-up, loss-reason links |
| **tech-support@sess.co.in** | breakdown calls, service correspondence, spare offers |

**Every email sent is recorded** against the offer or contract — when, to whom,
which document. When a customer says "we never received it", that is the answer.

**Needed before building:** SMTP host, port, authentication and credentials for
both addresses. SURANTHER holds these.

---

# 16. STAGE 2 — THE AI DOCUMENT WRITER

**Build only after stage 1 is in use.**

```
Data sheet + customer input
        ↓
AI writes the offer in the SESS standard format
        ↓
Word document
        ↓
TD edits it in Word
        ↓
Uploaded back to the ERP
        ↓
ERP converts to PDF
        ↓
Emailed with a covering letter
```

| Section | Source |
|---|---|
| Cover letter, scope, clarifications | generated |
| Main technical specifications | **directly from the data sheet** |
| Construction, refrigeration, control, utilities | generated per chamber type |
| Installation, warranty, commercial terms, bank details, exclusions | **fixed template** |
| **Prices** | **entered by the TD, line by line** |
| Cover image | **uploaded by the TD** |

**Prices are never generated.** The TD holds a price list per model and adjusts
each offer to the customer. That judgement does not automate.

## Cloud and local, both

**Cloud when the internet is available. Local on the PC when it is not.** SESS
must be able to produce an offer during an outage.

**Report before building:** whether a local model can produce this quality on
SESS hardware, and what hardware it would need. Without a GPU, a local model may
take fifteen minutes for twelve pages where cloud takes thirty seconds.

**This feature is for SESS only.** Customers who buy NexaERP do not write offers
with it, so the cloud dependency is not a product concern.

---

# 17. EFFORT

| Stage | Days |
|---|---|
| **1 — enquiry, data sheet, four offer types, funnel, follow-up, contract review, AMC tracking, portal, dashboards** | **35-45** |
| **2 — AI writer, PDF conversion, email dispatch** | **40-60** |

---

# 18. STILL TO DECIDE

| Question |
|---|
| Proprietorship company prefix — `SPRO`, or something else |
| On expiry at thirty days — a new offer, or a revision? |
| What does a salesperson see of another region's offers? |
| How many years of historical offer data will be imported? |
| Does the contract review belong to the salesperson, or to the TD alone? |
| SMTP settings for both addresses |
| Whether SESS hardware can run a local model at usable speed |
| Are CAMC returned defective parts tracked as customer property, or simply recorded? |

---

## What is still needed before building

**The data sheet.** The Technical Director will supply the existing Google Sheet
for one chamber type; the ERP form is built from it and the remaining types
follow the same shape.

**Until that arrives the form cannot be designed.** Everything else in this
document can be.
