using System.Net;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record SupplierInvoiceWitnessContext(HttpClient Client,DbContextOptions<NexaErpDbContext> Options,
        TaxWorkflowUser User,string Stage,Guid PurchaseOrderId,string Band,Guid AccountsId,Guid PurchaseId,
        Guid StoresId,string RuntimeConnection);
#if WORKFLOW_WITNESS
    [Fact]
    public Task SupplierInvoicesTrackRealReceiptsReversalsAndImmutableEvidence()
    {
        var recorded=new Dictionary<Guid,(SupplierInvoiceView Invoice,RecordSupplierInvoiceRequest Request)>();
        return RunCompletePurchaseFlow(supplierInvoices:async context =>
        {
            if(context.Band is not ("LOW" or "INVOICE"))return;
            var user=context.User;
            await using var permissionHost=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,user,useRealPagePermissions:true);
            context=context with { Client=permissionHost.Client };
            var client=context.Client;
            await using var db=new NexaErpDbContext(context.Options);
            var po=await db.PurchaseOrders.Include(x=>x.Lines).SingleAsync(x=>x.Id==context.PurchaseOrderId);
            var line=Assert.Single(po.Lines);
            user.Set(context.AccountsId,"SESS-14",Rev869ARoleCodes.AccountsManager);
            if(context.Stage=="ISSUED")
            {
                var poOptions=await Get<PagedResponse<SupplierInvoicePurchaseOrderOption>>(client,
                    $"/api/v1/accounts/supplier-invoices/purchase-order-options?search={Uri.EscapeDataString(po.PoNumber)}&pageSize=1");
                Assert.Equal(1,poOptions.TotalCount);
                var selectedPo=Assert.Single(poOptions.Items);
                Assert.Equal(po.Id,selectedPo.Id);
                var selectedLine=Assert.Single(selectedPo.Lines);
                Assert.Equal(line.Id,selectedLine.Id);
                var request=new RecordSupplierInvoiceRequest(selectedPo.Id,"TRIAL-BILL-"+context.Band,
                    DateOnly.FromDateTime(DateTime.UtcNow),selectedPo.CurrencyCode,
                    [new(selectedLine.Id,1,selectedLine.UnitRate,selectedLine.TotalPayableValue)],
                    new("supplier-invoice.pdf","application/pdf",SupplierInvoiceFixturePdf(context.Band)),
                    "supplier-invoice-"+context.Band);
                await AssertPostStatus(client,"/api/v1/accounts/supplier-invoices/",
                    request with { CurrencyCode="USD",IdempotencyKey=request.IdempotencyKey+"-wrong-currency" },HttpStatusCode.Conflict);
                var assistantId=await db.Employees.Where(x=>x.EmployeeCode=="SESS-41").Select(x=>x.Id).SingleAsync();
                user.Set(assistantId,"SESS-41","ACCOUNTS_ASSISTANT");
                var assistantOptions=await Get<PagedResponse<SupplierInvoicePurchaseOrderOption>>(client,
                    $"/api/v1/accounts/supplier-invoices/purchase-order-options?search={Uri.EscapeDataString(po.PoNumber)}");
                Assert.Equal(selectedPo.Id,Assert.Single(assistantOptions.Items).Id);
                var incorrect=await Post<SupplierInvoiceView>(client,"/api/v1/accounts/supplier-invoices/",
                    request with { InvoiceNumber=request.InvoiceNumber+"-ENTRY-ERROR",IdempotencyKey=request.IdempotencyKey+"-incorrect" });
                incorrect=await Get<SupplierInvoiceView>(client,$"/api/v1/accounts/supplier-invoices/{incorrect.Id}");
                await AssertPostStatus(client,$"/api/v1/accounts/supplier-invoices/{incorrect.Id}/cancel",
                    new CancelSupplierInvoiceRequest(incorrect.Version,"Support cannot cancel evidence",request.IdempotencyKey+"-support-cancel"),HttpStatusCode.Forbidden);
                user.Set(context.AccountsId,"SESS-14",Rev869ARoleCodes.AccountsManager);
                incorrect=await Get<SupplierInvoiceView>(client,$"/api/v1/accounts/supplier-invoices/{incorrect.Id}");
                var cancelled=await Post<SupplierInvoiceView>(client,$"/api/v1/accounts/supplier-invoices/{incorrect.Id}/cancel",
                    new CancelSupplierInvoiceRequest(incorrect.Version,"Correct documentary entry; retain original evidence.",request.IdempotencyKey+"-cancel"));
                Assert.Equal("CANCELLED",cancelled.Status);
                var invoice=await Post<SupplierInvoiceView>(client,"/api/v1/accounts/supplier-invoices/",request);
                recorded.Add(po.Id,(invoice,request));
                Assert.Equal(0,Assert.Single(invoice.Lines).ReceivedQuantity);
                Assert.Equal(1,Assert.Single(invoice.Lines).OutstandingQuantity);
                await AssertPostStatus(client,"/api/v1/accounts/supplier-invoices/",
                    request with { IdempotencyKey=request.IdempotencyKey+"-duplicate" },HttpStatusCode.Conflict);
                await AssertPostStatus(client,"/api/v1/accounts/supplier-invoices/",
                    request with { InvoiceNumber="DIFFERENT" },HttpStatusCode.Conflict);
                using var download=await client.GetAsync($"/api/v1/accounts/supplier-invoices/{invoice.Id}/evidence");
                Assert.Equal(HttpStatusCode.OK,download.StatusCode);
                Assert.Equal(request.Evidence.Content,await download.Content.ReadAsByteArrayAsync());
                var page=await SupplierInvoiceReport(context);
                Assert.Equal(1m,Assert.Single(page.Totals).GetProperty("quantity").GetDecimal());
                Assert.Equal(line.TotalPayableValue,Assert.Single(page.Totals).GetProperty("invoiceValue").GetDecimal());
                user.Set(context.PurchaseId,"SESS-15",Rev869ARoleCodes.PurchaseManager,
                    Rev869ARoleCodes.PurchaseExecutive,Rev869ARoleCodes.PurchaseManager,Rev869ARoleCodes.StoresExecutive);
                return;
            }
            var retained=recorded[po.Id];
            var current=await Get<SupplierInvoiceView>(client,$"/api/v1/accounts/supplier-invoices/{retained.Invoice.Id}");
            if(context.Stage=="RECEIVED")
            {
                var expected=context.Band=="LOW"?1m:.4m;
                Assert.Equal(expected,Assert.Single(current.Lines).ReceivedQuantity);
                Assert.Equal(expected,Assert.Single(current.ReceiptMatches).Quantity);
                var page=await SupplierInvoiceReport(context);
                if(context.Band=="LOW")Assert.Empty(page.Rows);
                else Assert.Equal(.6m,Assert.Single(page.Totals).GetProperty("quantity").GetDecimal());
                user.Set(context.StoresId,"SESS-35",Rev869ARoleCodes.StoresExecutive,Rev869ARoleCodes.StoresExecutive);
                return;
            }
            Assert.Equal("FINAL",context.Stage);
            var partial=await SupplierInvoiceReport(context);
            var total=Assert.Single(partial.Totals);
            Assert.Equal(.6m,total.GetProperty("quantity").GetDecimal());
            Assert.Equal(decimal.Round(line.TotalPayableValue*.6m,6),total.GetProperty("invoiceValue").GetDecimal());
            var details=await SupplierInvoiceReport(context,new(Mode:"details",Group:total.GetProperty("group").GetRawText(),Metric:"invoiceValue"));
            Assert.Equal(total.GetProperty("invoiceValue").GetDecimal(),Assert.Single(details.Rows).GetProperty("invoiceValue").GetDecimal());
            var service=await SupplierInvoiceReportService(context);
            await using var runtime=service.Database;
            var file=await ObserveSingleReportCommand(()=>service.Service.ExportAsync("billed-not-received",new(),default));
            using(var book=new XLWorkbook(new MemoryStream(file.Content)))
            {
                Assert.Equal(.6m,book.Worksheet("Totals").Cell(2,3).GetValue<decimal>());
                Assert.Equal(total.GetProperty("invoiceValue").GetDecimal(),book.Worksheet("Totals").Cell(2,4).GetValue<decimal>());
                Assert.True(book.Worksheet("Totals").Cell(2,4).HasHyperlink);
            }
            var firstGrn=await db.GoodsReceipts.AsNoTracking().SingleAsync(x=>x.PurchaseOrderId==po.Id&&x.DocumentKind=="NORMAL");
            // The existing one-effective-GRN-per-bill guard refuses a split bill.
            // Refusal must remain atomic and readable to the caller.
            var movementsBeforeRefusal=await db.StockMovements.CountAsync();
            var layersBeforeRefusal=await db.FifoInventoryCostLayers.CountAsync();
            var second=await SupplierInvoiceReceive(context,.6m,"second",refuseFinalization:true);
            Assert.Equal("DRAFT",second.Status);
            Assert.Equal(movementsBeforeRefusal,await db.StockMovements.CountAsync());
            Assert.Equal(layersBeforeRefusal,await db.FifoInventoryCostLayers.CountAsync());
            user.Set(context.AccountsId,"SESS-14",Rev869ARoleCodes.AccountsManager);
            current=await Get<SupplierInvoiceView>(client,$"/api/v1/accounts/supplier-invoices/{retained.Invoice.Id}");
            Assert.Equal(.4m,Assert.Single(current.Lines).ReceivedQuantity);
            Assert.Single(current.ReceiptMatches);
            Assert.Equal(.6m,Assert.Single((await SupplierInvoiceReport(context)).Totals).GetProperty("quantity").GetDecimal());
            user.Set(context.StoresId,"SESS-35",Rev869ARoleCodes.StoresExecutive,Rev869ARoleCodes.StoresExecutive);
            var reversed=await Post<GoodsReceiptResult>(client,$"/api/v1/stores/goods-receipts/{firstGrn.Id}/reverse",
                new ReverseGoodsReceiptRequest(firstGrn.Version,"Witness receipt correction; retain original GRN.","supplier-invoice-grn-reverse"));
            Assert.Equal("REVERSAL",reversed.DocumentKind);
            user.Set(context.AccountsId,"SESS-14",Rev869ARoleCodes.AccountsManager);
            current=await Get<SupplierInvoiceView>(client,$"/api/v1/accounts/supplier-invoices/{retained.Invoice.Id}");
            Assert.Equal(0m,Assert.Single(current.Lines).ReceivedQuantity);
            Assert.Equal(-.4m,Assert.Single(current.ReceiptMatches,x=>x.ReversesMatchId.HasValue).Quantity);
            Assert.Equal(1m,Assert.Single((await SupplierInvoiceReport(context)).Totals).GetProperty("quantity").GetDecimal());
            var replacement=await SupplierInvoiceReceive(context,1m,"replacement");
            user.Set(context.AccountsId,"SESS-14",Rev869ARoleCodes.AccountsManager);
            current=await Get<SupplierInvoiceView>(client,$"/api/v1/accounts/supplier-invoices/{retained.Invoice.Id}");
            Assert.Equal(1,Assert.Single(current.Lines).ReceivedQuantity);
            Assert.Equal(3,current.ReceiptMatches.Count);
            Assert.Empty((await SupplierInvoiceReport(context)).Rows);
            var replay=await Post<SupplierInvoiceView>(client,"/api/v1/accounts/supplier-invoices/",retained.Request);
            Assert.True(replay.Replayed);Assert.Equal(0,Assert.Single(replay.Lines).ReceivedQuantity);
            var low=recorded.Single(x=>x.Key!=po.Id).Value;
            var billOptions=await Get<VendorBillPage>(client,
                $"/api/v1/accounts/vendor-bills/?billNumber={Uri.EscapeDataString(low.Invoice.InvoiceNumber)}&status=ACCEPTED&vendorId={low.Invoice.VendorId}");
            var lowBill=Assert.Single(billOptions.Items);
            Assert.Equal(low.Request.PurchaseOrderId,lowBill.PurchaseOrderId);
            var lowCurrent=await Get<SupplierInvoiceView>(client,$"/api/v1/accounts/supplier-invoices/{low.Invoice.Id}");
            var linked=await Post<SupplierInvoiceView>(client,$"/api/v1/accounts/supplier-invoices/{low.Invoice.Id}/link-accepted-bill",
                new LinkSupplierInvoiceAcceptedBillRequest(lowCurrent.Version,lowBill.Id,"supplier-invoice-link-accepted"));
            Assert.Equal(lowBill.Id,Assert.Single(linked.AcceptedBillIds));
            await AssertPostStatus(client,$"/api/v1/accounts/supplier-invoices/{low.Invoice.Id}/cancel",
                new CancelSupplierInvoiceRequest(linked.Version,"Attempt to hide accepted evidence","supplier-invoice-refused-cancel"),HttpStatusCode.Conflict);
            user.Set(context.StoresId,"SESS-35",Rev869ARoleCodes.StoresExecutive,Rev869ARoleCodes.StoresExecutive);
            using(var forbidden=await client.GetAsync($"/api/v1/accounts/supplier-invoices/{low.Invoice.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden,forbidden.StatusCode);
            using(var forbidden=await client.GetAsync(WitnessReportPath("/api/v1/reports/billed-not-received")))
                Assert.Equal(HttpStatusCode.Forbidden,forbidden.StatusCode);
            var direct=await Assert.ThrowsAsync<Npgsql.PostgresException>(()=>runtime.Database.ExecuteSqlRawAsync("SELECT * FROM advance.supplier_invoices"));
            Assert.Equal("42501",direct.SqlState);
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item15");Directory.CreateDirectory(evidence);
            await File.WriteAllBytesAsync(Path.Combine(evidence,"billed-not-received.xlsx"),file.Content);
            await File.WriteAllTextAsync(Path.Combine(evidence,"billed-not-received.json"),
                JsonSerializer.Serialize(new{BeforeReceipt=retained.Invoice,Partial=partial,Details=details,Final=current,Linked=linked,
                    OriginalReceipt=firstGrn.Id,RefusedSecondReceiptDraft=second.Id,Reversal=reversed.Id,Replacement=replacement.Id},
                    new JsonSerializerOptions{WriteIndented=true}));
            await runtime.DisposeAsync();
            await WitnessReversedReceiptFifoEligibility(context,replacement.Id);
        });
    }
#endif
    private static async Task<(NexaErpDbContext Database,EfCompanyReportService Service)> SupplierInvoiceReportService(SupplierInvoiceWitnessContext context)
    {
        await using var db=new NexaErpDbContext(context.Options);
        var company=await db.Companies.SingleAsync(x=>x.Code=="SESS_PVT_LTD");
        var assignments=await db.EmployeeRoleAssignments.Include(x=>x.Role)
            .Where(x=>x.EmployeeId==context.AccountsId&&x.CompanyId==company.Id&&x.EffectiveTo==null).ToListAsync();
        var user=new ReportWitnessUser(context.AccountsId,
            assignments.Select(x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)).ToArray());
        var runtime=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(context.RuntimeConnection).Options);
        return(runtime,new EfCompanyReportService(runtime,user));
    }
    private static async Task<CompanyReportPage> SupplierInvoiceReport(SupplierInvoiceWitnessContext context,CompanyReportRequest? request=null)
    {
        var pair=await SupplierInvoiceReportService(context);await using var db=pair.Database;
        return await ObserveSingleReportCommand(()=>pair.Service.GetAsync("billed-not-received",request??new(),default));
    }
    private static async Task<GoodsReceiptResult> SupplierInvoiceReceive(SupplierInvoiceWitnessContext context,decimal quantity,string suffix,bool refuseFinalization=false)
    {
        await using var db=new NexaErpDbContext(context.Options);
        var po=await db.PurchaseOrders.Include(x=>x.Lines).SingleAsync(x=>x.Id==context.PurchaseOrderId);
        context.User.Set(context.StoresId,"SESS-35",Rev869ARoleCodes.StoresExecutive,Rev869ARoleCodes.StoresExecutive);
        var client=context.Client;var key="supplier-invoice-"+suffix;var date=DateOnly.FromDateTime(DateTime.UtcNow);
        var gate=await Post<GateEntryResult>(client,"/api/v1/stores/gate-entries/",
            new CreateGateEntryRequest(po.PoNumber,key,"TRIAL-VEHICLE","ROAD",DateTimeOffset.UtcNow,
                "{\"packagesChecked\":true}",[new(Assert.Single(po.Lines).Id,quantity)]),key+"-gate");
        gate=await Post<GateEntryResult>(client,$"/api/v1/stores/gate-entries/{gate.Id}/finalize",
            new FinalizeGateEntryRequest(gate.Version,key+"-gate-finalize"));
        var grn=await Post<GoodsReceiptResult>(client,"/api/v1/stores/goods-receipts/",
            new CreateGoodsReceiptRequest(gate.GateEntryNumber,"TRIAL-BILL-INVOICE",date,DateTimeOffset.UtcNow,
                "{\"billChecked\":true}",[new(Assert.Single(gate.Lines).Id,[new(1,quantity,key,null,date.AddMonths(-1),date.AddYears(2))],[])]),key+"-grn");
        if(refuseFinalization)
        {
            await AssertPostStatus(client,$"/api/v1/stores/goods-receipts/{grn.Id}/finalize",
                new FinalizeGoodsReceiptRequest(grn.Version,key+"-grn-finalize"),HttpStatusCode.Conflict);
            return await Get<GoodsReceiptResult>(client,$"/api/v1/stores/goods-receipts/{grn.Id}");
        }
        return await Post<GoodsReceiptResult>(client,$"/api/v1/stores/goods-receipts/{grn.Id}/finalize",
            new FinalizeGoodsReceiptRequest(grn.Version,key+"-grn-finalize"));
    }
    private static byte[] SupplierInvoiceFixturePdf(string band)
    {
        var text=$"BT /F1 12 Tf 40 100 Td (TRIAL supplier invoice {band}: quantity 1) Tj ET";
        var objects=new[]{"<< /Type /Catalog /Pages 2 0 R >>","<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 400 200] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",$"<< /Length {Encoding.ASCII.GetByteCount(text)} >>\nstream\n{text}\nendstream"};
        var pdf=new StringBuilder("%PDF-1.4\n");var offsets=new List<int>{0};
        for(var i=0;i<objects.Length;i++){offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));pdf.Append($"{i+1} 0 obj\n{objects[i]}\nendobj\n");}
        var xref=Encoding.ASCII.GetByteCount(pdf.ToString());pdf.Append($"xref\n0 {objects.Length+1}\n0000000000 65535 f \n");
        foreach(var offset in offsets.Skip(1))pdf.Append($"{offset:0000000000} 00000 n \n");
        pdf.Append($"trailer\n<< /Size {objects.Length+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
