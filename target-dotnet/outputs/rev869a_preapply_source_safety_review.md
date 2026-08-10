# REV869A Pre-Apply Source Safety Review

Date: 2026-08-10

Baseline commit: `c94d7f516ba19a6dc9b3e4241c500f41d68dbccd`

Migration reviewed: `20260810120000_Rev869AIdentityMasterScopeFoundation`

Review mode: source-only. No PostgreSQL connection, helper execution, migration application/removal, database creation/drop/restore/backup, production access, frontend work, or REV869B work occurred.

## Decision

**STOP — MATERIAL MIGRATION AND AUTHORIZATION DEFECTS FOUND. DO NOT APPLY REV869A.**

The requested secure apply helper and its focused helper tests were intentionally not created. Creating an application path for a migration known to violate approved fail-closed rules would contradict the instruction to stop after documenting a material defect.

The only permitted future verification database remains `sess_nexaerp_rev869a_verify`, but no future command is approved until the defects in this report are corrected in source, the logical migration is regenerated/reviewed, and management approves a replacement pre-apply checkpoint.

## Material defects

1. `IX_warehouse_condition_locations_OrganizationId_WarehouseId_Co~` is unique on `(OrganizationId, WarehouseId, ConditionCode, IsActive)` with an active-row filter but omits `RackBinId`. It permits only one active Rack/Bin for a condition in a warehouse. A warehouse therefore cannot have two active `AVAILABLE` bins. The model must instead protect the intended Warehouse + Rack/Bin + condition identity.
2. `warehouse_condition_locations` has independent foreign keys to warehouse and Rack/Bin, but no database constraint proves that `RackBinId` belongs to `WarehouseId`. The API checks this only on its create route; direct database or later service writes can persist a cross-warehouse mapping.
3. Missing warehouse-condition configuration does not fail closed in stock availability/reservation. Runtime stock filtering checks `rack_bins.MaterialCondition = AVAILABLE`, but does not require an active matching `warehouse_condition_locations` row.
4. `IRecordScopeAuthorizer` is registered but has no production caller. PR endpoints use a separate local query helper, and the remaining backend services/endpoints do not invoke the central authorizer. Backend-wide record-scope enforcement is therefore not established.
5. PR scope returns the unscoped query when `EmployeeId` is missing. That is fail-open for any authenticated principal without the new employee GUID claim.
6. `ClaimsCurrentUser.LoginId` falls back from OIDC subject to email, name identifier, and preferred username. `IEmployeeIdentityResolver` itself is issuer/subject-only and fail-closed, but it is not integrated into authentication/current-user construction. The end-to-end identity path therefore does not prove the approved no-email/name/employee-code fallback rule.
7. Existing items are not backfilled from their required legacy `UomId` into `BaseUomId`; `BaseUomId` remains nullable. The database does not enforce exactly one Base UOM per item even though a safe legacy source value exists.
8. Existing UOM rows receive `MeasurementDimension=''`. This does not block DDL but is not usable backfill readiness. The conversion service rejects incompatible dimensions later; no controlled dimension mapping is supplied.
9. The India tax key has `SupplyType` but no explicit supply-state field. The approved HSN/SAC + supply state + vendor registration resolution key is therefore incomplete.
10. Effective-dated unique indexes containing nullable owner/scope columns use PostgreSQL's default `NULLS DISTINCT`. Duplicate logical rows remain possible for operational scopes, item-owned/category-owned QC policies, and vendor qualifications when nullable columns are null.
11. **Corrected in the 2026-08-10 role-reuse checkpoint:** REV869A no longer seeds a duplicate `DEPARTMENT_MANAGER`; it reuses the single suitable active legacy role and adds eight owned view/print/download/audit-history permission rows for the new pages.
12. Controlled configuration history has no database immutability control. The provided endpoints append history, but the table permits update/delete through other backend code or direct SQL.

Any one of defects 1–7 is sufficient to withhold application. Together they make `database_acceptance_state=PASS` impossible to justify safely.

## 1. Nine new tables and purposes

| Table | Purpose |
|---|---|
| `nexa.controlled_configuration_histories` | Before/after evidence for controlled identity, scope, UOM, tax, vendor, warehouse-condition, and QC configuration changes. |
| `nexa.employee_identity_mappings` | Effective Issuer + Subject to employee identity mapping. |
| `nexa.employee_operational_scopes` | Effective employee department, warehouse, and Rack/Bin record scope plus privileged cross-scope flag. |
| `nexa.organization_policies` | Effective organization policy code/value configuration, initially vendor final approver and inventory valuation method. |
| `nexa.qc_inspection_policies` | Effective item- or item-category-specific QC parameter, UOM, limit, method, and sample rules. |
| `nexa.tax_gst_settings` | Effective jurisdiction/HSN-SAC/supply-type/vendor-registration GST configuration. |
| `nexa.uom_conversions` | Effective controlled conversion versions between UOMs. |
| `nexa.vendor_qualifications` | Effective vendor/category qualification verification and approval state. |
| `nexa.warehouse_condition_locations` | Warehouse/RackBin/inventory-condition mapping. |

## 2. Exact columns, datatypes, nullability, defaults and Version

Every new table has this common audit block. None of these columns has a database default:

| Column | PostgreSQL datatype | Nullability | Default |
|---|---|---|---|
| `Id` | `uuid` | NOT NULL | none |
| `CreatedAt` | `timestamp with time zone` | NOT NULL | none |
| `CreatedBy` | `text` | NOT NULL | none |
| `UpdatedAt` | `timestamp with time zone` | NULL | none |
| `UpdatedBy` | `text` | NULL | none |
| `Version` | `bigint` | NOT NULL | none |

All table-specific columns below also have no database default.

### `controlled_configuration_histories`

| Column | Datatype | Nullability |
|---|---|---|
| `OrganizationId` | `character varying(100)` | NOT NULL |
| `EntityType` | `character varying(100)` | NOT NULL |
| `EntityId` | `uuid` | NOT NULL |
| `Action` | `character varying(100)` | NOT NULL |
| `BeforeJson` | `jsonb` | NULL |
| `AfterJson` | `jsonb` | NULL |
| `ActorLoginId` | `character varying(256)` | NOT NULL |
| `ActorRoleCode` | `character varying(100)` | NOT NULL |
| `Remarks` | `character varying(2000)` | NOT NULL |
| `CorrelationId` | `character varying(200)` | NOT NULL |

### `employee_identity_mappings`

| Column | Datatype | Nullability |
|---|---|---|
| `OrganizationId` | `character varying(100)` | NOT NULL |
| `Issuer` | `character varying(500)` | NOT NULL |
| `Subject` | `character varying(500)` | NOT NULL |
| `EmployeeId` | `uuid` | NOT NULL |
| `IdentityType` | `character varying(20)` | NOT NULL |
| `EffectiveFrom` | `date` | NOT NULL |
| `EffectiveTo` | `date` | NULL |
| `IsActive` | `boolean` | NOT NULL |

### `employee_operational_scopes`

| Column | Datatype | Nullability |
|---|---|---|
| `OrganizationId` | `character varying(100)` | NOT NULL |
| `EmployeeId` | `uuid` | NOT NULL |
| `DepartmentId` | `uuid` | NULL |
| `WarehouseId` | `uuid` | NULL |
| `RackBinId` | `uuid` | NULL |
| `AllowsPrivilegedCrossScope` | `boolean` | NOT NULL |
| `EffectiveFrom` | `date` | NOT NULL |
| `EffectiveTo` | `date` | NULL |
| `IsActive` | `boolean` | NOT NULL |
| `Remarks` | `character varying(1000)` | NOT NULL |

### `organization_policies`

| Column | Datatype | Nullability |
|---|---|---|
| `OrganizationId` | `character varying(100)` | NOT NULL |
| `PolicyCode` | `character varying(100)` | NOT NULL |
| `PolicyValue` | `character varying(200)` | NOT NULL |
| `EffectiveFrom` | `date` | NOT NULL |
| `EffectiveTo` | `date` | NULL |
| `IsActive` | `boolean` | NOT NULL |

### `qc_inspection_policies`

| Column | Datatype | Nullability |
|---|---|---|
| `OrganizationId` | `character varying(100)` | NOT NULL |
| `ItemId` | `uuid` | NULL |
| `ItemCategoryId` | `uuid` | NULL |
| `ParameterCode` | `character varying(100)` | NOT NULL |
| `MeasurementUomId` | `uuid` | NOT NULL |
| `LowerLimit` | `numeric(24,6)` | NULL |
| `UpperLimit` | `numeric(24,6)` | NULL |
| `InspectionMethod` | `character varying(200)` | NOT NULL |
| `SampleSize` | `integer` | NOT NULL |
| `EffectiveFrom` | `date` | NOT NULL |
| `EffectiveTo` | `date` | NULL |
| `ApprovalStatus` | `character varying(50)` | NOT NULL |
| `IsActive` | `boolean` | NOT NULL |

### `tax_gst_settings`

| Column | Datatype | Nullability |
|---|---|---|
| `OrganizationId` | `character varying(100)` | NOT NULL |
| `JurisdictionCode` | `character varying(30)` | NOT NULL |
| `HsnSacCode` | `character varying(30)` | NOT NULL |
| `SupplyType` | `character varying(30)` | NOT NULL |
| `VendorRegistrationType` | `character varying(30)` | NOT NULL |
| `GstRate` | `numeric(9,6)` | NOT NULL |
| `CgstRate` | `numeric(9,6)` | NOT NULL |
| `SgstRate` | `numeric(9,6)` | NOT NULL |
| `IgstRate` | `numeric(9,6)` | NOT NULL |
| `CessRate` | `numeric(9,6)` | NOT NULL |
| `IsExempt` | `boolean` | NOT NULL |
| `IsReverseCharge` | `boolean` | NOT NULL |
| `CurrencyCode` | `character varying(3)` | NOT NULL |
| `RoundingScale` | `integer` | NOT NULL |
| `EffectiveFrom` | `date` | NOT NULL |
| `EffectiveTo` | `date` | NULL |
| `ApprovalStatus` | `character varying(50)` | NOT NULL |
| `IsActive` | `boolean` | NOT NULL |

### `uom_conversions`

| Column | Datatype | Nullability |
|---|---|---|
| `OrganizationId` | `character varying(100)` | NOT NULL |
| `FromUomId` | `uuid` | NOT NULL |
| `ToUomId` | `uuid` | NOT NULL |
| `MeasurementDimension` | `character varying(50)` | NOT NULL |
| `ConversionFactor` | `numeric(24,12)` | NOT NULL |
| `QuantityPrecision` | `integer` | NOT NULL |
| `EffectiveFrom` | `date` | NOT NULL |
| `EffectiveTo` | `date` | NULL |
| `ApprovalStatus` | `character varying(50)` | NOT NULL |
| `IsActive` | `boolean` | NOT NULL |
| `FirstUsedAt` | `timestamp with time zone` | NULL |

### `vendor_qualifications`

| Column | Datatype | Nullability |
|---|---|---|
| `OrganizationId` | `character varying(100)` | NOT NULL |
| `VendorId` | `uuid` | NOT NULL |
| `ItemCategoryId` | `uuid` | NULL |
| `QualificationCode` | `character varying(100)` | NOT NULL |
| `EffectiveFrom` | `date` | NOT NULL |
| `EffectiveTo` | `date` | NULL |
| `VerificationStatus` | `character varying(50)` | NOT NULL |
| `ApprovalStatus` | `character varying(50)` | NOT NULL |
| `IsActive` | `boolean` | NOT NULL |

### `warehouse_condition_locations`

| Column | Datatype | Nullability |
|---|---|---|
| `OrganizationId` | `character varying(100)` | NOT NULL |
| `WarehouseId` | `uuid` | NOT NULL |
| `RackBinId` | `uuid` | NOT NULL |
| `ConditionCode` | `character varying(30)` | NOT NULL |
| `IsActive` | `boolean` | NOT NULL |

## 3. Exact keys, indexes and checks

### Primary keys

Each new table has `PK_<table>` on `Id`: `PK_controlled_configuration_histories`, `PK_employee_identity_mappings`, `PK_employee_operational_scopes`, `PK_organization_policies`, `PK_qc_inspection_policies`, `PK_tax_gst_settings`, `PK_uom_conversions`, `PK_vendor_qualifications`, and `PK_warehouse_condition_locations`.

### Foreign keys — all `ON DELETE RESTRICT`

- `FK_employee_identity_mappings_employees_EmployeeId`: `EmployeeId -> employees.Id`.
- `FK_employee_operational_scopes_departments_DepartmentId`: `DepartmentId -> departments.Id`.
- `FK_employee_operational_scopes_employees_EmployeeId`: `EmployeeId -> employees.Id`.
- `FK_employee_operational_scopes_rack_bins_RackBinId`: `RackBinId -> rack_bins.Id`.
- `FK_employee_operational_scopes_warehouses_WarehouseId`: `WarehouseId -> warehouses.Id`.
- `FK_qc_inspection_policies_item_categories_ItemCategoryId`: `ItemCategoryId -> item_categories.Id`.
- `FK_qc_inspection_policies_items_ItemId`: `ItemId -> items.Id`.
- `FK_qc_inspection_policies_uoms_MeasurementUomId`: `MeasurementUomId -> uoms.Id`.
- `FK_uom_conversions_uoms_FromUomId`: `FromUomId -> uoms.Id`.
- `FK_uom_conversions_uoms_ToUomId`: `ToUomId -> uoms.Id`.
- `FK_vendor_qualifications_item_categories_ItemCategoryId`: `ItemCategoryId -> item_categories.Id`.
- `FK_vendor_qualifications_vendors_VendorId`: `VendorId -> vendors.Id`.
- `FK_warehouse_condition_locations_rack_bins_RackBinId`: `RackBinId -> rack_bins.Id`.
- `FK_warehouse_condition_locations_warehouses_WarehouseId`: `WarehouseId -> warehouses.Id`.
- Existing-table FK `FK_items_uoms_BaseUomId`: `items.BaseUomId -> uoms.Id`, `ON DELETE RESTRICT`.

### Unique indexes

- `IX_employee_identity_mappings_OrganizationId_EmployeeId_Identi~`: `(OrganizationId, EmployeeId, IdentityType, IsActive)`, filtered by `IsActive=TRUE AND IdentityType='HUMAN'`.
- `IX_employee_identity_mappings_OrganizationId_Issuer_Subject_Is~`: `(OrganizationId, Issuer, Subject, IsActive)`, filtered by `IsActive=TRUE`.
- `IX_employee_operational_scopes_OrganizationId_EmployeeId_Depar~`: `(OrganizationId, EmployeeId, DepartmentId, WarehouseId, RackBinId, EffectiveFrom)`.
- `IX_organization_policies_OrganizationId_PolicyCode_EffectiveFr~`: `(OrganizationId, PolicyCode, EffectiveFrom)`.
- `IX_qc_inspection_policies_OrganizationId_ItemId_ItemCategoryId~`: `(OrganizationId, ItemId, ItemCategoryId, ParameterCode, EffectiveFrom)`.
- `IX_tax_gst_settings_OrganizationId_JurisdictionCode_HsnSacCode~`: `(OrganizationId, JurisdictionCode, HsnSacCode, SupplyType, VendorRegistrationType, EffectiveFrom)`.
- `IX_uom_conversions_OrganizationId_FromUomId_ToUomId_EffectiveF~`: `(OrganizationId, FromUomId, ToUomId, EffectiveFrom)`.
- `IX_vendor_qualifications_OrganizationId_VendorId_ItemCategoryI~`: `(OrganizationId, VendorId, ItemCategoryId, QualificationCode, EffectiveFrom)`.
- `IX_warehouse_condition_locations_OrganizationId_WarehouseId_Co~`: `(OrganizationId, WarehouseId, ConditionCode, IsActive)`, filtered by `IsActive=TRUE` — material defect because `RackBinId` is absent.

### Normal indexes

- `IX_items_BaseUomId` on `items(BaseUomId)`.
- `IX_controlled_configuration_histories_CorrelationId` on `(CorrelationId)`.
- `IX_controlled_configuration_histories_EntityType_EntityId_Crea~` on `(EntityType, EntityId, CreatedAt)`.
- `IX_employee_identity_mappings_EmployeeId` on `(EmployeeId)`.
- `IX_employee_operational_scopes_DepartmentId`, `...EmployeeId`, `...RackBinId`, and `...WarehouseId` on their named columns.
- `IX_qc_inspection_policies_ItemCategoryId`, `...ItemId`, and `...MeasurementUomId` on their named columns.
- `IX_uom_conversions_FromUomId` and `...ToUomId` on their named columns.
- `IX_vendor_qualifications_ItemCategoryId` and `...VendorId` on their named columns.
- `IX_warehouse_condition_locations_RackBinId` and `...WarehouseId` on their named columns.

### Check constraints

- Identity: `CK_employee_identity_mapping_dates` (`EffectiveTo IS NULL OR EffectiveTo >= EffectiveFrom`); `CK_employee_identity_mapping_type` (`IdentityType IN ('HUMAN','SERVICE')`).
- Scope: `CK_employee_operational_scope_dates` (valid effective range).
- Organization policy: `CK_organization_policy_dates` (valid effective range).
- QC: `CK_qc_policy_dates`; `CK_qc_policy_limits` (`UpperLimit >= LowerLimit` when both exist); `CK_qc_policy_owner` (exactly one of ItemId/ItemCategoryId); `CK_qc_policy_sample` (`SampleSize > 0`).
- Tax: `CK_tax_gst_dates`; `CK_tax_gst_rates` (each GST component between 0 and 100); `CK_tax_gst_rounding` (`RoundingScale BETWEEN 0 AND 6`).
- UOM conversion: `CK_uom_conversion_dates`; `CK_uom_conversion_distinct`; `CK_uom_conversion_factor` (`ConversionFactor > 0`); `CK_uom_conversion_precision` (`QuantityPrecision = 6`).
- Vendor qualification: `CK_vendor_qualification_dates`.
- Warehouse condition: `CK_warehouse_condition_code` restricting values to `AVAILABLE`, `QC_HOLD`, `REJECTED`, `QUARANTINE`, `RETURN_TO_VENDOR`, `SCRAP`.

No database check enforces compatible UOM dimensions, RackBin/Warehouse ownership, immutable used conversions, immutable history, non-overlapping effective ranges, ISO-4217 membership, Indian supply-state selection, or approved-status enumerations.

## 4. Exact alterations to existing tables

Up makes only these existing-table changes:

| Table | Added column/index/FK | Datatype | Nullability | Database default for legacy rows |
|---|---|---|---|---|
| `vendors` | `CommercialVerificationStatus` | `varchar(50)` | NOT NULL | `'Draft'` |
| `vendors` | `CommercialVerifiedAt` | `timestamptz` | NULL | none |
| `vendors` | `CommercialVerifiedBy` | `text` | NULL | none |
| `vendors` | `EffectiveFrom` | `date` | NOT NULL | PostgreSQL `DATE '-infinity'` |
| `vendors` | `EffectiveTo` | `date` | NULL | none |
| `vendors` | `RequiresReverification` | `boolean` | NOT NULL | `FALSE` |
| `uoms` | `MeasurementDimension` | `varchar(50)` | NOT NULL | empty string |
| `uoms` | `QuantityPrecision` | `integer` | NOT NULL | `6` |
| `items` | `BaseUomId` | `uuid` | NULL | none |
| `items` | `IX_items_BaseUomId` | normal index | n/a | n/a |
| `items` | `FK_items_uoms_BaseUomId` | restrictive FK | n/a | n/a |

There is no `UPDATE` or backfill operation. No existing row is otherwise rewritten.

## 5. Exact inserted, updated and seeded rows

There are **88 REV869A-owned inserted rows, zero updated legacy rows, and zero deleted rows in Up**:

- Two `organization_policies` rows:
  - `50000000-0000-0000-0000-000000000001`: organization `SESS`, code `VENDOR_FINAL_APPROVER`, value `MANAGING_DIRECTOR`, effective 2026-08-10, active, Version 0.
  - `50000000-0000-0000-0000-000000000002`: organization `SESS`, code `INVENTORY_VALUATION_METHOD`, value `WEIGHTED_AVERAGE`, effective 2026-08-10, active, Version 0.
- Eight `page_definitions` rows with IDs `40000000-0000-0000-0000-000000000001` through `...008`, respectively:
  - `security.employee-identities`, Security, Employee Identities, `/security/employee-identities`.
  - `security.operational-scopes`, Security, Operational Scopes, `/security/operational-scopes`.
  - `masters.uoms`, Masters, UOM Master, `/masters/uoms`.
  - `masters.uom-conversions`, Masters, UOM Conversion Master, `/masters/uom-conversions`.
  - `settings.tax-gst`, Settings, Tax/GST Settings, `/settings/tax-gst`.
  - `masters.vendor-qualifications`, Masters, Vendor Qualifications, `/masters/vendor-qualifications`.
  - `masters.warehouse-condition-locations`, Masters, Warehouse Condition Locations, `/masters/warehouse-condition-locations`.
  - `qc.inspection-policies`, QC, QC Inspection Policies, `/qc/inspection-policies`.
- Four new `roles` rows:
  - `30000000-0000-0000-0000-000000000001`, `PURCHASE_MANAGER`, Purchase Manager, privileged.
  - `30000000-0000-0000-0000-000000000002`, `STORES_MANAGER`, Stores Manager, privileged.
  - `30000000-0000-0000-0000-000000000003`, `QC_MANAGER`, QC Manager, privileged.
  - `30000000-0000-0000-0000-000000000004`, `QC_INSPECTOR`, QC Inspector, not privileged.
- The single active pre-existing `DEPARTMENT_MANAGER` role is reused. REV869A does not insert, update, or delete it; its modeled values, assignments, and legacy permissions remain legacy-owned. REV869A adds only eight owned permissions for the eight new pages.
- Seventy-four `role_page_permissions` rows:
  - The complete Cartesian product of the eight page IDs above and eight roles: the four new roles plus existing `purchase_executive` (`46899b83-f5d7-793d-f008-5b15bcf06b17`), `stores_executive` (`8481d263-cb63-6bc1-76ac-b4c2a56fc1c5`), `technical_director` (`45eb9032-3689-8526-caee-41db0e7e2644`), and `managing_director` (`03325f4f-c6d4-b3f3-f4b3-11b728c275da`): 64 rows, plus eight view/print/download/audit-history permission rows for the reused Department Manager role.
  - Existing `accounts_head` (`10000000-0000-0000-0000-000000000003`) on `masters.vendor-qualifications` and `settings.tax-gst`: two rows.

Every permission row has `CreatedAt=1970-01-01T00:00:00Z`, `CreatedBy='migration-rev869a'`, `UpdatedAt/UpdatedBy=NULL`, `Version=0`, `CanReplaceAttachment=FALSE`, and an exact deterministic ID equal to the first 16 SHA-256 bytes interpreted as a GUID for `rev869a-permission|<stored role code>|<page key>`.

The exact flag algorithm in the committed seed is:

- Directors: view/create/verify every page; TD does not approve except identity override remains false, MD approves all; both export, view commercial values/history; MD has full control.
- Purchase Manager: view all; create/update/submit/resubmit/cancel/upload on UOM, conversion, tax, and vendor pages; commercial visibility; no export.
- Stores Manager: view all; create/update/submit/resubmit/cancel/upload on UOM, conversion, and warehouse-condition pages; verifies warehouse-condition; no export.
- QC Manager: view all; create/update/submit/resubmit/cancel/upload/verify/approve/reject/deactivate on QC policy; no export.
- Purchase Executive, Stores Executive, and QC Inspector: view/print/download every one of the eight pages and no mutation/export/attachment/history/commercial permission.
- Department Manager: the single suitable active pre-existing role is reused without mutation; REV869A adds eight owned view/print/download/audit-history rows for its new pages. Existing assignments and legacy permissions remain legacy-owned, and the role values are protected by pre/post fingerprint equality.
- Accounts Head: view/print/download/export/commercial/history plus verify/reject/request clarification/request revision on tax and vendor qualification; no create/approve/upload.
- Identity-page override: only MD creates/approves; TD and MD verify; corresponding rejection/revision/clarification and audit-history flags follow verify/approve.

## 6. ON CONFLICT targets and unique arbiters

There are **no `ON CONFLICT` clauses** in Up or Down. All 88 REV869A-owned seed inserts are plain inserts. Therefore there is no ON CONFLICT target or arbiter to match.

Application can be blocked by collisions against existing arbiters:

- `roles`: PK `Id` and existing unique index on `Code`.
- `page_definitions`: PK `Id` and existing unique index on `PageKey`.
- `role_page_permissions`: PK `Id` and existing unique index on `(RoleId, PageDefinitionId)`.

The new policy table does not pre-exist in a clean prerequisite state, so its two rows have only the new PK and `(OrganizationId, PolicyCode, EffectiveFrom)` unique index after table creation.

## 7. Exact Up/Down ordering and symmetry

Up order:

1. Add six vendor columns.
2. Add two UOM columns.
3. Add nullable `items.BaseUomId`.
4. Create the nine tables in this order: controlled history, identity mapping, operational scope, organization policy, QC policy, tax setting, UOM conversion, vendor qualification, warehouse condition location.
5. After the Department Manager reuse guard passes, insert two policies, eight pages, four roles, and 74 permissions.
6. Create the item BaseUOM index, all new-table normal/unique indexes, then add the BaseUOM FK.
7. EF records the migration history row. Generated SQL is wrapped by one `START TRANSACTION`/`COMMIT`.

Down order:

1. Drop the BaseUOM FK.
2. Drop all nine new tables in the same table order used by scaffolded Down.
3. Drop `IX_items_BaseUomId`.
4. Delete the 66 statically seeded permission rows by exact UUID, then delete the eight reused-role permissions only when `CreatedBy='migration-rev869a'`, the role code is `DEPARTMENT_MANAGER`, and the page ID is one of the eight REV869A pages.
5. Delete the eight page rows by exact UUID.
6. Delete the four REV869A-created role rows by exact UUID; never delete the reused Department Manager role.
7. Drop the six vendor columns, two UOM columns, and item BaseUOM column.
8. EF removes the migration history row. Generated SQL is transaction wrapped.

Structural symmetry exists for objects and source-owned seeds. Data symmetry does not exist after use: Down drops all configuration/history rows created after Up and all values subsequently stored in the nine added existing-table columns. An external approved pre-migration backup is therefore mandatory before any eventual apply/rollback exercise.

## 8. Existing data and state that can block application

- Anything other than exactly the accepted 11 prerequisite migrations, each once, must fail preflight.
- Presence of REV869A in migration history or any partial new table, new existing-table column, new index/FK/check, seed ID, seed natural key, or migration-owned backup relation must fail preflight.
- Any collision for the four new role codes PURCHASE_MANAGER, STORES_MANAGER, QC_MANAGER, or QC_INSPECTOR blocks application. The normalized DEPARTMENT_MANAGER code must instead resolve to exactly one active suitable pre-existing role; missing, duplicate, inactive, or unsuitable reuse evidence fails closed.
- Existing page IDs or any of the eight PageKeys block plain inserts.
- Existing permission IDs or any of the 74 REV869A-owned `(RoleId, PageDefinitionId)` pairs block plain inserts.
- A conflicting object/index/constraint name blocks DDL even if the object shape differs.
- Existing items do not block nullable BaseUomId DDL, but any item without a controlled Base UOM remains semantically unready. All current items should be proven backfillable from `UomId` before correction.
- Existing UOMs do not block the empty-string dimension default, but every empty/unknown dimension is semantically unready.
- Existing vendors receive safe DDL defaults but are deliberately ineligible until verification; existing vendor status and commercial backfill counts must be reviewed.
- ALTER TABLE locks and long-running transactions can block application operationally.
- Once corrected future unique indexes use nullable columns, preflight must use `IS NOT DISTINCT FROM`/`NULLS NOT DISTINCT` semantics to detect logical duplicates.

## 9. Backup-table requirement

The current Up does not overwrite an existing business value, so a migration-owned pre-value backup table is not required for an immediate unused-migration rollback. However, an approved external pre-REV869A backup with SHA-256 is required before any future apply because Down destroys post-Up configuration/history and removes all new existing-table column values.

No migration-owned backup table currently exists or is created. A clean preflight must confirm that no relation with a REV869A backup naming pattern exists. The corrected migration should not introduce a backup table merely to mask the BaseUOM backfill defect; a deterministic `BaseUomId=UomId` backfill is naturally reversible by dropping the new column.

## 10. REV868/REV868C3 preservation

Source inspection shows no Up or Down operation against REV868/REV868C3 PRs, PR lines, approval histories, status histories, attachments, stock checks, allocations, reservations, reservation histories, PendingRFQ handoffs, employees, employee status/department/approval histories, department mappings, or audit logs.

The migration does not update or delete any existing row in Up. Down deletes only exact REV869A permission/page/role IDs and drops REV869A-owned tables/columns. All FKs from new configuration tables to existing masters are restrictive. Successful atomic application would therefore preserve the listed REV868/REV868C3 records. Seed collisions or partial prior artifacts must fail before apply rather than be overwritten.

## 11. Identity fail-closed audit

`EfEmployeeIdentityResolver` queries only normalized `Issuer`, `Subject`, organization, active flag, effective dates, and employee status/login enablement; it takes two rows and succeeds only when exactly one mapping exists. It never queries email, display name, login ID, or employee code and has no hard-coded employee identity.

End-to-end result: **FAIL**. The resolver is only registered. No authentication/current-user path calls it. `ClaimsCurrentUser.LoginId` explicitly falls back to email, name identifier, and preferred username, and `sess_employee_id` is accepted directly from a claim. Missing OIDC mapping therefore does not presently prevent all protected endpoint execution.

## 12. Backend record-scope audit

`EfRecordScopeAuthorizer` itself fails closed when no effective scope exists and checks department, warehouse and Rack/Bin. It is registered in dependency injection.

End-to-end result: **FAIL**. No source caller invokes `IRecordScopeAuthorizer.AuthorizeAsync`. PR endpoints use a separate query helper, which returns an unscoped query when EmployeeId is absent. Configuration/master endpoints rely on page permissions without consistently applying record scope. Enforcement is not centralized at backend service/repository/query boundaries.

## 13. Missing-configuration fail-closed audit

| Configuration | Source behavior | Result |
|---|---|---|
| OIDC mapping | Resolver fails closed, but resolver is not integrated and claims can bypass it. | FAIL |
| UOM conversion | `GetApprovedAsync` throws on missing/unapproved/ineffective conversion and rejects cross-dimension conversion. | PASS within service; not a global transaction guard yet |
| GST | Resolver requires exactly one approved effective match and throws on zero/ambiguous match. | PASS within service; supply-state key is incomplete |
| Vendor qualification | Missing/ineligible vendor returns false; final approver policy throws on missing/ambiguous mapping. | PASS within service |
| QC policy | Missing/inactive/unapproved policy resolves to `QC_HOLD`. | PASS in policy resolver; no GRN/QC transaction exists yet |
| Warehouse condition | Reservation checks RackBin material condition, not presence of an active canonical mapping. | FAIL |

## 14. Accidental master-introduction audit

The migration and its REV869A source set introduce no Customer, Project, Machine, Attendance, Daily Task, Service, Warranty, or AMC master/table/entity. Existing Customer and demand-source references remain untouched. No competing Location Master is introduced.

## 15. Future Machine Project and lifetime-cost compatibility

The source direction is potentially compatible because it uses stable item/UOM/vendor/warehouse/policy references, effective dates, ISO currency-code fields, weighted-average configuration, and no duplicate Machine/Project/Customer master. It can later reference an approved Machine Project system of record and retain commercial/tax components for lifetime-cost reporting.

Compatibility is conditional, not accepted. Before future Machine Project/lifetime-cost work, REV869A must correct Base UOM enforcement, supply-state tax resolution, canonical warehouse/RackBin constraints, centralized record scope, immutable configuration history, and end-to-end OIDC identity resolution. No Machine Project, consumption, asset lifecycle, service, warranty, AMC, maintenance, downtime, or lifetime-cost transaction is implemented here.

## Exact future helper contract — withheld

No `tools/apply-rev869a-isolated-foundation-secure.ps1` exists at this checkpoint because the migration is unsafe. Consequently there is no valid GeneratePlanOnly command to execute or publish. After a corrected migration and a clean replacement audit, the intended command shape would be reviewed, but must not be treated as approved now.

The accepted prerequisite list for that future review must be exactly:

1. `20260808110924_Phase1Foundation`
2. `20260808114550_Phase1AuthorizationSeed`
3. `20260808123411_Rev866EmployeePermissionMatrix`
4. `20260808142353_Rev866CorrectiveStatusPermissionAudit`
5. `20260808151207_Rev867MasterFoundation`
6. `20260808160435_Rev867C1Corrections`
7. `20260808182945_Rev868PurchaseRequisitionFoundation`
8. `20260808190920_Rev868PurchaseLocationAllocationCorrection`
9. `20260809123000_Rev868C2DepartmentManagerApprovalMapping`
10. `20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation`
11. `20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection`

The sole eventual target migration remains `20260810120000_Rev869AIdentityMasterScopeFoundation` only if its corrected replacement retains that management-approved logical identity. Any changed migration ID requires explicit management review.

## Source-only evidence completed

- Baseline commit and clean status were verified before review.
- `dotnet build SESS.NexaERP.slnx --no-restore` passed with 0 warnings and 0 errors.
- The complete non-PostgreSQL test filter passed: 221 passed, 0 failed, 0 skipped.
- EF `migrations list --no-connect` discovered REV869A exactly once, immediately after `20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection`.
- EF Up and Down SQL were generated offline using a non-routable localhost port-1 metadata connection string; no database connection was made.
- Generated Up: 472 lines, one transaction/commit, nine `CREATE TABLE` statements, ten `ALTER TABLE` statements (nine column additions plus the item BaseUOM FK), and 90 inserts including the EF migration-history row (89 business/configuration seeds).
- Generated Down: 308 lines, one transaction/commit, nine `DROP TABLE` statements, and 88 deletes including the EF migration-history row (87 explicit permission/page/role seed deletes).
- Migration discovery and ordering evidence remains source-verifiable with EF `--no-connect`.
- No ON CONFLICT statement, UpdateData operation, raw SQL operation, migration-owned backup table, database helper invocation, or database connection/application call exists in the migration.
- The material-defect stop condition intentionally prevented helper creation, helper parsing, focused helper tests, and any plan/helper execution.

## Required correction checkpoint

Do not apply REV869A. Correct the 12 defects above, regenerate/reconcile the migration and snapshot without modifying committed REV868/REV868C3 migrations, add exact preflight duplicate/readiness queries and end-to-end authorization tests, repeat offline Up/Down review, and obtain a new management pre-apply approval before creating any application helper.

## 2026-08-10 role-reuse and UOM-evidence correction addendum

Starting source commit: `0691a0d31c6d17a99df1e9a211eecf08dc7cbeb9`.

The observed collision is the normalized role code `DEPARTMENT_MANAGER`. Authoritative committed evidence is `20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation`: it creates or reuses that role through the unique role-code contract and attaches the accepted Department Manager permissions and employee assignments. REV869A now fails closed unless exactly one suitable active pre-existing role exists and the four genuinely new role codes have zero collisions.

REV869A preserves every modeled role field by performing no role update and comparing a pre/post fingerprint over Id, Code, Name, IsActive, IsPrivileged, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, and Version. `Role` has no mapped Description property/column in the authoritative current source, so no nonexistent field is fabricated. Existing assignments and legacy permissions are untouched. Eight new REV869A page permissions are inserted by selecting the reused role Id at apply time; they grant view, print, download, and audit-history visibility only. Down removes those eight rows only by REV869A ownership, reused role code, and the exact eight page IDs, and never deletes or mutates the reused role.

Exact derived ownership is 4 created roles + 8 pages + 74 permissions + 2 policies = 88 rows. The 74 permissions comprise 64 rows for eight non-Department-Manager logical roles across eight pages, 8 rows for the reused Department Manager, and 2 Accounts rows. Down removes the same 88 owned rows: 80 deterministic EF `DeleteData` rows plus the 8 ownership-qualified reused-role permissions.

UOM approval remains `PENDING`; both approved expected sets remain empty. Preflight reads all UOM masters with Id, Code, Name, active state, and item-reference count; emits referenced/unreferenced/null/invalid counts; emits a zero-master management-decision label; and reports safe item identity/classification fields while keeping proposed BaseUom `NOT_APPROVED`. No UOM or BaseUom value is guessed, inferred, defaulted, or approved.