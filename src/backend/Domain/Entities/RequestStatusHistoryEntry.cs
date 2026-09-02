using Domain.Enums;

namespace Domain.Entities;

public class RequestStatusHistoryEntry
{
    public const int CommentMaximumLength = 1024;

    public Guid Id { get; private set; }

    public Guid RequestId { get; private set; }

    public RequestStatus PreviousStatus { get; private set; }

    public RequestStatus NewStatus { get; private set; }

    public DateTime Date { get; private set; }

    public string Comment { get; private set; }

    public Guid ChangedById { get; private set; }

    public User ChangedBy { get; private set; }

    private RequestStatusHistoryEntry()
    {
        Comment = string.Empty;
        ChangedBy = null!;
    }

    public RequestStatusHistoryEntry(
        RequestStatus previousStatus,
        RequestStatus newStatus,
        DateTime date,
        string comment,
        User changedBy)
    {
        if (changedBy is null)
        {
            throw new ArgumentNullException(nameof(changedBy), "ChangedBy cannot be null.");
        }

        var normalizedComment = (comment ?? string.Empty).Trim();
        if (normalizedComment.Length > CommentMaximumLength)
        {
            throw new ArgumentException(
                $"History comment cannot exceed {CommentMaximumLength} characters.",
                nameof(comment));
        }

        Id = Guid.NewGuid();
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Date = date;
        Comment = normalizedComment;
        ChangedById = changedBy.Id;
        ChangedBy = changedBy;
    }
}
