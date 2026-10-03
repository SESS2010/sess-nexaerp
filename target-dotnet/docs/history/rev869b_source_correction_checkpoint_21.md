# REV869B source correction checkpoint 21

Date: 2026-08-14

Starting commit: `7a1e4739b733acb4a90594fa4112cad52aa0f71c`

Authority: `outputs/rev869b_preapply_source_safety_rereview_after_correction_20.md`, SHA-256 `829791495B94042F865278EA83A48B2868A59FD9A2FCB6B1C3715ED595C70502`.

This is a bounded source-only implementation checkpoint, not an independent source-safety approval. External provisioning, the dedicated lifecycle controller, the surviving control-plane database, and target-local transactional ledgers remain separate trust boundaries. No PostgreSQL connection or test, provisioning/helper execution, migration apply/remove, lifecycle operation, purge, recovery, quarantine, export, production, frontend, AWS, OIDC, REV869C, or independent Correction 21 review was performed.

## 1. Bounded committed inventory

The Correction 21 commit contains exactly these 11 files:

1. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
2. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
3. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs`
9. `tools/rev869b-control-plane-install.sql`
10. `tools/rev869b-control-plane-verify.sql`
11. `outputs/rev869b_source_correction_checkpoint_21.md`

No migration identity, designer, snapshot, Purchase entity/service/endpoint, provisioning helper, production, or frontend file changed. The ending commit is the commit containing this checkpoint and is reported in the handoff because a commit cannot embed its own SHA.

## 2. Five-blocker implementation map

| Blocker | Exact source implementation | Exact offline evidence |
|---|---|---|
| Command terminalization SQL | `rev869b_record_noncommit_outcome` now reads open-state evidence only from `rev869b_command_contexts` and joins `c."BackendPid"`/`c."TransactionId"` to authoritative attempt columns `a."TargetBackendPid"`/`a."TargetTransactionId"`. Rejected, RolledBack and Abandoned predicates are distinct; receipts still prohibit noncommit terminalization. No dynamic SQL or broad exception catch was added. | `TerminalizationReferencesOnlyAuthoritativeColumnsWithExactTypes` extracts columns from the two authoritative `CREATE TABLE` definitions, scans every `a.` and `c.` quoted reference, checks exact integer/bigint contracts, rejects the three invalid attempt-column names, and rejects dynamic/broad handling. |
| Quarantine evidence | The surviving control plane gains append-only `rev869b_quarantine_outcomes`, binding outcome, lease/request/attempt/execution instance, target database/cluster, source/observed target state, mismatch/interruption/retry-failure kind, reason, actor, issuer, operation, version, timestamp, principal and SHA-256. `rev869b_record_quarantine` requires exact nonempty bindings, enforces active-attempt identity, creates a dedicated quarantine attempt when none exists, terminalizes before transition, writes event plus outcome, and accepts replay only when every binding matches. Recovery requires a fresh attempt and marks an interrupted active attempt `Interrupted`; the former conflict-update rebinding path is removed. | `QuarantineRecoveryActionAndTerminalReplayAreDatabaseBound` checks the ledger, every binding, immutable trigger, exact attempt constraint, interruption evidence, freshness constraint and absence of an `ON CONFLICT` recovery rebind. Control-plane verification inventories the new relation and corrected function signature. |
| Purge retry linkage | Each authorization now binds `RootAuthorizationId`, unique `AuthorizedBatchId`, target-instance SHA-256, fixed operation, prior attempt, prior terminal outcome/evidence, retry ordinal, scope/cutoff/maximum, nonce, expiry and decision. Initial authorizations cannot bypass an existing failed/interrupted chain. Retries lock and compare the exact preceding attempt/event/policy, advance ordinal by one, and cannot branch because `PriorAttemptId` is unique. Start consumes only the exact fresh authorized batch; authorization/attempt deletion is denied and terminal events remain immutable. | `PurgeFreezesCandidatesAndBindsAOneWayMonotonicRetryChain` checks every column/FK/unique index, exact prior outcome/evidence comparison, monotonic assignment, exact batch constraint and purge-worker/audit separation. `Rev869BPurgeCoordinator.RegisterAsync` exposes and binds the complete SQL signature. |
| Target ACL closure | Install explicitly revokes tables, sequences and functions from PUBLIC and every package role before exact grants, and closes table/sequence/function defaults for all package roles. `rev869b_verify_target_catalogue_acl` symmetrically compares the complete business-table allowlist and all seven table privileges across every ordinary effective grantee; enumerates every function grant; denies all non-owner sequence rights, PUBLIC rights, arbitrary direct grants, ownership drift, default ACL grantees, membership drift, database/schema excess and role-capability/login drift. Control-plane install/verify performs the corresponding PUBLIC/package-role/default/role-capability closure. | `FrozenRoleFunctionsHaveExactPurposeGrantsAndNoDirectLedgerDml` requires relation matrices, every ACL dimension, sequence/default revocations and purpose-only grants. `CanonicalVerifierComparesCompleteObjectAndEffectiveAclSets` covers symmetric control-plane object/function/effective ACL inventories. |
| All 34 scenario contracts | The typed inventory now carries exact schema/table/constraint/function/trigger identity, before/after counts, affected rows, SQLSTATE/object when applicable, terminal outcome and cleanup outcome. The client requires an exact HTTPS origin, validates the live TLS certificate SPKI, pins source/manifest/cluster/signing-key hashes, hashes the complete contract, accepts only ECDSA-signed evidence, and requires unique fixture/lease/command/authorization/attempt/durable-evidence/cleanup-evidence IDs, exact state/action/count/fingerprint/object/outcome matches, nonzero behavioral evidence, zero unrelated mutations and finalized cleanup. T01 calls real allocation/release; T03 allocates two concurrent fixtures and proves distinct leases/databases/fixture hashes before dual cleanup. | `AcceptanceInventoryHasExactlyThirtyFourUniqueExecutableDatabaseFacts` executes offline, checks the exact ordered 34 IDs and all pin fields, finds 34 `[Fact]` bodies, 32 signed scenario calls, three real allocation calls and three cleanup calls, and rejects generic exception/source-label patterns. Discovery found exactly 34 matrix tests and executed zero. |

## 3. Exact 34-scenario evidence map

Every row is a typed contract in `Rev869BAcceptanceScenarioInventory` and a same-ID `[Fact]` in `Rev869BCorrection17PostgresScenarios`. Except for the direct T01/T03 allocation bodies, the action result must be inside the ECDSA-signed payload whose SHA-256 contract digest and environment pins match exactly.

| ID | Exact setup and action | Required pinned result/evidence |
|---|---|---|
| P01 | Externally provisioned exact cluster/control plane; run canonical read-only verifier | `ExternalProvisioned -> ExternalVerified`; control-plane manifest/fingerprint identity; nonzero evidence; Finalized cleanup. |
| P02 | Source/TLS manifest mismatch; run external preflight | exact `22012` from `pg_catalog.int4div(integer,integer)` fail-closed branch; unchanged state/count; durable denial and cleanup IDs. |
| P03 | Definition/effective-grant drift; run canonical verifier | exact `22012` from `pg_catalog.int4div(integer,integer)` fail-closed branch; manifest/trigger identity; unchanged state/count. |
| L01 | Exact Reserved disposable lease; provision through controller | `Reserved -> Ready`; lease/mark-ready/event identity and durable finalized cleanup. |
| L02 | Exact interruption fixture at every create phase; restart reconciliation | `Provisioning -> Ready`; active-attempt uniqueness identity; interruption/retry evidence and cleanup. |
| L03 | Active lifecycle attempt plus barrier; start a different concurrent attempt | exact `40001`, `UX_rev869b_one_active_lifecycle_attempt`; unchanged fingerprint/count and durable denial. |
| L04 | DropAuthorized lease and stable attempt; drop/finalize exact target and roles | `DropAuthorized -> Finalized`; lifecycle outcome/finalizer/immutable-trigger identity. |
| L05 | Ready target with marker/catalogue mismatch; verify use/drop denial then quarantine | exact `42501`, `rev869b_target_identity_mismatch`; `rev869b_quarantine_outcomes` identity; `Quarantined` terminal evidence then Finalized cleanup. |
| R01 | Quarantined lease plus fresh unconsumed decision; consume exact action/recover | decision/attempt bound `Quarantined -> Finalized`; recovery table/function/immutable-trigger identity. |
| R02 | Consumed decision; replay same and changed action/attempt | exact `42501`, `rev869b_recovery_decision_replay`; unchanged authoritative state/count and durable denial. |
| R03 | CleanupFailed plus fresh linked decision; recover | `CleanupFailed -> Finalized`; new attempt, decision and durable outcome/cleanup IDs. |
| C01 | Registered request plus exact runtime transaction; commit business/history/receipt/outcome | `AttemptStarted -> Committed`; nonzero row effect; receipt/function/immutable-trigger identity and persistence fingerprints. |
| C02 | Committed command with lost response; replay same request | authoritative receipt returns `Committed`; no duplicate mutation; exact receipt/durable IDs and fingerprints. |
| C03 | Same idempotency key with changed request digest; replay | exact `23505`, `rev869b_command_request_replay_mismatch`; unchanged request fingerprint/count. |
| C04 | Started attempt plus exact fixture trigger `TR_rev869b_command_receipt_failpoint`; attempt commit | exact `P0001` and trigger identity; business and receipt rollback proven by before/after counts/hashes; `RolledBack`. |
| C05 | Opened exact command transaction; roll back and independently terminalize | exact command/execution/service/ownership binding; `RolledBack` outcome ID persists outside the rolled-back transaction. |
| C06 | Separate pinned interruptions before open, after open, during commit and after response; restart reconciler | exact signed subcase evidence summarized as `CommittedRolledBackOrAbandoned`; no forced Abandoned claim after a durable commit. |
| C07 | One request plus barrier; start two differently bound attempts | exact `40001`, `rev869b_command_attempt_active`; single active-attempt count/fingerprint retained. |
| C08 | Exact attempt plus substituted backend/actor/org/role/operation; open or terminalize | exact `42501`, `rev869b_attempt_binding`; no affected rows and unchanged authoritative fingerprint. |
| G01 | Missing/expired/wrong-target/wrong-batch/wrong-org authorization; start purge | exact `42501`, `rev869b_purge_batch_binding`; authorization/attempt/target IDs and unchanged ledger fingerprint. |
| G02 | Fresh scoped exact-batch authorization with verified zero eligible rows; freeze candidates | before and after eligible count exactly zero; explicit `ZeroRows` terminal event/evidence; only permitted zero-row action. |
| G03 | Fresh scoped exact-batch authorization plus eligible temporary contexts and durable histories; execute | exact nonzero frozen deletion; candidate/evidence hashes; histories remain; `Succeeded`. |
| G04 | Started purge plus deterministic candidate drift; execute | exact `40001`, `rev869b_purge_candidate_drift`; deletion rollback, unchanged target fingerprint and durable failure evidence. |
| G05 | Started purge plus exact fixture trigger `TR_rev869b_purge_delete_failpoint`; execute then independent record | exact `P0001`/trigger identity; deletion rolled back; audit principal persists `Failed` evidence. |
| G06 | Concurrent start/execute plus failed prior attempt; race, reject substitutions, authorize exact retry | exact `42501`, `rev869b_purge_retry_binding` for substitution; signed concurrency subcase; same root/target/policy/prior outcome/evidence; monotonic single retry. |
| E01 | Approved organization/fields/row/as-of/expiry scope; prepare | immutable minimized `Prepared` batch; exact authorization/batch/row hashes and nonzero evidence. |
| E02 | Prepared batch; insert later ledger row and reread | prepared row count/hash unchanged; later row excluded; durable batch identity retained. |
| E03 | Expired/wrong-terminal/concurrently active release; read/authorize | exact `42501`, `rev869b_export_release_sequence`; unchanged batch/release fingerprint. |
| E04 | ReleaseStarted plus deterministic delivery loss; record Interrupted and authorize fresh release ID | `InterruptedThenReleaseStarted`; distinct old/new release evidence, immutable prepared batch and Finalized cleanup. |
| A01 | Canonical control-plane and target packages; enumerate every ordinary effective privilege | `Installed -> Verified`; exact function/table/sequence/schema/database/default/role/membership matrices. |
| A02 | Runtime/purge/export/recovery/arbitrary ordinary roles; attempt every protected direct/ungranted capability | exact `42501`, `rev869b_protected_object_acl`; no inheritance/direct-grant bypass and unchanged fingerprints. |
| T01 | Exact opt-in and pinned controller; call `AllocateAsync`, inspect allocation, call `ReleaseAsync` | real controller-owned InUse fixture; exact source/manifest/cluster/TLS pins; unique lease; durable release evidence ID/hash; target/roles absent. |
| T02 | Pinned deterministic scenario failure; dispose and restart cleanup | `CleanupFailed -> Finalized`; surviving control-plane evidence and exact cleanup ID/hash after restart. |
| T03 | Two simultaneous `AllocateAsync` calls with independent IDs/barriers; verify then release both | distinct lease IDs, database names and fixture SHA-256 values; no cross-mutation; two durable Finalized cleanup outcomes. |

For every signed scenario the common contract also requires exact fixture, command, authorization and attempt IDs; exact setup/action/state; exact before/after count and SHA-256; exact database-object identity; exact SQLSTATE when applicable; a durable evidence ID/SHA-256; terminal outcome; cleanup evidence ID/outcome; zero unrelated mutations; target absence and role absence. Generic exceptions, message-only matching, missing/shared fixtures, zero-row mutation success and label-only source assertions cannot satisfy the client.

## 4. Reproduced safe offline validation

| Validation | Result |
|---|---|
| Solution build, no restore | PASS; 5 projects; 0 warnings; 0 errors. |
| Focused REV869B suite excluding `Postgres`/`PostgreSql` | PASS; 68 passed, 0 failed, 0 skipped. |
| Complete suite excluding `Postgres`/`PostgreSql` | PASS; 442 passed, 0 failed, 0 skipped. |
| Authoritative SQL-column contract scan | PASS inside focused suite; table-definition-derived alias scan and exact type checks executed. |
| ACL closure matrix scan | PASS inside focused suite; complete matrix/default/ownership/role dimensions required. |
| Exact 34-contract inventory | PASS inside focused suite; 34 unique ordered IDs, 34 facts, 32 signed calls, three allocations and three releases. |
| PostgreSQL compilation/discovery only | 87 PostgreSQL/PostgreSql-named tests overall; 59 REV869B-named; exactly 34 Correction 21 matrix tests; **0 executed**. |
| PowerShell 5.1 AST | PASS; version 5.1.19041.6456; 24 files; 0 parse errors; no helper executed. |
| EF migration discovery | PASS with `--no-connect` and inert `127.0.0.1:1`; 13 migrations; applied state intentionally unknown. |
| Migration ordering/uniqueness | PASS; REV869A count 1 ordinal 12; REV869B count 1 ordinal 13; adjacent. |
| Model/snapshot parity | PASS; explicit no-connect test 1 passed, 0 failed, 0 skipped. |
| Offline REV869A -> REV869B Up SQL, `--no-transactions` | 265,204 bytes; 2,321 lines; SHA-256 `32EF1BFD9E0C84C79F6516E562E57204EEEB152D830F8B498FEE4B9AEDA2EC26`. |
| Offline REV869B -> REV869A Down SQL, `--no-transactions` | 10,257 bytes; 214 lines; SHA-256 `2BFE91FA3F9F3DE54F3D8FED15E020EA40AF7E4C78A2BF67FD64085563144940`. |
| Generated SQL prohibited operations | 0 `CREATE DATABASE`; 0 `DROP DATABASE`; 0 `pg_terminate_backend`. |
| Temporary SQL artifacts | Removed; 0 remain. |
| Changed executable source/test/tool secret scan | 0 embedded password/client-secret/private-key/access-token/bearer assignments. |
| Changed executable source/test/tool privacy scan | 0 DOB/payroll/bank/government-ID literals. |
| Changed executable source/test/tool prohibited-scope scan | 0 database/role create/drop, AWS, OIDC or backend-termination operations. |
| Terminal dynamic/broad handling scan | 0 executable findings; two textual hits are negative `Assert.DoesNotContain` tests. |
| `git diff --check -- .` | PASS. |

Offline generation, compilation, discovery and source tests do not prove PostgreSQL syntax or behavior. No PostgreSQL connection was attempted and no PostgreSQL test body ran.

## 5. Frozen architecture and Purchase preservation

The frozen architecture is retained without a competing design:

1. external IaC provisions cluster roles and databases;
2. the dedicated lifecycle controller owns target creation/drop orchestration;
3. the surviving control-plane database owns durable lifecycle, quarantine and recovery evidence;
4. the target database owns transactional command, purge and export ledgers.

No Purchase source, migration identity/designer/snapshot, REV869A data/model, endpoint, permission, approval-threshold, GST-calculation/provenance, history or segregation implementation changed. The complete 442-test non-PostgreSQL suite and the explicit model/snapshot parity test remain green.

## 6. Remaining external prerequisites and explicit nonclaims

The following remain external and blocking for operational acceptance:

1. External IaC provision of the exact NOINHERIT, capability-minimized control-plane and target roles/databases, closed memberships/database/schema/object/default privileges and rotated credentials.
2. A pinned isolated PostgreSQL cluster identity, endpoint and TLS/SPKI plus exact source and package manifests.
3. External lifecycle-administrator installation of the reviewed control-plane package in the surviving control-plane database.
4. Deployment of the dedicated lifecycle controller/reconciler and management approval writer outside application/test processes.
5. Controller support for the exact contract SHA-256, TLS pin, ECDSA signing public key pin, signed evidence envelope, 34 deterministic fixtures/actions/failpoints/barriers/restarts, evidence IDs and cleanup designs recorded above.
6. Separate authorization for read-only PostgreSQL preflight/verification and for PostgreSQL behavioral execution.
7. A fresh independent source-only safety re-review of the exact committed Correction 21 source set and its parent.

Explicit nonclaims:

- REV869B source safety PASS is unclaimed; this implementation checkpoint cannot self-approve.
- Execution-helper/control-plane readiness is unclaimed and externally blocked.
- PostgreSQL syntax/behavior/persistence/rollback/restart/replay/concurrency/denial/cleanup acceptance is unclaimed; 0 PostgreSQL tests executed.
- Provisioning, migration apply/remove, lifecycle, quarantine, recovery, purge, export, production, AWS, OIDC and frontend acceptance is unclaimed.

Exact next gate: perform a fresh independent source-only safety re-review of the committed Correction 21 diff against starting commit `7a1e4739b733acb4a90594fa4112cad52aa0f71c`. Do not access PostgreSQL or begin another correction in this task.
