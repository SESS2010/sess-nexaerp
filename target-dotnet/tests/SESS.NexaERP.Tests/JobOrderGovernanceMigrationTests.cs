using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string JobOrderGovernanceTarget = "20260908095057_GovernedJobOrderCreationWorkflow";

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