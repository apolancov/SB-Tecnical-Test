using Application.Common.Audit;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.Notifications;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Requests.CreateRequest;

public sealed class CreateRequestHandler
{
    private const int TitleMaximumLength = Request.TitleMaximumLength;
    private const int DescriptionMaximumLength = Request.DescriptionMaximumLength;
    private const int EvidenceUrlMaximumLength = Request.EvidenceUrlMaximumLength;

    private static readonly Error TitleRequiredError = Error.Validation(
        "Title is required.",
        "requests.create.title.required");

    private static readonly Error TitleTooLongError = Error.Validation(
        $"Title cannot exceed {TitleMaximumLength} characters.",
        "requests.create.title.too_long");

    private static readonly Error DescriptionTooLongError = Error.Validation(
        $"Description cannot exceed {DescriptionMaximumLength} characters.",
        "requests.create.description.too_long");

    private static readonly Error EvidenceUrlTooLongError = Error.Validation(
        $"EvidenceUrl cannot exceed {EvidenceUrlMaximumLength} characters.",
        "requests.create.evidence_url.too_long");

    private static readonly Error EvidenceUrlInvalidError = Error.Validation(
        "EvidenceUrl is not a valid http(s) URL.",
        "requests.create.evidence_url.invalid");

    private static readonly Error InvalidPriorityError = Error.Validation(
        "Priority is not a valid value.",
        "requests.create.priority.invalid");

    private static readonly Error InvalidAreaIdError = Error.Validation(
        "Area identifier is required.",
        "requests.create.area.invalid_id");

    private static readonly Error AreaNotFoundError = Error.Validation(
        "Area is not registered.",
        "requests.create.area.not_found");

    private static readonly Error InactiveAreaError = Error.Validation(
        "Area is inactive and cannot be assigned to new requests.",
        "requests.create.area.inactive");

    private static readonly Error InvalidRequestTypeIdError = Error.Validation(
        "Request type identifier is required.",
        "requests.create.request_type.invalid_id");

    private static readonly Error RequestTypeNotFoundError = Error.Validation(
        "Request type is not registered.",
        "requests.create.request_type.not_found");

    private static readonly Error InactiveRequestTypeError = Error.Validation(
        "Request type is inactive and cannot be assigned to new requests.",
        "requests.create.request_type.inactive");

    private static readonly Error DueDateInPastError = Error.Validation(
        "Due date cannot be in the past.",
        "requests.create.due_date.past");

    private static readonly Error UnauthorizedError = Error.Unauthorized(
        "Authenticated user could not be resolved.",
        "requests.create.user.unresolved");

    private static readonly Error InactiveRequesterError = Error.Validation(
        "Inactive users cannot create requests.",
        "requests.create.user.inactive");

    private static readonly Error RequesterNotFoundError = Error.Unauthorized(
        "Authenticated user does not exist in the system.",
        "requests.create.user.not_found");

    private readonly IRequestWriteRepository _writeRepository;
    private readonly IAreaReadRepository _areaRepository;
    private readonly IRequestTypeReadRepository _requestTypeRepository;
    private readonly IRequestCodeGenerator _codeGenerator;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly Application.Common.Users.IUserLookupRepository _userLookup;
    private readonly INotificationSender _notificationSender;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<CreateRequestHandler> _logger;

    public CreateRequestHandler(
        IRequestWriteRepository writeRepository,
        IAreaReadRepository areaRepository,
        IRequestTypeReadRepository requestTypeRepository,
        IRequestCodeGenerator codeGenerator,
        ICurrentUserAccessor currentUserAccessor,
        Application.Common.Users.IUserLookupRepository userLookup,
        INotificationSender notificationSender,
        IAuditLogger auditLogger,
        ILogger<CreateRequestHandler> logger)
    {
        _writeRepository = writeRepository;
        _areaRepository = areaRepository;
        _requestTypeRepository = requestTypeRepository;
        _codeGenerator = codeGenerator;
        _currentUserAccessor = currentUserAccessor;
        _userLookup = userLookup;
        _notificationSender = notificationSender;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Result<RequestDto>> HandleAsync(
        CreateRequestCommand command,
        CancellationToken cancellationToken)
    {
        var validation = Validate(command);
        if (validation is not null)
        {
            _logger.LogWarning(
                "Rejected request creation. Kind=Validation Code={ErrorCode} Message={ErrorMessage}",
                validation.Code,
                validation.Message);
            return Result<RequestDto>.Failure(validation);
        }

        var area = await _areaRepository
            .FindByIdAsync(command.AreaId, cancellationToken)
            .ConfigureAwait(false);

        if (area is null)
        {
            _logger.LogWarning(
                "Rejected request creation because the area is not registered. AreaId={AreaId}",
                command.AreaId);
            return Result<RequestDto>.Failure(AreaNotFoundError);
        }

        if (!area.IsActive)
        {
            _logger.LogWarning(
                "Rejected request creation because the area is inactive. AreaId={AreaId}",
                command.AreaId);
            return Result<RequestDto>.Failure(InactiveAreaError);
        }

        var requestType = await _requestTypeRepository
            .FindByIdAsync(command.RequestTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (requestType is null)
        {
            _logger.LogWarning(
                "Rejected request creation because the request type is not registered. RequestTypeId={RequestTypeId}",
                command.RequestTypeId);
            return Result<RequestDto>.Failure(RequestTypeNotFoundError);
        }

        if (!requestType.IsActive)
        {
            _logger.LogWarning(
                "Rejected request creation because the request type is inactive. RequestTypeId={RequestTypeId}",
                command.RequestTypeId);
            return Result<RequestDto>.Failure(InactiveRequestTypeError);
        }

        var requesterId = _currentUserAccessor.GetCurrentUserId();
        if (requesterId == Guid.Empty)
        {
            _logger.LogWarning(
                "Rejected request creation because the authenticated user could not be resolved.");
            return Result<RequestDto>.Failure(UnauthorizedError);
        }

        var requester = await _userLookup
            .FindByIdAsync(requesterId, cancellationToken)
            .ConfigureAwait(false);

        if (requester is null)
        {
            _logger.LogWarning(
                "Rejected request creation because the requester was not found. RequesterId={RequesterId}",
                requesterId);
            return Result<RequestDto>.Failure(RequesterNotFoundError);
        }

        if (!requester.IsActive)
        {
            _logger.LogWarning(
                "Rejected request creation because the requester is inactive. RequesterId={RequesterId}",
                requesterId);
            return Result<RequestDto>.Failure(InactiveRequesterError);
        }

        string code;
        try
        {
            code = await _codeGenerator
                .GenerateNextCodeAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Request code generation failed.");
            return Result<RequestDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while generating the request code.",
                    "requests.create.code.failed"));
        }

        var now = DateTime.UtcNow;
        var dueDate = command.DueDate?.ToUniversalTime();
        if (dueDate.HasValue && dueDate.Value < now)
        {
            _logger.LogWarning(
                "Rejected request creation because the due date is in the past. DueDate={DueDate}",
                dueDate);
            return Result<RequestDto>.Failure(DueDateInPastError);
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

        Request request;
        try
        {
            request = new Request(
                code: code,
                title: command.Title.Trim(),
                description: command.Description ?? string.Empty,
                priority: command.Priority,
                requester: requester,
                area: area,
                requestType: requestType,
                createdAt: now,
                dueDate: dueDate,
                evidenceUrl: normalizedEvidenceUrl);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Domain rejected request creation.");
            return Result<RequestDto>.Failure(
                Error.Validation(exception.Message, "requests.create.domain.invariant"));
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Domain rejected request creation.");
            return Result<RequestDto>.Failure(
                Error.Validation(exception.Message, "requests.create.domain.invariant"));
        }

        var notification = RequestNotificationFactory.Created(request, requester);

        try
        {
            await _writeRepository.AddAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Request creation failed. Code={Code}", code);
            return Result<RequestDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while creating the request.",
                    "requests.create.failed"));
        }

        try
        {
            await _notificationSender.SendAsync(notification, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Request created but the notification failed to persist. RequestId={RequestId}",
                request.Id);
        }

        _logger.LogInformation(
            "Request created. RequestId={RequestId} Code={Code} RequesterId={RequesterId}",
            request.Id,
            request.Code,
            requester.Id);

        await _auditLogger
            .LogRequestCreatedAsync(request, requester, cancellationToken)
            .ConfigureAwait(false);

        return Result<RequestDto>.Success(new RequestDto(
            request.Id,
            request.Code,
            request.Title,
            request.Description,
            request.Priority,
            request.Status,
            request.CreatedAt,
            request.DueDate,
            request.EvidenceUrl,
            request.ClosedAt,
            request.AreaId,
            area.Name,
            request.RequestTypeId,
            requestType.Name,
            request.RequesterId,
            requester.Username,
            requester.Email,
            request.ResponsibleId,
            request.Responsible?.Username,
            request.Responsible?.Email));
    }

    private static Error? Validate(CreateRequestCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return TitleRequiredError;
        }

        var title = command.Title.Trim();
        if (title.Length > TitleMaximumLength)
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

        if (command.AreaId == Guid.Empty)
        {
            return InvalidAreaIdError;
        }

        if (command.RequestTypeId == Guid.Empty)
        {
            return InvalidRequestTypeIdError;
        }

        return null;
    }
}