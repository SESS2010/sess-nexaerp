using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task KolkataDatabaseRoleAndActivationDatesRefuseBeforeStartAndAfterEndAcrossUtcBoundary()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("date-role-baseline.sql", model.GetService<IMigrator>().GenerateScript("0", "20261017110000_KolkataEventSnapshotDates"));
        void Clock(DateTimeOffset instant) => server.Execute("date-role-clock.sql", $$"""
            CREATE OR REPLACE FUNCTION advance.current_business_date() RETURNS date
            LANGUAGE sql STABLE SECURITY INVOKER SET search_path=pg_catalog,advance
            AS $clock$ SELECT (TIMESTAMPTZ '{{instant.ToString("O",CultureInfo.InvariantCulture)}}' AT TIME ZONE 'Asia/Kolkata')::date $clock$;
            """);
        // Only the clock dependency is substituted by the owner of this disposable fixture.
        // No authorization function, permission predicate, role restriction or production test switch is changed.
        Clock(DateTimeOffset.Parse("2026-10-17T06:00:00Z", CultureInfo.InvariantCulture));
        server.Execute("date-role-fixture.sql", """
            CREATE TABLE public.date_role_probe(code text PRIMARY KEY,role_id uuid,assignment_id uuid,actor uuid,company uuid,other_company uuid,authority uuid);
            DO $prepare$
            DECLARE company uuid; other_company uuid; actor uuid; authority uuid; template_role advance.roles%ROWTYPE;
              template_activation advance.company_role_activations%ROWTYPE; template_assignment advance.employee_role_assignments%ROWTYPE;
              probe record; role_id uuid; assignment_id uuid; today date:=DATE '2026-10-17';
            BEGIN
              SELECT "Id" INTO STRICT company FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
              SELECT "Id" INTO STRICT other_company FROM advance.companies WHERE "Code"='SESS_PROPRIETORSHIP';
              SELECT "Id" INTO STRICT actor FROM advance.employees WHERE "EmployeeCode"='SESS-35';
              SELECT a."Id" INTO STRICT authority FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                WHERE a."CompanyId"=company AND a."EmployeeId"<>actor AND r."Code"='TECHNICAL_DIRECTOR'
                  AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN ('Approved','SeedApproved')
                  AND a."EffectiveFrom"<=today AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=today) LIMIT 1;
              PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
              SELECT * INTO STRICT template_role FROM advance.roles WHERE "Code"='STORES_EXECUTIVE';
              SELECT * INTO STRICT template_activation FROM advance.company_role_activations
                WHERE "CompanyId"=company AND "RoleId"=template_role."Id" LIMIT 1;
              SELECT * INTO STRICT template_assignment FROM advance.employee_role_assignments
                WHERE "CompanyId"=company AND "EmployeeId"=actor AND "RoleId"=template_role."Id"
                  AND "AssignmentType"='FULL' ORDER BY "EffectiveFrom" DESC LIMIT 1;
              FOR probe IN SELECT * FROM (VALUES
                ('DATE_ASG_START',today,NULL::date,today-100,NULL::date,'FULL'),
                ('DATE_ASG_END',today-100,today-1,today-100,NULL::date,'FULL'),
                ('DATE_ACT_START',today-100,NULL::date,today,NULL::date,'FULL'),
                ('DATE_ACT_END',today-100,NULL::date,today-100,today-1,'FULL'),
                ('DATE_SUPPORT',today-100,NULL::date,today-100,NULL::date,'SUPPORT')
              ) x(code,role_from,role_to,activation_from,activation_to,assignment_type)
              LOOP
                role_id:=gen_random_uuid(); assignment_id:=gen_random_uuid();
                INSERT INTO advance.roles SELECT * FROM jsonb_populate_record(NULL::advance.roles,
                  to_jsonb(template_role)||jsonb_build_object('Id',role_id,'Code',probe.code,'Name',probe.code,'RoleName',probe.code,'IsActive',true));
                INSERT INTO advance.company_role_activations SELECT * FROM jsonb_populate_record(NULL::advance.company_role_activations,
                  to_jsonb(template_activation)||jsonb_build_object('Id',gen_random_uuid(),'RoleId',role_id,'IsEnabled',true,
                    'EffectiveFrom',probe.activation_from,'EffectiveTo',probe.activation_to));
                INSERT INTO advance.employee_role_assignments SELECT * FROM jsonb_populate_record(NULL::advance.employee_role_assignments,
                  to_jsonb(template_assignment)||jsonb_build_object('Id',assignment_id,'RoleId',role_id,'AssignmentType',probe.assignment_type,
                    'ApprovalStatus','SeedApproved','EffectiveFrom',probe.role_from,'EffectiveTo',probe.role_to,
                    'EndReason',CASE WHEN probe.role_to IS NULL THEN NULL ELSE 'Boundary fixture end' END,'EndedAt',CASE WHEN probe.role_to IS NULL THEN NULL ELSE TIMESTAMPTZ '2026-10-16 12:00:00Z' END,'EndedBy',CASE WHEN probe.role_to IS NULL THEN NULL ELSE 'DATE_BOUNDARY_FIXTURE' END,'Version',0,'CreatedBy','DATE_BOUNDARY_FIXTURE'));
                INSERT INTO public.date_role_probe VALUES(probe.code,role_id,assignment_id,actor,company,other_company,authority);
              END LOOP;
            END $prepare$;
            """);
        await using var connection = new NpgsqlConnection(server.ConnectionString);
        await connection.OpenAsync();
        async Task AssertRole(string code, string operation, bool allowed, bool otherCompany = false)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT r.\"AssignmentType\" FROM public.date_role_probe p CROSS JOIN LATERAL advance.resolve_employee_role_authority(p.actor,p.{(otherCompany ? "other_company" : "company")},advance.current_business_date(),@operation,ARRAY[p.code]) r WHERE p.code=@code";
            command.Parameters.AddWithValue("operation", operation); command.Parameters.AddWithValue("code", code);
            if (allowed) Assert.Equal(code == "DATE_SUPPORT" ? "SUPPORT" : "FULL", (string?)await command.ExecuteScalarAsync());
            else
            {
                var error = await Assert.ThrowsAsync<PostgresException>(async () => await command.ExecuteScalarAsync());
                Assert.Equal("42501", error.SqlState);
            }
        }
        foreach (var (instant, started) in new[]
        {
            ("2026-10-16T18:29:59Z",false), ("2026-10-16T18:30:00Z",true),
            ("2026-10-16T19:30:00Z",true), ("2026-10-16T23:59:59Z",true), ("2026-10-17T00:00:00Z",true)
        })
        {
            Clock(DateTimeOffset.Parse(instant, CultureInfo.InvariantCulture));
            await AssertRole("DATE_ASG_START", "create", started);
            await AssertRole("DATE_ASG_END", "create", !started);
            await AssertRole("DATE_ACT_START", "create", started);
            await AssertRole("DATE_ACT_END", "create", !started);
            await AssertRole("DATE_SUPPORT", "create", true);
            foreach (var decision in new[] { "approve", "reject", "cancel", "reverse" })
                await AssertRole("DATE_SUPPORT", decision, false);
            await AssertRole("DATE_ASG_START", "create", false, otherCompany: true);
        }
        server.Execute("date-role-inactive.sql", """
            UPDATE advance.roles SET "IsActive"=false WHERE "Code"='DATE_ASG_START';
            UPDATE advance.company_role_activations SET "IsEnabled"=false
              WHERE "RoleId"=(SELECT "Id" FROM advance.roles WHERE "Code"='DATE_ACT_START');
            """);
        await AssertRole("DATE_ASG_START", "create", false);
        await AssertRole("DATE_ACT_START", "create", false);
    }
}