# Item 26: failure behavior and automated backup

Status: partial. Posting interruption, host lifecycle, PR creation retry/audit-failure, migration rollback and client reconnect tests have passed in Release and Debug; remaining failure and backup cases are pending. No owner
database has been accessed. Tests must use owned disposable PostgreSQL clusters;
crash-recovery claims require fsync and synchronous_commit enabled.

## What is known before further changes

- A real Item 25 payment serialization failure occurred at command-receipt
  insertion, after its business writes. Commit 85e3e40 adds transaction-boundary
  error conversion. Release and Debug verify rollback, explicit retry and replay
  with a deterministic reproduction of that SQLSTATE. This is not connection
  loss, power-loss or disk-full evidence.
- The notification worker catches exceptions from scope creation and refresh,
  logs them, and retries on its next one-minute interval. The source therefore
  does not support the claim that a normal refresh exception currently crashes
  the host. Actual lifecycle tests are still required.
- The API separately validates its database principal before starting. Database
  unavailability at that point is distinct from a worker refresh failure.
- The existing backup helper is manual and tied to the owner database. It will
  not be executed in this session. The existing restore helper only writes a
  plan; it is not evidence of a successful restore.
- PR creation baseline reproduced duplicate drafts and a draft surviving audit failure. The correction and Release/Debug evidence are recorded below.

## Verification still required

| Scenario | Result |
| --- | --- |
| Database connection lost during posting | Release/Debug: HTTP 500, full rollback, retry/replay succeed once |
| PostgreSQL restarted during posting, durability enabled | Release/Debug: HTTP 500, recovery rolls back, retry/replay succeed once |
| Browser closes between submission and approval | Release/Debug: HTTP client closure preserves pending work; browser UI not exercised |
| Duplicate network request | Payment covered by Item 25; PR sequential/concurrent retry verified in Release/Debug |
| Disk full on a bounded disposable filesystem | Pending |
| Interrupted migration | Release/Debug: DDL/history roll back; retry applies once; rerun is unchanged |
| Two API instances using one database | Release/Debug: shared pending/approved state and duplicate refusal; same-port bind failure separately verified |
| Notification refresh failure and recovery | Release/Debug: first refresh fails, real one-minute retry succeeds, host stays alive |
| Scheduled daily backup and weekly globals | Not implemented |
| Retention and verified restore | Not implemented |
| Customer disk-failure procedure | Pending tested restore path |

An interruption witness will gate the command receipt after business writes,
terminate the specific posting backend or restart only its owned test server,
and inspect business, ledger, audit and receipt rows before retrying through
HTTP. A template clone is not a dump/restore test. Disk-full tests must never
fill the PC's working volume.
## First runtime evidence

The Release build passed with zero warnings/errors in 4m27.91s. The initial
batch passed **3 tests, 0 failures, 0 skips, 7m25s**. Connection-loss/full-flow
case: 4m11.714s; restart/full-flow case: 3m13.534s; host lifecycle case: 1m00.327s.
The host test is in a separate test class and can overlap the database tests;
these durations therefore do not sum to the batch wall time. Debug results follow below.

Both posting cases assert fsync=on and synchronous_commit=on. A temporary
receipt trigger blocks after the actual issue, FIFO, stock and audit writes.
The test observes that specific backend waiting at commit_command_receipt,
then terminates it or immediately stops and restarts only its owned temporary
PostgreSQL cluster. Restart refuses non-durable fixtures and port 5432.

Before and after interruption, the stored state is identical: 2 issues, 2 issue
lines, 13 issue-history rows, 9 batches, 17 movements, 2 FIFO consumptions,
147 audits, 107 command requests and 107 receipts. The target MIR remains
APPROVED at Version 2. After retry and replay there is exactly one additional
issue/line/history/batch/FIFO/audit/request/receipt and two movements; the MIR is
FULFILLED at Version 3. Retry and replay return the same issue ID. The parent
then completes the full three-band purchase, custody, costing and report flow.

Current responses are **HTTP 500, failing cleanly in these cases**. No production
failure-handling change was needed to make these assertions pass. Connection
loss took 1.8068744s, retry 0.8989678s, replay 0.3837014s. Restart took 2.2031990s,
retry 0.7809599s, replay 0.3806622s. These timings include deliberate interruption
and lock waits; they are not production posting latency. Logs contain no 40P01.

The actual notification worker runs in a Kestrel host with the default
BackgroundServiceExceptionBehavior.StopHost retained. Its injected first
processor failure is caught; the real one-minute retry succeeds and the host
continues answering health requests. A second Kestrel host on the same address
fails to bind with IOException, while the first remains alive. Failing that
second startup is appropriate; silently choosing a different port would obscure
the deployment error. This test covers worker and bind lifecycle, not the
separate production database-principal startup check.

Verified Release JSON/logs and the TRX are retained under local-evidence/item26.
The connection-loss and restart files use -verified-release suffixes. No
corruption, partial posting or orphan was observed in these two interleavings;
this does not cover disk-full, interrupted migration, arbitrary disconnect
points or an unverified restore.
## Verified Debug checkpoint

Debug build passed with zero warnings/errors in 4m27.28s. The same **3 tests
passed, 0 failures/skips, 7m32s**: connection-loss/full flow 4m19.905s,
restart/full flow 3m12.833s, host lifecycle 1m00.353s (separate test classes may
overlap). The same rollback, one-issue retry/replay, full three-band flows and
host-lifecycle assertions pass. This is a targeted count, not a new full-suite
total. Debug files are retained with -verified-debug suffixes.

Debug connection loss: HTTP 500 in 2.0662438s, retry 201 in 0.5474471s, replay
201 in 0.5218993s. Restart: HTTP 500 in 2.0553903s, retry 201 in 0.8875327s,
replay 201 in 0.6068739s. Stored pre-command and post-failure state match exactly.
No production code or frontend contract changed in this checkpoint.

The PR creation investigation and correction follow below. Item 26 remains partial.
## PR creation retry and audit failure

Baseline Release execution reproduced two defects: two identical POST requests
with the same Idempotency-Key returned 201 with different PR IDs and left two
drafts. A forced CreateDraft audit insertion failure returned 500 after the PR
had already committed; one draft survived without its creation audit. Retrying
then left two drafts with only one creation audit. Baseline JSON and PostgreSQL
log are preserved as pr-create-failures-baseline-release files.

The correction puts PR creation, status history, creation audit and command
receipt in one transaction. The existing immutable command ledger binds the
caller key to the request content, company, employee, verified issuer/subject,
role and effective assignment. Its database-generated command ID becomes the
new PR ID. A restricted SECURITY DEFINER reader returns only the stored receipt
for the registered actor context; runtime still cannot SELECT ledger tables.
Receipt replay retrieves the same accessible PR, with its current details.

**Frontend contract change:** POST /api/v1/purchase/requisitions now requires one
caller-supplied Idempotency-Key header, trimmed length 1–200. Preserve that key
and the original body when retrying an uncertain result; use a new key for a
new intentional PR. Missing keys return 400 after existing request/scope
validation. Reuse with changed body or actor authority returns 409. Replays
return 201 with the same PR ID; details can reflect its subsequent workflow
state. A concurrent registration conflict asks the client to retry the original
request. Production does not generate a substitute key.

The guarded migration 20260913060000_CommandReceiptReplay adds the reader and
has PostgreSQL/provider, protected-database, existing-function and exact
rollback-body checks. Installer provisioning restores its explicit runtime
EXECUTE grant and verifies ownership, SECURITY DEFINER, fixed search path and
ACLs. A real PostgreSQL test verifies Up, Down, reapply, provisioning twice,
42501 for a receipt read without registered context and 42501 for direct ledger
table reads. No business table shape changed and no pending model change exists.

Release build: zero warnings/errors, 4m19s for production changes; final fixture
build 24.36s. Final targeted batch: **2 passed, 0 failed, 0 skipped, 4m54s**.
PR/full-three-band case: 4m25.876s; migration/ACL case: 29.059s. This is not a
new full-suite total. Earlier runs exposed a validation-order regression and a
legacy fixture using its disposable administrator connection for PR creation;
the scope-first response was restored and fixture creation now uses restricted
runtime. Those failures are preserved separately.

Release observed sequential retry: 201/201, same ID, one draft (0.9043s/1.1052s).
Concurrently dispatched same-key requests: 201/201, one ID/draft/audit
(0.7950s/0.9287s); replay 201 in 1.0409s. This run did not take the concurrent
registration 409 branch. Missing-key and changed-body checks pass without
creating drafts. Forced audit failure: 500 in 0.8266s, zero draft and zero
command registration; retry: 201 in 1.0507s, one draft and one creation audit.
The retained failure-phase PostgreSQL log records the injected P0001. These
are local witness timings, not production latency measurements.

Debug verification passed: build zero warnings/errors in 4m15.99s; **2 passed,
0 failed, 0 skipped, 4m50s**. PR/full-flow case: 4m22.148s; migration/ACL:
28.237s. Sequential retry: 201/201 (1.1485s/0.8371s), concurrent dispatch:
201/201 (0.7745s/0.8154s), replay 201 (0.9768s). Audit failure: 500 (0.8374s),
zero surviving draft/registration; retry 201 (1.1097s), one draft and one
creation audit. The same ID/count assertions pass in both configurations.
Verified artifacts use -verified-release and -verified-debug suffixes.

Next: interrupted migration, client closure between completed submission and
approval, bounded disk-full behavior, and automated verified backup/retention
with a tested customer restore procedure. Item 26 remains partial.

## Interrupted migration and client closure

Release and Debug now verify both cases without further production changes.
Release build: zero warnings/errors, 4m40.71s; final test-only rebuild 30.99s.
Release batch: **2 passed, 0 failed, 0 skipped, 4m56s** (migration 1m16.544s,
client/full-three-band flow 3m40.223s). Debug build: zero warnings/errors,
4m27.39s; batch: **2 passed, 0 failed, 0 skipped, 5m07s** (migration 1m18.376s,
client/full-three-band flow 3m49.201s). These are targeted counts.

The migration test uses the production advance.__EFMigrationsHistory location
in a new owned cluster and asserts fsync=on and synchronous_commit=on. A
temporary trigger blocks the target migration's history INSERT after its DDL
has executed. The test observes that exact named backend at the advisory wait
and terminates it. Before and after interruption match: 84 history entries,
no receipt-reader function, zero command requests and receipts. PostgreSQL
reports termination by administrator command. Retrying through EF migrator
creates the function with the expected owner/search path/ACLs and exactly the
85th history entry. Another migrator run changes nothing. This proves the
transaction boundary for this migration; it is not a claim about arbitrary
nontransactional migrations. The deliberate wait is not normal migration
latency.

The client test starts after committed submission and department verification.
An HTTP client reads a PENDING_APPROVAL PR at Version 2, then is disposed before
any approval request. Status-history/approval-history/audit counts remain
3/0/3. A second running Kestrel instance against the same database reads the
same PR and Version 2. Approval advances it to Version 3 with counts 4/1/4.
A fresh client against the first instance sees that same committed state.

Submitting the original approval again returns **409 CONCURRENCY_CONFLICT,
stale record version**, leaving all counts unchanged. Refresh sees Version 3.
That refusal took 0.2735s in Release and 0.3256s in Debug. The initial test
incorrectly expected a 200 replay; its result is retained separately, and the
corrected test asserts the existing clean refusal. The successful first
approval survives the client closure; there is no extra approval or orphan.

This is an HTTP-client lifecycle test with real persisted role assignments,
page permissions and operational scopes, plus two running API instances.
It does not exercise browser UI or an in-flight uncommitted submission.
The parent completes the full three-band purchase/custody/report flow.
Verification JSON/logs are stored under local-evidence/item26 with
-verified-release and -verified-debug suffixes.

Next: actual disk-full behavior on the bounded VM filesystem, then scheduled
backup, retention, automatic restore verification and the customer recovery
procedure. Item 26 remains partial.

## Actual bounded disk exhaustion

Release now passes an actual ENOSPC witness, with no production changes.
Build: zero warnings/errors, 29.05s. Targeted test and its full three-band parent:
**1 passed, 0 failed, 0 skipped, 6m48s**. Debug results are recorded below.

The test verifies the separate native PostgreSQL cluster identity and a marked
32 MiB loop filesystem before writing a uniquely named filler. fsync and
synchronous_commit remain on. A restored purchase-flow database retains role
attributes and recorded membership grantors. A temporary receipt trigger writes
an 8 MiB probe after real material-issue, FIFO, stock and audit work. PostgreSQL
fails that write with **53100, No space left on device**.

Before and after the failed request are identical: issues/lines/FIFO 2/2/2,
issue history 13, batches 9, movements 17, audit 147, command requests/receipts
110/110; MIR APPROVED, Version 2. The failed HTTP request returns 500 in
7.8747 seconds. Removing the filler and retrying returns 201 in 6.8318 seconds;
replay returns 201 in 1.4915 seconds with the same issue ID and Replayed=true.
Counts increase once: one issue, line, history, batch, FIFO consumption, audit,
command and receipt; two stock movements. MIR becomes FULFILLED, Version 3.
The captured PostgreSQL log contains no 40P01. These emulated-VM timings are
not production performance measurements.

The native VM exercises the prepared prefix and issue/retry. The original
Windows fixture separately completes the full three-band parent flow. The
bounded probe models relation/tablespace exhaustion during receipt creation;
it is not WAL-volume exhaustion, whole-server disk exhaustion or physical
disk-loss proof. Only the unique filler and restored disposable database/
tablespace are removed; the retained VM disk is not formatted.

Two earlier fixture failures are retained: pg_dumpall role restoration failed
when the target OID-10 bootstrap role had a different name; after preserving
the original bootstrap identity, a missing probe-table owner privilege caused
42501 before disk exhaustion. Neither failure is counted as an ENOSPC pass.
The corrected setup retains all ALTER ROLE and GRANT statements, omitting only
the existing bootstrap role's CREATE ROLE. PostgreSQL documents this grantor
limitation in its [project discussion](https://www.postgresql.org/message-id/671134.1778008247%40sss.pgh.pa.us).

No API route, field or response envelope changed in this test-only increment.
Automated backup, scheduling and recovery implementation remain in progress.

Debug verification passed: build zero warnings/errors in 4m25.44s; targeted
test and full parent **1 passed, 0 failed, 0 skipped, 6m58s**. The same 53100,
rollback and exactly-once retry assertions pass. Failure: 500 in 7.1295s;
retry: 201 in 6.6600s; replay: 201 in 1.8499s. Final counts match Release.
Verified JSON and PostgreSQL logs have -verified-release/-verified-debug
suffixes under local-evidence/item26. These are targeted counts, not new full
suite totals. The opt-in build property is DiskFullWitness=true; the test
requires the explicit owned VM endpoint and system identifier.

## Automated backup and recovery: Release witness

The Installer now has backup run, verify and recover commands, plus Windows
daily scheduling scripts. No owner database was accessed. The source is the
completed three-band disposable witness, with matching global role definitions
saved on every run. Customer schedule activation remains separate: a durable
installation, independent destination and scheduled account must be configured.

Release build: zero warnings/errors, 4m27.74s initially; final revised build
31.14s. Final targeted batch: **8 passed, 0 failed, 0 skipped, 6m14s**.
It includes the full three-band backup/recovery case, two retention cases and
five SQL-normalization cases. Debug results are recorded below.

All 222 table counts match after restore, including 3 purchase orders,
28 stock movements, 3 FIFO consumptions and 128 command/receipt pairs.
Source and restored role attributes/membership grantors, object ownership,
effective ACLs, function bodies/search paths/security-definer flags, extension
versions, constraints, indexes and other selected catalog metadata match.
The archive is 2,497,672 bytes for this small witness database.

Direct backup plus automatic restore verification: 36.0196s. Recovery to a
retained, stopped new cluster: 29.0323s. The test restarts that recovered cluster,
validates ERP principal grants, resets the runtime credential, connects as
runtime and reads the expected employee count. Direct command-ledger SELECT
still fails with 42501. A deliberately changed globals file is refused by hash/
size validation before starting another restore; its original bytes are then
restored in this disposable test bundle.

A real Windows daily task was registered under a unique witness name,
explicitly triggered once, and removed before source disposal. It produced a
second VERIFIED bundle. Scheduled verification took 38.9967s from manifest
start to verified publication; task startup is outside that measurement.
LastTaskResult=0 and the wrapper ExitCode=0. Exported task XML, result, cleanup
confirmation and both manifests are retained. No witness task remains.

This tests the current-user interactive task mode. The production registration
path accepts the Windows account credential for operation while signed out;
that customer's account, network storage access and signed-out execution have
not been deployed or witnessed. Database secrets are supplied through a DPAPI
SecureString file for the scheduled account, not task arguments.

The first integration attempt restored successfully but refused raw catalog
differences: dropped-column physical numbering, explicit owner ACLs versus
default ACLs, and PostgreSQL's alternate equivalent expression formatting.
Its evidence remains unverified. Comparison now uses logical active-column
positions and effective default ACLs, with narrow quote-aware normalization
for literal unbounded varchar-to-text array casts. Tests keep bounded casts,
quoted identifiers, quoted content and changed values distinguishable.
Definition checks were retained.

Retention keeps daily bundles 30 days, Sunday-UTC weekly bundles 84 days and
at least the newest two verified bundles. Policy tests show expired recognized
bundles removed while newest, weekly-window and unfinished bundles remain;
unexpected files cause refusal without their deletion. These retention-only
fixtures are not additional database-restore witnesses.

Release evidence:
local-evidence/item26/automated-backup-d76e10608fb140b9ac012d08624833f2
and item26-backup-scheduled-release.trx. Earlier metadata-comparison failures
and the intermediate passing run remain separately recorded.

There is no API route, frontend field or envelope change. New commands are
Installer/operator interfaces. See automated-backup-and-recovery.md and
backup.example.json for deployment and tested recovery steps. Same-disk test
artifacts do not prove protection against physical laptop disk loss, and this
is not WAL archiving or point-in-time recovery.

Debug build passed with zero warnings/errors in 4m22.75s. The same final
targeted batch passed **8 tests, 0 failed, 0 skipped, 6m09s**. Direct backup and
verification: 34.3673s; recovery: 28.2938s; scheduled verification: 40.0881s.
All 222 table counts and catalog/permission checks match. The archive is
2,497,711 bytes in this run. LastTaskResult=0, wrapper ExitCode=0, two verified
bundles, and task removal is confirmed. Debug evidence is
local-evidence/item26/automated-backup-d8ef9af0d088468990004d3a35708746
with item26-backup-scheduled-debug.trx.

Item 26's implemented failure cases and automated database backup/recovery
capability are now witnessed in both builds. Customer installation/scheduling
remains pending; no owner database or customer schedule was touched.
Complete site recovery also requires separately retained application
configuration, external attachment/object storage and any local identity
provider data. The ERP database archive does not contain those external stores.
The actual disk-full witness remains bounded relation/tablespace exhaustion,
not WAL-volume or whole-server exhaustion.
