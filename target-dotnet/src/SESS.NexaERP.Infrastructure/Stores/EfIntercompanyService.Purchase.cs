using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfIntercompanyService
{
    public async Task<JsonElement> PurchaseOptionsAsync(CancellationToken ct)
    {
        RequireRole("issue", "PURCHASE_MANAGER");
        var company = await Company(ct);
        await using var command = await Command("SELECT advance.intercompany_purchase_options(@company,@actor)::text", ct);
        command.Parameters.AddWithValue("company", company);
        command.Parameters.AddWithValue("actor", user.EmployeeId!.Value);
        using var json = JsonDocument.Parse((string)(await command.ExecuteScalarAsync(ct))!);
        return json.RootElement.Clone();
    }

    public async Task<IntercompanyPurchaseView?> PurchaseAsync(Guid correlationId, CancellationToken ct)
    {
        RequireRole("view", "PURCHASE_MANAGER", "ACCOUNTS_MANAGER");
        var company = await Company(ct);
        await using var command = await Command("SELECT advance.intercompany_purchase_json(@company,@id,@actor,@role)::text", ct);
        command.Parameters.AddWithValue("company", company);
        command.Parameters.AddWithValue("id", correlationId);
        command.Parameters.AddWithValue("actor", user.EmployeeId!.Value);
        command.Parameters.AddWithValue("role", user.RoleCode);
        return await command.ExecuteScalarAsync(ct) is string value
            ? JsonSerializer.Deserialize<IntercompanyPurchaseView>(value, Json) : null;
    }

    public async Task<PagedResponse<IntercompanyPurchaseView>> PurchasesAsync(int? page, int? pageSize, CancellationToken ct)
    {
        RequireRole("view", "PURCHASE_MANAGER", "ACCOUNTS_MANAGER");
        var size = Math.Clamp(pageSize ?? 50, 1, 200);
        var number = Math.Clamp(page ?? 1, 1, int.MaxValue / size);
        var company = await Company(ct);
        await using var command = await Command("SELECT advance.intercompany_purchases_page(@company,@actor,@role,@offset,@limit)::text", ct);
        command.Parameters.AddWithValue("company", company);
        command.Parameters.AddWithValue("actor", user.EmployeeId!.Value);
        command.Parameters.AddWithValue("role", user.RoleCode);
        command.Parameters.AddWithValue("offset", (number - 1) * size);
        command.Parameters.AddWithValue("limit", size);
        using var json = JsonDocument.Parse((string)(await command.ExecuteScalarAsync(ct))!);
        return new(json.RootElement.GetProperty("totalCount").GetInt32(), number, size,
            json.RootElement.GetProperty("items").Deserialize<List<IntercompanyPurchaseView>>(Json)!);
    }

    public async Task<IntercompanyPurchaseView> PublishPurchaseAsync(PublishIntercompanyPurchaseRequest request, CancellationToken ct)
    {
        RequireRole("issue", "PURCHASE_MANAGER");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 100)
            throw new StoresValidationException("IdempotencyKey is required, at most 100 characters.");
        if (string.IsNullOrWhiteSpace(request.Remarks) || request.Remarks.Length > 2000)
            throw new StoresValidationException("Remarks are required, at most 2000 characters.");
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var company = await Company(ct);
            var po = await db.PurchaseOrders.AsNoTracking().Include(p => p.Lines)
                .SingleOrDefaultAsync(p => p.CompanyId == company && p.Id == request.PurchaseOrderId, ct)
                ?? throw new KeyNotFoundException("Purchase order was not found in the selected company.");
            var scope = await scopes.AuthorizeAsync(user.EmployeeId!.Value, user.RoleCode,
                new RecordScopeTarget(Organization, po.RequestingDepartmentId, po.DeliveryWarehouseId, null, po.OwnerEmployeeId),
                DateOnly.FromDateTime(DateTime.UtcNow), ct);
            if (!scope.Allowed) throw new UnauthorizedAccessException(scope.Reason);
            var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization, "IntercompanyPurchase.Publish", request.IdempotencyKey, request);
            var attempt = await Rev869BCommandContextAuthorizer.OpenForCreationAsync(db, user, Organization, envelope, "IntercompanyPurchase", ct);
            using var replay = await Rev869BCommandContextAuthorizer.ReadCommittedReceiptAsync(db, attempt, ct);
            if (replay is not null)
            {
                var prior = replay.RootElement.GetProperty("Purchase").Deserialize<IntercompanyPurchaseView>(Json)!;
                await tx.CommitAsync(ct);
                return prior;
            }
            if (po.Version != request.Version || !po.IsCurrentVersion || po.Status != Rev869BStatuses.Issued)
                throw new StoresConflictException("Intercompany publication requires the current issued PO and its current Version.");
            try { Rev869BPurchaseOrderSnapshot.RequireComplete(po, requireApproved: false); }
            catch (InvalidOperationException error) { throw new StoresConflictException(error.Message); }
            await using var command = await Command(
                "SELECT advance.publish_intercompany_purchase(@company,@command,@route,@po,@version,@remarks,@actor,@role,@assignment,@type,@login)::text", ct);
            command.Parameters.AddWithValue("company", company);
            command.Parameters.AddWithValue("command", attempt.CommandId);
            command.Parameters.AddWithValue("route", request.RouteId);
            command.Parameters.AddWithValue("po", request.PurchaseOrderId);
            command.Parameters.AddWithValue("version", (long)request.Version);
            command.Parameters.AddWithValue("remarks", request.Remarks.Trim());
            command.Parameters.AddWithValue("actor", user.EmployeeId!.Value);
            command.Parameters.AddWithValue("role", user.RoleCode);
            command.Parameters.AddWithValue("assignment", user.ResolvedRoleAssignmentId!.Value);
            command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!);
            command.Parameters.AddWithValue("login", user.LoginId);
            var value = (string?)await command.ExecuteScalarAsync(ct)
                ?? throw new InvalidOperationException("Intercompany publication returned no result.");
            var result = JsonSerializer.Deserialize<IntercompanyPurchaseView>(value, Json)!;
            await audit.WriteAsync("Purchase", "IntercompanyPurchase.Publish", "IntercompanyPurchase", result.CorrelationId.ToString(), null, request, ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Purchase = result });
            await tx.CommitAsync(ct);
            return result;
        }
        catch (Exception error) when (PostgreSqlConcurrency.IsSerializationFailure(error))
        { throw new DbUpdateConcurrencyException("Intercompany purchase changed concurrently. Refresh and retry.", error); }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        { throw new UnauthorizedAccessException(error.MessageText, error); }
        catch (PostgresException error) when (error.SqlState is PostgresErrorCodes.RaiseException or PostgresErrorCodes.CheckViolation or PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation)
        { throw new StoresConflictException(error.MessageText); }
    }
}
