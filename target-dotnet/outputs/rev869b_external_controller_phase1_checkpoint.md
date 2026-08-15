# REV869B external controller Option A — Phase 1 checkpoint

## Decision and scope

- Architecture approval: Option A approved by management.
- Phase: contract and trust skeleton only.
- Authoritative starting commit: `18dea1e66053bb5143668a5634e5be16d4eb6ce3`.
- Authoritative report: `outputs/rev869b_external_controller_architecture_freeze_review.md`.
- Verified report SHA-256: `26AE639332F9D4D46E1D01F444A45242B136FC402AC07A72EE776FB73783EE81`.
- Existing REV869B business API/service behavior: unchanged.
- F23-01: retained.

## Exact 16-file boundary

1. `src/SESS.NexaERP.ControlPlane.Contracts/SESS.NexaERP.ControlPlane.Contracts.csproj`
2. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
3. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BCompatibilityManifestV1.cs`
4. `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj`
5. `src/SESS.NexaERP.ControlPlane/Program.cs`
6. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
7. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
8. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
9. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
10. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
11. `src/SESS.NexaERP.AcceptanceVerifier/SESS.NexaERP.AcceptanceVerifier.csproj`
12. `src/SESS.NexaERP.AcceptanceVerifier/Program.cs`
13. `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`
14. `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj`
15. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
16. `outputs/rev869b_external_controller_phase1_checkpoint.md`

No seventeenth target file was created or modified. The pre-existing untracked sibling `../legacy-reference/` remains outside target scope and was not accessed.

## Ownership and deployment separation

| Component | Production source owner | Runtime identity | Deployment state |
|---|---|---|---|
| Control Plane | SESS | dedicated control-plane runtime | designed, not deployed |
| Acceptance Verifier | SESS | dedicated acceptance-verifier runtime | designed, not deployed |

The projects are independently deployable, independently keyed by contract, and separated from the target ERP process and database. Phase 1 supplies no deployment artifacts or runtime keys.

## Contract inventory

- Version/compatibility manifest: controller contract, evidence schema, deterministic canonicalization and signature-algorithm identifiers.
- Identity and scope: control-plane instance, target ERP instance/environment/database, company, scenario, subcase, observation, evidence-envelope and oracle identity/version/hash.
- Lifecycle: command, exact authorization, lease/version, replay key, signed envelope, attempt, transition, terminal outcome, quarantine, recovery, drop, purge and export authorization.
- Audit/evidence: durable audit reference, evidence request/response, fact-only before/after/durable observations, reader/source provenance, typed selectors, action result, SQLSTATE/error/object/count/state fields, canonical evidence envelope, calculated verification result, exact rejection codes and audit event.
- Enterprise bounds: company/global-master and separate financial/stock-ledger scope, bounded row/page contracts and continuation support.
- Keys: algorithm/version/key ID, injectable signer/verifier, key lifecycle, rotation and revocation contracts.

## Trust-boundary matrix

| Boundary | Permitted Phase-1 data | Fail-closed controls | Explicitly excluded |
|---|---|---|---|
| caller → Control Plane | signed command intent, scope, binding, role and replay key | exact versions, algorithm, key state, signature, age, replay, lease, binding and role | caller verdict/PASS, actual evidence, generic SQL |
| Control Plane → worker | contract shape only | exact operation/target/lease/version contract | worker implementation, queue, provisioning or migration execution |
| evidence reader → verifier | fact-only bounded observations and provenance | stage, uniqueness, source, selector-reader and size validation | oracle-derived actual evidence and caller PASS |
| Control Plane → verifier | canonical evidence and action facts | separate verifier abstraction/key, exact binding, oracle version/hash and signature | shared signer responsibility or controller verdict |
| verifier → audit | calculated disposition and exact rejection list | append-only sink abstraction and durable reference | audit persistence implementation |

No hard-coded key, password, token, credential, real endpoint or connection string is present. Configured endpoints must be HTTPS names in the reserved `.invalid` namespace at this phase.

## State-machine matrix

| From | Legal next states |
|---|---|
| Registered | Preflight, Quarantined |
| Preflight | Provisioning, Failed, Quarantined |
| Provisioning | Ready, Failed, Quarantined |
| Ready | MigrationAuthorized, Quarantined |
| MigrationAuthorized | Migrating, Quarantined |
| Migrating | VerificationPending, Failed, Quarantined |
| VerificationPending | Accepted, Failed, Quarantined |
| Accepted | DropAuthorized, Quarantined |
| Failed | Quarantined |
| Quarantined | RecoveryAuthorized, DropAuthorized |
| RecoveryAuthorized | Recovering |
| Recovering | Ready, Failed, Quarantined |
| DropAuthorized | Dropped |
| Dropped | PurgeAuthorized |
| PurgeAuthorized | Purging |
| Purging | Purged |
| Purged | none |

Every other transition throws the typed `IllegalTransition` rejection.

## Role/ACL matrix

| Production identity/role | Narrow responsibility |
|---|---|
| ControlPlaneRuntime | request verification, quarantine coordination |
| AcceptanceVerifier | calculate and record acceptance/failure |
| AuditWriter | append verification/lifecycle audit events |
| RegistryWriter | register target/control identities |
| ProvisioningExecutor | provisioning/ready/drop completion contract |
| MigrationExecutor | migration-start contract |
| RecoveryExecutor | recovery-start contract |
| PurgeAuthorizer | authorize purge only |
| PurgeExecutor | purge-start/completion contract |
| ExportReader | evidence-export contract |
| MonitoringReader | health/version observation only |
| Operator | bounded preflight/migration/drop authorization |
| RecoveryApprover | authorize recovery only |

There is no administrator/superuser catch-all role. Cross-role substitution is rejected.

## Offline validation record

| Check | Result |
|---|---|
| Phase-1 build | PASS — 0 warnings, 0 errors |
| New Phase-1 adversarial contract/trust tests | PASS — 12/12 |
| Focused existing REV869B non-PostgreSQL tests | PASS — 76/76 |
| Complete existing non-PostgreSQL suite | PASS — 450/450 |
| PowerShell AST parse | PASS — 24/24 scripts |
| EF discovery | PASS — 13 migrations listed with `--no-connect`; no database status queried |
| Migration uniqueness/order | PASS — 13 unique, strictly ordered IDs |
| Current model/snapshot parity without connecting | PASS — 1/1 |
| Secret/privacy/prohibited-operation scan | PASS |
| Exact 16-file boundary | PASS |
| `git diff --check` | PASS |

PostgreSQL tests and all PostgreSQL operations were intentionally not run.

## External prerequisites

The following remain blocking and unprovisioned: independently scaled Control Plane/verifier compute; HA control database; durable queue/outbox/dead-letter infrastructure; KMS/HSM and managed secrets with distinct keys; immutable encrypted ten-year evidence storage/legal hold; private network/DNS/load balancer and deny-by-default firewalling; signed artifact/SBOM delivery and isolated CI; centralized observability, paging, runbooks and 24x7 ownership; secondary-region DR/restore exercises; representative scale/chaos qualification; and a fresh independent source-only architecture/security review.

## Explicitly unimplemented and unauthorized

- No PostgreSQL access, control database, persistence, SQL or database create/drop.
- No provisioning, migration, lifecycle, quarantine, recovery, purge or export execution.
- No worker, queue, outbox, scheduler, network client or target-ERP integration.
- No live signing keys, real signature provider, credential store or key provisioning.
- No deployment, infrastructure-as-code, production endpoint or production access.
- No Correction 29 implementation or source-safety/readiness PASS declaration.

## Required final states

external_controller_architecture_state=APPROVED_OPTION_A
external_controller_phase1_source_state=COMPLETE_PENDING_REVIEW
control_plane_production_ownership_state=SESS_OWNED
acceptance_verifier_production_ownership_state=SESS_OWNED
deployment_separation_state=DESIGNED_NOT_DEPLOYED
real_key_provisioning_state=NOT_STARTED
external_prerequisite_blocking_state=YES
correction_29_source_only_gate=NO_GO
f23_01_state=PASS_RETAINED
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN

Mandatory next gate: a fresh independent source-only architecture/security review of the committed Phase-1 Control Plane and Acceptance Verifier skeleton.
