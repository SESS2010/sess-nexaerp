namespace SESS.NexaERP.Tests;

public sealed class VerifiedBackupNormalizationTests
{
    [Fact]
    public void EquivalentUnboundedArrayCastsMatchWithoutChangingValues()
    {
        const string source="CHECK (status::text = ANY ((ARRAY['A'::character varying, 'O''Brien::character varying'::character varying])::text[]))";
        const string restored="CHECK (status::text = ANY (ARRAY[('A'::character varying)::text, ('O''Brien::character varying'::character varying)::text]))";
        var normalized=BackupDatabaseSnapshot.NormalizeSqlDefinition(source);
        Assert.Equal(normalized,BackupDatabaseSnapshot.NormalizeSqlDefinition(restored));
        Assert.Contains("'O''Brien::character varying'::text",normalized,StringComparison.Ordinal);
        Assert.NotEqual(normalized,BackupDatabaseSnapshot.NormalizeSqlDefinition(restored.Replace("'A'","'B'",StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("CHECK ((value)::text = ('ABCDEFG'::character varying(2))::text)")]
    [InlineData("CHECK (\"ARRAY['A'::character varying]::text[]\" IS NOT NULL)")]
    [InlineData("CHECK (value = 'text ARRAY[''A''::character varying]::text[]')")]
    [InlineData("CHECK (myARRAY['A'::character varying]::text[] IS NOT NULL)")]
    public void BoundedCastsQuotedTextAndIdentifierNamesRemainUntouched(string definition)
    {
        Assert.Equal(definition,BackupDatabaseSnapshot.NormalizeSqlDefinition(definition));
    }
}
