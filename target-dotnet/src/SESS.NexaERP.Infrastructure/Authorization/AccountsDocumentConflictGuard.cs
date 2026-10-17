using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Stores;
using System.Text.Json;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Authorization;

public static class AccountsDocumentConflictGuard
{
    /// <summary>Checks retained duties of canonical SESS-41, independent of which of his realms is used.
    /// Historical subject mappings here attribute past audit facts only; they NEVER authenticate a login.
    /// Current identity/role/scope checks remain in the normal resolver and command contract.</summary>
    public static async Task RequireRequisitionDutyAsync(NexaErpDbContext db,ICurrentUser user,
        PurchaseRequisition pr,AccountsDocumentDuty proposed,CancellationToken ct)
    {
        if(user.EmployeeId is not { } actor || !user.IsAuthenticated)
            throw new UnauthorizedAccessException("A resolved employee identity is required.");
        if(user.EmployeeCode is not null && user.EmployeeCode!=AccountsBackupIdentityPolicy.EmployeeCode) return;
        if(!await db.Employees.AsNoTracking().AnyAsync(e=>e.Id==actor&&e.EmployeeCode==AccountsBackupIdentityPolicy.EmployeeCode,ct)) return;
        if(user.OrganizationId!=pr.OrganizationId || !await db.Companies.AsNoTracking().AnyAsync(c=>c.Id==pr.CompanyId&&c.Code==user.OrganizationId&&c.IsActive&&c.Status=="ACTIVE",ct))
            throw new UnauthorizedAccessException("The document is outside the active selected company.");
        if(db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Document duty checks require the command transaction.");
        var lockKey=$"R1-ACCOUNTS-DUTY:{pr.CompanyId:N}:{pr.Id:N}";
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey},0))",ct);
        var aliases=await db.EmployeeIdentityMappings.AsNoTracking().Where(m=>m.EmployeeId==actor&&m.CompanyId==pr.CompanyId)
            .Select(m=>m.Subject).Distinct().ToListAsync(ct);
        aliases.Add(AccountsBackupIdentityPolicy.EmployeeCode);aliases.Add(AccountsBackupIdentityPolicy.EmployeeCode.ToLowerInvariant());
        var facts=new List<AccountsDocumentParticipation>();
        if(pr.CreatorEmployeeId==actor || pr.RequesterEmployeeId==actor)facts.Add(new(actor,AccountsDocumentDuty.Recorded));
        if(await db.StockAvailabilityChecks.AsNoTracking().AnyAsync(c=>c.CompanyId==pr.CompanyId&&c.PurchaseRequisitionId==pr.Id&&aliases.Contains(c.CheckedBy),ct))
            facts.Add(new(actor,AccountsDocumentDuty.Recorded));
        var decisions=await db.PurchaseRequisitionApprovalHistories.AsNoTracking().Where(h=>h.CompanyId==pr.CompanyId&&h.PurchaseRequisitionId==pr.Id&&h.ResolvedEmployeeId==actor)
            .Select(h=>h.ResolvedRoleCode).ToListAsync(ct);
        foreach(var role in decisions)
            facts.Add(new(actor,role=="ACCOUNTS_MANAGER"?AccountsDocumentDuty.L1Decision:
                role is "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR"?AccountsDocumentDuty.FinalDecision:AccountsDocumentDuty.StoresDecision));
        if(await db.PurchaseRequisitionStatusHistories.AsNoTracking().AnyAsync(h=>h.CompanyId==pr.CompanyId&&h.PurchaseRequisitionId==pr.Id&&
            aliases.Contains(h.ActorLoginId)&&h.ActorRoleCode=="ACCOUNTS_MANAGER",ct))
            facts.Add(new(actor,AccountsDocumentDuty.DepartmentVerified));
        try { AccountsDocumentSegregation.RequireIndependent(actor,proposed,facts); }
        catch(InvalidOperationException e) { throw new UnauthorizedAccessException(e.Message,e); }
    }
    public static async Task RequireComparisonDutyAsync(NexaErpDbContext db,ICurrentUser user,
        CommercialComparison comparison,AccountsDocumentDuty proposed,CancellationToken ct)
    {
        if(!await BeginCanonicalDutyAsync(db,user,comparison.CompanyId,comparison.OrganizationId,comparison.Id,"COMPARISON",ct))return;
        var actor=user.EmployeeId!.Value;var facts=new List<AccountsDocumentParticipation>();
        if(comparison.CreatorEmployeeId==actor||comparison.OwnerEmployeeId==actor)facts.Add(new(actor,AccountsDocumentDuty.Recorded));
        var roles=await db.PurchaseTransactionApprovalHistories.AsNoTracking()
            .Where(h=>h.CompanyId==comparison.CompanyId&&h.CommercialComparisonId==comparison.Id&&h.ActorEmployeeId==actor)
            .Select(h=>h.ActorRoleCode).ToListAsync(ct);
        AddDecisionFacts(actor,roles,facts);
        RequireIndependent(actor,proposed,facts);
    }

    public static async Task RequirePurchaseOrderDutyAsync(NexaErpDbContext db,ICurrentUser user,
        PurchaseOrder order,AccountsDocumentDuty proposed,CancellationToken ct)
    {
        if(!await BeginCanonicalDutyAsync(db,user,order.CompanyId,order.OrganizationId,order.Id,"PO",ct))return;
        var actor=user.EmployeeId!.Value;var facts=new List<AccountsDocumentParticipation>();
        if(order.CreatorEmployeeId==actor||order.OwnerEmployeeId==actor)facts.Add(new(actor,AccountsDocumentDuty.Recorded));
        var history=await db.PurchaseOrderHistories.AsNoTracking()
            .Where(h=>h.CompanyId==order.CompanyId&&h.PurchaseOrderId==order.Id&&h.ActorEmployeeId==actor)
            .Select(h=>new {h.ActorRoleCode,h.Action}).ToListAsync(ct);
        AddDecisionFacts(actor,history.Where(h=>h.Action!="Issue").Select(h=>h.ActorRoleCode),facts);
        if(history.Any(h=>h.Action=="Issue"))facts.Add(new(actor,AccountsDocumentDuty.Issued));
        RequireIndependent(actor,proposed,facts);
    }

    public static async Task RequireStockAdjustmentDutyAsync(NexaErpDbContext db,ICurrentUser user,
        StockAdjustment adjustment,AccountsDocumentDuty proposed,CancellationToken ct)
    {
        if(!await BeginCanonicalDutyAsync(db,user,adjustment.CompanyId,user.OrganizationId!,adjustment.Id,"ADJUSTMENT",ct))return;
        var actor=user.EmployeeId!.Value;var facts=new List<AccountsDocumentParticipation>();
        if(adjustment.RecordedByEmployeeId==actor)facts.Add(new(actor,AccountsDocumentDuty.Recorded));
        Guid[] counters;
        try { counters=JsonSerializer.Deserialize<Guid[]>(adjustment.CounterEmployeeIdsJson)??throw new JsonException(); }
        catch(JsonException e){throw new UnauthorizedAccessException("Retained counter evidence is malformed; independent review cannot be established.",e);}
        if(counters.Contains(actor))facts.Add(new(actor,AccountsDocumentDuty.Counted));
        var roles=await db.StockAdjustmentDecisions.AsNoTracking()
            .Where(h=>h.CompanyId==adjustment.CompanyId&&h.StockAdjustmentId==adjustment.Id&&h.EmployeeId==actor)
            .Select(h=>h.RoleCode).ToListAsync(ct);
        foreach(var role in roles)facts.Add(new(actor,role=="ACCOUNTS_MANAGER"?AccountsDocumentDuty.ValueReviewed:
            role is "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR"?AccountsDocumentDuty.FinalDecision:AccountsDocumentDuty.StoresDecision));
        RequireIndependent(actor,proposed,facts);
    }
    private static async Task<bool> BeginCanonicalDutyAsync(NexaErpDbContext db,ICurrentUser user,
        Guid company,string organization,Guid document,string kind,CancellationToken ct)
    {
        if(user.EmployeeId is not { } actor||!user.IsAuthenticated)throw new UnauthorizedAccessException("A resolved employee identity is required.");
        if(user.EmployeeCode is not null&&user.EmployeeCode!=AccountsBackupIdentityPolicy.EmployeeCode)return false;
        if(!await db.Employees.AsNoTracking().AnyAsync(e=>e.Id==actor&&e.EmployeeCode==AccountsBackupIdentityPolicy.EmployeeCode,ct))return false;
        if(user.OrganizationId!=organization||!await db.Companies.AsNoTracking().AnyAsync(c=>c.Id==company&&c.Code==organization&&c.IsActive&&c.Status=="ACTIVE",ct))
            throw new UnauthorizedAccessException("The document is outside the active selected company.");
        if(db.Database.CurrentTransaction is null)throw new InvalidOperationException("Document duty checks require the command transaction.");
        var key=$"R1-ACCOUNTS-DUTY:{company:N}:{kind}:{document:N}";
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key},0))",ct);
        return true;
    }
    private static void AddDecisionFacts(Guid actor,IEnumerable<string> roles,List<AccountsDocumentParticipation> facts)
    {
        foreach(var role in roles)facts.Add(new(actor,role=="ACCOUNTS_MANAGER"?AccountsDocumentDuty.L1Decision:
            role is "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR"?AccountsDocumentDuty.FinalDecision:
            role is "STORES_MANAGER" or "STORES_EXECUTIVE" or "STORES_ASSISTANT"?AccountsDocumentDuty.StoresDecision:AccountsDocumentDuty.Recorded));
    }
    private static void RequireIndependent(Guid actor,AccountsDocumentDuty proposed,IEnumerable<AccountsDocumentParticipation> facts)
    {
        try { AccountsDocumentSegregation.RequireIndependent(actor,proposed,facts); }
        catch(InvalidOperationException e){throw new UnauthorizedAccessException(e.Message,e);}
    }}
