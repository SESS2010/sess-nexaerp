# REV869B Correction 24 internal-precheck failure reconciliation

Date: 2026-08-15

## 1. Reconciliation verdict

The Correction 24 internal-precheck failure is reconciled. F23-01 remains **PASS** with no contradictory source evidence. F23-02 remains uncorrected in the current source, but its 34/34 failure is reduced to four shared, bounded causes with an exact seven-file future Correction 25 boundary. A bounded source-only Correction 25 is **GO**; this report does not implement it and does not claim source safety or execution readiness.

## 2. Entry gate

| Gate | Result | Evidence |
|---|---|---|
| Authorized HEAD | PASS | `d2b7e2906e0c1bd66f133a480ed80e6730afb79e` |
| Expected parent | PASS | `25b14ce70b511b7d608feb6a09f260fdd00a5913` |
| Branch / subject | PASS | `master`; `Report REV869B Correction 24 internal adversarial precheck` |
| Authoritative report hash | PASS | `1FC199F437F8AFDBBE8CED5AC4035BCBC53CFE92851B4C5ABC0EC39BD67495C0` |
| HEAD commit scope | PASS | Exactly one added report: `outputs/rev869b_correction24_internal_adversarial_precheck.md` |
| Target-scoped worktree | PASS | Clean at entry |
| Later source/test/SQL/helper changes | PASS | None between Correction 24 implementation and authorized HEAD |
| Frozen architecture / ACL | PASS | `RETAIN`; no later source change and no contrary evidence |

The authoritative precheck was read completely. No PostgreSQL, provisioning, migration, lifecycle, purge, recovery, quarantine, export or production operation was executed. `../legacy-reference/` was not accessed.

## 3. F23-01 preservation

F23-01 remains PASS. `rev869b_authorize_normal_drop` creates the immutable `DropAuthorized` event, and `rev869b_begin_drop` resolves its distinct `registration_request_id` before mutation using exact lease, request, null attempt, Ready/InUse source, DropAuthorized destination, version, lifecycle principal, current lease pre-state and pinned target/control identities. Event uniqueness and immutability make `(LeaseId,RequestId,Version)` the exact event identity. Missing, stale, replayed, cross-target, cross-lease, wrong-version, wrong-event and wrong-authorization inputs fail closed. The normal attempt persists the authorization event evidence and the distinct transition event binds the new attempt. Existing ACLs remain purpose-specific.

Future Correction 25 must not edit this function or weaken its source contract. If `tools/rev869b-control-plane-install.sql` is edited for evidence-reader changes, an exact diff guard must prove that `rev869b_authorize_normal_drop` and `rev869b_begin_drop` are byte-for-byte unchanged from `25b14ce70b511b7d608feb6a09f260fdd00a5913`.

## 4. Consolidated F23-02 root causes

### RC25-01 — source defect: authoritative reader projections are incomplete

The control reader is rooted in an existing lease row, so it cannot return structured zero-row evidence for P02 or other expected-absence cases. The command and purge readers use inner joins that likewise cannot prove a missing attempt. Command evidence returns claims and ledger records but not the independently enumerated business/history rows and before/after counts needed by C01-C08. Purge evidence lacks independent eligibility/current-set and unrelated-row fingerprints required by G01-G06. Target ACL evidence lacks target identity and expected/observed set-difference projections needed by P01, A01, A02 and T01. These gaps cannot be repaired only in the client because missing rows were never returned by an authoritative database reader.

### RC25-02 — source/test-design defect: one audit stage supplies decisive formula facts

`FormulaAssertions` assigns every formula term to `EvidenceStage.Audit`. The client then treats one separately signed audit document as the source of database-verifiable counts, hashes and states. That violates local derivation from before/after/durable database reads. Controller audit may corroborate process-only facts such as restart instance or physical invocation count, but it may not supply a database acceptance count, expected value, terminal state, ACL delta or verdict.

### RC25-03 — source/test-design defect: formula text and assertions are not bijective

`ExactFormula` is only checked for non-emptiness. There is no immutable component-ID manifest and no proof that each formula component maps to exactly one executable assertion over an authorized observation. Removing a decisive assertion leaves the formula text unchanged and the contract valid.

### RC25-04 — test-design defect: mutants are detected but not killed

T03 verifies descriptor-hash change and marker insertion. It treats a removed assertion as success because the assertion is absent, and it never evaluates a tampered evidence bundle. Multi-subcase scenarios are compressed into one before/after/durable/audit read instead of independently keyed observations per boundary/substitution. Identity, count, state, row, lease, version and result mutations therefore lack an objective rejection test.

## 5. Defect classification

| Class | Items | Correction responsibility |
|---|---|---|
| Source defects | RC25-01; client portions of RC25-02/03 | Expand minimal verifier projections; derive typed metrics locally; require exact formula-component/assertion bijection |
| Test-design defects | Design/scenario/source-contract portions of RC25-02/03 and all RC25-04 | Replace shared audit assertions; enumerate subcase reads; evaluate tampered bundles; require every non-equivalent mutant to fail |
| Unavailable execution evidence | No provisioned cluster/principals/controller audit/action adapters and no PostgreSQL authorization | Later execution only; must not be represented by source labels, copied counts, signatures or PASS values |

## 6. Authoritative future evidence protocol

For every scenario and subcase, the future implementation must create a typed `FormulaComponent` with a stable component ID, authoritative stage/surface, exact row selector, local reducer and expected predicate. `ValidateContract` must enforce set equality between formula component IDs and executable assertion IDs. Database rows are ordered and canonicalized locally; counts are local cardinalities and hashes are locally recomputed. Before, after, durable and cleanup observations are separately opened queries and may be arrays keyed by boundary/subcase. The action result supplies correlated SQLSTATE/code/object and reached-boundary facts, never expected counts or a verdict. Controller audit is retained only as supplementary corroboration of irreducible process facts and cannot make an otherwise false database formula pass.

Acceptance for scenario `s` is:

`Accept_s = Pins_s AND Identity_s AND Fixture_s AND Before_s AND Action_s AND After_s AND Durable_s AND Error_s AND Terminal_s AND Isolation_s AND Cleanup_s AND Mutation_s`.

Every term must be executable. Missing evidence, a missing component/assertion, a duplicate read, an unauthorized audit-sourced database fact, a noncanonical row or any surviving non-equivalent mutant is FAIL.

## 7. Exact 34-row reconciliation matrix

Abbreviations: `CP` = control-plane verifier reader; `TC` = target command reader; `TP` = target purge reader; `TE` = target export reader; `TA` = target ACL reader. `B/A/D` means locally observed before/after/durable database evidence. File keys: `CPI`, `TSQL`, `D`, `S`, `SC`, `LC` correspond to the first six files in section 8.

| ID | Authoritative fixture and unique identity | Exact action/result | Local B/A/D and executable formula | Required tamper rejection | Supplementary controller evidence | Files |
|---|---|---|---|---|---|---|
| P01 | CP manifest plus target instance and compiled exact CP/target ACL inventories; unique target `InstanceId` | Run canonical verifier; no error | Fresh CP+TA rows before/after/durable; `pinMismatch=0 AND CPfacts=expected AND TAfacts=expected AND both set deltas empty AND fingerprints exact` | Delete/change/add one pin, owner, ACL/default/role/`PUBLIC` tuple or hash; affected component evaluates false | Invocation correlation only | CPI,TSQL,D,S,SC,LC |
| P02 | Immutable expected pins plus one locally mutated pin name/value and unique reservation request/run ID | Local preflight denies exact `REV869B_PREFLIGHT_PIN_MISMATCH/mutated-pin` | CP absence projection before/after/durable; `localPinMismatch=1 AND leaseDelta=0 AND eventDelta=0 AND targetAllocation=0`; local pin comparison is decisive | Equalize mutated pin, fabricate mismatch, add lease/event or remove zero-delta assertion; reject | Signed preflight response corroborates exact code/object only | CPI,D,S,SC,LC |
| P03 | Baseline CP catalogue/ACL rows plus one exact seeded object/grant delta identity | Run verifier; exact `REV869B_CONTROL_PLANE_CATALOGUE_MISMATCH/rev869b_control_plane_catalogue_acl` | CP facts before, seeded after, restored durable/cleanup; `seededDelta=reportedLocalDelta=1 AND protectedMutation=0 AND cleanupFingerprint=baseline` | Hide/substitute delta, copy baseline, accept sentinel/generic error or remove cleanup equality; reject | Verifier invocation/error corroboration only | CPI,D,S,SC,LC |
| L01 | Lease ID, reservation request, Reserved event/version and target identity | Resume exact attempt XOR separately authorized cleanup; Ready or absent | CP B/A/D keyed to lease/request/attempt; `ReservedEvents=1 AND exactlyOne(resume,cleanup) AND duplicateAttempts=0 AND ReadyHasOneTarget OR cleanupAbsent` | Swap attempt/request/version, make both/neither branches true, duplicate event or remove absence; reject | Physical cleanup invocation only | CPI,D,S,SC,LC |
| L02 | One lease/attempt per named create boundary with unique request/version | Restart/reconcile each boundary; Ready | Independent CP observation list per boundary; each `started=1 AND reconciled=1 AND state=Ready AND one target identity/role set` | Drop a boundary, reuse IDs, change count/state/version or copy another boundary; reject | Restart/barrier timestamps only | CPI,D,S,SC,LC |
| L03 | Ready and InUse leases, exact DropAuthorized event/request/version, two cleanup request IDs | Race cleanup; loser `40001/UX_rev869b_one_active_lifecycle_attempt` | CP B/A/D; `requests=2 AND DropStarted=1 AND activeAttempt=1 AND authorization-registration-transition chain exact`; durable terminal linkage required | Missing/stale/replayed/cross-lease/cross-target/wrong-version/wrong-event registration, count 2, or removed chain assertion; reject | Barrier and physical-invocation count corroboration only | CPI,D,S,SC,LC |
| L04 | Distinct lease/attempt per before/during/after DROP and role-cleanup boundary | Restart/reconcile; Finalized | Per-boundary CP lists; `DropStarted=1 AND Finalized=1 AND target/role absence durable`; event/attempt IDs exact | Remove boundary, duplicate Finalized, alter attempt/version, reuse evidence or remove absence; reject | Process restart/drop call count only | CPI,D,S,SC,LC |
| L05 | Ready lease with exact target marker/catalogue mismatch, attempt and observed hashes | Use/drop denied `42501/rev869b_target_identity_mismatch`; quarantine | CP B/A/D plus target identity row; `useDelta=0 AND dropDelta=0 AND exactAttemptQuarantine=1 AND state=Quarantined AND hashes recompute` | Replace instance/attempt/version/hash, fabricate quarantine label or remove zero mutation; reject | Detection/invocation timing only | CPI,TSQL,D,S,SC,LC |
| R01 | Quarantined lease, exact unconsumed decision/action/attempt and target identity | Consume and recover; Finalized | CP B/A/D; `decision=1 AND consumedAttempt=attempt AND authorizedAction=performedAction AND recovery=1 AND Finalized=1 AND target/roles absent` | Swap decision/action/attempt/lease, duplicate consumption or remove absence; reject | Physical recovery call only | CPI,D,S,SC,LC |
| R02 | Consumed decision plus immutable baseline attempt/event counts; unique subcase IDs | Replay wrong/expired/replayed/foreign/pre-state/action/nonce; `42501/rev869b_recovery_decision_replay` | Independent CP B/A/D for all eight subcases; each `attemptDelta=0 AND eventDelta=0 AND consumedOnce AND state=RecoveryAuthorized`; valid-preserved control | Remove/compress subcase, alter nonce/lease/version/action, accept generic error or nonzero delta; reject | Attempt invocation correlation only | CPI,D,S,SC,LC |
| R03 | CleanupFailed attempt/outcome, old decision, fresh exactly linked decision | Old denied; fresh consumed; recover Finalized | CP B/A/D; `failure=1 AND oldAccepted=0 AND freshLinked=1 AND freshConsumed=1 AND Finalized=1` | Reuse old/foreign decision, sever prior attempt/outcome/lease/action link or remove one assertion; reject | Restart evidence only | CPI,D,S,SC,LC |
| C01 | Command/request/attempt/context/claims plus exact business and history row IDs | Commit; receipt and Committed outcome | TC B/A/D must enumerate claimed business/history rows; `deltaBusiness=expected AND deltaHistory=expected AND receipts=1 AND committed=1 AND active=0`, all IDs/hashes exact | Change/remove a business/history row, receipt, outcome, identity or count; reject | Action boundary only | TSQL,D,S,SC,LC |
| C02 | Committed command and first-run business/history/receipt/response fingerprints | Replay same request after lost response | TC B/A/D; `business2=business1 AND history2=history1 AND receiptId2=receiptId1 AND responseHash2=responseHash1 AND receipts=outcomes=1` | Add second mutation/receipt/outcome, change response/hash or remove equality; reject | Lost-response marker only | TSQL,D,S,SC,LC |
| C03 | Registered request/idempotency key, changed digest and exact before counts | Replay changed digest; `23505/rev869b_command_request_replay_mismatch` | TC absence-capable B/A/D; `changedDigest!=registered AND requestDelta=attemptDelta=businessHistoryDelta=0` | Equalize digest, create attempt/row, change object identity or remove zero delta; reject | Action error correlation only | TSQL,D,S,SC,LC |
| C04 | Exact receipt failpoint attempt and business/history/receipt fingerprints | Commit attempt; `P0001/TR_rev869b_command_receipt_failpoint` | TC B/A/D after rollback; `businessDelta=historyDelta=receiptDelta=0 AND durable RolledBack outcome=1` | Use transaction-local outcome, add row/receipt, change trigger identity or remove durable proof; reject | Failpoint reached marker only | TSQL,D,S,SC,LC |
| C05 | Open exact attempt with backend/transaction/actor/org/version/role/operation binding | Roll back and independently terminalize RolledBack | TC B/A/D; `exactOpenedAttempt AND businessHistoryReceiptDelta=0 AND durable RolledBack=1` | Change any binding, use copied context, add mutation or remove rollback assertion; reject | Rollback invocation only | TSQL,D,S,SC,LC |
| C06 | Four distinct attempts for before-open/after-open/during-commit/after-response | Restart reconciler; exact prescribed terminal each | Four independently keyed TC B/A/D sets; `distinctEvidenceIds=4 AND one terminal row per attempt AND terminal vector exact` | Compress/reuse attempt/evidence, swap a terminal, remove a subcase or duplicate outcome; reject | Restart/boundary corroboration only | TSQL,D,S,SC,LC |
| C07 | One request, two distinct attempt-start requests and unrelated-row fingerprint | Concurrent start; loser `40001/rev869b_command_attempt_active` | TC B/A/D; `startRequests=2 AND started=active=1 AND unrelatedDelta=0` | Serialize/reuse ID, allow two active, change error object or remove isolation fingerprint; reject | Barrier timing only | TSQL,D,S,SC,LC |
| C08 | Exact attempt plus eight independently generated binding substitutions | Open/terminalize each; `42501/rev869b_attempt_binding` | Eight TC B/A/D sets; each `accepted=0 AND contextDelta=receiptDelta=businessHistoryDelta=0` | Remove a substitution, change backend/actor/org/version/role/operation/instance/lease, accept generic error or remove delta; reject | Action correlation only | TSQL,D,S,SC,LC |
| G01 | Five invalid authorization fixtures keyed by authorization/run: missing, expired, target, batch, organization | Start purge; `42501/rev869b_purge_batch_binding` | Absence-capable TP B/A/D for each; `attempts=candidates=events=0` | Drop/compress case, fabricate zero label, create row or alter error identity; reject | Action correlation only | TSQL,D,S,SC,LC |
| G02 | Fresh authorization and independently enumerated zero eligible context set | Freeze; terminal ZeroRows | TP B/A/D with eligibility projection; `eligibleBefore=frozen=deleted=0 AND ZeroRowsEvents=1` | Replace eligibility query with stored/default zero, add candidate or remove event; reject | Invocation only | TSQL,D,S,SC,LC |
| G03 | Scoped authorization, ordered eligible context IDs and unrelated-row fingerprint | Freeze/delete; Succeeded | TP B/A/D; `N=eligibleBefore>0 AND frozen=N AND candidateHash=localHash(eligibleIds) AND deleted=N AND remaining=0 AND success=1 AND unrelated unchanged` | Alter ID set/order/hash/count, delete unrelated row or remove equality; reject | Worker call count only | TSQL,D,S,SC,LC |
| G04 | Started attempt, frozen candidate set and independently enumerated drifted current set | Delete; `40001/rev869b_purge_candidate_drift` | TP B/A/D; `currentHash!=frozenHash AND deleted=0 AND contextAfter=before AND failedEvent=1` | Equalize hashes, delete row, change object or remove durable failure; reject | Drift barrier only | TSQL,D,S,SC,LC |
| G05 | Exact delete-failpoint attempt and context fingerprint | Delete; `P0001/TR_rev869b_purge_delete_failpoint` | TP B/A/D after rollback; `deleted=0 AND contextAfter=before AND independently durable Failed=1` | Transaction-local evidence, altered trigger, missing row fingerprint or removed failure; reject | Failpoint reached marker only | TSQL,D,S,SC,LC |
| G06 | Two starts/executions, actual failed parent and exact child authorization/root/policy | Race; wrong retry `42501/rev869b_purge_retry_binding`; one exact child | Per-race/substitution TP B/A/D; `starts=2 AND consumed=1 AND executions<=1 AND exact root/prior/target/op/scope/cutoff/max/ordinal/outcome/hash AND activeChild=1 AND substitutedChild=0` | Alter every link separately, reuse child, allow two children/executions or remove parent terminal proof; reject | Barrier/worker invocation only | TSQL,D,S,SC,LC |
| E01 | Authorization, ordered allowed fields, source-row IDs/hashes, as-of/expiry/max | Prepare immutable batch | TE B/A/D; `rows=exactAllowedProjection AND count<=max AND storedHash=localHash(canonicalRows) AND excludedFields=0 AND preparedEvent=1` | Add excluded field/row, alter order/hash/max or trust stored hash without recomputation; reject | Preparation invocation only | TSQL,D,S,SC,LC |
| E02 | Prepared batch fingerprint plus independently inserted later eligible row ID | Reread batch | TE B/A/D; `preparedAfter=before AND hashAfter=before AND countAfter=before AND laterRowExists=1 AND laterRowInBatch=0` | Include later row, omit existence proof, change hash/count or compare labels; reject | Insertion/action timing only | TSQL,D,S,SC,LC |
| E03 | Four invalid release fixtures: expired, wrong batch, terminal, concurrent active | Read/authorize; `42501/rev869b_export_release_sequence` | Four TE B/A/D sets; each `releasedRows=0 AND newReleaseEvents=0 AND preparedHash unchanged` | Drop case, allow release/event, alter object identity or remove batch equality; reject | Action correlation only | TSQL,D,S,SC,LC |
| E04 | ReleaseStarted R1, batch fingerprint and delivery-loss barrier | Record Interrupted; authorize distinct R2 | TE B/A/D; `R1=Interrupted AND R2!=R1 AND R2.Prior=R1 AND active=1 AND delivered<=1 AND batch unchanged` | Reuse ID, sever prior, allow two active/delivered, alter batch or remove equality; reject | Delivery-loss/barrier corroboration only | TSQL,D,S,SC,LC |
| A01 | Exact compiled CP/target owner/role/database/schema/table/sequence/function/default/`PUBLIC` inventories and target identity | Enumerate privileges; no error | CP+TA fresh rows; `observed=expected AND observedMinusExpected=empty AND expectedMinusObserved=empty` | Add/delete/change any tuple/dimension/owner/role attribute or remove set-difference assertion; reject | None needed | CPI,TSQL,D,S,SC,LC |
| A02 | Unique principal/object/operation tuple fixtures plus protected-row fingerprint and target identity | Attempt each direct access; `42501/rev869b_protected_object_acl` | Stage-specific TA plus applicable TC/TP/TE B/A/D; each `allowed=false AND protectedAfter=before AND durableDenial=1` | Remove tuple, allow operation, alter protected row, accept generic error or remove fingerprint; reject | Invocation correlation only | TSQL,D,S,SC,LC |
| T01 | Exact opt-in, CP lease/request/event, target instance and role/ACL inventory | Allocate; InUse | CP+TA B/A/D; `lease=1 AND fixturePreparedLocally AND target=1 AND targetHash=expected AND runtime/verifier roles exact AND adminCredentials=0 AND state=InUse`; cleanup absence | Inject admin connection, wrong target/role/hash, duplicate lease or remove cleanup; reject | Physical allocation timing only | CPI,TSQL,D,S,SC,LC |
| T02 | L04 during-DROP lease/attempt and durable CP event baseline | Fail controller, restart, reconcile exact attempt | CP B/A/D; `reconciledAttempt=survivingAttempt AND DropStarted=1 AND Finalized=1 AND target/roles absent AND cleanupEvidence=1` | Swap attempt/instance binding, duplicate event, retain target/role or remove cleanup; reject | Original/restarted process IDs are corroborative only | CPI,D,S,SC,LC |
| T03 | All 34 immutable component manifests and each uniquely identified mutant | Evaluate every structural and evidence mutant; every mutant rejected | Synthetic typed canonical bundles plus contract validation; `killedMutants=requiredNonEquivalentMutants AND survivors=0`, with per-mutant failed component ID | Remove assertion/component; alter identity/count/state/audit row/target/lease/version/result; duplicate/stale/cross-instance bundle; each must produce validation failure or `Evaluate=false` | None | D,S,SC,LC |

All 34 rows retain unique scenario, fixture, action, cleanup and observation identities. Multi-boundary/substitution scenarios require independent observation arrays, not one shared record or one aggregate label.

## 8. Smallest exhaustive Correction 25 allowlist

Exactly seven files are necessary and sufficient:

1. `tools/rev869b-control-plane-install.sql`
   - Modify only `rev869b_read_lifecycle_evidence` and `rev869b_read_control_plane_acl_evidence` projections so expected absence and local exact set/count/hash derivation are possible.
   - Preserve all lifecycle mutation functions, especially F23-01, byte-for-byte. Preserve owner, fixed search path and verifier-only ACLs.
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
   - Expand only the four verifier evidence readers to return absence-capable, ordered minimal rows for command business/history, purge eligibility/current/isolation, export linkage, ACL set differences and target identity.
   - Do not alter business, purge, export or authorization mutation functions; do not change reader signatures or broaden grants.
3. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
   - Replace audit-only formula assertions with typed per-component stage/surface/reducer definitions and independent subcase observation plans.
4. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
   - Execute the corrected evidence plans and replace T03 with objective structural and tampered-bundle rejection checks.
5. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
   - Pin the seven-file scope, reader projections, formula/assertion bijection, audit limitations, exact 34 names/formulas and mutation rejection requirements.
6. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
   - Support typed per-stage/subcase observations, local row-set cardinality/hash derivation, formula-component/assertion bijection, nondecisive controller audit and evidence-bundle tampering.
7. `outputs/rev869b_source_correction_checkpoint_25.md`
   - Record entry gate, exact diff, per-root-cause and 34-row implementation evidence, validation totals/hashes, external prerequisites and mandatory next gate.

No change is justified to `tools/rev869b-control-plane-verify.sql`, migration/designer/snapshot files, provisioning contracts, PostgreSQL behavior inventories, application/domain/API files or helpers. Function signatures and ACL inventory membership remain unchanged, so those files are outside the bounded scope. No convenience file may be added.

## 9. Assertion-removal and evidence-tampering strategy

For every scenario:

1. Assign every formula term an immutable `ComponentId` and compute `RequiredComponentIds` from the frozen scenario definition.
2. Require exact set equality: `RequiredComponentIds == AssertionComponentIds == EvaluatedComponentIds`; duplicates and extras fail.
3. Remove each assertion and each formula component independently. `ValidateContract` must fail with the missing component ID before any external call.
4. Build a pristine typed synthetic bundle whose local reducers satisfy the formula. Verify all components evaluate true.
5. Clone the bundle and independently mutate identity, row membership, row order, count, state, SQLSTATE/code/object, audit row ID, target instance, lease, request, attempt, version, result, cleanup and canonical hash. Recompute the outer canonical hash so rejection cannot depend only on stale hashing.
6. Require the intended component to evaluate false and require the scenario result to be FAIL. A marker in contract metadata is not a killed mutant.
7. Duplicate, substitute, stale and cross-instance observations must fail provenance checks even when their payload would otherwise satisfy numeric assertions.
8. T03 must report the exact killed-mutant ID set and assert it equals the exact required non-equivalent mutant ID set for all 34 scenarios; any survivor fails T03.

## 10. Explicitly prohibited future evidence designs

Correction 25 must statically reject controller-audit documents as decisive database proof, shared signed acceptance records, copied counts, echoed query/scenario labels, `22012`/`int4div` or other sentinels, constant/self-declared PASS, generic exception-only success, formula strings without executable component bijection, mutation tests that only detect text/hash changes, and tests that pass after a decisive assertion is removed.

## 11. Offline validation requirements

A future bounded Correction 25 must record:

- entry HEAD/hash/clean status and exact seven-file scope;
- build with 0 warnings/errors;
- focused Correction 25 formula-bijection and mutation tests;
- focused REV869B non-PostgreSQL and complete non-PostgreSQL suites;
- exactly 34 unique scenario discovery and PostgreSQL execution count 0;
- for every scenario, 100% required structural mutants and tampered-evidence mutants killed with exact IDs;
- source scans proving no decisive database formula component uses `ControllerAudit`;
- source scans proving no shared signature/count/label/sentinel/PASS evidence;
- PowerShell 5.1 AST without helper execution;
- EF `--no-connect` discovery, REV869A/B uniqueness/adjacency, model/snapshot parity and retained SQL contracts;
- raw in-memory Up/Down SQL byte/line counts and independent SHA-256;
- SQL column, reader-minimization, owner, function, default, role-inheritance and `PUBLIC` ACL scans;
- secret/privacy/prohibited-operation scans and `git diff --check`;
- a byte-for-byte F23-01 protected-slice comparison against Correction 24;
- final clean target-scoped status after the single implementation commit.

## 12. Later PostgreSQL evidence requirements

PostgreSQL execution remains unauthorized. After a successful Correction 25 source commit, internal adversarial precheck and fresh independent source-only review, a separately authorized external phase must provision the isolated cluster and exact principals, verify pins and controller/audit trust separation, apply the migration/control plane through authorized provisioning, then execute all 34 scenarios. It must persist per-scenario before/after/durable/cleanup canonical row sets, locally computed counts/hashes, exact action errors, formula-component results and killed-mutant IDs. T03's synthetic mutation corpus remains offline; the other scenarios require live authoritative rows. Database acceptance and helper readiness cannot be inferred from source compilation.

## 13. Recommendation and single next gate

Recommendation: **GO** for one bounded source-only Correction 25 restricted to the exact seven-file allowlist above. The correction must preserve F23-01, frozen architecture and ACL closure, must not execute PostgreSQL or operational workflows, and must stop after its checkpoint commit.

The single next gate is management authorization for that bounded source-only Correction 25 implementation.

f23_01_reconciliation_state=PASS
f23_02_reconciliation_state=PASS
correction_25_source_only_gate=GO
correction_24_failure_reconciliation_state=PASS
frozen_architecture_state=RETAIN
acl_boundary_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
