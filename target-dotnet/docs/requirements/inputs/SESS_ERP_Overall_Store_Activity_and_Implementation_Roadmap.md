> Converted from SESS_ERP_Overall_Store_Activity_and_Implementation_Roadmap.docx (SHA-256 EF371CEC26A58BDBE3155FDA09B6DED6D324F5FB55C9D52EF19F04443E2F82CA) to Markdown on 27 September 2026 for traceability. Wording unchanged; layout simplified (headings, lists, tables).

**SRI EASWARI SCIENTIFIC SOLUTION**

Overall Store Activity and ERP Implementation Roadmap

Functional Specification, Workflow Controls and Acceptance Requirements

| **Scope:** One integrated Store system for SESS Proprietorship and SESS Pvt. Ltd., covering manufacturing projects, service operations for SESS and other-brand products, tools, customer-owned machines, returnable demo materials and complete audit history. |
|---|

| **Document control** | **Details** |
|---|---|
| Document owner | Technical Director / Store Process Owner |
| Applicable entities | SESS Proprietorship (PROP) and SESS Pvt. Ltd. (PVT) |
| ERP integration | Sales → Project → Design → Purchase → Store → QC → Production → Dispatch → Finance → Service |
| Document status | ERP build specification - approval subject to end-to-end testing |
| Version / Date | Version 1.0 / 31 August 2026 |

Prepared for SESS ERP development, implementation and acceptance review

## Document Use and Approval Status

| **Verdict:** APPROVED WITH CORRECTIONS. The business requirement is valid. The Store module must include the controls in this document before final ERP approval. |
|---|

This document is the baseline for screen design, database rules, workflows, permissions, reports, alerts, testing and handover. Visual page completion alone is not proof of completion. Approval requires persistent records, correct stock calculations, secure permissions and successful end-to-end tests.

## Contents

| 1. Purpose, scope and operating principles | 15. Vendor returns, warranty and repairable spares |
|---|---|
| 2. Legal entity, warehouse and ownership architecture | 16. Dispatch staging, gate pass and outward control |
| 3. Master data required | 17. Scrap, damage, loss and stock adjustment |
| 4. Overall Store process pipeline | 18. Stock counting, valuation, aging and replenishment |
| 5. Purchase inward, GRN and QC | 19. Roles, permissions and approvals |
| 6. Daily project material issue, return and Actual BOM | 20. Dashboards, alerts and escalations |
| 7. Production WIP and machine traceability | 21. Required ERP pages, registers and reports |
| 8. Service spare parts for SESS and other-brand products | 22. Status, numbering, calculations and audit rules |
| 9. Customer-owned machines, motors and components | 23. Integrations and department handoffs |
| 10. Demo, sample, loan and other returnable material | 24. Exception handling and non-negotiable controls |
| 11. Tools, instruments and employee custody | 25. Implementation roadmap |
| 12. Engineer van, branch and site stock | 26. Acceptance test matrix and handover gate |
| 13. Subcontractor and job-work material | Appendix A. Minimum field dictionary |
| 14. Internal, inter-branch and intercompany transfers |  |

## 1. Purpose, Scope and Operating Principles

The Store ERP must provide one traceable record for every physical movement and every ownership class. A material, tool, customer machine or returnable item must be created once and reused from inward through issue, custody, consumption, return, dispatch or closure.

### 1.1 Included operations

- Manufacturing materials issued daily to factory projects and returned after work.
- Approved BOM, project reservation, Project WIP, actual fitment and As-Built BOM.
- Service spares for SESS-manufactured chambers and other-brand equipment.
- Customer machines, motors, controllers, compressors, sensors and demo materials received temporarily.
- Company tools permanently assigned to engineers and temporary tools due the same day or after a few days.
- Purchasing inward, GRN, QC, bin stock, transfers, job-work, vendor return, dispatch, scrap and stock count.

### 1.2 Non-negotiable operating principles

| **Principle** | **ERP rule** |
|---|---|
| One record, one lifecycle | No duplicate entry when the same item moves between departments. |
| Entity separation | PROP and PVT ownership, value, documents and ledgers remain separate. |
| Physical movement = system movement | No issue, return, transfer or outward without an ERP transaction. |
| No destructive deletion | Reverse/cancel with reason; preserve original transaction and audit history. |
| No negative stock | Block transactions unless an authorized emergency exception is recorded. |
| Ownership clarity | Company stock, customer property, supplier loan and employee custody are never mixed. |

## 2. Legal Entity, Warehouse and Ownership Architecture

### 2.1 Company control

| **Company code** | **Legal entity** | **Control** |
|---|---|---|
| PROP | Sri Easwari Scientific Solution - Proprietorship | Separate PO, stock valuation, project, service cost and outward documents. |
| PVT | Sri Easwari Scientific Solution Pvt. Ltd. | Separate PO, stock valuation, project, service cost and outward documents. |

| **Intercompany rule:** A PROP-owned item cannot be directly consumed in a PVT project, or vice versa. Use an approved intercompany loan/transfer/sale route selected by Accounts, with linked outward and inward references. |
|---|

### 2.2 Mandatory logical locations

| **Location class** | **Examples / purpose** |
|---|---|
| Available Store | Main store, branch store, rack/bin stock ready for issue. |
| QC / Quarantine | Received but not released; inspection pending. |
| Rejected / Return | Rejected material awaiting vendor outward. |
| Reserved | Quantity committed to a project or service job. |
| Project WIP | Issued to a project but not yet fitted, returned or scrapped. |
| Service WIP | Spare parts and customer property under service activity. |
| Customer Property | Zero-value custody location for customer-owned items. |
| Tool Crib / Custody | Tools available, employee assigned, temporary loan or calibration hold. |
| Demo / Returnable | Company or customer demo, sample, cylinder, pallet or loan item. |
| Engineer / Vehicle / Site | Named custodian sub-location with daily accountability. |
| Scrap / Damage | Segregated material pending approval and disposal. |
| Dispatch Staging | Packed and verified, not yet gate-out confirmed. |

## 3. Master Data Required

| **Master** | **Minimum controls** |
|---|---|
| Item Master | Unique item code, description, category, brand, model, UOM, HSN where used, drawing/specification, serial/batch rule, shelf-life, reorder levels, valuation method. |
| Tool & Instrument Master | Asset/tool code, type, make/model, serial number, calibration requirement, replacement value, condition and tool-kit membership. |
| Warehouse/Bin Master | Entity, branch, warehouse, zone, rack, shelf, bin, stock-status eligibility and responsible owner. |
| Project & Machine Master | Project number, company, customer, machine model/serial, approved BOM version, production owner and target dispatch date. |
| Customer/Vendor Master | Legal name, address, GST/PAN as applicable, contacts, customer/vendor code and outward-document requirements. |
| Employee/Engineer Master | Employee ID, department, manager, branch, active status and custody clearance status. |
| Reason/Status Master | Return, scrap, damage, adjustment, cancellation, rejection and delay reasons; configurable and auditable. |
| Approval Matrix | Transaction, value/condition, verifier, approver, escalation time and substitute approver. |

### 3.1 Item classification

- Raw material and bought-out component
- Manufactured/sub-assembly item
- Consumable and PPE
- Project-specific material
- Service spare and repairable spare
- Tool, jig, fixture and calibrated instrument
- Customer property
- Demo/sample/loan/returnable item
- Packing material, cylinder, pallet and container
- Scrap or obsolete material

## 4. Overall Store Process Pipeline

| **STAGE 1** | **STAGE 2** | **STAGE 3** | **STAGE 4** |
|---|---|---|---|
| **Create / Approve Masters** | **Receive / Verify** | **Store / Reserve** | **Issue / Track** |

| **STAGE 1** | **STAGE 2** | **STAGE 3** | **STAGE 4** |
|---|---|---|---|
| **Fit / Consume / Use** | **Return / Transfer** | **Reconcile / Close** | **Report / Audit** |

| **Trigger** | **Start record** | **End state** |
|---|---|---|
| Purchase material | Approved PO / expected inward | Accepted stock, rejected return or hold. |
| Project requirement | Approved BOM and MRS | Actual BOM, zero explained WIP and project reconciliation. |
| Service requirement | Complaint / service visit / job card | Consumed/returned spare and closed service cost. |
| Customer machine received | Customer Property Inward | Returned to customer with acknowledgement. |
| Tool issue | Approved custody/temporary request | Active custody or verified return/clearance. |
| Demo/loan receipt | Returnable Inward | Returned before due date or approved extension. |

## 5. Purchase Inward, GRN and QC

Expected inward should originate from an approved PO. Store selects the PO; the ERP carries vendor, company, item, ordered quantity and project reservation automatically.

### 5.1 Process

- Gate inward and vehicle/document capture
- PO match and duplicate invoice/challan check
- Physical quantity and packing verification
- GRN creation with batch/serial/photos
- QC or physical verification
- Accept, partial accept, reject or hold
- Bin put-away and barcode label
- Automatic update to inventory, project reservation and Accounts

### 5.2 Mandatory controls

| **Check** | **Rule** |
|---|---|
| Excess/short receipt | Tolerance configurable; excess requires approval. |
| Serial/batch | Mandatory for configured items; duplicate serial blocked. |
| QC release | Stock cannot be issued while QC/Hold status is active. |
| Attachments | Invoice/challan, inspection report, photos and certificates visible by filename. |
| Project-linked PO | Accepted stock automatically fulfills the linked reservation. |
| Partial receipt | PO balance remains open until completed or formally closed. |

## 6. Daily Project Material Issue, Return and Actual BOM

| **Critical design rule:** Material issue moves stock from Main Store to Project WIP. It does not immediately create project consumption or Actual BOM. |
|---|

### 6.1 Daily material workflow

| **STAGE 1** | **STAGE 2** | **STAGE 3** | **STAGE 4** |
|---|---|---|---|
| **Approved BOM / MRS** | **Reserve & Pick** | **Issue to Project WIP** | **Fit / Return / Scrap** |

- Production raises a Material Requisition Slip (MRS) against company, project, machine and approved BOM version.
- ERP shows BOM quantity, previous issue, previous return, actual fitment, WIP balance, pending reservation and store availability.
- Store picks by bin/batch/serial and records receiver employee ID and date/time acknowledgement.
- Production posts item activity: Fitted, Returned, Approved Scrap, Damaged, Pending in WIP, or Approved Project Transfer.
- Store physically verifies returned material and places it into Available, QC Hold, Repair or Scrap location.
- Production Manager and QC reconcile the machine; Actual/As-Built BOM is generated only from confirmed fitment.

### 6.2 BOM views

| **BOM view** | **Purpose** | **Editable by** |
|---|---|---|
| Estimated / Design BOM | Initial design and costing estimate. | Design; revision controlled. |
| Approved Production BOM | Reservation, request and standard project issue. | Design/Production; TD approval. |
| Actual / As-Built BOM | Components actually fitted in the finished machine. | System-generated from fitment; QC verification. |

### 6.3 Required calculations

| **Calculation** | **Formula / rule** |
|---|---|
| Net issued | Total issued − quantity physically returned to Store. |
| Project WIP balance | Issued − fitted − returned − approved scrap/damage − approved transfer out. |
| Actual BOM quantity | Quantity confirmed as fitted/installed against the machine serial number. |
| Quantity accountability | Fitted + returned + approved scrap/damage + transfer out + WIP balance must equal issue quantity. |
| BOM usage variance | Actual fitted quantity − approved BOM quantity. |
| Project material cost | Actual fitted cost + approved project scrap/wastage cost; show separately. |

### 6.4 Material return

The return must reference the original issue note. Store verifies item, quantity, batch/serial, condition and reason. A returned item must not become Available Stock until verification is complete.

| **Return condition** | **Destination** |
|---|---|
| Unused / sealed / good | Available stock. |
| Opened but reusable | Available with condition/remaining quantity. |
| Requires inspection | QC Hold. |
| Repairable | Repair / Service Hold. |
| Damaged or scrap | Scrap/Damage location pending approval. |
| Customer-owned | Customer Property location; never company stock. |

### 6.5 Project closure gate

- Project WIP balance is zero or an approved, explained exception exists.
- Every issued serialised item is fitted, returned or otherwise accounted.
- BOM deviations and substitute parts are approved.
- Scrap/damage is approved and cost posted separately.
- Actual BOM is verified by Production/QC and approved as configured.
- Open tools, customer property or returnable items linked to the project are cleared.

## 7. Production WIP and Machine Traceability

Each finished machine requires a unique machine serial number. Serialised components, compressor/controller/PLC/HMI/sensor details, software/firmware where relevant, actual fitment date and responsible engineer must be linked to that machine.

| **Control** | **Requirement** |
|---|---|
| WIP location | Project and machine-specific, not a single common untraceable WIP balance. |
| Sub-assembly | Create/consume through authorised production transaction with input and output traceability. |
| Replacement during testing | Remove old part, record condition and disposition, fit new part and update Actual BOM history. |
| Substitution | Design/QC reason and TD approval before permanent As-Built confirmation. |
| Test failure material | Move to Repair/Hold or Scrap; never silently reissue. |
| Finished machine | Actual BOM and serial trace support warranty and future service history. |

## 8. Service Spare Parts for SESS and Other-Brand Products

Service stock must support SESS-manufactured products and third-party/other-brand products. Each spare issue must link to the Service Complaint, Service Visit and Job Card; brand ownership does not change the audit requirement.

### 8.1 Service material flow

| **STAGE 1** | **STAGE 2** | **STAGE 3** | **STAGE 4** |
|---|---|---|---|
| **Complaint / Job Card** | **Engineer Request** | **Store / Van Issue** | **Use / Return / Bill** |

| **Usage type** | **ERP treatment** |
|---|---|
| Warranty - SESS machine | Post to warranty service cost; link original machine and complaint. |
| Chargeable SESS service | Post quantity/cost to job; pass billable line to Finance. |
| Other-brand service | Link other-brand make/model/serial and job; bill or contract-consume as authorised. |
| AMC/CAMC | Validate contract entitlement and included/excluded spare rule. |
| Trial / diagnosis | Temporary issue; unused quantity and removed old part must be returned/accounted. |
| Customer-supplied spare | Customer property; zero company stock value. |

### 8.2 Removed/defective parts

- Capture old part description, make/model, serial, failure reason, photos and customer disposition.
- Classify as customer returned, repairable core, warranty return, scrap with customer approval or left at site.
- If exchanged, link new serial to old serial and retain the chain for future repeat-call analysis.

## 9. Customer-Owned Machines, Motors and Components

| **Ownership rule:** Customer property is custody stock with zero company inventory value. It must be physically segregated and cannot be consumed, sold or transferred as SESS-owned stock. |
|---|

### 9.1 Customer Property Inward

| **Mandatory field** | **Requirement** |
|---|---|
| Customer and contact | Legal customer, site, responsible person, phone/email. |
| Item identity | Machine/motor/component, brand, model, serial number and customer asset ID. |
| Purpose | Repair, service, testing, calibration, inspection, trial or demo. |
| Condition | Working/non-working, visible damage, packing, seal and photos/video. |
| Accessories | Cable, sensor, controller, coupling, documents, fixtures; individual checklist. |
| Documents | Customer challan, PO/service request, inward gate pass and acknowledgement. |
| Dates | Receipt date, promised action date, expected return date and escalation dates. |
| Location/custodian | Customer Property bin, service area or assigned engineer. |

### 9.2 Outward and closure

- Service/QC completion and repair report attached.
- All accessories reconciled against inward checklist.
- Customer approval or dispatch instruction recorded.
- Outward gate pass/challan reference and courier/vehicle details captured.
- Customer acknowledgement, POD or signed receipt attached.
- Record closes only after return proof; overdue dashboard clears automatically.

## 10. Demo, Sample, Loan and Other Returnable Material

Use one Returnable Material Register for items received from customers/vendors and items issued outward by SESS. It covers demo units, sample products, trial controllers, sensors, cylinders, pallets, packaging boxes, fixtures and temporary loan equipment.

| **Direction** | **Control** |
|---|---|
| Customer/vendor → SESS | Record owner, due-return date, purpose, condition, custodian, extension approvals and final return proof. |
| SESS → customer/site | Reserve company asset, outward document, recipient acknowledgement, expected return, reminders and condition-on-return. |

### 10.1 Due-date control

- Reminder at configurable days before due date.
- Daily overdue reminder to custodian and Store.
- Escalate to department head, then TD/MD based on overdue days/value.
- Extension requires revised due date, reason and approval; preserve original due date.
- No closure without physical return verification or approved loss/write-off.

## 11. Tools, Instruments and Employee Custody

### 11.1 Two issue modes

| **Mode** | **Use** | **Return control** |
|---|---|---|
| Permanent / fixed custody | Tool kit assigned to engineer for ongoing work. | No daily due date; periodic verification and mandatory employee-exit clearance. |
| Temporary issue | Same-day or few-days requirement for project/site/service. | Mandatory due date/time, reminder, escalation and physical return verification. |

### 11.2 Tool lifecycle

| **STAGE 1** | **STAGE 2** | **STAGE 3** | **STAGE 4** |
|---|---|---|---|
| **Register / Label** | **Issue / Accept** | **Use / Inspect** | **Return / Clear** |

| **Activity** | **Mandatory rule** |
|---|---|
| Tool issue | Employee ID, project/service reference, condition, accessories, date/time and acknowledgement. |
| Tool transfer between engineers | Return/transfer transaction and receiver acceptance; never change custodian silently. |
| Return | Store checks condition, accessories, calibration seal and working status. |
| Loss/damage | Incident, employee explanation, manager verification and TD/MD decision; recovery if authorised. |
| Calibration instrument | Block issue after calibration expiry; alerts before due date; certificate history. |
| Employee resignation/transfer | HR full-and-final clearance blocked until every custody item is returned or formally settled. |

## 12. Engineer Van, Branch and Site Stock

Every engineer, vehicle, branch and long-running customer site must be configured as a controlled stock sub-location. Store-to-van movement is a transfer, not consumption.

- Engineer accepts transferred stock by barcode/quantity.
- Consumption requires a project or service job reference and customer/engineer confirmation as applicable.
- Unused stock returns to Store or remains as declared van balance.
- Periodic van stock count compares physical balance to ERP.
- Branch-to-branch and site-to-store movements require linked outward/inward transactions.
- High-value and serialised parts require individual traceability.

## 13. Subcontractor and Job-Work Material

| **STAGE 1** | **STAGE 2** | **STAGE 3** | **STAGE 4** |
|---|---|---|---|
| **Job-work Request** | **Material Outward** | **Vendor Processing** | **Return / Reconcile** |

| **Control** | **Requirement** |
|---|---|
| Outward | Company, project, subcontractor, raw item, quantity, serial/batch, job specification and expected return. |
| Ownership | Material remains SESS-owned and appears as Stock at Subcontractor. |
| Return | Finished item, balance raw material, scrap, rejected quantity and shortages recorded separately. |
| Reconciliation | Input issued = finished output equivalent + returned balance + approved scrap/loss. |
| Quality | QC accepts/rejects returned job-work material before Available/WIP release. |
| Overdue | Expected return reminders and vendor/Production escalation. |

## 14. Internal, Inter-Branch and Intercompany Transfers

| **Movement** | **ERP document and control** |
|---|---|
| Bin-to-bin | Internal Transfer Note; source decreases and destination increases in one controlled posting. |
| Warehouse/branch | Transfer Out → In Transit → Receiving confirmation; quantity/condition difference workflow. |
| Project-to-project | Return/reallocation or approved Project Transfer; both project cost and WIP updated. |
| PROP ↔ PVT | Intercompany reference with Accounts-approved supporting document; two separate company ledger postings linked. |

| **In-transit visibility:** ERP must show Current, Reserved, Project WIP, Service WIP, In Transit, QC Hold and Available balances separately. |
|---|

## 15. Vendor Returns, Warranty and Repairable Spares

- Vendor Return Note must reference GRN/PO, rejection reason, quantity, serial/batch, debit/replacement expectation and outward proof.
- Warranty/RMA item must track dispatch date, vendor RMA number, expected return/replacement, reminders and final resolution.
- Repairable spares require status: Defective → Sent for Repair → Returned → QC → Available/Rejected/Scrap.
- Replacement received must link original serial/item and close the vendor follow-up only after verification.

## 16. Dispatch Staging, Gate Pass and Outward Control

No material may leave the premises without an authorised outward transaction. Dispatch staging must not be treated as final gate-out until security or Store confirms physical exit.

| **Outward purpose** | **Required reference** |
|---|---|
| Customer machine delivery | Project/dispatch clearance, packing list, approved outward document and transport details. |
| Customer property return | Customer Property Inward, service completion and recipient/POD acknowledgement. |
| Tool/demo loan | Returnable outward, custodian/recipient and due-return date. |
| Vendor/job-work/warranty | Vendor Return, Job-work Outward or RMA. |
| Scrap disposal | Approved scrap disposal and buyer/vehicle/weighment references. |

## 17. Scrap, Damage, Loss and Stock Adjustment

| **Transaction** | **Approval/control** |
|---|---|
| Production scrap/wastage | Project, item, quantity, reason, photo/weight, cost and manager verification. |
| Damaged stock | Incident and responsible location/custodian; QC disposition. |
| Lost/theft | Incident report, investigation, management decision and recovery/write-off reference. |
| Stock adjustment | Only after count/investigation; system-calculated variance, mandatory reason and approver. |
| Scrap sale/disposal | Segregation, approval, buyer, rate/weight, outward proof and Finance posting. |

### 17.1 Value-based approval baseline

| **Value** | **Approval** |
|---|---|
| ₹0 - ₹50,000 | Responsible Manager |
| ₹50,001 - ₹5,00,000 | Technical Director |
| Above ₹5,00,000 | Managing Director |

Thresholds must be configurable. Sensitive, high-risk or intercompany transactions may require TD/MD approval regardless of value.

## 18. Stock Counting, Valuation, Aging and Replenishment

### 18.1 Physical stock verification

- Daily spot check for fast-moving/high-value items.
- Cycle count by ABC/FSN category and risk.
- Monthly project WIP and engineer/vehicle custody verification.
- Periodic full stock count with freeze or controlled transaction window.
- Blind count option, recount, variance investigation and approval.
- Count records preserved; no silent opening-balance overwrite.

### 18.2 Stock planning and valuation

| **Function** | **Requirement** |
|---|---|
| Available quantity | On-hand accepted − reserved − blocked/hold. |
| Reorder | Min/max/reorder quantity, lead time and configurable safety stock. |
| Aging | Receipt/last-movement age; non-moving and obsolete classifications. |
| Expiry/shelf life | FEFO where applicable; alerts and issue block after expiry. |
| Valuation | FIFO or weighted average, company-wise and configurable with controlled changes. |
| Project commitment | Reserved value, issued WIP, actual consumption and variance. |

## 19. Roles, Permissions and Approvals

| **Role** | **Main rights** | **Restrictions** |
|---|---|---|
| Store Executive | Inward entry, picking, issue, return receipt, bin movement and physical count entry. | Cannot approve own adjustments, scrap or high-value exceptions. |
| Store Manager | Verify GRN/returns, approve routine movement, supervise reconciliation and reports. | Cannot bypass entity ownership or audit controls. |
| Production Engineer/Supervisor | MRS, fitment, return initiation, WIP declaration and tool acknowledgement. | Cannot edit Store quantities or final Actual BOM approval. |
| Production Manager | Approve production requests, WIP reconciliation and project material closure. | Cannot erase transaction history. |
| Service Engineer | Service material request, van custody, usage/return and customer property handling. | Only assigned jobs/stock scope. |
| Service Manager | Allocate/verify service material and customer property closure. | No company-wide stock adjustment. |
| QC | Inspection, hold/release/reject and Actual BOM verification. | Cannot alter PO/project commercial values. |
| Accounts | Valuation, intercompany/document verification and Finance integration. | No physical stock manipulation. |
| TD/MD | Final exception, deviation, write-off and controlled full access. | Actions remain audited. |
| Viewer/Auditor | Read/export within authorised scope. | No transactional action. |

### 19.1 Permission architecture

Configure permissions in this order: Page Master → Role Master → Role-Page Permission → User Mapping → Dynamic Sidebar → Button Control → URL Security → Dashboard Control → Testing. Menu hiding alone is not security; block direct URL and backend/API access.

## 20. Dashboards, Alerts and Escalations

| **Dashboard / alert** | **Recipients** |
|---|---|
| Low stock, out-of-stock and pending reservation | Store, Purchase, Production/Service owner. |
| MRS pending / partially issued | Store and requester. |
| Project WIP aging and unaccounted material | Production Manager, Store Manager, TD. |
| Actual BOM variance / unapproved substitution | Design, QC, Production Manager, TD. |
| Temporary tool due/overdue | Employee, manager and Store; escalation to TD/HR. |
| Calibration due/expired | Custodian, Store/QC; block issue after expiry. |
| Customer/demo material due return | Custodian, Service/Project owner, Store and escalation chain. |
| Vendor/job-work/RMA overdue | Purchase, Store, Production/Service owner. |
| QC/rejected stock aging | QC, Purchase and Store. |
| Non-moving/obsolete stock | Store, Purchase, Accounts, TD/MD. |

## 21. Required ERP Pages, Registers and Reports

### 21.1 Transaction pages

| **Module** | **Pages** |
|---|---|
| Inward | Expected Inward, Gate Inward, GRN, QC/Physical Verification, Put-away, Rejection/Vendor Return. |
| Project materials | BOM Reservation, MRS, Pick List, Material Issue, Project WIP Activity, Material Return, Fitment, BOM Deviation, Actual BOM Reconciliation. |
| Service | Service Material Request, Service Issue/Return, Engineer/Van Stock, Removed Part, Warranty/Chargeable Consumption. |
| Customer property | Customer Property Inward, Movement/Custody, Service/Inspection, Return Outward and Acknowledgement. |
| Tools | Tool Register, Permanent Custody, Temporary Issue/Return, Transfer, Calibration, Damage/Loss and Employee Clearance. |
| Returnables | Returnable Inward/Outward, Due Extension, Reminder/Escalation and Closure. |
| Transfers | Bin, Warehouse, Branch, Project, In-Transit and Intercompany Transfer. |
| Controls | Stock Count, Adjustment, Scrap/Damage, Gate Pass, Dispatch Staging and Reversal/Cancel. |

### 21.2 Essential reports

- Company-wise Stock Ledger with opening, inward, outward and closing balance.
- Item-wise, bin-wise, batch-wise and serial-wise stock.
- Available, reserved, QC hold, Project WIP, Service WIP and in-transit stock.
- Project approved BOM vs issued vs returned vs fitted vs scrap vs Actual BOM variance.
- Project WIP aging and unaccounted issue report.
- Service job material and engineer/van stock report.
- Customer property and demo/returnable due/overdue register.
- Tool custody, overdue, loss/damage and calibration report.
- GRN pending QC, rejected stock and vendor return/RMA aging.
- Subcontractor stock and job-work reconciliation.
- Stock valuation, ABC/FSN, aging, non-moving and obsolete report.
- Scrap, damage, write-off and stock-adjustment audit report.
- Intercompany movement reconciliation.
- User activity, status history, approval history and change audit export.

## 22. Status, Numbering, Calculations and Audit Rules

### 22.1 Common statuses

Draft → Submitted → Verified → Approved → Reserved → Partially Issued → Fully Issued → In WIP/Custody → Partially Accounted → Reconciled → Closed. Exception states: Clarification, Revision Requested, Hold, Rejected, Overdue, Damaged, Scrap Pending, Cancelled/Reversed with Reason.

### 22.2 Document numbering

| **Document** | **Example pattern** |
|---|---|
| GRN | PVT-GRN-2026-27-00001 |
| Material Request | PVT-MRS-2026-27-00001 |
| Material Issue | PVT-MIN-2026-27-00001 |
| Material Return | PVT-MRN-2026-27-00001 |
| Tool Issue | PVT-TIN-2026-27-00001 |
| Customer Property Inward | PVT-CPI-2026-27-00001 |
| Returnable Outward | PVT-ROU-2026-27-00001 |

Number sequences must be continuous, company-aware, immutable after posting and configurable by financial year. Cancelled numbers remain visible.

### 22.3 Audit fields on every controlled record

- Created/modified/submitted/verified/approved/rejected/closed by and timestamp.
- Status, approval and change history with old value/new value.
- Company, branch, warehouse, project/service/customer/vendor/employee references as applicable.
- Attachments with filename, version, View/Download/Replace rights.
- Source transaction and reversal/correction reference.
- Unauthorized access attempt log and export history where sensitive.

## 23. Integrations and Department Handoffs

| **Source / destination** | **Automatic handoff** |
|---|---|
| Purchase → Store | Approved PO and expected inward lines; GRN/rejection/vendor return status back to Purchase. |
| Design/Project → Store | Approved BOM, revision and project reservation demand. |
| Store → Production | Availability, issue, WIP and return status; actual fitment feeds Actual BOM. |
| Store → Service | Job-linked spare availability/issue; engineer usage/return and customer property status. |
| QC ↔ Store | Inspection tasks, hold/release/reject decisions and certificates. |
| Store → Dispatch | Verified material/machine readiness, staging and outward clearance. |
| Store → Finance | GRN, consumption, valuation, intercompany, scrap/write-off and billable service spares. |
| HR ↔ Tool Custody | Employee status and mandatory asset/tool clearance before exit settlement. |

## 24. Exception Handling and Non-Negotiable Controls

| **Exception** | **System behavior** |
|---|---|
| Emergency after-hours issue | Allow controlled emergency issue to named custodian; next-working-day verification/approval mandatory. |
| Backdated transaction | Blocked after period lock; exceptional entry requires reason and authorised approval. |
| Wrong posting | Use reversal and correct repost; never edit/delete posted ledger movement. |
| Insufficient stock | Block negative stock; show alternate bin/entity/approved substitute route. |
| Excess BOM request | Create BOM deviation request; cannot silently exceed approved balance. |
| Return quantity above issue | Block and require investigation. |
| Duplicate serial/invoice/challan | Block or warn based on configured control and require verification. |
| Receiver refuses/does not acknowledge | Keep pending acceptance; escalate to manager. |
| Offline/site delay | Queue controlled entry with original physical-document time and later sync audit. |

| **Security:** Backend and database rules must enforce company scope, record scope, action rights and approval segregation. Hidden buttons or menus alone are insufficient. |
|---|

## 25. Implementation Roadmap

| **Phase** | **Scope** | **Completion evidence** |
|---|---|---|
| 1. Process & master lock | Confirm legal entities, warehouses/bins, item/tool classifications, owners, numbering, statuses and approval matrix. | Signed process map and master-data templates. |
| 2. Security foundation | Page/role/user permissions, company/branch scope, buttons, direct URL/API controls and audit. | Permission matrix and denied-access tests. |
| 3. Core inventory | PO expected inward, GRN, QC, put-away, stock ledger, reservation, transfers and returns. | Persistent end-to-end stock test. |
| 4. Project material control | MRS, issue to Project WIP, fitment, return, scrap, BOM deviation, Actual BOM and closure gate. | Complete BOM-to-As-Built demonstration. |
| 5. Service & customer property | Service spares, other-brand jobs, engineer/van stock, customer machine inward/return. | Complaint/job-to-material closure test. |
| 6. Tools & returnables | Permanent custody, temporary issue, calibration, resignation clearance, demo/sample due control. | Issue/overdue/return/clearance tests. |
| 7. Advanced flows | Job-work, vendor RMA, dispatch staging, intercompany, stock count, scrap and valuation. | Reconciliation and Finance handoff tests. |
| 8. Dashboards & reports | Pending tasks, alerts, aging, variance, valuation and audit exports. | Report-to-ledger reconciliation. |
| 9. Migration & UAT | Opening stock, serial/tools/customer items, role mapping, realistic user tests and corrections. | Signed UAT and unresolved-issue list. |
| 10. Go-live & handover | Cutover, training, backups, support, source/database/configuration handover. | Successful restore, runbook and final acceptance. |

### 25.1 Recommended build priority

| **First demonstration:** Approved BOM → MRS → reservation → issue to Project WIP → partial fitment → partial return → approved scrap → Actual BOM → zero WIP reconciliation → project material closure. |
|---|

## 26. Acceptance Test Matrix and Handover Gate

| **Test** | **Expected result** |
|---|---|
| Company segregation | PROP user/project cannot consume PVT stock without approved intercompany flow. |
| Duplicate prevention | Duplicate invoice, serial or repeated source transaction is blocked/warned as designed. |
| Save persistence | Record remains correct after refresh, logout/login and database restart. |
| Project reconciliation | Issue quantity fully reconciles to fitted, returned, scrap/damage, transfer and WIP. |
| Actual BOM | Contains fitted quantity only; returned and scrap quantities are excluded and separately reported. |
| Unauthorized access | Menu, direct URL, API and export are denied and attempt is logged. |
| Approval branches | Approve, reject, clarify, revise, resubmit, cancel/reverse and close work with full history. |
| Due alerts | Tool, demo, customer property, calibration and job-work reminders/escalations trigger correctly. |
| Attachment lifecycle | Upload, filename display, view, download, replace and failure message work. |
| Reports | Ledger, stock, project cost, Actual BOM and exports reconcile to database transactions. |
| Backup/restore | Critical data and attachments restore successfully in a repeatable test. |

### 26.1 Final handover requirements

- Working end-to-end modules with persistent database records.
- Final role/permission matrix and approval settings.
- Database schema, migrations and master-data import templates.
- Full unprotected source code, build/run instructions and configuration documentation.
- Backup/restore procedure and successful restore evidence.
- User manual, admin manual, training and test report.
- Open-issue list, support escalation path, SLA and warranty/support period.

| **Final approval gate:** Do not approve final acceptance or final payment until the working system, security tests, reconciled reports, source/database handover and backup/restore evidence are complete. |
|---|

## Appendix A. Minimum Field Dictionary

| **Record** | **Key mandatory fields** |
|---|---|
| All transactions | Document no., company, branch, warehouse, date/time, source, status, creator/verifier/approver, remarks and attachments. |
| Stock line | Item code, description, UOM, quantity, bin, batch/serial, condition/status, ownership and unit/value basis. |
| Project movement | Project, machine serial, BOM version/line, requested/reserved/issued/fitted/returned/scrap/WIP quantity and receiver. |
| Service movement | Complaint, visit/job card, machine make/model/serial, warranty/AMC/CAMC/chargeable classification and engineer. |
| Tool custody | Tool code/serial, kit, employee, permanent/temporary mode, issue/return condition, due date, calibration and clearance. |
| Customer property | Customer, item identity, condition/photos, accessories, inward document, purpose, promised/due date, location, outward/POD. |
| Returnable | Owner, direction, item/serial, purpose, custodian/recipient, condition, due/extension, reminder and return proof. |
| Exception | Reason code, explanation, value impact, evidence, approver and reversal/closure reference. |

## Management Decision Required

The business workflow and control design in this document may be used as the ERP development baseline. The next approval gate is the developer demonstration of the complete BOM-to-Actual-BOM flow described in Section 25.1. Only after that flow passes should later Store pages be approved.
