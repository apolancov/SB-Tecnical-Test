using Application.Common.Audit;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.AddComment;
using Application.Requests.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Requests.AddComment;

public class AddCommentHandlerTests
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

    private static AddCommentHandler CreateHandler(
        InMemoryRequestReadRepository readRepository,
        InMemoryRequestWriteRepository writeRepository,
        FakeCurrentUserAccessor currentUser,
        IAuditLogger? auditLogger = null,
        params User[] users)
    {
        return new AddCommentHandler(
            readRepository,
            writeRepository,
            currentUser,
            new InMemoryUserLookupRepository(users),
            auditLogger ?? new NullAuditLogger(),
            NullLogger<AddCommentHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WithValidComment_AppendsAndPersists()
    {
        var request = CreateSubmittedRequest(out var requester);
        var agent = new User("agent", "agent@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = agent.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester, agent);

        var result = await handler.HandleAsync(
            new AddCommentCommand(request.Id, "Investigation underway.", CommentVisibility.Internal),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Investigation underway.", result.Value.Text);
        Assert.Equal(CommentVisibility.Internal, result.Value.Visibility);
        Assert.Equal(agent.Id, result.Value.AuthorId);
        Assert.Equal(1, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyText_ReturnsValidationError()
    {
        var request = CreateSubmittedRequest(out var requester);
        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor { CurrentUserId = requester.Id };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester);

        var result = await handler.HandleAsync(
            new AddCommentCommand(request.Id, "   ", CommentVisibility.Requester),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.comment.text.required", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithInternalVisibility_PersistsAuthor()
    {
        var request = CreateSubmittedRequest(out var requester);
        var agent = new User("agent", "agent@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = agent.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester, agent);

        var result = await handler.HandleAsync(
            new AddCommentCommand(request.Id, "Internal note.", CommentVisibility.Internal),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CommentVisibility.Internal, result.Value.Visibility);
        Assert.Equal(agent.Username, result.Value.AuthorUsername);
    }

    [Fact]
    public async Task HandleAsync_RequesterPostingInternalComment_ReturnsForbidden()
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
            new AddCommentCommand(request.Id, "Trying internal.", CommentVisibility.Internal),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.comment.visibility.forbidden", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithRequesterVisibility_PersistsAuthor()
    {
        var request = CreateSubmittedRequest(out var requester);
        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor { CurrentUserId = requester.Id };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester);

        var result = await handler.HandleAsync(
            new AddCommentCommand(request.Id, "Just checking in.", CommentVisibility.Requester),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CommentVisibility.Requester, result.Value.Visibility);
        Assert.Equal(requester.Username, result.Value.AuthorUsername);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveAuthor_ReturnsForbidden()
    {
        var request = CreateSubmittedRequest(out var requester);
        var inactive = new User("former", "former@example.local", "hash", UserRole.Solicitante, isActive: false);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = inactive.Id,
            CurrentRole = UserRole.Solicitante
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester, inactive);

        var result = await handler.HandleAsync(
            new AddCommentCommand(request.Id, "Trying to comment.", CommentVisibility.Requester),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error.Kind);
        Assert.Equal("requests.comment.user.inactive", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownRequest_ReturnsNotFound()
    {
        var agent = new User("agent", "agent@example.local", "hash", UserRole.Solicitante);
        var readRepository = new InMemoryRequestReadRepository();
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = agent.Id,
            CurrentRole = UserRole.Solicitante
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, agent);

        var result = await handler.HandleAsync(
            new AddCommentCommand(Guid.NewGuid(), "Comment", CommentVisibility.Requester),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.comment.not_found", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_AssignsAuthorFromCurrentUserNotRequestBody()
    {
        var request = CreateSubmittedRequest(out var requester);
        var agent = new User("agent", "agent@example.local", "hash", UserRole.Analista);

        var readRepository = new InMemoryRequestReadRepository(new[] { request });
        var writeRepository = new InMemoryRequestWriteRepository();
        var currentUser = new FakeCurrentUserAccessor
        {
            CurrentUserId = agent.Id,
            CurrentRole = UserRole.Analista
        };
        var handler = CreateHandler(readRepository, writeRepository, currentUser, requester, agent);

        var result = await handler.HandleAsync(
            new AddCommentCommand(request.Id, "Note from agent.", CommentVisibility.Internal),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(agent.Id, result.Value.AuthorId);
        Assert.NotEqual(requester.Id, result.Value.AuthorId);
    }
}
