using Application.Common.Authentication;
using Application.Common.Results;
using Application.Users;
using Application.Users.CreateUser;
using Application.Users.TestUtilities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Users.CreateUser;

public class CreateUserHandlerTests
{
    private const string DefaultUsername = "new.user";
    private const string DefaultEmail = "new.user@example.local";
    private const string DefaultPassword = "NewUserPass123!";

    private static CreateUserCommand ValidCommand(
        string username = DefaultUsername,
        string email = DefaultEmail,
        string password = DefaultPassword,
        Domain.Enums.UserRole role = Domain.Enums.UserRole.Analista,
        bool isActive = true) =>
        new(username, email, password, role, isActive);

    private static CreateUserHandler CreateHandler(
        InMemoryUserAdministrationReadRepository readRepository,
        InMemoryUserAdministrationWriteRepository writeRepository,
        IPasswordHasher? hasher = null) =>
        new(
            readRepository,
            writeRepository,
            hasher ?? new FixedPasswordHasher(),
            NullLogger<CreateUserHandler>.Instance);

    [Fact]
    public async Task HandleAsync_WithValidArguments_PersistsUserAndReturnsDto()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<Domain.Entities.User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value!.Id);
        Assert.Equal(DefaultUsername, result.Value.Username);
        Assert.Equal(DefaultEmail, result.Value.Email);
        Assert.Equal(Domain.Enums.UserRole.Analista, result.Value.Role);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, writeRepository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsync_TrimsWhitespaceFromArguments()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<Domain.Entities.User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new CreateUserCommand(
            Username: $"  {DefaultUsername}  ",
            Email: $"  {DefaultEmail}  ",
            Password: DefaultPassword,
            Role: Domain.Enums.UserRole.User,
            IsActive: false);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DefaultUsername, result.Value!.Username);
        Assert.Equal(DefaultEmail, result.Value.Email);
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task HandleAsync_HashesPasswordBeforePersisting()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<Domain.Entities.User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(writeRepository.Users);
        Assert.Equal("HASH::" + DefaultPassword, stored.PasswordHash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithNullOrEmptyUsername_ReturnsValidationFailure(string? invalidUsername)
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<Domain.Entities.User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new CreateUserCommand(
            Username: invalidUsername!,
            Email: DefaultEmail,
            Password: DefaultPassword,
            Role: Domain.Enums.UserRole.Analista,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.create.username.required", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUsernameTooLong_ReturnsValidationFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<Domain.Entities.User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new CreateUserCommand(
            Username: new string('a', Domain.Entities.User.UsernameMaximumLength + 1),
            Email: DefaultEmail,
            Password: DefaultPassword,
            Role: Domain.Enums.UserRole.Analista,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.create.username.too_long", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithNullOrEmptyEmail_ReturnsValidationFailure(string? invalidEmail)
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<Domain.Entities.User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new CreateUserCommand(
            Username: DefaultUsername,
            Email: invalidEmail!,
            Password: DefaultPassword,
            Role: Domain.Enums.UserRole.Analista,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.create.email.required", result.Error.Code);
    }

    [Theory]
    [InlineData("plain-address")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@missing-local.com")]
    public async Task HandleAsync_WithInvalidEmailFormat_ReturnsValidationFailure(string invalidEmail)
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<Domain.Entities.User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new CreateUserCommand(
            Username: DefaultUsername,
            Email: invalidEmail,
            Password: DefaultPassword,
            Role: Domain.Enums.UserRole.Analista,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.create.email.invalid", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithEmailTooLong_ReturnsValidationFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<Domain.Entities.User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var longLocal = new string('a', Domain.Entities.User.EmailMaximumLength - 11);
        var command = new CreateUserCommand(
            Username: DefaultUsername,
            Email: $"{longLocal}@example.com",
            Password: DefaultPassword,
            Role: Domain.Enums.UserRole.Analista,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.create.email.too_long", result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task HandleAsync_WithNullOrEmptyPassword_ReturnsValidationFailure(string? invalidPassword)
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<Domain.Entities.User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new CreateUserCommand(
            Username: DefaultUsername,
            Email: DefaultEmail,
            Password: invalidPassword!,
            Role: Domain.Enums.UserRole.Analista,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.create.password.required", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithShortPassword_ReturnsValidationFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<Domain.Entities.User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new CreateUserCommand(
            Username: DefaultUsername,
            Email: DefaultEmail,
            Password: "short",
            Role: Domain.Enums.UserRole.Analista,
            IsActive: true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.create.password.too_short", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithExistingUsername_ReturnsConflictFailure()
    {
        var existing = new Domain.Entities.User(
            DefaultUsername,
            DefaultEmail,
            "HASH::existing",
            Domain.Enums.UserRole.Admin);
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { existing });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        Assert.Equal("users.create.username.conflict", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithExistingEmail_ReturnsConflictFailure()
    {
        var existing = new Domain.Entities.User(
            "other.user",
            DefaultEmail,
            "HASH::existing",
            Domain.Enums.UserRole.Admin);
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { existing });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<Domain.Entities.User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        Assert.Equal("users.create.email.conflict", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }
}
