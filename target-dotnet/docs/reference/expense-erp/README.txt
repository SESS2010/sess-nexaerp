SESS Combined Expense ERP V4.1 - Production Upgrade

FILES TO COPY INTO THE EXISTING APPS SCRIPT PROJECT
1. Code.gs (replace)
2. Auth.gs (replace)
3. Index.html (replace)
4. MapsV41.gs (add as a new script file)
5. PolicyV41.gs (add as a new script file)
6. appsscript.json (replace after enabling Show appsscript.json in Project Settings)

ONE-TIME UPGRADE
1. Keep a copy of the existing V4 spreadsheet before changing code.
2. Run setupSystemV41() once. Authorise the requested permissions.
3. Google Cloud: enable billing, Places API (New), and Routes API.
4. Create a separate API key restricted to only Places API (New) and Routes API.
5. Run setupGoogleMapsApiKeyV41() once and paste that key.
6. Use Deploy > Manage deployments > Edit > New version > Deploy. This keeps the existing web-app URL.
7. Test with one Employee, Accounts and MD login before sending the URL to all employees.

ROLE FLOW
- EMPLOYEE: submit and view only own records.
- ACCOUNTS: verify documents and recommend KM/amount; cannot final approve.
- MD: final approval after Accounts verification; final KM x rate becomes approved expense.
- ADMIN: setup and emergency administration.

TRAVEL CONTROL
- Every route location must be selected from live Google Places suggestions.
- The server recomputes all route legs and the total Google KM before saving.
- Employee Claimed KM and odometer KM remain separate evidence.
- Outside +/-10% of Google KM, or an odometer mismatch above 1 KM, requires a detailed reason.
- Accounts recommendation defaults to the lower of Employee KM and Google KM.
- MD can approve a different KM only within the employee claim; above +/-10% requires remarks.
- If excess KM is not accepted, keep/approve the Google-based recommendation.

EXPENSE AND PETTY CASH POLICY
- Food daily allowance: Standard ₹250, Senior ₹300, Superior ₹350. Set Expense Grade in Employee Master.
- Single lodging limit ₹800; double lodging limit ₹1500 with People Count = 2.
- Food, lodging, bus/train and petty purchase require bill/ticket Drive URL evidence.
- Non-GST petty cash is capped at ₹500 per employee per day.
- Petty purchase above ₹500 requires GST bill evidence; ₹5000 or above is blocked and must use Purchase/PO process.
- More than one evidence URL can be entered, one URL per line.
- Cash Flow Planner records GST, PF, ESI, rent, EB, insurance, salary, loan, TDS and vendor payment requirements.

SECURITY
- The Maps key is stored only in Script Properties; it is never sent to the browser.
- Only active Role Master emails receive OTP. Gmail, Yahoo, Outlook and company email IDs work.
- Employees cannot request or view another employee's data; enforcement is server-side.
- Accounts cannot use the MD approval endpoint.
- Keep the spreadsheet private; employees receive only the web-app URL.
- Prefer individual employee emails. Shared mailboxes reduce operator accountability.

BACKUP AND CAPACITY
- Use Full XLSX Backup from the dashboard regularly and save it on the company PC/Drive.
- With 60 employees x 20 lines/day, about 438,000 rows are created per year. Do not put three peak years into one Sheet.
- Keep one active quarter and separate read-only quarterly/FY archives; the employee portal remains the single entry point for the active period.
- Keep at least three financial years of archive files and maintain Archive Index. Use date/employee filters for normal reports.

IMPORTANT
- setupSystemV41() is safe to rerun: it adds missing V4.1 columns without deleting existing rows.
- Never paste the API key into Index.html or share it with employees.
