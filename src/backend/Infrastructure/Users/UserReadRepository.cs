using Application.Common.Persistence;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Users;

public sealed class UserReadRepository : IUserReadRepository
{
    private readonly ApplicationDbContext _context;

    public UserReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Task.FromResult<User?>(null);
        }

        var normalizedUsername = username.Trim();
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Username == normalizedUsername, cancellationToken);
    }
}
