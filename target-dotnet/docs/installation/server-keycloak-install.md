# D2: native Keycloak installation on DESKTOP-SPF5420

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.

These rules apply to every linked deployment, identity, certificate and backup procedure. Stop and report a conflict; do not reclaim ports or memory from protected services.

Use native **Keycloak 26.7.4**, PostgreSQL 17.11 and a dedicated Java 17 distribution.
Only PostgreSQL, the .NET runtime and server preparation are confirmed; Java 17
is NOT assumed installed. Verify its actual presence/version before continuing.
Keycloak currently lists OpenJDK 17, Windows and PostgreSQL 17 as supported runtime
choices. Java 17 is deprecated; plan its later replacement for Keycloak only, never
replace a JVM used by engineering software. Retained unpatched Windows 10 is still
the TD's OS decision, not a claim of vendor support for that OS. No VM/Hyper-V,
Docker, IIS rewrite, SDK, Maven or npm is required on the server.

Set `$kc="C:\SESS-Identity\keycloak-26.7.4"` in the installation console and use
that directory for each relative bin command below. Only the server agent applies this procedure. Commands require appropriate local
administrator/DBA rights. This is an installation recipe, not a claim it has run there.
Read [D1](server-frontend-oidc-contract.md), [D3](server-keycloak-realms.md),
[D4](server-https.md), [D5](server-identity-mapping.md) and the daily backup gate first.

1. Reference C:\SESS-ServerPrep for completed preparation. If Java 17 is absent,
   obtain the approved Windows x64 Java 17 distribution from its official publisher,
   verify its publisher checksum/signature, and install/extract into a dedicated C:\ path.
   Record the exact version, source URL, archive hash and actual absolute binary path;
   run that binary with `-version`. Do not change global JAVA_HOME or an engineering
   JVM. If Java 17 is already present, verify and record it rather than reinstalling. Check 8444 and 9000 have no listener
   and no existing service reservation; STOP on conflict. Inventory PostgreSQL's exact
   service name and data_directory. Keep local-only pg_hba and no 5432 firewall rule.
   ERP stays on 8443; do not use 8080. Check 6.4 GB free-RAM budget before proceeding.
2. Obtain keycloak-26.7.4.zip from the official versioned GitHub release linked by
   keycloak.org/downloads. Verify its release checksum/signature from the official
   release metadata before extraction and record URL, SHA256 and date in
   C:\SESS-Identity\deployment-version.json. Download signed Apache Commons Daemon **1.6.1** Windows binaries from Apache,
   verify their SHA512/signature and record the exact release and checksum too.
   Expected official asset digests and exact URLs are in keycloak/server-prerequisites.json;
   compare the downloaded bytes, not merely the filename. Those metadata digests were
   read from the publishers on 21 September; the binaries themselves are not included. Use the amd64 prunsrv.exe. Never silently use latest
   on a later rebuild. Extract into C:\SESS-Identity\keycloak-26.7.4; put prunsrv.exe in
   its bin folder. Refuse to overwrite an existing installation. These third-party
   binaries are separately acquired prerequisites, not asserted to be in the ERP package.
3. On THIS server open psql as DBA to 127.0.0.1:5432/postgres. Confirm no existing
   sess_keycloak database or sess_keycloak role; stop if either exists. Execute:

   ```sql
   CREATE ROLE sess_keycloak LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
   -- psql metacommand prompts twice; do not put the password in SQL/history:
   \password sess_keycloak
   CREATE DATABASE sess_keycloak OWNER sess_keycloak ENCODING 'UTF8' TEMPLATE template0;
   REVOKE ALL ON DATABASE sess_keycloak FROM PUBLIC;
   GRANT CONNECT, TEMPORARY ON DATABASE sess_keycloak TO sess_keycloak;
   \connect sess_keycloak
   REVOKE CREATE ON SCHEMA public FROM PUBLIC;
   GRANT ALL ON SCHEMA public TO sess_keycloak;
   ```

   Check owner/encoding, login with the dedicated role and no elevated attributes.
   This role owns ONLY its identity database; no ERP role membership or ERP grants.
   ERP and identity share the existing PostgreSQL cluster as explicitly decided, but
   never a database/schema. No Installer ERP migration is run on sess_keycloak.
4. Create local nonadministrator Windows account SESSKeycloak, random protected
   password, Log on as a service; deny interactive and remote interactive logon. Do
   not add it to Administrators. Protect C:\SESS-Identity: Administrators/SYSTEM Full,
   SESSKeycloak Read/Execute; give it Modify only on data and logs. Configure/augment
   as administrator before using the restricted service. No global JAVA_HOME change.
   Complete D4: trusted CA chain and separate Keycloak PEM certificate/private key.
   Grant SESSKeycloak read on its key/config, never ordinary Users.
5. Copy keycloak/server-keycloak.conf to conf/keycloak.conf. Its database password
   placeholder must be filled locally with the protected secret; use a random long
   alphanumeric password to avoid configuration-expression escaping. Never commit or
   send the filled file. This file is a backup secret. Set JAVA_HOME only in this
   installation console to the verified dedicated Java 17 path; set JAVA_OPTS_APPEND to
   '-Xms128m -Xmx512m'. Run bin\kc.bat build --db=postgres --health-enabled=true
   --metrics-enabled=true. Check exit 0. This is Java augmentation, not an SDK build.
   Follow D3 to import the credential-free realms offline before the first start.
6. With Keycloak stopped, run bin\kc.bat bootstrap-admin user --username sess-setup
   --password:env KC_BOOTSTRAP_TEMP_PASSWORD. Set that process variable from a secure
   prompt immediately beforehand; clear it immediately afterwards. Check exit 0.
   Never place a bootstrap password in service/global environment. Start manually
   only for commissioning, with `bin\kc.bat start --optimized`; check local readiness
   http://127.0.0.1:9000/health/ready returns UP and both HTTPS discovery endpoints
   advertise the D1 issuers. Stop only this foreground Keycloak process gracefully.
7. Run `bin\kc.bat tools windows-service install --help` and verify these supported
   options before registering (the exact PostgreSQL service name comes from step 1):

   ```powershell
   & "$kc\bin\kc.bat" tools windows-service install --name SESSKeycloak --startup=delayed --stop-timeout=60 --log-path=C:\SESS-Identity\logs --depends-on='<actual-postgresql-service>;Tcpip;Afd'
   if ($LASTEXITCODE -ne 0) { throw 'Keycloak service installation failed' }
   # Configure only this new service; use Services console Log On tab to enter the
   # SESSKeycloak account/password securely and verify Log on as a service.
   # Persist PRIVATE wrapper environment, not machine-wide Java variables:
   & "$kc\bin\prunsrv.exe" '//US//SESSKeycloak' "++Environment=JAVA_HOME=$javaHome" '++Environment=JAVA_OPTS_APPEND=-Xms128m -Xmx512m'
   if ($LASTEXITCODE -ne 0) { throw 'Private Java environment configuration failed' }
   sc.exe failure SESSKeycloak reset= 86400 actions= restart/10000/restart/30000/restart/60000
   sc.exe qc SESSKeycloak
   ```

   Set $kc to C:\SESS-Identity\keycloak-26.7.4 and $javaHome to the verified dedicated Java 17
   directory. Inspect SCM account/dependency/start mode and wrapper configuration;
   do not start it as the installer's default LocalSystem. The official wrapper runs
   kc.bat start; the prebuilt configuration must match. Use Services console to start
   SESSKeycloak; check service Running, logs, bounded heap, readiness and discovery.
8. Add only TCP8444 inbound from 192.168.68.0/24 (all profiles), named
   SESS-Keycloak-HTTPS-8444. Keep 9000 bound 127.0.0.1, no management/5432 firewall
   opening, no router forwarding. D1 requires browsers to reach 8444 directly, so
   the earlier ERP-only 8443 rule is extended for this expressly selected provider.
   Do not alter protected engineering rules. Test trusted HTTPS from another PC.
9. Sign in to the master admin console locally, create a named administrator with
   required OTP, verify that account independently, then delete sess-setup. Disable
   unused admin identities; never use an ERP employee as the temporary master admin.
   Complete D3/D5, configure daily ERP AND identity off-machine backups, then witness
   approved reboot/no-login automatic startup and login. Verify all protected services
   still operate. No user rollout on a failed check. Back up identity data/config/keys;
   realm JSON templates alone cannot recover passwords, subjects or TOTP secrets.

Pin a later upgrade only after a restore rehearsal; never point an older Keycloak at
an upgraded identity schema. Never use blanket taskkill/prunsrv commands: other
software may use the same wrapper. Logs should rotate (10 MB, five files initially).

Sources checked 21 September 2026:
- [Supported platforms/JVM/database](https://www.keycloak.org/server/supported-configurations)
- [Windows service installation](https://www.keycloak.org/server/windows-service)
- [Procrun environment settings](https://commons.apache.org/proper/commons-daemon/procrun.html)
- [Database configuration](https://www.keycloak.org/server/db)
- [Temporary administrator bootstrap](https://www.keycloak.org/server/bootstrap-admin-recovery)

## Field state confirmed 22 September

Keycloak is NOT installed. PostgreSQL 17.11 and ASP.NET Core .NET 10 runtime plus
server preparation are the only confirmed prerequisites. Check the exact Java 17
binary before running these steps; do not infer Java is installed from a planned
path. If absent, stage the approved Java 17 distribution on C: and verify publisher/
checksum and `java -version` before proceeding. Scope JAVA_HOME to SESSKeycloak;
never change Java used by protected engineering software. The ERP package does not
contain Java, Keycloak or Commons Daemon: stage the pinned, hash-checked prerequisites
in this guide separately. Follow server-agent-demo-start.md for the authorised order.
`Java 17` is listed in the upstream [supported configurations](https://www.keycloak.org/server/supported-configurations), checked 22 September 2026; retain the pinned distribution rather than silently upgrading it.

## Keycloak outage is an ERP outage — 22 September update

A browser reload goes through Keycloak by design. Treat failed Keycloak availability
as ERP unavailability even if an already-issued access token works briefly. No
fallback to development login or authentication bypass.

Configure recovery on **SESSKeycloak only**, using an elevated server operator:
```powershell
sc.exe failure SESSKeycloak reset= 86400 actions= restart/10000/restart/30000/restart/60000
sc.exe failureflag SESSKeycloak 1
sc.exe qfailure SESSKeycloak
sc.exe qfailureflag SESSKeycloak
```
Check automatic service startup plus the displayed recovery delays and failure flag.
This instruction does not execute or change any laptop/server service.

Use the updated `server-production-state.example.json` with Test-ProductionState:
it requires ERP readiness on 8443 AND HTTP 200 over trusted TLS on port 8444 for
both staff and approvers `.well-known/openid-configuration` URLs, plus the service
checks. Discovery on 8444 alone is not database readiness: the profile ALSO requires
HTTP 200 from `http://127.0.0.1:9000/health/ready`, on the already-configured loopback
management listener. Keep `health-enabled=true` AND `metrics-enabled=true` in the
Keycloak build so its database health check is included. Confirm UP and the database
check during commissioning. A 503 or unreachable endpoint makes the monitor fail.
No 9000 LAN firewall rule; management remains local. This combines actual readiness
with office-facing TLS/realm availability without exposing management endpoints.

Sources: [Keycloak health checks](https://www.keycloak.org/observability/health)
and [management interface](https://www.keycloak.org/server/management-interface),
checked 22 September. The existing D2 config already binds management to loopback
HTTP; only the monitor profile/read-back instructions change here.

If 8444 fails: record time, affected users and checker output; check SESSKeycloak
state/recovery and its logs, its PostgreSQL database availability, certificate dates,
clock, root trust and listener/firewall. Check local management readiness for dependency
failure. A qualified operator may start the stopped SESSKeycloak service once after
checking the cause, then repeat BOTH realm discovery checks, ERP readiness and a real
browser login/reload. Repeated failure is an escalation; retain logs, do not loop
blind restarts or restart protected engineering services. Do not return to service
until checks pass. Never log tokens, passwords or private keys.

The commissioning package 662e9a3 remains immutable. This updated monitor profile
and instruction must be supplied separately to the server agent; no package rebuild
is authorized. Retain a SHA-256 transfer receipt for the documentation/config overlay.
