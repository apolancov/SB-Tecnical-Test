using Domain.Enums;

namespace Application.Authentication.AuthenticateUser;

public sealed record AuthenticatedUserDto(
    Guid Id,
    string Username,
    string Email,
    UserRole Role);
