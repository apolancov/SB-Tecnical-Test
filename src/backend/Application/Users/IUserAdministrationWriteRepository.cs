using Domain.Entities;

namespace Application.Users;

public interface IUserAdministrationWriteRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);

    void Update(User user);

    void Remove(User user);
}
