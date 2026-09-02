using Domain.Enums;

namespace Application.Users.GetUsers;

public sealed record GetUsersQuery(
    int Page,
    int PageSize,
    string? Username,
    string? Email,
    Domain.Enums.UserRole? Role,
    bool? IsActive);
