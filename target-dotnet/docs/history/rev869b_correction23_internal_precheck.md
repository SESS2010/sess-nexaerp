# REV869B Correction 23 Internal Adversarial Precheck

## 1. Independence and verdict

This is the required post-commit internal adversarial precheck of Correction 23 commit `07a66905cf53a851927cfbc313aa348baa1f2133`. It was performed by the same working agent and is therefore explicitly **not independent**.

**Internal verdict: FAIL.** Correction 23 materially improves purge retry enforcement, ACL taxonomy and rollback liveness proof, and preserves both previously-PASS blockers. The adversarial read finds two remaining source/test blockers: normal-drop registration identity is not authoritatively linked to the preceding `DropAuthorized` request/event, and the 34-scenario layer still accepts one shared controller-signed evidence shape with copied counts and known sentinel denial evidence. These findings cannot be repaired in this stage because no Correction 24 or post-checkpoint source correction is authorized.

`correction_23_internal_precheck_independence_state=NOT_INDEPENDENT`

`correction_23_fresh_independent_review_required=YES`

## 2. Entry and lineage evidence

| Check | Result |
|---|:---:|
| HEAD | PASS — `07a66905cf53a851927cfbc313aa348baa1f2133`. |
| Parent | PASS — `9c9cbaa9548ba51a9f019a0005ddef62ee54518f`. |
| Subject | PASS — `Correct REV869B control-plane safety checkpoint 23`. |
| Committed file count | PASS — exactly 9 files: 8 bounded source/test/tool files plus the checkpoint. |
| Checkpoint SHA-256 | `BA9C29C4907BAB7EC3C018DFB87FCAD9559C0CA0056DC363000588DBF68ABCC4`. |
| Target-scoped status on entry | PASS — clean. |
| Correction 22 ancestry | PASS. |
| History mutation | None. |
| PostgreSQL/external execution | `0`; none performed. |
| Adjacent tree | `../legacy-reference/` was not read, modified, staged or committed. |

## 3. Adversarial method

The precheck treated the Correction 23 checkpoint as an assertion. It inspected the exact committed diff and current sources, traced each registration/transition and evidence path, counted scenario bodies/factory results, searched for shared evidence paths and sentinel objects, and reconciled the findings against the authoritative Correction 22 failure report. It also retained the already completed offline baseline: build 0 warnings/errors; 71 focused REV869B non-PostgreSQL tests; 21 focused source/mutation tests; 445 complete non-PostgreSQL tests; exactly 34 discovered scenarios; PostgreSQL tests executed 0; PowerShell 5.1 AST 24/0; EF no-connect discovery 13; REV869A/REV869B adjacent; model/snapshot parity; offline SQL hashes; scans; and `git diff --check`.

Passing offline shape tests do not override the source findings below.

## 4. Five-area adversarial matrix

| Area | Internal result | Evidence and reasoning |
|---|:---:|---|
| Recovery event/request identity | **FAIL** | Recovery attempts are now bound to their stored registration request and use a distinct transition request. The normal-drop branch, however, accepts any nonzero `registration_request_id`, inserts it into the new attempt, and never proves it equals the request ID of the immediately preceding immutable `DropAuthorized` event for that lease/version. Distinctness prevents collision but does not reject registration substitution. Source: `tools/rev869b-control-plane-install.sql`, `rev869b_authorize_normal_drop` and `rev869b_begin_drop`. |
| Purge retry-root enforcement | PASS at source-precheck level | Target/operation serialization, root/parent linkage, terminal consumption, expired-unused-child replacement and partial uniqueness are present. No offline bypass was found. Behavioral proof still requires PostgreSQL concurrency fixtures. |
| ACL closure | PASS at source-precheck level | Administrator is included in exact capability/database/schema/effective scans; owner membership is exact; predefined aggregate effects are classified separately while direct grants to `pg_*` roles are rejected across database/schema/relation/sequence/function/default ACLs. Owners, PUBLIC and defaults remain covered. PostgreSQL catalogue behavior remains an external acceptance prerequisite. |
| Rollback proof | PASS at source-precheck level | `pg_stat_activity` dependence is removed. The exact attempt transaction holds two transaction-scoped advisory fences; audit terminalization is denied while either remains held and remains bound to durable attempt/instance/service/ownership/receipt/outcome facts. Active/post-rollback behavior still requires the later two-session PostgreSQL test. |
| Exact 34-scenario evidence | **FAIL** | All IDs and fixture manifests are unique and typed, but 33 scenario facts still call one `RunAsync` method, one `RunAcceptanceScenarioAsync` endpoint and one shared `AcceptanceEvidence` record. The controller receives the expected contract and signs fields that the test compares back to that same contract; no independent verifier query result is obtained outside that shared response. Thirty-three contracts still explicitly carry copied `before=1, after=1` expectations. P02/P03 still expect SQLSTATE `22012` and `pg_catalog.int4div(integer,integer)`, the exact generic division sentinel already rejected by the authoritative reconciliation. T03 proves removal of contract metadata changes the hash/fails local shape validation; it does not mutation-remove each scenario's executable action, authoritative query and assertion implementation. |

## 5. Exact remaining failure evidence

### F23-01 — normal-drop registration substitution

Expected: `rev869b_begin_drop` must prove the registration request is the exact immutable `DropAuthorized` request/event for the same lease and expected version, while the transition request is new and distinct.

Actual: the normal branch inserts caller-supplied `registration_request_id` directly into `rev869b_lifecycle_attempts`. There is no lookup of `rev869b_database_lease_events` binding that ID to `ToState='DropAuthorized'`, the lease and `expected_version`.

Impact: a lifecycle caller can relabel the normal-drop attempt's registration identity even though the state transition itself is authorized. This violates request/transition binding and blocks recovery/lifecycle source acceptance.

Future evidence needed: an exact source predicate plus negative source tests for wrong, reused, cross-lease and cross-version registration IDs; later PostgreSQL positive/negative event-chain evidence.

### F23-02 — generic/shared scenario acceptance remains

Expected: each of 34 scenarios must have scenario-local fixture/action/authoritative query/assertion evidence, and no generic signer-supplied assertion, copied placeholder count, sentinel SQLSTATE/object, compressed case or label-only proof may pass.

Actual:

- `33` bodies call the same shared `RunAsync`; T03 is the offline meta-test.
- All external cases call `RunAcceptanceScenarioAsync` and deserialize the same signed `AcceptanceEvidence` shape.
- `33` inventory rows contain explicit `1,1` before/after expectations produced by mechanically expanding the former defaults; explicit syntax does not make them scenario-derived.
- P02 and P03 retain `22012` plus `pg_catalog.int4div(integer,integer)` rather than the required pin-specific preflight denial and exact catalogue/ACL delta.
- Fixture/action/query IDs are scenario-prefixed contracts for an unavailable external controller, not repository implementations or independently queried facts.
- T03 rejects blank contract fields, but it does not demonstrate that removing an executed action/query/assertion from each scenario causes its test to fail.

Impact: the scenario source design is improved but still cannot support B21-05 or any of the 34 scenario PASS verdicts.

Future evidence needed: a separately authorized, source-reviewed controller/fixture contract and scenario-local tests whose observed database rows/deltas are independently queried; replacement of copied counts and sentinels; true per-scenario mutation tests; later authorized PostgreSQL execution.

## 6. Preserved blockers and architecture

B21-01 remains preserved: authoritative physical command-terminalization columns, types and ownership were not weakened and focused contracts pass.

B21-02 remains preserved: quarantine evidence remains durable and bound to its stored attempt/request, instance, actor, operation, source version, authority evidence and terminal replay.

The frozen architecture remains **RETAIN**: external provisioning, dedicated lifecycle controller, surviving control-plane database, target-local transactional ledgers, and no lifecycle-administrator credential in tests/application code. No purchase workflow, permission, approval, calculation or audit/history file changed.

## 7. External prerequisites

1. Exact provisioned PostgreSQL/control-plane/target role and ACL universe.
2. Pinned cluster, endpoint, TLS/SPKI, source/package/controller and target-instance provenance.
3. Independently reviewed lifecycle controller/reconciler with scenario-specific action/query support.
4. Isolated deterministic barrier/restart/failpoint fixtures and teardown.
5. Separately authorized management/recovery/purge/export/audit/verifier identities.
6. A fresh independent source-only review before any PostgreSQL authorization.

External prerequisites do not excuse F23-01 or the copied/sentinel/shared source-test design in F23-02.

## 8. Exact next gate

Commit only this report. Then complete the required final offline validation and management status reports without changing source. The safety decision gate is a **fresh independent source-only review of commit `07a66905cf53a851927cfbc313aa348baa1f2133` together with this precheck**. The reviewer must confirm or reject F23-01/F23-02. No PostgreSQL execution, source correction, Correction 24 or history rewrite is authorized.

`correction_23_internal_precheck_state=FAIL`

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`

`frozen_architecture_state=RETAIN`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`correction_23_internal_precheck_independence_state=NOT_INDEPENDENT`

`correction_23_fresh_independent_review_required=YES`
