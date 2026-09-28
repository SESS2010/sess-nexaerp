> Converted from SESS_ERP_Advanced_Mobile_Service_Module_Guideline.docx (SHA-256 C43DE04B274A2F6FE585EB48FA0C7FFDE8DEC3480E651CE9B5F8880EDC73AF07) to Markdown on 27 September 2026 for traceability. Wording unchanged; layout simplified (headings, lists, tables).

**SERVICE MODULE GUIDELINE**

# SESS ERP Advanced Mobile Service Module

Business rules, machine ledger, mobile engineer workflow, controls and acceptance criteria

| **VERDICT:** APPROVED WITH CORRECTIONS — Mobile application is the primary working interface for service engineers. Web/desktop remains the primary control interface for managers, coordinators, Accounts and administrators. |
|---|

| **Document control** | **Value** |
|---|---|
| Scope | Service complaints, machine ledger, visits, job cards, contracts, spares, customer property, billing handoff and service history |
| Primary users | Service Engineers, Service Manager, Service Coordinator, Technical Manager, Accounts and Customers |
| Retention | Core service, asset, approval and audit records: minimum 10 years |
| Status | Implementation guideline — decisions in this document are to be treated as frozen unless changed through approved governance |

## 1. Purpose and Non-Negotiable Design Principles

This guideline defines an advanced, mobile-first Service Module for SESS ERP. It connects every customer complaint to one customer, one machine identity, every visit, engineer activity, spare movement, customer acknowledgement, billing decision and the complete lifetime service history.

- One Service Ticket is the parent record. Visits, job cards, existing ERP Tasks, expenses, feedback and documents must link to that ticket.
- One Customer Machine Ledger is the permanent machine identity used by internal ERP and the Customer Portal.
- Engineers work mainly from mobile, including offline operation. Managers and Accounts control approvals, exception handling and closure.
- Posted or approved history is never overwritten. Corrections use revisions, amendments or reversals with reason and audit evidence.
- Every role, approval and notification is effective-dated and configurable; employee names must not be hard-coded.

| **FROZEN DECISION — OTHER-BRAND MACHINES:** When a customer requests service for a machine that is not already registered—including any other-brand chamber—the ERP must create or identify a Customer Machine Ledger linked to the customer and service site. The same ledger must appear in the Customer Portal and retain all old and future service records for the full life of the machine. |
|---|

## 2. End-to-End Service Workflow

- Receive complaint from phone, email, WhatsApp, Customer Portal, AMC schedule or an internal alert.
- Create or locate the customer and Customer Machine Ledger; run duplicate checks before creating a new machine.
- Create the Service Ticket and record issue, priority, channel, requested date and attachments.
- Service Manager reviews the complaint, entitlement, safety/risk, expected work and assignment.
- Engineer receives a mobile notification and accepts, requests clarification or reports inability to attend.
- Engineer completes travel, gate-pass/PPE checks and live-photo/GPS punch-in at site.
- Engineer scans/selects the machine, diagnoses the problem, records readings and uploads before/during/after evidence.
- Spare requests, approvals, engineer custody, consumption, returns and removed-part disposition are recorded.
- Customer signs or confirms by OTP; the system generates and sends the service report.
- Engineer punches out, completes the Evening Report and submits expense evidence.
- Service Manager verifies technical completion; Accounts completes any billing handoff; authorized manager closes the ticket.

## 3. Complaint, Visit and Job Card Records

| **Record** | **Purpose** | **Control** |
|---|---|---|
| Service Ticket | Parent complaint/job and single source of truth | Never duplicate the same open complaint for the same customer, machine and issue without warning and manager confirmation |
| Service Visit | One planned or actual engineer attendance | A ticket may have multiple visits; every visit retains planned and actual times |
| ERP Task | Engineer work activity linked by service_ticket_id | Reuse the existing Task module; do not build a second task tracker |
| Job Card / Service Report | Visit output, diagnosis, work, readings, spares and sign-off | Generated from approved visit data; later changes are controlled amendments |

## 4. Customer Machine Ledger

A new machine ledger must be created when a service request is received and the machine does not already exist. It applies equally to SESS machines and other-brand machines. The ledger must remain independent of any single service contract so that history continues even after warranty, AMC or CAMC expires.

### 4.1 Machine Identity and Customer Link

| **Field group** | **Required information** |
|---|---|
| ERP identity | Immutable Machine Ledger ID / Service Asset ID and QR code |
| Customer | Customer account, billing customer, service site, address, contacts and Customer Portal visibility |
| Machine identity | Customer asset number, brand/make, model, serial number, machine type, capacity and operating range |
| Technical profile | Controller, compressor, refrigerant, electrical rating and configurable machine-type attributes |
| Evidence | Nameplate photo, machine photo, installation/commissioning documents and current-condition photo |
| Lifecycle | Installation date, commissioning date, active/inactive/transferred/scrapped status and ownership |
| Service coverage | Warranty, AMC, CAMC, chargeable status and current contract references |
| Responsibility | Assigned engineer, region, branch and Service Manager |

### 4.2 Other-Brand Machine Creation Rule

- Search first by customer, site, normalized serial number, customer asset number, model and prior service phone/contact.
- If a confirmed match exists, use the existing Machine Ledger and append the new Service Ticket. Never create a new history.
- If no match exists, create a new Machine Ledger from the service request. Minimum entry: customer, site, brand, model or machine type, available serial/asset number and machine/nameplate photo.
- If the serial number is unavailable or unreadable, create a Provisional Machine Ledger with a temporary ERP identity. Service Manager verification is required before permanent confirmation.
- When the correct serial is later found, merge through a governed duplicate-resolution function. Preserve both identities and all history; never delete old tickets.

### 4.3 Lifetime Service History

The Machine Ledger timeline must show all events in chronological order. Filters may separate SESS/other brand, warranty/AMC/CAMC/chargeable, branch, engineer, issue category and date, but the underlying record remains one continuous history.

- All complaints, visits, job cards and closure reports
- Diagnosis, fault codes, readings, root-cause analysis and corrective action
- Parts installed, removed, returned, replaced under warranty or billed
- Engineer allocation, arrival, punch-in/out, work time and visit outcome
- Customer signatures, OTP acknowledgements, feedback and communication log
- Warranty/AMC/CAMC changes, preventive schedules and next-due reminders
- Calibration certificates, modifications, relocation and decommissioning

### 4.4 Customer Portal View

- Customer sees only machines linked to the authorized customer account/site.
- Customer may raise a service request by selecting an existing machine or requesting registration of a new machine.
- Portal shows ticket status, visit schedule, service reports, recommendations, quotations/invoices when authorized, and full permitted service history.
- Internal notes, cost, margin, engineer route, approvals and confidential evidence are not exposed.
- Customer can download approved reports and acknowledge completion, pending work and recommended actions.

## 5. Service Entitlement and Commercial Decision

ERP must calculate and display an entitlement recommendation using an effective-dated snapshot. The Service Manager owns the final technical decision; the coordinator supports administration and billing handoff but cannot close the ticket or silently change entitlement.

| **Classification** | **System basis** | **Required action** |
|---|---|---|
| Warranty | Installation/commissioning date, warranty duration, covered machine, parts/labour and exclusions | Recommend free/covered or excluded; manager approves |
| AMC | Effective contract, included visits and labour/coverage | Track visit balance and non-covered materials |
| CAMC | Effective comprehensive coverage and explicit exclusions | Validate covered parts before issue |
| Chargeable | Expired/no contract, excluded cause/item or approved customer request | Customer estimate/approval and Accounts billing handoff |
| Approval required | Unclear ownership, missing contract, disputed cause or emergency | Block non-covered consumption or billing decision until authorized |

- Contract coverage is snapshotted when the ticket is reviewed so later master changes do not rewrite the original decision.
- Manager override requires reason, evidence, date/time and audit history.
- Engineer must see covered/non-covered status on mobile before using a spare.
- Emergency authorization may use a fast configured approval route, but no hidden bypass is allowed.

## 6. Mobile Engineer Application

The engineer mobile app must cover the complete field-service journey. A desktop-only dependency is not acceptable for normal engineer work.

| **Mobile area** | **Minimum capabilities** |
|---|---|
| Home | Today’s jobs, urgent breakdowns, AMC visits, waiting-for-spare, pending reports/expenses, alerts and offline sync state |
| Assignment | Accept, request clarification, unable-to-attend with reason, alternate time and manager notification |
| Travel | Start travel, on-the-way, arrival, navigation link and customer/site contact |
| Attendance | Live camera only, GPS/map address, device/time/battery/network metadata, punch-in/out and route capture only while punched in |
| Machine | QR/barcode scan, serial search, machine ledger, past history, manuals and contract status |
| Safety | Gate pass, PPE, insurance, work permit, hazard checklist and site acknowledgement |
| Diagnosis | Machine-type checklist, readings, alarms, voice-to-text notes and remote-support escalation |
| Evidence | Before/during/after photo/video, original timestamp, GPS watermark and attachment classification |
| Spares | Availability, reserve/request, approval, engineer custody, consume, unused return and removed-part disposition |
| Completion | Work result, pending action, customer OTP/signature, feedback, PDF report and WhatsApp/email sharing record |
| Daily reports | Morning Report add-on tasks; Evening Report completion comparison; 30-minute edit lock |
| Expenses | Travel, lodging, food and other configured heads with receipt capture and approval |

### 6.1 Offline and Sync Controls

- Assigned job, machine profile and required checklists must be available offline before travel.
- Offline work stores the original event timestamp, device identity and capture coordinates.
- Sync must be idempotent and must not duplicate photos, readings, parts or task updates.
- Conflicting edits must enter a visible reconciliation state; never silently overwrite manager or engineer data.
- Encrypted local cache must expire and be removable through remote logout/device revocation.

### 6.2 Morning and Evening Report Rule

| **CONTROL:** Only the Morning Report permits the engineer to add same-day add-on tasks. The Evening Report records the final status against the morning totals. Engineers have a 30-minute correction window; later changes require a manager amendment with reason and audit history. |
|---|

## 7. Site Diagnosis, Evidence and Technical Records

- Configurable checklist by machine type, model, complaint category and visit type.
- Readings may include temperature, humidity, voltage, current, pressure, alarms, compressor condition, refrigerant observations and safety results.
- Engineer selects root cause, action taken, temporary/permanent fix and further recommendation.
- Repeat complaint automatically links prior tickets and alerts the Service Manager.
- Remote support session, technical escalation and advice received are recorded against the same visit.
- Mandatory evidence is enforced by job type before the visit may be marked complete.

## 8. Spares, Engineer Custody and Removed Parts

| **Stage** | **Control** |
|---|---|
| Request | Engineer selects item, required quantity, reason, urgency and machine/ticket; system shows available stock |
| Approval | Entitlement and manager/Stores approval as configured; non-covered items require customer/commercial approval |
| Issue | Stock issue creates engineer/service custody; negative stock is never allowed |
| Consumption | Engineer records installed quantity and serial/lot where tracked |
| Unused return | Unused material returns through a receipt/return transaction and custody reconciliation |
| Removed part | Default ownership is customer-owned unless contract/warranty transfers ownership to SESS/manufacturer |
| Closure | All issued quantities reconcile to consumed, returned, transferred, approved loss or another typed disposition |

## 9. Customer Property and Accessories

Customer equipment held by SESS has zero SESS inventory value but may carry a declared/replacement liability value. It must remain traceable by physical location and custody owner.

- Serialised, high-value, calibrated and safety-critical accessories are recorded as individual lines.
- Lower-value accessories use a checklist with description, quantity, condition and photo.
- “Accessories as per list” alone is not sufficient evidence.
- Customer acknowledgement is required at receipt and return.
- Damage, shortage or missing accessory creates a case and blocks normal closure until disposition.

### 9.1 Due-Date Extension Approval

| **Situation** | **Approval** |
|---|---|
| First extension up to 30 days | Service Manager |
| Cumulative delay 31–60 days | Technical Director |
| More than 60 days, high value, repeat delay or legal/customer risk | TD/MD plus customer written acknowledgement |
| Customer-caused delay | Customer acknowledgement plus internal owner confirmation |

Every extension records the original date, revised date, cause, responsible party, approver, evidence, customer notification and escalation history.

## 10. AMC, CAMC and Preventive Service

- Contract master stores customer, contract number, period, covered machines, frequency, coverage, exclusions and contact/email rules.
- ERP automatically generates preventive Service Tickets quarterly, half-yearly, yearly or by configured frequency.
- Customer and team reminders are sent before visits; acknowledgements and rescheduling are tracked.
- Visit preparation includes gate pass, named engineer, PPE/insurance copy and work permit.
- Completed preventive visits update visit balance, next due date and machine service history.
- Expired contracts and missed visits appear on manager dashboards with escalation.

## 11. Ticket Status Model

| **Stage** | **Statuses** |
|---|---|
| Intake | New; Under Review |
| Planning | Assigned; Accepted; Travel Planned |
| Field work | On the Way; On Site; Diagnosis |
| Controlled waiting | Waiting for Customer; Waiting for Spare; Waiting for Approval |
| Resolution | Temporary Fix; Resolved — Feedback Pending |
| Commercial/verification | Billing Pending; Manager Verification |
| Terminal | Closed; Cancelled |

Status changes must be role-controlled. Waiting statuses require owner, reason, due date and escalation. Closed and Cancelled are terminal; corrections use an approved reopen/amendment process.

## 12. Roles, Permissions and Segregation

| **Role** | **Allowed** | **Restricted** |
|---|---|---|
| Service Manager | Review, entitlement decision, assign, approve, amend and close | Cannot erase audit/evidence or bypass stock controls |
| Senior Engineer | Regional/team jobs, diagnosis and technical escalation as assigned | No unrestricted commercial override |
| Junior Engineer | Only assigned jobs, mobile reports, evidence, parts request and expense | Cannot access other engineers’ jobs or close tickets |
| Service Coordinator | Register complaints, schedule, view all service jobs and billing coordination | Cannot make final entitlement decision or close ticket |
| Stores | Reserve, issue, receive and reconcile service material | Cannot approve technical entitlement |
| Accounts | Commercial validation, billing, tax/document and payment status | Cannot alter technical diagnosis |
| Customer Portal User | Authorized machines, requests, approved status/history/reports and acknowledgement | No internal notes, routes, costs, approvals or other customers’ data |

Employee ID is the login ID. Shared logins are prohibited. Dinesh may be configured as primary Service Manager, Sathishkumar as alternate/second-level manager and Venkat as Service Coordinator, but assignments must be effective-dated rather than hard-coded.

## 13. Notifications and Escalations

- New complaint, assignment and engineer acceptance/rejection
- Engineer late departure/arrival, missing punch or overdue report
- Waiting-for-spare/approval/customer aging
- Warranty/AMC expiry, preventive visit due and missed visit
- Repeat complaint, temporary fix, safety risk and customer dissatisfaction
- Unreconciled engineer custody, removed part or customer property
- Billing pending and closure overdue

## 14. Dashboards and Performance Indicators

| **Dashboard** | **Measures** |
|---|---|
| Operations | Open/overdue tickets, stage aging, engineer availability, branch/site workload and pending approvals |
| Service quality | Response time, resolution time, first-time fix rate, repeat-call rate, temporary-fix aging and RCA completion |
| Customer | Rating, feedback completion, acknowledgement, complaint recurrence and portal adoption |
| Engineer | Attendance compliance, on-time reporting, task completion, proof quality, travel/work time and assigned-job performance |
| Commercial | Warranty/AMC/CAMC/chargeable mix, billing pending, contract expiry and visit utilization |
| Material | Spare consumption, service custody, unused return, removed-part disposition and shortage |

## 15. Minimum Data Entities

| **Entity** | **Purpose** |
|---|---|
| service_ticket | Parent complaint/job |
| customer_machine_ledger / service_asset | Permanent machine identity and customer/site link |
| service_contract | Warranty/AMC/CAMC coverage and schedule |
| service_entitlement_snapshot | Frozen coverage decision basis per ticket |
| service_visit and engineer_assignment | Visit plan, acceptance, attendance and work session |
| task (existing) | Engineer work activities linked to ticket/visit |
| diagnosis and service_reading | Technical findings, measurements and RCA |
| spare_request and service_material_custody | Request, issue, consumption and return |
| removed_part | Identity, ownership and disposition of removed material |
| customer_property and accessory_line | Inbound custody, condition and return |
| customer_acknowledgement and feedback | OTP/signature, rating and communication |
| expense_claim | Field expense with evidence and approval |
| status_history and approval_history | Immutable decisions and lifecycle audit |
| offline_sync_event | Original client event, sync state and conflict resolution |

## 16. Security, Audit and Retention

- Role, branch, region, customer and assigned-engineer access must be enforced in the API, not only hidden in the screen.
- Optional biometric/PIN, device registration, token expiry and remote logout must be supported.
- Live punch-in photo must not accept a gallery image. Capture time, GPS, device and network metadata.
- Confidential attachments require classification, permission checks, encrypted transfer/storage and access logging.
- Every status, assignment, entitlement, approval, report amendment and master merge records old/new values, actor, time and reason.
- Core machine, service, contract, customer acknowledgement and audit history must be retained for at least 10 years.

## 17. Mandatory Acceptance Tests

- Register a phone/WhatsApp complaint and prevent an unverified duplicate open ticket.
- Create an other-brand Machine Ledger from a service request and show the same machine in the Customer Portal.
- Find an existing machine by serial/QR and append a new ticket without losing old service history.
- Create a provisional machine when serial is missing; later verify/merge while preserving complete ancestry.
- Recommend warranty/AMC/CAMC/chargeable status and audit a manager override.
- Engineer completes a visit offline and syncs once without duplicate events or evidence.
- Live-photo/GPS punch-in rejects gallery upload and records original time/device/location.
- Issue a spare to engineer custody and reconcile it to installed, unused-return and removed-part outcomes.
- Block non-covered spare consumption until the configured approval/customer acceptance is obtained.
- Capture customer OTP/signature, feedback and produce an approved PDF service report.
- Enforce Morning Report add-on rule, Evening Report comparison and 30-minute edit lock.
- Show AMC visit schedule, reminder, completed visit and updated next due date in the same machine history.
- Prevent an engineer from opening another engineer’s ticket through direct URL/API access.
- Verify Customer Portal isolation: no internal notes, other customers, routes, costs or approval evidence are exposed.
- Verify closed records and audit history remain retrievable for the configured 10-year retention period.

## 18. Implementation Direction

| **DEVELOPER INSTRUCTION:** Implement the Service Module as one mobile-first, audit-controlled process. Build the Customer Machine Ledger before finalizing complaint, visit and portal screens because it is the permanent anchor for all SESS and other-brand machine history. Mobile, web and Customer Portal must call the same controlled service APIs and must never maintain separate machine histories. |
|---|

- First freeze machine identity, duplicate/merge rules, customer/site access and entitlement snapshot.
- Then implement ticket/visit/task workflow, mobile offline events, attendance and evidence.
- Next connect service spares, engineer custody, removed parts, expenses, feedback and billing handoff.
- Complete Customer Portal, dashboards, notifications, security tests and acceptance evidence before go-live.
