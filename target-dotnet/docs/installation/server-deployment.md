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
authentication or invent employee identities to bypass this gate. An existing reachable
HTTPS OIDC provider and trusted ERP certificate are required; neither is bundled.

Use an elevated Windows PowerShell on the server for installation. Do not run builds,
`dotnet ef`, npm, Python or tests there. The package runs with PostgreSQL 17.11 and the
installed ASP.NET Core .NET 10.0.12 runtime; no SDK. First follow steps 1-6 against the
DEMO only. Record command exits, checks and operator/date after each step. Any failure
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
- C: SSD has 41.6 GB free; estimated six-month DB 5-8 GB fits. Keep at least 25 GB free
  after data/package/log growth; alert below 25 GB and address before falling below
  20 GB. Recheck actual daily growth/WAL/log retention; do not promise a fixed forecast.
  D: HDD is a different physical disk: backups `D:\SESS-Backups`, verification working
  root `D:\SESS-Backup-Verification`. E: shares D:'s HDD, not a third physical disk.
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
silently changing those services/rules. PostgreSQL remains loopback-only for ERP access.

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
`authentication.keycloak.example.json` or `authentication.cognito.example.json`,
not the example addresses. Follow `authentication-bootstrap.md` for the one-time
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
connection database to DEMO, retain Maximum Pool Size=20, merge Authentication/Reporting.
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
steps5-13. Missing production UI screens stop acceptance; a successful static build
is insufficient. Record TD/frontend developer acceptance and discrepancies.

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

On this server, repeat steps2-3 with `$db='sess_nexa_erp'`, only if this exact database
is absent and after demo acceptance. Do not restore DEMO or the laptop development DB.
All roles already exist: reconciliation preserves credentials. Verify130/head,
RECONCILED/VERIFIED and zero stock movements. Update BOTH service connection database
and ExpectedDatabase, perform real identity bootstrap and fresh-database runbook
steps6-13 (selective attachments, item master, category script, governed workbook
imports, warehouses/bins, QC/vendor/store rules, BOTH authorised opening-stock ceremonies).
The package includes installation docs and committed business scripts under
`installer/database/postgresql`; approved business workbooks/source dump must be
supplied separately. Review the real source inputs before applying any business script.
These scripts are not executed by package installation. For the older runbook's
relative `database/postgresql` paths, first run `Set-Location "$install\installer"`;
use explicit paths for separately supplied workbooks/dumps. Its source-based
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

## 8. Verified backup on D: and weekly off-machine copy

Create protected `C:\SESS-Backup` config/log directories (Administrators/SYSTEM and the
specific dedicated scheduled account only). Copy backup.example.json there as backup.json.
Set `ExpectedHost=127.0.0.1`, `ExpectedPort=5432`, `ExpectedDatabase=sess_nexa_erp` and the FIELD
system identifier captured in step2; never reuse the laptop identifier. Keep
BackupRoot `D:\SESS-Backups`, WorkingRoot `D:\SESS-Backup-Verification`. Do not clear
existing unmarked/nonempty roots. Grant the backup account the required paths; it
needs authority for source dump/globals and its own disposable restore verification.
As that account ON THIS SERVER, enter the complete approved backup DB connection:
```powershell
Read-Host 'Database backup connection string' -AsSecureString | Export-Clixml -LiteralPath 'C:\SESS-Backup\connection.clixml'
```
DPAPI is machine/account bound. As setup engineer register using that Windows credential:
```powershell
$backupAccount=Get-Credential
& "$install\installer\tools\Register-VerifiedDatabaseBackup.ps1" -InstallerPath $installer -ConfigPath 'C:\SESS-Backup\backup.json' -CredentialFile 'C:\SESS-Backup\connection.clixml' -LogDirectory 'C:\SESS-Backup\logs' -DailyAt '18:45' -Credential $backupAccount
Start-ScheduledTask -TaskName SESS-NexaERP-VerifiedBackup
```
Check completion, Task Scheduler LastTaskResult=0, protected `logs/last-run.json`, and
newest bundle manifest's verified restore result. Witness it running signed out.
One verification cluster at a time; do not overlap manual verification with the task.
Backup includes matching globals; keep configuration, certificates/keys, external
attachments and OIDC-provider backups by their separate protected procedures.

**Weekly**, copy the newest successfully verified COMPLETE bundle (including globals,
manifest and restore evidence) off this machine, to removable media then disconnect it,
or an independently protected immutable/offline destination. Copy the source root's `.sess-verified-backups.json` ownership marker into a new
private destination root alongside the copied `run-<id>` directory; preserve its ID.
Never overwrite another backup root's marker. Compare SHA256 of the marker and every
source/destination bundle file. For independent verification, copy backup.json to a
temporary protected config with BackupRoot set to this off-machine destination root,
keeping the same database/system identifier and D: WorkingRoot. Run the published
`Installer backup verify --config <copy-config> --bundle <destination-root>\run-<id>`
under the backup account with its connection environment available. Check exit 0 and
restore evidence before disconnecting it; no manual verification may overlap the daily
task. A copied bundle alone cannot be verified with the original D: BackupRoot config. Record location/date/operator,
checks and restore result. Assign a named weekly owner before go-live. C:/D: separation
protects against one disk failing, not ransomware on unpatched Windows encrypting both;
a permanently writable network share/attached USB is not the required offline copy.

After cutover the laptop returns to development/LabVIEW: no production-hours rule,
restore NI with `RESTORE-NI-Siemens.ps1`; its nightly development witness task may stay.
The server's no-new-non-ERP-work rule applies at all hours, preserving the explicitly
protected existing SOLIDWORKS/SQL/IIS services. Do not execute laptop NI restoration on
the server. End the field report with RESULT_REPORTED_PENDING_WITNESS until the named
human acceptance/reboot/backup witnesses have actually completed.
