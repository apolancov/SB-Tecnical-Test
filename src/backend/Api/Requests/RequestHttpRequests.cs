using Domain.Enums;

namespace Api.Requests;

public sealed record CreateRequestHttpRequest(
    string? Title,
    string? Description,
    RequestPriority? Priority,
    Guid? AreaId,
    Guid? RequestTypeId,
    DateTime? DueDate,
    string? EvidenceUrl);

public sealed record ChangeRequestStatusHttpRequest(
    RequestStatus? NewStatus,
    string? Comment);

public sealed record AssignRequestHttpRequest(
    Guid? ResponsibleUserId,
    string? Comment);

public sealed record AddRequestCommentHttpRequest(
    string? Text,
    CommentVisibility? Visibility);

public sealed record ReopenRequestHttpRequest(
    RequestStatus? TargetStatus,
    string? Comment);

public sealed record UpdateRequestHttpRequest(
    string? Title,
    string? Description,
    RequestPriority? Priority,
    DateTime? DueDate,
    string? EvidenceUrl);