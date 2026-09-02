using Domain.Enums;

namespace Domain.Entities;

public class Request
{
    public const int CodeMaximumLength = 32;
    public const int TitleMaximumLength = 256;
    public const int DescriptionMaximumLength = 4096;
    public const int CommentMaximumLength = 1024;
    public const int EvidenceUrlMaximumLength = 2048;

    private readonly List<RequestStatusHistoryEntry> _statusHistory = new();
    private readonly List<RequestComment> _comments = new();
    private readonly List<RequestNotification> _notifications = new();

    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public RequestPriority Priority { get; private set; }

    public RequestStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? DueDate { get; private set; }

    public Guid RequesterId { get; private set; }

    public User Requester { get; private set; }

    public Guid? ResponsibleId { get; private set; }

    public User? Responsible { get; private set; }

    public Guid AreaId { get; private set; }

    public Area Area { get; private set; }

    public Guid RequestTypeId { get; private set; }

    public RequestType RequestType { get; private set; }

    public string? EvidenceUrl { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    public IReadOnlyList<RequestStatusHistoryEntry> StatusHistory => _statusHistory.AsReadOnly();

    public IReadOnlyList<RequestComment> Comments => _comments.AsReadOnly();

    public IReadOnlyList<RequestNotification> Notifications => _notifications.AsReadOnly();

    private Request()
    {
        Code = string.Empty;
        Title = string.Empty;
        Description = string.Empty;
        Requester = null!;
        Area = null!;
        RequestType = null!;
    }

    public Request(
        string code,
        string title,
        string description,
        RequestPriority priority,
        User requester,
        Area area,
        RequestType requestType,
        DateTime createdAt,
        DateTime? dueDate,
        string? evidenceUrl = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be null or empty.", nameof(code));
        }

        var normalizedCode = code.Trim();
        if (normalizedCode.Length > CodeMaximumLength)
        {
            throw new ArgumentException(
                $"Code cannot exceed {CodeMaximumLength} characters.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be null or empty.", nameof(title));
        }

        var normalizedTitle = title.Trim();
        if (normalizedTitle.Length > TitleMaximumLength)
        {
            throw new ArgumentException(
                $"Title cannot exceed {TitleMaximumLength} characters.",
                nameof(title));
        }

        var normalizedDescription = (description ?? string.Empty).Trim();
        if (normalizedDescription.Length > DescriptionMaximumLength)
        {
            throw new ArgumentException(
                $"Description cannot exceed {DescriptionMaximumLength} characters.",
                nameof(description));
        }

        if (requester is null)
        {
            throw new ArgumentNullException(nameof(requester), "Requester cannot be null.");
        }

        if (area is null)
        {
            throw new ArgumentNullException(nameof(area), "Area cannot be null.");
        }

        if (!area.IsActive)
        {
            throw new InvalidOperationException(
                $"Area '{area.Name}' is inactive and cannot be assigned to new requests.");
        }

        if (requestType is null)
        {
            throw new ArgumentNullException(nameof(requestType), "Request type cannot be null.");
        }

        if (!requestType.IsActive)
        {
            throw new InvalidOperationException(
                $"Request type '{requestType.Name}' is inactive and cannot be assigned to new requests.");
        }

        if (dueDate.HasValue && dueDate.Value < createdAt)
        {
            throw new ArgumentException(
                "DueDate cannot be earlier than CreatedAt.",
                nameof(dueDate));
        }

        var normalizedEvidenceUrl = NormalizeEvidenceUrl(evidenceUrl);

        Id = Guid.NewGuid();
        Code = normalizedCode;
        Title = normalizedTitle;
        Description = normalizedDescription;
        Priority = priority;
        RequesterId = requester.Id;
        Requester = requester;
        AreaId = area.Id;
        Area = area;
        RequestTypeId = requestType.Id;
        RequestType = requestType;
        CreatedAt = createdAt;
        DueDate = dueDate;
        EvidenceUrl = normalizedEvidenceUrl;
        Status = RequestStatus.Submitted;

        _statusHistory.Add(new RequestStatusHistoryEntry(
            previousStatus: RequestStatus.Draft,
            newStatus: RequestStatus.Submitted,
            date: createdAt,
            comment: string.Empty,
            changedBy: requester));
    }

    public void ChangeStatus(RequestStatus newStatus, User changedBy, string comment, DateTime changeDate)
    {
        if (changedBy is null)
        {
            throw new ArgumentNullException(nameof(changedBy), "ChangedBy cannot be null.");
        }

        if (!RequestStatusTransition.IsAllowed(Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Transition from '{Status}' to '{newStatus}' is not allowed.");
        }

        var normalizedComment = (comment ?? string.Empty).Trim();
        if (normalizedComment.Length > CommentMaximumLength)
        {
            throw new ArgumentException(
                $"Status change comment cannot exceed {CommentMaximumLength} characters.",
                nameof(comment));
        }

        if (newStatus == RequestStatus.Closed && normalizedComment.Length == 0)
        {
            throw new InvalidOperationException(
                "Closing a request requires a non-empty resolution comment.");
        }

        var previousStatus = Status;
        Status = newStatus;

        if (newStatus == RequestStatus.Closed && ClosedAt is null)
        {
            ClosedAt = changeDate;
        }
        else if (newStatus != RequestStatus.Closed)
        {
            ClosedAt = null;
        }

        _statusHistory.Add(new RequestStatusHistoryEntry(
            previousStatus: previousStatus,
            newStatus: newStatus,
            date: changeDate,
            comment: normalizedComment,
            changedBy: changedBy));
    }

    public void Reopen(RequestStatus newStatus, User reopenedBy, string comment, DateTime reopenDate)
    {
        if (reopenedBy is null)
        {
            throw new ArgumentNullException(nameof(reopenedBy), "ReopenedBy cannot be null.");
        }

        if (Status != RequestStatus.Closed)
        {
            throw new InvalidOperationException(
                $"Only requests in 'Closed' status can be reopened. Current status: '{Status}'.");
        }

        if (!RequestStatusTransition.IsAllowed(RequestStatus.Closed, newStatus))
        {
            throw new InvalidOperationException(
                $"Reopening transition to '{newStatus}' is not allowed.");
        }

        if (newStatus == RequestStatus.Closed)
        {
            throw new InvalidOperationException(
                "Reopening must transition the request to a non-closed status.");
        }

        var normalizedComment = (comment ?? string.Empty).Trim();
        if (normalizedComment.Length > CommentMaximumLength)
        {
            throw new ArgumentException(
                $"Reopen comment cannot exceed {CommentMaximumLength} characters.",
                nameof(comment));
        }

        var previousStatus = Status;
        Status = newStatus;
        ClosedAt = null;

        _statusHistory.Add(new RequestStatusHistoryEntry(
            previousStatus: previousStatus,
            newStatus: newStatus,
            date: reopenDate,
            comment: normalizedComment.Length == 0
                ? $"Reopened by {reopenedBy.Username}."
                : normalizedComment,
            changedBy: reopenedBy));
    }

    public void UpdateDetails(
        string title,
        string description,
        RequestPriority priority,
        DateTime? dueDate,
        string? evidenceUrl,
        DateTime updateDate,
        User updatedBy)
    {
        if (updatedBy is null)
        {
            throw new ArgumentNullException(nameof(updatedBy), "UpdatedBy cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be null or empty.", nameof(title));
        }

        var normalizedTitle = title.Trim();
        if (normalizedTitle.Length > TitleMaximumLength)
        {
            throw new ArgumentException(
                $"Title cannot exceed {TitleMaximumLength} characters.",
                nameof(title));
        }

        var normalizedDescription = (description ?? string.Empty).Trim();
        if (normalizedDescription.Length > DescriptionMaximumLength)
        {
            throw new ArgumentException(
                $"Description cannot exceed {DescriptionMaximumLength} characters.",
                nameof(description));
        }

        if (dueDate.HasValue && dueDate.Value < CreatedAt)
        {
            throw new ArgumentException(
                "DueDate cannot be earlier than CreatedAt.",
                nameof(dueDate));
        }

        Title = normalizedTitle;
        Description = normalizedDescription;
        Priority = priority;
        DueDate = dueDate;
        EvidenceUrl = NormalizeEvidenceUrl(evidenceUrl);

        _statusHistory.Add(new RequestStatusHistoryEntry(
            previousStatus: Status,
            newStatus: Status,
            date: updateDate,
            comment: $"Details updated by {updatedBy.Username}.",
            changedBy: updatedBy));
    }

    public void UnassignResponsible(User unassignedBy, DateTime unassignmentDate, string comment)
    {
        if (unassignedBy is null)
        {
            throw new ArgumentNullException(nameof(unassignedBy), "UnassignedBy cannot be null.");
        }

        if (ResponsibleId is null)
        {
            throw new InvalidOperationException(
                "Request has no responsible user to remove.");
        }

        if (Status == RequestStatus.Closed || Status == RequestStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Cannot unassign a responsible user from a request in status '{Status}'.");
        }

        var normalizedComment = (comment ?? string.Empty).Trim();
        if (normalizedComment.Length > CommentMaximumLength)
        {
            throw new ArgumentException(
                $"Unassignment comment cannot exceed {CommentMaximumLength} characters.",
                nameof(comment));
        }

        var previousResponsibleId = ResponsibleId;
        ResponsibleId = null;
        Responsible = null;

        if (previousResponsibleId is not null)
        {
            var message = normalizedComment.Length == 0
                ? $"Responsible user removed by {unassignedBy.Username}."
                : normalizedComment;
            _statusHistory.Add(new RequestStatusHistoryEntry(
                previousStatus: Status,
                newStatus: Status,
                date: unassignmentDate,
                comment: message,
                changedBy: unassignedBy));
        }
    }

    private static string? NormalizeEvidenceUrl(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        var trimmed = candidate.Trim();
        if (trimmed.Length > EvidenceUrlMaximumLength)
        {
            throw new ArgumentException(
                $"EvidenceUrl cannot exceed {EvidenceUrlMaximumLength} characters.",
                nameof(candidate));
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                $"EvidenceUrl '{trimmed}' is not a valid absolute http(s) URL.",
                nameof(candidate));
        }

        return uri.ToString();
    }

    public void AssignResponsible(User responsible, User assignedBy, DateTime assignmentDate, string comment)
    {
        if (responsible is null)
        {
            throw new ArgumentNullException(nameof(responsible), "Responsible cannot be null.");
        }

        if (!responsible.IsActive)
        {
            throw new InvalidOperationException(
                $"User '{responsible.Username}' is inactive and cannot be assigned as responsible.");
        }

        if (assignedBy is null)
        {
            throw new ArgumentNullException(nameof(assignedBy), "AssignedBy cannot be null.");
        }

        if (Status == RequestStatus.Closed || Status == RequestStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Cannot assign a responsible user to a request in status '{Status}'.");
        }

        var normalizedComment = (comment ?? string.Empty).Trim();
        if (normalizedComment.Length > CommentMaximumLength)
        {
            throw new ArgumentException(
                $"Assignment comment cannot exceed {CommentMaximumLength} characters.",
                nameof(comment));
        }

        var previousResponsibleId = ResponsibleId;
        ResponsibleId = responsible.Id;
        Responsible = responsible;

        if (previousResponsibleId != responsible.Id)
        {
            var message = normalizedComment.Length == 0
                ? $"Assigned to {responsible.Username}."
                : normalizedComment;
            _statusHistory.Add(new RequestStatusHistoryEntry(
                previousStatus: Status,
                newStatus: Status,
                date: assignmentDate,
                comment: message,
                changedBy: assignedBy));
        }
    }

    public RequestComment AddComment(User author, string text, CommentVisibility visibility, DateTime commentDate)
    {
        if (author is null)
        {
            throw new ArgumentNullException(nameof(author), "Comment author cannot be null.");
        }

        if (!author.IsActive)
        {
            throw new InvalidOperationException(
                $"Inactive user '{author.Username}' cannot add comments.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Comment text cannot be null or empty.", nameof(text));
        }

        var normalizedText = text.Trim();
        if (normalizedText.Length > CommentMaximumLength)
        {
            throw new ArgumentException(
                $"Comment text cannot exceed {CommentMaximumLength} characters.",
                nameof(text));
        }

        var comment = new RequestComment(author, normalizedText, visibility, commentDate);
        _comments.Add(comment);
        return comment;
    }

    public RequestNotification RegisterNotification(
        User destinationUser,
        NotificationChannel channel,
        NotificationStatus status,
        string subject,
        string message,
        DateTime notificationDate)
    {
        if (destinationUser is null)
        {
            throw new ArgumentNullException(nameof(destinationUser), "Destination user cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Notification subject cannot be null or empty.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Notification message cannot be null or empty.", nameof(message));
        }

        var normalizedSubject = subject.Trim();
        var normalizedMessage = message.Trim();

        var notification = new RequestNotification(
            destinationUser: destinationUser,
            channel: channel,
            status: status,
            subject: normalizedSubject,
            message: normalizedMessage,
            date: notificationDate);

        notification.AttachToRequest(Id);
        _notifications.Add(notification);
        return notification;
    }
}
