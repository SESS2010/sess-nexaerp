# REV869B Correction 26 source implementation checkpoint

Date: 2026-08-15

## Authority and entry gate

- Authorized starting HEAD: `44857f6b3c9810d2499cf4f4ea2320f39ca5c4a2`
- Expected parent: `0ac13207a7e083943944af83899c1212424c21e9`
- Authoritative reconciliation: `outputs/rev869b_correction25_internal_precheck_failure_reconciliation.md`
- Reconciliation SHA-256: `AE313D0FF3F1ABD9DFA2EC623930EB2C644910EC31DECB23A4D22579420A235A`
- Entry status: PASS. HEAD, parent, report hash, branch and target-scoped cleanliness matched before editing.
- Reconciliation inventory: PASS, exactly 133 formula terms, the reported 105 missing selector names plus the separately reconciled `authorizedCleanup` atomic input, 34 frozen scenarios and 108 explicit subcases.
- F23-01 preservation: PASS. The byte slice from `rev869b_authorize_normal_drop` to immediately before `rev869b_register_recovery_decision` remains SHA-256 `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523`.
- Frozen architecture and ACL boundary: RETAIN.
- Ending commit: the single Correction 26 commit containing this immutable checkpoint; its resolved SHA is reported by the committing agent after commit because a commit cannot contain its own SHA.

No PostgreSQL connection, PostgreSQL test, provisioning, migration application/removal, lifecycle, drop, purge, recovery, quarantine, export, production or legacy-reference operation was performed.

## Exact nine-file scope

1. `tools/rev869b-control-plane-install.sql`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
3. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BCorrection26FrozenOracle.cs`
9. `outputs/rev869b_source_correction_checkpoint_26.md`

No tenth file was created, renamed or modified.

## Root-cause implementation

| Root cause | Bounded implementation | Objective offline evidence |
|---|---|---|
| 133 terms were not bijectively bound to typed selectors | Added an independent literal oracle with 133 unique component IDs, value types, reader IDs, source relations, exact scope, cardinality, null rejection, stage, operator, expectation and reducer ID | Oracle validation and plan validation require equal unique oracle/component/assertion sets |
| Reported 105 selectors plus one compound atomic input were absent | Replaced the missing inventory with reader-namespaced CP-L2, CP-A2, TC2, TP2, TE2, TA2 and OR2 selectors; cross-observation inputs are also exact reader-bound selector inputs | 133/133 decisive terms present; missing/null/extra selectors fail verification |
| Runtime evidence and assertions were self-derived | Added immutable oracle version/hash, separate preparation/attempt/evidence/expected-result IDs and exact scenario/subcase/action bindings | Oracle hash `944e9f20e0bc45866891142e9af604f3ddde2b8fca02c0984b39459fca60bd35`; 108 unique values for each identity class |
| Descriptor mutations did not alter decisive evidence | Added real evidence mutations for missing, extra, duplicate, altered, stale, replayed, fabricated, cross-instance, cross-lease, wrong-version, wrong-state and wrong-count evidence | Every mutation is passed through `VerifyEvidence` and must be rejected |
| Assertion removal/weakening could survive | Every exact plan includes removal and weakening mutants for each assertion; contract validation compares against the independent oracle | Removing or weakening any decisive assertion makes the oracle/component/assertion bijection invalid |
| Multi-label scenarios shared one preparation/attempt | Expanded the 34 top-level IDs to 108 frozen subcases, each with unique preparation, attempt, evidence and expected-result IDs and action ID | Oracle uniqueness checks and action/preparation correlation |
| ACL evidence omitted inheritance/effective privilege dimensions | CP-A2 and TA2 enumerate direct ACLs with `aclexplode`, ownership, default privileges, role memberships, PUBLIC, capabilities, effective table/function privilege and administrative bypass facts | Correction 26 source contract scans all dimensions and verifier-only grants |
| Purge evidence was target-wide | TP2 binds target instance, lease, authorization, root authorization, batch, attempt and subcase; context observations join the exact authorization scope and cutoff | Source contract rejects target-wide `contextRows` in TP2 and requires scoped root/batch joins |
| Target identity was passive | Target identity now contains one immutable unique `LeaseId`, populated fail-closed from externally supplied `nexa.rev869b_lease_id`; every target v2 reader requires exact instance hash plus lease | Source contract and function predicates require both identity terms |

Controller audit remains supplementary only. No Audit-stage formula assertion is permitted. No shared signed controller document, echoed PASS label, P02/P03 sentinel, copied 1/1 acceptance pair or constant PASS result decides acceptance.

## Authoritative reader and ACL boundary

| Reader | Authority | Exact scope | Principal |
|---|---|---|---|
| CP-L2 | surviving leases, immutable events, attempts, outcomes, decisions and quarantine | instance hash + lease + lifecycle version + scenario + subcase + request + attempt + decision | control-plane verifier |
| CP-A2 | pg_catalog database/schema/relation/function/default ACLs, ownership, memberships and role capabilities | oracle version + observation stage | control-plane verifier |
| TC2 | command request, attempt, contexts, claims, outcomes and receipts | instance + lease + scenario + subcase + command + attempt | target verifier |
| TP2 | purge authorization/root/prior attempt/candidates/events and scoped contexts | instance + lease + scenario + subcase + authorization + root + batch + attempt | target verifier |
| TE2 | export authorization/batch/rows/releases | instance + lease + scenario + subcase + authorization + batch + release + as-of | target verifier |
| TA2 | target pg_catalog ACL/owner/default/membership/capability/effective projections | instance + lease + scenario + subcase + principal + object + operation + stage | target verifier |
| OR2 | immutable offline oracle mutation result stream | oracle hash/version + scenario + subcase + mutation | offline verifier |

All v2 SQL readers are `SECURITY DEFINER STABLE`, pin `search_path=pg_catalog,nexa`, require the exact verifier session principal and receive only purpose-specific EXECUTE grants. PUBLIC and all non-verifier roles remain revoked.

## Exact 34-scenario implementation matrix

Every row uses its literal frozen scenario ID. The subcase count sums to 108 and the term count sums to 133. Each term is an executable assertion; each subcase is evaluated against all 12 evidence-tampering classes.

| ID | Expected terminal/result | Subcases | Terms | Authoritative readers | Result |
|---|---|---:|---:|---|---|
| P01 | ExternalVerified | 1 | 3 | CP-A2, TA2 | IMPLEMENTED_PENDING_PRECHECK |
| P02 | PreflightDenied / exact pin mismatch | 5 | 3 | CP-A2, CP-L2 | IMPLEMENTED_PENDING_PRECHECK |
| P03 | VerificationDenied / exact catalogue ACL delta | 4 | 4 | CP-A2 | IMPLEMENTED_PENDING_PRECHECK |
| L01 | Ready | 3 | 3 | CP-L2 | IMPLEMENTED_PENDING_PRECHECK |
| L02 | Ready | 6 | 5 | CP-L2, TA2 | IMPLEMENTED_PENDING_PRECHECK |
| L03 | DropStarted / 40001 exact active-attempt object | 5 | 5 | CP-L2 | IMPLEMENTED_PENDING_PRECHECK |
| L04 | Finalized | 5 | 5 | CP-L2, TA2 | IMPLEMENTED_PENDING_PRECHECK |
| L05 | Quarantined / 42501 exact identity object | 5 | 3 | TA2, CP-L2 | IMPLEMENTED_PENDING_PRECHECK |
| R01 | Finalized | 1 | 5 | CP-L2 | IMPLEMENTED_PENDING_PRECHECK |
| R02 | RecoveryAuthorized / 42501 exact replay object | 8 | 3 | CP-L2 | IMPLEMENTED_PENDING_PRECHECK |
| R03 | Finalized | 5 | 5 | CP-L2 | IMPLEMENTED_PENDING_PRECHECK |
| C01 | Committed | 1 | 5 | TC2 | IMPLEMENTED_PENDING_PRECHECK |
| C02 | Committed | 1 | 5 | TC2 | IMPLEMENTED_PENDING_PRECHECK |
| C03 | RequestRegistered / 23505 exact replay object | 1 | 4 | TC2 | IMPLEMENTED_PENDING_PRECHECK |
| C04 | RolledBack / P0001 exact failpoint | 5 | 4 | TC2 | IMPLEMENTED_PENDING_PRECHECK |
| C05 | RolledBack | 1 | 3 | TC2 | IMPLEMENTED_PENDING_PRECHECK |
| C06 | FourExactInterruptionOutcomesReconciled | 4 | 3 | TC2 | IMPLEMENTED_PENDING_PRECHECK |
| C07 | AttemptStarted / 40001 exact active object | 1 | 4 | TC2 | IMPLEMENTED_PENDING_PRECHECK |
| C08 | AttemptStarted / 42501 exact binding object | 8 | 4 | TC2 | IMPLEMENTED_PENDING_PRECHECK |
| G01 | Denied / 42501 exact batch binding | 5 | 3 | TP2 | IMPLEMENTED_PENDING_PRECHECK |
| G02 | ZeroRows | 1 | 4 | TP2 | IMPLEMENTED_PENDING_PRECHECK |
| G03 | Succeeded | 1 | 5 | TP2 | IMPLEMENTED_PENDING_PRECHECK |
| G04 | Failed / 40001 exact candidate drift | 1 | 4 | TP2 | IMPLEMENTED_PENDING_PRECHECK |
| G05 | Failed / P0001 exact delete failpoint | 3 | 3 | TP2 | IMPLEMENTED_PENDING_PRECHECK |
| G06 | Failed / 42501 exact retry binding | 4 | 5 | TP2 | IMPLEMENTED_PENDING_PRECHECK |
| E01 | Prepared | 1 | 4 | TE2 | IMPLEMENTED_PENDING_PRECHECK |
| E02 | Prepared | 1 | 4 | TE2 | IMPLEMENTED_PENDING_PRECHECK |
| E03 | Denied / 42501 exact release sequence | 4 | 3 | TE2 | IMPLEMENTED_PENDING_PRECHECK |
| E04 | ReleaseRetrySequenceVerified | 3 | 5 | TE2 | IMPLEMENTED_PENDING_PRECHECK |
| A01 | Verified | 1 | 3 | CP-A2, TA2 | IMPLEMENTED_PENDING_PRECHECK |
| A02 | Denied / 42501 exact protected ACL object | 7 | 3 | TA2 | IMPLEMENTED_PENDING_PRECHECK |
| T01 | InUse | 1 | 4 | CP-L2, TA2 | IMPLEMENTED_PENDING_PRECHECK |
| T02 | Finalized | 1 | 5 | CP-L2 | IMPLEMENTED_PENDING_PRECHECK |
| T03 | MutationSensitive | 4 | 2 | OR2 | IMPLEMENTED_PENDING_PRECHECK |

Totals: 34 scenarios, 108 subcases, 133 decisive formula terms. Scenario discovery is not treated as behavioral PASS; database acceptance remains unavailable and unauthorized.

## Assertion and mutation evidence

- Frozen oracle: `REV869B-C26-ORACLE-v1`
- Formula version: `REV869B-C26-FORMULA-v1`
- Canonical oracle SHA-256: `944e9f20e0bc45866891142e9af604f3ddde2b8fca02c0984b39459fca60bd35`
- Each term binds component ID -> typed reader selector -> stage -> local reducer -> operator/expected result -> exactly one assertion.
- Cross-observation equality and inequality inputs are included in the exact stage selector/reader set.
- ExactlyOneTrue is evaluated over separately observed operands, not a fabricated tuple selector.
- Pristine offline corpus: 108/108 subcases accepted.
- Evidence mutation corpus: 108 subcases x 12 mutation classes = 1,296 verifier rejections.
- Structural mutation corpus: action/read removal plus per-assertion removal and weakening; every changed contract is rejected.

## Offline validation

| Validation | Result |
|---|---|
| Build `--no-restore` | PASS: 0 warnings, 0 errors |
| Correction 26 oracle/contract/mutation/SQL-hash tests | PASS: 2/2 |
| Focused REV869B non-PostgreSQL suite | PASS: 75/75 |
| Complete non-PostgreSQL suite | PASS: 460/460; seven PostgreSQL-executing classes explicitly excluded |
| PostgreSQL discovery only | PASS: exactly 34 Correction 26 scenario tests discovered, 34 unique; executed=0 |
| PowerShell 5.1 AST | PASS: 5.1.19041.6456, 24 scripts, 0 parse errors, 0 executed |
| EF migration discovery | PASS: `--no-connect`, inert `127.0.0.1:1`, 13 migrations |
| REV869A/REV869B uniqueness/order | PASS: one primary migration each, REV869B immediately follows REV869A; each has one designer companion |
| Model/snapshot and retained SQL | PASS: 2/2 |
| Offline Up SQL | PASS: 298085 UTF-8 bytes, 2535 logical lines, SHA-256 `ECEEE2ECD1A5A3E1FC4D0E227AD59029F25F7903B41EF575CA16DFF8639BA7B3` |
| Offline Down SQL | PASS: 11046 UTF-8 bytes, 226 logical lines, SHA-256 `6D9F2A83DA9ADF14A0C763C90AF69DD287805CD00E09132367B714C0B931A4ED` |
| F23-01 preserved slice | PASS: `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523` |
| ACL/owner/default/role/PUBLIC contract | PASS offline: direct, inherited, effective, owner, default, role capability, runtime/admin/export and PUBLIC dimensions present; verifier-only grants |
| Added-line secret/privacy scan | PASS: 0 secret assignments, 0 privacy terms |
| Prohibited-operation scan | PASS: 0 operational calls, 0 added mutating Npgsql commands, 0 legacy-reference mentions |
| `git diff --check` | PASS |
| Exact pre-checkpoint scope | PASS: eight implementation files; checkpoint is ninth |

PostgreSQL was not contacted. The Up/Down scripts were generated in memory through the existing no-connect `IMigrator.GenerateScript` test. No generated SQL artifact was written.

## File SHA-256 before commit

| File | SHA-256 |
|---|---|
| `tools/rev869b-control-plane-install.sql` | `586797CB94C69941E37A4563B273D67EF28B495E32E094367EE032B67071CF6B` |
| `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | `79A59C8D8053777050C31146337983186F8D3AD6A7C07F3CD73674ADCA5F150B` |
| `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs` | `9732D1DA95C49A5815755D544A4E02F6FBE2B1722C171C89492C9CB3A29EEC78` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | `720D8ECB6F5FCA19DC8A10CBBB9623E60F3FF4D9B155F2491DAC61C81C3F859D` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | `F10135460DC0582B5CB5485D37EAA1820DF2659B44CEB93BE353F8A0E40A5B07` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | `80E780AFC3A07ABA56FFC5BC62D3AB258F99068586AEB496E95519B67595129A` |
| `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | `7B4875EF83EA41A2DB95B9AAD8CFE54744BCDA27365C072DD8E8BC1C779A1C04` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection26FrozenOracle.cs` | `061B99FD3A2BA2C74E2934B3D54590E0E60A7B79E473DC3C01FD50A936843BA0` |

## External prerequisites and unexecuted gates

Still required later under separate authorization:

1. External control-plane provisioning with the frozen owner/role/login/membership/default-ACL contract.
2. Dedicated lifecycle controller and independently pinned verifier endpoints/signing identities.
3. External target provisioning must set exact `nexa.rev869b_lease_id` for migration-session identity binding.
4. Isolated target database/roles and verifier-only credentials for all 108 subcases.
5. PostgreSQL compilation and execution of all 34 top-level scenarios/108 subcases.
6. Database-observed selector, ACL, purge retry-root, durable audit/history, cleanup and mutation-rejection evidence.

These prerequisites block database acceptance and execution-helper readiness. They do not authorize execution in this correction.

## Next gate

The only next gate is a separately authorized internal adversarial source-only precheck of committed Correction 26. No Correction 27 and no PostgreSQL execution are authorized.

correction_26_source_implementation_state=COMPLETE_PENDING_PRECHECK
f23_01_state=PASS_RETAINED
f23_02_source_correction_state=IMPLEMENTED_PENDING_PRECHECK
formula_term_coverage_state=PASS
scenario_subcase_design_state=PASS
frozen_architecture_state=RETAIN
acl_boundary_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
