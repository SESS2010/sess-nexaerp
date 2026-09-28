# SESS Service Engineer Hub — Data Model (V2.6)

Purpose: hand-over to the NexaERP team. Every sheet = one table; row 1 = column names. Identity of an employee across tables = `Employees.Email` (lower-case). Dates are stored as real dates in Asia/Kolkata.

## Relationships
```
Customers 1─* Machines 1─* Complaints(Machine_ID)
Customers 1─* Complaints 1─* Assignments *─1 Employees(Email)
Assignments 1─* Morning_Reports / Evening_Reports (Assignment_ID + day)
Complaints 1─* Expenses, Attachments, Status_History, Revenue_Ledger
Revenue_Ledger 1─* Revenue_Attribution *─1 Employees(Email)
Projects 1─* Complaints(Project_ID, production tasks)
Roles 1─* Employees(Role)   Registrations → Employees (on approval)
```

## Business rules
- Work streams: SERVICE (planned by Plan_Service roles), PRODUCTION (Plan_Production), TASK (Assign_Tasks). The same roles close their stream.
- Complaint → WORK_DONE automatically when every non-cancelled assignment is COMPLETED; a manager then verifies and closes.
- Report timeliness: morning due by Settings.MORNING_DUE_TIME, evening by EVENING_DUE_TIME on the work day. Expected days = allocation visit dates + days with a morning report. No report after the due time = MISSED.
- Score (0–100) = weighted average of: on-time completion, report timeliness (late/proxy = ½), acceptance, customer rating, revenue ÷ expense (5× = full), no missed reports. Weights in Settings.PERF_WEIGHTS.
- Daily plan: managers give many tasks at once (lines × employees). Accepting = morning report; evening update = evening report. Completion % counts only manager-given tasks; additional work is shown separately. Unfinished work is carried forward with the remaining quantity.
- Delete is allowed only when no other table refers to the record; otherwise set Active = FALSE. Every delete is written to Audit_Log with the full record.
- PIN is stored only as SHA-256 hash with salt + secret pepper (Script Properties).

## Complaints
Every work item: service complaint (SESS/SRV), production task (SESS/PRD) or employee task (SESS/TSK). One row per ticket.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Complaint_ID | Text | PK of Complaints (UUID) |  |
| 2 | Ticket_No | Text | Human ticket number SESS/SRV|PRD|TSK/0001/FY (unique per FY) |  |
| 3 | FY | Text |  | Financial year e.g. 2026-27 |
| 4 | Company_Code | Text | FK → Companies.Company_Code |  |
| 5 | Created_At | Date/DateTime |  |  |
| 6 | Created_By | Text | Employees.Email of creator |  |
| 7 | Source | Text |  | PLANNED (given by manager) | ADDITIONAL (added by employee) |
| 8 | Customer_ID | Text | FK → Customers.Customer_ID |  |
| 9 | Machine_ID | Text | FK → Machines.Machine_ID |  |
| 10 | Customer_Name | Text |  |  |
| 11 | End_User | Text |  |  |
| 12 | Contact_Name | Text |  |  |
| 13 | Contact_Phone | Text |  |  |
| 14 | Contact_Email | Text |  |  |
| 15 | Machine_Brand | Text |  |  |
| 16 | Model_No | Text |  |  |
| 17 | Serial_No | Text |  |  |
| 18 | Coverage | Text |  | Warranty | Out of Warranty | AMC | CAMC | CMC | Other Brand | Factory Work | Internal Task |
| 19 | AMC_CMC_No | Text |  |  |
| 20 | Category | Text |  |  |
| 21 | Priority | Text |  |  |
| 22 | Complaint | Text |  |  |
| 23 | Status | Text |  | Daily task: PLANNED, IN_PROGRESS, COMPLETED, PARTIAL, NOT_DONE, CARRIED_FORWARD, CANCELLED. Complaint: REGISTERED, ASSIGNED, IN_PROGRESS, WORK_DONE, CLOSED, CANCELLED, REOPENED, COMPLETED(legacy), REPORT_PENDING(legacy). Assignment: ASSIGNED, IN_PROGRESS, COMPLETED, CANCELLED, UNABLE, CLARIFICATION |
| 24 | Planned_Closure | Date/DateTime |  | Target date (tasks: due date) |
| 25 | Closed_At | Date/DateTime |  |  |
| 26 | Closure_Reason | Text |  |  |
| 27 | Updated_At | Date/DateTime |  |  |
| 28 | Updated_By | Text | Employees.Email |  |
| 29 | Work_Stream | Text |  | SERVICE | PRODUCTION | TASK |
| 30 | Service_Type | Text |  | Visit / work type (Options SERVICE_TYPE, PRODUCTION_TYPE, TASK_TYPE) |
| 31 | Project_ID | Text | FK → Projects.Project_ID |  |
| 32 | Replan_Count | Number |  | Times the target date was moved |
| 33 | Original_Planned | Date/DateTime |  | First target date before re-plans |

## Assignments
Allocation of a work item to an employee (one row per employee per allocation). LEAD/SUPPORT, response, status.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Assignment_ID | Text | PK of Assignments |  |
| 2 | Complaint_ID | Text | PK of Complaints (UUID) |  |
| 3 | Ticket_No | Text | Human ticket number SESS/SRV|PRD|TSK/0001/FY (unique per FY) |  |
| 4 | Engineer_Email | Text | FK → Employees.Email (employee identity) |  |
| 5 | Engineer_Name | Text |  |  |
| 6 | Assignment_Role | Text |  | LEAD | SUPPORT |
| 7 | Assigned_By | Text | Employees.Email |  |
| 8 | Assigned_At | Date/DateTime |  |  |
| 9 | Visit_Date | Date/DateTime |  |  |
| 10 | Instructions | Text |  |  |
| 11 | Response | Text |  | PENDING | ACCEPTED | CLARIFICATION | UNABLE |
| 12 | Response_At | Date/DateTime |  |  |
| 13 | Response_Remark | Text |  |  |
| 14 | Status | Text |  | Daily task: PLANNED, IN_PROGRESS, COMPLETED, PARTIAL, NOT_DONE, CARRIED_FORWARD, CANCELLED. Complaint: REGISTERED, ASSIGNED, IN_PROGRESS, WORK_DONE, CLOSED, CANCELLED, REOPENED, COMPLETED(legacy), REPORT_PENDING(legacy). Assignment: ASSIGNED, IN_PROGRESS, COMPLETED, CANCELLED, UNABLE, CLARIFICATION |
| 15 | Work_Stream | Text |  | SERVICE | PRODUCTION | TASK |
| 16 | Materials | Text |  | Material / tools the engineer must take or arrange (from manager) |

## Morning_Reports
Morning punch-in / plan per allocation per day.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Morning_ID | Text | PK |  |
| 2 | Assignment_ID | Text | PK of Assignments |  |
| 3 | Complaint_ID | Text | PK of Complaints (UUID) |  |
| 4 | Ticket_No | Text | Human ticket number SESS/SRV|PRD|TSK/0001/FY (unique per FY) |  |
| 5 | Engineer_Email | Text | FK → Employees.Email (employee identity) |  |
| 6 | Machine_ID | Text | FK → Machines.Machine_ID |  |
| 7 | Report_Date | Date/DateTime |  |  |
| 8 | Punch_In | Date/DateTime |  |  |
| 9 | Location | Text |  |  |
| 10 | Plan | Text |  |  |
| 11 | Safety_Check | Text |  |  |
| 12 | Created_At | Date/DateTime |  |  |
| 13 | Timeliness | Text |  | ON_TIME (by due time) | LATE (within REPORT_GRACE_HOURS) | VERY_LATE | PROXY (entered by coordinator/admin) | LEGACY |
| 14 | Delay_Hours | Text |  | Hours after the due time when it was submitted (0 = on time) |
| 15 | Entered_By | Text | Employees.Email |  |

## Evening_Reports
Evening status per employee per day (self, coordinator from WhatsApp, or legacy import).

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Evening_ID | Text | PK |  |
| 2 | Assignment_ID | Text | PK of Assignments |  |
| 3 | Complaint_ID | Text | PK of Complaints (UUID) |  |
| 4 | Ticket_No | Text | Human ticket number SESS/SRV|PRD|TSK/0001/FY (unique per FY) |  |
| 5 | Engineer_Email | Text | FK → Employees.Email (employee identity) |  |
| 6 | Machine_ID | Text | FK → Machines.Machine_ID |  |
| 7 | Report_Date | Date/DateTime |  |  |
| 8 | Completed | Text |  | Completed | Partially Completed | Not Completed |
| 9 | Work_Done | Text |  |  |
| 10 | Findings | Text |  |  |
| 11 | Parts_Used | Text |  |  |
| 12 | Next_Plan | Text |  |  |
| 13 | Pending_Reason | Text |  |  |
| 14 | Customer_Ack | Text |  |  |
| 15 | Created_At | Date/DateTime |  |  |
| 16 | Customer_ID | Text | FK → Customers.Customer_ID |  |
| 17 | Customer_Name | Text |  |  |
| 18 | Work_Type | Text |  |  |
| 19 | Report_Status | Text |  | SUBMITTED | NOT SUBMITTED | N/R |
| 20 | Report_No | Text |  |  |
| 21 | Closed_By | Text |  |  |
| 22 | Completion_Date | Date/DateTime |  |  |
| 23 | Remarks | Text |  |  |
| 24 | Entered_By | Text | Employees.Email |  |
| 25 | Entry_Mode | Text |  | SELF | COORDINATOR | LEGACY | LEGACY+WHATSAPP | WHATSAPP_LEDGER |
| 26 | WhatsApp_Text | Text |  | Original WhatsApp message pasted by coordinator |
| 27 | Timeliness | Text |  | ON_TIME (by due time) | LATE (within REPORT_GRACE_HOURS) | VERY_LATE | PROXY (entered by coordinator/admin) | LEGACY |
| 28 | Delay_Hours | Text |  | Hours after the due time when it was submitted (0 = on time) |

## Expenses
Employee expense per job/day with verification.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Expense_ID | Text | PK |  |
| 2 | Complaint_ID | Text | PK of Complaints (UUID) |  |
| 3 | Ticket_No | Text | Human ticket number SESS/SRV|PRD|TSK/0001/FY (unique per FY) |  |
| 4 | Engineer_Email | Text | FK → Employees.Email (employee identity) |  |
| 5 | Customer_ID | Text | FK → Customers.Customer_ID |  |
| 6 | Machine_ID | Text | FK → Machines.Machine_ID |  |
| 7 | Expense_Date | Date/DateTime |  |  |
| 8 | Transport | Number |  |  |
| 9 | Two_Wheeler_KM | Number |  |  |
| 10 | Two_Wheeler_Amount | Number |  |  |
| 11 | Boarding | Number |  |  |
| 12 | Lodging | Number |  |  |
| 13 | Food | Number |  |  |
| 14 | Other | Number |  |  |
| 15 | Material_Cost | Number |  |  |
| 16 | Total | Number |  |  |
| 17 | Receipt_URL | Text |  |  |
| 18 | Status | Text |  | Daily task: PLANNED, IN_PROGRESS, COMPLETED, PARTIAL, NOT_DONE, CARRIED_FORWARD, CANCELLED. Complaint: REGISTERED, ASSIGNED, IN_PROGRESS, WORK_DONE, CLOSED, CANCELLED, REOPENED, COMPLETED(legacy), REPORT_PENDING(legacy). Assignment: ASSIGNED, IN_PROGRESS, COMPLETED, CANCELLED, UNABLE, CLARIFICATION |
| 19 | Verified_By | Text | Employees.Email |  |
| 20 | Verified_At | Date/DateTime |  |  |
| 21 | Verify_Remark | Text |  |  |

## Feedback
Customer rating per complaint (1-5).

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Feedback_ID | Text | PK |  |
| 2 | Complaint_ID | Text | PK of Complaints (UUID) |  |
| 3 | Ticket_No | Text | Human ticket number SESS/SRV|PRD|TSK/0001/FY (unique per FY) |  |
| 4 | Rating | Number |  |  |
| 5 | Comments | Text |  |  |
| 6 | Customer_Name | Text |  |  |
| 7 | Customer_Email | Text |  |  |
| 8 | Submitted_At | Date/DateTime |  |  |

## Revenue_Ledger
Invoices / revenue documents, optionally linked to a ticket.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Revenue_ID | Text | PK of Revenue_Ledger |  |
| 2 | Complaint_ID | Text | PK of Complaints (UUID) |  |
| 3 | Ticket_No | Text | Human ticket number SESS/SRV|PRD|TSK/0001/FY (unique per FY) |  |
| 4 | Customer_ID | Text | FK → Customers.Customer_ID |  |
| 5 | Machine_ID | Text | FK → Machines.Machine_ID |  |
| 6 | Document_Type | Text |  |  |
| 7 | Document_No | Text |  |  |
| 8 | Document_Date | Date/DateTime |  |  |
| 9 | Basic_Value | Number |  |  |
| 10 | GST | Number |  |  |
| 11 | Collection_Value | Number |  |  |
| 12 | Verified | Boolean |  | TRUE / FALSE |
| 13 | Verified_By | Text | Employees.Email |  |
| 14 | Verified_At | Date/DateTime |  |  |
| 15 | Customer_Name | Text |  |  |
| 16 | Collection_Date | Date/DateTime |  |  |
| 17 | Remarks | Text |  |  |
| 18 | Entered_By | Text | Employees.Email |  |
| 19 | Entered_At | Date/DateTime |  |  |

## Revenue_Attribution
Share of each revenue document credited to employees (% and value).

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Attribution_ID | Text | PK |  |
| 2 | Revenue_ID | Text | PK of Revenue_Ledger |  |
| 3 | Engineer_Email | Text | FK → Employees.Email (employee identity) |  |
| 4 | Share_Percent | Number |  | % of Basic_Value credited |
| 5 | Attributed_Value | Number |  | Basic_Value × share % |

## Customers
Customer master.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Customer_ID | Text | FK → Customers.Customer_ID |  |
| 2 | Customer_Name | Text |  |  |
| 3 | End_User | Text |  |  |
| 4 | Contact_Name | Text |  |  |
| 5 | Designation | Text |  |  |
| 6 | Phone | Text |  |  |
| 7 | Alternate_Phone | Text |  |  |
| 8 | Email | Text |  |  |
| 9 | Alternate_Email | Text |  |  |
| 10 | GSTIN | Text |  |  |
| 11 | Billing_Address | Text |  |  |
| 12 | Site_Address | Text |  |  |
| 13 | City | Text |  |  |
| 14 | State | Text |  |  |
| 15 | Pincode | Text |  |  |
| 16 | Company_Code | Text | FK → Companies.Company_Code |  |
| 17 | Customer_Category | Text |  |  |
| 18 | Active | Boolean |  | TRUE / FALSE (soft delete) |

## Machines
Machine / warranty / AMC / CAMC contract master (per customer).

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Machine_ID | Text | FK → Machines.Machine_ID |  |
| 2 | Customer_ID | Text | FK → Customers.Customer_ID |  |
| 3 | Brand | Text |  |  |
| 4 | Machine_Type | Text |  |  |
| 5 | Model_No | Text |  |  |
| 6 | Serial_No | Text |  |  |
| 7 | Capacity | Text |  |  |
| 8 | Temperature_Range | Text |  |  |
| 9 | Humidity_Range | Text |  |  |
| 10 | Controller | Text |  |  |
| 11 | Refrigerant | Text |  |  |
| 12 | Install_Date | Date/DateTime |  |  |
| 13 | Commissioning_Date | Date/DateTime |  |  |
| 14 | Warranty_Start | Date/DateTime |  |  |
| 15 | Warranty_End | Date/DateTime |  |  |
| 16 | Coverage_Type | Text |  | Warranty | Out of Warranty | AMC | CAMC | CMC | Other Brand |
| 17 | Contract_No | Text |  |  |
| 18 | Contract_Start | Date/DateTime |  |  |
| 19 | Contract_End | Date/DateTime |  |  |
| 20 | Contract_Value | Number |  |  |
| 21 | Service_Frequency | Text |  |  |
| 22 | Next_Service_Date | Date/DateTime |  |  |
| 23 | Site_Location | Text |  |  |
| 24 | Machine_Status | Text |  |  |
| 25 | Remarks | Text |  |  |
| 26 | Active | Boolean |  | TRUE / FALSE (soft delete) |

## Employees
Employee master and login (Login ID, role, PIN hash).

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Employee_ID | Text | PK of Employees |  |
| 2 | Login_ID | Text | SESS-EMP-0001 (unique) |  |
| 3 | Name | Text |  |  |
| 4 | Email | Text |  |  |
| 5 | Role | Text | FK → Roles.Role_Code |  |
| 6 | Grade | Text |  |  |
| 7 | Department | Text |  |  |
| 8 | Active | Boolean |  | TRUE / FALSE (soft delete) |
| 9 | Joined_On | Date/DateTime |  |  |
| 10 | Pin_Hash | Text |  | SHA-256(salt|PIN|pepper) – never the PIN |
| 11 | Pin_Reset | Boolean |  | TRUE = temporary PIN, must change at next login |
| 12 | Last_Login | Date/DateTime |  |  |
| 13 | Sub_Department | Text |  |  |
| 14 | Phone | Text |  |  |
| 15 | Pin_Salt | Text |  | Salt for Pin_Hash (blank = Employee_ID) |
| 16 | Approval_Status | Text |  | LEGACY | APPROVED |
| 17 | Registered_At | Date/DateTime |  |  |
| 18 | Approved_By | Text |  |  |
| 19 | Approved_At | Date/DateTime |  |  |

## Companies
SESS legal entities (Pvt Ltd / Proprietorship).

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Company_Code | Text | FK → Companies.Company_Code |  |
| 2 | Company_Name | Text |  |  |
| 3 | GSTIN | Text |  |  |
| 4 | Address | Text |  |  |
| 5 | Active | Boolean |  | TRUE / FALSE (soft delete) |

## Options
All dropdown values (type + value + active).

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Option_Type | Text |  |  |
| 2 | Option_Value | Number |  |  |
| 3 | Sort_Order | Number |  |  |
| 4 | Active | Boolean |  | TRUE / FALSE (soft delete) |

## Status_History
Every status change of a work item (ISO audit trail).

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | History_ID | Text | PK |  |
| 2 | Complaint_ID | Text | PK of Complaints (UUID) |  |
| 3 | Ticket_No | Text | Human ticket number SESS/SRV|PRD|TSK/0001/FY (unique per FY) |  |
| 4 | From_Status | Text |  |  |
| 5 | To_Status | Text |  |  |
| 6 | Channel | Text |  |  |
| 7 | Remark | Text |  |  |
| 8 | Changed_By | Text |  |  |
| 9 | Changed_At | Date/DateTime |  |  |

## Audit_Log
Every create/update/delete/login with before/after JSON (ISO audit trail).

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Audit_ID | Text | PK |  |
| 2 | Entity | Text |  |  |
| 3 | Entity_ID | Text |  |  |
| 4 | Action | Text |  |  |
| 5 | Before_JSON | Text |  | Record before change (JSON) |
| 6 | After_JSON | Text |  | Record after change (JSON) |
| 7 | Reason | Text |  |  |
| 8 | Actor | Text |  |  |
| 9 | At | Text |  |  |

## Attachments
Report / DC / bill copies stored in Google Drive.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Attachment_ID | Text | PK |  |
| 2 | Assignment_ID | Text | PK of Assignments |  |
| 3 | Complaint_ID | Text | PK of Complaints (UUID) |  |
| 4 | Ticket_No | Text | Human ticket number SESS/SRV|PRD|TSK/0001/FY (unique per FY) |  |
| 5 | Engineer_Email | Text | FK → Employees.Email (employee identity) |  |
| 6 | Report_Type | Text |  |  |
| 7 | File_Name | Text |  |  |
| 8 | Mime_Type | Text |  |  |
| 9 | Drive_URL | Text |  |  |
| 10 | Uploaded_At | Date/DateTime |  |  |

## Projects
New machine projects (order to commissioning).

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Project_ID | Text | FK → Projects.Project_ID |  |
| 2 | Project_No | Text |  |  |
| 3 | FY | Text |  | Financial year e.g. 2026-27 |
| 4 | Company_Code | Text | FK → Companies.Company_Code |  |
| 5 | Customer_ID | Text | FK → Customers.Customer_ID |  |
| 6 | Machine_ID | Text | FK → Machines.Machine_ID |  |
| 7 | Customer_Name | Text |  |  |
| 8 | Project_Name | Text |  |  |
| 9 | PO_Tender_Offer | Text |  |  |
| 10 | Order_Date | Date/DateTime |  |  |
| 11 | Order_Value | Number |  |  |
| 12 | Stage | Text |  |  |
| 13 | Status_Blocker | Text |  |  |
| 14 | Next_Action | Text |  |  |
| 15 | Customer_Contact | Text |  |  |
| 16 | Customer_Email | Text |  |  |
| 17 | SESS_Owner | Text |  |  |
| 18 | Planned_Date | Date/DateTime |  |  |
| 19 | Start_Date | Date/DateTime |  |  |
| 20 | Target_Dispatch | Text |  |  |
| 21 | Installation_Date | Date/DateTime |  |  |
| 22 | Last_Updated | Text |  |  |
| 23 | Active | Boolean |  | TRUE / FALSE (soft delete) |
| 24 | Closed_At | Date/DateTime |  |  |
| 25 | Closure_Remark | Text |  |  |

## Archive_Log
Year-end archive files.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Archive_ID | Text | PK |  |
| 2 | FY | Text |  | Financial year e.g. 2026-27 |
| 3 | Spreadsheet_URL | Text |  |  |
| 4 | XLSX_URL | Text |  |  |
| 5 | Rows | Number |  |  |
| 6 | Created_By | Text | Employees.Email of creator |  |
| 7 | Created_At | Date/DateTime |  |  |
| 8 | Locked | Boolean |  |  |
| 9 | Spreadsheet_ID | Text |  |  |
| 10 | Details | Text |  |  |
| 11 | Removed_At | Date/DateTime |  |  |
| 12 | Removed_By | Text |  |  |
| 13 | Removed_Rows | Number |  | Rows removed from the app per ledger after year-end archive |
| 14 | Backup_Name | Text |  |  |

## Settings
Configuration key/value.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Key | Text |  |  |
| 2 | Value | Number |  |  |
| 3 | Description | Text |  |  |

## Roles
Role Master: portal and permission flags per role.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Role_Code | Text | PK of Roles |  |
| 2 | Role_Name | Text |  |  |
| 3 | Portal | Text |  | TD_MD | MANAGER | ADMIN_STAFF | EMPLOYEE | AUDITOR |
| 4 | Register_Complaint | Boolean |  | Permission flag |
| 5 | Plan_Service | Boolean |  | Permission flag |
| 6 | Plan_Production | Boolean |  | Permission flag |
| 7 | Close_Work | Boolean |  | Permission flag |
| 8 | Verify_Expense | Boolean |  | Permission flag |
| 9 | Manage_Masters | Boolean |  | Permission flag |
| 10 | Manage_Employees | Boolean |  | Permission flag |
| 11 | Manage_Roles | Boolean |  | Permission flag |
| 12 | View_All | Boolean |  | Permission flag |
| 13 | Field_Work | Boolean |  | Permission flag |
| 14 | Active | Boolean |  | TRUE / FALSE (soft delete) |
| 15 | Sort_Order | Number |  |  |
| 16 | Remarks | Text |  |  |
| 17 | Proxy_Report | Boolean |  | Permission flag |
| 18 | Assign_Tasks | Boolean |  | Permission flag |
| 19 | Manage_Revenue | Boolean |  | Permission flag |

## Daily_Tasks
Daily task plan: one row per task per employee per day (PLANNED by a manager or ADDITIONAL added by the employee), with acceptance, evening status, review and carry-forward.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Task_ID | Text |  |  |
| 2 | Batch_ID | Text |  | Tasks saved together in one plan save |
| 3 | Plan_Date | Date/DateTime |  | Day the task is planned for |
| 4 | Stream | Text |  | PRODUCTION (factory) | SERVICE | TASK (office) |
| 5 | Employee_Email | Text |  |  |
| 6 | Employee_Name | Text |  |  |
| 7 | Team | Text |  | Employee sub-department / department at planning time |
| 8 | Task | Text |  |  |
| 9 | Complaint_ID | Text | PK of Complaints (UUID) |  |
| 10 | Ticket_No | Text | Human ticket number SESS/SRV|PRD|TSK/0001/FY (unique per FY) |  |
| 11 | Project_ID | Text | FK → Projects.Project_ID |  |
| 12 | Target_Qty | Text |  |  |
| 13 | Unit | Text |  |  |
| 14 | Planned_Hours | Text |  |  |
| 15 | Priority | Text |  |  |
| 16 | Source | Text |  | PLANNED (given by manager) | ADDITIONAL (added by employee) |
| 17 | Assigned_By | Text | Employees.Email |  |
| 18 | Assigned_At | Date/DateTime |  |  |
| 19 | Accept_Status | Text |  | PENDING | ACCEPTED | CLARIFICATION | UNABLE |
| 20 | Accepted_At | Date/DateTime |  |  |
| 21 | Accept_Remark | Text |  |  |
| 22 | Status | Text |  | Daily task: PLANNED, IN_PROGRESS, COMPLETED, PARTIAL, NOT_DONE, CARRIED_FORWARD, CANCELLED. Complaint: REGISTERED, ASSIGNED, IN_PROGRESS, WORK_DONE, CLOSED, CANCELLED, REOPENED, COMPLETED(legacy), REPORT_PENDING(legacy). Assignment: ASSIGNED, IN_PROGRESS, COMPLETED, CANCELLED, UNABLE, CLARIFICATION |
| 23 | Done_Qty | Text |  |  |
| 24 | Actual_Hours | Text |  |  |
| 25 | Pending_Reason | Text |  |  |
| 26 | Evening_Remark | Text |  |  |
| 27 | Updated_At | Date/DateTime |  |  |
| 28 | Updated_By | Text | Employees.Email |  |
| 29 | Carry_From | Text |  | Task_ID of the unfinished task this continues |
| 30 | Carried_To | Text |  | Date the unfinished part moved to |
| 31 | Review_Status | Text |  | VERIFIED | REWORK | APPROVED | REJECTED |
| 32 | Reviewed_By | Text |  |  |
| 33 | Reviewed_At | Date/DateTime |  |  |
| 34 | Review_Remark | Text |  |  |
| 35 | Materials | Text |  | Material / tools the engineer must take or arrange (from manager) |
| 36 | Reassigned_From | Text |  | Previous employee when the manager moved the task |

## Registrations
Self-registration requests (email verified) waiting for admin approval.

| # | Column | Type | Key / relation | Meaning |
|---|---|---|---|---|
| 1 | Reg_ID | Text | PK |  |
| 2 | Name | Text |  |  |
| 3 | Email | Text |  |  |
| 4 | Phone | Text |  |  |
| 5 | Department | Text |  |  |
| 6 | Sub_Department | Text |  |  |
| 7 | Requested_Role | Text |  |  |
| 8 | Note | Text |  |  |
| 9 | Pin_Hash | Text |  | SHA-256(salt|PIN|pepper) – never the PIN |
| 10 | Pin_Salt | Text |  | Salt for Pin_Hash (blank = Employee_ID) |
| 11 | Email_Verified_At | Date/DateTime |  |  |
| 12 | Status | Text |  | Daily task: PLANNED, IN_PROGRESS, COMPLETED, PARTIAL, NOT_DONE, CARRIED_FORWARD, CANCELLED. Complaint: REGISTERED, ASSIGNED, IN_PROGRESS, WORK_DONE, CLOSED, CANCELLED, REOPENED, COMPLETED(legacy), REPORT_PENDING(legacy). Assignment: ASSIGNED, IN_PROGRESS, COMPLETED, CANCELLED, UNABLE, CLARIFICATION |
| 13 | Submitted_At | Date/DateTime |  |  |
| 14 | Decided_By | Text |  |  |
| 15 | Decided_At | Date/DateTime |  |  |
| 16 | Decision_Remark | Text |  |  |
| 17 | Employee_ID | Text | PK of Employees |  |
