# SESS Sales CRM Web App — Setup Guide

1. Upload `SESS_Sales_CRM_2020-2027_Live_Template.xlsx` to the Sales folder, then choose **Open with Google Sheets → File → Save as Google Sheets**. Loose `.gs` and `.html` files stored in Drive do not run by themselves.
2. Open **Extensions → Apps Script**.
3. Replace `Code.gs` with the supplied `Code.gs` content.
4. Add an HTML file named `Index` and paste `Index.html`.
5. In Project Settings, enable display of `appsscript.json`, then replace it with the supplied manifest. If your Google account does not permit domain-only deployment, change the deployment access setting manually.
6. Run `setupSystem()` once from the Apps Script editor and authorize it.
7. In the new `Users` sheet, fill the real Google-account email for each listed employee. Use role `Admin`, `Manager`, `Sales`, or `Viewer`, and keep `TRUE` under Active.
8. Choose **Deploy → New deployment → Web app**. Execute as **User accessing the web app** and restrict access to your organization/team.
9. Share the deployed web-app URL with the sales team. Keep direct Sheet editor access limited to administrators.

## Email, 10-day reminder and automatic stage update

1. Open the deployed CRM web app as Admin/Manager and switch **Master email system** ON. This is the emergency master switch; each lead also has its own **Auto Reminder for this Lead** Enabled/Disabled setting.
2. Click **Install 10-day reminder** once. The checker runs daily near **09:30 India time**, sends only when 10 days have passed since that offer's last successful reminder, and never sends on Sunday. If the due date is Sunday, it is processed on the next eligible day.
3. Click **Enable reply auto-stage** once. Authorize Gmail access when Google asks.
4. The **Send from Gmail** list shows only the signed-in Gmail account and its verified Gmail “Send mail as” aliases.
5. Emails sent from the lead ledger are logged in `Communications`. Customer replies are checked every two hours and added to the same ledger.
6. Clear phrases such as PO/order confirmation, cancellation, negotiation, technical evaluation, final approval, under review and on hold update the stage automatically. Unclear replies are retained as `MANUAL REVIEW REQUIRED` without changing stage.

Reminder eligibility is controlled per lead: customer email must exist, Auto Reminder must be Enabled, and the offer must fall within its selected Current FY / Last 6 Months / Last 12 Months scope. Every non-closed stage remains eligible. Won, Lost, Closed, Cancelled, completed/delivered and superseded revisions are skipped.

V3.1 also supports **Last 12 Months**. `Next Follow-up` is an internal planning date only and never delays or suppresses automatic reminders. The first reminder becomes due 10 days after the offer/revision date; later reminders are due 10 days after the last successfully sent automatic reminder. Superseded revisions are skipped.

## Required upgrade step

After replacing `Code.gs` and `Index.html` with this version, run `setupSystem()` again. It safely adds the new columns/sheets (`Line Items`, `Communications`, `Custom Options`, `Offer Revisions`, `Company Master`, `Terms & Conditions`) without deleting existing offers. Then create a **new deployment version**; editing code does not automatically update an old web-app deployment.

## Logo, company and quotation

Upload `SESS_logo.png` to Drive, copy its file ID, and add a Script Property named `COMPANY_LOGO_FILE_ID` with that value. The quotation print view uses the selected company, GSTIN, address, logo, line items, basic value, GST and total. Verify the company master and terms before production use.

## Edit versus revised offer

- **Edit / Correct Same Offer** keeps the same offer number and original offer date locked, and writes before/after audit data with a mandatory reason.
- **Create Revised Offer** creates `-R01`, `-R02`, etc., requires a new revision date, preserves the parent, and marks the previous revision as superseded.
- Use **Update Stage / Remark** for phone, WhatsApp, meeting or email confirmations; source and remark are retained in communication history.

## Editable terms and conditions

Standard terms load automatically for a new offer. Use **+ Add term / condition** to add any number of clauses. Every title and condition can be edited, and clauses can be removed. Payment, warranty, validity and bank details are therefore offer-specific. On save, the CRM stores an independent terms snapshot in `Offer Terms`; later changes to master defaults do not silently alter an already-saved quotation. A correction updates the same offer's terms, while a revision copies the parent terms and allows further changes.

## Number allocation and historical data

Opening the portal does not allocate an offer number. `saveLead()` first validates every mandatory field, then obtains a document lock, allocates the next FY sequence, and appends the row in the same critical section. Therefore, with simultaneous users, the first valid save gets the first number.

V3.2 uses one common numbering series for Sales, Service and Spares:

- Normal offer: `SESS/OFR/0941/2026-27`
- Revision: `SESS/OFR/0941/R01/2026-27`
- Each new financial year starts at `0001`.
- During upgrade, `setupSystem()` scans the existing FY ledger and continues from the highest recognizable legacy or current running number.

Do not create separate SPR/SCR sequences. Business category remains in `Business Type` and `Department`, so reporting stays separate without fragmenting the controlled offer-number sequence.

## Current ledger

The **Current Ledger** page loads without a search term and defaults to the current financial year. Users can switch to Last 3 Months, Last 6 Months or Last 12 Months. Selecting **View** opens the same controlled offer record with Edit/Correction, Create Revision, Stage/Remark, Print and Communication actions. Search/Edit remains available separately for older or specific records.

The delivered workbook already consolidates the supplied registers from 2020-21 through 2026-27. Use History Upload only for a genuinely new archive. Existing Offer IDs are skipped.

## Performance design

- One normalized `All Leads` table replaces numerous detail tabs.
- New records and CSV imports are written with a single batch operation.
- Dashboard aggregation is cached for five minutes and invalidated after changes.
- No installable `onEdit` trigger and no thousands of volatile full-column formulas.
- Search output is capped at 200 recent matches.

## Backup

Keep a monthly XLSX export and limit Apps Script editor access to administrators.
