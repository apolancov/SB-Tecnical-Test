using Application.Common.Pagination;
using Application.Users;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Users;

public sealed class UserAdministrationReadRepository : IUserAdministrationReadRepository
{
    private readonly ApplicationDbContext _context;

    public UserAdministrationReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<UserDto>> SearchAsync(
        UserQueryCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.Users
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, criteria);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(user => user.Username)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserDto(
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.IsActive,
                user.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<UserDto>(items, page, pageSize, totalItems);
    }

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Task.FromResult<User?>(null);
        }

        return _context.Users
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(string username, Guid? excludingUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Task.FromResult(false);
        }

        var normalized = username.Trim();
        var query = _context.Users.AsNoTracking()
            .Where(user => user.Username == normalized);

        if (excludingUserId.HasValue && excludingUserId.Value != Guid.Empty)
        {
            query = query.Where(user => user.Id != excludingUserId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludingUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult(false);
        }

        var normalized = email.Trim();
        var query = _context.Users.AsNoTracking()
            .Where(user => user.Email == normalized);

        if (excludingUserId.HasValue && excludingUserId.Value != Guid.Empty)
        {
            query = query.Where(user => user.Id != excludingUserId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    private static IQueryable<User> ApplyFilters(
        IQueryable<User> query,
        UserQueryCriteria criteria)
    {
        if (criteria.Username is not null)
        {
            var usernameFilter = criteria.Username;
            query = query.Where(user =>
                EF.Functions.Like(user.Username, $"%{usernameFilter}%"));
        }

        if (criteria.Email is not null)
        {
            var emailFilter = criteria.Email;
            query = query.Where(user =>
                EF.Functions.Like(user.Email, $"%{emailFilter}%"));
        }

        if (criteria.Role.HasValue)
        {
            var role = criteria.Role.Value;
            query = query.Where(user => user.Role == role);
        }

        if (criteria.IsActive.HasValue)
        {
            var isActive = criteria.IsActive.Value;
            query = query.Where(user => user.IsActive == isActive);
        }

        return query;
    }
}
