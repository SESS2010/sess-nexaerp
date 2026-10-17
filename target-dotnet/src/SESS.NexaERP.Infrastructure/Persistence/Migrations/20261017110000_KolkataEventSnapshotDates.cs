using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261017110000_KolkataEventSnapshotDates")]
public sealed class KolkataEventSnapshotDates : Migration
{
    private static readonly (string Signature, (string From, string To, int Occurrences)[] Changes)[] Functions =
    [
        ("advance.rev869b_commercial_snapshot_reconciles(uuid,jsonb,jsonb)", [
            ("q.\"ReceivedAt\"::date effective_on,", "ql.\"TaxRuleSnapshotJson\"->>'quotationDateBasis' quotation_date_basis,\n           (CASE WHEN ql.\"TaxRuleSnapshotJson\"->>'quotationDateBasis'='ASIA_KOLKATA_V1' THEN (q.\"ReceivedAt\" AT TIME ZONE 'Asia/Kolkata')::date ELSE (q.\"ReceivedAt\" AT TIME ZONE 'UTC')::date END) effective_on,", 1),
            ("RETURN p_commercial->'input'", "IF (p_tax->>'quotationDateBasis') IS DISTINCT FROM v.quotation_date_basis THEN RETURN FALSE; END IF;\n    IF p_tax ? 'quotationDateBasis' THEN\n      IF p_tax->>'quotationDateBasis' IS DISTINCT FROM 'ASIA_KOLKATA_V1' THEN RETURN FALSE; END IF;\n      expected_tax := expected_tax || jsonb_build_object('quotationDateBasis','ASIA_KOLKATA_V1');\n    END IF;\nRETURN p_commercial->'input'", 1),
        ]),
        ("advance.rev869b_guard_authoritative_transition()", [
            ("q.\"ReceivedAt\"::date", "(CASE WHEN ql.\"TaxRuleSnapshotJson\"->>'quotationDateBasis'='ASIA_KOLKATA_V1' THEN (q.\"ReceivedAt\" AT TIME ZONE 'Asia/Kolkata')::date ELSE (q.\"ReceivedAt\" AT TIME ZONE 'UTC')::date END)", 4),
            ("i.\"InvitedAt\"::date", "(CASE WHEN i.\"VendorQualificationSnapshotJson\"->>'businessDateBasis'='ASIA_KOLKATA_V1' THEN (i.\"InvitedAt\" AT TIME ZONE 'Asia/Kolkata')::date ELSE (i.\"InvitedAt\" AT TIME ZONE 'UTC')::date END)", 4),
            ("jsonb_typeof(i.\"VendorQualificationSnapshotJson\")='object' AND", "jsonb_typeof(i.\"VendorQualificationSnapshotJson\")='object' AND\n               (i.\"VendorQualificationSnapshotJson\"->>'businessDateBasis' IS NULL OR i.\"VendorQualificationSnapshotJson\"->>'businessDateBasis'='ASIA_KOLKATA_V1') AND", 2),
        ]),
        ("advance.stores_p2_goods_receipt_line_guard()", [
            ("parent.\"ReceivedAt\"::date", "(CASE WHEN parent.\"ConfigurationSnapshotJson\"->>'businessDateBasis'='ASIA_KOLKATA_V1' THEN (parent.\"ReceivedAt\" AT TIME ZONE 'Asia/Kolkata')::date ELSE (parent.\"ReceivedAt\" AT TIME ZONE 'UTC')::date END)", 2),
            ("  IF NOT FOUND OR route.", "  IF parent.\"ConfigurationSnapshotJson\"->>'businessDateBasis' IS NOT NULL AND\n     parent.\"ConfigurationSnapshotJson\"->>'businessDateBasis'<>'ASIA_KOLKATA_V1' THEN\n    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='GRN retained business-date policy is unknown.';\n  END IF;\n  IF NOT FOUND OR route.", 1),
        ]),
        ("advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)", [
            ("receipt.\"ReceivedAt\"::date", "(CASE WHEN receipt.\"ConfigurationSnapshotJson\"->>'businessDateBasis'='ASIA_KOLKATA_V1' THEN (receipt.\"ReceivedAt\" AT TIME ZONE 'Asia/Kolkata')::date ELSE (receipt.\"ReceivedAt\" AT TIME ZONE 'UTC')::date END)", 1),
            ("  SELECT * INTO posted FROM advance.post_stores_stock_batch(", "  IF receipt.\"ConfigurationSnapshotJson\"->>'businessDateBasis' IS NOT NULL AND\n     receipt.\"ConfigurationSnapshotJson\"->>'businessDateBasis'<>'ASIA_KOLKATA_V1' THEN\n    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='GRN retained business-date policy is unknown.';\n  END IF;\n  SELECT * INTO posted FROM advance.post_stores_stock_batch(", 1),
        ]),
    ];
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        Rewrite(migrationBuilder, false);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            DO $guard$ BEGIN
              LOCK TABLE advance.vendor_quotation_lines,advance.rfq_vendor_invitations,advance.goods_receipts IN ACCESS EXCLUSIVE MODE;
              IF EXISTS(SELECT 1 FROM advance.vendor_quotation_lines WHERE "TaxRuleSnapshotJson" ? 'quotationDateBasis')
                 OR EXISTS(SELECT 1 FROM advance.rfq_vendor_invitations WHERE "VendorQualificationSnapshotJson"->>'businessDateBasis' IS NOT NULL)
                 OR EXISTS(SELECT 1 FROM advance.goods_receipts WHERE "ConfigurationSnapshotJson"->>'businessDateBasis' IS NOT NULL) THEN
                RAISE EXCEPTION 'Kolkata snapshot rollback refuses retained versioned date evidence; keep the forward migration.';
              END IF;
            END $guard$;
            """));
        Rewrite(migrationBuilder, true);
    }
    private static void Rewrite(MigrationBuilder builder, bool down)
    {
        foreach (var (signature, changes) in Functions)
            builder.Sql(InstalledFunctionSql.Rewrite(signature, true, down
                ? changes.Reverse().Select(c => (From: c.To, To: c.From, Occurrences: c.Occurrences)).ToArray() : changes));
    }
}
