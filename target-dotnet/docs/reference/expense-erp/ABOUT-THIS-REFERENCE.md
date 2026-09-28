# SESS Combined Expense ERP V4.1: reference copy

A Google Apps Script web app built by SESS staff for employee expenses. It covers:
- travel claims with map KM control, and the expense register with a policy engine;
- employee advances and ledger settlement, and petty cash;
- a company cash-flow planner.

The Technical Director made it the reference for **Track C** (master requirements
`docs/requirements/SESS-NexaERP-Master-Requirements-v2.md` §2.C, §5.3).

It is **documentation only**. It is not built, run or deployed from this repository. NexaERP ports
its rules, not its mechanics (sheets, OTP e-mail logins, Drive evidence URLs).

**Source:** `C:\Users\User\Documents\Codex\audit\expense-erp\`, copied on 27 September 2026.
The hashes are of the originals, and "none" means byte-identical.

| File | SHA-256 of the original | Redacted |
|---|---|---|
| `appsscript.json` | `80392B9EDB228FA86F1BB974919EE8D73659E0F6D08AC5795F4EB11667EC224B` | none |
| `Auth.gs` | `F8523CCC74A2AB26C2D91BE610EF75F4AED0D4829017C38C7DEDB49BF53CA5B3` | none |
| `Code.gs` | `099DC9BEFF7514C93579F2C7F71CD3A4416C9B5619F9982D62E3AD716143C63F` | 7 e-mail addresses, 2 personal names (the seeded user register) |
| `Index.html` | `F44CF43368F8937D524525E0594B846DFEA773D744D9EF2CA13B5F6FE8D02DA4` | none |
| `MapsV41.gs` | `337AC4DB368BA03F619A02E1782019E20DEF8529461A54694940104D7FCA1A3D` | none |
| `PolicyV41.gs` | `9055925D342AFA03B50CA8F08D53629DAEE53421B6225FF0A20FC450A6F5A1D0` | none |
| `README.txt` | `7DC574B8328BDC6C48324FA851521C2B24982616EF37BDAEFF9FBFD35AB78D9D` | none |
| `SESS_ERP_V4.1_Ten_Audit_Report.txt` | `E038E64D238061AF2282194E53D106CB930A94B03B82BB66AC2F393C78C7F071` | none |
| `SESS_Combined_Expense_ERP_V4.1_Installation_Guide.docx` | `D08E08F14FFE060E3070A55F1E2A77B6398EC882DE28CEB2F603A78F4F64EDE9` | converted to INSTALLATION-GUIDE.md; 7 e-mail addresses (the authorised register table) |

**Not included, on purpose:**
- the base workbooks (`SESS_Combined_Expense_ERP_V4.1_Base*.xlsx`) and the employee user
  registers (`SESS_V4.1_Employee_User_Register*.xlsx`), which hold employee and expense data;
- the PDF copy of the installation guide.

The Google Maps API key is not in the code; the app keeps it in Script Properties.