using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void Opening_stock_governance_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        const string target = "20260912064200_GovernedOpeningStockThreeActorCeremony";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, target);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("opening-stock-governance-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("opening-stock-governance-up.sql",
            migrator.GenerateScript(predecessor, target) + OpeningStockAssertions);
        server.Execute("opening-stock-governance-down.sql", migrator.GenerateScript(target, predecessor));
        server.Execute("opening-stock-governance-reapply.sql",
            migrator.GenerateScript(predecessor, target) + OpeningStockAssertions);
    }

    [Fact]
    public async Task Opening_stock_posts_one_landed_layer_and_available_movement_after_three_distinct_actors()
    {
        const string target = "20260912064200_GovernedOpeningStockThreeActorCeremony";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, target);
        var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("opening-stock-witness-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("opening-stock-witness-trial-master-data.sql",
            "\\set expected_database advance_parser\n"
            + File.ReadAllText(Find("database", "postgresql", "trial-master-data-apply.sql")));
        server.Execute("opening-stock-witness-approved-item.sql", """
            UPDATE advance.items SET "Status"='Active',"ApprovalStatus"='Approved',"IsActive"=true
            WHERE "CreatedBy"='TRIAL_DATA';
            DO $assert$ BEGIN
              IF (SELECT count(*) FROM advance.items WHERE "CreatedBy"='TRIAL_DATA'
                    AND "ApprovalStatus"='Approved' AND "IsActive")<>20 THEN
                RAISE EXCEPTION 'Canonical witness item was not approved and active.';
              END IF;
            END $assert$;
            """);
        const string password = "opening-stock-runtime-123456789";
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, password);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("opening-stock-witness-up.sql", migrator.GenerateScript(predecessor, target));
        server.Execute("opening-stock-witness.sql", WitnessSql);
        server.Execute("opening-stock-report-schema.sql",migrator.GenerateScript(target,migrations[^1]));
        server.Execute("opening-stock-report-login.sql","""UPDATE advance.employees SET "LoginEnabled"=true WHERE "EmployeeCode" IN ('SESS-01','SESS-14');""");
        await using var ownerDb = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options);
        var director = await ownerDb.Employees.SingleAsync(row => row.EmployeeCode == "SESS-01");
        var company = await ownerDb.Companies.SingleAsync(row => row.Code == "SESS_PVT_LTD");
        var assignments = await ownerDb.EmployeeRoleAssignments.Include(row => row.Role)
            .Where(row => row.EmployeeId == director.Id && row.CompanyId == company.Id && row.EffectiveTo == null).ToListAsync();
        var actor = new ReportWitnessUser(director.Id,assignments.Select(row =>
            new SESS.NexaERP.Application.Common.EffectiveRoleAssignment(row.Id,row.Role!.Code,row.AssignmentType)).ToArray());
        var runtimeConnection = new Npgsql.NpgsqlConnectionStringBuilder(server.ConnectionString) { Username = "nexa_erp_runtime" }.ConnectionString;
        await using var reportDb = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(runtimeConnection).Options);
        var reports = new SESS.NexaERP.Infrastructure.Reporting.EfCompanyReportService(reportDb,actor);
        var postingDate = new DateOnly(2027,3,31); // The existing ceremony's fiscal-period end is its ledger date.
        var beforeOpening = await reports.GetAsync("stock-balance",new(ToDate:postingDate.AddDays(-1)),default);
        Assert.Empty(beforeOpening.Rows);
        var balance = await reports.GetAsync("stock-balance",new(ToDate:postingDate),default);
        Assert.Equal(10m,Assert.Single(balance.Totals).GetProperty("quantity").GetDecimal());
        var detail = await reports.GetAsync("stock-balance",new(ToDate:postingDate,Mode:"details"),default);
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null,Assert.Single(detail.Rows).GetProperty("openingStockLineId").ValueKind);
        var roll = await reports.GetAsync("movement-roll-forward",new(new DateOnly(2026,4,1),postingDate),default);
        var total = Assert.Single(roll.Totals);
        Assert.Equal(10m,total.GetProperty("openingIntroduced").GetDecimal());
        Assert.Equal(0m,total.GetProperty("receipts").GetDecimal());
        Assert.Equal(10m,total.GetProperty("adjustments").GetDecimal());
        Assert.Equal(10m,total.GetProperty("closing").GetDecimal());
        var accounts = await ownerDb.Employees.SingleAsync(row=>row.EmployeeCode=="SESS-14");
        var accountsAssignments = await ownerDb.EmployeeRoleAssignments.Include(row=>row.Role)
            .Where(row=>row.EmployeeId==accounts.Id&&row.CompanyId==company.Id&&row.EffectiveTo==null).ToListAsync();
        var accountsActor = new ReportWitnessUser(accounts.Id,accountsAssignments.Select(row=>
            new SESS.NexaERP.Application.Common.EffectiveRoleAssignment(row.Id,row.Role!.Code,row.AssignmentType)).ToArray());
        var valuation = new SESS.NexaERP.Infrastructure.Reporting.EfCompanyReportService(reportDb,accountsActor);
        Assert.Empty((await ObserveSingleReportCommand(()=>valuation.GetAsync("fifo-valuation",new(ToDate:postingDate.AddDays(-1)),default))).Rows);
        var fifo=await ObserveSingleReportCommand(()=>valuation.GetAsync("fifo-valuation",new(ToDate:postingDate),default));
        var fifoTotal=Assert.Single(fifo.Totals);
        Assert.Equal(10m,fifoTotal.GetProperty("quantity").GetDecimal());
        Assert.Equal(250m,fifoTotal.GetProperty("value").GetDecimal());
        Assert.Equal("INR",fifoTotal.GetProperty("currency").GetString());
        Assert.Equal("OPENING_LANDED",Assert.Single(fifo.Rows).GetProperty("costBasis").GetString());
        var fifoDetail=await valuation.GetAsync("fifo-valuation",new(ToDate:postingDate,Mode:"details",
            Group:fifoTotal.GetProperty("group").GetRawText(),Metric:"value"),default);
        var layer=Assert.Single(fifoDetail.Rows);
        Assert.Equal(25m,layer.GetProperty("unitCost").GetDecimal());
        Assert.Equal(250m,layer.GetProperty("value").GetDecimal());
        Assert.Equal(0,layer.GetProperty("ageDays").GetInt32());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null,layer.GetProperty("openingStockLineId").ValueKind);
        foreach(var (days,bucket) in new[]{(30,"0–30 days"),(31,"31–90 days"),(90,"31–90 days"),(91,"91–180 days"),
            (180,"91–180 days"),(181,"181–365 days"),(365,"181–365 days"),(366,"Over 365 days")})
        {
            var aged=await valuation.GetAsync("fifo-valuation",new(ToDate:postingDate.AddDays(days)),default);
            Assert.Equal(bucket,Assert.Single(aged.Rows).GetProperty("ageBucket").GetString());
            Assert.Equal(250m,Assert.Single(aged.Totals).GetProperty("value").GetDecimal());
        }
        var download=await ObserveSingleReportCommand(()=>valuation.ExportAsync("fifo-valuation",new(ToDate:postingDate),default));
        using var fifoStream=new MemoryStream(download.Content);
        using var fifoWorkbook=new ClosedXML.Excel.XLWorkbook(fifoStream);
        Assert.Equal(250m,fifoWorkbook.Worksheet("Totals").Cell(2,5).GetValue<decimal>());
        Assert.True(fifoWorkbook.Worksheet("Totals").Cell(2,5).HasHyperlink);


    }

    [Fact]
    public void Opening_stock_contract_keeps_three_actors_controlled_posting_and_immutable_fifo()
    {
        var sql = File.ReadAllText(Find("src", "SESS.NexaERP.Infrastructure", "Persistence",
            "Migrations", "OpeningStockSql.cs"));
        Assert.Contains("three separate employees", sql, StringComparison.Ordinal);
        Assert.Contains("Opening Stock is refused because this company already has stock movements", sql,
            StringComparison.Ordinal);
        Assert.Contains("'OPENING_BALANCE'", sql, StringComparison.Ordinal);
        Assert.Contains("'OPENING_LANDED'", sql, StringComparison.Ordinal);
        Assert.Contains("stage_opening_stock_import_line", sql, StringComparison.Ordinal);
        Assert.Contains("OpeningStock.Import", sql, StringComparison.Ordinal);
        Assert.Contains("REVOKE ALL ON advance.opening_stock_import_staging_lines", sql,
            StringComparison.Ordinal);
        Assert.Contains("expected_type:='OPENING_BALANCE'", OpeningStockSql.Up,
            StringComparison.Ordinal);
        Assert.Contains("Opening Stock movement source does not match its batch", OpeningStockSql.Up,
            StringComparison.Ordinal);
        Assert.Contains("Opening Stock posting does not reconcile", OpeningStockSql.Up,
            StringComparison.Ordinal);
    }

    private const string OpeningStockAssertions = """

        DO $assert$
        BEGIN
          IF to_regclass('advance.opening_stocks') IS NULL
             OR to_regclass('advance.opening_stock_lines') IS NULL
             OR to_regclass('advance.opening_stock_events') IS NULL
             OR to_regclass('advance.opening_stock_import_staging_lines') IS NULL THEN
            RAISE EXCEPTION 'Opening Stock tables are missing.';
          END IF;
          IF to_regprocedure('advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text)') IS NULL
             OR to_regprocedure('advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text)') IS NULL
             OR to_regprocedure('advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)') IS NULL
             OR to_regprocedure('advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)') IS NULL THEN
            RAISE EXCEPTION 'Opening Stock controlled functions are missing.';
          END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions p
                JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
                JOIN advance.roles r ON r."Id"=p."RoleId"
                WHERE d."PageKey"='stores.opening-stock'
                  AND ((r."Code"='STORES_MANAGER' AND p."CanCreate")
                    OR (r."Code"='ACCOUNTS_MANAGER' AND p."CanVerify")
                    OR (r."Code"='TECHNICAL_DIRECTOR' AND p."CanApprove")))<>3 THEN
            RAISE EXCEPTION 'Opening Stock three-actor page grants are wrong.';
          END IF;
        END $assert$;
        """;

    private const string WitnessSql = """
        SET SESSION AUTHORIZATION nexa_erp_runtime;
        DO $witness$
        DECLARE company_id uuid; stores_actor uuid; stores_assignment uuid; item_id uuid;
                warehouse_id uuid; bin_id uuid; staged_id uuid; command_id uuid;
                batch_id uuid:='f1300000-0000-0000-0000-000000000001';
        BEGIN
          SELECT "Id" INTO company_id FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
          SELECT e."Id",a."Id" INTO stores_actor,stores_assignment
          FROM advance.employee_role_assignments a JOIN advance.employees e ON e."Id"=a."EmployeeId"
          JOIN advance.roles r ON r."Id"=a."RoleId"
          WHERE a."CompanyId"=company_id AND e."EmployeeCode"='SESS-41' AND r."Code"='STORES_MANAGER'
            AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE);
          SELECT "Id" INTO item_id FROM advance.items WHERE "CreatedBy"='TRIAL_DATA' ORDER BY "Id" LIMIT 1;
          SELECT "Id" INTO warehouse_id FROM advance.warehouses WHERE "CreatedBy"='TRIAL_DATA'
            AND "CompanyId"=company_id ORDER BY "Id" LIMIT 1;
          SELECT "Id" INTO bin_id FROM advance.rack_bins WHERE "CreatedBy"='TRIAL_DATA'
            AND "CompanyId"=company_id AND "WarehouseId"=warehouse_id
            AND "MaterialCondition"='AVAILABLE' ORDER BY "Id" LIMIT 1;
          command_id:=advance.register_command_request('SESS_PVT_LTD','OpeningStock.Import',
            decode(repeat('11',32),'hex'),decode(repeat('12',32),'hex'),stores_actor,
            'https://opening.test','SESS-41','STORES_MANAGER',stores_assignment);
          staged_id:=advance.stage_opening_stock_import_line(company_id,'OPEN-001',item_id,
            warehouse_id,bin_id,'OPEN-LOT-001',NULL,10,25,stores_actor,'STORES_MANAGER',
            stores_assignment,'FULL','SESS-41');
          INSERT INTO advance.master_import_batches
            ("Id","MasterKey","TemplateVersion","CompanyId","ImportMode","Status","OriginalFileName",
             "FileSizeBytes","FileSha256","IdempotencyKey","RequestFingerprint","UploadedByEmployeeId",
             "UploadedByEmployeeCode","OperationalRoleCode","UploadedAt","CompletedAt","RetentionExpiresAt",
             "TotalRows","ValidRows","InvalidRows","CreatedRows","UpdatedRows","UnchangedRows","RejectedRows",
             "NotImportedRows","CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES(batch_id,'opening-stock',1,company_id,'REJECT_ENTIRE_FILE','COMPLETED','opening-stock.xlsx',
             100,repeat('a',64),'opening-import',repeat('b',64),stores_actor,'SESS-41','STORES_MANAGER',
             clock_timestamp(),clock_timestamp(),clock_timestamp()+interval '90 days',
             1,1,0,1,0,0,0,0,'f1310000-0000-0000-0000-000000000001',clock_timestamp(),'SESS-41',0);
          INSERT INTO advance.master_import_row_results
            ("Id","ImportBatchId","SourceRowNumber","BusinessCode","NormalizedBusinessCode","IntendedAction",
             "Outcome","ErrorsJson","ResultRecordId","ResultVersion","ProcessedAt","CreatedAt","CreatedBy","Version")
          VALUES('f1320000-0000-0000-0000-000000000001',batch_id,2,'OPEN-001','OPEN-001','CREATE',
             'CREATED','[]',staged_id,0,clock_timestamp(),clock_timestamp(),'SESS-41',0);
          PERFORM advance.commit_command_receipt(command_id,decode(repeat('13',32),'hex'),'{}',gen_random_uuid());
        END $witness$;

        DO $count$
        DECLARE company_id uuid; actor_id uuid; assignment_id uuid; command_id uuid; opening_id uuid;
        BEGIN
          SELECT "Id" INTO company_id FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
          SELECT e."Id",a."Id" INTO actor_id,assignment_id FROM advance.employee_role_assignments a
          JOIN advance.employees e ON e."Id"=a."EmployeeId" JOIN advance.roles r ON r."Id"=a."RoleId"
          WHERE a."CompanyId"=company_id AND e."EmployeeCode"='SESS-41' AND r."Code"='STORES_MANAGER'
            AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE);
          command_id:=advance.register_command_request('SESS_PVT_LTD','OpeningStock.RecordCount',
            decode(repeat('21',32),'hex'),decode(repeat('22',32),'hex'),actor_id,
            'https://opening.test','SESS-41','STORES_MANAGER',assignment_id);
          SELECT x."OpeningStockId" INTO opening_id FROM advance.record_opening_stock_count(
            company_id,'f1300000-0000-0000-0000-000000000001','2026-04-01','2027-03-31',
            'Physical count','count-key',repeat('c',64),actor_id,'STORES_MANAGER',assignment_id,'FULL','SESS-41') x;
          PERFORM advance.commit_command_receipt(command_id,decode(repeat('23',32),'hex'),'{}',gen_random_uuid());
        END $count$;

        DO $value$
        DECLARE company_id uuid; actor_id uuid; assignment_id uuid; command_id uuid; opening_id uuid;
        BEGIN
          SELECT "Id" INTO company_id FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
          SELECT "Id" INTO opening_id FROM advance.opening_stocks WHERE "CompanyId"=company_id;
          SELECT e."Id",a."Id" INTO actor_id,assignment_id FROM advance.employee_role_assignments a
          JOIN advance.employees e ON e."Id"=a."EmployeeId" JOIN advance.roles r ON r."Id"=a."RoleId"
          WHERE a."CompanyId"=company_id AND e."EmployeeCode"='SESS-14' AND r."Code"='ACCOUNTS_MANAGER'
            AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE);
          command_id:=advance.register_command_request('SESS_PVT_LTD','OpeningStock.ConfirmValue',
            decode(repeat('31',32),'hex'),decode(repeat('32',32),'hex'),actor_id,
            'https://opening.test','SESS-14','ACCOUNTS_MANAGER',assignment_id);
          PERFORM advance.confirm_opening_stock_value(company_id,opening_id,0,'Rate checked',
            'value-key',repeat('d',64),actor_id,'ACCOUNTS_MANAGER',assignment_id,'FULL','SESS-14');
          PERFORM advance.commit_command_receipt(command_id,decode(repeat('33',32),'hex'),'{}',gen_random_uuid());
        END $value$;

        DO $authorize$
        DECLARE company_id uuid; actor_id uuid; assignment_id uuid; command_id uuid; opening_id uuid;
        BEGIN
          SELECT "Id" INTO company_id FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
          SELECT "Id" INTO opening_id FROM advance.opening_stocks WHERE "CompanyId"=company_id;
          SELECT e."Id",a."Id" INTO actor_id,assignment_id FROM advance.employee_role_assignments a
          JOIN advance.employees e ON e."Id"=a."EmployeeId" JOIN advance.roles r ON r."Id"=a."RoleId"
          WHERE a."CompanyId"=company_id AND e."EmployeeCode"='SESS-01' AND r."Code"='TECHNICAL_DIRECTOR'
            AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE);
          command_id:=advance.register_command_request('SESS_PVT_LTD','OpeningStock.Authorize',
            decode(repeat('41',32),'hex'),decode(repeat('42',32),'hex'),actor_id,
            'https://opening.test','SESS-01','TECHNICAL_DIRECTOR',assignment_id);
          PERFORM advance.authorize_opening_stock(company_id,opening_id,1,'Opening approved',
            'authorize-key',repeat('e',64),actor_id,'TECHNICAL_DIRECTOR',assignment_id,'FULL','SESS-01');
          PERFORM advance.commit_command_receipt(command_id,decode(repeat('43',32),'hex'),'{}',gen_random_uuid());
        END $authorize$;
        RESET SESSION AUTHORIZATION;

        DO $assert$
        BEGIN
          IF (SELECT count(*) FROM advance.opening_stocks WHERE "Status"='POSTED'
                AND num_nonnulls("CountedByEmployeeId","ValuedByEmployeeId","AuthorizedByEmployeeId")=3
                AND "CountedByEmployeeId"<>"ValuedByEmployeeId"
                AND "CountedByEmployeeId"<>"AuthorizedByEmployeeId"
                AND "ValuedByEmployeeId"<>"AuthorizedByEmployeeId")<>1 THEN
            RAISE EXCEPTION 'Opening Stock did not retain three distinct actors.';
          END IF;
          IF (SELECT count(*) FROM advance.stock_movements WHERE "MovementType"='OPENING_BALANCE'
                AND "MovementLeg"='RECEIPT_IN' AND "ConditionCode"='AVAILABLE'
                AND "QuantityIn"=10 AND "QuantityOut"=0)<>1 THEN
            RAISE EXCEPTION 'Opening Stock did not post exactly one AVAILABLE receipt.';
          END IF;
          IF (SELECT count(*) FROM advance.fifo_inventory_cost_layers WHERE "CostBasis"='OPENING_LANDED'
                AND "QuantityReceived"=10 AND "UnitCost"=25 AND "LayerValue"=250)<>1 THEN
            RAISE EXCEPTION 'Opening Stock did not create its landed FIFO layer.';
          END IF;
          IF (SELECT count(*) FROM advance.opening_stock_events)<>3 THEN
            RAISE EXCEPTION 'Opening Stock event history is incomplete.';
          END IF;
        END $assert$;
        """;

    private static string Find(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
            directory = directory.Parent;
        return Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException(), Path.Combine(parts));
    }
}
