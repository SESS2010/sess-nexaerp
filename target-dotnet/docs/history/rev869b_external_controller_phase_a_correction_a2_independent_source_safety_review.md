# REV869B Option-A Phase-A Correction A2 Independent Source-Safety Review

Review date: 2026-08-16

Review type: fresh, independent, source-only, report-only

Reviewed commit: `aca12c48cbfbd59fba56264003b38f90e62b7ef8`

Reviewed parent: `12cff947a3928717e50e5357fa41c4f1c62aaf0d`

Frozen architecture baseline: `51476760adcea9ed7babbc04d642e53e371c6941`

Verdict: `FAIL`

Correction A2 preserves the raw-only ingress boundary and materially improves composite snapshot acquisition, lifecycle checks, authoritative evidence reads, readiness, and audit-receipt validation. It does not close the frozen Phase-A contract. The composite persistence contract still publicly exposes independently callable mutation facets; the lifecycle state machine accepts a provider-owned active grant whose exact approval binding has been substituted; the verifier lets caller-carried reader version data choose the authoritative reader descriptor; and the checkpoint records offline SQL sizes and hashes that contradict the unchanged pinned migration contract.

The only next gate is a separate report-only A2 failure reconciliation. Phase B and Correction 2 remain `NO_GO`.

## 1. Stage-0 gate

| Gate | Reproduced evidence | Result |
|---|---|---|
| HEAD | `aca12c48cbfbd59fba56264003b38f90e62b7ef8` | PASS |
| Parent | `12cff947a3928717e50e5357fa41c4f1c62aaf0d` | PASS |
| Subject | `REV869B Phase-A Correction A2 source safety` | PASS |
| Branch | `master` | PASS |
| Checkpoint | `outputs/rev869b_external_controller_phase_a_checkpoint.md` | PASS |
| Checkpoint SHA-256 | `319271FFF2E8D2E9EB35783FFD6100C1C5223EE13D8AD26A8CBA04ACF6456F47` | PASS |
| A2 commit boundary | exactly 10 changed files | PASS |
| A2 allowlist | all 10 files match the authorized A2 allowlist; 0 outside | PASS |
| Target-scoped entry status | clean | PASS |
| `../legacy-reference/` | repository status metadata showed one untracked sibling; index contained no tracked entry; contents were not enumerated, read, or modified | PASS within the mandatory no-access rule |
| Mandatory inputs | architecture-freeze specification, historical A1 checkpoint at the parent tree, A1 independent review, A1 failure reconciliation, and updated A2 checkpoint read completely | PASS |

Incremental review range:

`12cff947a3928717e50e5357fa41c4f1c62aaf0d...aca12c48cbfbd59fba56264003b38f90e62b7ef8`

Cumulative review range:

`51476760adcea9ed7babbc04d642e53e371c6941...aca12c48cbfbd59fba56264003b38f90e62b7ef8`

## 2. F01-F07 decision

| Finding | Result | Independent assessment |
|---|---|---|
| F01 raw canonical ingress | PASS | The two public protected authorities remain raw-byte entry points; no public typed protected-command bypass or mutating HTTP endpoint was found. |
| F02 ownership and provenance | FAIL | The durable owner still exports separate nonce, idempotency, and lease mutation APIs, and the verifier's reader version is selected from caller-carried bundle data. |
| F03 lifecycle and composite atomic enforcement | FAIL | A provider-owned current grant with substituted exact approval fields is accepted; the composite contract also permits partial facet calls. |
| F04 evidence-reader and oracle isolation | FAIL | Reader calls are authoritative and oracle input uses returned bundles, but the caller chooses the reader version used for descriptor resolution and `ReadAsync`. |
| F05 readiness, audit, and privacy | PASS | Exact dependency cardinality, policy, version, identity, freshness, common 200/503 mapping, minimized audit data, and receipt/hash checks are fail closed in the reviewed paths. |
| F06 tests and production mutants | FAIL | All required suites pass and four real mutants are valid/killed, but named coverage does not reject exact stored-grant substitution or caller-selected reader-version drift. |
| F07 checkpoint and evidence integrity | FAIL | The checkpoint's REV869B Up/Down SQL byte counts and hashes contradict the unchanged pinned offline test and prior evidence. |

## 3. Blocking findings

### A2-01 — CRITICAL — composite persistence contract permits partial mutation

Exact locations:

- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1462-1497`
- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1511-1529`

Violated invariant:

The durable control-plane provider must expose one authoritative snapshot and one composite atomic transaction. Its contract must not permit nonce registration, idempotency claim, lease acquisition/renewal/expiry, lifecycle mutation, authorization consumption, attempt mutation, or audit outcome to succeed independently.

Reproduction evidence:

`IDurableControlPlanePersistenceProvider` publicly inherits `INonceRegistrationAuthority`, `IIdempotencyAuthority`, and `ILeaseFenceAuthority`. The inherited public surface includes `RegisterNonceAsync`, `ClaimAsync`, `AcquireAsync`, `RenewAsync`, and `ExpireAsync` in addition to `ExecuteAtomicallyAsync`. The A2 constructor now receives one provider instance, but the exported contract still permits consumers to invoke the partial mutation methods without the composite transaction. This violates the A1 reconciliation formula `separate_state_lease_idempotency_owner_count = 0` and the frozen no-partial-success contract.

Operational/security impact:

A future production endpoint, worker, reconciler, or provider adapter can reserve a nonce, claim idempotency, or alter a lease/fence without committing the corresponding lifecycle, authorization, attempt, response, and audit outbox state. That recreates split-brain ownership, stuck reservations, stale fencing, duplicate destructive work, and unverifiable partial success.

Required correction:

Remove public mutation facets from the Phase-A composite provider contract. Retain only provider identity/version, one authoritative read contract, and one exact atomic decision contract. If internal provider implementation needs facet methods, keep them non-exported and unreachable to production consumers outside the atomic implementation.

Required acceptance evidence:

- Exported-interface reflection proves zero independently callable nonce/idempotency/lease/state mutation methods.
- The control authority and every service/endpoint receive exactly one composite provider dependency.
- Fault injection at every component boundary yields either one complete commit or no change.
- No partial facet call can be compiled from an exported production contract.

### A2-02 — CRITICAL — exact provider-owned authorization grant is not bound to execution

Exact locations:

- `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs:307-321`
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:811-846`

Violated invariant:

An execution transition must consume the exact provider-owned authorization: authorization/grant identity and hash, issuer, original subject/workload/role, policy version and row, scope, resource and resource version, authorized operation, canonical approved-request digest, evidence-manifest digest, lease/fence binding, and validity interval. A grant for one approved plan must never authorize a substituted plan or actor.

Reproduction evidence:

The state machine checks the current grant only for state, scope, resource type/id, mapped authorization operation, and time. It never compares the current grant's `ResourceVersion`, `CanonicalRequestSha256`, `EvidenceManifestSha256`, `AuthorizationId`, `GrantIssuer`, `AuthenticatedSubject`, `WorkloadIdentity`, `TrustedRole`, `TrustedScope`, `PolicyVersion`, `PolicyRowId`, or `GrantSha256` to the executing command or its server-resolved authorization, except that CANCEL checks subject alone.

A disposable, non-committed xUnit reproduction used the unmodified production state machine. It created a valid `PREPARE` command, then changed the provider-owned current grant to resource version 99, different canonical-request and evidence-manifest hashes, a different authorization ID, subject, workload, trusted role, policy version/row, and grant hash. `RequirePhaseACommand` still returned the `Preflight -> Provisioning` rule. Result: 1 passed, 0 failed. The disposable tree was deleted and target status returned clean.

Operational/security impact:

A valid active grant for the same resource and operation class can authorize a different approved-parameter set, evidence manifest, policy decision, actor, or resource version. This enables approval substitution, stale-plan execution, cross-actor grant reuse, and destructive work outside the exact management authorization while still producing an apparently valid atomic transition and audit record.

Required correction:

Define and enforce an exact grant-to-command binding. For operations that consume an existing grant, compare every immutable approval field and digest from `AuthoritativeControlPlaneSnapshotV3.CurrentAuthorization` with the server-resolved execution authorization, canonical command/plan digest, evidence manifest, scope/resource/version, and lease/fence. Reject any mismatch before lifecycle evaluation or `ExecuteAtomicallyAsync`.

Required acceptance evidence:

- A literal mutation matrix changes every stored-grant field independently and proves zero lifecycle/transaction calls.
- The raw production path rejects changed approved parameters and canonical request/evidence digests.
- Positive tests prove an exact grant is consumed once and a completed replay returns only its exact stored outcome.
- A real production mutant removing one exact-grant comparison is killed by the intended test.

### A2-03 — CRITICAL — caller-selected reader version controls authoritative evidence acquisition

Exact locations:

- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1190-1203`
- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1550-1566`
- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:521-550`
- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:568-603`

Violated invariant:

Reader identity, version, artifact, schema, and required-reader set must originate from a server-owned expectation/readiness policy. Caller-carried facts or configuration must not select the authoritative reader implementation or version, and identity/version drift must fail closed.

Reproduction evidence:

`EvidenceVerificationExpectationV3` contains no required reader IDs, versions, or artifact hashes. `AcceptanceVerifierOptions.RequiredReaderIds` pins only IDs. The verifier groups caller-declared bundles by reader ID, then calls `ResolveAsync(declaredBundle.ReaderId, declaredBundle.ReaderVersion)` and passes that descriptor to `ReadAsync`. Therefore the caller-selected `ReaderVersion` chooses which otherwise registered reader descriptor/version is invoked. A consistent bundle returned by that selected version passes descriptor, signature, and byte equality checks. The A2 fake registry reinforces the gap by echoing the requested version into its descriptor, and no A2 test supplies two valid registered versions and proves the non-current one is rejected.

Operational/security impact:

An older, alternate, or semantically different registered reader version can become the evidence authority at caller choice. That can change allowed fields, bounds, query semantics, snapshot behavior, or artifact code and can produce an acceptance verdict under version drift that the server-owned verification request never authorized.

Required correction:

Put the exact required reader ID/version/artifact/schema/stage set in the server-owned expectation or signed compatibility/readiness policy. Resolve descriptors from that trusted set, never from declared bundle version. Treat caller bundle metadata only as a claim that must byte-match the independently selected reader result.

Required acceptance evidence:

- Two simultaneously resolvable reader versions are configured; only the server-pinned version is called.
- Caller substitution of version/artifact/schema is rejected before `ReadAsync` and before oracle evaluation.
- Provider call traces prove descriptor selection comes solely from the trusted expectation.
- A production mutant that uses `declaredBundle.ReaderVersion` is killed.

### A2-04 — MEDIUM — checkpoint offline SQL/hash evidence is incorrect

Exact locations:

- `outputs/rev869b_external_controller_phase_a_checkpoint.md:110-111`
- `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs:446-466`

Violated invariant:

The checkpoint must record reproducible exact command results and hashes. A2 changed no migration, model, snapshot, or offline SQL source.

Reproduction evidence:

The checkpoint reports Up SQL `326,596` bytes with SHA-256 `1F043EC09F391970C111EFBBBF8C1C8A750DBC11DC4E776015841FC258A6FC21` and Down SQL `11,759` bytes with SHA-256 `18F834FDFE50270F0C7E7C01744176755CF7FC9F7BB1E6896E70604CF695EBF8`.

The unchanged pinned production-model test independently generated the migration scripts in memory without connecting and passed only with Up SQL `324,914` bytes / `39B067351894AB5732B6DF9C6348B04D708780AFAA18E073F8E6594D07FAF213` and Down SQL `11,720` bytes / `FC4BCB671501D601041FCED25D6053545BE9F38CF1D9982006953F47229E0AE4`. The focused three-test parity/SQL run passed 3/3. No A2 file can account for changed generated SQL.

Operational/security impact:

Management cannot reproduce or identify the claimed reviewed SQL artifact. Incorrect hashes break evidence custody and can conceal use of a different generation range, options set, tool output, or unreviewed artifact.

Required correction:

In the separately authorized reconciliation/correction flow, identify the exact generation command and range that produced the checkpoint values. Record machine-captured bytes, hashes, migration endpoints, options, tool version, and newline/encoding rules. Do not rewrite historical reports.

Required acceptance evidence:

- Checkpoint values equal an independently repeated in-memory generation byte-for-byte.
- Pinned tests and checkpoint use the same documented migration endpoints/options.
- Source/model/snapshot hashes and generated SQL hashes are captured in one machine-readable run.

## 4. Fourteen production responsibilities

The literal catalog contains all 14 entries and maps each name to one interface. Effective ownership is not fully closed.

| # | Responsibility | Result | Assessment |
|---:|---|---|---|
| 1 | NexaERP business runtime | PASS | No lifecycle or acceptance authority added. |
| 2 | Control Plane | FAIL | Execution does not bind the exact provider-owned grant. |
| 3 | Acceptance Verifier | FAIL | Caller reader version influences authoritative descriptor selection. |
| 4 | Durable Control Plane persistence | FAIL | Exported partial mutation facets remain alongside the composite transaction. |
| 5 | Trusted issuer/key registry | PASS | Exact issuer/key/algorithm/time checks remain. |
| 6 | KMS/HSM signing | PASS | Signing and verification stay behind the isolated provider. |
| 7 | Authoritative evidence reader | FAIL | Reader ID is pinned, but exact version/artifact selection is not server-owned. |
| 8 | Immutable audit/evidence | PASS | Exact receipt ID/hash/reference checks fail closed in reviewed verifier paths. |
| 9 | Lifecycle controller | FAIL | Provider-owned grant substitution reaches a legal transition. |
| 10 | Backup/recovery authority | PASS | Separate owner; no execution path introduced. |
| 11 | Purge authorizer | PASS | Separate owner; no execution path introduced. |
| 12 | Purge executor | PASS | Separate owner; no execution path introduced. |
| 13 | Export authorizer | PASS | Separate owner and export substates remain represented. |
| 14 | Export delivery executor | PASS | Separate owner and ordered delivery substates remain represented. |

Overall: 14/14 catalog entries present; 9/14 effective ownership assessments PASS; 5/14 FAIL. The required unambiguous effective-ownership condition is not met.

## 5. Lifecycle and protected-path assessment

The 30 concrete literal rows representing 26 frozen concepts are present. State/version, authorization state, export state, attempt, evidence IDs, lease ID/holder/epoch/fence/expiry, cancellation subject, expiry time, and proposed transaction receipt fields are checked. Unlisted state/operation pairs reject in the shipped suite.

The lifecycle result is nevertheless FAIL because exact grant/plan binding is missing and the durable contract permits partial mutation calls. Structural presence and raw-path traversal do not establish the frozen one-grant/one-plan/one-atomic-outcome invariant.

Both hosts expose health/version-only HTTP surfaces. No Phase-B persistence implementation, external operation, deployment, provisioning, database action, lifecycle action, recovery, purge, or export execution was introduced.

## 6. Independent offline validation

| Validation | Independent result |
|---|---|
| Affected Phase-A warning-as-error build | PASS; 4 projects; 0 warnings; 0 errors |
| Complete solution warning-as-error build | PASS; 5 projects; 0 warnings; 0 errors |
| A2 subset | PASS; 31 passed; 0 failed; 0 skipped |
| Complete Phase-A assembly | PASS; 50 passed; 0 failed; 0 skipped |
| Focused REV869B ERP non-PostgreSQL subset | PASS; 76 passed; 0 failed; 0 skipped |
| Complete ERP non-PostgreSQL assembly | PASS; 450 passed; 0 failed; 0 skipped |
| Model/snapshot/offline-SQL focused rerun | PASS; 3 passed; 0 failed; 0 skipped |
| PostgreSQL-named discovery | PASS; 87 discovered; 87 unique; 0 executed |
| Windows PowerShell AST | PASS; 5.1.19041.6456; 24 scripts; 0 parse errors; 0 executed |
| EF migration discovery | PASS; `--no-connect`; inert `127.0.0.1:1`; 13 migrations |
| REV869A/REV869B uniqueness and adjacency | PASS; one each; ordinals 12 and 13 of 13; adjacent |
| Incremental `git diff --check` | PASS; exit 0; no output |
| Cumulative exact-A2-allowlist `git diff --check` | PASS; exit 0; no output |
| Full cumulative-range `git diff --check` | exit 1 only for the already disclosed immutable A1 review Markdown hard-break whitespace at lines 3-6 and 291-295 |
| Incremental production safety scan | PASS; 435 added production lines; 0 actionable secret/key/database/process/network/mutating-endpoint/Phase-B hits |
| Cumulative source/test scan | 2 benign hits in the test that detects Phase-B/`DbContext` leakage; 0 actionable hits |
| PostgreSQL execution | exactly 0 |

The full cumulative whitespace output is historical and immutable, not an A2 source delta. It is disclosed rather than silently suppressed. It does not cure A2-04.

## 7. Test arithmetic

| Counting basis | Count | Relationship |
|---|---:|---|
| A2 subset | 31 | subset of the 50 Phase-A tests |
| Complete Phase-A assembly | 50 | unique within its assembly |
| Focused REV869B ERP subset | 76 | subset of the 450 ERP tests |
| Complete ERP non-PostgreSQL assembly | 450 | unique within its assembly |
| Total unique across the two assemblies | 500 | `50 + 450` |
| Raw pass events across the five formal invocations including parity rerun | 610 | `31 + 50 + 76 + 450 + 3`; not a unique count |
| PostgreSQL-named tests | 87 discovered / 0 executed | discovery only |

No overlapping subset invocation is added to the unique total.

## 8. Production-mutant reproduction

Mutants were applied only to a disposable copy containing the already-restored build graph. Each affected production project compiled with zero warnings/errors, the intended killer failed for the intended enforcement reason, and the disposable copy was removed. The reviewed target remained clean.

| Mutant | Independent mutated-file SHA-256 | Build | Intended killer | Result |
|---|---|---|---|---|
| M01 request-as-authority | `5E011E636495B0D3A6A158362F2EE616E41BCC1CF9DE08B986B41DDCCAF72105` | PASS | `A2_CallerStateVersionGrantExportAttemptAndEpochCannotBecomeTrustedFacts` | KILLED; provider/transition trace differed |
| M02 lifecycle version-gate bypass | `B37A76D051217B21C12AA28AEA866A7B42AA00DB2C1D2FA7D76ABC3FBC41F3AD` | PASS | `A2_EveryRowRejectsWrongStateVersionRoleScopeGrantEvidenceLeaseFenceEpochAttemptAndAudit` | KILLED; expected rejection was absent |
| M03 reader exact-cardinality bypass | `000D10AF51CE7B718C8DC22961F8AC12017AEA2FBC3F4BF8F25DBFEF108067F3` | PASS | `A2_DuplicateMissingUnknownOrExtraReaderFailsBeforeOracle` | KILLED; expected `READER_DUPLICATE`, actual `EVIDENCE_TAMPERED` |
| M04 readiness freshness bypass | `ED71CE4132679ED3EBF20DB390BB85006B3C0BDBA24818FEC444584A88DD8884` | PASS | `A2_NullExpiredFutureOrInvertedFreshnessReturns503OnBothRoutes` | KILLED; expected 503, actual 200 |

Mutant arithmetic: 4 compiled, 4 killed by intended assertions, 0 survived, 0 invalid, 0 left in the reviewed tree.

The four mutants are genuine, but they do not test the missing exact-grant binding or server-owned reader-version selection identified above. Therefore mutant validity alone does not make F06 pass.

## 9. Security and architecture conclusion

No hard-coded credential, private key, real secret, network client, database/migration action, protected mutating endpoint, sensitive logging, external operation, Phase-B implementation, or `legacy-reference` content access was found or performed.

The review cannot return `PHASE_A_CORRECTION_A2_PASS` because F02, F03, F04, F06, and F07 fail; five effective ownership responsibilities remain ambiguous or insufficiently enforced; caller-selected reader version can influence evidence authority; the exact provider-owned grant does not bind execution; the atomic provider contract permits partial operations; and checkpoint evidence is not reproducible.

Retained states:

```text
phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW
phase_b_state=NO_GO
correction_2_state=NO_GO
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
external_provisioning_state=NOT_STARTED
production_readiness_state=NOT_READY
```

## 10. Exact single next gate

`SEPARATE_REPORT_ONLY_PHASE_A_CORRECTION_A2_FAILURE_RECONCILIATION`

Do not begin another correction, Phase B, Correction 2, PostgreSQL execution, provisioning, deployment, or production operation automatically.
