using Application.Common.Results;
using Application.Users.GetUsers;
using Application.Users.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Users.GetUsers;

public class GetUsersQueryHandlerTests
{
    private static GetUsersQueryHandler CreateHandler(
        InMemoryUserAdministrationReadRepository readRepository) =>
        new(
            readRepository,
            NullLogger<GetUsersQueryHandler>.Instance);

    private static IReadOnlyList<User> SeedUsers() => new[]
    {
        new User("admin", "admin@example.local", "HASH::a", UserRole.Admin),
        new User("user", "user@example.local", "HASH::u", UserRole.User),
        new User("analista", "analista@example.local", "HASH::a", UserRole.Analista),
        new User("solicitante1", "solicitante1@example.local", "HASH::s", UserRole.Solicitante, isActive: false),
        new User("solicitante2", "solicitante2@example.local", "HASH::s", UserRole.Solicitante),
    };

    [Fact]
    public async Task HandleAsync_WithoutFilters_ReturnsAllUsersOrderedByUsername()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(SeedUsers());
        var handler = CreateHandler(readRepository);

        var result = await handler.HandleAsync(
            new GetUsersQuery(1, 20, Username: null, Email: null, Role: null, IsActive: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.TotalItems);
        Assert.Equal(new[] { "admin", "analista", "solicitante1", "solicitante2", "user" },
            result.Value.Items.Select(item => item.Username).ToArray());
    }

    [Fact]
    public async Task HandleAsync_PaginatesResults()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(SeedUsers());
        var handler = CreateHandler(readRepository);

        var firstPage = await handler.HandleAsync(
            new GetUsersQuery(1, 2, null, null, null, null),
            CancellationToken.None);

        var secondPage = await handler.HandleAsync(
            new GetUsersQuery(2, 2, null, null, null, null),
            CancellationToken.None);

        Assert.True(firstPage.IsSuccess);
        Assert.True(secondPage.IsSuccess);
        Assert.Equal(2, firstPage.Value!.Items.Count);
        Assert.Equal(2, secondPage.Value!.Items.Count);
        Assert.Equal(5, firstPage.Value.TotalItems);
        Assert.Equal(3, firstPage.Value.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_FiltersByRole()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(SeedUsers());
        var handler = CreateHandler(readRepository);

        var result = await handler.HandleAsync(
            new GetUsersQuery(1, 20, null, null, UserRole.Solicitante, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalItems);
        Assert.All(result.Value.Items, item => Assert.Equal(UserRole.Solicitante, item.Role));
    }

    [Fact]
    public async Task HandleAsync_FiltersByIsActiveFalse()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(SeedUsers());
        var handler = CreateHandler(readRepository);

        var result = await handler.HandleAsync(
            new GetUsersQuery(1, 20, null, null, null, IsActive: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.TotalItems);
        Assert.False(result.Value.Items[0].IsActive);
    }

    [Fact]
    public async Task HandleAsync_FiltersByUsernamePartially()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(SeedUsers());
        var handler = CreateHandler(readRepository);

        var result = await handler.HandleAsync(
            new GetUsersQuery(1, 20, "solicitante", null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalItems);
        Assert.All(result.Value.Items, item => Assert.Contains("solicitante", item.Username, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HandleAsync_FiltersByEmailPartially()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(SeedUsers());
        var handler = CreateHandler(readRepository);

        var result = await handler.HandleAsync(
            new GetUsersQuery(1, 20, null, "admin", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.TotalItems);
        Assert.Equal("admin", result.Value.Items[0].Username);
    }

    [Fact]
    public async Task HandleAsync_TreatsBlankFiltersAsAbsent()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(SeedUsers());
        var handler = CreateHandler(readRepository);

        var result = await handler.HandleAsync(
            new GetUsersQuery(1, 20, "   ", "   ", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.TotalItems);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPage_ReturnsValidationFailure()
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(SeedUsers());
        var handler = CreateHandler(readRepository);

        var result = await handler.HandleAsync(
            new GetUsersQuery(0, 20, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.pagination.invalid_page", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task HandleAsync_WithInvalidPageSize_ReturnsValidationFailure(int pageSize)
    {
        var readRepository = new InMemoryUserAdministrationReadRepository(SeedUsers());
        var handler = CreateHandler(readRepository);

        var result = await handler.HandleAsync(
            new GetUsersQuery(1, pageSize, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("users.pagination.invalid_page_size", result.Error.Code);
    }
}
