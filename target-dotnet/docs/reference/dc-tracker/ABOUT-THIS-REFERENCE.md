# DC Tracker Rev 2: reference copy

This is the Delivery Challan tracker that SESS's admin team built as a Google Apps Script web app
backed by a Google Sheet. The Technical Director made it the **reference for how DC tracking,
follow-up and reminders must work in NexaERP** (plan of 26 September 2026, items T2 and T7).

It is kept here as **documentation only**. It is not built, run or deployed from this repository,
and nothing in NexaERP depends on it.

**Source:** `C:\Users\User\OneDrive\Desktop\claude\DC-Tracker-Rev2\DC-Tracker-Rev2`, copied on
26 September 2026. The copy in `C:\Users\User\Documents\Codex\audit\dc-tracker\` is identical.

## What is here, and what is not

**Included:** the code, the README and (from 27 September 2026) the redacted install guide. SHA-256 values below are of the **original** files.

| File | SHA-256 of the original | Passwords redacted | Personal e-mails redacted |
|---|---|---|---|
| `Code.gs` | `2AA8AA8104D9ED41F5CC40496F660D10102967D610B4DC34094D0AE29490E7A1` | 2 | 3 |
| `Jobs.gs` | `577D80EB577B0E8B9BF432172A7BCB045E911E627F41171818A9D5FDBB38DFBE` | 0 | 0 |
| `Reports.gs` | `079C988FC1ABE7B6A0C029B6591F28D9866DA204963D53E378BB94E9D5E40A43` | 0 | 0 |
| `Print.gs` | `7A606804D58A588A84E09D311436C806BFE7BA1606727540E695F1C12CF6E48E` | 0 | 0 |
| `App.html` | `7EC33650D367B93341A79E9C09F7F7BB42A91C2D5E4D5305828F20434A8FDC5A` | 0 | 0 |
| `Css.html` | `26B9087542F6899AEDD6BFAF098E622FED11CAD308E98BA9CE2C7226246F93FE` | 0 | 0 |
| `Index.html` | `5934205271BA1020CC74401841911BA4C017932CD907E44274FB7BCF4C1D1A9E` | 0 | 0 |
| `appsscript.json` | `BE0FE63E5BFA065EE94D9645EA730D4BBDB045CAD32569AE6A4606FB529BA40A` | 0 | 0 |
| `README.md` | `C0D98DD02BF66F06891FD6FA44EFA333BD64E93884F72BA2EC352B14531BBD8A` | 7 | 1 |
| `INSTALL-GUIDE.html` | `C2F2E7C345F1EF69CFE96CCD138D51D7B03A9C76B4427F70B679F3DB0C287CD8` | 5 | 5 |

**Redaction:**
- Every default password (the admin password and the seeded users' first password) is replaced by
  `<REDACTED>`.
- Every **personal e-mail address** is replaced by `<REDACTED-EMAIL>`. This was added on
  27 September 2026 at the TD's instruction, by amending this unpushed commit.
- Company role mailboxes (`@sess.co.in`) in the company seed are kept.

No other change was made, so these copies differ from the originals only on those lines.

**Deliberately left out:**

- `DC-Tracker-Database-Rev1.xlsx`: customer data (303 DCs, 832 lines, 145 parties).
- `DC-Tracker-Import-Template.xlsx`: a workbook, not code.

**Anyone deploying the tracker itself** must set their own passwords. The originals stay with the
admin team.
