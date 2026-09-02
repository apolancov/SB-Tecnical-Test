using Application.Common.Pagination;
using Domain.Entities;

namespace Application.Users;

public interface IUserAdministrationReadRepository
{
    Task<PaginatedResult<UserDto>> SearchAsync(
        UserQueryCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> UsernameExistsAsync(string username, Guid? excludingUserId, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, Guid? excludingUserId, CancellationToken cancellationToken);
}
