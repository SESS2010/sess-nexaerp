# REV869B Option-A Phase A Source Checkpoint

Date: 2026-08-16  
Checkpoint type: report-only handoff for the authorized source-only Phase A implementation  
Architecture specification: `outputs/rev869b_external_controller_phase1_architecture_freeze_specification.md`  
Architecture specification SHA-256: `3F0BC461865D69E3D9827D763D7C403E3BD4E82ECF488AE4FDF3E48D9722DDB8`  
Entry HEAD: `51476760adcea9ed7babbc04d642e53e371c6941`  
Entry parent: `7c87b510ffdc6d5f2edaf821d385185b5f987cf5`

## Management gate and scope

Management approval for Option A architecture and Phase A only was accepted as the authority for this change. Phase B, Correction 2, PostgreSQL execution, external provisioning, deployment, live keys, network calls, lifecycle execution, and production operations were not authorized and were not performed.

The implementation remained inside the exact authorized thirteen-path maximum:

1. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
2. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BCompatibilityManifestV1.cs`
3. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
4. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
5. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
6. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
7. `src/SESS.NexaERP.ControlPlane/Program.cs`
8. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
9. `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`
10. `src/SESS.NexaERP.AcceptanceVerifier/Program.cs`
11. `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`
12. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
13. `outputs/rev869b_external_controller_phase_a_checkpoint.md`

The pre-existing untracked `../legacy-reference/` entry remained untouched. Its contents were not accessed, enumerated, or modified.

## Phase A implementation outcome

Phase A establishes compile-time architecture and trust contracts; it does not fabricate external production infrastructure.

- Fourteen production responsibilities have exactly one distinct owner interface. NexaERP business runtime, Control Plane, Acceptance Verifier, durable transaction authority, signing, readers, immutable audit, lifecycle, recovery, purge, and export authorities remain separated.
- Untrusted business intent cannot carry trusted role, permission, or authorized-scope grants. Trusted issuer, audience, role/scope, algorithm/version, clock, key metadata, and authorization bindings are explicit immutable contracts.
- The protected V2 command service has no public signing method and no public typed-envelope verification bypass. Its single public verification method accepts raw canonical header bytes, raw canonical payload bytes, signature bytes, authenticated transport subject, and expected resource binding.
- Cryptographic/trust parsing completes before a `VerifiedLifecycleCommandV3` is sent to the sole `ILifecycleControllerAuthority`. The service constructor no longer receives or mutates nonce, idempotency, lease, lifecycle, or audit stores independently.
- The Phase A lifecycle table binds state, operation, exact trusted role, success/failure state, lease requirement, evidence requirement set, and retry cap. Authorization, company/database scope, resource version, canonical digest, evidence manifest, idempotency identity, nonce, and lease/fence identity are checked together.
- Canonical payload parsing is strict, case-sensitive, unmapped-member rejecting, byte-exact, and bounded. Canonical-header framing failures have a distinct typed failure.
- Idempotency distinguishes a live owner from completed replay, payload collision, retryable takeover, non-retryable terminal failure, conflict, and committed outcome.
- Acceptance verification rejects duplicate readers and caller verdict/expectation/formula fields. Caller-supplied facts are transport data only: the oracle receives reader-returned authoritative observations and receipts, and the audit records the authoritative envelope digest.
- Contract sizes, observation/fact/string limits, retry caps, pagination limits, tenant scope, deployment identity, immutable audit, evidence, readiness, and compatibility versions are finite and explicit.
- Both service readiness routes use the shared Phase A dependency matrix. Missing providers are `NOT_CONFIGURED`; duplicate owners are `POLICY_MISMATCH`; identity and version claims are verified; every dependency must be present exactly once and `READY`. With no external dependencies provisioned, production composition remains intentionally fail closed with HTTP 503.

## Objective contract/security evidence

The Phase A test assembly contains 46 passing deterministic offline tests. In addition to the retained 25-field header mutation matrix, payload mutation matrix, trust substitution, temporal, nonce, idempotency, lease/fence, lifecycle, evidence, audit, readiness, malformed-input, and paging tests, it independently asserts:

- exact closed Phase A compatibility versions;
- 14/14 responsibility ownership with distinct interface types;
- business-runtime/control-plane/verifier authority separation;
- absence of role/permission authority from untrusted intent;
- raw canonical input and no public signing/typed verification bypass;
- lifecycle-controller-only persistence delegation;
- all 15 missing dependency states, including durable store, KMS/HSM, oracle registry, reader registry, immutable audit, and target identity/ACL;
- exact one-provider-per-dependency readiness, duplicate-owner rejection, and false-READY version rejection;
- all seven readiness-state-to-failure-code mappings and unique trust-failure enum values;
- caller fact isolation from authoritative reader facts;
- no caller verdict fields or secret-bearing fields on evidence, audit, readiness, or deployment descriptors;
- finite command, evidence, observation, page, and retry bounds.

These tests use in-memory fakes. They are not production KMS, IAM, durable database, immutable-storage, reader, oracle, concurrency, network, or deployment evidence.

## Offline validation

| Check | Result |
|---|---|
| Existing solution build | PASS — 5 projects, 0 warnings, 0 errors |
| Phase A project/test graph build | PASS — 4 projects, 0 warnings, 0 errors |
| Phase A contract/security tests | PASS — 46 passed, 0 failed, 0 skipped |
| Focused existing REV869B non-PostgreSQL tests | PASS — 76/76 |
| Complete existing non-PostgreSQL suite | PASS — 450/450 |
| PostgreSQL scenario discovery only | PASS — exactly 34 discovered; 0 executed |
| PowerShell 5.1 AST | PASS — 24 scripts, 0 parse errors |
| EF migration discovery | PASS — `--no-connect`, inert loopback port 1, 13 migrations; applied state intentionally unknown |
| Migration uniqueness/order | PASS — 13 unique; REV869A ordinal 12 and REV869B ordinal 13, adjacent |
| Model/snapshot parity | PASS — 1/1, no connection |
| Prohibited implementation scan | PASS — no added Npgsql/DB connection, EF apply, database create/drop, psql, process/network client, or protected mutation endpoint |
| Privacy/secret scan | PASS — no added private key, access key, hard-coded credential, or sensitive fact logging |
| `git diff --check` before checkpoint | PASS |
| Exact path boundary before checkpoint | PASS — 12/12 source/test paths; this report is path 13 |

No PostgreSQL connection, database command, migration apply/remove, password prompt, external provisioning, external API call, or production operation occurred.

## Implementation file SHA-256

| File | SHA-256 |
|---|---|
| `Rev869BControllerMessagesV1.cs` | `31FAA5A98442EE7A6F93952A6CAE6D1AC4327417F9996934907E81544F434C0E` |
| `Rev869BCompatibilityManifestV1.cs` | `8EC77B0642EE84B5F8EE0EB869A3C1B866DF3394FD910CC1B23693A83CA0FE42` |
| `Rev869BExecutionBinding.cs` | `84131E71B846F06135635A631FB9ECD7459FF57748626ECDB7A406B8B42623FA` |
| `Rev869BControllerStateMachine.cs` | `4E537DA84828D3980D63AD7DCC97D3B500EED5CB0F2072F86493A3A2A0831538` |
| `SignedEnvelopeService.cs` | `CF2F5FC89C7C3F14876CD4F8E426A9F594CA14C829D491D9E793999E59CA4198` |
| `ControlPlaneOptions.cs` | `AF068630FB585E925223A8927DB9D06F5204BC97184F2FB5CF78EEC31EBDCA37` |
| `ControlPlane/Program.cs` | `782F4EA9394D4373DCBBCA9799DE57D454C6485A3EC7BF3DAABDDBB0A82C1777` |
| `ControllerContractEndpointsV1.cs` | `24AE2963F76E3CD2202585E53ABEAA3CDC0E3655A57CFF0B22EF320EEF88EB53` |
| `AcceptanceVerifierOptions.cs` | `9E1560509B2E379E15CAD6D531E3D2B30F7844E04CDFA86613A8408F976043FA` |
| `AcceptanceVerifier/Program.cs` | `39C98D2B5938EB2CB7268A01AD7FCD58ED9A03386AC3985B96163B6762ACFECB` |
| `ClosedEvidenceVerifierV1.cs` | `652CA2505F60DF9B684387CB485CFBEF8DF7DB872111D38CBC1F17BF83BB07BA` |
| `ArchitectureFreezeContractTests.cs` | `36912E42546A8096EB1E50A3FC918700F0321FE3B6FFB80A513182E67FB2B254` |

## External prerequisites and mandatory next gate

The following remain absent and blocking: deployed and independently operated Control Plane and Acceptance Verifier; durable HA control database and transactional outbox/queue; KMS/HSM and non-exportable distinct keys; immutable issuer/policy registries; authoritative evidence readers and pinned oracle artifacts; immutable audit/evidence storage and legal hold; mTLS/workload identities, private network, IAM and database ACL realization; backup/restore and DR evidence; isolated PostgreSQL concurrency and behavioral evidence; scale/load/chaos qualification; monitoring, runbooks, training, and production approval.

The mandatory next gate is a fresh independent source-only architecture and security review of the committed Phase A source and this checkpoint. Phase B and Correction 2 remain NO_GO until separately approved after that review.

`management_approval_state=APPROVED_OPTION_A_PHASE_A_ONLY`

`phase_a_source_state=COMPLETE_PENDING_INDEPENDENT_REVIEW`

`phase_a_internal_validation_state=PASS`

`phase_a_fresh_independent_review_required=YES`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`frozen_architecture_state=RETAIN`

`external_prerequisite_blocking_state=YES`

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`
