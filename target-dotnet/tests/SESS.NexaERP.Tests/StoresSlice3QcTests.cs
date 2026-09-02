namespace SESS.NexaERP.Tests;

public sealed class StoresSlice3QcTests
{
    private static readonly string Root=FindRoot();

    [Fact]
    public void QcIsPerLotAllocationAndShortfallIsDiscrepancyPending()
    {
        var domain=Read("src","SESS.NexaERP.Domain","Stores","StoresPart3A.cs");
        Assert.Contains("GoodsReceiptLineLotAllocationId",domain);
        Assert.Contains("DiscrepancyPendingQuantity",domain);
        Assert.DoesNotContain("InspectionShortfallRejectedQuantity",domain);
    }

    [Fact]
    public void QcAndConcessionUseControlledPostingWithoutDirectLedgerAdds()
    {
        var service=Read("src","SESS.NexaERP.Infrastructure","Stores","EfQcWorkflowService.cs");
        Assert.Contains("advance.post_stores_stock_batch",service);
        Assert.DoesNotContain("StockMovements.Add",service);
        Assert.DoesNotContain("StockPostingBatches.Add",service);
        Assert.Contains("QC_MANAGER",service);Assert.Contains("TECHNICAL_DIRECTOR",service);
    }

    [Fact]
    public void ConcessionHasAcceptRejectOnlyAndCarriesProvenanceAnnotation()
    {
        var service=Read("src","SESS.NexaERP.Infrastructure","Stores","EfQcWorkflowService.cs");
        var mapping=Read("src","SESS.NexaERP.Infrastructure","Persistence","NexaErpDbContext.InventoryProvenanceGenealogy.cs");
        Assert.Contains("CONCESSION_ACCEPTANCE",service);Assert.Contains("InventoryProvenanceAnnotations.Add",service);
        Assert.Contains("('DRAFT','APPROVED','REJECTED','REVERSED')",mapping);Assert.DoesNotContain("CONCESSION_HOLD",service+mapping);
    }

    [Fact]
    public void MigrationAndApiExposeGuardedSlice3Contract()
    {
        var migration=Read("src","SESS.NexaERP.Infrastructure","Persistence","Migrations","StoresSlice3QcConcessionSql.cs");
        var endpoints=Read("src","SESS.NexaERP.Api","Endpoints","QcEndpoints.cs");
        Assert.Contains("Stores Slice 3 requires PostgreSQL 17 or later",migration);Assert.Contains("Stores Slice 3 rollback requires PostgreSQL 17 or later",migration);Assert.Contains("current_database() IN ('postgres','template0','template1')",migration);
        Assert.Contains("/api/v1/qc",endpoints);Assert.Contains("/queue",endpoints);Assert.Contains("/corrections",endpoints);Assert.Contains("/concessions",endpoints);
    }

    private static string Read(params string[] parts)=>File.ReadAllText(Path.Combine([Root,..parts]));
    private static string FindRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"SESS.NexaERP.slnx")))d=d.Parent;return d?.FullName??throw new DirectoryNotFoundException("Repository root not found.");}
}
