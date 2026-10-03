# REV869B pre-apply source safety re-review after Correction 19

Date: 2026-08-14 (Asia/Calcutta)

Review type: fresh independent source-only safety re-review

Reviewed commit: `9917812388c54a874df6061a32451878a6c88728`

Reviewed parent: `c8b692070c4257623877db42803510116ff1d830`

Reviewed subject: `Correct REV869B control-plane safety checkpoint 19`

Exact reviewed diff: `c8b692070c4257623877db42803510116ff1d830..9917812388c54a874df6061a32451878a6c88728`

## 1. Verdict

Correction 19 adopts the frozen ownership direction: external provisioning owns cluster roles/databases, the in-repository helper is plan/read-only verification only, the bootstrap/deprovision scripts are deleted, test allocation is routed through a typed HTTPS lifecycle-controller client, and target-local command receipts and purge success evidence are placed in their associated transactions. No second cluster-provisioning implementation was added.

The source gate nevertheless fails. The control-plane verifier compares object names/owners and selected ACL sets rather than canonical definitions and all effective privileges. Quarantine is present only as a state label and pure-model transition, not as a reachable control-plane API. Recovery actions are not enforced by the finalizer/drop APIs, and cleanup-failure recording is not idempotent. Command noncommit terminalization can be performed by the audit role against any known attempt and silently accepts mismatched replay evidence. Purge ignores its approved scope, does not validate `PriorAttemptId`, and gives failure-recording authority to the destructive worker. Export materializes every field regardless of the approved field subset and permits an unexpired release to become indefinitely readable and multiply released. Target ACL closure has no exact verifier.

The future PostgreSQL acceptance design also remains non-probative. The P01-T03 tests are source substring checks grouped behind labels; they do not create isolated fixtures or execute their named actions. The controller's opaque acceptance endpoint is called only for R03, and a handful of booleans/counts can satisfy it without evidence binding to a lease, cluster, fixture, SQLSTATE/object, or before/after digest. These tests cannot establish the required persistence, rollback, restart, replay, concurrency, denial and cleanup behavior.

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```

No PostgreSQL execution, provisioning, helper execution, migration apply/remove, database test, purge, recovery, quarantine, export, production/AWS/OIDC/frontend work, or Correction 20 is authorized by this review.

## 2. Entry gate and authoritative inputs

The entry gate passed before this report was created:

- HEAD, parent and subject matched the required values exactly.
- Target-scoped `git status --short --untracked-files=all -- .` was empty.
- Repository-wide status exposed only two pre-existing untracked paths under the prohibited sibling. Their contents were not enumerated, opened, hashed, copied, modified, staged or committed; `../legacy-reference/` remained unread and outside all pathspecs used for the review.
- `outputs/rev869b_architecture_freeze_root_cause_review.md`: 51,939 bytes; SHA-256 `FBD74D7663BB3FD989158DB97C5544A2DA31307E5113DD5C12283E7959BC1B08`.
- `outputs/rev869b_source_correction_checkpoint_19.md`: 14,149 bytes; SHA-256 `CD41495678E87AFC6415E5F9DE115AEF30A919175CAD73CCD79EEAA6DB3682C2`, exactly matching the supplied checkpoint hash.
- EF `--no-connect` discovery and migration-source enumeration each found 13 migrations. REV869A is ordinal 12 and REV869B ordinal 13; each occurs once and REV869B is immediately after REV869A.
- The exact diff contains 30 paths: 2 added, 2 deleted and 26 modified; 1,280 insertions and 4,579 deletions. It passes `git diff --check`.

## 3. Exact 30-file diff reconciliation

Every changed path is inside the frozen Correction 19 allowlist. No migration ID/class/designer/snapshot, earlier migration, domain schema, frontend, REV861, REV869C, AWS or OIDC file changed.

| # | Status | Path | Reconciliation |
|---:|:---:|---|---|
| 1 | A | `outputs/rev869b_source_correction_checkpoint_19.md` | Required checkpoint; its SHA-256 matches, but two validation claims do not reproduce exactly (section 6). |
| 2 | M | `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs` | Adds caller idempotency to protected qualification changes; retained business routes remain. |
| 3 | M | `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs` | Removes the prior live export API route; retained Purchase route permissions remain. |
| 4 | M | `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | Replaces target command/purge/export ledgers and ACLs; contains blocking binding, purge, export and verifier defects. |
| 5 | M | `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs` | Adds request/attempt/receipt flow and independent audit connection; database terminal ownership remains insufficiently bound. |
| 6 | M | `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs` | Threads request idempotency through comparison/PO commands; no business-scope expansion found. |
| 7 | M | `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.MaterialFollowUp.cs` | Threads request idempotency through material follow-up. |
| 8 | M | `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs` | Threads request idempotency through RFQ/quotation commands. |
| 9 | M | `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs` | Stages receipts before commit and noncommit outcomes after rollback; retained GST/approval/history logic remains. |
| 10 | M | `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs` | Structural control-plane contract only; cannot prove PostgreSQL catalogue/ACL behavior. |
| 11 | M | `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs` | Pure lifecycle model; its quarantine graph is not implemented by control-plane SQL. |
| 12 | M | `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | Label-to-report/source mapping; not a PostgreSQL behavior body. |
| 13 | M | `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs` | Source-only contract; useful structural guard, not behavior proof. |
| 14 | M | `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | Ten substring tests aggregate P01-T03 labels; blocking acceptance-design failure. |
| 15 | M | `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | Source-only structural assertions; not behavior proof. |
| 16 | M | `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs` | Source-only structural assertions; does not close target effective ACLs. |
| 17 | A | `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | Typed HTTPS client with no lifecycle-admin credential; opaque scenario evidence is under-bound. |
| 18 | M | `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs` | Removes direct lifecycle administration and uses target runtime/audit roles. |
| 19 | M | `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs` | Seven genuine application behavior bodies remain compiled and controller-allocated. |
| 20 | M | `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs` | Eighteen genuine direct behavior bodies remain compiled and controller-allocated. |
| 21 | M | `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs` | Retains offline migration/model/business contract checks. |
| 22 | M | `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs` | Uses target-local purpose functions, but failure authority is not independently separated. |
| 23 | M | `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs` | Removes direct create/drop and filesystem authority; delegates allocation/release to HTTPS controller. |
| 24 | M | `tools/manage-rev869b-control-plane-secure.ps1` | Modes are limited to plan, preflight and post-provision verification; no cluster mutation mode remains. |
| 25 | D | `tools/rev869b-control-plane-bootstrap.sql` | Correctly removes the competing in-repository cluster bootstrap path. |
| 26 | D | `tools/rev869b-control-plane-deprovision.sql` | Correctly removes the competing in-repository cluster deprovision path. |
| 27 | M | `tools/rev869b-control-plane-install.sql` | Transactional package for an externally provisioned database; lifecycle/action/verifier findings remain. |
| 28 | M | `tools/rev869b-control-plane-preflight.sql` | Read-only external identity/role/database preflight; exact-definition scope is incomplete. |
| 29 | M | `tools/rev869b-control-plane-rollback.sql` | Schema-only rollback with finalized-lease gate; not executed. |
| 30 | M | `tools/rev869b-control-plane-verify.sql` | Compares partial inventories, not canonical definitions or complete effective ACL closure. |

## 4. Frozen-architecture requirement matrix

| Frozen requirement | Result | Independent evidence |
|---|---|---|
| One selected design; no competing provisioning architecture | **PASS** | Bootstrap/deprovision scripts are deleted; the helper exposes no cluster mutation mode; test allocation uses the HTTPS controller client. |
| External provisioning completely separate from target migration ownership | **PASS** | Target/control-plane packages require externally existing roles/databases and do not issue `CREATE/DROP DATABASE/ROLE`; only assertion strings contain those tokens. |
| Dedicated lifecycle control and surviving evidence through creation, failure, quarantine, recovery and deletion | **FAIL / BLOCKING** | The surviving tables exist, but `Quarantined` has no SQL transition/API; recovery action is not enforced by begin-drop/finalize; cleanup-failure replay is not idempotent. |
| Target-local ledgers correctly scoped and rollback-safe | **FAIL / BLOCKING** | Command receipt and purge success are transaction-coupled, but purge ignores authorization `Scope`; purge failure is recorded by the destructive worker; retry linkage is unvalidated. |
| Exact, non-replayable instance/issuer/command/role/terminal binding | **FAIL / BLOCKING** | Runtime open/commit binding is strong, but the audit role may terminalize any known attempt; mismatched replay is silently accepted by `ON CONFLICT DO NOTHING`; start inputs are caller-supplied. |
| Purge, recovery, quarantine and export least privilege/auditable | **FAIL / BLOCKING** | Recovery action is not enforced, quarantine is unreachable, purge scope/prior-attempt linkage is unused, export ignores approved fields, and target ACL closure has no exact verifier. |
| Failure/interruption recoverable without filesystem-only evidence | **FAIL / BLOCKING** | Filesystem authority was removed, but cleanup-failure/noncommit replay and purge failure/retry paths are not exact; acceptance restart behavior is not implemented in test bodies. |
| Genuine isolated PostgreSQL acceptance bodies proving persistence, rollback, restart, replay, concurrency, denial and cleanup | **FAIL / BLOCKING** | The retained 18 direct + 7 application tests are real bodies, but the required P01-T03 matrix is only ten source substring tests and no executable scenario matrix exists. |
| No pass via zero affected rows, generic exceptions, missing fixtures or label-only assertions | **FAIL / BLOCKING** | P01-T03 pass by string presence and report labels. Controller evidence accepts generic booleans/counts; only R03 calls it. No named per-case fixture/action/state assertions exist. |
| Previously approved Purchase workflow, thresholds, GST, histories, segregation and permissions preserved | **PASS** | Changed production files retain page permissions, request-scoped authorization, configured approval routing, self-approval denial, GST provenance/reconciliation and history writes; focused and complete non-PostgreSQL suites pass. |

## 5. Finding-by-finding disposition

### R19-N01 - control-plane verification is not canonical or complete

**FAIL / BLOCKING.** `tools/rev869b-control-plane-verify.sql:3-36` compares table name/owner pairs, function signature/owner pairs, selected function execution and direct table DML for package-prefixed roles. It does not compare columns, types, defaults, constraints, indexes, triggers, function result/security/volatility/config/body definitions, sequences, default ACLs, schema privileges for non-PUBLIC roles, database privileges for every role, or effective privileges granted to non-prefixed principals. An extra arbitrary grantee or same-signature substituted body can pass.

`tools/rev869b-control-plane-preflight.sql:3-38` likewise checks the seven expected roles but does not reject an unexpected extra package role and does not canonically verify the already-provisioned database/role definitions beyond selected flags, membership and CONNECT closure. These are not the frozen canonical-definition and complete-effective-ACL checks.

Required before reconsideration: canonical catalogue inventories for every required relation/column/default/constraint/index/trigger/function body and complete database/schema/relation/sequence/function/default-ACL/membership allowlists across all grantees.

### R19-N02 - quarantine, recovery action and lifecycle replay are incomplete

**FAIL / BLOCKING.** The control-plane state check includes `Quarantined` (`rev869b-control-plane-install.sql:27`), and the pure model declares transitions into it (`Rev869BControlPlaneRegistry.cs:34-35`), but no installed purpose API can set that state. Cleanup failure always sets `CleanupFailed` (`rev869b-control-plane-install.sql:141-147`). Quarantine is therefore label-only/unreachable.

A recovery decision records `AuthorizedAction`, but after consumption neither `rev869b_begin_drop` nor `rev869b_finalize_absent_target` checks it (`rev869b-control-plane-install.sql:115-121,128-157`). A `FinalizeAbsent` decision can be followed by begin-drop, and a `DropAndFinalize` decision can be finalized directly. Exact management authorization is not enforced at the action boundary.

`rev869b_record_cleanup_failure` requires an unterminated attempt and inserts a unique outcome without an exact-replay branch (`:141-147`). A lost response after commit cannot be safely replayed. This falls short of restart-safe evidence across creation/drop failure and recovery.

### R19-N03 - command terminal ownership and replay binding are not exact

**FAIL / BLOCKING.** Request registration binds organization, operation, caller idempotency digest, request digest, actor, issuer, subject and role. Runtime open/commit additionally binds backend PID, transaction ID and runtime principal, and committed receipt/outcome is atomic with the business transaction (`Rev869BCommandContextSql.cs:59-100`). Those are material improvements.

The noncommit path is not equivalently bound. `rev869b_record_noncommit_outcome` accepts only an attempt UUID plus caller-selected terminal/category/outcome and checks only that `session_user` is the shared command-audit role (`Rev869BCommandContextSql.cs:101-105`). It does not bind the calling execution/service/ownership lease or prove the target transaction is no longer capable of committing. Any holder of that role that learns an active attempt UUID can terminalize it.

On replay, `ON CONFLICT("AttemptId") DO NOTHING` accepts a different outcome ID, terminal state or category and returns the prior outcome instead of rejecting mismatched evidence. This violates exact terminal-outcome binding and non-replayability.

### R19-N04 - purge authorization scope, failure authority and retry linkage are unenforced

**FAIL / BLOCKING.** `rev869b_register_purge_authorization` stores `Scope` and `PriorAttemptId`, but `rev869b_start_purge` selects every old command context solely by cutoff/limit and never uses `Scope` (`Rev869BCommandContextSql.cs:110-116`). The approved scope therefore does not constrain deletion candidates.

`PriorAttemptId` has no foreign key and registration does not prove that it references a Failed/Interrupted attempt with matching scope/cutoff/candidate policy. A purported new-authorization retry can be unlinked or linked arbitrarily.

The same `nexa_rev869b_purge_worker` that executes deletion is granted failure recording (`:118-124,149`). The coordinator opens an autocommit connection by convention, but the granted function can still be invoked inside a caller-controlled transaction. This does not implement the frozen independent purge audit/reconciler authority. Atomic delete+Succeeded evidence is correct, but the full purge contract is not.

### R19-N05 - export minimization/replay and target ACL closure fail

**FAIL / BLOCKING.** Export authorization accepts a subset of four allowed fields, but batch materialization always constructs all four fields and never consults `a."Fields"` (`Rev869BCommandContextSql.cs:126-130`). A one-field approval therefore releases data outside the approved field subset.

`rev869b_authorize_export_release` permits multiple releases for one batch because `BatchId` is not unique in the release table. `rev869b_read_prepared_export_batch` checks only `ReleaseStarted`, not current expiry, and does not consume the release (`:51,132-134`). A started release can be reread indefinitely until separately terminalized; a batch may receive additional release IDs. Audit rows exist, but authorization is replayable and least privilege is not exact.

Target ACL installation revokes/grants selected principals and functions (`:136-153`), but there is no canonical target ACL verifier. `rev869b_read_target_security_state` returns only ledger counts and an attempt-ID digest (`:107-108`). It cannot prove complete effective database/schema/relation/sequence/function/default-ACL/membership closure or detect an arbitrary extra grantee.

### R19-N06 - required PostgreSQL acceptance matrix remains label-only

**FAIL / BLOCKING.** `Rev869BCorrection14PostgresDesignTests.cs:8-19` maps labels to strings in the architecture report/source. `Rev869BCorrection17PostgresScenarios.cs:12-23` groups the 41 P01-T03 cases into ten methods that assert substring presence. None allocates a fixture, invokes PostgreSQL, injects a failpoint, drives restart/concurrency, or verifies authoritative before/after/outcome/cleanup state.

`Rev869BLifecycleControllerClient.RunAcceptanceScenarioAsync` accepts an opaque response if the scenario label matches, `ActionReached` and `CleanupFinalized` are true, `UnrelatedMutationCount` is zero, and `DurableEvidenceCount` is positive (`Rev869BLifecycleControllerClient.cs:36-45`). It does not bind evidence to lease ID, cluster identity, manifest, fixture digest, attempt/decision ID, exact expected state pair, SQLSTATE/object for denial, or evidence digest/signature. Repository search finds only one caller, `RecoverQuarantinedAsync`, hard-coded to R03 (`Rev869BTestDatabaseLease.cs:61-66`).

The 18 direct and 7 application PostgreSQL classes contain real test bodies and controller-owned fixture acquisition, but they do not replace the missing lifecycle/provisioning/recovery/purge/export/ACL matrix. The named acceptance gate can pass with missing fixtures and label-only assertions.

### R19-N07 - checkpoint validation totals and Up hash do not reproduce exactly

**FAIL as checkpoint reconciliation; not a separate architecture blocker.** Independent discovery lists 73 Postgres/PostgreSql-named tests overall and 45 REV869B-named tests, with zero executed. The checkpoint claims 75 overall.

Checkpoint-compatible EF generation with `--no-transactions` reproduces its byte/line counts and Down hash. The actual Up SHA-256 is `54CBC617C9B8738F8FC9C59995C3E5CA6B15375C4317A0A78ECAFBB5F581FD61`. The checkpoint prints `54CBC617C9B8738F8FC9C59995C3E5CA6B15375C4317A0A78ECAFBBF9F5D022986A08C`, which is 70 hexadecimal characters and therefore cannot be a SHA-256 value. This report records the independently reproduced 64-character hash.

## 6. Reproduced safe offline validation

| Validation | Independent result |
|---|---|
| Solution build: `dotnet build ... --no-restore` | **PASS**; 5 projects; 0 warnings; 0 errors. |
| Focused REV869B tests excluding `Postgres` and `PostgreSql` | **PASS**; 63 passed, 0 failed, 0 skipped. |
| Complete suite excluding `Postgres` and `PostgreSql` | **PASS**; 437 passed, 0 failed, 0 skipped. |
| Explicit no-connect model/snapshot parity | **PASS**; 1 passed, 0 failed, 0 skipped (also included in the focused/full totals). |
| PostgreSQL test compilation/discovery only | 73 Postgres/PostgreSql-named overall; 45 REV869B-named; **0 executed / NOT RUN**. |
| Windows PowerShell 5.1 AST | **PASS**; version 5.1.19041.6456; 24 files; 0 parse errors. No helper was executed. |
| EF migration discovery | **PASS** with `--no-connect` and inert `127.0.0.1:1`; 13 migrations; applied state intentionally unknown. |
| Migration order/uniqueness | **PASS**; REV869A ordinal 12, REV869B ordinal 13; each occurs once and is adjacent. |
| Model/designer/snapshot parity | **PASS**; no differences. |
| Offline Up SQL, REV869A to REV869B, `--no-transactions` | 241,416 bytes; 2,219 lines; SHA-256 `54CBC617C9B8738F8FC9C59995C3E5CA6B15375C4317A0A78ECAFBB5F581FD61`. |
| Offline Down SQL, REV869B to REV869A, `--no-transactions` | 10,010 bytes; 211 lines; SHA-256 `80CB8F249EF486FE62A8AC4A1E314662469F0D666994FD48D32E0330A6C032F1`. |
| Transaction-wrapped Up SQL cross-check | 241,447 bytes; 2,222 lines; SHA-256 `6FF4B7CD3994B33542895CED82D92FA42924B014D5277DDE1153FFAD4CC12FA8`. |
| Transaction-wrapped Down SQL cross-check | 10,041 bytes; 214 lines; SHA-256 `65B8F7C864258ADE8BB67AEDF68899A9044042DAF7F7CCECB1CE059C7ACED7D8`. |
| Generated SQL prohibited operations | 0 `CREATE DATABASE`; 0 `DROP DATABASE`; 0 `pg_terminate_backend` in Up and Down. |
| Current executable REV869B source prohibited cluster mutation scan | 0 executable hits; two test assertions contain prohibited text only to assert absence. |
| Credential literal scan | **PASS**; no password, bearer, client-secret, private-key or access-token literal assignment in REV869B source/tests/tools. |
| Durable-ledger privacy scan | **PASS** for reusable secrets; token hits are UUID context identifiers/GUC names, and remarks are stored as SHA-256. |
| Exact committed diff | 30 files; 1,280 insertions; 4,579 deletions. |
| `git diff --check` | **PASS**. |
| Target status before report creation | Clean. |

Both SQL forms were generated only to unique operating-system temporary files, read for size/line/hash/prohibited-token checks and removed. Neither was sent to PostgreSQL. The default transaction-wrapped form differs from `--no-transactions` only by the generated transaction wrapper and is included to make the hashing method explicit.

Passing build, non-database tests, source contracts, compilation and SQL generation does not adjudicate PostgreSQL syntax or behavior and does not override findings R19-N01 through R19-N06.

## 7. Preserved Purchase and business behavior

No changed path alters migration identity, designer, snapshot or earlier business schema. Source inspection confirms retained endpoint page permissions, organization/record-scope checks, role checks, self-approval prohibition, configured Manager/Technical Director/Managing Director thresholds, immutable GST provenance and recalculation, optimistic concurrency, status/approval/PO/configuration histories, qualification verification-before-approval segregation and late-child protections.

The 63 focused REV869B non-PostgreSQL tests and all 437 non-PostgreSQL tests pass. This is sufficient for source-preservation confidence at this gate, but it is not PostgreSQL behavioral acceptance.

## 8. Remaining external prerequisites

The following remain external, unmet and unexecuted:

1. A separately authorized bounded source correction that closes R19-N01 through R19-N06 without adding a competing architecture.
2. A fresh independent source-only review of that exact future correction and parent.
3. Management ratification of lifecycle owner/on-call authority, recovery RTO/nonterminal age, recovery/purge/export approver separation, retention, retry and emergency-admin policies.
4. An externally pinned isolated PostgreSQL system identifier, TLS/SPKI, endpoint/environment classification, clock, monitoring and backup controls.
5. External IaC creation of the exact capability-minimized roles, surviving control-plane database, CONNECT closure, credentials and rotation.
6. External review/installation of the exact control-plane package and deployment of the lifecycle controller/reconciler and approval writer outside application/test processes.
7. Controller-prepared deterministic isolated fixtures and action-sensitive failpoints, plus genuine per-case P01-T03 tests with exact initial/action/result/final/durable/cleanup evidence.
8. Separate authorization before any PostgreSQL verification, provisioning, migration, purge, recovery, quarantine, export, helper execution, database test or production use.

Nothing in this review establishes PostgreSQL syntax acceptance, runtime behavior, production readiness, or external-controller correctness.

## 9. Exact next gate and stop condition

Exact next gate: **stop after committing this report. REV869B remains NO-GO for PostgreSQL and helper execution. A future Correction 20 may begin only under a separate explicit authorization and must be bounded to R19-N01 through R19-N06; after it is committed, require another fresh independent source-only safety rereview before any execution gate.**

This task did not begin or implement Correction 20.
