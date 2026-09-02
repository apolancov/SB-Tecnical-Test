using Application.Common.Pagination;
using Application.Common.Results;
using Application.Requests;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Requests.ListRequests;

public sealed class ListRequestsQueryHandler
{
    private static readonly Error InvalidPageError = Error.Validation(
        "Page must be greater than or equal to 1.",
        "requests.pagination.invalid_page");

    private static readonly Error InvalidPageSizeError = Error.Validation(
        $"Page size must be between 1 and {PaginationDefaults.MaximumPageSize}.",
        "requests.pagination.invalid_page_size");

    private static readonly Error InvalidStatusError = Error.Validation(
        "Status is not a valid value.",
        "requests.filter.status.invalid");

    private static readonly Error InvalidPriorityError = Error.Validation(
        "Priority is not a valid value.",
        "requests.filter.priority.invalid");

    private static readonly Error InvalidSortFieldError = Error.Validation(
        "Sort field is not a valid value.",
        "requests.sort.field.invalid");

    private static readonly Error InvalidSortDirectionError = Error.Validation(
        "Sort direction is not a valid value.",
        "requests.sort.direction.invalid");

    private static readonly Error InvalidDateRangeError = Error.Validation(
        "FromDate cannot be later than ToDate.",
        "requests.filter.date_range.invalid");

    private readonly IRequestReadRepository _repository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ILogger<ListRequestsQueryHandler> _logger;

    public ListRequestsQueryHandler(
        IRequestReadRepository repository,
        ICurrentUserAccessor currentUserAccessor,
        ILogger<ListRequestsQueryHandler> logger)
    {
        _repository = repository;
        _currentUserAccessor = currentUserAccessor;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<RequestDto>>> HandleAsync(
        ListRequestsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page < PaginationDefaults.DefaultPage)
        {
            _logger.LogWarning(
                "Rejected request query with invalid page {Page}.",
                query.Page);
            return Result<PaginatedResult<RequestDto>>.Failure(InvalidPageError);
        }

        if (query.PageSize < 1 || query.PageSize > PaginationDefaults.MaximumPageSize)
        {
            _logger.LogWarning(
                "Rejected request query with invalid page size {PageSize}.",
                query.PageSize);
            return Result<PaginatedResult<RequestDto>>.Failure(InvalidPageSizeError);
        }

        if (query.Status.HasValue && !Enum.IsDefined(typeof(RequestStatus), query.Status.Value))
        {
            return Result<PaginatedResult<RequestDto>>.Failure(InvalidStatusError);
        }

        if (query.Priority.HasValue && !Enum.IsDefined(typeof(RequestPriority), query.Priority.Value))
        {
            return Result<PaginatedResult<RequestDto>>.Failure(InvalidPriorityError);
        }

        if (!Enum.IsDefined(typeof(RequestSortField), query.SortField))
        {
            return Result<PaginatedResult<RequestDto>>.Failure(InvalidSortFieldError);
        }

        if (!Enum.IsDefined(typeof(RequestSortDirection), query.SortDirection))
        {
            return Result<PaginatedResult<RequestDto>>.Failure(InvalidSortDirectionError);
        }

        var fromDate = query.FromDate?.ToUniversalTime();
        var toDate = query.ToDate?.ToUniversalTime();
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return Result<PaginatedResult<RequestDto>>.Failure(InvalidDateRangeError);
        }

        var criteria = new RequestQueryCriteria(
            Status: query.Status,
            Priority: query.Priority,
            AreaId: query.AreaId == Guid.Empty ? null : query.AreaId,
            RequestTypeId: query.RequestTypeId == Guid.Empty ? null : query.RequestTypeId,
            RequesterId: query.RequesterId == Guid.Empty ? null : query.RequesterId,
            ResponsibleId: query.ResponsibleId == Guid.Empty ? null : query.ResponsibleId,
            FromDate: fromDate,
            ToDate: toDate,
            Search: NormalizeFilter(query.Search),
            Code: NormalizeFilter(query.Code));

        var sortOptions = new RequestSortOptions(query.SortField, query.SortDirection);

        var currentUserId = _currentUserAccessor.GetCurrentUserId();
        var currentRole = _currentUserAccessor.GetCurrentUserRole();

        try
        {
            var result = await _repository
                .SearchAsync(criteria, query.Page, query.PageSize, sortOptions, currentUserId, currentRole, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Request query executed. Page={Page} PageSize={PageSize} TotalItems={TotalItems}",
                result.Page,
                result.PageSize,
                result.TotalItems);

            return Result<PaginatedResult<RequestDto>>.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Request query failed. Page={Page} PageSize={PageSize}",
                query.Page, query.PageSize);
            return Result<PaginatedResult<RequestDto>>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while querying requests.",
                    "requests.query.failed"));
        }
    }

    private static string? NormalizeFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}