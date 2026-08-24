// Inert preserved source: excluded from SESS.NexaERP.Tests by location.
// Original active-test source: tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs
// These assertions describe the archived pre-advance EF migration history beside this folder.
#if false

    [Fact]
    public void MigrationInstallsAndRemovesTheSingleFrozenLedgerPackage()
    {
        Assert.Single(Regex.Matches(Migration, @"Rev869BCommandContextSql\.Install"));
        Assert.Single(Regex.Matches(Migration, @"Rev869BCommandContextSql\.Remove"));
        Assert.DoesNotContain("CreateTable(\n                name: \"rev869b_command_requests\"", Migration);
    }
#endif
