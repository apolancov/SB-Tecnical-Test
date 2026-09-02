using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Common.Audit;
using Microsoft.AspNetCore.Authorization;

namespace Api.Audit;

public sealed class AuthorizationAuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationAuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var authorizeAttribute = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>();

        await _next(context);

        if (authorizeAttribute is null)
        {
            return;
        }

        if (context.Response.StatusCode != StatusCodes.Status403Forbidden)
        {
            return;
        }

        var auditLogger = context.RequestServices.GetService<IAuditLogger>();
        if (auditLogger is null)
        {
            return;
        }

        var policyName = ResolvePolicyName(authorizeAttribute);
        var actorId = ResolveActorId(context.User);
        var actorUserName = ResolveActorUserName(context.User);
        var endpointPath = $"{context.Request.Method} {context.Request.Path}";
        var clientIpAddress = ResolveClientIpAddress(context);

        try
        {
            await auditLogger.LogAuthorizationDeniedAsync(
                policyName: policyName,
                endpoint: endpointPath,
                actorUserId: actorId,
                actorUserName: actorUserName,
                ipAddress: clientIpAddress,
                cancellationToken: context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
        }
    }

    private static string ResolvePolicyName(AuthorizeAttribute attribute)
    {
        if (!string.IsNullOrWhiteSpace(attribute.Policy))
        {
            return attribute.Policy!;
        }

        if (attribute.Roles is { } roles && !string.IsNullOrWhiteSpace(roles))
        {
            return $"Roles:{roles}";
        }

        return "Authenticated";
    }

    private static Guid? ResolveActorId(ClaimsPrincipal user)
    {
        if (user is null)
        {
            return null;
        }

        var subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(subject) || !Guid.TryParse(subject, out var userId))
        {
            return null;
        }

        return userId == Guid.Empty ? null : userId;
    }

    private static string? ResolveActorUserName(ClaimsPrincipal user)
    {
        if (user is null)
        {
            return null;
        }

        var username = user.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.Identity?.Name;

        return string.IsNullOrWhiteSpace(username) ? null : username;
    }

    private static string? ResolveClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var firstForwarded = forwardedFor
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstForwarded))
            {
                return firstForwarded;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}