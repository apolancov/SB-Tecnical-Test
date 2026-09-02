using Domain.Enums;

namespace Api.Users;

public sealed class UpdateUserRequest
{
    public string? Username { get; set; }

    public string? Email { get; set; }

    public UserRole? Role { get; set; }

    public bool IsActive { get; set; }
}
