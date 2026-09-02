namespace Api.Common.Cors;

public sealed class CorsSettings
{
    public const string ConfigurationSectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();

    public bool IsValid => AllowedOrigins.Length > 0;
}