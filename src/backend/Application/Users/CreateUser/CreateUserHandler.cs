using Application.Common.Authentication;
using Application.Common.Results;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Users.CreateUser;

public sealed class CreateUserHandler
{
    private const int PasswordMinimumLength = 8;

    private static readonly Error UsernameRequiredError = Error.Validation(
        "Username is required.",
        "users.create.username.required");

    private static readonly Error UsernameTooLongError = Error.Validation(
        $"Username cannot exceed {User.UsernameMaximumLength} characters.",
        "users.create.username.too_long");

    private static readonly Error EmailRequiredError = Error.Validation(
        "Email is required.",
        "users.create.email.required");

    private static readonly Error EmailTooLongError = Error.Validation(
        $"Email cannot exceed {User.EmailMaximumLength} characters.",
        "users.create.email.too_long");

    private static readonly Error EmailInvalidError = Error.Validation(
        "Email is not in a valid format.",
        "users.create.email.invalid");

    private static readonly Error PasswordRequiredError = Error.Validation(
        "Password is required.",
        "users.create.password.required");

    private static readonly Error PasswordTooShortError = Error.Validation(
        $"Password must contain at least {PasswordMinimumLength} characters.",
        "users.create.password.too_short");

    private static readonly Error DuplicateUsernameError = Error.Conflict(
        "A user with the same username already exists.",
        "users.create.username.conflict");

    private static readonly Error DuplicateEmailError = Error.Conflict(
        "A user with the same email already exists.",
        "users.create.email.conflict");

    private readonly IUserAdministrationReadRepository _readRepository;
    private readonly IUserAdministrationWriteRepository _writeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(
        IUserAdministrationReadRepository readRepository,
        IUserAdministrationWriteRepository writeRepository,
        IPasswordHasher passwordHasher,
        ILogger<CreateUserHandler> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<UserDto>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var validation = Validate(command);
        if (validation is not null)
        {
            _logger.LogWarning(
                "Rejected user creation. Kind=Validation Code={ErrorCode} Message={ErrorMessage}",
                validation.Code,
                validation.Message);
            return Result<UserDto>.Failure(validation);
        }

        var username = command.Username.Trim();
        var email = command.Email.Trim();

        if (await _readRepository
            .UsernameExistsAsync(username, excludingUserId: null, cancellationToken)
            .ConfigureAwait(false))
        {
            _logger.LogWarning(
                "User creation rejected because the username is already registered. Username={Username}",
                username);
            return Result<UserDto>.Failure(DuplicateUsernameError);
        }

        if (await _readRepository
            .EmailExistsAsync(email, excludingUserId: null, cancellationToken)
            .ConfigureAwait(false))
        {
            _logger.LogWarning(
                "User creation rejected because the email is already registered. Email={Email}",
                email);
            return Result<UserDto>.Failure(DuplicateEmailError);
        }

        var passwordHash = _passwordHasher.Hash(command.Password);
        var user = new User(username, email, passwordHash, command.Role, command.IsActive);

        try
        {
            await _writeRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        }
        catch (DuplicateUserException exception)
        {
            _logger.LogWarning(
                exception,
                "User creation rejected because of a duplicate {Field}. Value={Value}",
                exception.Field,
                exception.Value);
            return exception.Field switch
            {
                "username" => Result<UserDto>.Failure(DuplicateUsernameError),
                "email" => Result<UserDto>.Failure(DuplicateEmailError),
                _ => Result<UserDto>.Failure(Error.Conflict(
                    exception.Message,
                    "users.create.conflict")),
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
                "User creation failed. Username={Username}",
                user.Username);
            return Result<UserDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while creating the user.",
                    "users.create.failed"));
        }

        _logger.LogInformation(
            "User created. UserId={UserId} Username={Username} Role={Role}",
            user.Id,
            user.Username,
            user.Role);

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAt));
    }

    private static Error? Validate(CreateUserCommand command)
    {
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

        if (string.IsNullOrEmpty(command.Password))
        {
            return PasswordRequiredError;
        }

        if (command.Password.Length < PasswordMinimumLength)
        {
            return PasswordTooShortError;
        }

        return null;
    }
}
