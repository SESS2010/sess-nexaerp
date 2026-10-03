namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string R1PurchaseSupportEmployeeCode = "TEST-R1-PURCHASE-SUPPORT";
    private const string R1AccountsSupportEmployeeCode = "TEST-R1-ACCOUNTS-SUPPORT";

    // Disposable security actors preserve the old mixed FULL/SUPPORT regressions after
    // the signed roster removes those combinations from real R1 employees. No real
    // employee assignment or login is changed by this fixture.
    private const string R1PurchaseFlowSupportFixtureSql = """
        INSERT INTO advance.employees
          ("Id","EmployeeCode","EmployeeName","OriginalImportedName","EmployeeType","Grade","DepartmentId","DesignationId",
           "Status","IsDateOfJoiningApproximate","LoginEnabled","ApprovalStatus","IsEmployeeCodeLocked","CreatedAt","CreatedBy","Version")
        SELECT md5('workflow-support:'||v.code)::uuid,v.code,v.name,v.name,e."EmployeeType",e."Grade",e."DepartmentId",e."DesignationId",
          'Active',false,true,'SeedApproved',true,now(),'WORKFLOW-SUPPORT-FIXTURE',0
          FROM (VALUES('TEST-R1-PURCHASE-SUPPORT','Disposable purchase support actor','SESS-15'),
            ('TEST-R1-ACCOUNTS-SUPPORT','Disposable accounts support actor','SESS-41')) v(code,name,source_code)
          JOIN advance.employees e ON e."EmployeeCode"=v.source_code;
        INSERT INTO advance.employee_company_assignments
          ("Id","CompanyId","EmployeeId","AssignmentType","EmployeeCode","CompanySiteId","EmploymentType","EffectiveFrom",
           "Status","IsActive","CreatedAt","CreatedBy","Version")
        SELECT gen_random_uuid(),a."CompanyId",synthetic."Id",a."AssignmentType",synthetic."EmployeeCode",a."CompanySiteId",a."EmploymentType",
          DATE '2026-01-01','ACTIVE',true,now(),'WORKFLOW-SUPPORT-FIXTURE',0
          FROM (VALUES('TEST-R1-PURCHASE-SUPPORT','SESS-15'),('TEST-R1-ACCOUNTS-SUPPORT','SESS-41')) v(code,source_code)
          JOIN advance.employees synthetic ON synthetic."EmployeeCode"=v.code JOIN advance.employees source ON source."EmployeeCode"=v.source_code
          JOIN advance.employee_company_assignments a ON a."EmployeeId"=source."Id" AND a."IsActive" AND a."EffectiveTo" IS NULL
          JOIN advance.companies c ON c."Id"=a."CompanyId" AND c."Code"='SESS_PVT_LTD';
        INSERT INTO advance.employee_department_assignments
          ("Id","CompanyId","EmployeeCompanyAssignmentId","DepartmentId","DesignationId","AssignmentType","EffectiveFrom",
           "IsPrimary","Status","IsActive","CreatedAt","CreatedBy","Version")
        SELECT gen_random_uuid(),a."CompanyId",a."Id",e."DepartmentId",e."DesignationId",'PRIMARY',DATE '2026-01-01',
          true,'ACTIVE',true,now(),'WORKFLOW-SUPPORT-FIXTURE',0 FROM advance.employee_company_assignments a
          JOIN advance.employees e ON e."Id"=a."EmployeeId" WHERE e."EmployeeCode" IN('TEST-R1-PURCHASE-SUPPORT','TEST-R1-ACCOUNTS-SUPPORT');
        DO $roles$ DECLARE company uuid; authority uuid; actor uuid; BEGIN
          SELECT "Id" INTO STRICT company FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
          SELECT a."Id",a."EmployeeId" INTO STRICT authority,actor FROM advance.employee_role_assignments a
            JOIN advance.roles r ON r."Id"=a."RoleId" JOIN advance.employees e ON e."Id"=a."EmployeeId"
            WHERE a."CompanyId"=company AND r."Code"='TECHNICAL_DIRECTOR' AND e."EmployeeCode"='SESS-01' AND a."EffectiveTo" IS NULL;
          PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
          INSERT INTO advance.employee_role_assignments
            ("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","AssignmentType","ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),company,e."Id",r."Id",DATE '2026-01-01',v.type,'SeedApproved',
            'Disposable mixed-authority security fixture; not a signed-roster production assignment',now(),'WORKFLOW-SUPPORT-FIXTURE',0
            FROM (VALUES('TEST-R1-PURCHASE-SUPPORT','PURCHASE_MANAGER','FULL'),
              ('TEST-R1-PURCHASE-SUPPORT','PURCHASE_EXECUTIVE','FULL'),('TEST-R1-PURCHASE-SUPPORT','STORES_EXECUTIVE','SUPPORT'),
              ('TEST-R1-ACCOUNTS-SUPPORT','ACCOUNTS_ASSISTANT','SUPPORT')) v(code,role,type)
            JOIN advance.employees e ON e."EmployeeCode"=v.code JOIN advance.roles r ON r."Code"=v.role;
          INSERT INTO advance.employee_role_assignment_events
            ("Id","CompanyId","EmployeeId","ActorEmployeeId","AssignmentId","Operation","ToRoleCode","ToAssignmentType",
             "NewEffectiveFrom","EffectiveOn","Reason","ActorLoginId","ActorRoleCode","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),a."CompanyId",a."EmployeeId",actor,a."Id",'WORKFLOW_SUPPORT_FIXTURE',r."Code",a."AssignmentType",
            a."EffectiveFrom",a."EffectiveFrom",'Disposable security fixture with independent configuration authority',
            'WORKFLOW-SUPPORT-FIXTURE','TECHNICAL_DIRECTOR',now(),'WORKFLOW-SUPPORT-FIXTURE',0
            FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId" WHERE a."CreatedBy"='WORKFLOW-SUPPORT-FIXTURE';
          PERFORM set_config('sess.role_authority_assignment_id','',true);
        END $roles$;
        """;
}