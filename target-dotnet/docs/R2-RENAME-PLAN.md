# R2 semantic rename plan: not executed

Review after R1 go-live on 15 October 2026. No filenames, namespaces, public types, DB objects or routes change now. Proposals use the CODE-MAP source snapshot. Semantic feature words replace historical revision labels; correction labels stay explicit where they distinguish evidence.

## Source / test file candidates

Candidate paths are file moves, not automatic public-type renames. Keep folders in the first pass except the proposed Application configuration-contract folder. Review aggregate files before selecting names; splitting responsibilities is a separate change.

| Old path | Proposed new path |
|---|---|
| `src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.Rev869A.cs` | `src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.ConfigurationFoundation.cs` |
| `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.QcPolicies.cs` | `src/SESS.NexaERP.Api/Endpoints/ControlledConfigurationEndpoints.QcPolicies.cs` |
| `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.StoreCategoryRoutes.cs` | `src/SESS.NexaERP.Api/Endpoints/ControlledConfigurationEndpoints.StoreCategoryRoutes.cs` |
| `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs` | `src/SESS.NexaERP.Api/Endpoints/ControlledConfigurationEndpoints.cs` |
| `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationReadEndpoints.cs` | `src/SESS.NexaERP.Api/Endpoints/ControlledConfigurationReadEndpoints.cs` |
| `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.Print.cs` | `src/SESS.NexaERP.Api/Endpoints/PurchaseEndpoints.Print.cs` |
| `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs` | `src/SESS.NexaERP.Api/Endpoints/PurchaseEndpoints.cs` |
| `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseReadEndpoints.cs` | `src/SESS.NexaERP.Api/Endpoints/PurchaseReadEndpoints.cs` |
| `src/SESS.NexaERP.Application/Masters/Rev869AFoundationServices.cs` | `src/SESS.NexaERP.Application/Masters/ControlledConfigurationServiceContracts.cs` |
| `src/SESS.NexaERP.Application/Purchase/Rev869BPurchaseContracts.cs` | `src/SESS.NexaERP.Application/Purchase/PurchaseContracts.cs` |
| `src/SESS.NexaERP.Application/Purchase/Rev869BPurchaseReadContracts.cs` | `src/SESS.NexaERP.Application/Purchase/PurchaseReadContracts.cs` |
| `src/SESS.NexaERP.Application/Rev869A/Rev869AContracts.cs` | `src/SESS.NexaERP.Application/ConfigurationFoundation/ConfigurationFoundationContracts.cs` |
| `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs` | `src/SESS.NexaERP.Domain/Purchase/PurchaseTransactions.cs` |
| `src/SESS.NexaERP.Infrastructure/Masters/EfRev869AFoundationServices.cs` | `src/SESS.NexaERP.Infrastructure/Masters/EfControlledConfigurationServices.cs` |
| `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869A.cs` | `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.ConfigurationFoundation.cs` |
| `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs` | `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.PurchaseTransactions.cs` |
| `src/SESS.NexaERP.Infrastructure/Persistence/Rev866SeedData.cs` | `src/SESS.NexaERP.Infrastructure/Persistence/EmployeePagePermissionSeedData.cs` |
| `src/SESS.NexaERP.Infrastructure/Persistence/Rev868C3EmployeeWorkbookData.cs` | `src/SESS.NexaERP.Infrastructure/Persistence/LegacyEmployeeDepartmentReconciliationData.cs` |
| `src/SESS.NexaERP.Infrastructure/Persistence/Rev869ASeedData.cs` | `src/SESS.NexaERP.Infrastructure/Persistence/ConfigurationFoundationSeedData.cs` |
| `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs` | `src/SESS.NexaERP.Infrastructure/Persistence/PurchaseTransactionsCommandContextAuthorizer.cs` |
| `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BSeedData.cs` | `src/SESS.NexaERP.Infrastructure/Persistence/PurchaseTransactionsSeedData.cs` |
| `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs` | `src/SESS.NexaERP.Infrastructure/Purchase/EfPurchaseService.ComparisonPo.cs` |
| `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.MaterialFollowUp.cs` | `src/SESS.NexaERP.Infrastructure/Purchase/EfPurchaseService.MaterialFollowUp.cs` |
| `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs` | `src/SESS.NexaERP.Infrastructure/Purchase/EfPurchaseService.RfqQuotation.cs` |
| `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs` | `src/SESS.NexaERP.Infrastructure/Purchase/EfPurchaseService.cs` |
| `tests/SESS.NexaERP.Tests/AppliedRev869BRetirementOwnershipMigrationTests.cs` | `tests/SESS.NexaERP.Tests/AppliedPurchaseTransactionsRetirementOwnershipMigrationTests.cs` |
| `tests/SESS.NexaERP.Tests/RetireRev869BOrdinaryDeploymentMigrationTests.cs` | `tests/SESS.NexaERP.Tests/RetirePurchaseTransactionsOrdinaryDeploymentMigrationTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev866SeedTests.cs` | `tests/SESS.NexaERP.Tests/EmployeePagePermissionsSeedTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev867C1PostgresVerificationTests.cs` | `tests/SESS.NexaERP.Tests/MasterFoundationCorrectionPostgresVerificationTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev867MasterFoundationTests.cs` | `tests/SESS.NexaERP.Tests/MasterFoundationTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev867VerificationScriptTests.cs` | `tests/SESS.NexaERP.Tests/MasterFoundationVerificationScriptTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev868C1PostgresWorkflowVerificationTests.cs` | `tests/SESS.NexaERP.Tests/PurchaseRequisitionPostgresWorkflowVerificationTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev868C1PreparationTests.cs` | `tests/SESS.NexaERP.Tests/PurchaseRequisitionVerificationPreparationTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev868C3EmployeeWorkbookDataTests.cs` | `tests/SESS.NexaERP.Tests/LegacyEmployeeDepartmentReconciliationDataTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev868C3ImplementationTests.cs` | `tests/SESS.NexaERP.Tests/EmployeeDepartmentReconciliationImplementationTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev868C3LegacyDepartmentCorrectionTests.cs` | `tests/SESS.NexaERP.Tests/EmployeeDepartmentReconciliationLegacyDepartmentCorrectionTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev868C3PostgreSqlWorkflowVerificationTests.cs` | `tests/SESS.NexaERP.Tests/EmployeeDepartmentReconciliationPostgreSqlWorkflowVerificationTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev868OvernightRemediationTests.cs` | `tests/SESS.NexaERP.Tests/PurchaseRequisitionFoundationOvernightRemediationTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev868PurchaseRequisitionTests.cs` | `tests/SESS.NexaERP.Tests/PurchaseRequisitionWorkflowTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev869AFoundationTests.cs` | `tests/SESS.NexaERP.Tests/ConfigurationFoundationTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev869AIsolatedDatabasePreparationHelperTests.cs` | `tests/SESS.NexaERP.Tests/ConfigurationFoundationIsolatedDatabasePreparationHelperTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev869AIsolatedExecutionHelperTests.cs` | `tests/SESS.NexaERP.Tests/ConfigurationFoundationIsolatedExecutionHelperTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev869ASourceCorrectionTests.cs` | `tests/SESS.NexaERP.Tests/ConfigurationFoundationSourceCorrectionTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev869BPurchaseBehaviorTests.cs` | `tests/SESS.NexaERP.Tests/PurchaseBehaviorTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs` | `tests/SESS.NexaERP.Tests/PurchaseCorrectionTests.cs` |
| `tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs` | `tests/SESS.NexaERP.Tests/PurchaseFoundationTests.cs` |

## Sequence / acceptance

1. TD and owners approve names and scope after go-live. Baseline a green SHA and create an R2 branch. Keep behavior changes separate.
2. Rename application/domain/service/endpoint/seed files in small commits per module. If types/namespaces change, inventory and update partial declarations, callers, project includes, architecture tests, docs and manifests together. File-only renames need not change public symbols.
3. Preserve HTTP paths, page keys, wire fields, table/schema/function names, migration IDs, embedded-resource names and command names. Runtime/database renaming is outside this plan.
4. Search old/new names across source, tests, projects, docs, tools and packaging; check case-sensitive consumers. Refresh CODE-MAP, retain an old-to-new index and use `git log --follow`.
5. Run focused module tests and the fast suite; required Debug/Release nightly and witness gates precede release integration. Tool moves require caller/packaging verification in disposable environments.

## Historical exceptions / operational tool candidates

| Old path / family | Proposal |
|---|---|
| `Persistence/Migrations/*Rev869B*.cs` and revision-named migration SQL helpers | Keep names and contents. Merged migration dependencies and security history are excluded. |
| `outputs/archive/ef_nexa_migration_history_pre_advance/` and dated checkpoint reports | Keep historical names and bodies. Fixture paths remain stable until a separate reviewed consumer update. |
| `database/postgresql/rev862_phase1_foundation_idempotent.sql` | Candidate `database/postgresql/phase1-foundation-idempotent.sql`; inventory installer/docs consumers and determine whether it remains historical input. |
| `database/postgresql/rev865_phase1_authorization_seed_idempotent.sql` | Candidate `database/postgresql/phase1-authorization-seed-idempotent.sql`, subject to the same review. |
| `database/postgresql/rev866_employee_permission_matrix_idempotent.sql` | Candidate `database/postgresql/employee-page-permission-matrix-idempotent.sql`, subject to the same review. |
| `tools/apply-rev869a-isolated-foundation-secure.ps1` | Candidate `tools/apply-configuration-foundation-isolated-secure.ps1`; update helper/test/doc consumers together. |
| `tools/prepare-rev869a-isolated-database-secure.ps1` | Candidate `tools/prepare-configuration-foundation-isolated-database-secure.ps1`. |
| `tools/manage-rev869b-control-plane-secure.ps1` | Keep historical tooling pending retirement review; not normal R1 setup. |
| Other `tools/*rev86*` apply/rollback/verification helpers | Keep until evidence/caller review is complete; use a semantic index instead of bulk renaming executable operational scripts. |

No cleanup/deletion is bundled with renaming. See [root clutter review](ROOT-CLUTTER-REVIEW.md) for separate approval.
