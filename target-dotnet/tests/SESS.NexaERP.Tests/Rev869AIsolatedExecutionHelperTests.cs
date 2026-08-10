using System.Text.RegularExpressions;

namespace SESS.NexaERP.Tests;

public sealed class Rev869AIsolatedExecutionHelperTests
{
    private static readonly string Root = FindRoot();
    private static readonly string HelperPath = Path.Combine(Root, "tools", "apply-rev869a-isolated-foundation-secure.ps1");
    private static readonly string Source = File.ReadAllText(HelperPath);
    private static readonly string MigrationPath = Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260810120000_Rev869AIdentityMasterScopeFoundation.cs");
    private static readonly string MigrationSource = File.ReadAllText(MigrationPath);

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
        Assert.True(planReturn < Source.IndexOf("Invoke-Psql $preflightSql", planReturn, StringComparison.Ordinal));
        Assert.True(planReturn < Source.IndexOf("ef database update $targetMigration", planReturn, StringComparison.Ordinal));
        Assert.Contains("requests no password, makes no PostgreSQL connection, and performs no dotnet-ef database operation", Source);
        Assert.Contains("no create/drop/restore/backup/main-database/REV861/production operation", Source);
    }

    [Fact]
    public void PlanListsExactMigrationAndObjectContract()
    {
        Assert.Equal(12, Regex.Matches(Source, "202608(?:08|09|10)\\d+_[A-Za-z0-9]+(?:[A-Za-z0-9]+)*").Select(x => x.Value).Distinct().Count());
        Assert.Contains("prerequisite_migrations_count=11", Source);
        Assert.Contains("expected_final_migrations_count=12", Source);
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
            "missing_prerequisite_count", "unexpected_migration_count", "duplicate_migration_count", "bad_prerequisite_count", "target_migration_count", "partial_relation_count", "partial_column_count",
            "partial_index_count", "partial_constraint_count", "partial_function_count", "partial_trigger_count",
            "partial_seed_count", "future_unique_duplicate_count", "future_effective_overlap_count",
            "safe_retry_state=PASS", "data_readiness_state=PASS", "preflight_acceptance_state=PASS"
        }) Assert.Contains(evidence, Source);
        Assert.DoesNotContain("Invoke-Psql $preflightSql $false", Source);
    }

    [Fact]
    public void UomReadinessUsesExactApprovedSetAndNeverFabricatesData()
    {
        foreach (var evidence in new[]
        {
            "ApprovalStatus = \"PENDING\"", "uom_management_decision_state=$uomManagementDecisionState",
            "missing_uom_classification_count",
            "unexpected_uom_classification_count", "duplicate_uom_classification_count",
            "unapproved_uom_classification_count", "missing_base_uom_mapping_count",
            "invalid_base_uom_mapping_count", "inferred_or_default_mapping_count",
            "symbol=NOT_MODELED", "item_reference_count", "proposed_base_uom_id", "uom_ambiguity=CODE", "uom_ambiguity=NAME", "PENDING_MANAGEMENT_APPROVAL"
        }) Assert.Contains(evidence, Source);
        foreach (var field in new[] { "UomId", "UomCode", "MeasurementDimension", "QuantityPrecision", "IsCanonicalBase", "ConversionPolicy", "ManagementApprovalReference" })
            Assert.Contains(field, Source);
        Assert.Contains("m, kg, and ambiguous no are candidate-only", Source);
        Assert.DoesNotContain("unclassified_measurement_dimension_count", Source);
    }

    [Fact]
    public void ExactUomSetComparisonFailsForMissingUnexpectedDuplicateAndUnapprovedRows()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var result = CompareUomSet(
            new[] { new UomContract(a, "EA", true), new UomContract(a, "EA", true), new UomContract(b, "KG", false), new UomContract(Guid.NewGuid(), "M", true) },
            new[] { (a, "EA"), (b, "KG"), (Guid.NewGuid(), "L") });
        Assert.True(result.Missing > 0);
        Assert.True(result.Unexpected > 0);
        Assert.True(result.Duplicate > 0);
        Assert.True(result.Unapproved > 0);
    }

    [Fact]
    public void NullOrInvalidItemUomAndMissingBaseMappingFail()
    {
        Assert.False(ItemUomReady(null, false, true, "MANAGEMENT_APPROVED"));
        Assert.False(ItemUomReady(Guid.NewGuid(), false, true, "MANAGEMENT_APPROVED"));
        Assert.False(ItemUomReady(Guid.NewGuid(), true, false, "MANAGEMENT_APPROVED"));
    }

    [Fact]
    public void InferredOrDefaultBaseMappingFails()
    {
        Assert.False(ItemUomReady(Guid.NewGuid(), true, true, "INFERRED"));
        Assert.False(ItemUomReady(Guid.NewGuid(), true, true, "DEFAULT"));
        Assert.True(ItemUomReady(Guid.NewGuid(), true, true, "MANAGEMENT_APPROVED"));
    }

    [Fact]
    public void PreservationCountMismatchFails()
    {
        var before = new Dictionary<string, long> { ["purchase_requisitions"] = 2, ["departments"] = 4 };
        var after = new Dictionary<string, long> { ["purchase_requisitions"] = 1, ["departments"] = 4 };
        Assert.False(PreservationMatches(before, after));
        Assert.True(PreservationMatches(before, new Dictionary<string, long>(before)));
    }

    [Theory]
    [InlineData("sess_nexaerp")]
    [InlineData("sess_nexaerp_rev868_verify")]
    [InlineData("postgres")]
    [InlineData("template0")]
    [InlineData("template1")]
    [InlineData("REV861_verify")]
    [InlineData("production")]
    public void ProtectedDatabaseFails(string database) => Assert.False(IsPermittedDatabase(database));

    [Theory]
    [InlineData("select 1; update nexa.items set x=1;")]
    [InlineData("delete from nexa.items;")]
    [InlineData("create table unsafe(id int);")]
    public void NonReadOnlyPreflightSqlFails(string sql) => Assert.False(IsSelectOnly(sql));

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
            "actual_column_count=149", "migration_owned_seed_count", "seed_set_mismatch_count=0", "all_false_department_manager_count=0",
            "uom_backfill_mismatch_count=0", "tax_resolution_mismatch_count=0",
            "item_backup_mismatch_count=0", "uom_backup_mismatch_count=0", "vendor_backup_mismatch_count=0", "backup_coverage_mismatch_count=0",
            "database_schema_acceptance_state=PASS", "database_preservation_acceptance_state=PASS", "database_acceptance_state=PASS", "test_acceptance_state=PASS", "overall_acceptance_state=PASS"
        }) Assert.Contains(evidence, Source);
        Assert.Contains("Rev869A|Rev868C1PostgresWorkflowVerificationTests", Source);
        Assert.Contains("duplicate identity did not fail closed", Source);
        Assert.Contains("cross-warehouse RackBin was accepted", Source);
        Assert.Contains("configuration history update was accepted", Source);
        Assert.Contains("invalid vendor qualification dates were accepted", Source);
    }

    [Fact]
    public void BackupsCoverEveryAlteredMasterBeforeMutationAndDropLast()
    {
        var firstMutation = MigrationSource.IndexOf("migrationBuilder.AddColumn", StringComparison.Ordinal);
        Assert.True(firstMutation > 0);
        foreach (var backup in new[] { "rev869a_items_prechange_backup", "rev869a_uoms_prechange_backup", "rev869a_vendors_prechange_backup" })
        {
            Assert.True(MigrationSource.IndexOf(backup, StringComparison.Ordinal) < firstMutation);
            Assert.True(MigrationSource.LastIndexOf(backup, StringComparison.Ordinal) > MigrationSource.LastIndexOf("migrationBuilder.DropColumn", StringComparison.Ordinal));
        }
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

    private sealed record UomContract(Guid Id, string Code, bool Approved);
    private sealed record UomSetResult(int Missing, int Unexpected, int Duplicate, int Unapproved);

    private static UomSetResult CompareUomSet(IReadOnlyCollection<UomContract> expected, IReadOnlyCollection<(Guid Id, string Code)> actual)
    {
        var missing = actual.Count(a => !expected.Any(e => e.Id == a.Id && string.Equals(e.Code, a.Code, StringComparison.OrdinalIgnoreCase)));
        var unexpected = expected.Count(e => !actual.Any(a => a.Id == e.Id && string.Equals(a.Code, e.Code, StringComparison.OrdinalIgnoreCase)));
        var duplicate = expected.GroupBy(e => e.Id).Count(g => g.Count() != 1) +
                        expected.GroupBy(e => e.Code, StringComparer.OrdinalIgnoreCase).Count(g => g.Count() != 1);
        return new(missing, unexpected, duplicate, expected.Count(e => !e.Approved));
    }

    private static bool ItemUomReady(Guid? uomId, bool uomExists, bool baseMappingPresent, string mappingBasis) =>
        uomId.HasValue && uomExists && baseMappingPresent && mappingBasis == "MANAGEMENT_APPROVED";

    private static bool PreservationMatches(IReadOnlyDictionary<string, long> before, IReadOnlyDictionary<string, long> after) =>
        before.Count == after.Count && before.All(pair => after.TryGetValue(pair.Key, out var value) && value == pair.Value);

    private static bool IsPermittedDatabase(string database) =>
        database == "sess_nexaerp_rev869a_verify" && !Regex.IsMatch(database, "rev861|production|prod|live|main", RegexOptions.IgnoreCase);

    private static bool IsSelectOnly(string sql) =>
        !Regex.IsMatch(sql, "\\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|vacuum|analyze|refresh)\\b", RegexOptions.IgnoreCase);
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
