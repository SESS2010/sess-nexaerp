# REV869B Option-A Phase-1 Correction 1 failure reconciliation

## Outcome

This report-only reconciliation is **PASS** as a complete reconciliation of the eight independent-review findings. The architecture decision is exactly:

`ARCHITECTURE_FREEZE_REQUIRED`

The source-only Correction 2 gate is **NO_GO**. This is not `EXTERNAL_PREREQUISITE_BLOCKED`, because the source is not yet sufficient: local defects remain and the current source does not define a complete production ownership, persistence, trust, evidence, readiness, or enterprise-scale boundary. It is not `CORRECTION_2_GO`, because those unresolved decisions prevent a closed and implementable file allowlist.

Canonical states:

`phase1_correction1_failure_reconciliation_state=PASS`

`phase1_correction2_source_only_gate=NO_GO`

`architecture_freeze_review_required=YES`

`external_prerequisite_blocking_state=YES`

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`correction_29_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

No source, test, project, migration, helper, provisioning, deployment, database, key, credential, lifecycle, quarantine, recovery, drop, purge, export, Correction 29, or production operation was performed.

## Current lineage and entry gate

- Reconciliation starting HEAD: `d9edba21ba2d34209ee0794f244520bc6dc0b028`.
- Starting HEAD parent: `aec347d34ec277a2ab7fa06c38a292ecfbeea892`.
- Starting HEAD subject: `Independently review REV869B Phase 1 Correction 1`.
- Starting HEAD boundary: exactly one added file, `outputs/rev869b_external_controller_phase1_correction1_independent_source_safety_review.md`.
- Reviewed source commit: `aec347d34ec277a2ab7fa06c38a292ecfbeea892`.
- Reviewed source parent: `5128e45f7938d8269ca0c40dd151f29c57c34882`.
- Reviewed source range: `5128e45f7938d8269ca0c40dd151f29c57c34882..aec347d34ec277a2ab7fa06c38a292ecfbeea892`.
- Independent review: `outputs/rev869b_external_controller_phase1_correction1_independent_source_safety_review.md`.
- Correction checkpoint: `outputs/rev869b_external_controller_phase1_correction1_checkpoint.md`.
- Target-scoped status at entry: clean.
- Repository-root metadata at entry: only `?? ../legacy-reference/`; its contents were not accessed, enumerated, or modified.
- History was preserved; no reset, rebase, amend, stash, or rewrite occurred.

## Classification summary

| ID | Primary classification | Secondary classification | Source-only resolution |
|---|---|---|---|
| F-01 | Local implementation defect | Test/evidence defect; insufficient previous allowlist | Yes for the local API/parser defect |
| F-02 | Missing production interface | Local defect; insufficient previous allowlist; external prerequisite | Partial only |
| F-03 | Contradiction in the selected architecture | Local defect; missing production interface; external prerequisite | No, not before ownership/atomicity freeze |
| F-04 | Missing production interface | Local defect; test/evidence defect; external prerequisite | Semantics yes; durability no |
| F-05 | Local implementation defect | Missing production interface; test/evidence defect; external prerequisite | Data flow yes; provenance/durability no |
| F-06 | Missing production interface | Test/evidence defect; external prerequisite | Composition yes; truthful READY evidence no |
| F-07 | Test/evidence defect | Insufficient previous allowlist | Yes, after independent contracts are frozen |
| F-08 | Contradiction in the selected architecture | Missing production interface; test/evidence defect; external prerequisite | No end-to-end source-only resolution |

“Exact files required” below identifies currently existing files implicated by each finding. It is not a Correction 2 allowlist. New production adapter/artifact files cannot be named exhaustively until the architecture freeze determines their project, owner, persistence boundary, and deployment boundary; that inability is itself part of the NO_GO decision.

## Eight findings reconciled

### F-01 — canonical command ingress and signing ownership

**Exact independent-review statement:** “Public typed-envelope verification bypasses mandatory strict raw canonical parsing; signing accepts arbitrary trust metadata.”

- **Root cause:** `SignedCommandServiceV2` exposes both raw-byte and typed-envelope verification. The typed overload reserializes a materialized header and never proves that the received bytes were canonical. The signer accepts a header containing caller-selectable trust fields rather than receiving a server-owned signing context.
- **Affected production path:** protected command construction, signing, canonical parsing, signature verification, and every lifecycle mutation reached through `SignedCommandServiceV2`.
- **Security consequence:** alternate encodings and parser differentials can evade the byte-level framing invariant; a caller with signing access can select issuer/audience/subject/role/scope/resource metadata not derived from policy.
- **Existing test weakness:** most service tests call the typed overload. The 25-field mutation test proves the codec path but not that it is the only ingress. The fake signature implementation is not independent cryptographic evidence.
- **Required source behavior:** one public protected-command verification method accepting bounded raw header bytes, raw canonical payload bytes or an equally byte-exact payload contract, and signature bytes; strict parse and byte-for-byte regeneration must occur before signature/policy/state work. Typed verification must be private. Signing must accept a trusted server-built context and payload, not an arbitrary header.
- **Required negative tests:** TA-01, TA-02, TA-03, TA-04, and TA-05 below.
- **Required authoritative evidence:** cross-language golden bytes/signatures; production-algorithm verification using an independent implementation; API-surface reflection proving no public typed bypass.
- **Can it be solved source-only:** **Yes** for this local defect, but it cannot independently establish KMS key custody.
- **Exact existing files required:** `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`; `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`; `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`.
- **External dependency:** production signer/KMS/HSM and authenticated transport identity.
- **Objective PASS formula:** `one_public_raw_ingress AND strict_parse_before_verify AND exact_regeneration AND no_public_typed_bypass AND server_owned_signing_context AND 25_of_25_mutations_rejected_with_exact_code AND independent_signature_vector_passes`.
- **Mandatory stop condition:** any public API can verify a materialized protected header, any trust field remains caller-constructed at signing, or any raw mutation reaches nonce/idempotency/state/audit work.

### F-02 — issuer, audience, subject, role, scope, and tenant policy ownership

**Exact independent-review statement:** “Trusted role/scope and exact organization/global-scope authorization are not derived from a server-owned policy matrix.”

- **Root cause:** issuer and resolver descriptors expose sets, while the request selects a member role/scope. There is no exact operation-to-audience-to-subject-class-to-role-to-scope policy or organization/shared-master scope grammar owned by a production authority.
- **Affected production path:** header signing, `ITrustedIssuerRegistry`, `IAuthorizationResolver`, operation authorization, company-ledger isolation, and shared-master access.
- **Security consequence:** role or scope substitution can remain valid within broad sets; cross-company ledger or global-master access may be authorized without an exact resource rule.
- **Existing test weakness:** one request-role case and one Operator path do not enumerate every prohibited identity/operation/audience/scope combination. Expectations come partly from production sets.
- **Required source behavior:** the request must contain no authoritative role grant. A server-owned immutable policy result must derive exactly one subject class, operation role, audience, organization scope, resource class, and shared-master permission from authenticated identity and target binding. Deny ambiguity and multiple matching grants.
- **Required negative tests:** TA-03, TA-04, TA-06, and TB-01 below.
- **Required authoritative evidence:** signed/versioned policy artifact digest; workload-identity claims from the deployment authority; exhaustive independently maintained trust matrix; two-company isolation evidence.
- **Can it be solved source-only:** **Partial**. Contracts and deterministic policy evaluation can be source-defined; trusted identities, policy distribution, and effective IAM/ACL evidence cannot.
- **Exact existing files required:** `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`; `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`; `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`; `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`; `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`; `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`.
- **External dependency:** immutable issuer/key/policy registry, KMS/HSM, workload identity, IAM, mTLS, runtime ACLs, and tenant catalogue.
- **Objective PASS formula:** `authenticated_identity -> exactly_one_policy_row AND request_role_is_never_authority AND exact_audience_operation_role_scope_match AND company_scope_isolation AND explicit_shared_master_exception AND full_negative_matrix_passes`.
- **Mandatory stop condition:** the request selects a trusted role/scope; a policy row is ambiguous; global scope implicitly covers a company ledger; or the effective external identity/ACL matrix is unavailable.

### F-03 — lifecycle authorization and atomic commit ownership

**Exact independent-review statement:** “Lifecycle authorization is under-bound, evidence is Boolean, and the command sequence is not atomic across nonce, lease, idempotency, audit, and lifecycle stores.”

- **Root cause:** authorization state omits the complete command/resource/evidence binding. The service converts evidence requirements to a Boolean and performs irreversible operations through independent store interfaces without one transaction, outbox, or explicitly modeled recovery protocol.
- **Affected production path:** authorization, prepare/execute/verify/recover/drop/purge/export transitions, lease/fence consumption, audit append, idempotency completion, cancellation, and expiry.
- **Security consequence:** authorization can be replayed or substituted; a crash can strand nonce/fence/idempotency state, record an audit for an uncommitted transition, or commit lifecycle state without a replayable result.
- **Existing test weakness:** transition truth is derived from `ListedOperationRules`; lease tests exercise a fake; there is no fault injection after each persistence boundary or restart reconciliation proof.
- **Required source behavior:** authorization must bind issuer, subject, policy digest, organization, cluster, database, resource/version, operation, canonical request/payload digest, evidence manifest/digest, lease/fence, expiry, and one-time state. Management must choose either one durable control-plane transactional owner for nonce/idempotency/lease/lifecycle plus an audit outbox, or an exact persisted saga with compensations and terminal reconciliation states. Mixing uncoordinated stores is prohibited.
- **Required negative tests:** TB-02, TB-03, TB-04, TB-05, TB-06, and TC-01 below.
- **Required authoritative evidence:** approved state/operation/role/evidence/lease matrix independent of code; durable schema/transaction design; crash-point/restart evidence; immutable audit/outbox correlation.
- **Can it be solved source-only:** **No**, because production transaction ownership is unresolved and durability/restart claims require a real store. Deterministic state-machine parts can be corrected offline only after freeze.
- **Exact existing files required:** `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`; `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`; `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`; `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`; `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`.
- **External dependency:** surviving durable control-plane database, transaction/outbox implementation, immutable audit sink, restart coordinator, and provisioned workload identities.
- **Objective PASS formula:** `complete_authorization_binding AND independent_transition_matrix_exact AND one_authoritative_commit_protocol AND each_fault_point_recovers_to_one_terminal_outcome AND fence_consumed_once AND audit_matches_committed_outcome`.
- **Mandatory stop condition:** no single commit owner/saga is approved; any authorization dimension is absent; evidence remains Boolean; or any injected failure produces an ambiguous or unrecoverable outcome.

### F-04 — durable nonce and idempotency decision semantics

**Exact independent-review statement:** “Idempotency reservation states are ignored; completed replay, in-progress duplicate, retryable failure, and crash recovery semantics are not implemented by the service.”

- **Root cause:** `ReserveAsync` returns a stateful outcome, but the service checks only two terminal codes and then continues mutation. The helper decision table is incomplete and unused. There is no production durable adapter.
- **Affected production path:** all protected command retries, lost-response recovery, concurrent duplicates, failure recording, nonce replay, and response replay.
- **Security consequence:** a duplicate can execute twice; a completed replay can mutate again; concurrent requests can race; a crash can leave an indefinite reservation; retryable and terminal failures can be confused.
- **Existing test weakness:** the concurrency test proves only the in-memory fake's row count. The service is not exercised through all states, and no restart or transactional evidence exists.
- **Required source behavior:** exact state machine: first request atomically becomes `IN_PROGRESS`; exact completed replay returns the stored response/audit without mutation; changed digest returns `IDEMPOTENCY_PAYLOAD_MISMATCH`; a live in-progress duplicate returns `IDEMPOTENCY_IN_PROGRESS`; expired orphan ownership is recovered only by an atomic compare-exchange and increments attempt; retryable failure permits the defined takeover; non-retryable failure returns `IDEMPOTENCY_NONRETRYABLE`; nonce replay remains independent and returns `NONCE_REPLAY`. Every downstream failure records or transactionally derives a stable outcome.
- **Required negative tests:** TC-01 through TC-07 below.
- **Required authoritative evidence:** durable unique constraints, isolation level, compare-exchange semantics, response/audit persistence, crash/restart and concurrent-session PostgreSQL evidence.
- **Can it be solved source-only:** decision logic can be specified and unit-tested; **durability and concurrency cannot**.
- **Exact existing files required:** `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`; `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`; `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`; `src/SESS.NexaERP.ControlPlane/Program.cs`; `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`; `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`.
- **External dependency:** durable transactional control store and future PostgreSQL concurrency/restart execution.
- **Objective PASS formula:** `first=one_execution AND completed_replay=stored_result_no_mutation AND changed_digest=IDEMPOTENCY_PAYLOAD_MISMATCH AND concurrent_duplicate=one_owner AND retryable=bounded_takeover AND nonretryable=stable_denial AND nonce_replay=independent_denial AND restart=one_terminal_outcome`.
- **Mandatory stop condition:** the commit owner from F-03 is unresolved; any reservation state falls through to mutation; or database-backed concurrency/restart evidence is absent when durability is claimed.

### F-05 — authoritative evidence provenance and oracle input

**Exact independent-review statement:** “Authoritative facts are read but discarded; caller raw facts reach the oracle, and the V2 evidence envelope is only self-hashed.”

- **Root cause:** the verifier retains authoritative receipts but evaluates the caller envelope. Its payload hash authenticates nothing because the caller can recompute it. Duplicate receipts collapse through set conversion, and the test oracle ignores its input.
- **Affected production path:** acceptance verification, evidence reader calls, oracle evaluation, audit verdicts, and transitions to accepted/failed.
- **Security consequence:** caller-controlled facts can obtain PASS; replayed, duplicate, stale, cross-scope, oversized, or tampered evidence can be misclassified; audit can preserve a verdict not derived from authoritative facts.
- **Existing test weakness:** `FakeClosedOracleV2` always passes; no independent negative oracle vectors, authenticated receipt/fact bundle, duplicate-reader proof, or caller-versus-authoritative conflict case exists.
- **Required source behavior:** construct a new server-owned oracle input exclusively from authenticated reader results. Each reader result must cryptographically bind reader/version/artifact, binding dimensions, observation ID/time/stage, canonical facts digest, and response bounds. Reject duplicates before set conversion, reject caller disposition/expected/formula fields with a typed error, and apply size/privacy checks before full materialization. Audit the exact authoritative input digest and oracle artifact digest.
- **Required negative tests:** TD-01 through TD-06 below.
- **Required authoritative evidence:** independently built oracle artifact/hash; signed reader artifacts and response vectors; immutable evidence/audit receipts; deterministic PASS and FAIL fixtures whose expected results are calculated outside production code.
- **Can it be solved source-only:** the discarded-facts defect and duplicate checks are source-local; **authentic provenance, deployment identity, durability, and independent oracle artifact are external**.
- **Exact existing files required:** `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`; `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`; `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`; `src/SESS.NexaERP.AcceptanceVerifier/Program.cs`; `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`.
- **External dependency:** deployed authoritative readers, reader identities/keys, pinned oracle build, immutable audit/evidence storage, and target database identity.
- **Objective PASS formula:** `oracle_input == authenticated_authoritative_facts_only AND caller_facts_never_influence_verdict AND every_receipt_unique_and_verified AND all_binding_time_size_privacy_rules_pass AND independent_positive_negative_vectors_match AND audit_digest_matches_exact_input`.
- **Mandatory stop condition:** caller facts or caller PASS metadata reaches the oracle; a receipt/fact bundle is self-authenticated only; duplicate/stale/out-of-scope evidence survives; or independent oracle/reader artifacts are unavailable.

### F-06 — real conjunctive readiness and protected endpoint gating

**Exact independent-review statement:** “Readiness is hardcoded unavailable rather than calculated dependency-by-dependency; endpoint non-execution is not tested.”

- **Root cause:** static probes return NOT_READY without querying production dependencies; applications lack the production dependency registrations and protected routes needed to prove gating.
- **Affected production path:** Control Plane `/health/ready`, Acceptance Verifier `/health/ready`, process startup, dependency registration, and every future protected endpoint.
- **Security consequence:** a future configuration change can report READY without evidence, or a protected route can execute despite an unavailable trust/durable/audit dependency.
- **Existing test weakness:** a Boolean fake is tested directly; actual HTTP routes and one-missing-dependency rows are not exercised; zero protected execution is not asserted.
- **Required source behavior:** readiness is the conjunction of issuer registry, key registry, algorithm/version policy, audience/operation policy, durable nonce/idempotency, server clock/freshness policy, oracle version/hash, authoritative readers, cluster/database identity, durable audit, runtime identity/ACL, and the F-03 commit owner. Each dependency returns a sanitized stable code. Any missing/invalid/stale dependency yields HTTP 503 and a middleware/endpoint guard prevents handler invocation.
- **Required negative tests:** TE-01, TE-02, and TE-03 below.
- **Required authoritative evidence:** deployment health/identity attestations, durable-store probes without mutation, oracle/reader artifact digests, audit capability evidence, and HTTP integration traces.
- **Can it be solved source-only:** the aggregator and route guard can; **truthful external dependency health cannot**.
- **Exact existing files required:** `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`; `src/SESS.NexaERP.ControlPlane/Program.cs`; `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`; `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`; `src/SESS.NexaERP.AcceptanceVerifier/Program.cs`; `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`; `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`.
- **External dependency:** every listed production dependency and deployment identity/ACL configuration.
- **Objective PASS formula:** `READY iff all_required_dependencies_valid AND each_single_failure=HTTP_503 AND protected_handler_invocations=0_when_not_ready AND dependency_codes_sanitized AND both_apps_same_policy_version`.
- **Mandatory stop condition:** any dependency is represented by a constant/fake in production, readiness can pass with one missing row, or a protected handler executes while NOT_READY.

### F-07 — independent and mutation-sensitive acceptance tests

**Exact independent-review statement:** “Several objective tests are circular or test permissive fakes instead of real production paths, yielding non-probative PASS results.”

- **Root cause:** expected transition rows are read from production; fake stores/oracles/readiness implement the property under test; declared cryptography, concurrency, routing, and persistence boundaries are bypassed.
- **Affected production path:** acceptance authority for all six blocker areas.
- **Security consequence:** weakened or removed controls can leave the suite green and falsely authorize unsafe source.
- **Existing test weakness:** circular transition matrix, unconditional PASS oracle, direct fake concurrency, Boolean readiness, typed-envelope service calls, fake offset paging, and no mutation-kill evidence.
- **Required source behavior:** tests must use an independently authored frozen expectation table, call the public production ingress/HTTP paths, use deterministic independent crypto/oracle vectors, assert exact typed outcomes and no-change state, and demonstrate that mutations of decisive controls are killed.
- **Required negative tests:** all TA–TF tests below plus mutation operators removing each signed field, parser ordering, role derivation, state/lease/evidence checks, idempotency branching, audit gating, readiness conjunction, and paging-token binding.
- **Required authoritative evidence:** checked-in independent vectors/specification digest, mutation report with every listed decisive mutant killed, and future database concurrency evidence separated from offline tests.
- **Can it be solved source-only:** **Yes** for offline test quality after architecture contracts are frozen; database/deployment evidence remains external.
- **Exact existing files required:** `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`. Production files may be exercised but are not authorized by this report.
- **External dependency:** future PostgreSQL test environment, production-algorithm test keys, and independently built oracle/policy artifacts.
- **Objective PASS formula:** `independent_expected_data AND real_public_paths AND exact_typed_results AND exact_state_and_audit_assertions AND all_decisive_mutants_killed AND offline_external_evidence_separated`.
- **Mandatory stop condition:** any expected result is derived from the implementation under test, any permissive fake decides PASS, a generic exception assertion remains, or a decisive mutant survives.

### F-08 — enterprise bounds, pagination, retries, and tenant isolation

**Exact independent-review statement:** “Enterprise bounds, paging, retries, tenant-ledger isolation, and bounded evidence handling are not implemented/proven end to end.”

- **Root cause:** configuration declarations are not consistently enforced at raw ingress; evidence is materialized before all limits; paging is a fake offset loop; retry/backpressure and two-company/shared-master ownership are not production contracts.
- **Affected production path:** command ingress, evidence ingestion/readers, master-data traversal, retries, tenant scoping, and readiness/scale claims.
- **Security consequence:** memory/CPU exhaustion, replay amplification, inconsistent page reads, token substitution, cross-ledger access, and false enterprise-capacity claims.
- **Existing test weakness:** the ten-million-row case never exercises production storage, signed continuation tokens, wrong-token binding, cancellation, backpressure, retry caps, or multi-company isolation.
- **Required source behavior:** reject oversized raw input before deserialization; stream or incrementally bound evidence; use signed opaque continuation tokens bound to issuer/subject/organization/resource/query/snapshot/page-size/expiry; cap retries and total work; propagate cancellation/backpressure; enforce company-ledger scope and explicit shared-master grants at query and policy layers.
- **Required negative tests:** TF-01 through TF-05 below.
- **Required authoritative evidence:** query plans and measured allocation/latency for 300,000 parties, 10,000,000 items, and 100,000 machines/projects; two-company isolation tests; token key custody; load/chaos and cancellation traces.
- **Can it be solved source-only:** **No end to end**. Contracts and bounded parsers can be source-defined, while persistence/query/runtime/tenant evidence requires an approved production architecture and provisioned infrastructure.
- **Exact existing files required:** `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`; `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`; `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`; `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`; `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`; `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`.
- **External dependency:** production data store/query layer, tenant catalogue, pagination signing key, performance environment, and load/chaos tooling.
- **Objective PASS formula:** `raw_bounds_before_materialization AND peak_memory_bounded AND opaque_token_all_dimensions_bound AND retries_and_total_work_capped AND cancellation_propagates AND two_ledgers_isolated AND shared_masters_explicit AND stated_scale_measurements_pass`.
- **Mandatory stop condition:** an unbounded/full collection is materialized, offset-only paging is used, a token is unsigned/unbound, retries are uncapped, tenant scope is implicit, or scale evidence is absent when compatibility is claimed.

## Six-blocker cross-reference

| Original blocker area | Reconciled findings | Current state | Required resolution |
|---|---|---|---|
| 1. Signed-envelope metadata and freshness | F-01, F-02, F-07, F-08 | FAIL | Single raw ingress, trusted signing context, exact freshness/size policy, independent vectors |
| 2. Issuer/audience/subject/role trust | F-01, F-02, F-07 | FAIL | Server-owned exact trust matrix plus external identity/policy authority |
| 3. Lifecycle/operation/lease/fencing | F-03, F-04, F-07 | FAIL | Fully bound authorization and approved atomic commit/recovery owner |
| 4. Durable nonce/idempotency | F-03, F-04, F-06, F-07 | FAIL | Exact decision table, durable transaction semantics, restart/concurrency evidence |
| 5. Oracle pinning/authoritative evidence | F-05, F-06, F-07, F-08 | FAIL | Authoritative-only oracle input, authenticated reader facts, independent oracle evidence |
| 6. Fail-closed readiness | F-02, F-03, F-04, F-05, F-06, F-07 | FAIL | Real conjunctive probes, guarded routes, one-missing-dependency HTTP tests |

## Architecture freeze decisions required

Management must approve one coherent answer for every item before a Correction 2 allowlist can be drafted:

1. **Command trust owner:** name the component that alone constructs signing context, owns raw canonical ingress, and maps authenticated identity to a policy version.
2. **Policy and tenant owner:** freeze the exact issuer/key/audience/operation/role/scope matrix, organization scope grammar, two-ledger isolation rule, and shared-master exception rule.
3. **Atomic commit owner:** select either one surviving control-plane transactional store with an audit outbox, or one fully specified durable saga. Name the owner of nonce, idempotency, lease/fence, lifecycle, authorization, response, and audit correlation state.
4. **Idempotency recovery rule:** freeze reservation ownership, expiry, takeover, retry caps, stored response semantics, terminal failures, and crash reconciliation.
5. **Evidence authority:** name the reader identity/key authority, canonical authoritative fact bundle, oracle build/signing owner, and immutable audit/evidence store. Caller facts must have no verdict authority.
6. **Deployment/readiness owner:** define the actual Control Plane and Acceptance Verifier deployments, protected routes, workload identities, ACLs, dependency probes, and common readiness-policy version.
7. **Independent acceptance authority:** designate the owner of frozen expected matrices, cryptographic/oracle vectors, mutation criteria, and later database evidence.
8. **Enterprise data boundary:** freeze pagination token ownership, query snapshot semantics, tenant enforcement layer, bounded-ingestion model, retry/backpressure policy, and measurable scale thresholds.

The selected Option-A concept—external provisioning, a dedicated lifecycle controller, a surviving control-plane database, and target-local ledgers—can remain a candidate. It is not sufficiently frozen to decide whether the control transaction resides wholly in the surviving database, spans target-local stores, or uses a saga. That ambiguity directly contradicts a source-only claim of atomic command behavior.

## Required typed outcomes

The freeze must approve these exact stable outcomes before source work. Existing codes are retained where adequate; new codes are explicitly marked.

| Condition | Required typed outcome |
|---|---|
| Unsupported contract/canonicalization/algorithm | `CONTRACT_UNSUPPORTED`, `CANONICALIZATION_UNSUPPORTED`, `ALGORITHM_UNSUPPORTED` |
| Malformed/noncanonical raw framing | `CANONICAL_HEADER_MALFORMED` (new) |
| Unknown/revoked/mismatched issuer or key | `ISSUER_UNKNOWN`, `KEY_UNKNOWN`, `KEY_REVOKED`, `ISSUER_KEY_MISMATCH` |
| Wrong audience/subject/request-selected role/scope | `AUDIENCE_MISMATCH`, `SUBJECT_UNAUTHORIZED`, `REQUEST_ROLE_FORBIDDEN`, `SCOPE_MISMATCH` (new) |
| Invalid signature/payload | `SIGNATURE_INVALID`, `PAYLOAD_HASH_MISMATCH` |
| Future/stale request | `NOT_YET_VALID`, `ENVELOPE_EXPIRED` |
| Nonce replay | `NONCE_REPLAY` |
| Wrong organization/cluster/instance/operation/resource version | `ORGANIZATION_MISMATCH`, `CLUSTER_MISMATCH`, `INSTANCE_MISMATCH`, `OPERATION_MISMATCH`, `RESOURCE_VERSION_STALE` |
| Missing/expired/stale lease or fence | `LEASE_REQUIRED`, `LEASE_EXPIRED`, `LEASE_FENCE_STALE` |
| Illegal transition or substituted authorization | `STATE_TRANSITION_ILLEGAL`, `AUTHORIZATION_BINDING_MISMATCH` (new) |
| Idempotency changed payload/live duplicate/non-retryable | `IDEMPOTENCY_PAYLOAD_MISMATCH`, `IDEMPOTENCY_IN_PROGRESS` (new), `IDEMPOTENCY_NONRETRYABLE` |
| Oracle mismatch | `ORACLE_MISMATCH` |
| Missing/duplicate/unauthorized/tampered reader evidence | `READER_MISSING`, `READER_DUPLICATE` (new), `READER_UNAUTHORIZED`, `EVIDENCE_TAMPERED` (new) |
| Caller verdict/unknown evidence property | `EVIDENCE_UNMAPPED_FIELD` (new) |
| Oversized/private evidence | `EVIDENCE_TOO_LARGE`, `EVIDENCE_SENSITIVE_FIELD` |
| Missing audit/dependency | `AUDIT_APPEND_FAILED`, `SERVICE_NOT_READY` |
| Invalid/expired/substituted page token | `PAGINATION_TOKEN_INVALID` (new) |

## Test acceptance design

These tests are requirements for a future architecture-approved implementation, not authorization to create them now. “No change” means no lifecycle mutation, fence consumption, authoritative attempt increment, success audit, or idempotency completion unless the row explicitly says otherwise.

### Command trust, lifecycle, and idempotency tests

| ID / unique name | Initial state and trusted configuration | Input / mutation or action | Independently derived expected result | State/audit/idempotency evidence | Mode / cleanup |
|---|---|---|---|---|---|
| TA-01 `RawIngressRejectsEachProtectedFieldMutation` | Registered resource v1; frozen 25-field vector and independent public key | For each of 25 fields, mutate raw bytes without resigning | Exact field-code mapping above; otherwise `SIGNATURE_INVALID`; never generic exception | No change; nonce/idempotency/audit calls = 0 | Offline; none |
| TA-02 `RawIngressRejectsCanonicalOrderAndFramingMutations` | Same vector; one public raw API | Reorder, duplicate, omit, CRLF, invalid UTF-8, wrong byte length, leading-zero integer, or append a field | `CANONICAL_HEADER_MALFORMED` for every row | No change; downstream calls = 0 | Offline; none |
| TA-03 `PublicApiHasNoTypedEnvelopeBypassAndSignerRejectsCallerTrust` | Reflection over production assembly; trusted signing-context factory | Search public methods; attempt arbitrary role/scope/issuer header signing | No public typed verifier/sign-header method; factory rejects with `REQUEST_ROLE_FORBIDDEN` or `SCOPE_MISMATCH` | No signed artifact; no state/audit/idempotency | Offline; none |
| TA-04 `TrustTupleMatrixRejectsEveryWrongDimension` | One independent approved tuple | Mutate issuer, audience, subject, key, algorithm, contract version, canonical version, role, or scope separately | Corresponding exact code from the typed-outcome table | No change; denial audit only if frozen policy requires it | Offline; none |
| TA-05 `FreshnessMatrixRejectsFutureAndExpiredCommands` | Fixed server clock, approved maximum lifetime/skew | Future issued/not-before; expired; excessive lifetime | `NOT_YET_VALID` or `ENVELOPE_EXPIRED` per independent time table | No nonce reservation or mutation; denial audit as frozen | Offline; none |
| TA-06 `TwoCompanyAndSharedMasterScopeMatrixIsExact` | Companies C1/C2, separate ledgers, one explicit global master grant | C1 identity targets C2 ledger; company identity targets global master; approved master reader accesses allowed fields | Denials return `SCOPE_MISMATCH`; only explicit shared-master row succeeds with `NONE` | Denials no change; success read-only audit with policy digest | Offline plus future integration; fixture rows removed |
| TB-01 `EveryProhibitedIdentityOperationPairIsDenied` | Independent frozen role matrix for every operation/audience | Submit every role not listed for each operation | `SUBJECT_UNAUTHORIZED` or `REQUEST_ROLE_FORBIDDEN` exactly as matrix says | No change; denial audit per policy | Offline; none |
| TB-02 `IndependentLifecycleMatrixMatchesExactly` | Independent state/operation/role/evidence/lease table, not production enumeration | Execute every listed row and every complement row | Listed row exact next state/`NONE`; complement `STATE_TRANSITION_ILLEGAL` | Success one committed audit/outcome; denial no mutation | Offline; reset in-memory fixture per row |
| TB-03 `AuthorizationBindingRejectsEverySubstitution` | Active authorization bound to complete approved tuple | Mutate request digest, issuer, subject, org, cluster, DB, resource/version, operation, evidence digest, lease, or fence | `AUTHORIZATION_BINDING_MISMATCH` except existing more-specific identity codes where table requires | Authorization remains ACTIVE/unconsumed; no success audit | Offline; none |
| TB-04 `LeaseExpiryAndFenceMatrixIsExact` | Resource requiring lease; durable lease L2/fence 9 | Missing lease, expired lease, L1, fence 8, wrong holder; then exact L2/9 | `LEASE_REQUIRED`, `LEASE_EXPIRED`, or `LEASE_FENCE_STALE`; exact row `NONE` | Rejected fence unconsumed; success consumes once and commits one audit | Offline and future PostgreSQL; release fixture lease |
| TB-05 `CancellationExpiryAndOperationReuseAreOneTime` | Active bound authorization with fixed expiry | Cancel by wrong authorizer, expire early, consume twice, reuse for another operation | `SUBJECT_UNAUTHORIZED`, `NOT_YET_VALID`, or `AUTHORIZATION_BINDING_MISMATCH` exactly | Original state unchanged for denial; valid cancel/expire one terminal audit | Offline; none |
| TB-06 `EveryCommitBoundaryFaultReconcilesToOneOutcome` | Approved commit protocol; fresh resource and request | Inject fault after nonce, reservation, fence, business state, outbox/audit, and response persistence; restart | Frozen recovery result for each point; never generic failure or second business execution | Exactly one terminal idempotency row, one lifecycle outcome, one correlated audit | Future PostgreSQL; controller-owned cleanup after evidence capture |
| TC-01 `FirstRequestExecutesExactlyOnce` | No nonce/idempotency row; legal state/lease | Submit valid request | `NONE`, one authoritative attempt | One mutation, fence consume, audit, completed response digest | Offline contract plus future PostgreSQL; cleanup fixture |
| TC-02 `CompletedReplayReturnsStoredResultWithoutExecution` | COMPLETED row with stored response/audit; fresh transport nonce under frozen replay rule | Repeat identical idempotency binding | `NONE`; exact stored result | Zero mutation/fence; same audit/response; attempt unchanged | Offline plus future PostgreSQL; cleanup fixture |
| TC-03 `ChangedPayloadWithSameKeyIsConflict` | COMPLETED binding digest A | Submit same key/request with digest B | `IDEMPOTENCY_PAYLOAD_MISMATCH` | No change; stored row unchanged | Offline plus future PostgreSQL; cleanup fixture |
| TC-04 `DuplicateNonceIsIndependent` | Nonce already reserved; no matching completed replay exemption | Submit otherwise valid envelope | `NONCE_REPLAY` | No idempotency/lifecycle/audit success | Offline plus future PostgreSQL; expire fixture nonce |
| TC-05 `ConcurrentDuplicateHasOneBusinessOwner` | No row; two sessions released by barrier | Submit identical command concurrently | One `NONE`; other `IDEMPOTENCY_IN_PROGRESS` or stored completion per frozen race rule | One attempt, mutation, fence consume, success audit and response | Future PostgreSQL; controller-owned cleanup |
| TC-06 `RetryableFailureUsesBoundedAtomicTakeover` | RETRYABLE_FAILURE row, expired owner, attempts below cap | Retry exact binding, then retry above cap | First obtains one new attempt; capped row returns frozen terminal typed code | One owner; attempt increments once; audits correlate | Future PostgreSQL; cleanup fixture |
| TC-07 `NonretryableAndExpiredEnvelopeNeverRetryMutation` | NONRETRYABLE row; separately expired envelope | Retry each | `IDEMPOTENCY_NONRETRYABLE`; `ENVELOPE_EXPIRED` | No change; terminal outcome stable | Offline plus future PostgreSQL; cleanup fixture |

### Evidence, readiness, and scale tests

| ID / unique name | Initial state and trusted configuration | Input / mutation or action | Independently derived expected result | State/audit/idempotency evidence | Mode / cleanup |
|---|---|---|---|---|---|
| TD-01 `OracleRejectsMissingVersionOrArtifactHash` | Independent oracle manifest O1/v1/hash H | Missing O1, wrong version, wrong hash | `ORACLE_MISMATCH` | No verdict/state; denial audit per freeze | Offline; none |
| TD-02 `ReaderSetRejectsMissingDuplicateUnknownAndTamperedRows` | Exact readers R1/R2 with independent signed vectors | Remove R1; duplicate R1; add R3; alter fact/receipt digest | `READER_MISSING`, `READER_DUPLICATE`, `READER_UNAUTHORIZED`, `EVIDENCE_TAMPERED` | No verdict/state; reader/audit calls exactly asserted | Offline; none |
| TD-03 `CallerFactsAndPassCannotInfluenceAuthoritativeVerdict` | Authoritative facts independently yield FAIL | Caller supplies PASS/verdict/expected/formula or conflicting raw facts | Unknown fields `EVIDENCE_UNMAPPED_FIELD`; conflicting facts ignored or `EVIDENCE_TAMPERED`; disposition remains independent FAIL | Audit digest equals authoritative input only; no accept transition | Offline; none |
| TD-04 `EvidenceBindingAndTimeMatrixIsExact` | Approved request/resource/lease and fixed observation window | Mutate org, DB, operation, request, resource/version, lease/fence, observation stage/time | Exact identity/lease code; stale time uses `EVIDENCE_TAMPERED` until a dedicated frozen code exists | No verdict/state; no success audit | Offline; none |
| TD-05 `EvidenceBoundsAndPrivacyFailBeforeMaterialization` | Server maxima and sensitive-field denylist | Exceed envelope/reader/observation/selector/fact/string/cumulative bytes; include each private field | `EVIDENCE_TOO_LARGE` or `EVIDENCE_SENSITIVE_FIELD` | Peak allocation below frozen bound; no raw sensitive value in logs/audit | Offline; none |
| TD-06 `IndependentOraclePositiveAndNegativeVectorsAreExact` | Signed oracle artifact and independently calculated PASS/FAIL vectors | Evaluate unchanged vectors and one decisive fact mutation each | `NONE` with exact PASS/FAIL/reasons; mutation flips/rejects as vector specifies | Audit includes exact authoritative-input and oracle hashes | Offline; none |
| TE-01 `ControlPlaneReadinessFailsForEachDependency` | All Control Plane dependencies healthy | Mark each required dependency missing/invalid/stale one at a time | HTTP 503, `SERVICE_NOT_READY`, exact sanitized dependency code | Protected handler invocation = 0; no mutation/audit success | Offline HTTP integration; dispose host |
| TE-02 `AcceptanceVerifierReadinessFailsForEachDependency` | All verifier dependencies healthy | Mark each oracle/reader/identity/audit/target dependency bad one at a time | HTTP 503, `SERVICE_NOT_READY`, exact sanitized dependency code | Verification handler invocation = 0; no verdict audit | Offline HTTP integration; dispose host |
| TE-03 `ReadinessNeverPassesWithIncorrectAggregate` | One dependency false amid all true; then all true | Exercise both real `/health/ready` routes and guarded protected routes | One false => 503; all true => 200 only with exact common policy version | Zero protected calls when false; readiness audit/metric sanitized | Offline then deployment evidence; dispose host |
| TF-01 `RawCommandAndEvidenceLimitsRejectBeforeDeserialization` | Frozen byte limits | Send limit+1 raw command/evidence and compression/amplification patterns | `EVIDENCE_TOO_LARGE` or frozen command-size code before object creation | No downstream calls; measured allocation within cap | Offline; none |
| TF-02 `OpaqueContinuationTokenBindsEveryDimension` | Snapshot S, page size 1000, signed token key | Mutate issuer, subject, org, resource, query, snapshot, page size, expiry, signature | `PAGINATION_TOKEN_INVALID` for every mutation | No rows returned/audit leakage; valid token advances once | Offline plus integration; delete fixture snapshot |
| TF-03 `CancellationBackpressureAndRetryBudgetAreBounded` | Slow reader, queue/backpressure and retry budget frozen | Cancel mid-page; induce transient failures through and beyond cap | Cancellation typed result per API; cap exhaustion frozen terminal code | No unbounded tasks/buffers; exact attempts; stable audit | Offline plus load environment; drain fixture queue |
| TF-04 `TenMillionItemTraversalIsSnapshotConsistentAndBounded` | 10,000,000-item production-like fixture and snapshot S | Traverse allowed pages while concurrent writer changes later data | Exact snapshot rows, no duplicates/gaps, bounded memory/latency | Page/token audit counts; no full collection | Future performance database; controller-owned teardown |
| TF-05 `TwoLedgersNeverCrossAndSharedMastersRequireGrant` | C1/C2 ledgers plus approved global masters | Cross-company queries/mutations and allowed master reads | `SCOPE_MISMATCH` for cross-ledger; only explicit master grant succeeds | No cross-tenant rows/audits; successful read records policy digest | Future PostgreSQL/IAM; controller-owned teardown |

Mutation acceptance is mandatory: removal of any protected field; changed canonical order; bypass of raw parsing; request-role acceptance; deletion of any issuer/audience/subject/key/algorithm/version/freshness check; nonce/idempotency branch removal; lifecycle/evidence/lease/fence check removal; use of caller facts; duplicate-reader acceptance; size/privacy check removal; audit-gate removal; readiness `AND` changed to `OR`; and page-token binding removal must each make at least one named test fail with the independently specified outcome.

## Source versus external-prerequisite separation

| Source/architecture work required before external verification | External evidence that source cannot fabricate |
|---|---|
| Single raw ingress and trusted signing-context API | KMS/HSM custody, key rotation/revocation, transport identity |
| Exact policy and tenant-scope evaluator | Effective IAM/ACL/mTLS and authoritative tenant catalogue |
| Fully bound authorization record and frozen commit protocol | Durable store isolation, crash/restart, concurrent sessions |
| Complete idempotency decision machine | PostgreSQL uniqueness/locking/recovery evidence |
| Authoritative-only oracle input and strict evidence validation | Signed deployed readers, independently built oracle, immutable evidence store |
| Conjunctive readiness aggregator and route guard | Actual deployment health and workload identity attestations |
| Independent deterministic/mutation-sensitive offline tests | Database, deployment, load, chaos, backup/restore, and DR evidence |
| Bounded ingress/token/retry/tenant contracts | Query plans, scale measurements, token-key custody, two-company isolation |

External prerequisites remain blocking, but they are not the only blockers. Therefore outcome C is not permitted at this gate.

## Enterprise-scale impact

No capacity claim is authorized for 300,000 users/customers/vendors, 10,000,000 items, or 100,000 machines/projects. The freeze must establish measurable maximum raw input, peak allocation, page size, total page work, retry count/time budget, cancellation latency, snapshot lifetime, query latency, and tenant-isolation thresholds. Two company ledgers must be structurally scoped and denied cross-company by both policy and persistence queries; shared master records require explicit read/write grants. Readiness cannot become READY merely because configuration contains limit values.

## Explicit exclusions

This report does not authorize or define a Correction 2 file allowlist. It does not authorize new source/test/project/migration/helper files, PostgreSQL access/tests, migration operations, provisioning, deployment, real keys/credentials, production access, lifecycle/quarantine/recovery/drop/purge/export execution, another independent review, Correction 29, or access to `../legacy-reference/`.

The exact-file lists under F-01 through F-08 are impact records only. They must not be combined into an implementation allowlist. A future allowlist may be produced only after all eight architecture decisions are approved and every new production artifact can be named exhaustively with a single owner.

## Exact GO/NO_GO decision

`phase1_correction2_source_only_gate=NO_GO`

Reason: at least F-03 and F-08 require architecture ownership decisions that cannot be completed by a bounded source-only correction, while F-02, F-04, F-05, and F-06 require both source contracts and external authorities. GO would combine a correction with unresolved contradictions and would repeat the insufficient-allowlist failure.

## Single next management gate

**Approve or reject one report-only Option-A architecture-freeze specification that answers all eight numbered decisions in “Architecture freeze decisions required,” names the single command commit owner and authoritative evidence owner, fixes the tenant/pagination/readiness boundaries, and only then determines whether one exhaustive Correction 2 source allowlist is possible.** Until that approval, Correction 2, PostgreSQL, provisioning, deployment, protected operations, Correction 29, and production remain unauthorized.
