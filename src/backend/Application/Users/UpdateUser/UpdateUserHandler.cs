using Application.Common.Results;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Users.UpdateUser;

public sealed class UpdateUserHandler
{
    private static readonly Error UsernameRequiredError = Error.Validation(
        "Username is required.",
        "users.update.username.required");

    private static readonly Error UsernameTooLongError = Error.Validation(
        $"Username cannot exceed {User.UsernameMaximumLength} characters.",
        "users.update.username.too_long");

    private static readonly Error EmailRequiredError = Error.Validation(
        "Email is required.",
        "users.update.email.required");

    private static readonly Error EmailTooLongError = Error.Validation(
        $"Email cannot exceed {User.EmailMaximumLength} characters.",
        "users.update.email.too_long");

    private static readonly Error EmailInvalidError = Error.Validation(
        "Email is not in a valid format.",
        "users.update.email.invalid");

    private static readonly Error NotFoundError = Error.NotFound(
        "User not found.",
        "users.update.not_found");

    private static readonly Error DuplicateUsernameError = Error.Conflict(
        "A user with the same username already exists.",
        "users.update.username.conflict");

    private static readonly Error DuplicateEmailError = Error.Conflict(
        "A user with the same email already exists.",
        "users.update.email.conflict");

    private readonly IUserAdministrationReadRepository _readRepository;
    private readonly IUserAdministrationWriteRepository _writeRepository;
    private readonly ILogger<UpdateUserHandler> _logger;

    public UpdateUserHandler(
        IUserAdministrationReadRepository readRepository,
        IUserAdministrationWriteRepository writeRepository,
        ILogger<UpdateUserHandler> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _logger = logger;
    }

    public async Task<Result<UserDto>> HandleAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        var validation = Validate(command);
        if (validation is not null)
        {
            _logger.LogWarning(
                "Rejected user update. Kind=Validation Code={ErrorCode} Message={ErrorMessage}",
                validation.Code,
                validation.Message);
            return Result<UserDto>.Failure(validation);
        }

        var user = await _readRepository
            .FindByIdAsync(command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.LogWarning(
                "User update rejected because the user was not found. UserId={UserId}",
                command.Id);
            return Result<UserDto>.Failure(NotFoundError);
        }

        var username = command.Username.Trim();
        var email = command.Email.Trim();

        if (!string.Equals(user.Username, username, StringComparison.Ordinal)
            && await _readRepository
                .UsernameExistsAsync(username, excludingUserId: user.Id, cancellationToken)
                .ConfigureAwait(false))
        {
            _logger.LogWarning(
                "User update rejected because the username is already registered. UserId={UserId} Username={Username}",
                command.Id,
                username);
            return Result<UserDto>.Failure(DuplicateUsernameError);
        }

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)
            && await _readRepository
                .EmailExistsAsync(email, excludingUserId: user.Id, cancellationToken)
                .ConfigureAwait(false))
        {
            _logger.LogWarning(
                "User update rejected because the email is already registered. UserId={UserId} Email={Email}",
                command.Id,
                email);
            return Result<UserDto>.Failure(DuplicateEmailError);
        }

        user.Update(username, email, command.Role, command.IsActive);

        try
        {
            _writeRepository.Update(user);
        }
        catch (DuplicateUserException exception)
        {
            _logger.LogWarning(
                exception,
                "User update rejected because of a duplicate {Field}. Value={Value}",
                exception.Field,
                exception.Value);
            return exception.Field switch
            {
                "username" => Result<UserDto>.Failure(DuplicateUsernameError),
                "email" => Result<UserDto>.Failure(DuplicateEmailError),
                _ => Result<UserDto>.Failure(Error.Conflict(
                    exception.Message,
                    "users.update.conflict")),
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "User update failed. UserId={UserId}",
                user.Id);
            return Result<UserDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while updating the user.",
                    "users.update.failed"));
        }

        _logger.LogInformation(
            "User updated. UserId={UserId} Username={Username} Role={Role} IsActive={IsActive}",
            user.Id,
            user.Username,
            user.Role,
            user.IsActive);

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAt));
    }

    private static Error? Validate(UpdateUserCommand command)
    {
        if (command.Id == Guid.Empty)
        {
            return NotFoundError;
        }

        if (string.IsNullOrWhiteSpace(command.Username))
        {
            return UsernameRequiredError;
        }

        var username = command.Username.Trim();
        if (username.Length > User.UsernameMaximumLength)
        {
            return UsernameTooLongError;
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return EmailRequiredError;
        }

        var email = command.Email.Trim();
        if (email.Length > User.EmailMaximumLength)
        {
            return EmailTooLongError;
        }

        if (!User.IsSupportedEmail(email))
        {
            return EmailInvalidError;
        }

        return null;
    }
}
