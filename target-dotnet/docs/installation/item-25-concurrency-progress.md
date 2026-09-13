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

## Duplicate GRN finalization (work after 083fefd)

The in-progress test uses separate runtime hosts, distinct command keys and the same draft version. Expected: one finalized history row, one version increment, one stock batch, one RECEIPT_IN movement of 1 and one FIFO layer of 1; the second call and its retry must return 409. It must observe both calls waiting on the existing per-GRN finalization lock before release, then continue the original replay and complete three-band witness.

Two initial runs failed before proving this overlap. The first observer encountered a transient null wait-event sample; it now retries incomplete samples and still requires an actual Lock observation. The second used KARTHICK's current FULL STORES_MANAGER assignment and witnessed HTTP 403 PERMISSION_DENIED before finalization. His older STORES_ASSISTANT assignment is superseded. The SQL function also excludes STORES_MANAGER despite naming SESS-41 as a receipt operator. No permission or role assignment has been changed; whether his manager role should retain receipt authority is pending clarification. Evidence is preserved as duplicate-grn-manager-permission-refusal.json and its PostgreSQL log.

The next run uses SUDALAI/SESS-35 (STORES_EXECUTIVE) and KAMALI/SESS-16 (STORES_ASSISTANT), both current FULL assignments. KAMALI receives the same disposable-fixture login, mapping and warehouse scope as the other fixture users; no role/page authority is added. Release build passed with zero warnings/errors in 17.76 seconds. Execution is pending; this is not yet a passing concurrency witness.
The authorized baseline now witnessed both finalizer backends blocked on the same advisory key. First request succeeded; second returned 500 for PostgreSQL 40001, and its exact retry returned 500 for P0001 (already finalized). Despite those incorrect envelopes, final state was one finalization/version increment, one batch, one movement/quantity 1, and one FIFO layer/quantity 1. No deadlock was observed. The failing Release run took 2m07s and is retained as item25-duplicate-grn-authorized-baseline-release.trx; diagnostic files have the authorized-baseline suffix.

The proposed service correction translates only serialization failures and the two explicit finalized/stale GRN refusals into the existing 409 response path. It does not retry a transaction, change database guards, change permissions or swallow unrelated database failures. Fixed-code verification is pending.
Fixed Release verification passed: 1 test, 0 failures/skips, 4m14s, including the complete three-band purchase/report flow and original-key replay. Both conflict envelope codes are asserted: CONCURRENCY_CONFLICT for the concurrent loser and BUSINESS_RULE_CONFLICT for the exact retry against the finalized receipt. Expected one-row/quantity results all passed. Production-change Release build passed with zero warnings/errors in 3m55.03s; stronger assertion build passed in 17.08s. Debug build passed with zero warnings/errors in 4m03.57s, including the optional volume branch. The three-race Debug batch is running; no new full-suite count is claimed.
Final Debug verification passed: **3 tests, 0 failures/skips, 10m38s** in item25-grn-and-return-races-debug.trx. This includes duplicate GRN finalization and both return/fitment cases, each continuing through its complete purchase/report flow. This change therefore has 1 targeted Release pass and 3 targeted Debug passes; the previous full-suite counts remain historical. Fixed GRN Release and Debug JSON/PostgreSQL logs are separately preserved. No owner database was accessed. Frontend consumers receive existing 409 conflict envelopes instead of 500 for these witnessed GRN conflicts; no route or DTO shape changed.

KARTHICK's receipt authority question remains pending. Next: the already-requested stock-check delivery warehouse and development SQL-log fixes, then continue the Item 25 race matrix. Item 25 remains partial.
## Two opening ceremonies for one company (work after 4a478e1)

A new test prepares separate completed-import fixtures and distinct periods in an empty disposable company. It reuses the existing controlled staging function plus import metadata fixture; it does not claim to upload two Excel files through the importer. Count and valuation use real HTTP operations with current FULL roles: SESS-41 STORES_MANAGER and SESS-14 ACCOUNTS_MANAGER. Two independent API hosts then authorize the separate valued ceremonies as SESS-01 TECHNICAL_DIRECTOR. This is two sessions of the sole authorizing role, not two newly authorized directors. Production page checks and role/command checks execute.

The observer holds OPENING:company and requires both authorize_opening_stock statements to wait on it before release. Expected: one200, one409 and retry409; one POSTED ceremony/version2 with three distinct actors, one VALUED ceremony/version1 without an authorizer, five ceremony events, one batch, one AVAILABLE movement of10 and one OPENING_LANDED FIFO layer of10 at25/value250. The winner's exact replay must return its original result. No opening business SQL is changed. Build and first execution are pending; none of these expectations are a passing witness yet.
The first opening-test build failed because two assertions used signed literals against unsigned stored versions. Corrected test build passed with zero warnings/errors in19.11s. The unchanged-production baseline execution is running; no passing count is claimed yet.

The baseline reproduced HTTP500 for PostgreSQL40001 on the losing authorization, after both function calls were observed blocked on the same company advisory key. First authorization returned200; retry of the loser returned409 because stock movements now exist. Final rows were already protected: one POSTED/version2 with three distinct actors, one VALUED/version1/no authorizer, five events, one batch/movement/quantity10 and one FIFO layer/quantity10/value250. No40P01 deadlock was observed. The failing baseline is retained in item25-opening-company-baseline-release.trx (0passed/1failed,1m32s) and opening-company-baseline.json/-postgresql.log.

The service correction wraps count, valuation and authorization transaction boundaries to translate only PostgreSQL serialization failure40001 into DbUpdateConcurrencyException, which the existing endpoint returns as409. It does not automatically retry, broaden permissions, modify the SQL functions, or suppress unrelated database failures. Fixed-code tests also assert CONCURRENCY_CONFLICT for the concurrent loser and BUSINESS_RULE_CONFLICT for the already-stocked retry. Verification is pending.
Fixed Release verification passed: **2 tests, 0 failures/skips, 4m48s** in item25-opening-fixed-and-three-band-release.trx. The new opening race (including winner replay and both conflict-code assertions) and the complete three-band purchase/report flow both passed. Release build had zero warnings/errors in4m00.56s. Separately preserved evidence: opening-company-fixed-release.json and opening-company-fixed-release-postgresql.log. Baseline request durations were3.8409s/1.8300s/0.3932s; fixed Release durations3.4222s/2.0245s/0.4834s for winner/loser/retry respectively. These include the deliberate overlap gate and are not ordinary latency or eleven-user throughput measurements. Debug verification is pending.
Final Debug verification passed: **2 tests, 0 failures/skips, 4m36s**, in item25-opening-fixed-and-three-band-debug.trx. Debug build passed with zero warnings/errors in4m16.93s. The same opening race and complete three-band flow pass in both configurations. Fixed Debug JSON/PostgreSQL logs are separately preserved. This correction has2 targeted Release passes and2 targeted Debug passes, not a new full-suite count. No owner database access or SQL migration occurred. Next: concurrent MIR approval; the remaining Item25 matrix and eleven-user mixed run remain incomplete.

## Concurrent MIR approval (work after 00e8c35)

The next test uses actual FULL PRODUCTION_MANAGER/SESS-25 and STORES_MANAGER/SESS-41 assignments against a submitted consumable MIR created by SESS-15. Separate hosts/actors and command keys retain real page, role and command checks. A temporary observer transaction takes FOR NO KEY UPDATE on that existing MIR row in the disposable database; it changes no business data. An independent observer connection records both blocked write attempts and their actual backend blockers, then the gate rolls back. This is a test-only row-lock gate, not a claimed production advisory lock.

Expected: first200, second409 and stale-version retry409, one APPROVE history by the first actor, one version increment and the first approver retained. The ordinary issue, custody and complete three-band/report assertions then continue. The shared fixture adds only an optional callback to the existing consumable approval; ordinary calls keep their original path. No MIR production code has changed. Build and baseline execution are pending.
MIR baseline reproduced a real wrapped serialization failure: InvalidOperationException -> DbUpdateException -> PostgresException40001 (could not serialize access due to concurrent update). Both writes were observed: first waited on the gate transaction, second on the first backend's tuple lock. First returned200 in3.4009s, second500 in1.6264s, exact retry409 in0.3048s. Final state retained one APPROVE history by SESS-25/PRODUCTION_MANAGER and one version increment to2, with that first approver retained. No40P01 was observed. The failing baseline (0passed/1failed,3m34s) and full server exception chain are retained in item25-mir-approval-baseline-release.trx; JSON and PostgreSQL logs have separate mir-approval-baseline filenames. Timings include the deliberate gate.

The MIR transition boundary now unwraps only the observed InvalidOperationException/DbUpdateException wrappers and checks for PostgreSQL40001. That specific failure becomes DbUpdateConcurrencyException and the existing409CONCURRENCY_CONFLICT response. Other SQLSTATEs, general transient failures and cancellation are not converted. No automatic retry, permission change or SQL migration is added. Fixed build and execution are pending.
Fixed MIR Release verification passed: **1 test, 0 failures/skips, 4m11s**, including the real two-approver race, both409CONCURRENCY_CONFLICT assertions, one retained approval, and the complete three-band purchase/custody/report flow. Release build passed with zero warnings/errors in3m40.40s. Fixed Release JSON/PostgreSQL logs are separately preserved. Debug build is running with the optional report-volume branch compiled; the MIR race skips volume execution. Debug test result is pending.

Final MIR Debug verification passed: **1 test, 0 failures/skips, 4m05s**, in item25-mir-approval-fixed-debug.trx. Debug build passed with zero warnings/errors in4m02.18s. Both configurations verify the real race and complete three-band purchase/custody/report flow; these are targeted counts, not a new full-suite result. Fixed Debug JSON and PostgreSQL logs are separately preserved. Expected final state is one APPROVE history, version2, first approver retained, with both losing calls returning409CONCURRENCY_CONFLICT. No owner database access occurred. Frontend receives the existing conflict envelope instead of500; no route or DTO changed. Next: PR approval race; Item25 remains partial.
