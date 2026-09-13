# Item 26: failure behavior and automated backup

Status: partial. The first three targeted tests passed in Release and Debug; remaining failure and backup cases are pending. No owner
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
- PR creation appears to commit before writing its audit. Its request has no
  idempotency key field. These are source findings awaiting actual failure and
  duplicate-request tests, not yet witnessed corruption or orphan claims.

## Verification still required

| Scenario | Result |
| --- | --- |
| Database connection lost during posting | Release/Debug: HTTP 500, full rollback, retry/replay succeed once |
| PostgreSQL restarted during posting, durability enabled | Release/Debug: HTTP 500, recovery rolls back, retry/replay succeed once |
| Browser closes between submission and approval | Pending |
| Duplicate network request | Payment covered by Item 25; broader command verification pending |
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
these durations therefore do not sum to the batch wall time. Debug is pending.

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

Next: PR creation duplicate-request and audit-failure baseline, interrupted
migration, the browser-between-steps case, bounded disk-full behavior, and the
automated backup/retention/tested restore path. Item 26 remains partial.