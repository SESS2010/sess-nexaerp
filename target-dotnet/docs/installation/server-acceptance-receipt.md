# Server-agent acceptance receipt (do not mark unwitnessed boxes complete)

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.

These rules apply to every linked deployment, identity, certificate and backup procedure. Stop and report a conflict; do not reclaim ports or memory from protected services.

Package HEAD/MANIFEST SHA256: ____________________   Operator/date: _______________
Frontend production SHA: ____________________   Server: DESKTOP-SPF5420

| Check | Evidence to record | Result |
|---|---|---|
| Package | Every payload hash matches, no extras, separate manifest digest | Pending |
| Prepared host | C:\SESS-ServerPrep record referenced, no repeat preparation | Pending |
| Protected services | Before/after inventory, engineering owner acceptance | Pending |
| Ports | ERP 8443, Keycloak 8444, management 127.0.0.1:9000; no owner displaced | Pending |
| PostgreSQL |17.11 / NetworkService / C: data / local-only pg_hba / no 5432 firewall| Pending |
| Capacity | Source sizes, measured run budgets, C: floor25 GiB, RAM load peak | Pending |
| DEMO schema | Exact DEMO name,130 migrations/head, RECONCILED and VERIFIED | Pending |
| Keycloak | Exact pinned ZIP/wrapper checksums, Java 17 path, own sess_keycloak DB | Pending |
| Realms | Imported exact clients/URLs, mandatory password+OTP flow | Pending |
| HTTPS | Issued leaf serials/expiry, root fingerprint, separate key ACLs | Pending |
| Eleven PCs | PC names/browser/date, BOTH origins trusted without warnings | Pending |
| SESS-12 | Verified Staff sub, one-time ceremony receipt, both company sessions | Pending |
| Approvers | SESS-01/02/14 each wrong/missing OTP refused, correct OTP accepted | Pending |
| Mappings | Approved employee/realm/sub/company list, history/audit receipts | Pending |
| Frontend | Production PKCE/login/logout/refresh; no dev endpoints/token storage | Pending |
| Refusals |401 missing/expired token;403 unmapped/company/MFA/permission cases| Pending |
| DEMO walk | Agreed business walk and permission separation | Pending |
| Reboot | No login; PostgreSQL/ERP/Keycloak start and health/login work | Pending |
| Local backups | ERP AND identity VERIFIED, field cluster identity, serial restore | Pending |
| Off-machine | Accepted receiver/account/share, file SHA256 match, fresh daily receipt | Pending |
| Receiver vault | Archive receipt, sender cannot access vault, at-rest encryption | Pending |
| Recovery | Copied ERP AND identity restore verification; source unavailable rehearsal | Pending |
| Backup scheduling | Signed-out tasks and receiver availability, freshness checks | Pending |
| DEMO deletion | Only exact DEMO dropped, Keycloak/protected DBs untouched | Pending |
| Fresh production | Option C built on server, exact database name, principal checks | Pending |
| Opening gate | BOTH authorised ceremonies posted, receipts, NO earlier test GRN | Pending |
| User admission | Ordinary access enabled only after both opening receipts | Pending |

No passwords, bearer tokens, TOTP seeds or certificate private keys in this receipt.
A pending/failing required check prevents go-live. No owner DB action from the laptop.
Report RESULT_REPORTED_PENDING_WITNESS while required field evidence remains pending.
