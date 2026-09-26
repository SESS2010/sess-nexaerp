using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Decided by the Technical Director on 25 September: no employee is login-enabled by default on a
/// fresh database; only people on the signed scope roster are enabled. The two September development
/// migrations enabled 11 employees on every database. This turns off exactly the flags they set and
/// nothing has changed since (their own UpdatedBy markers), and skips anyone with an active
/// non-development identity mapping, so a database where Step 6 has run keeps every mapped person.
///
/// It changes the flag only: never Status, mappings or roles. The SESS-12 authentication bootstrap
/// enables SESS-12 by itself, and anyone else is enabled through the governed activate-login.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260926090000_DevelopmentLoginsOffByDefault")]
public sealed class DevelopmentLoginsOffByDefault : Migration
{
    internal const string ChangedBy = "migration-development-logins-off-by-default";
    internal const string WorkflowMarker = "migration-workflow-development-logins";
    internal const string CompleteMarker = "migration-complete-workflow-development-logins";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql($$"""
            UPDATE advance.employees e
            SET "LoginEnabled"=false,
                "UpdatedAt"=clock_timestamp(),
                "UpdatedBy"='{{ChangedBy}}:'||e."UpdatedBy",
                "Version"=e."Version"+1
            WHERE e."LoginEnabled"
              AND e."UpdatedBy" IN ('{{WorkflowMarker}}','{{CompleteMarker}}')
              AND NOT EXISTS (
                SELECT 1 FROM advance.employee_identity_mappings m
                WHERE m."EmployeeId"=e."Id" AND m."IsActive"
                  AND m."Issuer"<>'urn:nexaerp:development');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        // The original marker is carried after the prefix, so Down restores exactly what Up changed.
        migrationBuilder.Sql($$"""
            UPDATE advance.employees e
            SET "LoginEnabled"=true,
                "UpdatedAt"=clock_timestamp(),
                "UpdatedBy"=substr(e."UpdatedBy",length('{{ChangedBy}}:')+1),
                "Version"=e."Version"+1
            WHERE NOT e."LoginEnabled"
              AND e."UpdatedBy" IN ('{{ChangedBy}}:{{WorkflowMarker}}','{{ChangedBy}}:{{CompleteMarker}}');
            """);
    }
}
