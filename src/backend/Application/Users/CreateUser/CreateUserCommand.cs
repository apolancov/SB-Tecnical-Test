using Domain.Enums;

namespace Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    UserRole Role,
    bool IsActive);
