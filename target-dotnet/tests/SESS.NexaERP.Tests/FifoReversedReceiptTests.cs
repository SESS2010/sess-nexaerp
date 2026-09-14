using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task WitnessReversedReceiptFifoEligibility(SupplierInvoiceWitnessContext context,Guid replacementReceiptId)
    {
        await using var parent=new NexaErpDbContext(context.Options);
        var source=new NpgsqlConnectionStringBuilder(parent.Database.GetConnectionString());
        Assert.Equal("127.0.0.1",source.Host);Assert.Equal("advance_parser",source.Database);
        Assert.Equal("postgres",source.Username);Assert.NotEqual(5432,source.Port);Assert.False(source.Pooling);
        var before=await parent.FifoCostConsumptions.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Quantity,x.ConsumedValue}).ToArrayAsync();
        var name="invoice_fifo_"+Guid.NewGuid().ToString("N");
        await using(var admin=new NpgsqlConnection(new NpgsqlConnectionStringBuilder(source.ConnectionString){Database="postgres"}.ConnectionString))
        {
            await admin.OpenAsync();
            await using(var connections=new NpgsqlCommand("SELECT count(*) FROM pg_stat_activity WHERE datname='advance_parser'",admin))
                Assert.Equal(0L,await connections.ExecuteScalarAsync());
            await using(var create=new NpgsqlCommand($"CREATE DATABASE \"{name}\" WITH TEMPLATE advance_parser OWNER nexa_erp_owner",admin))
            {create.CommandTimeout=60;await create.ExecuteNonQueryAsync();}
            await using var grants=new NpgsqlCommand($"REVOKE CONNECT,TEMPORARY ON DATABASE \"{name}\" FROM PUBLIC; GRANT CONNECT ON DATABASE \"{name}\" TO nexa_erp_runtime;",admin);
            await grants.ExecuteNonQueryAsync();
        }
        var options=new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(new NpgsqlConnectionStringBuilder(source.ConnectionString){Database=name}.ConnectionString).Options;
        var runtime=new NpgsqlConnectionStringBuilder(context.RuntimeConnection){Database=name,Pooling=false}.ConnectionString;
        await using var db=new NexaErpDbContext(options);
        var company=await db.Companies.SingleAsync(x=>x.Code=="SESS_PVT_LTD");
        var issue=await db.MaterialIssueLines.AsNoTracking().Where(x=>x.CompanyId==company.Id&&x.InventorySerialId!=null&&x.QuantityBase==1).FirstAsync();
        var assignment=await db.EmployeeRoleAssignments.Include(x=>x.Role).SingleAsync(x=>x.EmployeeId==context.StoresId&&x.CompanyId==company.Id&&x.Role!.Code=="STORES_EXECUTIVE"&&x.EffectiveTo==null);
        var subject=await db.EmployeeIdentityMappings.Where(x=>x.CompanyId==company.Id&&x.EmployeeId==context.StoresId&&x.IsActive).Select(x=>x.Subject).SingleAsync();
        var eligible=await db.Database.SqlQueryRaw<decimal>("""
            SELECT sum(f."QuantityReceived"-coalesce((SELECT sum(c."Quantity"-coalesce((SELECT sum(r."Quantity")
             FROM advance.fifo_cost_restorations r WHERE r."CompanyId"=f."CompanyId" AND r."FifoCostConsumptionId"=c."Id"),0))
             FROM advance.fifo_cost_consumptions c WHERE c."CompanyId"=f."CompanyId" AND c."FifoInventoryCostLayerId"=f."Id"),0)) AS "Value"
            FROM advance.fifo_inventory_cost_layers f
            LEFT JOIN advance.goods_receipt_lines gl ON gl."CompanyId"=f."CompanyId" AND gl."Id"=f."GoodsReceiptLineId"
            LEFT JOIN advance.goods_receipts g ON g."CompanyId"=f."CompanyId" AND g."Id"=gl."GoodsReceiptId"
            WHERE f."CompanyId"=@company AND f."ItemId"=@item
             AND EXISTS(SELECT 1 FROM advance.stock_movements m WHERE m."CompanyId"=f."CompanyId" AND m."OwnershipAccountId"=@ownership
              AND m."MovementLeg"='RECEIPT_IN' AND (m."GoodsReceiptLineId"=f."GoodsReceiptLineId" OR m."OpeningStockLineId"=f."OpeningStockLineId"))
             AND (f."OpeningStockLineId" IS NOT NULL OR (g."DocumentKind"='NORMAL' AND g."Status"='FINALIZED'
              AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts r WHERE r."CompanyId"=f."CompanyId"
               AND r."ReversesGoodsReceiptId"=g."Id" AND r."Status"='FINALIZED')))
            """,new NpgsqlParameter("company",company.Id),new NpgsqlParameter("item",issue.ItemId),
                new NpgsqlParameter("ownership",issue.OwnershipAccountId)).SingleAsync();
        Assert.True(eligible>0);
        async Task<FifoInput> Input(decimal quantity,string suffix)
        {
            var key="invoice-fifo-"+suffix+"-"+Guid.NewGuid().ToString("N");
            var input=new FifoInput(Guid.NewGuid(),Guid.NewGuid(),context.StoresId,subject,"STORES_EXECUTIVE",assignment.Id,key,
                SHA256.HashData(Encoding.UTF8.GetBytes(key)));
            await CreateDirectFifoInput(runtime,input,issue.MaterialIssueId,issue.Id,quantity);
            return input;
        }
        var originalCount=await db.FifoCostConsumptions.CountAsync();
        var movements=await db.StockMovements.CountAsync();
        var excess=await Input(eligible+.1m,"reversed-layer-must-not-fill");
        var refused=await CallDirectFifo(runtime,company.Id,excess);
        Assert.False(refused.Committed);Assert.Equal("P0001",refused.SqlState);
        Assert.Contains("Insufficient FIFO",refused.Message);
        Assert.Equal(originalCount,await db.FifoCostConsumptions.CountAsync());
        var drain=await Input(eligible,"eligible-only");
        var allowed=await CallDirectFifo(runtime,company.Id,drain);
        Assert.True(allowed.Committed);Assert.True(allowed.Affected>0);
        var receipt=await db.GoodsReceipts.AsNoTracking().SingleAsync(x=>x.Id==replacementReceiptId);
        await using(var connection=new NpgsqlConnection(runtime))
        {
            await connection.OpenAsync();
            await using var command=new NpgsqlCommand("""
                SELECT * FROM advance.reverse_goods_receipt(@company,@receipt,@version,@number,@reason,@key,@hash,@correlation,@actor,@role,@login)
                """,connection);
            var key="fifo-refused-reversal-"+Guid.NewGuid().ToString("N");
            command.Parameters.AddWithValue("company",company.Id);command.Parameters.AddWithValue("receipt",receipt.Id);
            command.Parameters.AddWithValue("version",(long)receipt.Version);command.Parameters.AddWithValue("number","FIFO-REFUSED-"+Guid.NewGuid().ToString("N"));
            command.Parameters.AddWithValue("reason","Boundary witness: consumed receipt must remain.");command.Parameters.AddWithValue("key",key);
            command.Parameters.AddWithValue("hash",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant());
            command.Parameters.AddWithValue("correlation",key);command.Parameters.AddWithValue("actor",context.StoresId);
            command.Parameters.AddWithValue("role","STORES_EXECUTIVE");command.Parameters.AddWithValue("login",subject);
            var failure=await Assert.ThrowsAsync<PostgresException>(()=>command.ExecuteNonQueryAsync());
            Assert.Contains("outstanding issue consumption",failure.MessageText);
        }
        Assert.Equal(movements,await db.StockMovements.CountAsync());
        Assert.False(await db.GoodsReceipts.AnyAsync(x=>x.ReversesGoodsReceiptId==replacementReceiptId));
        Assert.Equal(before,await parent.FifoCostConsumptions.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Quantity,x.ConsumedValue}).ToArrayAsync());
        var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item15","fifo-reversed-receipt.json");
        await File.WriteAllTextAsync(evidence,System.Text.Json.JsonSerializer.Serialize(new{
            Fixture="Restricted costing-function inputs in an isolated clone, not physical API issues; parent unchanged.",
            Database=name,EligibleQuantity=eligible,Refused=refused,Allowed=allowed,ConsumedReceiptReversalRefused=true},
            new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
    }
}
