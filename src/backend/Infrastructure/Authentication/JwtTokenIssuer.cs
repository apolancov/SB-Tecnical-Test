using Application.Common.Authentication;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Authentication;

public sealed class JwtTokenIssuer : IJwtTokenIssuer
{
    private const string BearerTokenType = "Bearer";
    private const string NameClaimType = JwtRegisteredClaimNames.UniqueName;
    private const string RoleClaimType = ClaimTypes.Role;
    private const string IdentifierClaimType = JwtRegisteredClaimNames.Sub;
    private const string EmailClaimType = JwtRegisteredClaimNames.Email;

    private readonly JwtSettings _settings;

    public JwtTokenIssuer(IOptions<JwtSettings> options)
    {
        _settings = options.Value;

        if (!_settings.IsValid)
        {
            throw new InvalidOperationException(
                "JWT settings are missing or invalid. "
                + "Configure Jwt:Issuer, Jwt:Audience, Jwt:SecretKey (>= 32 chars) "
                + "and Jwt:ExpirationMinutes (> 0) under the 'Jwt' section.");
        }
    }

    public AuthenticationTokenIssue IssueFor(User user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_settings.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(IdentifierClaimType, user.Id.ToString()),
            new(NameClaimType, user.Username),
            new(EmailClaimType, user.Email),
            new(RoleClaimType, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthenticationTokenIssue(
            AccessToken: accessToken,
            TokenType: BearerTokenType,
            ExpiresAtUtc: expiresAt,
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            Role: user.Role);
    }
}
