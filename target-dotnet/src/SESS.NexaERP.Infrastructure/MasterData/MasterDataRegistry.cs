using SESS.NexaERP.Application.Masters;

namespace SESS.NexaERP.Infrastructure.MasterData;

public sealed class MasterDataRegistry(IEnumerable<IMasterDataAdapter> adapters) : IMasterDataRegistry
{
    private readonly IReadOnlyDictionary<string, IMasterDataAdapter> entries = adapters.ToDictionary(
        x => x.Definition.MasterKey,
        StringComparer.OrdinalIgnoreCase);

    public IMasterDataAdapter GetRequired(string masterKey) =>
        TryGet(masterKey, out var adapter)
            ? adapter!
            : throw new MasterDataNotFoundException($"Master data definition '{masterKey}' is not enabled.");

    public bool TryGet(string masterKey, out IMasterDataAdapter? adapter)
    {
        if (string.IsNullOrWhiteSpace(masterKey))
        {
            adapter = null;
            return false;
        }
        return entries.TryGetValue(masterKey.Trim(), out adapter);
    }
}
