# REV869B pre-apply source safety rereview after Correction 21

Date: 2026-08-14 (Asia/Calcutta)

Review type: fresh independent source-only safety rereview

Reviewed commit: `b24ba9a7d813f3e2c32ac8fe69275423cbc12cc1`

Reviewed parent: `7a1e4739b733acb4a90594fa4112cad52aa0f71c`

Reviewed subject: `Correct REV869B control-plane safety checkpoint 21`

Exact diff: `7a1e4739b733acb4a90594fa4112cad52aa0f71c..b24ba9a7d813f3e2c32ac8fe69275423cbc12cc1`

Correction 21 checkpoint SHA-256: `64DE11A6552AA0E814150A77A235577D0113A6F5221E014A3E4F3F51F18CE50E`

## 1. Decision

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```

Correction 21 fixes the Correction 20 invalid command-attempt column references and preserves the selected component boundaries, but four of the five required blocker gates remain open.

The quarantine row contains the requested fields, but `ExecutionInstanceId`, actor and operation are caller claims that are not joined to an authoritative attempt/controller record. Purge authorizations store a target-instance SHA-256, but start/execute never compare it with the current target; consequently the claimed instance binding is passive. The target ACL verifier does not verify owners for the business allowlist, default ACLs for every owner, or non-REV869B function/PUBLIC EXECUTE closure. Most decisively, the 34 scenario facts are not scenario-specific executable evidence: 32 call one shared remote runner, most contracts inherit generic counts `1 -> 1` and affected rows `1`, the signed payload has no exact target database, and T01/T03 bypass the signed acceptance envelope. Several bodies also differ from the frozen matrix, including L01, L03, L04 and T03.

PostgreSQL remained unauthorized and was not contacted. Zero PostgreSQL tests were executed.

## 2. Entry gate and exact 11-file diff reconciliation

- HEAD, parent and subject matched exactly.
- Target-scoped Git status was clean before report creation.
- The only repository-level status entries were two pre-existing untracked files under `../legacy-reference/`; that directory and its contents were not read, enumerated, modified, staged or committed.
- The checkpoint hash matched the required SHA-256 exactly.
- The reviewed diff contains exactly 11 files, 549 insertions and 92 deletions.
- `git diff --check` passed.
- EF no-connect discovery found 13 unique migrations. REV869A occurs exactly once at ordinal 12 and REV869B occurs exactly once at ordinal 13, immediately after REV869A.

Exact reviewed inventory:

| # | File | Status | Insertions | Deletions | Reconciliation |
|---:|---|:---:|---:|---:|---|
| 1 | `outputs/rev869b_source_correction_checkpoint_21.md` | A | 139 | 0 | Correction checkpoint only; its safety/readiness claims were independently reevaluated. |
| 2 | `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | M | 70 | 13 | Corrects terminal aliases; adds purge chain fields/constraints and broader ACL scans. Target-instance and complete ACL bindings remain incomplete. |
| 3 | `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs` | M | 1 | 1 | Updates the quarantine function signature contract. |
| 4 | `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | M | 40 | 11 | Adds typed identity/count/outcome defaults for 34 contracts; the defaults are mostly generic rather than scenario-specific. |
| 5 | `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs` | M | 7 | 0 | Adds source assertions for quarantine and ACL text. |
| 6 | `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | M | 36 | 2 | Adds common evidence assertions and direct T01/T03 allocation paths; 32 facts still share one runner. |
| 7 | `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | M | 85 | 4 | Adds column, purge, ACL and inventory scans. These pass but do not establish PostgreSQL behavior. |
| 8 | `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | M | 102 | 32 | Adds origin/TLS/source/manifest/cluster/signing-key pins and ECDSA evidence verification; the common payload remains under-specified for the required matrix. |
| 9 | `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs` | M | 10 | 5 | Threads new authorization fields to SQL; target-instance digest is still caller-provided and not derived/verified against the opened target. |
| 10 | `tools/rev869b-control-plane-install.sql` | M | 50 | 20 | Adds durable quarantine outcomes and stronger revoke/default ACL setup. Instance/actor/operation authenticity remains unbound. |
| 11 | `tools/rev869b-control-plane-verify.sql` | M | 9 | 4 | Adds quarantine inventory, role-capability, PUBLIC and default checks for the control plane. |

No migration identity, designer, snapshot, Purchase domain/service/endpoint/model, provisioning helper, PowerShell helper, production, frontend, AWS, OIDC, REV869A or REV869C file changed.

## 3. Five-blocker PASS/FAIL matrix

| Correction 20 blocker | Result | Independent source evidence |
|---|:---:|---|
| 1. Every command-terminalization SQL column exists in the authoritative physical schema with the correct type and ownership | **PASS** | `rev869b_command_attempts` owns `TargetBackendPid integer` and `TargetTransactionId bigint`; `rev869b_command_contexts` owns `BackendPid integer`, `TransactionId bigint` and `OpenedAt`. The terminal function uses `a.TargetBackendPid`/`a.TargetTransactionId` and `c.BackendPid`/`c.TransactionId`; no invalid `a.OpenedAt`, `a.BackendPid` or `a.TransactionId` remains. The table-derived alias scan passes. This narrow PASS does not make C05 executable: after an explicit rollback, the transaction-local context row is absent, while the `RolledBack` branch requires that row to exist. |
| 2. Quarantine evidence is complete, durable, instance/operation/attempt-bound and cannot complete with missing evidence | **FAIL** | `rev869b_quarantine_outcomes` is append-only and requires nonempty fields, exact lease/request/attempt IDs, target/cluster, observed/source state, kind/reason, actor/issuer/operation, terminal outcome and SHA-256. Attempt identity is checked against `ActiveAttemptId`. However, lifecycle attempts have no authoritative execution-instance, actor, issuer or operation columns, so the function merely accepts and persists caller-supplied values. A nonempty arbitrary `ExecutionInstanceId`, actor or operation passes. The evidence is durable and syntactically complete but is not authoritatively instance/operation bound. |
| 3. Purge retries are bound to the original authorization, instance, batch, attempt sequence and prior terminal outcome | **FAIL** | Root authorization, unique batch, prior attempt FK/unique index, retry ordinal, prior terminal outcome/evidence, scope/cutoff/maximum and one-way prior-chain checks are present. Start binds `AuthorizedBatchId` to the attempt ID. But `TargetInstanceSha256` is only stored/compared to prior caller-supplied values; it is never compared with an authoritative current target identity at registration, start or execute. `Rev869BPurgeCoordinator.RegisterAsync` accepts the digest from its caller. Thus a chain can be internally consistent while bound to an invented or foreign instance digest. |
| 4. Target ACL closure covers owners, schemas, tables, sequences, functions, default privileges, role inheritance and `PUBLIC` | **FAIL** | Relation privilege comparison now spans all `nexa` relations and seven table privileges, sequences are denied, role capability/membership and schema/database ACLs are checked, and install revokes PUBLIC/package-role access before exact grants. However, owner checks cover only `rev869b_%` relations/functions, not the complete business-relation allowlist; default ACL checks cover only `nexa_rev869b_security_owner`, not every owner in the target schema; and function effective-grant comparison filters functions to `proname LIKE 'rev869b_%'`. PUBLIC/nonpackage EXECUTE on a non-REV869B target function is therefore outside the verifier. The required owner/function/default/PUBLIC closure is not complete. |
| 5. All 34 scenarios contain pinned, scenario-specific and objectively verifiable evidence | **FAIL** | IDs are unique and discoverable, origin/TLS/source/manifest/cluster/key pins exist, and the common response is ECDSA-verified. But 32 facts delegate to the same `RunAsync`; most contracts use helper defaults `before=1`, `after=1`, `affected=1`, terminal=`final`, cleanup=`Finalized`. The signed evidence contains no exact target database name and only format-checks most returned hashes. IDs are required nonempty but are not pinned or checked unique across scenarios. No source-backed fixture implementation exists for the claimed failpoints. T01/T03 use unsigned allocation/release responses instead of the signed scenario envelope. This does not meet the one-invariant-per-fact or exact-fixture evidence gate. |

## 4. All 34 scenario results

Common defects applying to P01-A02 and T02: the body delegates to a shared remote method; target database identity is absent from the signed payload; IDs are nonempty rather than pinned/cross-scenario unique; before/after/action hashes are format-valid assertions from the same external signer rather than independently anchored measurements; and the external fixture/controller implementation is an unmet prerequisite. T01/T03 instead use unsigned allocation/release payloads and omit the common authorization/command/attempt/before-after/terminal contract.

| ID | Result | Scenario-specific reconciliation |
|---|:---:|---|
| P01 | **FAIL** | One object identity plus generic counts/hashes does not enumerate exact system/TLS/environment/manifest, definitions and every effective ACL allowlist. |
| P02 | **FAIL** | The frozen action is a lifecycle request denied before mutation; the contract substitutes external preflight and does not prove the required minimized rejection event or every wrong pin independently. |
| P03 | **FAIL** | One generic changed definition/grant response does not identify and prove every exact mismatch without repair/widening. |
| L01 | **FAIL** | The frozen case interrupts after reservation before any role and then resumes or requires approved cleanup. The body only requests `Reserved -> Ready` provisioning. |
| L02 | **FAIL** | “Every create phase” is compressed into one signed result, with no phase-specific fixture, attempt, state/evidence or allowed Quarantined branch. |
| L03 | **FAIL** | The frozen case is two concurrent normal cleanup requests from Ready/InUse with one DropStarted and one DROP. The contract instead starts a concurrent lifecycle attempt from Provisioning. |
| L04 | **FAIL** | It omits the required interruption points before/during/after DROP, after role cleanup and before response, and does not prove same-attempt reconciliation/one Finalized event. |
| L05 | **FAIL** | The claimed `rev869b_target_identity_mismatch` exists only in the scenario contract. Quarantine fields are not authoritatively execution-instance/actor/operation bound. |
| R01 | **FAIL** | Returned decision/attempt IDs and Finalized state do not prove one-time exact action consumption and the full Finalized-or-CleanupFailed outcome path against an exact target. |
| R02 | **FAIL** | It covers an already-consumed replay label only, omitting wrong/expired/foreign target/pre-state/nonce variants and preservation of a valid unused decision. `rev869b_recovery_decision_replay` is not an executable database object. |
| R03 | **FAIL** | It does not independently prove the first failure/interrupt is durable, the first decision is non-reusable, and the retry uses a distinct linked decision and fresh attempt. |
| C01 | **FAIL** | No fact-local fixture/action independently proves business row, history, receipt and outcome committed atomically with exact counts/fingerprints. |
| C02 | **FAIL** | No fact-local replay proves the original receipt is returned with zero new business rows and zero new active attempt. Generic defaults remain `1 -> 1`, affected `1`. |
| C03 | **FAIL** | The signed denial is not paired with an independently queried unchanged request/business fingerprint and exact target instance. |
| C04 | **FAIL** | `TR_rev869b_command_receipt_failpoint` occurs only in test contract text; no reviewed executable fixture creates it. Transaction rollback and later terminalization are not independently observed. |
| C05 | **FAIL** | The terminal SQL's `RolledBack` predicate requires a surviving command-context row, but that row is opened in the explicitly rolled-back target transaction and cannot survive the rollback. The exact frozen RolledBack path is therefore not source-realizable. |
| C06 | **FAIL** | Four interruption positions are compressed into one contract and one composite terminal string. It provides no per-subcase fixture/evidence and no direct proof that a durable receipt always wins over Abandoned. |
| C07 | **FAIL** | A single signed result does not prove two barrier participants, one winner, loser observation, unique database-generated ordinals and no cross-mutation. |
| C08 | **FAIL** | The setup omits pool, transaction and version substitutions required by the frozen matrix, and one combined response cannot prove each substitution reached the intended function/constraint with no mutation. |
| G01 | **FAIL** | Wrong target digest is never compared to the target. A wrong-organization scope is accepted and can yield ZeroRows rather than the asserted `42501`. The combined missing/expired/target/batch/org contract is not one isolated invariant. |
| G02 | **FAIL** | The zero-row values are explicit, but no exact target fixture/eligible-set fingerprint independently proves pre-count zero and no Succeeded label. |
| G03 | **FAIL** | Generic defaults are before `1`, after `1`, affected `1`; they do not express deletion count change, exact frozen IDs/digest, or preservation counts/hashes for durable histories. |
| G04 | **FAIL** | Generic unchanged counts/hashes from the signer do not independently prove the deletion transaction rolled back and candidates remained after the exact drift fault. |
| G05 | **FAIL** | `TR_rev869b_purge_delete_failpoint` occurs only in test contract text; no reviewed fixture creates it. Separate durable failure persistence is not source-demonstrated. |
| G06 | **FAIL** | The final contract remains `Started -> Failed` even though its action also claims one accepted retry; it does not expose the new authorization/batch/ordinal/attempt as exact evidence. Target-instance binding is passive. |
| E01 | **FAIL** | Generic counts and one object identity do not prove exact approved fields, rows, as-of snapshot, expiry, minimized payload and matching batch digest. |
| E02 | **FAIL** | No fact-local source action inserts the later ledger row or independently compares the prepared row set/count/digest before and after. |
| E03 | **FAIL** | Expired, wrong, terminal and concurrently active cases plus read/authorize operations are compressed into one denial; exact release sequencing is not isolated per case. |
| E04 | **FAIL** | The composite terminal label `InterruptedThenReleaseStarted` and generic IDs do not prove a durable old release outcome plus a distinct new release ID against an unchanged immutable batch. |
| A01 | **FAIL** | The underlying target verifier omits business-object ownership, non-REV869B function/PUBLIC EXECUTE and all-owner default ACL closure; one generic signed result cannot supply the missing exact matrices. |
| A02 | **FAIL** | One denial does not attempt each direct privilege/ungranted function for every principal/object category, and grants on non-REV869B functions are outside target verification. `rev869b_protected_object_acl` is only a label. |
| T01 | **FAIL** | It genuinely calls allocate/release and rejects lifecycle-admin text in returned connection strings, but allocation/release evidence is unsigned and the body lacks exact authorization/command/attempt IDs, before/after fingerprints and terminal evidence required for every scenario. |
| T02 | **FAIL** | The shared remote response does not source-demonstrate dispose/restart behavior, surviving control-plane evidence, or no orphan target/role after a deterministic failure. |
| T03 | **FAIL** | The frozen scenario is a scenario-name/body mutation test proving removal of the intended action makes the test fail. Correction 21 replaces it with concurrent allocations. It therefore violates the authoritative matrix even though the two allocation responses are checked for distinct lease/database/fixture values. |

Scenario totals: **0 PASS, 34 FAIL**. Exactly 34 are discoverable and 0 were executed. Discovery/compilation is not behavioral acceptance.

## 5. Frozen-architecture compliance

| Frozen boundary | Result | Assessment |
|---|:---:|---|
| External IaC provisioning | **PASS structurally / unproven externally** | No source helper gained cluster role/database creation authority; provisioning remains external. |
| Dedicated lifecycle controller | **PASS structurally / readiness FAIL** | Tests call an external HTTPS controller and do not receive lifecycle-admin connection strings. The controller/fixture implementation is not present or deployed in reviewed evidence. |
| Surviving control-plane database | **FAIL implementation completeness** | Durable quarantine storage exists, but execution-instance/actor/operation authenticity is not tied to an authoritative attempt/controller fact. |
| Target-local command/purge/export ledgers | **FAIL implementation completeness** | Ledgers remain target-local, but C05 rollback terminalization is unrealizable, purge target-instance binding is passive, and ACL closure is incomplete. |
| Frozen 34-case matrix | **FAIL** | L01, L03, L04 and T03 materially differ from the authoritative actions; other cases compress multiple required subcases into a generic shared response. |

The four-component Option A boundary is retained and no competing architecture is introduced. Overall frozen compliance nevertheless fails because required responsibilities and the authoritative acceptance matrix are not implemented exactly. No architecture amendment is justified by these findings; they are bounded implementation/evidence defects.

Purchase and REV869A preservation remain **PASS for unchanged scope**: no Purchase endpoint/service/domain/model, migration identity/designer/snapshot or REV869A file changed, model/snapshot parity passes, and all 442 non-PostgreSQL tests pass.

## 6. Reproduced safe offline validation

| Validation | Independent result |
|---|---|
| Solution build, no restore | **PASS**; 5 projects; 0 warnings; 0 errors. |
| Focused REV869B suite excluding `Postgres`/`PostgreSql` | **PASS**; 68 passed, 0 failed, 0 skipped. |
| Complete suite excluding `Postgres`/`PostgreSql` | **PASS**; 442 passed, 0 failed, 0 skipped. |
| SQL-column and ACL source-contract scans | **PASS as source scans** inside the focused 68 tests. Their limitations are recorded in sections 3-4. |
| PostgreSQL compilation/discovery only | 87 PostgreSQL/PostgreSql-named tests overall; 59 REV869B-named; exactly 34 Correction 21 matrix tests; **0 executed**. |
| PowerShell 5.1 AST | **PASS**; version 5.1.19041.6456; 24 tracked scripts; 0 parse errors; no helper executed. |
| EF migration discovery | **PASS** with `--no-connect`, matching expected-database guard and inert `127.0.0.1:1`; 13 migrations; applied state intentionally unknown. The first invocation failed closed before discovery because the required expected-database guard was omitted; the corrected no-connect invocation passed and made no connection. |
| Migration order/uniqueness | **PASS**; REV869A count 1 ordinal 12; REV869B count 1 ordinal 13; adjacent. |
| Explicit model/snapshot parity | **PASS**; 1 passed, 0 failed, 0 skipped; inert localhost connection string, no connection opened. |
| Offline REV869A -> REV869B Up SQL, `--no-transactions` | 265,204 bytes; 2,321 lines; SHA-256 `32EF1BFD9E0C84C79F6516E562E57204EEEB152D830F8B498FEE4B9AEDA2EC26`. |
| Offline REV869B -> REV869A Down SQL, `--no-transactions` | 10,257 bytes; 214 lines; SHA-256 `2BFE91FA3F9F3DE54F3D8FED15E020EA40AF7E4C78A2BF67FD64085563144940`. |
| Generated SQL prohibited operations | 0 `CREATE DATABASE`; 0 `DROP DATABASE`; 0 `pg_terminate_backend`. |
| Temporary SQL artifacts | Removed; 0 remain. |
| Exact 10 changed executable source/test/tool files: secret scan | 0 embedded password/client-secret/private-key/access-token/bearer literal assignments. |
| Privacy scan | 0 DOB/payroll/bank/government-ID literals. |
| Prohibited-scope scan | 0 database/role create/drop, backend termination, AWS or OIDC additions. |
| Terminal dynamic/broad handling scan | 0 executable findings; 2 textual matches are negative `Assert.DoesNotContain("EXECUTE format", ...)` assertions. |
| `git diff --check` on exact Correction 21 diff | **PASS**. |

Offline generation, compilation, discovery and source-string/parsed-contract tests do not parse or execute PostgreSQL function bodies and do not prove runtime persistence, rollback, restart, replay, concurrency, failpoint, denial or cleanup behavior.

## 7. External prerequisite status

The following remain external, unverified and blocking for execution-helper/control-plane readiness:

1. Externally provisioned exact NOINHERIT, capability-minimized cluster/control-plane/target roles and databases, closed membership/database/schema/object/default/PUBLIC privileges, and rotated credentials.
2. Pinned isolated PostgreSQL system identifier, endpoint, TLS/SPKI and exact source/package manifest.
3. External lifecycle-administrator installation of the reviewed control-plane package in the surviving control-plane database.
4. Deployment and independent review of the dedicated lifecycle controller/reconciler and management approval writer outside application/test processes.
5. Controller implementation of exact scenario-specific fixtures, authoritative target-instance bindings, deterministic failpoints/barriers/restarts, signed evidence and durable cleanup for the frozen 34-case matrix.
6. Separate authorization for read-only PostgreSQL preflight/verifier work and, only after a future source PASS, separate authorization for PostgreSQL behavior execution.

No external prerequisite was claimed, provisioned or tested in this review.

## 8. Exact next gate and stop condition

Exact next gate: management reviews this fail-closed report and separately decides whether to authorize a bounded source correction limited to the four failed blocker rows and the frozen 34-case evidence matrix. Any future correction must then receive a fresh independent source-only review of its exact commit and parent before PostgreSQL is considered.

This report does not authorize or begin Correction 22. PostgreSQL access/tests, provisioning/helper execution, migration apply/remove, lifecycle, purge, recovery, quarantine, export, production, frontend, AWS, OIDC and REV869C remain NO-GO.

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```
