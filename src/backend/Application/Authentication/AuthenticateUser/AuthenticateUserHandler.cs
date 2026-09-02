using Application.Common.Authentication;
using Application.Common.Persistence;
using Application.Common.Results;
using Microsoft.Extensions.Logging;

namespace Application.Authentication.AuthenticateUser;

public sealed class AuthenticateUserHandler
{
    internal const string GenericFailureMessage =
        "Invalid username or password.";

    private static readonly Error AuthenticationFailedError = Error.Unauthorized(
        GenericFailureMessage,
        "authentication.credentials.invalid");

    private static readonly Error InactiveUserError = Error.Unauthorized(
        GenericFailureMessage,
        "authentication.account.disabled");

    private static readonly Error ValidationError = Error.Validation(
        "Username and password are required.",
        "authentication.credentials.required");

    private readonly IUserReadRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenIssuer _tokenIssuer;
    private readonly ILogger<AuthenticateUserHandler> _logger;

    public AuthenticateUserHandler(
        IUserReadRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenIssuer tokenIssuer,
        ILogger<AuthenticateUserHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _logger = logger;
    }

    public async Task<Result<AuthenticationResponse>> HandleAsync(
        AuthenticateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrEmpty(command.Password))
        {
            _logger.LogWarning(
                "Rejected authentication attempt because required credentials were missing.");
            return Result<AuthenticationResponse>.Failure(ValidationError);
        }

        var username = command.Username.Trim();
        var user = await _userRepository
            .FindByUsernameAsync(username, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.LogWarning(
                "Authentication failed for unknown user. UsernameLength={UsernameLength}",
                username.Length);
            return Result<AuthenticationResponse>.Failure(AuthenticationFailedError);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning(
                "Authentication rejected for disabled account. UserId={UserId}",
                user.Id);
            return Result<AuthenticationResponse>.Failure(InactiveUserError);
        }

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            _logger.LogWarning(
                "Authentication failed because password verification did not match. UserId={UserId}",
                user.Id);
            return Result<AuthenticationResponse>.Failure(AuthenticationFailedError);
        }

        var issued = _tokenIssuer.IssueFor(user);

        _logger.LogInformation(
            "Authentication succeeded. UserId={UserId} Role={Role}",
            user.Id,
            user.Role);

        var response = new AuthenticationResponse(
            AccessToken: issued.AccessToken,
            TokenType: issued.TokenType,
            ExpiresAt: issued.ExpiresAtUtc,
            User: new AuthenticatedUserDto(user.Id, user.Username, user.Email, user.Role));

        return Result<AuthenticationResponse>.Success(response);
    }
}
