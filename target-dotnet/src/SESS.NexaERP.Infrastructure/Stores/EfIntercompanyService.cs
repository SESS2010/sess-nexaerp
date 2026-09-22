using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfIntercompanyService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, IRecordScopeAuthorizer scopes) : IIntercompanyService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string Organization => user.OrganizationId?.Trim().ToUpperInvariant()
        ?? throw new UnauthorizedAccessException("Select a company.");

    private async Task<Guid> Company(CancellationToken ct) =>
        await db.Companies.Where(c => c.Code == Organization && c.IsActive && c.Status == "ACTIVE")
            .Select(c => (Guid?)c.Id).SingleOrDefaultAsync(ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");

    private void RequireRole(string action, params string[] roles)
    {
        _ = user.RequireRole(action, roles);
    }

    private async Task<NpgsqlCommand> Command(string sql, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        return new(sql, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
    }

    public async Task<JsonElement> RouteOptionsAsync(CancellationToken ct)
    {
        RequireRole("view", "ACCOUNTS_MANAGER", "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR");
        var company = await Company(ct);
        await using var command = await Command("SELECT advance.intercompany_route_options(@company)::text", ct);
        command.Parameters.AddWithValue("company", company);
        using var json = JsonDocument.Parse((string)(await command.ExecuteScalarAsync(ct))!);
        return json.RootElement.Clone();
    }

    public async Task<PagedResponse<IntercompanyRouteView>> RoutesAsync(int? page, int? pageSize, CancellationToken ct)
    {
        RequireRole("view", "ACCOUNTS_MANAGER", "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR");
        var size = Math.Clamp(pageSize ?? 50, 1, 200);
        var number = Math.Clamp(page ?? 1, 1, int.MaxValue / size);
        var company = await Company(ct);
        await using var command = await Command("SELECT advance.intercompany_routes_page(@company,@offset,@limit)::text", ct);
        command.Parameters.AddWithValue("company", company);
        command.Parameters.AddWithValue("offset", (number - 1) * size);
        command.Parameters.AddWithValue("limit", size);
        using var json = JsonDocument.Parse((string)(await command.ExecuteScalarAsync(ct))!);
        return new(json.RootElement.GetProperty("totalCount").GetInt32(), number, size,
            json.RootElement.GetProperty("items").Deserialize<List<IntercompanyRouteView>>(Json)!);
    }

    public async Task<IntercompanyRouteView?> RouteAsync(Guid id, CancellationToken ct)
    {
        RequireRole("view", "ACCOUNTS_MANAGER", "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR");
        var company = await Company(ct);
        await using var command = await Command("SELECT advance.intercompany_route_json(@company,@id)::text", ct);
        command.Parameters.AddWithValue("company", company);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteScalarAsync(ct) is string value
            ? JsonSerializer.Deserialize<IntercompanyRouteView>(value, Json) : null;
    }

    public Task<IntercompanyRouteView> ProposeRouteAsync(ProposeIntercompanyRouteRequest request, CancellationToken ct)
    {
        RequireRole("create", "ACCOUNTS_MANAGER");
        if (string.IsNullOrWhiteSpace(request.RouteCode) || request.RouteCode.Length > 50)
            throw new StoresValidationException("RouteCode is required, at most 50 characters.");
        if (request.EffectiveTo < request.EffectiveFrom)
            throw new StoresValidationException("Route effective end precedes its start.");
        return Execute(null, "IntercompanyRoute.Propose", request.IdempotencyKey, request.Remarks, request, ct);
    }

    public Task<IntercompanyRouteView> DecideRouteAsync(Guid id, string decision, DecideIntercompanyRouteRequest request, CancellationToken ct)
    {
        var action = decision switch
        {
            "APPROVED" => "approve",
            "REJECTED" => "reject",
            "REVOKED" => "deactivate",
            _ => throw new StoresValidationException("Decision must be APPROVED, REJECTED or REVOKED.")
        };
        RequireRole(action, "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR");
        return Execute(id, "IntercompanyRoute.Decide", request.IdempotencyKey, request.Remarks, new { request.Version, Decision = decision, request.Remarks }, ct);
    }

    private async Task<IntercompanyRouteView> Execute(Guid? id, string operation, string key, string remarks, object payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100)
            throw new StoresValidationException("IdempotencyKey is required, at most 100 characters.");
        if (string.IsNullOrWhiteSpace(remarks) || remarks.Length > 2000)
            throw new StoresValidationException("Remarks are required, at most 2000 characters.");
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var company = await Company(ct);
            var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization, operation, key, new { Id = id, Payload = payload });
            var attempt = await Rev869BCommandContextAuthorizer.OpenForCreationAsync(db, user, Organization, envelope, "IntercompanyRoute", ct);
            using var replay = await Rev869BCommandContextAuthorizer.ReadCommittedReceiptAsync(db, attempt, ct);
            if (replay is not null)
            {
                var prior = replay.RootElement.GetProperty("Route").Deserialize<IntercompanyRouteView>(Json)!;
                await tx.CommitAsync(ct);
                return prior;
            }
            await using var command = await Command(
                "SELECT advance.record_intercompany_route(@company,@command,@id,@operation,@payload,@actor,@role,@assignment,@type,@login)::text", ct);
            command.Parameters.AddWithValue("company", company);
            command.Parameters.AddWithValue("command", attempt.CommandId);
            command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = (object?)id ?? DBNull.Value;
            command.Parameters.AddWithValue("operation", operation);
            command.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(payload, Json));
            command.Parameters.AddWithValue("actor", user.EmployeeId!.Value);
            command.Parameters.AddWithValue("role", user.RoleCode);
            command.Parameters.AddWithValue("assignment", user.ResolvedRoleAssignmentId!.Value);
            command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!);
            command.Parameters.AddWithValue("login", user.LoginId);
            var value = (string?)await command.ExecuteScalarAsync(ct)
                ?? throw new InvalidOperationException("Intercompany route command returned no result.");
            var result = JsonSerializer.Deserialize<IntercompanyRouteView>(value, Json)!;
            await audit.WriteAsync("Stores", operation, "IntercompanyRoute", result.Id.ToString(), null, payload, ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Route = result });
            await tx.CommitAsync(ct);
            return result;
        }
        catch (Exception error) when (PostgreSqlConcurrency.IsSerializationFailure(error))
        { throw new DbUpdateConcurrencyException("Intercompany route changed concurrently. Refresh and retry.", error); }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        { throw new UnauthorizedAccessException(error.MessageText, error); }
        catch (PostgresException error) when (error.SqlState is PostgresErrorCodes.RaiseException or PostgresErrorCodes.CheckViolation or PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation)
        { throw new StoresConflictException(error.MessageText); }
    }
}
