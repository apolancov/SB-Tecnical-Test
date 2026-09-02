using Domain.Enums;

namespace Application.Requests.ListRequests;

public sealed record ListRequestsQuery(
    int Page,
    int PageSize,
    RequestStatus? Status,
    RequestPriority? Priority,
    Guid? AreaId,
    Guid? RequestTypeId,
    Guid? RequesterId,
    Guid? ResponsibleId,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Code,
    string? Search,
    RequestSortField SortField,
    RequestSortDirection SortDirection);