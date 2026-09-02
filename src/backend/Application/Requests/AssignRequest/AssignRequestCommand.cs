namespace Application.Requests.AssignRequest;

public sealed record AssignRequestCommand(
    Guid RequestId,
    Guid ResponsibleUserId,
    string Comment);
