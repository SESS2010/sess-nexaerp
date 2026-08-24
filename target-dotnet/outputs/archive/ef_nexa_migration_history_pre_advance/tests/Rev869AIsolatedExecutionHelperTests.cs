// Inert preserved source: excluded from SESS.NexaERP.Tests by location.
// Original active-test source: tests/SESS.NexaERP.Tests/Rev869AIsolatedExecutionHelperTests.cs
// These assertions describe the archived pre-advance EF migration history beside this folder.
#if false
    [Fact]
    public void BackupsCoverEveryAlteredMasterBeforeMutationAndDropLast()
    {
        var firstMutation = MigrationSource.IndexOf("migrationBuilder.AddColumn", StringComparison.Ordinal);
        Assert.True(firstMutation > 0);
        foreach (var backup in new[] { "rev869a_items_prechange_backup", "rev869a_uoms_prechange_backup", "rev869a_vendors_prechange_backup" })
        {
            Assert.True(MigrationSource.IndexOf(backup, StringComparison.Ordinal) < firstMutation);
            Assert.True(MigrationSource.LastIndexOf(backup, StringComparison.Ordinal) > MigrationSource.LastIndexOf("migrationBuilder.DropColumn", StringComparison.Ordinal));
        }
    }
#endif
