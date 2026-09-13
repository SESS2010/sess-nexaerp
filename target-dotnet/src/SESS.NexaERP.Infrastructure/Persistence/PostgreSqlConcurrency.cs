using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SESS.NexaERP.Infrastructure.Persistence;

internal static class PostgreSqlConcurrency
{
    internal static bool IsSerializationFailure(Exception error)
    {
        // EF/Npgsql can wrap a save failure in both of these exception types.
        // Match the SQLSTATE rather than the general transient-failure label.
        while (error is DbUpdateException or InvalidOperationException)
        {
            if (error.InnerException is null) return false;
            error = error.InnerException;
        }
        return error is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure };
    }
}