using Domain.Enums;

namespace Application.Requests.ReopenRequest;

public sealed record ReopenRequestCommand(
    Guid RequestId,
    RequestStatus TargetStatus,
    string Comment);