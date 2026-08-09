START TRANSACTION;
DO $EF$
BEGIN
    IF EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    DROP TABLE nexa.department_approval_mappings;
    END IF;
END $EF$;
DO $EF$
BEGIN
    IF EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
        update nexa.purchase_approval_route_settings r
        set "RouteCode" = b."RouteCode",
            "MinimumAmount" = b."MinimumAmount",
            "MaximumAmount" = b."MaximumAmount",
            "ApproverRoleCode" = b."ApproverRoleCode",
            "IsActive" = b."IsActive",
            "UpdatedAt" = b."UpdatedAt",
            "UpdatedBy" = b."UpdatedBy",
            "Version" = b."Version"
        from nexa.purchase_approval_route_settings_rev868c2_backup b
        where r."Id" = b."RouteSettingId";

        delete from nexa.purchase_approval_route_settings r
        where r."Id" in ('868c2000-0000-0000-0000-000000000001', '868c2000-0000-0000-0000-000000000002', '868c2000-0000-0000-0000-000000000003')
          and r."CreatedBy" = 'REV868C2_ROUTE_CANONICALIZATION'
          and not exists (
              select 1
              from nexa.purchase_approval_route_settings_rev868c2_backup b
              where b."RouteSettingId" = r."Id");

        update nexa.purchase_approval_route_settings
        set "ApproverRoleCode" = case "RouteCode"
            when 'MANAGER' then 'MANAGER'
            when 'TECHNICAL_DIRECTOR' then 'TECHNICAL_DIRECTOR'
            when 'MANAGING_DIRECTOR' then 'MANAGING_DIRECTOR'
            else 'MANAGER'
        end,
            "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00',
            "UpdatedBy" = 'REV868C2_ROUTE_CANONICALIZATION_ROLLBACK_NON_NULL_GUARD'
        where "ApproverRoleCode" is null;

        do $$
        begin
            if exists (select 1 from nexa.purchase_approval_route_settings where "ApproverRoleCode" is null) then
                raise exception 'REV868C2 rollback cannot restore NOT NULL ApproverRoleCode while null values remain';
            end if;
        end $$;
    END IF;
END $EF$;
DO $EF$
BEGIN
    IF EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    ALTER TABLE nexa.purchase_approval_route_settings ALTER COLUMN "ApproverRoleCode" SET NOT NULL;
    END IF;
END $EF$;
DO $EF$
BEGIN
    IF EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    ALTER TABLE nexa.purchase_approval_route_settings DROP COLUMN "ApproverResolutionType";
    END IF;
END $EF$;
DO $EF$
BEGIN
    IF EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
        drop table if exists nexa.purchase_approval_route_settings_rev868c2_backup;
    END IF;
END $EF$;
DO $EF$
BEGIN
    IF EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    DELETE FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping';
    END IF;
END $EF$;
COMMIT;

