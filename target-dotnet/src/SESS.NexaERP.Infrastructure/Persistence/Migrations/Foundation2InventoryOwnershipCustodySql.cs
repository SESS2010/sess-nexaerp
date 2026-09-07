namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class Foundation2InventoryOwnershipCustodySql
{
    public const string Up = """
        CREATE OR REPLACE VIEW advance.inventory_custody_case_source_links AS
        SELECT "Id", "CompanyId", "CustodyCaseId", "CustodyCaseLineId", "LinkRole",
               'GATE_ENTRY'::text AS "SourceType", "GateEntryId" AS "SourceId",
               "CreatedAt", "CreatedBy"
        FROM advance.inventory_custody_case_gate_entry_links
        UNION ALL
        SELECT "Id", "CompanyId", "CustodyCaseId", "CustodyCaseLineId", "LinkRole",
               'GOODS_RECEIPT'::text, "GoodsReceiptId", "CreatedAt", "CreatedBy"
        FROM advance.inventory_custody_case_goods_receipt_links
        UNION ALL
        SELECT "Id", "CompanyId", "CustodyCaseId", "CustodyCaseLineId", "LinkRole",
               'DELIVERY_CHALLAN'::text, "DeliveryChallanId", "CreatedAt", "CreatedBy"
        FROM advance.inventory_custody_case_delivery_challan_links
        UNION ALL
        SELECT "Id", "CompanyId", "CustodyCaseId", "CustodyCaseLineId", "LinkRole",
               'PURCHASE_ORDER'::text, "PurchaseOrderId", "CreatedAt", "CreatedBy"
        FROM advance.inventory_custody_case_purchase_order_links
        UNION ALL
        SELECT "Id", "CompanyId", "CustodyCaseId", "CustodyCaseLineId", "LinkRole",
               'CUSTOMER_PURCHASE_ORDER'::text, "CustomerPurchaseOrderId", "CreatedAt", "CreatedBy"
        FROM advance.inventory_custody_case_customer_purchase_order_links
        UNION ALL
        SELECT "Id", "CompanyId", "CustodyCaseId", "CustodyCaseLineId", "LinkRole",
               'JOB_ORDER'::text, "JobOrderId", "CreatedAt", "CreatedBy"
        FROM advance.inventory_custody_case_job_order_links;

        CREATE OR REPLACE FUNCTION advance.reject_inventory_memo_liability_event_mutation()
        RETURNS trigger
        LANGUAGE plpgsql
        AS $function$
        BEGIN
            RAISE EXCEPTION 'inventory_memo_liability_events is append-only; record a REVERSAL event instead'
                USING ERRCODE = '55000';
        END;
        $function$;

        CREATE TRIGGER trg_inventory_memo_liability_events_append_only
        BEFORE UPDATE OR DELETE ON advance.inventory_memo_liability_events
        FOR EACH ROW
        EXECUTE FUNCTION advance.reject_inventory_memo_liability_event_mutation();

        REVOKE ALL ON FUNCTION advance.reject_inventory_memo_liability_event_mutation() FROM PUBLIC;
        """;

    public const string Down = """
        DROP VIEW IF EXISTS advance.inventory_custody_case_source_links;
        DROP TRIGGER IF EXISTS trg_inventory_memo_liability_events_append_only
            ON advance.inventory_memo_liability_events;
        DROP FUNCTION IF EXISTS advance.reject_inventory_memo_liability_event_mutation();
        """;
}
