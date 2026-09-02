using Domain.Enums;

namespace Application.Requests.ChangeRequestStatus;

public sealed record ChangeRequestStatusCommand(
    Guid RequestId,
    RequestStatus NewStatus,
    string Comment);
