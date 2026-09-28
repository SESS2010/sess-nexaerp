# SESS Service Engineer Hub – Change history (newest first)

# V2.6 (23 Sep 2026) — Year-End Archive
- New page 🗄 Year-End Archive (TD / MD only).
- Preview per financial year: rows to archive and rows staying, per ledger.
- Step 1 Create archive: copies the year to a new Google Sheet in Drive (Settings › ARCHIVE_FOLDER_ID) + an Excel .xlsx copy for download to PC. Includes a Summary sheet and a reference copy of masters. Every sheet's row count is verified before success is reported. Nothing is removed.
- Step 2 Remove from app: only for finished years (after 31 March), only TD/MD, only after typing "DELETE 2025-26". A full safety copy of the whole backend is made first. Only rows whose ID is inside the archive file are removed.
- Never archived / removed: open complaints and tasks (with all their allocations, reports, expenses), pending registrations, active projects, and all masters (customers, machines, employees, roles, options, settings).
- Archived with each closed complaint/task: allocations, morning & evening reports, expenses, attachments list, status history, feedback, revenue + shares, daily tasks; plus that year's audit log, closed projects, decided registrations and reports without a ticket.
- Saving now also holds the write lock for new rows, so nothing can be added while a year is being removed.

# V2.5.1 (23 Sep 2026)
- Every ledger / table has a toolbar: 🔍 Filter rows (type any word – only matching rows stay), ⚙ Columns (tick / untick columns to show or hide), Reset.
- Column choices are remembered per table on each device (phone can show fewer columns than PC). Hidden columns are also hidden in the phone card view.
- Filter works on the rows on screen; to search all records use the page search / date filters, then filter.
- Only Index.html changed (+ version in Code.gs). No setup needed.

# V2.5 (23 Sep 2026)
- Before accepting, My Day shows the full job: ticket no., priority, visit type, customer, end user, site, contact person with tap-to-call mobile, email, machine make / type / model / serial, coverage & contract end, complaint text, target date.
- "Materials to take / arrange" – manager fills it per task line in Daily Plan (and in ticket allocation); shown in orange to the engineer; carried forward with the task.
- 📦 Request from store – engineer sends the material list; it becomes an Office task for every active Store employee (role STORE or sub-department STORE).
- Multi-day tickets: ⤵ "Bring unfinished from previous day" copies every unfinished task to the new day (same employee, remaining qty, materials) – the engineer accepts it again that morning. Plan board shows each person's open tickets – tap "+ ticket" to continue today.
- ↪ Reassign: manager moves tasks to another employee (e.g. engineer shifted to another site); the ticket allocation moves too and the previous engineer's allocation is ended with the reason.
- Ticket closed: remaining planned tasks for that ticket are cancelled automatically; the ticket file keeps a day-by-day task history (who, what, materials, status, pending reasons, reassignments).
- 🔄 View switch in the header: Auto (phone layout on phones, PC layout on computers) / 📱 Mobile / 🖥 PC – remembered on each device.
- Compact header on phones.

# V2.4 (23 Sep 2026)
- Phone layout: every table becomes cards on phones (My Day, Daily Plan, ledgers, lists) – no sideways scrolling; bigger touch buttons; inputs full width.
- Report timing: every morning/evening report stores submitted date & time, Delay_Hours and a grade: ON_TIME (by due time) / LATE (within grace, default 24 h = next day) / VERY_LATE / PROXY (entered by coordinator/admin) / MISSED. Employees may submit up to LATE_ALLOW_DAYS late (default 10) – marked VERY_LATE; older only via coordinator.
- Performance: Very late count and average delay hours; score: on time 1, late 0.75, very late 0.25, coordinator 0.5, missed 0.
- Coordinator / admin team: "Reports missing" shows morning AND evening per employee (daily tasks + tickets), with one-click "Enter morning" (new) and "Enter evening". Admin Staff role may now fill reports for others (switched on once by setup; can be changed in Role Master).
- 🌳 Task Ledger: tree Login ID → date → tasks with who gave it (TD/MD, Manager, Self-planned, Additional) and done counts. Managers can plan their own tasks when TD/MD is busy; they cannot verify their own tasks.
- Safe saving for many users: one lock per request; each update re-reads the row from the sheet inside the lock and writes only the changed columns – two people saving at the same time no longer overwrite each other.
- Automatic nightly backup: run installDailyBackup() once → full copy of the backend every night ~11 PM, last 30 kept (Settings › BACKUP_FOLDER_ID).
- Complaint file shows submitted time and timing of every morning/evening report.
- Privacy re-verified: an engineer can see only his own tasks, reports, calendar, expenses and score (16/16 server checks).

# V2.3.1 (23 Sep 2026)
- Left sidebar menu grouped: Plan & allocate, Daily reports, Records, Management, Masters & admin. Empty groups hide automatically per role.
- ☰ button in the header hides/shows the menu for full-width view (remembered on that device). On phones the menu slides in and closes after choosing a page.
- Header stays on top while scrolling; wider content area.
- No data or server changes (only version number in Code.gs).
- Dashboard cards no longer cut off on the right (2 equal columns, 1 column on phones).

# V2.3 (23 Sep 2026) — Daily task plan for factory, service and office teams
- New Daily_Tasks sheet; screens 📝 Daily Plan (managers) and ☀ My Day (every employee; employees land on it after login).
- Multiple tasks at once: many lines × many employees in one save, paste-a-list, per-line work type / ticket / target / unit / hours / priority.
- Stream rules: Factory = Plan production, Service = Plan service, Office (store, purchase, admin) = Assign tasks. Assistant Manager (new role) plans both.
- Service lines linked to a ticket allocate the ticket; completing them updates the allocation and moves the complaint to WORK_DONE.
- Accept (morning) creates the morning report; evening update creates the evening report (on-time / late / coordinator).
- Additional work lines (any number) counted separately from manager-given tasks.
- Manager review: verify, rework, approve/reject additional, carry forward remaining quantity, cancel; delete only if not yet accepted.
- Coordinator can accept/update on behalf of an employee (WhatsApp) – marked PROXY.
- Dashboard "Today's task plan by team"; Performance columns Daily tasks done / Additional; score weight 20 for daily completion (PERF_WEIGHTS now 7 values).
- Fixed: after the first PIN change the app opened the Dashboard instead of the employee's own page.

# V2.2 (23 Sep 2026)
Login: email typo guard (gmai.com, gmial.com …) on save/registration + health warning; login messages say exactly why (not registered, typo'd email saved by admin, inactive, no PIN, wrong PIN, registration pending); approval pre-selects the record with the SAME email and applies the employee's own PIN.
Tasks for every employee: new stream TASK (SESS/TSK/…) with due date, given by roles with "Assign tasks" (new Role Master tick) to any employee.
Report timeliness: evening report has a work date (back-date up to LATE_ALLOW_DAYS); morning/evening marked ON_TIME / LATE; coordinator entries PROXY; missing = MISSED. Report Calendar page (employee × day).
Performance: score 0–100 with rank (weights in Settings), done-on-time %, reports on time %, late, missed, via coordinator, revenue, expense, net. Each employee sees own score + 14-day calendar on dashboard.
Open & Overdue page: To close / Overdue / Not allocated / No target / On track; Re-plan with reason (counted), Allocate, Close, bulk close.
Dashboard: open / overdue now count ALL open work (older jobs were hidden before); overdue from the day after the target; aging chart; no-target KPI.
Revenue: invoice entry against ticket or customer, automatic equal share to the engineers (or manual %), duplicate invoice check, maker-checker verification; feeds Performance.
Delete: Customer, Machine, Employee, Project, Role, Option, Complaint (TD), coordinator day report, expense, revenue — only if nothing uses it; otherwise a message lists where it is used. Every delete is kept in Audit_Log.
Options page lists every dropdown value with Deactivate / Delete; Options sheet is the single master (defaults seeded once).
Data Quality page: 22 completeness checks incl. contracts ending in 30 days (renewal) and chargeable jobs closed without invoice.
Roles: new Admin Manager (approves logins), HR sees performance, new flags Assign_Tasks, Manage_Revenue.
UI: new colour theme, wrapping menu with icons and active page highlight.
Speed: version-stamped shared cache for dashboard/performance/calendar/data quality; indexed overdue lookup.
Data model: Data_Dictionary sheet + DATA_MODEL.md (313 columns) for the NexaERP team; PAGE_ACCESS.md.

# V2.1 (23 Sep 2026)
- Self registration: email OTP (6 digits, 10 min, 5 tries, 1 per minute, 5 per hour), employee sets own PIN, admin approval with role; "Link to existing" keeps old Login ID and history (also for already-activated placeholder records).
- Login with email or Login ID; Forgot PIN by email code; approval / rejection emails.
- Coordinator WhatsApp day-report entry (Proxy_Report permission), reports-missing list with WhatsApp links, Daily Report Ledger in the same columns as the Excel register, engineers see their own ledger.
- "+ Add new" on every dropdown; quick-add customer and machine with duplicate detection; full Customer Master also blocks duplicate names.
- Evening report: optional Service report status and Report No.
- Speed: each sheet read once per request + 2-minute shared cache for Employees/Roles/Settings/Options/Companies (cleared on every save). Measured: ~50% fewer sheet reads per request.
- Date boxes saved as the Indian calendar day (no UTC shift).
- New: Registrations sheet; Evening_Reports ledger columns (Customer, Work_Type, Report_Status, Report_No, Closed_By, Completion_Date, Remarks, Entered_By, Entry_Mode, WhatsApp_Text); Employees Phone/Pin_Salt/Approval fields; Roles.Proxy_Report. setup() adds and back-fills all of them; nothing is deleted.
- Backend V2.1 Excel: WhatsApp ledger (74 rows) imported — 52 matched to existing reports and enriched with the WhatsApp text, 22 added; 315 customers added from the MASTER list (50 were already present).
- Manifest: adds permission to send email (script.send_mail).

# V2.0 (23 Sep 2026) — Role Master, portals, Service + Production streams

- Role Master sheet + screen: portal and 10 permissions per role; add roles (e.g. STORE_KEEPER) without code changes. TD/MD always full; roles in use cannot be deactivated.
- Four portals + auditor: TD/MD, Manager, Admin staff, Employee. Menus follow the role's permissions.
- New default roles: TECHNICIAN, ADMIN_STAFF, ACCOUNTS, STORE, PURCHASE (plus existing ones).
- Register Work: every role with "Register" can raise a service complaint (engineers, admin staff, store, managers, TD/MD).
- Production tasks (SESS/PRD/0001/FY): created and allocated by Production Manager/TD/MD, optional link to a Project; work types Fabrication, Refrigeration, Electrical, PLC & Labview, Assembly, Testing/FAT, Packing/Dispatch.
- Service types incl. Warranty/AMC/CAMC × PM/Breakdown and Out-of-warranty; checked against machine coverage.
- Stream-based planning: Service Manager/Coordinator see and allocate only service; Production Manager only production. Any active field employee can be allocated either (cross-over).
- Allocation shows every employee's department, sub-department, grade, open tasks and work already booked on that date.
- Stream-based closing: Service Manager closes service, Production Manager closes production.
- Admin staff/employees see work they registered or worked on; View-all roles see everything.
- Dashboard, Work Ledger and Performance filter by Service / Production; ledger filter by service type.
- Employees: Sub_Department (Fabrication, Refrigeration, Electrical, PLC & Labview, Store, Purchase, Software, Marketing, IT Sales), Grade list.
- New columns: Complaints.Work_Stream/Service_Type/Project_ID, Assignments.Work_Stream, Employees.Sub_Department; new sheet Roles. setup() adds them and back-fills old rows.

# V1.9.3 (23 Sep 2026) — built for the SESS daily service workflow

- **Login ID + PIN** (hashed with salt+pepper, never sent to browser): works for Gmail users on mobile without sharing the Google Sheet. Forced PIN change on first login, 5-attempt lock-out, 6-hour sliding session, Logout, Change PIN, admin Set PIN. Google login kept for owner/Workspace users.
- **Task details for engineers**: customer, end user, site address, contact phone, machine, coverage, complaint text, priority.
- **Complaint file** (Ledger/Tasks › View): customer, machine, allocation, morning + evening reports, report copies (Drive links), expenses, full status history; Verify & Close / Reopen; Print/Save PDF.
- **Expense verification** screen (Accounts / Service Mgr / TD / MD): Verify / Reject with reason; cannot verify own expense; engineers see their own status. Legacy expenses excluded from the queue.
- **Engineer Performance** (1/3/6/12 months): assigned, done, open, unable, completion %, acceptance %, response hours, morning/evening counts, report compliance %, average close days, expense booked/verified/pending, rating. Printable.
- **AUDITOR** role: read-only across dashboard, ledger, files, expenses, performance.
- Closing a complaint cancels any still-open engineer assignments on it.
- Changing an employee's email moves their history (assignments, reports, expenses) to the new email.
- Audit_Log records PIN logins, failed logins, PIN changes and actor for every action.
- New columns (added automatically by setup()): Employees.Pin_Hash/Pin_Reset/Last_Login, Expenses.Verify_Remark.

# V1.9.2 — Review fixes (23 Sep 2026)

Same Google Sheet, same columns. Replace Code.gs + Index.html, run setup(), redeploy a New version.

## Critical bugs fixed
1. **Screens returning empty** – google.script.run cannot send Date values to the browser (it delivers null).
   V1.9.1 failed on: My Tasks, All Assigned Tasks, Complaint Ledger, Project Ledger, Employee Master list,
   Employee Preview, management Dashboard (whenever a critical/overdue case existed), first load of
   Customer/Machine Master, and Morning/Evening/Expense task dropdowns. All server returns now convert dates.
2. **Phone numbers like "+91 98400…" saved as #ERROR!** – text starting with + = - @ is now stored as text
   (also blocks formula injection).
3. **Allocation list missed older open complaints** – it only looked at the newest 50. New `listOpenComplaints()`.
4. **Legacy COMPLETED complaints counted as open** on dashboard/ledger. Closed set = CLOSED / COMPLETED / CANCELLED.
5. **Engineer "Open tasks" never reduced** – reports did not update status. Now:
   Morning report → assignment IN_PROGRESS (auto-accepts if pending), complaint IN_PROGRESS.
   Evening "Completed" → assignment COMPLETED; when all engineers complete → complaint WORK_DONE
   (awaiting management closure). New dashboard tile "Work done – to close".
6. **TD/MD Preview showed TD's own pending reports** instead of the employee's.
7. **Master list cache over 100 KB crashed** Customer/Machine Master (large legacy customer lists).
8. **Legacy linking wrote to fixed column positions** – now uses the real header, writes in one batch,
   and never overwrites rows already owned by another employee's email.

## Security
- setup(), migrateLegacyTodoData(), systemHealthCheck(), activateInitialAdmin() could be called from any
  logged-in browser. Now restricted to TD/MD/IT admins (activateInitialAdmin only works when no TD/MD exists).
- HR/IT could create a TD/MD login. Now only TD/MD can create or change TD/MD records.
- An admin cannot deactivate / re-role / re-email their own login (prevents lock-out).
- Allocation validates engineers against Employee Master, blocks closed complaints and duplicate open assignments.
- correctRecord() can no longer change a record's primary ID.

## Data quality
- Evening report: Pending Reason required if not Completed.
- Expense: no negative values, at least one amount, date required.
- Ticket/Project numbers never go backwards even if Script Properties are reset (checks the sheet).
- Close complaint rejects already-closed records.

## UI
- Mobile viewport now works (HtmlService ignores the HTML meta tag; set in doGet).
- Edit buttons: Project (progress updates), Customer, Machine/AMC contract.
- Prev/Next paging on Tasks and Ledger (previously only first 50 rows).
- Buttons disabled while saving (no double tickets on double-tap).
- Allocate button shown only to allocation roles; + Options only to admin roles; Reopen only to admins.
- Morning/Evening/Expense dropdowns always show the user's own open tasks (not the ALL view).
- Attachment size checked before upload; clear message if report saved but attachment failed.
- New options appear immediately without refresh.
