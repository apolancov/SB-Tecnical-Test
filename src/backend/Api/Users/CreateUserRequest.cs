using Domain.Enums;

namespace Api.Users;

public sealed class CreateUserRequest
{
    public string? Username { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public UserRole? Role { get; set; }

    public bool IsActive { get; set; } = true;
}
