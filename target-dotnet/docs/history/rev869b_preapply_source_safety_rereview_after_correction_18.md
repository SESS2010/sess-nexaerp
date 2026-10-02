# REV869B pre-apply source safety re-review after Correction 18

Date: 2026-08-14 (Asia/Calcutta)

Review type: fresh independent source-only safety re-review

Reviewed commit: `a3e61edc6c59db12e427a106fbe46dfd91461822`

Reviewed parent: `c2d5fb208cf7657ab817a1f5ca651f9a2c5ba7a4`

Reviewed subject: `Correct REV869B control-plane safety checkpoint 18`

Exact diff: `c2d5fb208cf7657ab817a1f5ca651f9a2c5ba7a4..a3e61edc6c59db12e427a106fbe46dfd91461822`

## 1. Verdict

Correction 18 makes material improvements: it adds a cluster-bound preflight, separates purge consumption from execution, removes caller grants on the generic lifecycle transition function, checks the full ordinary-drop lease binding, introduces purpose-specific finalization, binds command open/terminal calls to an exact attempt ID, stages committed attempt evidence inside the business transaction, revokes active old command-authority grants, and separates export authorization from reading.

Those changes do not close the six Correction 17 blockers. Provisioning remains non-resumable after partial bootstrap and its readiness checks are not exact catalogue/effective-ACL proofs. Lifecycle failure recording contains an impossible `Quarantined` outcome call, and successful drop-to-finalization is not restart-safe. Recovery authority is still self-asserted input. Failed purge attempts are labelled retryable but cannot be retried. Command idempotency is a process-wide environment value with database-wide uniqueness, not a request/command binding, and validation occurs after an unterminalized grant is issued. Export authorizations are indefinitely replayable and can return rows that did not exist when the audit was written. The 25 PostgreSQL scenarios are real compiled tests, but many still prove unrelated denials, share bodies, omit the named behavior, or cannot pass against the committed SQL.

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```

No PostgreSQL execution, provisioning, migration application, purge, recovery, export, protected business action, or Correction 19 is authorized.

## 2. Entry gate and exact scope

The entry gate passed before this report was created:

- HEAD, parent and subject matched the required values exactly.
- Target-scoped `git status --short --untracked-files=all -- .` was empty.
- The repository-level status showed only pre-existing untracked content under the prohibited sibling `../legacy-reference/`. That sibling was not enumerated, opened, hashed, copied, staged, modified or committed.
- EF no-connect discovery found 13 migrations. `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation` occurs exactly once, immediately after `20260810120000_Rev869AIdentityMasterScopeFoundation`.
- The exact committed diff passes `git diff --check` and contains 16 paths, 469 insertions and 194 deletions.
- Authoritative input hashes: checkpoint 18 is 6,156 bytes / SHA-256 `115B683645E515EE1165FB5DBD011FFF919F476735A13F327070F23D299F5D2A`; the Correction 17 rereview is 36,516 bytes / SHA-256 `90CF20CBEF35E4B7AC02E4628FD106D5DD3256A0F262CF6B5027A367566F8EE9`.

Exact changed-path scope:

1. `outputs/rev869b_source_correction_checkpoint_18.md`
2. `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
5. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`
10. `tools/manage-rev869b-control-plane-secure.ps1`
11. `tools/rev869b-control-plane-bootstrap.sql`
12. `tools/rev869b-control-plane-deprovision.sql`
13. `tools/rev869b-control-plane-install.sql`
14. `tools/rev869b-control-plane-preflight.sql`
15. `tools/rev869b-control-plane-rollback.sql`
16. `tools/rev869b-control-plane-verify.sql`

## 3. Correction 17 finding-by-finding disposition

| Correction 17 blocker | Result | Independent Correction 18 disposition |
|---|---|---|
| C17-N01 provisioning preflight, target identity, verification and rollback | **FAIL / BLOCKING** | The new preflight binds a supplied system identifier/address/port and rejects pre-existing package identities, but bootstrap is a non-transactional multi-step mutation with no partial-failure resume/removal path. Source-commit input is format-checked, not independently pinned. Verification remains count/name based and does not prove exact definitions or complete effective ACL closure. |
| C17-N02 lifecycle and recovery authorization/state enforcement | **FAIL / BLOCKING** | Generic transition grants are removed and exact drop inputs are checked. However recovery issuer identity remains supplied rather than registry-authoritative; an ordinary failed drop calls an outcome shape rejected by SQL; and Dropped-to-Finalized is a separate non-idempotent consumer step that can strand a lease. |
| C17-N03 purge reachability and durability | **FAIL / BLOCKING** | The audit-writer/executor split makes valid begin reachable, and deletion plus terminal evidence are atomic. On execution failure, however, the function commits authorization state `Failed` while writing `RetryEligible=true`; the only execution API requires `Started`, so controlled retry/recovery is impossible. |
| C17-N04 command attempts, idempotency and terminal linkage | **FAIL / BLOCKING** | Exact attempt IDs and atomic slot/attempt terminal writes close the prior post-commit split. The idempotency key is nevertheless read only from a process environment variable and uniquely constrained database-wide; no request caller supplies it. Grant issuance precedes environment/binding validation, leaving unterminalized grants on validation or uniqueness failure. |
| C17-N05 role/ACL/export closure | **FAIL / BLOCKING** | Active old issuer/runtime function grants are revoked and direct generic lifecycle access is absent. Both control-plane and target verification still sample privileges rather than reconstructing all effective ACLs. Export reading has no expiry or one-read guard and is not snapshot-bound to its earlier audit. |
| C17-N06 genuine, isolated, executable and probative PostgreSQL tests | **FAIL / BLOCKING** | The 25 methods compile, are discoverable and use isolated disposable databases/connections, but many remain label-to-unrelated-assertion mappings; several cannot establish their named property and at least one cannot produce the expected result against current SQL. |

## 4. Detailed source evidence

### C17-N01 - provisioning is not failure-resumable or exactly verified

Positive evidence:

- `tools/manage-rev869b-control-plane-secure.ps1:80-86,110-117` requires externally supplied cluster/manifest/execution fields and invokes preflight before bootstrap.
- `tools/rev869b-control-plane-preflight.sql:2-18` checks the control-system identifier, server endpoint, session user, empty target database and absence of all package role names.
- `tools/rev869b-control-plane-deprovision.sql:2-25` supplies a cluster-guarded full database/role removal path after a completed schema rollback.

Blocking evidence:

- `tools/rev869b-control-plane-bootstrap.sql:8-22,57` performs 15 unconditional `CREATE ROLE` statements and an unconditional `CREATE DATABASE` through one psql script. These operations are not one atomic transaction. A failure after any successful statement leaves package identity behind.
- Mandatory preflight then rejects the partial installation because `rev869b-control-plane-preflight.sql:10-18` requires both the database and every package role to be absent. `RollbackAuthorized` calls the target-database schema rollback first (`manage-rev869b-control-plane-secure.ps1:126-131`), so it cannot clean an early failure where the target database/schema was never completed. There is no partial-bootstrap resume or deprovision-only mode.
- `manage-rev869b-control-plane-secure.ps1:84` and `rev869b-control-plane-preflight.sql:8` accept any syntactically valid 40-hex source commit; neither pins it to the reviewed commit nor records/checks it in installed catalogue state.
- `tools/rev869b-control-plane-verify.sql:14-20,41-43` verifies relation column counts and a total index count, not exact column names/types/defaults/constraints/index definitions. `rev869b-control-plane-install.sql:459-477` counts function names and selected properties but does not compare exact identity arguments, result types, bodies or trigger definitions.
- `rev869b-control-plane-verify.sql:26-30` checks direct table privileges only for PUBLIC and the API role. It does not close database CONNECT, schema, relation, sequence, function, owner, inherited membership and default privileges for every role and arbitrary extra grantee.

Failure scenario: bootstrap creates several roles and then fails. Every subsequent authorized provision attempt fails preflight because those roles now exist, while rollback cannot connect to an absent/incomplete target schema. Manual state mutation outside the helper would be required. Alternatively, a same-count substituted table/index/function definition or an extra effective grantee can pass readiness.

Required correction: make bootstrap/deprovision an explicit restart-safe state machine with a safe partial-cleanup entry, pin and persist the reviewed source/manifest identity, and compare exact catalogue definitions and complete effective ACL allowlists rather than totals and sampled principals.

### C17-N02 - recovery authority and lifecycle failure/finalization remain incomplete

Positive evidence:

- The generic `rev869b_transition_database_lease` function is no longer granted to API/recovery callers (`rev869b-control-plane-install.sql:522-540`).
- `rev869b_begin_database_drop` checks the supplied lease fields (`:238-266`), and recovery permits `PreCreate` (`:308-316`).
- Recovery attempts and outcomes are separate immutable append-only rows (`:66-81,92-95,331-374`).

Blocking evidence:

- `rev869b_consume_recovery_approval` requires only a nonempty supplied `approval_issuer` and a constant supplied `approval_authority` (`:308-317`). There is no authoritative management-issuer registry/signature/credential binding; the recovery credential can self-assert both values.
- The SQL drop-outcome wrapper permits only `Dropped` or `CleanupFailed` (`rev869b-control-plane-install.sql:269-292`). The ordinary disposal failure consumer calls it with `observedPostState="Quarantined"` (`tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs:820-828`). That call must fail, leaving the control-plane lease in `DropStarted` while the target-side marker may be changed to Quarantined.
- Successful ordinary and recovery outcome recording is followed by a separate autocommit finalization call (`Rev869BControlPlaneRegistry.cs:136-162,199-240`). A crash after outcome commit but before finalization leaves state `Dropped`. Retrying the public consumer repeats the already-inapplicable outcome call and never reaches finalization. The source exposes no idempotent reconcile consumer for this interval.
- `rev869b_finalize_database_lease` proves the existence of events/outcomes (`rev869b-control-plane-install.sql:377-405`), but source availability of this low-level function does not repair the non-idempotent consumer flow.

Failure scenario: DROP succeeds and the audit-writer commits `Dropped`; the process terminates before `FinalizeLeaseAsync`. A retry rejects outcome replay/state mismatch and the lease remains permanently Dropped. On a failed ordinary DROP, the consumer attempts an SQL-invalid Quarantined outcome and produces an aggregate failure rather than authoritative quarantine state.

Required correction: bind recovery approval to an independently authoritative issuer/decision, support the exact failed-drop state through a purpose-specific wrapper, and make outcome-plus-finalization or its retry path idempotent and restart-safe.

### C17-N03 - purge failure recovery contradicts its durable evidence

Positive evidence:

- `ConsumerPrincipal` and `ExecutorPrincipal` are distinct (`Rev869BCommandContextSql.cs:160-161`); begin binds the audit writer (`:350-418`) and execution binds the purge executor (`:421-439`).
- Destructive work and success evidence share one transaction (`:458-492`); the PL/pgSQL exception subtransaction rolls deletion back before failure evidence is appended (`:493-514`).

Blocking evidence:

- On any caught execution error the function sets authorization state to `Failed` (`:496-497`) and writes `RetryEligible=true` (`:498-510`). The only executor entry requires authorization state `Started` (`:431-438`). No function transitions `Failed` back to `Started`, creates a replacement execution from the durable candidate set, or reconciles an interrupted execution.
- If a caller rolls back the outer transaction, failure evidence disappears and the prior Started state reappears; if it commits/autocommits, evidence persists but retry is prohibited. Thus the checkpoint claim that durable Started work remains retryable is false in both evidence/retry combinations.

Required correction: provide a purpose-specific, approval-bound retry/reconcile state transition that preserves the first failure and candidate fingerprint, or terminalize failed work and require a new authorization with an explicit linkage.

### C17-N04 - idempotency is process-global and grant issuance can remain nonterminal

Positive evidence:

- Attempts use an identity sequence and exact attempt ID (`Rev869BCommandContextSql.cs:118-150,620-705`).
- `rev869b_record_command_outcome` atomically appends all slot terminal rows and the exact attempt outcome (`:768-831`). Committed evidence is staged before the business commit (`Rev869BCommandContextAuthorizer.cs:126-132`; `EfRev869BPurchaseService.cs:41-68`).

Blocking evidence:

- The only production source of the purported external idempotency key is `Environment.GetEnvironmentVariable("REV869B_COMMAND_IDEMPOTENCY_KEY")` (`Rev869BCommandContextAuthorizer.cs:96-98`). No endpoint, request model or middleware sets/passes a per-command value; a repository search finds no other production occurrence.
- The database uniqueness rule is `UNIQUE("DatabaseInstanceFingerprint","IdempotencyKeyFingerprint")` (`Rev869BCommandContextSql.cs:131-133`). A stable process environment value therefore allows one command for the lifetime of that database. Multiple protected commands, concurrent requests, or multiple authorization opens in one service transaction collide even when their business fingerprints differ.
- Grant issuance occurs at `Rev869BCommandContextAuthorizer.cs:63-81`; execution/service/ownership/idempotency validation occurs afterward at `:83-109`. Missing/malformed environment data or a uniqueness collision leaves the already committed grant and Issued audit without an attempt terminal outcome.
- The read-only `rev869b_reconcile_command_outcome` returns an existing outcome ID (`Rev869BCommandContextSql.cs:833-839`); it cannot reconcile the grant-without-attempt interval.

Required correction: accept a caller/request-scoped idempotency key through the application contract, bind it to the exact command/business fingerprint and intended retry semantics, validate all inputs before issuance, and atomically issue the grant and durable attempt or provide a terminal reconciler for issuance failure.

### C17-N05 - ACL verification and export closure remain incomplete

Positive evidence:

- Authority rotation revokes active prior issuer/runtime function grants before granting the new pair (`Rev869BCommandContextSql.cs:842-876`).
- Purge and export principals receive narrow explicit functions and no direct ledger table privileges (`:911-936`).

Blocking evidence:

- Target ACL closure samples three purge roles, several functions and a few owner tables (`:937-958`). It does not reconstruct every effective privilege across database, schemas, all relations/sequences/functions, ownership, membership inheritance, PUBLIC and default ACLs; export-role exact closure is not in this predicate.
- Control-plane verification has the analogous sampled-principal/count defects described in C17-N01.
- Export authorization is immediately marked Consumed and audited before reading (`Rev869BCommandContextSql.cs:261-293`), but `rev869b_export_minimized_security_ledger` checks neither current time/`ExpiresAt` nor a one-read state (`:295-316`). The same authorization and nonce can be replayed indefinitely.
- The audit stores only a row count computed at authorization time (`:279-291`), while the reader executes a fresh live query each time (`:311-315`). Rows inserted after authorization can be exported without corresponding exact count/snapshot evidence.

Required correction: verify complete effective ACL allowlists and make export release single-use, expiry-checked and bound to an immutable result snapshot/fingerprint with exact release evidence.

### C17-N06 - PostgreSQL scenario design is still not independently probative

The 25 corrected tests are not merely empty labels: they compile, are discovered, create disposable owned databases, use distinct least-privilege connections in several paths, and contain real concurrency/fault fixtures. They were not executed in this review. Source inspection still yields the following failures:

| # | Scenario source-only result |
|---:|---|
| 1-2 | **FAIL:** both map to the identical `LifecycleTraceAsync` happy path (`Rev869BCorrection14PostgresDesignTests.cs:8-9`; scenario body `Rev869BCorrection17PostgresScenarios.cs:11-19`). No phase interruption or restart matrix exists. |
| 3 | **FAIL:** only removes an environment variable; it constructs no filesystem-only authorization and proves no target-absence reconciliation (`:21-32`). |
| 4-5 | **FAIL:** mutate migration fingerprint or use a random run ID and assert any exception (`:34-44`); no target-marker mismatch, stale/expired lease, duplicate reservation or race is constructed. |
| 6-8, 11 | **FAIL:** every approval starts from `Executing` (`:46-55`), while SQL recovery accepts only PreCreate/Failed/Quarantined/DropStarted/CleanupFailed. The supplied target fingerprint is also random (`:57-72`). The tests therefore prove unrelated denial, not issuer/state/expiry/post-state validation. |
| 9-10 | **FAIL:** both map to identical `RecoveryReplayAsync`; the first consume is expected to fail and no valid approval is ever consumed (`:75-85`). No failed recovery/outcome/non-reuse evidence exists. |
| 12 | **PARTIAL / FAIL overall:** missing approval and rejection audit are real (`:104-114`), but there is no fresh-success contrast in this named test. |
| 13 | **FAIL:** changes many fields simultaneously (`:116-133`) and cannot attribute database/policy/executor/cutoff/batch failures independently. |
| 14 | **PARTIAL:** concurrency is real and the corrected principal split makes a winner plausible (`:136-152`), but it does not verify complete consumption/attempt state. |
| 15 | **PARTIAL / FAIL overall:** proves a zero-row label/count only (`:154-174`), not the complete authorization/audit/pre/post contract. |
| 16 | **PARTIAL:** injects a real delete fault and checks preservation/failure count (`:207-241`), but not exact SQLSTATE/object, authorization terminal state or a usable recovery path. |
| 17 | **FAIL and non-executable as claimed:** subtracting one second from the sole fixture's `ReservedAt` (`:217-218`) does not change candidate IDs/count; candidate fingerprint hashes IDs only. Execution should succeed, contradicting the expected failed terminal result. |
| 18-19 | **PARTIAL / FAIL overall:** each tests one direct DML denial (`:243-255`), not the named approved-route/full protected-object matrix. |
| 20 | **FAIL:** the owner fabricates a grant/security-audit row (`:176-205`) and the assertion checks only grant deletion and security-audit survival (`:257-279`); it never creates or verifies a durable command attempt/outcome. |
| 21 | **FAIL:** checks runtime SELECT denial on three tables and performs one authorized export (`:281-334`), but does not test INSERT/UPDATE/DELETE, function denial, export replay, expiry, rollback or post-authorization row drift. |
| 22 | **FAIL:** injects failure while setting command context and rolls back (`:336-373`); it performs no protected business mutation and checks no business pre/post state or terminal attempt evidence. |
| 23 | **PARTIAL / FAIL overall:** proves one immutable-trigger SQLSTATE/constraint as owner (`:375-386`), not the least-privilege acceptance/rejection route. |
| 24 | **FAIL:** reuses the empty zero-row body and only asserts absence of Succeeded (`:154-174`); no eligible row is constructed to detect false zero-row success. |
| 25 | **FAIL:** proves only distinct PIDs/identity strings (`:388-403`); no concurrent actor operation, verifier observation, winner/loser or cleanup behavior is tested. |

The eight focused source-contract tests do not supply behavioral proof. For example, `ProvisioningDefinesExactRolesObjectsOwnershipAndEffectiveAclFailure` asserts the presence of role strings, count predicates and function names (`Rev869BCorrection17SourceContractTests.cs:33-54`); `TwentyFiveFutureScenariosUseRealFixturesConcurrencyAndExactPaths` checks global substrings across the harness rather than each named test body (`:118-133`). Their passing result is structural only.

## 5. Reconciled offline validation totals

| Validation | Independent result |
|---|---|
| Build: `dotnet build SESS.NexaERP.slnx --no-restore --nologo` | **PASS**; 0 warnings, 0 errors |
| Complete suite excluding names containing `Postgres` or `PostgreSql` | **PASS**; 453 passed, 0 failed, 0 skipped |
| Focused Correction 17 source contracts plus no-connect model/snapshot parity | **PASS**; 9 passed, 0 failed, 0 skipped |
| Exact PostgreSQL class discovery, list only | 50 discovered: 18 direct safety, 7 application behavior, 25 corrected scenarios; **0 executed / NOT RUN** |
| PowerShell 5.1 AST | **PASS**; 24 files, 0 parse errors; version 5.1.19041.6456 |
| `GeneratePlanOnly` | **PASS as offline operation**; `PostgreSqlAccessed=false`, `ContainsCredential=false`; manifest SHA-256 `667C6D0DE84521DF184D3F69BD23EB674B1EEA9278A3DCE34CEE6273C594B583` |
| EF migration discovery | **PASS** with `--no-connect` and inert `127.0.0.1:1`; 13 migrations; applied state unknown |
| Migration uniqueness/order | **PASS**; REV869B exactly once immediately after REV869A |
| Model/designer/snapshot parity | **PASS**; 1/1 no-connect test (included in focused total) |
| Offline Up SQL, REV869A to REV869B | **PASS generation only**; 281,656 bytes, 2,858 lines, SHA-256 `A65AD308E92746280732A9836787C166044A7D6CF70BCC39A3D4FBFD26AB38B6` |
| Offline Down SQL, REV869B to REV869A | **PASS generation only**; 11,139 bytes, 167 lines, SHA-256 `677FBA805101025E676BB2230795F88692A89C65BDA8FA4DDFD3DFDB190AB011` |
| Generated SQL prohibited operations | 0 CREATE DATABASE, 0 DROP DATABASE, 0 `pg_terminate_backend` in both generated files |
| Exact committed diff | 16 files; 469 insertions; 194 deletions |
| `git diff --check` | **PASS** |
| Target status before report | clean |
| Provisioning/helper source adequacy | **FAIL**; C17-N01 |
| Lifecycle/recovery completeness | **FAIL**; C17-N02 |
| Purge durability/recovery | **FAIL**; C17-N03 |
| Attempt/idempotency/terminal closure | **FAIL**; C17-N04 |
| ACL/export closure | **FAIL**; C17-N05 |
| PostgreSQL test probative design | **FAIL**; C17-N06 |

The generated SQL existed only in verified operating-system temporary paths for hashing/inspection and was removed. It was not sent to PostgreSQL. Passing compilation, structural source contracts and SQL generation do not adjudicate the blocking behavioral findings.

## 6. Remaining external prerequisites

The following remain external, closed and unexecuted:

1. A one-day architecture freeze/root-cause review covering partial provisioning recovery, authoritative management approval, lifecycle terminalization, purge retry state, request-scoped idempotency and export snapshot/single-use semantics.
2. A corrected source design that closes every BLOCKING item above before Correction 19 is authorized.
3. A separately governed immutable PostgreSQL cluster identity plus exact TLS/host allowlist and reviewed source/manifest allowlist.
4. Exact capability-minimized roles and credentials, provisioned only through a restart-safe, fully reversible package.
5. Independent issuer/recovery/purge/export authorization services and non-rollbackable/idempotent reconciliation boundaries.
6. Rewritten PostgreSQL tests with one named behavior per isolated fixture, exact SQLSTATE/object/state assertions, interruption matrices and authoritative pre/post/durable evidence.
7. A later, separately authorized source-only review. PostgreSQL execution is not the next gate.

None was provisioned, accessed or assumed as evidence.

## 7. Prohibited operations and exact next gate

No PostgreSQL connection was opened. No PostgreSQL, provisioning, lifecycle, recovery, purge, export or protected purchase test/action was executed. No role or database was provisioned. No migration was applied or removed. No generated SQL was executed. No source, migration, test, helper or configuration file was edited. `../legacy-reference/` remained unread, unmodified, uncommitted and unstaged.

Exact next gate: **stop after this report; impose a one-day REV869B architecture freeze and root-cause review. Do not authorize or begin Correction 19.** After the freeze, require a reviewed corrective source plan addressing all six blockers, then a new source-only correction and fresh independent source-only rereview. PostgreSQL execution remains unauthorized.
