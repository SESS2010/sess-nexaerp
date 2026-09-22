# Item 25 concurrency — partial checkpoint

## Current status

Item 25 remains partial. All rows below have targeted Release and Debug evidence; these are not a new full-suite total. No owner database was accessed.

Controlled race timings include deliberately imposed lock waits. They are not normal production-latency or eleven-user load measurements.

| Case | Verified result | Change |
| --- | --- | --- |
| Return versus fitment, nonserialized and serialized | One return; competing fitment refused; no deadlock | 083fefd |
| Duplicate GRN finalization | One finalization/stock posting/FIFO layer; loser and retry409 | 03400f7 |
| Two opening ceremonies for one company | One posted ceremony; second refused; one opening stock/value | 00e8c35 |
| MIR approval by Production and Stores managers | One approval/version increment; loser409 | 52e4aa4 |
| Two sessions of the named PR approver, plus early TD attempt | One step1 approval; duplicate409; early TD403; later proper step2 succeeds | b6ac9c1 |
| Two Accounts Manager sessions accepting one bill | One acceptance/cost allocation; loser/retry409; winner replays | d5086c1 |
| Two stores operators issuing the same serial against one MIR | One issue, one unit consumed from oldest FIFO remainder, no negative canonical balances; loser409 | 30769dd |
| Two Accounts Manager sessions settling bills in reversed order | One full settlement/two allocations; zero outstanding; no deadlock; migration permissions retained | c3c90dd |
| TD concession versus QC correction, TD first | Approval only; correction/retry409; AVAILABLE1/PENDING0; migration permissions retained | 45755cb |
| QC correction versus superseded concession, QC first | Correction only; old approval/retry409; rejected history retained beside the fresh approved decision; guarded migration/reversal path | 45249ed |
| Assignment transfer while an issue is in flight | Transfer commits during the wait; existing issue retains original FULL authority; fresh new-role request403; one unit/cost/history | 9c5f56d |
| Two direct FIFO calls for the last remaining unit | One consumption/request/receipt; competitor40001, retry insufficient, no negative remainder or orphan; isolated clone | 01f8426 |

Prerequisite also verified in both configurations: QC correction with linked reversal, immutable history and corrected concession provenance (54d5bdc). This is not itself a concurrency witness.

Remaining: issue versus governed adjustment (no adjustment API/service found yet); DC participation (no DC API/service found). The eleven-user supported-command workload is verified below. The same-serial issue case does not claim every nonserialized/distinct-MIR interleaving. Pending Item15 ownership-pool and return-credit decisions remain unchanged.

## Historical checkpoints

History starts from 8214992 (partial Item15). The following sections retain baseline failures, fixes, exact targeted counts and limitations.

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

## Concurrent PR approval (work after 52e4aa4)

The next witness uses two independent sessions of the actual named first approver, SESS-14 ACCOUNTS_MANAGER, in the MID two-step route. SESS-01 TECHNICAL_DIRECTOR also attempts the second step before the first commits; expected refusal403. No interchangeable approver authority is invented. All three hosts use the normal runtime database principal and actual page/operational permissions. The old full-flow PR approval host uses the disposable administrator connection, so runtime permission gaps are also under test.

A test-only FOR NO KEY UPDATE gate holds the existing PR row without changing data. Expected: both first-step writes overlap, first200, duplicate409, stale retry409, one first-step approval history/version increment, then the original second-step approval and full flow succeed. The shared observer now accepts the expected table name. The combined test also executes the MIR race later in the same full three-band flow. Build and unchanged-production baseline are pending; no PR concurrency success is claimed.
The initial test build passed0warnings/errors4m10.32s. Its1pass4m12s is NOT PR race evidence: the callback selected nonexistent MID while the actual middle-band code is TD. Only the MIR race and ordinary flow ran. Evidence is preserved as item25-pr-callback-not-invoked-release.trx. The selector is corrected toTD with original keyTD-pr-approve-1, and the fact now asserts that the callback executed. Corrected PR baseline remains pending.
The corrected selector reached the normal-runtime PR command, which succeeded200 after gate release. The observer assertion failed first: PostgreSQL was executing the approval-history INSERT whose trigger locks the PR parent FOR UPDATE, not a direct PR UPDATE. No second request was started, so this0pass/1fail2m09s is not a completed race. Separate observer-statement-mismatch evidence is retained. The observer now asserts that actual INSERT/table while still requiring its backend to be blocked by the known parent gate. Production PR code remains unchanged.
The completed PR baseline reproduced InvalidOperationException -> DbUpdateException -> PostgreSQL40001 inside the approval-history trigger parent lock. Both INSERTs were observed blocked; first200(4.6428s), duplicate500(1.5696s), futureTD403(1.7002s) explicitly awaitingSESS-14, stale retry409(0.5604s). Final state retained one step1 history by AccountsManager, CompletedApprovalStepCount1, Version3, PendingApproval; no deadlock. Baseline0pass/1fail2m11s and separate JSON/PG logs preserved. Durations include the deliberate gate. The PR decision boundary now translates only40001 to the existing409CONCURRENCY_CONFLICT path. The identical MIR SQLSTATE filter is shared in PostgreSqlConcurrency; the combined race test verifies both callers. No automatic retry, SQL migration, permission change or DTO change. Fixed verification is pending.
First fixed-code Release build failed on a missing Persistence namespace import in the MIR partial file after sharing the filter. Import corrected; no test was run from that failed build.
Fixed Release verification passed: **1 test, 0 failures/skips, 4m17s**, covering both PR and MIR races and the complete three-band purchase/custody/costing/report flow. Corrected Release build passed0warnings/errors3m47.39s. PR first200/duplicate409/futureTD403/stale retry409; one step1 history and Version3, then the ordinary TD second step completes successfully. Both conflict codes and the early-step refusal reason are asserted. Fixed Release PR and MIR JSON/PG logs are preserved separately. Debug verification is next; no new full-suite count is claimed.
Final Debug verification passed: **1 test, 0 failures/skips, 4m16s**, including both PR/MIR races and the complete three-band flow. Debug build passed0warnings/errors4m12.26s; optional volume code compiled, benchmark skipped for races. Fixed Debug evidence for both races is separately preserved. This correction has1 targeted Release pass and1 targeted Debug pass, not a new full-suite count. Runtime PR approval succeeded without grants or privileged-connection substitution. No owner database access occurred. Next: vendor-bill acceptance race; Item25 remains partial.

## Concurrent vendor-bill acceptance (work after b6ac9c1)

The new witness uses two independent sessions of current FULL AccountsManager/SESS-14, ordinary runtime database credentials, and real page/scope checks. It selects the matched draft for the third receipt (index2, MD band), preserving original keyvendor-bill-accept-2 and reason. A test-only row lock holds the existing bill while both decide_vendor_bill calls are observed blocked; no business row is changed by the gate.

Expected: one200, loser409CONCURRENCY_CONFLICT, retry409BUSINESS_RULE_CONFLICT, winning-key replay200/Replayedtrue; one ACCEPTED history, one version increment, one cost allocation with the original bill quantity/value, first decision key retained, and no40P01. The same test then completes the original flow and also exercises the PR/MIR races. The bill callback must execute. No vendor-bill production code has changed. Build and baseline execution are pending.
Release verification passed: **1 test, 0 failures/skips, 4m27s**, including bill/PR/MIR races and the complete three-band purchase/custody/costing/report flow. Build passed0warnings/errors4m02.24s. Existing vendor-bill code already returned200/409/409 and replay200/Replayedtrue. Final billACCEPTED/version1 (draft0), one ACCEPTED history, one cost allocation quantity1/value118000.01, original decision key retained, no40P01. Separate bill-batch Release JSON/log files preserve all three races. No vendor-bill production change is needed for this witnessed interleaving. Debug verification is next.
Final Debug verification passed: **1 test, 0 failures/skips, 4m23s**; build0warnings/errors3m59.24s. Bill/PR/MIR races and the complete three-band flow pass in both configurations. Separate bill-batch Debug evidence preserves all three races. This test-only addition has1 targeted Release pass and1 targeted Debug pass; no new full-suite count. No production code, SQL migration, authority or owner database change. Next: concurrent issue of the same serialized stock; the broader Item25 matrix and eleven-user run remain incomplete.

## Concurrent issue of the same serial (work after d5086c1)

The new witness replaces only the existing serialized issue command. SUDALAI/SESS-35 STORES_EXECUTIVE and KAMALI/SESS-16 STORES_ASSISTANT use separate hosts, actual FULL assignments, normal runtime credentials and real page/scope checks. Both scan the same third-receipt serial against the same approved MIR, with different command keys. Both current roles already have issue authority.

The gate holds the existing NUMBER:company:year:MI advisory lock. Both API transactions must be observed blocked there; stock selection occurs after this production lock. This is concurrent API execution serialized at numbering, not a claim of simultaneous FIFO-row writes. Expected: one issue and ISSUE history, MIR FULFILLED/version+1, one posting batch/two serial-preserving movement legs of1, warehouse serial balance0, no negative dimensional stock bucket or FIFO remainder, total FIFO consumption1, loser/retry409, and winning replaytrue. The oldest available FIFO remainder is captured before/after; the fixture may already have consumed part of that layer, so this does not claim an exact untouched one-unit FIFO layer. The original return/full-flow assertions follow. No issue-service production change has been made; build and baseline are pending.
Serial baseline build passed0warnings/errors4m07.90s; execution0pass/1fail3m40s. Both requests were observed waiting on the existing numbering advisory lock. Actual responses were201(3.1685s),500(1.8383s),retry409(0.4123s),replay201(0.5054s). The test initially expected200 for creation/replay; those expectations are corrected to the existing201 contract. Independently, recorded responses and PostgreSQL logs prove the loser500 caused by raw40001 read/write dependency failure in register_command_request. No40P01. Final rows retained one issue/history/version increment and one FIFO consumption of1 from the oldest remaining layer, which actually held exactly1 before the race and0 afterward. Baseline JSON/logs are preserved separately.
The first diagnostic grouping split QC source references into separate buckets, producing offsetting+1/-1 groups for QC source events. Foundation3InventoryProvenanceGenealogySql defines stock balance by company/item/location/ownership/custody/provenance/lot/serial, excluding QC/source-reference IDs. The test now asserts those actual dimensions and retains the reference grouping as diagnostic evidence. The frozen schema requires nonnegative AVAILABLE and0-or1 serialized balances; no stock SQL is changed to accommodate this test. The issue transaction boundary now translates only40001 with the shared narrow filter into409CONCURRENCY_CONFLICT; no automatic retry or authority change. Fixed verification is pending.
Fixed Release verification passed: **1 test, 0 failures/skips, 4m10s**, including the same-serial race and complete three-band purchase/custody/costing/report flow. Build passed0warnings/errors3m44.76s. Responses201/409CONCURRENCY_CONFLICT/retry409BUSINESS_RULE_CONFLICT/replay201true; one issue/history/version increment, one batch/two legs, warehouse serial quantity0, four canonical balance groups minimum0, and one FIFO consumption quantity1/value5900. The oldest remaining layer held exactly1 before and0 after. Separate fixed Release JSON/PG logs are preserved. Both API attempts overlapped at numbering; the loser failed at command registration, so this does not claim two simultaneously executing FIFO functions. Debug verification is pending.

Final serial Debug verification passed: **1 test, 0 failures/skips, 4m10s**; build0warnings/errors4m05.67s. The same serial race and complete three-band flow pass in Release and Debug. Separate fixed Debug JSON/log evidence is preserved. This correction has1 targeted Release pass and1 targeted Debug pass. Frontend gets409CONCURRENCY_CONFLICT instead of500 for the observed issue serialization failure; successful creation/replay remain201. No SQL migration, authority change or owner database access. Next: concurrent full bill settlement and payment lock ordering.

## Concurrent full settlements in reversed bill order (work after 30769dd)

The new test uses two sessions of the actual FULL AccountsManager/SESS-14, normal runtime credentials and real permissions. It reads actual outstanding payables and attempts full settlement of two accepted bills, with different payment keys/references and opposite bill order. Two test-only bill-row gates expose the order in which record_vendor_payment locks the bills: first waits onA, second may wait onB orA depending on implementation; releaseA, observe first waiting forB, then releaseB.

Expected: no40P01, one201/full settlement, loser/retry409, winning replay201true, one payment/two allocations for exactly the remaining liability, zero database outstanding balances and no remaining API payables. The ordinary fixture keeps its small partial payment; only this variant settles in full and expects the zero position to disappear. The original replay uses the actual winning command. No payment production code or SQL has changed. Release build passed0warnings/errors3m47.15s; baseline execution is pending.
The payment baseline reproduced a real40P01 deadlock between the two record_vendor_payment calls: first heldA and waitedB; second heldB and waitedA. The API concealed the victim as409. First409, second201; retry of the winning second key201, retry of the losing first key409. One payment123650.01 survived with allocations118000.01 and5650 (plus prior advance adjustment250), both outstanding balances0. Thus no overpayment occurred, but lock ordering failed. The failing baseline0pass/1fail3m17s and separate JSON/PG logs are preserved.
New migration20260913030000_VendorPaymentBillLockOrder sorts the existing validation/locking loop by bill UUID. Original allocation JSON, fingerprint, financial guards, ownership and grants are retained; the earlier migration is untouched. Up/Down require PostgreSQL guards, exact prior function body and the expected definer/search-path settings. The fixed witness also performs Down/reapply after runtime-principal provisioning and compares functionOID/owner/ACL/configuration, verifies runtimeEXECUTE and denied direct payment-tableSELECT, then runs the real race/full flow. No payment service code changed. Fixed build and execution are pending.

Fixed payment Release verification passed: **1 test, 0 failures/skips, 4m07s**, including migration Down/reapply after principal provisioning, unchanged function OID/owner/ACL/configuration, runtime EXECUTE with direct table SELECT denied, the real reversed-order settlement race, and the complete three-band flow. Build passed with zero warnings/errors in4m01.95s. First201, second409, loser retry409 for excess outstanding, winner replay201true; one payment123650.01, two allocations118000.01 and5650, both database outstanding balances0, no40P01. Fixed Release JSON/PG logs and function metadata are separately preserved. Debug verification is running; no new full-suite count is claimed.

Final payment Debug verification passed: **1 test, 0 failures/skips, 4m15s**; build0warnings/errors4m04.06s. Both configurations verify migration Down/reapply with unchanged function metadata and restricted runtime access, reversed-order full settlement, and the complete three-band flow. Separate fixed Debug JSON/PG logs and permission metadata are preserved. Expected final state: one payment123650.01, two allocations118000.01 and5650, both bill balances0; responses201/409/retry409/replay201true; no40P01. This correction has1 targeted Release pass and1 targeted Debug pass, not a new full-suite count. No owner database access, authority change, route or DTO change. Next: prove the QC correction prerequisite, then the QC/concession race; Item25 remains partial.

## QC correction prerequisite (work after c3c90dd)

Before a QC/concession race can be a valid witness, the existing correction operation must execute. The new runtime test uses actual FULL QC_MANAGER/SESS-33 with normal runtime credentials and real page/scope checks. On the existing rejected serial, it corrects the recorded measurement12 to11 while retaining the required rejection, before concession creation. Expected: original finalized revision retained, one correction/revision2 and matching reversal, unchanged serial quantity1, exact command replay, then ordinary concession acceptance and the complete three-band flow. Only this variant expects four QC revisions/dispositions/postings rather than three; concession evidence uses the measured value of the actual failed result. Ordinary flow remains three revisions. No QC production change has been made. Source review suggests reversal payload fields may be missing; build and baseline are pending, so this is not yet an observed failure or concurrency proof.

QC correction baseline build passed0warnings/errors4m04.74s; runtime execution failed0passed/1failed in2m58s. Actual HTTP500/P0001: Every Foundation3 leg requires ordinal,item,location,ownership,custody and provenance. The correction rolled back: original revision1FINALIZED retained, zero reversal batches, serial quantity1. Separate baseline JSON/PG log and TRX are preserved. This is a prerequisite defect, not a QC/concession race result.

ReverseBatch now supplies the original item, condition location and source references alongside linked reversal ID and inverse quantities; the existing posting function restores ownership/custody/provenance from that original. Both QC and concession reversals call this helper, but this test specifically exercises QC correction. No SQL migration or permission change. The strengthened test compares the entire original movement records before/after, exact linked inverse legs and dimensional identities, measured values12 then11, nonnegative canonical serial buckets and QC_HOLD0/AVAILABLE0/PENDING_RETURNABLE_DC1 before concession. Fixed build and full-flow execution are pending.

The payload-only fixed build passed0warnings/errors3m38.25s, but execution0passed/1failed3m04s exposed a second real defect:23505 on IX_stock_movements_CompanyId_PostingIdentity. Reposting reused TRANSFER_OUT:ordinal:original-provenance from the initial QC movement. Separate payload-fixed-failure JSON/PG log/TRX are retained; the transaction rolled back. New QC/concession leg identities now include their disposition/allocation ID, keeping distinct posting history while command replay remains guarded by its existing receipt. Earlier rows are untouched. The full flow now explicitly asserts concession revision, measured value and annotation against the actual corrected failed result. Rebuild and rerun pending.

Combined QC fixed Release verification passed: **1 test, 0 failures/skips, 4m11s**; build0warnings/errors3m42.05s. Correction returned200 in2.3769s, retained original revision/movements, recorded correction revision2 and exactly one linked reversal with inverse quantities and matching dimensions; measured values12 and11 retained. Canonical balances remain nonnegative, QC_HOLD0/AVAILABLE0/PENDING_RETURNABLE_DC1 and serial total1 before concession. Exact replay and downstream concession revision/measurement/annotation assertions pass, followed by the complete three-band purchase/custody/costing/report flow. Separate fixed Release JSON/PG logs preserved; both earlier failing runs remain separately retained. Debug build is running. This is a corrected runtime prerequisite, not the concurrent QC/concession witness.

Final QC prerequisite Debug verification passed: **1 test, 0 failures/skips, 4m12s**; build0warnings/errors4m10.84s. The same correction, unchanged original records, exact reversal, dimensional balances, replay and corrected concession evidence pass with the complete three-band flow. Separate fixed Debug JSON/PG logs preserved. This correction has1 targeted Release pass and1 targeted Debug pass, not a new full-suite count. It fixes two actual500 causes without a SQL migration, authority change or frontend route/DTO change. The shared helper also serves concession reversal; this witness specifically proves QC correction. Next: the actual concurrent QC/concession race; Item25 remains partial.

## Concurrent QC correction versus concession (work after 54d5bdc)

The prerequisite is now committed and independently verified. The new race uses real FULL SESS-01 TECHNICAL_DIRECTOR and SESS-33 QC_MANAGER through separate normal-runtime hosts with actual page/scope checks. The existing rejected MD serial and draft concession are reused. TD approval competes with a QC remeasurement that would accept that same serial. Both calls must be observed inside post_stores_stock_batch, blocked on the existing shared ownership posting lock; the test then releases the gate. No additional production change has been made.

Expected: TD200, correction409, correction retry409, TD replay200true; exactly the original QC revision, no QC reversal, one approved concession/version increment/batch/two legs; serial total1, AVAILABLE1/PENDING_RETURNABLE_DC0/QC_HOLD0, every canonical balance0..1, no40P01. The original full three-band flow then continues. The callback must execute. Build and actual baseline are pending; the draft/plan files are not evidence of success.

QC/concession baseline build passed0warnings/errors4m05.84s; test0passed/1failed3m06s. Both commands were observed blocked on the shared ownership posting lock. TD returned200(3.5672s), concurrent QC500(1.8453s) from40001, fresh QC retry200(1.3350s), TD replay200true(0.5375s). The retry committed a second QC revision and one reversal while the concession stayed approved: AVAILABLE2 across two provenance/custody layers, PENDING_RETURNABLE_DC-1, QC_HOLD0, grand total still1. This is an actual violation of the frozen nonnegative-stock requirement, not merely response handling. No40P01. Baseline JSON/PG log/TRX preserved separately.

New migration20260913040000_StockConditionBalanceGuard removes the AVAILABLE-only restriction from the existing canonical balance check, so the controlled database function refuses negative ownership/custody/provenance stock in every condition. It retains lock order, posting contracts and original data. Up/Down require the exact immutable earlier function body plus PostgreSQL/provider/protected-database/definer/search-path guards; no earlier migration changed. The witness performs Down/reapply after principal provisioning and compares OID/owner/ACL/configuration, runtimeEXECUTE and denied direct stock INSERT. QC finalization/correction, concession decisions and concession reversal transaction boundaries translate only observed40001 or the specific new P0001 balance refusal into409; unrelated failures remain visible. Expected fixed result: TD200/QC409/retry409/replay200true, original revision only, no reversal, one approved concession/batch/two legs, AVAILABLE1/PENDING0/QC_HOLD0. Both this race and the valid-correction prerequisite will run through the complete three-band flow. Fixed build is running; no passing result yet.

Fixed stock-guard Release verification passed: **2 tests, 0 failures/skips, 7m42s**; build0warnings/errors4m21.73s. Both the concurrent TD/QC case and valid correction prerequisite complete the full three-band flow. Race responses200/409/409/200true; original QC revision only, no reversal, one concession batch/two legs, AVAILABLE1/PENDING_RETURNABLE_DC0/QC_HOLD0. Concurrent and retry conflict messages are asserted. Migration Down/reapply preserves functionOID/owner/ACL/configuration/runtimeEXECUTE and denied direct stockINSERT. Separate fixed Release race evidence, correction-under-new-guard evidence and permission metadata are preserved. This proves the forced TD-first interleaving; it does not claim the opposite ordering was executed. Debug verification is next; no new full-suite count.

Final stock-guard Debug verification passed: **2 tests, 0 failures/skips, 7m22s**; build0warnings/errors4m23.00s. Both complete three-band flows, the exact race/retry refusals, nonnegative stock, valid correction and migration Down/reapply/permission checks pass in both configurations. Separate Debug race, valid-correction and function-permission evidence preserved. This correction has2 targeted Release passes and2 targeted Debug passes, not a new full-suite count. No owner database access. API routes/DTOs are unchanged; QC returns the existing409 envelope for observed serialization and source-stock refusal. The database balance restriction now covers all conditions; existing ledger rows are not rewritten. Next: independently prove the QC-first ordering, then the remaining Item25 cases.

## QC-first ordering (work after 45755cb)

A separate runtime witness now exercises the opposite ordering without changing production code or existing fixture behavior. It uses the existing qcCorrection hook: QC_MANAGER/SESS-33 creates the original rejected-stock concession, then correction wins the shared ownership lock before TECHNICAL_DIRECTOR/SESS-01 attempts that old concession. Correction changes measurement12 to11 while retaining rejection. Expected: correction200, old approval409, old approval retry409, correction replay200true; two QC revisions/one reversal, old concession stillDRAFT/version0/no posting, serial total1/PENDING1/AVAILABLE0/HOLD0 and no negative bucket or40P01. TD then rejects the superseded draft through the normal endpoint, and the existing full flow creates a fresh concession against corrected QC and completes all three bands. No success is claimed until execution. Release build is running.

The QC-first baseline build passed0warnings/errors4m13.29s. Execution failed0passed/1failed3m09s **after** the race and rejection passed: correction200, old approval409, retry409, correction replay200true; revision2, one reversal, old draft unposted, canonical serial stock1 in PENDING and no negative balance/40P01. TD rejected the old draft through the normal endpoint. Creating its replacement then failed23505 on the global CompanyId/InventorySerialId unique index. Initial JSON, rejected draft, PostgreSQL log and failing TRX are preserved separately (the final23505 is in TRX output). Frozen schema paragraphs239/945 require immutable decision history and reversal/re-decision, so the global lifetime uniqueness is an implementation defect.

The pending fix keeps the unique key within each concession allocation, changes the global serial index to a lookup, and adds a guard against simultaneous active allocation of the same serial and receipt allocation. Rejected or reversed decisions remain immutable history. CreateConcession now uses the existing narrow serialization handler and translates only the new named active-serial constraint into409. Migration20260913050000 has provider/cluster/protected-database/index/function/trigger guards; Down refuses once historical duplicate serial rows make the old unique index impossible without deleting records. New verification checks the compiled model snapshot, Down/reapply after principal provisioning, a direct runtime duplicate-allocation refusal with complete fixture rollback, the full QC-first three-band flow, and Down refusal after history exists. Build and actual fixed execution are pending; no success claimed yet.

Fixed QC-first Release build passed0warnings/errors4m00.94s; test **1passed/0failed/0skipped4m25s**. Compiled model has no pending changes. Post-provisioning migration Down/reapply and owner/ACL/index/trigger checks pass. Deliberate different-allocation/same-serial insertion through normal runtime fails23505/UX_concession_active_serial_allocation; its entire fixture transaction rolls back with0rows remaining. Race responses200(3.3951402s)/409(1.8433143s)/retry409(.8395945s)/replay200true(1.0168463s). Two immutable serial decision records survive: REJECTED against original QC and APPROVED against corrected QC. Down with that history refusesP0001 and preserves2rows/metadata. The full three-band flow passes. Separate fixed Release evidence is retained. TD-first regression and Debug verification are still pending; no new full-suite count.

Final concession-history verification: TD-first Release regression **1passed/0failed/0skipped4m13s**, in addition to QC-first1pass4m25s on the same successful Release build. Debug build passed0warnings/errors3m50.53s; combined Debug run **2passed/0failed/0skipped7m32s**. Both orderings complete the full three-band workflow in both configurations. QC-first Debug times2.993042s/1.841537s/.6905985s/.907799s for200/409/retry409/replay200true. TD-first Debug times3.3998202s/1.8159698s/.9903953s/.5340989s for200/409/retry409/replay200true. Snapshot, migration Down/reapply/permissions, active-allocation refusal, immutable rejected/replacement decision history and history-preserving Down refusal all pass. Separate Debug evidence is retained; initial failing evidence remains. This change has **2 targeted Release passes and2 targeted Debug passes**, not a new full-suite count. No owner database access. No frontend route, field or envelope changes; concession creation now returns the existing409 envelope for serialization failure or the specifically named active-serial conflict. Next: role assignment change mid-command, then direct FIFO contention and the supported mixed-user operations. Item25 remains partial.

The mixed-run source review also found no governed DC API/service. Pending Work item17 is superseded by Stores Completion item40, which this session excludes; this is the same pending delivery scope conflict already recorded for Item15. A concurrent DC posting cannot be claimed while that path is absent. No governed stock-adjustment API/service was found either. These prerequisite gaps do not block the role-change or direct FIFO witnesses.

## Assignment change during an issue (work after 45249ed)

A new witness pauses KAMALI/SESS-16's normal-runtime FULL STORES_ASSISTANT issue on the existing MI number lock. TECHNICAL_DIRECTOR/SESS-01 then uses the governed transfer endpoint to replace that assignment with SUPPORT on the same role, effective today and without retaining the old assignment. The test records whether transfer completes while the issue remains blocked, then releases the issue and makes a fresh request using a newly resolved current assignment. No production authorization change has been made. It captures response seconds, role event/old and new assignment versions, issue history and its recorded assignment, FIFO consumption and nonnegative canonical serial balances. Baseline expected in-flight201/fresh403 must be checked against actual execution; no cancellation/completion behavior is claimed yet. Release build succeeded0warnings/errors4m23.51s; execution is running.

The first role-change run failed **0passed/1failed3m47s** on a wrong test expectation, not a proven production defect. Transfer to SUPPORT returned200 in1.7840144s while the issue was blocked; the already-running issue returned201 in3.668057s and retained its original FULL assignment. A fresh SUPPORT request returned409 in1.5577028s because the MIR was fulfilled. Source inspection confirms SUPPORT excludes approval/rejection/cancellation/reversal/deactivation and role administration, but does **not** exclude issue. No permission policy is being changed. Baseline JSON/PG log/TRX are preserved. The corrected fixture transfers SESS-16 from STORES_ASSISTANT to SERVICE_ENGINEER, both FULL, so the fresh actor genuinely lacks issue authority. The new fact is AssignmentChangeDuringIssuePreservesAuditAndRefusesFormerRoleIssue. Corrected build/execution pending.

The SERVICE_ENGINEER variant rebuilt0warnings/errors22.82s and ran **0passed/1failed3m57s**. Its complete race assertions passed: transfer200(2.5514066s) while issue blocked; in-flight issue201(4.2960218s), fresh SERVICE_ENGINEER403(1.3785217s); original FULL assignment retained in issue/history, one governed transfer event, one FIFO consumption1@5900 and nonnegative canonical serial total1, no40P01. It subsequently failed an existing full-flow assertion that every MaterialIssue.Issue audit role must be STORES_EXECUTIVE. KAMALI correctly recorded STORES_ASSISTANT. Separate engineer-fixture-failure JSON/PG log/TRX are preserved. The fixture now compares every issue audit with its actual persisted issue, subject, assignment ID/type, assignment employee and role code. This strengthens audit attribution for multiple operators without changing production behavior. Rebuild/full-flow rerun pending.

Role-transfer verified Release result: build0warnings/errors21.47s, **1passed/0failed/0skipped4m15s**. The full three-band flow and strengthened multi-actor audit assertions pass. Issue201(3.3684779s), transfer200(1.6824587s) while the issue remains blocked, fresh SERVICE_ENGINEER403(1.5013623s). One issue, original FULL assignment/history, old assignment version1, new assignment version0, one TD transfer event, FIFO1@5900, no negative serial bucket or40P01. No production behavior changed: the already-started serializable command completes with its recorded authority; the next request resolves the replacement role and is refused. These elapsed call times include the deliberate advisory-lock gate and are not a normal production-latency benchmark. Debug build succeeded0warnings/errors4m03.32s; execution pending.

Final role-transfer Debug verification passed: **1passed/0failed/0skipped4m14s** after build0warnings/errors4m03.32s. Issue201(3.2399524s), transfer200(1.633421s) while the issue is blocked, fresh SERVICE_ENGINEER403(1.4803744s). All race, stock/value, original-assignment history, transfer-event and complete three-band/audit/report assertions pass in both configurations. Separate Debug evidence preserved; both earlier fixture failures remain independently retained. This test-only change has **1 targeted Release pass and1 targeted Debug pass**, not a new full-suite total. It proves completion of an already-started command with its original recorded authority and refusal on a fresh request after a role transfer; it does not cancel in-flight commands. No production behavior, frontend route/field/envelope or owner database changed. Next: direct FIFO-function contention, then supported mixed-user operations.

## Direct FIFO-function contention (work after 9c5f56d)

The new baseline uses an isolated clone of the real three-band witness database, taken before the last .05-unit engineer-report issue so the oldest remaining FIFO layer has exactly1 unit. The original witness then continues untouched. The clone is created only after validating the disposable source (127.0.0.1, random non5432 port, advance_parser, postgres test principal, pooling disabled, no connected source sessions). It retains the schema/data/role grants; runtime has CONNECT and still cannot SELECT financial tables. Two new unposted/uncosted material-issue header/line fixtures are explicitly costing inputs, not completed API issues; existing immutable records are never edited or deleted.

Two actual FULL Stores operators register assignment-bound commands and call consume_fifo_for_issue in separate Serializable runtime transactions. Both must be observed waiting on the existing FIFO company/item advisory lock. Expected: first commits one consumption and receipt, second40001 with rollback, fresh loser retryP0001 insufficient quantity, and old committed-command reentry42501 (committed receipts do not reopen mutation authority). Verify one added consumption1 at the original layer cost, zero remaining FIFO quantity, no negative remainder/deadlock, one request/receipt with no orphan, unchanged physical movements and original database remaining1. No production function or numbering lock was changed. Release build is running; this is not yet a passing result.

Direct FIFO Release verification passed: build0warnings/errors4m36.63s; **1passed/0failed/0skipped4m20s** including the unchanged parent three-band flow. Both direct function calls were observed waiting on the FIFO lock. Winner committed1 consumption in.2822206s; loser rolled back40001 in.1463559s; fresh loser retry refusedP0001 insufficient quantity in.1343643s; old committed-command reentry refused42501 in.090508s. Exactlyone unit was consumed from the original oldest remaining layer; all remainders nonnegative and total0. Exactlyone registered request/receipt, zero orphan requests; cloned physical movements unchanged and parentremaining1. Runtime financial SELECT remains denied. Clone/fixture scope and controlled gate are explicit in evidence. No production defect or fix was found for this interleaving. Separate Release JSON/PG log preserved. Debug build passed0warnings/errors4m17.72s; execution running.

Final direct FIFO Debug verification passed: **1passed/0failed/0skipped4m14s** after build0warnings/errors4m17.72s. Winner1 committed in.315058s; competitor40001/.1709509s; fresh retryP0001/.2436064s; committed-command reentry42501/.0881856s. One unit at118000.010000 was consumed, exactlyone request/receipt and no orphan, zero/nonnegative remaining cost quantities, no40P01, physical ledger unchanged and original database stillremaining1. Both configurations complete the parent full three-band flow after the isolated clone witness. Separate Debug evidence retained. **1 targeted Release pass and1 targeted Debug pass**, not a new full-suite count. Test-only change; no production function, permissions or frontend contract changed. This proves capacity/rollback at the direct FIFO boundary on the ordinary witness data, not resolution of the separately pending ownership-pool or return-credit valuation gaps. Next: eleven-user supported mixed operations; missing DC/adjustment prerequisites remain explicitly recorded.

## Eleven users on one API host

**Verified in Release and Debug.** The opening-stock prerequisite is committed
separately as **46d8be4**. This workload and its employee/payment corrections
follow that commit. Item 25 remains partial because adjustment and DC APIs are
absent; this does not claim those commands or every distinct-MIR interleaving.

The test starts eleven requests together against one API host. Each request has
its own test-authenticated actor and service scope, with actual stored role
assignments and the normal page, company, operational-scope and command checks.
Peak server overlap was **11** in both configurations. Real OIDC is the separate
Item 16 witness. There are no SQL gates in this initial workload.

The disposable fixture explicitly adds an Accounts Manager secondary IT
department assignment and creates its narrow operational scope through the
governed API to support the seeded IT approval route. This is test configuration,
not a claim about SESS's deployed assignments. Pending purchasing documents are
created through APIs. Opening begins with completed import staging fixtures,
not an Excel-upload witness. The guarded clone comes only from the owned,
random-port disposable PostgreSQL fixture. No owner database was accessed.

### Timings and retry behavior

The table gives each initial HTTP status and elapsed seconds. Fresh retries run
after the whole batch, using the original payload and key. Only typed
CONCURRENCY_CONFLICT responses qualify. Before retry, the test requires zero
failed command registrations and unchanged business state. All final requests
succeed; no automatic production retry or weaker isolation was introduced.

| Employee / command | Release initial | Debug initial |
| --- | ---: | ---: |
| SESS-16 / Issue | 409, 2.0994571s | 409, 2.486179s |
| SESS-25 / MIR approve | 200, 2.1231474s | 409, 2.1062861s |
| SESS-41 / MIR approve | 409, 1.81086s | 200, 2.0082457s |
| SESS-14 / Payment | 409, 1.8899837s | 201, 2.1031543s |
| SESS-35 / GRN finalize | 409, 2.529063s | 409, 2.4869463s |
| SESS-33 / QC finalize | 200, 2.9730518s | 200, 3.0860702s |
| SESS-15 / PO issue | 200, 2.213043s | 200, 2.6442455s |
| SESS-05 / Technical verify | 200, 2.3939525s | 200, 2.2485307s |
| SESS-02 / PR final approve | 200, 2.2546207s | 200, 2.0928277s |
| SESS-12 / Employee contact update | 200, 2.5849865s | 200, 2.7593523s |
| SESS-01 / Opening authorize | 409, 2.5391308s | 409, 2.3978585s |
Release: six initial successes, five refusals. Retry seconds: Issue 0.9438193,
MIR (SESS-41) 0.7327699, payment 0.9111390, GRN 1.0555143, opening 1.0832000.
Debug: seven initial successes, four refusals. Retry seconds: Issue 0.7219146,
MIR (SESS-25) 0.8574302, GRN 1.9502045, opening 1.3642790.
PostgreSQL logs contain no 40P01 or deadlock diagnostic in either run.

### Expected rows and reproduced defects

Both runs verify one issue, one FIFO consumption of 1 at 4720, two physical
movement legs, the actual issue/approval actors and assignments, one finalization
per prepared GRN, one QC revision, and one opening at Version 2 with three events
and quantity 10. Every canonical stock balance and FIFO remainder is nonnegative.
The initial payment has one INR allocation of 1. The parent completes the full
three-band purchase, custody, costing and report flow after the clone checks.

The first mixed fixture exposed an Installer defect: reprovisioning revoked the
four opening commands and restored direct writes on opening evidence tables.
46d8be4 restores the controlled boundary and verifies two consecutive reruns.

An earlier eleven-user run returned payment 500 from PostgreSQL 40001 during
command-receipt staging, outside the inner financial-function error conversion
(0 passed, 1 failed, 5m38s). The payment transaction boundary now converts this
specific serialization error to the existing concurrency 409. In addition to
actual concurrent refusals, a disposable-only receipt trigger reproduces that
failure after payment and audit writes. Both configurations verify no new
payment, allocation, registration or audit survives; retry and exact replay
create only one additional payment/allocation/audit and return the same payment
ID. Total paid remains 1 after failure, then 2 after retry and replay. This
injected check is separate from the eleven-user timings; no trigger is shipped.

The employee baseline accepted both edits with Version 2, kept Version 2 and
added two Update history rows. Its initial lock observer reused a statistics
snapshot and failed to observe both waiters (0 passed, 1 failed, 6m13s). The
corrected observer clears that snapshot on every poll and requires both blocked
writers. The employee update, approval-status and login-status API writers now
advance the existing concurrency token. Release and Debug each permit one edit
and refuse one with 409: Version 3 becomes 4 and only one history row is added.
The winners differ between configurations. Controlled edit times include lock
waits: Release 1.0789963/0.7675364s; Debug 0.7407469/0.9323225s.

Fresh approval, disabling login and re-enabling login advance Version to 5, 6
and 7. Stale approval and login requests return 409 without changing state or
adding history. Routes and DTOs are unchanged; clients must use the newly
returned Version for their next change. The employee import adapter is a
separate writer covered by the follow-up below; these API checks
do not claim that all employee writers are covered.

### Verification

Final builds passed with zero warnings/errors: Release 27.84s (after the
employee API rebuild, 45.69s); Debug 4m24.24s. Each combined batch passed **2 tests,
0 failures, 0 skips**: Release 6m28s; Debug 6m29s. The eleven-user/full-three-band
test took 5m13.620s Release and 5m13.984s Debug. The opening principal/ceremony/report
test took 1m14.376s Release and 1m15.714s Debug. These are targeted counts, not a
new full-suite total.

Baseline and verified JSON, PostgreSQL logs and TRX files are retained under
local-evidence/item25. Verified artifacts use -verified-release and
-verified-debug suffixes; employee contact, status and payment receipt failure
results are separate from the initial workload measurements.
## Employee import version follow-up

A focused real-PostgreSQL regression now covers the employee import adapter
using the restricted runtime role. It simulates an import row prepared before
another import commits; it is not an HTTP workbook-upload witness.

Baseline: the first update returned Version 2, and a stale prepared row with
Version 2 overwrote the name without an exception. The stored version stayed 2.
The baseline failed 1 test in 1m12s after a successful build (zero warnings/errors,
4m19.80s). Its JSON and TRX remain separately retained.

The adapter now advances Version and explicitly raises the import framework's
handled MasterDataConflictException for a stale prepared row. Missing employees
raise its handled not-found exception. It preserves login and existing company
assignment behavior. The Release regression passes: Version 2 becomes 3, the
stale row is refused, the first name remains and login is unchanged. One targeted
Release test passed with zero failures in 1m12.729s; build passed with zero
warnings/errors in 3m53.64s. Debug also passed one targeted test with zero failures/skips in 1m11s; its build passed with zero warnings/errors in 4m28.38s. No schema or migration changed.

Debug exact test duration: 00:01:11.3998780. Both configurations preserve the first import's name, return Version 3 and refuse the stale prepared row with MasterDataConflictException. The workbook contract is unchanged; successful import results now carry the advanced version. These are targeted adapter tests, not a new full-suite count.
