using Application.Common.Users;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Users;

public sealed class UserLookupRepository : IUserLookupRepository
{
    private readonly ApplicationDbContext _context;

    public UserLookupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Task.FromResult<User?>(null);
        }

        return _context.Users
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }
}
