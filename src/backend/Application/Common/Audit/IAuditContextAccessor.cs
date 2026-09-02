namespace Application.Common.Audit;

public interface IAuditContextAccessor
{
    string? GetClientIpAddress();
}