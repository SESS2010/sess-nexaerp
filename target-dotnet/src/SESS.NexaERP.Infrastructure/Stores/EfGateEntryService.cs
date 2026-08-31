using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfGateEntryService(NexaErpDbContext db,ICurrentUser user,IRecordScopeAuthorizer scopes,IAuditWriter audit) : IGateEntryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GateEntryResult> CreateAsync(CreateGateEntryRequest request,string idempotencyKey,CancellationToken ct)
    {
        var actor=Actor(); var org=Organization(); var key=Required(idempotencyKey,"Idempotency-Key");
        ValidateBody(request.VendorDcNumber,request.ModeOfTransport,request.ArrivedAt,request.IsoReceiptVerificationJson,request.Lines);
        var fingerprint=Hash(new { PurchaseOrderNumber=Required(request.PurchaseOrderNumber,"PurchaseOrderNumber").ToUpperInvariant(),VendorDcNumber=request.VendorDcNumber.Trim(),VehicleNumber=Trim(request.VehicleNumber),ModeOfTransport=request.ModeOfTransport.Trim(),request.ArrivedAt,IsoReceiptVerificationJson=CanonicalObject(request.IsoReceiptVerificationJson),Lines=request.Lines.OrderBy(x=>x.PurchaseOrderLineId).Select(x=>new{x.PurchaseOrderLineId,x.DeliveredQuantity}) });
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
        var company=await db.Companies.SingleOrDefaultAsync(x=>x.Code==org&&x.IsActive&&x.Status=="ACTIVE",ct) ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
        var createLock=$"GATE:CREATE:{company.Id}:{key}";
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({createLock},0))",ct);
        var replay=await db.GateEntries.Include(x=>x.PurchaseOrder).Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.CompanyId==company.Id&&x.IdempotencyKey==key,ct);
        if(replay is not null){if(replay.RequestFingerprint!=fingerprint)throw new StoresConflictException("Idempotency key was reused with different Gate Entry data."); await tx.CommitAsync(ct); return await LoadResultAsync(replay.Id,company.Id,ct);}
        var poNumber=Required(request.PurchaseOrderNumber,"PurchaseOrderNumber").ToUpperInvariant();
        var po=await db.PurchaseOrders.Include(x=>x.Vendor).Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.CompanyId==company.Id&&x.OrganizationId==org&&x.PoNumber==poNumber&&x.IsCurrentVersion,ct) ?? throw new KeyNotFoundException("Issued Purchase Order was not found in the selected company.");
        if(po.Status!=Rev869BStatuses.Issued)throw new StoresConflictException("Gate Entry requires a current issued Purchase Order.");
        await RequireScope(po.RequestingDepartmentId,po.DeliveryWarehouseId,po.OwnerEmployeeId,ct);
        var lines=BuildLines(request.Lines,po,company.Id);
        var number=await NextNumber(company.Id,org,DateOnly.FromDateTime(request.ArrivedAt.UtcDateTime),ct);
        var gate=new GateEntry{CompanyId=company.Id,GateEntryNumber=number,PurchaseOrderId=po.Id,VendorId=po.VendorId,VendorNameSnapshot=po.Vendor?.Name??throw new StoresConflictException("Purchase Order vendor is unavailable."),VendorDcNumber=request.VendorDcNumber.Trim(),VehicleNumber=Trim(request.VehicleNumber),ModeOfTransport=request.ModeOfTransport.Trim(),ArrivedAt=request.ArrivedAt,ReceivedByEmployeeId=actor,IsoReceiptVerificationJson=CanonicalObject(request.IsoReceiptVerificationJson),IdempotencyKey=key,RequestFingerprint=fingerprint,CreatedBy=user.LoginId,Lines=lines};
        db.GateEntries.Add(gate); AddHistory(gate,null,"DRAFT","CREATED",Hash($"GATE:CREATED:{gate.Id}"));
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Stores","CreateGateEntry",nameof(GateEntry),gate.Id.ToString(),null,new{gate.GateEntryNumber,po.PoNumber,gate.Status,Role=ActorRole()},ct);
        await tx.CommitAsync(ct); return await LoadResultAsync(gate.Id,company.Id,ct);
    }

    public async Task<GateEntryResult> UpdateAsync(Guid id,UpdateGateEntryRequest request,CancellationToken ct)
    {
        Actor(); var org=Organization(); ValidateBody(request.VendorDcNumber,request.ModeOfTransport,request.ArrivedAt,request.IsoReceiptVerificationJson,request.Lines);
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
        var company=await Company(org,ct); var gate=await db.GateEntries.AsNoTracking().Include(x=>x.PurchaseOrder).ThenInclude(x=>x!.Lines).Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.Id==id&&x.CompanyId==company.Id,ct)??throw new KeyNotFoundException("Gate Entry was not found.");
        if(gate.Status!="DRAFT")throw new StoresConflictException("A finalized Gate Entry is immutable."); if(gate.Version!=request.Version)throw new DbUpdateConcurrencyException("Gate Entry Version is stale.");
        var po=gate.PurchaseOrder!; await RequireScope(po.RequestingDepartmentId,po.DeliveryWarehouseId,po.OwnerEmployeeId,ct);
        var replacements=BuildLines(request.Lines,po,company.Id);var lineJson=JsonSerializer.Serialize(replacements.Select(x=>new{x.LineNumber,x.PurchaseOrderLineId,x.DeliveredQuantity}),JsonOptions);var isoJson=CanonicalObject(request.IsoReceiptVerificationJson);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT advance.replace_gate_entry_draft({company.Id},{id},{(long)request.Version},{request.VendorDcNumber.Trim()},{Trim(request.VehicleNumber)},{request.ModeOfTransport.Trim()},{request.ArrivedAt},{isoJson}::jsonb,{user.LoginId},{lineJson}::jsonb)",ct);
        var nextVersion=request.Version+1;await audit.WriteAsync("Stores","UpdateGateEntry",nameof(GateEntry),gate.Id.ToString(),new{Version=request.Version},new{Version=nextVersion,Role=ActorRole()},ct); await tx.CommitAsync(ct); return await LoadResultAsync(id,company.Id,ct);
    }

    public async Task<GateEntryResult> FinalizeAsync(Guid id,FinalizeGateEntryRequest request,CancellationToken ct)
    {
        var actor=Actor(); var org=Organization(); var correlation=Hash($"GATE:FINALIZE:{id}:{Required(request.IdempotencyKey,"IdempotencyKey")}");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct); var company=await Company(org,ct);
        var finalizeLock=$"GATE:FINALIZE:{id}";
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({finalizeLock},0))",ct);
        if(await db.StoresDocumentStatusHistories.AnyAsync(x=>x.GateEntryId==id&&x.CorrelationId==correlation,ct)){await tx.CommitAsync(ct);return await LoadResultAsync(id,company.Id,ct);}
        var gate=await db.GateEntries.AsNoTracking().Include(x=>x.PurchaseOrder).Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.Id==id&&x.CompanyId==company.Id,ct)??throw new KeyNotFoundException("Gate Entry was not found.");
        var po=gate.PurchaseOrder!; await RequireScope(po.RequestingDepartmentId,po.DeliveryWarehouseId,po.OwnerEmployeeId,ct);
        if(gate.Status!="DRAFT")throw new StoresConflictException("Gate Entry is already finalized."); if(gate.Version!=request.Version)throw new DbUpdateConcurrencyException("Gate Entry Version is stale."); if(gate.Lines.Count==0)throw new StoresValidationException("Gate Entry requires at least one delivered line.");
        var finalizedAt=DateTimeOffset.UtcNow;var nextVersion=request.Version+1;var affected=await db.GateEntries.Where(x=>x.Id==id&&x.CompanyId==company.Id&&x.Status=="DRAFT"&&x.Version==request.Version).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,"FINALIZED").SetProperty(x=>x.FinalizedAt,finalizedAt).SetProperty(x=>x.FinalizedByEmployeeId,actor).SetProperty(x=>x.Version,nextVersion).SetProperty(x=>x.UpdatedAt,finalizedAt).SetProperty(x=>x.UpdatedBy,user.LoginId),ct);if(affected!=1)throw new DbUpdateConcurrencyException("Gate Entry Version is stale.");AddHistory(gate,"DRAFT","FINALIZED","FINALIZED",correlation);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Stores","FinalizeGateEntry",nameof(GateEntry),gate.Id.ToString(),new{Status="DRAFT",request.Version},new{Status="FINALIZED",Version=nextVersion,Role=ActorRole()},ct); await tx.CommitAsync(ct); return await LoadResultAsync(id,company.Id,ct);
    }
}
