using Application.Common.Pagination;
using Application.Common.Results;
using Application.AuditLog;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.AuditLog.GetAuditLogEntries;

public sealed class GetAuditLogEntriesQueryHandler
{
    private static readonly Error InvalidPageError = Error.Validation(
        "Page must be greater than or equal to 1.",
        "auditlog.pagination.invalid_page");

    private static readonly Error InvalidPageSizeError = Error.Validation(
        $"Page size must be between 1 and {PaginationDefaults.MaximumPageSize}.",
        "auditlog.pagination.invalid_page_size");

    private static readonly Error InvalidActionError = Error.Validation(
        "Action is not a valid value.",
        "auditlog.filter.action.invalid");

    private static readonly Error InvalidOutcomeError = Error.Validation(
        "Outcome is not a valid value.",
        "auditlog.filter.outcome.invalid");

    private static readonly Error InvalidSortFieldError = Error.Validation(
        "Sort field is not a valid value.",
        "auditlog.sort.field.invalid");

    private static readonly Error InvalidSortDirectionError = Error.Validation(
        "Sort direction is not a valid value.",
        "auditlog.sort.direction.invalid");

    private static readonly Error InvalidDateRangeError = Error.Validation(
        "FromDate cannot be later than ToDate.",
        "auditlog.filter.date_range.invalid");

    private readonly IAuditLogReadRepository _repository;
    private readonly ILogger<GetAuditLogEntriesQueryHandler> _logger;

    public GetAuditLogEntriesQueryHandler(
        IAuditLogReadRepository repository,
        ILogger<GetAuditLogEntriesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<AuditLogEntryDto>>> HandleAsync(
        GetAuditLogEntriesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page < PaginationDefaults.DefaultPage)
        {
            _logger.LogWarning(
                "Rejected audit log query with invalid page {Page}.",
                query.Page);
            return Result<PaginatedResult<AuditLogEntryDto>>.Failure(InvalidPageError);
        }

        if (query.PageSize < 1 || query.PageSize > PaginationDefaults.MaximumPageSize)
        {
            _logger.LogWarning(
                "Rejected audit log query with invalid page size {PageSize}.",
                query.PageSize);
            return Result<PaginatedResult<AuditLogEntryDto>>.Failure(InvalidPageSizeError);
        }

        if (query.Action.HasValue && !Enum.IsDefined(typeof(AuditAction), query.Action.Value))
        {
            return Result<PaginatedResult<AuditLogEntryDto>>.Failure(InvalidActionError);
        }

        if (query.Outcome.HasValue && !Enum.IsDefined(typeof(AuditOutcome), query.Outcome.Value))
        {
            return Result<PaginatedResult<AuditLogEntryDto>>.Failure(InvalidOutcomeError);
        }

        if (!Enum.IsDefined(typeof(AuditLogSortField), query.SortField))
        {
            return Result<PaginatedResult<AuditLogEntryDto>>.Failure(InvalidSortFieldError);
        }

        if (!Enum.IsDefined(typeof(AuditLogSortDirection), query.SortDirection))
        {
            return Result<PaginatedResult<AuditLogEntryDto>>.Failure(InvalidSortDirectionError);
        }

        var fromDate = query.FromDate?.ToUniversalTime();
        var toDate = query.ToDate?.ToUniversalTime();
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return Result<PaginatedResult<AuditLogEntryDto>>.Failure(InvalidDateRangeError);
        }

        var criteria = new AuditLogQueryCriteria(
            FromDate: fromDate,
            ToDate: toDate,
            ActorUserId: query.ActorUserId == Guid.Empty ? null : query.ActorUserId,
            Action: query.Action,
            Outcome: query.Outcome,
            EntityType: NormalizeFilter(query.EntityType),
            Search: NormalizeFilter(query.Search));

        var sortOptions = new AuditLogSortOptions(query.SortField, query.SortDirection);

        try
        {
            var result = await _repository
                .SearchAsync(criteria, query.Page, query.PageSize, sortOptions, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Audit log query executed. Page={Page} PageSize={PageSize} TotalItems={TotalItems}",
                result.Page,
                result.PageSize,
                result.TotalItems);

            return Result<PaginatedResult<AuditLogEntryDto>>.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Audit log query failed. Page={Page} PageSize={PageSize}",
                query.Page,
                query.PageSize);
            return Result<PaginatedResult<AuditLogEntryDto>>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while querying the audit log.",
                    "auditlog.query.failed"));
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