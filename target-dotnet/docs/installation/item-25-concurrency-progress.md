# Item 25 concurrency — partial checkpoint

Parent: 8214992 (partial Item 15). No owner database was accessed. This work fixes and verifies return/fitment lock ordering; it does not complete Item 25's race matrix.

## Reproduced failure and correction

The first real HTTP race reproduced PostgreSQL **40P01: deadlock detected**. Return acceptance held the issue header while fitment held its issue line and waited for that header. Return's movement insertion then needed the line's foreign-key lock, completing the cycle. PostgreSQL aborted fitment; its HTTP 409 alone would have hidden the deadlock.

The new migration **20260913020000_FitmentIssueHeaderLockOrder** makes fitment read the parent reference without locking the line, lock the issue header, then lock the line and revalidate its parent. Existing job, authority, quantity, custody and costing checks remain. Up and Down have PostgreSQL/provider/protected-database guards and refuse an unexpected function body. The earlier landed-cost migration is unchanged. Down/reapply passes on disposable PostgreSQL.

## Witness and expected rows

Each concurrent request has a separate API host, actor object, service scope and restricted runtime connection. The fixture supplies the authenticated actor identity; production page-permission, operational-scope and database-command authority checks execute.

An observer holds the return function's existing posting advisory key. The test observes return blocked on that key using pg_blocking_pids, starts fitment, observes fitment blocked on return, and releases the gate. It adds no altered business function, manual data-row lock or disabled posting constraint. PostgreSQL verbose diagnostics distinguish 40P01 deadlocks from expected 40001 serialization refusals.

Two cases now have passing Release evidence:
- Non-serialized: .60 returned from a .95 issue while a .95 fitment competes.
- Same serial: one unit is issued against the existing open job through a real MIR and Technical Director excess approval (.90 BOM baseline, .10 excess), then that exact InventorySerialId is returned while a one-unit fitment competes.

Both require return HTTP 200, fitment HTTP 409, **one accepted return, one posting batch, two movement legs and zero competing fitments**, with no deadlock diagnostic. The serial case verifies both return legs retain the same serial. Retrying the exact losing fitment after the return commits must still return 409 with an insufficient-custody explanation. Both continue through the complete three-band purchase flow and report assertions.

The job-linked serial adds one line to each of two FAT reconciliations: four lines overall. Both serial reconciliation lines must show **1 issued, 1 returned, 0 fitted and 0 unexplained**. FAT readiness remains READY after the existing custody explanation.

## Verification

| Run | Result |
|---|---|
| item25-return-fitment-baseline.trx | 0 passed / 1 failed, 3m46s; expected reproduction of 40P01 |
| item25-return-fitment-fixed-release.trx | 3 passed / 0 failed/skipped, 7m34s; non-serialized overlap, ordinary three-band flow and report migration checks |
| item25-serial-and-retry-release.trx | 1 passed / 1 failed, 6m58s; non-serialized retry passed, serial race/retry passed but later FAT fixture count was stale |
| item25-serial-final-release.trx | 1 passed / 0 failed/skipped, 4m11s; corrected serial FAT expectations and complete flow passed |
| item25-races-final-debug.trx | 3 passed / 0 failed/skipped, 7m46s; both races with retries and report migration checks |

The first test build had a missing namespace import; its corrected build passed. Production-fix Release build: zero warnings/errors, 4m02.08s. Subsequent test-only builds passed in 17.61s and 18.36s. Debug built with zero warnings/errors in 3m51.25s, including the optional volume branch. The failed serial run is retained and is not counted as a passing overall regression.

Observed request durations include the deliberate overlap gate, so they are not ordinary request latency:

| Case | Return | Competing fitment | Exact retry |
|---|---:|---:|---:|
| Baseline deadlock | 4.1827 s | 2.6242 s (deadlock victim) | — |
| Corrected non-serialized, before retry extension | 3.7392 s | 1.9403 s (40001) | — |
| Corrected serial, final Release | 2.9876 s | 1.6192 s (40001) | 0.7038 s (insufficient custody) |

Evidence is under local-evidence/item25. Baseline, fixed Release and final serial JSON/PostgreSQL logs have separate filenames. Later runs may replace the generic latest-case files.

## Frontend and remaining work

The raced fitment now returns the existing **CONCURRENCY_CONFLICT** envelope. Its exact retry after return commits returns **BUSINESS_RULE_CONFLICT** with insufficient-custody detail. This correction adds no route or DTO shape.

Remaining: same-lot issues, duplicate GRN finalization, PR/MIR/bill approvals, QC/concession, issue/adjustment, role change mid-command, last FIFO quantity, opening ceremonies, payment allocations, and the eleven-user mixed run. The two return/fitment cases above prove the return-winning interleaving; they are not an exhaustive interleaving proof. FIFO return attribution and ownership-pool corrections retain the pending Item 15 decisions.

Source leads remain explicitly unproved: payment allocation locks follow incoming bill order; material issue holds a company/year/prefix number lock until commit, so an API race may block there before reaching FIFO. Tests must report the actual blocking point.

The optional volume benchmark runs only for the ordinary purchase case, avoiding repeated two-million-row exports in concurrency cases. Debug compilation includes that opt-in branch; these race checks skip volume execution. AC and DC sleep are already disabled; no power settings were changed.

Resume: continue Item 25 with duplicate GRN finalization by two settled receipt operators, then the remaining race matrix. This checkpoint has four distinct targeted Release tests passing across the documented runs and three targeted Debug tests passing; it is not a new full-suite result.
