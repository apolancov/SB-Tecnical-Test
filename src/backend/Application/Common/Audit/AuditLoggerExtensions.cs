using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Audit;

public static class AuditLoggerExtensions
{
    public static Task LogRequestCreatedAsync(
        this IAuditLogger auditLogger,
        Request request,
        User actor,
        CancellationToken cancellationToken)
    {
        return auditLogger.LogAsync(
            new AuditLogContext(
                Action: AuditAction.RequestCreated,
                Outcome: AuditOutcome.Success,
                EntityType: nameof(Request),
                EntityId: request.Id.ToString(),
                Details: $"Request '{request.Code}' created by '{actor.Username}'.",
                ActorUserId: actor.Id,
                ActorUserName: actor.Username),
            cancellationToken);
    }

    public static Task LogRequestUpdatedAsync(
        this IAuditLogger auditLogger,
        Request request,
        User actor,
        CancellationToken cancellationToken)
    {
        return auditLogger.LogAsync(
            new AuditLogContext(
                Action: AuditAction.RequestUpdated,
                Outcome: AuditOutcome.Success,
                EntityType: nameof(Request),
                EntityId: request.Id.ToString(),
                Details: $"Request '{request.Code}' updated by '{actor.Username}'.",
                ActorUserId: actor.Id,
                ActorUserName: actor.Username),
            cancellationToken);
    }

    public static Task LogRequestAssignedAsync(
        this IAuditLogger auditLogger,
        Request request,
        User responsible,
        User actor,
        CancellationToken cancellationToken)
    {
        return auditLogger.LogAsync(
            new AuditLogContext(
                Action: AuditAction.RequestAssigned,
                Outcome: AuditOutcome.Success,
                EntityType: nameof(Request),
                EntityId: request.Id.ToString(),
                Details: $"Request '{request.Code}' assigned to '{responsible.Username}' by '{actor.Username}'.",
                ActorUserId: actor.Id,
                ActorUserName: actor.Username),
            cancellationToken);
    }

    public static Task LogRequestStatusChangedAsync(
        this IAuditLogger auditLogger,
        Request request,
        RequestStatus previousStatus,
        RequestStatus newStatus,
        User actor,
        CancellationToken cancellationToken)
    {
        return auditLogger.LogAsync(
            new AuditLogContext(
                Action: AuditAction.RequestStatusChanged,
                Outcome: AuditOutcome.Success,
                EntityType: nameof(Request),
                EntityId: request.Id.ToString(),
                Details: $"Request '{request.Code}' status changed from '{previousStatus}' to '{newStatus}' by '{actor.Username}'.",
                ActorUserId: actor.Id,
                ActorUserName: actor.Username),
            cancellationToken);
    }

    public static Task LogRequestReopenedAsync(
        this IAuditLogger auditLogger,
        Request request,
        User actor,
        CancellationToken cancellationToken)
    {
        return auditLogger.LogAsync(
            new AuditLogContext(
                Action: AuditAction.RequestReopened,
                Outcome: AuditOutcome.Success,
                EntityType: nameof(Request),
                EntityId: request.Id.ToString(),
                Details: $"Request '{request.Code}' reopened by '{actor.Username}'.",
                ActorUserId: actor.Id,
                ActorUserName: actor.Username),
            cancellationToken);
    }

    public static Task LogRequestCommentAddedAsync(
        this IAuditLogger auditLogger,
        Request request,
        User actor,
        CancellationToken cancellationToken)
    {
        return auditLogger.LogAsync(
            new AuditLogContext(
                Action: AuditAction.RequestCommentAdded,
                Outcome: AuditOutcome.Success,
                EntityType: nameof(Request),
                EntityId: request.Id.ToString(),
                Details: $"Comment added to request '{request.Code}' by '{actor.Username}'.",
                ActorUserId: actor.Id,
                ActorUserName: actor.Username),
            cancellationToken);
    }

    public static Task LogInstitutionCreatedAsync(
        this IAuditLogger auditLogger,
        Institution institution,
        User actor,
        CancellationToken cancellationToken)
    {
        return auditLogger.LogAsync(
            new AuditLogContext(
                Action: AuditAction.InstitutionCreated,
                Outcome: AuditOutcome.Success,
                EntityType: nameof(Institution),
                EntityId: institution.Id.ToString(),
                Details: $"Institution '{institution.Name}' created by '{actor.Username}'.",
                ActorUserId: actor.Id,
                ActorUserName: actor.Username),
            cancellationToken);
    }

    public static Task LogInstitutionUpdatedAsync(
        this IAuditLogger auditLogger,
        Institution institution,
        User actor,
        CancellationToken cancellationToken)
    {
        return auditLogger.LogAsync(
            new AuditLogContext(
                Action: AuditAction.InstitutionUpdated,
                Outcome: AuditOutcome.Success,
                EntityType: nameof(Institution),
                EntityId: institution.Id.ToString(),
                Details: $"Institution '{institution.Name}' updated by '{actor.Username}'.",
                ActorUserId: actor.Id,
                ActorUserName: actor.Username),
            cancellationToken);
    }

    public static Task LogInstitutionDeletedAsync(
        this IAuditLogger auditLogger,
        Guid institutionId,
        string institutionName,
        User actor,
        CancellationToken cancellationToken)
    {
        return auditLogger.LogAsync(
            new AuditLogContext(
                Action: AuditAction.InstitutionDeleted,
                Outcome: AuditOutcome.Success,
                EntityType: nameof(Institution),
                EntityId: institutionId.ToString(),
                Details: $"Institution '{institutionName}' deleted by '{actor.Username}'.",
                ActorUserId: actor.Id,
                ActorUserName: actor.Username),
            cancellationToken);
    }

    public static Task LogAuthorizationDeniedAsync(
        this IAuditLogger auditLogger,
        string policyName,
        string endpoint,
        Guid? actorUserId,
        string? actorUserName,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var details = string.IsNullOrWhiteSpace(actorUserName)
            ? $"Authorization denied for policy '{policyName}' on '{endpoint}'."
            : $"Authorization denied for user '{actorUserName}' for policy '{policyName}' on '{endpoint}'.";

        return auditLogger.LogAsync(
            new AuditLogContext(
                Action: AuditAction.AuthorizationDenied,
                Outcome: AuditOutcome.Denied,
                EntityType: policyName,
                EntityId: endpoint,
                Details: details,
                ActorUserId: actorUserId,
                ActorUserName: actorUserName),
            cancellationToken);
    }
}