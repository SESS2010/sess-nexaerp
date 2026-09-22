using SESS.NexaERP.Domain.Stores;
namespace SESS.NexaERP.Tests;
public sealed class ToolOpeningRegisterReconciliationTests
{
    private static readonly ToolOpeningTypeRow[] Types =
        [new("Types!2", "SPANNER", 5, 3, 2), new("Types!3", "WRENCH", 3, 2, 1)];
    private static readonly ToolOpeningCustodyRow[] Custody =
        [new("E1!2", "SPANNER", "EMP-1", 2), new("E2!2", "SPANNER", "EMP-2", 1), new("E1!3", "WRENCH", "EMP-1", 2)];
    private static readonly ToolOpeningRegisterTotals Expected = new(2, 8, 5, 3, 3, 2);

    [Fact]
    public void Custody_source_lines_and_individual_issued_assets_are_different_counts()
    {
        var result = ToolOpeningRegisterReconciliation.Reconcile(Types, Custody, Expected);
        Assert.True(result.IsBalanced); Assert.Empty(result.Errors);
        Assert.Equal(3, result.Totals.CustodyLines); Assert.Equal(5, result.Totals.Issued);
    }
    [Fact]
    public void Balanced_grand_totals_cannot_hide_wrong_per_type_custody()
    {
        var rows = Custody.ToArray(); rows[0] = rows[0] with { Quantity = 1 };
        rows[2] = rows[2] with { Quantity = 3 };
        var result = ToolOpeningRegisterReconciliation.Reconcile(Types, rows, Expected);
        Assert.False(result.IsBalanced); Assert.Equal(Expected, result.Totals);
        Assert.Contains(result.Errors, x => x.Contains("SPANNER: custody quantity 2 differs from issued 3"));
        Assert.Contains(result.Errors, x => x.Contains("WRENCH: custody quantity 3 differs from issued 2"));
    }
    [Fact]
    public void Source_row_repeated_in_a_parse_is_not_silently_double_counted()
    {
        var result = ToolOpeningRegisterReconciliation.Reconcile(Types, [.. Custody, Custody[0]], Expected);
        Assert.False(result.IsBalanced);
        Assert.Contains(result.Errors, x => x.Contains("Duplicate custody source row"));
    }
    [Fact]
    public void Unknown_type_or_missing_holder_is_reported()
    {
        var rows = Custody.ToArray(); rows[0] = rows[0] with { ToolTypeCode = "UNKNOWN", HolderReference = "" };
        var result = ToolOpeningRegisterReconciliation.Reconcile(Types, rows, Expected);
        Assert.False(result.IsBalanced);
        Assert.Contains(result.Errors, x => x.Contains("unknown tool type UNKNOWN"));
        Assert.Contains(result.Errors, x => x.Contains("holder and positive individual quantity"));
    }
    [Fact]
    public void Incorrect_type_balance_is_rejected_even_with_matching_supplied_totals()
    {
        var rows = Types.ToArray(); rows[0] = rows[0] with { Balance = 3 };
        var result = ToolOpeningRegisterReconciliation.Reconcile(rows, Custody, Expected with { Balance = 4 });
        Assert.False(result.IsBalanced);
        Assert.Contains(result.Errors, x => x.Contains("purchased minus issued"));
    }
    [Fact]
    public void Small_synthetic_fixture_never_claims_to_be_the_real_sess_register()
    {
        var result = ToolOpeningRegisterReconciliation.Reconcile(Types, Custody, ToolOpeningRegisterReconciliation.SessExpected);
        Assert.False(result.IsBalanced);
        Assert.Contains(result.Errors, x => x.Contains("Register control totals differ"));
    }
    [Fact]
    public void Duplicate_tool_type_requires_mapping_correction_instead_of_merging_silently()
    {
        var result = ToolOpeningRegisterReconciliation.Reconcile([Types[0], Types[0] with { SourceRow = "Types!4" }], Custody, Expected);
        Assert.Contains(result.Errors, x => x.Contains("Duplicate tool type code: SPANNER"));
    }
}
