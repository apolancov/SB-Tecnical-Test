using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Common.Authorization;
using Api.Authentication;
using Application.Authentication.AuthenticateUser;
using Application.Common.Audit;
using Application.Common.Results;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthenticateUserHandler _handler;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthenticateUserHandler handler,
        IAuditLogger auditLogger,
        ILogger<AuthController> logger)
    {
        _handler = handler;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "Request body is required.",
                type: "authentication.request.missing");
        }

        var command = new AuthenticateUserCommand(
            Username: request.Username ?? string.Empty,
            Password: request.Password ?? string.Empty);

        var result = await _handler.HandleAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            await _auditLogger.LogAsync(
                new AuditLogContext(
                    Action: AuditAction.LoginSucceeded,
                    Outcome: AuditOutcome.Success,
                    EntityType: "User",
                    EntityId: result.Value.User.Id.ToString(),
                    Details: $"Login successful for user '{result.Value.User.Username}'.",
                    ActorUserId: result.Value.User.Id,
                    ActorUserName: result.Value.User.Username),
                cancellationToken).ConfigureAwait(false);

            return Ok(result.Value);
        }

        var error = result.Error!;

        _logger.LogWarning(
            "Authentication request rejected. Kind={ErrorKind} Code={ErrorCode}",
            error.Kind,
            error.Code);

        await _auditLogger.LogAsync(
            new AuditLogContext(
                Action: AuditAction.LoginFailed,
                Outcome: AuditOutcome.Failure,
                EntityType: "User",
                EntityId: null,
                Details: $"Login failed for username '{command.Username}'. Code={error.Code}.",
                ActorUserId: Guid.Empty,
                ActorUserName: command.Username),
            cancellationToken).ConfigureAwait(false);

        return error.Kind switch
        {
            ErrorKind.Validation => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid credentials format.",
                detail: error.Message,
                type: error.Code),
            ErrorKind.Unauthorized => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication failed.",
                detail: error.Message,
                type: error.Code),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred.",
                detail: error.Message,
                type: error.Code)
        };
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var user = ResolveCurrentUser();

        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(user);
    }

    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [HttpGet("admin/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminHealth()
    {
        return Ok(new
        {
            status = "ok",
            policy = AuthorizationPolicyNames.AdminOnly,
            checkedAt = DateTime.UtcNow
        });
    }

    private AuthenticatedUserDto? ResolveCurrentUser()
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (subject is null || !Guid.TryParse(subject, out var userId))
        {
            return null;
        }

        var username = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return null;
        }

        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return new AuthenticatedUserDto(userId, username, email, role);
    }
}
