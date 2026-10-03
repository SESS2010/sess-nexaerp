# REV869B Option-A Phase-A Correction A4 independent architecture and security review

Date: 2026-08-21

Review type: fresh independent, report-only, source/offline review

Reviewed commit: `1440f7736bb2870ca3175e669b9d8699d33a7808`

Reviewed parent: `a126de9d2ab9efe90490d6a1734aac320bab5f04`

Authoritative implementation checkpoint: `outputs/rev869b_external_controller_phase_a_checkpoint.md`

Architecture freeze: `outputs/rev869b_external_controller_phase_a_a4_lease_atomic_boundary_architecture_freeze.md`

## 1. Independent verdict

`PHASE_A_CORRECTION_A4_INDEPENDENT_SOURCE_REVIEW=FAIL`

The authoritative checkpoint, commit lineage, exact eight-file change boundary, source hashes, all 23 A4 tests, and all 10 documented production mutants were independently verified. The reproduced offline build and test evidence passes.

The architecture/security decision is nevertheless **FAIL**. Passing direct state-machine tests and the documented mutant set do not establish a usable or safe production A4 path. Five findings remain: two critical, two high and one medium. The committed raw ingress cannot reach the three new A4 operations through the lifecycle authority it calls; no production durable/target/reconciliation implementation consumes the A4 contracts; lease acquisition accepts an expired grant and can issue a lease beyond grant expiry; ingress reconstructs rather than preserves authoritative A4 grant/plan/lease provenance; and terminal reconciliation replay returns before reconciler authorization is checked.

No PostgreSQL execution, connection, migration application, provisioning, deployment, production access, Phase B or Correction 2 action occurred.

## 2. Boundary and checkpoint verification

| Gate | Independent result |
|---|---|
| Branch / entry HEAD | `master` / exact `1440f7736bb2870ca3175e669b9d8699d33a7808` |
| Parent | exact `a126de9d2ab9efe90490d6a1734aac320bab5f04` |
| Subject | `REV869B Phase-A Correction A4 source implementation` |
| Commit date | `2026-08-20T16:15:22+05:30` |
| Parent content | exactly the A4 architecture-freeze report |
| Architecture-freeze SHA-256 | exact `2DBC7293840F6BC2613EB3A3D473D28D848E7A8364F3BBA8361BAEF7C37A56C5` |
| Implementation checkpoint SHA-256 | `0FD4CA1C4545CBB1D1F225E5E96FFCD433EA4CD94AF9FBA4A7FD51B8F8F2C908` |
| Commit boundary | exactly seven implementation/test files plus the checkpoint |
| Entry worktree | target clean; only pre-existing untracked sibling `../legacy-reference/` |
| Legacy boundary | not opened, enumerated, read, modified or used |
| Committed diff check | PASS |

The seven implementation/test SHA-256 values independently match the checkpoint:

| File | SHA-256 |
|---|---|
| `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs` | `B39F178AC58B76221B85FC1A32A5639D5599712F42CE7A45C10C869919CD9D0C` |
| `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs` | `5BEFE3F342E6BC8B5F928C038C85EDCDD38B642D58FF4BEEF7B6A86FE85B020D` |
| `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs` | `2F6BDFD77EFACE2442884C683182F6B81CF20AB9A3AA7C68B01EA9A631E76264` |
| `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs` | `58D21C840ABD797A5CA9C041B424AA900B605BCA4977466C82492BEFB11EEAF4` |
| `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs` | `C2462EF3F95BE484BAA8B281344C0EEA851C0D92D4EEF1B0CF3BC94B2401F672` |
| `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs` | `1EA332D33342C735E6865CE9EFFEEFA3E3B8BBC930005A32124EB937F088D55B` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | `5A21AF4DF6B92B1E871430AD44EED2F213E09E5345B29A4D835E898DE2D17DB7` |

## 3. Findings

### A4-IR-F01 - CRITICAL - Raw production ingress cannot reach the new A4 operations

The raw authority always invokes `lifecycleController.TransitionAsync` before it calls `durableProvider.ExecuteAtomicallyAsync` and supplies `A4Operation` (`SignedEnvelopeService.cs:982-1011`). The lifecycle authority resolves commands exclusively through `PhaseARules` (`Rev869BControllerStateMachine.cs:262-269`). That rule table has no entry for `ACQUIRE_EXECUTION_LEASE`, `BEGIN_EXECUTE_AUTHORIZED_PLAN` or `RECONCILE_TERMINAL_RESULT`; the only production references to those names outside the enum are the A4 request builder (`Rev869BControllerStateMachine.cs:44-151`; `SignedEnvelopeService.cs:664-706`).

Therefore each new operation is rejected as `STATE_TRANSITION_ILLEGAL` before the composite durable A4 request can execute. A disposable reviewer-only test confirmed all three operations are absent from `PhaseARuleSnapshot`.

Impact: the claimed authorization -> lease -> begin -> target -> reconciliation production path is unreachable through the protected raw ingress. Direct tests of `Rev869BL1BoundaryStateMachine` do not exercise this ordering.

Required correction: freeze and implement one coherent dispatch model. Either the lifecycle authority must explicitly and exhaustively own the A4 transitions, or raw ingress must route the high-level A4 transaction without first requiring an incompatible legacy transition. Add raw-path tests for the complete authorize/acquire/begin/reconcile sequence and every denial/replay edge.

### A4-IR-F02 - CRITICAL - Atomic target execution and authoritative reconciliation are contracts/test fakes, not a committed production path

`IDurableControlPlanePersistenceProvider`, `ITargetAuthorizedPlanExecutionProvider` and `IAuthoritativeTargetResultProvider` are interfaces only (`Rev869BControllerMessagesV1.cs:1647-1671`). No committed production class implements the durable A4 operation, target-local transaction or authoritative target-result read. `ITargetAuthorizedPlanExecutionProvider` and `IAuthoritativeTargetResultProvider` have no production consumer.

The reconciliation builder creates `A4CompositeOperationKindV1.ReconcileTerminalResult` with only a canonical digest; `A4CompositeOperationRequestV1.Reconciliation` and its authoritative target result remain null (`SignedEnvelopeService.cs:681-686`). `PhaseAControlPlaneAuthority` does not receive either target interface. Consequently the source has no route that reads an immutable target result and invokes `ReconcileTerminalResult` without business re-execution.

Atomicity evidence comes from `AtomicA4Target`, a test-local counter object, and acquisition serialization comes from `LockedA4Store`, a process-local `lock` (`ArchitectureFreezeContractTests.cs:1976-2024`). These tests are useful unit contracts but do not implement or prove a serializable durable-provider transaction, a target database transaction, target audit/outbox atomicity, or cross-store reconciliation.

Impact: source cannot substantiate the checkpoint claims for atomic grant reservation/fence allocation, target-local exactly-once business mutation, durable terminal-result discovery or result-only reconciliation.

Required correction: implement or explicitly scope and freeze the production adapters. The durable provider must consume `A4Operation` atomically and return a checked `A4Result`; the target provider must enforce fence/idempotency/business/audit/outbox in one local transaction; reconciliation must read through the pinned authoritative result provider and pass a complete request. Add integration-contract tests against the real adapters without PostgreSQL execution at this gate.

### A4-IR-F03 - HIGH - Lease acquisition and renewal do not enforce the authorization grant time window

`AcquireExecutionLease` checks executor/issuer separation, grant identity, plan equality, lifecycle state, lease request expiry and fence monotonicity, but never checks `grant.NotBefore <= now`, `grant.ExpiresAt >= now`, or `request.RequestedExpiresAt <= grant.ExpiresAt` (`Rev869BControllerStateMachine.cs:729-767`). `RenewExecutionLease` likewise allows an extension beyond the grant expiry (`Rev869BControllerStateMachine.cs:678-705`). Target execution checks lease expiry but not the grant validity window (`Rev869BControllerStateMachine.cs:650-676`).

A disposable reviewer-only test constructed an already expired `ACTIVE` grant and proved that the production state machine returns `LeaseActive` with a lease expiring after the grant.

Impact: if expiry materialization lags or a stale `ACTIVE` grant is supplied, execution authority can be created or extended after management authorization has expired.

Required correction: validate not-before and expiry inside the same authoritative lease transaction, cap lease/renewal expiry to the grant expiry, and revalidate the signed grant/revocation attestation at target begin/commit. Add negative tests and mutants for not-yet-valid, expired, revoked and lease-outlives-grant cases.

### A4-IR-F04 - HIGH - Raw ingress reconstructs and changes authoritative A4 provenance instead of reading the committed grant and lease

`BuildA4Operation` reconstructs `A4ExecutionPlanBindingV1` on every command with `PlanVersion = snapshot.ResourceVersion` and reconstructs the grant with `AuthorizerWorkloadIdentity = transport.WorkloadIdentity` (`SignedEnvelopeService.cs:638-659`). Resource version advances after authorization, so a later acquire/begin request does not preserve the originally authorized plan version. On acquire, transport is the executor rather than the original management authorizer. On begin, the lease receipt is reconstructed with `AcquisitionRequestId = source.LeaseId` and `AcquisitionRequestSha256 = grant.AuthorizationRequestSha256`, not the original acquisition identity/digest (`SignedEnvelopeService.cs:688-705`).

The authoritative snapshot contract contains only the legacy `StoredAuthorizationGrantV3` and `LeaseFenceExpectationV3`, not the committed `A4AuthorizationGrantV1`/`A4ExecutionLeaseReceiptV1` needed to preserve these facts (`Rev869BControllerMessagesV1.cs:1235-1251`). Direct A4 tests bypass this adapter by constructing a stable plan and lease in helpers.

Impact: exact A4 equality can fail across legitimate transitions, or an adapter may be tempted to accept reconstructed fields that are not the original durable authorization/acquisition facts. Either outcome violates immutable plan/grant/lease provenance and weakens idempotency.

Required correction: extend the authoritative snapshot/result contract to return the exact committed A4 grant, lease, lifecycle and acquisition receipt. Treat raw fields as comparison-only. Never derive a new plan version/authorizer/acquisition digest during acquire or begin. Add raw-path provenance-substitution and version-advance tests.

### A4-IR-F05 - MEDIUM - Terminal reconciliation replay bypasses reconciler authorization

`ReconcileTerminalResult` checks for an existing terminal result and returns `COMPLETED_REPLAY` before validating `request.ReconcilerWorkloadIdentity == expectedReconciler` (`Rev869BControllerStateMachine.cs:581-600`). A disposable reviewer-only test completed one valid reconciliation, changed the request reconciler to `intruder`, changed the expected reconciler, and still received the stored terminal replay.

Impact: if this public boundary method is called by a future adapter without a stronger external check, an unauthorized caller with the replay identity/digest/result can retrieve the control outcome. This also makes authorization semantics differ between first-owner and replay paths.

Required correction: authenticate and bind the reconciler before both first-owner and replay handling. Return the stored result only after exact caller/authority and replay identity checks. Add a dedicated replay-authorization test and mutant.

## 4. A4 test verification

Exactly 23 unique literal A4 methods were discovered. The aggregate filter passed 23/23, and every method passed again in a separate exact fully-qualified invocation:

1. `A4_AuthorizationCreatesExactPlanExecutorGrantAndNoLease`
2. `A4_OnlyGrantBoundExecutorCanAcquireExecutionLease`
3. `A4_PlanIdVersionHashSubstitutionFailsBeforeLeaseOrLifecycle`
4. `A4_AuthorizerExecutorOrLeaseIssuerSubstitutionFailsClosed`
5. `A4_ExactLeaseAcquisitionReplayReturnsOriginalReceipt`
6. `A4_ConflictingLeaseAcquisitionReplayIsDenied`
7. `A4_ConcurrentLeaseAcquisitionHasExactlyOneWinner`
8. `A4_ExecutionBeforeAuthoritativeLeaseIsDenied`
9. `A4_StaleOrExpiredLeaseIsDeniedBeforeTargetMutation`
10. `A4_StaleFenceCannotCommitAtTarget`
11. `A4_RenewalRetainsFenceAndExtendsOnlySameBinding`
12. `A4_ReacquisitionRequiresNoResultProofAndGreaterFence`
13. `A4_TargetRollbackLeavesNoPartialBusinessResultAuditOrOutbox`
14. `A4_CommittedTargetResultSurvivesLostAcknowledgement`
15. `A4_ReconciliationNeverReexecutesBusinessMutation`
16. `A4_ExactExecutionReplayReturnsOriginalTargetAndControlResult`
17. `A4_ConflictingExecutionReplayIsDenied`
18. `A4_AuditOrOutboxFailureRollsBackItsLocalOperation`
19. `A4_ReaderMismatchIsRejectedBeforeReadAsync`
20. `A4_PreflightMismatchHasZeroReaderOracleLifecycleAndAtomicCalls`
21. `A4_F01RawIngressAndCanonicalTrustRegressionIsPreserved`
22. `A4_F02CompositeProviderHasNoPartialMutationCapability`
23. `A4_F05ReadinessAuditPrivacyAndEvidenceRegressionIsPreserved`

The tests pass as written. Their principal assurance limitation is scope: tests 1-18 call the pure A4 state machine and test-local stores directly; they do not pass the new operations through `PhaseAControlPlaneAuthority`, the legacy lifecycle authority and a production durable/target/reconciliation adapter. The ERP source-contract test's mutant arithmetic is a hard-coded ten-label count and is not executable mutation evidence by itself (`Rev869BCorrection17SourceContractTests.cs:529-546`).

## 5. Independent production-mutant reproduction

Each documented mutation was applied only to a disposable temp copy. Each mutant changed production code, built with warnings as errors, and ran only its intended killer test(s). All temp copies were removed. The authorized source hashes were rechecked afterward and remained exact.

| Mutant | Independent result |
|---|---|
| A4-M01 authorization populates a lease | compiled; killed by test 1 |
| A4-M02 grant executor comparison accepts substitution | compiled; killed by tests 2/4 |
| A4-M03 plan version/hash comparisons omitted | compiled; killed by test 3 |
| A4-M04 exported partial `ILeaseSetter` facet | compiled; killed by test 22 |
| A4-M05 begin accepts caller job without authoritative lease | compiled; killed by test 8 |
| A4-M06 both target watermark/fence comparisons removed | compiled; killed by test 10 |
| A4-M07 reconciliation remains `Executing` | compiled; killed by test 15 |
| A4-M08 acquisition/execution replay digest checks bypassed | compiled; killed by tests 6/17 |
| A4-M09 authorization audit/outbox guard removed | compiled; killed by test 18 |
| A4-M10 reader-version preflight comparison removed | compiled; killed by test 19 |

`A4_mutants_total=10`

`A4_mutants_compiled=10`

`A4_mutants_killed=10`

`A4_mutants_survived=0`

One preliminary campaign stopped after valid M01-M04 results because the reviewer mutation selector for M05 matched both begin and renewal source blocks. The temp copy was removed. M05 was narrowed to the named begin method and M05-M10 were then reproduced successfully. No ambiguous mutation is counted.

The mutant campaign validates its ten selected assertions. It does not include mutants for missing lifecycle routing, absent production providers, expired/not-yet-valid grants, lease expiry beyond grant expiry, reconstructed provenance, null reconciliation input or replay reconciler authorization. Those omissions align with the findings above.

## 6. Offline validation reproduced

| Gate | Independent result |
|---|---|
| Affected control-plane build, warnings as errors | PASS: 0 warnings, 0 errors |
| Repository solution build, warnings as errors | PASS: 0 warnings, 0 errors; the solution does not include control-plane projects, so the affected build was run separately |
| Exact A4 aggregate | PASS: 23/23 |
| Exact A4 individual invocations | PASS: 23/23 |
| Complete Phase-A control assembly | PASS: 86/86 |
| Complete ERP non-PostgreSQL assembly | PASS: 455/455 |
| Canonical offline SQL subset | PASS: 3/3 |
| Model/snapshot and A4 no-connect counter subset | PASS: 2/2 |
| A4 source-contract subset | PASS: 2/2 |
| Reviewer-only finding proofs in disposable copy | PASS: 3/3 demonstrated F01, F03 and F05; temp removed |
| Production mutants | PASS as mutation evidence: 10 compiled, 10 killed, 0 survived |
| EF migration discovery | PASS: 13 migrations, REV869A then REV869B, `--no-connect`, applied status intentionally unknown |
| PostgreSQL test discovery | PASS: 87 discovered, 0 executed |
| PowerShell AST | PASS: 24 scripts, 0 parse errors, 0 scripts executed |
| Source hashes after temp campaigns | PASS: all seven exact |
| Git diff checks | PASS before report |

Observed prohibited-operation counters:

`database_connection_open_count=0`

`migration_application_attempt_count=0`

`migration_application_completed_count=0`

`postgresql_test_execution_count=0`

`powershell_script_execution_count=0`

No restore/download was run. Existing restored dependencies were used with `--no-restore`. No service was started and no endpoint was contacted.

## 7. Retained states and exact next gate

`phase_a_correction_a4_source_implementation_state=FAIL_INDEPENDENT_REVIEW`

`phase_a_management_acceptance_state=FAIL`

`rev869b_source_safety_state=FAIL`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

Exact next gate: reconcile these five findings and obtain explicit authorization for one bounded source-only correction with a frozen allowlist. That correction must add raw-path and real-adapter contract tests plus the missing grant-validity/replay mutants. After the correction, require a new independent report-only source architecture/security review. Do not authorize PostgreSQL, infrastructure provisioning, deployment, production access, Phase B or Correction 2 from this report.
