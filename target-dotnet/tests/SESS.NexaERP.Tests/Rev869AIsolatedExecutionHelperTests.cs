using System.Data;
using System.Text.RegularExpressions;
using Npgsql;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev869AIsolatedExecutionHelperTests
{
    private static readonly string Root = FindRoot();
    private static readonly string HelperPath = Path.Combine(Root, "tools", "apply-rev869a-isolated-foundation-secure.ps1");
    private static string Source => NormalizeLineEndings(File.ReadAllText(HelperPath));
    private static readonly string MigrationPath = Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260824032638_AdvanceInitialBaseline.cs");
    private static string MigrationSource => File.ReadAllText(MigrationPath);
    private static string DbContextSource => File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.cs")) + Environment.NewLine +
        File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.Rev869A.cs"));
    private static string ModelSnapshotSource => File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "NexaErpDbContextModelSnapshot.cs"));
    private static string ProvisioningHelperSource => File.ReadAllText(Path.Combine(Root, "tools", "prepare-rev869a-isolated-database-secure.ps1"));
    private static string Rev868C3WorkbookSource => File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Rev868C3EmployeeWorkbookData.cs"));
    private static string Rev868C3VerifierSource => File.ReadAllText(Path.Combine(Root, "tools", "verify-rev868c3-postrun-readonly-secure.ps1"));
    private static readonly string[] ExpectedRelievedCodes =
    {
        "SESS-016", "SESS-018", "SESS-022", "SESS-027", "SESS-028", "SESS-032", "SESS-036", "SESS-037", "SESS-039"
    };
    private static readonly string[] ExpectedActiveCodes =
    {
        "SESS-001", "SESS-002", "SESS-003", "SESS-004", "SESS-005", "SESS-006", "SESS-007", "SESS-008", "SESS-009", "SESS-010", "SESS-011", "SESS-012", "SESS-013", "SESS-014", "SESS-015", "SESS-017", "SESS-019", "SESS-020", "SESS-021", "SESS-023", "SESS-024", "SESS-025", "SESS-026", "SESS-029", "SESS-030", "SESS-031", "SESS-033", "SESS-034", "SESS-035", "SESS-038", "SESS-040", "SESS-041", "SESS-042", "SESS-043", "SESS-044", "SESS-045", "SESS-046", "SESS-047", "SESS-048", "SESS-049", "SESS-050", "SESS-051"
    };
    private static readonly HashSet<string> AcceptedRelievedStatuses =
        new(new[] { "left / resigned", "left/resigned", "resigned", "inactive" }, StringComparer.Ordinal);
    private static readonly string[] CanonicalHelperRelations =
    {
        "controlled_configuration_histories", "department_approval_mappings", "departments", "employee_identity_mappings", "employees",
        "items", "organization_policies", "page_definitions", "purchase_requisition_approval_history", "purchase_requisitions",
        "qc_inspection_policies", "rack_bins", "rev869a_items_prechange_backup", "rev869a_uoms_prechange_backup",
        "rev869a_vendors_prechange_backup", "role_page_permissions", "roles", "stock_reservations", "tax_gst_settings",
        "uom_conversions", "uoms", "vendor_qualifications", "vendors", "warehouse_condition_locations", "warehouses"
    };
    private static readonly string[] CanonicalPreservationRelations =
    {
        "employees", "departments", "department_approval_mappings", "purchase_requisitions",
        "purchase_requisition_approval_history", "purchase_requisition_status_history", "stock_availability_checks",
        "stock_availability_check_lines", "stock_reservations", "stock_reservation_history", "purchase_requirement_handoffs",
        "purchase_approval_route_settings", "purchase_approval_workflow_steps", "page_definitions", "role_page_permissions",
        "audit_logs", "employee_status_history", "employee_department_history", "employee_approval_history", "employee_import_history"
    };

    [Fact]
    public void HelperHasFiveExplicitFailClosedModes()
    {
        Assert.Contains("[switch]$GeneratePlanOnly", Source);
        Assert.Contains("[switch]$PreflightOnly", Source);
        Assert.Contains("[switch]$Apply", Source);
        Assert.Contains("[switch]$PostMigrationVerification", Source);
        Assert.Contains("[switch]$ResumePostApplyAcceptance", Source);
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
    public void UomReadinessUsesTheExactManagementApprovedEaPlan()
    {
        foreach (var evidence in new[]
        {
            "ApprovalStatus = \"APPROVED\"", "uom_management_decision_state=$uomManagementDecisionState",
            "approved_uom_plan_count", "approved_new_uom_count", "approved_existing_uom_count",
            "uom_id_collision_count", "uom_code_collision_count", "uom_name_collision_count",
            "uom_creation_plan_state", "approved_item_mapping_count", "approved_mapping_actual_matched_count",
            "approved_mapping_missing_item_count", "approved_mapping_unexpected_item_count",
            "approved_mapping_duplicate_count", "approved_mapping_invalid_uom_count",
            "unresolved_unmapped_item_count", "item_mapping_plan_state"
        }) Assert.Contains(evidence, Source);
        foreach (var value in new[]
        {
            "f71a4725-bb15-e7bf-e97b-991985e96328", "EA", "Each", "COUNT", "IDENTITY_ONLY",
            "CREATE", "APPROVED", "MGMT-REV869A-UOM-20260810-001",
            "8c428e59-db05-471d-a7e7-4f7dc1c13b54", "REV868C1-ITEM", "MANAGEMENT_APPROVED"
        }) Assert.Contains(value, Source);
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
    public void GeneratedSqlContainsNoUnsupportedUuidMinOrMaxAggregate()
    {
        Assert.DoesNotMatch(new Regex(@"(?is)\b(?:min|max)\s*\(\s*""[^""]*(?:Id|ID)""\s*\)"), Source);
        Assert.DoesNotMatch(new Regex(@"(?is)\b(?:min|max)\s*\(\s*""UomId""\s*::\s*text"), Source);
    }

    [Fact]
    public void DuplicateUomIdAndNormalizedCodeGroupsAreCountedSeparatelyAndAdded()
    {
        Assert.Matches(new Regex(@"select\s+""UomId""\s+from expected_uom_classifications\s+group by ""UomId""", RegexOptions.IgnoreCase), Source);
        Assert.Contains("select upper(trim(\"UomCode\")) from expected_uom_classifications", Source, StringComparison.Ordinal);
        Assert.Contains("group by upper(trim(\"UomCode\"))", Source, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(Source, @"select ""UomId"" from expected_uom_classifications group by ""UomId""").Cast<Match>());
        Assert.Single(Regex.Matches(Source, @"select upper\(trim\(""UomCode""\)\) from expected_uom_classifications group by upper\(trim\(""UomCode""\)\)").Cast<Match>());
    }

    [Fact]
    public void DuplicateClassificationCountsAreAdditiveAndEmptySetIsZero()
    {
        Assert.Equal(0, CompareUomSet(Array.Empty<UomContract>(), Array.Empty<(Guid, string)>()).Duplicate);

        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        Assert.Equal(1, CompareUomSet(new[] { new UomContract(a, "EA", true), new UomContract(a, "KG", true) }, Array.Empty<(Guid, string)>()).Duplicate);
        Assert.Equal(1, CompareUomSet(new[] { new UomContract(a, " EA ", true), new UomContract(b, "ea", true) }, Array.Empty<(Guid, string)>()).Duplicate);
        Assert.Equal(2, CompareUomSet(new[] { new UomContract(a, "EA", true), new UomContract(a, " ea ", true) }, Array.Empty<(Guid, string)>()).Duplicate);
    }

    [Fact]
    public void EitherDuplicateIdOrDuplicateNormalizedCodeFailsReadiness()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var duplicateId = CompareUomSet(new[] { new UomContract(a, "EA", true), new UomContract(a, "KG", true) }, Array.Empty<(Guid, string)>());
        var duplicateCode = CompareUomSet(new[] { new UomContract(a, "EA", true), new UomContract(b, " ea ", true) }, Array.Empty<(Guid, string)>());

        Assert.False(duplicateId.Duplicate == 0);
        Assert.False(duplicateCode.Duplicate == 0);
    }

    [Fact]
    public void EveryGeneratedSqlRelationMatchesCanonicalPhysicalContract()
    {
        var preflight = FunctionBlock("function Get-PreflightSql", "function Get-PostMigrationSql");
        var post = FunctionBlock("function Get-PostMigrationSql", "function Get-TransactionalVerificationSql");
        var transactional = FunctionBlock("function Get-TransactionalVerificationSql", "function Invoke-Psql");
        var actual = new[] { preflight, post, transactional }.SelectMany(PhysicalRelations).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.Equal(CanonicalHelperRelations.OrderBy(x => x, StringComparer.Ordinal), actual);
        foreach (var relation in actual.Where(x => !x.StartsWith("rev869a_", StringComparison.Ordinal)))
        {
            Assert.Contains($"ToTable(\"{relation}\"", DbContextSource, StringComparison.Ordinal);
            Assert.Contains($"ToTable(\"{relation}\", \"advance\"", ModelSnapshotSource, StringComparison.Ordinal);
        }
        foreach (var backup in actual.Where(x => x.StartsWith("rev869a_", StringComparison.Ordinal)))
        {
            Assert.Contains(backup, Source, StringComparison.Ordinal);
            Assert.DoesNotContain($"ToTable(\"{backup}\"", ModelSnapshotSource, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("nexa.purchase_requisition_approval_histories", Source, StringComparison.Ordinal);
        Assert.Contains("nexa.purchase_requisition_approval_history", preflight, StringComparison.Ordinal);
        Assert.Contains("nexa.purchase_requisition_approval_history", post, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptedPreservationRelationsMatchProvisioningEfAndSnapshotContracts()
    {
        foreach (var relation in CanonicalPreservationRelations)
        {
            Assert.Contains($"{relation} = \"nexa.{relation}\"", ProvisioningHelperSource, StringComparison.Ordinal);
            Assert.Contains($"ToTable(\"{relation}\"", DbContextSource, StringComparison.Ordinal);
            Assert.Contains($"ToTable(\"{relation}\", \"advance\"", ModelSnapshotSource, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MissingOrInventedPhysicalRelationFailsExactContract()
    {
        Assert.True(HasExactPhysicalRelations(CanonicalHelperRelations));
        Assert.False(HasExactPhysicalRelations(CanonicalHelperRelations.Skip(1)));
        Assert.False(HasExactPhysicalRelations(CanonicalHelperRelations.Append("purchase_requisition_approval_histories")));
    }

    [Fact]
    public void RequiredPreservationCountsHaveNoGuessedOrSilentFallback()
    {
        var preflight = FunctionBlock("function Get-PreflightSql", "function Get-PostMigrationSql");
        var post = FunctionBlock("function Get-PostMigrationSql", "function Get-TransactionalVerificationSql");
        const string requiredCount = "(select count(*) from nexa.purchase_requisition_approval_history) as pr_approval_history_count";

        Assert.Single(Regex.Matches(preflight, Regex.Escape(requiredCount)).Cast<Match>());
        Assert.Single(Regex.Matches(post, Regex.Escape(requiredCount)).Cast<Match>());
        Assert.DoesNotMatch(new Regex(@"(?i)to_regclass\([^\r\n]*purchase_requisition_approval_histor"), preflight + post);
        Assert.DoesNotMatch(new Regex(@"(?i)information_schema[^\r\n]*purchase_requisition_approval_histor"), preflight + post);
        Assert.True(IsSelectOnly(preflight));
        Assert.True(IsSelectOnly(post));
    }

    [Fact]
    public void ExistingDepartmentManagerIsReusedAndRoleReadinessFailsClosed()
    {
        var preflight = FunctionBlock("function Get-PreflightSql", "function Get-PostMigrationSql");
        var post = FunctionBlock("function Get-PostMigrationSql", "function Get-TransactionalVerificationSql");
        foreach (var label in new[] { "existing_department_manager_role_count", "existing_department_manager_active_count", "existing_department_manager_duplicate_count", "existing_department_manager_reuse_state", "department_manager_role_fingerprint" })
        {
            Assert.Contains(label, preflight, StringComparison.Ordinal);
            Assert.Contains(label, post, StringComparison.Ordinal);
        }
        Assert.Contains("new_role_collision_count", preflight, StringComparison.Ordinal);
        Assert.Contains("role_readiness_state", preflight, StringComparison.Ordinal);
        Assert.Contains("existing_department_manager_role_count=1 and existing_department_manager_active_count=1 and existing_department_manager_duplicate_count=0", preflight, StringComparison.Ordinal);
        Assert.Contains("new_role_collision_count=0", preflight, StringComparison.Ordinal);
        Assert.Contains("Get-EvidenceTextValue $Before 'department_manager_role_fingerprint'", Source, StringComparison.Ordinal);
        Assert.Contains("expected_permission_specs", post, StringComparison.Ordinal);
        Assert.Contains("'DEPARTMENT_MANAGER'", post, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(2, 2, 2)]
    [InlineData(1, 1, 0)]
    public void MissingInactiveDuplicateOrUnsuitableDepartmentManagerFailsClosed(int count, int active, int suitable)
    {
        Assert.False(DepartmentManagerReusable(count, active, suitable));
    }

    [Fact]
    public void RawNullItemEvidenceIsAllowedOnlyWhenTheApprovedPlanCoversItExactly()
    {
        var preflight = FunctionBlock("function Get-PreflightSql", "function Get-PostMigrationSql");
        foreach (var evidence in new[] { "unmapped_item_count", "null_item_uom_count", "item_uom_problem=", "approved_item_mapping=", "approved_mapping_actual_matched_count", "unresolved_unmapped_item_count" })
            Assert.Contains(evidence, preflight, StringComparison.Ordinal);
        Assert.DoesNotContain("unmapped_item_count=0 and", preflight, StringComparison.Ordinal);
        Assert.Contains("approved_mapping_actual_matched_count=1", preflight, StringComparison.Ordinal);
        Assert.Contains("approved_mapping_unexpected_item_count=0", preflight, StringComparison.Ordinal);
        Assert.Contains("unresolved_unmapped_item_count=0", preflight, StringComparison.Ordinal);
        Assert.True(ApprovedPlanReady(1, 1, 0, 0, 0, 0, 1));
        Assert.False(ApprovedPlanReady(2, 1, 0, 1, 0, 0, 1));
        Assert.False(ApprovedPlanReady(1, 0, 1, 0, 0, 0, 1));
        Assert.False(ApprovedPlanReady(1, 1, 0, 0, 0, 0, 0));
        Assert.False(ApprovedPlanReady(1, 1, 0, 0, 0, 0, 2));
        Assert.False(ApprovedPlanReady(1, 1, 0, 0, 1, 0, 1));
        Assert.False(ApprovedPlanReady(1, 1, 0, 0, 0, 1, 1));
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
            "actual_column_count=149", "role_seed_count=4", "permission_seed_count=74", "migration_owned_seed_count=88", "seed_set_mismatch_count=0", "all_false_department_manager_count=0", "department_manager_permission_mismatch_count=0",
            "uom_backfill_mismatch_count=0", "tax_resolution_mismatch_count=0",
            "item_backup_mismatch_count=0", "uom_backup_mismatch_count=0", "vendor_backup_mismatch_count=0", "backup_coverage_mismatch_count=0",
            "database_schema_acceptance_state=PASS", "database_preservation_acceptance_state=PASS", "database_acceptance_state=PASS", "test_acceptance_state=PASS", "overall_acceptance_state=PASS"
        }) Assert.Contains(evidence, Source);
        Assert.Contains("Rev869A|Rev868C1PostgresWorkflowVerificationTests", Source);
        Assert.Contains("transactional_constraint_failed=duplicate_identity", Source);
        Assert.Contains("transactional_constraint_failed=cross_warehouse_rack_bin", Source);
        Assert.Contains("transactional_constraint_failed=configuration_history_mutation", Source);
        Assert.Contains("transactional_constraint_failed=invalid_vendor_qualification_dates", Source);
    }

    [Fact]
    public void ResumeModeUsesApprovedPreApplyEvidenceAndExactPreservationGates()
    {
        foreach (var value in new[]
        {
            "ApprovedPreApplyEvidencePath", "ApprovedPreApplyEvidenceSha256", "Get-FileHash -LiteralPath $fullPath -Algorithm SHA256",
            "database_identity=PASS", "target_migration_count=0", "preflight_acceptance_state=PASS",
            "migration_count=12", "target_migration_count=1", "database_schema_acceptance_state=PASS",
            "database_preservation_acceptance_state=PASS", "database_acceptance_state=PASS", "overall_acceptance_state=PASS"
        }) Assert.Contains(value, Source, StringComparison.Ordinal);

        foreach (var key in new[]
        {
            "preserve_pr_count", "preserve_pr_approval_history_count", "preserve_reservation_count",
            "preserve_active_employee_count", "preserve_department_count", "preserve_manager_mapping_count"
        }) Assert.Contains(key, Source, StringComparison.Ordinal);
        Assert.Contains("Numeric evidence key must occur exactly once and contain digits only", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingDuplicateMalformedOrConflictingResumeEvidenceFailsClosed()
    {
        Assert.False(HasExactlyOneEvidence("", "state", "PASS"));
        Assert.False(HasExactlyOneEvidence("state=PASS\nstate=PASS", "state", "PASS"));
        Assert.False(HasExactlyOneEvidence("state=pass", "state", "PASS"));
        Assert.False(HasExactlyOneEvidence("state=FAIL", "state", "PASS"));
        Assert.True(HasExactlyOneEvidence("state=PASS", "state", "PASS"));
        Assert.False(HasExactlyOneNumericEvidence("count=abc", "count"));
        Assert.False(HasExactlyOneNumericEvidence("count=1\ncount=1", "count"));
        Assert.True(HasExactlyOneNumericEvidence("count=12", "count"));
    }

    [Fact]
    public void ResumeWorkflowRunsOnlyRolledBackConstraintsAndExactRev869APostgresTests()
    {
        var transactional = FunctionBlock("function Get-TransactionalVerificationSql", "function Invoke-Psql");
        var resumeTests = FunctionBlock("function Invoke-ResumeAcceptanceTests", "try {\n    Assert-Mode");
        Assert.StartsWith("function Invoke-ResumeAcceptanceTests", resumeTests, StringComparison.Ordinal);
        Assert.Contains("Invoke-Psql (Get-TransactionalVerificationSql) $false", resumeTests, StringComparison.Ordinal);
        Assert.Contains("FullyQualifiedName~Rev869APostgresAcceptanceTests", resumeTests, StringComparison.Ordinal);
        Assert.Contains("--logger \"trx;LogFileName=rev869a_resume_acceptance.trx\"", resumeTests, StringComparison.Ordinal);
        Assert.Contains("begin;", transactional, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rollback;", transactional, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("commit", transactional, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(new Regex(@"(?i)\b(ef\s+database|pg_dump|pg_restore|createdb|create\s+database|drop\s+database|database\s+(?:update|remove)|repair)\b"), resumeTests);
    }

    [Fact]
    public void TransactionalVerifierUsesOnlyProvenEmployeeAndTestOwnedMasterPrerequisites()
    {
        var transactional = RenderedTransactionalSql();
        Assert.DoesNotContain("transactional prerequisites unavailable", transactional, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("select \"Id\" into v from nexa.vendors", transactional, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("select \"Id\" into w1 from nexa.warehouses", transactional, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("transactional_prerequisite_existing_vendor_state=NOT_REQUIRED_TEST_OWNED", transactional, StringComparison.Ordinal);
        Assert.Contains("insert into nexa.vendors", transactional, StringComparison.Ordinal);
        Assert.Contains("insert into nexa.warehouses", transactional, StringComparison.Ordinal);
        Assert.Contains("insert into nexa.rack_bins", transactional, StringComparison.Ordinal);
        Assert.DoesNotContain("\"LoginEnabled\"=true", transactional, StringComparison.OrdinalIgnoreCase);
        foreach (var label in new[] { "expected_count", "actual_matched_count", "missing_count", "unexpected_count", "duplicate_count", "status_mismatch_count", "state" })
            Assert.Single(Regex.Matches(transactional, $"transactional_prerequisite_active_employee_{label}=").Cast<Match>());
        foreach (var code in ExpectedActiveCodes)
        {
            Assert.Contains($"Employee(\"{code}\"", Rev868C3WorkbookSource, StringComparison.Ordinal);
            Assert.True(Regex.Matches(transactional, Regex.Escape(code)).Count >= 2, code);
        }
    }

    [Fact]
    public void RenderedTransactionalSqlHasSevenPairedUniqueDollarQuoteTags()
    {
        var sql = RenderedTransactionalSql();
        var openings = Regex.Matches(sql, @"(?im)^[ ]*do[ ]+([$]rev869a_[a-z]+[$])[ ]*$").Select(x => x.Groups[1].Value).ToArray();
        Assert.Equal(7, openings.Length);
        Assert.Equal(7, openings.Distinct(StringComparer.Ordinal).Count());
        foreach (var tag in openings) Assert.Equal(2, Regex.Matches(sql, Regex.Escape(tag)).Count);
        Assert.DoesNotMatch(new Regex(@"(?im)^[ ]*do[ ]+[$][a-z0-9_]+[ ]*$"), sql);
        Assert.DoesNotContain("`$", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void TransactionalActiveEmployeeSetExactlyMatchesRev868C3WorkbookSource()
    {
        var sql = RenderedTransactionalSql();
        var cteEnd = sql.IndexOf("), metrics as", StringComparison.Ordinal);
        Assert.True(cteEnd > 0);
        var sqlCodes = Regex.Matches(sql[..cteEnd], "SESS-[0-9]{3}").Select(x => x.Value).ToArray();
        Assert.Equal(ExpectedActiveCodes, sqlCodes);

        var workbookStart = Rev868C3WorkbookSource.IndexOf("ActiveEmployees =>", StringComparison.Ordinal);
        var workbookEnd = Rev868C3WorkbookSource.IndexOf("ManagerMappings =>", workbookStart, StringComparison.Ordinal);
        Assert.True(workbookStart >= 0 && workbookEnd > workbookStart);
        var workbookCodes = Regex.Matches(Rev868C3WorkbookSource[workbookStart..workbookEnd], @"Employee[(][""](?<code>SESS-[0-9]{3})[""]")
            .Select(x => x.Groups["code"].Value).ToArray();
        Assert.Equal(ExpectedActiveCodes, workbookCodes);
    }

    [Theory]
    [InlineData(0, 42, 0, 0, 0)]
    [InlineData(1, 41, 0, 0, 0)]
    [InlineData(43, 0, 1, 0, 0)]
    [InlineData(42, 0, 0, 1, 0)]
    [InlineData(42, 0, 0, 0, 1)]
    public void ZeroOneMultipleDuplicateOrStatusMismatchedActiveEmployeesFailClosed(int matched, int missing, int unexpected, int duplicate, int statusMismatch)
    {
        var counts = TransactionalPrerequisiteCounts();
        counts["active_employee_actual_matched"] = matched;
        counts["active_employee_missing"] = missing;
        counts["active_employee_unexpected"] = unexpected;
        counts["active_employee_duplicate"] = duplicate;
        counts["active_employee_status_mismatch"] = statusMismatch;
        Assert.False(TransactionalPrerequisitesPass(counts));
    }

    [Theory]
    [InlineData("active_employee")]
    [InlineData("identity_collision")]
    [InlineData("uom_collision")]
    [InlineData("tax_collision")]
    [InlineData("qc_collision")]
    [InlineData("vendor_collision")]
    [InlineData("warehouse_collision")]
    [InlineData("history_collision")]
    public void EveryIndividualMissingOrCollidingPrerequisiteFailsClosed(string failedPrerequisite)
    {
        var counts = TransactionalPrerequisiteCounts();
        if (failedPrerequisite == "active_employee") counts["active_employee_actual_matched"] = 41;
        else counts[failedPrerequisite] = 1;
        Assert.False(TransactionalPrerequisitesPass(counts));
        Assert.Contains($"transactional_prerequisite_failed={failedPrerequisite}", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void TemporarySupportingRecordsAreCreatedBeforeDependentNegativeTests()
    {
        var transactional = FunctionBlock("function Get-TransactionalVerificationSql", "function Invoke-Psql");
        Assert.True(transactional.IndexOf("insert into nexa.uoms", StringComparison.Ordinal) < transactional.IndexOf("insert into nexa.uom_conversions", StringComparison.Ordinal));
        Assert.True(transactional.IndexOf("insert into nexa.vendors", StringComparison.Ordinal) < transactional.IndexOf("insert into nexa.vendor_qualifications", StringComparison.Ordinal));
        Assert.True(transactional.IndexOf("insert into nexa.warehouses", StringComparison.Ordinal) < transactional.IndexOf("insert into nexa.rack_bins", StringComparison.Ordinal));
        Assert.True(transactional.IndexOf("insert into nexa.rack_bins", StringComparison.Ordinal) < transactional.IndexOf("insert into nexa.warehouse_condition_locations", StringComparison.Ordinal));
        Assert.True(transactional.IndexOf("insert into nexa.controlled_configuration_histories", StringComparison.Ordinal) < transactional.IndexOf("update nexa.controlled_configuration_histories", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryTemporaryUuidAndCodeHasACollisionGuard()
    {
        var transactional = FunctionBlock("function Get-TransactionalVerificationSql", "function Invoke-Psql");
        foreach (var label in new[] { "identity", "uom", "tax", "qc", "vendor", "warehouse", "history" })
        {
            Assert.Contains($"transactional_prerequisite_{label}_collision_count=", transactional, StringComparison.Ordinal);
            Assert.Contains($"transactional_prerequisite_{label}_collision_state=", transactional, StringComparison.Ordinal);
            Assert.Contains($"transactional_prerequisite_failed={label}_collision", transactional, StringComparison.Ordinal);
        }
        foreach (var code in new[] { "REV869A_TEST_FROM", "REV869A_TEST_TO", "REV869A_QC_TEST", "REV869A_TEST_VENDOR", "REV869A_TEST_WH_A", "REV869A_TEST_WH_B", "REV869A_TEST_BIN", "REV869A_TEST_HISTORY" })
            Assert.True(Regex.Matches(transactional, Regex.Escape(code)).Count >= 2, code);
    }

    [Fact]
    public void TransactionalSqlHasOneRollbackNoCommitAndNoBusinessRowMutation()
    {
        var transactional = FunctionBlock("function Get-TransactionalVerificationSql", "function Invoke-Psql");
        Assert.Single(Regex.Matches(transactional, @"(?im)^begin;$").Cast<Match>());
        Assert.Single(Regex.Matches(transactional, @"(?im)^rollback;$").Cast<Match>());
        Assert.DoesNotMatch(new Regex(@"(?im)^\s*commit\s*;"), transactional);
        Assert.DoesNotMatch(new Regex(@"(?i)\b(update|delete)\s+nexa\.(employees|uoms|vendors|warehouses|rack_bins|purchase_requisitions|stock_reservations)\b"), transactional);
        Assert.DoesNotContain("truncate ", transactional, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TransactionalFailurePreventsAndPassPermitsPostgresTests()
    {
        var resumeTests = FunctionBlock("function Invoke-ResumeAcceptanceTests", "try {\n    Assert-Mode");
        var invokeTransactional = resumeTests.IndexOf("Invoke-Psql (Get-TransactionalVerificationSql) $false", StringComparison.Ordinal);
        var enforceTransactional = resumeTests.IndexOf("Assert-Evidence $transactionalEvidence $required", StringComparison.Ordinal);
        var invokeDotnet = resumeTests.IndexOf("& $dotnet test", StringComparison.Ordinal);
        Assert.True(invokeTransactional >= 0 && enforceTransactional > invokeTransactional && invokeDotnet > enforceTransactional);
        Assert.False(CanRunPostgresTests(false));
        Assert.True(CanRunPostgresTests(true));
    }

    [Theory]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public void FinalOverallAcceptanceRequiresSchemaPreservationAndTests(bool schema, bool preservation, bool tests, bool expected) =>
        Assert.Equal(expected, OverallAcceptance(schema, preservation, tests));

    [Fact]
    public void ResumeTestEvidenceIsSanitizedAndCapturedOnFailure()
    {
        var resumeTests = FunctionBlock("function Invoke-ResumeAcceptanceTests", "try {\n    Assert-Mode");
        foreach (var label in new[] { "transactional_constraint_test_state", "transactional_output_evidence_path", "transactional_rollback_state", "trx_evidence_path", "postgresql_test_output_evidence_path", "rev869a_postgresql_test_state", "test_acceptance_state" })
            Assert.Contains(label, resumeTests, StringComparison.Ordinal);
        Assert.Contains("Protect-Text", resumeTests, StringComparison.Ordinal);
        Assert.Contains("Test-Path -LiteralPath $trxPath -PathType Leaf", resumeTests, StringComparison.Ordinal);
        Assert.Contains("WriteAllText($transactionalOutputPath", resumeTests, StringComparison.Ordinal);
        Assert.Contains("WriteAllText($postgresTestOutputPath", resumeTests, StringComparison.Ordinal);
    }

    [Fact]
    public void RelievedPreservationUsesExactNineCodesAndNoRelievedLiteralPredicate()
    {
        Assert.DoesNotContain("\"Status\"='Relieved'", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("relieved_employee_count", Source, StringComparison.Ordinal);
        var declaration = Regex.Match(Source, @"\$relievedEmployeeCodes = @\((?<values>[^)]*)\)");
        Assert.True(declaration.Success);
        var codes = Regex.Matches(declaration.Groups["values"].Value, "SESS-[0-9]{3}").Select(x => x.Value).ToArray();
        Assert.Equal(ExpectedRelievedCodes, codes);
        foreach (var code in ExpectedRelievedCodes)
            Assert.Contains($"Relieved(\"{code}\"", Rev868C3WorkbookSource, StringComparison.Ordinal);
    }

    [Fact]
    public void RelievedStatusNormalizationMatchesAcceptedRev868C3Source()
    {
        Assert.Contains("@('left / resigned','left/resigned','resigned','inactive')", Source, StringComparison.Ordinal);
        Assert.Contains("lower(\"Status\") as normalized_status", Source, StringComparison.Ordinal);
        Assert.Contains("lower(\"Status\") in ('left / resigned','left/resigned','resigned','inactive')", Rev868C3VerifierSource, StringComparison.Ordinal);
        Assert.True(RelievedSetPass(ExpectedRelievedCodes.Select((code, index) =>
            new EmployeeStatusRow(code, new[] { "Left / Resigned", "LEFT/RESIGNED", "Resigned", "Inactive" }[index % 4]))));
    }

    [Fact]
    public void RelievedMissingUnexpectedDuplicateActiveAndStatusMismatchFailClosed()
    {
        var canonical = AcceptedRelievedRows();
        Assert.False(RelievedSetPass(canonical.Skip(1)));
        Assert.False(RelievedSetPass(canonical.Append(new EmployeeStatusRow("SESS-999", "Left / Resigned"))));
        Assert.False(RelievedSetPass(canonical.Append(new EmployeeStatusRow("SESS-016", "Left / Resigned"))));
        var active = AcceptedRelievedRows();
        active[0] = active[0] with { Status = "Active" };
        Assert.False(RelievedSetPass(active));
        var mismatched = AcceptedRelievedRows();
        mismatched[1] = mismatched[1] with { Status = "Terminated" };
        Assert.False(RelievedSetPass(mismatched));
    }

    [Fact]
    public void PreflightAndPostVerificationRequireExactRelievedSetPass()
    {
        var preflight = FunctionBlock("function Get-PreflightSql", "function Get-PostMigrationSql");
        var post = FunctionBlock("function Get-PostMigrationSql", "function Get-TransactionalVerificationSql");
        foreach (var sql in new[] { preflight, post })
        {
            Assert.Contains("relieved_employee_expected_count", sql, StringComparison.Ordinal);
            Assert.Contains("relieved_employee_actual_matched_count", sql, StringComparison.Ordinal);
            Assert.Contains("relieved_employee_missing_count", sql, StringComparison.Ordinal);
            Assert.Contains("relieved_employee_unexpected_count", sql, StringComparison.Ordinal);
            Assert.Contains("relieved_employee_duplicate_count", sql, StringComparison.Ordinal);
            Assert.Contains("relieved_employee_status_mismatch_count", sql, StringComparison.Ordinal);
            Assert.Contains("relieved_employee_acceptance_state", sql, StringComparison.Ordinal);
        }
        Assert.Contains("safe_retry_state='||case", preflight, StringComparison.Ordinal);
        Assert.Contains("and relieved_employee_acceptance_state='PASS'", preflight, StringComparison.Ordinal);
        Assert.Contains("database_schema_acceptance_state='||case", post, StringComparison.Ordinal);
        Assert.Contains("and relieved_employee_acceptance_state='PASS'", post, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(Source, "Assert-Evidence \\$(?:preflight|post)Evidence \\\"relieved_employee_acceptance_state=PASS\\\"").Count);
    }

    [Fact]
    public void UomDecisionAndExpectedSetsContainExactlyOneApprovedEntryEach()
    {
        Assert.Contains("ApprovalStatus = \"APPROVED\"", Source, StringComparison.Ordinal);
        Assert.Contains("UomClassifications = @([pscustomobject]@{", Source, StringComparison.Ordinal);
        Assert.Contains("ItemBaseUomMappings = @([pscustomobject]@{", Source, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(Source, "LifecycleAction = \"CREATE\"").Cast<Match>());
        var contract = Source[Source.IndexOf("$approvedUomMappingContract", StringComparison.Ordinal)..Source.IndexOf("$uomManagementDecisionState", StringComparison.Ordinal)];
        Assert.Single(Regex.Matches(contract, "ItemId = \"8c428e59-db05-471d-a7e7-4f7dc1c13b54\"").Cast<Match>());
        Assert.True(UomAcceptanceCanPass("APPROVED", classifications: 1, mappings: 1));
        Assert.False(UomAcceptanceCanPass("APPROVED", classifications: 2, mappings: 1));
        Assert.False(UomAcceptanceCanPass("APPROVED", classifications: 1, mappings: 2));
    }

    [Fact]
    public void NoKgMDefaultOrInferredUomMappingWasIntroduced()
    {
        var contract = Source[Source.IndexOf("$approvedUomMappingContract", StringComparison.Ordinal)..Source.IndexOf("$relievedEmployeeCodes", StringComparison.Ordinal)];
        Assert.DoesNotContain("KG", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UomCode = \"M\"", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INFERRED", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DEFAULT", contract, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MappingBasis = \"MANAGEMENT_APPROVED\"", contract, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"(?i)(legacy|candidate).*(insert|update).*BaseUom"), Source);
    }

    [Fact]
    public void AnyEaIdentityCodeOrNameCollisionFailsTheCreationPlan()
    {
        Assert.True(UomCreationPlanReady(1, 1, 0, 0, 0, 0, 0));
        Assert.False(UomCreationPlanReady(1, 1, 1, 0, 0, 0, 0));
        Assert.False(UomCreationPlanReady(1, 1, 0, 1, 0, 0, 0));
        Assert.False(UomCreationPlanReady(1, 1, 0, 0, 1, 0, 0));
    }

    [Fact]
    public void EvidenceMismatchReportsLabelExpectedAndActual()
    {
        Assert.Contains("Required evidence mismatch:`nlabel=$label`nexpected=$expected`nactual=$actual", Source, StringComparison.Ordinal);
        Assert.Contains("Assert-Evidence $preflightEvidence \"data_readiness_state=PASS\"", Source, StringComparison.Ordinal);
    }
    [Fact]
    public void PostVerificationRequiresExactEaAuditAndTotalOwnedRows()
    {
        var post = FunctionBlock("function Get-PostMigrationSql", "function Get-TransactionalVerificationSql");
        Assert.Contains("from nexa.uoms where \"CreatedBy\"='migration-rev869a') as migration_created_uom_count", post, StringComparison.Ordinal);
        Assert.Contains("from nexa.controlled_configuration_histories where \"CreatedBy\"='migration-rev869a') as migration_created_uom_history_count", post, StringComparison.Ordinal);
        foreach (var field in new[] { "UomId", "UomCode", "Name", "MeasurementDimension", "QuantityPrecision", "IsCanonicalBase", "ConversionPolicy", "LifecycleAction", "ApprovalStatus", "ManagementApprovalReference", "ItemId", "ItemCode", "MappingStatus", "MappingBasis" })
            Assert.Contains("\"AfterJson\"->>'" + field + "'", post, StringComparison.Ordinal);
        foreach (var line in new[] { "security_configuration_owned_seed_count=88", "migration_created_uom_count=1", "migration_updated_item_count=1", "migration_created_uom_history_count=1", "total_inserted_migration_owned_row_count=90" })
            Assert.Contains("Assert-Evidence $postEvidence \"" + line + "\"", Source, StringComparison.Ordinal);
    }
    [Fact]
    public void PostConstraintContractCastsPostgreSqlInternalCharToText()
    {
        var post = FunctionBlock("function Get-PostMigrationSql", "function Get-TransactionalVerificationSql");
        Assert.Contains("'|type='||c.contype::text||'|definition='", post, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"\|\|\s*c\.contype\s*\|\|", RegexOptions.IgnoreCase), post);
        foreach (var internalCharField in new[] { "contype", "confdeltype", "confupdtype", "confmatchtype", "relkind", "attidentity", "attgenerated", "prokind", "typtype", "typcategory", "tgenabled", "ev_type" })
            Assert.DoesNotMatch(new Regex(@"\|\|\s*(?:[a-z_][a-z0-9_]*\.)?" + internalCharField + @"\s*(?!::text)\|\|", RegexOptions.IgnoreCase), Source);
    }

    [Fact]
    public void PostMigrationVerificationSqlRemainsStrictlyReadOnly()
    {
        var post = FunctionBlock("function Get-PostMigrationSql", "function Get-TransactionalVerificationSql");
        Assert.True(IsSelectOnly(post));
        Assert.DoesNotMatch(new Regex(@"(?i)\b(pg_dump|createdb|pg_restore|drop\s+database|create\s+database|migrations?\s+(?:remove|update)|database\s+update|repair)\b"), post);
        Assert.Contains("Assert-SelectOnlySql \"Post-migration verification\"", Source, StringComparison.Ordinal);
    }
    [Fact]
    public void LegacyBroadAllowListProducedExactlyThirtyTwoFalseMismatches()
    {
        var canonicalReusedRoleCodes = new[] { "PURCHASE_EXECUTIVE", "STORES_EXECUTIVE", "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR" };
        Assert.All(canonicalReusedRoleCodes, code => Assert.Contains(code, Rev866SeedData.AdditionalEmployeeRoles.Select(x => x.Code)));
        Assert.Equal(32, canonicalReusedRoleCodes.Length * Rev869ASeedData.Pages.Length);
        Assert.DoesNotContain("r.\"Code\" in ('PURCHASE_MANAGER','PURCHASE_EXECUTIVE'", FunctionBlock("function Get-PostMigrationSql", "function Get-TransactionalVerificationSql"), StringComparison.Ordinal);
    }
    [Fact]
    public void SeedVerifierUsesExactDeterministicSeventyFourPermissionContracts()
    {
        var post = FunctionBlock("function Get-PostMigrationSql", "function Get-TransactionalVerificationSql");
        var actual = ParsePermissionContracts(post);
        var expected = SourceDefinedPermissionContracts();
        Assert.Equal(74, actual.Count);
        Assert.Equal(74, actual.Select(x => x.Id).Distinct().Count());
        Assert.Equal(expected.OrderBy(x => x.Id), actual.OrderBy(x => x.Id));
        Assert.Contains("min(r.\"Id\"::text)::uuid", post, StringComparison.Ordinal);
        Assert.Contains("p.\"RoleId\" is distinct from e.\"RoleId\"", post, StringComparison.Ordinal);
        Assert.Contains("p.\"PageDefinitionId\" is distinct from e.\"PageDefinitionId\"", post, StringComparison.Ordinal);
        Assert.DoesNotContain("r.\"Code\" in ('PURCHASE_MANAGER','PURCHASE_EXECUTIVE'", post, StringComparison.Ordinal);
    }

    [Fact]
    public void SeedMismatchIsTheSumOfEveryIndependentCanonicalMetric()
    {
        var post = FunctionBlock("function Get-PostMigrationSql", "function Get-TransactionalVerificationSql");
        foreach (var label in SeedMismatchLabels)
        {
            Assert.Single(Regex.Matches(post, "union all select '" + Regex.Escape(label) + "=").Cast<Match>());
            Assert.Single(Regex.Matches(Source, "Assert-Evidence \\$postEvidence \\\"" + Regex.Escape(label) + "=0\\\"").Cast<Match>());
        }
        Assert.Contains("role_seed_unexpected_count+role_seed_missing_count+page_seed_unexpected_count+page_seed_missing_count+policy_seed_unexpected_count+policy_seed_missing_count+permission_seed_unexpected_count+permission_seed_missing_count+permission_flag_mismatch_count+permission_role_mapping_mismatch_count+permission_page_mapping_mismatch_count+duplicate_role_page_permission_count", post, StringComparison.Ordinal);
        Assert.Contains("seed_set_mismatch_count=0", post, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("wrong-role")]
    [InlineData("wrong-page")]
    [InlineData("wrong-flag")]
    [InlineData("missing")]
    [InlineData("unexpected")]
    [InlineData("duplicate")]
    [InlineData("wrong-created-by")]
    public void AnyPermissionSeedDefectFailsTheExactSeedSetAndDatabaseAcceptance(string defect)
    {
        var expected = MaterializePermissions(ParsePermissionContracts(FunctionBlock("function Get-PostMigrationSql", "function Get-TransactionalVerificationSql")));
        var actual = expected.Select(Clone).ToList();
        switch (defect)
        {
            case "wrong-role": actual[0] = actual[0] with { RoleId = Guid.NewGuid() }; break;
            case "wrong-page": actual[0] = actual[0] with { PageId = Guid.NewGuid() }; break;
            case "wrong-flag": actual[0].Flags[0] = !actual[0].Flags[0]; break;
            case "missing": actual.RemoveAt(0); break;
            case "unexpected": actual.Add(new SeedPermission(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new bool[20], "migration-rev869a")); break;
            case "duplicate": actual.Add(actual[0] with { Id = Guid.NewGuid(), Flags = actual[0].Flags.ToArray() }); break;
            case "wrong-created-by": actual[0] = actual[0] with { CreatedBy = "wrong-owner" }; break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }
        var mismatch = PermissionSeedMismatch(expected, actual);
        Assert.True(mismatch > 0);
        Assert.False(DatabaseSeedAcceptance(mismatch));
    }

    [Fact]
    public void WrongPolicyValueFailsTheExactSeedSetAndDatabaseAcceptance()
    {
        var expected = new[]
        {
            new SeedPolicy(Guid.Parse("50000000-0000-0000-0000-000000000001"), "SESS", "VENDOR_FINAL_APPROVER", "MANAGING_DIRECTOR", "migration-rev869a"),
            new SeedPolicy(Guid.Parse("50000000-0000-0000-0000-000000000002"), "SESS", "INVENTORY_VALUATION_METHOD", "WEIGHTED_AVERAGE", "migration-rev869a")
        };
        var actual = expected.Select(x => x with { }).ToArray();
        actual[0] = actual[0] with { Value = "TECHNICAL_DIRECTOR" };
        var mismatch = PolicySeedMismatch(expected, actual);
        Assert.True(mismatch > 0);
        Assert.False(DatabaseSeedAcceptance(mismatch));
    }
    [Fact]
    public void ArtifactPredicateAndExistingSafetyBoundariesRemainFailClosed()
    {
        Assert.Contains("pg_indexes where schemaname='nexa' and (indexname like '%rev869a%' or indexname in (", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("schemaname='nexa' and indexname like '%rev869a%' or schemaname='nexa'", Source, StringComparison.Ordinal);
        Assert.Contains("$Database -cne $targetDatabase", Source, StringComparison.Ordinal);
        Assert.Contains("rev861|production|prod|live|main", Source, StringComparison.Ordinal);
        Assert.Contains("Full apply requires approved pre-REV869A backup path and SHA-256 evidence", Source, StringComparison.Ordinal);
        Assert.Contains("Assert-SelectOnlySql \"Preflight\"", Source, StringComparison.Ordinal);
        Assert.Contains("Assert-SelectOnlySql \"Post-migration verification\"", Source, StringComparison.Ordinal);
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
    public void RollbackEvidenceIsExplicitAndOwnedRowCountsAreSeparated()
    {
        Assert.Contains("Down deletes exactly 88 security/configuration rows", Source);
        Assert.Contains("90 inserted migration-owned rows", Source);
        Assert.Contains("exactly 1 Item row is updated", Source);
        Assert.Contains("preserves REV868/REV868C3 business/history rows", Source);
        Assert.Contains("drops rev869a_vendors_prechange_backup, rev869a_uoms_prechange_backup, and rev869a_items_prechange_backup last", Source);
        Assert.Contains("Backup comparisons exclude only that approved mapping", Source);
    }

    private static readonly string[] SeedMismatchLabels =
    {
        "role_seed_unexpected_count", "role_seed_missing_count", "page_seed_unexpected_count", "page_seed_missing_count",
        "policy_seed_unexpected_count", "policy_seed_missing_count", "permission_seed_unexpected_count", "permission_seed_missing_count",
        "permission_flag_mismatch_count", "permission_role_mapping_mismatch_count", "permission_page_mapping_mismatch_count",
        "duplicate_role_page_permission_count"
    };

    private sealed record PermissionSpec(Guid Id, string RoleCode, Guid PageId, string FlagBits);
    private sealed record SeedPermission(Guid Id, Guid RoleId, Guid PageId, bool[] Flags, string CreatedBy);
    private sealed record SeedPolicy(Guid Id, string Organization, string Code, string Value, string CreatedBy);

    private static IReadOnlyList<PermissionSpec> ParsePermissionContracts(string post)
    {
        var start = post.IndexOf("expected_permission_specs", StringComparison.Ordinal);
        var end = post.IndexOf("), expected_permission_seeds as (", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var pattern = new Regex(@"\('(?<id>[0-9a-f-]{36})'::uuid,'(?<role>[A-Z_]+)','(?<page>[0-9a-f-]{36})'::uuid,(?<flags>(?:true|false)(?:,(?:true|false)){19})\)", RegexOptions.IgnoreCase);
        return pattern.Matches(post[start..end]).Select(m => new PermissionSpec(
            Guid.Parse(m.Groups["id"].Value), m.Groups["role"].Value.ToUpperInvariant(), Guid.Parse(m.Groups["page"].Value),
            string.Concat(m.Groups["flags"].Value.Split(',').Select(x => x == "true" ? '1' : '0')))).ToArray();
    }

    private static IReadOnlyList<PermissionSpec> SourceDefinedPermissionContracts()
    {
        var roles = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Concat(Rev869ASeedData.Roles).ToDictionary(x => x.Id);
        var expected = Rev869ASeedData.RolePagePermissions
            .Where(p => p.CreatedBy == "migration-rev869a")
            .Select(p => new PermissionSpec(
            p.Id, roles[p.RoleId].Code.Equals("accounts_head", StringComparison.OrdinalIgnoreCase) ? "ACCOUNTS_HEAD" : Rev869ARoleCodes.Normalize(roles[p.RoleId].Code),
            p.PageDefinitionId, FlagBits(p))).ToList();
        var departmentManagerIds = new[]
        {
            "aea2e8a1-18a6-72d2-a954-6f5513b80eeb", "f8e7d0a6-f056-175a-e604-14c1f9f6ad83",
            "a98dbcec-f959-9f7c-c5f7-3c3a2c8bec12", "15ee5b19-d532-c28c-b755-de4152769a7a",
            "5794f740-90b1-5a70-413a-d59bbc97ce78", "42e2a253-d767-6191-caf9-e1f79652c44f",
            "38371df3-5a46-5137-8204-4c5391633180", "680f7358-4b7c-0733-be42-f9d52e746d1b"
        };
        const string departmentManagerFlags = "10000000000011000010";
        expected.AddRange(Rev869ASeedData.Pages.Select((page, index) => new PermissionSpec(Guid.Parse(departmentManagerIds[index]), "DEPARTMENT_MANAGER", page.Id, departmentManagerFlags)));
        return expected;
    }

    private static string FlagBits(RolePagePermission p) => string.Concat(new[]
    {
        p.CanView,p.CanCreate,p.CanUpdate,p.CanSubmit,p.CanVerify,p.CanApprove,p.CanReject,p.CanRequestClarification,p.CanRequestRevision,p.CanResubmit,
        p.CanCancel,p.CanDeactivate,p.CanPrint,p.CanDownload,p.CanExport,p.CanUploadAttachment,p.CanReplaceAttachment,p.CanViewCommercialValues,p.CanViewAuditHistory,p.HasFullControl
    }.Select(x => x ? '1' : '0'));

    private static IReadOnlyList<SeedPermission> MaterializePermissions(IReadOnlyList<PermissionSpec> specs)
    {
        var roleIds = specs.Select(x => x.RoleCode).Distinct().ToDictionary(x => x, x => DeterministicGuid("role-contract", x));
        return specs.Select(x => new SeedPermission(x.Id, roleIds[x.RoleCode], x.PageId, x.FlagBits.Select(c => c == '1').ToArray(), "migration-rev869a")).ToArray();
    }

    private static SeedPermission Clone(SeedPermission value) => value with { Flags = value.Flags.ToArray() };

    private static int PermissionSeedMismatch(IReadOnlyList<SeedPermission> expected, IReadOnlyList<SeedPermission> actual)
    {
        var unexpected = actual.Count(a => a.CreatedBy == "migration-rev869a" && expected.All(e => e.Id != a.Id));
        var missing = expected.Count(e => actual.Count(a => a.Id == e.Id && a.CreatedBy == "migration-rev869a") != 1);
        var flags = expected.Sum(e => actual.Where(a => a.Id == e.Id).Count(a => !a.Flags.SequenceEqual(e.Flags)));
        var roles = expected.Sum(e => actual.Where(a => a.Id == e.Id).Count(a => a.RoleId != e.RoleId));
        var pages = expected.Sum(e => actual.Where(a => a.Id == e.Id).Count(a => a.PageId != e.PageId));
        var duplicates = actual.Where(a => a.CreatedBy == "migration-rev869a").GroupBy(a => (a.RoleId, a.PageId)).Count(g => g.Count() != 1);
        return unexpected + missing + flags + roles + pages + duplicates;
    }

    private static int PolicySeedMismatch(IReadOnlyList<SeedPolicy> expected, IReadOnlyList<SeedPolicy> actual)
    {
        var unexpected = actual.Count(a => a.CreatedBy == "migration-rev869a" && !expected.Contains(a));
        var missing = expected.Count(e => actual.Count(a => a == e) != 1);
        return unexpected + missing;
    }

    private static bool DatabaseSeedAcceptance(int seedSetMismatchCount) => seedSetMismatchCount == 0;

    private static Guid DeterministicGuid(params string[] values)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join('|', values)));
        return new Guid(hash[..16]);
    }
    private sealed record EmployeeStatusRow(string Code, string Status);

    private static EmployeeStatusRow[] AcceptedRelievedRows() =>
        ExpectedRelievedCodes.Select(code => new EmployeeStatusRow(code, "Left / Resigned")).ToArray();

    private static bool RelievedSetPass(IEnumerable<EmployeeStatusRow> sourceRows)
    {
        var rows = sourceRows.Select(x => x with { Status = x.Status.ToLowerInvariant() }).ToArray();
        var expected = ExpectedRelievedCodes.ToHashSet(StringComparer.Ordinal);
        var matched = rows.Count(x => expected.Contains(x.Code) && AcceptedRelievedStatuses.Contains(x.Status));
        var missing = expected.Count(code => rows.All(x => x.Code != code));
        var unexpected = rows.Count(x => !expected.Contains(x.Code) && AcceptedRelievedStatuses.Contains(x.Status));
        var duplicates = rows.Where(x => expected.Contains(x.Code)).GroupBy(x => x.Code, StringComparer.Ordinal).Count(x => x.Count() != 1);
        var statusMismatch = expected.Count(code => rows.Any(x => x.Code == code) && !rows.Any(x => x.Code == code && AcceptedRelievedStatuses.Contains(x.Status)));
        return expected.Count == 9 && matched == 9 && missing == 0 && unexpected == 0 && duplicates == 0 && statusMismatch == 0;
    }

    private static bool UomAcceptanceCanPass(string decision, int classifications, int mappings) =>
        decision == "APPROVED" && classifications == 1 && mappings == 1;

    private static bool ApprovedPlanReady(int rawNullItems, int matched, int missing, int unexpected, int duplicates, int invalid, int mappingCount) =>
        rawNullItems == matched && mappingCount == 1 && matched == 1 && missing == 0 && unexpected == 0 && duplicates == 0 && invalid == 0;

    private static bool UomCreationPlanReady(int planCount, int newCount, int idCollisions, int codeCollisions, int nameCollisions, int duplicates, int unapproved) =>
        planCount == 1 && newCount == 1 && idCollisions == 0 && codeCollisions == 0 && nameCollisions == 0 && duplicates == 0 && unapproved == 0;

    private static string FunctionBlock(string startMarker, string endMarker)
    {
        var start = Source.IndexOf(startMarker, StringComparison.Ordinal);
        var end = Source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return Source[start..end];
    }

    private static string NormalizeLineEndings(string source) =>
        source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private sealed record UomContract(Guid Id, string Code, bool Approved);
    private sealed record UomSetResult(int Missing, int Unexpected, int Duplicate, int Unapproved);

    private static UomSetResult CompareUomSet(IReadOnlyCollection<UomContract> expected, IReadOnlyCollection<(Guid Id, string Code)> actual)
    {
        var missing = actual.Count(a => !expected.Any(e => e.Id == a.Id && string.Equals(e.Code, a.Code, StringComparison.OrdinalIgnoreCase)));
        var unexpected = expected.Count(e => !actual.Any(a => a.Id == e.Id && string.Equals(a.Code, e.Code, StringComparison.OrdinalIgnoreCase)));
        var duplicate = expected.GroupBy(e => e.Id).Count(g => g.Count() != 1) +
                        expected.GroupBy(e => e.Code.Trim(), StringComparer.OrdinalIgnoreCase).Count(g => g.Count() != 1);
        return new(missing, unexpected, duplicate, expected.Count(e => !e.Approved));
    }

    private static IEnumerable<string> PhysicalRelations(string sqlBlock) =>
        Regex.Matches(sqlBlock, @"(?i)\bnexa\.([a-z_][a-z0-9_]*)")
            .Select(match => match.Groups[1].Value.ToLowerInvariant());

    private static bool HasExactPhysicalRelations(IEnumerable<string> actual) =>
        CanonicalHelperRelations.ToHashSet(StringComparer.Ordinal).SetEquals(actual);

    private static bool DepartmentManagerReusable(int count, int active, int suitable) =>
        count == 1 && active == 1 && suitable == 1;

    private static bool ItemUomReady(Guid? uomId, bool uomExists, bool baseMappingPresent, string mappingBasis) =>
        uomId.HasValue && uomExists && baseMappingPresent && mappingBasis == "MANAGEMENT_APPROVED";

    private static bool PreservationMatches(IReadOnlyDictionary<string, long> before, IReadOnlyDictionary<string, long> after) =>
        before.Count == after.Count && before.All(pair => after.TryGetValue(pair.Key, out var value) && value == pair.Value);

    private static bool HasExactlyOneEvidence(string evidence, string key, string expected)
    {
        var matches = Regex.Matches(evidence, "^" + Regex.Escape(key) + "=(.*)$", RegexOptions.Multiline);
        return matches.Count == 1 && matches[0].Groups[1].Value == expected;
    }

    private static bool HasExactlyOneNumericEvidence(string evidence, string key) =>
        Regex.Matches(evidence, "^" + Regex.Escape(key) + "=(\\d+)$", RegexOptions.Multiline).Count == 1 &&
        Regex.Matches(evidence, "^" + Regex.Escape(key) + "=", RegexOptions.Multiline).Count == 1;

    private static Dictionary<string, int> TransactionalPrerequisiteCounts() => new(StringComparer.Ordinal)
    {
        ["active_employee_expected"] = 42,
        ["active_employee_actual_matched"] = 42,
        ["active_employee_missing"] = 0,
        ["active_employee_unexpected"] = 0,
        ["active_employee_duplicate"] = 0,
        ["active_employee_status_mismatch"] = 0,
        ["identity_collision"] = 0,
        ["uom_collision"] = 0,
        ["tax_collision"] = 0,
        ["qc_collision"] = 0,
        ["vendor_collision"] = 0,
        ["warehouse_collision"] = 0,
        ["history_collision"] = 0
    };

    private static bool TransactionalPrerequisitesPass(IReadOnlyDictionary<string, int> counts) =>
        counts.Count == 13 && counts["active_employee_expected"] == 42 && counts["active_employee_actual_matched"] == 42 &&
        counts["active_employee_missing"] == 0 && counts["active_employee_unexpected"] == 0 &&
        counts["active_employee_duplicate"] == 0 && counts["active_employee_status_mismatch"] == 0 &&
        counts.Where(x => !x.Key.StartsWith("active_employee_", StringComparison.Ordinal)).All(x => x.Value == 0);

    private static string LegacyRegexTransactionalSqlExtractor()
    {
        var block = FunctionBlock("function Get-TransactionalVerificationSql", "function Invoke-Psql");
        var match = Regex.Match(block, "return @'\\\\r?\\\\n(?<sql>[\\\\s\\\\S]*?)\\\\r?\\\\n'@\\\\.Trim\\\\(\\\\)");
        Assert.True(match.Success);
        return match.Groups["sql"].Value;
    }

    private static string PlatformNewlineTransactionalSqlExtractor()
    {
        var block = FunctionBlock("function Get-TransactionalVerificationSql", "function Invoke-Psql");
        var newline = Environment.NewLine;
        var startMarker = "return @'" + newline;
        var endMarker = newline + "'@.Trim()";
        var start = block.IndexOf(startMarker, StringComparison.Ordinal);
        var end = start < 0 ? -1 : block.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return block[(start + startMarker.Length)..end];
    }

    private static string RenderedTransactionalSql()
    {
        var block = FunctionBlock("function Get-TransactionalVerificationSql", "function Invoke-Psql");
        var header = block.IndexOf("return @'", StringComparison.Ordinal);
        var start = header < 0 ? -1 : block.IndexOf((char)10, header) + 1;
        var end = start <= 0 ? -1 : block.IndexOf("'@.Trim()", start, StringComparison.Ordinal);
        Assert.True(header >= 0 && start > header && end > start);
        return block[start..end].TrimEnd((char)13, (char)10);
    }

    private static bool CanRunPostgresTests(bool transactionalAcceptance) => transactionalAcceptance;

    private static bool OverallAcceptance(bool schema, bool preservation, bool tests) => schema && preservation && tests;

    private static bool IsPermittedDatabase(string database) =>
        database == "sess_nexaerp_rev869a_verify" && !Regex.IsMatch(database, "rev861|production|prod|live|main", RegexOptions.IgnoreCase);

    private static bool IsSelectOnly(string sql)
    {
        var withoutLiterals = Regex.Replace(sql, @"'(?:[^']|'')*'", "''");
        return !Regex.IsMatch(withoutLiterals, "\\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|vacuum|analyze|refresh)\\b", RegexOptions.IgnoreCase);
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

public sealed class Rev869APostgresAcceptanceTests
{
    private const string ExpectedDatabase = "sess_nexaerp_rev869a_verify";
    private const string TargetMigration = "20260810120000_Rev869AIdentityMasterScopeFoundation";

    [Fact]
    public async Task Rev869A_applied_migration_and_preservation_contract_are_exact()
    {
        var raw = Environment.GetEnvironmentVariable("REV869A_POSTGRES");
        if (string.IsNullOrWhiteSpace(raw)) return;
        var builder = new NpgsqlConnectionStringBuilder(raw);
        Assert.Equal(ExpectedDatabase, builder.Database);
        Assert.False(builder.IncludeErrorDetail);

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using (var readOnly = new NpgsqlCommand("set transaction read only", connection, transaction))
            await readOnly.ExecuteNonQueryAsync();

        Assert.Equal(12, await ScalarAsync(connection, transaction, "select count(*) from public.\"__EFMigrationsHistory\""));
        Assert.Equal(1, await ScalarAsync(connection, transaction, $"select count(*) from public.\"__EFMigrationsHistory\" where \"MigrationId\"='{TargetMigration}'"));
        Assert.Equal(7, await ScalarAsync(connection, transaction, "select count(*) from nexa.purchase_requisitions"));
        Assert.Equal(3, await ScalarAsync(connection, transaction, "select count(*) from nexa.purchase_requisition_approval_history"));
        Assert.Equal(4, await ScalarAsync(connection, transaction, "select count(*) from nexa.stock_reservations"));
        Assert.Equal(42, await ScalarAsync(connection, transaction, "select count(*) from nexa.employees where \"EmployeeCode\" like 'SESS-%' and lower(trim(\"Status\"))='active'"));
        Assert.Equal(16, await ScalarAsync(connection, transaction, "select count(*) from nexa.departments"));
        Assert.Equal(14, await ScalarAsync(connection, transaction, "select count(*) from nexa.department_approval_mappings where \"IsActive\"=true"));
        await transaction.RollbackAsync();
    }

    private static async Task<long> ScalarAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
