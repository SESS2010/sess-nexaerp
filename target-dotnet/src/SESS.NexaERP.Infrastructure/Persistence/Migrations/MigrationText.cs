namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Raw C# string literals carry the line endings of the source file they were compiled
/// from. A worktree checked out with CRLF therefore compiles CRLF into migration SQL and
/// into the fragments migrations search for inside earlier baselines. Every derivation
/// and every comparison must normalize to LF before it looks at the text.
/// </summary>
internal static class MigrationText
{
    internal static string Lf(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal);
}
