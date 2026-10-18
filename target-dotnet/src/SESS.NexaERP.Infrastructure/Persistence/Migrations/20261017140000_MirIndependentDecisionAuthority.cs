using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261017140000_MirIndependentDecisionAuthority")]
public sealed class MirIndependentDecisionAuthority : Migration
{
    private static readonly (string Signature,string Before,string After)[] Rewrites =
    [
        ("advance.company_report_pending_approvals(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)",
            "ARRAY['STORES_MANAGER','PRODUCTION_MANAGER']::text[]", "ARRAY['STORES_MANAGER','PRODUCTION_MANAGER','MIR_APPROVER']::text[]"),
        ("advance.stores_workload(text,uuid,uuid[],text,text,uuid,bigint,integer)",
            """'["STORES_MANAGER","PRODUCTION_MANAGER"]'::jsonb""", """'["STORES_MANAGER","PRODUCTION_MANAGER","MIR_APPROVER"]'::jsonb"""),
    ];
    protected override void Up(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            LOCK TABLE advance.roles,advance.role_page_permissions,advance.company_role_activations IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.roles WHERE "Code"='MIR_APPROVER')
                 OR (SELECT count(*) FROM advance.page_definitions WHERE "PageKey"='stores.material-issue-requests' AND "IsActive")<>1 THEN
                RAISE EXCEPTION 'MIR decision role requires the unchanged MIR page and no pre-existing fallback role.';
              END IF;
            END $guard$;
            INSERT INTO advance.roles("Id","Code","Name","IsPrivileged","IsActive","Audience","BusinessArea","IsEmployeeAssignable","CreatedAt","CreatedBy","Version")
            VALUES(md5('RC26:MIR_APPROVER')::uuid,'MIR_APPROVER','MIR independent decision only',false,true,'INTERNAL_EMPLOYEE','GENERAL',true,
              clock_timestamp(),'MirIndependentDecisionAuthority',0);
            INSERT INTO advance.company_role_activations
              SELECT (jsonb_populate_record(NULL::advance.company_role_activations,to_jsonb(a)||jsonb_build_object(
                'Id',md5('RC26:MIR_APPROVER:'||a."CompanyId")::uuid,'RoleId',md5('RC26:MIR_APPROVER')::uuid,
                'Remarks','Decision role only; TD-approved roster required before employee assignment.','CreatedBy','MirIndependentDecisionAuthority'))).*
              FROM advance.company_role_activations a WHERE a."RoleId"=md5('RC26:MIR_REQUESTER')::uuid
                AND a."CompanyId" IN(SELECT "Id" FROM advance.companies WHERE "Code" IN('SESS_PVT_LTD','SESS_PROPRIETORSHIP'));
            INSERT INTO advance.role_page_permissions
              SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(g)||jsonb_build_object(
                'Id',md5('RC26:MIR_APPROVER:decisions')::uuid,'RoleId',md5('RC26:MIR_APPROVER')::uuid,
                'PageDefinitionId',p."Id",'CanCreate',false,'CanUpdate',false,'CanSubmit',false,'CanApprove',true,'CanReject',true,
                'CreatedBy','MirIndependentDecisionAuthority'))).*
              FROM advance.role_page_permissions g CROSS JOIN advance.page_definitions p
              WHERE g."Id"=md5('RC26:MIR_REQUESTER:portal')::uuid AND p."PageKey"='stores.material-issue-requests';
            -- No employee receives this role during migration. Existing KARTHICK/SARATH roles and self-approval guards are unchanged.
            """));
        foreach(var row in Rewrites) builder.Sql(InstalledFunctionSql.Rewrite(row.Signature,true,(row.Before,row.After,1)));
    }
    protected override void Down(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            LOCK TABLE advance.roles,advance.role_page_permissions,advance.company_role_activations,advance.employee_role_assignments IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.employee_role_assignments WHERE "RoleId"=md5('RC26:MIR_APPROVER')::uuid)
                 OR EXISTS(SELECT 1 FROM advance.material_issue_history WHERE "ActorRoleCode"='MIR_APPROVER') THEN
                RAISE EXCEPTION 'MIR decision rollback refuses retained assignments or decisions; keep the forward release.';
              END IF;
              IF NOT EXISTS(SELECT 1 FROM advance.roles WHERE "Id"=md5('RC26:MIR_APPROVER')::uuid AND "Code"='MIR_APPROVER'
                  AND "Name"='MIR independent decision only' AND NOT "IsPrivileged" AND "IsActive" AND "IsEmployeeAssignable"
                  AND "Audience"='INTERNAL_EMPLOYEE' AND "BusinessArea"='GENERAL' AND "ReplacementRoleId" IS NULL
                  AND "Version"=0 AND "UpdatedAt" IS NULL AND "CreatedBy"='MirIndependentDecisionAuthority')
                 OR (SELECT count(*) FROM advance.role_page_permissions WHERE "RoleId"=md5('RC26:MIR_APPROVER')::uuid)<>1
                 OR NOT EXISTS(SELECT 1 FROM advance.role_page_permissions g JOIN advance.page_definitions p ON p."Id"=g."PageDefinitionId"
                    WHERE g."Id"=md5('RC26:MIR_APPROVER:decisions')::uuid AND g."RoleId"=md5('RC26:MIR_APPROVER')::uuid
                      AND p."PageKey"='stores.material-issue-requests' AND g."Version"=0 AND g."UpdatedAt" IS NULL
                      AND g."CreatedBy"='MirIndependentDecisionAuthority' AND NOT EXISTS(SELECT 1 FROM jsonb_each(to_jsonb(g)) v
                        WHERE (v.key LIKE 'Can%' OR v.key='HasFullControl') AND v.value IS DISTINCT FROM to_jsonb(v.key IN ('CanView','CanApprove','CanReject'))))
                 OR (SELECT count(*) FROM advance.company_role_activations WHERE "RoleId"=md5('RC26:MIR_APPROVER')::uuid)<>2
                 OR (SELECT count(*) FROM advance.company_role_activations WHERE "RoleId"=md5('RC26:MIR_APPROVER')::uuid AND "IsEnabled"
                    AND "EffectiveFrom"=DATE '2026-10-17' AND "EffectiveTo" IS NULL AND "Version"=0 AND "UpdatedAt" IS NULL
                    AND "CreatedBy"='MirIndependentDecisionAuthority' AND "Id"=md5('RC26:MIR_APPROVER:'||"CompanyId")::uuid
                    AND "CompanyId" IN(SELECT "Id" FROM advance.companies WHERE "Code" IN('SESS_PVT_LTD','SESS_PROPRIETORSHIP'))
                    AND "Remarks"='Decision role only; TD-approved roster required before employee assignment.')<>2 THEN
                RAISE EXCEPTION 'MIR decision rollback refuses modified authority metadata.';
              END IF;
            END $guard$;
            """));
        foreach(var row in Rewrites) builder.Sql(InstalledFunctionSql.Rewrite(row.Signature,true,(row.After,row.Before,1)));
        builder.Sql(MigrationText.Lf("""
            DELETE FROM advance.role_page_permissions WHERE "Id"=md5('RC26:MIR_APPROVER:decisions')::uuid;
            DELETE FROM advance.company_role_activations WHERE "RoleId"=md5('RC26:MIR_APPROVER')::uuid;
            DELETE FROM advance.roles WHERE "Id"=md5('RC26:MIR_APPROVER')::uuid;
            """));
    }
}
