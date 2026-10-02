# REV869B Option-A Phase-A Independent Source-Safety Review

Date: 2026-08-16

Review type: fresh, report-only, source-only architecture and security review

Reviewed commit: `18a6458cbddf50e8cd45c9f789be2bdd2e859b08`

Reviewed parent: `51476760adcea9ed7babbc04d642e53e371c6941`

Exact range: `51476760adcea9ed7babbc04d642e53e371c6941..18a6458cbddf50e8cd45c9f789be2bdd2e859b08`

Architecture authority: `outputs/rev869b_external_controller_phase1_architecture_freeze_specification.md`

## Executive verdict

**FAIL — CORRECTION_REQUIRED.**

The commit remains source-safe in the narrow sense that it adds no database connection, migration execution, database create/drop, external provisioning, protected mutation endpoint, hard-coded credential, or sensitive-fact logging. It also builds and its test suites pass. Those facts do not satisfy the Phase-A architecture stop gate.

The implementation exposes public typed V1 command and typed V2 evidence entry points despite the raw-ingress requirement; declares one 14-row owner catalog while retaining parallel public authority/store interfaces; constructs an authorization policy context synthetically in the concrete raw command path; omits `QUARANTINE`, `CANCEL`, and `EXPIRE` from the authoritative V3 lifecycle table; cannot enforce the frozen export substates; accepts unsigned authoritative-reader results without recomputing their response digest; preserves a caller-supplied action receipt in the oracle input; and tests several safety claims through production-derived expectations, sequential in-memory fakes, or unconditional-PASS oracles. No decisive mutation report exists.

The frozen Phase-A stop gate says that any public typed bypass, ambiguous policy, incomplete table, permissive oracle, or surviving decisive mutant stops Phase A. Multiple conditions are present. Phase B therefore remains **NO_GO**.

## Canonical state

| State item | Independent result |
|---|---|
| Phase-A source verdict | **FAIL** |
| Finding disposition | **CORRECTION_REQUIRED** |
| Management approval | Pending; this review does not grant it |
| Phase B | **NO_GO** |
| Correction 2 | **NO_GO** |
| Source-safety review | Completed from source and offline evidence |
| Provisioning | **NOT_STARTED** |
| PostgreSQL | **NOT_AUTHORIZED_NOT_RUN** |
| Production readiness | **NOT_READY** |
| Recommended next gate | One separate report-only Phase-A failure reconciliation; no automatic source correction |

## Review controls and scope

The entry HEAD and parent matched the required identities. Target-scoped status was clean at entry. The exact commit contains the following 13 authorized paths and no others:

1. `outputs/rev869b_external_controller_phase_a_checkpoint.md`
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

No implementation, test, project, migration, script, helper, configuration, installer, archive, or external system was changed by this review. No PostgreSQL command or connection was attempted. No helper or provisioning process was executed.

Reviewer handling note: a late global `git status --untracked-files=all` check expanded two filenames beneath the already-known out-of-scope sibling `../legacy-reference/`. Their file contents were not opened, read, copied, hashed, staged, or changed. This metadata-only enumeration was unnecessary and is disclosed because the intended boundary was not to enumerate that sibling. It does not supply evidence used by this review.

## Finding summary

| ID | Class | Severity | Finding |
|---|---|---|---|
| F-01 | CORRECTION_REQUIRED | BLOCKING | Public typed command and evidence entry points survive the raw-only boundary |
| F-02 | CORRECTION_REQUIRED | BLOCKING | The 14-row owner catalog is declarative; parallel public authorities remain and the concrete command path synthesizes policy/grant authority |
| F-03 | CORRECTION_REQUIRED | BLOCKING | The authoritative Phase-A lifecycle table is incomplete and export sequencing is unenforceable |
| F-04 | CORRECTION_REQUIRED | BLOCKING | The acceptance path is not a closed, signed, authoritative-fact verifier |
| F-05 | CORRECTION_REQUIRED | REQUIRED | Audit and readiness contracts do not preserve all frozen fail-closed facts |
| F-06 | CORRECTION_REQUIRED | BLOCKING | The 46-test suite is green but not independently decisive; required mutation evidence is absent |
| F-07 | CORRECTION_REQUIRED | REQUIRED | The checkpoint's `git diff --check` PASS claim is false for the reviewed range |

## Detailed findings

### F-01 — public typed ingress bypasses remain

Class: **CORRECTION_REQUIRED**

Severity: **BLOCKING**

The V2 compatibility manifest explicitly says protected V1 is `UNSUPPORTED` (`Rev869BCompatibilityManifestV1.cs:22`). Nevertheless:

- `SignedEnvelopeService.Sign(LifecycleCommandV1)` remains public and accepts a typed command (`SignedEnvelopeService.cs:348-366`).
- `SignedEnvelopeVerificationService.Verify(SignedCommandEnvelopeV1, ...)` remains public and accepts a typed envelope (`SignedEnvelopeService.cs:369-415`).
- `ClosedEvidenceVerifierV1` remains public (`ClosedEvidenceVerifierV1.cs:152`).
- `ClosedEvidenceVerifierV2.VerifyAsync(EvidenceVerificationRequestV2)` remains public and accepts a typed evidence request (`ClosedEvidenceVerifierV1.cs:260-272`).
- `StrictEvidenceJsonV2` exists (`ClosedEvidenceVerifierV1.cs:92`) but is not the input to `ClosedEvidenceVerifierV2.VerifyAsync`.

`SignedCommandServiceV2.VerifyAsync` correctly accepts raw canonical header and payload bytes (`SignedEnvelopeService.cs:607-621`), but that does not make it the only public protected surface. The test at `ArchitectureFreezeContractTests.cs:753` inspects only `SignedCommandServiceV2`, so it cannot detect the surviving public V1 signer/verifier or the typed evidence verifier. Tests at lines 63 and 85 actively exercise the V1 typed path.

Required correction outcome: make unsupported typed protected paths non-public or otherwise impossible to invoke as protected authority, make raw canonical parsing the sole public command/evidence ingress, and prove the complement by reflection/API-surface expectations independent of the implementation under test.

### F-02 — ownership and policy authority are not closed

Class: **CORRECTION_REQUIRED**

Severity: **BLOCKING**

The `PhaseAOwnershipCatalog` contains 14 distinct interface types (`Rev869BControllerMessagesV1.cs:1395-1416`), but effective ownership remains ambiguous:

- `ILeaseFenceStore`, `IIdempotencyStore`, and `ILifecycleStateStore` remain public parallel state authorities (`Rev869BExecutionBinding.cs:110,132,158`) beside `IDurableControlPlanePersistenceProvider`.
- `ITrustedIssuerRegistry` remains public and is injected into the concrete raw command service (`SignedEnvelopeService.cs:39,426-430`) beside `ITrustedIssuerKeyRegistryProvider`.
- `IEnvelopeSigner` remains public (`SignedEnvelopeService.cs:12`) beside `IKmsHsmSigningProvider`.
- `IAuthoritativeEvidenceReader`, `ITrustAuditSinkV2`, and `IVerificationAuditSinkV2` remain public beside the V3 authoritative-reader and immutable-audit providers.

The concrete raw command path resolves authority through the legacy `ITrustedIssuerRegistry` and `IAuthorizationResolver`, then constructs a `ResolvedAuthorizationV3` itself. It creates a synthetic grant ID, uses the ownership-contract version as the policy version, creates a synthetic policy row ID, and uses the request digest as the grant digest (`SignedEnvelopeService.cs:516-529`). It also hard-codes controller epoch `1` and derives lease expiry from the request header (`SignedEnvelopeService.cs:551-558`). Generic reader IDs, versions, and durable stages are synthesized from caller evidence requirement names (`SignedEnvelopeService.cs:559-568`).

These are useful adapters for a test fixture, but they are not a closed contract proving that policy/grant, lease/epoch, or reader authority can come only from their frozen owners. The Phase-A stop gate explicitly rejects ambiguous policy.

Required correction outcome: one unambiguous public authority contract per responsibility; compatibility helpers must be non-authoritative and non-public where appropriate; the raw command boundary must consume a server-owned signed policy/grant result and durable lease/fence expectation rather than manufacture authority from request/header data.

## Fourteen-responsibility ownership matrix

“Catalog” assesses the one-to-one dictionary. “Effective boundary” assesses all public reviewed surfaces.

| Responsibility | Catalog owner | Catalog | Effective boundary |
|---|---|---|---|
| NexaERP business runtime | `INexaErpBusinessRuntime` | PASS | PASS — submits untrusted intent only |
| Control Plane | `IControlPlaneAuthority` | PASS | FAIL — concrete legacy/V2 services expose alternate protected entry paths |
| Acceptance Verifier | `IAcceptanceVerifierAuthority` | PASS | FAIL — public V1/V2 typed verifiers remain |
| Durable control-plane persistence | `IDurableControlPlanePersistenceProvider` | PASS | FAIL — three public legacy state-store authorities overlap it |
| Trusted issuer/key registry | `ITrustedIssuerKeyRegistryProvider` | PASS | FAIL — `ITrustedIssuerRegistry` drives the concrete command path |
| KMS/HSM signing | `IKmsHsmSigningProvider` | PASS | FAIL — public `IEnvelopeSigner` is an alternate signer |
| Authoritative evidence reader | `IAuthoritativeEvidenceReaderProvider` | PASS | FAIL — public unsigned V2 reader result path overlaps it |
| Immutable audit evidence | `IImmutableAuditEvidenceProvider` | PASS | FAIL — public V2 trust/verifier audit sinks overlap it |
| Lifecycle controller | `ILifecycleControllerAuthority` | PASS | FAIL — legacy public state-machine/store mutation path remains alongside V3 |
| Backup/recovery authority | `IBackupRecoveryAuthority` | PASS | PASS at Phase-A interface scope |
| Purge authorizer | `IPurgeAuthorizer` | PASS | PASS at Phase-A interface scope |
| Purge executor | `IPurgeExecutor` | PASS | PASS at Phase-A interface scope |
| Export authorizer | `IExportAuthorizer` | PASS | PASS at Phase-A interface scope |
| Export delivery executor | `IExportDeliveryExecutor` | PASS | PASS at Phase-A interface scope |

### F-03 — lifecycle table is incomplete

Class: **CORRECTION_REQUIRED**

Severity: **BLOCKING**

The authoritative V3 `PhaseARules` table contains 25 implementation rows (`Rev869BControllerStateMachine.cs:44-96`) but omits the frozen `QUARANTINE`, `CANCEL`, and `EXPIRE` rows. Those operations exist only in the legacy path (`Rev869BControllerStateMachine.cs:139-143,223-259`) and are not accepted by `RequirePhaseACommand`, which consults only `PhaseARules` (`Rev869BControllerStateMachine.cs:168-206`).

The three V3 export rules all use lifecycle state `Accepted` as both current and next state (`Rev869BControllerStateMachine.cs:90-95`). `VerifiedLifecycleCommandV3` carries no export substate, so the V3 route cannot enforce `NONE/EXPIRED/FAILED -> AUTHORIZED -> DELIVERING -> DELIVERED`. A caller can request `EXPORT` or `COMPLETE_EXPORT` from `Accepted` without proving the preceding export substate.

The V3 lease check compares expiry, lease ID, fence, and holder (`Rev869BControllerStateMachine.cs:195-203`) but cannot bind the lease to a resource because `LeaseFenceExpectationV3` has no resource ID. It also does not validate a positive/current controller epoch. Exact operation-specific audit names are collapsed into generic enum kinds.

The following matrix compares every frozen conceptual transition. “Partial” means the V3 table has the state/operation shape but does not freeze the full identity/evidence/audit semantics. It is not an acceptance result.

| # | Frozen transition | V3 result | Independent assessment |
|---:|---|---|---|
| 1 | Registered / AUTHORIZE_PREPARE / Preflight | Present | PARTIAL — simplified `Operator`, evidence ID, generic authorization audit |
| 2 | Preflight / PREPARE / Provisioning | Present | PARTIAL — exact identity/TLS/catalog/ACL and `prepare_started` not represented |
| 3 | Provisioning / COMPLETE_PREPARE / Ready | Present | PARTIAL — signed/authoritative semantics and exact audit not represented |
| 4 | Provisioning / FAIL / Failed | Present | PARTIAL — conflict-to-quarantine and exact audit semantics not frozen |
| 5 | Ready / AUTHORIZE_EXECUTE / MigrationAuthorized | Present | PARTIAL — deployment-controller grant semantics and exact audit not frozen |
| 6 | MigrationAuthorized / EXECUTE / Migrating | Present | PARTIAL — one-time consumption and exact audit not frozen by the rule |
| 7 | Migrating / COMPLETE_EXECUTE / VerificationPending | Present | PARTIAL — signed receipt semantics and exact audit not frozen |
| 8 | Migrating / FAIL / Failed | Present | PARTIAL — rollback/ambiguity semantics and exact audit not frozen |
| 9 | VerificationPending / VERIFY_ACCEPT / Accepted | Present | PARTIAL — exact PASS/hash/WORM semantics and audit not frozen |
| 10 | VerificationPending / VERIFY_REJECT / Failed | Present | PARTIAL — exact reason and unverifiable handling not frozen |
| 11 | Any nonterminal / QUARANTINE / Quarantined | **Missing** | **FAIL** |
| 12 | Quarantined / AUTHORIZE_RECOVER / RecoveryAuthorized | Present | PARTIAL — approval/identity semantics and exact audit not frozen |
| 13 | RecoveryAuthorized / RECOVER / Recovering | Present | PARTIAL — one-time grant consumption and exact audit not frozen |
| 14 | Recovering / COMPLETE_RECOVER / Ready | Present | PARTIAL — signed restored/ready semantics and exact audit not frozen |
| 15 | Recovering / FAIL / Failed | Present | PARTIAL — ambiguity handling and exact audit not frozen |
| 16 | Accepted, Failed, or Quarantined / AUTHORIZE_DROP / DropAuthorized | Expanded to 3 rows | PARTIAL — state coverage exists; dual approval/exact reason/audit do not |
| 17 | DropAuthorized / DROP / Dropped | Present | PARTIAL — interrupted outcome and exact audit not represented |
| 18 | Dropped / AUTHORIZE_PURGE / PurgeAuthorized | Present | PARTIAL — Records+Data Owner authorization and audit not represented |
| 19 | PurgeAuthorized / PURGE / Purging | Present | PARTIAL — exact batch/grant semantics and audit not represented |
| 20 | Purging / COMPLETE_PURGE / Purged | Present | PARTIAL — zero-candidate proof semantics and exact audit not represented |
| 21 | Purging / FAIL / Dropped | Present | PARTIAL — root-drift quarantine and exact audit not represented |
| 22 | Accepted NONE/EXPIRED/FAILED / AUTHORIZE_EXPORT / AUTHORIZED | Substate absent | **FAIL** |
| 23 | Accepted AUTHORIZED / EXPORT / DELIVERING | Substate absent | **FAIL** |
| 24 | Accepted DELIVERING / COMPLETE_EXPORT / DELIVERED | Substate absent | **FAIL** |
| 25 | Active unused authorization / CANCEL / CANCELLED | **Missing** | **FAIL** |
| 26 | Expired active authorization / EXPIRE / EXPIRED | **Missing** | **FAIL** |

Required correction outcome: one V3 table that preserves all 26 conceptual rows, including export and authorization substates, exact identity, evidence, lease/resource/epoch/fence/version, failure/retry, and typed audit semantics. The expected table must be frozen independently from production enumeration.

### F-04 — verifier facts are not cryptographically closed

Class: **CORRECTION_REQUIRED**

Severity: **BLOCKING**

`ClosedEvidenceVerifierV2` replaces caller raw facts with observations returned by `IAuthoritativeEvidenceReader`, which is directionally correct (`ClosedEvidenceVerifierV1.cs:317-352`). The returned `AuthoritativeEvidenceFactsV2` contains facts, a receipt, and observations but no key, algorithm, or signature (`Rev869BControllerMessagesV1.cs:661-664`). The verifier compares only reader ID, reader version, and the returned receipt's `ResponseDigest` to the caller-supplied receipt (`ClosedEvidenceVerifierV1.cs:327-334`). It never recomputes that digest over the returned facts/observations and never verifies a reader signature.

Consequently, an arbitrary reader implementation can return changed observations while copying the caller receipt digest. The tests do exactly that through `authoritativeMutation` and `FakeAuthoritativeReader`; no negative test binds mutated authoritative observations to a verified digest/signature.

Additional closure gaps:

- The caller-supplied `ActionReceipt` at `Rev869BControllerMessagesV1.cs:676` remains in `authoritativeEvidence` and reaches `oracle.Evaluate` (`ClosedEvidenceVerifierV1.cs:342-356`).
- Reader descriptor `AllowedFields`, `MaximumResponseFacts`, `MaximumResponseBytes`, and `SourceType` are not enforced against each returned response.
- The V2 temporal binding lacks attempt ID, exact action stage, and durable cleanup/reconciliation identity; only broad window checks are possible.
- The public typed request path does not invoke `StrictEvidenceJsonV2`.
- The oracle interface receives a typed envelope and the test oracle always returns PASS (`ArchitectureFreezeContractTests.cs:1658-1667`).
- Signed `AuthoritativeFactBundleV3` and `CanonicalEvidenceEnvelopeV3` types exist (`Rev869BControllerMessagesV1.cs:1073-1092`) but are not used by the concrete V2 verifier.

Required correction outcome: raw canonical evidence ingress; server-owned reader requests bound to company/database/resource/version/attempt/stage/time/watermark; signature and digest verification over every returned bundle; enforcement of descriptor source/field/count/byte bounds; elimination of caller action/outcome/expected-value influence; and mutation-sensitive PASS/FAIL oracle fixtures.

### F-05 — audit and readiness contracts are incomplete

Class: **CORRECTION_REQUIRED**

Severity: **REQUIRED**

`ImmutableAuditEventV3` contains actor, organization, database, resource, operation, request hash, policy, outcome, prior hash, and time (`Rev869BControllerMessagesV1.cs:1117-1130`). It does not carry exact previous/new lifecycle state and version, attempt ID, authorization/grant digest, lease/controller epoch/fence, source transaction ID, signing key/version, or immutable ingestion receipt. `AuditEventKindV3` is generic (`Rev869BControllerMessagesV1.cs:798-813`) and cannot encode the exact lifecycle event names frozen in the transition matrix.

Readiness correctly requires all dependency enum values to be present and READY (`Rev869BControllerMessagesV1.cs:1139-1148`), and both current hosts register zero providers, so `/health/ready` returns 503. However:

- a provider exception escapes `PhaseAReadinessAuthority` rather than producing a typed unavailable/degraded snapshot;
- dependency results contain no observation timestamp/expiry, so stale READY facts cannot be detected;
- no common protected-handler guard is represented or tested;
- the concrete V2 verifier still uses legacy `ITrustReadinessProbe`, not the V3 conjunctive authority;
- readiness tests use fakes and do not exercise both registered HTTP routes or exception/staleness cases.

No operational protected endpoint was added, which is correct for Phase A. Actual dependency adapters and common route enforcement remain later-phase work, but Phase-A contracts and offline expectations must still express and reject every missing, invalid, stale, exception, version, identity, and policy condition.

### F-06 — tests pass but are not independently decisive

Class: **CORRECTION_REQUIRED**

Severity: **BLOCKING**

All 46 tests pass, but the architecture requires independent expectation ownership and a decisive mutation report. No mutation report was supplied. Important weakenings survive the suite:

- The illegal-pair test derives its allowlist from `machine.ListedOperations` (`ArchitectureFreezeContractTests.cs:371-389`).
- The listed-transition test iterates `machine.ListedOperationRules` (`ArchitectureFreezeContractTests.cs:391-462`).
- Neither test asserts the exact V3 `PhaseARules` matrix; missing V3 `QUARANTINE`, `CANCEL`, `EXPIRE`, and export substates survive.
- The concurrency test calls an in-memory fake twice sequentially (`ArchitectureFreezeContractTests.cs:479`) rather than testing concurrent calls through the production boundary.
- The V2 oracle fake unconditionally returns PASS (`ArchitectureFreezeContractTests.cs:1658-1667`).
- The public-surface reflection test inspects only `SignedCommandServiceV2`.
- The readiness tests use fake providers and implementation enum enumeration.
- The ten-million-row test reads three pages from a fake offset pager (`ArchitectureFreezeContractTests.cs:686-700,1687`) and does not prove signed opaque-token, cancellation, wrong-token, gap/duplicate, retry, or memory behavior.

Per-test quality assessment:

| # | Test | Assessment |
|---:|---|---|
| 1 | `Compatibility_manifest_is_closed_and_versioned` | PARTIAL — constants only; surviving public V1 path not rejected |
| 2 | `Canonical_json_is_deterministic_and_sorts_object_keys` | PARTIAL — useful unit vector, not complete ingress proof |
| 3 | `State_machine_rejects_skip_and_allows_frozen_path` | NON-DECISIVE — legacy path, not exact V3 matrix |
| 4 | `Binding_comparison_fails_closed_on_company_instance_lease_and_subcase` | PARTIAL — legacy typed binding |
| 5 | `Command_signing_and_verification_rejects_replay_and_revocation` | NON-DECISIVE — exercises forbidden public V1 path |
| 6 | `Command_verification_fails_closed_for_version_algorithm_key_tamper_and_staleness` | NON-DECISIVE — exercises forbidden public V1 path |
| 7 | `Command_policy_rejects_stale_lease_and_cross_role_authorization` | PARTIAL — does not prove server-owned durable lease/policy |
| 8 | `Evidence_contract_has_no_caller_supplied_verdict` | PARTIAL — action receipt and typed evidence remain |
| 9 | `Closed_verifier_calculates_pass_and_writes_durable_audit_reference` | NON-DECISIVE — unconditional-PASS V1 oracle |
| 10 | `Closed_verifier_rejects_missing_durable_stage_before_oracle_runs` | PARTIAL — useful order assertion on legacy path |
| 11 | `Closed_verifier_enforces_bounded_payload_rules` | PARTIAL — useful bounds, not authoritative-reader closure |
| 12 | `Options_reject_production_identity_and_accept_bounded_nonproduction_pattern` | PARTIAL — configuration unit check |
| 13 | `CanonicalV2GoldenVectorIsByteExact` | DECISIVE for its single golden vector |
| 14 | `EveryProtectedHeaderMutationIsRejected` | STRONG/PARTIAL — fake crypto and legacy authorities remain |
| 15 | `EveryPayloadFieldMutationBreaksHash` | STRONG/PARTIAL — payload integrity only |
| 16 | `UnknownIssuerKeyAlgorithmVersionFailClosed` | PARTIAL — fake registry/crypto path |
| 17 | `RequestRoleCannotGrantAuthority` | PARTIAL — useful resolver check, not signed grant ownership |
| 18 | `AudienceSubjectAndScopeAreExact` | PARTIAL — useful binding checks |
| 19 | `ClusterInstanceOperationAndVersionSubstitutionReject` | PARTIAL — useful substitution checks |
| 20 | `TemporalWindowIsServerOwned` | PARTIAL — request-header time still supplies derived lease facts |
| 21 | `NonceReplayIsIndependentOfIdempotency` | PARTIAL — in-memory stores |
| 22 | `LeaseAcquireRenewExpireAndFenceAreMonotonic` | PARTIAL — in-memory fake, no resource/epoch contract proof |
| 23 | `EveryUnlistedStateOperationPairIsIllegal` | NON-DECISIVE — production-derived allowlist |
| 24 | `EveryListedTransitionHasExactRoleEvidenceAndFence` | NON-DECISIVE — iterates production legacy table |
| 25 | `IdempotencyDecisionTableIsExact` | PARTIAL — decision unit, not one durable transaction |
| 26 | `ConcurrentDuplicateHasOneAuthoritativeWinner` | NON-DECISIVE — sequential fake calls |
| 27 | `OracleManifestAndReadersAreServerPinned` | PARTIAL — no signature/digest binding of returned facts |
| 28 | `CallerVerdictAndExpectedValuesAreUnmapped` | PARTIAL — parser is not production verifier ingress |
| 29 | `TemporalEvidenceBindingIsExact` | PARTIAL — missing attempt/stage/cleanup binding |
| 30 | `AllEvidenceDimensionsAreServerBounded` | PARTIAL — descriptor response limits/source are unenforced |
| 31 | `CallerFactsCannotReplaceAuthoritativeReaderFacts` | PARTIAL — caller action receipt remains; reader facts unsigned |
| 32 | `SensitiveFactsNeverSerializeOrLog` | PARTIAL — useful serialization check, not full sink analysis |
| 33 | `MissingVerifierDependencyReturnsNotReady` | NON-DECISIVE — legacy Boolean fake |
| 34 | `RuntimeIdentityCannotEscalateAcrossRoles` | PARTIAL — contract/type assertion |
| 35 | `AuditAppendFailurePreventsVerdictCommit` | PARTIAL — useful fail-closed fake-sink check |
| 36 | `MalformedCanonicalInputHasTypedFailure` | PARTIAL — standalone parser not verifier call path |
| 37 | `TenMillionMasterContractUsesPagingOnly` | NON-DECISIVE — three-page offset fake |
| 38 | `PhaseACompatibilityManifestIsExactAndClosed` | DECISIVE for constant values only |
| 39 | `EveryPhaseAProductionResponsibilityHasOneDistinctOwnerContract` | PARTIAL — dictionary only; parallel interfaces ignored |
| 40 | `UntrustedIntentCannotCarryRoleScopeOrPermissionAuthority` | STRONG for the V3 intent DTO surface |
| 41 | `ProtectedCommandSurfaceAcceptsRawCanonicalBytesAndDelegatesOnlyToController` | NON-DECISIVE — inspects one class only |
| 42 | `MissingPhaseADependenciesAreEnumeratedAndFailClosed` | PARTIAL — no exceptions, stale facts, or HTTP integration |
| 43 | `OnlyOneReadyProviderPerDependencyCanEnableProtectedOperations` | PARTIAL — implementation enum supplies expectation universe |
| 44 | `EveryReadinessStateHasAnExactTypedFailureCode` | DECISIVE for enum-to-code mapping only |
| 45 | `PhaseAEvidenceAndAuditSurfacesContainNoCallerVerdictOrSecretMaterial` | PARTIAL — reflection names do not prove authoritative influence/privacy |
| 46 | `PhaseAContractBoundsAreFiniteAndEnforced` | PARTIAL — contract validators only; paging/evidence adapters unproved |

Required correction outcome: assurance-owned frozen matrices and complement lists, negative oracle fixtures, raw-ingress API-surface checks across the complete public assembly, truly concurrent boundary tests, digest/signature mutations, readiness exception/staleness/HTTP tests, paging-token mutations, and a report showing that every decisive mutant is killed.

### F-07 — checkpoint validation discrepancy

Class: **CORRECTION_REQUIRED**

Severity: **REQUIRED**

The checkpoint claims `git diff --check` PASS. Independent execution on the exact reviewed range fails on trailing whitespace at checkpoint lines 3-7. Those spaces appear to be Markdown hard breaks and do not affect product execution, but the recorded validation claim is objectively false and must not be carried forward as PASS.

Required correction outcome: the failure reconciliation must record the discrepancy. A later authorized correction may remove the whitespace and rerun the exact-range check; this report does not authorize that edit.

## Independently reproduced offline validation

| Validation | Result |
|---|---|
| Entry HEAD / parent | PASS — exact required identities |
| Target-scoped status at entry | PASS — clean |
| Exact commit boundary | PASS — 13/13 authorized paths |
| Warning-as-error solution build | PASS — 5 projects, 0 warnings, 0 errors |
| ControlPlane/Verifier graph through Phase-A tests | PASS — 4 projects built |
| Phase-A contract/security tests | PASS — 46 passed, 0 failed, 0 skipped |
| Focused existing REV869B non-PostgreSQL tests | PASS — 76/76 |
| Complete existing non-PostgreSQL suite | PASS — 450/450 |
| Future Correction-17 PostgreSQL scenario discovery | PASS — exactly 34 listed; 0 executed |
| PostgreSQL execution | **NOT_AUTHORIZED_NOT_RUN** |
| Windows PowerShell 5.1 AST | PASS — 24 scripts, 0 parse errors, 0 executed |
| EF migration discovery | PASS — `--no-connect`, inert `127.0.0.1:1`, 13 migrations; applied state unknown |
| REV869A / REV869B uniqueness and adjacency | PASS — ordinals 12 and 13 |
| Model/snapshot parity | PASS — 1/1 no-connect test |
| Offline migration SQL contract | PASS — Up 324,914 bytes / SHA-256 `39B067351894AB5732B6DF9C6348B04D708780AFAA18E073F8E6594D07FAF213`; Down 11,720 bytes / SHA-256 `FC4BCB671501D601041FCED25D6053545BE9F38CF1D9982006953F47229E0AE4` |
| Added-source hard-coded credential scan | PASS — 0 matches |
| Added-source sensitive-log scan | PASS — 0 matches |
| Added-source database action scan | PASS — 0 matches |
| Added-source protected mutation endpoint scan | PASS — 0 matches |
| `git diff --check` | **FAIL** — checkpoint lines 3-7 contain trailing whitespace |

Passing compilation and regression tests establish compatibility with their current assertions. They do not override the source-trace findings or satisfy independent-mutation acceptance.

## Audit, privacy, and source-safety assessment

- No private key, access key, hard-coded password, or credential was added in the reviewed source.
- No sensitive fact logging was found in the added source.
- Evidence DTOs use typed facts and bounds, but the V2 reader response is not signed or digest-recomputed and therefore cannot be accepted as authoritative.
- Audit append failure blocks a V2 verdict in the unit path, but the V3 immutable-audit contract lacks several frozen lifecycle/transaction/fence/signing fields.
- Both services expose only liveness, readiness, and version HTTP routes; no protected mutation endpoint was added.
- Both services are intentionally not ready with zero dependency providers. This is safe for Phase A and is not production readiness evidence.

## Enterprise alignment

The contracts contain company/database scope, finite envelope/fact/page/retry bounds, and an opaque page-token binding. These are useful Phase-A design primitives. The commit does not demonstrate production behavior at 300,000 users/customers/vendors, 10,000,000 items, 100,000 machines/projects, more than 1,000 employees, two-company isolation, or ten-year retention. The ten-million-row fake test is not capacity, query-plan, backpressure, cancellation, token-integrity, or memory evidence.

Accordingly:

- contract-level boundedness: **PARTIAL PASS**;
- measured enterprise scale: **EXTERNAL_PREREQUISITE**;
- durable indexes/partitions/retention: **PHASE_B_ITEM**;
- KMS/IAM/trust integration: **PHASE_C_ITEM**;
- signed least-privilege readers and paging behavior: **PHASE_D_ITEM**;
- production benchmark/DR/restore evidence: **EXTERNAL_PREREQUISITE**.

These later-phase items are not charged as missing Phase-A implementations. They remain explicitly unproven and cannot be used to claim production readiness.

## Required next action

Prepare one separate **report-only REV869B Option-A Phase-A failure reconciliation** covering F-01 through F-07. It must define an exact bounded correction authorization candidate, independent expectation ownership, and the decisive mutation evidence required for a later source correction. It must preserve:

- Phase B **NO_GO**;
- Correction 2 **NO_GO**;
- provisioning **NOT_STARTED**;
- PostgreSQL **NOT_AUTHORIZED_NOT_RUN**;
- production **NOT_READY**.

Do not automatically correct implementation or tests from this review. Do not provision, connect to PostgreSQL, execute future database scenarios, or advance to Phase B. After a separately authorized correction commit, run a fresh independent source-safety review of that exact commit.
