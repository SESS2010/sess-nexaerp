# REV869B Option-A Phase-A Correction A1 Independent Source-Safety Review

**Review date:** 2026-08-16  
**Review type:** Fresh, independent, source-only, report-only  
**Reviewed commit:** `3858eea9a4e88c58447880f5c9c36e0dfe2420e9`  
**Reviewed parent:** `7051b8fa93b1605e35b98394f21479e820c8f18c`  
**Verdict:** `FAIL`

Correction A1 does not independently close the approved Phase-A architecture and security requirements. Reconciled finding F01 is supported. Findings F02 through F07 remain blocking. Passing builds and tests do not cure the production trust-boundary, lifecycle, readiness, evidence-reader, test-decisiveness, and checkpoint-integrity defects described below.

## 1. Authority, scope, and safety constraints

This review used the supplied management request, the approved architecture-freeze specification, the failure-reconciliation report, the prior independent review, the A1 checkpoint, the incremental Correction-A1 diff, and the cumulative Phase-A implementation.

The review was strictly source-only. It did not execute PostgreSQL tests; connect to a database; use production, external infrastructure, credentials, keys, provisioning, deployment, or lifecycle operations; or modify source, tests, projects, migrations, or helpers. `../legacy-reference/` was not opened, queried, enumerated, or modified. Because that path was explicitly prohibited, its current filesystem contents were not independently inspected and no claim about its current untracked contents is used as review evidence.

## 2. Entry gates and commit boundary

| Gate | Reproduced evidence | Result |
|---|---|---|
| HEAD | `3858eea9a4e88c58447880f5c9c36e0dfe2420e9` | PASS |
| First parent | `7051b8fa93b1605e35b98394f21479e820c8f18c` | PASS |
| Target-scoped status before review | No output from `git status --short -- .` | PASS |
| A1 commit boundary | 8 changed files; after repository-prefix normalization, 8 expected, 0 unexpected, 0 missing | PASS |
| A1 checkpoint SHA-256 | `BCDD46A541262E8EDCA3E4BB52AC7EA0FDE8584994F1FFCB7157AAB6567A7D53` | PASS |
| Failure-reconciliation SHA-256 | `C310E9F23985AD70AB64B6231DB5FF46199D0AE1321E3A05913DB0D5E6AC4234` | PASS |
| Architecture-freeze SHA-256 | `3F0BC461865D69E3D9827D763D7C403E3BD4E82ECF488AE4FDF3E48D9722DDB8` | PASS |
| Prior independent-review SHA-256 | `F368E6A02050337308AD9ED2064DF4500DC4D477031CDF0A86E3C4A066E92E48` | PASS |

The reviewed A1 commit contains exactly these eight repository paths:

1. `outputs/rev869b_external_controller_phase_a_checkpoint.md`
2. `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`
3. `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`
4. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
5. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
6. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
7. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
8. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`

Both review views were assessed:

- Incremental A1: `7051b8fa93b1605e35b98394f21479e820c8f18c..3858eea9a4e88c58447880f5c9c36e0dfe2420e9`
- Cumulative Phase A: approved architecture-freeze baseline `51429dbfe9a307d7dc83b510419acd89d98d0057..3858eea9a4e88c58447880f5c9c36e0dfe2420e9`

## 3. Reconciled-finding decision matrix

| ID | Required closure | Independent result |
|---|---|---|
| F01 | Raw canonical ingress with no public typed protected-command bypass | **PASS** |
| F02 | Close all 14 ownership responsibilities and trusted authority boundaries | **FAIL** |
| F03 | Enforce every protected lifecycle transition using authoritative state, lease, fence, version, and grant facts | **FAIL** |
| F04 | Cryptographically close authoritative evidence readers and isolate the verifier oracle | **FAIL** |
| F05 | Fail-closed readiness and audit/privacy behavior | **FAIL** |
| F06 | Make all 27 tests and four decisive mutants independently probative | **FAIL** |
| F07 | Reconcile checkpoint evidence and validation-command integrity | **FAIL** |

### F01 — Raw canonical ingress: PASS

The production entry interfaces `IControlPlaneAuthority` and `IAcceptanceVerifierAuthority` expose raw byte ingress only. `PhaseAControlPlaneAuthority.ExecuteRawAsync` and `PhaseAAcceptanceVerifierAuthority.VerifyRawAsync` apply strict canonical decoding before protected authorization or verification. Legacy typed verifier/signing interfaces and implementations were made `internal`. No public typed protected-command verifier/signer bypass was found in the incremental or cumulative surface.

Evidence locations:

- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1317`
- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1327`
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:640`
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:652`
- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:462`
- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:479`

The A1 canonical-parser tests pass, and direct source inspection confirms the public authority boundary. No remaining F01 correction is required on the reviewed commit.

### F02 — Ownership and policy authority: FAIL

The ownership catalog names 14 responsibilities, but the executable trust flow does not implement the frozen ownership boundary:

- The raw control-plane authority accepts a separately injectable `ILeaseFenceAuthority`; it is not constrained to the same `IDurableControlPlanePersistenceProvider` that owns the durable composite state. No provider identity/version binding proves a single composite owner.
- The authority supplies trusted/current facts from request content or static operation mapping. It uses caller `requirementId`, treats the evidence schema as a reader version, hard-codes the evidence stage to `DURABLE`, computes current authorization state from the requested operation, and hard-codes export authorization substate to `NONE`.
- It constructs an active authorization binding and forwards the caller's expected lifecycle state and header resource version as `VerifiedLifecycleCommandV3` current state/version. Those are claims to be checked against trusted durable state, not authoritative observations.
- The registered authoritative reader's `ReadAsync` path is not called by the production verifier. The test fake deliberately throws from `ReadAsync`, demonstrating that caller-carried signed bundles, rather than a reader-owned observation operation, drive the decision.

Evidence locations:

- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:785`
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:794`
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:832`
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:833`
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:838`
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs` (`FakeReaderRegistry.ReadAsync`)

Required correction: make one durable persistence provider authoritative for current lifecycle state, authorization state, lease/fence, attempts, idempotency, and transition commit; obtain trusted policy and evidence facts from their designated owners; and compare caller claims against those facts. A catalog or dependency name is not sufficient.

### F03 — Lifecycle, lease, fencing, and transitions: FAIL

The 26-row rules table exists, but the raw production path cannot soundly reach or enforce the frozen transitions:

- Raw `EXPORT` and `COMPLETE_EXPORT` cannot meet rules that require `AUTHORIZED` or `DELIVERING` because the authority always supplies export substate `NONE`.
- Reauthorization from `EXPIRED` or `FAILED` cannot be represented by the operation-derived current authorization state.
- The state machine receives caller `ExpectedState` and caller header `ResourceVersion` as if they were authoritative current values. Its version comparison therefore compares values originating in the same request rather than a durable server record.
- The constructed binding is always `ACTIVE` while the separately derived current authorization state may be `CONSUMED`, producing inconsistent authority facts.
- Cancel/expire checks consume request-derived roles and expiry instead of an existing durable grant and its original authorizer/state.
- Quarantine is treated as not requiring a lease, with no durable observation of whether a lease is held and must be fenced.

The direct state-machine tests show that selected predicates exist; they do not establish correct production composition. The public pure state machine and its legacy replacement helper are not substitutes for a trusted durable transition boundary.

Evidence locations:

- `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs:5`
- `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs:222`
- `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs:306`
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:832`

Required correction: bind every transition to server-read durable state and version, enforce lease/fence rules conditionally from that state, validate the existing authorization grant and original authorizer, and atomically persist transition, audit, attempt, and idempotency outcome.

### F04 — Evidence readers and oracle isolation: FAIL

The verifier checks signed reader bundles, issuer/audience/subject, role/scope, nonce, freshness, tuple identity, stage, signature, and replay/idempotency inputs. These are useful controls, but the trust closure remains incomplete:

- Required-reader closure compares a set of reader IDs. Two bundles from the same required reader with different observation IDs are accepted by the shared uniqueness validator and collapse to one set member; the required exact-once reader invariant is not enforced and `READER_DUPLICATE` is not produced.
- The production verifier resolves reader descriptors but never calls `readerRegistry.ReadAsync`. The untrusted request transports the complete observation bundle. A valid reader signature authenticates a statement, but the frozen provider-owned observation/oracle-isolation path is absent.
- `AcceptanceVerifierOptions.MaximumObservations` and `MaximumStringBytes` are applied in the older verifier path but not in the Phase-A V3 authority. The V3 path relies only on global contract limits, so stricter server configuration is ineffective.
- The raw verifier has no independently supplied expected organization/request/resource/attempt/policy binding. A caller can choose an allowed identity and submit correspondingly signed recent bundles without comparison to a server-owned expected verification request.

Evidence locations:

- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:530`
- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:545`
- `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`

Required correction: acquire observations through the registered authoritative reader operation or bind signed observations to an independently trusted expected request; enforce exactly one observation per required reader; apply configured server bounds; and cryptographically bind all required identity, operation, policy, stage, attempt, watermark, and freshness facts.

### F05 — Audit, privacy, and fail-closed readiness: FAIL

Audit append in the verifier is fail-closed and the audit record avoids raw evidence payloads, credentials, tokens, signatures, and secrets. Both protected HTTP routes map a non-ready decision to HTTP 503. The readiness decision itself is incomplete:

- A `READY` check with `ValidUntil == null` is accepted because staleness is checked only when the value is present.
- `RequiredIdentity` and `ObservedIdentity` are never compared.
- `CanExecuteProtectedOperation` checks count, set membership, and `READY` state, but not mandatory freshness, future/old `CheckedAt`, identity equality, policy identity/version, duplicates, or a configured timeout result.
- The A1 readiness test does not invoke either protected HTTP route and does not test missing, duplicate, timeout, degraded, identity mismatch, policy/version mismatch, or malformed-ready results.

Evidence locations:

- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:83`
- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1257`
- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1282`
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:410`

Required correction: make freshness timestamps, identity, policy/version, exact cardinality, duplication, timeout, degraded/error states, and both HTTP routes part of one fail-closed readiness contract; add exact privacy/audit failure tests across both authorities and lifecycle persistence.

### F06 — Test and mutant decisiveness: FAIL

All 27 A1 tests pass, but source inspection shows they are not independently decisive for the claimed production properties:

- The test named to reject every non-canonical command mutation checks one trailing-space header mutation; the evidence equivalent checks one unknown property.
- The ownership test rejects a hard-coded list of type names but does not prove constructor dependency closure or the single durable composite owner.
- The trusted-field test mutates only a role, not policy, grant, epoch, current durable state, reader, or ownership source.
- The 26-concept test maps production `RuleId` values through a production concept mapping and checks concepts, not every frozen row field.
- The unlisted-transition test skips all quarantine, cancel, and expire combinations.
- The test named for every lifecycle-binding mutation uses one prepare row and mutates version, role, and evidence only.
- Export/cancel/expire/quarantine tests exercise the pure state machine, not the raw authority and durable transition composition.
- Readiness tests cover ready, stale, and throwing cases only, and do not call the protected HTTP routes.
- Audit exactness mutates only `AttemptId`.
- Concurrency/idempotency behavior is implemented inside test-only controllers rather than tested against the production authority and durable provider.
- The four-entry mutant manifest supplies four invalid inputs. It does not delete or invert the production parser, signature, policy, lease, version, audit, lifecycle, or idempotency gates and therefore is not a decisive mutant campaign. Its reported 4 killed / 0 surviving total is not evidence that those production checks are necessary.

Evidence locations:

- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:65`
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:171`
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:410`
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:544`

Required correction: replace self-referential and test-double proofs with independent frozen-oracle vectors, exercise raw production composition, cover the complete listed and unlisted transition space, and implement four actual gate-removal/inversion mutants that the suite demonstrably kills.

### F07 — Checkpoint and validation integrity: FAIL

Both required `git diff --check` commands independently pass. The checkpoint nevertheless labels four build/test commands as exact while the stored lines contain a literal tab after `.` and omit the path separators, for example the rendered form is equivalent to `dotnet build .<TAB>estsSESS.NexaERP...`. Lines 156–159 are not executable reproductions of the claimed commands. This is a report-integrity failure even though the independently reconstructed commands pass.

Evidence location:

- `outputs/rev869b_external_controller_phase_a_checkpoint.md:156`
- `outputs/rev869b_external_controller_phase_a_checkpoint.md:157`
- `outputs/rev869b_external_controller_phase_a_checkpoint.md:158`
- `outputs/rev869b_external_controller_phase_a_checkpoint.md:159`

Required correction: in the next authorized correction, regenerate the checkpoint from captured machine-readable command/result evidence and verify its literal stored command text before committing it.

## 4. Fourteen production ownership responsibilities

The frozen catalog is present, but effective runtime ownership is the controlling criterion.

| # | Responsibility | Catalog present | Effective source-only result |
|---:|---|---|---|
| 1 | ERP runtime | Yes | PASS — no competing protected authority identified |
| 2 | Control-plane authority | Yes | FAIL — synthesizes trusted/current fields from request/operation |
| 3 | Acceptance-verifier authority | Yes | FAIL — lacks independently trusted expected verification binding |
| 4 | Durable control-plane persistence | Yes | FAIL — not proven as the single composite owner of state/lease/fence/idempotency |
| 5 | Trusted issuer registry | Yes | PASS — allow-list and key resolution checks are present |
| 6 | KMS/signing authority | Yes | PASS — signing dependency is isolated behind an interface |
| 7 | Authoritative evidence readers | Yes | FAIL — production verification does not invoke `ReadAsync`; exact-once reader closure is absent |
| 8 | Immutable audit store | Yes | PASS for verifier append failure behavior; lifecycle atomicity remains part of F03 |
| 9 | Lifecycle-transition controller | Yes | FAIL — consumes request-derived current state/version rather than durable state |
| 10 | Backup/recovery authority | Yes | PASS — ownership separation is represented; no prohibited execution path found |
| 11 | Purge authorizer | Yes | PASS — separate authorization role is represented |
| 12 | Purge executor | Yes | PASS — separate execution role is represented |
| 13 | Export authorizer | Yes | PASS — separate authorization role is represented |
| 14 | Export delivery | Yes | PASS — separate delivery role is represented |

Overall ownership result: **FAIL**. Catalog completeness does not establish effective exclusive ownership.

## 5. Security-boundary assessment

| Boundary/control | Result | Independent assessment |
|---|---|---|
| Raw canonical command ingress | PASS | Public protected authorities accept raw bytes and canonicalize before decision |
| Public typed protected-command bypass | PASS | No public typed verifier/signer authority found |
| Issuer, audience, subject | PARTIAL/FAIL overall | Checks exist, but trusted expected-request binding is incomplete |
| Role and scope | PARTIAL/FAIL overall | Checks exist; current grant/role facts can originate from request-derived construction |
| Nonce and freshness | PARTIAL/FAIL overall | Checks exist; exact expected request/policy and mandatory readiness freshness do not |
| Idempotency/replay | PARTIAL/FAIL overall | Inputs/checks exist; production atomic durable ownership is not proven |
| Lease and fencing | FAIL | Separate injectable authority and no single durable composite transition |
| Tenant/organization isolation | PARTIAL/FAIL overall | Tuple consistency checks exist; no server-owned expected organization/request binding |
| Evidence-oracle isolation | FAIL | Reader operation is bypassed; caller supplies signed bundles |
| Audit failure/privacy | PARTIAL/FAIL overall | Verifier append is fail-closed and payload is minimized; lifecycle atomicity and test coverage are incomplete |
| Readiness fail-closed behavior | FAIL | Missing expiry/identity/policy/cardinality/route enforcement |

## 6. Lifecycle-rule coverage

The implementation declares all 26 architecture concepts (prepare/start/progress/complete/fail/cancel/expire/quarantine, export authorization/delivery/completion, and reauthorization variants). The direct rules-table concept enumeration passes. This is structural presence only. Because the raw authority supplies non-authoritative current state/version and hard-codes export substate, the cumulative executable path does not satisfy the 26-rule architecture contract. Therefore no individual protected transition receives a production-path PASS in this review.

## 7. Independent offline validation

| Validation | Result | Totals/evidence |
|---|---|---|
| Phase-A project build | PASS | 4 projects; 0 warnings; 0 errors |
| Full solution build | PASS | 5 projects; 0 warnings; 0 errors |
| A1 filtered suite | PASS | 27 passed; 0 failed; 0 skipped |
| Complete Phase-A test project | PASS | 27 passed; 0 failed; 0 skipped |
| Focused REV869B non-PostgreSQL tests | PASS | 76 passed; 0 failed; 0 skipped |
| Complete non-PostgreSQL suite | PASS | 450 passed; 0 failed; 0 skipped |
| PostgreSQL discovery only | PASS | 34 discovered; 34 unique; **0 executed** |
| PowerShell 5.1 AST parsing | PASS | PowerShell 5.1.19041.6456; 24 scripts; 0 parse errors; 0 scripts executed |
| EF migration discovery with `--no-connect` and inert `127.0.0.1:1` connection | PASS | 13 migrations; exit 0; no connection used |
| Migration uniqueness/order | PASS | 13 sources; REV869A once at ordinal 12; REV869B once at ordinal 13; adjacent |
| Model/snapshot parity plus offline SQL/hash tests | PASS | 3 passed; 0 failed; 0 skipped |
| REV869B Up SQL/hash | PASS | 324,914 bytes; `39B067351894AB5732B6DF9C6348B04D708780AFAA18E073F8E6594D07FAF213` |
| REV869B Down SQL/hash | PASS | 11,720 bytes; `FC4BCB671501D601041FCED25D6053545BE9F38CF1D9982006953F47229E0AE4` |
| Incremental `git diff --check` | PASS | Exit 0; no output |
| Cumulative `git diff --check` | PASS | Exit 0; no output |
| Incremental security/secret/privacy/prohibited-operation scan | PASS | 1,950 added source/test lines; 0 actionable hits |
| Cumulative security/secret/privacy/prohibited-operation scan | PASS | 3,198 added source/test lines; 0 actionable hits |

The initial broad cumulative text scan matched the architecture prose phrase “No test may open a socket, database, process...”. Source inspection classified it as documentation, not executable prohibited behavior. Refined executable source/test scans found zero credential, private-key, database-action, process/network, protected-mutation endpoint, or sensitive-logging hits.

Validation totals:

- Builds: 2 invocations, 9 project build results, 0 warnings, 0 errors.
- Tests executed: 556 passed, 0 failed, 0 skipped (`27 + 27 + 76 + 450 + 3`; suites overlap and are reported by invocation, not as unique test cases).
- PostgreSQL tests: 34 discovered, **0 executed**.
- PowerShell scripts: 24 parsed, 0 executed.
- EF migrations: 13 discovered without connection.
- Decisive mutants claimed by A1: 4 killed, 0 survivors; independent decisiveness assessment: **FAIL** because these are invalid-input cases, not production gate-removal/inversion mutants.

## 8. Incremental and cumulative whitespace evidence

The exact independent commands were:

```text
git diff --check 7051b8fa93b1605e35b98394f21479e820c8f18c 3858eea9a4e88c58447880f5c9c36e0dfe2420e9
git diff --check 51429dbfe9a307d7dc83b510419acd89d98d0057 3858eea9a4e88c58447880f5c9c36e0dfe2420e9
```

Both exited 0 and produced no output.

## 9. External prerequisites and retained states

This source-only review does not authorize PostgreSQL execution, provisioning, credentials, deployment, production operations, destructive lifecycle work, or Phase B.

```text
phase_a_correction_a1_independent_review_state=FAIL
phase_a_management_acceptance_state=PENDING
phase_b_state=NO_GO
correction_2_state=NO_GO
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
external_provisioning_state=NOT_STARTED
production_readiness_state=NOT_READY
```

## 10. Final decision and single next gate

**Review verdict:** `FAIL`  
**Reviewed commit:** `3858eea9a4e88c58447880f5c9c36e0dfe2420e9`  
**Reviewed parent:** `7051b8fa93b1605e35b98394f21479e820c8f18c`  
**Finding results:** F01 PASS; F02 FAIL; F03 FAIL; F04 FAIL; F05 FAIL; F06 FAIL; F07 FAIL  
**PostgreSQL executed count:** `0`  
**Exact single next gate:** a separate **report-only REV869B Option-A Phase-A Correction A1 failure reconciliation** covering F02–F07. No correction implementation, Phase B work, PostgreSQL execution, provisioning, deployment, or production operation is authorized by this review.

The report SHA-256, report-only commit identifier, committed-file count, and final target-scoped Git status are recorded after the report-only commit in the reviewer handoff; embedding a commit identifier in the committed report would make the report hash and commit self-referential.
