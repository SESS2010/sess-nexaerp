using SESS.NexaERP.Domain.Stores;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfMachineDeliveryService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : IMachineDeliveryService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string Organization => user.OrganizationId?.Trim().ToUpperInvariant() ?? throw new UnauthorizedAccessException("Select a company.");
    private async Task<Guid> Company(CancellationToken ct) => await db.Companies.Where(c => c.Code == Organization && c.IsActive && c.Status == "ACTIVE")
        .Select(c => (Guid?)c.Id).SingleOrDefaultAsync(ct) ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
    private async Task<NpgsqlCommand> Command(string sql, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        return new(sql, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
    }
    public Task<JsonElement> DispatchAsync(DispatchMachineRequest request, CancellationToken ct) =>
        Execute(null,"MachineDelivery.Dispatch",request.IdempotencyKey,request,null,ct);
    public Task<JsonElement> SignAsync(Guid id, SignMachineDeliveryRequest request, CancellationToken ct)
    {
        if (request.Evidence?.Content is not { Length: > 0 and <= 5242880 } content)
            throw new StoresValidationException("A retained customer-signed PDF, PNG or JPEG of at most 5 MB is required.");
        var type = content.AsSpan().StartsWith("%PDF-"u8) ? "application/pdf" :
            content.AsSpan().StartsWith(new byte[]{137,80,78,71,13,10,26,10}) ? "image/png" :
            content.AsSpan().StartsWith(new byte[]{255,216,255}) ? "image/jpeg" : "";
        var name=Path.GetFileName((request.Evidence.FileName ?? string.Empty).Replace('\\','/'));
        if(type.Length==0 || type!=request.Evidence.ContentType || string.IsNullOrWhiteSpace(name) || name.Length>255 || name.Any(char.IsControl))
            throw new StoresValidationException("Signature evidence filename or content type is invalid.");
        return Execute(id,"MachineDelivery.Sign",request.IdempotencyKey,
            new {request.DeliveredAt,request.CustomerSignatory,FileName=name,ContentType=type,
                ContentSha256=Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant()},content,ct);
    }
    private async Task<JsonElement> Execute(Guid? id,string operation,string key,object payload,byte[]? content,CancellationToken ct)
    {
        _=user.RequireRole("issue","STORES_ASSISTANT","STORES_EXECUTIVE","STORES_MANAGER");
        if(user.ResolvedRoleAssignmentType is not ("FULL" or "TEMPORARY")) throw new UnauthorizedAccessException("A substantive Stores assignment is required.");
        if(string.IsNullOrWhiteSpace(key) || key.Length>100) throw new StoresValidationException("IdempotencyKey is required, at most 100 characters.");
        try
        {
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
            var company=await Company(ct);
            var envelope=Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization,operation,key,new {Id=id,Payload=payload});
            var attempt=await Rev869BCommandContextAuthorizer.OpenForCreationAsync(db,user,Organization,envelope,"MachineDelivery",ct);
            using var replay=await Rev869BCommandContextAuthorizer.ReadCommittedReceiptAsync(db,attempt,ct);
            if(replay is not null) { var saved=replay.RootElement.GetProperty("Delivery").Clone(); await tx.CommitAsync(ct); return saved; }
            await using var command=await Command("SELECT advance.record_machine_delivery(@company,@command,@id,@operation,@payload,@content,@actor,@role,@assignment,@type,@login)::text",ct);
            command.Parameters.AddWithValue("company",company); command.Parameters.AddWithValue("command",attempt.CommandId);
            command.Parameters.Add("id",NpgsqlDbType.Uuid).Value=(object?)id ?? DBNull.Value;
            command.Parameters.AddWithValue("operation",operation); command.Parameters.AddWithValue("payload",NpgsqlDbType.Jsonb,JsonSerializer.Serialize(payload,Json));
            command.Parameters.Add("content",NpgsqlDbType.Bytea).Value=(object?)content ?? DBNull.Value;
            command.Parameters.AddWithValue("actor",user.EmployeeId!.Value); command.Parameters.AddWithValue("role",user.RoleCode);
            command.Parameters.AddWithValue("assignment",user.ResolvedRoleAssignmentId!.Value);
            command.Parameters.AddWithValue("type",user.ResolvedRoleAssignmentType!); command.Parameters.AddWithValue("login",user.LoginId);
            var value=(string?)await command.ExecuteScalarAsync(ct) ?? throw new InvalidOperationException("Delivery command returned no result.");
            using var json=JsonDocument.Parse(value); var result=json.RootElement.Clone();
            if(operation=="MachineDelivery.Dispatch" && result.GetProperty("Nature").GetString()=="NON_RETURNABLE") await NotifyDirector(company,result,ct);
            await audit.WriteAsync("Stores",operation,"MachineDelivery",result.GetProperty("Id").GetGuid().ToString(),null,payload,ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db,attempt,ct,new {Delivery=result});
            await tx.CommitAsync(ct); return result;
        }
        catch(Exception e) when(PostgreSqlConcurrency.IsSerializationFailure(e)) {throw new DbUpdateConcurrencyException("Machine delivery changed concurrently. Refresh and retry.",e);}
        catch(PostgresException e) when(e.SqlState==PostgresErrorCodes.InsufficientPrivilege) {throw new UnauthorizedAccessException(e.MessageText,e);}
        catch(PostgresException e) when(e.SqlState is PostgresErrorCodes.RaiseException or PostgresErrorCodes.CheckViolation or PostgresErrorCodes.UniqueViolation) {throw new StoresConflictException(e.MessageText);}
    }
    private async Task NotifyDirector(Guid company,JsonElement delivery,CancellationToken ct)
    {
        var today=DateOnly.FromDateTime(DateTime.UtcNow); var now=DateTimeOffset.UtcNow;
        var employees=await db.EmployeeRoleAssignments.AsNoTracking().Where(a=>a.CompanyId==company && a.Role!.Code=="MANAGING_DIRECTOR" && a.Role.IsActive &&
            (a.ApprovalStatus=="Approved" || a.ApprovalStatus=="SeedApproved") && a.EffectiveFrom<=today && (!a.EffectiveTo.HasValue || a.EffectiveTo>=today) &&
            a.Employee!.Status=="Active" && db.CompanyRoleActivations.Any(c=>c.CompanyId==company && c.RoleId==a.RoleId && c.IsEnabled && c.EffectiveFrom<=today && (!c.EffectiveTo.HasValue || c.EffectiveTo>=today)) &&
            db.EmployeeCompanyAssignments.Any(c=>c.CompanyId==company && c.EmployeeId==a.EmployeeId && c.IsActive && c.Status=="ACTIVE" && c.EffectiveFrom<=today && (!c.EffectiveTo.HasValue || c.EffectiveTo>=today)))
            .Select(a=>a.EmployeeId).Distinct().ToListAsync(ct);
        if(employees.Count==0) throw new StoresConflictException("NON_RETURNABLE dispatch requires an active Managing Director notification recipient.");
        var id=delivery.GetProperty("Id").GetGuid(); var number=delivery.GetProperty("DcNumber").GetString()!;
        db.NotificationEvents.Add(new NotificationEvent {CompanyId=company,EventType="MACHINE_NON_RETURNABLE_DISPATCH",SourceEntityType="MachineDelivery",SourceEntityId=id,
            SourceReferenceSnapshot=number,RecipientRoleCodes=["MANAGING_DIRECTOR"],TitleSnapshot="Machine dispatched",BodySnapshot=$"Machine DC {number} dispatched against customer PO.",
            DeepLinkSnapshot="/stores/machine-deliveries/"+id,PayloadJson=JsonSerializer.Serialize(new{DeliveryChallanId=id}),NotBeforeAt=now,Status="ACTIVE",IdempotencyKey="MACHINE-DC:"+id,CreatedAt=now,CreatedBy=user.LoginId,ActivatedAt=now,
            Recipients=employees.Select(employee=>new NotificationRecipient{CompanyId=company,RecipientEmployeeId=employee,ResolvedRoleCodes=["MANAGING_DIRECTOR"],ResolvedAt=now,InAppAvailableAt=now}).ToList()});
        await db.SaveChangesAsync(ct);
    }
    public async Task<SupplierInvoiceEvidenceInput?> SignatureAsync(Guid id,CancellationToken ct)
    {
        var company=await Company(ct);
        await using var command=await Command("SELECT * FROM advance.machine_delivery_signature_content(@company,@id)",ct);
        command.Parameters.AddWithValue("company",company); command.Parameters.AddWithValue("id",id);
        await using var reader=await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new(reader.GetString(0),reader.GetString(1),reader.GetFieldValue<byte[]>(2)) : null;
    }
    public async Task<JsonElement?> GetAsync(Guid id,CancellationToken ct)
    {
        var company=await Company(ct);
        await using var command=await Command("SELECT advance.machine_delivery_json(@company,@id)::text",ct);
        command.Parameters.AddWithValue("company",company); command.Parameters.AddWithValue("id",id);
        if(await command.ExecuteScalarAsync(ct) is not string result) return null;
        using var json=JsonDocument.Parse(result); return json.RootElement.Clone();
    }
}
