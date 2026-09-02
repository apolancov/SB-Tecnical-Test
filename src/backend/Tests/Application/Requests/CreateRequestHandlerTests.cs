using Application.Common.Audit;
using Application.Common.Results;
using Application.Common.Users;
using Application.Requests;
using Application.Requests.CreateRequest;
using Application.Requests.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Requests.CreateRequest;

public class CreateRequestHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static (User requester, Area area, RequestType type) SeedCatalog()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.User);
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        return (requester, area, type);
    }

    private static CreateRequestHandler CreateHandler(
        InMemoryRequestWriteRepository writeRepository,
        IAreaReadRepository? areaRepository = null,
        IRequestTypeReadRepository? requestTypeRepository = null,
        IUserLookupRepository? userLookup = null,
        IRequestCodeGenerator? codeGenerator = null,
        FakeCurrentUserAccessor? currentUser = null,
        (User requester, Area area, RequestType type)? catalog = null,
        InMemoryNotificationSender? notificationSender = null,
        IAuditLogger? auditLogger = null)
    {
        var seeded = catalog ?? SeedCatalog();
        currentUser ??= new FakeCurrentUserAccessor { CurrentUserId = seeded.requester.Id };
        return new CreateRequestHandler(
            writeRepository,
            areaRepository ?? new InMemoryAreaReadRepository(seeded.area),
            requestTypeRepository ?? new InMemoryRequestTypeReadRepository(seeded.type),
            codeGenerator ?? new SequenceRequestCodeGenerator(),
            currentUser,
            userLookup ?? new InMemoryUserLookupRepository(seeded.requester),
            notificationSender ?? new InMemoryNotificationSender(),
            auditLogger ?? new NullAuditLogger(),
            NullLogger<CreateRequestHandler>.Instance);
    }

    private static CreateRequestCommand ValidCommand(
        Area area,
        RequestType type,
        string title = "PC no enciende",
        string description = "La PC no enciende.",
        RequestPriority priority = RequestPriority.High,
        Guid? areaId = null,
        Guid? requestTypeId = null,
        DateTime? dueDate = null,
        string? evidenceUrl = null)
    {
        return new CreateRequestCommand(
            title,
            description,
            priority,
            areaId ?? area.Id,
            requestTypeId ?? type.Id,
            dueDate,
            evidenceUrl);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_PersistsRequestAndReturnsDto()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(writeRepository, catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type),
            CancellationToken.None);

        Assert.True(result.IsSuccess, $"Error: {result.Error?.Code} - {result.Error?.Message}");
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal("PC no enciende", result.Value.Title);
        Assert.StartsWith("SOL-", result.Value.Code);
        Assert.Equal(RequestStatus.Submitted, result.Value.Status);
        Assert.Equal(1, writeRepository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithFutureDueDate_Accepts()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(writeRepository, catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type, dueDate: DateTime.UtcNow.AddDays(5)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_WithMissingTitle_ReturnsValidationError()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(writeRepository, catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type, title: "   "),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("requests.create.title.required", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithTitleTooLong_ReturnsValidationError()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(writeRepository, catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type, title: new string('a', Request.TitleMaximumLength + 1)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.create.title.too_long", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownAreaId_ReturnsValidationError()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(writeRepository, catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type, areaId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.create.area.not_found", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveArea_ReturnsValidationError()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var inactiveArea = new Area("Atención al Ciudadano", isActive: false);
        var handler = CreateHandler(
            writeRepository,
            areaRepository: new InMemoryAreaReadRepository(inactiveArea),
            catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type, areaId: inactiveArea.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.create.area.inactive", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownRequestTypeId_ReturnsValidationError()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(writeRepository, catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type, requestTypeId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.create.request_type.not_found", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveRequestType_ReturnsValidationError()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var inactiveType = new RequestType("Incidente", "Reporte", isActive: false);
        var handler = CreateHandler(
            writeRepository,
            requestTypeRepository: new InMemoryRequestTypeReadRepository(inactiveType),
            catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type, requestTypeId: inactiveType.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.create.request_type.inactive", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyAreaId_ReturnsValidationError()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(writeRepository, catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type, areaId: Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.create.area.invalid_id", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyRequestTypeId_ReturnsValidationError()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(writeRepository, catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type, requestTypeId: Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.create.request_type.invalid_id", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithUnresolvedCurrentUser_ReturnsUnauthorized()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(
            writeRepository,
            currentUser: new FakeCurrentUserAccessor { CurrentUserId = Guid.Empty },
            catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Unauthorized, result.Error.Kind);
    }

    [Fact]
    public async Task HandleAsync_WithRequesterNotFound_ReturnsUnauthorized()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(
            writeRepository,
            userLookup: new InMemoryUserLookupRepository(),
            catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Unauthorized, result.Error.Kind);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveRequester_ReturnsValidationError()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var inactive = new User("requester", "requester@example.local", "hash", UserRole.User, isActive: false);
        var handler = CreateHandler(
            writeRepository,
            userLookup: new InMemoryUserLookupRepository(inactive),
            currentUser: new FakeCurrentUserAccessor { CurrentUserId = inactive.Id },
            catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("requests.create.user.inactive", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_DispatchesCreationNotification()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var notifications = new InMemoryNotificationSender();
        var handler = CreateHandler(
            writeRepository,
            catalog: seeded,
            notificationSender: notifications);

        var result = await handler.HandleAsync(
            ValidCommand(seeded.area, seeded.type),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(notifications.Sent);
        Assert.Equal(RequestNotificationFactory.RequestCreatedSubject, notifications.Sent[0].Subject);
    }

    [Fact]
    public async Task HandleAsync_WithEvidenceUrl_StoresAbsoluteUrl()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(writeRepository, catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(
                seeded.area,
                seeded.type,
                evidenceUrl: "https://example.com/evidence/123"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://example.com/evidence/123", result.Value.EvidenceUrl);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidEvidenceUrl_ReturnsValidationError()
    {
        var writeRepository = new InMemoryRequestWriteRepository();
        var seeded = SeedCatalog();
        var handler = CreateHandler(writeRepository, catalog: seeded);

        var result = await handler.HandleAsync(
            ValidCommand(
                seeded.area,
                seeded.type,
                evidenceUrl: "not-a-url"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("requests.create.evidence_url.invalid", result.Error.Code);
    }
}
