# REV869B Source Correction Checkpoint 14

Date: 2026-08-13

Starting commit: `b0976299874cd978edf1106e13c2eb79e7047752`

Ending commit: the commit containing this checkpoint (reported after commit; a commit cannot contain its own SHA-1 without changing it).
Scope: controlled source-only correction. PostgreSQL, database helpers, migration application/removal, database creation/drop/recovery/quarantine, purge execution/scheduling, backup/restore, production/REV861/frontend/REV869C, and `legacy-reference` were not accessed or executed.

## Entry gate

HEAD and parent matched `b0976299874cd978edf1106e13c2eb79e7047752` / `fd2fb607756d2de0db8be773fa6f7e874c5440e9`; one Correction 13 independent report was present; target scope was clean; one REV869B followed REV869A; sibling `192d84fa1116975a09e9676a7d8c864f975380f5` was not incorporated. Global status contained only the two previously known untracked legacy ZIP paths. They were not accessed or changed.

## Authoritative finding matrix and corrections

| Finding | Severity/root cause | Controlled correction and enforcement | Evidence/acceptance boundary |
|---|---|---|---|
| C13-N01 | BLOCKING; signed filesystem `PreCreateIntent` was treated as pre-marker authority | Added `Rev869BControlPlaneRegistry`: exact separately provisioned database/owner/API readiness, pooling disabled, no direct table privileges, exact target/run/token hash/owner/source database+fingerprint/source commit/migration set/request time/expiry/issuer+authority/state binding. `ReserveBeforeCreateAsync` precedes `CREATE ROLE`/`CREATE DATABASE`; marker/outcome is cross-bound. Files remain supplemental. | Offline static contract proves ordering and fail-closed strings. Hard interruption/mismatch/replay designs are discovered, NOT RUN. Registry provisioning and PostgreSQL reconciliation remain external gates. |
| C13-N02 | BLOCKING; recovery omitted issuer, exact pre-state and durable result | Recovery envelope now binds issuer, authority, exact pre/post state, approval reference, reason, executor, operation/purpose, database/run/token/owner/source/migration, timestamps and nonce. Control-plane consumption occurs before target access; success and failure call durable outcome API; failed consumption is not reusable. | Static issuer/pre-state/order/outcome contract passes. Exact expiry/replay/substitution/interruption tests are discovered, NOT RUN. |
| C13-N03 | BLOCKING; reusable migration-owner purge and no durable zero/failure evidence | Replaced old signature with database-owner registration of fresh execution-specific approval, atomic one-use begin, exact database/policy/90-day cutoff/batch/max rows/states/expiry/nonce/executor, execution by ID, and separate failure finalization. Started and zero-row evidence commits before deletion; unresolved Started is fail-closed; success/count mismatch is transactional. | Static SQL/inventory passes. No purge ran. Zero, concurrent replay, partial failure and mismatch designs are discovered, NOT RUN. |
| C13-N04 | BLOCKING; purge SECURITY DEFINER audit INSERT authority was not demonstrated | Added exact pre-provisioned LOGIN `nexa_rev869b_purge_executor`, inaccessible to migration owner; caller receives EXECUTE only on begin/purge/failure. Fixed `search_path`; PUBLIC revoked; security owner owns functions/tables and alone inserts immutable attempt evidence through the functions. Runtime/issuer receive no ledger access. | Source privilege scan and tests pass. Runtime permission behavior remains NOT RUN. |
| C13-N05 | REQUIRED CORRECTION/BLOCKING; temporary grants were the only command lifecycle evidence and runtime retained audit SELECT | Added separate append-only `rev869b_command_security_audits`. Issued evidence is independently committed with the issuer grant; Opened/Claimed evidence is appended inside the protected transaction, so accepted business mutation cannot commit without the exact claim audit. Audit stores fingerprints and exact operation/entity/version/status/policy, not credentials/tokens/assertions/remarks. Temporary purge only adds Expired evidence and never removes durable audit. Immutable triggers reject UPDATE/DELETE; runtime/issuer are revoked; runtime audit-log SELECT is revoked. | Static separation/privacy/immutability tests pass. PostgreSQL atomicity and denial designs are discovered, NOT RUN. Exceptional export remains disabled and requires separate approval/source. |
| C13-N06 | BLOCKING; no Correction 13 PostgreSQL design expansion | Added exactly 25 compiled/listable Correction 14 PostgreSQL design names covering all requested lifecycle, recovery, purge, audit, SQLSTATE/object, non-vacuity and independent actor/verifier scenarios. Each design uses two independent connections and a non-zero topology precondition. | REV869B PostgreSQL discovery increased 25 to 50; all 50 are NOT RUN. Behavioral acceptance is explicitly unclaimed. |

Prior C11/C12 concerns remain preserved in source: exact slot/principal/version/transition binding; issuer-before-mutation ordering; nontransactional claim ordinal; qualification compatibility; current-version/late-child guards; least-privilege command ledgers; rollback-safe business/history transactions; and approved retention boundaries. Their PostgreSQL behavior is not claimed.

## Lifecycles

Control plane: `Reserved/PreCreateIntent -> target create -> marker cross-bind -> OwnedActive`; failure records `Quarantined/Failed`. Missing, stale, duplicate, mismatched or unavailable registry evidence fails before provisioning. Normal cleanup/recovery still requires exact live catalogue/marker checks; unproven databases are not reused/repaired/dropped.

Recovery: fresh signed approval -> exact issuer/authority/pre-state/post-state/target binding -> durable single-use control-plane consumption -> live independent proof -> requested drop -> durable Succeeded; any exception -> durable Failed attempt, with approval remaining non-reusable. Filesystem consumption is supplemental only.

Purge: database owner registers exact fresh approval -> dedicated executor atomically begins it -> durable Started or ZeroRows -> bounded eligible temporary rows only -> durable Expired command events -> transactional delete/count check -> durable Succeeded. Execution failure leaves durable Started and requires `record_purge_failure`; no automatic retry or schedule exists.

## Changed paths

1. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
2. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`
3. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
7. `outputs/rev869b_source_correction_checkpoint_14.md`

## Offline validation

| Gate | Result |
|---|---|
| PowerShell 5.1 AST | 23/23; `5.1.19041.6456` |
| Build | succeeded; 0 warnings, 0 errors |
| Focused REV869B | 44/44 |
| Inclusive REV869B non-PostgreSQL | 64/64 (baseline 61; +3 static contracts) |
| Complete non-PostgreSQL | 438/438 (baseline 435; +3) |
| REV869B PostgreSQL discovery | 50 (baseline 25; +25); **NOT RUN** |
| EF discovery | 13 migrations using `--no-connect`; exactly one REV869B immediately after REV869A; applied state unknown |
| Pending model/snapshot parity | empty difference via focused offline test |
| Offline Up SQL | 249,438 bytes; SHA-256 `4FB671D4AA1131E0D9E6D588E9393311361A0702605B1707F6082E9602580E7A` |
| Offline Down SQL | 9,955 bytes; SHA-256 `8D31CB465CE76C05B177C351D92106EC13A9E4EF72C5DD062BEBA52EA3F06AAC` |
| Up inventory | 22 tables, 79 triggers, 32 function definitions, 46 FK occurrences, 70 indexes, 54 checks |
| `git diff --check` | clean (line-ending notices only) |

Offline SQL was generated with an inert loopback design-time identity and was not applied. Temporary SQL files were removed after size/hash/inventory calculation.

## External gates, remaining blockers and unclaimed states

Before any authorized PostgreSQL test/helper work, a separately governed `sess_nexaerp_rev869b_control_plane` owned by `nexa_rev869b_control_plane_owner` must be provisioned with the exact append-only lease/recovery APIs and caller no-direct-table-access contract. The target server must separately pre-provision `nexa_rev869b_security_owner` and `nexa_rev869b_purge_executor` with the specified role topology. Exact environment identity/commit/policy gates remain mandatory. Purge still requires a new database-owner-registered approval for each execution; no schedule is installed.

No PostgreSQL behavior, control-plane implementation, database ownership state, migration applicability/acceptance, purge behavior/acceptance, recovery behavior, production readiness, or final REV869B acceptance is claimed. This checkpoint does not declare either forbidden PASS state. A fresh independent source-only safety re-review is mandatory.
