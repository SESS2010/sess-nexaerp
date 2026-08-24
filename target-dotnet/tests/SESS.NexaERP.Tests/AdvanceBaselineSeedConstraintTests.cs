using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class AdvanceBaselineSeedConstraintTests
{
    private static readonly IModel Model = CreateModel();

    [Fact]
    public void EveryModelSeedSatisfiesPrimaryAlternateAndUniqueKeys()
    {
        foreach (var entity in Model.GetEntityTypes())
        {
            var seeds = entity.GetSeedData().ToArray();
            foreach (var key in entity.GetKeys())
                AssertUnique(entity, key.IsPrimaryKey() ? "primary key" : "alternate key", key.Properties, seeds, nullsDistinct: false, filter: null);

            foreach (var index in entity.GetIndexes().Where(index => index.IsUnique))
            {
                var nullsDistinct = index.FindAnnotation("Npgsql:NullsDistinct")?.Value as bool? ?? true;
                AssertUnique(entity, "unique index", index.Properties, seeds, nullsDistinct, index.GetFilter());
            }
        }
    }

    [Fact]
    public void GeneratedBaselineInsertOperationsSatisfyEveryModelUniqueConstraint()
    {
        using var db = CreateContext();
        var migrations = db.GetService<IMigrationsAssembly>();
        var migration = migrations.CreateMigration(Assert.Single(migrations.Migrations).Value, db.Database.ProviderName!);
        var rowsByEntity = Model.GetEntityTypes().ToDictionary(entity => entity, _ => new List<IDictionary<string, object?>>());
        var entitiesByTable = Model.GetEntityTypes().Where(entity => entity.GetTableName() is not null)
            .ToDictionary(entity => (Schema: entity.GetSchema(), Table: entity.GetTableName()!));

        foreach (var insert in migration.UpOperations.OfType<InsertDataOperation>())
        {
            var entity = entitiesByTable[(insert.Schema, insert.Table)];
            var store = StoreObjectIdentifier.Table(insert.Table, insert.Schema);
            var propertiesByColumn = entity.GetProperties().ToDictionary(property => property.GetColumnName(store)!);
            for (var rowIndex = 0; rowIndex < insert.Values.GetLength(0); rowIndex++)
            {
                var row = new Dictionary<string, object?>();
                for (var columnIndex = 0; columnIndex < insert.Columns.Length; columnIndex++)
                    row[propertiesByColumn[insert.Columns[columnIndex]].Name] = insert.Values[rowIndex, columnIndex];
                rowsByEntity[entity].Add(row);
            }
        }

        foreach (var entity in Model.GetEntityTypes())
        {
            var rows = rowsByEntity[entity];
            Assert.Equal(entity.GetSeedData().Count(), rows.Count);
            foreach (var key in entity.GetKeys())
                AssertUnique(entity, key.IsPrimaryKey() ? "migration primary key" : "migration alternate key", key.Properties, rows, nullsDistinct: false, filter: null);
            foreach (var index in entity.GetIndexes().Where(index => index.IsUnique))
            {
                var nullsDistinct = index.FindAnnotation("Npgsql:NullsDistinct")?.Value as bool? ?? true;
                AssertUnique(entity, "migration unique index", index.Properties, rows, nullsDistinct, index.GetFilter());
            }
        }
    }

    [Fact]
    public void EveryModelSeedSatisfiesEverySupportedCheckConstraint()
    {
        var validators = new Dictionary<(string Table, string Name), CheckValidator>
        {
            [("organization_policies", "CK_organization_policy_dates")] = new(
                "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"",
                row => DateOrderIsValid(row, "EffectiveFrom", "EffectiveTo")),
            [("purchase_transaction_approval_policies", "CK_purchase_transaction_policy_amounts")] = new(
                "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" >= \"MinimumAmount\")",
                row => DecimalValue(row, "MinimumAmount") >= 0m &&
                    (Value(row, "MaximumAmount") is null || DecimalValue(row, "MaximumAmount") >= DecimalValue(row, "MinimumAmount"))),
            [("purchase_transaction_approval_policies", "CK_purchase_transaction_policy_dates")] = new(
                "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"",
                row => DateOrderIsValid(row, "EffectiveFrom", "EffectiveTo"))
        };
        var observed = new HashSet<(string Table, string Name)>();

        foreach (var entity in SeededEntities())
        {
            var table = entity.GetTableName()!;
            var seeds = entity.GetSeedData().ToArray();
            foreach (var check in entity.GetCheckConstraints())
            {
                var identity = (table, check.Name!);
                Assert.True(validators.TryGetValue(identity, out var validator),
                    $"Seeded table {table} has unsupported check constraint {check.Name}: {check.Sql}");
                observed.Add(identity);
                Assert.Equal(validator!.Sql, check.Sql);
                var violatingRows = seeds.Where(row => !validator.Predicate(row)).Select(row => SeedIdentity(entity, row)).ToArray();
                Assert.True(violatingRows.Length == 0,
                    $"Seeded table {table} violates check constraint {check.Name}: {string.Join(", ", violatingRows)}");
            }
        }

        Assert.Equal(validators.Keys.OrderBy(KeyText), observed.OrderBy(KeyText));
    }

    [Fact]
    public void EveryModelSeedForeignKeyReferencesAnotherSeedOrIsNull()
    {
        foreach (var entity in SeededEntities())
        {
            foreach (var row in entity.GetSeedData())
            {
                foreach (var foreignKey in entity.GetForeignKeys())
                {
                    var dependentValues = foreignKey.Properties.Select(property => Value(row, property.Name)).ToArray();
                    if (dependentValues.Any(value => value is null)) continue;
                    var principalRows = foreignKey.PrincipalEntityType.GetSeedData();
                    var found = principalRows.Any(principal => foreignKey.PrincipalKey.Properties
                        .Select((property, index) => Equals(Value(principal, property.Name), dependentValues[index])).All(equal => equal));
                    Assert.True(found,
                        $"Seed {SeedIdentity(entity, row)} has unresolved foreign key ({string.Join(",", foreignKey.Properties.Select(p => p.Name))}).");
                }
            }
        }
    }

    [Fact]
    public void HistoricalRolePageOverlapsAreExactlyKnownAndRev869BWinsFinalValues()
    {
        var sources = new[]
        {
            (Name: "REV866", Rows: Rev866SeedData.RolePagePermissions),
            (Name: "REV869A", Rows: Rev869ASeedData.RolePagePermissions),
            (Name: "REV869B", Rows: Rev869BSeedData.RolePagePermissions)
        };
        var roles = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Concat(Rev869ASeedData.Roles)
            .ToDictionary(role => role.Id, role => role.Code);
        var pages = FoundationSeedData.Pages.Concat(Rev869ASeedData.Pages).Concat(Rev869BSeedData.Pages)
            .ToDictionary(page => page.Id, page => page.PageKey);
        var duplicates = sources.SelectMany(source => source.Rows.Select(row => (source.Name, Row: row)))
            .GroupBy(item => (item.Row.RoleId, item.Row.PageDefinitionId))
            .Where(group => group.Count() > 1)
            .ToArray();
        var expected = new[]
        {
            "ACCOUNTS_HEAD|purchase.po",
            "MANAGING_DIRECTOR|purchase.po",
            "MANAGING_DIRECTOR|purchase.rfq",
            "PURCHASE_EXECUTIVE|purchase.rfq",
            "STORES_EXECUTIVE|purchase.po",
            "TECHNICAL_DIRECTOR|purchase.po",
            "TECHNICAL_DIRECTOR|purchase.rfq",
            "TECHNICAL_ENGINEER|purchase.rfq"
        };
        var actual = duplicates.Select(group => $"{roles[group.Key.RoleId].ToUpperInvariant()}|{pages[group.Key.PageDefinitionId]}")
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, actual);
        Assert.All(duplicates, group => Assert.Equal(new[] { "REV866", "REV869B" }, group.Select(item => item.Name)));
        Assert.All(duplicates, group =>
        {
            var final = Assert.Single(AdvanceSeedData.RolePagePermissions,
                row => row.RoleId == group.Key.RoleId && row.PageDefinitionId == group.Key.PageDefinitionId);
            var rev869B = Assert.Single(group, item => item.Name == "REV869B").Row;
            Assert.Equal(rev869B.Id, final.Id);
            Assert.Equal("migration-rev869b", final.CreatedBy);
        });
    }

    [Fact]
    public void DepartmentManagerDynamicSeedsAreConsolidatedIntoTheFinalModel()
    {
        Assert.Equal(Rev869ARoleCodes.DepartmentManager, AdvanceSeedData.DepartmentManagerRole.Code);
        var permissions = AdvanceSeedData.RolePagePermissions
            .Where(row => row.RoleId == AdvanceSeedData.DepartmentManagerRole.Id).ToArray();
        Assert.Equal(11, permissions.Length);
        Assert.Equal(8, permissions.Count(row => row.CreatedBy == "migration-rev869a"));
        Assert.Equal(3, permissions.Count(row => row.CreatedBy == "migration-rev869b"));
        Assert.Equal(1086, AdvanceSeedData.RolePagePermissions.Count);
    }

    private static NexaErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=advance_seed_guard;Username=no_connect;Pooling=false")
            .Options;
        return new NexaErpDbContext(options);
    }

    private static IModel CreateModel()
    {
        using var db = CreateContext();
        return db.GetService<IDesignTimeModel>().Model;
    }

    private static IEnumerable<IEntityType> SeededEntities() =>
        Model.GetEntityTypes().Where(entity => entity.GetSeedData().Any());

    private static void AssertUnique(
        IEntityType entity,
        string constraintKind,
        IReadOnlyList<IProperty> properties,
        IReadOnlyList<IDictionary<string, object?>> seeds,
        bool nullsDistinct,
        string? filter)
    {
        IEnumerable<IDictionary<string, object?>> candidates = seeds;
        if (filter is not null)
            candidates = candidates.Where(row => FilterMatches(entity, row, filter));

        var rows = candidates.Select(row => (Row: row, Values: properties.Select(property => Value(row, property.Name)).ToArray()));
        if (nullsDistinct) rows = rows.Where(item => item.Values.All(value => value is not null));
        var duplicates = rows.GroupBy(item => item.Values, SeedValueArrayComparer.Instance)
            .Where(group => group.Count() > 1).ToArray();
        Assert.True(duplicates.Length == 0,
            $"Seeded table {entity.GetSchema()}.{entity.GetTableName()} violates {constraintKind} " +
            $"({string.Join(",", properties.Select(property => property.Name))}): " +
            string.Join("; ", duplicates.Select(group => $"[{string.Join(",", group.Key.Select(Display))}] x{group.Count()}")));
    }

    private static bool FilterMatches(IEntityType entity, IDictionary<string, object?> row, string filter)
    {
        var store = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
        foreach (var clause in filter.Split(" AND ", StringSplitOptions.None))
        {
            var match = Regex.Match(clause,
                "^\\\"(?<column>[^\\\"]+)\\\" (?:(?<notNull>IS NOT NULL)|= (?<boolean>TRUE|FALSE)|= '(?<text>[^']*)')$",
                RegexOptions.CultureInvariant);
            if (!match.Success) throw new InvalidDataException($"Seed guard does not support unique-index filter: {filter}");
            var property = entity.GetProperties().Single(candidate => candidate.GetColumnName(store) == match.Groups["column"].Value);
            var value = Value(row, property.Name);
            if (match.Groups["notNull"].Success && value is null) return false;
            if (match.Groups["boolean"].Success && (value is not bool flag || flag != (match.Groups["boolean"].Value == "TRUE"))) return false;
            if (match.Groups["text"].Success && !string.Equals(Convert.ToString(value, CultureInfo.InvariantCulture), match.Groups["text"].Value, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    private static bool DateOrderIsValid(IDictionary<string, object?> row, string fromName, string toName)
    {
        var to = Value(row, toName);
        return to is null || Comparer<object>.Default.Compare(Value(row, fromName)!, to) <= 0;
    }

    private static decimal DecimalValue(IDictionary<string, object?> row, string name) =>
        Convert.ToDecimal(Value(row, name), CultureInfo.InvariantCulture);

    private static object? Value(IDictionary<string, object?> row, string name) =>
        row.TryGetValue(name, out var value) ? value : throw new InvalidDataException($"Seed property {name} is missing.");

    private static string SeedIdentity(IEntityType entity, IDictionary<string, object?> row) =>
        $"{entity.GetTableName()}[{string.Join(",", entity.FindPrimaryKey()!.Properties.Select(property => Display(Value(row, property.Name))))}]";

    private static string Display(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>";
    private static string KeyText((string Table, string Name) key) => $"{key.Table}|{key.Name}";

    private sealed record CheckValidator(string Sql, Func<IDictionary<string, object?>, bool> Predicate);

    private sealed class SeedValueArrayComparer : IEqualityComparer<object?[]>
    {
        public static SeedValueArrayComparer Instance { get; } = new();
        public bool Equals(object?[]? left, object?[]? right) => left is not null && right is not null && left.SequenceEqual(right);
        public int GetHashCode(object?[] values)
        {
            var hash = new HashCode();
            foreach (var value in values) hash.Add(value);
            return hash.ToHashCode();
        }
    }
}