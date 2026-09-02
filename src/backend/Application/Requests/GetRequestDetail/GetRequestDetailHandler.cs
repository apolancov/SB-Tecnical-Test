using Application.Common.Results;
using Application.Requests;
using Application.Requests.Authorization;
using Microsoft.Extensions.Logging;

namespace Application.Requests.GetRequestDetail;

public sealed class GetRequestDetailHandler
{
    private static readonly Error NotFoundError = Error.NotFound(
        "Request not found.",
        "requests.read.not_found");

    private static readonly Error ForbiddenError = Error.Forbidden(
        "You do not have access to this request.",
        "requests.read.forbidden");

    private readonly IRequestReadRepository _repository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ILogger<GetRequestDetailHandler> _logger;

    public GetRequestDetailHandler(
        IRequestReadRepository repository,
        ICurrentUserAccessor currentUserAccessor,
        ILogger<GetRequestDetailHandler> logger)
    {
        _repository = repository;
        _currentUserAccessor = currentUserAccessor;
        _logger = logger;
    }

    public async Task<Result<RequestDetailDto>> HandleAsync(
        GetRequestDetailQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Id == Guid.Empty)
        {
            _logger.LogWarning(
                "Rejected request detail read because the identifier was empty.");
            return Result<RequestDetailDto>.Failure(NotFoundError);
        }

        var currentUserId = _currentUserAccessor.GetCurrentUserId();
        var currentRole = _currentUserAccessor.GetCurrentUserRole();

        var detail = await _repository
            .GetDetailAsync(query.Id, currentUserId, currentRole, cancellationToken)
            .ConfigureAwait(false);

        if (detail is null)
        {
            _logger.LogInformation(
                "Request detail returned no result. RequestId={RequestId}",
                query.Id);
            return Result<RequestDetailDto>.Failure(NotFoundError);
        }

        return Result<RequestDetailDto>.Success(detail);
    }
}