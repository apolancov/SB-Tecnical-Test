using Application.Common.Audit;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.ReopenRequest;
using Application.Requests.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Requests.ReopenRequest;

public class ReopenRequestHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Request CreateClosedRequest(User requester)
    {
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        var request = new Request(
            code: "SOL-2026-0001",
            title: "Title",
            description: "Description",
            priority: RequestPriority.Medium,
            requester: requester,
            area: area,
            requestType: type,
            createdAt: FixedNow,
            dueDate: null);

        var actor = new User("actor", "actor@example.local", "hash", UserRole.Analista);
        request.ChangeStatus(RequestStatus.InReview, actor, string.Empty, FixedNow.AddHours(1));
        request.ChangeStatus(RequestStatus.Assigned, actor, string.Empty, FixedNow.AddHours(2));
        request.ChangeStatus(RequestStatus.InProgress, actor, string.Empty, FixedNow.AddHours(3));
        request.ChangeStatus(RequestStatus.Resolved, actor, string.Empty, FixedNow.AddHours(4));
        request.ChangeStatus(RequestStatus.Closed, actor, "Resolved.", FixedNow.AddHours(5));

        return request;
    }

    private static ReopenRequestHandler CreateHandler(
        InMemoryRequestReadRepository readRepository,
        InMemoryRequestWriteRepository writeRepository,
        FakeCurrentUserAccessor currentUser,
        InMemoryNotificationSender? notificationSender = null,
        IAuditLogger? auditLogger = null,
        params User[] users)
    {
        return new ReopenRequestHandler(
            readRepository,
            writeRepository,
            currentUser,
            new InMemoryUserLookupRepository(users),
            notificationSender ?? new InMemoryNotificationSender(),
            auditLogger ?? new NullAuditLogger(),
            NullLogger<ReopenRequestHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ByAnalista_ReopensAndPersists()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.Solicitante);
        var request = CreateClosedRequest(requester);
        var analyst = new User("analyst", "analyst@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var notifications = new InMemoryNotificationSender();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = analyst.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, notifications, requester, analyst);

        var result = await handler.HandleAsync(
            new ReopenRequestCommand(request.Id, RequestStatus.InProgress, "Need follow-up."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestStatus.InProgress, result.Value.Status);
        Assert.Null(result.Value.ClosedAt);
        Assert.Equal(1, writeRepository.UpdateCallCount);
        Assert.Single(notifications.Sent);
    }

    [Fact]
    public async Task HandleAsync_ByAdmin_Reopens()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.Solicitante);
        var request = CreateClosedRequest(requester);
        var admin = new User("admin", "admin@example.local", "hash", UserRole.Admin);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = admin.Id,
            CurrentRole = UserRole.Admin
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, null, requester, admin);

        var result = await handler.HandleAsync(
            new ReopenRequestCommand(request.Id, RequestStatus.InProgress, string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestStatus.InProgress, result.Value.Status);
    }

    [Fact]
    public async Task HandleAsync_BySolicitante_ReturnsForbidden()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.Solicitante);
        var request = CreateClosedRequest(requester);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = requester.Id,
            CurrentRole = UserRole.Solicitante
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, null, requester);

        var result = await handler.HandleAsync(
            new ReopenRequestCommand(request.Id, RequestStatus.InProgress, "I want to reopen"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error.Kind);
        Assert.Equal("requests.reopen.forbidden", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_OnNonClosedRequest_ReturnsConflict()
    {
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        var requester = new User("requester", "requester@example.local", "hash", UserRole.Solicitante);
        var request = new Request(
            code: "SOL-2026-0001",
            title: "Title",
            description: "Description",
            priority: RequestPriority.Medium,
            requester: requester,
            area: area,
            requestType: type,
            createdAt: FixedNow,
            dueDate: null);

        var analyst = new User("analyst", "analyst@example.local", "hash", UserRole.Analista);
        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = analyst.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, null, requester, analyst);

        var result = await handler.HandleAsync(
            new ReopenRequestCommand(request.Id, RequestStatus.InProgress, string.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error.Kind);
        Assert.Equal("requests.reopen.conflict.not_closed", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidTargetStatus_ReturnsValidationError()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.Solicitante);
        var request = CreateClosedRequest(requester);
        var analyst = new User("analyst", "analyst@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = analyst.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, null, requester, analyst);

        var result = await handler.HandleAsync(
            new ReopenRequestCommand(request.Id, RequestStatus.Closed, "Stay closed"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("requests.reopen.transition.invalid", result.Error.Code);
    }
}