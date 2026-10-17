using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task MirRequesterMigrationAddsOnlyPortalAuthorityAndRefusesRetainedAssignmentsOnDown()
    {
        const string before="20261017120000_KolkataRecordedEventDates";
        const string after="20261017130000_MirRequesterPortalAuthority";
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator=model.GetService<IMigrator>();
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("portal-before.sql",migrator.GenerateScript("0",before));
        server.Execute("portal-baseline.sql", """
            CREATE TABLE public.portal_grant_before AS SELECT "Id",to_jsonb(g) data FROM advance.role_page_permissions g;
            CREATE TABLE public.portal_role_before AS SELECT "Id",to_jsonb(r) data FROM advance.roles r;
            CREATE TABLE public.portal_assignment_before AS SELECT "Id",to_jsonb(a) data FROM advance.employee_role_assignments a;
            """);
        var up=migrator.GenerateScript(before,after); var down=migrator.GenerateScript(after,before);
        server.Execute("portal-up.sql",up);
        const string contract="""
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.portal_grant_before b LEFT JOIN advance.role_page_permissions g ON g."Id"=b."Id" WHERE to_jsonb(g) IS DISTINCT FROM b.data)
                OR EXISTS(SELECT 1 FROM public.portal_role_before b LEFT JOIN advance.roles r ON r."Id"=b."Id" WHERE to_jsonb(r) IS DISTINCT FROM b.data)
                OR EXISTS(SELECT 1 FROM public.portal_assignment_before b LEFT JOIN advance.employee_role_assignments a ON a."Id"=b."Id" WHERE to_jsonb(a) IS DISTINCT FROM b.data)
                OR (SELECT count(*) FROM advance.employee_role_assignments)<>(SELECT count(*) FROM public.portal_assignment_before) THEN
                RAISE EXCEPTION 'Portal install altered existing roles, grants or employee assignments';
              END IF;
              IF (SELECT count(*) FROM advance.roles WHERE "Code"='MIR_REQUESTER' AND NOT "IsPrivileged" AND "IsEmployeeAssignable")<>1
                 OR (SELECT count(*) FROM advance.company_role_activations WHERE "RoleId"=md5('RC26:MIR_REQUESTER')::uuid AND "IsEnabled")<>2 THEN
                RAISE EXCEPTION 'Portal role/companies are incomplete';
              END IF;
            END $assert$;
            """;
        server.Execute("portal-contract.sql",contract);
        await using(var db=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options))
        {
            var permissions=new EfPagePermissionService(db);
            var role=new EffectiveRoleAssignment(Guid.NewGuid(),MirRequesterPolicy.Role,"FULL");
            var projected=await permissions.ResolveEffectivePermissionsAsync([role],"SESS_PVT_LTD",Guid.Empty,default);
            Assert.Equal(new[]{"employee.my-mirs:create","employee.my-mirs:submit","employee.my-mirs:update","employee.my-mirs:view"},projected);
            foreach(var company in new[]{"SESS_PVT_LTD","SESS_PROPRIETORSHIP"})
            {
                var actual=await permissions.ResolveEffectivePermissionsAsync([role],company,Guid.Empty,default);
                Assert.Equal(projected,actual);
            }
            foreach(var page in new[]{"masters.items","masters.vendors","stores.material-issue-requests","stores.material-issues","qc.inspection-policies","purchase.po"})
            foreach(var action in new[]{"view","create","submit","approve","issue"})
                Assert.False(await permissions.HasPermissionAsync([MirRequesterPolicy.Role],page,action,default));
        }
        server.Execute("portal-down.sql",down);
        server.Execute("portal-up-again.sql",up);
        server.Execute("portal-contract-again.sql",contract);
        server.Execute("portal-changed-name.sql","""UPDATE advance.roles SET "Name"='Changed' WHERE "Code"='MIR_REQUESTER';""");
        server.AssertRejected("portal-down-changed-refused.sql",down,"refuses modified role/page/activation/grant state");
        server.Execute("portal-restore-name.sql","""UPDATE advance.roles SET "Name"='MIR requester only' WHERE "Code"='MIR_REQUESTER';""");
        server.Execute("portal-assignment.sql", """
            DO $fixture$ DECLARE source advance.employee_role_assignments%ROWTYPE; company uuid; authority uuid; BEGIN
              SELECT "Id" INTO STRICT company FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
              SELECT a."Id" INTO STRICT authority FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                JOIN advance.employees e ON e."Id"=a."EmployeeId" WHERE a."CompanyId"=company AND e."EmployeeCode"='SESS-01'
                  AND r."Code"='TECHNICAL_DIRECTOR' AND a."EffectiveTo" IS NULL LIMIT 1;
              PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
              SELECT a.* INTO STRICT source FROM advance.employee_role_assignments a JOIN advance.employees e ON e."Id"=a."EmployeeId"
                JOIN advance.roles r ON r."Id"=a."RoleId" WHERE a."CompanyId"=company AND e."EmployeeCode"='SESS-35'
                  AND r."Code"='STORES_EXECUTIVE' AND a."EffectiveTo" IS NULL AND a."AssignmentType"='FULL' LIMIT 1;
              INSERT INTO advance.employee_role_assignments SELECT * FROM jsonb_populate_record(NULL::advance.employee_role_assignments,
                to_jsonb(source)||jsonb_build_object('Id',gen_random_uuid(),'RoleId',md5('RC26:MIR_REQUESTER')::uuid,
                  'CreatedBy','PORTAL-RETAINED-ASSIGNMENT-FIXTURE','EffectiveFrom',advance.current_business_date(),'Version',0));
            END $fixture$;
            """);
        server.AssertRejected("portal-down-assignment-refused.sql",down,"refuses retained assignments");
        server.Execute("portal-assignment-retained.sql", """
            DO $assert$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM advance.employee_role_assignments WHERE "CreatedBy"='PORTAL-RETAINED-ASSIGNMENT-FIXTURE') THEN
                RAISE EXCEPTION 'Refusing Down lost the retained assignment';
              END IF;
            END $assert$;
            """);
    }
    [Fact]
    public async Task MirDecisionMigrationAddsOnlyIndependentDecisionAuthorityAndRestoresFunctionMetadata()
    {
        const string before="20261017130000_MirRequesterPortalAuthority",after="20261017140000_MirIndependentDecisionAuthority";
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator=model.GetService<IMigrator>();using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("mir-decision-before.sql",migrator.GenerateScript("0",before));
        server.Execute("mir-decision-functions-before.sql", """
            CREATE TABLE public.mir_decision_functions_before AS SELECT oid,to_jsonb(p) data FROM pg_proc p
              WHERE p.oid IN('advance.company_report_pending_approvals(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)'::regprocedure,
                'advance.stores_workload(text,uuid,uuid[],text,text,uuid,bigint,integer)'::regprocedure);
            CREATE TABLE public.mir_decision_grants_before AS SELECT "Id",to_jsonb(g) data FROM advance.role_page_permissions g;
            """);
        var up=migrator.GenerateScript(before,after);var down=migrator.GenerateScript(after,before);
        server.Execute("mir-decision-up.sql",up);
        await using(var db=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options))
        {
            var service=new EfPagePermissionService(db);
            foreach(var company in new[]{"SESS_PVT_LTD","SESS_PROPRIETORSHIP"})
            {
                var role=new EffectiveRoleAssignment(Guid.NewGuid(),MirRequesterPolicy.ApproverRole,"FULL");
                var permissions=await service.ResolveEffectivePermissionsAsync([role],company,Guid.Empty,default);
                Assert.Equal(new[]{"stores.material-issue-requests:approve","stores.material-issue-requests:reject","stores.material-issue-requests:view"},permissions);
            }
            Assert.Equal(0,await db.EmployeeRoleAssignments.CountAsync(x=>x.Role!.Code==MirRequesterPolicy.ApproverRole));
            Assert.False(await service.HasPermissionAsync([MirRequesterPolicy.ApproverRole],"stores.material-issues","issue",default));
            Assert.False(await service.HasPermissionAsync([MirRequesterPolicy.ApproverRole],MirRequesterPolicy.DecisionPage,"create",default));
        }
        server.Execute("mir-decision-old-grants-unchanged.sql", """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.mir_decision_grants_before b LEFT JOIN advance.role_page_permissions g ON g."Id"=b."Id" WHERE to_jsonb(g) IS DISTINCT FROM b.data) THEN
                RAISE EXCEPTION 'Existing permission changed';
              END IF;
              IF EXISTS(SELECT 1 FROM public.mir_decision_functions_before b JOIN pg_proc p ON p.oid=b.oid
                 WHERE to_jsonb(p)-'prosrc' IS DISTINCT FROM b.data-'prosrc') THEN RAISE EXCEPTION 'Function security/owner/ACL/settings changed'; END IF;
            END $assert$;
            """);
        server.Execute("mir-decision-down.sql",down);
        server.Execute("mir-decision-functions-restored.sql", """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.mir_decision_functions_before b LEFT JOIN pg_proc p ON p.oid=b.oid WHERE to_jsonb(p) IS DISTINCT FROM b.data) THEN
                RAISE EXCEPTION 'Function body or metadata not exactly restored';
              END IF;
              IF EXISTS(SELECT 1 FROM advance.roles WHERE "Code"='MIR_APPROVER') THEN RAISE EXCEPTION 'Decision role retained after empty rollback'; END IF;
            END $assert$;
            """);
        server.Execute("mir-decision-up-again.sql",up);
        server.Execute("mir-decision-retained-assignment.sql", """
            DO $fixture$ DECLARE authority uuid; source advance.employee_role_assignments%ROWTYPE; BEGIN
              SELECT a."Id" INTO STRICT authority FROM advance.employee_role_assignments a JOIN advance.employees e ON e."Id"=a."EmployeeId"
                JOIN advance.roles r ON r."Id"=a."RoleId" WHERE e."EmployeeCode"='SESS-01' AND r."Code"='TECHNICAL_DIRECTOR'
                  AND a."CompanyId"='70000000-0000-0000-0000-000000000001' AND a."EffectiveTo" IS NULL LIMIT 1;
              PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
              SELECT a.* INTO STRICT source FROM advance.employee_role_assignments a JOIN advance.employees e ON e."Id"=a."EmployeeId"
                WHERE e."EmployeeCode"='SESS-35' AND a."CompanyId"='70000000-0000-0000-0000-000000000001' AND a."EffectiveTo" IS NULL AND a."AssignmentType"='FULL' LIMIT 1;
              INSERT INTO advance.employee_role_assignments SELECT * FROM jsonb_populate_record(NULL::advance.employee_role_assignments,
                to_jsonb(source)||jsonb_build_object('Id',gen_random_uuid(),'RoleId',md5('RC26:MIR_APPROVER')::uuid,
                  'CreatedBy','MIR-DECISION-FIXTURE','EffectiveFrom',advance.current_business_date(),'Version',0));
            END $fixture$;
            """);
        server.AssertRejected("mir-decision-down-retained-refused.sql",down,"refuses retained assignments or decisions");
    }

}
