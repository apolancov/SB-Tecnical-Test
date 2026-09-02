using Domain.Enums;

namespace Application.Common.Audit;

public interface IAuditLogger
{
    Task LogAsync(AuditLogContext context, CancellationToken cancellationToken);
}

public sealed record AuditLogContext(
    AuditAction Action,
    AuditOutcome Outcome,
    string EntityType,
    string? EntityId = null,
    string? Details = null,
    Guid? ActorUserId = null,
    string? ActorUserName = null);