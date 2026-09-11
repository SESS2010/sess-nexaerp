#if DEBUG
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Identity;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task DevelopmentWorkflowIdentitiesConvergeLegacySubjectsAndRefuseUnrelatedMappings()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migration = db.Database.GetMigrations().Last();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("development-workflow-identities-up.sql", migrator.GenerateScript("0", migration));
        server.Execute("development-workflow-identities-legacy.sql", """
            INSERT INTO advance.employee_identity_mappings
              ("Id","CompanyId","OrganizationId","Issuer","Subject","EmployeeId","IdentityType",
               "EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version")
            SELECT gen_random_uuid(),company."Id",company."Code",'urn:nexaerp:development',
                   'dev-'||lower(employee."EmployeeCode"),employee."Id",'HUMAN',
                   current_date,true,clock_timestamp(),'LEGACY_DEVELOPMENT_IDENTITY',0
            FROM advance.employees employee
            CROSS JOIN advance.companies company
            WHERE employee."EmployeeCode" IN ('SESS-04','SESS-12');
            """);

        using var environment = DevelopmentWorkflowIdentityEnvironment(server.ConnectionString);
        var arguments = new[] { "workflow-identities-development", "provision" };
        Assert.Equal(0, await InstallerCommand.RunAsync(arguments));
        Assert.Equal(0, await InstallerCommand.RunAsync(arguments));
        server.Execute("development-workflow-identities-witness.sql", """
            DO $assert$
            DECLARE expected_codes constant text[] := ARRAY[
              'SESS-01','SESS-02','SESS-04','SESS-12','SESS-14','SESS-15',
              'SESS-16','SESS-25','SESS-33','SESS-35','SESS-41'];
            BEGIN
              IF (SELECT count(*) FROM advance.employee_identity_mappings mapping
                  JOIN advance.employees employee ON employee."Id"=mapping."EmployeeId"
                  WHERE employee."EmployeeCode"=ANY(expected_codes)
                    AND mapping."Issuer"='urn:nexaerp:development'
                    AND mapping."Subject"=employee."EmployeeCode"
                    AND mapping."IdentityType"='HUMAN' AND mapping."IsActive")<>22 THEN
                RAISE EXCEPTION 'Expected 22 active exact development mappings.';
              END IF;
              IF (SELECT count(*) FROM advance.employee_identity_mappings
                  WHERE "CreatedBy"='DEVELOPMENT_WORKFLOW_IDENTITIES')<>22 THEN
                RAISE EXCEPTION 'Idempotent replay created additional mappings.';
              END IF;
              IF (SELECT count(*) FROM advance.employee_identity_mappings
                  WHERE "CreatedBy"='LEGACY_DEVELOPMENT_IDENTITY' AND NOT "IsActive"
                    AND "EffectiveTo"=current_date AND "Version"=1
                    AND "UpdatedBy"='DEVELOPMENT_WORKFLOW_IDENTITIES')<>4 THEN
                RAISE EXCEPTION 'The four legacy company mappings were not closed immutably.';
              END IF;
              IF (SELECT count(*) FROM advance.audit_logs
                  WHERE "CreatedBy"='DEVELOPMENT_WORKFLOW_IDENTITIES'
                    AND "Action"='DevelopmentIdentityConverged')<>22 THEN
                RAISE EXCEPTION 'Expected one immutable convergence audit row per active mapping.';
              END IF;
              IF (SELECT count(*) FROM advance.audit_logs
                  WHERE "CreatedBy"='DEVELOPMENT_WORKFLOW_IDENTITIES'
                    AND "Action"='LegacyDevelopmentIdentityClosed')<>4 THEN
                RAISE EXCEPTION 'Expected one immutable closure audit row per legacy mapping.';
              END IF;
            END $assert$;
            """);

        await using (var resolverDb = new NexaErpDbContext(
            new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options))
        {
            var resolver = new EfEmployeeIdentityResolver(resolverDb);
            var databaseDate = await resolverDb.Database
                .SqlQueryRaw<DateOnly>("SELECT CURRENT_DATE AS \"Value\"").SingleAsync();
            foreach (var employeeCode in new[] {
                         "SESS-01","SESS-02","SESS-04","SESS-12","SESS-14","SESS-15",
                         "SESS-16","SESS-25","SESS-33","SESS-35","SESS-41" })
            foreach (var organization in new[] { "SESS_PVT_LTD", "SESS_PROPRIETORSHIP" })
            {
                var resolved = await resolver.ResolveAsync(
                    "urn:nexaerp:development", employeeCode, organization, databaseDate, default);
                Assert.True(resolved.Success, $"{employeeCode}/{organization}: {resolved.Message}");
                Assert.Equal(employeeCode, resolved.EmployeeCode);
            }
        }
        server.Execute("development-workflow-identities-unrelated.sql", """
            UPDATE advance.employee_identity_mappings mapping
               SET "EffectiveTo"=current_date,"IsActive"=false,"UpdatedAt"=clock_timestamp(),
                   "UpdatedBy"='TEST_CLOSE',"Version"=mapping."Version"+1
              FROM advance.employees employee
             WHERE mapping."EmployeeId"=employee."Id" AND employee."EmployeeCode"='SESS-01'
               AND mapping."CompanyId"='70000000-0000-0000-0000-000000000001'
               AND mapping."IsActive";
            INSERT INTO advance.employee_identity_mappings
              ("Id","CompanyId","OrganizationId","Issuer","Subject","EmployeeId","IdentityType",
               "EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version")
            SELECT gen_random_uuid(),'70000000-0000-0000-0000-000000000001',
                   'SESS_PVT_LTD','https://unrelated.example.test','unrelated-subject',
                   "Id",'HUMAN',current_date,true,clock_timestamp(),'UNRELATED_TEST',0
              FROM advance.employees WHERE "EmployeeCode"='SESS-01';
            """);
        Assert.Equal(1, await InstallerCommand.RunAsync(arguments));
        server.Execute("development-workflow-identities-refusal-witness.sql", """
            DO $assert$
            BEGIN
              IF (SELECT count(*) FROM advance.employee_identity_mappings WHERE "CreatedBy"='DEVELOPMENT_WORKFLOW_IDENTITIES')<>22 THEN
                RAISE EXCEPTION 'Refused convergence changed development mappings.';
              END IF;
              IF (SELECT count(*) FROM advance.employee_identity_mappings WHERE "CreatedBy"='UNRELATED_TEST' AND "IsActive")<>1 THEN
                RAISE EXCEPTION 'Refused convergence changed the unrelated mapping.';
              END IF;
            END $assert$;
            """);
        server.Execute("development-workflow-identities-close-unrelated.sql", """
            UPDATE advance.employee_identity_mappings
               SET "EffectiveTo"=current_date,"IsActive"=false,"UpdatedAt"=clock_timestamp(),
                   "UpdatedBy"='TEST_CLOSE_UNRELATED',"Version"="Version"+1
             WHERE "CreatedBy"='UNRELATED_TEST' AND "IsActive";
            """);
        Assert.Equal(0, await InstallerCommand.RunAsync(arguments));

        server.Execute("development-workflow-identities-principals.sql",
            InstallerPasswordSettings + DatabasePrincipalProvisioningSql.Provision +
            DatabasePrincipalProvisioningSql.Verify);
        server.Execute("development-workflow-identities-command-witness.sql", """
            CREATE TEMP TABLE identity_write_witness AS
            SELECT employee."Id",employee."EmployeeCode",resolved."AssignmentId",resolved."RoleCode"
            FROM advance.employees employee
            CROSS JOIN LATERAL advance.resolve_employee_role_authority(
              employee."Id",'70000000-0000-0000-0000-000000000001',current_date,'create',
              ARRAY(SELECT role."Code"
                      FROM advance.employee_role_assignments assignment
                      JOIN advance.roles role ON role."Id"=assignment."RoleId"
                     WHERE assignment."CompanyId"='70000000-0000-0000-0000-000000000001'
                       AND assignment."EmployeeId"=employee."Id"
                       AND assignment."ApprovalStatus" IN ('Approved','SeedApproved')
                       AND assignment."EffectiveFrom"<=current_date
                       AND (assignment."EffectiveTo" IS NULL OR assignment."EffectiveTo">=current_date))) resolved
            WHERE employee."EmployeeCode"=ANY(ARRAY[
              'SESS-01','SESS-02','SESS-04','SESS-12','SESS-14','SESS-15',
              'SESS-16','SESS-25','SESS-33','SESS-35','SESS-41']);

            GRANT SELECT ON identity_write_witness TO nexa_erp_runtime;

            SET SESSION AUTHORIZATION nexa_erp_runtime;
            DO $commands$
            DECLARE
              employee record;
              command_id uuid;
              receipt_id uuid;
            BEGIN
              FOR employee IN SELECT * FROM identity_write_witness ORDER BY "EmployeeCode"
              LOOP
                command_id := advance.register_command_request(
                  'SESS_PVT_LTD','development:identity-write-witness:create',
                  sha256(convert_to('key-'||employee."EmployeeCode",'UTF8')),
                  sha256(convert_to('request-'||employee."EmployeeCode",'UTF8')),
                  employee."Id",'urn:nexaerp:development',employee."EmployeeCode",
                  employee."RoleCode",employee."AssignmentId");
                receipt_id := advance.commit_command_receipt(
                  command_id,sha256(convert_to('business-'||employee."EmployeeCode",'UTF8')),
                  jsonb_build_object('employeeCode',employee."EmployeeCode"),gen_random_uuid());
              END LOOP;
            END $commands$;
            RESET SESSION AUTHORIZATION;
            DO $assert$
            BEGIN
              IF (SELECT count(*) FROM advance.command_requests
                    WHERE "Operation"='development:identity-write-witness:create')<>11
                 OR (SELECT count(*) FROM advance.command_receipts receipt
                     JOIN advance.command_requests request ON request."CommandId"=receipt."CommandId"
                    WHERE request."Operation"='development:identity-write-witness:create')<>11 THEN
                RAISE EXCEPTION 'Every development identity must complete one atomic ordinary command write.';
              END IF;
              IF EXISTS (
                SELECT 1 FROM advance.command_requests request
                WHERE request."Operation"='development:identity-write-witness:create'
                  AND NOT EXISTS (
                    SELECT 1 FROM advance.employee_identity_mappings mapping
                    WHERE mapping."CompanyId"='70000000-0000-0000-0000-000000000001'
                      AND mapping."EmployeeId"=request."ActorEmployeeId"
                      AND mapping."Issuer"=request."IdentityIssuer"
                      AND mapping."Subject"=request."IdentitySubject"
                      AND mapping."IsActive")) THEN
                RAISE EXCEPTION 'A controlled write lacks its exact effective development identity mapping.';
              END IF;
            END $assert$;
            """);
    }

    private static IDisposable DevelopmentWorkflowIdentityEnvironment(string connectionString) => new EnvironmentVariables(
        ("DOTNET_ENVIRONMENT", "Development"),
        (InstallerCommand.DevelopmentWorkflowIdentitiesSetting, "true"),
        ("ConnectionStrings__NexaErpDevelopmentBootstrap", connectionString),
        ("NexaErp__ExpectedDatabase", "advance_parser"));
}
#endif