using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfGateEntryService
{
    public async Task<GateEntryResult?> GetAsync(Guid id,CancellationToken ct)
    {
        await RequireReceiptOperatorAsync(ct); var company=await Company(Organization(),ct); var gate=await Query().SingleOrDefaultAsync(x=>x.Id==id&&x.CompanyId==company.Id,ct); if(gate is null)return null;
        await RequireScope(gate.PurchaseOrder!.RequestingDepartmentId,gate.PurchaseOrder.DeliveryWarehouseId,gate.PurchaseOrder.OwnerEmployeeId,ct); return Map(gate);
    }

    public async Task<GateEntryListResult> ListAsync(string? poNumber,Guid? vendorId,DateOnly? from,DateOnly? to,string? state,int page,int pageSize,CancellationToken ct)
    {
        await RequireReceiptOperatorAsync(ct); if(page<1||pageSize is <1 or >100)throw new StoresValidationException("page must be positive and pageSize must be 1-100."); var company=await Company(Organization(),ct);
        var q=Query().Where(x=>x.CompanyId==company.Id);
        if(!string.IsNullOrWhiteSpace(poNumber))q=q.Where(x=>x.PurchaseOrder!.PoNumber==poNumber.Trim().ToUpperInvariant()); if(vendorId.HasValue)q=q.Where(x=>x.VendorId==vendorId);
        if(from.HasValue)q=q.Where(x=>x.ArrivedAt>=from.Value.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc)); if(to.HasValue)q=q.Where(x=>x.ArrivedAt<to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc));
        if(!string.IsNullOrWhiteSpace(state)){var s=state.Trim().ToUpperInvariant();if(s is not("DRAFT" or "FINALIZED"))throw new StoresValidationException("state must be DRAFT or FINALIZED.");q=q.Where(x=>x.Status==s);}
        var candidates=await q.OrderByDescending(x=>x.ArrivedAt).ThenBy(x=>x.Id).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct); var result=new List<GateEntryResult>();
        foreach(var gate in candidates)if((await scopes.AuthorizeAsync(Actor(),ActorRole(),new RecordScopeTarget(Organization(),gate.PurchaseOrder!.RequestingDepartmentId,gate.PurchaseOrder.DeliveryWarehouseId,null,gate.PurchaseOrder.OwnerEmployeeId),DateOnly.FromDateTime(DateTime.UtcNow),ct)).Allowed)result.Add(Map(gate));
        return new(page,pageSize,result);
    }

    private IQueryable<GateEntry> Query()=>db.GateEntries.AsNoTracking().Include(x=>x.PurchaseOrder).Include(x=>x.Lines).Include(x=>x.Vendor);
    private async Task<GateEntryResult> LoadResultAsync(Guid id,Guid companyId,CancellationToken ct)=>Map(await Query().SingleAsync(x=>x.Id==id&&x.CompanyId==companyId,ct));
    private GateEntryResult Map(GateEntry x)
    {
        var history=db.StoresDocumentStatusHistories.AsNoTracking().Where(h=>h.GateEntryId==x.Id).OrderBy(h=>h.OccurredAt).Select(h=>new GateEntryHistoryResult(h.FromStatus,h.ToStatus,h.Action,h.ActorEmployeeId,h.ActorRoleCode,h.OccurredAt)).ToList();
        return new(x.Id,x.GateEntryNumber,x.PurchaseOrder!.PoNumber,x.PurchaseOrderId,x.VendorId,x.VendorNameSnapshot,x.VendorDcNumber,x.VehicleNumber,x.ModeOfTransport,x.ArrivedAt,x.IsoReceiptVerificationJson,x.Status,x.Version,x.Lines.OrderBy(l=>l.LineNumber).Select(l=>new GateEntryLineResult(l.Id,l.LineNumber,l.PurchaseOrderLineId,l.ItemId,l.ItemCodeSnapshot,l.UomSnapshot,l.DeliveredQuantity)).ToList(),history);
    }
    private List<GateEntryLine> BuildLines(IReadOnlyList<GateEntryLineRequest> input,PurchaseOrder po,Guid companyId)
    {
        if(input.Count==0)throw new StoresValidationException("At least one delivered PO line is required."); if(input.Select(x=>x.PurchaseOrderLineId).Distinct().Count()!=input.Count)throw new StoresValidationException("A PO line may appear only once."); var result=new List<GateEntryLine>(); var n=0;
        foreach(var row in input){if(row.DeliveredQuantity<=0)throw new StoresValidationException("DeliveredQuantity must be positive.");var line=po.Lines.SingleOrDefault(x=>x.Id==row.PurchaseOrderLineId)??throw new StoresValidationException("Every Gate Entry line must belong to the selected Purchase Order.");result.Add(new GateEntryLine{CompanyId=companyId,PurchaseOrderId=po.Id,PurchaseOrderLineId=line.Id,LineNumber=++n,ItemId=line.ItemId,ItemCodeSnapshot=line.ItemCodeSnapshot,UomSnapshot=line.UomSnapshot,DeliveredQuantity=row.DeliveredQuantity,CreatedBy=user.LoginId});} return result;
    }
    private async Task<string> NextNumber(Guid companyId,string org,DateOnly date,CancellationToken ct)
    {
        var year=date.Month>=4?$"{date.Year%100:00}-{(date.Year+1)%100:00}":$"{(date.Year-1)%100:00}-{date.Year%100:00}"; var numberLock=$"NUMBER:{org}:{year}:GE"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({numberLock},0))",ct);
        var seq=await db.PurchaseNumberSequences.SingleOrDefaultAsync(x=>x.CompanyId==companyId&&x.OrganizationId==org&&x.FinancialYear==year&&x.Prefix=="GE"&&x.IsActive,ct); if(seq is null){seq=new PurchaseNumberSequence{CompanyId=companyId,OrganizationId=org,FinancialYear=year,Prefix="GE",CreatedBy=user.LoginId};db.PurchaseNumberSequences.Add(seq);} seq.LastNumber++;seq.UpdatedAt=DateTimeOffset.UtcNow;seq.UpdatedBy=user.LoginId;return $"GE-{year}-{seq.LastNumber:000001}";
    }
    private void AddHistory(GateEntry gate,string? from,string to,string action,string correlation)=>db.StoresDocumentStatusHistories.Add(new StoresDocumentStatusHistory{CompanyId=gate.CompanyId,GateEntryId=gate.Id,FromStatus=from,ToStatus=to,Action=action,ActorEmployeeId=Actor(),ActorRoleCode=ActorRole(),OccurredAt=DateTimeOffset.UtcNow,CorrelationId=correlation});
    private async Task RequireScope(Guid? department,Guid? warehouse,Guid owner,CancellationToken ct){var decision=await scopes.AuthorizeAsync(Actor(),ActorRole(),new RecordScopeTarget(Organization(),department,warehouse,null,owner),DateOnly.FromDateTime(DateTime.UtcNow),ct);if(!decision.Allowed)throw new UnauthorizedAccessException("Gate Entry record scope is denied.");}
    private Guid Actor()=>user.IsAuthenticated&&user.EmployeeId.HasValue?user.EmployeeId.Value:throw new UnauthorizedAccessException("A resolved employee identity is required.");
    private string ActorRole(){foreach(var role in new[]{"STORES_EXECUTIVE","STORES_ASSISTANT"})if(user.RoleCodes.Contains(role,StringComparer.OrdinalIgnoreCase))return role;throw new UnauthorizedAccessException("A Stores receipt operational role is required.");}
    private async Task RequireReceiptOperatorAsync(CancellationToken ct){var code=await db.Employees.AsNoTracking().Where(x=>x.Id==Actor()).Select(x=>x.EmployeeCode).SingleOrDefaultAsync(ct);if(code is not("SESS-16" or "SESS-35" or "SESS-41"))throw new UnauthorizedAccessException("Gate Entry is restricted to the three settled receipt operators.");}
    private string Organization()=>!string.IsNullOrWhiteSpace(user.OrganizationId)?user.OrganizationId.Trim().ToUpperInvariant():throw new UnauthorizedAccessException("Company scope is required.");
    private async Task<Company> Company(string org,CancellationToken ct)=>await db.Companies.SingleOrDefaultAsync(x=>x.Code==org&&x.IsActive&&x.Status=="ACTIVE",ct)??throw new UnauthorizedAccessException("Selected company is unavailable.");
    private static void ValidateBody(string dc,string mode,DateTimeOffset arrived,string json,IReadOnlyList<GateEntryLineRequest> lines){Required(dc,"VendorDcNumber");Required(mode,"ModeOfTransport");if(arrived==default)throw new StoresValidationException("ArrivedAt is required.");CanonicalObject(json);if(lines is null)throw new StoresValidationException("Lines are required.");}
    private static string CanonicalObject(string value){try{using var d=JsonDocument.Parse(Required(value,"IsoReceiptVerificationJson"));if(d.RootElement.ValueKind!=JsonValueKind.Object)throw new StoresValidationException("IsoReceiptVerificationJson must be a JSON object.");return JsonSerializer.Serialize(d.RootElement,JsonOptions);}catch(JsonException){throw new StoresValidationException("IsoReceiptVerificationJson must be valid JSON.");}}
    private static string Hash(object value)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value is string s?s:JsonSerializer.Serialize(value,JsonOptions)))).ToLowerInvariant();
    private static string Required(string? value,string name)=>!string.IsNullOrWhiteSpace(value)?value.Trim():throw new StoresValidationException($"{name} is required.");
    private static string? Trim(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}
