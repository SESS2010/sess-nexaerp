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

Two companies: SESS Pvt Ltd and SESS Proprietorship. Separation is absolute. After login, the user selects a company at the top of the ERP without a new login or re-authentication. The selection persists for the session. Changing company resets the current working context. Company context is carried on every request. Nobody has cross-company visibility, including the Technical Director and Managing Director; they switch company to see the other side. Cross-company reporting is not provided now; each company is viewed separately. Everything below shows only that company's stores, PR, PO, GRN, job orders, BOM, AMC, warranty, uploads, and downloads. Each company has separate physical stores.

Employees are the only shared thing. Hours are charged to the company whose work it was. SESS Pvt Ltd bills SESS Proprietorship for labour.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | User | Selects or switches company in the UI without a new login or re-authentication. Selection persists for the session; switching resets the current working context. | None stated. | Company context | **Partial** - company foundations exist; session persistence/reset requires service and UI support. |
| 2 | ERP | Carries company context on every request, gives nobody cross-company visibility including TD and MD, and provides no cross-company report now. | Mandatory separation; neither approval nor notification. | Company-scoped request/view/transaction/report | **Partial** - current Purchase/foundations are scoped; several listed areas have no schema. |
| 3 | Stores | Operates physically separate stores for each company. | None stated. | Company store | **Supported** for company-scoped warehouse foundations. |
| 4 | Employee | Records hours against the company whose work was performed. | None stated. | Company time charge | **NO SCHEMA SUPPORT TODAY**. |
| 5 | SESS Pvt Ltd / Accounts | Issues a normal monthly GST service invoice to SESS Proprietorship for labour. Accounts decides the HSN. | None stated. | Monthly GST service invoice | **NO SCHEMA SUPPORT TODAY**. |

## 4. Approval matrix

This matrix is already implemented. The 5,000 and 100,000 boundaries are editable configuration values governed by Section 17.1. Each document uses the immutable configuration snapshot taken at creation.

| Amount | Required approval sequence |
|---:|---|
| Below 5,000 | Department Manager only |
| 5,000 to 100,000 | Department Manager, then `TECHNICAL_DIRECTOR` |
| Above 100,000 | Department Manager, then `MANAGING_DIRECTOR` |

Level 1 follows the requesting department: production-side departments go to `PRODUCTION_MANAGER` (SESS-25); office-side departments go to `ACCOUNTS_MANAGER` (SESS-14). There is one manager per department; "boss" means the department manager and is not ambiguous. MD is the higher authority and may approve any level, including acting in place of TD when TD is unavailable. TD cannot act in place of MD above 100,000. A PR raised by TD or MD never self-approves and needs only the remaining level: for example, a 50,000 PR raised by TD is approved at Level 1 by the department manager and at Level 2 by MD.

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
| 2 | Person with need | Raises PR with mandatory `REQUIRED-BY DATE`. | **APPROVAL REQUIRED:** raiser's department manager first, then TD or MD by amount; TD/MD raisers never self-approve and use the remaining levels stated in Section 4. | PR | **Supported** for required-by date and workflow; substitution/self-approval rules require service enforcement. |
| 2E | Person making an emergency purchase | Makes an emergency purchase without PR against a bill only below the configured 5,000 value limit. Maximum is the configured 10 per month per company. ERP warns as the count approaches the configured limit and blocks beyond it. Above the configured value limit, PR is mandatory. | No extra approval stated; applicable controls use the configuration snapshot. | Emergency-purchase bill and monthly counter | **NO SCHEMA SUPPORT TODAY** for this exception, bill intake, warning, counter, and block. |
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
2. Lead time that misses the mandatory PR `REQUIRED-BY DATE`: reject.
3. Among the rest, prefer warranty even at 1-2% higher price.
4. Otherwise lowest price.

Four vendors are typical, not required. Two offers are acceptable. One offer is allowed with a recorded reason and no extra approval; the normal amount matrix applies. The ERP recommends. A human chooses finally and must record the reason.

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

Each company has its own store and QC rack. KAMALI (SESS-16), SUDALAI (SESS-35), and KARTHICK (SESS-41) enter GRNs for both companies.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Stores today; security later | Makes gate entry with vendor DC number, vehicle number, date and time, and received by. | None stated. | Gate entry | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Stores / ERP | Checks Item master; GRN cannot be entered for a nonexistent item. | Mandatory prerequisite. | Item check | **Partial** - Item exists; GRN does not. |
| 3 | Item creator | First creates missing item with internal barcode, model number, manufacturer part number, HSN, GST, UOM, category, and any item-level override of the configured serial threshold. Item master does not hold serial numbers. | None stated. | Item | **Partial** - listed descriptive fields and a serial-tracking flag exist; editable threshold/override and separate internal/manufacturer barcode fields are not fully supported. |
| 4 | ERP / Stores | Uses manufacturer barcode if present; otherwise generates `SESS-<CAT>-<serial>` using a per-company sequence. Category codes: `ELE` electrical, `REF` refrigeration, `FAS` fasteners, `PLC` instrumentation, `FAB` fabrication, `MEC` mechanical. Prints and fixes the sticker to the component. | None stated. | Barcode sticker | **Partial** - barcode fields exist; separate source barcodes, format/sequence, and printing have **NO SCHEMA SUPPORT TODAY**. |
| 5 | KAMALI, SUDALAI, or KARTHICK | Enters GRN in either company. Per-unit serial is mandatory when item value is above the configured 5,000 threshold, unless the Item master override applies. At or below the threshold, serial may be recorded if available but is not mandatory. Records bill date; installation date is recorded when it occurs. Warranty expiry is the earlier of 12 months from installation or 13 months from bill date; before installation, use 13 months from bill date. | Mandatory serial rule is driven by editable configuration plus Item override. | GRN, serial records, warranty dates/expiry | **NO SCHEMA SUPPORT TODAY** for GRN/unit warranty and threshold evaluation; Item serial-tracking flag exists in advance. |
| 6 | Stores | Moves received material to that company's QC rack; it is unavailable for issue. | Mandatory availability control. | QC-rack movement | **Partial** - company-scoped QC-hold locations exist; receipt movement does not. |
| 7 | NARREN S (SESS-33, `QC_MANAGER`) | Inspects incoming material. ISO scope also covers CNC output, powder-coating output, in-process, QC sheets, FAT, and dispatch-document audit. All powder-coating, CNC, milling, and lathe subcontract returns require QC on return. | Inspection, not stated as approval. | QC evidence | **NO SCHEMA SUPPORT TODAY** for operational records; only policy/location foundations exist. |
| 8 | Stores / ERP | For partial QC, moves accepted quantity to regular racks and makes it available. The accepted portion is not re-inspected. | QC acceptance prerequisite. | Accepted-stock movement | **Partial** - foundations exist; acceptance posting does not. |
| 9 | Stores | Keeps rejected quantity in QC rack until returnable DC is raised; it never enters regular racks. | Mandatory location/availability control. | Rejected-stock state | **NO SCHEMA SUPPORT TODAY** for the transaction/state. |
| 10 | Bill-entry user | Enters bill only for accepted quantity. | None stated. | Accepted-quantity bill | **NO SCHEMA SUPPORT TODAY**. |
| 11 | Stores | Sends rejected quantity on returnable DC with reason `rejection - replacement request`. | **APPROVAL REQUIRED:** PR raiser first, then TD or MD for high value. | Returnable DC | **NO SCHEMA SUPPORT TODAY**. |
| 12 | ERP / responsible user | If vendor does not collect rejected material within one week, records an escalation and notifies PRIYA E, TD, and MD together. Material is not scrapped automatically. | **ESCALATION REQUIRED after one week; not approval.** | Escalation record | **NO SCHEMA SUPPORT TODAY**. |
| 13 | Accounts / ERP | Holds payment until replacement arrives and DC closes. | Mandatory hold, not approval. | Payment hold | **NO SCHEMA SUPPORT TODAY**. |
| 14 | Stores | Receives replacement as a new GRN and sends it through the full QC flow again; closes the earlier returnable DC when the replacement return is received. | Full QC required. | New GRN, QC record, closed DC | **NO SCHEMA SUPPORT TODAY**. |

## 8. Outbound issue flow

**Stores never issues material without an approved Internal Issue Request. There is no exception for any destination.**

One request may contain many items, but every item on it must be for one Job Order. Issue beyond the Estimated BOM is allowed and must not be blocked. When Actual BOM exceeds Estimated BOM, the ERP sends a notification.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Requester | Raises one Internal Issue Request containing one or more items for exactly one Job Order. | None at creation. | Internal Issue Request | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Production Manager or department owner | Reviews request. | **APPROVAL REQUIRED.** | Approved request | **NO SCHEMA SUPPORT TODAY**. |
| 3 | Stores incharge | Issues only against approved request, including quantities beyond Estimated BOM. | Mandatory approval; no exception. Do not block BOM overrun. | Material issue | **NO SCHEMA SUPPORT TODAY**; only minimal movement foundation exists. |
| 4 | Stores / ERP | Records purpose and reference. | None stated. | Referenced movement | **NO SCHEMA SUPPORT TODAY** for these codes/references. |
| 5 | ERP | Detects when Actual BOM exceeds Estimated BOM. | **NOTIFICATION REQUIRED; not approval and not a block.** | BOM-overrun notification | **NO SCHEMA SUPPORT TODAY**. |

| Purpose | Required meaning/reference |
|---|---|
| `FACTORY_ASSEMBLY` | To a Job Order |
| `PROJECT` | To a Job Order |
| `SERVICE` | Against customer PO, warranty, or sale bill |
| `WARRANTY` | Free replacement |
| `DEMO` | To client; returnable by default |
| `SALE` | Billed |
| `FREE_OF_COST` | Given free |

## 9. Delivery Challans

### Returnable

Material comes back: subcontract (powder coating, CNC, milling, lathe), rejected vendor material, and demo. Demo is returnable by default. Every returnable DC requires a manually entered `EXPECTED RETURN DATE`; different jobs may have different periods. The DC stays `OUTSTANDING` until return is received, and overdue is measured against that date.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Requesting department / Stores | Creates returnable movement; manually entered expected return date is mandatory. | Approved Internal Issue Request for every issue; rejection also follows Section 7. | Returnable DC | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Stores | Dispatches and keeps DC outstanding. | None stated. | Outstanding DC | **NO SCHEMA SUPPORT TODAY**. |
| 3 | ERP | Marks overdue against expected return date. | None stated. | Overdue status | **NO SCHEMA SUPPORT TODAY**. |
| 4 | Stores | Receives return and closes DC. | Close only on receipt. | Return/closed DC | **NO SCHEMA SUPPORT TODAY**. |

### Non-returnable

Material does not come back: warranty supply, bill through, and free of cost.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Requesting department / Stores | Creates non-returnable movement. | Approved Internal Issue Request remains mandatory. | Non-returnable DC | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Department owner | Reviews DC. | **APPROVAL REQUIRED.** | Approval | **NO SCHEMA SUPPORT TODAY**. |
| 3 | ERP | Notifies both TD and MD in-app and by email because material leaves without payment. | **MANDATORY NOTIFICATION to both; not approval.** | In-app and email notifications | **NO SCHEMA SUPPORT TODAY**. |
| 4 | Stores | Dispatches. | Department-owner approval required; both notifications are mandatory. | Dispatched DC | **NO SCHEMA SUPPORT TODAY**. |

## 10. Subcontract

Powder coating, CNC cutting and folding, milling, and lathe work. All returns go through QC. Subcontract labour posts to BOM at the PO bill value.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Requester | Raises Internal Issue Request. | **APPROVAL REQUIRED** by Production Manager or department owner. | Approved request | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Stores | Sends material on returnable DC with expected return date. | Approved request required. | Outstanding DC | **NO SCHEMA SUPPORT TODAY**. |
| 3 | Vendor / Purchase | Vendor quotes; Purchase raises PO. No separate document type. | **Same PO approval flow.** | Quote/PO | **Partial** - quote/PO exist; subcontract linkage does not. |
| 4 | Stores | Receives material back. | None stated. | Return receipt | **NO SCHEMA SUPPORT TODAY**. |
| 5 | NARREN S / QC | Inspects every powder-coating, CNC, milling, and lathe return. | QC inspection required. | QC inspection | **NO SCHEMA SUPPORT TODAY**. |
| 6 | Stores | Closes returnable DC after return is received. | None stated. | Closed DC | **NO SCHEMA SUPPORT TODAY**. |
| 7 | ERP | Posts subcontract labour to project BOM at PO bill value. | None stated. | BOM subcontract cost | **NO SCHEMA SUPPORT TODAY**. |

## 11. Scrap and write-off

Stores decides. TD and MD approval is mandatory at any value.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Stores | Decides scrap/write-off. | Stores decision. | Decision | **NO SCHEMA SUPPORT TODAY**; scrap location is only a foundation. |
| 2 | Technical Director | Reviews at any value. | **APPROVAL REQUIRED.** | TD approval | **NO SCHEMA SUPPORT TODAY**. |
| 3 | Managing Director | Reviews at any value. | **APPROVAL REQUIRED.** | MD approval | **NO SCHEMA SUPPORT TODAY**. |
| 4 | Stores | Executes after both approvals. | Both approvals mandatory. | Stock movement | **NO SCHEMA SUPPORT TODAY**. |

## 12. Sales to Job Order

Job Orders are created after contract review is mutually agreed. There is no separate approval step.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Customer | Sends RFQ. | None. | Customer RFQ | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Sales / TD, sometimes ALFATHIMA (Accounts); VENKAT for service parts | Prepares an AI-assisted Word offer in ERP from standard model reference copies. | None stated. | Word offer | **NO SCHEMA SUPPORT TODAY**. |
| 3 | ERP / preparer | Looks up live-store cost by HSN or barcode, then adds margin percentage. | None stated. | Costed offer | **NO SCHEMA SUPPORT TODAY**; HSN/barcode are partial foundations. |
| 4 | Customer | Sends customer PO. | None. | Customer PO | **NO SCHEMA SUPPORT TODAY**. |
| 5 | Sales / TD | Compares offer against PO in an AI-assisted Word contract-review document held in ERP. | Review, not a separate approval. | Word contract review | **NO SCHEMA SUPPORT TODAY**; generic document/revision foundations exist. |
| 6 | SESS and customer | Mutually agree the contract review. ERP records the agreement and attaches the signed Word document. | Mutual agreement required; no separate approval step. | Agreement record and signed Word attachment | **NO SCHEMA SUPPORT TODAY** for the agreement workflow; generic document/revision foundations exist. |
| 7 | Responsible user | Creates Job Orders after mutual agreement. | No separate Job Order approval. | Job Orders | **NO SCHEMA SUPPORT TODAY**. |

## 13. Job Order and BOM

Job Order and BOM are different. Job Order closes on bill entry. BOM stays `OPEN` until warranty ends.

### One Job Order per chamber

One per chamber is mandatory. A customer PO covering three chambers creates three Job Orders even for one lump-sum price. Chamber-wise price breakup comes from the offer letter, which always prices each chamber separately.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | ERP / responsible user | Creates exactly one Job Order per chamber after mutual contract-review agreement, using the offer-letter breakup. | No separate approval. | Job Order(s) | **NO SCHEMA SUPPORT TODAY**; generic Project is not Job Order. |
| 2 | ERP / responsible user | Records machine model and customer name. ERP generates the machine serial with the Job Order in format `SESS-<CompanyCode>-<Year>-<Sequence>`, for example `SESS-PVT-2026-0042`. | None stated. | Job Order details and machine serial | **NO SCHEMA SUPPORT TODAY**. |
| 3 | ERP | Closes Job Order on bill entry. | None stated. | Closed Job Order | **NO SCHEMA SUPPORT TODAY**. |
| 4 | ERP | Keeps BOM open until warranty ends. | Mandatory rule. | BOM status | **NO SCHEMA SUPPORT TODAY**. |

### Estimated versus Actual BOM

Design creates Estimated BOM at offer time, and it drives offer price. Actual BOM records what was really consumed. ERP must show both side by side. This comparison is the primary business purpose of the system.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Design team | Creates Estimated BOM at offer time. | None stated. | Estimated BOM | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Sales / TD | Uses it to drive offer price. | None stated. | Offer price | **NO SCHEMA SUPPORT TODAY**. |
| 3 | ERP | Accumulates actual consumed costs. | None. | Actual BOM | **NO SCHEMA SUPPORT TODAY**. |
| 4 | ERP | Shows both side by side. | None. | Comparison | **NO SCHEMA SUPPORT TODAY**. |
| 5 | ERP | Allows issue beyond estimate and notifies when Actual exceeds Estimated. | **NOTIFICATION REQUIRED; do not block.** | Overrun notification | **NO SCHEMA SUPPORT TODAY**. |

Actual BOM includes:

- Material at accepted bill value
- Subcontract charges at PO bill value
- Production labour
- QC labour
- Installation labour
- Installation expenses: travel, hotel, food
- Warranty-period service spares and labour

AMC and CAMC material and labour do not enter the project BOM. They are separate service activities with their own revenue and cost. All stated BOM requirements have **NO SCHEMA SUPPORT TODAY**.

## 14. Labour costing and task reporting

Rate = `monthly salary / 208 hours`, where `208 = 26 days x 8 hours`. Use the rate on the issue date of the hours: the month in which the work was done, not a project average. No overhead loading: no factory rent, electricity, or admin. The same rate is used for inter-company billing.

An engineer may work on several Job Orders in one day. Hours are split across Job Orders in the task report. Factory-engineer hours are entered by the Production Manager, not the engineer, and approved only by the Production Manager. The Service Manager enters and approves hours for service engineers.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Production Manager | Assigns factory-engineer tasks referencing Job Order number. | None stated. | Task | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Service Manager | Assigns service-engineer tasks referencing Job Order number. | None stated. | Task | **NO SCHEMA SUPPORT TODAY**. |
| 3 | Production Manager / Service Manager | Production Manager enters factory-engineer hours; Service Manager enters service-engineer hours. Each manager splits the day's hours across all Job Orders worked, within morning/evening reporting. Engineers do not enter the hours. | **APPROVAL REQUIRED by the same responsible manager only:** Production Manager for factory engineers; Service Manager for service engineers. | Time/task report and manager approval | **NO SCHEMA SUPPORT TODAY**. |
| 4 | ERP | Costs each hours issue using that work month's salary / 208, with no overhead and no project averaging. | None. | Labour cost | **NO SCHEMA SUPPORT TODAY**. |
| 5 | ERP | Charges work to the company whose work it was. | None. | Company charge | **NO SCHEMA SUPPORT TODAY**. |
| 6 | SESS Pvt Ltd / Accounts | Issues a normal monthly GST service invoice to SESS Proprietorship at the same labour rate. Accounts decides the HSN. | None stated. | Monthly GST service invoice | **NO SCHEMA SUPPORT TODAY**. |
| 7 | ERP | Feeds completed and pending task counts to performance/incentive view. | None. | Performance/incentive view | **NO SCHEMA SUPPORT TODAY**. |


### 14.1 Installation and service expense rules

Installation expenses - travel, hotel/lodging, and food - post to the BOM. The following values are shown here for process clarity and are governed centrally by Section 17.1; they are editable configuration, not hard-coded:

| Expense rule | Configured value |
|---|---:|
| Food | 300 per person per day |
| Lodging - one person | 800 per day |
| Lodging - two persons | 1,200 per day |
| Daily approval cap | 5,000 per day |
| Travel distance qualification | More than 100 km from the engineer's location |
| Travel duration qualification | One day or more |

Travel qualifies when either the site is more than the configured 100 km from the engineer's location or the trip is one day or more.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Employee / responsible manager | Records food, lodging, local transport, and travel details. Local transport is against a bill. | None at entry. | Expense claim and bills | **NO SCHEMA SUPPORT TODAY**. |
| 2 | Service Manager | Reviews service-engineer expenses within the configured limits when supported by a proper GST bill. | **APPROVAL REQUIRED within configured limits for service engineers.** | Service Manager approval | **NO SCHEMA SUPPORT TODAY**. |
| 3 | Employee's own manager | May approve an expense within the configured limits when supported by a proper GST bill. | **APPROVAL PERMITTED with proper GST bill.** | Manager approval | **NO SCHEMA SUPPORT TODAY**. |
| 4 | TD or MD | Either one reviews any expense above the configured 5,000 per-day cap, regardless of bill. | **APPROVAL REQUIRED from TD or MD.** | TD/MD approval | **NO SCHEMA SUPPORT TODAY**. |
| 5 | TD or MD | Either one reviews any expense without a proper bill, regardless of amount. | **APPROVAL REQUIRED from TD or MD.** | TD/MD approval | **NO SCHEMA SUPPORT TODAY**. |
| 6 | ERP | Posts approved installation travel, lodging, and food to the BOM. | Required approvals are prerequisites. | Actual BOM expense | **NO SCHEMA SUPPORT TODAY**. |

## 15. Warranty, AMC, and CAMC

These three contracts must not be confused.

| Contract | Rule | Cost treatment | Schema support |
|---|---|---|---|
| `WARRANTY` | Free, included in project value | Warranty service spares and labour enter project BOM | **Partial** - generic asset warranty dates exist; contract/service cost-to-BOM does not. |
| `AMC` | Customer pays; customer supplies material | Material and labour are separate service cost/revenue and do not enter project BOM | **NO SCHEMA SUPPORT TODAY**. |
| `CAMC` | Customer pays; SESS supplies consumables | Material and labour are separate service cost/revenue and do not enter project BOM | **NO SCHEMA SUPPORT TODAY**. |

## 16. E-way bill

E-way-bill payload is generated in ERP for both companies and exported as JSON for manual upload to the GST portal. The JSON follows the official NIC bulk-upload schema from ewaybillgst.gov.in. There is no GSP API integration now. The same NIC structure must serve future API integration without rework.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | Responsible user / ERP | Builds e-way-bill payload in the selected company context. | None stated. | E-way-bill payload | **NO SCHEMA SUPPORT TODAY**. |
| 2 | ERP | Validates and exports payload as JSON using the official NIC bulk-upload schema from ewaybillgst.gov.in. | None stated. | NIC-schema JSON export | **NO SCHEMA SUPPORT TODAY**. |
| 3 | Responsible user | Manually uploads JSON to GST portal. | None stated. | GST portal upload | Outside ERP; no GSP API integration now. |
| 4 | System design | Uses the same NIC structure for later API integration without rework. | Mandatory design constraint. | Extensible e-way-bill data contract | **NO SCHEMA SUPPORT TODAY**. |

## 17. Approval versus notification register

| Event | Control | Required party or parties |
|---|---|---|
| Standard PR | **Approval** | Department manager; then TD for 5,000 to 100,000 or MD above 100,000 |
| PR raised by TD or MD | **Approval** | Never self-approved; department manager Level 1, then remaining higher level as stated in Section 4 |
| TD unavailable | **Authority substitution** | MD may act for TD at any level |
| MD approval above 100,000 | **Approval** | MD required; TD cannot act in MD's place |
| Emergency purchase below configured 5,000 limit | **Exception with configured cap** | PR not required; purchase is against a bill; warn near 10 per company/month and block beyond configured cap |
| Emergency purchase at or above configured 5,000 limit | **Prohibited exception** | PR mandatory |
| Single vendor offer | **Approval** | No extra approval; recorded reason plus normal amount matrix |
| Comparison | **Approval** | Required; approver unstated |
| PO | **Approval** | Required; exact routing unstated |
| Every Internal Issue Request | **Approval** | Production Manager or department owner |
| Actual BOM exceeds Estimated BOM | **Notification, not approval** | Recipient/channel unstated; do not block issue |
| Rejected-material return | **Approval** | PR raiser first; then TD or MD for high value |
| Rejected material uncollected for one week | **Escalation and notification** | Notify PRIYA E, TD, and MD together; record escalation; do not scrap automatically |
| Non-returnable DC | **Approval** | Department owner |
| Non-returnable DC | **Mandatory notification, not approval** | TD and MD by in-app notification and email |
| Subcontract PO | **Approval** | Same PO approval flow |
| Factory-engineer task hours | **Entry and approval** | Production Manager enters and is the only approver |
| Service-engineer task hours | **Entry and approval** | Service Manager enters and approves |
| Expense within configured limits with proper GST bill | **Approval** | Employee's own manager may approve; Service Manager approves service-engineer expenses |
| Expense above configured 5,000/day or without proper bill | **Approval** | Either TD or MD; one of them is mandatory |
| Scrap/write-off at any value | **Approval** | TD and MD, both mandatory |
| Configuration change | **Role-authorized change; no separate approval stated** | TD, MD, or IT Manager only; complete old/new/reason history |
| Existing document after configuration change | **Snapshot control** | Keeps configuration captured at document creation; new value applies only to new documents |

No other approval or notification is added by this specification.
### 17.1 Central configuration governance

All editable business limits live in one company-scoped, effective-dated Business Rule Configuration registry. This is the proposed single source of truth.

| Configuration key | Initial value | Scope |
|---|---:|---|
| Emergency purchase count limit | 10 per month | Per company |
| Emergency purchase value limit | 5,000 | Per company |
| Mandatory serial-capture threshold | Above 5,000 | Per company; Item master may override |
| Approval lower band boundary | 5,000 | Per company |
| Approval upper band boundary | 100,000 | Per company |
| Food limit | 300 per person per day | Per company |
| Lodging limit - one person | 800 per day | Per company |
| Lodging limit - two persons | 1,200 per day | Per company |
| Expense daily approval cap | 5,000 per day | Per company |
| Travel distance qualification | More than 100 km | Per company |

Only `TECHNICAL_DIRECTOR`, `MANAGING_DIRECTOR`, and `IT_MANAGER` may change configuration. Nobody else may change it, including `PRODUCTION_MANAGER`, `ACCOUNTS_MANAGER`, and `PURCHASE_MANAGER`.

Every change records who changed it, when, old value, new value, and reason. Approval-limit and expense-limit history must be as complete as approval history.

Configuration changes affect new documents only. The mechanism is:

1. Store each configuration value as a company-scoped, effective-dated row with an immutable version.
2. At document creation, resolve the effective rows and store an immutable configuration snapshot on the document.
3. All validation, routing, warnings, blocks, and approvals for that document use its snapshot.
4. A later configuration change creates a new effective version; it does not rewrite prior rows or document snapshots.
5. Documents already created, submitted, or in approval keep the values under which they were created.

| Step | Actor | Action | Approval / notification | Document produced | Schema support |
|---:|---|---|---|---|---|
| 1 | TD, MD, or IT Manager | Creates a new effective configuration version with a reason. | Role authorization required; no separate approval was stated. | Effective-dated configuration row | **Partial** - Purchase approval-policy rows exist in advance; a unified registry for all listed values is new. |
| 2 | ERP | Records actor, time, old value, new value, and reason without overwriting history. | Mandatory audit control. | Configuration change history | **Partial** - audit/history patterns exist; complete configuration history is new. |
| 3 | ERP | Resolves current company configuration at document creation and stores an immutable snapshot. | Mandatory snapshot control. | Document configuration snapshot | **Partial** for Purchase approval snapshots; new for the other processes. |
| 4 | ERP | Applies later values only to documents created under the later effective version. | Mandatory lifecycle control. | Version-bound rule evaluation | **NO SCHEMA SUPPORT TODAY** across all specified processes. |
## 18. Open questions - final residual list

Section 18 is not empty. The final answers close company-session behavior, separate reporting, contract-review evidence, inter-company GST invoicing frequency/HSN ownership, approval authority, emergency count/value controls, single-offer treatment, required-by date, warranty fallback, rejected-vendor escalation recipients, serial threshold/override, mandatory DC return date, machine-serial format, manager-entered task hours, expense limits/approvals, NIC JSON structure, and configuration governance. They do not answer the following points, which remain open and must not be inferred.

### Company, approval, and configuration

1. Who is authorized to select each company context?
2. How must company separation apply to audit views, notification histories, generated-file storage, and backups?
3. Which exact departments are production-side and which are office-side?
4. What currency and value basis (pre-tax, post-tax, line, or full document total) drive the approval and emergency-purchase value limits?
5. Who acts when the department manager or MD is unavailable?
6. Who approves comparisons and POs, and does the amount matrix apply separately to each?
7. For rejected-material returns, what is high value, what value is tested, and when does TD versus MD apply?
8. Does PR raiser first for a rejected return mean approval, verification, or confirmation?
9. Must TD approve scrap/write-off before MD, or may both mandatory approvals occur in either order?
10. Who may use the emergency-purchase exception, and what facts make a purchase an emergency?
11. At what count must the approach-limit warning fire before the configured monthly emergency-purchase cap?
12. May configuration changes be future-dated or backdated, and if so under what rule?

### Purchase, comparison, invoice, and follow-up

13. What fields link a need to project BOM, reorder trigger, or service requirement?
14. How is warranty preference applied when the warranty vendor is less than 1% or more than 2% higher?
15. How are warranty duration, scope, exclusions, and value compared?
16. Is recommendation per line, per order, or both, and may an award be split among vendors?
17. Who makes the final human vendor choice, and what override reasons are allowed?
18. What happens when no vendor meets technical qualification or the required-by date?
19. Which taxes, freight, packing, insurance, discounts, and other charges determine lowest price?
20. What columns and printable/export format must the comparison chart contain?
21. What material-follow-up events, owners, reminders, and escalations are required?
22. Who chooses invoice replacement versus PO revision, and who approves a mismatch-driven revision?
23. How are partial invoices, credit/debit notes, tax, freight, discount, rounding, and non-price mismatches handled?
24. How is accepted bill value allocated per unit and BOM line?
25. What bill fields, numbering, matching, correction, cancellation, and accounting handoff are required?

### Item, gate entry, GRN, and QC

26. What uniqueness and validation rules apply to item code, manufacturer barcode, model, and manufacturer part number beyond the decided internal barcode format?
27. Are internal and manufacturer barcodes stored as two distinct values?
28. Which barcode symbology, printer, sticker size, and sticker contents are required?
29. Who creates and approves new items, and what happens to material waiting for item creation?
30. What gate-entry number, amendment, cancellation, and security-handover rules apply?
31. Must every GRN reference PO, gate entry, vendor DC, and vendor invoice?
32. How are partial, excess, short, damaged, free, and without-PO receipts handled?
33. What GRN numbering, correction, cancellation, reversal, and duplicate-receipt rules apply?
34. What QC criteria, sampling, tolerances, statuses, evidence, and signatures apply to each ISO scope activity?
35. Who performs QC when NARREN S is unavailable?
36. Are QC hold, reinspection of rejected material, and concession allowed, and who decides them?
37. What documents and fields constitute QC sheets, FAT, and dispatch-document audit?
38. How does material not requiring QC become available?
39. Does payment hold cover the full invoice or rejected quantity only?
40. How is a replacement's new GRN linked to rejected units, original GRN, PO, bill, and DC?
41. After the one-week escalation, what happens if collection/replacement still does not occur or the vendor offers credit instead?

### Issue, DC, subcontract, scrap, and expenses

42. Who may raise Internal Issue Requests, and what fields, attachments, and receiver evidence are mandatory?
43. When does Production Manager approve versus department owner, and who is the department owner?
44. Can an issue be partial, changed, cancelled, reversed, or returned, and what approvals apply?
45. What reference is required for `DEMO` and `FREE_OF_COST`?
46. When is `SERVICE` linked to customer PO, warranty, or sale bill?
47. How does `WARRANTY` purpose differ from `SERVICE` against warranty?
48. Under what condition may the default returnable treatment for demo be overridden?
49. What does bill through mean, and what bill is linked?
50. Beyond mandatory expected return date, what numbers, signatures, transporter details, print format, and closure evidence must a DC contain?
51. What action follows an overdue returnable DC for demo or subcontract, or a short/damaged/consumed return?
52. Must TD/MD non-returnable-DC notifications be delivered before dispatch, and what acknowledgement, retry, and evidence rules apply?
53. How are outgoing material, returned processed material, wastage, vendor PO, QC result, and project/Job Order linked for subcontract?
54. How is PO bill value split when subcontract work serves multiple projects or Job Orders?
55. What distinguishes scrap from write-off, and what reasons and evidence are required?
56. How is approved scrap valued and disposed of, and does a scrap sale require a bill, DC, or e-way bill?

### Sales, Job Order, BOM, labour, and service contracts

57. Which standard model copies govern AI-assisted offers, and what prompt, version, human-review, access, and retention controls apply?
58. Which live-stock cost is used when accepted bill values differ, stock is reserved/unavailable, or HSN/barcode matches multiple items?
59. Who sets and approves margin, and may it vary by chamber, item, customer, or service part?
60. What numbering and revision rules apply to customer RFQ, offer, contract review, customer PO, and Job Order?
61. What qualifies as a chamber and how is it identified?
62. How are lump-sum discount, tax, freight, installation, and other common charges allocated across chamber prices?
63. Which bill closes the Job Order, and can a Job Order reopen after correction, cancellation, or credit note?
64. Which project warranty dates control when the BOM closes, and how are they related to component warranty?
65. What BOM versions, approvals, substitutions, quantities, and variance reasons are required?
66. How are shared material, subcontract, labour, travel, lodging, and food costs allocated across Job Orders?
67. What columns, calculations, thresholds, and reports are required in the side-by-side BOM view?
68. Which monthly salary value is divided by 208, and how are partial months, leave, overtime, holidays, idle time, and fractional hours treated?
69. What precision and rounding apply to labour rate, hours, BOM cost, expenses, and inter-company billing?
70. What morning/evening report fields, deadlines, and correction rules apply?
71. How do completed/pending counts produce performance and incentive outcomes?
72. When work for multiple companies occurs on one day, what task-report and inter-company separation rules apply?
73. What fields, dates, renewals, billing, entitlements, visits, and closure rules apply to Warranty, AMC, and CAMC?
74. How are customer materials under AMC received, tracked, issued, returned, lost, damaged, or consumed?
75. Which consumables are included under CAMC and how are they costed?
76. How is one visit split when it contains warranty and chargeable work?

### E-way-bill JSON export

77. Which transactions require an e-way bill, and what value, distance, movement-type, and exemption rules apply?
78. Which ERP source documents supply payload data for receipt, each DC type, subcontract, rejection, demo, warranty, sale, and free-of-cost movement?
79. What file naming, retention, correction, and version-adoption rules apply around the official NIC schema?
80. Who may generate, export, correct, and manually upload the JSON?
81. How is the portal-generated e-way-bill number/status recorded back in ERP, and how are cancellation, extension, and amendment tracked?
## 19. Build dependencies: schema, services, and APIs

The order below is dependency order, not an effort estimate. "Advance" means the current source already contains a reusable foundation. "New" means the stated capability must be added. Existing foundations do not by themselves satisfy the full business process.

| Order | Dependency area | Layer | Advance or new | Required support |
|---:|---|---|---|---|
| 1 | Company request context and isolation | Schema | **Advance - partial** | Reuse Company and company-scoped entities; ensure every new operational entity is company-scoped. |
| 2 | Company request context and isolation | Service | **New / extension** | Persist selected company per session, carry it on every request, prohibit cross-company reads/reports, and reset working context on switch without re-authentication. |
| 3 | Company request context and isolation | API | **New / extension** | Require and validate company context on every endpoint, including files, separate-company reports, notifications, and exports. |
| 3A | Central business-rule configuration | Schema | **Advance plus new** | Reuse effective-dated Purchase approval-policy foundations; add company-scoped versioned rows for emergency count/value, serial threshold, approval bands, and expense limits, with actor/time/old/new/reason history. |
| 3B | Central business-rule configuration | Service | **New / extension** | Restrict changes to TD, MD, and IT Manager; resolve effective values at document creation; write immutable document snapshots; apply changes only to new documents; warn/block emergency counts. |
| 3C | Central business-rule configuration | API | **New** | Read effective configuration/history, create new effective versions with reasons, enforce role authorization, and expose document snapshots read-only. |
| 4 | Employee, department, role, and authority | Schema | **Advance - partial** | Reuse employees, company/department assignments, reporting, roles, department mapping, and approval-policy snapshots; add only missing substitution/exception evidence. |
| 5 | Approval engine | Service | **Advance plus extension** | Reuse implemented amount routing and policy snapshots; add MD higher-authority handling, no self-approval for TD/MD raisers, remaining-level resolution, any-value TD+MD scrap approval, normal-matrix single-offer handling, and configuration-snapshot evaluation. |
| 6 | Approval engine | API | **Advance plus extension** | Reuse PR/comparison/PO transition APIs; expose substitution, emergency bill intake, return approval, issue approval, DC approval/notification, and scrap approval actions. |
| 7 | Core masters and numbering | Schema | **Advance plus new** | Reuse Item, category, UOM, vendor, customer, warehouse, rack/bin, barcode, reorder, QC/serial flags, and document sequences; add separate manufacturer/internal barcodes, per-company barcode sequence/category codes, and Item-level serial-threshold override. |
| 8 | Core masters and numbering | Service | **Advance plus extension** | Validate item-before-GRN, generate `SESS-<CAT>-<serial>` per company, and create labels. |
| 9 | Core masters and numbering | API | **Advance plus extension** | Reuse master APIs; add missing category/UOM/configuration and barcode-generation/label endpoints. |
| 10 | Sales documents, contract review, Job Order, and chamber | Schema | **New** | Customer RFQ, AI-assisted Word offer/revisions, customer PO, contract-review agreement with signed Word attachment, chamber breakup, one Job Order per chamber, and machine serial in `SESS-<CompanyCode>-<Year>-<Sequence>` format. Generic Project/Document/Customer foundations are reusable advances. |
| 11 | Sales-to-Job-Order | Service | **New** | Live-stock cost lookup by HSN/barcode, margin application, offer/PO comparison, signed-Word mutual-agreement recording, Job Order generation, and formatted machine-serial generation. |
| 12 | Sales-to-Job-Order | API | **New** | Customer RFQ, offer, Word artifact, customer PO, contract-review agreement, chamber, and Job Order endpoints. |
| 13 | Estimated and Actual BOM | Schema | **New** | Versioned Estimated BOM at offer time; Actual BOM lines for accepted-bill material, subcontract, labour, installation expense, and warranty service; warranty-open lifecycle and variance/notification evidence. |
| 14 | BOM costing | Service | **New** | Side-by-side comparison, actual accumulation, accepted-bill allocation, PO-bill subcontract costing, warranty costing, AMC/CAMC exclusion, and non-blocking overrun notification. |
| 15 | BOM | API | **New** | Estimated/Actual BOM authoring, issue linkage, comparison, variance, cost-detail, and lifecycle endpoints. |
| 16 | PR through PO | Schema | **Advance plus new** | Reuse PR, required-by date, RFQ, invitations, quotations, technical verification, comparison, PO/revisions, histories, and minimal follow-up; add emergency bill/counter/snapshot evidence and any missing rule evidence. |
| 17 | PR through PO | Service | **Advance plus extension** | Enforce offer-count rules with normal matrix/no extra single-offer approval, required-by lead-time rejection, ordered recommendation, final-choice reasons, configured emergency warning/block, price mismatch, and material follow-up. |
| 18 | PR through PO | API | **Advance plus extension** | Reuse current Purchase APIs; add emergency purchase, comparison chart/export, mismatch decision, invoice replacement, and full follow-up endpoints. |
| 19 | Vendor invoice and bill | Schema | **New** | Vendor bill/header/lines, PO/GRN matching, accepted quantity/value, mismatch decision, revisions/replacements, and bill values feeding BOM. |
| 20 | Vendor invoice and bill | Service/API | **New** | Match invoice to PO/accepted quantity, reject-or-revise only, hold payment, and expose bill entry/status/actions. |
| 21 | Gate entry and GRN | Schema | **New** | Gate fields, GRN/lines, PO/DC/invoice references, serials driven by configured 5,000 threshold and Item override, bill/installation dates, warranty fallback/expiry, replacements, histories, and per-company numbering. |
| 22 | Gate entry and GRN | Service/API | **New** | Item prerequisite, named-user entry in both companies, mandatory-above-threshold/optional-below serial logic with Item override, 13-month pre-installation fallback, final warranty calculation, replacement-as-new-GRN, and receipt endpoints. |
| 23 | QC and condition movement | Schema | **Advance plus new** | Reuse QC policy and company warehouse condition locations; add inspection lots/results, partial acceptance/rejection, QC evidence, rejected-in-QC state, subcontract-return inspection, and histories. |
| 24 | QC and condition movement | Service/API | **New** | QC queue, inspection, partial disposition, accepted posting, rejected hold, no accepted-portion reinspection, full replacement QC, and QC endpoints. |
| 25 | Stock ledger and availability | Schema | **Advance plus new** | Reuse warehouse/rack/bin, stock checks, reservations, handoffs, and minimal StockMovement; add immutable receipt/condition/issue/return/reversal/value postings and balances. |
| 26 | Stock ledger and availability | Service/API | **New** | Make only accepted stock available, preserve QC/rejected isolation, issue/return/posting/reversal, balance/reconciliation, and ledger endpoints. |
| 27 | Internal Issue Request | Schema | **New** | Header/lines constrained to one Job Order, purpose/reference, approval, issue, receiver, partial/reversal/return evidence, and BOM variance link. |
| 28 | Internal Issue Request | Service/API | **New** | Enforce approval without destination exception, allow multiple items/one Job Order, permit BOM overrun, notify without blocking, and expose request/approval/issue endpoints. |
| 29 | Returnable/non-returnable DC | Schema | **New** | DC type, purpose, linked issue/rejection/subcontract/demo, mandatory expected return date on every returnable DC, outstanding/overdue/closed states, return receipt, approvals, and notification evidence. |
| 30 | DC workflow | Service/API | **New** | Enforce mandatory return date, default demo returnable, calculate overdue, notify PRIYA E+TD+MD together after one rejected-vendor week/no auto-scrap, perform non-returnable owner approval and TD/MD in-app+email notifications, and expose DC endpoints. |
| 31 | Subcontract | Schema/service/API | **Advance plus new** | Reuse vendor quotation and PO; add material-out/DC, material-return, mandatory QC, PO-bill value, BOM allocation, workflow services, and endpoints. |
| 32 | Scrap/write-off | Schema/service/API | **New** | Stores decision, any-value TD and MD approvals, disposition/ledger evidence, services, and endpoints; reuse scrap location foundation only. |
| 33 | Labour, tasks, expenses, performance, inter-company billing | Schema | **Advance plus new** | Reuse employee/company assignments; add manager-entered/approved task hours split by Job Order/company, effective monthly salary rate, configurable expense claims/limits/bills/approvals, performance counts, and monthly GST service invoice with Accounts-selected HSN. |
| 34 | Labour, task, and expense workflow | Service/API | **New** | Production Manager enters/approves factory hours; Service Manager enters/approves service hours; apply issue-date salary/208, expense qualification/limit/proper-bill routes, BOM posting, company charging, performance view, GST service invoicing, and endpoints. |
| 35 | Warranty, AMC, and CAMC | Schema/service/API | **Advance plus new** | Reuse generic Asset warranty dates; add distinct contract/activity/cost/revenue models, project-BOM warranty costs, AMC/CAMC exclusion, services, and endpoints. |
| 36 | Notifications and escalation | Schema/service/API | **New** | Durable in-app/email notification and escalation records for non-returnable DC, BOM overrun, rejected-vendor one-week notification to PRIYA E+TD+MD, delivery/retry/evidence, configured emergency-count approach warning, and query endpoints. |
| 37 | Document/file handling | Schema | **Advance plus extension** | Reuse Document/DocumentRevision foundations; add typed links and metadata for offers, contract reviews, QC/FAT, DCs, bills, and JSON exports. |
| 38 | Document generation/storage | Service/API | **New** | AI-assisted Word generation with stored revisions, controlled downloads/uploads, comparison export, barcode labels, and file endpoints. |
| 39 | E-way-bill JSON | Schema/service/API | **New** | Company-scoped payload/version/status using the official NIC bulk-upload schema from ewaybillgst.gov.in, JSON validation/export, manual-upload tracking, portal result capture, and endpoints; reuse the same structure for future API integration, with no GSP integration now. |
| 40 | Cross-module audit and traceability | Schema/service/API | **Advance plus extension** | Reuse audit/history patterns; provide company-isolated end-to-end trace from need/PR through PO, GRN/QC, issue/DC, BOM, bill, warranty/service, labour, and e-way-bill export. |

RESULT_REPORTED_PENDING_WITNESS