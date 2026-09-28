> Converted from SESS_ERP_Stores_Full_Schema_Guideline.docx (SHA-256 5B0D026BECABE9734BC544E0B68DCAC5BBBAD227FE640C3D9318958D93A6C2A3) to Markdown on 27 September 2026 for traceability. Wording unchanged; layout simplified (headings, lists, tables).

**SESS ERP**

# Stores Full-Schema Decision & Implementation Guideline

**For SESS Proprietorship and SESS Pvt. Ltd.**

| **PURPOSE** Freeze the business rules before full-schema development, protect stock and custody traceability, and give the ERP team one approved implementation baseline. |
|---|

| **Document field** | **Value** |
|---|---|
| Document status | Guideline for Technical Director review and approval |
| Version | 1.0 |
| Prepared date | 31 August 2026 |
| Source reviewed | The full.docx - 12 contradictions and all 90 schema questions |
| Applies to | Stores, Purchase, Production, Service, QC, Projects, Accounts and HR handoffs |
| System scope | Separate company books with controlled shared masters and authorized consolidated reporting |

**Approval: Technical Director / Managing Director**

## 1. Executive Direction

The full-schema programme may proceed only after the rules in this guideline are accepted as the controlled baseline. Database tables, APIs, screens and reports must all enforce the same rule; a screen warning must never contradict a database hard control.

| **GO-LIVE RULE** No module may be released merely because its screen is complete. It must pass workflow, permission, posting, reversal, audit, reconciliation, reporting and acceptance tests. |
|---|

### 1.1 Current implementation position

| **Capability** | **Current position** | **Guideline** |
|---|---|---|
| Gate Entry, Warehouse, Rack | Executable services/endpoints exist | Retain and regression-test against the frozen company/site/location rules. |
| GRN, QC, MIR, DC, Stock Posting | Applied tables and PostgreSQL controls; application services/endpoints pending | Build services only after the 12 contradiction decisions are signed off. |
| BOM, Costing, Tools, Service and extended Stores | Specification stage | Design to the 90-question baseline and the approvals in this document. |

### 1.2 Guiding principles

- Physical truth first: record what entered, where it is, its condition, owner and custodian.
- Commercial authorization first: PO revision precedes acceptance of excess quantity; approved MIR precedes issue.
- No negative stock under any circumstance. Emergency work uses fast approval, transfer or approved substitute.
- Every posted movement is immutable. Correction is by typed reversal and reposting, never edit or delete.
- One item identity is retained across Purchase, Stores, Tool, Production and Service processes.
- Company scope comes from authenticated session and document ancestry, never operator-entered company text.
- Physical location, ownership, custody, reservation and valuation are separate dimensions.
- Every exception needs a named owner, reason, due date, evidence, approval and closure/reconciliation path.

## 2. Frozen Decisions for the 12 Contradictions

| **No.** | **Decision topic** | **Freeze** | **Implementation guideline** |
|---|---|---|---|
| 1 | QC hold | **KEEP** | QC_HOLD is a pending custody state, not a completed inspection result. Final QC is accept, partial accept or reject. |
| 2 | Over-receipt | **KEEP** | Record delivered excess physically, segregate it for return and exclude it from received stock. Revise the PO before acceptance. |
| 3 | Negative stock | **KEEP** | Hard block. No emergency override. Use another bin, authorized transfer, approved substitute or wait for receipt. |
| 4 | Issue before MIR approval | **KEEP** | Approval must precede physical issue and ledger posting. Provide fast/on-call emergency approval. |
| 5 | Issue beyond approved BOM | **CHANGE** | Require an approved BOM deviation/substitution before issue beyond the approved BOM balance. |
| 6 | Duplicate serial | **KEEP** | Warnings may occur during entry, but normalized stored serial identity must be unique at finalization. |
| 7 | Returnable loss/write-off | **CHANGE** | Allow closure only through an approved typed loss/damage/write-off reconciliation leg; never force-close. |
| 8 | Actual BOM costing | **KEEP** | Actual BOM uses accepted vendor bill allocation. Inventory issue valuation (weighted average/FIFO) remains separate. |
| 9 | QC inspector assignment | **CHANGE** | Resolve the effective QC_MANAGER employee and record the actual inspector; retain a recorded PR-raiser fallback. |
| 10 | Reserved stock | **KEEP** | Reservation is a commitment/availability bucket, not a location or stock movement. |
| 11 | QC hold vs quarantine | **KEEP** | QC_HOLD is routine inspection custody; QUARANTINE is exceptional safety/identity/compliance isolation. |
| 12 | Tool master identity | **KEEP** | Tool Master is a capability: existing Item plus serialized tool asset, calibration, kit and custody extensions. |

| **MANDATORY SIGN-OFF** If management changes any of these 12 decisions, the ERP team must issue a documented impact note covering affected tables, controls, APIs, screens, reports, migrations and test cases before coding. |
|---|

## 3. End-to-End Stores Operating Guideline

| **Step** | **Stage** | **Primary record** | **Owner** | **Release condition** |
|---|---|---|---|---|
| 1 | Demand | PR / project / service request | Department requester | Approved need and company/project identity |
| 2 | Purchase | PO and revisions | Purchase | Supplier, item, quantity, rate, tax and delivery authorization |
| 3 | Gate Entry | Vehicle/material inward | Security / Stores | Physical delivery, challan/invoice and delivered quantity recorded |
| 4 | GRN | Receipt against PO | Storekeeper | Received quantity within PO balance; excess segregated |
| 5 | QC | Inspection and disposition | Assigned QC employee | Accepted to AVAILABLE; rejected to PENDING_RETURNABLE_DC |
| 6 | Storage | Put-away / rack transfer | Stores | Exact warehouse, rack, condition, lot/serial and owner |
| 7 | Reservation | Commit stock | Authorized planner/request owner | Availability reduced; no physical movement |
| 8 | MIR & Issue | Approved material issue | Requester + approver + Stores | Atomic issue to project/service/employee custody |
| 9 | Fitment/Use | Production or service confirmation | Engineer / Production + QC | Actual consumption and BOM/service ancestry created |
| 10 | Return/Transfer | Unused return, DC, inter-site or intercompany | Current and receiving custodians | Dual acknowledgement and reconciliation |
| 11 | Billing/Cost | Accepted bill and valuation | Accounts | Procurement cost and inventory valuation retained separately |
| 12 | Closure | Project/DC/tool/service closure | Process owner | All quantities and exceptions reconcile to zero/open balance truth |

## 4. Mandatory Control Rules

| **Control** | **Required ERP behaviour** |
|---|---|
| Company separation | Every transactional table contains CompanyId derived from session/document chain. Cross-company visibility is privileged and read-only unless a formal transfer is active. |
| Site and location | Use company site -> warehouse -> rack/bin hierarchy. Do not store branch, van or site in free text. |
| Ownership | SESS-owned, customer-owned, supplier-loan and intercompany ownership remain separate from physical location. |
| Quantity | Posted AVAILABLE balance cannot fall below zero. Serialized balance is zero or one per company/location/custody state. |
| Serial/lot | Normalize serials; hard-unique at finalization. Batch, manufacture and expiry become mandatory by effective item policy. |
| Approvals | Approval authority is effective-dated, company-scoped and snapshotted on the transaction. Delegation must be explicit, time-bound and scope-bound. |
| Evidence | Accepted evidence is versioned and superseded, never overwritten. Store hash, uploader, time, document type and access classification. |
| Posting | All stock postings use one controlled atomic service/function with deterministic locking and typed source ancestry. |
| Correction | Use reversal linked to the original movement and corrected reposting. Preserve old and new records. |
| Closure | Status CLOSED is derived only when every line is returned, replaced, consumed, transferred, scrapped or reconciled through an approved exception. |

## 5. Process-Specific Guidelines

### 5.1 Receipt, GRN and QC

- Gate Entry records delivered quantity even when it exceeds the PO; GRN accepts only the PO balance.
- All GRN stock first enters QC_HOLD when QC is required. Items exempt by effective policy may route directly to AVAILABLE.
- Partial acceptance records accepted and rejected quantities in one final QC revision.
- Rejected material moves to PENDING_RETURNABLE_DC and needs disposition/return evidence.
- QUARANTINE is used only for exceptional identity, contamination, safety or compliance uncertainty.

### 5.2 Reservation, MIR and Issue

- Reservation changes availability only; it does not relocate stock.
- Every issue needs an approved MIR tied to company, project/service/cost centre and intended custodian.
- Emergency requests use fast approval; stock cannot leave before approval.
- BOM-controlled issues must reference a BOM line or approved deviation/substitution.
- Issue is not consumption. Actual BOM consumption begins only after confirmed fitment/use.

### 5.3 Production, Project and Actual BOM

- Project/Customer Order owns the project identity; Production owns machine instances under it.
- Each machine pins an approved BOM revision. Fitment must reference material receipt/issue origins.
- Shared material is explicitly allocated to machine instances; no unattributed average quantity or cost.
- Erroneous fitment is reversed and corrected; QC re-verifies the corrected fitment.
- Project closure is blocked until WIP is fully explained and reconciled.

### 5.4 Tools, Kits and Engineer Custody

- Purchased tool remains an Item and receives an individual asset identity when configured as serialized/controlled.
- Temporary issue records employee, purpose, issue time, due timestamp, condition and acknowledgement.
- Permanent assignment uses a stronger approval route and remains visible for resignation/clearance.
- Calibration-required tools are blocked after expiry until accepted calibration is recorded.
- Kit membership is effective-dated so historical contents remain reconstructable.

### 5.5 Customer Property, Demo Material and Returnables

- Customer material is custody property with zero SESS inventory value and separate declared/replacement value.
- Record customer, asset ID, make/model/serial, accessories, received condition, photographs, due date and owner.
- Transfers between engineers/sites require immutable handover and dual acknowledgement.
- Return evidence includes named recipient, timestamp, POD/signature and attachment hash.
- Loss or damage closes only through approved reconciliation, liability and recovery settlement references.

### 5.6 Service and Other-Brand Equipment

- ERP owns complaint, visit and job-card identities so spare issue and service history remain traceable.
- Installed assets use customer + customer asset ID + make/model/serial and a generated ERP asset ID.
- Warranty, AMC, CAMC and chargeable entitlement is effective-dated and snapshotted at the job decision.
- Removed-part ownership is explicitly decided; it is never inferred from physical possession.
- Old-to-new replacement serial ancestry is permanent.

### 5.7 Vendor Return, Jobwork and RMA

- Dispatch uses approved DC, item/serial/quantity, purpose, due date, carrier and POD.
- Jobwork inputs reconcile to returned output, accepted loss/scrap and open balance using approved conversion rules.
- Partial output acceptance and partial return are supported line-by-line.
- RMA resolution is typed: repair, replacement, credit/refund, return-as-is or approved write-off.
- Payment hold/release evidence is linked to the rejected line or invoice as decided by Accounts.

### 5.8 Site, Branch and Intercompany Transfers

- Transfer lifecycle is dispatch -> in transit -> receiving confirmation, with partial receipt and discrepancy handling.
- Issuing company retains ownership/value until destination acceptance.
- Destination storekeeper creates/accepts the receiving-company inward from the authorized transfer identity.
- GST invoice/e-way bill and accounting references are supplied by Accounts for applicable routes.
- Separate company projections share one immutable correlation ID; only authorized reconciliation roles see both sides.

## 6. Costing and Inventory Valuation

| **DO NOT COMBINE THESE TWO VALUES** Actual Procurement Cost answers what SESS agreed to pay for the material fitted to the machine. Inventory Issue Valuation answers what accounting value the stock ledger relieved. Both must be stored and reported. |
|---|

| **Measure** | **Source** | **Use** |
|---|---|---|
| ActualProcurementCost | Accepted vendor bill value allocated to fitted quantity/BOM line | Offer-versus-actual machine/project cost and purchasing performance |
| InventoryIssueValuation | Weighted-average initially; FIFO may be added prospectively | Inventory accounting and cost-of-stock relief |
| ValuationDifference | Actual procurement cost minus inventory issue valuation | Reconciliation/explanation only; never replaces Actual BOM cost |
| Memo/Replacement value | Declared or approved replacement value | Customer property, supplier-loan, warranty/free receipt and loss exposure |

Recommended policy: weighted average per company and Item at initial go-live. Any future method change is prospective from an approved effective date; historical postings are never recalculated.

## 7. Roles, Segregation and Approval Guideline

| **Activity** | **Prepare/Perform** | **Approve/Verify** | **Must not be combined** |
|---|---|---|---|
| PR / demand | Department requester | Department/project authority | Requester cannot self-approve above delegated authority |
| PO / revision | Purchase | Authorized Purchase/TD/MD route | Supplier cannot alter ERP authorization |
| Gate Entry / GRN | Security / Storekeeper | PO/system validation | Storekeeper cannot override PO balance |
| QC | Assigned QC employee | QC authority where required | Inspector cannot rewrite posted receipt |
| MIR issue | Requester + Storekeeper | Authorized department/project approver | Stores cannot approve its own demand |
| BOM deviation | Production/Project owner | Technical authority + commercial owner | Issue cannot precede deviation approval |
| Adjustment / write-off | Stores records facts | Independent approver; Accounts/TD for thresholds | Preparer cannot final-approve |
| Tool loss/recovery | Stores custody case | HR/Finance + authority | Stores does not decide payroll recovery |
| Vendor suspension | Purchase Manager | TD or MD | Score preparer cannot alone suspend/reactivate |

| **CONFIGURATION REQUIRED** Monetary, quantity, safety and duration thresholds must be effective-dated by company and transaction type. Until management approves the threshold matrix, the ERP must not hard-code amounts. |
|---|

## 8. SLA, Alerts and Escalation

| **Event** | **Recommended target** | **Escalation** |
|---|---|---|
| Gate Entry to GRN | Same working day; next working day for late arrival | Storekeeper -> Stores Manager |
| GRN requiring QC | Assignment immediately; disposition within 2 working days | QC assignee -> QC Manager -> TD for critical project |
| Rejected material return | DC preparation within 1 working day; dispatch within 3 | Stores -> Purchase -> vendor owner |
| Emergency MIR | Fast/on-call decision target 30 minutes | Department approver -> designated alternate |
| Temporary tool return | Reminder 3 days, 1 day and at due time | Engineer -> reporting manager -> HR clearance where overdue |
| Customer/demo returnable | Reminder 7 days, 3 days, 1 day and overdue daily | Custodian -> owner -> Stores Manager/TD |
| Transfer discrepancy | Record at receipt; investigate within 1 working day | Both Stores Managers -> Accounts/TD |
| Integration failure | Automatic idempotent retry; operator queue after terminal limit | Module owner -> IT/ERP administrator |

## 9. Documents, Audit and Security

- Minimum attachment metadata: company, source record, document type, version, original filename, MIME type, size, hash, uploader, upload time and sensitivity class.
- Recommended allowed formats: PDF, DOCX, XLSX, CSV, JPG and PNG. Executables, scripts and macro-enabled files are blocked unless IT explicitly approves.
- Recommended maximum: 25 MB per normal attachment; larger drawings/test data use controlled external storage linked by immutable reference.
- Sensitive classes: bank/KYC, pricing, customer confidential, inspection/test data, HR, legal/NDA and credentials/secrets. Access is role-based and logged.
- Retention follows the longer of statutory, contract, warranty/AMC/CAMC, tax, litigation-hold or company policy requirements. Deletion is never left to ordinary users.
- Capture both physical-event time and ERP-entry time. Delayed entry requires a reason and user identity.
- Finance and HR integrations use a transactional outbox with idempotent retries; Stores posting remains committed even if downstream delivery is temporarily unavailable.

## 10. Implementation Roadmap and Release Gates

| **Phase** | **Scope** | **Release gate** |
|---|---|---|
| 0. Freeze | Approve 12 contradictions, open-policy defaults, roles and thresholds | Signed baseline and change-control owner |
| 1. Core inward | Gate Entry, GRN, QC, put-away, serial/lot and rejected route | PO/GRN/QC reconciliation; no excess received stock |
| 2. Core outward | Reservation, MIR, issue, return, DC and transfers | No negative stock; approval-before-issue; full ancestry |
| 3. Project/BOM | BOM revisions, deviations, machine instances, fitment, WIP and Actual BOM | Offer-vs-actual and WIP closure reconcile |
| 4. Tools/Customer property | Tool assets, kits, calibration, engineer custody, demo/customer returnables | Due/overdue, handover, loss/write-off and clearance work |
| 5. Service/Jobwork/RMA | Installed assets, entitlements, service spares, vendor jobwork and RMA | Serial replacement and input-output reconciliation |
| 6. Costing/Planning | Accepted-bill allocation, weighted average, reorder, aging and vendor scorecards | Inventory GL/control totals and management reports agree |
| 7. Hardening | Security, integrations, performance, backup, DR and migration | UAT, reconciliation, permission and recovery evidence accepted |

## 11. Mandatory Acceptance Tests

- Attempt GRN received quantity above remaining PO balance: system blocks acceptance and routes excess to returnable segregation.
- Attempt duplicate normalized serial finalization: system blocks it while preserving the operator's verification trail.
- Attempt issue from AVAILABLE that would become negative: database and service both reject it.
- Attempt physical issue without approved MIR or required BOM deviation: system rejects it.
- Partial QC acceptance: accepted and rejected quantities reconcile exactly to inspected quantity and post to correct conditions.
- Reservation: availability reduces without a stock movement or physical location change.
- Reversal: original movement remains immutable and the reversal restores all item/location/serial/cost control totals.
- Tool calibration expiry: controlled tool cannot be issued; renewed certificate enables issue only after approval.
- Customer property transfer: sender and receiver acknowledgements preserve complete custody history.
- Loss/write-off: CLOSED is derived only after approved reconciliation; a direct force-close is impossible.
- Intercompany transfer: issuing and receiving companies remain isolated while shared correlation reconciles quantities.
- Actual BOM cost and inventory issue valuation appear separately and reconcile for multi-receipt material.
- Unauthorized user attempts cross-company access, approval delegation and attachment access: all are denied and audited.
- Integration retry duplicates the same message: downstream result remains idempotent with no duplicate accounting/HR record.

## 12. Management Decisions Still Requiring Confirmation

| **Area** | **Decision required** | **Owner** | **Required output** |
|---|---|---|---|
| Ownership/Finance | Supplier-loan valuation; customer-property declared/replacement value | TD + Accounts | Approve memo/liability valuation and closure rules |
| BOM/Production | BOM approver roles; subassembly yield/scrap/allocation | TD + Production | Approve revision workflow and conversion tolerances |
| Service | Billable spare rules; removed-part ownership | Service + Sales/Accounts | Define by Warranty/AMC/CAMC/Chargeable category |
| Customer property | Accessory tracking classes; due-date extensions; demo capitalization | Stores + TD + Accounts | Approve risk/value/duration matrix |
| Tools | Asset threshold; custody approvals; calibration owner/provider; loss recovery | TD + Stores + HR/Accounts | Approve tool class policies |
| Jobwork/RMA | Conversion and scrap tolerance; RMA outcomes; invoice hold/release | Production + Purchase + Accounts | Approve process/vendor/item policies |
| Intercompany | Permitted routes; GST/e-way bill/accounting documents | Accounts + TD | Approve legal routes before activation |
| Stock exceptions | Adjustment thresholds; backdating; ABC/FSN counts; scrap sale | Stores + Accounts + TD | Approve control matrix and open-period rule |
| Valuation/Planning | Landed-cost components; min/max approval; aging bands | Accounts + Purchase + Stores | Approve per-company/item-class policy |
| Documents/Security | File limits, sensitivity and retention; offline entry | IT/Security + Legal/Accounts | Approve information-control policy |
| Vendor performance | Frequency, weights and minimum sample | Purchase + TD | Approve scorecard policy |

## 13. Sign-Off Sheet

| **APPROVAL INSTRUCTION** Record any change against its question or contradiction number. Attach the approved impact note before authorizing development. Blank or pending items are not approval to hard-code a rule. |
|---|

| **Approval item** | **Decision / remarks** | **Name** | **Signature** | **Date** |
|---|---|---|---|---|
| 12 contradiction decisions | Approved / changes attached |  |  |  |
| 90-question baseline | Approved / changes attached |  |  |  |
| Role and approval matrix | Approved / pending |  |  |  |
| Implementation phases | Approved / revised |  |  |  |
| Authorization to proceed | YES / NO |  |  |  |

## Appendix A - Complete 90-Question Schema Baseline

This appendix carries every question from the supplied decision sheet. 'DEFAULT' means the proposed rule may be implemented unless management records a change. 'CONFIRM' means the recommended business rule must be formally confirmed before the dependent feature is released.

### Questions L1-L7

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| L1 | Is Reserved a commitment or ledger location? | commitment only; inherit contradiction 10. | **DEFAULT** |
| L2 | Is Project/Service WIP a physical location, custody account, or both? | both dimensions: actual physical location plus project/service custody account. | **DEFAULT** |
| L3 | Can one rack/bin contain multiple ownership classes? | yes, if physically identifiable; ownership is a balance/lot dimension, not a rack property. | **DEFAULT** |
| L4 | How is supplier-loan stock valued and closed? | Supplier-loan stock has zero owned-inventory value; record memo/replacement liability and close by return or approved consumption/loss. | **CONFIRM** |
| L5 | Does customer property need liability/replacement value despite zero inventory value? | yes; zero SESS inventory value, separate declared/replacement value. | **DEFAULT** |
| L6 | For future van/site stock, is the custodian employee, vehicle, site, or combination? | normalized custody assignment supporting employee plus optional vehicle/site; do not encode all three in one text field. | **DEFAULT** |
| L7 | Is every branch a company site, and may a site have multiple warehouses? | yes; company_site -> many warehouses. | **DEFAULT** |

### Questions P1-P7

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| P1 | Which module creates project and machine identity? | Project/Customer Order owns project; Production owns machine instances under it. | **DEFAULT** |
| P2 | BOM revision states, approvers and effective dates? | Draft -> Submitted -> Approved -> Superseded/Cancelled; machine pins approved effective revision; Technical authority approves. | **CONFIRM** |
| P3 | Can different machines in one project use different approved BOM versions? | yes; each machine instance pins its approved BOM revision. | **DEFAULT** |
| P4 | When does issued material become consumption? | confirmed fitment, followed by Production/QC reconciliation—not issue. | **DEFAULT** |
| P5 | How is erroneous fitment corrected? | reversing fitment plus corrected fitment revision; Production initiates and QC re-verifies. | **DEFAULT** |
| P6 | How are shared materials allocated across machines? | explicit quantity/cost allocations from one issue origin to individual machine instances. | **DEFAULT** |
| P7 | How do subassembly inputs become output quantity and cost? | Transformation batch records expected output, actual output, recoverable scrap and approved loss; allocate cost by output quantity unless approved otherwise. | **CONFIRM** |

### Questions P8-P10 (continued)

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| P8 | What evidence and approval does substitution require? | original item, substitute, quantities, reason, evidence, machine/BOM version and approved deviation. | **DEFAULT** |
| P9 | What happens on an excess-BOM request? | Approved BOM deviation/substitution is required before excess issue. | **DEFAULT** |
| P10 | Can a project close with nonzero WIP? | no unexplained WIP; only returned, fitted, transferred, scrapped, lost or reversed quantities may reconcile it to zero. | **DEFAULT** |

### Questions S1-S6

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| S1 | Does ERP own complaints, visits and job cards? | ERP owns them; service-spare ancestry otherwise remains fragmented. | **DEFAULT** |
| S2 | How is another-brand installed equipment identified? | customer + customer asset ID + make/model/serial, with a generated ERP asset ID. | **DEFAULT** |
| S3 | How are warranty/AMC/CAMC/chargeable entitlements represented? | effective-dated service-entitlement records attached to installed assets. | **DEFAULT** |
| S4 | Which service-spare uses are billable? | Warranty/CAMC covered spares are non-billable within contract scope; AMC labour only unless contract includes parts; chargeable work requires customer authorization. | **CONFIRM** |
| S5 | Who owns removed parts and what is their default disposition? | Record ownership for every removed part. Customer-owned by default unless PO/contract/warranty replacement terms transfer ownership. | **CONFIRM** |
| S6 | Must old-to-new serial ancestry be permanent? | yes, preserve every replacement link. | **DEFAULT** |

### Questions C1-C7

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| C1 | One register or separate customer-property and returnable registers? | common custody/movement core with typed customer-property and returnable cases. | **DEFAULT** |
| C2 | Which accessories need individual identity and condition? | Individually track serialized, high-value, safety-critical and customer-declared accessories; quantity-track low-value consumables. | **CONFIRM** |
| C3 | What proves customer receipt/return? | named recipient, timestamp, signature/POD and immutable attachment hash. | **DEFAULT** |
| C4 | Who approves due-date extensions and by what thresholds? | Custody owner proposes; department/project owner approves routine extension; TD approves repeated, high-value or overdue extensions. | **CONFIRM** |
| C5 | Can loss/write-off close a returnable? | Close only through approved loss/write-off reconciliation; no force-close. | **DEFAULT** |
| C6 | Can customer property move between sites or engineers? | yes, every transfer is an immutable handover. | **DEFAULT** |
| C7 | Is inbound demo equipment inventory, custody property or fixed asset? | Inbound demo equipment is custody property by default; capitalize only through explicit Accounts approval and asset-creation entry. | **CONFIRM** |

### Questions T1-T7

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| T1 | Is every tool an Item plus serialized asset extension? | yes; inherit contradiction 12. | **DEFAULT** |
| T2 | Which tools are individual assets versus quantity stock? | Create assets for serialized, calibration-required, powered, safety-critical or high-value tools; quantity-stock ordinary consumables/hand tools by policy. | **CONFIRM** |
| T3 | Is kit membership fixed or effective-dated? | effective-dated membership. | **DEFAULT** |
| T4 | Who approves permanent and temporary custody? | Department manager approves temporary custody; Stores Manager approves routine renewal; TD/MD approves permanent assignment/high-risk tools. | **CONFIRM** |
| T5 | Are temporary due dates dates, timestamps or class defaults? | exact timestamp, defaulted from effective tool-class policy. | **DEFAULT** |
| T6 | Who defines calibration intervals and provider authority? | Technical/Quality owner defines interval and accepted standards; Purchase maintains approved calibration providers; QC verifies certificate. | **CONFIRM** |
| T7 | Does expired calibration absolutely block issue/use? | yes for calibration-required tools; safety control should not be bypassed. | **DEFAULT** |

### Questions T8-T8 (continued)

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| T8 | How are loss, damage and employee recovery settled? | Stores records incident and custody; technical owner assesses; TD approves write-off; HR/Finance decides recovery with employee notice/evidence. | **CONFIRM** |

### Questions J1-J7

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| J1 | How are jobwork inputs reconciled to outputs? | Configure input-output equivalence by process, item, UOM and weight; each batch reconciles output, scrap/loss and remaining input. | **CONFIRM** |
| J2 | What scrap or weight tolerance is allowed? | Maintain effective tolerance by process/vendor/item; within tolerance auto-accepts, beyond tolerance requires deviation approval. | **CONFIRM** |
| J3 | How are subcontract PO/bill costs allocated? | allocate accepted bill cost to returned outputs/projects using explicit quantities or approved allocation basis. | **DEFAULT** |
| J4 | Can jobwork output be partially accepted and partially returned? | yes. | **DEFAULT** |
| J5 | Which RMA resolutions exist? | Support repair, replacement, credit/refund, return-as-is and approved write-off as typed terminal resolutions. | **CONFIRM** |
| J6 | What is the payment-hold lifecycle? | Hold the affected invoice line by default; full invoice hold only for legal/tax/payment dependency; Accounts releases with evidence. | **CONFIRM** |
| J7 | Who approves vendor-return dispatch? | Stores prepares; requesting/owning department approves before dispatch. Confirm escalation thresholds. | **DEFAULT** |

### Questions X1-X7

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| X1 | One-step transfer or dispatch/in-transit/receipt? | dispatch → in transit → receiving confirmation. | **DEFAULT** |
| X2 | Are partial receipts and transfer differences allowed? | yes, but differences require investigation and disposition. | **DEFAULT** |
| X3 | Who owns and values stock in transit? | issuing company retains ownership/carrying value until destination accepts it. | **DEFAULT** |
| X4 | Which intercompany routes are permitted? | Activate only company/site/warehouse pairs approved by TD and Accounts; all other routes hard-blocked. | **CONFIRM** |
| X5 | Which Finance documents accompany intercompany movement? | Accounts defines GST invoice/delivery challan/e-way bill and journal rules per legal route before go-live. | **CONFIRM** |
| X6 | Who creates the receiving-company inward record? | destination storekeeper accepts; the system prepares a company-scoped pending inward from the authorized transfer. | **DEFAULT** |
| X7 | How are both sides reconciled without broad cross-company visibility? | immutable transfer identity plus company-scoped views; privileged reconciliation role sees both. | **DEFAULT** |

### Questions G1-G7

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| G1 | Is QC hold a decision or custody state? | QC_HOLD is pending custody, not a terminal inspection decision. | **DEFAULT** |
| G2 | Is over-receipt permitted? | No received excess; record and segregate physical excess for return until PO revision. | **DEFAULT** |
| G3 | Can duplicate stored serials exist? | Normalized stored serial identity must be unique at finalization. | **DEFAULT** |
| G4 | Which items require batch/manufacture/expiry data? | effective Item/company inventory policy using existing tracking flags. | **DEFAULT** |
| G5 | Is inspector NARREN/fallback or effective QC role? | Resolve effective QC_MANAGER employee and record actual inspector; retain recorded fallback. | **DEFAULT** |
| G6 | Which returns require QC? | all configured item returns plus powder coating, CNC, milling and lathe returns; exact policy remains effective-dated. | **DEFAULT** |
| G7 | Where do rejected returns go? | PENDING_RETURNABLE_DC pending vendor/customer disposition; repair/scrap only through later governed decisions. | **DEFAULT** |

### Questions E1-E7

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| E1 | Can stock ever become negative? | Negative stock is never permitted. | **DEFAULT** |
| E2 | Can physical issue precede MIR approval? | Physical issue and posting require prior MIR approval; use fast emergency approval. | **DEFAULT** |
| E3 | Which adjustment types exist? | opening, count variance, damage, scrap, loss/write-off and correction/reversal; never a generic unexplained adjustment. | **DEFAULT** |
| E4 | What approval matrix applies to adjustments? | Use quantity/value/risk bands: independent approval required; Accounts and TD approve material thresholds and all write-offs. | **CONFIRM** |
| E5 | Who may backdate, and how far? | Backdate only into an open accounting period with Accounts approval, event date, entry date, reason and evidence. | **CONFIRM** |
| E6 | What is the opening-stock ceremony? | controlled import, full validation, dual approval, immutable batch and no overwrite. | **DEFAULT** |
| E7 | What count frequency and ABC/FSN schedule apply? | ABC: A monthly, B quarterly, C half-yearly; non-moving/critical items at least quarterly; annual wall-to-wall count. | **CONFIRM** |

### Questions E8-E10 (continued)

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| E8 | Does counting freeze everything or use a controlled window? | controlled freeze by company/warehouse/location/item scope, not a global shutdown. | **DEFAULT** |
| E9 | Who orders recount and approves final variance? | Independent person performs recount; Stores Manager approves routine variance; Accounts/TD approves threshold breach/write-off. | **CONFIRM** |
| E10 | How does scrap sale reach Finance? | Stores supplies approved scrap quantity/weight and custody evidence; Accounts creates tax invoice/receipt and confirms disposal closure. | **CONFIRM** |

### Questions V1-V7

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| V1 | FIFO or weighted average, and at what scope? | weighted average initially, per company and Item; preserve ability to add FIFO prospectively. | **DEFAULT** |
| V2 | Can valuation method change? | controlled prospective change only; never rewrite historical postings. | **DEFAULT** |
| V3 | Which cost components enter inventory value? | Exclude recoverable GST; include nonrecoverable tax, freight-in, duty and insurance. Installation is project cost unless needed to make inventory usable. | **CONFIRM** |
| V4 | What value is used before bill acceptance? | provisional PO value, replaced/reconciled by accepted bill after PO/bill match. | **DEFAULT** |
| V5 | Is Actual BOM accepted-bill cost or issue valuation? | accepted-bill cost; keep inventory issue valuation separately. | **DEFAULT** |
| V6 | How are free/warranty receipts valued? | zero procurement cost unless a real accepted liability exists; record memo/replacement value separately. | **DEFAULT** |
| V7 | Who maintains min/max/reorder/safety stock? | Stores/Purchase prepares min/max/reorder/safety stock; department owner reviews; designated manager approves effective version. | **CONFIRM** |

### Questions V8-V8 (continued)

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| V8 | What are slow/non-moving/obsolete thresholds? | Default aging: slow >90 days, non-moving >180 days, obsolete only by technical/commercial approval; override by item class. | **CONFIRM** |

### Questions D1-D7

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| D1 | File types, limits, storage and retention? | Allow PDF/DOCX/XLSX/CSV/JPG/PNG, normally <=25 MB; block executables/macros; retain per statutory/contract/warranty/litigation policy. | **CONFIRM** |
| D2 | Does replacement overwrite or supersede an attachment? | supersede; never overwrite accepted evidence. | **DEFAULT** |
| D3 | Which attachments are sensitive? | Classify bank/KYC, pricing, customer confidential, inspection/test, HR, legal/NDA and secrets as restricted with access audit. | **CONFIRM** |
| D4 | Must every document have Verified and Approved states? | no universal state chain; each document uses only business-required decisions. | **DEFAULT** |
| D5 | Are substitute/delegated approvers allowed? | only explicit, time-bound, scope-bound ERP delegation; never provider claims/groups. | **DEFAULT** |
| D6 | Is offline/delayed entry required? | Support physical-event time plus delayed-entry audit now; build offline client later only after sync/conflict design is accepted. | **CONFIRM** |
| D7 | Are Finance/HR integrations synchronous? | transactional outbox; Stores transaction commits independently, integration delivery is asynchronous/idempotent. | **DEFAULT** |

### Questions D8-D8 (continued)

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| D8 | What retry and reconciliation controls apply? | idempotent retries, durable attempts, terminal failure queue and operator reconciliation. | **DEFAULT** |

### Questions VP1-VP5

| **ID** | **Question** | **Guideline answer** | **Status** |
|---|---|---|---|
| VP1 | Evaluation period and frequency? | Calculate monthly with rolling 12-month view and immutable period snapshots. | **CONFIRM** |
| VP2 | Metric weights? | Initial weights: Quality 35%, Delivery 30%, Price 15%, Response 10%, Closure 10%; TD/Purchase may approve versioned changes. | **CONFIRM** |
| VP3 | Minimum sample before scoring? | Publish score only after at least 3 completed POs or 5 evaluated lines; otherwise show 'Insufficient sample'. | **CONFIRM** |
| VP4 | Who approves reevaluation or suspension? | Purchase Manager initiates; TD or MD approves suspension/reactivation. | **DEFAULT** |
| VP5 | Shared or company-scoped performance? | shared vendor identity, company-scoped scorecards, optional authorized consolidated management view. | **DEFAULT** |

## Appendix B - ERP Change-Control Form

| **Field** | **Required entry** |
|---|---|
| Change request number |  |
| Requested business-rule change |  |
| Reason / operational evidence |  |
| Companies / sites affected |  |
| Affected modules |  |
| Data migration impact |  |
| Permission / approval impact |  |
| Accounting / GST / HR impact |  |
| Reports and integrations affected |  |
| New test cases |  |
| Rollback / transition plan |  |
| Approvals | Technical owner / Process owner / Accounts / TD or MD |
