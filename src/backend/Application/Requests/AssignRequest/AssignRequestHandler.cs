using Application.Common.Audit;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.Authorization;
using Application.Requests.Notifications;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Requests.AssignRequest;

public sealed class AssignRequestHandler
{
    private const int CommentMaximumLength = Request.CommentMaximumLength;

    private static readonly Error RequestNotFoundError = Error.NotFound(
        "Request not found.",
        "requests.assign.not_found");

    private static readonly Error ResponsibleNotFoundError = Error.NotFound(
        "Responsible user not found.",
        "requests.assign.responsible.not_found");

    private static readonly Error InactiveResponsibleError = Error.Validation(
        "The target user is inactive and cannot be assigned.",
        "requests.assign.responsible.inactive");

    private static readonly Error InvalidResponsibleIdError = Error.Validation(
        "Responsible user identifier is required.",
        "requests.assign.responsible.invalid_id");

    private static readonly Error CommentTooLongError = Error.Validation(
        $"Comment cannot exceed {CommentMaximumLength} characters.",
        "requests.assign.comment.too_long");

    private static readonly Error UnauthorizedError = Error.Unauthorized(
        "Authenticated user could not be resolved.",
        "requests.assign.user.unresolved");

    private static readonly Error InactiveActorError = Error.Forbidden(
        "Inactive users cannot assign requests.",
        "requests.assign.user.inactive");

    private static readonly Error ActorNotFoundError = Error.Unauthorized(
        "Authenticated user does not exist in the system.",
        "requests.assign.user.not_found");

    private static readonly Error ForbiddenError = Error.Forbidden(
        "Only Admin and Analista users can assign requests.",
        "requests.assign.forbidden");

    private readonly IRequestReadRepository _readRepository;
    private readonly IRequestWriteRepository _writeRepository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly Application.Common.Users.IUserLookupRepository _userLookup;
    private readonly INotificationSender _notificationSender;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<AssignRequestHandler> _logger;

    public AssignRequestHandler(
        IRequestReadRepository readRepository,
        IRequestWriteRepository writeRepository,
        ICurrentUserAccessor currentUserAccessor,
        Application.Common.Users.IUserLookupRepository userLookup,
        INotificationSender notificationSender,
        IAuditLogger auditLogger,
        ILogger<AssignRequestHandler> logger)
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
        AssignRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (command.RequestId == Guid.Empty)
        {
            return Result<RequestDto>.Failure(RequestNotFoundError);
        }

        if (command.ResponsibleUserId == Guid.Empty)
        {
            return Result<RequestDto>.Failure(InvalidResponsibleIdError);
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

        var actor = await _userLookup.FindByIdAsync(actorId, cancellationToken).ConfigureAwait(false);
        if (actor is null)
        {
            return Result<RequestDto>.Failure(ActorNotFoundError);
        }

        if (!actor.IsActive)
        {
            return Result<RequestDto>.Failure(InactiveActorError);
        }

        var responsible = await _userLookup
            .FindByIdAsync(command.ResponsibleUserId, cancellationToken)
            .ConfigureAwait(false);

        if (responsible is null)
        {
            _logger.LogWarning(
                "Rejected assignment because the responsible user was not found. ResponsibleUserId={ResponsibleUserId}",
                command.ResponsibleUserId);
            return Result<RequestDto>.Failure(ResponsibleNotFoundError);
        }

        if (!responsible.IsActive)
        {
            _logger.LogWarning(
                "Rejected assignment because the responsible user is inactive. ResponsibleUserId={ResponsibleUserId}",
                command.ResponsibleUserId);
            return Result<RequestDto>.Failure(InactiveResponsibleError);
        }

        var request = await _readRepository
            .FindEntityByIdAsync(command.RequestId, cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
        {
            _logger.LogWarning(
                "Rejected assignment because the request was not found. RequestId={RequestId}",
                command.RequestId);
            return Result<RequestDto>.Failure(RequestNotFoundError);
        }

        if (!RequestAuthorization.CanAssign(request, actorId, actor.Role))
        {
            return Result<RequestDto>.Failure(ForbiddenError);
        }

        var wasReassignment = request.ResponsibleId.HasValue
            && request.ResponsibleId.Value != responsible.Id;

        try
        {
            request.AssignResponsible(responsible, actor, DateTime.UtcNow, comment);
            await _writeRepository.UpdateAsync(request, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Assignment rejected by domain invariants.");
            return Result<RequestDto>.Failure(
                Error.Validation(exception.Message, "requests.assign.domain.invariant"));
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Assignment rejected by domain invariants.");
            return Result<RequestDto>.Failure(
                Error.Validation(exception.Message, "requests.assign.domain.invariant"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Assignment failed. RequestId={RequestId}", command.RequestId);
            return Result<RequestDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while assigning the request.",
                    "requests.assign.failed"));
        }

        try
        {
            var notification = wasReassignment
                ? RequestNotificationFactory.Reassigned(request, request.Requester, responsible)
                : RequestNotificationFactory.Assigned(request, request.Requester, responsible);
            await _notificationSender.SendAsync(notification, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Assignment succeeded but notification failed. RequestId={RequestId}",
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
            "Request assigned. RequestId={RequestId} ResponsibleUserId={ResponsibleUserId}",
            request.Id,
            responsible.Id);

        await _auditLogger
            .LogRequestAssignedAsync(request, responsible, actor, cancellationToken)
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