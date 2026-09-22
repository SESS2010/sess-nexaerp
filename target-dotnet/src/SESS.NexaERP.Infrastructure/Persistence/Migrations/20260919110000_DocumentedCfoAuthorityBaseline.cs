using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260919110000_DocumentedCfoAuthorityBaseline")]
public sealed class DocumentedCfoAuthorityBaseline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN ('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Documented CFO baseline refuses this database.'; END IF;
             IF to_regclass('advance.employee_role_assignment_events') IS NULL
              OR to_regprocedure('advance.resolve_employee_role_authority(uuid,uuid,date,text,text[])') IS NULL
             THEN RAISE EXCEPTION 'Documented CFO baseline requires governed employee role history.'; END IF;
            END $guard$;
            """);
        using var stream = typeof(DocumentedCfoAuthorityBaseline).Assembly.GetManifestResourceStream("InventoryPeriodCfoBaseline.20260919110000.sql")
            ?? throw new InvalidOperationException("Missing documented CFO baseline SQL.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$
            DECLARE role_id uuid:=md5('role:CHIEF_FINANCIAL_OFFICER')::uuid; assignment record; authority uuid;
            BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN ('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Documented CFO baseline rollback refuses this database.'; END IF;
             LOCK TABLE advance.roles,advance.company_role_activations,advance.employee_role_assignments,
              advance.employee_role_assignment_events,advance.command_requests,advance.role_page_permissions IN ACCESS EXCLUSIVE MODE;
             IF to_regclass('advance.inventory_period_events') IS NOT NULL
              OR EXISTS(SELECT 1 FROM advance.role_page_permissions WHERE "RoleId"=role_id)
              OR EXISTS(SELECT 1 FROM advance.command_requests WHERE "ActorRoleCode"='CHIEF_FINANCIAL_OFFICER')
              OR (SELECT count(*) FROM advance.roles WHERE "Id"=role_id AND "Code"='CHIEF_FINANCIAL_OFFICER'
                AND "Version"=0 AND "CreatedBy"='migration-governed-inventory-periods' AND "UpdatedAt" IS NULL
                AND "Name"='Chief Financial Officer' AND "IsActive" AND "IsPrivileged" AND "IsEmployeeAssignable"
                AND "Audience"='INTERNAL_EMPLOYEE' AND "BusinessArea"='GOVERNANCE')<>1
              OR (SELECT count(*) FROM advance.employee_role_assignments WHERE "RoleId"=role_id)<>2
              OR (SELECT count(*) FROM advance.employee_role_assignments a JOIN advance.employees e ON e."Id"=a."EmployeeId"
                WHERE a."RoleId"=role_id AND e."EmployeeCode"='SESS-02' AND a."Version"=0 AND a."UpdatedAt" IS NULL
                AND a."Id"=md5('inventory-period-cfo-assignment:'||a."CompanyId"::text)::uuid
                AND a."CreatedBy"='migration-governed-inventory-periods' AND a."ApprovalStatus"='SeedApproved'
                AND a."AssignmentType"='FULL' AND a."EffectiveTo" IS NULL AND a."EndedAt" IS NULL
                AND a."EffectiveFrom"=(a."CreatedAt" AT TIME ZONE 'UTC')::date)<>2
              OR (SELECT count(*) FROM advance.company_role_activations WHERE "RoleId"=role_id)<>2
              OR (SELECT count(*) FROM advance.company_role_activations a WHERE a."RoleId"=role_id
                AND a."Version"=0 AND a."UpdatedAt" IS NULL AND a."IsEnabled" AND a."EffectiveTo" IS NULL
                AND a."Id"=md5('inventory-period-cfo-activation:'||a."CompanyId"::text)::uuid
                AND a."CreatedBy"='migration-governed-inventory-periods')<>2
              OR (SELECT count(*) FROM advance.employee_role_assignment_events WHERE
                "ToRoleCode"='CHIEF_FINANCIAL_OFFICER' OR "FromRoleCode"='CHIEF_FINANCIAL_OFFICER')<>2
              OR (SELECT count(*) FROM advance.employee_role_assignment_events WHERE "ToRoleCode"='CHIEF_FINANCIAL_OFFICER'
                AND "Operation"='BASELINE_CONFIRM' AND "Version"=0 AND "UpdatedAt" IS NULL
                AND "CreatedBy"='migration-governed-inventory-periods'
                AND "Id"=md5('inventory-period-cfo-event:'||"CompanyId"::text)::uuid)<>2
             THEN RAISE EXCEPTION 'CFO rollback refuses changed or used authority; only the untouched installation baseline can be removed.'; END IF;
             -- The DDL and two seed-event deletes share one transaction. Any refusal restores the guard.
             ALTER TABLE advance.employee_role_assignment_events DISABLE TRIGGER "TR_employee_role_assignment_event_immutable";
             DELETE FROM advance.employee_role_assignment_events WHERE "ToRoleCode"='CHIEF_FINANCIAL_OFFICER'
              AND "CreatedBy"='migration-governed-inventory-periods';
             ALTER TABLE advance.employee_role_assignment_events ENABLE TRIGGER "TR_employee_role_assignment_event_immutable";
             FOR assignment IN SELECT "Id","CompanyId" FROM advance.employee_role_assignments WHERE "RoleId"=role_id LOOP
              SELECT a."Id" INTO authority FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
               JOIN advance.employees e ON e."Id"=a."EmployeeId" WHERE a."CompanyId"=assignment."CompanyId"
                AND e."EmployeeCode"='SESS-01' AND r."Code"='TECHNICAL_DIRECTOR' AND a."AssignmentType"='FULL'
                AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=current_date
                AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date) ORDER BY a."Id" LIMIT 1;
              PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
              DELETE FROM advance.employee_role_assignments WHERE "Id"=assignment."Id";
             END LOOP;
             DELETE FROM advance.company_role_activations WHERE "RoleId"=role_id;
             DELETE FROM advance.roles WHERE "Id"=role_id;
            END $guard$;
            """);
    }
}
