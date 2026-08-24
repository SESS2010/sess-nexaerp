using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

public partial class Rev868C3LegacyMixedDepartmentDeactivationCorrection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            create table nexa.rev868c3_legacy_department_deactivation_backup
            (
                "DepartmentId" uuid primary key,
                "Code" character varying(80) not null unique,
                "IsActive" boolean not null,
                "CreatedAt" timestamp with time zone not null,
                "CreatedBy" text not null,
                "UpdatedAt" timestamp with time zone null,
                "UpdatedBy" text null,
                "Version" bigint not null,
                "CapturedAt" timestamp with time zone not null,
                "CapturedBy" text not null
            );

            insert into nexa.rev868c3_legacy_department_deactivation_backup
                ("DepartmentId", "Code", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version", "CapturedAt", "CapturedBy")
            select "Id", "Code", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version",
                   TIMESTAMPTZ '2026-08-10T11:00:00+00:00', 'REV868C3_LEGACY_DEPARTMENT_DEACTIVATION_CORRECTION'
            from nexa.departments
            where "Code" in ('ENGINEER_TECHNICAL','MANAGER','JUNIOR_ASSISTANT','ADMIN_ACCOUNTS_STORES');

            do $rev868c3_legacy_department_up$
            declare affected_count integer;
            begin
                if (select count(*) from nexa.rev868c3_legacy_department_deactivation_backup) <> 4
                   or (select count(distinct "Code") from nexa.rev868c3_legacy_department_deactivation_backup) <> 4
                   or exists (
                       select 1 from (values ('ENGINEER_TECHNICAL'),('MANAGER'),('JUNIOR_ASSISTANT'),('ADMIN_ACCOUNTS_STORES')) expected("Code")
                       left join nexa.rev868c3_legacy_department_deactivation_backup b using ("Code") where b."DepartmentId" is null
                   ) then
                    raise exception 'REV868C3 corrective migration requires exactly the four legacy mixed departments';
                end if;

                update nexa.departments
                set "IsActive" = false,
                    "UpdatedAt" = TIMESTAMPTZ '2026-08-10T11:00:00+00:00',
                    "UpdatedBy" = 'REV868C3_LEGACY_DEPARTMENT_DEACTIVATION_CORRECTION',
                    "Version" = "Version" + 1
                where "Code" in ('ENGINEER_TECHNICAL','MANAGER','JUNIOR_ASSISTANT','ADMIN_ACCOUNTS_STORES');
                get diagnostics affected_count = row_count;
                if affected_count <> 4 then
                    raise exception 'REV868C3 corrective migration did not update exactly four legacy mixed departments';
                end if;
                if exists (select 1 from nexa.departments where "Code" in ('ENGINEER_TECHNICAL','MANAGER','JUNIOR_ASSISTANT','ADMIN_ACCOUNTS_STORES') and "IsActive" = true) then
                    raise exception 'REV868C3 corrective migration left a legacy mixed department active';
                end if;
            end
            $rev868c3_legacy_department_up$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            do $rev868c3_legacy_department_down$
            declare affected_count integer;
            begin
                if to_regclass('nexa.rev868c3_legacy_department_deactivation_backup') is null then
                    raise exception 'REV868C3 corrective rollback blocked: backup table is missing';
                end if;
                if (select count(*) from nexa.rev868c3_legacy_department_deactivation_backup) <> 4
                   or (select count(distinct "Code") from nexa.rev868c3_legacy_department_deactivation_backup) <> 4 then
                    raise exception 'REV868C3 corrective rollback blocked: backup set is not exactly four rows';
                end if;

                update nexa.departments d
                set "IsActive" = b."IsActive",
                    "CreatedAt" = b."CreatedAt",
                    "CreatedBy" = b."CreatedBy",
                    "UpdatedAt" = b."UpdatedAt",
                    "UpdatedBy" = b."UpdatedBy",
                    "Version" = b."Version"
                from nexa.rev868c3_legacy_department_deactivation_backup b
                where d."Id" = b."DepartmentId" and d."Code" = b."Code";
                get diagnostics affected_count = row_count;
                if affected_count <> 4 then
                    raise exception 'REV868C3 corrective rollback blocked: exact four-row restore was not proven';
                end if;
            end
            $rev868c3_legacy_department_down$;

            drop table nexa.rev868c3_legacy_department_deactivation_backup;
            """);
    }
}
