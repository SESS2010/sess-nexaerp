using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class FirstStoresPart3AMigrationTests
{
    private static NexaErpDbContext CreateContext() => new(
        new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);

    [Fact]
    public void Part3AContainsExactlyTheTenApprovedTables()
    {
        using var db = CreateContext();
        Type[] types = [typeof(QcInspection),typeof(QcInspectionRevision),typeof(QcInspectionParameterResult),typeof(QcInspectionSerialDisposition),typeof(JobOrder),typeof(MaterialIssueRequest),typeof(MaterialIssueRequestLine),typeof(StoresApprovalHistory),typeof(DeliveryChallan),typeof(DeliveryChallanLine)];
        var actual=types.Select(x=>db.Model.FindEntityType(x)!.GetTableName()).Order().ToArray();
        string[] expected=["delivery_challan_lines","delivery_challans","job_orders","material_issue_request_lines","material_issue_requests","qc_inspection_parameter_results","qc_inspection_revisions","qc_inspection_serial_dispositions","qc_inspections","stores_approval_history"];
        Assert.Equal(expected,actual);
    }

    [Fact]
    public void Part3AIsLatestAndPart3BIsAbsent()
    {
        using var db=CreateContext();
        Assert.Equal("20260827115729_FirstStoresPart3AQcOutboundDocuments",db.Database.GetMigrations().Last());
        Assert.DoesNotContain("stock_posting_batches",db.Model.GetEntityTypes().Select(x=>x.GetTableName()));
    }

    [Fact]
    public void QcAndOutboundQuantitiesUseSettledPrecision()
    {
        using var db=CreateContext();
        foreach(var pair in new[]{(typeof(QcInspectionRevision),nameof(QcInspectionRevision.AcceptedQuantity)),(typeof(QcInspectionRevision),nameof(QcInspectionRevision.RejectedQuantity)),(typeof(MaterialIssueRequestLine),nameof(MaterialIssueRequestLine.RequestedQuantity)),(typeof(DeliveryChallanLine),nameof(DeliveryChallanLine.Quantity))})
        {
            var property=db.Model.FindEntityType(pair.Item1)!.FindProperty(pair.Item2)!;
            Assert.Equal(24,property.GetPrecision()); Assert.Equal(6,property.GetScale());
        }
    }

    [Fact]
    public void QcIdentityHasExactlyTheTwoSettledTypedSources()
    {
        using var db=CreateContext(); var entity=db.Model.FindEntityType(typeof(QcInspection))!;
        Assert.True(entity.FindProperty(nameof(QcInspection.GoodsReceiptLineId))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(QcInspection.DeliveryChallanLineId))!.IsNullable);
        Assert.Contains(entity.GetForeignKeys(),x=>x.Properties.Any(p=>p.Name==nameof(QcInspection.GoodsReceiptLineId)));
        Assert.Contains(entity.GetForeignKeys(),x=>x.Properties.Any(p=>p.Name==nameof(QcInspection.DeliveryChallanLineId)));
    }

    [Fact]
    public void StatusHistoryCarriesAllSixTypedDocumentSources()
    {
        using var db=CreateContext(); var entity=db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(StoresDocumentStatusHistory))!;
        foreach(var name in new[]{nameof(StoresDocumentStatusHistory.GateEntryId),nameof(StoresDocumentStatusHistory.GoodsReceiptId),nameof(StoresDocumentStatusHistory.QcInspectionRevisionId),nameof(StoresDocumentStatusHistory.JobOrderId),nameof(StoresDocumentStatusHistory.MaterialIssueRequestId),nameof(StoresDocumentStatusHistory.DeliveryChallanId)}) Assert.True(entity.FindProperty(name)!.IsNullable);
    }

    [Fact]
    public void Part3ASqlFailsClosedAndKeepsInventoryInactive()
    {
        var type=typeof(NexaErpDbContext).Assembly.GetType("SESS.NexaERP.Infrastructure.Persistence.Migrations.FirstStoresPart3ASql",true)!;
        string Read(string name)=>(string)type.GetProperty(name,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
        var pre=Read("PreUp"); var up=Read("Up"); var down=Read("Down");
        Assert.Contains("stock_posting_batches",pre); Assert.Contains("QC finalisation is disabled",up); Assert.Contains("Material issue posting/finalisation is disabled",up); Assert.Contains("DC dispatch, receipt and finalisation are disabled",up);
        Assert.Contains("requires PostgreSQL 17",pre); Assert.Contains("down requires PostgreSQL 17",down); Assert.Contains("rollback refuses persisted document or history data",down);
    }
}
