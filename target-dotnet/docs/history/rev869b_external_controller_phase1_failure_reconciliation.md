# REV869B Option-A Phase-1 independent-review failure reconciliation

## Entry gate and decision

- Starting HEAD: `a150297ec50ddaa4f34a74c59555d861e477dbc9`
- Reviewed Phase-1 source commit: `5c20bc19e6b690859f1379c09fdd29a23a857d5b`
- Reviewed source parent: `18dea1e66053bb5143668a5634e5be16d4eb6ce3`
- Independent review: `outputs/rev869b_external_controller_phase1_independent_source_safety_review.md`
- Preserved blocker commit: `1ea9ec6560e96d6342bc168a7061f940c1b359cd` is an ancestor of the starting HEAD.
- Target-scoped worktree at entry: clean.
- `../legacy-reference/`: remains untracked and was not accessed or modified.

Reconciliation verdict: **PASS**. The six blockers can be addressed in one bounded source-only correction without PostgreSQL, deployment, real keys, network calls or lifecycle execution.

Phase-1 Correction 1 source-only gate: **GO**, limited to the exact 13-file allowlist below. This is authorization readiness, not authorization to implement.

## Blocker root causes and affected files

| Blocker | Root cause | Affected correction files |
|---|---|---|
| B-01 signed-envelope completeness | V1 signs only the command payload; security metadata and freshness time are outside the signature, key algorithm is not bound to registry metadata, and malformed hash parsing is not typed. | `Rev869BControllerMessagesV1.cs`, `Rev869BCompatibilityManifestV1.cs`, `SignedEnvelopeService.cs`, `ArchitectureFreezeContractTests.cs` |
| B-02 issuer/audience/role trust | The command contains its own role list; no issuer/audience registry, key-to-issuer binding or authenticated-subject resolver exists. | `Rev869BControllerMessagesV1.cs`, `SignedEnvelopeService.cs`, `Rev869BExecutionBinding.cs`, `ControlPlaneOptions.cs`, both `Program.cs` files, `ArchitectureFreezeContractTests.cs` |
| B-03 lifecycle/operation/lease enforcement | The state machine is not invoked by command verification; operation IDs are not mapped to command kinds; lease equality is intra-request rather than an authoritative acquisition/renewal/expiry/fence check. | `Rev869BControllerMessagesV1.cs`, `Rev869BExecutionBinding.cs`, `Rev869BControllerStateMachine.cs`, `SignedEnvelopeService.cs`, `ArchitectureFreezeContractTests.cs` |
| B-04 durable idempotency | `IReplayGuard` only rejects a duplicate key and cannot distinguish exact replay, changed-payload collision, concurrent duplicate or stored terminal result. | `Rev869BControllerMessagesV1.cs`, `Rev869BExecutionBinding.cs`, `SignedEnvelopeService.cs`, `ArchitectureFreezeContractTests.cs` |
| B-05 oracle/evidence ownership | Oracle version/hash and reader provenance are submitted strings, not compared with a server-owned manifest; expected values can travel with evidence; no authoritative reader registry or evidence replay policy exists. | `Rev869BControllerMessagesV1.cs`, `Rev869BCompatibilityManifestV1.cs`, `ClosedEvidenceVerifierV1.cs`, `AcceptanceVerifierOptions.cs`, `ArchitectureFreezeContractTests.cs` |
| B-06 verifier fail-closed readiness | The verifier has no validated trust options and does not prove its registries/readers/idempotency/audit dependencies before returning readiness success. | `AcceptanceVerifierOptions.cs`, `ClosedEvidenceVerifierV1.cs`, `SESS.NexaERP.AcceptanceVerifier/Program.cs`, `ControlPlaneOptions.cs`, `SESS.NexaERP.ControlPlane/Program.cs`, `ControllerContractEndpointsV1.cs`, `ArchitectureFreezeContractTests.cs` |

## Smallest exhaustive Correction 1 allowlist

Exactly these files may be created or modified; no substitution or additional path is implied:

1. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
   - Add V2 protected-header, issuer/subject/scope, resource, lease/fence, temporal, idempotency, evidence, audit, readiness and typed failure contracts while retaining V1 only as an explicitly unsupported compatibility surface for protected operations.
2. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BCompatibilityManifestV1.cs`
   - Publish exact V2 contract/canonicalization/algorithm identifiers and immutable allowlist descriptors; do not silently reinterpret V1.
3. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
   - Implement exact canonical bytes, complete signature verification order, issuer/key/algorithm binding, temporal validation, nonce replay and typed fail-closed parsing through injectable abstractions.
4. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
   - Replace request-controlled role acceptance with trusted authorization resolution; define authoritative lease/fence and durable idempotency interfaces/decision logic.
5. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
   - Implement the exact operation/state/evidence/role/version/lease transition matrix and typed illegal-transition outcomes.
6. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
   - Add issuer/audience/key/clock/nonce/idempotency/lease/payload/readiness policy with server-owned hard limits and fail-closed validation.
7. `src/SESS.NexaERP.ControlPlane/Program.cs`
   - Register and eagerly validate required trust/readiness abstractions; no operational lifecycle endpoint is authorized.
8. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
   - Make readiness return `503 NOT_READY` with sanitized typed dependency codes until every required dependency is healthy; retain health/version-only scope.
9. `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs` (new)
   - Define verifier-specific issuer, audience, key, oracle, reader, clock, payload, replay, audit and ACL readiness configuration.
10. `src/SESS.NexaERP.AcceptanceVerifier/Program.cs`
    - Bind/validate verifier options, register required abstractions, validate dependency resolvability at startup and report fail-closed readiness; add no verification network endpoint.
11. `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`
    - Add pinned-oracle/reader ownership, server-owned selectors, evidence time/replay/privacy/size checks, exact failure results and required durable audit receipt.
12. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
    - Add the complete objective offline and mutation matrix specified below, using deterministic test doubles rather than real keys, network or PostgreSQL.
13. `outputs/rev869b_external_controller_phase1_correction1_checkpoint.md` (new)
    - Record lineage, exact changed files, build/test counts, mutation results, unresolved external prerequisites, exclusions and conservative states.

No project file change is required: SDK globbing includes the one new verifier configuration source, and the existing test project already references all three affected projects and required offline packages.

## Explicit excluded-file list

The following are expressly excluded, together with every unnamed path:

- `SESS.NexaERP.slnx` and every `.csproj` file.
- `src/SESS.NexaERP.Api/**`, `src/SESS.NexaERP.Application/**`, `src/SESS.NexaERP.Domain/**`, and `src/SESS.NexaERP.Infrastructure/**`.
- All migrations, designers and `NexaErpDbContextModelSnapshot.cs`.
- `tests/SESS.NexaERP.Tests/**` and all existing PostgreSQL test/helper sources.
- Every file under `tools/**`.
- Existing architecture, checkpoint, blocker, independent-review and reconciliation reports.
- `../legacy-reference/**`.
- Correction 29 or any later correction path.
- Deployment, IaC, secrets, certificates, keys, database scripts, queue/worker and production configuration files.

If any excluded or fourteenth path is necessary, Correction 1 must stop before editing and return to management with an allowlist blocker.

## Exact V2 interfaces and data contracts

The correction must add or replace the following source-level contracts. Names may not be weakened to untyped dictionaries.

### Shared contract types

- `CanonicalSignedHeaderV2`: the 25 fields listed in the canonical contract below, with strong scalar types.
- `CanonicalCommandPayloadV2`: command kind, expected state, requested state, scenario/subcase/action identity, approved parameters and evidence requirements. It contains no roles or verdict.
- `SignedCommandEnvelopeV2`: header, payload and signature bytes only; no unsigned security-relevant metadata.
- `TrustedIssuerDescriptorV2`: issuer ID, allowed audiences, contract versions, algorithms, key IDs, subject patterns, roles, scopes, operations, activation and revocation times.
- `AuthenticatedSubjectV2`: issuer, immutable subject ID, workload identity, audience and externally resolved role/scope grants.
- `ResourceBindingV2`: organization ID, cluster ID, instance ID, resource type/ID, expected resource version and operation.
- `LeaseFenceV2`: lease ID, resource ID, fencing token, acquired/renewed/expires times and holder subject.
- `TemporalAuthorizationV2`: nonce, issued-at, not-before and expires-at.
- `IdempotencyBindingV2`: issuer, organization, instance, operation, request ID, idempotency key and canonical request digest.
- `IdempotencyOutcomeV2`: reservation state, attempt number, retryability, terminal failure code, response digest, audit reference and completion time.
- `OracleManifestV2`: oracle ID, semantic version, artifact SHA-256, evidence schema version, allowed reader IDs/versions and activation/revocation times.
- `EvidenceReaderDescriptorV2`: reader ID/version/hash, source type, allowed organization/resource/field scopes and maximum response dimensions.
- `CanonicalEvidenceEnvelopeV2`: envelope ID, binding, observation time window, raw typed facts, reader receipts, action receipt and payload hash; it has no expected values, formula, disposition, PASS or FAIL field.
- `VerificationAuditEventV2`: issuer, subject, key ID, request ID, evidence-envelope ID/hash, binding, lease/fence, oracle ID/version/hash, reader receipt digests, calculated disposition, exact reason codes and timestamp.
- `ReadinessResultV2`: `READY` or `NOT_READY`, sanitized dependency codes and checked-at time; never contains secrets/endpoints/credentials.
- `TrustFailureCodeV2` with at least: `CONTRACT_UNSUPPORTED`, `CANONICALIZATION_UNSUPPORTED`, `ALGORITHM_UNSUPPORTED`, `KEY_UNKNOWN`, `KEY_REVOKED`, `ISSUER_UNKNOWN`, `ISSUER_KEY_MISMATCH`, `AUDIENCE_MISMATCH`, `SUBJECT_UNAUTHORIZED`, `REQUEST_ROLE_FORBIDDEN`, `SIGNATURE_INVALID`, `PAYLOAD_HASH_MISMATCH`, `NOT_YET_VALID`, `ENVELOPE_EXPIRED`, `NONCE_REPLAY`, `ORGANIZATION_MISMATCH`, `CLUSTER_MISMATCH`, `INSTANCE_MISMATCH`, `OPERATION_MISMATCH`, `RESOURCE_VERSION_STALE`, `LEASE_REQUIRED`, `LEASE_EXPIRED`, `LEASE_FENCE_STALE`, `STATE_TRANSITION_ILLEGAL`, `IDEMPOTENCY_PAYLOAD_MISMATCH`, `IDEMPOTENCY_NONRETRYABLE`, `ORACLE_MISMATCH`, `READER_MISSING`, `READER_UNAUTHORIZED`, `EVIDENCE_TOO_LARGE`, `EVIDENCE_SENSITIVE_FIELD`, `AUDIT_APPEND_FAILED`, and `SERVICE_NOT_READY`.

### Required injectable interfaces

- `ITrustedIssuerRegistry.Resolve(issuerId, keyId)` returns one active immutable issuer/key policy or exact failure.
- `IAuthorizationResolver.Resolve(authenticatedSubject, resourceBinding)` returns trusted grants; it never consumes roles from the request.
- `IEnvelopeSigner.Sign(keyId, canonicalBytes)` signs only after trusted context creates the header.
- `IEnvelopeSignatureVerifier.Verify(keyDescriptor, canonicalBytes, signature)` uses the descriptor's exact algorithm and key.
- `INonceReplayStore.TryReserveAsync(issuer, nonce, expiresAt)` atomically reserves a nonce.
- `ILeaseFenceStore.AcquireAsync`, `RenewAsync`, `ReadCurrentAsync`, `ConsumeFenceAsync` enforce monotonically increasing fencing tokens.
- `IIdempotencyStore.ReserveAsync`, `ReadAsync`, `CompleteAsync`, `RecordFailureAsync` implement the decision table below atomically.
- `ILifecycleStateStore.ReadAsync` and `CompareExchangeAsync` enforce expected resource version and transition.
- `IOracleManifestRegistry.Resolve(oracleId)` returns the server-pinned version/hash/reader inventory.
- `IAuthoritativeEvidenceReader.ReadFactsAsync(request)` returns facts plus a signed/hashed reader receipt, never a verdict or expected value.
- `IEvidenceReaderRegistry.Resolve(readerId, version)` enforces source/scope/field policy.
- `IVerificationAuditSinkV2.AppendAsync(event)` returns a non-empty durable append receipt or fails the protected operation.
- `ITrustReadinessProbe.CheckAsync()` checks every required dependency without exposing protected configuration.

Correction 1 may provide deterministic in-memory fakes only inside the existing test file. Production implementations of durable stores, key systems, readers and audit persistence remain external prerequisites and must not be fabricated.

## Exact canonical signed-field contract

### Signed fields and order

The signature input is one UTF-8 byte sequence with this exact order:

1. `contract_version`
2. `canonicalization_version`
3. `algorithm`
4. `key_id`
5. `issuer`
6. `audience`
7. `subject`
8. `authorized_role`
9. `authorized_scope`
10. `organization_id`
11. `database_cluster_id`
12. `database_instance_id`
13. `operation`
14. `resource_id`
15. `resource_version`
16. `lease_id`
17. `fencing_token`
18. `request_id`
19. `idempotency_key`
20. `nonce`
21. `issued_at`
22. `not_before`
23. `expires_at`
24. `canonical_payload_sha256`
25. `canonical_payload_length`

Framing is:

`SESS-REV869B-COMMAND-V2\n` followed by each field as `field_name=<UTF8-byte-length>:<value>\n`. Field names are lower ASCII exactly as listed. The decimal byte length has no sign or leading zero except `0`. The final line ends in LF. There is no BOM and no CR.

### Normalization and rejection

- Encoding is strict UTF-8; invalid sequences, NUL and ASCII control characters are rejected.
- Security identifiers are non-empty ASCII `[A-Za-z0-9._:/-]`, case-sensitive, maximum 128 bytes, with no leading/trailing whitespace.
- `authorized_role` and `operation` use exact uppercase registered tokens. Unknown tokens fail closed.
- `authorized_scope` is exactly `ORG:<organization_id>`, `GLOBAL:CONTROL`, or `GLOBAL:MASTER`, with the two global scopes permitted only to registry-authorized operations; no comma-separated or wildcard scope is accepted.
- Organization, cluster, instance, resource, request, idempotency, lease and nonce values are never trimmed or case-folded.
- Nonce is unpadded base64url encoding of exactly 16 random bytes at deployment; tests use fixed non-secret vectors. Duplicate nonce is rejected independently of idempotency.
- Unsigned integers use invariant base-10 with no sign/leading zeros. Resource version and fencing token must be greater than zero.
- Times are UTC and exactly `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`; offsets, leap-second text, noncanonical fractions and `not_before > expires_at` are rejected. `issued_at <= not_before <= expires_at`, and the configured maximum lifetime is enforced internally.
- SHA-256 is exactly 64 lowercase hexadecimal characters. Payload length is invariant decimal and must equal the received canonical payload byte count.
- Canonical payload JSON uses explicit frozen serializer options: UTF-8, no BOM/indentation, ordinal property ordering, ordinal dictionary-key ordering, array order preserved, NFC strings, duplicate names rejected, integers only for numeric contract fields, invariant decimal strings for business decimal facts, and exact UTC time text. Non-NFC input is rejected rather than silently changed.
- The payload hash is calculated from the canonical payload bytes and included in the signed header. Every payload field is therefore protected.
- The signature value itself and transport-only content length are the only fields outside the signature. Transport metadata may not override any signed field.

### Verification order

1. Enforce total byte and field-count limits before allocation-heavy parsing.
2. Parse strict framing and reject duplicate/missing/extra fields.
3. Reject unsupported contract/canonicalization/algorithm identifiers.
4. Resolve issuer and key; require active, non-revoked, issuer-bound key with the same algorithm/audience/version.
5. Verify signature over the exact received canonical header bytes.
6. Recompute payload length/hash in fixed time and compare.
7. Validate audience, authenticated subject, trusted role/scope and resource binding.
8. Validate issued-at/not-before/expiry using server-owned clock/skew/lifetime limits.
9. Atomically reserve nonce.
10. Resolve idempotency decision, current resource version, legal transition and active lease/fencing token.
11. Append denial or accepted-attempt audit evidence before returning.

No accepted security field may be sourced from an unsigned duplicate HTTP header, query value or payload extension.

## Exact role and trust matrix

| Identity | Trusted issuer/audience | Allowed authority | Explicit denial |
|---|---|---|---|
| ERP runtime | ERP workload issuer / ERP API | ordinary approved ERP business operations only | controller signing, registry write, verifier, audit append, lease management, drop/purge/export authorization |
| ControlPlaneRuntime | controller workload issuer / control-plane audience | coordinate state reads, request verification, quarantine after exact policy | self-grant roles, oracle PASS, audit mutation, purge authorization/execution |
| CommandSigner | KMS workload issuer / signing audience | sign an already authorized canonical envelope for one key/issuer | construct roles/scopes, lifecycle execution, verification verdict |
| RegistryWriter | management issuer / registry audience | register immutable target/cluster/instance/issuer descriptors | execute migrations/lifecycle, rewrite audit history |
| ProvisioningExecutor | worker issuer / provisioning audience | prepare registered non-production targets under lease/fence | authorize itself, migrate, verify, purge |
| MigrationExecutor | worker issuer / migration audience | execute one authorized migration operation under lease/fence | authorize migration, verify PASS, purge/drop |
| AcceptanceVerifier | verifier issuer / verifier audience | resolve pinned oracle/readers and calculate disposition | accept caller verdict/expectations, sign controller command, mutate target |
| AuditWriter | audit issuer / audit audience | append immutable audit event and receipt only | update/delete/read unrelated evidence, controller execution |
| RecoveryApprover | management issuer / recovery-approval audience | authorize one bounded recovery plan | execute recovery, authorize purge |
| RecoveryExecutor | worker issuer / recovery audience | execute approved recovery under lease/fence | create its approval, drop/purge/export |
| DropAuthorizer | management issuer / drop-approval audience | authorize exact resource/version after evidence/retention checks | execute drop or purge |
| DropExecutor | worker issuer / drop audience | execute one drop authorization under fence | authorize drop/purge, delete audit evidence |
| PurgeAuthorizer | retention issuer / purge-approval audience | authorize exact frozen candidate digest after retention/legal-hold checks | execute purge or alter candidates |
| PurgeExecutor | worker issuer / purge audience | delete only frozen authorized candidates under fence | choose candidates, authorize, alter audit history |
| ExportAuthorizer | privacy issuer / export-approval audience | approve purpose, fields, recipient and expiry | read/deliver evidence directly |
| ExportExecutor | export issuer / export audience | prepare/deliver minimized immutable batch under release token | widen fields/scope, reuse expired release |
| MonitoringReader | monitoring issuer / health audience | sanitized health/version/readiness only | commands, facts, secrets, evidence bodies, admin authority |

Issuer registry configuration is immutable to all runtime identities. A request role claim is rejected with `REQUEST_ROLE_FORBIDDEN`; only `IAuthorizationResolver` grants are used.

## Exact lifecycle transition matrix

Every row requires an exact current resource version and produces one compare-exchange version increment. Any unlisted `(state, operation)` returns `STATE_TRANSITION_ILLEGAL`, leaves state/version unchanged and appends a denial audit event. “Exact replay” means the idempotency table returns the stored outcome without a second transition.

| Current state/resource | Authorized operation | Required role | Required evidence | Next state/resource | Lease/fence | Expiry behavior | Retry behavior |
|---|---|---|---|---|---|---|---|
| Registered | AUTHORIZE_PREPARE | Operator | approved target/manifest digest | Preflight | no lease; exact version | expired authorization → Registered | exact replay returns authorization receipt |
| Preflight | PREPARE | ProvisioningExecutor | manifest, ACL and target-pin checks | Provisioning | active lease/current fence | pre-start expiry → Preflight; mid-operation expiry → Quarantined | retryable failure resumes same attempt/fence only |
| Provisioning | COMPLETE_PREPARE | ProvisioningExecutor | prepared-target receipt and fingerprint | Ready | same active lease/fence | expiry → Quarantined | exact replay returns stored Ready receipt |
| Provisioning | FAIL | ProvisioningExecutor | typed failure and no-success receipt | Failed | current fence | expiry recorded with failure | nonretryable result is stable |
| Ready | AUTHORIZE_EXECUTE | Operator | approved operation/payload digest | MigrationAuthorized | no lease; exact version | expiry → Ready | exact replay returns authorization receipt |
| MigrationAuthorized | EXECUTE | MigrationExecutor | preparation receipt and migration identity | Migrating | active lease/current fence | pre-start expiry → Ready; mid-operation expiry → Quarantined | resume only a recorded retryable attempt |
| Migrating | COMPLETE_EXECUTE | MigrationExecutor | action receipt and durable target facts | VerificationPending | same active lease/fence | expiry → Quarantined | exact replay returns stored receipt |
| Migrating | FAIL | MigrationExecutor | rollback/terminal facts | Failed or Quarantined if outcome uncertain | current fence | expiry cannot imply rollback | retry follows recorded retryability |
| VerificationPending | VERIFY_ACCEPT | AcceptanceVerifier | pinned oracle result, reader receipts, durable audit receipt | Accepted | current controller lease/fence | expired lease → Quarantined, no verdict commit | exact replay returns stored calculated result |
| VerificationPending | VERIFY_REJECT | AcceptanceVerifier | exact rejection facts and durable audit receipt | Failed | current controller lease/fence | expired lease → Quarantined | exact replay returns stored rejection |
| Any nonterminal state | QUARANTINE | ControlPlaneRuntime | anomaly code and current fingerprint | Quarantined | emergency compare-exchange; no execution lease | never converts unknown outcome to success | duplicate returns original quarantine receipt |
| Quarantined | AUTHORIZE_RECOVER | RecoveryApprover | immutable recovery plan/digest | RecoveryAuthorized | no lease; exact version | expiry → Quarantined | exact replay returns approval receipt |
| RecoveryAuthorized | RECOVER | RecoveryExecutor | approved plan and before fingerprint | Recovering | active lease/current fence | pre-start expiry → Quarantined; mid-operation → Quarantined | retryable attempt retains plan/digest |
| Recovering | COMPLETE_RECOVER | RecoveryExecutor | after fingerprint and reconciliation facts | Ready | same active lease/fence | expiry → Quarantined | exact replay returns Ready receipt |
| Recovering | FAIL | RecoveryExecutor | typed failure and cleanup facts | Failed or Quarantined if uncertain | current fence | expiry recorded, never success | stable nonretryable outcome |
| Accepted, Failed or Quarantined | AUTHORIZE_DROP | DropAuthorizer | identity pin, retention/legal-hold and disposition evidence | DropAuthorized | no execution lease; exact version | expiry → prior state | exact replay returns approval receipt |
| DropAuthorized | DROP | DropExecutor | exact authorization digest and target fingerprint | Dropped | active lease/current fence | pre-start expiry → prior state; uncertain mid-drop → Quarantined | at most one physical execution |
| Dropped | AUTHORIZE_PURGE | PurgeAuthorizer | retention expiry, legal-hold clear and frozen candidate digest | PurgeAuthorized | no execution lease; exact version | expiry → Dropped | exact replay returns approval receipt |
| PurgeAuthorized | PURGE | PurgeExecutor | frozen candidates and approval digest | Purging | active lease/current fence | pre-start expiry → Dropped; mid-operation → Quarantined | at most one candidate execution |
| Purging | COMPLETE_PURGE | PurgeExecutor | deleted-count/candidate digest and immutable receipt | Purged | same active lease/fence | expiry → Quarantined until reconciled | exact replay returns Purged receipt |
| Purging | FAIL | PurgeExecutor | rollback/failure receipt and candidate fingerprint | Dropped or Quarantined if uncertain | current fence | expiry never implies deletion success | retry only after reconciliation/new authorization |
| Accepted + Export.None | AUTHORIZE_EXPORT | ExportAuthorizer | purpose, recipient, minimized field set and expiry | Accepted + Export.Authorized | exact resource version; no execution lease | expiry → Export.Expired | exact replay returns release receipt |
| Accepted + Export.Authorized | EXPORT | ExportExecutor | immutable batch digest and release token | Accepted + Export.Delivering | export lease/current fence | expiry before delivery → Export.Expired | interrupted delivery requires new release |
| Accepted + Export.Delivering | COMPLETE_EXPORT | ExportExecutor | delivery receipt with batch digest | Accepted + Export.Delivered | same export fence | expiry during delivery → Export.Failed pending reconciliation | exact replay returns delivery receipt |
| Any active authorization | CANCEL | the same authorizer role | cancellation reason and unused-authorization proof | primary state unchanged + authorization Cancelled | exact authorization version; no lease | already expired stays Expired | exact replay returns cancellation receipt |
| Any active authorization | EXPIRE | ControlPlaneRuntime clock policy | server time beyond expiry and unused proof | primary state unchanged + authorization Expired | compare-exchange version | only server clock may expire | duplicate returns expiry receipt |

Direct assignment to `Accepted`, `Dropped`, `Purged` or `Export.Delivered` is never legal. No ordinary runtime role may update/delete transition, authorization, idempotency, nonce, lease or audit history.

## Durable idempotency and replay decision table

The authoritative key is `(issuer, organization_id, database_instance_id, operation, idempotency_key)`. It has a unique constraint. `request_digest` is the SHA-256 of the complete canonical signed request. Nonce uniqueness is separately constrained by `(issuer, nonce)` until expiry. Reservation, state compare-exchange and attempt creation must be one transaction in the future durable implementation.

| Condition | Atomic decision | Returned outcome | Durable evidence | Exact failure code |
|---|---|---|---|---|
| First valid request | insert `Reserved`, digest, nonce and attempt 1; one winner | execute once, then stored terminal response | reservation, nonce, attempt, transition and response digest | none |
| Exact replay while Reserved/Running | do not create attempt; join/read winner | `PENDING` with original request/audit reference, then identical stored result | duplicate-seen event linked to original | none |
| Exact replay after Completed | no mutation | byte-equivalent stored response and receipt | replay event linked to completed record | none |
| Same key, changed payload digest | no state/attempt mutation | typed rejection | denial event with both non-sensitive digests | `IDEMPOTENCY_PAYLOAD_MISMATCH` |
| Duplicate nonce under any other key | no reservation/attempt | typed rejection | nonce-replay denial, issuer and request ID | `NONCE_REPLAY` |
| Expired envelope | no nonce/idempotency reservation | typed rejection | sanitized expiry denial | `ENVELOPE_EXPIRED` |
| Concurrent exact duplicates | unique reservation elects one winner; losers read winner | one execution; every caller gets same terminal response | one attempt/transition, N linked replay events | none |
| Prior retryable failure | require same digest, fresh nonce, unexpired retry authorization and compare-exchange attempt number | resume/new attempt as policy specifies; never duplicate completed effects | prior failure, retry authorization and next attempt link | none, or stored retry failure code |
| Prior non-retryable failure | no new attempt | exact stored failure | replay event linked to terminal failure | `IDEMPOTENCY_NONRETRYABLE` |
| Store unavailable/ambiguous | reject before operation | service unavailable, no execution | readiness/denial event if audit is available | `SERVICE_NOT_READY` |

Retention must cover the maximum command retry horizon plus audit policy; terminal digests and references remain for the required business/audit retention even after nonce expiry.

## Oracle and authoritative evidence ownership

- SESS Acceptance Verification owns the oracle source and version manifest. Deployment pins one immutable artifact SHA-256 and semantic version in protected verifier configuration.
- `IOracleManifestRegistry` is server-owned. Evidence-supplied oracle ID/version/hash must exactly equal the active pinned manifest; evidence cannot select an alternative oracle.
- Expected values, formula components and disposition rules live only in the pinned oracle catalogue. They are never accepted from a controller/caller evidence envelope.
- Evidence readers are independently registered by ID/version/hash and permitted source/scope/field list. A reader returns raw typed facts plus a receipt; it cannot return PASS/FAIL.
- Before facts must precede the action; after facts must follow it; durable facts must be independently read after the action receipt. All timestamps must lie within configured skew/window and share exact organization/cluster/instance/resource/lease/fence/request bindings.
- `DatabaseObserved` and `OfflineContract` are distinct evidence classes. `OfflineContract` can validate schemas/canonicalization only and can never produce production acceptance.
- A missing, unhealthy, mismatched or unauthorized reader returns `READER_MISSING` or `READER_UNAUTHORIZED`; the verifier records no Accepted transition.
- Any payload/receipt/hash/binding mutation returns the exact trust/evidence failure and appends a denial audit event if the audit dependency is healthy.
- Caller-supplied `pass`, `fail`, `verdict`, `disposition`, expected selector or formula fields are rejected as unmapped input before oracle evaluation.

## Verifier fail-closed readiness decision table

All requirements are conjunctive. Protected verification remains unavailable unless the result is `READY`. Health liveness may return 200 for the process; readiness returns HTTP 503 and sanitized codes for any row that fails.

| Dependency/check | READY condition | NOT_READY code | Protected-operation behavior |
|---|---|---|---|
| Verifier options | one complete, unambiguous validated section | `CONFIG_MISSING_OR_INVALID` | reject `SERVICE_NOT_READY` |
| Trusted issuer registry | loaded, unique issuers and active policy version | `ISSUER_REGISTRY_UNAVAILABLE` | reject |
| Key registry | loaded; every active key issuer-bound; no duplicate key IDs | `KEY_REGISTRY_UNAVAILABLE` | reject |
| Algorithms/contracts | non-empty exact allowlists; no fallback/default | `TRUST_ALLOWLIST_INVALID` | reject |
| Audience/service identity | exact verifier audience and workload identity available | `SERVICE_IDENTITY_UNAVAILABLE` | reject |
| Oracle manifest | one active oracle ID/version/hash pinned | `ORACLE_NOT_PINNED` | reject |
| Evidence readers | every oracle-required reader ID/version/hash registered and healthy | `READER_SET_INCOMPLETE` | reject |
| Nonce/idempotency provider | atomic reserve/read/complete health check succeeds | `IDEMPOTENCY_UNAVAILABLE` | reject before evidence read |
| Clock policy | server clock, maximum skew/lifetime and monotonic source configured | `CLOCK_POLICY_INVALID` | reject |
| Audit writer | append health and immutable receipt verification succeed | `AUDIT_WRITER_UNAVAILABLE` | reject; never return verdict |
| Cluster/instance policy | exact permitted cluster/instance IDs loaded | `TARGET_IDENTITY_UNAVAILABLE` | reject |
| Payload/privacy policy | total/field/collection limits and allowed fact schema loaded | `EVIDENCE_POLICY_INVALID` | reject |
| ACL identity | verifier role and denial of controller/audit/purge roles proven by resolver | `ACL_IDENTITY_INVALID` | reject |

Readiness output contains only status, code set, policy version and checked-at time. It must not serialize endpoint addresses, key material, issuer internals or exception text.

## Enterprise-scale contract

- Commands: maximum canonical payload 64 KiB; maximum envelope 96 KiB; no arbitrary JSON extensions.
- Evidence: maximum envelope 4 MiB, 512 observations, 128 selectors, 256 facts per observation, 4 KiB per string and 2 MiB cumulative fact bytes. Lower oracle/reader-specific limits may apply; callers cannot raise them.
- Pagination: default 100, hard maximum 1,000; opaque signed continuation token bound to issuer, organization, query and snapshot; no offset scan for large ledgers.
- Retries: maximum 8 policy-controlled attempts with bounded backoff and one durable idempotency record; no unbounded in-memory queue/list.
- Ten million item masters, 300,000 users/customers/vendors and 100,000 machines/projects are accessed through indexed scope/resource keys and streaming pages; never loaded into one evidence envelope or process collection.
- Shared approved masters use `GLOBAL:MASTER` ownership plus explicit per-organization visibility. Company 1 and Company 2 transactional/financial/stock ledgers always use distinct `ORG:<id>` bindings and cannot be queried with global scope.
- Durable stores must partition/index issuer, organization, instance, resource and time; retain audit/evidence for the approved ten-year policy; archive without weakening immutable references.
- Scale/load, database query plans, concurrency and retention capacity cannot be proven offline and remain external qualification prerequisites.

## Objective Correction 1 acceptance tests

“Persisted evidence” below means either an inspected deterministic fake-store record for an offline contract test or real durable rows/receipts for a separately authorized future PostgreSQL test. An offline test may not claim database acceptance.

| Test name | Initial state | Input | Action | Independently expected result | Required persisted evidence | Failure code | Cleanup | Class |
|---|---|---|---|---|---|---|---|---|
| `CanonicalV2GoldenVectorIsByteExact` | fixed UTC clock and published field vector | one complete V2 command | canonicalize twice and compare with checked-in literal bytes/hash in test source | byte-for-byte equality, exact field order/LF/lengths/hash | none; fake stores untouched | none | none | Offline |
| `EveryProtectedHeaderMutationIsRejected` | valid signed envelope and two trusted test keys/issuers | one-at-a-time mutation table below | verify without re-signing | no authorization/state/idempotency mutation; exact typed rejection per field | one sanitized denial in fake audit when issuer/key resolvable; otherwise no audit dependency disclosure | table below | clear deterministic fakes | Offline |
| `EveryPayloadFieldMutationBreaksHash` | valid envelope | mutate each payload property without changing signed hash | verify | rejected before authorization/state transition | denial with request ID and original/observed digest only | `PAYLOAD_HASH_MISMATCH` | clear fakes | Offline |
| `UnknownIssuerKeyAlgorithmVersionFailClosed` | registries loaded with one issuer/key/version/algorithm | four separately re-signed invalid vectors | verify each | state/version unchanged; no nonce/idempotency reservation | exact denial code per vector | `ISSUER_UNKNOWN`, `KEY_UNKNOWN`, `ALGORITHM_UNSUPPORTED`, `CONTRACT_UNSUPPORTED` | clear fakes | Offline |
| `RequestRoleCannotGrantAuthority` | subject resolves only MonitoringReader | envelope containing Operator role or operation | verify | rejected; resolver grant remains MonitoringReader | denial; zero lifecycle attempts | `REQUEST_ROLE_FORBIDDEN` or `SUBJECT_UNAUTHORIZED` | clear fakes | Offline |
| `AudienceSubjectAndScopeAreExact` | valid issuer/key, wrong audience/subject/org variants | separately signed variants | verify | each rejected, resource unchanged | exact denial per variant | `AUDIENCE_MISMATCH`, `SUBJECT_UNAUTHORIZED`, `ORGANIZATION_MISMATCH` | clear fakes | Offline |
| `ClusterInstanceOperationAndVersionSubstitutionReject` | resource version 7 in cluster C/instance I | signed variants C2, I2, wrong operation, version 6/8 | verify/authorize | no attempt or transition | denial bound to request/resource, no business facts | `CLUSTER_MISMATCH`, `INSTANCE_MISMATCH`, `OPERATION_MISMATCH`, `RESOURCE_VERSION_STALE` | clear fakes | Offline |
| `TemporalWindowIsServerOwned` | fixed clock T, max lifetime 5 min/skew 30 sec | future issued-at, future not-before, expired, 6-minute lifetime | verify | each rejected before nonce reservation | sanitized denial with policy time/category | `NOT_YET_VALID` or `ENVELOPE_EXPIRED` | clear fakes | Offline |
| `NonceReplayIsIndependentOfIdempotency` | first nonce reserved | same nonce with different idempotency key/request | verify second | second rejected; one reservation/attempt maximum | original nonce row plus replay denial | `NONCE_REPLAY` | clear fakes | Offline + future PostgreSQL |
| `LeaseAcquireRenewExpireAndFenceAreMonotonic` | resource Ready, no lease | acquire, renew, expire, then use old token | call lease contract and transition authorization | tokens strictly increase; old/expired token cannot mutate version/state | lease events and denial; one current token | `LEASE_EXPIRED` or `LEASE_FENCE_STALE` | release fake lease; future DB dropped only by authorized fixture | Offline + future PostgreSQL |
| `EveryUnlistedStateOperationPairIsIllegal` | each lifecycle state/version | every operation not present in matrix | evaluate transition | exact same state/version; no attempt | denial event for each pair | `STATE_TRANSITION_ILLEGAL` | clear fakes | Offline |
| `EveryListedTransitionHasExactRoleEvidenceAndFence` | each matrix row | valid and one-missing-requirement variants | evaluate/compare-exchange | valid row returns declared next state/version+1; missing role/evidence/fence leaves unchanged | transition or exact denial record | `SUBJECT_UNAUTHORIZED`, `READER_MISSING`, `LEASE_REQUIRED` or `LEASE_FENCE_STALE` | clear fakes | Offline + future PostgreSQL |
| `IdempotencyDecisionTableIsExact` | fake durable store in each table state | first/replay/collision/concurrent/retryable/nonretryable vectors | reserve/read/complete | outcome exactly matches decision table; one execution maximum | exact reservation/attempt/result/replay records | `IDEMPOTENCY_PAYLOAD_MISMATCH`, `NONCE_REPLAY`, `IDEMPOTENCY_NONRETRYABLE` as applicable | clear fakes | Offline + future PostgreSQL |
| `ConcurrentDuplicateHasOneAuthoritativeWinner` | no idempotency row | two simultaneous identical requests | synchronize at reservation barrier | one attempt/transition; identical result digest to both | one reservation and attempt, two caller/replay links | none | clear fakes; future isolated DB fixture cleanup | Offline + future PostgreSQL |
| `OracleManifestAndReadersAreServerPinned` | pinned oracle O/v2/hash H and readers R1/R2 | evidence claims wrong version/hash or missing/substituted reader | verify | no oracle evaluation/verdict/Accepted transition | exact denial; zero acceptance audit | `ORACLE_MISMATCH`, `READER_MISSING`, `READER_UNAUTHORIZED` | clear fakes | Offline + future PostgreSQL readers |
| `CallerVerdictAndExpectedValuesAreUnmapped` | strict JSON parser and pinned oracle | extra `pass`, `verdict`, `disposition`, `expected` or `formula` property | deserialize/verify | input rejected before oracle call | denial with field category, not value | `EVIDENCE_SENSITIVE_FIELD` for prohibited semantic fields | clear fakes | Offline |
| `TemporalEvidenceBindingIsExact` | action at T with binding B | before after T, after before T, future durable, wrong lease/fence/request | verify | no disposition or state mutation | exact denial with envelope ID/binding category | `READER_UNAUTHORIZED`, `LEASE_FENCE_STALE` or `NOT_YET_VALID` | clear fakes | Offline + future PostgreSQL |
| `AllEvidenceDimensionsAreServerBounded` | server limits from options | each limit+1: envelope bytes, observations, selectors, facts, string, cumulative facts | verify | reject before oracle/large allocation; memory remains bounded | one sanitized denial; no fact values logged | `EVIDENCE_TOO_LARGE` | release buffers/fakes | Offline |
| `SensitiveFactsNeverSerializeOrLog` | schema permits only count/hash/status | password/token/private key/PAN/bank/payroll/free-text fields and sentinel values | verify, inspect JSON/log/audit fakes | rejection; sentinel absent from every output | denial contains only field category/digest | `EVIDENCE_SENSITIVE_FIELD` | clear buffers/fakes | Offline |
| `MissingVerifierDependencyReturnsNotReady` | one readiness dependency removed per row | health/readiness request and direct verifier call | check readiness then call | HTTP 503 `NOT_READY`; direct call rejected; no oracle/state action | sanitized dependency code only | `SERVICE_NOT_READY` | restore fake registration | Offline |
| `RuntimeIdentityCannotEscalateAcrossRoles` | each identity in role matrix | every prohibited operation/audience pair | resolve/authorize | exact denial; no signing/state/evidence/audit mutation | denial audit where permitted | `SUBJECT_UNAUTHORIZED` | clear fakes | Offline + future IAM/database ACL |
| `AuditAppendFailurePreventsVerdictCommit` | otherwise valid acceptance, audit sink fails | authoritative evidence | verify | no Accepted/Failed transition returned or committed | failed audit attempt and unchanged lifecycle version | `AUDIT_APPEND_FAILED` | clear fakes | Offline + future PostgreSQL/storage |
| `MalformedCanonicalInputHasTypedFailure` | loaded registries | bad UTF-8, CRLF, duplicate field, length mismatch, invalid hex/base64/time/number | parse/verify | deterministic typed rejection; no raw exception text | sanitized denial only when safe | `SIGNATURE_INVALID`, `PAYLOAD_HASH_MISMATCH` or `CONTRACT_UNSUPPORTED` per parser stage | clear buffers | Offline |
| `TenMillionMasterContractUsesPagingOnly` | synthetic reader reports 10,000,000 rows without materializing them | pages of 1,000 with bound token | enumerate three pages/cancel | maximum in-memory page ≤1,000; token scope exact; cancellation stops reads | page receipts/counts only | none; wrong token → `READER_UNAUTHORIZED` | dispose enumerator/fakes | Offline; future scale qualification |

### Protected-header mutation expectations

The parameterized `EveryProtectedHeaderMutationIsRejected` test mutates exactly one received field without re-signing. Registry setup supplies alternate known values where needed so verification reaches the intended check. Exact outcomes are:

| Mutated field | Expected code |
|---|---|
| `contract_version` | `CONTRACT_UNSUPPORTED` |
| `canonicalization_version` | `CANONICALIZATION_UNSUPPORTED` |
| `algorithm` | `ALGORITHM_UNSUPPORTED` |
| `key_id` | `KEY_UNKNOWN` for unknown ID; `SIGNATURE_INVALID` for another known issuer-bound key |
| `issuer` | `ISSUER_UNKNOWN` for unknown issuer; `SIGNATURE_INVALID` for another known issuer |
| `audience` | `SIGNATURE_INVALID` before semantic audience evaluation |
| `subject` | `SIGNATURE_INVALID` |
| `authorized_role` | `SIGNATURE_INVALID` |
| `authorized_scope` | `SIGNATURE_INVALID` |
| `organization_id` | `SIGNATURE_INVALID` |
| `database_cluster_id` | `SIGNATURE_INVALID` |
| `database_instance_id` | `SIGNATURE_INVALID` |
| `operation` | `SIGNATURE_INVALID` |
| `resource_id` | `SIGNATURE_INVALID` |
| `resource_version` | `SIGNATURE_INVALID` |
| `lease_id` | `SIGNATURE_INVALID` |
| `fencing_token` | `SIGNATURE_INVALID` |
| `request_id` | `SIGNATURE_INVALID` |
| `idempotency_key` | `SIGNATURE_INVALID` |
| `nonce` | `SIGNATURE_INVALID` |
| `issued_at` | `SIGNATURE_INVALID` |
| `not_before` | `SIGNATURE_INVALID` |
| `expires_at` | `SIGNATURE_INVALID` |
| `canonical_payload_sha256` | `SIGNATURE_INVALID` because the signed header is verified before payload comparison |
| `canonical_payload_length` | `SIGNATURE_INVALID` because the signed header is verified before payload comparison |

Separately re-signed semantic-invalid vectors prove issuer, audience, subject, scope, temporal, resource, lease and operation rules. Mutation tests alone are not authorization tests.

## External prerequisites not provable offline

- Real KMS/HSM signing and verification, protected issuer/key registry, rotation/revocation and workload-identity issuance.
- Durable PostgreSQL/control-store transactions for nonce, idempotency, lease/fence, state compare-exchange, transition and audit atomicity.
- Deployed authoritative database evidence readers, pinned built oracle artifact and independent verifier deployment.
- Actual backend/database ACL grants and denials for all identities in the role matrix.
- Immutable evidence/audit object storage, retention/legal hold, export delivery controls and privacy operations.
- Private networking, mTLS, service discovery, queue/outbox/workers, monitoring, paging, runbooks and disaster recovery.
- Representative scale/query-plan/load/chaos qualification and ten-year retention capacity.

Offline fakes may prove decision logic and fail-closed interfaces only. They may not be reported as PostgreSQL, IAM, KMS, deployment, immutable-storage or production evidence.

## Stop conditions

Correction 1 must stop without source commit if any of these occurs:

1. Starting HEAD or required report hashes/lineage differ from the management authorization.
2. Any path outside the exact 13-file allowlist is needed or changed.
3. A real key, credential, endpoint, connection string, PostgreSQL/network call or production dependency is required.
4. Complete signed-field coverage cannot be demonstrated by the mutation matrix.
5. Request-supplied roles, expectations or verdict fields remain accepted.
6. State, lease/fence, nonce or idempotency decisions can bypass an authoritative interface.
7. Verifier readiness can report READY with a missing/ambiguous/unhealthy dependency.
8. Any required build/test/scan fails or warnings are introduced.
9. Existing ERP Purchase/Stores behavior, F23-01, migrations or snapshots change.
10. The correction would claim source safety, execution readiness or PostgreSQL evidence before a fresh independent review.

Safe completed work may be checkpointed only if internally consistent and all changed paths remain within the allowlist; tests or acceptance rules must not be weakened.

## Exact next management gate

Authorize **REV869B Option-A Phase-1 Correction 1 source-only implementation** from the reconciliation report commit, repeating the exact 13-file allowlist above and the stop conditions. The authorization must continue to prohibit PostgreSQL, deployment, provisioning, real keys, external calls, lifecycle execution, Correction 29 and legacy-reference access. It must require one correction source commit, the specified checkpoint, complete offline/mutation validation, and then a new fresh independent source-only security review before any later gate.

## Required states

phase1_failure_reconciliation_state=PASS
phase1_correction_1_source_only_gate=GO
frozen_architecture_state=RETAIN
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
correction_29_state=NOT_STARTED
production_readiness_state=NOT_READY
