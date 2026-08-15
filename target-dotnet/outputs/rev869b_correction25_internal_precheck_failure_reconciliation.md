# REV869B Correction 25 internal-precheck failure reconciliation

Date: 2026-08-15

Decision: **GO** for one bounded source-only Correction 26 under the exact allowlist in this report. The frozen architecture and ACL ownership boundary remain retained. This report reconciles the failed evidence interface; it does not implement Correction 26 or claim source safety.

## 1. Entry gate and authority

| Gate | Evidence | Result |
|---|---|---|
| Authorized HEAD | `0ac13207a7e083943944af83899c1212424c21e9` | PASS |
| Expected parent | `1a19780ca8a85415adc54d2926055a733ba94253` | PASS |
| Authoritative precheck | `outputs/rev869b_correction25_internal_adversarial_precheck.md` | PASS |
| Precheck SHA-256 | `0415257F239AA0BD847767F11C677D18DB377FF442C19DB59C06D7485CE93135` | PASS |
| Precheck commit scope | exactly one report | PASS |
| Later commits/source changes | none | PASS |
| Target-scoped status | clean | PASS |
| F23-01 accepted slice | unchanged; SHA-256 `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523` | PASS_RETAINED |
| Architecture / ACL boundary | external provisioning, dedicated lifecycle controller, surviving control plane, target-local ledgers and verifier-only grants retained | PASS |

The authoritative precheck was read completely. No PostgreSQL, provisioning, migration, lifecycle, purge, recovery, quarantine, export, production or external operation was executed. `../legacy-reference/` was not accessed.

## 2. Exact root-cause matrix

| ID | Class | Root cause | Affected scope | Correction 26 requirement | Acceptance evidence |
|---|---|---|---|---|---|
| RC26-01 | SOURCE_DEFECT | Runtime evaluates raw reader JSON, but all 34 formulas contain absent decisive selectors. | 133 terms; reported 105 missing names | Typed v2 reader schemas plus explicit local reducers; build must fail on unknown selector/type. | Oracle-to-reader schema join reports 100% coverage, no unknown/duplicate semantic selector. |
| RC26-02 | TEST_DESIGN_DEFECT | Synthetic pristine evidence is generated from the assertions it is meant to test. | all 34 | Delete assertion-derived evidence builder from acceptance proof; use reader-shaped fixtures independent of assertions and the same runtime verifier. | Each pristine fixture validates against reader schema before evaluation; changing assertion cannot change fixture. |
| RC26-03 | TEST_DESIGN_DEFECT | Required components and formula components are derived from the assertion list. | all 34 | Independent literal frozen oracle with version/hash and explicit component/subcase inventory. | Oracle IDs equal assertion IDs; oracle is not constructed from assertions; pinned canonical hash exact. |
| RC26-04 | TEST_DESIGN_DEFECT | Named mutations often change descriptors or append assertions and are counted as killed structurally. | T03/all | Mutate actual typed evidence bundles and require the real verifier to return exact failed component IDs. | 11 evidence classes per applicable selector; killed count equals oracle-required mutant count; survivors 0. |
| RC26-05 | SOURCE_DEFECT | Multi-subcase labels share one preparation/attempt/evidence binding. | 19 multi-subcase scenarios | 108 explicit subcase bindings while retaining 34 top-level IDs. | Unique nonzero preparation, attempt and evidence IDs per subcase; no cross-subcase reuse. |
| RC26-06 | SOURCE_DEFECT | Command “business/history rows” are claims, not independent actual rows; export lacks eligible source projection. | C01-C08, E01-E04 | Static allowlisted actual-relation projections and independently enumerated export eligibility. | Claim-to-actual bijection, ordered row hashes and before/after cardinalities. |
| RC26-07 | SOURCE_DEFECT | ACL readers omit membership/effective privilege closure and global terms combine two databases. | P01/P03/A01/A02/T01 | Separate CP and target ACL components, membership edges, direct/inherited/effective/PUBLIC/owner/admin facts. | Both set differences zero per database and dimension; no global label result. |
| RC26-08 | SOURCE_DEFECT / PRIVACY | Purge `contextRows` is target-wide and not execution/lease/auth/batch scoped. | G01-G06 | Exact instance+lease+authorization+root+batch+attempt+subcase reader; remove unscoped projection. | Unrelated organization/lease rows never returned; local before/after/durable and retry chain exact. |
| RC26-09 | DUPLICATE_OR_DERIVED_FAILURE | 34 scenario failures are symptoms of RC26-01 through RC26-08, not separate fixes. | 34/34 | One schema/oracle/evaluator correction, with scenario-specific components. | Fresh precheck must adjudicate every row independently. |
| RC26-10 | EXTERNAL_PROVISIONING_PREREQUISITE | Actual database behavior remains unavailable and unauthorized. | later execution only | Preserve pending external gate; do not simulate database acceptance. | Later separately authorized isolated PostgreSQL evidence. |

F23-01 is excluded from Correction 26 source edits. Its exact accepted slice and tests remain a mandatory no-change hash gate.

## 3. Complete 133-term selector coverage matrix

Future reader contracts:

- **CP-L2**: `nexa.rev869b_read_lifecycle_evidence_v2(instance_sha256,lease_id,subcase_id,attempt_id,request_id,decision_id,expected_version)` over exact control lease/event/attempt/outcome/decision/quarantine/manifest rows.
- **CP-A2**: `nexa.rev869b_read_control_plane_acl_evidence_v2(oracle_version,observation_stage)` over control manifest and ownership/direct/default/membership/effective/PUBLIC/admin facts.
- **TC2**: `nexa.rev869b_read_command_evidence_v2(instance_sha256,lease_id,command_id,attempt_id,subcase_id)` over command ledgers and static actual business/history projections.
- **TP2**: `nexa.rev869b_read_purge_evidence_v2(instance_sha256,lease_id,authorization_id,root_id,batch_id,attempt_id,subcase_id)` with no target-wide row set.
- **TE2**: `nexa.rev869b_read_export_evidence_v2(instance_sha256,lease_id,authorization_id,batch_id,release_id,subcase_id,as_of)` including eligible source rows and minimized batch rows.
- **TA2**: `nexa.rev869b_read_target_acl_evidence_v2(instance_sha256,lease_id,principal,object,operation,subcase_id,stage)` over target catalogs and scoped protected/audit facts.
- **OR2**: offline mutation-result reader keyed by oracle hash/version, scenario, subcase and mutation ID; it reads actual verifier results, not expected assertions.

Every non-rejected row maps to exactly one typed output. A reducer consumes only the exact scoped output. Rejected terms are removed from formula version C26 and replaced by independently observable components.

| Component ID | Current selector | Type | Future output | Source relation/view/function | Filtering scope | Cardinality / predicate | Disposition |
|---|---|---|---|---|---|---|---|
| `P01:formula-pin-mismatch` | `pinMismatchCount` | int64 | `CP-A2.pinMismatchCount` | control manifest + pg_catalog ACL/member/effective facts | cluster+database+oracleFactSet+subcase+stage | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `P01:formula-target-acl-delta` | `targetAclDeltaCount` | int64 | `TA2.targetAclDeltaCount` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `P01:formula-verify` | `verifyResult` | string/enum | `—` | not independently observable as declared | replacement components have exact separate scopes | EqualsLiteral Exact; one scalar/exact subcase | REJECT; split to CP/target mismatch counts |
| `P02:formula-pin-mismatch` | `pinMismatchCount` | int64 | `CP-A2.pinMismatchCount` | control manifest + pg_catalog ACL/member/effective facts | cluster+database+oracleFactSet+subcase+stage | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `P02:formula-lease-zero` | `allocatedLeaseCount` | int64 | `CP-L2.allocatedLeaseCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `P02:formula-action-zero` | `actionCount` | int64 | `—` | not independently observable as declared | replacement components have exact separate scopes | Zero; one scalar/exact subcase | REJECT; replace with lease/event/allocation deltas |
| `P03:formula-seeded-one` | `seededDeltaCount` | int64 | `CP-A2.seededDeltaCount` | control manifest + pg_catalog ACL/member/effective facts | cluster+database+oracleFactSet+subcase+stage | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `P03:formula-reported-delta` | `reportedDeltaSha256` | sha256 | `CP-A2.reportedDeltaSha256` | control manifest + pg_catalog ACL/member/effective facts | cluster+database+oracleFactSet+subcase+stage | EqualsObservationPath Before:seededDeltaSha256; one scalar/exact subcase | ADD typed output and reducer |
| `P03:formula-protected-zero` | `protectedMutationCount` | int64 | `CP-A2.protectedMutationCount` | control manifest + pg_catalog ACL/member/effective facts | cluster+database+oracleFactSet+subcase+stage | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `P03:formula-cleanup-baseline` | `cleanupFingerprint` | sha256 | `CP-A2.cleanupFingerprint` | control manifest + pg_catalog ACL/member/effective facts | cluster+database+oracleFactSet+subcase+stage | EqualsObservationPath Before:baselineFingerprint; one scalar/exact subcase | ADD typed output and reducer |
| `L01:formula-reserved` | `reservedEventCount` | int64 | `CP-L2.reservedEventCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `L01:formula-branch-xor` | `resumeSameAttempt\|authorizedCleanup` | bool tuple | `CP-L2.resumeSameAttempt_xor_authorizedCleanup` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | ExactlyOneTrue Before:resumeSameAttempt\|Before:authorizedCleanup; one scalar/exact subcase | ADD typed output and reducer |
| `L01:formula-duplicates-zero` | `duplicateAttemptCount` | int64 | `CP-L2.duplicateAttemptCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `L02:formula-boundary-count` | `boundaryCount` | int64 | `CP-L2.boundaryCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | GreaterThanZero; one scalar/exact subcase | ADD typed output and reducer |
| `L02:formula-started-each` | `startedAttemptsPerBoundary` | int64 | `CP-L2.startedAttemptsPerBoundary` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `L02:formula-reconciled-each` | `reconciledAttemptsPerBoundary` | int64 | `CP-L2.reconciledAttemptsPerBoundary` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `L02:formula-target-each` | `targetCountPerBoundary` | int64 | `TA2.targetCountPerBoundary` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `L02:formula-roles-each` | `roleSetCountPerBoundary` | int64 | `TA2.roleSetCountPerBoundary` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `L03:formula-requests` | `cleanupRequestCount` | int64 | `CP-L2.cleanupRequestCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 2; one scalar/exact subcase | ADD typed output and reducer |
| `L03:formula-dropstarted` | `dropStartedEventCount` | int64 | `CP-L2.dropStartedEventCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `L03:formula-active` | `activeDropAttemptCount` | int64 | `CP-L2.activeDropAttemptCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `L03:formula-physical` | `physicalDropExecutionCount` | int64 | `—` | not independently observable as declared | replacement components have exact separate scopes | EqualsLiteral 1; one scalar/exact subcase | REJECT; external process count supplementary; use exact DB event chain |
| `L03:formula-authorization-chain` | `authorizationRegistrationTransitionCount` | int64 | `CP-L2.authorizationRegistrationTransitionCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `L04:formula-dropstarted` | `dropStartedEventsPerBoundary` | int64 | `CP-L2.dropStartedEventsPerBoundary` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `L04:formula-finalized` | `finalizedEventsPerBoundary` | int64 | `CP-L2.finalizedEventsPerBoundary` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `L04:formula-physical` | `physicalDropExecutionMax` | int64 | `—` | not independently observable as declared | replacement components have exact separate scopes | AtMostOne; one scalar/exact subcase | REJECT; external process count supplementary; use terminal uniqueness |
| `L04:formula-target-zero` | `targetCount` | int64 | `TA2.targetCount` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `L04:formula-roles-zero` | `roleCount` | int64 | `TA2.roleCount` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `L05:formula-use-zero` | `useMutationCount` | int64 | `TA2.useMutationCount` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `L05:formula-drop-zero` | `dropMutationCount` | int64 | `TA2.dropMutationCount` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `L05:formula-quarantine-one` | `quarantineOutcomeCount` | int64 | `CP-L2.quarantineOutcomeCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `R01:formula-decision-one` | `decisionCount` | int64 | `CP-L2.decisionCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | RETAIN raw output; namespace/type and bind reducer |
| `R01:formula-consumed-attempt` | `consumedAttemptId` | uuid | `CP-L2.consumedAttemptId` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsObservationPath Before:attemptId; one scalar/exact subcase | ADD typed output and reducer |
| `R01:formula-action` | `authorizedAction` | string/enum | `CP-L2.authorizedAction` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsObservationPath Before:performedAction; one scalar/exact subcase | ADD typed output and reducer |
| `R01:formula-recovery-one` | `recoveryAttemptCount` | int64 | `CP-L2.recoveryAttemptCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `R01:formula-finalized-one` | `finalizedEventCount` | int64 | `CP-L2.finalizedEventCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `R02:formula-attempts-zero` | `newAttemptCount` | int64 | `CP-L2.newAttemptCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `R02:formula-events-zero` | `newEventCount` | int64 | `CP-L2.newEventCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `R02:formula-consumed-one` | `decisionConsumedCount` | int64 | `CP-L2.decisionConsumedCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `R03:formula-failure-one` | `cleanupFailureCount` | int64 | `CP-L2.cleanupFailureCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `R03:formula-old-zero` | `oldDecisionAcceptedCount` | int64 | `CP-L2.oldDecisionAcceptedCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `R03:formula-fresh-one` | `freshLinkedDecisionCount` | int64 | `CP-L2.freshLinkedDecisionCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `R03:formula-consumed-one` | `freshDecisionConsumedCount` | int64 | `CP-L2.freshDecisionConsumedCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `R03:formula-finalized-one` | `finalizedEventCount` | int64 | `CP-L2.finalizedEventCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `C01:formula-business-delta` | `businessRowDelta` | int64 | `TC2.businessRowDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsObservationPath Before:expectedBusinessRowDelta; one scalar/exact subcase | ADD typed output and reducer |
| `C01:formula-history-delta` | `historyRowDelta` | int64 | `TC2.historyRowDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsObservationPath Before:expectedHistoryRowDelta; one scalar/exact subcase | ADD typed output and reducer |
| `C01:formula-receipt-one` | `receiptCount` | int64 | `TC2.receiptCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsLiteral 1; one scalar/exact subcase | RETAIN raw output; namespace/type and bind reducer |
| `C01:formula-outcome-one` | `committedOutcomeCount` | int64 | `TC2.committedOutcomeCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `C01:formula-active-zero` | `activeAttemptCount` | int64 | `TC2.activeAttemptCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | RETAIN raw output; namespace/type and bind reducer |
| `C02:formula-business-same` | `businessAfter2Sha256` | sha256 | `TC2.businessAfter2Sha256` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsObservationPath Before:businessAfter1Sha256; one scalar/exact subcase | ADD typed output and reducer |
| `C02:formula-history-same` | `historyAfter2Sha256` | sha256 | `TC2.historyAfter2Sha256` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsObservationPath Before:historyAfter1Sha256; one scalar/exact subcase | ADD typed output and reducer |
| `C02:formula-receipt-same` | `receiptId2` | uuid | `TC2.receiptId2` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsObservationPath Before:receiptId1; one scalar/exact subcase | ADD typed output and reducer |
| `C02:formula-response-same` | `responseSha2562` | sha256 | `TC2.responseSha2562` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsObservationPath Before:responseSha2561; one scalar/exact subcase | ADD typed output and reducer |
| `C02:formula-receipt-one` | `receiptCount` | int64 | `TC2.receiptCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsLiteral 1; one scalar/exact subcase | RETAIN raw output; namespace/type and bind reducer |
| `C03:formula-digest-different` | `changedDigest` | sha256 | `TC2.changedDigest` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | NotEqualsObservationPath Before:registeredDigest; one scalar/exact subcase | ADD typed output and reducer |
| `C03:formula-request-zero` | `requestDelta` | int64 | `TC2.requestDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `C03:formula-attempt-zero` | `attemptDelta` | int64 | `TC2.attemptDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `C03:formula-business-zero` | `businessHistoryDelta` | int64 | `TC2.businessHistoryDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `C04:formula-business-zero` | `businessRowDelta` | int64 | `TC2.businessRowDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `C04:formula-history-zero` | `historyRowDelta` | int64 | `TC2.historyRowDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `C04:formula-receipt-zero` | `receiptDelta` | int64 | `TC2.receiptDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `C04:formula-rollback-one` | `rolledBackOutcomeCount` | int64 | `TC2.rolledBackOutcomeCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `C05:formula-business-zero` | `businessHistoryReceiptDelta` | int64 | `TC2.businessHistoryReceiptDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `C05:formula-rollback-one` | `rolledBackOutcomeCount` | int64 | `TC2.rolledBackOutcomeCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `C05:formula-opened-attempt` | `openedAttemptId` | uuid | `TC2.openedAttemptId` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsObservationPath Before:attemptId; one scalar/exact subcase | ADD typed output and reducer |
| `C06:formula-subcases-four` | `interruptionSubcaseCount` | int64 | `TC2.interruptionSubcaseCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsLiteral 4; one scalar/exact subcase | ADD typed output and reducer |
| `C06:formula-distinct-evidence` | `distinctEvidenceIdCount` | int64 | `TC2.distinctEvidenceIdCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsLiteral 4; one scalar/exact subcase | ADD typed output and reducer |
| `C06:formula-terminal-each` | `terminalOutcomeCountPerAttempt` | int64 | `TC2.terminalOutcomeCountPerAttempt` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `C07:formula-requests-two` | `startRequestCount` | int64 | `TC2.startRequestCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsLiteral 2; one scalar/exact subcase | ADD typed output and reducer |
| `C07:formula-started-one` | `startedAttemptCount` | int64 | `TC2.startedAttemptCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `C07:formula-active-one` | `activeAttemptCount` | int64 | `TC2.activeAttemptCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | EqualsLiteral 1; one scalar/exact subcase | RETAIN raw output; namespace/type and bind reducer |
| `C07:formula-unrelated-zero` | `unrelatedMutationCount` | int64 | `TC2.unrelatedMutationCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `C08:formula-accepted-zero` | `acceptedSubstitutionCount` | int64 | `TC2.acceptedSubstitutionCount` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `C08:formula-contexts-zero` | `contextDelta` | int64 | `TC2.contextDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `C08:formula-receipts-zero` | `receiptDelta` | int64 | `TC2.receiptDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `C08:formula-business-zero` | `businessHistoryDelta` | int64 | `TC2.businessHistoryDelta` | command ledgers + static actual business/history union | instance+lease+command+attempt+subcase+entityIds | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `G01:formula-attempts-zero` | `startedAttemptCount` | int64 | `TP2.startedAttemptCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `G01:formula-candidates-zero` | `candidateCount` | int64 | `TP2.candidateCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | Zero; one scalar/exact subcase | RETAIN raw output; namespace/type and bind reducer |
| `G01:formula-events-zero` | `purgeEventCount` | int64 | `TP2.purgeEventCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `G02:formula-eligible-zero` | `eligibleBeforeCount` | int64 | `TP2.eligibleBeforeCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `G02:formula-frozen-zero` | `frozenCandidateCount` | int64 | `TP2.frozenCandidateCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `G02:formula-deleted-zero` | `deletedRowCount` | int64 | `TP2.deletedRowCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `G02:formula-event-one` | `zeroRowsEventCount` | int64 | `TP2.zeroRowsEventCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `G03:formula-eligible-positive` | `eligibleBeforeCount` | int64 | `TP2.eligibleBeforeCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | GreaterThanZero; one scalar/exact subcase | ADD typed output and reducer |
| `G03:formula-frozen-equals` | `frozenCandidateCount` | uuid | `TP2.frozenCandidateCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | EqualsObservationPath Before:eligibleBeforeCount; one scalar/exact subcase | ADD typed output and reducer |
| `G03:formula-deleted-equals` | `deletedRowCount` | int64 | `TP2.deletedRowCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | EqualsObservationPath Before:eligibleBeforeCount; one scalar/exact subcase | ADD typed output and reducer |
| `G03:formula-remaining-zero` | `remainingEligibleCount` | int64 | `TP2.remainingEligibleCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `G03:formula-event-one` | `succeededEventCount` | int64 | `TP2.succeededEventCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `G04:formula-hash-different` | `currentCandidateSha256` | sha256 | `TP2.currentCandidateSha256` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | NotEqualsObservationPath Before:frozenCandidateSha256; one scalar/exact subcase | ADD typed output and reducer |
| `G04:formula-deleted-zero` | `deletedRowCount` | int64 | `TP2.deletedRowCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `G04:formula-context-same` | `contextAfterSha256` | sha256 | `TP2.contextAfterSha256` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | EqualsObservationPath Before:contextBeforeSha256; one scalar/exact subcase | ADD typed output and reducer |
| `G04:formula-event-one` | `failedEventCount` | int64 | `TP2.failedEventCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `G05:formula-deleted-zero` | `deletedRowCount` | int64 | `TP2.deletedRowCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `G05:formula-context-same` | `contextAfterSha256` | sha256 | `TP2.contextAfterSha256` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | EqualsObservationPath Before:contextBeforeSha256; one scalar/exact subcase | ADD typed output and reducer |
| `G05:formula-event-one` | `failedEventCount` | int64 | `TP2.failedEventCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `G06:formula-starts-two` | `concurrentStartCount` | int64 | `TP2.concurrentStartCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | EqualsLiteral 2; one scalar/exact subcase | ADD typed output and reducer |
| `G06:formula-consumed-one` | `consumedAuthorizationCount` | int64 | `TP2.consumedAuthorizationCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `G06:formula-execution-max` | `executionCount` | int64 | `TP2.executionCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | AtMostOne; one scalar/exact subcase | ADD typed output and reducer |
| `G06:formula-child-one` | `activeChildCount` | int64 | `TP2.activeChildCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | EqualsLiteral 1; one scalar/exact subcase | RETAIN raw output; namespace/type and bind reducer |
| `G06:formula-substituted-zero` | `substitutedChildCount` | int64 | `TP2.substitutedChildCount` | purge root/auth/attempt/candidate/event + scoped contexts | instance+lease+auth+root+batch+attempt+subcase | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `E01:formula-within-max` | `preparedRowCountWithinMaximum` | bool | `TE2.preparedRowCountWithinMaximum` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | EqualsLiteral true; one scalar/exact subcase | ADD typed output and reducer |
| `E01:formula-hash` | `preparedSha256` | sha256 | `TE2.preparedSha256` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | EqualsObservationPath Before:recomputedPreparedSha256; one scalar/exact subcase | ADD typed output and reducer |
| `E01:formula-excluded-zero` | `excludedFieldCount` | int64 | `TE2.excludedFieldCount` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `E01:formula-event-one` | `preparedEventCount` | int64 | `TE2.preparedEventCount` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `E02:formula-rows-same` | `preparedAfterSha256` | sha256 | `TE2.preparedAfterSha256` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | EqualsObservationPath Before:preparedBeforeSha256; one scalar/exact subcase | ADD typed output and reducer |
| `E02:formula-count-same` | `preparedAfterCount` | int64 | `TE2.preparedAfterCount` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | EqualsObservationPath Before:preparedBeforeCount; one scalar/exact subcase | ADD typed output and reducer |
| `E02:formula-later-one` | `laterEligibleRowCount` | int64 | `TE2.laterEligibleRowCount` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `E02:formula-later-batch-zero` | `laterRowInBatchCount` | int64 | `TE2.laterRowInBatchCount` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `E03:formula-released-zero` | `releasedRowCount` | int64 | `TE2.releasedRowCount` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `E03:formula-events-zero` | `newReleaseEventCount` | int64 | `TE2.newReleaseEventCount` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `E03:formula-batch-same` | `preparedAfterSha256` | sha256 | `TE2.preparedAfterSha256` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | EqualsObservationPath Before:preparedBeforeSha256; one scalar/exact subcase | ADD typed output and reducer |
| `E04:formula-release-distinct` | `releaseId2` | uuid | `TE2.releaseId2` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | NotEqualsObservationPath Before:releaseId1; one scalar/exact subcase | ADD typed output and reducer |
| `E04:formula-prior-link` | `priorReleaseId2` | uuid | `TE2.priorReleaseId2` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | EqualsObservationPath Before:releaseId1; one scalar/exact subcase | ADD typed output and reducer |
| `E04:formula-active-one` | `activeReleaseCount` | int64 | `TE2.activeReleaseCount` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | EqualsLiteral 1; one scalar/exact subcase | RETAIN raw output; namespace/type and bind reducer |
| `E04:formula-success-max` | `deliverySuccessCount` | int64 | `TE2.deliverySuccessCount` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | AtMostOne; one scalar/exact subcase | ADD typed output and reducer |
| `E04:formula-batch-same` | `batchAfterSha256` | sha256 | `TE2.batchAfterSha256` | export auth/batch/rows/releases + eligible sources | instance+lease+auth+batch+release+subcase+asOf | EqualsObservationPath Before:batchBeforeSha256; one scalar/exact subcase | ADD typed output and reducer |
| `A01:formula-unexpected-zero` | `observedMinusExpectedCount` | int64 | `—` | split into CP-A2 and TA2 components | replacement components have exact separate scopes | Zero; one scalar/exact subcase | REJECT/SPLIT global ACL universe into CP-A2 and TA2 |
| `A01:formula-missing-zero` | `expectedMinusObservedCount` | int64 | `—` | split into CP-A2 and TA2 components | replacement components have exact separate scopes | Zero; one scalar/exact subcase | REJECT/SPLIT global ACL universe into CP-A2 and TA2 |
| `A01:formula-dimensions` | `aclDimensionCount` | int64 | `—` | split into CP-A2 and TA2 components | replacement components have exact separate scopes | GreaterThanZero; one scalar/exact subcase | REJECT/SPLIT global ACL universe into CP-A2 and TA2 |
| `A02:formula-allowed-zero` | `allowedProtectedOperationCount` | int64 | `TA2.allowedProtectedOperationCount` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | Zero; one scalar/exact subcase | ADD typed output and reducer |
| `A02:formula-tuple-count` | `durableDenialCount` | int64 | `TA2.durableDenialCount` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | EqualsObservationPath Before:requiredDenialTupleCount; one scalar/exact subcase | ADD typed output and reducer |
| `A02:formula-fingerprint-same` | `protectedAfterSha256` | sha256 | `TA2.protectedAfterSha256` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | EqualsObservationPath Before:protectedBeforeSha256; one scalar/exact subcase | ADD typed output and reducer |
| `T01:formula-lease-one` | `leaseCount` | int64 | `CP-L2.leaseCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | RETAIN raw output; namespace/type and bind reducer |
| `T01:formula-target-one` | `targetCount` | int64 | `TA2.targetCount` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `T01:formula-admin-zero` | `adminCredentialCountInTest` | int64 | `—` | not independently observable as declared | replacement components have exact separate scopes | Zero; one scalar/exact subcase | REJECT; source invariant plus TA2 session-principal fact |
| `T01:formula-fixture` | `fixturePrepared` | bool | `TA2.fixturePrepared` | target identity + pg_catalog + scoped protected/audit rows | instance+lease+principal+object+operation+subcase+stage | EqualsLiteral true; one scalar/exact subcase | ADD typed output and reducer |
| `T02:formula-instance-different` | `restartedControllerInstanceId` | uuid | `—` | not independently observable as declared | replacement components have exact separate scopes | NotEqualsObservationPath Before:originalControllerInstanceId; one scalar/exact subcase | REJECT; process identity supplementary; use surviving DB attempt/event |
| `T02:formula-attempt-same` | `reconciledAttemptId` | uuid | `CP-L2.reconciledAttemptId` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsObservationPath Before:survivingAttemptId; one scalar/exact subcase | ADD typed output and reducer |
| `T02:formula-dropstarted-one` | `dropStartedEventCount` | int64 | `CP-L2.dropStartedEventCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `T02:formula-finalized-one` | `finalizedEventCount` | int64 | `CP-L2.finalizedEventCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `T02:formula-cleanup-one` | `cleanupEvidenceCount` | int64 | `CP-L2.cleanupEvidenceCount` | control leases/events/attempts/outcomes/decisions/quarantine/manifest | instance+lease+subcase+request+attempt+decision+version | EqualsLiteral 1; one scalar/exact subcase | ADD typed output and reducer |
| `T03:formula-killed-equals` | `killedMutants` | int64 | `OR2.killedMutants` | frozen-oracle mutation result stream | oracleHash+version+scenario+subcase+mutation | EqualsObservationPath Before:requiredNonEquivalentMutants; one scalar/exact subcase | ADD typed output and reducer |
| `T03:formula-survivors-zero` | `survivingMutants` | int64 | `OR2.survivingMutants` | frozen-oracle mutation result stream | oracleHash+version+scenario+subcase+mutation | Zero; one scalar/exact subcase | ADD typed output and reducer |

## Selector reconciliation

The precheck's selector algorithm takes the first root token of each assertion path. On that exact algorithm, the 105 distinct missing selectors are:

1. acceptedSubstitutionCount
2. aclDimensionCount
3. actionCount
4. activeDropAttemptCount
5. adminCredentialCountInTest
6. allocatedLeaseCount
7. allowedProtectedOperationCount
8. attemptDelta
9. authorizationRegistrationTransitionCount
10. authorizedAction
11. batchAfterSha256
12. boundaryCount
13. businessAfter2Sha256
14. businessHistoryDelta
15. businessHistoryReceiptDelta
16. businessRowDelta
17. changedDigest
18. cleanupEvidenceCount
19. cleanupFailureCount
20. cleanupFingerprint
21. cleanupRequestCount
22. committedOutcomeCount
23. concurrentStartCount
24. consumedAttemptId
25. consumedAuthorizationCount
26. contextAfterSha256
27. contextDelta
28. currentCandidateSha256
29. decisionConsumedCount
30. deletedRowCount
31. deliverySuccessCount
32. distinctEvidenceIdCount
33. dropMutationCount
34. dropStartedEventCount
35. dropStartedEventsPerBoundary
36. duplicateAttemptCount
37. durableDenialCount
38. eligibleBeforeCount
39. excludedFieldCount
40. executionCount
41. expectedMinusObservedCount
42. failedEventCount
43. finalizedEventCount
44. finalizedEventsPerBoundary
45. fixturePrepared
46. freshDecisionConsumedCount
47. freshLinkedDecisionCount
48. frozenCandidateCount
49. historyAfter2Sha256
50. historyRowDelta
51. interruptionSubcaseCount
52. killedMutants
53. laterEligibleRowCount
54. laterRowInBatchCount
55. newAttemptCount
56. newEventCount
57. newReleaseEventCount
58. observedMinusExpectedCount
59. oldDecisionAcceptedCount
60. openedAttemptId
61. physicalDropExecutionCount
62. physicalDropExecutionMax
63. pinMismatchCount
64. preparedAfterCount
65. preparedAfterSha256
66. preparedEventCount
67. preparedRowCountWithinMaximum
68. preparedSha256
69. priorReleaseId2
70. protectedAfterSha256
71. protectedMutationCount
72. purgeEventCount
73. quarantineOutcomeCount
74. receiptDelta
75. receiptId2
76. reconciledAttemptId
77. reconciledAttemptsPerBoundary
78. recoveryAttemptCount
79. releasedRowCount
80. releaseId2
81. remainingEligibleCount
82. reportedDeltaSha256
83. requestDelta
84. reservedEventCount
85. responseSha2562
86. restartedControllerInstanceId
87. resumeSameAttempt
88. roleCount
89. roleSetCountPerBoundary
90. rolledBackOutcomeCount
91. seededDeltaCount
92. startedAttemptCount
93. startedAttemptsPerBoundary
94. startRequestCount
95. substitutedChildCount
96. succeededEventCount
97. survivingMutants
98. targetAclDeltaCount
99. targetCount
100. targetCountPerBoundary
101. terminalOutcomeCountPerAttempt
102. unrelatedMutationCount
103. useMutationCount
104. verifyResult
105. zeroRowsEventCount

L01:formula-resume-or-cleanup is a compound path. The precheck counted only resumeSameAttempt; its second atomic root, authorizedCleanup, is also missing. Thus this reconciles the exact reported 105 and records 106 missing atomic outputs without altering the authoritative 133 formula-term inventory.

Eighteen selector names are reused. Seventeen may remain only as typed, reader-namespaced outputs: pinMismatchCount, dropStartedEventCount, targetCount, finalizedEventCount, businessRowDelta, historyRowDelta, receiptCount, activeAttemptCount, businessHistoryDelta, receiptDelta, rolledBackOutcomeCount, eligibleBeforeCount, frozenCandidateCount, deletedRowCount, contextAfterSha256, failedEventCount, and preparedAfterSha256. startedAttemptCount crosses command and purge domains and must split into CP-L2.commandStartedAttemptCount and TP2.purgeStartedAttemptCount. No unqualified selector may carry different meanings.

The six non-observable or self-referential terms rejected by the matrix are verifyResult, actionCount, physicalDropExecutionCount, physicalDropExecutionMax, adminCredentialCountInTest, and restartedControllerInstanceId. A01 global ACL terms split into independently scoped control-plane and target components. These are formula-versioned replacements, not silent weakening.
## Independent frozen oracle design

The future oracle is a new committed literal contract, REV869B-C26-ORACLE-v1. It is authored independently of FormulaAssertions, scenario runtime evidence, and controller-audit generation. It contains exactly 34 immutable top-level scenario IDs and 108 immutable subcase rows. Every row fixes scenario ID, subcase ID, formula version, fixture identity class, action, reader/version, typed selector IDs, reducer, stage, expected SQLSTATE/code/object/outcome, expected cardinality, and permitted supplementary evidence. Its canonical UTF-8 SHA-256 and version are pinned independently in a source-contract test and the Correction 26 checkpoint/management gate.

Assertions are generated or checked from the oracle, never the reverse. Runtime evidence carries oracle hash/version plus scenario/subcase identity. Comparison rejects unknown, missing, extra, duplicate, stale-version, or hash-mismatched rows. Removing or weakening one decisive assertion leaves an oracle component without a bijective executable binding and fails the source-contract test before behavior is considered. Generic Exact, Verified, PASS, labels, controller signatures, and formula prose cannot satisfy an oracle component.

The rejected terms and A01 splits above require an explicit formula-version increment in the corresponding oracle rows. Each replacement records old term, new typed component(s), and strictly stronger database-observable predicate. No formula may change without changing the oracle version and pinned hash.

## Executable evidence-tampering design

All evidence mutations pass through the same production verifier/reducer used for unmodified evidence. The required mutation set is:

| Mutation | Actual evidence alteration | Mandatory rejection |
|---|---|---|
| missing | Remove one decisive typed output or durable row | Missing component ID/cardinality |
| extra | Add an unrecognized selector or row | Exact-set/cardinality mismatch |
| duplicated | Duplicate evidence ID or unique relation row | Uniqueness/cardinality mismatch |
| stale | Replace asOf/version/attempt with predecessor | Freshness/version mismatch |
| substituted | Replace fixture, request, authorization, batch, object, or evidence identity | Join/bijection mismatch |
| cross-instance | Replace target instance | Instance scope mismatch |
| cross-lease | Replace lease | Immutable lease binding mismatch |
| wrong-version | Change lifecycle/formula/oracle version | Version/hash mismatch |
| wrong-count | Change a scalar or add/remove an observed row | Reducer predicate mismatch |
| wrong-state | Change before/after/terminal state | Transition predicate mismatch |
| fabricated | Supply a controller-only or unsigned/unbacked value | No authoritative reader provenance |

For every oracle component there are two mandatory source-only mutations: assertion removal and predicate weakening. Each must fail oracle-to-assertion bijection. For representative runtime structures in every reader family, the eleven evidence alterations above must invoke the real verifier and produce the exact rejected component IDs. Descriptor-text mutation remains structural coverage only and cannot count as behavioral acceptance. T03 counts mutants from this actual verifier result stream, not constants.

## Multi-subcase reconciliation

The 34 frozen top-level IDs expand to 108 explicit subcase bindings. Each subcase receives distinct preparation_request_id, attempt_id, evidence_id, action/result, and oracle row. A top-level scenario may aggregate subcases only after every subcase passes independently.

| Scenario | Explicit subcases | Required binding |
|---|---:|---|
| P02 | 5 | missing, substituted, stale, replayed, cross-instance pin |
| P03 | 4 | active, stale, cross-instance, wrong-version target identity |
| L01 | 3 | same-attempt resume, authorized cleanup, unauthorized cleanup |
| L02 | 6 | missing, stale, substituted, cross-instance, cross-lease, wrong-version decision |
| L03 | 5 | interruption boundaries with one start/terminal per boundary |
| L04 | 5 | lease overlap/expiry/restart boundaries |
| L05 | 5 | authorization replay/substitution boundaries |
| R02 | 8 | quarantine evidence identity/count/state tamper cases |
| R03 | 5 | terminal retry boundaries |
| C04 | 5 | rollback interruption boundaries |
| C06 | 4 | recovery authorization/attempt boundaries |
| C08 | 8 | context/evidence substitution boundaries |
| G01 | 5 | purge start/retry-root boundaries |
| G05 | 3 | purge failure/retry boundaries |
| G06 | 4 | purge authorization/batch isolation boundaries |
| E03 | 4 | export preparation interruption boundaries |
| E04 | 3 | export release/delivery boundaries |
| A02 | 7 | direct, inherited, aggregate, ownership, PUBLIC, runtime, admin |
| T03 | 4 | assertion removal, predicate weakening, evidence tamper, oracle mismatch |
| Remaining 15 IDs | 15 | one explicit subcase each |
| Total | 108 | no shared preparation, attempt, evidence, or expected-result identity |

## ACL evidence design

CP-A2 projects control-plane owners, schema/table/sequence/function privileges, default privileges, role inheritance, effective privileges, and PUBLIC grants. TA2 projects the corresponding target surface plus protected operations. Direct grants come from catalog ACL expansion; inherited and aggregate-role membership comes from pg_auth_members closure; effective schema/table/sequence/function privileges are calculated with has_*_privilege; ownership is explicit; PUBLIC is a first-class principal; administrative bypass is evaluated separately. Expected and observed sets are scoped independently for control-plane and target objects. The verifier/export roles receive only fixed, row-scoped reader EXECUTE privileges and no unrestricted ledger or business/export payload access.

A01 compares exact expected-minus-observed and observed-minus-expected sets for CP-A2 and TA2 separately. A02 supplies a unique subcase for each bypass class and proves protected state unchanged plus one durable denial tuple. Session principal and membership provenance are authoritative database facts; controller claims are supplementary.

## Purge evidence design

TP2 replaces target-wide context projections with rows filtered by execution_id, target_instance_id, immutable lease identity, authorization_id, batch_id, attempt_id, retry_root_attempt_id, subcase_id, and asOf. Every purge subcase captures local before and after candidate/count/fingerprint state, terminal outcome, durable purge event/history, and exact retry-root linkage. A retry must reference an earlier terminal failed/rolled-back attempt under the same execution, instance, lease, authorization, and batch; attempt sequence must increase by one; substituted or unscoped roots fail closed. The target identity relation must carry the immutable lease binding (or an equivalently enforced authoritative relation). Target-wide aggregate rows cannot decide PASS.

## Exact 34-scenario reconciliation matrix

All fixture and action wording below is taken from the committed Setup and Action functions. Formula means the committed intent, re-expressed in the independent versioned oracle with the replacements identified in the 133-term matrix. DB observations are decisive; controller audit is supplementary.

| ID | Subcases | Independent fixture | Action | Reader(s) | Objective formula / expected result | Executable and mutation acceptance | Required Correction 26 files |
|---|---:|---|---|---|---|---|---|
| P01 | 1 | Externally provisioned exact cluster and control plane | Run canonical read-only verifier | CP-A2 + TA2 | PinMismatchCount=0 AND ControlFingerprint=Expected AND TargetAclDelta=empty AND VerifyResult=Exact; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; ProvisioningContract; DesignTests; Scenarios |
| P02 | 5 | Pinned cluster with one independently mutated provenance pin | Run external preflight | CP-A2 + CP-L2 | PinMismatchCount=1 AND AllocatedLeaseCount=0 AND ActionCount=0 AND ProblemCode/Object=mutated-pin; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; ProvisioningContract; DesignTests; Scenarios |
| P03 | 4 | Control plane with one seeded definition or effective-grant delta | Run canonical verifier against seeded delta | CP-A2 | SeededDeltaCount=1 AND ReportedDelta=SeededDelta AND ProtectedMutationCount=0 AND CleanupFingerprint=Baseline; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; ProvisioningContract; DesignTests; Scenarios |
| L01 | 3 | Reserved lease interrupted after reservation before role creation | Resume same attempt or execute separately approved cleanup | CP-L2 | ReservedEvents=1 AND (ResumeSameAttempt XOR AuthorizedCleanup) AND DuplicateAttempts=0; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| L02 | 6 | Reserved lease with a deterministic interruption at every create phase | Restart controller reconciliation at every boundary | CP-L2 + TA2 | for each boundary: StartedAttempts=1 AND ReconciledAttempts=1 AND LeaseState=Ready AND TargetCount=1 AND RoleSetCount=1; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| L03 | 5 | Ready and InUse leases with exact DropAuthorized events and two cleanup requests at a barrier | Race normal cleanup using exact authorization registration | CP-L2 | CleanupRequests=2 AND DropStartedEvents=1 AND ActiveDropAttempts=1 AND PhysicalDropExecutions=1 AND exact authorization-registration-transition chain; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| L04 | 5 | DropStarted leases interrupted before during and after DROP and role cleanup | Restart and reconcile each cleanup boundary | CP-L2 + TA2 | per boundary: DropStartedEvents=1 AND FinalizedEvents=1 AND PhysicalDropExecutions<=1 AND TargetCount=0 AND RoleCount=0; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| L05 | 5 | Ready target with independently observed marker or catalogue mismatch | Deny use/drop and quarantine exact mismatch | CP-L2 + TA2 | UseMutations=0 AND DropMutations=0 AND exact mismatch error AND QuarantineOutcomeCount=1 AND LeaseState=Quarantined; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| R01 | 1 | Quarantined lease and exact valid unconsumed management decision | Consume exact action and recover | CP-L2 | DecisionCount=1 AND ConsumedAttemptId=AttemptId AND AuthorizedAction=PerformedAction AND RecoveryAttempts=1 AND FinalizedEvents=1; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| R02 | 8 | Consumed recovery decision with immutable baseline counts | Replay decision with same and changed actions | CP-L2 | NewAttempts=0 AND NewEvents=0 AND exact replay error AND DecisionConsumedOnce AND LeaseState=RecoveryAuthorized; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| R03 | 5 | CleanupFailed lease and fresh exactly linked recovery decision | Recover using only fresh linked decision | CP-L2 | CleanupFailureCount=1 AND OldDecisionAccepted=0 AND FreshLinkedDecisionCount=1 AND FreshDecisionConsumedOnce AND FinalizedEvents=1; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| C01 | 1 | Registered request exact attempt context claims and runtime transaction | Commit protected rows histories receipt and outcome | TC2 | DeltaBusiness=Expected AND DeltaHistory=Expected AND Receipts=1 AND CommittedOutcomes=1 AND ActiveAttempts=0; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| C02 | 1 | Committed command with lost response and preserved first-run fingerprints | Replay same request and read authoritative receipt | TC2 | Business2=Business1 AND History2=History1 AND ReceiptId2=ReceiptId1 AND ResponseHash2=ResponseHash1 AND counts=1; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| C03 | 1 | Registered idempotency key with independently changed request digest | Replay changed request | TC2 | ChangedDigest!=RegisteredDigest AND exact replay error AND DeltaRequests/Attempts/BusinessHistory=0; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| C04 | 5 | Started attempt with exact receipt failpoint fixture | Attempt business commit through receipt failpoint | TC2 | exact receipt failpoint AND DeltaBusiness/History/Receipts=0 AND RolledBackOutcome=1; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| C05 | 1 | Opened exact command transaction and durable attempt identity | Rollback and independently terminalize | TC2 | OpenedExactAttempt AND TransactionRollback AND DeltaBusinessHistoryReceipts=0 AND exact RolledBackOutcome=1; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| C06 | 4 | Four distinct attempts interrupted at exact transaction boundaries | Restart authoritative reconciler for four attempts | TC2 | distinct before-open/after-open/during-commit/after-response evidence AND exactly one authoritative terminal each; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| C07 | 1 | One command request with concurrent attempt barrier | Start two differently bound attempts | TC2 | StartRequests=2 AND StartedAttempts=1 AND ActiveAttempts=1 AND exact loser error AND UnrelatedMutationCount=0; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| C08 | 8 | Exact attempt plus independently generated binding substitutions | Open or terminalize each substituted binding | TC2 | per substitution: Accepted=0 AND exact binding error AND DeltaContexts/Receipts/BusinessHistory=0; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| G01 | 5 | Five independently invalid purge authorization fixtures | Attempt start for each invalid authorization | TP2 | per invalid authorization: StartedAttempts=0 AND Candidates=0 AND PurgeEvents=0 AND exact binding error; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| G02 | 1 | Fresh authorization with independently verified zero eligible rows | Freeze independently empty candidate batch | TP2 | EligibleBefore=0 AND FrozenCandidates=0 AND DeletedRows=0 AND ZeroRowsEvent=1; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| G03 | 1 | Fresh scoped authorization with independently listed eligible contexts | Delete exact frozen candidates and commit | TP2 | N=EligibleBefore>0 AND Frozen=N AND CandidateHash=Hash(EligibleIds) AND Deleted=N AND Remaining=0 AND SucceededEvent=1; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| G04 | 1 | Started purge with independently observed deterministic candidate drift | Execute drifted frozen deletion | TP2 | CurrentCandidateHash!=FrozenHash AND DeletedRows=0 AND ContextFingerprintAfter=Before AND FailedEvent=1; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| G05 | 3 | Started purge with exact delete failpoint fixture | Rollback failed delete then independently record failure | TP2 | exact delete failpoint AND DeletedRows=0 AND ContextFingerprintAfter=Before AND independently committed FailedEvent=1; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| G06 | 4 | Concurrent starts/executions plus actual failed parent and prospective child | Race then reject substituted retry and accept one exact retry | TP2 | ConcurrentStarts=2 AND ConsumedAuthorizations=1 AND Executions<=1 AND exact monotonic root/prior/policy/outcome retry chain; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| E01 | 1 | Exact organization field as-of expiry authorization and source-row hashes | Prepare immutable minimized batch | TE2 | PreparedRows=ExactAllowedProjection AND Count<=MaximumRows AND PreparedHash=Hash(CanonicalRows) AND ExcludedFieldCount=0; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| E02 | 1 | Prepared batch plus independently inserted later eligible ledger row | Insert later row and reread immutable batch | TE2 | PreparedRowsAfter=Before AND PreparedHashAfter=Before AND CountAfter=Before AND later row independently absent; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| E03 | 4 | Expired wrong-batch terminal and concurrent-active release fixtures | Read or authorize each invalid release | TE2 | per invalid release: ReleasedRows=0 AND NewReleaseEvents=0 AND exact sequence error AND BatchFingerprint unchanged; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| E04 | 3 | ReleaseStarted batch with deterministic delivery-loss barrier | Record Interrupted and authorize distinct linked release | TE2 | R1=Interrupted AND R2.Id!=R1.Id AND R2.Prior=R1.Id AND ActiveReleaseCount=1 AND DeliverySuccessCount<=1; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| A01 | 1 | Canonical control-plane and target ACL inventories | Enumerate every effective privilege | CP-A2 + TA2 | ObservedEffectivePrivileges=Expected AND Observed-Expected=empty AND Expected-Observed=empty; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; ProvisioningContract; DesignTests; Scenarios; Client |
| A02 | 7 | Exact principal protected-object ungranted-operation Cartesian fixtures | Attempt every protected direct privilege and ungranted function | CP-A2 + TA2 | per principal/object/operation: Allowed=false AND exact ACL error AND ProtectedFingerprintAfter=Before; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; ProvisioningContract; DesignTests; Scenarios; Client |
| T01 | 1 | Controller request with exact isolated opt-in and independent verifier connections | Allocate controller-owned fixture | CP-L2 + TA2 | LeaseCount=1 AND FixturePrepared AND TargetCount=1 AND TargetIdentityHash=Expected AND AdminCredentialCountInTest=0 AND LeaseState=InUse; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| T02 | 1 | L04 during-DROP fixture with deterministic controller process failure | Dispose restart and reconcile surviving cleanup attempt | CP-L2 | RestartedControllerInstance!=Original AND ReconciledAttempt=SurvivingAttempt AND one DropStarted/Finalized AND exact absence/cleanup; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | install.sql; CommandContextSql; DesignTests; Scenarios; Client |
| T03 | 4 | All 34 pristine executable plans and semantic mutant corpus | Execute every non-equivalent action read assertion denial and cleanup mutant | OR2 | for every scenario: KilledMutants=RequiredNonEquivalentMutants AND action/read/assertion/denial/cleanup mutants are individually identified; exact SQLSTATE/code/object and terminal from frozen contract | One assertion per oracle component; assertion-removal plus actual evidence tamper rejected for every subcase | DesignTests; SourceContractTests; Client; FrozenOracle |

Result: 34/34 top-level scenarios reconciled into 108/108 uniquely bound subcases. This is a design reconciliation, not execution evidence and not a source-safety PASS. For each row the exact acceptance equation is: identity joins AND before reducer predicates AND action identity AND after reducer predicates AND durable audit/history predicates AND exact SQLSTATE/object/outcome AND post-state predicates AND exact evidence-set/cardinality. Every conjunct is a separately named oracle component and executable assertion. One failed or absent conjunct fails the subcase.
## Correction 26 exhaustive file allowlist

A bounded source-only Correction 26 is GO with exactly these nine files:

1. tools/rev869b-control-plane-install.sql
   - Add/version the least-privilege CP-L2, CP-A2, TC2, TP2, TE2 and TA2 authoritative readers, exact scopes, grants, ownership and revocations.
2. src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs
   - Keep retained migration SQL byte/contract parity with the authoritative install SQL and expose no new bypass.
3. tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs
   - Contract-test reader signatures, owners, grants, defaults, PUBLIC closure, role inheritance and effective privileges.
4. tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs
   - Replace descriptor-derived assertions with oracle-bound typed assertions and corrected formula versions.
5. tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs
   - Execute 34 frozen scenarios/108 explicit subcases later, consuming authoritative observations and the real verifier.
6. tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs
   - Pin oracle hash/version, exact coverage, selector bijection, assertion-removal failures, SQL/ACL contracts, and F23-01 preservation.
7. tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs
   - Implement typed reader DTOs, provenance/scoping validation, reducers, and actual evidence-tampering verifier entry points; controller audit remains supplementary.
8. tests/SESS.NexaERP.Tests/Rev869BCorrection26FrozenOracle.cs
   - New independent literal 34-ID/108-subcase oracle; it must not import or derive from FormulaAssertions or runtime evidence generators.
9. outputs/rev869b_source_correction_checkpoint_26.md
   - Immutable implementation checkpoint containing hashes, mappings, validation, retained boundaries, and pending external gates.

No convenience file, production configuration, migration class/snapshot, application workflow file, or helper outside this list is justified. Correction 26 must stop if implementation requires a tenth file and return to management.

## Mandatory change classification

Mandatory source/SQL changes are the scoped readers and grants in files 1-2. Mandatory test/contract changes are files 3-8. File 9 is the required checkpoint. F23-01's accepted slice is immutable and must retain SHA-256 34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523.

Unavailable execution evidence remains external: provisioned control-plane and target instances; exact runtime/admin/export roles and memberships; pinned TLS/endpoint/system identity; lifecycle controller credentials and process barriers; fixtures/failpoints; later PostgreSQL application and execution for all 108 subcases. These prerequisites block safety/readiness acceptance, but do not block the bounded source correction.

## Objective Correction 26 acceptance

Correction 26 source implementation is acceptable for internal precheck only when all of the following hold:

- Frozen oracle has exactly 34 top-level IDs and 108 unique subcase IDs; its version and SHA-256 are independently pinned.
- The authoritative old inventory remains exactly 133 terms, and every retained or versioned replacement component has exactly one typed reader output, one reducer, one oracle component, and one executable assertion.
- There are no missing, extra, duplicate-meaning, unscoped, label-derived, sentinel, constant-PASS, controller-decisive, or prose-only components.
- All 11 real evidence mutation classes are rejected through the production verifier; assertion removal and weakening fail oracle comparison.
- Multi-subcases have distinct preparation, attempt, evidence, expected result and durable database observation identities.
- CP-A2/TA2 cover direct, inherited, aggregate, owner, PUBLIC and administrative-bypass dimensions; TP2 is exact execution/instance/lease/authorization/batch/attempt/retry-root scoped.
- F23-01 slice hash remains exact; frozen architecture and ACL boundaries are unchanged.
- Offline build has zero warnings/errors; focused, Correction 26 contract/mutation and complete non-PostgreSQL suites pass; exactly 34/108 oracle discovery occurs; PostgreSQL executed-test count is zero.
- PowerShell 5.1 AST, EF no-connect discovery, REV869A/REV869B uniqueness/adjacency, model/snapshot/retained-SQL parity, offline Up/Down generation and independent SHA-256, ACL/owner/default/PUBLIC, secret/privacy/prohibited-operation scans, scope check and git diff --check pass.
- A later independent review still adjudicates source safety; later authorized PostgreSQL execution supplies database acceptance.

## Frozen architecture and boundaries

The four frozen decisions remain internally consistent and necessary: external provisioning establishes pinned infrastructure; the dedicated lifecycle controller owns privileged lifecycle actions; its control-plane database survives target destruction; target-local transactional ledgers prove business transaction outcomes. New readers observe those authorities through least privilege and do not relocate decisions or durable records. No architecture-freeze review is required.

ACL boundaries remain retained: no application runtime role gains control-plane ledger mutation, target lifecycle, unrestricted audit/export reads, role administration, ownership, or administrative bypass. Reader outputs are minimized, scoped, identity-bound projections.

## Decision and next gate

The reconciliation succeeds: the 34/34 failure is consolidated into selector-interface incompleteness, absence of an independent frozen oracle, descriptor-only mutation testing, shared multi-subcase binding, incomplete ACL dimensions, and unscoped purge evidence. Those defects are source-correctable within the nine-file allowlist. External prerequisites remain unavailable and must be used only after separate authorization.

The single next management gate is authorization for one bounded source-only Correction 26 implementation using exactly the nine-file allowlist above. PostgreSQL execution remains prohibited.

f23_01_reconciliation_state=PASS_RETAINED
f23_02_reconciliation_state=PASS
formula_term_inventory_count=133
missing_selector_reconciliation_state=PASS
frozen_oracle_design_state=PASS
mutation_rejection_design_state=PASS
multi_subcase_binding_design_state=PASS
acl_projection_design_state=PASS
purge_scope_design_state=PASS
correction_26_source_only_gate=GO
architecture_freeze_review_required=NO
frozen_architecture_state=RETAIN
acl_boundary_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
