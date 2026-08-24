using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void GeneratedBaselineScriptsHaveBalancedPostgreSqlSyntax()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=advance_sql_syntax_no_connect;Username=no_connect")
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var baselineMigration = Assert.Single(db.Database.GetMigrations());
        AssertBalanced("Up", migrator.GenerateScript("0", baselineMigration));
        AssertBalanced("Down", migrator.GenerateScript(baselineMigration, "0"));
    }

    [Theory]
    [InlineData("CHECK (\"unterminated)")]
    [InlineData("SELECT 'unterminated")]
    [InlineData("SELECT (1")]
    [InlineData("DO $body$ BEGIN PERFORM (1; END $body$;")]
    [InlineData("DO $body$ BEGIN END;")]
    public void ValidatorRejectsEachUnbalancedPostgreSqlDelimiter(string sql) =>
        Assert.NotEmpty(PostgreSqlBalanceValidator.Validate(sql));

    private static void AssertBalanced(string direction, string sql)
    {
        var errors = PostgreSqlBalanceValidator.Validate(sql);
        Assert.True(errors.Count == 0,
            $"Generated {direction} SQL contains unbalanced PostgreSQL syntax:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    private static class PostgreSqlBalanceValidator
    {
        public static IReadOnlyList<string> Validate(string sql)
        {
            var errors = new List<string>();
            ValidateRegion(sql, 0, sql.Length, errors);
            return errors;
        }

        private static void ValidateRegion(string sql, int start, int end, List<string> errors)
        {
            var parentheses = new Stack<int>();
            var index = start;
            while (index < end)
            {
                if (StartsWith(sql, index, end, "--"))
                {
                    index += 2;
                    while (index < end && sql[index] != '\n') index++;
                    continue;
                }
                if (StartsWith(sql, index, end, "/*"))
                {
                    var opening = index;
                    index += 2;
                    var depth = 1;
                    while (index < end && depth > 0)
                    {
                        if (StartsWith(sql, index, end, "/*")) { depth++; index += 2; }
                        else if (StartsWith(sql, index, end, "*/")) { depth--; index += 2; }
                        else index++;
                    }
                    if (depth > 0) errors.Add(At(sql, opening, "unterminated block comment"));
                    continue;
                }
                if (sql[index] == '\'')
                {
                    var opening = index;
                    var escapeString = index > start && (sql[index - 1] is 'E' or 'e') &&
                        (index - 1 == start || !IsIdentifierPart(sql[index - 2]));
                    index++;
                    var closed = false;
                    while (index < end)
                    {
                        if (escapeString && sql[index] == '\\' && index + 1 < end) { index += 2; continue; }
                        if (sql[index] != '\'') { index++; continue; }
                        if (index + 1 < end && sql[index + 1] == '\'') { index += 2; continue; }
                        index++;
                        closed = true;
                        break;
                    }
                    if (!closed) errors.Add(At(sql, opening, "unterminated single-quoted string"));
                    continue;
                }
                if (sql[index] == '"')
                {
                    var opening = index++;
                    var closed = false;
                    while (index < end)
                    {
                        if (sql[index] != '"') { index++; continue; }
                        if (index + 1 < end && sql[index + 1] == '"') { index += 2; continue; }
                        index++;
                        closed = true;
                        break;
                    }
                    if (!closed) errors.Add(At(sql, opening, "unterminated double-quoted identifier"));
                    continue;
                }
                if (sql[index] == '$' && TryReadDollarDelimiter(sql, index, end, out var delimiter))
                {
                    var bodyStart = index + delimiter.Length;
                    var close = sql.IndexOf(delimiter, bodyStart, end - bodyStart, StringComparison.Ordinal);
                    if (close < 0)
                    {
                        errors.Add(At(sql, index, $"unterminated dollar-quoted body {delimiter}"));
                        index = end;
                        continue;
                    }
                    ValidateRegion(sql, bodyStart, close, errors);
                    index = close + delimiter.Length;
                    continue;
                }
                if (sql[index] == '(') parentheses.Push(index);
                else if (sql[index] == ')')
                {
                    if (parentheses.Count == 0) errors.Add(At(sql, index, "unmatched closing parenthesis"));
                    else parentheses.Pop();
                }
                index++;
            }
            foreach (var opening in parentheses.Reverse()) errors.Add(At(sql, opening, "unmatched opening parenthesis"));
        }

        private static bool TryReadDollarDelimiter(string sql, int start, int end, out string delimiter)
        {
            var index = start + 1;
            if (index < end && sql[index] == '$') { delimiter = "$$"; return true; }
            if (index >= end || !(char.IsAsciiLetter(sql[index]) || sql[index] == '_'))
            {
                delimiter = string.Empty;
                return false;
            }
            index++;
            while (index < end && IsIdentifierPart(sql[index])) index++;
            if (index >= end || sql[index] != '$')
            {
                delimiter = string.Empty;
                return false;
            }
            delimiter = sql[start..(index + 1)];
            return true;
        }

        private static bool StartsWith(string sql, int index, int end, string value) =>
            index + value.Length <= end && sql.AsSpan(index, value.Length).SequenceEqual(value);
        private static bool IsIdentifierPart(char value) => char.IsAsciiLetterOrDigit(value) || value == '_';
        private static string At(string sql, int index, string message)
        {
            var line = 1;
            var column = 1;
            for (var offset = 0; offset < index; offset++)
            {
                if (sql[offset] == '\n') { line++; column = 1; }
                else column++;
            }
            var lineStart = sql.LastIndexOf('\n', Math.Max(0, index - 1)) + 1;
            var lineEnd = sql.IndexOf('\n', index);
            if (lineEnd < 0) lineEnd = sql.Length;
            return $"line {line}, column {column}: {message}: {sql[lineStart..lineEnd].Trim()}";
        }
    }
}
