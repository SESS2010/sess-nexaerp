using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.SecurityMigrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static readonly string[] ExpectedBusinessMigrationIds =
    [
        "20260824032638_AdvanceInitialBaseline",
        "20260824135450_MultiCompanySharedIdentityFoundation",
        "20260824150742_CalibrationPurchasePairItemTypeCorrections",
        "20260825063221_EmployeeMasterRebuild42",
        "20260825073027_CorrectManagingDirectorDepartmentPriority",
        "20260825092016_AuthenticationBootstrapFoundation",
        "20260825125621_MultiCompanyEmployeeAuthorizationPart1",
        "20260825135023_ApprovalConfigurationAndPermissionsPart2",
        "20260827093952_FirstStoresPart1FoundationInboundNotifications",
        "20260827110550_FirstStoresPart2GrnAndSerials"
    ];

    private const string RelationshipCodesUpAssertions = """
        DO $assert$
        BEGIN
          IF (SELECT count(*) FROM information_schema.columns
              WHERE table_schema='advance'
                AND ((table_name='customer_company_relationships' AND column_name='CustomerAssignedSupplierCode')
                  OR (table_name='vendor_company_relationships' AND column_name='VendorAssignedCustomerCode'))
                AND data_type='character varying' AND character_maximum_length=80 AND is_nullable='YES') <> 2
          THEN RAISE EXCEPTION 'relationship external code columns are not exact nullable varchar(80)'; END IF;

          IF (SELECT count(*) FROM pg_indexes
              WHERE schemaname='advance'
                AND indexname IN ('IX_customer_company_relationships_CustomerAssignedSupplierCode',
                                  'IX_vendor_company_relationships_VendorAssignedCustomerCode')
                AND indexdef LIKE '%IS NOT NULL%') <> 2
          THEN RAISE EXCEPTION 'relationship external code filtered indexes are missing'; END IF;
        END $assert$;
        """;

    private const string RelationshipCodesDownAssertions = """
        DO $assert$
        BEGIN
          IF EXISTS (SELECT 1 FROM information_schema.columns
                     WHERE table_schema='advance'
                       AND column_name IN ('CustomerAssignedSupplierCode','VendorAssignedCustomerCode'))
          THEN RAISE EXCEPTION 'relationship external code columns survived Down'; END IF;
          IF EXISTS (SELECT 1 FROM pg_indexes
                     WHERE schemaname='advance'
                       AND indexname IN ('IX_customer_company_relationships_CustomerAssignedSupplierCode',
                                         'IX_vendor_company_relationships_VendorAssignedCustomerCode'))
          THEN RAISE EXCEPTION 'relationship external code indexes survived Down'; END IF;
        END $assert$;
        """;

    [Fact]
    public void TrialMasterDataPackageIsExplicitlyMarkedDevelopmentOnlyAndRemovable()
    {
        var root = FindRepositoryRoot();
        var apply = File.ReadAllText(Path.Combine(root, "database", "postgresql", "trial-master-data-apply.sql"));
        var remove = File.ReadAllText(Path.Combine(root, "database", "postgresql", "trial-master-data-remove.sql"));
        var wrapper = File.ReadAllText(Path.Combine(root, "tools", "trial-master-data.ps1"));

        Assert.Contains("'TRIAL_DATA'", apply, StringComparison.Ordinal);
        Assert.Contains("LIKE 'TRIAL-%'", apply, StringComparison.Ordinal);
        Assert.Contains("ARRAY[6,6,4,5,15,20,2,22,26,12,0]", apply, StringComparison.Ordinal);
        Assert.Contains("('TRIAL-NOS',0),('TRIAL-SET',0),('TRIAL-LOT',0)", apply, StringComparison.Ordinal);
        Assert.Contains("('TRIAL-KG',3),('TRIAL-MTR',3),('TRIAL-LTR',3)", apply, StringComparison.Ordinal);
        Assert.Contains("principal-provisioned database", apply, StringComparison.Ordinal);
        Assert.Contains("principal-provisioned database", remove, StringComparison.Ordinal);
        Assert.True(remove.IndexOf("DELETE FROM advance.store_category_routes", StringComparison.Ordinal) <
                    remove.IndexOf("DELETE FROM advance.warehouse_condition_locations", StringComparison.Ordinal));
        Assert.True(remove.IndexOf("DELETE FROM advance.warehouse_condition_locations", StringComparison.Ordinal) <
                    remove.IndexOf("DELETE FROM advance.rack_bins", StringComparison.Ordinal));
        Assert.True(remove.IndexOf("DELETE FROM advance.rack_bins", StringComparison.Ordinal) <
                    remove.IndexOf("DELETE FROM advance.warehouses", StringComparison.Ordinal));
        Assert.True(remove.IndexOf("DELETE FROM advance.items", StringComparison.Ordinal) <
                    remove.IndexOf("DELETE FROM advance.vendors", StringComparison.Ordinal));
        Assert.Contains("DOTNET_ENVIRONMENT -cne 'Development'", wrapper, StringComparison.Ordinal);
        Assert.Contains("NexaErp__AllowTrialData -cne 'true'", wrapper, StringComparison.Ordinal);
        Assert.Contains("PGDATABASE -cne $env:NexaErp__ExpectedDatabase", wrapper, StringComparison.Ordinal);
        Assert.Contains("ConvertTo-PostgreSqlDirectoryVersion $_.Name", wrapper, StringComparison.Ordinal);
        Assert.Contains("[version]::Parse(($parts -join '.'))", wrapper, StringComparison.Ordinal);
        Assert.DoesNotContain("[version]$_.Name", wrapper, StringComparison.Ordinal);
    }

    [Fact]
    public void TrialMasterDataAppliesTwiceAndRemovesOnDisposablePostgreSql()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migration = db.Database.GetMigrations().Last();
        var root = FindRepositoryRoot();
        var apply = File.ReadAllText(Path.Combine(root, "database", "postgresql", "trial-master-data-apply.sql"));
        var remove = File.ReadAllText(Path.Combine(root, "database", "postgresql", "trial-master-data-remove.sql"));

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("trial-business-up.sql", migrator.GenerateScript("0", migration));
        server.AssertRejected("trial-wrong-database.sql", "\\set expected_database wrong_database\n" + apply, "database mismatch");
        server.Execute("trial-apply.sql", "\\set expected_database advance_parser\n" + apply);
        server.Execute("trial-reapply.sql", "\\set expected_database advance_parser\n" + apply);
        server.Execute("trial-remove.sql", "\\set expected_database advance_parser\n" + remove);
        server.Execute("trial-remove-again.sql", "\\set expected_database advance_parser\n" + remove);
        server.Execute("trial-managed-role.sql", "CREATE ROLE nexa_erp_runtime NOLOGIN;");
        server.AssertRejected("trial-managed-role-rejected.sql", "\\set expected_database advance_parser\n" + apply,
            "principal-provisioned database");
        server.Execute("trial-managed-role-drop.sql", "DROP ROLE nexa_erp_runtime;");
    }

    [Fact]
    public void CompanyRelationshipExternalCodesApplyAndRevertOnDisposablePostgreSql()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        const string target = "20260829045502_CompanyRelationshipExternalCodes";
        var targetIndex = Array.IndexOf(migrations, target);
        Assert.True(targetIndex > 0);
        var predecessor = migrations[targetIndex - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("relationship-codes-prerequisite.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("relationship-codes-up.sql", migrator.GenerateScript(predecessor, target) + RelationshipCodesUpAssertions);
        server.Execute("relationship-codes-down.sql", migrator.GenerateScript(target, predecessor) + RelationshipCodesDownAssertions);
    }

    [Fact]
    public void MasterDataImportMigrationGuardsUpAndDownAndPurgesOnlyExpiredSensitiveValues()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        const string target = "20260829065338_MasterDataImportFramework";
        var targetIndex = Array.IndexOf(migrations, target);
        Assert.True(targetIndex > 0);
        var predecessor = migrations[targetIndex - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("master-import-prerequisite.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("master-import-up.sql", migrator.GenerateScript(predecessor, target) + """
            DO $assert$
            BEGIN
              IF to_regclass('advance.master_import_batches') IS NULL
                 OR to_regclass('advance.master_import_row_results') IS NULL THEN
                RAISE EXCEPTION 'master import tables are missing';
              END IF;
              IF (SELECT pg_get_expr(d.adbin,d.adrelid) FROM pg_attrdef d
                  JOIN pg_attribute a ON a.attrelid=d.adrelid AND a.attnum=d.adnum
                  WHERE d.adrelid='advance.master_import_batches'::regclass AND a.attname='RetentionExpiresAt') NOT LIKE '%90 days%' THEN
                RAISE EXCEPTION '90-day retention default is missing';
              END IF;
              IF to_regprocedure('advance.purge_expired_master_import_sensitive_values()') IS NULL THEN
                RAISE EXCEPTION 'retention purge function is missing';
              END IF;
            END $assert$;

            INSERT INTO advance.master_import_batches
              ("Id","MasterKey","TemplateVersion","CompanyId","ImportMode","Status","OriginalFileName",
               "FileSizeBytes","FileSha256","IdempotencyKey","RequestFingerprint","UploadedByEmployeeId",
               "UploadedByEmployeeCode","OperationalRoleCode","UploadedAt","CompletedAt","RetentionExpiresAt",
               "TotalRows","ValidRows","InvalidRows","CreatedRows","UpdatedRows","UnchangedRows","RejectedRows",
               "NotImportedRows","CorrelationId","CreatedAt","CreatedBy","Version")
            SELECT '91000000-0000-0000-0000-000000000001','uoms',1,c."Id",'IMPORT_VALID_ROWS','COMPLETED_WITH_ERRORS',
                   'uoms.xlsx',100,repeat('a',64),'purge-test',repeat('b',64),e."Id",e."EmployeeCode",'STORES_MANAGER',
                   clock_timestamp()-interval '100 days',clock_timestamp()-interval '100 days',clock_timestamp()-interval '10 days',
                   1,0,1,0,0,0,1,0,'92000000-0000-0000-0000-000000000001',clock_timestamp()-interval '100 days','test',0
            FROM advance.companies c CROSS JOIN LATERAL (SELECT "Id","EmployeeCode" FROM advance.employees ORDER BY "Id" LIMIT 1) e
            ORDER BY c."Id" LIMIT 1;

            INSERT INTO advance.master_import_row_results
              ("Id","ImportBatchId","SourceRowNumber","BusinessCode","NormalizedBusinessCode","IntendedAction","Outcome",
               "SubmittedValuesJson","ErrorsJson","ProcessedAt","CreatedAt","CreatedBy","Version")
            VALUES ('93000000-0000-0000-0000-000000000001','91000000-0000-0000-0000-000000000001',2,'BAD','BAD','CREATE','REJECTED',
                    '{"Code":"BAD","Contact":"private"}',
                    '[{"columnKey":"Code","columnHeader":"Code","code":"INVALID","message":"Bad code","attemptedValue":"private"}]',
                    clock_timestamp(),clock_timestamp(),'test',0);
            """);
        server.AssertRejected("master-import-append-only.sql", """
            UPDATE advance.master_import_row_results SET "BusinessCode"='EDITED'
            WHERE "Id"='93000000-0000-0000-0000-000000000001';
            """, "append-only");
        server.Execute("master-import-purge.sql", """
            SELECT * FROM advance.purge_expired_master_import_sensitive_values();
            DO $assert$
            DECLARE values_json jsonb; errors_json jsonb;
            BEGIN
              SELECT "SubmittedValuesJson","ErrorsJson" INTO values_json,errors_json
              FROM advance.master_import_row_results WHERE "Id"='93000000-0000-0000-0000-000000000001';
              IF values_json IS NOT NULL THEN RAISE EXCEPTION 'submitted values survived purge'; END IF;
              IF errors_json @? '$[*].attemptedValue' THEN RAISE EXCEPTION 'attempted value survived purge'; END IF;
              IF errors_json->0->>'code'<>'INVALID' OR errors_json->0->>'message'<>'Bad code' THEN
                RAISE EXCEPTION 'permanent error evidence was changed';
              END IF;
              IF (SELECT "TotalRows" FROM advance.master_import_batches WHERE "Id"='91000000-0000-0000-0000-000000000001')<>1
                 OR (SELECT "FileSha256" FROM advance.master_import_batches WHERE "Id"='91000000-0000-0000-0000-000000000001')<>repeat('a',64)
                 OR (SELECT "SensitiveValuesPurgedAt" FROM advance.master_import_batches WHERE "Id"='91000000-0000-0000-0000-000000000001') IS NULL THEN
                RAISE EXCEPTION 'permanent batch evidence was changed or purge was not recorded';
              END IF;
            END $assert$;
            """);
        server.Execute("master-import-down.sql", migrator.GenerateScript(target, predecessor) + """
            DO $assert$
            BEGIN
              IF to_regclass('advance.master_import_batches') IS NOT NULL
                 OR to_regclass('advance.master_import_row_results') IS NOT NULL
                 OR to_regprocedure('advance.purge_expired_master_import_sensitive_values()') IS NOT NULL THEN
                RAISE EXCEPTION 'master import objects survived Down';
              END IF;
            END $assert$;
            """);
    }

    [Fact]
    public void ControlledTaxGstWorkflowGuardsUpAndDownOnDisposablePostgreSql()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        const string target = "20260829114544_ControlledTaxGstWorkflow";
        var targetIndex = Array.IndexOf(migrations, target);
        Assert.True(targetIndex > 0);
        var predecessor = migrations[targetIndex - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("tax-workflow-prerequisite.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("tax-workflow-up.sql", migrator.GenerateScript(predecessor, target) + """
            DO $assert$
            BEGIN
              IF to_regprocedure('advance.tax_gst_guard_controlled_mutation()') IS NULL
                 OR to_regprocedure('advance.tax_gst_guard_history_insert()') IS NULL
                 OR to_regprocedure('advance.tax_gst_require_history()') IS NULL THEN
                RAISE EXCEPTION 'GST workflow functions are missing';
              END IF;
              IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                WHERE table_schema='advance' AND table_name='tax_gst_settings' AND column_name='CreatorEmployeeId') THEN
                RAISE EXCEPTION 'GST creator identity is missing';
              END IF;
            END $assert$;
            """);
        server.AssertRejected("tax-workflow-direct-postgres-denied.sql", """
            DO $assert$
            DECLARE error_state text; error_constraint text; error_message text;
            BEGIN
              BEGIN
                INSERT INTO advance.tax_gst_settings
                  ("Id","CompanyId","OrganizationId","JurisdictionCode","HsnSacCode","SupplyType",
                   "SupplierStateCode","PlaceOfSupplyStateCode","VendorRegistrationType","GstRate","CgstRate",
                   "SgstRate","IgstRate","CessRate","IsExempt","IsReverseCharge","CurrencyCode","RoundingScale",
                   "EffectiveFrom","ApprovalStatus","CreatorEmployeeId","IsActive","CreatedAt","CreatedBy","Version")
                SELECT '94000000-0000-0000-0000-000000000001',c."Id",c."Code",'IN_GST','9025','INTRASTATE',
                       '33','33','REGULAR',18,9,9,0,0,false,false,'INR',2,current_date,'Pending Approval',
                       e."Id",true,clock_timestamp(),'postgres',0
                FROM advance.companies c CROSS JOIN LATERAL
                  (SELECT "Id" FROM advance.employees ORDER BY "Id" LIMIT 1) e
                ORDER BY c."Id" LIMIT 1;
              EXCEPTION WHEN OTHERS THEN
                GET STACKED DIAGNOSTICS error_state=RETURNED_SQLSTATE,error_constraint=CONSTRAINT_NAME,error_message=MESSAGE_TEXT;
                IF error_state='42501' AND error_constraint='tax_gst_signed_context_required' THEN
                  RAISE EXCEPTION 'exact signed command principal';
                END IF;
                RAISE EXCEPTION 'unexpected direct-write result: state=%, constraint=%, message=%',
                  error_state,error_constraint,error_message;
              END;
              RAISE EXCEPTION 'direct postgres execution was allowed';
            END $assert$;
            """, "exact signed command principal");
        server.Execute("tax-workflow-down.sql", migrator.GenerateScript(target, predecessor) + """
            DO $assert$
            BEGIN
              IF to_regprocedure('advance.tax_gst_guard_controlled_mutation()') IS NOT NULL
                 OR EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_schema='advance' AND table_name='tax_gst_settings' AND column_name='CreatorEmployeeId') THEN
                RAISE EXCEPTION 'GST workflow objects survived Down';
              END IF;
              IF NOT EXISTS (SELECT 1 FROM pg_trigger
                WHERE tgrelid='advance.tax_gst_settings'::regclass AND tgname='trg_rev869a_tax_version_guard') THEN
                RAISE EXCEPTION 'REV869A tax guard was not restored';
              END IF;
            END $assert$;
            """);
    }

    [Fact]
    public void GeneratedBusinessBaselineScriptsAreAcceptedByDisposablePostgreSql()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        AssertExpectedBusinessMigrations(migrations);
        var migration = migrations[^1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("bootstrap-role-prerequisites.sql", BootstrapRolePrerequisites);
        server.Execute("business-up.sql", migrator.GenerateScript("0", migration));
        server.Execute("business-part2-assertions.sql", Part2Assertions);
        server.Execute("business-down.sql", migrator.GenerateScript(migration, "0"));
    }

    [Fact]
    public void AuthenticationBootstrapMigrationAppliesAndRevertsWithoutManagedRoles()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migration = db.Database.GetMigrations().Last();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("business-no-roles-up.sql", migrator.GenerateScript("0", migration));
        server.Execute("business-no-roles-assertions.sql", NoManagedRoleAssertions);
        server.Execute("business-no-roles-down.sql", migrator.GenerateScript(migration, "0"));
    }

    [Fact]
    public void AuthenticationBootstrapMigrationRefusesPartialManagedRoleStateAndNamesMissingRoles()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        const string migration = "20260826065344_AuthenticationBootstrapCeremonySteps7To12";
        var predecessor = migrations[Array.IndexOf(migrations, migration) - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("business-partial-prerequisite.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("business-one-role.sql", "CREATE ROLE nexa_erp_runtime LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;");
        server.AssertRejected("business-partial-role-up.sql", migrator.GenerateScript(predecessor, migration),
            "missing managed roles: nexa_erp_bootstrap, nexa_erp_migration, nexa_erp_owner");
    }

    [Fact]
    public void PrincipalProvisioningReconcilesCeremonyAclCreatedBeforeManagedRoles()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migration = db.Database.GetMigrations().Last();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("business-before-principals.sql", migrator.GenerateScript("0", migration));
        server.Execute("installer-reconcile.sql", InstallerPasswordSettings + DatabasePrincipalProvisioningSql.Provision +
            DatabasePrincipalProvisioningSql.Verify + CeremonyAclAssertions + DatabasePrincipalProvisioningSql.RoleStatus);
        server.Execute("business-after-principals-down.sql", migrator.GenerateScript(migration, "0"));
    }

    [Fact]
    public void AuthenticationBootstrapCeremonyCompletesExactlyOnceForSess12InBothCompanies()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migration = db.Database.GetMigrations().Last();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("bootstrap-role-prerequisites.sql", BootstrapRolePrerequisites);
        server.Execute("bootstrap-business-up.sql", migrator.GenerateScript("0", migration));
        server.Execute("bootstrap-ceremony.sql", BootstrapCeremony);
        server.AssertRejected("bootstrap-replay.sql", "SET SESSION AUTHORIZATION nexa_erp_bootstrap; SELECT advance.complete_authentication_bootstrap('https://issuer.example.test/tenant/v2.0','5d80fd62-63af-4d89-a5e6-44d22f866001');");
    }

#if DEBUG
    [Fact]
    public async Task DevelopmentBootstrapRollsBackSessionAuthorizationAndTemporaryRoleAfterCeremonyFailure()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migration = db.Database.GetMigrations().Last();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("development-bootstrap-failure-up.sql", migrator.GenerateScript("0", migration));
        using var environment = DevelopmentBootstrapEnvironment(server.ConnectionString);
        var arguments = new[] {
            "authentication-bootstrap-development", "--issuer", "https://issuer.example.test/tenant/v2.0",
            "--subject", "5d80fd62-63af-4d89-a5e6-44d22f866001" };

        server.Execute("development-bootstrap-managed-role.sql", "CREATE ROLE nexa_erp_runtime NOLOGIN;");
        Assert.Equal(1, await InstallerCommand.RunAsync(arguments));
        server.Execute("development-bootstrap-remove-managed-role.sql", "DROP ROLE nexa_erp_runtime;");
        server.Execute("development-bootstrap-break-precondition.sql", """
            UPDATE advance.employee_company_assignments
               SET "IsActive"=false
             WHERE "EmployeeId"=(SELECT "Id" FROM advance.employees WHERE "EmployeeCode"='SESS-12')
               AND "CompanyId"='70000000-0000-0000-0000-000000000002';
            """);
        var result = await InstallerCommand.RunAsync([
            "authentication-bootstrap-development", "--issuer", "https://issuer.example.test/tenant/v2.0",
            "--subject", "5d80fd62-63af-4d89-a5e6-44d22f866001"]);

        Assert.Equal(1, result);
        server.Execute("development-bootstrap-failure-witness.sql", """
            DO $assert$
            BEGIN
              IF session_user<>'postgres' OR current_user<>'postgres' THEN
                RAISE EXCEPTION 'Session authorization was not restored.';
              END IF;
              IF EXISTS(SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_bootstrap') THEN
                RAISE EXCEPTION 'Temporary bootstrap role survived failed ceremony.';
              END IF;
              IF (SELECT "Status" FROM advance.authentication_bootstrap_state
                  WHERE "Id"='81000000-0000-0000-0000-000000000001')<>'PENDING' THEN
                RAISE EXCEPTION 'Failed ceremony consumed bootstrap state.';
              END IF;
            END $assert$;
            """);
    }

    [Fact]
    public async Task DevelopmentBootstrapUsesProductionFunctionDropsRoleAndRefusesReplay()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migration = db.Database.GetMigrations().Last();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("development-bootstrap-success-up.sql", migrator.GenerateScript("0", migration));
        server.AssertRejected("development-bootstrap-direct-postgres.sql",
            "SELECT advance.complete_authentication_bootstrap('https://issuer.example.test/tenant/v2.0','5d80fd62-63af-4d89-a5e6-44d22f866001');",
            "requires the dedicated nexa_erp_bootstrap login");

        using var environment = DevelopmentBootstrapEnvironment(server.ConnectionString);
        var arguments = new[] {
            "authentication-bootstrap-development", "--issuer", "https://issuer.example.test/tenant/v2.0",
            "--subject", "5d80fd62-63af-4d89-a5e6-44d22f866001" };
        Assert.Equal(0, await InstallerCommand.RunAsync(arguments));
        server.Execute("development-bootstrap-success-witness.sql", """
            DO $assert$
            BEGIN
              IF EXISTS(SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_bootstrap') THEN
                RAISE EXCEPTION 'Temporary bootstrap role survived successful ceremony.';
              END IF;
              IF (SELECT "Status" FROM advance.authentication_bootstrap_state
                  WHERE "Id"='81000000-0000-0000-0000-000000000001')<>'COMPLETED' THEN
                RAISE EXCEPTION 'Development ceremony did not complete.';
              END IF;
            END $assert$;
            """);
        Assert.Equal(1, await InstallerCommand.RunAsync(arguments));
        server.Execute("development-bootstrap-replay-witness.sql", """
            DO $assert$
            BEGIN
              IF EXISTS(SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_bootstrap') THEN
                RAISE EXCEPTION 'Temporary bootstrap role survived refused replay.';
              END IF;
              IF (SELECT count(*) FROM advance.employee_identity_mappings
                  WHERE "CreatedBy"='AUTHENTICATION_BOOTSTRAP_INSTALLER')<>2 THEN
                RAISE EXCEPTION 'Replay changed bootstrap identity rows.';
              END IF;
            END $assert$;
            """);
    }
#endif

    [Fact]
    public void ReleaseInstallerOmitsDevelopmentCommandAndRejectsSettingPresence()
    {
        var root = FindRepositoryRoot();
        var output = Path.Combine(Path.GetTempPath(), $"nexa-installer-release-{Guid.NewGuid():N}");
        Directory.CreateDirectory(output);
        try
        {
            var project = Path.Combine(root, "src", "SESS.NexaERP.Installer", "SESS.NexaERP.Installer.csproj");
            Require(RunDotnet(root, null, "build", project, "--configuration", "Release", "--no-restore", "--output", output), "Release installer build");
            var assembly = Path.Combine(output, "SESS.NexaERP.Installer.dll");
            var absent = RunDotnet(root, null, assembly, "authentication-bootstrap-development", "--issuer", "https://issuer.example.test", "--subject", "subject");
            Assert.Equal(2, absent.Code);
            Assert.DoesNotContain("authentication-bootstrap-development", absent.Output, StringComparison.Ordinal);

            var rejected = RunDotnet(root, "false", assembly, "database-principals", "plan");
            Assert.Equal(1, rejected.Code);
            Assert.Contains("must not be present in a Release build, even when set to false", rejected.Output, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, true);
        }
    }

    [Fact]
    public void MultiCompanyFoundationCatalogueAndRejectionContractsHold()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migration = db.Database.GetMigrations().Single(x => x == "20260824150742_CalibrationPurchasePairItemTypeCorrections");
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("foundation-up.sql", migrator.GenerateScript("0", migration));
        server.Execute("foundation-catalogue-assertions.sql", FoundationCatalogueAssertions);

        server.AssertRejected("reject-duplicate-company.sql",
            """INSERT INTO advance.companies ("Id","Code","LegalName","EntityType","Status","IsActive","CreatedAt","CreatedBy","Version") VALUES(gen_random_uuid(),'SESS_PVT_LTD','Duplicate','PRIVATE_LIMITED','ACTIVE',true,clock_timestamp(),'test',0);""");
        server.AssertRejected("reject-invalid-principal.sql",
            """INSERT INTO advance.user_accounts ("Id","LoginId","DisplayName","PasswordHash","UserType","PrincipalType","RoleId","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),'invalid-principal','Invalid','x','Internal','EMPLOYEE',r."Id",true,clock_timestamp(),'test',0 FROM advance.roles r LIMIT 1;""");
        server.AssertRejected("reject-second-payroll.sql",
            """INSERT INTO advance.employee_company_assignments ("Id","CompanyId","EmployeeId","AssignmentType","EmployeeCode","EmploymentType","EffectiveFrom","Status","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),"CompanyId","EmployeeId",'PAYROLL',"EmployeeCode"||'-DUP',"EmploymentType","EffectiveFrom",'ACTIVE',true,clock_timestamp(),'test',0 FROM advance.employee_company_assignments LIMIT 1;""");
        server.AssertRejected("reject-second-primary-department.sql",
            """INSERT INTO advance.employee_department_assignments ("Id","CompanyId","EmployeeCompanyAssignmentId","DepartmentId","DesignationId","AssignmentType","EffectiveFrom","IsPrimary","Status","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),a."CompanyId",a."EmployeeCompanyAssignmentId",d."Id",a."DesignationId",'PRIMARY',a."EffectiveFrom",true,'ACTIVE',true,clock_timestamp(),'test',0 FROM advance.employee_department_assignments a CROSS JOIN LATERAL (SELECT "Id" FROM advance.departments WHERE "Id"<>a."DepartmentId" LIMIT 1) d WHERE a."IsPrimary" LIMIT 1;""");
        server.AssertRejected("reject-company-code-mismatch.sql",
            """UPDATE advance.organization_policies SET "CompanyId"='70000000-0000-0000-0000-000000000002' WHERE "Id"='50000000-0000-0000-0000-000000000001';""");
        server.AssertRejected("reject-invalid-gstin.sql",
            """INSERT INTO advance.company_gst_registrations ("Id","CompanyId","Gstin","RegisteredLegalName","StateCode","RegistrationType","EffectiveFrom","IsPrimary","IsActive","CreatedAt","CreatedBy","Version") VALUES(gen_random_uuid(),'70000000-0000-0000-0000-000000000001','BAD','Bad GST','33','PRIVATE_LIMITED',DATE '2026-08-24',false,true,clock_timestamp(),'test',0);""");
        server.AssertRejected("reject-invalid-audit-scope.sql",
            """INSERT INTO advance.audit_logs ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId","UserLoginId","Result","CorrelationId","CreatedAt","CreatedBy","Version") VALUES(gen_random_uuid(),NULL,'COMPANY','Test','Test','Test','1','test','Success','test',clock_timestamp(),'test',0);""");
        server.Execute("item-rejection-uom-prerequisite.sql",
            """INSERT INTO advance.uoms ("Id","Code","Name","MeasurementDimension","QuantityPrecision","IsActive","CreatedAt","CreatedBy","Version") VALUES('73000000-0000-0000-0000-000000000001','TEST-NOS','Test Number','COUNT',0,true,clock_timestamp(),'test',0);""");
        server.AssertRejected("reject-invalid-item-type.sql",
            """INSERT INTO advance.items ("Id","ItemCode","IsItemCodeLocked","Name","DetailedDescription","MaterialType","ItemType","IsReturnable","Uom","BaseUomId","GstPercentage","QcRequired","SerialNumberTracking","BatchTracking","ShelfLifeTracking","MinimumStock","MaximumStock","ReorderLevel","Status","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),'BAD-TYPE',false,'Bad','Bad','Legacy','INVALID',false,u."Code",u."Id",0,false,false,false,false,0,0,0,'Active','Approved',true,clock_timestamp(),'test',0 FROM advance.uoms u LIMIT 1;""");
        server.AssertRejected("reject-nonreturnable-tool.sql",
            """INSERT INTO advance.items ("Id","ItemCode","IsItemCodeLocked","Name","DetailedDescription","MaterialType","ItemType","IsReturnable","Uom","BaseUomId","GstPercentage","QcRequired","SerialNumberTracking","BatchTracking","ShelfLifeTracking","MinimumStock","MaximumStock","ReorderLevel","Status","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),'BAD-TOOL',false,'Bad','Bad','Legacy','TOOL',false,u."Code",u."Id",0,false,false,false,false,0,0,0,'Active','Approved',true,clock_timestamp(),'test',0 FROM advance.uoms u LIMIT 1;""");
        server.AssertRejected("reject-returnable-nontool.sql",
            """INSERT INTO advance.items ("Id","ItemCode","IsItemCodeLocked","Name","DetailedDescription","MaterialType","ItemType","IsReturnable","Uom","BaseUomId","GstPercentage","QcRequired","SerialNumberTracking","BatchTracking","ShelfLifeTracking","MinimumStock","MaximumStock","ReorderLevel","Status","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),'BAD-COMPONENT',false,'Bad','Bad','Legacy','COMPONENT',true,u."Code",u."Id",0,false,false,false,false,0,0,0,'Active','Approved',true,clock_timestamp(),'test',0 FROM advance.uoms u LIMIT 1;""");
        server.Execute("foundation-down.sql", migrator.GenerateScript(migration, "0"));
    }

    [Fact]
    public void GeneratedSecurityPackageScriptsAreAcceptedByDisposablePostgreSql()
    {
        var connection = "Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect";
        var businessOptions = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(connection).Options;
        var securityOptions = new DbContextOptionsBuilder<Rev869BSecurityDbContext>()
            .UseNpgsql(
                connection,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(Rev869BSecurityDbContext).Assembly.FullName);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Rev869BSecurity", "advance");
                })
            .Options;
        using var business = new NexaErpDbContext(businessOptions);
        using var security = new Rev869BSecurityDbContext(securityOptions);
        var businessMigrator = business.GetService<IMigrator>();
        var securityMigrator = security.GetService<IMigrator>();
        var businessMigrations = business.Database.GetMigrations().ToArray();
        AssertExpectedBusinessMigrations(businessMigrations);
        var businessMigration = businessMigrations.Single(x =>
            x == "20260824150742_CalibrationPurchasePairItemTypeCorrections");
        var securityMigration = Assert.Single(security.Database.GetMigrations());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("business-up.sql", businessMigrator.GenerateScript("0", businessMigration));
        server.Execute("external-role-prerequisites.sql", ExternalRolePrerequisites);
        server.Execute("security-up.sql", securityMigrator.GenerateScript("0", securityMigration));
        server.Execute("security-down.sql", securityMigrator.GenerateScript(securityMigration, "0"));
        server.Execute("business-down.sql", businessMigrator.GenerateScript(businessMigration, "0"));
    }

    private static void AssertExpectedBusinessMigrations(IEnumerable<string> migrations)
    {
        var actual = migrations.ToHashSet(StringComparer.Ordinal);
        foreach (var expected in ExpectedBusinessMigrationIds)
            Assert.Contains(expected, actual);
    }

    [Theory]
    [InlineData("CREATE TABLE broken (\"Id integer);")]
    [InlineData("SELECT 'unterminated;")]
    [InlineData("SELECT (1;")]
    [InlineData("CREATE FUNCTION broken() RETURNS void LANGUAGE plpgsql AS $b$ BEGIN PERFORM (1; END $b$;")]
    [InlineData("CREATE FUNCTION broken() RETURNS void LANGUAGE plpgsql AS $b$ BEGIN RETURN;")]
    public void PostgreSqlRejectsEachMalformedRegression(string sql)
    {
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.AssertRejected("malformed-regression.sql", sql);
    }

    private const string ExternalRolePrerequisites = """
        CREATE EXTENSION pgcrypto;
        CREATE FUNCTION pg_catalog.digest(bytea,text) RETURNS bytea LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE
          AS 'SELECT public.digest($1,$2)';
        CREATE FUNCTION pg_catalog.digest(text,text) RETURNS bytea LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE
          AS 'SELECT public.digest($1,$2)';
        ALTER DATABASE advance_parser SET advance.rev869b_lease_id = '11111111-1111-1111-1111-111111111111';
        CREATE ROLE nexa_rev869b_security_owner NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_lifecycle_administrator LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_app_runtime LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_command_audit LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_management_writer LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_purge_worker LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_purge_audit LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_export_service LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_target_verifier LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        GRANT nexa_rev869b_security_owner TO nexa_rev869b_lifecycle_administrator;
        """;

    private const string BootstrapRolePrerequisites = """
        CREATE ROLE nexa_erp_owner NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_erp_bootstrap LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_erp_runtime LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_erp_migration LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        """;

    private const string NoManagedRoleAssertions = """
        DO $assert$
        BEGIN
          IF (SELECT count(*) FROM pg_catalog.pg_roles WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime'))<>0 THEN
            RAISE EXCEPTION 'Expected no managed roles.';
          END IF;
          IF EXISTS(
            SELECT 1 FROM pg_catalog.pg_proc p
            CROSS JOIN LATERAL pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) acl
            WHERE p.oid=to_regprocedure('advance.complete_authentication_bootstrap(text,text)')
              AND acl.privilege_type='EXECUTE' AND acl.grantee=0) THEN
            RAISE EXCEPTION 'PUBLIC must not execute the ceremony function.';
          END IF;
        END $assert$;
        """;

    private const string InstallerPasswordSettings = """
        SELECT pg_catalog.set_config('nexa.installer.migration_password','migration-test-password-0001',false);
        SELECT pg_catalog.set_config('nexa.installer.bootstrap_password','bootstrap-test-password-0001',false);
        SELECT pg_catalog.set_config('nexa.installer.runtime_password','runtime-test-password-000001',false);
        """;

    private const string CeremonyAclAssertions = """
        DO $assert$
        BEGIN
          IF NOT has_function_privilege('nexa_erp_bootstrap','advance.complete_authentication_bootstrap(text,text)','EXECUTE') THEN
            RAISE EXCEPTION 'Bootstrap ceremony EXECUTE grant is missing.';
          END IF;
          IF has_function_privilege('nexa_erp_runtime','advance.complete_authentication_bootstrap(text,text)','EXECUTE')
             OR has_function_privilege('nexa_erp_migration','advance.complete_authentication_bootstrap(text,text)','EXECUTE') THEN
            RAISE EXCEPTION 'Runtime or migration can execute the ceremony function.';
          END IF;
        END $assert$;
        """;

    private const string BootstrapCeremony = """
        GRANT USAGE ON SCHEMA advance TO nexa_erp_bootstrap;
        SET SESSION AUTHORIZATION nexa_erp_bootstrap;
        SELECT advance.complete_authentication_bootstrap('https://issuer.example.test/tenant/v2.0','5d80fd62-63af-4d89-a5e6-44d22f866001');
        RESET SESSION AUTHORIZATION;
        DO $assert$
        BEGIN
          IF (SELECT count(*) FROM advance.employee_identity_mappings WHERE "EmployeeId"=(SELECT "Id" FROM advance.employees WHERE "EmployeeCode"='SESS-12') AND "IsActive")<>2 THEN RAISE EXCEPTION 'Expected two SESS-12 company identity mappings.'; END IF;
          IF (SELECT count(*) FROM advance.employees WHERE "EmployeeCode"='SESS-12' AND "EmployeeName"='SURANTHER P' AND "LoginEnabled")<>1 THEN RAISE EXCEPTION 'Expected SESS-12 login to be enabled.'; END IF;
          IF (SELECT count(*) FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId" WHERE a."EmployeeId"=(SELECT "Id" FROM advance.employees WHERE "EmployeeCode"='SESS-12') AND r."Code"='IT_MANAGER')<>2 THEN RAISE EXCEPTION 'Expected two SESS-12 IT_MANAGER assignments.'; END IF;
          IF (SELECT count(*) FROM advance.employee_operational_scopes WHERE "EmployeeId"=(SELECT "Id" FROM advance.employees WHERE "EmployeeCode"='SESS-12') AND "IsActive")<>2 THEN RAISE EXCEPTION 'Expected two pre-existing company-correct SESS-12 scopes.'; END IF;
          IF (SELECT count(*) FROM advance.audit_logs WHERE "CreatedBy"='AUTHENTICATION_BOOTSTRAP_INSTALLER' AND "Scope"='COMPANY')<>2 THEN RAISE EXCEPTION 'Expected two company bootstrap audit rows.'; END IF;
          IF (SELECT count(*) FROM advance.authentication_bootstrap_state WHERE "Status"='COMPLETED' AND "CompanyCount"=2 AND octet_length("CompanySetSha256")=32 AND octet_length("IssuerSha256")=32 AND octet_length("SubjectSha256")=32)<>1 THEN RAISE EXCEPTION 'Bootstrap completion witness mismatch.'; END IF;
        END $assert$;
        """;

    private const string Part2Assertions = """
        DO $assert$
        BEGIN
          IF (SELECT count(*) FROM advance.roles)<>45 THEN RAISE EXCEPTION 'Expected 45 roles.'; END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions)<>1219 THEN RAISE EXCEPTION 'Expected 1219 permissions.'; END IF;
          IF (SELECT count(*) FROM advance.employee_company_assignments)<>93 THEN RAISE EXCEPTION 'Expected 93 company assignments.'; END IF;
          IF (SELECT count(*) FROM advance.employee_department_assignments)<>586 THEN RAISE EXCEPTION 'Expected 586 department assignments.'; END IF;
          IF (SELECT count(*) FROM advance.employee_role_assignments)<>99 THEN RAISE EXCEPTION 'Expected 99 role assignments.'; END IF;
          IF (SELECT count(*) FROM advance.employee_operational_scopes)<>398 THEN RAISE EXCEPTION 'Expected 398 operational scopes.'; END IF;
          IF (SELECT count(*) FROM advance.employee_identity_mappings)<>0 THEN RAISE EXCEPTION 'Fresh chain must have no identity mappings before bootstrap.'; END IF;
          IF (SELECT count(*) FROM advance.purchase_transaction_approval_policies WHERE "IsActive")<>6 THEN RAISE EXCEPTION 'Expected six active approval policies.'; END IF;
          IF (SELECT count(*) FROM advance.purchase_approval_route_settings WHERE "IsActive")<>6 THEN RAISE EXCEPTION 'Expected six active approval route settings.'; END IF;
          IF (SELECT count(*) FROM advance.purchase_approval_workflow_steps WHERE "IsActive")<>10 THEN RAISE EXCEPTION 'Expected ten active approval workflow steps.'; END IF;
          IF (SELECT count(*) FROM advance.department_approval_mappings WHERE "IsActive")<>42 THEN RAISE EXCEPTION 'Expected 42 active department approval mappings.'; END IF;
        END $assert$;
        """;

    private const string FoundationCatalogueAssertions = """
        DO $assert$
        DECLARE value_count integer;
        BEGIN
          SELECT count(*) INTO value_count FROM pg_catalog.pg_tables WHERE schemaname='advance';
          IF value_count<>93 THEN RAISE EXCEPTION 'expected 93 advance tables, found %',value_count; END IF;

          SELECT count(*) INTO value_count FROM advance.companies
           WHERE ("Code","LegalName","EntityType") IN
             (('SESS_PROPRIETORSHIP','Sri Easwari Scientific Solution','PROPRIETORSHIP'),
              ('SESS_PVT_LTD','Sri Easwari Scientific Solution Private Limited','PRIVATE_LIMITED'));
          IF value_count<>2 THEN RAISE EXCEPTION 'company seed mismatch'; END IF;

          SELECT count(*) INTO value_count FROM advance.company_gst_registrations
           WHERE ("Gstin","StateCode") IN (('33APRPA5532K1ZU','33'),('33ABACS5491H1ZA','33'));
          IF value_count<>2 THEN RAISE EXCEPTION 'GST seed mismatch'; END IF;

          SELECT count(*) INTO value_count FROM advance.departments WHERE "IsActive";
          IF value_count<>21 THEN RAISE EXCEPTION 'expected 21 active departments, found %',value_count; END IF;
          SELECT count(*) INTO value_count FROM advance.departments WHERE "IsActive" AND "ParentDepartmentId" IS NULL;
          IF value_count<>17 THEN RAISE EXCEPTION 'expected 17 top-level departments, found %',value_count; END IF;
          SELECT count(*) INTO value_count FROM advance.departments WHERE "Code"='CALIBRATION' AND "IsActive" AND "ParentDepartmentId" IS NULL;
          IF value_count<>1 THEN RAISE EXCEPTION 'Calibration department seed mismatch'; END IF;

          SELECT count(*) INTO value_count FROM advance.employee_company_assignments WHERE "AssignmentType"='PAYROLL' AND "IsActive";
          IF value_count<>39 THEN RAISE EXCEPTION 'expected 39 PAYROLL assignments, found %',value_count; END IF;
          SELECT count(*) INTO value_count FROM advance.employee_company_assignments WHERE "AssignmentType"='WORK';
          IF value_count<>0 THEN RAISE EXCEPTION 'migration must not seed WORK assignments'; END IF;
          SELECT count(*) INTO value_count FROM advance.employee_department_assignments;
          IF value_count<>186 THEN RAISE EXCEPTION 'expected 186 department assignments, found %',value_count; END IF;
          SELECT count(*) INTO value_count FROM advance.employee_department_assignments WHERE "IsPrimary";
          IF value_count<>39 THEN RAISE EXCEPTION 'expected 39 primary department assignments, found %',value_count; END IF;
          SELECT count(*) INTO value_count FROM advance.employee_department_assignments WHERE NOT "IsPrimary";
          IF value_count<>147 THEN RAISE EXCEPTION 'expected 147 secondary department assignments, found %',value_count; END IF;

          SELECT count(*) INTO value_count
          FROM advance.employees e JOIN advance.departments d ON d."Id"=e."DepartmentId"
          JOIN advance.designations g ON g."Id"=e."DesignationId"
          WHERE e."EmployeeCode"='SESS-012' AND d."Code"='PURCHASE' AND g."Code"='PURCHASE_EXECUTIVE';
          IF value_count<>1 THEN RAISE EXCEPTION 'SESS-012 primary mapping mismatch'; END IF;
          SELECT count(*) INTO value_count
          FROM advance.employee_department_assignments a
          JOIN advance.employee_company_assignments c ON c."Id"=a."EmployeeCompanyAssignmentId"
          JOIN advance.departments d ON d."Id"=a."DepartmentId"
          WHERE c."EmployeeCode" IN ('SESS-012','SESS-014') AND NOT a."IsPrimary"
            AND ((c."EmployeeCode"='SESS-012' AND d."Code"='STORES') OR (c."EmployeeCode"='SESS-014' AND d."Code"='PURCHASE'));
          IF value_count<>2 THEN RAISE EXCEPTION 'Stores/Purchase secondary pair mismatch'; END IF;

          SELECT count(*) INTO value_count FROM information_schema.columns
          WHERE table_schema='advance' AND table_name='items' AND column_name='ItemType' AND is_nullable='NO' AND column_default IS NULL;
          IF value_count<>1 THEN RAISE EXCEPTION 'ItemType must be NOT NULL without a default'; END IF;
          SELECT count(*) INTO value_count FROM information_schema.columns
          WHERE table_schema='advance' AND table_name='items' AND column_name='IsReturnable' AND is_nullable='NO' AND column_default='false';
          IF value_count<>1 THEN RAISE EXCEPTION 'IsReturnable default contract mismatch'; END IF;
          SELECT count(*) INTO value_count FROM information_schema.columns
          WHERE table_schema='advance' AND table_name='items' AND column_name='MaterialType';
          IF value_count<>1 THEN RAISE EXCEPTION 'MaterialType was removed unexpectedly'; END IF;

          SELECT count(*) INTO value_count FROM advance.organization_policies
           WHERE "CompanyId"='70000000-0000-0000-0000-000000000001' AND "OrganizationId"='SESS_PVT_LTD';
          IF value_count<>2 THEN RAISE EXCEPTION 'organization policy mapping mismatch'; END IF;
          SELECT count(*) INTO value_count FROM advance.purchase_transaction_approval_policies
           WHERE "CompanyId"='70000000-0000-0000-0000-000000000001' AND "OrganizationId"='SESS_PVT_LTD';
          IF value_count<>3 THEN RAISE EXCEPTION 'approval policy mapping mismatch'; END IF;

          IF NOT EXISTS (
            SELECT 1 FROM pg_catalog.pg_constraint c
            JOIN pg_catalog.pg_class t ON t.oid=c.conrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
            WHERE n.nspname='advance' AND t.relname='purchase_orders' AND c.contype='f'
              AND pg_get_constraintdef(c.oid) LIKE 'FOREIGN KEY ("CompanyId", "OrganizationId")%')
          THEN RAISE EXCEPTION 'purchase_orders composite company/code FK missing'; END IF;

          IF NOT EXISTS (
            SELECT 1 FROM pg_catalog.pg_constraint c
            JOIN pg_catalog.pg_class t ON t.oid=c.conrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
            WHERE n.nspname='advance' AND t.relname='purchase_requisition_lines' AND c.contype='f'
              AND pg_get_constraintdef(c.oid) LIKE 'FOREIGN KEY ("AssetId")%')
          THEN RAISE EXCEPTION 'MachineReference AssetId FK missing'; END IF;

          IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_attribute a
            JOIN pg_catalog.pg_class t ON t.oid=a.attrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
            WHERE n.nspname='advance' AND a.attname='CompanyId' AND a.attnotnull
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_index i
                WHERE i.indrelid=t.oid AND (i.indkey::smallint[])[0]=a.attnum))
          THEN RAISE EXCEPTION 'required company scope lacks a CompanyId-leading index'; END IF;
        END $assert$;
        """;

    private static IDisposable DevelopmentBootstrapEnvironment(string connectionString) => new EnvironmentVariables(
        ("DOTNET_ENVIRONMENT", "Development"),
        (InstallerCommand.DevelopmentBootstrapSetting, "true"),
        ("ConnectionStrings__NexaErpDevelopmentBootstrap", connectionString),
        ("NexaErp__ExpectedDatabase", "advance_parser"));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private static ProcessResult RunDotnet(string workingDirectory, string? developmentSetting, params string[] arguments)
    {
        var info = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (developmentSetting is null) info.Environment.Remove(InstallerCommand.DevelopmentBootstrapSetting);
        else info.Environment[InstallerCommand.DevelopmentBootstrapSetting] = developmentSetting;
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Cannot start dotnet.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromMinutes(3))) { process.Kill(true); throw new TimeoutException("dotnet timed out."); }
        Task.WaitAll(standardOutput, standardError);
        return new ProcessResult(process.ExitCode, standardOutput.Result + standardError.Result);
    }

    private static void Require(ProcessResult result, string operation)
    {
        if (result.Code != 0) throw new InvalidOperationException($"{operation} failed ({result.Code}):\n{result.Output}");
    }

    private static string FindPostgreSqlBin()
    {
        var configured = Environment.GetEnvironmentVariable("ADVANCE_POSTGRES_BIN");
        var root = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL")
            : "/usr/lib/postgresql";
        var candidates = string.IsNullOrWhiteSpace(configured) ? Array.Empty<string>() : new[] { configured };
        if (Directory.Exists(root)) candidates = candidates.Concat(Directory.GetDirectories(root)
            .OrderDescending().Select(path => Path.Combine(path, "bin"))).ToArray();
        var suffix = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
        return candidates.FirstOrDefault(path => new[] { "initdb", "pg_ctl", "psql", "createdb" }
                   .All(name => File.Exists(Path.Combine(path, name + suffix))))
               ?? throw new InvalidOperationException("Set ADVANCE_POSTGRES_BIN to a real PostgreSQL bin directory.");
    }

    private sealed class DisposablePostgreSql : IDisposable
    {
        private readonly string _bin;
        private readonly string _root;
        private readonly string _data;
        private readonly int _port;
        private bool _started;

        private DisposablePostgreSql(string bin)
        {
            _bin = bin;
            _root = Path.Combine(Path.GetTempPath(), $"advance-postgresql-parser-{Guid.NewGuid():N}");
            _data = Path.Combine(_root, "data");
            _port = ReservePort();
            Directory.CreateDirectory(_root);
        }

        public static DisposablePostgreSql Start(string bin)
        {
            var server = new DisposablePostgreSql(bin);
            try
            {
                server.Require(server.Run("initdb", "-D", server._data, "--username=postgres", "--auth=trust",
                    "--encoding=UTF8", "--no-locale"), "initdb");
                server.Require(server.Run("pg_ctl", "-D", server._data, "-l", Path.Combine(server._root, "postgres.log"),
                    "-o", $"-h 127.0.0.1 -p {server._port} -c fsync=off -c synchronous_commit=off",
                    "-w", "start"), "pg_ctl start");
                server._started = true;
                server.Require(server.Run("createdb", "--host", "127.0.0.1", "--port",
                    server._port.ToString(System.Globalization.CultureInfo.InvariantCulture), "--username", "postgres",
                    "advance_parser"), "createdb");
                return server;
            }
            catch
            {
                server.Dispose();
                throw;
            }
        }

        public void AssertRejected(string name, string sql, string? expectedMessage = null)
        {
            var result = Psql(name, sql);
            Assert.NotEqual(0, result.Code);
            Assert.Contains("ERROR:", result.Output, StringComparison.OrdinalIgnoreCase);
            if (expectedMessage is not null)
                Assert.True(result.Output.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase), result.Output);
        }

        public void Execute(string name, string sql) => Require(Psql(name, sql), name);

        public string ConnectionString => $"Host=127.0.0.1;Port={_port};Database=advance_parser;Username=postgres;Pooling=false";

        private Result Psql(string name, string sql)
        {
            var file = Path.Combine(_root, name);
            File.WriteAllText(file, sql);
            return Run("psql", "-X", "--quiet", "--set", "ON_ERROR_STOP=1", "--host", "127.0.0.1", "--port",
                _port.ToString(System.Globalization.CultureInfo.InvariantCulture), "--username", "postgres",
                "--dbname", "advance_parser", "--file", file);
        }

        public void Dispose()
        {
            if (_started) Run("pg_ctl", "-D", _data, "-m", "immediate", "-w", "stop");
            var root = Path.GetFullPath(_root);
            var temp = Path.GetFullPath(Path.GetTempPath());
            if (root.StartsWith(temp, StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(root).StartsWith("advance-postgresql-parser-", StringComparison.Ordinal) &&
                Directory.Exists(root)) Directory.Delete(root, true);
        }

        private Result Run(string executable, params string[] arguments)
        {
            var suffix = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
            var capture = executable != "pg_ctl" || !arguments.Contains("start", StringComparer.Ordinal);
            var info = new ProcessStartInfo(Path.Combine(_bin, executable + suffix))
            {
                RedirectStandardOutput = capture,
                RedirectStandardError = capture,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var argument in arguments) info.ArgumentList.Add(argument);
            using var process = Process.Start(info) ?? throw new InvalidOperationException($"Cannot start {executable}.");
            var output = capture ? process.StandardOutput.ReadToEndAsync() : Task.FromResult(string.Empty);
            var error = capture ? process.StandardError.ReadToEndAsync() : Task.FromResult(string.Empty);
            if (!process.WaitForExit(TimeSpan.FromMinutes(3)))
            {
                process.Kill(true);
                throw new TimeoutException($"{executable} timed out.");
            }
            Task.WaitAll(output, error);
            return new Result(process.ExitCode, output.Result + error.Result);
        }

        private void Require(Result result, string operation) =>
            Assert.True(result.Code == 0,
                $"PostgreSQL rejected {operation}:{Environment.NewLine}{Tail(result.Output)}");

        private static string Tail(string value) => value.Length <= 12000 ? value : value[^12000..];

        private static int ReservePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private sealed record Result(int Code, string Output);
    }

    private sealed class EnvironmentVariables : IDisposable
    {
        private readonly (string Name, string? Value)[] _original;

        public EnvironmentVariables(params (string Name, string Value)[] values)
        {
            _original = values.Select(value => (value.Name, Environment.GetEnvironmentVariable(value.Name))).ToArray();
            foreach (var value in values) Environment.SetEnvironmentVariable(value.Name, value.Value);
        }

        public void Dispose()
        {
            foreach (var value in _original) Environment.SetEnvironmentVariable(value.Name, value.Value);
        }
    }

    private sealed record ProcessResult(int Code, string Output);
}
