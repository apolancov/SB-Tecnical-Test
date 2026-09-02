using Application.Common.Audit;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Audit;

public sealed class AuditLogger : IAuditLogger
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditContextAccessor _contextAccessor;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(
        ApplicationDbContext context,
        IAuditContextAccessor contextAccessor,
        ILogger<AuditLogger> logger)
    {
        _context = context;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(AuditLogContext context, CancellationToken cancellationToken)
    {
        try
        {
            var entry = new AuditLogEntry(
                action: context.Action,
                outcome: context.Outcome,
                entityType: context.EntityType,
                entityId: context.EntityId,
                details: context.Details,
                ipAddress: _contextAccessor.GetClientIpAddress(),
                actorUserId: context.ActorUserId,
                actorUserName: context.ActorUserName);

            _context.AuditLog.Add(entry);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Audit log entry written. Id={EntryId} Action={Action} Outcome={Outcome} EntityType={EntityType}",
                entry.Id,
                entry.Action,
                entry.Outcome,
                entry.EntityType);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to write audit log entry. Action={Action} Outcome={Outcome} EntityType={EntityType}",
                context.Action,
                context.Outcome,
                context.EntityType);
        }
    }
}