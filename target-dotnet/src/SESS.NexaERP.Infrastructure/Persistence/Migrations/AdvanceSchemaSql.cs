namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class AdvanceSchemaSql
{
    internal const string Token = "__advance_schema__";

    internal static string Expand(string template)
    {
        if (!template.Contains(Token, StringComparison.Ordinal))
            throw new InvalidOperationException("Advance migration SQL must contain the centralized schema token.");

        return template.Replace(Token, DatabaseSchemas.Advance, StringComparison.Ordinal);
    }
}