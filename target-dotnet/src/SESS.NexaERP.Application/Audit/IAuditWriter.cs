namespace SESS.NexaERP.Application.Audit;

public interface IAuditWriter
{
    Task WriteAsync(string module, string action, string entityName, string entityId, object? before, object? after, CancellationToken cancellationToken);
}
