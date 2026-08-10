[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp_rev869a_verify",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$GitPath = "",
    [string]$DotnetPath = "",
    [string]$PsqlPath = "C:\Program Files\PostgreSQL\17\bin\psql.exe",
    [string]$ApprovedBackupPath = "",
    [string]$ApprovedBackupSha256 = "",
    [switch]$GeneratePlanOnly,
    [switch]$PreflightOnly,
    [switch]$Apply,
    [switch]$PostMigrationVerification
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$targetDatabase = "sess_nexaerp_rev869a_verify"
$targetMigration = "20260810120000_Rev869AIdentityMasterScopeFoundation"
$prerequisiteMigrations = @(
    "20260808110924_Phase1Foundation",
    "20260808114550_Phase1AuthorizationSeed",
    "20260808123411_Rev866EmployeePermissionMatrix",
    "20260808142353_Rev866CorrectiveStatusPermissionAudit",
    "20260808151207_Rev867MasterFoundation",
    "20260808160435_Rev867C1Corrections",
    "20260808182945_Rev868PurchaseRequisitionFoundation",
    "20260808190920_Rev868PurchaseLocationAllocationCorrection",
    "20260809123000_Rev868C2DepartmentManagerApprovalMapping",
    "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation",
    "20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection"
)
$protectedDatabases = @(
    "sess_nexaerp",
    "sess_nexaerp_rev868_verify",
    "postgres",
    "template0",
    "template1",
    "REV861-like names",
    "production-like names",
    "every database except sess_nexaerp_rev869a_verify"
)
$foundationTables = @(
    "controlled_configuration_histories",
    "employee_identity_mappings",
    "employee_operational_scopes",
    "organization_policies",
    "qc_inspection_policies",
    "tax_gst_settings",
    "uom_conversions",
    "vendor_qualifications",
    "warehouse_condition_locations"
)
$backupTables = @(
    "rev869a_items_prechange_backup",
    "rev869a_uoms_prechange_backup",
    "rev869a_vendors_prechange_backup"
)
$nullSafeIndexes = @(
    "IX_employee_operational_scopes_OrganizationId_EmployeeId_Depar~",
    "IX_organization_policies_OrganizationId_PolicyCode_EffectiveFr~",
    "IX_qc_inspection_policies_OrganizationId_ItemId_ItemCategoryId~",
    "IX_tax_gst_settings_OrganizationId_JurisdictionCode_HsnSacCode~",
    "IX_uom_conversions_OrganizationId_FromUomId_ToUomId_EffectiveF~",
    "IX_vendor_qualifications_OrganizationId_VendorId_ItemCategoryI~",
    "IX_warehouse_condition_locations_OrganizationId_WarehouseId_Ra~"
)

$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$reportDirectory = Join-Path $targetRoot "local-evidence\rev869a"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = Join-Path $reportDirectory "rev869a_isolated_execution_$timestamp.md"
$securePassword = $null
$plainPassword = $null
$preflightEvidence = ""
$postEvidence = ""
$testEvidence = "NOT_RUN"

function Assert-Mode {
    $selected = @(@($GeneratePlanOnly, $PreflightOnly, $Apply, $PostMigrationVerification) | Where-Object { $_ }).Count
    if ($selected -ne 1) { throw "Select exactly one mode: -GeneratePlanOnly, -PreflightOnly, -Apply, or -PostMigrationVerification." }
}

function Assert-TargetSafety {
    if ($Database -cne $targetDatabase) { throw "Refusing database '$Database'. The only permitted target is $targetDatabase." }
    if ($Database -match '(?i)rev861|production|prod|live|main') { throw "Protected or production-like database name rejected: $Database" }
    if ($protectedDatabases -contains $Database) { throw "Protected database rejected: $Database" }
    if ($Port -lt 1 -or $Port -gt 65535) { throw "Port is outside the valid range." }
    if ($HostName -notmatch '^[A-Za-z0-9._:-]+$') { throw "Host contains unsafe characters." }
    if ($UserName -notmatch '^[A-Za-z_][A-Za-z0-9_.-]{0,62}$') { throw "PostgreSQL user is unsafe." }
}

function Resolve-Executable([string]$ExplicitPath, [string]$CommandName, [string]$Label) {
    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        $resolved = Resolve-Path -LiteralPath $ExplicitPath -ErrorAction Stop
        return (Get-Item -LiteralPath $resolved.Path -ErrorAction Stop).FullName
    }
    $command = Get-Command $CommandName -ErrorAction SilentlyContinue
    if (-not $command -or -not $command.Source) { throw "$Label was not found. Supply its explicit path." }
    return $command.Source
}

function Protect-Text([string]$Text) {
    if ([string]::IsNullOrEmpty($Text)) { return "" }
    $sanitized = [regex]::Replace($Text, '(?i)(password|pwd|secret|token)\s*[=:]\s*[^;\s]+', '$1=[REDACTED]')
    $sanitized = [regex]::Replace($sanitized, '(?i)(Host|Server)=[^;\r\n]+;Port=\d+;Database=[^;\r\n]+;Username=[^;\r\n]+;Password=[^;\r\n]+', '[REDACTED_CONNECTION]')
    $sanitized = [regex]::Replace($sanitized, '(?i)Npgsql[^\r\n]*connection[^\r\n]*', 'Npgsql failure evidence [SANITIZED]')
    return $sanitized
}

function Assert-SelectOnlySql([string]$Title, [string]$Sql) {
    if ([string]::IsNullOrWhiteSpace($Sql) -or -not $Sql.TrimEnd().EndsWith(';')) { throw "SQL '$Title' is empty or unterminated." }
    $withoutStrings = [regex]::Replace($Sql, "'([^']|'')*'", "'value'")
    $withoutComments = [regex]::Replace($withoutStrings, '(?m)--.*$', '')
    if ($withoutComments -match '(?i)\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|vacuum|analyze|refresh)\b') {
        throw "SQL '$Title' is not SELECT-only."
    }
}

function Get-PreflightSql {
    return @"
with expected_migrations("MigrationId", ordinal) as (
    values
    ('20260808110924_Phase1Foundation',1),
    ('20260808114550_Phase1AuthorizationSeed',2),
    ('20260808123411_Rev866EmployeePermissionMatrix',3),
    ('20260808142353_Rev866CorrectiveStatusPermissionAudit',4),
    ('20260808151207_Rev867MasterFoundation',5),
    ('20260808160435_Rev867C1Corrections',6),
    ('20260808182945_Rev868PurchaseRequisitionFoundation',7),
    ('20260808190920_Rev868PurchaseLocationAllocationCorrection',8),
    ('20260809123000_Rev868C2DepartmentManagerApprovalMapping',9),
    ('20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation',10),
    ('20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection',11)
), migration_state as (
    select
      (select count(*) from "public"."__EFMigrationsHistory") as total_count,
      (select count(*) from (select e."MigrationId" from expected_migrations e left join "public"."__EFMigrationsHistory" h on h."MigrationId"=e."MigrationId" group by e."MigrationId" having count(h."MigrationId")<>1) bad) as bad_prerequisite_count,
      (select count(*) from "public"."__EFMigrationsHistory" where "MigrationId"='$targetMigration') as target_count
), expected_relations(name) as (values
    ('controlled_configuration_histories'),('employee_identity_mappings'),('employee_operational_scopes'),
    ('organization_policies'),('qc_inspection_policies'),('tax_gst_settings'),('uom_conversions'),
    ('vendor_qualifications'),('warehouse_condition_locations'),('rev869a_items_prechange_backup'),
    ('rev869a_uoms_prechange_backup'),('rev869a_vendors_prechange_backup')
), artifact_state as (
    select
      (select count(*) from expected_relations e where to_regclass('nexa.'||e.name) is not null) as relation_count,
      (select count(*) from information_schema.columns where table_schema='nexa' and (
        (table_name='items' and column_name='BaseUomId') or
        (table_name='uoms' and column_name in ('MeasurementDimension','QuantityPrecision')) or
        (table_name='vendors' and column_name in ('CommercialVerificationStatus','CommercialVerifiedAt','CommercialVerifiedBy','EffectiveFrom','EffectiveTo','RequiresReverification')))) as column_count,
      (select count(*) from pg_indexes where schemaname='nexa' and indexname like '%rev869a%' or schemaname='nexa' and indexname in (
        'IX_items_BaseUomId','IX_employee_identity_mappings_Issuer_Subject_IsActive',
        'IX_employee_operational_scopes_OrganizationId_EmployeeId_Depar~',
        'IX_organization_policies_OrganizationId_PolicyCode_EffectiveFr~',
        'IX_qc_inspection_policies_OrganizationId_ItemId_ItemCategoryId~',
        'IX_tax_gst_settings_OrganizationId_JurisdictionCode_HsnSacCode~',
        'IX_uom_conversions_OrganizationId_FromUomId_ToUomId_EffectiveF~',
        'IX_vendor_qualifications_OrganizationId_VendorId_ItemCategoryI~',
        'IX_warehouse_condition_locations_OrganizationId_WarehouseId_Ra~')) as index_count,
      (select count(*) from pg_constraint where connamespace='nexa'::regnamespace and (conname like '%rev869a%' or conname='AK_rack_bins_WarehouseId_Id' or conname='FK_items_uoms_BaseUomId')) as constraint_count,
      (select count(*) from pg_proc p join pg_namespace n on n.oid=p.pronamespace where n.nspname='nexa' and p.proname like 'rev869a_%') as function_count,
      (select count(*) from pg_trigger where not tgisinternal and tgname like 'trg_rev869a_%') as trigger_count,
      (select count(*) from nexa.roles where "CreatedBy"='migration-rev869a' or "Id" in ('30000000-0000-0000-0000-000000000001'::uuid,'30000000-0000-0000-0000-000000000002'::uuid,'30000000-0000-0000-0000-000000000003'::uuid,'30000000-0000-0000-0000-000000000004'::uuid,'30000000-0000-0000-0000-000000000005'::uuid)) +
      (select count(*) from nexa.page_definitions where "CreatedBy"='migration-rev869a' or "Id"::text like '40000000-0000-0000-0000-00000000000%') +
      (select count(*) from nexa.role_page_permissions where "CreatedBy"='migration-rev869a') as seed_count
), collision_state as (
    select
      (select count(*) from nexa.roles where upper("Code") in ('PURCHASE_MANAGER','STORES_MANAGER','QC_MANAGER','QC_INSPECTOR','DEPARTMENT_MANAGER') and "Id" not in ('30000000-0000-0000-0000-000000000001'::uuid,'30000000-0000-0000-0000-000000000002'::uuid,'30000000-0000-0000-0000-000000000003'::uuid,'30000000-0000-0000-0000-000000000004'::uuid,'30000000-0000-0000-0000-000000000005'::uuid)) as role_collision_count,
      (select count(*) from nexa.page_definitions where "PageKey" in ('security.employee-identities','security.operational-scopes','masters.uoms','masters.uom-conversions','settings.tax-gst','masters.vendor-qualifications','masters.warehouse-condition-locations','qc.inspection-policies') and "Id"::text not like '40000000-0000-0000-0000-00000000000%') as page_collision_count,
      (select count(*) from (select "WarehouseId","Id",count(*) from nexa.rack_bins group by "WarehouseId","Id" having count(*)>1) d) as rack_key_duplicate_count
), readiness_state as (
    select
      (select count(*) from nexa.items where "UomId" is null) as unmapped_item_count,
      (select count(*) from nexa.items i left join nexa.uoms u on u."Id"=i."UomId" where i."UomId" is not null and u."Id" is null) as invalid_uom_reference_count,
      (select count(*) from nexa.items i join nexa.uoms u on u."Id"=i."UomId") as exact_item_uom_evidence_count,
      (select count(distinct i."UomId") from nexa.items i where i."UomId" is not null) as unclassified_measurement_dimension_count
), preservation_state as (
    select
      (select count(*) from nexa.purchase_requisitions) as pr_count,
      (select count(*) from nexa.purchase_requisition_approval_histories) as pr_approval_history_count,
      (select count(*) from nexa.stock_reservations) as reservation_count,
      (select count(*) from nexa.employees where "Status"='Active') as active_employee_count,
      (select count(*) from nexa.employees where "Status"='Relieved') as relieved_employee_count,
      (select count(*) from nexa.departments) as department_count,
      (select count(*) from nexa.department_approval_mappings) as manager_mapping_count
)
select 'database_identity='||case when current_database()='$targetDatabase' then 'PASS' else 'FAIL' end
union all select 'database='||current_database()
union all select 'user='||current_user
union all select 'host='||coalesce(inet_server_addr()::text,'local_socket')
union all select 'port='||inet_server_port()::text
union all select 'prerequisite_total='||total_count from migration_state
union all select 'bad_prerequisite_count='||bad_prerequisite_count from migration_state
union all select 'target_migration_count='||target_count from migration_state
union all select 'partial_relation_count='||relation_count from artifact_state
union all select 'partial_column_count='||column_count from artifact_state
union all select 'partial_index_count='||index_count from artifact_state
union all select 'partial_constraint_count='||constraint_count from artifact_state
union all select 'partial_function_count='||function_count from artifact_state
union all select 'partial_trigger_count='||trigger_count from artifact_state
union all select 'partial_seed_count='||seed_count from artifact_state
union all select 'role_collision_count='||role_collision_count from collision_state
union all select 'page_collision_count='||page_collision_count from collision_state
union all select 'rack_key_duplicate_count='||rack_key_duplicate_count from collision_state
union all select 'future_unique_duplicate_count='||case when relation_count=0 then 0 else -1 end from artifact_state
union all select 'future_effective_overlap_count='||case when relation_count=0 then 0 else -1 end from artifact_state
union all select 'unmapped_item_count='||unmapped_item_count from readiness_state
union all select 'invalid_uom_reference_count='||invalid_uom_reference_count from readiness_state
union all select 'exact_item_uom_evidence_count='||exact_item_uom_evidence_count from readiness_state
union all select 'unclassified_measurement_dimension_count='||unclassified_measurement_dimension_count from readiness_state
union all select 'safe_retry_state='||case when total_count=11 and bad_prerequisite_count=0 and target_count=0 and relation_count=0 and column_count=0 and index_count=0 and constraint_count=0 and function_count=0 and trigger_count=0 and seed_count=0 and role_collision_count=0 and page_collision_count=0 and rack_key_duplicate_count=0 then 'PASS' else 'FAIL' end from migration_state cross join artifact_state cross join collision_state
union all select 'data_readiness_state='||case when unmapped_item_count=0 and invalid_uom_reference_count=0 and unclassified_measurement_dimension_count=0 then 'PASS' else 'FAIL' end from readiness_state
union all select 'preflight_acceptance_state='||case when total_count=11 and bad_prerequisite_count=0 and target_count=0 and relation_count=0 and column_count=0 and index_count=0 and constraint_count=0 and function_count=0 and trigger_count=0 and seed_count=0 and role_collision_count=0 and page_collision_count=0 and rack_key_duplicate_count=0 and unmapped_item_count=0 and invalid_uom_reference_count=0 and unclassified_measurement_dimension_count=0 then 'PASS' else 'FAIL' end from migration_state cross join artifact_state cross join collision_state cross join readiness_state
union all select 'preserve_pr_count='||pr_count from preservation_state
union all select 'preserve_pr_approval_history_count='||pr_approval_history_count from preservation_state
union all select 'preserve_reservation_count='||reservation_count from preservation_state
union all select 'preserve_active_employee_count='||active_employee_count from preservation_state
union all select 'preserve_relieved_employee_count='||relieved_employee_count from preservation_state
union all select 'preserve_department_count='||department_count from preservation_state
union all select 'preserve_manager_mapping_count='||manager_mapping_count from preservation_state;
"@.Trim()
}

function Get-PostMigrationSql {
    return @"
with expected_relations(name) as (values
 ('controlled_configuration_histories'),('employee_identity_mappings'),('employee_operational_scopes'),
 ('organization_policies'),('qc_inspection_policies'),('tax_gst_settings'),('uom_conversions'),
 ('vendor_qualifications'),('warehouse_condition_locations')
), expected_backups(name) as (values ('rev869a_items_prechange_backup'),('rev869a_uoms_prechange_backup'),('rev869a_vendors_prechange_backup')),
schema_state as (
 select
  (select count(*) from "public"."__EFMigrationsHistory" where "MigrationId"='$targetMigration') as target_count,
  (select count(*) from "public"."__EFMigrationsHistory") as migration_count,
  (select count(*) from expected_relations where to_regclass('nexa.'||name) is not null) as foundation_table_count,
  (select count(*) from expected_backups where to_regclass('nexa.'||name) is not null) as backup_table_count,
  (select count(*) from pg_indexes where schemaname='nexa' and indexname in (
   'IX_employee_operational_scopes_OrganizationId_EmployeeId_Depar~','IX_organization_policies_OrganizationId_PolicyCode_EffectiveFr~',
   'IX_qc_inspection_policies_OrganizationId_ItemId_ItemCategoryId~','IX_tax_gst_settings_OrganizationId_JurisdictionCode_HsnSacCode~',
   'IX_uom_conversions_OrganizationId_FromUomId_ToUomId_EffectiveF~','IX_vendor_qualifications_OrganizationId_VendorId_ItemCategoryI~',
   'IX_warehouse_condition_locations_OrganizationId_WarehouseId_Ra~') and indexdef like '%NULLS NOT DISTINCT%') as null_safe_index_count,
  (select count(*) from pg_constraint where conname in ('AK_rack_bins_WarehouseId_Id','FK_employee_operational_scopes_rack_bins_WarehouseId_RackBinId','FK_warehouse_condition_locations_rack_bins_WarehouseId_RackBinId')) as composite_integrity_count,
  (select count(*) from pg_constraint where contype='p' and connamespace='nexa'::regnamespace and conrelid in (select ('nexa.'||name)::regclass from expected_relations)) as primary_key_count,
  (select count(*) from pg_constraint where contype='f' and confdeltype='r' and connamespace='nexa'::regnamespace and (conrelid in (select ('nexa.'||name)::regclass from expected_relations) or conname='FK_items_uoms_BaseUomId')) as restrictive_fk_count,
  (select count(*) from pg_constraint where contype='c' and connamespace='nexa'::regnamespace and conrelid in (select ('nexa.'||name)::regclass from expected_relations)) as check_constraint_count,
  (select count(*) from pg_trigger where not tgisinternal and tgname like 'trg_rev869a_%') as guard_trigger_count
), column_state as (
 select
  (select count(*) from information_schema.columns where table_schema='nexa' and table_name in (select name from expected_relations)) as actual_column_count,
  (select count(*) from (values
    ('controlled_configuration_histories',16,4),('employee_identity_mappings',14,3),('employee_operational_scopes',17,6),
    ('organization_policies',12,3),('qc_inspection_policies',19,7),('tax_gst_settings',26,3),
    ('uom_conversions',17,4),('vendor_qualifications',15,4),('warehouse_condition_locations',13,3)
   ) expected(table_name,column_count,nullable_count)
   where (select count(*) from information_schema.columns c where c.table_schema='nexa' and c.table_name=expected.table_name)<>expected.column_count
      or (select count(*) from information_schema.columns c where c.table_schema='nexa' and c.table_name=expected.table_name and c.is_nullable='YES')<>expected.nullable_count) as table_shape_mismatch_count,
  (select count(*) from information_schema.columns where table_schema='nexa' and table_name='items' and column_name='BaseUomId' and data_type='uuid' and is_nullable='NO') as base_uom_column_count,
  (select count(*) from nexa.items where "BaseUomId" is distinct from "UomId") as uom_backfill_mismatch_count,
  (select count(*) from nexa.tax_gst_settings where not (("SupplierStateCode"="PlaceOfSupplyStateCode" and "SupplyType"='INTRASTATE' and "IgstRate"=0 and "CgstRate"+"SgstRate"="GstRate") or ("SupplierStateCode"<>"PlaceOfSupplyStateCode" and "SupplyType"='INTERSTATE' and "CgstRate"=0 and "SgstRate"=0 and "IgstRate"="GstRate"))) as tax_resolution_mismatch_count
), seed_state as (
 select
  (select count(*) from nexa.roles where "CreatedBy"='migration-rev869a') as role_seed_count,
  (select count(*) from nexa.page_definitions where "CreatedBy"='migration-rev869a') as page_seed_count,
  (select count(*) from nexa.role_page_permissions where "CreatedBy"='migration-rev869a') as permission_seed_count,
  (select count(*) from nexa.organization_policies where "CreatedBy"='migration-rev869a') as policy_seed_count,
  (select count(*) from nexa.role_page_permissions p join nexa.roles r on r."Id"=p."RoleId" where r."Code"='DEPARTMENT_MANAGER' and p."CreatedBy"='migration-rev869a' and not (p."CanView" or p."CanCreate" or p."CanUpdate" or p."CanSubmit" or p."CanVerify" or p."CanApprove" or p."CanReject" or p."CanExport")) as all_false_department_manager_count
), backup_state as (
 select
  (select count(*) from nexa.rev869a_items_prechange_backup b full join nexa.items i on i."Id"=b."Id" where b."Id" is null or i."Id" is null or (to_jsonb(i)-'BaseUomId') is distinct from to_jsonb(b)) as item_backup_mismatch_count,
  (select count(*) from nexa.rev869a_uoms_prechange_backup b full join nexa.uoms u on u."Id"=b."Id" where b."Id" is null or u."Id" is null or (to_jsonb(u)-array['MeasurementDimension','QuantityPrecision']) is distinct from to_jsonb(b)) as uom_backup_mismatch_count,
  (select count(*) from nexa.rev869a_vendors_prechange_backup b full join nexa.vendors v on v."Id"=b."Id" where b."Id" is null or v."Id" is null or (to_jsonb(v)-array['CommercialVerificationStatus','CommercialVerifiedAt','CommercialVerifiedBy','EffectiveFrom','EffectiveTo','RequiresReverification']) is distinct from to_jsonb(b)) as vendor_backup_mismatch_count
), preservation_state as (
 select
  (select count(*) from nexa.purchase_requisitions) as pr_count,
  (select count(*) from nexa.purchase_requisition_approval_histories) as pr_approval_history_count,
  (select count(*) from nexa.stock_reservations) as reservation_count,
  (select count(*) from nexa.employees where "Status"='Active') as active_employee_count,
  (select count(*) from nexa.employees where "Status"='Relieved') as relieved_employee_count,
  (select count(*) from nexa.departments) as department_count,
  (select count(*) from nexa.department_approval_mappings) as manager_mapping_count
)
select 'target_migration_count='||target_count from schema_state
union all select 'migration_count='||migration_count from schema_state
union all select 'foundation_table_count='||foundation_table_count from schema_state
union all select 'backup_table_count='||backup_table_count from schema_state
union all select 'null_safe_index_count='||null_safe_index_count from schema_state
union all select 'composite_integrity_count='||composite_integrity_count from schema_state
union all select 'primary_key_count='||primary_key_count from schema_state
union all select 'restrictive_fk_count='||restrictive_fk_count from schema_state
union all select 'check_constraint_count='||check_constraint_count from schema_state
union all select 'guard_trigger_count='||guard_trigger_count from schema_state
union all select 'actual_foundation_column_count='||actual_column_count from column_state
union all select 'table_shape_mismatch_count='||table_shape_mismatch_count from column_state
union all select 'base_uom_column_count='||base_uom_column_count from column_state
union all select 'uom_backfill_mismatch_count='||uom_backfill_mismatch_count from column_state
union all select 'tax_resolution_mismatch_count='||tax_resolution_mismatch_count from column_state
union all select 'role_seed_count='||role_seed_count from seed_state
union all select 'page_seed_count='||page_seed_count from seed_state
union all select 'permission_seed_count='||permission_seed_count from seed_state
union all select 'policy_seed_count='||policy_seed_count from seed_state
union all select 'migration_owned_seed_count='||(role_seed_count+page_seed_count+permission_seed_count+policy_seed_count) from seed_state
union all select 'all_false_department_manager_count='||all_false_department_manager_count from seed_state
union all select 'item_backup_mismatch_count='||item_backup_mismatch_count from backup_state
union all select 'uom_backup_mismatch_count='||uom_backup_mismatch_count from backup_state
union all select 'vendor_backup_mismatch_count='||vendor_backup_mismatch_count from backup_state
union all select 'preserve_pr_count='||pr_count from preservation_state
union all select 'preserve_pr_approval_history_count='||pr_approval_history_count from preservation_state
union all select 'preserve_reservation_count='||reservation_count from preservation_state
union all select 'preserve_active_employee_count='||active_employee_count from preservation_state
union all select 'preserve_relieved_employee_count='||relieved_employee_count from preservation_state
union all select 'preserve_department_count='||department_count from preservation_state
union all select 'preserve_manager_mapping_count='||manager_mapping_count from preservation_state
union all select 'database_acceptance_state='||case when target_count=1 and migration_count=12 and foundation_table_count=9 and backup_table_count=3 and null_safe_index_count=7 and composite_integrity_count=3 and primary_key_count=9 and restrictive_fk_count=15 and check_constraint_count=22 and guard_trigger_count>=10 and actual_column_count=149 and table_shape_mismatch_count=0 and base_uom_column_count=1 and uom_backfill_mismatch_count=0 and tax_resolution_mismatch_count=0 and role_seed_count=5 and page_seed_count=8 and permission_seed_count=66 and policy_seed_count=2 and all_false_department_manager_count=0 and item_backup_mismatch_count=0 and uom_backup_mismatch_count=0 and vendor_backup_mismatch_count=0 then 'PASS' else 'FAIL' end from schema_state cross join column_state cross join seed_state cross join backup_state
union all select 'column_contract='||table_name||'.'||column_name||'|type='||data_type||'|udt='||udt_name||'|nullable='||is_nullable from information_schema.columns where table_schema='nexa' and table_name in (select name from expected_relations)
union all select 'constraint_contract='||c.conname||'|type='||c.contype||'|definition='||pg_get_constraintdef(c.oid) from pg_constraint c where c.connamespace='nexa'::regnamespace and (c.conrelid in (select ('nexa.'||name)::regclass from expected_relations) or c.conname in ('AK_rack_bins_WarehouseId_Id','FK_items_uoms_BaseUomId'))
union all select 'index_contract='||indexname||'|definition='||indexdef from pg_indexes where schemaname='nexa' and (tablename in (select name from expected_relations) or indexname='IX_items_BaseUomId')
union all select 'seed_contract=roles|'||"Id"::text from nexa.roles where "CreatedBy"='migration-rev869a'
union all select 'seed_contract=page_definitions|'||"Id"::text from nexa.page_definitions where "CreatedBy"='migration-rev869a'
union all select 'seed_contract=role_page_permissions|'||"Id"::text from nexa.role_page_permissions where "CreatedBy"='migration-rev869a'
union all select 'seed_contract=organization_policies|'||"Id"::text from nexa.organization_policies where "CreatedBy"='migration-rev869a';
"@.Trim()
}

function Get-TransactionalVerificationSql {
    return @"
begin;
do `$test`$
declare e uuid; u1 uuid; u2 uuid; w1 uuid; w2 uuid; rb uuid; v uuid; failed boolean;
begin
 select "Id" into e from nexa.employees where "Status"='Active' and "LoginEnabled"=true order by "Id" limit 1;
 select "Id" into u1 from nexa.uoms order by "Id" limit 1;
 select "Id" into u2 from nexa.uoms where "Id"<>u1 order by "Id" limit 1;
 select "Id" into w1 from nexa.warehouses order by "Id" limit 1;
 select "Id" into w2 from nexa.warehouses where "Id"<>w1 order by "Id" limit 1;
 select "Id" into rb from nexa.rack_bins where "WarehouseId"=w1 order by "Id" limit 1;
 select "Id" into v from nexa.vendors order by "Id" limit 1;
 if e is null or u1 is null or u2 is null or w1 is null or w2 is null or rb is null or v is null then raise exception 'REV869A transactional prerequisites unavailable'; end if;

 insert into nexa.employee_identity_mappings ("Id","OrganizationId","Issuer","Subject","EmployeeId","IdentityType","EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000001','SESS','https://offline.invalid','rev869a-test-subject',e,'HUMAN',current_date,true,now(),'REV869A_TEST',0);
 failed:=false; begin insert into nexa.employee_identity_mappings ("Id","OrganizationId","Issuer","Subject","EmployeeId","IdentityType","EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000002','OTHER','https://offline.invalid','rev869a-test-subject',e,'HUMAN',current_date,true,now(),'REV869A_TEST',0); exception when unique_violation then failed:=true; end; if not failed then raise exception 'duplicate identity did not fail closed'; end if;
 failed:=false; begin insert into nexa.uom_conversions ("Id","OrganizationId","FromUomId","ToUomId","MeasurementDimension","ConversionFactor","QuantityPrecision","EffectiveFrom","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000003','SESS',u1,u2,'TEST',0,6,current_date,'PendingApproval',true,now(),'REV869A_TEST',0); exception when check_violation then failed:=true; end; if not failed then raise exception 'invalid UOM conversion was accepted'; end if;
 failed:=false; begin insert into nexa.tax_gst_settings ("Id","OrganizationId","JurisdictionCode","HsnSacCode","SupplierStateCode","PlaceOfSupplyStateCode","SupplyType","VendorRegistrationType","GstRate","CgstRate","SgstRate","IgstRate","CessRate","IsExempt","IsReverseCharge","CurrencyCode","RoundingScale","EffectiveFrom","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000004','SESS','IN','TEST','27','29','INTRASTATE','REGISTERED',18,9,9,0,0,false,false,'INR',2,current_date,'PendingApproval',true,now(),'REV869A_TEST',0); exception when check_violation then failed:=true; end; if not failed then raise exception 'invalid state GST rule was accepted'; end if;
 failed:=false; begin insert into nexa.qc_inspection_policies ("Id","OrganizationId","ParameterCode","MeasurementUomId","InspectionMethod","SampleSize","EffectiveFrom","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000005','SESS','TEST',u1,'TEST',1,current_date,'PendingApproval',true,now(),'REV869A_TEST',0); exception when check_violation then failed:=true; end; if not failed then raise exception 'QC owner fail-closed constraint was bypassed'; end if;
 failed:=false; begin insert into nexa.vendor_qualifications ("Id","OrganizationId","VendorId","QualificationCode","EffectiveFrom","EffectiveTo","VerificationStatus","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000008','SESS',v,'TEST',current_date,current_date-1,'PendingApproval','PendingApproval',true,now(),'REV869A_TEST',0); exception when check_violation then failed:=true; end; if not failed then raise exception 'invalid vendor qualification dates were accepted'; end if;
 failed:=false; begin insert into nexa.warehouse_condition_locations ("Id","OrganizationId","WarehouseId","RackBinId","ConditionCode","EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000006','SESS',w2,rb,'AVAILABLE',current_date,true,now(),'REV869A_TEST',0); exception when foreign_key_violation then failed:=true; end; if not failed then raise exception 'cross-warehouse RackBin was accepted'; end if;
 insert into nexa.controlled_configuration_histories ("Id","OrganizationId","EntityType","EntityId","Action","ActorLoginId","ActorRoleCode","Remarks","CorrelationId","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000007','SESS','TEST','869a0000-0000-0000-0000-000000000007','Create','REV869A_TEST','TEST','TEST','REV869A_TEST',now(),'REV869A_TEST',0);
 failed:=false; begin update nexa.controlled_configuration_histories set "Remarks"='REWRITE' where "Id"='869a0000-0000-0000-0000-000000000007'; exception when raise_exception then failed:=true; end; if not failed then raise exception 'configuration history update was accepted'; end if;
end `$test`$;
rollback;
"@.Trim()
}

function Invoke-Psql([string]$Sql, [bool]$ReadOnly) {
    $tempSql = Join-Path ([IO.Path]::GetTempPath()) ("rev869a_" + [Guid]::NewGuid().ToString("N") + ".sql")
    try {
        $content = $Sql
        if ($ReadOnly) { $content = "begin transaction read only;`n$Sql`nrollback;" }
        [IO.File]::WriteAllText($tempSql, $content, [Text.UTF8Encoding]::new($false))
        $previous = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try { $output = @(& $script:psql -X -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -At -f $tempSql 2>&1); $exitCode = $LASTEXITCODE }
        finally { $ErrorActionPreference = $previous }
        if ($exitCode -ne 0) { throw (Protect-Text ("psql failed with exit code $exitCode. " + (($output | ForEach-Object { $_.ToString() }) -join "`n"))) }
        return (@($output | Where-Object { $_.ToString() -notin @('BEGIN','ROLLBACK','COMMIT','DO') }) -join "`n")
    }
    finally { Remove-Item -LiteralPath $tempSql -Force -ErrorAction SilentlyContinue }
}

function Assert-Evidence([string]$Evidence, [string]$RequiredLine) {
    if ($Evidence -notmatch ('(?m)^' + [regex]::Escape($RequiredLine) + '$')) { throw "Required evidence is not PASS: $RequiredLine" }
}

function Get-EvidenceValue([string]$Evidence, [string]$Key) {
    $match = [regex]::Match($Evidence, '(?m)^' + [regex]::Escape($Key) + '=(\d+)$')
    if (-not $match.Success) { throw "Evidence key is missing: $Key" }
    return [long]$match.Groups[1].Value
}

function Assert-Preservation([string]$Before, [string]$After) {
    foreach ($key in @('preserve_pr_count','preserve_pr_approval_history_count','preserve_reservation_count','preserve_active_employee_count','preserve_relieved_employee_count','preserve_department_count','preserve_manager_mapping_count')) {
        if ((Get-EvidenceValue $Before $key) -ne (Get-EvidenceValue $After $key)) { throw "REV868/REV868C3 preservation failed for $key." }
    }
}
function Write-Plan([string]$PreflightSql, [string]$PostSql) {
    Write-Output "REV869A GeneratePlanOnly"
    Write-Output "host=$HostName"
    Write-Output "port=$Port"
    Write-Output "user=$UserName"
    Write-Output "target_database=$targetDatabase"
    Write-Output "protected_databases=$($protectedDatabases -join ', ')"
    Write-Output "prerequisite_migrations_count=11"
    for ($i=0; $i -lt $prerequisiteMigrations.Count; $i++) { Write-Output ("prerequisite_{0}={1}" -f ($i+1),$prerequisiteMigrations[$i]) }
    Write-Output "target_migration_only=$targetMigration"
    Write-Output "foundation_tables=$($foundationTables -join ', ')"
    Write-Output "migration_owned_backup_tables=$($backupTables -join ', ')"
    Write-Output "null_safe_unique_indexes=$($nullSafeIndexes -join ', ')"
    Write-Output "UOM readiness: every item must have exact UomId evidence; no default or automatic update is permitted."
    Write-Output "Measurement-dimension readiness: every referenced UOM requires approved exact classification; unclassified count must be zero."
    Write-Output "Preflight SQL (SELECT-only/read-only):"
    Write-Output $PreflightSql
    Write-Output "Post-migration verification SQL (SELECT-only/read-only):"
    Write-Output $PostSql
    Write-Output "Full apply additionally runs transaction-rolled-back negative constraint tests and PostgreSQL-backed .NET tests."
    Write-Output "Rollback design: Down removes exactly 81 migration-owned seeds, removes only REV869A-owned objects, preserves REV868/REV868C3 business/history rows, and drops rev869a_vendors_prechange_backup, rev869a_uoms_prechange_backup, and rev869a_items_prechange_backup last."
    Write-Output "Rollback value evidence: backup/current legacy-column comparisons must be zero before rollback; REV869A adds columns but does not rewrite pre-existing columns, so dropping the additions restores the exact pre-REV869A row shape and values."
    Write-Output "Explicit prohibition: no create/drop/restore/backup/main-database/REV861/production operation."
    Write-Output "GeneratePlanOnly requests no password, makes no PostgreSQL connection, and performs no dotnet-ef database operation."
}

function Initialize-DatabaseAccess {
    $script:psql = Resolve-Executable $PsqlPath "psql.exe" "psql.exe"
    $script:securePassword = Read-Host -AsSecureString "Enter the password for isolated database sess_nexaerp_rev869a_verify only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($script:securePassword)
    try { $script:plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $script:plainPassword
}

function Write-SanitizedReport([string]$Mode, [string]$Preflight, [string]$Post, [string]$Tests, [string]$OverallState) {
    New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
    $body = @(
        "# REV869A isolated execution evidence",
        "",
        "- mode=$Mode",
        "- host=$HostName",
        "- port=$Port",
        "- user=$UserName",
        "- target_database=$Database",
        "- target_migration=$targetMigration",
        "- backup_path_evidence=$(if ($ApprovedBackupPath) { Split-Path -Leaf $ApprovedBackupPath } else { 'NOT_REQUIRED_FOR_MODE' })",
        "- backup_sha256_evidence=$(if ($ApprovedBackupSha256) { $ApprovedBackupSha256.ToUpperInvariant() } else { 'NOT_REQUIRED_FOR_MODE' })",
        "- overall_acceptance_state=$OverallState",
        "",
        "## Preflight evidence",
        '```text', (Protect-Text $Preflight), '```',
        "## Post-migration evidence",
        '```text', (Protect-Text $Post), '```',
        "## Test evidence",
        '```text', (Protect-Text $Tests), '```'
    ) -join "`n"
    [IO.File]::WriteAllText($reportPath, $body, [Text.UTF8Encoding]::new($false))
    Write-Host "Sanitized REV869A evidence report: $reportPath"
}

try {
    Assert-Mode
    Assert-TargetSafety
    $preflightSql = Get-PreflightSql
    $postSql = Get-PostMigrationSql
    Assert-SelectOnlySql "Preflight" $preflightSql
    Assert-SelectOnlySql "Post-migration verification" $postSql

    if ($GeneratePlanOnly) { Write-Plan $preflightSql $postSql; return }

    Initialize-DatabaseAccess

    if ($PreflightOnly -or $Apply) {
        $preflightEvidence = Invoke-Psql $preflightSql $true
        Assert-Evidence $preflightEvidence "database_identity=PASS"
        Assert-Evidence $preflightEvidence "safe_retry_state=PASS"
        Assert-Evidence $preflightEvidence "data_readiness_state=PASS"
        Assert-Evidence $preflightEvidence "preflight_acceptance_state=PASS"
        if ($PreflightOnly) { Write-SanitizedReport "PreflightOnly" $preflightEvidence "NOT_RUN" "NOT_RUN" "PASS"; return }
    }

    if ($Apply) {
        if ([string]::IsNullOrWhiteSpace($ApprovedBackupPath) -or [string]::IsNullOrWhiteSpace($ApprovedBackupSha256)) { throw "Full apply requires approved pre-REV869A backup path and SHA-256 evidence." }
        $backup = Resolve-Path -LiteralPath $ApprovedBackupPath -ErrorAction Stop
        $actualHash = (Get-FileHash -LiteralPath $backup.Path -Algorithm SHA256).Hash
        if ($actualHash -cne $ApprovedBackupSha256.ToUpperInvariant()) { throw "Approved backup SHA-256 evidence does not match the supplied file." }

        $git = Resolve-Executable $GitPath "git.exe" "git.exe"
        $dotnet = Resolve-Executable $DotnetPath "dotnet.exe" "dotnet.exe"
        $workspaceStatus = @(& $git -C $targetRoot status --short -- .)
        if ($workspaceStatus.Count -ne 0) { throw "Target workspace must be clean before isolated apply." }

        $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$script:plainPassword"
        $env:NexaErp__ExpectedDatabase = $Database
        $env:REV869A_POSTGRES = $env:ConnectionStrings__NexaErp
        $env:REV868C1_POSTGRES = $env:ConnectionStrings__NexaErp
        Set-Location $targetRoot
        $efOutput = @(& $dotnet ef database update $targetMigration --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext 2>&1)
        if ($LASTEXITCODE -ne 0) { throw (Protect-Text ("REV869A EF apply failed. " + (($efOutput | ForEach-Object { $_.ToString() }) -join "`n"))) }

        Invoke-Psql (Get-TransactionalVerificationSql) $false | Out-Null
        $testOutput = @(& $dotnet test .\tests\SESS.NexaERP.Tests\SESS.NexaERP.Tests.csproj --no-restore --filter "Rev869A|Rev868C1PostgresWorkflowVerificationTests" --logger "console;verbosity=minimal" 2>&1)
        if ($LASTEXITCODE -ne 0) { throw (Protect-Text ("PostgreSQL-backed tests failed. " + (($testOutput | ForEach-Object { $_.ToString() }) -join "`n"))) }
        $testEvidence = (($testOutput | Select-Object -Last 25) -join "`n") + "`ntest_acceptance_state=PASS"
    }

    $postEvidence = Invoke-Psql $postSql $true
    Assert-Evidence $postEvidence "database_acceptance_state=PASS"
    if ($Apply) { Assert-Preservation $preflightEvidence $postEvidence }

    if ($PostMigrationVerification) {
        $testEvidence = "Post-verification-only mode does not rerun transactional tests.`ntest_acceptance_state=NOT_RUN"
        Write-SanitizedReport "PostMigrationVerification" "NOT_RUN" $postEvidence $testEvidence "NOT_CLAIMED"
        return
    }

    Assert-Evidence $testEvidence "test_acceptance_state=PASS"
    $postEvidence = $postEvidence + "`ntest_acceptance_state=PASS`noverall_acceptance_state=PASS"
    Write-SanitizedReport "FullApply" $preflightEvidence $postEvidence $testEvidence "PASS"
}
catch {
    $message = Protect-Text $_.Exception.Message
    if (-not $GeneratePlanOnly) { Write-SanitizedReport "FAILED" $preflightEvidence $postEvidence $testEvidence "FAIL" }
    throw $message
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    Remove-Item Env:\REV869A_POSTGRES -ErrorAction SilentlyContinue
    Remove-Item Env:\REV868C1_POSTGRES -ErrorAction SilentlyContinue
    if ($script:plainPassword) { $script:plainPassword = $null }
    if ($script:securePassword) { $script:securePassword.Dispose() }
}
