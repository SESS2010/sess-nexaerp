using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

/// <summary>
/// Opening stock Option A (26 Sep): a wrong COUNTED or VALUED record is withdrawn with a reason by the
/// Stores Manager who counted it or by the Technical Director; the period is then free for a recount
/// from a new import. Run at the latest migration, after the installer's principal provisioning.
/// </summary>
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task A_wrong_opening_count_is_withdrawn_and_the_period_recounted()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        const string password = "opening-withdraw-runtime-123456789";
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, password);
        server.Execute("withdraw-schema.sql", migrator.GenerateScript("0", model.Database.GetMigrations().Last()));
        server.Execute("withdraw-trial.sql", "\\set expected_database advance_parser\n"
            + File.ReadAllText(Find("database", "postgresql", "trial-master-data-apply.sql")));
        server.Execute("withdraw-items.sql", """UPDATE advance.items SET "Status"='Active',"ApprovalStatus"='Approved',"IsActive"=true WHERE "CreatedBy"='TRIAL_DATA';""");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("withdraw-acl.sql", """
            DO $acl$ BEGIN
              IF NOT has_function_privilege('nexa_erp_runtime','advance.withdraw_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
                 OR has_function_privilege('nexa_erp_bootstrap','advance.withdraw_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
                 OR NOT (SELECT prosecdef FROM pg_proc WHERE oid='advance.withdraw_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)'::regprocedure)
              THEN RAISE EXCEPTION 'withdraw_opening_stock is not provisioned for runtime only.'; END IF;
            END $acl$;
            """);

        server.Execute("withdraw-import-1.sql", ImportSql(1));
        server.Execute("withdraw-count-1.sql", CountSql(1));
        server.AssertRejected("withdraw-recount-refused.sql", ImportSql(3) + CountSql(3), "Opening Stock already exists for this company and period");
        server.AssertRejected("withdraw-by-accounts.sql", WithdrawSql("SESS-14", "ACCOUNTS_MANAGER", 0, "w-accounts"), "requires current FULL or TEMPORARY STORES_MANAGER or TECHNICAL_DIRECTOR");
        server.AssertRejected("withdraw-no-reason.sql", WithdrawSql("SESS-41", "STORES_MANAGER", 0, "w-empty", reason: " "), "A reason is required to withdraw Opening Stock");
        server.AssertRejected("withdraw-stale.sql", WithdrawSql("SESS-41", "STORES_MANAGER", 5, "w-stale"), "stale version");
        server.Execute("withdraw-by-counter.sql", WithdrawSql("SESS-41", "STORES_MANAGER", 0, "w-counter"));
        server.Execute("withdraw-replay.sql", WithdrawSql("SESS-41", "STORES_MANAGER", 0, "w-counter", expectReplay: true));
        server.AssertRejected("withdraw-twice.sql", WithdrawSql("SESS-01", "TECHNICAL_DIRECTOR", 1, "w-again"), "This Opening Stock is WITHDRAWN, so it cannot be withdrawn");

        // The period is free: a corrected import is counted, valued, and withdrawn again by the Technical Director.
        server.Execute("withdraw-import-2.sql", ImportSql(2));
        server.Execute("withdraw-count-2.sql", CountSql(2));
        server.Execute("withdraw-value-2.sql", ValueSql());
        server.Execute("withdraw-by-director.sql", WithdrawSql("SESS-01", "TECHNICAL_DIRECTOR", 1, "w-director", batch: 2));
        server.Execute("withdraw-assert.sql", """
            DO $assert$ BEGIN
              IF (SELECT count(*) FROM advance.opening_stocks WHERE "Status"='WITHDRAWN')<>2 OR (SELECT count(*) FROM advance.opening_stocks)<>2 THEN
                RAISE EXCEPTION 'Both counts should be WITHDRAWN.'; END IF;
              IF (SELECT count(*) FROM advance.opening_stock_events WHERE "Action"='WITHDRAW' AND "ToStatus"='WITHDRAWN')<>2
                 OR NOT EXISTS(SELECT 1 FROM advance.opening_stock_events WHERE "Action"='WITHDRAW' AND "FromStatus"='VALUED' AND "ActorRoleCode"='TECHNICAL_DIRECTOR' AND "Reason"='Wrong rack in the workbook')
              THEN RAISE EXCEPTION 'The withdrawal events are not as expected.'; END IF;
              IF EXISTS(SELECT 1 FROM advance.stock_movements) THEN RAISE EXCEPTION 'A withdrawal must not move stock.'; END IF;
            END $assert$;
            """);

        // Down refuses once WITHDRAWN evidence exists.
        var migrations = model.Database.GetMigrations().ToArray();
        var withdraw = Array.IndexOf(migrations, "20260926130000_OpeningStockWithdraw");
        Assert.True(withdraw > 0);
        server.AssertRejected("withdraw-down-refused.sql", migrator.GenerateScript(migrations[^1], migrations[withdraw - 1]), "refuses retained WITHDRAWN evidence");
    }

    private static string Actor(string employee, string role) => $"""
          SELECT "Id" INTO company_id FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
          SELECT e."Id",a."Id" INTO actor_id,assignment_id FROM advance.employee_role_assignments a
          JOIN advance.employees e ON e."Id"=a."EmployeeId" JOIN advance.roles r ON r."Id"=a."RoleId"
          WHERE a."CompanyId"=company_id AND e."EmployeeCode"='{employee}' AND r."Code"='{role}'
            AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE);
          IF actor_id IS NULL THEN RAISE EXCEPTION 'Witness actor {employee} {role} is missing.'; END IF;
        """;

    private static string Hex(string seed) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(seed))).ToLowerInvariant();

    private static string ImportSql(int n) => $$"""
        SET SESSION AUTHORIZATION nexa_erp_runtime;
        DO $import$
        DECLARE company_id uuid; actor_id uuid; assignment_id uuid; item_id uuid; warehouse_id uuid; bin_id uuid; staged_id uuid; command_id uuid;
        BEGIN
          {{Actor("SESS-41", "STORES_MANAGER")}}
          SELECT "Id" INTO item_id FROM advance.items WHERE "CreatedBy"='TRIAL_DATA' ORDER BY "Id" LIMIT 1;
          SELECT "Id" INTO warehouse_id FROM advance.warehouses WHERE "CreatedBy"='TRIAL_DATA' AND "CompanyId"=company_id ORDER BY "Id" LIMIT 1;
          SELECT "Id" INTO bin_id FROM advance.rack_bins WHERE "CreatedBy"='TRIAL_DATA' AND "CompanyId"=company_id AND "WarehouseId"=warehouse_id AND "MaterialCondition"='AVAILABLE' ORDER BY "Id" LIMIT 1;
          command_id:=advance.register_command_request('SESS_PVT_LTD','OpeningStock.Import',decode('{{Hex("import-key-" + n)}}','hex'),decode('{{Hex("import-fp-" + n)}}','hex'),actor_id,'https://opening.test','SESS-41','STORES_MANAGER',assignment_id);
          staged_id:=advance.stage_opening_stock_import_line(company_id,'OPEN-00{{n}}',item_id,warehouse_id,bin_id,'OPEN-LOT-00{{n}}',NULL,10,25,actor_id,'STORES_MANAGER',assignment_id,'FULL','SESS-41');
          INSERT INTO advance.master_import_batches("Id","MasterKey","TemplateVersion","CompanyId","ImportMode","Status","OriginalFileName","FileSizeBytes","FileSha256","IdempotencyKey","RequestFingerprint","UploadedByEmployeeId","UploadedByEmployeeCode","OperationalRoleCode","UploadedAt","CompletedAt","RetentionExpiresAt","TotalRows","ValidRows","InvalidRows","CreatedRows","UpdatedRows","UnchangedRows","RejectedRows","NotImportedRows","CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES('f1300000-0000-0000-0000-00000000000{{n}}','opening-stock',1,company_id,'REJECT_ENTIRE_FILE','COMPLETED','opening-stock-{{n}}.xlsx',100,repeat('a',64),'opening-import-{{n}}',repeat('b',64),actor_id,'SESS-41','STORES_MANAGER',clock_timestamp(),clock_timestamp(),clock_timestamp()+interval '90 days',1,1,0,1,0,0,0,0,'f1310000-0000-0000-0000-00000000000{{n}}',clock_timestamp(),'SESS-41',0);
          INSERT INTO advance.master_import_row_results("Id","ImportBatchId","SourceRowNumber","BusinessCode","NormalizedBusinessCode","IntendedAction","Outcome","ErrorsJson","ResultRecordId","ResultVersion","ProcessedAt","CreatedAt","CreatedBy","Version")
          VALUES('f1320000-0000-0000-0000-00000000000{{n}}','f1300000-0000-0000-0000-00000000000{{n}}',2,'OPEN-00{{n}}','OPEN-00{{n}}','CREATE','CREATED','[]',staged_id,0,clock_timestamp(),clock_timestamp(),'SESS-41',0);
          PERFORM advance.commit_command_receipt(command_id,decode('{{Hex("import-business-" + n)}}','hex'),'{}',gen_random_uuid());
        END $import$;
        RESET SESSION AUTHORIZATION;
        """;

    private static string CountSql(int n) => $$"""
        SET SESSION AUTHORIZATION nexa_erp_runtime;
        DO $count$
        DECLARE company_id uuid; actor_id uuid; assignment_id uuid; command_id uuid;
        BEGIN
          {{Actor("SESS-41", "STORES_MANAGER")}}
          command_id:=advance.register_command_request('SESS_PVT_LTD','OpeningStock.RecordCount',decode('{{Hex("count-key-" + n)}}','hex'),decode('{{Hex("count-fp-" + n)}}','hex'),actor_id,'https://opening.test','SESS-41','STORES_MANAGER',assignment_id);
          PERFORM advance.record_opening_stock_count(company_id,'f1300000-0000-0000-0000-00000000000{{n}}','2026-04-01','2027-03-31','Physical count {{n}}','count-key-{{n}}','{{Hex("count-hash-" + n)}}',actor_id,'STORES_MANAGER',assignment_id,'FULL','SESS-41');
          PERFORM advance.commit_command_receipt(command_id,decode('{{Hex("count-business-" + n)}}','hex'),'{}',gen_random_uuid());
        END $count$;
        RESET SESSION AUTHORIZATION;
        """;

    private static string ValueSql() => $$"""
        SET SESSION AUTHORIZATION nexa_erp_runtime;
        DO $value$
        DECLARE company_id uuid; actor_id uuid; assignment_id uuid; command_id uuid; opening_id uuid;
        BEGIN
          {{Actor("SESS-14", "ACCOUNTS_MANAGER")}}
          SELECT "Id" INTO opening_id FROM advance.opening_stocks WHERE "CompanyId"=company_id AND "ImportBatchId"='f1300000-0000-0000-0000-000000000002';
          command_id:=advance.register_command_request('SESS_PVT_LTD','OpeningStock.ConfirmValue',decode('{{Hex("value-key")}}','hex'),decode('{{Hex("value-fp")}}','hex'),actor_id,'https://opening.test','SESS-14','ACCOUNTS_MANAGER',assignment_id);
          PERFORM advance.confirm_opening_stock_value(company_id,opening_id,0,'Rate checked','value-key','{{Hex("value-hash")}}',actor_id,'ACCOUNTS_MANAGER',assignment_id,'FULL','SESS-14');
          PERFORM advance.commit_command_receipt(command_id,decode('{{Hex("value-business")}}','hex'),'{}',gen_random_uuid());
        END $value$;
        RESET SESSION AUTHORIZATION;
        """;

    private static string WithdrawSql(string employee, string role, long version, string key, string reason = "Wrong rack in the workbook", int batch = 1, bool expectReplay = false) => $$"""
        SET SESSION AUTHORIZATION nexa_erp_runtime;
        DO $withdraw$
        DECLARE company_id uuid; actor_id uuid; assignment_id uuid; command_id uuid; opening_id uuid; replayed boolean;
        BEGIN
          {{Actor(employee, role)}}
          SELECT "Id" INTO opening_id FROM advance.opening_stocks WHERE "CompanyId"=company_id AND "ImportBatchId"='f1300000-0000-0000-0000-00000000000{{batch}}';
          command_id:=advance.register_command_request('SESS_PVT_LTD','OpeningStock.Withdraw',decode('{{Hex("withdraw-key-" + key)}}','hex'),decode('{{Hex("withdraw-fp-" + key)}}','hex'),actor_id,'https://opening.test','{{employee}}','{{role}}',assignment_id);
          SELECT x."Replayed" INTO replayed FROM advance.withdraw_opening_stock(company_id,opening_id,{{version}},'{{reason}}','{{key}}','{{Hex("withdraw-hash-" + key)}}',actor_id,'{{role}}',assignment_id,'FULL','{{employee}}') x;
          IF replayed IS DISTINCT FROM {{(expectReplay ? "true" : "false")}} THEN RAISE EXCEPTION 'Unexpected replay flag %.',replayed; END IF;
          {{(expectReplay ? "" : "PERFORM advance.commit_command_receipt(command_id,decode('" + Hex("withdraw-business-" + key) + "','hex'),'{}',gen_random_uuid());")}}
        END $withdraw$;
        RESET SESSION AUTHORIZATION;
        """;
}
