# REV869B Option-A Phase-A A5 boundary and immutable plan-contract decision

Date: 2026-08-21

Decision type: report-only architecture and boundary decision; no implementation authority

Starting HEAD: `86cf8e09677fe81923296a808e7ed31b70f0f323`

Expected parent: `cce7cc01dd0606b3e4c628ad6cb737eeec56e76a`

## 1. Decision

`A5_REVISED_PHASE_A_SOURCE_ONLY_GATE=GO`

A bounded source-only Phase-A implementation is possible. The missing immutable-plan responsibility is closed by a fixed, compile-time Purchase action contract with 19 allowed action IDs and exactly one server-owned handler mapping for each action. The plan carries typed canonical parameters and trusted business-actor provenance; it never carries a .NET type, method, SQL, script, route, plugin, arbitrary handler name or caller-selected executable input.

The existing Purchase domain, application contracts and 19 Purchase operation implementations remain immutable and continue to own business validation, organization/record-scope authorization, optimistic version checks, business history, command authorization and normal Purchase audit. One shared Infrastructure service-base file may gain an internal, capability-bound ambient-transaction enlistment mode so the A5 target provider can own the single target transaction. Normal public Purchase requests continue to use the existing service-owned transaction mode.

This decision authorizes no implementation automatically. It does not authorize PostgreSQL, migration execution, infrastructure, provisioning, deployment, service start, production use, Phase B or Correction 2.

## 2. Authoritative input and entry verification

The following required reports were read completely before this decision:

| Artifact | Lines | SHA-256 |
|---|---:|---|
| `outputs/rev869b_external_controller_phase_a_a4_lease_atomic_boundary_architecture_freeze.md` | 425 | `2DBC7293840F6BC2613EB3A3D473D28D848E7A8364F3BBA8361BAEF7C37A56C5` |
| `outputs/rev869b_external_controller_phase_a_a4_failure_reconciliation.md` | 309 | `1AD5CEF72BDD3292FFEA244FAA77652A32B6501EFF42059DC729BE6E99075A78` |
| `outputs/rev869b_external_controller_phase_a_a5_checkpoint.md` | 61 | `874C0643697D2C80089568F604A4E88A5B8E410AD063C9A10332FFF0071D1747` |

Entry HEAD, parent and subject matched exactly, and the target-scoped worktree was clean. The A5 blocker commit contains exactly its checkpoint. `../legacy-reference/` was not enumerated, opened, read, changed or used.

Read-only source inspection established that `IRev869BPurchaseService` has 19 fixed methods; every current method uses the same `EfRev869BPurchaseService.BeginTransactionScopeAsync` boundary; the shared base currently rejects `db.Database.CurrentTransaction`; and the existing methods already perform role, organization, record-scope, version, idempotency, history and audit work. This is a transaction-composition gap, not a need to duplicate Purchase business logic.

## 3. Fixed immutable business-action contract

### 3.1 Contract identity

- Contract ID: `sess.rev869b.purchase-action`
- Contract version: `1`
- Schema ID: `sess.rev869b.purchase-action/v1`
- Canonicalization: existing `CanonicalJsonV1`, UTF-8, normalized fixed DTO property names, ordinal property ordering, arrays retained in declared business order except explicitly set-valued identifiers are sorted ordinally, UTC timestamps in round-trip `O` form, dates as `yyyy-MM-dd`, GUIDs lowercase `D`, decimal JSON numbers without exponent or locale formatting, uppercase normalized document/currency/state codes, and no insignificant whitespace.
- Schema manifest encoding: the 20 literal manifest rows in section 3.3 joined by one LF, UTF-8 without BOM and without a trailing LF.
- Schema manifest size: 20 lines, 2,668 bytes.
- Schema manifest SHA-256: `EDAF648EFF4BD77158EF3A18A780D7B0DAD634FFB90CDBA8564A27D4DCFC95CB`.

The contract stores and signs `ActionId`, `ActionVersion`, `ParameterSchemaId`, `ParameterSchemaSha256`, canonical parameter bytes SHA-256, handler descriptor identity/version/artifact SHA-256, and the complete bindings below. Unknown versions or hashes fail before any target database call.

### 3.2 Required plan bindings

One `A4PurchaseBusinessActionV1` is embedded in the management-approved immutable `A4ExecutionPlanBindingV1`. It contains exactly:

1. action ID and version;
2. schema ID and manifest SHA-256;
3. canonical parameters and their SHA-256;
4. organization ID and exact target database identity;
5. resource type, normalized resource key, expected record version and any explicitly listed parent/source versions;
6. management plan ID/version/hash and evidence-manifest hash;
7. business actor issuer, subject, expected employee ID, approved role, organization, identity-resolution policy version and authorization-decision ID/SHA-256;
8. management authorizer workload identity, policy row/version/artifact SHA-256 and authorization request ID/SHA-256;
9. exact executor workload identity and target-provider descriptor;
10. grant ID/version/not-before/expiry/revocation state;
11. action idempotency identity derived by the controller from organization, target, action, resource, plan, grant and canonical parameter digest.

Canonical parameters never contain an idempotency key, actor chosen by the caller, organization override, connection identity, handler name or executable content. The target adapter derives the existing Purchase request idempotency key from the signed A4 execution identity and canonical digest.

### 3.3 Exact action manifest and Purchase mapping

The first manifest row is `schema=sess.rev869b.purchase-action/v1`. The remaining literal rows, in this order, are:

| Fixed action ID/version | Canonical parameter row after `|` | Sole Purchase mapping |
|---|---|---|
| `purchase.rfq.create@1` | `quoteDueAt:utc,currencyCode:code,isSingleSource:bool,singleSourceJustification:string?,lines:[handoffId:uuid,quantity:decimal]` | `CreateRfqAsync` |
| `purchase.rfq.invite-vendor@1` | `rfqNumber:code,vendorId:uuid,remarks:string,expectedVersion:uint` | `InviteVendorAsync` |
| `purchase.quotation.submit-revision@1` | `invitationId:uuid,vendorQuoteReference:string,currencyCode:code,paymentTerms:string,deliveryTerms:string,warrantyTerms:string,requestLateAuthorization:bool,lateAuthorizationRemarks:string?,submissionSource:code,receivedAt:utc,attachmentObjectKey:string,attachmentSha256:sha256,vendorAttestation:string,expectedInvitationVersion:uint,previousQuotationVersion:uint?,headerDiscountValue:decimal,lines:[rfqLineId:uuid,quantity:decimal,unitRate:decimal,discountValue:decimal,packingForwarding:decimal,freight:decimal,insurance:decimal,otherCharges:decimal,promisedDeliveryDate:date,hsnSacCode:code,supplierStateCode:code,placeOfSupplyStateCode:code,vendorRegistrationType:code,roundOff:decimal]` | `SubmitQuotationRevisionAsync` |
| `purchase.quotation.verify-technical@1` | `quotationNumber:code,vendorQuotationLineId:uuid,isCompliant:bool,complianceEvidenceJson:canonical-json,remarks:string,expectedVersion:uint` | `VerifyTechnicalAsync` |
| `purchase.comparison.create@1` | `rfqNumber:code,expectedRfqVersion:uint` | `CreateComparisonAsync` |
| `purchase.comparison.recommend@1` | `comparisonNumber:code,vendorQuotationId:uuid,recommendationRemarks:string,singleSourceJustification:string?,expectedVersion:uint` | `RecommendAsync` |
| `purchase.comparison.approve@1` | `comparisonNumber:code,remarks:string,expectedVersion:uint` | `ApproveAsync` |
| `purchase.comparison.reject@1` | `comparisonNumber:code,remarks:string,expectedVersion:uint` | `RejectAsync` |
| `purchase.comparison.request-revision@1` | `comparisonNumber:code,remarks:string,expectedVersion:uint` | `RequestRevisionAsync` |
| `purchase.comparison.resubmit@1` | `comparisonNumber:code,remarks:string,expectedVersion:uint` | `ResubmitAsync` |
| `purchase.po.create@1` | `comparisonNumber:code,expectedComparisonVersion:uint` | `CreatePurchaseOrderAsync` |
| `purchase.po.submit@1` | `poNumber:code,remarks:string,expectedVersion:uint` | `SubmitPurchaseOrderAsync` |
| `purchase.po.issue@1` | `poNumber:code,remarks:string,expectedVersion:uint` | `IssuePurchaseOrderAsync` |
| `purchase.po.amend@1` | `poNumber:code,amendmentReason:string,paymentTerms:string,deliveryTerms:string,warrantyTerms:string,expectedVersion:uint` | `AmendPurchaseOrderAsync` |
| `purchase.po.revise-rejected@1` | `poNumber:code,revisionReason:string,paymentTerms:string,deliveryTerms:string,warrantyTerms:string,rejectedVersion:uint` | `ReviseRejectedPurchaseOrderAsync` |
| `purchase.po.approve@1` | `poNumber:code,remarks:string,expectedPendingVersion:uint,expectedCurrentVersion:uint?` | `ApprovePurchaseOrderAsync` |
| `purchase.po.reject@1` | `poNumber:code,remarks:string,expectedPendingVersion:uint,expectedCurrentVersion:uint?` | `RejectPurchaseOrderAsync` |
| `purchase.po.cancel@1` | `poNumber:code,reason:string,expectedVersion:uint` | `CancelPurchaseOrderAsync` |
| `purchase.material-followup.transition@1` | `handoffId:uuid,toStatus:code,reason:string,expectedVersion:uint` | `TransitionMaterialFollowUpAsync` |

Each adapter constructs exactly the existing request DTO, injecting only its derived A4 idempotency key. No generic dictionary-to-method invocation is allowed. The action registry is an internal sealed ordinal switch/table compiled into `NpgsqlA4TargetExecutionProvider`; cardinality must be exactly 19, with one unique action ID/version and one mapping each. Reflection, dynamic loading, service enumeration, caller-supplied type names, method names, delegates or SQL are forbidden.

### 3.4 Tenant, record and version enforcement

- Organization in the signed plan, grant, job, business actor resolution, target connection identity and loaded Purchase aggregate must be identical.
- The target endpoint authenticates the executor workload before parsing or returning job data. The executor is not the business actor.
- The business actor is re-resolved at server time from signed issuer + subject through the existing employee identity resolver. Exactly one active mapping must match the signed employee ID, organization and approved role.
- Existing Purchase role and record-scope authorization runs again inside the target transaction. A management grant cannot bypass Purchase authorization.
- Resource key/type and every expected version are compared both by the A5 adapter and by the existing Purchase method. Create actions bind their authoritative source records and source versions; resulting record identifiers are server-generated and returned in the receipt.
- Any organization, target, actor, role, resource, parent, source-version or action substitution fails before handler invocation. Any optimistic conflict rolls back the entire target transaction.

### 3.5 Authorization provenance and idempotency

The terminal evidence retains the immutable management authorizer, policy, plan, grant, executor and business actor provenance separately. The management authorizer approves the exact action and actor; the target IAM resolver and Purchase service independently revalidate the actor and scope. Neither identity may overwrite the other.

Target idempotency identity is:

`SHA256(org || target || action-id || action-version || resource-type || resource-key || plan-id || plan-version || grant-id || execution-id)`.

Its payload digest is the signed canonical job digest including the parameter digest, actor authorization digest, lease and fence. Same identity + same digest returns the immutable target result without invoking Purchase. Same identity + any different digest conflicts. The derived Purchase idempotency key is `a4:` plus the lowercase target identity digest; caller parameters cannot set it.

### 3.6 Fencing, transaction and business history

The target provider begins one serializable `NexaErpDbContext` transaction, locks the A4 target fence/idempotency row, validates schema, job signature, grant/lease validity, action/actor bindings and fence, and checks replay before resolving the handler.

For first ownership it creates a capability-bound internal transaction-enlistment context and invokes the fixed Purchase method using the same `NexaErpDbContext`, plan-bound `ICurrentUser`, existing record-scope authorizer and existing supporting services. The shared Purchase base may join only when this unforgeable internal context matches the current transaction and execution digest. It stages its existing command receipts but cannot commit, roll back or dispose the outer transaction. Public Purchase calls see no such context and retain existing service-owned transactions.

Within that one target transaction the following commit together or all roll back:

1. existing Purchase business rows and compare-and-swap version changes;
2. existing `PurchaseTransactionStatusHistory` and command-context receipts;
3. existing normal Purchase audit rows;
4. target fencing watermark and execution idempotency row;
5. immutable A4 target terminal result;
6. A4 target security audit row; and
7. A4 target outbox row.

Direct reimplementation of Purchase mutations in the A5 provider is forbidden. Audit/outbox or terminal-receipt failure rolls back Purchase changes and history. Equal fence is accepted only for exact replay; lower fence or equal fence with a different identity/digest fails before the Purchase method.

### 3.7 Immutable terminal receipt

The signed/read-only terminal receipt contains execution ID and job digest; action ID/version, schema hash and parameter digest; organization, target, resource type/key and before/after versions; plan ID/version/hash; grant ID/version and management authorizer provenance; lease ID/version, controller epoch and fence; executor workload; resolved business actor issuer/subject/employee/role and authorization-decision digest; Purchase result ID/number/status/version and a canonical business-result SHA-256; target transaction ID; Purchase history/command receipt references; normal Purchase audit reference; A4 target audit reference; A4 outbox reference; success/failure code and committed time.

Failure before commit produces no successful terminal receipt. Failure diagnostics cannot disclose parameters or protected business data. The pinned result reader returns exactly one committed receipt and has no write or execution capability.

## 4. Responsibility classification

| Required responsibility | Classification | Exact boundary |
|---|---|---|
| Immutable action union, schema manifest/hash, actor/provenance and terminal-receipt contracts | Phase A | Control-plane contracts only |
| Raw verification, plan creation, committed-provenance reuse and one A4 dispatcher | Phase A | Control-plane authority/security/state machine |
| Concrete control schema, serializable provider, locks, grant/lease/fence/lifecycle/audit/outbox | Phase A | Control-plane source; source schema only |
| Signed target endpoint, fixed 19-action registry, plan-bound actor adapter, target transaction, fence/result/audit/outbox | Phase A | API + Infrastructure source only |
| Internal capability-bound ambient transaction enlistment | Phase A | Shared `EfRev869BPurchaseService.cs` base only; no business-rule change |
| RFQ, quotation, comparison, PO and material-follow-up DTOs/interfaces | Existing immutable Purchase source | No change |
| All 19 Purchase operation partials and their validation/mapping/calculation logic | Existing immutable Purchase source | No change |
| Purchase domain entities/status contracts/calculators and accepted command-context SQL | Existing immutable Purchase source | No change |
| Existing organization, employee identity, role, record-scope, vendor and tax enforcement | Existing immutable Purchase source | Invoked unchanged; cannot be bypassed |
| Existing Purchase history and normal audit generation | Existing immutable Purchase source | Must participate in the outer target transaction unchanged |
| Target A4 raw-SQL migration source and offline deterministic verification | Phase A | New forward raw-SQL migration only; no application |
| Control/target database creation, roles, ACL application, connection endpoints, IAM workload identities, certificates/keys and policy artifacts | External prerequisite | Supplied by separately authorized secure environment; never embedded |
| Applying migrations, provisioning, deploying, starting services and enabling routes | Phase B | Not part of A5 |
| PostgreSQL integration, failover/restore, load/concurrency and operational acceptance | Phase B | Separate gate after source review |
| Production Purchase execution, reconciliation, recovery, purge or export | Phase B/operations | Explicitly prohibited in A5 |
| Correction 2 | Separate future management decision | No overlap with A5 |

Phase A proves source architecture and offline behavior only. It does not claim that unprovisioned schemas, IAM or database transaction semantics have been operationally accepted.

## 5. Exhaustive revised A5 implementation allowlist

One future revised A5 correction may add or modify only these **27 paths**. `NEW` means absent at this decision. The decision report and every prior report/checkpoint remain immutable.

1. `SESS.NexaERP.slnx`
2. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
3. `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj`
4. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
5. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
6. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
7. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
8. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
9. `src/SESS.NexaERP.ControlPlane/Program.cs`
10. `src/SESS.NexaERP.ControlPlane/Persistence/Rev869BA4ControlPlaneSchemaV1.cs` — NEW
11. `src/SESS.NexaERP.ControlPlane/Persistence/NpgsqlA4DurableControlPlanePersistenceProvider.cs` — NEW
12. `src/SESS.NexaERP.ControlPlane/Reconciliation/PinnedA4TargetResultProvider.cs` — NEW
13. `src/SESS.NexaERP.ControlPlane/Reconciliation/A4TerminalResultReconciliationService.cs` — NEW
14. `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj`
15. `src/SESS.NexaERP.Infrastructure/DependencyInjection.cs`
16. `src/SESS.NexaERP.Infrastructure/Persistence/NpgsqlA4TargetExecutionProvider.cs` — NEW
17. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BA4TargetExecutionBoundarySql.cs` — NEW
18. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.cs` — NEW
19. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.Designer.cs` — NEW
20. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
21. `src/SESS.NexaERP.Api/Program.cs`
22. `src/SESS.NexaERP.Api/Endpoints/Rev869BA4TargetExecutionEndpoints.cs` — NEW
23. `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj`
24. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
25. `tests/SESS.NexaERP.ControlPlane.Tests/A4FailureCorrectionContractTests.cs` — NEW
26. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
27. `outputs/rev869b_external_controller_phase_a_a5_implementation_checkpoint.md` — NEW

The ControlPlane test project file is included so production-graph tests can reference the API/Infrastructure graph without source-string substitutes. No new package download is authorized. The accepted REV869B migration/designer, model snapshot, `Rev869BCommandContextSql.cs`, all Purchase application/domain files, all three Purchase operation partials, API public Purchase endpoints, Acceptance Verifier, scripts/helpers and existing checkpoints are immutable.

Any needed 28th path, package, schema object, public interface, business-rule change or snapshot update stops the correction before that edit.

## 6. Exact required tests

The revised correction must contain exactly these **30 literal `A5_` test methods**, invoked together and individually:

1. `A5_RawAuthorizeAcquireBeginTargetReconcileUsesOneCanonicalProductionPath`
2. `A5_EachNewOperationReachesCompositeProviderWithoutLegacyLifecycleRejection`
3. `A5_RawCanonicalIdentityRoleScopeFreshnessAndDigestDenialsHaveZeroDurableCalls`
4. `A5_UnknownNullOrMultiplyPopulatedA4OperationFailsBeforeMutation`
5. `A5_DurableAuthorizeCommitsGrantLifecycleAuditOutboxAndReplayAtomically`
6. `A5_DurableAcquireHasOneSerializableWinnerAndCommitsReservationFenceLeaseAuditOutboxAtomically`
7. `A5_DurableBeginCommitsDispatchLifecycleAuditOutboxAndRetainsReservedGrantAtomically`
8. `A5_NoPublicOrInjectablePartialGrantLeaseFenceLifecycleNonceAuditOrOutboxMutationExists`
9. `A5_TargetCommitIncludesBusinessHistoryWatermarkResultAuditAndOutboxInOneTransaction`
10. `A5_TargetRollbackLeavesEveryTargetRelationAndWatermarkUnchanged`
11. `A5_StaleFenceOrEqualFenceDifferentDigestFailsBeforeActionHandler`
12. `A5_ExpiredNotYetValidOrRevokedGrantCannotAcquireRenewBeginOrCommit`
13. `A5_LeaseAndRenewalCannotOutliveGrantAndReacquisitionRequiresGreaterFenceAndNoResultProof`
14. `A5_AuthoritativeSnapshotReturnsExactlyOneCommittedGrantPlanLeaseDispatchAndLifecycle`
15. `A5_ResourceVersionAdvanceNeverReconstructsPlanAuthorizerAcquisitionIdentityOrDigest`
16. `A5_EveryPlanGrantLeaseFenceAuthorizerExecutorAndPolicySubstitutionFailsBeforeMutation`
17. `A5_ReconciliationReadsOnlyPinnedAuthoritativeTargetResultAndNeverCallsTargetExecution`
18. `A5_MissingDuplicateAmbiguousConflictingOrStaleTargetResultQuarantinesWithoutConsumption`
19. `A5_ReconciliationFirstOwnerAndReplayBothRequireExactReconcilerIdentityRoleAndScope`
20. `A5_ExactTargetAndControlReplayReturnOriginalResultsWithoutBusinessReexecutionOrVersionChange`
21. `A5_HostsResolveOnlyPinnedConcreteProvidersAndExposeOnlyProtectedRawInternalRoutes`
22. `A5_OfflineControlAndTargetSchemaUpDownAclFingerprintBoundaryAndNoConnectEvidenceAreExact`
23. `A5_ActionManifestHasExactSchemaHashAndNineteenUniqueServerOwnedMappings`
24. `A5_CanonicalParametersAreTypedDeterministicAndExcludeCallerAuthorityIdempotencyAndExecutableInput`
25. `A5_UnknownVersionSchemaHashActionOrMultiplyPopulatedPayloadFailsBeforeDatabaseAndHandler`
26. `A5_PlanBoundActorIsReResolvedAndEveryOrganizationRoleResourceAndVersionSubstitutionFailsClosed`
27. `A5_EachActionInvokesExactlyItsExistingPurchaseMethodWithDerivedIdempotencyAndNoDirectBusinessDml`
28. `A5_AmbientPurchaseEnlistmentCannotCommitRollbackEscapeOrExistOutsideExactTargetCapability`
29. `A5_PublicPurchaseOperationsRetainServiceOwnedTransactionsAndExistingAuthorizationBehavior`
30. `A5_TerminalReceiptBindsActionActorBusinessHistoryAuditOutboxResultAndTransactionExactly`

All 23 retained A4 tests, the complete Phase-A non-PostgreSQL control assembly and complete non-PostgreSQL ERP assembly must pass. Offline relational tests may use deterministic command/connection/transaction harnesses; production-graph endpoint tests may not register fake, null or in-memory production providers. No test may connect to PostgreSQL or start a service.

## 7. Exact production mutants

All A4-M01 through A4-M10 and A5-M11 through A5-M30 from the authoritative freeze/reconciliation remain required. Add these ten compiled production mutants:

| ID | Production mutation | Intended killer |
|---|---|---|
| A5-M31 | Swap two action IDs or map one ID to the wrong Purchase method | A5-23/A5-27 |
| A5-M32 | Accept a caller handler/type/method/delegate or reflection-selected action | A5-24/A5-27 |
| A5-M33 | Ignore action version, schema ID, schema hash or handler artifact pin | A5-23/A5-25 |
| A5-M34 | Hash noncanonical parameters or omit one parameter from the digest | A5-24/A5-25 |
| A5-M35 | Permit organization, resource key, parent/source version or expected-version substitution | A5-26 |
| A5-M36 | Trust caller employee/role or use executor as business actor without server re-resolution | A5-26 |
| A5-M37 | Pass a caller idempotency key instead of deriving the A4 target identity | A5-24/A5-27 |
| A5-M38 | Let enlisted Purchase scope commit, roll back or dispose the outer target transaction | A5-09/A5-10/A5-28 |
| A5-M39 | Bypass `IRev869BPurchaseService` with direct business DML in the A5 provider | A5-27/A5-29 |
| A5-M40 | Omit action, actor, history, audit, outbox, result or transaction binding from terminal receipt | A5-30 |

Required arithmetic: `retained_A4=10`, `retained_A5=20`, `new_action_contract=10`, `total=40`, `compiled=40`, `killed=40`, `survived=0`, `invalid=0`. Every mutant must change production code, compile in a disposable copy, be killed by an intended test and be deleted with source equality reverified.

## 8. Acceptance criteria

The revised A5 passes only when all of the following are machine-evidenced in its new checkpoint:

1. exact authorized entry decision commit, parent, subject and clean worktree;
2. changed paths are a subset of the 27-path allowlist and every required production artifact is present; no prior report/checkpoint changes;
3. warning-as-error offline builds of both hosts, both test graphs and `SESS.NexaERP.slnx`: zero warnings/errors;
4. exact A5 tests `30/30`, each individual invocation `1/1`, with zero failures/skips;
5. retained A4 tests `23/23`, complete Phase-A control tests and complete non-PostgreSQL ERP tests all pass;
6. all 19 manifest rows, 2,668 bytes and SHA-256 `EDAF648EFF4BD77158EF3A18A780D7B0DAD634FFB90CDBA8564A27D4DCFC95CB` reproduce identically in two fresh processes;
7. all 19 fixed mappings invoke only their existing Purchase method, with no direct business DML and no caller-selected execution input;
8. deterministic rollback evidence shows zero surviving business/history/fence/result/audit/outbox changes; commit evidence shows all required relations in one transaction;
9. normal public Purchase operations retain existing transaction and authorization behavior;
10. exactly 40 valid production mutants compile and are killed; none survives;
11. control schema and target migration Up/Down SQL bytes/lines/SHA reproduce in two processes, ACL inventory is exact, migration appears once after REV869B under EF `--no-connect`, zero connections open, and model/snapshot parity remains exact;
12. production registrations resolve one pinned concrete owner per responsibility; endpoint authentication binds workload before parsing/processing; no direct table-DML role or alternate route exists;
13. PowerShell AST parse only, source privacy/secret/prohibited-operation scans, `git diff --check`, executable/source hashes and disposable-artifact deletion all pass;
14. observed counters remain: PostgreSQL connections `0`, PostgreSQL tests executed `0`, migration applications `0`, PowerShell scripts executed `0`, services started `0`, network/package downloads `0`; and
15. exactly one source-only correction commit is created, final target-scoped status is clean, then work stops for a fresh independent report-only architecture/security review.

## 9. Stop conditions

Implementation must stop before the first edit, or immediately before the newly discovered requirement, if:

1. entry HEAD is not this exact decision commit or target scope is dirty;
2. any path outside the 27-path allowlist is required;
3. any Purchase application/domain contract, Purchase operation partial, accepted migration/designer, snapshot, command SQL, public Purchase endpoint, Acceptance Verifier, helper or prior report/checkpoint must change;
4. any action cannot map exactly to one existing Purchase method without new business rules;
5. the ambient enlistment seam cannot remain internal, capability-bound and confined to the same `NexaErpDbContext` transaction;
6. any existing Purchase method commits/rolls back/disposes independently while enlisted or cannot stage its command receipts/audit in the outer transaction;
7. actor identity/role/scope cannot be re-resolved and checked before mutation without trusting caller fields or executor identity;
8. action parameters require a generic dictionary, reflection, dynamic code, caller handler/type/method, arbitrary SQL/script or unknown schema;
9. target transaction cannot atomically include business, history, fence, idempotency, receipt, both audits and outbox;
10. a second lifecycle owner, partial durable mutation interface, alternate provider or direct Purchase DML is needed;
11. schema requires a snapshot change, accepted migration rewrite, automatic install or runtime direct-DML privilege outside the stored boundary;
12. host authentication cannot bind executor workload before request parsing or protected result disclosure;
13. a package is not already cached or any network, PostgreSQL, credential, infrastructure, migration application, service or production access becomes necessary;
14. any required test/regression fails or any mutant is invalid, noncompiling or survives; or
15. any secret, private key, password, production identity or protected parameter enters source, arguments, logs or evidence.

A stop permits only one new report-only blocker/failure reconciliation; it does not permit boundary expansion during implementation.

## 10. Prohibited actions and states

This decision modified no source, test, project, migration, helper or checkpoint and ran no build or test. It made no PostgreSQL connection, migration operation, provisioning/deployment, network/package download, service start, production operation, Phase B, Correction 2, recovery, purge or export action.

`phase_a_correction_a5_boundary_decision=GO_AWAITING_EXPLICIT_IMPLEMENTATION_AUTHORIZATION`

`phase_a_correction_a5_implementation_state=NOT_STARTED`

`phase_a_management_acceptance_state=FAIL_PENDING_CORRECTION_AND_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`production_readiness_state=NOT_READY`

## 11. Exact next management gate

Management may authorize one revised `REV869B Option-A Phase-A Correction A5` source-only implementation starting from this decision commit, constrained to the exact 27-path maximum allowlist, fixed 19-action manifest, 30 A5 tests, retained 23 A4 tests, 40 production mutants, acceptance criteria and stop conditions above.

If authorized, implementation creates exactly one source-only correction commit and stops for a fresh independent report-only architecture/security review. No implementation begins automatically from this report.
