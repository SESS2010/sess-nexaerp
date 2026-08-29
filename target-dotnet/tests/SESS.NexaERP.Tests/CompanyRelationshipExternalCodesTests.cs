using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class CompanyRelationshipExternalCodesTests
{
    [Fact]
    public void Relationship_models_expose_nullable_eighty_character_search_codes()
    {
        using var db = CreateContext();
        AssertCode(db.Model.FindEntityType(typeof(CustomerCompanyRelationship))!,
            nameof(CustomerCompanyRelationship.CustomerAssignedSupplierCode));
        AssertCode(db.Model.FindEntityType(typeof(VendorCompanyRelationship))!,
            nameof(VendorCompanyRelationship.VendorAssignedCustomerCode));
    }

    [Fact]
    public void Company_relationship_api_contracts_expose_both_nullable_codes()
    {
        AssertNullableString<CustomerCompanyRelationshipDetail>("CustomerAssignedSupplierCode");
        AssertNullableString<UpsertCustomerCompanyRelationshipRequest>("CustomerAssignedSupplierCode");
        AssertNullableString<VendorCompanyRelationshipDetail>("VendorAssignedCustomerCode");
        AssertNullableString<UpsertVendorCompanyRelationshipRequest>("VendorAssignedCustomerCode");
    }

    [Fact]
    public void Migration_adds_only_nullable_columns_and_filtered_indexes_and_guards_both_directions()
    {
        var source = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations",
            "20260829045502_CompanyRelationshipExternalCodes.cs");
        Assert.Equal(2, Count(source, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Equal(2, Count(source, "migrationBuilder.AddColumn<string>("));
        Assert.Equal(2, Count(source, "nullable: true"));
        Assert.Equal(2, Count(source, "migrationBuilder.CreateIndex("));
        Assert.Equal(2, Count(source, "IS NOT NULL"));
        Assert.DoesNotContain("InsertData", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateData", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteData", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Up")]
    [InlineData("Down")]
    public void Migration_guard_rejects_non_postgresql_provider_in_both_directions(string methodName)
    {
        var migration = new SESS.NexaERP.Infrastructure.Persistence.Migrations.CompanyRelationshipExternalCodes();
        var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var error = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(migration, [new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite")]));
        Assert.IsType<NotSupportedException>(error.InnerException);
    }

    [Fact]
    public void Contract_document_defines_relationship_code_meanings()
    {
        var contract = Read("outputs", "sess_api_contract.md");
        Assert.Contains("CustomerAssignedSupplierCode", contract, StringComparison.Ordinal);
        Assert.Contains("VendorAssignedCustomerCode", contract, StringComparison.Ordinal);
        Assert.Contains("neither belongs on the shared customer or vendor master", contract, StringComparison.Ordinal);
    }

    private static void AssertCode(IEntityType entity, string propertyName)
    {
        var property = entity.FindProperty(propertyName)!;
        Assert.True(property.IsNullable);
        Assert.Equal(80, property.GetMaxLength());
        var index = Assert.Single(entity.GetIndexes(), x => x.Properties.Count == 1 && x.Properties[0] == property);
        Assert.False(index.IsUnique);
        Assert.Equal(string.Concat('"', propertyName, '"', " IS NOT NULL"), index.GetFilter());
    }

    private static void AssertNullableString<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName)!;
        Assert.Equal(typeof(string), property.PropertyType);
        Assert.Equal(NullabilityState.Nullable, new NullabilityInfoContext().Create(property).ReadState);
    }

    private static NexaErpDbContext CreateContext() => new(new DbContextOptionsBuilder<NexaErpDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);

    private static int Count(string text, string value) =>
        (text.Length - text.Replace(value, "", StringComparison.Ordinal).Length) / value.Length;

    private static string Read(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine([root, .. parts]));
    }
}
