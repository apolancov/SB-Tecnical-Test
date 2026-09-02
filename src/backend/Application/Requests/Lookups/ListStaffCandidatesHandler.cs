using Application.Common.Results;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Requests.Lookups;

public sealed class ListStaffCandidatesHandler
{
    private static readonly Error ForbiddenError = Error.Forbidden(
        "Only Admin and Analista users can list staff candidates.",
        "requests.lookups.staff.forbidden");

    private static readonly Error UnauthorizedError = Error.Unauthorized(
        "Authenticated user could not be resolved.",
        "requests.lookups.staff.user.unresolved");

    private readonly IStaffCandidateRepository _repository;
    private readonly Application.Requests.ICurrentUserAccessor _currentUserAccessor;
    private readonly ILogger<ListStaffCandidatesHandler> _logger;

    public ListStaffCandidatesHandler(
        IStaffCandidateRepository repository,
        Application.Requests.ICurrentUserAccessor currentUserAccessor,
        ILogger<ListStaffCandidatesHandler> logger)
    {
        _repository = repository;
        _currentUserAccessor = currentUserAccessor;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<StaffCandidateDto>>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserAccessor.GetCurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            return Result<IReadOnlyList<StaffCandidateDto>>.Failure(UnauthorizedError);
        }

        var currentRole = _currentUserAccessor.GetCurrentUserRole();
        if (currentRole != UserRole.Admin && currentRole != UserRole.Analista)
        {
            return Result<IReadOnlyList<StaffCandidateDto>>.Failure(ForbiddenError);
        }

        try
        {
            var users = await _repository
                .ListStaffCandidatesAsync(cancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<StaffCandidateDto> items = users
                .OrderBy(user => user.Username, System.StringComparer.OrdinalIgnoreCase)
                .Select(user => new StaffCandidateDto(
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Role.ToString()))
                .ToList();

            _logger.LogInformation(
                "Staff candidates listed. Count={Count} RequestedBy={RequestedById}",
                items.Count,
                currentUserId);

            return Result<IReadOnlyList<StaffCandidateDto>>.Success(items);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Staff candidates lookup failed.");
            return Result<IReadOnlyList<StaffCandidateDto>>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while reading staff candidates.",
                    "requests.lookups.staff.failed"));
        }
    }
}