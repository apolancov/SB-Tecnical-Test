using Domain.Entities;
using Domain.Enums;

namespace Application.Requests.Notifications;

public static class RequestNotificationFactory
{
    public const string RequestCreatedSubject = "Solicitud registrada";
    public const string RequestAssignedSubject = "Solicitud asignada";
    public const string RequestReassignedSubject = "Solicitud reasignada";
    public const string RequestStatusChangedSubject = "Estado de la solicitud actualizado";
    public const string RequestClosedSubject = "Solicitud cerrada";
    public const string RequestReopenedSubject = "Solicitud reabierta";

    public static RequestNotification Created(
        Request request,
        User destination)
    {
        return Build(
            request,
            destination,
            RequestCreatedSubject,
            $"Se ha registrado la solicitud {request.Code}: {request.Title}.");
    }

    public static RequestNotification Assigned(
        Request request,
        User destination,
        User responsible)
    {
        return Build(
            request,
            destination,
            RequestAssignedSubject,
            $"La solicitud {request.Code} ha sido asignada a {responsible.Username}.");
    }

    public static RequestNotification Reassigned(
        Request request,
        User destination,
        User responsible)
    {
        return Build(
            request,
            destination,
            RequestReassignedSubject,
            $"La solicitud {request.Code} ha sido reasignada a {responsible.Username}.");
    }

    public static RequestNotification StatusChanged(
        Request request,
        User destination,
        RequestStatus newStatus)
    {
        return Build(
            request,
            destination,
            RequestStatusChangedSubject,
            $"La solicitud {request.Code} cambió al estado {newStatus}.");
    }

    public static RequestNotification Closed(
        Request request,
        User destination,
        string resolutionComment)
    {
        return Build(
            request,
            destination,
            RequestClosedSubject,
            $"La solicitud {request.Code} fue cerrada. Resolución: {resolutionComment}.");
    }

    public static RequestNotification Reopened(
        Request request,
        User destination,
        string comment)
    {
        var message = string.IsNullOrWhiteSpace(comment)
            ? $"La solicitud {request.Code} fue reabierta."
            : $"La solicitud {request.Code} fue reabierta. Motivo: {comment}.";
        return Build(request, destination, RequestReopenedSubject, message);
    }

    private static RequestNotification Build(
        Request request,
        User destination,
        string subject,
        string message)
    {
        return request.RegisterNotification(
            destinationUser: destination,
            channel: NotificationChannel.InApp,
            status: NotificationStatus.Pending,
            subject: subject,
            message: message,
            notificationDate: DateTime.UtcNow);
    }
}