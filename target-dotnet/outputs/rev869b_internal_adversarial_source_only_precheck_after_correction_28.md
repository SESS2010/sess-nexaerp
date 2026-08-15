# REV869B Correction 28 internal adversarial source-only precheck

## Verdict

**FAIL.** This is the single authorized internal, non-independent, source-only precheck of committed Correction 28 at `c40dea00d41e12cb1d9b42b0238b30090787dc7f`. Build and offline regression tests pass, but they do not establish trustworthy acceptance evidence. The committed live reader/adapter path cannot satisfy its own identity contract, does not bind subcase identity in the adapter, reads current state under caller-supplied stage labels rather than persisted temporal observations, derives several expected/reference facts from the same snapshot as actual facts, does not verify decisive action-result fields, assigns mutation actual codes from expected codes, and does not execute OR3 through the live T03 scenario path.

The required next gate is a report-only Correction 28 failure reconciliation. No PostgreSQL operation or source correction is authorized by this report.

## Entry gate

| Check | Evidence | Result |
|---|---|---|
| HEAD | `c40dea00d41e12cb1d9b42b0238b30090787dc7f` | PASS |
| Parent | `b7ad8c9517274b98cc44a02c5a640e526c397845` | PASS |
| Target-scoped status | clean before report creation | PASS |
| Correction 28 boundary | exactly the authorized ten files | PASS |
| Committed checkpoint SHA-256 | `0851096E93FEE0A840B7E927709828B2088069BBB2DB8C6C65081BCC4ADC54AE` | PASS |
| Later source/SQL/test/migration/helper changes | none; `c40dea0..HEAD` was empty | PASS |
| F23-01 | 11,001 UTF-8 bytes; `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523` | PASS_RETAINED |
| Frozen architecture | unchanged | RETAIN |
| ACL boundary | six v4 security-definer readers; fixed search paths; PUBLIC function revokes retained | RETAIN |
| Trusted adapter production owner | `EXTERNAL_PENDING` | PASS |

## Adversarial findings

### C28P-01 — the live reader scope cannot satisfy the adapter and subcase scope is not consumed

`BuildReadCommand` sends `operationId = ScenarioPreparation.AttemptId` and `scenarioExecutionId = ScenarioPreparation.PreparationId` (`Rev869BLifecycleControllerClient.cs:1683-1688`). `RequirePreparation` requires those preparation/attempt IDs to equal the frozen subcase IDs (`:1725-1729`). `AdaptStage`, however, requires the returned operation and execution IDs to equal separately generated Correction 28 fixture `ActionIdentity` and `PreparationIdentity` (`:833-849`). Those identity families differ. For P01, the frozen preparation/attempt IDs are `9499763b-394f-e65f-63de-0c2addaef4f7` / `dce222cf-a613-4b69-190d-4d1c00281810`, while the independent fixture preparation/action IDs are `fed5dca9-dfb9-28a9-7ebe-7831dabd5448` / `9c595221-5595-f832-d24f-20136b3ab210`. Therefore a live database response is rejected before formula evaluation.

The raw schema parses `subcaseId` (`:705-712`), but `AdaptStage` never compares `observation.Scope.SubcaseId` to `subcase.SubcaseId` (`:838-849`). Cross-subcase evidence can therefore survive the authenticated scope check. Company, instance, lease, version, operation, execution and stage are compared; subcase is not.

### C28P-02 — before/after/durable are current-state relabels, not fresh historical observations

`ObserveAsync` opens a new connection for each stage, which is a useful transport separation, but every v4 SQL reader queries current relations and merely echoes `observation_stage` into scope and transaction text. No reader selects a persisted observation, action boundary, historical sequence, or stage-specific cutoff. A stage parameter therefore changes the label and digest while the relation snapshot may be identical.

The target readers also build reference values and actual values from the same CTE snapshot. Examples include command `expectedBusinessRowDelta = business_count`, both before/after claim hashes from `claim_sha`, purge `frozenCandidateSha256 = candidate_sha` and `contextBeforeSha256 = context_sha`, and export before/after hashes and counts from the same current `rows` CTE. These comparisons can self-produce equality and cannot prove temporal invariants. Control ACL `reportedDeltaSha256`, `cleanupFingerprint`, `seededDeltaSha256` and `baselineFingerprint` all reduce to the same current ACL snapshot hash.

Although the fixture catalog has 108 unique observation/envelope identities and five unique labels per subcase, those are deterministic labels. They are not database observation identities or durable-history records.

### C28P-03 — fixture construction is oracle-equivalent and pristine PASS is authored

The independent fixture file has no direct type reference to `Rev869BCorrection26FrozenOracle`, but independence is semantic, not textual. Its 34 `ScenarioActionShapes` reproduce the frozen oracle terminal state, SQLSTATE/error code and database object values. `Correction28RawFactTemplates` in the lifecycle client hard-codes the decisive values and references for all 133 frozen selectors. `BuildDatabaseShapedRawEvidence` copies those templates into typed raw facts, computes a digest, adapts them, and the tests require pristine PASS. The fixture path does not query an independent producer and can author exactly the values the verifier expects.

Tampering is passed through the typed adapter, but that proves only that mutations of the self-authored JSON are detected. It does not prove the pristine facts were independently obtained.

### C28P-04 — action results are not independently verified

The offline action object is built from the hard-coded fixture catalog (`:804-827`). `VerifyEvidence` never reads `bundle.Action`; repository search found no consumption of `sqlState`, `errorCode`, `databaseObject`, `affectedRows` or `actionReached`. In the live path, the post-formula correlation check verifies identities and only `TerminalState == subcase.ExpectedResult` (`:120-127`). The scenario test checks `ActionReached` but not exact SQLSTATE, object, row count, or rejection reason. Expected result is also sent to both prepare and action endpoints. Expected and actual construction paths are therefore not independent.

### C28P-05 — mutation rejection codes are circular and classification is broad

Twenty JSON mutations per subcase execute offline, and unexpected exception types escape. However, `EvaluatePipelineMutation` computes `expectedCode`, obtains only a boolean `killed`, and assigns `actualCode = expectedCode` whenever killed (`:1052-1062`). It does not capture the thrown exception type/message or verifier rejection identity. `IsExpectedAdapterRejection` accepts broad message substrings such as `Raw`, `fact`, `scope`, `evidence`, `digest`, or `contract` (`:1097-1103`). Thus the asserted exact expected/actual equality is constructed, not observed.

Decisive assertion removal is rejected by `ValidateContract` because metadata no longer matches the frozen component set; it does not demonstrate that an executable evidence run fails due to the removed decisive check.

### C28P-06 — OR3 is offline, malformed for adaptation, and not live-dispatched by T03

The OR3 branch precedes Npgsql construction, but the T03 `[Fact]` directly executes the offline fixture/mutation loop; it does not call `RunAcceptanceScenarioAsync` (`Rev869BCorrection17PostgresScenarios.cs:40-87`). The live `ObserveLocalOr3` branch is therefore not exercised by T03.

Further, `SerializeLocalOr3` omits required raw-scope property `subcaseId` (`Rev869BLifecycleControllerClient.cs:1615-1624` versus exact schema at `:705-707`), so parsing its output fails. `DispatchLocalOr3` sets scope operation ID to observation ID (`:1081-1082`), while `AdaptStage` requires fixture action ID (`:846`). The local OR3 route cannot reach a valid adapted observation. Wrong-operation and altered-record unit tests only exercise helpers directly.

### C28P-07 — reader semantics, ACL projection, and purge scope are not authoritative

| Reader | Exact relation/function trace and predicates | Adversarial result |
|---|---|---|
| `CP-L4` / `rev869b_read_lifecycle_facts_v4` | lease by lease/version/instance; events by request **OR** attempt; attempts by attempt+lease; outcomes/quarantine by attempt; decision by decision+lease | FAIL: scenario execution, subcase and stage are echoes; OR can combine separate event scopes; several “per-boundary” facts are ungrouped totals; `sourceRowCount` reports one aggregate, not source cardinality. |
| `CP-A4` / `rev869b_read_control_acl_facts_v4` | full current database/schema/nexa relation/function/default ACL and role-membership snapshot plus control manifest fingerprint | FAIL: principal/object/operation do not scope the snapshot; multiple named facts are aliases of one manifest comparison or one ACL hash; no seeded mutation/action relation is read. |
| `TC4` / `rev869b_read_command_facts_v4` | request by company+command; attempt by request+attempt; contexts/claims/outcomes/receipts through attempt | FAIL: request/attempt are not bound to target identity/lease; lease version, scenario execution, subcase and stage are echoes; before/reference and actual values share one current snapshot. |
| `TP4` / `rev869b_read_purge_facts_v4` | identity; exact auth/root/batch; attempt with execution=attempt; candidates/events by attempt; eligible contexts by company+cutoff ordered and limited by maximum rows | FAIL: lease version, scenario execution, subcase and stage are echoes; before/current hashes share one query; “consumed” means merely non-expired; current eligibility is relabeled as before/after; target-wide temporal substitution is not excluded. |
| `TE4` / `rev869b_read_export_facts_v4` | exact company/auth/batch/as-of; rows by batch with `LIMIT 1000`; releases by batch | FAIL: before/after hashes share current rows; `laterEligibleRowCount` and `laterRowInBatchCount` are the same batch-row expression and never inspect later eligible source rows; scenario/subcase/stage are echoes. |
| `TA4` / `rev869b_read_target_acl_facts_v4` | identity plus database/schema/default ACL, selected relation/function ACL, membership, ownership, role capability and effective privilege | FAIL: invalid object identity broadens relation/function predicates through `coalesce(to_regclass($8), c.oid)` / equivalent function expression; company is not relation-bound; `USAGE`/`DROP` are searched as relation ACL strings and yield misleading zeroes; stage/subcase/execution are not database facts. |
| `OR3` | local mutation records only | FAIL: not a database reader, not executed through live T03, serializer lacks `subcaseId`, and operation binding contradicts adapter. |

Null/missing/extra/duplicate raw JSON properties generally fail closed, and scalar subqueries can fail on duplicate rows. Those local checks do not cure the provenance, scope and temporal defects above.

### C28P-08 — source contracts prove labels and self-authored execution, not live wiring

The source contracts extensively use `Assert.Contains`/`DoesNotContain` over source text. The executable Correction 28 tests run `BuildDatabaseShapedRawEvidence`, the local adapter/verifier and local mutation helpers. They do not execute any of the six SQL functions, do not execute 33 live acceptance scenarios, do not capture live action-result fields, and do not execute live OR3. Consequently 16/16 passing is regression evidence for the in-memory design, not proof of authoritative wiring.

## ACL, purge, and enterprise-scale assessment

ACL ownership/revoke/grant boundaries are retained, but `acl_projection_state=FAIL` because the decisive CP-A4/TA4 facts are not exact action-scoped projections and invalid object identities broaden catalog selection. Direct, PUBLIC, ownership, default ACL, membership and effective checks are present; their projection into the acceptance formula is not authoritative.

`purge_scope_state=FAIL`: authorization/root/batch/attempt predicates exist and eligibility is company/cutoff/maximum bounded, but observation stage/subcase/execution are caller echoes, historical before values are computed from current state, and target-wide temporal substitution is not rejected.

`enterprise_scale_compatibility_state=PASS`: customer/master/ledger reads are joined from exact request, attempt, authorization or batch roots; purge eligibility is ordered and capped by authorization maximum; export batch rows are capped at 1,000; supporting request/attempt and organization/opened-token indexes are present. Catalog-wide ACL scans are metadata scans, not complete business-ledger loads. This scale result does not imply semantic correctness.

## Inventory results

- Frozen formula terms: exactly 133; 0 accepted, 133 failed.
- Scenarios: exactly 34; 0 passed, 34 failed.
- Subcases: exactly 108; 0 passed, 108 failed.
- Fixture observation identities: 108 unique.
- Fixture envelope identities: 108 unique.
- Fixture preparation, attempt and action identities: 108 unique in each family.

Every term fails this gate because the common live identity/provenance/temporal pipeline is invalid. The term-to-reader assertion mapping is exhaustive:

| Formula component | Reader/function | Stage | Selector | Declared source | Result |
|---|---|---|---|---|---|
| `P01:formula-pin-mismatch` | `CP-A4` / `rev869b_read_control_acl_facts_v4` | Before | `pinMismatchCount` | control manifest + pg_catalog ACL/member/effective facts | **FAIL** |
| `P01:formula-target-acl-delta` | `TA4` / `rev869b_read_target_acl_facts_v4` | After | `targetAclDeltaCount` | target identity + pg_catalog + scoped protected/audit rows | **FAIL** |
| `P01:formula-verify` | `CP-A4` / `rev869b_read_control_acl_facts_v4` | Before | `verificationMismatchCount` | control manifest and control ACL projections | **FAIL** |
| `P02:formula-pin-mismatch` | `CP-A4` / `rev869b_read_control_acl_facts_v4` | Before | `pinMismatchCount` | control manifest + pg_catalog ACL/member/effective facts | **FAIL** |
| `P02:formula-lease-zero` | `CP-L4` / `rev869b_read_lifecycle_facts_v4` | Durable | `allocatedLeaseCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | **FAIL** |
| `P02:formula-action-zero` | `CP-L4` / `rev869b_read_lifecycle_facts_v4` | Durable | `lifecycleMutationCount` | control lease/event/attempt projections | **FAIL** |
| `P03:formula-seeded-one` | `CP-A4` / `rev869b_read_control_acl_facts_v4` | Before | `seededDeltaCount` | control manifest + pg_catalog ACL/member/effective facts | **FAIL** |
| `P03:formula-reported-delta` | `CP-A4` / `rev869b_read_control_acl_facts_v4` | Before | `reportedDeltaSha256` | control manifest + pg_catalog ACL/member/effective facts | **FAIL** |
| `P03:formula-protected-zero` | `CP-A4` / `rev869b_read_control_acl_facts_v4` | Before | `protectedMutationCount` | control manifest + pg_catalog ACL/member/effective facts | **FAIL** |
| `P03:formula-cleanup-baseline` | `CP-A4` / `rev869b_read_control_acl_facts_v4` | Before | `cleanupFingerprint` | control manifest + pg_catalog ACL/member/effective facts | **FAIL** |
| `L01:formula-reserved` | `CP-L4` / `rev869b_read_lifecycle_facts_v4` | Durable | `reservedEventCount` | lifecycle ledgers | **FAIL** |
| `L01:formula-branch-xor` | `CP-L4` / `rev869b_read_lifecycle_facts_v4` | Durable | `resumeSameAttempt_xor_authorizedCleanup` | lifecycle ledgers | **FAIL** |
| `L01:formula-duplicates-zero` | `CP-L4` / `rev869b_read_lifecycle_facts_v4` | Durable | `duplicateAttemptCount` | lifecycle ledgers | **FAIL** |
| `L02:formula-boundary-count` | `CP-L4` / `rev869b_read_lifecycle_facts_v4` | Durable | `boundaryCount` | lifecycle ledgers | **FAIL** |
| `L02:formula-started-each` | `CP-L4` / `rev869b_read_lifecycle_facts_v4` | Durable | `startedAttemptsPerBoundary` | lifecycle ledgers | **FAIL** |
| `L02:formula-reconciled-each` | `CP-L4` / `rev869b_read_lifecycle_facts_v4` | Durable | `reconciledAttemptsPerBoundary` | lifecycle ledgers | **FAIL** |
| `L02:formula-target-each` | `TA4` / `rev869b_read_target_acl_facts_v4` | After | `targetCountPerBoundary` | target identity/catalog | **FAIL** |
| `L02:formula-roles-each` | `TA4` / `rev869b_read_target_acl_facts_v4` | After | `roleSetCountPerBoundary` | target identity/catalog | **FAIL** |
| `L03:formula-requests` | `CP-L4` | Durable | `cleanupRequestCount` | lifecycle ledgers | **FAIL** |
| `L03:formula-dropstarted` | `CP-L4` | Durable | `dropStartedEventCount` | lifecycle ledgers | **FAIL** |
| `L03:formula-active` | `CP-L4` | Durable | `activeDropAttemptCount` | lifecycle ledgers | **FAIL** |
| `L03:formula-physical` | `CP-L4` | Durable | `normalDropTerminalChainCount` | lifecycle outcome chain | **FAIL** |
| `L03:formula-authorization-chain` | `CP-L4` | Durable | `authorizationRegistrationTransitionCount` | lifecycle ledgers | **FAIL** |
| `L04:formula-dropstarted` | `CP-L4` | Durable | `dropStartedEventsPerBoundary` | lifecycle ledgers | **FAIL** |
| `L04:formula-finalized` | `CP-L4` | Durable | `finalizedEventsPerBoundary` | lifecycle ledgers | **FAIL** |
| `L04:formula-physical` | `CP-L4` | Durable | `terminalOutcomeCountPerBoundary` | lifecycle outcome chain | **FAIL** |
| `L04:formula-target-zero` | `TA4` | After | `targetCount` | target identity/catalog | **FAIL** |
| `L04:formula-roles-zero` | `TA4` | After | `roleCount` | target identity/catalog | **FAIL** |
| `L05:formula-use-zero` | `TA4` | After | `useMutationCount` | target ACL projection | **FAIL** |
| `L05:formula-drop-zero` | `TA4` | After | `dropMutationCount` | target ACL projection | **FAIL** |
| `L05:formula-quarantine-one` | `CP-L4` | Durable | `quarantineOutcomeCount` | lifecycle ledgers | **FAIL** |
| `R01:formula-decision-one` | `CP-L4` | Durable | `decisionCount` | lifecycle/recovery ledgers | **FAIL** |
| `R01:formula-consumed-attempt` | `CP-L4` | Durable | `consumedAttemptId` | lifecycle/recovery ledgers | **FAIL** |
| `R01:formula-action` | `CP-L4` | Durable | `authorizedAction` | lifecycle/recovery ledgers | **FAIL** |
| `R01:formula-recovery-one` | `CP-L4` | Durable | `recoveryAttemptCount` | lifecycle/recovery ledgers | **FAIL** |
| `R01:formula-finalized-one` | `CP-L4` | Durable | `finalizedEventCount` | lifecycle ledgers | **FAIL** |
| `R02:formula-attempts-zero` | `CP-L4` | Durable | `newAttemptCount` | lifecycle ledgers | **FAIL** |
| `R02:formula-events-zero` | `CP-L4` | Durable | `newEventCount` | lifecycle ledgers | **FAIL** |
| `R02:formula-consumed-one` | `CP-L4` | Durable | `decisionConsumedCount` | recovery decisions | **FAIL** |
| `R03:formula-failure-one` | `CP-L4` | Durable | `cleanupFailureCount` | lifecycle outcomes | **FAIL** |
| `R03:formula-old-zero` | `CP-L4` | Durable | `oldDecisionAcceptedCount` | recovery decisions | **FAIL** |
| `R03:formula-fresh-one` | `CP-L4` | Durable | `freshLinkedDecisionCount` | recovery decisions | **FAIL** |
| `R03:formula-consumed-one` | `CP-L4` | Durable | `freshDecisionConsumedCount` | recovery decisions | **FAIL** |
| `R03:formula-finalized-one` | `CP-L4` | Durable | `finalizedEventCount` | lifecycle ledgers | **FAIL** |
| `C01:formula-business-delta` | `TC4` / `rev869b_read_command_facts_v4` | Durable | `businessRowDelta` | command ledgers | **FAIL** |
| `C01:formula-history-delta` | `TC4` | Durable | `historyRowDelta` | command ledgers | **FAIL** |
| `C01:formula-receipt-one` | `TC4` | Durable | `receiptCount` | command ledgers | **FAIL** |
| `C01:formula-outcome-one` | `TC4` | Durable | `committedOutcomeCount` | command ledgers | **FAIL** |
| `C01:formula-active-zero` | `TC4` | Durable | `activeAttemptCount` | command ledgers | **FAIL** |
| `C02:formula-business-same` | `TC4` | Durable | `businessAfter2Sha256` | command ledgers | **FAIL** |
| `C02:formula-history-same` | `TC4` | Durable | `historyAfter2Sha256` | command ledgers | **FAIL** |
| `C02:formula-receipt-same` | `TC4` | Durable | `receiptId2` | command ledgers | **FAIL** |
| `C02:formula-response-same` | `TC4` | Durable | `responseSha2562` | command ledgers | **FAIL** |
| `C02:formula-receipt-one` | `TC4` | Durable | `receiptCount` | command ledgers | **FAIL** |
| `C03:formula-digest-different` | `TC4` | Durable | `changedDigest` | command ledgers | **FAIL** |
| `C03:formula-request-zero` | `TC4` | Durable | `requestDelta` | command ledgers | **FAIL** |
| `C03:formula-attempt-zero` | `TC4` | Durable | `attemptDelta` | command ledgers | **FAIL** |
| `C03:formula-business-zero` | `TC4` | Durable | `businessHistoryDelta` | command ledgers | **FAIL** |
| `C04:formula-business-zero` | `TC4` | Durable | `businessRowDelta` | command ledgers | **FAIL** |
| `C04:formula-history-zero` | `TC4` | Durable | `historyRowDelta` | command ledgers | **FAIL** |
| `C04:formula-receipt-zero` | `TC4` | Durable | `receiptDelta` | command ledgers | **FAIL** |
| `C04:formula-rollback-one` | `TC4` | Durable | `rolledBackOutcomeCount` | command ledgers | **FAIL** |
| `C05:formula-business-zero` | `TC4` | Durable | `businessHistoryReceiptDelta` | command ledgers | **FAIL** |
| `C05:formula-rollback-one` | `TC4` | Durable | `rolledBackOutcomeCount` | command ledgers | **FAIL** |
| `C05:formula-opened-attempt` | `TC4` | Durable | `openedAttemptId` | command ledgers | **FAIL** |
| `C06:formula-subcases-four` | `TC4` | Durable | `interruptionSubcaseCount` | command ledgers | **FAIL** |
| `C06:formula-distinct-evidence` | `TC4` | Durable | `distinctEvidenceIdCount` | command ledgers | **FAIL** |
| `C06:formula-terminal-each` | `TC4` | Durable | `terminalOutcomeCountPerAttempt` | command ledgers | **FAIL** |
| `C07:formula-requests-two` | `TC4` | Durable | `startRequestCount` | command ledgers | **FAIL** |
| `C07:formula-started-one` | `TC4` | Durable | `startedAttemptCount` | command ledgers | **FAIL** |
| `C07:formula-active-one` | `TC4` | Durable | `activeAttemptCount` | command ledgers | **FAIL** |
| `C07:formula-unrelated-zero` | `TC4` | Durable | `unrelatedMutationCount` | command ledgers | **FAIL** |
| `C08:formula-accepted-zero` | `TC4` | Durable | `acceptedSubstitutionCount` | command ledgers | **FAIL** |
| `C08:formula-contexts-zero` | `TC4` | Durable | `contextDelta` | command ledgers | **FAIL** |
| `C08:formula-receipts-zero` | `TC4` | Durable | `receiptDelta` | command ledgers | **FAIL** |
| `C08:formula-business-zero` | `TC4` | Durable | `businessHistoryDelta` | command ledgers | **FAIL** |
| `G01:formula-attempts-zero` | `TP4` / `rev869b_read_purge_facts_v4` | Durable | `startedAttemptCount` | purge ledgers | **FAIL** |
| `G01:formula-candidates-zero` | `TP4` | Durable | `candidateCount` | purge ledgers | **FAIL** |
| `G01:formula-events-zero` | `TP4` | Durable | `purgeEventCount` | purge ledgers | **FAIL** |
| `G02:formula-eligible-zero` | `TP4` | Durable | `eligibleBeforeCount` | purge/context ledgers | **FAIL** |
| `G02:formula-frozen-zero` | `TP4` | Durable | `frozenCandidateCount` | purge ledgers | **FAIL** |
| `G02:formula-deleted-zero` | `TP4` | Durable | `deletedRowCount` | purge ledgers | **FAIL** |
| `G02:formula-event-one` | `TP4` | Durable | `zeroRowsEventCount` | purge events | **FAIL** |
| `G03:formula-eligible-positive` | `TP4` | Durable | `eligibleBeforeCount` | purge/context ledgers | **FAIL** |
| `G03:formula-frozen-equals` | `TP4` | Durable | `frozenCandidateCount` | purge ledgers | **FAIL** |
| `G03:formula-deleted-equals` | `TP4` | Durable | `deletedRowCount` | purge ledgers | **FAIL** |
| `G03:formula-remaining-zero` | `TP4` | Durable | `remainingEligibleCount` | purge/context ledgers | **FAIL** |
| `G03:formula-event-one` | `TP4` | Durable | `succeededEventCount` | purge events | **FAIL** |
| `G04:formula-hash-different` | `TP4` | Durable | `currentCandidateSha256` | purge candidates | **FAIL** |
| `G04:formula-deleted-zero` | `TP4` | Durable | `deletedRowCount` | purge ledgers | **FAIL** |
| `G04:formula-context-same` | `TP4` | Durable | `contextAfterSha256` | purge/context ledgers | **FAIL** |
| `G04:formula-event-one` | `TP4` | Durable | `failedEventCount` | purge events | **FAIL** |
| `G05:formula-deleted-zero` | `TP4` | Durable | `deletedRowCount` | purge ledgers | **FAIL** |
| `G05:formula-context-same` | `TP4` | Durable | `contextAfterSha256` | purge/context ledgers | **FAIL** |
| `G05:formula-event-one` | `TP4` | Durable | `failedEventCount` | purge events | **FAIL** |
| `G06:formula-starts-two` | `TP4` | Durable | `concurrentStartCount` | purge attempts | **FAIL** |
| `G06:formula-consumed-one` | `TP4` | Durable | `consumedAuthorizationCount` | purge authorization | **FAIL** |
| `G06:formula-execution-max` | `TP4` | Durable | `executionCount` | purge attempts | **FAIL** |
| `G06:formula-child-one` | `TP4` | Durable | `activeChildCount` | purge authorization | **FAIL** |
| `G06:formula-substituted-zero` | `TP4` | Durable | `substitutedChildCount` | purge authorization | **FAIL** |
| `E01:formula-within-max` | `TE4` / `rev869b_read_export_facts_v4` | Durable | `preparedRowCountWithinMaximum` | export ledgers | **FAIL** |
| `E01:formula-hash` | `TE4` | Durable | `preparedSha256` | export rows | **FAIL** |
| `E01:formula-excluded-zero` | `TE4` | Durable | `excludedFieldCount` | export rows | **FAIL** |
| `E01:formula-event-one` | `TE4` | Durable | `preparedEventCount` | export batch | **FAIL** |
| `E02:formula-rows-same` | `TE4` | Durable | `preparedAfterSha256` | export rows | **FAIL** |
| `E02:formula-count-same` | `TE4` | Durable | `preparedAfterCount` | export rows | **FAIL** |
| `E02:formula-later-one` | `TE4` | Durable | `laterEligibleRowCount` | export rows | **FAIL** |
| `E02:formula-later-batch-zero` | `TE4` | Durable | `laterRowInBatchCount` | export rows | **FAIL** |
| `E03:formula-released-zero` | `TE4` | Durable | `releasedRowCount` | export releases | **FAIL** |
| `E03:formula-events-zero` | `TE4` | Durable | `newReleaseEventCount` | export releases | **FAIL** |
| `E03:formula-batch-same` | `TE4` | Durable | `preparedAfterSha256` | export rows | **FAIL** |
| `E04:formula-release-distinct` | `TE4` | Durable | `releaseId2` | export releases | **FAIL** |
| `E04:formula-prior-link` | `TE4` | Durable | `priorReleaseId2` | export releases | **FAIL** |
| `E04:formula-active-one` | `TE4` | Durable | `activeReleaseCount` | export releases | **FAIL** |
| `E04:formula-success-max` | `TE4` | Durable | `deliverySuccessCount` | export releases | **FAIL** |
| `E04:formula-batch-same` | `TE4` | Durable | `batchAfterSha256` | export rows | **FAIL** |
| `A01:formula-unexpected-zero` | `CP-A4` | Before | `controlObservedMinusExpectedCount` | control ACL projections | **FAIL** |
| `A01:formula-missing-zero` | `TA4` | After | `targetExpectedMinusObservedCount` | target ACL projections | **FAIL** |
| `A01:formula-dimensions` | `TA4` | After | `targetAclDimensionCount` | target ACL projections | **FAIL** |
| `A02:formula-allowed-zero` | `TA4` | After | `allowedProtectedOperationCount` | effective privilege | **FAIL** |
| `A02:formula-tuple-count` | `TA4` | After | `durableDenialCount` | effective privilege | **FAIL** |
| `A02:formula-fingerprint-same` | `TA4` | After | `protectedAfterSha256` | target ACL snapshot | **FAIL** |
| `T01:formula-lease-one` | `CP-L4` | Durable | `leaseCount` | lifecycle ledgers | **FAIL** |
| `T01:formula-target-one` | `TA4` | After | `targetCount` | target identity | **FAIL** |
| `T01:formula-admin-zero` | `TA4` | After | `administrativeBypassCount` | role capability | **FAIL** |
| `T01:formula-fixture` | `TA4` | After | `fixturePrepared` | target identity | **FAIL** |
| `T02:formula-instance-different` | `CP-L4` | Durable | `survivingAttemptCount` | lifecycle attempts | **FAIL** |
| `T02:formula-attempt-same` | `CP-L4` | Durable | `reconciledAttemptId` | lifecycle attempts | **FAIL** |
| `T02:formula-dropstarted-one` | `CP-L4` | Durable | `dropStartedEventCount` | lifecycle events | **FAIL** |
| `T02:formula-finalized-one` | `CP-L4` | Durable | `finalizedEventCount` | lifecycle events | **FAIL** |
| `T02:formula-cleanup-one` | `CP-L4` | Durable | `cleanupEvidenceCount` | lifecycle outcomes | **FAIL** |
| `T03:formula-killed-equals` | `OR3` / `ObserveLocalOr3` | Durable | `killedMutants` | local mutation records | **FAIL** |
| `T03:formula-survivors-zero` | `OR3` / `ObserveLocalOr3` | Durable | `survivingMutants` | local mutation records | **FAIL** |

## Scenario and subcase PASS/FAIL matrix

| Scenario | Terms | Subcases | Exhaustive subcase IDs | Result |
|---|---:|---:|---|---|
| `P01` | 3 | 1 | P01:p01-action | **FAIL** |
| `P02` | 3 | 5 | P02:wrong-system-id, P02:wrong-tls-spki, P02:wrong-endpoint, P02:wrong-source, P02:wrong-manifest | **FAIL** |
| `P03` | 4 | 4 | P03:unexpected-role, P03:unexpected-database, P03:unexpected-object, P03:unexpected-grant | **FAIL** |
| `L01` | 3 | 3 | L01:reserved, L01:interrupt-before-role, L01:resume-or-approved-cleanup | **FAIL** |
| `L02` | 5 | 6 | L02:reserved, L02:database-created, L02:roles-created, L02:migration-applied, L02:verified, L02:ready | **FAIL** |
| `L03` | 5 | 5 | L03:ready-cleanup-race, L03:inuse-cleanup-race, L03:single-dropstarted, L03:single-drop, L03:authorization-event-binding | **FAIL** |
| `L04` | 5 | 5 | L04:before-drop, L04:during-drop, L04:after-drop, L04:during-role-cleanup, L04:finalized-once | **FAIL** |
| `L05` | 3 | 5 | L05:mismatch-detected, L05:use-denied, L05:drop-denied, L05:quarantine-authorized, L05:quarantined | **FAIL** |
| `R01` | 5 | 1 | R01:r01-action | **FAIL** |
| `R02` | 3 | 8 | R02:wrong, R02:expired, R02:replayed, R02:foreign, R02:pre-state, R02:action, R02:nonce, R02:valid-preserved | **FAIL** |
| `R03` | 5 | 5 | R03:first-failure, R03:restart, R03:old-decision-denied, R03:fresh-linked-decision, R03:finalized | **FAIL** |
| `C01` | 5 | 1 | C01:c01-action | **FAIL** |
| `C02` | 5 | 1 | C02:c02-action | **FAIL** |
| `C03` | 4 | 1 | C03:c03-action | **FAIL** |
| `C04` | 4 | 5 | C04:receipt-failpoint, C04:business-rollback, C04:history-rollback, C04:receipt-rollback, C04:durable-noncommit | **FAIL** |
| `C05` | 3 | 1 | C05:c05-action | **FAIL** |
| `C06` | 3 | 4 | C06:before-open, C06:after-open, C06:during-commit, C06:after-response | **FAIL** |
| `C07` | 4 | 1 | C07:c07-action | **FAIL** |
| `C08` | 4 | 8 | C08:pool, C08:backend, C08:transaction, C08:actor, C08:organization, C08:version, C08:role, C08:operation | **FAIL** |
| `G01` | 3 | 5 | G01:missing, G01:expired, G01:wrong-target, G01:wrong-batch, G01:wrong-organization | **FAIL** |
| `G02` | 4 | 1 | G02:g02-action | **FAIL** |
| `G03` | 5 | 1 | G03:g03-action | **FAIL** |
| `G04` | 4 | 1 | G04:g04-action | **FAIL** |
| `G05` | 3 | 3 | G05:delete-failpoint, G05:deletion-rollback, G05:independent-audit | **FAIL** |
| `G06` | 5 | 4 | G06:concurrent-start, G06:concurrent-execute, G06:substituted-policy-denied, G06:exact-retry | **FAIL** |
| `E01` | 4 | 1 | E01:e01-action | **FAIL** |
| `E02` | 4 | 1 | E02:e02-action | **FAIL** |
| `E03` | 3 | 4 | E03:expired, E03:wrong-batch, E03:terminal, E03:concurrent | **FAIL** |
| `E04` | 5 | 3 | E04:old-release-interrupted, E04:fresh-release-started, E04:batch-unchanged | **FAIL** |
| `A01` | 3 | 1 | A01:a01-action | **FAIL** |
| `A02` | 3 | 7 | A02:runtime, A02:purge, A02:export, A02:recovery, A02:administrator, A02:ordinary-principal, A02:public | **FAIL** |
| `T01` | 4 | 1 | T01:t01-action | **FAIL** |
| `T02` | 5 | 1 | T02:t02-action | **FAIL** |
| `T03` | 2 | 4 | T03:all-34-actions, T03:all-34-reads, T03:all-34-assertions, T03:all-34-cleanups | **FAIL** |

## Reproduced offline validation

| Validation | Result |
|---|---|
| Build | PASS: 0 warnings, 0 errors |
| Correction 28 tier | PASS: 16/16 |
| Focused REV869B non-PostgreSQL tier | PASS: 76/76 |
| Complete non-PostgreSQL suite | PASS: 450/450 |
| PowerShell 5.1 AST | PASS: 24 scripts, 0 errors; 5.1.19041.6456 |
| EF migration discovery | PASS: `--no-connect`, inert `127.0.0.1:1`; 13 migrations |
| REV869A/REV869B | PASS: unique in EF primary inventory and adjacent at ordinals 12/13 |
| Model/snapshot and retained SQL | PASS: 2/2 explicit no-connect contracts |
| Offline Up SQL | PASS: 324,914 bytes; 2,635 lines; `39B067351894AB5732B6DF9C6348B04D708780AFAA18E073F8E6594D07FAF213` |
| Offline Down SQL | PASS: 11,720 bytes; 231 lines; `FC4BCB671501D601041FCED25D6053545BE9F38CF1D9982006953F47229E0AE4` |
| F23-01 | PASS_RETAINED: 11,001 bytes; `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523` |
| Secret scan of Correction 28 added lines | PASS: 0 matches |
| Privacy scan of Correction 28 added lines | PASS: 0 matches |
| Prohibited-operation invocation scan | PASS: 0 matches |
| ACL boundary scan | PASS: six v4 security-definer readers, two PUBLIC function revoke boundaries |
| Enterprise-scale bounded-query scan | PASS, with semantic defects reported above |
| `git diff --check HEAD^ HEAD` | PASS |
| PostgreSQL connections/commands/executions | `0 / 0 / 0` |

No PostgreSQL, provisioning, migration, lifecycle, purge, recovery, quarantine, export or production operation was executed. No legacy-reference content was accessed.

correction_28_internal_precheck_state=FAIL

correction_28_internal_precheck_independence_state=NOT_INDEPENDENT

f23_01_state=PASS_RETAINED

f23_02_internal_precheck_state=FAIL

formula_term_pass_count=0

formula_term_fail_count=133

scenario_pass_count=0

scenario_fail_count=34

subcase_pass_count=0

subcase_fail_count=108

authoritative_reader_state=FAIL

fresh_observation_state=FAIL

independent_fixture_state=FAIL

action_result_independence_state=FAIL

mutation_rejection_state=FAIL

live_or3_dispatch_state=FAIL

acl_projection_state=FAIL

purge_scope_state=FAIL

enterprise_scale_compatibility_state=PASS

trusted_adapter_production_ownership_state=EXTERNAL_PENDING

frozen_architecture_state=RETAIN

acl_boundary_state=RETAIN

external_prerequisite_blocking_state=YES

rev869b_source_safety_state=FAIL

rev869b_execution_helper_readiness_state=FAIL

postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
