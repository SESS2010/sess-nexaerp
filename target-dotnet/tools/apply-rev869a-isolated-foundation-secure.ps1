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
    [string]$ApprovedPreApplyEvidencePath = "",
    [string]$ApprovedPreApplyEvidenceSha256 = "",
    [switch]$GeneratePlanOnly,
    [switch]$PreflightOnly,
    [switch]$Apply,
    [switch]$PostMigrationVerification,
    [switch]$ResumePostApplyAcceptance
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

# Exact management-approved creation/backfill contract. No value is inferred from legacy data.
$approvedUomMappingContract = [pscustomobject]@{
    ContractVersion = "REV869A-UOM-READINESS-2"
    ApprovalStatus = "APPROVED"
    ManagementApprovalReference = "MGMT-REV869A-UOM-20260810-001"
    UomClassifications = @([pscustomobject]@{
        UomId = "f71a4725-bb15-e7bf-e97b-991985e96328"; UomCode = "EA"; Name = "Each"
        MeasurementDimension = "COUNT"; QuantityPrecision = 0; IsCanonicalBase = $true
        ConversionPolicy = "IDENTITY_ONLY"; LifecycleAction = "CREATE"; ApprovalStatus = "APPROVED"
        ManagementApprovalReference = "MGMT-REV869A-UOM-20260810-001"
    })
    ItemBaseUomMappings = @([pscustomobject]@{
        ItemId = "8c428e59-db05-471d-a7e7-4f7dc1c13b54"; ItemCode = "REV868C1-ITEM"
        BaseUomId = "f71a4725-bb15-e7bf-e97b-991985e96328"; MappingStatus = "APPROVED"
        MappingBasis = "MANAGEMENT_APPROVED"; ManagementApprovalReference = "MGMT-REV869A-UOM-20260810-001"
    })
}
$uomManagementDecisionState = $approvedUomMappingContract.ApprovalStatus
$approvedEaUomId = "f71a4725-bb15-e7bf-e97b-991985e96328"
$approvedEaItemId = "8c428e59-db05-471d-a7e7-4f7dc1c13b54"
$approvedEaReference = "MGMT-REV869A-UOM-20260810-001"
$relievedEmployeeCodes = @('SESS-016','SESS-018','SESS-022','SESS-027','SESS-028','SESS-032','SESS-036','SESS-037','SESS-039')
$acceptedRelievedStatuses = @('left / resigned','left/resigned','resigned','inactive')

$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$reportDirectory = Join-Path $targetRoot "local-evidence\rev869a"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss_fff"
$reportPath = Join-Path $reportDirectory "rev869a_isolated_execution_$timestamp.md"
$securePassword = $null
$plainPassword = $null
$preflightEvidence = ""
$postEvidence = ""
$testEvidence = "NOT_RUN"

function Assert-Mode {
    $selected = @(@($GeneratePlanOnly, $PreflightOnly, $Apply, $PostMigrationVerification, $ResumePostApplyAcceptance) | Where-Object { $_ }).Count
    if ($selected -ne 1) { throw "Select exactly one mode: -GeneratePlanOnly, -PreflightOnly, -Apply, -PostMigrationVerification, or -ResumePostApplyAcceptance." }
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

function Get-RelievedEmployeeCtesSql {
    $expectedCodes = (($relievedEmployeeCodes | ForEach-Object { "('" + ($_ -replace "'", "''") + "')" }) -join ',')
    $acceptedStatuses = (($acceptedRelievedStatuses | ForEach-Object { "('" + ($_ -replace "'", "''") + "')" }) -join ',')
    return @"
expected_relieved_employees(code) as (values $expectedCodes),
accepted_relieved_statuses(status) as (values $acceptedStatuses),
relieved_employee_rows as (
    select "EmployeeCode" as code, lower("Status") as normalized_status
    from nexa.employees
    where "EmployeeCode" like 'SESS-%'
), relieved_employee_metrics as (
    select
      (select count(*) from expected_relieved_employees) as relieved_employee_expected_count,
      (select count(*) from relieved_employee_rows r join expected_relieved_employees e using(code) join accepted_relieved_statuses s on s.status=r.normalized_status) as relieved_employee_actual_matched_count,
      (select count(*) from expected_relieved_employees e where not exists (select 1 from relieved_employee_rows r where r.code=e.code)) as relieved_employee_missing_count,
      (select count(*) from relieved_employee_rows r join accepted_relieved_statuses s on s.status=r.normalized_status where not exists (select 1 from expected_relieved_employees e where e.code=r.code)) as relieved_employee_unexpected_count,
      (select count(*) from (select r.code from relieved_employee_rows r join expected_relieved_employees e using(code) group by r.code having count(*)<>1) d) as relieved_employee_duplicate_count,
      (select count(*) from expected_relieved_employees e where exists (select 1 from relieved_employee_rows r where r.code=e.code) and not exists (select 1 from relieved_employee_rows r join accepted_relieved_statuses s on s.status=r.normalized_status where r.code=e.code)) as relieved_employee_status_mismatch_count
), relieved_employee_state as (
    select *, case when relieved_employee_expected_count=9 and relieved_employee_actual_matched_count=9 and relieved_employee_missing_count=0 and relieved_employee_unexpected_count=0 and relieved_employee_duplicate_count=0 and relieved_employee_status_mismatch_count=0 then 'PASS' else 'FAIL' end as relieved_employee_acceptance_state
    from relieved_employee_metrics
)
"@.Trim()
}
function Get-PreflightSql {
    $relievedCtes = Get-RelievedEmployeeCtesSql
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
), expected_uom_classifications("UomId","UomCode","Name","MeasurementDimension","QuantityPrecision","IsCanonicalBase","ConversionPolicy","LifecycleAction","ManagementApprovalReference","ApprovalStatus") as (
    values ('f71a4725-bb15-e7bf-e97b-991985e96328'::uuid,'EA','Each','COUNT',0,true,'IDENTITY_ONLY','CREATE','MGMT-REV869A-UOM-20260810-001','APPROVED')
), expected_item_base_uom_mappings("ItemId","ItemCode","BaseUomId","MappingStatus","MappingBasis","ManagementApprovalReference") as (
    values ('8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid,'REV868C1-ITEM','f71a4725-bb15-e7bf-e97b-991985e96328'::uuid,'APPROVED','MANAGEMENT_APPROVED','MGMT-REV869A-UOM-20260810-001')), $relievedCtes
, migration_state as (
    select
      (select count(*) from "public"."__EFMigrationsHistory") as total_count,
      (select count(*) from expected_migrations e left join "public"."__EFMigrationsHistory" h on h."MigrationId"=e."MigrationId" where h."MigrationId" is null) as missing_prerequisite_count,
      (select count(*) from "public"."__EFMigrationsHistory" h left join expected_migrations e on e."MigrationId"=h."MigrationId" where e."MigrationId" is null and h."MigrationId"<>'$targetMigration') as unexpected_migration_count,
      (select count(*) from (select "MigrationId" from "public"."__EFMigrationsHistory" group by "MigrationId" having count(*)<>1) d) as duplicate_migration_count,
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
      (select count(*) from pg_indexes where schemaname='nexa' and (indexname like '%rev869a%' or indexname in (
        'IX_items_BaseUomId','IX_employee_identity_mappings_Issuer_Subject_IsActive',
        'IX_employee_operational_scopes_OrganizationId_EmployeeId_Depar~',
        'IX_organization_policies_OrganizationId_PolicyCode_EffectiveFr~',
        'IX_qc_inspection_policies_OrganizationId_ItemId_ItemCategoryId~',
        'IX_tax_gst_settings_OrganizationId_JurisdictionCode_HsnSacCode~',
        'IX_uom_conversions_OrganizationId_FromUomId_ToUomId_EffectiveF~',
        'IX_vendor_qualifications_OrganizationId_VendorId_ItemCategoryI~',
        'IX_warehouse_condition_locations_OrganizationId_WarehouseId_Ra~'))) as index_count,
      (select count(*) from pg_constraint where connamespace='nexa'::regnamespace and (conname like '%rev869a%' or conname='AK_rack_bins_WarehouseId_Id' or conname='FK_items_uoms_BaseUomId')) as constraint_count,
      (select count(*) from pg_proc p join pg_namespace n on n.oid=p.pronamespace where n.nspname='nexa' and p.proname like 'rev869a_%') as function_count,
      (select count(*) from pg_trigger where not tgisinternal and tgname like 'trg_rev869a_%') as trigger_count,
      (select count(*) from nexa.roles where "CreatedBy"='migration-rev869a' or "Id" in ('30000000-0000-0000-0000-000000000001'::uuid,'30000000-0000-0000-0000-000000000002'::uuid,'30000000-0000-0000-0000-000000000003'::uuid,'30000000-0000-0000-0000-000000000004'::uuid)) +
      (select count(*) from nexa.page_definitions where "CreatedBy"='migration-rev869a' or "Id"::text like '40000000-0000-0000-0000-00000000000%') +
      (select count(*) from nexa.role_page_permissions where "CreatedBy"='migration-rev869a' or "Id" in ('aea2e8a1-18a6-72d2-a954-6f5513b80eeb'::uuid,'f8e7d0a6-f056-175a-e604-14c1f9f6ad83'::uuid,'a98dbcec-f959-9f7c-c5f7-3c3a2c8bec12'::uuid,'15ee5b19-d532-c28c-b755-de4152769a7a'::uuid,'5794f740-90b1-5a70-413a-d59bbc97ce78'::uuid,'42e2a253-d767-6191-caf9-e1f79652c44f'::uuid,'38371df3-5a46-5137-8204-4c5391633180'::uuid,'680f7358-4b7c-0733-be42-f9d52e746d1b'::uuid)) as seed_count
), role_counts as (
    select
      (select count(*) from nexa.roles where upper(trim("Code"))='DEPARTMENT_MANAGER') as existing_department_manager_role_count,
      (select count(*) from nexa.roles where upper(trim("Code"))='DEPARTMENT_MANAGER' and "IsActive") as existing_department_manager_active_count,
      (select greatest(count(*)-1,0) from nexa.roles where upper(trim("Code"))='DEPARTMENT_MANAGER') as existing_department_manager_duplicate_count,
      (select count(*) from nexa.roles where "Code"='DEPARTMENT_MANAGER' and "IsActive" and nullif(trim("Name"),'') is not null and nullif(trim("CreatedBy"),'') is not null and "CreatedBy"<>'migration-rev869a' and "Id" not in ('30000000-0000-0000-0000-000000000001'::uuid,'30000000-0000-0000-0000-000000000002'::uuid,'30000000-0000-0000-0000-000000000003'::uuid,'30000000-0000-0000-0000-000000000004'::uuid)) as existing_department_manager_suitable_count,
      (select count(*) from nexa.roles where upper(trim("Code")) in ('PURCHASE_MANAGER','STORES_MANAGER','QC_MANAGER','QC_INSPECTOR')) as new_role_collision_count,
      (select coalesce(string_agg(md5(concat_ws('|',"Id"::text,"Code","Name","IsActive"::text,"IsPrivileged"::text,"CreatedAt"::text,"CreatedBy",coalesce("UpdatedAt"::text,''),coalesce("UpdatedBy",''),"Version"::text)),',' order by "Id"),'MISSING') from nexa.roles where upper(trim("Code"))='DEPARTMENT_MANAGER') as department_manager_role_fingerprint
), role_state as (
    select *,
      case when existing_department_manager_role_count=1 and existing_department_manager_active_count=1 and existing_department_manager_duplicate_count=0 and existing_department_manager_suitable_count=1 then 'PASS' else 'FAIL' end as existing_department_manager_reuse_state,
      case when existing_department_manager_role_count=1 and existing_department_manager_active_count=1 and existing_department_manager_duplicate_count=0 and existing_department_manager_suitable_count=1 and new_role_collision_count=0 then 'PASS' else 'FAIL' end as role_readiness_state
    from role_counts
), collision_state as (
    select
      (select count(*) from nexa.page_definitions where "PageKey" in ('security.employee-identities','security.operational-scopes','masters.uoms','masters.uom-conversions','settings.tax-gst','masters.vendor-qualifications','masters.warehouse-condition-locations','qc.inspection-policies') and "Id"::text not like '40000000-0000-0000-0000-00000000000%') as page_collision_count,
      (select count(*) from (select "WarehouseId","Id",count(*) from nexa.rack_bins group by "WarehouseId","Id" having count(*)>1) d) as rack_key_duplicate_count
), uom_master as (
    select u."Id",u."Code",u."Name",u."IsActive",count(i."Id") as item_reference_count
    from nexa.uoms u left join nexa.items i on i."UomId"=u."Id"
    group by u."Id",u."Code",u."Name",u."IsActive"
), uom_creation_metrics as (
    select
      (select count(*) from expected_uom_classifications) as approved_uom_plan_count,
      (select count(*) from expected_uom_classifications e where e."LifecycleAction"='CREATE' and not exists (select 1 from nexa.uoms u where u."Id"=e."UomId" or upper(trim(u."Code"))=upper(trim(e."UomCode")) or upper(trim(u."Name"))=upper(trim(e."Name")))) as approved_new_uom_count,
      (select count(*) from expected_uom_classifications e join nexa.uoms u on u."Id"=e."UomId" and upper(trim(u."Code"))=upper(trim(e."UomCode")) and upper(trim(u."Name"))=upper(trim(e."Name"))) as approved_existing_uom_count,
      (select count(*) from nexa.uoms u join expected_uom_classifications e on u."Id"=e."UomId") as uom_id_collision_count,
      (select count(*) from nexa.uoms u join expected_uom_classifications e on upper(trim(u."Code"))=upper(trim(e."UomCode"))) as uom_code_collision_count,
      (select count(*) from nexa.uoms u join expected_uom_classifications e on upper(trim(u."Name"))=upper(trim(e."Name"))) as uom_name_collision_count,
      ((select count(*) from (select "UomId" from expected_uom_classifications group by "UomId" having count(*)<>1) d) +
       (select count(*) from (select upper(trim("UomCode")) from expected_uom_classifications group by upper(trim("UomCode")) having count(*)<>1) d) +
       (select count(*) from (select upper(trim("Name")) from expected_uom_classifications group by upper(trim("Name")) having count(*)<>1) d)) as duplicate_uom_classification_count,
      (select count(*) from expected_uom_classifications where "UomId"<>'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid or "UomCode"<>'EA' or "Name"<>'Each' or "MeasurementDimension"<>'COUNT' or "QuantityPrecision"<>0 or not "IsCanonicalBase" or "ConversionPolicy"<>'IDENTITY_ONLY' or "LifecycleAction"<>'CREATE' or "ApprovalStatus"<>'APPROVED' or "ManagementApprovalReference"<>'MGMT-REV869A-UOM-20260810-001') as unapproved_uom_classification_count
), uom_creation_state as (
    select *, case when approved_uom_plan_count=1 and approved_new_uom_count=1 and approved_existing_uom_count=0 and uom_id_collision_count=0 and uom_code_collision_count=0 and uom_name_collision_count=0 and duplicate_uom_classification_count=0 and unapproved_uom_classification_count=0 then 'PASS' else 'FAIL' end as uom_creation_plan_state
    from uom_creation_metrics
), item_mapping_metrics as (
    select
      (select count(*) from expected_item_base_uom_mappings) as approved_item_mapping_count,
      (select count(*) from expected_item_base_uom_mappings e join nexa.items i on i."Id"=e."ItemId" and i."ItemCode"=e."ItemCode" and i."Name"='REV868C1 Item' and i."MaterialType"='Material' and i."Status"='Active' and i."IsActive" and i."UomId" is null) as approved_mapping_actual_matched_count,
      (select count(*) from expected_item_base_uom_mappings e where not exists (select 1 from nexa.items i where i."Id"=e."ItemId" and i."ItemCode"=e."ItemCode" and i."Name"='REV868C1 Item' and i."MaterialType"='Material' and i."Status"='Active' and i."IsActive" and i."UomId" is null)) as approved_mapping_missing_item_count,
      (select count(*) from nexa.items i where i."UomId" is null and not exists (select 1 from expected_item_base_uom_mappings e where e."ItemId"=i."Id" and e."ItemCode"=i."ItemCode" and e."BaseUomId"='f71a4725-bb15-e7bf-e97b-991985e96328'::uuid and e."MappingStatus"='APPROVED' and e."MappingBasis"='MANAGEMENT_APPROVED' and e."ManagementApprovalReference"='MGMT-REV869A-UOM-20260810-001')) as approved_mapping_unexpected_item_count,
      (select count(*) from (select "ItemId" from expected_item_base_uom_mappings group by "ItemId" having count(*)<>1) d) as approved_mapping_duplicate_count,
      (select count(*) from expected_item_base_uom_mappings e where e."ItemId"<>'8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid or e."ItemCode"<>'REV868C1-ITEM' or e."BaseUomId"<>'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid or e."MappingStatus"<>'APPROVED' or e."MappingBasis"<>'MANAGEMENT_APPROVED' or e."ManagementApprovalReference"<>'MGMT-REV869A-UOM-20260810-001' or not exists (select 1 from expected_uom_classifications u where u."UomId"=e."BaseUomId" and u."ApprovalStatus"='APPROVED')) as approved_mapping_invalid_uom_count,
      (select count(*) from nexa.items i where i."UomId" is null and not exists (select 1 from expected_item_base_uom_mappings e where e."ItemId"=i."Id" and e."ItemCode"=i."ItemCode" and e."BaseUomId"='f71a4725-bb15-e7bf-e97b-991985e96328'::uuid and e."MappingStatus"='APPROVED' and e."MappingBasis"='MANAGEMENT_APPROVED' and e."ManagementApprovalReference"='MGMT-REV869A-UOM-20260810-001')) as unresolved_unmapped_item_count
), item_mapping_state as (
    select *, case when approved_item_mapping_count=1 and approved_mapping_actual_matched_count=1 and approved_mapping_missing_item_count=0 and approved_mapping_unexpected_item_count=0 and approved_mapping_duplicate_count=0 and approved_mapping_invalid_uom_count=0 and unresolved_unmapped_item_count=0 then 'PASS' else 'FAIL' end as item_mapping_plan_state
    from item_mapping_metrics
), readiness_state as (
    select
      (select count(*) from uom_master) as uom_master_count,
      (select count(*) from uom_master where item_reference_count>0) as referenced_uom_count,
      (select count(*) from uom_master where item_reference_count=0) as unreferenced_uom_count,
      (select count(*) from nexa.items where "UomId" is null) as unmapped_item_count,
      (select count(*) from nexa.items where "UomId" is null) as null_item_uom_count,
      (select count(*) from nexa.items i left join nexa.uoms u on u."Id"=i."UomId" where i."UomId" is not null and u."Id" is null) as invalid_uom_reference_count,
      (select count(*) from nexa.items i left join nexa.uoms u on u."Id"=i."UomId" where i."UomId" is not null and u."Id" is null) as invalid_item_uom_count,
      (select count(*) from nexa.items i join nexa.uoms u on u."Id"=i."UomId") as exact_item_uom_evidence_count,
      (select count(*) from (select upper(trim("Code")) from nexa.uoms group by upper(trim("Code")) having count(*)>1 union all select upper(trim("Name")) from nexa.uoms group by upper(trim("Name")) having count(*)>1) d) as duplicate_or_ambiguous_uom_count), preservation_state as (
    select
      (select count(*) from nexa.purchase_requisitions) as pr_count,
      (select count(*) from nexa.purchase_requisition_approval_history) as pr_approval_history_count,
      (select count(*) from nexa.stock_reservations) as reservation_count,
      (select count(*) from nexa.employees where "Status"='Active') as active_employee_count,
      (select count(*) from nexa.departments) as department_count,
      (select count(*) from nexa.department_approval_mappings) as manager_mapping_count
)
select 'database_identity='||case when current_database()='$targetDatabase' then 'PASS' else 'FAIL' end
union all select 'database='||current_database()
union all select 'user='||current_user
union all select 'host='||coalesce(inet_server_addr()::text,'local_socket')
union all select 'port='||inet_server_port()::text
union all select 'prerequisite_total='||total_count from migration_state
union all select 'missing_prerequisite_count='||missing_prerequisite_count from migration_state
union all select 'unexpected_migration_count='||unexpected_migration_count from migration_state
union all select 'duplicate_migration_count='||duplicate_migration_count from migration_state
union all select 'bad_prerequisite_count='||bad_prerequisite_count from migration_state
union all select 'target_migration_count='||target_count from migration_state
union all select 'partial_relation_count='||relation_count from artifact_state
union all select 'partial_column_count='||column_count from artifact_state
union all select 'partial_index_count='||index_count from artifact_state
union all select 'partial_constraint_count='||constraint_count from artifact_state
union all select 'partial_function_count='||function_count from artifact_state
union all select 'partial_trigger_count='||trigger_count from artifact_state
union all select 'partial_seed_count='||seed_count from artifact_state
union all select 'existing_department_manager_role_count='||existing_department_manager_role_count from role_state
union all select 'existing_department_manager_active_count='||existing_department_manager_active_count from role_state
union all select 'existing_department_manager_duplicate_count='||existing_department_manager_duplicate_count from role_state
union all select 'existing_department_manager_reuse_state='||existing_department_manager_reuse_state from role_state
union all select 'new_role_collision_count='||new_role_collision_count from role_state
union all select 'role_readiness_state='||role_readiness_state from role_state
union all select 'department_manager_role_fingerprint='||department_manager_role_fingerprint from role_state
union all select 'page_collision_count='||page_collision_count from collision_state
union all select 'rack_key_duplicate_count='||rack_key_duplicate_count from collision_state
union all select 'future_unique_duplicate_count='||case when relation_count=0 then 0 else -1 end from artifact_state
union all select 'future_effective_overlap_count='||case when relation_count=0 then 0 else -1 end from artifact_state
union all select 'uom_master_count='||uom_master_count from readiness_state
union all select 'referenced_uom_count='||referenced_uom_count from readiness_state
union all select 'unreferenced_uom_count='||unreferenced_uom_count from readiness_state
union all select 'unmapped_item_count='||unmapped_item_count from readiness_state
union all select 'null_item_uom_count='||null_item_uom_count from readiness_state
union all select 'invalid_uom_reference_count='||invalid_uom_reference_count from readiness_state
union all select 'invalid_item_uom_count='||invalid_item_uom_count from readiness_state
union all select 'exact_item_uom_evidence_count='||exact_item_uom_evidence_count from readiness_state
union all select 'duplicate_or_ambiguous_uom_count='||duplicate_or_ambiguous_uom_count from readiness_state
union all select 'approved_uom_plan_count='||approved_uom_plan_count from uom_creation_state
union all select 'approved_new_uom_count='||approved_new_uom_count from uom_creation_state
union all select 'approved_existing_uom_count='||approved_existing_uom_count from uom_creation_state
union all select 'uom_id_collision_count='||uom_id_collision_count from uom_creation_state
union all select 'uom_code_collision_count='||uom_code_collision_count from uom_creation_state
union all select 'uom_name_collision_count='||uom_name_collision_count from uom_creation_state
union all select 'duplicate_uom_classification_count='||duplicate_uom_classification_count from uom_creation_state
union all select 'unapproved_uom_classification_count='||unapproved_uom_classification_count from uom_creation_state
union all select 'uom_creation_plan_state='||uom_creation_plan_state from uom_creation_state
union all select 'approved_item_mapping_count='||approved_item_mapping_count from item_mapping_state
union all select 'approved_mapping_missing_item_count='||approved_mapping_missing_item_count from item_mapping_state
union all select 'approved_mapping_unexpected_item_count='||approved_mapping_unexpected_item_count from item_mapping_state
union all select 'approved_mapping_duplicate_count='||approved_mapping_duplicate_count from item_mapping_state
union all select 'approved_mapping_invalid_uom_count='||approved_mapping_invalid_uom_count from item_mapping_state
union all select 'unresolved_unmapped_item_count='||unresolved_unmapped_item_count from item_mapping_state
union all select 'item_mapping_plan_state='||item_mapping_plan_state from item_mapping_state
union all select 'missing_uom_classification_count=0'
union all select 'unexpected_uom_classification_count=0'
union all select 'missing_base_uom_mapping_count='||approved_mapping_missing_item_count from item_mapping_state
union all select 'invalid_base_uom_mapping_count='||approved_mapping_invalid_uom_count from item_mapping_state
union all select 'inferred_or_default_mapping_count='||case when item_mapping_plan_state='PASS' then 0 else 1 end from item_mapping_state
union all select 'relieved_employee_expected_count='||relieved_employee_expected_count from relieved_employee_state
union all select 'relieved_employee_actual_matched_count='||relieved_employee_actual_matched_count from relieved_employee_state
union all select 'relieved_employee_missing_count='||relieved_employee_missing_count from relieved_employee_state
union all select 'relieved_employee_unexpected_count='||relieved_employee_unexpected_count from relieved_employee_state
union all select 'relieved_employee_duplicate_count='||relieved_employee_duplicate_count from relieved_employee_state
union all select 'relieved_employee_status_mismatch_count='||relieved_employee_status_mismatch_count from relieved_employee_state
union all select 'relieved_employee_acceptance_state='||relieved_employee_acceptance_state from relieved_employee_state
union all select 'uom_management_decision_state=$uomManagementDecisionState'
union all select 'safe_retry_state='||case when total_count=11 and missing_prerequisite_count=0 and unexpected_migration_count=0 and duplicate_migration_count=0 and bad_prerequisite_count=0 and target_count=0 and relation_count=0 and column_count=0 and index_count=0 and constraint_count=0 and function_count=0 and trigger_count=0 and seed_count=0 and role_readiness_state='PASS' and page_collision_count=0 and rack_key_duplicate_count=0 and relieved_employee_acceptance_state='PASS' then 'PASS' else 'FAIL' end from migration_state cross join artifact_state cross join role_state cross join collision_state cross join relieved_employee_state
union all select 'data_readiness_state='||case when '$uomManagementDecisionState'='APPROVED' and uom_creation_plan_state='PASS' and item_mapping_plan_state='PASS' and invalid_uom_reference_count=0 then 'PASS' else 'FAIL' end from readiness_state cross join uom_creation_state cross join item_mapping_state
union all select 'preflight_acceptance_state='||case when '$uomManagementDecisionState'='APPROVED' and total_count=11 and missing_prerequisite_count=0 and unexpected_migration_count=0 and duplicate_migration_count=0 and bad_prerequisite_count=0 and target_count=0 and relation_count=0 and column_count=0 and index_count=0 and constraint_count=0 and function_count=0 and trigger_count=0 and seed_count=0 and role_readiness_state='PASS' and page_collision_count=0 and rack_key_duplicate_count=0 and uom_creation_plan_state='PASS' and item_mapping_plan_state='PASS' and invalid_uom_reference_count=0 and relieved_employee_acceptance_state='PASS' then 'PASS' else 'FAIL' end from migration_state cross join artifact_state cross join role_state cross join collision_state cross join readiness_state cross join uom_creation_state cross join item_mapping_state cross join relieved_employee_state
union all select 'uom_master_candidate='||r."Id"||'|code='||replace(replace(r."Code",'|','/'),chr(10),' ')||'|name='||replace(replace(r."Name",'|','/'),chr(10),' ')||'|active='||r."IsActive"||'|item_reference_count='||r.item_reference_count from uom_master r
union all select 'uom_creation_management_decision_required='||case when '$uomManagementDecisionState'='APPROVED' then 'NO' else 'YES' end
union all select 'item_uom_problem='||i."Id"||'|item_code='||replace(replace(i."ItemCode",'|','/'),chr(10),' ')||'|item_name='||replace(replace(i."Name",'|','/'),chr(10),' ')||'|material_type='||replace(replace(i."MaterialType",'|','/'),chr(10),' ')||'|current_uom_id='||coalesce(i."UomId"::text,'NULL')||'|item_status='||replace(replace(i."Status",'|','/'),chr(10),' ')||'|approved_uom_id='||coalesce(e."BaseUomId"::text,'MISSING')||'|approved_uom_code=EA|mapping_status='||coalesce(e."MappingStatus",'MISSING') from nexa.items i left join expected_item_base_uom_mappings e on e."ItemId"=i."Id" where i."UomId" is null
union all select 'approved_item_mapping='||e."ItemId"||'|item_code='||e."ItemCode"||'|uom_id='||e."BaseUomId"||'|base_uom_id='||e."BaseUomId"||'|status='||e."MappingStatus"||'|basis='||e."MappingBasis"||'|approval_reference='||e."ManagementApprovalReference" from expected_item_base_uom_mappings e
union all select 'uom_ambiguity=CODE|normalized='||upper(trim("Code"))||'|ids='||string_agg("Id"::text,',' order by "Id") from nexa.uoms group by upper(trim("Code")) having count(*)>1
union all select 'uom_ambiguity=NAME|normalized='||upper(trim("Name"))||'|ids='||string_agg("Id"::text,',' order by "Id") from nexa.uoms group by upper(trim("Name")) having count(*)>1
union all select 'approved_uom_plan=f71a4725-bb15-e7bf-e97b-991985e96328|code=EA|name=Each|dimension=COUNT|precision=0|canonical_base=true|conversion_policy=IDENTITY_ONLY|lifecycle=CREATE|approval=APPROVED|reference=MGMT-REV869A-UOM-20260810-001'
union all select 'legacy_uom_candidate_warning=m, kg, and ambiguous no are candidate-only and not authoritative'
union all select 'preserve_pr_count='||pr_count from preservation_state
union all select 'preserve_pr_approval_history_count='||pr_approval_history_count from preservation_state
union all select 'preserve_reservation_count='||reservation_count from preservation_state
union all select 'preserve_active_employee_count='||active_employee_count from preservation_state
union all select 'preserve_department_count='||department_count from preservation_state
union all select 'preserve_manager_mapping_count='||manager_mapping_count from preservation_state;
"@.Trim()
}

function Get-PostMigrationSql {
    $relievedCtes = Get-RelievedEmployeeCtesSql
    return @"
with expected_migrations("MigrationId") as (values
 ('20260808110924_Phase1Foundation'),('20260808114550_Phase1AuthorizationSeed'),('20260808123411_Rev866EmployeePermissionMatrix'),('20260808142353_Rev866CorrectiveStatusPermissionAudit'),('20260808151207_Rev867MasterFoundation'),('20260808160435_Rev867C1Corrections'),('20260808182945_Rev868PurchaseRequisitionFoundation'),('20260808190920_Rev868PurchaseLocationAllocationCorrection'),('20260809123000_Rev868C2DepartmentManagerApprovalMapping'),('20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation'),('20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection'),('20260810120000_Rev869AIdentityMasterScopeFoundation')
), expected_relations(name) as (values
 ('controlled_configuration_histories'),('employee_identity_mappings'),('employee_operational_scopes'),
 ('organization_policies'),('qc_inspection_policies'),('tax_gst_settings'),('uom_conversions'),
 ('vendor_qualifications'),('warehouse_condition_locations')
), expected_backups(name) as (values ('rev869a_items_prechange_backup'),('rev869a_uoms_prechange_backup'),('rev869a_vendors_prechange_backup')),
expected_role_seeds("Id","Code","Name","IsPrivileged","IsActive","CreatedBy") as (values
 ('30000000-0000-0000-0000-000000000001'::uuid,'PURCHASE_MANAGER','Purchase Manager',true,true,'migration-rev869a'),
 ('30000000-0000-0000-0000-000000000002'::uuid,'STORES_MANAGER','Stores Manager',true,true,'migration-rev869a'),
 ('30000000-0000-0000-0000-000000000003'::uuid,'QC_MANAGER','QC Manager',true,true,'migration-rev869a'),
 ('30000000-0000-0000-0000-000000000004'::uuid,'QC_INSPECTOR','QC Inspector',false,true,'migration-rev869a')
), expected_page_seeds("Id","PageKey","Module","Title","Route","IsActive","CreatedBy") as (values
 ('40000000-0000-0000-0000-000000000001'::uuid,'security.employee-identities','Security','Employee Identities','/security/employee-identities',true,'migration-rev869a'),
 ('40000000-0000-0000-0000-000000000002'::uuid,'security.operational-scopes','Security','Operational Scopes','/security/operational-scopes',true,'migration-rev869a'),
 ('40000000-0000-0000-0000-000000000003'::uuid,'masters.uoms','Masters','UOM Master','/masters/uoms',true,'migration-rev869a'),
 ('40000000-0000-0000-0000-000000000004'::uuid,'masters.uom-conversions','Masters','UOM Conversion Master','/masters/uom-conversions',true,'migration-rev869a'),
 ('40000000-0000-0000-0000-000000000005'::uuid,'settings.tax-gst','Settings','Tax/GST Settings','/settings/tax-gst',true,'migration-rev869a'),
 ('40000000-0000-0000-0000-000000000006'::uuid,'masters.vendor-qualifications','Masters','Vendor Qualifications','/masters/vendor-qualifications',true,'migration-rev869a'),
 ('40000000-0000-0000-0000-000000000007'::uuid,'masters.warehouse-condition-locations','Masters','Warehouse Condition Locations','/masters/warehouse-condition-locations',true,'migration-rev869a'),
 ('40000000-0000-0000-0000-000000000008'::uuid,'qc.inspection-policies','QC','QC Inspection Policies','/qc/inspection-policies',true,'migration-rev869a')
), expected_policy_seeds("Id","OrganizationId","PolicyCode","PolicyValue","EffectiveFrom","IsActive","CreatedBy") as (values
 ('50000000-0000-0000-0000-000000000001'::uuid,'SESS','VENDOR_FINAL_APPROVER','MANAGING_DIRECTOR',date '2026-08-10',true,'migration-rev869a'),
 ('50000000-0000-0000-0000-000000000002'::uuid,'SESS','INVENTORY_VALUATION_METHOD','WEIGHTED_AVERAGE',date '2026-08-10',true,'migration-rev869a')
), expected_permission_specs("Id","RoleCode","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl") as (values
    ('ec586367-c47c-fa2c-a3a6-7b652a8bcf03'::uuid,'PURCHASE_MANAGER','40000000-0000-0000-0000-000000000001'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,true,false,false),
    ('6f876ecb-f37c-9c97-c30e-fe992cf56d10'::uuid,'PURCHASE_MANAGER','40000000-0000-0000-0000-000000000002'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,true,false,false),
    ('4a045df1-dc0e-e920-6a8e-02afdc1f9f37'::uuid,'PURCHASE_MANAGER','40000000-0000-0000-0000-000000000003'::uuid,true,true,true,true,false,false,false,false,false,true,true,false,true,true,false,true,false,true,false,false),
    ('1c7a6074-478b-76b5-920b-da17f5147d7c'::uuid,'PURCHASE_MANAGER','40000000-0000-0000-0000-000000000004'::uuid,true,true,true,true,false,false,false,false,false,true,true,false,true,true,false,true,false,true,false,false),
    ('4be63323-a734-943b-8d03-b7d80fd58683'::uuid,'PURCHASE_MANAGER','40000000-0000-0000-0000-000000000005'::uuid,true,true,true,true,false,false,false,false,false,true,true,false,true,true,false,true,false,true,false,false),
    ('21c63dbc-0985-5d45-72a0-6db78ecf2a39'::uuid,'PURCHASE_MANAGER','40000000-0000-0000-0000-000000000006'::uuid,true,true,true,true,false,false,false,false,false,true,true,false,true,true,false,true,false,true,false,false),
    ('c6c95969-c68e-f1d9-c708-d280df85c29e'::uuid,'PURCHASE_MANAGER','40000000-0000-0000-0000-000000000007'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,true,false,false),
    ('b2cfa60a-5fc8-d083-5d04-5a01d70cbc02'::uuid,'PURCHASE_MANAGER','40000000-0000-0000-0000-000000000008'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,true,false,false),
    ('2a6b1d70-e88b-a9f8-3f68-1e4cbfdd8b67'::uuid,'PURCHASE_EXECUTIVE','40000000-0000-0000-0000-000000000001'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('c66d4c06-b1a6-2b18-37b1-56b7d9677643'::uuid,'PURCHASE_EXECUTIVE','40000000-0000-0000-0000-000000000002'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('8fec4773-1c6b-0cae-e623-f394e71f3901'::uuid,'PURCHASE_EXECUTIVE','40000000-0000-0000-0000-000000000003'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('65ac8a90-8d09-c31b-1285-cf09b38f6c6f'::uuid,'PURCHASE_EXECUTIVE','40000000-0000-0000-0000-000000000004'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('c78eb0ff-8d8e-0082-a51f-f862c75a0ca9'::uuid,'PURCHASE_EXECUTIVE','40000000-0000-0000-0000-000000000005'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('5ceb4c02-d702-580c-00ca-75404dada0f7'::uuid,'PURCHASE_EXECUTIVE','40000000-0000-0000-0000-000000000006'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('11038140-87fb-6522-7425-da633f209502'::uuid,'PURCHASE_EXECUTIVE','40000000-0000-0000-0000-000000000007'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('338f3857-ff34-b27c-d671-ad42eb33fe3d'::uuid,'PURCHASE_EXECUTIVE','40000000-0000-0000-0000-000000000008'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('ce23c00d-3772-21ee-ec50-8e903fe1fc81'::uuid,'STORES_MANAGER','40000000-0000-0000-0000-000000000001'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('062c8d00-221a-5347-8c3b-bd87604fc083'::uuid,'STORES_MANAGER','40000000-0000-0000-0000-000000000002'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('e7b9e221-0799-7867-f623-9ec602b64c84'::uuid,'STORES_MANAGER','40000000-0000-0000-0000-000000000003'::uuid,true,true,true,true,false,false,false,false,false,true,true,false,true,true,false,true,false,false,false,false),
    ('a3c118c5-33e2-98c4-a904-8a7cf3a5a7ad'::uuid,'STORES_MANAGER','40000000-0000-0000-0000-000000000004'::uuid,true,true,true,true,false,false,false,false,false,true,true,false,true,true,false,true,false,false,false,false),
    ('125a37ad-46bf-b2c4-c02f-d588f0969a84'::uuid,'STORES_MANAGER','40000000-0000-0000-0000-000000000005'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('c63323be-cd04-b0e3-eb1c-97442843e6ba'::uuid,'STORES_MANAGER','40000000-0000-0000-0000-000000000006'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('0f9deb7e-4745-0527-9d8e-bb60c8cececa'::uuid,'STORES_MANAGER','40000000-0000-0000-0000-000000000007'::uuid,true,true,true,true,true,false,true,true,true,true,true,false,true,true,false,true,false,false,true,false),
    ('2d3b700e-1aeb-d373-de93-2c2fa8a3370e'::uuid,'STORES_MANAGER','40000000-0000-0000-0000-000000000008'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('eaa52044-6e94-fcd9-82d8-9a3323450753'::uuid,'STORES_EXECUTIVE','40000000-0000-0000-0000-000000000001'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('0229a2fa-bdb6-b6b5-4da0-db1e3bc6d395'::uuid,'STORES_EXECUTIVE','40000000-0000-0000-0000-000000000002'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('b1302fd0-129b-62d1-3006-293ac6bf6a87'::uuid,'STORES_EXECUTIVE','40000000-0000-0000-0000-000000000003'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('1913267c-7d4c-e241-5011-8cf30bd84137'::uuid,'STORES_EXECUTIVE','40000000-0000-0000-0000-000000000004'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('dc466d18-679e-85aa-a346-e4062dfbeddc'::uuid,'STORES_EXECUTIVE','40000000-0000-0000-0000-000000000005'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('bd4fa4d4-57b6-6b58-3f9d-f5e17b47865e'::uuid,'STORES_EXECUTIVE','40000000-0000-0000-0000-000000000006'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('d15f336b-f96e-94d4-9ac0-764d82895884'::uuid,'STORES_EXECUTIVE','40000000-0000-0000-0000-000000000007'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('a41ce315-d082-63f6-b0cb-5cde4bd4fe03'::uuid,'STORES_EXECUTIVE','40000000-0000-0000-0000-000000000008'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('a9d6e145-ea26-2b7f-1844-90682dffd78f'::uuid,'QC_MANAGER','40000000-0000-0000-0000-000000000001'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('68b94637-ea35-95bb-731a-68ab0d83b6f5'::uuid,'QC_MANAGER','40000000-0000-0000-0000-000000000002'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('799bd43d-d80a-0eb5-777d-6ba1afc0717b'::uuid,'QC_MANAGER','40000000-0000-0000-0000-000000000003'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('d8d1e9af-6bf3-d7f7-cb14-7a9f6d1d13ab'::uuid,'QC_MANAGER','40000000-0000-0000-0000-000000000004'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('2139e45b-4437-632d-a851-c87145ba4071'::uuid,'QC_MANAGER','40000000-0000-0000-0000-000000000005'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('cecbb5f3-5709-025a-d5f7-807d4151a665'::uuid,'QC_MANAGER','40000000-0000-0000-0000-000000000006'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('8626e388-a399-ab33-a557-df27a097aa40'::uuid,'QC_MANAGER','40000000-0000-0000-0000-000000000007'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('451ff88f-816b-39fb-0097-18ecd1e752d2'::uuid,'QC_MANAGER','40000000-0000-0000-0000-000000000008'::uuid,true,true,true,true,true,false,false,true,false,true,true,false,true,true,false,true,false,false,true,false),
    ('f5240291-ec17-bea1-5a31-eead7c8a0ec9'::uuid,'QC_INSPECTOR','40000000-0000-0000-0000-000000000001'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('dc9b2f94-6506-f50d-5518-46d6e04af43a'::uuid,'QC_INSPECTOR','40000000-0000-0000-0000-000000000002'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('01b635f9-b7c0-6952-aad1-db0a13aabe39'::uuid,'QC_INSPECTOR','40000000-0000-0000-0000-000000000003'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('5d19be17-57d3-0652-d98b-5a11f62faf19'::uuid,'QC_INSPECTOR','40000000-0000-0000-0000-000000000004'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('8c9dff2e-5ed3-13c3-668b-b11f7602e9d8'::uuid,'QC_INSPECTOR','40000000-0000-0000-0000-000000000005'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('973f3950-0ae0-1df9-ad6f-570f6cd38b89'::uuid,'QC_INSPECTOR','40000000-0000-0000-0000-000000000006'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('7f52e39c-fb55-8e9b-3865-d29bffaee942'::uuid,'QC_INSPECTOR','40000000-0000-0000-0000-000000000007'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('7170ddae-3c8f-154e-5e20-e51f8d572074'::uuid,'QC_INSPECTOR','40000000-0000-0000-0000-000000000008'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,false,false),
    ('a1153b99-f614-049c-8f62-2e6672c1163d'::uuid,'TECHNICAL_DIRECTOR','40000000-0000-0000-0000-000000000001'::uuid,true,false,false,false,true,false,true,true,true,false,false,false,true,true,true,false,false,true,true,false),
    ('6ecea443-25c0-ccd6-067d-c53b9cb5369b'::uuid,'TECHNICAL_DIRECTOR','40000000-0000-0000-0000-000000000002'::uuid,true,false,false,false,true,false,true,true,true,false,false,false,true,true,true,false,false,true,true,false),
    ('8642768b-72de-85b7-3700-52f204bc2412'::uuid,'TECHNICAL_DIRECTOR','40000000-0000-0000-0000-000000000003'::uuid,true,true,true,true,true,false,true,true,true,true,true,false,true,true,true,true,false,true,true,false),
    ('625a6c21-32f6-45b9-911c-fef812d43657'::uuid,'TECHNICAL_DIRECTOR','40000000-0000-0000-0000-000000000004'::uuid,true,true,true,true,true,false,true,true,true,true,true,false,true,true,true,true,false,true,true,false),
    ('baff3f8c-6e8c-e814-86d6-9431df1251d1'::uuid,'TECHNICAL_DIRECTOR','40000000-0000-0000-0000-000000000005'::uuid,true,true,true,true,true,false,true,true,true,true,true,false,true,true,true,true,false,true,true,false),
    ('fcf487a0-7345-b5f0-8f88-784ce8f0016a'::uuid,'TECHNICAL_DIRECTOR','40000000-0000-0000-0000-000000000006'::uuid,true,true,true,true,true,false,true,true,true,true,true,false,true,true,true,true,false,true,true,false),
    ('35376d76-a0b1-7ee1-b32d-1499b7e24f06'::uuid,'TECHNICAL_DIRECTOR','40000000-0000-0000-0000-000000000007'::uuid,true,true,true,true,true,false,true,true,true,true,true,false,true,true,true,true,false,true,true,false),
    ('7fa66608-1650-7481-0d97-33b93ff14201'::uuid,'TECHNICAL_DIRECTOR','40000000-0000-0000-0000-000000000008'::uuid,true,true,true,true,true,false,true,true,true,true,true,false,true,true,true,true,false,true,true,false),
    ('97eb4bbf-2fea-a75a-5226-bbfd8aa0d667'::uuid,'MANAGING_DIRECTOR','40000000-0000-0000-0000-000000000001'::uuid,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,false,true,true,true),
    ('61063a45-9de0-6ada-716f-b308ab881c76'::uuid,'MANAGING_DIRECTOR','40000000-0000-0000-0000-000000000002'::uuid,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,false,true,true,true),
    ('d1224cfb-0e09-3337-c4dc-b5fc728b4450'::uuid,'MANAGING_DIRECTOR','40000000-0000-0000-0000-000000000003'::uuid,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,false,true,true,true),
    ('7492c61c-6d29-ca2a-0f2c-e7fe98b66bc0'::uuid,'MANAGING_DIRECTOR','40000000-0000-0000-0000-000000000004'::uuid,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,false,true,true,true),
    ('90b24916-a7da-926c-85db-d40df0bb5cb5'::uuid,'MANAGING_DIRECTOR','40000000-0000-0000-0000-000000000005'::uuid,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,false,true,true,true),
    ('8db2124b-a20d-c86d-0c55-44f6a9b83dcb'::uuid,'MANAGING_DIRECTOR','40000000-0000-0000-0000-000000000006'::uuid,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,false,true,true,true),
    ('99230bc2-6f8e-6513-4b7a-d6424d3cf345'::uuid,'MANAGING_DIRECTOR','40000000-0000-0000-0000-000000000007'::uuid,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,false,true,true,true),
    ('8c9ccb5e-d2ee-b5c2-70b3-26f0805ab6d3'::uuid,'MANAGING_DIRECTOR','40000000-0000-0000-0000-000000000008'::uuid,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,false,true,true,true),
    ('75b705db-ab96-19a0-0fc7-1f5ec2ada945'::uuid,'ACCOUNTS_HEAD','40000000-0000-0000-0000-000000000005'::uuid,true,false,false,false,true,false,true,true,true,false,false,false,true,true,true,false,false,true,true,false),
    ('f9c9f6cc-48b9-8727-4c81-4196e4444b59'::uuid,'ACCOUNTS_HEAD','40000000-0000-0000-0000-000000000006'::uuid,true,false,false,false,true,false,true,true,true,false,false,false,true,true,true,false,false,true,true,false),
    ('aea2e8a1-18a6-72d2-a954-6f5513b80eeb'::uuid,'DEPARTMENT_MANAGER','40000000-0000-0000-0000-000000000001'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,true,false),
    ('f8e7d0a6-f056-175a-e604-14c1f9f6ad83'::uuid,'DEPARTMENT_MANAGER','40000000-0000-0000-0000-000000000002'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,true,false),
    ('a98dbcec-f959-9f7c-c5f7-3c3a2c8bec12'::uuid,'DEPARTMENT_MANAGER','40000000-0000-0000-0000-000000000003'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,true,false),
    ('15ee5b19-d532-c28c-b755-de4152769a7a'::uuid,'DEPARTMENT_MANAGER','40000000-0000-0000-0000-000000000004'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,true,false),
    ('5794f740-90b1-5a70-413a-d59bbc97ce78'::uuid,'DEPARTMENT_MANAGER','40000000-0000-0000-0000-000000000005'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,true,false),
    ('42e2a253-d767-6191-caf9-e1f79652c44f'::uuid,'DEPARTMENT_MANAGER','40000000-0000-0000-0000-000000000006'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,true,false),
    ('38371df3-5a46-5137-8204-4c5391633180'::uuid,'DEPARTMENT_MANAGER','40000000-0000-0000-0000-000000000007'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,true,false),
    ('680f7358-4b7c-0733-be42-f9d52e746d1b'::uuid,'DEPARTMENT_MANAGER','40000000-0000-0000-0000-000000000008'::uuid,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,false,false,true,false)
), expected_permission_seeds as (
 select e.*, (select case when count(*)=1 then min(r."Id"::text)::uuid else null end from nexa.roles r where upper(trim(r."Code"))=e."RoleCode") as "RoleId"
 from expected_permission_specs e
),
$relievedCtes
, schema_state as (
 select
  (select count(*) from "public"."__EFMigrationsHistory" where "MigrationId"='$targetMigration') as target_count,
  (select count(*) from "public"."__EFMigrationsHistory") as migration_count,
  (select count(*) from expected_migrations e left join "public"."__EFMigrationsHistory" h on h."MigrationId"=e."MigrationId" where h."MigrationId" is null) as missing_migration_count,
  (select count(*) from "public"."__EFMigrationsHistory" h left join expected_migrations e on e."MigrationId"=h."MigrationId" where e."MigrationId" is null) as unexpected_migration_count,
  (select count(*) from (select "MigrationId" from "public"."__EFMigrationsHistory" group by "MigrationId" having count(*)<>1) d) as duplicate_migration_count,
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
), role_counts as (
 select
  (select count(*) from nexa.roles where upper(trim("Code"))='DEPARTMENT_MANAGER') as existing_department_manager_role_count,
  (select count(*) from nexa.roles where upper(trim("Code"))='DEPARTMENT_MANAGER' and "IsActive") as existing_department_manager_active_count,
  (select greatest(count(*)-1,0) from nexa.roles where upper(trim("Code"))='DEPARTMENT_MANAGER') as existing_department_manager_duplicate_count,
  (select count(*) from nexa.roles where "Code"='DEPARTMENT_MANAGER' and "IsActive" and nullif(trim("Name"),'') is not null and nullif(trim("CreatedBy"),'') is not null and "CreatedBy"<>'migration-rev869a' and "Id" not in ('30000000-0000-0000-0000-000000000001'::uuid,'30000000-0000-0000-0000-000000000002'::uuid,'30000000-0000-0000-0000-000000000003'::uuid,'30000000-0000-0000-0000-000000000004'::uuid)) as existing_department_manager_suitable_count,
  (select coalesce(string_agg(md5(concat_ws('|',"Id"::text,"Code","Name","IsActive"::text,"IsPrivileged"::text,"CreatedAt"::text,"CreatedBy",coalesce("UpdatedAt"::text,''),coalesce("UpdatedBy",''),"Version"::text)),',' order by "Id"),'MISSING') from nexa.roles where upper(trim("Code"))='DEPARTMENT_MANAGER') as department_manager_role_fingerprint
), role_state as (
 select *, case when existing_department_manager_role_count=1 and existing_department_manager_active_count=1 and existing_department_manager_duplicate_count=0 and existing_department_manager_suitable_count=1 then 'PASS' else 'FAIL' end as existing_department_manager_reuse_state
 from role_counts
), seed_counts as (
 select
  (select count(*) from nexa.roles where "CreatedBy"='migration-rev869a') as role_seed_count,
  (select count(*) from nexa.page_definitions where "CreatedBy"='migration-rev869a') as page_seed_count,
  (select count(*) from nexa.role_page_permissions where "CreatedBy"='migration-rev869a') as permission_seed_count,
  (select count(*) from nexa.organization_policies where "CreatedBy"='migration-rev869a') as policy_seed_count
), seed_metrics as (
 select
  (select count(*) from nexa.roles r where r."CreatedBy"='migration-rev869a' and not exists (select 1 from expected_role_seeds e where e."Id"=r."Id" and e."Code"=r."Code" and e."Name"=r."Name" and e."IsPrivileged"=r."IsPrivileged" and e."IsActive"=r."IsActive" and e."CreatedBy"=r."CreatedBy")) as role_seed_unexpected_count,
  (select count(*) from expected_role_seeds e where (select count(*) from nexa.roles r where r."Id"=e."Id" and r."Code"=e."Code" and r."Name"=e."Name" and r."IsPrivileged"=e."IsPrivileged" and r."IsActive"=e."IsActive" and r."CreatedBy"=e."CreatedBy")<>1) as role_seed_missing_count,
  (select count(*) from nexa.page_definitions p where p."CreatedBy"='migration-rev869a' and not exists (select 1 from expected_page_seeds e where e."Id"=p."Id" and e."PageKey"=p."PageKey" and e."Module"=p."Module" and e."Title"=p."Title" and e."Route"=p."Route" and e."IsActive"=p."IsActive" and e."CreatedBy"=p."CreatedBy")) as page_seed_unexpected_count,
  (select count(*) from expected_page_seeds e where (select count(*) from nexa.page_definitions p where p."Id"=e."Id" and p."PageKey"=e."PageKey" and p."Module"=e."Module" and p."Title"=e."Title" and p."Route"=e."Route" and p."IsActive"=e."IsActive" and p."CreatedBy"=e."CreatedBy")<>1) as page_seed_missing_count,
  (select count(*) from nexa.organization_policies p where p."CreatedBy"='migration-rev869a' and not exists (select 1 from expected_policy_seeds e where e."Id"=p."Id" and e."OrganizationId"=p."OrganizationId" and e."PolicyCode"=p."PolicyCode" and e."PolicyValue"=p."PolicyValue" and e."EffectiveFrom"=p."EffectiveFrom" and e."IsActive"=p."IsActive" and e."CreatedBy"=p."CreatedBy")) as policy_seed_unexpected_count,
  (select count(*) from expected_policy_seeds e where (select count(*) from nexa.organization_policies p where p."Id"=e."Id" and p."OrganizationId"=e."OrganizationId" and p."PolicyCode"=e."PolicyCode" and p."PolicyValue"=e."PolicyValue" and p."EffectiveFrom"=e."EffectiveFrom" and p."IsActive"=e."IsActive" and p."CreatedBy"=e."CreatedBy")<>1) as policy_seed_missing_count,
  (select count(*) from nexa.role_page_permissions p where p."CreatedBy"='migration-rev869a' and not exists (select 1 from expected_permission_seeds e where e."Id"=p."Id")) as permission_seed_unexpected_count,
  (select count(*) from expected_permission_seeds e where (select count(*) from nexa.role_page_permissions p where p."Id"=e."Id" and p."CreatedBy"='migration-rev869a')<>1) as permission_seed_missing_count,
  (select count(*) from expected_permission_seeds e join nexa.role_page_permissions p on p."Id"=e."Id" where row(p."CanView",p."CanCreate",p."CanUpdate",p."CanSubmit",p."CanVerify",p."CanApprove",p."CanReject",p."CanRequestClarification",p."CanRequestRevision",p."CanResubmit",p."CanCancel",p."CanDeactivate",p."CanPrint",p."CanDownload",p."CanExport",p."CanUploadAttachment",p."CanReplaceAttachment",p."CanViewCommercialValues",p."CanViewAuditHistory",p."HasFullControl") is distinct from row(e."CanView",e."CanCreate",e."CanUpdate",e."CanSubmit",e."CanVerify",e."CanApprove",e."CanReject",e."CanRequestClarification",e."CanRequestRevision",e."CanResubmit",e."CanCancel",e."CanDeactivate",e."CanPrint",e."CanDownload",e."CanExport",e."CanUploadAttachment",e."CanReplaceAttachment",e."CanViewCommercialValues",e."CanViewAuditHistory",e."HasFullControl")) as permission_flag_mismatch_count,
  (select count(*) from expected_permission_seeds e join nexa.role_page_permissions p on p."Id"=e."Id" where p."RoleId" is distinct from e."RoleId") as permission_role_mapping_mismatch_count,
  (select count(*) from expected_permission_seeds e join nexa.role_page_permissions p on p."Id"=e."Id" where p."PageDefinitionId" is distinct from e."PageDefinitionId") as permission_page_mapping_mismatch_count,
  (select count(*) from (select "RoleId","PageDefinitionId" from nexa.role_page_permissions where "CreatedBy"='migration-rev869a' group by "RoleId","PageDefinitionId" having count(*)<>1) d) as duplicate_role_page_permission_count
), seed_state as (
 select c.*,m.*,
  (role_seed_unexpected_count+role_seed_missing_count+page_seed_unexpected_count+page_seed_missing_count+policy_seed_unexpected_count+policy_seed_missing_count+permission_seed_unexpected_count+permission_seed_missing_count+permission_flag_mismatch_count+permission_role_mapping_mismatch_count+permission_page_mapping_mismatch_count+duplicate_role_page_permission_count) as seed_set_mismatch_count,
  (select count(*) from nexa.role_page_permissions p join nexa.roles r on r."Id"=p."RoleId" where r."Code"='DEPARTMENT_MANAGER' and not (p."CanView" or p."CanCreate" or p."CanUpdate" or p."CanSubmit" or p."CanVerify" or p."CanApprove" or p."CanReject" or p."CanRequestClarification" or p."CanRequestRevision" or p."CanResubmit" or p."CanCancel" or p."CanDeactivate" or p."CanPrint" or p."CanDownload" or p."CanExport" or p."CanUploadAttachment" or p."CanReplaceAttachment" or p."CanViewCommercialValues" or p."CanViewAuditHistory" or p."HasFullControl")) as all_false_department_manager_count,
  (select count(*) from nexa.role_page_permissions p join nexa.roles r on r."Id"=p."RoleId" join nexa.page_definitions d on d."Id"=p."PageDefinitionId" where r."Code"='DEPARTMENT_MANAGER' and p."CreatedBy"='migration-rev869a' and d."PageKey" in ('security.employee-identities','security.operational-scopes','masters.uoms','masters.uom-conversions','settings.tax-gst','masters.vendor-qualifications','masters.warehouse-condition-locations','qc.inspection-policies') and not (p."CanView" and p."CanPrint" and p."CanDownload" and p."CanViewAuditHistory" and not (p."CanCreate" or p."CanUpdate" or p."CanSubmit" or p."CanVerify" or p."CanApprove" or p."CanReject" or p."CanRequestClarification" or p."CanRequestRevision" or p."CanResubmit" or p."CanCancel" or p."CanDeactivate" or p."CanExport" or p."CanUploadAttachment" or p."CanReplaceAttachment" or p."CanViewCommercialValues" or p."HasFullControl"))) as department_manager_permission_mismatch_count,
  (select count(*) from (values ('PURCHASE_MANAGER'),('STORES_MANAGER'),('QC_MANAGER'),('QC_INSPECTOR'),('DEPARTMENT_MANAGER')) e(code) where (select count(*) from nexa.roles r where upper(trim(r."Code"))=e.code)<>1) as logical_role_code_mismatch_count
 from seed_counts c cross join seed_metrics m), approved_uom_state as (
 select
  (select count(*) from nexa.uoms where "Id"='f71a4725-bb15-e7bf-e97b-991985e96328'::uuid and "Code"='EA' and "Name"='Each' and "MeasurementDimension"='COUNT' and "QuantityPrecision"=0 and "IsActive" and "CreatedBy"='migration-rev869a' and "Version"=0) as exact_ea_uom_count,
  (select count(*) from nexa.uoms where "Id"='f71a4725-bb15-e7bf-e97b-991985e96328'::uuid and not ("Code"='EA' and "Name"='Each' and "MeasurementDimension"='COUNT' and "QuantityPrecision"=0 and "IsActive" and "CreatedBy"='migration-rev869a')) as ea_uom_attribute_mismatch_count,
  (select count(*) from nexa.uoms where upper(trim("Code"))='EA' and "Id"<>'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid) as ea_uom_code_collision_count,
  (select count(*) from nexa.uoms where upper(trim("Name"))='EACH' and "Id"<>'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid) as ea_uom_name_collision_count,
  (select count(*) from nexa.items where "Id"='8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid and "ItemCode"='REV868C1-ITEM' and "UomId"='f71a4725-bb15-e7bf-e97b-991985e96328'::uuid and "BaseUomId"='f71a4725-bb15-e7bf-e97b-991985e96328'::uuid) as exact_item_ea_mapping_count,
  (select count(*) from nexa.controlled_configuration_histories where "Id"='0007efa3-4888-a87d-45ef-72cc55f4dd45'::uuid and "EntityType"='UOM' and "EntityId"='f71a4725-bb15-e7bf-e97b-991985e96328'::uuid and "Action"='MANAGEMENT_APPROVED_CREATE_AND_ITEM_BACKFILL' and "CorrelationId"='MGMT-REV869A-UOM-20260810-001' and "CreatedBy"='migration-rev869a' and "AfterJson"->>'UomId'='f71a4725-bb15-e7bf-e97b-991985e96328' and "AfterJson"->>'UomCode'='EA' and "AfterJson"->>'Name'='Each' and "AfterJson"->>'MeasurementDimension'='COUNT' and "AfterJson"->>'QuantityPrecision'='0' and "AfterJson"->>'IsCanonicalBase'='true' and "AfterJson"->>'ConversionPolicy'='IDENTITY_ONLY' and "AfterJson"->>'LifecycleAction'='CREATE' and "AfterJson"->>'ApprovalStatus'='APPROVED' and "AfterJson"->>'ManagementApprovalReference'='MGMT-REV869A-UOM-20260810-001' and "AfterJson"->>'ItemId'='8c428e59-db05-471d-a7e7-4f7dc1c13b54' and "AfterJson"->>'ItemCode'='REV868C1-ITEM' and "AfterJson"->>'MappingStatus'='APPROVED' and "AfterJson"->>'MappingBasis'='MANAGEMENT_APPROVED') as exact_ea_approval_history_count,
  (select count(*) from nexa.uoms where "CreatedBy"='migration-rev869a') as migration_created_uom_count,
  (select count(*) from nexa.items where "Id"='8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid and "UomId"='f71a4725-bb15-e7bf-e97b-991985e96328'::uuid and "BaseUomId"='f71a4725-bb15-e7bf-e97b-991985e96328'::uuid) as migration_updated_item_count,
  (select count(*) from nexa.controlled_configuration_histories where "CreatedBy"='migration-rev869a') as migration_created_uom_history_count
), backup_state as (
 select
  (select count(*) from nexa.rev869a_items_prechange_backup b full join nexa.items i on i."Id"=b."Id" where b."Id" is null or i."Id" is null or (i."Id"<>'8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid and (to_jsonb(i)-'BaseUomId') is distinct from to_jsonb(b)) or (i."Id"='8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid and (to_jsonb(i)-array['BaseUomId','UomId']) is distinct from (to_jsonb(b)-'UomId'))) as item_backup_mismatch_count,
  (select count(*) from nexa.rev869a_uoms_prechange_backup b full join (select * from nexa.uoms where "Id"<>'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid) u on u."Id"=b."Id" where b."Id" is null or u."Id" is null or (to_jsonb(u)-array['MeasurementDimension','QuantityPrecision']) is distinct from to_jsonb(b)) as uom_backup_mismatch_count,
  (select count(*) from nexa.rev869a_vendors_prechange_backup b full join nexa.vendors v on v."Id"=b."Id" where b."Id" is null or v."Id" is null or (to_jsonb(v)-array['CommercialVerificationStatus','CommercialVerifiedAt','CommercialVerifiedBy','EffectiveFrom','EffectiveTo','RequiresReverification']) is distinct from to_jsonb(b)) as vendor_backup_mismatch_count,
  (select abs((select count(*) from nexa.rev869a_items_prechange_backup)-(select count(*) from nexa.items)) + abs(((select count(*) from nexa.rev869a_uoms_prechange_backup)+1)-(select count(*) from nexa.uoms)) + abs((select count(*) from nexa.rev869a_vendors_prechange_backup)-(select count(*) from nexa.vendors))) as backup_coverage_mismatch_count,
  (select count(*) from nexa.rev869a_uoms_prechange_backup where "Id"='f71a4725-bb15-e7bf-e97b-991985e96328'::uuid or upper(trim("Code"))='EA' or upper(trim("Name"))='EACH') as preexisting_ea_backup_collision_count), preservation_state as (
 select
  (select count(*) from nexa.purchase_requisitions) as pr_count,
  (select count(*) from nexa.purchase_requisition_approval_history) as pr_approval_history_count,
  (select count(*) from nexa.stock_reservations) as reservation_count,
  (select count(*) from nexa.employees where "Status"='Active') as active_employee_count,
  (select count(*) from nexa.departments) as department_count,
  (select count(*) from nexa.department_approval_mappings) as manager_mapping_count
)
select 'target_migration_count='||target_count from schema_state
union all select 'migration_count='||migration_count from schema_state
union all select 'missing_migration_count='||missing_migration_count from schema_state
union all select 'unexpected_migration_count='||unexpected_migration_count from schema_state
union all select 'duplicate_migration_count='||duplicate_migration_count from schema_state
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
union all select 'existing_department_manager_role_count='||existing_department_manager_role_count from role_state
union all select 'existing_department_manager_active_count='||existing_department_manager_active_count from role_state
union all select 'existing_department_manager_duplicate_count='||existing_department_manager_duplicate_count from role_state
union all select 'existing_department_manager_reuse_state='||existing_department_manager_reuse_state from role_state
union all select 'department_manager_role_fingerprint='||department_manager_role_fingerprint from role_state
union all select 'role_seed_count='||role_seed_count from seed_state
union all select 'page_seed_count='||page_seed_count from seed_state
union all select 'permission_seed_count='||permission_seed_count from seed_state
union all select 'policy_seed_count='||policy_seed_count from seed_state
union all select 'role_seed_unexpected_count='||role_seed_unexpected_count from seed_state
union all select 'role_seed_missing_count='||role_seed_missing_count from seed_state
union all select 'page_seed_unexpected_count='||page_seed_unexpected_count from seed_state
union all select 'page_seed_missing_count='||page_seed_missing_count from seed_state
union all select 'policy_seed_unexpected_count='||policy_seed_unexpected_count from seed_state
union all select 'policy_seed_missing_count='||policy_seed_missing_count from seed_state
union all select 'permission_seed_unexpected_count='||permission_seed_unexpected_count from seed_state
union all select 'permission_seed_missing_count='||permission_seed_missing_count from seed_state
union all select 'permission_flag_mismatch_count='||permission_flag_mismatch_count from seed_state
union all select 'permission_role_mapping_mismatch_count='||permission_role_mapping_mismatch_count from seed_state
union all select 'permission_page_mapping_mismatch_count='||permission_page_mapping_mismatch_count from seed_state
union all select 'duplicate_role_page_permission_count='||duplicate_role_page_permission_count from seed_state
union all select 'seed_set_mismatch_count='||seed_set_mismatch_count from seed_state
union all select 'migration_owned_seed_count='||(role_seed_count+page_seed_count+permission_seed_count+policy_seed_count) from seed_state
union all select 'security_configuration_owned_seed_count='||(role_seed_count+page_seed_count+permission_seed_count+policy_seed_count) from seed_state
union all select 'migration_created_uom_count='||migration_created_uom_count from approved_uom_state
union all select 'migration_updated_item_count='||migration_updated_item_count from approved_uom_state
union all select 'migration_created_uom_history_count='||migration_created_uom_history_count from approved_uom_state
union all select 'total_inserted_migration_owned_row_count='||(role_seed_count+page_seed_count+permission_seed_count+policy_seed_count+migration_created_uom_count+migration_created_uom_history_count) from seed_state cross join approved_uom_state
union all select 'exact_ea_uom_count='||exact_ea_uom_count from approved_uom_state
union all select 'ea_uom_attribute_mismatch_count='||ea_uom_attribute_mismatch_count from approved_uom_state
union all select 'ea_uom_code_collision_count='||ea_uom_code_collision_count from approved_uom_state
union all select 'ea_uom_name_collision_count='||ea_uom_name_collision_count from approved_uom_state
union all select 'exact_item_ea_mapping_count='||exact_item_ea_mapping_count from approved_uom_state
union all select 'exact_ea_approval_history_count='||exact_ea_approval_history_count from approved_uom_state
union all select 'all_false_department_manager_count='||all_false_department_manager_count from seed_state
union all select 'department_manager_permission_mismatch_count='||department_manager_permission_mismatch_count from seed_state
union all select 'logical_role_code_mismatch_count='||logical_role_code_mismatch_count from seed_state
union all select 'item_backup_mismatch_count='||item_backup_mismatch_count from backup_state
union all select 'uom_backup_mismatch_count='||uom_backup_mismatch_count from backup_state
union all select 'vendor_backup_mismatch_count='||vendor_backup_mismatch_count from backup_state
union all select 'backup_coverage_mismatch_count='||backup_coverage_mismatch_count from backup_state
union all select 'preexisting_ea_backup_collision_count='||preexisting_ea_backup_collision_count from backup_state
union all select 'relieved_employee_expected_count='||relieved_employee_expected_count from relieved_employee_state
union all select 'relieved_employee_actual_matched_count='||relieved_employee_actual_matched_count from relieved_employee_state
union all select 'relieved_employee_missing_count='||relieved_employee_missing_count from relieved_employee_state
union all select 'relieved_employee_unexpected_count='||relieved_employee_unexpected_count from relieved_employee_state
union all select 'relieved_employee_duplicate_count='||relieved_employee_duplicate_count from relieved_employee_state
union all select 'relieved_employee_status_mismatch_count='||relieved_employee_status_mismatch_count from relieved_employee_state
union all select 'relieved_employee_acceptance_state='||relieved_employee_acceptance_state from relieved_employee_state
union all select 'preserve_pr_count='||pr_count from preservation_state
union all select 'preserve_pr_approval_history_count='||pr_approval_history_count from preservation_state
union all select 'preserve_reservation_count='||reservation_count from preservation_state
union all select 'preserve_active_employee_count='||active_employee_count from preservation_state
union all select 'preserve_department_count='||department_count from preservation_state
union all select 'preserve_manager_mapping_count='||manager_mapping_count from preservation_state
union all select 'database_schema_acceptance_state='||case when target_count=1 and migration_count=12 and missing_migration_count=0 and unexpected_migration_count=0 and duplicate_migration_count=0 and foundation_table_count=9 and backup_table_count=3 and null_safe_index_count=7 and composite_integrity_count=3 and primary_key_count=9 and restrictive_fk_count=15 and check_constraint_count=22 and guard_trigger_count>=10 and actual_column_count=149 and table_shape_mismatch_count=0 and base_uom_column_count=1 and uom_backfill_mismatch_count=0 and tax_resolution_mismatch_count=0 and existing_department_manager_reuse_state='PASS' and role_seed_count=4 and page_seed_count=8 and permission_seed_count=74 and policy_seed_count=2 and (role_seed_count+page_seed_count+permission_seed_count+policy_seed_count)=88 and seed_set_mismatch_count=0 and logical_role_code_mismatch_count=0 and duplicate_role_page_permission_count=0 and all_false_department_manager_count=0 and department_manager_permission_mismatch_count=0 and exact_ea_uom_count=1 and ea_uom_attribute_mismatch_count=0 and ea_uom_code_collision_count=0 and ea_uom_name_collision_count=0 and exact_item_ea_mapping_count=1 and exact_ea_approval_history_count=1 and migration_created_uom_count=1 and migration_updated_item_count=1 and migration_created_uom_history_count=1 and (role_seed_count+page_seed_count+permission_seed_count+policy_seed_count+migration_created_uom_count+migration_created_uom_history_count)=90 and item_backup_mismatch_count=0 and uom_backup_mismatch_count=0 and vendor_backup_mismatch_count=0 and backup_coverage_mismatch_count=0 and preexisting_ea_backup_collision_count=0 and relieved_employee_acceptance_state='PASS' then 'PASS' else 'FAIL' end from schema_state cross join column_state cross join role_state cross join seed_state cross join approved_uom_state cross join backup_state cross join relieved_employee_state
union all select 'column_contract='||table_name||'.'||column_name||'|type='||data_type||'|udt='||udt_name||'|nullable='||is_nullable from information_schema.columns where table_schema='nexa' and table_name in (select name from expected_relations)
union all select 'constraint_contract='||c.conname||'|type='||c.contype::text||'|definition='||pg_get_constraintdef(c.oid) from pg_constraint c where c.connamespace='nexa'::regnamespace and (c.conrelid in (select ('nexa.'||name)::regclass from expected_relations) or c.conname in ('AK_rack_bins_WarehouseId_Id','FK_items_uoms_BaseUomId'))
union all select 'index_contract='||indexname||'|definition='||indexdef from pg_indexes where schemaname='nexa' and (tablename in (select name from expected_relations) or indexname='IX_items_BaseUomId')
union all select 'seed_contract=roles|'||"Id"::text from nexa.roles where "CreatedBy"='migration-rev869a'
union all select 'seed_contract=page_definitions|'||"Id"::text from nexa.page_definitions where "CreatedBy"='migration-rev869a'
union all select 'seed_contract=role_page_permissions|'||"Id"::text from nexa.role_page_permissions where "CreatedBy"='migration-rev869a'
union all select 'seed_contract=organization_policies|'||"Id"::text from nexa.organization_policies where "CreatedBy"='migration-rev869a';
"@.Trim()
}

function Get-TransactionalVerificationSql {
    return @'
begin;
with expected(code) as (values
 ('SESS-001'),('SESS-002'),('SESS-003'),('SESS-004'),('SESS-005'),('SESS-006'),('SESS-007'),('SESS-008'),('SESS-009'),('SESS-010'),('SESS-011'),('SESS-012'),('SESS-013'),('SESS-014'),('SESS-015'),('SESS-017'),('SESS-019'),('SESS-020'),('SESS-021'),('SESS-023'),('SESS-024'),('SESS-025'),('SESS-026'),('SESS-029'),('SESS-030'),('SESS-031'),('SESS-033'),('SESS-034'),('SESS-035'),('SESS-038'),('SESS-040'),('SESS-041'),('SESS-042'),('SESS-043'),('SESS-044'),('SESS-045'),('SESS-046'),('SESS-047'),('SESS-048'),('SESS-049'),('SESS-050'),('SESS-051')
), metrics as (
 select
  (select count(*) from expected) expected_count,
  (select count(*) from nexa.employees e join expected x on x.code=e."EmployeeCode" where lower(trim(e."Status"))='active') actual_matched_count,
  (select count(*) from expected x where not exists (select 1 from nexa.employees e where e."EmployeeCode"=x.code)) missing_count,
  (select count(*) from nexa.employees e where e."EmployeeCode" like 'SESS-%' and lower(trim(e."Status"))='active' and not exists (select 1 from expected x where x.code=e."EmployeeCode")) unexpected_count,
  (select count(*) from (select e."EmployeeCode" from nexa.employees e join expected x on x.code=e."EmployeeCode" group by e."EmployeeCode" having count(*)<>1) d) duplicate_count,
  (select count(*) from expected x where exists (select 1 from nexa.employees e where e."EmployeeCode"=x.code) and not exists (select 1 from nexa.employees e where e."EmployeeCode"=x.code and lower(trim(e."Status"))='active')) status_mismatch_count
)
select 'transactional_prerequisite_active_employee_expected_count='||expected_count from metrics
union all select 'transactional_prerequisite_active_employee_actual_matched_count='||actual_matched_count from metrics
union all select 'transactional_prerequisite_active_employee_missing_count='||missing_count from metrics
union all select 'transactional_prerequisite_active_employee_unexpected_count='||unexpected_count from metrics
union all select 'transactional_prerequisite_active_employee_duplicate_count='||duplicate_count from metrics
union all select 'transactional_prerequisite_active_employee_status_mismatch_count='||status_mismatch_count from metrics
union all select 'transactional_prerequisite_active_employee_state='||case when expected_count=42 and actual_matched_count=42 and missing_count=0 and unexpected_count=0 and duplicate_count=0 and status_mismatch_count=0 then 'PASS' else 'FAIL' end from metrics;
select 'transactional_prerequisite_existing_vendor_count='||count(*) from nexa.vendors;
select 'transactional_prerequisite_existing_vendor_state=NOT_REQUIRED_TEST_OWNED';

select 'transactional_prerequisite_identity_collision_count='||count(*) from nexa.employee_identity_mappings where "Id" in ('869a0000-0000-0000-0000-000000000001','869a0000-0000-0000-0000-000000000002') or ("Issuer"='https://offline.invalid' and "Subject"='rev869a-test-subject');
select 'transactional_prerequisite_identity_collision_state='||case when count(*)=0 then 'PASS' else 'FAIL' end from nexa.employee_identity_mappings where "Id" in ('869a0000-0000-0000-0000-000000000001','869a0000-0000-0000-0000-000000000002') or ("Issuer"='https://offline.invalid' and "Subject"='rev869a-test-subject');
do $rev869a_identity$
declare e uuid; expected_codes text[] := array['SESS-001','SESS-002','SESS-003','SESS-004','SESS-005','SESS-006','SESS-007','SESS-008','SESS-009','SESS-010','SESS-011','SESS-012','SESS-013','SESS-014','SESS-015','SESS-017','SESS-019','SESS-020','SESS-021','SESS-023','SESS-024','SESS-025','SESS-026','SESS-029','SESS-030','SESS-031','SESS-033','SESS-034','SESS-035','SESS-038','SESS-040','SESS-041','SESS-042','SESS-043','SESS-044','SESS-045','SESS-046','SESS-047','SESS-048','SESS-049','SESS-050','SESS-051']; expected_count bigint; matched_count bigint; missing_count bigint; unexpected_count bigint; duplicate_count bigint; status_mismatch_count bigint; collision_count bigint; failed boolean;
begin
 expected_count:=cardinality(expected_codes);
 select count(*) into matched_count from nexa.employees e where e."EmployeeCode"=any(expected_codes) and lower(trim(e."Status"))='active';
 select count(*) into missing_count from unnest(expected_codes) x(code) where not exists (select 1 from nexa.employees e where e."EmployeeCode"=x.code);
 select count(*) into unexpected_count from nexa.employees e where e."EmployeeCode" like 'SESS-%' and lower(trim(e."Status"))='active' and not (e."EmployeeCode"=any(expected_codes));
 select count(*) into duplicate_count from (select e."EmployeeCode" from nexa.employees e where e."EmployeeCode"=any(expected_codes) group by e."EmployeeCode" having count(*)<>1) d;
 select count(*) into status_mismatch_count from unnest(expected_codes) x(code) where exists (select 1 from nexa.employees e where e."EmployeeCode"=x.code) and not exists (select 1 from nexa.employees e where e."EmployeeCode"=x.code and lower(trim(e."Status"))='active');
 if expected_count<>42 or matched_count<>42 or missing_count<>0 or unexpected_count<>0 or duplicate_count<>0 or status_mismatch_count<>0 then raise exception 'transactional_prerequisite_failed=active_employee|expected_count=%|actual_matched_count=%|missing_count=%|unexpected_count=%|duplicate_count=%|status_mismatch_count=%',expected_count,matched_count,missing_count,unexpected_count,duplicate_count,status_mismatch_count; end if;
 select count(*) into collision_count from nexa.employee_identity_mappings where "Id" in ('869a0000-0000-0000-0000-000000000001','869a0000-0000-0000-0000-000000000002') or ("Issuer"='https://offline.invalid' and "Subject"='rev869a-test-subject');
 if collision_count<>0 then raise exception 'transactional_prerequisite_failed=identity_collision|expected_count=0|actual_count=%',collision_count; end if;
 select "Id" into e from nexa.employees where "EmployeeCode"=any(expected_codes) and lower(trim("Status"))='active' order by "EmployeeCode" limit 1;
 insert into nexa.employee_identity_mappings ("Id","OrganizationId","Issuer","Subject","EmployeeId","IdentityType","EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000001','SESS','https://offline.invalid','rev869a-test-subject',e,'HUMAN',current_date,true,now(),'REV869A_TEST',0);
 failed:=false; begin insert into nexa.employee_identity_mappings ("Id","OrganizationId","Issuer","Subject","EmployeeId","IdentityType","EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000002','OTHER','https://offline.invalid','rev869a-test-subject',e,'HUMAN',current_date,true,now(),'REV869A_TEST',0); exception when unique_violation then failed:=true; end;
 if not failed then raise exception 'transactional_constraint_failed=duplicate_identity'; end if;
end
$rev869a_identity$;
select 'transactional_constraint_identity_state=PASS';

select 'transactional_prerequisite_uom_collision_count='||((select count(*) from nexa.uoms where "Id" in ('869a0000-0000-0000-0000-000000000097','869a0000-0000-0000-0000-000000000098') or upper(trim("Code")) in ('REV869A_TEST_FROM','REV869A_TEST_TO'))+(select count(*) from nexa.uom_conversions where "Id"='869a0000-0000-0000-0000-000000000003'));
select 'transactional_prerequisite_uom_collision_state='||case when ((select count(*) from nexa.uoms where "Id" in ('869a0000-0000-0000-0000-000000000097','869a0000-0000-0000-0000-000000000098') or upper(trim("Code")) in ('REV869A_TEST_FROM','REV869A_TEST_TO'))+(select count(*) from nexa.uom_conversions where "Id"='869a0000-0000-0000-0000-000000000003'))=0 then 'PASS' else 'FAIL' end;
do $rev869a_uom$
declare from_u uuid := '869a0000-0000-0000-0000-000000000097'; to_u uuid := '869a0000-0000-0000-0000-000000000098'; collision_count bigint; failed boolean;
begin
 select (select count(*) from nexa.uoms where "Id" in (from_u,to_u) or upper(trim("Code")) in ('REV869A_TEST_FROM','REV869A_TEST_TO'))+(select count(*) from nexa.uom_conversions where "Id"='869a0000-0000-0000-0000-000000000003') into collision_count;
 if collision_count<>0 then raise exception 'transactional_prerequisite_failed=uom_collision|expected_count=0|actual_count=%',collision_count; end if;
 insert into nexa.uoms ("Id","Code","Name","IsActive","CreatedAt","CreatedBy","Version","MeasurementDimension","QuantityPrecision") values (from_u,'REV869A_TEST_FROM','REV869A rolled-back from UOM',true,now(),'REV869A_TEST',0,'TEST',6),(to_u,'REV869A_TEST_TO','REV869A rolled-back to UOM',true,now(),'REV869A_TEST',0,'TEST',6);
 failed:=false; begin insert into nexa.uom_conversions ("Id","OrganizationId","FromUomId","ToUomId","MeasurementDimension","ConversionFactor","QuantityPrecision","EffectiveFrom","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000003','SESS',from_u,to_u,'TEST',0,6,current_date,'PendingApproval',true,now(),'REV869A_TEST',0); exception when check_violation then failed:=true; end;
 if not failed then raise exception 'transactional_constraint_failed=invalid_uom_factor'; end if;
end
$rev869a_uom$;
select 'transactional_constraint_uom_state=PASS';

select 'transactional_prerequisite_tax_collision_count='||count(*) from nexa.tax_gst_settings where "Id"='869a0000-0000-0000-0000-000000000004' or ("OrganizationId"='SESS' and "HsnSacCode"='REV869A_TEST');
select 'transactional_prerequisite_tax_collision_state='||case when count(*)=0 then 'PASS' else 'FAIL' end from nexa.tax_gst_settings where "Id"='869a0000-0000-0000-0000-000000000004' or ("OrganizationId"='SESS' and "HsnSacCode"='REV869A_TEST');
do $rev869a_tax$
declare collision_count bigint; failed boolean;
begin
 select count(*) into collision_count from nexa.tax_gst_settings where "Id"='869a0000-0000-0000-0000-000000000004' or ("OrganizationId"='SESS' and "HsnSacCode"='REV869A_TEST');
 if collision_count<>0 then raise exception 'transactional_prerequisite_failed=tax_collision|expected_count=0|actual_count=%',collision_count; end if;
 failed:=false; begin insert into nexa.tax_gst_settings ("Id","OrganizationId","JurisdictionCode","HsnSacCode","SupplierStateCode","PlaceOfSupplyStateCode","SupplyType","VendorRegistrationType","GstRate","CgstRate","SgstRate","IgstRate","CessRate","IsExempt","IsReverseCharge","CurrencyCode","RoundingScale","EffectiveFrom","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000004','SESS','IN','REV869A_TEST','27','29','INTRASTATE','REGISTERED',18,9,9,0,0,false,false,'INR',2,current_date,'PendingApproval',true,now(),'REV869A_TEST',0); exception when check_violation then failed:=true; end;
 if not failed then raise exception 'transactional_constraint_failed=invalid_state_gst'; end if;
end
$rev869a_tax$;
select 'transactional_constraint_tax_state=PASS';

select 'transactional_prerequisite_qc_collision_count='||((select count(*) from nexa.uoms where "Id"='869a0000-0000-0000-0000-000000000096' or upper(trim("Code"))='REV869A_QC_TEST')+(select count(*) from nexa.qc_inspection_policies where "Id"='869a0000-0000-0000-0000-000000000005' or ("OrganizationId"='SESS' and "ParameterCode"='REV869A_TEST')));
select 'transactional_prerequisite_qc_collision_state='||case when ((select count(*) from nexa.uoms where "Id"='869a0000-0000-0000-0000-000000000096' or upper(trim("Code"))='REV869A_QC_TEST')+(select count(*) from nexa.qc_inspection_policies where "Id"='869a0000-0000-0000-0000-000000000005' or ("OrganizationId"='SESS' and "ParameterCode"='REV869A_TEST')))=0 then 'PASS' else 'FAIL' end;
do $rev869a_qc$
declare qc_u uuid := '869a0000-0000-0000-0000-000000000096'; collision_count bigint; failed boolean;
begin
 select (select count(*) from nexa.uoms where "Id"=qc_u or upper(trim("Code"))='REV869A_QC_TEST')+(select count(*) from nexa.qc_inspection_policies where "Id"='869a0000-0000-0000-0000-000000000005' or ("OrganizationId"='SESS' and "ParameterCode"='REV869A_TEST')) into collision_count;
 if collision_count<>0 then raise exception 'transactional_prerequisite_failed=qc_collision|expected_count=0|actual_count=%',collision_count; end if;
 insert into nexa.uoms ("Id","Code","Name","IsActive","CreatedAt","CreatedBy","Version","MeasurementDimension","QuantityPrecision") values (qc_u,'REV869A_QC_TEST','REV869A rolled-back QC UOM',true,now(),'REV869A_TEST',0,'TEST',6);
 failed:=false; begin insert into nexa.qc_inspection_policies ("Id","OrganizationId","ParameterCode","MeasurementUomId","InspectionMethod","SampleSize","EffectiveFrom","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000005','SESS','REV869A_TEST',qc_u,'TEST',1,current_date,'PendingApproval',true,now(),'REV869A_TEST',0); exception when check_violation then failed:=true; end;
 if not failed then raise exception 'transactional_constraint_failed=qc_missing_owner'; end if;
end
$rev869a_qc$;
select 'transactional_constraint_qc_state=PASS';

select 'transactional_prerequisite_vendor_collision_count='||((select count(*) from nexa.vendors where "Id"='869a0000-0000-0000-0000-000000000095' or upper(trim("VendorCode"))='REV869A_TEST_VENDOR')+(select count(*) from nexa.vendor_qualifications where "Id"='869a0000-0000-0000-0000-000000000008' or ("OrganizationId"='SESS' and "QualificationCode"='REV869A_TEST')));
select 'transactional_prerequisite_vendor_collision_state='||case when ((select count(*) from nexa.vendors where "Id"='869a0000-0000-0000-0000-000000000095' or upper(trim("VendorCode"))='REV869A_TEST_VENDOR')+(select count(*) from nexa.vendor_qualifications where "Id"='869a0000-0000-0000-0000-000000000008' or ("OrganizationId"='SESS' and "QualificationCode"='REV869A_TEST')))=0 then 'PASS' else 'FAIL' end;
do $rev869a_vendor$
declare test_v uuid := '869a0000-0000-0000-0000-000000000095'; collision_count bigint; failed boolean;
begin
 select (select count(*) from nexa.vendors where "Id"=test_v or upper(trim("VendorCode"))='REV869A_TEST_VENDOR')+(select count(*) from nexa.vendor_qualifications where "Id"='869a0000-0000-0000-0000-000000000008' or ("OrganizationId"='SESS' and "QualificationCode"='REV869A_TEST')) into collision_count;
 if collision_count<>0 then raise exception 'transactional_prerequisite_failed=vendor_collision|expected_count=0|actual_count=%',collision_count; end if;
 insert into nexa.vendors ("Id","VendorCode","Name","LegalVendorName","VendorType","Country","PortalOrganizationId","ApprovalStatus","VendorStatus","CommercialVerificationStatus","EffectiveFrom","RequiresReverification","IsVendorCodeLocked","MsmeStatus","IsActive","CreatedAt","CreatedBy","Version") values (test_v,'REV869A_TEST_VENDOR','REV869A rolled-back vendor','REV869A rolled-back vendor','TEST','India','REV869A_TEST','Draft','Draft','Draft',current_date,false,false,false,true,now(),'REV869A_TEST',0);
 failed:=false; begin insert into nexa.vendor_qualifications ("Id","OrganizationId","VendorId","QualificationCode","EffectiveFrom","EffectiveTo","VerificationStatus","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000008','SESS',test_v,'REV869A_TEST',current_date,current_date-1,'PendingApproval','PendingApproval',true,now(),'REV869A_TEST',0); exception when check_violation then failed:=true; end;
 if not failed then raise exception 'transactional_constraint_failed=invalid_vendor_qualification_dates'; end if;
end
$rev869a_vendor$;
select 'transactional_constraint_vendor_state=PASS';

select 'transactional_prerequisite_warehouse_collision_count='||((select count(*) from nexa.warehouses where "Id" in ('869a0000-0000-0000-0000-000000000093','869a0000-0000-0000-0000-000000000094') or upper(trim("WarehouseCode")) in ('REV869A_TEST_WH_A','REV869A_TEST_WH_B'))+(select count(*) from nexa.rack_bins where "Id"='869a0000-0000-0000-0000-000000000092' or upper(trim("BinCode"))='REV869A_TEST_BIN')+(select count(*) from nexa.warehouse_condition_locations where "Id"='869a0000-0000-0000-0000-000000000006'));
select 'transactional_prerequisite_warehouse_collision_state='||case when ((select count(*) from nexa.warehouses where "Id" in ('869a0000-0000-0000-0000-000000000093','869a0000-0000-0000-0000-000000000094') or upper(trim("WarehouseCode")) in ('REV869A_TEST_WH_A','REV869A_TEST_WH_B'))+(select count(*) from nexa.rack_bins where "Id"='869a0000-0000-0000-0000-000000000092' or upper(trim("BinCode"))='REV869A_TEST_BIN')+(select count(*) from nexa.warehouse_condition_locations where "Id"='869a0000-0000-0000-0000-000000000006'))=0 then 'PASS' else 'FAIL' end;
do $rev869a_warehouse$
declare wh_a uuid := '869a0000-0000-0000-0000-000000000093'; wh_b uuid := '869a0000-0000-0000-0000-000000000094'; test_rb uuid := '869a0000-0000-0000-0000-000000000092'; collision_count bigint; failed boolean;
begin
 select (select count(*) from nexa.warehouses where "Id" in (wh_a,wh_b) or upper(trim("WarehouseCode")) in ('REV869A_TEST_WH_A','REV869A_TEST_WH_B'))+(select count(*) from nexa.rack_bins where "Id"=test_rb or upper(trim("BinCode"))='REV869A_TEST_BIN')+(select count(*) from nexa.warehouse_condition_locations where "Id"='869a0000-0000-0000-0000-000000000006') into collision_count;
 if collision_count<>0 then raise exception 'transactional_prerequisite_failed=warehouse_collision|expected_count=0|actual_count=%',collision_count; end if;
 insert into nexa.warehouses ("Id","WarehouseCode","Name","WarehouseType","Status","ApprovalStatus","IsWarehouseCodeLocked","IsActive","CreatedAt","CreatedBy","Version") values (wh_a,'REV869A_TEST_WH_A','REV869A rolled-back warehouse A','TEST','Draft','Draft',false,true,now(),'REV869A_TEST',0),(wh_b,'REV869A_TEST_WH_B','REV869A rolled-back warehouse B','TEST','Draft','Draft',false,true,now(),'REV869A_TEST',0);
 insert into nexa.rack_bins ("Id","WarehouseId","BinCode","RackName","BinNameNumber","LocationType","MaterialCondition","Status","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") values (test_rb,wh_a,'REV869A_TEST_BIN','REV869A_TEST_RACK','REV869A_TEST_BIN','TEST','AVAILABLE','Draft','Draft',true,now(),'REV869A_TEST',0);
 failed:=false; begin insert into nexa.warehouse_condition_locations ("Id","OrganizationId","WarehouseId","RackBinId","ConditionCode","EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000006','SESS',wh_b,test_rb,'AVAILABLE',current_date,true,now(),'REV869A_TEST',0); exception when foreign_key_violation then failed:=true; end;
 if not failed then raise exception 'transactional_constraint_failed=cross_warehouse_rack_bin'; end if;
end
$rev869a_warehouse$;
select 'transactional_constraint_warehouse_state=PASS';

select 'transactional_prerequisite_history_collision_count='||count(*) from nexa.controlled_configuration_histories where "Id"='869a0000-0000-0000-0000-000000000007' or "CorrelationId"='REV869A_TEST_HISTORY';
select 'transactional_prerequisite_history_collision_state='||case when count(*)=0 then 'PASS' else 'FAIL' end from nexa.controlled_configuration_histories where "Id"='869a0000-0000-0000-0000-000000000007' or "CorrelationId"='REV869A_TEST_HISTORY';
do $rev869a_history$
declare collision_count bigint; failed boolean;
begin
 select count(*) into collision_count from nexa.controlled_configuration_histories where "Id"='869a0000-0000-0000-0000-000000000007' or "CorrelationId"='REV869A_TEST_HISTORY';
 if collision_count<>0 then raise exception 'transactional_prerequisite_failed=history_collision|expected_count=0|actual_count=%',collision_count; end if;
 insert into nexa.controlled_configuration_histories ("Id","OrganizationId","EntityType","EntityId","Action","ActorLoginId","ActorRoleCode","Remarks","CorrelationId","CreatedAt","CreatedBy","Version") values ('869a0000-0000-0000-0000-000000000007','SESS','TEST','869a0000-0000-0000-0000-000000000007','Create','REV869A_TEST','TEST','TEST','REV869A_TEST_HISTORY',now(),'REV869A_TEST',0);
 failed:=false; begin update nexa.controlled_configuration_histories set "Remarks"='REWRITE' where "Id"='869a0000-0000-0000-0000-000000000007'; exception when raise_exception then failed:=true; end;
 if not failed then raise exception 'transactional_constraint_failed=configuration_history_mutation'; end if;
end
$rev869a_history$;
select 'transactional_constraint_history_state=PASS';
select 'transactional_constraint_test_state=PASS';
rollback;
'@.Trim()
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
    $separator = $RequiredLine.IndexOf('=')
    if ($separator -lt 1) { throw "Required evidence contract is malformed: $RequiredLine" }
    $label = $RequiredLine.Substring(0, $separator)
    $expected = $RequiredLine.Substring($separator + 1)
    $matches = [regex]::Matches($Evidence, '(?m)^' + [regex]::Escape($label) + '=(.*)$')
    $actual = if ($matches.Count -eq 0) { 'MISSING' } elseif ($matches.Count -gt 1) { 'DUPLICATE' } else { $matches[0].Groups[1].Value }
    if ($matches.Count -ne 1 -or $actual -cne $expected) {
        throw "Required evidence mismatch:`nlabel=$label`nexpected=$expected`nactual=$actual"
    }
}

function Get-EvidenceValue([string]$Evidence, [string]$Key) {
    $matches = [regex]::Matches($Evidence, '(?m)^' + [regex]::Escape($Key) + '=(\d+)$')
    if ($matches.Count -ne 1) { throw "Numeric evidence key must occur exactly once and contain digits only: $Key" }
    return [long]$matches[0].Groups[1].Value
}

function Get-EvidenceTextValue([string]$Evidence, [string]$Key) {
    $matches = [regex]::Matches($Evidence, '(?m)^' + [regex]::Escape($Key) + '=([^\r\n]+)$')
    if ($matches.Count -ne 1) { throw "Evidence key must occur exactly once: $Key" }
    return $matches[0].Groups[1].Value
}

function Assert-Preservation([string]$Before, [string]$After) {
    foreach ($key in @('preserve_pr_count','preserve_pr_approval_history_count','preserve_reservation_count','preserve_active_employee_count','relieved_employee_expected_count','relieved_employee_actual_matched_count','relieved_employee_missing_count','relieved_employee_unexpected_count','relieved_employee_duplicate_count','relieved_employee_status_mismatch_count','preserve_department_count','preserve_manager_mapping_count')) {
        if ((Get-EvidenceValue $Before $key) -ne (Get-EvidenceValue $After $key)) { throw "REV868/REV868C3 preservation failed for $key." }
    }
    if ((Get-EvidenceTextValue $Before 'department_manager_role_fingerprint') -cne (Get-EvidenceTextValue $After 'department_manager_role_fingerprint')) { throw "Reused DEPARTMENT_MANAGER role values changed." }
}

function Get-ApprovedPreApplyEvidence {
    if ([string]::IsNullOrWhiteSpace($ApprovedPreApplyEvidencePath) -or [string]::IsNullOrWhiteSpace($ApprovedPreApplyEvidenceSha256)) {
        throw "ResumePostApplyAcceptance requires approved pre-apply evidence path and SHA-256."
    }
    if ($ApprovedPreApplyEvidenceSha256 -notmatch '^[A-Fa-f0-9]{64}$') { throw "Approved pre-apply evidence SHA-256 is malformed." }
    $resolved = Resolve-Path -LiteralPath $ApprovedPreApplyEvidencePath -ErrorAction Stop
    $allowedRoot = [IO.Path]::GetFullPath((Join-Path $targetRoot "local-evidence\rev869a")) + [IO.Path]::DirectorySeparatorChar
    $fullPath = [IO.Path]::GetFullPath($resolved.Path)
    if (-not $fullPath.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw "Approved pre-apply evidence must be under local-evidence\rev869a." }
    $actualHash = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash
    if ($actualHash -cne $ApprovedPreApplyEvidenceSha256.ToUpperInvariant()) { throw "Approved pre-apply evidence SHA-256 does not match." }
    $content = [IO.File]::ReadAllText($fullPath)
    $sections = [regex]::Matches($content, '(?ms)^## Preflight evidence\r?\n```text\r?\n(?<evidence>.*?)\r?\n```')
    if ($sections.Count -ne 1) { throw "Approved pre-apply evidence must contain exactly one canonical Preflight evidence section." }
    $headerTarget = [regex]::Matches($content, '(?m)^- target_database=([^\r\n]+)$')
    $headerMigration = [regex]::Matches($content, '(?m)^- target_migration=([^\r\n]+)$')
    if ($headerTarget.Count -ne 1 -or $headerTarget[0].Groups[1].Value -cne $targetDatabase) { throw "Approved pre-apply evidence target is missing, duplicate, or conflicting." }
    if ($headerMigration.Count -ne 1 -or $headerMigration[0].Groups[1].Value -cne $targetMigration) { throw "Approved pre-apply evidence migration is missing, duplicate, or conflicting." }
    $evidence = $sections[0].Groups['evidence'].Value
    Assert-Evidence $evidence "database_identity=PASS"
    Assert-Evidence $evidence "target_migration_count=0"
    Assert-Evidence $evidence "preflight_acceptance_state=PASS"
    foreach ($key in @('preserve_pr_count','preserve_pr_approval_history_count','preserve_reservation_count','preserve_active_employee_count','preserve_department_count','preserve_manager_mapping_count')) { [void](Get-EvidenceValue $evidence $key) }
    return $evidence
}
function Write-Plan([string]$PreflightSql, [string]$PostSql) {
    Write-Output "REV869A GeneratePlanOnly"
    Write-Output "host=$HostName"
    Write-Output "port=$Port"
    Write-Output "user=$UserName"
    Write-Output "target_database=$targetDatabase"
    Write-Output "protected_databases=$($protectedDatabases -join ', ')"
    Write-Output "prerequisite_migrations_count=11"
    Write-Output "expected_final_migrations_count=12"
    for ($i=0; $i -lt $prerequisiteMigrations.Count; $i++) { Write-Output ("prerequisite_{0}={1}" -f ($i+1),$prerequisiteMigrations[$i]) }
    Write-Output "target_migration_only=$targetMigration"
    Write-Output "foundation_tables=$($foundationTables -join ', ')"
    Write-Output "migration_owned_backup_tables=$($backupTables -join ', ')"
    Write-Output "null_safe_unique_indexes=$($nullSafeIndexes -join ', ')"
    Write-Output "uom_management_decision_state=$uomManagementDecisionState"
    Write-Output "UOM readiness: management-approved exact plan creates only EA (f71a4725-bb15-e7bf-e97b-991985e96328) and maps only REV868C1-ITEM; raw null is allowed only when this exact approved plan covers it."
    Write-Output "Approved UOM contract: EA / Each / COUNT / precision 0 / canonical base / IDENTITY_ONLY / CREATE / APPROVED / MGMT-REV869A-UOM-20260810-001."
    Write-Output "Approved item mapping: 8c428e59-db05-471d-a7e7-4f7dc1c13b54 / REV868C1-ITEM to the exact EA UomId and BaseUomId; no other null-UOM item is permitted."
    Write-Output "Preflight SQL (SELECT-only/read-only):"
    Write-Output $PreflightSql
    Write-Output "Post-migration verification SQL (SELECT-only/read-only):"
    Write-Output $PostSql
    Write-Output "Full apply additionally runs transaction-rolled-back negative constraint tests and PostgreSQL-backed .NET tests."
    Write-Output "Role reuse: exactly one active pre-existing DEPARTMENT_MANAGER is required and preserved; REV869A creates exactly four new roles and never seeds or deletes DEPARTMENT_MANAGER."
    Write-Output "Seed contract: 4 REV869A roles + 8 pages + 74 permissions + 2 policies = 88 migration-owned rows; five logical REV869A role codes include the reused DEPARTMENT_MANAGER."
    Write-Output "Rollback design: Down deletes exactly 88 security/configuration rows, one EA approval-history row, and one migration-owned EA UOM; restores exactly one Item UomId to its backed-up NULL value; removes only REV869A-owned objects; preserves REV868/REV868C3 business/history rows; and drops rev869a_vendors_prechange_backup, rev869a_uoms_prechange_backup, and rev869a_items_prechange_backup last."
    Write-Output "Ownership totals: 88 security/configuration inserts + 1 EA UOM insert + 1 controlled approval-history insert = 90 inserted migration-owned rows; exactly 1 Item row is updated. Backup comparisons exclude only that approved mapping and prove every other Item/UOM unchanged."
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
        "- approved_preapply_evidence=$(if ($ApprovedPreApplyEvidencePath) { Split-Path -Leaf $ApprovedPreApplyEvidencePath } else { 'NOT_REQUIRED_FOR_MODE' })",
        "- approved_preapply_sha256=$(if ($ApprovedPreApplyEvidenceSha256) { $ApprovedPreApplyEvidenceSha256.ToUpperInvariant() } else { 'NOT_REQUIRED_FOR_MODE' })",
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

function Invoke-ResumeAcceptanceTests {
    $dotnet = Resolve-Executable $DotnetPath "dotnet.exe" "dotnet.exe"
    $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$script:plainPassword"
    $env:NexaErp__ExpectedDatabase = $Database
    $env:REV869A_POSTGRES = $env:ConnectionStrings__NexaErp
    Set-Location $targetRoot
    $trxDirectory = Join-Path $reportDirectory ("trx\" + $timestamp)
    New-Item -ItemType Directory -Force -Path $trxDirectory | Out-Null
    $trxPath = Join-Path $trxDirectory "rev869a_resume_acceptance.trx"
    $transactionalOutputPath = Join-Path $trxDirectory "rev869a_transactional_constraint_output.txt"
    $postgresTestOutputPath = Join-Path $trxDirectory "rev869a_postgresql_test_output.txt"
    try {
        $transactionalEvidence = Invoke-Psql (Get-TransactionalVerificationSql) $false
        foreach ($required in @(
            'transactional_prerequisite_active_employee_expected_count=42','transactional_prerequisite_active_employee_actual_matched_count=42',
            'transactional_prerequisite_active_employee_missing_count=0','transactional_prerequisite_active_employee_unexpected_count=0',
            'transactional_prerequisite_active_employee_duplicate_count=0','transactional_prerequisite_active_employee_status_mismatch_count=0',
            'transactional_prerequisite_active_employee_state=PASS','transactional_prerequisite_existing_vendor_state=NOT_REQUIRED_TEST_OWNED',
            'transactional_prerequisite_identity_collision_count=0','transactional_prerequisite_identity_collision_state=PASS',
            'transactional_prerequisite_uom_collision_count=0','transactional_prerequisite_uom_collision_state=PASS',
            'transactional_prerequisite_tax_collision_count=0','transactional_prerequisite_tax_collision_state=PASS',
            'transactional_prerequisite_qc_collision_count=0','transactional_prerequisite_qc_collision_state=PASS',
            'transactional_prerequisite_vendor_collision_count=0','transactional_prerequisite_vendor_collision_state=PASS',
            'transactional_prerequisite_warehouse_collision_count=0','transactional_prerequisite_warehouse_collision_state=PASS',
            'transactional_prerequisite_history_collision_count=0','transactional_prerequisite_history_collision_state=PASS',
            'transactional_constraint_identity_state=PASS','transactional_constraint_uom_state=PASS','transactional_constraint_tax_state=PASS',
            'transactional_constraint_qc_state=PASS','transactional_constraint_vendor_state=PASS','transactional_constraint_warehouse_state=PASS',
            'transactional_constraint_history_state=PASS','transactional_constraint_test_state=PASS')) { Assert-Evidence $transactionalEvidence $required }
        [IO.File]::WriteAllText($transactionalOutputPath, (Protect-Text $transactionalEvidence), [Text.UTF8Encoding]::new($false))
        $script:testEvidence = $transactionalEvidence + "`ntransactional_output_evidence_path=$transactionalOutputPath`ntransactional_rollback_state=PASS`ntrx_evidence_path=NOT_RUN`nrev869a_postgresql_test_state=NOT_RUN`ntest_acceptance_state=NOT_RUN"
    }
    catch {
        $safeFailure = Protect-Text $_.Exception.Message
        [IO.File]::WriteAllText($transactionalOutputPath, $safeFailure, [Text.UTF8Encoding]::new($false))
        $script:testEvidence = "transactional_constraint_test_state=FAIL`ntransactional_output_evidence_path=$transactionalOutputPath`ntransactional_rollback_state=FAIL_CLOSED_CONNECTION_ROLLBACK`ntrx_evidence_path=NOT_RUN_TRANSACTIONAL_FAILURE`nrev869a_postgresql_test_state=NOT_RUN`ntest_acceptance_state=FAIL`nerror=$safeFailure"
        throw
    }
    $testOutput = @(& $dotnet test .\tests\SESS.NexaERP.Tests\SESS.NexaERP.Tests.csproj --no-restore --filter "FullyQualifiedName~Rev869APostgresAcceptanceTests" --logger "trx;LogFileName=rev869a_resume_acceptance.trx" --results-directory $trxDirectory --logger "console;verbosity=minimal" 2>&1)
    $safeOutput = Protect-Text (($testOutput | Select-Object -Last 25 | ForEach-Object { $_.ToString() }) -join "`n")
    [IO.File]::WriteAllText($postgresTestOutputPath, $safeOutput, [Text.UTF8Encoding]::new($false))
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $trxPath -PathType Leaf)) {
        $script:testEvidence = $transactionalEvidence + "`ntransactional_output_evidence_path=$transactionalOutputPath`ntransactional_rollback_state=PASS`ntrx_evidence_path=$(if (Test-Path -LiteralPath $trxPath -PathType Leaf) { $trxPath } else { 'NOT_CREATED' })`npostgresql_test_output_evidence_path=$postgresTestOutputPath`nrev869a_postgresql_test_state=FAIL`ntest_acceptance_state=FAIL`n$safeOutput"
        throw "REV869A PostgreSQL-backed tests failed or did not create TRX evidence."
    }
    $script:testEvidence = $transactionalEvidence + "`ntransactional_output_evidence_path=$transactionalOutputPath`ntransactional_rollback_state=PASS`ntrx_evidence_path=$trxPath`npostgresql_test_output_evidence_path=$postgresTestOutputPath`nrev869a_postgresql_test_state=PASS`ntest_acceptance_state=PASS`n$safeOutput"
}

try {
    Assert-Mode
    Assert-TargetSafety
    $preflightSql = Get-PreflightSql
    $postSql = Get-PostMigrationSql
    Assert-SelectOnlySql "Preflight" $preflightSql
    Assert-SelectOnlySql "Post-migration verification" $postSql

    if ($GeneratePlanOnly) { Write-Plan $preflightSql $postSql; return }

    if ($ResumePostApplyAcceptance) {
        $preflightEvidence = Get-ApprovedPreApplyEvidence
        $testEvidence = "transactional_constraint_test_state=NOT_RUN`ntrx_evidence_path=NOT_CREATED`nrev869a_postgresql_test_state=NOT_RUN`ntest_acceptance_state=NOT_RUN"
    }
    Initialize-DatabaseAccess

    if ($PreflightOnly -or $Apply) {
        $preflightEvidence = Invoke-Psql $preflightSql $true
        Assert-Evidence $preflightEvidence "database_identity=PASS"
        Assert-Evidence $preflightEvidence "relieved_employee_acceptance_state=PASS"
        Assert-Evidence $preflightEvidence "existing_department_manager_reuse_state=PASS"
        Assert-Evidence $preflightEvidence "role_readiness_state=PASS"
        Assert-Evidence $preflightEvidence "safe_retry_state=PASS"
        Assert-Evidence $preflightEvidence "uom_management_decision_state=APPROVED"
        Assert-Evidence $preflightEvidence "approved_uom_plan_count=1"
        Assert-Evidence $preflightEvidence "approved_new_uom_count=1"
        Assert-Evidence $preflightEvidence "approved_existing_uom_count=0"
        Assert-Evidence $preflightEvidence "uom_id_collision_count=0"
        Assert-Evidence $preflightEvidence "uom_code_collision_count=0"
        Assert-Evidence $preflightEvidence "uom_name_collision_count=0"
        Assert-Evidence $preflightEvidence "uom_creation_plan_state=PASS"
        Assert-Evidence $preflightEvidence "approved_item_mapping_count=1"
        Assert-Evidence $preflightEvidence "approved_mapping_missing_item_count=0"
        Assert-Evidence $preflightEvidence "approved_mapping_unexpected_item_count=0"
        Assert-Evidence $preflightEvidence "approved_mapping_duplicate_count=0"
        Assert-Evidence $preflightEvidence "approved_mapping_invalid_uom_count=0"
        Assert-Evidence $preflightEvidence "unresolved_unmapped_item_count=0"
        Assert-Evidence $preflightEvidence "item_mapping_plan_state=PASS"
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
    Assert-Evidence $postEvidence "migration_count=12"
    Assert-Evidence $postEvidence "target_migration_count=1"
    Assert-Evidence $postEvidence "relieved_employee_acceptance_state=PASS"
    Assert-Evidence $postEvidence "existing_department_manager_reuse_state=PASS"
    Assert-Evidence $postEvidence "role_seed_count=4"
    Assert-Evidence $postEvidence "page_seed_count=8"
    Assert-Evidence $postEvidence "permission_seed_count=74"
    Assert-Evidence $postEvidence "policy_seed_count=2"
    Assert-Evidence $postEvidence "role_seed_unexpected_count=0"
    Assert-Evidence $postEvidence "role_seed_missing_count=0"
    Assert-Evidence $postEvidence "page_seed_unexpected_count=0"
    Assert-Evidence $postEvidence "page_seed_missing_count=0"
    Assert-Evidence $postEvidence "policy_seed_unexpected_count=0"
    Assert-Evidence $postEvidence "policy_seed_missing_count=0"
    Assert-Evidence $postEvidence "permission_seed_unexpected_count=0"
    Assert-Evidence $postEvidence "permission_seed_missing_count=0"
    Assert-Evidence $postEvidence "permission_flag_mismatch_count=0"
    Assert-Evidence $postEvidence "permission_role_mapping_mismatch_count=0"
    Assert-Evidence $postEvidence "permission_page_mapping_mismatch_count=0"
    Assert-Evidence $postEvidence "duplicate_role_page_permission_count=0"
    Assert-Evidence $postEvidence "migration_owned_seed_count=88"
    Assert-Evidence $postEvidence "security_configuration_owned_seed_count=88"
    Assert-Evidence $postEvidence "migration_created_uom_count=1"
    Assert-Evidence $postEvidence "migration_updated_item_count=1"
    Assert-Evidence $postEvidence "migration_created_uom_history_count=1"
    Assert-Evidence $postEvidence "total_inserted_migration_owned_row_count=90"
    Assert-Evidence $postEvidence "exact_ea_uom_count=1"
    Assert-Evidence $postEvidence "ea_uom_attribute_mismatch_count=0"
    Assert-Evidence $postEvidence "ea_uom_code_collision_count=0"
    Assert-Evidence $postEvidence "ea_uom_name_collision_count=0"
    Assert-Evidence $postEvidence "exact_item_ea_mapping_count=1"
    Assert-Evidence $postEvidence "exact_ea_approval_history_count=1"
    Assert-Evidence $postEvidence "preexisting_ea_backup_collision_count=0"
    Assert-Evidence $postEvidence "logical_role_code_mismatch_count=0"
    Assert-Evidence $postEvidence "all_false_department_manager_count=0"
    Assert-Evidence $postEvidence "department_manager_permission_mismatch_count=0"
    Assert-Evidence $postEvidence "database_schema_acceptance_state=PASS"
    if ($Apply -or $ResumePostApplyAcceptance) {
        Assert-Preservation $preflightEvidence $postEvidence
        $postEvidence = $postEvidence + "`ndatabase_preservation_acceptance_state=PASS`ndatabase_acceptance_state=PASS"
    }
    else { $postEvidence = $postEvidence + "`ndatabase_preservation_acceptance_state=NOT_CLAIMED`ndatabase_acceptance_state=NOT_CLAIMED" }

    if ($PostMigrationVerification) {
        $testEvidence = "Post-verification-only mode does not rerun transactional tests.`ntest_acceptance_state=NOT_RUN"
        Write-SanitizedReport "PostMigrationVerification" "NOT_RUN" $postEvidence $testEvidence "NOT_CLAIMED"
        return
    }

    if ($ResumePostApplyAcceptance) {
        Assert-Evidence $postEvidence "database_schema_acceptance_state=PASS"
        Assert-Evidence $postEvidence "database_preservation_acceptance_state=PASS"
        Assert-Evidence $postEvidence "database_acceptance_state=PASS"
        Invoke-ResumeAcceptanceTests
        Assert-Evidence $testEvidence "transactional_constraint_test_state=PASS"
        Assert-Evidence $testEvidence "rev869a_postgresql_test_state=PASS"
        Assert-Evidence $testEvidence "test_acceptance_state=PASS"
        $postEvidence = $postEvidence + "`noverall_acceptance_state=PASS"
        Assert-Evidence $postEvidence "overall_acceptance_state=PASS"
        Write-SanitizedReport "ResumePostApplyAcceptance" $preflightEvidence $postEvidence $testEvidence "PASS"
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
