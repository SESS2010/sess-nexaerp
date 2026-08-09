using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[Migration("20260809115500_Rev868C2ApprovalRouteCanonicalization")]
public partial class Rev868C2ApprovalRouteCanonicalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            update nexa.purchase_approval_route_settings
            set "IsActive" = false,
                "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00',
                "UpdatedBy" = 'REV868C2_ROUTE_CANONICALIZATION_ROLLBACK'
            where "RouteCode" in ('MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')
              and "CreatedBy" = 'REV868C2_ROUTE_CANONICALIZATION';
        """);

        migrationBuilder.AlterColumn<string>(
            name: "ApproverRoleCode",
            schema: "nexa",
            table: "purchase_approval_route_settings",
            type: "character varying(80)",
            maxLength: 80,
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(string),
            oldType: "character varying(80)",
            oldMaxLength: 80,
            oldNullable: true);

        migrationBuilder.DropColumn(
            name: "ApproverResolutionType",
            schema: "nexa",
            table: "purchase_approval_route_settings");
    }
}
