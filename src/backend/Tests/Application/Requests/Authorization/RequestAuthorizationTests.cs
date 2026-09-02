using Application.Requests;
using Application.Requests.Authorization;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Requests.Authorization;

public class RequestAuthorizationTests
{
    [Fact]
    public void CanViewRequest_AdminOrAnalista_AlwaysTrue()
    {
        var request = BuildRequest();
        Assert.True(RequestAuthorization.CanViewRequest(request, Guid.NewGuid(), UserRole.Admin));
        Assert.True(RequestAuthorization.CanViewRequest(request, Guid.NewGuid(), UserRole.Analista));
    }

    [Fact]
    public void CanViewRequest_RequesterOrResponsible_True()
    {
        var request = BuildRequest();
        Assert.True(RequestAuthorization.CanViewRequest(request, request.RequesterId, UserRole.Solicitante));
        Assert.True(RequestAuthorization.CanViewRequest(request, request.ResponsibleId!.Value, UserRole.Solicitante));
    }

    [Fact]
    public void CanViewRequest_OtherSolicitante_False()
    {
        var request = BuildRequest();
        Assert.False(RequestAuthorization.CanViewRequest(request, Guid.NewGuid(), UserRole.Solicitante));
        Assert.False(RequestAuthorization.CanViewRequest(request, Guid.NewGuid(), UserRole.User));
    }

    [Fact]
    public void CanReopen_OnlyAdminOrAnalista()
    {
        var request = BuildRequest();
        Assert.True(RequestAuthorization.CanReopen(request, Guid.NewGuid(), UserRole.Admin));
        Assert.True(RequestAuthorization.CanReopen(request, Guid.NewGuid(), UserRole.Analista));
        Assert.False(RequestAuthorization.CanReopen(request, Guid.NewGuid(), UserRole.Solicitante));
        Assert.False(RequestAuthorization.CanReopen(request, Guid.NewGuid(), UserRole.User));
    }

    [Fact]
    public void CanChangeStatus_OnlyAdminOrAnalista()
    {
        var request = BuildRequest();
        Assert.True(RequestAuthorization.CanChangeStatus(request, Guid.NewGuid(), UserRole.Admin));
        Assert.True(RequestAuthorization.CanChangeStatus(request, Guid.NewGuid(), UserRole.Analista));
        Assert.False(RequestAuthorization.CanChangeStatus(request, Guid.NewGuid(), UserRole.Solicitante));
    }

    [Fact]
    public void CanAddComment_InternalVisibility_RequiresAdminOrAnalista()
    {
        Assert.True(RequestAuthorization.CanAddComment(CommentVisibility.Internal, Guid.NewGuid(), UserRole.Admin));
        Assert.True(RequestAuthorization.CanAddComment(CommentVisibility.Internal, Guid.NewGuid(), UserRole.Analista));
        Assert.False(RequestAuthorization.CanAddComment(CommentVisibility.Internal, Guid.NewGuid(), UserRole.Solicitante));
    }

    [Fact]
    public void CanAddComment_RequesterVisibility_AllowedForAll()
    {
        Assert.True(RequestAuthorization.CanAddComment(CommentVisibility.Requester, Guid.NewGuid(), UserRole.Solicitante));
        Assert.True(RequestAuthorization.CanAddComment(CommentVisibility.Requester, Guid.NewGuid(), UserRole.Analista));
        Assert.True(RequestAuthorization.CanAddComment(CommentVisibility.Requester, Guid.NewGuid(), UserRole.Admin));
    }

    [Fact]
    public void CanViewComment_Internal_OnlyAdminOrAnalista()
    {
        var request = BuildRequest();
        var comment = new RequestComment(
            new User("a", "a@x", "h", UserRole.Analista),
            "Internal.",
            CommentVisibility.Internal,
            DateTime.UtcNow);

        Assert.True(RequestAuthorization.CanViewComment(comment, Guid.NewGuid(), UserRole.Admin, request.RequesterId));
        Assert.True(RequestAuthorization.CanViewComment(comment, Guid.NewGuid(), UserRole.Analista, request.RequesterId));
        Assert.False(RequestAuthorization.CanViewComment(comment, request.RequesterId, UserRole.Solicitante, request.RequesterId));
    }

    [Fact]
    public void CanViewComment_RequesterVisibility_Anyone()
    {
        var request = BuildRequest();
        var comment = new RequestComment(
            new User("a", "a@x", "h", UserRole.Solicitante),
            "Public.",
            CommentVisibility.Requester,
            DateTime.UtcNow);

        Assert.True(RequestAuthorization.CanViewComment(comment, request.RequesterId, UserRole.Solicitante, request.RequesterId));
        Assert.True(RequestAuthorization.CanViewComment(comment, Guid.NewGuid(), UserRole.Admin, request.RequesterId));
    }

    private static Request BuildRequest()
    {
        var requester = new User("requester", "r@x", "h", UserRole.Solicitante);
        var responsible = new User("responsible", "resp@x", "h", UserRole.Analista);
        var area = new Area("Atención");
        var type = new RequestType("Incidente", "Reporte");
        var request = new Request(
            code: "SOL-2026-0001",
            title: "Title",
            description: "Description",
            priority: RequestPriority.Medium,
            requester: requester,
            area: area,
            requestType: type,
            createdAt: DateTime.UtcNow,
            dueDate: null);
        request.AssignResponsible(responsible, requester, DateTime.UtcNow, "Routing.");
        return request;
    }
}