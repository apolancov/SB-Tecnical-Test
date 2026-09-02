using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Domain.Requests;

public class RequestTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static (User requester, Area area, RequestType type) SeedDomain()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.User);
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        return (requester, area, type);
    }

    [Fact]
    public void Constructor_WithValidArguments_AssignsPropertiesAndStartsAtSubmitted()
    {
        var (requester, area, type) = SeedDomain();

        var request = new Request(
            code: "SOL-2026-0001",
            title: "PC no enciende",
            description: "La PC del puesto 12 no enciende.",
            priority: RequestPriority.High,
            requester: requester,
            area: area,
            requestType: type,
            createdAt: FixedNow,
            dueDate: FixedNow.AddDays(3));

        Assert.Equal("SOL-2026-0001", request.Code);
        Assert.Equal("PC no enciende", request.Title);
        Assert.Equal("La PC del puesto 12 no enciende.", request.Description);
        Assert.Equal(RequestPriority.High, request.Priority);
        Assert.Equal(RequestStatus.Submitted, request.Status);
        Assert.Equal(FixedNow, request.CreatedAt);
        Assert.Equal(FixedNow.AddDays(3), request.DueDate);
        Assert.Equal(requester.Id, request.RequesterId);
        Assert.Equal(area.Id, request.AreaId);
        Assert.Equal(type.Id, request.RequestTypeId);
        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Null(request.Responsible);
    }

    [Fact]
    public void Constructor_SeedsInitialStatusHistory()
    {
        var (requester, area, type) = SeedDomain();

        var request = new Request(
            code: "SOL-2026-0001",
            title: "Title",
            description: string.Empty,
            priority: RequestPriority.Low,
            requester: requester,
            area: area,
            requestType: type,
            createdAt: FixedNow,
            dueDate: null);

        var history = Assert.Single(request.StatusHistory);
        Assert.Equal(RequestStatus.Draft, history.PreviousStatus);
        Assert.Equal(RequestStatus.Submitted, history.NewStatus);
        Assert.Equal(FixedNow, history.Date);
        Assert.Equal(requester.Id, history.ChangedById);
    }

    [Fact]
    public void Constructor_WithNullRequester_Throws()
    {
        var (_, area, type) = SeedDomain();

        Assert.Throws<ArgumentNullException>(() =>
            new Request("SOL-2026-0001", "Title", "Description",
                RequestPriority.Low, null!, area, type, FixedNow, null));
    }

    [Fact]
    public void Constructor_WithNullArea_Throws()
    {
        var (requester, _, type) = SeedDomain();

        Assert.Throws<ArgumentNullException>(() =>
            new Request("SOL-2026-0001", "Title", "Description",
                RequestPriority.Low, requester, null!, type, FixedNow, null));
    }

    [Fact]
    public void Constructor_WithInactiveArea_Throws()
    {
        var (requester, _, type) = SeedDomain();
        var inactiveArea = new Area("Atención al Ciudadano", isActive: false);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Request("SOL-2026-0001", "Title", "Description",
                RequestPriority.Low, requester, inactiveArea, type, FixedNow, null));

        Assert.Contains("inactive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithInactiveRequestType_Throws()
    {
        var (requester, area, _) = SeedDomain();
        var inactiveType = new RequestType("Incidente", "Reporte", isActive: false);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Request("SOL-2026-0001", "Title", "Description",
                RequestPriority.Low, requester, area, inactiveType, FixedNow, null));

        Assert.Contains("inactive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithNullTitle_Throws()
    {
        var (requester, area, type) = SeedDomain();

        Assert.Throws<ArgumentException>(() =>
            new Request("SOL-2026-0001", null!, "Description",
                RequestPriority.Low, requester, area, type, FixedNow, null));
    }

    [Fact]
    public void Constructor_WithTitleTooLong_Throws()
    {
        var (requester, area, type) = SeedDomain();
        var longTitle = new string('a', Request.TitleMaximumLength + 1);

        Assert.Throws<ArgumentException>(() =>
            new Request("SOL-2026-0001", longTitle, "Description",
                RequestPriority.Low, requester, area, type, FixedNow, null));
    }

    [Fact]
    public void Constructor_WithDescriptionTooLong_Throws()
    {
        var (requester, area, type) = SeedDomain();
        var longDescription = new string('a', Request.DescriptionMaximumLength + 1);

        Assert.Throws<ArgumentException>(() =>
            new Request("SOL-2026-0001", "Title", longDescription,
                RequestPriority.Low, requester, area, type, FixedNow, null));
    }

    [Fact]
    public void Constructor_WithDueDateBeforeCreatedAt_Throws()
    {
        var (requester, area, type) = SeedDomain();

        Assert.Throws<ArgumentException>(() =>
            new Request("SOL-2026-0001", "Title", "Description",
                RequestPriority.Low, requester, area, type,
                createdAt: FixedNow,
                dueDate: FixedNow.AddDays(-1)));
    }

    [Fact]
    public void ChangeStatus_FromSubmittedToInReview_RecordsHistoryEntry()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);
        var reviewer = new User("reviewer", "reviewer@example.local", "hash", UserRole.User);

        request.ChangeStatus(RequestStatus.InReview, reviewer, "Triaged.", FixedNow.AddHours(1));

        Assert.Equal(RequestStatus.InReview, request.Status);
        var history = request.StatusHistory.ToList();
        Assert.Equal(2, history.Count);
        var latest = history[^1];
        Assert.Equal(RequestStatus.Submitted, latest.PreviousStatus);
        Assert.Equal(RequestStatus.InReview, latest.NewStatus);
        Assert.Equal("Triaged.", latest.Comment);
        Assert.Equal(reviewer.Id, latest.ChangedById);
    }

    [Fact]
    public void ChangeStatus_WithInvalidTransition_Throws()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);
        var actor = new User("actor", "actor@example.local", "hash", UserRole.User);

        Assert.Throws<InvalidOperationException>(() =>
            request.ChangeStatus(RequestStatus.Closed, actor, "Cannot close.", FixedNow.AddHours(1)));
    }

    [Fact]
    public void ChangeStatus_FromClosed_Throws()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);
        var actor = new User("actor", "actor@example.local", "hash", UserRole.User);

        request.ChangeStatus(RequestStatus.InReview, actor, string.Empty, FixedNow.AddHours(1));
        request.ChangeStatus(RequestStatus.Assigned, actor, string.Empty, FixedNow.AddHours(2));
        request.ChangeStatus(RequestStatus.InProgress, actor, string.Empty, FixedNow.AddHours(3));
        request.ChangeStatus(RequestStatus.Resolved, actor, string.Empty, FixedNow.AddHours(4));
        request.ChangeStatus(RequestStatus.Closed, actor, "Resolution notes.", FixedNow.AddHours(5));

        Assert.Throws<InvalidOperationException>(() =>
            request.ChangeStatus(RequestStatus.InProgress, actor, string.Empty, FixedNow.AddHours(6)));
    }

    [Fact]
    public void AssignResponsible_ToActiveUser_StoresResponsibleAndAddsHistory()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);
        var responsible = new User("agent", "agent@example.local", "hash", UserRole.User);

        request.AssignResponsible(responsible, requester, FixedNow.AddMinutes(30), "Routing to agent.");

        Assert.Equal(responsible.Id, request.ResponsibleId);
        Assert.Same(responsible, request.Responsible);
        var last = request.StatusHistory[^1];
        Assert.Equal(responsible.Id, last.ChangedById == responsible.Id ? responsible.Id : responsible.Id);
    }

    [Fact]
    public void AssignResponsible_ToInactiveUser_Throws()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);
        var inactive = new User("agent", "agent@example.local", "hash", UserRole.User, isActive: false);

        Assert.Throws<InvalidOperationException>(() =>
            request.AssignResponsible(inactive, requester, FixedNow.AddMinutes(30), "Try."));
    }

    [Fact]
    public void AssignResponsible_ToSameUser_IsIdempotent()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);
        var responsible = new User("agent", "agent@example.local", "hash", UserRole.User);

        request.AssignResponsible(responsible, requester, FixedNow.AddMinutes(30), "First.");
        var historyCountAfterFirst = request.StatusHistory.Count;

        request.AssignResponsible(responsible, requester, FixedNow.AddMinutes(60), "Second.");

        Assert.Equal(historyCountAfterFirst, request.StatusHistory.Count);
    }

    [Fact]
    public void AddComment_WithValidArguments_AppendsComment()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);

        var comment = request.AddComment(requester, "Will review.", CommentVisibility.Requester, FixedNow.AddHours(2));

        Assert.Single(request.Comments);
        Assert.Equal("Will review.", comment.Text);
        Assert.Equal(CommentVisibility.Requester, comment.Visibility);
        Assert.Equal(requester.Id, comment.AuthorId);
    }

    [Fact]
    public void AddComment_WithEmptyText_Throws()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);

        Assert.Throws<ArgumentException>(() =>
            request.AddComment(requester, "   ", CommentVisibility.Internal, FixedNow.AddHours(2)));
    }

    [Fact]
    public void AddComment_FromInactiveAuthor_Throws()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);
        var inactive = new User("former", "former@example.local", "hash", UserRole.User, isActive: false);

        Assert.Throws<InvalidOperationException>(() =>
            request.AddComment(inactive, "Comment", CommentVisibility.Internal, FixedNow.AddHours(2)));
    }

    [Fact]
    public void RegisterNotification_AddsNotificationWithPersistedFields()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);

        var notification = request.RegisterNotification(
            destinationUser: requester,
            channel: NotificationChannel.InApp,
            status: NotificationStatus.Pending,
            subject: "Tu solicitud fue recibida",
            message: "Hemos recibido tu solicitud.",
            notificationDate: FixedNow.AddHours(2));

        Assert.Single(request.Notifications);
        Assert.Equal(NotificationChannel.InApp, notification.Channel);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal("Tu solicitud fue recibida", notification.Subject);
        Assert.Equal(requester.Id, notification.DestinationUserId);
    }

    [Fact]
    public void RequestStatusTransition_IsAllowed_RespectsTable()
    {
        Assert.True(RequestStatusTransition.IsAllowed(RequestStatus.Submitted, RequestStatus.InReview));
        Assert.True(RequestStatusTransition.IsAllowed(RequestStatus.InReview, RequestStatus.Assigned));
        Assert.True(RequestStatusTransition.IsAllowed(RequestStatus.InProgress, RequestStatus.Resolved));
        Assert.True(RequestStatusTransition.IsAllowed(RequestStatus.Closed, RequestStatus.InProgress));
        Assert.False(RequestStatusTransition.IsAllowed(RequestStatus.Closed, RequestStatus.Closed));
        Assert.False(RequestStatusTransition.IsAllowed(RequestStatus.Submitted, RequestStatus.Submitted));
        Assert.False(RequestStatusTransition.IsAllowed(RequestStatus.Submitted, RequestStatus.Closed));
    }

    [Fact]
    public void ChangeStatus_ToClosed_WithoutResolutionComment_Throws()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);
        var actor = new User("actor", "actor@example.local", "hash", UserRole.Analista);

        request.ChangeStatus(RequestStatus.InReview, actor, string.Empty, FixedNow.AddHours(1));
        request.ChangeStatus(RequestStatus.Assigned, actor, string.Empty, FixedNow.AddHours(2));
        request.ChangeStatus(RequestStatus.InProgress, actor, string.Empty, FixedNow.AddHours(3));
        request.ChangeStatus(RequestStatus.Resolved, actor, string.Empty, FixedNow.AddHours(4));

        Assert.Throws<InvalidOperationException>(() =>
            request.ChangeStatus(RequestStatus.Closed, actor, string.Empty, FixedNow.AddHours(5)));
    }

    [Fact]
    public void ChangeStatus_ToClosed_SetsClosedAt()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);
        var actor = new User("actor", "actor@example.local", "hash", UserRole.Analista);

        request.ChangeStatus(RequestStatus.InReview, actor, string.Empty, FixedNow.AddHours(1));
        request.ChangeStatus(RequestStatus.Assigned, actor, string.Empty, FixedNow.AddHours(2));
        request.ChangeStatus(RequestStatus.InProgress, actor, string.Empty, FixedNow.AddHours(3));
        request.ChangeStatus(RequestStatus.Resolved, actor, string.Empty, FixedNow.AddHours(4));
        var closureDate = FixedNow.AddHours(5);
        request.ChangeStatus(RequestStatus.Closed, actor, "Resolved.", closureDate);

        Assert.Equal(RequestStatus.Closed, request.Status);
        Assert.Equal(closureDate, request.ClosedAt);
    }

    [Fact]
    public void Reopen_FromClosed_ResetsClosedAtAndCreatesHistoryEntry()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);
        var actor = new User("actor", "actor@example.local", "hash", UserRole.Analista);

        request.ChangeStatus(RequestStatus.InReview, actor, string.Empty, FixedNow.AddHours(1));
        request.ChangeStatus(RequestStatus.Assigned, actor, string.Empty, FixedNow.AddHours(2));
        request.ChangeStatus(RequestStatus.InProgress, actor, string.Empty, FixedNow.AddHours(3));
        request.ChangeStatus(RequestStatus.Resolved, actor, string.Empty, FixedNow.AddHours(4));
        request.ChangeStatus(RequestStatus.Closed, actor, "Done.", FixedNow.AddHours(5));
        Assert.NotNull(request.ClosedAt);

        var reopenDate = FixedNow.AddHours(6);
        request.Reopen(RequestStatus.InProgress, actor, "Need follow-up.", reopenDate);

        Assert.Equal(RequestStatus.InProgress, request.Status);
        Assert.Null(request.ClosedAt);
        var last = request.StatusHistory[^1];
        Assert.Equal(RequestStatus.Closed, last.PreviousStatus);
        Assert.Equal(RequestStatus.InProgress, last.NewStatus);
        Assert.Equal("Need follow-up.", last.Comment);
    }

    [Fact]
    public void Reopen_FromNonClosedStatus_Throws()
    {
        var (requester, area, type) = SeedDomain();
        var request = CreateSubmittedRequest(requester, area, type);
        var actor = new User("actor", "actor@example.local", "hash", UserRole.Analista);

        Assert.Throws<InvalidOperationException>(() =>
            request.Reopen(RequestStatus.InProgress, actor, "Try.", DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithEvidenceUrl_StoresAbsoluteUrl()
    {
        var (requester, area, type) = SeedDomain();

        var request = new Request(
            code: "SOL-2026-0001",
            title: "Title",
            description: "Description",
            priority: RequestPriority.Medium,
            requester: requester,
            area: area,
            requestType: type,
            createdAt: FixedNow,
            dueDate: null,
            evidenceUrl: "https://example.com/evidence/123");

        Assert.Equal("https://example.com/evidence/123", request.EvidenceUrl);
    }

    [Fact]
    public void Constructor_WithInvalidEvidenceUrl_Throws()
    {
        var (requester, area, type) = SeedDomain();

        Assert.Throws<ArgumentException>(() =>
            new Request(
                code: "SOL-2026-0001",
                title: "Title",
                description: "Description",
                priority: RequestPriority.Medium,
                requester: requester,
                area: area,
                requestType: type,
                createdAt: FixedNow,
                dueDate: null,
                evidenceUrl: "not-a-url"));
    }

    private static Request CreateSubmittedRequest(User requester, Area area, RequestType type)
    {
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
}
