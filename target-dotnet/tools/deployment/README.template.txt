SESS NexaERP - DESKTOP-SPF5420 deployment candidate
Backend/package source HEAD: {{HEAD}}
Frontend source commit: {{FRONTEND}} (built from the frontend developer branch)

READINESS: CANDIDATE, NOT CLEARED FOR DEMO OR GO-LIVE.
The selected frontend branch production build still signs in through Debug-only
/api/v1/dev endpoints. Release deliberately excludes these. Obtain and integrate
the frontend developer's production/OIDC-capable source and provider settings,
then rebuild/retest the package. Never enable development authentication in Release.
No production credentials, certificate private keys or identity-provider service
are included. Install local Keycloak on this server following installer/docs/server-keycloak-install.md;
Java 17 is already present. Production frontend integration remains a prerequisite. The ERP binaries themselves need only .NET 10 ASP.NET Core runtime
and PostgreSQL 17; Node, an SDK and EF tools are not required on the server.

api/       Release framework-dependent API; Windows service SESSNexaERP, HTTPS 8443.
web/       npm production-build static files; copy into installed api/wwwroot.
installer/ Release framework-dependent database-principals, bootstrap and backup CLI;
           tools/ backup schedule and package verification scripts; docs/ instructions.
migrate/   Framework-dependent win-x64 efbundle.exe; migration login uses
           Options=-c role=nexa_erp_owner. proof.json records SDK-free proof.
MANIFEST   Source SHA, UTC build time, readiness, migration head, size and SHA-256
           of every other file. A file cannot contain its own hash. Verify MANIFEST
           against the separately supplied <HEAD>.MANIFEST.sha256 before trusting it.

Apply in this exact order, checking each numbered server-runbook step below:
1. Copy unchanged package; verify MANIFEST and all payload hashes.
2. Create only sess_nexa_erp_DEMO on the server's existing PostgreSQL cluster.
3. CREATE SCHEMA advance; provision principals; run efbundle.exe; reconcile/verify.
4. Resolve the production frontend/OIDC gate, configure runtime principal and trusted
   HTTPS certificate, install/start Windows service; health check.
5. Serve web at same origin, scope firewall 8443 to 192.168.68.0/24; another-PC check.
6. Complete the demo walk and reboot/no-login witness, then drop only the demo DB.
7. Create sess_nexa_erp directly on server with Option C. No stock-moving command
   before BOTH opening-stock ceremonies are posted. Do not restore the demo.
8. Configure and witness daily verified ERP/identity backups on C: PLUS DAILY off-machine copy;
   D: and E: are rejected because their physical HDD is suspect. Follow
   installer/docs/server-daily-backups.md, including the 25 GiB free-space gate.

Never touch SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, ewserver or their data;
ALL NI, Siemens and Rockwell services stay; preserve IIS Default Web Site and
/Updater; Wamp remains stopped/manual. Reference C:\SESS-ServerPrep; no repeated preparation. Never clean-install Windows. No owner DB was touched
by this package build/proof. Field application remains a separate witnessed action.
