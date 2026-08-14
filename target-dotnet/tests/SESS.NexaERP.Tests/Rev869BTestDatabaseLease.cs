using Npgsql;

namespace SESS.NexaERP.Tests;

/// <summary>Disposable target lease owned and cleaned exclusively by the external lifecycle controller.</summary>
internal sealed class Rev869BTestDatabaseLease : IAsyncDisposable
{
    internal const string DatabasePrefix = "sess_nexaerp_rev869b_";
    internal const string ExactSourceDatabase = "sess_nexaerp";
    private readonly Rev869BLifecycleControllerClient controller;
    private readonly Rev869BLifecycleControllerClient.LeaseAllocation allocation;
    private bool disposed;

    private Rev869BTestDatabaseLease(Rev869BLifecycleControllerClient controller,
        Rev869BLifecycleControllerClient.LeaseAllocation allocation, string family)
    {
        this.controller = controller;
        this.allocation = allocation;
        Family = family;
    }

    internal string ConnectionString => allocation.RuntimeConnectionString;
    internal string VerifierConnectionString => allocation.VerifierConnectionString;
    internal string DatabaseName => allocation.DatabaseName;
    internal string RunId => allocation.RunId;
    internal string OwnershipToken => allocation.OwnershipNonceSha256;
    internal string Family { get; }
    internal string MarkerFingerprint => allocation.MarkerSha256;
    internal Guid LeaseId => allocation.LeaseId;
    internal long LeaseVersion => allocation.Version;

    internal static async Task<Rev869BTestDatabaseLease> CreateAsync(string scenario, string family)
    {
        var controller = Rev869BLifecycleControllerClient.Create();
        try
        {
            var allocation = await controller.AllocateAsync(scenario, family);
            return new(controller, allocation, family);
        }
        catch
        {
            await controller.DisposeAsync();
            throw;
        }
    }

    internal async Task<NpgsqlConnection> OpenVerifiedConnectionAsync()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var identity = new NpgsqlCommand("SELECT current_database(),session_user", connection);
        await using var reader = await identity.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || reader.GetString(0) != DatabaseName || reader.GetString(1) != "nexa_rev869b_app_runtime")
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException("Runtime target identity does not match the controller allocation.");
        }
        return connection;
    }

    internal static async Task RecoverQuarantinedAsync(string scenario)
    {
        await using var controller = Rev869BLifecycleControllerClient.Create();
        var evidence = await controller.RunAcceptanceScenarioAsync(Rev869BAcceptanceScenarioInventory.R03);
        if (evidence.FinalState is not ("CleanupFailed" or "Finalized"))
            throw new InvalidOperationException("Quarantine recovery did not produce an authoritative terminal state for " + scenario + ".");
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        try { await controller.ReleaseAsync(allocation.LeaseId, Guid.NewGuid()); }
        finally { await controller.DisposeAsync(); }
    }
}
