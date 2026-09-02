using Application.AuditLog.GetAuditLogEntries;
using Application.AuditLog.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.AuditLog.GetAuditLogEntries;

public class GetAuditLogEntriesQueryHandlerTests
{
    private static readonly DateTime FixedTimestamp =
        new(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc);

    private static AuditLogEntry CreateEntry(
        AuditAction action,
        AuditOutcome outcome = AuditOutcome.Success,
        string entityType = "Request",
        string? actorUserName = null,
        Guid? actorUserId = null,
        DateTime? timestamp = null,
        string? details = null,
        string? entityId = null)
    {
        return new AuditLogEntry(
            action: action,
            outcome: outcome,
            entityType: entityType,
            entityId: entityId,
            details: details,
            actorUserId: actorUserId,
            actorUserName: actorUserName,
            timestamp: timestamp ?? FixedTimestamp);
    }

    private static GetAuditLogEntriesQueryHandler CreateHandler(
        IAuditLogReadRepository repository)
    {
        return new GetAuditLogEntriesQueryHandler(
            repository,
            NullLogger<GetAuditLogEntriesQueryHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaginatedEntries()
    {
        var entries = new[]
        {
            CreateEntry(AuditAction.LoginSucceeded, actorUserName: "admin"),
            CreateEntry(AuditAction.RequestCreated, actorUserName: "user1"),
            CreateEntry(AuditAction.RequestUpdated, actorUserName: "user2")
        };
        var repository = new InMemoryAuditLogReadRepository(entries);

        var handler = CreateHandler(repository);
        var result = await handler.HandleAsync(
            new GetAuditLogEntriesQuery(
                Page: 1,
                PageSize: 20,
                FromDate: null,
                ToDate: null,
                ActorUserId: null,
                Action: null,
                Outcome: null,
                EntityType: null,
                Search: null,
                SortField: AuditLogSortField.Timestamp,
                SortDirection: AuditLogSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(3, result.Value.Items.Count);
    }

    [Fact]
    public async Task HandleAsync_FilterByAction_RestrictsResults()
    {
        var entries = new[]
        {
            CreateEntry(AuditAction.LoginSucceeded, actorUserName: "admin"),
            CreateEntry(AuditAction.RequestCreated, actorUserName: "user1"),
            CreateEntry(AuditAction.LoginFailed, outcome: AuditOutcome.Failure, actorUserName: "user2")
        };
        var repository = new InMemoryAuditLogReadRepository(entries);

        var handler = CreateHandler(repository);
        var result = await handler.HandleAsync(
            new GetAuditLogEntriesQuery(
                Page: 1,
                PageSize: 20,
                FromDate: null,
                ToDate: null,
                ActorUserId: null,
                Action: AuditAction.LoginSucceeded,
                Outcome: null,
                EntityType: null,
                Search: null,
                SortField: AuditLogSortField.Timestamp,
                SortDirection: AuditLogSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal(AuditAction.LoginSucceeded, result.Value.Items[0].Action);
    }

    [Fact]
    public async Task HandleAsync_FilterByActorUserId_RestrictsResults()
    {
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var entries = new[]
        {
            CreateEntry(AuditAction.LoginSucceeded, actorUserName: "admin", actorUserId: adminId),
            CreateEntry(AuditAction.RequestCreated, actorUserName: "user", actorUserId: userId)
        };
        var repository = new InMemoryAuditLogReadRepository(entries);

        var handler = CreateHandler(repository);
        var result = await handler.HandleAsync(
            new GetAuditLogEntriesQuery(
                Page: 1,
                PageSize: 20,
                FromDate: null,
                ToDate: null,
                ActorUserId: adminId,
                Action: null,
                Outcome: null,
                EntityType: null,
                Search: null,
                SortField: AuditLogSortField.Timestamp,
                SortDirection: AuditLogSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal(adminId, result.Value.Items[0].ActorUserId);
    }

    [Fact]
    public async Task HandleAsync_SearchByDetails_ReturnsMatchingEntries()
    {
        var entries = new[]
        {
            CreateEntry(AuditAction.LoginSucceeded, details: "Login successful", actorUserName: "admin"),
            CreateEntry(AuditAction.RequestCreated, details: "Request created by 'admin'", actorUserName: "admin"),
            CreateEntry(AuditAction.InstitutionCreated, details: "Institution created", actorUserName: "admin")
        };
        var repository = new InMemoryAuditLogReadRepository(entries);

        var handler = CreateHandler(repository);
        var result = await handler.HandleAsync(
            new GetAuditLogEntriesQuery(
                Page: 1,
                PageSize: 20,
                FromDate: null,
                ToDate: null,
                ActorUserId: null,
                Action: null,
                Outcome: null,
                EntityType: null,
                Search: "created",
                SortField: AuditLogSortField.Timestamp,
                SortDirection: AuditLogSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.All(result.Value.Items, item =>
            Assert.Contains("created", item.Details!, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HandleAsync_FilterByDateRange_RestrictsResults()
    {
        var entries = new[]
        {
            CreateEntry(AuditAction.LoginSucceeded, timestamp: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateEntry(AuditAction.LoginSucceeded, timestamp: new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc)),
            CreateEntry(AuditAction.LoginSucceeded, timestamp: new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc))
        };
        var repository = new InMemoryAuditLogReadRepository(entries);

        var handler = CreateHandler(repository);
        var result = await handler.HandleAsync(
            new GetAuditLogEntriesQuery(
                Page: 1,
                PageSize: 20,
                FromDate: new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                ToDate: new DateTime(2026, 9, 2, 23, 0, 0, DateTimeKind.Utc),
                ActorUserId: null,
                Action: null,
                Outcome: null,
                EntityType: null,
                Search: null,
                SortField: AuditLogSortField.Timestamp,
                SortDirection: AuditLogSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalItems);
    }

    [Fact]
    public async Task HandleAsync_InvalidPage_ReturnsValidationFailure()
    {
        var handler = CreateHandler(new InMemoryAuditLogReadRepository());

        var result = await handler.HandleAsync(
            new GetAuditLogEntriesQuery(
                Page: 0,
                PageSize: 20,
                FromDate: null,
                ToDate: null,
                ActorUserId: null,
                Action: null,
                Outcome: null,
                EntityType: null,
                Search: null,
                SortField: AuditLogSortField.Timestamp,
                SortDirection: AuditLogSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("auditlog.pagination.invalid_page", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_InvalidPageSize_ReturnsValidationFailure()
    {
        var handler = CreateHandler(new InMemoryAuditLogReadRepository());

        var result = await handler.HandleAsync(
            new GetAuditLogEntriesQuery(
                Page: 1,
                PageSize: 0,
                FromDate: null,
                ToDate: null,
                ActorUserId: null,
                Action: null,
                Outcome: null,
                EntityType: null,
                Search: null,
                SortField: AuditLogSortField.Timestamp,
                SortDirection: AuditLogSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("auditlog.pagination.invalid_page_size", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_FromDateAfterToDate_ReturnsValidationFailure()
    {
        var handler = CreateHandler(new InMemoryAuditLogReadRepository());

        var result = await handler.HandleAsync(
            new GetAuditLogEntriesQuery(
                Page: 1,
                PageSize: 20,
                FromDate: new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc),
                ToDate: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                ActorUserId: null,
                Action: null,
                Outcome: null,
                EntityType: null,
                Search: null,
                SortField: AuditLogSortField.Timestamp,
                SortDirection: AuditLogSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("auditlog.filter.date_range.invalid", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_PageBeyondRange_ReturnsEmptyItemsButCorrectTotals()
    {
        var entries = new[]
        {
            CreateEntry(AuditAction.LoginSucceeded),
            CreateEntry(AuditAction.LoginFailed, outcome: AuditOutcome.Failure)
        };
        var repository = new InMemoryAuditLogReadRepository(entries);

        var handler = CreateHandler(repository);
        var result = await handler.HandleAsync(
            new GetAuditLogEntriesQuery(
                Page: 3,
                PageSize: 20,
                FromDate: null,
                ToDate: null,
                ActorUserId: null,
                Action: null,
                Outcome: null,
                EntityType: null,
                Search: null,
                SortField: AuditLogSortField.Timestamp,
                SortDirection: AuditLogSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Empty(result.Value.Items);
    }
}