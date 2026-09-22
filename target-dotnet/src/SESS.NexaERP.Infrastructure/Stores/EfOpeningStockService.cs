using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfOpeningStockService(
    NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : IOpeningStockService
{
    private string Organization() => !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.OrganizationId.Trim().ToUpperInvariant()
        : throw new UnauthorizedAccessException("Company scope is required.");
    private Guid Actor() => user.EmployeeId
        ?? throw new UnauthorizedAccessException("Resolved employee identity is required.");
    private async Task<Company> CompanyAsync(CancellationToken ct) =>
        await db.Companies.SingleOrDefaultAsync(x => x.Code == Organization()
            && x.IsActive && x.Status == "ACTIVE", ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");

    public async Task<OpeningStockPage> ListAsync(
        string? status, int page, int pageSize, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var query = db.OpeningStocks.AsNoTracking().Where(x => x.CompanyId == company.Id);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status.Trim().ToUpperInvariant());
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.PeriodEnd).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.Id).ToListAsync(ct);
        var items = new List<OpeningStockView>(rows.Count);
        foreach (var id in rows) items.Add(await LoadAsync(id, false, ct));
        return new(total, page, pageSize, items);
    }

    public async Task<OpeningStockView?> GetAsync(Guid id, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        if (!await db.OpeningStocks.AsNoTracking().AnyAsync(
                x => x.CompanyId == company.Id && x.Id == id, ct)) return null;
        return await LoadAsync(id, false, ct);
    }

    public Task<OpeningStockView> RecordCountAsync(
        CreateOpeningStockFromImportRequest request, CancellationToken ct) =>
        WithConcurrencyHandlingAsync(() => RecordCountCoreAsync(request, ct));

    private async Task<OpeningStockView> RecordCountCoreAsync(
        CreateOpeningStockFromImportRequest request, CancellationToken ct)
    {
        var role = user.RequireRole("create", "STORES_MANAGER");
        ValidatePeriod(request.PeriodStart, request.PeriodEnd);
        var company = await CompanyAsync(ct);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
            Organization(), "OpeningStock.RecordCount", Required(request.IdempotencyKey, "IdempotencyKey"), request);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(
            db, user, Organization(), envelope, "opening_stock_events", nameof(OpeningStock),
            request.ImportBatchId, "COUNT", 0, null, OpeningStockStatuses.Counted,
            envelope.RequestFingerprint, Required(request.Reason, "Reason"), ct);
        var result = await Execute(
            @"SELECT ""OpeningStockId"",""Replayed"" FROM advance.record_opening_stock_count(@company,@batch,@from,@to,@reason,@key,@hash,@actor,@role,@assignment,@type,@login)",
            request.ImportBatchId, request.PeriodStart, request.PeriodEnd, 0,
            request.Reason, request.IdempotencyKey, envelope.RequestFingerprint, company.Id, role, ct);
        if (!result.Replayed)
            await audit.WriteAsync("Stores", "OpeningStock.RecordCount", nameof(OpeningStock),
                result.Id.ToString(), null, new { request.ImportBatchId, request.PeriodStart, request.PeriodEnd }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        return await LoadAsync(result.Id, result.Replayed, ct);
    }

    public Task<OpeningStockView> ConfirmValueAsync(
        Guid id, OpeningStockTransitionRequest request, CancellationToken ct) =>
        WithConcurrencyHandlingAsync(() => TransitionAsync(id, request, "OpeningStock.ConfirmValue", "VALUE",
            OpeningStockStatuses.Counted, OpeningStockStatuses.Valued, "ACCOUNTS_MANAGER", ct));

    public Task<OpeningStockView> AuthorizeAsync(
        Guid id, OpeningStockTransitionRequest request, CancellationToken ct) =>
        WithConcurrencyHandlingAsync(() => TransitionAsync(id, request, "OpeningStock.Authorize", "AUTHORIZE",
            OpeningStockStatuses.Valued, OpeningStockStatuses.Posted, "TECHNICAL_DIRECTOR", ct));

    private static async Task<OpeningStockView> WithConcurrencyHandlingAsync(Func<Task<OpeningStockView>> command)
    {
        try { return await command(); }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new DbUpdateConcurrencyException(
                "Opening Stock changed concurrently. Reload the ceremony before retrying.", error);
        }
    }

    private async Task<OpeningStockView> TransitionAsync(
        Guid id, OpeningStockTransitionRequest request, string operation, string action,
        string from, string to, string requiredRole, CancellationToken ct)
    {
        var role = user.RequireRole("approve", requiredRole);
        var company = await CompanyAsync(ct);
        var reason = Required(request.Reason, "Reason");
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
            Organization(), operation, Required(request.IdempotencyKey, "IdempotencyKey"),
            new { id, request });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(
            db, user, Organization(), envelope, "opening_stock_events", nameof(OpeningStock),
            id, action, request.Version, from, to, envelope.RequestFingerprint, reason, ct);
        var function = action == "VALUE"
            ? "advance.confirm_opening_stock_value"
            : "advance.authorize_opening_stock";
        var result = await Execute(
            $@"SELECT ""OpeningStockId"",""Replayed"" FROM {function}(@company,@id,@version,@reason,@key,@hash,@actor,@role,@assignment,@type,@login)",
            id, default, default, request.Version, reason, request.IdempotencyKey,
            envelope.RequestFingerprint, company.Id, role, ct);
        if (!result.Replayed)
            await audit.WriteAsync("Stores", operation, nameof(OpeningStock), id.ToString(),
                new { Status = from, request.Version }, new { Status = to, Version = request.Version + 1 }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        return await LoadAsync(id, result.Replayed, ct);
    }

    private async Task<(Guid Id, bool Replayed)> Execute(
        string sql, Guid sourceId, DateOnly from, DateOnly to, long version,
        string reason, string key, string hash, Guid companyId, string role, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = new NpgsqlCommand(sql, connection,
            (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        command.Parameters.AddWithValue("company", companyId);
        if (sql.Contains("@batch", StringComparison.Ordinal))
        {
            command.Parameters.AddWithValue("batch", sourceId);
            command.Parameters.AddWithValue("from", from);
            command.Parameters.AddWithValue("to", to);
        }
        else
        {
            command.Parameters.AddWithValue("id", sourceId);
            command.Parameters.AddWithValue("version", version);
        }
        command.Parameters.AddWithValue("reason", reason.Trim());
        command.Parameters.AddWithValue("key", key.Trim());
        command.Parameters.AddWithValue("hash", hash);
        command.Parameters.AddWithValue("actor", Actor());
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("assignment", user.ResolvedRoleAssignmentId!.Value);
        command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!);
        command.Parameters.AddWithValue("login", user.LoginId);
        try
        {
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                throw new StoresConflictException("Controlled opening-stock command returned no result.");
            return (reader.GetGuid(0), reader.GetBoolean(1));
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        {
            throw new UnauthorizedAccessException("Opening-stock authority was refused.", e);
        }
        catch (PostgresException e) when (e.SqlState is "23505" or "23514" or "P0001")
        {
            throw new StoresConflictException(e.MessageText);
        }
    }

    private async Task<OpeningStockView> LoadAsync(Guid id, bool replayed, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var x = await db.OpeningStocks.AsNoTracking()
            .Include(o => o.CountedByEmployee).Include(o => o.ValuedByEmployee)
            .Include(o => o.AuthorizedByEmployee)
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .Include(o => o.Lines).ThenInclude(l => l.Warehouse)
            .Include(o => o.Lines).ThenInclude(l => l.RackBin)
            .SingleAsync(o => o.CompanyId == company.Id && o.Id == id, ct);
        OpeningStockActorView? ActorView(Guid? employeeId, string? roleCode,
            Guid? assignmentId, string? type, DateTimeOffset? at, string? reason,
            SESS.NexaERP.Domain.Employees.Employee? employee) =>
            employeeId.HasValue && assignmentId.HasValue && at.HasValue && employee is not null
                ? new(employeeId.Value, employee.EmployeeCode, employee.EmployeeName,
                    roleCode!, assignmentId.Value, type!, at.Value, reason!)
                : null;
        var counted = ActorView(x.CountedByEmployeeId, x.CountActorRoleCode,
            x.CountRoleAssignmentId, x.CountRoleAssignmentType, x.CountedAt,
            x.CountReason, x.CountedByEmployee)!;
        return new(x.Id, x.ImportBatchId, x.PeriodStart, x.PeriodEnd, x.Status,
            x.Version, x.Lines.Sum(l => l.Quantity), x.Lines.Sum(l => l.LineValue),
            counted, ActorView(x.ValuedByEmployeeId, x.ValueActorRoleCode,
                x.ValueRoleAssignmentId, x.ValueRoleAssignmentType, x.ValuedAt,
                x.ValueReason, x.ValuedByEmployee),
            ActorView(x.AuthorizedByEmployeeId, x.AuthorizationActorRoleCode,
                x.AuthorizationRoleAssignmentId, x.AuthorizationRoleAssignmentType,
                x.AuthorizedAt, x.AuthorizationReason, x.AuthorizedByEmployee),
            x.StockPostingBatchId, replayed,
            x.Lines.OrderBy(l => l.LineNumber).Select(l => new OpeningStockLineView(
                l.Id, l.LineNumber, l.LineReference, l.ItemId, l.Item!.ItemCode,
                l.Item.Name, l.WarehouseId, l.Warehouse!.WarehouseCode, l.RackBinId,
                l.RackBin!.BinCode, l.WarehouseConditionLocationId, l.LotNumber,
                l.SerialNumber, l.Quantity, l.UnitRate, l.LineValue, l.InventoryLotId,
                l.InventorySerialId, l.FifoInventoryCostLayerId, l.VendorName, l.VendorBillNumber, l.BillDate, l.PurchaseDate,
                l.Make, l.Model, l.PartNumber, l.Remarks)).ToArray());
    }

    private static void ValidatePeriod(DateOnly from, DateOnly to)
    {
        if (from == default || to == default || from > to)
            throw new StoresValidationException("A valid opening-stock period is required.");
    }
    private static string Required(string? value, string name) =>
        !string.IsNullOrWhiteSpace(value) ? value.Trim()
        : throw new StoresValidationException($"{name} is required.");
}
