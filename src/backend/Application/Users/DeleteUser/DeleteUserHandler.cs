using Application.Common.Results;
using Microsoft.Extensions.Logging;

namespace Application.Users.DeleteUser;

public sealed class DeleteUserHandler
{
    private static readonly Error NotFoundError = Error.NotFound(
        "User not found.",
        "users.delete.not_found");

    private static readonly Error AlreadyInactiveError = Error.Conflict(
        "User is already inactive.",
        "users.delete.already_inactive");

    private readonly IUserAdministrationReadRepository _readRepository;
    private readonly IUserAdministrationWriteRepository _writeRepository;
    private readonly ILogger<DeleteUserHandler> _logger;

    public DeleteUserHandler(
        IUserAdministrationReadRepository readRepository,
        IUserAdministrationWriteRepository writeRepository,
        ILogger<DeleteUserHandler> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(
        DeleteUserCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Id == Guid.Empty)
        {
            _logger.LogWarning(
                "Rejected user deactivation because the identifier was empty.");
            return Result.Failure(NotFoundError);
        }

        var user = await _readRepository
            .FindByIdAsync(command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.LogWarning(
                "User deactivation rejected because the user was not found. UserId={UserId}",
                command.Id);
            return Result.Failure(NotFoundError);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning(
                "User deactivation rejected because the user is already inactive. UserId={UserId}",
                command.Id);
            return Result.Failure(AlreadyInactiveError);
        }

        user.Deactivate();

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
                "User deactivation failed. UserId={UserId}",
                user.Id);
            return Result.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while deactivating the user.",
                    "users.delete.failed"));
        }

        _logger.LogInformation(
            "User deactivated. UserId={UserId} Username={Username}",
            user.Id,
            user.Username);

        return Result.Success();
    }
}
