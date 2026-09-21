# E: SSD verification plus DAILY off-machine ERP and identity backups

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.

These rules apply to every linked deployment, identity, certificate and backup procedure. Stop and report a conflict; do not reclaim ports or memory from protected services.

**D: AND E: ARE PROHIBITED on DESKTOP-SPF5420 for backup or restore verification.**
Disk1 has 786 I/O errors/90 days (latest 15:56), 42 NTFS corruption events, 22 flush
failures, corrupt indexes/148 lost files and 44 recovery folders across 12 dates.
E: scanning clean and zero SMART read-error counters do not override that evidence.
Cable/port fault is suspected, not proved. Do not run repair from this laptop or treat
cable replacement as clearance. No new backup data is written to that HDD.

## Destination and accounts

Designated receiver: **Ilamparuthi's PC**, the frontend developer's machine,
identified by the owner as **IT TEAM 2**.
**DESKTOP-AP is the Technical Director's development laptop and is NOT the receiver.**
The exact `hostname` command output is pending: **[ILAMPARUTHI PC hostname]**. Replace this
placeholder in the plan's DestinationHost, both UNC roots and every account/command
below before setup. The placeholder deliberately fails hostname validation; do not
schedule until `hostname` on Ilamparuthi's PC and name resolution agree.
`IT TEAM 2` contains spaces and is not accepted by the hostname guard; do not guess
IT-TEAM-2 or ITTEAM2, rename the PC, or loosen validation to make setup pass.
Microsoft's [DNS host naming rules](https://learn.microsoft.com/en-us/troubleshoot/windows-server/active-directory/naming-conventions-for-computer-domain-site-ou#dns-host-names)
exclude whitespace. Confirm the actual computer name locally before filling UNC paths.

Proposed receiver-local paths are D:\SESS-Backup-Incoming and D:\SESS-Backup-Vault;
these are NOT the prohibited server HDD. Confirm this PC has that healthy volume,
capacity for retention, and daily availability before creating anything. No capacity
measurement from DESKTOP-AP applies to this receiver. If its approved volume differs,
update receiver-local paths together. No account/share/task has been created here.
The receiver must remain awake and connected for the 18:45 backup window and retries.
No fallback to a server-local path or DESKTOP-AP is allowed.

On the SERVER create nonadministrator **DESKTOP-SPF5420\SESSBackup** with Log on as
a batch job. Task uses PASSWORD logon, runs whether a user is logged on or not, and
must be allowed SMB network access. Do not use S4U/"Do not store password", SYSTEM,
Guest or an interactive-only task for this sender. On the RECEIVER create
**[ILAMPARUTHI PC hostname]\SESSBackup**, nonadministrator, matching strong password for workgroup
pass-through authentication. Permit its share/network logon; deny interactive/RDP
logon after one-time credential provisioning. Store the password in the approved
secret store, not scripts; rotate both and the task credential together. Domain sites
may instead use a dedicated domain backup identity, with the same narrow rights.

Receiver setup (receiver agent/admin only, no changes to the server's services):
create Incoming, D:\SESS-Backup-Vault and D:\SESS-Backup-ReceiverStatus. Disable NTFS
inheritance on these NEW folders; Incoming grants SYSTEM/Administrators Full and
[ILAMPARUTHI PC hostname]\SESSBackup Modify. Vault and ReceiverStatus grant only SYSTEM/Administrators
Full; **no SESSBackup access**, no network share for Vault. Never reuse a general
Everyone-writable folder. Then:

```powershell
New-SmbShare -Name 'SESS-Backup-Incoming$' -Path 'D:\SESS-Backup-Incoming' -ChangeAccess '[ILAMPARUTHI PC hostname]\SESSBackup' -FullAccess 'BUILTIN\Administrators' -EncryptData $true -CachingMode None
Get-SmbShareAccess -Name 'SESS-Backup-Incoming$'
```

Check share AND NTFS permissions, no Everyone/Guest grant. This account may write
only the SESS-Backup-Incoming$ share: audit its effective permissions on all other
receiver shares (including access inherited through Users/Everyone), and verify
writes there are denied. Resolve any conflicting grant before accepting the setup;
do not change unrelated shares blindly. Receiver firewall permits
SMB TCP445 from 192.168.68.130 only for this transfer; review existing overlapping
share rules without breaking unrelated use. No 445/5432 inbound opening is added to
the ERP server. Require SMB3 encryption; record Get-SmbConnection under the task
identity. Encrypt receiver storage at rest (BitLocker or approved equivalent), escrow
recovery keys separately; bundles include identity/TOTP and certificate secrets.

## Capacity and local retention

C: currently has 41.6 GB free; floor is **25 GiB** in the script (slightly conservative
against the requested 25 GB). Only ~16 GiB is available for all new runtime/data,
identity, WAL, logs, retained dumps and a temporary restore. The old 5-8 GB ERP-only
six-month estimate does NOT establish that this combined layout will fit.

Local roots: C:\SESS-Backups\ERP and \Identity; working roots:
C:\SESS-Backup-Verification\ERP and \Identity. Never overlap them or enroll a
nonempty unmarked folder. Keep only the **newest TWO verified bundles per database**
locally. Older local bundles are deleted only after all their files match the owned
off-machine copy. Nothing incomplete/unrecognized is automatically deleted. This
wrapper shortens the generic Installer's 30-day/84-day history policy for THIS site;
longer history belongs to the receiver, not scarce server C:.

Before first scheduling, measure each source DB/cluster/WAL and a disposable restore
outside production peak hours. Fill RequiredRunGiB in the plan (zero deliberately
REFUSES). Per job budget: at least a full uncompressed source DB allowance for a new
dump, **twice its restored DB size** for verification, configuration files, plus 25%
margin and 2 GiB. Use measured higher peak if greater. Recheck source DB growth and budgets every day;
a manually configured budget does not automatically grow with the database. Free space must cover 25 GiB
PLUS this budget, after retained history. Identity and ERP run sequentially, not
concurrently. With an 8 GiB database the provisional run budget alone can exceed the
available ~16 GiB: reclaim/expand healthy SSD storage BEFORE that point; do not lower
the floor or quietly move work to the faulty HDD.

This is an admission check, not a filesystem quota or mathematical guarantee against
concurrent database/WAL growth. Daily capacity monitoring and measured budget review
are mandatory; alert at 30 GiB, stop admitting backup work below its budget/floor,
and escalate before 25 GiB. Database growth takes priority over backup HISTORY:
move copied old bundles off C:, trim reviewed logs/failed synthetic runs safely, then
increase healthy capacity. Never delete database/WAL files, the newest two local
verified bundles or the sole off-machine recovery copy to make a task green. If two
local bundles plus one verification cannot fit, the site is blocked pending capacity.

## Configure and schedule one serial site job on the server

1. Protect C:\SESS-Backup configuration/log/status/Installer directories: SYSTEM,
   Administrators Full; SESSBackup Read/Execute on scripts/Installer/config, Modify
   on logs/status and its backup/working roots. Only administrators may edit scripts
   or plan. Copy published Installer plus Invoke-VerifiedDatabaseBackup.ps1,
   Invoke-ServerDailyBackup.ps1, VerifiedBackupTransfer.psm1 together into the tools
   directory. No SDK/Python is required. The old separate daily jobs must not also
   run: review and disable only the previous ERP/identity backup tasks if installed.
2. Copy backup.example.json as ERP.json and keycloak/backup.identity.example.json as
   Identity.json; fill this FIELD cluster's system identifier in BOTH. Existing
   cluster is shared, databases separate. Never use a laptop witness identifier.
   Include authoritative API config and its exportable recovery PFX/key material as
   protected ConfigurationFiles for ERP; include Keycloak config/PEM keys/version
   file as already listed. Cover any external attachments separately before go-live.
   Grant SESSBackup Read on EACH ConfigurationFiles source specifically, including
   Keycloak conf/PEM key and the ERP protected recovery PFX/config. It otherwise cannot
   read the service-only key ACLs. This is privileged backup access; do not grant Users
   access or broad access to unrelated certificates/engineering folders. Keep the PFX
   password in the separately escrowed recovery secret store, not the manifest.
   No config/key rotation during backup window. Freeze identity/role administration
   across both snapshots; they are sequential, not one atomic site snapshot.
3. Once, run PowerShell as SESSBackup ON THIS SERVER and export two secure connection
   strings (127.0.0.1:5432; respective database; approved DBA backup credential) using
   Read-Host -AsSecureString | Export-Clixml to ERP.clixml / Identity.clixml. Installer
   backup currently requires superuser/global-role visibility and original OID10
   administrator retained for private restore; do not pretend runtime credentials
   suffice. DPAPI binds these files to THIS Windows account and THIS machine.
4. Copy server-daily-backup-plan.example.json to C:\SESS-Backup\daily-plan.json,
   fill InstallerPath and measured RequiredRunGiB values. Check both remote roots
   resolve to the accepted receiver. As the task identity, test a throwaway file in
   Incoming and confirm Vault access is DENIED. No credentials embedded in the plan.
5. Register ONE daily job (setup console, secure credential prompt):

   ```powershell
   $cred=Get-Credential 'DESKTOP-SPF5420\SESSBackup'
   $action=New-ScheduledTaskAction -Execute "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -Argument '-WindowStyle Hidden -NoProfile -NonInteractive -File "C:\SESS-Backup\tools\Invoke-ServerDailyBackup.ps1" -PlanPath "C:\SESS-Backup\daily-plan.json"'
   $trigger=New-ScheduledTaskTrigger -Daily -At '18:45'
   $settings=New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -RestartCount 2 -RestartInterval (New-TimeSpan -Minutes 30) -ExecutionTimeLimit (New-TimeSpan -Hours 8)
   if (Get-ScheduledTask -TaskName SESS-NexaERP-SiteDailyBackup -ErrorAction SilentlyContinue) { throw 'Review existing task; do not overwrite' }
   Register-ScheduledTask -TaskName SESS-NexaERP-SiteDailyBackup -Action $action -Trigger $trigger -Settings $settings -User $cred.UserName -Password $cred.GetNetworkCredential().Password
   Start-ScheduledTask -TaskName SESS-NexaERP-SiteDailyBackup
   ```

6. Check task completed LastTaskResult=0; both local logs/last-run.json success;
   C:\SESS-Backup\status\daily-last-run.json ExitCode=0 and OffMachineVerified=true.
   The job first transfers retained verified runs, checks capacity, performs each
   restore-verified backup, copies into a new remote staging directory, reads back
   every file's SHA256/length and publishes by rename only when equal. It also
   preserves the root ownership marker; mismatched/nonempty foreign roots refuse.
   A copying/space/network failure is nonzero, not a fresh success. No stock operation
   is used to test backups. Verify signed-out operation and one private cluster only.

## Receiver-owned archive and acceptance

On Ilamparuthi's PC copy Archive-IncomingBackups.ps1 and VerifiedBackupTransfer.psm1 into
an administrator-controlled tools folder. Register an hourly task as **SYSTEM**, run
whether logged on or not, invoking the script with -Receiver "[ILAMPARUTHI PC hostname]" -Incoming
D:\SESS-Backup-Incoming -Archive D:\SESS-Backup-Vault -StatusDirectory
D:\SESS-Backup-ReceiverStatus. Use IgnoreNew, StartWhenAvailable; Task Scheduler GUI
can set daily trigger, repeat every 1 hour for 1 day indefinitely. No credentials for
this archive identity exist on the ERP server. Check archive-last-run.json exit 0 and
both newest verified timestamps <=26h. New/copied files are verified before publication;
existing archive bundles are compared, never overwritten. Server-writable Incoming
alone is NOT ransomware isolation; accept the site only after Vault is populated and
the sender cannot read/write it. Receiver malware/admin compromise remains a separate
risk: maintain a disconnected/immutable copy of Vault according to site policy.

Vault/incoming retention is receiver-admin owned: keep 14 daily and four weekly
verified site sets initially, measure bundle sizes/free space before accepting this
capacity. The supplied receiver script never deletes history. Receiver administrator
may remove older matched copies after independently validating retained recovery
sets; no sender-side mirrored deletion or robocopy /MIR. Monitor receiver free space
and allocate more storage if those sets do not fit. Verify the first off-machine ERP
AND identity bundles through `Installer backup verify` using copies of their config,
BackupRoot pointing at the copied owned root, and a NEW private WorkingRoot on healthy
storage. Keep the marker's ID. Run this serially, never alongside another verification
or regression cluster. Confirm exit 0 and restore evidence. Later perform regular
receiver recovery rehearsals with source unavailable; a byte-equal copy check alone
is not that recovery rehearsal. Rehearsal may update evidence logs, so work from a
separate private COPY, not the immutable Vault bundle.

Go-live requires receipt of: fresh local VERIFIED + fresh DAILY off-machine hash
verification + receiver archive success + first off-machine restore evidence +
signed-out task/reboot checks + storage admission. An offline receiver, stale result,
missing identity copy or capacity breach is ACTION_REQUIRED, never success.

## Later HDD transition

Only after replacement or documented cable/port repair AND disk/filesystem health
acceptance, approved by TD, move backup and verification roots to that healthy disk.
Copy/verify owned roots, update plans, rerun restore/signed-out checks, then retire
old roots carefully. Keep DAILY off-machine transfer permanently. No deployment step
here repairs/reformats D:/E: or changes protected engineering data.

References: [encrypted SMB share and permissions](https://learn.microsoft.com/en-us/powershell/module/smbshare/new-smbshare),
[Task Scheduler logon modes](https://learn.microsoft.com/en-us/windows/win32/taskschd/principal-logontype).

## Daily production-state check

Copy packaged installer/tools/Test-ProductionState.ps1 and Test-DailyBackupState.ps1
into C:\SESS-Backup\tools. Copy server-production-state.example.json to
C:\SESS-Backup\production-state.json; confirm service names and endpoints locally.
This server profile does not require NI/Siemens services to stop and does not apply
the old laptop production-hours rule. Run every morning on the server:

```powershell
powershell -NoProfile -File C:\SESS-Backup\tools\Test-ProductionState.ps1 -ConfigPath C:\SESS-Backup\production-state.json
```

The daily off-machine check requires a successful, enabled scheduled task and a
receipt no older than 26 hours with ExitCode=0 and OffMachineVerified=true. Missing,
unreadable, malformed, future-dated or stale receipts and failed/disabled/missing
tasks produce FAIL and a nonzero exit, including with -BeforeGoLive. Investigate and
record resolution the same day; never dismiss a missed day as silent success.
The receipt represents both ERP and identity copied and hashed after landing. Also
check the receiver's separate protected archive receipt; the server does not gain
access to the receiver-owned vault or its status directory through this monitor.
