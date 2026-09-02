using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Institutions;
using Application.Common.Audit;
using Application.Common.Pagination;
using Application.Common.Results;
using Application.Institutions.CreateInstitution;
using Application.Institutions.DeleteInstitution;
using Application.Institutions.GetInstitutionById;
using Application.Institutions.GetInstitutionFilterOptions;
using Application.Institutions.GetInstitutions;
using Application.Institutions.UpdateInstitution;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/institutions")]
public sealed class InstitutionsController : ControllerBase
{
    private readonly GetInstitutionsQueryHandler _handler;
    private readonly GetInstitutionFilterOptionsQueryHandler _filterOptionsHandler;
    private readonly GetInstitutionByIdHandler _getByIdHandler;
    private readonly CreateInstitutionHandler _createHandler;
    private readonly UpdateInstitutionHandler _updateHandler;
    private readonly DeleteInstitutionHandler _deleteHandler;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<InstitutionsController> _logger;

    public InstitutionsController(
        GetInstitutionsQueryHandler handler,
        GetInstitutionFilterOptionsQueryHandler filterOptionsHandler,
        GetInstitutionByIdHandler getByIdHandler,
        CreateInstitutionHandler createHandler,
        UpdateInstitutionHandler updateHandler,
        DeleteInstitutionHandler deleteHandler,
        IAuditLogger auditLogger,
        ILogger<InstitutionsController> logger)
    {
        _handler = handler;
        _filterOptionsHandler = filterOptionsHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResult<InstitutionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInstitutions(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? name,
        [FromQuery] string? category,
        [FromQuery] string? statePower,
        [FromQuery] string? sector,
        CancellationToken cancellationToken)
    {
        var query = new GetInstitutionsQuery(
            Page: page ?? PaginationDefaults.DefaultPage,
            PageSize: pageSize ?? PaginationDefaults.DefaultPageSize,
            Name: name,
            Category: category,
            StatePower: statePower,
            Sector: sector);

        var result = await _handler.HandleAsync(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var error = result.Error!;
        _logger.LogWarning(
            "Institution query rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return error.Kind switch
        {
            ErrorKind.Validation => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request parameters.",
                detail: error.Message,
                type: error.Code),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred.",
                detail: error.Message,
                type: error.Code)
        };
    }

    [HttpGet("filter-options")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InstitutionFilterOptionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken)
    {
        var result = await _filterOptionsHandler.HandleAsync(
            new GetInstitutionFilterOptionsQuery(),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var error = result.Error!;
        _logger.LogWarning(
            "Institution filter options query rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred.",
            detail: error.Message,
            type: error.Code);
    }

    [HttpGet("{id:guid}", Name = nameof(GetInstitutionById))]
    [ProducesResponseType(typeof(InstitutionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInstitutionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid institution identifier.",
                detail: "Institution id must be a non-empty Guid.",
                type: "institutions.read.id.invalid");
        }

        var result = await _getByIdHandler.HandleAsync(
            new GetInstitutionByIdQuery(id),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var error = result.Error!;
        _logger.LogWarning(
            "Institution read rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return MapErrorToResponse(error, "An unexpected error occurred while reading the institution.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(InstitutionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateInstitution(
        [FromBody] CreateInstitutionRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "Request body is required.",
                type: "institutions.create.body.missing");
        }

        var command = new CreateInstitutionCommand(
            Name: request.Name ?? string.Empty,
            Category: request.Category ?? string.Empty,
            StatePower: request.StatePower ?? string.Empty,
            Sector: request.Sector ?? string.Empty);

        var result = await _createHandler.HandleAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            var institution = result.Value;
            await _auditLogger.LogAsync(
                BuildAuditContext(
                    AuditAction.InstitutionCreated,
                    institution.Id.ToString(),
                    $"Institution '{institution.Name}' created via API."),
                cancellationToken);

            return CreatedAtAction(
                actionName: nameof(GetInstitutionById),
                routeValues: new { id = institution.Id },
                value: institution);
        }

        var error = result.Error!;
        _logger.LogWarning(
            "Institution creation rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return MapErrorToResponse(error, "An unexpected error occurred while creating the institution.");
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(InstitutionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateInstitution(
        Guid id,
        [FromBody] UpdateInstitutionRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid institution identifier.",
                detail: "Institution id must be a non-empty Guid.",
                type: "institutions.update.id.invalid");
        }

        if (request is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "Request body is required.",
                type: "institutions.update.body.missing");
        }

        var command = new UpdateInstitutionCommand(
            Id: id,
            Name: request.Name ?? string.Empty,
            Category: request.Category ?? string.Empty,
            StatePower: request.StatePower ?? string.Empty,
            Sector: request.Sector ?? string.Empty);

        var result = await _updateHandler.HandleAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            var updated = result.Value;
            await _auditLogger.LogAsync(
                BuildAuditContext(
                    AuditAction.InstitutionUpdated,
                    updated.Id.ToString(),
                    $"Institution '{updated.Name}' updated via API."),
                cancellationToken);

            return Ok(updated);
        }

        var error = result.Error!;
        _logger.LogWarning(
            "Institution update rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return MapErrorToResponse(error, "An unexpected error occurred while updating the institution.");
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteInstitution(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid institution identifier.",
                detail: "Institution id must be a non-empty Guid.",
                type: "institutions.delete.id.invalid");
        }

        var command = new DeleteInstitutionCommand(id);
        var result = await _deleteHandler.HandleAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            await _auditLogger.LogAsync(
                BuildAuditContext(
                    AuditAction.InstitutionDeleted,
                    id.ToString(),
                    $"Institution '{id}' deleted via API."),
                cancellationToken);

            return NoContent();
        }

        var error = result.Error!;
        _logger.LogWarning(
            "Institution deletion rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return MapErrorToResponse(error, "An unexpected error occurred while deleting the institution.");
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
            EntityType: "Institution",
            EntityId: entityId,
            Details: details,
            ActorUserId: actorId == Guid.Empty ? null : actorId,
            ActorUserName: string.IsNullOrWhiteSpace(username) ? null : username);
    }

    private IActionResult MapErrorToResponse(Error error, string unexpectedTitle)
    {
        return error.Kind switch
        {
            ErrorKind.Validation => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid institution payload.",
                detail: error.Message,
                type: error.Code),
            ErrorKind.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Institution not found.",
                detail: error.Message,
                type: error.Code),
            ErrorKind.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Institution conflict.",
                detail: error.Message,
                type: error.Code),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: unexpectedTitle,
                detail: error.Message,
                type: error.Code)
        };
    }
}
