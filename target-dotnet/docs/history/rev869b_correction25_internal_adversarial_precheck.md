# REV869B Correction 25 internal adversarial source-only precheck

Date: 2026-08-15

Verdict: **FAIL**. F23-01 remains byte-for-byte preserved and fail-closed. F23-02 is not source-complete: every one of the 34 scenario formulas contains at least one decisive selector that no committed authoritative reader returns. The mechanically passing mutation tests construct their pristine values from the assertions being tested rather than from the reader schemas, so they cannot detect this runtime interface contradiction.

This is an internal precheck and is not independent.

## 1. Entry gate

| Gate | Evidence | Result |
|---|---|---|
| HEAD | `1a19780ca8a85415adc54d2926055a733ba94253` | PASS |
| Parent | `6fce8512bbf8682edee4502529f6dcd49df65351` | PASS |
| Branch | `master` | PASS |
| Checkpoint | `outputs/rev869b_source_correction_checkpoint_25.md` | PASS |
| Checkpoint SHA-256 | `8F3B87D521C0439040A9BC195B8EB1CC7C2112E8C56E4FAA997F4193251FEC23` | PASS |
| Target-scoped status | 0 entries before review | PASS |
| Correction 25 scope | exactly the authorized seven files | PASS |
| Later commits/changes | 0 later commits; no later source/test/SQL/helper change | PASS |
| F23-01 slice SHA-256 | `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523` | PASS |
| Frozen architecture / ACL boundary | checkpoint states RETAIN; committed diff does not redesign provisioning/controller/control-plane/target-ledger ownership | PASS |

The seven committed files are the two authorized SQL sources, four authorized test/client sources, and `outputs/rev869b_source_correction_checkpoint_25.md`. No PostgreSQL, provisioning, migration application/removal, lifecycle, purge, recovery, quarantine, export or production operation was executed. `../legacy-reference/` was not accessed.

## 2. F23-01 — PASS_RETAINED

The authoritative slice from `rev869b_authorize_normal_drop` through the function immediately before recovery-decision registration has the exact expected SHA-256. Correction 25 changed only evidence-reader SQL elsewhere in the control-plane file.

`tools/rev869b-control-plane-install.sql:164-182` still requires:

- distinct nonzero registration and transition request IDs;
- the exact immutable event with the supplied lease and registration request, no attempt, `Ready`/`InUse` pre-state, `DropAuthorized` transition and exact expected version;
- exact lifecycle principal and manifest-bound target identity attributes;
- the lease still in `DropAuthorized` at that same version;
- insertion of the normal-drop attempt with the authoritative event evidence hash; and
- an atomic exact-version transition to `DropStarted`.

Missing, substituted, stale, replayed, cross-instance, cross-lease, wrong-version and wrong-event registrations remain fail-closed. No Correction 25 hunk overlaps or weakens the accepted block.

## 3. F23-02 consolidated adversarial findings

### F25-PRE-01 — universal runtime formula/reader interface contradiction

All formula assertions are forced onto `EvidenceStage.Durable` at `Rev869BCorrection14PostgresDesignTests.cs:119-160`. Runtime receives unmodified JSON from exactly one reader surface at `Rev869BLifecycleControllerClient.cs:96-107,481-512`; it performs no derivation/projection step before `Evaluate`.

A source-level comparison found:

- 34 scenario formulas inspected;
- 133 declared formula terms;
- 105 distinct missing top-level selector names; and
- 34/34 scenarios with at least one missing decisive selector.

A missing selector makes `Resolve` return null and its decisive assertion evaluate false. Thus the committed runtime cannot satisfy any complete scenario formula even if all external prerequisites were later supplied correctly.

### F25-PRE-02 — synthetic test evidence is generated from expected assertions

`BuildSyntheticEvidence` (`Rev869BLifecycleControllerClient.cs:228-293`) iterates each assertion and writes a value selected specifically to satisfy that assertion: zero for `Zero`, one for `GreaterThanZero`, the expected string for `EqualsLiteral`, a 64-character string for SHA-256, and paired synthetic strings for cross-observation equality. The test then evaluates those generated values (`Rev869BCorrection17SourceContractTests.cs:311-325`; `Rev869BCorrection17PostgresScenarios.cs:41-56`).

This proves evaluator mechanics against a tautological bundle. It does not prove that any authoritative reader emits the selector, type, cardinality or row set used by the formula.

### F25-PRE-03 — formula manifest is derived from the assertion list, not independently immutable

At `Rev869BCorrection14PostgresDesignTests.cs:24-29`, both `RequiredComponentIds` and `FormulaComponents` are generated directly from `assertions`; every `LocalReducer` is the same label, `authoritative-local-reducer`. No reducer function exists and no component independently specifies a row selector/cardinality/hash computation over reader output. Set equality in `ValidateContract` therefore proves that three copies derived from the same list agree, not that the frozen formula is complete.

`RemoveAssertion` is rejected only because `ApplyMutation` deletes the assertion after the manifest was built. Removing a decisive assertion from the source inventory before plan construction removes it from all three derived lists, so the offline contract lacks an independent expected-component oracle.

### F25-PRE-04 — semantic mutation corpus does not require authoritative mutated evidence rejection

The per-assertion tamper loop does make each synthetic value fail its own assertion, but the named cross-instance/cross-lease/wrong-version/wrong-count mutants append new assertions or change descriptor structure (`Rev869BLifecycleControllerClient.cs:383-441`). They do not mutate an observed reader bundle. In T03, most are accepted as killed when `ValidateContract` throws; if structural validation does not throw, lines 68-75 merely require a marker expected value and do not call `Evaluate` on the changed contract.

No test binds an identity/lease/version/count mutation to a real reader-shaped JSON schema and proves that the related authoritative component fails.

### F25-PRE-05 — multi-subcase evidence is label-only

Subcase requirements are strings (`Rev869BLifecycleControllerClient.cs:685-693`). The preparation request sends only the scenario and single fixture/action IDs (`:444-456`). `ScenarioPreparation` contains one lease, command, authorization, attempt, decision, batch and release (`:724-728`), and every before/after/durable read reuses that one binding. There is no subcase binding collection.

R02, C06, C08, G01, G06, E03, A02 and boundary/race scenarios therefore cannot produce independently keyed per-subcase fixtures and observations required by their formulas.

### F25-PRE-06 — reader content is incomplete or not least-data

- `rev869b_read_command_evidence` calls command claims `businessRows`/`historyRows`; it does not query the actual business and history relations, so it cannot independently detect a missing or altered claimed row.
- `rev869b_read_purge_evidence` returns `contextRows` for the entire target command-context table without authorization/organization scope. That is not a least-data scenario projection and can disclose unrelated row identifiers/attempts to the verifier function caller.
- Both ACL readers return raw ACL and role-attribute text but contain zero `pg_auth_members` membership facts and zero effective `has_*_privilege` evaluations. They cannot establish inherited/effective role closure or the declared observed-minus-expected formulas.
- Export evidence returns stored rows and recomputed payload hashes but not the independently selected eligible source-row projection needed to establish later-row exclusion and exact allowed source values.

The functions remain executable only by their verifier roles, use fixed search paths, and have no `PUBLIC` execute grant. Those retained ACL controls do not cure incomplete or overbroad evidence projections.

### Preserved partial controls

The following controls pass but do not rescue a scenario:

- 34 unique scenario IDs and operation/read labels exist;
- exact terminal and applicable SQLSTATE/code/object mappings exist at `Rev869BCorrection14PostgresDesignTests.cs:175-205`;
- `ValidateContract` prohibits decisive `Audit` stage assertions, `Audit:` references and self-asserted literal PASS;
- no P02/P03 arithmetic sentinel is used as acceptance evidence;
- action correlation IDs and pinned controller/audit origins remain checked; and
- reader execute grants remain verifier-only with fixed search paths and zero `PUBLIC` reader grants.

## 4. Explicit 34-scenario matrix

Evidence locations are abbreviated: `D127` means the formula assertions at `Rev869BCorrection14PostgresDesignTests.cs:127`; `CL` is the control lifecycle reader at `tools/rev869b-control-plane-install.sql:227-247`; `CA` is the control ACL reader at `:248-256`; `TC`, `TP`, `TE`, and `TA` are target command/purge/export/ACL readers at `Rev869BCommandContextSql.cs:159-180,182-201,203-217,219-234`. All rows also inherit the synthetic-evidence defect at client lines 228-328 and the non-independent component construction at design lines 24-29.

| ID | Formula / exact expectation | Reader and missing decisive selectors | Assertion/removal/tamper result | Verdict |
|---|---|---|---|---|
| P01 | Pin mismatch 0; exact control/target ACL; exact verify (`D127`) | CA; missing `pinMismatchCount`, `targetAclDeltaCount`, `verifyResult` | 3/3 formula terms unreachable; synthetic removal/tamper passes mechanically | FAIL |
| P02 | mismatch 1; lease/action 0; exact `REV869B_PREFLIGHT_PIN_MISMATCH/mutated-pin` (`D128`) | CL; missing `pinMismatchCount`, `allocatedLeaseCount`, `actionCount` | 3/3 unreachable; absence is not locally derived | FAIL |
| P03 | one seeded delta; reported=seeded; protected 0; cleanup=baseline; exact catalogue error (`D129`) | CA; missing `seededDeltaCount`, `reportedDeltaSha256`, `protectedMutationCount`, `cleanupFingerprint` | 4/4 unreachable; synthetic equality is fabricated | FAIL |
| L01 | Reserved 1; resume XOR cleanup; duplicates 0 (`D130`) | CL; missing `reservedEventCount`, `resumeSameAttempt`, `duplicateAttemptCount` (and no authoritative `authorizedCleanup`) | 3/3 unreachable; branch values are synthetic | FAIL |
| L02 | each boundary started/reconciled/target/roles=1 (`D131`) | CL; all 5 selectors missing; one preparation binding only | 5/5 unreachable; boundary labels are not independent rows | FAIL |
| L03 | requests 2; DropStarted/active/physical/chain=1; exact `40001/UX_rev869b_one_active_lifecycle_attempt` (`D132`) | CL; all 5 selectors missing | 5/5 unreachable; F23-01 source protection is preserved but scenario proof is absent | FAIL |
| L04 | per boundary DropStarted/Finalized=1, physical<=1, target/roles=0 (`D133`) | CL; all 5 selectors missing; one binding | 5/5 unreachable; no per-boundary bundle | FAIL |
| L05 | use/drop 0; quarantine 1; exact `42501/rev869b_target_identity_mismatch` (`D134`) | CL; missing all 3 formula selectors | 3/3 unreachable; quarantine object presence does not compute formula | FAIL |
| R01 | decision 1; exact consumed attempt/action; recovery/finalized 1 (`D135`) | CL; missing 4 of 5: consumed/action/recovery/finalized | decisive chain incomplete; synthetic references only | FAIL |
| R02 | new attempts/events 0; consumed once; exact replay error (`D136`) | CL; all 3 selectors missing; eight subcases share one binding | 3/3 unreachable; subcases label-only | FAIL |
| R03 | failure 1; old accepted 0; fresh linked/consumed/finalized 1 (`D137`) | CL; all 5 selectors missing | 5/5 unreachable; retry chain not reduced locally | FAIL |
| C01 | business/history deltas expected; receipt/Committed=1; active=0 (`D138`) | TC; missing business/history deltas and `committedOutcomeCount` | 3/5 unreachable; returned “rows” are claims, not business/history rows | FAIL |
| C02 | business/history/receipt/response replay equality; singular count (`D139`) | TC; missing 4 of 5 replay fingerprint selectors | equality values synthetic; no locally computed before/after fingerprints | FAIL |
| C03 | changed digest; request/attempt/business-history deltas 0; exact replay error (`D140`) | TC; all 4 selectors missing | 4/4 unreachable; reader counts are not converted to deltas | FAIL |
| C04 | failpoint; business/history/receipt deltas 0; durable RolledBack=1 (`D141`) | TC; all 4 selectors missing | 4/4 unreachable; outcome row exists but formula count is not derived | FAIL |
| C05 | exact open; business/history/receipt delta 0; RolledBack=1 (`D142`) | TC; all 3 selectors missing | 3/3 unreachable; one attempt binding cannot prove all opening terms | FAIL |
| C06 | four distinct interruptions/evidence IDs; one terminal each (`D143`) | TC; all 3 selectors missing; no four attempt bindings | 3/3 unreachable; subcase labels only | FAIL |
| C07 | starts 2; started/active 1; unrelated 0; exact loser error (`D144`) | TC; missing start/started/unrelated selectors | 3/4 unreachable; active count alone is insufficient | FAIL |
| C08 | each substitution accepted 0; context/receipt/business-history deltas 0; exact error (`D145`) | TC; all 4 selectors missing; eight substitutions share one binding | 4/4 unreachable; subcase mutation is not authoritative evidence | FAIL |
| G01 | per invalid auth attempts/candidates/events 0; exact binding error (`D146`) | TP; missing `startedAttemptCount`, `purgeEventCount`; five cases share one binding | decisive zero proof incomplete and compressed | FAIL |
| G02 | eligible/frozen/deleted 0; ZeroRows event 1 (`D147`) | TP; all 4 selectors missing | 4/4 unreachable; row arrays are not locally reduced | FAIL |
| G03 | N>0; frozen/deleted=N; local hash; remaining 0; success 1 (`D148`) | TP; all 5 selectors missing | 5/5 unreachable; stored fields do not implement equality reducers | FAIL |
| G04 | current hash differs; deleted 0; context unchanged; failed 1; exact drift error (`D149`) | TP; all 4 selectors missing | 4/4 unreachable; unscoped context projection is not scenario isolation | FAIL |
| G05 | delete failpoint; deleted 0; context unchanged; durable failed 1 (`D150`) | TP; all 3 selectors missing | 3/3 unreachable; no independently derived failure count | FAIL |
| G06 | starts 2; consumed 1; execution<=1; child 1; substituted 0; exact retry error (`D151`) | TP; missing 4 of 5 selectors; race/substitutions share one binding | chain fields exist but declared reducers do not | FAIL |
| E01 | allowed projection; count<=max; local hash; excluded 0; prepared event (`D152`) | TE; all 4 executable selectors missing | 4/4 unreachable; eligible source projection absent | FAIL |
| E02 | prepared rows/hash/count unchanged; later row exists but absent from batch (`D153`) | TE; all 4 selectors missing | 4/4 unreachable; later eligible source row is not observed | FAIL |
| E03 | four invalid releases: released/events 0; batch unchanged; exact sequence error (`D154`) | TE; all 3 selectors missing; one release binding | 3/3 unreachable; subcases compressed | FAIL |
| E04 | distinct linked retry release; active 1; delivery<=1; batch unchanged (`D155`) | TE; missing 4 of 5 selectors | exact release chain is not mapped to formula selectors | FAIL |
| A01 | observed effective privileges exact; both set differences empty (`D156`) | TA; all 3 selectors missing; membership/effective privilege facts absent | 3/3 unreachable; raw ACL facts are not effective closure | FAIL |
| A02 | each access denied; protected fingerprint unchanged; durable denial exact (`D157`) | TA; all 3 selectors missing; access tuples share one binding | 3/3 unreachable; no durable denial projection | FAIL |
| T01 | lease/target=1; fixture true; identity exact; admin credentials 0; InUse (`D158`) | CL; missing target/fixture/admin selectors | 3/4 unreachable; signed preparation cannot decide database formula | FAIL |
| T02 | restarted instance differs; attempt same; DropStarted/Finalized/cleanup=1 (`D159`) | CL; all 5 selectors missing | 5/5 unreachable; process IDs are not database projections | FAIL |
| T03 | killed=required; survivors 0; every mutant identified (`D160`) | Offline synthetic path; `killedMutants` and `survivingMutants` are generated from the assertions, not test results | no killed/survivor accumulator; named mutants often die structurally without observed bundle mutation | FAIL |

Totals: **0 PASS / 34 FAIL**.

## 5. Offline validation reproduction

| Validation | Result |
|---|---|
| Build (`--no-restore`) | PASS: 0 warnings, 0 errors |
| Correction 25 contract/mutation filter | PASS mechanically: 14/14; does not validate authoritative selector availability |
| Focused REV869B non-PostgreSQL | PASS: 75/75 |
| Complete non-PostgreSQL suite | PASS: 466/466 |
| Scenario discovery only | PASS: 34 discovered, 34 unique, PostgreSQL executed=0 |
| PowerShell 5.1 AST | PASS: 5.1.19041.6456; 24 scripts, 0 errors, 0 executed |
| EF migration discovery | PASS: `--no-connect`, inert `127.0.0.1:1`, exactly 13 migrations |
| Migration uniqueness/order | PASS: one REV869A and one REV869B primary migration; adjacent |
| Model/snapshot, retained SQL and pinned SQL contracts | PASS: 3/3 |
| Offline Up SQL | PASS: 284254 UTF-8 bytes, 2426 lines, SHA-256 `554C8CA562DEC27CDFC80B70D72EEE6D68AB67F86A4F585DF77FAD38B6A1787A` |
| Offline Down SQL | PASS: 10629 UTF-8 bytes, 222 lines, SHA-256 `78F17339EE2FCB75D09B4E7581C8D84ED7199B1C687825FE0A1892B6798B7360` |
| F23-01 slice | PASS: `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523` |
| ACL mechanical scan | PASS: 6 readers, 6 fixed search paths, 0 `PUBLIC` reader grants |
| ACL adversarial completeness | FAIL: 0 membership projections, 0 effective-privilege checks; declared A01/A02 selectors absent |
| Reader minimization | FAIL: purge `contextRows` is target-wide rather than authorization/scenario scoped |
| Formula/reader selector scan | FAIL: 34/34 formulas missing decisive selectors; 105 distinct missing names across 133 terms |
| Secret/privacy/prohibited-operation source-diff scan | PASS: 0 literal secret assignments, 0 privacy terms, 0 legacy tokens, 0 mutating client SQL commands |
| `git diff --check HEAD^ HEAD` | PASS |
| Target-scoped status before report | PASS: clean |

The offline SQL was generated in-process through the pinned `IMigrator.GenerateScript` test; no PostgreSQL connection was opened and no SQL was applied or written as a generated artifact.

## 6. Architecture, ACL boundary and external prerequisites

The frozen architecture remains valid and retained: external provisioning, dedicated lifecycle controller, surviving control-plane database and target-local transactional ledgers. The failures are source evidence-interface and test-design defects and do not justify architectural redesign.

The existing ownership/grant boundary is retained: fixed security-definer search paths, verifier-only reader execute grants and no `PUBLIC` reader grant. Correction 25 nevertheless fails to prove effective ACL closure and least-data evidence because role membership/effective privilege projections are absent and the purge reader is overbroad. `acl_boundary_state=RETAIN` means the frozen ownership/grant design is not redesigned; it is not an A01/A02 PASS.

Unavailable external prerequisites remain blocking for later execution: an isolated externally provisioned PostgreSQL cluster; exact external principals; pinned source/manifest/TLS/cluster/signing identities; independent action/audit origins and trust roots; separately authorized provisioning/application; and later authorization to execute and retain all scenario evidence. These prerequisites cannot repair missing source selectors, absent subcase bindings, synthetic tautology or incomplete reader schemas.

## 7. Required next gate

The single next gate is **management authorization for a report-only Correction 25 internal-precheck failure reconciliation**. It must define a bounded correction that provides an independently frozen component manifest, real local reducer functions over typed reader schemas, exact per-subcase bindings, authoritative business/history and eligible-source projections, complete effective ACL/membership evidence, scoped purge evidence, and mutation tests against reader-shaped bundles. It must not begin Correction 26 without that reconciliation.

correction_25_internal_precheck_state=FAIL
correction_25_internal_precheck_independence_state=NOT_INDEPENDENT
f23_01_state=PASS_RETAINED
f23_02_internal_precheck_state=FAIL
correction_25_scenario_pass_count=0
correction_25_scenario_fail_count=34
frozen_architecture_state=RETAIN
acl_boundary_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN