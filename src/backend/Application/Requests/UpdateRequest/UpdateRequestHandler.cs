using Application.Common.Audit;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.Authorization;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Requests.UpdateRequest;

public sealed class UpdateRequestHandler
{
    private const int TitleMaximumLength = Request.TitleMaximumLength;
    private const int DescriptionMaximumLength = Request.DescriptionMaximumLength;
    private const int EvidenceUrlMaximumLength = Request.EvidenceUrlMaximumLength;

    private static readonly Error RequestNotFoundError = Error.NotFound(
        "Request not found.",
        "requests.update.not_found");

    private static readonly Error TitleRequiredError = Error.Validation(
        "Title is required.",
        "requests.update.title.required");

    private static readonly Error TitleTooLongError = Error.Validation(
        $"Title cannot exceed {TitleMaximumLength} characters.",
        "requests.update.title.too_long");

    private static readonly Error DescriptionTooLongError = Error.Validation(
        $"Description cannot exceed {DescriptionMaximumLength} characters.",
        "requests.update.description.too_long");

    private static readonly Error EvidenceUrlTooLongError = Error.Validation(
        $"EvidenceUrl cannot exceed {EvidenceUrlMaximumLength} characters.",
        "requests.update.evidence_url.too_long");

    private static readonly Error EvidenceUrlInvalidError = Error.Validation(
        "EvidenceUrl is not a valid http(s) URL.",
        "requests.update.evidence_url.invalid");

    private static readonly Error InvalidPriorityError = Error.Validation(
        "Priority is not a valid value.",
        "requests.update.priority.invalid");

    private static readonly Error DueDateInPastError = Error.Validation(
        "Due date cannot be earlier than the request's creation date.",
        "requests.update.due_date.past");

    private static readonly Error UnauthorizedError = Error.Unauthorized(
        "Authenticated user could not be resolved.",
        "requests.update.user.unresolved");

    private static readonly Error UserNotFoundError = Error.Unauthorized(
        "Authenticated user does not exist in the system.",
        "requests.update.user.not_found");

    private static readonly Error InactiveUserError = Error.Forbidden(
        "Inactive users cannot update requests.",
        "requests.update.user.inactive");

    private static readonly Error ForbiddenError = Error.Forbidden(
        "Only Admin and Analista users (or the original requester on a Submitted request) can update a request.",
        "requests.update.forbidden");

    private static readonly Error ImmutableStatusError = Error.Conflict(
        "Closed or cancelled requests cannot be modified.",
        "requests.update.status.conflict");

    private readonly IRequestReadRepository _readRepository;
    private readonly IRequestWriteRepository _writeRepository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly Application.Common.Users.IUserLookupRepository _userLookup;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<UpdateRequestHandler> _logger;

    public UpdateRequestHandler(
        IRequestReadRepository readRepository,
        IRequestWriteRepository writeRepository,
        ICurrentUserAccessor currentUserAccessor,
        Application.Common.Users.IUserLookupRepository userLookup,
        IAuditLogger auditLogger,
        ILogger<UpdateRequestHandler> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _currentUserAccessor = currentUserAccessor;
        _userLookup = userLookup;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Result<RequestDto>> HandleAsync(
        UpdateRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (command.RequestId == Guid.Empty)
        {
            return Result<RequestDto>.Failure(RequestNotFoundError);
        }

        var validation = Validate(command);
        if (validation is not null)
        {
            return Result<RequestDto>.Failure(validation);
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

        if (!RequestAuthorization.CanEditRequest(request, actorId, actor.Role))
        {
            return Result<RequestDto>.Failure(ForbiddenError);
        }

        if (request.Status == RequestStatus.Closed || request.Status == RequestStatus.Cancelled)
        {
            return Result<RequestDto>.Failure(ImmutableStatusError);
        }

        string? normalizedEvidenceUrl = null;
        if (!string.IsNullOrWhiteSpace(command.EvidenceUrl))
        {
            var trimmed = command.EvidenceUrl.Trim();
            if (trimmed.Length > EvidenceUrlMaximumLength)
            {
                return Result<RequestDto>.Failure(EvidenceUrlTooLongError);
            }

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return Result<RequestDto>.Failure(EvidenceUrlInvalidError);
            }

            normalizedEvidenceUrl = uri.ToString();
        }

        var dueDate = command.DueDate?.ToUniversalTime();
        if (dueDate.HasValue && dueDate.Value < request.CreatedAt)
        {
            return Result<RequestDto>.Failure(DueDateInPastError);
        }

        try
        {
            request.UpdateDetails(
                title: command.Title.Trim(),
                description: command.Description ?? string.Empty,
                priority: command.Priority,
                dueDate: dueDate,
                evidenceUrl: normalizedEvidenceUrl,
                updateDate: DateTime.UtcNow,
                updatedBy: actor);
            await _writeRepository.UpdateAsync(request, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Update rejected by domain invariants.");
            return Result<RequestDto>.Failure(
                Error.Validation(exception.Message, "requests.update.domain.invariant"));
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Update rejected by domain invariants.");
            return Result<RequestDto>.Failure(
                Error.Validation(exception.Message, "requests.update.domain.invariant"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Update failed. RequestId={RequestId}", command.RequestId);
            return Result<RequestDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while updating the request.",
                    "requests.update.failed"));
        }

        var detail = await _readRepository
            .GetDetailAsync(command.RequestId, actorId, actor.Role, cancellationToken)
            .ConfigureAwait(false);

        if (detail is null)
        {
            return Result<RequestDto>.Failure(RequestNotFoundError);
        }

        _logger.LogInformation(
            "Request updated. RequestId={RequestId} UpdatedById={UpdatedById}",
            request.Id,
            actor.Id);

        await _auditLogger
            .LogRequestUpdatedAsync(request, actor, cancellationToken)
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

    private static Error? Validate(UpdateRequestCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return TitleRequiredError;
        }

        if (command.Title.Trim().Length > TitleMaximumLength)
        {
            return TitleTooLongError;
        }

        var description = command.Description ?? string.Empty;
        if (description.Trim().Length > DescriptionMaximumLength)
        {
            return DescriptionTooLongError;
        }

        if (!Enum.IsDefined(typeof(RequestPriority), command.Priority))
        {
            return InvalidPriorityError;
        }

        return null;
    }
}