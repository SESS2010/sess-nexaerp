using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Audit;
using SESS.NexaERP.Infrastructure.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

/// <summary>
/// R10 (26 Sep): each company's legal identity is stored in the database, read by any signed-in
/// employee, changed only by the Technical Director with a version, a reason and an audit row.
/// </summary>
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static SaveCompanyProfileRequest Profile(long version = 0) => new(
        "SRI EASWARI SCIENTIFIC SOLUTION PVT LTD", "SESS", "33abacs5491h1za", "ABACS5491H", "33", "Tamil Nadu",
        "No. 1, Example Street", null, "Chennai", "600001", null, null, version, "Legal details from the GST certificate");

    [Fact]
    public void A_valid_company_profile_is_accepted_and_normalised()
    {
        var clean = EfCompanyProfileService.Validate(Profile());
        Assert.Equal("33ABACS5491H1ZA", clean.Gstin);
    }

    [Theory]
    [InlineData("Gstin", "33ABACS5491H1Z", "ABACS5491H", "33", "600001")]
    [InlineData("Pan", "33ABACS5491H1ZA", "ABACS549", "33", "600001")]
    [InlineData("StateCode", "33ABACS5491H1ZA", "ABACS5491H", "29", "600001")]
    [InlineData("Pan", "33ABACS5491H1ZA", "ZZZZZ9999Z", "33", "600001")]
    [InlineData("PinCode", "33ABACS5491H1ZA", "ABACS5491H", "33", "060001")]
    public void An_inconsistent_company_profile_names_the_field(string field, string gstin, string pan, string state, string pin)
    {
        var failure = Assert.Throws<StoresValidationException>(() =>
            EfCompanyProfileService.Validate(Profile() with { Gstin = gstin, Pan = pan, StateCode = state, PinCode = pin }));
        Assert.Contains(field, failure.Errors!.Keys);
    }

    [Fact]
    public async Task The_company_profile_is_saved_by_the_TD_with_version_reason_and_audit()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("company-profile.sql", model.GetService<IMigrator>().GenerateScript("0", model.Database.GetMigrations().Last()));
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options);

        var td = new ProfileUser("td-login", "TECHNICAL_DIRECTOR");
        var service = new EfCompanyProfileService(db, td, new EfAuditWriter(db, td));
        Assert.False((await service.GetAsync(CancellationToken.None)).IsComplete);

        var saved = await service.SaveAsync(Profile(), CancellationToken.None);
        Assert.True(saved.IsComplete);
        Assert.Equal(("33ABACS5491H1ZA", "ABACS5491H", "33", 0L), (saved.Gstin, saved.Pan, saved.StateCode, saved.Version));
        var updated = await service.SaveAsync(Profile(0) with { Phone = "044 0000 0000" }, CancellationToken.None);
        Assert.Equal(1L, updated.Version);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.SaveAsync(Profile(0), CancellationToken.None));

        var stores = new ProfileUser("stores-login", "STORES_MANAGER");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new EfCompanyProfileService(db, stores, new EfAuditWriter(db, stores)).SaveAsync(Profile(1), CancellationToken.None));
        Assert.Equal("044 0000 0000", (await new EfCompanyProfileService(db, stores, new EfAuditWriter(db, stores)).GetAsync(CancellationToken.None)).Phone);
        Assert.Equal(2, await db.AuditLogs.CountAsync(x => x.Action == "CompanyProfileSaved"));

        // The database refuses a GSTIN that disagrees with its PAN, whatever the caller.
        server.AssertRejected("company-profile-mismatch.sql",
            """UPDATE advance.company_profiles SET "Pan"='ZZZZZ9999Z';""", "CK_company_profiles_gstin_pan");

        // Rolling the schema back must not silently delete the company's legal details.
        server.AssertRejected("company-profile-down-refused.sql",
            model.GetService<IMigrator>().GenerateScript(model.Database.GetMigrations().Last(), "20260926100000_VendorBillSeparateDecider"),
            "Company profile rollback refuses retained company legal details");
    }

    private sealed class ProfileUser(string login, string role) : ICurrentUser
    {
        private readonly EffectiveRoleAssignment[] assignments = [new(Guid.NewGuid(), role, "FULL")];
        public string LoginId => login;
        public string RoleCode => role;
        public string? OrganizationId => "SESS_PVT_LTD";
        public bool IsAuthenticated => true;
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments => assignments;
        public void SetResolvedRoleAuthority(ResolvedRoleAuthority authority) { }
    }
}
