# DC Tracker – Delivery Challan Follow-up System (Rev 2)

**Rev 2 adds:**
- **Two companies:** SPVT = SRI EASWARI SCIENTIFIC SOLUTION PVT LTD, GSTIN 33ABACS5491H1ZA. SESS = SRI EASWARI SCIENTIFIC SOLUTION (proprietorship), GSTIN 33APRPA5532K1ZU. Each has its own number series and print header.
- **Bulk import / export** with an Excel template: `DC-Tracker-Import-Template.xlsx`, or download it in the app.
- **Editable Terms & Conditions** (Rule 55 CGST Rules / Sec. 143) and Original / Duplicate / Triplicate copies before printing.
- **Daily reminders until each DC is closed**, copied to the employee's reporting manager, plus one daily copy to `COMPANY_COPY_EMAIL` (<REDACTED-EMAIL>).

**Upgrading from Rev 1:** replace the code files (paste `Reports.gs` into your `Report.gs` and `Jobs.gs` into your `Job.gs`), run `setup` once, then Deploy → Manage deployments → New version. Full steps are in INSTALL-GUIDE.html, section "Upgrade from Rev 1 to Rev 2".

Google Apps Script web app. The front end is HTML, the data lives in a Google Sheet, and scanned copies are stored in Google Drive (one folder per DC).
Built for SRI EASWARI SCIENTIFIC SOLUTION PVT LTD. It works for any manufacturing or service company that sends material out on DCs.

## Files

| File | Paste into Apps Script as | What it does |
|---|---|---|
| `Code.gs` | Script file **Code** | Setup, login, database, DC create / edit / return / hardcopy / billing / close rules |
| `Reports.gs` | Script file **Reports** | Dashboard numbers, ISO audit report, export to Google Sheet |
| `Jobs.gs` | Script file **Jobs** | Daily reminder e-mails and import of the old Excel register |
| `Print.gs` | Script file **Print** | DC print / PDF in the Tally "Delivery Note" layout |
| `Index.html`, `Css.html`, `App.html` | HTML files **Index**, **Css**, **App** | The web app screens |
| `appsscript.json` | Project manifest | Time zone (Asia/Kolkata), permissions, web-app access |

## Setup (about 15 minutes)

1. Go to **drive.google.com** and sign in with the company Google account that should own the data.
2. Create a new Google Sheet named **DC Tracker – Database**. Open **Extensions → Apps Script**.
3. In the Apps Script editor:
   - Replace everything in the default `Code.gs` with the provided `Code.gs`.
   - **+ → Script**: create `Reports`, `Jobs`, `Print` and paste each file.
   - **+ → HTML**: create `Index`, `Css`, `App` (no `.html` in the name) and paste each file.
   - **Project Settings (gear) → tick "Show appsscript.json manifest file"**. Then open `appsscript.json` and replace its content with the one provided.
4. Select the function **`setup`** in the toolbar and click **Run**. Approve the permissions (Sheets, Drive, Gmail send, triggers).
   `setup()` does the following:
   - creates all sheets (DC, DCItems, Movements, Documents, Extensions, Users, Parties, Items, Settings, Counters, AuditLog)
   - creates the Drive folder **DC Tracker – Documents**
   - sets the DC counters so the next numbers are **RDC/SPVT/220/26-27** and **NRDC/SPVT/068/26-27**
   - creates the admin login and logins for every employee in your register's MASTER sheet
   - installs the daily 9 AM reminder trigger
5. **Deploy → New deployment → type: Web app**
   - Execute as: **Me**
   - Who has access: **Anyone** (engineers log in with the app's own login ID and password, so they don't need Google accounts)
   - Copy the **Web app URL** and share it with the team. Engineers can add it to their phone home screen.
6. Open the URL and sign in as **admin / <REDACTED>**. You'll be asked to set a new password.
7. In **Settings**:
   - fill in `STORE_EMAIL`, `ACCOUNTS_EMAIL` and `ADMIN_EMAIL`
   - check the company details and counters
8. In **Users & Logins**:
   - add each engineer's e-mail, so they get reminders
   - add their reporting manager's e-mail, so overdue DCs are escalated
   - deactivate anyone not needed

### Logins created by setup

| Role | Login ID (pattern) | First password |
|---|---|---|
| Admin | `admin` | `<REDACTED>` |
| MD / TD | `md`, `td` | `<REDACTED>` |
| Purchase / Accounts | `purchase.team`, `accounts.team` | `<REDACTED>` |
| Service Manager / Production Manager | `service.manager`, `production.manager` | `<REDACTED>` |
| Store (E Priya, L Kamali, Sudalai) | `e.priya`, `l.kamali`, `sudalai` | `<REDACTED>` |
| Employees (T Dinesh, B Prakasam …) | `t.dinesh`, `b.prakasam`, `s.manikandan.sr`, `mohan.sir` … | `<REDACTED>` |

Roles:
- **Admin**: everything.
- **MD / TD**: all DCs, reports, audit log; approve extensions.
- **Store**: create DCs, verify returns, confirm hardcopy, close.
- **Purchase**: vendor, job-work and warranty DCs; follow up vendor returns.
- **Accounts**: invoices.
- **Service / Production Manager**: all DCs; approve extensions; daily digest of their team's overdue DCs, linked through each employee's *Reporting manager e-mail*.
- **Employee**: own DCs only.
- **Viewer**: read-only.

See INSTALL-GUIDE.html for full details.

Every user must change the first password at first login. The admin can reset passwords from **Users & Logins**.

### Database Excel (included): `DC-Tracker-Database-Rev1.xlsx`

This file has every app sheet already filled with your old register: 303 DCs, 832 item lines and 145 parties. Upload it to Drive, **File → Save as Google Sheets**, then use that sheet in setup step 2 instead of a blank sheet. You then don't need `importLegacy`. It also serves as the Excel backup of the converted register.

### Import the old register (only if you started from a blank sheet)

1. Upload `DC REGISTER SPVT 2026-2027.xlsx` to Drive, open it, then **File → Save as Google Sheets**.
2. Copy that sheet's ID (the long part of the URL between `/d/` and `/edit`) into Settings → `LEGACY_SHEET_ID`.
3. In the Apps Script editor, run **`importLegacy`**.
   - Rows with the same DC No and date are merged into one DC with several item lines. Your register has 450 RDC rows (196 DCs) and 382 NRDC rows (107 DCs).
   - The register often has **`=TODAY()`** as the RDC due date. These are imported as *no due date* and flagged red on the dashboard, so you can set real due dates.
   - Safe to run again: DCs already imported are skipped.
   - After import, use the dashboard's **"(not assigned)"** row to find DCs with no responsible person, and edit them.

## How each DC type closes

| Type | Number | Due date | Stays open until |
|---|---|---|---|
| **RDC** – Returnable | `RDC/SPVT/220/26-27` | **Compulsory** | Every item is returned / consumed / written off, **and** store confirms the signed hardcopy is received. If chargeable, the invoice must be entered too. |
| **NRDC** – Non-returnable | `NRDC/SPVT/068/26-27` | – | Accounts enters the **invoice number** (or marks it *not billable* with a reason). Daily reminder to accounts after `NRDC_BILL_REMIND_DAYS`. |
| **WDC** – Warranty | `WDC/SPVT/001/26-27` | **Compulsory** | Same as RDC, plus vendor / OEM, claim / RMA no, serial no, fault and outcome (repaired / replaced / credit note). Kept separate in **Warranty DCs**. |

Store can always **close manually** with remarks, e.g. when lost material is charged. Admin can **cancel** or **reopen** a DC with a reason. DCs are never deleted.

## Engineer portal (proof of submission)

Each engineer sees only DCs issued in their name (**My DCs**). From a phone they can:
- **Upload the signed copy / proof** – photo or PDF of the customer-signed DC, gate entry or end-user acknowledgement, with gate entry no, date and end-user name. Photos are compressed before upload and saved in that DC's Drive folder.
- **Return / consumption** – enter qty *returned to store*, *consumed at site* and *lost / damaged* per item, with date and condition. Each submission gets an **acknowledgement number and timestamp**. It can be printed, and a copy is e-mailed. Store then accepts or rejects it with a reason.
- **I handed over hardcopy** – records the date the signed hardcopy was given to store. Store confirms receipt.
- **Request extension** – a new due date with reason. Store approves or rejects.

Whatever store does afterwards, the system keeps the engineer's submission date, so an engineer can show they submitted on time.

## Store desk, billing, dashboard, ISO report

- **Store Desk** – returns to verify, hardcopies to confirm, extension requests, and DCs where material is back but the hardcopy isn't.
- **Billing** – every DC waiting for an invoice. Enter the invoice no inline, or mark it not billable.
- **Dashboard**
  - Live counts: open, overdue, due soon, awaiting invoice, hardcopy pending, returns to verify, signed copy missing, no due date, GST job-work alerts.
  - Period figures: issued, closed, closure %, on-time %.
  - Charts: status by type, overdue ageing, monthly trend, consumable vs non-consumable movement.
  - **Employee accountability table**: who has how many open / overdue DCs, pending hardcopies and unverified returns.
  - Every number is clickable and opens the matching list.
- **ISO Audit Report** – for ISO 9001 §7.5 documented information, §8.5.2 traceability and §8.5.3 property of customers / external providers.
  - Summary by type: closure %, on-time return %, average days to close and to bill, hardcopy %, signed-copy %.
  - Employee-wise compliance %.
  - Material category movement.
  - A **non-conformity list** for CAPA.
  - Printable with *Prepared / Reviewed / Approved* signature lines. CSV export available.
- **Audit Log** – every create, edit (with old → new values), upload, verification, extension and closure, with user and time.
- **Export** – any register view exports to a new Google Sheet (in `DC Tracker – Documents/Reports`) or downloads as CSV.

## Features added beyond the request (common gaps in manufacturing / service DC control)

- **Item category on every line** – Consumable / Non-consumable / Tool & equipment / Spare part / Job-work material. This drives the consumable analysis.
- **Consumed vs returned vs lost** tracked separately, with remarks compulsory for lost / damaged.
- **Due-date extension workflow** – the original due date is kept for audit, and the number of extensions is counted per DC.
- **Escalation** – overdue beyond `ESCALATE_AFTER_DAYS` copies the engineer's reporting manager.
- **Due-soon alerts** before the due date, not only after.
- **GST job-work alert** (Sec. 143) – job-work material out close to 1 year is flagged. After 1 year it is treated as a supply.
- **Vehicle no and e-way bill no** fields – an e-way bill is needed above ₹50,000 even on a DC.
- **Chargeable RDC** – consumables used on a returnable DC can be marked chargeable and then need an invoice before closing.
- **Manual DC number** – you can type the Tally number when a DC was made in Tally. Duplicates are blocked.
- **One Drive folder per DC** (`FY 26-27 / RDC / RDC-SPVT-220-26-27`) holding the DC PDF and all scans.
- Party and item masters auto-fill address, GSTIN, HSN, unit and category.
- Login lock-out after 5 wrong passwords (15 minutes). Sessions expire after 6 hours of inactivity.

## Daily e-mails (09:00)

- Each engineer gets one digest of their overdue, due-soon, hardcopy-pending and signed-copy-missing DCs.
- Store gets pending verifications, hardcopies to confirm, extension requests and the overdue list.
- Accounts gets the list of DCs waiting for an invoice.
- Management gets a summary with the employees who have the most overdue DCs.

Switch off with `SEND_EMAILS = NO`. Admin → Settings → **Send reminders now** sends them immediately.

## Updating the app later

After pasting new code: **Deploy → Manage deployments → ✏ Edit → Version: New version → Deploy**. The URL stays the same.

## Limits to know

- Gmail accounts can send ~100 e-mails/day from Apps Script (Workspace: 1,500). One digest per engineer keeps this low.
- Uploads: images or PDF, up to 10 MB each.
- `FILE_LINK_SHARING = YES` makes each uploaded scan viewable by anyone who has its link. This lets engineers without Google accounts open their files. Set it to `NO` to keep files private to the owner account.
- The Google Sheet is the database. Don't rename its tabs or header cells. Adding columns at the far right is fine.
