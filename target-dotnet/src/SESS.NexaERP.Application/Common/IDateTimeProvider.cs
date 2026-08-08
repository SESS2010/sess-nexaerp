namespace SESS.NexaERP.Application.Common;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
