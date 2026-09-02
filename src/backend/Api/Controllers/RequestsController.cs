using Api.Requests;
using Application.Common.Pagination;
using Application.Common.Results;
using Application.Requests;
using Application.Requests.AddComment;
using Application.Requests.AssignRequest;
using Application.Requests.ChangeRequestStatus;
using Application.Requests.CreateRequest;
using Application.Requests.GetRequestDetail;
using Application.Requests.ListRequests;
using Application.Requests.ReopenRequest;
using Application.Requests.UpdateRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/solicitudes")]
public sealed class RequestsController : ControllerBase
{
    private readonly CreateRequestHandler _createHandler;
    private readonly ListRequestsQueryHandler _listHandler;
    private readonly GetRequestDetailHandler _detailHandler;
    private readonly ChangeRequestStatusHandler _changeStatusHandler;
    private readonly AssignRequestHandler _assignHandler;
    private readonly AddCommentHandler _addCommentHandler;
    private readonly ReopenRequestHandler _reopenHandler;
    private readonly UpdateRequestHandler _updateHandler;
    private readonly ILogger<RequestsController> _logger;

    public RequestsController(
        CreateRequestHandler createHandler,
        ListRequestsQueryHandler listHandler,
        GetRequestDetailHandler detailHandler,
        ChangeRequestStatusHandler changeStatusHandler,
        AssignRequestHandler assignHandler,
        AddCommentHandler addCommentHandler,
        ReopenRequestHandler reopenHandler,
        UpdateRequestHandler updateHandler,
        ILogger<RequestsController> logger)
    {
        _createHandler = createHandler;
        _listHandler = listHandler;
        _detailHandler = detailHandler;
        _changeStatusHandler = changeStatusHandler;
        _assignHandler = assignHandler;
        _addCommentHandler = addCommentHandler;
        _reopenHandler = reopenHandler;
        _updateHandler = updateHandler;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<RequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListRequests(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] Domain.Enums.RequestStatus? status,
        [FromQuery] Domain.Enums.RequestPriority? priority,
        [FromQuery] Guid? areaId,
        [FromQuery] Guid? requestTypeId,
        [FromQuery] Guid? requesterId,
        [FromQuery] Guid? responsibleId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? code,
        [FromQuery] string? search,
        [FromQuery] RequestSortField? sortBy,
        [FromQuery] RequestSortDirection? sortDirection,
        CancellationToken cancellationToken)
    {
        var query = new ListRequestsQuery(
            Page: page ?? PaginationDefaults.DefaultPage,
            PageSize: pageSize ?? PaginationDefaults.DefaultPageSize,
            Status: status,
            Priority: priority,
            AreaId: areaId,
            RequestTypeId: requestTypeId,
            RequesterId: requesterId,
            ResponsibleId: responsibleId,
            FromDate: fromDate,
            ToDate: toDate,
            Code: code,
            Search: search,
            SortField: sortBy ?? RequestSortField.CreatedAt,
            SortDirection: sortDirection ?? RequestSortDirection.Descending);

        var result = await _listHandler.HandleAsync(query, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while listing requests.");
    }

    [HttpGet("{id:guid}", Name = nameof(GetRequestDetail))]
    [ProducesResponseType(typeof(RequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRequestDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _detailHandler.HandleAsync(new GetRequestDetailQuery(id), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while reading the request.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRequest(
        [FromBody] CreateRequestHttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "Request body is required.",
                type: "requests.create.body.missing");
        }

        if (request.Title is null
            || request.Description is null
            || request.Priority is null
            || request.AreaId is null
            || request.RequestTypeId is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request payload.",
                detail: "Title, Description, Priority, AreaId and RequestTypeId are required.",
                type: "requests.create.body.incomplete");
        }

        var command = new CreateRequestCommand(
            Title: request.Title,
            Description: request.Description,
            Priority: request.Priority.Value,
            AreaId: request.AreaId.Value,
            RequestTypeId: request.RequestTypeId.Value,
            DueDate: request.DueDate,
            EvidenceUrl: request.EvidenceUrl);

        var result = await _createHandler.HandleAsync(command, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(
                actionName: nameof(GetRequestDetail),
                routeValues: new { id = result.Value.Id },
                value: result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while creating the request.");
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateRequest(
        Guid id,
        [FromBody] UpdateRequestHttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "Request body is required.",
                type: "requests.update.body.missing");
        }

        if (request.Title is null
            || request.Description is null
            || request.Priority is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request payload.",
                detail: "Title, Description and Priority are required.",
                type: "requests.update.body.incomplete");
        }

        var command = new UpdateRequestCommand(
            RequestId: id,
            Title: request.Title,
            Description: request.Description,
            Priority: request.Priority.Value,
            DueDate: request.DueDate,
            EvidenceUrl: request.EvidenceUrl);

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while updating the request.");
    }

    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeRequestStatusHttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.NewStatus is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "NewStatus is required.",
                type: "requests.change_status.body.incomplete");
        }

        var command = new ChangeRequestStatusCommand(
            RequestId: id,
            NewStatus: request.NewStatus.Value,
            Comment: request.Comment ?? string.Empty);

        var result = await _changeStatusHandler.HandleAsync(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while changing the request status.");
    }

    [HttpPatch("{id:guid}/asignacion")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignRequestHttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.ResponsibleUserId is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "ResponsibleUserId is required.",
                type: "requests.assign.body.incomplete");
        }

        var command = new AssignRequestCommand(
            RequestId: id,
            ResponsibleUserId: request.ResponsibleUserId.Value,
            Comment: request.Comment ?? string.Empty);

        var result = await _assignHandler.HandleAsync(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while assigning the request.");
    }

    [HttpPost("{id:guid}/reapertura")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reopen(
        Guid id,
        [FromBody] ReopenRequestHttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.TargetStatus is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "TargetStatus is required.",
                type: "requests.reopen.body.incomplete");
        }

        var command = new ReopenRequestCommand(
            RequestId: id,
            TargetStatus: request.TargetStatus.Value,
            Comment: request.Comment ?? string.Empty);

        var result = await _reopenHandler.HandleAsync(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while reopening the request.");
    }

    [HttpPost("{id:guid}/comentarios")]
    [ProducesResponseType(typeof(RequestCommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] AddRequestCommentHttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Text is null || request.Visibility is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request body.",
                detail: "Text and Visibility are required.",
                type: "requests.comment.body.incomplete");
        }

        var command = new AddCommentCommand(
            RequestId: id,
            Text: request.Text,
            Visibility: request.Visibility.Value);

        var result = await _addCommentHandler.HandleAsync(command, cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return MapError(result.Error!, "An unexpected error occurred while adding the comment.");
    }

    private IActionResult MapError(Error error, string unexpectedTitle)
    {
        _logger.LogWarning(
            "Request operation rejected. Kind={ErrorKind} Code={ErrorCode} Message={ErrorMessage}",
            error.Kind,
            error.Code,
            error.Message);

        return error.Kind switch
        {
            ErrorKind.Validation => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request payload.",
                detail: error.Message,
                type: error.Code),
            ErrorKind.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Request not found.",
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
            ErrorKind.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Request conflict.",
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