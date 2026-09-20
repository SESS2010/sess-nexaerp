using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Emits every raw SQL operation with LF line endings, whatever line endings the source
/// files were compiled with. Function bodies installed from a CRLF worktree are then
/// byte-identical to those installed from an LF checkout, and every stored-body guard
/// (which already normalizes <c>prosrc</c>) compares like with like. Escape sequences such
/// as <c>E'\r\n'</c> are backslash text, not line endings, and are left untouched.
/// </summary>
#pragma warning disable EF1001 // The provider generator's constructor takes its singleton options.
public sealed class LineEndingNormalizingMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    INpgsqlSingletonOptions npgsqlSingletonOptions)
    : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlSingletonOptions)
#pragma warning restore EF1001
{
    protected override void Generate(SqlOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        var normalized = MigrationText.Lf(operation.Sql);
        if (ReferenceEquals(normalized, operation.Sql) || normalized == operation.Sql)
        {
            base.Generate(operation, model, builder);
            return;
        }
        var replacement = new SqlOperation { Sql = normalized, SuppressTransaction = operation.SuppressTransaction };
        foreach (var annotation in operation.GetAnnotations())
            replacement.AddAnnotation(annotation.Name, annotation.Value);
        base.Generate(replacement, model, builder);
    }
}
