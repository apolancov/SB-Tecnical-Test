using Application.Authentication.AuthenticateUser;
using Application.Authentication.AuthenticateUser.TestUtilities;
using Application.Common.Results;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Authentication.AuthenticateUser;

public class AuthenticateUserHandlerTests
{
    private const string DefaultUsername = "admin";
    private const string DefaultEmail = "admin@example.local";
    private const string DefaultPassword = "AdminPass123!";

    private static InMemoryUserReadRepository CreateRepository(params User[] users)
    {
        return new InMemoryUserReadRepository(users);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsSuccessAndToken()
    {
        var user = new User(DefaultUsername, DefaultEmail, "HASH::" + DefaultPassword, UserRole.Admin);
        var repository = CreateRepository(user);

        var handler = AuthenticateUserHandlerTestFactory.CreateHandler(
            repository,
            out var hasher,
            out var tokenIssuer);

        var result = await handler.HandleAsync(
            new AuthenticateUserCommand(DefaultUsername, DefaultPassword),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Bearer", result.Value!.TokenType);
        Assert.Equal(DefaultUsername, result.Value.User.Username);
        Assert.Equal(DefaultEmail, result.Value.User.Email);
        Assert.Equal(UserRole.Admin, result.Value.User.Role);
        Assert.Equal(user.Id, result.Value.User.Id);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.True(result.Value.ExpiresAt > DateTime.UtcNow);
        Assert.Equal(1, tokenIssuer.IssueCallCount);
    }

    [Fact]
    public async Task HandleAsync_TrimsUsernameBeforeLookup()
    {
        var user = new User(DefaultUsername, DefaultEmail, "HASH::" + DefaultPassword, UserRole.Admin);
        var repository = CreateRepository(user);
        var handler = AuthenticateUserHandlerTestFactory.CreateHandler(
            repository, out _, out _);

        var result = await handler.HandleAsync(
            new AuthenticateUserCommand("  admin  ", DefaultPassword),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownUsername_ReturnsUnauthorizedAndDoesNotIssueToken()
    {
        var repository = CreateRepository();
        var handler = AuthenticateUserHandlerTestFactory.CreateHandler(
            repository, out _, out var tokenIssuer);

        var result = await handler.HandleAsync(
            new AuthenticateUserCommand("ghost", DefaultPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Unauthorized, result.Error!.Kind);
        Assert.Equal(0, tokenIssuer.IssueCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithIncorrectPassword_ReturnsUnauthorized()
    {
        var user = new User(DefaultUsername, DefaultEmail, "HASH::" + DefaultPassword, UserRole.Admin);
        var repository = CreateRepository(user);
        var handler = AuthenticateUserHandlerTestFactory.CreateHandler(
            repository, out _, out var tokenIssuer);

        var result = await handler.HandleAsync(
            new AuthenticateUserCommand(DefaultUsername, "WrongPassword!"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Unauthorized, result.Error!.Kind);
        Assert.Equal(0, tokenIssuer.IssueCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveUser_ReturnsUnauthorized()
    {
        var user = new User(DefaultUsername, DefaultEmail, "HASH::" + DefaultPassword, UserRole.Admin, isActive: false);
        var repository = CreateRepository(user);
        var handler = AuthenticateUserHandlerTestFactory.CreateHandler(
            repository, out _, out var tokenIssuer);

        var result = await handler.HandleAsync(
            new AuthenticateUserCommand(DefaultUsername, DefaultPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Unauthorized, result.Error!.Kind);
        Assert.Equal(0, tokenIssuer.IssueCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithRegularUserRole_StillReturnsToken()
    {
        var user = new User("user", "user@example.local", "HASH::SecretPass1!", UserRole.User);
        var repository = CreateRepository(user);
        var handler = AuthenticateUserHandlerTestFactory.CreateHandler(
            repository, out _, out var tokenIssuer);

        var result = await handler.HandleAsync(
            new AuthenticateUserCommand("user", "SecretPass1!"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.User, result.Value!.User.Role);
        Assert.Equal("user@example.local", result.Value.User.Email);
        Assert.Equal(1, tokenIssuer.IssueCallCount);
    }

    [Theory]
    [InlineData(null, "Password")]
    [InlineData("", "Password")]
    [InlineData("   ", "Password")]
    [InlineData("admin", null)]
    [InlineData("admin", "")]
    public async Task HandleAsync_WithMissingCredentials_ReturnsValidationError(
        string? username, string? password)
    {
        var repository = CreateRepository();
        var handler = AuthenticateUserHandlerTestFactory.CreateHandler(
            repository, out _, out _);

        var result = await handler.HandleAsync(
            new AuthenticateUserCommand(username!, password!),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public async Task HandleAsync_DoesNotExposePasswordHashOrPassword()
    {
        var user = new User(DefaultUsername, DefaultEmail, "HASH::" + DefaultPassword, UserRole.Admin);
        var repository = CreateRepository(user);
        var handler = AuthenticateUserHandlerTestFactory.CreateHandler(
            repository, out _, out var tokenIssuer);

        var result = await handler.HandleAsync(
            new AuthenticateUserCommand(DefaultUsername, DefaultPassword),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(DefaultPassword, result.Value!.AccessToken, StringComparison.Ordinal);
        Assert.DoesNotContain("HASH::", result.Value.AccessToken, StringComparison.Ordinal);
        Assert.DoesNotContain(DefaultPassword, result.Value.User.Username, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WithDisabledUser_DoesNotConsultPasswordHasher()
    {
        var user = new User(DefaultUsername, DefaultEmail, "HASH::" + DefaultPassword, UserRole.Admin, isActive: false);
        var repository = CreateRepository(user);

        var hasher = new RecordingPasswordHasher();
        var tokenIssuer = new CapturingTokenIssuer();

        var handler = new AuthenticateUserHandler(
            repository,
            hasher,
            tokenIssuer,
            NullLogger<AuthenticateUserHandler>.Instance);

        var result = await handler.HandleAsync(
            new AuthenticateUserCommand(DefaultUsername, DefaultPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Unauthorized, result.Error!.Kind);
        Assert.Equal(0, hasher.HashCallCount);
    }

    private sealed class RecordingPasswordHasher : Application.Common.Authentication.IPasswordHasher
    {
        public int HashCallCount { get; private set; }

        public string Hash(string password)
        {
            HashCallCount++;
            return "HASH::" + password;
        }

        public bool Verify(string password, string storedHash) => true;
    }
}
