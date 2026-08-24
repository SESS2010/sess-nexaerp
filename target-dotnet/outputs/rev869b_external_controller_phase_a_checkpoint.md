# REV869B Option-A Phase-A Correction A4 source implementation checkpoint

Date: 2026-08-20

Checkpoint type: `A4_SOURCE_IMPLEMENTATION_COMPLETE_PENDING_INDEPENDENT_REVIEW`

Entry HEAD: `a126de9d2ab9efe90490d6a1734aac320bab5f04`

Entry parent: `21ab39c1e6e4658be892bfc06fc8a18b768c4d32`

Architecture freeze: `outputs/rev869b_external_controller_phase_a_a4_lease_atomic_boundary_architecture_freeze.md`

Architecture SHA-256: `2DBC7293840F6BC2613EB3A3D473D28D848E7A8364F3BBA8361BAEF7C37A56C5`

Decision: `PHASE_A_CORRECTION_A4_REVISED_SOURCE_ONLY_GATE=GO_IMPLEMENTED_PENDING_INDEPENDENT_REVIEW`

Option L1 is implemented within the exact eight-file allowlist. Authorization creates an exact plan/executor-bound `ACTIVE` grant and no lease. `AcquireExecutionLease` is the only high-level lease authority and atomically reserves the grant, advances the fence, stores the immutable receipt, lifecycle, audit and outbox. Begin-execution requires the committed lease and retains `RESERVED`. Target execution is target-local, fenced and idempotent. Reconciliation consumes the grant and terminalizes from an immutable target result without invoking business mutation.

## Stage-0 and boundary result

| Gate | Result |
|---|---|
| HEAD / parent | Exact: `a126de9d2ab9efe90490d6a1734aac320bab5f04` / `21ab39c1e6e4658be892bfc06fc8a18b768c4d32` |
| Subject / branch | `REV869B Phase-A A4 lease atomic boundary freeze` / `master` |
| Entry HEAD content | Exactly the A4 architecture-freeze report |
| Report hash | Exact: `2DBC7293840F6BC2613EB3A3D473D28D848E7A8364F3BBA8361BAEF7C37A56C5` |
| Target status | Clean before implementation |
| Legacy boundary | `../legacy-reference/` remained untracked; no legacy content was opened, read, modified or used |
| Changed boundary | Exactly the seven implementation/test files plus this checkpoint; no ninth file |

## Implementation result

- Contracts add the exact A4 lifecycle and grant vocabularies, high-level acquire/begin/reconcile operations, immutable plan/grant/lease/job/result/reconciliation records, composite transaction discrimination and high-level target result interfaces.
- Options pin distinct management authorizer, lease issuer, executor, target execution and reconciliation identities and reject forbidden overlaps.
- `Rev869BL1BoundaryStateMachine` owns grant-only authorization, one-winner acquisition/replay, same-fence renewal, proof-bound expiry, greater-fence reacquisition, lease-before-dispatch, stale-fence denial and result-only reconciliation.
- Raw protected ingress remains `AcceptRawCommandAsync`; it constructs typed high-level A4 composite requests from server-resolved identities and never acquires a lease inside execution.
- Reader identity/version/artifact/schema/stage/binding/cardinality preflight now completes before oracle resolution and `ReadAsync`.
- The architecture suite contains exactly 23 unique literal `A4_` facts. Fixtures inject faults only; production code owns every decision.

## F01-F07 implementation result

| Finding | Source-only result | Evidence |
|---|---|---|
| F01 raw ingress/canonical trust | COMPLETE | Protected authority remains raw-only; canonical/header/payload/signature regressions pass and denial has zero lifecycle/atomic calls. |
| F02 composite ownership | COMPLETE | One durable provider retains one snapshot read and one composite atomic mutation; no partial lease/fence/grant setter is exported or injectable. |
| F03 lease/atomic boundary | COMPLETE | Option L1 exact grant, separate lease acquisition, reserved grant, lease-before-dispatch, target-local transaction and result-only reconciliation implemented. |
| F04 reader preflight | COMPLETE | Complete declared metadata, binding and cardinality mismatch is rejected before oracle resolution and `ReadAsync`. |
| F05 readiness/audit/privacy | COMPLETE | Fail-closed readiness, local atomic audit/outbox and sensitive-field denial regressions pass. |
| F06 independent assurance | COMPLETE_PENDING_REVIEW | Exactly 23 A4 tests, 10/10 A4 mutants and 8/8 retained mutants pass their implementation gates; independent review is still required. |
| F07 evidence/arithmetic | COMPLETE_PENDING_REVIEW | Unique/raw arithmetic, executable/source/SQL hashes, no-connect counters, scans, diffs and Git boundary are recorded; independent review is still required. |

Ownership result: the control-plane database alone owns canonical lifecycle, grant, lease, fence allocation, dispatch and reconciliation facts. The target ERP boundary alone owns fenced business mutation, business/history rows, target audit/outbox, target fencing watermark and immutable target terminal result. No distributed ACID claim or duplicate lifecycle owner was introduced.

Lifecycle result: canonical A4 statuses are exactly `Draft`, `Rejected`, `Authorized`, `LeaseActive`, `Executing`, `Succeeded`, `Failed`, `Expired`, `Revoked`, `Cancelled`, `Quarantined`; grant states are exactly `ACTIVE`, `RESERVED`, `CONSUMED`, `REVOKED`, `EXPIRED`, `REJECTED`. Authorization creates no lease; lease acquisition reserves; begin retains reservation; reconciliation consumes exactly once.

## Validation and arithmetic

| Gate | Result |
|---|---|
| Affected control-plane warning-as-error build | 0 warnings; 0 errors |
| Full solution warning-as-error build | 0 warnings; 0 errors |
| Exact A4 literal subset | 23 passed; 0 failed; 0 skipped |
| A4 individual invocations | 23 of 23 passed; each literal method invoked separately |
| Complete Phase-A control assembly | 86 passed; 0 failed; 0 skipped |
| Focused REV869B non-PostgreSQL subset | 81 passed; 0 failed; 0 skipped |
| Complete ERP non-PostgreSQL assembly | 455 passed; 0 failed; 0 skipped |
| Canonical SQL/source subset | 3 passed; 0 failed; 0 skipped; two fresh workers byte-identical |
| Model/snapshot/source parity | 2 passed; 0 failed; 0 skipped |
| PostgreSQL discovery/execution | 87 discovered; 0 executed |
| EF no-connect discovery | 13 migrations; applied status intentionally unknown; connection opens 0 |
| PowerShell AST | 24 files; 0 parse errors; scripts executed 0 |
| Production mutants | 10 compiled; 10 killed; 0 survived; 0 invalid |

`A4_unique=23`

`PhaseA_unique=86`

`ERP_unique=455`

`Combined_unique=541`

`Raw_pass_events=790`

`PostgreSQL_discovered=87`

`PostgreSQL_executed=0`

Raw pass events include the first A4 diagnostic run (`22` pass, `1` fixture-timestamp failure), corrected A4 run `23`, 23 separate A4 method invocations, complete control `86`, A4 source evidence `2`, complete ERP `455`, canonical subset `3`, focused REV869B `81`, parity `2`, final checkpoint-sensitive control `86`, final checkpoint evidence `1`, four passing assertions from mixed A4-mutant invocations, and two preliminary retained-mutant diagnostic passes. The preliminary M06 diagnostic removed only one of two target fence guards and passed because the second guard remained; the defined complete M06 removed both comparisons, compiled and was killed. The preliminary retained A2 cardinality and freshness diagnostics likewise removed only one of multiple enforcement points; their complete defined bypasses compiled and were killed. No invocation is silently omitted.

## Production mutant evidence

| ID | Production mutation | Intended killer | Result |
|---|---|---|---|
| A4-M01 | Authorization populates a lease | Test 1 | compiled; killed |
| A4-M02 | Grant executor comparison accepts substitution | Tests 2 and 4 | compiled; killed by both |
| A4-M03 | Acquisition compares plan ID but omits version/hash | Test 3 | compiled; killed |
| A4-M04 | Exported `ILeaseSetter` partial mutation facet | Test 22 | compiled; killed |
| A4-M05 | Begin execution accepts caller job without authoritative lease | Test 8 | compiled; killed |
| A4-M06 | Both target fence/watermark comparisons removed | Test 10 | compiled; killed |
| A4-M07 | Reconciliation remains `Executing` instead of terminalizing discovered result | Tests 14 and 15 | compiled; killed by Test 15 |
| A4-M08 | Acquisition and execution replay digest comparisons bypassed | Tests 6 and 17 | compiled; killed by both |
| A4-M09 | Authorization audit/outbox failure no longer rolls back | Tests 13 and 18 | compiled; killed by Test 18 |
| A4-M10 | Reader version mismatch allowed past pre-read preflight | Tests 19 and 20 | compiled; killed by Test 19 |

Every defined mutant changed production code in `C:\Users\User\AppData\Local\Temp\rev869b-a4-mutants-20260820`, built with warnings as errors and ran only its named killer subset. Source equality was SHA-256 verified after restoration, then the entire temporary directory was removed. No mutant artifact remains.

Retained regression campaign:

| ID | Retained production mutation | Intended killer | Result |
|---|---|---|---|
| A3-M01 | Partial composite nonce mutation API exposed | `A3_ExportedOrInjectablePartialNonceIdempotencyLeaseAndStateMutationIsImpossible` | compiled; killed |
| A3-M02 | Caller stored grant substitutes authoritative provider grant | `A3_EveryStoredGrantIssuerActorPolicyTenantPlanVersionEvidenceLeaseFenceAndExpirySubstitutionFailsBeforeLifecycle` | compiled; killed |
| A3-M03 | Caller reader version controls descriptor selection | `A3_CallerSelectedReaderVersionUpgradeDowngradeArtifactOrSchemaNeverSelectsReader` | compiled; killed |
| A3-M04 | Offline SQL input production source drifts | `A3_CheckpointSqlEvidenceMatchesMachineCapturedCanonicalResultExactly` | compiled; killed |
| A2-M01 | Request/provider identity becomes authority | `A2_CallerStateVersionGrantExportAttemptAndEpochCannotBecomeTrustedFacts` | compiled; killed |
| A2-M02 | Both lifecycle version comparisons bypassed | `A2_EveryRowRejectsWrongStateVersionRoleScopeGrantEvidenceLeaseFenceEpochAttemptAndAudit` | compiled; killed |
| A2-M03 | Complete exact reader-cardinality gate bypassed | `A2_DuplicateMissingUnknownOrExtraReaderFailsBeforeOracle` | compiled; killed |
| A2-M04 | Both readiness freshness enforcement points bypassed | `A2_NullExpiredFutureOrInvertedFreshnessReturns503OnBothRoutes` | compiled; killed |

`retained_mutants_valid=8`

`retained_mutants_compiled=8`

`retained_mutants_killed=8`

`retained_mutants_survived=0`

`retained_mutants_invalid=0`

The retained campaign used `C:\Users\User\AppData\Local\Temp\rev869b-a4-retained-mutants-20260820`; source equality was verified after every restore and the complete directory was removed.

## Implementation source evidence

| File | Lines | Bytes | Uppercase SHA-256 |
|---|---:|---:|---|
| `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs` | 1994 | 65330 | `B39F178AC58B76221B85FC1A32A5639D5599712F42CE7A45C10C869919CD9D0C` |
| `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs` | 202 | 10741 | `5BEFE3F342E6BC8B5F928C038C85EDCDD38B642D58FF4BEEF7B6A86FE85B020D` |
| `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs` | 821 | 52017 | `2F6BDFD77EFACE2442884C683182F6B81CF20AB9A3AA7C68B01EA9A631E76264` |
| `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs` | 1056 | 50108 | `58D21C840ABD797A5CA9C041B424AA900B605BCA4977466C82492BEFB11EEAF4` |
| `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs` | 906 | 46273 | `C2462EF3F95BE484BAA8B281344C0EEA851C0D92D4EEF1B0CF3BC94B2401F672` |
| `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs` | 3268 | 157165 | `1EA332D33342C735E6865CE9EFFEEFA3E3B8BBC930005A32124EB937F088D55B` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | 858 | 52637 | `5A21AF4DF6B92B1E871430AD44EED2F213E09E5345B29A4D835E898DE2D17DB7` |

The checkpoint's own final hash is intentionally supplied after the one commit; embedding it here would be self-referential. Canonical SQL input hashes and byte/LF/hash evidence remain the unchanged A3 block below and were revalidated in two fresh processes.

## Executable and environment evidence

| Tool | Resolved absolute path | File version | Bytes | Uppercase SHA-256 |
|---|---|---|---:|---|
| dotnet | `C:\Program Files\dotnet\dotnet.exe` | `10,0,1126,37416 @Commit: e2f47b0110ed922f21a1522da67279133ce28f32` | 167208 | `AB1B71FD3DD71062E074C9FAB8312081A81B7F2B3E0327C48C4D249C8D1A3135` |
| git | `C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe` | `2.53.0.windows.3` | 46464 | `C53279919FDEA03474BB23B465B3A82287157491F1BD69A5EB82DD9831582333` |
| rg | `C:\Users\User\.vscode\extensions\openai.chatgpt-26.810.52044-win32-x64\bin\windows-x86_64\rg.exe` | file version unavailable | 4218880 | `14231169855EC5205CF5A1B6F1DB358FF4AED4247C86B69CE8AAE647C77F6680` |
| robocopy | `C:\WINDOWS\system32\Robocopy.exe` | `10.0.19041.1` | 172544 | `42B03B12BD26D23BCE6192991E37ABE55E5B12E38AA95B2CF1CD46F33EB58716` |
| Windows PowerShell | `C:\WINDOWS\System32\WindowsPowerShell\v1.0\powershell.exe` | `10.0.19041.1` | 455680 | `9785001B0DCF755EDDB8AF294A373C0B87B2498660F724E76C4D53F9C217C7A3` |
| apply-patch engine | `C:\Users\User\.vscode\extensions\openai.chatgpt-26.810.52044-win32-x64\bin\windows-x86_64\codex.exe` | file version unavailable | 295151920 | `88AA986D1405D41DCC9C2F777D7B028DE07EDC33B6468A8DD8DB6A0CC62C315F` |

Environment: SDK `10.0.303`; runtime `.NET 10.0.11`; EF CLI `10.0.10`; OS `Microsoft Windows 10.0.19045`; culture `en-US`; encoding for checkpoint/source evidence UTF-8; repository line endings validated by `git diff --check`.

Principal validation commands were warning-as-error `dotnet build` for both affected test projects and `SESS.NexaERP.slnx`; `dotnet test --no-build --no-restore` for A4, complete control, focused REV869B, complete ERP non-PostgreSQL, canonical evidence and parity subsets; `dotnet test --list-tests` for PostgreSQL discovery only; `dotnet ef migrations list --no-connect --no-build` with inert `127.0.0.1:1`, pooling disabled; PowerShell parser API calls only; Git boundary/diff checks; and source/security scans. No PowerShell script was invoked.

Observed operational counters:

`database_connection_open_count=0`

`migration_application_attempt_count=0`

`migration_application_completed_count=0`

`postgresql_test_execution_count=0`

`powershell_script_execution_count=0`

## Prohibited operations and retained states

No PostgreSQL connection/test, migration application or rollback, installed ERP operation, D-drive or external-disk access, lifecycle/recovery/purge/export operation, external provisioning, deployment, production access, credential/key use, Phase B, Correction 2, or legacy-reference access occurred. No `Program`, endpoint, project, solution, migration, model, snapshot, SQL, script, helper, execution-binding or ninth implementation path changed.

`phase_a_correction_a4_source_implementation_state=COMPLETE_PENDING_INDEPENDENT_REVIEW`

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

Exact single next gate: one fresh independent report-only REV869B Option-A Phase-A Correction A4 source architecture/security review of the final one-commit eight-file diff. No source edit, PostgreSQL, Phase B, Correction 2, provisioning, deployment or production action is included in that gate.

---

# Historical superseded A4 architecture blocker checkpoint

Date: 2026-08-17

Checkpoint type: `A4_SOURCE_IMPLEMENTATION_BLOCKED_BEFORE_EDIT`

Starting HEAD: `cb4e90852947f2c9fdada3ea60a3110660d5cec8`

Expected parent: `ba99fc90a06d387d98396cbe80a23323f8f0baf0`

Authorization: one bounded source-only A4 correction against the eight-file allowlist.

Decision: `PHASE_A_CORRECTION_A4_SOURCE_ONLY_GATE=NO_GO_ARCHITECTURE_FREEZE_REQUIRED`

No A4 production source, test, project, migration or helper edit was made. This checkpoint is the only changed file.

## Stage-0 result

| Gate | Result |
|---|---|
| HEAD / parent | Exact: `cb4e90852947f2c9fdada3ea60a3110660d5cec8` / `ba99fc90a06d387d98396cbe80a23323f8f0baf0` |
| Subject / branch | `REV869B Phase-A Correction A3 failure reconciliation` / `master` |
| HEAD content | Exactly one reconciliation report |
| Reconciliation path | `outputs/rev869b_external_controller_phase_a_correction_a3_failure_reconciliation.md` |
| Reconciliation SHA-256 | Exact: `438F99CE1E5AB2F13C305AB5418452798D9A2DB4B49CEA68FA36A6105AA3AEEF` |
| Target status | Clean before this blocker checkpoint |
| Legacy boundary | `../legacy-reference/` remained untracked; no legacy content was opened, read, modified or used |
| Mandatory reading | Architecture freeze, A2 review/reconciliation, A3 checkpoint/review/reconciliation and all eight authorized files were read before the boundary decision |

Stage 0 passed. The stop condition arose from the required production design, not from lineage or worktree mismatch.

## Blocking architecture contradiction

The authorized A4 outcome simultaneously requires:

1. authorization approval with no lease;
2. an exact durable grant;
3. a later executor lease request and authoritative lease acquisition;
4. exact grant, plan, executor, lease and fence validation before lifecycle evaluation or persistence mutation;
5. no partial persistence mutation API;
6. one composite atomic execution operation; and
7. preservation of the frozen Phase-A lifecycle table.

The current frozen contract has no operation or transaction phase that can produce the authoritative lease between authorization and execution:

- `ControllerOperationV2` in `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:527-553` contains no lease-acquisition operation.
- Frozen authorization rules in `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs:26-95` require no lease.
- Frozen execution rules require a lease and fence already to exist.
- `IDurableControlPlanePersistenceProvider` in `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1502-1511` intentionally exposes only one authoritative snapshot read and one composite atomic mutation; F02 prohibits adding an independently callable lease mutation.
- `PhaseAControlPlaneAuthority` in `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:775-919` reads the snapshot, validates an already-existing lease, calls the lifecycle controller, and only then calls `ExecuteAtomicallyAsync`.

Therefore:

- If execution begins with no lease and the existing atomic call acquires it, lifecycle evaluation necessarily occurs before authoritative lease/fence acquisition and validation. That violates the required order.
- If the durable provider acquires a lease in an earlier call, that adds a separately callable mutation capability prohibited by retained F02 unless a new grant-bound composite transaction and lifecycle contract are frozen.
- If `ACQUIRE_LEASE` or an equivalent operation is added, the exhaustive frozen lifecycle table, legal state/operation matrix, authorization consumption semantics, idempotency/replay rules and audit event model change. That is an architecture-freeze change, not an implementation detail.
- If lease acquisition occurs outside the control-plane durable owner, authoritative ownership becomes ambiguous and violates retained F02.

No implementation within the authorized eight-file correction may silently choose among these materially different architectures.

## Exact architecture-freeze questions

Management must freeze one design before source implementation:

1. Is lease acquisition a distinct grant-bound control-plane command, or an internal phase of the execution transaction?
2. If distinct, what exact operation name, legal source/target state, authorized executor, evidence set, retry limit and audit event apply?
3. Does the lease-acquisition transaction change lifecycle state/version or only lease/epoch/fence state?
4. Does grant consumption occur at lease acquisition or at business execution start?
5. How is an acquired but never-used lease expired/released without exposing partial mutation APIs?
6. If acquisition is internal to execution, which owner performs lifecycle validation after the newly generated fence while preserving the separate lifecycle-controller owner?
7. What exact atomic boundary includes nonce, idempotency, grant state, lease/fence, lifecycle, attempt, outcome and audit outbox for acquisition and execution?
8. What is the exact completed-replay result for lease acquisition, execution and a crash between them?
9. Which request digest and idempotency identity bind the lease request versus the execution request?
10. May one authorization grant authorize both lease acquisition and execution, and how is one-time use represented without premature consumption?
11. What denial audit receipt is required for failed acquisition, stale fence, substituted executor and abandoned lease?
12. Does the chosen design require a durable-provider implementation or other file outside the current eight-file source-only allowlist?

## Reconciliation impact

F03 remains unresolved because the required lifecycle sequence cannot be represented without answering the freeze questions.

F04 remains eligible for an implementation after the architecture decision, but the authorization permits only one bounded A4 commit and forbids partial implementation when a required correction is blocked.

F06 cannot be completed because the exact lease-order test and M03 lease-order mutant lack a frozen production enforcement point.

F07 remains eligible after the architecture decision, but must not be implemented alone under this all-or-nothing A4 authorization.

F01, F02 and F05 remain preserved. In particular, no partial lease API was introduced to work around the contradiction.

## Validation and prohibited operations

Because the stop condition fired before implementation, the 17-test matrix, six A4 mutants, retained mutants and post-change validation were not run or claimed. The previously independently measured baseline remains informational only: A3 `16`, Phase-A `63`, focused ERP `79`, ERP non-PostgreSQL `453`, unique `516`, raw `611`, PostgreSQL `87` discovered / `0` executed.

This turn performed no PostgreSQL connection/test, migration application/rollback, lifecycle, recovery, purge, export, provisioning, deployment, production access, Phase B or Correction 2 operation. Measured operations performed in this turn: PostgreSQL connections `0`; migration applications attempted `0`; migration applications completed `0`.

## Retained states and next gate

`phase_a_correction_a4_source_implementation_state=BLOCKED_ARCHITECTURE_FREEZE_REQUIRED`

`phase_a_management_acceptance_state=FAIL`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

Exact single next management gate: a separate report-only Phase-A architecture-freeze decision answering the twelve lease-acquisition and atomic-boundary questions above and issuing a revised exhaustive A4 allowlist. Do not begin A4 implementation before that decision.

---

The prior A3 checkpoint is retained below as historical evidence only. It is superseded as the current implementation-state header by this A4 blocker checkpoint.

# REV869B Option-A Phase-A Correction A3 checkpoint

Date: 2026-08-17
Checkpoint type: bounded source-only implementation handoff pending independent review
Authorization: `PHASE_A_CORRECTION_A3_SOURCE_ONLY_GATE=APPROVED`
Starting HEAD: `8c78f6a480fcbf86afbf9f5460598ece5b8d6732`
Starting parent: `ef38eeb58a03cdf76a19320832f7194b468b70d5`
Ending commit: the single Correction-A3 commit containing this checkpoint. A commit cannot contain its own SHA-1 without changing that SHA-1; the exact authoritative ending identifier is the post-commit `git rev-parse HEAD` reported in the final handoff.
Authoritative reconciliation: `outputs/rev869b_external_controller_phase_a_correction_a2_failure_reconciliation.md`
Reconciliation SHA-256: `D0D578542A7183EAEF87E77C9ED98F06406493C8061D4FD02C5247027B7A9F64`
Entry-blocker SHA-256: `BA7EE5E76AB7A95BA96300FEEC4535E78483449B617E6C1FF8E1FF24C873041B`

## Verdict and boundary

`phase_a_correction_a3_source_implementation_state=COMPLETE_PENDING_INDEPENDENT_REVIEW`

Exactly the ten authorized files below changed. The duplicate-entry blocker and all historical reports/commits remain immutable. No file in `../legacy-reference/` was accessed or modified.

1. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
2. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
3. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
4. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
5. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
6. `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`
7. `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`
8. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
10. `outputs/rev869b_external_controller_phase_a_checkpoint.md`

## Finding closure and preservation

| Finding | Source-only result pending independent review |
|---|---|
| F02 ownership/provenance | Closed by one non-decomposable `IDurableControlPlanePersistenceProvider` surface, one snapshot read, one atomic mutation, server-pinned provider/controller identity, semantic version, artifact and readiness-policy version, and rejection before any owner call. |
| F03 authorization/atomicity | Closed by separated current execution authorization and immutable stored management grant. Exact issuer/key/version/signature, authorization/original authorizer, operation/executor, tenant/organization/database/resource/version, plan/evidence, lease/epoch/fence, policy row/artifact, time window and one-time state comparisons precede lifecycle and atomic persistence. Replay returns only the original grant-bound result. |
| F04 reader/oracle closure | Closed by server-owned exact reader descriptor multiset. Resolution uses only server selection; exact service/source identity, schema/stage, artifact, version/compatibility, downgrade and revocation fields are checked before read/oracle. Caller metadata is comparison-only after the selected reader runs; oracle selection remains independent. |
| F06 independent assurance | Closed by 16 literal A3 tests, matrix assertions inside those methods, raw production traces, four compiled/killed A3 production mutants and four killed A2 regression mutants. |
| F07 checkpoint integrity | Closed by this A3 checkpoint, machine-consumed SQL evidence, explicit unique/raw count arithmetic, exact commands, hashes, boundaries and retained states. |
| F01 preservation | Raw-only ingress, canonical envelope/signature/nonce/freshness/idempotency/scope/lease/fence checks, non-public typed services, and health/version-only host surfaces remain intact. |
| F05 preservation | Frozen readiness, audit receipt, privacy/minimization, freshness, fail-closed 503 behavior and immutable evidence requirements remain covered by the passing regression suite. |

## Effective-owner closure: 14/14

The literal catalog has 14 entries, 14 distinct owner interfaces, and the executable validator proves constructor graph, capability surface, trusted descriptor and denial traces:

1. Nexa ERP business runtime
2. Control Plane
3. Acceptance Verifier
4. Durable control-plane persistence
5. Trusted issuer key registry
6. KMS/HSM signing
7. Authoritative evidence reader
8. Immutable audit evidence
9. Lifecycle controller
10. Backup/recovery authority
11. Purge authorizer
12. Purge executor
13. Export authorizer
14. Export delivery executor

The five formerly open effective owners—Control Plane, Acceptance Verifier, durable persistence, authoritative reader and lifecycle controller—now have one effective constructor-graph owner each. Public partial nonce/idempotency/lease/state mutation facets do not exist and cannot be cast or injected.

## Exact A3 tests

All 16 required literal `[Fact]` methods were discovered and passed:

1. `A3_CompositeProviderHasOnePinnedOwnerAndOneAtomicMutationCapability`
2. `A3_ExportedOrInjectablePartialNonceIdempotencyLeaseAndStateMutationIsImpossible`
3. `A3_SelfAttestedProviderOrLifecycleIdentityVersionArtifactIsRejectedBeforeSnapshotUse`
4. `A3_All14ResponsibilitiesHaveOneCatalogOwnerAndOneEffectiveOwner`
5. `A3_AuthorizeThenConsumeExactGrantAndApprovedPlanThroughRawProductionPath`
6. `A3_ExactCompletedReplayReturnsOnlyOriginalGrantBoundOutcome`
7. `A3_EveryStoredGrantIssuerActorPolicyTenantPlanVersionEvidenceLeaseFenceAndExpirySubstitutionFailsBeforeLifecycle`
8. `A3_MissingDuplicateConsumedStaleOrAmbiguousGrantFailsClosedWithoutAtomicCall`
9. `A3_CallerSnapshotGrantAndApprovedPlanClaimsRemainComparisonOnly`
10. `A3_ServerPinnedReaderIdentityVersionArtifactSetSelectsEveryReaderExactlyOnce`
11. `A3_OracleReceivesOnlyFactsFromServerSelectedReaders`
12. `A3_CallerSelectedReaderVersionUpgradeDowngradeArtifactOrSchemaNeverSelectsReader`
13. `A3_MissingDuplicateUnexpectedRevokedOrStalePinnedReaderFailsBeforeReadAndOracle`
14. `A3_CanonicalOfflineSqlGenerationIsStableAcrossTwoFreshProcesses`
15. `A3_CheckpointSqlEvidenceMatchesMachineCapturedCanonicalResultExactly`
16. `A3_WrongMigrationEndpointOptionInputHashNewlineEncodingSizeOrSqlHashFailsEvidenceGate`

Result: `16 passed; 0 failed; 0 skipped`. Looped substitution matrices remain inside the literal methods and do not inflate unique counts.

## Production-mutant evidence

All mutations were made only in isolated disposable copies, compiled, killed by their intended assertion, and removed. One preliminary M04 attempt lacking Git metadata was corrected and rerun; it is not part of the valid campaign.

| ID | Mutated production SHA-256 | Intended enforcement and result |
|---|---|---|
| A3-M01-PARTIAL-MUTATION-API-BYPASS | `F8142A5599903C63A44F850D492388728D09246151FD6097DF9A089A85A0109A` | Exported/injectable partial mutation capability; killed by A3 test 2. |
| A3-M02-AUTHORIZATION-PROVIDER-SUBSTITUTION | `3F668217A5A82A75A231A3E37F88EE3F30749079AB06ED5EC118FF39785B25BF` | Provider substitution; killed by the stored-grant exact-comparison matrix. |
| A3-M03-CALLER-READER-VERSION-DOWNGRADE | `EBDF15EF56BA591CC6ED8EAC2215C76447546BD7A603B19A8DC749D0285020FB` | Caller reader downgrade selection; killed before authoritative read/oracle. |
| A3-M04-SQL-EVIDENCE-PRODUCTION-DRIFT | `0069AD7667B2AF9ED3EA357BA6D9C8B093FFA6B571424A9B9D3F7418BB844050` | Copied migration installation operation removed; killed by the machine checkpoint evidence gate. |

`A3_mutants_total=4`
`A3_mutants_compiled=4`
`A3_mutants_killed_by_intended_assertion=4`
`A3_mutants_survived=0`
`A3_mutants_invalid=0`

The original A2 request-as-authority, lifecycle-version-gate, reader-cardinality and readiness-freshness production mutants were rerun and killed:

`A2_regression_mutants_total=4`
`A2_regression_mutants_killed=4`
`A2_regression_mutants_survived=0`

## Canonical offline SQL evidence

The procedure runs in the authorized ERP source-contract test with `IMigrator.GenerateScript`, `MigrationsSqlGenerationOptions.Default`, inert `127.0.0.1:1`, pooling disabled, and an EF connection interceptor. It generates REV869A→REV869B and REV869B→REV869A in memory, normalizes CRLF and lone CR to LF only, performs no trim/format/rewrite, encodes UTF-8 without BOM, and records bytes, LF count and uppercase SHA-256. Two fresh worker processes returned byte-identical JSON. Temporary JSON and disposable mutant directories were deleted.

The evidence `Commit` is the exact authorized source baseline. This avoids an impossible self-referential final-commit hash; each actual SQL input is independently bound below by SHA-256, and the final commit is supplied by the post-commit handoff.

Commands and environment:

```powershell
$env:ConnectionStrings__NexaErp = "Host=127.0.0.1;Port=1;Database=rev869b_no_connect;Username=no_connect;Timeout=1;Pooling=false"
$env:NexaErp__ExpectedDatabase = "rev869b_no_connect"
dotnet ef migrations list --no-connect --project src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj --startup-project src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj --context NexaErpDbContext
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Rev869BCorrection17SourceContractTests.A3_CanonicalOfflineSqlGenerationIsStableAcrossTwoFreshProcesses|FullyQualifiedName~Rev869BCorrection17SourceContractTests.A3_CheckpointSqlEvidenceMatchesMachineCapturedCanonicalResultExactly|FullyQualifiedName~Rev869BCorrection17SourceContractTests.A3_WrongMigrationEndpointOptionInputHashNewlineEncodingSizeOrSqlHashFailsEvidenceGate"
```

A3_CANONICAL_SQL_EVIDENCE_JSON_BEGIN
{"Commit":"16dcf5a61956350d690ceeb81bd27012d456e256","SdkVersion":"10.0.303","RuntimeVersion":".NET 10.0.11","EfCliVersion":"Entity Framework Core .NET Command-line Tools\r\n10.0.10","EfCoreVersion":"10.0.10.0","NpgsqlVersion":"10.0.3.0","OperatingSystem":"Microsoft Windows 10.0.19045","Culture":"en-US","ConnectionString":"Host=127.0.0.1;Port=1;Database=rev869b_no_connect;Username=no_connect;Timeout=1;Pooling=false","UpFrom":"0","UpTo":"20260824032638_AdvanceInitialBaseline","DownFrom":"20260824032638_AdvanceInitialBaseline","DownTo":"0","GenerationOptions":"Default","NewlineRule":"CRLF and lone CR to LF only; no trim, format, rewrite, or execute","EncodingRule":"UTF-8 without BOM","SourceHashes":{"src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260824032638_AdvanceInitialBaseline.cs":"D22DAC92B6F8FE2F310258C18853A34975E15E2F00A7D00A1CA0B6090B5B6B04","src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260824032638_AdvanceInitialBaseline.Designer.cs":"55557BFB59AF842BAC2EA6D0F7A261049339E5A0A4927327CD277F386CF5727A","src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.cs":"84995702E8A209935D165210A638CEE2E1E6B2394844974D54B773A5B30943B5","src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869A.cs":"C7E4FF487AA69B1941FFB423B9E845753092F011F0D6CDE3D6DE3C7F4C8B4583","src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs":"605B68D6A29C8D9536477EA491B945B17971C977CB092F2D8A609035208EE3E3","src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs":"704C44F521AD08A07C48762842A5FB458DA3DC1F0D0C177037F1A942DBEDE5F0"},"UpByteCount":1237305,"UpLfCount":5826,"UpSha256":"786B43B041EC71E730A22430CCB939A0ED8C7B8FC78C5B4CB83037AF0979EED1","DownByteCount":4409,"DownLfCount":169,"DownSha256":"8C28E419015584F3A310D447CDFD4125BA5E865981FDD5DBD20D138C92BDB3E1","ConnectionOpenCount":0,"MigrationApplyCount":0}
A3_CANONICAL_SQL_EVIDENCE_JSON_END

## Validation and test arithmetic

| Gate | Result |
|---|---|
| Control-plane warning-as-error build | 0 warnings; 0 errors |
| Complete solution warning-as-error build | 0 warnings; 0 errors |
| A3 architecture subset | 13 passed; 0 failed; 0 skipped |
| Complete Phase-A control assembly | 63 passed; 0 failed; 0 skipped |
| Focused REV869B ERP subset | 79 passed; 0 failed; 0 skipped |
| Complete ERP non-PostgreSQL assembly | 453 passed; 0 failed; 0 skipped |
| Canonical A3 ERP subset | 3 passed; 0 failed; 0 skipped |
| PowerShell 5.1 AST | 24 files; 0 parse errors; no scripts executed |
| EF no-connect discovery | 13 migrations; REV869A and REV869B unique, adjacent, positions 12 and 13; applied status intentionally unknown |
| Source/model/snapshot parity | Passing focused and full source-contract/model-differ coverage |
| SQL process equality | 2 fresh processes; exact JSON/byte equality |
| Database connection/application counters | 0 opens; 0 migration applications |
| PostgreSQL discovery/execution | 87 unique discovered; 0 executed |
| Incremental/cumulative diff checks | exit 0 / exit 0 |
| Boundary/security scans | 10 changed target files; 0 outside allowlist; 1,240 added lines; 0 secret, prohibited-operation or conflict-marker hits |
| Temporary artifacts | no mutant/evidence artifacts retained |

Counting basis:

- New A3 tests: `16`.
- Final Phase-A unique test total: `63`.
- Focused REV869B subset: `79` (contained in the ERP total).
- Complete ERP non-PostgreSQL total: `453`.
- Unique total across the two assemblies: `63 + 453 = 516`.
- Raw overlapping formal invocation pass events: `13 + 63 + 79 + 453 = 608`.
- The separate three-test canonical rerun is diagnostic confirmation and is not added to the stated four-invocation raw formal total or any unique total.
- PostgreSQL tests discovered: `87`; executed: `0`.

Principal validation commands:

```powershell
dotnet build tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-restore -warnaserror
dotnet build SESS.NexaERP.slnx --no-restore -warnaserror
dotnet test tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~ArchitectureFreezeContractTests.A3_&FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Rev869B&FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --list-tests --filter "FullyQualifiedName~Postgres" --logger "console;verbosity=minimal"
git diff --check 8c78f6a480fcbf86afbf9f5460598ece5b8d6732 -- <exact-ten-file-allowlist>
git diff --check 51476760adcea9ed7babbc04d642e53e371c6941 -- <exact-ten-file-allowlist>
```

## Final source/test artifact SHA-256 before checkpoint commit

| File | SHA-256 |
|---|---|
| `Rev869BControllerMessagesV1.cs` | `A156C11AEE62954504F348394E8C92A5FD9BD4B3FAD37635DF0EA7373B7E1BBE` |
| `ControlPlaneOptions.cs` | `7AA59A7308BE18C99099ACAEA1FE94AF8D5340D631A9F692721DBF02E0703254` |
| `Rev869BExecutionBinding.cs` | `627E1BD4B08DE0F429E5327E0169DCE8EC8DEF5EE745B78CE7526866D01A9745` |
| `Rev869BControllerStateMachine.cs` | `E80EA9F58431CB150A1296E170BE54586E99FDECDE3EE57FEA205818C952AA05` |
| `SignedEnvelopeService.cs` | `CA3AC0715D6C79E516AF23A4DB745C84F0A249378F053873874139BCA290F5C5` |
| `AcceptanceVerifierOptions.cs` | `B742F7285117676C719F27A7D9CE533B7442F10FB71BDDF731A02135D4219694` |
| `ClosedEvidenceVerifierV1.cs` | `95C7FB4C8B5E08E40FD0672037B7A70C69F0E6A0876B7B06A6161E2027BF49EB` |
| `ArchitectureFreezeContractTests.cs` | `6EEC68E7E98762631136BB3F11CDC2A272B0DF17F40FF2A06C576A41F44899A5` |
| `Rev869BCorrection17SourceContractTests.cs` | `CB3A379B7C553705AB10024E98A1386871C7D34C0937F827EF2D46F2B298297B` |

## Prohibited operations and remaining prerequisites

No PostgreSQL access/test execution, migration application, Phase B, Correction 2, provisioning, deployment, production access, real key/credential/trust-root use, lifecycle/recovery/purge/export execution, frontend work, or unrelated Purchase/Stores work occurred. Migration/model/snapshot/project/solution/helper files were not changed. The exact target-scoped worktree is required clean after the one commit.

External prerequisites remain: deployed composite durable persistence; workload identity/IAM and private networking; production issuer/policy stores and non-exportable KMS/HSM keys; authoritative least-privilege readers and pinned oracle artifacts; immutable audit/evidence storage; HA/failover; authorized PostgreSQL behavioral/concurrency/rollback/restart/PITR evidence; backup/restore/DR, scale/load/chaos, monitoring, runbooks, training and management approval.

## Retained states and exact next gate

`phase_a_correction_a3_source_implementation_state=COMPLETE_PENDING_INDEPENDENT_REVIEW`

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

Exact next gate: a fresh independent report-only source architecture/security review of the committed A3 diff. It must remain separate from implementation.
