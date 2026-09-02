using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Common.Authorization;
using Api.Users;
using Application.Common.Audit;
using Application.Common.Pagination;
using Application.Common.Results;
using Application.Users;
using Application.Users.ChangeUserPassword;
using Application.Users.CreateUser;
using Application.Users.DeleteUser;
using Application.Users.GetUserById;
using Application.Users.GetUsers;
using Application.Users.UpdateUser;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly GetUsersQueryHandler _listHandler;
    private readonly GetUserByIdHandler _getByIdHandler;
    private readonly CreateUserHandler _createHandler;
    private readonly UpdateUserHandler _updateHandler;
    private readonly ChangeUserPasswordHandler _changePasswordHandler;
    private readonly DeleteUserHandler _deleteHandler;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        GetUsersQueryHandler listHandler,
        GetUserByIdHandler getByIdHandler,
        CreateUserHandler createHandler,
        UpdateUserHandler updateHandler,
        ChangeUserPasswordHandler changePasswordHandler,
        DeleteUserHandler deleteHandler,
        IAuditLogger auditLogger,
        ILogger<UsersController> logger)
    {
        _listHandler = listHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _changePasswordHandler = changePasswordHandler;
        _deleteHandler = deleteHandler;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? username,
        [FromQuery] string? email,
        [FromQuery] UserRole? role,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = new GetUsersQuery(
            Page: page ?? PaginationDefaults.DefaultPage,
            PageSize: pageSize ?? PaginationDefaults.DefaultPageSize,
            Username: username,
            Email: email,
            Role: role,
            IsActive: isActive);

        var result = await _listHandler.HandleAsync(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(
            result.Error!,
            "An unexpected error occurred while querying users.",
            "users query");
    }

    [HttpGet("{id:guid}", Name = nameof(GetUserById))]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(
            new GetUserByIdQuery(id),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(
            result.Error!,
            "An unexpected error occurred while reading the user.",
            "user read");
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "Request body is required.",
                type: "users.create.body.missing");
        }

        if (request.Role is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "Role is required.",
                type: "users.create.role.required");
        }

        var command = new CreateUserCommand(
            Username: request.Username ?? string.Empty,
            Email: request.Email ?? string.Empty,
            Password: request.Password ?? string.Empty,
            Role: request.Role.Value,
            IsActive: request.IsActive);

        var result = await _createHandler.HandleAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            var created = result.Value;
            await _auditLogger.LogAsync(
                BuildAuditContext(
                    AuditAction.UserCreated,
                    created.Id.ToString(),
                    $"User '{created.Username}' created via API."),
                cancellationToken);

            return CreatedAtAction(
                actionName: nameof(GetUserById),
                routeValues: new { id = created.Id },
                value: created);
        }

        var error = result.Error!;
        _logger.LogWarning(
            "User creation rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return MapError(
            error,
            "An unexpected error occurred while creating the user.",
            "user creation");
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid user identifier.",
                detail: "User id must be a non-empty Guid.",
                type: "users.update.id.invalid");
        }

        if (request is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "Request body is required.",
                type: "users.update.body.missing");
        }

        if (request.Role is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "Role is required.",
                type: "users.update.role.required");
        }

        var command = new UpdateUserCommand(
            Id: id,
            Username: request.Username ?? string.Empty,
            Email: request.Email ?? string.Empty,
            Role: request.Role.Value,
            IsActive: request.IsActive);

        var result = await _updateHandler.HandleAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            var updated = result.Value;
            await _auditLogger.LogAsync(
                BuildAuditContext(
                    AuditAction.UserUpdated,
                    updated.Id.ToString(),
                    $"User '{updated.Username}' updated via API."),
                cancellationToken);

            return Ok(updated);
        }

        var error = result.Error!;
        _logger.LogWarning(
            "User update rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return MapError(
            error,
            "An unexpected error occurred while updating the user.",
            "user update");
    }

    [HttpPut("{id:guid}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangeUserPassword(
        Guid id,
        [FromBody] ChangeUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid user identifier.",
                detail: "User id must be a non-empty Guid.",
                type: "users.password.id.invalid");
        }

        if (request is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "Request body is required.",
                type: "users.password.body.missing");
        }

        var command = new ChangeUserPasswordCommand(
            Id: id,
            NewPassword: request.NewPassword ?? string.Empty);

        var result = await _changePasswordHandler.HandleAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            await _auditLogger.LogAsync(
                BuildAuditContext(
                    AuditAction.UserPasswordChanged,
                    id.ToString(),
                    $"Password for user '{id}' changed via API."),
                cancellationToken);

            return NoContent();
        }

        var error = result.Error!;
        _logger.LogWarning(
            "User password change rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return MapError(
            error,
            "An unexpected error occurred while changing the user password.",
            "user password change");
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid user identifier.",
                detail: "User id must be a non-empty Guid.",
                type: "users.delete.id.invalid");
        }

        var result = await _deleteHandler.HandleAsync(
            new DeleteUserCommand(id),
            cancellationToken);

        if (result.IsSuccess)
        {
            await _auditLogger.LogAsync(
                BuildAuditContext(
                    AuditAction.UserDeactivated,
                    id.ToString(),
                    $"User '{id}' deactivated via API."),
                cancellationToken);

            return NoContent();
        }

        var error = result.Error!;
        _logger.LogWarning(
            "User deactivation rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return MapError(
            error,
            "An unexpected error occurred while deactivating the user.",
            "user deactivation");
    }

    private AuditLogContext BuildAuditContext(AuditAction action, string entityId, string details)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        Guid.TryParse(subject, out var actorId);

        var username = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name;

        return new AuditLogContext(
            Action: action,
            Outcome: AuditOutcome.Success,
            EntityType: "User",
            EntityId: entityId,
            Details: details,
            ActorUserId: actorId == Guid.Empty ? null : actorId,
            ActorUserName: string.IsNullOrWhiteSpace(username) ? null : username);
    }

    private IActionResult MapError(Error error, string unexpectedTitle, string operation)
    {
        _logger.LogWarning(
            "User operation rejected during {Operation}. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            operation,
            error.Kind,
            error.Code,
            error.Message);

        return error.Kind switch
        {
            ErrorKind.Validation => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid user request.",
                detail: error.Message,
                type: error.Code),
            ErrorKind.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "User not found.",
                detail: error.Message,
                type: error.Code),
            ErrorKind.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "User conflict.",
                detail: error.Message,
                type: error.Code),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: unexpectedTitle,
                detail: error.Message,
                type: error.Code),
        };
    }
}
