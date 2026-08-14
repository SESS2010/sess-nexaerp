# REV869B pre-apply source safety re-review after Correction 21

Date: 2026-08-14  
Review mode: fresh independent, source-only, offline  
Reviewed commit: `b24ba9a7d813f3e2c32ac8fe69275423cbc12cc1`  
Reviewed parent: `7a1e4739b733acb4a90594fa4112cad52aa0f71c`  
Reviewed subject: `Correct REV869B control-plane safety checkpoint 21`  
Exact reviewed range: `7a1e4739b733acb4a90594fa4112cad52aa0f71c..b24ba9a7d813f3e2c32ac8fe69275423cbc12cc1`  
Checkpoint SHA-256: `64DE11A6552AA0E814150A77A235577D0113A6F5221E014A3E4F3F51F18CE50E` (matched)

## 1. Decision

Correction 21 does not pass the source-only safety gate. The command-terminalization column defect is corrected, but four blockers remain:

1. Quarantine replay accepts a mismatched supplied lease version because the replay branch returns the stored result before validating `expected_version` against `LeaseVersion`.
2. Purge retry linkage can still be bypassed by presenting a failed purge as a new root and changing any policy field used by the initial-authorization `EXISTS` predicate.
3. Target function ACL verification enumerates only `rev869b_%` functions even though the authoritative target schema contains three REV869A functions. Direct EXECUTE granted to an arbitrary ordinary principal on a REV869A function is therefore outside the delta.
4. The 34 discovered tests are contracts for a not-present external acceptance endpoint, not 34 source-present PostgreSQL behaviors. Thirty-two tests accept a signed controller assertion; T01 and T03 accept unsigned allocation/release JSON. A signature authenticates the configured signer, but it does not independently prove that the signer ran the described setup/action or derived the returned IDs, hashes, SQLSTATE and object identity from PostgreSQL.

The frozen architecture remains valid and unchanged. These failures do not require merging or redesigning external provisioning, the lifecycle controller, the surviving control-plane database, or target-local transactional ledgers.

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`

PostgreSQL remains unauthorized. This review made no PostgreSQL connection and executed zero PostgreSQL tests.

## 2. Entry gate and exact 11-file diff reconciliation

The entry gate passed: HEAD, parent and subject match; target-scoped status was clean; the checkpoint hash matched; and EF discovery found 13 migrations with REV869A exactly once at ordinal 12 and REV869B exactly once at ordinal 13, immediately adjacent. `../legacy-reference/` was not read, enumerated, modified or staged.

The exact committed range contains 11 files, 549 insertions and 92 deletions:

| # | File | Status | Added | Deleted | Independent reconciliation |
|---:|---|:---:|---:|---:|---|
| 1 | `outputs/rev869b_source_correction_checkpoint_21.md` | A | 139 | 0 | Implementation claim only; hash matched, but claims were independently retested. |
| 2 | `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | M | 70 | 13 | Corrects terminal column ownership; adds purge-chain fields and wider ACL checks, but retains retry and function-inventory gaps. |
| 3 | `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs` | M | 1 | 1 | Updates the quarantine function signature string. |
| 4 | `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | M | 40 | 11 | Adds typed labels and expected values for 34 contracts; it does not implement their database behaviors. |
| 5 | `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs` | M | 7 | 0 | Adds source-string coverage for quarantine fields. |
| 6 | `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | M | 36 | 2 | Contains 32 one-line delegates plus T01/T03 HTTP allocation bodies; no PostgreSQL behavior is present locally. |
| 7 | `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | M | 85 | 4 | Adds offline string/shape scans; these pass while the semantic bypasses below remain. |
| 8 | `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | M | 102 | 32 | Adds origin/TLS/signing pins and signed evidence shape checks; hashes and facts remain signer-supplied rather than independently derived. |
| 9 | `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs` | M | 10 | 5 | Exposes the expanded purge authorization arguments. |
| 10 | `tools/rev869b-control-plane-install.sql` | M | 50 | 20 | Adds durable quarantine outcomes and recovery attempt freshness; replay version binding remains incomplete. |
| 11 | `tools/rev869b-control-plane-verify.sql` | M | 9 | 4 | Adds quarantine relation/function inventory and broader ACL checks for the dedicated control-plane database. |

No migration identity, designer, snapshot, REV869A, Purchase domain/service/endpoint, production, frontend, AWS or OIDC file changed.

## 3. Five-blocker result matrix

| Blocker | Result | Independent physical-source evidence |
|---|:---:|---|
| 1. Command-terminalization columns | **PASS** | `rev869b_command_attempts` defines `TargetBackendPid integer` and `TargetTransactionId bigint`; `rev869b_command_contexts` defines `BackendPid integer`, `TransactionId bigint` and `OpenedAt timestamptz` (`Rev869BCommandContextSql.cs:20-27`). The terminalizer now reads context fields through `c` and compares them to the corresponding attempt target fields through `a` (`:116-142`). No reference to assumed `a.OpenedAt`, `a.BackendPid` or `a.TransactionId`, dynamic SQL, or broad exception handling remains. The package functions/relations are transferred to the security owner at `:233-235`. |
| 2. Quarantine evidence | **FAIL** | The new append-only outcome contains lease/request/attempt/execution instance, target identity, source/observed state, evidence kind, reason, actor/issuer, operation, version, outcome, timestamp and evidence (`rev869b-control-plane-install.sql:60-75`). New transitions validate inputs and write attempt, event and outcome transactionally (`:127-149`). However replay selects only by lease/request and returns at `:130-134` before reading the lease. Its comparison omits the caller's `expected_version`; a replay with a false expected version succeeds and returns the old `LeaseVersion`. Thus mismatched evidence can be reported as completed. Actor/issuer are also only nonempty caller-supplied text, with no authoritative actor/issuer record against which mismatch can be tested. |
| 3. Purge retry linkage | **FAIL** | Retry rows bind root authorization, unique prior attempt and batch, target hash, policy, ordinal, prior outcome/evidence; exact linked retries are locked and compared (`Rev869BCommandContextSql.cs:38-53,158-169`). But the `prior_attempt IS NULL` branch rejects an old failure only when target, operation, scope, cutoff and maximum all equal (`:161-163`). After a failure, changing cutoff, maximum, scope or target hash makes that `EXISTS` false and permits a new root. This is the prohibited authorization/target/retention substitution. Unique `PriorAttemptId` prevents branches only after the caller admits the retry. |
| 4. Target ACL closure | **FAIL** | Relation, sequence, database, schema, default-ACL, role capability and inheritance checks are substantially widened (`Rev869BCommandContextSql.cs:211-230`). Function `actual`, however, is restricted to `p.proname LIKE 'rev869b_%'` at `:208`; ownership is likewise restricted at `:225`. The authoritative predecessor migration creates `nexa.rev869a_block_history_mutation`, `nexa.rev869a_guard_controlled_version` and `nexa.rev869a_guard_used_uom_conversion` (`20260810120000_Rev869AIdentityMasterScopeFoundation.cs:903-928`). An arbitrary ordinary role can therefore hold direct EXECUTE on one of those target functions without entering the function ACL delta. Install-time revocation from named roles and `PUBLIC` does not remove or detect every arbitrary direct grant. |
| 5. All 34 pinned scenarios | **FAIL** | Exactly 34 tests compile and are discovered, but 32 bodies are `RunAsync(inventory)` delegates (`Rev869BCorrection17PostgresScenarios.cs:9-39,51,67-93`). `RunAcceptanceScenarioAsync` sends the contract and failpoint label to one external endpoint and accepts a payload signed by the configured key (`Rev869BLifecycleControllerClient.cs:61-89`). The verifier compares returned labels/counts/identities and checks hashes only for 64-hex shape (`:92-139`); expected before/after hashes and durable evidence provenance are absent from the contract. The external controller implementation is not in the reviewed diff or repository. T01/T03 allocation and release responses are not signed (`:50-58,165-200`). Consequently none of the 34 source bodies independently performs or observes its claimed PostgreSQL behavior. |

The offline source tests for column and ACL strings pass, but a passing string scan is not contrary evidence to the semantic replay, retry, ACL-inventory and behavioral-proof failures above.

## 4. Individual 34-scenario reconciliation

All 34 IDs are unique and discoverable. Every result is FAIL because the reviewed source lacks scenario-local, behavior-derived evidence satisfying all required pins. The reasons below are scenario-specific; the common signed-envelope limitation applies to P01 through A02 and T02.

| ID | Result | Exact reconciliation |
|---|:---:|---|
| P01 | **FAIL** | Signed `ExternalVerified` labels do not show the canonical verifier query or its returned catalogue/ACL rows. |
| P02 | **FAIL** | Expects synthetic SQLSTATE `22012`/`pg_catalog.int4div`; it does not prove rejection at the mismatched manifest field. |
| P03 | **FAIL** | Also expects the verifier's division-by-zero sentinel rather than a pinned changed definition/grant and its exact rejected delta. |
| L01 | **FAIL** | No source-present provisioning phases or durable Ready transition are executed; the controller supplies all facts. |
| L02 | **FAIL** | “Every create phase” is aggregated into one response with no per-phase attempt, interruption, restart and recovery evidence. |
| L03 | **FAIL** | No two real actors/barrier or exact losing attempt is visible in the body. |
| L04 | **FAIL** | Target/role absence and finalization are signer assertions, not independently observed cleanup facts. |
| L05 | **FAIL** | No mismatched marker/catalogue fixture and no separate use/drop denial evidence; quarantine replay is version-bypassable. |
| R01 | **FAIL** | No source-present valid decision, consumption row and exact action execution are observed. |
| R02 | **FAIL** | Same/changed replay is aggregated; wrong, expired, foreign-target, pre-state, action and nonce variants and preservation of an unused valid decision are absent. |
| R03 | **FAIL** | No deterministic cleanup failure, surviving evidence, fresh decision and recovery sequence is implemented in the body. |
| C01 | **FAIL** | No transaction creates and verifies business rows, histories, receipt and terminal outcome atomically. |
| C02 | **FAIL** | No lost-response boundary, restart/retry and authoritative receipt read is performed locally. |
| C03 | **FAIL** | No exact original fixture and changed fingerprint replay are executed; SQLSTATE/object are returned labels. |
| C04 | **FAIL** | `TR_rev869b_command_receipt_failpoint` exists only as contract text; no fixture trigger creation/action/rollback verification is in reviewed source. |
| C05 | **FAIL** | No real rollback and independent terminalization/readback is executed. |
| C06 | **FAIL** | Four interruption boundaries collapse into one contract and the non-exact union terminal string `CommittedRolledBackOrAbandoned`. |
| C07 | **FAIL** | No concurrent commands, barrier, winner/loser IDs or durable losing SQLSTATE are observed. |
| C08 | **FAIL** | Backend, actor, organization, role and operation substitutions are aggregated and not individually pinned or executed. |
| G01 | **FAIL** | Missing, expired, wrong-target, wrong-batch and wrong-organization cases are aggregated without distinct authorization/attempt facts. |
| G02 | **FAIL** | Zero rows are allowed by contract, but no authoritative eligible-row query/fingerprint proves a genuine empty candidate set. |
| G03 | **FAIL** | No exact candidate rows, deletion set, preserved histories and committed audit row are read by the test. |
| G04 | **FAIL** | No deterministic candidate drift mutation or before/after rollback evidence is implemented locally. |
| G05 | **FAIL** | `TR_rev869b_purge_delete_failpoint` is a label only; no fixture trigger, rolled-back deletion and separately committed failure evidence is shown. |
| G06 | **FAIL** | Concurrency and all retry substitutions collapse into one response; the new-root policy-substitution bypass remains in SQL. |
| E01 | **FAIL** | No as-of organization-field fixture, minimized row set, immutable batch and exact counts are independently read. |
| E02 | **FAIL** | No later ledger insertion and unchanged prepared-batch fingerprint are performed and compared. |
| E03 | **FAIL** | Expired, wrong-terminal and concurrent-release denials are aggregated without individual release IDs and exact failures. |
| E04 | **FAIL** | No delivery interruption, durable first outcome and fresh second release sequence is executed. |
| A01 | **FAIL** | The test trusts a signed `Verified` response; it does not enumerate effective privileges, and the target function inventory is incomplete. |
| A02 | **FAIL** | Principals, object categories and ungranted functions are aggregated; the REV869A-function direct-grant gap is not exercised. |
| T01 | **FAIL** | It calls allocation/release, but responses are unsigned and the body lacks command, authorization, attempt, before/after fingerprint and terminal evidence pins required for every scenario. |
| T02 | **FAIL** | “Any scenario” is not an exact failed fixture; no restart boundary and surviving cleanup evidence are observed by the body. |
| T03 | **FAIL** | Two unsigned allocations prove only unequal lease/database/hash strings. There are no command/authorization/attempt/durable IDs, barriers, mutations, cross-read denial, or signed cleanup outcomes. |

Additional common defects prevent the generic envelope from curing these results:

- The contract normally defaults both before and after counts to `1`; it does not pin expected fingerprints.
- `BeforeSha256`, `AfterSha256`, state hashes, fixture hash and durable-evidence hash are checked for shape, not derivation or relationship.
- One signer can assert setup completed, action reached, affected rows, SQLSTATE, object identity, durable evidence, target absence and role absence without the test querying any authoritative store.
- Compound scenarios have one command/authorization/attempt/evidence tuple, so distinct subcases cannot be proven individually.
- No reviewed controller implementation establishes genuine fixture setup, failpoints, barriers, restarts, cleanup, or PostgreSQL exception capture.

## 5. Frozen architecture and preserved behavior

| Frozen boundary | Assessment |
|---|---|
| External provisioning | **RETAINED.** No CREATE/DROP DATABASE or role provisioning was added to migration/application/test code. It remains an external prerequisite. |
| Dedicated lifecycle controller | **RETAINED.** Tests call an HTTPS controller and do not receive lifecycle-administrator credentials. The missing controller implementation/evidence is a prerequisite, not authority to merge this boundary. |
| Surviving control-plane database | **RETAINED.** Lease, recovery, lifecycle and quarantine evidence remains in the dedicated control-plane package. Quarantine replay binding must be corrected within this boundary. |
| Target-local transactional ledgers | **RETAINED.** Command, purge and export ledgers remain target-local. Terminal columns are corrected; purge retry and target ACL verification need source correction within the same boundary. |

Frozen architecture state: **RETAIN**. No finding requires an architecture amendment.

Purchase preservation: **PASS for unchanged scope.** The 11-file diff changes no Purchase model, endpoint, application/domain workflow, approval threshold, GST calculation/provenance, history or permission implementation; changes to the migration SQL helper do not alter EF model/snapshot identity or REV869A data. Model/snapshot parity passes, and all 442 non-PostgreSQL tests pass. Previously approved Purchase behavior therefore remains preserved by source-diff evidence.

## 6. Reproduced safe offline validation

| Validation | Independent result |
|---|---|
| Solution build, no restore | **PASS**; 5 projects; 0 warnings; 0 errors. |
| Focused REV869B suite excluding `Postgres`/`PostgreSql` | **PASS**; 68 passed, 0 failed, 0 skipped. |
| Complete suite excluding `Postgres`/`PostgreSql` | **PASS**; 442 passed, 0 failed, 0 skipped. |
| PostgreSQL compilation/discovery only | 87 PostgreSQL/PostgreSql-named overall; 59 REV869B-named; exactly 34 Correction 21 matrix tests; **0 executed**. |
| Exact scenario inventory | **PASS for enumeration only**; 34 unique discovered IDs in the required order. Behavioral probity **FAILS** as Section 4 records. |
| Authoritative SQL-column contract scan | Automated focused test **PASS**; independent physical-table reconciliation **PASS** for blocker 1. |
| ACL closure matrix scan | Automated focused test **PASS**; independent complete-function inventory **FAIL** for blocker 4. |
| PowerShell 5.1 AST | **PASS**; version 5.1.19041.6456; 24 files; 0 parse errors; no helper executed. |
| EF migration discovery | **PASS** with `--no-connect` and inert `127.0.0.1:1`; 13 migrations; applied state unknown by design. |
| Migration ordering/uniqueness | **PASS**; REV869A count 1 ordinal 12; REV869B count 1 ordinal 13; adjacent. |
| Model/snapshot parity | **PASS**; 1 passed, 0 failed, 0 skipped. |
| Offline Up SQL, REV869A to REV869B, `--no-transactions` | 265,204 bytes; 2,321 lines; SHA-256 `32EF1BFD9E0C84C79F6516E562E57204EEEB152D830F8B498FEE4B9AEDA2EC26`. |
| Offline Down SQL, REV869B to REV869A, `--no-transactions` | 10,257 bytes; 214 lines; SHA-256 `2BFE91FA3F9F3DE54F3D8FED15E020EA40AF7E4C78A2BF67FD64085563144940`. |
| Generated SQL prohibited operations | 0 CREATE DATABASE; 0 DROP DATABASE; 0 `pg_terminate_backend`. |
| Temporary SQL artifacts | Removed; 0 remain. |
| Changed executable source/test/tool secret scan | 10 executable changed files; 0 embedded password/client-secret/private-key/access-token/bearer assignments. |
| Changed executable privacy scan | 0 DOB/payroll/bank/government-ID assignments. |
| Changed executable prohibited-scope scan | 0 CREATE/DROP DATABASE; 0 CREATE/DROP ROLE/USER; 0 backend termination; 0 AWS/OIDC additions. |
| `git diff --check` for the exact range | **PASS**. |

Build, C# tests, EF generation and discovery do not parse or execute PostgreSQL function bodies. Their PASS results do not override the source-semantic findings. No helper, provisioning action, migration apply/remove, lifecycle action, quarantine, recovery, purge or export action was executed.

## 7. Remaining external prerequisites

These remain blocking and cannot be satisfied source-only:

1. Externally provisioned dedicated lifecycle-controller and target roles/databases with no administrator credentials exposed to tests.
2. Pinned isolated PostgreSQL system identifier, exact endpoint, TLS/SPKI, environment, source commit and package manifest.
3. Independently deployed lifecycle controller whose reviewed implementation supports the exact provisioning, lease, quarantine, recovery and cleanup state machine.
4. Surviving control-plane database installed from a canonical approved package under its external owner.
5. Management-issued, short-lived, single-use recovery, purge and export decisions delivered through an approved secret/identity channel.
6. Reviewed scenario implementation for 34 deterministic fixtures, actions, failpoints, barriers, restarts, database-derived evidence and cleanup, with signed provenance that cannot be fabricated by merely echoing a contract.
7. Separate explicit authorization for read-only PostgreSQL preflight/verification and later for behavioral PostgreSQL execution.

External prerequisite blocking state: **YES**. External provisioning cannot cure the four source defects identified here; both must be resolved before execution authorization.

## 8. Exact next gate

The exact next gate is a source-only failure-reconciliation of this report that defines a minimal bounded correction for:

1. quarantine replay binding to the supplied pre-transition version and authoritative actor/issuer evidence;
2. an unbypassable purge retry-chain identity that rejects relabeling through any policy substitution;
3. complete target-function ownership/effective-EXECUTE closure across every `nexa` function, including REV869A and arbitrary ordinary roles; and
4. 34 scenario-local, behavior-derived acceptance implementations with separately pinned subcases and evidence provenance, rather than externally signed assertion labels.

Only after that correction is committed may another independent source-only re-review occur. PostgreSQL access, PostgreSQL tests, provisioning/helper execution, migration apply/remove, lifecycle, quarantine, recovery, purge, export and production acceptance remain unauthorized. No further correction was begun in this review.
