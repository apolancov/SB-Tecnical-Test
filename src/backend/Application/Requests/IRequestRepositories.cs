using Application.Common.Pagination;
using Domain.Entities;
using Domain.Enums;

namespace Application.Requests;

public interface IRequestReadRepository
{
    Task<PaginatedResult<RequestDto>> SearchAsync(
        RequestQueryCriteria criteria,
        int page,
        int pageSize,
        RequestSortOptions sortOptions,
        Guid currentUserId,
        UserRole currentRole,
        CancellationToken cancellationToken);

    Task<RequestDetailDto?> GetDetailAsync(
        Guid requestId,
        Guid currentUserId,
        UserRole currentRole,
        CancellationToken cancellationToken);

    Task<Request?> FindEntityByIdAsync(Guid requestId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestStatusHistoryDto>> GetStatusHistoryAsync(
        Guid requestId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestCommentDto>> GetCommentsAsync(
        Guid requestId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestSummaryDto>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken);

    Task<int> CountOverdueAsync(CancellationToken cancellationToken);
}

public interface IRequestWriteRepository
{
    Task AddAsync(Request request, CancellationToken cancellationToken);

    Task UpdateAsync(Request request, CancellationToken cancellationToken);
}

public interface IAreaReadRepository
{
    Task<Area?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Area>> ListActiveAsync(CancellationToken cancellationToken);
}

public interface IRequestTypeReadRepository
{
    Task<RequestType?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestType>> ListActiveAsync(CancellationToken cancellationToken);
}

public interface ICurrentUserAccessor
{
    Guid GetCurrentUserId();

    UserRole GetCurrentUserRole();
}

public sealed record RequestLookupOptions(
    IReadOnlyList<RequestStatusLookup> Statuses,
    IReadOnlyList<RequestPriorityLookup> Priorities);

public sealed record RequestStatusLookup(RequestStatus Value, string Name);

public sealed record RequestPriorityLookup(RequestPriority Value, string Name);

public sealed record RequestSummaryDto(
    Guid Id,
    string Code,
    string Title,
    RequestPriority Priority,
    RequestStatus Status,
    DateTime CreatedAt,
    DateTime? DueDate,
    Guid RequesterId,
    string RequesterUsername,
    Guid? ResponsibleId,
    string? ResponsibleUsername);

public sealed record RequestSortOptions(
    RequestSortField Field,
    RequestSortDirection Direction)
{
    public static readonly RequestSortOptions Default =
        new(RequestSortField.CreatedAt, RequestSortDirection.Descending);
}

public enum RequestSortField
{
    CreatedAt,
    Code,
    Title,
    Priority,
    Status,
    DueDate
}

public enum RequestSortDirection
{
    Ascending,
    Descending
}