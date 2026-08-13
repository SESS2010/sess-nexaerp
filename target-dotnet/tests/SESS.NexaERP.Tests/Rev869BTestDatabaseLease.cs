using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;

namespace SESS.NexaERP.Tests;

/// <summary>
/// Source-only design for a disposable REV869B database lease. Merely discovering/listing tests
/// never calls this type. Provisioning is opt-in and every destructive boundary re-proves the
/// source/target identity, retained migration and an unguessable per-run ownership marker.
/// A failed proof quarantines the database by refusing DROP; it never attempts name-only repair.
/// </summary>
internal sealed class Rev869BTestDatabaseLease : IAsyncDisposable
{
    internal const string ExactSourceDatabase = "sess_nexaerp_rev869b_verify";
    internal const string ExactOptIn = "ISOLATED_REV869B_BEHAVIOR_TESTS";
    internal const string MigrationId = "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation";
    internal const string DatabasePrefix = "sess_nexaerp_rev869b_owned_";
    private const string MarkerTable = "rev869b_test_database_lease";
    private const string ApprovedRetentionPolicy = "MGMT-REV869B-SECURITY-LEDGER-20260813-001";
    private const string RecoveryPurpose = "REV869B_QUARANTINE_DROP_V1";
    private readonly string adminConnectionString;
    private readonly string ownerConnectionString;
    private readonly string issuerConnectionString;
    private readonly string runtimeRole;
    private readonly string issuerRole;
    private readonly string? previousIssuerConnection;
    private readonly string scenarioHash;
    private readonly string sourceFingerprint;
    private readonly string migrationFingerprint;
    private readonly Rev869BControlPlaneRegistry.LeaseReservation reservation;
    private readonly SemaphoreSlim disposalGate = new(1, 1);
    private string expectedOwner = string.Empty;
    private DateTimeOffset provisionedAt;
    private string markerFingerprint = string.Empty;
    private bool disposed;

    private Rev869BTestDatabaseLease(
        string connectionString,
        string ownerConnectionString,
        string issuerConnectionString,
        string adminConnectionString,
        string databaseName,
        string runId,
        string ownershipToken,
        string runtimeRole,
        string issuerRole,
        string? previousIssuerConnection,
        string family,
        string scenarioHash,
        string sourceFingerprint,
        string migrationFingerprint,
        Rev869BControlPlaneRegistry.LeaseReservation reservation)
    {
        ConnectionString = connectionString;
        this.ownerConnectionString = ownerConnectionString;
        this.issuerConnectionString = issuerConnectionString;
        this.adminConnectionString = adminConnectionString;
        DatabaseName = databaseName;
        RunId = runId;
        OwnershipToken = ownershipToken;
        this.runtimeRole = runtimeRole;
        this.issuerRole = issuerRole;
        this.previousIssuerConnection = previousIssuerConnection;
        Family = family;
        this.scenarioHash = scenarioHash;
        this.sourceFingerprint = sourceFingerprint;
        this.migrationFingerprint = migrationFingerprint;
        this.reservation = reservation;
    }

    internal string ConnectionString { get; }
    internal string OwnerConnectionString => ownerConnectionString;
    internal string DatabaseName { get; }
    internal string RunId { get; }
    internal string OwnershipToken { get; }
    internal string Family { get; }
    internal Rev869BControlPlaneRegistry.LeaseReservation ControlPlaneLease => reservation;
    internal string MarkerFingerprint => markerFingerprint;

    internal static async Task<Rev869BTestDatabaseLease> CreateAsync(string scenario, string family)
    {
        RequireApprovedRetentionConfiguration();
        if (!string.Equals(Environment.GetEnvironmentVariable("REV869B_POSTGRES_OPT_IN"), ExactOptIn, StringComparison.Ordinal))
            throw new InvalidOperationException($"Set REV869B_POSTGRES_OPT_IN={ExactOptIn} explicitly.");
        var raw = Environment.GetEnvironmentVariable("REV869B_POSTGRES")
            ?? throw new InvalidOperationException("REV869B_POSTGRES is required; no fallback is permitted.");
        var source = new NpgsqlConnectionStringBuilder(raw) { Pooling = false };
        if (!string.Equals(source.Database, ExactSourceDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException($"Only the exact isolated source database {ExactSourceDatabase} is permitted.");
        var sourceProof = await VerifySourceAsync(source.ConnectionString);

        var runId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var ownershipToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var scenarioHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scenario)));
        var runtimeRole = "rev869b_rt_" + runId[..16];
        var issuerRole = "rev869b_iss_" + runId[..16];
        var runtimePassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var issuerPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var previousIssuerConnection = Environment.GetEnvironmentVariable("REV869B_COMMAND_ISSUER_CONNECTION");
        var databaseName = DatabasePrefix + runId[..24];
        RequireSafeOwnedName(databaseName);
        var intentAt = DateTimeOffset.UtcNow;
        var ownershipTokenHash = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(ownershipToken)));
        var controlPlaneIssuer = Environment.GetEnvironmentVariable("REV869B_CONTROL_PLANE_REQUEST_ISSUER")
            ?? throw new InvalidOperationException("REV869B_CONTROL_PLANE_REQUEST_ISSUER is required.");
        var controlPlaneAuthority = Environment.GetEnvironmentVariable("REV869B_CONTROL_PLANE_ISSUER_AUTHORITY")
            ?? throw new InvalidOperationException("REV869B_CONTROL_PLANE_ISSUER_AUTHORITY is required.");
        var sourceCommitFingerprint = ResolveAuthoritativeSourceCommitFingerprint();
        var reservation = new Rev869BControlPlaneRegistry.LeaseReservation(databaseName, runId, ownershipTokenHash,
            family, scenarioHash, ExactSourceDatabase, sourceProof.SourceFingerprint, sourceCommitFingerprint, MigrationId,
            sourceProof.MigrationFingerprint, sourceProof.OwnerPrincipal, intentAt, intentAt.AddHours(4), runtimeRole, issuerRole,
            controlPlaneIssuer, controlPlaneAuthority);
        // This durable registry reservation is the authority. It must commit before role/database creation.
        await Rev869BControlPlaneRegistry.ReserveBeforeCreateAsync(reservation);
        await WriteEvidenceAsync(new QuarantineEvidence(databaseName, runId,
            ownershipTokenHash, family,
            scenarioHash, ExactSourceDatabase, sourceProof.SourceFingerprint, sourceCommitFingerprint,
            MigrationId, sourceProof.MigrationFingerprint, sourceProof.OwnerPrincipal, intentAt, intentAt.AddHours(4),
            runtimeRole, issuerRole, Rev869BControlPlaneRegistry.Policy, "PreCreateIntent", null));
        var admin = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = "postgres", Pooling = false };
        try
        {
            await using var connection = new NpgsqlConnection(admin.ConnectionString);
            await connection.OpenAsync();
            await RequireCurrentDatabaseAsync(connection, "postgres", "administrative CREATE boundary");
            await RequireDatabaseAbsentAsync(connection, databaseName);
            await RequireRoleAbsentAsync(connection, runtimeRole);
            await RequireRoleAbsentAsync(connection, issuerRole);
            var quotedRuntime = new NpgsqlCommandBuilder().QuoteIdentifier(runtimeRole);
            var quotedIssuer = new NpgsqlCommandBuilder().QuoteIdentifier(issuerRole);
            await using (var createRuntime = new NpgsqlCommand(
                $"CREATE ROLE {quotedRuntime} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION PASSWORD @password", connection))
            {
                createRuntime.Parameters.AddWithValue("password", runtimePassword);
                await createRuntime.ExecuteNonQueryAsync();
            }
            await using (var createIssuer = new NpgsqlCommand(
                $"CREATE ROLE {quotedIssuer} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION PASSWORD @password", connection))
            {
                createIssuer.Parameters.AddWithValue("password", issuerPassword);
                await createIssuer.ExecuteNonQueryAsync();
            }
            var quotedOwned = new NpgsqlCommandBuilder().QuoteIdentifier(databaseName);
            var quotedTemplate = new NpgsqlCommandBuilder().QuoteIdentifier(ExactSourceDatabase);
            await new NpgsqlCommand($"CREATE DATABASE {quotedOwned} WITH TEMPLATE {quotedTemplate}", connection).ExecuteNonQueryAsync();
            await new NpgsqlCommand($"GRANT CONNECT ON DATABASE {quotedOwned} TO {quotedRuntime},{quotedIssuer}", connection).ExecuteNonQueryAsync();
        }
        catch (Exception preMarkerFailure)
        {
            await Rev869BControlPlaneRegistry.BindMarkerAndOutcomeAsync(
                reservation, "PreCreateIntent", "Quarantined", null, "Failed", preMarkerFailure.GetType().Name);
            throw;
        }

        var owner = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = databaseName, Pooling = false };
        var runtime = new NpgsqlConnectionStringBuilder(owner.ConnectionString) { Username = runtimeRole, Password = runtimePassword };
        var issuer = new NpgsqlConnectionStringBuilder(owner.ConnectionString) { Username = issuerRole, Password = issuerPassword };
        var lease = new Rev869BTestDatabaseLease(runtime.ConnectionString, owner.ConnectionString, issuer.ConnectionString,
            admin.ConnectionString, databaseName, runId, ownershipToken, runtimeRole, issuerRole,
            previousIssuerConnection, family, scenarioHash, sourceProof.SourceFingerprint, sourceProof.MigrationFingerprint,
            reservation);
        try
        {
            await lease.EstablishMarkerAsync(scenario);
            await lease.VerifyOwnershipAsync();
            lease.markerFingerprint = lease.ComputeMarkerFingerprint("OwnedActive");
            await Rev869BControlPlaneRegistry.BindMarkerAndOutcomeAsync(
                reservation, "PreCreateIntent", "OwnedActive", lease.markerFingerprint, "Succeeded", null);
            await lease.WriteEvidenceAsync("OwnedActive");
            Environment.SetEnvironmentVariable("REV869B_COMMAND_ISSUER_CONNECTION", issuer.ConnectionString);
            return lease;
        }
        catch (Exception creationFailure)
        {
            try
            {
                await Rev869BControlPlaneRegistry.BindMarkerAndOutcomeAsync(
                    reservation, "PreCreateIntent", "Quarantined", null, "Failed", creationFailure.GetType().Name);
            }
            catch (Exception registryFailure)
            {
                creationFailure = new AggregateException(
                    "Provisioning failed and its durable control-plane failure outcome could not be recorded.",
                    creationFailure, registryFailure);
            }
            // The unique database is deliberately quarantined when its marker cannot be proved.
            // An explicit cleanup must independently verify the exact marker before DROP.
            try { await lease.DisposeAsync(); }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(
                    "Disposable database creation failed and proof-bound cleanup also failed; the database remains quarantined.",
                    creationFailure, cleanupFailure);
            }
            throw;
        }
    }

    internal async Task<NpgsqlConnection> OpenVerifiedConnectionAsync()
    {
        if (disposed) throw new ObjectDisposedException(nameof(Rev869BTestDatabaseLease));
        await VerifyOwnershipAsync();
        var connection = new NpgsqlConnection(ConnectionString);
        try
        {
            await connection.OpenAsync();
            await RequireCurrentDatabaseAsync(connection, DatabaseName, "least-privilege runtime boundary");
            await RequireMigrationOnceAsync(connection);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static async Task<SourceProof> VerifySourceAsync(string sourceConnectionString)
    {
        await using var source = new NpgsqlConnection(sourceConnectionString);
        await source.OpenAsync();
        await RequireCurrentDatabaseAsync(source, ExactSourceDatabase, "source identity before CREATE");
        await RequireMigrationOnceAsync(source);
        await using (var owner = new NpgsqlCommand("""
            SELECT
              (SELECT count(*) FROM pg_roles r WHERE r.rolname='nexa_rev869b_security_owner' AND NOT r.rolcanlogin
                AND NOT r.rolsuper AND NOT r.rolcreatedb AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls),
              (SELECT count(*) FROM pg_roles r WHERE r.rolname='nexa_rev869b_purge_executor' AND r.rolcanlogin
                AND NOT r.rolsuper AND NOT r.rolcreatedb AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls),
              (SELECT count(*) FROM pg_roles r WHERE r.rolname='nexa_rev869b_purge_authorizer' AND r.rolcanlogin
                AND NOT r.rolsuper AND NOT r.rolcreatedb AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls),
              (SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                   WHERE n.nspname='nexa' AND p.proname IN ('rev869b_slot_fingerprint','rev869b_issue_command_grant',
                    'rev869b_open_command_context','rev869b_command_context_valid','rev869b_claim_command_context',
                    'rev869b_provision_command_authority','rev869b_reject_security_audit_mutation',
                    'rev869b_register_purge_authorization','rev869b_begin_purge_execution',
                    'rev869b_purge_temporary_security_ledger','rev869b_record_command_outcome')
                    AND pg_get_userbyid(p.proowner)='nexa_rev869b_security_owner'),
              (SELECT count(*) FROM pg_auth_members m JOIN pg_roles role ON role.oid=m.roleid
                JOIN pg_roles member ON member.oid=m.member
                WHERE role.rolname IN ('nexa_rev869b_purge_executor','nexa_rev869b_purge_authorizer')
                   OR member.rolname IN ('nexa_rev869b_security_owner','nexa_rev869b_purge_executor','nexa_rev869b_purge_authorizer')),
              (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='nexa' AND c.relname LIKE 'rev869b_%'
                  AND pg_get_userbyid(c.relowner) IN ('nexa_rev869b_purge_executor','nexa_rev869b_purge_authorizer')),
              (SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='nexa' AND p.proname LIKE 'rev869b_%'
                  AND pg_get_userbyid(p.proowner) IN ('nexa_rev869b_purge_executor','nexa_rev869b_purge_authorizer')),
              (SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='nexa' AND
                  ((p.proname='rev869b_register_purge_authorization'
                    AND has_function_privilege('nexa_rev869b_purge_authorizer',p.oid,'EXECUTE')
                    AND NOT has_function_privilege('nexa_rev869b_purge_executor',p.oid,'EXECUTE'))
                   OR (p.proname IN ('rev869b_begin_purge_execution','rev869b_purge_temporary_security_ledger')
                    AND has_function_privilege('nexa_rev869b_purge_executor',p.oid,'EXECUTE')
                    AND NOT has_function_privilege('nexa_rev869b_purge_authorizer',p.oid,'EXECUTE')))),
              (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='nexa' AND c.relname IN ('rev869b_command_grants','rev869b_command_security_audits',
                  'rev869b_purge_authorizations','rev869b_purge_attempt_audits','rev869b_purge_rejection_audits')
                  AND (has_table_privilege('nexa_rev869b_purge_executor',c.oid,'SELECT,INSERT,UPDATE,DELETE')
                    OR has_table_privilege('nexa_rev869b_purge_authorizer',c.oid,'SELECT,INSERT,UPDATE,DELETE')))
            """, source))
        await using (var ownerReader = await owner.ExecuteReaderAsync())
        {
            if (!await ownerReader.ReadAsync() || ownerReader.GetInt64(0) != 1 || ownerReader.GetInt64(1) != 1 ||
                ownerReader.GetInt64(2) != 1 || ownerReader.GetInt64(3) != 11 || ownerReader.GetInt64(4) != 0 ||
                ownerReader.GetInt64(5) != 0 || ownerReader.GetInt64(6) != 0 || ownerReader.GetInt64(7) != 3 ||
                ownerReader.GetInt64(8) != 0)
                throw new InvalidOperationException("Source must prove exact capability-free security, purge-authorizer and purge-executor roles with closed membership, ownership, and all eleven security functions.");
        }
        await using var fingerprint = new NpgsqlCommand("""
            SELECT
              encode(public.digest(convert_to(jsonb_build_object(
                'database',current_database(),
                'schemaOwner',(SELECT pg_get_userbyid(n.nspowner) FROM pg_namespace n WHERE n.nspname='nexa'),
                'migrations',(SELECT jsonb_agg(jsonb_build_array(h."MigrationId",h."ProductVersion") ORDER BY h."MigrationId") FROM nexa."__EFMigrationsHistory" h),
                'functions',(SELECT jsonb_agg(jsonb_build_array(p.proname,pg_get_function_identity_arguments(p.oid),pg_get_userbyid(p.proowner),pg_get_functiondef(p.oid)) ORDER BY p.proname,pg_get_function_identity_arguments(p.oid)) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='nexa' AND p.proname LIKE 'rev869b_%'),
                'triggers',(SELECT jsonb_agg(jsonb_build_array(c.relname,t.tgname,pg_get_triggerdef(t.oid,true)) ORDER BY c.relname,t.tgname) FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='nexa' AND t.tgname LIKE 'trg_rev869b_%' AND NOT t.tgisinternal)
              )::text,'UTF8'),'sha256'),'hex'),
              encode(public.digest(convert_to((SELECT jsonb_build_array(h."MigrationId",h."ProductVersion")::text FROM nexa."__EFMigrationsHistory" h WHERE h."MigrationId"=@migration),'UTF8'),'sha256'),'hex'),
              current_user::text
            """, source);
        fingerprint.Parameters.AddWithValue("migration", MigrationId);
        await using var proofReader = await fingerprint.ExecuteReaderAsync();
        if (!await proofReader.ReadAsync() || proofReader.IsDBNull(0) || proofReader.IsDBNull(1) || proofReader.IsDBNull(2))
            throw new InvalidOperationException("Exact source and REV869B migration fingerprints are unavailable.");
        var proof = new SourceProof(proofReader.GetString(0), proofReader.GetString(1), proofReader.GetString(2));
        if (proof.SourceFingerprint.Length != 64 || proof.MigrationFingerprint.Length != 64)
            throw new InvalidOperationException("Exact source and migration SHA-256 fingerprints are invalid.");
        return proof;
    }

    private async Task EstablishMarkerAsync(string scenario)
    {
        await using var connection = new NpgsqlConnection(ownerConnectionString);
        await connection.OpenAsync();
        await RequireCurrentDatabaseAsync(connection, DatabaseName, "target marker provisioning");
        var quotedRuntime = new NpgsqlCommandBuilder().QuoteIdentifier(runtimeRole);
        var quotedIssuer = new NpgsqlCommandBuilder().QuoteIdentifier(issuerRole);
        await using var command = new NpgsqlCommand($$"""
            CREATE TABLE nexa.{{MarkerTable}}(
              "OwnershipToken" text PRIMARY KEY,
              "RunId" text NOT NULL UNIQUE,
              "DatabaseName" text NOT NULL UNIQUE,
              "ControlPlanePolicy" text NOT NULL,
              "SourceCommitFingerprint" text NOT NULL CHECK ("SourceCommitFingerprint"~'^[0-9a-f]{40}$'),
              "SourceDatabase" text NOT NULL,
              "SourceFingerprint" text NOT NULL CHECK ("SourceFingerprint"~'^[0-9a-f]{64}$'),
              "MigrationId" text NOT NULL,
              "MigrationFingerprint" text NOT NULL CHECK ("MigrationFingerprint"~'^[0-9a-f]{64}$'),
              "FixtureFamily" text NOT NULL,
              "ScenarioHash" text NOT NULL,
              "ExpectedOwner" name NOT NULL,
              "LeaseRequestedAt" timestamptz NOT NULL,
              "LeaseExpiresAt" timestamptz NOT NULL,
              "RegistryState" text NOT NULL CHECK ("RegistryState" IN ('Reserved','OwnedActive','DropStarted','Quarantined')),
              "MarkerFingerprint" text NOT NULL CHECK ("MarkerFingerprint"~'^[0-9A-F]{64}$'),
              "QuarantineState" text NOT NULL CHECK ("QuarantineState" IN ('OwnedActive','Quarantined')),
              "ProvisionedAt" timestamptz NOT NULL DEFAULT statement_timestamp()
            );
            REVOKE ALL ON nexa.{{MarkerTable}} FROM PUBLIC;
            INSERT INTO nexa.{{MarkerTable}}("OwnershipToken","RunId","DatabaseName","ControlPlanePolicy","SourceCommitFingerprint",
              "SourceDatabase","SourceFingerprint","MigrationId","MigrationFingerprint","FixtureFamily","ScenarioHash","ExpectedOwner",
              "LeaseRequestedAt","LeaseExpiresAt","RegistryState","MarkerFingerprint","QuarantineState")
            VALUES(@token,@run,@database,@policy,@sourceCommit,@source,@sourceFingerprint,@migration,@migrationFingerprint,
              @family,@scenario,current_user,@requested,@leaseExpires,'OwnedActive',@markerFingerprint,'OwnedActive');
            DELETE FROM nexa.rev869b_command_contexts;
            DELETE FROM nexa.rev869b_command_grants;
            DELETE FROM nexa.rev869b_command_authorities;
            GRANT USAGE ON SCHEMA nexa TO {{quotedRuntime}},{{quotedIssuer}};
            GRANT SELECT,INSERT,UPDATE,DELETE ON ALL TABLES IN SCHEMA nexa TO {{quotedRuntime}};
            REVOKE SELECT,UPDATE,DELETE ON nexa.audit_logs FROM {{quotedRuntime}};
            REVOKE ALL ON nexa.rev869b_command_authorities,nexa.rev869b_command_grants,nexa.rev869b_command_contexts,
              nexa.rev869b_claim_sequence_pool,nexa.rev869b_command_security_audits,nexa.rev869b_purge_authorizations,
              nexa.rev869b_purge_attempt_audits,nexa.rev869b_purge_rejection_audits,nexa.{{MarkerTable}}
              FROM {{quotedRuntime}},{{quotedIssuer}};
            SELECT nexa.rev869b_provision_command_authority(@issuer,@runtime,NULL);
            """, connection);
        command.Parameters.AddWithValue("token", OwnershipToken);
        command.Parameters.AddWithValue("run", RunId);
        command.Parameters.AddWithValue("database", DatabaseName);
        command.Parameters.AddWithValue("policy", Rev869BControlPlaneRegistry.Policy);
        command.Parameters.AddWithValue("sourceCommit", reservation.SourceCommitFingerprint);
        command.Parameters.AddWithValue("source", ExactSourceDatabase);
        command.Parameters.AddWithValue("sourceFingerprint", sourceFingerprint);
        command.Parameters.AddWithValue("migration", MigrationId);
        command.Parameters.AddWithValue("migrationFingerprint", migrationFingerprint);
        command.Parameters.AddWithValue("family", Family);
        command.Parameters.AddWithValue("scenario", scenarioHash);
        command.Parameters.AddWithValue("requested", reservation.RequestedAt);
        command.Parameters.AddWithValue("leaseExpires", reservation.LeaseExpiresAt);
        command.Parameters.AddWithValue("markerFingerprint", ComputeMarkerFingerprint("OwnedActive"));
        command.Parameters.AddWithValue("issuer", issuerRole);
        command.Parameters.AddWithValue("runtime", runtimeRole);
        await command.ExecuteNonQueryAsync();
        await using var markerState = new NpgsqlCommand($$"""
            SELECT "ExpectedOwner","ProvisionedAt" FROM nexa.{{MarkerTable}}
            WHERE "OwnershipToken"=@token AND "RunId"=@run AND "ScenarioHash"=@scenario
            """, connection);
        markerState.Parameters.AddWithValue("token", OwnershipToken);
        markerState.Parameters.AddWithValue("run", RunId);
        markerState.Parameters.AddWithValue("scenario", scenarioHash);
        await using var reader = await markerState.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("Exact marker provisioning evidence is missing.");
        expectedOwner = reader.GetString(0);
        provisionedAt = reader.GetFieldValue<DateTimeOffset>(1);
    }

    private async Task VerifyOwnershipAsync()
    {
        await using var connection = new NpgsqlConnection(ownerConnectionString);
        await connection.OpenAsync();
        await VerifyOwnershipAsync(connection);
    }

    private async Task VerifyOwnershipAsync(NpgsqlConnection connection)
    {
        RequireSafeOwnedName(DatabaseName);
        await RequireCurrentDatabaseAsync(connection, DatabaseName, "target connect/use/drop proof");
        await RequireMigrationOnceAsync(connection);
        await using var marker = new NpgsqlCommand($$"""
            SELECT count(*) FROM nexa.{{MarkerTable}}
            WHERE "OwnershipToken"=@token AND "RunId"=@run AND "DatabaseName"=@database
              AND "ControlPlanePolicy"=@policy AND "SourceCommitFingerprint"=@sourceCommit
              AND "SourceDatabase"=@source AND "SourceFingerprint"=@sourceFingerprint
              AND "MigrationId"=@migration AND "MigrationFingerprint"=@migrationFingerprint AND "FixtureFamily"=@family
              AND "ScenarioHash"=@scenario AND "ExpectedOwner"=@owner AND "ExpectedOwner"=current_user
              AND "LeaseRequestedAt"=@requested AND "LeaseExpiresAt"=@leaseExpires
              AND "RegistryState"='OwnedActive' AND "MarkerFingerprint"=@markerFingerprint
              AND "ProvisionedAt"=@provisioned AND "QuarantineState"='OwnedActive'
            """, connection);
        marker.Parameters.AddWithValue("token", OwnershipToken);
        marker.Parameters.AddWithValue("run", RunId);
        marker.Parameters.AddWithValue("database", DatabaseName);
        marker.Parameters.AddWithValue("policy", Rev869BControlPlaneRegistry.Policy);
        marker.Parameters.AddWithValue("sourceCommit", reservation.SourceCommitFingerprint);
        marker.Parameters.AddWithValue("source", ExactSourceDatabase);
        marker.Parameters.AddWithValue("sourceFingerprint", sourceFingerprint);
        marker.Parameters.AddWithValue("migration", MigrationId);
        marker.Parameters.AddWithValue("migrationFingerprint", migrationFingerprint);
        marker.Parameters.AddWithValue("family", Family);
        marker.Parameters.AddWithValue("scenario", scenarioHash);
        marker.Parameters.AddWithValue("owner", expectedOwner);
        marker.Parameters.AddWithValue("requested", reservation.RequestedAt);
        marker.Parameters.AddWithValue("leaseExpires", reservation.LeaseExpiresAt);
        marker.Parameters.AddWithValue("markerFingerprint", markerFingerprint);
        marker.Parameters.AddWithValue("provisioned", provisionedAt);
        if (Convert.ToInt64(await marker.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("Owned database marker mismatch; database is quarantined and DROP is refused.");
        var registry = await Rev869BControlPlaneRegistry.ReadExactLeaseAsync(reservation, "OwnedActive");
        if (!string.Equals(registry.MarkerFingerprint, markerFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("Control-plane and target marker fingerprints do not match.");
    }

    private string ComputeMarkerFingerprint(string state) => ComputeMarkerFingerprint(
        DatabaseName, RunId, Convert.ToHexString(SHA256.HashData(Convert.FromHexString(OwnershipToken))),
        Family, scenarioHash, ExactSourceDatabase, sourceFingerprint, reservation.SourceCommitFingerprint,
        MigrationId, migrationFingerprint, reservation.ExpectedOwner, reservation.RequestedAt,
        reservation.LeaseExpiresAt, state);

    private static string ComputeMarkerFingerprint(
        string databaseName, string runId, string ownershipHash, string family, string scenario,
        string sourceDatabase, string sourceFingerprint, string sourceCommit, string migrationId,
        string migrationFingerprint, string owner, DateTimeOffset requestedAt, DateTimeOffset expiresAt, string state) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
            databaseName, runId, ownershipHash, family, scenario, sourceDatabase, sourceFingerprint, sourceCommit,
            migrationId, migrationFingerprint, owner, requestedAt.ToUniversalTime().ToString("O"),
            expiresAt.ToUniversalTime().ToString("O"), Rev869BControlPlaneRegistry.Policy, state))));

    private static string ResolveAuthoritativeSourceCommitFingerprint()
    {
        var informational = typeof(Rev869BTestDatabaseLease).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        var match = Regex.Match(informational, @"(?:\+|\.)(?<commit>[0-9a-fA-F]{40})(?:$|\.)", RegexOptions.CultureInvariant);
        if (!match.Success)
            throw new InvalidOperationException("The executing test assembly does not contain an authoritative 40-hex source revision.");
        var commit = match.Groups["commit"].Value.ToLowerInvariant();
        var supplied = Environment.GetEnvironmentVariable("REV869B_SOURCE_COMMIT_FINGERPRINT");
        if (!string.IsNullOrWhiteSpace(supplied) && !string.Equals(supplied, commit, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Caller-supplied source revision does not match the executing assembly revision.");
        return commit;
    }

    private static async Task RequireMigrationOnceAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SELECT count(*) FROM nexa.\"__EFMigrationsHistory\" WHERE \"MigrationId\"=@migration", connection);
        command.Parameters.AddWithValue("migration", MigrationId);
        if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("The retained REV869B migration must be installed exactly once.");
    }

    private static async Task RequireCurrentDatabaseAsync(NpgsqlConnection connection, string expected, string boundary)
    {
        var actual = Convert.ToString(await new NpgsqlCommand("SELECT current_database()", connection).ExecuteScalarAsync());
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidOperationException($"Database identity failed at {boundary}.");
    }

    private static async Task RequireDatabaseAbsentAsync(NpgsqlConnection connection, string databaseName)
    {
        await using var exists = new NpgsqlCommand("SELECT count(*) FROM pg_database WHERE datname=@name", connection);
        exists.Parameters.AddWithValue("name", databaseName);
        if (Convert.ToInt64(await exists.ExecuteScalarAsync()) != 0)
            throw new InvalidOperationException("Unique test database collision; ownership is not proven and no repair is attempted.");
    }

    private static async Task RequireRoleAbsentAsync(NpgsqlConnection connection, string roleName)
    {
        await using var command = new NpgsqlCommand("SELECT count(*) FROM pg_roles WHERE rolname=@role", connection);
        command.Parameters.AddWithValue("role", roleName);
        if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 0)
            throw new InvalidOperationException("Unique test role collision; ownership is not proven and no repair is attempted.");
    }

    private static void RequireSafeOwnedRole(string roleName, string runId, string prefix)
    {
        if (!string.Equals(roleName, prefix + runId[..16], StringComparison.Ordinal) ||
            roleName.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '_')))
            throw new InvalidOperationException("Unsafe or unexpected REV869B disposable role name.");
    }

    private static async Task DropOwnedRolesAsync(NpgsqlConnection admin, string runtimeRole, string issuerRole, string runId)
    {
        RequireSafeOwnedRole(runtimeRole, runId, "rev869b_rt_");
        RequireSafeOwnedRole(issuerRole, runId, "rev869b_iss_");
        var quotedRuntime = new NpgsqlCommandBuilder().QuoteIdentifier(runtimeRole);
        var quotedIssuer = new NpgsqlCommandBuilder().QuoteIdentifier(issuerRole);
        await new NpgsqlCommand($"DROP ROLE IF EXISTS {quotedRuntime}", admin).ExecuteNonQueryAsync();
        await new NpgsqlCommand($"DROP ROLE IF EXISTS {quotedIssuer}", admin).ExecuteNonQueryAsync();
        await RequireRoleAbsentAsync(admin, runtimeRole);
        await RequireRoleAbsentAsync(admin, issuerRole);
    }

    private static async Task RequireNoTargetConnectionsAsync(NpgsqlConnection admin, string databaseName)
    {
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM pg_stat_activity WHERE datname=@name AND pid<>pg_backend_pid()", admin);
        command.Parameters.AddWithValue("name", databaseName);
        if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 0)
            throw new InvalidOperationException("Target has active connections; broad termination is prohibited and DROP is refused.");
    }

    private static void RequireSafeOwnedName(string databaseName)
    {
        var suffix = databaseName.StartsWith(DatabasePrefix, StringComparison.Ordinal) ? databaseName[DatabasePrefix.Length..] : string.Empty;
        if (suffix.Length != 24 || suffix.Any(c => !Uri.IsHexDigit(c)) ||
            string.Equals(databaseName, ExactSourceDatabase, StringComparison.Ordinal) ||
            databaseName is "postgres" or "template0" or "template1" ||
            databaseName.Contains("rev861", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Contains("rev868", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Contains("rev869a", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unsafe or unexpected REV869B disposable database name.");
    }

    internal static async Task RecoverQuarantinedAsync(
        string databaseName,
        string runId,
        string ownershipToken,
        string family,
        string scenarioHash,
        string expectedOwner,
        DateTimeOffset provisionedAt,
        string runtimeRole,
        string issuerRole)
    {
        RequireApprovedRetentionConfiguration();
        if (!string.Equals(Environment.GetEnvironmentVariable("REV869B_POSTGRES_OPT_IN"), ExactOptIn, StringComparison.Ordinal))
            throw new InvalidOperationException($"Set REV869B_POSTGRES_OPT_IN={ExactOptIn} explicitly.");
        var raw = Environment.GetEnvironmentVariable("REV869B_POSTGRES")
            ?? throw new InvalidOperationException("REV869B_POSTGRES is required; no fallback is permitted.");
        var source = new NpgsqlConnectionStringBuilder(raw) { Pooling = false };
        if (!string.Equals(source.Database, ExactSourceDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException($"Only the exact isolated source database {ExactSourceDatabase} is permitted.");
        var sourceProof = await VerifySourceAsync(source.ConnectionString);
        RequireSafeOwnedName(databaseName);
        if (runId.Length != 32 || runId.Any(c => !Uri.IsHexDigit(c)) ||
            ownershipToken.Length != 64 || ownershipToken.Any(c => !Uri.IsHexDigit(c)) ||
            !string.Equals(databaseName, DatabasePrefix + runId[..24], StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(family) || scenarioHash.Length != 64 || scenarioHash.Any(c => !Uri.IsHexDigit(c)) ||
            string.IsNullOrWhiteSpace(expectedOwner) || provisionedAt == default)
            throw new InvalidOperationException("Complete high-entropy quarantine recovery proof is required.");
        RequireSafeOwnedRole(runtimeRole, runId, "rev869b_rt_");
        RequireSafeOwnedRole(issuerRole, runId, "rev869b_iss_");
        var ownershipHash = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(ownershipToken)));
        var sourceCommitFingerprint = ResolveAuthoritativeSourceCommitFingerprint();
        var leaseExpiresAt = provisionedAt.AddHours(4);
        var quarantineMarkerFingerprint = ComputeMarkerFingerprint(databaseName, runId, ownershipHash, family,
            scenarioHash, ExactSourceDatabase, sourceProof.SourceFingerprint, sourceCommitFingerprint, MigrationId,
            sourceProof.MigrationFingerprint, expectedOwner, provisionedAt, leaseExpiresAt, "Quarantined");
        var evidence = await ReadVerifiedEvidenceAsync(databaseName, runId);
        var quarantinedEvidence = new QuarantineEvidence(databaseName, runId, ownershipHash, family, scenarioHash,
            ExactSourceDatabase, sourceProof.SourceFingerprint, sourceCommitFingerprint, MigrationId,
            sourceProof.MigrationFingerprint, expectedOwner, provisionedAt, leaseExpiresAt, runtimeRole, issuerRole,
            Rev869BControlPlaneRegistry.Policy, "Quarantined", quarantineMarkerFingerprint);
        var preCreateEvidence = quarantinedEvidence with { State = "PreCreateIntent", MarkerFingerprint = null };
        if (evidence != quarantinedEvidence && evidence != preCreateEvidence)
            throw new InvalidOperationException("Durable quarantine evidence does not match the exact recovery target.");
        var recoveringPreCreateInterruption = evidence.State == "PreCreateIntent";
        var authorizationValue = Environment.GetEnvironmentVariable("REV869B_QUARANTINE_RECOVERY_AUTHORIZATION")
            ?? throw new InvalidOperationException("A fresh quarantine recovery authorization envelope is required.");
        var authorization = JsonSerializer.Deserialize<RecoveryAuthorization>(authorizationValue)
            ?? throw new InvalidOperationException("The quarantine recovery authorization envelope is invalid.");
        var now = DateTimeOffset.UtcNow;
        if (authorization.Purpose != RecoveryPurpose || authorization.AuthorizationId == Guid.Empty ||
            authorization.Nonce.Length < 32 || authorization.IssuedAt > now || authorization.ExpiresAt <= now ||
            authorization.ExpiresAt > authorization.IssuedAt.AddMinutes(15) ||
            string.IsNullOrWhiteSpace(authorization.ApprovalIssuer) ||
            string.IsNullOrWhiteSpace(authorization.IssuerAuthority) || string.IsNullOrWhiteSpace(authorization.ApprovalReference) ||
            string.IsNullOrWhiteSpace(authorization.Reason) || string.IsNullOrWhiteSpace(authorization.ExecutorIdentity) ||
            authorization.ExpectedPreState != evidence.State || authorization.AuthorizedPostState != "Dropped")
            throw new InvalidOperationException("Recovery authorization is stale, expired, wrong-purpose, or not bounded to fifteen minutes.");
        var approvalCanonical = string.Join('|', RecoveryPurpose, authorization.AuthorizationId, authorization.Nonce,
            authorization.ApprovalIssuer, authorization.IssuerAuthority, authorization.ExpectedPreState, authorization.AuthorizedPostState,
            authorization.ApprovalReference, authorization.Reason, authorization.ExecutorIdentity,
            authorization.IssuedAt.ToUniversalTime().ToString("O"), authorization.ExpiresAt.ToUniversalTime().ToString("O"),
            databaseName, runId, ownershipHash, family, scenarioHash,
            ExactSourceDatabase, sourceProof.SourceFingerprint, MigrationId, sourceProof.MigrationFingerprint,
            expectedOwner, provisionedAt.ToUniversalTime().ToString("O"), runtimeRole, issuerRole);
        var expectedApproval = HMACSHA256.HashData(RecoveryAuthorizationKey(), Encoding.UTF8.GetBytes(approvalCanonical));
        if (authorization.Signature.Length != 64 || authorization.Signature.Any(c => !Uri.IsHexDigit(c)) ||
            !CryptographicOperations.FixedTimeEquals(expectedApproval, Convert.FromHexString(authorization.Signature)))
            throw new InvalidOperationException("A separately governed, instance-bound quarantine recovery signature is required.");
        var recoveryReservation = new Rev869BControlPlaneRegistry.LeaseReservation(databaseName, runId, ownershipHash,
            family, scenarioHash, ExactSourceDatabase, sourceProof.SourceFingerprint,
            sourceCommitFingerprint, MigrationId,
            sourceProof.MigrationFingerprint, expectedOwner, provisionedAt, provisionedAt.AddHours(4), runtimeRole, issuerRole,
            authorization.ApprovalIssuer, authorization.IssuerAuthority);
        var targetFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(approvalCanonical)));
        var recoveryApproval = new Rev869BControlPlaneRegistry.RecoveryApproval(authorization.AuthorizationId,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(authorization.Nonce))), authorization.Purpose,
            authorization.ApprovalIssuer, authorization.IssuerAuthority, authorization.ExpectedPreState,
            authorization.AuthorizedPostState, authorization.ApprovalReference, authorization.Reason,
            authorization.ExecutorIdentity, authorization.IssuedAt, authorization.ExpiresAt);
        var registryLease = await Rev869BControlPlaneRegistry.ReadExactLeaseAsync(
            recoveryReservation, evidence.State);
        if (!string.Equals(registryLease.MarkerFingerprint, evidence.MarkerFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("Recovery control-plane lease and supplemental marker evidence do not match.");
        // The registry atomically validates scope and consumes the approval before any target access or DROP.
        var recoveryAttempt = await Rev869BControlPlaneRegistry.ConsumeRecoveryBeforeMutationAsync(
            recoveryReservation, recoveryApproval, targetFingerprint);

        try
        {
        // Filesystem replay evidence is supplemental; any failure is now inside the terminal-outcome boundary.
        await ConsumeRecoveryAuthorizationAsync(databaseName, runId, authorization, approvalCanonical);
        var adminBuilder = new NpgsqlConnectionStringBuilder(source.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };
        await using (var preTargetAdmin = new NpgsqlConnection(adminBuilder.ConnectionString))
        {
            await preTargetAdmin.OpenAsync();
            await RequireCurrentDatabaseAsync(preTargetAdmin, "postgres", "pre-target recovery catalogue proof");
            await using var targetExists = new NpgsqlCommand(
                "SELECT count(*) FROM pg_database WHERE datname=@database", preTargetAdmin);
            targetExists.Parameters.AddWithValue("database", databaseName);
            if (Convert.ToInt64(await targetExists.ExecuteScalarAsync()) == 0)
            {
                if (!recoveringPreCreateInterruption)
                    throw new InvalidOperationException("Only a registry-proven pre-create interruption may recover without a target database.");
                await DropOwnedRolesAsync(preTargetAdmin, runtimeRole, issuerRole, runId);
                await Rev869BControlPlaneRegistry.RecordRecoveryOutcomeAsync(
                    recoveryAttempt, evidence.State, "Dropped", null, "Succeeded", null);
                await WriteEvidenceAsync(evidence with { State = "Dropped", MarkerFingerprint = null });
                return;
            }
        }
        var targetBuilder = new NpgsqlConnectionStringBuilder(source.ConnectionString)
        {
            Database = databaseName,
            Pooling = false
        };
        await using (var target = new NpgsqlConnection(targetBuilder.ConnectionString))
        {
            await target.OpenAsync();
            await RequireCurrentDatabaseAsync(target, databaseName, "quarantine recovery target proof");
            await RequireMigrationOnceAsync(target);
            if (recoveringPreCreateInterruption)
            {
                await using var preMarker = new NpgsqlCommand($$"""
                    SELECT count(*) FROM pg_database
                    WHERE datname=current_database() AND pg_get_userbyid(datdba)=@owner
                      AND to_regclass('nexa.{{MarkerTable}}') IS NULL
                    """, target);
                preMarker.Parameters.AddWithValue("owner", expectedOwner);
                if (Convert.ToInt64(await preMarker.ExecuteScalarAsync()) != 1)
                    throw new InvalidOperationException("Pre-marker recovery proof mismatch; DROP is refused.");
            }
            else
            {
                await using var marker = new NpgsqlCommand($$"""
                    SELECT count(*) FROM nexa.{{MarkerTable}}
                    WHERE "OwnershipToken"=@token AND "RunId"=@run AND "DatabaseName"=@database
                      AND "ControlPlanePolicy"=@policy AND "SourceCommitFingerprint"=@sourceCommit
                      AND "SourceDatabase"=@source AND "SourceFingerprint"=@sourceFingerprint
                      AND "MigrationId"=@migration AND "MigrationFingerprint"=@migrationFingerprint AND "FixtureFamily"=@family
                      AND "ScenarioHash"=@scenario AND "ExpectedOwner"=@owner AND "ExpectedOwner"=current_user
                      AND "LeaseRequestedAt"=@provisioned AND "LeaseExpiresAt"=@leaseExpires
                      AND "RegistryState"='Quarantined' AND "MarkerFingerprint"=@markerFingerprint
                      AND "ProvisionedAt"=@provisioned AND "QuarantineState"='Quarantined'
                    """, target);
                marker.Parameters.AddWithValue("token", ownershipToken);
                marker.Parameters.AddWithValue("run", runId);
                marker.Parameters.AddWithValue("database", databaseName);
                marker.Parameters.AddWithValue("policy", Rev869BControlPlaneRegistry.Policy);
                marker.Parameters.AddWithValue("sourceCommit", sourceCommitFingerprint);
                marker.Parameters.AddWithValue("source", ExactSourceDatabase);
                marker.Parameters.AddWithValue("sourceFingerprint", sourceProof.SourceFingerprint);
                marker.Parameters.AddWithValue("migration", MigrationId);
                marker.Parameters.AddWithValue("migrationFingerprint", sourceProof.MigrationFingerprint);
                marker.Parameters.AddWithValue("family", family);
                marker.Parameters.AddWithValue("scenario", scenarioHash);
                marker.Parameters.AddWithValue("owner", expectedOwner);
                marker.Parameters.AddWithValue("provisioned", provisionedAt);
                marker.Parameters.AddWithValue("leaseExpires", leaseExpiresAt);
                marker.Parameters.AddWithValue("markerFingerprint", evidence.MarkerFingerprint!);
                if (Convert.ToInt64(await marker.ExecuteScalarAsync()) != 1)
                    throw new InvalidOperationException("Quarantine recovery proof mismatch; DROP is refused.");
            }
        }

        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
        await admin.OpenAsync();
        await RequireCurrentDatabaseAsync(admin, "postgres", "quarantine recovery DROP boundary");
        RequireSafeOwnedName(databaseName);
        await RequireNoTargetConnectionsAsync(admin, databaseName);
        var quoted = new NpgsqlCommandBuilder().QuoteIdentifier(databaseName);
        await new NpgsqlCommand($"DROP DATABASE {quoted}", admin).ExecuteNonQueryAsync();
        await RequireDatabaseAbsentAsync(admin, databaseName);
        await DropOwnedRolesAsync(admin, runtimeRole, issuerRole, runId);
        await Rev869BControlPlaneRegistry.RecordRecoveryOutcomeAsync(recoveryAttempt, evidence.State, "Dropped",
            evidence.MarkerFingerprint, "Succeeded", null);
        await WriteEvidenceAsync(evidence with { State = "Dropped" });
        }
        catch (Exception recoveryFailure)
        {
            try
            {
                await Rev869BControlPlaneRegistry.RecordRecoveryOutcomeAsync(recoveryAttempt, evidence.State,
                    evidence.State, evidence.MarkerFingerprint, "Failed", recoveryFailure.GetType().Name);
            }
            catch (Exception outcomeFailure)
            {
                throw new AggregateException("Recovery failed and its durable control-plane outcome could not be recorded.",
                    recoveryFailure, outcomeFailure);
            }
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await disposalGate.WaitAsync();
        Guid? dropAttempt = null;
        try
        {
            if (disposed) return;
            // Verify while the target is still reachable. Any mismatch leaves it quarantined.
            await VerifyOwnershipAsync();
            dropAttempt = await Rev869BControlPlaneRegistry.BeginLeaseDropAsync(
                reservation, "OwnedActive", markerFingerprint, "Dropped");
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(adminConnectionString);
            await admin.OpenAsync();
            await RequireCurrentDatabaseAsync(admin, "postgres", "administrative DROP boundary");
            RequireSafeOwnedName(DatabaseName);
            await RequireNoTargetConnectionsAsync(admin, DatabaseName);
            var quoted = new NpgsqlCommandBuilder().QuoteIdentifier(DatabaseName);
            await new NpgsqlCommand($"DROP DATABASE {quoted}", admin).ExecuteNonQueryAsync();
            await RequireDatabaseAbsentAsync(admin, DatabaseName);
            await DropOwnedRolesAsync(admin, runtimeRole, issuerRole, RunId);
            await Rev869BControlPlaneRegistry.RecordLeaseDropOutcomeAsync(
                dropAttempt.Value, reservation, "OwnedActive", "Dropped", markerFingerprint, "Succeeded", null);
            await WriteEvidenceAsync("Dropped");
            if (string.Equals(Environment.GetEnvironmentVariable("REV869B_COMMAND_ISSUER_CONNECTION"), issuerConnectionString, StringComparison.Ordinal))
                Environment.SetEnvironmentVariable("REV869B_COMMAND_ISSUER_CONNECTION", previousIssuerConnection);
            disposed = true;
        }
        catch (Exception disposalFailure)
        {
            Exception? registryFailure = null;
            if (dropAttempt.HasValue)
            {
                try
                {
                    await Rev869BControlPlaneRegistry.RecordLeaseDropOutcomeAsync(
                        dropAttempt.Value, reservation, "OwnedActive", "Quarantined", markerFingerprint,
                        "Failed", disposalFailure.GetType().Name);
                }
                catch (Exception error) { registryFailure = error; }
            }
            Exception? quarantineFailure = null;
            try { await MarkQuarantinedAsync(); }
            catch (Exception error) { quarantineFailure = error; }
            if (registryFailure is not null || quarantineFailure is not null)
                throw new AggregateException("Cleanup failed and durable quarantine reconciliation was incomplete.",
                    new[] { disposalFailure, registryFailure, quarantineFailure }.Where(x => x is not null).Cast<Exception>());
            throw;
        }
        finally
        {
            disposalGate.Release();
        }
    }

    private async Task MarkQuarantinedAsync()
    {
        await using var connection = new NpgsqlConnection(ownerConnectionString);
        await connection.OpenAsync();
        await RequireCurrentDatabaseAsync(connection, DatabaseName, "quarantine marker boundary");
        var quarantinedFingerprint = ComputeMarkerFingerprint("Quarantined");
        await using var command = new NpgsqlCommand($$"""
                UPDATE nexa.{{MarkerTable}} SET "QuarantineState"='Quarantined',"RegistryState"='Quarantined',
                  "MarkerFingerprint"=@quarantinedFingerprint
                WHERE "OwnershipToken"=@token AND "RunId"=@run AND "DatabaseName"=@database
                  AND "ControlPlanePolicy"=@policy AND "SourceCommitFingerprint"=@sourceCommit
                  AND "SourceDatabase"=@source AND "SourceFingerprint"=@sourceFingerprint
                  AND "MigrationId"=@migration AND "MigrationFingerprint"=@migrationFingerprint
                  AND "FixtureFamily"=@family AND "ExpectedOwner"=current_user
                  AND "LeaseRequestedAt"=@requested AND "LeaseExpiresAt"=@leaseExpires
                  AND "MarkerFingerprint"=@activeFingerprint
                """, connection);
        command.Parameters.AddWithValue("token", OwnershipToken);
        command.Parameters.AddWithValue("run", RunId);
        command.Parameters.AddWithValue("database", DatabaseName);
        command.Parameters.AddWithValue("policy", Rev869BControlPlaneRegistry.Policy);
        command.Parameters.AddWithValue("sourceCommit", reservation.SourceCommitFingerprint);
        command.Parameters.AddWithValue("source", ExactSourceDatabase);
        command.Parameters.AddWithValue("sourceFingerprint", sourceFingerprint);
        command.Parameters.AddWithValue("migration", MigrationId);
        command.Parameters.AddWithValue("migrationFingerprint", migrationFingerprint);
        command.Parameters.AddWithValue("family", Family);
        command.Parameters.AddWithValue("requested", reservation.RequestedAt);
        command.Parameters.AddWithValue("leaseExpires", reservation.LeaseExpiresAt);
        command.Parameters.AddWithValue("activeFingerprint", markerFingerprint);
        command.Parameters.AddWithValue("quarantinedFingerprint", quarantinedFingerprint);
        if (await command.ExecuteNonQueryAsync() != 1)
            throw new InvalidOperationException("Exact quarantine marker update failed.");
        await Rev869BControlPlaneRegistry.BindMarkerAndOutcomeAsync(
            reservation, "OwnedActive", "Quarantined", quarantinedFingerprint, "Failed", "CleanupFailure");
        markerFingerprint = quarantinedFingerprint;
        await WriteEvidenceAsync("Quarantined");
    }

    private async Task WriteEvidenceAsync(string state)
    {
        var evidence = new QuarantineEvidence(DatabaseName, RunId,
            Convert.ToHexString(SHA256.HashData(Convert.FromHexString(OwnershipToken))), Family,
            scenarioHash, ExactSourceDatabase, sourceFingerprint, reservation.SourceCommitFingerprint,
            MigrationId, migrationFingerprint, expectedOwner, reservation.RequestedAt, reservation.LeaseExpiresAt,
            runtimeRole, issuerRole, Rev869BControlPlaneRegistry.Policy, state,
            state == "PreCreateIntent" ? null : markerFingerprint);
        await WriteEvidenceAsync(evidence);
    }

    private static async Task WriteEvidenceAsync(QuarantineEvidence evidence)
    {
        var payload = JsonSerializer.Serialize(evidence);
        var signature = Convert.ToHexString(HMACSHA256.HashData(RecoveryEvidenceKey(), Encoding.UTF8.GetBytes(payload)));
        var path = EvidencePath(evidence.DatabaseName, evidence.RunId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new SignedQuarantineEvidence(payload, signature)), Encoding.UTF8);
    }

    private static async Task<QuarantineEvidence> ReadVerifiedEvidenceAsync(string databaseName, string runId)
    {
        var path = EvidencePath(databaseName, runId);
        var envelope = JsonSerializer.Deserialize<SignedQuarantineEvidence>(await File.ReadAllTextAsync(path, Encoding.UTF8))
            ?? throw new InvalidOperationException("Signed quarantine evidence is missing.");
        var expected = HMACSHA256.HashData(RecoveryEvidenceKey(), Encoding.UTF8.GetBytes(envelope.Payload));
        var supplied = Convert.FromHexString(envelope.Signature);
        if (!CryptographicOperations.FixedTimeEquals(expected, supplied))
            throw new InvalidOperationException("Signed quarantine evidence verification failed.");
        return JsonSerializer.Deserialize<QuarantineEvidence>(envelope.Payload)
            ?? throw new InvalidOperationException("Signed quarantine evidence payload is invalid.");
    }

    private static string EvidencePath(string databaseName, string runId)
    {
        RequireSafeOwnedName(databaseName);
        if (runId.Length != 32 || runId.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException("Exact evidence run identifier is required.");
        var directory = Environment.GetEnvironmentVariable("REV869B_QUARANTINE_EVIDENCE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory))
            throw new InvalidOperationException("A fully qualified protected quarantine evidence directory is required.");
        return Path.Combine(Path.GetFullPath(directory), $"{databaseName}.{runId}.json");
    }

    private static byte[] RecoveryEvidenceKey()
    {
        var value = Environment.GetEnvironmentVariable("REV869B_QUARANTINE_RECOVERY_KEY");
        if (value is null || value.Length != 64 || value.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException("A separately managed 256-bit quarantine evidence key is required.");
        return Convert.FromHexString(value);
    }

    private static byte[] RecoveryAuthorizationKey()
    {
        var value = Environment.GetEnvironmentVariable("REV869B_QUARANTINE_RECOVERY_AUTHORIZATION_KEY");
        if (value is null || value.Length != 64 || value.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException("A separately governed 256-bit recovery authorization key is required.");
        var evidenceKey = Environment.GetEnvironmentVariable("REV869B_QUARANTINE_RECOVERY_KEY");
        if (string.Equals(value, evidenceKey, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Recovery authorization and quarantine evidence keys must be distinct.");
        return Convert.FromHexString(value);
    }

    private static async Task ConsumeRecoveryAuthorizationAsync(string databaseName, string runId,
        RecoveryAuthorization authorization, string canonical)
    {
        var directory = Path.GetDirectoryName(EvidencePath(databaseName, runId))!;
        var consumedDirectory = Path.Combine(directory, "consumed-authorizations");
        Directory.CreateDirectory(consumedDirectory);
        var path = Path.Combine(consumedDirectory, $"{authorization.AuthorizationId:N}.json");
        var evidence = JsonSerializer.Serialize(new
        {
            authorization.AuthorizationId,
            authorization.Nonce,
            authorization.Purpose,
            authorization.IssuedAt,
            authorization.ExpiresAt,
            TargetFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))),
            ConsumedAt = DateTimeOffset.UtcNow
        });
        try
        {
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.WriteThrough | FileOptions.Asynchronous);
            await JsonSerializer.SerializeAsync(stream, JsonSerializer.Deserialize<JsonElement>(evidence));
            await stream.FlushAsync();
        }
        catch (IOException error)
        {
            throw new InvalidOperationException("Recovery authorization was already consumed or cannot be durably reserved.", error);
        }
    }

    private static void RequireApprovedRetentionConfiguration()
    {
        static string Required(string name) => Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"{name} is required before any REV869B database/helper execution.");
        if (Required("REV869B_SECURITY_LEDGER_POLICY_APPROVAL") != ApprovedRetentionPolicy ||
            Required("REV869B_UNCONSUMED_GRANT_MAX_MINUTES") != "15" ||
            Required("REV869B_TEMPORARY_LEDGER_RETENTION_DAYS") != "90" ||
            Required("REV869B_DURABLE_AUDIT_RETENTION_YEARS") != "10" ||
            Required("REV869B_SECURITY_LEDGER_EXPORTS") != "DISABLED")
            throw new InvalidOperationException("REV869B management-approved retention/privacy configuration is invalid.");
        if (!int.TryParse(Required("REV869B_TEMPORARY_LEDGER_PURGE_BATCH"), out var batch) || batch is < 1 or > 1000 ||
            string.IsNullOrWhiteSpace(Required("REV869B_TEMPORARY_LEDGER_PURGE_SCHEDULE_UTC")))
            throw new InvalidOperationException("REV869B temporary-ledger purge must be scheduled and bounded to 1..1000 rows.");
    }

    private sealed record SourceProof(string SourceFingerprint, string MigrationFingerprint, string OwnerPrincipal);
    private sealed record QuarantineEvidence(string DatabaseName, string RunId, string OwnershipTokenHash,
        string FixtureFamily, string ScenarioHash, string SourceDatabase, string SourceFingerprint,
        string SourceCommitFingerprint, string MigrationId, string MigrationFingerprint,
        string ExpectedOwner, DateTimeOffset ProvisionedAt, DateTimeOffset LeaseExpiresAt,
        string RuntimeRole, string IssuerRole, string ControlPlanePolicy, string State, string? MarkerFingerprint);
    private sealed record SignedQuarantineEvidence(string Payload, string Signature);
    private sealed record RecoveryAuthorization(Guid AuthorizationId, string Nonce, string Purpose,
        string ApprovalIssuer, string IssuerAuthority, string ExpectedPreState, string AuthorizedPostState,
        string ApprovalReference, string Reason, string ExecutorIdentity,
        DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt, string Signature);
}
