namespace Application.Common.Authentication;

public sealed class JwtSettings
{
    public const string ConfigurationSectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Issuer)
        && !string.IsNullOrWhiteSpace(Audience)
        && !string.IsNullOrWhiteSpace(SecretKey)
        && SecretKey.Length >= MinimumSecretKeyLength
        && ExpirationMinutes > 0;

    public const int MinimumSecretKeyLength = 32;
}
