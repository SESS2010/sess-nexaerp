using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    [Migration("20260809115500_Rev868C2ApprovalRouteCanonicalization")]
    public partial class Rev868C2ApprovalRouteCanonicalization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                update nexa.purchase_approval_route_settings
                set "RouteCode" = 'MANAGER',
                    "ApproverRoleCode" = 'DEPARTMENT_MANAGER',
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
                    ("Id", "RouteCode", "MinimumAmount", "MaximumAmount", "ApproverRoleCode", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
                values
                    ('868c2000-0000-0000-0000-000000000001', 'MANAGER', 0.00, 50000.00, 'DEPARTMENT_MANAGER', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C2_ROUTE_CANONICALIZATION', null, null, 0),
                    ('868c2000-0000-0000-0000-000000000002', 'TECHNICAL_DIRECTOR', 50000.01, 500000.00, 'TECHNICAL_DIRECTOR', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C2_ROUTE_CANONICALIZATION', null, null, 0),
                    ('868c2000-0000-0000-0000-000000000003', 'MANAGING_DIRECTOR', 500000.01, null, 'MANAGING_DIRECTOR', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C2_ROUTE_CANONICALIZATION', null, null, 0)
                on conflict ("RouteCode") do update set
                    "MinimumAmount" = excluded."MinimumAmount",
                    "MaximumAmount" = excluded."MaximumAmount",
                    "ApproverRoleCode" = excluded."ApproverRoleCode",
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
        }
    }
}
