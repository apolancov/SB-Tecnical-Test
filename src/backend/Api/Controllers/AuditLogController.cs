using Api.Common.Authorization;
using Application.AuditLog;
using Application.AuditLog.GetAuditLogEntries;
using Application.AuditLog.GetAuditLogEntryById;
using Application.Common.Pagination;
using Application.Common.Results;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
[Route("api/auditoria")]
public sealed class AuditLogController : ControllerBase
{
    private readonly GetAuditLogEntriesQueryHandler _listHandler;
    private readonly GetAuditLogEntryByIdHandler _detailHandler;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(
        GetAuditLogEntriesQueryHandler listHandler,
        GetAuditLogEntryByIdHandler detailHandler,
        ILogger<AuditLogController> logger)
    {
        _listHandler = listHandler;
        _detailHandler = detailHandler;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<AuditLogEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? actorUserId,
        [FromQuery] AuditAction? action,
        [FromQuery] AuditOutcome? outcome,
        [FromQuery] string? entityType,
        [FromQuery] string? search,
        [FromQuery] AuditLogSortField? sortBy,
        [FromQuery] AuditLogSortDirection? sortDirection,
        CancellationToken cancellationToken)
    {
        var query = new GetAuditLogEntriesQuery(
            Page: page ?? PaginationDefaults.DefaultPage,
            PageSize: pageSize ?? PaginationDefaults.DefaultPageSize,
            FromDate: fromDate,
            ToDate: toDate,
            ActorUserId: actorUserId,
            Action: action,
            Outcome: outcome,
            EntityType: entityType,
            Search: search,
            SortField: sortBy ?? AuditLogSortField.Timestamp,
            SortDirection: sortDirection ?? AuditLogSortDirection.Descending);

        var result = await _listHandler.HandleAsync(query, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(
            result.Error!,
            "An unexpected error occurred while querying the audit log.",
            "audit log query");
    }

    [HttpGet("{id:guid}", Name = nameof(GetAuditLogEntryById))]
    [ProducesResponseType(typeof(AuditLogEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuditLogEntryById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _detailHandler.HandleAsync(
            new GetAuditLogEntryByIdQuery(id),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(
            result.Error!,
            "An unexpected error occurred while reading the audit log entry.",
            "audit log read");
    }

    private IActionResult MapError(Error error, string unexpectedTitle, string operation)
    {
        _logger.LogWarning(
            "Audit log operation rejected during {Operation}. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            operation,
            error.Kind,
            error.Code,
            error.Message);

        return error.Kind switch
        {
            ErrorKind.Validation => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid audit log request.",
                detail: error.Message,
                type: error.Code),
            ErrorKind.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Audit log entry not found.",
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