using Domain.Entities;

namespace Application.Common.Users;

public interface IUserLookupRepository
{
    Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);
}
