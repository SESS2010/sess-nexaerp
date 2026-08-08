using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Infrastructure;

public sealed class SystemClock : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
