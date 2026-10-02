# REV869B Source Correction Checkpoint 23

## 1. Scope and disposition

Correction 23 implements only the five source-correctable groups authorized by management from `outputs/rev869b_correction_22_failure_reconciliation.md`. It does not claim independent source-safety approval. PostgreSQL and every external execution path remain prohibited and were not used.

Starting HEAD: `9c9cbaa9548ba51a9f019a0005ddef62ee54518f`

Starting parent: `5c00e55cbc7248e7155d23247c13e25347a75e9a`

Correction 22 remains in ancestry at `5a114cb0dcb4a304916343c1e23f4bf75299132c`.

The updated entry gate passed: the starting commit and parent matched, both intervening commits were report-only as authorized, the reconciliation and blocker-report SHA-256 values matched, the target-scoped worktree was clean, and REV869A/REV869B were unique and adjacent. No history was rewritten. `../legacy-reference/` was not read, modified, staged or committed.

## 2. Exact bounded implementation files

| File | Bounded purpose |
|---|---|
| `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | Purge-root serialization/consumption, exact rollback fence, target ACL taxonomy and owner closure. |
| `tools/rev869b-control-plane-install.sql` | Distinct recovery registration and transition request identities. |
| `tools/rev869b-control-plane-verify.sql` | Updated function signature and exact ACL taxonomy including administrator and predefined-role direct-grant checks. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | Exactly 34 explicit contracts, per-subcase results, authoritative queries and source-declared test-only fixtures. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs` | Recovery request/attempt/replay contract pins. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | Frozen scenario actions, exact evidence assertions and all-contract offline mutation checks. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | Negative placeholder/shared-fixture scans and SQL/ACL/rollback/purge contract checks. |
| `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | Exact runtime-derived fixture/action/query/subcase evidence validation; removed label-derived expected identifiers and hashes. |
| `outputs/rev869b_source_correction_checkpoint_23.md` | This checkpoint. |

No application, domain, API purchase workflow, permission, approval, calculation, entity-model, migration-identity, designer or snapshot file changed.

## 3. Five authorized correction groups

### A. Recovery event/request identity

`rev869b_begin_drop` now accepts distinct `transition_request_id` and `registration_request_id` values. It rejects zero IDs and reuse, binds the stored attempt to the registration request, binds recovery action/actor/execution instance/operation, and writes the immutable `DropStarted` event under the separate transition request. Function ownership/grants and canonical verification signatures use the exact new signature. This removes the valid-path `(LeaseId, RequestId)` collision while preserving event uniqueness and replay rejection.

### B. Purge retry-root enforcement

Purge authorization creation is serialized by target-instance and operation. A new root is rejected while any failed/interrupted root lacks a consumed terminal `Succeeded` or `ZeroRows` attempt. An expired, unused approved child is durably marked `Expired` and can be replaced; a non-expired or consumed child blocks another child. Root, parent attempt, target, operation, scope, cutoff, maximum rows, prior terminal state, prior evidence and monotonic ordinal remain exact and immutable. The partial uniqueness constraint permits only replacement of a genuinely expired unused child.

### C. ACL closure

Target and control-plane verifiers now classify PostgreSQL predefined `pg_*` aggregate roles separately: their built-in aggregate effects are excluded from ordinary-role effective-delta scans, while every direct database/schema/relation/sequence/function/default grant to a predefined role is rejected. Lifecycle administrator database/schema/function/relation/sequence capability is checked instead of excluded wholesale. Exact owner-to-administrator membership is required; all other `nexa_rev869b_*` memberships are rejected. Target database and schema ownership, all object/function ownership, explicit grants, defaults, PUBLIC, role capabilities, runtime/audit/purge/export/verifier roles, SECURITY DEFINER catalogue definitions and fixed search paths remain covered.

### D. Rollback proof

The noncommit terminalizer no longer depends on restricted `pg_stat_activity.backend_xid`. Opening the exact command transaction acquires two independently seeded transaction-scoped advisory fences bound to the attempt UUID. `RolledBack` or `Abandoned` is denied while either exact fence is still held; after rollback the minimized audit principal can acquire both without statistics or superuser privileges. Durable attempt identity, execution instance, service identity, ownership lease, receipt absence, context consistency, outcome ID/state/category and immutable replay remain bound. The already-PASS physical SQL column/type/ownership contract is unchanged.

### E. Exact 34-scenario evidence

Exactly 34 unique contracts and 34 `[Fact]` bodies remain. Every contract explicitly supplies affected-row, SQLSTATE/object, decision, before/after count and terminal expectations; optional/default result parameters were removed. Each has unique fixture/action/cleanup operation IDs, an authoritative readback function, exact object identity and typed subcase requirements. Runtime evidence must provide nonzero distinct run/lease/fixture/command/authorization/attempt/durable/cleanup identities as applicable, actual SHA-256 fingerprints, exact states/counts/SQLSTATE/object/terminal results, durable subcase evidence and finalized absence cleanup.

C04 and G05 contain exact source-declared test-only trigger/function DDL plus reverse teardown; those objects do not occur in production migration SQL. Compressed C04/C06/G05/G06/E04 cases now carry exact per-subcase terminal results. P02/L01/L03/L04/T03 frozen actions are restored. T03 mutation-checks every contract's setup, action, states, terminal/cleanup results, fixture operation, action operation, authoritative query, cleanup operation and typed subcase evidence; test-only fixture DDL is separately mutation-checked.

No scenario can pass solely on a generic exception, zero-row label, shared record, missing fixture, label-derived identifier/hash, placeholder, constant PASS result or a composite terminal string.

## 4. Previously-PASS blockers preserved

| Blocker | State | Evidence |
|---|:---:|---|
| B21-01 — every command-terminalization SQL column exists in the authoritative physical schema with correct type and ownership | PRESERVED | Focused alias/type/ownership contracts pass; no physical column was removed or re-owned. |
| B21-02 — quarantine evidence is complete, durable, instance/operation/attempt-bound and cannot complete with missing evidence | PRESERVED | Quarantine registration, active attempt/request, actor, operation, execution instance, evidence digest, source lease version, terminal outcome and exact replay checks remain unchanged and pass focused source contracts. |

The remaining three five-blocker statements are implemented for independent source-only re-review; this checkpoint does not self-award PASS.

## 5. Frozen architecture and regression boundary

The architecture remains RETAIN:

1. Provisioning is external.
2. A dedicated lifecycle controller alone holds lifecycle administration.
3. The control-plane database survives target disposal.
4. Command, purge and export ledgers remain target-local and transactional.
5. Tests and application code receive no lifecycle-administrator credential.

Purchase workflow, organization scope, permissions, approval thresholds, commercial masking, GST/payable calculations, revision/rejection/resubmission and audit/history retention files were outside the diff. The full non-PostgreSQL suite passed. Runtime PostgreSQL non-regression is not claimed.

## 6. Offline validation

| Validation | Result |
|---|---|
| Release build, no restore | PASS — 0 warnings, 0 errors. |
| Focused REV869B non-PostgreSQL suite | PASS — 71 passed, 0 failed, 0 skipped. |
| Focused Correction 23 source contracts plus offline T03 mutation meta-test | PASS — 21 passed, 0 failed, 0 skipped. |
| Complete non-PostgreSQL suite | PASS — 445 passed, 0 failed, 0 skipped. |
| Correction 23 scenario discovery | PASS — exactly 34 unique tests listed; 33 external database/controller bodies not executed; T03 is an offline source mutation test. |
| PostgreSQL access/tests | `0`; NOT RUN and NOT AUTHORIZED. |
| Windows PowerShell 5.1 AST | PASS — 24 files parsed, 0 errors; scripts were not executed. |
| EF migration discovery | PASS — `--no-connect`, inert loopback configuration; 13 migrations. |
| REV869A/REV869B uniqueness/order | PASS — two tracked files each (migration plus designer); indices 11 and 12, adjacent. |
| Model/snapshot parity and retained SQL contract | PASS — 2 explicit no-connect tests. |
| Offline Up SQL | PASS — 270,321 bytes; 2,346 lines; SHA-256 `EA79B9EA510F769209476B3D7567B8B01EF3321696967BF5F85650F79FE23CA2`. |
| Offline Down SQL | PASS — 10,320 bytes; 214 lines; SHA-256 `46F279DF26C23B54A7316147F7C65FBDB347C29B6029B35CA2BF443D84A0459C`. |
| SQL-column, purge, recovery and ACL contract scans | PASS through focused executable source contracts. |
| Generic evidence/placeholder/scenario-drift scans | PASS; no optional result defaults, label-derived expected IDs/hashes, undeclared production failpoints or prohibited composite terminal labels. |
| Secret/privacy/connection/prohibited-operation scan of diff | PASS — no credential, connection execution, production identifier, database create/drop/apply, or adjacent-tree reference added. |
| `git diff --check` | PASS. |

Offline SQL files were generated only as temporary artifacts, hashed, and removed. No migration was applied or removed.

## 7. External prerequisites

The following remain unavailable and block execution-helper readiness and every behavioral PostgreSQL verdict:

1. Exact externally provisioned PostgreSQL cluster, surviving control-plane database, target database, roles, memberships, database/schema/object/default/PUBLIC ACLs and rotated credentials.
2. Pinned cluster system identifier, endpoint, TLS/SPKI, source/package/controller manifests and target-instance provenance.
3. Independently reviewed deployed lifecycle controller/reconciler and signing keys supporting the exact scenario fixture/action/query/cleanup manifests.
4. Separately authorized management, recovery, purge, export, audit and verifier identities/decisions.
5. Isolated targets plus deterministic barriers, restart boundaries and reviewed creation/teardown of the C04/G05 test-only failpoints.
6. A separate explicit PostgreSQL execution authorization after a fresh independent source-only safety review.

## 8. Exact next gate and canonical states

Exact next gate: commit this bounded Correction 23 and checkpoint, then perform the required internal source-only adversarial precheck. After that, a fresh independent source-only safety re-review must decide source safety. PostgreSQL execution remains forbidden unless separately authorized after that review.

`correction_23_source_implementation_state=READY_FOR_INDEPENDENT_SOURCE_REVIEW`

`rev869b_source_safety_state=NOT_SELF_DECLARED`

`rev869b_execution_helper_readiness_state=FAIL`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`frozen_architecture_state=RETAIN`
