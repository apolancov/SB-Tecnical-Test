using System.Text.RegularExpressions;
using Xunit;

namespace Database;

public class RequestSeedSqlFileTests
{
    private static readonly string SeedSqlPath =
        Path.Combine(AppContext.BaseDirectory, "seed.sql");

    private static string ReadSeedSql() => File.ReadAllText(SeedSqlPath);

    private const int ExpectedAreaCount = 6;
    private const int ExpectedRequestTypeCount = 6;

    [Fact]
    public void SeedSqlFile_ContainsExpectedAreaRecordCount()
    {
        var seedSql = ReadSeedSql();

        var insertGuards = Regex.Matches(
            seedSql,
            @"^IF NOT EXISTS \(SELECT 1 FROM dbo\.Areas WHERE Name = ",
            RegexOptions.Multiline).Count;

        Assert.Equal(ExpectedAreaCount, insertGuards);
    }

    [Fact]
    public void SeedSqlFile_ContainsExpectedRequestTypeRecordCount()
    {
        var seedSql = ReadSeedSql();

        var insertGuards = Regex.Matches(
            seedSql,
            @"^IF NOT EXISTS \(SELECT 1 FROM dbo\.RequestTypes WHERE Name = ",
            RegexOptions.Multiline).Count;

        Assert.Equal(ExpectedRequestTypeCount, insertGuards);
    }

    [Fact]
    public void SeedSqlFile_AllAreasHaveIsActiveTrue()
    {
        var seedSql = ReadSeedSql();

        var areaInserts = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.Areas \(Id, Name, IsActive\)",
            RegexOptions.Multiline).Count;

        var activeInserts = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.Areas \(Id, Name, IsActive\)\s+VALUES \(NEWID\(\), N'[^']+',\s*1\);",
            RegexOptions.Multiline).Count;

        Assert.Equal(areaInserts, activeInserts);
        Assert.Equal(ExpectedAreaCount, areaInserts);
    }

    [Fact]
    public void SeedSqlFile_AllRequestTypesHaveIsActiveTrue()
    {
        var seedSql = ReadSeedSql();

        var requestTypeInserts = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.RequestTypes \(Id, Name, Description, IsActive\)",
            RegexOptions.Multiline).Count;

        var activeInserts = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.RequestTypes \(Id, Name, Description, IsActive\)\s+VALUES \(NEWID\(\), N'[^']+', N'[^']*',\s*1\);",
            RegexOptions.Multiline).Count;

        Assert.Equal(requestTypeInserts, activeInserts);
        Assert.Equal(ExpectedRequestTypeCount, requestTypeInserts);
    }

    [Fact]
    public void SeedSqlFile_ContainsKnownAreas()
    {
        var seedSql = ReadSeedSql();

        Assert.Contains("N'Atención al Ciudadano'", seedSql, StringComparison.Ordinal);
        Assert.Contains("N'Soporte Técnico'", seedSql, StringComparison.Ordinal);
        Assert.Contains("N'Mantenimiento'", seedSql, StringComparison.Ordinal);
    }

    [Fact]
    public void SeedSqlFile_ContainsKnownRequestTypes()
    {
        var seedSql = ReadSeedSql();

        Assert.Contains("N'Incidente'", seedSql, StringComparison.Ordinal);
        Assert.Contains("N'Requerimiento'", seedSql, StringComparison.Ordinal);
        Assert.Contains("N'Consulta'", seedSql, StringComparison.Ordinal);
    }

    [Fact]
    public void SeedSqlFile_PreservesAccentedCharacters()
    {
        var seedSql = ReadSeedSql();

        Assert.Contains("N'Atención al Ciudadano'", seedSql, StringComparison.Ordinal);
        Assert.Contains("N'Requerimiento'", seedSql, StringComparison.Ordinal);
    }
}
