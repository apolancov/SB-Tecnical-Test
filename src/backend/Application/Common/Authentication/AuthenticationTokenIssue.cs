using Domain.Enums;

namespace Application.Common.Authentication;

public sealed record AuthenticationTokenIssue(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Username,
    string Email,
    UserRole Role);
