using Application.Common.Audit;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.Authorization;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Requests.AddComment;

public sealed class AddCommentHandler
{
    private const int TextMaximumLength = 4096;

    private static readonly Error RequestNotFoundError = Error.NotFound(
        "Request not found.",
        "requests.comment.not_found");

    private static readonly Error TextRequiredError = Error.Validation(
        "Comment text is required.",
        "requests.comment.text.required");

    private static readonly Error TextTooLongError = Error.Validation(
        $"Comment text cannot exceed {TextMaximumLength} characters.",
        "requests.comment.text.too_long");

    private static readonly Error InvalidVisibilityError = Error.Validation(
        "Visibility is not a valid value.",
        "requests.comment.visibility.invalid");

    private static readonly Error UnauthorizedError = Error.Unauthorized(
        "Authenticated user could not be resolved.",
        "requests.comment.user.unresolved");

    private static readonly Error InactiveUserError = Error.Forbidden(
        "Inactive users cannot add comments.",
        "requests.comment.user.inactive");

    private static readonly Error UserNotFoundError = Error.Unauthorized(
        "Authenticated user does not exist in the system.",
        "requests.comment.user.not_found");

    private static readonly Error ForbiddenError = Error.Forbidden(
        "You do not have permission to comment on this request.",
        "requests.comment.forbidden");

    private static readonly Error InternalVisibilityForbiddenError = Error.Forbidden(
        "Only Admin and Analista users can post internal comments.",
        "requests.comment.visibility.forbidden");

    private readonly IRequestReadRepository _readRepository;
    private readonly IRequestWriteRepository _writeRepository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly Application.Common.Users.IUserLookupRepository _userLookup;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<AddCommentHandler> _logger;

    public AddCommentHandler(
        IRequestReadRepository readRepository,
        IRequestWriteRepository writeRepository,
        ICurrentUserAccessor currentUserAccessor,
        Application.Common.Users.IUserLookupRepository userLookup,
        IAuditLogger auditLogger,
        ILogger<AddCommentHandler> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _currentUserAccessor = currentUserAccessor;
        _userLookup = userLookup;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Result<RequestCommentDto>> HandleAsync(
        AddCommentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.RequestId == Guid.Empty)
        {
            return Result<RequestCommentDto>.Failure(RequestNotFoundError);
        }

        if (string.IsNullOrWhiteSpace(command.Text))
        {
            return Result<RequestCommentDto>.Failure(TextRequiredError);
        }

        var normalized = command.Text.Trim();
        if (normalized.Length > TextMaximumLength)
        {
            return Result<RequestCommentDto>.Failure(TextTooLongError);
        }

        if (!Enum.IsDefined(typeof(CommentVisibility), command.Visibility))
        {
            return Result<RequestCommentDto>.Failure(InvalidVisibilityError);
        }

        var authorId = _currentUserAccessor.GetCurrentUserId();
        if (authorId == Guid.Empty)
        {
            return Result<RequestCommentDto>.Failure(UnauthorizedError);
        }

        var author = await _userLookup.FindByIdAsync(authorId, cancellationToken).ConfigureAwait(false);
        if (author is null)
        {
            return Result<RequestCommentDto>.Failure(UserNotFoundError);
        }

        if (!author.IsActive)
        {
            return Result<RequestCommentDto>.Failure(InactiveUserError);
        }

        if (!RequestAuthorization.CanAddComment(command.Visibility, authorId, author.Role))
        {
            return Result<RequestCommentDto>.Failure(InternalVisibilityForbiddenError);
        }

        var request = await _readRepository
            .FindEntityByIdAsync(command.RequestId, cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
        {
            _logger.LogWarning(
                "Rejected comment because the request was not found. RequestId={RequestId}",
                command.RequestId);
            return Result<RequestCommentDto>.Failure(RequestNotFoundError);
        }

        if (!RequestAuthorization.CanViewRequest(request, authorId, author.Role))
        {
            return Result<RequestCommentDto>.Failure(ForbiddenError);
        }

        try
        {
            var comment = request.AddComment(author, normalized, command.Visibility, DateTime.UtcNow);
            await _writeRepository.UpdateAsync(request, cancellationToken);

            _logger.LogInformation(
                "Comment added. RequestId={RequestId} CommentId={CommentId} AuthorId={AuthorId} Visibility={Visibility}",
                request.Id,
                comment.Id,
                author.Id,
                command.Visibility);

            await _auditLogger
                .LogRequestCommentAddedAsync(request, author, cancellationToken)
                .ConfigureAwait(false);

            return Result<RequestCommentDto>.Success(new RequestCommentDto(
                comment.Id,
                comment.Text,
                comment.Visibility,
                comment.Date,
                comment.AuthorId,
                comment.Author.Username));
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Comment rejected by domain invariants.");
            return Result<RequestCommentDto>.Failure(
                Error.Validation(exception.Message, "requests.comment.domain.invariant"));
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Comment rejected by domain invariants.");
            return Result<RequestCommentDto>.Failure(
                Error.Validation(exception.Message, "requests.comment.domain.invariant"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Comment creation failed. RequestId={RequestId}", command.RequestId);
            return Result<RequestCommentDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while adding the comment.",
                    "requests.comment.failed"));
        }
    }
}