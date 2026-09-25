using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Domain.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
namespace SESS.NexaERP.Tests;
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
 private sealed record MachineDeliveryWitnessContext(DbContextOptions<NexaErpDbContext> Options,string RuntimeConnection,TaxWorkflowUser User,Guid StoresId,Guid DirectorId,Guid AccountsId,Guid ProductionId);
#if WORKFLOW_WITNESS
 [Theory]
 [InlineData("RETURNABLE")]
 [InlineData("NON_RETURNABLE")]
 public Task SignedMachineDeliveryProducesDossierWithoutStockConsumption(string nature) => RunCompletePurchaseFlow(machineDelivery: async context =>
 {
  await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,context.User,useRealPagePermissions:true);
  var client=host.Client; var user=context.User;
  await using var db=new NexaErpDbContext(context.Options);
  var job=await db.JobOrders.SingleAsync(j=>j.MachineSerial=="WITNESS-MACHINE-001-CORRECTED");
  Assert.Equal("READY",job.FatReadinessStatus);
  var movements=await db.StockMovements.CountAsync(); var consumptions=await db.FifoCostConsumptions.CountAsync();
  var boms=await db.ActualBomEntries.CountAsync();
  var selection=Uri.EscapeDataString(JsonSerializer.Serialize(new {machineSerial=job.MachineSerial}));
  var path=$"/api/v1/reports/machine-dossier?selection={selection}&mode=details&pageSize=1000";
  user.Set(context.DirectorId,"SESS-01",Rev869ARoleCodes.TechnicalDirector);
  user.Set(context.AccountsId,"SESS-14",Rev869ARoleCodes.AccountsManager);
  var prior=await Get<CompanyReportPage>(client,path); Assert.Empty(prior.Rows);
  user.Set(context.ProductionId,"SESS-25","PRODUCTION_MANAGER");
  var actual=await Get<ActualBomView>(client,$"/api/v1/production/component-fitments/job-orders/{job.Id}/actual-bom");
  user.Set(context.StoresId,"SESS-35",Rev869ARoleCodes.StoresExecutive);
  var candidates=await Get<PagedResponse<MachineDeliveryJobOrderCandidate>>(client,"/api/v1/stores/machine-deliveries/job-orders?search="+Uri.EscapeDataString(job.MachineSerial));
  var selected=Assert.Single(candidates.Items);
  var dispatch=new DispatchMachineRequest(selected.JobOrderId,"WITNESS-MACHINE-DC-001",nature,nature=="RETURNABLE" ? "DEMO" : "CUSTOMER_PO_BASED",DateOnly.FromDateTime(DateTime.Now),nature=="RETURNABLE" ? DateOnly.FromDateTime(DateTime.Now).AddDays(7) : null,"Witness customer gate","machine-dispatch-001");
  user.Set(context.AccountsId,"SESS-14",Rev869ARoleCodes.AccountsManager);
  using(var denied=await client.PostAsJsonAsync("/api/v1/stores/machine-deliveries/",dispatch)) Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
  user.Set(context.StoresId,"SESS-35",Rev869ARoleCodes.StoresExecutive);
  // #34: omitting DcNumber and Destination reached their NOT NULL columns as a 500. #35: a field rule
  // answered 409. Both are now a 400 naming each field. Nothing is written: the dispatch below would
  // otherwise meet this job's one-DC rule.
  using(var omitted=await client.PostAsync("/api/v1/stores/machine-deliveries/",new StringContent(JsonSerializer.Serialize(new {dispatch.JobOrderId,dispatch.Nature,dispatch.Purpose,
    dispatch.DispatchDate,dispatch.ExpectedReturnDate,IdempotencyKey="machine-dispatch-omitted"}),Encoding.UTF8,"application/json")))
   await MachineDcFieldRefusal(omitted,"DcNumber","Destination");
  using(var wrongPurpose=await client.PostAsJsonAsync("/api/v1/stores/machine-deliveries/",dispatch with {Purpose=nature=="RETURNABLE" ? "CUSTOMER_PO_BASED" : "DEMO",IdempotencyKey="machine-dispatch-wrong-purpose"}))
   await MachineDcFieldRefusal(wrongPurpose,"Purpose");
  var dc=await Post<JsonElement>(client,"/api/v1/stores/machine-deliveries/",dispatch);
  var id=dc.GetProperty("Id").GetGuid(); Assert.Equal("DISPATCHED",dc.GetProperty("MachineState").GetString());
  var replay=await Post<JsonElement>(client,"/api/v1/stores/machine-deliveries/",dispatch); Assert.Equal(id,replay.GetProperty("Id").GetGuid());
  // A second DC for the same job is a state conflict and stays 409, now in words instead of constraint text.
  using(var second=await client.PostAsJsonAsync("/api/v1/stores/machine-deliveries/",dispatch with {DcNumber="WITNESS-MACHINE-DC-002",IdempotencyKey="machine-dispatch-second"}))
   await MachineDcConflict(second,"This job already has a machine DC.");
  using(var noSignatory=await client.PostAsync($"/api/v1/stores/machine-deliveries/{id}/signature",new StringContent(JsonSerializer.Serialize(new {DeliveredAt=DateTimeOffset.UtcNow,
    Evidence=new SupplierInvoiceEvidenceInput("signed.pdf","application/pdf",Encoding.ASCII.GetBytes("%PDF-1.7")),IdempotencyKey="machine-sign-omitted"}),Encoding.UTF8,"application/json")))
   await MachineDcFieldRefusal(noSignatory,"CustomerSignatory");
  using(var bad=await client.PostAsJsonAsync($"/api/v1/stores/machine-deliveries/{id}/signature",
    new SignMachineDeliveryRequest(DateTimeOffset.UtcNow,"Witness customer",new("signature.pdf","application/pdf",[]),"unsigned-refusal"))) Assert.False(bad.IsSuccessStatusCode);
  using(var badName=await client.PostAsJsonAsync($"/api/v1/stores/machine-deliveries/{id}/signature",
    new SignMachineDeliveryRequest(DateTimeOffset.UtcNow,"Witness customer",new(null!,"application/pdf",Encoding.ASCII.GetBytes("%PDF-1.7")),"null-signature-name"))) Assert.Equal(HttpStatusCode.BadRequest,badName.StatusCode);
  if(nature=="NON_RETURNABLE") {
   var notification=await db.NotificationEvents.Include(n=>n.Recipients).SingleAsync(n=>n.SourceEntityId==id && n.EventType=="MACHINE_NON_RETURNABLE_DISPATCH");
   Assert.NotEmpty(notification.Recipients);
   Assert.Contains("MANAGING_DIRECTOR",notification.RecipientRoleCodes);
  }
  var signed=new SignMachineDeliveryRequest(DateTimeOffset.UtcNow,"Witness customer representative",new("signed-machine-dc.pdf","application/pdf",Encoding.ASCII.GetBytes("%PDF-1.7\nWitness customer signed MACHINE DC\n%%EOF")),"machine-sign-001");
  var delivered=await Post<JsonElement>(client,$"/api/v1/stores/machine-deliveries/{id}/signature",signed);
  Assert.Equal("DELIVERED",delivered.GetProperty("MachineState").GetString()); Assert.Equal(nature=="RETURNABLE" ? "OUTSTANDING" : "CLOSED",delivered.GetProperty("DcState").GetString());
  var signReplay=await Post<JsonElement>(client,$"/api/v1/stores/machine-deliveries/{id}/signature",signed); Assert.Equal(delivered.GetRawText(),signReplay.GetRawText());
  using(var resign=await client.PostAsJsonAsync($"/api/v1/stores/machine-deliveries/{id}/signature",signed with {IdempotencyKey="machine-sign-second"}))
   await MachineDcConflict(resign,"This machine DC is already signed.");
  Assert.Equal(movements,await db.StockMovements.CountAsync()); Assert.Equal(consumptions,await db.FifoCostConsumptions.CountAsync()); Assert.Equal(boms,await db.ActualBomEntries.CountAsync());
  user.Set(context.DirectorId,"SESS-01",Rev869ARoleCodes.TechnicalDirector);
  user.Set(context.AccountsId,"SESS-14",Rev869ARoleCodes.AccountsManager);
  using(var signatureFile=await client.GetAsync($"/api/v1/stores/machine-deliveries/{id}/signature-evidence")) {
   Assert.True(signatureFile.IsSuccessStatusCode); Assert.Equal(signed.Evidence.Content,await signatureFile.Content.ReadAsByteArrayAsync());
  }
  var report=await Get<CompanyReportPage>(client,path); Assert.NotEmpty(report.Rows);
  Assert.Equal(actual.TotalAcceptedMaterialValue,report.Rows.Sum(r=>r.GetProperty("materialValue").GetDecimal()));
  Assert.Equal(actual.TotalAllocatedChargeValue,report.Rows.Sum(r=>r.GetProperty("allocatedCharges").GetDecimal()));
  Assert.Equal(actual.TotalAcceptedValue,report.Rows.Sum(r=>r.GetProperty("landedValue").GetDecimal()));
  Assert.Contains(report.Rows,r=>r.GetProperty("reversalId").ValueKind!=JsonValueKind.Null);
  var evidence=string.Concat(report.Rows.Where(r=>r.GetProperty("evidencePart").GetInt32()>0).Select(r=>r.GetProperty("evidence").GetString()));
  Assert.Contains("AcceptedBills",evidence); Assert.Contains("ChargeAllocations",evidence); Assert.Contains("QcHistory",evidence); Assert.Contains("GrnNumber",evidence);
  using var excel=await client.GetAsync($"/api/v1/reports/machine-dossier/excel?selection={selection}");
  Assert.True(excel.IsSuccessStatusCode,await excel.Content.ReadAsStringAsync());
  var bytes=await excel.Content.ReadAsByteArrayAsync(); Assert.True(bytes.Length>1000);
  await using(var runtime=new Npgsql.NpgsqlConnection(context.RuntimeConnection)) {
   await runtime.OpenAsync(); await using var forbidden=runtime.CreateCommand(); forbidden.CommandText="SELECT * FROM advance.machine_delivery_signatures";
   var failure=await Assert.ThrowsAsync<Npgsql.PostgresException>(()=>forbidden.ExecuteNonQueryAsync()); Assert.Equal("42501",failure.SqlState);
  }
  user.Set(context.StoresId,"SESS-35",Rev869ARoleCodes.StoresExecutive);
  using(var deniedReport=await client.GetAsync(path)) Assert.Equal(HttpStatusCode.Forbidden,deniedReport.StatusCode);
  var root=Path.Combine(FindRepositoryRoot(),"local-evidence","item15"); Directory.CreateDirectory(root);
  await File.WriteAllBytesAsync(Path.Combine(root,"machine-dossier-"+nature+"-witness.xlsx"),bytes);
  await File.WriteAllTextAsync(Path.Combine(root,"machine-dossier-"+nature+"-witness.json"),JsonSerializer.Serialize(new {Job=job.Id,Machine=job.MachineSerial,Delivery=delivered,Report=report,StockMovementsBefore=movements,StockMovementsAfter=await db.StockMovements.CountAsync(),FifoConsumptions=consumptions},new JsonSerializerOptions{WriteIndented=true}));
 });
 private static async Task MachineDcFieldRefusal(HttpResponseMessage response,params string[] fields)
 {
  var body=await response.Content.ReadAsStringAsync();
  Assert.True(response.StatusCode==HttpStatusCode.BadRequest,body);
  using var envelope=JsonDocument.Parse(body);
  Assert.Equal("VALIDATION_FAILED",envelope.RootElement.GetProperty("Code").GetString());
  Assert.Equal(fields.Order(StringComparer.Ordinal),envelope.RootElement.GetProperty("Errors").EnumerateObject().Select(e=>e.Name).Order(StringComparer.Ordinal));
 }
 private static async Task MachineDcConflict(HttpResponseMessage response,string detail)
 {
  var body=await response.Content.ReadAsStringAsync();
  Assert.True(response.StatusCode==HttpStatusCode.Conflict,body);
  using var envelope=JsonDocument.Parse(body);
  Assert.Equal("BUSINESS_RULE_CONFLICT",envelope.RootElement.GetProperty("Code").GetString());
  Assert.Equal(detail,envelope.RootElement.GetProperty("Detail").GetString());
 }
#endif
}
