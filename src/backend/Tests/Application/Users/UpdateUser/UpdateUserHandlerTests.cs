using Application.Common.Results;
using Application.Users;
using Application.Users.TestUtilities;
using Application.Users.UpdateUser;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Users.UpdateUser;

public class UpdateUserHandlerTests
{
    private const string DefaultUsername = "update.user";
    private const string DefaultEmail = "update.user@example.local";

    private static User Seed() =>
        new(
            username: DefaultUsername,
            email: DefaultEmail,
            passwordHash: "HASH::seed",
            role: UserRole.Analista,
            isActive: true);

    private static UpdateUserHandler CreateHandler(
        InMemoryUserAdministrationReadRepository readRepository,
        InMemoryUserAdministrationWriteRepository writeRepository) =>
        new(
            readRepository,
            writeRepository,
            NullLogger<UpdateUserHandler>.Instance);

    [Fact]
    public async Task HandleAsync_WithValidArguments_UpdatesAndReturnsDto()
    {
        var user = Seed();
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateUserCommand(
            Id: user.Id,
            Username: "renamed.user",
            Email: "renamed.user@example.local",
            Role: UserRole.Admin,
            IsActive: false);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value!.Id);
        Assert.Equal("renamed.user", result.Value.Username);
        Assert.Equal("renamed.user@example.local", result.Value.Email);
        Assert.Equal(UserRole.Admin, result.Value.Role);
        Assert.False(result.Value.IsActive);
        Assert.Equal(1, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithoutRoleOrEmailChanges_DoesNotConsultUniquenessChecks()
    {
        var user = Seed();
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateUserCommand(
            Id: user.Id,
            Username: DefaultUsername,
            Email: DefaultEmail,
            Role: UserRole.Analista,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ReturnsNotFoundFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateUserCommand(
            Id: Guid.NewGuid(),
            Username: DefaultUsername,
            Email: DefaultEmail,
            Role: UserRole.Admin,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        Assert.Equal("users.update.not_found", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyId_ReturnsNotFoundFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateUserCommand(
            Id: Guid.Empty,
            Username: DefaultUsername,
            Email: DefaultEmail,
            Role: UserRole.Admin,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithNullOrEmptyUsername_ReturnsValidationFailure(string? invalidUsername)
    {
        var user = Seed();
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateUserCommand(
            Id: user.Id,
            Username: invalidUsername!,
            Email: DefaultEmail,
            Role: UserRole.Admin,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.update.username.required", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUsernameTooLong_ReturnsValidationFailure()
    {
        var user = Seed();
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateUserCommand(
            Id: user.Id,
            Username: new string('a', User.UsernameMaximumLength + 1),
            Email: DefaultEmail,
            Role: UserRole.Admin,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.update.username.too_long", result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithNullOrEmptyEmail_ReturnsValidationFailure(string? invalidEmail)
    {
        var user = Seed();
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateUserCommand(
            Id: user.Id,
            Username: DefaultUsername,
            Email: invalidEmail!,
            Role: UserRole.Admin,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.update.email.required", result.Error.Code);
    }

    [Theory]
    [InlineData("plain-address")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@missing-local.com")]
    public async Task HandleAsync_WithInvalidEmailFormat_ReturnsValidationFailure(string invalidEmail)
    {
        var user = Seed();
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateUserCommand(
            Id: user.Id,
            Username: DefaultUsername,
            Email: invalidEmail,
            Role: UserRole.Admin,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.update.email.invalid", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithExistingUsernameOfAnotherUser_ReturnsConflictFailure()
    {
        var existing = Seed();
        var other = new User(
            username: "other.user",
            email: "other.user@example.local",
            passwordHash: "HASH::other",
            role: UserRole.User);
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { existing, other });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateUserCommand(
            Id: existing.Id,
            Username: "other.user",
            Email: DefaultEmail,
            Role: UserRole.Admin,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        Assert.Equal("users.update.username.conflict", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithExistingEmailOfAnotherUser_ReturnsConflictFailure()
    {
        var existing = Seed();
        var other = new User(
            username: "other.user",
            email: "other.user@example.local",
            passwordHash: "HASH::other",
            role: UserRole.User);
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { existing, other });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateUserCommand(
            Id: existing.Id,
            Username: DefaultUsername,
            Email: "other.user@example.local",
            Role: UserRole.Admin,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        Assert.Equal("users.update.email.conflict", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }
}
