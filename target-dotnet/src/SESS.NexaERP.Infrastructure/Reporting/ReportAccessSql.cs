namespace SESS.NexaERP.Infrastructure.Reporting;

/// <summary>Report access and data share one statement and its PostgreSQL snapshot.</summary>
internal static class ReportAccessSql
{
    internal const string Context = """
        company AS MATERIALIZED (
          SELECT c."Id",c."Code"
          FROM advance.companies c
          WHERE c."Code"=@organization AND c."IsActive" AND c."Status"='ACTIVE'
            AND EXISTS(SELECT 1 FROM advance.employees e WHERE e."Id"=@employee
              AND e."LoginEnabled" AND upper(e."Status")='ACTIVE')
            AND (SELECT count(*) FROM advance.employee_company_assignments a
              WHERE a."CompanyId"=c."Id" AND a."EmployeeId"=@employee
                AND a."IsActive" AND a."Status"='ACTIVE' AND a."EffectiveFrom"<=CURRENT_DATE
                AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE))=1
            AND EXISTS(SELECT 1 FROM advance.employee_operational_scopes s
              WHERE s."CompanyId"=c."Id" AND s."OrganizationId"=c."Code"
                AND s."EmployeeId"=@employee AND s."IsActive" AND s."EffectiveFrom"<=CURRENT_DATE
                AND (s."EffectiveTo" IS NULL OR s."EffectiveTo">=CURRENT_DATE))
        ),
        effective_roles AS MATERIALIZED (
          SELECT DISTINCT r."Id",r."Code"
          FROM advance.employee_role_assignments a
          JOIN advance.roles r ON r."Id"=a."RoleId" AND r."IsActive"
          JOIN company c ON c."Id"=a."CompanyId"
          WHERE a."EmployeeId"=@employee AND a."Id"=ANY(@assignments)
            AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=CURRENT_DATE
            AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE)
            AND EXISTS(SELECT 1 FROM advance.company_role_activations ca
              WHERE ca."CompanyId"=a."CompanyId" AND ca."RoleId"=a."RoleId"
                AND ca."IsEnabled" AND ca."EffectiveFrom"<=CURRENT_DATE
                AND (ca."EffectiveTo" IS NULL OR ca."EffectiveTo">=CURRENT_DATE))
        ),
        report_grants AS MATERIALIZED (
          SELECT p."PageKey",
            bool_or(rp."CanView" OR rp."HasFullControl") AS can_view,
            bool_or(rp."CanExport" OR rp."HasFullControl") AS can_export,
            bool_or(rp."CanViewCommercialValues" OR rp."HasFullControl") AS can_commercial
          FROM advance.page_definitions p
          JOIN advance.role_page_permissions rp ON rp."PageDefinitionId"=p."Id"
          JOIN effective_roles r ON r."Id"=rp."RoleId"
          WHERE p."IsActive" AND p."PageKey" LIKE 'reports.%'
          GROUP BY p."PageKey"
        ),
        employee_report_grants AS MATERIALIZED (
          SELECT p."PageKey",bool_or(ep."CanView") AS can_view
          FROM advance.employee_page_permissions ep
          JOIN company c ON c."Id"=ep."CompanyId"
          JOIN advance.page_definitions p ON p."Id"=ep."PageDefinitionId" AND p."IsActive"
          WHERE ep."EmployeeId"=@employee AND p."PageKey" LIKE 'reports.%'
          GROUP BY p."PageKey"
        )
        """;

    internal const string Access = """
        access AS MATERIALIZED (
          SELECT (SELECT "Id" FROM company) AS company_id,
            EXISTS(SELECT 1 FROM company)
            AND (coalesce((SELECT can_view FROM report_grants WHERE "PageKey"=@page_key),false)
              OR coalesce((SELECT can_view FROM employee_report_grants WHERE "PageKey"=@page_key),false))
            AND (NOT @export OR coalesce((SELECT can_export FROM report_grants WHERE "PageKey"=@page_key),false))
            AND (NOT @commercial OR coalesce((SELECT can_commercial FROM report_grants WHERE "PageKey"=@page_key),false))
            AS allowed
        ),
        report_audit AS (
          INSERT INTO advance.audit_logs
            ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId",
             "UserLoginId","ActorRoleCode","Result","CorrelationId","CreatedAt","CreatedBy","Version","AfterJson")
          SELECT gen_random_uuid(),a.company_id,
            CASE WHEN a.company_id IS NULL THEN 'GLOBAL' ELSE 'COMPANY' END,
            'Reports',CASE WHEN a.allowed THEN 'Export' ELSE 'Denied' END,
            'CompanyReport',@page_key,@login,
            coalesce((SELECT min("Code") FROM effective_roles),'none'),
            CASE WHEN a.allowed THEN 'Success' ELSE 'Failure' END,
            @correlation,clock_timestamp(),@login,0,
            jsonb_build_object('organization',@organization,'report',@page_key,
              'timeZone',@report_timezone,'fromDate',@from_date,'toDate',@to_date,'mode',@mode)::text
          FROM access a WHERE @export OR NOT a.allowed
          RETURNING "Id"
        )
        """;
}
