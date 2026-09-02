using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Api.Functional;

internal static class TestTokenFactory
{
    public const string TestIssuer = "SB.Api.Tests";
    public const string TestAudience = "SB.Client.Tests";
    public const string TestSecretKey = "TEST_SECRET_KEY_AT_LEAST_THIRTYTWO_CHARACTERS_LONG";

    public static string CreateToken(
        Guid userId,
        string username,
        string role,
        DateTime? expiresAt = null,
        DateTime? notBefore = null,
        string? issuer = null,
        string? audience = null)
    {
        var now = DateTime.UtcNow;
        var expires = expiresAt ?? now.AddMinutes(60);
        var nbf = notBefore ?? now;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer ?? TestIssuer,
            audience: audience ?? TestAudience,
            claims: claims,
            notBefore: nbf,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateExpiredToken(Guid userId, string username, string role)
    {
        var past = DateTime.UtcNow.AddHours(-1);
        return CreateToken(userId, username, role, expiresAt: past.AddMinutes(30), notBefore: past);
    }
}
