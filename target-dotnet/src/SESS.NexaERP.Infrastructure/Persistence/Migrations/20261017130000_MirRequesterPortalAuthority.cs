using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261017130000_MirRequesterPortalAuthority")]
public sealed class MirRequesterPortalAuthority : Migration
{
    protected override void Up(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            LOCK TABLE advance.roles,advance.page_definitions,advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.roles WHERE "Code"='MIR_REQUESTER')
                 OR EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='employee.my-mirs') THEN
                RAISE EXCEPTION 'MIR requester install refuses pre-existing role/page state; reconcile before installing.';
              END IF;
              IF (SELECT count(*) FROM advance.companies WHERE "Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP') AND "IsActive" AND "Status"='ACTIVE')<>2 THEN
                RAISE EXCEPTION 'MIR requester portal requires both active R1 companies.';
              END IF;
            END $guard$;
            INSERT INTO advance.roles("Id","Code","Name","IsPrivileged","IsActive","Audience","BusinessArea",
              "IsEmployeeAssignable","CreatedAt","CreatedBy","Version")
            VALUES(md5('RC26:MIR_REQUESTER')::uuid,'MIR_REQUESTER','MIR requester only',false,true,'INTERNAL_EMPLOYEE','GENERAL',
              true,clock_timestamp(),'MirRequesterPortalAuthority',0);
            INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
            VALUES(md5('RC26:employee.my-mirs')::uuid,'employee.my-mirs','Employee','My material requests','/employee/my-mirs',true,
              clock_timestamp(),'MirRequesterPortalAuthority',0);
            INSERT INTO advance.company_role_activations("Id","CompanyId","RoleId","IsEnabled","EffectiveFrom","Remarks","CreatedAt","CreatedBy","Version")
            SELECT md5('RC26:MIR_REQUESTER:'||c."Id")::uuid,c."Id",md5('RC26:MIR_REQUESTER')::uuid,true,DATE '2026-10-17',
              'Portal role only; employee assignments require a TD-approved roster.',clock_timestamp(),'MirRequesterPortalAuthority',0
              FROM advance.companies c WHERE c."Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP');
            INSERT INTO advance.role_page_permissions("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit",
              "CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel",
              "CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues",
              "CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
            VALUES(md5('RC26:MIR_REQUESTER:portal')::uuid,md5('RC26:MIR_REQUESTER')::uuid,md5('RC26:employee.my-mirs')::uuid,
              true,true,true,true,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,
              clock_timestamp(),'MirRequesterPortalAuthority',0);
            -- Deliberately no employee role, identity, scope or department assignment here.
            """));
    }
    protected override void Down(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            LOCK TABLE advance.roles,advance.page_definitions,advance.role_page_permissions,advance.employee_role_assignments,
              advance.employee_page_permissions,advance.company_role_activations IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ DECLARE role_id uuid:=md5('RC26:MIR_REQUESTER')::uuid; page_id uuid:=md5('RC26:employee.my-mirs')::uuid;
            BEGIN
              IF EXISTS(SELECT 1 FROM advance.employee_role_assignments WHERE "RoleId"=role_id)
                 OR EXISTS(SELECT 1 FROM advance.employee_page_permissions WHERE "PageDefinitionId"=page_id)
                 OR EXISTS(SELECT 1 FROM advance.role_page_permissions WHERE ("RoleId"=role_id OR "PageDefinitionId"=page_id)
                     AND "Id"<>md5('RC26:MIR_REQUESTER:portal')::uuid) THEN
                RAISE EXCEPTION 'MIR requester rollback refuses retained assignments or additional grants; keep the forward migration.';
              END IF;
              IF NOT EXISTS(SELECT 1 FROM advance.roles WHERE "Id"=role_id AND "Code"='MIR_REQUESTER'
                AND "Name"='MIR requester only' AND NOT "IsPrivileged" AND "IsActive" AND "IsEmployeeAssignable"
                AND "Audience"='INTERNAL_EMPLOYEE' AND "BusinessArea"='GENERAL' AND "ReplacementRoleId" IS NULL
                AND "Version"=0 AND "CreatedBy"='MirRequesterPortalAuthority' AND "UpdatedAt" IS NULL)
                OR NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "Id"=page_id AND "PageKey"='employee.my-mirs'
                  AND "Module"='Employee' AND "Title"='My material requests' AND "Route"='/employee/my-mirs' AND "IsActive"
                  AND "Version"=0 AND "CreatedBy"='MirRequesterPortalAuthority' AND "UpdatedAt" IS NULL)
                OR (SELECT count(*) FROM advance.company_role_activations WHERE "RoleId"=role_id)<>2
                OR (SELECT count(*) FROM advance.company_role_activations WHERE "RoleId"=role_id AND "IsEnabled"
                  AND "EffectiveFrom"=DATE '2026-10-17' AND "EffectiveTo" IS NULL AND "Version"=0
                  AND "CreatedBy"='MirRequesterPortalAuthority' AND "UpdatedAt" IS NULL
                  AND "Remarks"='Portal role only; employee assignments require a TD-approved roster.'
                  AND "Id"=md5('RC26:MIR_REQUESTER:'||"CompanyId")::uuid
                  AND "CompanyId" IN (SELECT "Id" FROM advance.companies WHERE "Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP')))<>2
                OR NOT EXISTS(SELECT 1 FROM advance.role_page_permissions g WHERE g."Id"=md5('RC26:MIR_REQUESTER:portal')::uuid
                  AND g."RoleId"=role_id AND g."PageDefinitionId"=page_id AND g."Version"=0 AND g."UpdatedAt" IS NULL
                  AND g."CreatedBy"='MirRequesterPortalAuthority' AND NOT EXISTS(
                    SELECT 1 FROM jsonb_each(to_jsonb(g)) v WHERE (v.key LIKE 'Can%' OR v.key='HasFullControl')
                      AND v.value IS DISTINCT FROM to_jsonb(v.key IN ('CanView','CanCreate','CanUpdate','CanSubmit')))) THEN
                RAISE EXCEPTION 'MIR requester rollback refuses modified role/page/activation/grant state.';
              END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "Id"=md5('RC26:MIR_REQUESTER:portal')::uuid;
            DELETE FROM advance.company_role_activations WHERE "RoleId"=md5('RC26:MIR_REQUESTER')::uuid;
            DELETE FROM advance.page_definitions WHERE "Id"=md5('RC26:employee.my-mirs')::uuid;
            DELETE FROM advance.roles WHERE "Id"=md5('RC26:MIR_REQUESTER')::uuid;
            """));
    }
}
