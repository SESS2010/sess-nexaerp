# REV869B Option-A Phase-1 Correction 1 checkpoint

## Outcome and authority

Correction 1 is complete as one bounded source-only implementation and is ready for a fresh independent source-only architecture/security review.

- Starting HEAD: `5128e45f7938d8269ca0c40dd151f29c57c34882`.
- Authorizing reconciliation: `outputs/rev869b_external_controller_phase1_failure_reconciliation.md`.
- Reconciliation SHA-256: `C76F40A7E2D0EE5DE9362287DD798008805C34FD5B4F5112B1BA6F40BB67FF5D`.
- Frozen architecture: retained — external provisioning, dedicated lifecycle controller, surviving control-plane database, and target-local transactional ledgers.
- Validation completed: 2026-08-16 10:52 Asia/Kolkata, followed by final post-correction build/test and scan verification.
- PostgreSQL connections/tests/commands executed: 0.
- Provisioning, deployment, lifecycle, quarantine, recovery, drop, purge, export and production execution: 0.
- Real keys, credentials, external endpoints and network calls: 0.
- `../legacy-reference/`: not accessed or modified.

This checkpoint records offline source/interface behavior only. It does not claim PostgreSQL, KMS, IAM, deployment, immutable-storage, production or final ERP acceptance.

## Exact 13-file boundary

Exactly the authorized 12 source/test files and this checkpoint are changed:

1. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
2. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BCompatibilityManifestV1.cs`
3. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
4. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
5. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
6. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
7. `src/SESS.NexaERP.ControlPlane/Program.cs`
8. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
9. `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`
10. `src/SESS.NexaERP.AcceptanceVerifier/Program.cs`
11. `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`
12. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
13. `outputs/rev869b_external_controller_phase1_correction1_checkpoint.md`

No project, solution, migration, designer, model snapshot, Purchase/Stores source, PostgreSQL helper, SQL, deployment, infrastructure, prior report or excluded path changed.

## Correction summary

### B-01 — complete signed envelope

- V1 remains available only as the existing compatibility surface; protected operations publish `V1=UNSUPPORTED`.
- V2 signs exactly 25 ordered protected header fields plus the complete canonical payload digest and byte length.
- Framing is strict UTF-8 with the required prefix, LF endings, ordinal field order and UTF-8 byte lengths.
- Raw parsing rejects invalid UTF-8, CRLF, missing/extra/out-of-order fields, duplicate-equivalent framing, length mismatch, leading-zero/invalid integers, noncanonical UTC times, invalid nonce, invalid digest and non-byte-exact regeneration.
- Golden header SHA-256: `6DC374D17C11CD3B16432DAE30CCB280E5CD72ECBA97695773C22F28C307AF0D`.
- Signature verification precedes payload comparison and all semantic authorization checks.

### B-02 — issuer, audience and role trust

- Added immutable issuer/key/version/algorithm/audience/subject/role/scope/operation descriptors.
- Key-to-issuer and algorithm binding, activation/revocation, audience, authenticated subject and configured issuer policy are mandatory.
- Roles/scopes come from `IAuthorizationResolver`; a request cannot grant itself authority.
- Organization, cluster, instance, resource, version and operation substitutions produce typed failures.
- Runtime identity tests deny the protected Operator path to every non-Operator frozen identity class.

### B-03 — lifecycle, operation and lease/fence enforcement

- Added authoritative lease acquire/renew/read/consume and lifecycle read/compare-exchange interfaces.
- The exact primary operation/state/role/evidence/lease rows are encoded; every unlisted state/operation pair fails closed.
- Authorization state is versioned as Active, Consumed, Cancelled or Expired.
- Export state is versioned as None, Authorized, Delivering, Delivered, Expired or Failed.
- `CANCEL` requires the same authorizer and an unused active authorization; `EXPIRE` requires ControlPlaneRuntime, server time beyond expiry and an active authorization.
- Protected commands append an accepted-attempt audit receipt, perform exact lifecycle compare-exchange, then complete the idempotency result.

### B-04 — durable idempotency

- Added atomic reserve/read/complete/failure interfaces and canonical binding across issuer, organization, instance, operation, request ID, key and complete request digest.
- Changed-payload collisions and prior non-retryable failures are typed and stable.
- Nonce uniqueness is independent from idempotency.
- Deterministic concurrency tests prove one authoritative reservation/attempt result for two exact duplicates.
- Production durability and transaction atomicity remain external PostgreSQL/control-store prerequisites.

### B-05 — oracle and reader ownership

- Added server-pinned oracle manifests and reader registries with exact versions, hashes, scopes, fields and response bounds.
- Evidence contains raw typed facts, reader receipts and action receipt only; it contains no expected values, formula or caller disposition.
- Strict JSON parsing rejects caller `pass`, `fail`, `verdict`, `disposition`, `expected`, `formula` and every unmapped property.
- The verifier re-reads authoritative facts and matches reader receipt identity/version/hash/digest before oracle evaluation.
- Exact request, organization, cluster, instance, resource, version, operation, lease and fence bindings are enforced.
- Before observations must precede the action; After and Durable observations must follow it and remain within the server-owned window/skew.

### B-06 — fail-closed verifier/readiness

- Added separately validated Acceptance Verifier trust/oracle/reader/target/privacy/size/time options.
- Missing dependencies return `NOT_READY` with sanitized dependency codes and HTTP 503.
- Liveness remains separate; no protected verification network endpoint was added.
- Verifier options, workload identity, pinned target, oracle, readers, evidence limits and durable audit receipt are conjunctive.
- Audit append failure prevents a verification result from being returned.

## Server-owned limits and privacy

- Command header maximum: 96 KiB.
- Verifier evidence maximum: 4 MiB, 512 observations, 128 selectors, 256 facts per observation, 4 KiB per string and 2 MiB cumulative fact bytes.
- Configuration may lower these limits but cannot exceed them.
- Allowed fact fields and sensitive-field denials are disjoint and validated.
- Tests prove sensitive sentinel values are absent from rejection text and audit output.
- Paging test models a 10,000,000-row master while materializing at most 1,000 rows per page and only three requested pages.

## Objective offline test matrix

All 24 reconciliation-named objective tests are present and passing:

1. `CanonicalV2GoldenVectorIsByteExact`
2. `EveryProtectedHeaderMutationIsRejected` — 25/25 fields.
3. `EveryPayloadFieldMutationBreaksHash` — 8/8 payload components.
4. `UnknownIssuerKeyAlgorithmVersionFailClosed`
5. `RequestRoleCannotGrantAuthority`
6. `AudienceSubjectAndScopeAreExact`
7. `ClusterInstanceOperationAndVersionSubstitutionReject`
8. `TemporalWindowIsServerOwned`
9. `NonceReplayIsIndependentOfIdempotency`
10. `LeaseAcquireRenewExpireAndFenceAreMonotonic`
11. `EveryUnlistedStateOperationPairIsIllegal`
12. `EveryListedTransitionHasExactRoleEvidenceAndFence`
13. `IdempotencyDecisionTableIsExact`
14. `ConcurrentDuplicateHasOneAuthoritativeWinner`
15. `OracleManifestAndReadersAreServerPinned`
16. `CallerVerdictAndExpectedValuesAreUnmapped`
17. `TemporalEvidenceBindingIsExact`
18. `AllEvidenceDimensionsAreServerBounded`
19. `SensitiveFactsNeverSerializeOrLog`
20. `MissingVerifierDependencyReturnsNotReady`
21. `RuntimeIdentityCannotEscalateAcrossRoles`
22. `AuditAppendFailurePreventsVerdictCommit`
23. `MalformedCanonicalInputHasTypedFailure`
24. `TenMillionMasterContractUsesPagingOnly`

These are deterministic offline contract tests with in-memory fakes. They are not database, KMS, IAM, network, immutable-storage or production evidence.

## Offline validation

| Check | Result |
|---|---|
| Existing solution build | PASS — 5 projects, 0 warnings, 0 errors |
| Phase-1 project/test graph build | PASS — 4 projects, 0 warnings, 0 errors |
| Phase-1 contract/trust tests | PASS — 36 passed, 0 failed, 0 skipped |
| Focused existing REV869B non-PostgreSQL tests | PASS — 76/76 |
| Complete existing non-PostgreSQL suite | PASS — 450/450 |
| PostgreSQL scenario discovery only | PASS — 34 discovered, 34 unique, 0 executed |
| PowerShell 5.1 AST | PASS — 24 scripts, 0 errors |
| EF migration discovery | PASS — `--no-connect`, inert loopback port 1, 13 listed, applied status intentionally unknown |
| Migration uniqueness/order | PASS — 13 unique; REV869A ordinal 12 and REV869B ordinal 13, adjacent |
| Model/snapshot parity | PASS — 1/1, no connection |
| Offline Up SQL | PASS — 324,914 UTF-8 bytes, 2,635 lines, SHA-256 `39B067351894AB5732B6DF9C6348B04D708780AFAA18E073F8E6594D07FAF213` |
| Offline Down SQL | PASS — 11,720 UTF-8 bytes, 231 lines, SHA-256 `FC4BCB671501D601041FCED25D6053545BE9F38CF1D9982006953F47229E0AE4` |
| Prohibited implementation scan | PASS — no Npgsql/DB connection, EF apply, database create/drop, psql, process/network client or protected mutation endpoint in changed production sources |
| Privacy/logging scan | PASS — no changed production logging of fact, password, token, private-key, PAN, bank or payroll data |
| Secret material scan | PASS — no private key, access key or hard-coded credential assignment |
| Required named test inventory | PASS — 24/24 |
| Exact path boundary before checkpoint | PASS — 12/12 source/test paths; this report is the authorized thirteenth path |
| `git diff --check` | PASS |

## External prerequisites and exclusions

Still absent and blocking:

- real KMS/HSM keys, immutable issuer/key registry, rotation/revocation operations and workload identity;
- independently deployed Control Plane and Acceptance Verifier;
- durable control database/queue/outbox and atomic nonce/idempotency/lease/lifecycle stores;
- immutable audit/evidence storage with retention/legal hold;
- deployed authoritative readers and pinned built oracle artifact;
- exact IAM, mTLS/private networking, runtime ACL and database least-privilege evidence;
- PostgreSQL concurrency, replay, fencing, audit rollback, reader, drop/purge denial and behavioral scenario evidence;
- scale/load/chaos, backup/restore, DR, monitoring, runbooks, training and production approval.

No production implementation of a durable store, signer, reader, oracle catalogue or audit sink was fabricated. Production readiness endpoints remain fail closed.

## Conservative states and next gate

`external_controller_architecture_state=APPROVED_OPTION_A`

`phase1_correction_1_source_state=COMPLETE_PENDING_INDEPENDENT_REVIEW`

`phase1_correction_1_internal_validation_state=PASS`

`phase1_fresh_independent_review_required=YES`

`frozen_architecture_state=RETAIN`

`external_prerequisite_blocking_state=YES`

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`correction_29_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

Exact next gate: a fresh independent source-only architecture/security review of the committed Phase-1 Correction 1 source and this checkpoint. PostgreSQL, provisioning, deployment, real keys, external calls, lifecycle execution, Correction 29 and production remain unauthorized.
