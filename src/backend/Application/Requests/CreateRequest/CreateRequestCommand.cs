using Domain.Enums;

namespace Application.Requests.CreateRequest;

public sealed record CreateRequestCommand(
    string Title,
    string Description,
    RequestPriority Priority,
    Guid AreaId,
    Guid RequestTypeId,
    DateTime? DueDate,
    string? EvidenceUrl);