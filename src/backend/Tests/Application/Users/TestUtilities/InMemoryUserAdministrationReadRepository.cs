using Application.Common.Pagination;
using Application.Users;
using Domain.Entities;

namespace Application.Users.TestUtilities;

public sealed class InMemoryUserAdministrationReadRepository : IUserAdministrationReadRepository
{
    private readonly List<User> _users;
    private readonly Dictionary<Guid, User> _byId;

    public InMemoryUserAdministrationReadRepository(IEnumerable<User> users)
    {
        _users = users.ToList();
        _byId = _users.ToDictionary(user => user.Id);
    }

    public int SearchCallCount { get; private set; }

    public int FindByIdCallCount { get; private set; }

    public IReadOnlyList<User> Users => _users;

    public Task<PaginatedResult<UserDto>> SearchAsync(
        UserQueryCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        SearchCallCount++;

        IEnumerable<User> filtered = _users;

        if (!string.IsNullOrWhiteSpace(criteria.Username))
        {
            var term = criteria.Username;
            filtered = filtered.Where(user =>
                user.Username.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Email))
        {
            var term = criteria.Email;
            filtered = filtered.Where(user =>
                user.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.Role.HasValue)
        {
            var role = criteria.Role.Value;
            filtered = filtered.Where(user => user.Role == role);
        }

        if (criteria.IsActive.HasValue)
        {
            var isActive = criteria.IsActive.Value;
            filtered = filtered.Where(user => user.IsActive == isActive);
        }

        var ordered = filtered
            .OrderBy(user => user.Username, StringComparer.Ordinal)
            .ThenBy(user => user.Id)
            .ToList();

        var totalItems = ordered.Count;
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserDto(
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.IsActive,
                user.CreatedAt))
            .ToList();

        return Task.FromResult(new PaginatedResult<UserDto>(
            items, page, pageSize, totalItems));
    }

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        FindByIdCallCount++;
        _byId.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<bool> UsernameExistsAsync(string username, Guid? excludingUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Task.FromResult(false);
        }

        var normalized = username.Trim();
        var exists = _users.Any(user =>
            string.Equals(user.Username, normalized, StringComparison.Ordinal)
            && (!excludingUserId.HasValue || user.Id != excludingUserId.Value));

        return Task.FromResult(exists);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludingUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult(false);
        }

        var normalized = email.Trim();
        var exists = _users.Any(user =>
            string.Equals(user.Email, normalized, StringComparison.Ordinal)
            && (!excludingUserId.HasValue || user.Id != excludingUserId.Value));

        return Task.FromResult(exists);
    }
}
