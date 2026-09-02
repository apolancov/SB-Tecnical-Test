using Domain.Entities;

namespace Application.Common.Persistence;

public interface IUserReadRepository
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
}
