# G — laptop read-only findings, 22 September 2026

Checked on DESKTOP-AP. No backup task, service, shell component, root-folder ACL or
installed product was changed by this investigation. The development nightly witness
is separate; its controlled acceptance-run disable/restore is reported with Round 4.

## Old backup task and the access-denied result

At the late recheck, `\NEXA_ERP_Daily_Backup` is still Ready/enabled, next run
22 September 23:00:30 IST. The two `SESS NexaERP PostgreSQL` tasks are now absent;
the user reports removing them. This supersedes the earlier 03:00 observation when
all three existed. Action arguments were not read: no claim that an old clear-text
password has been removed, and no password is reproduced here.

Owner: **BUILTIN\Administrators**. Task principal: User; LogonType 3
(InteractiveToken); RunLevel 1 (HighestAvailable). The run principal controls how
the task runs; it does not give the caller permission to delete its registration.
Task COM security descriptor:

```text
O:BAG:S-1-5-21-2920683000-1276536941-1977317487-513D:(A;ID;0x1f019f;;;BA)(A;ID;0x1f019f;;;SY)(A;ID;FA;;;BA)(A;;FR;;;S-1-5-21-2920683000-1276536941-1977317487-1011)
```

The task file under C:\Windows\System32\Tasks has the same Administrators owner,
Administrators/SYSTEM full rights and explicit read-only rights for the User SID.
This tool's current token has Medium integrity; Administrators is **deny-only** and
IsInRole(Administrator)=false. A sandbox-approved command is not UAC elevation.
That explains access denied from this token. The observed descriptor grants an
actually enabled Administrators token full/delete rights; it does **not establish
why the user's genuinely elevated attempt failed**. The earlier prompt's token,
exact error/command and descriptor at that time were not retained here. Do not
misreport a proven ownership fault or prescribe an unnecessary ACL takeover.

Exact future command, **NOT RUN**: open Windows PowerShell using Run as administrator,
then execute this guarded block (the confirmation names the single task):

```powershell
$p = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'This PowerShell is not UAC elevated; no removal attempted.'
}
Unregister-ScheduledTask -TaskPath '\' -TaskName 'NEXA_ERP_Daily_Backup' -Confirm
Get-ScheduledTask -TaskPath '\' -TaskName 'NEXA_ERP_Daily_Backup' -ErrorAction SilentlyContinue
```

If this elevated command is still denied, retain its full error and token/descriptor
for diagnosis. Do not manually delete the task file/TaskCache, reset its ACL or remove
other tasks. No deletion is authorized by this report-only instruction.

## Start-menu watcher: no demonstrated cure

NI/Siemens disablement is recorded at 09:00–09:02 on 21 September; shell package
re-registration completed at 09:04–09:05 after initial resource-in-use failures.
The CSV sampler stops at **09:09:39**. Its five samples from 09:05:29 through 09:09:39
show StartMenuExperienceHost PID 11964 and Explorer responding. This is only four
minutes of post-maintenance sampling, not a full-day success result.

A later incident capture exists at **23:13:48**, after that disablement and a reboot
recorded at 09:33:52. Its shell-process table has **no StartMenuExperienceHost**;
Explorer had restarted at 18:35:36, while reported free RAM was 20.6 GB and CPU low.
The installed StartMenuExperienceHost package still reports Status=Ok. This establishes later process absence, not by itself a failed Start click, and
the watcher does not prove the problem cured. These
files do not record a direct Start-button click/result or prove the cause: **whether
Start visibly failed again is not conclusively recorded**. No causation claim against
NI/Siemens/OPC Bonjour follows. No watcher restart or repair was performed here.

Sources: pc-maintenance-20260921/apply-log.txt, shellwatch-logs/shellwatch-20260921.csv,
and capture-20260921-231348.txt. The sampler checks processes, not UI functionality.

## OPCF Bonjour — installed by Siemens TIA Portal V20 via OPC discovery

Service `OPCF Bonjour Service`, Manual/Running, points to
`C:\Program Files (x86)\Common Files\OPC Foundation\UA\Discovery\bin\mDNSResponder.exe`.
File metadata says Apple Bonjour 3.0.0.2; its Authenticode signature validates with
OPC Foundation as signer. This is the OPC-discovery distribution of Bonjour.

Installed product **OPC UA Local Discovery Server 1.04**, publisher OPC Foundation,
version 1.04.405.481, product code `{D8E79A81-2606-4662-8BCA-5F6AC88C0710}`, has registry
InstallDate 20260205. Its cached MSI `C:\Windows\Installer\88a00e.msi` was opened
read-only; the File table includes `mDNSResponder.exe.4E6EDC62C0B245CF831D3C5D4806F990`
with filename `MDNSRE~1.EXE|mDNSResponder.exe`. No MSI install/repair was invoked.
Windows Installer's read-only ComponentPath lookup for component
`{FB17703D-2F47-4A1E-BF5A-07B2D6303211}` in that product resolves to the EXACT running
service executable path above, establishing installed-file ownership.

**The parent installer is confirmed: Siemens SIMATIC TIA Portal STEP 7 V20 - WinCC
V20, on 5 February 2026.** Its retained Installer Assistant log
`SIA_STEP7_S7FPLUS_WINCC_V20@26-02-05_12-24-05.log` identifies the bundle at line 197
and, at line 18850 / 12:31:18, launches OPC UA Local Discovery Server 1.04.405.481
through VersionWrapper and directs logging to `install_OPCUALDS_1.04.log`.
The latter records Bonjour's registration action at line 2131 / 12:31:23 using this
exact mDNSResponder.exe with its install argument, then successful product installation
at lines 2392–2394 / 12:31:27 (status 0). Both logs are under
`C:\ProgramData\Siemens\Automation\Logfiles\Setup` on THIS laptop.
This is direct installer-chain evidence, not an inference from matching dates.
No installer, service registration or repair action was executed by this investigation.
Do not remove the dependency without engineering review.

## Everyone ACLs

**YES, Everyone = Full Control remains on both C:\ and C:\Users.** Each has an
explicit Allow ACE for SID S-1-1-0, mask 2032127 (FullControl), ObjectInherit and
ContainerInherit, not inherited. C:\ owner is TrustedInstaller; C:\Users owner is
SYSTEM. Additional sandbox-specific DENY entries do not cancel this broad grant
for every user. This report performs no repair; blanket recursive ACL resets would
need a separate, reviewed plan. The newly authorized C:\SESS-CA folder has its own
restricted ACL; that does not repair either parent folder.

Local retained metadata: local-evidence/consolidated-doc-drafts/laptop-security-readonly.json
and laptop-task-recheck-late.json. These are laptop observations, not server evidence.
