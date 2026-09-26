SESS NexaERP - DESKTOP-SPF5420 deployment candidate
Backend/package source HEAD: {{HEAD}}
Frontend source commit: {{FRONTEND}} (built from the frontend developer branch)

NEVER TOUCH: SQL Server SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser and design
 databases; SOLIDWORKS Electrical/ewserver; any Rockwell service;
 IIS Default Web Site and /Updater; Wampserver (leave stopped/manual).
 D: and E: stay connected under the TD trial exception with daily disk-event watch,
 but are prohibited for ANY ERP use. Never clean-install Windows or install
 a .NET SDK. Do not stop protected services or reboot without an approved window.

START HERE: installer/docs/server-agent-demo-start.md, beginning with step 0.
That is the single initial server instruction. Only PostgreSQL 17.11, the .NET 10
ASP.NET Core runtime and server preparation are confirmed. Java 17 and Keycloak
must be verified/staged/installed as directed; do not assume Java is present.

READINESS: CANDIDATE, NOT CLEARED FOR FRONTEND DEMO OR GO-LIVE.
The initial handoff permits checked server preparation, DEMO API commissioning
and SESS-12 bootstrap only, then STOP and report. Named-employee wrapper rehearsal
comes later. The selected frontend still signs in through Debug-only /api/v1/dev
endpoints; Release excludes these. Do not serve web/ until production OIDC login
is integrated and witnessed. Never enable development authentication in Release.
No credentials, certificate private keys, Java or Keycloak binaries are included.
Third-party identity prerequisites are acquired separately per the handoff.
ERP API, Installer and migration bundle need .NET 10 ASP.NET Core runtime and
PostgreSQL 17; Node, a .NET SDK and EF tools are not required on the server.

api/       Release framework-dependent API; Windows service SESSNexaERP, HTTPS 8443.
web/       npm production-build static files; copy into installed api/wwwroot.
installer/ Release framework-dependent database-principals, bootstrap and backup CLI;
           tools/ backup schedule and package verification scripts; docs/ instructions.
migrate/   Framework-dependent win-x64 efbundle.exe; migration login uses
           Options=-c role=nexa_erp_owner. proof.json records SDK-free proof.
MANIFEST   Source SHA, UTC build time, readiness, migration head, size and SHA-256
           of every other file. A file cannot contain its own hash. Verify MANIFEST
           against the separately supplied <HEAD>.MANIFEST.sha256 before trusting it.

INITIAL HANDOFF ONLY - exact checks and commands are in server-agent-demo-start.md:
0. READ-ONLY ACL/group check for C:\, C:\Users, PostgreSQL data and C:\SESS-Deploy.
   Report to TD. Every later ACL change needs approval of exact paths/principals/rights.
1. Copy the package to C:; verify the independently supplied MANIFEST hash and every file.
2. D4: approved office CA and separate server certificates/keys for ERP 8443 and
   Keycloak 8444. The CA private key stays offline, never on the ERP server.
3. D2: verify/install dedicated Java 17; stage verified Keycloak/Commons Daemon;
   create its own database/account; configure its Windows service on HTTPS 8444.
4. D3: import Staff/Approvers at D2's offline-import point, including wrapper
   loopback callbacks; then complete D2 startup and discovery/health checks.
5. Create only sess_nexa_erp_DEMO; CREATE SCHEMA advance; provision principals;
   run efbundle.exe; require RECONCILED and VERIFIED. No developer dump.
6. Install/start the API Windows service on 8443 targeting DEMO; check trusted HTTPS
   and health. Do not serve the development-login frontend.
7. D5: TD signs the complete scope roster BEFORE any scope entry; bootstrap SESS-12
   using the named employee's verified identity. Enter scope once from that form.
   STOP AND REPORT. Missing named identity means bootstrap remains pending.

LATER PHASES - NOT AUTHORISED BY THE INITIAL HANDOFF:
After production frontend/OIDC integration, follow server-deployment.md for
another-office-PC checks, named-employee wrapper rehearsal, the demo walk and an
approved reboot/no-login witness. Only after accepted DEMO completion drop the DEMO
database, then create sess_nexa_erp directly on the server with Option C.
No stock-moving command before BOTH opening-stock ceremonies are posted.
Do not restore any demo/developer database. NO DUMP: items come from the checked-in
legacy script. Go-live moved to 8 October by governed decision on 23 September:
28-30 Sep deploy and prove the real login; 1-3 Oct SESS enters other setup using the
governed SETUP-BEFORE-FIRST-GRN checklist/fallbacks; 5-6 Oct both template-v2 opening
ceremonies; 7 Oct final checks; daily transactions start 8 October.

Configure and witness daily verified ERP/identity backups on C: PLUS DAILY off-machine copy;
   D: and E: are rejected because their physical HDD is suspect. Follow
   installer/docs/server-daily-backups.md, including the 25 GiB free-space gate.
   Receiver is Ilamparuthi's PC (identified as IT TEAM 2), NOT DESKTOP-AP. Replace RECEIVER_NOT_SET
   with its verified Windows hostname before setup; the example intentionally refuses.
   Use server-production-state.example.json with Test-ProductionState.ps1 every
   morning: stale/missing/failed daily off-machine receipts must produce FAIL.

Never touch SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, ewserver or their data;
NI and Siemens were reported removed; do not reinstall. Rockwell services stay; preserve IIS Default Web Site and
/Updater; Wamp remains stopped/manual. Reference C:\SESS-ServerPrep; no repeated preparation. Never clean-install Windows. No owner DB was touched
by this package build/proof. Field application remains a separate witnessed action.
