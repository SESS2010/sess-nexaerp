# REV869B pre-apply source safety rereview after Correction 20

Date: 2026-08-14 (Asia/Calcutta)

Review type: fresh independent source-only safety rereview

Reviewed commit: `c0f0fccf0cc240dfd8c3cbe2da8954d89ef46950`

Reviewed parent: `3234622e886a5fde3d90fe2cf98f7cedceb6fbef`

Exact diff: `3234622e886a5fde3d90fe2cf98f7cedceb6fbef..c0f0fccf0cc240dfd8c3cbe2da8954d89ef46950`

Checkpoint SHA-256: `BC58518ADFF4E761579B9088C9C18DD6BF64F43D307A663F52FCAEB30FAF199C`

## 1. Decision

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```

Correction 20 retains the four frozen components, but it does not close the six reconciled blockers. The strongest executable defect is in `rev869b_record_noncommit_outcome`: it reads `OpenedAt`, `BackendPid` and `TransactionId` from alias `a` of `rev869b_command_attempts`, although that relation defines `TargetBackendPid` and `TargetTransactionId`; the first three columns exist only on `rev869b_command_contexts`. The function therefore cannot provide the claimed terminalization contract.

The 34 PostgreSQL tests compile and are discoverable, but they remain dependent on one generic, unpinned HTTPS response contract. Returned source, manifest, TLS, cluster and evidence hashes are checked only for shape, not against reviewed expected values or a signature. A substitute HTTPS endpoint can echo the requested labels and return arbitrary format-valid IDs, counts and hashes. This is not fail-closed external provisioning evidence.

PostgreSQL remains unauthorized and was not contacted. Zero PostgreSQL tests were executed.

## 2. Entry gate and exact 15-file diff reconciliation

- HEAD matched `c0f0fccf0cc240dfd8c3cbe2da8954d89ef46950`.
- Parent matched `3234622e886a5fde3d90fe2cf98f7cedceb6fbef`.
- Subject was `Correct REV869B source checkpoint 20`.
- Target-scoped Git status was clean before report creation.
- The checkpoint hash matched the required SHA-256.
- The reviewed diff contains exactly 15 files, 619 insertions and 105 deletions.
- `git diff --check` passed.
- No path under `../legacy-reference/` was read, enumerated, modified, staged or committed.

Reviewed files:

1. `outputs/rev869b_source_correction_checkpoint_20.md`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs`
12. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`
13. `tools/rev869b-control-plane-install.sql`
14. `tools/rev869b-control-plane-preflight.sql`
15. `tools/rev869b-control-plane-verify.sql`

Migration discovery found 13 migrations. REV869A occurs exactly once at ordinal 12; REV869B occurs exactly once at ordinal 13; they are adjacent.

## 3. Six-blocker result matrix

| Reconciled blocker | Result | Independent evidence |
|---|---|---|
| R19-N01: control-plane verification is not canonical or complete | **FAIL** | The new catalogue hash is captured from the same installed catalogue and later compared with that captured baseline (`rev869b-control-plane-install.sql:174-184,197`; verifier line 41). It is not a committed canonical definition inventory. A changed definition present at installation becomes the accepted baseline. The fingerprint also hashes selected attributes rather than a complete canonical definition. The added mutation test exercises the disconnected pure helper `IsExactSetMatch`, not the SQL verifier, so it does not prove that a column/default/constraint/index/trigger/function/owner/ACL mutation is rejected. |
| R19-N02: quarantine, recovery action and lifecycle replay are incomplete | **FAIL** | Recovery action gating and cleanup/finalization replay improved. However, `rev869b_record_quarantine` validates `observed_target_state` and `failure_category` and then discards both; the event stores only the caller-supplied evidence hash (`install.sql:115-123`). Replay compares only destination state and that hash, so different observed state/category values with the same hash are accepted. No quarantine outcome preserves those fields. Recovery consumption also permits `ON CONFLICT(AttemptId) DO UPDATE` to replace an existing nonterminal attempt's kind/decision (`:145-148`) instead of requiring an exact new/stable decision-attempt binding. |
| R19-N03: command terminal ownership and replay binding are not exact | **FAIL** | `rev869b_record_noncommit_outcome` references `a."OpenedAt"`, `a."BackendPid"` and `a."TransactionId"` from `rev869b_command_attempts` (`Rev869BCommandContextSql.cs:113`). That table defines `TargetBackendPid` and `TargetTransactionId` at line 23; `OpenedAt`, `BackendPid` and `TransactionId` belong to `rev869b_command_contexts` at lines 26-27. The added source test checks that those strings occur and therefore passes the defect. Offline EF generation does not parse PostgreSQL function bodies. |
| R19-N04: purge authorization scope, failure authority and retry linkage are unenforced | **FAIL** | Organization scope is now applied and failure recording is separated to `nexa_rev869b_purge_audit`. The retry rule remains bypassable: `PriorAttemptId` is nullable, and the Failed/Interrupted/policy-link check runs only when the caller supplies it (`Rev869BCommandContextSql.cs:130`). After a failed attempt, a new authorization with the same policy and `prior_attempt=NULL` is accepted and starts normally. The database therefore cannot enforce “retry requires a new linked authorization.” G06 also expects SQLSTATE 40001, while the actual retry-binding function raises 42501. |
| R19-N05: export minimization/replay and target ACL closure fail | **FAIL** | Approved-field projection, expiry checks and one-active-release sequencing improved. Target ACL closure remains incomplete. Catalogue and effective-function checks are restricted to names matching `rev869b_%`, and direct relation/sequence checks are restricted to relations matching `rev869b_%` (`Rev869BCommandContextSql.cs:121-126,168,171-172`). An arbitrary ordinary role can therefore receive a direct grant on a non-REV869B target business relation or function without detection. The verifier also does not recheck target role capability flags or compare a canonical expected default-ACL/definition set. |
| R19-N06: PostgreSQL acceptance matrix is label-only/non-probative | **FAIL** | Exactly 34 facts exist, but all are one-line delegates to the same `RunAsync` method (`Rev869BCorrection17PostgresScenarios.cs:9-47`). That method POSTs setup/action/expected values to a generic endpoint and verifies echoed values. The client accepts any HTTPS URL; it does not pin controller identity, expected source commit, manifest, TLS SPKI or cluster ID, and it verifies digests only as 64 hexadecimal characters (`Rev869BLifecycleControllerClient.cs:15-22,36-105`). There is no signed evidence field. Several contracts also contradict the frozen scenario or actual SQLSTATE/object, detailed below. |

All five source-implementation defects remain blocking. The missing/non-probative evidence blocker also remains blocking. No finding requires changing the frozen architecture; the defects are implementation and evidence failures inside the retained design.

## 4. All 34 objective scenario results

Common evidence defect **C** applies to every row: the fact delegates to the same generic remote endpoint; setup/action are request strings rather than locally implemented scenario operations; response identities and hashes are neither pinned to authoritative expected values nor cryptographically signed; and no separately reviewed controller implementation exists in this source set. Consequently none of the 34 bodies can presently be counted as genuine behavioral evidence.

| ID | Result | Individual reconciliation |
|---|---|---|
| P01 | **FAIL** | C; the canonical verifier is self-baselined and incomplete, so the requested “exact definitions/effective ACLs” result is not implemented. |
| P02 | **FAIL** | C; the contract expects 42501/`rev869b_external_manifest`, but preflight has no such database object or explicit 42501 failure contract. |
| P03 | **FAIL** | C; the contract expects 42501/`rev869b_control_plane_catalogue_acl`, which exists only as a test label, while the verifier fails through its generic CASE expression. |
| L01 | **FAIL** | C; the frozen case requires interruption after reservation and deterministic resume-or-approved-cleanup evidence. The contract merely provisions Reserved to Ready. |
| L02 | **FAIL** | C; every create phase is compressed into one generic call and only Provisioning to Ready is accepted, without per-phase state/evidence or the frozen Quarantined alternative. |
| L03 | **FAIL** | C; the frozen case is concurrent normal cleanup from Ready/InUse with one DropStarted/one DROP. The contract instead starts a generic concurrent lifecycle attempt from Provisioning. |
| L04 | **FAIL** | C; it omits the required interruption points before/during/after DROP, role cleanup, acknowledgement-loss replay and one-Finalized proof. |
| L05 | **FAIL** | C; Quarantined is reachable, but observed target state and failure category are not durably stored or exact-replay-bound. |
| R01 | **FAIL** | C; a nonempty returned decision ID is not proven to be linked to the returned lease, attempt, exact action or durable control-plane row. |
| R02 | **FAIL** | C; it tests only an already-consumed replay label, not wrong/expired/foreign target/prestate/action/nonce variants or preservation of a valid unused decision. |
| R03 | **FAIL** | C; it does not prove the first failure is durable, the first decision is unusable, and a distinct new decision is linked to the retry. |
| C01 | **FAIL** | C; no source body executes the exact business/history/receipt transaction or independently checks its committed rows. |
| C02 | **FAIL** | C; no body performs the same-key replay or independently proves no new business row/attempt. |
| C03 | **FAIL** | C; denial is an asserted remote response rather than a database action with independently observed no-mutation evidence. |
| C04 | **FAIL** | C; no executable receipt-insertion failpoint named by this contract exists in the reviewed source set. |
| C05 | **FAIL** | C; the terminalization function used to establish RolledBack contains invalid attempt-table column references. |
| C06 | **FAIL** | C; one contract forces final state Abandoned for interruptions including “after response,” contradicting the frozen requirement that an existing receipt determines Committed. |
| C07 | **FAIL** | C; the contract expects object `UX_rev869b_one_active_command_attempt`, but the function's explicit loser path raises constraint `rev869b_command_attempt_active`. |
| C08 | **FAIL** | C; the open-path guard exists, but terminal substitution reaches the invalid terminalization SQL and the single response does not distinguish every substituted dimension. |
| G01 | **FAIL** | C; missing/expired start raises generic “Purge authorization unavailable,” not the asserted 42501/`rev869b_purge_authorization_scope`; that object exists only as a label. |
| G02 | **FAIL** | C; zero rows is specially allowed, but the client checks `beforeRowCount >= 0`, not the frozen exact pre-count zero and exact eligible-set proof. |
| G03 | **FAIL** | C; no body independently queries the frozen candidate digest, exact deletion set, retained durable histories and atomic Succeeded event. |
| G04 | **FAIL** | C; the contract does not independently establish that the deletion transaction rolled back and candidates remained after drift. |
| G05 | **FAIL** | C; `rev869b_purge_delete_failpoint` occurs only in the contract label and has no executable failpoint implementation in the reviewed source. |
| G06 | **FAIL** | C; nullable `PriorAttemptId` bypasses linkage, and the contract expects 40001 while the actual retry-binding function raises 42501. |
| E01 | **FAIL** | C; field minimization exists, but the generic response does not independently verify exact row/field/as-of payload and digest. |
| E02 | **FAIL** | C; no body inserts the later row and independently compares the original prepared row set/digest. |
| E03 | **FAIL** | C; the combined “read or authorize” action does not prove each expired, foreign, terminal, replayed and concurrently active denial; a denied read can also return zero rows rather than the asserted exception. |
| E04 | **FAIL** | C; the action says it records Interrupted and authorizes a new release ID, but the expected final state remains Interrupted and does not prove the new release reached ReleaseStarted. |
| A01 | **FAIL** | C; target enumeration excludes non-REV869B relations/functions and role capability drift, so “every ordinary effective privilege” is false. |
| A02 | **FAIL** | C; a grant to an arbitrary principal on a non-REV869B target business object is outside the verifier filters and is not denied by this source contract. |
| T01 | **FAIL** | C; this fact uses the generic acceptance endpoint, not `AllocateAsync`, so its path does not exercise `RequireAllocation` or target connection validation. |
| T02 | **FAIL** | C; restart/cleanup is represented by returned booleans/hashes from the same unpinned endpoint, without an independently observed surviving control-plane row. |
| T03 | **FAIL** | C; the evidence model contains only one lease ID and one fixture ID, so it cannot prove two independent fixtures, barrier participants, isolation and cleanup for both. |

The 34 names and contracts are discoverable. That count is not behavioral evidence. Generic exception-only, zero-row, missing-fixture, label-only and structural-only claims are rejected.

## 5. Frozen-architecture compliance

| Frozen boundary | Result | Assessment |
|---|---|---|
| External provisioning | **Structurally retained; fail-closed behavior FAIL** | Preflight remains read-only and no cluster provisioning was added. Test/controller trust can nevertheless be redirected to any HTTPS endpoint, and returned cluster/source/manifest/TLS values are only shape-checked. |
| Dedicated lifecycle controller | **Retained but unproven** | Test code calls an external controller and does not receive lifecycle-admin credentials. The controller identity, implementation and evidence signature are absent/unpinned. |
| Surviving control-plane database | **Retained but implementation FAIL** | Lease/action/outcome tables remain separate from targets. Quarantine evidence is incomplete and recovery attempt rebinding remains possible. |
| Target-local transactional ledgers | **Retained but implementation FAIL** | Command/purge/export ledgers remain target-local; command noncommit SQL is invalid, purge retry linkage is optional, and target ACL verification is incomplete. |

No competing architecture or trust-boundary merge was introduced. The architecture should be retained; these failures require source correction, not an architecture amendment.

## 6. Install, preflight and verifier review

- **Preflight:** read-only and exact-role symmetric differences are improvements. It still relies on externally supplied manifest/source values and has no database object matching P02's asserted denial object.
- **Control-plane install:** purpose functions and role separation remain. Catalogue equivalence is self-baselined, quarantine details are discarded, and recovery attempt conflict handling can rebind an existing nonterminal attempt.
- **Control-plane verify:** checks names/owners, selected effective execution, direct relation/sequence access, database/schema/PUBLIC/membership facts and baseline hash. It does not compare installed definitions to a committed canonical definition set.
- **Target install:** field-minimized export, expiry, release sequencing, organization-scoped purge and independent purge audit are present. Command terminal SQL references nonexistent columns.
- **Target verifier:** function/relation/sequence checks are restricted to `rev869b_%`; complete target business-object privilege closure and role capability drift are not covered.

## 7. Reproduced safe offline validation

| Validation | Independent result |
|---|---|
| Solution build | **PASS**; 5 projects; 0 warnings; 0 errors |
| Focused REV869B tests excluding `Postgres`/`PostgreSql` | **PASS**; 66 passed, 0 failed, 0 skipped |
| Complete suite excluding `Postgres`/`PostgreSql` | **PASS**; 440 passed, 0 failed, 0 skipped |
| PostgreSQL compilation/discovery only | 88 PostgreSQL/PostgreSql-named overall; 60 REV869B-named; exactly 34 Correction 20 scenarios; **0 executed** |
| PowerShell 5.1 AST | **PASS**; version 5.1.19041.6456; 24 files; 0 parse errors; no helper executed |
| EF migration discovery | **PASS** with `--no-connect` and inert `127.0.0.1:1`; 13 migrations; applied state unknown |
| Migration order/uniqueness | **PASS**; REV869A ordinal 12 and REV869B ordinal 13; each once and adjacent |
| Explicit model/snapshot parity | **PASS**; 1 passed, 0 failed, 0 skipped |
| Offline Up SQL, REV869A to REV869B, `--no-transactions` | 255,932 bytes; 2,264 lines; SHA-256 `BF59E6B096315342498C483F669A7C92DA61F73B29C73C79902F2EC1B7347799` |
| Offline Down SQL, REV869B to REV869A, `--no-transactions` | 10,225 bytes; 214 lines; SHA-256 `DF43E23C0AFC77E5AB91AEB06462109394756EB2A7E2840208F438AD5338B297` |
| Generated SQL prohibited operations | 0 CREATE DATABASE; 0 DROP DATABASE; 0 `pg_terminate_backend` |
| Temporary SQL artifacts | removed; 0 remain |
| Exact 14 executable source/test/tool files: secret scan | 0 credential-literal assignments |
| Privacy scan | 0 DOB/payroll/bank/government-ID/private-employee literals |
| Prohibited-token scan | 2 non-operative hits: one unchanged OIDC authentication error message and defensive assertions that rollback contains no DROP DATABASE/ROLE; 0 added executable prohibited operations |
| `git diff --check` | **PASS** |

Build, EF generation and C# test compilation do not parse or execute PostgreSQL function bodies. Their PASS results therefore do not contradict the invalid-column finding.

## 8. Purchase and REV869A preservation

**PASS for unchanged scope.** The 15-file diff contains no Purchase endpoint/service/domain/model, migration identity/designer/snapshot or REV869A file. Model/snapshot parity passes. The focused 66-test and complete 440-test non-PostgreSQL suites pass. No source evidence reopens the previously approved Purchase workflows, approval thresholds, GST calculations/provenance, histories, segregation or permissions.

## 9. External prerequisites and next gate

External prerequisites remain unmet and blocking:

1. Exact externally provisioned NOINHERIT/capability-minimized roles, surviving control-plane database, closed CONNECT/default privileges and rotated credentials.
2. Pinned isolated PostgreSQL system identifier, endpoint, TLS/SPKI, environment and exact source/package manifest.
3. Externally reviewed control-plane package installation.
4. Deployed dedicated lifecycle controller/reconciler and management approval writer.
5. A controller evidence protocol pinned to the reviewed source/manifest/cluster/TLS identity and authenticated or signed so an arbitrary HTTPS responder cannot fabricate acceptance.
6. Genuine isolated fixtures, deterministic failpoints, restart/barrier controls and scenario-specific evidence for all 34 cases.
7. A bounded source correction for the findings in this report followed by another fresh independent source-only review.
8. Separate explicit authorization before any PostgreSQL verification or behavior execution.

Exact next gate: perform a source-only failure-reconciliation of this report and authorize a bounded correction scope. PostgreSQL, provisioning, helpers, migrations, lifecycle, recovery, quarantine, purge and export remain blocked. This review did not begin or implement Correction 21.
