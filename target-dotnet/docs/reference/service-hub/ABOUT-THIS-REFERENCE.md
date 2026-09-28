# SESS Service Engineer Hub V2.6: reference copy

A Google Apps Script web app built by SESS staff for service, production and office teams:
- tickets, daily plans, morning and evening reports;
- expenses, revenue, feedback, performance, and the year-end archive.

The Technical Director made it a **reference for behaviour** in the tracking programme (requirements
file `docs/requirements/SESS-NexaERP-Tracking-Requirements-v1.md` §3). Its Stores and Purchase
integration points are in scope now; a full Service module is phase 2.

It is kept here as **documentation only**. It is not built, run or deployed from this repository.
NexaERP ports its rules, not its mechanics (sheets, triggers, PIN logins). Its hard delete and its
year-end removal of rows and audit log are deliberately **not** ported (requirements §5).

**Source:** `C:\Users\User\Documents\Codex\audit\service-hub\`, copied on 27 September 2026.

| File | SHA-256 of the original | Redacted |
|---|---|---|
| `00_READ_ME_FIRST.txt` | `30EEF6DE6BC41525F2A602AEAC28718929F567D667865699022AF3058A4CD943` | none |
| `appsscript.json` | `65FC4BA5A42F525FE16EBD68967B5C06A7E44972CD24FFB543BC59E00BCFAE7B` | none |
| `CHANGE_HISTORY.md` | `FC76629000177E84929013682E82FEE911D670E14CC60A8C63059E259B3371F1` | none |
| `Code.gs` | `460BCD985FF48522743EE0AEC6671906FB1A9D62BE2E8538110513BFE331FBFF` | the Google Sheet ID (`SPREADSHEET_ID`) |
| `COMPLETE_GUIDE_V2.6.md` | `DC496E89120F133294F0ED5252C1A32A29105D4CFA606E36A401AE161B96F784` | none |
| `DATA_MODEL.md` | `B7D4D719CB9C6DC0F30CC693ED5EBEA8CC413847C0FF356310921664F0B6B0E2` | none |
| `Index.html` | `06F4B9A193208111402676732D4924085DF9282B1964231F7EC7B4125E12F969` | 1 placeholder e-mail in the login hint |
| `PAGE_ACCESS.md` | `AEFF107EE8981A0DAB1E518E2472503815D0CF1393AFDD77A0165D9584E4B257` | none |

**Not included, on purpose:**
- the two backend workbooks (`SESS_Service_Engineer_Backend_V2_6_With_Legacy_Data*.xlsx`), which
  hold customer, employee and ticket data;
- `COMPLETE_GUIDE_V2.6.pdf`, the same guide as the `.md`.

The code holds no password: PINs are hashed with a salt and a pepper kept in Script Properties.
