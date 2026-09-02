using Application.Common.Results;
using Application.Users;
using Application.Users.DeleteUser;
using Application.Users.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Users.DeleteUser;

public class DeleteUserHandlerTests
{
    private static User Seed(bool isActive = true) =>
        new(
            username: "delete.user",
            email: "delete.user@example.local",
            passwordHash: "HASH::seed",
            role: UserRole.Analista,
            isActive: isActive);

    private static DeleteUserHandler CreateHandler(
        InMemoryUserAdministrationReadRepository readRepository,
        InMemoryUserAdministrationWriteRepository writeRepository) =>
        new(
            readRepository,
            writeRepository,
            NullLogger<DeleteUserHandler>.Instance);

    [Fact]
    public async Task HandleAsync_WhenUserExistsAndIsActive_DeactivatesAndReturnsSuccess()
    {
        var user = Seed(isActive: true);
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(
            new DeleteUserCommand(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsNotFoundFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(
            new DeleteUserCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        Assert.Equal("users.delete.not_found", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenIdIsEmpty_ReturnsNotFoundFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<User>());
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(
            new DeleteUserCommand(Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task HandleAsync_WhenUserAlreadyInactive_ReturnsConflictFailure()
    {
        var user = Seed(isActive: false);
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var writeRepository = new InMemoryUserAdministrationWriteRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository, writeRepository);

        var result = await handler.HandleAsync(
            new DeleteUserCommand(user.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        Assert.Equal("users.delete.already_inactive", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }
}
