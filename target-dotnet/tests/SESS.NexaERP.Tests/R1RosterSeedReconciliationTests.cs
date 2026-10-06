using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Domain.Audit;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string R1RosterBefore = "20261003083000_MirPendingScopeAlignment";
    private const string R1RosterAfter = "20261003090000_R1RosterSeedReconciliation";
    private static readonly Dictionary<string, string[]> R1RosterRoles = new()
    {
        ["SESS-01"] = ["TECHNICAL_DIRECTOR"], ["SESS-02"] = ["CHIEF_FINANCIAL_OFFICER", "MANAGING_DIRECTOR"],
        ["SESS-12"] = ["IT_MANAGER"], ["SESS-14"] = ["ACCOUNTS_MANAGER"], ["SESS-15"] = ["PURCHASE_EXECUTIVE", "PURCHASE_MANAGER"],
        ["SESS-16"] = ["STORES_ASSISTANT"], ["SESS-17"] = ["DESIGN_ENGINEER", "SERVICE_ENGINEER"],
        ["SESS-19"] = ["DESIGN_ENGINEER", "SERVICE_ENGINEER"], ["SESS-21"] = ["HR_EXECUTIVE", "HR_MANAGER"],
        ["SESS-25"] = ["PRODUCTION_MANAGER"], ["SESS-28"] = ["ACCOUNTS_ASSISTANT"], ["SESS-33"] = ["QC_MANAGER"],
        ["SESS-35"] = ["STORES_EXECUTIVE"], ["SESS-41"] = ["STORES_MANAGER"]
    };

    [Fact]
    public async Task R1RosterCutoffRoundTripRetainsExceptionsAssignmentsAndImmutableHistory()
    {
        using var model = R1RosterModel();
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("roster-before.sql", migrator.GenerateScript("0", R1RosterBefore));
        server.Execute("roster-snapshots.sql", """
            CREATE TABLE public.r1_original_roles AS SELECT "Id",to_jsonb(a) row FROM advance.employee_role_assignments a;
            CREATE TABLE public.r1_original_events AS SELECT "Id" FROM advance.employee_role_assignment_events;
            CREATE TABLE public.r1_original_configuration AS
            SELECT 'employees' kind,md5(jsonb_agg(to_jsonb(e) ORDER BY "Id")::text) digest FROM advance.employees e UNION ALL
            SELECT 'identities',md5(jsonb_agg(to_jsonb(e) ORDER BY "Id")::text) FROM advance.employee_identity_mappings e UNION ALL
            SELECT 'memberships',md5(jsonb_agg(to_jsonb(e) ORDER BY "Id")::text) FROM advance.employee_company_assignments e UNION ALL
            SELECT 'scopes',md5(jsonb_agg(to_jsonb(e) ORDER BY "Id")::text) FROM advance.employee_operational_scopes e;
            """);
        var up = migrator.GenerateScript(R1RosterBefore, R1RosterAfter);
        var down = migrator.GenerateScript(R1RosterAfter, R1RosterBefore);
        server.Execute("roster-up.sql", up);
        await AssertR1RosterManifest(server.ConnectionString);
        server.Execute("roster-principal-reconcile.sql",InstallerPasswordSettings+DatabasePrincipalProvisioningSql.Provision+DatabasePrincipalProvisioningSql.Verify);
        server.Execute("roster-journal-acl.sql", """
            DO $check$ DECLARE relation text; BEGIN
              FOREACH relation IN ARRAY ARRAY['r1_roster_reconciliation_runs','r1_roster_reconciliation_entries','r1_roster_reconciliation_activations'] LOOP
                IF has_table_privilege('nexa_erp_runtime','advance.'||relation,'SELECT,INSERT,UPDATE,DELETE')
                  OR has_table_privilege('nexa_erp_bootstrap','advance.'||relation,'SELECT,INSERT,UPDATE,DELETE')
                  OR (SELECT pg_get_userbyid(relowner) FROM pg_class WHERE oid=('advance.'||relation)::regclass)<>'nexa_erp_owner' THEN
                  RAISE EXCEPTION 'R1 roster journals lost owner-only access after installer provisioning.';
                END IF;
              END LOOP;
            END $check$;
            """);
        server.Execute("roster-cutoff-and-history.sql", """
            DO $check$ BEGIN
              IF (SELECT count(*) FROM advance.employee_role_assignments WHERE "UpdatedBy"='R1RosterSeedReconciliation'
                   AND "EffectiveTo"=DATE '2026-10-09' AND "ApprovalStatus" IN('Approved','SeedApproved')
                   AND "EndReason" IS NOT NULL AND "EndedAt" IS NOT NULL AND "EndedBy" IS NOT NULL)<>6
                OR (SELECT count(*) FROM advance.employee_role_assignments WHERE "CreatedBy"='R1RosterSeedReconciliation'
                    AND "EffectiveFrom"=DATE '2026-10-10' AND "EffectiveTo" IS NULL AND "AssignmentType"='FULL'
                    AND "ApprovalStatus"='SeedApproved')<>2
                OR EXISTS(SELECT 1 FROM advance.employee_role_assignments a JOIN advance.employees e ON e."Id"=a."EmployeeId"
                    JOIN advance.roles r ON r."Id"=a."RoleId" WHERE e."EmployeeCode" IN('SESS-17','SESS-19')
                    AND r."Code" IN('DESIGN_ENGINEER','SERVICE_ENGINEER') AND a."ApprovalStatus" IN('Approved','SeedApproved')
                    AND (a."EffectiveTo" IS NOT NULL OR a."UpdatedBy"='R1RosterSeedReconciliation'))
                OR (SELECT count(*) FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                    JOIN advance.employees e ON e."Id"=a."EmployeeId" WHERE e."EmployeeCode"='SESS-21' AND r."Code"='HR_EXECUTIVE'
                    AND a."ApprovalStatus" IN('Approved','SeedApproved') AND a."EffectiveFrom"<=DATE '2026-10-09'
                    AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-10-09'))<>0
                OR EXISTS(SELECT 1 FROM public.r1_original_events o LEFT JOIN advance.employee_role_assignment_events e ON e."Id"=o."Id" WHERE e."Id" IS NULL)
                OR EXISTS(SELECT 1 FROM public.r1_original_roles o JOIN advance.employee_role_assignments a ON a."Id"=o."Id"
                    JOIN advance.employees e ON e."Id"=a."EmployeeId" WHERE e."EmployeeCode" IN('SESS-32','SESS-40') AND to_jsonb(a)<>o.row) THEN
                RAISE EXCEPTION 'R1 cutoff, corrected Design exceptions, DEMO exclusion or history preservation failed.';
              END IF;
              IF EXISTS(SELECT 1 FROM public.r1_original_configuration old JOIN
                (SELECT 'employees' kind,md5(jsonb_agg(to_jsonb(e) ORDER BY "Id")::text) digest FROM advance.employees e UNION ALL
                 SELECT 'identities',md5(jsonb_agg(to_jsonb(e) ORDER BY "Id")::text) FROM advance.employee_identity_mappings e UNION ALL
                 SELECT 'memberships',md5(jsonb_agg(to_jsonb(e) ORDER BY "Id")::text) FROM advance.employee_company_assignments e UNION ALL
                 SELECT 'scopes',md5(jsonb_agg(to_jsonb(e) ORDER BY "Id")::text) FROM advance.employee_operational_scopes e) current USING(kind)
                 WHERE old.digest IS DISTINCT FROM current.digest) THEN
                RAISE EXCEPTION 'R1 role reconciliation changed login, identity, company access or operational scopes.';
              END IF;
            END $check$;
            """);
        server.Execute("roster-probe-change.sql", """
            SELECT set_config('sess.role_authority_assignment_id',(SELECT a."Id"::text FROM advance.employee_role_assignments a
              JOIN advance.roles r ON r."Id"=a."RoleId" JOIN advance.employees e ON e."Id"=a."EmployeeId"
              WHERE r."Code"='TECHNICAL_DIRECTOR' AND e."EmployeeCode"='SESS-01' AND a."EffectiveTo" IS NULL ORDER BY a."CompanyId" LIMIT 1),false);
            UPDATE advance.employee_role_assignments SET "Remarks"='Later administrator edit' WHERE "Id"=
              (SELECT b.assignment_id FROM advance.r1_roster_reconciliation_entries b JOIN advance.employee_role_assignments a ON a."Id"=b.assignment_id
               WHERE b.before_row IS DISTINCT FROM b.after_row AND b.before_row IS NOT NULL ORDER BY a."CompanyId",a."Id" LIMIT 1);
            """);
        server.AssertRejected("roster-down-refuses-change.sql", down, "refuses changed, removed or additional assignments");
        server.Execute("roster-restore-probe.sql", """
            SELECT set_config('sess.role_authority_assignment_id',(SELECT a."Id"::text FROM advance.employee_role_assignments a
              JOIN advance.roles r ON r."Id"=a."RoleId" JOIN advance.employees e ON e."Id"=a."EmployeeId"
              WHERE r."Code"='TECHNICAL_DIRECTOR' AND e."EmployeeCode"='SESS-01' AND a."EffectiveTo" IS NULL ORDER BY a."CompanyId" LIMIT 1),false);
            UPDATE advance.employee_role_assignments a SET "Remarks"=b.after_row->>'Remarks'
              FROM advance.r1_roster_reconciliation_entries b WHERE b.assignment_id=a."Id" AND a."Remarks"='Later administrator edit';
            """);
        server.Execute("roster-down.sql", down);
        server.Execute("roster-down-retains-history.sql", """
            DO $check$ BEGIN
              IF EXISTS(SELECT 1 FROM public.r1_original_roles o LEFT JOIN advance.employee_role_assignments a ON a."Id"=o."Id" WHERE a."Id" IS NULL
                    OR a."EffectiveFrom" IS DISTINCT FROM (o.row->>'EffectiveFrom')::date OR a."EffectiveTo" IS DISTINCT FROM (o.row->>'EffectiveTo')::date
                    OR a."ApprovalStatus" IS DISTINCT FROM o.row->>'ApprovalStatus' OR a."AssignmentType" IS DISTINCT FROM o.row->>'AssignmentType')
                OR EXISTS(SELECT 1 FROM public.r1_original_events o LEFT JOIN advance.employee_role_assignment_events e ON e."Id"=o."Id" WHERE e."Id" IS NULL)
                OR (SELECT count(*) FROM advance.employee_role_assignments WHERE "CreatedBy"='R1RosterSeedReconciliation' AND "ApprovalStatus"='Ended')<>2
                OR EXISTS(SELECT 1 FROM advance.r1_roster_reconciliation_runs WHERE applied)
                OR NOT EXISTS(SELECT 1 FROM advance.employee_role_assignment_events WHERE "Operation"='R1_ROSTER_ROLLBACK') THEN
                RAISE EXCEPTION 'R1 Down lost authority history or failed to restore previous validity.';
              END IF;
            END $check$;
            """);
        server.Execute("roster-up-again.sql", up);
        await AssertR1RosterManifest(server.ConnectionString);
    }

    [Fact]
    public void R1RosterRefusesUnknownExtraWithoutApplyingPartialChanges()
    {
        using var model = R1RosterModel();
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("roster-extra-before.sql", migrator.GenerateScript("0", R1RosterBefore));
        server.Execute("roster-extra-probe.sql", """
            SELECT set_config('sess.role_authority_assignment_id',(SELECT a."Id"::text FROM advance.employee_role_assignments a
              JOIN advance.roles r ON r."Id"=a."RoleId" JOIN advance.employees e ON e."Id"=a."EmployeeId"
              WHERE r."Code"='TECHNICAL_DIRECTOR' AND e."EmployeeCode"='SESS-01' AND a."EffectiveTo" IS NULL ORDER BY a."CompanyId" LIMIT 1),false);
            INSERT INTO advance.employee_role_assignments("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","AssignmentType","ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
              SELECT gen_random_uuid(),c."Id",e."Id",r."Id",DATE '2026-09-05','FULL','SeedApproved','Unexpected authority probe',now(),'TEST',0
              FROM advance.companies c CROSS JOIN advance.employees e CROSS JOIN advance.roles r
              WHERE c."Code"='SESS_PVT_LTD' AND e."EmployeeCode"='SESS-15' AND r."Code"='HR_MANAGER';
            """);
        server.AssertRejected("roster-extra-refused.sql", migrator.GenerateScript(R1RosterBefore,R1RosterAfter), "refuses an unexpected extra assignment");
        server.Execute("roster-extra-atomic.sql", """
            DO $check$ BEGIN
              IF EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId"='20261003090000_R1RosterSeedReconciliation')
                OR EXISTS(SELECT 1 FROM advance.employee_role_assignments WHERE "CreatedBy"='R1RosterSeedReconciliation' OR "UpdatedBy"='R1RosterSeedReconciliation') THEN
                RAISE EXCEPTION 'Rejected R1 roster partially applied.';
              END IF;
            END $check$;
            """);
    }

    [Fact]
    public async Task R1RosterRollbackRefusesNewAuthorityRecordedInImmutableAudit()
    {
        using var model = R1RosterModel();
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("roster-used-up.sql", migrator.GenerateScript("0", R1RosterAfter));
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options);
        var assignment = await db.EmployeeRoleAssignments.FirstAsync(x => x.CreatedBy == "R1RosterSeedReconciliation");
        db.AuditLogs.Add(new AuditLog { CompanyId=assignment.CompanyId,Scope="COMPANY",Module="Employees",Action="Readback",
            EntityName="EmployeeRoleAssignment",EntityId=assignment.Id.ToString(),UserLoginId="R1_ROSTER_TEST",ActorRoleCode="HR_EXECUTIVE",
            ResolvedRoleAssignmentId=assignment.Id,ResolvedRoleAssignmentType="FULL",CorrelationId="R1_ROSTER_USE_PROBE",CreatedBy="R1_ROSTER_TEST" });
        await db.SaveChangesAsync();
        server.AssertRejected("roster-used-down-refused.sql", migrator.GenerateScript(R1RosterAfter,R1RosterBefore), "refuses used new authority");
        await AssertR1RosterManifest(server.ConnectionString);
        Assert.Equal(1, await db.AuditLogs.CountAsync(x => x.CorrelationId == "R1_ROSTER_USE_PROBE"));
    }

    [Fact]
    public async Task R1RosterLateFreshCfoBaselineAlignsToCutoffAndRoundTrips()
    {
        using var model = R1RosterModel();
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("roster-late-fresh-before.sql", migrator.GenerateScript("0",R1RosterBefore));
        // Model the untouched merged CFO seed produced on a later installation day.
        server.Execute("roster-late-fresh-fixture.sql", """
            DO $fixture$ DECLARE company uuid; authority uuid; future date:=greatest(CURRENT_DATE+1,DATE '2026-10-16'); BEGIN
              FOR company IN SELECT "Id" FROM advance.companies WHERE "Code" IN('SESS_PVT_LTD','SESS_PROPRIETORSHIP') LOOP
                SELECT a."Id" INTO STRICT authority FROM advance.employee_role_assignments a
                  JOIN advance.roles r ON r."Id"=a."RoleId" JOIN advance.employees e ON e."Id"=a."EmployeeId"
                  WHERE a."CompanyId"=company AND r."Code"='TECHNICAL_DIRECTOR' AND e."EmployeeCode"='SESS-01' AND a."EffectiveTo" IS NULL;
                PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
                UPDATE advance.employee_role_assignments a SET "EffectiveFrom"=future,"CreatedAt"=future::timestamp AT TIME ZONE 'UTC'
                  FROM advance.roles r WHERE r."Id"=a."RoleId" AND r."Code"='CHIEF_FINANCIAL_OFFICER' AND a."CompanyId"=company;
                UPDATE advance.company_role_activations a SET "EffectiveFrom"=future,"CreatedAt"=future::timestamp AT TIME ZONE 'UTC'
                  FROM advance.roles r WHERE r."Id"=a."RoleId" AND r."Code"='CHIEF_FINANCIAL_OFFICER' AND a."CompanyId"=company;
              END LOOP;
            END $fixture$;
            """);
        server.Execute("roster-late-fresh-up.sql",migrator.GenerateScript(R1RosterBefore,R1RosterAfter));
        await AssertR1RosterManifest(server.ConnectionString);
        server.Execute("roster-late-fresh-snapshot-check.sql", """
            DO $check$ BEGIN
              IF (SELECT count(*) FROM advance.r1_roster_reconciliation_activations)<>2
                OR (SELECT count(*) FROM advance.employee_role_assignment_events WHERE "Operation"='R1_SEED_START_ALIGN')<>2 THEN
                RAISE EXCEPTION 'Late fresh CFO start correction was not completely journaled.';
              END IF;
            END $check$;
            """);
        server.Execute("roster-late-fresh-down.sql",migrator.GenerateScript(R1RosterAfter,R1RosterBefore));
        server.Execute("roster-late-fresh-restoration.sql", """
            DO $check$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                WHERE r."Code"='CHIEF_FINANCIAL_OFFICER' AND a."EffectiveFrom"<>greatest(CURRENT_DATE+1,DATE '2026-10-16'))
                OR EXISTS(SELECT 1 FROM advance.company_role_activations a JOIN advance.roles r ON r."Id"=a."RoleId"
                  WHERE r."Code"='CHIEF_FINANCIAL_OFFICER' AND a."EffectiveFrom"<>greatest(CURRENT_DATE+1,DATE '2026-10-16'))
                OR (SELECT count(*) FROM advance.employee_role_assignment_events WHERE "Operation"='R1_SEED_START_ALIGN')<>2 THEN
                RAISE EXCEPTION 'Late fresh CFO rollback changed original dates or deleted history.';
              END IF;
            END $check$;
            """);
        server.Execute("roster-late-fresh-up-again.sql",migrator.GenerateScript(R1RosterBefore,R1RosterAfter));
        await AssertR1RosterManifest(server.ConnectionString);
    }
    [Fact]
    public Task R1RosterPurchaseFlowRetainsSupportRefusalsAfterTheCutoff() =>
        RunCompletePurchaseFlow(rosterSupportFixture: true);
    private static NexaErpDbContext R1RosterModel() => new(new DbContextOptionsBuilder<NexaErpDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);

    private static async Task AssertR1RosterManifest(string connectionString)
    {
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(connectionString).Options);
        foreach (var date in new[] {new DateOnly(2026,10,10),new DateOnly(2026,10,11),new DateOnly(2026,10,15)})
        foreach (var companyCode in new[] {"SESS_PVT_LTD","SESS_PROPRIETORSHIP"})
        {
            var company = await db.Companies.SingleAsync(x => x.Code == companyCode);
            foreach (var (code, expected) in R1RosterRoles)
            {
                var employee = await db.Employees.SingleAsync(x => x.EmployeeCode == code);
                var assignments = await db.EmployeeRoleAssignments.AsNoTracking().Include(x=>x.Role)
                    .Where(x=>x.CompanyId==company.Id && x.EmployeeId==employee.Id &&
                        (x.ApprovalStatus=="Approved" || x.ApprovalStatus=="SeedApproved") && x.EffectiveFrom<=date &&
                        (x.EffectiveTo==null || x.EffectiveTo>=date)).ToListAsync();
                Assert.Equal(expected, assignments.Select(x=>x.Role!.Code).Order());
                Assert.All(assignments,x=>Assert.Equal("FULL",x.AssignmentType));
                foreach(var assignment in assignments)
                    Assert.True(await db.CompanyRoleActivations.AnyAsync(x=>x.CompanyId==company.Id && x.RoleId==assignment.RoleId &&
                        x.IsEnabled && x.EffectiveFrom<=date && (x.EffectiveTo==null || x.EffectiveTo>=date)));
            }
        }
    }
}