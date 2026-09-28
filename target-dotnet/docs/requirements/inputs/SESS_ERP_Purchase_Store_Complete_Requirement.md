> Converted from SESS_ERP_Purchase_Store_Complete_Requirement.docx (SHA-256 04B1B1C2AEC71499B79E587390D197A5F721A46286D3600866A68633FB943CD2) to Markdown on 27 September 2026 for traceability. Wording unchanged; layout simplified (headings, lists, tables).

**SRI EASWARI SCIENTIFIC SOLUTION**

SESS Proprietorship & SESS Pvt. Ltd.

**ERP REQUIREMENT SPECIFICATION**

**Employee Login, Master Documents, Purchase and Store**

From Internal Requirement to Material Outward / Return / Consumption

| **Core rule:** Every employee shall use an individual employee-ID login. Every transaction shall be created once, approved through the configured workflow, passed automatically to the next department, and retained with full audit history. No shared login and no permanent deletion. |
|---|

| **Document Control** | **Detail** |
|---|---|
| Document owner | Technical Director / Management |
| Primary process owners | Purchase Manager and Store Manager |
| Applicable entities | Sri Easwari Scientific Solution (Proprietorship) and Sri Easwari Scientific Solution Pvt. Ltd. |
| Version / date | Version 1.0 \| 01 September 2026 |
| Purpose | Development, correction, testing and final acceptance of Purchase and Store modules |
| Classification | Internal controlled document |

## 1. Management Decision and Scope

| **Required decision:** This ERP module is NOT APPROVED until the complete process works from employee login and approved masters through Purchase, Store, material issue/outward, return/consumption, Accounts handoff, reports and audit evidence. |
|---|

The first implementation priority is the integrated Purchase and Store process. The system must support manufacturing projects, service work, capital assets, tools, customer-returnable materials, demo items, repairs, subcontract/job work and routine operating purchases. It must not be limited to a simple Purchase Order screen or stock quantity page.

### 1.1 Included business boundary

- Individual employee login, role-based portal and mobile-responsive operation.
- Company, branch, employee, role, vendor, customer, item, project, BOM, tax, warehouse and document masters.
- Controlled company documents and master attachments with issue/expiry alerts.
- Purchase Requisition (PR) through RFQ, quotation, comparison, negotiation, approval, PO and follow-up.
- Gate entry, GRN, QC/physical verification, acceptance/rejection/hold and put-away.
- Stock reservation, project issue, department issue, service issue, return, consumption and surplus reconciliation.
- Tools issued to employees; same-day or multi-day return; resignation/clearance recovery.
- Customer material, other-brand machine/motor, demo material and repair items received and returned before due date.
- Returnable and non-returnable material outward, gate pass, stock transfer, dispatch, scrap and vendor return.
- Purchase-to-Accounts and Store-to-Production/Service/Dispatch handoffs, dashboards, notifications and audit trail.

### 1.2 Out of this phase but integration-ready

Sales, Project, Design, Production, QC, Dispatch, Finance and Service may be completed in later phases, but Purchase and Store must expose clean interfaces and use shared masters so no data migration or duplicate entry is required later.

### 1.3 End-to-end process boundary

Employee Login → Requirement / Project / Service Trigger → Stock Check → PR → Approval → RFQ → Vendor Quotes → Comparison & Negotiation → PO Approval → PO Release → Material Follow-up → Gate Entry → GRN → QC / Physical Verification → Accepted / Rejected / Hold → Put-away → Reservation → Material Issue / Outward → Return / Consumption / Dispatch → Stock Ledger → Accounts & Cost Analysis → Vendor Performance.

## 2. Mandatory ERP Architecture Rules

- One ERP URL with role-based dashboards. The employee is redirected to the correct default portal after login.
- The employee code is the login ID. Role code is only a permission group; it must never be used as a shared login.
- Central PostgreSQL database with backend permission enforcement. Hiding buttons or menus alone is not security.
- One record is created once and reused. Vendor, item, project, PR, PO, GRN and issue details must not be typed again in the next module.
- Two legal entities must have separate numbering, books, tax registrations, stock ownership, bank data and approval configuration while permitted common masters may be shared.
- No permanent deletion of business records. Use Draft, Cancelled, Inactive, Reversed or Superseded with mandatory reason, user and timestamp.
- All statutory rates, approval thresholds, numbering patterns and policy limits must be configurable, effective-dated settings.
- All timestamps must retain user, device/session and server time. Critical actions must record old value and new value.
- System must retain at least 10 years of transactions, documents and history with backup/restore capability.

## 3. Employee Login, Roles and Permission Control

### 3.1 Employee login requirements

| **Requirement** | **System rule** |
|---|---|
| Unique login | Use active Employee ID as login_id. Shared departmental logins are prohibited. |
| Authentication | Password policy, first-login reset, failed-attempt lock, session timeout and optional MFA for approvers/admin. |
| Employment status | Join date activates access; resignation/termination blocks access automatically after approved relieving rule. |
| Company/branch scope | User sees only authorized entity, branch, warehouse, project and cost centre records. |
| Role assignment | One employee may have multiple permission roles but one accountable process owner must be identified. |
| Delegation | Approver delegation requires from/to date, scope, reason and audit. Delegator remains visible. |
| Device/session | Show active sessions; allow secure logout; log suspicious or unauthorized attempts. |
| Data export | Export/print rights controlled separately from view rights. |

### 3.2 Permission architecture

Implement permissions in this order: Page Master → Role Master → Role-Page Permission → User Mapping → Dynamic Sidebar → Button Control → URL Security → API/Backend Security → Dashboard Control → Test Evidence.

### 3.3 Default role matrix

| **Role** | **Create/Edit** | **Verify/Approve** | **View scope** | **Restrictions** |
|---|---|---|---|---|
| TD / MD | Authorized masters and exceptions | Final approval as configured | All authorized entities | Critical changes still require audit reason |
| Purchase Manager | RFQ, comparison, PO preparation, follow-up | PR/PO level as configured | Purchase and relevant stock/project | Cannot self-create and self-final-approve above threshold |
| Purchase Executive | PR assistance, RFQ, quote entry, follow-up | No final approval | Assigned purchases | No vendor bank/tax approval |
| Store Manager | GRN, location, issue/return control | Store verification | Assigned warehouse | Cannot alter approved PO price |
| Store Keeper | Gate entry, counting, put-away, issue entry | No final QC/financial approval | Assigned store/bin | No negative-stock override |
| QC | Inspection result and NCR | Accept/Reject/Hold | Items assigned for inspection | Cannot change PO/GRN quantity |
| Project/Production | PR and material request | Department verification | Own projects and reservations | No PO price/vendor editing |
| Service Manager/Engineer | Service material request and usage/return | Manager verifies consumption | Assigned jobs/tools | Engineer cannot close without return/usage statement |
| Accounts | Invoice, three-way match, payment status | Finance approval per policy | Commercial and tax data | Cannot change physical receipt |
| Admin/IT | User configuration only as delegated | No business approval by default | Technical logs | No silent transaction edit/delete |
| Auditor/View Only | None | None | Configured read-only records | No download/export unless separately authorized |

### 3.4 Segregation of duties

- PR creator cannot final-approve the same PR when value requires higher approval.
- Quote comparison preparer and final PO approver must be separately visible.
- PO price/terms changes after approval require amendment workflow and reapproval.
- Store receipt confirmation, QC disposition and Accounts invoice approval are separate acts.
- Vendor master bank/GST changes require maker-checker approval and notification to Accounts/Management.

## 4. Company, Branch and Controlled Document Masters

### 4.1 Legal-entity structure

| **Master** | **Mandatory fields / rules** |
|---|---|
| Company Master | Legal name, trade name, entity type, PAN, GSTIN, CIN if applicable, MSME/Udyam, registered/branch addresses, financial year, logo, letterhead, authorized signatories. |
| Branch Master | Company, branch code, full address, GST registration, contact, default warehouse, state/place of supply and active status. |
| Bank Master | Company-specific bank, account type, masked account number, IFSC/SWIFT, branch, currency, approval and effective dates. |
| Number Series | Separate PR/RFQ/PO/GRN/Issue/Gate Pass series by entity, branch and financial year. No reuse after cancellation. |
| Intercompany rule | Movement between Proprietorship and Pvt. Ltd. cannot be treated as an invisible stock transfer. Use approved intercompany sale/purchase or legally configured document flow. |

### 4.2 Master document register

The ERP must keep controlled documents, not only text fields. Each document needs document type, number, issuing authority, issue date, expiry date, entity/branch applicability, current version, attachment, owner, reminder days and history.

| **Document category** | **Examples** | **Controls** |
|---|---|---|
| Legal & tax | PAN, GST certificate, CIN, MOA/AOA, partnership/proprietorship evidence | Entity-specific; version and change history |
| Registrations | MSME/Udyam, Startup India, IEC, GeM, D-U-N-S where available | Expiry/reminder and usage permission |
| Quality | ISO 9001, calibration accreditation/supporting certificates | Validity, scope, issuing body, renewal alert |
| Banking | Cancelled cheque, bank confirmation, EEFC/export account documents | Restricted view/download; maker-checker replacement |
| Insurance & statutory | Factory/employee/asset/transit policies and statutory licences | Expiry escalation to owner and management |
| Vendor/customer documents | GST, PAN, MSME, bank proof, NDA, agreement, rate contract | Party-linked, verified status and expiry |
| Item/technical | Datasheet, drawing, specification, CoC, MSDS, warranty | Revision control and item/project linkage |

### 4.3 Document actions

- Upload with visible filename, type, size, uploader and upload result.
- View, download and replace as separate permissions.
- Replace creates a new version; old version remains available to authorized roles.
- Expiry reminders at configurable intervals such as 90/60/30/7 days and overdue escalation.
- No attachment overwrite without version history; malware/type/size validation required.

## 5. Common Masters Required Before Purchase and Store

| **Master** | **Key fields and validations** | **Owner / approval** |
|---|---|---|
| Employee & Department | Employee ID, company, branch, department, manager, role, status, join/relieve dates | HR/Admin; access approved by authorized management |
| Vendor Master | Legal name, addresses, GST/PAN/CIN/MSME, contacts, bank, payment terms, categories, rating, blacklisting, documents | Purchase creates; Accounts/tax verifies; management approves critical changes |
| Customer Master | Legal identity, locations, GST, contacts, SESS vendor code, portal/linkage | Sales/Accounts; Store view where customer material is held |
| Item Master | Unique code, description, category, make/model, UOM, HSN/SAC, GST, drawing/spec revision, serial/batch control, min/max/reorder, shelf life | Store/Purchase proposes; technical owner verifies |
| Item Category | Raw material, bought-out, fabrication, consumable, spare, tool, asset, service, subcontract, customer-owned, demo, scrap | Admin/authorized master owner |
| UOM & Conversion | Base/purchase/issue UOM and approved conversion factors with rounding | Store/Accounts verification |
| Warehouse/Location | Entity, branch, store, zone, rack, shelf, bin, quarantine, rejection, scrap | Store Manager |
| Project Master | Project number, customer PO/OA, entity, budget, dates, owner, status, machine/serial linkage | Production Manager; TD/MD approval |
| BOM | Project, item/revision, planned qty, scrap allowance, substitutes, issue basis | Design/Production; technical approval |
| Service Job/Machine | Customer, machine ledger, brand, model, serial, complaint/job number, assigned engineer | Service Manager |
| Tax & Charge | GST, TDS applicability, freight, packing, insurance, customs and other charges; effective dates | Accounts |
| Payment/Delivery Terms | Standard templates, credit days, milestone, warranty, LD, inspection, shipping | Purchase/Accounts/Management |
| Approval Matrix | Document, entity, department, amount/condition, level, role/user, SLA, escalation | TD/MD authorized administration |
| Reason Codes | Urgent, breakdown, replacement, excess, short, damaged, rejected, return, scrap, cancellation | Process owner |

### 5.1 Duplicate prevention

- Vendor duplicates checked by GSTIN/PAN/legal name/bank account/mobile/email.
- Item duplicates checked by normalized description, make, model, manufacturer part number and drawing number.
- Project duplicates checked by customer PO line/OA and entity.
- Employee duplicates checked by employee ID and approved identity attributes.
- GRN duplicates checked by vendor invoice/challan, PO and branch.

### 5.2 Master change control

Critical master fields such as vendor bank, GSTIN, item tax/UOM, project entity, approval matrix and warehouse ownership must use maker-checker approval. Transactions shall retain the master values effective on the transaction date; later master changes must not rewrite historical documents.

## 6. Purchase Module - Complete Functional Requirement

### 6.1 Purchase triggers

| **Trigger** | **Source / rule** |
|---|---|
| Project BOM shortage | Automatic shortage after current, reserved and open-PO/in-transit stock calculation |
| Non-BOM project need | Production/Design raises justified requirement linked to project and cost code |
| Store reorder | Min/max/reorder or safety-stock alert |
| Service requirement | Complaint/job card/machine-linked spare, tool or subcontract request |
| Breakdown/emergency | Urgent workflow with reason; regularization and higher visibility required |
| Capital asset/tool | Capex budget and asset-category approval |
| Routine/consumable | Department and cost-centre requirement |
| Subcontract/job work | Material-to-vendor issue, process specification and expected return tracking |
| Import purchase | Currency, Incoterms, freight/customs, lead time and document requirements |

### 6.2 Purchase Requisition (PR)

| **Area** | **Mandatory requirements** |
|---|---|
| Header | PR number/date, entity, branch, department, requester, required-by date, priority, project/service/cost centre, delivery location. |
| Line | Item code/revision, full description/spec, make/model, UOM, quantity, estimated rate, suggested vendors, purpose and attachment. |
| System checks | Current stock, reserved stock, available stock, in-transit/open PO, pending PR, reorder level, BOM balance and budget. |
| User decision | Use stock / reserve stock / buy balance / request substitute. Prevent duplicate open demand. |
| Documents | Drawing/specification/BOQ/customer requirement/previous PO or emergency evidence. |
| Approval | Department verification and amount/condition approval. Rejection/revision remarks mandatory. |

PR statuses: Draft → Submitted → Department Verified → Approval Pending → Approved → Partially Sourced → Fully Sourced → Closed. Exception statuses: Clarification Required, Revision Requested, Rejected, Cancelled, On Hold.

### 6.3 Stock and demand logic before purchase

| **Calculation:** Net Purchase Requirement = Required Quantity + Approved Safety Quantity - Usable Available Stock - Allocatable In-Transit Quantity. Reserved stock for other projects cannot be treated as available without authorized de-reservation. |
|---|

- The requester must see stock by entity, branch, warehouse and condition: unrestricted, reserved, quarantine, rejected and customer-owned.
- Open PR and PO quantities must be visible to prevent repeated purchasing.
- Approved substitute items may be proposed, but technical approval is mandatory before replacement.

### 6.4 RFQ

| **Requirement** | **Rule** |
|---|---|
| RFQ creation | Create from approved PR lines; combine compatible PRs without losing source linkage. |
| Vendor selection | Approved vendor/category list with rating, tax/location and conflict/blacklist warning. |
| RFQ content | Specification, quantity, delivery location/date, commercial template, quote due date, warranty/inspection and attachments. |
| Communication | Generate PDF/email-ready RFQ; record sent date, recipients, acknowledgement and reminders. |
| Revision | RFQ revisions retain old version and notify all selected vendors. |
| Response tracking | Quoted / Declined / No response / Extension requested with timestamps and remarks. |

### 6.5 Vendor quotation capture

- Capture vendor quote number/date/validity, currency, basic rate, discount, GST, freight, packing, insurance, payment, delivery, warranty, make/model and deviations.
- Attach original quotation. Manual entered values must show entered-by and verified-by.
- Support line-level alternative, optional item, minimum order quantity and partial quote.
- Detect expired quote and arithmetic mismatch. Do not allow silent alteration of vendor offer.

### 6.6 Technical and commercial comparison

| **Comparison block** | **Required output** |
|---|---|
| Technical compliance | Comply / Deviate / Not offered with engineer remarks and attachment reference |
| Landed cost | Basic - discount + tax/non-creditable tax + freight + packing + insurance + duty + other charge |
| Delivery | Promised date/lead time compared with required-by date |
| Commercial terms | Payment, credit, warranty, validity, LD, inspection and support |
| Vendor history | Last purchase rate/date, recent offers, rejection/late delivery/quality score |
| Recommendation | Lowest compliant offer or justified alternate. Selected line highlighted with reason. |

The comparison must preserve every quote and negotiation revision. It must support item-wise split award to different vendors and show total impact before approval.

### 6.7 Negotiation and approval

| **Total approval value** | **Default authority** | **Control** |
|---|---|---|
| ₹0 to ₹50,000 | Manager | Configurable by entity/document/category |
| ₹50,001 to ₹5,00,000 | Technical Director | Department/technical verification remains required |
| Above ₹5,00,000 | Managing Director | Full comparison and deviation reasons required |

Thresholds are initial SESS policy values and must remain configurable. Approval amount shall be based on the configured gross/landed/commitment value, consistently displayed in request, approval, print and report. Splitting purchases to avoid approval levels must be flagged.

### 6.8 Purchase Order (PO)

| **PO block** | **Mandatory content / behavior** |
|---|---|
| Header | Entity/branch, PO series/date, vendor/bill-to/ship-to, reference PR/RFQ/quote/comparison, currency and contact. |
| Lines | Item/service, revision/spec, make/model, HSN/SAC, UOM, qty, rate, discount, tax, delivery schedule, project/cost allocation. |
| Terms | Payment, freight, packing, insurance, warranty, inspection, LD, confidentiality, rejection/replacement and statutory terms. |
| Approvals | Prepared/verified/approved names, dates, digital approval history; release only after required approval. |
| Output | Controlled PDF with version, entity letterhead and authorized signatory; email/send status logged. |
| Balance | Ordered, received, accepted, rejected, returned, invoiced, cancelled and open quantities. |

### 6.9 PO amendment, cancellation and closure

- Any change to rate, quantity, tax, vendor, delivery or payment terms creates an amendment version and re-enters approval as configured.
- Received/invoiced quantities cannot be cancelled or reduced without controlled reversal and linked documents.
- Cancellation requires reason, impact on project/stock/budget and vendor communication.
- PO closes only when receipt/return/invoice balance is reconciled or authorized short closure is recorded.

### 6.10 Material follow-up and expediting

| **Control** | **Requirement** |
|---|---|
| Schedule | Line-wise committed and revised delivery dates with quantity split |
| Follow-up log | Date/time, vendor contact, promised action, next follow-up, employee and remarks |
| Alerts | Upcoming due, due today, overdue, project-critical and part-delivery balance |
| Vendor documents | Dispatch advice, invoice/challan, e-way bill, LR/AWB, CoC, test/warranty certificate |
| Escalation | Configurable escalation from buyer to Purchase Manager, project owner, TD/MD |
| Dashboard | Open PO, in-transit, overdue value, days late and project impact |

### 6.11 Purchase exceptions

| **Exception** | **Required control** |
|---|---|
| Emergency purchase | Reason, urgency, approver and post-facto regularization; no bypass of audit |
| Single-source/proprietary | Technical justification and authorized approval |
| Advance payment | Proforma invoice, approval, payment milestone and recovery against receipt/invoice |
| Petty cash/local buy | Defined limit, receipt and cost centre; later GRN/consumption as applicable |
| Service PO | Service scope, milestone, completion certificate and SAC/GST controls |
| Subcontract PO | Material issued, process, expected yield/return, scrap and reconciliation |
| Import PO | Foreign currency, exchange basis, Incoterms, customs and logistics documents |

## 7. Purchase-to-Store and Purchase-to-Accounts Handoffs

### 7.1 Data passed automatically to Store

- Approved PO header, lines, specifications, quantity, delivery schedule, project allocation and inspection requirements.
- Vendor dispatch/in-transit details and expected arrival.
- PO amendment/cancellation/short-close status in real time.
- Documents required at receipt: invoice/challan, e-way bill, CoC, test report, warranty, packing list or import documents.

### 7.2 Data passed automatically to Accounts

- Approved PO commitment, advance terms and budget/cost allocation.
- GRN receipt, QC acceptance/rejection and vendor return quantities.
- Supplier invoice and tax data with PO-GRN-Invoice three-way matching.
- Payment hold for unresolved rejection, quantity/rate/tax mismatch or missing mandatory document.

### 7.3 Three-way match

| **Control:** Invoice payable quantity/value must reconcile with approved PO and accepted GRN, subject to configurable tolerance. Any override requires authorized reason and complete audit history. |
|---|

## 8. Store Module - Complete Functional Requirement

### 8.1 Store operating zones

| **Zone** | **Use / stock status** |
|---|---|
| Receiving | Arrived but GRN/counting not completed; not available for issue |
| Quarantine / QC Hold | Awaiting inspection or on hold; not available for normal issue |
| Accepted Stock | Available or reserved after put-away |
| Rejected Stock | Blocked; pending vendor return/rework/decision |
| Project Staging | Reserved/kitted for a named project |
| Tool Room | Employee/project-issued returnable tools and calibration status |
| Customer-Owned | Customer material/demo/repair items; excluded from SESS owned stock value |
| Service/Repair | Machines, motors, parts or assemblies received for repair |
| Scrap | Approved scrap segregated pending disposal |
| Dispatch | Picked/packed and awaiting outward confirmation |

### 8.2 Inward categories

- Against Purchase Order, subcontract return, stock transfer, customer-returnable material, service/repair machine or motor, demo/sample, employee tool return, project material return, sales return and miscellaneous authorized inward.
- Every inward must identify ownership: SESS Proprietorship, SESS Pvt. Ltd., customer, vendor, employee or other party.
- No inward shall increase usable stock until the correct verification/QC stage is completed.

### 8.3 Gate Entry / Material Inward Register

| **Field group** | **Required fields** |
|---|---|
| Reference | Gate entry no/date/time, entity, branch, gate/location, inward type, PO/transfer/service/customer reference |
| Party/transport | Vendor/customer/from location, transporter, vehicle, driver/contact, LR/AWB/e-way bill |
| Documents | Invoice/challan/packing list numbers and dates; attachment/photo |
| Package | No. of packages, gross/approx. weight, seal/visible condition, received by/security |
| Decision | Accept for unloading / hold at gate / reject entry with reason and approver |

### 8.4 GRN

| **Area** | **Requirement** |
|---|---|
| Source | Create from gate entry and PO/authorized inward; no duplicate data entry |
| Quantities | Ordered, previous received, current challan, physical received, short, excess, damaged and pending |
| Identification | Batch/lot, serial, manufacturer part, manufacture/expiry date and warranty as applicable |
| Condition | Package and material condition; photos for damage/exception |
| Location | Temporary receiving/quarantine location until disposition |
| Mismatch | Quantity/spec/document/PO mismatch triggers hold and notification |
| Status | Draft → Counted → Submitted → QC Pending / Direct Accept → Accepted / Rejected / Hold → Put-away → Closed |

### 8.5 QC and physical verification

| **Control** | **Requirement** |
|---|---|
| Inspection plan | Item/category-specific checks, sample size, parameter, tolerance and required instruments |
| Disposition | Accepted, Rejected, Hold, Conditional Accept, Rework or Return-to-Vendor |
| Evidence | Measured values, inspector, date/time, photos, CoC/test report and calibration reference |
| NCR | Nonconformance number, defect, responsibility, corrective action, due date and closure |
| Partial decision | Permit split quantities across accept/reject/hold with total reconciliation |
| Release | Only accepted quantity becomes issuable inventory |

### 8.6 Put-away and stock ledger

- Suggest warehouse/rack/bin based on item category, hazard, size, shelf-life and available capacity.
- Record physical put-away confirmation, quantity, operator and timestamp. Support barcode/QR scan.
- Stock ledger is immutable and transaction-based: opening + inward - outward ± controlled adjustment = closing.
- Maintain current, available, reserved, in-transit, QC hold, rejected, customer-owned and issued-out quantities separately.
- Use FIFO by default for physical issue and configured weighted-average or approved valuation method for Accounts. Shelf-life items use FEFO where required.
- Negative stock is blocked. Exceptional override requires management permission, reason and later reconciliation.

### 8.7 Stock reservation and project kitting

| **Function** | **Requirement** |
|---|---|
| Reservation | Reserve accepted stock to project/BOM/service job/department; show available balance after reservation |
| Priority | Project priority and required date; authorized transfer between reservations with reason |
| Kitting | Prepare BOM kit, identify ready/short/alternate/pending items and scan during issue |
| Shortage | Auto-generate purchase demand suggestion from confirmed net shortage |
| De-reservation | Only authorized role; record project impact, approver and notification |

## 9. Material Issue, Consumption and Return

### 9.1 Material request and issue

| **Stage** | **Requirement** |
|---|---|
| Request | Employee/department/project/service job requests item, quantity, need date and purpose |
| Validation | Check authorization, BOM/approved non-BOM requirement, reservation, available stock and pending issue |
| Approval | Project/department/service owner approves according to item/value/exception rule |
| Pick | Store creates pick list by bin/batch/serial; scan and count |
| Handover | Receiver employee confirms quantity/condition by login, PIN/OTP/signature as configured |
| Ledger | Issue reduces store stock and creates project/employee/job custody or consumption record |

### 9.2 Manufacturing project material flow

Daily Store issue must be linked to the running project and BOM. The system must separately show planned BOM, issued quantity, returned quantity, net consumed quantity, WIP quantity, scrap, variance and pending requirement.

| **Formula:** Net Project Consumption = Total Issued - Usable Return - Transfer Out + Transfer In - Approved Reversal. Actual BOM variance = Net Project Consumption - Approved BOM Quantity. |
|---|

- Material fixed into the machine is marked consumed only through authorized consumption confirmation, not merely because it left Store.
- Unused usable material returns to Store with condition and original project linkage.
- Damaged/offcut/scrap return is recorded separately and cannot increase usable stock.
- Non-BOM use requires reason and technical/project approval. Excess issue beyond tolerance is blocked or escalated.
- Project closing requires material reconciliation before final cost and dispatch closure.

### 9.3 Return to Store

| **Return type** | **System treatment** |
|---|---|
| Unused usable material | Inspect and return to available stock/bin; reverse project custody/consumption as applicable |
| Part-used quantity | Accept measurable balance using approved UOM/conversion; record loss/consumption |
| Damaged material | Move to hold/rejection/scrap based on decision; do not add usable stock |
| Wrong item/excess issue | Link original issue and reason; revalidate batch/serial and quantity |
| Tool return | Condition/accessories/calibration check; close employee custody only after Store acceptance |
| Service spare return | Link service job, used/not-used/defective/warranty-return status |

### 9.4 Physical stock adjustment

Stock adjustment is not an edit of closing quantity. It is a controlled transaction created from cycle count/stock verification, with book quantity, physical quantity, variance, reason, value impact, approval and audit. Large or repeated variances must escalate to management.

## 10. Tools, Assets and Employee Custody

| **Requirement** | **Control** |
|---|---|
| Tool master | Unique asset/tool ID, make/model/serial, ownership entity, purchase/warranty, location, condition, calibration and accessories |
| Issue duration | Same-day, multi-day, project duration or permanent custody with expected return date |
| Handover | Employee accepts condition/accessories; photo/signature where needed |
| Reminder | Due-soon, due-today and overdue notifications to employee, manager and Store |
| Return | Store checks condition, missing accessories, damage and calibration status before closure |
| Repair/loss | Incident, responsibility, repair/replacement cost and approval; no silent closure |
| Resignation clearance | Open tools/assets automatically appear in employee exit clearance; relieving cannot close until resolved/approved |
| History | Lifetime custody, usage, repair, calibration and location history retained |

## 11. Customer, Demo and Service Materials

### 11.1 Customer-owned material register

- Customer, project/service job, customer challan/PO, item/machine details, make/model/serial, quantity, condition and photos.
- Ownership remains customer-owned and is excluded from SESS inventory valuation.
- Expected return date, responsible employee, storage location, work purpose and customer contact are mandatory.
- Due-date reminders and escalation must begin before the promised return date.
- Return outward requires linked receipt, customer acknowledgement/POD and closure of custody.

### 11.2 Other-brand machine/motor received for service

Create or reuse a Machine Ledger with customer, brand, model, serial number, machine type, installed location and complete old service history. Each inward creates a service job/custody record. Inspection, parts used, repair work, trial, quotation/approval, outward and customer acknowledgement remain linked to the same machine ledger and customer portal record.

### 11.3 Demo/sample/loan material

| **Direction** | **Requirement** |
|---|---|
| Received from customer/vendor | Owner, receipt proof, purpose, condition, responsible employee, due date and return proof |
| Sent to customer/site | Approval, recipient, location, expected return, value, insurance/transport and acknowledgement |
| Extension | Revised due date requires reason and approval; preserve original date |
| Converted to sale/use | Formal commercial/consumption document required; no direct custody closure |

## 12. Material Outward and Gate Pass

### 12.1 Outward classifications

| **Outward type** | **Stock/custody effect** | **Closure evidence** |
|---|---|---|
| Project/production issue | Store stock to project custody/consumption | Receiver acceptance and project reconciliation |
| Service engineer issue | Store stock/tool to engineer/job custody | Usage/return statement and manager verification |
| Return to vendor | Rejected/repair/warranty stock leaves Store | Vendor acknowledgement, replacement/credit note/return completion |
| Subcontract/job work | Material moves to vendor custody; ownership retained | Processed return, yield/scrap reconciliation |
| Customer material return | Customer-owned custody closes after dispatch | Customer POD/acknowledgement |
| Demo/loan outward | Employee/customer/site custody; return due | Return inward or approved conversion |
| Stock transfer - same entity | Source decreases; in-transit; destination receives | Destination GRN/transfer receipt |
| Intercompany movement | Legal transaction between entities | Configured sale/purchase/tax documents and receipt |
| Sales/project dispatch | Finished goods/approved items leave dispatch | Invoice/challan/e-way bill/POD and project link |
| Scrap disposal | Approved scrap stock reduces | Bid/approval/weighment/invoice/receipt as applicable |
| Non-returnable consumption | Stock reduces to department/job/cost centre | Authorized receipt/consumption confirmation |

### 12.2 Gate pass mandatory fields

- Gate pass number/date/time, entity/branch, outward type and returnable/non-returnable flag.
- Reference document: issue, vendor return, subcontract, service job, dispatch, transfer, demo or customer return.
- Consignee/party/site, full delivery address, contact, transporter/vehicle/driver and e-way bill/LR where applicable.
- Item, description, quantity/UOM, batch/serial/tool ID, package count, approximate value and condition.
- Expected return date for returnable outward, responsible employee, escalation owner and extension history.
- Prepared, verified, approved, security exit confirmation and recipient acknowledgement.
- Attachments: challan, invoice, e-way bill, packing list, photos and approval evidence.

### 12.3 Returnable outward control

Returnable outward remains OPEN until full inward is received and verified. It must show original quantity, returned quantity, damaged/lost quantity, balance, original due date, current due date, overdue days and responsible owner. Partial returns are allowed with full balance history.

### 12.4 Security gate verification

- Security sees only approved gate passes for the correct gate/branch and current validity period.
- Scan QR/barcode or enter gate pass; verify package/item/vehicle and mark exit time.
- Expired, cancelled, already-used or unapproved gate pass is blocked and logged.
- Material cannot be edited at gate. Variance returns to Store for correction and reapproval.

## 13. Vendor Return, Rework and Warranty Return

| **Stage** | **Requirement** |
|---|---|
| Decision | Link rejection/NCR/defect/warranty; select return, replacement, rework or credit note |
| Approval | Quantity/value/reason and transport responsibility approved |
| Outward | Return challan/gate pass, batch/serial, documents and carrier details |
| Tracking | Vendor acknowledgement, replacement due date, credit-note due and follow-up |
| Receipt/settlement | Replacement GRN or credit note closes linked balance; original history retained |

## 14. Inventory Valuation, Project Cost and Reconciliation

### 14.1 Stock value

- Maintain quantity and value by entity, warehouse, item, batch/serial and stock condition.
- Valuation method is a company setting (recommended weighted average for financial value, with FIFO/FEFO for physical issue as applicable).
- Customer-owned, demo-received and vendor-owned material is quantity-tracked but excluded from SESS owned stock valuation.
- Rejected/hold/scrap values are separately visible and handled per Accounts policy.

### 14.2 Project actual material cost

| **Measure** | **Calculation / source** |
|---|---|
| Planned material cost | Approved BOM quantity × approved standard/estimated rate |
| Issued value | All project issues at configured valuation |
| Returned value | Usable returns credited using same controlled valuation basis |
| Net consumed value | Issued value - usable return value ± authorized transfers/reversals |
| Purchase commitment | Approved open PO value allocated to the project |
| Variance | Actual net consumption/cost compared with approved BOM/budget |

### 14.3 Project/PO/stock closure gates

- Project material reconciliation completed: issued, returned, consumed, scrap, WIP and missing balances.
- PO received/rejected/returned/invoiced/cancelled/open quantities reconcile.
- GRN accepted/rejected/hold/returned/put-away quantities reconcile.
- Employee and vendor custody balances are either returned or formally resolved.
- Customer-owned items are returned or authorized to remain with revised due date.

## 15. Status, Numbering and Workflow Rules

| **Record** | **Illustrative numbering** | **Main statuses** |
|---|---|---|
| PR | PVT/PR/26-27/0001 | Draft, Submitted, Verified, Approval Pending, Approved, Rejected, Revision, Closed, Cancelled |
| RFQ | PVT/RFQ/26-27/0001 | Draft, Sent, Responses Pending, Compared, Closed, Cancelled |
| PO | PVT/PO/26-27/0001 | Draft, Approval Pending, Approved, Released, Part Received, Completed, Short Closed, Cancelled |
| Gate Entry | PVT/GE/26-27/0001 | Entered, Held, Unloaded, Rejected, GRN Created |
| GRN | PVT/GRN/26-27/0001 | Draft, Counted, QC Pending, Accepted, Rejected, Hold, Put-away, Closed |
| Issue | PVT/MI/26-27/0001 | Requested, Approved, Picked, Issued, Part Returned, Reconciled, Closed |
| Gate Pass | PVT/GP/26-27/0001 | Draft, Approved, Exit Confirmed, Part Returned, Returned, Overdue, Closed, Cancelled |

Series examples are configurable. Separate entity/branch/financial-year series are mandatory. Numbers are generated by the server only; users cannot type, overwrite or reuse them.

### 15.1 Approval behavior

- Support single-level and multi-level approval based on amount, category, project, department, urgency, exception and entity.
- Reject and Revision Request require remarks. Resubmission starts a new approval-cycle version while old actions remain visible.
- Approver sees document, attachments, comparisons, historical price, budget/stock impact and prior remarks before deciding.
- Pending approvals appear in dashboard and notifications with age/SLA. Escalation does not silently auto-approve.

## 16. Notifications, Pending Tasks and Escalation

| **Event** | **Recipients / behavior** |
|---|---|
| PR submitted/revised/rejected | Requester, department verifier and configured approvers |
| RFQ due / no response | Buyer and Purchase Manager |
| PO approval/release/amendment | Purchase owner, requester/project owner and Store as applicable |
| Delivery due/overdue | Buyer, Purchase Manager, project owner; escalate critical delay |
| Material arrival / QC pending | Store, QC and requester/project owner |
| Rejection/hold/NCR | Purchase, vendor owner, QC, project owner and Accounts hold |
| Low stock/shortage | Store, Purchase and authorized project/department owners |
| Tool/material return due | Custodian employee, manager and Store; escalation after due date |
| Customer/vendor returnable due | Responsible employee, process manager and management based on overdue age |
| Stock variance | Store Manager, Accounts and management based on value/percentage threshold |

Each notification must deep-link to the authorized record. Read/unread and action status are stored. Email/WhatsApp/SMS integration may be configured later, but in-app notification is mandatory.

## 17. Dashboards and Reports

### 17.1 Purchase dashboards/reports

- PR pending by approver, age, department, project, priority and value.
- RFQ response status, comparison pending and negotiation savings.
- PO summary by entity/vendor/project/category; open/partial/complete/cancelled.
- Delivery due/overdue, in-transit, lead-time variance and project impact.
- Purchase price variance, last purchase price, landed cost and budget vs commitment.
- Vendor on-time delivery, quality acceptance, response, price, service and overall score.
- Advance outstanding, missing invoice/document and three-way-match exception.

### 17.2 Store dashboards/reports

- Current, available, reserved, in-transit, QC hold, rejected and customer-owned stock.
- Stock ledger and transaction drill-down by entity/warehouse/item/batch/serial/project.
- Low stock, reorder, zero stock, non-moving, slow-moving, aging and shelf-expiry.
- GRN pending counting/QC/put-away and vendor-return pending.
- Project reserved/issued/returned/consumed/scrap/variance and BOM shortage.
- Tools/assets by employee, project, location, due date, overdue, condition and calibration.
- Returnable outward pending by customer/vendor/employee/site and overdue days.
- Customer-owned/demo/service items in custody and due for return.
- Physical stock variance and adjustment approvals.

### 17.3 Report controls

- Date range, entity, branch, warehouse, department, project, vendor, item/category, status and employee filters.
- Drill-down from totals to source record with permission enforcement.
- Excel/PDF/print export only for authorized roles; export event logged.
- Displayed, exported and printed totals must match saved database values.

## 18. Mobile and Desktop User Experience

### 18.1 Mobile priority functions

| **User** | **Mobile functions** |
|---|---|
| Employee/requester | Create PR/material request, attach photo, track status, accept issued item/tool, submit return/usage |
| Approver | Pending approvals, document/quote/comparison view, approve/reject/revise with remarks |
| Store | Gate entry, barcode scan, GRN count, put-away, pick, issue, return and gate pass verification |
| QC | Inspection checklist, measured values, photo, accept/reject/hold and NCR |
| Purchase | Follow-up log, vendor contact, promised date update, quote/PO status |
| Engineer | Service material/tool receipt, site usage, unused return and damage report |
| Security | Scan approved gate pass, verify and confirm entry/exit only |

### 18.2 Usability standards

- One page, one process; role-appropriate sidebar and clear page title/purpose.
- Clear Save Draft, Submit, Verify, Approve, Reject, Request Revision, Cancel and Close actions.
- Confirmation before material actions; immediate success/error message with generated number.
- Attachment filename, upload status and View/Download/Replace actions visible.
- Search, filters, pagination, recent records and quick view for large registers.
- No clipped menus, overlapping controls, hidden required action or desktop-only critical flow.
- Scanning must support camera/manual fallback and prevent duplicate scan quantity.

## 19. Audit, Security and Data Protection

### 19.1 Required audit fields

| **Audit area** | **Minimum evidence** |
|---|---|
| Record creation | Created by employee ID, date/time, entity, branch, source and initial values |
| Approval | Level, approver, decision, remarks, date/time, version and delegation if any |
| Change history | Field-level old/new value, reason, user and date/time for controlled changes |
| Status history | Every status transition, actor, time and triggering action |
| Attachments | Uploader, filename, hash/version, replacement history, view/download where required |
| Inventory | Every movement quantity/value, source document, from/to location and custodian |
| Security | Login, failed login, permission denial, direct-URL/API denial, export and privileged admin action |

### 19.2 Backend security

- Authorize every API request against user, role, entity, branch, warehouse, record scope and action.
- Parameterized database access and validation against tampering, duplicate submission and over-issue/over-receipt.
- Sensitive vendor bank/company bank data encrypted/masked and restricted.
- Files stored securely with controlled download and no executable upload.
- Database backup, point-in-time recovery where available, restore test and off-site copy policy.
- Production secrets/configuration kept outside source code; test and production data separated.

## 20. Core Database and API Expectations

The exact names may differ, but the design must include normalized, linked records equivalent to the following. Transaction lines must reference immutable header/version/master identifiers, not copied free text only.

| **Domain** | **Expected core records** |
|---|---|
| Security | employees, users, roles, permissions, user_roles, approval_delegations, sessions, security_audit |
| Masters | companies, branches, departments, warehouses, bins, items, item_revisions, uoms, tax_rates, parties, party_sites, documents |
| Project/Service | projects, boms, bom_lines, machines, service_jobs, cost_centres, budgets |
| Purchase | prs/lines, rfqs/vendors/lines, quotations/lines, comparisons, negotiations, approvals, purchase_orders/lines/amendments, followups |
| Store inward | gate_entries, grns/lines, inspections, ncrs, putaways, batches, serials |
| Inventory | stock_transactions, stock_balances, reservations, material_requests, issues/lines, returns/lines, adjustments |
| Custody/outward | tools_assets, custody_transactions, gate_passes/lines, stock_transfers, vendor_returns, customer_owned_items, dispatches |
| Audit/alerts | status_history, change_history, attachments, notifications, escalations, exports, integration_log |

### 20.1 API rules

- Server-side validation is authoritative; frontend validation is supplementary.
- Create/submit/approve/issue endpoints must be idempotent to prevent double-click duplicate transactions.
- Inventory posting occurs in one database transaction with rollback on failure.
- Concurrency control prevents two users issuing the same available stock.
- Every API returns clear success/failure, record number and safe user message; technical error is logged.
- Integration failures use retry/status and do not create partial untraceable records.

## 21. Acceptance Test Matrix

| **No.** | **Test scenario** | **Required evidence** |
|---|---|---|
| 1 | Unique employee login and correct role dashboard | Screens + session/audit record |
| 2 | Unauthorized menu, direct URL and API blocked | 403/denial evidence and security log |
| 3 | Create PR from project shortage and prevent duplicate | PR, stock calculation and duplicate validation |
| 4 | PR approve, reject, revise and resubmit | Complete approval/status history |
| 5 | RFQ to multiple vendors and quotation versions | Sent log, attachments and response status |
| 6 | Technical/commercial comparison and landed-cost recalculation | Independent calculation matching screen/export |
| 7 | Amount-based PO approval for all three levels | Manager/TD/MD routing evidence |
| 8 | PO amendment after approval | Old/new versions and reapproval |
| 9 | Partial delivery, excess, short and damaged receipt | Gate entry/GRN quantities reconcile |
| 10 | QC partial accept/reject/hold | Inspection/NCR and stock availability |
| 11 | Put-away with batch/serial and barcode | Bin stock and scan evidence |
| 12 | Project reservation and issue with concurrent users | No over-issue; ledger balances |
| 13 | Usable, damaged and scrap project returns | Separate stock/status/value treatment |
| 14 | Actual BOM consumption and variance | Project reconciliation report |
| 15 | Tool issue, overdue reminder and return | Employee custody and condition history |
| 16 | Employee resignation with tool pending | Clearance block/escalation |
| 17 | Customer-owned machine/demo inward and due return | Machine/custody ledger and reminder |
| 18 | Returnable outward partial return and overdue | Open balance, due-date and escalation |
| 19 | Vendor return with replacement/credit note | Linked closure chain |
| 20 | Same-entity transfer and destination receipt | In-transit and destination confirmation |
| 21 | Intercompany movement is not silent stock transfer | Legal configured transaction flow |
| 22 | Three-way match and mismatch hold | PO-GRN-invoice reconciliation |
| 23 | Stock count and approved adjustment | Book/physical variance and immutable ledger |
| 24 | Save-refresh-logout-login persistence | Database record remains correct |
| 25 | Attachment upload/view/download/replace failure | Version/history and user feedback |
| 26 | Mobile workflow for request, approval, scan, issue and gate | Screens on phone widths |
| 27 | Report/export reconciliation | Screen, database, Excel/PDF totals match |
| 28 | Realistic volume/performance | Response-time test with production-sized records |
| 29 | Backup and restore | Documented successful restore test |
| 30 | No permanent delete | Cancellation/reversal history and permission test |

### 21.1 Final acceptance evidence

- Working screens and reproducible end-to-end demonstration with persistent database data.
- Role/permission matrix and unauthorized direct-URL/API test report.
- Database schema, migrations, seed/master data and ER diagram.
- Full unprotected source code, build/run instructions and environment/configuration list.
- Backup/restore procedure and successful restore evidence.
- Open issue list with severity, owner and committed closure date.
- Deployment, admin manual, user manual, support/escalation path, SLA and warranty/support period.

## 22. Implementation Priority and Release Gates

| **Phase** | **Scope** | **Release gate** |
|---|---|---|
| P0 - Foundation | Employee login, company/branch, roles/permissions, numbering, approval settings, audit and documents | Security and master approval tests pass |
| P1 - Purchase Core | PR, stock check, RFQ, quote, comparison, approval, PO, amendment and follow-up | Approved PO handed to Store/Accounts without re-entry |
| P2 - Store Inward | Gate entry, GRN, QC, put-away, stock ledger and vendor return | Accepted stock and rejected/hold separation proven |
| P3 - Store Outward | Reservation, material request, issue, return, project consumption, gate pass and transfers | Every movement reconciles to ledger/custody |
| P4 - Special Custody | Tools, service, customer-owned, demo, subcontract and return-due control | Due/overdue and closure evidence proven |
| P5 - Finance/Reports | Three-way match, valuation, project cost, vendor performance, dashboards and exports | Calculations and exports reconcile |
| P6 - Handover | Performance, backup/restore, documentation, training, source and support | Final acceptance checklist signed |

## 23. Copy-Ready Instruction to ERP Developer / Codex

| **Implementation instruction:** Implement the Purchase and Store modules as one integrated, database-persistent ERP process for both SESS entities. Begin with unique employee-ID login and backend-enforced role/record permissions. Complete all masters and controlled documents first. Then implement PR → RFQ → Quote → Comparison → Approval → PO → Follow-up → Gate Entry → GRN → QC → Put-away → Reservation → Issue/Outward → Return/Consumption/Dispatch → Stock Ledger → Accounts/Cost/Vendor Performance. Reuse each approved record without duplicate entry. Add configurable approval thresholds, complete status/approval/change history, no permanent deletion, mobile-responsive screens, pending tasks, alerts and reconciliation reports. Cover project BOM material, tools, service engineer custody, customer-owned/demo/repair items, vendor returns, subcontract material, returnable gate pass, same-entity transfer and controlled intercompany movement. Do not mark any page complete until the acceptance tests in this document pass with persistent database and audit evidence. |
|---|

## 24. Final Approval Position

| **Decision** | **Condition** |
|---|---|
| Current requirement status | APPROVED FOR DEVELOPMENT / CORRECTION |
| Module acceptance status | NOT APPROVED until the complete end-to-end test and handover evidence is submitted |
| Primary next action | Developer shall map existing ERP screens/APIs/tables against this document and submit a Completed / Missing / Correction Required matrix |
| Final authority | SESS TD/MD after Purchase Manager, Store Manager, QC, Accounts and user acceptance testing |

## Appendix A - Minimum Transaction Field Audit

| **Every controlled record must show** | **Required** |
|---|---|
| Identity | Entity, branch, unique number, version and date |
| Ownership | Owner, creator, verifier, approver and viewers |
| Business link | Project/service/customer/vendor/employee/cost centre as applicable |
| Commercial/quantity | UOM, qty, rate/value/tax/charges and balances as applicable |
| Documents | Attachments, filenames, types, versions and status |
| Workflow | Current status, next action, pending with and SLA age |
| History | Status, approval, change and attachment history |
| Closure | Closure reason/evidence, balance reconciliation and timestamp |

## Appendix B - Daily Management Review

- PR and PO approvals pending beyond SLA.
- Project-critical shortages and overdue vendor deliveries.
- GRN waiting for QC/put-away and rejected stock awaiting vendor action.
- Material issued but not acknowledged, consumed or returned.
- Tools, customer materials, demos and returnable gate passes due/overdue.
- Stock variances, negative-stock attempts and unauthorized access events.
- Purchase commitments, advances, invoice mismatches and vendor performance exceptions.
