using Domain.Enums;

namespace Domain.Entities;

public class AuditLogEntry
{
    public const int EntityTypeMaximumLength = 64;
    public const int EntityIdMaximumLength = 64;
    public const int DetailsMaximumLength = 2048;
    public const int IpAddressMaximumLength = 64;
    public const int UserNameMaximumLength = 64;

    public Guid Id { get; private set; }

    public DateTime Timestamp { get; private set; }

    public AuditAction Action { get; private set; }

    public AuditOutcome Outcome { get; private set; }

    public string EntityType { get; private set; }

    public string? EntityId { get; private set; }

    public string? Details { get; private set; }

    public string? IpAddress { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string? ActorUserName { get; private set; }

    private AuditLogEntry()
    {
        EntityType = string.Empty;
    }

    public AuditLogEntry(
        AuditAction action,
        AuditOutcome outcome,
        string entityType,
        string? entityId = null,
        string? details = null,
        string? ipAddress = null,
        Guid? actorUserId = null,
        string? actorUserName = null,
        DateTime? timestamp = null)
    {
        if (!Enum.IsDefined(typeof(AuditAction), action))
        {
            throw new ArgumentException(
                $"AuditAction '{action}' is not a supported value.",
                nameof(action));
        }

        if (!Enum.IsDefined(typeof(AuditOutcome), outcome))
        {
            throw new ArgumentException(
                $"AuditOutcome '{outcome}' is not a supported value.",
                nameof(outcome));
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException(
                "EntityType cannot be null or empty.",
                nameof(entityType));
        }

        var normalizedEntityType = entityType.Trim();
        if (normalizedEntityType.Length > EntityTypeMaximumLength)
        {
            throw new ArgumentException(
                $"EntityType cannot exceed {EntityTypeMaximumLength} characters.",
                nameof(entityType));
        }

        var normalizedEntityId = NormalizeOptional(entityId, EntityIdMaximumLength, nameof(entityId));
        var normalizedDetails = NormalizeOptional(details, DetailsMaximumLength, nameof(details));
        var normalizedIpAddress = NormalizeOptional(ipAddress, IpAddressMaximumLength, nameof(ipAddress));
        var normalizedActorUserName = NormalizeOptional(actorUserName, UserNameMaximumLength, nameof(actorUserName));

        Id = Guid.NewGuid();
        Timestamp = (timestamp ?? DateTime.UtcNow).ToUniversalTime();
        Action = action;
        Outcome = outcome;
        EntityType = normalizedEntityType;
        EntityId = normalizedEntityId;
        Details = normalizedDetails;
        IpAddress = normalizedIpAddress;
        ActorUserId = actorUserId == Guid.Empty ? null : actorUserId;
        ActorUserName = normalizedActorUserName;
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return trimmed;
    }
}