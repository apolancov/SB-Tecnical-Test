using Application.Common.Pagination;
using Application.AuditLog;

namespace Application.AuditLog.GetAuditLogEntries;

public interface IAuditLogReadRepository
{
    Task<PaginatedResult<AuditLogEntryDto>> SearchAsync(
        AuditLogQueryCriteria criteria,
        int page,
        int pageSize,
        AuditLogSortOptions sortOptions,
        CancellationToken cancellationToken);

    Task<AuditLogEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}