using Application.Common.Results;
using Microsoft.Extensions.Logging;

namespace Application.Users.GetUserById;

public sealed class GetUserByIdHandler
{
    private static readonly Error NotFoundError = Error.NotFound(
        "User not found.",
        "users.read.not_found");

    private readonly IUserAdministrationReadRepository _repository;
    private readonly ILogger<GetUserByIdHandler> _logger;

    public GetUserByIdHandler(
        IUserAdministrationReadRepository repository,
        ILogger<GetUserByIdHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<UserDto>> HandleAsync(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Id == Guid.Empty)
        {
            _logger.LogWarning(
                "Rejected user read because the identifier was empty.");
            return Result<UserDto>.Failure(NotFoundError);
        }

        var user = await _repository
            .FindByIdAsync(query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.LogInformation(
                "User read returned no result. UserId={UserId}",
                query.Id);
            return Result<UserDto>.Failure(NotFoundError);
        }

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAt));
    }
}
