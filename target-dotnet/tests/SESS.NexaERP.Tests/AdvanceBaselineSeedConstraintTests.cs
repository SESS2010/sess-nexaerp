using System.Globalization;
using System.Text.Json;
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
        var migration = migrations.CreateMigration(migrations.Migrations.First().Value, db.Database.ProviderName!);
        var migrationModel = migration.TargetModel;
        var rowsByEntity = migrationModel.GetEntityTypes().ToDictionary(entity => entity, _ => new List<IDictionary<string, object?>>());
        var entitiesByTable = migrationModel.GetEntityTypes().Where(entity => entity.GetTableName() is not null)
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

        foreach (var entity in migrationModel.GetEntityTypes())
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
            [("authentication_bootstrap_state", "CK_authentication_bootstrap_completion")] = new(
                "(\"Status\"='PENDING' AND \"EmployeeId\" IS NULL AND \"CompanyId\" IS NULL AND \"OrganizationId\" IS NULL AND \"IssuerSha256\" IS NULL AND \"SubjectSha256\" IS NULL AND \"CompanyCount\" IS NULL AND \"CompanySetSha256\" IS NULL AND \"CompletedAt\" IS NULL AND \"CompletedBy\" IS NULL) OR (\"Status\"='COMPLETED' AND \"EmployeeId\" IS NOT NULL AND \"CompanyId\" IS NOT NULL AND length(trim(\"OrganizationId\"))>0 AND octet_length(\"IssuerSha256\")=32 AND octet_length(\"SubjectSha256\")=32 AND \"CompanyCount\">0 AND octet_length(\"CompanySetSha256\")=32 AND \"CompletedAt\" IS NOT NULL AND length(trim(\"CompletedBy\"))>0)",
                row => StringValue(row, "Status") == "PENDING"
                    ? new[] { "EmployeeId", "CompanyId", "OrganizationId", "IssuerSha256", "SubjectSha256", "CompanyCount", "CompanySetSha256", "CompletedAt", "CompletedBy" }.All(x => Value(row, x) is null)
                    : StringValue(row, "Status") == "COMPLETED"
                      && Value(row, "EmployeeId") is not null && Value(row, "CompanyId") is not null
                      && !string.IsNullOrWhiteSpace(StringValue(row, "OrganizationId"))
                      && Value(row, "IssuerSha256") is byte[] issuer && issuer.Length == 32
                      && Value(row, "SubjectSha256") is byte[] subject && subject.Length == 32
                      && Value(row, "CompanyCount") is int companyCount && companyCount > 0
                      && Value(row, "CompanySetSha256") is byte[] companySet && companySet.Length == 32
                      && Value(row, "CompletedAt") is not null && !string.IsNullOrWhiteSpace(StringValue(row, "CompletedBy"))),
            [("authentication_bootstrap_state", "CK_authentication_bootstrap_singleton")] = new(
                "\"Id\" = '81000000-0000-0000-0000-000000000001'::uuid",
                row => Value(row, "Id") is Guid id && id == Guid.Parse("81000000-0000-0000-0000-000000000001")),
            [("authentication_bootstrap_state", "CK_authentication_bootstrap_status")] = new(
                "\"Status\" IN ('PENDING','COMPLETED')",
                row => StringValue(row, "Status") is "PENDING" or "COMPLETED"),
            [("audit_logs", "CK_audit_logs_scope")] = new(
                @"(""Scope"" = 'GLOBAL' AND ""CompanyId"" IS NULL) OR (""Scope"" = 'COMPANY' AND ""CompanyId"" IS NOT NULL)",
                row => StringValue(row, "Scope") == "GLOBAL" ? Value(row, "CompanyId") is null :
                    StringValue(row, "Scope") == "COMPANY" && Value(row, "CompanyId") is not null),
            [("companies", "CK_companies_entity_type")] = new(
                @"""EntityType"" IN ('PROPRIETORSHIP','PRIVATE_LIMITED')",
                row => StringValue(row, "EntityType") is "PROPRIETORSHIP" or "PRIVATE_LIMITED"),
            [("companies", "CK_companies_status")] = new(
                @"""Status"" IN ('ACTIVE','INACTIVE')",
                row => StringValue(row, "Status") is "ACTIVE" or "INACTIVE"),
            [("company_gst_registrations", "CK_company_gst_registration_dates")] = new(
                @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" >= ""EffectiveFrom""",
                row => DateOrderIsValid(row, "EffectiveFrom", "EffectiveTo")),
            [("company_gst_registrations", "CK_company_gst_registrations_gstin")] = new(
                @"char_length(""Gstin"") = 15",
                row => StringValue(row, "Gstin").Length == 15),
            [("currencies", "CK_currencies_code")] = new(
                @"char_length(""Code"") = 3",
                row => StringValue(row, "Code").Length == 3),
            [("currencies", "CK_currencies_minor_units")] = new(
                @"""MinorUnitDigits"" BETWEEN 0 AND 6",
                row => Convert.ToInt32(Value(row, "MinorUnitDigits"), CultureInfo.InvariantCulture) is >= 0 and <= 6),
            [("employee_company_assignments", "CK_employee_company_assignment_dates")] = new(
                @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" >= ""EffectiveFrom""",
                row => DateOrderIsValid(row, "EffectiveFrom", "EffectiveTo")),
            [("employee_company_assignments", "CK_employee_company_assignment_type")] = new(
                @"""AssignmentType"" IN ('PAYROLL','WORK')",
                row => StringValue(row, "AssignmentType") is "PAYROLL" or "WORK"),
            [("employee_department_assignments", "CK_employee_department_assignment_dates")] = new(
                @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" >= ""EffectiveFrom""",
                row => DateOrderIsValid(row, "EffectiveFrom", "EffectiveTo")),
            [("employee_department_assignments", "CK_employee_department_assignment_primary")] = new(
                @"(""AssignmentType"" = 'PRIMARY') = ""IsPrimary""",
                row => (StringValue(row, "AssignmentType") == "PRIMARY") == Convert.ToBoolean(Value(row, "IsPrimary"), CultureInfo.InvariantCulture)),
            [("employee_department_assignments", "CK_employee_department_assignment_type")] = new(
                @"""AssignmentType"" IN ('PRIMARY','SECONDARY')",
                row => StringValue(row, "AssignmentType") is "PRIMARY" or "SECONDARY"),
            [("organization_policies", "CK_organization_policy_dates")] = new(
                "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"",
                row => DateOrderIsValid(row, "EffectiveFrom", "EffectiveTo")),
            [("purchase_transaction_approval_policies", "CK_purchase_transaction_policy_amounts")] = new(
                "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" >= \"MinimumAmount\")",
                row => DecimalValue(row, "MinimumAmount") >= 0m &&
                    (Value(row, "MaximumAmount") is null || DecimalValue(row, "MaximumAmount") >= DecimalValue(row, "MinimumAmount"))),
            [("purchase_transaction_approval_policies", "CK_purchase_transaction_policy_dates")] = new(
                "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"",
                row => DateOrderIsValid(row, "EffectiveFrom", "EffectiveTo")),
            [("business_rule_configuration_versions", "CK_business_rule_configuration_first_version")] = new(
                "(\"VersionNumber\" = 1 AND \"PreviousVersionId\" IS NULL AND \"OldValueJson\" IS NULL) OR (\"VersionNumber\" > 1 AND \"PreviousVersionId\" IS NOT NULL AND \"OldValueJson\" IS NOT NULL)",
                row => Convert.ToInt32(Value(row, "VersionNumber"), CultureInfo.InvariantCulture) == 1
                    ? Value(row, "PreviousVersionId") is null && Value(row, "OldValueJson") is null
                    : Value(row, "PreviousVersionId") is not null && Value(row, "OldValueJson") is not null),
            [("business_rule_configuration_versions", "CK_business_rule_configuration_json")] = new(
                "jsonb_typeof(\"NewValueJson\") IN ('number','boolean','string') AND (\"OldValueJson\" IS NULL OR jsonb_typeof(\"OldValueJson\") IN ('number','boolean','string'))",
                row => IsScalarJson(StringValue(row, "NewValueJson"))
                    && (Value(row, "OldValueJson") is null || IsScalarJson(StringValue(row, "OldValueJson")))),
            [("business_rule_configuration_versions", "CK_business_rule_configuration_role")] = new(
                "\"ChangedByRoleCode\" IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR','IT_MANAGER')",
                row => StringValue(row, "ChangedByRoleCode") is "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR" or "IT_MANAGER"),
            [("business_rule_configuration_versions", "CK_business_rule_configuration_value_type")] = new(
                "\"ValueType\" IN ('INTEGER','DECIMAL','BOOLEAN','TEXT')",
                row => StringValue(row, "ValueType") is "INTEGER" or "DECIMAL" or "BOOLEAN" or "TEXT"),
            [("business_rule_configuration_versions", "CK_business_rule_configuration_version_number")] = new(
                "\"VersionNumber\" > 0",
                row => Convert.ToInt32(Value(row, "VersionNumber"), CultureInfo.InvariantCulture) > 0),
            [("roles", "CK_roles_code_canonical")] = new(
                "\"Code\" = upper(btrim(\"Code\"))",
                row => StringValue(row, "Code") == StringValue(row, "Code").Trim().ToUpperInvariant())
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
        Assert.Equal(1090, AdvanceSeedData.RolePagePermissions.Count);
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
            var bareBoolean = Regex.Match(clause, "^\"(?<column>[^\"]+)\"$", RegexOptions.CultureInvariant);
            if (bareBoolean.Success)
            {
                var booleanProperty = entity.GetProperties().Single(candidate =>
                    candidate.GetColumnName(store) == bareBoolean.Groups["column"].Value);
                if (Value(row, booleanProperty.Name) is not true) return false;
                continue;
            }
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

    private static bool IsScalarJson(string value)
    {
        using var document = JsonDocument.Parse(value);
        return document.RootElement.ValueKind is JsonValueKind.Number or JsonValueKind.True
            or JsonValueKind.False or JsonValueKind.String;
    }

    private static string StringValue(IDictionary<string, object?> row, string name) =>
        Convert.ToString(Value(row, name), CultureInfo.InvariantCulture) ?? string.Empty;

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
