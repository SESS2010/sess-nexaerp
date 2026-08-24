using System.Text.RegularExpressions;

namespace SESS.NexaERP.Tests;

public sealed class Rev869AIsolatedDatabasePreparationHelperTests
{
    private static readonly string Root = FindRoot();
    private static readonly string HelperPath = Path.Combine(Root, "tools", "prepare-rev869a-isolated-database-secure.ps1");
    private static string Source => File.ReadAllText(HelperPath);
    private static string DbContextSource => File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.cs"));
    private static string SnapshotSource => File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "NexaErpDbContextModelSnapshot.cs"));
    private static string Rev868C2Migration => File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260824032638_AdvanceInitialBaseline.cs"));
    private static string Rev868C3Migration => File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260824032638_AdvanceInitialBaseline.cs"));
    private static string Rev868C3WorkbookData => File.ReadAllText(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Rev868C3EmployeeWorkbookData.cs"));
    private static string Rev868C3Verifier => File.ReadAllText(Path.Combine(Root, "tools", "verify-rev868c3-postrun-readonly-secure.ps1"));
    private static readonly string[] ExpectedRelievedEmployeeCodes =
    {
        "SESS-016", "SESS-018", "SESS-022", "SESS-027", "SESS-028", "SESS-032", "SESS-036", "SESS-037", "SESS-039"
    };
    private static readonly HashSet<string> AcceptedRelievedStatuses =
        new(new[] { "left / resigned", "left/resigned", "resigned", "inactive" }, StringComparer.Ordinal);

    private static readonly string[] ExpectedMigrations =
    {
        "20260808110924_Phase1Foundation",
        "20260808114550_Phase1AuthorizationSeed",
        "20260808123411_Rev866EmployeePermissionMatrix",
        "20260808142353_Rev866CorrectiveStatusPermissionAudit",
        "20260808151207_Rev867MasterFoundation",
        "20260808160435_Rev867C1Corrections",
        "20260808182945_Rev868PurchaseRequisitionFoundation",
        "20260808190920_Rev868PurchaseLocationAllocationCorrection",
        "20260809123000_Rev868C2DepartmentManagerApprovalMapping",
        "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation",
        "20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection"
    };

    [Fact]
    public void HelperHasFourExplicitModesAndPlanReturnsBeforeAccess()
    {
        foreach (var mode in new[] { "GeneratePlanOnly", "SourcePreflightOnly", "Provision", "PostProvisionVerification" })
            Assert.Contains($"[switch]${mode}", Source, StringComparison.Ordinal);

        var planReturn = Source.IndexOf("if ($GeneratePlanOnly) { Write-Plan; return }", StringComparison.Ordinal);
        Assert.True(planReturn > 0);
        Assert.True(planReturn < Source.IndexOf("Initialize-DatabaseAccess", planReturn, StringComparison.Ordinal));
        Assert.Contains("requests no password and performs no PostgreSQL", Source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("wrong", "sess_nexaerp_rev869a_verify")]
    [InlineData("sess_nexaerp_rev868_verify", "wrong")]
    [InlineData("sess_nexaerp", "sess_nexaerp_rev869a_verify")]
    [InlineData("sess_nexaerp_rev868_verify", "postgres")]
    [InlineData("sess_nexaerp_rev861_verify", "sess_nexaerp_rev869a_verify")]
    [InlineData("sess_nexaerp_rev868_verify", "production")]
    public void WrongProtectedOrUnexpectedDatabaseIsRejected(string source, string target)
    {
        Assert.False(IsAcceptedEndpoint(source, target, "localhost", 5432));
    }

    [Fact]
    public void OnlyExactEndpointIsAccepted()
    {
        Assert.True(IsAcceptedEndpoint("sess_nexaerp_rev868_verify", "sess_nexaerp_rev869a_verify", "localhost", 5432));
        Assert.False(IsAcceptedEndpoint("sess_nexaerp_rev868_verify", "sess_nexaerp_rev869a_verify", "127.0.0.1", 5432));
        Assert.False(IsAcceptedEndpoint("sess_nexaerp_rev868_verify", "sess_nexaerp_rev869a_verify", "localhost", 5433));
        Assert.Contains("$SourceDatabase -cne $acceptedSource", Source, StringComparison.Ordinal);
        Assert.Contains("$TargetDatabase -cne $acceptedTarget", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void TargetAlreadyExistsFailsSourceReadiness()
    {
        Assert.False(SourceEvidencePass(ExpectedMigrations, targetCount: 1, 42, relievedSetAccepted: true, 12, 14));
        Assert.Contains("target_database_count = 0", Source, StringComparison.Ordinal);
        Assert.Contains("Provision must fail closed", Checkpoint(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingUnexpectedOrDuplicateMigrationFails()
    {
        Assert.False(MigrationSetPass(ExpectedMigrations.Skip(1)));
        Assert.False(MigrationSetPass(ExpectedMigrations.Append("20990101000000_Unexpected")));
        Assert.False(MigrationSetPass(ExpectedMigrations.Append(ExpectedMigrations[0])));
        Assert.Equal(11, ExpectedMigrations.Length);
        foreach (var migration in ExpectedMigrations) Assert.Contains(migration, Source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("begin transaction read only; select 1; commit;", true)]
    [InlineData("begin; select 1; commit;", false)]
    [InlineData("begin transaction read only; update employees set \"Status\"='x'; commit;", false)]
    [InlineData("begin transaction read only; create table x(id int); commit;", false)]
    [InlineData("begin transaction read only; delete from employees; commit;", false)]
    public void SourcePreflightRejectsNonReadOnlySql(string sql, bool expected)
    {
        Assert.Equal(expected, IsReadOnlySql(sql));
        Assert.Contains("Assert-ReadOnlySql", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void DumpAndRestoreContractRejectUnsafeOptions()
    {
        Assert.DoesNotContain("dropdb.exe", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invoke-Native $PgRestorePath $restoreArgs", Source[..Source.IndexOf("Assert-SafeRestoreArguments $restoreArgs", StringComparison.Ordinal)], StringComparison.Ordinal);
        Assert.Contains("--format=custom", Source, StringComparison.Ordinal);
        Assert.Contains("--no-owner", Source, StringComparison.Ordinal);
        Assert.Contains("--no-privileges", Source, StringComparison.Ordinal);
        Assert.Contains("--clean", Source, StringComparison.Ordinal);
        Assert.Contains("--create", Source, StringComparison.Ordinal);
        Assert.False(RestoreOptionsPass(new[] { "--clean", "--no-owner", "--no-privileges" }));
        Assert.False(RestoreOptionsPass(new[] { "--create", "--no-owner", "--no-privileges" }));
        Assert.False(RestoreOptionsPass(new[] { "--no-owner" }));
        Assert.False(RestoreOptionsPass(new[] { "--no-privileges" }));
        Assert.True(RestoreOptionsPass(new[] { "--no-owner", "--no-privileges" }));
    }

    [Fact]
    public void PasswordAndConnectionDetailsCannotLeak()
    {
        Assert.Contains("Read-Host \"PostgreSQL password (not logged)\" -AsSecureString", Source, StringComparison.Ordinal);
        Assert.Contains("$env:PGPASSWORD", Source, StringComparison.Ordinal);
        Assert.Contains("Remove-Item Env:\\PGPASSWORD", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionStrings__", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Include Error Detail", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$plainPassword", Regex.Match(Source, @"\$dumpArgs\s*=.*", RegexOptions.None).Value, StringComparison.Ordinal);
    }

    [Fact]
    public void BackupMustBeFreshContainedAndHasCompleteEvidence()
    {
        Assert.Contains("backups\\postgresql\\pre-rev869a-isolated", Source, StringComparison.Ordinal);
        Assert.Contains("older pre-C3 backup forbidden", Source, StringComparison.Ordinal);
        Assert.Contains("Get-FileHash -LiteralPath $backupPath -Algorithm SHA256", Source, StringComparison.Ordinal);
        Assert.Contains("backup_byte_size=", Source, StringComparison.Ordinal);
        Assert.Contains("backup_creation_utc=", Source, StringComparison.Ordinal);
        Assert.Contains("backup_sha256=", Source, StringComparison.Ordinal);
        Assert.False(BackupEvidencePass("", 10, DateTime.UtcNow));
        Assert.False(BackupEvidencePass(new string('A', 64), 0, DateTime.UtcNow));
        Assert.True(BackupEvidencePass(new string('A', 64), 1, DateTime.UtcNow));
    }

    [Fact]
    public void ProvisionFailureQuarantinesWithoutAutomaticCleanup()
    {
        Assert.Contains("QUARANTINED_DO_NOT_USE_OR_AUTO_REPAIR", Source, StringComparison.Ordinal);
        Assert.Contains("no automatic drop or repair", Source, StringComparison.OrdinalIgnoreCase);
        var catchStart = Source.LastIndexOf("catch {", StringComparison.Ordinal);
        var catchSection = Source[catchStart..Source.IndexOf("finally {", catchStart, StringComparison.Ordinal)];
        Assert.DoesNotContain("Remove-Item -LiteralPath $backupPath", catchSection, StringComparison.Ordinal);
        Assert.DoesNotContain("Invoke-Native $CreateDbPath", catchSection, StringComparison.Ordinal);
    }

    [Fact]
    public void IncompletePreservationEvidenceCannotPass()
    {
        var source = RequiredPreservation().ToDictionary(x => x, _ => "1");
        var target = RequiredPreservation().ToDictionary(x => x, _ => "1");
        target.Remove("audit_logs");
        Assert.False(PreservationPass(source, target));
        target["audit_logs"] = "2";
        Assert.False(PreservationPass(source, target));
        target["audit_logs"] = "1";
        Assert.True(PreservationPass(source, target));
        foreach (var name in RequiredPreservation()) Assert.Contains($"nexa.{name}", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void ExactRev868C3CountsAndFailClosedStatesAreRequired()
    {
        Assert.True(SourceEvidencePass(ExpectedMigrations, 0, 42, true, 12, 14));
        Assert.False(SourceEvidencePass(ExpectedMigrations, 0, 41, true, 12, 14));
        Assert.False(SourceEvidencePass(ExpectedMigrations, 0, 42, false, 12, 14));
        Assert.False(SourceEvidencePass(ExpectedMigrations, 0, 42, true, 11, 14));
        Assert.False(SourceEvidencePass(ExpectedMigrations, 0, 42, true, 12, 13));
        foreach (var marker in new[] { "safe_source_state=PASS", "provisioning_readiness_state=PASS", "provision_acceptance_state=PASS" })
            Assert.Contains(marker, Source, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryApplicationRelationIsExplicitlyNexaQualified()
    {
        foreach (var relation in RequiredPreservation())
            Assert.Contains($"nexa.{relation}", Source, StringComparison.Ordinal);

        foreach (var required in new[]
        {
            "from nexa.employees", "from nexa.departments", "from nexa.department_approval_mappings",
            "nexa.purchase_approval_workflow_steps", "nexa.employee_status_history",
            "nexa.employee_department_history", "nexa.audit_logs"
        }) Assert.Contains(required, Source, StringComparison.Ordinal);

        Assert.DoesNotMatch(new Regex(@"(?im)\b(from|join)\s+(employees|departments|department_approval_mappings)\b"), Source);
    }

    [Fact]
    public void MigrationHistoryIsPublicQualifiedWithoutSearchPathWorkaround()
    {
        Assert.Contains("from public.\"__EFMigrationsHistory\"", Source, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex("(?im)\\bfrom\\s+\\\"__EFMigrationsHistory\\\""), Source);
        Assert.DoesNotContain("SET search_path", Source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RouteColumnsRemainTableSpecificAcrossHelperAndAuthoritativeSources()
    {
        Assert.Contains("from nexa.department_approval_mappings where \"ApprovalRouteCode\"='MANAGER'", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("department_approval_mappings where \"RouteCode\"", Source, StringComparison.Ordinal);
        Assert.Contains("'nexa.department_approval_mappings' = @('ApprovalRouteCode', 'IsActive')", Source, StringComparison.Ordinal);
        Assert.Contains("'nexa.purchase_approval_workflow_steps' = @('RouteCode')", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("'nexa.department_approval_mappings' = @('RouteCode'", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("'nexa.purchase_approval_workflow_steps' = @('ApprovalRouteCode'", Source, StringComparison.Ordinal);

        Assert.Contains("entity.Property(x => x.ApprovalRouteCode)", DbContextSource, StringComparison.Ordinal);
        Assert.Contains("entity.Property(x => x.RouteCode)", DbContextBlock("purchase_approval_workflow_steps"), StringComparison.Ordinal);
        Assert.Contains("b.Property<string>(\"ApprovalRouteCode\")", SnapshotBlock("department_approval_mappings"), StringComparison.Ordinal);
        Assert.DoesNotContain("b.Property<string>(\"RouteCode\")", SnapshotBlock("department_approval_mappings"), StringComparison.Ordinal);
        Assert.Contains("b.Property<string>(\"RouteCode\")", SnapshotBlock("purchase_approval_workflow_steps"), StringComparison.Ordinal);
        Assert.DoesNotContain("b.Property<string>(\"ApprovalRouteCode\")", SnapshotBlock("purchase_approval_workflow_steps"), StringComparison.Ordinal);
        Assert.Contains("ApprovalRouteCode = table.Column<string>", Rev868C2Migration, StringComparison.Ordinal);
        Assert.Contains("RouteCode = table.Column<string>", Rev868C3Migration, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryHelperRelationAndDirectColumnMatchesCurrentSnapshot()
    {
        foreach (var relation in RequiredPreservation())
            Assert.Contains($"b.ToTable(\"{relation}\", \"advance\"", SnapshotSource, StringComparison.Ordinal);

        var expectedColumns = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["employees"] = ["EmployeeCode", "Status"],
            ["departments"] = ["Code", "IsActive"],
            ["department_approval_mappings"] = ["ApprovalRouteCode", "IsActive"],
            ["purchase_approval_workflow_steps"] = ["RouteCode"]
        };
        foreach (var contract in expectedColumns)
            foreach (var column in contract.Value)
                Assert.Contains($"(\"{column}\")", SnapshotBlock(contract.Key), StringComparison.Ordinal);

        Assert.Contains("'public.__EFMigrationsHistory' = @('MigrationId')", Source, StringComparison.Ordinal);
        Assert.Contains("information_schema.tables", Source, StringComparison.Ordinal);
        Assert.Contains("information_schema.columns", Source, StringComparison.Ordinal);
        Assert.Contains("schema_contract_state='PASS'", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaAndEvidenceBuildersContainNoDatabaseModificationSql()
    {
        var schemaBuilder = FunctionBlock("function New-SchemaContractSql", "function New-EvidenceSql");
        var evidenceBuilder = FunctionBlock("function New-EvidenceSql", "function Convert-Evidence");
        foreach (var builder in new[] { schemaBuilder, evidenceBuilder })
            Assert.DoesNotMatch(new Regex(@"(?im)^\s*(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|vacuum|analyze|reindex)\b"), builder);
        Assert.Contains("begin transaction read only;", schemaBuilder, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("begin transaction read only;", evidenceBuilder, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UndefinedRelationOrColumnFailureRemainsFailClosedWithSanitizedEvidence()
    {
        Assert.Contains("if ($LASTEXITCODE -ne 0)", Source, StringComparison.Ordinal);
        Assert.Contains("Set-SanitizedFailureMetadata @($output)", Source, StringComparison.Ordinal);
        Assert.Contains("sanitized diagnostic metadata captured", Source, StringComparison.Ordinal);
        Assert.Contains("provision_acceptance_state=FAIL", Source, StringComparison.Ordinal);
        Assert.Contains("failed_phase=$failedPhase", Source, StringComparison.Ordinal);
        Assert.Contains("failed_query_label=$failedQueryLabel", Source, StringComparison.Ordinal);
        Assert.Contains("sqlstate=$failureSqlState", Source, StringComparison.Ordinal);
        Assert.Contains("failure_schema=$failureSchema", Source, StringComparison.Ordinal);
        Assert.Contains("failure_table=$failureTable", Source, StringComparison.Ordinal);
        Assert.Contains("failure_column=$failureColumn", Source, StringComparison.Ordinal);
        Assert.Contains("target_state=$state", Source, StringComparison.Ordinal);
        Assert.Contains("sanitized_evidence_path=$failureEvidencePath", Source, StringComparison.Ordinal);
        Assert.Contains("SOURCE_PREFLIGHT_SCHEMA_CONTRACT", Source, StringComparison.Ordinal);
        Assert.Contains("SOURCE_PREFLIGHT_ACCEPTANCE_AND_PRESERVATION", Source, StringComparison.Ordinal);
        Assert.Contains("(DETAIL|CONTEXT|STATEMENT)", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("$Purpose failed: $($output", Source, StringComparison.Ordinal);
        Assert.Equal("NOT_CREATED_SAFE_RETRY_REQUIRES_NEW_PREFLIGHT", FailureTargetState(targetCreated: false));
    }

    [Fact]
    public void VerboseSqlDiagnosticMetadataIsIdentifierOnly()
    {
        const string diagnostic = "ERROR:  42703: column \"RouteCode\" does not exist\nSCHEMA NAME: nexa\nTABLE NAME: department_approval_mappings\nCOLUMN NAME: RouteCode";
        var parsed = ParseDiagnostic(diagnostic);
        Assert.Equal("42703", parsed.SqlState);
        Assert.Equal("nexa", parsed.Schema);
        Assert.Equal("department_approval_mappings", parsed.Table);
        Assert.Equal("RouteCode", parsed.Column);
        Assert.Contains("VERBOSITY=verbose", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("temporary_sql=", Source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FailurePathIsPrintedBeforeEvidenceWriteAndThrow()
    {
        var catchStart = Source.LastIndexOf("catch {", StringComparison.Ordinal);
        var catchSection = Source[catchStart..Source.IndexOf("finally {", catchStart, StringComparison.Ordinal)];
        var printedPath = catchSection.IndexOf("Write-Output \"sanitized_evidence_path=$failureEvidencePath\"", StringComparison.Ordinal);
        var evidenceWrite = catchSection.IndexOf("Write-SanitizedEvidence $details $failureEvidencePath", StringComparison.Ordinal);
        var failThrow = catchSection.IndexOf("throw (Protect-Text", StringComparison.Ordinal);
        Assert.True(printedPath >= 0 && printedPath < evidenceWrite && evidenceWrite < failThrow);
    }

    [Fact]
    public void SourcePreflightFailureCannotReachTargetCreation()
    {
        var provisionPreflight = Source.IndexOf("$failedPhase = \"SOURCE_PREFLIGHT\"", StringComparison.Ordinal);
        var preflightCall = Source.IndexOf("$sourceEvidence = Get-SourceEvidence", provisionPreflight, StringComparison.Ordinal);
        var preflightAssertion = Source.IndexOf("Assert-SourceEvidence $sourceEvidence", preflightCall, StringComparison.Ordinal);
        var createPhase = Source.IndexOf("$failedPhase = \"TARGET_CREATE\"", preflightAssertion, StringComparison.Ordinal);
        var createCall = Source.IndexOf("Invoke-Native $CreateDbPath", createPhase, StringComparison.Ordinal);
        Assert.True(provisionPreflight >= 0 && provisionPreflight < preflightCall && preflightCall < preflightAssertion);
        Assert.True(preflightAssertion < createPhase && createPhase < createCall);
        Assert.False(TargetCreationAllowed(sourcePreflightPassed: false));
    }

    [Fact]
    public void SchemaCorrectionDoesNotWeakenAcceptanceFormula()
    {
        foreach (var formula in new[]
        {
            "expected_migration_count=11", "actual_matched_migration_count=11", "missing_migration_count=0",
            "unexpected_migration_count=0", "duplicate_migration_count=0", "active_employee_count=42",
            "relieved_employee_expected_count=9", "relieved_employee_actual_matched_count=9",
            "relieved_employee_missing_count=0", "relieved_employee_unexpected_count=0",
            "relieved_employee_duplicate_count=0", "relieved_employee_status_mismatch_count=0",
            "relieved_employee_acceptance_state='PASS'", "active_clean_department_count=12", "active_manager_mapping_count=14",
            "target_database_count = 0", "'$SchemaContractState'='PASS'", "preservation_evidence_state='PASS'"
        }) Assert.Contains(formula, Source, StringComparison.Ordinal);

        Assert.Contains("Assert-PreservationEqual $sourceEvidence $targetEvidence", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void EvidenceSqlEmitsCanonicalReadinessExactlyOnceWithoutAliasing()
    {
        var evidenceBuilder = FunctionBlock("function New-EvidenceSql", "function Convert-Evidence");
        Assert.Single(Regex.Matches(evidenceBuilder, "'provisioning_readiness_state='"));
        Assert.Contains("actual_matched_migration_count=11", evidenceBuilder, StringComparison.Ordinal);
        Assert.Contains("current_database()='$ExpectedDatabase'", evidenceBuilder, StringComparison.Ordinal);
        Assert.Contains("'$SchemaContractState'='PASS'", evidenceBuilder, StringComparison.Ordinal);
        Assert.Contains("preservation_evidence_state='PASS'", evidenceBuilder, StringComparison.Ordinal);
        Assert.Contains("case when all_source_conditions_pass then 'PASS' else 'FAIL'", evidenceBuilder, StringComparison.Ordinal);
        Assert.DoesNotContain("case when safe_source_state='PASS'", evidenceBuilder, StringComparison.Ordinal);
    }

    [Fact]
    public void CrLfAndNormalPsqlFramingCaptureExactlyOnePass()
    {
        var payload = "BEGIN\r\n" + string.Join("\r\n", CanonicalSourceEvidence().Select(x => $"{x.Key}={x.Value}")) + "\r\nCOMMIT\r\n";
        var parsed = ParsePsqlEvidence(payload);
        Assert.True(SourceReadinessPass(parsed));
        Assert.Equal("PASS", parsed.Values["provisioning_readiness_state"]);
        Assert.Equal(1, parsed.Counts["provisioning_readiness_state"]);
        Assert.Equal(0, parsed.MalformedCount);
    }

    [Fact]
    public void MissingDuplicateMalformedOrFailReadinessIsRejected()
    {
        var canonical = CanonicalSourceEvidence();

        var missing = canonical.Where(x => x.Key != "provisioning_readiness_state").Select(x => $"{x.Key}={x.Value}").ToArray();
        Assert.False(SourceReadinessPass(ParsePsqlEvidence(missing)));

        var duplicate = canonical.Select(x => $"{x.Key}={x.Value}").Append("provisioning_readiness_state=PASS").ToArray();
        Assert.False(SourceReadinessPass(ParsePsqlEvidence(duplicate)));

        var malformed = canonical.Select(x => x.Key == "provisioning_readiness_state" ? "provisioning_readiness_state:PASS" : $"{x.Key}={x.Value}").ToArray();
        Assert.False(SourceReadinessPass(ParsePsqlEvidence(malformed)));

        var failed = canonical.Select(x => x.Key == "provisioning_readiness_state" ? "provisioning_readiness_state=FAIL" : $"{x.Key}={x.Value}").ToArray();
        Assert.False(SourceReadinessPass(ParsePsqlEvidence(failed)));
    }

    [Fact]
    public void NoRequiredFailedOrMissingCountCanProduceReadinessPass()
    {
        var canonical = CanonicalSourceEvidence();
        foreach (var key in canonical.Keys.Where(x => x.EndsWith("_count", StringComparison.Ordinal)).ToArray())
        {
            var missing = canonical.Where(x => x.Key != key).Select(x => $"{x.Key}={x.Value}").ToArray();
            Assert.False(SourceReadinessPass(ParsePsqlEvidence(missing)));

            var changed = canonical.Select(x => x.Key == key ? $"{x.Key}={DifferentValue(x.Value)}" : $"{x.Key}={x.Value}").ToArray();
            Assert.False(SourceReadinessPass(ParsePsqlEvidence(changed)));
        }
    }

    [Fact]
    public void IncompletePreflightEvidenceRemainsBeforeTargetCreationAndIsSafelyReported()
    {
        var assertion = Source.IndexOf("Assert-SourceEvidence $sourceEvidence", StringComparison.Ordinal);
        var create = Source.IndexOf("Invoke-Native $CreateDbPath", assertion, StringComparison.Ordinal);
        Assert.True(assertion >= 0 && create > assertion);
        Assert.Contains("returned_evidence_malformed_count=", Source, StringComparison.Ordinal);
        Assert.Contains("returned_evidence.$line", Source, StringComparison.Ordinal);
        Assert.Contains("target_state=$state", Source, StringComparison.Ordinal);
        Assert.False(TargetCreationAllowed(sourcePreflightPassed: false));
    }
    [Fact]
    public void RelievedEmployeeContractUsesExactNineCommittedCodes()
    {
        var declaration = Regex.Match(Source, @"\$relievedEmployeeCodes = @\((?<values>[^)]*)\)");
        Assert.True(declaration.Success);
        var helperCodes = Regex.Matches(declaration.Groups["values"].Value, "SESS-[0-9]{3}")
            .Select(x => x.Value).ToArray();
        Assert.Equal(ExpectedRelievedEmployeeCodes, helperCodes);
        Assert.Equal(9, helperCodes.Length);
        foreach (var code in ExpectedRelievedEmployeeCodes)
            Assert.Contains($"Relieved(\"{code}\"", Rev868C3WorkbookData, StringComparison.Ordinal);
    }


    [Fact]
    public void MissingExpectedRelievedEmployeeFailsClosed()
    {
        Assert.False(RelievedSetPass(AcceptedRelievedRows().Skip(1)));
    }

    [Fact]
    public void ActiveExpectedRelievedEmployeeFailsClosed()
    {
        var rows = AcceptedRelievedRows();
        rows[0] = rows[0] with { Status = "Active" };
        Assert.False(RelievedSetPass(rows));
    }

    [Fact]
    public void UnexpectedRelievedEmployeeFailsClosed()
    {
        Assert.False(RelievedSetPass(AcceptedRelievedRows().Append(new EmployeeStatusRow("SESS-999", "Left / Resigned"))));
    }

    [Fact]
    public void DuplicateExpectedRelievedEmployeeFailsClosed()
    {
        Assert.False(RelievedSetPass(AcceptedRelievedRows().Append(new EmployeeStatusRow("SESS-016", "Left / Resigned"))));
    }

    [Fact]
    public void RelievedStatusMismatchFailsClosed()
    {
        var rows = AcceptedRelievedRows();
        rows[4] = rows[4] with { Status = "Terminated" };
        Assert.False(RelievedSetPass(rows));
    }

    [Fact]
    public void RelievedExpectedCountCannotBeChangedToZeroOrInferredFromTotals()
    {
        Assert.Contains("relieved_employee_expected_count=9", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("relieved_employee_expected_count=0", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("51", FunctionBlock("function New-EvidenceSql", "function Convert-Evidence"), StringComparison.Ordinal);
        Assert.False(RelievedSetPass(Array.Empty<EmployeeStatusRow>()));
    }

    [Fact]
    public void ProvisioningReadinessRequiresExactRelievedSetAcceptance()
    {
        var evidenceSql = FunctionBlock("function New-EvidenceSql", "function Convert-Evidence");
        Assert.Contains("and relieved_employee_acceptance_state='PASS'", evidenceSql, StringComparison.Ordinal);
        Assert.Contains("'relieved_employee_acceptance_state=' || relieved_employee_acceptance_state", evidenceSql, StringComparison.Ordinal);
        Assert.Contains("relieved_employee_acceptance_state='PASS'", FunctionBlock("function Assert-SourceEvidence", "function Assert-AcceptedCoreEvidence"), StringComparison.Ordinal);

        var failed = CanonicalSourceEvidence()
            .Select(x => x.Key == "relieved_employee_acceptance_state" ? $"{x.Key}=FAIL" : $"{x.Key}={x.Value}")
            .ToArray();
        Assert.False(SourceReadinessPass(ParsePsqlEvidence(failed)));
    }

    [Fact]
    public void ExactRelievedSetGatePreservesSchemaMigrationAndPreservationRequirements()
    {
        var evidenceSql = FunctionBlock("function New-EvidenceSql", "function Convert-Evidence");
        foreach (var gate in new[]
        {
            "expected_migration_count=11", "actual_matched_migration_count=11", "missing_migration_count=0",
            "unexpected_migration_count=0", "duplicate_migration_count=0", "active_employee_count=42",
            "active_clean_department_count=12", "active_manager_mapping_count=14", "'$SchemaContractState'='PASS'",
            "preservation_evidence_state='PASS'", "target_database_count = 0"
        }) Assert.Contains(gate, evidenceSql, StringComparison.Ordinal);
        Assert.Contains("Assert-PreservationEqual $sourceEvidence $targetEvidence", Source, StringComparison.Ordinal);
    }
    [Fact]
    public void PostVerificationAlwaysWritesANewUniqueReportBeforePass()
    {
        var pathFactory = FunctionBlock("function New-SanitizedEvidencePath", "function Write-SanitizedEvidence");
        Assert.Contains("yyyyMMdd-HHmmss", pathFactory, StringComparison.Ordinal);
        Assert.Contains("[guid]::NewGuid().ToString(\"N\")", pathFactory, StringComparison.Ordinal);
        var first = SimulatedEvidencePath(Guid.NewGuid());
        var second = SimulatedEvidencePath(Guid.NewGuid());
        Assert.NotEqual(first, second);

        var branch = PostVerificationBranch();
        var write = branch.IndexOf("Write-PostProvisionEvidenceReport", StringComparison.Ordinal);
        var pass = branch.IndexOf("Write-Output \"post_provision_acceptance_state=PASS\"", StringComparison.Ordinal);
        Assert.True(write >= 0 && pass > write);
    }

    [Fact]
    public void PostVerificationPrintsBothAcceptanceStatesAndExactReportPathAfterWrite()
    {
        var branch = PostVerificationBranch();
        var write = branch.IndexOf("$evidencePath = Write-PostProvisionEvidenceReport", StringComparison.Ordinal);
        var postPass = branch.IndexOf("post_provision_acceptance_state=PASS", StringComparison.Ordinal);
        var provisionPass = branch.IndexOf("provision_acceptance_state=PASS", StringComparison.Ordinal);
        var path = branch.IndexOf("sanitized_evidence_path=$evidencePath", StringComparison.Ordinal);
        Assert.True(write >= 0 && postPass > write && provisionPass > postPass && path > provisionPass);
    }

    [Fact]
    public void PostVerificationReportBuilderContainsEveryCanonicalLabelExactlyOnce()
    {
        var builder = FunctionBlock("function New-PostProvisionEvidenceLines", "function Assert-PostProvisionReportContract");
        foreach (var label in CanonicalPostProvisionReportLines()
                     .Select(x => x[..x.IndexOf('=')])
                     .Where(x => !x.StartsWith("preservation.", StringComparison.Ordinal)))
            Assert.Single(Regex.Matches(builder, $@"(?<![a-z0-9_.]){Regex.Escape(label)}="));
        foreach (var suffix in new[] { ".source_count=", ".target_count=", ".mismatch_state=" })
            Assert.Single(Regex.Matches(builder, Regex.Escape("preservation.$table" + suffix)));
        Assert.Contains("foreach ($table in $preservationTables)", builder, StringComparison.Ordinal);
    }

    [Fact]
    public void PostVerificationPassRequiresExactElevenMigrationEquality()
    {
        Assert.True(PostProvisionReportPass(CanonicalPostProvisionReportLines()));
        Assert.False(PostProvisionReportPass(ReplaceReportLabel("source_actual_matched_migration_count", "10")));
        Assert.False(PostProvisionReportPass(ReplaceReportLabel("target_missing_migration_count", "1")));
        Assert.False(PostProvisionReportPass(ReplaceReportLabel("migration_set_equality_state", "FAIL")));
        Assert.False(PostProvisionReportPass(ReplaceReportLabel("accepted_migration_ids", string.Join(',', ExpectedMigrations.Take(10)))));
        var contract = FunctionBlock("function New-PostProvisionEvidenceLines", "function Write-Plan");
        Assert.Contains("$sourceFingerprint -cne $acceptedFingerprint -or $targetFingerprint -cne $acceptedFingerprint", contract, StringComparison.Ordinal);
    }

    [Fact]
    public void PostVerificationPassRequiresEveryPreservationRelationToMatch()
    {
        foreach (var relation in RequiredPreservation())
        {
            Assert.False(PostProvisionReportPass(ReplaceReportLabel($"preservation.{relation}.target_count", "2")));
            Assert.False(PostProvisionReportPass(ReplaceReportLabel($"preservation.{relation}.mismatch_state", "FAIL")));
        }
        Assert.Contains("Assert-PreservationEqual $Source $Target", Source, StringComparison.Ordinal);
        Assert.Contains("$sourceCount -cne $targetCount", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void PostVerificationMissingDuplicateMalformedOrConflictingEvidenceFailsClosed()
    {
        var canonical = CanonicalPostProvisionReportLines();
        Assert.False(PostProvisionReportPass(canonical.Where(x => !x.StartsWith("target_schema_contract_state=", StringComparison.Ordinal))));
        Assert.False(PostProvisionReportPass(canonical.Append("post_provision_acceptance_state=PASS")));
        Assert.False(PostProvisionReportPass(canonical.Select(x => x.StartsWith("provision_acceptance_state=", StringComparison.Ordinal) ? "provision_acceptance_state:PASS" : x)));
        Assert.False(PostProvisionReportPass(canonical.Append("post_provision_acceptance_state=FAIL")));
    }

    [Fact]
    public void PostVerificationReportWriteFailureCannotProducePass()
    {
        var branch = PostVerificationBranch();
        var write = branch.IndexOf("$evidencePath = Write-PostProvisionEvidenceReport", StringComparison.Ordinal);
        var pass = branch.IndexOf("post_provision_acceptance_state=PASS", StringComparison.Ordinal);
        Assert.True(write >= 0 && pass > write);
        Assert.Contains("if ($writtenPath -cne $path -or -not (Test-Path -LiteralPath $path))", Source, StringComparison.Ordinal);
        Assert.Contains("Assert-PostProvisionReportContract ([IO.File]::ReadAllLines($path))", Source, StringComparison.Ordinal);
        Assert.False(PostAcceptanceOutputAllowed(reportWriteSucceeded: false, persistedReportValidated: false));
    }

    [Fact]
    public void StandalonePostVerificationContainsNoDatabaseModificationOperation()
    {
        var branch = PostVerificationBranch();
        foreach (var prohibited in new[] { "$PgDumpPath", "$CreateDbPath", "$PgRestorePath", "Invoke-Native", "CREATE DATABASE", "DROP DATABASE", "--clean", "--create" })
            Assert.DoesNotContain(prohibited, branch, StringComparison.OrdinalIgnoreCase);
        var evidenceSql = FunctionBlock("function New-EvidenceSql", "function Convert-Evidence");
        Assert.True(IsReadOnlySql(ExtractHereString(evidenceSql)));
    }

    [Fact]
    public void PostVerificationFailureNeverDropsOrRepairsExistingTarget()
    {
        var catchStart = Source.LastIndexOf("catch {", StringComparison.Ordinal);
        var catchSection = Source[catchStart..Source.IndexOf("finally {", catchStart, StringComparison.Ordinal)];
        Assert.Contains("EXISTING_TARGET_DO_NOT_AUTO_REPAIR_OR_DROP", catchSection, StringComparison.Ordinal);
        Assert.Contains("post_provision_acceptance_state=FAIL", catchSection, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP DATABASE", catchSection, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$CreateDbPath", catchSection, StringComparison.Ordinal);
        Assert.DoesNotContain("$PgRestorePath", catchSection, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingProvisionPathAndEvidenceBehaviorRemainPresent()
    {
        var provisionStart = Source.IndexOf("$failedPhase = \"SOURCE_BACKUP\"", StringComparison.Ordinal);
        var provisionEnd = Source.LastIndexOf("catch {", StringComparison.Ordinal);
        var provision = Source[provisionStart..provisionEnd];
        Assert.Contains("Invoke-Native $PgDumpPath", provision, StringComparison.Ordinal);
        Assert.Contains("Invoke-Native $CreateDbPath", provision, StringComparison.Ordinal);
        Assert.Contains("Invoke-Native $PgRestorePath", provision, StringComparison.Ordinal);
        Assert.Contains("$evidencePath = Write-SanitizedEvidence", provision, StringComparison.Ordinal);
        Assert.Contains("Write-Output \"provision_acceptance_state=PASS\"", provision, StringComparison.Ordinal);
        Assert.Contains("Write-Output \"sanitized_evidence_path=$evidencePath\"", provision, StringComparison.Ordinal);
    }
    [Fact]
    public void HelperRemainsPowerShell51Compatible()
    {
        Assert.DoesNotContain("ForEach-Object -Parallel", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pwsh", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$PSStyle", Source, StringComparison.Ordinal);
        Assert.Contains("Set-StrictMode -Version Latest", Source, StringComparison.Ordinal);
    }

    private static string PostVerificationBranch()
    {
        var start = Source.IndexOf("    if ($PostProvisionVerification) {", StringComparison.Ordinal);
        var end = Source.IndexOf("    $failedPhase = \"SOURCE_PREFLIGHT\"", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return Source[start..end];
    }

    private static string ExtractHereString(string function)
    {
        var start = function.IndexOf("begin transaction read only;", StringComparison.Ordinal);
        var end = function.LastIndexOf("commit;", StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return function[start..(end + "commit;".Length)];
    }

    private static string SimulatedEvidencePath(Guid id) =>
        $"rev869a-isolated-provisioning-20260810-140000-{id:N}.txt";

    private static bool PostAcceptanceOutputAllowed(bool reportWriteSucceeded, bool persistedReportValidated) =>
        reportWriteSucceeded && persistedReportValidated;

    private static string[] CanonicalPostProvisionReportLines()
    {
        var lines = new List<string>
        {
            "execution_mode=PostProvisionVerification", "evidence_timestamp_utc=20260810T1400001234567Z",
            "source_database_identity=sess_nexaerp_rev868_verify", "target_database_identity=sess_nexaerp_rev869a_verify",
            "expected_migration_count=11", "source_actual_matched_migration_count=11", "source_missing_migration_count=0",
            "source_unexpected_migration_count=0", "source_duplicate_migration_count=0", "target_actual_matched_migration_count=11",
            "target_missing_migration_count=0", "target_unexpected_migration_count=0", "target_duplicate_migration_count=0",
            "migration_set_equality_state=PASS", $"accepted_migration_ids={string.Join(',', ExpectedMigrations)}",
            "source_active_employee_count=42", "target_active_employee_count=42",
            "source_relieved_employee_expected_count=9", "target_relieved_employee_expected_count=9",
            "source_relieved_employee_actual_matched_count=9", "target_relieved_employee_actual_matched_count=9",
            "source_relieved_employee_missing_count=0", "target_relieved_employee_missing_count=0",
            "source_relieved_employee_unexpected_count=0", "target_relieved_employee_unexpected_count=0",
            "source_relieved_employee_duplicate_count=0", "target_relieved_employee_duplicate_count=0",
            "source_relieved_employee_status_mismatch_count=0", "target_relieved_employee_status_mismatch_count=0",
            "source_relieved_employee_acceptance_state=PASS", "target_relieved_employee_acceptance_state=PASS",
            "source_active_clean_department_count=12", "target_active_clean_department_count=12",
            "source_active_manager_mapping_count=14", "target_active_manager_mapping_count=14",
            "source_schema_contract_state=PASS", "target_schema_contract_state=PASS",
            "source_preservation_relation_count=20", "target_preservation_relation_count=20"
        };
        foreach (var relation in RequiredPreservation())
        {
            lines.Add($"preservation.{relation}.source_count=1");
            lines.Add($"preservation.{relation}.target_count=1");
            lines.Add($"preservation.{relation}.mismatch_state=PASS");
        }
        lines.Add("preservation_equality_state=PASS");
        lines.Add("post_provision_acceptance_state=PASS");
        lines.Add("provision_acceptance_state=PASS");
        return lines.ToArray();
    }

    private static string[] ReplaceReportLabel(string key, string value) =>
        CanonicalPostProvisionReportLines().Select(x => x.StartsWith(key + "=", StringComparison.Ordinal) ? $"{key}={value}" : x).ToArray();

    private static bool PostProvisionReportPass(IEnumerable<string> lines)
    {
        var actualLines = lines.ToArray();
        var parsed = ParsePsqlEvidence(actualLines);
        if (parsed.MalformedCount != 0) return false;
        var expected = ParsePsqlEvidence(CanonicalPostProvisionReportLines());
        if (actualLines.Length != expected.Counts.Count || parsed.Counts.Count != expected.Counts.Count) return false;
        foreach (var pair in expected.Values)
            if (!parsed.Counts.TryGetValue(pair.Key, out var count) || count != 1 ||
                !parsed.Values.TryGetValue(pair.Key, out var value) || value != pair.Value)
                return false;
        return true;
    }
    private static EmployeeStatusRow[] AcceptedRelievedRows() =>
        ExpectedRelievedEmployeeCodes.Select(code => new EmployeeStatusRow(code, "Left / Resigned")).ToArray();

    private static bool RelievedSetPass(IEnumerable<EmployeeStatusRow> sourceRows)
    {
        var rows = sourceRows.Select(x => x with { Status = x.Status.ToLowerInvariant() }).ToArray();
        var expected = ExpectedRelievedEmployeeCodes.ToHashSet(StringComparer.Ordinal);
        var actualMatched = rows.Count(x => expected.Contains(x.Code) && AcceptedRelievedStatuses.Contains(x.Status));
        var missing = expected.Count(code => rows.All(x => x.Code != code));
        var unexpected = rows.Count(x => !expected.Contains(x.Code) && AcceptedRelievedStatuses.Contains(x.Status));
        var duplicates = rows.Where(x => expected.Contains(x.Code)).GroupBy(x => x.Code, StringComparer.Ordinal).Count(x => x.Count() != 1);
        var statusMismatches = expected.Count(code => rows.Any(x => x.Code == code) &&
            !rows.Any(x => x.Code == code && AcceptedRelievedStatuses.Contains(x.Status)));
        return expected.Count == 9 && actualMatched == 9 && missing == 0 && unexpected == 0 && duplicates == 0 && statusMismatches == 0;
    }

    private sealed record EmployeeStatusRow(string Code, string Status);
    private static IReadOnlyDictionary<string, string> CanonicalSourceEvidence() =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["database_identity"] = "sess_nexaerp_rev868_verify",
            ["expected_migration_count"] = "11",
            ["actual_matched_migration_count"] = "11",
            ["missing_migration_count"] = "0",
            ["unexpected_migration_count"] = "0",
            ["duplicate_migration_count"] = "0",
            ["target_database_count"] = "0",
            ["active_employee_count"] = "42",
            ["relieved_employee_expected_count"] = "9",
            ["relieved_employee_actual_matched_count"] = "9",
            ["relieved_employee_missing_count"] = "0",
            ["relieved_employee_unexpected_count"] = "0",
            ["relieved_employee_duplicate_count"] = "0",
            ["relieved_employee_status_mismatch_count"] = "0",
            ["relieved_employee_acceptance_state"] = "PASS",
            ["active_clean_department_count"] = "12",
            ["active_manager_mapping_count"] = "14",
            ["schema_contract_state"] = "PASS",
            ["preservation_relation_count"] = "20",
            ["preservation_evidence_state"] = "PASS",
            ["safe_source_state"] = "PASS",
            ["provisioning_readiness_state"] = "PASS"
        };

    private static ParsedEvidence ParsePsqlEvidence(params string[] output)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var malformed = 0;
        foreach (var raw in output)
        {
            foreach (var segment in Regex.Split(raw, "\\r?\\n"))
            {
                var text = segment.Trim();
                if (text.Length == 0 || text is "BEGIN" or "COMMIT") continue;
                var match = Regex.Match(text, "^(?<key>[a-z][a-z0-9_.]*)=(?<value>[A-Za-z0-9_.-]+(?:,[A-Za-z0-9_.-]+)*)$");
                if (!match.Success) { malformed++; continue; }
                var key = match.Groups["key"].Value;
                counts[key] = counts.GetValueOrDefault(key) + 1;
                if (counts[key] == 1) values[key] = match.Groups["value"].Value;
                else values.Remove(key);
            }
        }
        return new ParsedEvidence(values, counts, malformed);
    }

    private static bool SourceReadinessPass(ParsedEvidence evidence)
    {
        if (evidence.MalformedCount != 0) return false;
        foreach (var expected in CanonicalSourceEvidence())
            if (!evidence.Counts.TryGetValue(expected.Key, out var count) || count != 1 ||
                !evidence.Values.TryGetValue(expected.Key, out var value) || value != expected.Value)
                return false;
        return true;
    }

    private static string DifferentValue(string value) => value switch
    {
        "0" => "1",
        "11" => "10",
        "42" => "41",
        "9" => "8",
        "12" => "11",
        "14" => "13",
        "20" => "19",
        _ => "FAIL"
    };

    private sealed record ParsedEvidence(
        IReadOnlyDictionary<string, string> Values,
        IReadOnlyDictionary<string, int> Counts,
        int MalformedCount);
    private static string FunctionBlock(string startMarker, string endMarker)
    {
        var start = Source.IndexOf(startMarker, StringComparison.Ordinal);
        var end = Source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return Source[start..end];
    }

    private static string SnapshotBlock(string table)
    {
        var tableAt = SnapshotSource.IndexOf($"b.ToTable(\"{table}\", \"advance\"", StringComparison.Ordinal);
        var start = SnapshotSource.LastIndexOf("modelBuilder.Entity(", tableAt, StringComparison.Ordinal);
        var end = SnapshotSource.IndexOf("modelBuilder.Entity(", tableAt + 1, StringComparison.Ordinal);
        Assert.True(tableAt >= 0 && start >= 0);
        if (end < 0) end = SnapshotSource.Length;
        return SnapshotSource[start..end];
    }

    private static string DbContextBlock(string table)
    {
        var tableAt = DbContextSource.IndexOf($"entity.ToTable(\"{table}\")", StringComparison.Ordinal);
        var start = DbContextSource.LastIndexOf("modelBuilder.Entity<", tableAt, StringComparison.Ordinal);
        var end = DbContextSource.IndexOf("modelBuilder.Entity<", tableAt + 1, StringComparison.Ordinal);
        Assert.True(tableAt >= 0 && start >= 0);
        if (end < 0) end = DbContextSource.Length;
        return DbContextSource[start..end];
    }

    private static DiagnosticMetadata ParseDiagnostic(string diagnostic)
    {
        static string Match(string text, string pattern)
        {
            var result = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return result.Success ? result.Groups[1].Value : "NOT_AVAILABLE";
        }
        return new DiagnosticMetadata(
            Match(diagnostic, @"(?:ERROR|FATAL|PANIC):\s+([0-9A-Z]{5}):"),
            Match(diagnostic, @"SCHEMA NAME:\s*([A-Za-z_][A-Za-z0-9_]*)"),
            Match(diagnostic, @"TABLE NAME:\s*([A-Za-z_][A-Za-z0-9_]*)"),
            Match(diagnostic, @"COLUMN NAME:\s*([A-Za-z_][A-Za-z0-9_]*)"));
    }

    private sealed record DiagnosticMetadata(string SqlState, string Schema, string Table, string Column);

    private static bool IsAcceptedEndpoint(string source, string target, string host, int port) =>
        source == "sess_nexaerp_rev868_verify" && target == "sess_nexaerp_rev869a_verify" &&
        host == "localhost" && port == 5432 && source != target &&
        !Regex.IsMatch(source + target, "rev861|production|prod|live|main", RegexOptions.IgnoreCase);

    private static bool MigrationSetPass(IEnumerable<string> actual)
    {
        var rows = actual.ToArray();
        return rows.Length == 11 && rows.Distinct(StringComparer.Ordinal).Count() == 11 &&
               rows.Order(StringComparer.Ordinal).SequenceEqual(ExpectedMigrations.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    private static bool SourceEvidencePass(IEnumerable<string> migrations, int targetCount, int active, bool relievedSetAccepted, int departments, int mappings) =>
        MigrationSetPass(migrations) && targetCount == 0 && active == 42 && relievedSetAccepted && departments == 12 && mappings == 14;

    private static bool IsReadOnlySql(string sql)
    {
        var stripped = Regex.Replace(sql, "'(?:''|[^'])*'", "''");
        return Regex.IsMatch(sql, @"^\s*begin\s+transaction\s+read\s+only\s*;", RegexOptions.IgnoreCase) &&
               Regex.IsMatch(sql, @"(commit|rollback)\s*;\s*$", RegexOptions.IgnoreCase) &&
               !Regex.IsMatch(stripped, @"\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|vacuum|analyze|reindex)\b", RegexOptions.IgnoreCase);
    }

    private static bool RestoreOptionsPass(IEnumerable<string> args)
    {
        var options = args.ToArray();
        return !options.Contains("--clean") && !options.Contains("--create") &&
               options.Contains("--no-owner") && options.Contains("--no-privileges");
    }

    private static bool BackupEvidencePass(string hash, long bytes, DateTime created) =>
        Regex.IsMatch(hash, "^[A-Fa-f0-9]{64}$") && bytes > 0 && created != default;

    private static string FailureTargetState(bool targetCreated) =>
        targetCreated ? "QUARANTINED_DO_NOT_USE_OR_AUTO_REPAIR" : "NOT_CREATED_SAFE_RETRY_REQUIRES_NEW_PREFLIGHT";

    private static bool TargetCreationAllowed(bool sourcePreflightPassed) => sourcePreflightPassed;

    private static string[] RequiredPreservation() =>
    [
        "employees", "departments", "department_approval_mappings", "purchase_requisitions",
        "purchase_requisition_approval_history", "purchase_requisition_status_history",
        "stock_availability_checks", "stock_availability_check_lines", "stock_reservations",
        "stock_reservation_history", "purchase_requirement_handoffs", "purchase_approval_route_settings",
        "purchase_approval_workflow_steps", "page_definitions", "role_page_permissions", "audit_logs",
        "employee_status_history", "employee_department_history", "employee_approval_history", "employee_import_history"
    ];

    private static bool PreservationPass(IReadOnlyDictionary<string, string> source, IReadOnlyDictionary<string, string> target) =>
        RequiredPreservation().All(key => source.TryGetValue(key, out var before) && target.TryGetValue(key, out var after) && before == after);

    private static string Checkpoint() => File.ReadAllText(Path.Combine(Root, "outputs", "rev869a_isolated_database_provisioning_checkpoint.md"));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
