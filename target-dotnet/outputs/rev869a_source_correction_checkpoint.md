# REV869A source correction checkpoint

Date: 2026-08-10
Baseline: `a6c2913c9d35cd6af1caa393d33163ed819848ff`
Migration: `20260810120000_Rev869AIdentityMasterScopeFoundation`
Scope: source, the existing REV869A migration/model snapshot, and offline tests only.

## Result

The identified source blockers are corrected. The solution builds with zero errors and zero warnings, 252/252 non-PostgreSQL tests pass, the 17 focused REV869A tests pass, EF discovers the 11 accepted prerequisite migrations followed by REV869A exactly once, and EF reports no pending model changes.

No PostgreSQL connection, helper, migration apply/remove, database create/drop/restore/backup, protected database, REV861, production, frontend, or REV869B operation occurred. Database acceptance is not claimed.

## Corrected behavior

- Authenticated current-user construction consumes only the middleware result of exact OIDC `Issuer + Subject` resolution. Missing, zero, duplicate, inactive, or disabled employee mappings fail closed. Email, display name, name identifier, and employee-code identity linkage are absent.
- Active `Issuer + Subject` is globally unique; the employee-side active HUMAN mapping remains organization-scoped.
- Every page-permission-protected employee endpoint first requires authenticated `EmployeeId`, organization, and an active operational scope through `IRecordScopeAuthorizer`. Missing identity returns 401; missing organization/scope returns 403.
- PR list, detail, actions, stock check, history, approvals, reservations, and PendingRFQ handoffs use fail-closed organization/department/warehouse/owner scope. Direct record queries return no row outside scope.
- Fixed employee-code/login comparison was removed from manager resolution. Actor resolution requires `EmployeeId`; missing, inactive, unauthorized, ambiguous, or self mappings fail closed.
- Warehouse-condition records require a physical Warehouse and Rack/Bin. Composite database integrity proves that the Rack/Bin belongs to the Warehouse.
- Reservation stock locations require an active, effective `AVAILABLE` warehouse/RackBin mapping. Missing, inactive, expired/future, `QC_HOLD`, `REJECTED`, `QUARANTINE`, `RETURN_TO_VENDOR`, or `SCRAP` locations are excluded. No issue transaction was introduced; future issue processing must use the same condition mapping.
- Existing item Base UOM is deterministically copied only from the item’s existing `UomId`. Any null source mapping raises a migration readiness exception before `BaseUomId` becomes NOT NULL. No invented UOM is seeded.
- GST selection requires organization, jurisdiction, HSN/SAC, supplier state, place-of-supply state, vendor registration, transaction date, and taxable value. State equality derives intrastate CGST+SGST; inequality derives interstate IGST. Missing or multiple effective rules fail closed.
- Effective configuration create paths reject overlapping periods. Seven nullable effective-date unique indexes use PostgreSQL `NULLS NOT DISTINCT` semantics.
- The eight all-false REV869A `DEPARTMENT_MANAGER` permission rows are absent. Previously accepted Department Manager permission rows are not updated; absence remains denial.
- Controlled configuration history has a database `BEFORE UPDATE OR DELETE` rejection trigger. Effective configuration records reject hard delete and field rewriting; only the first valid close of an open row is allowed, followed by a new version. Used UOM conversions are additionally immutable.

## Exact new tables and columns

All nine tables use `Id uuid NOT NULL` as PK plus `CreatedAt timestamptz NOT NULL`, `CreatedBy text NOT NULL`, `UpdatedAt timestamptz NULL`, `UpdatedBy text NULL`, and concurrency `Version bigint NOT NULL`; none of those common columns has a database default.

| Table | Purpose | Additional columns (`type`, nullability) |
|---|---|---|
| `nexa.controlled_configuration_histories` | Immutable configuration evidence | `OrganizationId varchar(100) NOT NULL`, `EntityType varchar(100) NOT NULL`, `EntityId uuid NOT NULL`, `Action varchar(100) NOT NULL`, `BeforeJson jsonb NULL`, `AfterJson jsonb NULL`, `ActorLoginId varchar(256) NOT NULL`, `ActorRoleCode varchar(100) NOT NULL`, `Remarks varchar(2000) NOT NULL`, `CorrelationId varchar(200) NOT NULL` |
| `nexa.employee_identity_mappings` | Effective exact OIDC identity mapping | `OrganizationId varchar(100) NOT NULL`, `Issuer varchar(500) NOT NULL`, `Subject varchar(500) NOT NULL`, `EmployeeId uuid NOT NULL`, `IdentityType varchar(20) NOT NULL`, `EffectiveFrom date NOT NULL`, `EffectiveTo date NULL`, `IsActive boolean NOT NULL` |
| `nexa.employee_operational_scopes` | Department/warehouse/RackBin/owner scope | `OrganizationId varchar(100) NOT NULL`, `EmployeeId uuid NOT NULL`, `DepartmentId uuid NULL`, `WarehouseId uuid NULL`, `RackBinId uuid NULL`, `OwnRecordsOnly boolean NOT NULL`, `AllowsPrivilegedCrossScope boolean NOT NULL`, `EffectiveFrom date NOT NULL`, `EffectiveTo date NULL`, `IsActive boolean NOT NULL`, `Remarks varchar(1000) NOT NULL` |
| `nexa.organization_policies` | Effective organization policy | `OrganizationId varchar(100) NOT NULL`, `PolicyCode varchar(100) NOT NULL`, `PolicyValue varchar(200) NOT NULL`, `EffectiveFrom date NOT NULL`, `EffectiveTo date NULL`, `IsActive boolean NOT NULL` |
| `nexa.qc_inspection_policies` | Item/category inspection policy | `OrganizationId varchar(100) NOT NULL`, `ItemId uuid NULL`, `ItemCategoryId uuid NULL`, `ParameterCode varchar(100) NOT NULL`, `MeasurementUomId uuid NOT NULL`, `LowerLimit numeric(24,6) NULL`, `UpperLimit numeric(24,6) NULL`, `InspectionMethod varchar(200) NOT NULL`, `SampleSize integer NOT NULL`, `EffectiveFrom date NOT NULL`, `EffectiveTo date NULL`, `ApprovalStatus varchar(50) NOT NULL`, `IsActive boolean NOT NULL` |
| `nexa.tax_gst_settings` | State-aware effective GST rules | `OrganizationId varchar(100) NOT NULL`, `JurisdictionCode varchar(30) NOT NULL`, `HsnSacCode varchar(30) NOT NULL`, `SupplierStateCode varchar(10) NOT NULL`, `PlaceOfSupplyStateCode varchar(10) NOT NULL`, `SupplyType varchar(30) NOT NULL`, `VendorRegistrationType varchar(30) NOT NULL`, `GstRate/CgstRate/SgstRate/IgstRate/CessRate numeric(9,6) NOT NULL`, `IsExempt boolean NOT NULL`, `IsReverseCharge boolean NOT NULL`, `CurrencyCode varchar(3) NOT NULL`, `RoundingScale integer NOT NULL`, `EffectiveFrom date NOT NULL`, `EffectiveTo date NULL`, `ApprovalStatus varchar(50) NOT NULL`, `IsActive boolean NOT NULL` |
| `nexa.uom_conversions` | Effective controlled UOM conversions | `OrganizationId varchar(100) NOT NULL`, `FromUomId uuid NOT NULL`, `ToUomId uuid NOT NULL`, `MeasurementDimension varchar(50) NOT NULL`, `ConversionFactor numeric(24,12) NOT NULL`, `QuantityPrecision integer NOT NULL`, `EffectiveFrom date NOT NULL`, `EffectiveTo date NULL`, `ApprovalStatus varchar(50) NOT NULL`, `IsActive boolean NOT NULL`, `FirstUsedAt timestamptz NULL` |
| `nexa.vendor_qualifications` | Effective vendor qualification | `OrganizationId varchar(100) NOT NULL`, `VendorId uuid NOT NULL`, `ItemCategoryId uuid NULL`, `QualificationCode varchar(100) NOT NULL`, `EffectiveFrom date NOT NULL`, `EffectiveTo date NULL`, `VerificationStatus varchar(50) NOT NULL`, `ApprovalStatus varchar(50) NOT NULL`, `IsActive boolean NOT NULL` |
| `nexa.warehouse_condition_locations` | Physical inventory-condition mapping | `OrganizationId varchar(100) NOT NULL`, `WarehouseId uuid NOT NULL`, `RackBinId uuid NOT NULL`, `ConditionCode varchar(30) NOT NULL`, `EffectiveFrom date NOT NULL`, `EffectiveTo date NULL`, `IsActive boolean NOT NULL` |

## Existing-table alterations and backup boundary

- Before any alteration, Up creates exact migration-owned copies: `nexa.rev869a_items_prechange_backup`, `nexa.rev869a_uoms_prechange_backup`, and `nexa.rev869a_vendors_prechange_backup`.
- `vendors`: adds `CommercialVerificationStatus varchar(50) NOT NULL DEFAULT 'Draft'`, `CommercialVerifiedAt timestamptz NULL`, `CommercialVerifiedBy text NULL`, `EffectiveFrom date NOT NULL`, `EffectiveTo date NULL`, and `RequiresReverification boolean NOT NULL DEFAULT FALSE`.
- `uoms`: adds `MeasurementDimension varchar(50) NOT NULL` (legacy rows remain unclassified; the temporary empty-string add default is dropped) and `QuantityPrecision integer NOT NULL DEFAULT 6`.
- `items`: adds nullable `BaseUomId uuid`, fails if any existing `UomId` is null, copies `UomId` exactly, sets `BaseUomId NOT NULL`, adds `IX_items_BaseUomId`, and adds restrictive `FK_items_uoms_BaseUomId`.
- `rack_bins`: adds alternate unique key `AK_rack_bins_WarehouseId_Id` on `(WarehouseId, Id)` for composite ownership FKs.
- No pre-existing business/audit column is updated. Down removes only REV869A additions and drops the three backup tables last; the exact pre-REV869A rows and columns remain unchanged.

## Keys, foreign keys, indexes, and checks

Each new table has `PK_<table>` on `Id`. All FKs are `ON DELETE RESTRICT`:

- Identity: `EmployeeId -> employees.Id`.
- Scope: `DepartmentId -> departments.Id`, `EmployeeId -> employees.Id`, `WarehouseId -> warehouses.Id`, and composite `(WarehouseId,RackBinId) -> rack_bins(WarehouseId,Id)`.
- QC: `ItemCategoryId -> item_categories.Id`, `ItemId -> items.Id`, `MeasurementUomId -> uoms.Id`.
- UOM conversion: `FromUomId` and `ToUomId -> uoms.Id`.
- Vendor qualification: `ItemCategoryId -> item_categories.Id`, `VendorId -> vendors.Id`.
- Warehouse condition: `WarehouseId -> warehouses.Id` and composite `(WarehouseId,RackBinId) -> rack_bins(WarehouseId,Id)`.
- Existing item: `BaseUomId -> uoms.Id`.

Unique indexes:

- Identity: global active `(Issuer,Subject,IsActive)` and organization-scoped active HUMAN `(OrganizationId,EmployeeId,IdentityType,IsActive)`.
- Null-safe effective indexes: scope `(OrganizationId,EmployeeId,DepartmentId,WarehouseId,RackBinId,OwnRecordsOnly,EffectiveFrom,EffectiveTo)`; policy `(OrganizationId,PolicyCode,EffectiveFrom,EffectiveTo)`; QC `(OrganizationId,ItemId,ItemCategoryId,ParameterCode,EffectiveFrom,EffectiveTo)`; GST `(OrganizationId,JurisdictionCode,HsnSacCode,SupplierStateCode,PlaceOfSupplyStateCode,VendorRegistrationType,EffectiveFrom,EffectiveTo)`; UOM conversion `(OrganizationId,FromUomId,ToUomId,EffectiveFrom,EffectiveTo)`; vendor qualification `(OrganizationId,VendorId,ItemCategoryId,QualificationCode,EffectiveFrom,EffectiveTo)`; warehouse condition `(OrganizationId,WarehouseId,RackBinId,ConditionCode,EffectiveFrom,EffectiveTo)`.

Normal indexes cover item Base UOM; history correlation/entity/time; identity employee; scope department/employee/warehouse/composite warehouse-RackBin; QC item/category/UOM; conversion from/to UOM; vendor category/vendor; and warehouse-condition warehouse/composite warehouse-RackBin.

Checks: identity type/date; scope date and RackBin-requires-Warehouse; policy date; QC date/limits/exactly-one-owner/positive-sample; GST date/rate/rounding/supply enumeration/state-to-supply consistency/component split; UOM conversion date/distinct UOM/positive factor/precision=6; vendor qualification date; warehouse-condition date and allowed condition enumeration.

## Seeds and exact symmetry

Up owns exactly 88 business/configuration rows: 4 new roles + 8 pages + 74 permission rows + 2 organization policies. The pre-existing active `DEPARTMENT_MANAGER` role is reused and is neither seeded nor mutated. Down removes exactly 88 owned rows: 80 deterministic `DeleteData` rows plus eight owned-only Department Manager permission rows; it never deletes the reused role; the two policy rows are explicitly deleted before their table is dropped. No employee-specific workflow row is seeded.

## Rollback ordering

Down first removes migration-owned triggers/functions, explicitly removes the two policy seeds, drops the item Base UOM FK, drops the nine new tables in dependency-safe order, removes the item index, deletes the remaining owned seeds, drops added vendor/UOM/item columns, drops the RackBin alternate key after dependent tables, and drops the three migration-owned backup tables last. It does not delete REV868/REV868C3 PRs, approvals, reservations, handoffs, employees, departments, manager mappings, histories, or audit rows.

## Offline validation evidence

- Build: PASS, 0 warnings, 0 errors.
- Focused REV869A tests: PASS, 17 passed, 0 failed, 0 skipped.
- All non-PostgreSQL tests: PASS, 252 passed, 0 failed, 0 skipped; the six PostgreSQL workflow tests remain source-preserved and were not connected/executed as database acceptance.
- EF discovery (`--no-connect`): PASS; 11 prerequisites then REV869A exactly once.
- EF model/snapshot check: PASS; no pending model changes.
- Offline SQL generation: PASS; Up and Down generated without connection. Review found 12 `CREATE TABLE` / 12 `DROP TABLE` statements (9 foundation + 3 backup), 7 `NULLS NOT DISTINCT` indexes, all readiness/append-only guards, and no protected database name.
- PowerShell 5.1 parse: not applicable; no PowerShell file was added or changed.
- Secret/privacy/protected-database scan: PASS; no credential or protected database target introduced. OIDC subjects are not written to audit evidence; history records a hash.
- `git diff --check`: PASS.

## Exact intended changed files

1. `src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionEndpointHelpers.cs`
2. `src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionEndpoints.cs`
3. `src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionSupport.cs`
4. `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs`
5. `src/SESS.NexaERP.Api/Middleware/EmployeeIdentityResolutionMiddleware.cs`
6. `src/SESS.NexaERP.Api/Program.cs`
7. `src/SESS.NexaERP.Api/Security/ClaimsCurrentUser.cs`
8. `src/SESS.NexaERP.Api/Security/EmployeeScopeEndpointFilter.cs`
9. `src/SESS.NexaERP.Api/Security/PagePermissionEndpointFilter.cs`
10. `src/SESS.NexaERP.Application/Authorization/IRecordScopeAuthorizer.cs`
11. `src/SESS.NexaERP.Application/Identity/IEmployeeIdentityResolver.cs`
12. `src/SESS.NexaERP.Application/Masters/Rev869AFoundationServices.cs`
13. `src/SESS.NexaERP.Application/Rev869A/Rev869AContracts.cs`
14. `src/SESS.NexaERP.Domain/Authorization/EmployeeOperationalScope.cs`
15. `src/SESS.NexaERP.Domain/Inventory/Item.cs`
16. `src/SESS.NexaERP.Domain/Inventory/WarehouseConditionLocation.cs`
17. `src/SESS.NexaERP.Domain/Masters/TaxGstSetting.cs`
18. `src/SESS.NexaERP.Infrastructure/Authorization/EfRecordScopeAuthorizer.cs`
19. `src/SESS.NexaERP.Infrastructure/Identity/EfEmployeeIdentityResolver.cs`
20. `src/SESS.NexaERP.Infrastructure/Masters/EfRev869AFoundationServices.cs`
21. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260810120000_Rev869AIdentityMasterScopeFoundation.cs`
22. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260810120000_Rev869AIdentityMasterScopeFoundation.Designer.cs`
23. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
24. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869A.cs`
25. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869ASeedData.cs`
26. `tests/SESS.NexaERP.Tests/AuthorizationIntegrationTests.cs`
27. `tests/SESS.NexaERP.Tests/Rev868C1PostgresWorkflowVerificationTests.cs`
28. `tests/SESS.NexaERP.Tests/Rev869AFoundationTests.cs`
29. `tests/SESS.NexaERP.Tests/Rev869ASourceCorrectionTests.cs`
30. `outputs/rev869a_source_correction_checkpoint.md`

## Remaining blockers

- PostgreSQL preflight, application, constraint execution, rollback, and the six database workflow tests remain required later against the separately approved isolated target. None was performed here.
- Actual existing-data readiness is intentionally unknown without PostgreSQL access. Any existing item with null `UomId` will stop migration application; management must then approve an exact item-to-UOM correction rather than a default.
- Existing UOM measurement dimensions require controlled business classification before conversions can use them; this source checkpoint does not fabricate that data.
- Production OIDC mapping and real OIDC testing remain a production blocker.
- REV869A contains no material-issue transaction; that excluded future workflow must reuse the corrected `AVAILABLE` condition and operational-scope authorizer.

No management data decision was fabricated in this checkpoint.

## 2026-08-10 role-reuse and UOM-evidence correction addendum

Starting source commit: `0691a0d31c6d17a99df1e9a211eecf08dc7cbeb9`.

The observed collision is the normalized role code `DEPARTMENT_MANAGER`. Authoritative committed evidence is `20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation`: it creates or reuses that role through the unique role-code contract and attaches the accepted Department Manager permissions and employee assignments. REV869A now fails closed unless exactly one suitable active pre-existing role exists and the four genuinely new role codes have zero collisions.

REV869A preserves every modeled role field by performing no role update and comparing a pre/post fingerprint over Id, Code, Name, IsActive, IsPrivileged, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, and Version. `Role` has no mapped Description property/column in the authoritative current source, so no nonexistent field is fabricated. Existing assignments and legacy permissions are untouched. Eight new REV869A page permissions are inserted by selecting the reused role Id at apply time; they grant view, print, download, and audit-history visibility only. Down removes those eight rows only by REV869A ownership, reused role code, and the exact eight page IDs, and never deletes or mutates the reused role.

Exact derived ownership is 4 created roles + 8 pages + 74 permissions + 2 policies = 88 rows. The 74 permissions comprise 64 rows for eight non-Department-Manager logical roles across eight pages, 8 rows for the reused Department Manager, and 2 Accounts rows. Down removes the same 88 owned rows: 80 deterministic EF `DeleteData` rows plus the 8 ownership-qualified reused-role permissions.

UOM approval remains `PENDING`; both approved expected sets remain empty. Preflight reads all UOM masters with Id, Code, Name, active state, and item-reference count; emits referenced/unreferenced/null/invalid counts; emits a zero-master management-decision label; and reports safe item identity/classification fields while keeping proposed BaseUom `NOT_APPROVED`. No UOM or BaseUom value is guessed, inferred, defaulted, or approved.