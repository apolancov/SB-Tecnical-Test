using Application.Common.Pagination;
using Application.Common.Results;
using Microsoft.Extensions.Logging;

namespace Application.Users.GetUsers;

public sealed class GetUsersQueryHandler
{
    private static readonly Error InvalidPageError = Error.Validation(
        "Page must be greater than or equal to 1.",
        "users.pagination.invalid_page");

    private static readonly Error InvalidPageSizeError = Error.Validation(
        $"Page size must be between 1 and {PaginationDefaults.MaximumPageSize}.",
        "users.pagination.invalid_page_size");

    private readonly IUserAdministrationReadRepository _repository;
    private readonly ILogger<GetUsersQueryHandler> _logger;

    public GetUsersQueryHandler(
        IUserAdministrationReadRepository repository,
        ILogger<GetUsersQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<UserDto>>> HandleAsync(
        GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page < PaginationDefaults.DefaultPage)
        {
            _logger.LogWarning(
                "Rejected user query with invalid page {Page}.",
                query.Page);
            return Result<PaginatedResult<UserDto>>.Failure(InvalidPageError);
        }

        if (query.PageSize < 1
            || query.PageSize > PaginationDefaults.MaximumPageSize)
        {
            _logger.LogWarning(
                "Rejected user query with invalid page size {PageSize}.",
                query.PageSize);
            return Result<PaginatedResult<UserDto>>.Failure(InvalidPageSizeError);
        }

        var criteria = new UserQueryCriteria(
            Username: NormalizeFilter(query.Username),
            Email: NormalizeFilter(query.Email),
            Role: query.Role,
            IsActive: query.IsActive);

        try
        {
            var result = await _repository.SearchAsync(
                criteria,
                query.Page,
                query.PageSize,
                cancellationToken);

            _logger.LogInformation(
                "User query executed. Page={Page} PageSize={PageSize} "
                    + "UsernameFilter={UsernameFilter} EmailFilter={EmailFilter} "
                    + "RoleFilter={RoleFilter} IsActiveFilter={IsActiveFilter} "
                    + "TotalItems={TotalItems}",
                result.Page,
                result.PageSize,
                criteria.Username ?? "(none)",
                criteria.Email ?? "(none)",
                criteria.Role?.ToString() ?? "(none)",
                criteria.IsActive?.ToString() ?? "(none)",
                result.TotalItems);

            return Result<PaginatedResult<UserDto>>.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "User query failed. Page={Page} PageSize={PageSize}",
                query.Page,
                query.PageSize);

            return Result<PaginatedResult<UserDto>>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while querying users.",
                    "users.query.failed"));
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
