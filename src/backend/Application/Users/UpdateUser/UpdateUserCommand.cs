using Domain.Enums;

namespace Application.Users.UpdateUser;

public sealed record UpdateUserCommand(
    Guid Id,
    string Username,
    string Email,
    UserRole Role,
    bool IsActive);
