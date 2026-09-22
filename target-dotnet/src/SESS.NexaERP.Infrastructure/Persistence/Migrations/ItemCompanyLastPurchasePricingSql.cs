namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class ItemCompanyLastPurchasePricingSql
{
    internal const string Up = """
        CREATE FUNCTION advance.guard_item_company_last_purchase()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        BEGIN
          IF current_setting('sess.vendor_bill_write',true) IS DISTINCT FROM txid_current()::text THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Item last purchase values may change only with controlled Vendor Bill acceptance or reversal.';
          END IF;
          IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Item last purchase cache is maintained, never deleted.'; END IF;
          RETURN NEW;
        END $function$;

        CREATE FUNCTION advance.refresh_item_company_last_purchase(p_company uuid,p_item uuid,p_login text)
        RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE source_bill uuid; source_date date; source_rate numeric(24,6);
        BEGIN
          SELECT b."Id",b."BillDate",round(sum(a."AcceptedValue"+a."AllocatedChargeValue")/sum(a."AllocatedQuantity"),6)
            INTO source_bill,source_date,source_rate
          FROM advance.vendor_bills b
          JOIN advance.vendor_bill_lines l ON l."CompanyId"=b."CompanyId" AND l."VendorBillId"=b."Id" AND l."ItemId"=p_item
          JOIN advance.vendor_bill_cost_allocations a ON a."CompanyId"=l."CompanyId" AND a."VendorBillLineId"=l."Id"
          WHERE b."CompanyId"=p_company AND b."Status"='ACCEPTED'
          GROUP BY b."Id",b."BillDate",b."DecidedAt"
          ORDER BY b."DecidedAt" DESC,b."Id" DESC LIMIT 1;
          IF FOUND THEN
            INSERT INTO advance.item_company_last_purchases
              ("Id","CompanyId","ItemId","LastPurchaseRate","LastPurchaseDate","LastPurchaseBillId","CreatedAt","CreatedBy","Version")
            VALUES(gen_random_uuid(),p_company,p_item,source_rate,source_date,source_bill,clock_timestamp(),p_login,0)
            ON CONFLICT ("CompanyId","ItemId") DO UPDATE SET
              "LastPurchaseRate"=excluded."LastPurchaseRate","LastPurchaseDate"=excluded."LastPurchaseDate",
              "LastPurchaseBillId"=excluded."LastPurchaseBillId","UpdatedAt"=clock_timestamp(),
              "UpdatedBy"=p_login,"Version"=advance.item_company_last_purchases."Version"+1;
          ELSE
            UPDATE advance.item_company_last_purchases SET "LastPurchaseRate"=NULL,"LastPurchaseDate"=NULL,
              "LastPurchaseBillId"=NULL,"UpdatedAt"=clock_timestamp(),"UpdatedBy"=p_login,"Version"="Version"+1
            WHERE "CompanyId"=p_company AND "ItemId"=p_item;
          END IF;
        END $function$;

        CREATE FUNCTION advance.maintain_item_company_last_purchase()
        RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE affected record;
        BEGIN
          IF (OLD."Status"='DRAFT' AND NEW."Status"='ACCEPTED') OR
             (OLD."Status"='ACCEPTED' AND NEW."Status"='REVERSED') THEN
            FOR affected IN SELECT DISTINCT "ItemId" FROM advance.vendor_bill_lines
              WHERE "CompanyId"=NEW."CompanyId" AND "VendorBillId"=NEW."Id"
            LOOP
              PERFORM advance.refresh_item_company_last_purchase(NEW."CompanyId",affected."ItemId",coalesce(NEW."UpdatedBy",NEW."CreatedBy"));
            END LOOP;
          END IF;
          RETURN NEW;
        END $function$;

        CREATE TRIGGER trg_item_company_last_purchase_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.item_company_last_purchases FOR EACH ROW EXECUTE FUNCTION advance.guard_item_company_last_purchase();
        CREATE TRIGGER trg_vendor_bill_maintain_item_last_purchase AFTER UPDATE OF "Status"
          ON advance.vendor_bills FOR EACH ROW EXECUTE FUNCTION advance.maintain_item_company_last_purchase();

        DO $backfill$ DECLARE row record; BEGIN
          PERFORM set_config('sess.vendor_bill_write',txid_current()::text,true);
          FOR row IN SELECT DISTINCT b."CompanyId",l."ItemId" FROM advance.vendor_bills b
            JOIN advance.vendor_bill_lines l ON l."CompanyId"=b."CompanyId" AND l."VendorBillId"=b."Id"
            WHERE b."Status"='ACCEPTED'
          LOOP PERFORM advance.refresh_item_company_last_purchase(row."CompanyId",row."ItemId",'ItemCompanyLastPurchasePricing'); END LOOP;
        END $backfill$;

        REVOKE ALL ON TABLE advance.item_company_last_purchases FROM PUBLIC;
        REVOKE ALL ON FUNCTION advance.guard_item_company_last_purchase(),advance.refresh_item_company_last_purchase(uuid,uuid,text),advance.maintain_item_company_last_purchase() FROM PUBLIC;
        DO $roles$ BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER TABLE advance.item_company_last_purchases OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_item_company_last_purchase() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.refresh_item_company_last_purchase(uuid,uuid,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.maintain_item_company_last_purchase() OWNER TO nexa_erp_owner;
          END IF;
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
            REVOKE INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER ON advance.item_company_last_purchases FROM nexa_erp_runtime;
            GRANT SELECT ON advance.item_company_last_purchases TO nexa_erp_runtime;
            REVOKE ALL ON FUNCTION advance.refresh_item_company_last_purchase(uuid,uuid,text),advance.maintain_item_company_last_purchase(),advance.guard_item_company_last_purchase() FROM nexa_erp_runtime;
          END IF;
        END $roles$;
        """;

    internal const string Down = """
        DROP TRIGGER IF EXISTS trg_vendor_bill_maintain_item_last_purchase ON advance.vendor_bills;
        DROP TRIGGER IF EXISTS trg_item_company_last_purchase_guard ON advance.item_company_last_purchases;
        DROP FUNCTION IF EXISTS advance.maintain_item_company_last_purchase();
        DROP FUNCTION IF EXISTS advance.refresh_item_company_last_purchase(uuid,uuid,text);
        DROP FUNCTION IF EXISTS advance.guard_item_company_last_purchase();
        """;
}