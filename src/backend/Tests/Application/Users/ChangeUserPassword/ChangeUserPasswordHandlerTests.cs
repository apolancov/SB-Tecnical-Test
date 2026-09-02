using Application.Common.Results;
using Application.Users.ChangeUserPassword;
using Application.Users.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Users.ChangeUserPassword;

public class ChangeUserPasswordHandlerTests
{
    private const string DefaultNewPassword = "NewSecretPass123!";

    private static User Seed() =>
        new(
            username: "change.user",
            email: "change.user@example.local",
            passwordHash: "HASH::old",
            role: UserRole.Analista);

    private static ChangeUserPasswordHandler CreateHandler(
        InMemoryUserAdministrationReadRepository readRepository,
        InMemoryUserAdministrationWriteRepository writeRepository,
        Application.Common.Authentication.IPasswordHasher? hasher = null) =>
        new(
            readRepository,
            writeRepository,
            hasher ?? new FixedPasswordHasher(),
            NullLogger<ChangeUserPasswordHandler>.Instance);

    [Fact]
    public async Task HandleAsync_WithValidArguments_HashesAndUpdatesPassword()
    {
        var user = Seed();
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(
            new ChangeUserPasswordCommand(user.Id, DefaultNewPassword),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, writeRepository.UpdateCallCount);
        Assert.Equal("HASH::" + DefaultNewPassword, user.PasswordHash);
    }

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ReturnsNotFoundFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(
            new ChangeUserPasswordCommand(Guid.NewGuid(), DefaultNewPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        Assert.Equal("users.password.not_found", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenIdIsEmpty_ReturnsNotFoundFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(
            new ChangeUserPasswordCommand(Guid.Empty, DefaultNewPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task HandleAsync_WithNullOrEmptyPassword_ReturnsValidationFailure(string? invalidPassword)
    {
        var user = Seed();
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(
            new ChangeUserPasswordCommand(user.Id, invalidPassword!),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.password.required", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithShortPassword_ReturnsValidationFailure()
    {
        var user = Seed();
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(
            new ChangeUserPasswordCommand(user.Id, "short"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.password.too_short", result.Error.Code);
    }
}
