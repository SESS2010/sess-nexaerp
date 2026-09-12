

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class ControlledCompanyReportSql
{
    internal const string Signature = "(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)";
    private static readonly string[] Parameters =
        ["organization","employee","assignments","export","login","correlation","from_date","to_date","mode","metric","group_filter","offset","page_size","report_timezone"];

    internal static string Invocation(string key) =>
        $"SELECT kind,ordinal,payload FROM advance.{Name(key)}({string.Join(",",Parameters.Select(parameter => "@" + parameter))}) ORDER BY kind,ordinal";

    private static string Name(string key) => key switch
    {
        "grni" => "company_report_grni",
        "vendor-purchases" => "company_report_vendor_purchases",
        "fifo-valuation" => "company_report_fifo_valuation",
        "pending-approvals" => "company_report_pending_approvals",
        "purchase-register" => "company_report_purchase_register",
        _ => throw new ArgumentOutOfRangeException(nameof(key))
    };

    // This migration is a fixed snapshot. Later report changes need a new migration.
    internal static string Up
    {
        get
        {
            using var stream=typeof(ControlledCompanyReportSql).Assembly
                .GetManifestResourceStream("CompanyReports.20260913010000.sql")
                ?? throw new InvalidOperationException("The company-report migration SQL resource is missing.");
            using var reader=new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    internal static string Down => string.Join("\n",new[] { "grni","vendor-purchases","purchase-register","pending-approvals","fifo-valuation" }.Select(key =>
        $"DROP FUNCTION advance.{Name(key)}{Signature};"));
}
