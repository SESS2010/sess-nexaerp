using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class ManagingDirectorDepartmentPriorityMigrationTests
{
    private const string MigrationId = "20260825073027_CorrectManagingDirectorDepartmentPriority";
    private static readonly Assembly Infrastructure = typeof(NexaErpDbContext).Assembly;

    [Fact]
    public void Follow_up_migration_is_last_and_guards_both_directions()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect")
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrations = db.Database.GetMigrations().ToArray();

        Assert.Equal("20260825092016_AuthenticationBootstrapFoundation", migrations[Array.IndexOf(migrations, MigrationId) + 1]);
        Assert.Equal(1, migrations.Count(x => x == MigrationId));

        var source = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260825073027_CorrectManagingDirectorDepartmentPriority.cs");
        Assert.Equal(2, Count(source, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Contains("migrationBuilder.Sql(ManagingDirectorDepartmentPrioritySql.Up);", source);
        Assert.Contains("migrationBuilder.Sql(ManagingDirectorDepartmentPrioritySql.Down);", source);
    }

    [Fact]
    public void Up_is_effective_dated_and_enforces_the_exact_department_swap()
    {
        var up = Sql("Up");

        Assert.Contains("SESS-02 assignment state is missing or ambiguous", up);
        Assert.Contains("\"EffectiveTo\"=DATE '2026-08-25'", up);
        Assert.Equal(2, Count(up, "\"EffectiveFrom\",\"IsPrimary\""));
        Assert.Contains("'PRIMARY',DATE '2026-08-26',true", up);
        Assert.Contains("'SECONDARY',DATE '2026-08-26',false", up);
        Assert.Contains("\"DepartmentId\"=x.management_department_id", up);
        Assert.Contains("exactly one active Management primary", up);
        Assert.Contains("exactly one active Accounts secondary", up);
        Assert.Contains("SESS-02 has unexpected active department assignments", up);
        Assert.DoesNotContain("SET \"DepartmentId\"=management_department_id", up);
        Assert.DoesNotContain("SET \"IsPrimary\"=", up);
        Assert.DoesNotContain("SET \"AssignmentType\"=", up);
    }

    [Fact]
    public void Down_is_guarded_and_restores_only_the_two_superseded_rebuild_rows()
    {
        var down = Sql("Down");

        Assert.Contains("requires PostgreSQL 17 or later", down);
        Assert.Contains("refuses a PostgreSQL administrative database", down);
        Assert.Contains("correction rollback state is missing or ambiguous", down);
        Assert.Contains("\"CreatedBy\"='MD_DEPARTMENT_PRIORITY_CORRECTION'", down);
        Assert.Contains("\"CreatedBy\"='EMPLOYEE_MASTER_REBUILD_42'", down);
        Assert.Contains("\"DepartmentId\"=x.accounts_department_id", down);
    }

    private static string Sql(string property)
    {
        var type = Infrastructure.GetType("SESS.NexaERP.Infrastructure.Persistence.Migrations.ManagingDirectorDepartmentPrioritySql", true)!;
        return (string)type.GetProperty(property, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
    }

    private static int Count(string text, string value) =>
        (text.Length - text.Replace(value, "", StringComparison.Ordinal).Length) / value.Length;

    private static string Read(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine([root, .. parts]));
    }
}
