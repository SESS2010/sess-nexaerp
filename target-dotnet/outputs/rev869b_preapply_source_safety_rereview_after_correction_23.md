# REV869B Correction 23 independent source-only safety re-review

Date: 2026-08-15 (Asia/Calcutta)  
Review type: fresh source-first, offline, non-operational review  
Verdict: **FAIL — NOT APPROVED FOR EXECUTION**

## 1. Scope and entry gate

The reviewed source commit is `07a66905cf53a851927cfbc313aa348baa1f2133` (`Correct REV869B control-plane safety checkpoint 23`), whose exact parent is `9c9cbaa9548ba51a9f019a0005ddef62ee54518f`. The internal precheck is the report-only descendant `5b4cd483b299563e492035d9d5fb7d1ad7cf7622`.

At review start, branch `master` was at `31ad0dc53144f87f32957dbb8a71c98252f47989`. The ancestry from the reviewed source was:

1. `07a66905cf53a851927cfbc313aa348baa1f2133` — Correction 23, nine changed files.
2. `5b4cd483b299563e492035d9d5fb7d1ad7cf7622` — internal precheck, one report only.
3. `d879d61413d642c28f1618e0e0451215fd3a80bd` — roadmap/status report only.
4. `31ad0dc53144f87f32957dbb8a71c98252f47989` — final status report only.

Both required commits are ancestors of HEAD. The three descendants of Correction 23 contain no source, test, migration, or helper changes, and `git diff 07a66905..HEAD -- src tests tools` is empty. The target-scoped worktree was clean before review. No history operation was performed.

The checkpoint SHA-256 is `BA9C29C4907BAB7EC3C018DFB87FCAD9559C0CA0056DC363000588DBF68ABCC4`. The internal-precheck SHA-256 is `71EB65B6D203AA0071F2A4CC67F4A3CBDA435D6A3C07B2259FF6DF3C48BFEF42`.

`../legacy-reference/` was not read, listed, searched, modified, or committed.

## 2. Exact Correction 23 diff reconciliation

The exact parent-to-Correction-23 diff contains nine files, 559 insertions and 182 deletions. `git diff --check` returned success with no output.

| Status | File | Review disposition |
|---|---|---|
| Added | `outputs/rev869b_source_correction_checkpoint_23.md` | Claim document; read fully, not treated as proof. |
| Modified | `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | Target command, purge, rollback and ACL SQL reviewed. |
| Modified | `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | All 34 acceptance contracts reviewed. |
| Modified | `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs` | Lifecycle source assertions reviewed. |
| Modified | `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | Scenario dispatch and T03 reviewed. |
| Modified | `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | Retained source contracts reviewed. |
| Modified | `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | Shared acceptance request/evidence validation reviewed. |
| Modified | `tools/rev869b-control-plane-install.sql` | Lifecycle functions, owners and grants reviewed. |
| Modified | `tools/rev869b-control-plane-verify.sql` | Catalogue, signature and ACL verifier reviewed. |

No purchase application workflow, permission handler, approval service, calculation code, domain model, migration identity, designer, or snapshot file changed in this correction. The complete offline suite and model checks below found no regression, but they do not cure the two safety defects.

## 3. Independent F23 adjudication

### F23-01 — normal-drop registration substitution: CONFIRMED / FAIL

Expected result: a normal drop must prove that its `registration_request_id` is the exact authoritative authorization request for the same lease and transition lineage; a caller must not be able to substitute an unrelated UUID and have it recorded as authoritative.

Actual source:

- `tools/rev869b-control-plane-install.sql:124-129` records the immutable `DropAuthorized` event under its authorization `request_id`.
- `rev869b_begin_drop` at lines 164-171 accepts distinct `transition_request_id` and `registration_request_id`, rejects zero/equal IDs, and writes the `DropStarted` event under the transition ID.
- In the normal lifecycle branch at line 169, the caller-supplied `registration_request_id` is inserted directly into `rev869b_lifecycle_attempts`. There is no join to `rev869b_database_lease_events` proving that it equals the preceding `DropAuthorized` event request for the same lease and authoritative version.
- The recovery branch at line 170 does bind `a.RegistrationRequestId=registration_request_id` together with decision, action, actor, instance and operation. That recovery protection is preserved, but it does not protect normal drop.
- `Rev869BCorrection16SourceContractTests.cs:90-100` checks the distinct IDs and recovery-attempt predicate. It has no negative normal-drop registration-substitution test and no assertion for an authorization-event lookup.

Root cause: the correction separated the event-transition ID from the attempt-registration ID to avoid the uniqueness collision, but did not bind the normal-drop registration identity back to an immutable authorization fact. The schema uniqueness constraints prevent duplicates; they do not establish provenance.

Required correction: derive or validate the normal-drop registration ID against the exact `DropAuthorized` event for the lease and expected predecessor version, reject substitution with a stable SQLSTATE/object identity, and add positive, replay, cross-lease, stale-version and substituted-ID source/acceptance evidence.

PostgreSQL later required: yes, after source approval, to prove concurrency, transaction behavior and catalogue-installed behavior. It is not required to identify this source defect.

### F23-02 — generic/shared scenario acceptance: CONFIRMED / FAIL

Expected result: each of the exact 34 scenarios must contain scenario-specific executable setup/action/evidence/cleanup, objectively derived before/after facts, durable evidence IDs, exact terminal results, and mutation tests that remove or corrupt the intended executable action or evidence assertion.

Actual source:

- Thirty-three PostgreSQL facts call the same `RunAsync` path; T03 is the one offline fact.
- `Rev869BLifecycleControllerClient.RunAcceptanceScenarioAsync` posts the complete expected contract to one generic endpoint and accepts one shared `AcceptanceEvidence` shape. The verifier primarily compares response fields to the submitted expected contract.
- `EvidenceQuery` is required, serialized and compared as a returned string, but the client contains zero `NpgsqlCommand`, `ExecuteReader`, `ExecuteScalar`, or `ExecuteNonQuery` calls. There is no independent execution of each scenario's evidence query outside the response being adjudicated.
- All 33 runtime scenarios use copied `1 -> 1` before/after expectations except G02 (`0 -> 0`). These constants are not scenario-derived fixture facts.
- P02 and P03 both still use SQLSTATE `22012` and `pg_catalog.int4div(integer,integer)`, a common division-by-zero sentinel unrelated to the claimed manifest/TLS or catalogue/ACL denial.
- T03 creates 13 blank-string contract mutations (and optionally removes one fixture-DDL entry), then proves hash/metadata validation failure. It does not remove or corrupt the executed scenario action, database mutation, independent evidence query, or outcome assertion.
- The 34 IDs are present and unique, and their typed manifests are non-empty. Those structural improvements do not establish objective execution evidence.

Root cause: descriptive contracts and signed echoes were substituted for independently derived scenario-local facts. The common runner compresses distinct safety cases into one acceptance mechanism, while T03 tests contract completeness rather than action/evidence mutation sensitivity.

Required correction: provide scenario-local executable adapters or an equivalently typed exhaustive dispatcher whose branches actually perform each setup/action/evidence/cleanup; derive counts/fingerprints from authoritative stores; independently execute or otherwise independently attest each evidence query; replace P02/P03 sentinels with their actual denial identity; and mutate each executable action and objective assertion so the corresponding test demonstrably fails.

PostgreSQL later required: yes, for the 33 operational scenarios after source approval and external provisioning. It is not required to identify the source/test-design defect.

## 4. Consolidated safety matrix

| Correction 23 area | Result | Evidence and disposition |
|---|---:|---|
| A. Lifecycle authorization/attempt/replay binding | **FAIL** | F23-01: recovery binding is retained, but normal-drop registration provenance is bypassable. |
| B. Quarantine durability and instance/operation/attempt binding | **PASS (source)** | Dedicated attempt registration and exact binding remain present; terminal outcome insertion is immutable and missing evidence cannot satisfy the source contract. Runtime proof remains external. |
| C. Purge retry-root linkage | **PASS (source)** | `RootAuthorizationId`, `PriorAttemptId`, target, operation, scope, cutoff, maximum rows, retry ordinal, prior terminal outcome and prior evidence hash are compared; unresolved chains and duplicate children are rejected. Runtime proof remains external. |
| D. ACL closure and durable rollback evidence | **PASS (source)** | Owner loops cover tables, sequences, views/materialized views/foreign tables and functions; schema/database owners, role capabilities, explicit grants, default privileges and `PUBLIC` revocation are checked. RolledBack recording uses durable attempt identity and advisory exclusion without requiring a surviving rolled-back receipt. Runtime/catalogue proof remains external. |
| E. Exact 34 scenario evidence | **FAIL** | F23-02: a shared signed echo path, copied counts, unrelated P02/P03 sentinel and metadata-only T03 leave every scenario without the required independent objective proof. |

The previously passing authoritative SQL-column/schema blocker is preserved: the retained physical schema and source contract still define the referenced command-terminalization columns with their intended types and ownership. No Correction 23 change removed that protection.

Totals: **3 PASS (source), 2 FAIL**. Any FAIL is release-blocking.

## 5. Complete 34-scenario source-review matrix

“Expected” below is the committed terminal outcome and, for denial scenarios, the committed SQLSTATE/object. “Result” judges source-level evidence sufficiency; no PostgreSQL scenario was executed.

| ID | Expected | Result | Exact defect |
|---|---|---:|---|
| P01 | ExternalVerified | FAIL | Shared returned contract/evidence string; no independent verifier-result derivation. |
| P02 | PreflightDenied / `22012` / `pg_catalog.int4div(integer,integer)` | FAIL | Unrelated sentinel plus shared echo path. |
| P03 | VerificationDenied / `22012` / `pg_catalog.int4div(integer,integer)` | FAIL | Unrelated sentinel plus shared echo path. |
| L01 | Ready | FAIL | Copied `1 -> 1`; no scenario-local interruption/resume evidence execution. |
| L02 | Ready | FAIL | Copied `1 -> 1`; no independently derived per-create-boundary evidence. |
| L03 | DropStarted / `40001` / `UX_rev869b_one_active_lifecycle_attempt` | FAIL | Shared runner does not independently prove the two-request barrier and single drop. |
| L04 | Finalized | FAIL | Shared runner does not independently prove every interruption boundary. |
| L05 | Quarantined / `42501` / `rev869b_target_identity_mismatch` | FAIL | Identity/quarantine result is echoed, not independently queried. |
| R01 | Finalized | FAIL | Recovery decision/action/attempt evidence is not independently derived. |
| R02 | RecoveryAuthorized / `42501` / `rev869b_recovery_decision_replay` | FAIL | Replay denial is accepted through the common response contract only. |
| R03 | Finalized | FAIL | Failure-to-fresh-decision linkage is not independently derived. |
| C01 | Committed | FAIL | Business, receipt and outcome facts are not independently queried. |
| C02 | Committed | FAIL | Lost-response receipt replay is not independently established. |
| C03 | RequestRegistered / `23505` / `rev869b_command_request_replay_mismatch` | FAIL | Changed-request denial is accepted through the shared response. |
| C04 | RolledBack / `P0001` / `TR_rev869b_command_receipt_failpoint` | FAIL | Rollback/failpoint facts use copied counts and shared response. |
| C05 | RolledBack | FAIL | Durable rollback outcome is not independently queried. |
| C06 | FourExactInterruptionOutcomesReconciled | FAIL | Four boundaries are compressed into one label/response. |
| C07 | AttemptStarted / `40001` / `rev869b_command_attempt_active` | FAIL | Concurrent barrier/result is not independently established. |
| C08 | AttemptStarted / `42501` / `rev869b_attempt_binding` | FAIL | Required substitutions are compressed into one common acceptance record. |
| G01 | Denied / `42501` / `rev869b_purge_batch_binding` | FAIL | Distinct invalid-authorization subcases are not independently derived. |
| G02 | ZeroRows / `0 -> 0` | FAIL | Only zero-row contract; no independent candidate/fingerprint proof. |
| G03 | Succeeded | FAIL | Frozen candidates, deletion and audit facts are not independently queried. |
| G04 | Failed / `40001` / `rev869b_purge_candidate_drift` | FAIL | Drift and rollback evidence are accepted through the shared response. |
| G05 | Failed / `P0001` / `TR_rev869b_purge_delete_failpoint` | FAIL | Independent failure record is not independently queried. |
| G06 | Failed / `42501` / `rev869b_purge_retry_binding` | FAIL | Concurrency, substituted retry and monotonic retry are compressed. |
| E01 | Prepared | FAIL | Minimized batch contents/fingerprint are not independently derived. |
| E02 | Prepared | FAIL | Immutability after later insertion is not independently queried. |
| E03 | Denied / `42501` / `rev869b_export_release_sequence` | FAIL | Expired/wrong/concurrent subcases are compressed into one record. |
| E04 | ReleaseRetrySequenceVerified | FAIL | Interrupted delivery/new release sequence is a returned label. |
| A01 | Verified | FAIL | Effective privilege inventory is not independently enumerated by the client. |
| A02 | Denied / `42501` / `rev869b_protected_object_acl` | FAIL | Principals/objects/functions are compressed; no per-attempt independent result. |
| T01 | InUse | FAIL | Controller-owned allocation and exact target are returned, not independently attested. |
| T02 | Finalized | FAIL | Restart/surviving cleanup evidence is returned through the shared path. |
| T03 | MutationSensitive | FAIL | Blanks metadata/contract fields; does not mutate executed actions or objective assertions. |

Scenario totals: **0 PASS, 34 FAIL** for source-safety evidence sufficiency. Structural discovery totals remain 34 unique scenarios: 33 PostgreSQL-tagged operational facts plus T03 offline.

## 6. Frozen architecture and regression decision

The frozen architecture remains coherent and is **RETAINED**:

- external provisioning remains external;
- a dedicated lifecycle controller remains the lifecycle authority;
- the control-plane database survives target lifecycle operations;
- command, purge and export ledgers remain target-local and transactional.

F23-01 is an implementation binding omission within that architecture. F23-02 is an acceptance-test/evidence design defect. Neither justifies moving lifecycle authority into the target database or changing the four frozen boundaries.

Purchase workflow, permission, approval, calculation and audit-history regression result: **no source regression found**. Correction 23 did not change those application/domain surfaces; the solution built with zero warnings/errors, all 445 non-PostgreSQL tests passed, and model/snapshot checks passed. This is an offline source conclusion only.

## 7. Reproduced offline validation

| Validation | Result |
|---|---|
| Release build, `--no-restore` | PASS — 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL tests | PASS — 71/71 |
| Correction source-contract plus T03 focus | PASS — 21/21 |
| Complete non-PostgreSQL suite | PASS — 445/445 |
| PostgreSQL scenario discovery | PASS — exactly 34 listed |
| PostgreSQL tests executed | **0** |
| PowerShell 5.1 AST | PASS — 24 files, 0 parse errors |
| EF migration discovery | PASS — `--no-connect`, 13 migrations |
| REV869A/REV869B uniqueness/order | PASS — one migration class plus one designer each; REV869A index 11, REV869B index 12 |
| Model/snapshot and retained SQL contracts | PASS — 2/2 |
| Offline Up SQL | PASS — 270,321 bytes, 2,346 lines, SHA-256 `EA79B9EA510F769209476B3D7567B8B01EF3321696967BF5F85650F79FE23CA2` |
| Offline Down SQL | PASS — 10,320 bytes, 214 lines, SHA-256 `46F279DF26C23B54A7316147F7C65FBDB347C29B6029B35CA2BF443D84A0459C` |
| SQL column / purge / ACL / owner / default / `PUBLIC` scans | PASS for retained source contracts; F23-01 remains the lifecycle-binding exception described above |
| High-confidence secret scan | PASS — 0 hits |
| Privacy pattern scan | PASS — 0 hits |
| Operational API scan of reviewed harness | PASS — one connection-string parser reference, zero connection/open/execute calls in the acceptance client |
| Exact Correction 23 `git diff --check` | PASS |

Temporary SQL files were created only for offline hashing and removed immediately after their exact paths were verified. Test binaries and ordinary build outputs are ignored artifacts and were not committed.

Passing offline tests are not evidence that F23-01 or F23-02 is safe: the source-contract tests affirm the incomplete normal-drop predicate, and the acceptance tests affirm contract-shape echoes rather than independent scenario outcomes.

## 8. External prerequisites

The following remain unavailable and were neither provisioned nor accessed:

- an approved isolated PostgreSQL cluster and surviving control-plane database;
- externally provisioned non-superuser roles, ownership, certificates and pinned cluster identity;
- canonical installed control-plane/target packages and manifest hashes;
- the separately deployed lifecycle controller and signing key;
- authorized fixture/failpoint facilities and durable evidence storage;
- management approvals for lifecycle, recovery, purge and export operations.

These prerequisites block later execution evidence. They do **not** cause or excuse the two source-correctable defects.

## 9. Unresolved blockers and single next gate

Unresolved blockers:

1. F23-01: normal-drop `registration_request_id` is not bound to the authoritative `DropAuthorized` event/lease/version lineage.
2. F23-02: all 34 scenarios lack independently derived, scenario-specific executable acceptance evidence; P02/P03 retain an unrelated sentinel and T03 is metadata-only.

**Single next gate:** management authorization for a controlled source-only Correction 23 failure reconciliation that defines the smallest bounded Correction 24 scope and objective acceptance evidence. No Correction 24 implementation and no PostgreSQL or operational execution is authorized by this report.

## 10. Canonical states

`correction_23_independent_review_state=FAIL`

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`

`frozen_architecture_state=RETAIN`

`external_prerequisite_blocking_state=YES`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`
