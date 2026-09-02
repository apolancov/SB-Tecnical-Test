using Domain.Enums;

namespace Application.Requests.UpdateRequest;

public sealed record UpdateRequestCommand(
    Guid RequestId,
    string Title,
    string Description,
    RequestPriority Priority,
    DateTime? DueDate,
    string? EvidenceUrl);