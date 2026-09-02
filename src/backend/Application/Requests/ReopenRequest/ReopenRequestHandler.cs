using Application.Common.Audit;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.Authorization;
using Application.Requests.Notifications;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Requests.ReopenRequest;

public sealed class ReopenRequestHandler
{
    private static readonly Error RequestNotFoundError = Error.NotFound(
        "Request not found.",
        "requests.reopen.not_found");

    private static readonly Error InvalidStatusError = Error.Validation(
        "Target status is not a valid value.",
        "requests.reopen.status.invalid");

    private static readonly Error InvalidTransitionError = Error.Validation(
        "The requested reopen transition is not allowed.",
        "requests.reopen.transition.invalid");

    private static readonly Error AlreadyOpenError = Error.Conflict(
        "Only requests in 'Closed' status can be reopened.",
        "requests.reopen.conflict.not_closed");

    private static readonly Error UnauthorizedError = Error.Unauthorized(
        "Authenticated user could not be resolved.",
        "requests.reopen.user.unresolved");

    private static readonly Error UserNotFoundError = Error.Unauthorized(
        "Authenticated user does not exist in the system.",
        "requests.reopen.user.not_found");

    private static readonly Error InactiveUserError = Error.Forbidden(
        "Inactive users cannot reopen requests.",
        "requests.reopen.user.inactive");

    private static readonly Error ForbiddenError = Error.Forbidden(
        "Only Admin and Analista users can reopen requests.",
        "requests.reopen.forbidden");

    private readonly IRequestReadRepository _readRepository;
    private readonly IRequestWriteRepository _writeRepository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly Application.Common.Users.IUserLookupRepository _userLookup;
    private readonly INotificationSender _notificationSender;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<ReopenRequestHandler> _logger;

    public ReopenRequestHandler(
        IRequestReadRepository readRepository,
        IRequestWriteRepository writeRepository,
        ICurrentUserAccessor currentUserAccessor,
        Application.Common.Users.IUserLookupRepository userLookup,
        INotificationSender notificationSender,
        IAuditLogger auditLogger,
        ILogger<ReopenRequestHandler> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _currentUserAccessor = currentUserAccessor;
        _userLookup = userLookup;
        _notificationSender = notificationSender;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Result<RequestDto>> HandleAsync(
        ReopenRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (command.RequestId == Guid.Empty)
        {
            return Result<RequestDto>.Failure(RequestNotFoundError);
        }

        if (!Enum.IsDefined(typeof(RequestStatus), command.TargetStatus))
        {
            return Result<RequestDto>.Failure(InvalidStatusError);
        }

        var actorId = _currentUserAccessor.GetCurrentUserId();
        if (actorId == Guid.Empty)
        {
            return Result<RequestDto>.Failure(UnauthorizedError);
        }

        var actor = await _userLookup.FindByIdAsync(actorId, cancellationToken).ConfigureAwait(false);
        if (actor is null)
        {
            return Result<RequestDto>.Failure(UserNotFoundError);
        }

        if (!actor.IsActive)
        {
            return Result<RequestDto>.Failure(InactiveUserError);
        }

        var request = await _readRepository
            .FindEntityByIdAsync(command.RequestId, cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
        {
            return Result<RequestDto>.Failure(RequestNotFoundError);
        }

        if (!RequestAuthorization.CanReopen(request, actorId, actor.Role))
        {
            return Result<RequestDto>.Failure(ForbiddenError);
        }

        if (request.Status != RequestStatus.Closed)
        {
            return Result<RequestDto>.Failure(AlreadyOpenError);
        }

        if (!RequestStatusTransition.IsAllowed(RequestStatus.Closed, command.TargetStatus))
        {
            return Result<RequestDto>.Failure(InvalidTransitionError);
        }

        var comment = command.Comment ?? string.Empty;

        try
        {
            request.Reopen(command.TargetStatus, actor, comment, DateTime.UtcNow);
            await _writeRepository.UpdateAsync(request, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Reopen rejected by domain invariants.");
            return Result<RequestDto>.Failure(
                Error.Validation(exception.Message, "requests.reopen.domain.invariant"));
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Reopen rejected by domain invariants.");
            return Result<RequestDto>.Failure(
                Error.Validation(exception.Message, "requests.reopen.domain.invariant"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Reopen failed. RequestId={RequestId}", command.RequestId);
            return Result<RequestDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while reopening the request.",
                    "requests.reopen.failed"));
        }

        try
        {
            await _notificationSender.SendAsync(
                RequestNotificationFactory.Reopened(request, request.Requester, comment),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Reopen succeeded but notification failed. RequestId={RequestId}",
                request.Id);
        }

        var detail = await _readRepository
            .GetDetailAsync(command.RequestId, actorId, actor.Role, cancellationToken)
            .ConfigureAwait(false);

        if (detail is null)
        {
            return Result<RequestDto>.Failure(RequestNotFoundError);
        }

        _logger.LogInformation(
            "Request reopened. RequestId={RequestId} TargetStatus={TargetStatus}",
            request.Id,
            command.TargetStatus);

        await _auditLogger
            .LogRequestReopenedAsync(request, actor, cancellationToken)
            .ConfigureAwait(false);

        return Result<RequestDto>.Success(new RequestDto(
            detail.Id,
            detail.Code,
            detail.Title,
            detail.Description,
            detail.Priority,
            detail.Status,
            detail.CreatedAt,
            detail.DueDate,
            detail.EvidenceUrl,
            detail.ClosedAt,
            detail.Area.Id,
            detail.Area.Name,
            detail.RequestType.Id,
            detail.RequestType.Name,
            detail.Requester.Id,
            detail.Requester.Username,
            detail.Requester.Email,
            detail.Responsible?.Id,
            detail.Responsible?.Username,
            detail.Responsible?.Email));
    }
}