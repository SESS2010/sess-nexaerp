using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string JobOrderGovernanceTarget = "20260908095057_GovernedJobOrderCreationWorkflow";

#if MIGRATION_LIFECYCLE_WITNESS
    [Fact]
    public void Governed_job_order_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>(); var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, JobOrderGovernanceTarget); Assert.True(index > 0); var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("job-order-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("job-order-up.sql", migrator.GenerateScript(predecessor, JobOrderGovernanceTarget) + JobOrderAssertions);
        server.Execute("job-order-down.sql", migrator.GenerateScript(JobOrderGovernanceTarget, predecessor));
        server.Execute("job-order-reapply.sql", migrator.GenerateScript(predecessor, JobOrderGovernanceTarget) + JobOrderAssertions);
    }
#endif

    [Fact]
    public void Job_order_accounts_return_and_resubmission_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        const string target = "20260911065425_JobOrderAccountsReturnAndResubmission";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>(); var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, target); Assert.True(index > 0); var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("job-order-recovery-pre.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("job-order-recovery-up.sql", migrator.GenerateScript(predecessor, target) + RecoveryAssertions(true));
        server.Execute("job-order-recovery-down.sql", migrator.GenerateScript(target, predecessor) + RecoveryAssertions(false));
        server.Execute("job-order-recovery-reup.sql", migrator.GenerateScript(predecessor, target) + RecoveryAssertions(true));
    }

    private static string RecoveryAssertions(bool enabled) => $"""
        DO $assert$
        DECLARE production_count integer; accounts_count integer; joint_definition text;
        BEGIN
          SELECT count(*) INTO production_count FROM advance.role_page_permissions p
          JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
          JOIN advance.roles r ON r."Id"=p."RoleId"
          WHERE d."PageKey"='production.job-orders'
            AND r."Code" IN ('PRODUCTION_COORDINATOR','PRODUCTION_MANAGER')
            AND p."CanUpdate" IS {enabled.ToString().ToUpperInvariant()}
            AND p."CanSubmit" IS {enabled.ToString().ToUpperInvariant()};
          SELECT count(*) INTO accounts_count FROM advance.role_page_permissions p
          JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
          JOIN advance.roles r ON r."Id"=p."RoleId"
          WHERE d."PageKey"='production.job-orders'
            AND r."Code" IN ('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER')
            AND p."CanReject" IS {enabled.ToString().ToUpperInvariant()};
          SELECT pg_get_constraintdef(oid) INTO joint_definition FROM pg_constraint
          WHERE conrelid='advance.job_orders'::regclass AND conname='CK_job_order_joint_governance';
          IF production_count<>2 OR accounts_count<>2
             OR (position('''DRAFT''' in joint_definition)>0) IS DISTINCT FROM {enabled.ToString().ToLowerInvariant()} THEN
            RAISE EXCEPTION 'Job Order recovery authority or lifecycle mismatch.';
          END IF;
        END $assert$;
        """;

    private const string JobOrderAssertions = """
        DO $assert$ BEGIN
          IF to_regclass('advance.job_order_history') IS NULL
             OR to_regprocedure('advance.guard_governed_job_order()') IS NULL
             OR to_regprocedure('advance.guard_job_order_history()') IS NULL THEN
            RAISE EXCEPTION 'Governed Job Order evidence or guards are missing.';
          END IF;
          IF (SELECT count(*) FROM information_schema.columns WHERE table_schema='advance' AND table_name='job_orders' AND column_name IN ('CustomerPurchaseOrderId','CustomerPurchaseOrderLineId','MachineOrdinal','InitiatedRoleAssignmentId','AccountsConfirmationRoleAssignmentId'))<>5 THEN
            RAISE EXCEPTION 'Governed Job Order columns are incomplete.';
          END IF;
          IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey"='production.job-orders')<>1
             OR (SELECT count(*) FROM advance.role_page_permissions WHERE "CreatedBy"='GovernedJobOrderCreationWorkflow')<>4 THEN
            RAISE EXCEPTION 'Governed Job Order page/grants are incomplete.';
          END IF;
        END $assert$;
        """;
}