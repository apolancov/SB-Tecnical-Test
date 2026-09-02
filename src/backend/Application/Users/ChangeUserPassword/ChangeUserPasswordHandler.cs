using Application.Common.Authentication;
using Application.Common.Results;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Users.ChangeUserPassword;

public sealed class ChangeUserPasswordHandler
{
    private const int PasswordMinimumLength = 8;

    private static readonly Error NotFoundError = Error.NotFound(
        "User not found.",
        "users.password.not_found");

    private static readonly Error PasswordRequiredError = Error.Validation(
        "Password is required.",
        "users.password.required");

    private static readonly Error PasswordTooShortError = Error.Validation(
        $"Password must contain at least {PasswordMinimumLength} characters.",
        "users.password.too_short");

    private readonly IUserAdministrationReadRepository _readRepository;
    private readonly IUserAdministrationWriteRepository _writeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ChangeUserPasswordHandler> _logger;

    public ChangeUserPasswordHandler(
        IUserAdministrationReadRepository readRepository,
        IUserAdministrationWriteRepository writeRepository,
        IPasswordHasher passwordHasher,
        ILogger<ChangeUserPasswordHandler> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(
        ChangeUserPasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Id == Guid.Empty)
        {
            _logger.LogWarning(
                "Rejected password change because the identifier was empty.");
            return Result.Failure(NotFoundError);
        }

        if (string.IsNullOrEmpty(command.NewPassword))
        {
            _logger.LogWarning(
                "Rejected password change because the password was empty. UserId={UserId}",
                command.Id);
            return Result.Failure(PasswordRequiredError);
        }

        if (command.NewPassword.Length < PasswordMinimumLength)
        {
            _logger.LogWarning(
                "Rejected password change because the password is too short. UserId={UserId}",
                command.Id);
            return Result.Failure(PasswordTooShortError);
        }

        var user = await _readRepository
            .FindByIdAsync(command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.LogWarning(
                "Password change rejected because the user was not found. UserId={UserId}",
                command.Id);
            return Result.Failure(NotFoundError);
        }

        var newHash = _passwordHasher.Hash(command.NewPassword);
        user.ChangePasswordHash(newHash);

        try
        {
            _writeRepository.Update(user);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Password change failed. UserId={UserId}",
                user.Id);
            return Result.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while changing the user password.",
                    "users.password.failed"));
        }

        _logger.LogInformation(
            "User password changed. UserId={UserId} Username={Username}",
            user.Id,
            user.Username);

        return Result.Success();
    }
}
