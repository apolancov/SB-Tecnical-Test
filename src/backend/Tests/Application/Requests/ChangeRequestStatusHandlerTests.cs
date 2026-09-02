using Application.Common.Audit;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.ChangeRequestStatus;
using Application.Requests.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Requests.ChangeRequestStatus;

public class ChangeRequestStatusHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Request CreateSubmittedRequest(out User requester, out Area area, out RequestType type)
    {
        requester = new User("requester", "requester@example.local", "hash", UserRole.Solicitante);
        area = new Area("Atención al Ciudadano");
        type = new RequestType("Incidente", "Reporte");
        return new Request(
            code: "SOL-2026-0001",
            title: "Title",
            description: "Description",
            priority: RequestPriority.Medium,
            requester: requester,
            area: area,
            requestType: type,
            createdAt: FixedNow,
            dueDate: null);
    }

    private static ChangeRequestStatusHandler CreateHandler(
        InMemoryRequestReadRepository readRepository,
        InMemoryRequestWriteRepository writeRepository,
        FakeCurrentUserAccessor currentUser,
        InMemoryNotificationSender? notificationSender = null,
        IAuditLogger? auditLogger = null,
        params User[] users)
    {
        return new ChangeRequestStatusHandler(
            readRepository,
            writeRepository,
            currentUser,
            new InMemoryUserLookupRepository(users),
            notificationSender ?? new InMemoryNotificationSender(),
            auditLogger ?? new NullAuditLogger(),
            NullLogger<ChangeRequestStatusHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WithValidTransition_UpdatesStatusAndPersists()
    {
        var request = CreateSubmittedRequest(out var requester, out var area, out var type);
        var reviewer = new User("reviewer", "reviewer@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = reviewer.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, reviewer, requester);

        var result = await handler.HandleAsync(
            new ChangeRequestStatusCommand(request.Id, RequestStatus.InReview, "Triaged."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestStatus.InReview, result.Value.Status);
        Assert.Equal(1, writeRepository.UpdateCallCount);
        var detail = (await readRepository.GetDetailAsync(
            request.Id,
            reviewer.Id,
            UserRole.Analista,
            CancellationToken.None))!;
        Assert.Equal(RequestStatus.InReview, detail.Status);
        var lastHistory = detail.StatusHistory[^1];
        Assert.Equal(RequestStatus.Submitted, lastHistory.PreviousStatus);
        Assert.Equal(RequestStatus.InReview, lastHistory.NewStatus);
        Assert.Equal("Triaged.", lastHistory.Comment);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidTransition_ReturnsValidationError()
    {
        var request = CreateSubmittedRequest(out var requester, out var area, out var type);
        var reviewer = new User("reviewer", "reviewer@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = reviewer.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, reviewer, requester);

        var result = await handler.HandleAsync(
            new ChangeRequestStatusCommand(request.Id, RequestStatus.Closed, "Cannot close."),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("requests.change_status.transition.invalid", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_PreservesPreviousStatus()
    {
        var request = CreateSubmittedRequest(out var requester, out var area, out var type);
        var reviewer = new User("reviewer", "reviewer@example.local", "hash", UserRole.Analista);
        request.ChangeStatus(RequestStatus.InReview, reviewer, "Triaged.", FixedNow.AddHours(1));

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = reviewer.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, reviewer, requester);

        var result = await handler.HandleAsync(
            new ChangeRequestStatusCommand(request.Id, RequestStatus.Assigned, "Assigning."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var detail = (await readRepository.GetDetailAsync(
            request.Id,
            reviewer.Id,
            UserRole.Analista,
            CancellationToken.None))!;
        var lastHistory = detail.StatusHistory[^1];
        Assert.Equal(RequestStatus.InReview, lastHistory.PreviousStatus);
        Assert.Equal(RequestStatus.Assigned, lastHistory.NewStatus);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownRequest_ReturnsNotFound()
    {
        var readRepository = new InMemoryRequestReadRepository();
        var writeRepository = new InMemoryRequestWriteRepository();
        var reviewer = new User("reviewer", "reviewer@example.local", "hash", UserRole.Analista);
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = reviewer.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, reviewer);

        var result = await handler.HandleAsync(
            new ChangeRequestStatusCommand(Guid.NewGuid(), RequestStatus.InReview, "x"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveActor_ReturnsForbidden()
    {
        var request = CreateSubmittedRequest(out var requester, out var area, out var type);
        var inactive = new User("former", "former@example.local", "hash", UserRole.Analista, isActive: false);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = inactive.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, inactive, requester);

        var result = await handler.HandleAsync(
            new ChangeRequestStatusCommand(request.Id, RequestStatus.InReview, "x"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error.Kind);
        Assert.Equal("requests.change_status.user.inactive", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithSolicitanteActor_ReturnsForbidden()
    {
        var request = CreateSubmittedRequest(out var requester, out _, out _);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = requester.Id,
            CurrentRole = UserRole.Solicitante
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester);

        var result = await handler.HandleAsync(
            new ChangeRequestStatusCommand(request.Id, RequestStatus.InReview, "x"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error.Kind);
        Assert.Equal("requests.change_status.forbidden", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ClosingWithoutResolutionComment_ReturnsValidationError()
    {
        var request = CreateSubmittedRequest(out _, out _, out _);
        var reviewer = new User("reviewer", "reviewer@example.local", "hash", UserRole.Analista);
        request.ChangeStatus(RequestStatus.InReview, reviewer, "Triaged.", FixedNow.AddHours(1));
        request.ChangeStatus(RequestStatus.Assigned, reviewer, string.Empty, FixedNow.AddHours(2));
        request.ChangeStatus(RequestStatus.InProgress, reviewer, string.Empty, FixedNow.AddHours(3));
        request.ChangeStatus(RequestStatus.Resolved, reviewer, string.Empty, FixedNow.AddHours(4));

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = reviewer.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, reviewer);

        var result = await handler.HandleAsync(
            new ChangeRequestStatusCommand(request.Id, RequestStatus.Closed, string.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
    }

    [Fact]
    public async Task HandleAsync_ClosingWithResolutionComment_SucceedsAndNotifies()
    {
        var request = CreateSubmittedRequest(out var requester, out _, out _);
        var reviewer = new User("reviewer", "reviewer@example.local", "hash", UserRole.Analista);
        request.ChangeStatus(RequestStatus.InReview, reviewer, "Triaged.", FixedNow.AddHours(1));
        request.ChangeStatus(RequestStatus.Assigned, reviewer, string.Empty, FixedNow.AddHours(2));
        request.ChangeStatus(RequestStatus.InProgress, reviewer, string.Empty, FixedNow.AddHours(3));
        request.ChangeStatus(RequestStatus.Resolved, reviewer, string.Empty, FixedNow.AddHours(4));

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var notifications = new InMemoryNotificationSender();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = reviewer.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, notifications, reviewer);

        var result = await handler.HandleAsync(
            new ChangeRequestStatusCommand(
                request.Id,
                RequestStatus.Closed,
                "Resolved by patching the network."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestStatus.Closed, result.Value.Status);
        Assert.NotNull(result.Value.ClosedAt);
        Assert.True(notifications.Sent.Count >= 2);
    }
}