using Application.Common.Results;
using Application.Users.GetUserById;
using Application.Users.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Users.GetUserById;

public class GetUserByIdHandlerTests
{
    private static GetUserByIdHandler CreateHandler(
        InMemoryUserAdministrationReadRepository readRepository) =>
        new(
            readRepository,
            NullLogger<GetUserByIdHandler>.Instance);

    [Fact]
    public async Task HandleAsync_WhenUserExists_ReturnsDto()
    {
        var user = new User("admin", "admin@example.local", "HASH::a", UserRole.Admin);
        var readRepository = new InMemoryUserAdministrationReadRepository(new[] { user });
        var handler = CreateHandler(readRepository);

        var result = await handler.HandleAsync(
            new GetUserByIdQuery(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value!.Id);
        Assert.Equal(user.Username, result.Value.Username);
        Assert.Equal(user.Email, result.Value.Email);
        Assert.Equal(UserRole.Admin, result.Value.Role);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsNotFoundFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository);

        var result = await handler.HandleAsync(
            new GetUserByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        Assert.Equal("users.read.not_found", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WhenIdIsEmpty_ReturnsNotFoundFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(Array.Empty<User>());
        var handler = CreateHandler(readRepository);

        var result = await handler.HandleAsync(
            new GetUserByIdQuery(Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }
}
