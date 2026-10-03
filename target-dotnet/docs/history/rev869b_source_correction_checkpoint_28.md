# REV869B source Correction checkpoint 28

## Decision and bounded lineage

Correction 28 is implemented as a source-only change inside the authorized ten-file boundary. This checkpoint is implementation evidence, not an internal or independent review, not database acceptance, and not a production-readiness declaration.

| Gate | Evidence | Result |
|---|---|---|
| Authorized starting HEAD | `b7ad8c9517274b98cc44a02c5a640e526c397845` | PASS |
| Required parent | `16528851cd6971a286f2c1705e80ce0a3d061b3e` | PASS |
| Reconciliation | `outputs/rev869b_correction27_failure_reconciliation.md` | PASS: sole file in starting commit |
| Reconciliation SHA-256 | `EBB8EFB06F2116DABAA76D62BA5376AEDCC8ED11F77E6081BEAD4493631F7DC8` | PASS |
| Reconciliation inventory | 7 findings / 133 terms / 34 scenarios / 108 subcases | PASS |
| Entry target status | clean | PASS |
| Ending commit | the single controlled commit containing this checkpoint; reported in the handoff because a commit cannot contain its own hash | PENDING COMMIT |

## Exact exhaustive ten-file scope

1. `tools/rev869b-control-plane-install.sql`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
3. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCorrection26FrozenOracle.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BCorrection28IndependentEvidenceFixtures.cs`
10. `outputs/rev869b_source_correction_checkpoint_28.md`

No primary migration class/designer, model snapshot, production service, endpoint, authorizer, provisioning helper, prior report, or legacy-reference file is changed.

## Seven-finding correction matrix

| Finding | Correction 28 implementation evidence |
|---|---|
| `C27F-01` | Six readers are versioned to `CP-L4`, `CP-A4`, `TC4`, `TP4`, `TE4`, and `TA4`; exact subcase scope is added to company/instance/lease/version/operation/execution/stage scope; caller-echo identifiers and literal zero projections are replaced by selected relation/catalog reductions; missing/null/unrequested/duplicated facts fail the exact-set contract. |
| `C27F-02` | Before, after, durable, audit, and cleanup observations have distinct ordered identities and transaction boundaries. No snapshot object is reused between stages. |
| `C27F-03` | `Rev869BCorrection28IndependentEvidenceFixtures` materializes exactly 108 subcase-keyed raw, action, history, provenance, preparation, attempt, observation, envelope, and expected-result identities/hashes. |
| `C27F-04` | Actual action facts are authored in the independent fixture catalog and consumed by the adapter; the frozen oracle is referenced only after actual adaptation for comparison. |
| `C27F-05` | The broad exception fallback is removed. Twenty mutations per subcase return structured mutation ID, boundary, component, expected/actual rejection code, evaluation stage, killed/survived state, and evidence hash; unexpected exceptions escape and fail. |
| `C27F-06` | `ObserveAsync` routes `OR3` before any Npgsql construction. `DispatchLocalOr3` consumes immutable mutation-run records, rejects wrong operation/exact-set/code/hash identity, and derives killed/surviving counts. |
| `C27F-07` | Executable contracts call the adapter/verifier, require all readers/mappings/assertions, prove 2,160 structured rejections, assertion removal, independent-fixture separation, positive OR3 routing, and wrong-operation rejection. |

## 133-term mapping and scenario/subcase inventory

The frozen v4 mapping is byte-pinned by computed SHA-256 `bc0d3a4b292553041a6e1b6bf756ca1c04a84d0b76fc3062092e3c54c9b5c0ca`. Its complete 133 rows declare component, selector, type, reader, source, scope, cardinality, null semantics, stage, operator, expected value, reducer, raw path, and mapping ID.

| Reader | Terms |
|---|---:|
| `CP-L4` | 36 |
| `CP-A4` | 8 |
| `TC4` | 32 |
| `TP4` | 24 |
| `TE4` | 16 |
| `TA4` | 15 |
| `OR3` | 2 |
| **Total** | **133** |

The 34-scenario subcase counts remain: P01 1, P02 5, P03 4, L01 3, L02 6, L03 5, L04 5, L05 5, R01 1, R02 8, R03 5, C01 1, C02 1, C03 1, C04 5, C05 1, C06 4, C07 1, C08 8, G01 5, G02 1, G03 1, G04 1, G05 3, G06 4, E01 1, E02 1, E03 4, E04 3, A01 1, A02 7, T01 1, T02 1, T03 4: exactly 108.

Inventory contracts require 108 unique observation identities, 108 unique envelope identities, 108 raw hashes, 108 action hashes, 108 history hashes, and 108 provenance hashes. Each subcase also has five distinct temporal observation IDs.

## Independent action, observation, mutation, and OR3 evidence

- Actual action receipts contain independently materialized reached state, affected-row count, terminal state, SQLSTATE, error code, database object, action identity, and action-fixture hash.
- Before and after are separately built and adapted; durable history and independent audit are separate observations; every raw scope includes exact subcase identity.
- The 20 frozen mutation kinds execute for every 108 subcases: `20 x 108 = 2,160`. Every result is killed at the named boundary and has identical expected and actual rejection codes. Assertion tamper and removal checks execute separately for every decisive formula component.
- OR3 positive routing is executable and occurs before `new NpgsqlConnection`. Wrong reader/stage/cardinality produces exact `OR3_WRONG_OPERATION`; altered record codes produce exact `OR3_RECORD_EXACT_SET`.

## ACL, purge, and enterprise-scale evidence

- Security-definer readers retain fixed `pg_catalog,nexa` search paths. PUBLIC execute is revoked and only verifier roles receive the v4 reader grants. Direct ACL, inherited membership, aggregate roles, ownership, default privileges, PUBLIC, effective table/function privilege, and administrative bypass projections remain present.
- Purge scope binds organization, target instance, lease/version, authorization, execution, retry root, batch, attempt, scenario execution, stage, and subcase. Execution equals the exact purge attempt. Eligibility is ordered and bounded by the authorization maximum; export rows remain capped at 1,000.
- Request/version, attempt/version, and organization/opened-token indexes remain present. Reader CTEs are scoped through exact identities and do not perform full customer, vendor, user, item, machine, project, master, or multi-company ledger loads.
- Frozen external provisioning, dedicated lifecycle controller, surviving control plane, target-local transactional ledgers, and purchase workflows are retained. ACL ownership is not widened.

## Offline validation

| Validation | Result |
|---|---|
| Build | PASS: 0 warnings / 0 errors |
| Correction 28 source-contract, dispatch, fixture, mutation tier | PASS: 16/16 |
| Focused REV869B non-PostgreSQL tier | PASS: 76/76 |
| Complete non-PostgreSQL tier | PASS: 450/450 |
| Scenario discovery | PASS: exactly 34 top-level `[Fact]` tests; no PostgreSQL execution |
| Inventory | PASS: 7 / 133 / 34 / 108; 108 observation IDs; 108 envelope IDs |
| Structured mutations | PASS: 2,160/2,160 plus assertion tamper/removal |
| PostgreSQL connections / commands / executions | `0 / 0 / 0` |
| PowerShell 5.1 AST | PASS: 24 scripts / 0 errors; version 5.1.19041.6456 |
| EF discovery | PASS: `--no-connect`, inert loopback port 1, 13 migrations |
| REV869A/REV869B | PASS: unique and adjacent; REV869B immediately follows REV869A |
| Model/snapshot and retained SQL | PASS: 2/2 explicit no-connect contracts |
| Offline Up SQL | PASS: 324,914 UTF-8 bytes / 2,635 lines / `39B067351894AB5732B6DF9C6348B04D708780AFAA18E073F8E6594D07FAF213` |
| Offline Down SQL | PASS: 11,720 UTF-8 bytes / 231 lines / `FC4BCB671501D601041FCED25D6053545BE9F38CF1D9982006953F47229E0AE4` |
| F23-01 | PASS_RETAINED: 11,001 bytes / `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523` |
| Secret / privacy / prohibited-operation scan | PASS: 0 / 0 / 0 added-line matches |
| ACL / enterprise-scale bounded-query scan | PASS |
| `git diff --check` | PASS |
| Exact scope | PASS: exactly ten authorized files after this checkpoint |

## Implementation file hashes

| File | SHA-256 |
|---|---|
| `tools/rev869b-control-plane-install.sql` | `9D9ECD0559B938D51A84538E6AD7D423FDAFB22FD61AC50A37884718DE2291A8` |
| `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | `C83A8371FEAD3E04171C3AAD4501945130086AF663F26E0A3ACA3686F33800BB` |
| `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs` | `8A3025FF1822E71F1484A202C77D03B3AE804962FA9DDC355517453F1A24CB47` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | `BFB24527DB8D881FF077045283989F5C8040AC90F91C80D1EBB0D570BE4594FE` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | `66C9E7EC4D9F63493BF0A30B0C91B8F77F6A1969D13E8AF864982879C92187AB` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | `609DCF55D729D527D3187BE2C55FEFB2BA66D5854D5E6D5CB7EBEDC0D99629D4` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection26FrozenOracle.cs` | `E13A9AA768C114817910B27992D6C768C982DC3C5275728CA1521E9A9EB2D311` |
| `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | `C02C9FC9867870C3FB642EF8A6AE9CC52D1AE91E8E58CD75B8311A6608FE0AE6` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection28IndependentEvidenceFixtures.cs` | `F0021A0EA5DD4E8D06AA5E1399B2C3AD7543F08CC5DB019753B644FB7F33FF97` |

## External prerequisites and mandatory successor gate

No PostgreSQL connection, provisioning, migration apply/remove, lifecycle, purge, recovery, quarantine, export, production operation, or legacy-reference access was performed. A named production trusted-adapter owner/interface, isolated control/target databases and roles, TLS/signing material, provisioned instances, and all 108 database-backed executions remain external prerequisites.

The mandatory next gate is a separately authorized internal adversarial source-only precheck of the committed Correction 28. No internal or independent review is performed by this implementation goal.

correction_28_source_implementation_state=COMPLETE_PENDING_PRECHECK

f23_01_state=PASS_RETAINED

f23_02_source_correction_state=IMPLEMENTED_PENDING_PRECHECK

independent_fixture_state=PASS

fresh_observation_binding_state=PASS

live_or3_dispatch_state=PASS

mutation_rejection_design_state=PASS

enterprise_scale_compatibility_state=PASS

trusted_adapter_production_ownership_state=EXTERNAL_PENDING

frozen_architecture_state=RETAIN

acl_boundary_state=RETAIN

external_prerequisite_blocking_state=YES

rev869b_source_safety_state=FAIL

rev869b_execution_helper_readiness_state=FAIL

postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
