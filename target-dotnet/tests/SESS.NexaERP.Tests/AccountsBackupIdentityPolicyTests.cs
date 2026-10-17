using SESS.NexaERP.Domain.Identity;
namespace SESS.NexaERP.Tests;
public sealed class AccountsBackupIdentityPolicyTests
{
    [Fact]
    public void StandardKarthickLoginRetainsStoresButNotNewAccountsManagerAuthority()
    {
        Assert.True(AccountsBackupIdentityPolicy.AllowsRole("SESS-41","HUMAN","STORES_MANAGER"));
        Assert.True(AccountsBackupIdentityPolicy.AllowsRole("SESS-41","HUMAN","ACCOUNTS_ASSISTANT"));
        Assert.False(AccountsBackupIdentityPolicy.AllowsRole("SESS-41","HUMAN","ACCOUNTS_MANAGER"));
        Assert.True(AccountsBackupIdentityPolicy.AllowsRole("SESS-14","HUMAN","ACCOUNTS_MANAGER"));
    }
    [Theory]
    [InlineData("STORES_MANAGER")]
    [InlineData("STORES_EXECUTIVE")]
    [InlineData("TECHNICAL_DIRECTOR")]
    [InlineData("MIR_APPROVER")]
    [InlineData("PURCHASE_MANAGER")]
    public void BackupIdentityCannotSelectAnyOtherCapacity(string role)
    {
        Assert.True(AccountsBackupIdentityPolicy.AllowsRole("SESS-41",AccountsBackupIdentityPolicy.MappingType,"ACCOUNTS_MANAGER"));
        Assert.False(AccountsBackupIdentityPolicy.AllowsRole("SESS-41",AccountsBackupIdentityPolicy.MappingType,role));
        Assert.False(AccountsBackupIdentityPolicy.AllowsRole("SESS-35",AccountsBackupIdentityPolicy.MappingType,"ACCOUNTS_MANAGER"));
    }
    [Fact]
    public void OnlyNamedCanonicalEmployeeThroughTrustedApproversIssuerCanUseBackupMapping()
    {
        Assert.True(AccountsBackupIdentityPolicy.IsValidMapping("SESS-41",AccountsBackupIdentityPolicy.MappingType,true));
        Assert.False(AccountsBackupIdentityPolicy.IsValidMapping("SESS-41",AccountsBackupIdentityPolicy.MappingType,false));
        Assert.False(AccountsBackupIdentityPolicy.IsValidMapping("SESS-35",AccountsBackupIdentityPolicy.MappingType,true));
    }
    [Theory]
    [InlineData("masters.items","create")]
    [InlineData("purchase.requisitions","create")]
    [InlineData("purchase.po","issue")]
    [InlineData("stores.material-issues","create")]
    [InlineData("settings.tax-gst","approve")]
    [InlineData("masters.vendors","approve")]
    public void AccountsIdentityCannotUseUniversalCreateOrOtherConflictingOperations(string page,string action) =>
        Assert.False(AccountsBackupIdentityPolicy.AllowsPageAction(page,action));
    [Theory]
    [InlineData("purchase.requisitions","verify")]
    [InlineData("purchase.commercial-comparisons","approve")]
    [InlineData("purchase.po","approve")]
    [InlineData("masters.vendors","verify")]
    [InlineData("settings.tax-gst","create")]
    [InlineData("stores.opening-stock","verify")]
    public void ApprovedAccountsActionsRemainSubjectToExistingGrantAndDocumentGuards(string page,string action) =>
        Assert.True(AccountsBackupIdentityPolicy.AllowsPageAction(page,action));
    [Theory]
    [InlineData("FULL",true)]
    [InlineData("TEMPORARY",true)]
    [InlineData("SUPPORT",false)]
    [InlineData("UNKNOWN",false)]
    public void BackupUsesOnlyEffectiveFullOrTemporaryAccountsAssignment(string assignment,bool allowed) =>
        Assert.Equal(allowed,AccountsBackupIdentityPolicy.AllowsAssignment("SESS-41",AccountsBackupIdentityPolicy.MappingType,"ACCOUNTS_MANAGER",assignment));
}
