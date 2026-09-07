using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260907114500_EnableWorkflowDevelopmentLogins")]
public sealed class EnableWorkflowDevelopmentLogins : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $workflow_logins$
            DECLARE matched integer;
            BEGIN
              SELECT count(*) INTO matched
              FROM advance.employees
              WHERE "EmployeeCode"=ANY(ARRAY['SESS-02','SESS-14','SESS-15','SESS-16','SESS-25','SESS-33','SESS-35','SESS-41']::text[]);
              IF matched<>8 THEN
                RAISE EXCEPTION USING ERRCODE='55000',
                  MESSAGE=format('Workflow development login enablement expected 8 employees; found %s.',matched);
              END IF;

              UPDATE advance.employees
              SET "LoginEnabled"=true,
                  "UpdatedAt"=clock_timestamp(),
                  "UpdatedBy"='migration-workflow-development-logins'
              WHERE "EmployeeCode"=ANY(ARRAY['SESS-02','SESS-14','SESS-15','SESS-16','SESS-25','SESS-33','SESS-35','SESS-41']::text[])
                AND NOT "LoginEnabled";
            END $workflow_logins$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            UPDATE advance.employees
            SET "LoginEnabled"=false,
                "UpdatedAt"=clock_timestamp(),
                "UpdatedBy"='migration-workflow-development-logins-down'
            WHERE "EmployeeCode"=ANY(ARRAY['SESS-02','SESS-14','SESS-15','SESS-16','SESS-25','SESS-33','SESS-35','SESS-41']::text[])
              AND "LoginEnabled"
              AND "UpdatedBy"='migration-workflow-development-logins';
            """);
    }
}
