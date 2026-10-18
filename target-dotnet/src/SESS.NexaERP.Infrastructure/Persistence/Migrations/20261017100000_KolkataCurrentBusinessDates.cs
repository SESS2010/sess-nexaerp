using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261017100000_KolkataCurrentBusinessDates")]
public sealed class KolkataCurrentBusinessDates : Migration
{
    internal const string KolkataUpper = "advance.CURRENT_BUSINESS_DATE()";
    internal const string KolkataLower = "advance.current_business_date()";
    // Exact installed baseline-151 contracts, not a global search/replace and not historical event conversions.
    private static readonly (string Signature, int Upper, int Lower)[] Functions =
    [
        ("advance.company_report_billed_not_received(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)", 8, 0),
        ("advance.company_report_fifo_valuation(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)", 8, 0),
        ("advance.company_report_grni(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)", 8, 0),
        ("advance.company_report_machine_dossier(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)", 8, 0),
        ("advance.company_report_pending_approvals(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)", 8, 10),
        ("advance.company_report_purchase_register(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)", 8, 0),
        ("advance.company_report_vendor_purchases(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)", 8, 0),
        ("advance.complete_authentication_bootstrap(text,text)", 0, 26),
        ("advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text)", 0, 1),
        ("advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text)", 0, 1),
        ("advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text)", 0, 2),
        ("advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text)", 0, 2),
        ("advance.fat_authority_valid(uuid,uuid,text,uuid,text,text[])", 4, 0),
        ("advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)", 0, 2),
        ("advance.fitment_authority_valid(uuid,uuid,text,uuid,text,text[])", 4, 0),
        ("advance.govern_authentication_bootstrap(text,text,boolean,boolean)", 0, 21),
        ("advance.guard_employee_role_administration()", 2, 0),
        ("advance.guard_estimated_bom_governance()", 2, 0),
        ("advance.guard_governed_job_order()", 0, 1),
        ("advance.guard_job_order_history()", 0, 1),
        ("advance.guard_production_engineering_history()", 2, 0),
        ("advance.guard_sensitive_audit_assignment()", 2, 0),
        ("advance.intercompany_purchase_json(uuid,uuid,uuid,text)", 0, 2),
        ("advance.intercompany_purchase_options(uuid,uuid)", 0, 2),
        ("advance.intercompany_purchase_scope(uuid,uuid,advance.purchase_orders)", 0, 2),
        ("advance.list_vendor_payables(uuid,uuid,boolean)", 2, 0),
        ("advance.opening_stock_command_valid(uuid,uuid,text,uuid,text,text,text)", 2, 0),
        ("advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)", 0, 2),
        ("advance.publish_intercompany_purchase(uuid,uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text)", 0, 4),
        ("advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer)", 0, 8),
        ("advance.purchase_open_orders(text,uuid,uuid[],text,uuid,text,uuid,boolean,bigint,integer)", 0, 8),
        ("advance.purchase_spending(text,uuid,uuid[],text,text,date,uuid,uuid,text,uuid,bigint,integer)", 0, 8),
        ("advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer)", 0, 8),
        ("advance.read_command_receipt(uuid)", 1, 0),
        ("advance.record_intercompany_invoice(uuid,uuid,uuid,text,date,text,text,bytea,uuid,text,uuid,text,text)", 0, 3),
        ("advance.record_intercompany_route(uuid,uuid,uuid,text,jsonb,uuid,text,uuid,text,text)", 0, 1),
        ("advance.record_machine_delivery(uuid,uuid,uuid,text,jsonb,bytea,uuid,text,uuid,text,text)", 0, 2),
        ("advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text)", 0, 1),
        ("advance.record_vendor_manual_assessment(uuid,uuid,uuid,bigint,uuid,numeric,numeric,numeric,text,uuid,text,uuid,text,text)", 0, 1),
        ("advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)", 1, 0),
        ("advance.require_inventory_period_authority(uuid,uuid,uuid,text,uuid,text,text)", 0, 1),
        ("advance.rev869b_register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)", 1, 0),
        ("advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)", 2, 0),
        ("advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text)", 0, 3),
        ("advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text)", 0, 2),
        ("advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text,text,text,date,date,text,text,text,text)", 2, 0),
        ("advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer)", 0, 8),
        ("advance.stores_workload(text,uuid,uuid[],text,text,uuid,bigint,integer)", 0, 8),
        ("advance.supplier_invoice_command_valid(uuid,uuid,text,uuid,text,text,text)", 0, 1),
        ("advance.tracking_history(text,uuid,uuid[],text,text,uuid)", 0, 8),
        ("advance.tracking_pending(text,uuid,uuid[],text)", 0, 8),
        ("advance.vendor_financial_command_valid(uuid,uuid,text,uuid,text,text)", 2, 0),
    ];

    private static readonly (string Signature, int Count)[] StatementFunctions =
    [
        ("advance.ordinary_guard_qualification_lifecycle()", 10),
        ("advance.rev869b_guard_history_insert()", 4),
        ("advance.tax_gst_guard_controlled_mutation()", 4),
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            CREATE FUNCTION advance.current_business_date() RETURNS date
            LANGUAGE sql STABLE SECURITY INVOKER SET search_path=pg_catalog,advance
            AS $clock$ SELECT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Kolkata')::date $clock$;
            REVOKE ALL ON FUNCTION advance.current_business_date() FROM PUBLIC;
            DO $grant$ BEGIN
              IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
                GRANT EXECUTE ON FUNCTION advance.current_business_date() TO nexa_erp_runtime;
              END IF;
            END $grant$;
            """));
        Rewrite(migrationBuilder, false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            DO $guard$ BEGIN
              LOCK TABLE advance.purchase_requisitions,advance.request_for_quotations,
                advance.material_issue_requests,advance.opening_stocks,advance.stock_movements,
                advance.fifo_inventory_cost_layers IN ACCESS EXCLUSIVE MODE;
              IF EXISTS(SELECT 1 FROM advance.purchase_requisitions)
                 OR EXISTS(SELECT 1 FROM advance.request_for_quotations)
                 OR EXISTS(SELECT 1 FROM advance.material_issue_requests)
                 OR EXISTS(SELECT 1 FROM advance.opening_stocks)
                 OR EXISTS(SELECT 1 FROM advance.stock_movements)
                 OR EXISTS(SELECT 1 FROM advance.fifo_inventory_cost_layers) THEN
                RAISE EXCEPTION 'Kolkata business date rollback refuses retained business evidence; keep the forward migration.';
              END IF;
            END $guard$;
            """));
        Rewrite(migrationBuilder, true);
        migrationBuilder.Sql("DROP FUNCTION advance.current_business_date();");
    }

    private static void Rewrite(MigrationBuilder builder, bool down)
    {
        foreach (var (signature, upper, lower) in Functions)
        {
            List<(string From, string To, int Occurrences)> changes = [];
            if (upper > 0) changes.Add(down ? (KolkataUpper, "CURRENT_DATE", upper) : ("CURRENT_DATE", KolkataUpper, upper));
            if (lower > 0) changes.Add(down ? (KolkataLower, "current_date", lower) : ("current_date", KolkataLower, lower));
            builder.Sql(InstalledFunctionSql.Rewrite(signature, true, changes.ToArray()));
        }
        foreach (var (signature, count) in StatementFunctions)
            builder.Sql(InstalledFunctionSql.Rewrite(signature, true, down
                ? (KolkataLower, "statement_timestamp()::date", count)
                : ("statement_timestamp()::date", KolkataLower, count)));
    }
}