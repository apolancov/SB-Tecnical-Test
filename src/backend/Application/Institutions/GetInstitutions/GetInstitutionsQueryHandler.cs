using Application.Common.Pagination;
using Application.Common.Results;
using Microsoft.Extensions.Logging;

namespace Application.Institutions.GetInstitutions;

public sealed class GetInstitutionsQueryHandler
{
    private static readonly Error InvalidPageError = Error.Validation(
        "Page must be greater than or equal to 1.",
        "institutions.pagination.invalid_page");

    private static readonly Error InvalidPageSizeError = Error.Validation(
        $"Page size must be between 1 and {PaginationDefaults.MaximumPageSize}.",
        "institutions.pagination.invalid_page_size");

    private readonly IInstitutionReadRepository _repository;
    private readonly ILogger<GetInstitutionsQueryHandler> _logger;

    public GetInstitutionsQueryHandler(
        IInstitutionReadRepository repository,
        ILogger<GetInstitutionsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<InstitutionDto>>> HandleAsync(
        GetInstitutionsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page < PaginationDefaults.DefaultPage)
        {
            _logger.LogWarning(
                "Rejected institution query with invalid page {Page}.",
                query.Page);
            return Result<PaginatedResult<InstitutionDto>>.Failure(InvalidPageError);
        }

        if (query.PageSize < 1
            || query.PageSize > PaginationDefaults.MaximumPageSize)
        {
            _logger.LogWarning(
                "Rejected institution query with invalid page size {PageSize}.",
                query.PageSize);
            return Result<PaginatedResult<InstitutionDto>>.Failure(InvalidPageSizeError);
        }

        var criteria = new InstitutionQueryCriteria(
            Name: NormalizeFilter(query.Name),
            Category: NormalizeFilter(query.Category),
            StatePower: NormalizeFilter(query.StatePower),
            Sector: NormalizeFilter(query.Sector));

        try
        {
            var result = await _repository.SearchAsync(
                criteria,
                query.Page,
                query.PageSize,
                cancellationToken);

            _logger.LogInformation(
                "Institution query executed. Page={Page} PageSize={PageSize} "
                    + "NameFilter={NameFilter} CategoryFilter={CategoryFilter} "
                    + "StatePowerFilter={StatePowerFilter} SectorFilter={SectorFilter} "
                    + "TotalItems={TotalItems}",
                result.Page,
                result.PageSize,
                criteria.Name ?? "(none)",
                criteria.Category ?? "(none)",
                criteria.StatePower ?? "(none)",
                criteria.Sector ?? "(none)",
                result.TotalItems);

            return Result<PaginatedResult<InstitutionDto>>.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Institution query failed. Page={Page} PageSize={PageSize}",
                query.Page,
                query.PageSize);

            return Result<PaginatedResult<InstitutionDto>>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while querying institutions.",
                    "institutions.query.failed"));
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