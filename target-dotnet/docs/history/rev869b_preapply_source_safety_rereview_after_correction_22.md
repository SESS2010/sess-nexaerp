# REV869B Correction 22 independent source-only safety re-review

Review date: 2026-08-14
Workspace: `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet`
Reviewed commit: `5a114cb0dcb4a304916343c1e23f4bf75299132c`
Required parent: `d571a08e6ba691da8e1dc1a803df7c6bf73f8b42`
Branch: `master`

## 1. Verdict

**FAIL.** Correction 22 preserves the previously passing physical-column blocker and materially improves quarantine-attempt binding, target identity, ACL breadth, rollback modeling, and signed scenario envelopes. It does not, however, close the complete reviewed safety contract.

Three of the five Correction 21 blocker statements fail. In addition, four consolidated source/test root causes remain independently reproducible: recovery cannot progress from `RecoveryAuthorized` to `DropStarted` with the bound request; purge retry roots can be reopened after merely registering an unused child; the ACL verifier cannot establish the requested exact privilege universe; and `RolledBack` can accept an active transaction because its capability-free definer cannot reliably observe another role's transaction in `pg_stat_activity`. The scenario layer still supplies deterministic label-derived placeholders through one generic runner instead of scenario-derived database facts, and several frozen scenario actions remain drifted or compressed. Consequently, every one of the 34 scenarios fails source-safety acceptance.

No PostgreSQL connection, test, provisioning action, migration apply/remove, lifecycle action, purge, recovery, quarantine, export, production operation, history rewrite, source correction, or Correction 23 work was performed. `../legacy-reference/` was not accessed.

## 2. Entry gate and reviewed inputs

| Gate | Result | Evidence |
|---|:---:|---|
| HEAD | PASS | `5a114cb0dcb4a304916343c1e23f4bf75299132c` |
| Parent | PASS | `d571a08e6ba691da8e1dc1a803df7c6bf73f8b42` |
| Subject | PASS | `Correct REV869B control-plane safety checkpoint 22` |
| Branch | PASS | `master` |
| Target-scoped worktree before review | PASS | Clean (`git status --short -- .` returned no entries). |
| Required reports read fully | PASS | `rev869b_source_correction_checkpoint_22.md` and `rev869b_correction21_failure_reconciliation.md` were read in full. |
| Exact committed diff read | PASS | The complete parent-to-HEAD diff was inspected for every changed file. |
| REV869A/REV869B uniqueness/order | PASS | Each migration ID occurs once in EF discovery; REV869B is entry 13 immediately after REV869A at entry 12. Each has exactly its migration and designer file. |
| Prohibited adjacent tree | PASS | No access to `../legacy-reference/`. |

## 3. Exact 11-file diff reconciliation

The committed diff contains exactly 11 files, 406 insertions and 95 deletions. Ten files are authorized implementation/test/tool changes and one is the Correction 22 checkpoint. No purchase-domain source, endpoint, permission policy, approval workflow, calculation, entity model, migration identity/designer, or model snapshot changed.

| File | + / - | Independent reconciliation |
|---|---:|---|
| `outputs/rev869b_source_correction_checkpoint_22.md` | 132 / 0 | Correction checkpoint; claims were treated as assertions to verify, not evidence. |
| `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | 37 / 22 | Adds target identity, broad ACL/ownership/default checks, retry-root predicate, durable rollback predicates; contains unresolved retry, ACL and rollback defects detailed below. |
| `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs` | 1 / 1 | Extends quarantine registration API. |
| `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs` | 18 / 0 | Adds lifecycle authority fields and exact replay/version comparisons. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | 28 / 5 | Adds deterministic IDs/hashes and subcase-key labels, while retaining optional/default generic evidence construction. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs` | 1 / 1 | Updates quarantine source-presence checks. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | 21 / 6 | Routes all 34 cases through the common runner; adds signed T01/T03 fixture calls and a T03 serialization-hash check. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | 67 / 2 | Adds source-shape assertions; these do not exercise the disputed SQL semantics or mutation-test all 34 intended actions. |
| `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | 55 / 26 | Adds signed, contract-bound allocation/action/cleanup response verification. |
| `tools/rev869b-control-plane-install.sql` | 37 / 24 | Adds lifecycle attempt authority, quarantine binding and expanded function ownership/defaults; recovery reuses a request ID prohibited by its event unique key. |
| `tools/rev869b-control-plane-verify.sql` | 9 / 8 | Expands function/signature/default ACL reconciliation; effective-role scans remain incomplete/unsatisfiable for the requested universe. |

The three files authorized by reconciliation but unchanged by Correction 22 were also considered where relevant: `Rev869BCommandContextAuthorizer.cs`, `Rev869BPurgeCoordinator.cs`, and `Rev869BTestDatabaseLease.cs`.

## 4. Consolidated Correction 21 root-cause result

| Root cause | Result | Independent finding |
|---|:---:|---|
| RC21-01 authoritative lifecycle/quarantine binding | FAIL | The quarantine path is now bound to an active stored attempt, request, actor, operation, execution instance, authority digest and source lease version. But recovery uses the same `request_id` for the `RecoveryAuthorized` event and subsequent `DropStarted` event while `rev869b_database_lease_events` enforces `UNIQUE(LeaseId,RequestId)`. The exact bound recovery path therefore necessarily conflicts. |
| RC21-02 target identity and unresolved purge retry chain | FAIL | The target singleton is active and immutable. New-root rejection, however, treats any child authorization as resolution even when that child is never started or terminalized. A fresh root can then be registered, and an expired unused child cannot be replaced because `PriorAttemptId` is unique. No serialization lock protects the new-root check. |
| RC21-03 complete ACL universe | FAIL | All-`nexa` function ownership/default/PUBLIC coverage is broader, but actual privilege scans cross-join every non-superuser role. Supported predefined aggregate roles such as `pg_read_all_data`/`pg_write_all_data` can appear as unexpected privilege holders, while no classification/exclusion exists. Conversely the lifecycle administrator is wholly excluded, so its exact admin capability matrix is never verified. |
| RC21-04 durable rollback proof | FAIL | The context row is no longer mandatory and physical aliases remain correct. The SECURITY DEFINER recorder is ultimately owned by capability-free `nexa_rev869b_security_owner`; it has neither superuser nor `pg_read_all_stats`. PostgreSQL can hide another role's `backend_xid`, allowing `NOT EXISTS(pg_stat_activity...)` to pass while the original transaction is active. |
| RC21-05 behavior-derived evidence | FAIL | The common factory retains optional/default result inputs and derives IDs/hashes from `REV869B-C22|ID|purpose`; the common runner validates a signer and envelope shape, not the described database action. The expected target-instance hash is label-derived although the target singleton is generated at install time. |
| RC21-06 frozen matrix drift/compression | FAIL | P02, L01, L03, L04 and T03 still implement different actions than the frozen cases; L02, R02, C06, C08, G01, G06, E03 and E04 remain compressed into label-only subcase lists. T03 changes only its own serialized action, not every scenario's intended action. |
| RC21-07 unavailable runtime environment | EXTERNAL | Correctly remains external and unclaimed. It cannot be substituted with source-generated PASS data. |

## 5. Five-blocker matrix

| Blocker | Result | Evidence |
|---|:---:|---|
| B21-01 — every command-terminalization SQL column exists in the authoritative physical schema with correct type and ownership | **PASS** | Preserved. Attempts own `TargetBackendPid integer` and `TargetTransactionId bigint`; contexts own their backend/transaction/opened fields. The generated SQL, model and focused alias/type contracts agree. |
| B21-02 — quarantine evidence is complete, durable, instance/operation/attempt-bound and cannot complete with missing evidence | **PASS** | The narrow quarantine statement is closed: registration persists authoritative identity/action/evidence; the audit function derives it, requires the live exact attempt/request, records source version, and exact replay compares version and evidence. Zero/missing attempt or request cannot terminalize. The separate recovery-event collision does not negate this quarantine-only result. |
| B21-03 — purge retries are bound to original authorization, instance, batch, attempt sequence and prior terminal outcome | **FAIL** | A child authorization's mere existence suppresses unresolved-failure detection. It need not start, complete or be the currently consumed retry. This permits a new root after an unused child and strands an expired child. |
| B21-04 — target ACL closure covers owners, schemas, tables, sequences, functions, default privileges, role inheritance and PUBLIC | **FAIL** | The canonical actual-role universe mishandles PostgreSQL predefined aggregate roles and entirely excludes lifecycle administrator exactness. Thus runtime/admin/export and inherited/effective closure cannot be objectively proven. |
| B21-05 — all 34 scenarios contain pinned, scenario-specific, objectively verifiable evidence | **FAIL** | All 34 still depend on the shared generic envelope, label-derived expected facts and common shape assertions. Compound subcases are strings, failpoints are undeclared, target identity is not database-derived, and frozen action drift remains. |

**Totals: 2 PASS, 3 FAIL.** The previously passing B21-01 remains preserved.

## 6. Complete 34-scenario result matrix

Each row was reviewed separately. “Generic envelope” means the row still accepts deterministic label-derived identifiers/hashes and common response fields without an independent fixture/action/query implementation proving its scenario-specific facts. Every row therefore lacks at least one required exact fixture/instance, authority/command/attempt identity, SQLSTATE/object, before/after fact, durable evidence, exact terminal result, safe cleanup, or non-generic action proof.

| ID | Result | Exact independent failure |
|---|:---:|---|
| P01 | FAIL | Generic `ExternalVerified` envelope does not carry query-derived complete manifest/catalogue/ACL delta sets; ACL verifier itself is defective. |
| P02 | FAIL | Still substitutes a generic preflight/division sentinel for individual wrong system/TLS/endpoint/source/manifest lifecycle denials; subcases are labels only. |
| P03 | FAIL | No concrete unexpected role/database/object/grant fixture and exact symmetric database-derived delta; ACL verifier is not complete. |
| L01 | FAIL | Still models ordinary Reserved-to-Ready provisioning, not interruption after reservation and exact resume or approved cleanup. |
| L02 | FAIL | Six create-phase cases are compressed into string keys with no phase-specific fixture, fingerprints, restart evidence or terminal evidence. |
| L03 | FAIL | Still models a concurrent lifecycle attempt from Provisioning, not two normal cleanup requests from Ready/InUse with one DropStarted and one DROP. |
| L04 | FAIL | Still performs straight drop/finalize; before/during/after DROP and role-cleanup interruption boundaries exist only as labels. |
| L05 | FAIL | Quarantine binding improved, but use/drop denial and mismatch facts remain common-envelope assertions rather than independently queried action evidence. |
| R01 | FAIL | Recovery-authorized drop cannot execute because the same `(LeaseId,RequestId)` is inserted twice; generic evidence cannot prove the expected terminal result. |
| R02 | FAIL | Wrong/expired/replayed/foreign/pre-state/action/nonce variants remain compressed; exact per-variant denial and preserved valid decision are absent. |
| R03 | FAIL | Recovery retry/finalization inherits the duplicate request-event collision and lacks a real restart plus old/new linked-decision trace. |
| C01 | FAIL | Generic counts/hashes do not prove atomic business/history/receipt/outcome facts for a concrete command fixture. |
| C02 | FAIL | Default affected-row evidence cannot prove zero duplicate mutation, original receipt identity, or no new attempt after a lost response. |
| C03 | FAIL | SQLSTATE/object and unchanged hash are signer-supplied expected fields, not independent original-versus-changed request readback. |
| C04 | FAIL | Named failpoint has no source fixture/declaration; rollback and durable terminal result are asserted through the generic envelope. |
| C05 | FAIL | `RolledBack` relies on `pg_stat_activity.backend_xid` visibility unavailable to its capability-free definer, so inactive transaction is not authoritative. |
| C06 | FAIL | Four interruption points are compressed and terminal outcome remains the union label `CommittedRolledBackOrAbandoned`, not one exact result per subcase. |
| C07 | FAIL | No two-actor barrier, winner/loser identifiers, unique ordinals or independent one-active-attempt query. |
| C08 | FAIL | Binding substitutions remain compressed and omit independently exercised pool/transaction/version variants and per-field zero-mutation proof. |
| G01 | FAIL | Target singleton helps, but five invalid-authorization variants are label-only and do not independently prove exact denial/zero candidates/zero deletion. |
| G02 | FAIL | Zero rows is supplied by the contract, not proven by an authoritative eligible-set query and deterministic noneligible fixture. |
| G03 | FAIL | Generic before/after values do not identify frozen candidates, exact deletion delta, or separately preserved durable histories. |
| G04 | FAIL | No deterministic drift action/readback proves rollback and candidate preservation; envelope hashes are label-derived. |
| G05 | FAIL | Named delete failpoint has no source fixture/declaration or independently proven separate-principal failure commit. |
| G06 | FAIL | Race/substitution/retry cases are compressed, and source permits fresh-root bypass after an unused linked child authorization. |
| E01 | FAIL | No exact authorized rows/as-of/payload projection query; minimized immutable batch is asserted through generic hashes. |
| E02 | FAIL | No concrete later ledger insertion and independent same-batch reread proving exclusion and immutability. |
| E03 | FAIL | Expired/wrong/terminal/concurrent release variants are compressed without per-release exact denial and unchanged-state evidence. |
| E04 | FAIL | `InterruptedThenReleaseStarted` is a composite label, not two durable records with distinct old/new release IDs and an unchanged batch. |
| A01 | FAIL | Generic Verified response cannot repair the predefined-role/admin gap or expose complete expected/actual matrices with zero deltas. |
| A02 | FAIL | Principal/category variants are labels; administrator exactness is excluded and protected direct denials are not individually exercised. |
| T01 | FAIL | Signed allocation/cleanup is an improvement, but expected target identity is label-derived and no independent database fact proves fixture provenance/absence. |
| T02 | FAIL | No exact deterministic failed scenario, process boundary, surviving control-plane readback and independently proven cleanup outcome. |
| T03 | FAIL | Checks only that removing T03's own action changes serialization. It does not mutation-test each of the 34 intended actions and still replaces the frozen meta-test with concurrent fixtures. |

**Scenario totals: 0 PASS, 34 FAIL.** IDs are unique and discovery count is exactly 34; none is accepted merely for having a unique ID. No row passed on a generic exception, zero-row label, shared record, placeholder hash, missing fixture, compressed subcase list or constant/signer-supplied PASS result.

## 7. Frozen architecture and regression review

| Boundary | Decision | Evidence |
|---|:---:|---|
| External provisioning | RETAIN | No in-scope path provisions PostgreSQL databases/roles or embeds an alternate provisioner. |
| Dedicated lifecycle controller | RETAIN | Signed controller interfaces remain the lifecycle boundary; test/application code is not granted administrator credentials. |
| Surviving control-plane database | RETAIN | Lease, recovery and quarantine durability remain outside disposable targets. The recovery SQL defect is an implementation error within this boundary. |
| Target-local transactional ledgers | RETAIN | Command receipts/outcomes, purge and export ledgers remain target-local. |

The frozen architecture remains unchanged and valid. The FAIL verdict does not authorize moving trust boundaries or inventing an alternate lifecycle path.

Purchase workflow, permissions, approvals, calculations and audit histories show **no direct source/model regression** in this bounded diff: none of their files changed; the build, full non-PostgreSQL suite and model/snapshot parity passed. The broad `nexa` ownership/ACL SQL is cross-cutting, however, so runtime PostgreSQL non-regression is not claimed and must wait for a source PASS plus separate execution authorization.

## 8. Offline validation reproduced

| Validation | Result | Totals/evidence |
|---|:---:|---|
| Build | PASS | 5 projects; 0 warnings; 0 errors. |
| Focused REV869B non-PostgreSQL tests | PASS | 71 passed, 0 failed, 0 skipped. |
| Complete non-PostgreSQL suite | PASS | 445 passed, 0 failed, 0 skipped. |
| Correction 22 PostgreSQL discovery only | PASS | Exactly 34 discovered; 0 executed. |
| PostgreSQL tests executed | PASS | 0. No connection was attempted. |
| PowerShell 5.1 AST | PASS | PowerShell 5.1.19041.6456; 24 scripts; 0 parse errors; 0 helpers executed. |
| EF no-connect migration discovery | PASS | Inert endpoint `127.0.0.1:1`; 13 migrations discovered; no connection. |
| REV869A/REV869B uniqueness/adjacency | PASS | One ID each; REV869B immediately follows REV869A. |
| Model/snapshot parity | PASS | Exact parity test: 1 passed, 0 failed. |
| Offline Up SQL | PASS | 8,040 lines; 1,527,501 UTF-8 bytes; SHA-256 `8492D3D3FA4702C1D7454ADE96FFAFA96E6E00B556DE1046F728DC96BEA3FCB2`. |
| Offline Down SQL | PASS | 4,544 lines; 147,601 UTF-8 bytes; SHA-256 `BC106348BDDAD9E038636C49A494C01A75E8B2064B9293F10677F28049C6DBA0`. |
| Independent SQL hash agreement | PASS | Both hashes exactly match the Correction 22 checkpoint. |
| SQL-column contracts | PASS | Physical type/owner aliases agree; B21-01 preserved. |
| ACL/owner/default/PUBLIC scans | FAIL | Default-privilege clauses and all-function ownership/PUBLIC logic exist, but predefined aggregate roles and lifecycle-administrator exactness invalidate closure. |
| Secret scan of added source | PASS | 0 matches. |
| Privacy scan of added source | PASS | 0 matches. |
| Prohibited-operation scan of added source and generated SQL | PASS | 0 matches for database/role creation or deletion, backend termination, migration apply/drop or PostgreSQL client execution patterns. |
| Failpoint declaration scan | FAIL | 0 source declarations for the C04/G05 named failpoints. |
| `git diff --check` | PASS | Exit 0. |

The SQL hashes establish deterministic offline generation only. They do not cure the semantic defects above or constitute migration execution evidence.

## 9. External prerequisites and nonclaims

The following remain unavailable and block execution-helper readiness:

1. An externally provisioned isolated PostgreSQL cluster/control plane with exact capability-minimized roles, membership, database/schema/object/default/PUBLIC privileges and rotated credentials.
2. Pinned system identifier, endpoint, TLS/SPKI, environment, reviewed source/package/controller manifests and target-instance provenance.
3. External lifecycle-administrator installation of the reviewed package in the surviving control-plane database.
4. A deployed independently reviewed lifecycle controller/reconciler implementing the corrected APIs, signed envelopes, durable restart behavior and no credential exposure.
5. Approved management writers and short-lived single-use recovery/purge/export decisions.
6. Deterministic scenario-specific fixtures, failpoints, barriers, process restarts and database-derived evidence for all 34 frozen cases.
7. Separate future authorization for read-only PostgreSQL preflight and, only after that gate, behavioral execution.

No external prerequisite was simulated, and no PostgreSQL readiness claim is made.

## 10. Exact next gate

Stop at this report. The exact next gate is a **controlled source-only REV869B Correction 22 failure reconciliation** that independently classifies these findings, consolidates shared causes, preserves B21-01 and the narrow B21-02 gain, and defines the smallest possible future correction boundary only if justified. It must not access PostgreSQL, provision infrastructure, execute helpers or lifecycle/data operations, rewrite history, access `../legacy-reference/`, or implement Correction 23.

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`
