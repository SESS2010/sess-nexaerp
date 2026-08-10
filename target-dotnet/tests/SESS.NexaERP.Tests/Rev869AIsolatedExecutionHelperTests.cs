using System.Text.RegularExpressions;

namespace SESS.NexaERP.Tests;

public sealed class Rev869AIsolatedExecutionHelperTests
{
    private static readonly string Root = FindRoot();
    private static readonly string HelperPath = Path.Combine(Root, "tools", "apply-rev869a-isolated-foundation-secure.ps1");
    private static readonly string Source = File.ReadAllText(HelperPath);

    [Fact]
    public void HelperHasFourExplicitFailClosedModes()
    {
        Assert.Contains("[switch]$GeneratePlanOnly", Source);
        Assert.Contains("[switch]$PreflightOnly", Source);
        Assert.Contains("[switch]$Apply", Source);
        Assert.Contains("[switch]$PostMigrationVerification", Source);
        Assert.Contains("Select exactly one mode", Source);
    }

    [Fact]
    public void ExactTargetAndProtectedDatabasesAreHardGuarded()
    {
        Assert.Contains("$targetDatabase = \"sess_nexaerp_rev869a_verify\"", Source);
        Assert.Contains("$Database -cne $targetDatabase", Source);
        foreach (var protectedName in new[] { "sess_nexaerp", "sess_nexaerp_rev868_verify", "postgres", "template0", "template1", "REV861-like names", "production-like names" })
            Assert.Contains(protectedName, Source, StringComparison.Ordinal);
        Assert.Contains("rev861|production|prod|live|main", Source);
    }

    [Fact]
    public void PlanReturnsBeforePasswordPostgresOrEfResolution()
    {
        var planReturn = Source.IndexOf("if ($GeneratePlanOnly) { Write-Plan $preflightSql $postSql; return }", StringComparison.Ordinal);
        Assert.True(planReturn > 0);
        Assert.True(planReturn < Source.IndexOf("Initialize-DatabaseAccess", planReturn, StringComparison.Ordinal));
        Assert.Contains("requests no password, makes no PostgreSQL connection, and performs no dotnet-ef database operation", Source);
        Assert.Contains("no create/drop/restore/backup/main-database/REV861/production operation", Source);
    }

    [Fact]
    public void PlanListsExactMigrationAndObjectContract()
    {
        Assert.Equal(12, Regex.Matches(Source, "202608(?:08|09|10)\\d+_[A-Za-z0-9]+(?:[A-Za-z0-9]+)*").Select(x => x.Value).Distinct().Count());
        Assert.Contains("prerequisite_migrations_count=11", Source);
        Assert.Contains("target_migration_only=$targetMigration", Source);
        Assert.Contains("20260810120000_Rev869AIdentityMasterScopeFoundation", Source);
        Assert.Equal(9, ArraySection("$foundationTables = @(").Count);
        Assert.Equal(3, ArraySection("$backupTables = @(").Count);
        Assert.Equal(7, ArraySection("$nullSafeIndexes = @(").Count);
    }

    [Fact]
    public void PreflightIsSelectOnlyAndChecksEveryAcceptanceGate()
    {
        Assert.Contains("begin transaction read only", Source);
        Assert.Contains("Assert-SelectOnlySql \"Preflight\"", Source);
        foreach (var evidence in new[]
        {
            "bad_prerequisite_count", "target_migration_count", "partial_relation_count", "partial_column_count",
            "partial_index_count", "partial_constraint_count", "partial_function_count", "partial_trigger_count",
            "partial_seed_count", "future_unique_duplicate_count", "future_effective_overlap_count",
            "safe_retry_state=PASS", "data_readiness_state=PASS", "preflight_acceptance_state=PASS"
        }) Assert.Contains(evidence, Source);
        Assert.DoesNotContain("Invoke-Psql $preflightSql $false", Source);
    }

    [Fact]
    public void UomReadinessNeverFabricatesOrUpdatesData()
    {
        Assert.Contains("unmapped_item_count", Source);
        Assert.Contains("invalid_uom_reference_count", Source);
        Assert.Contains("exact_item_uom_evidence_count", Source);
        Assert.Contains("unclassified_measurement_dimension_count", Source);
        Assert.Contains("no default or automatic update is permitted", Source);
        Assert.Contains("unclassified_measurement_dimension_count=0", Source);
    }

    [Fact]
    public void FullApplyRequiresPreflightBackupAndExactTargetMigration()
    {
        var preflight = Source.IndexOf("Assert-Evidence $preflightEvidence \"preflight_acceptance_state=PASS\"", StringComparison.Ordinal);
        var apply = Source.IndexOf("ef database update $targetMigration", StringComparison.Ordinal);
        Assert.True(preflight > 0 && preflight < apply);
        Assert.Contains("Full apply requires approved pre-REV869A backup path and SHA-256 evidence", Source);
        Assert.Contains("Get-FileHash -LiteralPath $backup.Path -Algorithm SHA256", Source);
        Assert.DoesNotContain("database update 0", Source);
        Assert.DoesNotContain("EnsureDeleted", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnsureCreated", Source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PostVerificationCoversSchemaSeedsSafetyAndPreservation()
    {
        foreach (var evidence in new[]
        {
            "foundation_table_count=9", "backup_table_count=3", "null_safe_index_count=7",
            "primary_key_count=9", "restrictive_fk_count=15", "check_constraint_count=22",
            "actual_column_count=149", "migration_owned_seed_count", "all_false_department_manager_count=0",
            "uom_backfill_mismatch_count=0", "tax_resolution_mismatch_count=0",
            "item_backup_mismatch_count=0", "uom_backup_mismatch_count=0", "vendor_backup_mismatch_count=0",
            "database_acceptance_state=PASS", "test_acceptance_state=PASS", "overall_acceptance_state=PASS"
        }) Assert.Contains(evidence, Source);
        Assert.Contains("Rev869A|Rev868C1PostgresWorkflowVerificationTests", Source);
        Assert.Contains("duplicate identity did not fail closed", Source);
        Assert.Contains("cross-warehouse RackBin was accepted", Source);
        Assert.Contains("configuration history update was accepted", Source);
        Assert.Contains("invalid vendor qualification dates were accepted", Source);
    }

    [Fact]
    public void FailureEvidenceIsSanitizedAndSecretsAreCleared()
    {
        Assert.Contains("function Protect-Text", Source);
        Assert.Contains("[REDACTED_CONNECTION]", Source);
        Assert.Contains("overall_acceptance_state=$OverallState", Source);
        foreach (var environmentName in new[] { "PGPASSWORD", "ConnectionStrings__NexaErp", "NexaErp__ExpectedDatabase", "REV869A_POSTGRES", "REV868C1_POSTGRES" })
            Assert.Contains($"Remove-Item Env:\\{environmentName}", Source);
    }

    [Fact]
    public void RollbackEvidenceIsExplicitAndMigrationSourceIsUnchangedByToolingCheckpoint()
    {
        Assert.Contains("Down removes exactly 81 migration-owned seeds", Source);
        Assert.Contains("preserves REV868/REV868C3 business/history rows", Source);
        Assert.Contains("drops rev869a_vendors_prechange_backup, rev869a_uoms_prechange_backup, and rev869a_items_prechange_backup last", Source);
        Assert.Contains("backup/current legacy-column comparisons must be zero", Source);
    }

    private static IReadOnlyList<string> ArraySection(string marker)
    {
        var start = Source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = Source.IndexOf("\n)", start, StringComparison.Ordinal);
        Assert.True(end > start);
        return Regex.Matches(Source[start..end], "\"([^\"]+)\"").Select(x => x.Groups[1].Value).ToList();
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
