namespace SESS.NexaERP.Tests;

public sealed class Rev869BCorrection14PostgresDesignTests
{
    private static readonly string Report = Source("outputs/rev869b_architecture_freeze_root_cause_review.md");
    private static readonly string Contracts = Source("tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs");

    [Fact] public void AcceptanceMatrixHasProvisioningCases() => Map("P01", "P02", "P03");
    [Fact] public void AcceptanceMatrixHasLifecycleCases() => Map("L01", "L02", "L03", "L04", "L05");
    [Fact] public void AcceptanceMatrixHasRecoveryCases() => Map("R01", "R02", "R03");
    [Fact] public void AcceptanceMatrixHasCommandCases() => Map("C01", "C02", "C03", "C04", "C05", "C06", "C07", "C08");
    [Fact] public void AcceptanceMatrixHasPurgeCases() => Map("G01", "G02", "G03", "G04", "G05", "G06");
    [Fact] public void AcceptanceMatrixHasExportCases() => Map("E01", "E02", "E03", "E04");
    [Fact] public void AcceptanceMatrixHasAclCases() => Map("A01", "A02");
    [Fact] public void AcceptanceMatrixHasTestOwnershipCases() => Map("T01", "T02", "T03");
    [Fact] public void ContractsRejectLabelOnlySuccess() { foreach (var term in new[] { "ActionReached", "UnrelatedMutationCount", "CleanupFinalized", "DurableEvidenceCount", "SqlState", "DatabaseObject" }) Assert.Contains(term, Contracts); }
    [Fact] public void PostgreSqlExecutionRemainsExternallyGated() { Assert.Contains("future design only", Report, StringComparison.OrdinalIgnoreCase); Assert.Contains("separately authorized PostgreSQL execution", Report); }

    private static void Map(params string[] ids) { Assert.All(ids, id => Assert.Contains("| " + id + " |", Report)); Assert.Contains(string.Concat(ids.First(), ids.Last()), Contracts); }
    private static string Source(string relative) => File.ReadAllText(Path.Combine(FindRoot(), relative.Replace('/', Path.DirectorySeparatorChar)));
    private static string FindRoot() { for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent) if (File.Exists(Path.Combine(d.FullName, "SESS.NexaERP.slnx"))) return d.FullName; throw new DirectoryNotFoundException(); }
}
