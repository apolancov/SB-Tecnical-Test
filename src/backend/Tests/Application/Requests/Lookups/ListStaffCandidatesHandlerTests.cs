using Application.Requests.Lookups;
using Application.Requests.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Requests.Lookups;

public class ListStaffCandidatesHandlerTests
{
    [Fact]
    public async Task HandleAsync_AsAdmin_ReturnsAllActiveStaffUsers()
    {
        var admin = new User("admin", "admin@example.local", "hash", UserRole.Admin);
        var analyst = new User("ana", "ana@example.local", "hash", UserRole.Analista);
        var solicitante = new User("juan", "juan@example.local", "hash", UserRole.Solicitante);
        var inactiveAdmin = new User("old", "old@example.local", "hash", UserRole.Admin, isActive: false);

        var repository = new InMemoryStaffCandidateRepository(admin, analyst, solicitante, inactiveAdmin);
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = admin.Id,
            CurrentRole = UserRole.Admin,
        };

        var handler = new ListStaffCandidatesHandler(
            repository,
            currentUser,
            NullLogger<ListStaffCandidatesHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess, $"Error: {result.Error?.Code} - {result.Error?.Message}");
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, candidate => candidate.Id == admin.Id && candidate.Role == "Admin");
        Assert.Contains(result.Value, candidate => candidate.Id == analyst.Id && candidate.Role == "Analista");
        Assert.DoesNotContain(result.Value, candidate => candidate.Id == solicitante.Id);
        Assert.DoesNotContain(result.Value, candidate => candidate.Id == inactiveAdmin.Id);
    }

    [Fact]
    public async Task HandleAsync_AsAnalista_ReturnsStaffCandidates()
    {
        var admin = new User("admin", "admin@example.local", "hash", UserRole.Admin);
        var analyst = new User("ana", "ana@example.local", "hash", UserRole.Analista);

        var repository = new InMemoryStaffCandidateRepository(admin, analyst);
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = analyst.Id,
            CurrentRole = UserRole.Analista,
        };

        var handler = new ListStaffCandidatesHandler(
            repository,
            currentUser,
            NullLogger<ListStaffCandidatesHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task HandleAsync_AsSolicitante_ReturnsForbidden()
    {
        var solicitante = new User("juan", "juan@example.local", "hash", UserRole.Solicitante);
        var admin = new User("admin", "admin@example.local", "hash", UserRole.Admin);

        var repository = new InMemoryStaffCandidateRepository(admin);
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = solicitante.Id,
            CurrentRole = UserRole.Solicitante,
        };

        var handler = new ListStaffCandidatesHandler(
            repository,
            currentUser,
            NullLogger<ListStaffCandidatesHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task HandleAsync_WhenCurrentUserIsAnonymous_ReturnsUnauthorized()
    {
        var admin = new User("admin", "admin@example.local", "hash", UserRole.Admin);
        var repository = new InMemoryStaffCandidateRepository(admin);
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = Guid.Empty,
            CurrentRole = UserRole.Admin,
        };

        var handler = new ListStaffCandidatesHandler(
            repository,
            currentUser,
            NullLogger<ListStaffCandidatesHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unauthorized, result.Error!.Kind);
    }

    [Fact]
    public async Task HandleAsync_ResultsAreOrderedByUsername()
    {
        var admin = new User("Zara", "zara@example.local", "hash", UserRole.Admin);
        var analyst = new User("ana", "ana@example.local", "hash", UserRole.Analista);
        var otherAnalyst = new User("Beatriz", "beatriz@example.local", "hash", UserRole.Analista);

        var repository = new InMemoryStaffCandidateRepository(admin, analyst, otherAnalyst);
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = admin.Id,
            CurrentRole = UserRole.Admin,
        };

        var handler = new ListStaffCandidatesHandler(
            repository,
            currentUser,
            NullLogger<ListStaffCandidatesHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "ana", "Beatriz", "Zara" }, result.Value!.Select(c => c.Username));
    }
}