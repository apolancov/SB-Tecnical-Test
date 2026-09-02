using Application.Common.Audit;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.AssignRequest;
using Application.Requests.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Requests.AssignRequest;

public class AssignRequestHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Request CreateSubmittedRequest(out User requester)
    {
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        requester = new User("requester", "requester@example.local", "hash", UserRole.Solicitante);
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

    private static AssignRequestHandler CreateHandler(
        InMemoryRequestReadRepository readRepository,
        InMemoryRequestWriteRepository writeRepository,
        FakeCurrentUserAccessor currentUser,
        InMemoryNotificationSender? notificationSender = null,
        IAuditLogger? auditLogger = null,
        params User[] users)
    {
        return new AssignRequestHandler(
            readRepository,
            writeRepository,
            currentUser,
            new InMemoryUserLookupRepository(users),
            notificationSender ?? new InMemoryNotificationSender(),
            auditLogger ?? new NullAuditLogger(),
            NullLogger<AssignRequestHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WithActiveResponsibleUser_AssignsAndPersists()
    {
        var request = CreateSubmittedRequest(out var requester);
        var responsible = new User("agent", "agent@example.local", "hash", UserRole.Analista);
        var analyst = new User("analyst", "analyst@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = analyst.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester, responsible, analyst);

        var result = await handler.HandleAsync(
            new AssignRequestCommand(request.Id, responsible.Id, "Routing."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(responsible.Id, result.Value.ResponsibleId);
        Assert.Equal(1, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveResponsibleUser_ReturnsValidationError()
    {
        var request = CreateSubmittedRequest(out var requester);
        var inactive = new User("former", "former@example.local", "hash", UserRole.Analista, isActive: false);
        var analyst = new User("analyst", "analyst@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = analyst.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester, inactive, analyst);

        var result = await handler.HandleAsync(
            new AssignRequestCommand(request.Id, inactive.Id, "Try."),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("requests.assign.responsible.inactive", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownResponsibleUser_ReturnsNotFound()
    {
        var request = CreateSubmittedRequest(out var requester);
        var analyst = new User("analyst", "analyst@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = analyst.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester, analyst);

        var result = await handler.HandleAsync(
            new AssignRequestCommand(request.Id, Guid.NewGuid(), "Try."),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("requests.assign.responsible.not_found", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownRequest_ReturnsNotFound()
    {
        var responsible = new User("agent", "agent@example.local", "hash", UserRole.Analista);
        var analyst = new User("analyst", "analyst@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository();
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = analyst.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, responsible, analyst);

        var result = await handler.HandleAsync(
            new AssignRequestCommand(Guid.NewGuid(), responsible.Id, "Try."),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("requests.assign.not_found", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyResponsibleId_ReturnsValidationError()
    {
        var request = CreateSubmittedRequest(out var requester);
        var analyst = new User("analyst", "analyst@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = analyst.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester, analyst);

        var result = await handler.HandleAsync(
            new AssignRequestCommand(request.Id, Guid.Empty, "Try."),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.assign.responsible.invalid_id", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithSolicitanteActor_ReturnsForbidden()
    {
        var request = CreateSubmittedRequest(out var requester);
        var responsible = new User("agent", "agent@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = requester.Id,
            CurrentRole = UserRole.Solicitante
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester, responsible);

        var result = await handler.HandleAsync(
            new AssignRequestCommand(request.Id, responsible.Id, "Try."),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error.Kind);
        Assert.Equal("requests.assign.forbidden", result.Error.Code);
    }
}