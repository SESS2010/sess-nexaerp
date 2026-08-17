# REV869B Option-A Phase-A Correction A3 independent source safety review

Reviewed commit: `634a5b3edb29bd241a6cb703a9e8c9ecfda092f5`

Reviewed parent: `8c78f6a480fcbf86afbf9f5460598ece5b8d6732`

Frozen architecture baseline: `51476760adcea9ed7babbc04d642e53e371c6941`

Verdict: `FAIL`

Correction A3 preserves F01 and F05 and closes the exported partial-mutation surface. It does not satisfy the frozen Phase-A acceptance contract. A management authorization grant cannot express an explicit plan version or a separately approved executor identity, and the positive test succeeds only because authorizer and executor fixtures reuse the same workload identity and pre-bind the execution lease. The acceptance verifier also invokes the authoritative reader before it rejects caller-substituted version, artifact or schema metadata. Finally, the checkpoint excludes a real three-test invocation from its raw-event total and does not bind exact executable/tool artifacts or an observed migration-application counter.

The only next gate is a separate report-only A3 failure reconciliation. Phase B and Correction 2 remain `NO_GO`.

## 1. Stage-0 entry gate

| Gate | Independent result | Status |
|---|---|---|
| HEAD / parent | `634a5b3edb29bd241a6cb703a9e8c9ecfda092f5` / `8c78f6a480fcbf86afbf9f5460598ece5b8d6732` | PASS |
| Branch / subject | `master` / `REV869B Phase-A Correction A3 source safety` | PASS |
| Commit count | Exactly 1 commit in the incremental range | PASS |
| Changed-file count | Exactly 10 | PASS |
| Exhaustive allowlist | All 10 changed paths are in the supplied allowlist; no path is missing or outside it | PASS |
| Target-scoped status | Clean before review and before report creation | PASS |
| Checkpoint identity | Committed blob and worktree are both 18,083 bytes and SHA-256 `9159C9E1D339BC498FBF7CCF912041D524F3878B65FAA90ED53542FFF740E7B4` | PASS |
| Legacy boundary | `../legacy-reference/` remained untracked; no legacy file content was opened, read, changed or used | PASS |
| Mandatory reading | Architecture freeze, A1 review/reconciliation, A2 review/reconciliation, duplicate-entry blocker, A3 checkpoint and all 10 changed files were read completely | PASS |

Incremental range: `8c78f6a480fcbf86afbf9f5460598ece5b8d6732...634a5b3edb29bd241a6cb703a9e8c9ecfda092f5`.

Cumulative range: `51476760adcea9ed7babbc04d642e53e371c6941...634a5b3edb29bd241a6cb703a9e8c9ecfda092f5`.

## 2. F01-F07 decision

| Finding | Result | Independent assessment |
|---|---|---|
| F01 raw canonical ingress | PASS | Protected production authorities remain raw-byte entry points. Signature, canonicalization, nonce, freshness, transport identity, scope, policy, lease/fence, idempotency and health/version-only host boundaries remain present. No typed protected-command endpoint bypass was found. |
| F02 ownership and provenance | PASS | `IDurableControlPlanePersistenceProvider` exposes only `Descriptor`, one authoritative snapshot read and one `ExecuteAtomicallyAsync` mutation. It inherits no partial facets; no exported injectable nonce/idempotency/lease/lifecycle/state mutation authority remains. Provider and lifecycle descriptors are server-configured and checked before snapshot use. |
| F03 lifecycle and composite atomic enforcement | FAIL | `StoredAuthorizationGrantV3` has no explicit plan-version field and stores the current authorizer workload as `ExecutorClass`; consumption requires the executor workload to equal that same value. The positive test hides this by reusing `workload` and by attaching the execution lease during authorization creation. Exact separate authorizer/executor, plan-version and acquire-after-authorization binding is therefore not proven or representable. |
| F04 evidence-reader and oracle isolation | FAIL | Reader resolution is server-pinned, and the oracle receives only reader-returned facts, but caller-declared bundle bytes are compared only after `ReadAsync`. Caller version/artifact/schema substitution therefore causes an authoritative read before rejection, contrary to the mandatory pre-read failure invariant. |
| F05 readiness, audit and privacy | PASS | Dependency cardinality, identity/version, freshness and common 200/503 mapping fail closed. Audit receipt/result binding and server-controlled evidence count/byte/string/field restrictions remain covered by production-path tests. |
| F06 independent assurance | FAIL | Eight production mutants were valid and killed and all suites pass, but A3 tests encode `ReadCount == 1` for mismatched reader metadata and do not use distinct authorizer/executor workload identities or an explicit plan version. Tests therefore weaken or omit the mandatory production rules. |
| F07 checkpoint and evidence integrity | FAIL | Actual invocations produce 611 raw pass events, not 608. The canonical record contains tool version strings but not executable/tool path and hash, defaults `Commit` to the parent rather than the reviewed commit, and records `MigrationApplyCount` as literal `0` rather than an observed counter. Exact evidence identity is incomplete. |

## 3. Blocking and required findings

### A3-IR-01 — CRITICAL — grant cannot bind a distinct executor or explicit plan version

Exact source locations:

- `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1072-1090`
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:824-850`
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:858-874`
- `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:958-959`
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:1402-1428`
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:2028`

Violated invariant:

Every stored management authorization grant must bind issuer, actor, policy, tenant, approved plan, explicit plan version, evidence, separately approved executor, lease/fence, expiry and lifecycle operation. Authorization creation and later execution must remain separate authoritative identities and lifecycle steps.

Reproduction evidence:

`StoredAuthorizationGrantV3` contains `ResourceVersion`, `ExecutorClass` and `ApprovedIntentSha256`, but no explicit plan-version member. `HashApprovedIntent` hashes only `ApprovedParameters`. At grant creation, `ExecutorClass` is assigned `transportIdentity.WorkloadIdentity`, which is the authenticated workload currently performing `AUTHORIZE_*`. At consumption, the code requires `payload.StoredGrantClaim.ExecutorClass == transportIdentity.WorkloadIdentity`. A distinct management authorizer workload and execution workload cannot both satisfy that equality unless they are intentionally collapsed into one identity.

The positive A3 production-path test changes the role from `Operator` to `ProvisioningExecutor` but its fixture keeps `AuthenticatedWorkloadIdentityV3(..., "workload", ...)` for both calls. It also invokes `AUTHORIZE_PREPARE` with `requiresLease: true` and carries that same pre-existing lease/fence into `PREPARE`. This does not prove the frozen authorize-then-acquire-lease-then-execute workflow and cannot prove a separately identified executor.

Operational/security impact:

Production must either collapse management authorization and execution into one workload identity, contradicting separation of duties, or reject a legitimate separately identified executor. Because plan version is not explicit, reviewers cannot prove that authorization applies to one immutable plan revision rather than merely a parameter hash/resource version. Pre-binding the execution lease at authorization time also prevents a clean later lease acquisition and encourages stale or self-authorized fencing semantics.

Required correction:

Add explicit immutable approved-plan identity and plan-version fields, plus a separately management-approved executor identity/class. Resolve all of them from server-side authorization providers. Define whether authorization precedes lease acquisition; if so, bind the acquired lease/fence through a server-authorized transition without treating caller claims as authority. Reject every mismatch before lifecycle evaluation and atomic mutation.

Required acceptance evidence:

- A raw positive path using distinct management-authorizer and executor workload identities.
- An explicit plan ID and plan-version mutation matrix, independent from resource version and parameters.
- A positive authorize-without-lease, acquire-lease, consume-once trace if that is the frozen lifecycle.
- Negative substitutions for authorizer, executor, plan ID/version, evidence, lease/epoch/fence and expiry with zero lifecycle and atomic calls.
- A compiled production mutant removing each new exact binding, killed by the intended assertion.

### A3-IR-02 — CRITICAL — mismatched caller reader metadata is rejected only after authoritative read

Exact source locations:

- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:557-568`
- `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:586-612`
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:1589-1609`

Violated invariant:

Missing, duplicate, revoked, stale, unexpected or mismatched reader identity/version/artifact/schema/stage must fail before reading evidence or invoking the oracle. Caller-selected metadata may be comparison-only, but comparison must precede any authoritative reader side effect.

Reproduction evidence:

The verifier resolves and validates the server-pinned descriptor, then enters the read loop. It locates `declaredBundle`, calls `readerRegistry.ReadAsync`, serializes the returned and caller-declared bundles, and only then checks byte equality. Consequently a caller substitution of reader version, artifact or schema still invokes the selected reader once. The named A3 test explicitly asserts `ReaderRegistry.ReadCount == 1` for all four substitutions while asserting oracle count zero. That is direct executable evidence of the prohibited ordering.

Operational/security impact:

An unauthorized or incompatible caller envelope can trigger authoritative evidence collection, database/service load, access logs, timing exposure and reader-side effects before it is rejected. This creates a pre-authorization resource-consumption and information-access surface and violates the required no-read boundary.

Required correction:

Before `ReadAsync`, compare caller-declared reader identity, version, artifact, schema and required lifecycle stage with the complete server-pinned descriptor/expectation. Reject any mismatch with zero reads and zero oracle calls. Keep authoritative facts themselves reader-produced; do not trust caller facts.

Required acceptance evidence:

- Version upgrade, downgrade, artifact, schema, stage and identity substitutions each show descriptor resolution only, `ReadCount == 0`, `OracleCount == 0`.
- Missing, duplicate, revoked, stale and unexpected descriptors show the same zero-read trace.
- A positive path proves each pinned reader is read exactly once and only returned facts reach the oracle.
- A production-ordering mutant that moves comparison after read is compiled and killed specifically on read count.

### A3-IR-03 — HIGH — assurance tests encode or omit mandatory trust separations

Exact source locations:

- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:1402-1428`
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:1589-1609`
- `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:2028`

Violated invariant:

Tests must exercise production rules without duplicating, replacing, weakening or reinterpreting them. Test names and passing counts cannot substitute for denial traces at the mandated enforcement boundary.

Reproduction evidence:

The grant positive path varies trusted role but not workload identity, has no explicit plan-version value, and pre-binds a lease to the authorization request. The reader substitution test's name says the caller “never selects reader,” but it affirmatively expects one evidence read. All 16 named A3 tests pass while these mandatory cases remain unproven or contradicted.

Operational/security impact:

The suite can remain green while production collapses separation of duties and performs evidence reads for caller-mismatched envelopes. Future regressions may preserve the same incorrect expectations.

Required correction:

Change production rules first, then make fixtures model distinct authoritative identities and lifecycle steps. Assertions must distinguish descriptor selection from evidence read and require zero reads for mismatched caller metadata.

Required acceptance evidence:

- Literal raw-path tests for distinct authorizer/executor identity and explicit plan version.
- Zero-read/zero-oracle assertions for every reader metadata mismatch.
- Trace assertions showing the exact gate that rejected each case.
- Re-run of all eight independent production mutants against the corrected expectations.

### A3-IR-04 — HIGH — checkpoint arithmetic and canonical tool identity are not exact

Exact source locations:

- `outputs/rev869b_external_controller_phase_a_checkpoint.md:113-125`
- `outputs/rev869b_external_controller_phase_a_checkpoint.md:128-158`
- `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs:528-590`

Violated invariant:

Raw test events must count every actual invocation, while unique totals must de-duplicate overlapping subsets. Canonical SQL evidence must bind the exact reviewed source, executable/tool identity, endpoint/options, input hashes, encoding/newlines, bytes and SQL hashes, and its zero-application claim must be observed rather than asserted.

Reproduction evidence:

The exact A3 subset requires two invocations: 13 control-plane A3 tests and 3 ERP canonical tests. Together with 63, 79 and 453, actual pass events are `13 + 3 + 63 + 79 + 453 = 611`. The checkpoint reports `13 + 63 + 79 + 453 = 608` and labels the separately executed 3-test invocation diagnostic; diagnostic execution is still a raw event. The unique total remains correctly `63 + 453 = 516`, because both subsets overlap their complete assemblies.

The canonical JSON defaults `Commit` to parent `8c78f6...`, not reviewed commit `634a5b3...`. It records SDK/EF version strings but no executable or EF tool artifact path/hash. Independent identity was `C:\Program Files\dotnet\dotnet.exe`, SHA-256 `AB1B71FD3DD71062E074C9FAB8312081A81B7F2B3E0327C48C4D249C8D1A3135`; the resolved EF artifact was `dotnet-ef.dll` 10.0.10, SHA-256 `520513FA1B7AC3E6F4195CF3CEFDF9D2F50924750EB480E44583A30A69BD8D25`. The evidence constructs `MigrationApplyCount` with literal `0`; only connection opens are intercepted and counted.

Operational/security impact:

The checkpoint cannot be reproduced under one consistent raw-event definition and cannot prove which exact executable artifacts generated the SQL. A declared zero-apply field is weaker than an observed counter and can give false assurance if generation code later changes.

Required correction:

Report 611 raw pass events for these five invocations and 516 unique tests. Bind the reviewed source through a non-self-referential manifest/input hash or supplied reviewed commit, and record exact executable/tool paths and hashes. Instrument or otherwise independently prove migration-application count instead of assigning literal zero.

Required acceptance evidence:

- Command-by-command discovery and result logs totaling exactly 611 raw events and 516 unique tests.
- Canonical JSON containing exact source-manifest identity, dotnet and EF tool artifact paths/hashes, all existing SQL inputs/options and output hashes.
- Two fresh-process outputs identical byte-for-byte.
- Observed zero connection opens and zero migration applications.

## 4. Fourteen effective ownership assessments

| # | Frozen responsibility | Result | Effective-owner assessment |
|---:|---|---|---|
| 1 | NexaERP business runtime | PASS | No lifecycle or acceptance authority was added to ERP runtime. |
| 2 | Control Plane | FAIL | Its grant schema/path cannot bind a distinct executor or explicit plan version. |
| 3 | Acceptance Verifier | FAIL | It authorizes a reader call before rejecting caller metadata mismatch. |
| 4 | Durable control-plane persistence | PASS | One descriptor, one snapshot read and one atomic mutation; no exported partial mutation facet. |
| 5 | Trusted issuer/key registry | PASS | Exact issuer/key/algorithm/time verification remains isolated. |
| 6 | KMS/HSM signing | PASS | Signing stays behind the isolated signing provider. |
| 7 | Authoritative evidence reader | FAIL | Server selection is pinned, but mismatched caller metadata can still cause its invocation. |
| 8 | Immutable audit/evidence | PASS | Exact receipt/reference/hash/result checks remain fail closed. |
| 9 | Lifecycle controller | FAIL | It receives a grant model that collapses authorizer/executor identity and lease timing. |
| 10 | Backup/recovery authority | PASS | Separate owner; no production execution was introduced. |
| 11 | Purge authorizer | PASS | Separate catalog/interface owner; no production execution was introduced. |
| 12 | Purge executor | PASS | Separate catalog/interface owner; no production execution was introduced. |
| 13 | Export authorizer | PASS | Separate catalog/interface owner; frozen substate checks remain. |
| 14 | Export delivery executor | PASS | Separate catalog/interface owner; frozen evidence/substate checks remain. |

Overall: 14/14 catalog labels and distinct interfaces are present; 10/14 effective assessments PASS and 4/14 FAIL. The required one-effective-authoritative-owner condition is not met.

## 5. Independent production-mutant campaign

All mutations changed real production enforcement points only in `C:\Users\User\AppData\Local\Temp\rev869b-a3-independent-mutants-20260817`. Each affected graph compiled independently with zero warnings/errors. Each intended test then failed for the stated semantic reason. After the eighth result, every mutated source hash was compared with the reviewed target and matched exactly; the validated temporary directory was removed.

| Mutant | Mutated production SHA-256 | Independent build | Intended killer and semantic failure | Result |
|---|---|---|---|---|
| A3-M01 partial composite-mutation bypass | `837568652E66C595DC4FA07734F940ECE70E88F94C9501FAAC51C8C0005452CC` | PASS | `A3_ExportedOrInjectablePartialNonceIdempotencyLeaseAndStateMutationIsImpossible`; reflected `RegisterNonceAsync` was found | KILLED |
| A3-M02 authorization-provider substitution | `773E518F0C9853112C154669B903DD20DFCC1643D2A482CEE880E4EA210620A1` | PASS | `A3_EveryStoredGrant...SubstitutionFailsBeforeLifecycle`; expected rejection disappeared when caller claim replaced provider grant | KILLED |
| A3-M03 caller reader-version downgrade | `4E78E885E3859E5162F96B1DCC7ACCAA2E52F5F884C78FFDBEC0A5D64AE5E7AE` | PASS | `A3_CallerSelectedReaderVersion...`; descriptor request changed from pinned version to `0.9.0` | KILLED |
| A3-M04 offline-SQL evidence drift | `0069AD7667B2AF9ED3EA357BA6D9C8B093FFA6B571424A9B9D3F7418BB844050` | PASS | `A3_CheckpointSqlEvidenceMatchesMachineCapturedCanonicalResultExactly`; migration source hash differed after install call removal | KILLED |
| A2-M01 request-as-authority | `3803BD783E43F8A0200A66ADA8C2F75B0D40415917B8FA1FA456429E460B9B3B` | PASS | `A2_CallerStateVersionGrantExportAttemptAndEpochCannotBecomeTrustedFacts`; expected rejection disappeared | KILLED |
| A2-M02 lifecycle version-gate bypass | `C1076881437EDAFCB2AC1D19ECF0E3A4A70CDED5325748FDBDC5E3956B58D2DA` | PASS | `A2_EveryRowRejectsWrongStateVersion...`; wrong version no longer rejected | KILLED |
| A2-M03 reader exact-cardinality bypass | `68FDAFE556A17700B59699EF98E56C42B463A5676ED717C2CAC07E6FFD4CA10C` | PASS | `A2_DuplicateMissingUnknownOrExtraReaderFailsBeforeOracle`; duplicate reached `Single` and threw `InvalidOperationException` instead of trust failure | KILLED |
| A2-M04 readiness freshness bypass | `26B96280E870B65E77D9402517A8B2402642278363839701DCB9935126E4B81A` | PASS | `A2_NullExpiredFutureOrInvertedFreshnessReturns503OnBothRoutes`; expected 503 became 200 | KILLED |

Mutant arithmetic: 8 valid, 8 independently compiled, 8 killed by intended assertions, 0 survivors, 0 invalid, 0 residue.

## 6. Independent offline validation

| Validation | Result |
|---|---|
| Release solution build, warnings as errors | PASS; 0 warnings, 0 errors |
| Four affected Phase-A project builds, warnings as errors | PASS; each 0 warnings, 0 errors |
| Debug solution/control builds needed by fresh SQL workers | PASS; 0 warnings, 0 errors |
| A3 control-plane subset | 13 passed, 0 failed, 0 skipped |
| A3 canonical ERP subset | 3 passed, 0 failed, 0 skipped; two fresh worker processes exercised by tests |
| Complete Phase-A assembly | 63 passed, 0 failed, 0 skipped |
| Focused REV869B ERP non-PostgreSQL subset | 79 passed, 0 failed, 0 skipped |
| Complete ERP non-PostgreSQL assembly | 453 passed, 0 failed, 0 skipped |
| PostgreSQL-named tests | 87 uniquely discovered, 0 executed |
| Windows PowerShell 5.1 AST | 24 files, 0 parse errors; scripts not executed |
| EF migration discovery | `--no-connect`; 13 migrations; REV869A/REV869B unique and adjacent at positions 12/13; applied state intentionally unknown |
| Model/snapshot parity and retained SQL contracts | Included and passing in both 79- and 453-test invocations |
| Canonical SQL evidence | Three tests pass; Up 323,960 bytes / SHA `55FB...BDE22C`; Down 11,527 bytes / SHA `39A2...0E5778`; connection-open count 0 |
| Incremental exact-allowlist `git diff --check` | Exit 0 |
| Cumulative exact-allowlist `git diff --check` | Exit 0 |
| Security/secret/prohibited-operation scan | 1,240 added lines; 0 credential/private-key/real-DB/migration-apply/prohibited API hits |
| External or production operations | None; no PostgreSQL, provisioning, deployment, lifecycle, recovery, purge, export, Phase B or Correction 2 operation |

## 7. Exact test arithmetic and checkpoint reconciliation

| Invocation | Passed | Relationship |
|---|---:|---|
| Control-plane A3 filter | 13 | Subset of 63 |
| ERP canonical A3 filter | 3 | Subset of 79 and 453 |
| Complete Phase-A assembly | 63 | Unique control-plane assembly total |
| Focused REV869B ERP filter | 79 | Subset of 453 |
| Complete ERP non-PostgreSQL assembly | 453 | Unique ERP assembly total |

Exact A3 subset: `13 + 3 = 16`.

Raw pass events for actual invocations: `13 + 3 + 63 + 79 + 453 = 611`.

Unique cross-assembly total: `63 + 453 = 516`.

The three canonical tests are already members of the 79 focused and 453 full ERP sets, so they do not change unique totals. They were also separately executed, so they must count once in raw invocation events. The checkpoint's 608 statement is therefore not exact under the supplied raw-event definition.

The checkpoint's committed/worktree SHA reconciliation passes, its SQL bytes/hashes reproduce, and its source hashes detect the valid M04 drift. F07 still fails for the raw-count mismatch, parent-valued commit field, absent executable/tool artifact hashes and non-observed application count.

## 8. Boundary and retained states

No source, test, project, migration, helper or checkpoint correction was made. No PostgreSQL connection/test, migration apply, production access, provisioning, deployment, real key/credential use, lifecycle/recovery/purge/export execution, Phase B or Correction 2 activity occurred. The review created only this report.

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

Exact single next gate: a separate report-only A3 failure reconciliation. Do not begin another correction.
