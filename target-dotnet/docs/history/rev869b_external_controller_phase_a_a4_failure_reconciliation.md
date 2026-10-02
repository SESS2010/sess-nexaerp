# REV869B Option-A Phase-A Correction A4 failure reconciliation

Date: 2026-08-21

Reconciliation type: separate report-only failure reconciliation

Authoritative review commit: `0474169e379b2a1cd7aef800fabd225114d01fc8`

Reviewed implementation commit: `1440f7736bb2870ca3175e669b9d8699d33a7808`

Authoritative review report: `outputs/rev869b_external_controller_phase_a_a4_independent_review.md`

Review-report SHA-256: `C7BCB87EC7CBF63DEC4CB0B514E69641188127ED3509AED01035BC5A341DAD4D`

## 1. Reconciliation decision

`PHASE_A_CORRECTION_A4_FAILURE_RECONCILIATION=COMPLETE`

`PROPOSED_PHASE_A_CORRECTION_A5_SOURCE_ONLY_GATE=AWAITING_MANAGEMENT_AUTHORIZATION`

All five independent-review findings are accepted and reconciled into one bounded corrective design. None is dismissed, downgraded or declared closed. This report defines the smallest exhaustive boundary that can close the findings in source; it does not authorize or begin implementation.

The original eight-file A4 boundary is insufficient. Finding F02 cannot be closed by adding more interfaces or test fakes. The correction must include concrete control-plane persistence, target-local execution/result persistence, protected host wiring and an additive target schema. The correction remains source-only: no PostgreSQL access, migration application, infrastructure provisioning, service deployment or production action is part of the proposed gate.

## 2. Entry boundary

| Gate | Reconciled state |
|---|---|
| Entry HEAD | exact `0474169e379b2a1cd7aef800fabd225114d01fc8` |
| Entry parent | exact `1440f7736bb2870ca3175e669b9d8699d33a7808` |
| Entry subject | `REV869B Phase-A A4 independent review` |
| Review commit content | exactly the independent review report |
| Review verdict | `FAIL`: two critical, two high, one medium |
| Target status | clean at entry |
| Legacy boundary | pre-existing `../legacy-reference/` remains untracked and was not opened, enumerated, read, modified or used |

## 3. Reconciled architecture decision

The proposed correction label is **Phase-A Correction A5**. It preserves Option L1 and makes its production path explicit:

`raw protected command -> one A4 dispatcher -> serializable control transaction -> durable control outbox -> target signed-job endpoint -> one target-local transaction -> immutable target result -> pinned read-only target-result client -> serializable control reconciliation transaction`

The following rules are frozen for the proposed correction:

1. The raw authority performs canonical envelope, signature, workload identity, role, scope, freshness, nonce, request digest, plan and evidence checks before any durable mutation.
2. `Authorize`, `AcquireExecutionLease`, `BeginExecuteAuthorizedPlan` and `ReconcileTerminalResult` use one A4 lifecycle dispatcher. The three new operations must not be passed through a legacy rule table that cannot recognize them.
3. The durable control-plane provider is one concrete, non-decomposable owner. It performs snapshot read plus one serializable composite mutation; no public grant, lease, fence, nonce, lifecycle, audit or outbox setter exists.
4. `Authorize` stores one exact immutable A4 grant/plan. Later commands read that exact record; they never reconstruct it from current resource version or caller/transport fields.
5. Lease acquisition validates exact grant identity/version/state, not-before, expiry, revocation, plan, authorizer, executor, policy/evidence bindings and lifecycle version under the same lock/transaction that reserves the grant, allocates the monotonic fence, writes the lease, lifecycle, response, audit and outbox.
6. A lease or renewal may not expire after its grant. Begin and target commit revalidate the grant/lease attestation and deny expired or revoked authority.
7. The target provider validates the signed immutable job, exact grant/lease/plan/acquisition provenance and fencing watermark before invoking the server-owned action handler.
8. Business mutation, business/history rows, target fencing watermark, idempotency reservation/result, target audit and target outbox commit in one target transaction or all roll back.
9. An equal fence is accepted only for exact replay of the same execution identity and digest. A lower fence, or equal fence with any different binding/digest, fails before the action handler.
10. Reconciliation accepts no caller-supplied terminal truth. It reads through the pinned read-only authoritative target-result provider, rejects missing/duplicate/ambiguous/conflicting/stale facts, and never calls target execution.
11. Reconciler identity/role/scope is checked before both first-owner and replay responses. Exact replay returns the stored result only to the same authorized reconciliation authority.
12. Control and target databases remain separate local transactions. No distributed ACID claim is introduced.
13. The existing REV869B migration and accepted command-ledger SQL remain immutable. Any target additions use one new forward migration containing raw SQL only; no accepted migration rewrite is permitted.
14. No schema is installed automatically by the host. Startup readiness validates exact schema/ACL fingerprints and fails closed. External provisioning and migration execution remain separate future gates.

## 4. Finding-by-finding reconciliation

### A4-IR-F01 - unreachable A4 raw ingress operations

Reconciliation: **accepted; requires one dispatcher and host-path correction**.

Required enforcement points:

- `PhaseAControlPlaneAuthority.AcceptRawCommandAsync` must select one path: legacy operations use the retained legacy lifecycle controller; A4 operations use the A4 dispatcher. An A4 operation may not call the legacy transition first.
- The A4 dispatcher must accept only the typed high-level operation produced after raw verification and must call the composite durable provider exactly once.
- Every operation kind is exhaustive. Unknown, null, mismatched or multiply populated variants fail before provider mutation.
- Controller host wiring exposes one protected raw-command route only. No public typed lease, fence, reconciliation or target-mutation endpoint is allowed.

Closure evidence: tests A5-01 through A5-04 and mutants A5-M11/A5-M12/A5-M28/A5-M29.

### A4-IR-F02 - missing durable target execution and reconciliation implementation

Reconciliation: **accepted; concrete production adapters and additive schema are mandatory**.

Required enforcement points:

- Add one concrete Npgsql-backed durable control-plane provider that consumes `A4Operation`, uses a serializable transaction and row/advisory locks, and returns a checked `A4Result`.
- Add one explicit control-plane schema/ACL source artifact for grants, plans, leases, scoped fence sequence, lifecycle, nonce/idempotency, dispatch, reconciliation attempts, control audit and control outbox. The host validates but does not auto-install it.
- Add one concrete target-local provider in ERP Infrastructure. It consumes a signed immutable job and invokes a server-owned action-handler registry inside the target transaction.
- Add one raw-SQL-only forward target migration for the A4 fencing watermark, execution identity/digest, immutable terminal result, target audit and target outbox enforcement. It must not modify the accepted REV869B migration.
- Add one pinned read-only target-result client and reconciliation service in the control-plane host. The result client cannot execute business mutation and has no target write capability.
- Wire exact concrete identities/artifact hashes/roles/connection endpoints through validated options and dependency injection. Null, in-memory, fake or unpinned production registrations fail readiness.

Closure evidence: tests A5-05 through A5-11, A5-17 through A5-22 and mutants A5-M13 through A5-M19, A5-M25/A5-M26/A5-M28 through A5-M30.

### A4-IR-F03 - grant and lease expiry conflict

Reconciliation: **accepted; validity is enforced at every authority boundary**.

Required enforcement points:

- Acquisition requires `grant.NotBefore <= serverNow < grant.ExpiresAt`, state `ACTIVE`, no revocation and `RequestedExpiresAt <= grant.ExpiresAt`.
- Renewal requires the same exact binding, an unexpired/unrevoked grant and `RequestedExpiresAt <= grant.ExpiresAt`; renewal retains the fence.
- Begin requires the committed lease and grant to remain valid at server time.
- Target begin and pre-commit checks require the signed grant/lease attestation to remain valid. Expiry/revocation cannot be hidden by a stale job.
- Expiry/reacquisition requires authoritative no-result proof and a greater fence; it may never reactivate an expired/revoked grant.

Closure evidence: tests A5-12/A5-13 and mutants A5-M20/A5-M21/A5-M22.

### A4-IR-F04 - reconstructed grant/plan/lease provenance

Reconciliation: **accepted; exact committed provenance becomes part of the authoritative snapshot**.

Required enforcement points:

- Extend the authoritative snapshot with the exact committed A4 state: immutable grant and plan, lease receipt, dispatch, terminal result reference, lifecycle version and cardinalities.
- `BuildA4Operation` may construct a grant only during authorization. Acquire, begin and reconcile read the committed grant/lease and compare caller/transport fields; they do not create replacements.
- Plan version is the management-approved plan version, not the current lifecycle resource version.
- Management authorizer is retained from the stored grant and never overwritten by the executor transport identity.
- Acquisition request ID/digest, lease ID/version, fence and controller epoch are copied exactly from the durable receipt.
- Missing, duplicate or inconsistent A4 snapshot rows fail before lifecycle, target execution or reconciliation.

Closure evidence: tests A5-14 through A5-16 and mutants A5-M23/A5-M24/A5-M25.

### A4-IR-F05 - reconciler authorization missing on replay

Reconciliation: **accepted; authorization precedes replay lookup/return**.

Required enforcement points:

- The reconciliation service authenticates the pinned reconciler workload, role, scope and target before reading or returning any target/control result.
- The state machine checks `request.ReconcilerWorkloadIdentity == expectedReconciler` before the terminal-result replay branch.
- Replay requires the original reconciliation identity/digest, target result digest and caller authority. A changed reconciler receives denial with no state change and no result disclosure.
- Reconciliation audit records first-owner, replay and denied replay without duplicating terminalization.

Closure evidence: tests A5-18/A5-19 and mutant A5-M27.

## 5. Smallest exhaustive correction boundary

The proposed A5 implementation may add or modify only the following **25 paths**. New paths are marked `NEW`. This is a maximum allowlist, not an instruction to touch every path unnecessarily.

1. `SESS.NexaERP.slnx`
2. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
3. `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj`
4. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
5. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
6. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
7. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
8. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
9. `src/SESS.NexaERP.ControlPlane/Program.cs`
10. `src/SESS.NexaERP.ControlPlane/Persistence/Rev869BA4ControlPlaneSchemaV1.cs` - **NEW**
11. `src/SESS.NexaERP.ControlPlane/Persistence/NpgsqlA4DurableControlPlanePersistenceProvider.cs` - **NEW**
12. `src/SESS.NexaERP.ControlPlane/Reconciliation/PinnedA4TargetResultProvider.cs` - **NEW**
13. `src/SESS.NexaERP.ControlPlane/Reconciliation/A4TerminalResultReconciliationService.cs` - **NEW**
14. `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj`
15. `src/SESS.NexaERP.Infrastructure/DependencyInjection.cs`
16. `src/SESS.NexaERP.Infrastructure/Persistence/NpgsqlA4TargetExecutionProvider.cs` - **NEW**
17. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BA4TargetExecutionBoundarySql.cs` - **NEW**
18. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.cs` - **NEW**
19. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.Designer.cs` - **NEW**
20. `src/SESS.NexaERP.Api/Program.cs`
21. `src/SESS.NexaERP.Api/Endpoints/Rev869BA4TargetExecutionEndpoints.cs` - **NEW**
22. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
23. `tests/SESS.NexaERP.ControlPlane.Tests/A4FailureCorrectionContractTests.cs` - **NEW**
24. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
25. `outputs/rev869b_external_controller_phase_a_a5_checkpoint.md` - **NEW**

Boundary rules:

- The accepted REV869B migration primary/designer, current snapshot, `Rev869BCommandContextSql.cs`, Purchase application/domain source, Acceptance Verifier source, PowerShell helpers and all prior reports are immutable.
- The new migration is raw-SQL-only and therefore must not alter the EF model snapshot. If a model/snapshot change is required, stop and obtain a new architecture/boundary freeze.
- No new public partial mutation interface, alternate provider, second lifecycle owner, generic caller-selected handler, credential source or auto-schema installer is permitted.
- Any additional file, project reference, package, schema object, endpoint or responsibility is an immediate stop condition requiring a new report-only boundary decision.

## 6. Exact required tests

The proposed correction must add exactly these 22 literal `A5_` tests. Looped substitution matrices may exist inside a named test but may not inflate the unique count.

1. `A5_RawAuthorizeAcquireBeginTargetReconcileUsesOneCanonicalProductionPath`
2. `A5_EachNewOperationReachesCompositeProviderWithoutLegacyLifecycleRejection`
3. `A5_RawCanonicalIdentityRoleScopeFreshnessAndDigestDenialsHaveZeroDurableCalls`
4. `A5_UnknownNullOrMultiplyPopulatedA4OperationFailsBeforeMutation`
5. `A5_DurableAuthorizeCommitsGrantLifecycleAuditOutboxAndReplayAtomically`
6. `A5_DurableAcquireHasOneSerializableWinnerAndCommitsReservationFenceLeaseAuditOutboxAtomically`
7. `A5_DurableBeginCommitsDispatchLifecycleAuditOutboxAndRetainsReservedGrantAtomically`
8. `A5_NoPublicOrInjectablePartialGrantLeaseFenceLifecycleNonceAuditOrOutboxMutationExists`
9. `A5_TargetCommitIncludesBusinessHistoryWatermarkResultAuditAndOutboxInOneTransaction`
10. `A5_TargetRollbackLeavesEveryTargetRelationAndWatermarkUnchanged`
11. `A5_StaleFenceOrEqualFenceDifferentDigestFailsBeforeActionHandler`
12. `A5_ExpiredNotYetValidOrRevokedGrantCannotAcquireRenewBeginOrCommit`
13. `A5_LeaseAndRenewalCannotOutliveGrantAndReacquisitionRequiresGreaterFenceAndNoResultProof`
14. `A5_AuthoritativeSnapshotReturnsExactlyOneCommittedGrantPlanLeaseDispatchAndLifecycle`
15. `A5_ResourceVersionAdvanceNeverReconstructsPlanAuthorizerAcquisitionIdentityOrDigest`
16. `A5_EveryPlanGrantLeaseFenceAuthorizerExecutorAndPolicySubstitutionFailsBeforeMutation`
17. `A5_ReconciliationReadsOnlyPinnedAuthoritativeTargetResultAndNeverCallsTargetExecution`
18. `A5_MissingDuplicateAmbiguousConflictingOrStaleTargetResultQuarantinesWithoutConsumption`
19. `A5_ReconciliationFirstOwnerAndReplayBothRequireExactReconcilerIdentityRoleAndScope`
20. `A5_ExactTargetAndControlReplayReturnOriginalResultsWithoutBusinessReexecutionOrVersionChange`
21. `A5_HostsResolveOnlyPinnedConcreteProvidersAndExposeOnlyProtectedRawInternalRoutes`
22. `A5_OfflineControlAndTargetSchemaUpDownAclFingerprintBoundaryAndNoConnectEvidenceAreExact`

Retained requirements:

- All 23 A4 tests must remain present and pass.
- All prior Phase-A control tests and the complete non-PostgreSQL ERP suite must pass.
- Raw endpoint tests must use the production dependency graph, not direct construction of the state machine, `LockedA4Store`, `AtomicA4Target`, an in-memory provider or a fake production registration.
- Relational transaction tests may use an offline deterministic command/connection harness, but final PostgreSQL behavior remains a later separately authorized gate.

## 7. Exact required production mutants

All original A4-M01 through A4-M10 mutants must be rerun and killed. The correction must additionally compile and kill these 20 production mutants in disposable copies:

| ID | Required production mutation | Intended killer |
|---|---|---|
| A5-M11 | A4 operation is sent through the legacy lifecycle transition first | A5-02 |
| A5-M12 | `A4Operation` is omitted/ignored before the composite durable call | A5-01/A5-02 |
| A5-M13 | Export a partial grant/lease/fence/state setter | A5-08 |
| A5-M14 | Split grant reservation, fence allocation, lease, audit or outbox into separate control commits | A5-06 |
| A5-M15 | Replace database winner/unique locking with a process-local lock | A5-06 |
| A5-M16 | Commit target business/history before terminal result/watermark | A5-09/A5-10 |
| A5-M17 | Move target audit or outbox outside the target transaction | A5-09/A5-10 |
| A5-M18 | Remove stale-fence or equal-fence/digest enforcement | A5-11 |
| A5-M19 | Bypass exact target/control replay digest comparison | A5-20 |
| A5-M20 | Permit not-yet-valid, expired or revoked grant acquisition/begin | A5-12 |
| A5-M21 | Permit lease or renewal expiry after grant expiry | A5-13 |
| A5-M22 | Permit reacquisition without no-result proof or without a greater fence | A5-13 |
| A5-M23 | Rebuild plan version from lifecycle resource version | A5-15 |
| A5-M24 | Replace stored management authorizer with executor transport identity | A5-15/A5-16 |
| A5-M25 | Replace stored acquisition request ID/digest with lease or authorization values | A5-15/A5-16 |
| A5-M26 | Trust a caller-carried terminal result instead of the pinned result provider | A5-17/A5-18 |
| A5-M27 | Reconciliation invokes target execution on missing result | A5-17 |
| A5-M28 | Move reconciler authorization after the terminal replay return | A5-19 |
| A5-M29 | Register a null, fake, in-memory, duplicate or unpinned production provider | A5-21 |
| A5-M30 | Grant direct table DML or bypass signed-job checks on the target internal route | A5-21/A5-22 |

Required arithmetic:

`retained_A4_mutants=10`

`new_A5_mutants=20`

`total_required_mutants=30`

`compiled_required_mutants=30`

`killed_required_mutants=30`

`survived_required_mutants=0`

Invalid, non-compiling or test-only mutations do not count. Every temp copy must be removed and source equality reverified after each restore.

## 8. Offline validation required from the proposed correction

The future correction checkpoint must record:

- exact entry HEAD/parent/subject and exact 25-path allowlist result;
- warning-as-error build of both host graphs and `SESS.NexaERP.slnx`, with the control-plane projects now included in the solution;
- exact 22/22 A5 tests, each invoked individually;
- retained 23/23 A4 tests and complete Phase-A control assembly;
- complete non-PostgreSQL ERP suite;
- 30/30 compiled and killed production mutants;
- deterministic offline control-schema and target-migration Up/Down SQL bytes, lines and SHA-256 from two fresh processes;
- exact schema object, ownership, grant and revocation inventory;
- EF `--no-connect` discovery with the new migration exactly once after REV869B and zero connection opens;
- model/snapshot parity proving the raw-SQL-only migration creates no pending model change;
- PowerShell AST parse only, secret/privacy/prohibited-operation scans and `git diff --check`;
- zero database connections, zero migration applications, zero PostgreSQL test executions, zero PowerShell script executions and zero service starts;
- source/executable hashes and deletion of every temporary artifact.

No package download is authorized by this report. If an exact already-cached provider package cannot satisfy the pinned build, stop and request a separate dependency decision.

## 9. Stop conditions

Implementation must stop without editing if any of these is true:

1. entry HEAD is not this reconciliation commit or its single report-only successor expected by management;
2. the worktree has a target-scoped change other than the authorized correction;
3. any path outside the exact allowlist is required;
4. the accepted REV869B migration, snapshot, command SQL, Acceptance Verifier or Purchase business source would need modification;
5. target execution cannot identify a server-owned action handler from the immutable plan without caller-selected code/type/input;
6. concrete control or target transactions cannot be implemented without a second lifecycle owner or partial mutation capability;
7. additive schema cannot prove exact Up/Down, ACL closure and no direct runtime DML;
8. host authentication cannot bind the workload identity before raw command/target job processing;
9. any test requires PostgreSQL, network, infrastructure, credentials, service deployment or production access;
10. any mutant survives or is invalid/non-compiling;
11. any retained A4/Phase-A/non-PostgreSQL regression fails;
12. any secret, private key, connection password or production identity would enter source, command arguments, logs or evidence.

## 10. Prohibited actions and retained states

This reconciliation performed no source edit, PostgreSQL connection/test, migration application/rollback, infrastructure provisioning, service deployment/start, production access, credential/key use, Phase B, Correction 2, recovery, purge, export or legacy-reference access.

`phase_a_correction_a4_review_state=FAIL_RECONCILED_NOT_CLOSED`

`phase_a_correction_a5_authorization_state=NOT_AUTHORIZED`

`phase_a_management_acceptance_state=FAIL`

`rev869b_source_safety_state=FAIL`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

## 11. Exact next management gate

Management must decide whether to authorize **one bounded REV869B Option-A Phase-A Correction A5 source-only implementation** against the exact 25-path maximum allowlist, 22 named tests, retained 23 A4 tests and 30 total production mutants defined in this report.

Approval of that gate authorizes source changes and offline validation only. It does not authorize PostgreSQL, migration execution, infrastructure provisioning, service start/deployment, production access, Phase B or Correction 2. If management does not explicitly approve the A5 gate, no correction starts.
