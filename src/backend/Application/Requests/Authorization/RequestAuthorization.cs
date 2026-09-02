using Domain.Entities;
using Domain.Enums;

namespace Application.Requests.Authorization;

public static class RequestAuthorization
{
    public static bool CanViewRequest(
        Request request,
        Guid currentUserId,
        UserRole currentRole)
    {
        if (request is null)
        {
            return false;
        }

        if (currentRole == UserRole.Admin || currentRole == UserRole.Analista)
        {
            return true;
        }

        return request.RequesterId == currentUserId
            || request.ResponsibleId == currentUserId;
    }

    public static bool CanEditRequest(
        Request request,
        Guid currentUserId,
        UserRole currentRole)
    {
        if (request is null)
        {
            return false;
        }

        if (currentRole == UserRole.Admin || currentRole == UserRole.Analista)
        {
            return true;
        }

        return request.RequesterId == currentUserId
            && request.Status == RequestStatus.Submitted;
    }

    public static bool CanChangeStatus(
        Request request,
        Guid currentUserId,
        UserRole currentRole)
    {
        if (request is null)
        {
            return false;
        }

        return currentRole == UserRole.Admin || currentRole == UserRole.Analista;
    }

    public static bool CanReopen(
        Request request,
        Guid currentUserId,
        UserRole currentRole)
    {
        if (request is null)
        {
            return false;
        }

        return currentRole == UserRole.Admin || currentRole == UserRole.Analista;
    }

    public static bool CanAssign(
        Request request,
        Guid currentUserId,
        UserRole currentRole)
    {
        if (request is null)
        {
            return false;
        }

        return currentRole == UserRole.Admin || currentRole == UserRole.Analista;
    }

    public static bool CanViewComment(
        RequestComment comment,
        Guid currentUserId,
        UserRole currentRole,
        Guid requesterId)
    {
        if (comment is null)
        {
            return false;
        }

        if (comment.Visibility == CommentVisibility.Requester)
        {
            return true;
        }

        return currentRole == UserRole.Admin || currentRole == UserRole.Analista;
    }

    public static bool CanAddComment(
        CommentVisibility visibility,
        Guid currentUserId,
        UserRole currentRole)
    {
        if (visibility == CommentVisibility.Internal)
        {
            return currentRole == UserRole.Admin || currentRole == UserRole.Analista;
        }

        return true;
    }

    public static IQueryable<Request> ApplyVisibilityFilter(
        IQueryable<Request> query,
        Guid currentUserId,
        UserRole currentRole)
    {
        if (currentRole == UserRole.Admin || currentRole == UserRole.Analista)
        {
            return query;
        }

        return query.Where(request =>
            request.RequesterId == currentUserId
            || request.ResponsibleId == currentUserId);
    }
}