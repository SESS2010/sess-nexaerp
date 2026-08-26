using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class AuthenticationBootstrapFoundationTests
{
    private const string MigrationId = "20260825092016_AuthenticationBootstrapFoundation";
    private static readonly Assembly Infrastructure = typeof(NexaErpDbContext).Assembly;

    [Fact]
    public void Migration_is_guarded_precedes_part1_and_does_not_replace_existing_permission_rows()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect")
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrations = db.Database.GetMigrations().ToArray();
        Assert.Equal("20260825125621_MultiCompanyEmployeeAuthorizationPart1", migrations[Array.IndexOf(migrations, MigrationId) + 1]);

        var source = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", MigrationId + ".cs");
        Assert.Equal(2, Count(source, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        var up = source[..source.IndexOf("protected override void Down", StringComparison.Ordinal)];
        Assert.DoesNotContain("DeleteData(", up);
        Assert.Equal(2, Count(up, "new Guid(\"82000000-0000-0000-0000-00000000000"));
    }

    [Fact]
    public void Canonical_roles_preserve_role_ids_and_it_permission_ids()
    {
        var roles = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Concat(Rev869ASeedData.Roles)
            .Append(AdvanceSeedData.DepartmentManagerRole)
            .GroupBy(x => x.Id).Select(x => x.First()).ToArray();
        Assert.Equal(43, roles.Length);
        Assert.All(roles, role => Assert.Equal(role.Code.Trim().ToUpperInvariant(), role.Code));

        var it = Assert.Single(roles, role => role.Code == "IT_MANAGER");
        Assert.Equal(Guid.Parse("10000000-0000-0000-0000-000000000014"), it.Id);
        var permissions = Rev866SeedData.RolePagePermissions.Concat(Rev869ASeedData.RolePagePermissions)
            .Where(x => x.RoleId == it.Id).ToArray();
        Assert.Equal(28, permissions.Length);
        Assert.Equal(28, permissions.Select(x => x.Id).Distinct().Count());
        Assert.Contains(permissions, x => x.Id == Rev869ASeedData.ItManagerEmployeeIdentitiesPermissionId);
        Assert.Contains(permissions, x => x.Id == Rev869ASeedData.ItManagerOperationalScopesPermissionId);
        Assert.All(permissions, x => Assert.False(x.HasFullControl));
    }

    [Fact]
    public void Sql_stops_on_collision_and_protects_consumed_bootstrap_from_rollback()
    {
        var preUp = Sql("PreUp");
        var postUp = Sql("PostUp");
        var preDown = Sql("PreDown");

        Assert.Contains("would create a case or semantic collision", preUp);
        Assert.Contains("expected exactly 1,086 permissions", preUp);
        Assert.Contains("expected 1,088 permissions", postUp);
        Assert.Contains("bootstrap has been used or its state changed", preDown);
        Assert.Contains("IT_MANAGER now has employee assignments", preDown);
    }

    [Fact]
    public void Installer_principal_contract_is_secret_safe_replay_safe_and_least_privilege()
    {
        var program = Read("src", "SESS.NexaERP.Installer", "Program.cs");
        var sql = Read("src", "SESS.NexaERP.Installer", "DatabasePrincipalProvisioningSql.cs");

        Assert.Contains("database-principals <plan|status|provision>", program);
        Assert.Contains("NEXAERP_MIGRATION_PASSWORD", program);
        Assert.Contains("contain at least 24 characters", program);
        Assert.DoesNotContain("Password=", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pg_advisory_xact_lock", sql);
        Assert.Contains("Partial NexaERP principal state", sql);
        Assert.Contains("GRANT SELECT,INSERT,UPDATE ON TABLE", sql);
        Assert.Contains("has_table_privilege('nexa_erp_runtime',c.oid,'DELETE')", sql);
        Assert.Contains("REVOKE ALL ON TABLE advance.authentication_bootstrap_state", sql);
        Assert.Contains("Replays reconcile ownership and grants but never rotate existing credentials", sql);
        Assert.Contains("complete_authentication_bootstrap(text,text)", sql);
        Assert.Contains("Ceremony function EXECUTE ACL must grant only nexa_erp_bootstrap", sql);
        Assert.Contains("ROLE_STATUS role=", program);
        Assert.Contains("ceremony_execute_grant=", program);
    }

    private static string Sql(string property)
    {
        var type = Infrastructure.GetType("SESS.NexaERP.Infrastructure.Persistence.Migrations.AuthenticationBootstrapFoundationSql", true)!;
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
