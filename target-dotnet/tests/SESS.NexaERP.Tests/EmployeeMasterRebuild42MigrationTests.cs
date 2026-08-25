using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class EmployeeMasterRebuild42MigrationTests
{
    private const string MigrationId = "20260825063221_EmployeeMasterRebuild42";
    private static readonly Assembly Infrastructure = typeof(NexaErpDbContext).Assembly;

    [Fact]
    public void Migration_is_discoverable_last_and_guards_both_directions()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect")
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrations = db.Database.GetMigrations().ToArray();

        Assert.Equal("20260825073027_CorrectManagingDirectorDepartmentPriority", migrations[Array.IndexOf(migrations, MigrationId) + 1]);
        Assert.Equal(1, migrations.Count(x => x == MigrationId));

        var source = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260825063221_EmployeeMasterRebuild42.cs");
        Assert.Equal(2, Count(source, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Contains("migrationBuilder.Sql(EmployeeMasterRebuild42Sql.Up);", source);
        Assert.Contains("migrationBuilder.Sql(EmployeeMasterRebuild42Sql.Down);", source);
    }

    [Fact]
    public void Authoritative_roster_has_exact_cardinalities_codes_and_assignments()
    {
        var roster = Rows("Roster");
        var leavers = Rows("Leavers");

        Assert.Equal(42, roster.Length);
        Assert.Equal(9, leavers.Length);
        Assert.Equal(42, roster.Select(x => Text(x, "Code")).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(Enumerable.Range(1, 42).Select(i => $"SESS-{i:00}"), roster.Select(x => Text(x, "Code")));
        Assert.Equal(30, roster.Count(x => Value(x, "OldCode") is not null));
        Assert.Equal(12, roster.Count(x => Value(x, "NewEmployeeId") is not null));
        Assert.Equal(157, roster.Sum(x => Strings(x, "SecondaryDepartments").Length));
        Assert.DoesNotContain(roster, x => Text(x, "PrimaryDepartment") == "MAINTENANCE");
        Assert.DoesNotContain(roster.SelectMany(x => Strings(x, "SecondaryDepartments")), x => x == "MAINTENANCE");
        Assert.All(roster, x => Assert.DoesNotContain(Text(x, "PrimaryDepartment"), Strings(x, "SecondaryDepartments")));
    }

    [Fact]
    public void Manikandan_Parameshwaran_and_Stores_Accounts_decisions_are_exact()
    {
        var roster = Rows("Roster");
        var manikandans = roster.Where(x => Text(x, "Name") == "MANIKANDAN S").ToArray();
        Assert.Equal(2, manikandans.Length);

        AssertRow(Assert.Single(manikandans, x => Text(x, "Code") == "SESS-09"), "SESS-009", "2024-01-02", "REFRIGERATION");
        AssertRow(Assert.Single(manikandans, x => Text(x, "Code") == "SESS-27"), "SESS-030", "2025-09-01", "ELECTRICAL");

        var parameshwaran = Assert.Single(roster, x => Text(x, "Name") == "PARAMESHWARAN S");
        Assert.Equal("SESS-13", Text(parameshwaran, "Code"));
        Assert.Null(Value(parameshwaran, "OldCode"));

        var karthick = Assert.Single(roster, x => Text(x, "Code") == "SESS-41");
        Assert.Equal("STORES", Text(karthick, "PrimaryDepartment"));
        Assert.Equal(["ACCOUNTS"], Strings(karthick, "SecondaryDepartments"));
    }

    [Fact]
    public void Designations_reuse_exact_names_and_create_only_missing_exact_names()
    {
        var requested = Rows("Roster").Select(x => Text(x, "DesignationName")).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var seeded = Rev866SeedData.Designations.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var reused = requested.Where(seeded.Contains).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var created = requested.Where(x => !seeded.Contains(x)).Order(StringComparer.OrdinalIgnoreCase).ToArray();

        Assert.Equal(8, reused.Length);
        Assert.Equal(18, created.Length);
        Assert.Contains("Refrigeration Engineer", created);
        Assert.Contains("Junior Accountant", created);
    }

    [Fact]
    public void Sql_is_fail_closed_history_preserving_and_excludes_pii_payloads()
    {
        var sqlType = Infrastructure.GetType("SESS.NexaERP.Infrastructure.Persistence.Migrations.EmployeeMasterRebuild42Sql", true)!;
        var up = (string)sqlType.GetProperty("Up", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var dataType = Infrastructure.GetType("SESS.NexaERP.Infrastructure.Persistence.Migrations.EmployeeMasterRebuild42Data", true)!;
        var rosterType = dataType.GetNestedType("RosterRow", BindingFlags.NonPublic)!;
        var rosterProperties = rosterType.GetProperties().Select(x => x.Name).ToArray();

        Assert.Contains("requires PostgreSQL 17 or later", up);
        Assert.Contains("name match is missing or ambiguous", up);
        Assert.Contains("DOJ contradicts 2024-01-02", up);
        Assert.Contains("DOJ contradicts 2025-09-01", up);
        Assert.Contains("DATE '2026-06-24'", up);
        Assert.Contains("\"Status\"='LEFT'", up);
        Assert.Contains("Maintenance must be active and empty", up);
        Assert.Contains("mobile PII was populated", up);
        Assert.DoesNotContain(rosterProperties, x => new[] { "Aadhaar", "Aadhar", "Pan", "Uan", "Esi", "BankAccount", "Ifsc", "Mobile", "EmergencyContact" }.Contains(x, StringComparer.OrdinalIgnoreCase));
        Assert.DoesNotContain("ALTER TABLE", up, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE TABLE", up, StringComparison.OrdinalIgnoreCase);
    }

    private static object[] Rows(string fieldName)
    {
        var type = Infrastructure.GetType("SESS.NexaERP.Infrastructure.Persistence.Migrations.EmployeeMasterRebuild42Data", true)!;
        var value = (IEnumerable)type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        return value.Cast<object>().ToArray();
    }

    private static object? Value(object row, string property) => row.GetType().GetProperty(property)!.GetValue(row);
    private static string Text(object row, string property) => (string)Value(row, property)!;
    private static string[] Strings(object row, string property) => (string[])Value(row, property)!;

    private static void AssertRow(object row, string oldCode, string doj, string department)
    {
        Assert.Equal(oldCode, Text(row, "OldCode"));
        Assert.Equal(doj, Text(row, "Doj"));
        Assert.Equal(department, Text(row, "PrimaryDepartment"));
    }

    private static int Count(string text, string value) => (text.Length - text.Replace(value, "", StringComparison.Ordinal).Length) / value.Length;

    private static string Read(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine([root, .. parts]));
    }
}