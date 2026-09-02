using System.Text.RegularExpressions;

namespace Api.Functional;

internal static class SampleSeedProvider
{
    private static readonly string SeedSqlPath =
        Path.Combine(AppContext.BaseDirectory, "seed.sql");

    private static readonly Regex ValuesRegex = new(
        @"^    VALUES \(\s*NEWID\(\),\s*(N'(?:[^']|'')*'),\s*\(SELECT Id FROM dbo\.Categories WHERE Name = (N'(?:[^']|'')*')\),\s*\(SELECT Id FROM dbo\.StatePowers WHERE Name = (N'(?:[^']|'')*')\),\s*\(SELECT Id FROM dbo\.Sectors WHERE Name = (N'(?:[^']|'')*')\)\);",
        RegexOptions.Multiline);

    public static IReadOnlyList<SeedInstitution> LoadFromSeedSql()
    {
        var seedSql = File.ReadAllText(SeedSqlPath);
        var matches = ValuesRegex.Matches(seedSql);

        var list = new List<SeedInstitution>(matches.Count);

        foreach (Match match in matches)
        {
            list.Add(new SeedInstitution(
                Unwrap(match.Groups[1].Value),
                Unwrap(match.Groups[2].Value),
                Unwrap(match.Groups[3].Value),
                Unwrap(match.Groups[4].Value)));
        }

        return list;
    }

    private static string Unwrap(string literal)
    {
        return literal.Substring(2, literal.Length - 3).Replace("''", "'");
    }

    internal sealed record SeedInstitution(
        string Name,
        string Category,
        string StatePower,
        string Sector);
}