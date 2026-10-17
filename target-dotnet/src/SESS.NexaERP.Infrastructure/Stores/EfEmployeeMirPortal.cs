using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

/// <summary>Own-document facade for every portal user, including users with other business roles.</summary>
public sealed class EfEmployeeMirPortal(NexaErpDbContext db, ICurrentUser user, IMaterialIssueService service) : IEmployeeMirPortal
{
    private Guid Actor => user.IsAuthenticated && user.EmployeeId is { } id && id != Guid.Empty
        ? id : throw new UnauthorizedAccessException("Resolved employee identity is required.");
    private async Task<Guid> Company(CancellationToken ct)
    {
        _ = Actor;
        return await db.Companies.AsNoTracking().Where(c => c.Code == user.OrganizationId && c.IsActive && c.Status == "ACTIVE")
            .Select(c => (Guid?)c.Id).SingleOrDefaultAsync(ct) ?? throw new UnauthorizedAccessException("Company is unavailable.");
    }
    private async Task OwnRequest(Guid id, CancellationToken ct)
    {
        var company = await Company(ct); var actor = Actor;
        if (!await db.MaterialIssueRequests.AsNoTracking().AnyAsync(x => x.CompanyId == company && x.Id == id && x.RequestedByEmployeeId == actor,ct))
            throw new KeyNotFoundException("MIR was not found.");
    }
    private async Task OwnIssue(Guid id, CancellationToken ct)
    {
        var company = await Company(ct); var actor = Actor;
        if (!await db.MaterialIssues.AsNoTracking().AnyAsync(x => x.CompanyId == company && x.Id == id && x.IssuedToEmployeeId == actor,ct))
            throw new KeyNotFoundException("Material Issue was not found.");
    }
    public async Task<MaterialIssueRequestPage> ListAsync(string? number,string? status,int page,int pageSize,CancellationToken ct)
    {
        var company=await Company(ct); var actor=Actor; page=Math.Max(1,page); pageSize=Math.Clamp(pageSize,1,50);
        var query=db.MaterialIssueRequests.AsNoTracking().Where(x=>x.CompanyId==company && x.RequestedByEmployeeId==actor);
        if(!string.IsNullOrWhiteSpace(number)) { var term=number.Trim().ToUpperInvariant(); query=query.Where(x=>x.RequestNumber.ToUpper().Contains(term)); }
        if(!string.IsNullOrWhiteSpace(status)) { var value=status.Trim().ToUpperInvariant(); query=query.Where(x=>x.Status==value); }
        var count=await query.CountAsync(ct);
        var ids=await query.OrderByDescending(x=>x.CreatedAt).ThenBy(x=>x.Id).Skip((page-1)*pageSize).Take(pageSize).Select(x=>x.Id).ToListAsync(ct);
        List<MaterialIssueRequestView> rows=[];
        foreach(var id in ids) if(await service.GetRequestAsync(id,ct) is { } row) rows.Add(row);
        return new(count,page,pageSize,rows);
    }
    public async Task<MaterialIssueRequestView?> GetAsync(Guid id,CancellationToken ct)
    {
        await OwnRequest(id,ct); return await service.GetRequestAsync(id,ct);
    }
    public async Task<MaterialIssueRequestView> CreateAsync(EmployeeMirCreate command,CancellationToken ct)
    {
        if(command.Request is null) throw new StoresValidationException("MIR fields are required.");
        await Validate(command.Request.RequestingDepartmentId,command.Request.Lines,ct);
        return await service.CreateRequestAsync(command.Request with { Lines=JobLines(command.JobOrMachine,command.Request.Lines) },ct);
    }
    public async Task<MaterialIssueRequestView> UpdateAsync(Guid id,EmployeeMirUpdate command,CancellationToken ct)
    {
        await OwnRequest(id,ct);
        if(command.Request is null) throw new StoresValidationException("MIR fields are required.");
        await Validate(command.Request.RequestingDepartmentId,command.Request.Lines,ct);
        return await service.UpdateRequestAsync(id,command.Request with { Lines=JobLines(command.JobOrMachine,command.Request.Lines) },ct);
    }
    public async Task<MaterialIssueRequestView> SubmitAsync(Guid id,MaterialIssueTransitionRequest command,CancellationToken ct)
    {
        await OwnRequest(id,ct); return await service.SubmitAsync(id,command,ct);
    }
    private async Task Validate(Guid department,IReadOnlyList<MaterialIssueRequestLineInput> lines,CancellationToken ct)
    {
        _=await Company(ct);
        if(!user.DepartmentId.HasValue || department!=user.DepartmentId.Value)
            throw new UnauthorizedAccessException("Requesting department must be your resolved department.");
        if(lines is null || lines.Count==0) throw new StoresValidationException("At least one MIR line is required.");
        var ids=lines.Select(x=>x.ItemId).Distinct().ToArray();
        if(await db.Items.AsNoTracking().CountAsync(x=>ids.Contains(x.Id) && x.IsActive && x.ApprovalStatus==MasterApprovalStatuses.Approved,ct)!=ids.Length)
            throw new StoresValidationException("Every requested item must be approved and active.");
    }
    private static IReadOnlyList<MaterialIssueRequestLineInput> JobLines(string reference,IReadOnlyList<MaterialIssueRequestLineInput> lines)
    {
        if(string.IsNullOrWhiteSpace(reference) || reference.Length>200)
            throw new StoresValidationException("Job/machine reference is required (maximum 200 characters).");
        var value=reference.Trim().Replace('\r',' ').Replace('\n',' ');
        return lines.Select(line=>
        {
            var remarks="[Job/machine] "+value+"\n"+
                string.Join("\n",(line.Remarks??"").Split('\n').Where(part=>!part.StartsWith("[Job/machine] ",StringComparison.Ordinal))).Trim();
            if(remarks.Length>1000) throw new StoresValidationException("Job/machine reference and line remarks must fit within 1000 characters.");
            return line with { Remarks=remarks };
        }).ToArray();
    }
    public async Task<IReadOnlyList<EmployeeMirItem>> SearchItemsAsync(string? query,bool scan,CancellationToken ct)
    {
        var company=await Company(ct); var term=query?.Trim()??"";
        if(term.Length==0 || term.Length>200) return [];
        if(term.Any(char.IsControl)) throw new StoresValidationException("Enter a text barcode, item code or serial without control characters.");
        var code=term.ToUpperInvariant();
        var isLot=GrnLotBarcode.TryParse(code,out var lotCompany,out var lotNumber,out var lotLine,out var lotOrdinal) && lotCompany==user.OrganizationId;
        return await db.Items.AsNoTracking().Where(x=>x.IsActive && x.ApprovalStatus==MasterApprovalStatuses.Approved
            && x.BaseUom!=null && x.BaseUom.IsActive && (x.ItemCode.ToUpper()==code || x.Barcode!=null && x.Barcode.ToUpper()==code
                || !scan && (x.ItemCode.StartsWith(code) || x.Name.ToUpper().Contains(code))
                || db.InventorySerials.Any(serial=>serial.CompanyId==company && serial.ItemId==x.Id && serial.StoredSerialNumber.ToUpper()==code)
                || isLot && db.GoodsReceiptLineLotAllocations.Any(a=>a.CompanyId==company && a.LotOrdinal==lotOrdinal
                    && a.GoodsReceiptLine!=null && a.GoodsReceiptLine.ItemId==x.Id && a.GoodsReceiptLine.LineNumber==lotLine
                    && a.GoodsReceiptLine.GoodsReceipt!=null && a.GoodsReceiptLine.GoodsReceipt.GrnNumber==lotNumber
                    && a.GoodsReceiptLine.GoodsReceipt.CompanyId==company)))
            .OrderBy(x=>x.ItemCode).Take(50).Select(x=>new EmployeeMirItem(x.Id,x.ItemCode,x.Name,x.BaseUomId,x.BaseUom!.Code) { Barcode=x.Barcode }).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<EmployeeMirSpareLine>> SpareLinesAsync(Guid customerId,CancellationToken ct)
    {
        var company=await Company(ct);
        return await db.CustomerPurchaseOrderLines.AsNoTracking().Where(x=>x.CustomerPurchaseOrder!=null
            && x.CustomerPurchaseOrder.CompanyId==company && x.CustomerPurchaseOrder.CustomerId==customerId
            && x.RevisionNumber==x.CustomerPurchaseOrder.CurrentRevisionNumber
            && (x.CustomerPurchaseOrder.SalesType=="Spares" || x.CustomerPurchaseOrder.SalesType=="Spares & Service")
            && x.Quantity>0)
            .OrderBy(x=>x.CustomerPurchaseOrder!.PoRecordNumber).ThenBy(x=>x.SlNo)
            .Select(x=>new EmployeeMirSpareLine(x.Id,x.ItemId,x.CustomerPurchaseOrder!.CustomerPoNumber,x.SlNo,x.Description,x.Quantity!.Value)).ToListAsync(ct);
    }
    public async Task<EmployeeMirReferences> ReferencesAsync(CancellationToken ct)
    {
        var company=await Company(ct); var department=user.DepartmentId ?? throw new UnauthorizedAccessException("Department is required.");
        var departmentCode=await db.Departments.AsNoTracking().Where(x=>x.Id==department && x.IsActive).Select(x=>x.Code).SingleOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("Department is unavailable.");
        var jobs=await db.JobOrders.AsNoTracking().Where(x=>x.CompanyId==company && x.Status=="OPEN" && x.AccountsConfirmedAt!=null && x.PinnedProductionBomRevisionId!=null)
            .OrderBy(x=>x.JobOrderNumber).Select(x=>new EmployeeMirReference(x.Id,x.JobOrderNumber,x.CustomerName)).ToListAsync(ct);
        var customers=await db.Customers.AsNoTracking().Where(x=>x.IsActive && x.ApprovalStatus==MasterApprovalStatuses.Approved)
            .OrderBy(x=>x.CustomerCode).Select(x=>new EmployeeMirReference(x.Id,x.CustomerCode,x.Name)).ToListAsync(ct);
        return new(department,departmentCode,jobs,customers);
    }
    public async Task<IReadOnlyList<OutstandingEngineerCustodyView>> CustodyAsync(CancellationToken ct)
    {
        _=await Company(ct); return await service.OutstandingCustodyAsync(Actor,null,ct);
    }
    public async Task<EmployeeMirIssue?> GetIssueAsync(Guid id,CancellationToken ct)
    {
        await OwnIssue(id,ct);
        var company=await Company(ct);
        var issue=await service.GetIssueAsync(id,ct);
        if(issue is null) return null;
        var ids=issue.Lines.Select(x=>x.ItemId).Distinct().ToArray();
        var items=await db.Items.AsNoTracking().Where(x=>ids.Contains(x.Id))
            .Select(x=>new EmployeeMirItem(x.Id,x.ItemCode,x.Name,x.BaseUomId,x.BaseUom!.Code) { Barcode=x.Barcode }).ToListAsync(ct);
        var lineIds=issue.Lines.Select(x=>x.Id).ToArray();
        var declared=await db.MaterialReturnLines.AsNoTracking().Where(x=>x.CompanyId==company && lineIds.Contains(x.MaterialIssueLineId))
            .GroupBy(x=>x.MaterialIssueLineId).Select(g=>new { Id=g.Key, Quantity=g.Sum(x=>x.ReturnedQuantityBase) }).ToDictionaryAsync(x=>x.Id,x=>x.Quantity,ct);
        var returnable=issue.Lines.ToDictionary(x=>x.Id,x=>Math.Max(0,x.QuantityBase-x.FittedQuantityBase-declared.GetValueOrDefault(x.Id)));
        return new(issue,items,returnable);
    }
    public async Task<MaterialReturnPage> ReturnsAsync(int page,int pageSize,CancellationToken ct)
    {
        var company=await Company(ct); var actor=Actor; page=Math.Max(1,page); pageSize=Math.Clamp(pageSize,1,50);
        var query=db.MaterialReturns.AsNoTracking().Where(x=>x.CompanyId==company && x.ReturnedByEmployeeId==actor
            && x.MaterialIssue!=null && x.MaterialIssue.IssuedToEmployeeId==actor);
        var total=await query.CountAsync(ct);
        var ids=await query.OrderByDescending(x=>x.DeclaredAt).ThenBy(x=>x.Id).Skip((page-1)*pageSize).Take(pageSize).Select(x=>x.Id).ToListAsync(ct);
        List<MaterialReturnView> rows=[];
        foreach(var id in ids) if(await service.GetReturnAsync(id,ct) is { } row) rows.Add(row);
        return new(total,page,pageSize,rows);
    }
    public async Task<MaterialReturnView?> GetReturnAsync(Guid id,CancellationToken ct)
    {
        var company=await Company(ct); var actor=Actor;
        if(!await db.MaterialReturns.AsNoTracking().AnyAsync(x=>x.Id==id && x.CompanyId==company && x.ReturnedByEmployeeId==actor
            && x.MaterialIssue!=null && x.MaterialIssue.IssuedToEmployeeId==actor,ct)) throw new KeyNotFoundException("Material Return was not found.");
        return await service.GetReturnAsync(id,ct);
    }
    public async Task<MaterialReturnView> DeclareReturnAsync(Guid issueId,CreateMaterialReturn command,CancellationToken ct)
    {
        await OwnIssue(issueId,ct); return await service.CreateReturnAsync(issueId,command,ct);
    }
}
