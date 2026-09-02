using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Requests;
using Domain.Enums;

namespace Api.Authentication;

public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            return Guid.Empty;
        }

        var subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (subject is null || !Guid.TryParse(subject, out var userId))
        {
            return Guid.Empty;
        }

        return userId;
    }

    public UserRole GetCurrentUserRole()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            return UserRole.User;
        }

        var roleClaim = user.FindFirstValue(ClaimTypes.Role);
        if (Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return role;
        }

        return UserRole.User;
    }
}
