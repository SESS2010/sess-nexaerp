# REV869B Option-A Phase-A Correction A1 Failure Reconciliation

Date: 2026-08-16

Review mode: report-only, source-only reconciliation

Reconciled HEAD: `82e1d7052576f8715ff76ccecab13540eea47bff`

Reviewed source commit: `3858eea9a4e88c58447880f5c9c36e0dfe2420e9`

Independent-review report SHA-256: `9320CAD73798099548C8DB1ABA503870AAC2E11D852AA2AD0DCD28709A60A0AD`

## 1. Reconciliation verdict

`PHASE_A_CORRECTION_A2_SOURCE_ONLY_GATE=GO`

F02 through F07 are real and blocking, but none requires a new ownership, trust, persistence, deployment, or evidence-architecture decision. Each is a source composition, contract enforcement, assurance, or checkpoint-integrity deviation from decisions already fixed by the approved Option-A architecture. A2 may correct only the Phase-A contract/orchestration and offline assurance boundary defined here. It must not implement the Phase-B durable provider, access PostgreSQL, introduce deployment/provisioning, or claim production readiness.

This GO is a bounded recommendation for the next management authorization. It is not implementation authority by itself.

## 2. Stage-0 gate

| Gate | Reproduced result | Status |
|---|---|---|
| Exact HEAD | `82e1d7052576f8715ff76ccecab13540eea47bff` | PASS |
| Exact parent | `3858eea9a4e88c58447880f5c9c36e0dfe2420e9` | PASS |
| Review report path | `outputs/rev869b_external_controller_phase_a_correction_a1_independent_source_safety_review.md` exists | PASS |
| Review report SHA-256 | `9320CAD73798099548C8DB1ABA503870AAC2E11D852AA2AD0DCD28709A60A0AD` | PASS |
| Review commit boundary | Exactly one file, the independent-review report | PASS |
| Target-scoped status | No output from `git status --short -- .` | PASS |
| `../legacy-reference/` | Previously established untracked state retained; no command in this reconciliation opened, queried, enumerated, or modified it | PASS within prohibition |

The explicit no-access rule prevents a fresh filesystem or path query of `../legacy-reference/`. This reconciliation confirms untouched status from its own operation log and does not use sibling content as evidence.

## 3. Classification summary

| Finding | Severity | Primary ownership responsibility | Primary security boundary | Classification |
|---|---|---|---|---|
| F02 | CRITICAL | Control Plane, durable control-plane persistence, lifecycle controller, evidence readers | Trusted authority provenance and exactly-one ownership | `SOURCE_CORRECTION` |
| F03 | CRITICAL | Durable control-plane persistence and lifecycle controller | State/version, authorization, lease/fence, and atomic transition boundary | `SOURCE_CORRECTION` |
| F04 | CRITICAL | Acceptance Verifier and authoritative evidence readers | Evidence provenance, reader exactness, tenant binding, and oracle isolation | `SOURCE_CORRECTION` |
| F05 | HIGH | Deployment/readiness owner and immutable audit store | Fail-closed readiness, protected invocation, audit/privacy | `SOURCE_CORRECTION` |
| F06 | HIGH | Independent Assurance | Test-oracle independence and mutation decisiveness | `SOURCE_CORRECTION` |
| F07 | MEDIUM | Independent Assurance / checkpoint owner | Evidence integrity and reproducibility | `SOURCE_CORRECTION`; immutable A1-review formatting is `OUT_OF_SCOPE` |

No finding is an `ARCHITECTURE_DECISION`, `EXTERNAL_PREREQUISITE`, `PHASE_B_ITEM`, or external-infrastructure dependency for the limited Phase-A source proof. Actual durable storage, real KMS/readers/WORM, deployment identities, PostgreSQL execution, and operational performance evidence remain later authorized phases and must not be pulled into A2.

## 4. F02 reconciliation — ownership and trusted authority provenance

**Exact finding and severity:** CRITICAL. The 14-owner catalog exists, but executable composition does not prove exactly-one effective ownership. The raw control-plane authority accepts a separate lease authority and synthesizes trusted/current facts from request or operation data.

**Affected ownership and boundary:** Control Plane authority; durable control-plane persistence; lifecycle controller; authoritative evidence readers; trusted policy resolver. The affected boundary is untrusted caller intent versus server-owned authorization, policy, reader metadata, lease/fence, lifecycle state, export substate, attempt, and idempotency facts.

**Exact source locations:**

- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:640-650` — constructor accepts separate `ILeaseFenceAuthority` and `ILifecycleControllerAuthority`.
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:764-780` — separate lease read.
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:782-798` — request requirement IDs are converted into trusted descriptors and stage is hard-coded.
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:800-834` — active binding, caller expected state/version, operation-derived authorization state, fixed export substate, audit and attempt identifiers are assembled.
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:838-846` — current authorization state is derived from requested operation.
- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1404-1457` — separate facets and composite owner contracts coexist without enforcing composition identity.
- `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs:203-227` — catalog validation checks interface presence/separation, not constructor/runtime owner uniqueness.

**Required invariant:** Every trusted current fact used for a protected decision originates from the designated frozen owner. The control-plane authority must depend on exactly one composite `IDurableControlPlanePersistenceProvider` for current state, authorization state, export state, attempt/idempotency, lease/fence, and atomic outcome. Caller fields remain comparison claims only. Catalog owner, runtime dependency, provider version, and observed fact provenance must agree.

**Current behavior:** The authority performs good issuer, key, signature, audience, subject, role, scope, policy, nonce/freshness-input, and lease value checks, but then promotes request-derived or locally synthesized values into `VerifiedLifecycleCommandV3`.

**Root cause:** A1 closed public typed ingress but adapted raw input into an already-trusted command DTO before acquiring one authoritative composite snapshot. Interface inheritance describes ownership without making runtime composition enforce it.

**Exploit/failure path:** A correctly signed caller chooses an expected lifecycle state/version and operation combination. The authority maps those claims into current state and authorization/export facts. A test-double or incorrectly composed lifecycle controller can accept a transition without proving the durable record, original grant, provider identity, or composite lease state.

**Operational impact:** Stale-state execution, cross-attempt confusion, authorization replay, incorrect export sequencing, split-brain lease acceptance, and audit/state divergence could be certified as valid at the Phase-A boundary.

**Smallest safe correction:**

1. Change the raw authority composition to consume the single composite durable-provider contract, not separately injectable state/lease/idempotency authorities.
2. Extend the Phase-A composite contract with one authoritative read/transaction input that returns the exact lifecycle, resource version, authorization/grant, export, attempt, lease/fence, and provider-version snapshot required by the frozen rules.
3. Compare header/payload expectations with that snapshot; never copy them into trusted current fields.
4. Keep actual persistence implementation out of A2. An adversarial offline provider proves the contract and call order.
5. Strengthen ownership validation to inspect the exported constructor/dependency graph and reject duplicate effective owners.

**Required positive tests:**

- `A2_All14ResponsibilitiesHaveOneEffectiveOwnerAndOneCatalogOwner`
- `A2_RawAuthorityReadsOneCompositeSnapshotBeforeLifecycleDecision`
- `A2_TrustedStateGrantPolicyLeaseReaderAndAttemptFactsMatchCompositeProvider`
- `A2_CompositeProviderVersionAndIdentityBindTheTransaction`

**Required negative/adversarial tests:**

- `A2_SeparateLeaseOrStateOwnerCompositionIsRejected`
- `A2_CallerStateVersionGrantExportAttemptAndEpochCannotBecomeTrustedFacts`
- `A2_DuplicateProviderIdentityOrVersionMismatchFailsClosed`
- `A2_CrossResourceCrossOrganizationAndCrossAttemptSnapshotIsRejectedWithoutMutation`

**Acceptance formula and evidence:**

```text
catalog_owner_count(each responsibility) = 1
AND effective_owner_count(each responsibility) = 1
AND control_authority_composite_provider_count = 1
AND separate_state_lease_idempotency_owner_count = 0
AND trusted_current_fields_from_composite_snapshot = ALL
AND trusted_current_fields_from_request_or_operation = 0
AND provider_identity_version_mismatches_accepted = 0
```

Evidence must include exported-constructor inventory, a provider-call trace, raw authority positive/negative vectors, zero lifecycle calls on provenance failure, and zero state changes on every denied case.

**Classification:** `SOURCE_CORRECTION`.

## 5. F03 reconciliation — lifecycle, authorization, lease/fence, and atomic transitions

**Exact finding and severity:** CRITICAL. The 26-row rule table is structurally present, but the raw production composition cannot enforce it from authoritative current state. Export and reauthorization paths are unreachable or inconsistent because export state is fixed to `NONE` and authorization state is derived from the requested operation.

**Affected ownership and boundary:** Lifecycle controller and durable control-plane persistence. The boundary covers current versus requested state, optimistic version, authorization grant consumption, lease/fence/epoch, attempt ownership, idempotency, transition result, and audit correlation.

**Exact source locations:**

- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:801-835` — constructs current state/version and substates before controller delegation.
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:832-846` — operation-derived authorization and fixed export substate.
- `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs:222-280` — rule enforcement trusts the supplied `VerifiedLifecycleCommandV3` current facts.
- `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs:249-272` — authorization/evidence checks occur after the authority has promoted request-derived facts.
- `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs:306` — replacement helper remains a non-atomic pure construction surface.
- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1047-1110` — trusted command and transaction DTOs do not distinguish caller expectations from provider observations strongly enough.

**Required invariant:** A protected transition is permitted only when one provider-owned snapshot matches one literal frozen rule and the caller's expected state/version; required grant, original authorizer, evidence, lease/fence/epoch, export substate, and attempt all match; and one composite transaction commits nonce, idempotency, transition, grant consumption, attempt, and audit outcome. Every unlisted combination denies without state change.

**Current behavior:** Direct state-machine calls enforce selected rule predicates, but the raw authority supplies the decisive current fields. `EXPORT`/`COMPLETE_EXPORT`, reauthorization, cancel/expire, quarantine-with-held-lease, and stale-version cases therefore lack a reliable production-path proof.

**Root cause:** The pure rules table was treated as the authority boundary. The missing boundary is acquisition and atomic use of server-owned current facts.

**Exploit/failure path:** A signed request declares a convenient expected state/version or operation. Static mapping selects a compatible authorization state, omits the actual export state, or treats no-lease as sufficient. The state machine validates internally consistent constructed data rather than the durable resource.

**Operational impact:** Illegal state transitions, duplicate or out-of-order export, grant reuse, lost updates, stale fencing, wrong-actor cancellation, premature expiry, and non-atomic audit/idempotency outcomes.

**Smallest safe correction:** Separate `UntrustedLifecycleExpectationV3` from `AuthoritativeLifecycleSnapshotV3`; make rule evaluation require both; have the composite provider own the snapshot and atomic transaction boundary; require lease conditionally from provider state; preserve the exact 26 frozen rows and strict complement. Do not implement a database provider in A2.

**Required positive tests:**

- `A2_All26FrozenRowsExecuteThroughRawAuthorityWithAuthoritativeSnapshot`
- `A2_ExportAuthorizeDeliverCompleteAndReauthorizeSequencesAreReachableInOrder`
- `A2_CancelExpireAndQuarantineUseExistingGrantActorTimeAndHeldLease`
- `A2_AtomicProviderCommitsNonceIdempotencyTransitionGrantAttemptAndAuditOnce`
- `A2_ConcurrentIdenticalRequestReturnsOneCommittedOutcome`

**Required negative/adversarial tests:**

- `A2_CompleteLiteralComplementOf26RowsIsIllegal`
- `A2_EveryRowRejectsWrongStateVersionRoleScopeGrantEvidenceLeaseFenceEpochAttemptAndAudit`
- `A2_ExportSkipReplayAndWrongReleaseAreRejected`
- `A2_WrongOriginalAuthorizerOrEarlyExpiryIsRejected`
- `A2_StaleAndCrossResourceLeaseNeverChangesState`
- `A2_IdempotencyDigestCollisionConcurrentOwnerAndNonretryableReplayFailClosed`

**Acceptance formula and evidence:**

```text
frozen_rows = 26
AND exact_raw_path_rows_accepted = 26
AND unlisted_pairs_accepted = 0
AND caller_current_facts_used = 0
AND denied_mutations_with_state_change = 0
AND export_skip_paths = 0
AND stale_version_or_fence_successes = 0
AND successful_transactions_with_atomic_component_count != 1 = 0
```

Evidence must be based on a literal assurance-owned 26-row oracle, raw canonical ingress, an instrumented adversarial composite provider, exact call/commit traces, and replay/concurrency barriers that exercise the production authority rather than a test-only controller.

**Classification:** `SOURCE_CORRECTION`.

## 6. F04 reconciliation — authoritative readers and oracle isolation

**Exact finding and severity:** CRITICAL. Signed bundle validation is substantial, but the verifier accepts caller-carried bundles without invoking the authoritative reader operation, permits duplicate reader IDs when observation IDs differ, does not apply all configured limits, and lacks an independently trusted expected verification binding.

**Affected ownership and boundary:** Acceptance Verifier, authoritative evidence readers, trusted issuer/key registry, oracle registry, and immutable audit provider. The boundary is caller envelope versus reader-owned facts and server-owned expected operation/tenant/resource/attempt/stage.

**Exact source locations:**

- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:462-470` — verifier dependencies.
- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:479-518` — raw parsing and canonical validation.
- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:530-534` — set equality collapses duplicate reader IDs.
- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:541-604` — descriptor/signature checks do not call `readerRegistry.ReadAsync`.
- `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs:29-32` — configured observation/string bounds.
- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1469-1478` — provider already defines `ResolveAsync` and `ReadAsync`.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:1140` — fake `ReadAsync` throws, proving it is not used by the tested V3 path.

**Required invariant:** The verifier creates or obtains a server-owned expected binding; resolves each required reader exactly once; invokes each authoritative reader for that binding; accepts only the returned canonical signed bundle; verifies descriptor, artifact, schema, stage, issuer/key/algorithm, signature, digest, tenant/database/resource/version/attempt, time, snapshot/watermark, source type, fields, counts, and bytes; and gives the oracle only minimized verified facts. Caller expected values, verdicts, receipts, or facts cannot enter oracle input.

**Current behavior:** Reader descriptor, key, signature, fact digest, freshness, stage, allowed organization/cluster/instance/resource/fields, and cumulative bytes are checked. But request transport is still the source of the complete bundle, exact-one reader cardinality is absent, and server option maxima are partially bypassed.

**Root cause:** A1 treated a signed caller-carried observation as equivalent to a reader-owned observation call and used set equality where a multiset/exact cardinality invariant was required.

**Exploit/failure path:** A caller replays a recent valid bundle for a self-selected allowed binding, supplies two bundles for one required reader with distinct observation IDs, or exceeds a stricter configured observation/string limit while remaining under global limits. The oracle evaluates inputs not tied to an independently trusted expected request.

**Operational impact:** Cross-request or cross-attempt evidence substitution, duplicate weighting, tenant/resource confusion, resource exhaustion beyond configured policy, and an acceptance verdict over facts the verifier did not authoritatively request.

**Smallest safe correction:** Add a server-owned expected verification request/binding provider contract or require the existing reader provider to generate all bundles from that binding; enforce exact-one required reader by count and grouping; call `ReadAsync`; compare the returned bundle against the expected binding; apply the minimum of descriptor, server option, and global limits; preserve raw ingress and signed-bundle verification.

**Required positive tests:**

- `A2_VerifierInvokesEveryRequiredReaderExactlyOnceWithServerOwnedBinding`
- `A2_ExactSignedReaderBundlesProducePinnedOracleVerdictAndAuditReceipt`
- `A2_StricterConfiguredObservationStringFactAndByteLimitsAreApplied`
- `A2_TwoOrganizationsAndInstancesRemainCryptographicallyIsolated`

**Required negative/adversarial tests:**

- `A2_DuplicateMissingUnknownOrExtraReaderFailsBeforeOracle`
- `A2_CallerCarriedBundleCannotReplaceReaderReturnedBundle`
- `A2_CrossOrganizationResourceVersionAttemptStageOrWatermarkFailsBeforeOracle`
- `A2_TamperedArtifactSchemaKeyAlgorithmSignatureDigestOrFactFailsBeforeOracle`
- `A2_FutureStaleOversizedSensitiveOrUnlistedFactFailsBeforeOracle`
- `A2_ReaderFailureTimeoutOrAmbiguousBindingReturnsNoVerdictAndNoSuccessAudit`

**Acceptance formula and evidence:**

```text
actual_reader_ids multiset = required_reader_ids multiset
AND count(each required reader) = 1
AND ReadAsync_calls = required_reader_count
AND caller_supplied_authoritative_bundle_count = 0
AND all_scope_temporal_operation_fields = trusted_expected_binding
AND applied_limit(each dimension) = min(global, server_option, descriptor)
AND oracle_inputs = minimized_verified_reader_facts_only
AND denied_cases_with_oracle_calls_or_verdicts = 0
```

Evidence must contain reader call traces, canonical fixture hashes, independent crypto vectors, tenant/organization adversarial pairs, exact error codes, zero-oracle-call assertions, and append-before-verdict audit ordering.

**Classification:** `SOURCE_CORRECTION`.

## 7. F05 reconciliation — readiness, audit failure, and privacy

**Exact finding and severity:** HIGH. Readiness can report executable when a `READY` dependency has no expiry or mismatched identity; the snapshot predicate checks only count, set, and state. Route-level coverage is incomplete. Audit privacy/failure behavior is partially correct but not proven across lifecycle and verifier paths.

**Affected ownership and boundary:** Deployment/readiness owner and immutable audit evidence provider. The boundary is health evidence versus permission to invoke protected logic, and protected result versus durable sanitized audit receipt.

**Exact source locations:**

- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:75-98` — expiry is checked only when present; identity fields are not compared.
- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1249-1286` — nullable timestamps/identity and weak `CanExecuteProtectedOperation` predicate.
- `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs:9-17` — control-plane readiness route.
- `src/SESS.NexaERP.AcceptanceVerifier/Program.cs:19-27` — verifier readiness route.
- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:489-492` — verifier depends on the weak snapshot predicate.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:410-467` — named matrix covers only a subset and does not invoke both routes.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:468` onward — audit failure coverage is narrower than the claimed exactness.

**Required invariant:** `READY` is true only for an exact, unique dependency set where every item has the expected policy, required/observed version and identity match, non-null bounded freshness interval, safe state, and sanitized diagnostic. Timeout, exception, missing, duplicate, stale, future, degraded, mismatch, malformed-ready, or policy disagreement returns non-ready; both hosts return HTTP 503 and invoke no protected handler. No protected success/verdict is returned until exact typed audit append succeeds, and no secret/private/raw evidence enters audit or diagnostic output.

**Current behavior:** Exceptions and internal timeouts are normalized, staleness is partially detected, and both ready routes choose 503 when `CanExecuteProtectedOperation` is false. A malformed `READY` object can still make that predicate true. Verifier audit append is fail-closed, but lifecycle atomic audit proof and full privacy mutations are absent.

**Root cause:** Readiness DTO defaults were designed for convenience, while the predicate was implemented as enum coverage plus state equality rather than the frozen conjunctive policy. Tests call the authority directly instead of the deployed route mapping.

**Exploit/failure path:** A probe returns `READY` with null expiry, empty or mismatched identity, or inconsistent policy metadata. The snapshot reports executable and the verifier proceeds. Separately, an incomplete audit test can miss a lifecycle success returned before a durable receipt or a sensitive field copied into diagnostic/audit data.

**Operational impact:** Protected work under stale/wrong dependencies, inconsistent host behavior, unverifiable state transitions, privacy leakage, and false readiness during dependency or identity drift.

**Smallest safe correction:** Make readiness timestamps and identity mandatory for `READY`; validate policy/version/identity/freshness/cardinality in the authority and snapshot; centralize one fail-closed HTTP mapping used by both hosts; require exact audit receipt before success; constrain audit/diagnostic fields to typed allowlists and digests.

**Required positive tests:**

- `A2_ExactFreshIdentityVersionPolicyDependencySetIsReady`
- `A2_ControlPlaneAndVerifierReadyRoutesReturn200ForExactSnapshot`
- `A2_ExactSanitizedAuditAppendPrecedesTransitionOrVerdictSuccess`

**Required negative/adversarial tests:**

- `A2_MissingDuplicateTimeoutExceptionDegradedAndUnsafeDependencyReturn503OnBothRoutes`
- `A2_NullExpiredFutureOrInvertedFreshnessReturns503OnBothRoutes`
- `A2_IdentityVersionPolicyOrDependencyMismatchReturns503OnBothRoutes`
- `A2_AuditThrowTimeoutWrongReceiptOrChainMismatchReturnsNoProtectedSuccess`
- `A2_SecretsCredentialsKeysSignaturesRawFactsAndPrivateFieldsNeverAppearInAuditOrDiagnostics`

**Acceptance formula and evidence:**

```text
READY iff exact_dependency_set
  AND unique_dependencies
  AND all_policy_version_match
  AND all_required_observed_version_match
  AND all_required_observed_identity_match
  AND all_CheckedAt_ValidUntil_present_ordered_fresh
  AND all_states_READY
AND every_nonready_case_http_status(control, verifier) = 503
AND protected_handler_calls_on_nonready = 0
AND protected_success implies exact_sanitized_audit_receipt
```

Evidence must include route-level status/body/handler counters for both hosts, a complete dependency mutation matrix, audit call ordering, exact receipt comparisons, and prohibited-field scans.

**Classification:** `SOURCE_CORRECTION`.

## 8. F06 reconciliation — independent tests and decisive mutants

**Exact finding and severity:** HIGH. The 27 A1 tests pass but several names overstate their coverage, expected values are partly derived from production, concurrency/idempotency is implemented in test-only controllers, and the four claimed mutants are invalid inputs rather than mutations of production gates.

**Affected ownership and boundary:** Independent Assurance. The boundary is implementation under review versus the independent acceptance oracle.

**Exact source locations:**

- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:65` — “every” canonical mutation test uses a narrow sample.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:129` — trusted provenance test mutates only a subset.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:171` — lifecycle mutation test uses one row and three fields.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:410` — readiness matrix omits required cases/routes.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:468` — audit exactness is narrow.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:544-585` — four “mutants” are hand-crafted invalid inputs, not production source mutations.
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:858-941` — concurrency behavior resides in test-only controllers.

**Required invariant:** Expected owner inventory, 26 lifecycle rows, error codes, crypto vectors, readiness matrix, audit schema, and mutation expectations are literal assurance-owned fixtures independent of production enumerations/mappings. Tests traverse raw production authorities and instrumented provider boundaries. Four actual production gate-removal/inversion variants must compile, run the targeted suite, and be killed for the intended assertion.

**Current behavior:** Passing tests prove selected local behavior but not necessity of the decisive production gates or correct end-to-end composition.

**Root cause:** A1 optimized for named test counts and used production tables/test doubles as both implementation and oracle.

**Exploit/failure path:** A developer removes or inverts a trust, lifecycle, reader, or readiness gate while the derived expectation changes with production or the test-only controller continues to enforce behavior absent from the product.

**Operational impact:** False PASS evidence, repeated review failure, regression escape, and premature management authorization.

**Smallest safe correction:** Keep the single authorized test file, replace derived expectations with literal frozen fixtures, run every case through the raw production composition, and run four reproducible source mutants in temporary copies/worktrees without changing the reviewed tree.

**Required positive tests:** All exact A2 tests named in F02–F05 plus:

- `A2_PublicProtectedSurfaceRemainsRawOnly`
- `A2_Literal14OwnerInventoryMatchesEffectiveConstructorGraph`
- `A2_Literal26RowOracleMatchesProductionWithoutProductionMappingHelpers`
- `A2_OfflineCryptoVectorsAndCanonicalBytesMatchPinnedHashes`
- `A2_ProductionAuthorityConcurrencyAndReplayTraceMatchesLiteralOracle`

**Required negative/adversarial tests:** Complete canonical JSON/header mutation corpus; complete 26-row complement; every trusted binding field mutation; reader, crypto, tenant, freshness, limits, oracle, audit, readiness, replay, collision, timeout, cancellation, and concurrency cases listed in F02–F05; and a test that fails if any expectation is loaded from a production rule/catalog mapping.

**Four decisive production mutants:**

| Mutant ID | Exact mutation | Required killer |
|---|---|---|
| `A2-M01-REQUEST-AS-AUTHORITY` | Replace composite current-state/version/auth/export/lease facts with header/payload/operation-derived values in `PhaseAControlPlaneAuthority` | F02 provenance and cross-state/cross-version tests |
| `A2-M02-LIFECYCLE-GATE-BYPASS` | Invert one decisive state/version/lease-fence/grant conjunction in `RequirePhaseACommand` so an unlisted or stale transition would pass | Literal 26-row complement and every-binding mutation tests |
| `A2-M03-READER-CLOSURE-BYPASS` | Skip `ReadAsync` or replace exact-one reader cardinality with set equality over caller bundles | Duplicate/caller-substitution/reader-call-trace tests |
| `A2-M04-READINESS-AUDIT-BYPASS` | Permit `READY` with missing expiry/identity or return success before required audit receipt | Both-route malformed-ready and audit-failure ordering tests |

Each mutant must be an actual temporary source patch, build successfully, run the targeted tests, fail for its named reason, then be discarded. A mutant that does not compile is invalid; an unrelated failure is not a kill.

**Acceptance formula and evidence:**

```text
production_derived_expected_fixtures = 0
AND required_positive_cases_pass = ALL
AND required_negative_cases_pass = ALL
AND raw_production_authority_paths_covered = ALL
AND decisive_mutants_total = 4
AND decisive_mutants_compiled = 4
AND decisive_mutants_killed_by_intended_assertion = 4
AND decisive_mutants_survived = 0
```

Evidence must identify mutant patch hash, changed production line, build result, targeted test, intended failing assertion, exit code, and cleanup confirmation.

**Classification:** `SOURCE_CORRECTION`.

## 9. F07 reconciliation — checkpoint and immutable report integrity

**Exact finding and severity:** MEDIUM. The A1 checkpoint stores invalid command text at lines 156–159. The independent-review report also contains an incorrect aggregate test total and Markdown trailing-whitespace warnings.

**Affected ownership and boundary:** Independent Assurance and checkpoint owner. The boundary is executed validation evidence versus the immutable record presented to management.

**Exact locations:**

- `outputs/rev869b_external_controller_phase_a_checkpoint.md:156-159` — literal tab after `.` and missing path separators in four commands.
- `outputs/rev869b_external_controller_phase_a_correction_a1_independent_source_safety_review.md:258` — says 556 although the listed invocation arithmetic is 583.
- The independent report's lines 3–6 and 291–295 contain Markdown hard-break trailing spaces.

**Required invariant:** Historical reports/commits remain immutable. A new/future checkpoint must distinguish raw invocation events from unique tests, reproduce exact executable command bytes, record exit code and output, pass final exact-range `git diff --check`, and be generated only after the reviewed commit is fixed.

**Current behavior:** The validation table lists correct per-invocation results, but its aggregate arithmetic is wrong. The checkpoint's stored commands are not executable as printed. Formatting warnings are cosmetic but separately real.

**Root cause:** Manual transcription and aggregation occurred after validation, with no machine check of literal report bytes and arithmetic.

**Exploit/failure path:** A reviewer copies an invalid command or relies on an inflated/deflated aggregate, making evidence irreproducible even when underlying test invocations passed.

**Operational impact:** Approval-chain ambiguity and wasted review cycles. This does not create a product execution vulnerability, so it remains separate from F02–F05 source-safety failures.

**Smallest safe correction:** Preserve commit `82e1d7052576f8715ff76ccecab13540eea47bff` and the A1 independent report unchanged. During authorized A2, update only the existing checkpoint path to disclose the immutable defects and record exact A2 machine-captured commands/results. Do not amend, reset, rebase, or rewrite history.

**Required positive tests/evidence:**

- Machine-add the reported invocation counts and unique-test inventory.
- Parse every stored dotnet/Git command from the checkpoint and compare it byte-for-byte with the executed command capture.
- Run incremental and cumulative `git diff --check` after the final checkpoint bytes exist.
- Prove the old report blob/hash is unchanged.

**Required negative/adversarial tests:** Introduce a temporary bad arithmetic total, tab/path corruption, trailing whitespace, conflict marker, omitted exit code, or modified historical-report blob and prove the evidence gate rejects it.

**Acceptance formula and evidence:**

```text
A1_raw_pass_events = 27 + 27 + 76 + 450 + 3 = 583
AND authoritative_unique_non_postgresql_tests = 450
AND A1_unique_phase_a_tests = 27
AND A1_unique_focused_REV869B_non_postgresql_tests = 76
AND postgresql_tests_executed = 0
AND stored_commands = executed_commands byte-for-byte
AND incremental_diff_check_exit = 0 with no output
AND cumulative_diff_check_exit = 0 with no output
AND old_review_sha256 = 9320CAD73798099548C8DB1ABA503870AAC2E11D852AA2AD0DCD28709A60A0AD
```

**Classification:** `SOURCE_CORRECTION` for new A2 checkpoint evidence. Editing the immutable A1 independent-review report or rewriting its commit is `OUT_OF_SCOPE`.

## 10. Authoritative reconciled test count

Two different totals must not be conflated:

| Counting basis | Authoritative total | Explanation |
|---|---:|---|
| Raw passed test events across the five reported invocations | **583** | Arithmetic is `27 + 27 + 76 + 450 + 3`; this counts reruns and overlapping filters each time they executed |
| Unique non-PostgreSQL tests | **450** | The complete non-PostgreSQL invocation is the superset; the 27 A1, 76 focused REV869B, and 3 parity/hash cases are subsets or reruns and are not added again |
| Unique A1 Phase-A tests | **27** | The filtered A1 suite and complete Phase-A project invocation selected the same 27 tests |
| Unique focused REV869B non-PostgreSQL tests | **76** | Subset of the complete 450-test non-PostgreSQL suite |
| PostgreSQL tests | 34 discovered; **0 executed** | Discovery only, as authorized |

Therefore `556` is not authoritative. `583` is the invocation-event sum, while `450` is the correct non-double-counted unique executed-test count.

## 11. Exhaustive minimal A2 file allowlist

Maximum file count: **10**. The list is exhaustive. No related, generated, project, solution, migration, helper, configuration, new report, or additional test file is implicit.

| # | Exact file | Finding mapping | Only authorized purpose |
|---:|---|---|---|
| 1 | `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs` | F02, F03, F04, F05 | Separate caller expectations from authoritative snapshots; exact composite transaction, reader binding, readiness, audit/privacy contracts |
| 2 | `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs` | F02, F03 | Enforce effective owner/dependency uniqueness and exact authoritative binding; no persistence implementation |
| 3 | `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs` | F03, F06 | Evaluate literal 26-row rules from authoritative snapshot plus caller expectation; strict complement |
| 4 | `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs` | F02, F03, F06 | Make raw authority use the one composite owner and remove all request/operation-derived trusted current facts |
| 5 | `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs` | F04, F05 | Complete validated expected binding, identity, freshness, and finite server bounds |
| 6 | `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs` | F04, F05, F06 | Invoke exact authoritative readers, enforce exact-one/bounds/binding/crypto/oracle/audit closure |
| 7 | `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs` | F05, F06 | Use the common fail-closed readiness-to-HTTP mapping; no protected mutation route |
| 8 | `src/SESS.NexaERP.AcceptanceVerifier/Program.cs` | F05, F06 | Use the same readiness-to-HTTP mapping and preserve health/version-only host surface |
| 9 | `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs` | F02, F03, F04, F05, F06, F07 | Literal independent fixtures, raw production paths, full adversarial matrices, and mutant killers |
| 10 | `outputs/rev869b_external_controller_phase_a_checkpoint.md` | F07 and all | Record exact A2 evidence, totals, mutants, hashes, commands, exclusions, and retained states; disclose immutable A1 report defects |

Any need to change a file outside this list stops A2 and requires a new report-only reconciliation. The existing independent-review report is explicitly immutable and not allowlisted.

## 12. Exact A2 validation and evidence gate

After management authorization and implementation, the correction must independently run, offline and without restore/download:

1. Exact HEAD/parent, changed-file subset, one-correction-commit, and clean target-scope checks.
2. Build the Phase-A test project and complete solution with `--no-restore` and warnings as errors.
3. Run the complete A2 Phase-A test project.
4. Run focused REV869B non-PostgreSQL tests.
5. Run the complete non-PostgreSQL suite.
6. List PostgreSQL tests only; executed count must remain zero.
7. Parse all PowerShell scripts with Windows PowerShell 5.1 AST without executing them.
8. Run EF migration discovery with `--no-connect` and an inert loopback endpoint; no database connection.
9. Verify migration uniqueness/order, model/snapshot parity, and offline SQL/hash fixtures without modifying migrations.
10. Run incremental and cumulative `git diff --check` after final checkpoint bytes exist.
11. Run security, secret, privacy, network/process, database-action, protected-endpoint, and prohibited-operation scans over incremental and cumulative added lines.
12. Run all four actual production mutants and capture patch hash, compile, intended kill, exit, and cleanup evidence.

Required formulas:

```text
changed_files subset_of exact_10_file_allowlist
AND changed_file_count <= 10
AND source_test_project_migration_helper_changes_outside_allowlist = 0
AND builds_failed = 0
AND warnings = 0
AND required_tests_failed_or_skipped = 0
AND postgresql_tests_executed = 0
AND decisive_mutants = 4 killed / 0 survived / 0 invalid
AND prohibited_scan_actionable_hits = 0
AND incremental_diff_check = exit 0 / no output
AND cumulative_diff_check = exit 0 / no output
AND final_target_status = clean
```

## 13. Immediate steps, risks, and acceleration guidance

### Immediate technical and functional sequence

1. Obtain management authorization for exactly this 10-file A2 boundary.
2. Freeze literal assurance fixtures first: 14 owners, 26 rows, trusted-field provenance, reader binding, readiness matrix, audit schema, and four mutant patches.
3. Correct the composite owner and caller-versus-authoritative DTO boundary before changing lifecycle or verifier behavior.
4. Correct lifecycle composition, then authoritative reader acquisition/oracle isolation, then readiness/audit behavior.
5. Run targeted tests after each boundary, followed by the full authorized offline gate.
6. Generate the checkpoint last from captured command results and commit one A2 correction commit.
7. Stop for a fresh independent source-only review. Do not proceed to management acceptance or Phase B automatically.

### Architecture and process risks

- Accidentally implementing an in-memory or database “temporary” durable provider in A2 would cross into Phase B and create a second authority.
- Treating signed caller-carried evidence as reader-owned evidence preserves the F04 oracle-substitution risk.
- Letting tests derive expectations from production recreates F06 even if coverage counts rise.
- Combining filtered-run totals with the complete suite inflates evidence; report both 583 raw events and 450 unique tests.
- Updating the immutable A1 report, amending history, or creating extra checkpoint/report artifacts breaks evidence custody.
- Broadening constructors or adding convenience typed overloads can recreate the F01 bypass.
- Parallel edits to contracts and tests without a frozen literal oracle can produce self-consistent but architecturally wrong results.

### Acceleration without efficiency loss

- Build the literal fixtures and provider call-trace harness first; they become stable acceptance inputs for every subsequent change.
- Keep one composite fake provider with deterministic barriers and event traces instead of multiple behavioral test controllers.
- Reuse one canonical mutation generator across command and evidence envelopes, with bounded case tables to avoid redundant test startup cost.
- Run fast targeted F02–F05 tests during development, then one complete 450-test non-PostgreSQL gate at the end; do not sum overlapping filters as unique coverage.
- Cache build artifacts only within the existing restored dependency graph; retain final `--no-restore` verification.
- Generate checkpoint tables and arithmetic directly from machine-readable test/mutant results to eliminate transcription defects.

## 14. Retained states and exact next gate

```text
phase_a_management_acceptance_state=FAIL
phase_b_state=NO_GO
correction_2_state=NO_GO
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
external_provisioning_state=NOT_STARTED
production_readiness_state=NOT_READY
```

The label A2 means the second bounded correction attempt inside Phase A; it does not authorize the separately retained `correction_2_state`, Phase B, persistence, deployment, or external work.

**Exact single next management gate:** management review and explicit authorization (or rejection) of one `REV869B Option-A Phase-A Correction A2` source-only correction using exactly the 10-file allowlist, tests, four mutants, validation formulas, exclusions, one-correction-commit rule, and mandatory fresh independent review defined in this report.
