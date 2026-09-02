using Application.Common.Results;
using Application.Requests;
using Application.Requests.ListRequests;
using Application.Requests.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Requests.ListRequests;

public class ListRequestsQueryHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Request CreateRequest(
        string code,
        string title,
        User requester,
        Area area,
        RequestType type,
        RequestStatus status = RequestStatus.Submitted,
        RequestPriority priority = RequestPriority.Medium,
        DateTime? createdAt = null,
        DateTime? dueDate = null)
    {
        var request = new Request(
            code: code,
            title: title,
            description: "Description",
            priority: priority,
            requester: requester,
            area: area,
            requestType: type,
            createdAt: createdAt ?? FixedNow,
            dueDate: dueDate);

        if (status != RequestStatus.Submitted)
        {
            request.ChangeStatus(RequestStatus.InReview, requester, "Test transition.", FixedNow.AddMinutes(1));
            if (status == RequestStatus.InReview)
            {
                return request;
            }

            request.ChangeStatus(RequestStatus.Assigned, requester, "Test transition.", FixedNow.AddMinutes(2));
            if (status == RequestStatus.Assigned)
            {
                return request;
            }

            request.ChangeStatus(RequestStatus.InProgress, requester, "Test transition.", FixedNow.AddMinutes(3));
            if (status == RequestStatus.InProgress)
            {
                return request;
            }
        }

        return request;
    }

    private static ListRequestsQueryHandler CreateHandler(
        InMemoryRequestReadRepository repository,
        FakeCurrentUserAccessor currentUser)
    {
        return new ListRequestsQueryHandler(
            repository,
            currentUser,
            NullLogger<ListRequestsQueryHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WithoutFilters_ReturnsAllRequestsForAdmin()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.User);
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        var requests = new[]
        {
            CreateRequest("SOL-2026-0001", "Alpha", requester, area, type),
            CreateRequest("SOL-2026-0002", "Beta", requester, area, type)
        };
        var repository = new InMemoryRequestReadRepository(requests);
        var handler = CreateHandler(repository, new FakeCurrentUserAccessor
        {
            CurrentUserId = Guid.NewGuid(),
            CurrentRole = UserRole.Admin
        });

        var result = await handler.HandleAsync(
            new ListRequestsQuery(
                Page: 1,
                PageSize: 20,
                Status: null,
                Priority: null,
                AreaId: null,
                RequestTypeId: null,
                RequesterId: null,
                ResponsibleId: null,
                FromDate: null,
                ToDate: null,
                Code: null,
                Search: null,
                SortField: RequestSortField.CreatedAt,
                SortDirection: RequestSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalItems);
    }

    [Fact]
    public async Task HandleAsync_FilterByStatus_ReturnsOnlyMatchingRequests()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.User);
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        var requests = new[]
        {
            CreateRequest("SOL-2026-0001", "Alpha", requester, area, type, status: RequestStatus.Submitted),
            CreateRequest("SOL-2026-0002", "Beta", requester, area, type, status: RequestStatus.InReview)
        };
        var repository = new InMemoryRequestReadRepository(requests);
        var handler = CreateHandler(repository, new FakeCurrentUserAccessor
        {
            CurrentUserId = Guid.NewGuid(),
            CurrentRole = UserRole.Admin
        });

        var result = await handler.HandleAsync(
            new ListRequestsQuery(
                Page: 1,
                PageSize: 20,
                Status: RequestStatus.InReview,
                Priority: null,
                AreaId: null,
                RequestTypeId: null,
                RequesterId: null,
                ResponsibleId: null,
                FromDate: null,
                ToDate: null,
                Code: null,
                Search: null,
                SortField: RequestSortField.CreatedAt,
                SortDirection: RequestSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal("Beta", result.Value.Items[0].Title);
    }

    [Fact]
    public async Task HandleAsync_SearchByCodeTitle_AppliesLikeFilter()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.User);
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        var requests = new[]
        {
            CreateRequest("SOL-2026-0001", "PC Issue", requester, area, type),
            CreateRequest("SOL-2026-0002", "Network", requester, area, type)
        };
        var repository = new InMemoryRequestReadRepository(requests);
        var handler = CreateHandler(repository, new FakeCurrentUserAccessor
        {
            CurrentUserId = Guid.NewGuid(),
            CurrentRole = UserRole.Admin
        });

        var result = await handler.HandleAsync(
            new ListRequestsQuery(
                Page: 1,
                PageSize: 20,
                Status: null,
                Priority: null,
                AreaId: null,
                RequestTypeId: null,
                RequesterId: null,
                ResponsibleId: null,
                FromDate: null,
                ToDate: null,
                Code: null,
                Search: "PC",
                SortField: RequestSortField.CreatedAt,
                SortDirection: RequestSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal("PC Issue", result.Value.Items[0].Title);
    }

    [Fact]
    public async Task HandleAsync_FilterByDateRange_AppliesCreatedAtBounds()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.User);
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        var older = CreateRequest(
            "SOL-2026-0001",
            "Old",
            requester,
            area,
            type,
            createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = CreateRequest(
            "SOL-2026-0002",
            "New",
            requester,
            area,
            type,
            createdAt: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var repository = new InMemoryRequestReadRepository(new[] { older, newer });
        var handler = CreateHandler(repository, new FakeCurrentUserAccessor
        {
            CurrentUserId = Guid.NewGuid(),
            CurrentRole = UserRole.Admin
        });

        var result = await handler.HandleAsync(
            new ListRequestsQuery(
                Page: 1,
                PageSize: 20,
                Status: null,
                Priority: null,
                AreaId: null,
                RequestTypeId: null,
                RequesterId: null,
                ResponsibleId: null,
                FromDate: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                ToDate: new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                Code: null,
                Search: null,
                SortField: RequestSortField.CreatedAt,
                SortDirection: RequestSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal("New", result.Value.Items[0].Title);
    }

    [Fact]
    public async Task HandleAsync_AsRequester_FiltersOutOthersRequests()
    {
        var alice = new User("alice", "alice@example.local", "hash", UserRole.Solicitante);
        var bob = new User("bob", "bob@example.local", "hash", UserRole.Solicitante);
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        var requests = new[]
        {
            CreateRequest("SOL-2026-0001", "Alice", alice, area, type),
            CreateRequest("SOL-2026-0002", "Bob", bob, area, type)
        };
        var repository = new InMemoryRequestReadRepository(requests);
        var handler = CreateHandler(repository, new FakeCurrentUserAccessor
        {
            CurrentUserId = alice.Id,
            CurrentRole = UserRole.Solicitante
        });

        var result = await handler.HandleAsync(
            new ListRequestsQuery(
                Page: 1,
                PageSize: 20,
                Status: null,
                Priority: null,
                AreaId: null,
                RequestTypeId: null,
                RequesterId: null,
                ResponsibleId: null,
                FromDate: null,
                ToDate: null,
                Code: null,
                Search: null,
                SortField: RequestSortField.CreatedAt,
                SortDirection: RequestSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal("Alice", result.Value.Items[0].Title);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task HandleAsync_InvalidPage_ReturnsValidationError(int invalidPage)
    {
        var repository = new InMemoryRequestReadRepository();
        var handler = CreateHandler(repository, new FakeCurrentUserAccessor
        {
            CurrentUserId = Guid.NewGuid(),
            CurrentRole = UserRole.Admin
        });

        var result = await handler.HandleAsync(
            new ListRequestsQuery(
                Page: invalidPage,
                PageSize: 20,
                Status: null,
                Priority: null,
                AreaId: null,
                RequestTypeId: null,
                RequesterId: null,
                ResponsibleId: null,
                FromDate: null,
                ToDate: null,
                Code: null,
                Search: null,
                SortField: RequestSortField.CreatedAt,
                SortDirection: RequestSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.pagination.invalid_page", result.Error.Code);
        Assert.Equal(0, repository.SearchCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task HandleAsync_InvalidPageSize_ReturnsValidationError(int invalidPageSize)
    {
        var repository = new InMemoryRequestReadRepository();
        var handler = CreateHandler(repository, new FakeCurrentUserAccessor
        {
            CurrentUserId = Guid.NewGuid(),
            CurrentRole = UserRole.Admin
        });

        var result = await handler.HandleAsync(
            new ListRequestsQuery(
                Page: 1,
                PageSize: invalidPageSize,
                Status: null,
                Priority: null,
                AreaId: null,
                RequestTypeId: null,
                RequesterId: null,
                ResponsibleId: null,
                FromDate: null,
                ToDate: null,
                Code: null,
                Search: null,
                SortField: RequestSortField.CreatedAt,
                SortDirection: RequestSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.pagination.invalid_page_size", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_FromDateGreaterThanToDate_ReturnsValidationError()
    {
        var repository = new InMemoryRequestReadRepository();
        var handler = CreateHandler(repository, new FakeCurrentUserAccessor
        {
            CurrentUserId = Guid.NewGuid(),
            CurrentRole = UserRole.Admin
        });

        var result = await handler.HandleAsync(
            new ListRequestsQuery(
                Page: 1,
                PageSize: 20,
                Status: null,
                Priority: null,
                AreaId: null,
                RequestTypeId: null,
                RequesterId: null,
                ResponsibleId: null,
                FromDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                ToDate: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                Code: null,
                Search: null,
                SortField: RequestSortField.CreatedAt,
                SortDirection: RequestSortDirection.Descending),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.filter.date_range.invalid", result.Error.Code);
    }
}