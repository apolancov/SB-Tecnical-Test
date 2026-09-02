using System.Text.RegularExpressions;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Database;

public class InstitutionSeedIntegrationTests
{
    private const int ExpectedInstitutionCount = 181;
    private const int ExpectedCategoryCount = 7;
    private const int ExpectedStatePowerCount = 1;
    private const int ExpectedSectorCount = 25;

    private static readonly string SeedSqlPath =
        Path.Combine(AppContext.BaseDirectory, "seed.sql");

    private static readonly Regex SeedInstitutionRegex = new(
        @"^    VALUES \(\s*NEWID\(\),\s*(N'(?:[^']|'')*'),\s*\(SELECT Id FROM dbo\.Categories WHERE Name = (N'(?:[^']|'')*')\),\s*\(SELECT Id FROM dbo\.StatePowers WHERE Name = (N'(?:[^']|'')*')\),\s*\(SELECT Id FROM dbo\.Sectors WHERE Name = (N'(?:[^']|'')*')\)\);",
        RegexOptions.Multiline);

    private static readonly Regex SeedClassificationRegex = new(
        @"^    VALUES \(NEWID\(\), (N'(?:[^']|'')*')\);",
        RegexOptions.Multiline);

    private static IReadOnlyList<SeedInstitution> LoadSeedInstitutions()
    {
        var seedSql = File.ReadAllText(SeedSqlPath);

        var matches = SeedInstitutionRegex.Matches(seedSql);

        var list = new List<SeedInstitution>(matches.Count);

        foreach (Match match in matches)
        {
            var name = UnwrapSqlString(match.Groups[1].Value);
            var category = UnwrapSqlString(match.Groups[2].Value);
            var statePower = UnwrapSqlString(match.Groups[3].Value);
            var sector = UnwrapSqlString(match.Groups[4].Value);

            list.Add(new SeedInstitution(name, category, statePower, sector));
        }

        return list;
    }

    private static IReadOnlyList<string> LoadSeedValues(string tableName)
    {
        var seedSql = File.ReadAllText(SeedSqlPath);
        var regex = new Regex(
            @"^IF NOT EXISTS \(SELECT 1 FROM dbo\." + Regex.Escape(tableName)
                + @" WHERE Name = (N'(?:[^']|'')*')\)",
            RegexOptions.Multiline);

        var names = new List<string>();
        foreach (Match match in regex.Matches(seedSql))
        {
            names.Add(UnwrapSqlString(match.Groups[1].Value));
        }

        return names;
    }

    private static string UnwrapSqlString(string literal)
    {
        Assert.StartsWith("N'", literal, StringComparison.Ordinal);
        Assert.EndsWith("'", literal, StringComparison.Ordinal);
        return literal.Substring(2, literal.Length - 3).Replace("''", "'");
    }

    private static ApplicationDbContext CreateSqliteContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void SeedInstitutions_AreLoadedFromSeedSql()
    {
        var institutions = LoadSeedInstitutions();

        Assert.Equal(ExpectedInstitutionCount, institutions.Count);
    }

    [Fact]
    public void SeedInstitutions_ContainKnownRecords()
    {
        var institutions = LoadSeedInstitutions();

        Assert.Contains(institutions, i => i.Name == "Acuario Nacional");
        Assert.Contains(institutions, i => i.Name == "Archivo General de la Nación");
        Assert.Contains(
            institutions,
            i => i.Name == "Jardín Botánico Nacional \"Dr. Rafael M. Moscoso\"");
        Assert.Contains(
            institutions,
            i => i.Name == "Museo Nacional de Historia Natural \"Prof. Eugenio de Jesús Marcano\"");
        Assert.Contains(
            institutions,
            i => i.Name == "Instituto de Educación Superior en Formación Diplomática y Consular “Dr. Eduardo Latorre Rodríguez”");
        Assert.Contains(
            institutions,
            i => i.Name == "Universidad Autónoma de Santo Domingo");
    }

    [Fact]
    public void SeedInstitutions_AllHaveDistinctNames()
    {
        var institutions = LoadSeedInstitutions();

        var names = institutions.Select(i => i.Name).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void SeedInstitutions_AllHaveNonEmptyClassificationFields()
    {
        var institutions = LoadSeedInstitutions();

        Assert.All(institutions, institution =>
        {
            Assert.False(string.IsNullOrWhiteSpace(institution.Name));
            Assert.False(string.IsNullOrWhiteSpace(institution.Category));
            Assert.False(string.IsNullOrWhiteSpace(institution.StatePower));
            Assert.False(string.IsNullOrWhiteSpace(institution.Sector));
        });
    }

    [Fact]
    public void SeedInstitutions_ReferenceLookupTableNames()
    {
        var institutions = LoadSeedInstitutions();
        var categories = LoadSeedValues("Categories").ToHashSet(StringComparer.Ordinal);
        var statePowers = LoadSeedValues("StatePowers").ToHashSet(StringComparer.Ordinal);
        var sectors = LoadSeedValues("Sectors").ToHashSet(StringComparer.Ordinal);

        Assert.Equal(ExpectedCategoryCount, categories.Count);
        Assert.Equal(ExpectedStatePowerCount, statePowers.Count);
        Assert.Equal(ExpectedSectorCount, sectors.Count);

        Assert.All(institutions, institution =>
        {
            Assert.Contains(institution.Category, categories);
            Assert.Contains(institution.StatePower, statePowers);
            Assert.Contains(institution.Sector, sectors);
        });
    }

    [Fact]
    public void SeedInstitutions_AreConstructibleThroughDomainEntity()
    {
        var institutions = LoadSeedInstitutions();

        foreach (var seed in institutions)
        {
            var exception = Record.Exception(() => new Institution(
                seed.Name,
                new Category(seed.Category),
                new StatePower(seed.StatePower),
                new Sector(seed.Sector)));

            Assert.Null(exception);
        }
    }

    [Fact]
    public void SeedInstitutions_CanBePersistedThroughEfCoreSqlite()
    {
        var institutions = LoadSeedInstitutions();

        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        using (var context = CreateSqliteContext(connection))
        {
            context.Database.EnsureCreated();

            SeedLookupRows(context, institutions);
            SeedInstitutions(context, institutions);

            context.SaveChanges();
        }

        using (var context = CreateSqliteContext(connection))
        {
            var actualCount = context.Institutions.Count();
            Assert.Equal(ExpectedInstitutionCount, actualCount);

            var acuario = context.Institutions
                .Include(i => i.Category)
                .Include(i => i.StatePower)
                .Include(i => i.Sector)
                .Single(i => i.Name == "Acuario Nacional");
            Assert.Equal("Organismo Descentralizado Funcionalmente", acuario.Category.Name);
            Assert.Equal("Poder Ejecutivo", acuario.StatePower.Name);
            Assert.Equal("Medio Ambiente y Recursos Naturales", acuario.Sector.Name);
            Assert.NotEqual(Guid.Empty, acuario.Id);

            var jardin = context.Institutions
                .Include(i => i.Category)
                .Include(i => i.StatePower)
                .Include(i => i.Sector)
                .Single(i => i.Name == "Jardín Botánico Nacional \"Dr. Rafael M. Moscoso\"");
            Assert.Equal("Medio Ambiente y Recursos Naturales", jardin.Sector.Name);
        }
    }

    [Fact]
    public void SeedInstitutions_HaveNonOverlappingIdentifiersAfterInsertion()
    {
        var institutions = LoadSeedInstitutions();

        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var ids = new List<Guid>();

        using (var context = CreateSqliteContext(connection))
        {
            context.Database.EnsureCreated();

            SeedLookupRows(context, institutions);
            SeedInstitutions(context, institutions);

            context.SaveChanges();

            ids = context.Institutions.Select(i => i.Id).ToList();
        }

        Assert.Equal(ExpectedInstitutionCount, ids.Count);
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void SeedInstitutions_ReinsertionIsBlockedByUniqueNameIndex()
    {
        var institutions = LoadSeedInstitutions();

        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        using (var context = CreateSqliteContext(connection))
        {
            context.Database.EnsureCreated();

            SeedLookupRows(context, institutions);
            SeedInstitutions(context, institutions);

            context.SaveChanges();

            context.Institutions.Add(new Institution(
                "Acuario Nacional",
                new Category("Otra Categoría"),
                new StatePower("Otro Poder del Estado"),
                new Sector("Otro Sector")));

            Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        }

        using (var context = CreateSqliteContext(connection))
        {
            var acuarioCount = context.Institutions.Count(i => i.Name == "Acuario Nacional");
            Assert.Equal(1, acuarioCount);
        }
    }

    [Fact]
    public void SeedInstitutions_PreservedAccentedAndQuotedNamesRoundTrip()
    {
        var institutions = LoadSeedInstitutions();

        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        using (var context = CreateSqliteContext(connection))
        {
            context.Database.EnsureCreated();

            SeedLookupRows(context, institutions);
            SeedInstitutions(context, institutions);

            context.SaveChanges();
        }

        using (var context = CreateSqliteContext(connection))
        {
            var namesFromDb = context.Institutions.Select(i => i.Name).ToList();

            var namesFromSeed = institutions.Select(i => i.Name).ToList();

            Assert.Equal(namesFromSeed.OrderBy(n => n, StringComparer.Ordinal), namesFromDb.OrderBy(n => n, StringComparer.Ordinal));
        }
    }

    private static void SeedLookupRows(
        ApplicationDbContext context,
        IReadOnlyList<SeedInstitution> institutions)
    {
        var categories = institutions
            .Select(item => item.Category)
            .Distinct(StringComparer.Ordinal)
            .Select(name => new Category(name))
            .ToList();

        var statePowers = institutions
            .Select(item => item.StatePower)
            .Distinct(StringComparer.Ordinal)
            .Select(name => new StatePower(name))
            .ToList();

        var sectors = institutions
            .Select(item => item.Sector)
            .Distinct(StringComparer.Ordinal)
            .Select(name => new Sector(name))
            .ToList();

        context.Categories.AddRange(categories);
        context.StatePowers.AddRange(statePowers);
        context.Sectors.AddRange(sectors);

        context.SaveChanges();
    }

    private static void SeedInstitutions(
        ApplicationDbContext context,
        IReadOnlyList<SeedInstitution> institutions)
    {
        var categoriesByName = context.Categories
            .ToDictionary(category => category.Name, StringComparer.Ordinal);
        var statePowersByName = context.StatePowers
            .ToDictionary(statePower => statePower.Name, StringComparer.Ordinal);
        var sectorsByName = context.Sectors
            .ToDictionary(sector => sector.Name, StringComparer.Ordinal);

        foreach (var seed in institutions)
        {
            context.Institutions.Add(new Institution(
                seed.Name,
                categoriesByName[seed.Category],
                statePowersByName[seed.StatePower],
                sectorsByName[seed.Sector]));
        }
    }

    private sealed record SeedInstitution(string Name, string Category, string StatePower, string Sector);
}