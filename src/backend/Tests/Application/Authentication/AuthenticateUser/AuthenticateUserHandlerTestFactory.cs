using Application.Common.Authentication;
using Domain.Enums;

namespace Application.Authentication.AuthenticateUser.TestUtilities;

public sealed class InMemoryUserReadRepository : Application.Common.Persistence.IUserReadRepository
{
    private readonly Dictionary<string, Domain.Entities.User> _users;

    public InMemoryUserReadRepository(IEnumerable<Domain.Entities.User> users)
    {
        _users = users.ToDictionary(
            user => user.Username,
            user => user,
            StringComparer.Ordinal);
    }

    public IReadOnlyCollection<Domain.Entities.User> Users => _users.Values;

    public Task<Domain.Entities.User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Task.FromResult<Domain.Entities.User?>(null);
        }

        var normalized = username.Trim();
        _users.TryGetValue(normalized, out var user);
        return Task.FromResult<Domain.Entities.User?>(user);
    }
}

public sealed class FixedPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return "HASH::" + password;
    }

    public bool Verify(string password, string storedHash)
    {
        return string.Equals(Hash(password), storedHash, StringComparison.Ordinal);
    }
}

public sealed class CapturingTokenIssuer : IJwtTokenIssuer
{
    public AuthenticationTokenIssue LastIssue { get; private set; } = default!;

    public int IssueCallCount { get; private set; }

    public AuthenticationTokenIssue IssueFor(Domain.Entities.User user)
    {
        IssueCallCount++;

        LastIssue = new AuthenticationTokenIssue(
            AccessToken: $"token::{user.Id:N}::{user.Username}::{user.Role}",
            TokenType: "Bearer",
            ExpiresAtUtc: DateTime.UtcNow.AddMinutes(60),
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            Role: user.Role);

        return LastIssue;
    }
}

public static class AuthenticateUserHandlerTestFactory
{
    public static AuthenticateUserHandler CreateHandler(
        InMemoryUserReadRepository repository,
        out FixedPasswordHasher hasher,
        out CapturingTokenIssuer tokenIssuer)
    {
        hasher = new FixedPasswordHasher();
        tokenIssuer = new CapturingTokenIssuer();

        return new AuthenticateUserHandler(
            repository,
            hasher,
            tokenIssuer,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthenticateUserHandler>.Instance);
    }
}
