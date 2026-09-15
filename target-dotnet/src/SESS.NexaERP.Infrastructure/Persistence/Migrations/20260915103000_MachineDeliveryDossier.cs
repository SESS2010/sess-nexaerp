using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Reporting;
namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260915103000_MachineDeliveryDossier")]
public sealed class MachineDeliveryDossier : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  PostgreSqlClusterGuard.Require(migrationBuilder);
  migrationBuilder.Sql("""
   DO $guard$ BEGIN
    IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1') THEN RAISE EXCEPTION 'Machine delivery refuses an unsupported or system database.'; END IF;
    IF to_regclass('advance.machine_delivery_challans') IS NOT NULL THEN RAISE EXCEPTION 'Machine delivery package already exists.'; END IF;
    IF to_regprocedure('advance.get_actual_bom_landed_valuations(uuid,uuid)') IS NULL OR to_regclass('advance.job_order_fat_reconciliations') IS NULL THEN RAISE EXCEPTION 'Machine delivery requires FAT and Actual BOM costing.'; END IF;
   END $guard$;
   """);
  migrationBuilder.Sql(MachineDeliveryMigrationSql.Snapshot);
  migrationBuilder.Sql(MachineDeliveryMigrationSql.Report);
  migrationBuilder.Sql("""
   INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version") VALUES
    (md5('stores.machine-deliveries')::uuid,'stores.machine-deliveries','Stores','Machine delivery challans','/stores/machine-deliveries',true,now(),'MachineDeliveryDossier',0),
    (md5('reports.machine-dossier')::uuid,'reports.machine-dossier','Reports','Delivered machine audit dossier','/reports/machine-dossier',true,now(),'MachineDeliveryDossier',0);
   INSERT INTO advance.role_page_permissions
   SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(source)||jsonb_build_object(
    'Id',md5('stores.machine-deliveries:'||source."RoleId"::text)::uuid,'PageDefinitionId',md5('stores.machine-deliveries')::uuid,
    'CreatedAt',now(),'CreatedBy','MachineDeliveryDossier','Version',0))).*
   FROM advance.role_page_permissions source JOIN advance.page_definitions p ON p."Id"=source."PageDefinitionId"
   JOIN advance.roles r ON r."Id"=source."RoleId"
   WHERE p."PageKey"='stores.material-issues' AND r."Code" IN('STORES_ASSISTANT','STORES_EXECUTIVE','STORES_MANAGER');
   INSERT INTO advance.role_page_permissions
   SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(source)||jsonb_build_object(
    'Id',md5('reports.machine-dossier:'||source."RoleId"::text)::uuid,'PageDefinitionId',md5('reports.machine-dossier')::uuid,
    'CreatedAt',now(),'CreatedBy','MachineDeliveryDossier','Version',0))).*
   FROM advance.role_page_permissions source JOIN advance.page_definitions p ON p."Id"=source."PageDefinitionId"
   WHERE p."PageKey"='reports.fifo-valuation';
   """);
  migrationBuilder.Sql(MachineDeliveryMigrationSql.Ownership);
 }
 protected override void Down(MigrationBuilder migrationBuilder)
 {
  PostgreSqlClusterGuard.Require(migrationBuilder);
  migrationBuilder.Sql("""
   DO $guard$ BEGIN
    IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1') THEN RAISE EXCEPTION 'Machine delivery rollback refuses this database.'; END IF;
    IF EXISTS(SELECT 1 FROM advance.machine_delivery_challans) THEN RAISE EXCEPTION 'Machine delivery rollback refuses retained evidence.'; END IF;
   END $guard$;
   DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId" IN(md5('stores.machine-deliveries')::uuid,md5('reports.machine-dossier')::uuid);
   DELETE FROM advance.page_definitions WHERE "PageKey" IN('stores.machine-deliveries','reports.machine-dossier');
   DROP FUNCTION advance.company_report_machine_dossier(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text);
   DROP FUNCTION advance.record_machine_delivery(uuid,uuid,uuid,text,jsonb,bytea,uuid,text,uuid,text,text);
   DROP FUNCTION advance.machine_delivery_signature_content(uuid,uuid);
   DROP FUNCTION advance.machine_delivery_json(uuid,uuid);
   DROP TABLE advance.machine_delivery_bom_entries;
   DROP TABLE advance.machine_delivery_signatures;
   DROP TABLE advance.machine_delivery_challans;
   DROP FUNCTION advance.guard_machine_delivery_evidence();
   """);
 }
}
internal static class MachineDeliveryMigrationSql
{
 internal static string Snapshot
 {
  get { using var stream=typeof(MachineDeliveryMigrationSql).Assembly.GetManifestResourceStream("MachineDeliveryDossier.20260915103000.sql")!;
   using var reader=new StreamReader(stream); return reader.ReadToEnd(); }
 }
 internal static string Report
 {
  get { using var stream=typeof(MachineDeliveryMigrationSql).Assembly.GetManifestResourceStream("MachineDossierReport.20260915103000.sql")!;
   using var reader=new StreamReader(stream); return reader.ReadToEnd(); }
 }
 internal const string Ownership = """
 DO $machine_acl$ DECLARE relation text; signature text; principal text; BEGIN
 IF to_regclass('advance.machine_delivery_challans') IS NOT NULL THEN
  FOREACH relation IN ARRAY ARRAY['machine_delivery_challans','machine_delivery_signatures','machine_delivery_bom_entries'] LOOP
   EXECUTE format('REVOKE ALL ON TABLE advance.%I FROM PUBLIC',relation);
   IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER TABLE advance.%I OWNER TO nexa_erp_owner',relation); END IF;
   FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration'] LOOP
    IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON TABLE advance.%I FROM %I',relation,principal); END IF;
   END LOOP;
  END LOOP;
  FOREACH signature IN ARRAY ARRAY['advance.guard_machine_delivery_evidence()','advance.machine_delivery_signature_content(uuid,uuid)','advance.machine_delivery_json(uuid,uuid)',
   'advance.record_machine_delivery(uuid,uuid,uuid,text,jsonb,bytea,uuid,text,uuid,text,text)',
   'advance.company_report_machine_dossier(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)'] LOOP
   EXECUTE format('REVOKE ALL ON FUNCTION %s FROM PUBLIC',signature);
   IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER FUNCTION %s OWNER TO nexa_erp_owner',signature); END IF;
   FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration'] LOOP
    IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON FUNCTION %s FROM %I',signature,principal); END IF;
   END LOOP;
  END LOOP;
  IF to_regrole('nexa_erp_runtime') IS NOT NULL THEN
   GRANT EXECUTE ON FUNCTION advance.machine_delivery_signature_content(uuid,uuid),advance.machine_delivery_json(uuid,uuid),advance.record_machine_delivery(uuid,uuid,uuid,text,jsonb,bytea,uuid,text,uuid,text,text),
    advance.company_report_machine_dossier(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) TO nexa_erp_runtime;
  END IF;
 END IF;
 END $machine_acl$;
 """;
}
