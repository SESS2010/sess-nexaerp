# SESS Service Engineer Hub – Complete Guide (Version 2.6.0)

## 1. What is in this package
| Folder | Contents | Use |
|---|---|---|
| 1_App_Code | Code.gs, Index.html, appsscript.json | Paste into Apps Script (every upgrade) |
| 2_Backend_Excel | SESS_Service_Engineer_Backend_V2_6_With_Legacy_Data.xlsx | **New installation only** (all 22 sheets, legacy data, settings, roles) |
| 3_Guides | This guide (PDF + MD), page access by role, data model (359 columns), change history | Reading / ERP team |

**Important:** your app is already running on the Google Sheet *SESS_Service_Engineer_Backend_V2_3_With_Legacy_Data*. **Do not upload the Excel again** – that would replace the live data entered since then. For an upgrade you only change the code and run `setup`; `setup` adds every new column and setting automatically without touching existing data.

## 2. Google Drive folders (one time)
Create this structure in Google Drive and note each folder ID (the text after `/folders/` in the address bar):

| Drive folder | Put its ID in Settings sheet › Key | Purpose |
|---|---|---|
| SESS Service Hub / Reports | ATTACHMENT_FOLDER_ID | Photos / PDF report copies uploaded by engineers (already done: 1a5jIiI_vSpllkUvgnvg5bDlnGn97Dl7N) |
| SESS Service Hub / Backups | BACKUP_FOLDER_ID | Automatic nightly full copy of the backend (last 30 kept) |
| SESS Service Hub / Archives | ARCHIVE_FOLDER_ID | Year-end archive Google Sheet + Excel |
| SESS Service Hub / Packages | – | Keep every zip you receive (version history) |

To enter an ID: open the backend Google Sheet → ☰ (bottom left) → **Settings** → find the Key in column A → paste the ID in column B → Enter.

## 3A. Upgrade the running app (your case) – 10 minutes
1. Unzip the package on your PC.
2. Apps Script → ⚙ Project Settings → tick *Show "appsscript.json" manifest file in editor*.
3. Editor (<>): open **Code** → Ctrl+A → Delete → paste `1_App_Code/Code.gs` (open it with Notepad, Ctrl+A, Ctrl+C) → Ctrl+S.
4. Same for **Index** ← `Index.html`, and **appsscript.json** ← `appsscript.json`. Save each.
5. Delete any other .gs / .html files in the left panel (old copies cause wrong code to run).
6. Run **setup** (function list next to *Debug* → Run). If Google asks for permission → Review → your account → Advanced → Go to project → Allow.
7. Run **installDailyBackup** (once only). Log shows *Backup created … scheduled at 11 PM*.
8. Run **systemHealthCheck** → log must show `"version": "2.6.0"` and your sheet name. Read the `warnings` list and fix each (see section 10).
9. Settings sheet: check MORNING_DUE_TIME 09:30, EVENING_DUE_TIME 19:30, REPORT_GRACE_HOURS 24, LATE_ALLOW_DAYS 10, BACKUP_FOLDER_ID, ARCHIVE_FOLDER_ID.
10. Deploy › **Manage deployments** › ✏ Edit (the deployment whose URL you use) › Version: **New version** › Deploy.
11. Open the app link → **Ctrl+F5** → header shows **V2.6.0** and the left menu.

## 3B. New installation (only for a brand-new setup)
1. Upload `2_Backend_Excel/…V2_6….xlsx` to Drive → open → File › **Save as Google Sheets** → copy the ID between `/d/` and `/edit`.
2. Apps Script project → Project Settings › Script properties › **SPREADSHEET_ID** = that ID.
3. Do steps 2–8 of section 3A, then run **activateInitialAdmin** (makes you TD).
4. Create the Drive folders of section 2 and fill the Settings IDs.
5. Deploy › New deployment › Web app › Execute as **Me** › Who has access **Anyone with Google account** › Deploy › copy the /exec link.

## 4. Settings reference (Settings sheet)
| Key | Recommended | Meaning |
|---|---|---|
| MORNING_DUE_TIME | 09:30 | Morning report / task acceptance due |
| EVENING_DUE_TIME | 19:30 | Evening report due |
| REPORT_GRACE_HOURS | 24 | Within this after due = LATE; later = VERY LATE |
| LATE_ALLOW_DAYS | 10 | Employee can still submit up to 10 days late (marked); older → coordinator |
| PERF_WEIGHTS | 25,20,10,10,15,10,20 | Score: on-time completion, report timeliness, acceptance, rating, revenue vs expense, no missed, daily tasks |
| PLAN_UNITS | Nos,Sets,Panels,Metres,Kg,Hours,% | Units in Daily Plan |
| REGISTRATION_OPEN | TRUE | New employees can self-register |
| REGISTRATION_NOTIFY_EMAIL | your email | Mail for every new registration |
| ATTACHMENT_FOLDER_ID / BACKUP_FOLDER_ID / ARCHIVE_FOLDER_ID | folder IDs | See section 2 |

Change only column B (Value). Changes to the Settings sheet show in the app within 2 minutes.

## 5. Logins and employees
- **Link:** share only the app /exec link on WhatsApp. Never share the Google Sheet.
- **New employee:** opens link → *New employee* → name, email → *Send code* → enters code, mobile, department → chooses own PIN → Submit.
- **Approval (HR / Admin Manager / TD / MD):** Employees → New employee registrations → **Approve** → role, department, sub-department. If the person already exists (old record) choose **Link to existing** – keeps old Login ID and history.
- **Login:** email or Login ID + PIN. *Forgot PIN* tab → email code → new PIN.
- **Admin can also:** Employees → Edit (role, Active) → **Set PIN** (temporary; employee changes it at first login).
- **Roles:** Role Master decides portal and permissions (see PAGE_ACCESS). TD/MD always have everything.
- **Store team:** role STORE or sub-department STORE (receives material requests).

## 6. Daily use
**Manager (📝 Daily Plan) – morning**
1. Date → tick one or many employees → **Add tasks** → add lines (or 📋 paste many). Each line: Factory / Service / Office, task, ticket (optional), **materials to take**, target, hours, priority.
2. **⤵ Bring unfinished from previous day** for multi-day tickets. Open tickets appear under each person – tap **+ ticket** to continue today.
3. **↪ Reassign** to move a task (and its ticket) to another engineer with a reason.

**Employee (☀ My Day) – mobile**
1. Morning: read each task card (ticket, customer, end user, mobile, email, machine make/model/serial, complaint, materials) → **Accept all & start** (= morning report, time stamped).
2. **📦 Request from store** if material is needed.
3. Evening: each task ✔ Completed / ◐ Partly / ✖ Not done, qty, hours, reason → **+ Additional work** → **Submit evening update** (= evening report).

**Coordinator / Admin (🗒 Daily Reports)**
- *Reports missing* shows who has not sent morning/evening → WhatsApp them or **Enter morning / Enter evening** for them (marked P with your name and time).

**Service Manager** – ➕ Register Work (service complaint), ⏰ Open & Overdue each morning (To close → Verify & Close; Overdue → Re-plan with reason / Allocate / Close).
**Accounts** – 💳 Expenses (verify / reject), 💰 Revenue (invoice against ticket; another person verifies).
**TD / MD / HR** – 🏆 Performance, 📅 Report Calendar, 🌳 Task Ledger, ✅ Data Quality.
**Every table:** 🔍 Filter rows, ⚙ Columns (show/hide, remembered on each phone/PC), Reset. Header 🔄 switches Auto / 📱 Mobile / 🖥 PC view.

## 7. Report timing and performance
| Mark | Meaning |
|---|---|
| ✔ On time | By 09:30 (morning) / 19:30 (evening) on the work day |
| L Late | Within 24 h after due (next day) |
| ! Very late | Later than that (e.g. one week) – allowed up to 10 days, clearly marked |
| P Coordinator | Entered by coordinator/admin |
| ✖ Missed | Nothing sent |

Performance score (0–100) uses on-time completion, report timing, acceptance, customer rating, revenue vs expense, no missed reports and daily task completion. Each employee sees only their own score; managers see everyone.

## 8. Year end (after 31 March, TD / MD)
1. 🗄 Year-End Archive → **Preview** the finished year.
2. **Create archive in Drive** → open the Google Sheet and Excel links and check. (Nothing removed yet.)
3. **Remove from app** → type `DELETE 2026-27`. A full safety copy is saved first; only archived rows are removed; open complaints and all masters stay.

## 9. Safety rules
- Never edit column headers or *_ID* columns in the Google Sheet; add/change data through the app (audit trail).
- Never upload the Excel again over a running system.
- Backups: nightly copy in the Backups folder + Google Sheets *File › Version history*.
- Keep every package zip in Drive › Packages.
- Before any upgrade: run **dailyBackup** once (manual copy) – then paste new code.

## 10. Troubleshooting
| Problem | Fix |
|---|---|
| Header still shows old version | Old file still there (step 3A-5) or no *New version* deployed (3A-10); Ctrl+F5 |
| "Not logged in / Invalid Login ID or PIN" | Read the exact message: typo in saved email, waiting approval, PIN not set, wrong PIN |
| Attachment upload fails | ATTACHMENT_FOLDER_ID empty or wrong |
| systemHealthCheck: no backup | Run installDailyBackup once |
| systemHealthCheck: no active TD/MD | Run activateInitialAdmin (only if you are not yet TD) |
| "An active TD/MD already exists" | Normal – you are already TD |
| Something wrong after upgrade | Deploy › Manage deployments › Edit › choose the previous version number → Deploy (instant rollback); send the screenshot |
| Banner "created by a Google Apps Script user" | Added by Google for Gmail-owned apps; tap ✕ (removable only with Google Workspace) |
