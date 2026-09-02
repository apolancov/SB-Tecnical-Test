using Domain.Enums;

namespace Domain.Entities;

public static class RequestStatusTransition
{
    private static readonly Dictionary<RequestStatus, RequestStatus[]> AllowedTransitions = new()
    {
        [RequestStatus.Draft] = new[] { RequestStatus.Submitted, RequestStatus.Cancelled },
        [RequestStatus.Submitted] = new[] { RequestStatus.InReview, RequestStatus.Cancelled, RequestStatus.Assigned },
        [RequestStatus.InReview] = new[] { RequestStatus.Assigned, RequestStatus.InProgress, RequestStatus.OnHold, RequestStatus.Cancelled },
        [RequestStatus.Assigned] = new[] { RequestStatus.InProgress, RequestStatus.OnHold, RequestStatus.Cancelled },
        [RequestStatus.InProgress] = new[] { RequestStatus.OnHold, RequestStatus.Resolved, RequestStatus.Cancelled },
        [RequestStatus.OnHold] = new[] { RequestStatus.InProgress, RequestStatus.Cancelled },
        [RequestStatus.Resolved] = new[] { RequestStatus.Closed, RequestStatus.InProgress, RequestStatus.Cancelled },
        [RequestStatus.Closed] = new[] { RequestStatus.InProgress },
        [RequestStatus.Cancelled] = Array.Empty<RequestStatus>()
    };

    public static bool IsAllowed(RequestStatus from, RequestStatus to)
    {
        if (from == to)
        {
            return false;
        }

        if (!AllowedTransitions.TryGetValue(from, out var allowed))
        {
            return false;
        }

        return Array.IndexOf(allowed, to) >= 0;
    }

    public static bool IsReopen(RequestStatus from, RequestStatus to)
    {
        return from == RequestStatus.Closed && to != RequestStatus.Closed;
    }
}