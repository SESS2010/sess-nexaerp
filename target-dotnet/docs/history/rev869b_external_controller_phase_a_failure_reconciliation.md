# REV869B Option-A Phase-A Independent-Review Failure Reconciliation

Date: 2026-08-16

Task type: report-only reconciliation; no implementation authority

Entry HEAD: `ba37f6ac746bfd6eccaae571a0676f4b1f28b9ee`

Reviewed Phase-A source commit: `18a6458cbddf50e8cd45c9f789be2bdd2e859b08`

Phase-A source parent: `51476760adcea9ed7babbc04d642e53e371c6941`

Independent review: `outputs/rev869b_external_controller_phase_a_independent_source_safety_review.md`

## Decision

**PHASE_A_CORRECTION_A1_GO**

All seven independent-review findings are implementation/assurance deviations from the still-valid frozen Option-A architecture. They can be resolved in one closed, source-only Phase-A Correction A1 using the exact 13-file allowlist in this report. None requires durable persistence, KMS infrastructure, PostgreSQL behavior, provisioning, deployment, production credentials, or Phase-B implementation to validate its Phase-A contract and offline acceptance boundary.

This is authority to present Correction A1 to management for approval. It is not source-correction authority. Correction A1 must not start until management separately approves the exact allowlist, tests, formulas, exclusions, and commit gate below.

## Canonical states

| State | Value |
|---|---|
| `phase_a_failure_reconciliation_state` | **PASS** |
| `phase_a_correction_a1_source_only_gate` | **GO** |
| `architecture_reopen_required` | **NO** |
| `phase_b_dependency_blocking_state` | **NO** |
| `phase_a_management_acceptance_state` | **NOT_APPROVED** |
| `phase_b_source_only_gate` | **NO_GO** |
| `phase1_correction2_source_only_gate` | **NO_GO** |
| `rev869b_source_safety_state` | **FAIL** |
| `rev869b_execution_helper_readiness_state` | **FAIL** |
| `postgresql_execution_state` | **NOT_AUTHORIZED_NOT_RUN** |
| `production_readiness_state` | **NOT_READY** |

## Entry-gate evidence

| Gate | Evidence | Result |
|---|---|---|
| Expected HEAD | `git rev-parse HEAD` returned `ba37f6ac746bfd6eccaae571a0676f4b1f28b9ee` | PASS |
| HEAD parent | `git rev-parse HEAD^` returned `18a6458cbddf50e8cd45c9f789be2bdd2e859b08` | PASS |
| Review commit boundary | HEAD adds only `outputs/rev869b_external_controller_phase_a_independent_source_safety_review.md` | PASS |
| Target-scoped status | `git status --short -- .` returned no entries | PASS |
| Independent verdict | Exact text `FAIL — CORRECTION_REQUIRED` | PASS |
| Finding count | Exactly 7 | PASS |
| Severity count | Exactly 5 BLOCKING and 2 REQUIRED | PASS |
| Ownership coverage | Exactly 14 responsibility rows | PASS |
| Lifecycle coverage | Exactly 26 conceptual transitions | PASS |
| Test coverage | Exactly 46 test-quality rows | PASS |
| Diff-check contradiction | Independent report records FAIL; exact reproduction exits 2 | PASS |
| Out-of-scope sibling | No command in this reconciliation enumerated or read `../legacy-reference/` | PASS |

No gate failed. The reconciliation therefore proceeded. No PostgreSQL connection/test, migration operation, provisioning, deployment, key use, lifecycle action, helper, or external call occurred.

## Architecture and phase classification

| Question | Determination |
|---|---|
| Is frozen Option A internally contradictory? | **NO.** It already requires raw ingress, server-owned policy, one owner per responsibility, a complete lifecycle table, signed authoritative facts, conjunctive readiness, and independent expectations. |
| Do the findings invalidate ownership separation? | **NO.** They show reviewed code did not close the approved separation. |
| Can contracts be corrected without durable storage? | **YES.** Exact interfaces, DTOs, validators, state tables, API surfaces, fake-provider fault contracts, and mutation tests are Phase A. |
| Is Phase B required for A1 PASS? | **NO.** Database atomicity/concurrency remains unclaimed and cannot be tested as production behavior in A1. |
| Is external infrastructure required for A1 PASS? | **NO.** KMS/IAM/readers/audit adapters remain later phases; A1 validates their closed interfaces with deterministic offline fixtures. |
| Does A1 make the product production-ready? | **NO.** It only restores the Phase-A source contract gate. |

## Seven findings extracted verbatim

The independent report's seven finding headings are reproduced verbatim:

1. `F-01 — public typed ingress bypasses remain`
2. `F-02 — ownership and policy authority are not closed`
3. `F-03 — lifecycle table is incomplete`
4. `F-04 — verifier facts are not cryptographically closed`
5. `F-05 — audit and readiness contracts are incomplete`
6. `F-06 — tests pass but are not independently decisive`
7. `F-07 — checkpoint validation discrepancy`

## F-01 reconciliation — public typed ingress bypasses remain

| Required field | Reconciliation |
|---|---|
| Finding ID / severity | F-01 / BLOCKING |
| Independent-review evidence | Public `SignedEnvelopeService.Sign(LifecycleCommandV1)`, `SignedEnvelopeVerificationService.Verify(SignedCommandEnvelopeV1, ...)`, `ClosedEvidenceVerifierV1`, and typed `ClosedEvidenceVerifierV2.VerifyAsync(EvidenceVerificationRequestV2)` survive although protected V1 is declared unsupported. The raw-surface test inspects only `SignedCommandServiceV2`. |
| Exact affected production path | Command bypass: caller → `SignedEnvelopeVerificationService.Verify(typed V1)`; authoritative candidate: caller raw header/payload/signature → `SignedCommandServiceV2.VerifyAsync` → strict codecs → controller. Evidence bypass: caller → `ClosedEvidenceVerifierV2.VerifyAsync(typed request)`; required path: caller raw canonical envelope/signatures + transport identity → strict evidence codec → verifier authority. No protected HTTP endpoint currently exists. |
| Root cause | Compatibility code remained public and tests treated one concrete class as the whole public boundary. Evidence parsing was implemented as an optional helper instead of the mandatory ingress. |
| Why tests passed | V1 tests exercised the forbidden typed path; reflection inspected only `SignedCommandServiceV2`; malformed-evidence tests invoked `StrictEvidenceJsonV2` separately from verifier execution. |
| Consequence | A caller can bypass byte-exact canonical parsing, unknown/duplicate/case/order checks, and the sole controller/verifier authority boundary. Different parsers can authorize different meanings. |
| Architecture still valid | YES — the frozen architecture already mandates sole raw canonical ingress. |
| Phase A / Phase B / external | Phase A: YES. Phase B: NO. External prerequisite: NO. |
| Exact source behavior required | Protected V1 compatibility surfaces become non-public and cannot be resolved as authority. `IControlPlaneAuthority` exposes only raw header/payload/signature plus authenticated transport identity. `IAcceptanceVerifierAuthority` exposes only raw canonical evidence/bundle/signature input plus authenticated transport identity. Each invokes exactly one strict codec internally. No overload accepts a parsed protected DTO. Unsupported V1 returns `CONTRACT_UNSUPPORTED`; malformed/duplicate/unknown/noncanonical raw input returns `CANONICAL_HEADER_MALFORMED` or `EVIDENCE_UNMAPPED_FIELD` before policy, controller, reader, oracle, or audit-success execution. |
| Exact negative tests | Full exported-API reflection; typed V1 resolution attempt; typed V2 evidence resolution attempt; alternate JSON case/order/duplicate/unknown mutations through the actual authority; parser-call counter proves exactly one strict parser; controller/oracle counters remain zero on failure. |
| Exact typed result | `TrustFailureExceptionV2.Code` equals the specific code above; lifecycle state/version unchanged; no verdict; denial audit only where the contract requires a parsed request identity. |
| Smallest files | Contracts messages, compatibility manifest, signed-envelope service, closed-evidence verifier, both Programs/endpoints for DI/public route closure, tests. |
| Objective PASS formula | `public_typed_protected_entry_points = 0 ∧ raw_authority_entry_points = 2 ∧ alternate_parsers = 0 ∧ all_raw_mutants_rejected_before_authoritative_delegate` |
| Stop condition | Any public typed protected overload/class remains usable, any malformed input reaches controller/oracle, or any acceptance relies only on source-text matching. |

## F-02 reconciliation — ownership and policy authority are not closed

| Required field | Reconciliation |
|---|---|
| Finding ID / severity | F-02 / BLOCKING |
| Independent-review evidence | The 14-row catalog is distinct, but public legacy stores/registries/signers/readers/audit sinks overlap V3 owners. The concrete raw command service uses legacy issuer/resolver contracts, synthesizes grant/policy IDs and digest, hard-codes controller epoch `1`, derives lease expiry from the request, and synthesizes reader descriptors. |
| Exact affected production path | `SignedCommandServiceV2.VerifyAsync` → `ITrustedIssuerRegistry` + `IAuthorizationResolver` → locally created `ResolvedAuthorizationV3`/lease/evidence requirements → `ILifecycleControllerAuthority`. Legacy `ILeaseFenceStore`/`IIdempotencyStore`/`ILifecycleStateStore` create alternate mutation seams. |
| Root cause | The V3 ownership catalog was added alongside, rather than replacing or encapsulating, the earlier public authority contracts. Adapter construction copied untrusted/request facts into trusted DTOs. |
| Why tests passed | The owner test counts only catalog entries and distinct mapped types. It does not enumerate all exported authority-like interfaces, DI constructor dependencies, or data provenance into trusted records. Fakes accept synthetic policy/epoch/reader values. |
| Consequence | More than one component can appear authoritative; request metadata can be promoted to grant/lease/reader authority; audit cannot prove which policy owner decided. |
| Architecture still valid | YES — exactly-one ownership is explicit and unchanged. |
| Phase A / Phase B / external | Phase A: YES for interface closure and provenance. Phase B: NO for A1; actual durable provider remains Phase B. External prerequisite: NO; actual KMS/IAM adapters remain Phase C. |
| Exact source behavior required | Remove or make non-authoritative/non-public the overlapping legacy store, registry, signer, reader, and audit contracts. Production constructors consume only catalog owners or documented facets of the single composite durable owner. A server-owned `ResolvedAuthorizationV3` must come from the trusted resolver/policy providers and retain signed policy/grant digest/version. Lease resource ID, positive controller epoch, fence, holder, and expiry come from `ILeaseFenceAuthority`, never the request. Reader ID/version/artifact/stage come from the reader registry. Caller role/scope remains untrusted comparison data only and cannot create authority. |
| Exact negative tests | Exported-interface inventory against an assurance-owned 14-row expected map; forbidden legacy type list; constructor dependency reflection; caller role/scope/policy/grant/epoch/reader substitutions; duplicate-owner injection; zero/mismatched epoch and cross-resource lease. |
| Exact typed result | Ambiguous owner/provider: startup/readiness `DEPENDENCY_POLICY_MISMATCH`; caller role: `REQUEST_ROLE_FORBIDDEN`; scope: `SCOPE_MISMATCH`; policy/grant mismatch: `AUTHORIZATION_BINDING_MISMATCH`; stale/cross-resource lease or epoch: `LEASE_FENCE_STALE`; no state/version change. |
| Smallest files | Contracts messages, execution binding, signed-envelope service, state machine, Programs/options, verifier, tests. |
| Objective PASS formula | `catalog_rows = 14 ∧ distinct_authoritative_owners = 14 ∧ exported_parallel_authorities = 0 ∧ trusted_fields_from_untrusted_input = 0 ∧ controller_delegate_count = 1 only after all bindings pass` |
| Stop condition | Any required owner is unknown, duplicated, injectable through a legacy path, or any policy/grant/lease/reader trusted field is synthesized from the request. |

## Fourteen-owner reconciliation matrix

| Responsibility | Current declared owner | Required authoritative owner | Duplicate/missing now | A1 interface correction | Exactly-one-owner proof |
|---|---|---|---|---|---|
| NexaERP business runtime | `INexaErpBusinessRuntime` | Same | None identified | Keep intent-only; forbid role/scope/policy/grant fields | Assurance map exact type; DTO mutation cannot add authority |
| Control Plane | `IControlPlaneAuthority` | Same | Typed command services are alternate entry paths | Raw-only authority; compatibility code non-public | Exported API contains one protected command authority |
| Acceptance Verifier | `IAcceptanceVerifierAuthority` | Same | Public V1/V2 typed verifiers | Raw-only verifier authority | Exported API contains one protected evidence authority |
| Durable control-plane persistence | `IDurableControlPlanePersistenceProvider` | Same composite owner | Legacy lease/idempotency/lifecycle stores overlap | Remove/non-public legacy authorities; services depend on composite/facets only | Constructor/export inventory; duplicate provider makes readiness fail |
| Trusted issuer/key registry | `ITrustedIssuerKeyRegistryProvider` | Same | `ITrustedIssuerRegistry` drives concrete service | Use V3 provider only | Forbidden legacy dependency and issuer/key substitution tests |
| KMS/HSM signing | `IKmsHsmSigningProvider` | Same | `IEnvelopeSigner` overlaps | Compatibility signer non-public; authority uses KMS contract only | Export/constructor inventory plus wrong-provider identity test |
| Authoritative evidence reader | `IAuthoritativeEvidenceReaderProvider` | Same | Unsigned V2 reader path overlaps | One provider returns signed V3 bundles | Reader signature/digest/source mutation matrix |
| Immutable audit evidence | `IImmutableAuditEvidenceProvider` | Same | V2 trust/verifier sinks overlap | One immutable provider; compatibility adapters non-authoritative | Audit constructor/export inventory and append-failure test |
| Lifecycle controller | `ILifecycleControllerAuthority` | Same | Legacy state store/state-machine mutation path | Only controller accepts verified commands; state machine becomes policy component | Direct mutation API unavailable; controller called once |
| Backup/recovery authority | `IBackupRecoveryAuthority` | Same | None in Phase-A path | Retain separate attestation-only contract | Exact catalog and denied lifecycle mutation reflection |
| Purge authorizer | `IPurgeAuthorizer` | Same | None in Phase-A path | Retain authorization-only contract | Cannot execute purge; exact method surface |
| Purge executor | `IPurgeExecutor` | Same | None in Phase-A path | Retain execution-only contract requiring grant | Cannot authorize; exact method surface |
| Export authorizer | `IExportAuthorizer` | Same | None in Phase-A path | Retain authorization-only contract | Cannot deliver; exact method surface |
| Export delivery executor | `IExportDeliveryExecutor` | Same | None in Phase-A path | Retain delivery-only contract requiring release | Cannot authorize; exact method surface |

## Command/trust ingress trace and closure

There is no protected HTTP endpoint in either Phase-A host. That absence is correct and must remain. The current and required library call paths are:

| Path | Current trace | A1 disposition |
|---|---|---|
| Control Plane raw candidate | `SignedCommandServiceV2.VerifyAsync(raw header, raw payload, signature, subject, resource)` → strict V2 codecs → legacy issuer/resolver → synthetic V3 authorization/lease/readers → `ILifecycleControllerAuthority.TransitionAsync` | Replace legacy/synthetic authority with server-owned V3 providers; retain exactly one raw parser and one controller delegation |
| Command typed bypass | `SignedEnvelopeVerificationService.Verify(SignedCommandEnvelopeV1, maximumAge)` | Non-public/non-authoritative; public resolution impossible; V1 protected request returns `CONTRACT_UNSUPPORTED` through raw boundary |
| Acceptance current | `ClosedEvidenceVerifierV2.VerifyAsync(EvidenceVerificationRequestV2)` → V2 readiness → manifest/reader → typed oracle → V2 audit sink | Replace public entry with raw V3 evidence authority and strict codec; use signed bundles and V3 readiness/audit owners |
| Evidence parser side path | caller/test → `StrictEvidenceJsonV2.Parse` separately | Parser becomes mandatory internal first step, not an optional test helper |
| HTTP hosts | `/health/live`, `/health/ready`, `/version` only | Keep no protected operational endpoint; common guard contract tested offline and applied when later endpoint is authorized |

Closure determinations:

| Risk | Current | Required A1 result |
|---|---|---|
| Typed-object verification bypass | YES | NO; exported count zero |
| Caller-supplied trusted role/scope | Compared then used to construct trusted record | Never authoritative; mismatch gets exact typed denial |
| Alternate canonical parser | YES, because typed paths bypass codecs | NO; one internal strict parser per raw authority |
| Unsigned trust metadata | Synthetic policy/grant/lease/reader context | NO; trusted provider result and digest/version required |
| Missing tenant/database/operation binding | Many checks exist; grant/reader/lease provenance incomplete | Exact org/cluster/instance/resource/version/operation/request/attempt binding on every artifact |
| Direct lifecycle execution without controller authorization | Legacy public seams permit alternate composition | NO; only controller accepts a verified command; direct public state mutation absent |

## F-03 reconciliation — lifecycle table is incomplete

| Required field | Reconciliation |
|---|---|
| Finding ID / severity | F-03 / BLOCKING |
| Independent-review evidence | V3 table omits `QUARANTINE`, `CANCEL`, and `EXPIRE`; export rules all use `Accepted` without export substate; lease lacks resource binding and positive/current epoch; exact audit events are collapsed. |
| Exact affected production path | Raw command service creates `VerifiedLifecycleCommandV3` → `ILifecycleControllerAuthority` → `Rev869BControllerStateMachine.RequirePhaseACommand` → `PhaseARules`. Legacy `CreateReplacement` contains some missing semantics but is not the authoritative V3 table. |
| Root cause | A partial V3 rule table was added beside the more detailed legacy state object; V3 DTOs omitted authorization/export substates and exact audit identity. |
| Why tests passed | Tests derive allowed transitions from production `ListedOperations`/`ListedOperationRules`, test the legacy table, and never compare V3 rules with an independent 26-row expectation. |
| Consequence | Required quarantine/cancellation/expiry is unavailable on the authority path; export can skip authorization/delivery order; lease can be replayed across resources/epochs; audit cannot identify the exact transition. |
| Architecture still valid | YES — the frozen 26-row matrix remains the authoritative correction target. |
| Phase A / Phase B / external | Phase A: YES for table/types/validators. Phase B: NO for A1; durable enforcement remains Phase B. External prerequisite: NO. |
| Exact source behavior required | One V3 conceptual table covers all 26 frozen rows. DTOs carry lifecycle state/version, authorization state/grant digest, export substate/release, resource-bound lease with positive controller epoch/fence/holder/expiry, attempt, evidence requirement descriptors, failure/retry, and exact audit kind. Authorization operations create active one-time grants; execution consumes the exact grant; cancel/expire are terminal authorization-substate changes; export order is strict. Every unlisted state/substate/operation is illegal. |
| Exact negative tests | Independent 26-row theory; complete complement; omitted-row mutants; role/evidence/lease/version/grant/retry/audit mutants per row; export skip/replay; cancellation by wrong actor; expiry before server time; cross-resource/zero/stale epoch; direct requested-next-state mutation. |
| Exact typed result | Legal row returns its exact next lifecycle/substate and `LIFECYCLE_COMMITTED`; illegal/unlisted returns `STATE_TRANSITION_ILLEGAL`; stale version `RESOURCE_VERSION_STALE`; grant mismatch/reuse `AUTHORIZATION_BINDING_MISMATCH`; missing/expired/stale lease `LEASE_REQUIRED`/`LEASE_EXPIRED`/`LEASE_FENCE_STALE`; no state/version change on denial. |
| Smallest files | Contracts messages, execution binding, state machine, signed-envelope service, tests. |
| Objective PASS formula | `frozen_conceptual_rows = 26 ∧ implemented_rows_cover_exactly_26 ∧ missing_rows = 0 ∧ unlisted_allowed = 0 ∧ export_skip_paths = 0 ∧ denied_mutations_change_state = 0` |
| Stop condition | Any expected row is missing, production defines test truth, an unlisted pair succeeds, export skips a substate, authorization is reusable, or a denial changes state/version. |

## Twenty-six-transition Correction-A1 acceptance matrix

Every row additionally requires exact organization, database cluster/instance, resource, operation, request digest, current version, policy/grant digest, and the exact audit event named below. A failed gate changes no lifecycle, authorization, export, attempt, or resource version.

| # | Initial / operation | Required authority and evidence | Exact accepted result | Required negative assertion |
|---:|---|---|---|---|
| 1 | Registered / AUTHORIZE_PREPARE | Management Operator via trusted Deployment Controller; target registration + identity manifest; no lease | Preflight + ACTIVE prepare grant; `prepare_authorized` | Caller role/grant or wrong version → `REQUEST_ROLE_FORBIDDEN`/`AUTHORIZATION_BINDING_MISMATCH`/`RESOURCE_VERSION_STALE` |
| 2 | Preflight / PREPARE | ProvisioningExecutor; identity/TLS/catalog/ACL facts; current lease/fence/epoch | Provisioning; grant CONSUMED; `prepare_started` | Missing fact or cross-resource lease → `READER_MISSING`/`LEASE_FENCE_STALE` |
| 3 | Provisioning / COMPLETE_PREPARE | ProvisioningExecutor; signed receipt + ready facts; current lease | Ready; `prepare_completed` | Unsigned/tampered receipt → `EVIDENCE_TAMPERED` |
| 4 | Provisioning / FAIL | ProvisioningExecutor; signed terminal failure facts; current lease | Failed; `prepare_failed` | Conflicting facts cannot produce Failed; typed tamper denial and no change |
| 5 | Ready / AUTHORIZE_EXECUTE | Management Operator via trusted controller; exact migration/source/manifest/target plan; no lease | MigrationAuthorized + ACTIVE execute grant; `execute_authorized` | Cross-operation grant → `AUTHORIZATION_BINDING_MISMATCH` |
| 6 | MigrationAuthorized / EXECUTE | MigrationExecutor; bound active grant + preflight facts; current lease | Migrating; grant CONSUMED; `execute_started` | Reused grant → `AUTHORIZATION_BINDING_MISMATCH` |
| 7 | Migrating / COMPLETE_EXECUTE | MigrationExecutor; signed receipt + ledger; current lease | VerificationPending; `execute_completed` | Wrong attempt/watermark → `EVIDENCE_TAMPERED` |
| 8 | Migrating / FAIL | MigrationExecutor; signed terminal failure/rollback facts; current lease | Failed; `execute_failed` | Ambiguous binding cannot commit Failed; no change |
| 9 | VerificationPending / VERIFY_ACCEPT | AcceptanceVerifier; signed PASS verdict, oracle/artifact hash, bundle hashes, WORM receipt; current lease | Accepted; `verification_accepted` | Caller PASS or wrong oracle → `EVIDENCE_TAMPERED`/`ORACLE_MISMATCH` |
| 10 | VerificationPending / VERIFY_REJECT | AcceptanceVerifier; signed FAIL + exact reasons; current lease | Failed; `verification_rejected` | Missing reasons/signature → `EVIDENCE_TAMPERED` |
| 11 | Any nonterminal except Purged / QUARANTINE | ControlPlaneRuntime/Reconciler; exact inconsistency; current version and lease when held | Quarantined; `resource_quarantined`; no auto-exit | Purged or caller-requested quarantine → `STATE_TRANSITION_ILLEGAL`/`SUBJECT_UNAUTHORIZED` |
| 12 | Quarantined / AUTHORIZE_RECOVER | RecoveryApprover via trusted controller; diagnosis/identity/plan/approvals; no lease | RecoveryAuthorized + ACTIVE one-time grant; `recovery_authorized` | Stale/foreign approval → `AUTHORIZATION_BINDING_MISMATCH` |
| 13 | RecoveryAuthorized / RECOVER | RecoveryExecutor; active grant + before facts; current lease | Recovering; grant CONSUMED; `recovery_started` | Consumed/reused grant → `AUTHORIZATION_BINDING_MISMATCH` |
| 14 | Recovering / COMPLETE_RECOVER | RecoveryExecutor; signed restored/ready facts; current lease | Ready; `recovery_completed` | Wrong target identity → `INSTANCE_MISMATCH` |
| 15 | Recovering / FAIL | RecoveryExecutor; signed terminal failure; current lease | Failed; `recovery_failed` | Ambiguous/tampered evidence → `EVIDENCE_TAMPERED` |
| 16 | Accepted, Failed, or Quarantined / AUTHORIZE_DROP | DropAuthorizer via dual approval; identity/retention/backup/no-use/reason; no lease | DropAuthorized + ACTIVE one-time grant; `drop_authorized` | Any other state/one approval → `STATE_TRANSITION_ILLEGAL`/`AUTHORIZATION_BINDING_MISMATCH` |
| 17 | DropAuthorized / DROP | DropExecutor; active grant + backup/hold/target facts; current lease | Dropped; grant CONSUMED; `drop_completed` or typed interrupted result | New automatic attempt or wrong target → typed denial, no state change |
| 18 | Dropped / AUTHORIZE_PURGE | PurgeAuthorizer via Records+Data Owner; root/hold/retention; no lease | PurgeAuthorized + ACTIVE one-time grant; `purge_authorized` | Hold or root substitution → `AUTHORIZATION_BINDING_MISMATCH` |
| 19 | PurgeAuthorized / PURGE | PurgeExecutor; exact root/batches + active grant; current lease | Purging; grant CONSUMED; `purge_started` | Candidate/batch drift → `EVIDENCE_TAMPERED` |
| 20 | Purging / COMPLETE_PURGE | PurgeExecutor; zero candidates + per-batch audit; current lease | Purged; `purge_completed` | Nonzero/unaudited candidates → `EVIDENCE_TAMPERED` |
| 21 | Purging / FAIL | PurgeExecutor; signed failure + remaining root; current lease | Dropped; `purge_failed` | Root drift cannot commit ordinary failure; quarantine/typed denial contract applies |
| 22 | Accepted export NONE/EXPIRED/FAILED / AUTHORIZE_EXPORT | ExportAuthorizer via Data Owner+Privacy; immutable minimized root/purpose/recipients/expiry; no lease | Accepted/AUTHORIZED + one-time release; `export_authorized` | DELIVERED/active release or recipient/root substitution → `STATE_TRANSITION_ILLEGAL`/`AUTHORIZATION_BINDING_MISMATCH` |
| 23 | Accepted/AUTHORIZED / EXPORT | ExportDelivery; active release + exact root; current lease | Accepted/DELIVERING; release consumed for start; `export_started` | NONE/DELIVERING/DELIVERED or foreign release → exact typed denial |
| 24 | Accepted/DELIVERING / COMPLETE_EXPORT | ExportDelivery; signed matching recipient receipt; current lease | Accepted/DELIVERED; `export_delivered` | Completion from AUTHORIZED or substituted receipt → `STATE_TRANSITION_ILLEGAL`/`EVIDENCE_TAMPERED` |
| 25 | Any state with unused ACTIVE authorization / CANCEL | Original authorizer; exact grant + reason; no lease | Same lifecycle state; authorization CANCELLED; `authorization_cancelled` | Different actor, consumed/cancelled grant → `SUBJECT_UNAUTHORIZED`/`AUTHORIZATION_BINDING_MISMATCH` |
| 26 | Any state with expired ACTIVE authorization / EXPIRE | ControlPlane Reconciler; server time beyond expiry; no lease | Same lifecycle state; authorization EXPIRED; `authorization_expired` | Before expiry or caller time → `NOT_YET_VALID`/`SUBJECT_UNAUTHORIZED` |

## F-04 reconciliation — verifier facts are not cryptographically closed

| Required field | Reconciliation |
|---|---|
| Finding ID / severity | F-04 / BLOCKING |
| Independent-review evidence | V2 authoritative facts have no signature; verifier copies caller receipt digest without recomputing it over returned observations; caller `ActionReceipt` reaches the oracle; descriptor fields/limits/source are not fully enforced; typed parser bypass and unconditional-PASS oracle survive. |
| Exact affected production path | `ClosedEvidenceVerifierV2.VerifyAsync(typed request)` → legacy readiness → manifest registry → reader registry → unsigned reader result → replace `RawFacts` only → typed oracle receives envelope including caller action receipt → V2 audit sink. |
| Root cause | The concrete verifier remained on V2 compatibility DTOs while signed V3 bundle contracts were added separately. Trust was assigned to provider identity rather than verified response bytes. |
| Why tests passed | Fake reader can mutate observations and copy digest; fake oracle always PASS; caller-fact test checks only `RawFacts`; descriptor bound mutations and action-receipt influence are absent. |
| Consequence | Reader compromise/bug or caller-controlled outcome metadata can influence PASS without cryptographic binding, exact scope/time, or registry limits. |
| Architecture still valid | YES — signed authoritative fact bundles and closed oracle ownership are already frozen. |
| Phase A / Phase B / external | Phase A: YES for raw contract/verifier/fixtures. Phase B: NO. External prerequisite: NO for offline fake keys; real reader/KMS/WORM integrations remain later phases. |
| Exact source behavior required | Raw V3 evidence only. Verifier creates server-owned `EvidenceScopeTemporalBindingV3`; resolves exact oracle and readers; readers return raw signed bundles, never verdict/expected/PASS fields; verifier checks artifact, key, algorithm, signature, canonical facts hash, request digest, org/database/resource/version/attempt/stage/time/watermark, source type, allowed fields, per-reader/global count and byte limits. Oracle receives only minimized verified facts and server-owned action outcome/receipt facts. Caller facts/action receipt/expected values cannot enter oracle input. Audit append must succeed before signed verdict return. |
| Exact negative tests | Caller raw fact/action/expected/PASS mutations; copied digest with changed fact; wrong reader key/artifact/source/scope/time/attempt/stage/watermark; field/count/byte overflow; duplicate/missing reader; oracle artifact mismatch; audit failure; reader attempts to return verdict. |
| Exact typed result | Tamper/signature/hash/binding → `EVIDENCE_TAMPERED`; unauthorized reader/source/field → `READER_UNAUTHORIZED`; missing/duplicate → `READER_MISSING`/`READER_DUPLICATE`; bounds → `EVIDENCE_TOO_LARGE` or `CONTRACT_LIMIT_EXCEEDED`; oracle mismatch → `ORACLE_MISMATCH`; audit failure → `AUDIT_APPEND_FAILED`; no verdict/oracle call where pre-oracle validation fails. |
| Smallest files | Contracts messages, closed-evidence verifier, verifier options/Program, signed-envelope codec where shared, tests. |
| Objective PASS formula | `oracle_inputs = verified_server_owned_facts_only ∧ verified_bundle_signatures = bundle_count ∧ recomputed_hashes = bundle_count ∧ caller_outcome_fields_reaching_oracle = 0 ∧ invalid_bundle_or_audit_returns_no_verdict` |
| Stop condition | Any caller fact/outcome reaches oracle, any reader response is accepted without signature/hash recomputation, any descriptor limit is unenforced, or an unconditional-PASS oracle remains acceptance evidence. |

## Evidence, oracle, and readiness closure matrix

| Question | Current determination | A1 required result |
|---|---|---|
| Can caller facts reach oracle? | `RawFacts` replaced, but caller `ActionReceipt` remains | No caller facts, action result, expected value, verdict, or PASS-like field in oracle input |
| Can reader submit derived PASS? | V2 type has facts, but typed oracle and unverified observations permit equivalent influence | Reader bundle schema rejects verdict/expected/outcome fields with `EVIDENCE_UNMAPPED_FIELD` |
| Reader/oracle ownership duplicated? | Yes, V2 and V3 public contracts coexist | One catalog owner each; compatibility types non-authoritative |
| Fact scope/time complete? | No attempt/stage/cleanup signature binding; weak age model | Exact scope-temporal binding signed and compared field-by-field |
| Limits caller-controlled? | Global options exist; descriptor limits/source not fully enforced | Server registry sets all limits; effective limit is the stricter server value |
| READY without reader/oracle/audit? | Empty real providers are safe, but fake/legacy path and exceptions/staleness are incomplete | All exact providers present, unique, fresh, identity/version/policy matched; otherwise typed NOT_READY and no oracle/verdict |

## F-05 reconciliation — audit and readiness contracts are incomplete

| Required field | Reconciliation |
|---|---|
| Finding ID / severity | F-05 / REQUIRED |
| Independent-review evidence | V3 audit omits prior/new lifecycle state/version, attempt, grant digest, lease/epoch/fence, source transaction, signing key/version and ingestion receipt. Audit kinds are generic. Readiness lacks per-fact observation/expiry, converts no provider exceptions, has no common guard proof, and concrete verifier uses legacy readiness. |
| Exact affected production path | `PhaseAReadinessAuthority.CheckAsync` → dependency providers; `/health/ready` in both hosts. Verifier separately injects `ITrustReadinessProbe`. `ImmutableAuditEventV3` and V2 audit sinks are separate paths. |
| Root cause | Minimal enums/DTOs proved presence but not freshness or exact transition provenance; hosts and verifier were not unified on the V3 authority. |
| Why tests passed | Providers always return values rather than throw; enum values generate expected dependencies; no stale timestamp or HTTP integration; audit tests inspect a subset and use fake sinks. |
| Consequence | A stale/failed dependency can produce an exception/500 rather than a complete typed NOT_READY snapshot; downstream code may bypass one guard. Audit cannot reconstruct exact state/fence/grant causality. |
| Architecture still valid | YES. |
| Phase A / Phase B / external | Phase A: YES for exact DTOs, mapping, common guard abstraction, offline host tests. Phase B: NO for A1. External prerequisite: NO; real WORM/health adapters remain later. |
| Exact source behavior required | Dependency result carries checked-at, valid-until, required/observed identity/version/policy and sanitized diagnostic. Authority catches provider failure/cancellation according to policy and returns exact non-READY state; duplicates/missing/stale/mismatch never READY. Both hosts and verifier use the same V3 authority/guard. Audit event carries exact before/after state+version, attempt, grant/policy digest, lease/resource/epoch/fence, transaction/correlation, key/version, prior hash and exact event kind; append receipt is required before externally successful result where frozen. |
| Exact negative tests | Each dependency absent, duplicate, exception, timeout, stale, wrong version/identity/policy, degraded; both `/health/ready` routes return 503 and protected delegate count zero. Exact audit field mutation; audit append null/failure; forbidden secret/fact field. |
| Exact typed result | Dependency codes map one-to-one to `DEPENDENCY_NOT_CONFIGURED`, `DEPENDENCY_UNAVAILABLE`, `DEPENDENCY_VERSION_MISMATCH`, `DEPENDENCY_IDENTITY_MISMATCH`, `DEPENDENCY_POLICY_MISMATCH`, `DEPENDENCY_DEGRADED_UNSAFE`; stale maps to unavailable or a new explicitly frozen typed state/code, not READY. Audit failure is `AUDIT_APPEND_FAILED`; no success/verdict. |
| Smallest files | Contracts messages, both options/Programs, controller endpoints, verifier, tests. |
| Objective PASS formula | `READY ⇔ exact_dependency_set ∧ unique ∧ all_fresh ∧ all_identity_version_policy_match ∧ all_safe ∧ common_guard_invoked`; and `successful_result ⇒ exact_typed_audit ∧ durable_receipt` |
| Stop condition | Exception/timeout/stale can yield READY or untyped 500, any protected delegate runs while non-READY, audit lacks an exact frozen binding, or audit failure returns success. |

## F-06 reconciliation — tests pass but are not independently decisive

| Required field | Reconciliation |
|---|---|
| Finding ID / severity | F-06 / BLOCKING |
| Independent-review evidence | Production-derived transition allowlists, sequential in-memory concurrency, unconditional-PASS oracle, incomplete reflection, enum-derived readiness, fake offset paging, and no decisive mutation report. |
| Exact affected production path | `ArchitectureFreezeContractTests.cs` is the only authorized Phase-A test file; several tests call fakes or legacy helpers directly rather than raw authority → policy/readiness/controller/verifier/audit boundaries. |
| Root cause | Tests optimized for type presence and happy-path compatibility instead of assurance-owned expected artifacts and mutation survival. |
| Why tests passed | Implementation and expected truth share the same enumeration; permissive fakes return success; concurrency is not concurrent; surface checks omit other exported types. |
| Consequence | Weakening/removing security checks can leave all tests green, so the suite cannot authorize later phases. |
| Architecture still valid | YES — independent Assurance ownership is already frozen. |
| Phase A / Phase B / external | Phase A: YES. Phase B: NO; no production DB claims. External prerequisite: NO; mutation runner must be offline. |
| Exact source behavior required | Test expectations are literal assurance-owned data in the test file, never production enumeration. Tests traverse actual raw authority paths with deterministic strict fake providers. Every negative asserts exact `TrustFailureCodeV2`, exact state/version/no-change, controller/oracle/audit call counts, and sanitized audit. True concurrency starts blocked tasks together at the service boundary. Mutation report covers every decisive gate and has zero surviving decisive mutants. |
| Exact negative tests | The complete required-test matrix below, plus named mutants for removing parser, signature, policy, scope, lease, version, evidence, readiness, audit, lifecycle, export, and idempotency checks. |
| Exact typed result | Per matrix below; generic `Throws`, source-text-only checks, and `Assert.True` without exact state/code are not acceptance. |
| Smallest files | `ArchitectureFreezeContractTests.cs` plus production files exercised; no new test file required. |
| Objective PASS formula | `required_tests_pass ∧ decisive_mutants_total > 0 ∧ decisive_mutants_survived = 0 ∧ production_derived_expectations = 0 ∧ unconditional_pass_fakes = 0 ∧ generic_exception_acceptance = 0` |
| Stop condition | Any decisive mutant survives, expected matrix comes from production, any negative lacks exact code/state/call-count assertion, or concurrency/readiness/paging remains fake-only and non-probative. |

## Existing 46-test quality reconciliation

Each row identifies the production behavior the current test does not fully prove and the required A1 replacement/mutation. “Keep” still requires exact typed assertions through the authoritative path.

| # | Existing test | Why non-probative or incomplete | Required production exercise / mutation | Independently expected typed outcome |
|---:|---|---|---|---|
| 1 | `Compatibility_manifest_is_closed_and_versioned` | Constants do not close APIs | Enumerate all exported protected entry points; mutate V1 version | V1 raw request → `CONTRACT_UNSUPPORTED`; zero typed entries |
| 2 | `Canonical_json_is_deterministic_and_sorts_object_keys` | Single serializer unit vector | Feed order/case/number/duplicate mutants through raw authority | Noncanonical → `CANONICAL_HEADER_MALFORMED`; no delegate |
| 3 | `State_machine_rejects_skip_and_allows_frozen_path` | Legacy path, not literal V3 matrix | Use assurance-owned 26 rows and skip mutants | Legal exact result; skip → `STATE_TRANSITION_ILLEGAL`, no change |
| 4 | `Binding_comparison_fails_closed_on_company_instance_lease_and_subcase` | Legacy typed binding | Raw authority substitutions across all binding fields | Exact mismatch code; no controller/state change |
| 5 | `Command_signing_and_verification_rejects_replay_and_revocation` | Exercises forbidden V1 API | Raw V3 signed bytes with replay/revocation mutations | `NONCE_REPLAY` / `KEY_REVOKED`; no transition |
| 6 | `Command_verification_fails_closed_for_version_algorithm_key_tamper_and_staleness` | Forbidden V1 path | Same mutants through sole raw authority | Exact contract/algorithm/signature/expiry codes |
| 7 | `Command_policy_rejects_stale_lease_and_cross_role_authorization` | Does not prove authoritative provider provenance | Provider-owned policy/lease, caller substitution | `LEASE_FENCE_STALE` / `REQUEST_ROLE_FORBIDDEN`; no change |
| 8 | `Evidence_contract_has_no_caller_supplied_verdict` | Names only; action receipt remains | Raw evidence adds verdict/expected/action result | `EVIDENCE_UNMAPPED_FIELD`; oracle count 0 |
| 9 | `Closed_verifier_calculates_pass_and_writes_durable_audit_reference` | Unconditional-PASS V1 oracle | Mutation-sensitive V3 oracle with signed bundles | Exact signed verdict only after durable audit receipt |
| 10 | `Closed_verifier_rejects_missing_durable_stage_before_oracle_runs` | Legacy path | Remove each required signed stage in raw V3 path | `READER_MISSING`; oracle/audit-success count 0 |
| 11 | `Closed_verifier_enforces_bounded_payload_rules` | Not per-reader/source bound | Exceed every global and descriptor bound by one | `EVIDENCE_TOO_LARGE`/`CONTRACT_LIMIT_EXCEEDED` |
| 12 | `Options_reject_production_identity_and_accept_bounded_nonproduction_pattern` | Configuration unit only | Invalid identity/version/policy through readiness | 503 + exact dependency code; no delegate |
| 13 | `CanonicalV2GoldenVectorIsByteExact` | Useful but one vector | Keep; add independently pinned V3 command/evidence vectors | Exact byte/hash equality; one-byte mutation `SIGNATURE_INVALID` |
| 14 | `EveryProtectedHeaderMutationIsRejected` | Fake crypto/legacy authority | Run every signed field mutation through raw V3 authority | Exact field-specific code; no controller call |
| 15 | `EveryPayloadFieldMutationBreaksHash` | Integrity only | Keep through raw authority; mutation with recomputed caller hash but invalid grant | `PAYLOAD_HASH_MISMATCH` or `AUTHORIZATION_BINDING_MISMATCH` |
| 16 | `UnknownIssuerKeyAlgorithmVersionFailClosed` | Legacy fake registry | V3 registry/KMS/policy provider variants | Exact issuer/key/algorithm/version code |
| 17 | `RequestRoleCannotGrantAuthority` | Resolver result provenance incomplete | Caller role with trusted resolver returning different role | `REQUEST_ROLE_FORBIDDEN`; no trusted DTO created |
| 18 | `AudienceSubjectAndScopeAreExact` | Useful partial binding | Mutate each through raw authority and trusted identity | `AUDIENCE_MISMATCH`/`SUBJECT_UNAUTHORIZED`/`SCOPE_MISMATCH` |
| 19 | `ClusterInstanceOperationAndVersionSubstitutionReject` | Useful partial binding | Add org/resource/request/grant/attempt substitutions | Exact mismatch code; state/version unchanged |
| 20 | `TemporalWindowIsServerOwned` | Header derives lease facts | Provider clock and lease facts; caller time mutations | `NOT_YET_VALID`/`ENVELOPE_EXPIRED`/`LEASE_EXPIRED` |
| 21 | `NonceReplayIsIndependentOfIdempotency` | Direct in-memory stores | Raw service with coordinated provider decisions | Replay → `NONCE_REPLAY`; idempotency result untouched |
| 22 | `LeaseAcquireRenewExpireAndFenceAreMonotonic` | No resource/epoch authority proof | Provider fixture with resource/epoch/fence mutants | `LEASE_FENCE_STALE`; no transition |
| 23 | `EveryUnlistedStateOperationPairIsIllegal` | Production-derived allowlist | Literal complement of assurance 26-row table | Every complement → `STATE_TRANSITION_ILLEGAL` |
| 24 | `EveryListedTransitionHasExactRoleEvidenceAndFence` | Iterates production table | Literal 26-row expected role/evidence/lease/audit data | Each row exact; each one-field mutant exact denial |
| 25 | `IdempotencyDecisionTableIsExact` | Decision unit only | Raw authority → composite provider outcomes | Exact replay/in-progress/conflict typed outcome, no duplicate delegate |
| 26 | `ConcurrentDuplicateHasOneAuthoritativeWinner` | Sequential fake calls | Barrier-started concurrent raw authority calls | One FIRST_OWNER; other IN_PROGRESS/replay; business delegate once |
| 27 | `OracleManifestAndReadersAreServerPinned` | Reader response unsigned | Signed bundle key/artifact/hash/source mutants | `EVIDENCE_TAMPERED`/`READER_UNAUTHORIZED`; oracle 0 |
| 28 | `CallerVerdictAndExpectedValuesAreUnmapped` | Parser separate from verifier | Mutants through raw verifier entry | `EVIDENCE_UNMAPPED_FIELD`; oracle 0 |
| 29 | `TemporalEvidenceBindingIsExact` | Missing attempt/stage/watermark closure | Mutate every V3 temporal binding field | `EVIDENCE_TAMPERED`; no oracle/verdict |
| 30 | `AllEvidenceDimensionsAreServerBounded` | Descriptor limits/source not enforced | Per-reader/global field/fact/byte/source overages | Exact bounds/reader code; oracle 0 |
| 31 | `CallerFactsCannotReplaceAuthoritativeReaderFacts` | Caller action receipt survives | Mutate raw facts, action receipt, expected/outcome independently | Oracle input hash unchanged or input rejected; no caller influence |
| 32 | `SensitiveFactsNeverSerializeOrLog` | Serialization subset only | Inject every forbidden field into reader/audit/readiness errors | `EVIDENCE_SENSITIVE_FIELD`/`AUDIT_DATA_FORBIDDEN`; sanitized output |
| 33 | `MissingVerifierDependencyReturnsNotReady` | Boolean legacy fake | Real V3 authority and raw verifier; each dependency missing | 503/`SERVICE_NOT_READY`; reader/oracle/verdict counts 0 |
| 34 | `RuntimeIdentityCannotEscalateAcrossRoles` | Type assertion only | Runtime identity attempts each authorizer/executor role | `SUBJECT_UNAUTHORIZED`; no provider/controller call |
| 35 | `AuditAppendFailurePreventsVerdictCommit` | Useful fake-sink check | V3 immutable provider failure after calculation | `AUDIT_APPEND_FAILED`; no signed verdict returned |
| 36 | `MalformedCanonicalInputHasTypedFailure` | Standalone parser | Actual command and evidence raw authority inputs | Exact malformed/unmapped code; downstream counts 0 |
| 37 | `TenMillionMasterContractUsesPagingOnly` | Three-page offset fake | Signed opaque-token codec: wrong scope/snapshot/prior hash/expiry/page size/cancel | `PAGINATION_TOKEN_INVALID`/`CONTRACT_LIMIT_EXCEEDED`; no offset API |
| 38 | `PhaseACompatibilityManifestIsExactAndClosed` | Constants only | Keep plus exported-contract compatibility closure | Wrong version → exact typed code; unsupported V1 unreachable typed |
| 39 | `EveryPhaseAProductionResponsibilityHasOneDistinctOwnerContract` | Catalog only | Full exported authority/constructor inventory | Exact 14 map; duplicate → `DEPENDENCY_POLICY_MISMATCH` |
| 40 | `UntrustedIntentCannotCarryRoleScopeOrPermissionAuthority` | Strong DTO check only | Keep; raw extension/unmapped authority fields | `CANONICAL_HEADER_MALFORMED`/`EVIDENCE_UNMAPPED_FIELD` |
| 41 | `ProtectedCommandSurfaceAcceptsRawCanonicalBytesAndDelegatesOnlyToController` | One class only | Whole assembly API + actual raw call | Exactly one controller call after gates; zero typed surfaces |
| 42 | `MissingPhaseADependenciesAreEnumeratedAndFailClosed` | No exception/stale/HTTP | Missing, throw, timeout, stale via both hosts/verifier | Exact dependency code, 503, delegate 0 |
| 43 | `OnlyOneReadyProviderPerDependencyCanEnableProtectedOperations` | Enum supplies expectation | Literal dependency list owned by test; duplicate/wrong identity | Non-READY + exact policy/identity code |
| 44 | `EveryReadinessStateHasAnExactTypedFailureCode` | Mapping only | Keep; add exception/stale transitions | Exact code; READY never mapped from unsafe state |
| 45 | `PhaseAEvidenceAndAuditSurfacesContainNoCallerVerdictOrSecretMaterial` | Reflection names only | Runtime serialization/audit mutation and oracle input capture | Typed forbidden-data code; forbidden bytes absent |
| 46 | `PhaseAContractBoundsAreFiniteAndEnforced` | Validator only | Through command/evidence/paging authorities at limit and limit+1 | Limit accepted; +1 exact bounds code and no delegate |

## Required Correction-A1 test acceptance matrix

These are exact required test names. They may be `[Theory]` tests with independently declared rows. All are offline. No test may open a socket, database, process, KMS, external service, or file beneath the prohibited sibling.

| Required unique test | Initial state | Trusted / untrusted inputs | Action | Expected typed result and state | Audit/evidence | Required rejected mutation |
|---|---|---|---|---|---|---|
| `A1_PublicProtectedSurfaceHasOnlyTwoRawAuthorities` | Loaded four Phase-A assemblies | Trusted literal API allowlist / exported types | Reflect public methods/constructors | Exact command+evidence raw authorities; zero typed protected entries | None | Re-publicize any legacy verifier/signer fails test |
| `A1_RawCommandCodecRejectsEveryNonCanonicalMutationBeforeAuthority` | Registered v1 resource | Trusted identity/policy / mutated bytes | Call control authority | Exact malformed/signature/binding code; state/version unchanged | Denial only; controller 0 | Duplicate, unknown, case, order, number, UTF-8, trailing byte |
| `A1_RawEvidenceCodecRejectsEveryNonCanonicalMutationBeforeReaderOrOracle` | VerificationPending | Trusted registry / mutated evidence bytes | Call verifier authority | Exact malformed/unmapped code; no verdict | Reader/oracle/audit-success 0 | Duplicate, unknown, typed bypass, caller verdict/action |
| `A1_ExactlyFourteenAuthoritativeOwnersAndNoParallelPublicAuthority` | Loaded assemblies | Trusted literal owner map / constructors | Reflect types and dependencies | Exact 14 distinct owners; no forbidden legacy owner | None | Duplicate/missing/legacy constructor dependency |
| `A1_TrustedGrantPolicyLeaseAndReaderFactsCannotBeSynthesizedFromRequest` | Registered | Trusted providers / substituted request fields | Call raw command authority | Exact role/scope/auth/lease/reader code; no transition | Sanitized denial | Policy version, grant digest, epoch, reader ID copied from caller |
| `A1_FrozenLifecycleMatrixHasExactTwentySixConceptualRows` | Each frozen state/substate | Literal trusted row fixtures / exact command | Execute state-machine policy through controller fixture | Exact next state/substate/version/audit kind | Exact evidence requirement set | Remove/change/add any row or field |
| `A1_EveryUnlistedLifecycleCombinationIsIllegal` | Every state/substate | Trusted matrix / complete literal complement | Execute each complement | `STATE_TRANSITION_ILLEGAL`; no state/version change | Denial audit | Production enumeration used as expected truth |
| `A1_EveryLifecycleBindingMutationFailsWithoutStateChange` | Each of 26 rows | One valid row / mutate one role,evidence,grant,version,lease,audit field | Execute | Exact field-specific failure; no change | Exact denial; no commit event | Delete any binding check |
| `A1_ExportSubstatesCannotBeSkippedOrReused` | NONE, AUTHORIZED, DELIVERING, DELIVERED, FAILED, EXPIRED | Trusted release / wrong operation/release | Authorize/start/complete | Only exact sequence succeeds; otherwise exact state/auth code | Exact export events | Complete from AUTHORIZED; start from NONE; reuse release |
| `A1_CancelAndExpireChangeOnlyAuthorizationSubstate` | Active authorization in each legal lifecycle state | Original authorizer/server clock / wrong actor/time | CANCEL or EXPIRE | CANCELLED/EXPIRED; lifecycle/version rules exact | Exact cancellation/expiry audit | Cancel consumed grant; expire before server time |
| `A1_QuarantineIsControllerOwnedAndPurgedIsTerminal` | Every state | Reconciler + inconsistency / caller request | QUARANTINE | Nonterminal → Quarantined; Purged illegal | `resource_quarantined` only on success | Caller role; no evidence; Purged |
| `A1_LeaseBindsResourceHolderEpochFenceAndExpiry` | Lease-required row | Trusted lease authority / one-field mutations | Execute | `LEASE_REQUIRED`/`LEASE_EXPIRED`/`LEASE_FENCE_STALE`; no change | Denial | Cross-resource, zero/stale epoch, holder/fence/expiry |
| `A1_AuthorizationIsOneTimeAndOperationBound` | Active grant | Trusted grant / reuse or operation substitution | Execute twice | First exact success; second `AUTHORIZATION_BINDING_MISMATCH` | One commit + one denial | Reuse across execute/recover/drop/purge/export |
| `A1_ReaderBundleSignatureHashAndScopeAreRecomputed` | VerificationPending | Trusted key/descriptor / bundle mutants | Verify raw evidence | Valid calculated verdict; mutants exact tamper/reader code | Verified bundle hashes + audit receipt | Copy digest while changing fact; wrong signature/source/scope/time |
| `A1_CallerFactsActionReceiptExpectedValuesAndVerdictNeverReachOracle` | VerificationPending | Trusted bundles / caller forbidden fields | Verify/capture oracle input | Forbidden input rejected or oracle input byte-identical to trusted facts | No forbidden audit bytes | Toggle caller PASS/action/expected/raw fact |
| `A1_ReaderAndGlobalLimitsUseStricterServerOwnedBound` | VerificationPending | Trusted descriptor/options / limit and +1 | Verify | Limit accepted; +1 exact bounds code; no verdict | Sanitized denial | Caller raises limit or changes source type |
| `A1_OracleIsPinnedMutationSensitiveAndCannotAcceptReaderVerdict` | VerificationPending | Trusted oracle artifact / fact and artifact mutants | Verify | Expected PASS/FAIL from literal facts; mismatch exact code | Calculation hash + durable audit | Unconditional PASS oracle; reader verdict field |
| `A1_ReadinessFailsClosedForMissingDuplicateExceptionTimeoutStaleAndMismatch` | Both hosts + verifier | Literal 15 dependencies / unsafe variants | Check readiness and invoke guarded delegate | 503 + exact dependency code; delegate 0 | Sanitized transition audit | Exception or stale result treated READY |
| `A1_ImmutableAuditEventBindsExactStateGrantLeaseAttemptTransactionAndKey` | Legal transition/verdict | Trusted bindings / one-field audit mutations | Build/append audit | Exact event and durable receipt required | Hash-linked exact fields | Omit/change before/after version, grant, epoch/fence, transaction/key |
| `A1_AuditAppendFailureCannotReturnProtectedSuccessOrVerdict` | Calculated transition/verdict | Trusted operation / failing provider | Complete call | `AUDIT_APPEND_FAILED`; no external success/verdict | Append attempt recorded | Null/exception treated as success |
| `A1_ConcurrentDuplicateThroughRawAuthorityHasOneOwnerAndOneDelegate` | Registered, unclaimed idempotency | Trusted coordinated composite provider / same request tasks | Barrier-start concurrent calls | One FIRST_OWNER; others IN_PROGRESS or exact replay; delegate count 1 | Exact decisions | Sequential-only fake; two winners |
| `A1_ChangedPayloadIdempotencyCollisionNeverReusesResult` | Completed request | Trusted stored digest / same key new digest | Call raw authority | `IDEMPOTENCY_PAYLOAD_MISMATCH`; no delegate/state change | Collision denial | Return original result for changed payload |
| `A1_OpaquePageTokenBindsScopeSnapshotPriorDigestExpiryAndLimit` | Server snapshot | Trusted token signer / token mutants | Parse next page token | Valid exact page; mutants `PAGINATION_TOKEN_INVALID` or limit code | No sensitive token log | Offset-only API, wrong tenant/snapshot/hash/expiry/page size |
| `A1_AllForbiddenEvidenceAuditAndReadinessFieldsAreRejectedAndSanitized` | Any | Trusted allowlists / sensitive fields | Parse/verify/audit/readiness | Exact sensitive/audit code; secret absent from output | Sanitized code only | Password/token/key/PAN/bank/payroll value |
| `A1_DecisiveSecurityMutationManifestHasZeroSurvivors` | Built test assembly | Trusted literal mutant manifest / mutation results | Run offline mutation gate | Total > 0; survived = 0; every required gate mapped | Mutation report in checkpoint | Delete/invert each parser,trust,binding,lifecycle,evidence,readiness,audit gate |
| `A1_ReviewedPhaseARangeHasNoWhitespaceOrConflictMarkerError` | A1 HEAD | Frozen source parent / cumulative range | Run exact Git checks | Both exit 0 | Checkpoint records commands/output | Reintroduce checkpoint trailing spaces or conflict marker |

## F-07 reconciliation — checkpoint validation discrepancy

| Required field | Reconciliation |
|---|---|
| Finding ID / severity | F-07 / REQUIRED |
| Independent-review evidence | Checkpoint claims `git diff --check` PASS, while the exact historical source range fails on five trailing-whitespace lines. |
| Exact affected production path | Report path only: `outputs/rev869b_external_controller_phase_a_checkpoint.md:3-7`. Product execution is not involved, but the validation/approval chain is. |
| Root cause | Markdown hard-break spaces were introduced and the claimed command was not run on the exact final committed range, or its nonzero exit/output was not propagated into the checkpoint. |
| Why tests passed | Build/test runners do not inspect Markdown whitespace; no final exact-range Git gate was enforced after checkpoint creation. |
| Consequence | The checkpoint contains false acceptance evidence and future allowlists can miss whitespace/conflict defects. Treating it as cosmetic would weaken evidence integrity. |
| Architecture still valid | YES. |
| Phase A / Phase B / external | Phase A report correction: YES. Phase B: NO. External prerequisite: NO. |
| Exact source behavior required | Correction A1 updates the existing Phase-A checkpoint path, removes trailing whitespace at lines 3-7, records that the historical `514...18a...` command failed with exit 2, and records successful A1 incremental and cumulative diff checks. No historical commit is rewritten. |
| Exact negative tests | Git checks fail if trailing whitespace or conflict markers are introduced; checkpoint assertion must match captured exit code/output. |
| Exact typed result | Process exit code `0` for accepted A1 ranges; any nonzero is a stop. The historical reproduction remains recorded as exit `2`. |
| Smallest files | Existing Phase-A checkpoint only. |
| Objective PASS formula | `historical_claim_corrected ∧ git_diff_check(A1_parent..A1_HEAD)=0 ∧ git_diff_check(51476760...A1_HEAD)=0 ∧ reported_exit_codes=actual_exit_codes` |
| Stop condition | Any Git check is nonzero, any reported path/line is omitted, checkpoint still claims historical PASS, or history rewrite is proposed. |

### Exact contradiction reproduction

Command executed exactly:

```powershell
git diff --check 51476760adcea9ed7babbc04d642e53e371c6941..18a6458cbddf50e8cd45c9f789be2bdd2e859b08
```

Exit code: **2**

Every reported error:

| Path | Line | Category |
|---|---:|---|
| `target-dotnet/outputs/rev869b_external_controller_phase_a_checkpoint.md` | 3 | trailing whitespace |
| same | 4 | trailing whitespace |
| same | 5 | trailing whitespace |
| same | 6 | trailing whitespace |
| same | 7 | trailing whitespace |

The checkpoint's PASS claim is incorrect. The likely validation defect is order-of-operations: a pre-check may have run before the final checkpoint bytes existed, or the final nonzero exit was ignored. No evidence proves which occurred, so the report does not assert a more specific cause.

Required A1 future commands, executed from the A1 commit as `HEAD`, are:

```powershell
git diff --check HEAD^..HEAD
git diff --check 51476760adcea9ed7babbc04d642e53e371c6941..HEAD
```

Both must exit `0` with no output. The second cumulative command proves that the ending Phase-A tree corrects the checkpoint whitespace without rewriting the immutable historical commit.

## Exhaustive Phase-A Correction-A1 allowlist

Maximum file count: **13**. The list is exhaustive. No “related”, “supporting”, generated, project, solution, migration, or additional report file is implicit.

| # | Exact file | Why required / findings | Exact expected change | Explicitly non-authorized neighbors |
|---:|---|---|---|---|
| 1 | `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs` | Raw authority, owner DTOs, lifecycle/export/auth substates, signed bundles, audit/readiness; F-01–F-05 | Close V3 public contracts and exact typed bindings/limits/results | Any new contract file or project file |
| 2 | `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BCompatibilityManifestV1.cs` | Unsupported V1 and exact compatibility; F-01, F-06 | Freeze raw V3 versions/fields; retain explicit protected V1 unsupported | Other manifests/configuration |
| 3 | `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs` | Remove/encapsulate duplicate authority seams; F-02, F-03 | Non-authoritative compatibility helpers; exact composite facets/bindings | Persistence project/schema/migration |
| 4 | `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs` | Exact 26-row lifecycle; F-03 | One complete V3 matrix, strict complement, exact export/auth/audit behavior | Any business-runtime service or database state code |
| 5 | `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs` | Sole raw command ingress and trusted providers; F-01–F-03 | Remove public typed authority, one codec, V3 provider provenance, controller-only delegation | New security/KMS integration project |
| 6 | `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs` | Exact bounds/readiness versions; F-02, F-05 | Add only Phase-A server-owned validated contract settings | `appsettings*`, secrets, deployment files |
| 7 | `src/SESS.NexaERP.ControlPlane/Program.cs` | DI owner uniqueness/common readiness; F-01, F-02, F-05 | Register only safe Phase-A authorities/guard; remain no operational endpoint | Project file, hosting/deployment configuration |
| 8 | `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs` | Shared readiness semantics; F-01, F-05 | Liveness/readiness/version only; exact common guard contract proof | Any POST/PUT/PATCH/DELETE protected route |
| 9 | `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs` | Server-owned reader/oracle/evidence bounds/readiness; F-04, F-05 | Exact validated versions, identities and finite stricter limits | `appsettings*`, secrets, external registry files |
| 10 | `src/SESS.NexaERP.AcceptanceVerifier/Program.cs` | One verifier owner and common V3 readiness; F-01, F-04, F-05 | Register raw authority/guard interfaces only; no operational endpoint | Project/hosting/deployment files |
| 11 | `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs` | Raw verifier and signed authoritative facts; F-01, F-02, F-04, F-05 | Make compatibility paths non-authoritative; strict V3 raw verifier, oracle/audit closure | New reader/oracle/audit implementation projects |
| 12 | `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs` | Independent tests/mutations; all findings | Replace production-derived/permissive tests and add exact matrix above | Test project file or any second test file |
| 13 | `outputs/rev869b_external_controller_phase_a_checkpoint.md` | Correct false evidence and serve as the one A1 checkpoint; F-07 and all | Remove whitespace, disclose historical failure, record A1 evidence/mutation results and stop state | No additional checkpoint/report file |

Explicit global exclusions from A1:

- `SESS.NexaERP.slnx` and every `.csproj`;
- every migration, snapshot, SQL file, script, helper, installer, archive, configuration, secret, credential, certificate, or key;
- all `SESS.NexaERP.Api`, Application, Domain, Infrastructure, and `tests/SESS.NexaERP.Tests` files;
- all PostgreSQL fixtures/scenarios including the active IDE file `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`;
- all existing independent-review/failure-reconciliation reports, including this file;
- `../legacy-reference/` and all sibling/out-of-workspace paths.

## Exact Correction-A1 validation commands

Run only after separate management authorization and implementation of the exact allowlist. Restore/download is not authorized; use the existing restored dependency graph.

```powershell
dotnet build .\tests\SESS.NexaERP.ControlPlane.Tests\SESS.NexaERP.ControlPlane.Tests.csproj --no-restore -warnaserror
dotnet test .\tests\SESS.NexaERP.ControlPlane.Tests\SESS.NexaERP.ControlPlane.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~ArchitectureFreezeContractTests" --logger "console;verbosity=minimal"
dotnet test .\tests\SESS.NexaERP.Tests\SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Rev869B&FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test .\tests\SESS.NexaERP.Tests\SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
git diff --check HEAD^..HEAD
git diff --check 51476760adcea9ed7babbc04d642e53e371c6941..HEAD
git status --short -- .
```

Additional mandatory offline acceptance:

1. all required A1 named tests above pass with zero skipped;
2. every original applicable regression passes;
3. the checkpoint contains the exact assurance-owned 26-row/14-owner/test/mutant artifact hashes or literal inventories;
4. a deterministic mutation run reports a positive decisive-mutant count and zero decisive survivors;
5. no PostgreSQL-labelled test is executed;
6. added-source scans report zero hard-coded credentials, sensitive logging, database connections/actions, process/network clients, and protected mutation endpoints;
7. the A1 commit and cumulative Phase-A range both pass `git diff --check` with no output.

## Correction-A1 commit requirements

- Start only from the committed reconciliation HEAD approved by management; record its exact hash before editing.
- Preserve history; no reset, amend, rebase, stash, checkout, or rewrite.
- One source-correction commit only, containing exactly the changed subset of the 13 allowlisted paths and no others.
- The checkpoint must be present in that same commit and be the sole report changed by A1.
- Target-scoped status must be clean after commit.
- Commit message must identify REV869B Phase-A Correction A1 without claiming Phase B, PostgreSQL, deployment, or production readiness.
- Stop immediately after the commit and request a fresh independent source-only review of the exact A1 commit.
- A1 is not PASS until that independent review returns PASS. Management approval and implementation self-checks are not substitutes.

## External prerequisites and enterprise-scale impact

Correction A1 has no external prerequisite. It uses deterministic offline keys/providers only as test fixtures and makes no production cryptographic or durability claim.

The following remain external or later-phase requirements and are not blockers to validating Phase-A contracts:

| Requirement | Classification after A1 |
|---|---|
| Durable atomic nonce/idempotency/authorization/lease/lifecycle/attempt/outbox/audit persistence | PHASE_B_ITEM; not implemented or claimed |
| Real workload identity, signed trust bundles, KMS/HSM, rotation/revocation and trusted time | LATER_PHASE_EXTERNAL_PREREQUISITE |
| Least-privilege signed control/target/audit readers and WORM evidence | LATER_PHASE_EXTERNAL_PREREQUISITE |
| PostgreSQL uniqueness/concurrency/rollback/restart/PITR behavior | NOT_AUTHORIZED_NOT_RUN |
| 300,000 users/customers/vendors, 10,000,000 items, 100,000 machines/projects, >1,000 employees | Benchmark/query-plan/capacity prerequisite; unproven |
| Two-company isolation and approved shared masters | Later integration/database evidence; unproven |
| Ten-year retention, WORM chain, backup/restore and DR | Phase B/later operations evidence; unproven |

A1 must retain finite contract bounds, company/database/resource scope, signed opaque page-token binding, cancellation, and retry caps. Its paging tests prove token contract behavior only, not 10-million-row throughput or memory. No benchmark or production readiness may be inferred.

## Explicit prohibited actions

This reconciliation does not authorize:

- any Phase-A source or test correction now;
- Phase B, Correction 2, Correction 29, schema, migration, PostgreSQL, or database work;
- restore, package download, provisioning, deployment, production access, network call, real key/credential use, or helper execution;
- lifecycle, quarantine, recovery, drop, purge, or export execution;
- changes outside the future 13-file A1 allowlist;
- access to `../legacy-reference/`;
- architecture reopening, because no contradiction requiring it was found.

## Single next management gate

Management must decide only whether to authorize **one REV869B Option-A Phase-A Correction A1** using the exact 13-file allowlist, required tests, mutation gate, Git formulas, exclusions, one-commit rule, and mandatory independent-review stop in this report.

Until that explicit approval, `phase_a_management_acceptance_state=NOT_APPROVED`, Phase-A source safety remains FAIL, Phase B and Correction 2 remain NO_GO, PostgreSQL remains NOT_AUTHORIZED_NOT_RUN, and production remains NOT_READY.
