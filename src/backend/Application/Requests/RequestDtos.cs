using Domain.Enums;

namespace Application.Requests;

public sealed record RequestDto(
    Guid Id,
    string Code,
    string Title,
    string Description,
    RequestPriority Priority,
    RequestStatus Status,
    DateTime CreatedAt,
    DateTime? DueDate,
    string? EvidenceUrl,
    DateTime? ClosedAt,
    Guid AreaId,
    string Area,
    Guid RequestTypeId,
    string RequestType,
    Guid RequesterId,
    string RequesterUsername,
    string RequesterEmail,
    Guid? ResponsibleId,
    string? ResponsibleUsername,
    string? ResponsibleEmail);

public sealed record RequestDetailDto(
    Guid Id,
    string Code,
    string Title,
    string Description,
    RequestPriority Priority,
    RequestStatus Status,
    DateTime CreatedAt,
    DateTime? DueDate,
    string? EvidenceUrl,
    DateTime? ClosedAt,
    RequestUserDto Requester,
    RequestUserDto? Responsible,
    RequestLookupDto Area,
    RequestLookupDto RequestType,
    IReadOnlyList<RequestStatusHistoryDto> StatusHistory,
    IReadOnlyList<RequestCommentDto> Comments);

public sealed record RequestUserDto(
    Guid Id,
    string Username,
    string Email,
    UserRole Role);

public sealed record RequestLookupDto(
    Guid Id,
    string Name);

public sealed record RequestStatusHistoryDto(
    Guid Id,
    RequestStatus PreviousStatus,
    RequestStatus NewStatus,
    DateTime Date,
    string Comment,
    Guid ChangedById,
    string ChangedByUsername);

public sealed record RequestCommentDto(
    Guid Id,
    string Text,
    CommentVisibility Visibility,
    DateTime Date,
    Guid AuthorId,
    string AuthorUsername);