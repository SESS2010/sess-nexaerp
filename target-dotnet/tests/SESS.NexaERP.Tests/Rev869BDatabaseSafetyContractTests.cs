using System.Text.RegularExpressions;

namespace SESS.NexaERP.Tests;

public sealed class Rev869BDatabaseSafetyContractTests
{
    private static string Command => Source("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs");
    private static string Controlled => Source("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs");
    private static string Migration => Source("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs");


    [Fact]
    public void TriggerContractUsesExactTransactionLocalContextAndSingleUseClaims()
    {
        Assert.Contains("rev869b_command_context_valid", Controlled);
        Assert.Contains("rev869b_claim_command_context", Controlled);
        Assert.Contains("UNIQUE(\"ContextToken\",\"ClaimKind\",\"HistoryId\")", Command);
        Assert.Contains("rev869b_exact_command_slot", Command);
        Assert.Contains("BackendPid\"=pg_backend_pid()", Command);
        Assert.Contains("TransactionId\"=txid_current()", Command);
        Assert.Contains("set_config('__advance_schema__.rev869b_command_token',token::text,true)", Command);
    }

    [Fact]
    public void DurableEvidenceRelationsAreAppendOnlyAndTerminalStatesAreClosed()
    {
        foreach (var trigger in new[] { "TR_rev869b_command_outcomes_immutable", "TR_rev869b_command_receipts_immutable", "TR_rev869b_purge_events_immutable", "TR_rev869b_export_rows_immutable" })
            Assert.Contains(trigger, Command);
        Assert.Contains("'Committed','Rejected','RolledBack','Abandoned'", Command);
        Assert.Contains("'ZeroRows','Started','Succeeded','Failed','Interrupted'", Command);
        Assert.Contains("'ReleaseStarted','Delivered','Failed','Interrupted'", Command);
    }

    [Fact]
    public void RemoveDropsFunctionsBeforeTheirLedgerRelations()
    {
        var remove = Command[Command.IndexOf("private const string RemoveTemplate", StringComparison.Ordinal)..];
        Assert.True(remove.IndexOf("DROP FUNCTION", StringComparison.Ordinal) < remove.IndexOf("DROP TABLE", StringComparison.Ordinal));
        Assert.True(remove.IndexOf("rev869b_command_contexts", StringComparison.Ordinal) < remove.LastIndexOf("rev869b_command_attempts", StringComparison.Ordinal));
        Assert.True(remove.LastIndexOf("rev869b_command_attempts", StringComparison.Ordinal) < remove.LastIndexOf("rev869b_command_requests", StringComparison.Ordinal));
    }

    [Fact]
    public void SecurityDefinerFunctionsPinSearchPathAndPublicExecuteIsClosed()
    {
        var definitions = Regex.Matches(Command, @"CREATE FUNCTION __advance_schema__\.(?<name>[a-z0-9_]+)\([^;]+?\$f\$;", RegexOptions.Singleline);
        Assert.NotEmpty(definitions);
        foreach (Match definition in definitions.Where(x => x.Value.Contains("SECURITY DEFINER", StringComparison.Ordinal)))
            Assert.Contains("SET search_path=pg_catalog,__advance_schema__", definition.Value);
        Assert.Contains("REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA __advance_schema__ FROM PUBLIC", Command);
        Assert.Contains("ALTER DEFAULT PRIVILEGES", Command);
    }

    [Fact]
    public void QualificationAndPurchaseHistoryRemainBoundToSameTransactionParents()
    {
        foreach (var invariant in new[] { "rev869b_history_parent_transition", "rev869b_approval_history_parent_transition", "rev869b_po_history_parent_transition", "rev869b_qualification_requires_history", "rev869b_creator_self_approval", "rev869b_verifier_approver_separation", "rev869b_issuer_approver_separation" })
            Assert.Contains(invariant, Controlled);
        Assert.Contains("xmin::text::bigint=txid_current()", Controlled);
    }

    [Fact]
    public void NoSupersededGrantRetryOrLiveExportDesignRemains()
    {
        foreach (var removed in new[] { "rev869b_command_grants", "rev869b_issue_command_grant", "REV869B_COMMAND_IDEMPOTENCY_KEY", "RetryEligible", "rev869b_export_minimized_security_ledger" })
            Assert.DoesNotContain(removed, Command);
    }

    private static string Source(string relative) => File.ReadAllText(Path.Combine(FindRoot(), relative.Replace('/', Path.DirectorySeparatorChar)));
    private static string FindRoot() { for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent) if (File.Exists(Path.Combine(d.FullName, "SESS.NexaERP.slnx"))) return d.FullName; throw new DirectoryNotFoundException(); }
}
