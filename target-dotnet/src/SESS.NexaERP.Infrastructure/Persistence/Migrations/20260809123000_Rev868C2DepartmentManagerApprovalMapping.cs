using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

public partial class Rev868C2DepartmentManagerApprovalMapping : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            create table if not exists nexa.purchase_approval_route_settings_rev868c2_backup
            (
                "RouteSettingId" uuid primary key,
                "RouteCode" character varying(40) not null,
                "MinimumAmount" numeric(18,2) not null,
                "MaximumAmount" numeric(18,2) null,
                "ApproverRoleCode" character varying(80) not null,
                "IsActive" boolean not null,
                "UpdatedAt" timestamp with time zone null,
                "UpdatedBy" text null,
                "Version" bigint not null,
                "CapturedAt" timestamp with time zone not null,
                "CapturedBy" text not null
            );

            insert into nexa.purchase_approval_route_settings_rev868c2_backup
                ("RouteSettingId", "RouteCode", "MinimumAmount", "MaximumAmount", "ApproverRoleCode", "IsActive", "UpdatedAt", "UpdatedBy", "Version", "CapturedAt", "CapturedBy")
            select "Id", "RouteCode", "MinimumAmount", "MaximumAmount", "ApproverRoleCode", "IsActive", "UpdatedAt", "UpdatedBy", "Version", TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C2_ROUTE_BACKUP'
            from nexa.purchase_approval_route_settings
            where "RouteCode" in ('Manager','MANAGER','BRANCH_MANAGER','MANAGER_APPROVAL','TD','TechnicalDirector','TECHNICALDIRECTOR','TECHNICAL_DIRECTOR','MD','ManagingDirector','MANAGINGDIRECTOR','MANAGING_DIRECTOR')
            on conflict ("RouteSettingId") do nothing;
        """);
        migrationBuilder.AddColumn<string>(
            name: "ApproverResolutionType",
            schema: "nexa",
            table: "purchase_approval_route_settings",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "FIXED_ROLE");

        migrationBuilder.AlterColumn<string>(
            name: "ApproverRoleCode",
            schema: "nexa",
            table: "purchase_approval_route_settings",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(80)",
            oldMaxLength: 80);

        migrationBuilder.Sql("""
            update nexa.purchase_approval_route_settings
            set "RouteCode" = 'MANAGER',
                "ApproverRoleCode" = null,
                "ApproverResolutionType" = 'DEPARTMENT_MAPPING',
                "MinimumAmount" = 0.00,
                "MaximumAmount" = 50000.00,
                "IsActive" = true,
                "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00',
                "UpdatedBy" = 'REV868C2_ROUTE_CANONICALIZATION'
            where "RouteCode" in ('Manager','MANAGER','BRANCH_MANAGER','MANAGER_APPROVAL')
              and not exists (
                  select 1 from nexa.purchase_approval_route_settings r
                  where r."RouteCode" = 'MANAGER'
                    and r."Id" <> nexa.purchase_approval_route_settings."Id");

            update nexa.purchase_approval_route_settings
            set "RouteCode" = 'TECHNICAL_DIRECTOR',
                "ApproverRoleCode" = 'TECHNICAL_DIRECTOR',
                "ApproverResolutionType" = 'FIXED_ROLE',
                "MinimumAmount" = 50000.01,
                "MaximumAmount" = 500000.00,
                "IsActive" = true,
                "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00',
                "UpdatedBy" = 'REV868C2_ROUTE_CANONICALIZATION'
            where "RouteCode" in ('TD','TechnicalDirector','TECHNICALDIRECTOR','TECHNICAL_DIRECTOR')
              and not exists (
                  select 1 from nexa.purchase_approval_route_settings r
                  where r."RouteCode" = 'TECHNICAL_DIRECTOR'
                    and r."Id" <> nexa.purchase_approval_route_settings."Id");

            update nexa.purchase_approval_route_settings
            set "RouteCode" = 'MANAGING_DIRECTOR',
                "ApproverRoleCode" = 'MANAGING_DIRECTOR',
                "ApproverResolutionType" = 'FIXED_ROLE',
                "MinimumAmount" = 500000.01,
                "MaximumAmount" = null,
                "IsActive" = true,
                "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00',
                "UpdatedBy" = 'REV868C2_ROUTE_CANONICALIZATION'
            where "RouteCode" in ('MD','ManagingDirector','MANAGINGDIRECTOR','MANAGING_DIRECTOR')
              and not exists (
                  select 1 from nexa.purchase_approval_route_settings r
                  where r."RouteCode" = 'MANAGING_DIRECTOR'
                    and r."Id" <> nexa.purchase_approval_route_settings."Id");

            insert into nexa.purchase_approval_route_settings
                ("Id", "RouteCode", "MinimumAmount", "MaximumAmount", "ApproverRoleCode", "ApproverResolutionType", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
            values
                ('868c2000-0000-0000-0000-000000000001', 'MANAGER', 0.00, 50000.00, null, 'DEPARTMENT_MAPPING', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C2_ROUTE_CANONICALIZATION', null, null, 0),
                ('868c2000-0000-0000-0000-000000000002', 'TECHNICAL_DIRECTOR', 50000.01, 500000.00, 'TECHNICAL_DIRECTOR', 'FIXED_ROLE', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C2_ROUTE_CANONICALIZATION', null, null, 0),
                ('868c2000-0000-0000-0000-000000000003', 'MANAGING_DIRECTOR', 500000.01, null, 'MANAGING_DIRECTOR', 'FIXED_ROLE', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C2_ROUTE_CANONICALIZATION', null, null, 0)
            on conflict ("RouteCode") do update set
                "MinimumAmount" = excluded."MinimumAmount",
                "MaximumAmount" = excluded."MaximumAmount",
                "ApproverRoleCode" = excluded."ApproverRoleCode",
                "ApproverResolutionType" = excluded."ApproverResolutionType",
                "IsActive" = true,
                "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00',
                "UpdatedBy" = 'REV868C2_ROUTE_CANONICALIZATION';
        """);

        migrationBuilder.CreateTable(
            name: "department_approval_mappings",
            schema: "nexa",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                ApprovalRouteCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                PrimaryApproverEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                AlternateApproverEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_department_approval_mappings", x => x.Id);
                table.CheckConstraint("CK_department_approval_mapping_effective_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                table.CheckConstraint("CK_department_approval_mapping_manager_route", "\"ApprovalRouteCode\" = 'MANAGER'");
                table.ForeignKey("FK_department_approval_mappings_departments_DepartmentId", x => x.DepartmentId, "nexa", "departments", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_department_approval_mappings_employees_AlternateApproverEmployeeId", x => x.AlternateApproverEmployeeId, "nexa", "employees", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_department_approval_mappings_employees_PrimaryApproverEmployeeId", x => x.PrimaryApproverEmployeeId, "nexa", "employees", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod",
            schema: "nexa",
            table: "department_approval_mappings",
            columns: new[] { "DepartmentId", "ApprovalRouteCode", "EffectiveFrom" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod1",
            schema: "nexa",
            table: "department_approval_mappings",
            columns: new[] { "DepartmentId", "ApprovalRouteCode", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_department_approval_mappings_PrimaryApproverEmployeeId",
            schema: "nexa",
            table: "department_approval_mappings",
            column: "PrimaryApproverEmployeeId");

        migrationBuilder.CreateIndex(
            name: "IX_department_approval_mappings_AlternateApproverEmployeeId",
            schema: "nexa",
            table: "department_approval_mappings",
            column: "AlternateApproverEmployeeId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "department_approval_mappings", schema: "nexa");

        migrationBuilder.Sql("""
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
        """);

        migrationBuilder.AlterColumn<string>(
            name: "ApproverRoleCode",
            schema: "nexa",
            table: "purchase_approval_route_settings",
            type: "character varying(80)",
            maxLength: 80,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(80)",
            oldMaxLength: 80,
            oldNullable: true);

        migrationBuilder.DropColumn(
            name: "ApproverResolutionType",
            schema: "nexa",
            table: "purchase_approval_route_settings");
        migrationBuilder.Sql("""
            drop table if exists nexa.purchase_approval_route_settings_rev868c2_backup;
        """);
    }
}
