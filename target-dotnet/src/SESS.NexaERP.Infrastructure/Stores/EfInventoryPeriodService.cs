using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfInventoryPeriodService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit)
    : IInventoryPeriodService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string Organization => !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.OrganizationId.Trim().ToUpperInvariant() : throw new UnauthorizedAccessException("Select a company.");
    private void RequireRole(string action) => user.RequireRole(action, "CHIEF_FINANCIAL_OFFICER");
    private async Task<Guid> Company(CancellationToken ct) =>
        await db.Companies.Where(x => x.Code == Organization && x.IsActive && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
    private static string Required(string? value, string name, int maximum)
    {
        var text = value?.Trim() ?? "";
        return text.Length > 0 && text.Length <= maximum && !text.Any(char.IsControl)
            ? text : throw new StoresValidationException($"{name} requires 1 to {maximum} characters without control characters.");
    }
    private async Task<NpgsqlCommand> Command(string sql, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        return new(sql, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
    }
    public async Task<InventoryPeriodView> OpenAsync(OpenInventoryPeriodRequest request, CancellationToken ct)
    {
        RequireRole("approve");
        var code = Required(request.Code, "Code", 30).ToUpperInvariant();
        var name = Required(request.Name, "Name", 150);
        var reason = Required(request.Reason, "Reason", 2000);
        var key = Required(request.IdempotencyKey, "IdempotencyKey", 100);
        if (request.StartDate == default || request.EndDate < request.StartDate)
            throw new StoresValidationException("A valid inclusive inventory period is required.");
        return await Decide("InventoryPeriod.Open", key, new { code, name, request.StartDate, request.EndDate, reason },
            "SELECT advance.open_inventory_period(@company,@command,@code,@name,@from,@to,@reason,@actor,@role,@assignment,@type,@login)",
            command => {
                command.Parameters.AddWithValue("code", code); command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("from", request.StartDate); command.Parameters.AddWithValue("to", request.EndDate);
            }, reason, ct);
    }
    public async Task<InventoryPeriodView> CloseAsync(Guid id, CloseInventoryPeriodRequest request, CancellationToken ct)
    {
        RequireRole("approve");
        if (id == Guid.Empty || request.Version < 0) throw new StoresValidationException("Period identity and current version are required.");
        var reason = Required(request.Reason, "Reason", 2000);
        var key = Required(request.IdempotencyKey, "IdempotencyKey", 100);
        return await Decide("InventoryPeriod.Close", key, new { id, request.Version, reason },
            "SELECT advance.close_inventory_period(@company,@command,@period,@version,@reason,@actor,@role,@assignment,@type,@login)",
            command => {
                command.Parameters.AddWithValue("period", id); command.Parameters.AddWithValue("version", request.Version);
            }, reason, ct);
    }
    private async Task<InventoryPeriodView> Decide(string operation, string key, object fingerprintPayload,
        string sql, Action<NpgsqlCommand> parameters, string reason, CancellationToken ct)
    {
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var company = await Company(ct);
            var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization, operation, key, fingerprintPayload);
            var attempt = await Rev869BCommandContextAuthorizer.OpenForCreationAsync(db, user, Organization, envelope, "InventoryPeriod", ct);
            using var replay = await Rev869BCommandContextAuthorizer.ReadCommittedReceiptAsync(db, attempt, ct);
            if (replay is not null)
            {
                var retained = replay.RootElement.GetProperty("Period").Deserialize<InventoryPeriodView>(Json)!;
                await tx.CommitAsync(ct);
                return retained with { Replayed = true };
            }
            await using var command = await Command(sql, ct);
            command.Parameters.AddWithValue("company", company); command.Parameters.AddWithValue("command", attempt.CommandId);
            command.Parameters.AddWithValue("reason", reason);
            command.Parameters.AddWithValue("actor", user.EmployeeId!.Value); command.Parameters.AddWithValue("role", user.RoleCode);
            command.Parameters.AddWithValue("assignment", user.ResolvedRoleAssignmentId!.Value);
            command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!);
            command.Parameters.AddWithValue("login", user.LoginId);
            parameters(command);
            var id = (Guid)(await command.ExecuteScalarAsync(ct)
                ?? throw new InvalidOperationException("Period command returned no identity."));
            var result = await Read(company, id, ct) ?? throw new InvalidOperationException("Retained period is unavailable.");
            await audit.WriteAsync("Accounts", operation, "InventoryPeriod", id.ToString(), null,
                new { result.Code, result.Status, result.Version, reason }, ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Period = result });
            await tx.CommitAsync(ct);
            return result;
        }
        catch (Exception error) when (PostgreSqlConcurrency.IsSerializationFailure(error))
        { throw new DbUpdateConcurrencyException("Inventory period changed concurrently. Refresh and retry.", error); }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        { throw new UnauthorizedAccessException(error.MessageText, error); }
        catch (PostgresException error) when (error.SqlState is PostgresErrorCodes.RaiseException or PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation or PostgresErrorCodes.ForeignKeyViolation)
        { throw new StoresConflictException(error.MessageText); }
    }
    public async Task<IReadOnlyList<InventoryPeriodView>> ListAsync(CancellationToken ct)
    {
        RequireRole("view");
        await using var command = await Command("SELECT advance.inventory_periods_json(@company)::text", ct);
        command.Parameters.AddWithValue("company", await Company(ct));
        var result = (string)(await command.ExecuteScalarAsync(ct))!;
        return JsonSerializer.Deserialize<List<InventoryPeriodView>>(result, Json)!;
    }
    public async Task<InventoryPeriodView?> GetAsync(Guid id, CancellationToken ct)
    {
        RequireRole("view");
        return await Read(await Company(ct), id, ct);
    }
    private async Task<InventoryPeriodView?> Read(Guid company, Guid id, CancellationToken ct)
    {
        await using var command = await Command("SELECT advance.inventory_period_json(@company,@id)::text", ct);
        command.Parameters.AddWithValue("company", company); command.Parameters.AddWithValue("id", id);
        return await command.ExecuteScalarAsync(ct) is string value
            ? JsonSerializer.Deserialize<InventoryPeriodView>(value, Json) : null;
    }
}
