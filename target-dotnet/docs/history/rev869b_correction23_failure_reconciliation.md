# REV869B Correction 23 failure reconciliation

Date: 2026-08-15 (Asia/Calcutta)  
Authorization: management-authorized source-only reconciliation  
Disposition: **Correction 24 source-only gate GO; Correction 23 remains FAIL**

## 1. Entry gate and lineage

| Gate | Result |
|---|---|
| Authorized HEAD | PASS — `6abf33271c67b14f1d3d82eb42fe50c245ce1fb1` |
| HEAD parent | `31ad0dc53144f87f32957dbb8a71c98252f47989` |
| Branch | `master` |
| Target-scoped status | PASS — clean |
| Correction 23 ancestry | PASS — `07a66905cf53a851927cfbc313aa348baa1f2133` is an ancestor |
| Internal-precheck ancestry | PASS — `5b4cd483b299563e492035d9d5fb7d1ad7cf7622` is an ancestor |
| Independent review commit | PASS — HEAD contains exactly the one review report |
| History mutation | None |
| PostgreSQL or operational execution | `0` |
| `../legacy-reference/` | Not read, listed, searched, modified, staged or committed |

The reconciled records were read as separate evidence:

1. `outputs/rev869b_source_correction_checkpoint_23.md`, the implementation claim at Correction 23.
2. `outputs/rev869b_correction23_internal_precheck.md`, a report-only internal adversarial FAIL.
3. `outputs/rev869b_preapply_source_safety_rereview_after_correction_23.md`, the committed independent FAIL.
4. The exact committed Correction 23 source and tests, with no source/test/helper drift after `07a66905cf53a851927cfbc313aa348baa1f2133`.

The checkpoint SHA-256 is `BA9C29C4907BAB7EC3C018DFB87FCAD9559C0CA0056DC363000588DBF68ABCC4`; the internal-precheck SHA-256 is `71EB65B6D203AA0071F2A4CC67F4A3CBDA435D6A3C07B2259FF6DF3C48BFEF42`; the independent-review SHA-256 is `CF6C5D9D564B7FF3DDAFCF5D6E82383FC5C770DF357E3CE215FB00F24923C310`.

## 2. Failure reconciliation

| Finding | Primary classification | Exact expected result | Actual failure | Root cause | Shared resolution | PostgreSQL needed later | Blocks Correction 24 source gate |
|---|---|---|---|---|---|---|---|
| F23-01 | `SOURCE_DEFECT` | Normal-drop registration identity equals the immutable `DropAuthorized` request for the same lease and predecessor version; the transition request is new and distinct. | The lifecycle branch of `rev869b_begin_drop` inserts caller-supplied `registration_request_id` without looking up the authorization event. | Correction 23 separated transition and registration UUIDs to solve the event uniqueness collision, but treated nonzero/distinct/unique as provenance. Uniqueness does not prove authorization origin. | One SQL predicate plus bounded positive/negative source tests resolves the normal-drop symptoms while preserving recovery binding. | Yes, only for later transaction/concurrency/catalogue behavior. | No; it justifies the bounded fix. |
| F23-01-T | `TEST_DESIGN_DEFECT` | Tests reject substituted, reused, cross-lease and stale-version registration IDs. | Source tests check distinctness and recovery binding but never require a normal-drop authorization-event lookup. | Tests mirrored the new parameter shape rather than the security invariant. | Resolved with F23-01; not a separate production fix. | Yes, for later behavioral negatives. | No. |
| F23-02 | `TEST_DESIGN_DEFECT` | Each of 34 scenarios executes a scenario-specific plan and is accepted only from independently derived facts. | Thirty-three operational facts share one signed-response adjudicator; T03 only mutates metadata. Counts are copied, P02/P03 use the same unrelated sentinel, and evidence-query text is echoed rather than independently executed. | Descriptive contracts were mistaken for observations. The evidence producer and adjudicator share the submitted expectations, so signatures prove origin/integrity but not truth. | One typed evidence-plan redesign resolves the 34 derived symptoms; it must remain exhaustive per scenario. | Yes, for 33 later operational scenarios. | No; source design is correctable offline. |
| F23-02-X | `EXTERNAL_PROVISIONING_PREREQUISITE` | Approved controller, cluster, roles, pins, fixtures and evidence stores exist and match reviewed contracts. | Those systems are unavailable and were not accessed. | Deliberate architecture boundary, not a repository defect. | No source change can supply them. They remain a later execution gate. | Yes. | No for source-only implementation; yes for execution readiness. |

F23-01 and F23-02 are independent root causes. F23-01 is not caused by unavailable provisioning. F23-02's repository design defect is not excused by unavailable provisioning. The 34 scenario failures are derived manifestations of one shared evidence-design defect, not 34 unrelated production defects.

## 3. Preserved controls

Correction 24 must preserve without redesign:

- authoritative command-terminalization physical columns, types and ownership;
- quarantine attempt, request, instance, actor, operation, source-version, evidence and immutable terminal binding;
- purge root/parent/target/operation/policy/ordinal/prior-outcome/prior-evidence linkage and child serialization;
- target/control-plane owner, schema, table, sequence, function, default, inheritance, administrator, runtime, audit, purge, export, verifier and `PUBLIC` ACL closure;
- durable RolledBack/Abandoned evidence using the two transaction-scoped advisory fences and durable attempt identity;
- migration identities, designer, snapshot, purchase workflows, permissions, approvals, calculations and audit histories.

These controls passed the Correction 23 source review. Reopening them would exceed the smallest justified scope.

## 4. Smallest bounded Correction 24 scope

Correction 24 is authorized only to:

1. Bind normal-drop `registration_request_id` to the exact immediately preceding immutable `DropAuthorized` event for the same lease and `expected_version`, while retaining a distinct unused transition request.
2. Give that rejection a stable source-pinned SQLSTATE and constraint/object identity, without changing the public function signature unless an independently demonstrated impossibility requires a new reconciliation.
3. Replace the scenario signed-echo adjudication with an exhaustive typed plan in which the controller response is one input, not the verdict.
4. Obtain independent read-only observations through the already bounded verifier/audit interfaces and verifier connection; no lifecycle-administrator credential may enter tests.
5. Replace copied counts and P02/P03 sentinels with scenario-derived counts/fingerprints and real error-domain/code/object identities.
6. Make T03 mutate the executable action, independent query and assertion for every scenario and prove that each semantic mutant is killed.
7. Add source-contract guards that reject any regression to generic evidence, optional/default expectations, constant PASS, unexecuted query strings or compressed subcases.
8. Create the Correction 24 checkpoint and stop for internal and independent source review.

Not authorized: application/domain/API changes, migration identity/designer/snapshot changes, new production tables, purge/ACL/rollback redesign, PostgreSQL execution, external controller deployment, or operational work.

## 5. Exact Correction 24 file allowlist

| File | Permitted change |
|---|---|
| `tools/rev869b-control-plane-install.sql` | Add the F23-01 normal-drop authorization-event predicate and stable rejection identity only. |
| `tools/rev869b-control-plane-verify.sql` | Pin/verify only the affected function definition and any stable verifier denial identity required to replace the P03 sentinel. No ACL redesign. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs` | Positive and negative F23-01 source contracts. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | Replace copied/sentinel results with all 34 explicit typed evidence plans and formulas. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | Bind every fact to its exact plan and implement semantic T03 mutation coverage. |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | Add structural guards against shared echo adjudication, placeholder counts/sentinels, query non-execution, compressed cases and mutation drift. |
| `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | Separate signed controller claims from independently queried observations; validate verifier-only connections and compare canonical evidence. |
| `outputs/rev869b_source_correction_checkpoint_24.md` | Correction 24 scope, reconciliation, validation and next-gate report only. |

The allowlist is exhaustive. No new file is authorized. In particular, `Rev869BCommandContextSql.cs`, migration files, application/domain/API files and existing Correction 23 reports are excluded.

## 6. Objective acceptance model

For each scenario `s`, Correction 24 must define a typed immutable plan:

`Plan_s = (Fixture_s, Target_s, IDs_s, Action_s, Qbefore_s, Qafter_s, Qdurable_s, Error_s, Terminal_s, Cleanup_s, Mutants_s)`.

The common runner may provide transport and canonicalization only. It must not select expected results from labels or accept controller-returned expectations as observations.

For every scenario, acceptance is the conjunction:

`Accept_s = Pins_s ∧ Identity_s ∧ Fixture_s ∧ Before_s ∧ Action_s ∧ After_s ∧ Durable_s ∧ Error_s ∧ Terminal_s ∧ Isolation_s ∧ Cleanup_s ∧ Mutation_s`.

The terms mean:

- `Pins_s`: source, package/manifest, TLS SPKI, cluster system identifier, controller signing key and target-instance hash equal separately supplied pins.
- `Identity_s`: run, lease, fixture, authorization, command, attempt, decision where required, durable-evidence and cleanup-evidence IDs are nonzero, scenario-bound and pairwise distinct except where an explicit equality is the tested invariant.
- `Fixture_s`: an independent verifier read proves the exact fixture rows/objects and target instance; `FixtureSha256 = SHA256(Canonical(FixtureRowsAndObjects))`.
- `Before_s`: `BeforeCount = |Qbefore_s|` and `BeforeSha256 = SHA256(Canonical(Qbefore_s))`, computed by the test from an independent read before action.
- `Action_s`: the plan invokes the exact scenario adapter; `ActionReached` and affected rows come from the action result, not its requested contract.
- `After_s`: `AfterCount = |Qafter_s|` and `AfterSha256 = SHA256(Canonical(Qafter_s))`, computed independently after action. The plan states the exact set/delta relation, not a copied default.
- `Durable_s`: independent read returns exactly the required immutable rows keyed by scenario, run, lease/target, authorization, command, attempt and subcase; each evidence hash is recomputed from canonical content.
- `Error_s`: success requires no error; denial requires the exact error domain, SQLSTATE or stable controller code, constraint/object identity and zero prohibited mutation. Generic exceptions do not satisfy it.
- `Terminal_s`: independent authoritative state/outcome rows equal the exact terminal tuple; labels alone do not satisfy it.
- `Isolation_s`: unrelated sentinel rows/fingerprints are unchanged and no unplanned object/row mutation occurred.
- `Cleanup_s`: cleanup evidence is independently durable and proves exact target/role/fixture absence or the explicitly retained safe quarantine state. Missing cleanup evidence fails.
- `Mutation_s`: removing/corrupting the executable action, independent query or decisive assertion makes the scenario test fail for the intended invariant. Contract-hash or blank-field failure alone is insufficient.

The signed controller envelope remains useful for authenticity and correlation, but `Accept_s` must fail if the signed claims disagree with independent observations or if any independent query is absent.

## 7. Exact 34-scenario formulas and evidence

Each row below supplements the global conjunction. `Δ(X)=After(X)-Before(X)`. `Count_k(X)` means the independently queried durable row count under the exact key `k`. All hashes are recomputed over canonical ordered values.

| ID | Objective formula and independently required evidence |
|---|---|
| P01 | `PinMismatchCount=0 ∧ ControlFingerprint=ExpectedControlFingerprint ∧ TargetAclDelta=∅ ∧ VerifyResult=Exact`. Evidence: independent control-plane fingerprint, target verifier result, role/owner/default/`PUBLIC` inventory and pinned provenance. |
| P02 | For each source/manifest/TLS/cluster/signing-key subcase, `PinMismatchCount=1 ∧ AllocatedLeaseCount=0 ∧ ActionCount=0 ∧ ProblemCode=REV869B_PREFLIGHT_PIN_MISMATCH ∧ ProblemObject=the mutated pin`. Evidence comes from the preflight result and independent control-plane absence query; no SQL division sentinel or PostgreSQL SQLSTATE is permitted. |
| P03 | For each seeded catalogue/ACL delta, `SeededDeltaCount=1 ∧ VerifyResult=Denied ∧ ReportedDelta=SeededDelta ∧ ProtectedMutationCount=0 ∧ CleanupFingerprint=BaselineFingerprint`. Require a stable verifier SQLSTATE/controller code and the exact drifted object/grant identity from independent catalogue inventory; `int4div` is prohibited. |
| L01 | `ReservedEvents=1 ∧ (ResumeSameAttempt XOR SeparatelyAuthorizedCleanup) ∧ DuplicateAttempts=0`; resume branch ends `Ready` with one target/role set, cleanup branch ends `Finalized` with target/roles absent. Evidence: lease/event/attempt chain and external target/role inventory. |
| L02 | For every create boundary `b`, `StartedAttempts_b=1 ∧ ReconciledAttempts_b=1 ∧ LeaseState_b=Ready ∧ TargetCount_b=1 ∧ RoleSetCount_b=1`. Evidence is boundary-specific, restart-derived and independently read. |
| L03 | For Ready and InUse barriers, `CleanupRequests=2 ∧ DropStartedEvents=1 ∧ ActiveDropAttempts=1 ∧ PhysicalDropExecutions=1 ∧ LoserError=(40001,UX_rev869b_one_active_lifecycle_attempt)`. Also require the F23-01 authorization-registration-transition chain. |
| L04 | For every before/during/after DROP and role-cleanup boundary, `DropStartedEvents=1 ∧ FinalizedEvents=1 ∧ PhysicalDropExecutions≤1 ∧ TargetCount=0 ∧ RoleCount=0`. Evidence: surviving control-plane chain plus independently inventoried absence. |
| L05 | For each marker/catalogue mismatch, `UseMutations=0 ∧ DropMutations=0 ∧ Error=(42501,rev869b_target_identity_mismatch) ∧ QuarantineOutcomeCount_exactAttempt=1 ∧ LeaseState=Quarantined`. All identity, evidence and attempt hashes must recompute. |
| R01 | `DecisionCount=1 ∧ ConsumedAttemptId=AttemptId ∧ AuthorizedAction=PerformedAction ∧ RecoveryAttempts=1 ∧ FinalizedEvents=1 ∧ TargetAndRolesAbsent=true`. Evidence is independently read from decisions, attempts and lease events. |
| R02 | For same-action and changed-action replay, `NewAttempts=0 ∧ NewEvents=0 ∧ Error=(42501,rev869b_recovery_decision_replay) ∧ DecisionConsumedOnce ∧ LeaseState=RecoveryAuthorized`. |
| R03 | `CleanupFailureCount=1 ∧ OldDecisionAccepted=0 ∧ FreshLinkedDecisionCount=1 ∧ FreshDecisionConsumedOnce ∧ FinalizedEvents=1`. Bind failure evidence, new decision and recovery attempt by exact lease/instance/operation/action. |
| C01 | `Δ(BusinessRows)=ExpectedBusinessRows ∧ Δ(HistoryRows)=ExpectedHistoryRows ∧ Count_attempt(Receipts)=1 ∧ Count_attempt(CommittedOutcomes)=1 ∧ ActiveAttemptCount=0`. Fingerprint every row and bind command/request/attempt/context. |
| C02 | Across replay, `BusinessAfter2=BusinessAfter1 ∧ HistoryAfter2=HistoryAfter1 ∧ ReceiptId2=ReceiptId1 ∧ ResponseHash2=ResponseHash1 ∧ Count_attempt(Receipts)=1 ∧ Count_attempt(Outcomes)=1`. |
| C03 | `ChangedDigest≠RegisteredDigest ∧ Error=(23505,rev869b_command_request_replay_mismatch) ∧ Δ(Requests)=0 ∧ Δ(Attempts)=0 ∧ Δ(BusinessAndHistory)=0`. |
| C04 | `Error=(P0001,TR_rev869b_command_receipt_failpoint) ∧ Δ(BusinessRows)=0 ∧ Δ(HistoryRows)=0 ∧ Δ(Receipts)=0 ∧ Count_attempt(RolledBackOutcome)=1`; durable outcome must be recorded after rollback via independent audit transaction. |
| C05 | `OpenedExactAttempt=true ∧ TransactionRollback=true ∧ Δ(BusinessAndHistoryAndReceipts)=0 ∧ Count_attempt(RolledBackOutcome)=1 ∧ OutcomeBinding=(attempt,instance,service,ownership)`. |
| C06 | For before-open, after-open, during-commit and after-response subcases, independently prove respectively `Abandoned`, `Abandoned`, `RolledBack`, and authoritative committed/replayed receipt; each subcase has a distinct evidence ID and exactly one terminal outcome. |
| C07 | At the barrier, `StartRequests=2 ∧ StartedAttempts=1 ∧ ActiveAttempts=1 ∧ LoserError=(40001,rev869b_command_attempt_active) ∧ UnrelatedMutationCount=0`. |
| C08 | For backend, actor, organization, role and operation substitutions, `Accepted=0 ∧ Error=(42501,rev869b_attempt_binding) ∧ Δ(Contexts)=0 ∧ Δ(Receipts)=0 ∧ Δ(BusinessAndHistory)=0`, with a distinct evidence row per substitution. |
| G01 | For missing, expired, wrong-target, wrong-batch and wrong-organization authorization, `StartedAttempts=0 ∧ Candidates=0 ∧ PurgeEvents=0 ∧ Error=(42501,rev869b_purge_batch_binding)`, independently per subcase. |
| G02 | `EligibleRowsBefore=0 ∧ FrozenCandidates=0 ∧ DeletedRows=0 ∧ Count_attempt(ZeroRowsEvent)=1 ∧ Terminal=ZeroRows`; prove eligibility with an independent scoped query, not a zero-row label. |
| G03 | `N=EligibleRowsBefore>0 ∧ FrozenCandidates=N ∧ CandidateHash=Hash(EligibleIds) ∧ DeletedRows=N ∧ RemainingEligible=0 ∧ Count_attempt(SucceededEvent)=1 ∧ UnrelatedFingerprintAfter=Before`. |
| G04 | After deterministic candidate drift, `CurrentCandidateHash≠FrozenHash ∧ DeletedRows=0 ∧ ContextFingerprintAfter=Before ∧ Count_attempt(FailedEvent)=1 ∧ Error=(40001,rev869b_purge_candidate_drift)`. |
| G05 | `Error=(P0001,TR_rev869b_purge_delete_failpoint) ∧ DeletedRows=0 ∧ ContextFingerprintAfter=Before ∧ Count_attempt(FailedEvent)=1`; failure event must be independently committed after delete rollback. |
| G06 | `ConcurrentStarts=2 ∧ ConsumedAuthorizations=1 ∧ Executions≤1`; for retry, `Child.Root=Parent.Root ∧ Child.PriorAttempt=Parent.Attempt ∧ Child.Target/Operation/Scope/Cutoff/Max=Parent ∧ Child.Ordinal=Parent.Ordinal+1 ∧ Child.PriorOutcome/Hash=ActualParentTerminalEvidence ∧ ActiveChildCount=1`. Every substitution must be rejected with zero child. |
| E01 | `PreparedRows=ExactAllowedProjection(organization,fields,asOf) ∧ PreparedCount≤MaximumRows ∧ PreparedHash=Hash(CanonicalPreparedRows) ∧ ExcludedFieldCount=0 ∧ Count_batch(PreparedEvent)=1`. |
| E02 | After inserting a later eligible ledger row, `PreparedRowsAfter=PreparedRowsBefore ∧ PreparedHashAfter=PreparedHashBefore ∧ PreparedCountAfter=PreparedCountBefore`; the later row exists independently but is absent from the immutable batch. |
| E03 | For expired, wrong-terminal and concurrent-active release, `ReleasedRows=0 ∧ NewReleaseEvents=0 ∧ Error=(42501,rev869b_export_release_sequence)` per subcase, with unchanged prepared-batch fingerprint. |
| E04 | `Release1.State=Interrupted ∧ Release2.Id≠Release1.Id ∧ Release2.PriorReleaseId=Release1.Id ∧ ActiveReleaseCount=1 ∧ DeliverySuccessCount≤1 ∧ BatchHashUnchanged`; each sequence step has distinct durable evidence. |
| A01 | `ObservedEffectivePrivileges=ExpectedPrivileges ∧ Observed-Expected=∅ ∧ Expected-Observed=∅`; independently include database/schema/table/sequence/function/default grants, owners, role attributes/memberships, administrator, runtime/audit/purge/export/verifier and `PUBLIC`. |
| A02 | For every `(principal,protected object/function,ungranted operation)` tuple, `Allowed=false ∧ Error=(42501,rev869b_protected_object_acl) ∧ ProtectedFingerprintAfter=Before`; require one durable result per tuple, not one aggregate label. |
| T01 | `LeaseCount_run=1 ∧ FixturePrepared=true ∧ TargetCount=1 ∧ TargetIdentityHash=Expected ∧ RuntimeRole=nexa_rev869b_app_runtime ∧ VerifierRole=nexa_rev869b_target_verifier ∧ AdminCredentialCountInTest=0 ∧ LeaseState=InUse`; cleanup then proves exact absence. |
| T02 | After the during-DROP controller failure, `RestartedControllerInstance≠OriginalInstance ∧ ReconciledAttemptId=SurvivingAttemptId ∧ DropStartedEvents=1 ∧ FinalizedEvents=1 ∧ TargetAndRolesAbsent=true ∧ CleanupEvidenceCount=1`. |
| T03 | For every scenario `s`, independently replace/remove each executable `Action_s`, `Qbefore_s/Qafter_s/Qdurable_s`, decisive comparison, denial identity check and cleanup proof. `KilledMutants_s=RequiredNonEquivalentMutants_s`, and each failure identifies the intended invariant. Metadata blanks and contract-hash changes do not count. |

## 8. Correction 24 objective source acceptance

Before a Correction 24 checkpoint may claim readiness for independent source review, all of the following must be true offline:

1. Exact allowlist only; no unlisted tracked file changed.
2. F23-01 source scan proves an immutable event lookup binding lease, `DropAuthorized`, request ID and predecessor/current version; negative tests cover wrong, reused, cross-lease and stale-version IDs.
3. Exactly 34 unique scenario facts and exactly 34 unique typed evidence plans exist; no scenario is compressed into another's result.
4. Every plan declares exact fixture/target/IDs, independent before/action/after/durable/cleanup queries, formula, denial identity and semantic mutants.
5. The adjudicator opens only verifier/audit-scoped connections, executes each declared read, canonicalizes rows, recomputes hashes/counts and compares signed claims to independent observations.
6. P02/P03 contain no `22012`/`pg_catalog.int4div(integer,integer)` sentinel.
7. No 33-fold copied `1,1` pattern, optional/default expected results, constant PASS, generic exception-only acceptance, label-only terminal, missing fixture, shared evidence record as sole proof, or unexecuted query string remains.
8. T03 reports 100% kill of all required non-equivalent action/query/assertion/cleanup mutants across all 34 plans.
9. Retained source-contract tests for physical columns, quarantine, purge, ACL and rollback continue to pass.
10. Build is 0 warnings/0 errors; focused REV869B and complete non-PostgreSQL suites pass; PostgreSQL discovery is exactly 34 with execution count 0; PowerShell AST, EF no-connect discovery, REV869A/B order, model/snapshot, offline SQL hashes, scans and `git diff --check` pass.
11. A post-commit internal adversarial precheck and a later fresh independent source-only review are separate report-only gates.

Passing these source checks cannot set execution-helper readiness PASS. Behavioral acceptance remains unavailable until the external prerequisites exist and a separate PostgreSQL execution authorization is granted.

## 9. Frozen architecture and external prerequisites

Frozen architecture decision: **RETAIN**.

- Provisioning stays external.
- Only the dedicated lifecycle controller holds lifecycle-administrator authority.
- The control-plane database survives target disposal.
- Command, purge and export ledgers remain target-local and transactional.
- Tests may receive only narrowly scoped runtime/verifier/audit access, never lifecycle-administrator credentials.

Unavailable external prerequisites remain:

1. Approved isolated PostgreSQL cluster, surviving control plane and disposable targets.
2. Exact roles, owners, memberships, database/schema/object/default/`PUBLIC` ACLs and rotated credentials.
3. Pinned cluster/TLS/source/package/controller/target provenance.
4. Independently reviewed deployed controller/reconciler implementing the exact typed action protocol.
5. Deterministic barrier, restart and test-only failpoint facilities with reviewed teardown.
6. Authorized management/recovery/purge/export/audit/verifier identities and decisions.
7. A separate management authorization for PostgreSQL execution after source review PASS.

These prerequisites block execution readiness, not the bounded Correction 24 source implementation.

## 10. Explicit prohibitions and single next gate

Until a new authorization is issued: no source/test/helper change, no Correction 24 implementation, no PostgreSQL connection/test, no migration apply/remove, no provisioning, no lifecycle/purge/recovery/quarantine/export/production operation, no history rewrite, and no access to `../legacy-reference/`.

**Single next gate:** after this report-only commit, management may authorize one bounded source-only Correction 24 implementation starting from the commit containing this report and restricted to the exact eight-file allowlist above. That implementation must stop after its checkpoint commit for internal adversarial precheck; PostgreSQL remains prohibited.

correction_24_source_only_gate=GO
frozen_architecture_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
