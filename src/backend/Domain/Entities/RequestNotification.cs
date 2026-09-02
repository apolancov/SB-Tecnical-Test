using Domain.Enums;

namespace Domain.Entities;

public class RequestNotification
{
    public const int SubjectMaximumLength = 256;
    public const int MessageMaximumLength = 4096;

    public Guid Id { get; private set; }

    public Guid RequestId { get; private set; }

    public Guid DestinationUserId { get; private set; }

    public User DestinationUser { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public NotificationStatus Status { get; private set; }

    public string Subject { get; private set; }

    public string Message { get; private set; }

    public DateTime Date { get; private set; }

    private RequestNotification()
    {
        Subject = string.Empty;
        Message = string.Empty;
        DestinationUser = null!;
    }

    public RequestNotification(
        User destinationUser,
        NotificationChannel channel,
        NotificationStatus status,
        string subject,
        string message,
        DateTime date)
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
        if (normalizedSubject.Length > SubjectMaximumLength)
        {
            throw new ArgumentException(
                $"Notification subject cannot exceed {SubjectMaximumLength} characters.",
                nameof(subject));
        }

        var normalizedMessage = message.Trim();
        if (normalizedMessage.Length > MessageMaximumLength)
        {
            throw new ArgumentException(
                $"Notification message cannot exceed {MessageMaximumLength} characters.",
                nameof(message));
        }

        Id = Guid.NewGuid();
        DestinationUserId = destinationUser.Id;
        DestinationUser = destinationUser;
        Channel = channel;
        Status = status;
        Subject = normalizedSubject;
        Message = normalizedMessage;
        Date = date;
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
    }

    public void MarkFailed()
    {
        Status = NotificationStatus.Failed;
    }

    internal void AttachToRequest(Guid requestId)
    {
        if (requestId == Guid.Empty)
        {
            throw new ArgumentException(
                "RequestId cannot be empty.",
                nameof(requestId));
        }

        RequestId = requestId;
    }
}
