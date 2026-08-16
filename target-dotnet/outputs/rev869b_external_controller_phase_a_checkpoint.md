# REV869B Option-A Phase-A Correction A2 checkpoint

Date: 2026-08-16
Checkpoint type: source-only implementation handoff pending independent review
Authorization: `PHASE_A_CORRECTION_A2_SOURCE_ONLY_GATE=APPROVED`
Starting HEAD: `12cff947a3928717e50e5357fa41c4f1c62aaf0d`
Starting parent: `82e1d7052576f8715ff76ccecab13540eea47bff`
Ending commit: the single Correction-A2 commit containing this checkpoint. A commit cannot contain its own SHA-1 without changing that SHA-1; the exact authoritative identifier is the post-commit `git rev-parse HEAD` reported in the final handoff.
Authoritative reconciliation: `outputs/rev869b_external_controller_phase_a_correction_a1_failure_reconciliation.md`
Reconciliation SHA-256: `B108365830F6CE2AE1ED97835980601484A7C1AE749048AFC4535457DCC360A3`
Architecture-freeze SHA-256: `3F0BC461865D69E3D9827D763D7C403E3BD4E82ECF488AE4FDF3E48D9722DDB8`

## Verdict and scope

`PHASE_A_CORRECTION_A2_SOURCE_IMPLEMENTATION=COMPLETE_PENDING_INDEPENDENT_REVIEW`

F02-F07 were corrected within the approved frozen Phase-A architecture. F01 raw-only ingress controls remain intact. No Phase B, Correction 2, PostgreSQL execution, migration application, provisioning, deployment, production access, real key/credential use, or lifecycle/recovery/purge/export operation was performed.

The checkpoint records source implementation evidence only. It does not declare Phase-A management acceptance or production readiness.

## Exact changed-file boundary

Exactly these ten allowlisted files belong to Correction A2:

1. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
2. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
3. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
4. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
5. `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`
6. `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`
7. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
8. `src/SESS.NexaERP.AcceptanceVerifier/Program.cs`
9. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
10. `outputs/rev869b_external_controller_phase_a_checkpoint.md`

No file outside this list was created, modified, staged, or committed. The A1 independent-review and failure-reconciliation reports remain byte-for-byte immutable.

## Finding implementation map

| Finding | Result | Production correction and enforcement |
|---|---|---|
| F02 authoritative ownership/provenance | PASS pending independent review | One composite `IDurableControlPlanePersistenceProvider` supplies the authoritative lifecycle/version/grant/export/attempt/lease snapshot and owns the atomic outcome. The runtime constructor rejects separately injectable durable facets. See contracts lines 1065, 1524; raw authority lines 764-888; ownership validator lines 229-242. |
| F03 lifecycle/authorization/lease/atomicity | PASS pending independent review | Raw claims are comparison-only. The state machine binds snapshot identity, state, version, grant operation/state, original cancellation subject and role, export substate, attempt, evidence, lease/epoch/fence/holder/expiry. Only `FIRST_OWNER` and `COMPLETED_REPLAY` exact composite results can return. See state machine lines 224-392 and raw authority lines 847-888. |
| F04 reader/oracle closure | PASS pending independent review | The verifier resolves a server-owned expectation, enforces exact-one reader cardinality, calls each authoritative reader exactly once, byte-compares caller transport to the returned signed bundle, binds snapshot/watermark and all scope/operation/attempt fields, applies stricter configured/descriptor/global limits, and sends only verified reader facts to the oracle. See verifier lines 515-653 and 791-830. |
| F05 readiness/audit/privacy | PASS pending independent review | READY requires the frozen policy, exact unique dependency set, matching version/identity, and non-null ordered fresh timestamps. Both hosts use one 200/503 guard. Evidence and audit receipts are exact and precede verdict success; atomic lifecycle output must include audit outbox success and exact proposed receipt fields. See contracts lines 23-120 and 1332-1360; verifier lines 670-725; raw authority lines 867-888. |
| F06 independent assurance | PASS pending independent review | A literal assurance-owned 26-concept/30-concrete-row oracle replaces production-derived lifecycle expectations. Fifty Phase-A tests traverse raw production authorities and instrumented owner boundaries. Four actual production gate-bypass mutants compiled and were killed by their intended assertions. |
| F07 checkpoint integrity | PASS pending independent review | This checkpoint replaces the stale A1 checkpoint, uses executable command text, separates invocation events from unique tests, records exact hashes/results, and passes whitespace/conflict-marker and exact-range checks. Historical review reports and commits were not edited. |

## F01 preservation

- Public protected authority methods remain only `IControlPlaneAuthority.AcceptRawCommandAsync` and `IAcceptanceVerifierAuthority.VerifyRawAsync`.
- Former typed verification/command services remain non-public.
- Canonical header, payload, evidence, signature, issuer, audience, subject, role, scope, nonce, freshness, idempotency, tenant, database, resource, version, attempt, lease and fence checks remain fail closed.
- No public mutating HTTP endpoint was added. Both hosts retain health/version-only route surfaces.
- The literal 14-owner catalog remains exact and the effective Control Plane constructor has one composite durable owner.

## Test inventory and counting basis

Formal final validation invocations:

| Invocation | Passed | Failed | Skipped | Counting role |
|---|---:|---:|---:|---|
| A2-named Phase-A tests | 31 | 0 | 0 | A2 subset |
| Complete Phase-A contract-test project | 50 | 0 | 0 | unique Phase-A assembly |
| Focused REV869B non-PostgreSQL ERP tests | 76 | 0 | 0 | focused subset of ERP assembly |
| Complete ERP non-PostgreSQL suite | 450 | 0 | 0 | unique ERP assembly |
| Model/snapshot/offline-SQL focused rerun | 3 | 0 | 0 | subset of the 450 ERP tests |

Counting methodology:

- Raw passed test events across the five formal invocations: `31 + 50 + 76 + 450 + 3 = 610`.
- Unique Phase-A tests: `50`.
- Unique A2 tests: `31`, all contained in the 50 Phase-A tests.
- Unique ERP non-PostgreSQL tests: `450`.
- Unique tests across both distinct test assemblies: `50 + 450 = 500`.
- Focused REV869B tests: `76`, contained in the 450 ERP tests.
- The three parity/SQL tests are contained in the 450 ERP tests.
- Therefore focused and parity reruns are not added to unique totals.

The historical A1 review's `556` aggregate is not authoritative. Its corrected A1 raw-event sum was `583`, but that historical figure does not include A2. The earlier reconciliation called `450` the overall unique total while separately listing a distinct 27-test Phase-A assembly; this checkpoint resolves that ambiguity by reporting per-assembly unique counts and the cross-assembly total.

## Decisive production mutants

Each designated mutant was a temporary patch to an allowlisted production enforcement point. Each compiled with zero warnings/errors, failed its named test for the intended assertion, and was reversed. The final 50-test Phase-A run passed after cleanup.

| ID | Patch SHA-256 | Production gate | Build | Intended killer and result |
|---|---|---|---|---|
| A2-M01-REQUEST-AS-AUTHORITY | `CE4AFCE751F951666DF5CAC2247DBE3A0854CF518968A6E5C606C49A55E5F6AF` | Raw authority snapshot state/version acquisition around lines 764-846 | exit 0 | `A2_CallerStateVersionGrantExportAttemptAndEpochCannotBecomeTrustedFacts`; exit 1, expected trust rejection but no exception was thrown |
| A2-M02-LIFECYCLE-GATE-BYPASS | `4D8B434E8EEAD40E381D0AEB7F58BD02798602B27422F7097A820351383E3C64` | State-machine authoritative/current version conjunctions around lines 237-249 | exit 0 | `A2_EveryRowRejectsWrongStateVersionRoleScopeGrantEvidenceLeaseFenceEpochAttemptAndAudit`; exit 1, expected trust rejection but no exception was thrown |
| A2-M03-READER-CLOSURE-BYPASS | `F92C460B4BCADEC5BAC61896BEA25DFDB711A3D601A3AEE118606D84BBEFF68D` | Exact-one reader cardinality around verifier lines 522-531 | exit 0 | `A2_DuplicateMissingUnknownOrExtraReaderFailsBeforeOracle`; exit 1, expected `READER_DUPLICATE`, actual `EVIDENCE_TAMPERED` |
| A2-M04-READINESS-AUDIT-BYPASS | `3DF82969490E5F8E960A240AD3CA03FAC56B64F4773784DB11C668C7F278124B` | Readiness normalization and executable predicate around contract lines 83-92 and 1337-1347 | exit 0 | `A2_NullExpiredFutureOrInvertedFreshnessReturns503OnBothRoutes`; exit 1, expected HTTP 503, actual 200 |

M04 required removal of both normalization and predicate enforcement to constitute the specified effective bypass. Removing only explicit null checks did not bypass nullable comparison behavior and was not counted as a decisive mutant. Final decisive mutants: 4 compiled, 4 killed by intended assertions, 0 survived, 0 left in the tree.

## Offline validation evidence

| Validation | Result |
|---|---|
| Affected Phase-A project warning-as-error build | exit 0; 4 projects; 0 warnings; 0 errors |
| Complete solution warning-as-error build | exit 0; 5 projects; 0 warnings; 0 errors |
| A2 tests | 31 passed; 0 failed; 0 skipped |
| Complete Phase-A suite | 50 passed; 0 failed; 0 skipped |
| Focused REV869B non-PostgreSQL | 76 passed; 0 failed; 0 skipped |
| Complete ERP non-PostgreSQL | 450 passed; 0 failed; 0 skipped |
| PostgreSQL-named discovery only | 87 discovered; 87 unique; 0 executed |
| PowerShell 5.1 AST | version 5.1.19041.6456; 24 scripts; 0 parse errors; 0 executed |
| EF migration discovery | exit 0; `--no-connect`; inert `127.0.0.1:1`; 13 migrations; applied state unknown |
| REV869A/REV869B uniqueness/order | one REV869A and one REV869B; ordinals 12 and 13 of 13; adjacent |
| Model/snapshot and generated SQL contracts | 3 passed; 0 failed; 0 skipped |
| Offline REV869B Up SQL | 326,596 UTF-8 bytes; SHA-256 `1F043EC09F391970C111EFBBBF8C1C8A750DBC11DC4E776015841FC258A6FC21` |
| Offline REV869B Down SQL | 11,759 UTF-8 bytes; SHA-256 `18F834FDFE50270F0C7E7C01744176755CF7FC9F7BB1E6896E70604CF695EBF8` |
| Added production-line security scans | 435 added lines; 0 hard-coded credential, private-key, database/migration-action, process/network-client, sensitive-logging, Phase-B, public typed-command, or mutating-endpoint hits |
| Incremental and cumulative diff checks | required final commands below; both must be exit 0 before and after commit |
| PostgreSQL execution | exactly 0 |

Exact principal validation commands:

```powershell
dotnet build tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-restore -warnaserror
dotnet build SESS.NexaERP.slnx --no-restore -warnaserror
dotnet test tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~ArchitectureFreezeContractTests.A2_&FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Rev869B&FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --list-tests --filter "FullyQualifiedName~Postgres" --logger "console;verbosity=minimal"
```

EF discovery used `dotnet ef migrations list --no-connect` with an inert loopback connection string and matching inert expected-database name. Up and Down SQL were generated in memory with `dotnet ef migrations script`, hashed, and discarded. No database connection or migration application occurred.

Final immutable-range checks:

```powershell
git diff --check 12cff947a3928717e50e5357fa41c4f1c62aaf0d -- <exact-ten-file-allowlist>
git diff --check 51476760adcea9ed7babbc04d642e53e371c6941 -- <exact-ten-file-allowlist>
git diff --check HEAD^..HEAD -- <exact-ten-file-allowlist>
git diff --check 51476760adcea9ed7babbc04d642e53e371c6941..HEAD -- <exact-ten-file-allowlist>
```

## Source/test artifact SHA-256 before checkpoint commit

| File | SHA-256 |
|---|---|
| `Rev869BControllerMessagesV1.cs` | `0807E5D103B96FCFCFEC15FD47688D3345158EC8763BF6F15FB505E7A1441CE7` |
| `Rev869BExecutionBinding.cs` | `51B8E996DBBFC8F448DB999A3FAEA2E737549DD4BA969E38031B2A20A464F7AC` |
| `Rev869BControllerStateMachine.cs` | `FCCDF6A5279AD150D4DB7D4061C3AA55BA08D3AD72C4313792437E7A400C04DD` |
| `SignedEnvelopeService.cs` | `BEAD1BB7937A2FF2013AEAE6C7E62AA08ADE9095A5E83FE3D5E1812693E98587` |
| `AcceptanceVerifierOptions.cs` | `9A222C06C2BBE08278041C01D29D8271C4D94BAD0B2E1A413892A2005894548C` |
| `ClosedEvidenceVerifierV1.cs` | `903C6AA520AB09F12976062101698BF5F47A426DA9E5801F4490E30E17BC4DBF` |
| `ControllerContractEndpointsV1.cs` | `10CCFCB1C611B62DF2B167B5E2223531BE0FF3937B0ACA43D2DA1A2E87A2376F` |
| `AcceptanceVerifier/Program.cs` | `8A534C9F7F5566ECD72B9463C074036A3A36BE96AEAF4C6CD3FA0951B2B41BA9` |
| `ArchitectureFreezeContractTests.cs` | `36F16991930C53E9013A3CE124AC4DE44D771E13232A60D48DD731690C482309` |

## Integrity and prohibited-operation confirmation

- Independent-review report remains SHA-256 `9320CAD73798099548C8DB1ABA503870AAC2E11D852AA2AD0DCD28709A60A0AD`.
- Reconciliation remains SHA-256 `B108365830F6CE2AE1ED97835980601484A7C1AE749048AFC4535457DCC360A3`.
- Historical report formatting/count defects remain immutable and are disclosed rather than rewritten.
- No PostgreSQL scenario was executed.
- No migration was applied.
- No production, provisioning, deployment, network, external infrastructure, real credential, key, trust root, lifecycle, recovery, purge, or export operation was accessed or performed.
- `../legacy-reference/` was not accessed or modified.
- Phase B and Correction 2 remain out of scope and `NO_GO`.

## Remaining external prerequisites and risks

Source correction does not supply deployed durable persistence, real workload identity/IAM, private networking, issuer/policy stores, non-exportable KMS/HSM keys, authoritative least-privilege evidence readers, pinned production oracle artifacts, WORM audit/evidence storage, HA/failover, PostgreSQL behavioral/concurrency/rollback/restart/PITR evidence, backup/restore/DR, scale/load/chaos evidence, monitoring, runbooks, training, or production approval.

Primary review risks are provider implementations that violate the new composite contract, deployment identity/version drift, readers returning caller-influenced facts, audit stores returning non-exact receipts, and future tests deriving expected rows from production. Acceleration should retain the literal fixtures and fast A2 subset during development, then run one complete final non-PostgreSQL gate; overlapping invocations must never be summed as unique coverage.

## Retained states and exact next gate

`phase_a_correction_a2_source_implementation_state=COMPLETE_PENDING_INDEPENDENT_REVIEW`

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

Exact single next gate: a fresh independent report-only source architecture and security review of the committed Correction-A2 diff. That review must be separate from this implementation.
