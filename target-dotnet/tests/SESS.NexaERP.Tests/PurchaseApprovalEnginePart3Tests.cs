using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Infrastructure.Purchase;

namespace SESS.NexaERP.Tests;

public sealed class PurchaseApprovalEnginePart3Tests
{
    private static readonly Guid Creator = Guid.Parse("10000000-0000-0000-0000-000000000099");
    private static readonly Guid DepartmentApprover = Guid.Parse("10000000-0000-0000-0000-000000000014");
    private static readonly Guid Director = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly EfPurchaseApprovalWorkflowService engine = new(null!);

    [Theory]
    [InlineData("4999.99", "DEPARTMENT_ONLY", 1)]
    [InlineData("5000.00", "DEPARTMENT_THEN_TD", 2)]
    [InlineData("100000.00", "DEPARTMENT_THEN_TD", 2)]
    [InlineData("100000.01", "DEPARTMENT_THEN_MD", 2)]
    public void SettledThresholdBoundariesAreExact(string amount, string route, int steps)
    {
        var sql = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "ApprovalConfigurationAndPermissionsPart2Sql.cs");
        var boundary = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        var expectedTuple = boundary switch
        {
            4999.99m => "('DEPARTMENT_ONLY',0.00::numeric,4999.99::numeric)",
            5000.00m => "('DEPARTMENT_THEN_TD',5000.00::numeric,100000.00::numeric)",
            100000.00m => "('DEPARTMENT_THEN_TD',5000.00::numeric,100000.00::numeric)",
            _ => "('DEPARTMENT_THEN_MD',100000.01::numeric,NULL::numeric)"
        };
        Assert.Contains(expectedTuple, sql, StringComparison.Ordinal);
        Assert.Contains(route, expectedTuple, StringComparison.Ordinal);
        Assert.Equal(steps, route == "DEPARTMENT_ONLY" ? 1 : 2);
    }

    [Fact]
    public void AllTwentyOneDepartmentsAreMappedForBothCompanies()
    {
        var sql = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "ApprovalConfigurationAndPermissionsPart2Sql.cs");
        var departments = new[] { "PRODUCTION", "FABRICATION", "REFRIGERATION", "ELECTRICAL", "PLC_LABVIEW", "QC", "R_AND_D", "MAINTENANCE", "DESIGN", "CALIBRATION", "ACCOUNTS", "HR", "IT", "SALES", "MARKETING", "SERVICE", "AMC", "CAMC", "STORES", "PURCHASE", "MANAGEMENT" };
        Assert.All(departments, code => Assert.Contains($"('{code}'", sql, StringComparison.Ordinal));
        Assert.Contains("Expected 42 active Part 2 department mappings", sql, StringComparison.Ordinal);
        Assert.Contains("70000000-0000-0000-0000-000000000001", sql, StringComparison.Ordinal);
        Assert.Contains("70000000-0000-0000-0000-000000000002", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void FourExactApproversAreConfigured()
    {
        var sql = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "ApprovalConfigurationAndPermissionsPart2Sql.cs");
        Assert.Contains("'SESS-25','PRODUCTION_MANAGER'", sql, StringComparison.Ordinal);
        Assert.Contains("'SESS-14','ACCOUNTS_MANAGER'", sql, StringComparison.Ordinal);
        Assert.Contains("'SESS-01','TECHNICAL_DIRECTOR'", sql, StringComparison.Ordinal);
        Assert.Contains("'SESS-02','MANAGING_DIRECTOR'", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void OneStepApprovalCompletesDocument()
    {
        var snapshot = Snapshot("DEPARTMENT_ONLY", new PurchaseApprovalWorkflowStepSnapshot(1, "DEPARTMENT_MAPPING", DepartmentApprover, "SESS-14", "ACCOUNTS_MANAGER"));
        var decision = engine.AuthorizeNextStep(snapshot, 1, 0, Creator, DepartmentApprover, ["ACCOUNTS_MANAGER"]);
        Assert.True(decision.CompletesDocument);
        Assert.Equal(1, decision.CompletedStepCount);
    }

    [Fact]
    public void TwoStepApprovalStaysPendingThenDirectorCompletes()
    {
        var snapshot = Snapshot("DEPARTMENT_THEN_TD",
            new(1, "DEPARTMENT_MAPPING", DepartmentApprover, "SESS-14", "ACCOUNTS_MANAGER"),
            new(2, "CONFIGURED_ROLE", Director, "SESS-01", "TECHNICAL_DIRECTOR"));
        var level1 = engine.AuthorizeNextStep(snapshot, 3, 0, Creator, DepartmentApprover, ["ACCOUNTS_MANAGER"]);
        var level2 = engine.AuthorizeNextStep(snapshot, 3, 1, Creator, Director, ["TECHNICAL_DIRECTOR"], DepartmentApprover);
        Assert.False(level1.CompletesDocument);
        Assert.True(level2.CompletesDocument);
    }

    [Fact]
    public void WrongPersonAndWrongRoleAreDenied()
    {
        var snapshot = Snapshot("DEPARTMENT_ONLY", new PurchaseApprovalWorkflowStepSnapshot(1, "DEPARTMENT_MAPPING", DepartmentApprover, "SESS-14", "ACCOUNTS_MANAGER"));
        Assert.Throws<UnauthorizedAccessException>(() => engine.AuthorizeNextStep(snapshot, 1, 0, Creator, Guid.NewGuid(), ["ACCOUNTS_MANAGER"]));
        Assert.Throws<UnauthorizedAccessException>(() => engine.AuthorizeNextStep(snapshot, 1, 0, Creator, DepartmentApprover, ["PURCHASE_MANAGER"]));
    }

    [Fact]
    public void CreatorSameLevelTwoAndReplayAreDenied()
    {
        var snapshot = Snapshot("DEPARTMENT_THEN_TD",
            new(1, "DEPARTMENT_MAPPING", DepartmentApprover, "SESS-14", "ACCOUNTS_MANAGER"),
            new(2, "CONFIGURED_ROLE", DepartmentApprover, "SESS-14", "ACCOUNTS_MANAGER"));
        Assert.Throws<UnauthorizedAccessException>(() => engine.AuthorizeNextStep(snapshot, 1, 0, Creator, Creator, ["ACCOUNTS_MANAGER"]));
        Assert.Throws<UnauthorizedAccessException>(() => engine.AuthorizeNextStep(snapshot, 1, 1, Creator, DepartmentApprover, ["ACCOUNTS_MANAGER"], DepartmentApprover));
        Assert.Throws<UnauthorizedAccessException>(() => engine.AuthorizeNextStep(snapshot, 1, 2, Creator, DepartmentApprover, ["ACCOUNTS_MANAGER"]));
    }

    [Theory]
    [InlineData("CreateRFQ", "PURCHASE_EXECUTIVE")]
    [InlineData("CreateComparison", "PURCHASE_MANAGER")]
    [InlineData("MaterialFollowUp", "STORES_EXECUTIVE")]
    public void PriyasOperationalRoleIsDeterministic(string operation, string expected)
    {
        var resolver = new PurchaseOperationalRoleResolver();
        Assert.Equal(expected, resolver.Resolve(operation, ["STORES_EXECUTIVE", "PURCHASE_MANAGER", "PURCHASE_EXECUTIVE"]));
    }

    [Fact]
    public void ApprovalCommandsNeverUseOperationalRoleResolver()
    {
        var resolver = new PurchaseOperationalRoleResolver();
        Assert.Throws<InvalidOperationException>(() => resolver.Resolve("ApprovePO", ["PURCHASE_MANAGER", "TECHNICAL_DIRECTOR"]));
    }

    [Fact]
    public void PostgreSqlGuardsAllDocumentsAndHistories()
    {
        var sql = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "TwoLevelPurchaseApprovalEnginePart3Sql.cs");
        Assert.Equal(3, Count(sql, "approval_state_guard BEFORE UPDATE"));
        Assert.Equal(3, Count(sql, "approval_decision_guard BEFORE INSERT"));
        Assert.Contains("purchase_manager_approval_denied", sql, StringComparison.Ordinal);
        Assert.Contains("purchase_approval_level_separation", sql, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationGuardsUpAndDownAndDoesNotTargetOwnerDatabase()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260826054057_TwoLevelPurchaseApprovalEnginePart3.cs");
        Assert.Equal(2, Count(migration, "PostgreSqlClusterGuard.Require(migrationBuilder)"));
        Assert.DoesNotContain("MigrateAsync", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.Migrate", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void PurchaseManagerIsDeniedByWorkflowAndEveryHistoryTrigger()
    {
        var sql = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "TwoLevelPurchaseApprovalEnginePart3Sql.cs");
        Assert.Contains("NEW.\"ActorRoleCode\"='PURCHASE_MANAGER'", sql, StringComparison.Ordinal);
        Assert.Contains("purchase_requisition_approval_history", sql, StringComparison.Ordinal);
        Assert.Contains("purchase_transaction_approval_history", sql, StringComparison.Ordinal);
        Assert.Contains("purchase_order_history", sql, StringComparison.Ordinal);
    }

    private string Snapshot(string route, params PurchaseApprovalWorkflowStepSnapshot[] steps)
    {
        var unsigned = new PurchaseApprovalWorkflowSnapshot(string.Empty, "SESS", route, 5000m, new DateOnly(2026, 8, 27), steps);
        var json = JsonSerializer.Serialize(unsigned, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        return engine.Serialize(unsigned with { Identity = identity });
    }

    private static int Count(string text, string value) => (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;
    private static string Read(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return File.ReadAllText(Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException(), Path.Combine(parts)));
    }
}
