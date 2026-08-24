// Inert preserved source: excluded from SESS.NexaERP.Tests by location.
// Original active-test source: tests/SESS.NexaERP.Tests/Rev869AIsolatedDatabasePreparationHelperTests.cs
// These assertions describe the archived pre-advance EF migration history beside this folder.
#if false

    [Fact]
    public void RelievedStatusNormalizationMatchesCommittedRev868C3Sources()
    {
        Assert.Contains("@('left / resigned','left/resigned','resigned','inactive')", Source, StringComparison.Ordinal);
        Assert.Contains("lower(\"Status\") as normalized_status", Source, StringComparison.Ordinal);
        Assert.Contains("lower(\"Status\") in ('left / resigned','left/resigned','resigned','inactive')", Rev868C3Verifier, StringComparison.Ordinal);
        Assert.Contains("set \"Status\" = 'Left / Resigned'", Rev868C3Migration, StringComparison.Ordinal);
        Assert.Contains("new(code, name, \"LEFT / RESIGNED\"", Rev868C3WorkbookData, StringComparison.Ordinal);
        Assert.True(RelievedSetPass(ExpectedRelievedEmployeeCodes.Select((code, index) =>
            new EmployeeStatusRow(code, new[] { "Left / Resigned", "LEFT/RESIGNED", "Resigned", "Inactive" }[index % 4]))));
    }
#endif
