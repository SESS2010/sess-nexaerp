# REV869B Phase-A A5 mutant-gate failure reconciliation

## 1. Decision

`A5_REVISED_MUTANT_GATE=GO`

This is a report-only reconciliation. It authorizes no source change and performs no build, database, migration, service, deployment, Phase-B, Correction-2, infrastructure, or production operation. The only permitted change in this commit is this report.

The gate is `GO` because A5-M18 and A5-11 are replaced below by a behaviorally decisive fence contract, and every A5-M19 through A5-M40 mutation is reduced to one concrete, compilable production transformation with a distinct security property and an authoritative intended killer. All 40 mutants must nevertheless be executed afresh by a later, separately authorized correction; no prior mutant observation is accepted as completion evidence.

## 2. Authoritative entry and evidence boundary

- Required and observed entry commit: `4e1a308ed12b4e3d8a092a3853b37b1b6b6e6677`.
- Entry parent: `fe704bd65879cb1b3fc64193d9050387834144e3`.
- Entry subject: `REV869B Phase-A A5 mutant gate blocker checkpoint`.
- Authoritative immediate checkpoint: `outputs/rev869b_external_controller_phase_a_a5_implementation_checkpoint.md`.
- Observed checkpoint SHA-256: `9C2F4FA3FA1B4D1FEB33E776C2B3908D26A6BEB61A2A06304CEBFB034D3DD3A1`.
- The entry target-scoped worktree was clean before this report was created.
- The A4 architecture freeze, A4 failure reconciliation, A5 boundary/immutable plan-contract decision, and A5 blocker checkpoint were read as governing evidence. Earlier reports and checkpoints remain immutable.
- `../legacy-reference/` was not accessed.

The blocker checkpoint is accepted as an honest failure record: A5-M18 merely renamed a PostgreSQL exception token, so it did not remove a security decision and was invalid; A5-11 used an `IndexOf` ordering expression for which a missing marker produced `-1`, so the assertion could pass without proving the fence gate. M19-M40 were not executed.

## 3. Actual stale-fence enforcement point

The authoritative in-process production decision already exists in:

`src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`, `Rev869BL1BoundaryStateMachine.RequireTargetExecution`.

At the entry commit its decisive statements are:

- line 664: submitted fence must be greater than or equal to the target watermark;
- lines 665-668: when no terminal result exists, submitted fence must be strictly greater than the watermark;
- lines 670-674: when a terminal result exists, execution ID, request digest, grant, lease, equal fence, and plan must all match before replay.

Those comparisons—not an SQL error-message token—are the current semantic fence enforcement. Revised A5 shall retain this method as the single C# fence classifier. The target SQL boundary must lock and return the authoritative watermark and any committed result as facts; it must not substitute error-token parsing for the classifier. `NpgsqlA4TargetExecutionProvider` must call the classifier before actor resolution, action dispatch, the Purchase service, or any target write, and must convert a classified refusal into the structured result defined below. Target completion must require the first-owner decision returned by that same invocation and transaction.

This layered path is mandatory:

`locked authoritative target facts -> RequireTargetExecution -> structured gate outcome -> only FIRST_OWNER may resolve/dispatch/write/complete`.

An exception message, SQLSTATE text, source ordering, or the presence of a string literal is never enforcement evidence.

## 4. Immutable target fence result contract

Revised A5 must add a server-owned structured target-attempt result in the existing ControlPlane contract path. Names may follow repository conventions, but the serialized semantics are fixed:

- `Outcome`: exactly `FIRST_OWNER`, `COMPLETED_REPLAY`, or `REJECTED`;
- `FailureCode`: `NONE`, `LEASE_FENCE_STALE`, `IDEMPOTENCY_PAYLOAD_MISMATCH`, or the already-authorized fail-closed code applicable to an incomplete equal-fence attempt;
- `SubmittedFencingToken`;
- `AuthoritativeFencingToken`, read under the target transaction lock;
- authoritative execution ID and request SHA-256 when a committed target result exists;
- the original immutable terminal result only for `COMPLETED_REPLAY`;
- no caller-selected authority, handler, idempotency key, or executable material.

The classification is exhaustive:

1. `submitted < authoritative`: `REJECTED / LEASE_FENCE_STALE`; no target write and no handler call.
2. `submitted > authoritative` and no committed result: `FIRST_OWNER`; execution may continue inside that same target transaction.
3. `submitted == authoritative` and an immutable result exists with the same execution ID, request digest, grant, lease, fence, and plan: `COMPLETED_REPLAY`; return the original result without re-execution or version change.
4. `submitted == authoritative` and the execution ID or any bound payload differs: `REJECTED / IDEMPOTENCY_PAYLOAD_MISMATCH`; disclose no terminal payload and perform no write.
5. `submitted == authoritative` with no committed terminal result: fail closed using the already-authorized incomplete/in-progress code; it is not a first owner and cannot execute again.

The target provider must expose an internal, non-DI, friend-test transaction seam in its authorized production file. Production DI must still resolve only the public pinned concrete provider. The seam supplies deterministic locked target facts and write counters to the tests; it may not weaken the public route or create an alternate production provider.

## 5. Corrected A5-11

The literal test name remains:

`A5_StaleFenceOrEqualFenceDifferentDigestFailsBeforeActionHandler`

It must invoke the real `NpgsqlA4TargetExecutionProvider` orchestration through the internal transaction seam. Direct state-machine-only testing, source-text inspection, `IndexOf`, `Contains`, SQL token matching, or a fake production registration cannot satisfy A5-11.

The test must use an authoritative fence of `42` and make these independent calls and assertions:

### Lower-fence attempt

Submit fence `41` with a valid signed job. Assert all of the following:

- outcome is exactly `REJECTED`;
- structured failure code is exactly `LEASE_FENCE_STALE`;
- submitted fence is exactly `41` and authoritative fence is exactly `42`;
- authoritative execution ID/digest fields equal the locked facts, or are explicitly absent when no committed result exists;
- Purchase handler invocation count is zero;
- Purchase business mutation count is zero;
- Purchase history count is zero;
- Purchase command-idempotency receipt count is zero;
- normal Purchase audit count is zero;
- A4 target watermark/result/idempotency write count is zero;
- A4 target audit count is zero;
- A4 target outbox count is zero;
- target commit count is zero and the read-only target transaction is aborted/rolled back exactly once without disposal ownership escape;
- the pre/post control lifecycle snapshot and grant snapshot are value-identical, and control lifecycle/grant/audit/outbox write counters are zero.

### Equal-fence exact replay

Seed the committed immutable result at fence `42`, then submit the same execution ID and exact request payload/digest. Assert:

- outcome is `COMPLETED_REPLAY` and failure is `NONE`;
- the original terminal result is returned byte/value-identically;
- submitted and authoritative fences are both `42`;
- handler and every Purchase, history, idempotency, audit, outbox, watermark, target-result, and control write counter remain zero;
- no business or lifecycle version changes.

### Equal-fence collision and incomplete case

- Change the execution ID or one canonical payload field while retaining fence `42`; assert `REJECTED / IDEMPOTENCY_PAYLOAD_MISMATCH`, exact fence values, no terminal-result disclosure, zero handler calls, and zero writes.
- With fence `42` and the same payload but no committed result, assert the fail-closed incomplete/in-progress result, zero handler calls, and zero writes. Equal fence is never a new owner.

The lower-fence, exact-replay, collision, and incomplete assertions are separately observed; a loop that obscures their individual counters or outcomes is not accepted.

## 6. Corrected semantic A5-M18

`A5-M18` is one coherent two-predicate production mutation in `Rev869BL1BoundaryStateMachine.RequireTargetExecution`:

- replace the lower/equal fence predicate at entry line 664 with an unconditional successful predicate; and
- replace the strict first-owner predicate at entry line 667 with an unconditional successful predicate.

No exception code, SQL token, message, test, helper, or manifest is changed. This production mutation compiles because only boolean arguments to existing `Require` calls change. It removes the actual fence decision and makes a lower fence with no committed result appear eligible for first ownership. A5-11 must kill it because the lower-fence call ceases to return the required structured rejection and attempts the handler/write path. The exact-replay and collision cases independently protect the equal-fence half of the invariant.

The disposable mutant is invalid unless its patch selector matches exactly those two predicates once, the production assembly compiles, the baseline A5-11 passes, mutated A5-11 fails for the fence/write assertions, and post-restore production-tree equality is proven.

## 7. Independent semantic review of A5-M19 through A5-M40

Each row below replaces any earlier compound wording. Each mutation changes production code only, has one primary property, and has a decisive behavioral assertion. The future checkpoint must record the exact file, symbol, before/after production diff hash, compile identity, intended test identity, and failure assertion for every row.

| ID | Exact production transformation | Exact enforcement point | Decisive killer and assertion | Validity / separation |
|---|---|---|---|---|
| A5-M19 | In the control durable replay branch, replace the comparison of stored acquisition/operation request SHA-256 to incoming SHA-256 with `true`, while retaining request-ID equality. | `NpgsqlA4DurableControlPlanePersistenceProvider`, locked idempotency replay classifier. | A5-20: same ID plus changed digest returns `IDEMPOTENCY_PAYLOAD_MISMATCH`, with identical lifecycle/grant/lease/result and zero audit/outbox writes; exact same digest separately returns the original result. | Semantic and compilable; control replay only, so it does not duplicate M18 target fencing. |
| A5-M20 | Remove the `RequireGrantValid(grant, now)` call from durable lease acquisition. | State machine acquisition path invoked by the durable provider before mutation. | A5-12: a not-yet-valid grant cannot acquire; lifecycle/grant/lease/audit/outbox and target-call counters remain zero. Expired and revoked rows remain baseline matrix assertions. | Semantic single-call deletion; distinct from lease-expiry bounding in M21. |
| A5-M21 | Replace the renewal predicate `request.RequestedExpiresAt <= grant.ExpiresAt` with `true`. | `RenewExecutionLease` before renewed receipt construction. | A5-13: requested renewal one tick after grant expiry is rejected and state/audit/outbox remain identical. | Semantic, compilable, and limited to renewal upper bound. |
| A5-M22 | Replace the `noTargetResultProof` predicate with `true` while retaining expiry and greater-fence requirements. | `ExpireUnusedLease`/reacquisition transition. | A5-13: absent proof cannot reactivate the grant or allocate a new fence; lifecycle/grant/lease/audit/outbox remain unchanged. | Semantic; does not repeat M18's target fence comparison or M21's time bound. |
| A5-M23 | When building the dispatch job, substitute the current lifecycle resource version for the immutable stored plan version. | `SignedCommandServiceV2` A4 job construction. | A5-15: after lifecycle advancement, the emitted job's plan is value-identical to the committed grant/lease plan and no reconstructed value is accepted. | Semantic; production value substitution compiles and is distinct from identities/digests. |
| A5-M24 | Substitute executor transport identity for stored management authorizer in the dispatch job. | Same A4 job builder, authorizer assignment. | A5-15/A5-16: exact stored authorizer is retained; substitution fails before target/provider mutation. | Semantic; one identity substitution, distinct from business actor M36. |
| A5-M25 | Substitute authorization request ID/SHA for the committed acquisition request ID/SHA in the dispatch job. | Same A4 job builder, acquisition provenance assignment. | A5-15/A5-16: acquisition ID and digest equal the locked lease receipt exactly; substitution fails with zero mutation. | Semantic; provenance-specific and non-duplicate. |
| A5-M26 | In reconciliation, synthesize a terminal result from control dispatch fields instead of calling the pinned result reader. | `A4TerminalResultReconciliationService` result-source branch. | A5-17/A5-18: pinned reader is called exactly once; its exact result/digest is required; missing/conflicting data quarantines with zero execution and zero control consumption. | Semantic and compilable using existing job/result constructors; source-authenticity property only. |
| A5-M27 | On an authoritative result-reader `404`/missing outcome, issue `POST /execute` with the stored dispatch instead of returning missing. | `PinnedA4TargetResultProvider`; its baseline internal HTTP operation accepts the full dispatch but performs one result `GET` only. | A5-17: deterministic HTTP handler observes exactly one `GET`, zero `POST`, zero request body, zero handler/execution calls; missing remains unavailable/quarantined. | Semantic; this is active re-execution, not M26's result synthesis. |
| A5-M28 | Move the exact reconciler workload/role/scope check below the terminal replay return. | Reconciliation entry before any result read or replay disclosure. | A5-19: wrong reconciler is rejected on first-owner and replay paths before provider read; state, result disclosure, audit/outbox, and consumption counters are zero. | Semantic ordering mutation; no string-order assertion. |
| A5-M29 | Replace the pinned concrete durable-provider DI factory with a null-returning factory of the same interface type. | ControlPlane/API production service registration. | A5-21: offline production service graph has exactly one pinned concrete descriptor and resolution/validation succeeds; mutant resolution/validation fails. No host is started. | Compilable; exact DI integrity mutation, not a fake runtime implementation. |
| A5-M30 | Replace the verified recomputed signed-job digest comparison with `true`. | Target provider internal route verification before locked facts, actor resolution, or handler. | A5-25: mutate one signed field while retaining original signature/digest; return `PAYLOAD_HASH_MISMATCH` (or the frozen signature failure code) with zero relational and handler calls. | Semantic; signed-job authenticity only, distinct from direct DML M39 and action schema M33. |
| A5-M31 | Map `purchase.comparison.approve` to the existing reject Purchase service method of the same typed signature. | Fixed 19-action switch in the target provider. | A5-23/A5-27: manifest remains 19 unique rows and this action invokes approve exactly once, reject zero, every other method zero. | Compilable wrong-method mapping; behavioral spy, not source text. |
| A5-M32 | Add a fallback for an unknown action ID that reflects a caller-carried handler identity into an existing Purchase method and invokes it. | Default arm of the fixed target action dispatcher. | A5-24/A5-27: unknown action plus a method-like handler value returns `OPERATION_MISMATCH`, with zero reflection/handler/database calls. | A coherent executable negative mutant; it tests caller-selected execution, unlike M33's known-action schema pin. |
| A5-M33 | Replace the known action's parameter-schema SHA-256 equality predicate with `true`; retain action version, schema ID, and handler pins. | Manifest lookup/contract validation before database access. | A5-23/A5-25: wrong schema hash for a known action fails before relational/handler calls. | Single pin removal; semantic and not compound. |
| A5-M34 | For `purchase.rfq.create`, omit `QuoteDueAt` from canonical parameter serialization/hash while leaving all other fields. | Server-owned canonical parameter encoder. | A5-24/A5-25: two typed payloads differing only in `QuoteDueAt` have different canonical bytes/hash and the changed digest cannot replay; zero handler calls on collision. | Concrete canonicalization defect, distinct from M33 metadata validation. |
| A5-M35 | Replace the plan-resource-key to typed Purchase record/source-key equality predicate with `true`; retain organization and version checks. | Plan/action binding validator before actor resolution/handler. | A5-26: signed plan resource A with typed payload resource B fails before handler/database; all versions and writes unchanged. | One binding removal; semantic and non-compound. |
| A5-M36 | Construct `PlanBound` business-user context from signed actor fields without comparing the server-resolved employee/role. | `IEmployeeIdentityResolver` result check in target provider. | A5-26: resolver returns a different employee/role; baseline rejects before handler, mutant calls it. | Semantic actor-provenance bypass; distinct from transport authorizer M24. |
| A5-M37 | Replace derived Purchase idempotency key `a4:<TargetIdentitySha256>` with `a4:<AuthorizationDecisionId>`. | Target provider Purchase service invocation. | A5-24/A5-27: captured key equals only the target-identity derivation, has no caller/provenance value, and remains stable on exact replay. | Compilable caller/provenance-key substitution; target replay itself remains M18/M19. |
| A5-M38 | In the ambient enlisted Purchase scope branch, call `CommitAsync` on the outer transaction. | `EfRev869BPurchaseService` enlisted-scope implementation, the only authorized transaction-enlistment seam. | A5-09/A5-10/A5-28: Purchase scope outer commit/rollback/dispose counters are all zero; only target provider owns exactly one commit or rollback. | Semantic ownership escape; one concrete commit mutation. |
| A5-M39 | Replace the Purchase service call for one fixed action with a direct business `ExecuteUpdateAsync`/raw-DML call in the target provider. | Fixed action execution branch in `NpgsqlA4TargetExecutionProvider`. | A5-27/A5-29: service method count is exactly one, direct-business-DML counter is zero, and public Purchase transaction behavior is unchanged. | Concrete architectural bypass; distinct from signed validation M30. |
| A5-M40 | Set `TargetTransactionId` to empty when constructing a successful terminal receipt. | Target result construction immediately before atomic persistence. | A5-30: receipt transaction ID is nonempty and exactly equals the committed target transaction; receipt digest binds it with action, actor, history, audit, outbox, and result identities. | Single receipt-field omission; semantic and compilable. |

M32 is permitted only as a disposable mutation. No reflection fallback or caller executable field may exist in baseline source. M27's full-dispatch internal reader input exists solely so reconciliation can bind the requested result to the authoritative dispatch; baseline performs only the pinned `GET` and never sends that dispatch to an execution route.

## 8. Corrected test obligations

The 30 literal `A5_` test names frozen by the A5 decision remain exact; no 31st `A5_` method is authorized. A5-11 is replaced in substance by section 5. The following tests receive mandatory strengthened assertions without renaming:

- A5-12: separate not-before, expiry, revoked, acquire, renew, begin, and target-commit cases; each asserts exact failure code and zero state/write/downstream calls.
- A5-13: separate renewal-after-grant, reacquisition-without-proof, and non-greater-fence cases; each asserts identical lifecycle/grant/lease and zero audit/outbox.
- A5-15/A5-16: compare full immutable plan and provenance values, not string presence.
- A5-17/A5-18: assert pinned-reader method/URI/count, zero execution method/count, exact result provenance, quarantine, and zero consumption.
- A5-19: assert authorization precedes both read and replay disclosure by zero provider/result-access counters for a wrong reconciler.
- A5-20: independently assert exact replay and changed-payload collision for target and control, with zero re-execution/version change.
- A5-21: inspect and resolve the actual production service collection offline; no host or service starts.
- A5-23 through A5-27: use typed manifest/dispatcher/provider behavior and service spies; source-string occurrence tests are supplementary only and cannot kill a mutant.
- A5-28: count ownership operations on the real enlisted-scope seam.
- A5-29: prove public Purchase paths still own their transactions while A5 cannot issue business DML.
- A5-30: compare the terminal receipt to captured committed history/audit/outbox/result/transaction identities and its canonical digest.

Each decisive assertion must fail on its assigned mutant for the stated semantic reason. A test failure caused only by a changed string, source layout, reflection inventory, compile warning, or unrelated exception is not a kill.

## 9. Exhaustive future implementation boundary

One later revised A5 correction may change only these 27 paths:

1. `SESS.NexaERP.slnx`
2. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
3. `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj`
4. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
5. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
6. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
7. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
8. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
9. `src/SESS.NexaERP.ControlPlane/Program.cs`
10. `src/SESS.NexaERP.ControlPlane/Persistence/Rev869BA4ControlPlaneSchemaV1.cs` — NEW
11. `src/SESS.NexaERP.ControlPlane/Persistence/NpgsqlA4DurableControlPlanePersistenceProvider.cs` — NEW
12. `src/SESS.NexaERP.ControlPlane/Reconciliation/PinnedA4TargetResultProvider.cs` — NEW
13. `src/SESS.NexaERP.ControlPlane/Reconciliation/A4TerminalResultReconciliationService.cs` — NEW
14. `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj`
15. `src/SESS.NexaERP.Infrastructure/DependencyInjection.cs`
16. `src/SESS.NexaERP.Infrastructure/Persistence/NpgsqlA4TargetExecutionProvider.cs` — NEW
17. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BA4TargetExecutionBoundarySql.cs` — NEW
18. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.cs` — NEW
19. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.Designer.cs` — NEW
20. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
21. `src/SESS.NexaERP.Api/Program.cs`
22. `src/SESS.NexaERP.Api/Endpoints/Rev869BA4TargetExecutionEndpoints.cs` — NEW
23. `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj`
24. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
25. `tests/SESS.NexaERP.ControlPlane.Tests/A4FailureCorrectionContractTests.cs` — NEW
26. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
27. `outputs/rev869b_external_controller_phase_a_a5_revised_implementation_checkpoint.md` — NEW

The prior A5 checkpoint cannot be overwritten. The friend-test seam must be implemented within paths 14/16/23; no helper or 28th path is permitted. Immutable Purchase application/domain contracts, operation partials, business rules, existing migration/model snapshot, `Rev869BCommandContextSql.cs`, public Purchase endpoints, prior reports/checkpoints, and all external helpers remain out of bounds.

## 10. Acceptance formulas and evidence

The future correction must restart the complete campaign from a clean exact descendant of this report commit:

- `literal_A5_tests = 30`
- `A5_individual_pass = 30/30`
- `retained_A4_tests = 23/23`
- `retained_A4_mutants = 10`
- `A5_mutants_M11_to_M30 = 20`
- `A5_mutants_M31_to_M40 = 10`
- `total_mutants = 10 + 20 + 10 = 40`
- `compiled = 40`
- `killed = 40`
- `survivors = 0`
- `invalid = 0`
- `duplicate_production_diff_hashes = 0`
- `external_database_connections = 0`
- `migration_applications = 0`
- `services_started = 0`
- `production_operations = 0`

For each mutant `m`:

`valid(m) = production_only(m) AND exact_unique_selector(m) AND compiles(m) AND baseline_passes(killer(m)) AND mutant_fails_decisive_assertion(m) AND restored_source_equals_baseline(m)`.

Overall acceptance is:

`PASS = exact_lineage AND exact_27_path_diff AND 30/30_A5 AND 23/23_A4 AND all_authorized_offline_regressions AND count({m | valid(m)})=40 AND zero_external_operations AND clean_target_scoped_worktree`.

The checkpoint must independently record executable identities and SHA-256 hashes for SDK/compiler/test assemblies, baseline production inputs, each disposable mutant diff, test result artifacts, the 20-row/2,668-byte action manifest and its frozen SHA-256 `EDAF648EFF4BD77158EF3A18A780D7B0DAD634FFB90CDBA8564A27D4DCFC95CB`, and all zero-operation counters. Previous M11-M17 observations may be cited historically but cannot be counted.

## 11. Stop conditions

Create only the revised blocker checkpoint at path 27 and stop without a source correction commit if any of these occurs:

- entry lineage, report hash, manifest byte count/hash, or clean target-scoped worktree is not exact;
- a required edit, test seam, mutant, package, project, helper, migration snapshot, or checkpoint needs an unnamed 28th path;
- the production provider cannot return submitted and authoritative fence values from locked facts;
- A5-11 cannot drive the actual provider orchestration and observe every required zero-write/state counter offline;
- lower-fence rejection or equal-fence replay relies on SQL/error/source text instead of structured behavior;
- any M18-M40 selector is absent, ambiguous, test-only, non-compiling, behaviorally duplicate, or not killed by its decisive assertion;
- any intended killer fails for an unrelated token, text, layout, or exception reason;
- any immutable Purchase rule/interface/operation partial must change;
- any database connection, migration application, service start, provisioning, deployment, production operation, Phase-B responsibility, Correction 2, or `../legacy-reference/` access becomes necessary.

No alternate mutant may be silently substituted after a stop. A new management decision is required.

## 12. Next management gate

The next gate may authorize exactly one revised REV869B Option-A Phase-A Correction A5 starting from the commit containing this report, within section 9's 27 paths, implementing section 4's structured fence contract and section 5's behavioral A5-11, and rerunning all section 10 evidence. It must end with exactly one source-only correction commit or exactly one blocker checkpoint commit, then stop for a fresh independent report-only review.

This report does not start that correction automatically.
