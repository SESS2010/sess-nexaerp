using SESS.NexaERP.Application.Rev869A;

namespace SESS.NexaERP.Tests;

public sealed class ConfigurationReadReachabilityTests
{
    [Fact]
    public void Every_previously_write_only_configuration_page_has_a_paged_view_endpoint()
    {
        var map = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869AConfigurationEndpoints.cs");
        foreach (var endpoint in new[] { "employee-identities", "operational-scopes", "uom-conversions", "tax-gst", "vendor-qualifications" })
        {
            Assert.Contains($"MapGet(\"/{endpoint}\"", map, StringComparison.Ordinal);
            Assert.Contains($"MapPost(\"/{endpoint}\"", map, StringComparison.Ordinal);
        }
        var reads = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869AConfigurationReadEndpoints.cs");
        Assert.Equal(5, Count(reads, "new PagedResponse<"));
        Assert.Equal(4, Count(reads, "CurrentCompanyId(db"));
        Assert.Contains("db.UomConversions.AsNoTracking().Where(x=>x.OrganizationId==organization)", reads, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_contracts_return_identifiers_filters_effective_ranges_remarks_and_versions()
    {
        AssertFields<EmployeeIdentityMappingSummary>("Id","OrganizationId","Issuer","SubjectSha256","EmployeeId","EmployeeCode","IdentityType","EffectiveFrom","EffectiveTo","IsActive","Remarks","Version");
        AssertFields<OperationalScopeSummary>("Id","OrganizationId","EmployeeId","EmployeeCode","DepartmentId","DepartmentCode","WarehouseId","WarehouseCode","RackBinId","BinCode","OwnRecordsOnly","AllowsPrivilegedCrossScope","EffectiveFrom","EffectiveTo","IsActive","Remarks","Version");
        AssertFields<UomConversionSummary>("Id","OrganizationId","FromUomId","FromUomCode","ToUomId","ToUomCode","MeasurementDimension","ConversionFactor","EffectiveFrom","EffectiveTo","ApprovalStatus","IsActive","Remarks","Version");
        AssertFields<TaxGstSettingSummary>("Id","OrganizationId","JurisdictionCode","HsnSacCode","SupplierStateCode","PlaceOfSupplyStateCode","VendorRegistrationType","GstRate","CgstRate","SgstRate","IgstRate","CessRate","IsExempt","IsReverseCharge","CurrencyCode","RoundingScale","EffectiveFrom","EffectiveTo","ApprovalStatus","SupersedesTaxGstSettingId","IsActive","Remarks","Version");
        AssertFields<VendorQualificationSummary>("Id","OrganizationId","VendorId","VendorCode","ItemCategoryId","ItemCategoryCode","QualificationCode","EffectiveFrom","EffectiveTo","VerificationStatus","ApprovalStatus","IsActive","Remarks","Version");
        Assert.Null(typeof(EmployeeIdentityMappingSummary).GetProperty("Subject"));
    }

    private static void AssertFields<T>(params string[] fields)
    {
        var properties=typeof(T).GetProperties().Select(x=>x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.All(fields, field=>Assert.Contains(field,properties));
    }
    private static int Count(string source,string value)=>source.Split(value,StringSplitOptions.None).Length-1;
    private static string Read(params string[] parts)=>File.ReadAllText(Path.Combine([Root,..parts]));
    private static readonly string Root=FindRoot();
    private static string FindRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"SESS.NexaERP.slnx")))d=d.Parent;return d?.FullName??throw new DirectoryNotFoundException("Repository root not found.");}
}