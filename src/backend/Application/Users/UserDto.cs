using Domain.Entities;
using Domain.Enums;

namespace Application.Users;

public sealed record UserDto(
    Guid Id,
    string Username,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);

public sealed record UserQueryCriteria(
    string? Username,
    string? Email,
    UserRole? Role,
    bool? IsActive);
