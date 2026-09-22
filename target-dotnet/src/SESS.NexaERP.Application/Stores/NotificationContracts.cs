using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Application.Stores;

public sealed record InAppNotificationView(
    Guid RecipientId,
    Guid EventId,
    string EventType,
    string SourceEntityType,
    Guid SourceEntityId,
    string SourceReference,
    string Title,
    string Body,
    string DeepLink,
    string Status,
    DateTimeOffset AvailableAt,
    DateTimeOffset? ReadAt);

public interface IInAppNotificationService
{
    Task<PagedResponse<InAppNotificationView>> ListAsync(bool unreadOnly, int page, int pageSize, CancellationToken ct);
    Task<int> UnreadCountAsync(CancellationToken ct);
    Task MarkReadAsync(Guid recipientId, string correlationId, CancellationToken ct);
}

public interface INotificationDueEventProcessor
{
    Task<int> RefreshAsync(DateTimeOffset now, CancellationToken ct);
}
