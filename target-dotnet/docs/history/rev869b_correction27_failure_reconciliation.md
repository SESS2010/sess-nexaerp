# REV869B Correction 27 failure reconciliation

## Decision

**Reconciliation PASS; proposed bounded Correction 28 source authorization GO.**

PASS means the Correction 27 failure is completely understood, mapped and bounded. It does not mean Correction 27, REV869B, the execution helper or any database behavior has passed. Correction 28 implementation remains separately unauthorized.

The seven management-listed blocker families are confirmed. They reduce to four source/interface defects, two test-design defects and one derived coverage failure. All can be corrected without changing the frozen architecture, ACL ownership boundary, F23-01 logic, migration identity/order or production business workflows. Target-reader correction necessarily changes generated Up SQL and likely Down SQL; therefore the old Correction 27 SQL hashes cannot remain acceptance pins. Correction 28 must generate and pin new offline hashes without applying a migration.

## Entry gate

| Gate | Reproduced evidence | Result |
|---|---|---|
| Authorized HEAD | `16528851cd6971a286f2c1705e80ce0a3d061b3e` | PASS |
| Expected parent / Correction 27 | `2e256d6cfd3e557e353dd3f7446000457f37290a` | PASS |
| Correction 27 parent | `7e4d01f97c3eb8ac6cf402666c095fc54e49b3f1` | PASS |
| HEAD content | exactly `outputs/rev869b_internal_adversarial_source_only_precheck_after_correction_27.md` | PASS: 1 file |
| Failed-precheck SHA-256 | `B66A421B34D5AD50EAE99393553BE1DDDC1C1E4426F78C73970770BFDEF59022` | PASS |
| Precheck verdict | FAIL; 0/34 scenarios PASS | PASS |
| Target-scoped status | clean at entry | PASS |
| PostgreSQL connections / commands / database executions | 0 / 0 / 0 | PASS |
| Immediate lineage | report commit immediately follows the nine-file Correction 27 implementation | PASS |

## Commit lineage and authoritative artifacts

| Artifact | Commit / hash | Authority in this reconciliation |
|---|---|---|
| Correction 27 source | `2e256d6cfd3e557e353dd3f7446000457f37290a` | reviewed implementation |
| Correction 27 checkpoint | SHA-256 `90E153BD3030AA8D28346754B1190E86739876B6CC8CC3D3010B0DF5460D5A3B` | implementation claims, not accepted without review |
| Internal adversarial precheck | commit `16528851cd6971a286f2c1705e80ce0a3d061b3e`; SHA-256 `B66A421B34D5AD50EAE99393553BE1DDDC1C1E4426F78C73970770BFDEF59022` | authoritative failed findings |
| Correction 26 reconciliation | SHA-256 `5265F2F4C874888821385AB05598462D4E961551DF61EA0DDCD7987CE279FE13` | Option A architecture and prior finding identity |
| F23-01 accepted slice | `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523` | immutable preservation gate |
| Correction 27 oracle payload | `6a1196cdad0bcbb086c771efb4f46f9b15db86aaabf6a1ff89e67afca5383bda` | expectation integrity only |
| Correction 27 Up / Down | `B4D22AB600F2F7B27A8ACBD417B067ACC5D8A1488E513F562BEAAAD146781F1C` / `268D0FC8FCE08B7F3ADBE378879AD0A325965F784A87FC987D2BAF2FAFA42131` | reproduced old pins; must change if target SQL is corrected |

## Reconciled finding register

| Stable ID | Classification | Exact failure | Scope | Required correction |
|---|---|---|---|---|
| `C27F-01` | SOURCE_DEFECT | CP-L3/CP-A3/TC3/TP3/TE3/TA3 emit decisive constants, caller echoes, inferred outcomes or semantically collapsed aggregates | 131 database-reader terms; all non-T03 scenarios | replace every defective reducer with an exact relation/catalog observation or a verifier derivation from independently observed primitives; fail closed if unavailable |
| `C27F-02` | SOURCE_DEFECT | historical before/after claims reuse a current snapshot or two aliases of one value | equality/delta/fingerprint formulas across P03, C01-C03, G03-G05, E01-E04, A02 and related rows | add stage-specific immutable snapshot/history sources and bind transaction/evidence identity; never reconstruct before-state from current state |
| `C27F-03` | TEST_DESIGN_DEFECT | 133 raw fixtures are keyed only by scenario/component; 34 action fixtures are keyed only by scenario, so 108 subcases receive shared claimed actuals | 108/108 subcases | introduce an independently compiled fixture catalog with one raw/action/history provenance record per subcase and unique content/provenance hashes |
| `C27F-04` | TEST_DESIGN_DEFECT | 33/34 action tuples copy the frozen oracle outcome/SQLSTATE/code/object tuple; adapter sets `actionReached=true` | 33 scenarios directly; T03 uses placeholders | separate actual fixture authorship from oracle types/data; require independence mutations where changing either side alone fails |
| `C27F-05` | TEST_DESIGN_DEFECT | `PipelineMutationIsRejected` treats broad setup/parser/adapter exceptions as successful intended mutation rejection | claimed 2,160 mutations and T03 | return structured rejection results; require exact mutation ID, boundary, component/stage and expected rejection code; unrelated exceptions fail the harness |
| `C27F-06` | SOURCE_DEFECT | OR3 is accepted by parser/oracle but absent from the live observation dispatch / `BuildReadCommand` path | T03 and four subcases | implement a non-PostgreSQL live OR3 observation command over immutable structured mutation-run records; do not route OR3 through Npgsql |
| `C27F-07` | DUPLICATE_OR_DERIVED_FAILURE | existing source contracts prove inventory/presence/in-memory success but not reducer meaning, independent provenance or intended rejection reason | 15/15, 75/75 and 449/449 false confidence | add independent reader-semantic, fixture-independence, structured-mutation and OR3 dispatch contracts |

## Exact finding-to-file/type/method mapping

| Finding | File | Exact symbol/slice | Gap |
|---|---|---|---|
| C27F-01/C27F-02 | `tools/rev869b-control-plane-install.sql` | `rev869b_read_lifecycle_facts_v3`, `rev869b_read_control_acl_facts_v3` | `$4` identity echoes, collapsed lifecycle aggregates, structural/default constants, no immutable historical selector source |
| C27F-01/C27F-02 | `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | `rev869b_read_command_facts_v3`, `rev869b_read_purge_facts_v3`, `rev869b_read_export_facts_v3`, `rev869b_read_target_acl_facts_v3`, `rev869b_build_raw_facts_v3` | absolute counts labeled deltas, inferred deletes, current-snapshot aliases, constants, hard-coded `sourceRowCount=1` |
| C27F-01 | `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs` | function inventory / hashes / role contract | pins function presence but not selector reducer semantics |
| C27F-03/C27F-04 | `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | `Correction27RawFixtures`, `Correction27ActionFixtures`, `RawFixtureSpec`, `IndependentActionFixture`, `BuildDatabaseShapedRawEvidence`, `BuildRawDocument`, `AdaptAndVerifyDatabaseShapedEvidence` | scenario-only keys and copied outcomes produce positive evidence |
| C27F-05 | same | `PipelineMutationIsRejected`, `MutateRawObservation`, `FailingMutationValue` | broad exception catch returns Boolean success without exact rejection identity |
| C27F-06 | same | `ObserveAsync`, `RequiredRawFacts`, `BuildReadCommand`, `MutationRunObservationV3` | parser type exists, dispatch command does not |
| C27F-03/C27F-07 | `tests/SESS.NexaERP.Tests/Rev869BCorrection26FrozenOracle.cs` | `Scenarios`, `Subcases`, `Selectors`, `Validate` | expectation integrity is strong but no independent actual-fixture catalog/hash relationship |
| C27F-03/C27F-07 | `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | `S`, `Plan`, `Assertions`, `Mutations`, `Surface`, `BeforeSurface`, `AfterSurface` | structural bijection does not require subcase-specific actual provenance or temporal source |
| C27F-05/C27F-06/C27F-07 | `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | `T03_EveryScenarioActionQueryAssertionAndCleanupIsMutationSensitive`, `RunAsync` | Boolean mutation acceptance and in-memory OR3 path mask live dispatch gap |
| C27F-01 through C27F-07 | `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | `AcceptanceInventoryHasExactlyThirtyFourUniqueIndependentEvidencePlans`, `Correction27FactPipelineIsBijectiveLocallyEvaluatedAndTamperSensitive`, `Correction27OfflineUpDownSqlIsGeneratedWithoutConnectingAndHasPinnedHashes` | asserts tokens/counts and positive fixture behavior, not authoritative reducer semantics or failure provenance |

## Evidence taxonomy required by Correction 28

- **Database-observed fact:** exact value read from a named authoritative relation/catalog with complete organization/instance/lease/version/operation/attempt/batch/stage scope and observed row count/hash.
- **Action-observed fact:** actual signed action receipt bound to the unique subcase preparation/action ID; never constructed from the oracle or expected-result table.
- **Historical fact:** immutable pre-action snapshot or durable history/audit record with its own evidence ID and transaction boundary; never an alias of a current query.
- **Verifier-derived value:** deterministic reduction over independently observed primitive facts, declared in a closed mapping catalog; the database must not emit PASS or the expected result.
- **Oracle expectation:** immutable expected operator/outcome stored only in the oracle; it may be read only after actual observation adaptation is complete.

No fact may change category implicitly. If an observation is unavailable, the verifier must reject missing evidence rather than substitute zero, one, an input parameter or a current snapshot.

## Complete 133-term classification and missing-observation register

The component ID makes duplicate selector names scenario-specific. `Observed-current` is not itself a failure, but its scenario still fails C27F-03/C27F-04 until independent subcase action/raw provenance exists.

| Component | Reader / selector | Current classification | Missing independent observation or correction |
|---|---|---|---|
| `P01:formula-pin-mismatch` | `CP-A3 / pinMismatchCount` | Semantically collapsed manifest comparison | Exact per-dimension observed-minus-expected set and hash scoped to the subcase mutation |
| `P01:formula-target-acl-delta` | `TA3 / targetAclDeltaCount` | Database-observed current catalog/identity fact; subcase actual still shared | Retain exact scoped observation and bind a unique subcase raw/action provenance record |
| `P01:formula-verify` | `CP-A3 / verificationMismatchCount` | Semantically collapsed manifest comparison | Exact per-dimension observed-minus-expected set and hash scoped to the subcase mutation |
| `P02:formula-pin-mismatch` | `CP-A3 / pinMismatchCount` | Semantically collapsed manifest comparison | Exact per-dimension observed-minus-expected set and hash scoped to the subcase mutation |
| `P02:formula-lease-zero` | `CP-L3 / allocatedLeaseCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `P02:formula-action-zero` | `CP-L3 / lifecycleMutationCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `P03:formula-seeded-one` | `CP-A3 / seededDeltaCount` | Constant 1 | Count of the exact seeded delta relation/catalog tuples bound to the subcase |
| `P03:formula-reported-delta` | `CP-A3 / reportedDeltaSha256` | Reused current ACL snapshot | Separate immutable seeded/current/baseline snapshots with evidence IDs |
| `P03:formula-protected-zero` | `CP-A3 / protectedMutationCount` | Default constant 0 | Durable protected-object mutation/audit count for the exact action interval |
| `P03:formula-cleanup-baseline` | `CP-A3 / cleanupFingerprint` | Reused current ACL snapshot | Separate immutable seeded/current/baseline snapshots with evidence IDs |
| `L01:formula-reserved` | `CP-L3 / reservedEventCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `L01:formula-branch-xor` | `CP-L3 / resumeSameAttempt_xor_authorizedCleanup` | Inferred or whole-set aggregate labeled as exact chain/boundary | Boundary-keyed immutable event/attempt/outcome chain rows and exact transition join |
| `L01:formula-duplicates-zero` | `CP-L3 / duplicateAttemptCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `L02:formula-boundary-count` | `CP-L3 / boundaryCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `L02:formula-started-each` | `CP-L3 / startedAttemptsPerBoundary` | Inferred or whole-set aggregate labeled as exact chain/boundary | Boundary-keyed immutable event/attempt/outcome chain rows and exact transition join |
| `L02:formula-reconciled-each` | `CP-L3 / reconciledAttemptsPerBoundary` | Inferred or whole-set aggregate labeled as exact chain/boundary | Boundary-keyed immutable event/attempt/outcome chain rows and exact transition join |
| `L02:formula-target-each` | `TA3 / targetCountPerBoundary` | Current count/boolean mislabeled per-boundary | Boundary-keyed target existence and exact role-set snapshot in the surviving control plane |
| `L02:formula-roles-each` | `TA3 / roleSetCountPerBoundary` | Current count/boolean mislabeled per-boundary | Boundary-keyed target existence and exact role-set snapshot in the surviving control plane |
| `L03:formula-requests` | `CP-L3 / cleanupRequestCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `L03:formula-dropstarted` | `CP-L3 / dropStartedEventCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `L03:formula-active` | `CP-L3 / activeDropAttemptCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `L03:formula-physical` | `CP-L3 / normalDropTerminalChainCount` | Inferred or whole-set aggregate labeled as exact chain/boundary | Boundary-keyed immutable event/attempt/outcome chain rows and exact transition join |
| `L03:formula-authorization-chain` | `CP-L3 / authorizationRegistrationTransitionCount` | Inferred or whole-set aggregate labeled as exact chain/boundary | Boundary-keyed immutable event/attempt/outcome chain rows and exact transition join |
| `L04:formula-dropstarted` | `CP-L3 / dropStartedEventsPerBoundary` | Inferred or whole-set aggregate labeled as exact chain/boundary | Boundary-keyed immutable event/attempt/outcome chain rows and exact transition join |
| `L04:formula-finalized` | `CP-L3 / finalizedEventsPerBoundary` | Inferred or whole-set aggregate labeled as exact chain/boundary | Boundary-keyed immutable event/attempt/outcome chain rows and exact transition join |
| `L04:formula-physical` | `CP-L3 / terminalOutcomeCountPerBoundary` | Inferred or whole-set aggregate labeled as exact chain/boundary | Boundary-keyed immutable event/attempt/outcome chain rows and exact transition join |
| `L04:formula-target-zero` | `TA3 / targetCount` | Database-observed current catalog/identity fact; subcase actual still shared | Retain exact scoped observation and bind a unique subcase raw/action provenance record |
| `L04:formula-roles-zero` | `TA3 / roleCount` | Database-observed current catalog/identity fact; subcase actual still shared | Retain exact scoped observation and bind a unique subcase raw/action provenance record |
| `L05:formula-use-zero` | `TA3 / useMutationCount` | Constant | Exact audit mutation count or independently computed ACL set/dimension difference |
| `L05:formula-drop-zero` | `TA3 / dropMutationCount` | Constant | Exact audit mutation count or independently computed ACL set/dimension difference |
| `L05:formula-quarantine-one` | `CP-L3 / quarantineOutcomeCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `R01:formula-decision-one` | `CP-L3 / decisionCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `R01:formula-consumed-attempt` | `CP-L3 / consumedAttemptId` | Caller-echoed operation parameter | Attempt ID selected from the consumed decision or durable reconciliation row |
| `R01:formula-action` | `CP-L3 / authorizedAction` | Semantically collapsed coalesce | Authorized action from exact decision plus independently observed performed action |
| `R01:formula-recovery-one` | `CP-L3 / recoveryAttemptCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `R01:formula-finalized-one` | `CP-L3 / finalizedEventCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `R02:formula-attempts-zero` | `CP-L3 / newAttemptCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `R02:formula-events-zero` | `CP-L3 / newEventCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `R02:formula-consumed-one` | `CP-L3 / decisionConsumedCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `R03:formula-failure-one` | `CP-L3 / cleanupFailureCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `R03:formula-old-zero` | `CP-L3 / oldDecisionAcceptedCount` | Structurally constant 0 | Count of old decisions tested against the fresh attempt outside the fresh-decision filter |
| `R03:formula-fresh-one` | `CP-L3 / freshLinkedDecisionCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `R03:formula-consumed-one` | `CP-L3 / freshDecisionConsumedCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `R03:formula-finalized-one` | `CP-L3 / finalizedEventCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `C01:formula-business-delta` | `TC3 / businessRowDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `C01:formula-history-delta` | `TC3 / historyRowDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `C01:formula-receipt-one` | `TC3 / receiptCount` | Database-observed current command-ledger aggregate; subcase actual still shared | Retain exact scoped observation plus unique subcase fixture/action/history provenance |
| `C01:formula-outcome-one` | `TC3 / committedOutcomeCount` | Database-observed current command-ledger aggregate; subcase actual still shared | Retain exact scoped observation plus unique subcase fixture/action/history provenance |
| `C01:formula-active-zero` | `TC3 / activeAttemptCount` | Database-observed current command-ledger aggregate; subcase actual still shared | Retain exact scoped observation plus unique subcase fixture/action/history provenance |
| `C02:formula-business-same` | `TC3 / businessAfter2Sha256` | Same-current-snapshot alias or semantically collapsed hash | Distinct typed business/history/request pre/post hashes from immutable observations |
| `C02:formula-history-same` | `TC3 / historyAfter2Sha256` | Same-current-snapshot alias or semantically collapsed hash | Distinct typed business/history/request pre/post hashes from immutable observations |
| `C02:formula-receipt-same` | `TC3 / receiptId2` | Database-observed current command-ledger aggregate; subcase actual still shared | Retain exact scoped observation plus unique subcase fixture/action/history provenance |
| `C02:formula-response-same` | `TC3 / responseSha2562` | Database-observed current command-ledger aggregate; subcase actual still shared | Retain exact scoped observation plus unique subcase fixture/action/history provenance |
| `C02:formula-receipt-one` | `TC3 / receiptCount` | Database-observed current command-ledger aggregate; subcase actual still shared | Retain exact scoped observation plus unique subcase fixture/action/history provenance |
| `C03:formula-digest-different` | `TC3 / changedDigest` | Same-current-snapshot alias or semantically collapsed hash | Distinct typed business/history/request pre/post hashes from immutable observations |
| `C03:formula-request-zero` | `TC3 / requestDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `C03:formula-attempt-zero` | `TC3 / attemptDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `C03:formula-business-zero` | `TC3 / businessHistoryDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `C04:formula-business-zero` | `TC3 / businessRowDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `C04:formula-history-zero` | `TC3 / historyRowDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `C04:formula-receipt-zero` | `TC3 / receiptDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `C04:formula-rollback-one` | `TC3 / rolledBackOutcomeCount` | Database-observed current command-ledger aggregate; subcase actual still shared | Retain exact scoped observation plus unique subcase fixture/action/history provenance |
| `C05:formula-business-zero` | `TC3 / businessHistoryReceiptDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `C05:formula-rollback-one` | `TC3 / rolledBackOutcomeCount` | Database-observed current command-ledger aggregate; subcase actual still shared | Retain exact scoped observation plus unique subcase fixture/action/history provenance |
| `C05:formula-opened-attempt` | `TC3 / openedAttemptId` | Caller-echoed attempt parameter | Attempt ID selected from the authoritative opened-attempt row |
| `C06:formula-subcases-four` | `TC3 / interruptionSubcaseCount` | One outcome count reused for different meanings | Distinct subcase/evidence/attempt grouped counts from exact durable rows |
| `C06:formula-distinct-evidence` | `TC3 / distinctEvidenceIdCount` | One outcome count reused for different meanings | Distinct subcase/evidence/attempt grouped counts from exact durable rows |
| `C06:formula-terminal-each` | `TC3 / terminalOutcomeCountPerAttempt` | One outcome count reused for different meanings | Distinct subcase/evidence/attempt grouped counts from exact durable rows |
| `C07:formula-requests-two` | `TC3 / startRequestCount` | Database-observed current command-ledger aggregate; subcase actual still shared | Retain exact scoped observation plus unique subcase fixture/action/history provenance |
| `C07:formula-started-one` | `TC3 / startedAttemptCount` | Database-observed current command-ledger aggregate; subcase actual still shared | Retain exact scoped observation plus unique subcase fixture/action/history provenance |
| `C07:formula-active-one` | `TC3 / activeAttemptCount` | Database-observed current command-ledger aggregate; subcase actual still shared | Retain exact scoped observation plus unique subcase fixture/action/history provenance |
| `C07:formula-unrelated-zero` | `TC3 / unrelatedMutationCount` | Constant 0 | Exact audit/history count for unrelated mutations or accepted substitutions |
| `C08:formula-accepted-zero` | `TC3 / acceptedSubstitutionCount` | Constant 0 | Exact audit/history count for unrelated mutations or accepted substitutions |
| `C08:formula-contexts-zero` | `TC3 / contextDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `C08:formula-receipts-zero` | `TC3 / receiptDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `C08:formula-business-zero` | `TC3 / businessHistoryDelta` | Absolute current count mislabeled delta | Independent before and after scoped counts with verifier subtraction |
| `G01:formula-attempts-zero` | `TP3 / startedAttemptCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G01:formula-candidates-zero` | `TP3 / candidateCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G01:formula-events-zero` | `TP3 / purgeEventCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G02:formula-eligible-zero` | `TP3 / eligibleBeforeCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G02:formula-frozen-zero` | `TP3 / frozenCandidateCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G02:formula-deleted-zero` | `TP3 / deletedRowCount` | Inferred from terminal state/candidate count | Actual deleted audit rows and independent post-action eligibility query |
| `G02:formula-event-one` | `TP3 / zeroRowsEventCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G03:formula-eligible-positive` | `TP3 / eligibleBeforeCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G03:formula-frozen-equals` | `TP3 / frozenCandidateCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G03:formula-deleted-equals` | `TP3 / deletedRowCount` | Inferred from terminal state/candidate count | Actual deleted audit rows and independent post-action eligibility query |
| `G03:formula-remaining-zero` | `TP3 / remainingEligibleCount` | Inferred from terminal state/candidate count | Actual deleted audit rows and independent post-action eligibility query |
| `G03:formula-event-one` | `TP3 / succeededEventCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G04:formula-hash-different` | `TP3 / currentCandidateSha256` | Current snapshot reused against before reference | Immutable frozen/pre-action hash plus separately observed current/post-action hash |
| `G04:formula-deleted-zero` | `TP3 / deletedRowCount` | Inferred from terminal state/candidate count | Actual deleted audit rows and independent post-action eligibility query |
| `G04:formula-context-same` | `TP3 / contextAfterSha256` | Current snapshot reused against before reference | Immutable frozen/pre-action hash plus separately observed current/post-action hash |
| `G04:formula-event-one` | `TP3 / failedEventCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G05:formula-deleted-zero` | `TP3 / deletedRowCount` | Inferred from terminal state/candidate count | Actual deleted audit rows and independent post-action eligibility query |
| `G05:formula-context-same` | `TP3 / contextAfterSha256` | Current snapshot reused against before reference | Immutable frozen/pre-action hash plus separately observed current/post-action hash |
| `G05:formula-event-one` | `TP3 / failedEventCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G06:formula-starts-two` | `TP3 / concurrentStartCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G06:formula-consumed-one` | `TP3 / consumedAuthorizationCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G06:formula-execution-max` | `TP3 / executionCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G06:formula-child-one` | `TP3 / activeChildCount` | Database-observed current purge aggregate; subcase actual still shared | Retain exact root/auth/execution/batch/attempt observation and add unique subcase provenance |
| `G06:formula-substituted-zero` | `TP3 / substitutedChildCount` | Constant 0 | Exact rejected/accepted child binding count for substituted authorization/policy |
| `E01:formula-within-max` | `TE3 / preparedRowCountWithinMaximum` | Database-observed current export aggregate; subcase actual still shared | Retain exact auth/batch/release observation and add unique subcase provenance |
| `E01:formula-hash` | `TE3 / preparedSha256` | Current batch snapshot reused as historical reference | Distinct immutable prepared/before snapshot and separately observed after snapshot |
| `E01:formula-excluded-zero` | `TE3 / excludedFieldCount` | Constant 0 | Exact excluded-column diff and eligible-after-asOf/batch-membership observations |
| `E01:formula-event-one` | `TE3 / preparedEventCount` | Inferred always-one expression | Count of exact immutable Prepared event rows |
| `E02:formula-rows-same` | `TE3 / preparedAfterSha256` | Current batch snapshot reused as historical reference | Distinct immutable prepared/before snapshot and separately observed after snapshot |
| `E02:formula-count-same` | `TE3 / preparedAfterCount` | Current batch snapshot reused as historical reference | Distinct immutable prepared/before snapshot and separately observed after snapshot |
| `E02:formula-later-one` | `TE3 / laterEligibleRowCount` | Constant 0 | Exact excluded-column diff and eligible-after-asOf/batch-membership observations |
| `E02:formula-later-batch-zero` | `TE3 / laterRowInBatchCount` | Constant 0 | Exact excluded-column diff and eligible-after-asOf/batch-membership observations |
| `E03:formula-released-zero` | `TE3 / releasedRowCount` | Database-observed current export aggregate; subcase actual still shared | Retain exact auth/batch/release observation and add unique subcase provenance |
| `E03:formula-events-zero` | `TE3 / newReleaseEventCount` | Database-observed current export aggregate; subcase actual still shared | Retain exact auth/batch/release observation and add unique subcase provenance |
| `E03:formula-batch-same` | `TE3 / preparedAfterSha256` | Current batch snapshot reused as historical reference | Distinct immutable prepared/before snapshot and separately observed after snapshot |
| `E04:formula-release-distinct` | `TE3 / releaseId2` | Database-observed current export aggregate; subcase actual still shared | Retain exact auth/batch/release observation and add unique subcase provenance |
| `E04:formula-prior-link` | `TE3 / priorReleaseId2` | Database-observed current export aggregate; subcase actual still shared | Retain exact auth/batch/release observation and add unique subcase provenance |
| `E04:formula-active-one` | `TE3 / activeReleaseCount` | Database-observed current export aggregate; subcase actual still shared | Retain exact auth/batch/release observation and add unique subcase provenance |
| `E04:formula-success-max` | `TE3 / deliverySuccessCount` | Database-observed current export aggregate; subcase actual still shared | Retain exact auth/batch/release observation and add unique subcase provenance |
| `E04:formula-batch-same` | `TE3 / batchAfterSha256` | Current batch snapshot reused as historical reference | Distinct immutable prepared/before snapshot and separately observed after snapshot |
| `A01:formula-unexpected-zero` | `CP-A3 / controlObservedMinusExpectedCount` | Semantically collapsed manifest comparison | Exact per-dimension observed-minus-expected set and hash scoped to the subcase mutation |
| `A01:formula-missing-zero` | `TA3 / targetExpectedMinusObservedCount` | Constant | Exact audit mutation count or independently computed ACL set/dimension difference |
| `A01:formula-dimensions` | `TA3 / targetAclDimensionCount` | Constant | Exact audit mutation count or independently computed ACL set/dimension difference |
| `A02:formula-allowed-zero` | `TA3 / allowedProtectedOperationCount` | Database-observed current catalog/identity fact; subcase actual still shared | Retain exact scoped observation and bind a unique subcase raw/action provenance record |
| `A02:formula-tuple-count` | `TA3 / durableDenialCount` | Inferred from effective permission | Durable denial audit row count for exact principal/object/operation/action |
| `A02:formula-fingerprint-same` | `TA3 / protectedAfterSha256` | Current ACL snapshot reused as before/after | Distinct immutable pre-action and post-action protected-object fingerprints |
| `T01:formula-lease-one` | `CP-L3 / leaseCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `T01:formula-target-one` | `TA3 / targetCount` | Database-observed current catalog/identity fact; subcase actual still shared | Retain exact scoped observation and bind a unique subcase raw/action provenance record |
| `T01:formula-admin-zero` | `TA3 / administrativeBypassCount` | Database-observed current catalog/identity fact; subcase actual still shared | Retain exact scoped observation and bind a unique subcase raw/action provenance record |
| `T01:formula-fixture` | `TA3 / fixturePrepared` | Database-observed current catalog/identity fact; subcase actual still shared | Retain exact scoped observation and bind a unique subcase raw/action provenance record |
| `T02:formula-instance-different` | `CP-L3 / survivingAttemptCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `T02:formula-attempt-same` | `CP-L3 / reconciledAttemptId` | Caller-echoed operation parameter | Attempt ID selected from the consumed decision or durable reconciliation row |
| `T02:formula-dropstarted-one` | `CP-L3 / dropStartedEventCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `T02:formula-finalized-one` | `CP-L3 / finalizedEventCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `T02:formula-cleanup-one` | `CP-L3 / cleanupEvidenceCount` | Database-observed current aggregate; subcase actual still shared | Keep exact scoped relation observation and add unique subcase fixture/action/history provenance |
| `T03:formula-killed-equals` | `OR3 / killedMutants` | Missing live reader command; in-memory literal only | Immutable structured mutation-run record with mutation ID, target, expected rejection code and actual verifier result |
| `T03:formula-survivors-zero` | `OR3 / survivingMutants` | Missing live reader command; in-memory literal only | Immutable structured mutation-run record with mutation ID, target, expected rejection code and actual verifier result |

Count: 133/133 component rows reconciled.

## Thirty-four-scenario reconciliation matrix

Every scenario remains FAIL in Correction 27. The proposed Correction 28 acceptance evidence is source-only: unique subcase fixtures and strict contracts; it does not prescribe or claim database acceptance.

| ID | Subcases / terms | Applicable findings | Current decisive provenance defect | Required Correction 28 source acceptance |
|---|---:|---|---|---|
| P01 | 1 / 3 | C27F-01,-03,-04,-07 | collapsed CP-A3/TA3 mismatch claims and copied action | exact manifest/ACL set differences plus unique P01 actual receipt |
| P02 | 5 / 3 | C27F-01,-03,-04,-07 | five pin mutations share one raw/action payload | five unique pin-dimension observations and action results |
| P03 | 4 / 4 | C27F-01,-02,-03,-04,-07 | constant seeded/protected facts and reused ACL hashes | four unique injected-delta, protected-history and cleanup-baseline records |
| L01 | 3 / 3 | C27F-01,-03,-04,-07 | whole-set XOR/counts and shared action | boundary-keyed events and three unique branch results |
| L02 | 6 / 5 | C27F-01,-03,-04,-07 | whole-set “per boundary” counts | six stage-specific attempts/outcomes/target/role snapshots |
| L03 | 5 / 5 | C27F-01,-03,-04,-07 | incomplete terminal/authorization chain | five exact race/transition action records and immutable chain joins |
| L04 | 5 / 5 | C27F-01,-02,-03,-04,-07 | target-local after-drop and non-boundary evidence | surviving-control-plane absence/role facts and five boundary chains |
| L05 | 5 / 3 | C27F-01,-03,-04,-07 | constant use/drop zeros | exact denial/quarantine audit rows for five subcases |
| R01 | 1 / 5 | C27F-01,-03,-04,-07 | caller-echoed consumed attempt | decision-to-attempt join and unique performed-action observation |
| R02 | 8 / 3 | C27F-01,-03,-04,-07 | one zero/count payload for eight rejection modes | eight unique rejection inputs/results and no-new-row observations |
| R03 | 5 / 5 | C27F-01,-02,-03,-04,-07 | structurally zero old-decision fact | old/fresh decision observations outside the fresh filter and five histories |
| C01 | 1 / 5 | C27F-01,-02,-03,-04,-07 | absolute counts self-equal expected deltas | independent before/after business/history counts and receipt/outcome rows |
| C02 | 1 / 5 | C27F-01,-02,-03,-04,-07 | one current snapshot supplies both replay sides | immutable original and replay receipt/business/history observations |
| C03 | 1 / 4 | C27F-01,-02,-03,-04,-07 | same request digest and absolute “deltas” | registered versus changed digest plus before/after row counts |
| C04 | 5 / 4 | C27F-01,-02,-03,-04,-07 | five rollback boundaries share one payload | five independent pre/post business/history/receipt/outcome observations |
| C05 | 1 / 3 | C27F-01,-03,-04,-07 | attempt ID echoed from caller | opened attempt selected from ledger and durable rollback outcome |
| C06 | 4 / 3 | C27F-01,-03,-04,-07 | outcome count reused as distinct-evidence count | four attempt/evidence identities and grouped terminal outcomes |
| C07 | 1 / 4 | C27F-01,-03,-04,-07 | constant unrelated-mutation zero | exact request/attempt and unrelated audit-set difference |
| C08 | 8 / 4 | C27F-01,-03,-04,-07 | constant accepted-substitution zero | eight unique substituted fields with exact rejection/no-delta records |
| G01 | 5 / 3 | C27F-01,-03,-04,-07 | five invalid authorizations share zero payload | five exact auth/target/batch/org rejection observations |
| G02 | 1 / 4 | C27F-01,-03,-04,-07 | deleted/remaining inferred | exact empty eligibility/candidate/deletion/audit observations |
| G03 | 1 / 5 | C27F-01,-02,-03,-04,-07 | deletion inferred from success/candidate count | before eligibility, frozen candidates, actual delete audit and post eligibility |
| G04 | 1 / 4 | C27F-01,-02,-03,-04,-07 | current/frozen/context snapshot reuse | immutable frozen hash, drifted current hash and post-failure context hash |
| G05 | 3 / 3 | C27F-01,-02,-03,-04,-07 | inferred deletion and shared rollback payload | three unique failpoint/rollback/audit histories |
| G06 | 4 / 5 | C27F-01,-03,-04,-07 | constant substituted-child zero | exact concurrency/root/child/retry observations for four subcases |
| E01 | 1 / 4 | C27F-01,-02,-03,-04,-07 | constant excluded count, inferred event | exact selected/excluded schema set and Prepared event row |
| E02 | 1 / 4 | C27F-01,-02,-03,-04,-07 | constant later-row facts and snapshot reuse | immutable prepared snapshot plus later eligibility/batch exclusion observation |
| E03 | 4 / 3 | C27F-01,-02,-03,-04,-07 | four release denials share action and current batch | four unique denial results and immutable pre/post batch hashes |
| E04 | 3 / 5 | C27F-01,-02,-03,-04,-07 | three steps share action/current hash | unique old/fresh/batch subcase actions and release-link history |
| A01 | 1 / 3 | C27F-01,-03,-04,-07 | expected-minus-observed zero and dimension three are constants | independently frozen expected ACL set and observed exact set difference |
| A02 | 7 / 3 | C27F-01,-02,-03,-04,-07 | denial inferred from permission; seven classes share action | seven principal-specific effective/denial-audit/pre-post fingerprint records |
| T01 | 1 / 4 | C27F-01,-03,-04,-07 | shared copied action and mixed TA3 provenance | unique allocation/action plus exact non-admin identity/fixture observations |
| T02 | 1 / 5 | C27F-01,-02,-03,-04,-07 | caller-echoed reconciliation identity | original/restarted controller IDs and durable attempt/cleanup chain |
| T03 | 4 / 2 | C27F-03,-04,-05,-06,-07 | OR3 absent live; Boolean broad-exception mutation proof | four unique structured mutation-run records through local OR3 dispatch |
| **Total** | **108 / 133** | **C27F-01 through C27F-07** | **0/34 currently acceptable** | **34/34 must pass a later internal source-only precheck** |

## Complete 108-subcase provenance matrix

Current raw provenance is the scenario-level `Correction27RawFixtures[scenario]`; current action provenance is `Correction27ActionFixtures[scenario]`. Both are reused within multi-subcase scenarios. Expected-result provenance is the immutable `Rev869BCorrection26FrozenOracle.Subcases` row plus the top-level `Scenarios` tuple. The required historical source below must become independently keyed by subcase, preparation, attempt, evidence and action IDs.

| Subcase | Current raw fact provenance | Current action fact provenance | Current/missing historical fact provenance | Expected-result provenance | Findings |
|---|---|---|---|---|---|
| `P01:p01-action` | shared `Correction27RawFixtures[P01]`; no subcase key | shared copied `Correction27ActionFixtures[P01]` | CP-A3 manifest/registration facts + TA3 target/ACL facts | oracle subcase -> `ExternalVerified`; `rev869b/P01/p01-action/action/v3` | C27F-01,-03,-04,-07 |
| `P02:wrong-system-id` | shared `Correction27RawFixtures[P02]`; no subcase key | shared copied `Correction27ActionFixtures[P02]` | CP-A3 manifest/registration facts + TA3 target/ACL facts | oracle subcase -> `PreflightDenied`; `rev869b/P02/wrong-system-id/action/v3` | C27F-01,-03,-04,-07 |
| `P02:wrong-tls-spki` | shared `Correction27RawFixtures[P02]`; no subcase key | shared copied `Correction27ActionFixtures[P02]` | CP-A3 manifest/registration facts + TA3 target/ACL facts | oracle subcase -> `PreflightDenied`; `rev869b/P02/wrong-tls-spki/action/v3` | C27F-01,-03,-04,-07 |
| `P02:wrong-endpoint` | shared `Correction27RawFixtures[P02]`; no subcase key | shared copied `Correction27ActionFixtures[P02]` | CP-A3 manifest/registration facts + TA3 target/ACL facts | oracle subcase -> `PreflightDenied`; `rev869b/P02/wrong-endpoint/action/v3` | C27F-01,-03,-04,-07 |
| `P02:wrong-source` | shared `Correction27RawFixtures[P02]`; no subcase key | shared copied `Correction27ActionFixtures[P02]` | CP-A3 manifest/registration facts + TA3 target/ACL facts | oracle subcase -> `PreflightDenied`; `rev869b/P02/wrong-source/action/v3` | C27F-01,-03,-04,-07 |
| `P02:wrong-manifest` | shared `Correction27RawFixtures[P02]`; no subcase key | shared copied `Correction27ActionFixtures[P02]` | CP-A3 manifest/registration facts + TA3 target/ACL facts | oracle subcase -> `PreflightDenied`; `rev869b/P02/wrong-manifest/action/v3` | C27F-01,-03,-04,-07 |
| `P03:unexpected-role` | shared `Correction27RawFixtures[P03]`; no subcase key | shared copied `Correction27ActionFixtures[P03]` | CP-A3 manifest/registration facts + TA3 target/ACL facts | oracle subcase -> `VerificationDenied`; `rev869b/P03/unexpected-role/action/v3` | C27F-01,-02,-03,-04,-07 |
| `P03:unexpected-database` | shared `Correction27RawFixtures[P03]`; no subcase key | shared copied `Correction27ActionFixtures[P03]` | CP-A3 manifest/registration facts + TA3 target/ACL facts | oracle subcase -> `VerificationDenied`; `rev869b/P03/unexpected-database/action/v3` | C27F-01,-02,-03,-04,-07 |
| `P03:unexpected-object` | shared `Correction27RawFixtures[P03]`; no subcase key | shared copied `Correction27ActionFixtures[P03]` | CP-A3 manifest/registration facts + TA3 target/ACL facts | oracle subcase -> `VerificationDenied`; `rev869b/P03/unexpected-object/action/v3` | C27F-01,-02,-03,-04,-07 |
| `P03:unexpected-grant` | shared `Correction27RawFixtures[P03]`; no subcase key | shared copied `Correction27ActionFixtures[P03]` | CP-A3 manifest/registration facts + TA3 target/ACL facts | oracle subcase -> `VerificationDenied`; `rev869b/P03/unexpected-grant/action/v3` | C27F-01,-02,-03,-04,-07 |
| `L01:reserved` | shared `Correction27RawFixtures[L01]`; no subcase key | shared copied `Correction27ActionFixtures[L01]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Ready`; `rev869b/L01/reserved/action/v3` | C27F-01,-03,-04,-07 |
| `L01:interrupt-before-role` | shared `Correction27RawFixtures[L01]`; no subcase key | shared copied `Correction27ActionFixtures[L01]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Ready`; `rev869b/L01/interrupt-before-role/action/v3` | C27F-01,-03,-04,-07 |
| `L01:resume-or-approved-cleanup` | shared `Correction27RawFixtures[L01]`; no subcase key | shared copied `Correction27ActionFixtures[L01]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Ready`; `rev869b/L01/resume-or-approved-cleanup/action/v3` | C27F-01,-03,-04,-07 |
| `L02:reserved` | shared `Correction27RawFixtures[L02]`; no subcase key | shared copied `Correction27ActionFixtures[L02]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Ready`; `rev869b/L02/reserved/action/v3` | C27F-01,-03,-04,-07 |
| `L02:database-created` | shared `Correction27RawFixtures[L02]`; no subcase key | shared copied `Correction27ActionFixtures[L02]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Ready`; `rev869b/L02/database-created/action/v3` | C27F-01,-03,-04,-07 |
| `L02:roles-created` | shared `Correction27RawFixtures[L02]`; no subcase key | shared copied `Correction27ActionFixtures[L02]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Ready`; `rev869b/L02/roles-created/action/v3` | C27F-01,-03,-04,-07 |
| `L02:migration-applied` | shared `Correction27RawFixtures[L02]`; no subcase key | shared copied `Correction27ActionFixtures[L02]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Ready`; `rev869b/L02/migration-applied/action/v3` | C27F-01,-03,-04,-07 |
| `L02:verified` | shared `Correction27RawFixtures[L02]`; no subcase key | shared copied `Correction27ActionFixtures[L02]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Ready`; `rev869b/L02/verified/action/v3` | C27F-01,-03,-04,-07 |
| `L02:ready` | shared `Correction27RawFixtures[L02]`; no subcase key | shared copied `Correction27ActionFixtures[L02]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Ready`; `rev869b/L02/ready/action/v3` | C27F-01,-03,-04,-07 |
| `L03:ready-cleanup-race` | shared `Correction27RawFixtures[L03]`; no subcase key | shared copied `Correction27ActionFixtures[L03]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `DropStarted`; `rev869b/L03/ready-cleanup-race/action/v3` | C27F-01,-03,-04,-07 |
| `L03:inuse-cleanup-race` | shared `Correction27RawFixtures[L03]`; no subcase key | shared copied `Correction27ActionFixtures[L03]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `DropStarted`; `rev869b/L03/inuse-cleanup-race/action/v3` | C27F-01,-03,-04,-07 |
| `L03:single-dropstarted` | shared `Correction27RawFixtures[L03]`; no subcase key | shared copied `Correction27ActionFixtures[L03]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `DropStarted`; `rev869b/L03/single-dropstarted/action/v3` | C27F-01,-03,-04,-07 |
| `L03:single-drop` | shared `Correction27RawFixtures[L03]`; no subcase key | shared copied `Correction27ActionFixtures[L03]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `DropStarted`; `rev869b/L03/single-drop/action/v3` | C27F-01,-03,-04,-07 |
| `L03:authorization-event-binding` | shared `Correction27RawFixtures[L03]`; no subcase key | shared copied `Correction27ActionFixtures[L03]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `DropStarted`; `rev869b/L03/authorization-event-binding/action/v3` | C27F-01,-03,-04,-07 |
| `L04:before-drop` | shared `Correction27RawFixtures[L04]`; no subcase key | shared copied `Correction27ActionFixtures[L04]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Finalized`; `rev869b/L04/before-drop/action/v3` | C27F-01,-03,-04,-07 |
| `L04:during-drop` | shared `Correction27RawFixtures[L04]`; no subcase key | shared copied `Correction27ActionFixtures[L04]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Finalized`; `rev869b/L04/during-drop/action/v3` | C27F-01,-03,-04,-07 |
| `L04:after-drop` | shared `Correction27RawFixtures[L04]`; no subcase key | shared copied `Correction27ActionFixtures[L04]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Finalized`; `rev869b/L04/after-drop/action/v3` | C27F-01,-03,-04,-07 |
| `L04:during-role-cleanup` | shared `Correction27RawFixtures[L04]`; no subcase key | shared copied `Correction27ActionFixtures[L04]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Finalized`; `rev869b/L04/during-role-cleanup/action/v3` | C27F-01,-03,-04,-07 |
| `L04:finalized-once` | shared `Correction27RawFixtures[L04]`; no subcase key | shared copied `Correction27ActionFixtures[L04]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Finalized`; `rev869b/L04/finalized-once/action/v3` | C27F-01,-03,-04,-07 |
| `L05:mismatch-detected` | shared `Correction27RawFixtures[L05]`; no subcase key | shared copied `Correction27ActionFixtures[L05]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Quarantined`; `rev869b/L05/mismatch-detected/action/v3` | C27F-01,-03,-04,-07 |
| `L05:use-denied` | shared `Correction27RawFixtures[L05]`; no subcase key | shared copied `Correction27ActionFixtures[L05]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Quarantined`; `rev869b/L05/use-denied/action/v3` | C27F-01,-03,-04,-07 |
| `L05:drop-denied` | shared `Correction27RawFixtures[L05]`; no subcase key | shared copied `Correction27ActionFixtures[L05]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Quarantined`; `rev869b/L05/drop-denied/action/v3` | C27F-01,-03,-04,-07 |
| `L05:quarantine-authorized` | shared `Correction27RawFixtures[L05]`; no subcase key | shared copied `Correction27ActionFixtures[L05]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Quarantined`; `rev869b/L05/quarantine-authorized/action/v3` | C27F-01,-03,-04,-07 |
| `L05:quarantined` | shared `Correction27RawFixtures[L05]`; no subcase key | shared copied `Correction27ActionFixtures[L05]` | CP-L3 immutable lifecycle ledger + TA3 target state/role facts | oracle subcase -> `Quarantined`; `rev869b/L05/quarantined/action/v3` | C27F-01,-03,-04,-07 |
| `R01:r01-action` | shared `Correction27RawFixtures[R01]`; no subcase key | shared copied `Correction27ActionFixtures[R01]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `Finalized`; `rev869b/R01/r01-action/action/v3` | C27F-01,-03,-04,-07 |
| `R02:wrong` | shared `Correction27RawFixtures[R02]`; no subcase key | shared copied `Correction27ActionFixtures[R02]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `RecoveryAuthorized`; `rev869b/R02/wrong/action/v3` | C27F-01,-03,-04,-07 |
| `R02:expired` | shared `Correction27RawFixtures[R02]`; no subcase key | shared copied `Correction27ActionFixtures[R02]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `RecoveryAuthorized`; `rev869b/R02/expired/action/v3` | C27F-01,-03,-04,-07 |
| `R02:replayed` | shared `Correction27RawFixtures[R02]`; no subcase key | shared copied `Correction27ActionFixtures[R02]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `RecoveryAuthorized`; `rev869b/R02/replayed/action/v3` | C27F-01,-03,-04,-07 |
| `R02:foreign` | shared `Correction27RawFixtures[R02]`; no subcase key | shared copied `Correction27ActionFixtures[R02]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `RecoveryAuthorized`; `rev869b/R02/foreign/action/v3` | C27F-01,-03,-04,-07 |
| `R02:pre-state` | shared `Correction27RawFixtures[R02]`; no subcase key | shared copied `Correction27ActionFixtures[R02]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `RecoveryAuthorized`; `rev869b/R02/pre-state/action/v3` | C27F-01,-03,-04,-07 |
| `R02:action` | shared `Correction27RawFixtures[R02]`; no subcase key | shared copied `Correction27ActionFixtures[R02]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `RecoveryAuthorized`; `rev869b/R02/action/action/v3` | C27F-01,-03,-04,-07 |
| `R02:nonce` | shared `Correction27RawFixtures[R02]`; no subcase key | shared copied `Correction27ActionFixtures[R02]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `RecoveryAuthorized`; `rev869b/R02/nonce/action/v3` | C27F-01,-03,-04,-07 |
| `R02:valid-preserved` | shared `Correction27RawFixtures[R02]`; no subcase key | shared copied `Correction27ActionFixtures[R02]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `RecoveryAuthorized`; `rev869b/R02/valid-preserved/action/v3` | C27F-01,-03,-04,-07 |
| `R03:first-failure` | shared `Correction27RawFixtures[R03]`; no subcase key | shared copied `Correction27ActionFixtures[R03]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `Finalized`; `rev869b/R03/first-failure/action/v3` | C27F-01,-03,-04,-07 |
| `R03:restart` | shared `Correction27RawFixtures[R03]`; no subcase key | shared copied `Correction27ActionFixtures[R03]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `Finalized`; `rev869b/R03/restart/action/v3` | C27F-01,-03,-04,-07 |
| `R03:old-decision-denied` | shared `Correction27RawFixtures[R03]`; no subcase key | shared copied `Correction27ActionFixtures[R03]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `Finalized`; `rev869b/R03/old-decision-denied/action/v3` | C27F-01,-03,-04,-07 |
| `R03:fresh-linked-decision` | shared `Correction27RawFixtures[R03]`; no subcase key | shared copied `Correction27ActionFixtures[R03]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `Finalized`; `rev869b/R03/fresh-linked-decision/action/v3` | C27F-01,-03,-04,-07 |
| `R03:finalized` | shared `Correction27RawFixtures[R03]`; no subcase key | shared copied `Correction27ActionFixtures[R03]` | CP-L3 recovery decision/attempt ledger + TA3 target cleanup state | oracle subcase -> `Finalized`; `rev869b/R03/finalized/action/v3` | C27F-01,-03,-04,-07 |
| `C01:c01-action` | shared `Correction27RawFixtures[C01]`; no subcase key | shared copied `Correction27ActionFixtures[C01]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `Committed`; `rev869b/C01/c01-action/action/v3` | C27F-01,-02,-03,-04,-07 |
| `C02:c02-action` | shared `Correction27RawFixtures[C02]`; no subcase key | shared copied `Correction27ActionFixtures[C02]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `Committed`; `rev869b/C02/c02-action/action/v3` | C27F-01,-02,-03,-04,-07 |
| `C03:c03-action` | shared `Correction27RawFixtures[C03]`; no subcase key | shared copied `Correction27ActionFixtures[C03]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `RequestRegistered`; `rev869b/C03/c03-action/action/v3` | C27F-01,-02,-03,-04,-07 |
| `C04:receipt-failpoint` | shared `Correction27RawFixtures[C04]`; no subcase key | shared copied `Correction27ActionFixtures[C04]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `RolledBack`; `rev869b/C04/receipt-failpoint/action/v3` | C27F-01,-02,-03,-04,-07 |
| `C04:business-rollback` | shared `Correction27RawFixtures[C04]`; no subcase key | shared copied `Correction27ActionFixtures[C04]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `RolledBack`; `rev869b/C04/business-rollback/action/v3` | C27F-01,-02,-03,-04,-07 |
| `C04:history-rollback` | shared `Correction27RawFixtures[C04]`; no subcase key | shared copied `Correction27ActionFixtures[C04]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `RolledBack`; `rev869b/C04/history-rollback/action/v3` | C27F-01,-02,-03,-04,-07 |
| `C04:receipt-rollback` | shared `Correction27RawFixtures[C04]`; no subcase key | shared copied `Correction27ActionFixtures[C04]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `RolledBack`; `rev869b/C04/receipt-rollback/action/v3` | C27F-01,-02,-03,-04,-07 |
| `C04:durable-noncommit` | shared `Correction27RawFixtures[C04]`; no subcase key | shared copied `Correction27ActionFixtures[C04]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `RolledBack`; `rev869b/C04/durable-noncommit/action/v3` | C27F-01,-02,-03,-04,-07 |
| `C05:c05-action` | shared `Correction27RawFixtures[C05]`; no subcase key | shared copied `Correction27ActionFixtures[C05]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `RolledBack`; `rev869b/C05/c05-action/action/v3` | C27F-01,-03,-04,-07 |
| `C06:before-open` | shared `Correction27RawFixtures[C06]`; no subcase key | shared copied `Correction27ActionFixtures[C06]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `FourExactInterruptionOutcomesReconciled`; `rev869b/C06/before-open/action/v3` | C27F-01,-03,-04,-07 |
| `C06:after-open` | shared `Correction27RawFixtures[C06]`; no subcase key | shared copied `Correction27ActionFixtures[C06]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `FourExactInterruptionOutcomesReconciled`; `rev869b/C06/after-open/action/v3` | C27F-01,-03,-04,-07 |
| `C06:during-commit` | shared `Correction27RawFixtures[C06]`; no subcase key | shared copied `Correction27ActionFixtures[C06]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `FourExactInterruptionOutcomesReconciled`; `rev869b/C06/during-commit/action/v3` | C27F-01,-03,-04,-07 |
| `C06:after-response` | shared `Correction27RawFixtures[C06]`; no subcase key | shared copied `Correction27ActionFixtures[C06]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `FourExactInterruptionOutcomesReconciled`; `rev869b/C06/after-response/action/v3` | C27F-01,-03,-04,-07 |
| `C07:c07-action` | shared `Correction27RawFixtures[C07]`; no subcase key | shared copied `Correction27ActionFixtures[C07]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `AttemptStarted`; `rev869b/C07/c07-action/action/v3` | C27F-01,-03,-04,-07 |
| `C08:pool` | shared `Correction27RawFixtures[C08]`; no subcase key | shared copied `Correction27ActionFixtures[C08]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `AttemptStarted`; `rev869b/C08/pool/action/v3` | C27F-01,-03,-04,-07 |
| `C08:backend` | shared `Correction27RawFixtures[C08]`; no subcase key | shared copied `Correction27ActionFixtures[C08]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `AttemptStarted`; `rev869b/C08/backend/action/v3` | C27F-01,-03,-04,-07 |
| `C08:transaction` | shared `Correction27RawFixtures[C08]`; no subcase key | shared copied `Correction27ActionFixtures[C08]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `AttemptStarted`; `rev869b/C08/transaction/action/v3` | C27F-01,-03,-04,-07 |
| `C08:actor` | shared `Correction27RawFixtures[C08]`; no subcase key | shared copied `Correction27ActionFixtures[C08]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `AttemptStarted`; `rev869b/C08/actor/action/v3` | C27F-01,-03,-04,-07 |
| `C08:organization` | shared `Correction27RawFixtures[C08]`; no subcase key | shared copied `Correction27ActionFixtures[C08]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `AttemptStarted`; `rev869b/C08/organization/action/v3` | C27F-01,-03,-04,-07 |
| `C08:version` | shared `Correction27RawFixtures[C08]`; no subcase key | shared copied `Correction27ActionFixtures[C08]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `AttemptStarted`; `rev869b/C08/version/action/v3` | C27F-01,-03,-04,-07 |
| `C08:role` | shared `Correction27RawFixtures[C08]`; no subcase key | shared copied `Correction27ActionFixtures[C08]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `AttemptStarted`; `rev869b/C08/role/action/v3` | C27F-01,-03,-04,-07 |
| `C08:operation` | shared `Correction27RawFixtures[C08]`; no subcase key | shared copied `Correction27ActionFixtures[C08]` | TC3 command/receipt/outcome/business-history facts | oracle subcase -> `AttemptStarted`; `rev869b/C08/operation/action/v3` | C27F-01,-03,-04,-07 |
| `G01:missing` | shared `Correction27RawFixtures[G01]`; no subcase key | shared copied `Correction27ActionFixtures[G01]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Denied`; `rev869b/G01/missing/action/v3` | C27F-01,-03,-04,-07 |
| `G01:expired` | shared `Correction27RawFixtures[G01]`; no subcase key | shared copied `Correction27ActionFixtures[G01]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Denied`; `rev869b/G01/expired/action/v3` | C27F-01,-03,-04,-07 |
| `G01:wrong-target` | shared `Correction27RawFixtures[G01]`; no subcase key | shared copied `Correction27ActionFixtures[G01]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Denied`; `rev869b/G01/wrong-target/action/v3` | C27F-01,-03,-04,-07 |
| `G01:wrong-batch` | shared `Correction27RawFixtures[G01]`; no subcase key | shared copied `Correction27ActionFixtures[G01]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Denied`; `rev869b/G01/wrong-batch/action/v3` | C27F-01,-03,-04,-07 |
| `G01:wrong-organization` | shared `Correction27RawFixtures[G01]`; no subcase key | shared copied `Correction27ActionFixtures[G01]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Denied`; `rev869b/G01/wrong-organization/action/v3` | C27F-01,-03,-04,-07 |
| `G02:g02-action` | shared `Correction27RawFixtures[G02]`; no subcase key | shared copied `Correction27ActionFixtures[G02]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `ZeroRows`; `rev869b/G02/g02-action/action/v3` | C27F-01,-03,-04,-07 |
| `G03:g03-action` | shared `Correction27RawFixtures[G03]`; no subcase key | shared copied `Correction27ActionFixtures[G03]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Succeeded`; `rev869b/G03/g03-action/action/v3` | C27F-01,-02,-03,-04,-07 |
| `G04:g04-action` | shared `Correction27RawFixtures[G04]`; no subcase key | shared copied `Correction27ActionFixtures[G04]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Failed`; `rev869b/G04/g04-action/action/v3` | C27F-01,-02,-03,-04,-07 |
| `G05:delete-failpoint` | shared `Correction27RawFixtures[G05]`; no subcase key | shared copied `Correction27ActionFixtures[G05]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Failed`; `rev869b/G05/delete-failpoint/action/v3` | C27F-01,-02,-03,-04,-07 |
| `G05:deletion-rollback` | shared `Correction27RawFixtures[G05]`; no subcase key | shared copied `Correction27ActionFixtures[G05]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Failed`; `rev869b/G05/deletion-rollback/action/v3` | C27F-01,-02,-03,-04,-07 |
| `G05:independent-audit` | shared `Correction27RawFixtures[G05]`; no subcase key | shared copied `Correction27ActionFixtures[G05]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Failed`; `rev869b/G05/independent-audit/action/v3` | C27F-01,-02,-03,-04,-07 |
| `G06:concurrent-start` | shared `Correction27RawFixtures[G06]`; no subcase key | shared copied `Correction27ActionFixtures[G06]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Failed`; `rev869b/G06/concurrent-start/action/v3` | C27F-01,-03,-04,-07 |
| `G06:concurrent-execute` | shared `Correction27RawFixtures[G06]`; no subcase key | shared copied `Correction27ActionFixtures[G06]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Failed`; `rev869b/G06/concurrent-execute/action/v3` | C27F-01,-03,-04,-07 |
| `G06:substituted-policy-denied` | shared `Correction27RawFixtures[G06]`; no subcase key | shared copied `Correction27ActionFixtures[G06]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Failed`; `rev869b/G06/substituted-policy-denied/action/v3` | C27F-01,-03,-04,-07 |
| `G06:exact-retry` | shared `Correction27RawFixtures[G06]`; no subcase key | shared copied `Correction27ActionFixtures[G06]` | TP3 purge decision/root/batch/eligibility/audit facts | oracle subcase -> `Failed`; `rev869b/G06/exact-retry/action/v3` | C27F-01,-03,-04,-07 |
| `E01:e01-action` | shared `Correction27RawFixtures[E01]`; no subcase key | shared copied `Correction27ActionFixtures[E01]` | TE3 export preparation/batch/eligibility/release history | oracle subcase -> `Prepared`; `rev869b/E01/e01-action/action/v3` | C27F-01,-02,-03,-04,-07 |
| `E02:e02-action` | shared `Correction27RawFixtures[E02]`; no subcase key | shared copied `Correction27ActionFixtures[E02]` | TE3 export preparation/batch/eligibility/release history | oracle subcase -> `Prepared`; `rev869b/E02/e02-action/action/v3` | C27F-01,-02,-03,-04,-07 |
| `E03:expired` | shared `Correction27RawFixtures[E03]`; no subcase key | shared copied `Correction27ActionFixtures[E03]` | TE3 export preparation/batch/eligibility/release history | oracle subcase -> `Denied`; `rev869b/E03/expired/action/v3` | C27F-01,-02,-03,-04,-07 |
| `E03:wrong-batch` | shared `Correction27RawFixtures[E03]`; no subcase key | shared copied `Correction27ActionFixtures[E03]` | TE3 export preparation/batch/eligibility/release history | oracle subcase -> `Denied`; `rev869b/E03/wrong-batch/action/v3` | C27F-01,-02,-03,-04,-07 |
| `E03:terminal` | shared `Correction27RawFixtures[E03]`; no subcase key | shared copied `Correction27ActionFixtures[E03]` | TE3 export preparation/batch/eligibility/release history | oracle subcase -> `Denied`; `rev869b/E03/terminal/action/v3` | C27F-01,-02,-03,-04,-07 |
| `E03:concurrent` | shared `Correction27RawFixtures[E03]`; no subcase key | shared copied `Correction27ActionFixtures[E03]` | TE3 export preparation/batch/eligibility/release history | oracle subcase -> `Denied`; `rev869b/E03/concurrent/action/v3` | C27F-01,-02,-03,-04,-07 |
| `E04:old-release-interrupted` | shared `Correction27RawFixtures[E04]`; no subcase key | shared copied `Correction27ActionFixtures[E04]` | TE3 export preparation/batch/eligibility/release history | oracle subcase -> `ReleaseRetrySequenceVerified`; `rev869b/E04/old-release-interrupted/action/v3` | C27F-01,-02,-03,-04,-07 |
| `E04:fresh-release-started` | shared `Correction27RawFixtures[E04]`; no subcase key | shared copied `Correction27ActionFixtures[E04]` | TE3 export preparation/batch/eligibility/release history | oracle subcase -> `ReleaseRetrySequenceVerified`; `rev869b/E04/fresh-release-started/action/v3` | C27F-01,-02,-03,-04,-07 |
| `E04:batch-unchanged` | shared `Correction27RawFixtures[E04]`; no subcase key | shared copied `Correction27ActionFixtures[E04]` | TE3 export preparation/batch/eligibility/release history | oracle subcase -> `ReleaseRetrySequenceVerified`; `rev869b/E04/batch-unchanged/action/v3` | C27F-01,-02,-03,-04,-07 |
| `A01:a01-action` | shared `Correction27RawFixtures[A01]`; no subcase key | shared copied `Correction27ActionFixtures[A01]` | CP-A3 role/grant/default/PUBLIC facts + TA3 effective target ACL facts | oracle subcase -> `Verified`; `rev869b/A01/a01-action/action/v3` | C27F-01,-03,-04,-07 |
| `A02:runtime` | shared `Correction27RawFixtures[A02]`; no subcase key | shared copied `Correction27ActionFixtures[A02]` | CP-A3 role/grant/default/PUBLIC facts + TA3 effective target ACL facts | oracle subcase -> `Denied`; `rev869b/A02/runtime/action/v3` | C27F-01,-02,-03,-04,-07 |
| `A02:purge` | shared `Correction27RawFixtures[A02]`; no subcase key | shared copied `Correction27ActionFixtures[A02]` | CP-A3 role/grant/default/PUBLIC facts + TA3 effective target ACL facts | oracle subcase -> `Denied`; `rev869b/A02/purge/action/v3` | C27F-01,-02,-03,-04,-07 |
| `A02:export` | shared `Correction27RawFixtures[A02]`; no subcase key | shared copied `Correction27ActionFixtures[A02]` | CP-A3 role/grant/default/PUBLIC facts + TA3 effective target ACL facts | oracle subcase -> `Denied`; `rev869b/A02/export/action/v3` | C27F-01,-02,-03,-04,-07 |
| `A02:recovery` | shared `Correction27RawFixtures[A02]`; no subcase key | shared copied `Correction27ActionFixtures[A02]` | CP-A3 role/grant/default/PUBLIC facts + TA3 effective target ACL facts | oracle subcase -> `Denied`; `rev869b/A02/recovery/action/v3` | C27F-01,-02,-03,-04,-07 |
| `A02:administrator` | shared `Correction27RawFixtures[A02]`; no subcase key | shared copied `Correction27ActionFixtures[A02]` | CP-A3 role/grant/default/PUBLIC facts + TA3 effective target ACL facts | oracle subcase -> `Denied`; `rev869b/A02/administrator/action/v3` | C27F-01,-02,-03,-04,-07 |
| `A02:ordinary-principal` | shared `Correction27RawFixtures[A02]`; no subcase key | shared copied `Correction27ActionFixtures[A02]` | CP-A3 role/grant/default/PUBLIC facts + TA3 effective target ACL facts | oracle subcase -> `Denied`; `rev869b/A02/ordinary-principal/action/v3` | C27F-01,-02,-03,-04,-07 |
| `A02:public` | shared `Correction27RawFixtures[A02]`; no subcase key | shared copied `Correction27ActionFixtures[A02]` | CP-A3 role/grant/default/PUBLIC facts + TA3 effective target ACL facts | oracle subcase -> `Denied`; `rev869b/A02/public/action/v3` | C27F-01,-02,-03,-04,-07 |
| `T01:t01-action` | shared `Correction27RawFixtures[T01]`; no subcase key | shared copied `Correction27ActionFixtures[T01]` | TA3 allocation/non-admin identity facts | oracle subcase -> `InUse`; `rev869b/T01/t01-action/action/v3` | C27F-01,-03,-04,-07 |
| `T02:t02-action` | shared `Correction27RawFixtures[T02]`; no subcase key | shared copied `Correction27ActionFixtures[T02]` | CP-L3 controller/attempt/cleanup chain + TA3 target cleanup state | oracle subcase -> `Finalized`; `rev869b/T02/t02-action/action/v3` | C27F-01,-02,-03,-04,-07 |
| `T03:all-34-actions` | shared `Correction27RawFixtures[T03]`; no subcase key | shared T03 placeholder action literal | OR3 mutation-run record absent from live dispatch | oracle subcase -> `MutationSensitive`; `rev869b/T03/all-34-actions/action/v3` | C27F-03,-04,-05,-06,-07 |
| `T03:all-34-reads` | shared `Correction27RawFixtures[T03]`; no subcase key | shared T03 placeholder action literal | OR3 mutation-run record absent from live dispatch | oracle subcase -> `MutationSensitive`; `rev869b/T03/all-34-reads/action/v3` | C27F-03,-04,-05,-06,-07 |
| `T03:all-34-assertions` | shared `Correction27RawFixtures[T03]`; no subcase key | shared T03 placeholder action literal | OR3 mutation-run record absent from live dispatch | oracle subcase -> `MutationSensitive`; `rev869b/T03/all-34-assertions/action/v3` | C27F-03,-04,-05,-06,-07 |
| `T03:all-34-cleanups` | shared `Correction27RawFixtures[T03]`; no subcase key | shared T03 placeholder action literal | OR3 mutation-run record absent from live dispatch | oracle subcase -> `MutationSensitive`; `rev869b/T03/all-34-cleanups/action/v3` | C27F-03,-04,-05,-06,-07 |

Count: 108/108 frozen subcases mapped. Each Correction 28 record must add unique raw-fixture ID/hash, action-fixture ID/hash, historical-evidence ID/hash and provenance-source ID; uniqueness of GUID wrappers alone is insufficient.


## Why the Correction 27 evidence produced false confidence

The 15/15 Correction 27 contract results prove exact catalog counts and tokens over in-memory positive fixtures. They do not independently establish that the SQL reducers return authoritative facts with the represented meaning. The focused 75/75 and complete 449/449 results deliberately exclude PostgreSQL execution and contain no independent semantic oracle for the v3 reader results. This is useful offline coverage, but it cannot promote those projections to database acceptance evidence.

The mutation harness catches a broad exception and treats the exception as a killed mutation. An unrelated parser, setup or adapter exception can therefore masquerade as rejection by the intended evidence boundary. In addition, the action and raw-fixture tables copy expected or shared values into the material later judged against those same values. Hashes establish byte integrity of that circular material; they do not establish truth, provenance or semantic independence. Source tests prohibit selected words and require selected fragments, but constants and aliases that satisfy the fragments can still collapse distinct facts.

These are not 34 unrelated implementation failures. They are consequences of the seven consolidated findings and the shared evidence construction path.

## Required source-contract additions

Correction 28 must add all of the following contracts without using PostgreSQL:

1. A frozen, independent reader-semantic catalog for all 133 formula components. Each entry must identify the exact authoritative relation or catalog, join/filter keys, observation stage, result type, expected cardinality and reducer category (`AUTHORITATIVE_RAW`, `DERIVED_BY_VERIFIER`, `SUPPLEMENTARY` or `EXTERNAL_PENDING`). A component cannot be accepted when its value is a constant, a caller echo, an oracle echo or an undocumented inference.
2. Exact source/SQL contracts for every decisive relation, key and stage. Mutation variants that remove or substitute a decisive relation, key, scope predicate or temporal stage must fail the contract.
3. A fixture provenance catalog with exactly 108 unique subcase records. Each record must carry independently authored raw-fixture, action-fixture and historical-evidence identities and hashes. This catalog must not import, instantiate or derive values from the frozen oracle. Changing only oracle expectation or only observed evidence must produce a mismatch.
4. An action catalog that does not use `ScenarioSpec.ExpectedOutcome`, expected SQLSTATE, expected object identity or acceptance-formula literals to construct actual results.
5. Immutable historical identifiers for every before, after and durable observation. Where the formula distinguishes before and after, the records and content hashes must be distinct and ordered; a current snapshot cannot satisfy both terms.
6. A non-PostgreSQL test selection guard that excludes every PostgreSQL-labelled case and records an executed PostgreSQL test count of zero.
7. Retained gates for the exact F23-01 slice hash, REV869A/REV869B uniqueness and adjacency, model/snapshot parity, and newly generated offline Up/Down SQL hashes.

Every one of the 34 scenario contracts must bind its complete component list to these independent outputs. Discovery, label presence, formula text and a self-declared PASS flag are never decisive evidence.

## Required mutation-harness semantics

The future verifier must return a structured result rather than using exception presence as success. At minimum it must contain mutation ID, scenario ID, subcase ID, target boundary, target component, expected rejection code, actual rejection code, evaluation stage, survived/killed state and evidence hash. Expected codes must be boundary-specific, for example `RAW_EXACT_SET`, `RAW_SCOPE`, `RAW_DIGEST`, `ENVELOPE_IDENTITY`, and `ASSERTION_<component-id>`.

For every one of 108 subcases, the real verifier must evaluate and reject the 20 frozen mutation kinds: missing, extra, duplicate, reordered, stale, substituted, cross-scenario, cross-subcase, cross-instance, cross-lease, wrong-version, wrong-count, wrong-state, wrong-SQLSTATE, wrong-object, wrong-audit identity, wrong-history identity, fabricated evidence, assertion removal and decisive-value alteration. The required base total is therefore 2,160 structured mutation results, with any formula-specific assertion-removal cases added explicitly.

An unexpected exception is a harness failure, not a killed mutation. A negative control must inject an unrelated setup/parser failure and prove that it cannot satisfy a mutation expectation. Each mutation test must also prove that the mutation reached the named boundary and that the actual rejection code equals the expected code.

## OR3 executable live path

OR3 must use an actually dispatched, non-PostgreSQL local observation command. `ObserveAsync` must route this command before any Npgsql object or connection can be created. It must consume immutable structured mutation-run records, not SQL, source descriptors, labels or copied oracle values.

Each `MutationRunRecord` must include oracle version/hash, run ID, scenario/subcase identity, mutation ID, target component, expected rejection code, actual rejection code, survived state and evidence hash. The OR3 reducer must derive killed and surviving sets from those records and fail on absence, duplication, unexpected results or identity/hash mismatch. The four T03 subcases require distinct preparation, attempt and evidence identities. The production adapter owner remains an external prerequisite and cannot be inferred from this local validation path.

## Frozen architecture and ACL decision

The frozen architecture remains valid and is retained: external provisioning, a dedicated lifecycle controller, a surviving control-plane database and target-local transactional ledgers. The findings concern observation semantics and test independence; they do not require an architecture redesign. When a target has been dropped, authoritative absence evidence must come from the surviving control plane rather than the absent target.

The existing ownership, grants and restrictive boundaries are also retained. Correction 28 may add narrower authoritative readers but must not widen ledger/export access, introduce `PUBLIC` privileges, weaken ownership, or grant runtime/admin/export roles broader access. Role and ACL observations must independently cover direct grants, inherited grants, aggregate roles, ownership, `PUBLIC`, default privileges and administrative bypass.

Correcting the embedded target v3 reader SQL necessarily changes generated migration SQL. The Correction 27 Up/Down hashes therefore cannot be immutable pins for Correction 28. Migration identity, order and model/snapshot parity remain unchanged, while Correction 28 must independently generate and pin its new offline Up and Down hashes. The accepted F23-01 slice must remain byte-exact at SHA-256 `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523`.

Purchase workflow, permissions, approvals, calculations and audit-history behavior outside the REV869B evidence surface are outside the correction boundary and must remain unchanged.

## External prerequisites

The following remain external and are unavailable in this source-only gate:

- a named production trusted-adapter owner and its production interface;
- isolated PostgreSQL control/target databases, least-privilege roles, aggregate-role topology and representative ACL state;
- TLS/signing identities and secrets supplied outside the repository;
- real provisioned instances and all 108 database-backed fixture/action executions;
- explicit authorization for provisioning, migration apply/remove and later lifecycle acceptance;
- later independent database-backed validation of SQLSTATEs, object identities, row counts, temporal facts, audit/history durability and cleanup/quarantine behavior.

These prerequisites block execution-helper readiness and database acceptance. They do not prevent a bounded source correction from being implemented and reviewed offline.

## Smallest closed Correction 28 file boundary

One bounded source-only Correction 28 is justified, but this report does not authorize its implementation. Its exhaustive ten-file boundary is:

1. `tools/rev869b-control-plane-install.sql` — introduce exact CP-L4/CP-A4 authoritative readers and retained least-privilege ownership/grants while preserving the F23-01 slice.
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` — introduce exact TC4/TP4/TE4/TA4 readers and immutable historical primitives; regenerate the offline SQL contract from this retained migration payload.
3. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs` — pin versioned reader APIs, ownership, grants, default/PUBLIC closure and semantic catalog bindings.
4. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` — bind the frozen scenario plan to the v4 temporal and scoped observation sources without executing PostgreSQL.
5. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` — retain exactly 34 top-level scenarios and add structured per-subcase assertion/mutation evaluation, still excluded from execution here.
6. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` — enforce semantic independence, exact rejection reasons, assertion-removal behavior, F23 retention and new offline SQL hashes.
7. `tests/SESS.NexaERP.Tests/Rev869BCorrection26FrozenOracle.cs` — retain expectations only while versioning the v4 mapping/hash; it must not manufacture actual observations.
8. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` — implement the parser/adapter/verifier changes, narrow structured mutation evaluator and local non-Npgsql OR3 dispatch.
9. `tests/SESS.NexaERP.Tests/Rev869BCorrection28IndependentEvidenceFixtures.cs` — new independent 108-subcase fixture/action/provenance catalog; it must not reference oracle types or expected-value construction.
10. `outputs/rev869b_source_correction_checkpoint_28.md` — new immutable source-correction checkpoint.

No eleventh file is authorized. Specifically excluded are the primary migration class/designer and model snapshot, production services/endpoints/authorizers, `tools/rev869b-control-plane-verify.sql`, provisioning helpers, prior reports/checkpoints and every file outside the list above.

## Objective Correction 28 acceptance and offline validation

The future correction must demonstrate:

- 133/133 formula components mapped bijectively to an exact authoritative observation or an explicitly non-decisive external/supplementary term;
- 108/108 unique subcase raw-fixture, action-fixture and historical-evidence records with independent provenance and hashes;
- 34/34 scenario contracts whose every decisive formula component is executable and mutation-sensitive;
- at least 2,160/2,160 boundary-specific structured mutation results, plus every required formula-specific assertion-removal case;
- 4/4 OR3 local live subcases through the non-Npgsql dispatch path;
- zero shared decisive signatures, echoed labels, copied acceptance counts, sentinel PASS values or oracle-derived actuals;
- build with zero warnings/errors; focused REV869B non-PostgreSQL tests; Correction 28 contract/mutation tests; complete non-PostgreSQL suite; exactly 34 discovered top-level scenarios; and zero executed PostgreSQL tests;
- PowerShell 5.1 AST validation, EF no-connect discovery, REV869A/REV869B uniqueness/adjacency, model/snapshot parity, retained SQL contracts, new independently calculated Up/Down SHA-256 values, exact F23-01 slice hash, ACL/secret/privacy/prohibited-operation scans, exact ten-file scope and `git diff --check`.

Passing implementation-owned tests cannot self-declare source safety or helper readiness. The required successor gate is an internal adversarial source-only precheck, followed—only if that passes—by a separately authorized fresh independent source-only review.

## Rollback boundary, stop conditions and open questions

Correction 28, if separately authorized, must be one source-only commit limited to the ten files above. No history rewrite is permitted. No rollback is executed by this reconciliation. Its offline Down SQL must remove only the newly versioned objects; apply/remove remains prohibited.

Implementation must stop without broadening scope if any required relation or schema needs an unlisted file, the F23-01 slice changes, architecture or ACL redesign becomes necessary, immutable history cannot be represented inside the listed SQL payloads, a production-only interface is required to pass an offline contract, PostgreSQL execution becomes necessary, an eleventh file is needed, or its entry hash/status differs.

Open questions intentionally left to external authorization are the named production adapter owner/interface and the eventual provisioned database topology. The precise new Up/Down hashes cannot be known before the authorized source change and independent offline generation. None of these is an architecture contradiction.

## Single next gate

The single next gate is management authorization for one bounded REV869B Correction 28 source-only implementation using exactly the ten-file boundary above. Correction 28 is not authorized by this report. PostgreSQL execution remains not authorized.

correction_27_failure_reconciliation_state=PASS

correction_28_source_authorization_state=GO

f23_01_state=PASS_RETAINED

f23_02_source_correction_state=FAIL

rev869b_source_safety_state=FAIL

rev869b_execution_helper_readiness_state=FAIL

postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN

trusted_adapter_production_ownership_state=EXTERNAL_PENDING

frozen_architecture_state=RETAIN

acl_boundary_state=RETAIN

external_prerequisite_blocking_state=YES
