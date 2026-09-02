using Application.AuditLog;
using Application.Common.Results;
using Application.AuditLog.GetAuditLogEntries;
using Microsoft.Extensions.Logging;

namespace Application.AuditLog.GetAuditLogEntryById;

public sealed class GetAuditLogEntryByIdHandler
{
    private readonly IAuditLogReadRepository _repository;
    private readonly ILogger<GetAuditLogEntryByIdHandler> _logger;

    public GetAuditLogEntryByIdHandler(
        IAuditLogReadRepository repository,
        ILogger<GetAuditLogEntryByIdHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<AuditLogEntryDto>> HandleAsync(
        GetAuditLogEntryByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Id == Guid.Empty)
        {
            _logger.LogWarning(
                "Rejected audit log entry lookup because the id was empty.");
            return Result<AuditLogEntryDto>.Failure(
                Error.Validation(
                    "Audit log entry id must be a non-empty Guid.",
                    "auditlog.read.id.invalid"));
        }

        try
        {
            var entry = await _repository.GetByIdAsync(query.Id, cancellationToken);
            if (entry is null)
            {
                _logger.LogWarning(
                    "Audit log entry not found. Id={EntryId}",
                    query.Id);
                return Result<AuditLogEntryDto>.Failure(
                    Error.NotFound(
                        $"Audit log entry '{query.Id}' was not found.",
                        "auditlog.read.not_found"));
            }

            return Result<AuditLogEntryDto>.Success(entry);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Audit log entry read failed. Id={EntryId}",
                query.Id);
            return Result<AuditLogEntryDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while reading the audit log entry.",
                    "auditlog.read.failed"));
        }
    }
}