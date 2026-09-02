using Application.Common.Results;
using Application.Requests.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly GetDashboardSummaryHandler _summaryHandler;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        GetDashboardSummaryHandler summaryHandler,
        ILogger<DashboardController> logger)
    {
        _summaryHandler = summaryHandler;
        _logger = logger;
    }

    [HttpGet("resumen")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _summaryHandler.HandleAsync(cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while computing the dashboard summary.");
    }

    private IActionResult MapError(Error error, string unexpectedTitle)
    {
        _logger.LogWarning(
            "Dashboard operation rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return error.Kind switch
        {
            ErrorKind.Validation => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid dashboard request.",
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