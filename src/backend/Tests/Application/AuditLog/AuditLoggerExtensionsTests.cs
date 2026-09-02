using Application.Common.Audit;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Common.Audit;

public class AuditLoggerExtensionsTests
{
    private sealed class RecordingLogger : IAuditLogger
    {
        public List<AuditLogContext> Recorded { get; } = new();

        public Task LogAsync(AuditLogContext context, CancellationToken cancellationToken)
        {
            Recorded.Add(context);
            return Task.CompletedTask;
        }
    }

    private static readonly DateTime FixedNow =
        new(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc);

    private static User CreateActor()
    {
        return new User("admin", "admin@example.local", "hash", UserRole.Admin);
    }

    private static Institution CreateInstitution()
    {
        return new Institution(
            "Acuario Nacional",
            new Category("Museo"),
            new StatePower("Poder Ejecutivo"),
            new Sector("Cultura"));
    }

    private static Request CreateRequest(User user)
    {
        var request = new Request(
            code: "SOL-2026-0001",
            title: "Title",
            description: "Description",
            priority: RequestPriority.Medium,
            requester: user,
            area: new Area("Atención"),
            requestType: new RequestType("Incidente", "Reporte"),
            createdAt: FixedNow);
        return request;
    }

    [Fact]
    public async Task LogRequestCreatedAsync_BuildsRequestCreatedContext()
    {
        var actor = CreateActor();
        var request = CreateRequest(actor);
        var logger = new RecordingLogger();

        await logger.LogRequestCreatedAsync(request, actor, CancellationToken.None);

        var entry = Assert.Single(logger.Recorded);
        Assert.Equal(AuditAction.RequestCreated, entry.Action);
        Assert.Equal(AuditOutcome.Success, entry.Outcome);
        Assert.Equal("Request", entry.EntityType);
        Assert.Equal(request.Id.ToString(), entry.EntityId);
        Assert.Equal(actor.Id, entry.ActorUserId);
        Assert.Equal(actor.Username, entry.ActorUserName);
    }

    [Fact]
    public async Task LogInstitutionCreatedAsync_BuildsInstitutionCreatedContext()
    {
        var actor = CreateActor();
        var institution = CreateInstitution();
        var logger = new RecordingLogger();

        await logger.LogInstitutionCreatedAsync(institution, actor, CancellationToken.None);

        var entry = Assert.Single(logger.Recorded);
        Assert.Equal(AuditAction.InstitutionCreated, entry.Action);
        Assert.Equal("Institution", entry.EntityType);
        Assert.Equal(institution.Id.ToString(), entry.EntityId);
        Assert.Equal(actor.Username, entry.ActorUserName);
    }

    [Fact]
    public async Task LogInstitutionDeletedAsync_BuildsInstitutionDeletedContext()
    {
        var actor = CreateActor();
        var institutionId = Guid.NewGuid();
        var logger = new RecordingLogger();

        await logger.LogInstitutionDeletedAsync(
            institutionId,
            "Acuario Nacional",
            actor,
            CancellationToken.None);

        var entry = Assert.Single(logger.Recorded);
        Assert.Equal(AuditAction.InstitutionDeleted, entry.Action);
        Assert.Equal(institutionId.ToString(), entry.EntityId);
    }

    [Fact]
    public async Task LogAuthorizationDeniedAsync_BuildsAuthorizationDeniedContext()
    {
        var logger = new RecordingLogger();

        await logger.LogAuthorizationDeniedAsync(
            policyName: "AdminOnly",
            endpoint: "GET /api/auditoria",
            actorUserId: Guid.NewGuid(),
            actorUserName: "user",
            ipAddress: null,
            CancellationToken.None);

        var entry = Assert.Single(logger.Recorded);
        Assert.Equal(AuditAction.AuthorizationDenied, entry.Action);
        Assert.Equal(AuditOutcome.Denied, entry.Outcome);
        Assert.Equal("AdminOnly", entry.EntityType);
        Assert.Equal("GET /api/auditoria", entry.EntityId);
        Assert.Equal("user", entry.ActorUserName);
    }

    [Fact]
    public async Task LogRequestStatusChangedAsync_BuildsStatusChangedContext()
    {
        var actor = CreateActor();
        var request = CreateRequest(actor);
        var logger = new RecordingLogger();

        await logger.LogRequestStatusChangedAsync(
            request,
            RequestStatus.Submitted,
            RequestStatus.InReview,
            actor,
            CancellationToken.None);

        var entry = Assert.Single(logger.Recorded);
        Assert.Equal(AuditAction.RequestStatusChanged, entry.Action);
        Assert.Equal("Submitted", entry.Details, ignoreCase: false);
        Assert.Contains("InReview", entry.Details);
    }
}