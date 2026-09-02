using Domain.Enums;

namespace Application.AuditLog;

public sealed record AuditLogEntryDto(
    Guid Id,
    DateTime Timestamp,
    AuditAction Action,
    AuditOutcome Outcome,
    string EntityType,
    string? EntityId,
    string? Details,
    string? IpAddress,
    Guid? ActorUserId,
    string? ActorUserName);

public sealed record AuditLogQueryCriteria(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? ActorUserId,
    AuditAction? Action,
    AuditOutcome? Outcome,
    string? EntityType,
    string? Search);

public enum AuditLogSortField
{
    Timestamp,
    Action,
    Outcome,
    EntityType,
    ActorUserName
}

public enum AuditLogSortDirection
{
    Ascending,
    Descending
}

public sealed record AuditLogSortOptions(
    AuditLogSortField Field,
    AuditLogSortDirection Direction)
{
    public static readonly AuditLogSortOptions Default =
        new(AuditLogSortField.Timestamp, AuditLogSortDirection.Descending);
}