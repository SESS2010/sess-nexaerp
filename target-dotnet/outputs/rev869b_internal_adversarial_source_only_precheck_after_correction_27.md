# FAIL - REV869B Correction 27 internal adversarial source-only precheck

## Verdict

Correction 27 is **not source-safety approved**. The committed implementation reproduces its structural offline counts, hashes and build results, and F23-01 remains byte-for-byte preserved. However, independent source tracing finds that the v3 evidence readers do not consistently return authoritative observed facts, the positive offline action evidence copies the frozen expected outcomes, the 108 subcases do not have subcase-specific raw fact/action payloads, the mutation harness can report success for unrelated broad exceptions, and the OR3 live evidence path is absent. These shared defects invalidate the decisive evidence for all 34 scenarios.

This report is an internal precheck and is not independent. It makes no source correction and grants no PostgreSQL, provisioning, execution-helper, database-acceptance or production approval.

## Entry gate - reproduced

| Gate | Reproduced evidence | Result |
|---|---|---|
| HEAD | `2e256d6cfd3e557e353dd3f7446000457f37290a` | PASS |
| Parent | `7e4d01f97c3eb8ac6cf402666c095fc54e49b3f1` | PASS |
| Commit subject | `Correct REV869B source evidence pipeline checkpoint 27` | PASS |
| Committed file count | exactly 9 | PASS |
| Target-scoped status | clean before review | PASS |
| Checkpoint | `outputs/rev869b_source_correction_checkpoint_27.md` | PASS |
| Checkpoint SHA-256 | `90E153BD3030AA8D28346754B1190E86739876B6CC8CC3D3010B0DF5460D5A3B` | PASS |
| Review boundary | complete review possible from committed target source and provably offline checks | PASS |

Exact committed files:

1. `outputs/rev869b_source_correction_checkpoint_27.md`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
3. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCorrection26FrozenOracle.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
9. `tools/rev869b-control-plane-install.sql`

The complete parent-to-commit diff was reconciled: 1,528 additions, 327 deletions, 1,981 zero-context diff lines. Per-file additions/deletions were checkpoint 174/0, command SQL 110/5, provisioning contract 7/2, design tests 6/6, scenario tests 18/15, source contracts 68/30, oracle 148/139, client 920/129 and control SQL 77/1. `git diff --check` passed.

## Reproduced evidence versus checkpoint claims

| Subject | Reproduced result | Adjudication |
|---|---|---|
| Build | 0 warnings, 0 errors | PASS |
| Structural inventory | 34 scenarios, 108 unique subcase identity rows, 133 formula components | PASS as inventory only |
| Raw fixture table | 133 literal component rows, 34 scenario IDs, no `SubcaseId` field | FAIL independence/exhaustiveness |
| Action fixture table | 34 scenario rows, no `SubcaseId`; 33/34 outcome/error/object tuples identical to frozen oracle scenarios | FAIL independent actual-result evidence |
| Source-contract/mutation tier | 15/15 passes | PASS as test execution; insufficient as safety proof |
| Focused non-PostgreSQL | 75/75 passes | PASS |
| Complete non-PostgreSQL | 449/449 passes | PASS |
| Model/snapshot and retained SQL | 2/2 passes | PASS |
| Pipeline mutation loop | reports 20 x 108 = 2,160 rejected | FAIL as objective proof because broad exception catch can produce false kills |
| Decisive assertion removal | every plan assertion removal is rejected by `ValidateContract` | PASS structural contract |
| Decisive value tampering | synchronous T03 loop reports rejection | PASS for the in-memory evaluator only |
| Live reader compatibility | constants, request echoes, inferred values, missing OR3 command branch | FAIL |
| Trusted adapter ownership | `EXTERNAL_PENDING` | correctly remains pending |

## Blockers

### B27-01 - v3 readers contain self-asserted, constant or caller-echoed decisive facts

This is a source defect. Fact-only means the database must calculate the value from an authoritative relation or catalog observation, not choose the value needed by the formula.

- `tools/rev869b-control-plane-install.sql:324-377`: CP-L3 emits `consumedAttemptId`, `reconciledAttemptId`, `attemptId` and `survivingAttemptId` from caller parameter `$4`; several differently named facts collapse to the same aggregate; `oldDecisionAcceptedCount` is evaluated inside a decision set already constrained to `DecisionId=$6`, making the `<>$6` branch structurally zero.
- `tools/rev869b-control-plane-install.sql:379-390`: CP-A3 emits `seededDeltaCount=1`; unhandled requested names such as `protectedMutationCount` fall through to zero. The principal/object/operation inputs do not select the decisive ACL snapshot values.
- `Rev869BCommandContextSql.cs:257-281`: TC3 emits `openedAttemptId` and `attemptId` from `$6`, uses absolute current counts as “delta” values, sets before/reference values from the same snapshot, sets `unrelatedMutationCount=0` and `acceptedSubstitutionCount=0`, and equates distinct-evidence count with outcome count rather than counting distinct evidence identities.
- `Rev869BCommandContextSql.cs:303-323`: TP3 infers deleted/remaining counts from terminal state instead of observing deletion/audit rows and emits `substitutedChildCount=0` unconditionally.
- `Rev869BCommandContextSql.cs:355-374`: TE3 emits `excludedFieldCount=0`, `laterEligibleRowCount=0`, `laterRowInBatchCount=0`, and an always-true prepared-event count expression (`row_count>=0`). It reuses one current batch hash/count for before and after references.
- `Rev869BCommandContextSql.cs:394-414`: TA3 emits `useMutationCount=0`, `dropMutationCount=0`, `targetExpectedMinusObservedCount=0` and `targetAclDimensionCount=3` rather than deriving those claims from exact observed sets. Every returned fact also receives hard-coded `sourceRowCount=1`, including aggregate facts.

Because these are decisive formula terms, canonical hashes and provenance merely authenticate an invalid claim; they do not make it authoritative.

### B27-02 - reader semantics do not match the frozen selector meanings

This is a source/interface defect derived from B27-01 but independently blocking. Several selectors with different security meanings are mapped to one count/hash. Examples include business “expected” and actual delta values from the same TC3 snapshot, current and frozen purge hashes from the same TP3 value, export before/after hashes from the same TE3 snapshot, and multiple lifecycle per-boundary counts from whole selected sets. A valid JSON/type/cardinality contract cannot compensate for the wrong reducer semantics.

The SQL observation stage is largely envelope metadata. Where a formula requires historical before-state after an action, the reader does not select a durable before-snapshot relation; it often recomputes current state or returns the same current value under two names.

### B27-03 - offline positive evidence is scenario-level copied expected data, not 108 independently authored subcase observations

This is a test-design defect.

- `Correction27RawFixtures` has 133 literal rows keyed by scenario/component only and contains no subcase key.
- `BuildDatabaseShapedRawEvidence` reuses the same scenario values for every subcase and changes only generated scope/provenance identities.
- `Correction27ActionFixtures` has one row per top-level scenario, contains no subcase identity and duplicates 33 of the 34 frozen oracle outcome/SQLSTATE/code/object tuples exactly.
- `AdaptAndVerifyDatabaseShapedEvidence` uses that table to construct `actionReached=true` and the exact terminal/error/object payload accepted by the scenario assertions.

The literal raw table is not dynamically generated from `selector.Operator`; that narrow property passes. It nevertheless is not independent decisive evidence for each subcase, and the action result is copied expectation data. Unique GUID wrappers do not make shared claimed actuals independent.

### B27-04 - the 2,160 “rejected mutations” can be false positives

This is a test-design defect. `PipelineMutationIsRejected` encloses contract validation, fixture construction, mutation targeting, parsing, adapting and verifying in one `try`, then returns `true` for any `InvalidOperationException`, `ArgumentException`, `JsonException` or `FormatException`. An unrelated fixture/setup/parser failure is therefore counted as a successful rejection of the intended mutation. The test records only a Boolean and does not require a mutation-specific rejection code, stage or component.

The separate assertion-removal checks are stronger and pass, but they do not repair the 20 x 108 pipeline claim.

### B27-05 - OR3 has no live reader command

This is a source defect. The frozen oracle assigns both T03 components to OR3 and the parser accepts OR3. `BuildReadCommand` implements CP-L3, CP-A3, TC3, TP3, TE3 and TA3 only; its default throws `Unsupported v3 database evidence reader.` Thus T03 cannot traverse the live observation pipeline. Its passing evidence exists only in the in-memory fixture builder.

### B27-06 - current source contracts assert structure/presence but do not detect B27-01 through B27-05

This is a duplicate/derived test-design failure. The tests establish exact counts, hashes, property sets and in-memory tamper sensitivity. They neither compare every SQL reducer to an independently specified authoritative query nor prove the intended mutation-specific failure reason. Consequently all offline suites can pass while the live source evidence interface remains unsound.

## PostgreSQL guard review

The guard itself is correctly ordered in the inspected source:

- `Rev869BLifecycleControllerClient.Create()` checks exact `REV869B_POSTGRES_OPT_IN` before reading endpoints, creating HTTP clients, constructing Npgsql connections or opening anything.
- `Rev869BTestDatabaseLease.CreateAsync()` calls `Create()` before allocation; connection construction/opening occurs later in `OpenVerifiedConnectionAsync()`.
- Application PostgreSQL helpers reach the same lease guard; their separate direct path also checks the exact opt-in before constructing/opening a connection.
- No `ThrowsAny`/generic-exception test was accepted as proof. This precheck used source ordering and discovery-only enumeration; it did not invoke PostgreSQL-labelled behavior methods.

Exact precheck execution counts:

`postgresql_connection_attempt_count=0`

`postgresql_command_count=0`

`postgresql_database_backed_execution_count=0`

`postgresql_labelled_behavior_test_execution_count=0`

The synchronous T03 source/mutation method was selected by exact fully qualified name after its body was inspected to contain no `Create`, Npgsql, HTTP or external call. The other 33 scenario methods were discovery-only.

## Complete 34-scenario matrix

The subcase and selector counts are independently reproduced from the frozen arrays. Every row fails source safety because at least one decisive actual-evidence path is constant/echoed/inferred/shared or absent. No PostgreSQL behavior is inferred.

| ID | Subcases | Terms | Expected | Independent source adjudication | Result |
|---|---:|---:|---|---|---|
| P01 | 1 | 3 | ExternalVerified | CP-A3/TA3 mismatch terms include derived/default-zero claims; action outcome is copied fixture | FAIL |
| P02 | 5 | 3 | PreflightDenied | one shared scenario fixture covers five distinct pin mutations; CP-A3 mismatch and CP-L3 zero claims are not subcase-specific | FAIL |
| P03 | 4 | 4 | VerificationDenied | CP-A3 `seededDeltaCount=1`, default-zero protected count and same snapshot hashes self-prove drift | FAIL |
| L01 | 3 | 3 | Ready | CP-L3 whole-set counts/XOR cannot prove each boundary; one shared fact payload covers three subcases | FAIL |
| L02 | 6 | 5 | Ready | “per boundary” terms are whole selected counts; TA3 role-set term collapses to `fact_count>0` | FAIL |
| L03 | 5 | 5 | DropStarted | physical chain/authorization registration reducers do not prove the exact chain; one action fixture covers five races/bindings | FAIL |
| L04 | 5 | 5 | Finalized | per-boundary terms are not boundary-keyed; TA3 target/role observations are not durable boundary records | FAIL |
| L05 | 5 | 3 | Quarantined | TA3 use/drop mutation counts are constant zero; shared scenario evidence cannot distinguish five steps | FAIL |
| R01 | 1 | 5 | Finalized | consumed attempt is caller `$4`; action/reference values can echo the same selected/input values | FAIL |
| R02 | 8 | 3 | RecoveryAuthorized | shared zero/count fixture does not distinguish wrong/expired/replayed/foreign/version/action/nonce/valid subcases | FAIL |
| R03 | 5 | 5 | Finalized | `oldDecisionAcceptedCount` is structurally zero inside `$6`-selected set; no independent old-decision observation | FAIL |
| C01 | 1 | 5 | Committed | expected and actual deltas derive from the same current TC3 snapshot; no independent expected fixture count | FAIL |
| C02 | 1 | 5 | Committed | before/after hashes, receipt and response references reuse the same current TC3 values | FAIL |
| C03 | 1 | 4 | RequestRegistered | changed and registered digest come from the same request snapshot while “delta” fields are absolute counts | FAIL |
| C04 | 5 | 4 | RolledBack | one scenario-level zero/rollback fixture covers five rollback boundaries; SQL uses current absolute counts | FAIL |
| C05 | 1 | 3 | RolledBack | opened attempt/attempt reference echoes `$6`; rollback evidence is not independently bound to before-state | FAIL |
| C06 | 4 | 3 | FourExactInterruptionOutcomesReconciled | distinct evidence count equals outcome count, not distinct evidence IDs; one fact set covers four interruptions | FAIL |
| C07 | 1 | 4 | AttemptStarted | start/attempt counts are current aggregates and `unrelatedMutationCount` is constant zero | FAIL |
| C08 | 8 | 4 | AttemptStarted | `acceptedSubstitutionCount` is constant zero and one fact payload covers eight substitution classes | FAIL |
| G01 | 5 | 3 | Denied | one shared zero fixture covers five invalid authorizations; no subcase-specific rejected-input observation | FAIL |
| G02 | 1 | 4 | ZeroRows | deleted/remaining evidence is inferred from state rather than independently observed deletion/audit facts | FAIL |
| G03 | 1 | 5 | Succeeded | deleted count is inferred as candidate count on success; remaining zero is inferred, not queried | FAIL |
| G04 | 1 | 4 | Failed | current/frozen/context comparisons reuse current TP3 snapshots; failure does not prove preserved context | FAIL |
| G05 | 3 | 3 | Failed | one payload covers fault/rollback/durable subcases; deleted/context results are inferred/shared | FAIL |
| G06 | 4 | 5 | Failed | `substitutedChildCount=0` is constant; shared counts cannot distinguish concurrency/retry substitutions | FAIL |
| E01 | 1 | 4 | Prepared | excluded-field count is constant zero and prepared-event count is effectively always one | FAIL |
| E02 | 1 | 4 | Prepared | later eligible and later-in-batch counts are constant zero; expected later eligible result is not observed | FAIL |
| E03 | 4 | 3 | Denied | one scenario fixture covers four invalid releases; before/after batch hash comes from the same current batch | FAIL |
| E04 | 3 | 5 | ReleaseRetrySequenceVerified | one shared action/fact payload covers three release steps; batch before/after hash is the same current value | FAIL |
| A01 | 1 | 3 | Verified | control/target expected-minus-observed terms are default/constant zero; dimension count is constant three | FAIL |
| A02 | 7 | 3 | Denied | seven denial classes reuse one fact/action payload; denial count is inferred solely from effective permission | FAIL |
| T01 | 1 | 4 | InUse | some identity/admin facts are queried, but the scenario is still coupled to shared copied action evidence and flawed TA3 provenance | FAIL |
| T02 | 1 | 5 | Finalized | reconciled/surviving attempt references echo `$4`; durable restart identity is not independently observed | FAIL |
| T03 | 4 | 2 | MutationSensitive | OR3 has no live command branch; broad catch permits false mutation kills; one raw payload covers four mutation subcases | FAIL |
| **Total** | **108** | **133** | **34 IDs** | **shared blockers B27-01 through B27-06** | **0 PASS / 34 FAIL** |

## Offline validation reproduced

| Check | Command/evidence | Result |
|---|---|---|
| Build | `dotnet build tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-restore --configuration Debug` | PASS: 0 warnings, 0 errors |
| Source contracts + synchronous mutation | exact class filter plus exact T03 FQN | PASS: 15/15; not sufficient for safety |
| Focused non-PostgreSQL | `FullyQualifiedName~Rev869B&FullyQualifiedName!~Postgres` | PASS: 75/75 |
| Complete non-PostgreSQL | `FullyQualifiedName!~Postgres` | PASS: 449/449 |
| Scenario discovery | `--list-tests`, scenario class only | PASS: exactly 34, executed 0 |
| Model/snapshot + retained SQL | exact two no-connect test FQNs | PASS: 2/2 |
| PowerShell AST | Windows PowerShell 5.1.19041.6456 parser | PASS: 24 scripts, 0 errors |
| EF discovery | `dotnet ef migrations list ... --no-connect` with inert `127.0.0.1:1` design value | PASS: 13 discovered, no connection |
| Migration uniqueness/order | exact primary/designer filename scan | PASS: one each; REV869B immediately after REV869A |
| Complete committed diff | `git diff --check parent..commit -- .` | PASS |
| Secret/privacy | complete added-line regex scan | PASS: 0/0 |

Pinned offline SQL was regenerated by `Correction27OfflineUpDownSqlIsGeneratedWithoutConnectingAndHasPinnedHashes` within the 15-test source-contract tier:

- Up: 322,999 UTF-8 bytes, 2,635 lines, SHA-256 `B4D22AB600F2F7B27A8ACBD417B067ACC5D8A1488E513F562BEAAAD146781F1C` - PASS.
- Down: 11,700 UTF-8 bytes, 231 lines, SHA-256 `268D0FC8FCE08B7F3ADBE378879AD0A325965F784A87FC987D2BAF2FAFA42131` - PASS.
- Frozen oracle computed SHA-256: `6a1196cdad0bcbb086c771efb4f46f9b15db86aaabf6a1ff89e67afca5383bda` - PASS as integrity, not as actual-evidence independence.
- F23-01 slice: 11,001 UTF-8 bytes, SHA-256 `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523` - PASS_RETAINED.

## Architecture, ACLs and regression boundary

The frozen architecture remains valid and unchanged: external provisioning, dedicated lifecycle controller, surviving control-plane database and target-local transactional ledgers are retained. Correction 27 did not redesign purchase workflow, permissions, approvals, calculations or audit-history production paths.

ACL ownership and grant boundaries remain syntactically retained: v3 functions use fixed search paths, PUBLIC execution is revoked and verifier execution grants remain bounded. This does not approve the ACL evidence claims: CP-A3/TA3 reducer semantics in B27-01 are insufficient to prove exact effective closure. The architecture and ACL boundary should be retained while the evidence interface is reconciled.

## External prerequisites

Still external and blocking for later execution:

- management assignment and review of the production trusted-adapter owner;
- isolated control-plane and target PostgreSQL databases;
- externally provisioned database identities, roles, ownership and ACLs;
- pinned controller/verifier TLS and signing identities;
- independently prepared fixtures/actions for all 108 subcases;
- authorized installation/application of the committed SQL and migration;
- later PostgreSQL behavioral acceptance and durable audit evidence;
- production authorization, which is not implied by any source result.

## Prohibited-action confirmation

No source/test/helper/SQL/migration file was changed. No PostgreSQL connection was attempted, no PostgreSQL command was sent, no database-backed test ran, and no provisioning, migration apply/remove, lifecycle, purge, recovery, quarantine, export, production, credential, identity or ACL operation ran. No legacy-reference path was accessed. No Correction 28 was implemented or started. Git history was not rewritten.

## Single next gate

The single next authorization gate is **management authorization for one report-only Correction 27 failure reconciliation** covering B27-01 through B27-06 and the 34-row failure matrix. It must define a bounded future correction and exact allowlist; implementation is not authorized by this precheck.

correction_27_internal_precheck_state=FAIL
correction_27_internal_precheck_independence_state=NOT_INDEPENDENT
f23_01_state=PASS_RETAINED
f23_02_internal_precheck_state=FAIL
correction_27_scenario_pass_count=0
correction_27_scenario_fail_count=34
formula_term_inventory_state=PASS_133_OF_133
formula_term_authoritative_evidence_state=FAIL
evidence_pipeline_implementation_state=FAIL
trusted_adapter_production_ownership_state=EXTERNAL_PENDING
frozen_architecture_state=RETAIN
acl_boundary_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN