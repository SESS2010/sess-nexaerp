using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260907220500_CompleteSeededWorkflowDevelopmentLogins")]
public sealed class CompleteSeededWorkflowDevelopmentLogins : Migration
{
    private const string ChangedBy = "migration-complete-workflow-development-logins";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql($$"""
            DO $workflow_logins$
            DECLARE matched integer;
            BEGIN
              SELECT count(*) INTO matched
              FROM advance.employees
              WHERE "EmployeeCode"=ANY(ARRAY['SESS-01','SESS-04','SESS-12']::text[])
                AND upper("Status")='ACTIVE';
              IF matched<>3 THEN
                RAISE EXCEPTION USING ERRCODE='55000',
                  MESSAGE=format('Workflow login completion expected 3 active seeded employees; found %s.',matched);
              END IF;

              UPDATE advance.employees
              SET "LoginEnabled"=true,
                  "UpdatedAt"=clock_timestamp(),
                  "UpdatedBy"='{{ChangedBy}}'
              WHERE "EmployeeCode"=ANY(ARRAY['SESS-01','SESS-04','SESS-12']::text[])
                AND upper("Status")='ACTIVE'
                AND NOT "LoginEnabled";
            END $workflow_logins$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql($$"""
            UPDATE advance.employees
            SET "LoginEnabled"=false,
                "UpdatedAt"=clock_timestamp(),
                "UpdatedBy"='{{ChangedBy}}-down'
            WHERE "EmployeeCode"=ANY(ARRAY['SESS-01','SESS-04','SESS-12']::text[])
              AND "LoginEnabled"
              AND "UpdatedBy"='{{ChangedBy}}';
            """);
    }
}
