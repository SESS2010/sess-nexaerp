# REV869A Source Implementation Checkpoint

Date: 2026-08-10
Baseline: `9367c1ff148fa204af5a38ed6ba2c16892e91893`
Workspace: `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet`
Scope: source, one logical EF migration, offline tests, and this report only

## Result

REV869A implements the approved identity, master-data, operational-scope, permission, audit, and REV868/REV868C3 regression-protection foundation. It does not implement REV869B or any transactional RFQ, quotation, comparison, PO, GRN/QC, inventory-ledger, frontend, Customer Master, or Project Master workflow.

Migration ID: `20260810120000_Rev869AIdentityMasterScopeFoundation`
Logical migration name: `Rev869AIdentityMasterScopeFoundation`

## Approved policies implemented

1. Identity uses normalized Issuer + Subject. Filtered unique constraints permit only one active mapping for that identity and one active HUMAN identity per employee and organization. Resolution requires exactly one effective mapping to an active, login-enabled employee and never queries email or name.
2. Legacy development/test claims remain temporarily compatible. Production OIDC activation is not enabled and remains blocked on verified real-Issuer/Subject acceptance.
3. Fixed employee-name/code workflow fallbacks were removed. PR workflow seed steps now use configurable roles or effective department mappings. Missing, ambiguous, inactive, unauthorized, or self mappings fail closed and denied attempts are audited.
4. Operational scope is modeled by organization, employee, department, warehouse, and Rack/Bin. API/data authorization intersects permission and record scope; TD/MD cross-scope requires the explicit role and an effective `AllowsPrivilegedCrossScope` grant.
5. Warehouse -> Rack/Bin is canonical. `LocationKey` is derived from their IDs; no Location Master was added, and employee work location was not conflated with Rack/Bin.
6. Items reference one Base UOM. UOMs carry measurement dimension and six-decimal quantity precision. Effective-dated conversion versions enforce positive factors, distinct UOMs, compatible dimensions, and immutability after first use.
7. Effective-dated tax configuration supports jurisdiction, HSN/SAC, supply type/state context, vendor registration, GST components, exemption, reverse charge, ISO currency code, and configurable rounding; no tax percentage is hard-coded.
8. Commercial calculations retain taxable, tax, freight/other charges, discount, rounding, and separately calculated TotalPayableValue/ApprovalValue evidence.
9. Vendor creation remains Purchase-owned; Accounts verification and policy-driven final approval are enforced. SESS configures `VENDOR_FINAL_APPROVER=MANAGING_DIRECTOR` without an employee seed. Eligibility requires Active + Approved + commercially verified + effective. Controlled GST/PAN/bank/commercial changes reset verification/approval and preserve before/after history.
10. `INVENTORY_VALUATION_METHOD=WEIGHTED_AVERAGE` is configuration, allowing later FIFO without rewriting historical entries.
11. Conditions are exactly AVAILABLE, QC_HOLD, REJECTED, QUARANTINE, RETURN_TO_VENDOR, and SCRAP. Only AVAILABLE is reservable/issuable.
12. Effective item/category QC policies retain parameter, measurement UOM, limits, method, and sample size. Missing/inactive policy resolves to QC_HOLD.
13. No Customer or Project master was created. Existing controlled PR demand-source references remain in place; Project consumption remains blocked.
14. The nine approved role codes are permission-group codes, not login IDs. Eight protected configuration page keys, 72 approved-role permissions, and two Accounts verification permissions are source-owned seeds.

The design remains configuration-driven and organization-scoped where safe, with stable policy codes, effective dates, UTC audit timestamps, ISO currency codes, localization-ready labels, tax-jurisdiction codes, backend authorization, append-only history creation paths, and restrictive foreign-key deletion behavior. No sweeping multi-tenancy rewrite was made.

## Source boundary and changed files

API and security:

- `src/SESS.NexaERP.Api/Endpoints/InventoryEndpoints.cs`
- `src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.cs`
- `src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.Rev869A.cs`
- `src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionEndpointHelpers.cs`
- `src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionEndpoints.cs`
- `src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionSupport.cs`
- `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs`
- `src/SESS.NexaERP.Api/Program.cs`
- `src/SESS.NexaERP.Api/Security/AuthorizationPolicies.cs`
- `src/SESS.NexaERP.Api/Security/ClaimsCurrentUser.cs`

Application contracts:

- `src/SESS.NexaERP.Application/Authorization/IRecordScopeAuthorizer.cs`
- `src/SESS.NexaERP.Application/Common/ICurrentUser.cs`
- `src/SESS.NexaERP.Application/Identity/IEmployeeIdentityResolver.cs`
- `src/SESS.NexaERP.Application/Masters/Rev869AFoundationServices.cs`
- `src/SESS.NexaERP.Application/Rev869A/Rev869AContracts.cs`

Domain:

- `src/SESS.NexaERP.Domain/Authorization/EmployeeOperationalScope.cs`
- `src/SESS.NexaERP.Domain/Identity/EmployeeIdentityMapping.cs`
- `src/SESS.NexaERP.Domain/Inventory/Item.cs`
- `src/SESS.NexaERP.Domain/Inventory/QcInspectionPolicy.cs`
- `src/SESS.NexaERP.Domain/Inventory/WarehouseConditionLocation.cs`
- `src/SESS.NexaERP.Domain/Masters/MasterSupport.cs`
- `src/SESS.NexaERP.Domain/Masters/TaxGstSetting.cs`
- `src/SESS.NexaERP.Domain/Masters/UomConversion.cs`
- `src/SESS.NexaERP.Domain/Masters/Vendor.cs`
- `src/SESS.NexaERP.Domain/Masters/VendorQualification.cs`
- `src/SESS.NexaERP.Domain/Purchase/PurchaseRequisition.cs`

Infrastructure and migration:

- `src/SESS.NexaERP.Infrastructure/Authorization/EfRecordScopeAuthorizer.cs`
- `src/SESS.NexaERP.Infrastructure/DependencyInjection.cs`
- `src/SESS.NexaERP.Infrastructure/Identity/EfEmployeeIdentityResolver.cs`
- `src/SESS.NexaERP.Infrastructure/Masters/EfRev869AFoundationServices.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869A.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Rev869ASeedData.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260810120000_Rev869AIdentityMasterScopeFoundation.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260810120000_Rev869AIdentityMasterScopeFoundation.Designer.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`

Tests and evidence:

- `tests/SESS.NexaERP.Tests/Rev868C3ImplementationTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869AFoundationTests.cs`
- `outputs/rev869a_source_implementation_checkpoint.md`

Total intended files: 39.

## Migration Up and Down review

`Up` is atomic under EF's default PostgreSQL transaction and contains no `suppressTransaction` operation. Generated source-only SQL starts with `START TRANSACTION`, has 472 lines, creates nine tables, adds the Base UOM/UOM/vendor foundation columns, restrictive foreign keys, check constraints, filtered/unique indexes, eight pages, five missing roles, 72 approved-role page permissions, two Accounts verification permissions, and two organization policies. It contains no SESS employee-code seed.

The migration was deliberately metadata-stamped `20260810120000` so it follows committed `20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection`. Scaffold drift for already committed REV868C3 employee/department changes was removed from the REV869A operation list while retained correctly in the current model snapshot.

`Down` is source-generatable, transaction wrapped, has 308 lines, drops the nine REV869A tables, removes only REV869A-owned permission/page/role seeds, and removes only the new Base UOM/UOM/vendor columns. UUID metadata is explicit for rollback seed keys. It does not delete unrelated business, status, approval, audit, employee-department, or REV868 history records. Newly captured REV869A configuration history is necessarily removed with its new table on a deliberate migration rollback; pre-existing history remains untouched.

No pre-existing value is overwritten by `Up`; new required legacy-row columns use safe defaults and nullable links. Therefore no pre-mutation value backup table is required for exact rollback.

## Validation evidence

- `dotnet build SESS.NexaERP.slnx --no-restore`: passed, 0 warnings, 0 errors.
- Focused `Rev869AFoundationTests`: 9 passed, 0 failed, 0 skipped.
- All tests with `FullyQualifiedName!~Postgres`: 221 passed, 0 failed, 0 skipped.
- REV868C3 approval-boundary and migration regression tests remain passing; runtime expectations now confirm no fixed approver employee code.
- EF migration discovery used `dotnet ef migrations list --no-connect`; REV869A was discovered exactly once immediately after the REV868C3 corrective migration.
- Source-only EF Up and Down scripts generated successfully using a non-routable localhost port-1 metadata connection string; no connection was attempted.
- Up/Down review: 9 creates in Up, 9 drops in Down, current migration history row added/removed, no REV868 history target, and no employee-specific seed.
- Secret/privacy scan: no private keys, API keys, client secrets, passwords, or embedded connection strings in intended files.
- Protected-scope scan: no `sess_nexaerp`, REV861, database create/drop, connection opening, or migration-application call in intended files.
- `git diff --check`: passed.

## Remaining blockers and later acceptance

- PostgreSQL migration Up/Down, constraint/index behavior, exact legacy-row backfill, query-plan, and rollback acceptance are not claimed and must be performed later only in an approved isolated database with an approved backup.
- Real OIDC Issuer + Subject mapping, inactive/duplicate identity enforcement through the deployed provider, claim normalization, and production activation remain blocked on the approved production/OIDC exercise.
- Existing development/test claim compatibility is intentionally temporary. Production must fail closed unless `EmployeeId` originates from a verified identity mapping.
- Project consumption remains blocked until the Project Master system of record and controlled reference contract are approved.
- Historical UOM dimensions/Base UOMs, warehouse/RackBin condition mappings, tax rules, and vendor commercial verification require approved data-cleansing/backfill decisions before database rollout.
- REV869A supplies configuration APIs and permission enforcement but intentionally supplies no frontend. UI acceptance is deferred to REV869F.
- RFQ, vendor quotation, comparison, PO, follow-up, GRN/QC transactions, inventory ledger transactions, issue/return, aging, and Project consumption are outside this checkpoint.

## Safety attestation

No PostgreSQL command, database helper, migration application, database backup/restore, main database access, `sess_nexaerp` access, REV861 change, production/OIDC activation, frontend implementation, or REV869B work occurred. This checkpoint makes no database acceptance claim.
