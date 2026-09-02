using System.Text.RegularExpressions;
using Xunit;

namespace Database;

public class SeedSqlFileTests
{
    private const int ExpectedInstitutionCount = 181;
    private const int ExpectedCategoryCount = 7;
    private const int ExpectedStatePowerCount = 1;
    private const int ExpectedSectorCount = 25;

    private static readonly string SeedSqlPath =
        Path.Combine(AppContext.BaseDirectory, "seed.sql");

    private static string ReadSeedSql() =>
        File.ReadAllText(SeedSqlPath);

    [Fact]
    public void SeedSqlFile_Exists()
    {
        Assert.True(File.Exists(SeedSqlPath), $"Expected seed file at '{SeedSqlPath}'.");
    }

    [Fact]
    public void SeedSqlFile_HasExpectedInstitutionRecordCount()
    {
        var seedSql = ReadSeedSql();

        var insertGuards = Regex.Matches(
            seedSql,
            @"^IF NOT EXISTS \(SELECT 1 FROM dbo\.Institutions WHERE Name = ",
            RegexOptions.Multiline).Count;

        Assert.Equal(ExpectedInstitutionCount, insertGuards);
    }

    [Fact]
    public void SeedSqlFile_HasExpectedCategoryRecordCount()
    {
        var seedSql = ReadSeedSql();

        var insertGuards = Regex.Matches(
            seedSql,
            @"^IF NOT EXISTS \(SELECT 1 FROM dbo\.Categories WHERE Name = ",
            RegexOptions.Multiline).Count;

        Assert.Equal(ExpectedCategoryCount, insertGuards);
    }

    [Fact]
    public void SeedSqlFile_HasExpectedStatePowerRecordCount()
    {
        var seedSql = ReadSeedSql();

        var insertGuards = Regex.Matches(
            seedSql,
            @"^IF NOT EXISTS \(SELECT 1 FROM dbo\.StatePowers WHERE Name = ",
            RegexOptions.Multiline).Count;

        Assert.Equal(ExpectedStatePowerCount, insertGuards);
    }

    [Fact]
    public void SeedSqlFile_HasExpectedSectorRecordCount()
    {
        var seedSql = ReadSeedSql();

        var insertGuards = Regex.Matches(
            seedSql,
            @"^IF NOT EXISTS \(SELECT 1 FROM dbo\.Sectors WHERE Name = ",
            RegexOptions.Multiline).Count;

        Assert.Equal(ExpectedSectorCount, insertGuards);
    }

    [Fact]
    public void SeedSqlFile_AllRecordsUseUniqueNameGuard()
    {
        var seedSql = ReadSeedSql();

        var totalRecords = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.Institutions \(Id, Name, CategoryId, StatePowerId, SectorId\)",
            RegexOptions.Multiline).Count;

        var guards = Regex.Matches(
            seedSql,
            @"^IF NOT EXISTS \(SELECT 1 FROM dbo\.Institutions",
            RegexOptions.Multiline).Count;

        Assert.Equal(totalRecords, guards);
        Assert.Equal(ExpectedInstitutionCount, totalRecords);
    }

    [Fact]
    public void SeedSqlFile_AllRecordsHaveBeginEndBlock()
    {
        var seedSql = ReadSeedSql();

        var begins = Regex.Matches(seedSql, @"^BEGIN\s*$", RegexOptions.Multiline).Count;
        var ends = Regex.Matches(seedSql, @"^END;\s*$", RegexOptions.Multiline).Count;

        var institutionInserts = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.Institutions \(Id, Name, CategoryId, StatePowerId, SectorId\)",
            RegexOptions.Multiline).Count;
        var categoryInserts = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.Categories",
            RegexOptions.Multiline).Count;
        var statePowerInserts = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.StatePowers",
            RegexOptions.Multiline).Count;
        var sectorInserts = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.Sectors",
            RegexOptions.Multiline).Count;
        var userInserts = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.Users \(Id, Username, Email, PasswordHash, Role, IsActive\)",
            RegexOptions.Multiline).Count;
        var areaInserts = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.Areas \(Id, Name, IsActive\)",
            RegexOptions.Multiline).Count;
        var requestTypeInserts = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.RequestTypes \(Id, Name, Description, IsActive\)",
            RegexOptions.Multiline).Count;

        var totalInserts = institutionInserts + categoryInserts + statePowerInserts
            + sectorInserts + userInserts + areaInserts + requestTypeInserts;

        Assert.Equal(ExpectedInstitutionCount, institutionInserts);
        Assert.Equal(totalInserts, begins);
        Assert.Equal(totalInserts, ends);
    }

    [Fact]
    public void SeedSqlFile_HasGoSeparators()
    {
        var seedSql = ReadSeedSql();

        var goCount = Regex.Matches(seedSql, @"^GO\s*$", RegexOptions.Multiline).Count;

        Assert.True(
            goCount >= ExpectedInstitutionCount,
            $"Expected at least {ExpectedInstitutionCount} GO separators, found {goCount}.");
    }

    [Fact]
    public void SeedSqlFile_AllInstitutionNamesAreUnique()
    {
        var seedSql = ReadSeedSql();

        var names = Regex.Matches(
                seedSql,
                @"^IF NOT EXISTS \(SELECT 1 FROM dbo\.Institutions WHERE Name = N'((?:[^']|'')*)'\)",
                RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value.Replace("''", "'"))
            .ToList();

        Assert.Equal(ExpectedInstitutionCount, names.Count);
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void SeedSqlFile_ContainsKnownInstitution()
    {
        var seedSql = ReadSeedSql();

        Assert.Contains("Acuario Nacional", seedSql, StringComparison.Ordinal);
        Assert.Contains("Archivo General de la Nación", seedSql, StringComparison.Ordinal);
        Assert.Contains("Universidad Autónoma de Santo Domingo", seedSql, StringComparison.Ordinal);
    }

    [Fact]
    public void SeedSqlFile_PreservesAccentedCharacters()
    {
        var seedSql = ReadSeedSql();

        Assert.Contains("Acuario Nacional", seedSql, StringComparison.Ordinal);
        Assert.Contains("Administración", seedSql, StringComparison.Ordinal);
        Assert.Contains("Nación", seedSql, StringComparison.Ordinal);
        Assert.Contains("Educación", seedSql, StringComparison.Ordinal);
        Assert.Contains("Órgano Colegiado", seedSql, StringComparison.Ordinal);
    }

    [Fact]
    public void SeedSqlFile_PreservesStraightDoubleQuotes()
    {
        var seedSql = ReadSeedSql();

        Assert.Contains(
            "N'Jardín Botánico Nacional \"Dr. Rafael M. Moscoso\"'",
            seedSql,
            StringComparison.Ordinal);

        Assert.Contains(
            "N'Museo Nacional de Historia Natural \"Prof. Eugenio de Jesús Marcano\"'",
            seedSql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SeedSqlFile_PreservesCurlyDoubleQuotes()
    {
        var seedSql = ReadSeedSql();

        Assert.Contains(
            "“Dr. Eduardo Latorre Rodríguez”",
            seedSql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SeedSqlFile_TargetsExpectedTables()
    {
        var seedSql = ReadSeedSql();

        Assert.Contains("dbo.Categories", seedSql, StringComparison.Ordinal);
        Assert.Contains("dbo.StatePowers", seedSql, StringComparison.Ordinal);
        Assert.Contains("dbo.Sectors", seedSql, StringComparison.Ordinal);
        Assert.Contains("dbo.Institutions", seedSql, StringComparison.Ordinal);
        Assert.Contains("dbo.Users", seedSql, StringComparison.Ordinal);
    }

    [Fact]
    public void SeedSqlFile_InstitutionInsertsReferenceClassificationTables()
    {
        var seedSql = ReadSeedSql();

        var institutionInsertCount = Regex.Matches(
            seedSql,
            @"INSERT INTO dbo\.Institutions \(Id, Name, CategoryId, StatePowerId, SectorId\)",
            RegexOptions.Multiline).Count;

        var categoryReferenceCount = Regex.Matches(
            seedSql,
            @"SELECT Id FROM dbo\.Categories WHERE Name = ",
            RegexOptions.Multiline).Count;

        var statePowerReferenceCount = Regex.Matches(
            seedSql,
            @"SELECT Id FROM dbo\.StatePowers WHERE Name = ",
            RegexOptions.Multiline).Count;

        var sectorReferenceCount = Regex.Matches(
            seedSql,
            @"SELECT Id FROM dbo\.Sectors WHERE Name = ",
            RegexOptions.Multiline).Count;

        Assert.Equal(ExpectedInstitutionCount, institutionInsertCount);
        Assert.Equal(ExpectedInstitutionCount, categoryReferenceCount);
        Assert.Equal(ExpectedInstitutionCount, statePowerReferenceCount);
        Assert.Equal(ExpectedInstitutionCount, sectorReferenceCount);
    }

    [Fact]
    public void SeedSqlFile_HasNoApostrophesInsideLiteralValues()
    {
        var seedSql = ReadSeedSql();

        var names = Regex.Matches(
                seedSql,
                @"^IF NOT EXISTS \(SELECT 1 FROM dbo\.Institutions WHERE Name = N'((?:[^']|'')*)'\)",
                RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value);

        Assert.All(names, name =>
        {
            Assert.False(
                name.Contains("'", StringComparison.Ordinal),
                $"Found unescaped single quote inside literal: {name}");
        });
    }

    [Fact]
    public void SeedSqlFile_HasDocumentationHeader()
    {
        var seedSql = ReadSeedSql();

        Assert.StartsWith("--", seedSql, StringComparison.Ordinal);
        Assert.Contains("XLSX", seedSql, StringComparison.Ordinal);
        Assert.Contains("deterministic", seedSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Idempotency", seedSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SeedSqlFile_AllRecordsHaveFourClassificationColumnsViaSelect()
    {
        var seedSql = ReadSeedSql();

        var rows = Regex.Matches(
            seedSql,
            @"^    VALUES \(\s*NEWID\(\),\s*(N'(?:[^']|'')*'),\s*\(SELECT Id FROM dbo\.Categories WHERE Name = (N'(?:[^']|'')*')\),\s*\(SELECT Id FROM dbo\.StatePowers WHERE Name = (N'(?:[^']|'')*')\),\s*\(SELECT Id FROM dbo\.Sectors WHERE Name = (N'(?:[^']|'')*')\)\);",
            RegexOptions.Multiline);

        Assert.Equal(ExpectedInstitutionCount, rows.Count);

        foreach (Match match in rows)
        {
            Assert.False(string.IsNullOrWhiteSpace(match.Groups[1].Value));
            Assert.False(string.IsNullOrWhiteSpace(match.Groups[2].Value));
            Assert.False(string.IsNullOrWhiteSpace(match.Groups[3].Value));
            Assert.False(string.IsNullOrWhiteSpace(match.Groups[4].Value));
        }
    }
}