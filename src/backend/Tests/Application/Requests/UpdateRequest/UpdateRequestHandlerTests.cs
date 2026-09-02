using Application.Common.Audit;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.TestUtilities;
using Application.Requests.UpdateRequest;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Requests.UpdateRequest;

public class UpdateRequestHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Request CreateSubmittedRequest(out User requester)
    {
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        requester = new User("requester", "requester@example.local", "hash", UserRole.Solicitante);
        return new Request(
            code: "SOL-2026-0001",
            title: "Original Title",
            description: "Original description.",
            priority: RequestPriority.Medium,
            requester: requester,
            area: area,
            requestType: type,
            createdAt: FixedNow,
            dueDate: FixedNow.AddDays(2),
            evidenceUrl: "https://example.com/original");
    }

    private static UpdateRequestHandler CreateHandler(
        InMemoryRequestReadRepository readRepository,
        InMemoryRequestWriteRepository writeRepository,
        FakeCurrentUserAccessor currentUser,
        IAuditLogger? auditLogger = null,
        params User[] users)
    {
        return new UpdateRequestHandler(
            readRepository,
            writeRepository,
            currentUser,
            new InMemoryUserLookupRepository(users),
            auditLogger ?? new NullAuditLogger(),
            NullLogger<UpdateRequestHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ByAnalista_UpdatesEditableFields()
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
            new UpdateRequestCommand(
                request.Id,
                Title: "Updated Title",
                Description: "Updated description.",
                Priority: RequestPriority.Critical,
                DueDate: FixedNow.AddDays(5),
                EvidenceUrl: "https://example.com/updated"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Title", result.Value.Title);
        Assert.Equal("Updated description.", result.Value.Description);
        Assert.Equal(RequestPriority.Critical, result.Value.Priority);
        Assert.Equal(FixedNow.AddDays(5), result.Value.DueDate);
        Assert.Equal("https://example.com/updated", result.Value.EvidenceUrl);
        Assert.Equal(1, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_ByOriginalRequester_OnSubmittedRequest_Allowed()
    {
        var request = CreateSubmittedRequest(out var requester);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = requester.Id,
            CurrentRole = UserRole.Solicitante
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester);

        var result = await handler.HandleAsync(
            new UpdateRequestCommand(
                request.Id,
                Title: "Self-update",
                Description: "Self description.",
                Priority: RequestPriority.Low,
                DueDate: FixedNow.AddDays(3),
                EvidenceUrl: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Self-update", result.Value.Title);
    }

    [Fact]
    public async Task HandleAsync_ByOtherSolicitante_ReturnsForbidden()
    {
        var request = CreateSubmittedRequest(out var requester);
        var other = new User("other", "other@example.local", "hash", UserRole.Solicitante);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = other.Id,
            CurrentRole = UserRole.Solicitante
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester, other);

        var result = await handler.HandleAsync(
            new UpdateRequestCommand(
                request.Id,
                Title: "Other user",
                Description: "Other description.",
                Priority: RequestPriority.Low,
                DueDate: FixedNow.AddDays(3),
                EvidenceUrl: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error.Kind);
        Assert.Equal("requests.update.forbidden", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_OnClosedRequest_ReturnsConflict()
    {
        var request = CreateSubmittedRequest(out var requester);
        var actor = new User("actor", "actor@example.local", "hash", UserRole.Analista);
        request.ChangeStatus(RequestStatus.InReview, actor, string.Empty, FixedNow.AddHours(1));
        request.ChangeStatus(RequestStatus.Assigned, actor, string.Empty, FixedNow.AddHours(2));
        request.ChangeStatus(RequestStatus.InProgress, actor, string.Empty, FixedNow.AddHours(3));
        request.ChangeStatus(RequestStatus.Resolved, actor, string.Empty, FixedNow.AddHours(4));
        request.ChangeStatus(RequestStatus.Closed, actor, "Done.", FixedNow.AddHours(5));

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = actor.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester, actor);

        var result = await handler.HandleAsync(
            new UpdateRequestCommand(
                request.Id,
                Title: "Cannot update",
                Description: "Closed.",
                Priority: RequestPriority.Low,
                DueDate: null,
                EvidenceUrl: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error.Kind);
        Assert.Equal("requests.update.status.conflict", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidEvidenceUrl_ReturnsValidationError()
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
            new UpdateRequestCommand(
                request.Id,
                Title: "Updated",
                Description: "Updated.",
                Priority: RequestPriority.Medium,
                DueDate: null,
                EvidenceUrl: "ftp://example.com/evidence"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.update.evidence_url.invalid", result.Error.Code);
    }
}