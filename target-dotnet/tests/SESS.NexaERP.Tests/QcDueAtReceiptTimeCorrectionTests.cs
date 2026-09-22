using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string QcDueAtReceiptTimeTarget = "20260911104631_AlignGrnQcDueAtWithReceiptTime";

    [Fact]
    public void Qc_due_at_receipt_time_correction_runs_up_down_and_reapply_on_disposable_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, QcDueAtReceiptTimeTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("qc-due-at-predecessor.sql", migrator.GenerateScript("0", predecessor) + FinalizationAssertions);
        server.Execute("qc-due-at-up.sql", migrator.GenerateScript(predecessor, QcDueAtReceiptTimeTarget) + ReceiptAssertions);
        server.Execute("qc-due-at-down.sql", migrator.GenerateScript(QcDueAtReceiptTimeTarget, predecessor) + FinalizationAssertions);
        server.Execute("qc-due-at-reapply.sql", migrator.GenerateScript(predecessor, QcDueAtReceiptTimeTarget) + ReceiptAssertions);
    }

    [Fact]
    public void Application_qc_ageing_paths_all_measure_from_receipt_time()
    {
        var root = FindRepositoryRoot();
        var receipt = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Stores", "EfGoodsReceiptService.cs"));
        var queue = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Stores", "EfQcWorkflowService.cs"));
        var notifications = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Stores", "EfInAppNotificationService.cs"));

        Assert.DoesNotContain("FinalizedAt.AddDays", receipt);
        Assert.Contains("request.ReceivedAt.AddDays(qcDays)", receipt);
        Assert.Contains("receipt.ReceivedAt.AddDays(receipt.QcCompletionDaysSnapshot)", queue);
        Assert.Contains("GoodsReceipt.ReceivedAt.AddDays", notifications);
    }

    private const string ReceiptAssertions = """
        DO $assert$ DECLARE guard_definition text; finalizer_definition text; BEGIN
          SELECT pg_get_functiondef('advance.stores_p2_goods_receipt_guard()'::regprocedure) INTO guard_definition;
          SELECT pg_get_functiondef('advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)'::regprocedure) INTO finalizer_definition;
          IF position('NEW."QcDueAt"<>NEW."ReceivedAt"+make_interval(days=>NEW."QcCompletionDaysSnapshot")' in guard_definition)=0
             OR position('"QcDueAt"=receipt."ReceivedAt"+make_interval(days=>"QcCompletionDaysSnapshot")' in finalizer_definition)=0
             OR position('"QcDueAt"=finalized_at+make_interval(days=>"QcCompletionDaysSnapshot")' in finalizer_definition)>0 THEN
            RAISE EXCEPTION 'QC deadline controls do not use receipt time.';
          END IF;
        END $assert$;
        """;

    private const string FinalizationAssertions = """
        DO $assert$ DECLARE guard_definition text; finalizer_definition text; BEGIN
          SELECT pg_get_functiondef('advance.stores_p2_goods_receipt_guard()'::regprocedure) INTO guard_definition;
          SELECT pg_get_functiondef('advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)'::regprocedure) INTO finalizer_definition;
          IF position('NEW."QcDueAt"<>NEW."FinalizedAt"+make_interval(days=>NEW."QcCompletionDaysSnapshot")' in guard_definition)=0
             OR position('"QcDueAt"=finalized_at+make_interval(days=>"QcCompletionDaysSnapshot")' in finalizer_definition)=0 THEN
            RAISE EXCEPTION 'Predecessor QC deadline controls do not use finalisation time.';
          END IF;
        END $assert$;
        """;
}
