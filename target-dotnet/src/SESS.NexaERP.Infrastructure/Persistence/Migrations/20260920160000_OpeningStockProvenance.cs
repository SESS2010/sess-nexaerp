using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Opening stock provenance (decision of the Technical Director, 20 September 2026). The
/// opening-stock template carries optional vendor name, vendor bill number, bill date, purchase
/// date, make, model, part number and remarks. They are provenance only: filled where SESS knows
/// them, blank where it does not, and never part of the valuation, which is the line's
/// Accounts-confirmed ex-tax unit value. Staging and opening lines gain the eight columns; the
/// staging function gains an overload that records them (the original signature stays as a
/// wrapper so the installer contract and existing callers are unchanged); the count function
/// copies them from staging to the immutable opening line.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920160000_OpeningStockProvenance")]
public sealed class OpeningStockProvenance : Migration
{
    private const string Columns = @"""VendorName"" varchar(200) NULL,ADD COLUMN ""VendorBillNumber"" varchar(100) NULL,ADD COLUMN ""BillDate"" date NULL,ADD COLUMN ""PurchaseDate"" date NULL,ADD COLUMN ""Make"" varchar(100) NULL,ADD COLUMN ""Model"" varchar(100) NULL,ADD COLUMN ""PartNumber"" varchar(100) NULL,ADD COLUMN ""Remarks"" varchar(500) NULL";
    private const string Short = "advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text)";
    private const string Long = "advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text,text,text,date,date,text,text,text,text)";
    private const string LongArguments = "p_company uuid,p_reference text,p_item uuid,p_warehouse uuid,p_bin uuid,p_lot text,p_serial text,p_quantity numeric,p_rate numeric,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text,p_vendor_name text,p_vendor_bill text,p_bill_date date,p_purchase_date date,p_make text,p_model text,p_part_number text,p_remarks text";
    private const string ShortArguments = "p_company uuid,p_reference text,p_item uuid,p_warehouse uuid,p_bin uuid,p_lot text,p_serial text,p_quantity numeric,p_rate numeric,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text";

    private const string StageInsertBefore = @"INSERT INTO advance.opening_stock_import_staging_lines(""Id"",""CompanyId"",""LineReference"",""ItemId"",""WarehouseId"",""RackBinId"",""LotNumber"",""SerialNumber"",""Quantity"",""UnitRate"",""CreatedAt"",""CreatedBy"",""Version"") VALUES(result_id,p_company,btrim(p_reference),p_item,p_warehouse,p_bin,nullif(btrim(p_lot),''),nullif(btrim(p_serial),''),p_quantity,p_rate,clock_timestamp(),p_login,0);";
    private const string StageInsertAfter = @"IF length(btrim(coalesce(p_vendor_name,'')))>200 OR length(btrim(coalesce(p_vendor_bill,'')))>100 OR length(btrim(coalesce(p_make,'')))>100 OR length(btrim(coalesce(p_model,'')))>100 OR length(btrim(coalesce(p_part_number,'')))>100 OR length(btrim(coalesce(p_remarks,'')))>500 THEN RAISE EXCEPTION 'Opening Stock provenance text exceeds its length.'; END IF;
          INSERT INTO advance.opening_stock_import_staging_lines(""Id"",""CompanyId"",""LineReference"",""ItemId"",""WarehouseId"",""RackBinId"",""LotNumber"",""SerialNumber"",""Quantity"",""UnitRate"",""VendorName"",""VendorBillNumber"",""BillDate"",""PurchaseDate"",""Make"",""Model"",""PartNumber"",""Remarks"",""CreatedAt"",""CreatedBy"",""Version"") VALUES(result_id,p_company,btrim(p_reference),p_item,p_warehouse,p_bin,nullif(btrim(p_lot),''),nullif(btrim(p_serial),''),p_quantity,p_rate,nullif(btrim(p_vendor_name),''),nullif(btrim(p_vendor_bill),''),p_bill_date,p_purchase_date,nullif(btrim(p_make),''),nullif(btrim(p_model),''),nullif(btrim(p_part_number),''),nullif(btrim(p_remarks),''),clock_timestamp(),p_login,0);";
    private const string CountColumnsBefore = @"""UnitRate"",""LineValue"",""CreatedAt"",""CreatedBy"")";
    private const string CountColumnsAfter = @"""UnitRate"",""LineValue"",""VendorName"",""VendorBillNumber"",""BillDate"",""PurchaseDate"",""Make"",""Model"",""PartNumber"",""Remarks"",""CreatedAt"",""CreatedBy"")";
    private const string CountValuesBefore = @"s.""UnitRate"",s.""Quantity""*s.""UnitRate"",clock_timestamp(),p_login";
    private const string CountValuesAfter = @"s.""UnitRate"",s.""Quantity""*s.""UnitRate"",s.""VendorName"",s.""VendorBillNumber"",s.""BillDate"",s.""PurchaseDate"",s.""Make"",s.""Model"",s.""PartNumber"",s.""Remarks"",clock_timestamp(),p_login";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf($$"""
            DO $guard$
            BEGIN
              IF to_regprocedure('{{Short}}') IS NULL OR to_regprocedure('{{Long}}') IS NOT NULL
                 OR to_regprocedure('advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text)') IS NULL THEN
                RAISE EXCEPTION 'Opening Stock provenance requires the installed staging and count functions and no prior overload.';
              END IF;
            END $guard$;
            ALTER TABLE advance.opening_stock_import_staging_lines ADD COLUMN {{Columns}};
            ALTER TABLE advance.opening_stock_lines ADD COLUMN {{Columns}};
            DO $stage$
            DECLARE body text;
            BEGIN
              SELECT replace(prosrc,E'\r\n',E'\n') INTO STRICT body FROM pg_proc WHERE oid='{{Short}}'::regprocedure;
              IF (length(body)-length(replace(body,{{Q(StageInsertBefore)}},'')))/length({{Q(StageInsertBefore)}}) <> 1 THEN
                RAISE EXCEPTION 'stage_opening_stock_import_line is not at the expected contract; refusing to extend it.';
              END IF;
              body := replace(body,{{Q(StageInsertBefore)}},{{Q(StageInsertAfter)}});
              EXECUTE 'CREATE FUNCTION advance.stage_opening_stock_import_line({{LongArguments}}) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS '||quote_literal(body);
              EXECUTE 'CREATE OR REPLACE FUNCTION advance.stage_opening_stock_import_line({{ShortArguments}}) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS '
                ||quote_literal('BEGIN RETURN advance.stage_opening_stock_import_line(p_company,p_reference,p_item,p_warehouse,p_bin,p_lot,p_serial,p_quantity,p_rate,p_actor,p_role,p_assignment,p_type,p_login,NULL::text,NULL::text,NULL::date,NULL::date,NULL::text,NULL::text,NULL::text,NULL::text); END');
            END $stage$;
            REVOKE ALL ON FUNCTION {{Long}} FROM PUBLIC;
            DO $roles$ BEGIN
              IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
                ALTER FUNCTION {{Long}} OWNER TO nexa_erp_owner;
                REVOKE ALL ON FUNCTION {{Long}} FROM nexa_erp_bootstrap,nexa_erp_migration;
                GRANT EXECUTE ON FUNCTION {{Long}} TO nexa_erp_runtime;
              END IF;
            END $roles$;
            {{InstalledFunctionSql.Rewrite("advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text)", (CountColumnsBefore, CountColumnsAfter), (CountValuesBefore, CountValuesAfter))}}
            """));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf($$"""
            DO $guard$
            BEGIN
              IF EXISTS (SELECT 1 FROM advance.opening_stock_lines WHERE num_nonnulls("VendorName","VendorBillNumber","BillDate","PurchaseDate","Make","Model","PartNumber","Remarks")>0)
                 OR EXISTS (SELECT 1 FROM advance.opening_stock_import_staging_lines WHERE num_nonnulls("VendorName","VendorBillNumber","BillDate","PurchaseDate","Make","Model","PartNumber","Remarks")>0) THEN
                RAISE EXCEPTION 'Refusing rollback: opening stock provenance has been declared.';
              END IF;
            END $guard$;
            {{InstalledFunctionSql.Rewrite("advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text)", (CountColumnsAfter, CountColumnsBefore), (CountValuesAfter, CountValuesBefore))}}
            DO $stage$
            DECLARE body text;
            BEGIN
              SELECT replace(prosrc,E'\r\n',E'\n') INTO STRICT body FROM pg_proc WHERE oid='{{Long}}'::regprocedure;
              IF (length(body)-length(replace(body,{{Q(StageInsertAfter)}},'')))/length({{Q(StageInsertAfter)}}) <> 1 THEN
                RAISE EXCEPTION 'stage_opening_stock_import_line overload is not at the expected contract; refusing to roll it back.';
              END IF;
              body := replace(body,{{Q(StageInsertAfter)}},{{Q(StageInsertBefore)}});
              EXECUTE 'CREATE OR REPLACE FUNCTION advance.stage_opening_stock_import_line({{ShortArguments}}) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS '||quote_literal(body);
            END $stage$;
            DROP FUNCTION {{Long}};
            ALTER TABLE advance.opening_stock_lines DROP COLUMN "VendorName",DROP COLUMN "VendorBillNumber",DROP COLUMN "BillDate",DROP COLUMN "PurchaseDate",DROP COLUMN "Make",DROP COLUMN "Model",DROP COLUMN "PartNumber",DROP COLUMN "Remarks";
            ALTER TABLE advance.opening_stock_import_staging_lines DROP COLUMN "VendorName",DROP COLUMN "VendorBillNumber",DROP COLUMN "BillDate",DROP COLUMN "PurchaseDate",DROP COLUMN "Make",DROP COLUMN "Model",DROP COLUMN "PartNumber",DROP COLUMN "Remarks";
            """));
    }

    private static string Q(string clause) => InstalledFunctionSql.Quote(MigrationText.Lf(clause));
}
