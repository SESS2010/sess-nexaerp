using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Print permission (approved by name 26 September 2026) for the two new print endpoints:
/// purchase.po for the Purchase Manager, Technical Director and Managing Director wherever they
/// already view POs (the Purchase Manager was seeded without print); stores.machine-deliveries for
/// the three Stores roles and the Technical Director (who gets a view-and-print row if none exists).
/// Only CanPrint (and CanView on the new Technical Director row) is set; every other grant is
/// unchanged. Rows touched are marked PrintPermissionGrants so Down reverts exactly them.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260926140000_PrintPermissionGrants")]
public sealed class PrintPermissionGrants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            UPDATE advance.role_page_permissions p SET "CanPrint"=true,"UpdatedAt"=now(),"UpdatedBy"='PrintPermissionGrants',"Version"=p."Version"+1
            FROM advance.page_definitions d, advance.roles r
            WHERE d."Id"=p."PageDefinitionId" AND r."Id"=p."RoleId" AND NOT p."CanPrint" AND p."CanView"
              AND ((d."PageKey"='purchase.po' AND r."Code" IN ('PURCHASE_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))
                OR (d."PageKey"='stores.machine-deliveries' AND r."Code" IN ('STORES_ASSISTANT','STORES_EXECUTIVE','STORES_MANAGER','TECHNICAL_DIRECTOR')));
            INSERT INTO advance.role_page_permissions
            SELECT (jsonb_populate_record(NULL::advance.role_page_permissions, to_jsonb(source)
              || jsonb_build_object('Id',md5('stores.machine-deliveries:'||td."Id"::text)::uuid,'RoleId',td."Id",
                   'CanView',true,'CanCreate',false,'CanUpdate',false,'CanSubmit',false,'CanIssue',false,'CanVerify',false,'CanApprove',false,
                   'CanReject',false,'CanRequestClarification',false,'CanRequestRevision',false,'CanResubmit',false,'CanCancel',false,
                   'CanDeactivate',false,'CanPrint',true,'CanDownload',false,'CanExport',false,'CanUploadAttachment',false,
                   'CanReplaceAttachment',false,'CanViewCommercialValues',false,'CanViewAuditHistory',false,'HasFullControl',false,
                   'CreatedAt',now(),'CreatedBy','PrintPermissionGrants','UpdatedAt',NULL,'UpdatedBy',NULL,'Version',0))).*
            FROM advance.role_page_permissions source
            JOIN advance.page_definitions d ON d."Id"=source."PageDefinitionId" AND d."PageKey"='stores.machine-deliveries'
            JOIN advance.roles sm ON sm."Id"=source."RoleId" AND sm."Code"='STORES_MANAGER'
            CROSS JOIN advance.roles td
            WHERE td."Code"='TECHNICAL_DIRECTOR'
              AND NOT EXISTS(SELECT 1 FROM advance.role_page_permissions x WHERE x."RoleId"=td."Id" AND x."PageDefinitionId"=d."Id");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DELETE FROM advance.role_page_permissions WHERE "CreatedBy"='PrintPermissionGrants';
            UPDATE advance.role_page_permissions SET "CanPrint"=false,"UpdatedAt"=now(),"UpdatedBy"='PrintPermissionGrants.Down',"Version"="Version"+1
            WHERE "UpdatedBy"='PrintPermissionGrants';
            """);
    }
}
