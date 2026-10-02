# REV869B Option-A Phase-A Correction A2 Failure Reconciliation

Date: 2026-08-16

Reconciliation mode: separate, report-only, source-only

Starting HEAD: `78bd47837d656cb0b6914c870a86f97e40412f15`

Starting parent: `aca12c48cbfbd59fba56264003b38f90e62b7ef8`

Reviewed A2 source commit: `aca12c48cbfbd59fba56264003b38f90e62b7ef8`

Independent-review report: `outputs/rev869b_external_controller_phase_a_correction_a2_independent_source_safety_review.md`

Independent-review SHA-256: `057D12427458B2B1348156B1DA38A920C073F9F9D28508F1DB319C4D5DCA41DC`

## 1. Reconciliation verdict and selected decision

`PHASE_A_CORRECTION_A3_SOURCE_ONLY_GATE=GO`

F02, F03, F04, F06, and F07 are confirmed. All four blockers are deviations from the already-frozen Option-A Phase-A architecture, not reasons to change that architecture. The required correction is to make the existing source contracts enforce one inaccessible atomic mutation boundary, exact server-pinned provider and grant provenance, server-selected reader versions, independently decisive tests/mutants, and one deterministic SQL-evidence procedure.

This GO is a bounded recommendation for a separate management authorization. It is not A3 implementation authority. It authorizes no Phase B persistence implementation, PostgreSQL action, migration change/application, provisioning, deployment, external service, credential, production operation, or access to `../legacy-reference/`.

## 2. Stage-0 gate

| Gate | Reproduced evidence | Result |
|---|---|---|
| HEAD | `78bd47837d656cb0b6914c870a86f97e40412f15` | PASS |
| Parent | `aca12c48cbfbd59fba56264003b38f90e62b7ef8` | PASS |
| Branch | `master` | PASS |
| HEAD subject | `REV869B Phase-A Correction A2 independent source safety review` | PASS |
| Parent subject | `REV869B Phase-A Correction A2 source safety` | PASS |
| Review path | required report exists | PASS |
| Review SHA-256 | `057D12427458B2B1348156B1DA38A920C073F9F9D28508F1DB319C4D5DCA41DC` | PASS |
| HEAD boundary | exactly one added independent-review report | PASS |
| Target-scoped entry status | clean | PASS |
| `../legacy-reference/` | untracked in repository status metadata and absent from the index; contents not enumerated, read, or modified | PASS within prohibition |

The frozen architecture specification, A1 independent review, A1 failure reconciliation, A2 checkpoint, and A2 independent review were read completely. Their reproduced SHA-256 values are respectively `3F0BC461865D69E3D9827D763D7C403E3BD4E82ECF488AE4FDF3E48D9722DDB8`, `9320CAD73798099548C8DB1ABA503870AAC2E11D852AA2AD0DCD28709A60A0AD`, `B108365830F6CE2AE1ED97835980601484A7C1AE749048AFC4535457DCC360A3`, `319271FFF2E8D2E9EB35783FFD6100C1C5223EE13D8AD26A8CBA04ACF6456F47`, and `057D12427458B2B1348156B1DA38A920C073F9F9D28508F1DB319C4D5DCA41DC`.

No entry mismatch exists. Reconciliation may proceed.

## 3. Classification summary

| Finding | Classification | Reason |
|---|---|---|
| F02 ownership and provenance | `SOURCE_CORRECTION` | Remove exported partial mutation facets and bind provider/controller identity to a server-pinned contract. |
| F03 lifecycle and composite atomic enforcement | `SOURCE_CORRECTION` | Bind the exact stored grant and approved plan before the one atomic decision. |
| F04 evidence-reader/oracle isolation | `SOURCE_CORRECTION` | Select reader ID/version/artifact from the server-owned expectation, not caller metadata. |
| F06 tests and production-mutant coverage | `SOURCE_CORRECTION` | Replace weak/derived proofs with tests and mutants at the vulnerable production points. |
| F07 checkpoint/evidence integrity | `SOURCE_CORRECTION` | Canonicalize offline SQL generation and machine-bind the checkpoint to the independently generated evidence. |

Actual durable storage, database constraints/concurrency, deployed workload identity, real KMS, external readers, WORM storage, and operational evidence remain later `PHASE_B_ITEM` or `EXTERNAL_PREREQUISITE` work. None is required to correct the Phase-A contract defects.

## 4. Four-blocker root-cause table

| Blocker | Visible failure | Root cause | Smallest closure |
|---|---|---|---|
| Composite persistence partial APIs | nonce, idempotency, and lease can be mutated outside `ExecuteAtomicallyAsync` | `IDurableControlPlanePersistenceProvider` inherits legacy public mutation facets; A2 checked constructor count, not exported method capability | remove public mutation inheritance/methods; expose only pinned identity/version, authoritative snapshot read, committed-result read if strictly read-only, and one atomic decision |
| Stored-grant substitution | a heavily substituted provider-owned grant still reaches a transition | proposed execution authorization and stored management grant share a loose `AuthorizationBindingV3`; raw ingress carries no exact grant reference/approved-plan binding; state machine checks only operation/scope/resource/time | introduce an immutable signed grant reference and approved-intent digest; compare every exact binding field before lifecycle evaluation; pin provider and lifecycle authority identity/version |
| Caller-selected reader version | caller bundle version chooses `ResolveAsync` and therefore `ReadAsync` | server expectation pins scope/time but omits required reader version/artifact set; options pin only reader IDs | put exact compatible reader descriptors in server-owned expectation/policy; resolve from that list before comparing caller transport; reject downgrade/drift before reader/oracle calls |
| SQL evidence mismatch | checkpoint hashes differ with no migration/model change | checkpoint values came from an undocumented or differently normalized producer and were manually recorded without a machine-enforced join to the pinned in-process generator | define one normalized in-process generation/hash contract, run twice, capture machine-readable evidence, and make checkpoint validation consume that exact capture |

## 5. F02 reconciliation — ownership, provenance, and composite persistence

### Exact source and responsible component

- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1462-1529` — exported persistence APIs; durable persistence component.
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:640-650` — injectable provider, reader registry, and lifecycle controller; Control Plane component.
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:764-779` — provider self-identity/version comparison; Control Plane component.
- `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs:229-242` — constructor-owner validation; ownership enforcement component.

### Frozen responsibility owner

The durable control-plane PostgreSQL contract, schema, and atomic semantics are owned by Control Plane Engineering and operated by DBA Operations. The Control Plane is the sole lifecycle decision authority. Phase A defines the contract only; Phase B supplies the durable implementation later.

### Required invariant

There is exactly one effective durable owner. No exported or injectable production API may separately mutate nonce, idempotency, authorization, lease/fence, lifecycle, attempt, recovery, purge/export, response, evidence correlation, or audit-outbox state. Provider identity/version and lifecycle-authority identity/version are pinned by trusted server configuration/readiness policy, not self-attested by the injected object.

### Current behavior and complete mutation API inventory

Public/injectable persistence APIs are:

1. `INonceRegistrationAuthority.RegisterNonceAsync` — partial nonce mutation.
2. `IIdempotencyAuthority.ClaimAsync` — partial idempotency ownership mutation.
3. `IIdempotencyAuthority.ReadCommittedResultAsync` — read-only, but coupled to the legacy partial interface.
4. `ILeaseFenceAuthority.ReadCurrentAsync` — read-only legacy facet.
5. `ILeaseFenceAuthority.AcquireAsync` — partial lease/fence mutation.
6. `ILeaseFenceAuthority.RenewAsync` — partial lease/fence mutation.
7. `ILeaseFenceAuthority.ExpireAsync` — partial lease/fence mutation.
8. `ILifecycleStateAuthority.ReadAsync` — read-only legacy facet.
9. Marker facets `IAuthorizationStateAuthority`, `IExecutionAttemptAuthority`, `IRecoveryQuarantineAuthority`, `IExportStateAuthority`, and `IPurgeStateAuthority` — no current methods but exported as separately castable ownership surfaces.
10. `IDurableControlPlanePersistenceProvider.ReadAuthoritativeSnapshotAsync` — intended composite read.
11. `IDurableControlPlanePersistenceProvider.ExecuteAtomicallyAsync` — intended sole mutation.

The composite provider inherits items 1-9. A consumer can cast it to a partial facet and call a mutation without the lifecycle, grant, attempt, response, and audit outbox. A2 proves that the Control Plane constructor has one composite parameter but does not prove that no caller can compose partial success.

Provider provenance is also self-referential: `durableProvider.ProviderIdentity` and `ProviderVersion` are compared to values returned by the same provider snapshot, and `ExpectedProviderVersion` is populated from that same object. No independent signed/pinned expected identity/version participates. `ILifecycleControllerAuthority` is separately injectable and has no identity/version contract.

### Exact bypass path

A consumer obtains the injected composite provider, casts it to `INonceRegistrationAuthority`, `IIdempotencyAuthority`, or `ILeaseFenceAuthority`, and calls a partial mutation. Alternatively, a substituted provider supplies matching self-reported identity/version in both properties and snapshot, then returns a self-consistent atomic receipt. The raw authority cannot distinguish it from the approved provider contract.

### Root cause

A2 treated one constructor parameter and interface inheritance as equivalent to one mutation capability. Ownership was validated by type/count, while capability closure and independently pinned runtime provenance were not validated.

### Security and operational impact

Partial nonce/idempotency/lease state can diverge from lifecycle, grant, attempt, response, and audit state. A substituted provider or lifecycle controller can self-attest its own version. Consequences include stuck or replayable requests, stale fencing, duplicate destructive execution, inconsistent recovery, and audit records that do not prove the actual authority.

### Smallest safe correction

Remove the mutation facet inheritance and exported mutation interfaces from the Phase-A durable contract. Preserve only an exact read-only snapshot/result contract and one `ExecuteAtomicallyAsync` mutation. Add a trusted server-owned provider/lifecycle descriptor with exact identity, semantic contract version, artifact hash, and readiness-policy version. The raw authority compares injected implementations and returned snapshots to that pinned descriptor before any decision. No database implementation is added.

### Required tests

Positive: `A3_CompositeProviderHasOnePinnedOwnerAndOneAtomicMutationCapability`.

Denial/adversarial: `A3_ExportedOrInjectablePartialNonceIdempotencyLeaseAndStateMutationIsImpossible` and `A3_SelfAttestedProviderOrLifecycleIdentityVersionArtifactIsRejectedBeforeSnapshotUse`.

### Required production mutant

`A3-M01-PARTIAL-MUTATION-API-BYPASS`: temporarily reintroduce an exported nonce/idempotency/lease mutation and invoke it before the composite operation in the raw production authority. It must compile and be killed by exported-surface reflection plus a provider call trace proving zero partial calls.

### Acceptance formula and evidence

```text
exported_partial_mutation_methods = 0
AND injectable_partial_persistence_owners = 0
AND composite_mutation_methods = 1
AND pinned_provider_identity_version_artifact_matches = true
AND pinned_lifecycle_identity_version_artifact_matches = true
AND partial_calls_before_or_after_atomic_decision = 0
AND denied_provenance_cases_with_snapshot_or_lifecycle_calls = 0
```

Evidence: exported-interface inventory, constructor graph, pinned descriptor fixture, call trace, wrong/self-attested provider matrix, and one-commit/no-change fault-injection results.

Classification: `SOURCE_CORRECTION`.

## 6. F03 reconciliation — authorization provenance and lifecycle consumption

### Exact source and responsible component

- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:573-608` — raw command lacks exact stored-grant reference; contracts component.
- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:979-992` and `1052-1096` — resolved authorization, stored binding, snapshot, and command contracts.
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:721-846` — intent/resolver/snapshot/current-command composition; Control Plane.
- `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs:224-353` — lifecycle and stored-grant enforcement; lifecycle controller.
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:847-888` — proposed transition and atomic receipt validation.

### Frozen responsibility owner

The Deployment/Orchestration Controller and management policy own grant creation. The durable control provider owns immutable grant consumption state. The Control Plane alone verifies the raw command and grant, evaluates lifecycle rules, and atomically commits consumption and transition. Executors do not create authority.

### Required invariant

Grant creation and consumption are joined by one immutable reference and digest covering grant issuer/key/version/signature, authorization ID, authorized operation, original authorizer identity/role, executor subject class where applicable, tenant/organization/database/resource/resource version, canonical approved-intent/plan digest, evidence manifest, lease/fence/epoch, policy version/row/artifact, not-before, expiry, and one-time state. Caller values are comparison claims only.

### Current creation-to-consumption trace

For `AUTHORIZE_*`, raw ingress parses a signed command, resolves current caller authorization, constructs a new active `AuthorizationBindingV3`, obtains a provider snapshot, evaluates a lifecycle row, and sends a proposed transition to `ExecuteAtomicallyAsync`; the provider is expected to persist the active grant.

For execution/completion/cancel/expire, a new current-operation authorization binding is built from the current caller. The provider snapshot supplies `CurrentAuthorization`. `ExpectedGrantOperation` maps the execution operation to the authorization operation. The state machine checks grant state, scope, resource type/id, mapped operation, and time. It does not perform an exact join on the remaining immutable grant/plan/policy fields.

### Provider, DI, caller, and test-double substitution paths

1. A substituted durable provider controls both self-reported identity/version and `CurrentAuthorization`.
2. A substituted `ILifecycleControllerAuthority` can propose a result; the raw authority checks selected receipt fields but does not pin the controller implementation/artifact.
3. The caller supplies expected state/version, operation, approved parameters, evidence requirement IDs, lease ID/fence, and no exact grant ID/hash. Snapshot comparisons protect some fields but no exact grant/approved-plan reference exists.
4. Test doubles construct stored grants directly and are accepted when operation/scope/resource/time are coherent, even if all other grant fields differ.

### Exact bypass path

Supply or return an active stored grant for the same scope/resource/authorization operation and valid time, but substitute its authorization ID, issuer, original subject/workload/role, policy version/row, grant hash, resource version, canonical request hash, evidence manifest hash, or approved plan. The state machine still selects the legal row. The independent A2 reproduction changed these fields and `PREPARE` still reached `Preflight -> Provisioning`.

### Root cause

`AuthorizationBindingV3` is overloaded for two different concepts: the newly resolved executor authorization and the existing management grant. The raw contract carries no exact grant reference or approved-plan digest, and the lifecycle controller has no complete immutable comparison to perform.

### Security and operational impact

An authorization for one approved plan can be replayed or substituted for another plan, policy decision, actor, resource version, or evidence manifest. This can execute migration, recovery, drop, purge, or export work beyond the exact management approval while still producing a superficially valid transition and audit outcome.

### Smallest safe correction

Separate `ExecutionAuthorizationV3` from `StoredAuthorizationGrantV3`. Add exact grant ID/hash and approved-intent digest claims to raw ingress, validate them only by comparison with the provider-owned signed grant, and bind every immutable field listed above. Pin provider and lifecycle authority provenance. Missing, unknown, substituted, stale, expired, consumed, wrong-operation, wrong-tenant/resource/version, wrong-policy, wrong-plan, wrong-lease/fence, or ambiguous grants fail before lifecycle evaluation and before the atomic provider call.

### Required tests

Positive: `A3_AuthorizeThenConsumeExactGrantAndApprovedPlanThroughRawProductionPath` and `A3_ExactCompletedReplayReturnsOnlyOriginalGrantBoundOutcome`.

Denial/adversarial: `A3_EveryStoredGrantIssuerActorPolicyTenantPlanVersionEvidenceLeaseFenceAndExpirySubstitutionFailsBeforeLifecycle` and `A3_MissingDuplicateConsumedStaleOrAmbiguousGrantFailsClosedWithoutAtomicCall`.

### Required production mutant

`A3-M02-AUTHORIZATION-PROVIDER-SUBSTITUTION`: after the authoritative snapshot read, replace `CurrentAuthorization` with a request/resolver-derived grant or bypass the exact grant/approved-intent digest comparison. It must compile and be killed by the raw-path field matrix with zero lifecycle/atomic calls.

### Acceptance formula and evidence

```text
grant_creation_records_exact_binding = ALL_FIELDS
AND grant_consumption_matches_exact_binding = ALL_FIELDS
AND caller_or_resolver_fields_promoted_to_stored_grant = 0
AND missing_or_ambiguous_grants_accepted = 0
AND stale_expired_consumed_wrong_plan_wrong_policy_successes = 0
AND denied_cases_with_lifecycle_calls = 0
AND denied_cases_with_atomic_calls = 0
AND exact_grant_consumption_commits = 1
```

Evidence: raw canonical vectors, signed grant fixtures independent of implementation, provider/lifecycle call traces, every-field mutation table, concurrent one-time consumption, replay result equality, and exact audit grant hash.

Classification: `SOURCE_CORRECTION`.

## 7. F04 reconciliation — server-owned reader selection and oracle isolation

### Exact source and responsible component

- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1190-1203` — expectation lacks reader descriptors.
- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1548-1566` — descriptor/read interfaces.
- `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs:13-65` — required reader IDs but no pinned versions/artifacts.
- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:521-603` — caller declaration determines `ResolveAsync` version and then `ReadAsync` descriptor.

### Frozen responsibility owner

Security owns reader identities/keys and compatibility policy. DBA/Data Assurance own source schemas and reader artifacts. The Acceptance Verifier owns acquisition orchestration and verdict calculation. The caller owns no reader selection or oracle input.

### Required invariant

The server-owned signed expectation/readiness policy specifies the exact required reader multiset and each reader's ID, compatible semantic version or exact version, artifact hash, schema, stage, service identity, source identity, and allowed downgrade policy. Descriptor resolution uses only that trusted selection. Caller bundle metadata is compared afterward and never chooses the reader. Oracle selection remains independently pinned and receives only verified returned facts.

### Current behavior

Exact reader ID cardinality is enforced. A server-owned scope/time expectation is resolved. Every reader is invoked, returned bytes are compared to caller transport, signatures and limits are verified, and the oracle receives only returned bundles. However, options pin only reader IDs, the expectation contains no reader set, and the verifier passes `declaredBundle.ReaderVersion` to `ResolveAsync`. Any version the registry recognizes can therefore become the authoritative reader at caller choice.

### Exact bypass path

Declare the required reader ID with an older or alternate registered version. The verifier resolves that version, `ReadAsync` returns a consistent signed bundle from it, and all current descriptor/signature/byte checks pass. There is no comparison to a server-pinned required version/artifact or downgrade floor.

### Root cause

A2 correctly moved fact acquisition to readers but left descriptor selection coupled to the caller-carried bundle. Identity cardinality was treated as complete reader provenance; version/artifact selection was omitted from the server-owned expectation.

### Security and operational impact

An older or different reader can change fields, query semantics, privacy filtering, snapshot/watermark behavior, bounds, or source identity and cause a verdict under unauthorized version drift. A permissive older artifact can turn authoritative collection into an acceptance bypass without modifying the oracle.

### Smallest safe correction

Add exact pinned reader descriptors or signed compatible-version constraints to the server-owned expectation and verifier options. Resolve descriptor/read operations from that trusted list. Reject missing, duplicate, extra, unexpected, revoked, stale, downgrade, artifact, schema, stage, identity, or compatibility mismatch before `ReadAsync`; reject returned mismatch before oracle. Do not let reader selection influence oracle selection.

### Required tests

Positive: `A3_ServerPinnedReaderIdentityVersionArtifactSetSelectsEveryReaderExactlyOnce` and `A3_OracleReceivesOnlyFactsFromServerSelectedReaders`.

Denial/adversarial: `A3_CallerSelectedReaderVersionUpgradeDowngradeArtifactOrSchemaNeverSelectsReader` and `A3_MissingDuplicateUnexpectedRevokedOrStalePinnedReaderFailsBeforeReadAndOracle`.

### Required production mutant

`A3-M03-CALLER-READER-VERSION-DOWNGRADE`: change descriptor resolution back from the trusted expected descriptor to `declaredBundle.ReaderVersion`. It must compile and be killed by a two-valid-version fixture proving the downgraded reader receives zero calls and the oracle receives zero calls.

### Acceptance formula and evidence

```text
server_pinned_reader_multiset = required_reader_multiset
AND descriptor_resolution_inputs_from_caller = 0
AND count(each_server_selected_reader_ReadAsync) = 1
AND downgrade_or_unexpected_version_ReadAsync_calls = 0
AND invalid_reader_cases_oracle_calls = 0
AND oracle_descriptor_selection_independent_of_reader_metadata = true
AND oracle_inputs = verified_server_selected_reader_facts_only
```

Evidence: two-version registry traces, signed expected descriptor fixture, downgrade/revocation/compatibility matrix, caller-declaration substitutions, exact error codes, and zero-read/zero-oracle counters.

Classification: `SOURCE_CORRECTION`.

## 8. F06 reconciliation — why passing tests and mutants missed the defects

### Exact source and responsible component

- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:129-248` — ownership/provenance tests.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:399-462` — lifecycle mutation matrix.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:819-913` — reader closure tests.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:1964-2044` — reader fake echoes caller-requested version.
- `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs:446-466` — real offline SQL generation and pinned evidence.

### Frozen responsibility owner and invariant

Independent Assurance owns literal expected behavior, complete complement matrices, production-path tests, and decisive mutants. Tests must reject vulnerable implementations, not merely validate self-consistent fakes or catalog entries.

### Current behavior and root cause

The 31 A2 tests and 50-test assembly pass because:

1. Ownership tests count constructor/provider types but do not reject inherited exported mutation capabilities.
2. Lifecycle tests mutate command/snapshot top-level state, version, role, evidence, lease/fence, and attempt, but not every nested stored-grant field against an independently frozen expected grant.
3. The reader fake returns a descriptor whose version echoes the caller-supplied version, so it cannot distinguish server selection from caller selection.
4. The four mutants target request-as-state, version conjunctions, reader cardinality, and readiness freshness. They are valid and killed, but none targets partial persistence methods, exact stored-grant binding, or reader-version downgrade.
5. The SQL test validates the real generator, while no Phase-A evidence gate compares the checkpoint's claimed values to that machine result.

This is assurance-oracle incompleteness, not a reason to distrust all passing tests.

### Security and operational impact

Named tests overstate closure, allowing management to receive green counts while critical authorization, ownership, version-drift, and evidence-integrity bypasses remain.

### Smallest safe correction

Keep literal fixtures independent, replace capability-counting with exported-method and call-trace proofs, add complete stored-grant mutation and two-version reader fixtures, and bind checkpoint evidence to the real in-process SQL generator. Preserve the four original A2 mutants as regression mutants but add the four A3 mutants below as the decisive A3 campaign.

### Required tests

Positive: all 16 exact A3 tests in section 12.

Denial/adversarial: every negative test in sections 5-7 and the SQL evidence mismatch test in section 10.

### Required production mutant

All four A3 mutants in section 13. Descriptor-only, string-only, invalid-input-only, and test-mirror variants are invalid.

### Acceptance formula and evidence

```text
new_A3_named_test_methods = 16
AND required_A3_tests_failed_or_skipped = 0
AND production_derived_expected_grant_or_reader_fixtures = 0
AND exported_capability_and_call_trace_proofs = PASS
AND decisive_A3_mutants = 4 compiled / 4 intended kills / 0 survivors / 0 invalid
AND original_A2_mutants = 4 intended kills / 0 survivors
```

Classification: `SOURCE_CORRECTION`.

## 9. F07 reconciliation — SQL and checkpoint evidence integrity

### Exact source and responsible component

- `outputs/rev869b_external_controller_phase_a_checkpoint.md:110-111` — conflicting A2 values; checkpoint owner.
- `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs:446-466` — current in-process generator and pinned values; Independent Assurance.
- `global.json:3-4` — SDK baseline and roll-forward behavior; build environment.
- `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj:9-15` — EF Core 10.0.10 and Npgsql EF 10.0.3 inputs.

### Frozen responsibility owner and invariant

Independent Assurance owns reproducible expected evidence. Every reported byte count/hash must identify exact source commit, migrations, compiled model/snapshot, package/tool versions, generation options, encoding, newline normalization, and command/result capture.

### Current behavior

At reconciliation, `dotnet --version` is 10.0.303, `global.json` requests 10.0.302 with `latestFeature`, EF CLI is 10.0.10, EF packages are 10.0.10, and Npgsql EF is 10.0.3. The real test uses an inert loopback connection string without connecting, calls `IMigrator.GenerateScript(Rev869A, Rev869B)` and the reverse, hashes raw `Encoding.UTF8.GetBytes` output, and pins 324,914/11,720 bytes plus the older hashes. The A2 checkpoint reports 326,596/11,759 bytes and different hashes. A2 changed no migration/model/snapshot source.

### Exact failure path and root cause

The checkpoint accepted manually transcribed values from an undocumented producer that did not prove identical migration endpoints, compiled inputs, options, tool/runtime versions, newline behavior, or encoding. Because no machine-readable result was joined to checkpoint validation, different bytes could be reported while the real pinned test still passed. The repository cannot reconstruct the alternate producer; that missing provenance is the root cause. No new hash is invented in this reconciliation.

### Security and operational impact

Evidence custody is broken: reviewers cannot determine which SQL artifact was reviewed, whether the difference is environment normalization or different generated content, or whether an unreviewed artifact was substituted.

### Smallest safe correction

Use one canonical offline procedure:

1. Check out the exact reviewed commit with a clean target tree.
2. Record actual SDK, runtime, EF CLI, EF package, Npgsql package, OS, and culture values.
3. Record SHA-256 for both migration files, their designers, current model, snapshot, and SQL helper inputs.
4. Instantiate `NexaErpDbContext` in-process with inert `127.0.0.1:1`, pooling disabled, and no connection/open/apply call.
5. Use `IMigrator.GenerateScript` for exact `Rev869A -> Rev869B` and `Rev869B -> Rev869A`, with explicitly recorded `MigrationsSqlGenerationOptions`.
6. Normalize `CRLF` and lone `CR` to `LF`; perform no whitespace trimming, formatting, token rewriting, or SQL execution.
7. Encode normalized text as UTF-8 without BOM; record byte count, LF line count, and uppercase SHA-256.
8. Run in two fresh processes and require byte equality.
9. Emit one machine-readable JSON result; derive the test assertion and checkpoint table from that same result.
10. Fail if stored checkpoint values, command metadata, input hashes, or generated bytes differ.

Source/model parity remains a separate deterministic assertion. Raw unnormalized output may be recorded diagnostically but is not approval evidence.

### Required tests

Positive: `A3_CanonicalOfflineSqlGenerationIsStableAcrossTwoFreshProcesses` and `A3_CheckpointSqlEvidenceMatchesMachineCapturedCanonicalResultExactly`.

Denial/adversarial: `A3_WrongMigrationEndpointOptionInputHashNewlineEncodingSizeOrSqlHashFailsEvidenceGate`.

### Required production mutant

`A3-M04-SQL-EVIDENCE-PRODUCTION-DRIFT`: temporarily remove or invert a real REV869B production migration installation operation, such as the actual `Rev869BCommandContextSql.Install` invocation, while preserving compilation. The real in-process EF generator and checkpoint evidence gate must detect changed canonical SQL. Mutating only a descriptor, expected string, checkpoint text, or test mirror is invalid.

### Acceptance formula and evidence

```text
canonical_generation_processes = 2
AND canonical_up_bytes_run1 = canonical_up_bytes_run2
AND canonical_down_bytes_run1 = canonical_down_bytes_run2
AND canonical_hashes_match_machine_capture = true
AND checkpoint_values_match_machine_capture = true
AND source_model_snapshot_parity = true
AND database_connections = 0
AND migration_applications = 0
AND invented_or_manually_transcribed_hashes = 0
```

Classification: `SOURCE_CORRECTION`.

## 10. Fourteen-owner closure assessment

The catalog remains necessary but is not proof of effective ownership.

| # | Responsibility | Current result | Closure requirement |
|---:|---|---|---|
| 1 | NexaERP business runtime | PASS | Preserve no lifecycle/verdict authority. |
| 2 | Control Plane | FAIL | Pin provider/controller provenance and enforce exact stored-grant/plan binding before transition. |
| 3 | Acceptance Verifier | FAIL | Select exact readers from server-owned expectation, never caller version. |
| 4 | Durable control-plane persistence | FAIL | Remove exported partial mutations; retain one atomic mutation capability. |
| 5 | Trusted issuer/key registry | PASS | Preserve exact issuer/key/algorithm/time checks. |
| 6 | KMS/HSM signing | PASS | Preserve isolated non-caller signing interface. |
| 7 | Authoritative evidence reader | FAIL | Pin reader version/artifact/schema/stage and downgrade policy server-side. |
| 8 | Immutable audit/evidence | PASS | Preserve exact fail-closed receipts and minimized data. |
| 9 | Lifecycle controller | FAIL | Pin controller identity/artifact and require exact grant/approved-plan invariants. |
| 10 | Backup/recovery authority | PASS | Preserve separate owner and no Phase-A execution. |
| 11 | Purge authorizer | PASS | Preserve separate authorization owner. |
| 12 | Purge executor | PASS | Preserve separate execution owner. |
| 13 | Export authorizer | PASS | Preserve separate authorization owner and exact release. |
| 14 | Export delivery executor | PASS | Preserve separate delivery owner and ordered substates. |

### Five failed responsibilities

| Responsibility | Current effective owner | Required authoritative owner | Enforcement gap | Smallest closure | Source evidence required for PASS |
|---|---|---|---|---|---|
| Control Plane | `PhaseAControlPlaneAuthority`, but injected providers/controllers can self-attest | pinned Control Plane authority using frozen policy and exact composite dependencies | no independent provider/controller pin; incomplete grant join | add trusted descriptors and exact grant comparison before any delegate | constructor graph, descriptor pins, raw-path call trace, zero calls on mismatch |
| Acceptance Verifier | `PhaseAAcceptanceVerifierAuthority`, with reader version selected by caller declaration | verifier using server-owned expected reader set and independent oracle | caller version enters descriptor selection | resolve pinned descriptors first; compare caller bytes second | two-version call trace and zero oracle calls on drift |
| Durable persistence | composite interface plus public mutation facets | one provider with one atomic mutation capability | castable partial mutation paths | remove/inaccessible facet methods; one atomic method | exported-method reflection and fault-injection atomicity trace |
| Evidence reader | provider registry, but exact version chosen by caller bundle | Security/Data Assurance pinned reader descriptor | no server-owned version/artifact/downgrade policy | put descriptors in signed expectation/options | signed expected set, downgrade matrix, exact reader call counts |
| Lifecycle controller | separately injectable `ILifecycleControllerAuthority` | pinned Control Plane lifecycle authority over exact snapshot/grant | no identity/artifact pin; partial grant comparisons | pin controller and enforce complete immutable binding | wrong-controller fixture, complete grant matrix, exact literal transition outcome |

Source-only PASS requires 14/14 effective responsibilities, not only 14 catalog entries.

## 11. Exhaustive minimal A3 file allowlist

Maximum file count: 10. The list is exhaustive; no related file is implicit.

| # | Exact file | Finding mapping | Authorized purpose only |
|---:|---|---|---|
| 1 | `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs` | F02, F03, F04 | remove public partial mutations; define pinned provider/lifecycle descriptors, exact grant/approved-intent binding, and pinned reader expectation |
| 2 | `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs` | F02, F03 | validate exact trusted provider/lifecycle identity, version, artifact, and policy pins; no deployed configuration |
| 3 | `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs` | F02, F06 | validate exported capability and effective ownership closure |
| 4 | `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs` | F03 | enforce every immutable stored-grant and approved-plan field before a rule can match |
| 5 | `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs` | F02, F03 | use independently pinned provider/controller provenance and exact raw grant claims; eliminate self-attestation |
| 6 | `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs` | F04 | pin reader identity/version/artifact/schema/stage and compatible-version policy |
| 7 | `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs` | F04 | select descriptors from server expectation before caller comparison; enforce downgrade prevention and oracle isolation |
| 8 | `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs` | F02, F03, F04, F06 | exact production-path tests, independent fixtures, call traces, and A3 mutant killers |
| 9 | `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | F06, F07 | canonical real in-process SQL generation/normalization, double-run determinism, and checkpoint evidence gate; no database access |
| 10 | `outputs/rev869b_external_controller_phase_a_checkpoint.md` | F07 and all | machine-captured A3 commands/results, corrected evidence, hashes, arithmetic, mutants, exclusions, and retained states |

Any permanent change outside these ten files stops A3 and requires another report-only reconciliation. Migration, model, snapshot, project, solution, Program/endpoint, helper, existing-review, and new-report changes are not authorized.

## 12. Exact A3 test inventory

The A3 implementation must add or replace tests so these exact 16 `[Fact]` methods are discovered and pass through real production contracts:

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

Looped field/version matrices must remain inside these literal independent tests so one renamed theory does not inflate unique counts. Existing F01, readiness, audit/privacy, 30-row lifecycle, raw canonical, and four A2 mutant-killer coverage must remain green.

## 13. Exact A3 production mutants

| ID | Actual vulnerable production point | Temporary mutation | Required killer |
|---|---|---|---|
| `A3-M01-PARTIAL-MUTATION-API-BYPASS` | exported durable contract and raw authority | reintroduce/invoke partial nonce/idempotency/lease mutation outside the composite decision | tests 1-3 plus zero-partial-call trace |
| `A3-M02-AUTHORIZATION-PROVIDER-SUBSTITUTION` | raw authority snapshot composition and state-machine exact grant gate | replace current stored grant with resolver/request-derived binding or bypass approved-intent/grant digest equality | tests 5-9 |
| `A3-M03-CALLER-READER-VERSION-DOWNGRADE` | verifier descriptor resolution | use caller `declaredBundle.ReaderVersion` instead of server-pinned expected version | tests 10-13 with two valid versions |
| `A3-M04-SQL-EVIDENCE-PRODUCTION-DRIFT` | real REV869B migration installation operation | remove/invert one real production migration install operation while preserving compilation | tests 14-16 and existing model/snapshot parity |

Each mutant must be applied in a disposable copy, compile with zero warnings/errors, fail only for its intended assertion, and be removed. Invalid-input, descriptor-only, expected-string-only, checkpoint-only, and test-mirror mutations do not count.

Required result:

```text
A3_mutants_total = 4
AND A3_mutants_compiled = 4
AND A3_mutants_killed_by_intended_assertion = 4
AND A3_mutants_survived = 0
AND A3_mutants_invalid = 0
AND original_A2_mutants_killed = 4
AND temporary_mutant_files_remaining = 0
```

## 14. Exact validation commands and acceptance gate

Principal offline commands after authorized A3 implementation:

```powershell
dotnet build tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-restore -warnaserror
dotnet build SESS.NexaERP.slnx --no-restore -warnaserror
dotnet test tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~ArchitectureFreezeContractTests.A3_&FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Rev869B&FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --list-tests --filter "FullyQualifiedName~Postgres" --logger "console;verbosity=minimal"
```

Also required: Windows PowerShell 5.1 AST parse; EF `--no-connect` discovery with inert `127.0.0.1:1`; 13-migration uniqueness/adjacency; canonical SQL double generation without connection; source/model/snapshot parity; incremental/cumulative exact-allowlist `git diff --check`; source/security/privacy/prohibited-operation scans; exact 10-file boundary; four A3 and four regression A2 mutants; final clean target status.

Overall A3 acceptance formula:

```text
changed_files subset_of exact_10_file_allowlist
AND changed_file_count <= 10
AND permanent_migration_model_snapshot_project_solution_helper_report_changes = 0
AND exported_partial_mutation_methods = 0
AND effective_owner_count(each_of_14_responsibilities) = 1
AND exact_grant_binding_fields_enforced = ALL
AND caller_selected_reader_descriptor_inputs = 0
AND required_A3_tests = 16 passed / 0 failed / 0 skipped
AND all_existing_non_postgresql_tests_failed_or_skipped = 0
AND A3_mutants = 4 compiled / 4 killed / 0 survivors / 0 invalid
AND A2_regression_mutants = 4 killed / 0 survivors
AND canonical_SQL_two_process_byte_equality = true
AND checkpoint_SQL_evidence_matches_machine_capture = true
AND postgresql_tests_executed = 0
AND warnings = 0
AND prohibited_actionable_hits = 0
AND exact_allowlist_diff_checks = exit 0 / no output
AND final_target_status = clean
```

## 15. Retained test arithmetic

Current reviewed evidence remains:

| Counting basis | Count |
|---|---:|
| A2 subset | 31 |
| Complete Phase-A assembly | 50 unique |
| Focused REV869B ERP subset | 76 |
| Complete ERP non-PostgreSQL assembly | 450 unique |
| Total unique across the two assemblies | `50 + 450 = 500` |
| PostgreSQL tests | 87 discovered; 0 executed |

The 31 and 76 are overlapping subsets and are not added to 500. A future A3 checkpoint must separately report its newly discovered A3 tests and recompute post-A3 assembly totals without rewriting this reconciliation's historical arithmetic.

## 16. Retained states and exact next management gate

```text
phase_a_management_acceptance_state=FAIL
phase_b_state=NO_GO
correction_2_state=NO_GO
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
external_provisioning_state=NOT_STARTED
production_readiness_state=NOT_READY
```

Exact single next management gate:

**Approve or reject one `REV869B Option-A Phase-A Correction A3` source-only correction using exactly the 10-file allowlist, 16 named tests, four A3 production mutants, four A2 regression mutants, canonical offline SQL evidence procedure, validation commands, acceptance formulas, exclusions, one-correction-commit rule, and mandatory fresh independent review defined in this report.**

Do not begin A3, Phase B, Correction 2, PostgreSQL execution, migration activity, provisioning, deployment, or production work automatically.
