# REV869B Correction 22 source correction checkpoint

Date: 2026-08-14

## 1. Entry gate and authority

- Starting HEAD: `d571a08e6ba691da8e1dc1a803df7c6bf73f8b42`
- Starting parent: `6e2e39660867843df1389b9b165d6b4f93118a12`
- Branch: `master`
- Correction 21 source commit `b24ba9a7d813f3e2c32ac8fe69275423cbc12cc1` remains in ancestry.
- Correction 21 reconciliation SHA-256: `AED651B959ADD5D8FF863668C77BE19C8D535729800C21CF9236B55AE60C65B4` (matched before editing).
- The target-scoped worktree was clean at entry.
- EF no-connect discovery found exactly 13 migrations. REV869A is ordinal 12 and REV869B is ordinal 13; both are unique and adjacent.
- `../legacy-reference/` was not opened, read, modified, staged or committed.
- No history rewrite, reset, cherry-pick, merge, rebase or deletion was performed.

The authoritative Correction 21 checkpoint, independent re-review, failure reconciliation, exact Correction 21 diff and frozen-architecture review were read before implementation. Correction 22 remains bounded to the failure-reconciliation authorization.

## 2. Exact bounded file reconciliation

The correction modifies 10 executable source/test/tool files and creates this one checkpoint: 11 files total.

| # | File | Disposition | Bounded purpose |
|---:|---|---|---|
| 1 | `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | Modified | Durable rollback proof, authoritative target identity, purge-chain binding and complete target ACL/owner universe. |
| 2 | `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs` | Modified | Adds the purpose-specific quarantine-attempt registration API to the frozen contract. |
| 3 | `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs` | Modified | Models immutable lifecycle-attempt authority and exact quarantine binding. |
| 4 | `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | Modified | Pins scenario-specific IDs, target database/instance, hashes and compound subcase keys for all 34 scenarios. |
| 5 | `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs` | Modified | Requires source-version and lifecycle registration fields in quarantine evidence. |
| 6 | `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | Modified | Makes every scenario consume exact signed evidence and adds the T03 action-removal mutation guard. |
| 7 | `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | Modified | Adds objective Correction 22 rollback, target identity, retry, ACL, authority and 34-scenario contract scans. |
| 8 | `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | Modified | Requires signed allocation/action/cleanup envelopes and exact contract-bound evidence. |
| 9 | `tools/rev869b-control-plane-install.sql` | Modified | Registers authoritative lifecycle attempts before quarantine and binds all attempts to execution/actor/issuer/operation/request/evidence. |
| 10 | `tools/rev869b-control-plane-verify.sql` | Modified | Reconciles exact function grants/signatures, schema owner and every default-ACL owner. |
| 11 | `outputs/rev869b_source_correction_checkpoint_22.md` | Added | This report. |

The other three authorized files were inspected or preserved but not changed: `Rev869BCommandContextAuthorizer.cs`, `Rev869BPurgeCoordinator.cs`, and `Rev869BTestDatabaseLease.cs`. No file outside the exact 14-file allowlist changed.

## 3. Consolidated root-cause corrections

| Reconciliation group | Correction 22 implementation | Objective offline evidence |
|---|---|---|
| RC21-01 lifecycle/quarantine authority and replay | `rev869b_lifecycle_attempts` now durably owns `ExecutionInstanceId`, `ActorId`, `ActorIssuer`, `Operation`, `RegistrationRequestId`, and `AuthorityEvidenceSha256`. The lifecycle API registers a purpose-specific quarantine attempt before the audit principal may terminalize it. Audit derives authority fields from that attempt, binds request and attempt, records `SourceLeaseVersion`, and exact replay rejects a changed source version or evidence. | Focused source contracts require every field, the lifecycle/audit split, immutable outcome, exact replay comparison and corrected grants/signatures. |
| RC21-02 target identity and purge root | A singleton immutable `rev869b_target_instance_identity` binds database name, instance UUID and SHA-256. Its fingerprint is available only through the verifier API. Purge registration and start join the supplied authorization to this local fact. A new root is rejected while any failed/interrupted attempt for the target and operation has no exact child retry, independent of substituted scope/cutoff/max-row labels. | Source tests require singleton creation, immutability, verifier exposure, registration/start joins and unresolved-child rejection. |
| RC21-03 complete ACL universe | Target ownership and fingerprints cover the complete `nexa` schema: schema, tables/partitions, sequences, views, materialized views, foreign tables and every function (including REV869A). Effective function grants are compared across every `nexa` function. Default ACL inspection covers every defining owner and `PUBLIC`; schema owner is exact. Control-plane verifier signatures and grants are reconciled to the corrected APIs. | Focused ACL contracts and exact verifier deltas pass; no sampled `rev869b_%` effective-function or owner restriction remains. |
| RC21-04 rollback proof | `RolledBack` uses the durable attempt's target backend PID and transaction ID, requires no committed receipt, proves the original transaction is not active, and rejects a surviving mismatched context. It no longer requires the transaction-local context row that rollback removes. Existing physical column type/alias ownership remains preserved. | `Correction22RollbackProofUsesDurableAttemptIdentityWithoutRequiringRolledBackContext` plus table-derived alias/type checks pass. |
| RC21-05/06 exact scenario evidence and frozen matrix | Every contract derives unique exact fixture, command, authorization, attempt, optional decision, durable-evidence and cleanup-evidence IDs; exact target database and target-instance hash; exact before/after/durable/cleanup hashes; exact counts, SQLSTATE/object identity, terminal/cleanup outcomes and scenario-specific subcase keys. Allocation, action and cleanup responses are signed and contract-hash bound. All 34 bodies consume the exact acceptance contract. T03 proves removing its intended action changes the signed contract hash before exercising independent fixtures. | Inventory contract checks, signature checks, exact evidence comparisons, 34-body scan and discovery-only enumeration pass. |

## 4. Thirty-four scenario contract matrix

For scenario `ID`, exact deterministic values are derived as follows: SHA-256 is lowercase SHA-256 of `REV869B-C22|ID|purpose`; each GUID is the first 16 digest bytes; the target database is `sess_nexaerp_rev869b_` plus the first 24 hex characters of the database digest. These values are serialized into the signed contract and compared exactly to returned evidence. No shared evidence record is accepted.

| ID | Required action evidence | Exact result / denial | Scenario-specific durable subcases |
|---|---|---|---|
| P01 | Canonical external verifier | `ExternalVerified`; exact manifest object identity | `p01-action` |
| P02 | Pinned external preflight | `22012`, `pg_catalog.int4div(integer,integer)` | `p02-action` |
| P03 | Full catalogue/effective-grant verifier | `22012`, canonical verifier object | `p03-action` |
| L01 | Reserved-to-ready provisioning | `Ready` | `l01-action` |
| L02 | Restart after every create phase | `Ready` | reserved, database-created, roles-created, migration-applied, verified, ready |
| L03 | Concurrent lifecycle attempt | `40001`, active-attempt unique index | ready-race, inuse-race, single-cleanup-winner |
| L04 | Drop and exact finalization | `Finalized` | drop-started, sessions-terminated, database-absent, roles-absent, finalized |
| L05 | Identity mismatch denial/quarantine | `42501`, target identity mismatch; `Quarantined` | mismatch-detected, use-denied, drop-denied, quarantine-authorized, quarantined |
| R01 | Consume exact management decision | `Finalized` | `r01-action` |
| R02 | Same/changed decision replay | `42501`, recovery-decision replay | first-consumption, same-action-replay-denied, changed-action-replay-denied |
| R03 | Fresh linked recovery after cleanup failure | `Finalized` | `r03-action` |
| C01 | Atomic business/receipt/outcome commit | `Committed` | `c01-action` |
| C02 | Lost-response authoritative replay | `Committed` | `c02-action` |
| C03 | Changed request replay | `23505`, request replay mismatch | `c03-action` |
| C04 | Receipt failpoint rollback | `P0001`, exact failpoint trigger; `RolledBack` | `c04-action` |
| C05 | Explicit rollback terminalization | `RolledBack` with durable backend/transaction proof | `c05-action` |
| C06 | Restart reconciliation at four phases | `CommittedRolledBackOrAbandoned` | before-open, after-open, during-commit, after-response |
| C07 | Concurrent command attempt | `40001`, active-attempt constraint | `c07-action` |
| C08 | Substituted binding denial | `42501`, exact attempt binding | backend, actor, organization, role, operation |
| G01 | Invalid purge authorization matrix | `42501`, exact batch binding | missing, expired, wrong-target, wrong-batch, wrong-organization |
| G02 | Verified empty candidate set | `ZeroRows`; exact zero before/after | `g02-action` |
| G03 | Frozen candidate deletion | `Succeeded` | `g03-action` |
| G04 | Candidate drift | `40001`, candidate-drift constraint | `g04-action` |
| G05 | Delete failpoint and independent failure | `P0001`, exact trigger; `Failed` | `g05-action` |
| G06 | Concurrency and exact retry root | `42501`, retry binding | concurrent-start, concurrent-execute, substituted-policy-denied, exact-retry |
| E01 | Minimized immutable batch | `Prepared` | `e01-action` |
| E02 | Immutable as-of reread | `Prepared` | `e02-action` |
| E03 | Invalid release matrix | `42501`, release sequence | `e03-action` |
| E04 | Interrupted delivery/new release | `InterruptedThenReleaseStarted` | `e04-action` |
| A01 | Complete effective privilege inventory | `Verified` | `a01-action` |
| A02 | Protected direct-access denial | `42501`, protected-object ACL | runtime, purge, export, recovery, ordinary-principal, public |
| T01 | Controller-owned exact fixture | `InUse`, then signed `Finalized` cleanup | `t01-action` |
| T02 | Cleanup after scenario failure/restart | `Finalized` | `t02-action` |
| T03 | Mutation-sensitive concurrent isolation | `Finalized`; action-removal changes contract hash | fixture-a, fixture-b, barrier, isolation, cleanup |

Each contract additionally requires exact schema/table/constraint/function/trigger identity, exact before/after counts and hashes, a unique durable evidence ID/hash, exact terminal outcome, signed cleanup evidence, target absence, role absence, zero unrelated mutations and finalized cleanup. Generic exception-only, label-only, shared-record, missing-fixture and unproved zero-row acceptance are rejected.

## 5. Frozen architecture and external prerequisites

Frozen architecture is retained:

- External provisioning remains external and was not invoked.
- The dedicated lifecycle controller remains the only holder of lifecycle authority.
- The control-plane database remains separately surviving and controller-owned.
- Command, purge and export ledgers remain target-local and transactional.

Correction 22 changes bindings and evidence contracts only; it does not introduce a new service, database, trust boundary or execution path.

External prerequisites remain unavailable and intentionally untested: provisioned pinned PostgreSQL cluster/control plane, exact role/bootstrap state, pinned TLS/SPKI and manifest, lifecycle-controller endpoint and signing key, controller support for the corrected API/evidence schema, and separately authorized execution credentials. Their absence blocks PostgreSQL execution evidence but does not require another source redesign. No source-safety or execution-helper-readiness PASS is declared here; those decisions belong to the next independent re-review and later separately authorized execution gate.

## 6. Offline validation

| Validation | Result |
|---|---|
| Warning-as-error solution build | PASS; 0 warnings, 0 errors. |
| Focused REV869B non-PostgreSQL tests | PASS; 71 passed, 0 failed, 0 skipped. |
| Complete non-PostgreSQL suite | PASS; 445 passed, 0 failed, 0 skipped. |
| PostgreSQL acceptance discovery/compilation only | PASS; exactly 34 Correction 22 scenarios enumerated; none executed. |
| PowerShell 5.1 AST | PASS; version 5.1.19041.6456; 24 files; 0 parse errors; no helper executed. |
| EF no-connect discovery | PASS; inert `127.0.0.1:1`; exactly 13 migrations. |
| Migration uniqueness/order | PASS; REV869A ordinal 12, REV869B ordinal 13, unique and adjacent. |
| Model/snapshot parity | PASS; explicit no-connect parity test 1/1. |
| Offline Up SQL | PASS; 8,040 lines; 1,527,501 UTF-8 bytes; SHA-256 `8492D3D3FA4702C1D7454ADE96FFAFA96E6E00B556DE1046F728DC96BEA3FCB2`. |
| Offline Down SQL | PASS; 4,544 lines; 147,601 UTF-8 bytes; SHA-256 `BC106348BDDAD9E038636C49A494C01A75E8B2064B9293F10677F28049C6DBA0`. |
| Generated SQL prohibited operations | PASS; 0 `CREATE DATABASE`, 0 `DROP DATABASE`, 0 `pg_terminate_backend`. |
| SQL-column/rollback/ACL contract scans | PASS within the focused suite. |
| Added-line secret scan | PASS; 0 password/client-secret/private-key/access-token/bearer literal assignments. |
| Added-line privacy scan | PASS; 0 DOB/payroll/bank/government-ID literal assignments. |
| Added-line prohibited-scope scan | PASS; 0 database/role create/drop, AWS/OIDC or backend-termination additions. |
| `git diff --check` | PASS. |

Build and test artifact writes were limited to normal workspace `bin`/`obj` outputs and are ignored. No PostgreSQL connection or test was made. No provisioning/helper, migration apply/remove, lifecycle, purge, recovery, quarantine or export execution occurred.

## 7. Exact next gate

The next gate is a fresh independent source-only safety re-review of the committed Correction 22 diff and this checkpoint. That review must independently re-evaluate all five blockers and all 34 scenario contracts, reproduce the offline validations, verify the exact committed file boundary and hashes, and decide source-safety and helper-readiness states. PostgreSQL execution remains prohibited until a later explicitly authorized gate with every external prerequisite present.

Stop after Correction 22. Do not implement Correction 23 and do not run PostgreSQL.
