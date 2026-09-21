# DESKTOP-SPF5420: package-to-server deployment

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.


**Status: candidate; production frontend/OIDC integration and field witness pending.**
The selected `feature/frontend` source at `0c59254f58bd49fc13a8b919387ba0cf5a1d9988`
builds, but its login uses `/api/v1/dev/*`, deliberately absent
from Release. Obtain the frontend developer's production/OIDC-capable source, rebuild,
repeat acceptance and issue a new hashed package before step 4. Do not enable Debug
authentication or invent employee identities to bypass this gate. Install local Keycloak using server-keycloak-install.md and trusted HTTPS using
server-https.md. The production frontend must implement server-frontend-oidc-contract.md.

Use an elevated Windows PowerShell on the server for installation. Do not run builds,
`dotnet ef`, npm, Python or tests there. The package runs with PostgreSQL 17.11 and the
installed ASP.NET Core .NET 10.0.12 runtime; no SDK. First follow steps 1-6 against the
DEMO only. Record command exits, checks and operator/date after each step in
server-acceptance-receipt.md. Any failure
stops progression; never call a partially completed demo an accepted deployment.

## Protected machine and preflight

- Windows 10 stays by TD decision. Never clean-install Windows. Plan a later supported
  Windows 11 in-place upgrade, with backup and compatibility checks; commercial ESU is
  USD $61 / $122 / $244 per device in successive years, cumulative purchases, taxes and
  reseller terms additional. Without ESU this host remains unpatched. See runbook 15.1a.
- NEVER stop, disable, modify or remove `SESS_SQLEXPRESS`, `TEW_SQLEXPRESS`, their SQL
  services/databases, or SOLIDWORKS Electrical Collaborative Server. About 77 design
  databases are protected. Existing engineering services are the explicit exception
  to the server's no-new-non-ERP-work rule. No new development or ad-hoc workloads; existing NI/Siemens workloads stay running.
- Leave Wamp Apache/MySQL/MariaDB stopped/manual. Leave IIS ports 80/81 and its sites.
- Reserve 192.168.68.130, gateway 192.168.68.1. Gigabit adapter currently at 100 Mbps:
  sufficient for eleven ERP users; no go-live gigabit prerequisite. Large copies/restores
  will take longer. Replace/test the cable later without making it a launch dependency.
- C: SSD has 41.6 GB free; the old 5-8 GB estimate covers only ERP data, not backups. Keep at least 25 GB free
  after data/package/log growth; alert below 25 GB and address before falling below
  20 GB. Recheck actual daily growth/WAL/log retention; do not promise a fixed forecast.
  D:/E: share the suspect HDD and are prohibited. Use the C: plus DAILY off-machine
  design in server-daily-backups.md; the six-month combined capacity is not yet proved.
- With existing workloads running, 6.4 GB RAM is available. PostgreSQL initial sizing: shared_buffers
  512MB, effective_cache_size 2GB (planner hint), work_mem 4MB, maintenance_work_mem
  128MB, max_connections 40, max_parallel_workers 2, max_parallel_workers_per_gather 1.
  API pool maximum 15; Keycloak pool maximum 10. Validate under eleven-user load, alert at <2 GB available RAM.
  Apply only to the ERP PostgreSQL instance after verifying its data_directory; never
  adjust SQL Server/SOLIDWORKS resources. Record PostgreSQL settings and planned ERP-only
  service restart separately; keep other workloads running.

Read-only inventory/checks:
```powershell
hostname
Get-Service | Where-Object { $_.Name -match 'SQL|Solid|W3SVC|postgres|wamp' } | Format-Table Name,Status,StartType
Get-NetTCPConnection -State Listen | Where-Object { $_.LocalPort -in 80,81,8443,5432 }
Get-Volume -DriveLetter C,D,E
Get-Partition -DriveLetter C,D,E | Get-Disk | Select-Object Number,FriendlyName,SerialNumber
Get-NetAdapter | Select-Object Name,Status,LinkSpeed
& 'C:\Program Files\dotnet\dotnet.exe' --list-runtimes
& 'C:\Program Files\PostgreSQL\17\bin\psql.exe' --version
& 'C:\Program Files\PostgreSQL\17\bin\psql.exe' -X -h 127.0.0.1 -U postgres -d postgres -c 'SHOW data_directory;' -c 'SHOW listen_addresses;'
Get-MpComputerStatus | Select-Object AntivirusEnabled,RealTimeProtectionEnabled,AntivirusSignatureLastUpdated
```
Check host/IP/disks/runtime against the facts above. PostgreSQL data_directory must
resolve to C: on the SSD; verify before changing its configuration. If 8443 is occupied,
identify its owner and stop this installation; never kill an existing service to take
the port. Time/power/account/Defender/RDP/network-profile preparation is ALREADY DONE:
reference C:\SESS-ServerPrep and its checks, not repeat setup commands. Defender is
active, signatures update every four hours daily, RDP is off and network is Private.
Review existing firewall rules without breaking protected engineering traffic. The ERP
inbound rule must allow ONLY TCP8443 from 192.168.68.0/24; broad existing allow rules can
undermine this. Escalate any conflict with protected SOLIDWORKS rules to TD rather than
silently changing those services/rules. Keep pg_hba local-only; current listen_addresses='*' does not authorize LAN access.
No 5432 firewall opening; API and Keycloak connect locally.

## 1. Copy and verify the immutable package

Copy `C:\SESS-Deploy\<head-sha>` from USB/share to the same server path. Obtain the full
HEAD and MANIFEST SHA256 through the separately supplied build report/digest (not solely
from an untrusted copy). Set these values, then verify before execution:
```powershell
$head='<full-40-character-head-from-report>'
$manifestDigest='<sha256-from-report>'
$package="C:\SESS-Deploy\$head"
(Get-FileHash -LiteralPath "$package\MANIFEST" -Algorithm SHA256).Hash
# First compare the printed digest to $manifestDigest; stop on mismatch.
& "$package\installer\tools\Verify-Package.ps1" -Root $package -ExpectedHead $head -ExpectedManifestSha256 $manifestDigest
```
Check `VERIFIED`, every listed file hash/size, no extra/missing files. MANIFEST excludes
itself by necessity; its separately supplied digest covers it. Keep the verified package
unchanged. Copy to a new durable install directory `C:\SESS\$head` and use `$install`
for it below. Refuse to overwrite an existing installed version without a reviewed rollback.
```powershell
$install="C:\SESS\$head"
if (Test-Path -LiteralPath $install) { throw 'Install destination already exists.' }
New-Item -ItemType Directory -Path 'C:\SESS' -Force | Out-Null
Copy-Item -LiteralPath $package -Destination $install -Recurse
$pg='C:\Program Files\PostgreSQL\17\bin'
$installer="$install\installer\SESS.NexaERP.Installer.exe"
function Assert-Exit { if ($LASTEXITCODE -ne 0) { throw "Command failed: $LASTEXITCODE" } }
```
Check original MANIFEST again; configuration edits occur only in `$install`.

## 2. Create the DEMO database first

Use the existing PostgreSQL instance, not SQL Server, and never initdb over its data.
Use secure password prompts; never include a password in command history or a report.
```powershell
$db='sess_nexa_erp_DEMO'
& "$pg\psql.exe" -X -h 127.0.0.1 -U postgres -d postgres -c 'SELECT datname FROM pg_database;'
Assert-Exit
# If this exact DEMO name exists, STOP and identify its owner/content. Do not drop it automatically.
& "$pg\createdb.exe" -h 127.0.0.1 -U postgres $db
Assert-Exit
& "$pg\psql.exe" -X -h 127.0.0.1 -U postgres -d $db -v ON_ERROR_STOP=1 -c 'SELECT current_database(), system_identifier FROM pg_control_system();' -c 'CREATE SCHEMA advance;'
Assert-Exit
```
Check current_database is exactly `sess_nexa_erp_DEMO`, record this server's system
identifier, and confirm advance is empty. The Installer requires the empty schema to
exist; its pre-migration provisioning now supports absent application tables.

## 3. Provision, migrate and reconcile (API stopped)

Secure process-only secret input helper (do not enable a transcript):
```powershell
function Set-SecretEnvironment([string]$Name,[string]$Prompt) {
 $secret=Read-Host $Prompt -AsSecureString
 $ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
 try { [Environment]::SetEnvironmentVariable($Name,[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr),'Process') }
 finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}
$env:NexaErp__ExpectedDatabase=$db
Set-SecretEnvironment 'ConnectionStrings__NexaErpInstaller' "Complete DBA connection string to 127.0.0.1:5432 database $db"
# Only for first provisioning when NONE of the four roles exists; passwords >=24 characters.
Set-SecretEnvironment 'NEXAERP_MIGRATION_PASSWORD' 'New migration password (store securely)'
Set-SecretEnvironment 'NEXAERP_BOOTSTRAP_PASSWORD' 'New bootstrap password (store securely)'
Set-SecretEnvironment 'NEXAERP_RUNTIME_PASSWORD' 'New runtime password (store securely)'
& $installer database-principals provision
Assert-Exit
& $installer database-principals status
Assert-Exit
Set-SecretEnvironment 'ConnectionStrings__NexaErp' "Migration connection: Host=127.0.0.1;Port=5432;Database=$db;Username=nexa_erp_migration;Password=<secret>;Options=-c role=nexa_erp_owner;Pooling=false"
& "$install\migrate\efbundle.exe"
Assert-Exit
& $installer database-principals provision
Assert-Exit
& $installer database-principals status
Assert-Exit
@'
SET ROLE nexa_erp_owner;
SELECT count(*), max("MigrationId") FROM advance."__EFMigrationsHistory";
SELECT count(*) FROM advance.stock_movements;
'@ | Set-Content -LiteralPath "$install\migrate\check.sql" -Encoding UTF8
& "$pg\psql.exe" -X -h 127.0.0.1 -U nexa_erp_migration -d $db -v ON_ERROR_STOP=1 -f "$install\migrate\check.sql"
Assert-Exit
```
Check first `PROVISIONED` (or existing four roles `RECONCILED`), post-migration
`RECONCILED` then `VERIFIED`; 130 migrations, head matching MANIFEST; zero stock movements.
The file-based query preserves SQL quoting on Windows PowerShell 5.
Roles are cluster-wide; the subsequent go-live
provisioning reuses all four roles and preserves passwords. Mixed role state refuses.

Replay: keep API stopped, rerun the same bundle and reconciliation; already applied
migrations are skipped by EF history. Check history count/head unchanged. On failure,
retain output and investigate; do not edit migration history to bypass a guard.
Clear process secrets after use:
```powershell
foreach ($name in @('ConnectionStrings__NexaErpInstaller','ConnectionStrings__NexaErp','NEXAERP_MIGRATION_PASSWORD','NEXAERP_BOOTSTRAP_PASSWORD','NEXAERP_RUNTIME_PASSWORD')) { [Environment]::SetEnvironmentVariable($name,$null,'Process') }
```
Check no SDK was installed to accomplish this. See `sdk-free-migration-proof.md` and
`migrate/proof.json` for the exact laptop witness and its field limitations.

## 4. Configure and start the API Windows service

**STOP here while the production frontend/OIDC gate is unresolved.** Use the real
provider's issuer/metadata/JWKS/client/audience settings from
`authentication.server.keycloak.json` and the exact D1 contract; complete D2-D5 first. Follow `authentication-bootstrap.md` for the one-time
SESS-12 issuer/subject ceremony using the published Installer as `nexa_erp_bootstrap`
(`authentication-bootstrap --issuer <https-issuer> --subject <stable-subject>`), then
map employees through governed APIs. Never use a development identity command.
```powershell
$env:NexaErp__ExpectedDatabase=$db
Set-SecretEnvironment 'ConnectionStrings__NexaErpBootstrap' "Bootstrap login connection to database $db as nexa_erp_bootstrap"
$issuer=Read-Host 'Approved exact HTTPS issuer for SESS-12'
$subject=Read-Host 'Verified stable provider subject for SESS-12'
& $installer authentication-bootstrap --issuer $issuer --subject $subject
Assert-Exit
Remove-Item Env:\ConnectionStrings__NexaErpBootstrap
```
Check `COMPLETED` for the one-time ceremony; do not replay it as a connectivity test.

Copy `installer/service-settings.example.json` to `api/appsettings.Production.json`
on the installed copy; enter runtime password locally, set ExpectedDatabase and the
connection database to DEMO, set Maximum Pool Size=15;Minimum Pool Size=0, merge Authentication/Reporting.
Do not include any development flags, even set false. Install a certificate into
LocalMachine\My, subject DESKTOP-SPF5420, with server-auth EKU, IP SAN 192.168.68.130,
valid dates, private key; the server and clients must trust its issuer. Use the configured unique
certificate subject and grant ONLY this service account read access to its private
key via Certificates MMC. Do not store a PFX password in the package.

```powershell
if (Get-Service -Name SESSNexaERP -ErrorAction SilentlyContinue) { throw 'ERP service already exists; inspect before replacing.' }
$binary='"'+$install+'\api\SESS.NexaERP.Api.exe" --contentRoot "'+$install+'\api" --environment Production'
New-Service -Name SESSNexaERP -BinaryPathName $binary -StartupType Automatic -DisplayName 'SESS NexaERP API' -ErrorAction Stop
sc.exe config SESSNexaERP start= delayed-auto obj= 'NT SERVICE\SESSNexaERP'
Assert-Exit
sc.exe failure SESSNexaERP reset= 86400 actions= restart/10000/restart/30000/restart/60000
Assert-Exit
icacls "$install\api" /inheritance:r /grant:r 'SYSTEM:(OI)(CI)F' 'BUILTIN\Administrators:(OI)(CI)F' 'NT SERVICE\SESSNexaERP:(OI)(CI)RX'
Assert-Exit
New-Item -ItemType Directory -Path "$install\api\wwwroot" -Force | Out-Null
# Verify the PostgreSQL service identified in preflight is automatic/running; do not
# change any SQL Server or SOLIDWORKS service. Check sc.exe qc output before start.
sc.exe qc SESSNexaERP
# Before starting: certlm.msc -> ERP certificate -> Manage Private Keys ->
# grant Read to NT SERVICE\SESSNexaERP (the service now exists).
Start-Service SESSNexaERP
Get-Service SESSNexaERP
Invoke-WebRequest 'https://192.168.68.130:8443/health/live' -UseBasicParsing
Invoke-WebRequest 'https://192.168.68.130:8443/health/ready' -UseBasicParsing
Invoke-WebRequest 'https://192.168.68.130:8443/health/db' -UseBasicParsing
```
Check Running and HTTP200, runtime principal validation succeeded, no password in logs,
no non-ERP service changed. Inspect service/event logs on failure. Health is not a
substitute for authenticated `/api/v1/session/me` and the demo walk. If SCM argument
quoting on the field PowerShell differs, verify `sc.exe qc SESSNexaERP` before starting;
its BINARY_PATH_NAME must contain both quoted absolute paths exactly as above.

## 5. Frontend, firewall and another office PC

```powershell
New-Item -ItemType Directory -Path "$install\api\wwwroot" -Force | Out-Null
Copy-Item -Path "$install\web\*" -Destination "$install\api\wwwroot" -Recurse
if (Get-NetFirewallRule -Name SESS-NexaERP-HTTPS-8443 -ErrorAction SilentlyContinue) { throw 'ERP firewall rule already exists; inspect scope before changing.' }
New-NetFirewallRule -Name SESS-NexaERP-HTTPS-8443 -DisplayName 'SESS NexaERP HTTPS (office LAN only)' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 8443 -RemoteAddress 192.168.68.0/24 -Profile Any
Get-NetFirewallRule -Name SESS-NexaERP-HTTPS-8443 | Get-NetFirewallAddressFilter
Get-NetFirewallRule -Name SESS-NexaERP-HTTPS-8443 | Get-NetFirewallPortFilter
```
Check Windows Firewall enabled/default inbound block; audit and resolve overlapping
broad allows. Do not expose PostgreSQL, RDP, or add an ERP rule for 80/81. Existing
protected engineering/IIS usage must remain operational; ask TD to resolve any demand
to remove their existing rules. On ANOTHER office PC, open
**https://192.168.68.130:8443**, confirm trusted TLS, sign-in/company/roles, refresh a
deep link, read a known API response; unknown `/api/missing` must be 404. Confirm access
outside 192.168.68.0/24 is blocked using an authorised test host. Record this witness.

## 6. Demo walk, reboot witness, then drop DEMO

Run the agreed frontend walk in DEMO only: identities and permissions, master imports,
Stores topology/routing, both opening-stock ceremonies, purchase/GRN/QC, issue/fitment,
Actual BOM provenance (no payables leakage), commercial dossier under its permission,
reports and one verified backup/restore. Refer to the detailed fresh-database runbook
steps 5-13. Missing production UI screens stop acceptance; a successful static build
is insufficient. Record TD/frontend developer acceptance and discrepancies.

For the DEMO backup witness, copy the backup example to a separate protected demo.json:
ExpectedDatabase=sess_nexa_erp_DEMO, the same FIELD system identifier, distinct
C:\SESS-Backups\DEMO and C:\SESS-Backup-Verification\DEMO roots. Under the backup
account with its secure connection environment, invoke the published Installer
`backup run --config <absolute-demo.json>` and check VERIFIED_BACKUP/exit 0; it performs
a private restore verification. Keep its receipt separate. The production daily plan
accepts ONLY sess_nexa_erp and sess_keycloak: do not point that task at DEMO or pretend
its backup is production protection. Witness production daily/off-machine/archive
operation in step 8 before admitting ordinary users.

Coordinate a reboot with the engineering users (do not stop/disable their services).
After reboot, with no server login, test ERP from the other PC; later inspect SCM and
confirm protected engineering services still have their prior state. Only after demo
acceptance, stop **only** SESSNexaERP, verify it targets DEMO, disconnect demo clients:
```powershell
Stop-Service SESSNexaERP
& "$pg\psql.exe" -X -h 127.0.0.1 -U postgres -d sess_nexa_erp_DEMO -c 'SELECT current_database();'
Assert-Exit
& "$pg\dropdb.exe" -h 127.0.0.1 -U postgres sess_nexa_erp_DEMO
Assert-Exit
```
Check exact DEMO name absent in pg_database. No force/drop wildcard; if connections
remain, identify their owners before retry. Never drop a database supplied through an
unreviewed variable. Preserve demo witness/backup outside its database.

## 7. Only now create the fresh go-live database: Option C

On this server, repeat steps 2-3 with `$db='sess_nexa_erp'`, only if this exact database
is absent and after demo acceptance. Do not restore DEMO or the laptop development DB.
All roles already exist: reconciliation preserves credentials. Verify 130/head,
RECONCILED/VERIFIED and zero stock movements. Update BOTH service connection database
and ExpectedDatabase, perform real identity bootstrap and fresh-database runbook
steps 6-13: checked-in legacy item script, SESS-entered business setup through screens,
and BOTH authorised template-v2 opening-stock ceremonies on 28-30 September.
**NO DUMP:** nothing is carried from the frontend developer's database, including
attachments. SESS uploads its own supplier certificates into this clean database.
Follow the SETUP-BEFORE-FIRST-GRN checklist in dependency order, with its named roles,
independent decisions and effective-state checks. Demonstrate required setup screens in
DEMO before training; unresolved screen gaps are acceptance gates, not SQL/API shortcuts.
Configuration entries do not create stock movements and do not block either ceremony;
ANY GRN, issue or adjustment before BOTH ceremonies is forbidden. Daily use begins
1 October only after both POSTED receipts and all deployment gates are accepted.
The package includes installation docs and committed business scripts under
`installer/database/postgresql`; SESS supplies its own physical-count template-v2
workbooks and supporting documents separately. Review these inputs before the ceremonies.
These scripts are not executed by package installation. For the older runbook's
relative `database/postgresql` paths, first run `Set-Location "$install\installer"`;
use explicit paths for the two separately prepared opening-stock workbooks. Its source-based
`dotnet run`/`dotnet ef` examples are replaced by this runbook's published Installer
and bundle commands; do not install an SDK on the server.

**NO stock-moving command on sess_nexa_erp before BOTH opening-stock ceremonies are
posted. A single test GRN can make a ceremony refuse.** Start API only for the governed
setup/ceremonies with designated operators. Bootstrap/map only the named setup, Stores,
Accounts and TD identities needed for those ceremonies; enable/map the remaining users
after both posted receipts. Do not invite ordinary users or run the demo again against
go-live. The two authorised opening postings are the planned exceptions; no GRN, issue,
return, adjustment or other test stock transaction is allowed before both are posted. Retain each ceremony receipt and verify both posted before
releasing ordinary use. This rule includes smoke-test purchase/GRN/issue/adjustment calls.

## 8. SSD verified backups and DAILY off-machine copy

Follow [server-daily-backups.md](server-daily-backups.md) completely. D:/E: are rejected.
Use C:\SESS-Backups and C:\SESS-Backup-Verification component subdirectories plus the
accepted off-machine receiver. Check local restore verification, remote SHA256 match,
receiver-owned archive, capacity and signed-out daily schedule before go-live.

After cutover the LAPTOP returns to development/LabVIEW; its production-hours rule
ends, RESTORE-NI-Siemens.ps1 is for that laptop only, and its nightly witness may stay.
NEVER run NI/Siemens restoration/disablement on this server: all those services stay.
End the field report RESULT_REPORTED_PENDING_WITNESS until actual acceptance.

### Section E receiver correction

The daily backup receiver is **Ilamparuthi's PC (frontend developer), identified as IT TEAM 2**. DESKTOP-AP
is the Technical Director's development laptop and must not be configured as the
receiver. The exact hostname is pending: `[ILAMPARUTHI PC hostname]`. Confirm it on
the receiver before replacing the placeholders in the site plan and commands.
Follow [daily backups](server-daily-backups.md) for the dedicated password account,
one writable incoming share, hash verification after landing and receiver-owned
archive. Install the supplied server profile for `Test-ProductionState`: a missed
or failed daily off-machine copy must return FAIL/nonzero, never silent success.
