namespace SESS.NexaERP.Infrastructure.MasterData;

public static class MasterImportPolicy
{
    public static bool RequiresWholeFile(string masterKey) =>
        masterKey.Trim().ToLowerInvariant() is "vendors" or "items" or "uoms" or "manufacturers";
}
