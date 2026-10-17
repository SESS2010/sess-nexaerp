using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task OnboardingPrimaryDepartmentCreatesBothCompaniesIdempotentlyAndRefusesTransferOrSelfAssignment()
    {
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("onboarding-schema.sql",model.GetService<IMigrator>().GenerateScript());
        server.Execute("onboarding-trial-masters.sql","\\set expected_database advance_parser\n"+File.ReadAllText(Path.Combine(FindRepositoryRoot(),"database","postgresql","trial-master-data-apply.sql")));
        var options=new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options;
        Guid employeeId,actorId,departmentId,tdId; string departmentCode; uint employeeVersion;
        var memberships=new Dictionary<string,(Guid Id,uint Version)>();
        await using(var db=new NexaErpDbContext(options))
        {
            var source=await db.Employees.SingleAsync(x=>x.EmployeeCode=="SESS-05");
            actorId=await db.Employees.Where(x=>x.EmployeeCode=="SESS-12").Select(x=>x.Id).SingleAsync();
            tdId=await db.Employees.Where(x=>x.EmployeeCode=="SESS-01").Select(x=>x.Id).SingleAsync();
            await db.Employees.Where(x=>x.Id==tdId || x.Id==actorId).ExecuteUpdateAsync(x=>x.SetProperty(e=>e.LoginEnabled,true));
            departmentId=source.DepartmentId;
            departmentCode=await db.Departments.Where(x=>x.Id==departmentId).Select(x=>x.Code).SingleAsync();
            var employee=new Employee {EmployeeCode="TEST-ONBOARD-NEW",EmployeeName="Disposable new employee",OriginalImportedName="Disposable new employee",
                EmployeeType=source.EmployeeType,Grade=source.Grade,DepartmentId=source.DepartmentId,DesignationId=source.DesignationId,
                LoginEnabled=true,CreatedBy="ONBOARDING_TEST"};
            db.Employees.Add(employee); employeeId=employee.Id;
            foreach(var company in await db.Companies.Where(x=>x.Code=="SESS_PVT_LTD"||x.Code=="SESS_PROPRIETORSHIP").ToListAsync())
            {
                var membership=new EmployeeCompanyAssignment {CompanyId=company.Id,EmployeeId=employeeId,EmployeeCode=employee.EmployeeCode,
                    AssignmentType="WORK",EmploymentType="PERMANENT",EffectiveFrom=new DateOnly(2026,10,17),CreatedBy="ONBOARDING_TEST"};
                db.EmployeeCompanyAssignments.Add(membership); memberships.Add(company.Code,(membership.Id,membership.Version));
                db.EmployeeIdentityMappings.Add(new EmployeeIdentityMapping {CompanyId=company.Id,OrganizationId=company.Code,
                    EmployeeId=employeeId,Issuer="https://onboarding.test/staff",Subject="test-new",EffectiveFrom=new DateOnly(2026,10,17),CreatedBy="ONBOARDING_TEST"});
                db.EmployeeIdentityMappings.Add(new EmployeeIdentityMapping {CompanyId=company.Id,OrganizationId=company.Code,
                    EmployeeId=tdId,Issuer="https://onboarding.test/approvers",Subject="td-fallback",EffectiveFrom=new DateOnly(2026,10,17),CreatedBy="ONBOARDING_TEST"});
                db.EmployeeIdentityMappings.Add(new EmployeeIdentityMapping {CompanyId=company.Id,OrganizationId=company.Code,
                    EmployeeId=actorId,Issuer="https://issuer.purchase-flow.test",Subject="SESS-12",EffectiveFrom=new DateOnly(2026,10,17),CreatedBy="ONBOARDING_TEST"});
            }
            await db.SaveChangesAsync();employeeVersion=employee.Version;
        }
        const string password="onboarding-disposable-runtime-123456789";
        using var environment=new OrdinaryPrincipalEnvironment(server.ConnectionString,password);
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        var runtime=new Npgsql.NpgsqlConnectionStringBuilder(server.ConnectionString){Username="nexa_erp_runtime",Password=password,Pooling=false}.ConnectionString;
        foreach(var organization in memberships.Keys)
        {
            var assignments=await Query(options,async db=>(await db.EmployeeRoleAssignments.Include(x=>x.Role)
                .Where(x=>x.EmployeeId==actorId && x.CompanyId==db.Companies.Where(c=>c.Code==organization).Select(c=>c.Id).Single()
                    && x.Role!.Code=="IT_MANAGER" && x.EffectiveTo==null).ToListAsync())
                .ToDictionary(x=>TaxWorkflowUser.AssignmentKey(x.EmployeeId,x.Role!.Code),x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)));
            Assert.Single(assignments);
            var user=new TaxWorkflowUser(actorId,"SESS-12","IT_MANAGER",assignments);user.SetOrganization(organization);
            await using var host=await PurchaseFlowHost.StartAsync(runtime,user,useRealPagePermissions:true);
            var read="/api/v1/rev869a/configuration/employee-onboarding/primary-departments?employeeCode=TEST-ONBOARD-NEW";
            var before=await Get<JsonElement>(host.Client,read);
            Assert.Empty(before.GetProperty("DepartmentAssignments").EnumerateArray());
            var parent=memberships[organization];
            var command=new EnsureOnboardingPrimaryDepartmentRequest("TEST-ONBOARD-NEW",organization,departmentCode,
                new DateOnly(2026,10,17),employeeVersion,parent.Id,parent.Version,"TD-DISPOSABLE-TEST",new string('A',64),"Signed primary department only","ensure-primary");
            const string path="/api/v1/rev869a/configuration/employee-onboarding/primary-department";
            var created=await Post<JsonElement>(host.Client,path,command);
            Assert.False(created.GetProperty("Replayed").GetBoolean());
            var replay=await Post<JsonElement>(host.Client,path,command);
            Assert.True(replay.GetProperty("Replayed").GetBoolean());
            Assert.Equal(created.GetProperty("Id").GetGuid(),replay.GetProperty("Id").GetGuid());
            await AssertPostStatus(host.Client,path,command with {Remarks="Changed content"},HttpStatusCode.Conflict);
            await AssertPostStatus(host.Client,path,command with {EffectiveFrom=new DateOnly(2026,10,18),IdempotencyKey="replace-refused"},HttpStatusCode.Conflict);
            await AssertPostStatus(host.Client,path,command with {EmployeeCode="SESS-12",IdempotencyKey="self-refused"},HttpStatusCode.Forbidden);
            await AssertPostStatus(host.Client,path,command with {OrganizationId=organization=="SESS_PVT_LTD"?"SESS_PROPRIETORSHIP":"SESS_PVT_LTD"},HttpStatusCode.Forbidden);
            var after=await Get<JsonElement>(host.Client,read);
            Assert.Single(after.GetProperty("DepartmentAssignments").EnumerateArray());
            _=await Post<JsonElement>(host.Client,"/api/v1/rev869a/configuration/operational-scopes",
                new SESS.NexaERP.Application.Rev869A.CreateOperationalScopeRequest(organization,"TEST-ONBOARD-NEW",departmentCode,null,null,true,false,
                    new DateOnly(2026,10,17),null,"Disposable requester own-record scope"));
            var tdDepartment=await Query(options,db=>db.Employees.Where(x=>x.Id==tdId).Join(db.Departments,e=>e.DepartmentId,d=>d.Id,(e,d)=>d.Code).SingleAsync());
            if(!await Query(options,db=>db.EmployeeOperationalScopes.AnyAsync(x=>x.EmployeeId==tdId && x.OrganizationId==organization && x.IsActive)))
                _=await Post<JsonElement>(host.Client,"/api/v1/rev869a/configuration/operational-scopes",
                    new SESS.NexaERP.Application.Rev869A.CreateOperationalScopeRequest(organization,"SESS-01",tdDepartment,null,null,false,false,
                        new DateOnly(2026,10,17),null,"Disposable existing-department TD scope; no PURCHASE membership"));

            await using var checkedDb=new NexaErpDbContext(options);
            var company=await checkedDb.Companies.Where(x=>x.Code==organization).Select(x=>x.Id).SingleAsync();
            Assert.Equal(1,await checkedDb.ControlledConfigurationHistories.CountAsync(x=>x.CompanyId==company && x.EntityId==created.GetProperty("Id").GetGuid() && x.Action=="EnsurePrimaryDepartment"));
            Assert.Equal(0,await checkedDb.EmployeeRoleAssignments.CountAsync(x=>x.EmployeeId==employeeId));
            Assert.Equal(employeeVersion,(await checkedDb.Employees.SingleAsync(x=>x.Id==employeeId)).Version);
            var resolved=await new EfEmployeeIdentityResolver(checkedDb).ResolveAsync("https://onboarding.test/staff","test-new",organization,new DateOnly(2026,10,17),default);
            Assert.True(resolved.Success,resolved.Message);Assert.Equal(departmentId,resolved.DepartmentId);
        }
        server.Execute("onboarding-requester-fixture.sql", """
            DO $fixture$ DECLARE company record; authority uuid; source advance.employee_role_assignments%ROWTYPE; target uuid; BEGIN
              SELECT "Id" INTO STRICT target FROM advance.employees WHERE "EmployeeCode"='TEST-ONBOARD-NEW';
              FOR company IN SELECT "Id" FROM advance.companies WHERE "Code" IN('SESS_PVT_LTD','SESS_PROPRIETORSHIP') LOOP
                SELECT a."Id" INTO STRICT authority FROM advance.employee_role_assignments a JOIN advance.employees e ON e."Id"=a."EmployeeId"
                  JOIN advance.roles r ON r."Id"=a."RoleId" WHERE e."EmployeeCode"='SESS-01' AND r."Code"='TECHNICAL_DIRECTOR'
                    AND a."CompanyId"=company."Id" AND a."EffectiveTo" IS NULL LIMIT 1;
                PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
                SELECT a.* INTO STRICT source FROM advance.employee_role_assignments a JOIN advance.employees e ON e."Id"=a."EmployeeId"
                  WHERE e."EmployeeCode"='SESS-35' AND a."CompanyId"='70000000-0000-0000-0000-000000000001' AND a."EffectiveTo" IS NULL AND a."AssignmentType"='FULL' LIMIT 1;
                INSERT INTO advance.employee_role_assignments SELECT * FROM jsonb_populate_record(NULL::advance.employee_role_assignments,
                  to_jsonb(source)||jsonb_build_object('Id',gen_random_uuid(),'EmployeeId',target,'CompanyId',company."Id",
                    'RoleId',md5('RC26:MIR_REQUESTER')::uuid,'EffectiveFrom',advance.current_business_date(),'EffectiveTo',advance.current_business_date()+1,
                    'EndReason','Disposable expiry proof','EndedAt',clock_timestamp(),'EndedBy','ONBOARDING_TEST',
                    'CreatedBy','ONBOARDING_TEST','Version',0));
                SELECT a."Id" INTO STRICT authority FROM advance.employee_role_assignments a JOIN advance.employees e ON e."Id"=a."EmployeeId"
                  JOIN advance.roles r ON r."Id"=a."RoleId" WHERE e."EmployeeCode"='SESS-02' AND r."Code"='MANAGING_DIRECTOR'
                    AND a."CompanyId"=company."Id" AND a."EffectiveTo" IS NULL LIMIT 1;
                PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
                INSERT INTO advance.employee_role_assignments SELECT * FROM jsonb_populate_record(NULL::advance.employee_role_assignments,
                  to_jsonb(source)||jsonb_build_object('Id',gen_random_uuid(),'EmployeeId',(SELECT "Id" FROM advance.employees WHERE "EmployeeCode"='SESS-01'),
                    'CompanyId',company."Id",'RoleId',md5('RC26:MIR_APPROVER')::uuid,'EffectiveFrom',advance.current_business_date(),
                    'CreatedBy','ONBOARDING_TD_FALLBACK_TEST','Version',0));
              END LOOP;
            END $fixture$;
            """);
        await using(var db=new NexaErpDbContext(options))
        {
            var resolver=new EfEmployeeIdentityResolver(db);
            var today=db.BusinessDate;
            foreach(var company in memberships.Keys)
            {
                var active=await resolver.ResolveAsync("https://onboarding.test/staff","test-new",company,today.AddDays(1),default);
                Assert.True(active.Success,active.Message);Assert.Equal(new[]{MirRequesterPolicy.Role},active.RoleCodes);
                Assert.Equal(departmentId,active.DepartmentId);
                var role=Assert.Single(active.EffectiveRoleAssignments!);
                var known=new Dictionary<string,EffectiveRoleAssignment>{{TaxWorkflowUser.AssignmentKey(employeeId,MirRequesterPolicy.Role),role}};
                var requester=new TaxWorkflowUser(employeeId,"test-new",MirRequesterPolicy.Role,known);
                requester.SetOrganization(company);requester.SetIdentityContext("https://onboarding.test/staff",departmentId);
                await using(var portalHost=await PurchaseFlowHost.StartAsync(runtime,requester,useRealPagePermissions:true,useRealOperationalScopes:true))
                {
                    var item=await db.Items.Where(x=>x.ItemCode=="TRIAL-ITEM-001").Select(x=>new{x.Id,x.BaseUomId}).SingleAsync();
                    var fields=new SESS.NexaERP.Application.Stores.CreateMaterialIssueRequest("DEMO","CONSUMABLE_OFFICE","DEPARTMENT",
                        null,null,null,departmentId,"Housekeeping department",departmentId,today,
                        [new SESS.NexaERP.Application.Stores.MaterialIssueRequestLineInput(item.Id,item.BaseUomId,1,null,"Employee job")],$"requester-create-{company}");
                    const string portal="/api/v1/employee/my-mirs";
                    var command=new SESS.NexaERP.Application.Stores.EmployeeMirCreate("Machine / job test",fields);
                    var errors = new QualificationRefusalErrorCapture();
                    portalHost.AddErrorCapture(errors);
                    SESS.NexaERP.Application.Stores.MaterialIssueRequestView created;
                    try { created=await Post<SESS.NexaERP.Application.Stores.MaterialIssueRequestView>(portalHost.Client,portal,command); }
                    catch(Exception exception) { throw new InvalidOperationException($"New requester create failed. Captured server exception: {errors.Error}",exception); }
                    Assert.Equal(employeeId,created.RequestedByEmployeeId);Assert.Equal("DRAFT",created.Status);
                    Assert.Contains("[Job/machine] Machine / job test",Assert.Single(created.Lines).Remarks);
                    Assert.Equal(created.Id,(await Post<SESS.NexaERP.Application.Stores.MaterialIssueRequestView>(portalHost.Client,portal,command)).Id);
                    await AssertPostStatus(portalHost.Client,portal,command with {JobOrMachine=""},HttpStatusCode.BadRequest);
                    await AssertPostStatus(portalHost.Client,portal,command with {Request=fields with {RequestingDepartmentId=Guid.NewGuid(),IdempotencyKey=$"foreign-department-{company}"}},HttpStatusCode.Forbidden);
                    using(var foreign=await portalHost.Client.GetAsync($"{portal}/{Guid.NewGuid()}"))Assert.Equal(HttpStatusCode.NotFound,foreign.StatusCode);
                    var submitted=await Post<SESS.NexaERP.Application.Stores.MaterialIssueRequestView>(portalHost.Client,$"{portal}/{created.Id}/submit",
                        new SESS.NexaERP.Application.Stores.MaterialIssueTransitionRequest(created.Version,"Need materials",$"requester-submit-{company}"));
                    Assert.Equal("SUBMITTED",submitted.Status);
                    var tdAssignment=await db.EmployeeRoleAssignments.Include(x=>x.Role).SingleAsync(x=>x.EmployeeId==tdId && x.Role!.Code==MirRequesterPolicy.ApproverRole
                        && x.CompanyId==db.Companies.Where(c=>c.Code==company).Select(c=>c.Id).Single());
                    var tdKnown=new Dictionary<string,EffectiveRoleAssignment>{{TaxWorkflowUser.AssignmentKey(tdId,MirRequesterPolicy.ApproverRole),
                        new(tdAssignment.Id,MirRequesterPolicy.ApproverRole,tdAssignment.AssignmentType)}};
                    var tdUser=new TaxWorkflowUser(tdId,"td-fallback",MirRequesterPolicy.ApproverRole,tdKnown);
                    tdUser.SetOrganization(company);tdUser.SetIdentityContext("https://onboarding.test/approvers",await db.Employees.Where(x=>x.Id==tdId).Select(x=>x.DepartmentId).SingleAsync());
                    await using(var fallbackHost=await PurchaseFlowHost.StartAsync(runtime,tdUser,useRealPagePermissions:true,useRealOperationalScopes:true))
                    {
                        var approved=await Post<SESS.NexaERP.Application.Stores.MaterialIssueRequestView>(fallbackHost.Client,$"/api/v1/stores/material-issue-requests/{created.Id}/approve",
                            new SESS.NexaERP.Application.Stores.MaterialIssueTransitionRequest(submitted.Version,"Independent TD fallback",$"td-fallback-approve-{company}"));
                        Assert.Equal("APPROVED",approved.Status);
                        Assert.Equal(1,await db.MaterialIssueHistories.CountAsync(x=>x.MaterialIssueRequestId==created.Id && x.Action=="APPROVE" && x.ActorEmployeeId==tdId && x.ActorRoleCode==MirRequesterPolicy.ApproverRole));
                    }

                    await AssertPostStatus(portalHost.Client,$"/api/v1/stores/material-issue-requests/{created.Id}/approve",
                        new SESS.NexaERP.Application.Stores.MaterialIssueTransitionRequest(submitted.Version,"Cannot self approve",$"requester-self-refused-{company}"),HttpStatusCode.Forbidden);
                }

                var expired=await resolver.ResolveAsync("https://onboarding.test/staff","test-new",company,today.AddDays(2),default);
                Assert.False(expired.Success);Assert.Contains("not currently effective",expired.Message);
                var beforeStart=await resolver.ResolveAsync("https://onboarding.test/staff","test-new",company,today.AddDays(-1),default);
                Assert.False(beforeStart.Success);
                var companyId=await db.Companies.Where(x=>x.Code==company).Select(x=>x.Id).SingleAsync();
                await db.EmployeeCompanyAssignments.Where(x=>x.EmployeeId==employeeId && x.CompanyId==companyId).ExecuteUpdateAsync(x=>x.SetProperty(a=>a.IsActive,false));
                Assert.False((await resolver.ResolveAsync("https://onboarding.test/staff","test-new",company,today,default)).Success);
                await db.EmployeeCompanyAssignments.Where(x=>x.EmployeeId==employeeId && x.CompanyId==companyId).ExecuteUpdateAsync(x=>x.SetProperty(a=>a.IsActive,true));
                var identity=await db.EmployeeIdentityMappings.AsNoTracking().SingleAsync(x=>x.EmployeeId==employeeId && x.CompanyId==companyId);
                var adminRole=await db.EmployeeRoleAssignments.Include(x=>x.Role).SingleAsync(x=>x.EmployeeId==actorId && x.CompanyId==companyId && x.Role!.Code=="IT_MANAGER" && x.EffectiveTo==null);
                var adminKnown=new Dictionary<string,EffectiveRoleAssignment>{{TaxWorkflowUser.AssignmentKey(actorId,"IT_MANAGER"),new(adminRole.Id,"IT_MANAGER",adminRole.AssignmentType)}};
                var admin=new TaxWorkflowUser(actorId,"SESS-12","IT_MANAGER",adminKnown);admin.SetOrganization(company);
                await using(var revokeHost=await PurchaseFlowHost.StartAsync(runtime,admin,useRealPagePermissions:true))
                    _=await Post<JsonElement>(revokeHost.Client,$"/api/v1/rev869a/configuration/employee-identities/{identity.Id}/revoke",
                        new RevokeEmployeeIdentityRequest(identity.Version,"Disposable requester revocation proof"));
                Assert.False((await resolver.ResolveAsync("https://onboarding.test/staff","test-new",company,today,default)).Success);

            }
        }

    }
}
