using Domain.Enums;

namespace Application.Requests;

public sealed record RequestQueryCriteria(
    RequestStatus? Status,
    RequestPriority? Priority,
    Guid? AreaId,
    Guid? RequestTypeId,
    Guid? RequesterId,
    Guid? ResponsibleId,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Search,
    string? Code);