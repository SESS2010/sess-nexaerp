# REV869B source Correction 25 checkpoint

Date: 2026-08-15

## Authority, entry gate, and commit boundary

- Authorized starting HEAD and parent of this correction: `6fce8512bbf8682edee4502529f6dcd49df65351`.
- Authoritative reconciliation: `outputs/rev869b_correction24_internal_precheck_failure_reconciliation.md`.
- Reconciliation SHA-256: `E8090F1793A3A208B57980AA760F8EF1EDA658B36FC0B0855FEE0267D74BCB97` (exact match).
- The target-scoped worktree was clean at entry. The report was read completely and contained exactly 34 unique mapped scenario rows.
- Entry states were confirmed: `f23_01_reconciliation_state=PASS`, `correction_25_source_only_gate=GO`, `frozen_architecture_state=RETAIN`, and `acl_boundary_state=RETAIN`.
- Ending commit: the single Git commit containing these seven files. Its SHA is reported in the post-commit handoff; a commit cannot embed its own SHA without changing that SHA.
- No PostgreSQL access/test, provisioning, migration apply/remove, lifecycle/drop/purge/recovery/quarantine/export/production operation, or access to `../legacy-reference/` occurred.

## Exact exhaustive seven-file scope

1. `tools/rev869b-control-plane-install.sql`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
3. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
7. `outputs/rev869b_source_correction_checkpoint_25.md`

No eighth file was created, renamed, or modified.

## Root-cause implementation mapping

| Root cause | Correction 25 implementation | Objective offline evidence |
|---|---|---|
| RC25-01 incomplete authoritative projections | Control lifecycle and command/purge/export readers are absence-capable and return exact target identity, request/authorization/attempt linkage, ordered row sets, counts, terminal/audit rows, isolation rows, hashes, owners, defaults, roles and protected-state projections. | Six reader definitions, six fixed search paths, zero `PUBLIC` reader grants; ACL scan and focused contracts pass. |
| RC25-02 controller-audit verdict dependency | Controller audit is retained as a supplementary process observation only. `ValidateContract` rejects every decisive `EvidenceStage.Audit`, `Audit:` reference, and literal PASS assertion. | All 34 contracts validate; literal scan finds only defensive rejection code. |
| RC25-03 formula text was not executable/bijective | Every typed `FormulaComponent` has one stable component ID, authoritative stage/selector, local reducer/operator and expected predicate. Component IDs, required IDs and assertion IDs must be equal sets and exact definitions. | The 34-contract offline test proves one-to-one set equality and rejects removal/substitution through contract validation. |
| RC25-04 descriptor-only mutations survived | `BuildSyntheticEvidence` constructs typed pristine observations; `TamperEvidence` alters the decisive observed path/canonical hash; every assertion is re-evaluated and must become false. Explicit action/read/assertion/fabricated/duplicate/substituted/stale/cross-instance/cross-lease/wrong-version/wrong-count mutants are retained. | Correction 25 source/contract/mutation set passes 14/14; focused REV869B passes 75/75. |
| RC25-05 absence/count/hash evidence was copied or asserted | Counts are reader cardinalities, row sets are ordered, hashes are recomputed from returned authoritative rows, and expected absence is represented by structured zero/null projections instead of missing function results or sentinels. | P02/P03 sentinel rejection, pinned SQL, reader-contract tests and evidence-tamper tests pass. |

F23-01 is preserved byte-for-byte. The `rev869b_authorize_normal_drop` through pre-recovery-decision slice SHA-256 remains `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523`. It continues to bind the exact immutable `DropAuthorized` event, registration request, target instance, lease, lifecycle version, authorization identity and expected pre-state and retains fail-closed missing/substituted/stale/replayed/cross-instance/cross-lease/wrong-version/wrong-event behavior.

## Complete 34-scenario implementation matrix

Each row has a unique `rev869b/<ID>/fixture/v2`, action, cleanup and five read IDs. `DB` means the decisive source is a fresh verifier query; `Action` supplies only exact reached/error correlation; `Audit` is supplementary and cannot decide PASS. Every row has assertion-removal plus observed-evidence tampering coverage.

| ID | Authoritative local formula / decisive evidence | Mutation rejection evidence | State |
|---|---|---|---|
| P01 | DB CP/target ACL facts: pin mismatch 0, exact inventories/set deltas 0, exact verifier result and hashes. | pin/owner/ACL/default/role/PUBLIC/count/hash tamper | Implemented pending precheck |
| P02 | DB absence plus local pins: mismatch 1, lease/action allocation 0; exact preflight error from Action. | fabricated mismatch, lease/event insertion, removed zero assertion | Implemented pending precheck |
| P03 | DB before/after/cleanup: one seeded delta, local reported hash equality, protected mutation 0, baseline restored. | hidden/substituted delta, copied baseline, sentinel/generic error | Implemented pending precheck |
| L01 | DB lease/event chain: Reserved 1, resume XOR authorized cleanup, duplicate attempt 0. | attempt/request/version/branch/event/absence tamper | Implemented pending precheck |
| L02 | DB per-boundary lifecycle evidence: boundary positive and exactly one started/reconciled/target/role set. | missing/reused boundary, count/state/version substitution | Implemented pending precheck |
| L03 | DB request/event/attempt chain: requests 2, DropStarted 1, active 1, exact authorization-registration transition. | missing/stale/replayed/cross-lease/target/version/event registration | Implemented pending precheck |
| L04 | DB per-boundary: DropStarted 1, Finalized 1, physical maximum 1, target/roles absent. | removed boundary, duplicate terminal, altered attempt/version, retained object | Implemented pending precheck |
| L05 | DB lifecycle/identity: use/drop mutations 0, quarantine outcome 1 and exact hashes. | instance/attempt/version/hash/quarantine/zero-count tamper | Implemented pending precheck |
| R01 | DB decision chain: decision 1, exact consumed attempt/action, recovery 1, Finalized 1. | decision/action/attempt/lease/consumption/absence tamper | Implemented pending precheck |
| R02 | DB eight subcases: new attempts/events 0 and decision consumed exactly once; exact replay error. | subcase/nonce/lease/version/action/count/error tamper | Implemented pending precheck |
| R03 | DB: failure 1, old accepted 0, fresh linked/consumed 1, Finalized 1. | old/foreign decision, severed root/outcome/lease/action | Implemented pending precheck |
| C01 | DB command rows: business/history deltas exact, receipt 1, Committed 1, active 0. | row/receipt/outcome/identity/count tamper | Implemented pending precheck |
| C02 | DB replay: business/history/receipt/response fingerprints equal and receipt/outcome singular. | second mutation/receipt/outcome, response/hash/equality removal | Implemented pending precheck |
| C03 | DB absence-capable rows: digest differs; request/attempt/business-history deltas 0; exact error. | digest equality, inserted attempt/row, object/zero assertion tamper | Implemented pending precheck |
| C04 | DB rollback rows: business/history/receipt deltas 0 and durable RolledBack 1; exact trigger error. | transaction-local/fabricated outcome, row/receipt/trigger tamper | Implemented pending precheck |
| C05 | DB exact opened binding: business/history/receipt delta 0 and durable RolledBack 1. | backend/actor/org/version/role/operation/context/row tamper | Implemented pending precheck |
| C06 | DB four distinct attempts: four evidence IDs and one exact terminal outcome per attempt. | compressed/reused evidence, swapped/duplicate/missing terminal | Implemented pending precheck |
| C07 | DB concurrency: starts 2, started/active 1, unrelated mutation 0; exact loser error. | serialized/reused request, active count 2, isolation/error tamper | Implemented pending precheck |
| C08 | DB eight substitutions: accepted 0 and context/receipt/business-history deltas 0. | missing substitution or any binding/count/error change | Implemented pending precheck |
| G01 | DB five absence projections: attempts/candidates/events 0; exact authorization error. | missing/compressed case, fabricated zero, created row/error change | Implemented pending precheck |
| G02 | DB eligibility projection: eligible/frozen/deleted 0 and ZeroRows event 1. | stored/default zero, candidate insertion, event removal | Implemented pending precheck |
| G03 | DB ordered IDs: N>0, frozen/deleted=N, local hash exact, remaining 0, success 1. | ID/order/hash/count/unrelated-row tamper | Implemented pending precheck |
| G04 | DB frozen/current sets: hashes differ, deleted 0, context unchanged, failed event 1. | equalized hash, deletion/context/error/durable-event tamper | Implemented pending precheck |
| G05 | DB rollback: deleted 0, context fingerprint unchanged, independently durable Failed 1. | transaction-local evidence, trigger/fingerprint/failure removal | Implemented pending precheck |
| G06 | DB race/retry chain: starts 2, consumed 1, execution <=1, exact root/prior/policy/outcome/hash, one child. | each linkage, reused child, count/parent-terminal tamper | Implemented pending precheck |
| E01 | DB ordered minimized rows: allowed fields only, count <= max, locally recomputed row/batch hashes, Prepared event. | excluded field/row/order/hash/max/stored-hash trust | Implemented pending precheck |
| E02 | DB immutable batch: batch/hash/count unchanged, later row exists and is excluded. | later-row inclusion, omitted existence, hash/count/label tamper | Implemented pending precheck |
| E03 | DB four invalid releases: released rows/events 0, prepared hash unchanged; exact error. | missing case, release/event insertion, object/batch equality tamper | Implemented pending precheck |
| E04 | DB release chain: R1 Interrupted, R2 distinct with Prior=R1, active 1, delivered <=1, batch unchanged. | reused ID, severed prior, active/delivered/batch tamper | Implemented pending precheck |
| A01 | DB exact CP/target facts: observed=expected and both set differences empty across all ACL dimensions. | any tuple/owner/role/default/PUBLIC/set-difference tamper | Implemented pending precheck |
| A02 | DB direct-access cases: allowed false, protected fingerprint unchanged, durable denial 1; exact error. | removed tuple, allowed operation, row/error/fingerprint tamper | Implemented pending precheck |
| T01 | DB lease/target/ACL: lease 1, local fixture identity/hash, roles exact, admin credentials 0, InUse; cleanup absence. | admin/wrong target/role/hash/duplicate/cleanup tamper | Implemented pending precheck |
| T02 | DB surviving attempt: exact reconciliation, DropStarted/Finalized 1, target/roles absent, cleanup evidence 1. | attempt/instance/event/retained-object/cleanup tamper | Implemented pending precheck |
| T03 | Synthetic typed canonical bundles: killed mutants equal all required components and survivors 0. | every identity/count/state/audit-row/target/lease/version/result and structural mutant evaluates false or fails validation | Implemented pending precheck |

Totals: 34 unique scenario identities mapped; 34 implementation rows; 0 PostgreSQL scenarios executed. No scenario is compressed into another, and no controller-audit signature, echoed label, copied 1/1 evidence, P02/P03 sentinel, source-text-only mutation, or self-declared PASS value can decide acceptance.

## Assertion-to-formula and mutation evidence

- `RequiredComponentIds`, typed `FormulaComponents`, and executable `EvidenceAssertion` IDs are enforced as equal unique sets for every scenario.
- A component binds its authoritative stage and JSON selector, local reducer/operator, and expected literal or cross-observation selector to exactly one assertion.
- Decisive stages are Action, Before, After, Durable, and Cleanup. Audit-stage assertions are rejected.
- Every pristine component evaluates true in the offline typed corpus. Tampering that component's observed value/path/hash makes that exact assertion false.
- Removing a decisive assertion leaves the immutable component manifest unmatched and fails contract validation.
- Each scenario includes 14 structural/evidence-class mutants plus one remove-assertion mutant per decisive component: action; five reads; fabricated, duplicate, substituted, stale, cross-instance, cross-lease, wrong-version and wrong-count evidence.

## Offline validation record

| Validation | Result |
|---|---|
| Build (`--no-restore`) | PASS: 0 warnings, 0 errors |
| Correction 25 source-contract/mutation/SQL-hash tests | PASS: 14/14 |
| Focused REV869B non-PostgreSQL suite | PASS: 75/75 |
| Complete non-PostgreSQL suite | PASS: 466/466 |
| Scenario discovery | PASS: 34 discovered, 34 unique, PostgreSQL executed=0 |
| PowerShell 5.1 AST | PASS: 5.1.19041.6456; 24 scripts, 0 errors, 0 executed |
| EF migration discovery | PASS: explicit `--no-connect`, inert `127.0.0.1:1`, exactly 13 migrations |
| REV869A/REV869B uniqueness/order | PASS: one primary migration each; adjacent |
| Model/snapshot and retained SQL contracts | PASS: 2/2 |
| Offline Up SQL | PASS: in-process `IMigrator.GenerateScript`, no connection; 284254 UTF-8 bytes, 2426 logical lines, SHA-256 `554C8CA562DEC27CDFC80B70D72EEE6D68AB67F86A4F585DF77FAD38B6A1787A` |
| Offline Down SQL | PASS: in-process `IMigrator.GenerateScript`, no connection; 10629 UTF-8 bytes, 222 logical lines, SHA-256 `78F17339EE2FCB75D09B4E7581C8D84ED7199B1C687825FE0A1892B6798B7360` |
| ACL/owner/default/role/PUBLIC scan | PASS: 6/6 readers fixed-search-path, 0 missing, 0 `PUBLIC` grants; owner/default/role/identity projections present |
| Secret/privacy/legacy scan | PASS: 0 added literal secret assignments, 0 privacy terms, 0 legacy-reference mentions |
| Prohibited-operation scan | PASS: 0 mutating `NpgsqlCommand` paths; no helper or operational execution |
| `git diff --check` | PASS |
| Exact scope before checkpoint | PASS: exactly six authorized implementation files; this checkpoint is the seventh |

The EF CLI's migration-script command has no `--no-connect` option, so SQL hashes were generated and pinned through the compiled in-process `IMigrator.GenerateScript` contract. That path created SQL in memory and did not open a database connection or write a generated SQL file.

## File SHA-256 before commit

| File | SHA-256 |
|---|---|
| `tools/rev869b-control-plane-install.sql` | `526319FE9B467A40F8CA3593512A4E3A5A69104A1C19F5C20A9C238AE63B9CE3` |
| `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | `251C5AE38E90B767B3B719EEC8817446DF5BEC26192540FC26F4C175530A00B9` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | `64622D3D8CF51C3378270F1475FBB4A56EF0791252A8BF15A7341C5702444700` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | `DB52B0A5697A16C562CD8A34A29B08D16F1B52055B9E714E0C52E3D4DFC6584D` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | `B03C3494EDD0597270F3CC1D36BDE713E43B0EE1FBF8CBC496A4C4A093030738` |
| `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | `D2CA76AE1FB62F00D9886343BAB98C36D5CB18AFC8EE1E1C595B9C9EED392D93` |

## Architecture, prerequisites, and next gate

The frozen architecture is retained unchanged: external provisioning, a dedicated lifecycle controller, a surviving control-plane database, and target-local transactional ledgers. Existing ownership, retention, runtime/admin/audit/purge/export/verifier and `PUBLIC` boundaries remain retained; no direct ledger DML or privilege broadening was added.

External prerequisites remain blocking for later execution evidence: a separately provisioned isolated PostgreSQL cluster; independently pinned source, manifest, TLS, cluster and signing identities; exact owner/lifecycle/runtime/admin/audit/purge/export/recovery/verifier principals; independent controller action and audit origins/trust roots; applied committed package through separately authorized provisioning; and explicit authorization to execute all 34 PostgreSQL scenarios and preserve their per-scenario before/after/durable/cleanup rows and local component verdicts.

The single mandatory next gate is a separately authorized internal adversarial source-only precheck of the committed Correction 25. This implementation makes no source-safety PASS, execution-helper-readiness PASS, database-acceptance, or production-readiness claim.

correction_25_source_implementation_state=COMPLETE_PENDING_PRECHECK
f23_01_state=PASS_RETAINED
f23_02_source_correction_state=IMPLEMENTED_PENDING_PRECHECK
frozen_architecture_state=RETAIN
acl_boundary_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN