# REV869B Option-A Phase-1 Correction 1 independent source-safety review

## Verdict

**FAIL.** The correction builds and its offline tests pass, but the reviewed source does not close any of the six original safety blockers to an independently defensible standard. The most consequential failures are a public verification path that bypasses strict raw canonical parsing, non-atomic command mutation across independent stores, incomplete durable-idempotency decision handling, and authoritative evidence facts that are read but not used by the oracle.

No implementation was changed. No PostgreSQL, provisioning, deployment, lifecycle, quarantine, recovery, drop, purge, export, production, network, or real-key operation was performed.

Canonical states:

`phase1_correction1_independent_review_state=FAIL`

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`

`external_provisioning_state=NOT_STARTED`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`correction_29_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

## Entry gate and review boundary

- Reviewed commit: `aec347d34ec277a2ab7fa06c38a292ecfbeea892`
- Required parent: `5128e45f7938d8269ca0c40dd151f29c57c34882`
- Commit subject: `Implement REV869B Phase 1 Correction 1`
- Review range: `5128e45f7938d8269ca0c40dd151f29c57c34882..aec347d34ec277a2ab7fa06c38a292ecfbeea892`
- Checkpoint SHA-256 independently calculated: `6CECC16F6240232EFF9804A84B6128ACD2AD52D769AFB4EE2C6C987DCF50140B`
- Target-scoped worktree at entry: clean.
- Repository-root metadata showed only `?? ../legacy-reference/`. Its contents were not accessed, enumerated, or modified.
- History was preserved; no reset, stash, amend, rebase, or rewrite was performed.

The range changes exactly these 13 authorized files:

1. `outputs/rev869b_external_controller_phase1_correction1_checkpoint.md`
2. `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`
3. `src/SESS.NexaERP.AcceptanceVerifier/Program.cs`
4. `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`
5. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BCompatibilityManifestV1.cs`
6. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
7. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
8. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
9. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
10. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
11. `src/SESS.NexaERP.ControlPlane/Program.cs`
12. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
13. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`

No project, solution, migration, helper, deployment, or prior-report file is in the reviewed range.

## Reproduced offline validation

| Check | Independent result |
|---|---|
| Existing solution build | PASS — 5 projects, 0 warnings, 0 errors |
| Phase-1 project/test graph build | PASS — 4 projects, 0 warnings, 0 errors |
| Phase-1 tests | PASS — 36 passed, 0 failed, 0 skipped |
| Focused REV869B non-PostgreSQL tests | PASS — 76/76 |
| Complete non-PostgreSQL suite | PASS — 450/450 |
| PostgreSQL scenario discovery | PASS — 34 discovered, 34 unique, 0 executed |
| PowerShell 5.1 AST validation | PASS — 24 scripts, 0 parse errors |
| EF migration discovery | PASS — `--no-connect`, inert loopback port 1, 13 migrations listed; applied state intentionally unknown |
| REV869A/REV869B uniqueness and adjacency | PASS — 13 unique migration IDs; REV869A is ordinal 12 and REV869B ordinal 13 |
| Model/snapshot parity | PASS — 1/1 without connection |
| Offline Up/Down SQL pinned-hash test | PASS — 1/1 without connection |
| Offline Up SQL | PASS — 324,914 UTF-8 bytes, 2,635 lines, SHA-256 `39B067351894AB5732B6DF9C6348B04D708780AFAA18E073F8E6594D07FAF213` |
| Offline Down SQL | PASS — 11,720 UTF-8 bytes, 231 lines, SHA-256 `FC4BCB671501D601041FCED25D6053545BE9F38CF1D9982006953F47229E0AE4` |
| Prohibited-operation scan over added production lines | PASS — 0 matches |
| Privacy/logging scan over added production lines | PASS — 0 matches |
| Secret-material scan over the reviewed diff | PASS — 0 matches |
| `git diff --check` | PASS — no output |
| Exact correction boundary | PASS — exactly 13 authorized files |

Passing compilation, deterministic tests, and scans do not override the blocking architecture and test-probativeness findings below.

## Six original blockers

### 1. Signed-envelope completeness — FAIL

The V2 codec defines deterministic framing for 25 protected header fields, and the named mutation test exercises all 25. That positive evidence is insufficient because the public API retains an alternate verification path.

- `SignedCommandServiceV2.VerifyAsync(SignedCommandEnvelopeV2, ...)` accepts an already materialized typed header and reserializes it for signature checking (`SignedEnvelopeService.cs:389-416`). It never invokes the strict raw parser.
- The separate raw-byte overload invokes `CanonicalSignedHeaderCodecV2.Parse` and then delegates to the typed overload (`SignedEnvelopeService.cs:512-526`). Because the typed overload remains public, strict UTF-8, field ordering, duplicate framing, exact byte regeneration, and framing-length checks are not mandatory for every protected command.
- `Sign(CanonicalSignedHeaderV2, ...)` signs a caller-supplied header after only filling payload-derived fields (`SignedEnvelopeService.cs:382-386`). Issuer, audience, subject, authorized role/scope, operation, resource, lease, nonce, and temporal trust metadata are not constructed by a trusted server-owned command builder.

Therefore the tests prove a codec and selected paths, not the required absence of an alternate parser path or unsigned/request-controlled trust construction.

### 2. Issuer, audience, and authorization trust — FAIL

Positive controls bind issuer/key, algorithm/version, signature, transport subject, audience, configured roles/scopes, operation, and resource identity (`SignedEnvelopeService.cs:401-452`). The trust boundary remains incomplete:

- The signer accepts arbitrary header trust claims (`SignedEnvelopeService.cs:382-386`); there is no production trusted-context construction boundary.
- Role and scope are supplied inside the signed request and accepted when they are members of resolver and issuer sets (`SignedEnvelopeService.cs:437-443`). The service does not derive the one permitted role/scope from operation and resource context, nor reject the existence of a request-selected role.
- Scope is set membership only. There is no exact organization scope such as `ORG:<organization_id>`, global-versus-company scope rule, shared-master exception rule, or two-ledger isolation matrix.
- Runtime identities are represented as strings and sets. The reviewed range contains no production workload-identity, ACL, issuer registry, key registry, signer, rotation, or revocation implementation.

The source-level role split is useful but not a complete enforcement boundary.

### 3. Lifecycle, authorization, and lease enforcement — FAIL

The state machine contains explicit operation/state/role rows (`Rev869BControllerStateMachine.cs:15-41`), fail-closed lookup, lease presence checks, and export substates. However:

- Evidence authorization is reduced to a caller-derived Boolean: `EvidenceRequirements.Count > 0` (`SignedEnvelopeService.cs:482-491`) and `hasEvidence` (`Rev869BControllerStateMachine.cs:77-96`). Required reader identities, evidence digests, action receipt, and exact evidence types are not bound to the transition.
- `LifecycleResourceStateV2` stores only active operation, authorizer role, and expiry for authorization (`Rev869BExecutionBinding.cs:96-105`). It omits request/payload digest, issuer/subject, organization, cluster/instance, resource version, evidence digest, lease ID, and fence; authorization substitution and reuse are therefore not prevented by the state record.
- Nonce reservation, lifecycle read, lease read, idempotency reservation, fence consumption, audit append, lifecycle compare-exchange, and idempotency completion are independent calls (`SignedEnvelopeService.cs:458-508`). No transaction, outbox, compensating protocol, or recovery state makes them atomic. Failures can strand a consumed nonce/fence or reserved idempotency row, append an accepted audit before a failed state transition, or commit lifecycle state before idempotency completion.
- The reviewed range defines store interfaces but no production durable implementation (`Rev869BExecutionBinding.cs:107-165`).

Cancellation/expiry and export state transitions exist, but exact authorization binding and atomic execution are blocking gaps.

### 4. Durable idempotency — FAIL

The interfaces name durable operations, and nonce replay is separated from idempotency. The command service does not implement the required decision table:

- After `ReserveAsync`, it rejects only payload mismatch and non-retryable terminal codes, then executes the lifecycle mutation regardless of reservation state (`SignedEnvelopeService.cs:473-508`).
- It does not return a stored completed response, join/deny an in-progress duplicate, explicitly retry a retryable failure, or persist a failure on downstream exceptions.
- `IdempotencyDecisionV2.RequireReusable` distinguishes only changed digest and non-retryable failure; all other states are returned without state-specific semantics (`Rev869BExecutionBinding.cs:167-189`). The service does not call this helper.
- The concurrency test invokes `FakeIdempotencyStore.ReserveAsync` twice, not `SignedCommandServiceV2` or a durable adapter (`ArchitectureFreezeContractTests.cs:480-488`). It proves the fake's lock behavior, not one authoritative business execution.
- No production durable nonce/idempotency implementation is in the reviewed range.

Missing infrastructure does keep published readiness at NOT_READY, but that does not establish durable idempotency correctness.

### 5. Oracle and authoritative evidence — FAIL

Oracle manifests and reader descriptors are pinned by identifier/version/hash, and verification fails if durable audit append fails. A decisive data-flow error remains:

- The verifier calls the authoritative reader (`ClosedEvidenceVerifierV1.cs:314-328`) but retains only its receipt.
- It validates and hashes the caller-provided `evidence.RawFacts`, then passes the caller evidence directly to `oracle.Evaluate` (`ClosedEvidenceVerifierV1.cs:331-340`). The returned authoritative `facts.Facts` are never compared with or substituted for caller raw facts.
- `PayloadSha256` is a self-hash that the caller can recompute (`ClosedEvidenceVerifierV1.cs:333-336`); V2 has no evidence signature or authenticated reader-response envelope in this path.
- Reader receipt matching checks ID, version, and response-digest string equality, but does not cryptographically authenticate that digest (`ClosedEvidenceVerifierV1.cs:323-327`).
- Reader sets are converted to `HashSet` before equality (`ClosedEvidenceVerifierV1.cs:306-312`), so duplicate receipts collapse rather than fail. V2 also lacks the V1 observation-ID uniqueness check.
- The test oracle always returns PASS and ignores evidence (`ArchitectureFreezeContractTests.cs:1308-1316`), so tests cannot demonstrate that caller-provided PASS-like facts are irrelevant to a real verdict.

Consequently caller-controlled observations can influence the calculated verdict while authoritative facts are unused.

### 6. Fail-closed readiness — FAIL

The current endpoints are safely unavailable: both applications register probes that always return `NOT_READY`, and readiness maps that result to HTTP 503 (`ControllerContractEndpointsV1.cs:10-41`, `AcceptanceVerifier/Program.cs:10-22`, `ClosedEvidenceVerifierV1.cs:71-88`). This is conservative but not the required readiness implementation.

- The probes return static missing-dependency lists; they do not independently inspect issuer/key registries, policy, durable nonce/idempotency, clock policy, oracle artifact, readers, target identity, audit, runtime identity, or ACL configuration.
- The programs do not register production command/verifier dependency implementations that could be conjunctively probed.
- The missing-dependency test supplies `FakeReadinessProbe(bool)` directly (`ArchitectureFreezeContractTests.cs:1250-1286`); it does not exercise both real HTTP endpoints once per absent/invalid dependency.
- No protected mutation or verification endpoint is present, so the requirement that protected endpoints cannot execute while unready is not tested against a real route.

HTTP 503 is presently fail closed, but the required dependency-by-dependency readiness decision table is absent and non-probative.

## Test-quality assessment — FAIL / BLOCKING

All 24 named adversarial tests exist and the Phase-1 project reports 36/36 passing. They do not collectively meet the requested standard.

- Transition expectations are derived from production collections: the unlisted-pair test skips `machine.ListedOperations`, while the listed-transition test iterates `machine.ListedOperationRules` (`ArchitectureFreezeContractTests.cs:372-414`). A weakened production matrix can weaken both implementation and expected truth together.
- The signature fake uses deterministic digest comparison rather than an independent implementation of the declared asymmetric algorithm; key/algorithm behavior is not cryptographically proven.
- The concurrency case tests an in-memory fake directly, not the production service (`ArchitectureFreezeContractTests.cs:480-488`).
- The readiness case tests a Boolean fake rather than the registered probes and HTTP routes (`ArchitectureFreezeContractTests.cs:1250-1286`).
- The oracle fake unconditionally returns PASS (`ArchitectureFreezeContractTests.cs:1308-1316`), so evidence influence and negative oracle behavior are not tested.
- The ten-million-row case is a fake offset pager that materializes integer arrays (`ArchitectureFreezeContractTests.cs:1337-1354`); it does not test a production signed opaque continuation token, wrong-token rejection, cancellation, backpressure, or retry bounds.
- There is no mutation-test evidence showing that removal or weakening of decisive assertions is detected.
- Most service tests use the typed envelope overload, leaving the alternate-parser bypass unchallenged.

These are false-positive/non-probative acceptance gaps and are classified BLOCKING as required.

## Permission and trust matrix assessment

| Boundary | Source representation | Assessment |
|---|---|---|
| Operator vs provisioning/migration executors | Separate role strings in operation rules | Partial; no production identity/ACL enforcement and expected rows are circularly tested |
| Acceptance Verifier | Separate role on verify accept/reject | Partial; verifier evidence path is unsafe and deployment identity is absent |
| Recovery approver/executor | Separate role strings | Partial; authorization is not fully request/evidence/resource bound |
| Drop authorizer/executor | Separate role strings | Partial; source separation exists, runtime enforcement absent |
| Purge authorizer/executor | Separate role strings | Partial; source separation exists, runtime enforcement absent |
| Export authorizer/executor | Separate role strings and export substates | Partial; delivery authorization binding is incomplete |
| Audit writer | Interface boundary only | Partial; durability/immutability and transactional semantics are external |
| Issuer/key/audience/operation | Registry descriptors and set checks | Partial; trusted construction and exact per-operation audience/scope matrix absent |
| Company ledgers/shared masters | No exact organization/global-scope rule | FAIL |

The matrix is a useful source model, but it does not yet prove least privilege, separation of duties, or request-role substitution resistance at runtime.

## Lifecycle, lease, and idempotency assessment

State/operation/role rows, monotonic fence interfaces, compare-exchange interfaces, cancellation/expiry, and export substates are present. Safety is not established because authorization records are under-bound, evidence is a Boolean, durable adapters are absent, idempotency states are not consumed correctly, and the multi-store execution sequence is non-atomic. Exact completed replay, concurrent duplicate behavior, retryable failure, and crash recovery remain unproven.

## Audit, evidence, and readiness assessment

Audit append is mandatory before a verifier verdict returns, and command verification checks for a nonempty durable reference. However, audit receipts are not cryptographically verified, command audit/state/idempotency writes are not atomic, authoritative facts are discarded, caller facts reach the oracle, duplicate evidence is not fully rejected, and readiness is static rather than dependency-derived. The combined control is FAIL.

## Enterprise-scale assessment — FAIL / BLOCKING

The source is not yet demonstrably compatible with the stated enterprise scale.

- Control-plane limits are validated as configuration values, but several are not enforced by `SignedCommandServiceV2`; the typed overload also bypasses the raw 96-KiB framing boundary.
- Evidence serialization and `SelectMany(...).ToArray()` materialize complete caller collections (`ClosedEvidenceVerifierV1.cs:333-378`). Bounds are checked only after an envelope object exists and after canonical serialization in the main path.
- The ten-million-row proof is an isolated fake offset pager, not a production bounded, signed, opaque continuation protocol.
- No production retry cap/backoff, streaming/backpressure, durable replay store, or authoritative evidence-reader paging implementation is present.
- Two companies with separate ledgers and shared approved masters are not enforced by exact scope/resource semantics.
- The declared 300,000-party, 10,000,000-item, and 100,000-machine/project capacities have no load, memory, query-plan, or bounded-allocation evidence in this source-only phase.

The absence of a production in-memory replay implementation avoids falsely claiming one as durable, but compatibility at scale is still unproven.

## Findings

### BLOCKING

1. Public typed-envelope verification bypasses mandatory strict raw canonical parsing; signing accepts arbitrary trust metadata.
2. Trusted role/scope and exact organization/global-scope authorization are not derived from a server-owned policy matrix.
3. Lifecycle authorization is under-bound, evidence is Boolean, and the command sequence is not atomic across nonce, lease, idempotency, audit, and lifecycle stores.
4. Idempotency reservation states are ignored; completed replay, in-progress duplicate, retryable failure, and crash recovery semantics are not implemented by the service.
5. Authoritative facts are read but discarded; caller raw facts reach the oracle, and the V2 evidence envelope is only self-hashed.
6. Readiness is hardcoded unavailable rather than calculated dependency-by-dependency; endpoint non-execution is not tested.
7. Several objective tests are circular or test permissive fakes instead of real production paths, yielding non-probative PASS results.
8. Enterprise bounds, paging, retries, tenant-ledger isolation, and bounded evidence handling are not implemented/proven end to end.

### REQUIRED

1. Make strict raw-byte parsing the single protected-command entry point; remove or make non-public every typed bypass, and add independent 25-field raw mutation/error vectors.
2. Construct signed headers only from authenticated server-owned identity/policy context; define exact issuer/key/audience/operation/role/scope and company/shared-master matrices.
3. Bind authorization to issuer, subject, request/payload digest, organization, cluster/instance, resource/version, evidence digest, lease/fence, operation, expiry, and one-time consumption.
4. Specify and implement one atomic durable transaction or explicit outbox/reconciliation protocol for nonce, idempotency, fence, audit, and lifecycle state.
5. Implement exact idempotency branches for first, completed replay, collision, nonce replay, in-progress duplicate, retryable/non-retryable failure, and expiry; test them through the real service.
6. Build the oracle input solely from authenticated authoritative reader results; cryptographically bind receipts/facts and reject duplicate IDs/receipts, stale data, scope drift, oversize data, and tampering before allocation-heavy processing.
7. Replace unconditional-PASS and circular fixtures with independent expected tables, deterministic positive/negative oracle vectors, actual route tests, and mutation-sensitive assertions.
8. Implement real conjunctive readiness probes and verify HTTP 503 plus zero protected execution for every missing/invalid dependency.
9. Define production bounded payload/evidence processing, signed opaque pagination, cancellation/backpressure, retry caps, tenant isolation, and scale validation contracts.

### IMPROVEMENT

1. Add cross-language golden framing and signature vectors using the declared production algorithm.
2. Use typed role, scope, operation, and resource identifiers rather than free-form strings.
3. Add automated mutation testing and fault injection at every store boundary.
4. Add sanitized metrics for readiness dependency state, replay decisions, lease/fence rejection, audit append, and evidence-reader failures.

## External prerequisites

The following remain absent and blocking independently of the source findings:

- Real KMS/HSM signing keys; immutable issuer/key registry; rotation and revocation operations.
- Independently deployed Control Plane and Acceptance Verifier workload identities.
- Durable control database/queue/outbox and atomic nonce, idempotency, lease, lifecycle, and audit behavior.
- Immutable audit/evidence storage with retention and legal hold.
- Deployed authoritative readers and an independently built, pinned oracle artifact.
- Exact IAM, mTLS/private networking, runtime ACL, database least-privilege, and tenant-ledger isolation evidence.
- PostgreSQL concurrency, replay, fencing, audit rollback, reader, drop/purge denial, and behavioral scenario evidence.
- Load/scale/chaos, backup/restore, disaster recovery, monitoring, runbooks, training, and production approval.

## Honest final verdict and exact next gate

REV869B Option-A Phase-1 Correction 1 is **not source-safe**. All six original blockers remain FAIL based on the reviewed source and the probativeness of its tests. The passing offline validation demonstrates build health and regression stability only; it does not authorize PostgreSQL, external provisioning, deployment, protected lifecycle execution, Correction 29, or production use.

**Single exact next gate:** management authorization for one report-only Phase-1 Correction 1 failure reconciliation that classifies the eight bounded BLOCKING findings above and defines at most one exact source-correction allowlist; no source correction, PostgreSQL activity, external provisioning, deployment, lifecycle operation, Correction 29, or production action is authorized by this review.
