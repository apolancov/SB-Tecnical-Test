using Application.Common.Pagination;
using Domain.Entities;

namespace Application.Institutions.GetInstitutions;

public interface IInstitutionReadRepository
{
    Task<PaginatedResult<InstitutionDto>> SearchAsync(
        InstitutionQueryCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Institution?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
}