# Witness harness: resume markers and memory guard

**Proposal only. Approved in principle by the Technical Director on 23 September 2026,
scheduled for AFTER go-live on 8 October 2026. Nothing here is built. Do not start it
before go-live: every engineering day until then belongs to the launch, and changing the
acceptance harness during acceptance is the wrong risk to take.**

Estimate: **3 to 4.5 engineering days** for both parts together, including tests and a
witnessed proof. They are estimated together because they share the same plumbing; built
separately they would cost more. Allow the usual ±50% until design.

Build order when the time comes, as directed: **(3) key the run on the manifest digest**
and **(4) the orphan sweep with refusal on a live foreign cluster** first. They are the
parts that prevent wrong evidence and wasted machine time, and they are useful even if
the rest is never built.

## Why this exists

The harness is [tools/Run-WorkflowWitness.ps1](../../tools/Run-WorkflowWitness.ps1). It
runs the three witness gates — `WorkflowWitness`, `ConcurrencyWitness`,
`MigrationLifecycleWitness` — one after another, each with its own build because each gate
is a compile-time constant, and exits non-zero if any gate fails. A complete run takes
about four and a half hours: measured 21 September 2026, Workflow 1h56m, Concurrency
1h29m, MigrationLifecycle 56m.

It has no memory of what it has already done. Two interruptions on 22 September 2026 show
what that costs:

| When | What happened | What was left behind |
|---|---|---|
| 19:44 start, 21:14 power-off | User-session power-off during the Workflow gate. Windows User32 event 1074, shutdown 21:14:18, boot 21:15:12. | No TRX. Concurrency and Lifecycle never started. Directory retained as `2026-09-22-round4-poweroff-interrupted`. |
| 21:59 restart | The resume wrapper disabled the scheduled task, started the gates, and the session ended inside its `try` block. | No TRX. The `finally` never ran, so `NexaERP nightly witnesses` was left **Disabled** and went unnoticed until 23 September. |

Both runs also leaked their disposable clusters. Four orphan directories totalling
467 MB were found under `%TEMP%\advance-postgresql-parser-*` on 23 September and removed
under [this receipt](../../local-evidence/consolidated-B-round4/orphan-cluster-cleanup-20260923.json).

Neither interruption was a test failure, and neither was anyone's mistake — but the
harness could not say so by itself. The closure report had to **argue in prose** that an
interrupted run is "not a pass or a test assertion failure". That belongs in a file the
harness writes, not in a paragraph a reader has to trust.

## Part A — resume markers

### A1. Per-gate completion marker

After a gate's TRX is written and its exit code known, write `<gate>.done.json` next to
the TRX containing: gate name, exit code, TRX path and SHA-256, start and finish times,
and the **digest of the 884-file source manifest** the gate ran against.

On startup, skip any gate whose marker exists *and* whose manifest digest still matches
the working tree. A restart then resumes at the first unfinished gate instead of repeating
four and a half hours of work. A changed manifest invalidates every marker and forces a
full run — a resumed run must never mix results from two different candidates.

### A2. In-flight marker

Write `running.json` (gate, process id, start time, manifest digest) before each gate and
delete it on clean completion. If it is present at startup, the previous run died inside
that gate. Record that explicitly as `INTERRUPTED` — **never as a pass and never as a
failure** — and re-run that one gate.

This is the distinction the two 22 September runs could not express.

### A3. Key the run on the manifest digest, not the date — **build first**

Today the evidence directory is `local-evidence/nightly/<date>`. That is why the aborted
21:59 restart wrote a partial directory under the same stamp as an earlier archived run,
and why a run crossing midnight would split its own evidence across two directories.

Key each run on `<manifest-digest>-<run-id>` and keep the date as a label inside the run
receipt. A partial directory then cannot be confused with a result, and a resumed run
lands in the same place as the run it continues.

### A4. Deliberately NOT proposed: boot-triggered auto-resume

A Task Scheduler `-Boot` trigger could resume an interrupted run automatically. **This is
deliberately not proposed, and the Technical Director agreed on 23 September 2026: heavy
tests must never start with nobody present.** A machine that has just restarted may have
restarted for a reason nobody has looked at yet, and a four-and-a-half-hour PostgreSQL
workload is not something to launch at an empty desk.

The marker plus a single documented manual resume command gives the same recovery with a
person in the loop.

## Part B — memory guard

The [runbook's 21 September inspection](go-live-fresh-database-runbook.md) records that
the current protection is **partial**, and that remains true. The specific gaps:

| Gap | Current behaviour | Proposed |
|---|---|---|
| No machine-wide lock | `DisposablePostgreSql` belongs to one xUnit class whose tests run sequentially in one process. Nothing stops a second manual or scheduled test process starting its own cluster at the same time. | A named machine-wide mutex held for the lifetime of a cluster, with a clear refusal naming the holder rather than a silent wait. |
| Partial startup escapes cleanup | `_started` is set only after `pg_ctl start` succeeds. A cluster that starts partially is not stopped. | Set the flag before the start attempt and make stop tolerant of a cluster that never came up, so the data directory is always removed. |
| Stop result ignored | `Dispose` does not check the `pg_ctl stop` exit code, so a failed stop looks identical to a successful one. | Check the exit code, retry once with `-m immediate`, and record a refusal if the cluster is still up. |
| No orphan check between gates — **build first** | The wrapper runs the next gate regardless of what the last one left behind. Four orphans accumulated on 22 September. | Before each gate: remove `advance-postgresql-parser-*` directories with no live postmaster, and **refuse to start if a live cluster exists that this run does not own**. Record what was swept. |
| Process termination bypasses disposal | Nothing survives a power-off. | Partly unavoidable. The orphan sweep above makes the *next* run clean up, which is the achievable half. |

## Boundaries

- Disposable clusters only, one at a time. This proposal does not change that rule; it
  makes it enforceable rather than conventional.
- No owner database, no server, no production data is involved at any point.
- The three gates, their assertions and the full Debug/Release TRX acceptance are
  unchanged. This is harness plumbing, not a change to what is being proved.
- The sweep touches `%TEMP%\advance-postgresql-parser-*` and nothing else, under the same
  name, path and live-owner guards used for the 23 September cleanup.
- The development nightly witness stays on this laptop and is **never** installed on
  DESKTOP-SPF5420. See runbook section 15.2.

## Related

- [Go-live runbook](go-live-fresh-database-runbook.md), step 0 and section 15.2.
- [Backlog after go-live](post-october-backlog.md) — the separate "suite time: about 60
  toward 30 minutes" item, 3-5 days, is **not** this work and should not be merged with it.
- [Rehearsal findings](fresh-company-rehearsal-findings.md), witness-gated tests.
