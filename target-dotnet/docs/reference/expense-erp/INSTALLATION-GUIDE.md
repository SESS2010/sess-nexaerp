> Converted from SESS_Combined_Expense_ERP_V4.1_Installation_Guide.docx (SHA-256 D08E08F14FFE060E3070A55F1E2A77B6398EC882DE28CEB2F603A78F4F64EDE9) on 27 September 2026. Wording unchanged except redactions.

# SESS Combined Expense ERP V4.1

**Google Places, Route KM Control and Production Upgrade Guide**

Release: 20 September 2026 | Time zone: Asia/Kolkata

## 1. What V4.1 adds

- Live Google Places autocomplete for Start, intermediate stops and End. Employees must select a Google result, not free-type an unverified address.
- Google Routes API leg-by-leg distance and total route KM. The server recomputes the route before saving, so browser values cannot be trusted or altered.
- Separate Google KM, employee claimed KM and odometer KM; automatic variance percentage and ±10% classification.
- A detailed diversion reason is mandatory outside ±10% or when employee KM and odometer differ by more than 1 KM.
- Accounts recommends KM and amount. MD alone makes the final decision after Accounts verification; MD-approved KM × rate is the final expense.
- Dashboard KPIs for Google KM, employee claimed KM, MD-approved KM, exceptions and KM reduction, plus existing date/employee/financial-year filters.

## 2. Upgrade the existing V4 project

**1.** Download and unzip the V4.1 package. Before editing, make a copy of the current spreadsheet and use File > Version history > Name current version.

**2.** Open the existing V4 spreadsheet, then Extensions > Apps Script.

**3.** Replace Code.gs, Auth.gs and Index.html with the V4.1 files. Add new script files named MapsV41.gs and PolicyV41.gs and paste the supplied content.

**4.** In Project Settings enable “Show appsscript.json manifest file in editor”; replace it with the supplied appsscript.json. Save all files.

**5.** Select setupSystemV41 from the function list and click Run once. Accept permissions and wait for Execution completed. This adds missing V4.1 columns and Archive Index without deleting rows.

**6.** Complete Google Cloud/API setup in Section 3, then run setupGoogleMapsApiKeyV41 once and paste the restricted key.

**7.** Use Deploy > Manage deployments > select the current Web App > Edit > Version: New version > Deploy. This keeps the same employee URL.

**8.** Complete Section 7 tests using one Employee, Accounts and MD login before company-wide use.

## 3. Google Cloud setup (required)

**1.** Open Google Cloud Console and select or create the project used for this ERP. Attach an active billing account.

**2.** APIs & Services > Library: enable Places API (New) and Routes API. Do not rely on the older Places API only.

**3.** Credentials > Create credentials > API key. Under API restrictions choose Restrict key and select only Places API (New) and Routes API.

**4.** Because Apps Script makes the calls server-side, do not put the key in Index.html and do not use a browser HTTP-referrer restriction. Store it only through setupGoogleMapsApiKeyV41.

**5.** Set Google Cloud budget alerts and review API usage regularly. Google route/place calls may incur charges according to the Cloud account.

## 4. Travel entry behaviour

| **Step** | **Employee action** | **System control** |
|---|---|---|
| 1 | Select Start from suggestions | Stores Google Place ID |
| 2 | Add each customer/diversion as Stop | Builds ordered route legs |
| 3 | Select final End | Closes complete journey |
| 4 | Enter odometer and claimed KM | Keeps both as separate evidence |
| 5 | Calculate Google Route KM | Shows each leg and total |
| 6 | Enter reason if exception | Minimum detail enforced |
| 7 | Submit | Server calls Google again and saves authoritative route |

**Example:** SESS → Danfoss → Mando Automotive → SESS is calculated as three legs and added into one Google total. If the employee claims 160 KM against Google 120 KM, the claim can be submitted only with a detailed diversion reason and remains an exception for Accounts/MD review.

## 5. Approval and calculation rule

| **Stage** | **Allowed role** | **KM/amount rule** |
|---|---|---|
| Submitted | Employee | Claimed KM × configured rate |
| Verified | Accounts or Admin | Recommend 0 to employee claimed KM; default is lower of Google and employee KM |
| Final approval | MD or Admin | Approve 0 to employee claimed KM; outside ±10% requires remarks |
| Rejected / clarification | Current stage owner | Remarks mandatory; audit row retained |

- Accounts cannot final approve. MD cannot bypass Accounts verification.
- If excess KM is not accepted, Accounts/MD should keep the Google-based recommendation.
- Final travel expense equals MD Approved KM × Rate/KM. Employee claimed value is never overwritten.

## 6. Authorised register included

| **Email** | **Role** | **Use** |
|---|---|---|
| <REDACTED-EMAIL> | MD | Final approval and management dashboard |
| <REDACTED-EMAIL> | ADMIN | System administration |
| <REDACTED-EMAIL> | ACCOUNTS | Verification only; no final approval |
| <REDACTED-EMAIL> | EMPLOYEE | Own entries/ledger only |
| <REDACTED-EMAIL> | EMPLOYEE | Own entries/ledger only |
| <REDACTED-EMAIL> | EMPLOYEE | Own entries/ledger only |
| <REDACTED-EMAIL> | EMPLOYEE | Own entries/ledger only |
| info@sess.co.in | EMPLOYEE | Own entries/ledger only |

**Important:** Test OTP delivery for every mailbox. Add each individual employee email to Role Master with the matching Employee Master ID. Gmail, Yahoo, Outlook and company email addresses are supported when they can receive OTP.

## 7. Mandatory production acceptance tests

| **Test** | **Expected result** |
|---|---|
| Type 3+ letters in any route field | Live Google suggestions appear; selected full address is retained |
| Multi-stop route | Every leg and total KM display |
| 110 KM against Google 100 KM | Accepted as boundary within ±10% |
| More than ±10% with short/no reason | Submission blocked |
| More than ±10% with detailed reason | Saved as exception for review |
| Employee alters browser KM | Server recomputes Google route |
| Employee A login | Only Employee A submissions and ledger |
| Accounts login | May verify/recommend; cannot final approve |
| MD login | May approve only Accounts Verified item |
| Final travel calculation | MD Approved KM × configured Rate/KM |
| 1/3/6/12 month, FY, All Data | KPIs, charts and ledger follow filter |
| XLSX backup | Download opens successfully |

## 8. Expense policy petty cash and cash flow

| **Expense policy** | **System rule** |
|---|---|
| Food Standard | ₹250 per employee/day |
| Food Senior | ₹300 per employee/day |
| Food Superior | ₹350 per employee/day |
| Single lodging | ₹800; hotel bill mandatory |
| Double lodging | ₹1500 for exactly two people; hotel bill mandatory |
| Bus / Train | Ticket URL mandatory |
| Non-GST petty cash | Maximum ₹500 per employee/day |
| Site petty purchase | Above ₹500 requires GST bill and invoice number; ₹5000+ goes to Purchase/PO |

- Employee Master now includes Expense Grade. Keep Standard unless Accounts authorises Senior or Superior.
- Food, lodging, travel ticket and petty purchase accept multiple Google Drive evidence URLs, one per line. Exception claims retain the original amount and reason for Accounts/MD review.
- Cash Flow Planner books GST, PF, ESI, rent, EB, insurance, salary, loan, TDS, vendor payments and other monthly requirements with due date, paid status, reference and challan/bill URL.
- Dashboard shows petty cash, policy exceptions, missing evidence, planned cash need, paid cash, funding gap and unpaid obligations.

## 9. Backup performance and three year retention

- Use Full XLSX Backup monthly, before code/configuration changes, and at financial-year close. Save one copy on the company PC and one controlled Drive copy.
- 60 employees × 20 entries/day is about 438,000 rows/year. With policy and audit columns, even one full peak year can exceed a practical Google Sheet cell limit. Three years must not be dumped into one spreadsheet.
- Use one active quarter for detailed line entry and separate read-only quarterly or financial-year archive files. Record every file URL and period in Archive Index. This is one employee portal operationally, but separate storage files prevent a slow, oversized workbook.
- The dashboard returns at most 2,000 detailed ledger rows to the browser. Use employee/date filters and monthly summaries for normal work; open the correct archive only for old line-level audit.
- Do not add volatile whole-column formulas to transaction tabs. Portal calculations are server-side values; keep formulas only in controlled summary sheets.

## 10. Troubleshooting

| **Message / issue** | **Correction** |
|---|---|
| GOOGLE_MAPS_API_KEY_MISSING | Run setupGoogleMapsApiKeyV41 as owner. |
| Google Maps API 403 | Check billing, enabled APIs and API restrictions. |
| No suggestions | Enter 3+ letters; confirm Places API (New) is enabled. |
| Route not found | Select every point from suggestions and remove invalid stop. |
| AUTH_REQUIRED | Session expired; log in again with OTP. |
| Employee cannot login | Match Role Master email/ID; Active? must be Yes; test OTP inbox/spam. |
| Old UI still appears | Manage deployments > Edit > create/deploy New version, then hard refresh. |
| Slow report | Use a shorter date filter and archive older data. |

## 11. Go-live checklist

- Current V4 spreadsheet backed up and version named.
- All five source files plus manifest saved; setupSystemV41 completed.
- Places API (New), Routes API, billing and restricted key confirmed.
- Employee, Accounts and MD end-to-end route claim tested.
- KM boundary, exception reason and MD final amount checked.
- Employee isolation, audit trail and XLSX backup checked.
- Existing deployment updated to a new version and same URL verified.

**Release status:** Code and package passed ten offline audits. Final production activation still requires the live Google Cloud key, live route/API response, OTP delivery and the three-role acceptance tests above because those external services cannot be executed from the offline package.
