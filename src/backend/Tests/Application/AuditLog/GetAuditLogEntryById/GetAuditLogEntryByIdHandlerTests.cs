using Application.AuditLog.GetAuditLogEntries;
using Application.AuditLog.GetAuditLogEntryById;
using Application.AuditLog.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.AuditLog.GetAuditLogEntryById;

public class GetAuditLogEntryByIdHandlerTests
{
    private static GetAuditLogEntryByIdHandler CreateHandler(IAuditLogReadRepository repository)
    {
        return new GetAuditLogEntryByIdHandler(
            repository,
            NullLogger<GetAuditLogEntryByIdHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WithKnownId_ReturnsEntry()
    {
        var entry = new AuditLogEntry(
            action: AuditAction.LoginSucceeded,
            outcome: AuditOutcome.Success,
            entityType: "User",
            entityId: Guid.NewGuid().ToString(),
            actorUserId: Guid.NewGuid(),
            actorUserName: "admin");

        var repository = new InMemoryAuditLogReadRepository(entry);
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(
            new GetAuditLogEntryByIdQuery(entry.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(entry.Id, result.Value.Id);
        Assert.Equal("admin", result.Value.ActorUserName);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNotFoundFailure()
    {
        var handler = CreateHandler(new InMemoryAuditLogReadRepository());

        var result = await handler.HandleAsync(
            new GetAuditLogEntryByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("auditlog.read.not_found", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyId_ReturnsValidationFailure()
    {
        var handler = CreateHandler(new InMemoryAuditLogReadRepository());

        var result = await handler.HandleAsync(
            new GetAuditLogEntryByIdQuery(Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("auditlog.read.id.invalid", result.Error.Code);
    }
}