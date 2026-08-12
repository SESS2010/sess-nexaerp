namespace SESS.NexaERP.Tests;

public sealed class Rev869BDatabaseSafetyContractTests
{
    private static readonly string Root = FindRoot();
    private static readonly string Safety = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BDatabaseSafetySql.cs");
    private static readonly string Lifecycle = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BDatabaseLifecycleSql.cs");
    private static readonly string Migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs");
    private static readonly string Postgres = Read("tests", "SESS.NexaERP.Tests", "Rev869BPostgresBehaviorTests.cs") +
        Read("tests", "SESS.NexaERP.Tests", "Rev869BPostgresApplicationBehaviorTests.cs");

    [Fact]
    public void ChildInsertGuardsRequireExactEditableParentVersion()
    {
        foreach (var table in new[] { "request_for_quotation_lines", "rfq_vendor_invitations", "vendor_quotation_lines", "commercial_comparison_lines", "purchase_order_lines", "quotation_technical_verifications", "material_followup_handoffs" })
            Assert.Contains($"TG_TABLE_NAME='{table}'", Safety);
        Assert.Contains("child INSERT requires exactly one editable parent version", Safety);
        Assert.Contains("q.\"Status\"='Draft' AND q.\"Version\"=0", Safety);
        Assert.Contains("c.\"Status\"='Draft' AND c.\"Version\"=0", Safety);
        Assert.Contains("p.\"Status\" IN ('Draft','RevisionDraft')", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.request_for_quotation_lines", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.rfq_vendor_invitations", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.vendor_quotation_lines", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.commercial_comparison_lines", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.purchase_order_lines", Safety);
        Assert.Contains("BEFORE INSERT ON nexa.material_followup_handoffs", Safety);
        Assert.Contains("('Draft','Submitted')", Lifecycle);
    }

    [Fact]
    public void EveryRev869BRelationHasControlledInsertUpdateAndDeleteCoverage()
    {
        foreach (var trigger in new[]
        {
            "trg_rev869b_rfq_lines_immutable", "trg_rev869b_invitation_snapshot_immutable",
            "trg_rev869b_comparison_lines_delete_guard", "trg_rev869b_followup_immutable",
            "trg_rev869b_vendor_quotation_lines_immutable", "trg_rev869b_technical_verifications_immutable",
            "trg_rev869b_purchase_order_lines_immutable", "trg_rev869b_purchase_approval_history_immutable",
            "trg_rev869b_purchase_order_history_immutable", "trg_rev869b_purchase_status_history_immutable"
        })
            Assert.Contains(trigger, Safety + Migration);
        Assert.Contains("qualification and provenance snapshot is immutable", Safety);
        Assert.Contains("BEFORE UPDATE OR DELETE ON nexa.request_for_quotation_lines", Safety);
        Assert.Contains("BEFORE UPDATE OR DELETE ON nexa.rfq_vendor_invitations", Safety);
        Assert.Contains("BEFORE DELETE ON nexa.commercial_comparison_lines", Safety);
        Assert.Contains("BEFORE UPDATE OR DELETE ON nexa.material_followup_handoffs", Safety);
    }

    [Fact]
    public void CanonicalCommercialFunctionUsesRelationalInputsAndFailClosedJson()
    {
        Assert.Contains("rev869b_commercial_snapshot_reconciles", Safety);
        foreach (var field in new[] { "quantity", "unit_rate", "gross", "assessable", "taxable", "cgst", "sgst", "igst", "cess", "payable" })
            Assert.Contains(field, Safety, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("jsonb_typeof", Safety);
        Assert.Contains("IS NOT DISTINCT FROM", Safety);
        Assert.Contains("EXCEPTION WHEN OTHERS THEN RETURN FALSE", Safety);
        Assert.DoesNotContain("->'taxRule' <>", Safety);
        Assert.Contains("->'taxRule' IS NOT NULL", Safety);
        Assert.DoesNotContain(") IS NOT NULL;", Safety);
        Assert.Contains(") IS TRUE;", Lifecycle);
    }

    [Fact]
    public void AuthoritativeTransitionsUseExactJoinsCardinalityTaxAndPoProvenance()
    {
        foreach (var relation in new[] { "request_for_quotations", "request_for_quotation_lines", "rfq_vendor_invitations", "vendor_quotations", "vendor_quotation_lines", "quotation_technical_verifications", "commercial_comparisons", "commercial_comparison_lines", "purchase_orders", "purchase_order_lines", "tax_gst_settings", "purchase_transaction_approval_policies", "purchase_order_history" })
            Assert.Contains(relation, Safety);
        Assert.Contains("expected_count", Safety);
        Assert.Contains("matched_count", Safety);
        Assert.Contains("approval_count<>1", Safety);
        Assert.Contains("exact source/version/cardinality/commercial provenance", Safety);
        Assert.Contains("issue requires exactly one approval history", Safety);
        Assert.DoesNotContain("trg_rev869b_quotation_authoritative_guard", Safety);
        Assert.All(new[] { Safety, Lifecycle }, source => Assert.Contains("SET search_path = pg_catalog, nexa", source));
    }

    [Fact]
    public void MigrationInstallsAndRemovesOnlyOwnedSafetyObjects()
    {
        Assert.Contains("Rev869BDatabaseSafetySql.Install", Migration);
        Assert.Contains("Rev869BDatabaseLifecycleSql.Install", Migration);
        Assert.Contains("Rev869BDatabaseLifecycleSql.Remove", Migration);
        Assert.Contains("Rev869BDatabaseSafetySql.Remove", Migration);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_", Safety);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_", Lifecycle);
    }

    [Fact]
    public void FuturePostgresSourceRetainsExactDatabaseSafetyAndNoFallback()
    {
        Assert.Contains("sess_nexaerp_rev869b_verify", Postgres);
        Assert.Contains("ISOLATED_REV869B_BEHAVIOR_TESTS", Postgres);
        Assert.Contains("current_database()", Postgres);
        Assert.Contains("no fallback is permitted", Postgres);
        Assert.DoesNotContain("ORDER BY \"Id\" LIMIT 1", Postgres);
        Assert.DoesNotContain("FROM nexa.request_for_quotations r LIMIT 1", Postgres);
        Assert.Contains("BeginTransactionAsync(IsolationLevel.Serializable)", Postgres);
        Assert.Contains("transaction.RollbackAsync()", Postgres);
        Assert.Contains("deterministic fixture collision", Postgres, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("new EfRev869BPurchaseService", Postgres);
        Assert.Contains("CreateRfqAsync", Postgres);
        Assert.Contains("PurchaseTransactionStatusHistories", Postgres);
        Assert.Contains("AuditLogs", Postgres);
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
