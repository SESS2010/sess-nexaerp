using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task NamedAccountsIdentityMigrationKeepsStandardHumanUniquenessAndRefusesRetainedIdentityDown()
    {
        const string before="20261017150000_IndependentMdOpeningValueReview";
        const string after="20261017160000_NamedAccountsBackupIdentity";
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator=model.GetService<IMigrator>();
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("accounts-identity-before.sql",migrator.GenerateScript("0",before));
        // Purchase and other controlled writes now share the ordinary ledger. The retired
        // table must stay absent; neither rollback nor new authority may depend on it.
        server.Execute("accounts-current-ledger-precondition.sql","""
            DO $assert$ BEGIN
              IF to_regclass('advance.command_requests') IS NULL OR to_regclass('advance.rev869b_command_requests') IS NOT NULL THEN
                RAISE EXCEPTION 'Expected the current ordinary ledger and no retired purchase table.';
              END IF;
            END $assert$;
            """);
        server.Execute("accounts-identity-baseline.sql","""
            CREATE TABLE public.accounts_register_before AS SELECT to_jsonb(p) data FROM pg_proc p
              WHERE p.oid='advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)'::regprocedure;
            CREATE TABLE public.accounts_identity_indexes_before AS SELECT indexname,indexdef FROM pg_indexes
              WHERE schemaname='advance' AND tablename='employee_identity_mappings';
            CREATE TABLE public.accounts_identity_rows_before AS SELECT "Id",to_jsonb(m) data FROM advance.employee_identity_mappings m;
            CREATE TABLE public.accounts_identity_grants_before AS SELECT "Id",to_jsonb(g) data FROM advance.role_page_permissions g;
            CREATE TABLE public.accounts_identity_assignments_before AS SELECT "Id",to_jsonb(a) data FROM advance.employee_role_assignments a;
            """);
        var up=migrator.GenerateScript(before,after);var down=migrator.GenerateScript(after,before);
        server.Execute("accounts-identity-up.sql",up);
        const string unchanged="""
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.accounts_identity_indexes_before b LEFT JOIN pg_indexes i
                  ON i.schemaname='advance' AND i.tablename='employee_identity_mappings' AND i.indexname=b.indexname
                  WHERE i.indexdef IS DISTINCT FROM b.indexdef) OR
                 EXISTS(SELECT 1 FROM public.accounts_identity_rows_before b LEFT JOIN advance.employee_identity_mappings m ON m."Id"=b."Id"
                  WHERE to_jsonb(m) IS DISTINCT FROM b.data) OR
                 EXISTS(SELECT 1 FROM public.accounts_identity_grants_before b LEFT JOIN advance.role_page_permissions g ON g."Id"=b."Id"
                  WHERE to_jsonb(g) IS DISTINCT FROM b.data) OR
                 EXISTS(SELECT 1 FROM public.accounts_identity_assignments_before b LEFT JOIN advance.employee_role_assignments a ON a."Id"=b."Id"
                  WHERE to_jsonb(a) IS DISTINCT FROM b.data) OR
                 (SELECT count(*) FROM advance.employee_identity_mappings)<>(SELECT count(*) FROM public.accounts_identity_rows_before) OR
                 (SELECT count(*) FROM advance.employee_role_assignments)<>(SELECT count(*) FROM public.accounts_identity_assignments_before) THEN
                RAISE EXCEPTION 'Accounts identity install modified an existing identity/index/grant/assignment.';
              END IF;
            END $assert$;
            """;
        server.Execute("accounts-identity-unchanged.sql",unchanged);
        server.Execute("accounts-register-up-metadata.sql","""
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.accounts_register_before b CROSS JOIN pg_proc p
                WHERE p.oid='advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)'::regprocedure
                  AND (to_jsonb(p)-'prosrc') IS DISTINCT FROM (b.data-'prosrc')) THEN
                RAISE EXCEPTION 'Accounts profile rewrite changed register signature/owner/ACL/search_path metadata.';
              END IF;
            END $assert$;
            """);
        server.Execute("accounts-identity-function.sql","""
            DO $assert$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid='advance.guard_accounts_backup_identity()'::regprocedure
                  AND p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog, advance']) OR
                 EXISTS(SELECT 1 FROM pg_proc p CROSS JOIN LATERAL aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                  WHERE p.oid='advance.guard_accounts_backup_identity()'::regprocedure AND a.grantee=0) THEN
                RAISE EXCEPTION 'Accounts identity guard metadata or PUBLIC privilege differs.';
              END IF;
            END $assert$;
            """);
        server.Execute("accounts-identity-roundtrip-down.sql",down);
        server.Execute("accounts-identity-after-down.sql",unchanged+"""
            DO $assert$ BEGIN
              IF to_regprocedure('advance.guard_accounts_backup_identity()') IS NOT NULL OR
                 to_regclass('advance."IX_employee_identity_accounts_backup"') IS NOT NULL THEN
                RAISE EXCEPTION 'Accounts identity rollback left its function/index.';
              END IF;
            END $assert$;
            """);
        server.Execute("accounts-register-restored.sql","""
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.accounts_register_before b CROSS JOIN pg_proc p
                WHERE p.oid='advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)'::regprocedure
                  AND to_jsonb(p) IS DISTINCT FROM b.data) OR
                to_regprocedure('advance.accounts_identity_role_allowed(text,uuid,text,text,text,text,uuid)') IS NOT NULL THEN
                RAISE EXCEPTION 'Accounts identity Down did not restore the exact ordinary register definition and metadata.';
              END IF;
            END $assert$;
            """);
        server.Execute("accounts-identity-reapply.sql",up);
        var password="accounts-identity-disposable-"+Guid.NewGuid().ToString("N");
        using var environment=new OrdinaryPrincipalEnvironment(server.ConnectionString,password);
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        var options=new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options;
        await using(var db=new NexaErpDbContext(options))
        {
            var date=db.BusinessDate;
            var karthick=await db.Employees.SingleAsync(e=>e.EmployeeCode=="SESS-41");
            var md=await db.Employees.SingleAsync(e=>e.EmployeeCode=="SESS-02");
            var other=await db.Employees.SingleAsync(e=>e.EmployeeCode=="SESS-35");
            karthick.LoginEnabled=true;md.LoginEnabled=true;other.LoginEnabled=true;await db.SaveChangesAsync();
            foreach(var company in await db.Companies.Where(c=>c.Code=="SESS_PVT_LTD"||c.Code=="SESS_PROPRIETORSHIP").ToListAsync())
            {
                const string staff="https://accounts-identity.test/realms/staff";
                const string approvers="https://accounts-identity.test/realms/approvers";
                db.EmployeeIdentityMappings.AddRange(
                    new(){CompanyId=company.Id,OrganizationId=company.Code,EmployeeId=karthick.Id,Issuer=staff,Subject="stores-subject",IdentityType=IdentityTypes.Human,EffectiveFrom=date},
                    new(){CompanyId=company.Id,OrganizationId=company.Code,EmployeeId=md.Id,Issuer=approvers,Subject="md-subject",IdentityType=IdentityTypes.Human,EffectiveFrom=date});
                await db.SaveChangesAsync();
                async Task Refuse(Guid employee,string issuer,string subject,string state)
                {
                    await using var invalid=new NexaErpDbContext(options);
                    invalid.EmployeeIdentityMappings.Add(new(){CompanyId=company.Id,OrganizationId=company.Code,EmployeeId=employee,
                        Issuer=issuer,Subject=subject,IdentityType=AccountsBackupIdentityPolicy.MappingType,EffectiveFrom=date});
                    var exception=await Assert.ThrowsAsync<DbUpdateException>(()=>invalid.SaveChangesAsync());
                    Assert.Equal(state,Assert.IsType<PostgresException>(exception.InnerException).SqlState);
                }
                await Refuse(other.Id,approvers,"other-employee-backup","42501");
                await Refuse(karthick.Id,staff,"wrong-realm-backup","42501");
                await Refuse(karthick.Id,approvers,"stores-subject","42501");
                db.EmployeeIdentityMappings.Add(new(){CompanyId=company.Id,OrganizationId=company.Code,EmployeeId=karthick.Id,
                    Issuer=approvers,Subject="accounts-subject",IdentityType=AccountsBackupIdentityPolicy.MappingType,EffectiveFrom=date});
                await db.SaveChangesAsync();
                await Refuse(karthick.Id,approvers,"second-accounts-subject","23505");
                Assert.Equal(2,await db.EmployeeIdentityMappings.CountAsync(m=>m.CompanyId==company.Id&&m.EmployeeId==karthick.Id&&m.IsActive));
                Assert.Equal(1,await db.EmployeeIdentityMappings.CountAsync(m=>m.CompanyId==company.Id&&m.EmployeeId==karthick.Id&&m.IdentityType==IdentityTypes.Human));
            }
        }
        var retained=Assert.ThrowsAny<Exception>(()=>server.Execute("accounts-identity-retained-down-refused.sql",down));
        Assert.Contains("Accounts identity rollback refuses retained",retained.Message);
        server.Execute("accounts-identity-fixture-revoke.sql","""
            UPDATE advance.employee_identity_mappings SET "IsActive"=false,
              "EffectiveTo"=advance.current_business_date(),"UpdatedAt"=clock_timestamp(),
              "UpdatedBy"='ACCOUNTS_IDENTITY_FIXTURE_CLOSE',"Version"="Version"+1
              WHERE "IdentityType"='HUMAN_ACCT_BACKUP';
            DO $assert$ BEGIN
              IF (SELECT count(*) FROM advance.employee_identity_mappings WHERE "IdentityType"='HUMAN_ACCT_BACKUP'
                  AND NOT "IsActive" AND "EffectiveTo" IS NOT NULL)<>2 THEN
                RAISE EXCEPTION 'Both company identities must remain as closed history.';
              END IF;
            END $assert$;
            """);
        var revoked=Assert.ThrowsAny<Exception>(()=>server.Execute("accounts-identity-revoked-down-refused.sql",down));
        Assert.Contains("Accounts identity rollback refuses retained",revoked.Message);
        server.Execute("accounts-identity-history-still-retained.sql","""
            DO $assert$ BEGIN
              IF (SELECT count(*) FROM advance.employee_identity_mappings WHERE "IdentityType"='HUMAN_ACCT_BACKUP')<>2 THEN
                RAISE EXCEPTION 'Refused Down must preserve both retained identity rows.';
              END IF;
            END $assert$;
            """);
    }
}
