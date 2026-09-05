using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Audit;
using SESS.NexaERP.Infrastructure.Masters;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.SecurityMigrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task ControlledTaxWorkflowRunsAgainstDisposablePostgreSqlWithRealEmployeesAndSignedContext()
    {
        var adminOptions = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var model = new NexaErpDbContext(adminOptions);
        var migrator = model.GetService<IMigrator>();
        var latest = model.Database.GetMigrations().Last();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("tax-real-business-up.sql", migrator.GenerateScript("0", latest));
        var trial = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "database", "postgresql", "trial-master-data-apply.sql"));
        server.Execute("tax-real-trial.sql", "\\set expected_database advance_parser\n" + trial);

        Guid accountsId;
        Guid tdId;
        Guid mdId;
        IReadOnlyDictionary<string, EffectiveRoleAssignment> roleAssignments;
        await using (var admin = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(server.ConnectionString).Options))
        {
            accountsId = await admin.Employees.Where(x => x.EmployeeCode == "SESS-14").Select(x => x.Id).SingleAsync();
            tdId = await admin.Employees.Where(x => x.EmployeeCode == "SESS-01").Select(x => x.Id).SingleAsync();
            mdId = await admin.Employees.Where(x => x.EmployeeCode == "SESS-02").Select(x => x.Id).SingleAsync();            roleAssignments = await admin.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                .Where(x => x.CompanyId == Guid.Parse("70000000-0000-0000-0000-000000000001") && x.EffectiveTo == null)
                .ToDictionaryAsync(x => TaxWorkflowUser.AssignmentKey(x.EmployeeId, x.Role!.Code),
                    x => new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType));
            var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
            admin.EmployeeIdentityMappings.AddRange(
                Mapping(companyId, accountsId, "SESS-14"),
                Mapping(companyId, tdId, "SESS-01"),
                Mapping(companyId, mdId, "SESS-02"));
            await admin.SaveChangesAsync();
        }

        server.Execute("tax-real-security-roles.sql", ExternalRolePrerequisites);
        var securityOptions = new DbContextOptionsBuilder<Rev869BSecurityDbContext>()
            .UseNpgsql(server.ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(Rev869BSecurityDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Rev869BSecurity", "advance");
            }).Options;
        using (var security = new Rev869BSecurityDbContext(securityOptions))
        {
            var securityMigrator = security.GetService<IMigrator>();
            var securityMigration = Assert.Single(security.Database.GetMigrations());
            server.Execute("tax-real-security-up.sql", securityMigrator.GenerateScript("0", securityMigration));
        }

        var runtime = new NpgsqlConnectionStringBuilder(server.ConnectionString)
        {
            Username = "nexa_rev869b_app_runtime",
            Pooling = false
        }.ConnectionString;
        var auditConnection = new NpgsqlConnectionStringBuilder(server.ConnectionString)
        {
            Username = "nexa_rev869b_command_audit",
            Pooling = false
        }.ConnectionString;
        using var environment = new TaxWorkflowEnvironment(auditConnection);
        var user = new TaxWorkflowUser(accountsId, "SESS-14", "ACCOUNTS_MANAGER", roleAssignments);
        var runtimeOptions = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(runtime).Options;

        Guid approvedId;
        await using (var db = new NexaErpDbContext(runtimeOptions))
        {
            var service = new EfTaxGstWorkflowService(db, user, new EfAuditWriter(db, user));
            var created = await service.CreateAsync(Request("9025"), "tax-create-9025", default);
            approvedId = created.Id;
            Assert.Equal(MasterApprovalStatuses.PendingApproval, created.ApprovalStatus);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.ApproveAsync(created.Id, new(0, "creator cannot approve", "tax-self-denial"), default));

            user.Set(tdId, "SESS-01", "TECHNICAL_DIRECTOR");
            var approved = await service.ApproveAsync(created.Id, new(0, "GST portal manually cross-checked", "tax-approve-9025"), default);
            Assert.Equal(MasterApprovalStatuses.Approved, approved.ApprovalStatus);
            Assert.Equal((uint)1, approved.Version);
            Assert.Equal(tdId, approved.DecisionEmployeeId);
            await Assert.ThrowsAnyAsync<Exception>(() =>
                service.ApproveAsync(created.Id, new(0, "replay", "tax-approve-9025"), default));
        }

        await using (var db = new NexaErpDbContext(runtimeOptions))
        {
            user.Set(accountsId, "SESS-14", "ACCOUNTS_MANAGER");
            var service = new EfTaxGstWorkflowService(db, user, new EfAuditWriter(db, user));
            var rejected = await service.CreateAsync(Request("9026"), "tax-create-9026", default);
            user.Set(mdId, "SESS-02", "MANAGING_DIRECTOR");
            var decision = await service.RejectAsync(rejected.Id, new(0, "Incorrect HSN applicability", "tax-reject-9026"), default);
            Assert.Equal(MasterApprovalStatuses.Rejected, decision.ApprovalStatus);
        }

        Guid successorId;
        await using (var db = new NexaErpDbContext(runtimeOptions))
        {
            user.Set(accountsId, "SESS-14", "ACCOUNTS_MANAGER");
            var service = new EfTaxGstWorkflowService(db, user, new EfAuditWriter(db, user));
            var successor = await service.CreateAsync(
                Request("9025") with
                {
                    GstRate = 12, CgstRate = 6, SgstRate = 6,
                    EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
                    SupersedesTaxGstSettingId = approvedId
                },
                "tax-supersede-9025", default);
            successorId = successor.Id;
            user.Set(mdId, "SESS-02", "MANAGING_DIRECTOR");
            await service.ApproveAsync(successor.Id, new(0, "Changed rate manually cross-checked", "tax-approve-successor"), default);
        }

        await using (var verifier = new NexaErpDbContext(runtimeOptions))
        {
            var predecessor = await verifier.TaxGstSettings.AsNoTracking().SingleAsync(x => x.Id == approvedId);
            Assert.Equal(MasterApprovalStatuses.Approved, predecessor.ApprovalStatus);
            Assert.True(predecessor.IsActive);
            Assert.Equal((uint)1, predecessor.Version);
            Assert.Equal(2, await verifier.ControlledConfigurationHistories.CountAsync(x =>
                x.EntityType == nameof(TaxGstSetting) && x.EntityId == approvedId));
            Assert.Equal(2, await verifier.AuditLogs.CountAsync(x =>
                x.EntityName == nameof(TaxGstSetting) && x.EntityId == approvedId.ToString()));
            var resolved = await new EfTaxGstResolver(verifier).ResolveAsync(
                new TaxResolutionRequest("SESS_PVT_LTD", TaxJurisdictions.IndiaGst, "9025", "33", "33",
                    VendorRegistrationType.REGULAR.ToCanonicalValue(), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), 100), default);
            Assert.Equal(successorId, resolved.Id);
            Assert.Equal(12, resolved.GstRate);
        }
    }

    private static EmployeeIdentityMapping Mapping(Guid companyId, Guid employeeId, string subject) => new()
    {
        CompanyId = companyId,
        OrganizationId = "SESS_PVT_LTD",
        Issuer = "https://issuer.purchase-flow.test",
        Subject = subject,
        EmployeeId = employeeId,
        IdentityType = IdentityTypes.Human,
        EffectiveFrom = new DateOnly(2026, 1, 1),
        IsActive = true,
        CreatedBy = "PURCHASE_FLOW_TEST"
    };

    private static CreateTaxGstSettingRequest Request(string hsn) => new(
        "SESS_PVT_LTD", TaxJurisdictions.IndiaGst, hsn, "33", "33",
        VendorRegistrationType.REGULAR.ToCanonicalValue(), 18, 9, 9, 0, 0,
        false, false, "INR", 2, DateOnly.FromDateTime(DateTime.UtcNow), null, "Manual GST portal cross-check required");

    private sealed class TaxWorkflowUser : ICurrentUser
    {
        private readonly IReadOnlyDictionary<string, EffectiveRoleAssignment> knownAssignments;
        private IReadOnlyList<EffectiveRoleAssignment> assignments = [];
        private ResolvedRoleAuthority? authority;
        private string selectedRole = "none";
        public TaxWorkflowUser(Guid employeeId, string login, string role,
            IReadOnlyDictionary<string, EffectiveRoleAssignment>? assignmentsByEmployeeAndRole = null)
        {
            knownAssignments = assignmentsByEmployeeAndRole ?? new Dictionary<string, EffectiveRoleAssignment>();
            Set(employeeId, login, role);
        }
        public static string AssignmentKey(Guid employeeId, string roleCode) => $"{employeeId:N}|{roleCode.Trim().ToUpperInvariant()}";
        public Guid CurrentEmployeeId { get; private set; }
        public string LoginId { get; private set; } = string.Empty;
        public string RoleCode => authority?.RoleCode ?? selectedRole;
        public IReadOnlyList<string> RoleCodes => assignments.Select(x => x.RoleCode).ToArray();
        public IReadOnlyList<string> FullAuthorityRoleCodes => assignments.Where(x => x.AssignmentType != "SUPPORT").Select(x => x.RoleCode).ToArray();
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments => assignments;
        public Guid? ResolvedRoleAssignmentId => authority?.AssignmentId;
        public string? ResolvedRoleAssignmentType => authority?.AssignmentType;
        public string? OrganizationId => "SESS_PVT_LTD";
        public bool IsAuthenticated => true;
        public string? IdentityIssuer => "https://issuer.purchase-flow.test";
        public string? IdentitySubject => LoginId;
        public Guid? EmployeeId => CurrentEmployeeId;
        public void SetResolvedRoleAuthority(ResolvedRoleAuthority value) => authority = value;
        public void Set(Guid id, string subject, string roleCode, params string[] effectiveRoles)
        {
            CurrentEmployeeId = id;
            LoginId = subject;
            selectedRole = roleCode;
            authority = null;
            var roles = effectiveRoles.Length == 0 ? [roleCode] : effectiveRoles;
            assignments = roles.Distinct(StringComparer.Ordinal).Select(code =>
                knownAssignments.TryGetValue(AssignmentKey(id, code), out var assignment)
                    ? assignment
                    : new EffectiveRoleAssignment(Guid.Empty, code, "FULL")).ToArray();
        }
    }
    private sealed class TaxWorkflowEnvironment : IDisposable
    {
        private readonly Dictionary<string, string?> prior = new(StringComparer.Ordinal);
        public TaxWorkflowEnvironment(string auditConnection)
        {
            Set("REV869B_COMMAND_AUDIT_CONNECTION", auditConnection);
            Set("REV869B_EXECUTION_INSTANCE_ID", "95000000-0000-0000-0000-000000000001");
            Set("REV869B_SERVICE_INSTANCE_FINGERPRINT", new string('a', 64));
            Set("REV869B_OWNERSHIP_LEASE_FINGERPRINT", new string('b', 64));
        }
        private void Set(string name, string value)
        {
            prior[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }
        public void Dispose()
        {
            foreach (var pair in prior) Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        }
    }
}
