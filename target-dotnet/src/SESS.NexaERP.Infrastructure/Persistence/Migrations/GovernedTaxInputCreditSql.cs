namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class GovernedTaxInputCreditSql
{
    public static string Restore
    {
        get
        {
            var sql = Rev869BDatabaseSafetySql.Install;
            const string marker = "CREATE OR REPLACE FUNCTION advance.rev869b_commercial_snapshot_reconciles(";
            var start = sql.IndexOf(marker, StringComparison.Ordinal);
            const string endMarker = "END $rev869b$;";
            if (start < 0) throw new InvalidOperationException("Commercial reconciliation function was not found.");
            var end = sql.IndexOf(endMarker, start, StringComparison.Ordinal);
            if (end < 0) throw new InvalidOperationException("Commercial reconciliation function was not found.");
            return sql.Substring(start, end + endMarker.Length - start)
                .Replace("\r\n", "\n", StringComparison.Ordinal);
        }
    }

    public static string Install => Restore
        .Replace("t.\"ApprovalStatus\" approval_status, t.\"IsActive\" tax_active",
            "t.\"ApprovalStatus\" approval_status, t.\"IsActive\" tax_active, t.\"ItcEligibility\" itc_eligibility, t.\"RecoverableTaxPercent\" recoverable_tax_percent", StringComparison.Ordinal)
        .Replace("RETURN p_commercial->'input' IS NOT DISTINCT FROM expected_input AND", """
            IF p_tax ? 'itcEligibility' OR p_tax ? 'recoverableTaxPercent' THEN
                expected_tax := expected_tax || jsonb_build_object(
                    'itcEligibility',v.itc_eligibility,'recoverableTaxPercent',v.recoverable_tax_percent);
            ELSIF v.itc_eligibility IS DISTINCT FROM 'FULLY_RECOVERABLE' OR v.recoverable_tax_percent IS NOT NULL THEN
                RETURN FALSE;
            END IF;
            RETURN p_commercial->'input' IS NOT DISTINCT FROM expected_input AND
            """, StringComparison.Ordinal)
        .Replace("\r\n", "\n", StringComparison.Ordinal);
}
