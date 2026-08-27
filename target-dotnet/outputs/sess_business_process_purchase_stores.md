# SESS Business Process Specification - Purchase and Stores

## 1. Authority, scope, and current reality

This document records the decisions made by the Technical Director.

- Purchase and Stores run on paper and Excel today.
- REV861 holds some data but is not the working system.
- This ERP is a **new process, not a migration**.
- Document only: no code, migration, or database access.

## 2. Schema-support notation

Support statements refer only to current repository source inspected for this document; no database was accessed.

| Mark | Meaning |
|---|---|
| **Supported** | A dedicated current schema model exists. |
| **Partial** | Some relevant fields/models exist, but not the complete requirement. |
| **NO SCHEMA SUPPORT TODAY** | No dedicated current schema model exists. |

Current source supports company-scoped foundations and Purchase schema for PR, stock check/reservation/handoff, RFQ, vendor quotation, technical verification, commercial comparison, PO, and a minimal follow-up handoff. Item, warehouse, rack/bin, QC-policy, and minimal stock-movement foundations also exist. Operational Stores processes after follow-up do not have dedicated schema today.

## 3. Multi-company boundary

Two companies: SESS Pvt Ltd and SESS Proprietorship. Separation is absolute. After login, the user selects a company at the top of the ERP. Everything below shows only that company's stores, PR, PO, GRN, job orders, BOM, AMC, warranty, uploads, and downloads. Each company has separate physical stores.

Employees are the only shared thing. Hours are charged to the company whose work it was. SESS Pvt Ltd bills SESS Proprietorship for labour.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | User | Selects one company at the top of the ERP after login. | None stated. | Company context | **Partial** - company foundations exist. |
| 2 | ERP | Restricts every listed area to that company. | Mandatory separation; neither approval nor notification. | Company-scoped view/transaction | **Partial** - current Purchase/foundations are scoped; several listed areas have no schema. |
| 3 | Stores | Operates physically separate stores for each company. | None stated. | Company store | **Supported** for company-scoped warehouse foundations. |
| 4 | Employee | Records hours against the company whose work was performed. | None stated. | Company time charge | **NO SCHEMA SUPPORT TODAY**. |
| 5 | SESS Pvt Ltd | Bills SESS Proprietorship for labour. | None stated. | Inter-company bill | **NO SCHEMA SUPPORT TODAY**. |

## 4. Approval matrix

This matrix is already implemented.

| Amount | Required approval sequence |
|---:|---|
| Below 5,000 | Department Manager only |
| 5,000 to 100,000 | Department Manager, then `TECHNICAL_DIRECTOR` |
| Above 100,000 | Department Manager, then `MANAGING_DIRECTOR` |

Level 1 follows the requesting department: production-side departments go to `PRODUCTION_MANAGER` (SESS-25); office-side departments go to `ACCOUNTS_MANAGER` (SESS-14).

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | ERP | Resolves department side and amount band. | Routing only. | Route snapshot | **Supported**. |
| 2 | SESS-25 or SESS-14 | Performs Level 1 review. | **APPROVAL REQUIRED.** | Level 1 approval | **Supported**. |
| 3 | Technical Director | Reviews 5,000 to 100,000 after Level 1. | **APPROVAL REQUIRED.** | Level 2 approval | **Supported**. |
| 4 | Managing Director | Reviews above 100,000 after Level 1. | **APPROVAL REQUIRED.** | Level 2 approval | **Supported**. |

## 5. Purchase flow

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Design, TD, refrigeration, electrical, PLC engineer, service engineer, admin, Purchase, or Stores | Identifies need from project BOM, reorder level, or service requirement. Anyone listed may raise it. | None. | Purchase need | **Partial** - PR references/reorder fields exist; estimated BOM and service requirement do not. |
| 2 | Person with need | Raises PR. | **APPROVAL REQUIRED:** raiser's own manager first, then TD or MD by amount. | PR | **Supported**. |
| 3 | PRIYA E (SESS-15, `PURCHASE_MANAGER`) | Sends RFQ to vendors. | None stated. | RFQ/invitations | **Supported**. |
| 4 | Purchase | Records vendor offers. | None stated. | Vendor quotations | **Supported**. |
| 5 | PR raiser: engineer, design, Production Manager, Stores, or admin | Performs technical verification. | Verification, not stated as approval. | Verification | **Partial** - verification exists; enforcing verifier = PR raiser is not fully represented. |
| 6 | ERP | Automatically produces comparison and recommendation using the rules below in order. | Recommendation only. | Comparison chart/recommendation | **Partial** - comparison fields exist; exact ordered rules are not dedicated schema. |
| 7 | Human final decision-maker | Makes final choice and records reason. | Human decision mandatory. | Selection/reason | **Partial** - fields exist; decision-maker role is unstated. |
| 8 | Comparison approver | Approves comparison. | **APPROVAL REQUIRED.** | Approved comparison | **Supported**; approver rule is open. |
| 9 | Purchase | Creates PO. | None at creation. | PO | **Supported**. |
| 10 | PO approver | Approves PO. | **APPROVAL REQUIRED.** | Approved PO | **Supported**; exact routing is open. |
| 11 | Purchase | Issues approved PO. | Approval prerequisite. | Issued PO | **Supported**. |
| 12 | Purchase | Follows up material. | None stated. | Follow-up | **Partial** - only minimal handoff exists. |

### Comparison rules, in order

1. Technically not qualified: reject regardless of price.
2. Lead time unacceptable for urgency: reject.
3. Among the rest, prefer warranty even at 12% higher price.
4. Otherwise lowest price.

Typically four vendors are compared. The ERP recommends. A human chooses finally and must record the reason.

## 6. Invoice-price mismatch

There is no middle ground. If vendor invoice price differs from PO price, either reject and replace the invoice or revise the PO. Nothing else is accepted. Actual BOM cost is always the accepted bill value.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Bill-entry user / ERP | Compares invoice and PO prices. | None stated. | Match result | **NO SCHEMA SUPPORT TODAY** for invoice/bill entry. |
| 2A | Bill-entry user / vendor | Rejects and replaces mismatched invoice. | None stated. | Rejected/replacement invoice | **NO SCHEMA SUPPORT TODAY**. |
| 2B | Purchase | Revises PO instead. | Applicable PO process; no extra rule stated. | Revised PO | **Partial** - PO revision exists; mismatch linkage does not. |
| 3 | ERP | Accepts no other treatment. | Mandatory control. | Match evidence | **NO SCHEMA SUPPORT TODAY**. |
| 4 | ERP | Posts accepted bill value to Actual BOM. | None stated. | BOM cost | **NO SCHEMA SUPPORT TODAY**. |

## 7. Inbound flow

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Stores today; security later | Makes gate entry. | None stated. | Gate entry | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Stores / ERP | Checks Item master; GRN cannot be entered for a nonexistent item. | Mandatory prerequisite. | Item check | **Partial** - Item exists; GRN does not. |
| 3 | Item creator | First creates missing item with internal barcode, model number, manufacturer part number, HSN, GST, UOM, category. Item master does not hold serials. | None stated. | Item | **Partial** - listed fields largely exist; separate internal/manufacturer barcodes do not both exist. Item has no unit serial field, as required. |
| 4 | ERP / Stores | Uses manufacturer barcode if present, otherwise generates one; prints/fixes component sticker with one of six symbols: electrical; refrigeration; fasteners; PLC/instrumentation; fabrication (sheet metal, SS, MS); mechanical (lathe, CNC). | None stated. | Barcode sticker/symbol | **Partial** - barcode fields exist; precedence, generation, printing, stickers, and symbols have **NO SCHEMA SUPPORT TODAY**. |
| 5 | KAMALI (SESS-16), SUDALAI (SESS-35), or KARTHICK (SESS-41) | Enters GRN; records serial number and warranty period per unit to track component warranty. | None stated. | GRN/unit warranty | **NO SCHEMA SUPPORT TODAY**. |
| 6 | Stores | Moves material to QC rack; unavailable for issue. | Mandatory availability control. | QC-rack movement | **Partial** - QC-hold locations exist; receipt movement does not. |
| 7 | NARREN S (SESS-33, `QC_MANAGER`) | Inspects incoming material. ISO scope also covers CNC output, powder-coating output, in-process, QC sheets, FAT, and dispatch-document audit. | Inspection, not stated as approval. | QC evidence | **NO SCHEMA SUPPORT TODAY** for operational records; only policy/location foundations exist. |
| 8 | Stores / ERP | Moves accepted quantity to regular racks and makes available. | QC acceptance prerequisite. | Accepted-stock movement | **Partial** - foundations exist; acceptance posting does not. |
| 9 | Bill-entry user | Enters bill only for accepted quantity. | None stated. | Bill entry | **NO SCHEMA SUPPORT TODAY**. |
| 10 | Stores | Sends rejected quantity on returnable DC with reason `rejection - replacement request`. | **APPROVAL REQUIRED:** PR raiser first, then TD or MD for high value. | Returnable DC | **NO SCHEMA SUPPORT TODAY**. |
| 11 | Accounts / ERP | Holds payment until replacement arrives and DC closes. | Mandatory hold, not approval. | Payment hold | **NO SCHEMA SUPPORT TODAY**. |
| 12 | Stores | Receives replacement and closes DC. | None stated. | Receipt/closed DC | **NO SCHEMA SUPPORT TODAY**. |

## 8. Outbound issue flow

**Stores never issues material without an approved Internal Issue Request. There is no exception for any destination.**

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Requester | Raises Internal Issue Request. | None at creation. | Internal Issue Request | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Production Manager or department owner | Reviews request. | **APPROVAL REQUIRED.** | Approved request | **NO SCHEMA SUPPORT TODAY**. |
| 3 | Stores incharge | Issues only against approved request. | Mandatory approval; no exception. | Material issue | **NO SCHEMA SUPPORT TODAY**; only minimal movement foundation exists. |
| 4 | Stores / ERP | Records purpose and reference. | None stated. | Referenced movement | **NO SCHEMA SUPPORT TODAY** for these codes/references. |

| Purpose | Required meaning/reference |
|---|---|
| `FACTORY_ASSEMBLY` | To a job order |
| `PROJECT` | To a job order |
| `SERVICE` | Against customer PO, warranty, or sale bill |
| `WARRANTY` | Free replacement |
| `DEMO` | To client, usually returnable |
| `SALE` | Billed |
| `FREE_OF_COST` | Given free |

## 9. Delivery Challans

### Returnable

Material comes back: subcontract (powder coating, CNC, milling, lathe), rejected vendor material, and demo. DC stays `OUTSTANDING` until return is received.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Requesting department / Stores | Creates returnable movement. | Approved Internal Issue Request for every issue; rejection also follows Section 7. | Returnable DC | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Stores | Dispatches and keeps outstanding. | None stated. | Outstanding DC | **NO SCHEMA SUPPORT TODAY**. |
| 3 | Stores | Receives return and closes DC. | Close only on receipt. | Return/closed DC | **NO SCHEMA SUPPORT TODAY**. |

### Non-returnable

Material does not come back: warranty supply, bill through, and free of cost.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Requesting department / Stores | Creates non-returnable movement. | Approved Internal Issue Request remains mandatory. | Non-returnable DC | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Department owner | Reviews DC. | **APPROVAL REQUIRED.** | Approval | **NO SCHEMA SUPPORT TODAY**. |
| 3 | ERP / responsible user | Tells TD and MD because material leaves without payment. | **MANDATORY NOTIFICATION to both; not approval.** | Two notifications | **NO SCHEMA SUPPORT TODAY**. |
| 4 | Stores | Dispatches. | Approval and notifications are prerequisites. | Dispatched DC | **NO SCHEMA SUPPORT TODAY**. |

## 10. Subcontract

Powder coating, CNC cutting and folding, milling, and lathe work.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Requester | Raises Internal Issue Request. | **APPROVAL REQUIRED** by Production Manager or department owner. | Approved request | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Stores | Sends material on returnable DC. | Approved request required. | Outstanding DC | **NO SCHEMA SUPPORT TODAY**. |
| 3 | Vendor / Purchase | Vendor quotes; Purchase raises PO. No separate document type. | **Same PO approval flow.** | Quote/PO | **Partial** - quote/PO exist; subcontract linkage does not. |
| 4 | Stores | Receives material and closes DC. | None stated. | Return/closed DC | **NO SCHEMA SUPPORT TODAY**. |
| 5 | ERP | Posts labour charge to project BOM. | None stated. | BOM subcontract cost | **NO SCHEMA SUPPORT TODAY**. |

## 11. Scrap and write-off

Stores decides. TD and MD approval is mandatory.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Stores | Decides scrap/write-off. | Stores decision. | Decision | **NO SCHEMA SUPPORT TODAY**; scrap location is only a foundation. |
| 2 | Technical Director | Reviews. | **APPROVAL REQUIRED.** | TD approval | **NO SCHEMA SUPPORT TODAY**. |
| 3 | Managing Director | Reviews. | **APPROVAL REQUIRED.** | MD approval | **NO SCHEMA SUPPORT TODAY**. |
| 4 | Stores | Executes after both approvals. | Both approvals mandatory. | Stock movement | **NO SCHEMA SUPPORT TODAY**. |

## 12. Sales to Job Order

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Customer | Sends RFQ. | None. | Customer RFQ | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Sales / TD, sometimes ALFATHIMA (Accounts); VENKAT for service parts | Prepares an AI-assisted Word offer in ERP from standard model reference copies. | None stated. | Word offer | **NO SCHEMA SUPPORT TODAY**. |
| 3 | ERP / preparer | Looks up live-store cost by HSN or barcode, then adds margin percentage. | None stated. | Costed offer | **NO SCHEMA SUPPORT TODAY**; HSN/barcode are partial foundations. |
| 4 | Customer | Sends customer PO. | None. | Customer PO | **NO SCHEMA SUPPORT TODAY**. |
| 5 | Sales / TD | Compares offer against PO in an AI-assisted Word contract review. | Review; no internal approval stated. | Word contract review | **NO SCHEMA SUPPORT TODAY**. |
| 6 | SESS and customer | Agree. | Both sides must agree. | Agreed review | **NO SCHEMA SUPPORT TODAY**. |
| 7 | Responsible user | Creates Job Orders. | Agreement prerequisite. | Job Orders | **NO SCHEMA SUPPORT TODAY**. |

## 13. Job Order and BOM

Job Order and BOM are two different things. Job Order closes when the bill is entered. BOM stays `OPEN` until the warranty period ends.

### One Job Order per chamber

One per chamber is mandatory. A customer PO covering three chambers creates three Job Orders even for one lump-sum price. The offer already prices each chamber separately, so the internal breakup always exists.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Responsible user | Creates exactly one Job Order per chamber. | Mandatory rule. | Job Order(s) | **NO SCHEMA SUPPORT TODAY**; generic Project is not Job Order. |
| 2 | Responsible user | Records machine model number, internal machine serial number, and customer name. | None stated. | Job Order details | **NO SCHEMA SUPPORT TODAY**. |
| 3 | ERP | Closes Job Order when bill is entered. | None stated. | Closed Job Order | **NO SCHEMA SUPPORT TODAY**. |
| 4 | ERP | Keeps BOM open through warranty end. | Mandatory rule. | BOM status | **NO SCHEMA SUPPORT TODAY**. |

### Estimated versus Actual BOM

Estimated BOM is made at design time and drives offer price. Actual BOM records what was really consumed. ERP must show both side by side. This comparison is the primary business purpose of the system.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Design | Creates Estimated BOM at design time. | None stated. | Estimated BOM | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Sales / TD | Uses it to drive offer price. | None stated. | Offer price | **NO SCHEMA SUPPORT TODAY**. |
| 3 | ERP | Accumulates actual consumed costs. | None. | Actual BOM | **NO SCHEMA SUPPORT TODAY**. |
| 4 | ERP | Shows both side by side. | None. | Comparison | **NO SCHEMA SUPPORT TODAY**. |

Actual BOM includes:

- Material at accepted bill value
- Subcontract charges
- Production labour
- QC labour
- Installation labour
- Installation expenses: travel, hotel, food
- Warranty-period service spares and labour

AMC and CAMC are not in BOM. They are separate service activities with their own revenue and cost. All stated BOM requirements have **NO SCHEMA SUPPORT TODAY**.

## 14. Labour costing and task reporting

Rate = `monthly salary / 208 hours`, where `208 = 26 days x 8 hours`. No overhead loading: no factory rent, electricity, or admin. The same rate is used for inter-company billing.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Production Manager | Assigns factory-engineer tasks referencing Job Order number. | None stated. | Task | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Service Manager | Assigns service-engineer tasks referencing Job Order number. | None stated. | Task | **NO SCHEMA SUPPORT TODAY**. |
| 3 | Engineer | Records hours with morning and evening reporting. | None stated. | Time/task report | **NO SCHEMA SUPPORT TODAY**. |
| 4 | ERP | Applies recorded Job Order hours at monthly salary / 208, with no overhead. | None. | Labour cost | **NO SCHEMA SUPPORT TODAY**. |
| 5 | ERP | Charges work to the company whose work it was. | None. | Company charge | **NO SCHEMA SUPPORT TODAY**. |
| 6 | SESS Pvt Ltd | Bills SESS Proprietorship at the same rate. | None stated. | Inter-company bill | **NO SCHEMA SUPPORT TODAY**. |
| 7 | ERP | Feeds completed and pending task counts to performance/incentive view. | None. | Performance/incentive view | **NO SCHEMA SUPPORT TODAY**. |

## 15. Warranty, AMC, and CAMC

These three contracts must not be confused.

| Contract | Rule | Cost treatment | Schema support |
|---|---|---|---|
| `WARRANTY` | Free, included in project value | Cost goes to BOM | **Partial** - generic asset warranty dates exist; contract/service cost-to-BOM does not. |
| `AMC` | Customer pays; customer supplies material | Separate revenue and cost; not BOM | **NO SCHEMA SUPPORT TODAY**. |
| `CAMC` | Customer pays; SESS supplies consumables | Separate revenue and cost; not BOM | **NO SCHEMA SUPPORT TODAY**. |

## 16. E-way bill

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Responsible user / ERP | Generates e-way bill from this ERP, not separately on the GST portal. | None stated. | E-way bill | **NO SCHEMA SUPPORT TODAY**. |

## 17. Approval versus notification register

| Event | Control | Required party or parties |
|---|---|---|
| PR | **Approval** | Raiser's manager; then TD for 5,000 to 100,000 or MD above 100,000 |
| Comparison | **Approval** | Required; approver unstated |
| PO | **Approval** | Required; exact routing unstated |
| Every Internal Issue Request | **Approval** | Production Manager or department owner |
| Rejected-material return | **Approval** | PR raiser first; then TD or MD for high value |
| Non-returnable DC | **Approval** | Department owner |
| Non-returnable DC | **Mandatory notification, not approval** | TD and MD |
| Subcontract PO | **Approval** | Same PO approval flow |
| Scrap/write-off | **Approval** | TD and MD, both mandatory |

No other approval or notification is added by this specification.

## 18. Open questions - do not infer answers

These are points a developer would otherwise have to guess. They are intentionally unanswered.

### Company and approvals

1. How is company access granted, selected, changed, and locked while a transaction is open?
2. Which masters are company-specific or shared? Only employees are expressly shared.
3. How does separation apply to reports, audit logs, search, notifications, generated files, and backups?
4. Which departments are production-side and which are office-side?
5. What currency and value basis (pre-tax, post-tax, line, or document total) drive thresholds?
6. What happens when a requester is an approver or an approver is unavailable?
7. Who approves comparisons and POs, and does the matrix apply separately to each?
8. For rejected returns, what is high value, what value is tested, and when does TD versus MD apply?
9. Does PR raiser first mean approval, verification, or confirmation?
10. Must TD approve scrap/write-off before MD, or may both act in either order?

### Purchase and invoice

11. What fields link a need to project BOM, reorder trigger, or service requirement?
12. What happens if fewer or more than four vendor offers are received?
13. Who determines acceptable lead time, and where is urgency recorded?
14. How is warranty preference applied below 1%, at 12%, and above 2% higher price?
15. How are warranty duration, scope, and exclusions compared?
16. Is recommendation per line, per order, or both; may award be split among vendors?
17. Who makes the final human choice, and what override reasons are allowed?
18. What happens when no vendor qualifies technically or by lead time?
19. Which charges are included in lowest price?
20. What are the required comparison-chart columns and format?
21. What follow-up events, owners, reminders, and escalations are required?
22. Who chooses invoice replacement versus PO revision, and who approves revision?
23. How are partial invoices, credit/debit notes, tax, freight, discount, rounding, and other mismatches handled?
24. How is accepted bill value allocated per unit and BOM line?

### Item, receipt, and QC

25. What uniqueness/format rules apply to item code, internal barcode, manufacturer barcode, model, and part number?
26. Are internal and manufacturer barcodes stored separately?
27. Which barcode symbology, printer, sticker size, and contents are required?
28. What visual marks implement the six symbols, and how do they map to categories?
29. Who creates/approves new items, and what happens to the waiting receipt?
30. What gate-entry fields, numbering, and transfer-to-security rules apply?
31. Must GRN reference PO, gate entry, challan, and invoice?
32. How are partial, excess, short, damaged, free, and without-PO receipts handled?
33. Is warranty period a duration, dates, or both, and what starts it?
34. How are missing/non-controlled serial numbers handled?
35. What GRN numbering, correction, cancellation, and reversal rules apply?
36. What QC criteria, samples, tolerances, statuses, evidence, and signatures apply to each ISO scope activity?
37. Who covers NARREN S when unavailable?
38. Are hold, reinspection, and concession allowed, and who decides them?
39. What constitutes QC sheets, FAT, and dispatch-document audit?
40. How does non-QC-required material become available?
41. Does payment hold cover the full invoice or rejected quantity only?
42. How is replacement matched to rejected unit, GRN, PO, bill, and DC?
43. What happens if replacement never arrives or the vendor gives credit instead?

### Issue, DC, subcontract, and scrap

44. Who may raise Internal Issue Requests, and what fields/evidence are mandatory?
45. When does Production Manager approve versus department owner, and who is department owner?
46. Can issue be partial, changed, cancelled, reversed, or returned, and under what approvals?
47. What reference is required for `DEMO` and `FREE_OF_COST`?
48. When is `SERVICE` linked to customer PO, warranty, or sale bill?
49. How does `WARRANTY` purpose differ from `SERVICE` against warranty?
50. When is `DEMO` returnable versus non-returnable?
51. What does bill through mean, and what bill is linked?
52. What fields, numbers, signatures, dates, transport details, print, and closure evidence does a DC require?
53. What happens when returnable material is late, short, damaged, consumed, or never returned?
54. How are TD/MD notifications delivered, acknowledged, retried, and evidenced; may dispatch precede delivery?
55. How are outgoing material, returned work, wastage, vendor PO, and project/Job Order linked for subcontract?
56. Does returned subcontract material follow gate, GRN, warranty, QC-rack, and QC steps?
57. How are subcontract costs split across multiple projects or Job Orders?
58. What distinguishes scrap from write-off; what reasons/evidence are required?
59. How is scrap valued/disposed after approval, and does sale require bill, DC, or e-way bill?

### Sales, Job Order, BOM, labour, and service

60. Which standard model copies govern AI offers, and what version, prompt, human-review, approval, and retention controls apply?
61. Which live-stock cost is used when accepted bill values differ, stock is reserved/unavailable, or HSN/barcode matches multiple items?
62. Who sets/approves margin, and may it vary by chamber, item, customer, or service part?
63. How are offer-versus-PO differences recorded and agreed?
64. What numbering rules apply to customer PO, offer, review, and Job Order?
65. What qualifies as a chamber and how is it identified?
66. How are lump-sum discount, tax, freight, installation, and other charges allocated per chamber?
67. How are machine model and internal serial generated and kept unique?
68. Which bill closes Job Order, and can it reopen after correction/cancellation?
69. What starts and ends the warranty period controlling BOM closure?
70. What BOM versions, approvals, substitutions, quantities, and variance reasons are required?
71. How are shared material, subcontract, labour, travel, hotel, and food costs allocated?
72. What columns, calculations, thresholds, and reports are required side by side?
73. Which salary value is divided by 208, and how are salary changes, partial months, leave, overtime, holidays, idle time, and fractional hours treated?
74. What precision/rounding applies to labour rate, hours, BOM cost, and inter-company billing?
75. What morning/evening report fields, deadlines, corrections, and approvals apply?
76. How do completed/pending counts produce performance and incentive outcomes?
77. Can one task/time entry cover multiple Job Orders or companies?
78. What inter-company invoice, GST, frequency, and approval details apply?
79. What fields, dates, renewals, billing, entitlements, visits, and closure rules apply to Warranty, AMC, and CAMC?
80. How are customer materials under AMC received, tracked, issued, returned, lost, damaged, or consumed?
81. Which consumables are included under CAMC and how are they costed?
82. How is a visit split when it contains warranty and chargeable work?

### E-way bill

83. Which transactions require e-way bills, and what value, distance, movement, and exemption rules apply?
84. Which source document supplies data for receipt, every DC type, subcontract, rejection, demo, warranty, sale, and free-of-cost movement?
85. What credentials, authorization, numbering, storage, print, cancellation, extension, amendment, failure, and retry rules apply?
86. Who may generate, cancel, extend, or amend an e-way bill?

RESULT_REPORTED_PENDING_WITNESS
