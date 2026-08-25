using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed class DatabaseRuntimePrincipalGuard(
    NexaErpDbContext db,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<DatabaseRuntimePrincipalGuard> logger)
{
    public const string DevelopmentExemptionSetting = "DatabaseSecurity:AllowDevelopmentSuperuser";
    private const string ExpectedRuntimePrincipal = "nexa_erp_runtime";

    private const string EvidenceSql = """
        SELECT session_user,
               current_user,
               role.rolsuper,
               role.rolcreatedb,
               role.rolcreaterole,
               role.rolreplication,
               role.rolbypassrls,
               session_user = pg_catalog.pg_get_userbyid(database.datdba),
               COALESCE(session_user = pg_catalog.pg_get_userbyid(namespace.nspowner), false),
               COALESCE((
                   SELECT pg_catalog.pg_has_role(session_user, owner.oid, 'MEMBER')
                   FROM pg_catalog.pg_roles owner
                   WHERE owner.rolname = 'nexa_erp_owner'
               ), false)
        FROM pg_catalog.pg_roles role
        JOIN pg_catalog.pg_database database ON database.datname = current_database()
        LEFT JOIN pg_catalog.pg_namespace namespace ON namespace.nspname = 'advance'
        WHERE role.rolname = session_user;
        """;

    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        var configuredValue = configuration[DevelopmentExemptionSetting];
        var settingIsPresent = configuredValue is not null;

#if !DEBUG
        if (settingIsPresent)
            throw new InvalidOperationException(
                $"{DevelopmentExemptionSetting} must not be present in a Release build.");
#endif

        var allowDevelopmentSuperuser = false;
        if (settingIsPresent && !bool.TryParse(configuredValue, out allowDevelopmentSuperuser))
            throw new InvalidOperationException($"{DevelopmentExemptionSetting} must be true or false.");
        if (allowDevelopmentSuperuser && !environment.IsDevelopment())
            throw new InvalidOperationException(
                $"{DevelopmentExemptionSetting} can be enabled only in the Development environment.");
        if (allowDevelopmentSuperuser)
            logger.LogCritical(
                "SECURITY WARNING: {Setting} is active. This Development startup may use a PostgreSQL superuser and must never be deployed.",
                DevelopmentExemptionSetting);

        var evidence = await ReadEvidenceAsync(cancellationToken);
        if (evidence.IsStrictRuntimePrincipal)
            return;

        if (allowDevelopmentSuperuser && evidence.IsSuperuser)
        {
            logger.LogCritical(
                "SECURITY WARNING: API startup accepted PostgreSQL superuser {DatabasePrincipal} under the Development-only exemption.",
                evidence.SessionUser);
            return;
        }

        throw new InvalidOperationException(
            $"Database runtime-principal guard refused startup. Session principal '{evidence.SessionUser}' must be '{ExpectedRuntimePrincipal}', must remain current_user, and must not be privileged, an owner, or an owner-role member.");
    }

    private async Task<PrincipalEvidence> ReadEvidenceAsync(CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = EvidenceSql;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("Database runtime-principal guard returned no role evidence.");

            return new PrincipalEvidence(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetBoolean(2),
                reader.GetBoolean(3),
                reader.GetBoolean(4),
                reader.GetBoolean(5),
                reader.GetBoolean(6),
                reader.GetBoolean(7),
                reader.GetBoolean(8),
                reader.GetBoolean(9));
        }
        finally
        {
            if (openedHere)
                await db.Database.CloseConnectionAsync();
        }
    }

    private sealed record PrincipalEvidence(
        string SessionUser,
        string CurrentUser,
        bool IsSuperuser,
        bool CanCreateDatabase,
        bool CanCreateRole,
        bool CanReplicate,
        bool CanBypassRowLevelSecurity,
        bool IsDatabaseOwner,
        bool IsAdvanceSchemaOwner,
        bool IsOwnerRoleMember)
    {
        public bool IsStrictRuntimePrincipal =>
            string.Equals(SessionUser, ExpectedRuntimePrincipal, StringComparison.Ordinal) &&
            string.Equals(CurrentUser, ExpectedRuntimePrincipal, StringComparison.Ordinal) &&
            !IsSuperuser && !CanCreateDatabase && !CanCreateRole && !CanReplicate &&
            !CanBypassRowLevelSecurity && !IsDatabaseOwner && !IsAdvanceSchemaOwner && !IsOwnerRoleMember;
    }
}
