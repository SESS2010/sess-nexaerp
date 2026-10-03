# REV869B Option-A Phase-1 independent source-safety review

## Review identity and verdict

- Reviewed commit: `5c20bc19e6b690859f1379c09fdd29a23a857d5b`
- Required and observed parent: `18dea1e66053bb5143668a5634e5be16d4eb6ce3`
- Review method: fresh inspection of the exact parent-to-reviewed-commit diff; checkpoint conclusions were not treated as evidence.
- Exact reviewed-file count: 16.
- Source-safety verdict: **FAIL**.
- Execution readiness: **FAIL**.

The projects compile and the permitted offline tests pass, but the committed skeleton does not yet enforce all required source-level trust, lifecycle, evidence, configuration, and bounded-input properties. This report does not claim PostgreSQL, deployment, external-controller, production, or independent runtime evidence.

## Entry-gate evidence

- The reviewed Git object exists and its parent is exact.
- The target-scoped worktree was clean before this report was created.
- Current branch HEAD at review start was the later blocker-report commit `1ea9ec6560e96d6342bc168a7061f940c1b359cd`; this does not alter the immutable reviewed diff.
- `../legacy-reference/` remained untracked and was not accessed or modified.
- The reviewed commit contains exactly the following files:

1. `outputs/rev869b_external_controller_phase1_checkpoint.md`
2. `src/SESS.NexaERP.AcceptanceVerifier/Program.cs`
3. `src/SESS.NexaERP.AcceptanceVerifier/SESS.NexaERP.AcceptanceVerifier.csproj`
4. `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`
5. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BCompatibilityManifestV1.cs`
6. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
7. `src/SESS.NexaERP.ControlPlane.Contracts/SESS.NexaERP.ControlPlane.Contracts.csproj`
8. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
9. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
10. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
11. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
12. `src/SESS.NexaERP.ControlPlane/Program.cs`
13. `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj`
14. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
15. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
16. `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj`

## Reproduced offline validation

| Validation | Independent result |
|---|---|
| Affected-project build | PASS — 0 warnings, 0 errors |
| New Control Plane/Verifier tests | PASS — 12/12 |
| Focused REV869B non-PostgreSQL tests | PASS — 76/76 |
| Complete non-PostgreSQL suite | PASS — 450/450 |
| PostgreSQL-name test discovery | 87 discovered; 0 executed |
| PowerShell AST | PASS — 24/24 scripts |
| EF migration discovery | PASS — 13 migrations listed with `--no-connect`; applied status intentionally unavailable |
| Model/snapshot parity without connecting | PASS — 1/1 |
| Migration uniqueness/order | PASS — 13 unique, strictly ordered IDs |
| Exact reviewed diff `git diff --check` | PASS |
| Secret/prohibited-operation scan | PASS — no embedded private key, assigned password/token/connection string, database/network execution API, Correction 29, or legacy-reference usage found in reviewed source/tests |

The filtered PostgreSQL discovery count includes any test whose fully qualified name contains `Postgres`; discovery is not behavioral evidence. No PostgreSQL test or connection was executed.

## Architecture and production ownership findings

### Independently verified strengths

- Control Plane and Acceptance Verifier are separate `Microsoft.NET.Sdk.Web` projects under `src`, each referencing only the shared contract project, so they are source-owned production components rather than test-only adapters.
- They have separate programs, health/version surfaces and assembly boundaries. Neither project is embedded in the ERP runtime project.
- The reviewed diff adds no database, worker, provisioning, migration, purge, recovery, export, or production execution implementation.
- Roles for control runtime, verifier, audit writer, registry writer, executors, purge authorization/execution, export and monitoring are distinct in the contract.

### Architectural limitations

- The deployable hosts are contract-only. The Control Plane exposes only health/version endpoints; the verifier exposes only health/version endpoints. This is honest for Phase 1, but it is not operational readiness.
- The Acceptance Verifier registers `ClosedEvidenceVerifierV1` without registering or validating its signature-verifier, oracle-catalog, audit-sink or time-provider dependencies (`src/SESS.NexaERP.AcceptanceVerifier/Program.cs:4-6`). It still returns HTTP 200 from `/health/ready` while stating that operations are unimplemented (`Program.cs:9-14`).
- Control Plane startup validates placeholder configuration, but no signer, key registry, replay store, lease store, audit store or authorization issuer is wired. Its `.invalid`-only endpoint validation (`ControlPlaneOptions.cs:41-43,96-99`) also prevents the same configuration contract from accepting a future real private production endpoint.
- The new projects are not added to the existing solution because that path was outside the 16-file allowlist. Direct project builds pass, but solution-level ownership/build integration remains a later bounded change.

## Security and trust findings

### BLOCKING B-01 — command signature does not protect envelope trust metadata or time

`SignedEnvelopeService` signs only canonicalized `LifecycleCommandV1` (`SignedEnvelopeService.cs:30-33`). Contract version, key ID, algorithm, canonicalization version and `SignedAtUtc` are added after signing (`:34-45`). Verification again verifies only the command bytes (`:71-90`). Therefore `SignedAtUtc` can be changed without invalidating the signature, defeating the maximum-age control when the signed replay expiry has not elapsed. Key ID and algorithm are also outside the signed bytes, creating key/algorithm substitution risk as the registry evolves.

The key descriptor's `Algorithm` is never compared with the envelope algorithm. The command hash hex parser at `:75-77` can also throw an untyped `FormatException`, rather than the declared exact rejection code.

Required correction: define a canonical signed body/header containing issuer, audience, contract version, canonicalization version, algorithm, key ID, nonce, issued-at, expiry and the complete command; sign and hash that single structure; validate the registry descriptor's issuer/audience/algorithm/key binding; reject all parsing failures with typed codes.

### BLOCKING B-02 — authorization issuer and subject authority are not established

`CommandAuthorization` contains a subject, caller-supplied role list and timestamp, but no issuer, audience, credential binding or authoritative role-resolution reference (`Rev869BControllerMessagesV1.cs:132-142`). `SignedEnvelopeService.Sign` signs that role list without first obtaining authorization from a trusted issuer. `ControllerAuthorizationPolicyV1` merely checks whether the signed/self-asserted list contains the role mapped to the command (`Rev869BExecutionBinding.cs:57-92`).

Consequently the source cannot fail closed on an unknown issuer, and possession of signing capability can turn caller-provided roles into accepted authority. Runtime identity separation exists as enum values, not as enforced authentication/authorization.

Required correction: introduce typed issuer, subject, audience and authenticated workload identity; resolve roles from a protected authority rather than request data; bind issuer-to-key and issuer-to-operation policy; separate signing permission from command construction; add unknown-issuer and subject/key-confusion tests.

### BLOCKING B-03 — lifecycle state, operation and lease fencing are not enforced

The legal transition table is explicit and rejects a direct skipped transition. However signed-command verification never invokes the state machine. `LifecycleCommandV1.ExpectedState` and `RequestedState` are accepted without checking legality or matching `ControllerCommandKind`. `Binding.OperationId` is checked only for non-emptiness, not for correspondence to the command kind or an approved operation.

The lease model consists only of ID/version equality between two fields in the same request (`Rev869BExecutionBinding.cs:80-87`). There is no acquisition, renewal, authoritative expiry, current-version lookup, fencing token consumption or stale-writer store. `MaximumLeaseSeconds` is validated configuration but unused.

Required correction: add an authoritative lease/fence abstraction and contracts for acquisition/renewal/expiry; atomically compare current fence version; bind command kind, operation ID and legal transition; integrate state-machine validation into the command acceptance path; add concurrency/stale-writer tests before any operational endpoint exists.

### BLOCKING B-04 — duplicate handling is replay rejection, not idempotency

`IReplayGuard.TryAccept` exposes only a Boolean (`SignedEnvelopeService.cs:23-25`). Verification rejects a repeated replay key (`:92-94`), but there is no atomic `(issuer, company, instance, operation, request ID, digest)` idempotency record, collision rejection, or stored authoritative result for a legitimate retry. `MaximumReplayWindowSeconds` is not applied by the verifier, and `maximumAge` is supplied by its caller.

Required correction: separate nonce replay prevention from idempotent request handling; persist request digest and terminal result atomically; return the original result for an exact retry and reject changed-payload collisions; enforce configured time limits internally.

### BLOCKING B-05 — verifier does not pin the claimed oracle or authoritative readers

Evidence `OracleVersion` and `OracleSha256` are checked only for non-empty/hex shape (`ClosedEvidenceVerifierV1.cs:46-50`). The oracle interface/catalog exposes only `OracleId`; the supplied version/hash is never compared with the selected implementation (`:102-104`). Reader IDs, reader versions, source identities and facts are strings inside the signed evidence. No trusted reader registry validates them.

Selectors also carry `Expected` values in the submitted evidence, and the oracle receives the entire envelope. The interface does not prevent an oracle implementation from using caller/controller-supplied expectations. The unit `PassingOracle` always returns true (`ArchitectureFreezeContractTests.cs:302-305`), so the happy-path PASS does not establish closed authoritative fact evaluation.

Required correction: pin oracle ID/version/hash in server-owned configuration/catalogue; keep expected values/formulas outside submitted evidence; authorize reader contracts and source identities independently; verify observations against authoritative fact-reader outputs; bind evidence nonce/time and prevent replay.

### BLOCKING B-06 — verifier trust configuration does not fail closed at startup

Unlike the Control Plane, the Acceptance Verifier has no validated options object. It does not validate key IDs, issuer/audience, oracle inventory/hashes, reader registry, evidence limits, replay window, audit sink or retention policy. Its unresolved services are not proven at startup and readiness reports HTTP success.

Required correction: add verifier-specific fail-closed options and startup validation, register all required trust services, eagerly validate resolvability and key/oracle/reader policy, and make readiness fail until every dependency is usable.

## Permission and ACL findings

### Verified design intent

- There is no administrator/superuser enum member.
- Purge authorization and purge execution are separate roles.
- Verifier, audit writer, registry writer, provisioning, migration, recovery, export and monitoring are represented separately.
- The policy table maps each command kind to a narrow nominal role (`Rev869BExecutionBinding.cs:57-77`).

### REQUIRED CORRECTION R-01 — ACLs are declarative and request-controlled

No endpoint authentication, issuer mapping, workload identity adapter, database role contract or audit-writer enforcement exists. `AuditWriter` is declared but not required by any source path. Direct API/database restrictions therefore are not enforceable in this phase, and the role list accepted by policy comes from the signed request rather than a trusted principal.

Add explicit authentication/audience contracts, protected role resolution, per-component service identities, direct-access deny contracts and tests proving that runtime, monitor, exporter and worker identities cannot obtain controller/verifier/audit/purge authority.

### REQUIRED CORRECTION R-02 — destructive authorization needs stronger separation

`AuthorizeDrop` is assigned to the general `Operator` role and `RecordDropped` to `ProvisioningExecutor`. Before any destructive implementation, define a dedicated drop authorizer/executor separation or document and prove the approved dual-control policy. No deletion is implemented now, so no uncontrolled permanent deletion occurred in this review.

## Audit and evidence findings

### REQUIRED CORRECTION R-03 — audit contract is incomplete and immutability is not enforceable

`VerificationAuditEventV1` contains event ID, execution binding, disposition, rejection codes and time (`Rev869BControllerMessagesV1.cs:273-278`), but omits issuer, authenticated subject, command/request ID, evidence-envelope ID/hash, oracle version/hash, signing key ID and reader provenance digest. `IVerificationAuditSinkV1.Append` is only an interface and the returned reference is not checked for emptiness (`ClosedEvidenceVerifierV1.cs:108-111`). There is no append-only/immutable storage or ordinary-role denial contract.

Extend the event and receipt contracts, require a durable append receipt before returning a verdict, define immutability/retention semantics, and later prove them with least-privilege PostgreSQL and storage evidence.

### REQUIRED CORRECTION R-04 — evidence is neither fully bounded nor privacy-minimized

The verifier bounds observation count and facts per observation, but the request supplies those limits, up to 10,000 × 1,000 (`ClosedEvidenceVerifierV1.cs:51-55`). Selector count, dictionary/string lengths, total serialized bytes, source/object/error/reference lengths, nesting and cumulative evidence size are unbounded. Facts permit arbitrary field names and string values (`Rev869BControllerMessagesV1.cs:216-226`), with no minimization allowlist or sensitive-field rejection.

The provenance predicate at `ClosedEvidenceVerifierV1.cs:60-63` also allows any durable observation through the `|| item.Stage == Durable` branch even if `SourceIdentity` is blank.

Add server-owned hard limits for every collection/string/byte dimension, checked before expensive parsing/canonicalization; add approved fact schemas and sensitive-field redaction/rejection; correct provenance grouping and validate reader/source/stage relationships.

### REQUIRED CORRECTION R-05 — failure outcome and evidence timing are underspecified

Any oracle `false` is reported as `IncompleteActionResult` regardless of the actual rejection (`ClosedEvidenceVerifierV1.cs:104-107`). Observation timestamps are not checked for future/stale/order coherence. Evidence has no verifier replay/nonce store. Exact rejection/audit semantics therefore are not yet reliable.

## Configuration and enterprise-scale findings

- Positive: Control Plane options require non-empty identities/key IDs/versions/limits, distinct command/evidence key IDs, allowlisted environments/patterns and prohibited production-name rejection.
- Required: use `MaximumReplayWindowSeconds`, `MaximumLeaseSeconds` and evidence limits inside verification rather than accepting caller limits.
- Required: add retry/concurrency/rate limits, total payload limits and validated pagination. `PageRequestV1` and `EnterpriseDataScopeV1.PageSize` are plain values with no validation.
- Required: replace `.invalid`-only endpoint validation in the deployment phase with protected private-endpoint policy while retaining safe Phase-1 defaults.
- Improvement: freeze explicit `JsonSerializerOptions`, numeric/date/string normalization rules and cross-language canonical test vectors. Recursive ordinal property sorting is deterministic for the current typed .NET inputs, but the compatibility contract is not sufficient for independent implementations.
- Improvement: use the declared typed scenario/subcase/observation/evidence identity types in the binding/envelope fields; they are presently declared but most fields remain raw strings.

## Test-quality findings

The 12 new tests are fast offline unit/contract tests and materially demonstrate one property-key canonicalization example, one illegal transition, several binding substitutions, command-ID tampering, unknown command version/algorithm/key, stale unsigned signature time, replay rejection, revoked key, role mismatch, absence of verdict fields in serialized evidence, one calculated PASS, one missing stage, and basic option checks.

They do not satisfy the full required negative matrix:

- No issuer test is possible because issuer is absent.
- No full protected-field mutation matrix exists; only command ID is tampered.
- No key-ID/algorithm/issuer substitution test using distinct key material and registry metadata exists.
- No command issued-at/expiry coherence, future issue, empty nonce, maximum replay window or malformed hash typed-rejection test exists.
- No authoritative lease acquisition/renewal/expiry/fencing or concurrent stale-writer test exists.
- No wrong operation ID versus command kind or requested transition integration test exists.
- Duplicate requests are rejected as replay; exact idempotent retry and changed-payload collision are not tested.
- The caller-PASS test only asserts that three property-name strings are absent from serialization (`ArchitectureFreezeContractTests.cs:126-132`); it does not test unknown input rejection or ensure expectations cannot influence the oracle.
- The oversize test supplies an invalid caller limit of `2`; it does not construct evidence exceeding a valid server-owned limit (`:176-188`).
- Missing configuration is not tested. The option test covers a production database name, not absent keys/issuer/oracle/readers/audit services.
- No sensitive-data/log serialization, total byte size, long string, excessive selector, blank durable provenance or future timestamp test exists.
- No runtime-identity privilege-escalation matrix exists.
- `FakeCrypto` uses SHA-256 digest equality while declaring ECDSA, so it validates interface plumbing but not signature-algorithm behavior (`:279-289`).
- `PassingOracle` ignores all facts and always returns true, so the PASS test is self-generated expectation, not evidence of independent acceptance logic.

Existing focused and complete suites passing protects prior ERP source behavior, but many existing tests inspect source contracts or self-generated expectations. None supplies external controller, production identity, immutable audit-store or PostgreSQL behavioral evidence for this new skeleton.

## Findings by classification

### BLOCKING

1. B-01: incomplete signed-envelope coverage and unsigned freshness metadata.
2. B-02: absent issuer/audience trust and request-controlled roles.
3. B-03: state/operation/lease fencing not integrated or authoritative.
4. B-04: no real idempotency/result replay model.
5. B-05: oracle version/hash/readers/expectations are not independently pinned.
6. B-06: Acceptance Verifier trust configuration and readiness do not fail closed.

### REQUIRED CORRECTION

1. R-01: enforce ACL identities at authentication, API and later database boundaries.
2. R-02: strengthen drop authorization/execution separation before destructive work.
3. R-03: complete audit binding and immutable append receipt semantics.
4. R-04: impose server-owned total/field/collection limits and privacy schemas; fix durable provenance logic.
5. R-05: exact oracle failure codes, observation-time validation and evidence replay prevention.
6. Add the missing material negative tests listed above without weakening existing tests.

### IMPROVEMENT

1. Publish explicit canonical serializer rules and cross-language golden vectors.
2. Use the declared strong identity types consistently instead of raw strings.
3. Validate pagination/continuation contracts and define streaming backpressure.
4. Add solution/build-graph integration in a separately authorized path.

## External prerequisites and future PostgreSQL evidence

These are not source findings and remain separate gates:

- Managed KMS/HSM, distinct controller/verifier keys, issuer-to-key registry, rotation/revocation operations and protected secret/config service.
- Independent workload identities, mTLS/private endpoints, deny-by-default network policy and per-role authorization assignments.
- Durable control database/queue/outbox, atomic lease/fencing/replay/idempotency stores and immutable audit/evidence storage with retention/legal hold.
- Independently deployed authoritative fact readers and pinned oracle artifacts.
- Signed build provenance/SBOM, isolated CI promotion, monitoring/paging/runbooks, DR and restore exercises.
- Representative enterprise-scale, failure/chaos and privacy qualification.
- Future authorized PostgreSQL tests proving concurrency fences, immutable history, least privilege, audit failure rollback, idempotency, purge/drop denial and authoritative facts. None was executed here.

## Exact next gate

Management authorization for one bounded source-correction phase addressing all six blocking findings and required negative tests, with an explicit file allowlist. After that commit, require another fresh independent source-only architecture/security review. PostgreSQL, provisioning, deployment, real keys and lifecycle execution must remain unauthorized until source safety passes and their own later gates are approved.

## Canonical states

independent_review_state=FAIL
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
external_provisioning_state=NOT_STARTED
correction_29_state=NOT_STARTED
production_readiness_state=NOT_READY
