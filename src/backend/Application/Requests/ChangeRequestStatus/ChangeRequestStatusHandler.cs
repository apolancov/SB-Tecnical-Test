using Application.Common.Audit;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.Authorization;
using Application.Requests.Notifications;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Requests.ChangeRequestStatus;

public sealed class ChangeRequestStatusHandler
{
    private const int CommentMaximumLength = Request.CommentMaximumLength;

    private static readonly Error RequestNotFoundError = Error.NotFound(
        "Request not found.",
        "requests.change_status.not_found");

    private static readonly Error InvalidStatusError = Error.Validation(
        "NewStatus is not a valid value.",
        "requests.change_status.status.invalid");

    private static readonly Error CommentTooLongError = Error.Validation(
        $"Comment cannot exceed {CommentMaximumLength} characters.",
        "requests.change_status.comment.too_long");

    private static readonly Error InvalidTransitionError = Error.Validation(
        "The requested status transition is not allowed from the current status.",
        "requests.change_status.transition.invalid");

    private static readonly Error UnauthorizedError = Error.Unauthorized(
        "Authenticated user could not be resolved.",
        "requests.change_status.user.unresolved");

    private static readonly Error InactiveUserError = Error.Forbidden(
        "Inactive users cannot change request status.",
        "requests.change_status.user.inactive");

    private static readonly Error UserNotFoundError = Error.Unauthorized(
        "Authenticated user does not exist in the system.",
        "requests.change_status.user.not_found");

    private static readonly Error ForbiddenError = Error.Forbidden(
        "Only Admin and Analista users can change request status.",
        "requests.change_status.forbidden");

    private static readonly Error ResourceForbiddenError = Error.Forbidden(
        "You do not have access to this request.",
        "requests.change_status.resource.forbidden");

    private readonly IRequestReadRepository _readRepository;
    private readonly IRequestWriteRepository _writeRepository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly Application.Common.Users.IUserLookupRepository _userLookup;
    private readonly INotificationSender _notificationSender;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<ChangeRequestStatusHandler> _logger;

    public ChangeRequestStatusHandler(
        IRequestReadRepository readRepository,
        IRequestWriteRepository writeRepository,
        ICurrentUserAccessor currentUserAccessor,
        Application.Common.Users.IUserLookupRepository userLookup,
        INotificationSender notificationSender,
        IAuditLogger auditLogger,
        ILogger<ChangeRequestStatusHandler> logger)
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
        ChangeRequestStatusCommand command,
        CancellationToken cancellationToken)
    {
        if (command.RequestId == Guid.Empty)
        {
            return Result<RequestDto>.Failure(RequestNotFoundError);
        }

        if (!Enum.IsDefined(typeof(RequestStatus), command.NewStatus))
        {
            _logger.LogWarning(
                "Rejected status change because the new status is invalid. NewStatus={NewStatus}",
                command.NewStatus);
            return Result<RequestDto>.Failure(InvalidStatusError);
        }

        var comment = command.Comment ?? string.Empty;
        if (comment.Trim().Length > CommentMaximumLength)
        {
            return Result<RequestDto>.Failure(CommentTooLongError);
        }

        var actorId = _currentUserAccessor.GetCurrentUserId();
        if (actorId == Guid.Empty)
        {
            return Result<RequestDto>.Failure(UnauthorizedError);
        }

        var actor = await _userLookup
            .FindByIdAsync(actorId, cancellationToken)
            .ConfigureAwait(false);

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
            _logger.LogWarning(
                "Rejected status change because the request was not found. RequestId={RequestId}",
                command.RequestId);
            return Result<RequestDto>.Failure(RequestNotFoundError);
        }

        if (!RequestAuthorization.CanChangeStatus(request, actorId, actor.Role))
        {
            return Result<RequestDto>.Failure(ForbiddenError);
        }

        var currentStatus = request.Status;
        if (!RequestStatusTransition.IsAllowed(currentStatus, command.NewStatus))
        {
            _logger.LogWarning(
                "Rejected status change because the transition is not allowed. "
                + "RequestId={RequestId} FromStatus={FromStatus} ToStatus={ToStatus}",
                command.RequestId,
                currentStatus,
                command.NewStatus);
            return Result<RequestDto>.Failure(InvalidTransitionError);
        }

        try
        {
            request.ChangeStatus(command.NewStatus, actor, comment, DateTime.UtcNow);
            await _writeRepository.UpdateAsync(request, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Status change rejected by domain invariants.");
            return Result<RequestDto>.Failure(
                Error.Validation(exception.Message, "requests.change_status.domain.invariant"));
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Status change rejected by domain invariants.");
            return Result<RequestDto>.Failure(
                Error.Validation(exception.Message, "requests.change_status.domain.invariant"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Status change failed. RequestId={RequestId}", command.RequestId);
            return Result<RequestDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while changing the request status.",
                    "requests.change_status.failed"));
        }

        await DispatchNotificationsAsync(
            request,
            currentStatus,
            command.NewStatus,
            comment,
            cancellationToken);

        var reloaded = await _readRepository
            .GetDetailAsync(command.RequestId, actorId, actor.Role, cancellationToken)
            .ConfigureAwait(false);

        if (reloaded is null)
        {
            return Result<RequestDto>.Failure(RequestNotFoundError);
        }

        _logger.LogInformation(
            "Request status changed. RequestId={RequestId} FromStatus={FromStatus} ToStatus={ToStatus}",
            command.RequestId,
            currentStatus,
            command.NewStatus);

        await _auditLogger
            .LogRequestStatusChangedAsync(
                request,
                currentStatus,
                command.NewStatus,
                actor,
                cancellationToken)
            .ConfigureAwait(false);

        return Result<RequestDto>.Success(new RequestDto(
            reloaded.Id,
            reloaded.Code,
            reloaded.Title,
            reloaded.Description,
            reloaded.Priority,
            reloaded.Status,
            reloaded.CreatedAt,
            reloaded.DueDate,
            reloaded.EvidenceUrl,
            reloaded.ClosedAt,
            reloaded.Area.Id,
            reloaded.Area.Name,
            reloaded.RequestType.Id,
            reloaded.RequestType.Name,
            reloaded.Requester.Id,
            reloaded.Requester.Username,
            reloaded.Requester.Email,
            reloaded.Responsible?.Id,
            reloaded.Responsible?.Username,
            reloaded.Responsible?.Email));
    }

    private async Task DispatchNotificationsAsync(
        Request request,
        RequestStatus previousStatus,
        RequestStatus newStatus,
        string comment,
        CancellationToken cancellationToken)
    {
        try
        {
            await _notificationSender.SendAsync(
                RequestNotificationFactory.StatusChanged(request, request.Requester, newStatus),
                cancellationToken);

            if (newStatus == RequestStatus.Closed)
            {
                await _notificationSender.SendAsync(
                    RequestNotificationFactory.Closed(request, request.Requester, comment),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Status change succeeded but notifications failed. RequestId={RequestId}",
                request.Id);
        }
    }
}