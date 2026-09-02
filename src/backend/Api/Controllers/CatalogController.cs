using Application.Common.Results;
using Application.Requests.Lookups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/catalogos")]
public sealed class CatalogController : ControllerBase
{
    private readonly ListActiveAreasHandler _areasHandler;
    private readonly ListActiveRequestTypesHandler _requestTypesHandler;
    private readonly ListStaffCandidatesHandler _staffHandler;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(
        ListActiveAreasHandler areasHandler,
        ListActiveRequestTypesHandler requestTypesHandler,
        ListStaffCandidatesHandler staffHandler,
        ILogger<CatalogController> logger)
    {
        _areasHandler = areasHandler;
        _requestTypesHandler = requestTypesHandler;
        _staffHandler = staffHandler;
        _logger = logger;
    }

    [HttpGet("areas")]
    [ProducesResponseType(typeof(IReadOnlyList<AreaLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListAreas(CancellationToken cancellationToken)
    {
        var result = await _areasHandler.HandleAsync(cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while reading areas.");
    }

    [HttpGet("tipos-solicitud")]
    [ProducesResponseType(typeof(IReadOnlyList<RequestTypeLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListRequestTypes(CancellationToken cancellationToken)
    {
        var result = await _requestTypesHandler.HandleAsync(cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while reading request types.");
    }

    [HttpGet("responsables")]
    [ProducesResponseType(typeof(IReadOnlyList<StaffCandidateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListStaffCandidates(CancellationToken cancellationToken)
    {
        var result = await _staffHandler.HandleAsync(cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while reading staff candidates.");
    }

    private IActionResult MapError(Error error, string unexpectedTitle)
    {
        _logger.LogWarning(
            "Catalog operation rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return error.Kind switch
        {
            ErrorKind.Validation => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid catalog request.",
                detail: error.Message,
                type: error.Code),
            ErrorKind.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Catalog not found.",
                detail: error.Message,
                type: error.Code),
            ErrorKind.Unauthorized => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication required.",
                detail: error.Message,
                type: error.Code),
            ErrorKind.Forbidden => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Operation not permitted.",
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