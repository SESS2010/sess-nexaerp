using System.Text.Json;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Audit;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Audit;

public sealed class EfAuditWriter(NexaErpDbContext dbContext, ICurrentUser currentUser) : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(string module, string action, string entityName, string entityId, object? before, object? after, CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            Module = module,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            UserLoginId = currentUser.LoginId,
            Result = string.Equals(action, "Denied", StringComparison.OrdinalIgnoreCase) ? "Failure" : "Success",
            CorrelationId = Guid.NewGuid().ToString("N"),
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            CreatedBy = currentUser.LoginId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
