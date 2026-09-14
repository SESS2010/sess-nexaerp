# Automated verified backup and recovery

Release and Debug verification each passed eight targeted tests, including a real scheduled run and recovery. This is not a
deployed customer schedule. No owner database was accessed during these tests.

The Installer supports:

```text
SESS.NexaERP.Installer.exe backup run --config C:\SESS-Backup\backup.json
SESS.NexaERP.Installer.exe backup verify --config C:\SESS-Backup\backup.json --bundle E:\SESS-Backups\run-<id>
SESS.NexaERP.Installer.exe backup recover --config C:\SESS-Backup\backup.json --bundle E:\SESS-Backups\run-<id> --destination C:\SESS-Recovered
```

Copy backup.example.json and replace every placeholder with the reviewed source
identity and real paths. The database connection comes from the environment
variable named in ConnectionEnvironment; it is never a command-line argument.
The setup engineer supplies a connection able to dump all ERP data and global
role definitions and read pg_control_system. The ERP runtime account is
insufficient. The test source uses a disposable administrator connection;
a separately restricted backup account has not been witnessed.

Use PostgreSQL 17 tools and an independent external or network destination
that supports the required directory permissions. Use a UNC path for network storage; mapped drive letters may be unavailable to a scheduled account. WorkingRoot is a separate
local directory used for temporary verification clusters. The backup root
must initially be empty. The Installer creates a private ownership marker and
refuses unmarked nonempty directories, overlapping roots and reparse points.
A same-disk backup does not protect against the laptop disk failing.

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
.\Register-VerifiedDatabaseBackup.ps1 -InstallerPath 'C:\SESS-Backup\Installer\SESS.NexaERP.Installer.exe' -ConfigPath 'C:\SESS-Backup\backup.json' -CredentialFile 'C:\SESS-Backup\connection.clixml' -LogDirectory 'C:\SESS-Backup\logs' -DailyAt '02:00' -Credential $backupAccount
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

## If the laptop disk fails

1. Keep the external/network backup safe. Select the newest VERIFIED bundle;
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
