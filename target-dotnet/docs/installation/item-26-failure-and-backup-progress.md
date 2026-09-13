# Item 26: failure behavior and automated backup

Status: partial. Posting interruption, host lifecycle, and PR creation retry/audit-failure tests have passed in Release and Debug; remaining failure and backup cases are pending. No owner
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
| Browser closes between submission and approval | Pending |
| Duplicate network request | Payment covered by Item 25; PR sequential/concurrent retry verified in Release/Debug |
| Disk full on a bounded disposable filesystem | Pending |
| Interrupted migration | Pending |
| Two API instances using one database | Earlier concurrent tests use separate hosts; explicit lifecycle/bind checks pending |
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
