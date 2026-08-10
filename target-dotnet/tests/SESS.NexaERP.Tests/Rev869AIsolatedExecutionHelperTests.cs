using System.Text.RegularExpressions;

namespace SESS.NexaERP.Tests;

public sealed class Rev869AIsolatedExecutionHelperTests
{
    private static readonly string Root = FindRoot();
    private static readonly string HelperPath = Path.Combine(Root, "tools", "apply-rev869a-isolated-foundation-secure.ps1");
    private static readonly string Source = File.ReadAllText(HelperPath);
    private static readonly string MigrationPath = Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260810120000_Rev869AIdentityMasterScopeFoundation.cs");
    private static readonly string MigrationSource = File.ReadAllText(MigrationPath);
    private static readonly string DbContextSource = File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.cs")) + Environment.NewLine +
        File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.Rev869A.cs"));
    private static readonly string ModelSnapshotSource = File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "NexaErpDbContextModelSnapshot.cs"));
    private static readonly string ProvisioningHelperSource = File.ReadAllText(Path.Combine(Root, "tools", "prepare-rev869a-isolated-database-secure.ps1"));
    private static readonly string Rev868C3WorkbookSource = File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Rev868C3EmployeeWorkbookData.cs"));
    private static readonly string Rev868C3VerifierSource = File.ReadAllText(Path.Combine(Root, "tools", "verify-rev868c3-postrun-readonly-secure.ps1"));
    private static readonly string[] ExpectedRelievedCodes =
    {
        "SESS-016", "SESS-018", "SESS-022", "SESS-027", "SESS-028", "SESS-032", "SESS-036", "SESS-037", "SESS-039"
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
            "uom_master_candidate=", "item_reference_count", "proposed_base_uom_id", "uom_ambiguity=CODE", "uom_ambiguity=NAME", "PENDING_MANAGEMENT_APPROVAL"
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
    public void GeneratedSqlContainsNoUnsupportedUuidMinOrMaxAggregate()
    {
        Assert.DoesNotMatch(new Regex(@"(?is)\b(?:min|max)\s*\(\s*""[^""]*(?:Id|ID)""\s*\)"), Source);
        Assert.DoesNotMatch(new Regex(@"(?is)\b(?:min|max)\s*\(\s*""UomId""\s*::\s*text"), Source);
    }

    [Fact]
    public void DuplicateUomIdAndNormalizedCodeGroupsAreCountedSeparatelyAndAdded()
    {
        Assert.Contains("select \"UomId\"\n          from expected_uom_classifications\n          group by \"UomId\"", Source.Replace("\r\n", "\n"), StringComparison.Ordinal);
        Assert.Contains("select upper(trim(\"UomCode\")) as normalized_uom_code", Source, StringComparison.Ordinal);
        Assert.Contains("group by upper(trim(\"UomCode\"))", Source, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"(?s)\) duplicate_uom_ids\)\s*\+\s*\(select count\(\*\) from \(.*?\) duplicate_uom_codes\)\s*\) as duplicate_uom_classification_count"), Source);
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
            Assert.Contains($"ToTable(\"{relation}\", \"nexa\"", ModelSnapshotSource, StringComparison.Ordinal);
        }
        foreach (var backup in actual.Where(x => x.StartsWith("rev869a_", StringComparison.Ordinal)))
            Assert.Contains(backup, MigrationSource, StringComparison.Ordinal);

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
            Assert.Contains($"ToTable(\"{relation}\", \"nexa\"", ModelSnapshotSource, StringComparison.Ordinal);
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
        Assert.Contains("greatest(0,74-count(distinct (r.\"Code\",d.\"PageKey\")))", post, StringComparison.Ordinal);
        Assert.Contains("'DEPARTMENT_MANAGER','TECHNICAL_DIRECTOR'", post, StringComparison.Ordinal);
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
    public void AllUomMastersAndSafeUnmappedItemFieldsAreReportedWithoutApproval()
    {
        var preflight = FunctionBlock("function Get-PreflightSql", "function Get-PostMigrationSql");
        foreach (var evidence in new[] { "uom_master_count", "uom_master_candidate=", "referenced_uom_count", "unreferenced_uom_count", "null_item_uom_count", "invalid_item_uom_count", "uom_creation_management_decision_required=" })
            Assert.Contains(evidence, preflight, StringComparison.Ordinal);
        foreach (var field in new[] { "item_code=", "item_name=", "material_type=", "current_uom_id=", "current_uom_code=", "current_uom_name=", "item_status=", "proposed_base_uom=NOT_APPROVED" })
            Assert.Contains(field, preflight, StringComparison.Ordinal);
        Assert.Contains("from nexa.uoms u left join nexa.items", preflight, StringComparison.Ordinal);
        Assert.Contains("uom_master_count>0", preflight, StringComparison.Ordinal);
        Assert.Contains("ApprovalStatus = \"PENDING\"", Source, StringComparison.Ordinal);
        Assert.Contains("UomClassifications = @()", Source, StringComparison.Ordinal);
        Assert.Contains("ItemBaseUomMappings = @()", Source, StringComparison.Ordinal);
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
        Assert.Contains("duplicate identity did not fail closed", Source);
        Assert.Contains("cross-warehouse RackBin was accepted", Source);
        Assert.Contains("configuration history update was accepted", Source);
        Assert.Contains("invalid vendor qualification dates were accepted", Source);
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
    public void UomDecisionAndExpectedSetsRemainPendingAndEmpty()
    {
        Assert.Contains("ApprovalStatus = \"PENDING\"", Source, StringComparison.Ordinal);
        Assert.Contains("UomClassifications = @()", Source, StringComparison.Ordinal);
        Assert.Contains("ItemBaseUomMappings = @()", Source, StringComparison.Ordinal);
        Assert.Contains("select null::uuid,null::text,null::text,null::integer,null::boolean,null::text,null::text,null::text where false", Source, StringComparison.Ordinal);
        Assert.Contains("select null::uuid,null::uuid,null::text,null::text,null::text where false", Source, StringComparison.Ordinal);
        Assert.Contains("'$uomManagementDecisionState'='APPROVED'", Source, StringComparison.Ordinal);
        Assert.False(UomAcceptanceCanPass("PENDING", classifications: 0, mappings: 0));
    }

    [Fact]
    public void NoGuessedDefaultOrInferredUomMappingWasIntroduced()
    {
        Assert.Contains("no guessed, default, inferred, or automatic UOM/BaseUom mapping is permitted", Source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PENDING_MANAGEMENT_APPROVAL", Source, StringComparison.Ordinal);
        Assert.Contains("proposed_base_uom_id='||coalesce(e.\"BaseUomId\"::text,'NOT_APPROVED')", Source, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"(?i)(legacy|candidate).*(insert|update).*BaseUom"), Source);
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
        Assert.Contains("Down removes exactly 88 migration-owned seeds", Source);
        Assert.Contains("preserves REV868/REV868C3 business/history rows", Source);
        Assert.Contains("drops rev869a_vendors_prechange_backup, rev869a_uoms_prechange_backup, and rev869a_items_prechange_backup last", Source);
        Assert.Contains("backup/current legacy-column comparisons must be zero", Source);
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
        decision == "APPROVED" && classifications > 0 && mappings > 0;

    private static string FunctionBlock(string startMarker, string endMarker)
    {
        var start = Source.IndexOf(startMarker, StringComparison.Ordinal);
        var end = Source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return Source[start..end];
    }
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
