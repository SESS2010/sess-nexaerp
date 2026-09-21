# Automated verified backup and recovery
## Selected production server: DESKTOP-SPF5420 (21 September 2026)

Windows 10 stays by Technical Director decision. A later Windows 11 upgrade is planned;
it is not a prerequisite for this package. NEVER a clean Windows install.
At least weekly copy the newest complete VERIFIED backup, identity/configuration and
external-file backups OFF this machine, verify the copied hashes, and disconnect the
media or protect retained versions from this PC's credentials. Ransomware on this
unpatched OS can encrypt C: and D: together despite their being separate disks.

Option C creates the fresh go-live database directly on this server. Production
PostgreSQL data stays on C: (224 GB WDC SSD, currently 40 GB free). Verified bundles
go to `D:\SESS-Backups` on the separate 932 GB Seagate HDD (D: 284 GB free, E: 339 GB
free). Use the existing `D:\SESS-Backup-Verification` for restore working space, separate
from the existing `D:\SESS-Backups`. Do not clear unmarked nonempty roots. D: and E: share the HDD;
neither is independent of the other, but both are physically separate from C:.
Record the actual disk mapping at installation. SSD failure leaves the HDD bundles
available for recovery. Keep an external/offsite copy for whole-PC loss.

Create the configuration and DPAPI file on DESKTOP-SPF5420 under its scheduled account;
use the new cluster's system identifier, not the laptop's. `backup.example.json`
shows the selected roots but still requires a verified database name/system identifier.
The ERP backup schedule is 18:45; give the separate identity job its own roots and a
non-overlapping window. Verification clusters are permitted ERP backup operations;
development builds, tests and nightly witnesses never run on this server.

At the estimated six-month 5-8 GB PostgreSQL size, 40 GB free on C: is adequate, provided
installation/OS/other growth leaves at least 25 GB free. Target 40 GB or more after
installation and the planned colleague-file moves. Keep dump/restore work off C:.
Reserve roughly twice the restored database size plus margin in WorkingRoot (25 GB
free for this estimate); if on D:, add that to the retained-bundle budget. Allow at
least 20 GB free on D: beyond retained bundles. About 30-42 retained full dumps times
the **measured** bundle size, plus identity/failed-run space, determines backup capacity:
42 x 3 GB = 126 GB; 42 x 8 GB = 336 GB, more than today's 284 GB free on D:. The earlier
under-10-GB retention estimate is withdrawn. Monitor sizes and expand/reallocate space
before the limit; do not silently change retention. See runbook section 15 for cutover.

## 15 September: identity backup extension

Local Keycloak is now the SESS default. Use the same backup commands and scheduler
with a separate identity configuration, connection credential, task name, backup
root and verification root. See keycloak/backup.identity.example.json and
item-16-local-keycloak-operations.md. The owner reports the ERP chain 78 to 103
applied, RECONCILED and VERIFIED; earlier migration failures below are history.

Optional ConfigurationFiles maps safe logical names to explicit absolute files.
When supplied, a format-2 bundle includes configuration-* artifacts, their sizes
and SHA-256 hashes alongside database.dump and globals.sql. Recovery copies them
into the new protected recovery directory; it does not apply them to live
services. Existing format-1 bundles and their checks remain supported. Configuration
files are held open against Windows writes/deletion through backup verification.
Maintain a stable deployment configuration during a run.

Keycloak password hashes, OTP credentials, subjects and realm signing keys are
database contents and are backed up. The PostgreSQL role passwords deliberately
omitted from globals are separate service credentials. A successful identity
restore must additionally prove password/TOTP login with the original subjects.

Both ERP and identity schedules must be fresh and successful. The operations
monitor checks both. Back up any remaining external ERP attachment/object storage
separately; adding identity does not make unrelated external files automatic.
The identity bundle contains authentication secrets: restrict access and use
independent encrypted storage. This implementation does not itself encrypt dumps.

New recovery witness and full-suite acceptance: pending; see the current Item 16
operations evidence rather than treating earlier ERP-only witnesses as proof.


Release and Debug verification each passed eight targeted tests, including a real scheduled run and recovery. This is not a
deployed customer schedule. No owner database was accessed during these tests.

The Installer supports:

```text
SESS.NexaERP.Installer.exe backup run --config C:\SESS-Backup\backup.json
SESS.NexaERP.Installer.exe backup verify --config C:\SESS-Backup\backup.json --bundle D:\SESS-Backups\run-<id>
SESS.NexaERP.Installer.exe backup recover --config C:\SESS-Backup\backup.json --bundle D:\SESS-Backups\run-<id> --destination C:\SESS-Recovered
```

Copy backup.example.json and replace every placeholder with the reviewed source
identity and real paths. The database connection comes from the environment
variable named in ConnectionEnvironment; it is never a command-line argument.
The setup engineer supplies a connection able to dump all ERP data and global
role definitions and read pg_control_system. The ERP runtime account is
insufficient. The test source uses a disposable administrator connection;
a separately restricted backup account has not been witnessed.

Use PostgreSQL 17 tools and a destination on an independent physical disk, external
media or network storage that supports the required directory permissions. The selected
server uses D: on its separate HDD plus the required weekly off-machine copy. Use a UNC path for network storage; mapped drive letters may be unavailable to a scheduled account. WorkingRoot is a separate
local directory used for temporary verification clusters. The backup root
must initially be empty. The Installer creates a private ownership marker and
refuses unmarked nonempty directories, overlapping roots and reparse points.
A same-physical-disk backup does not protect against that disk failing; the selected
server's C:-to-D: layout avoids that limitation. E: is another partition of the D: HDD.

Every run exports a read-only REPEATABLE READ database snapshot, captures
table counts and selected schema/security metadata, and creates a custom
pg_dump archive using that snapshot. Matching global role definitions are
saved with every daily backup, without password hashes. Global roles have no
shared database snapshot; observed role changes during backup cause refusal.
Source TLS mode, root certificate and client-certificate paths are forwarded
to the PostgreSQL tools. Encrypted client-key passwords are currently refused.

A new private PostgreSQL cluster restores the globals and database, retaining
the source bootstrap role name, object ownership, grants and database-level
settings. Its filesystem and source-system identity are checked. It binds a
random loopback port with fsync and synchronous_commit on and a generated
SCRAM password. Tablespace placement is not reproduced. Locale, extension or
other restore incompatibilities must fail verification.

Only a successful restore, matching table counts and selected schema/security
metadata, and unchanged SHA-256/size checks permit a VERIFIED manifest.
Counts and metadata are not a proof that every data field was independently
compared. The archive itself has a SHA-256 digest. A failed run retains its
evidence and does not qualify for successful-backup retention.

Daily bundles are kept 30 days; runs starting on Sunday UTC are weekly bundles
kept 84 days. The newest two verified bundles remain protected regardless of
age. Retention runs only after a new backup is verified. It deletes only
recognized owned bundles whose hashes still match, refuses unexpected files,
and keeps matching globals with every retained database dump. A retention
failure can leave a newly verified backup; inspect its manifest separately
from the failed command exit code.

## Schedule setup

Use a dedicated Windows account with the required source, PostgreSQL binary,
backup destination and local working-directory access. Publish the Installer
to a durable installation path. Keep the two scheduling scripts together in
a durable tools directory. Do not schedule binaries in temporary test output.

Sign in as the account that will run the task. Store the complete database
connection string with Windows DPAPI in a protected configuration directory:

```powershell
Read-Host 'Database backup connection string' -AsSecureString |
    Export-Clixml -LiteralPath 'C:\SESS-Backup\connection.clixml'
```

The file is tied to that Windows account and machine. Do not paste the secret
into a script, task argument, repository or report. Create a protected log
directory, then have the setup engineer register the daily task:

```powershell
$backupAccount = Get-Credential
.\Register-VerifiedDatabaseBackup.ps1 -InstallerPath 'C:\SESS-Backup\Installer\SESS.NexaERP.Installer.exe' -ConfigPath 'C:\SESS-Backup\backup.json' -CredentialFile 'C:\SESS-Backup\connection.clixml' -LogDirectory 'C:\SESS-Backup\logs' -DailyAt '18:45' -Credential $backupAccount
```

The task starts when available, allows battery operation, does not overlap
another run, and retries failures twice at five-minute intervals. It requires
the scheduled-account credential for operation while signed out.
CurrentUserWitness is only a disposable logged-in-account test mode.

Check Task Scheduler's Last Run Result, logs/last-run.json and the latest
bundle's manifest.json. A file merely existing is insufficient. Customer
registration and destination/account configuration remain deployment work.


These commands cover the ERP PostgreSQL database and global roles. Retain
application configuration, external attachment/object-storage data and any
local identity provider's data through their own backup procedures. Restore
those dependencies before enabling users after a complete site loss.

## Restore prerequisite: preserve the original administrator identity

For the SESS backup restored on 14 September 2026, the original PostgreSQL
bootstrap administrator is `postgres`. Initialize a fresh recovery cluster
with that same name (`initdb --username=postgres`), before restoring globals.
This is the original bootstrap role (OID 10), not merely any superuser named
`postgres` created later. PostgreSQL preserves role-grant attribution: our
restore into a cluster initialized under a different administrator name
refused membership grants recorded as `GRANTED BY postgres`.

The Installer's `backup recover` already reads the source bootstrap name from
its backup evidence and passes it to `initdb`. The IT team must preserve this
rule when preparing a fresh server or carrying out a separately reviewed
manual restore. Do not initialize or reinitialize an existing server as part
of these instructions; use the new, absent recovery destination below.

When restoring globals, omit only the one `CREATE ROLE` statement for that
already initialized bootstrap role from a private working copy. Retain its
`ALTER ROLE` and every `GRANT`, including grantor attribution. The Installer
requires exactly one matching creation statement and refuses an unexpected
file shape. Do not blanket-remove grants or replace administrator names.
Keep the original globals file unchanged and private; externally supplied
files may contain password hashes even though Installer bundles omit them.

Verify restored role attributes, memberships and grantors as well as object
ownership before enabling the ERP. A successful database archive restore
alone does not establish that the globals restored correctly. This lesson
was reproduced during the Item 26 witness and the Tuesday backup restore;
see [Tuesday migration-chain evidence](tuesday-migration-chain-witness-2026-09-14.md).
Successful restore is separate from successful upgrade: that exact-name
78-applied copy subsequently stopped at `20260913060000_CommandReceiptReplay`.

## If the production SSD or whole PC fails

1. If only the C: SSD failed, preserve the D: HDD backup; if the whole PC is lost,
   use the external/offsite copy. Select the newest accessible VERIFIED bundle;
   retain its database.dump, globals.sql and manifest.json together.
2. On the replacement machine, install the compatible PostgreSQL 17 binaries,
   required extensions and the Installer. Restore the backup root and ownership
   marker from independent storage. Recreate the reviewed configuration using
   the original expected source-system identifier. Recovery does not connect
   to that old source server.
3. In a private administrator session, securely set the process environment
   variable NexaErp__RecoveryBootstrapPassword to a new password of at least
   14 characters. Never put it in command arguments.
4. Run backup recover into a new, absent destination. The command verifies
   hashes, restores into a fresh private cluster, compares counts and metadata,
   stops it and writes RECOVERED.json. Do not start the API after a failed
   recovery. The old server/database is never overwritten by this command.
5. The setup engineer configures the recovered server's service, network,
   authentication rules and persistent storage. Re-establish database service
   passwords: globals deliberately omit them. Verify the ERP principal grants,
   configure the API connection and verify access before enabling users.
6. Recreate the DPAPI backup connection file under the replacement scheduled
   account, register the schedule, and run a verified backup immediately.
   Passwords/DPAPI files from the old machine are not a recovery dependency.

The recovery-point loss is everything since the latest usable backup, not
necessarily midnight. This implementation does not provide WAL archiving or
point-in-time recovery.

References: [pg_dump snapshot](https://www.postgresql.org/docs/17/app-pgdump.html),
[pg_restore](https://www.postgresql.org/docs/17/app-pgrestore.html),
[global role dumps](https://www.postgresql.org/docs/17/app-pg-dumpall.html),
[initdb authentication](https://www.postgresql.org/docs/17/app-initdb.html),
[task settings](https://learn.microsoft.com/en-us/powershell/module/scheduledtasks/new-scheduledtasksettingsset?view=windowsserver2025-ps).
